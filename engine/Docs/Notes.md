# Port notes

These notes were kept in Claude's local memory during the engine work (2026-10-16 to 2026-10-30)
and moved into the repo on 2026-10-30, when the project went to GitHub. `Docs/Engine-Plan.md` is the
main document; this file keeps the history, working rules and gotchas that aren't there. Paths are
the repo's (`engine/` was `/run/media/system/M.2_2TB/Stat Fick Source`, `old-game/` was `.../Stats`).
`claude-work/` means `~/.cache/stat-fick-build/claude-work/` on the original PC (not in the repo).
The capture recipes that used `~/.cache/stat-fick-build/chromium-152/stat-fick-tests` now use
`Tools/Scripts/chromium.sh`.

The user asked (2026-10-16) for a full engine on BRUTAL (C#/Vulkan, see the BRUTAL test notes below) and a complete port of Stat Fick (the old game's notes (old-game/Docs/Notes.md)).

**Engine name:** Statventure (the user chose it on 2026-10-16). Namespaces are `Statventure.*`.

**Order of work:**
1. Build the engine first, fully tested and functional so it "handles the game perfectly".
2. Then port the game.
3. Check over every system and function of the game.

**Working rules:**
- "If any questions arise, ask them instantly and do not improvise anything." See the design-choice rule (CLAUDE.md): question tool, options with a recommendation, answer recorded in Engine-Plan.md.
- Start every session with `Stat Fick Source/Docs/Engine-Plan.md` → "How to continue" (current task, method, open questions per milestone); keep that file, the area's design doc and this memory current after each step.
- Stop when a milestone is finished (tests pass, ticked, notes updated): report and wait for the user's go-ahead before the next milestone (user rule, 2026-10-25).
- Remake every character, enemy and item in Blender, more detailed and more consistent, in the same art style.
- Scenery is remade in Blender too.
- Give the shipped game a proper game-like folder structure.

**Folders:**
- `/run/media/system/M.2_2TB/Stat Fick/`: only what the game needs to run. The user had it emptied on 2026-10-16.
- `engine/`: engine and game source, tests, Blender sources, tools. There's no git, so this is the only copy.
- `~/.cache/stat-fick-build/`: the .NET SDK and NuGet caches, which can be re-downloaded.
- The old JS game in `old-game` stays the reference.

**User decisions (2026-10-16):**
- **Characters, enemies, items:** real-time 3D models from Blender, drawn by the engine with cel shading and ink lines. They need full animation sets: idle, attacks per weapon type, cast, block, dodge, hit, death, victory, boss specials.
- **Battles:** the same fixed side view as now (orthographic), on a real-time 3D stage built in Blender, cel-shaded with ink lines.
- **Region map:** real-time 3D terrain from Blender, seen from above in the ink style, with zoom and pan.
- **Banners and cutscenes:** also remade in Blender.
- **UI:** a custom game UI on BRUTAL (FreeType text with the game's Lato/STIX fonts), recreating the parchment look. Not ImGui.
- **Layout:** responsive (panels reflow like the web page), not a fixed 16:9.
- **Music:** port the live synthesizer (`music-composer.js`/`music-themes.js`) to C#. Audio through OpenAL Soft.
- **Saves:** a fresh save format; old saves are not imported.
- **Data:** only classes stay `.json` (shipped). Everything else (enemies, items, spells, races, regions, forge, music themes) goes into binary `.pak` files, created and edited with an editor tool built on the engine UI that saves straight into the packs.
- **Platforms:** Linux and Windows. Windows can be built here but not tested.
- **Symbol icons:** ♜ ♡ ◈ ▣ ◌ ◫ ◆, which the game set in "Georgia, serif" (not shipped, so each OS substituted its own font), become vector icons drawn by the engine. The user chose this on 2026-10-16.
- **Text antialiasing:** follow the system as Chromium does (fontconfig or ClearType); LCD subpixel where possible. The user chose this on 2026-10-16.
- **Text:** FreeType for glyphs, through my own bindings, because BRUTAL's FreeType package ships natives only. HarfBuzzSharp (MIT) does the shaping; the user approved it.
- **UI authoring:** CSS-like stylesheets (as in Unity's UI Toolkit) compiled into the packs, so the current `styles.css` carries over nearly rule by rule. Screens are built in C#.
- **Tooltips and dropdown lists (2026-10-17):** `title` tooltips and the open list of a `<select>` are drawn in the game's style (like the leather combat tooltips; parchment list, game font), not Chromium's plain look. The engine makes them styleable elements; the game's stylesheet gives the look at port time.
- **Form validation (2026-10-18):** Chromium's checks (required, min/max/step, maxlength) block the submit and focus the field; Chromium's message shows in a game-styled bubble (styleable element, like tooltips).
- **No spellcheck (2026-10-18)** in text fields.
- **Grabbing cursor (2026-10-18):** game-styled cursor image (engine supports image cursors with hotspots).
- **No menu bar, no OS dialogs (2026-10-17):** the desktop app's menu (Save/Load Character, Open game folder, Quit, Reload, Zoom, Fullscreen, About) and its native file/message dialogs move into the game's own UI, file picking included.
- **LCD vs grayscale text (2026-10-24):** match Chromium exactly: port Blink's compositing decisions (which elements get their own cc layer, whether each is opaque) so text is grayscale exactly where Chromium's is (the game's fixed side menu, modal panels, scrolling lists) and LCD elsewhere. The user chose this over "LCD everywhere".
- **Inline SVG (2026-10-25):** port CSS transforms for SVG children only (transform attribute as presentation attribute, transform-box/origin, transform animations, Blink visual rects); fill/stroke/opacity/filters stay attribute-only (art becomes Blender renders).
- **Path antialiasing (2026-10-25):** keep the canvas's own path rasterizer (quarter-pixel cache, flattened curves, own stroker); edge differences from Skia are a known difference, not ported.
- **Game code scheduling (2026-10-25):** Chromium's event loop in the engine (SetTimeout/SetInterval, QueueMicrotask, RequestAnimationFrame, events, ResizeObserver, matchMedia in Chromium's task/microtask/frame order); C# async/await runs on it (continuations = microtasks; an element animation's Finished is a Task).
- **Web Animations (2026-10-30):** core of element.animate (offsets, per-keyframe easing, duration/delay/endDelay/iterations/direction/fill/easing, Finished/Ready tasks, Cancel/Finish/Pause/Play, PlayState/CurrentTime/StartTime, finish/cancel events, composited after CSS animations); not KeyframeEffect/playbackRate/commitStyles/replace/composite modes/getAnimations.
- **Keyframes in C# (2026-10-30):** `new Keyframe { ["transform"] = "…", Offset = 0.3 }` dictionaries of CSS text + typed `AnimateOptions { Duration, Easing, Fill }`, times in ms.
- **Toolchain:** the SDK is in `~/.cache/stat-fick-build/dotnet` (10.0.402), the NuGet cache in `~/.cache/stat-fick-build/nuget`, and the BRUTAL source in `~/.cache/stat-fick-build/brutal-src`.
- **Packages:** all BRUTAL packages are on 2026.10.x and ship both linux-x64 and win-x64 natives. VMA's import name is `vma`; OpenAL's is `openal`.

The 43 MP3s in `Stats/music/` are not referenced by the game; music is composed live.

**Progress:** see `Stat Fick Source/Docs/Engine-Plan.md`, the living plan with milestones M0–M12 and a progress section.
- 2026-10-16: M0–M3 are done (GPU core; canvas and text matching the game's Chromium renders through the test client; 42 tests).
- 2026-10-17: M4 is done (129 tests): styles, layout, painting and animations match Chromium 152 (36 fixtures, 11 captured game screens box for box and by screenshot). Updates are incremental (invalidation sets, shared styles, persistent boxes, a layout cache); `IncrementalUpdateTests` compares random changes against fresh documents.
- 2026-10-18: M5 in progress (187 tests): hit testing, events, focus/keyboard and scrolling+sticky done (tasks #24-#26); next #27 form controls/text editing, then #28 tooltips/window host.
- 2026-10-18 (later, 215 tests): #27 mostly done: click activation (checkbox/radio/label/summary/buttons/forms), spin buttons, range, search cancel, select keys + game-styled popup (`sv-select-popup`), text-field editing (Blink key bindings, undo coalescing, ICU word breaks from generated Unicode 17 tables via `Tools/UnicodeData/generate.py`, backspace state machine, number filtering, maxlength), mouse selection in fields. Page-text selection model done (press/drag/double/triple, Ctrl+A, copy). Remaining in #27: caret and selection painting (screenshot references), textarea scrolling, animationend/transitionend.
- 2026-10-23 (221 tests): painting matched to Chromium screenshots: Blink caret rect (round(x−0.5) clamped, font height), outline: auto focus rings (`UiPainter.Outlines.cs`: OutlinePainter port, union of outline rects incl. block ink overflow, region boundary + SkCornerPathEffect), fast-selection text painting (unselected glyphs, highlight, selected glyphs clipped), line-break highlight (font space width), aliased text-field border (Skia non-AA), select last_on_change_option_ on option insert/remove. Harness flushes 2 rAFs per step (references recaptured). Text decorations then ported from Blink (applied decorations in style, decorating boxes, offsets, styles, skip-ink via unhinted FreeType outlines, overlay selection); 224 tests. Known gaps listed in Engine-Plan.md (M5 "Known differences left"). Then textarea scrolling/reveal/page keys (225 tests, 1 failing: field-scroll screenshots need the textarea resizer + scrollbar corner + ring over scrollbar). Unicode 17 data for generate.py: download from unicode.org/Public/17.0.0 (system /usr/share/unicode is 18).
- 2026-10-23 (later, 233 tests): **#27 done.** Textarea resizer + scroll corner; CSS animation/transition events ported from Blink's event delegates (`AnimationEngine.UpdateTiming` at frame start, queue dispatched after scroll events, cancels queued at once), transition update reworked to Blink's order (cancel first, before-change sampled now, reversing shortening factor), finished animations no longer restart on restyle; ancestor-filter bug fixed (`#a ~ #b > #c`). Input fixture `animation-events` logs elapsed to a tenth; opt-in event types via any element's `data-log`. Recapture a single new fixture by filtering `Reference/input-fixtures.json` into scratch and running `~/.cache/stat-fick-build/chromium-152/stat-fick-tests` directly, then merge its key into `input-results.json` (avoids re-recording every fixture). Then **#28 done** (254 tests): title tooltips ported from Blink ChromeClient + views TooltipController (`UiInput.Tooltips.cs`, sv-tooltip in top layer), `UiHost`/`UiHostInput` (GLFW → DOM key/code, wheel notches, focus, cursors incl. `cursor: url() x y` images), `Platform/SystemSettings` (GTK blink/animations via gsettings or settings.ini; Win32), sandbox = UI demo. GLFW now reports content scale 1.65 here (KDE Wayland). Old game uses no HTML drag&drop/<dialog>; uses element.animate, rAF, ResizeObserver, matchMedia, setPointerCapture. Next: #29 fractional-scale paint check vs Chromium, #30 Electron-style page zoom (level ±0.5, 1.2^level), #31 script APIs (rAF, Web Animations, ResizeObserver, matchMedia).
- 2026-10-24 (273 tests): #29/#30 zoom mostly in: document zoom API, zoomed styles/theme/scrollbars, DOM geometry in CSS px; all 1.65 paint+layout references pass. Key finding: Chromium 152 on Linux renders every web font via Fontations + Skia path rasterization (not FreeType's renderer); engine ported this (`Engine/Statventure/Text/Skia`: AAA scan converter, LCD 4× FIR + RGB565, SkMaskGamma contrast 0.2/gamma 1.2 (internal 51/255, 76/64), 5-bit CPU LCD blend, Skia glyph position rounding). References are CPU-rastered (golden LCD values = 255 − ((255·m)>>5)). Skia sources cached in `~/.cache/stat-fick-build/reference-src/skia` (rev b6d10629…), skrifa v0.44 in `.../reference-src/skrifa`. Next: rest of zoom (input DIP→device px, wheel/scroll steps, focus ring/caret, popups at zoom, page-zoom refs, host SetZoom, layout/game/input refs at 1.65), then port skrifa's autohinter (FreeType 2.14 afadjust differs at 1×), LCD text in opacity layers (cc preserves LCD: layer initialised from backdrop), Skia-stroked fake bold, then #31.
- 2026-10-24 (later, 418 tests): **#29/#30 zoom done.** References at 1.65 and 1.65×1.2 (paint, layout, game screens, input via `make-input-references.sh 1.65`). Capture gotchas: Electron's capturePage resamples unless the window is whole device px (blurred goldens looked like "AA differences" — check capture before blaming the engine); the scaled input harness needs `--disable-frame-rate-limit` (off-screen X11 windows get a frame per 2 s) and records the viewport (the WM adds pixels). Next: port skrifa 0.44's autohinter (source in `~/.cache/stat-fick-build/reference-src/skrifa/autohint`, generated styles alongside; Chromium enables `autohint_shaping`), then LCD text in opacity layers, Skia fake bold, then #31.
- 2026-10-24 (later, 421 tests): skrifa autohinter ported (`Engine/Statventure/Text/Autohint`, data via `Tools/Autohint/generate.py`); glyph sheets at 1× (`TextHintingReferenceTests`, `make-scaled-paint-references.sh 1 1 text-fixtures.json`) match Chromium's hinted outlines exactly. Glyph advances now use skrifa's FreeType-style fixed scale (`TextLayout.Advance`), fixing quarter-pixel flips. Next: LCD text in opacity layers, Skia-stroked fake bold, then #31.
- 2026-10-25 (445 tests, 2 failing): Blink compositing port (#30) mostly done: property trees + paint chunks + layerizer (`Engine/Statventure/UI/Paint/Compositing`), two paint passes (record-only analysis, then draw with per-chunk LCD verdicts). `CompositingReferenceTests` vs cc-trace layer lists: 20/22 game screens exact; only shop (1×, 1.65) open (SVG overflow clip missing from UA sheet, disabled-button layers, SVG child transform nodes). Everything (rules learned, file map, `COMPOSITING_CHUNKS=1` debug dump, open items) is in `Stat Fick Source/Docs/Compositing-Port.md`. Lesson: when layers differ, dump chunks and find the Blink paint-order/chunking rule (atomic flex/grid items, layer subsequences, paint offset translations) before touching the layerizer. Next: shop, then Skia fake bold, then #31.
- 2026-10-25 (later, 467 tests): **#30 compositing done, all 22 game screens.** Shop cause 1 was the capture setup: shop stock is `Math.random` and layer traces are a separate run → `game-capture.js` seeds Math.random around `Shop.open` (references recaptured). Ported Blink svg.css (inline svg overflow hidden, SVG elements origin 0 0), replaced boxes never scroll containers, flex/grid auto-min by overflow value. User decisions (2026-10-25): inline SVG gets CSS transforms only (transform attribute = presentation attribute, transform-box, TransformHelper; fill/stroke/opacity/filters stay attribute-only); path antialiasing keeps the canvas rasterizer (edges may differ from Skia; SVG fixtures at tolerance 60). New `svg-transforms` paint fixture; capture one fixture by filtering `paint-fixtures.js`/`.json` into a temp list (scaled script takes a list name in `Reference/` as 3rd arg). Next: Skia stroked fake bold, then #31.
- 2026-10-25 (later, 472 tests): **Skia fake bold done**: SkStroke port (`Text/Skia/SkiaStroke.cs` etc.), stroke-and-fill of the glyph path; FreeType no longer draws text. Found a real 1.65 text bug on the way: glyphs must be rasterized at Blink's EffectiveFontSize (floor to 0.01 px, 12 px × 1.65 = 19.79), as advances already were. STIX/Lato glyph sheets at 1× and 1.65 now exact. Gotcha: the PaintReference harness only loaded Lato in paint mode, so Chromium used the system's STIX Two Text (with a real Bold) — check which font Chromium actually used before comparing. Next: #31 script APIs.
- 2026-10-30 (481 tests): #31 part 1 done: `UiEventLoop` (timers/microtasks/async on a SynchronizationContext), rAF, matchMedia, resize event, ResizeObserver, Blink frame order; `Update` runs frame steps once per frame. Script harness `Tools/ScriptReference` + `make-script-references.sh`; fixtures are JS + C# twins in `ScriptFixtures.cs` (logs tagged with frames; use 0 ms timers only, delays are real time in Chromium). Design doc `Docs/Scripting.md`. Next: Web Animations (decided scope above), then rest of M5 checklist.
- 2026-10-30 (later, 491 tests): **#31 done**: Web Animations (`UiAnimation.cs`, script section of `AnimationEngine`), 13 script fixtures match Chromium (incl. the game's battle keyframes via seek-and-sample). Gotcha: Chromium's offscreen frame times jitter ~±0.1 ms, so live-playback fixtures need durations whose per-frame values stay ≥0.5 ms from rounding edges/frame boundaries; capture at least twice. Next: check the M5 checklist (drag and drop = pointer capture only in the game), then tick M5 and stop.
- 2026-10-30 (end, 493 tests): **M5 done and ticked.** Last step: `inert` (UA `[inert] { interactivity: inert !important }`, IsCSSInert inherited, used pointer-events/user-select none, unfocusable, attribute is style-only), plus two general Blink rules found via the `inert` input fixture: :hover under a still pointer is recomputed only after layout/scroll (not paint-only changes), and focus fixup blurs in a task with blur/focusout. Single-fixture capture: filter `input-fixtures.json` into scratch, run the harness binary (`--ozone-platform=x11 --scale=1.65` for scaled), merge the key into the results file. Open for the port: window `visibilitychange`/`pagehide`. **Next: M6 only when the user says so.**
- 2026-10-30: **Moved to GitHub**: the folders moved into the repo folder `/run/media/system/M.2_2TB/Stat Fick Repo/` (`engine/` was `Stat Fick Source/`, `old-game/` was `Stats/`; backups of both in `~/.cache/stat-fick-build/claude-work/github-move-backup/`) and went to the private repo github.com/Vatrix33/statfick. Remote work: Claude Code cloud (+phone) and a Windows PC (build/run/test); Claude's memory notes moved into the repo docs; the reference-src cache stays out. Details in Engine-Plan.md "Decisions for the move to GitHub".
- 2026-10-30 (cloud, 496 tests): border styles/widths classified by used widths (`StyleDifference.BorderFields`): layout only when a used width changes, else repaint (Blink's DiffNeedsFullLayout, kBorderWidth). Gotcha: this cloud session started without .NET (the environment's setup script hadn't run); installing the packages `cloud-setup.sh` lists (`dotnet-sdk-10.0 mesa-vulkan-drivers libvulkan1 vulkan-tools fontconfig xvfb xauth`) with apt was enough for `test.sh`.
- 2026-10-30 (501 tests): **M6 step 1 (3D core) done**: `Engine/Statventure/Render3D/` (Camera, Mesh, RenderTarget3D, Renderer3D), shaders `mesh*.{vert,frag,glsl}`, Blender reference harness (`Tools/BlenderReference/render_scenes.py`, `make-blender-references.sh`; scenes from `Render3D/Scenes3D.cs` via `Reference/r3d-scenes.json`). Gotchas: a System.Numerics row is a column of the column-vector matrix (glTF/Blender order), the first scene file had them transposed; front faces are CounterClockwise despite the projection's Y flip (Clockwise culled the front faces, visible only where boxes intersect). EEVEE renders take ~0.03 s each. Details in `Docs/Renderer-3D.md`. Next: step 2 (glTF import).
- 2026-10-30 (515 tests): **M6 step 2 (glTF import) done**: `Render3D/GltfImport.cs`, `ModelData.cs`, `Palette.cs`, `Model.cs`; test models from `Tools/BlenderReference/make_models.py` (run by `make-blender-references.sh` before the renders). BRUTAL.Gltf facts: `float4x4` X/Y/Z/W are glTF's columns = System.Numerics rows; nodes without matrix/TRS come back as identity; `GltfLoader(path)` loads .glb and data-URI .gltf, but a stream-loaded GLB can't reach its BIN chunk; no sparse support (ours decodes it). Gotchas: a skinned mesh ignores its node's transform (placed by joints); Blender exports mirrored objects as negative scale (flip winding); Blender materials are double-sided unless backface culling is on; in the Blender reference a mesh shared by two objects shares its material slots (replace each material once). Next: step 3 (cel shading).
- 2026-10-30 (521 tests): **M6 step 3 (cel shading) done**: `Shaders/Source/mesh_cel.frag`, `SunLight`/`CelStyle` in `Render3D/Renderer3D.cs`, palette shadow tones. The lizardman script (its files deleted 2026-10-16) is recoverable from session transcript `fceb096b-…jsonl` (search `ShaderNodeShaderToRGB`); its rule: Diffuse → Shader to RGB → RGB to BW → constant ramp at 0.3, sun strength 3, shadow = mix(lit, ink #130e0b, 0.42) in sRGB, no world. EEVEE facts (probed): white diffuse under a sun = strength/π · cos θ; a world can show the background to camera rays only (Light Path "Is Camera Ray") without lighting anything; turn off `use_raytracing`/`use_fast_gi` so only lights count. Test sensitivity: band-edge errors only show where the band crosses large smooth surfaces (`cel-closeup`). Next: step 4 (ink lines vs Freestyle).
- **Input references:** `Tools/InputReference` is an Electron harness run by the old test client's Electron (Chromium 152, unpacked to `~/.cache/stat-fick-build/chromium-152`) via `Tools/Scripts/make-input-references.sh`: real trusted input (`sendInputEvent`, held buttons as modifiers, wheel ticks), window shown offscreen for real focus. Fixtures in `Tests/.../Ui/InputFixtures.cs`; `InputReferenceTests` replays them at 60 Hz frames and compares event logs, states and snapshots. Probe one-off questions by writing ad-hoc fixture JSON and running the harness binary directly (much of Chromium's event timing was settled this way, e.g. an emptied focused field fires no selectionchange).
- Reference sources (Chromium 152 files, Skia, skrifa, read-fonts, cc traces) live in `~/.cache/stat-fick-build/reference-src/` (README there); always save new downloads there, never in a chat temp folder. Chromium source for exact behaviour: fetch from `https://chromium.googlesource.com/chromium/src/+/refs/tags/152.0.7977.89/<path>?format=TEXT` (base64) — the game's Electron is Chromium 152. Used for scrollbars, scroll curves, wheel, sticky, overflow. Wheel: 120 px/notch on Linux, 100 on Windows (not 53⅓).
- Performance tool: `Tools/Scripts/benchmark.sh [state ...]`. Profiling: `dotnet-trace` is in `~/.cache/stat-fick-build/tools` (`--format speedscope`; self time shows as CPU_TIME frames, use inclusive time).
- Auto scrollbars keep their state between layouts (Blink-like, history dependent); the incremental test seeds the fresh document's scrollbars through `LayoutContext.ScrollbarHint`.
- Browser comparison tools: `Tools/Scripts/make-layout-references.sh` and `make-game-references.sh` (the latter drives the old game through its screens).
- Probe scripts are in `~/.cache/stat-fick-build/claude-work/ui-probe`.
- Open question to ask at port time: UI symbols outside the decided icon set (⚙ ✦ ☾ ↓ ＋ ⚔ ♟ …) aren't in Lato or STIX.
- Build and test with `Tools/Scripts/test.sh`.
- GLFW enum gotcha: `GlfwFocusEvent.LostFocus`/`GainFocus`.

## Asking about design choices (user rule)

On the Stat Fick engine and port work (the port notes (engine/Docs/Notes.md)), ask the user about design choices the moment they come up, with the question tool: 2–4 concrete options, the recommended one first with its reason. Then record the answer with its date under "User decisions" in `Stat Fick Source/Docs/Engine-Plan.md` (and in the port notes (engine/Docs/Notes.md) when it's a lasting decision).

**Why:** the user's standing instruction is "If any questions arise, ask them instantly and do not improvise anything", and on 2026-10-25 they asked that new chats keep asking about design choices the way earlier ones did (e.g. LCD vs grayscale text, tooltip/select/validation looks, spellcheck, grabbing cursor, menu bar and OS dialogs, 3D vs sprites).

**How to apply:** ask about anything the player would see, hear or do differently, looks and styles, behaviour the old game or Chromium leaves open, Chromium-vs-alternative trade-offs, data formats, folder layout and scope. Don't ask what the code, Chromium's behaviour or the docs already answer. Ask before starting each later milestone (M6–M12) using the open-question list in Engine-Plan.md's "How to continue" section. Don't continue on an assumed answer.

## Blender (tool notes, 2026-10-16)

Blender 5.2 LTS is installed as the Flatpak `org.blender.Blender`. It has host filesystem access.

- **Run:** `flatpak run org.blender.Blender -b --python <ABSOLUTE path to script.py>`. A relative path fails, because the Flatpak starts in `/app`.
- **Gotcha:** `bpy.ops.object.transform_apply()` applies location and rotation by default too. Pass `location=False, rotation=False` when only the scale should be applied.
- **Rendering:** Cycles on CPU, 64 samples, 512×768 renders in about 2 seconds. View the PNG with Read.
- **Work folder:** the first test, `test-sword.py`, is in `~/.cache/stat-fick-build/claude-work/blender/`. Since the BRUTAL test, Blender work goes in `/run/media/system/M.2_2TB/Stat Fick/blender/` (the BRUTAL test notes below).
- **Gotcha:** `bpy.ops.object.shade_smooth()` acts on selected objects, not the active one. Use `obj.data.shade_smooth()`.
- **Reference renders (M6):** `Tools/Scripts/make-blender-references.sh` runs `-b --factory-startup` with absolute paths. The Flatpak has a private `/tmp`, so a chat's scratch folder in `/tmp` is invisible to it. In 5.2 `Material.use_nodes` and `World.use_nodes` are deprecated (always nodes); EEVEE's engine id is `BLENDER_EEVEE`; set `render.dither_intensity = 0` for exact colours.

Tested 2026-10-16 when the user asked whether Claude could make character and item models with it. The game itself draws characters and items as SVG (the old game's notes (old-game/Docs/Notes.md)), so 3D work needs an integration decision first.

## The BRUTAL test (history, 2026-10-16)

**Note:** everything described below was deleted at the user's request on 2026-10-16 to start the port (the port notes (engine/Docs/Notes.md)); the folder layout no longer exists. The API notes, Linux gotchas and art-style findings still apply.

On 2026-10-16 the user asked for a BRUTAL test and said to save everything for it in `/run/media/system/M.2_2TB/Stat Fick/`. That folder is separate from the game folder, `old-game`.

**Layout:**
- `tools/dotnet`: the .NET 10 SDK (10.0.402), installed locally with no root.
- `tools/env.sh`: source it first. It sets `DOTNET_ROOT`, `PATH`, `NUGET_PACKAGES=tools/nuget-packages`, `DOTNET_CLI_HOME` and telemetry opt-out, so nothing goes to `~`.
- `reference/BRUTAL`: a shallow clone of github.com/ahwootech/BRUTAL, used to read the API. It has no API docs, and building from source isn't supported yet.
- `blender/quasar-jet.py`: exports the sword to `BrutalTest/assets/quasar-jet.glb`.
- `BrutalTest/`: the C# app.
  - It uses NuGet `Ahwoo.BRUTAL.*` 2026.10.x: Core, Glfw, Vulkan, Vulkan.Abstractions, ShaderC, Gltf.
  - Files: `Program.cs` (GLFW window, Vulkan render pass with depth, swapchain rebuild, 2 frames in flight), `Shaders.cs` (GLSL compiled at runtime by ShaderC; PBR with studio reflections), `Model.cs` (`GltfLoader`, bakes node transforms and materials into vertices) and `Png.cs`.
  - `run.sh` opens the window; `--screenshot <png> [--frames N]` saves a frame and exits.
  - It runs at 60 fps on the RX 9070 XT (RADV).

**Linux gotchas:**
- BRUTAL loads Vulkan by the name `vulkan` (`libvulkan.so`, which only the dev package provides) and ShaderC as `shaderc`, while the package ships `libshaderc_shared.so`.
- Fixed with an `AssemblyLoadContext.Default.ResolvingUnmanagedDll` fallback at the top of `Program.cs`. A `SetDllImportResolver` can't be used, because BRUTAL sets its own.

**API notes:**
- Add global usings for `Brutal`, `Brutal.Pointers.Extensions` and `Brutal.Memory` (for `ByteSize` and `AsPointer`).
- Call `ShaderC.PInvoke.shaderc_compile_into_spv` with byte pointers; `RefString8` is awkward to construct.
- `gpu.FindMemoryType` replaces indexing `MemoryTypes` directly, which gives a CS0170 error.
- `GlfwWindow.Dispose()` destroys the window.

**Licence:** a shipped product needs the BRUTAL notice in its credits, the logo before gameplay (waived for non-profit) and `NOTICE.txt`.

Related: the Blender notes below, the old game's notes (old-game/Docs/Notes.md)

**Second test (2026-10-16): the lizardman combat sprite.** The user asked for a Blender lizardman with Keening, the Transformer and the Stormhide Cuirass, in the game's art style, as a 2D combat view, also running on BRUTAL.

`blender/lizardman/`:
- **`lizardman.py`:**
  - Shapes: skin modifier plus subsurf (`SKIN_SCALE` 1.32 offsets the shrink). Start each chain at an end point, or the hull turns into a box; add end points to round the caps.
  - Placement: details sit on surfaces via ray-casts, cast from outside inward.
  - Shading: EEVEE with Shader-to-RGB cel shading in the game's exact SVG palette, screen-space hatching in shadow, Standard view transform and no light shadows.
  - Lines: Freestyle, an outline of 22 px plus inner lines of 8 px, ink `#130e0b`.
  - Output: renders `render-main.png` and a glow pass, and saves `lizardman.blend`.
- **`compose.py`:** puts the glow behind the sprite and screens it over, halves the size, and writes `lizardman-sprite.png` and `lizardman-glow.png`.
- **`game-stage.png`:** the game's town battle scenery, captured by `capture-stage.js` in the test client.

`BrutalCombat/`:
- **`prepare-assets.py`:** builds `assets/` and `combat-preview.png`, with the player at the game's spot: feet at 71.5% of the stage height, body at 29.5% of its width, 40% of the stage tall.
- **`Program.cs`:** textured quads, sRGB blending through UNORM like a browser, CPU mipmaps, premultiplied alpha, and alpha 0 meaning additive. He breathes, the glow pulses, Space lunges. `--screenshot`/`--time`/`--lunge` exist for tests.

**Game inconsistency:** the battle sprite (`sprite-equipment.js` `MASTERWORK_SHIELDS.transformer`) still draws the Transformer as the old round copper shield, while the forged item and its icon are a kite shield. The user was told; nothing has been changed.

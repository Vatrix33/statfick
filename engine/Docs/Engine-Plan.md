# Statventure engine plan

Statventure is the engine for the BRUTAL port of Stat Fick. It's built on BRUTAL (C#, Vulkan 1.3).
The engine is finished, fully tested, before the game is ported onto it.

## How to continue (read this first)

**Current task (2026-10-30):** M6, the 3D renderer (the user's go 2026-10-30; decisions under "Decisions for
M6"). Design doc, step plan and status: `Docs/Renderer-3D.md`. Step 1 (core) is done; next is step 2 (glTF import). The project is one git repo (`../`, see `../CLAUDE.md`),
on the private GitHub repo `github.com/Vatrix33/statfick`.
Before each later milestone (M7 on) starts, first ask its questions under "Open questions for later milestones"
below (all at once, with the question tool), record the answers, and only then plan and write code.

**Design docs by area** (read the one for the area you work on): `Docs/Compositing-Port.md` (Blink
compositing, LCD vs grayscale text, inline SVG transforms), `Docs/Scripting.md` (event loop, frame
callbacks, matchMedia, ResizeObserver, Web Animations). The rest of M0–M5 is described in Progress below.
A new area (M6 on) gets its own design doc in `Docs/`.

**Method.** Everything the old game shows behaves as Chromium 152 (the game's Electron) behaves:
1. Get ground truth first: capture references with the harnesses (`Tools/Scripts/make-*-references.sh`;
   Electron apps that `Tools/Scripts/chromium.sh` runs in Chromium 152 (Electron 44.5.0 from npm, the
   old game's): `Tools/InputReference` (real input), `Tools/PaintReference` (screenshots, any scale),
   `Tools/GameReference` (the old game's screens), `Tools/ScriptReference` (script scenarios), the old
   game's test client `../old-game/test-client`, cc traces) and read the Chromium source
   (`~/.cache/stat-fick-build/reference-src/`, README there; fetch missing files into that folder; in
   a cloud session the folder is empty: fetch what you need from googlesource, which the environment
   must allow). Captures run on Linux (the PC, or the cloud under Xvfb); the 1.65 ones need X11.
   To capture one new input fixture without replaying all of them: filter
   `Tests/Statventure.Tests/Reference/input-fixtures.json` down to it in a scratch file, run
   `Tools/Scripts/chromium.sh Tools/InputReference --shots=<dir> --fixtures=<file> --out=<file> --fonts=../old-game/objects/fonts`
   (for 1.65 add `--ozone-platform=x11 --scale=1.65` instead of `--shots`), then merge its key into
   `input-results.json` (or `scaled/input-results@1.65.json`). Back up a results file before merging.
2. Port the rule from the source, don't fit numbers until a test passes. When a result differs, find the
   rule that explains it (dump the engine's intermediate data and compare). Check the capture setup
   before blaming the engine (see the capture gotchas under Zoom in Progress).
3. Write a test against the reference; `Tools/Scripts/test.sh` must pass in full before a step is done.
   Report failures as they are.

**Keep the notes current.** After each finished step: this file's Progress section, the design doc of
the area (e.g. `Docs/Compositing-Port.md`), and `Docs/Notes.md` for history and gotchas (on the user's PC
also Claude's memory file `stat-fick-port.md`; cloud sessions only see the repo). Write the state
down before a long session's context runs out, so the next chat can pick up from the notes alone.

**Stop at the end of each milestone.** When a milestone's work is done and its tests pass: tick it in
Milestones, update the notes, report to the user what was done, and stop. Don't start the next
milestone until the user says to.

**Ask the user about design choices — at once, never improvise.** The user's standing rule: "If any
questions arise, ask them instantly and do not improvise anything."
- Ask about: anything the player would see, hear or do differently; looks and styles; behaviour the
  old game or Chromium doesn't define; trade-offs between matching Chromium and something else; data
  formats, file and folder layout; scope (cutting, adding or deferring a feature); anything in the
  open questions below.
- Don't ask about what has a clear answer in the code, in Chromium's behaviour or in this file.
- How: the question tool with 2–4 concrete options, the recommended one first with its reason, as soon
  as the question comes up — don't pick a default and carry on, don't save questions for the end.
  Record each answer with its date under "User decisions" (or the milestone's decisions).
- Examples of questions asked so far: LCD vs grayscale text, how tooltips, select lists and validation
  bubbles look, spellchecking, the grabbing cursor, menu bar and OS dialogs, 3D models vs sprites.

**Open questions for later milestones** (ask each before starting that milestone; the answers decide
the design and the tests):
- **M6 3D renderer:** answered 2026-10-30 (see "Decisions for M6" below).
- **M7 Animation:** which animation set each character, enemy and boss needs (full sets were decided
  on 2026-10-16; which clips: idle, attacks, casts, hits, deaths, walks…)? In-place or root motion? Who approves the first
  model before the rest are made?
- **M8 Audio:** must the C# synthesizer match the old JavaScript one sample for sample (offline renders
  compared) or by ear? Are sound effects synthesized as now? Output sample rate and latency?
- **M9 Assets and data:** pack layout (which data in which pack), compression, mod or loose-folder
  overrides, where saves and settings live on Linux and Windows, settings file format, JSON import and
  export of characters?
- **M10 Scene and sequencer:** how cutscenes are authored (editor timeline, code, Blender export), and
  whether they can be skipped?
- **M11 Editor:** what it must edit (items, enemies, spells, shops, map…), separate app or in-game dev
  mode (the old game has one)?
- **M12 Packaging:** shipped folder layout and executable name, self-contained .NET, installer or
  archive per platform, icon, versioning, updates, crash log location?
- **At port time:** UI symbols outside Lato and STIX (⚙ ✦ ☾ ↓ ＋ ⚔ ♟ …); how the inline SVG art and its
  transforms map to the Blender remakes; the grabbing cursor's design; the level of detail of the item
  models.

## Folders

| Path | Contents |
| --- | --- |
| The repo (on the PC: `/run/media/system/M.2_2TB/Stat Fick Repo/`; private on GitHub) | `engine/` (this tree: engine, port, tests, tools, asset sources) and `old-game/` (the JavaScript game, the port's reference). `../CLAUDE.md` covers the machines it's worked on. |
| `$STATVENTURE_GAME` (on the PC `/run/media/system/M.2_2TB/Stat Fick/`, set in the git-ignored `Tools/Scripts/env.local.sh`; elsewhere `runtime/` in the repo) | Only what the game needs to run (the build output). |
| `~/.cache/stat-fick-build/` (Windows: `%LOCALAPPDATA%\stat-fick-build`) | The .NET SDK (unless the system has one), the NuGet cache, Electron 44.5.0 for the harnesses (`electron-44.5.0/`), and on the PC the BRUTAL source for reference. All re-downloadable. |
| `~/.cache/stat-fick-build/reference-src/` | Upstream sources the engine ports from (Chromium 152, Skia, skrifa, read-fonts) and the game's cc traces; README inside. Only on the PC, not in the repo (user decision 2026-10-30). Re-downloadable. |

Source tree:

| Folder | Contents |
| --- | --- |
| `Engine/Statventure/` | The engine library (one assembly; namespaces `Statventure.*`). |
| `Tests/Statventure.Tests/` | Unit tests and headless GPU tests with golden images. |
| `Samples/Statventure.Sandbox/` | A visual test bed. |
| `Tools/` | Build scripts, the shader compiler, the packer and later the data editor. |
| `Assets/` | Asset sources: Blender files, fonts, shaders, stylesheets. |
| `Docs/` | Plans and notes. |

## User decisions (2026-10-16)

- **Characters, enemies, items:** real-time 3D models made in Blender, drawn with cel shading and ink lines in the game's art style. They get full animation sets.
- **Battles:** the current fixed side view (orthographic) on a real-time 3D stage built in Blender.
- **Map:** real-time 3D terrain seen from above. Banners and cutscenes are remade in Blender as well.
- **UI:** a custom, responsive game UI.
  - CSS-like stylesheets compiled into the packs.
  - Screens are built in C#.
  - Text uses the game's fonts (Lato, STIX Two Text): FreeType draws the glyphs and HarfBuzzSharp shapes them.
- **Music:** the live synthesizer is ported to C#. Audio goes through OpenAL Soft.
- **Data:**
  - Classes stay `.json`.
  - Everything else is binary `.pak` files, created and edited with an editor tool.
  - Saves start fresh.
- **Platforms:** Linux and Windows.

**Decisions for M5 (2026-10-17):**
- `title` tooltips and the open list of a `<select>` are game-styled (like the leather combat tooltips; a parchment list in the game font). The engine draws them as styleable elements with a plain default style; the game's stylesheet styles them at port time.
- No native menu bar and no OS dialogs: Save/Load Character, Open game folder, Quit, Reload, Zoom, Fullscreen, About and file picking all become in-game UI.
- Form validation (2026-10-18): the engine checks fields as Chromium does (required, min/max/step, maxlength), blocks the submit, focuses the field, and shows Chromium's message in a game-styled bubble (a styleable element, like the tooltips).
- No spellchecking in text fields (2026-10-18).
- The `grabbing` cursor (map panning) is a game-styled cursor image (2026-10-18); the engine supports image cursors with hotspots.
- Inline SVG (2026-10-25): port CSS transforms for SVG children: the `transform` attribute as a presentation attribute of CSS `transform`, `transform-origin`/`transform-box` and transform animations on SVG elements, and Blink's SVG visual rects (layer bounds). Fill, stroke, opacity and filters stay attribute-only (the art becomes Blender renders); full Blink SVG styling was not chosen.
- Game code scheduling (2026-10-25): the engine gets Chromium's event loop — SetTimeout/SetInterval, QueueMicrotask, RequestAnimationFrame, events, ResizeObserver and matchMedia listeners in Chromium's task, microtask and frame order — and C# async/await runs on it (continuations as microtasks, as JS promises; an element animation's Finished is a Task), so the port can mirror the old JavaScript closely. (Chosen over only the four frame APIs, or frame-counted awaits.)
- Web Animations (2026-10-30): port the core of `element.animate`: keyframes with offsets and per-keyframe easing; duration, delay, endDelay, iterations, direction, fill, easing; the animation object with Finished/Ready tasks, Cancel/Finish/Pause/Play, PlayState, CurrentTime, StartTime, finish and cancel events; composited after CSS animations as in Blink; each part tested against Chromium. (Not the game's subset only, not the full API: no KeyframeEffect objects, playbackRate, commitStyles, replace, composite modes or getAnimations.)
- Keyframes in C# (2026-10-30): dictionaries of CSS text with typed options, e.g. `el.Animate([new Keyframe { ["transform"] = "translateY(0)", ["opacity"] = "1" }, new Keyframe { Offset = 0.3, ["transform"] = "translateY(-6%)" }], new AnimateOptions { Duration = 620, Easing = "ease-in", Fill = FillMode.None })`; times in ms as SetTimeout. (Not @keyframes text, not anonymous objects.)
- Path antialiasing (2026-10-25): keep the canvas's own path rasterizer (quarter-pixel cached positions, polygon-flattened curves, its own stroker). Path edges may differ from Skia's by up to ~60 levels; it's a known difference, and SVG paint fixtures compare at a tolerance that covers edge antialiasing. Porting Skia's analytic AA and stroker for paths was not chosen.

**Decisions for M6 (2026-10-30):**
- Reference for the look: Blender renders of the same test models (cel shading in the game's palette, Freestyle ink lines, as in the lizardman test); the engine's renders are compared with them like the UI with Chromium: shading bands and colours closely, lines with a tolerance.
- Ink lines (outlines and inner edges) have a fixed width on screen, scaled with the display's pixel density, whatever the model's size or zoom.
- Shadows: real-time cast shadows on the battle stage and the map, drawn in the art style (shadowed areas take the darker palette tone and hatching).
- Map camera: orthographic, straight down, as the current map (labels, markers and travel lines map over 1:1; zoom 1–10× of fit); the 3D terrain gives relief, light and shadows.
- Anti-aliasing: MSAA 4×.
- Target: Vulkan 1.3 GPUs from about 2016 on (GTX 1050, RX 470, recent Intel integrated) at 60 fps in 1080p.
- Models come out of Blender as glTF 2.0 binary (.glb); M9 converts them into the packs.

**Decisions for the move to GitHub (2026-10-30):**
- One private GitHub repo with two folders: `engine/` (the engine and port, formerly `Stat Fick Source/`) and `old-game/` (the JavaScript game, formerly `Stats/`).
- On this PC both move into one repo folder, `/run/media/system/M.2_2TB/Stat Fick Repo/`, which becomes the only working copy (scripts, docs and notes follow the new paths).
- Remote work: Claude Code cloud sessions (also from the phone app) and a Windows PC. The cloud gets a setup script (no GPU: software Vulkan). Windows must build, run and test (PowerShell scripts, test fonts from the repo); Chromium reference captures stay Linux/cloud only.
- The project notes kept in Claude's local memory move into the repo docs (cloud sessions don't see that memory). The reference source cache stays out (re-downloadable).
- Git workflow: cloud sessions push their own branch and open a pull request that the user merges on GitHub; sessions on the PC commit to `main` and push. The repo goes into the user's existing, empty private GitHub repo `github.com/Vatrix33/statfick`; commits carry `meistervatrix@gmail.com`; this PC logs in with the GitHub CLI (`gh`).
- Downloads (2026-10-30): everything on GitHub stays private. The old game goes up as a release in the private repo (Linux and Windows zips: the executable with the game files it reads); for others, the user shares the same zips through another site with a secret link.

## Progress

- **M0 is done** (2026-10-16):
  - `Statventure.slnx`, `Directory.Build.props`.
  - `Tools/Scripts/env.sh`, `build.sh` (shaders, then the solution), `test.sh` and `accept-golden.sh`.
  - `Tools/ShaderCompiler`: `Engine/Statventure/Shaders/Source/*.vert|frag|comp` become `Shaders/Bin/*.spv`, embedded. `*.glsl` files are includes.
  - `Core/Log`, `Core/NativeLibraries`, xUnit tests.
- **M1 is done** (2026-10-16):
  - **Platform:** `Platform/Window` (GLFW; logical units versus pixels; cursors, clipboard, fullscreen) and `Platform/Input` (an event queue plus state).
  - **Device:** `Graphics/GpuDevice`.
    - Vulkan 1.3: dynamic rendering, sync2, descriptor indexing, buffer device address, a timeline semaphore.
    - VMA memory.
    - Deferred release scopes: anything released while recording waits for that recording's submission.
    - `Immediate`, and frames in flight with a per-frame upload buffer.
  - **Resources:**
    - `GpuBuffer`: Device, Upload or Readback memory, all with device addresses.
    - `GpuImage`: tracks its layout; upload, mipmaps by blitting, readback.
  - **Pipelines and textures:** `Bindless` (16384 textures, 6 samplers, white texture at index 0), `ShaderLibrary` and `PipelineCache` (one shared layout: bindless set 0 plus 128 bytes of push constants; blend modes; vertices pulled from buffers).
  - **Drawing and presenting:** `CommandList` (barriers, dynamic rendering, copies) and `Swapchain` (UNORM, rebuilds itself).
  - **Loop:** `Core/Application` (main loop, minimize handling, frame capture to PNG).
  - **Images:** `Imaging/ImageFile` (stb load, PNG save).
  - **Sandbox:** `Samples/Statventure.Sandbox` (`--capture file.png`).
  - **Tests (13):** a headless GPU fixture; golden images in `Tests/Statventure.Tests/Golden` (renders that don't match go to `Actual/` for review); a real window and swapchain test.
  - **Display scale:** the engine follows the scale GLFW reports. It reported 1 for this display at first; since 2026-10-23 it reports 1.65 (KDE Wayland, fractional scaling).
- **M2 is done** (2026-10-16), in `Canvas/`. 36 tests.
  - **Canvas:** records in logical units at any scale and renders through one instanced pipeline (`canvas.vert`/`.frag`).
    - Rounded boxes: per-corner elliptical radii, per-side border widths, dashes as Chromium draws them (2w dash, w gap, spread evenly).
    - Linear, radial and repeating gradients.
    - Outer and inset box shadows (σ = blur / 2).
    - Images, coverage (glyphs, paths), rounded clips, 2D transforms, per-draw colour filters, multiply and screen blending.
    - Layers: opacity, the CSS filter chain (blur, drop-shadow with σ = value, colour matrices) and backdrop blur (mirrored edges).
  - **Paths:** `Path` (SVG path-data parser, arcs, flattening to 0.1 px), `PathRasterizer` (exact area coverage, nonzero/even-odd, stroker with joins and caps), and a path cache in `TextureAtlas` (R8 shelf-packed pages, which the glyphs will share).
  - **Browser references:** `Tests/.../Canvas/ReferenceScenes.cs` defines scenes in CSS/SVG plus canvas calls. `Tools/Scripts/make-css-references.sh` renders them in the old game's test client into `Golden/css-*.png`. All five scenes match within tolerance 12 / 1%.
  - **Review tools:** `Tools/Scripts/diff-images.py` shows reference, render and difference; `STATVENTURE_KEEP_RENDERS=1` keeps renders.
  - **Performance:** 20,000 primitives record in 4 ms; the whole frame takes 6 ms.
- **M3 is done** (2026-10-16), in `Text/` plus `Canvas/CanvasText.cs`. 42 tests.
  - **FreeType:** own bindings (`Text/FreeType.cs`, FreeType 2.14.3 layouts; `CLong` for C long; the glyph slot has `glyph_index` after `next`).
  - **Fonts:** `FontFace` (FreeType plus HarfBuzzSharp) and `FontLibrary` (CSS font matching, synthetic bold when 600+ is asked of a ≤500 face, synthetic oblique, per-character fallback families).
  - **Rasterizing:** `GlyphRasterizer` follows the system's hinting; fake bold is ppem/24, fake italic a 1/4 shear; quarter-pixel offsets.
  - **System settings:** `FontRenderSettings.System` reads fontconfig through `FcFontMatch` (here: RGB subpixel, slight hinting, default LCD filter) or Windows font smoothing.
  - **Layout:** `TextLayout` handles white-space modes, text-transform, HarfBuzz shaping with fractional advances (as Blink), and wrapping at spaces and dashes with hanging spaces. It also does `overflow-wrap: anywhere`, Blink line boxes (rounded ascent, descent and gap; top half-leading floored), alignment, ellipsis that keeps the preceding space, line clamp, and min/max-content widths.
  - **Canvas text:** glyphs are cached in the atlas. LCD subpixel text uses dual-source blending (`canvas_lcd.frag`, `BlendMode.DualSourceLcd`), only straight on the target with no layer, filter, blend or rotation; otherwise grayscale.
  - **Mask gamma:** Skia's mask-gamma formula with fitted gamma 1.4 / contrast 0.64. Glyphs are placed at quarter pixels (as the game's Chromium does).
  - **Browser reference:** the css-text scene matches Chromium (0.28% of pixels beyond 40 levels).
  - **Not covered yet:** rotated text draws axis-aligned glyph quads; carets and hit testing come with M5.
- **M4 is done** (2026-10-17). Styles, the box tree, layout, painting, transitions and animations, and incremental updates. 129 tests.
  - **CSS:** `UI/Css` (tokenizer, parser, selectors and matching) and `UI/Style`:
    - `Properties` (+ `.Shorthands`): every property the game's stylesheet uses, with parsing, shorthands, aliases and logical properties. The game's sheet has no unknown properties (a test checks this).
    - `Stylesheet` prepares declarations once: shorthands expanded, invalid values dropped, var() kept for later.
    - `StyleEngine`: a rule index per media environment; the cascade (UA, author, style attribute, !important, presentational hints); custom properties with cycles; var() substitution, also in shorthands; CSS-wide keywords; blockification; pseudo-elements (::before, ::after, ::placeholder, ::selection); keyframes.
    - `UserAgentStyles`: the parts of Chromium's html.css the UI uses.
  - **DOM:** `UI/Dom/UiDocument` (invalidation and the update: styles, boxes, layout, paint) and `Markup` (an HTML fragment parser and serializer, as innerHTML). Elements have sibling links and version counters (attributes; anything in the subtree) for caches.
  - **Layout:** `UI/Layout`:
    - `BoxBuilder` builds the box tree: anonymous blocks and items, ::before/::after, list markers (outside and inside), closed details, containing blocks.
    - `LayoutEngine`: block flow with margin collapsing, widths and heights (box-sizing, min/max, aspect-ratio), scroll containers and scrollbars (15 px, thin 10 px, gutter), viewport scrollbars with overflow propagation, positioned boxes (static positions aligned in flex and grid), relative offsets, intrinsic sizes, centred button content.
    - `InlineLayout`: white space, shaping (ligatures off under letter-spacing, tabular-nums), break opportunities, inline box fragments, atomic inlines, vertical-align (sub/super in LayoutUnits), text-align (overflowing lines start-align), justify, ellipsis (keeps the space before it), line clamp (hidden lines still laid out), hanging preserved spaces, br and newline fragments.
    - `FlexLayout` (CSS Flexbox §9, automatic minimum sizes from content, wrap-reverse) and `GridLayout` (placement, auto-fill/fit, named lines and areas, the track sizing algorithm with definite, indefinite and min/max-content modes).
    - `ReplacedContent`: images, canvases, inline svg (viewBox ratio), and form controls sized as Chromium does (text fields fitted for Lato and STIX at 10–30 px, selects measured word by word, checkbox/radio/range baselines).
  - **Font rounding:** ascent, descent and line gap round halves up, as Skia does (it was banker's rounding before).
  - **Browser comparisons:**
    - `Tests/.../Ui/LayoutFixtures.cs` has 36 fixture pages. `Tools/Scripts/make-layout-references.sh` measures them in the old game's Chromium (152) into `Golden/layout-reference.json`. Every element box and text fragment matches within 0.5 px.
    - `Tools/Scripts/make-game-references.sh` drives the old game through 11 screens (sheet, seven modals, shop, map, combat; transforms and animations frozen). It stores the markup and the measured boxes in `Golden/game-ui.json.gz`, and the stylesheet in `Data/Game/styles.css`. All 11 screens match box for box.
    - Symbols missing from Lato and STIX come from system fonts in Chromium. The test emulates that: fontconfig, plus a table of the fonts Chromium picked for the game's 20 symbols.
  - **LayoutUnits:** Blink lays out in 1/64 px fixed point. The engine truncates as Blink does: resolved lengths, number line-heights, flexed sizes, grid fr and distributions, and text item widths rounded up. Pixel snapping then rounds box edges the same way. At 0.02 px tolerance, 37 of 49 comparisons match exactly.
  - **Painting:** `UI/Paint/UiPainter` (+ `.Content`, `.Controls`, `.Svg`):
    - Appendix E paint order with stacking contexts (z-index, flex/grid item z-index), positioned layers and atomic inlines as pseudo-contexts.
    - Scroll-container clips (also for out-of-order layers), transforms (transform, translate, rotate, scale), opacity/filter/blend layers, backdrop blur, clip-path inset and clip rect.
    - Backgrounds: colours, gradients (stop resolution, radial ending shapes), url and SVG images, size/position (3/4-value syntax)/repeat, background-attachment fixed, root/body propagation to the canvas.
    - Borders with per-side colours and styles (new canvas `DrawBox(BorderSides)`; sides meet on the corner diagonals), box shadows, outlines that follow radii.
    - Text: glyph runs (new `Canvas.DrawGlyphs`), text-shadow (blurred via layers), decorations propagated from ancestors, hidden clamp lines.
    - Images with object-fit, inline SVG and SVG images: a vector subset with gradients, transforms, use, viewBox and preserveAspectRatio. SVG images whose rect uses feTurbulence (+ feColorMatrix) render through `Imaging/Turbulence` (the spec's reference algorithm, linearRGB to sRGB).
    - Form controls (checkbox, radio, range, progress, text fields with value/placeholder and the 15 px number spinner, select with chevron), outside/inside list markers and disclosure triangles, classic scrollbars with scrollbar-color.
  - **Screenshot comparisons:** `make-game-references.sh` also stores Chromium's screenshot of every state (`Golden/game-*.png`). `GamePaintTests` paints each state and compares (tolerance 24, at most 3% of pixels; inline SVG art is masked because Blender renders replace it). All 11 pass. Beyond 12 levels: 0.1–1.3% of pixels, mostly glyph edges.
  - **Known approximations:**
    - At fractional font sizes (e.g. 18.72 px), Chromium's text is up to 0.2% narrower. The cause isn't found yet: integer and quarter sizes match exactly, and FreeType's linear and hinted advances both disagree. The game uses mostly integer sizes.
    - Variable fonts' weight axes aren't applied (only system symbol fallbacks are variable).
    - Inside disc/circle/square markers are text-based (the game has none).
    - The inside disclosure marker's width is fitted (1.0593em).
    - Content of closed details isn't laid out. Chromium lays it out invisibly, so the tests skip it.
    - A block inside an inline box makes the inline a block; the game has none, and `UiDocument.LayoutDiagnostics` reports any.
  - **Animations:** `Style/AnimationEngine` and `Style/Interpolation`.
    - @keyframes with per-keyframe easing; iteration count, direction, fill mode, delay (also negative), play state.
    - Transitions start from the value shown and stop when no longer listed.
    - Interpolation: premultiplied colours; transform lists pairwise or through 2D matrix decomposition; filter and shadow lists; clip-path inset; visibility.
    - Paint-only values are written into the shown style in place; layout values relayout the element; inherited values restyle the children.
    - `AnimationReferenceTests`: 6 fixtures sampled at 10 times match Chromium's getComputedStyle (`make-animation-references.sh`).
  - **Incremental updates:** an update redoes only what a change needs.
    - **Invalidation sets** (`Style/RuleFeatures`): every class, id, attribute and state the selectors name, with how far a change reaches (the element, its descendants, its following siblings). Changes to what :has() arguments look at restyle the ancestors. Attributes decide :checked, :disabled, :placeholder-shown and :any-link. A class, attribute or state no selector names restyles nothing.
    - **Matching:** rules are bucketed by id, class, tag, attribute, root, and the alternatives of :is()/:where(). A counting Bloom filter of the ancestors (as WebKit's) skips selectors whose ancestors can't be there. Structural pseudo-classes and attribute words don't allocate.
    - **Shared styles:** elements with the same matched rules, style attribute and parent style share one computed style. A recomputed style identical to the old one keeps the old instance (so the children's shared styles still apply). Computed styles are never changed after computing; animated elements show a private copy.
    - **Pseudo-elements:** ::before/::after are computed only when a `content` value wins; ::placeholder and ::selection when first asked for.
    - **Style differences** (`Style/StyleDifference`): compiled structural comparisons classify a new style as no change, paint only, layout, or box tree (display, position, content, markers, white-space, becoming a containing block).
    - **Boxes read their element's current style** (anonymous boxes and markers derive theirs again when it changes), so paint-only changes need no box work. Elements keep their boxes: a box-tree change rebuilds that element's boxes and has its parent collect its child boxes again; containing blocks are reassigned in one pass.
    - **Layout cache:** a box laid out again in the space of its last layout, with nothing changed in it, keeps that layout. Changes mark the box and its ancestors; intrinsic sizes and collected inline content stay until then. Flex and grid measure items through a separate cache of measurements, so measuring doesn't undo the final layout.
    - **Scrollbars** of overflow: auto scrollers stay from one layout to the next until the content fits without them, as in Blink (they no longer flip each layout). Positioned boxes are laid out inside their containing block's frame, before its overflow and scrollbars are measured; positioned boxes inside inline containing blocks are laid out too.
    - **Fonts:** glyph coverage per face and character fallbacks are cached; font lookups don't lock on hits.
    - **Painting:** SVG shapes, transforms, id maps and inherited paints are kept until the SVG changes. Layer images of a recording that is never rendered go back to the pool.
    - **Tests:** `IncrementalUpdateTests` changes every game screen at random (classes, text, inline styles, attributes, states, removed, copied and inserted elements) and compares each update with a fresh document of the same tree: every computed style and every box with its lines and items, exactly. `STATVENTURE_INCREMENTAL_ROUNDS` and `STATVENTURE_INCREMENTAL_SEED` widen the search (seeds 0–15 pass). `IncrementalWorkTests` checks the work done through `StyleEngine.StylesComputed` and `LayoutEngine.BoxesLaidOut`.
    - **Benchmark:** `Tools/Scripts/benchmark.sh [state ...]` (`Tools/UiBenchmark`) times the captured screens. Release, this machine:

      | | sheet | combat | shop |
      |---|---|---|---|
      | Cold first update (styles, boxes, layout) | 37 ms | 38 ms | 21 ms |
      | Restyle everything | 4.8 ms | 0.9 ms | 1.4 ms |
      | Relayout everything | 6.8 ms | 0.6 ms | 2.5 ms |
      | Class toggle, paint-only style, unused attribute | < 0.03 ms | < 0.01 ms | < 0.01 ms |
      | Text edit, padding change | 0.15 ms | 0.03 ms | 0.03 ms |
      | Paint and render a frame | 3.3 ms | 2.5 ms | 6.6 ms |

      Before this work, one attribute change took 20 ms on the sheet and a full restyle and layout 130–150 ms.
    - **Known cost:** a box that is measured and then laid out at another size (stretched flex and grid items) is laid out twice when something inside it changes.
  - **Open question for the port:** symbols such as ⚙ ✦ ☾ ↓ ＋ ⚔ ♟ in UI text aren't in Lato or STIX. The decision so far covers ♜ ♡ ◈ ▣ ◌ ◫ ◆ (vector icons). Ask about the rest when porting.
- **M5 is done** (2026-10-17 to 10-30). 496 tests.
  - **Hit testing** (`UiDocument.HitTest`): the painter's traversal records hit regions in paint order (Blink's 1×1 square test for boxes, pixel-snapped rects for text, rounded and overflow clips, opacity 0 still hit, pointer-events, visibility, SVG shapes). `GameHitTests` checks a 10 px grid of `elementFromPoint` on all 11 captured screens.
  - **Events** (`UiEvents`, `UiInput`): DOM dispatch (capture, target, bubble), Chromium's pointer and mouse order, boundary events, :hover and :active chains, clicks, double clicks, auxclick, contextmenu, pointer capture, focus and blur events, :focus-visible, Tab order, Enter and Space activation, fake mouse moves after visual changes.
  - **Scrolling** (`UiDocument.Scrolling`, `UiInput.Scrolling`), from Chromium 152's source (`cc/`, `ui/native_theme`, Blink `scroll/`):
    - Scrollbars are Fluent scrollbars: 15 px (thin 10 px), 18 px arrow buttons, a pill thumb at least 17 px long, `#FCFCFC`/`#8B8B8B`, hovered and pressed `#636363`, scrollbar-color with Chromium's contrast blending. Arrows are triangles filled without antialiasing. `ui-scrollbars` matches Chromium to the pixel except two thumb-corner pixels.
    - Scroll API: offsets clamp (also after layout), the root's are the document's, `ScrollTo`/`ScrollBy`/`ScrollIntoView` (block/inline alignment, scroll-margin, scroll-padding, every scroller out to the document), `ScrollMode` (auto follows scroll-behavior).
    - Animations as cc's: programmatic √distance frames on cubic-bezier(0.4, 0, 0, 1) up to 1.5 s, keyboard 0.15 s, wheel 6–12 frames ease-in-out, retargeted with the current speed, linear autoscroll.
    - Wheel: 500 ms latching, chaining only when a turn starts, overscroll-behavior, Shift turns it horizontal, Ctrl zooms (`ZoomWheel`), only the first event of a turn cancelable. The host converts notches: Chromium 152 scrolls 120 px per notch on Linux and 100 px (3 lines of 33⅓) on Windows.
    - Keyboard: arrows 40 px, Page Up/Down and Space 87.5% of the scrollport, Home/End, from the focused element (else the last pressed one) outwards; text fields keep their keys.
    - Scrollbars: thumb drags, Shift+click jumps, buttons and track scroll and repeat after 250 ms at 20 steps a second, snap-back on Windows only; the page sees mousedown/mouseup but no click; focus only moves to a focusable element.
    - `scroll` events once per frame, `scrollend` when input and animations stop.
    - position: sticky as Blink's StickyPositionScrollingConstraints (against the scroller's content box; the containing block less the sticky box's margins). `ui-sticky` and `ui-sticky-viewport` match Chromium.
    - Scrollable overflow follows Blink's ScrollableOverflowCalculator (end padding after in-flow margin boxes, not after negative-margin children or out-of-flow boxes).
    - CSSOM geometry: clientWidth/Height/Left/Top, scrollWidth/Height, offsetWidth/Height/Left/Top, offsetParent, getBoundingClientRect, getClientRects.
  - **Paint references** (`PaintFixtures`, `make-paint-references.sh`): fixtures screenshotted in the game's Chromium.
  - **Canvas:** box coverage now converts distances to pixels through the affine local coordinates (the distance's own derivatives were wrong on 1 px boxes). Pages get a white base background, as in a browser (`UiDocument.BaseBackground`).
  - **Forms and text editing (#27, done 2026-10-23):** activation, spin buttons, range, search cancel, selects with the game-styled popup, Blink's editing commands, undo, ICU word breaks, mouse and page selection. Painting is matched to Chromium screenshots (`InputPaintReferenceTests`, captured by the input harness):
    - Caret: Blink's caret rect (x = round(x − 0.5) inside the line and editor, as tall as the font; an empty editor's at its start, centred on the line).
    - Selections: Blink's fast selection painting (unselected glyphs, highlight, selected glyphs clipped to it), a space's width more when the line's end is selected (also for `<br>` and preserved newlines), inactive-window colours; decorated text takes Blink's highlight overlay path.
    - Focus rings (`outline: auto`, `UiPainter.Outlines.cs`): OutlinePainter's two strokes (2 px white outside, 2 px outline colour inside, 1 px over a border), corner radii (a themed control's own, author radii, else 3 px), around the union of the outline rects including block ink overflow (children, text, anchors' out-of-flow boxes), right-angle region paths rounded like SkCornerPathEffect.
    - Text decorations (`UiPainter.Decorations.cs`): applied decorations propagated in style (stopped by atomic inlines and out-of-flow boxes), decorating boxes (the line's or an inline box's font), thickness, `text-underline-offset`, `text-underline-position`, solid/double/dotted/dashed/wavy lines as DecorationLinePainter draws them, `text-decoration-skip-ink` from unhinted glyph outlines (`Text/GlyphOutlines.cs`) with Blink's CJK exceptions (a generated table), decoration shadows; fields and placeholders use the same painter. `ui-decorations` matches Chromium.
    - Text fields' theme border and background are drawn without antialiasing (as with Skia's default paint flags).
  - **Text fields scroll (2026-10-23):** a textarea is a scroll container (wrapped text sets its overflow, auto scrollbars); the caret is revealed only for user moves, typing and mouse selection, through every scroller out to the document (Blink's RevealSelection: ToEdgeIfNeeded, TopAlways after a page move); Page Up/Down follow ModifyWithPageGranularity (a one-line field leaves them to the page); up/down keep their x; inner-editor scrolls fire scroll/scrollend at the input; scroll and scrollend share Blink's per-frame queue; selectionchange runs after the frame's scroll events. `field-scroll` events, states and screenshots match Chromium.
  - **Resizers (2026-10-23):** `resize` (a textarea's `-internal-textarea-auto` is both ways) on scroll containers; Blink's corner rect and scroll corner (bars shortened by it), the diagonal grip lines drawn without antialiasing; dragging sets the inline width and height (at least the corner's size) with the resize cursors. Scrollbars paint with the box's background and its own outline after its content (Blink's order). The `resize` fixture matches Chromium.
  - **Animation and transition events (2026-10-23):** Blink's event delegates: each animation and transition keeps the phase its events last saw; the frame's timing update queues animationstart/iteration/end and transitionrun/start/end (transitions first, then each in composite order), a cancel queues its event at once (removed animation, transition replaced or no longer listed, display: none, removal, a pseudo-element going away); the queue is dispatched after the frame's scroll events. With it the transition update follows Blink: a running transition to another value is cancelled first, the before-change style samples the running transitions now, a reversal is shortened (CSS Transitions' reversing shortening factor), `transition: none` cancels all; finished animations stay matched by name (a restyle no longer restarts them) and an element shown again starts its animations. The `animation-events` fixture (events and elapsed times, to a tenth) matches Chromium; `UiAnimationTests` covers restyles, display: none and reversal.
  - **Title tooltips (#28, 2026-10-23, `UiInput.Tooltips.cs`):** Chromium's two halves ported from source (the browser draws them in a window of its own, which the harness can't capture): Blink's text under the pointer (nearest title, an SVG element's first `<title>`, else an invalid control's validation message unless its form is `novalidate`; passed on when the point or text changes, cleared first when the same text comes from another node, nothing over scrollbars) and views' TooltipController (shown 500 ms after the pointer rests, no hiding by time; a press hides it until the text under the pointer changes, a key press until the next text change; the wheel and leaving the window hide it; text cut at 2048 characters, trimmed, tabs as 8 spaces). `<sv-tooltip>` in the top layer, placed as TooltipAura does with the window as its screen (10 px right and 15 px below the pointer, max width min(800, (width + 1) / 2), shifted left, above the pointer when it doesn't fit). Select lists carry their options' and groups' titles (a page of their own). `UiTooltipTests`.
  - **Window host (#28, 2026-10-23, `UI/UiHost.cs`, `UI/UiHostInput.cs`, `Platform/SystemSettings.cs`):** runs a document in a GLFW window: key values from GLFW's char events (else named keys, the keypad's navigation keys without Num Lock, the layout's character with Shift/Caps Lock for letters), codes by US position, a modifier key held while it goes down, wheel notches as 120 px (Linux) or 100 px (Windows), focus and leave events; the cursor as a system shape or a cursor image (`cursor: url() x y, keyword` parsed as Blink does; the first loaded image ≤ 128 px, one over 32 px only where it fits in the viewport; scaled to the pixel density); the system clipboard; the caret blink interval and reduced motion as Chromium reads them (GTK's gtk-cursor-blink/-time and gtk-enable-animations via GNOME's settings or settings.ini; GetCaretBlinkTime and client-area animations on Windows); painting at the window's density. The sandbox is now a UI demo (`--capture` works through the swapchain). `UiHostInputTests`, `UiCursorTests`.
  - **Selector fix:** the ancestor filter required compounds before a sibling combinator to be ancestors (`#a:focus ~ #b > #c` never matched `#c` in styles); now only compounds followed by a descendant or child combinator count, as in Blink's SelectorFilter.
  - **Known differences left (not yet ported):** a non-auto outline on an inline box that wraps lines is drawn per line box (Blink unites them with ComplexOutlinePainter); `::selection`'s own text decorations aren't drawn; a list box's focused option ring; Blink drops the ellipsis of editable text holding the selection focus (TextOverflowClipWithSelection); rounded-corner antialiasing differs from Skia by a few pixels at small radii; text drag and drop isn't implemented; animation events of different elements in the same frame come in tree order (Blink's timeline sorts them by the elements' addresses, which follow creation); `transition-behavior: allow-discrete` isn't parsed. Host: GLFW doesn't say whether scrolling comes from a wheel or a touchpad (all are notches); the wait, help, cell, alias, copy, zoom and grab cursors show the arrow unless the page gives an image (the game uses none of them; its grabbing cursor will be an image); a non-letter key with Ctrl and Shift reports its unshifted character; AltGr is reported as Alt; SVG cursor images and .cur hotspots aren't supported.
  - **Zoom (#29 + #30), done 2026-10-24 (418 tests).** Measured 2026-10-23: painting the paint fixtures with the canvas scaled by 1.65 differs from Chromium at a 1.65 device scale in 0.1–3% of pixels (1× matches within 0.2%): Chromium lays the page out in device pixels (zoom-for-DSF), so 1 px borders stay one device pixel and text baselines and edges snap to device pixels. Plan (Blink's effective zoom; device scale and page zoom are one zoom):
    - Document: `DeviceScale` (the window's), `PageZoom` (Electron's View menu: level ±0.5, factor 1.2^level), `Zoom` = both. The viewport is given in window units (DIPs); layout works in device pixels (DIP × DeviceScale); the CSS viewport (media queries, vw/vh) is device px / Zoom.
    - Style: every length is its CSS px value × Zoom (CSSLengthResolver::ZoomedComputedPixels: px, absolute units, em/rem from specified sizes, viewport units); font sizes zoomed; border and outline widths floored after zooming (ClampLineWidth: (0, 1) → 1); images' natural sizes × Zoom; SVG content scaled.
    - Hard-coded sizes in layout and painting follow Blink: theme parts with the element's zoom, scrollbars (15 px, arrows, thumb) and scroll steps (40 px lines) with the device scale only (Chromium's scale_from_dip, WindowToViewportScalar), focus rings, caret.
    - DOM in CSS px: event coordinates, geometry (getBoundingClientRect, offset*, client*, scroll*) and scroll setters converted as Blink's AdjustForAbsoluteZoom does; input from the host in DIPs.
    - Painting at canvas scale 1 (device pixels).
    - References: `Tools/PaintReference` (DevTools device metrics override, `--scale=1.65`; `--mode=zoom` for page zoom) for paint fixtures; layout fixtures and the game screens at 1.65 too. Everything at zoom 1 must stay as it is.
    - **Done (2026-10-24):** document zoom API, zoomed style lengths, theme parts, scrollbars, DOM geometry in CSS px, whole-pixel scroll offsets, text metrics at zoom; the paint fixtures at a 1.65 device scale (references from X11 Chromium with `--force-device-scale-factor`, CPU raster) match in paint and layout. Text: Chromium 152 on Linux loads every web font with Fontations and Skia draws its glyphs from the outline, not with FreeType's renderer. The engine now does the same (`Text/Skia`: Skia's analytic anti-aliasing scan converter, its 4× LCD FIR filter and RGB565 masks, SkMaskGamma with Linux's contrast 0.2 / gamma 1.2, the CPU blitter's 5-bit LCD blend, SkGlyphPositionRoundingSpec placement); the fitted shader gamma is only left for FreeType's fake bold. Field, select and button text are painted from the unsnapped content box (Blink paints them at fractional positions).
    - **Hinting (2026-10-24):** at a device scale of 1 Chromium hints with Fontations: skrifa 0.44's autohinter in light mode for TrueType fonts (Skia forces it for glyf fonts at slight hinting). Ported as `Text/Autohint` (font access through FreeType's unscaled outlines, the GSUB coverage walk of `autohint_shaping`, style classes generated from skrifa's tables by `Tools/Autohint/generate.py`, outline topology, widths and blue zones, segments and edges, edge and point hinting). `TextHintingReferenceTests` compares glyph sheets (every letter, digit and sign the game writes, 9–28 px, Lato regular and bold) with Chromium at scale 1 (`make-scaled-paint-references.sh 1 1 text-fixtures.json`): every hinted outline matches. Advances now follow skrifa too (the hmtx advance through a 16.16 FreeType scale and FT_MulDiv, kerning linear), which moved glyphs that sat at quarter-pixel boundaries onto Chromium's quarter.
    - **Text differences left:** LCD blending: Skia's CPU blitter (the references) floors `(src − dst)·m >> 5`, the GPU rounds (one level); Blink sums glyph positions in another float order, so a glyph within a float ulp of a quarter pixel can land on the next (about one in 2,500); CFF (STIX) outlines and their light hinting come from FreeType rather than skrifa (the STIX glyph sheets match Chromium exactly at 1× and 1.65 all the same); text-decoration ink skipping of synthetic bold text takes FreeType's emboldened outline (Skia's path strike strokes it; no game text does this); system fallback fonts are drawn like web fonts (Chromium draws them with its FreeType backend, whose fake bold emboldens and widens advances); medium and full hinting (not this system's setting) use FreeType's autohinter where Chromium runs skrifa's TrueType interpreter for fonts with instructions.
    - **Done (2026-10-24, later):** input in window units (DIPs) scaled to device pixels for hit testing and scrolling, DOM event coordinates in CSS px (MouseEvent floored, PointerEvent fractional except click/auxclick/contextmenu, movement from truncated screen positions), wheel deltas over devicePixelRatio, 40 px × device scale line steps; caret width max(1, device scale); focus ring widths and offsets by Blink's zoom rules; tooltip, select popup and validation bubble placed in DIPs; the host's `SetZoom(window content scale, page zoom)` with the canvas in device pixels. Font sizes resolve from the unzoomed specified size (em/rem lengths are specified × zoom), number line heights round as Blink, fixed lengths truncate to LayoutUnits, line tops round before the baseline, decoration lines snap before their shift, inline max-content sums text items without float error (a 1/64 px short sum wrapped a `<summary>` at zoom 1.98). References at 1.65 and at 1.65 with page zoom 1.2: paint fixtures (`make-scaled-paint-references.sh SCALE [PAGE_ZOOM]`), layout fixtures (`make-scaled-layout-references.sh`), the 11 game screens (`Tools/GameReference`, `make-scaled-game-references.sh`), the input fixtures (`make-input-references.sh 1.65`: an X11 window at the scale instead of offscreen rendering, which ignores it). Input fixes the 1.65 input references showed: the document's programmatic and scroll-into-view scrolls compare unrounded offsets (Blink's RootFrameViewport never rounds: one that rounds back to where it is fires scrollend without scroll), while elements round first; a press inside a field's selection is decided by the position under it (FrameSelection::Contains, both ends included), not the painted highlight; a field's inner editor scrolls to rounded offsets within its pixel-snapped content (text plus caret width) and reports CSS px; resizing works in CSS px (box size, pointer and resizer over the zoom, LayoutUnit truncation) from floored pointer positions. Capture rules learnt: the window must be whole device pixels (capturePage scales the surface to ceil(DIPs × scale): 884 DIPs at 1.65 blurred every page-zoom golden), the page zoom is set before the fixture loads (a later zoom scales scroll offsets already set), frames are sized to whole device pixels; an X11 window out of sight gets a frame only every 2 s, so the scaled input harness runs with `--disable-frame-rate-limit`, and the window manager adds a few pixels to X11 windows, so each scaled input result records the page's viewport and the replay uses it.
  - **LCD text and compositing (2026-10-24 to 10-25, done: all 22 game screens exact):** opacity layers start from their backdrop and keep LCD text unless the frame has a filter or blend layer; isolation groups around blending elements turn their text grayscale (`TextHintingReferenceTests` text-opacity fixtures). cc traces of the game (`lcd_text_disallowed_reason` per layer, from `contentTracing` with disabled-by-default-cc.debug) show the rest: Chromium composites the fixed side menu, modal layers, their scrolling panels and what overlaps them into separate cc layers, and text in a layer that isn't opaque (contents_opaque, or text known to be on an opaque background) is grayscale. **User decision (2026-10-24): match Chromium exactly** — port Blink's compositing reasons, layerization (overlap, merging) and opaque-area tracking, tested against per-screen layer lists from traces. Done: property trees, paint chunks, layerizer and per-chunk LCD verdicts drive the canvas (two paint passes); `CompositingReferenceTests` compares every game screen's layers with Chromium's. **Details, rules, file map, debugging and the SVG differences left: `Docs/Compositing-Port.md`.**
  - **Shop compositing and inline SVG (2026-10-25, 467 tests):** all 22 game screens now composite like Chromium. The shop's references were inconsistent (random stock; `game-capture.js` now seeds it). Ported Blink's svg.css (inline svg clips its overflow; SVG elements transform about 0 0), replaced elements never being scroll containers, flex/grid automatic minimums by the axis's overflow value, and (user decision) CSS transforms for SVG children: the `transform` attribute as a presentation attribute, `transform-box`, TransformHelper's reference boxes and zoom, CSS `matrix()` translations zoomed, Blink's SVG bounding boxes, one display item per shape with Blink's visual rect, SVG elements' client rects. New `svg-transforms` paint fixture (1×, 1.65, 1.65×1.2; geometry exact through element boxes, edges at tolerance 60 by the path antialiasing decision) and `SvgTransformTests`. Details: `Docs/Compositing-Port.md`.
  - **Synthetic bold and font sizes (2026-10-25, 472 tests):** fake bold is Skia's: the glyph outline stroked and filled (SkScalerContextRec::useStrokeForFakeBold: width text size × 1/24 at 9 px to 1/32 at 36 px, miter joins, limit 4) by a port of SkStroke/SkPathStroker with its joiners, cappers, quad approximation of curves, rect special case and stroke-and-fill direction (`Text/Skia/SkiaStroke.cs`, `SkiaGeometry.cs`, `SkiaPathBuilder.cs`); conics reach the scan converter as quads (SkAutoConicToQuads, tolerance 0.25). FreeType no longer draws any text (the fitted shader text gamma is gone). Glyph images are now made at Blink's font cache size, truncated to 0.01 px (FontDescription::EffectiveFontSize: 12 px at 1.65 is 19.79), as advances already were. New glyph sheets: STIX regular and synthetic bold at 1× and 1.65, Lato at 1.65 (`TextHintingReferenceTests`, tolerance 1); all match Chromium. The PaintReference harness now loads the game's STIX faces in paint mode too (the system's STIX Two Text has a real bold face and stood in before).
  - **Script APIs, part 1 (2026-10-30, 481 tests, `Docs/Scripting.md`):** the document's event loop (`UiEventLoop`: timers with DOMTimer's nesting clamp, tasks, microtask checkpoints after tasks and after every callback the engine calls, C# async/await continuations as microtasks), frame callbacks, matchMedia lists with change events, the window's resize event, ResizeObserver (Blink's depth rounds, loop-limit error event, entries with LayoutUnit sizes and the snapped device pixel box), in Blink's frame order. `UiDocument.Update` now runs the frame's steps once per frame (a nested Update only does style and layout; before, a hover restyle could run the frame steps twice). New harness `Tools/ScriptReference` (`make-script-references.sh`): scenarios in JavaScript and their C# twins (`ScriptFixtures`) log callbacks tagged with frames; all 8 match Chromium.
  - **Web Animations (2026-10-30, 491 tests, `Docs/Scripting.md`):** `element.Animate` with Blink's Animation procedures (hold/start time, pending play and pause committed after the frame, finish/cancel, seeking, the finished state per frame with its microtask, Ready/Finished tasks reset as promise properties, finish and cancel events), Timing::CalculateTimings, KeyframeEffectModel's offsets, neutral keyframes and interpolation records, composited after CSS animations in creation order; Blink's animation clock between frames; gfx's CubicBezier solver (unclamped, extrapolating by the end gradients) and steps with the before flag. `GetComputedStyle()` samples animations at the frame's time in frame callbacks. Fixtures `animate-playback`, `animate-sampling` (including the game's battle keyframes), `animate-control`, `animate-composite` match Chromium; `ScriptApiTests` covers C#-only behaviour. **#31 is done.**
  - **inert, hover and focus fixup (2026-10-30, 493 tests):** the game's compact menu is `inert`. Ported Chromium 152's rule: the UA sheet's `[inert] { interactivity: inert !important }`, `interactivity` with Blink's IsCSSInert (inherited, never undone below), used pointer-events and user-select none, not focusable, an inert field's selection has no caret position (getSelection is empty); the attribute only restyles. Two general rules came with the reference: Blink recomputes :hover under a still pointer only after layout or a scroll (a fake mouse move), not after paint-only changes (the cursor still follows any change, as its update timer does); and the focus fixup blurs a focused element that can no longer take focus in a task, with blur and focusout (it was silent). New input fixture `inert` (1× and 1.65) matches Chromium; all earlier fixtures still match.
  - **M5 checklist (2026-10-30):** hit testing, event routing, hover/active/focus, keyboard navigation, text input, scroll views, tooltips, modals (the game's are `aria-modal` divs and `inert`) and drag and drop (the game drags with pointer capture only; no HTML drag and drop) are done. **M5 is finished.**
  - **Moved to one git repo for GitHub (2026-10-30, 494 tests):** `engine/` + `old-game/` in `/run/media/system/M.2_2TB/Stat Fick Repo/` (user decisions above). Paths come from the repo (`Tools/Scripts/env.sh`, `env.ps1`; a git-ignored `env.local.sh` keeps this PC's runtime folder); the harnesses and the old game's test client (its source unpacked into `old-game/test-client`) run on Electron 44.5.0 from npm (`Tools/Scripts/chromium.sh`, Xvfb without a display); the tests act as Chromium on Linux with this PC's font settings (`Infrastructure/ReferencePlatform.cs`, `Platform/PlatformBehaviour.cs`) and take system fonts from `Data/SystemFonts` (fontconfig's answers recorded here, `Infrastructure/SystemFonts.cs`); native libraries load from the portable runtime folder (Ubuntu's .NET reports `ubuntu.24.04-x64`); the frame speed check and two opacity-text tolerances allow for a CPU Vulkan. Checked: 494 pass on the GPU and on Mesa's lavapipe; a fresh clone in a clean Ubuntu 24.04 container prepared only by `Tools/Scripts/cloud-setup.sh` builds, passes all tests, runs the old game's smoke suite and reproduces the script references byte for byte; a win-x64 build carries every native library; the PowerShell scripts parse (Windows itself untested here). Found on the way: media query lists with listeners were only weakly held, so a garbage collection could lose their change events (a flaky `resize-observer-window`); they're now kept alive as in Blink (`ScriptApiTests.ListenersKeepWhatTheyListenToAlive`).
  - **Border-style changes repaint (2026-10-30, 496 tests):** a change to border styles or widths now lays out only when a used width (0 for none and hidden) changes, otherwise it only repaints, as Blink's ComputedStyle::DiffNeedsFullLayout treats its kBorderWidth group (`StyleDifference.BorderFields`). The UA style's button :active (outset to inset) no longer lays out, so :hover isn't recomputed after it either. `IncrementalWorkTests`: outset to inset, none to hidden (also with another width) don't lay out; a width change and none to solid do.
  - **M6 step 1, 3D core (2026-10-30, 501 tests, `Docs/Renderer-3D.md`):** cameras (orthographic, perspective; reversed depth, Vulkan's Y flip), GPU meshes pulled by address, MSAA 4× sRGB targets with D32 depth and a resolve, a flat-colour pipeline. First Blender reference harness: the tests write `Reference/r3d-scenes.json`, `Tools/Scripts/make-blender-references.sh` renders it with EEVEE (`Tools/BlenderReference/render_scenes.py`) into `Golden/r3d-*.png`, and `Render3DReferenceTests` compares (interiors within 2 levels, edges by the MSAA tolerance). Three scenes match Blender, interiors exactly. The reference found a culling bug: with the projection's Y flip glTF's front faces stay counter-clockwise on screen, and culling clockwise had drawn the back faces.
  - **Left for the port, not M5:** the game listens for `visibilitychange` and `pagehide` (map.js pauses travel); the window host doesn't send them yet.

## Milestones

Each milestone is finished only when its tests pass.

- [x] **M0 Setup:**
  - Solution, shared build settings, environment and build scripts.
  - Native library resolution, logging.
  - Test project, sandbox.
- [x] **M1 Platform and GPU:**
  - Window and input (GLFW).
  - Vulkan 1.3 device: dynamic rendering, synchronization2, descriptor indexing.
  - VMA memory, buffers, images, uploads, bindless textures.
  - Samplers, SPIR-V shaders compiled at build time, pipelines.
  - Swapchain with resize, offscreen targets, readback, frames in flight with deferred deletion.
  - A headless GPU test fixture with golden-image comparison.
- [x] **M2 2D canvas:**
  - Batched rectangles with per-corner radii, borders and box shadows.
  - Linear and radial gradients, images.
  - Vector paths (fill and stroke).
  - Clip stack (rectangles and rounded rectangles), 2D transforms.
  - Opacity layers and filters (blur, drop-shadow, brightness, contrast, saturate, grayscale, sepia, hue-rotate).
  - Blend modes.
- [x] **M3 Text:**
  - FreeType bindings, HarfBuzz shaping, a dynamic glyph atlas.
  - Font families and styles, fallback.
  - Layout: line breaking, alignment, ellipsis, letter-spacing, text-transform, rich spans.
  - Measurement for the layout engine.
- [x] **M4 UI:**
  - Element tree, a stylesheet language (selectors, pseudo-classes, variables, calc, media queries, keyframes, transitions).
  - Box, flex, grid and absolute layout, overflow and scrolling, stacking.
  - Painting through the canvas.
- [x] **M5 UI interaction:**
  - Hit testing, event routing, hover, active and focus states, keyboard navigation.
  - Text input, scroll views, tooltips, modals, drag and drop.
- [ ] **M6 3D renderer:**
  - glTF import, meshes and materials, GPU skinning.
  - Cel shading in palette colours, ink lines (silhouette and inner edges), hatching, glow.
  - Orthographic and perspective cameras, terrain, particles.
  - 3D views inside the UI (portraits, previews).
- [ ] **M7 Animation:**
  - Skeletons and clips from glTF, sampling, cross-fades, layers.
  - Events, state machines.
- [ ] **M8 Audio:**
  - OpenAL Soft streaming, a mixer with buses and fades.
  - Synthesizer: oscillators, noise, envelopes, biquad filters, convolution reverb, panning, sample-accurate scheduling.
  - Offline rendering for tests.
- [ ] **M9 Assets and data:**
  - The `.pak` format: writer, reader, compression, hashes.
  - A virtual file system (packs plus loose development folders).
  - Asset loaders, binary data serialization with versioning, the JSON class loader.
  - Saves and settings.
- [ ] **M10 Scene and sequencer:**
  - Nodes and transforms.
  - A timeline sequencer for cutscenes: tracks for transforms, animations, camera, text, audio and effects. Tweening.
- [ ] **M11 Editor foundation:**
  - An editor app shell on the engine UI: property grid, lists, undo and redo.
  - Pack editing.
- [ ] **M12 Packaging:**
  - Linux and Windows publish into the shipped folder layout, crash logs.

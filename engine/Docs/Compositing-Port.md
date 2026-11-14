# Compositing port (LCD vs grayscale text)

User decision (2026-10-24): match Chromium exactly — text is grayscale where Chromium 152 puts it in a
cc layer that can't use LCD text, LCD elsewhere. This needs Blink's layerization.

## Ground truth

`contentTracing` with `disabled-by-default-cc.debug` (+ `devtools.timeline.layers`, `viz.quads`) records
`LayerTreeHostImpl:snapshot` instant events. `args.snapshot.active_tree.layers` (with `children`) has,
per cc layer: `layer_name` (Blink debug name: "LayoutFlexibleBox (positioned) ASIDE id='x' class='y'"),
`compositing_reason_ids` (e.g. RootScroller, OverflowScrolling, FixedPosition, BackdropFilter, Overlap,
Scrollbar, Canvas, WillChangeTransform), `layer_quad` (screen quad), `contents_opaque`,
`lcd_text_disallowed_reason` (none, no-text, background-color-not-opaque, contents-not-opaque, …),
`owner_node`, `draws_content`. The renderer's tree is the one with a "LayoutView" layer.

Findings on the game (2026-10-24, 1× and 1.65 alike): the root scrolling contents layer is opaque (LCD);
the fixed side menu (`.main-tools`, fixed + scroller) is its own layer by **Overlap** (MergeFixedLayers:
fixed-only transforms try to merge first) and not opaque (its box shadow widens the layer) → grayscale;
modal layers are FixedPosition + BackdropFilter; their scrolling panels and later siblings that overlap
become Overlap layers; scrollbars are their own layers (RasterInducingScroll is stable on Linux).

## Blink rules collected (Chromium 152)

- LCD text preference: DSF ≥ 1.5 → kIgnored; else kStronglyPreferred (WidgetBase::ComputeLCDTextPreference).
- Direct reasons (CompositingReasonFinder::DirectReasonsForPaintProperties): animations of
  transform/scale/rotate/translate/opacity/filter/backdrop-filter; will-change (transform,
  opacity, filter, backdrop-filter, …; top/left/… → WillChangeOther); 3D transforms; perspective/
  preserve-3d with 3D descendants; root scroller; fixed position when the layout viewport has
  overflow (kFixedPosition), sticky with a scroll-dependent offset; backdrop-filter; 3D scene leaf.
- Composited scrolling (PaintArtifactCompositor::ComputeNeedsCompositedScrolling): direct reasons →
  yes; not user scrollable → no; select / <input> scrollers prefer non-composited; if LCD preference
  isn't kStronglyPreferred → yes; else yes only when the first chunk after the ScrollHitTest chunk
  (skipping chunks in the same transform as the hit test) is in the scroll translation, the overflow
  clip, the same effect, and its rect_known_to_be_opaque contains the scroll contents rect.
- Layerize (Layerizer::LayerizeGroup): chunks grouped by effect recursively; each chunk → pending
  layer (types: ScrollHitTest layer if composited scroll, foreign, scrollbar layer (RasterInducingScroll
  → every scrollbar with a scroll translation), other). A new layer whose nearest directly composited
  transform is seen for the first time can't merge (except fixed-only / sticky-only transforms:
  MergeFixedLayers, MergeStickyLayers stable). Else try merging into earlier layers in the group
  (reverse order, distance limit 0x10000000); stop at the first that MightOverlap → overlap layer.
  Subgroups that produced exactly one layer may be decomposited (DecompositeEffect: no direct
  reasons, CanUpcastWith, blend mode rules) and then merged like a chunk.
- PendingLayer::CanMerge: CanUpcastWith (same effect; transforms in the same compositing boundary —
  same nearest directly composited ancestor, or fixed-only/sticky-only mergeable; same nearest
  composited scroll translation; clip chains within the boundary; scroll translations covered);
  sparsity: merged area − (home + guest) ≤ 10000·DPR²; opaque: MaximumCoveredRect of mapped rects;
  text_known_to_be_on_opaque_background rules; with kStronglyPreferred, refuse merges that would lose
  LCD text of a side that had it.
- Opaque: DrawingDisplayItem::CalculateRectKnownToBeOpaqueForRecord (≤ 8 ops; Save skipped, ClipRect
  narrows; DrawRect/IRect/RRect (inset 0.3·radius)/opaque images with opaque solid colour or opaque
  shader and no colour-changing flags; MaximumCoveredRect). Chunk: rect_known_to_be_opaque =
  MaximumCoveredRect over items; text_known_to_be_on_opaque_background = each text item's visual rect
  inside the chunk's opaque rect so far.
- Background layers in the opaque analysis (2026-11-06, with the background-layer port; Engine-Plan.md Progress,
  "Background layers as Blink paints them"): a layer one tile covers is drawn as that tile (`PaintFastBottomLayer` for the
  bottom layer, square boxes too since `ContouredRect::HasRoundCurvature` holds without radii, else
  `OptimizeToSingleTileDraw`): a DrawRect/DrawRRect with the gradient's shader, opaque when every stop is; a repeated tile
  is a DrawRect with a picture shader (`GeneratedImage::DrawPattern`), never opaque (`PaintShader::IsOpaque`,
  `kPaintRecord`). The game's dialog headers depend on it: `settings` and `companions` at 1× composite as Chromium only
  with the header's gradient one tile (101.296875 px tall with a 1 px border, one tile only by the snapped size the fast
  path tests). Checked in Chromium with layer traces and `LayerTree.snapshotCommandLog` (a single tile is a dithered
  gradient `drawRect`, a pattern a picture shader).
- cc layer: contents_opaque = background colour not transparent && rect_known_to_be_opaque contains the
  layer rect; else contents_opaque_for_text = has text && text known on opaque background. cc
  LCD disallowed: will-change transform (node or ancestors), transform animation, filter/backdrop
  filter effect node (lcd_text_disallowed_by_*), non-integral offset (unless raster translation
  aligns), !contents_opaque_for_text.
- Save layers in a layer's display list: SaveLayerAlpha keeps LCD (kInitWithPrevious) unless the
  list has a SaveLayerOp (filter, blend mode); isolation groups (opacity 1) around blending
  descendants.
- Which verdict a drawing gets (2026-11-06, Engine-Plan.md Progress, "Text first in a paint chunk"): cc rasters a
  layer's tiles with or without LCD text as a whole (`TileManager`: `playback_settings.use_lcd_text =
  tile->can_use_lcd_text()`; `RasterBufferProvider`: an unknown pixel geometry turns LCD off), so every display item
  takes the verdict of the layer its chunk went to. A display item joins its chunk when it ends, after its drawings
  were recorded (`DrawingRecorder`'s destructor → `PaintController::CreateAndAppend` → `PaintChunker::
  IncrementDisplayItemIndex` → `EnsureCurrentChunk`): the current chunk, or a new one when one will start
  (`WillCreateNewChunk`: a forced new chunk, or other properties). The engine draws while it records, so text in an
  item takes the verdict of the chunk that will be current when the item ends, which may not exist yet:
  `PaintTarget.UseChunkTextVerdict` before each text drawing (glyph runs and a control's laid-out label). Before
  that fix the verdict was set only when a chunk was made (at an item's end, or at a hit test rect or a drawing
  outside an item), so text first in a chunk (in an `overflow: hidden` or `clip` box, after a stacked layer's
  subsequence) took the previous chunk's: grayscale where Chromium has LCD after a chunk without text, LCD where it
  has grayscale after an LCD layer.
- `background-clip: text` (GM1 step 1a, 2026-11-13; `Docs/Game/Foundation.md`, "How it works", step 1a):
  `BoxPainterBase::PaintFillLayerTextFillBox` clips to the box's snapped border box, opens a layer (a SaveLayerOp), paints
  the layer's colour and image into it, opens a second layer with `SkBlendMode::kDstIn` and paints the box's text into
  it in black (the `kTextClip` phase), then closes both.
  - *In the display item:* a save, a clip and a save layer, which end the item's opaqueness analysis.
    `PaintTarget.PushTextClipLayer` records them so.
  - *The mask layer:* `PushTextMask` composites it with `BlendMode.DestinationIn` (`Graphics/PipelineCache.cs`: the
    destination times the source's alpha). Text drawn into it is grayscale: it is drawn into a transparent save layer,
    whatever the chunk's verdict.
  - *Not modelled:* cc's rule that a SaveLayerOp in a cc layer's record keeps that layer's SaveLayerAlpha ops from
    preserving LCD text (`PaintOpBuffer`'s `has_effects_preventing_lcd_text_for_save_layer_alpha`). Text in a translucent
    element sharing a cc layer with a clip-text box is grayscale in Chromium at 1×, LCD in the engine. No captured game
    screen is expected to have a clip-text box.
- Icons (GM1 step 7; `Docs/Game/Foundation.md`, "How it works", step 7): an icon is a glyph of an icon face in a text
  run, drawn by `Canvas.DrawGlyphs` as its outline through the path fill. *Its part in the verdict:* the painter records
  its run as text (`PaintTarget.DrawGlyphs`: `Record(…, text: true)`, `UseChunkTextVerdict`), so a chunk holding an icon
  is a text chunk for the layerization (`contents_opaque_for_text`, text known to be on an opaque background) as
  Chromium's chunk holding the fallback glyph it stands for was (the drawing's recorded bounds the icon face's box).
  *Its own drawing:* always grayscale with no mask gamma, whatever the chunk's verdict (`Canvas.TextLcd` isn't
  read for it), as Chromium fills an inline SVG path, the icons' reference (`Ui/IconReferenceTests`); letters beside it
  keep the verdict. Why not LCD: an icon isn't a glyph of a font, Skia never draws a path with LCD coverage, and the
  comparison with Chromium is with its path fill.
- A text's y is rounded against its parent's offset (`TextFragmentPainter`'s `ComputeBoxGeometry`; GM1 step 1a).
  - *The parent:* the innermost inline box around it that has a box fragment on the line
    (`LayoutInline::ComputeInitialShouldCreateBoxFragment`: positioned, translucent, filtered, blended, clipped, a
    background, a border, a shadow, padding, margin or an outline), else the line. `BoxFragmentPainter` passes a box
    item's own offset as the parent offset to its children.
  - *Before:* the engine rounded every text against its line's top, so text in a relatively placed span could be one
    device pixel off at fractional scales.

## The iframe and the main frame (2026-11-14)

**A `Tools/PaintReference` capture is an iframe; the game's and the editor's documents are main frames.** For the same
markup, Chromium 152 can composite a fixed (or otherwise separately composited) box over the page differently in the two,
and give its text another LCD verdict. The engine models every document as a main frame and is right for the game. Found
by GM1 step 8's tooltip fixture: the engine drew the tooltip's text grayscale, the fixture LCD.

- **Why, from Blink's source:**
  - *The root scroller:* `CompositingReasonFinder::RequiresCompositingForRootScroller` gives `kRootScroller` only to
    `IsGlobalRootScroller()`, the page's root scroller: the main frame's LayoutView. So the main frame's scroll translation
    is directly composited even when nothing scrolls; an iframe's isn't.
  - *A fixed box:* gets `kFixedPosition` only when its frame's layout viewport `HasOverflow()`
    (`CompositingReasonsForScrollDependentPosition`; the viewport-scroll reasons only in the outermost main frame).
  - *In the main frame:* the fixed box's paint offset translation hangs off the root transform, outside the composited
    scroll translation, so it isn't in the page contents' compositing boundary (`PendingLayer::CanUpcastWith`). It can't
    merge into the page's layer; it overlaps it, so it is a layer of its own (the trace names its reasons `Viewport,
    OverflowScrolling` while the page doesn't overflow, `Overlap` when it does).
  - *Its verdict:* the layer's own, from cc's `PictureLayerImpl::ComputeLCDTextDisallowedReason`. Without
    `contents_opaque_for_text` (text known to be on an opaque background) and with no opaque background colour, the
    reason is `kBackgroundColorNotOpaque`.
  - *The tooltip:* its 2 px radius and its shadow (drawn inside save, clip, restore) each end its display item's opaque
    analysis. With the radius under an opaque border the gradient is painted under a rounded clip (the bleed avoidance's
    shrinking, `BoxPainterBase::PaintFillLayer` off its fast path); and with the box's fractional height one tile doesn't
    cover it, so it is also a pattern, not known to be opaque (`DrawTiledBackground`'s single-tile test). So its text
    isn't known to be on an opaque background, and it is grayscale.
  - *In a non-overflowing iframe:* nothing is directly composited, and the frame's content, the fixed box included, merges
    into the host page's opaque scrolling layer with LCD text.
- **The probe** (`claude-work/gm1/lcd/`: `LayerProbe`, the cc layer trace of `Tools/GameReference/layers.cjs`, the markup
  rendered both ways at 1 and 1.65). A fixed `sv-top-layer` (inset 0, z-index max) holds a box with a leather gradient,
  a border, Lato 14px/1.6, over a parchment page:
  - *In the main frame,* two layers every time: the page's scrolling background (opaque, LCD) and `SV-TOP-LAYER` (not
    opaque). Its LCD is `none` for the plain box. It is `background-color-not-opaque` with a 2 px radius, the
    `0 10px 30px` shadow, both (the tooltip), a solid colour with both, or a translucent background.
  - *In the iframe,* one layer (everything LCD), unless the iframe's page overflows: then the top layer is an `Overlap`
    layer, grayscale, as in the main frame.
  - *The pixels:* Chromium against itself (main frame against iframe, the 800 × 600 tooltip scene) differs in 2,777 pixels
    past 12 levels at 1 (0.58 %) and 5,234 at 1.65 (0.40 %). Step 8 measured 2,830 and 5,515 between the engine and its
    iframe fixture.
- **What changed:** `Tools/PaintReference` takes a fixture field `mainFrame: true`. The fixture's page is the own document
  of a `WebContentsView` of the fixture's size over the window, and everything else is done the same. A fixture without
  the field is captured exactly as before: the `stylesheet-rule` and `chunk-text` lists captured again came out byte for
  byte, their rectangles and computed values too.
  - *Sizes:* a main-frame fixture's size must be whole DIPs, so use sizes that are whole device pixels at 1.65 (multiples
    of 20 px). An iframe can be 664.85 px tall where a view can't, and a layout a fraction apart moves every pane.
  - *When to use it:* for any screen with a fixed box or a box composited apart from the page's layer over it: the top
    layer (tooltips, select lists, validation bubbles) and fixed dialogs or overlays.
  - *The engine's fixtures:* `Ui/TopLayerReferenceTests` (`Reference/top-layer-fixtures.json`, main-frame, 480 × 240,
    a page with two lines of text and boxes in its top layer), each at 1 and 1.65 against Chromium's main frame, pixels
    past 12 levels (limits 230 and 627):
    - `top-layer-tooltip`, the tooltip, two lines and one: 8 and 48. The same markup in an iframe (recorded, not a
      test) differs from the engine in 3,385 pixels at 1 (2.94 %) and 6,564 at 1.65 (2.09 %).
    - `top-layer-plain`, the two-line box alone without radius or shadow (LCD): 0 and 0.
    - `top-layer-radius`, the radius alone (grayscale): 4 and 24. Its box has a height of whole pixels, so its gradient
      is one tile and only the rounded clip ends the analysis.
    - `top-layer-shadow`, the shadow alone (grayscale): 0 and 0.
    - The test also checks each fixture's layers as the main frame's trace has them (the page's layer opaque with LCD;
      the top layer an overlap layer, not opaque, its reason the fixture's), at both scales.
    - *A layer has one rect known to be opaque:* with two boxes even the plain one is grayscale in Chromium's main frame
      and in the engine (the second box's text is off the layer's opaque rect). The variations have one box each.
- **The editor's screens** have no fixed boxes. `checks-problems` and `references-list` at 1.65 composite the same in
  Chromium's main frame as in its iframe (six and eight layers).

## Status (2026-10-25)

**All 22 game screens composite exactly like Chromium** (`CompositingReferenceTests`: every screen
at 1× and 1.65; layer for layer: owner, screen rect ±1.5 px, contents opaque, LCD-disallowed reason).
The shop was finished on 2026-10-25 (see "Shop, finished" below). Full suite then: 467 tests, all
pass (493 at the end of M5, 2026-10-30).

The painter runs two passes per frame (`UiPainter.Paint`): an analysis pass (`Traverse(canvas,
analyze: true)`: `PaintTarget` in record-only mode — everything is "drawn", nothing reaches the
canvas) that builds the property trees, records paint chunks and runs the layerizer; then the draw
pass, which walks the same chunk sequence and, before each text drawing, sets `Canvas.TextLcd` from the
verdict (`PaintChunk.LcdText`) of the chunk the drawing belongs to (since 2026-11-06 the chunk its display
item will join, which may not be made yet; "Which verdict a drawing gets" above). Opacity layers start from
the backdrop only when their effect has LCD text.
The old heuristics (ScanLayerEffects, grayscale scopes) and the `SV_OLD_HINT` switch are gone.

### Files

- `UI/Paint/Compositing/PropertyTrees.cs` — transform/clip/effect/scroll nodes (screen space).
- `UI/Paint/Compositing/PropertyTreeBuilder.cs` — Blink's property tree builder subset (see rules).
- `UI/Paint/Compositing/Geometry.cs` — gfx rect semantics, GeometryMapper (visual rects, visibility
  limit, overlap with scroll ranges).
- `UI/Paint/Compositing/Layerizer.cs` — PaintChunk, PendingLayer, LayerizeGroup, Merge, DecompositeEffect,
  composited scrolling, background colour, cc LCD reason, hit test opaqueness union.
- `UI/Paint/PaintTarget.cs` — chunk recording: `UseState`, `BeginItem/EndItem` (display items),
  `Record` (DrawingDisplayItem opaque/background analysis), `RecordScrollHitTest`, `RecordScrollbar`,
  `RecordForeign`, `RecordScrollingBackground`, `BeginLayer/EndLayer` (subsequences), `SuppressChunks`.
- `UI/Paint/UiPainter*.cs` — calls the above where Blink paints each thing.
- `UI/Paint/UiPainter.Svg.cs` — inline SVG: transforms from computed styles (TransformHelper), Blink's
  bounding boxes, one display item per shape; `UI/Style/SvgTransformList.cs` — the transform
  attribute (parser and CSS mapping).
- Test: `Tests/Statventure.Tests/Ui/CompositingReferenceTests.cs`; references
  `Reference/scaled/game-layers@1.json` / `@1.65.json` (`Tools/Scripts/make-layer-references.sh <scale>`).
  Text's verdict per chunk: `Tests/Statventure.Tests/Ui/ChunkTextLcdReferenceTests.cs` (the `chunk-text-lcd`
  fixture, its own list `Reference/chunk-text-fixtures.json`, Chromium 152 at 1 and 1.65 in
  `Golden/ui-chunk-text-lcd@*.png`: `Tools/Scripts/make-scaled-paint-references.sh <scale> 1 chunk-text-fixtures.json`).
  A box in the top layer as the main frame composites it: `Tests/Statventure.Tests/Ui/TopLayerReferenceTests.cs`
  (`Reference/top-layer-fixtures.json`, a main-frame fixture; "The iframe and the main frame" above).
- **Debugging:** `COMPOSITING_CHUNKS=1 dotnet test … --filter "FullyQualifiedName~CompositingReference&DisplayName~shop"`
  lists each engine layer's first 40 chunks (owner, bounds, drawable, opaque rect, background colour,
  text flags, clip, effect). Blink sources for Chromium 152 are cached in
  `~/.cache/stat-fick-build/reference-src/chromium-152-source` (see the README there; fetch more with the googlesource `?format=TEXT` URL, tag 152.0.7977.89, and keep them in that folder).
- **Threads (2026-11-03):** the analysis pass isn't pure layout: underlines that skip ink load glyph outlines
  from FreeType (`GlyphOutlines`; of the captured screens only the map's, its underlined destinations), and
  the tests run it on many threads sharing fonts. A FreeType face takes one thread at a time, so everything
  that uses a face's FreeType state holds `FontFace.FreeTypeLock` (`FreeTypeThreadingTests`); before that,
  `map` failed now and then in a full run (a NullReferenceException reading an outline another thread's
  glyph load was replacing).

### Rules ported on 2026-10-25 (each one fixed screens)

- **Paint order:** flex and grid items paint atomically (`IsPaintedAtomically`, like inline blocks:
  all phases in the parent's foreground phase, in order-modified order) — `UiPainter.PaintsAtomically`.
  A stacked layer is painted as a **subsequence** that forces new chunks before and after when its
  border box state differs from the last chunk's, it has ≥ 2 descendant PaintLayers, its hit-test
  transparency differs from the current chunk's, or joining the last chunk would be sparse
  (> 10000·dpr² + both areas) — `PaintLayerPainter::ShouldCreateSubsequence`.
- **Display items:** a box decoration item's visual rect is the box's self ink overflow (border box +
  outer shadows' reach of 1.5 × blur + spread, + outline), not its drawings; items that draw nothing
  aren't recorded. Opaque analysis (`CalculateRectKnownToBeOpaqueForRecord`): ≤ 8 ops counting saves
  and clips, a restore or rounded clip ends it, so **any outer box shadow makes the item non-opaque**
  (Blink draws shadows first inside save/clip/restore); results are clipped to the visual rect and the
  item's own clips. The background colour candidate is separate: first plain fill, ignoring the
  8-op limit (gradients aren't candidates).
- **Document background:** the base colour fills the pixel-snapped document rect (not the viewport),
  skipped when the root background is opaque.
- **Backdrop filters** are effect properties, not drawings (no chunk item).
- **Hit test data:** every visible box records its border box (snapped) with an opaqueness:
  transparent for pointer-events: none (still recorded), mixed with border radius, else opaque;
  cc's `UnionHitTestOpaqueness`. An inline SVG records its border box even when it takes input at its
  overflowing shapes.
- **Scrollers:** scroll node only for `NeedsScrollAndScrollTranslation` (content overflowing an
  auto/scroll axis by whole pixels, or scrolled, or overscroll-behavior not auto) — overflow: hidden
  boxes don't get one otherwise. Scroll node rects are whole pixels (contents size snapped half up).
  Scrollbars (non-overlay) record in the background phase before the scrolling background; an
  auto/scroll box records its scrolling-contents hit test (contents rect + borders) in its contents
  state, with the background when it goes there.
- **Paint offset translations** (`NeedsPaintOffsetTranslation`): boxes needing a scroll node, with
  scrollbars or a resizer, SVG roots, backdrop filters, effect will-change/animations get a translation
  node of their own; fixed-to-view boxes always do (FixedPosition reason only when the page
  overflows). This decides which clips limit a layer's bounds (VisibilityLimit needs the clip in the
  layer's own transform), e.g. backdrop-filter panels aren't clipped by an ancestor's overflow.
- **SVG roots:** contents in a replaced-content transform node, clipped to the content box unless
  overflow is visible. Replaced content is one display item in the box's contents state.
- **Effects:** `isolation: isolate` alone makes no effect node (only around blending descendants,
  opacity, blend, backdrop filter, mask — `NeedsEffectIgnoringClipPathAnd2DScale`).
- **Canvas:** a drawn `<canvas>` (the host gives it a surface texture) is a foreign layer (reason Canvas),
  as is a `<surface>` showing a 3D view (`UiHost.Views`, Docs/Renderer-3D.md, "3D views in the UI").

### Shop, finished (2026-10-25)

- **Capture setup:** the shop rolls its stock with `Math.random` as it opens, and the layer traces
  (`make-layer-references.sh`) are a separate run from the screen captures, so the trace showed other
  items than the captured markup (that was the "disabled buy button" difference). `game-capture.js`
  now seeds `Math.random` (mulberry32, seed 0x5eed) around `Shop.open`; all game references were
  captured again (only the shop changed; script `?v=` stamps and combat idle delays still differ
  between runs, harmlessly).
- **UA sheet (Blink svg.css):** `svg:not(:root), symbol, image, marker, pattern, foreignObject
  { overflow: hidden }` (clip at the content box, as overflow-clip-margin: content-box) and SVG
  elements' `transform-origin: 0 0` (`svg *`). This fixed `svg.sb-live`'s bounds. Blink's replaced
  elements never scroll, so an inline svg isn't a scroll container (`LayoutBox.IsScrollContainer`);
  flex and grid automatic minimum sizes now look at the axis's computed overflow value
  (`IsOverflowValueScrollableX/Y`: hidden, scroll, auto), as FlexLayoutAlgorithm::ShouldApplyAutoMinSize
  and CalculateIntrinsicMinimumContribution do.
- **SVG transforms from CSS (user decision 2026-10-25):** the capture's freeze sheet
  (`transform: none !important`) overrides the SVG `transform` attribute in Chromium (it's CSS
  transform's presentation attribute), so the weapon art's sword stood upright, overflowing its box,
  and the sparse merge rule split the svg's filter group into two layers. Ported: the attribute
  parsed as SVGTransformList::ParseInternal and mapped to CSS as SVGTransformList::CssValue
  (`Style/SvgTransformList.cs`, a presentational hint shared by attribute text so styles can still be
  shared; changes restyle the element); `transform-box`; SVG children painted, hit tested and
  measured with TransformHelper::ComputeTransform (reference box fill-box/stroke-box/view-box from
  0 0, zoomed reference box, unzoomed translation); CSS `matrix()` translations zoomed
  (TransformBuilder). Blink's SVG geometry: object bounding boxes (rect/ellipse geometry, tight path
  bounds = SkPath::computeTightBounds, containers' unions), ApproximateStrokeBoundingBox, and **each
  shape is a display item whose visual rect is its stroke box enclosed in whole user units**
  (SVGDrawingRecorder) — that is what gives Chromium's fractional layer quads. SVG elements' client
  rects are their object bounding boxes through their transforms (Element::ClientQuads).
- **Unnamed layers:** Chromium reports no debug name for the first layer of the weapon art's filter
  group (its first chunk's client recorded none); the test treats an empty Chromium name as unnamed.

### Known differences left (SVG)

- SVG children with running transform animations get composited transform nodes in Chromium
  (directly composited, own layers); the engine paints them inside their svg's layer. Transform,
  opacity and filter property nodes for SVG children aren't built (group opacity is applied per shape,
  filtered shapes aren't drawn). Fill, stroke and opacity come from attributes and style attributes
  only (user decision: no full Blink SVG styling).
- A stroke-box reference box uses the approximate stroke box (Blink strokes the path for it); SVG
  text has no bounding box (it isn't drawn).
- Path antialiasing differs from Skia at edges (user decision 2026-10-25: keep the canvas's
  rasterizer); the `svg-transforms` paint fixture compares at tolerance 60 and checks geometry
  exactly through the element boxes at 1.65 and 1.98.

### Next

Compositing is done for every captured screen. Text pixels in composited layers are covered by
GamePaintTests (they pass with the per-chunk LCD verdicts). Nothing is left here; M5 is finished
(2026-10-30, 493 tests). Come back to this doc when a change moves layers or LCD verdicts
(`CompositingReferenceTests` fails) or when new screens are captured.

### The recapture (GM1 step 1b, 2026-11-14)

The 11 states were captured again from today's old game, with the layer traces at 1 and 1.65 (`Docs/Game/Foundation.md`,
"How it works", step 1b).
- *Before the step's layout rules,* `CompositingReferenceTests` failed for 20 states (every state but combat, at both
  scales), on the document's height alone (2231 px against 2780 at 1). That came from the equipment slots' layout (a
  zero percentage taken as `0px`).
- *Once that was ported,* every captured screen's layers and LCD verdicts match at 1 and 1.65. Nothing new in
  compositing came out from behind it.
- *The capture:* the scale-1 trace needs a 1440 × 950 output when cage runs it, since cage maximises the window.

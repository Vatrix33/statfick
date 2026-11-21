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
  descendants. A kInitWithPrevious layer starts with the backdrop's colours whatever the target's pixel format (Skia
  draws the prior device into it; "The target's format", 2026-11-15).
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
- Four rules ported by GM1 step 9 (2026-11-14; `Docs/Game/Foundation.md`, "How it works", step 9; the fixtures
  `Ui/LcdTextRuleReferenceTests`, main frames at 1 and 1.65, each case's LCD verdict read from Chromium's pixels and its
  cc trace):
  - *Skia's 48 px limit* (`SkScalerContext.cpp` `too_big_for_lcd`, `SK_MAX_SIZE_FOR_LCDTEXT`): a glyph run larger than 48
    device px is no LCD mask. With a matrix that only translates, the text size past 48 (48 itself keeps LCD); with one
    that scales or skews, the size squared times the signed area of the 2×2 part (each entry `sk_relax`ed to 1/1024) past
    48². One verdict per run (`Canvas/CanvasText.cs`, `TooBigForLcd`). Skia then draws it exactly as text in a layer
    without LCD, which the engine already had; Fontations ignores `kGenA8FromLCD`; Skia's full-to-normal hinting for
    non-LCD text changes nothing, the engine's Full and Medium loading alike.
  - *cc's save layers* (in place of the rule two items up, which made all of a layer's text grayscale when the layer held
    a filter or a blend): Blink's `StartEffect` (`paint_chunks_to_cc_layer.cc`) records a filter and a blend mode as a
    `SaveLayerOp` and opacity alone (an isolation group too) as a `SaveLayerAlphaOp` (a backdrop filter's, a
    `SaveLayerFiltersOp`, is a layer's own: a backdrop filter is a direct compositing reason). A `SaveLayerOp` in a layer's
    record sets `has_effects_preventing_lcd_text_for_save_layer_alpha` (`paint_op_buffer.cc`), and only then do the
    record's `SaveLayerAlphaOp`s lose `kPreserveLCDText | kInitWithPrevious` (`paint_op.cc`; a `SaveLayerOp` never has
    them); Skia gives a layer without `kPreserveLCDText` an unknown pixel geometry (`SkCanvas.cpp`), so its text is
    grayscale. So: text in no save layer keeps its layer's verdict; text in save layers is grayscale when the layer's
    record holds a `SaveLayerOp` (its own among them), else keeps LCD (`Layerizer.TextKeepsLcdInLayer`).
  - *The root element is a stacking context* (`StyleAdjuster`'s `ForcesStackingContext` for `is_document_element`), so
    it is the isolation group around blending descendants no nearer stacking context holds
    (`NeedsEffectIgnoringClipPathAnd2DScale`: a stacking context whose layer `HasNonIsolatedDescendantWithBlendMode`), an
    effect node under the root effect; not the body's, not the view's (`PropertyTreeBuilder.CreatesStackingContext`).
    With it, `ViewPainter::PaintRootGroup`: the view's base background alone (the Root Group's backdrop) in the view's
    contents state, then the root's background in the Root Element Group's state, the root's border-box state without
    its own transform, without the base (`UiPainter.PaintCanvasBackground`, `RootElementGroup`).
  - *A decomposited blend mode's merge* (`PendingLayer::CanMerge` with `force_merge`): it merges whatever the sparsity
    or the text, and the merged layer keeps `Merge`'s defaults: nothing known opaque and no text known to be on an opaque
    background (the engine had kept the home layer's text verdict). Where LCD text is strongly preferred (below 1.5)
    the next layer with text on an opaque ground then refuses to merge into it: in `lcd-blend` at 1 an opacity box after
    the root's blend is a layer of its own (Overlap) and keeps LCD, at 1.65 one layer (`Layerizer.Merge`).

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

## Layout units (GM1's engine task of Q34, 2026-11-14 and 15)

**Blink lays every box out in LayoutUnits, at every scale; the engine now does too.** Not compositing, but it decides which
device pixel a border, a glyph row or a layer's edge falls on, so it is kept here beside the rules that read those pixels.
Found by GM1 step 9's start screen at 1.65: its dialog at 263.9969 device px where Chromium has 264.0, so the buttons'
borders at 314.4969 snapped a pixel left of Chromium's 314.5 (about 2,100 pixels a light start screen); step 10's picker
the same (148.4937 against 148.5).

- **The rule (`platform/geometry/layout_unit.h`).** A LayoutUnit is a whole number of 1/64 px. Blink lays out in device
  pixels: its lengths are zoomed by the device scale and the page zoom first (zoom-for-DSF). The engine has done that since
  2026-10-24 ("Zoom", Engine-Plan.md's Progress), so a CSS-px emulation was never the cause. Three things follow:
  - A float becomes a LayoutUnit by truncation (`LayoutUnit(float)`), unless a rounding is named.
  - `LayoutUnit / int` divides the raw count as integers, so every centring's `free_space / 2` drops its odd 1/64.
  - Sums of LayoutUnits are exact (in a float too, under 2^18 px): what Blink sums, the engine sums once each term is a
    LayoutUnit.
  The engine kept floats and truncated at most of Blink's conversion points, not all. `UI/Layout/LayoutUnit.cs` has
  `Truncate`, `Round`, `Ceil`, `Divide` (the integer division), `MulDiv`, `Ratio` and `LayoutUnitDiffuser` for the rest.
- **The sites, each from its Blink function** (Chromium 152, files saved in
  `reference-src/chromium-152-source/layout-scale/`):
  - *Centring and halves, `LayoutUnit.Divide`:*
    - Grid items: `AlignmentOffset` (`layout_utils.cc` 640, `margin_start + free_space / 2`); auto margins are its
      centre too, never negative (`GridLayout.AlignOffset` and the item's auto margins).
    - Grid tracks: `justify-content` and `align-content`, `start_offset += FreeSpace() / 2`; `space-between`, `-around`
      and `-evenly` share `free / n` (`grid_track_sizing_algorithm.cc` 196–290; `GridLayout.PositionTracks`).
    - Flex items: `align-self: center`, `offset = space / 2` (`flex_layout_algorithm.cc` 1970); cross-axis auto margins
      `margin_space / 2` (1938); `justify-content` and `align-content` positions, `InitialContentPositionOffset` (1624;
      `space-around` `free / (2n)`, `space-evenly` `free / (n + 1)`, the safe centre never negative).
    - Blocks: `ResolveInlineAutoMargins` (`length_utils.cc` 1551): the start margin `(available_space / 2)` never
      negative, the end margin the rest of the available size, whatever its sign (block and replaced boxes,
      `LayoutEngine.ResolveInlineAutoMargins`).
    - Out-of-flow boxes: `ComputeMargins` (`absolute_utils.cc` 256): auto margins `free_space / 2`, the end the rest;
      inline-axis with a negative free space the start margin 0 (start dominant), the block axis halved whatever the
      sign. A static position centred in a flex or grid container: the point `start + size / 2` (flex 582, grid
      `grid_layout_utils.cc` 554), the inset-modified containing block grown equally to its nearer edge and the box
      centred in it, `amount / 2` (`ComputeUnclampedIMCBInOneAxis` 128, `ComputeInsets` 326; `LayoutEngine.StaticOffset`).
    - A button's or an input's content: `AlignBlockContent` (`block_layout_algorithm_utils.cc` 188), `free_space / 2`, a
      button's never negative.
    - Text: `text-align: center`, `LineOffsetForTextAlign` (`length_utils.cc` 1607, `(space_left / 2)` never negative);
      `vertical-align: middle` (`inline_box_state.cc` 1319, `(ascent − descent) / 2` less the parent's x-height halved
      and rounded); the top half-leading `((line_height − height) / 2).Floor()` (`line_utils.cc` 36, CalculateLeadingSpace).
  - *Shares that hand the leftover on:*
    - `LayoutUnitDiffuser` (`core/layout/geometry/layout_unit_diffuser.h`): the remainder's 1/64 steps spread over the
      buckets. Flex `space-between`, `-around` and `-evenly` (`ContentDistributionSpace`, 1679), main-axis auto margins
      (1838) and lines stretched by `align-content` (1763) use it.
    - Flexible lengths: `LineFlexer` (`flex/line_flexer.cc`). Items with a zero factor, or a hypothetical size on the
      wrong side of the base size, are frozen first. Then each pass shares the free space out from the last unfrozen item
      to the first: each item takes its factor's fraction of what is left (`free_space * fraction`, by weighted shrink
      when shrinking), rounded (`FromDoubleRound`), and the first takes the rest. Min/max violations freeze the items on
      the total violation's side (`FreezeItems`) and the pass runs again. Factors summing to less than 1 share only that
      part of the initial free space, truncated. (`FlexLayout.ResolveFlexibleLengths`; it replaced the spec's float
      loop, whose truncated sums lost up to a 1/64 an item.)
    - Grid fr tracks: `ExpandFlexibleTracks` (`grid_track_sizing_algorithm.cc` 1110). The fr size is the leftover's raw
      count over the flex factors (1204), as a float. Each track gets fr × flex plus the fraction the tracks before it
      lost, plus a float epsilon, truncated to a raw count; the lost fraction is carried on. GM1's combat actions (six
      `minmax(0, 1fr)` columns) show it: the fourth column starts 1/64 later than truncated shares put it.
    - Grid free space: `DistributeExtraSpaceToSetsEqually` (537, 776). The tracks are taken by growth potential, the
      smallest first; each gets the space left over the tracks left, in raw integers, up to its potential, and the last
      gets the rest. `MaximizeTracks` (1024) uses it, `StretchAutoTracks` (1054, then beyond the limits) too, and so do
      the engine's simplified spans of intrinsic tracks (`GridLayout.DistributeEqually`).
  - *Truncations the intrinsic sizes missed:* an input button's label width (a text item's `SnappedWidth`, rounded up),
    a flex container's max-content gap (Blink's `gap_between_items_` is a
    LayoutUnit), a flex basis and fixed margins in `BasisOuter`, fixed `width`, `min-width` and `max-width` and a
    `min()`/`max()`/`clamp()` width in `LayoutEngine.IntrinsicWidth`, a grid item's fixed margins in its minimum
    contribution. Step 9's dialog came from the first: a 16 px gap counted 26.4 at 1.65 where Blink has 26.390625. The
    brand's max-content went off the 1/64 grid, the grid's auto column grew to it and stretched by a truncated share to
    1240.80625 instead of 1240.8125, and the dialog was centred in that.
  - *Aspect ratios:* `InlineSizeFromAspectRatio` and `BlockSizeFromAspectRatio` (`length_utils.cc` 281, 295) apply the
    ratio's two LayoutUnit terms with `MulDiv`, a raw product over a raw divisor truncated (a border box never under its
    border and padding). The terms come from `LayoutRatioFromSizeF` (`platform/geometry/physical_size.cc` 37): the terms
    themselves when exact, 1:1 for equal ones, else a continued fraction's convergent. `ComputedStyle.AspectRatioTerms`
    holds them for `aspect-ratio`; `ReplacedContent.NaturalRatioTerms` gives a natural size's or a viewBox's.
- **A control's one line of text** (a second rule, in paint and in the controls' baselines; the known item "Control
  labels a device pixel low at 1.65"):
  - *Blink lays it out on an inner block's line box,* not centred by its glyphs' own height. An input button's value and
    a text field's inner editor are centred by `AlignBlockContent`, a button's never above the top. A menu list's inner
    element (`menu_list_inner_element.cc`) is centred by auto margins in the select's flex box, never above the top, with
    a themed list's 1 × zoom padding above and below; its line-height is the font's height when the select's is initial.
  - *The line:* the top half-leading floored, the ascent and descent rounded at the zoomed size (`InlineLayout.MetricsFor`);
    the glyphs' baseline on a whole pixel because the text painter rounds the line's top (`ComputePhysicalBoxGeometry`,
    `text_fragment_painter.cc` 67).
  - *`LayoutTheme::AdjustStyle`* (`layout_theme.cc` 546, 567) resets the line-height to normal for a themed menu list
    (menulist, menulist-button) and a push button (an `input` button). The engine does it in the style engine, so
    `getComputedStyle` reports `normal` as Chromium's does.
  - *A text field's inner editor* (`TextControlInnerEditorElement::CustomStyleForLayoutObject`,
    `text_control_inner_elements.cc` 133) takes the field's line-height, or normal when the font size reaches the
    field's computed line height in whole pixels, when a fixed height is taller than that, or when the height has a
    percentage. The field's intrinsic height is that line as a LayoutUnit.
  - *In the engine:* `ReplacedContent.LineMetrics` and `LineBaseline` are used by the controls' baselines in lines, the
    input buttons' and menu lists' labels (`UiPainter.LabelTop`), the one-line fields' text, caret and selection
    (`FieldGeometry`), and the fields' intrinsic size. A stat input (`700 26px/1.2 Lato`) now has its text at 25 px under
    its content top, not 26 (the item step 1a recorded).
- **Float arithmetic Blink keeps as floats too** (gone through on 2026-11-15, nothing to port):
  - Justification's space per opportunity (`ShapeResultSpacing::SetExpansion`, float).
  - A glyph run's advances, and the text item's `FromFloatCeil` of their sum (ported earlier).
  - Font metrics and the x-height (floats, rounded where named).
  - A number line-height's percentage of the rounded font size (ported earlier).
  - An SVG's natural size from its attributes and viewBox (floats, truncated by `Units` when they become a size).
  - A form control's average character width (fitted earlier).
  - The scrollbar's parts (whole pixels in the theme, a float thumb position).
  - The auto-repeat count (a floor of a division).
  - Text offsets mapped between the DOM and the processed text.
- **The fixtures** (`Ui/LayoutUnitReferenceTests`, `Reference/layout-unit-fixtures.json`, captured as the main frame by
  `make-scaled-paint-references.sh <scale> <page zoom> layout-unit-fixtures.json`):
  - *The pages:* `units-centring` (a start dialog), `units-flex` (thirteen flex rows), `units-margins` (blocks,
    out-of-flow boxes, a static position, `vertical-align`, six grids, two aspect ratios, centred text),
    `units-controls` (menu lists, input buttons, text fields, a button) and `units-cases` (what the mutation rounds
    showed missing: a grid item's auto margins, three stretched flex lines, auto tracks maximized, fixed `minmax` tracks
    maximized with one capped, a button's content, a fixed width, flex bases and a 3.3 px gap in shrink-to-fit boxes,
    controls in a line, two of them with an odd count of 1/64 px free around their line at 1 and at 1.65, an input
    button's own height, a line 1/64 px tighter than its font, a focused field with a selection at a height where its
    line box's baseline and its glyphs' centre round to different pixels at 1).
  - *Each control of `units-controls`* at 1 and 1.65: at most 60 pixels of its box past 12 levels (a label a device row
    off moves about a thousand, 955 to 2,290 before the port; the themes' arrows and faces and a glyph a subpixel aside
    a few dozen at most: the overflowing input button's label at 1.65 has 34 on the GPU and 44 on lavapipe).
  - *Boxes:* every element's box against Chromium's `getBoundingClientRect`, to 0.002 device px, at 1, 1.25, 1.5,
    1.65, 1.75 and 1.65 with a page zoom of 1.2.
  - *Paint:* at 1 and 1.65 within 12 levels and 0.2 %.
  - *`main` fails them:* 29 of the 30 box cases (`units-controls` at 1.25 matched before), four of the ten paintings
    (`units-controls` at 1×, 3,174 pixels past 12 levels, and at 1.65, 6,503; `units-cases` at 1×, 1,677, and at
    1.65, 1,097) and both per-control checks.
- **Left, recorded in "Small tasks left from earlier milestones":**
  - A 40 px menu list at a device scale of 1.5 has its label a device row lower than Chromium's. Its line-height is
    the font's height; the same select with that height given explicitly matches, as do 59 and 61 device-px tall
    ones. 1.5 is a scale the suite doesn't compare.
  - A text item's ceil'd width at 13 px Lato × 1.65 is 1/64 wider than Chromium's ("Middle ": 70.78125 against
    70.765625). Its glyph advances are the shaper's (`Text/`), so `units-margins` puts its inline-block first.
  - `will-change: transform` boxes' text at 1.65 in an iframe capture lies a device pixel from Chromium's (found by
    trying `AControlsLabelTakesItsChunksVerdict` against Chromium again; the label rows themselves matched).
  - A themed select squeezed below its own height (its inner block and padding taller than its content box) has its
    label, and its baseline in a line, a device pixel lower than Chromium's: the clamped inner block with its padding
    above puts the line a pixel lower than Chromium does.

## The target's format (2026-11-15, the branch `gm1-start-grey`)

**What a layer reads from its target is the target's colours, whatever its pixel format.** Found by GM1 step 11: in the
real program the start screen's whole dialog was drawn grey whenever Continue was greyed, while its fixture passed
against Chromium.
- **Where the canvas reads its target.** A layer that starts from its backdrop (`LayerOptions.StartsFromBackdrop`: an
  opacity, filter or blend layer whose effect holds LCD text, cc's `SaveLayerAlpha` with `kInitWithPrevious |
  kPreserveLCDText`) begins as a copy of the pixels under it (`CanvasRender.CopyBackdrop`), and a backdrop blur copies what
  is under it before blurring (`BlurBackdrop`). Both copied with `vkCmdCopyImage`, which copies bytes.
- **The program's target isn't the tests'.** A program draws into its window's swapchain image, B8G8R8A8_UNORM where the
  surface offers it (`Swapchain.PreferredFormat`, the first choice of `ChooseFormat`; so on the reference PC, on the GPU
  and on lavapipe). The canvas's layers are R8G8B8A8_UNORM (`LayerFormat`), and every test drew into R8G8B8A8 images. From
  a BGRA target into an RGBA layer a byte copy trades red and blue.
- **What it did.** A layer from its backdrop is composited back over the same pixels at its opacity, so where it draws
  nothing the result was opacity × the swapped backdrop + (1 − opacity) × the backdrop. The greyed Continue's layer
  (`opacity: .5`, LCD text kept) spans the canvas, clipped by the dialog's overflow, so the whole dialog came out with
  R = B = (R + B) / 2, which looks grey: its ground (37, 28, 22) → (29, 28, 29), the left-out line's gold (226, 193, 130) →
  (178, 193, 178); Continue itself, drawn in the layer, kept its colours. A layer of opacity 1 from its backdrop comes
  out fully swapped, and so did every backdrop blur: behind each game window's darkened ground the page had red and blue
  swapped (the light theme's parchment (70, 61, 48) as (50, 61, 66); the old game's combat screen, were it in a window,
  (192, 165, 119) as (119, 165, 192)).
- **Chromium.** Skia makes a layer of its parent device's colour type (`SkCanvas::internalSaveLayer`,
  `image_filter_color_type`: RGBA_8888 and BGRA_8888 are kept) and, for `kInitWithPrevious` and a backdrop filter,
  *draws* the prior device into the new one (`internalDrawDeviceWithFilter(priorDevice, newDevice, …)`), so the layer
  holds the backdrop's colours in any format.
- **The engine now** (`CanvasRender.CopyColours`): images of the same format copy their bytes as before (every harness's
  render is unchanged), others are blitted (`CommandList.BlitImage`: `vkCmdBlitImage` one to one with the nearest texel,
  each texel's colour converted; exact between 8-bit UNORM formats; B8G8R8A8 and R8G8B8A8 UNORM have `BLIT_SRC` and
  `BLIT_DST` in Vulkan's required format support). A swapchain image is copied from only when the surface allows it as a
  transfer source (`Swapchain` asks for that use only where supported); the canvas doesn't check it (unchanged; the
  PC's surfaces offer it; Engine-Plan.md, "Small tasks left from earlier milestones").
- **Why every comparison passed:** a byte copy between images of one format is exact, and every test drew into R8G8B8A8.
  No test drew a document the way a program does: into a swapchain image, through the window's host, captured by the
  application.
- **The gap closed.** The comparisons with Chromium of the documents a program shows in its window (the game's screens,
  the editor's screens, the splash, the credits and the crash window) now draw into `Swapchain.PreferredFormat`; and
  real windows are compared with the harness: `Ui/ProgramPathTests` (the engine's: a dialog with a greyed button and a
  blurred window ground, each drawn by a real application in a hidden window, through its `UiHost` into the swapchain
  image and saved by its capture, against the R8G8B8A8 harness, pixel for pixel), `Ui/TargetFormatTests` (both copies
  in both formats, a bounded layer across two grounds), and the game's `Ui/ProgramPathScreenTests` (every game-screen
  fixture at 1 through a real window, pixel for pixel against `GameScreenTests.Render`).
- **Every program's documents, the program's picture before (`main` at `b357aef`) and now** (the same fixtures drawn in
  B8G8R8A8 and R8G8B8A8; now both are the harness's picture): the game's screens 110 of 124 renders changed (all but the
  page, the tooltips and `start-continue-*`), the start screens with Continue greyed most, by up to 79 levels; every
  window's blurred ground by 5–7 levels in the dark theme and 14–35 in the light; the editor's 48 renders, the sandbox's
  page and About unchanged; the old game's states, shown by no program, 20 of 22 (all but `game-sheet`). The full
  table: `Docs/Game/Foundation.md`, "How it works", "The start dialog drawn grey".

## Visual rects tightened to one plain fill (GM2 step 12, 2026-11-15)

Found by GM2 step 12 (`Docs/Game/Hero.md`, "How it works", step 12): seven more sound effects made the game's
`audio/effects` folder overflow the editor's entry list in `preview-effect`, so at 1.65 the list scrolls composited, its
contents an overlap layer over its scrollbar, and the status line under it, whose top is at 1014.75 device px where the
list ends, became an overlap layer of its own in the engine, not opaque, with grayscale text; Chromium keeps it in the
page's layer with LCD text (its traces, main frame and iframe alike). 1913 pixels of text, 3489 in all (0.20 %, over the
editor fixtures' limit). Ruled out first: the overlap test (`GeometryMapper::MightOverlapForCompositing`, ported as
Chromium's), the paint order, composited scrolling (both sides composite it), `PendingLayer::Merge`'s scroll-range cases
(the list's scroll is composited). Chromium's own experiments on the page (the layer probe, main frame, at 1.65): the line
with a border alone, no background, no overflow clip, a 6 px border: no layer; with a background and the border: an
overlap layer; a background with its top at a whole pixel: none; the line overlapping the list by 30 px: a layer.
- **The rule (Chromium 152, saved in `~/.cache/stat-fick-build/reference-src/chromium-152-source/`):**
  `graphics/drawing_display_item.h` 80–100, the `DrawingDisplayItem` constructor: when the record has exactly one op
  (`ShouldTightenVisualRect`), the item's visual rect is `TightenVisualRect(visual_rect, record)` (`drawing_display_item.cc`
  285–321): if the op is a paint op with flags that are a draw-area analysis candidate (`IsDrawAreaAnalysisCandidate`,
  66–82: fill style, no looper, colour filter or image filter, blend src or src-over, and no shader), a DrawRect takes its
  rect enclosed (`ToEnclosingRect`), a DrawIRect its rect, a DrawRRect its rrect's rect enclosed; anything else keeps the
  visual rect it was given. Chunk bounds are the union of the items' visual rects and hit test rects
  (`paint_chunker.cc` 90–110), pending layers start from them, and the overlap test reads them; and the item's
  known-opaque rect is clipped by its visual rect (`CalculateRectKnownToBeOpaqueForRecord`'s `clip_rect`). So the status
  line's border-top, one FillRect of the snapped side from 1015 (`box_border_painter.cc`: one visible side, solid, opaque,
  on a square box takes the general path, `PaintSide` and `DrawSolidBoxSide`, one `FillRect`), doesn't reach the list's
  container rect (to 1015, `ToPixelSnappedRect`, `paint_property_tree_builder.cc` 3461), while the box's ink overflow,
  enclosed, starts at 1014.
- **Which ops Blink records** (the engine's drawings in its terms, for the rule): a colour fill (`FillRect`, a background
  colour alone, square or rounded) is one plain DrawRect or DrawRRect; a gradient or an image is one op with a shader; a
  background's colour under its image is two; a border (`BoxBorderPainter`): with every visible side one colour and style,
  solid, the fast path (`PaintBorderFastPath`, 1276–1331) draws four sides as one stroked rect or ring and fewer sides in a
  translucent colour on a square box as one path (neither a plain fill); otherwise the general path (1470–1495) clips a
  rounded box first and draws each side, a solid side without a miter as one FillRect; so a border is one plain fill only
  when one side is visible, solid, on a square box, and not taken by the fast path (which takes a translucent one, unless
  another side has a width but no visible colour or style: that makes the colours not uniform); outlines, shadows (a save,
  a clip, a draw with a looper, a restore), text, paths and images are never one plain fill.
- **The engine** (`UI/Paint/PaintTarget.cs`): each display item counts the ops Blink's record of it holds
  (`Item.RecordSize`: every save, clip and restore the item records, `RecordOp`, and each drawing's own count, `Record`'s
  `ops`) and keeps its first op's rect when that op is a plain fill (`Item.FirstPlainFill`, given by `FillRect`, `DrawBox`
  and the scrolling contents' background); `DrawBox` with sides counts a background and a border as above
  (`BorderOps`). An item of one op that is a plain fill takes that rect, enclosed, as its visual rect when it joins its
  chunk (`AddItem`), and its known-opaque rect is that fill's clipped by it. Items whose visual rect was their drawings'
  bounds already had it; the change is for items given a box's ink overflow (box decorations).
- **Its fixture** (`Ui/VisualRectTighteningReferenceTests`, `Reference/visual-rect-fixtures.json`, captured as the main
  frame by `make-scaled-paint-references.sh 1 1 visual-rect-fixtures.json` and `1.65 1 …`): the editor's case made
  small, a top bar, a list that overflows and scrolls and a 25 px status line at the bottom of a 640 px page (at 1.65 the
  line's top at 1014.75, at 1013.75 with a border on it), one fixture per case: the line with one plain fill (its border's
  top side, a background, a rounded background: no layer of its own at either scale), with records that aren't one plain
  fill (a background and a border, a background's colour under a gradient, a gradient, a translucent side: an overlap
  layer at 1.65, LCD where its text is on its opaque fill), and two that the list really overlaps (6 px of it over the
  line: with a border, an overlap layer at both scales, grayscale; with a background whose top, 1014.27 at 1.65, the
  snapped fill rounds down where the box's whole pixels inside it start at 1015: an overlap layer from 1014 whose contents
  are opaque, its fill clipped by its tightened visual rect). The layers (which, their kind, the status line's LCD verdict
  and opaqueness) against Chromium's trace (the probe, 2026-11-15), the pictures at 12 levels / 0.2 %.
- **What moved** (every one of the suite's 379 picture comparisons with Chromium, the full suite on the GPU with the rule
  and without it, each comparison's pixels past its limit's levels logged; the layer lists' tests pass either way but for
  the new fixture's): seven moved, all nearer, none farther; every limit unchanged.

  | Comparison | Pixels past 12 levels, before → after | Of the picture |
  | --- | --- | --- |
  | `editor-preview-effect@1.65` | 3,489 → 1,576 | 0.200 % → 0.090 % (the step's case; the old screen, before the step's seven sounds, 1,568) |
  | `editor-checks-problems@1.65` | 3,677 → 2,331 | 0.159 % → 0.101 % |
  | `editor-references-list@1.65` | 3,350 → 1,910 | 0.145 % → 0.082 % |
  | `editor-form-controls@1.65` | 6,548 → 5,733 | 0.134 % → 0.118 % |
  | `editor-form-controls-end@1.65` | 7,458 → 6,643 | 0.153 % → 0.136 % |
  | `editor-preview-model@1.65` | 2,202 → 235 | 0.126 % → 0.014 % |
  | `ui-tight-border@1.65` (the new fixture) | 1,581 → 28 | 0.227 % → 0.004 % |

  The editor's screens are the status line's: it is the known item "A status line's text grayscale at 1.65 beside a tall
  scroller" (`Docs/Engine-Plan.md`, "Small tasks left from earlier milestones", found 2026-11-07), closed by this rule:
  `checks-problems` gained the 1,346 pixels of its status line, `references-list` the 1,440 and `form-controls` the 815
  the item measured. Not checked: the item beside it, the editor's dialog over a composited scroller (its scene isn't a
  fixture). (The table was made on `8a761b5`; after the rebase onto GM2 step 3a, its figure mode's 84 tests pass with the
  rule and without, their eight renders byte for byte the same: 3D views, not drawn by the painter.)
- **Breaks** (`mutate.py`, `claude-work/gm2/step12/mutations-rule.json`): 9, 6 killed (no tightening; an item of several
  ops tightened to its first; a shader not keeping a fill from being plain; a translucent side not taken by the fast path;
  a background and a border counted as one op; the tightened item's opaqueness clipped by the box's whole pixels instead
  of its visual rect). 3 accounted for: the side's own rect against the box's (the box's hit test rect, its pixel-snapped
  border box, is in the same chunk, so the chunk's bounds can't differ; the drawable bounds only say whether it draws);
  FillRect not a plain fill, and an item's saves, clips and restores not counted (in the suite no item given a box's ink
  overflow holds a lone FillRect, and every item that records a save, clip or restore also draws two ops or more, so
  neither can change a visual rect).

## Three text rules (GM2's engine fixes, 2026-11-17, the branch `gm2-engine-fixes`)

Found by GM2 step 7 (the character sheet's screen against Chromium; `Docs/Game/Hero.md`, "How it works", "The engine
fixes"): in a narrow sheet the word PLATINUM drew broken as PLATINU/M, the numbers of the sheet's stat fields sat 7.5 or
15 px off, and a 40 px heading at `line-height: 1.05` was 41.984375 px high where Chromium's is 42. Ported on their own
branch, merged before steps 7 and 8, which rebase onto it. Every rule from Chromium 152's source (saved in
`~/.cache/stat-fick-build/reference-src/chromium-152-source/`, `fetched-gm2-step7/` and `fetched-gm2-engine-fixes/`).
- **Words break only on a line that can wrap** (`LineBreaker::SetCurrentStyleForce`, `line_breaker.cc`): everything
  about breaking inside words is set only when the style wraps (`auto_wrap_`: white-space normal, pre-wrap, pre-line), so
  under nowrap or pre a word overflows whole whatever overflow-wrap or word-break say. Where it wraps: word-break:
  break-word sets `break_anywhere_if_overflow_` in every mode (a word that doesn't fit breaks, and its letters count apart
  for the min-content size); otherwise overflow-wrap: anywhere does the same, overflow-wrap: break-word only in the line
  breaker's content mode (lines, not the min-content size); word-break: break-all picks `LineBreakType::kBreakAll`, whose
  break iterator (`NextBreakablePosition` with `ShouldBreakAfterBreakAll` and `kBreakAllLineBreakClassTable`,
  `text_break_iterator.cc`) adds an opportunity between two characters whose UAX #14 classes the table allows (after a
  letter, digit, solidus or break-after character: before a letter, digit, opening bracket, prefix sign or break-after
  character; after a closing bracket or brace: before a letter, digit or prefix; after an exclamation or postfix sign:
  before a letter, digit, prefix or postfix; after an infix stop: before a letter or digit; prefix before postfix;
  hyphen before digit; '+' read as a letter); keep-all keeps ideographs together. The engine (`UI/Layout/InlineLayout.cs`,
  `AddClusters` and `MarkBreaks`): pieces carry `Anywhere` (lines), `AnywhereMin` (the min-content size) and `BreakAll`,
  all from a style that wraps; `word-break` is a property of its own (`ComputedStyle.WordBreak`; it was folded into
  overflow-wrap, so a later `overflow-wrap: normal` cancelled `word-break: break-word`); `overflow-wrap: break-word` is
  its own value (`OverflowWrapping.BreakWord`; it was read as anywhere); break-all's table for the classes of the
  scripts the game uses (`BreakAllClass`: letters, digits and the ASCII punctuation; anything else adds no opportunity);
  keep-all is normal here (the engine breaks no ideographs).
- **A number field's spin button keeps its room** (`TextFieldInputType::CreateShadowSubtree`, `text_field_input_type.cc`:
  every steppable field gets the button, the last flex item of its decoration container, beside the flexible editing
  view port; `html.css`: `::-webkit-inner-spin-button { appearance: auto; display: inline-block; flex: none }`, its
  `-internal-auto-base()` branch only under the experimental AppearanceBase; `LayoutTheme::AdjustAppearanceWithElementType`,
  `layout_theme.cc`: a number field's `textfield` is its auto look anyway; `LayoutTheme::AdjustInnerSpinButtonStyle`: width
  and min-width the theme's 15 px times the zoom): the button's look is its own, so the field's appearance (auto,
  textfield, none, menulist-button, `-webkit-appearance`; `-moz-appearance` isn't Blink's) changes nothing. The room is the
  button's margin box: the theme's width whatever the author's (an author width under the button's own appearance: none,
  auto then none), its paddings and borders (inside the width under box-sizing: border-box), its margins (auto 0); display:
  none takes the button away. The theme paints it in its border box (nothing under its own appearance: none). The old
  game's stat inputs (`appearance: textfield`, the button's margin 0) keep their 15 px. The engine
  (`UI/Paint/UiPainter.Controls.cs`, `SpinButtonOf`: the box and the room) dropped its rule that a field's textfield or
  none look had no button; the editor's width (`EditorOf`, `FieldGeometry.EditorRects`), hit testing and painting read it.
- **A number line-height keeps its exact value** (`StyleBuilderConverter::ConvertLineHeight`,
  `style_builder_converter.cc`: a number is kept as `Length::Percent(ClampTo<float>(number × 100.0))` from the parsed
  double; `ComputedStyle::ComputedLineHeightAsFixed`, `computed_style.cc`: the percentage of `ComputedFontSizeAsFixed`
  through `MinimumValueForLength`, `length_functions.cc`: a LayoutUnit times the percentage over 100 in float, truncated;
  `ComputedLineHeight`: the same of `LayoutUnit(ComputedSize())`; `CSSToLengthConversionData::LineHeightSize::Lh`: the lh
  unit, `ComputedLineHeight` unzoomed by the font's zoom and zoomed again; `ComputedStyleUtils::ValueForLineHeight`'s
  `FloatValueForLength` for a float). The engine kept the number as a float (1.05 is 1.0499999523, its percentage
  104.99999, so a 40 px heading's line 41.984375; of the old game's numbers 1.05 and 1.06 lose so, on font sizes that are
  multiples of 5 and 25); now `LineHeight.FromNumber` keeps the percentage from the parser's double
  (`ValueReader.TryNumberValue`) and `LineHeight.Resolve` is `MinimumValueForLength`. Its readers: line boxes
  (`InlineLayout.MetricsFor`), controls' lines (`ReplacedContent.CharacterMetrics`: a text field's editor line, the spin
  button's height, a text area's rows), a text field's inner editor rule (`ReplacedContent.LineMetrics`: its line-height
  normal when the font size reaches the int `ComputedLineHeight`; a 20 px field at 1.05 keeps its 21 px line, 3 px
  shorter than normal), the lh unit (`ComputedStyle.LineHeightForUnits`, both places the style engine sets it) and
  `ComputedStyle.ToTextStyle`. (A calc() number line-height still goes through the calc evaluator's float.)
- **Its fixtures** (`Ui/TextRulesReferenceTests`, `Reference/text-rules-fixtures.json`, three Chromium 152 main-frame pages
  captured through `Tools/PaintReference` with the flags of `make-scaled-paint-references.sh`, inside a headless cage, at
  1, 1.25, 1.5, 1.65, 1.75 and 1.65 with a page zoom of 1.2): `textrule-nowrap` (the four properties under nowrap and pre,
  a nowrap span in a breaking block, the same where lines wrap, the two properties against each other, keep-all, the
  min-content sizes and a flex item's automatic minimum, break-all in lines and pair by pair in 27 min-content boxes),
  `textrule-spin-room` (23 number fields: the field's appearance values, the button's appearance none, display none,
  margins, paddings, a border, border-box, author widths, the game's rule, buttons painted by an author opacity, a
  focused field), `textrule-line-height` (40 px headings at 1.05, 20 px at 1.06, the font shorthand, an inherited number,
  the lh unit, text fields and an empty text area at 1.05). Every box Chromium's to 0.002 device px at the six
  configurations, the pages at 1 and 1.65 within 12 levels and 0.2 %, each spin field within 60 pixels past 12 levels, and
  the text style's line height Chromium's 42 px. Sizes multiples of 100 px (whole device pixels at 1.65 × 1.2 too).
- **Shaped around differences that aren't these rules** (found here, left as they are; the manager's word, 2026-11-17):
  the min-content words of `textrule-nowrap` measured letter by letter have their widest letter first ("MILL", not
  "GOLD"); its break-all lines break only between letters that don't kern; the spin page's focused field has no focus
  ring; the text area is empty. Each below, under "Left as they were".
- **What moved** (the full suite on the GPU with the three rules and without them, on `3c22527` with the branch's tests,
  every one of its 385 picture comparisons with Chromium and its 80 layout comparisons with a loose limit logged by a
  scratch line, every render kept; after the rebase onto GM2 step 4a's `86c75c5`, the editor's and the UI's 1,780 tests
  again both ways, step 4a's 18 editor goldens among them): no layout comparison moved (each box and line the same to
  the 1/1000 px logged, the tight ones passing both ways), no editor screen and none of the port's game screens moved (not
  a byte of their renders), and the old game's screens (`game-*`, `Ui/GamePaintTests`, the old stylesheet) moved, all
  nearer to Chromium, none farther, every limit unchanged. The cause is the second rule: the sheet's defence bonus fields
  (`.defense-bonus-input`, `appearance: textfield`, the number at the end) had their "0" at the field's edge, now 15 px in
  as Chromium draws it; in the screens that show the sheet under a dimmed overlay it is a few levels only.

  | Comparison (1× and 1.65) | Pixels past 24 levels (the limit's), before → after | Past 12 levels, before → after |
  | --- | --- | --- |
  | `game-sheet` | 884 → 781; 1,925 → 1,696 | 2,375 → 2,263; 5,120 → 4,874 |
  | `game-settings` | 18 → 18; 111 → 111 | 176 → 170; 906 → 877 |
  | `game-passives` | 76 → 76; 156 → 156 | 108 → 100; 214 → 185 |
  | `game-skills` | 50 → 50; 156 → 156 | 70 → 62; 212 → 183 |
  | `game-classes`, `-inventory`, `-map`, `-shop`, `-spellbook` | unchanged | unchanged; in the fields' strip the pixels past 3 levels 386, 488, 21, 144, 488 → 0, 0, 0, 2, 0 at 1× (1,018, 1,260, 16, 310, 1,260 → 0, 0, 0, 11, 0 at 1.65) |

  Without the rules the new fixtures fail 17 of their 28 tests (every box of `textrule-nowrap` and `textrule-line-height`
  at every configuration, the nowrap and spin pages' pictures, each spin field); with them all pass.
- **Breaks** (`mutate.py` under the lock, `claude-work/gm2/engine-fixes/mutations.json` and `mutations-rerun.json`, the
  new tests' class): 79. The first rule's 54: the nowrap gate for lines and for the min-content size; each property's
  part in each mode (anywhere, break-word, word-break: break-word; the min-content size reading the lines' flag);
  break-all off and its flag never set; each class of each row of its table taken out (19), each row's first classes
  taken out of the row (7), each character class unclassed (11), '+' and '&' not letters; the four values parsed wrongly,
  break-word read as anywhere (the old mapping). The second's 15: the old rule (no button under textfield or none) and
  under textfield alone; display: none ignored; the theme's width under the button's appearance: none, no author width
  there, the author's width over the theme's; borders, paddings, box-sizing, each margin not counted; the box placed
  without its right margin; the editor's clip and the field's text less the box instead of the room; the theme painted
  under the button's appearance: none. The third's 10: the number's percentage from its float, the parser's number as a
  float, each reader through the float (line boxes, controls' lines, the inner editor's rule, the lh unit, the text
  style), the lh unit as the old reader, `Resolve` truncating nothing, the text style reading the percentage as the
  number. **78 killed, 1 that can't differ:** the text style's line height through the number's float (its only
  difference is a float's last step, 21.1999989 against 21.2000008 px for 20 px at 1.06, the same 42 px for 40 px at
  1.05; Chromium shows its own only as a computed value serialized to "21.2px", and no code of the engine reads it).
  The first run's other survivor, the editor's clip less the button's box instead of its room, was a gap: the spin page
  gained a long number clipped at the editor's end beside a button with 12 px margins, and it is killed. Nothing written
  to the player's real folders.
- **Left as they were** (differences from Chromium found beside the three rules; known differences, not fixed here, the
  manager's word of 2026-11-17; also listed in `Docs/Engine-Plan.md` with the small tasks left):
  - *Min-content words after an item's first:* Blink measures the min-content size word by word from the item's shaping
    (`LineBreaker::HandleTextForFastMinContent`, `FastMinTextContext::Add`, `ShapeResult::CachedWidth`, `shape_result.cc`:
    the difference of two character positions, each the run's advance up to it ceiled to a LayoutUnit,
    `ComputePositionData`); the engine ceils each word's own width (`InlineLayout.IntrinsicWidth`, `CloseItem`). A word
    after the first can come out 1/64 px wider: "GOLD" letter by letter (overflow-wrap: anywhere) 12.8125 px where Chromium
    has 12.796875 at 1× (1/64 device px at every scale), a box's width only.
  - *Kerning across a break inside a word:* Blink shapes the end of a line that breaks where it isn't safe to break
    again (`ShapingLineBreaker::ShapeLine`, `shaping_line_breaker.cc`: from the last safe-to-break offset to the break), so
    the kerning to the next letter goes; the engine keeps the cluster widths of the whole word.
    break-all's "12ab.c|d(e)f%…" at 50 px: the first line 46.78125 px, Chromium's 46.890625 ("12ab.c" alone), 7/64 px.
  - *The focus ring at 1.65:* a bordered number field with no appearance at 220 px, focused: its ring's top and bottom
    rows a fraction of a device pixel from Chromium's (`OutlinePainter`, `outline_painter.cc`; the engine's
    `UI/Paint/UiPainter.Outlines.cs`), 615 pixels past 12 levels in that field's box; at 1× it matches. Cause not sought.
  - *Percentage line-heights:* `ConvertLineHeight` makes a percentage a fixed length from the font size times the
    percentage *as an int* (`ClampTo<int>`), so 150.5 % is 150 %; the engine resolves the exact percentage. 150.5 % at
    16 px: Chromium 24 px, the engine 24.078125; 133.9 % at 20 px: 26.59375 against 26.78125. The game's sheets use none.
  - *A number field's own width:* with a finite min and max (and a step that isn't any) Blink sizes it to the digits of
    min, max and step plus the spin button (`NumberInputType::GetSizeWithDecoration`, `number_input_type.cc`;
    `TextFieldIntrinsicInlineSize`, `layout_box.cc`); the engine gives every field 20 characters
    (`ReplacedContent.TextFieldSize`). `min="1" max="50" step="1"` at 16 px Lato: Chromium 62 px, the engine 209 (147
    more). The old game's combat quantity and companion resource fields have both; GM2's screens none. **An engine task
    before the first screen with such a field** (the manager's word, 2026-11-17; `Docs/Engine-Plan.md`, the small tasks).
  - (Known before: a text area lays its text out at the normal line height whatever its line-height,
    `Docs/Editor.md`, step 11.)

## Thin rounded borders (GM2's engine task for step 8's race strip, 2026-11-18, the branch `gm2-round-borders`)

Found by GM2 step 8 (the race carousel; `Docs/Game/Hero.md`, step 8): the strip of 28 small medallions under the stage
(circles of 30 and 38 px, `border: 1px solid` over a fill and an inset shadow, `border-radius: 50%`, the enemy-only ones at
`opacity: .8`, the shown one with a 2 px ring of `box-shadow`, on the dialog's grain) differed from Chromium all along its
rims: Dev Mode's light fixture at 1 had 3,337 pixels past 12 levels (the limit 2,579), 2,746 of them in the strip's band.
Every rule from Chromium 152's and Skia's source (Skia at Chromium 152's pin; saved in `~/.cache/stat-fick-build/
reference-src/skia/`, among them `SkScan_Antihair.cpp`, `SkScan_Hairline.cpp` and `SkScaleToSides.h` fetched for this, and
`chromium-152-source/`, `offset/` and `round-borders/`: `float_rounded_rect.cc/.h`, `contoured_rect.cc/.h`,
`border_edge.cc/.h`). A Python model of Skia's hairline (`claude-work/gm2/round-borders/scripts/hairsim.py`) drew
Chromium's rings to 2 levels before the port.
- **A border Blink draws as one ring is a stroke, and Skia draws a stroke one device pixel wide as a hairline.**
  `BoxBorderPainter::PaintBorderFastPath` (`box_border_painter.cc` 1276–1331): with every side visible in one colour, of
  one style that is solid (`BorderEdge::EffectiveStyle`: a double under 3 device px, a ridge or groove of 1 is solid) and
  an inner edge that is renderable, a rounded box's border is `DrawBleedAdjustedDRRect` (282–318): an inverse fill of the
  inner rrect when the background is clipped as a layer, otherwise `GraphicsContext::FillDRRect` (`graphics_context.cc`
  792–870), which strokes the outer rrect inset by half the width (`SkRRect::inset`) when the pair is simple
  (`IsSimpleDRRect`: one width all round, and at each corner a circle's radius whose inner radius is the outer one less
  the width, or both square; inner radii are the outer ones less the widths, kept at zero by `gfx::SizeF`), and else fills
  the even-odd path of the two. `SkCanvas::onDrawRRect` (`SkCanvas.cpp` 2167) draws an oval as `drawOval`; `skcpu::Draw::
  drawRRect` and `drawPath` (`SkDraw.cpp` 849–1080) draw a stroke as a hairline when the matrix keeps it at most one pixel
  wide along both axes (`DrawTreatAAStrokeAsHairline`, `fast_len`), a narrower one with the paint's alpha scaled by its width
  (`modifyPaintForHairlines`); a square rect never (`drawRect` frames it, `AntiFrameRect`: the band). A 1 px border is one
  device px at every scale under 2 (`ClampLineWidth`), so such a ring is a hairline at 1, 1.25, 1.5, 1.65 and 1.75. The
  hairline (`SkScan::AntiHairPath`: `hair_path` with butt caps; `do_anti_hairline`): the path `SkPath::Oval` (clockwise from
  the right end's middle) or `SkPath::RRect` (from the left side's lower end) makes; each quarter conic cut into 2^n quads
  within a quarter pixel (`SkConic::computeQuadPOW2`, `chopIntoQuadsPOW2`), each quad into 2^level lines
  (`compute_quad_level`, `hair_quad`); every line in 26.6 fixed point, a column (or row) at a time two pixels across it split
  by the fraction of its 16.16 position, its end pixels weighed by its part of them; every blit one src-over, so a pixel two
  lines share is covered twice. The canvas drew the ring as the band between the two rounded rects (distance ramps), which
  spreads it over two pixels where the hairline puts most of it in one.
- **The background under a rounded border that hides its edge shrinks by half the border** (bleed avoidance,
  `BoxDecorationData::ComputeBleedAvoidance`'s `kBackgroundBleedShrinkBackground`; `BackgroundContouredRectAdjustedForBleed
  Avoidance`, `box_painter_base.cc` 1161–1193, a sixth only for a side whose effective style is double): the painter did it
  for image layers only, and a colour went under the whole border box (a known difference since 2026-11-05: "the fill shows
  in the border's outer edge pixels"), which the hairline now uncovered. (A thin double border shrank it by a sixth: its
  effective style is solid.)
- **An outer shadow under an opaque background is cut out one device pixel inside the box** (`ClipToBorderEdge`,
  `box_painter_base.cc` 175–198; a known difference since 2026-11-05): the shown medallion's ring of `box-shadow` reaches
  under the border's outer pixels; cut at the box's edge, a light seam showed between it and the hairline, which Chromium
  hasn't.
- **A box's radii are constrained to its snapped border box** (`ContouredBorderGeometry::PixelSnappedContouredBorder`,
  `CalcRadiiFor` against the unsnapped size, then `FloatRoundedRect::ConstrainRadii` against the snapped rect): the painter
  fitted them to the unsnapped size, so a radius as large as the box (a pill, 50 %) came out a fraction of a pixel short
  where snapping grew it (24.75 for 25 px), which a hairline shows.
- **The engine.** `Canvas/SkiaHairline.cs`: `SkRRect` (setRectRadii's fitting with `SkScaleToSides::AdjustRadii`, `inset`,
  the type) and the hairline, ported line for line into a coverage mask whose blits compose as src-over of the paint's
  alpha. Two of Skia's branches aren't there, as they can't show: `chopIntoQuadsPOW2`'s pair of lines (a quarter circle
  never cuts so) and `do_anti_hairline`'s halving of a line over 511 px (Chromium rasters in tiles and clips a line to its
  tile first; halved, the halves would share a pixel, which Chromium's pictures don't show). `Canvas.StrokeRoundRect`
  (`Canvas/CanvasHairlines.cs`): the rrect in Blink's units (the canvas's units times its scale, put back on 1/1024 px:
  the floats' noise would move a point across a 1/64 px), its conversion and inset, the hairline test, the transform
  (a translation folds into the rect; anything else maps the path's points, `SkPathData::MakeTransform`); the mask cut into
  16 px tiles, the curved ones in an atlas of the hairlines' own and the straight ones (one value along each row, or down
  each column) as plain pixel boxes merged along their line; a wider stroke, or a rect the inset leaves square, is the band
  as before. The hairlines' atlas is cleared between frames only, once half of it is used (`TrimHairlineAtlas`): the shared
  coverage atlas is cleared when it fills, which spoils the draws made from it earlier in the frame; with the hairlines in
  it, the old game's shop (`game-shop`, its page behind the dialog) filled it and drew stray blocks (44,479 pixels past 12
  levels, within its 3 % at 24 levels). `PaintTarget.StrokeRoundRect` records one op, not a plain fill. The painter:
  `UiPainter.BorderRing` and `IsSimpleRing` decide Blink's ring (`PaintStyledFill`; a clip layer keeps the band),
  `BleedAvoidanceOf` (out of `PaintBoxBackground`, for a box that paints a background) and a colour under a shrinking
  border through `PaintBoxBackground`, `PaintOuterShadows` with the cut's inset (the canvas's outer shadow: `border.x`,
  `canvas.frag`), `BorderRadiiUnconstrained` for the decorations and outlines (their consumers constrain to the snapped
  box; `PaintTarget.DrawBox` records the constrained radii). The same Skia rule's neighbours: a solid outline round rounded
  corners (`PaintSingleRectOutline` is a `BoxBorderPainter`: `DrawOutline`), and the theme's rounded frames
  (`ControlTheme.StrokeRoundRect`, NativeThemeBase's `drawRoundRect`, 1 device px at both scales).
- **Its fixtures** (`Ui/RoundBorderReferenceTests`, `Reference/round-border-fixtures.json`, four Chromium 152 main-frame
  pages captured through `Tools/PaintReference` with the flags of `make-scaled-paint-references.sh`, inside a headless cage,
  at 1, 1.25, 1.5, 1.65 and 1.75): `rborder-circles` (circles of six sizes on a light and a dark ground: a 1 px border
  alone, over a fill, over a fill and an inset shadow, translucent, at opacity .8, with a 2 px ring of `box-shadow`, an
  outline alone and with an offset, a .5 px border, scaled by half), `rborder-radii` (rounded boxes of eight radii, a pill
  and 15 px circles among them, with a fill, without, square, tall, translucent, double, groove and ridge; guards that
  aren't rings: a 2 px border, a 3 px bottom, a side of another colour, a left side alone, a radius under the width, an
  elliptical corner, a translucent border over a fill; an outline round radii that overflow their box; a bar 581 px wide),
  `rborder-strip` (step 8's strip of 28 in Dev Mode, its `races.css` rules as test content, light and dark, without the
  emblems' glyphs), `rborder-theme` (buttons, a text field, a checkbox and a radio). Every box Chromium's to 0.002 device
  px at the five scales, the pages at 1 and 1.65 within 12 levels and 0.2 %, box by box the rule's with no pixel past 12
  levels (at 1 only for the scaled circles and the theme's field, checkbox and radio; the radius under the width, the
  translucent border over a fill and the outline among them) and the other guards within the per-control limit (60,
  `LayoutUnitReferenceTests`'); a pill's known-opaque rect from its constrained radii (`APillIsKnownOpaqueToItsConstrainedRadii`).
  `Canvas/HairlineAtlasTests`: the hairlines keep out of the shared atlas, theirs is cleared between frames only, a long
  straight side takes no room in it, and the draws reproduce the mask (a rounded rect a quarter pixel off the grid, within a
  level). Sizes are multiples of 20 px. **The strip on its page** (the step 8 fixtures' numbers in brackets): light at 1,
  2,715 pixels past 12 levels → 11 (2,746 in step 8's band); dark at 1, 1,927 → 29 (1,977); light at 1.65, 3,359 → 74
  (3,133); dark at 1.65, 1,156 → 65 (1,072). What is left is the shown medallion's ring of shadow (its outer edge, Skia's
  curve fill: below).
- **What moved** (the full suite on the GPU on `main`, `c043ad3`, and with the change, every picture comparison's pixels
  past its limit's levels and past 12 logged by a scratch line, every render kept): of the 419 comparisons with Chromium
  that both runs make, 263 moved, 262 nearer Chromium and one farther; no limit, tolerance, mask or reference changed, and
  no golden of the engine's own pictures moved (the ten compared at 1–4 levels).

  | Comparisons | Moved | Pixels past 12 levels, summed, before → after |
  | --- | --- | --- |
  | The UI fixtures (`ui-*`, `input-*`; the focus rings' fixtures by the theme's frames) | 47, all nearer | 24,838 → 14,790 |
  | The editor's screens | 44, all nearer | 31,256 → 29,016 |
  | The old game's screens (`game-*`) | 22, all nearer | 31,903 → 24,569 |
  | The port's screens (`game-screen-*`; the sheet's among them) | 150: 149 nearer, 1 farther | 56,894 → 39,282 |

  The most: `ui-shadow-rotate` 2,771 → 752 at 1 and 4,545 → 1,219 at 1.65 (the shadow's cut), `game-sheet` 2,263 → 1,098
  and 4,874 → 2,531 (past its 24 levels 781 → 545 and 1,696 → 1,103), the port's sheet (`sheet-played-light` 848 → 214 at
  1, `sheet-dev-light` 2,888 → 1,676 at 1.65), `game-map` 1,516 → 482 at 1.65. The one farther: `settings-mods-dark` at 1,
  663 → 677, where the theme's checkboxes are light and Chromium's dark (below): their rounded frames' corners now
  Chromium's hairline over the wrong fill. Step 8's screens aren't on `main`; its strip is the `rborder-strip` page.
- **Breaks** (`mutate.py` under the lock, `claude-work/gm2/round-borders/mutations.json` and its reruns): 46. Killed 38: in
  the hairline every line orientation's two pixels swapped, a line's first pixel whole and its last part pixel left out,
  quads cut once more, conics within half a pixel, a cut conic's weight kept, blits not composited, the coverage not over
  the paint's alpha, an rrect's sides left out, the inset not taking half the stroke off the radii; in the canvas a 1 px
  stroke not a hairline, a narrower one's alpha kept, the shared atlas, the hairline atlas never cleared, straight pieces
  into the atlas (rows, columns), runs not merged, a run's coverage left out, the theme's frames as the band; in the
  painter no ring, a ring under a clip layer, a colour not shrunk, bleed avoidance without a background, sides of other
  colours or widths a ring, a thin double or a 1 px groove not solid, a corner tighter than the width a ring, an opaque
  box's shadow cut at its edge (in the painter and in the shader), radii constrained to the unsnapped box (decorations and
  outlines), the records' radii unconstrained (both boxes), an outline never a ring, a thin double's background shrunk by a
  sixth. **8 standing:** three move a point by a 1/64 px at most, which no pixel shows past 12 levels (4 levels a blit):
  the 26.6 points rounded instead of truncated, the first column's y not rounded, Blink's units off the 1/1024 grid (the
  float noise of the canvas's units); three can't happen to a rounded rect: a cut quarter circle's y order (its control
  point stays between its ends), radii scaled to fit after the inset (the canvas fits them first, and the inset keeps them
  fitting), the inner edge's renderability (it holds whenever the outer radii fit); one draws the same: an oval as
  `SkPath::RRect`'s path instead of `SkPath::Oval`'s (the same four quarter conics; the rrect's lines have no length); and
  one gap left: an elliptical 1 px border drawn as a hairline moves its guard from 6 to 14 pixels past 12 levels at 1 and
  from 14 to 26 at 1.65, under the guards' per-control limit (an elliptical guard large enough to pass it is past the limit
  already: 12 px / 5 px, 63 at 1.65, Skia's even-odd fill against the band). Nothing written to the player's real folders.
- **Left as they were** (differences from Chromium found beside the rule; known, not fixed here):
  - *Skia's curve fills:* Skia fills a curved edge (a rounded fill, a ring wider than a pixel, a shadow's spread, an
    elliptical border's even-odd path) through quads within a quarter pixel of the curve, the canvas by its distance; a
    curve's edge pixels differ by up to about 0.2 of a pixel's coverage (the shown medallion's 2 px ring of shadow at 1:
    up to 39 levels on a high contrast; the circles page's 2 px rings, avoided; the radii page's guards 0–15 pixels a box).
    The known difference "rounded-corner antialiasing differs from Skia" for everything but the hairline.
  - *A translucent border over a fill:* Blink clips background and border as one layer and fills everything outside the
    inner rrect; the canvas composes the band over the clipped fill (the circles' translucent rows over a fill: 337 pixels
    of a row at 1, avoided; one small guard box kept).
  - *A hairline under a 2D scale at 1.65:* a scaled circle's extreme pixels up to 14 levels (4 pixels); a scaled rounded
    box's straight sides a quarter pixel from Chromium's at 1 too (up to 28 pixels a box, worst 20): the path the model of
    Skia draws there isn't Chromium's, cause not found (the transformed box's own geometry is the suspect). Not in the
    game's screens.
  - *SVG strokes under a pixel:* Skia draws them as hairlines too (the panels' corner brackets in game.css and sheet.css,
    `stroke-width='.9'` at 1); the canvas fills a stroke's polygon. Not measured: on the half pixels the brackets' lines
    lie on, a hairline and a polygon cover alike, so it is their joins and ends (a few pixels a bracket).
  - *Focus rings:* `DrawFocusRingRect` strokes the ring's inner 1 px at 1 as a hairline; routed through the hairline, a
    focused button's ring at 1 came no nearer Chromium's, so it stays the band (and the focus ring's known difference at
    1.65, "Three text rules", stays).
  - *`color-scheme: dark` ignored by the theme:* the game's dark settings and dialogs draw their checkboxes light where
    Chromium draws them dark (`game-screen-settings-mods-dark@1`: about 500 of its 663 pixels past 12 levels, 196 levels
    apart; `color-scheme` is parsed and ignored). **Done 2026-11-22** ("Colour schemes" below).

## Colour schemes (GM2's engine task, 2026-11-21 and 22, the branch `gm2-color-scheme`)

**The engine honours `color-scheme` as Chromium 152 does** (listed as "the theme ignores `color-scheme: dark`" by the
round borders' task: the game's dark screens drew their checkboxes and the theme's other parts light). The game asks for
it in one place, `html.dark-mode { color-scheme: dark; background: var(--paper); }` (`Assets/UI/styles/game.css`, as the
old game's `old-game/styles.css` line 34); the light theme leaves it `normal`, and the editor never sets it (its
convention 2). Every rule from Chromium 152.0.7977.89's source (the reference PC's Electron is .130, whose `html.css`
is the same bytes), saved in `~/.cache/stat-fick-build/reference-src/chromium-152-source/color-scheme/` (and `early/`,
the colour mixers), and every value checked against the reference PC's Chromium.

- **The property** (`longhands_custom.cc`: `ColorScheme::ParseSingleValue`, `ApplyColorSchemeValue`;
  `ComputedStyleBuilder::SetUsedColorScheme`, `computed_style.cc`): `normal`, or idents (`light`, `dark` in lower case, a
  custom ident as written, never a CSS-wide keyword or `default`), with `only` once, first or last, moved to the end;
  inherited (`ComputedStyle.ColorScheme`, the list, and `DarkColorScheme`, the used scheme, both copied to children).
  The used scheme is dark when `dark` is listed and either `light` isn't or the preferred scheme is dark; `normal` takes
  the page's schemes, which only a `<meta name="color-scheme">` gives: the engine's documents have no head, so it is
  light. Forced darkening isn't the engine's. Computed first of all (Blink's priority 2), before `font-size` and `color`.
- **The preferred scheme** (`prefers-color-scheme`, and the scheme of `color-scheme: light dark`):
  `MediaEnvironment.PrefersDark`, from `PlatformBehaviour.PrefersDarkColorScheme`, light unless set (the engine doesn't
  read the desktop's); the tests act as the reference PC, whose Chromium prefers dark (`ReferencePlatform`). The media
  feature was unknown to the engine (false either way); a change of the preference restyles the document.
- **Colours** (`StyleBuilderConverter::ResolveColorValue`, `style_builder_converter.cc`): system colours, `light-dark()`
  (CSS Color 5, `css_parsing_utils`' `ConsumeLightDark`: two colours, nested anywhere a colour goes, color-mix()
  included) and Blink's `-webkit-link`, `-webkit-activelink` and `-webkit-focus-ring-color` resolve at computed-value time
  against the element's own used scheme, and inherit as the colour they gave (a light child of a dark parent keeps the
  parent's white `CanvasText`). `ValueReader.TryColor(current, dark, …)`; `SystemColors` holds both schemes' values as the
  reference PC's Chromium resolves them through its colour provider (`LayoutTheme::SystemColor`,
  `color_provider_utils.cc`'s `CompleteDefaultCssSystemColorDefinition`; `DefaultSystemColor` for those it has no
  provider colour for): Canvas #ffffff/#121212, CanvasText, ButtonText, FieldText, ButtonBorder #000000/#ffffff (the
  provider's button text: the engine's light #767676 was `DefaultSystemColor`'s, wrong), ButtonFace #efefef/#6b6b6b, Field
  #ffffff/#3b3b3b, LinkText and `-webkit-link` #0000ee/#9e9eff, VisitedText #551a8b/#d0adf0, `-webkit-activelink`
  #ff0000/#ff9e9e, SelectedItem #1967d2/#99c8ff, SelectedItemText #ffffff/#3b3b3b, the focus ring #e59700/#eeeeee
  (`FocusRingRespectExplicitOutlineColorInDarkMode`, stable); ActiveText, GrayText, Mark, MarkText, Highlight
  (#0041c6cc) and HighlightText the same in both. The root's colour, when nothing sets it, and `color: initial` are white
  in the dark scheme (Blink's `DocumentElementUserAgentDeclarations` and `InitialColorForColorScheme`). SVG paints in an
  element's scheme too. A registered `<color>` custom property still computes in the light scheme (not used by the game).
- **The user agent's colours** (`html.css`, and `linux.css` after it on Linux): fields', text areas' and selects' borders
  `light-dark(#767676, #858585)`; disabled buttons `light-dark()` of their three colours; disabled fields and text areas
  their background and `light-dark(#545454, #aaaaaa)`; a disabled select's colour `light-dark(GrayText, #aaa)`; a
  range's colour `#9d968e` (`linux.css`; not painted) and its background `Field` (unpainted under its look; the engine had
  transparent); a dialog `Canvas` and `CanvasText`; a fieldset's groove `ButtonFace` (was #c0c0c0; `ThreeDFace`, which
  Chromium resolves as ButtonFace); a colour input `ButtonFace`, `ButtonText`, `ButtonBorder`; links `-webkit-link`.
  Placeholders stay #757575 in both (as `html.css`).
- **The native look** (`ThemePainterDefault` gives `NativeThemeBase` the colour provider of `style.UsedColorScheme()`;
  `color_provider_utils.cc`'s `CompleteDefaultWebNativeRendererColorIdsDefinition`, which `ControlTheme`'s dark palette
  already held for the accent's flip): buttons, fields, text areas, menu lists, checkboxes, radios, slider tracks and
  thumbs and progress bars paint the scheme's palette; an accent next to a checked box, a slider or a progress bar flips
  the scheme both ways now (`CalculateColorSchemeForAccentColor`: a dark scheme turns light for an accent under 3:1 with
  #3b3b3b, as a light one turns dark for one under 3:1 with white), and a translucent accent is made opaque over the
  scheme's Canvas (#121212 in dark; `GetAccentColor`). The number field's spin button and every scrollbar are Fluent's in
  the scheme (`fluent_ui_color_mixer.cc`: dark track, corner and arrow buttons #2c2c2c, thumb and arrows #9f9f9f, hovered
  and pressed #d1d1d1; `scrollbar-color` the same in both); a scrollbar's scheme is its box's (`Scrollbar::UsedColorScheme`),
  the viewport's the root's (the LayoutView takes `DarkColorScheme` from it; the root-scrollbar exception for a page of
  `normal` under a dark preference needs the browser's own preference, light in Electron). The search field's cancel
  button takes Chromium's dark images (`IDR_SEARCH_CANCEL_DARK_MODE`, `…_PRESSED_DARK_MODE`, now in `UI/Resources/`).
  The focus ring's outer stroke is #101010 in the dark scheme (`outline_painter.cc`'s `PaintFocusRing`), its inner the
  outline colour (the scheme's `-webkit-focus-ring-color`). The menu list's arrow already took the text colour.
- **The canvas:** under a dark root the page's base is #121212 (`UiDocument.UsedBaseBackground`;
  `StyleEngine::UpdateColorSchemeBackground` and `LocalFrameView::BaseBackgroundColor`: a main frame's base when it isn't
  transparent; an iframe of another scheme than its element's paints the same colour, so the paint fixtures' iframes
  agree). The game's dark theme gives its root a background, so nothing of it shows there.
- **The select's list** (`internal_popup_menu.cc` writes the owner's `DarkColorScheme` into the popup's
  `<meta name="color-scheme" content="only dark|light">`): the engine's `sv-select-popup` gets the select's scheme as
  `color-scheme: only dark` or `only light` in its style. The title tooltip and the validation bubble are the browser's,
  drawn in its own theme, not the page's: unchanged.
- **Not the scheme's, found on the way and left** (each the same in both schemes): a read-only field painted as disabled
  (Blink 152's `GetWebThemeState` counts only disabled; read-only disables only the spin button); inset, outset, groove
  and ridge borders painted solid (Blink shades a side, `CalculateInsetOutsetColor`: a button with its own background keeps
  the user agent's `2px outset ButtonBorder`, black now as in Chromium, which Chromium draws #a8a8a8 and #545454); a
  drop-down select's background `ButtonFace` on Linux (`linux.css`, `select:not(:-internal-list-box)`, which the engine's
  selectors can't say; only a select without its look and without a background of its own shows it); the search field's
  cancel button a device pixel off and filtered otherwise (both schemes: 56 pixels past 40 levels at 1, about 160 at
  1.65, worst 152–217); text areas one pixel narrower from `cols`, a text field from `size` and a fieldset's own width; the
  engine's meter (its own green bar, Chromium's is styled by `html.css`; the game has none) not switched.
- **Its pages** (`Ui/ColorSchemeReferenceTests`, `Ui/ColorSchemeFixtures`, `Reference/color-scheme-fixtures.json`, captured
  through `Tools/PaintReference` with the flags of `make-scaled-paint-references.sh` inside a headless cage, at 1 and
  1.65, each with Chromium's computed values of its elements with an id): `colors-light|dark` (every system colour as a
  swatch, `light-dark()` seven ways (four of them invalid), the root's colour, `color: initial`, a link, a focused field's ring colour, the user
  agent's colours of a dialog, fieldset, disabled field, text area, button, select and a range, the preference's media
  query, `light dark`, 30 values of `color-scheme`'s grammar, a system colour inherited into the other scheme, an SVG's
  system colour and `light-dark()`, the canvas), `controls-light|dark` (checkboxes, radios, ranges, progress bars,
  fields, selects, buttons, text areas, disabled, with accents that flip the scheme and a translucent one),
  `focus-number|button-light|dark` (a focused window's focus rings and spin button), `scrollbars-light|dark` (bars,
  corners, a thin one, `scrollbar-color`, the root's bars) and `nested` (a dark root, a light section, a dark one in it,
  `light dark` and `only light`, with controls and scrollers in each). Within the paint fixtures' limits (12 levels, the
  controls' 40, 0.2 %). The input fixture `scheme-states-dark` (`Ui/InputFixtures.cs`, Chromium's screenshots at 1 and its
  results at 1 and 1.65): a pressed button, checkbox, range thumb and scrollbar thumb in the dark scheme (the harness's
  offscreen page paints no hovered look, Blink's `GetWebThemeState` without a pointer that can hover; its light twin
  isn't there: in a headless cage the harness captures the first fixture of a run at the output's size).
  `Ui/ColorSchemeTests`: the engine's light default for the preference and a change of it (with and without a media query
  depending on it), a scheme change restyling the children and only painting, the select's list in the select's scheme,
  the dark cancel image, the spin button's dark arrows against Chromium's page (their greys within the controls' 40
  levels), the dark base under a dark root and not under a transparent base.
- **What moved** (the full suite on the GPU on `main`, `b8b2e28`, and with the change, every picture comparison's pixels
  past its limit's levels and past 12 logged by a scratch line, every render kept): of the 451 comparisons with Chromium
  both runs make, 6 moved, all nearer, and only their 6 renders differ byte for byte (of 626 kept): every light page, the
  editor's screens, the old game's and every other dark screen of the port (the emblems' sheet and carousel among them)
  the same bytes. No limit, tolerance, mask or golden changed (the two `control-select` screens captured again for their
  list's style came out the same bytes). The dark settings screens' unchecked boxes are now the scheme's #3b3b3b, as
  Chromium's; nothing else of a game screen changed.

  | Screen | At 1: pixels past 12 levels (worst) | At 1.65 |
  | --- | --- | --- |
  | `game-screen-settings-mods-dark` | 677 → 29 (196 → 76) | 1,786 → 162 (196 → 59) |
  | `game-screen-settings-no-mods-dark` | 649 → 1 (196 → 14) | 1,641 → 99 (196 → 44) |
  | `game-screen-settings-dev-mode-dark` | 326 → 2 (196 → 16) | 913 → 113 (196 → 44) |
- **Breaks** (`mutate.py` under the lock, `claude-work/gm2/color-scheme/mutations.json` and its rerun): 77. Killed 71,
  then the 4 gaps they showed closed and killed (a `light-dark()` argument followed by more, a change of the preference
  where no media query depends on it, the spin button's arrows, a pressed scrollbar thumb). **2 standing**, both unable to
  differ: `color-scheme` computed again in the loop after its early place (the same value twice), and the used scheme left
  out of `AffectsChildrenEquals` beside the computed list (the used scheme changes only with the list or with the
  preference, which restyles everything). Nothing written to the player's real folders.

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
  outer shadows' reach of 1.5 × blur + spread, + outline), not its drawings (unless its record is one plain fill of a
  rect: "Visual rects tightened to one plain fill", 2026-11-15); items that draw nothing
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

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

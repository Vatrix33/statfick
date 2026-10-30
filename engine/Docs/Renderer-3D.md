# 3D renderer (M6)

The 3D renderer draws the game's Blender models (characters, enemies, items), the battle stage and
the map terrain in the game's ink style: cel shading in palette colours, ink lines, hatching in
shadow, glow, cast shadows drawn as ink. It also renders 3D views into the UI (portraits, previews).

## Decisions (user, 2026-10-30)

- **Reference for the look:** Blender renders of the same test models (EEVEE, cel shading in the
  game's palette, Freestyle ink lines, as in the lizardman test). The engine's renders are compared
  with them the way the UI is compared with Chromium: geometry, shading bands and colours closely,
  ink lines with a tolerance (Freestyle draws lines differently from a real-time renderer).
- **Ink lines** (outlines and inner edges): a fixed width on screen, times the display's pixel
  density, whatever the model's size or zoom.
- **Shadows:** real-time cast shadows on the battle stage and the map, drawn in the art style
  (shadowed areas take the darker palette tone and hatching).
- **Map camera:** orthographic, straight down, as the current map (zoom 1–10× of fit).
- **Anti-aliasing:** MSAA 4×.
- **Target:** Vulkan 1.3 GPUs from about 2016 on (GTX 1050, RX 470, recent Intel integrated) at 60
  fps in 1080p.
- **Models:** glTF 2.0 binary (.glb) from Blender; M9 converts them into the packs.
- **glTF reader** (step 2): BRUTAL's glTF package (`Ahwoo.BRUTAL.Gltf`) parses the .glb container
  and its JSON; the engine decodes accessors (component types, strides, sparse) and builds the node
  hierarchy, materials and skins itself. M9's converter reuses it.
- **Cel shading** (step 3): the lizardman test's rule. Two tones: lit is the slot's colour; shadow
  is that colour mixed 42% toward the ink `#130e0b` (in sRGB, as the lizardman script mixed),
  unless the palette gives the slot its own shadow tone. The band edge is where a white diffuse
  surface lit by a sun of strength 3 falls below 0.3; EEVEE's diffuse is strength/π · N·L, so the
  edge is at N·L 0.314, 71.7° from the light. Engine defaults, settable per scene. Band edges are
  antialiased like Blender's render, over about a pixel from how fast the light changes on screen
  (not shaded per MSAA sample).
- **GPU skinning** (step 7): linear blend skinning (glTF's and Blender's default), 4 influences per
  vertex (JOINTS_0/WEIGHTS_0, Blender's export default). A posed mesh's normals are recomputed from
  the bent surface as Blender does (measured: its posed normals are the deformed mesh's own smooth
  normals, each vertex averaging its faces weighted by their corner angles, sharp edges kept); the
  bone matrices would turn them up to about 4° off at a bend.
- **Hatching** (step 6): in every shadow-tone area (a model's shadow side, cast shadows on
  scenery), as the lizardman hatched: lines 6 px apart, 1 px wide, at 72°, 35% toward the ink (the
  old game's battle sprites in a 1080p window), times the device scale. Screen-aligned and moving
  with its model (anchored to the model's origin on screen). A material with `hatch = 0` (glTF
  material extras) has none (the lizardman's crest and blade).
- **Glow** (step 6), marked by the Blender shader: a Principled BSDF with emission is cel-shaded and
  glows in its emission colour (the lizardman's blade); an Emission shader alone is drawn flat in
  that colour and glows (its badge, emblem); a Background shader is drawn flat without glow (its
  pupils; KHR_materials_unlit). The look is the lizardman's halo (`compose.py`): the glowing parts in
  their colours blurred by 8 px with the alpha boosted 2.2×, a 2.5 px blur over it, all behind the
  whole model (around its outline, never over its own parts); sizes times the device scale.
- **Shadows** (step 5): everything casts shadows; only scenery (the battle stage, the map terrain
  and its features; the game marks scenery models) receives them. Characters, enemies and items keep
  the lizardman's shading, without shadows (not even their own). Edges are softened by the sun's
  size (Blender's sun angle: 3° by default, settable) as Blender renders it: across a shadow's
  fringe the two tones blend by the share of the sun that's visible (EEVEE thresholds each sample's
  hard shadow from one point of the sun, then averages them), sharp near the caster and softer
  further away; everything else stays two-tone. (First answered after a wrong description of mine,
  a crisp edge moved inward; asked again.) No ink
  outline on cast shadows. Shadow maps fitted to the visible area (orthographic views of bounded
  areas need no cascades): 16 maps of 1024², one per direction over the sun's disc as Blender renders
  a sized sun (it jitters 64), in one 4096² atlas, 64 MB. (First decided as one 2048² map; a single
  map can't tell "under" from "behind" for floating parts, so its fringes came out wrong.)
- **Ink lines** (step 4): screen-space. After drawing, a pass finds edges per pixel from model and
  object IDs, depth and face normals, then draws each line centred on its edge at the set width,
  with soft edges and round ends; only visible lines (as Freestyle's visible-only linesets).
  - Outline per model: each model (a character with its gear, an item, a piece of scenery) gets the
    thick outline wherever it meets anything else. (The lizardman used Freestyle's external
    contour, which only met empty background because it was rendered alone.)
  - Inner lines, thin, the lizardman's set: silhouettes (one part over another), open mesh edges,
    creases where faces meet sharper than 120° (face normals more than 60° apart). No lines at
    colour boundaries.
  - Widths at device scale 1: outline 3.5 px, inner 2 px (the old game's battle sprites in a 1080p
    window), times the device scale. Ink `#130e0b`.
  - Parts without lines: a custom property on the Blender object, `ink = 0`, exported as glTF node
    extras (the lizardman kept them in a "nolines" collection).
  - Mesh feature edges (user decision during step 7): where a part lies on another (an armour plate
    on a body, a strap, a belt) the per-pixel tests see one continuous surface, but Freestyle inks
    the plate's own creases and open edges. So each mesh's feature edges are found at load and
    inked where they lie on the visible surface (intersections still get none).
- **Colours: palette slots** (step 2). Each Blender material names a slot (`metal.base`,
  `metal.trim`, `cloth`, `skin`, …); the engine fills the slots from a palette when it draws, so one
  sword model shows in every metal and one robe in every cloth, as the old game's palettes (metals
  with base/light/dark/trim/grip, cloths, skin per species) do. A material naming no slot in the
  palette (glows, fixed details) keeps its own base colour.

Already decided on 2026-10-16: characters, enemies and items are real-time 3D models with full
animation sets (animation itself is M7); battles keep the fixed orthographic side view on a 3D stage;
the map is 3D terrain seen from above.

## Method

As with the UI and Chromium:
1. **Reference first.** A test scene (models as .glb, camera, light, palette, line settings) is
   rendered by Blender 5.2 headless (`flatpak run org.blender.Blender -b --python <absolute path>`,
   on the user's PC; scripts in `Tools/BlenderReference/`, a `Tools/Scripts/make-blender-references.sh`
   to run them). The renders and the .glb files are committed as references
   (`Tests/Statventure.Tests/Golden/r3d-*.png`, `Tests/Statventure.Tests/Data/Models/`), so the
   tests run anywhere (the cloud has no Blender).
2. **Port the rule.** The engine reproduces what Blender computes (projection, the diffuse term,
   the colour ramp's bands, Freestyle's line rules) rather than tuning numbers until an image
   matches.
3. **Test against the reference**, at a tolerance only where the two renderers sample differently
   (MSAA 4× edges against EEVEE's filtered samples, Freestyle's stroked lines).

Test scene values (palette entries, light, line widths) are test data, not the game's look; the
game's own palette, light setups, line widths and hatch pattern are asked of the user when they
come up.

## Conventions

- glTF's coordinates: right-handed, Y up, metres; Blender's glTF exporter converts from its Z-up.
- Vulkan clip space (Y down, depth 0..1); reversed Z (1 near, 0 far) with a float depth buffer for
  perspective precision.
- Vertex data pulled from buffers by device address, push constants for per-draw data (as the canvas
  does: `Shaders/Source/common.glsl`).
- Renders go to an MSAA 4× colour and depth target, resolved to a single-sample image the canvas
  can composite (3D views inside the UI) or the window shows.
- Colour targets are sRGB (`R8G8B8A8SRGB`): shaders output linear values, the hardware encodes them
  and averages MSAA samples in linear, as Blender's Standard view transform encodes its linear
  render. The renderer takes linear colours (`DrawItem.Color`, the background), as glTF's
  baseColorFactor is; palettes are written in sRGB hex and converted (`Colors.LinearFromHex`).
- Matrices are System.Numerics' (row vectors, `v * M`). Uploaded as they are, GLSL reads each row as
  a column, so the shaders multiply `M * v`. A System.Numerics row is therefore a column of the
  column-vector matrix, which is also glTF's storage order.
- **Front faces are counter-clockwise** (`VkFrontFace.CounterClockwise`), back faces culled. The
  projection's Y flip makes up point up in Vulkan's y-down framebuffer, so the picture is upright,
  not mirrored, and glTF's counter-clockwise triangles stay counter-clockwise on screen (Vulkan's
  "counter-clockwise" is as seen on screen). Clockwise culled the front faces: with flat colours a
  single box still looked right, only boxes cutting through each other showed it.
- **Mirrored nodes** (a model matrix with a negative determinant, e.g. Blender's scale −1) wind
  their triangles the other way, as glTF says; the renderer draws them with clockwise front faces.
- **Double-sided materials** (glTF's doubleSided, Blender's backface culling off, which is Blender's
  default) are drawn without culling. Their back faces' normals get flipped once shading uses them
  (step 3).
- **Skinned meshes are placed by their joints**: glTF ignores the transform of the node a skinned
  mesh hangs on. Each vertex goes through its joints' inverse bind matrix and joint transform, mixed
  by its weights. Until GPU skinning (step 7) `Model` does this once on the CPU, at rest.
- **Ink lines, how they're found** (`ink_edges.frag`, `ink_lines.frag`):
  - The 3D pass writes a surface record per sample next to the colour (`RenderTarget3D.Surface`,
    R32G32_UINT, MSAA 4×, `mesh_surface.glsl`): the model id (10 bits, per `DrawItem.Group`; ids
    repeat past 1023 models in a view), the part (8 bits, the node), the ink flag, the face normal
    in view space turned toward the viewer (octahedral, 6+6 bits; from the screen derivatives of the
    view position, so it's the triangle's own) and the view depth.
  - Pass 1 decides, for each pixel and its right and lower neighbours (sample 0 of each), whether
    their boundary carries a line: the nearer side owns it and needs ink; another model or the
    background on the other side: the outline; within a model, a break in the surface (neither
    side's depth, carried on from the pixel behind it or along its face plane, reaches the other's
    within two pixels' worth): an inner line; two parts meeting without a break: none (Freestyle
    draws no intersections); within a part, face normals more than 180° minus the crease angle
    apart: an inner line. Faces less than the crease angle apart with one nearly edge-on
    (|N·V| < 0.25) count as continuous: their planes predict nothing. All eight samples of the two
    pixels then place the line: the samples on the first pixel's side, over 4, are where a straight
    edge crosses the middle of the two-pixel strip (a quarter-pixel step).
  - Pass 2 draws each line centred on those crossing points: coverage = clamp(half width − distance
    to the nearest point + 0.5), over a reach of half the widest line plus one pixel (7×7 at 3.5 px),
    blended over the resolved colour with straight alpha in linear.
  - Needs 4× MSAA R32G32_UINT colour attachments that can be sampled (checked: RADV, lavapipe;
    Vulkan 1.3 desktop GPUs have it).
- **Shadows, how they're made** (`Renderer3D.FitShadow`, `mesh_cel.frag`):
  - Draw items carry `CastsShadow` (models: all) and `ReceivesShadows` (`Model.Draw(..., scenery)`).
  - The sun's view: orthographic along the light, fitted to the receivers the camera sees (the
    frustum between the nearest and farthest the scene reaches, in the light's view), widened by
    the largest shear, deep enough for every caster; reversed depth.
  - 16 maps, one per direction over the sun's disc (a Vogel spiral, even over its area), each the
    centre's view sheared by distance (x' = x + u·tan(angle/2)·distance), rendered into the tiles of
    a 4096² atlas (1024² each), casters both sides.
  - A receiver's visibility: in each map its place is its place in the centre's view moved by the
    direction's offset times its distance; the four texels about it are compared and blended
    bilinearly; the point is pushed out along its normal by 1.5 texels. The mean over 16 maps.
  - The tone: the light band gives lit or shadow (antialiased, as before), then blends toward the
    shadow tone by the hidden share of the sun (`mix(shadow, tone, visibility)`): what averaging
    EEVEE's per-sample two-tone results gives.
- **Hatching, how it's drawn** (`mesh_cel.frag`): the model's origin (`DrawItem.Anchor`) projected to
  pixels anchors lines across the screen at the hatch angle; the nearest line's share of the pixel
  is the exact box filter of a straight band (the pixel square projected across the lines is a
  trapezoid); the colour mixes toward the ink by hatch ink × line share × the pixel's shadow share
  (Blender mixes each sample in shadow on a line, and averages). Materials: `ModelMaterial.Hatch`
  from the `hatch` material extra (read from the glTF JSON: BRUTAL drops extras).
- **Glow, how it's drawn** (`Renderer3D.RenderGlow`): materials carry `Emission` (glTF emissive ×
  KHR_materials_emissive_strength) and `Unlit` (KHR_materials_unlit); `EmissionOnly` is no base
  colour with emission (an Emission shader). Draw items: unlit → flat, no glow; emission only →
  flat in the emission, glowing; otherwise cel and glowing when emissive (`DrawItem.Glow`). Passes,
  between the 3D pass and the ink lines, in images made when a view first glows:
  1. `glow.frag`: the glowing items alone (their own depth test), sRGB-encoded glow colours, and
     each pixel's model and view depth;
  2. `glow_blur.frag`: Pillow's GaussianBlur (BoxBlur.c: an extended box of 2·whole + 1 pixels and
     one more at each end at the fractional weight, three passes per axis, edges repeated) of the
     premultiplied sources, at 8 px and 2.5 px times the device scale (`Renderer3D.BoxOf`);
  3. `glow_owner.frag`: the nearest glowing pixel's model and depth within the halo's reach;
  4. `glow_composite.frag`: compose.py's halo in sRGB (wide blur, straight colours, alpha × 2.2
     capped; the core over it), laid over the view by the share of the pixel's samples that are
     neither its model nor nearer than its glowing parts; then copied back into `Resolved`.
- **Feature edges, how they're drawn** (`Model.FeaturesOf`, `feature.vert/.frag`): at load a mesh's
  primitives are welded by position; every edge with one face (open) or more than two is kept, every
  edge between two faces keeps the cosine of their normals' angle (the crease test is the style's, at
  draw; crease status is taken at rest). Each frame (after skinning) every edge is a thin quad on
  screen; a pixel within 0.75 px of it records the nearest point (its offset), its view depth and
  the edge's part; the depth test keeps the nearest edge. `ink_edges.frag` keeps the point where it
  isn't behind the visible surface (that surface's plane carried to the point, half a pixel's worth
  of depth; two pixels' worth where the surface is seen nearly edge-on) and its part has ink;
  `ink_lines.frag` inks it as an inner line.
- **GPU skinning, how it's done** (`Renderer3D.Skin`, `skin.comp`, `normals.comp`): `Model.Draw`
  takes a `ModelPose` (local transforms for some nodes) and gives each skinned primitive a
  `SkinJob` (its joint matrices: inverse bind matrix, then the joint's posed transform; posed bounds
  for the shadow fit). Before any pass, skin.comp blends each vertex's four joint matrices into a
  per-frame device buffer (one per frame in flight, reset at the frame's first use), then
  normals.comp recomputes each vertex's normal as the angle-weighted sum of the triangles in its
  fan; every pass draws from the posed buffer. Fans: Blender's importer shades a face flat when all
  its normals equal its face normal, and posed such a face keeps its own normal (measured); so a
  skinned primitive's flat triangles get vertices of their own at load (one-triangle fans), and the
  smooth ones share fans by rest position and normal (a seam's split vertices share theirs).
- Cameras keep the vertical extent fixed and widen with the aspect ratio (a Blender camera with
  sensor fit vertical): perspective by a vertical field of view, orthographic by the height of the
  world in view (Blender's orthographic scale). `Camera.View` is `Matrix4x4.CreateLookAt`.

## Reference harness

- **Scenes:** `Tests/Statventure.Tests/Render3D/Scenes3D.cs` (shapes as positions and
  counter-clockwise triangles, objects with scale, rotation X then Y then Z, translation and an sRGB
  colour, a camera, size, background). The tests write them to `Reference/r3d-scenes.json`
  (`Render3DReferenceTests.TheSceneFileIsUpToDate`: shapes, each object's model matrix as four
  columns, the camera).
- **Blender:** `Tools/BlenderReference/render_scenes.py`, run by
  `Tools/Scripts/make-blender-references.sh [scene ...]` (Blender from `$STATVENTURE_BLENDER`, else
  `blender` on the PATH, else the Flatpak; always `-b --factory-startup` and absolute paths, since
  the Flatpak starts in its own folder and has a private /tmp). It converts glTF coordinates to
  Blender's as the glTF importer does (`(x, y, z)` → `(x, −z, y)`), bakes each object's matrix into
  its vertices, builds the camera from the look-at axes (sensor fit vertical, lens from the field of
  view, orthographic scale from the height), and renders with EEVEE: 64 samples, filter 1.0 px,
  Standard view transform, no dither, flat colours as emission of strength 1 on a world background.
  Writes `Golden/r3d-<scene>.png`.
- **Models:** `Tools/BlenderReference/make_models.py` builds the test models in Blender and exports
  them to `Tests/Statventure.Tests/Data/Models/*.glb` (the script is their source; the same
  `make-blender-references.sh` runs it first). `blocks.glb`: a node hierarchy, a mesh with two
  materials, one mesh on two nodes (one mirrored, both sunk into the slab so the winding shows), a
  double-sided banner seen from behind, palette-slot materials (`cloth.001` takes `cloth`) next to
  materials no palette names. `rigged.glb`: a tube on a two-bone armature in its rest pose, on a
  translated rig (so a skinned mesh placed by its node instead of its joints shows). Scenes place
  models (`Scene3D.Models`) and give the palette; Blender reads them with its own glTF importer and
  shows each material as its slot's palette colour or its base colour, keeping its backface culling.
- **Cel shading in Blender** (`render_scenes.py`): model materials become the lizardman test's toon
  material: a white Diffuse BSDF → Shader to RGB → RGB to BW → constant colour ramp (shadow tone at
  0, lit tone from the band edge) → emission; the shadow tone from the palette or mixed toward the
  ink in sRGB. The sun has no angular size and no shadows yet. The world shows the background to
  camera rays only (Light Path "Is Camera Ray"), black to lighting, as the lizardman test had no
  world; ray tracing and fast GI are off, so only the sun lights surfaces. Measured: EEVEE's white
  diffuse under a sun is strength/π · N·L (0.9546 · cos for strength 3). Shapes stay flat emission.
- **Hatching in Blender**: the toon material gets the engine's line pattern from Window coordinates
  (times the render size, Y flipped), anchored at the model's origin on screen
  (`world_to_camera_view`); on a line (distance under half the width) and in shadow (the ramp's
  input under the band edge), a sample mixes toward the ink by the hatch ink. Materials with
  `hatch = 0` (the importer keeps it as a custom property) have none.
- **Poses in Blender**: the test writes each posed joint's deformation in model space (back from its
  rest transform, then its posed one: the same for every skin using the joint) into the scene file;
  `render_scenes.py` sets each bone's pose matrix to that deformation in armature space times its
  rest matrix, parents first. Measured: Blender's posed vertices are the engine's deformation to
  3·10⁻⁷.
- **Glow in Blender**: imported materials are sorted by their shader (an Emission shader without a
  Principled BSDF: unlit; a Principled BSDF with a black base and emission: emission only); unlit
  and emission-only parts become flat emission. After the shading render, two more: the glowing
  parts alone in their glow colours (everything else hidden, film transparent) and the coverage of
  the models they belong to, into `Actual/blender-glow/`. `compose_glow.py` (Pillow) then lays the
  halo into `Golden/r3d-<scene>.png` as `compose.py` did, held back where those models cover.
- **Shadows in Blender**: in scenes with scenery, the sun casts shadows with its angle and
  `use_shadow_jitter` (no overblur): the exact disc light, 64 shadow origins over the samples.
  Scenery's materials keep the toon chain (Shader to RGB takes the shadow); other models' light is
  Lambert's from nodes (Geometry normal · sun direction × strength / π: the Geometry normal already
  faces the viewer on back faces), without shadows. Scenes without scenery are unchanged (no
  shadows, a point sun).
- **Ink lines in Blender**: after the shading render, each model's parts with ink are joined into one
  object (skins applied, parents cleared keeping places, coincident vertices welded back so a
  material seam is the crease it was in Blender rather than two open borders), so Freestyle's
  per-object "Contour" is the per-model outline. Parts with `ink = 0` go to a collection both
  linesets leave out. Linesets: "Outline" (Contour) and "Inner" (silhouette, border, crease at the
  crease angle), visible only, round caps, widths in pixels times the scene's device scale. Then a
  second render with every surface held out over a transparent film: `Golden/r3d-<scene>-ink.png`,
  whose alpha is the lines' coverage. The engine renders its line coverage alone for the test
  (`Renderer3D.InkCoverageOnly`) and its shading without lines (widths 0).
- **Freestyle's limits seen:** a face seen exactly edge-on (a flat top under a level orthographic
  side view) is neither clearly contour nor not, and Freestyle draws it thin with odd corners; the
  test camera looks slightly down. A face a pixel tall is shaded once per pixel under MSAA (Blender
  per sample); test cameras avoid such slivers.
- **Reproducible:** Blender's PNG stamps (date, render time) are off, so re-running the script on
  unchanged scenes writes the same bytes. The glTF exporter orders triangles differently from run to
  run (not by Python's hash seed), so `make_models.py` puts each index buffer's triangles in one
  order after export (each starting at its smallest index, winding kept, sorted).
- **Comparison** (`Render3DReferenceTests.Compare`): a pixel whose 3×3 neighbourhood in the
  reference is one colour must match within 2 levels. On an edge MSAA 4× covers the pixel in
  quarters and resolves it alone, where EEVEE filters 64 samples across neighbouring pixels: they
  differ by up to 0.61 of the edge's local contrast (the 3×3 range), about 3 levels on average. An
  edge half a pixel out differs by about 9 on average; a missing or extra feature by about the whole
  contrast. So each edge pixel within 0.7 of its local contrast (or 8 levels), all within 4 on
  average. Checked: drawing mirrored nodes with the wrong winding, or culling double-sided
  materials, fails `model-blocks` clearly. On failure the render goes to `Actual/r3d-<scene>.png`.
- **Ink comparison** (`Render3DReferenceTests.CompareInk`, on coverage maps): Freestyle line pixels
  (coverage ≥ 0.5) without an engine line pixel within one pixel ≤ 2% (measured 0.1%); engine line
  pixels away from any Freestyle line ≤ 3% (measured 1.7%, a corner where a part without ink meets
  another); total ink within 10% (measured 1.5%); mean coverage difference over line pixels ≤ 0.12
  (measured 0.05-0.09); the engine's ink in the 7×7 pixels about each Freestyle line pixel 0.75 to
  1.33 times Freestyle's for all but 3% (measured 1%). Checked: missing creases, lines 20% thin,
  lines on parts without ink and outlines only against the background each fail.
- **Shadow comparison** (`CompareShadowed`, for scenes with scenery): the engine also renders the
  scene without shadows; where that render has a geometric edge (non-uniform 3×3), pixels are
  compared as in `Compare`, with one stray pixel in 10,000 allowed (a sub-pixel band edge or contact
  shadow shaded once per pixel against Blender's per sample; measured up to 5). Inside surfaces: at
  most 0.2% of pixels may differ by more than 16 levels (measured 0.12%), and the summed difference
  from Blender may be at most a tenth of the summed difference between Blender and the shadowless
  render (measured at most 0.079). Fringes differ by single pixels up to about 10 levels: Blender
  averages 64 jittered maps, the engine 16 (its blend steps by sixteenths). The `shadow-fringe` scene
  (a pole and a floating bar seen from above at about 200 px/m, fringes ~15 px wide) is the one
  that tells the sun's size: hard shadows, a sun 30% too small or too large, shadows on characters
  and no shadows each fail.

## Steps

1. **Core:** cameras (orthographic, perspective), meshes on the GPU, depth, MSAA 4× targets and
   resolve, a flat-colour pipeline; first Blender reference harness and a geometry test (silhouettes
   of simple shapes from both camera types).
2. **glTF import (.glb):** meshes (positions, normals, UVs, joints, weights), materials, the node
   hierarchy, skins; test models exported by Blender.
3. **Cel shading:** the diffuse term from a sun light into palette bands as Blender's
   Shader-to-RGB and constant colour ramp compute them.
4. **Ink lines:** silhouettes and inner edges at a fixed screen width, against Freestyle.
5. **Shadows:** shadow maps; shadowed areas in the shadow tone.
6. **Hatching and glow.**
7. **GPU skinning:** joint matrices from a pose (sampling animations is M7).
8. **Terrain** (a height field under the orthographic top-down camera) **and particles.**
9. **3D views inside the UI:** an element showing a 3D scene, composited by the canvas.

## Status

Started 2026-10-30.

- **Step 1, core (2026-10-30, 501 tests): done.** `Engine/Statventure/Render3D/`: `Camera`
  (orthographic and perspective, reversed depth), `Mesh` (vertices pulled by address, 32-bit
  indices; `FromTriangles` gives flat faces), `RenderTarget3D` (MSAA 4× sRGB colour, D32 float
  depth, resolve target), `Renderer3D` (flat-colour pipeline `mesh.vert` / `mesh_flat.frag`, depth
  compare Greater cleared to 0). The Blender reference harness and three scenes: `ortho-boxes`,
  `perspective-boxes` (a box cutting through another), `shapes-roll` (slanted faces, a wide aspect,
  a rolled camera, shapes cut by the frame). Interior pixels match Blender exactly; edges within the
  MSAA tolerance. Passes on the GPU and on lavapipe.
- **Step 2, glTF import (2026-10-30, 515 tests): done.** `GltfImport` (BRUTAL.Gltf parses; the
  engine decodes accessors: component types, normalisation, strides, matrix column padding, sparse;
  strips and fans to lists; flat normals when a file has none; points and lines skipped; required
  extensions other than KHR_mesh_quantization refused) into `ModelData` (nodes with local matrices
  and TRS, meshes, materials with slots, skins); `Palette` (slot colours, Blender's `.001` suffix
  stripped); `Model` (GPU meshes per primitive, drawn by the hierarchy; skinned meshes posed by their
  joints at rest). Renderer: linear colours, double-sided and mirrored pipelines. Test models from
  `make_models.py`, scenes `model-blocks` and `model-rigged` match Blender; `GltfImportTests` checks
  the models' structure and colours and decodes a hand-written file (interleaved and quantized
  attributes, 8-bit indices, strip, fan, sparse positions, missing normals, a matrix node). Found
  by the references: a skinned mesh must be placed by its joints, not its node.
- **Step 3, cel shading (2026-10-30, 521 tests): done.** `mesh_cel.frag`: Blender's toon chain as
  arithmetic (strength/π · max(N·L, 0) against the band edge, the normal flipped on back faces), the
  band edge antialiased by `fwidth`; normals by the model's inverse transpose, centroid-sampled.
  `SunLight`, `CelStyle` (ink, shadow mix, band edge; the lizardman's values by default, shadow
  tones mixed in sRGB), `Palette` shadow tones per slot, `DrawItem.Shading` (models cel, shapes
  flat). Scenes `cel-shapes` (smooth sphere scaled unevenly, torus, flat box, a palette shadow tone,
  a colour no palette names, a double-sided card seen from behind, a mirrored smooth cone),
  `cel-closeup` (band edges across large surfaces), `cel-side` (orthographic, low sun from the left);
  `model-blocks` and `model-rigged` are cel-shaded now. All match Blender with interiors exact.
  Checked by breaking the rules: no back-face flip, normals by the model matrix and a band edge 2%
  off each fail.
- **Step 4, ink lines (2026-10-30, 530 tests): done.** User decisions: outline per model, 3.5 px
  outline and 2 px inner lines (times the device scale), the lizardman's inner set (silhouettes,
  open edges, creases sharper than 120°), `ink = 0` object property, screen-space technique.
  Surface records in the 3D pass, `ink_edges.frag` and `ink_lines.frag`, `DrawItem.Group`/`Part`/
  `Ink`, `CelStyle.OutlineWidth`/`InnerWidth`/`CreaseAngle`, `Render(..., scale)`; the node extra
  `ink` read from the glTF JSON (BRUTAL drops extras); integer MSAA resolves take sample 0
  (`CommandList`). Freestyle references for every model scene, a two-model scene (`ink-models`) and
  a 2× device-scale scene (`cel-shapes-2x`). Found on the way: edges decided by sample 0 alone sat up
  to ±0.5 px off (all four samples place them to a quarter pixel); a face plane seen nearly edge-on
  predicts no depth (false inner lines inside outlines); Freestyle's degenerate edge-on case.
  Not measured yet: the passes' cost on a 2016-class GPU (per pixel: 12-16 surface fetches, then
  49 edge fetches at 3.5 px).
- **Step 5, shadows (2026-10-30, 538 tests): done.** User decisions: scenery receives, everything
  casts; soft edges from a 3° sun as Blender renders them (a gradient across the fringe; first
  answered after a wrong description of mine, asked again); no ink on shadows; maps fitted to the
  view, 16 maps of 1024² (first one 2048² map; one map can't tell "under" from "behind" for
  floating parts, asked again). `SunLight.Angle`, `Mesh.BoundsMin/Max`, `DrawItem.CastsShadow/
  ReceivesShadows`, `Model.Draw(..., scenery)`, `FitShadow`, the shadow pass (`shadow.frag`), the
  visibility lookup in `mesh_cel.frag`. Test models `stage.glb` (ground, rock, wall, a standing ring)
  and `fringe.glb`; scenes `shadow-stage` (perspective), `shadow-side` (the battle view),
  `shadow-map` (the map view, 4° off straight down: Freestyle can't place faces exactly edge-on),
  `shadow-fringe`. Found on the way: Blender's Geometry normal already faces the viewer on back
  faces; a sized sun through EEVEE's ramp is a gradient, not a moved edge; one shadow map with rays
  marched toward the disc darkens fringes under floating parts; long thin shadows near the
  threshold break into dashes in both renderers. Not measured yet: the cost on a 2016-class GPU
  (16 depth passes of the casters; 64 fetches per receiver pixel).
- **Step 6, hatching and glow (2026-10-30, 543 tests): done.** User decisions: hatching 6 px apart,
  1 px wide, 72°, 35% ink, in all shadow areas, moving with each model, `hatch = 0` per material;
  glow marked by the Blender shader, the lizardman's halo (8 px with alpha × 2.2, a 2.5 px core,
  behind the model). Recovered `compose.py` from the lizardman session for the glow's exact rule;
  measured the old game's sprite hatching (`sprite-kit.js` `hatch`) and glow (`.sp-glow`) for the
  options. Test model `glowing.glb` (a glowing blade, a flat glowing badge, an unlit pupil), scene
  `glow-figure`; every model scene now hatched. Checked by breaking: hatching fixed to the screen,
  `hatch = 0` ignored, another angle, spacing 20% wider and none; no glow, a halo over its own
  model, a wide blur 20% larger and no alpha boost each fail.
- **Step 7, GPU skinning (2026-10-30, 547 tests): done.** User decisions: normals recomputed from
  the bent surface like Blender, linear blend skinning, 4 influences; and, found while testing poses,
  mesh feature edges for the ink lines. `ModelPose`, `SkinJob`, `Renderer3D.Skin` (skin.comp,
  normals.comp, a device arena per frame in flight), flat faces apart for skinned primitives,
  feature edges (`FeatureEdges`, feature.vert/.frag, `ink_edges` visibility). The CPU skinning at
  rest from step 2 is gone (rest is a pose without overrides). `rigged.glb` gained a fin (flat,
  12 segments, lying 5 mm off the tube), `glowing.glb` a belt lying on the body; scenes `pose-side`
  and `pose-perspective`. Found on the way: Blender recomputes posed normals (custom = auto normals
  after import) and shades all-flat faces per face; a joint's deformation for the reference must be
  skin-independent (posed after the inverse of rest, not the inverse bind matrix, which carries its
  mesh's place); an unsubdivided box skinned across a bend folds along its faces' diagonals (test
  models need subdivisions where they bend); a thin face seen edge-on is a sliver MSAA samples
  differently. Checked by breaking: normals turned with the bones, flat faces averaged, joint
  matrices in the other order and one influence each fail `pose-*`; without feature edges the belt's
  lines go missing (`glow-figure`).
- **Next: step 8, terrain and particles.**

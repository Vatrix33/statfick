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
- **Reproducible:** Blender's PNG stamps (date, render time) are off, so re-running the script on
  unchanged scenes writes the same bytes (models too).
- **Comparison** (`Render3DReferenceTests.Compare`): a pixel whose 3×3 neighbourhood in the
  reference is one colour must match within 2 levels. On an edge MSAA 4× covers the pixel in
  quarters and resolves it alone, where EEVEE filters 64 samples across neighbouring pixels: they
  differ by up to 0.61 of the edge's local contrast (the 3×3 range), about 3 levels on average. An
  edge half a pixel out differs by about 9 on average; a missing or extra feature by about the whole
  contrast. So each edge pixel within 0.7 of its local contrast (or 8 levels), all within 4 on
  average. Checked: drawing mirrored nodes with the wrong winding, or culling double-sided
  materials, fails `model-blocks` clearly. On failure the render goes to `Actual/r3d-<scene>.png`.

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
- **Next: step 3, cel shading.**

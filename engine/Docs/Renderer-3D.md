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

Started 2026-10-30. Next: step 1.

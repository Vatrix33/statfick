# Renders the 3D test scenes (Tests/Statventure.Tests/Reference/r3d-scenes.json, written by the
# tests) with Blender's EEVEE into Golden/r3d-<scene>.png: the look the engine's 3D renderer is
# compared with (Docs/Renderer-3D.md). Scenes with models also get their Freestyle ink lines alone
# in Golden/r3d-<scene>-ink.png (the alpha is the lines' coverage). Scenes with glowing parts also
# write those parts alone and their models' coverage into <glow folder>/<scene>-glow.png and
# -mask.png, which compose_glow.py turns into the halo. Run by Tools/Scripts/make-blender-references.sh:
#   blender -b --factory-startup --python render_scenes.py -- <scenes.json> <output folder> <glow folder> [scene ...]
#
# The scenes are in glTF's coordinates (right-handed, Y up); Blender's are Z up, so every position
# and direction goes through GLTF_TO_BLENDER, as Blender's glTF importer converts them. Models are
# read by Blender's own glTF importer, so the engine's reader is checked against it.
import json
import math
import os
import sys

import bmesh
import bpy
from bpy_extras.object_utils import world_to_camera_view
from mathutils import Matrix, Vector

GLTF_TO_BLENDER = Matrix(((1, 0, 0), (0, 0, -1), (0, 1, 0)))


def srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def hex_to_linear(text):
    value = int(text.lstrip("#"), 16)
    return tuple(srgb_to_linear(((value >> shift) & 255) / 255) for shift in (16, 8, 0))


def column_matrix(columns):
    """The scene file's 4×4 matrix (four columns) as a mathutils Matrix (rows)."""
    return Matrix([[columns[c][r] for c in range(4)] for r in range(4)])


def to_blender(v):
    return GLTF_TO_BLENDER @ Vector(v)


def eevee_engine():
    engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
    return "BLENDER_EEVEE" if "BLENDER_EEVEE" in engines else "BLENDER_EEVEE_NEXT"


def reset():
    GLOWS.clear()
    for collection in (bpy.data.objects, bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.armatures, bpy.data.lights, bpy.data.linestyles):
        for block in list(collection):
            collection.remove(block)


def setup_render(scene, spec):
    render = scene.render
    render.engine = eevee_engine()
    render.resolution_x = spec["width"]
    render.resolution_y = spec["height"]
    render.resolution_percentage = 100
    render.pixel_aspect_x = render.pixel_aspect_y = 1
    render.film_transparent = False
    render.use_freestyle = False
    render.dither_intensity = 0
    render.filter_size = 1.0
    render.use_motion_blur = False
    render.use_compositing = False
    render.use_sequencer = False
    render.image_settings.file_format = "PNG"
    render.image_settings.color_mode = "RGB"
    render.image_settings.color_depth = "8"
    # No date, render time or file name in the PNG: a render of an unchanged scene stays byte for byte the same.
    for prop in render.bl_rna.properties:
        if prop.identifier.startswith("use_stamp"):
            setattr(render, prop.identifier, False)
    scene.eevee.taa_render_samples = 64
    # Only the sun lights surfaces: no screen-space or ray-traced bounces (the world is black to them).
    scene.eevee.use_raytracing = False
    scene.eevee.use_fast_gi = False
    # Linear values go out as sRGB with nothing else applied, as the engine's sRGB target encodes them.
    scene.display_settings.display_device = "sRGB"
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.view_settings.exposure = 0
    scene.view_settings.gamma = 1
    scene.view_settings.use_curve_mapping = False


def setup_world(scene, background):
    """The background colour seen by the camera only: to light it's black, as in the lizardman test
    (which had no world), so only the sun lights surfaces."""
    world = scene.world or bpy.data.worlds.new("World")
    scene.world = world
    nodes = world.node_tree.nodes
    nodes.clear()
    seen = nodes.new("ShaderNodeBackground")
    seen.inputs["Color"].default_value = (*hex_to_linear(background), 1)
    dark = nodes.new("ShaderNodeBackground")
    dark.inputs["Color"].default_value = (0, 0, 0, 1)
    path = nodes.new("ShaderNodeLightPath")
    mix = nodes.new("ShaderNodeMixShader")
    output = nodes.new("ShaderNodeOutputWorld")
    links = world.node_tree.links
    links.new(path.outputs["Is Camera Ray"], mix.inputs[0])
    links.new(dark.outputs[0], mix.inputs[1])
    links.new(seen.outputs[0], mix.inputs[2])
    links.new(mix.outputs[0], output.inputs["Surface"])


def add_sun(scene, spec, shadows):
    """A sun shining from the direction given (toward the light), of the strength given. With
    shadows (a scene with scenery), its angular size softens them, jittered over the samples: the
    exact disc light. Without, it's a direction, as in steps 3 and 4."""
    light = bpy.data.lights.new("sun", "SUN")
    light.energy = spec["strength"]
    light.angle = math.radians(spec["angle"]) if shadows else 0
    light.use_shadow = shadows
    if shadows:
        light.use_shadow_jitter = True
        light.shadow_jitter_overblur = 0
    sun = bpy.data.objects.new("sun", light)
    scene.collection.objects.link(sun)
    # A sun shines down its local −Z.
    sun.rotation_euler = (-to_blender(spec["direction"])).to_track_quat("-Z", "Y").to_euler()


def srgb(c):
    return c * 12.92 if c <= 0.0031308 else 1.055 * c ** (1 / 2.4) - 0.055


def shadow_of(lit_linear, style):
    """The lizardman's shadow tone: the lit tone mixed toward the ink in sRGB."""
    ink = [((int(style["ink"].lstrip("#"), 16) >> shift) & 255) / 255 for shift in (16, 8, 0)]
    return tuple(srgb_to_linear(srgb(c) + (i - srgb(c)) * style["shadowMix"]) for c, i in zip(lit_linear, ink))


def hatch_amount(nodes, links, in_shadow, hatch):
    """The engine's hatching (Docs/Renderer-3D.md, "Hatching"): lines across the screen, `spacing`
    pixels apart and `width` wide, at `angle` (clockwise from the right, Y down), from the model's
    anchor (pixels); on a line and in shadow, a sample takes `ink` of the ink. Window coordinates
    run 0..1 over the render, Y up; per sample, so EEVEE's samples antialias the lines."""
    def op(operation, a, b=None, c=None):
        node = nodes.new("ShaderNodeMath")
        node.operation = operation
        for index, value in enumerate((a, b, c)):
            if value is None:
                continue
            if isinstance(value, (int, float)):
                node.inputs[index].default_value = value
            else:
                links.new(value, node.inputs[index])
        return node.outputs[0]
    coordinates = nodes.new("ShaderNodeTexCoord")
    split = nodes.new("ShaderNodeSeparateXYZ")
    links.new(coordinates.outputs["Window"], split.inputs[0])
    width, height = hatch["size"]
    x = op("MULTIPLY", split.outputs["X"], width)
    y = op("MULTIPLY_ADD", split.outputs["Y"], -height, height)
    nx, ny = -math.sin(math.radians(hatch["angle"])), math.cos(math.radians(hatch["angle"]))
    ax, ay = hatch["anchor"]
    across = op("ADD", op("MULTIPLY", op("SUBTRACT", x, ax), nx), op("MULTIPLY", op("SUBTRACT", y, ay), ny))
    phase = op("FRACT", op("MULTIPLY_ADD", across, 1 / hatch["spacing"], 0.5))
    distance = op("MULTIPLY", op("ABSOLUTE", op("SUBTRACT", phase, 0.5)), hatch["spacing"])
    on_line = op("LESS_THAN", distance, hatch["width"] / 2)
    return op("MULTIPLY", op("MULTIPLY", on_line, in_shadow), hatch["ink"])


def toon_material(name, lit, shadow, band, unshadowed=None, hatch=None):
    """The lizardman test's cel material: a white Diffuse BSDF through Shader to RGB and RGB to BW
    into a constant colour ramp (shadow below the band edge, lit from it), shown as emission. With
    `unshadowed` (the sun's direction and strength), the light is Lambert's from nodes instead,
    strength / π · N·L: the same light without shadows, for models that don't receive them. With
    `hatch`, samples in shadow on a hatch line mix toward the ink."""
    material = bpy.data.materials.new(name)
    nodes = material.node_tree.nodes
    nodes.clear()
    links = material.node_tree.links
    if unshadowed:
        direction, strength = unshadowed
        # The Geometry node's normal already faces the viewer on back faces (EEVEE flips it).
        geometry = nodes.new("ShaderNodeNewGeometry")
        dot = nodes.new("ShaderNodeVectorMath")
        dot.operation = "DOT_PRODUCT"
        links.new(geometry.outputs["Normal"], dot.inputs[0])
        dot.inputs[1].default_value = tuple(direction)
        clamp = nodes.new("ShaderNodeMath")
        clamp.operation = "MAXIMUM"
        clamp.inputs[1].default_value = 0
        links.new(dot.outputs["Value"], clamp.inputs[0])
        light = nodes.new("ShaderNodeMath")
        light.operation = "MULTIPLY"
        light.inputs[1].default_value = strength / math.pi
        links.new(clamp.outputs[0], light.inputs[0])
        source = light.outputs[0]
    else:
        diffuse = nodes.new("ShaderNodeBsdfDiffuse")
        diffuse.inputs["Color"].default_value = (1, 1, 1, 1)
        to_rgb = nodes.new("ShaderNodeShaderToRGB")
        bw = nodes.new("ShaderNodeRGBToBW")
        links.new(diffuse.outputs[0], to_rgb.inputs[0])
        links.new(to_rgb.outputs[0], bw.inputs[0])
        source = bw.outputs[0]
    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.interpolation = "CONSTANT"
    ramp.color_ramp.elements[0].position = 0
    ramp.color_ramp.elements[0].color = (*shadow, 1)
    ramp.color_ramp.elements[1].position = band
    ramp.color_ramp.elements[1].color = (*lit, 1)
    emission = nodes.new("ShaderNodeEmission")
    output = nodes.new("ShaderNodeOutputMaterial")
    links.new(source, ramp.inputs[0])
    colour = ramp.outputs[0]
    if hatch:
        in_shadow = nodes.new("ShaderNodeMath")
        in_shadow.operation = "LESS_THAN"
        in_shadow.inputs[1].default_value = band
        links.new(source, in_shadow.inputs[0])
        mix = nodes.new("ShaderNodeMix")
        mix.data_type = "RGBA"
        links.new(hatch_amount(nodes, links, in_shadow.outputs[0], hatch), mix.inputs["Factor"])
        links.new(colour, mix.inputs[6])
        mix.inputs[7].default_value = (*hatch["inkColour"], 1)
        colour = mix.outputs[2]
    links.new(colour, emission.inputs[0])
    links.new(emission.outputs[0], output.inputs["Surface"])
    return material


def flat_material(name, linear):
    """An unlit colour: emission of strength 1 shows the colour itself (flat colours so far)."""
    material = bpy.data.materials.new(name)
    nodes = material.node_tree.nodes
    nodes.clear()
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Color"].default_value = (*linear, 1)
    emission.inputs["Strength"].default_value = 1
    output = nodes.new("ShaderNodeOutputMaterial")
    material.node_tree.links.new(emission.outputs["Emission"], output.inputs["Surface"])
    return material


def add_object(scene, index, shape, spec):
    """The shape's positions placed by its model matrix, then turned into Blender's coordinates."""
    model = column_matrix(spec["matrix"])
    vertices = [to_blender((model @ Vector((*p, 1))).xyz) for p in shape["positions"]]
    t = shape["triangles"]
    faces = [(t[i], t[i + 1], t[i + 2]) for i in range(0, len(t), 3)]
    mesh = bpy.data.meshes.new(f"{spec['shape']}-{index}")
    mesh.from_pydata([tuple(v) for v in vertices], [], faces)
    mesh.update()
    mesh.materials.append(flat_material(f"color-{index}", hex_to_linear(spec["color"])))
    obj = bpy.data.objects.new(mesh.name, mesh)
    scene.collection.objects.link(obj)


def slot_of(name):
    """A material name's palette slot: without the ".001"-style suffix Blender gives a second material of a name."""
    return name[:-4] if len(name) > 4 and name[-4] == "." and name[-3:].isdigit() else name


def bake_model(objects, noink):
    """One object per model for Freestyle, whose "Contour" outlines each object: the model's parts
    with ink are joined (skins applied at rest, parents cleared keeping their places); parts with
    ink = 0 (the importer turns extras into custom properties) go to the collection the linesets
    leave out."""
    meshes = [o for o in objects if o.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.make_single_user(object=True, obdata=True)
    bpy.ops.object.convert(target="MESH")
    bpy.ops.object.select_all(action="DESELECT")
    for o in objects:
        if o.name in bpy.context.view_layer.objects:
            o.select_set(True)
    bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
    bpy.ops.object.select_all(action="DESELECT")
    inked = [o for o in meshes if o.get("ink", 1) != 0]
    for o in meshes:
        if o.get("ink", 1) == 0:
            for collection in list(o.users_collection):
                collection.objects.unlink(o)
            noink.objects.link(o)
    if len(inked) > 1:
        for o in inked:
            o.select_set(True)
        bpy.context.view_layer.objects.active = inked[0]
        bpy.ops.object.join()
    bpy.ops.object.select_all(action="DESELECT")
    # glTF splits a mesh's materials into primitives with their own vertices; welded again, the
    # edge where they meet is the crease it was in Blender rather than two open borders on top of
    # each other (which Freestyle draws only in pieces).
    for o in [o for o in bpy.context.scene.objects if o.type == "MESH" and o.name not in noink.objects]:
        bm = bmesh.new()
        bm.from_mesh(o.data)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
        bm.to_mesh(o.data)
        bm.free()


def setup_ink(scene, style, noink):
    """The lizardman's Freestyle setup per model: the outline is each (baked) model's contour; inner
    lines are silhouettes, open edges and creases sharper than the crease angle; visible lines only,
    round caps, widths in pixels."""
    render = scene.render
    render.line_thickness_mode = "ABSOLUTE"
    settings = bpy.context.view_layer.freestyle_settings
    settings.crease_angle = math.radians(style["creaseAngle"])
    settings.use_smoothness = False
    settings.use_culling = False
    for lineset in list(settings.linesets):
        settings.linesets.remove(lineset)
    for name, width, outline in (("Outline", style["outlineWidth"], True), ("Inner", style["innerWidth"], False)):
        lineset = settings.linesets.new(name)
        lineset.linestyle = bpy.data.linestyles.new(name)
        lineset.linestyle.color = (1, 1, 1)
        lineset.linestyle.thickness = width
        lineset.linestyle.caps = "ROUND"
        lineset.select_by_visibility = True
        lineset.visibility = "VISIBLE"
        lineset.select_by_edge_types = True
        lineset.select_by_collection = True
        lineset.collection = noink
        lineset.collection_negation = "EXCLUSIVE"
        lineset.select_silhouette = not outline
        lineset.select_border = not outline
        lineset.select_crease = not outline
        lineset.select_material_boundary = False
        lineset.select_contour = outline
        lineset.select_external_contour = False


def render_ink(scene, path):
    """The ink lines alone: every surface held out over a transparent film, so the PNG's alpha is the
    lines' coverage."""
    holdout = holdout_material()
    for obj in scene.objects:
        for slot in getattr(obj, "material_slots", []):
            slot.material = holdout
    scene.render.use_freestyle = True
    scene.render.film_transparent = True
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("wrote", path)
    scene.render.use_freestyle = False
    scene.render.film_transparent = False
    scene.render.image_settings.color_mode = "RGB"


def add_model(scene, path, spec, palette, shadows, style, unshadowed=None, scale=1):
    """Imports a .glb, places it by the matrix (glTF coordinates) and cel-shades each material in the
    tones its palette slot gives (lit, and shadow if the palette has one), or its own base colour
    when the palette has no such slot; shadow tones the palette doesn't give come from the style."""
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    imported = [o for o in bpy.data.objects if o not in before]
    to_blender4 = GLTF_TO_BLENDER.to_4x4()
    holder = bpy.data.objects.new("placement", None)
    scene.collection.objects.link(holder)
    holder.matrix_world = to_blender4 @ column_matrix(spec["matrix"]) @ to_blender4.inverted()
    for obj in imported:
        if obj.parent is None:
            obj.parent = holder
    imported.append(holder)
    # The hatching moves with the model: anchored where its origin is on screen (pixels, Y down).
    bpy.context.view_layer.update()
    seen = world_to_camera_view(scene, scene.camera, holder.matrix_world.translation)
    width, height = scene.render.resolution_x, scene.render.resolution_y
    hatch = {
        "size": (width, height), "anchor": (seen.x * width, (1 - seen.y) * height),
        "spacing": style["hatchSpacing"] * scale, "width": style["hatchWidth"] * scale,
        "angle": style["hatchAngle"], "ink": style["hatchInk"], "inkColour": hex_to_linear(style["ink"]),
    }
    # The importer keeps glTF's material name, base colour (Principled BSDF), emission and doubleSided
    # (backface culling); an unlit material comes back as an Emission shader without a Principled
    # BSDF. Each material is replaced once: a mesh used by two objects shares its slots.
    replaced = {}
    for obj in imported:
        for slot in getattr(obj, "material_slots", []):
            source = slot.material
            if source is None or source in replaced.values():
                continue
            if source not in replaced:
                slot_name = slot_of(source.name)
                colour = palette.get(slot_name)
                kind, base, emission = material_kind(source)
                if kind == "unlit":
                    flat = flat_material(f"unlit-{source.name}", hex_to_linear(colour) if colour else base)
                elif kind == "emission":
                    flat = flat_material(f"emission-{source.name}", emission)
                    GLOWS[flat] = emission
                else:
                    lit = hex_to_linear(colour) if colour else tuple(base)
                    shadow = hex_to_linear(shadows[slot_name]) if colour and slot_name in shadows else shadow_of(lit, style)
                    hatched = source.get("hatch", 1) != 0
                    flat = toon_material(f"toon-{source.name}", lit, shadow, style["bandEdge"], unshadowed, hatch if hatched else None)
                    if any(emission):
                        GLOWS[flat] = emission
                flat.use_backface_culling = source.use_backface_culling
                replaced[source] = flat
            slot.material = replaced[source]
    return imported


# Replaced materials that glow, and their glow colours (linear).
GLOWS = {}


def material_kind(material):
    """How the Blender shader marks a material (Docs/Renderer-3D.md, "Glow"): "unlit" (glTF unlit, which
    the importer rebuilds around an Emission shader), "emission" (an Emission shader alone: a
    Principled BSDF with a black base and only emission) or "lit"; its base colour and its emission
    (colour times strength), linear."""
    nodes = material.node_tree.nodes
    principled = next((n for n in nodes if n.bl_idname == "ShaderNodeBsdfPrincipled"), None)
    if principled is None:
        emission = next(n for n in nodes if n.bl_idname == "ShaderNodeEmission")
        return "unlit", tuple(emission.inputs["Color"].default_value[:3]), (0, 0, 0)
    base = tuple(principled.inputs["Base Color"].default_value[:3])
    strength = principled.inputs["Emission Strength"].default_value
    emission = tuple(c * strength for c in principled.inputs["Emission Color"].default_value[:3])
    return ("emission" if any(emission) and not any(base) else "lit"), base, emission


def render_glow(scene, placed, folder, name):
    """For compose_glow.py: the glowing parts alone in their glow colours over a transparent film, as
    the lizardman's glow render (everything else hidden), and the coverage of the models they belong
    to (their halo goes behind them)."""
    glowing = [objects for objects in placed if any(slot.material in GLOWS for o in objects for slot in getattr(o, "material_slots", []))]
    if not glowing:
        return
    saved = {o: [slot.material for slot in o.material_slots] for o in scene.objects if o.type == "MESH"}
    scene.render.film_transparent = True
    scene.render.image_settings.color_mode = "RGBA"
    for o in saved:
        lights = [GLOWS.get(m) for m in saved[o]]
        o.hide_render = not any(lights)
        for slot, light in zip(o.material_slots, lights):
            slot.material = flat_material("glow", light) if light else holdout_material()
    scene.render.filepath = os.path.join(folder, f"{name}-glow.png")
    bpy.ops.render.render(write_still=True)
    print("wrote", scene.render.filepath)
    owners = {o for objects in glowing for o in objects}
    white = flat_material("mask", (1, 1, 1))
    for o in saved:
        o.hide_render = o not in owners
        for slot in o.material_slots:
            slot.material = white
    scene.render.filepath = os.path.join(folder, f"{name}-mask.png")
    bpy.ops.render.render(write_still=True)
    print("wrote", scene.render.filepath)
    for o, materials in saved.items():
        o.hide_render = False
        for slot, m in zip(o.material_slots, materials):
            slot.material = m
    scene.render.film_transparent = False
    scene.render.image_settings.color_mode = "RGB"


def holdout_material():
    holdout = bpy.data.materials.new("holdout")
    nodes = holdout.node_tree.nodes
    nodes.clear()
    hold = nodes.new("ShaderNodeHoldout")
    output = nodes.new("ShaderNodeOutputMaterial")
    holdout.node_tree.links.new(hold.outputs[0], output.inputs["Surface"])
    return holdout


def principled_base_color(material):
    for node in material.node_tree.nodes:
        if node.bl_idname == "ShaderNodeBsdfPrincipled":
            return node.inputs["Base Color"].default_value[:3]
    return material.diffuse_color[:3]


def add_camera(scene, spec):
    """A camera at the position looking at the target, its vertical extent fixed (sensor fit vertical)."""
    data = bpy.data.cameras.new("camera")
    data.sensor_fit = "VERTICAL"
    data.sensor_height = 24
    data.clip_start = spec["near"]
    data.clip_end = spec["far"]
    if spec["projection"] == "orthographic":
        data.type = "ORTHO"
        data.ortho_scale = spec["orthographicHeight"]
    else:
        data.type = "PERSP"
        data.lens = data.sensor_height / (2 * math.tan(math.radians(spec["fieldOfViewDegrees"]) / 2))
    camera = bpy.data.objects.new("camera", data)
    scene.collection.objects.link(camera)
    position = to_blender(spec["position"])
    forward = (to_blender(spec["target"]) - position).normalized()
    right = forward.cross(to_blender(spec["up"])).normalized()
    up = right.cross(forward)
    # A Blender camera looks down its local −Z with +Y up.
    rotation = Matrix((right, up, -forward)).transposed().to_4x4()
    camera.matrix_world = Matrix.Translation(position) @ rotation
    scene.camera = camera


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    scenes_path, output, glow_folder = argv[0], argv[1], argv[2]
    os.makedirs(glow_folder, exist_ok=True)
    with open(scenes_path) as f:
        data = json.load(f)
    only = set(argv[3:])
    scene = bpy.context.scene
    for spec in data["scenes"]:
        if only and spec["name"] not in only:
            continue
        reset()
        setup_render(scene, spec)
        setup_world(scene, spec["background"])
        add_camera(scene, spec["camera"])
        # Only scenery receives shadows; the rest is lit by Lambert's law alone (Docs/Renderer-3D.md, "Shadows").
        shadows = any(m.get("scenery") for m in spec.get("models", []))
        if spec.get("sun"):
            add_sun(scene, spec["sun"], shadows)
        unshadowed = (to_blender(spec["sun"]["direction"]).normalized(), spec["sun"]["strength"]) if shadows else None
        for index, obj in enumerate(spec["objects"]):
            add_object(scene, index, data["shapes"][obj["shape"]], obj)
        models = os.path.normpath(os.path.join(os.path.dirname(scenes_path), data["modelFolder"]))
        placed = [add_model(scene, os.path.join(models, model["file"]), model, spec.get("palette", {}), spec.get("shadows", {}), spec["style"],
                            None if model.get("scenery") else unshadowed, spec.get("scale", 1))
                  for model in spec.get("models", [])]
        scene.render.filepath = os.path.join(output, f"r3d-{spec['name']}.png")
        bpy.ops.render.render(write_still=True)
        print("wrote", scene.render.filepath)
        stale = [os.path.join(glow_folder, f"{spec['name']}-{part}.png") for part in ("glow", "mask")]
        for path in stale:
            if os.path.exists(path):
                os.remove(path)
        render_glow(scene, placed, glow_folder, spec["name"])
        if placed:
            # Baking joins and converts meshes, which loses their imported normals: only after the
            # shading render (Freestyle works from face normals).
            noink = bpy.data.collections.get("noink") or bpy.data.collections.new("noink")
            if noink.name not in scene.collection.children:
                scene.collection.children.link(noink)
            for objects in placed:
                bake_model(objects, noink)
            setup_ink(scene, spec["style"], noink)
            render_ink(scene, os.path.join(output, f"r3d-{spec['name']}-ink.png"))


main()

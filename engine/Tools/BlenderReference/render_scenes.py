# Renders the 3D test scenes (Tests/Statventure.Tests/Reference/r3d-scenes.json, written by the
# tests) with Blender's EEVEE into Golden/r3d-<scene>.png: the look the engine's 3D renderer is
# compared with (Docs/Renderer-3D.md). Run by Tools/Scripts/make-blender-references.sh:
#   blender -b --factory-startup --python render_scenes.py -- <scenes.json> <output folder>
#
# The scenes are in glTF's coordinates (right-handed, Y up); Blender's are Z up, so every position
# and direction goes through GLTF_TO_BLENDER, as Blender's glTF importer converts them.
import json
import math
import os
import sys

import bpy
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
    for collection in (bpy.data.objects, bpy.data.meshes, bpy.data.materials, bpy.data.cameras):
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
    scene.eevee.taa_render_samples = 64
    # Linear values go out as sRGB with nothing else applied, as the engine's sRGB target encodes them.
    scene.display_settings.display_device = "sRGB"
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.view_settings.exposure = 0
    scene.view_settings.gamma = 1
    scene.view_settings.use_curve_mapping = False


def setup_world(scene, background):
    world = scene.world or bpy.data.worlds.new("World")
    scene.world = world
    nodes = world.node_tree.nodes
    nodes.clear()
    node = nodes.new("ShaderNodeBackground")
    node.inputs["Color"].default_value = (*hex_to_linear(background), 1)
    node.inputs["Strength"].default_value = 1
    output = nodes.new("ShaderNodeOutputWorld")
    world.node_tree.links.new(node.outputs["Background"], output.inputs["Surface"])


def flat_material(name, color):
    """An unlit colour: emission of strength 1 shows the colour itself (step 1 draws flat colours)."""
    material = bpy.data.materials.new(name)
    nodes = material.node_tree.nodes
    nodes.clear()
    emission = nodes.new("ShaderNodeEmission")
    emission.inputs["Color"].default_value = (*hex_to_linear(color), 1)
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
    mesh.materials.append(flat_material(f"color-{index}", spec["color"]))
    obj = bpy.data.objects.new(mesh.name, mesh)
    scene.collection.objects.link(obj)


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
    scenes_path, output = argv[0], argv[1]
    with open(scenes_path) as f:
        data = json.load(f)
    only = set(argv[2:])
    scene = bpy.context.scene
    for spec in data["scenes"]:
        if only and spec["name"] not in only:
            continue
        reset()
        setup_render(scene, spec)
        setup_world(scene, spec["background"])
        add_camera(scene, spec["camera"])
        for index, obj in enumerate(spec["objects"]):
            add_object(scene, index, data["shapes"][obj["shape"]], obj)
        scene.render.filepath = os.path.join(output, f"r3d-{spec['name']}.png")
        bpy.ops.render.render(write_still=True)
        print("wrote", scene.render.filepath)


main()

# Builds the 3D test models in Blender and exports them as .glb into
# Tests/Statventure.Tests/Data/Models (Docs/Renderer-3D.md, step 2). This script is their source.
# Run by Tools/Scripts/make-blender-references.sh --models:
#   blender -b --factory-startup --python make_models.py -- <output folder>
#
# Material names are palette slots (metal.base, cloth, ...); a second material named like a first
# gets Blender's ".001" suffix, which the engine strips.
import json
import math
import os
import struct
import sys

import bmesh
import bpy
from mathutils import Matrix


def srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def material(name, hex_color, culled=True):
    """A Principled material of this sRGB colour; the exporter writes it as baseColorFactor."""
    value = int(hex_color.lstrip("#"), 16)
    linear = [srgb_to_linear(((value >> shift) & 255) / 255) for shift in (16, 8, 0)]
    m = bpy.data.materials.new(name)
    m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*linear, 1)
    # Backface culling on exports doubleSided false; off (Blender's default) exports true.
    m.use_backface_culling = culled
    return m


def reset():
    for collection in (bpy.data.objects, bpy.data.meshes, bpy.data.materials, bpy.data.armatures):
        for block in list(collection):
            collection.remove(block)


def mesh_object(name, mesh, parent=None, location=(0, 0, 0), rotation=(0, 0, 0), scale=(1, 1, 1)):
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    obj.parent = parent
    obj.location = location
    obj.rotation_euler = [math.radians(a) for a in rotation]
    obj.scale = scale
    return obj


def box_mesh(name, size, materials):
    """A box of this size (x, y, z) around its centre; the top face (+Z) takes the last material."""
    bm = bmesh.new()
    bm.loops.layers.uv.new("UVMap")
    bmesh.ops.create_cube(bm, size=1, calc_uvs=True)
    bmesh.ops.scale(bm, vec=size, verts=bm.verts)
    for face in bm.faces:
        face.material_index = len(materials) - 1 if face.normal.z > 0.5 else 0
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    for m in materials:
        mesh.materials.append(m)
    return mesh


def cone_mesh(name, segments, radius1, radius2, depth, smooth, m):
    bm = bmesh.new()
    bm.loops.layers.uv.new("UVMap")
    bmesh.ops.create_cone(bm, cap_ends=True, segments=segments, radius1=radius1, radius2=radius2, depth=depth, calc_uvs=True)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    mesh.materials.append(m)
    if smooth:
        mesh.shade_smooth()
    return mesh


def uv_sphere_mesh(name, radius, m):
    bm = bmesh.new()
    bm.loops.layers.uv.new("UVMap")
    bmesh.ops.create_uvsphere(bm, u_segments=32, v_segments=16, radius=radius, calc_uvs=True)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    mesh.materials.append(m)
    mesh.shade_smooth()
    return mesh


def torus_mesh(name, major, minor, m, rings=32, sides=12):
    bm = bmesh.new()
    for r in range(rings):
        a = 2 * math.pi * r / rings
        for t in range(sides):
            b = 2 * math.pi * t / sides
            d = major + minor * math.cos(b)
            bm.verts.new((d * math.cos(a), d * math.sin(a), minor * math.sin(b)))
    bm.verts.ensure_lookup_table()
    for r in range(rings):
        for t in range(sides):
            a, b = r * sides + t, r * sides + (t + 1) % sides
            c, d = ((r + 1) % rings) * sides + (t + 1) % sides, ((r + 1) % rings) * sides + t
            bm.faces.new((bm.verts[a], bm.verts[d], bm.verts[c], bm.verts[b]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    mesh.materials.append(m)
    mesh.shade_smooth()
    return mesh


def canonical_triangles(path):
    """Puts each primitive's triangles in one order: the exporter's order changes from run to run
    (the same triangles), and the models should be the same bytes when nothing changed. Each
    triangle starts at its smallest index (its winding kept); the triangles are sorted."""
    data = bytearray(open(path, "rb").read())
    length = struct.unpack_from("<I", data, 12)[0]
    gltf = json.loads(data[20:20 + length])
    binary = 20 + length + 8
    formats = {5121: "B", 5123: "H", 5125: "I"}
    for mesh in gltf["meshes"]:
        for primitive in mesh["primitives"]:
            if primitive.get("mode", 4) != 4 or "indices" not in primitive:
                continue
            accessor = gltf["accessors"][primitive["indices"]]
            view = gltf["bufferViews"][accessor["bufferView"]]
            offset = binary + view.get("byteOffset", 0) + accessor.get("byteOffset", 0)
            code = "<" + str(accessor["count"]) + formats[accessor["componentType"]]
            indices = struct.unpack_from(code, data, offset)
            triangles = [indices[i:i + 3] for i in range(0, len(indices), 3)]
            triangles = sorted(min(t[k:] + t[:k] for k in range(3)) for t in triangles)
            struct.pack_into(code, data, offset, *[i for t in triangles for i in t])
    open(path, "wb").write(data)


def export(path, **options):
    # Extras carry the objects' custom properties (ink = 0: no ink lines).
    bpy.ops.export_scene.gltf(filepath=path, export_format="GLB", export_animations=False, export_apply=True,
                              export_extras=True, export_image_format="NONE", **options)
    canonical_triangles(path)
    print("wrote", path)


def blocks(path):
    """A small node hierarchy: a slab with two materials (the top in a slot), a smooth pillar and a
    cone cap as its descendants, a double-sided banner seen from behind, and one box mesh used by two
    nodes, the second mirrored (negative scale), both sunk into the slab; the first has no ink lines."""
    stone = material("stone", "#6b6258")
    cloth = material("cloth", "#7b5a3a")
    metal = material("metal.base", "#8a8f96")
    trim = material("metal.trim", "#c9a46a")
    banner_cloth = material("cloth", "#5d2f4e", culled=False)  # becomes "cloth.001"
    glow = material("glow", "#f2d488")

    stand = bpy.data.objects.new("Stand", None)
    bpy.context.scene.collection.objects.link(stand)
    stand.rotation_euler = (0, 0, math.radians(15))
    stand.scale = (1.2, 1.2, 1.2)

    base = mesh_object("Base", box_mesh("slab", (1.8, 1.4, 0.3), [stone, cloth]), stand, location=(0, 0, 0.15))
    pillar = mesh_object("Pillar", cone_mesh("pillar", 12, 0.22, 0.22, 1.4, True, metal), base, location=(0.35, 0.15, 0.85), rotation=(10, 0, 0))
    mesh_object("Cap", cone_mesh("cap", 8, 0.4, 0.0, 0.5, False, trim), pillar, location=(0, 0, 0.95))

    plane = bpy.data.meshes.new("banner")
    plane.from_pydata([(-0.35, 0, -0.45), (0.35, 0, -0.45), (0.35, 0, 0.45), (-0.35, 0, 0.45)], [], [(0, 1, 2, 3)])
    plane.materials.append(banner_cloth)
    uv = plane.uv_layers.new(name="UVMap")
    for loop, coord in zip(uv.data, ((0, 0), (1, 0), (1, 1), (0, 1))):
        loop.uv = coord
    # Its front faces −Y; the reference camera sees its back.
    mesh_object("Banner", plane, stand, location=(-0.55, -0.35, 0.95), rotation=(0, 0, 20))

    block = box_mesh("block", (0.35, 0.5, 0.25), [glow])
    # Drawn without ink lines (the lizardman's "nolines" details): a custom property, exported as extras.
    mesh_object("BlockA", block, stand, location=(-0.6, 0.45, 0.3), rotation=(0, 0, 25))["ink"] = 0
    mesh_object("BlockB", block, stand, location=(0.75, -0.45, 0.28), rotation=(0, 0, -30), scale=(-1, 1, 1))
    export(path)


def rigged(path):
    """A tube on a two-bone armature (weights by height), exported in its rest pose."""
    skin = material("skin", "#a9805e")
    arm_data = bpy.data.armatures.new("rig")
    rig = bpy.data.objects.new("Rig", arm_data)
    bpy.context.scene.collection.objects.link(rig)
    rig.location = (0.2, 0, 0)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    lower = arm_data.edit_bones.new("Lower")
    lower.head, lower.tail = (0, 0, 0), (0, 0, 1)
    upper = arm_data.edit_bones.new("Upper")
    upper.head, upper.tail = (0, 0, 1), (0.3, 0, 1.9)
    upper.parent = lower
    upper.use_connect = True
    bpy.ops.object.mode_set(mode="OBJECT")

    bm = bmesh.new()
    rings, segments = 6, 10
    for r in range(rings):
        z = 2.0 * r / (rings - 1)
        for s in range(segments):
            a = 2 * math.pi * s / segments
            bm.verts.new((0.2 * math.cos(a), 0.2 * math.sin(a), z))
    bm.verts.ensure_lookup_table()
    for r in range(rings - 1):
        for s in range(segments):
            a, b = r * segments + s, r * segments + (s + 1) % segments
            bm.faces.new((bm.verts[a], bm.verts[b], bm.verts[b + segments], bm.verts[a + segments]))
    bm.faces.new([bm.verts[s] for s in reversed(range(segments))])
    bm.faces.new([bm.verts[(rings - 1) * segments + s] for s in range(segments)])
    mesh = bpy.data.meshes.new("tube")
    bm.to_mesh(mesh)
    bm.free()
    mesh.materials.append(skin)
    mesh.shade_smooth()
    tube = mesh_object("Tube", mesh, rig)
    lower_group, upper_group = tube.vertex_groups.new(name="Lower"), tube.vertex_groups.new(name="Upper")
    for v in mesh.vertices:
        t = min(max((v.co.z - 0.6) / 0.8, 0), 1)
        if t < 1:
            lower_group.add([v.index], 1 - t, "REPLACE")
        if t > 0:
            upper_group.add([v.index], t, "REPLACE")
    modifier = tube.modifiers.new("Armature", "ARMATURE")
    modifier.object = rig
    export(path, export_skins=True)


def shapes(path):
    """Cel shading test shapes: a smooth sphere scaled unevenly (normals by the inverse transpose), a
    smooth torus, a flat box in a colour no palette names, a double-sided card seen from behind (its
    back is lit only with the normal flipped), and a smooth tilted cone mirrored (negative scale)."""
    cloth = material("cloth", "#7b5a3a")
    metal = material("metal.base", "#8a8f96")
    trim = material("metal.trim", "#c9a46a")
    stone = material("stone", "#6b6258")
    card = material("cloth", "#5d2f4e", culled=False)  # becomes "cloth.001"
    mesh_object("Ball", uv_sphere_mesh("ball", 0.5, cloth), location=(-1.5, 0, 0.5), scale=(1.3, 1.0, 0.75))
    mesh_object("Ring", torus_mesh("ring", 0.42, 0.16, metal), location=(-0.25, 0.1, 0.55), rotation=(65, 0, 20))
    box = box_mesh("box", (0.6, 0.6, 0.6), [stone])
    mesh_object("Box", box, location=(0.85, 0.2, 0.35), rotation=(20, 0, 35))
    plane = bpy.data.meshes.new("card")
    plane.from_pydata([(-0.3, 0, -0.4), (0.3, 0, -0.4), (0.3, 0, 0.4), (-0.3, 0, 0.4)], [], [(0, 3, 2, 1)])
    plane.materials.append(card)
    # Its front faces +Y, away from the camera; turned 30° about Z its back faces the sun.
    mesh_object("Card", plane, location=(0.55, -0.6, 0.45), rotation=(0, 0, 30))
    mesh_object("Spike", cone_mesh("spike", 24, 0.28, 0.0, 0.9, True, trim), location=(1.75, 0, 0.5), rotation=(0, 35, 0), scale=(-1, 1, 1))
    export(path)


def stage(path):
    """Scenery for shadows: a ground slab (its top at 0), a smooth rock, a wall, and a ring standing
    on edge whose shadow has a hole."""
    ground = material("ground", "#9a8a68")
    stone = material("stone", "#6b6258")
    mesh_object("Ground", box_mesh("ground", (6, 4, 0.2), [ground]), location=(0, 0, -0.1))
    bm = bmesh.new()
    bm.loops.layers.uv.new("UVMap")
    bmesh.ops.create_icosphere(bm, subdivisions=2, radius=0.35, calc_uvs=True)
    rock = bpy.data.meshes.new("rock")
    bm.to_mesh(rock)
    bm.free()
    rock.materials.append(stone)
    rock.shade_smooth()
    mesh_object("Rock", rock, location=(-1.9, 0.9, 0.15), scale=(1, 0.8, 0.7))
    mesh_object("Wall", box_mesh("wall", (0.25, 1.2, 1.4), [stone]), location=(2.3, 0.6, 0.7), rotation=(0, 0, 15))
    mesh_object("Arch", torus_mesh("arch", 0.5, 0.1, stone), location=(-0.6, 1.3, 0.6), rotation=(90, 0, 20))
    export(path)


def fringe(path):
    """Wide shadow fringes: a ground tile, a tall pole, and a bar floating 1.5 m up on two thin posts
    (a floating part: light passes under it), so a 3° sun spreads their shadows' fringes over many pixels."""
    ground = material("ground", "#9a8a68")
    stone = material("stone", "#6b6258")
    mesh_object("Ground", box_mesh("tile", (3, 2.4, 0.1), [ground]), location=(0, 0, -0.05))
    mesh_object("Pole", cone_mesh("pole", 12, 0.06, 0.06, 2.0, True, stone), location=(-0.9, 0.3, 1.0))
    mesh_object("Bar", box_mesh("bar", (1.2, 0.16, 0.12), [stone]), location=(0.35, 0.1, 1.5))
    post = box_mesh("post", (0.04, 0.04, 1.44), [stone])
    mesh_object("PostA", post, location=(-0.2, 0.1, 0.72))
    mesh_object("PostB", post, location=(0.9, 0.1, 0.72))
    export(path)


def main():
    output = sys.argv[sys.argv.index("--") + 1]
    os.makedirs(output, exist_ok=True)
    for name, build in (("blocks", blocks), ("rigged", rigged), ("shapes", shapes), ("stage", stage), ("fringe", fringe)):
        reset()
        build(os.path.join(output, name + ".glb"))


main()

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
from mathutils import Matrix, Quaternion, Vector


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


def glowing_material(name, hex_color, emission_hex, strength=1.0):
    """A Principled material of this colour that also emits: cel-shaded and glowing (the lizardman's blade)."""
    m = material(name, hex_color)
    value = int(emission_hex.lstrip("#"), 16)
    principled = m.node_tree.nodes["Principled BSDF"]
    principled.inputs["Emission Color"].default_value = (*[srgb_to_linear(((value >> s) & 255) / 255) for s in (16, 8, 0)], 1)
    principled.inputs["Emission Strength"].default_value = strength
    return m


def shader_material(name, hex_color, kind):
    """An Emission shader alone (flat and glowing: the lizardman's badge) or a Background shader (flat,
    no glow, exported unlit: its pupils)."""
    value = int(hex_color.lstrip("#"), 16)
    colour = (*[srgb_to_linear(((value >> s) & 255) / 255) for s in (16, 8, 0)], 1)
    m = bpy.data.materials.new(name)
    nodes = m.node_tree.nodes
    nodes.clear()
    shader = nodes.new("ShaderNodeEmission" if kind == "emission" else "ShaderNodeBackground")
    shader.inputs["Color"].default_value = colour
    output = nodes.new("ShaderNodeOutputMaterial")
    m.node_tree.links.new(shader.outputs[0], output.inputs["Surface"])
    m.use_backface_culling = True
    return m


def reset():
    for collection in (bpy.data.objects, bpy.data.meshes, bpy.data.materials, bpy.data.armatures, bpy.data.actions):
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
    """Exports the scene as a .glb. A path of None only builds it (evaluate_animations.py builds the
    animated models again to evaluate their actions)."""
    if path is None:
        return
    # Extras carry the objects' custom properties (ink = 0: no ink lines).
    settings = dict(export_format="GLB", export_animations=False, export_apply=True, export_extras=True,
                    export_image_format="NONE")
    settings.update(options)
    bpy.ops.export_scene.gltf(filepath=path, **settings)
    canonical_triangles(path)
    print("wrote", path)


# Animated models (Docs/Animation.md): actions are animated at 60 fps and exported sampled every
# frame, each as a glTF animation of its own (the "ACTIONS" mode exports the actions on NLA tracks),
# starting at 0 s, with only the channels the action keys ("Force keeping channels for bones" off;
# by default the exporter writes every bone into every clip).
ANIMATED = dict(export_skins=True, export_animations=True, export_animation_mode="ACTIONS", export_force_sampling=True,
                export_frame_step=1, export_optimize_animation_keep_anim_armature=False, export_anim_slide_to_zero=True)


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
    """A smooth tube and a flat fin on a two-bone armature (weights by height), exported in its rest pose."""
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
    # A flat-shaded fin lying on the tube (5 mm off it, as a plate on a body), weighted like it: under
    # a bend its faces must stay flat and its edges keep their ink. Cut into 12 lengths, as a plate or
    # strap that bends would be (a box of 8 vertices folds along its faces' diagonals).
    fin_mesh = box_mesh("fin", (0.06, 0.3, 1.2), [material("metal.trim", "#c9a46a")])
    bm = bmesh.new()
    bm.from_mesh(fin_mesh)
    upright = [e for e in bm.edges if abs(e.verts[0].co.z - e.verts[1].co.z) > 1.0]
    bmesh.ops.subdivide_edges(bm, edges=upright, cuts=11, use_grid_fill=True)
    bm.to_mesh(fin_mesh)
    bm.free()
    fin = mesh_object("Fin", fin_mesh, rig, location=(0.235, 0, 1.0))
    bpy.context.view_layer.update()
    fin_lower, fin_upper = fin.vertex_groups.new(name="Lower"), fin.vertex_groups.new(name="Upper")
    for v in fin.data.vertices:
        z = (fin.matrix_world @ v.co).z
        t = min(max((z - 0.6) / 0.8, 0), 1)
        if t < 1:
            fin_lower.add([v.index], 1 - t, "REPLACE")
        if t > 0:
            fin_upper.add([v.index], t, "REPLACE")
    fin.modifiers.new("Armature", "ARMATURE").object = rig
    export(path, export_skins=True)


def shapes(path):
    """Cel shading test shapes: a smooth sphere scaled unevenly (normals by the inverse transpose), a
    smooth torus, a flat box in a colour no palette names, a double-sided card seen from behind (its
    back is lit only with the normal flipped), and a smooth tilted cone mirrored (negative scale)."""
    cloth = material("cloth", "#7b5a3a")
    metal = material("metal.base", "#8a8f96")
    metal["hatch"] = 0  # no hatching in its shadow (a material custom property, exported as extras)
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


def glowing(path):
    """A figure with the lizardman's three kinds of special material: a blade that is cel-shaded and
    glows (Principled with emission), a badge drawn flat that glows (an Emission shader) and a pupil
    drawn flat without glow (a Background shader); and a belt lying on its body."""
    cloth = material("cloth", "#5d6f7d")
    skin = material("skin", "#7fa35a")
    blade = glowing_material("metal.base", "#88b5ac", "#bffff0")
    badge = shader_material("badge", "#ffe27a", "emission")
    pupil = shader_material("pupil", "#0d0907", "background")
    mesh_object("Body", cone_mesh("body", 16, 0.26, 0.22, 1.1, True, cloth), location=(0, 0, 0.55))
    mesh_object("Head", uv_sphere_mesh("head", 0.2, skin), location=(0, 0, 1.32))
    mesh_object("Blade", box_mesh("blade", (0.08, 0.05, 0.95), [blade]), location=(0.42, -0.25, 0.95), rotation=(0, 28, 0))
    mesh_object("Badge", box_mesh("badge", (0.14, 0.03, 0.14), [badge]), location=(-0.04, -0.25, 0.85))
    mesh_object("Pupil", uv_sphere_mesh("pupil", 0.035, pupil), location=(0.07, -0.18, 1.36))
    # A belt lying on the body (5 mm off it, 8 cm tall, its sides thinner than a pixel on screen): its
    # edges are creases of its own mesh that only the meshes' feature edges find (the belt and the body
    # look like one surface to a per-pixel test).
    belt = bpy.data.meshes.new("belt")
    bm = bmesh.new()
    segments, inner, outer, low, high = 24, 0.236, 0.247, 0.46, 0.54
    rings = []
    for z in (low, high):
        for r in (outer, inner):
            rings.append([bm.verts.new((r * math.cos(2 * math.pi * k / segments), r * math.sin(2 * math.pi * k / segments), z)) for k in range(segments)])
    out_low, in_low, out_high, in_high = rings
    for k in range(segments):
        n = (k + 1) % segments
        bm.faces.new((out_low[k], out_low[n], out_high[n], out_high[k]))   # outside
        bm.faces.new((in_low[n], in_low[k], in_high[k], in_high[n]))       # inside
        bm.faces.new((out_high[k], out_high[n], in_high[n], in_high[k]))   # top
        bm.faces.new((in_low[k], in_low[n], out_low[n], out_low[k]))       # bottom
    bm.to_mesh(belt)
    bm.free()
    belt.materials.append(material("belt", "#5a4232"))
    mesh_object("Belt", belt)
    export(path)


def armature(name, bones, location=(0, 0, 0)):
    """An armature object with these bones: (name, head, tail, parent, connected[, deform[, roll in degrees]])."""
    data = bpy.data.armatures.new(name.lower())
    rig = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(rig)
    rig.location = location
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    made = {}
    for bone in bones:
        bone_name, head, tail, parent, connected = bone[:5]
        edit = data.edit_bones.new(bone_name)
        edit.head, edit.tail = head, tail
        edit.use_deform = bone[5] if len(bone) > 5 else True
        edit.roll = math.radians(bone[6]) if len(bone) > 6 else 0
        if parent:
            edit.parent = made[parent]
            edit.use_connect = connected
        made[bone_name] = edit
    bpy.ops.object.mode_set(mode="OBJECT")
    return rig


def chain_tube(bm, deform, groups, chain, radii, material_index, segments=10, step=0.04, blend=0.04, depth=1.0):
    """A closed tube along a chain of bones [(bone, head, tail)], its radius going from radii[i] at
    the chain's i-th point to the next (depth scales it front to back), rings about every `step`.
    Each ring follows its bone, blended linearly over `blend` either side of an inner joint (two
    influences at most, as the exporter keeps four)."""
    points = [Vector(chain[0][1])] + [Vector(c[2]) for c in chain]
    lengths = [(points[i + 1] - points[i]).length for i in range(len(chain))]
    starts = [sum(lengths[:i]) for i in range(len(chain))]
    total = sum(lengths)
    count = max(2, round(total / step) + 1)
    rings = []
    for r in range(count):
        s = total * r / (count - 1)
        i = min(len(chain) - 1, max(k for k in range(len(chain)) if starts[k] <= s + 1e-9))
        u = (s - starts[i]) / lengths[i]
        position = points[i].lerp(points[i + 1], u)
        radius = radii[i] + (radii[i + 1] - radii[i]) * u
        direction = (points[i + 1] - points[i]).normalized()
        side = direction.cross(Vector((0, 1, 0)))
        if side.length < 1e-3:
            side = direction.cross(Vector((1, 0, 0)))
        side.normalize()
        front = side.cross(direction).normalized()
        weights = {chain[i][0]: 1.0}
        for j in range(1, len(chain)):
            if abs(s - starts[j]) < blend:
                t = (s - starts[j] + blend) / (2 * blend)
                weights = {chain[j - 1][0]: 1 - t, chain[j][0]: t}
        ring = []
        for k in range(segments):
            a = 2 * math.pi * k / segments
            vert = bm.verts.new(position + radius * (math.cos(a) * side + depth * math.sin(a) * front))
            for bone, w in weights.items():
                if w > 0:
                    vert[deform][groups.index(bone)] = w
            ring.append(vert)
        rings.append(ring)
    for r in range(count - 1):
        for k in range(segments):
            n = (k + 1) % segments
            bm.faces.new((rings[r][k], rings[r][n], rings[r + 1][n], rings[r + 1][k])).material_index = material_index
    bm.faces.new(list(reversed(rings[0]))).material_index = material_index
    bm.faces.new(rings[-1]).material_index = material_index


def weighted(bm, deform, groups, verts, bone):
    for vert in verts:
        vert[deform][groups.index(bone)] = 1.0


def skinned_mesh(name, rig, groups, materials, build):
    """A smooth mesh skinned to the rig: build(bm, deform layer) adds its parts."""
    bm = bmesh.new()
    deform = bm.verts.layers.deform.verify()
    build(bm, deform)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    for m in materials:
        mesh.materials.append(m)
    mesh.shade_smooth()
    obj = mesh_object(name.capitalize(), mesh, rig)
    for group in groups:
        obj.vertex_groups.new(name=group)
    obj.modifiers.new("Armature", "ARMATURE").object = rig
    return obj


def keys(rig, action, frame, bone, rotation=None, location=None, scale=None):
    """Keys a pose bone in this action: rotation as (axis, degrees) in the bone's rest space,
    location in metres and scale, both in the bone's rest space (Blender's pose channels)."""
    rig.animation_data.action = action
    pose = rig.pose.bones[bone]
    pose.rotation_mode = "QUATERNION"
    if rotation is not None:
        pose.rotation_quaternion = Quaternion(rotation[0], math.radians(rotation[1]))
        pose.keyframe_insert("rotation_quaternion", frame=frame)
    if location is not None:
        pose.location = location
        pose.keyframe_insert("location", frame=frame)
    if scale is not None:
        pose.scale = scale
        pose.keyframe_insert("scale", frame=frame)


def curves(action):
    return action.layers[0].strips[0].channelbag(action.slots[0]).fcurves


def stack_actions(rig, actions):
    """Puts each action on an NLA track of its own (the exporter's ACTIONS mode exports those), no
    action active, every bone back at rest."""
    rig.animation_data.action = None
    for action in actions:
        track = rig.animation_data.nla_tracks.new()
        track.name = action.name
        track.strips.new(action.name, int(action.frame_range[0]), action)
    for pose in rig.pose.bones:
        pose.location, pose.rotation_quaternion, pose.scale = (0, 0, 0), (1, 0, 0, 0), (1, 1, 1)


# The test figure's bones (Blender's coordinates: Z up, the figure faces -Y): name, head, tail,
# parent, connected. Mirrored left to right.
FIGURE_BONES = [
    ("hips", (0, 0, 0.95), (0, 0, 1.1), None, False),
    ("spine", (0, 0, 1.1), (0, 0, 1.3), "hips", True),
    ("chest", (0, 0, 1.3), (0, 0, 1.5), "spine", True),
    ("neck", (0, 0, 1.5), (0, 0, 1.6), "chest", True),
    ("head", (0, 0, 1.6), (0, 0, 1.84), "neck", True),
]
for side, sign in (("L", 1), ("R", -1)):
    FIGURE_BONES += [
        (f"upper_arm.{side}", (sign * 0.19, 0, 1.46), (sign * 0.27, 0.02, 1.19), "chest", False),
        (f"forearm.{side}", (sign * 0.27, 0.02, 1.19), (sign * 0.33, -0.02, 0.94), f"upper_arm.{side}", True),
        (f"hand.{side}", (sign * 0.33, -0.02, 0.94), (sign * 0.35, -0.03, 0.84), f"forearm.{side}", True),
        (f"thigh.{side}", (sign * 0.1, 0, 0.95), (sign * 0.11, -0.01, 0.52), "hips", False),
        (f"shin.{side}", (sign * 0.11, -0.01, 0.52), (sign * 0.11, 0.02, 0.1), f"thigh.{side}", True),
        (f"foot.{side}", (sign * 0.11, 0.02, 0.1), (sign * 0.11, -0.13, 0.03), f"shin.{side}", True),
    ]


def figure(path):
    """The animation test figure: a humanoid rig like the game's (hips, spine, chest, neck, head,
    arms, legs), a smooth body skinned to it, and three actions at 60 fps: "idle" (a two-second
    loop: the hips bob, the chest turns and swells by scale, the arms sway, the neck and head nod), "swing"
    (a 0.6 s one-handed swing with eased keys; the exporter writes quaternions with w >= 0, so the
    upper arm's change sign where its local rotation passes 180°) and "twist" (the left hand turns
    270° about its own axis, and the head snaps between poses with constant keys, which export as
    STEP)."""
    bpy.context.scene.render.fps, bpy.context.scene.render.fps_base = 60, 1
    rig = armature("Figure", FIGURE_BONES)
    bone = {b[0]: b for b in FIGURE_BONES}
    groups = [b[0] for b in FIGURE_BONES]
    cloth, skin = material("cloth", "#6a4f7a"), material("skin", "#c49a74")

    def build(bm, deform):
        chain_tube(bm, deform, groups, [("hips", (0, 0, 0.88), (0, 0, 1.1)), ("spine", (0, 0, 1.1), (0, 0, 1.3)),
                                        ("chest", (0, 0, 1.3), (0, 0, 1.52))], [0.13, 0.12, 0.14, 0.18], 0, segments=14, depth=0.65)
        chain_tube(bm, deform, groups, [("neck", (0, 0, 1.49), (0, 0, 1.6)), ("head", (0, 0, 1.6), (0, 0, 1.64))], [0.05, 0.05, 0.05], 1, step=0.03, blend=0.02)
        head = bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=10, radius=0.12, matrix=Matrix.Translation((0, 0, 1.72)))["verts"]
        weighted(bm, deform, groups, head, "head")
        for side in ("L", "R"):
            arm = [(n, bone[n][1], bone[n][2]) for n in (f"upper_arm.{side}", f"forearm.{side}", f"hand.{side}")]
            chain_tube(bm, deform, groups, arm, [0.05, 0.045, 0.04, 0.035], 1)
            leg = [(n, bone[n][1], bone[n][2]) for n in (f"thigh.{side}", f"shin.{side}")]
            chain_tube(bm, deform, groups, leg, [0.075, 0.06, 0.05], 1, segments=12)
            x = 0.11 if side == "L" else -0.11
            foot = bmesh.ops.create_cube(bm, size=1, matrix=Matrix.Translation((x, -0.05, 0.05)) @ Matrix.Diagonal((0.09, 0.22, 0.08, 1)))["verts"]
            weighted(bm, deform, groups, foot, f"foot.{side}")
        for face in bm.faces:
            if face.material_index not in (0, 1):
                face.material_index = 1

    skinned_mesh("body", rig, groups, [cloth, skin], build)
    rig.animation_data_create()

    idle = bpy.data.actions.new("idle")
    for frame, k in ((1, 0), (61, 1), (121, 0)):
        keys(rig, idle, frame, "hips", location=(0, -0.015 * k, 0))
        keys(rig, idle, frame, "chest", rotation=((1, 0, 0), 3 * k), scale=(1 + 0.03 * k, 1, 1 + 0.03 * k))
        keys(rig, idle, frame, "upper_arm.L", rotation=((1, 0, 0), 4 * k))
        keys(rig, idle, frame, "upper_arm.R", rotation=((1, 0, 0), -4 * k))
        keys(rig, idle, frame, "head", rotation=((1, 0, 0), 3 * k))
        keys(rig, idle, frame, "neck", rotation=((1, 0, 0), 2 * k))

    swing = bpy.data.actions.new("swing")
    for frame, arm, fore, twist, lean in ((1, 0, 0, 0, 0), (13, -150, -40, 20, 5), (23, -10, -10, -10, 2), (37, 0, 0, 0, 0)):
        keys(rig, swing, frame, "upper_arm.R", rotation=((1, 0, 0), arm))
        keys(rig, swing, frame, "forearm.R", rotation=((1, 0, 0), fore))
        keys(rig, swing, frame, "chest", rotation=((0, 1, 0), twist))
        keys(rig, swing, frame, "hips", rotation=((1, 0, 0), lean))
    swing.pose_markers.new("hit").frame = 20

    twist = bpy.data.actions.new("twist")
    for frame, turn in ((1, 0), (21, 135), (41, 270), (61, 0)):
        keys(rig, twist, frame, "hand.L", rotation=((0, 1, 0), turn))
    for frame, look in ((1, 0), (16, 20), (31, -20), (46, 0), (61, 0)):
        keys(rig, twist, frame, "head", rotation=((0, 0, 1), look))
    for curve in curves(twist):
        if 'pose.bones["head"]' in curve.data_path:
            for point in curve.keyframe_points:
                point.interpolation = "CONSTANT"

    stack_actions(rig, (idle, swing, twist))
    export(path, **ANIMATED)


def reach(path):
    """An arm of two bones on a base, bent by an IK constraint toward a target bone that the "reach"
    action moves (60 fps, 1 s): only the target is keyed; the arm's rotations come from the IK
    solver and are baked into the exported clip by sampling. The target stays inside the arm's
    reach: at full stretch the solution moves like the square root of the target's motion, which no
    sampling follows between frames."""
    bpy.context.scene.render.fps, bpy.context.scene.render.fps_base = 60, 1
    rig = armature("Reach", [
        ("base", (0, 0, 0), (0, 0, 0.2), None, False),
        ("upper", (0, 0, 0.2), (0, -0.05, 0.75), "base", True),
        ("lower", (0, -0.05, 0.75), (0, 0, 1.3), "upper", True),
        ("target", (0, 0, 1.2), (0, 0, 1.3), None, False, False),
        ("pole", (0, -0.8, 0.75), (0, -0.8, 0.85), None, False, False),
    ])
    groups = ["base", "upper", "lower"]

    def build(bm, deform):
        chain_tube(bm, deform, groups, [("base", (0, 0, 0), (0, 0, 0.2)), ("upper", (0, 0, 0.2), (0, -0.05, 0.75)),
                                        ("lower", (0, -0.05, 0.75), (0, 0, 1.3))], [0.07, 0.06, 0.05, 0.04], 0)

    skinned_mesh("arm", rig, groups, [material("skin", "#c49a74")], build)
    ik = rig.pose.bones["lower"].constraints.new("IK")
    ik.target, ik.subtarget = rig, "target"
    ik.pole_target, ik.pole_subtarget, ik.pole_angle = rig, "pole", math.radians(-90)
    ik.chain_count = 2
    rig.animation_data_create()
    action = bpy.data.actions.new("reach")
    for frame, place in ((1, (0, 0, 0)), (21, (0.35, -0.45, 0.2)), (41, (-0.3, -0.3, 0.3)), (61, (0, 0, 0))):
        keys(rig, action, frame, "target", location=place)
    stack_actions(rig, (action,))
    export(path, **ANIMATED)


# A stockier figure of the same rig family (Docs/Animation.md: clips are shared by bone names): the
# same bone names but other proportions, the upper arms rolled 30° (their own axes turned), no neck
# (the head hangs on the chest) and a bone the figure lacks (a beard).
STOCKY_BONES = [
    ("hips", (0, 0, 0.62), (0, 0, 0.76), None, False),
    ("spine", (0, 0, 0.76), (0, 0, 0.92), "hips", True),
    ("chest", (0, 0, 0.92), (0, 0, 1.1), "spine", True),
    ("head", (0, 0, 1.1), (0, 0, 1.32), "chest", True),
    ("beard", (0, -0.11, 1.17), (0, -0.13, 1.05), "head", False),
]
for side, sign in (("L", 1), ("R", -1)):
    STOCKY_BONES += [
        (f"upper_arm.{side}", (sign * 0.24, 0, 1.06), (sign * 0.38, 0.03, 0.86), "chest", False, True, sign * 30),
        (f"forearm.{side}", (sign * 0.38, 0.03, 0.86), (sign * 0.44, -0.02, 0.68), f"upper_arm.{side}", True),
        (f"hand.{side}", (sign * 0.44, -0.02, 0.68), (sign * 0.46, -0.03, 0.6), f"forearm.{side}", True),
        (f"thigh.{side}", (sign * 0.13, 0, 0.62), (sign * 0.14, -0.01, 0.35), "hips", False),
        (f"shin.{side}", (sign * 0.14, -0.01, 0.35), (sign * 0.14, 0.02, 0.08), f"thigh.{side}", True),
        (f"foot.{side}", (sign * 0.14, 0.02, 0.08), (sign * 0.14, -0.12, 0.02), f"shin.{side}", True),
    ]


def stocky(path):
    """The stocky figure in its rest pose, without actions of its own: the tests play the figure's
    clips on it, and Blender plays the figure's actions on its armature."""
    bpy.context.scene.render.fps, bpy.context.scene.render.fps_base = 60, 1
    rig = armature("Stocky", STOCKY_BONES)
    bone = {b[0]: b for b in STOCKY_BONES}
    groups = [b[0] for b in STOCKY_BONES]
    cloth, skin, hair = material("cloth", "#4f6a3a"), material("skin", "#b8876a"), material("hair", "#6b4a2e")

    def build(bm, deform):
        chain_tube(bm, deform, groups, [("hips", (0, 0, 0.56), (0, 0, 0.76)), ("spine", (0, 0, 0.76), (0, 0, 0.92)),
                                        ("chest", (0, 0, 0.92), (0, 0, 1.12))], [0.17, 0.16, 0.18, 0.21], 0, segments=14, depth=0.7)
        head = bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=10, radius=0.13, matrix=Matrix.Translation((0, 0, 1.22)))["verts"]
        weighted(bm, deform, groups, head, "head")
        beard = bmesh.ops.create_cube(bm, size=1, matrix=Matrix.Translation((0, -0.12, 1.1)) @ Matrix.Diagonal((0.12, 0.05, 0.14, 1)))["verts"]
        weighted(bm, deform, groups, beard, "beard")
        for face in bm.faces:
            if all(v in beard for v in face.verts):
                face.material_index = 2
        for side in ("L", "R"):
            arm = [(n, bone[n][1], bone[n][2]) for n in (f"upper_arm.{side}", f"forearm.{side}", f"hand.{side}")]
            chain_tube(bm, deform, groups, arm, [0.06, 0.055, 0.05, 0.045], 1)
            leg = [(n, bone[n][1], bone[n][2]) for n in (f"thigh.{side}", f"shin.{side}")]
            chain_tube(bm, deform, groups, leg, [0.085, 0.07, 0.06], 1, segments=12)
            x = 0.14 if side == "L" else -0.14
            foot = bmesh.ops.create_cube(bm, size=1, matrix=Matrix.Translation((x, -0.04, 0.04)) @ Matrix.Diagonal((0.1, 0.2, 0.07, 1)))["verts"]
            weighted(bm, deform, groups, foot, f"foot.{side}")

    skinned_mesh("body", rig, groups, [cloth, skin, hair], build)
    export(path, export_skins=True)


# Builders by model name (evaluate_animations.py builds the animated ones again).
BUILDERS = {"blocks": blocks, "rigged": rigged, "shapes": shapes, "stage": stage, "fringe": fringe, "glowing": glowing,
            "figure": figure, "reach": reach, "stocky": stocky}


def main():
    output = sys.argv[sys.argv.index("--") + 1]
    os.makedirs(output, exist_ok=True)
    for name, build in BUILDERS.items():
        reset()
        build(os.path.join(output, name + ".glb"))


if __name__ == "__main__":
    main()

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
    for collection in (bpy.data.objects, bpy.data.meshes, bpy.data.materials, bpy.data.armatures, bpy.data.actions, bpy.data.cameras,
                       bpy.data.lights):
        for block in list(collection):
            collection.remove(block)
    # A stage's timeline markers and their copy (jiruan's) don't carry over to the next model.
    scene = bpy.context.scene
    scene.timeline_markers.clear()
    if "events" in scene:
        del scene["events"]


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


def export_markers(action):
    """Copies the action's pose markers into its custom property "events" (Docs/Animation.md: clip
    events), which the exporter writes as the glTF animation's extras (it exports no markers): a
    list of {name, time}, the time in seconds from the clip's start (the action's first frame, where
    the exporter's slide to zero puts time 0), in frame order."""
    fps = bpy.context.scene.render.fps / bpy.context.scene.render.fps_base
    first = action.frame_range[0]
    markers = sorted(action.pose_markers, key=lambda m: (m.frame, m.name))
    if markers:
        action["events"] = [{"name": m.name, "time": (m.frame - first) / fps} for m in markers]


def stack_actions(rig, actions):
    """Puts each action on an NLA track of its own (the exporter's ACTIONS mode exports those), no
    action active, every bone back at rest; the actions' markers go into their extras."""
    for action in actions:
        export_markers(action)
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
# Socket bones (Docs/Animation.md, "Gear in detail"): non-deforming, an item's grip at the head, its
# axes the bone's (a blade along the bone's Y).
FIGURE_BONES += [
    ("socket.hand.R", (-0.34, -0.06, 0.88), (-0.34, -0.2, 0.92), "hand.R", False, False),
    ("socket.back", (0, 0.12, 1.35), (0.1, 0.13, 1.5), "chest", False, False),
]
DEFORMING = lambda bones: [b[0] for b in bones if len(b) <= 5 or b[5]]


def figure(path):
    """The animation test figure: a humanoid rig like the game's (hips, spine, chest, neck, head,
    arms, legs), a smooth body skinned to it, and three actions at 60 fps: "idle" (a two-second
    loop: the hips bob, the chest turns and swells by scale, the arms sway, the neck and head nod), "swing"
    (a 0.6 s one-handed swing with eased keys; the exporter writes quaternions with w >= 0, so the
    upper arm's change sign where its local rotation passes 180°) and "twist" (the left hand turns
    270° about its own axis, and the head snaps between poses with constant keys, which export as
    STEP); and "pulse" (0.67 s: the head squashes by scale, the hips shift, the chest turns about its
    own Z) for the layer tests."""
    bpy.context.scene.render.fps, bpy.context.scene.render.fps_base = 60, 1
    rig = armature("Figure", FIGURE_BONES)
    bone = {b[0]: b for b in FIGURE_BONES}
    groups = DEFORMING(FIGURE_BONES)
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
    swing.pose_markers.new("whoosh").frame = 9

    twist = bpy.data.actions.new("twist")
    for frame, turn in ((1, 0), (21, 135), (41, 270), (61, 0)):
        keys(rig, twist, frame, "hand.L", rotation=((0, 1, 0), turn))
    for frame, look in ((1, 0), (16, 20), (31, -20), (46, 0), (61, 0)):
        keys(rig, twist, frame, "head", rotation=((0, 0, 1), look))
    for curve in curves(twist):
        if 'pose.bones["head"]' in curve.data_path:
            for point in curve.keyframe_points:
                point.interpolation = "CONSTANT"

    # Big enough to tell the NLA's Combine rules apart: scales multiply by powers, locations add,
    # rotations about other axes than the swing's multiply in order.
    pulse = bpy.data.actions.new("pulse")
    for frame, k in ((1, 0), (21, 1), (41, 0)):
        keys(rig, pulse, frame, "head", scale=(1 + 0.4 * k, 1 - 0.3 * k, 1 + 0.4 * k))
        keys(rig, pulse, frame, "hips", location=(0.05 * k, 0, 0.02 * k))
        keys(rig, pulse, frame, "chest", rotation=((0, 0, 1), 25 * k))

    # Markers on the first and the last frame, and two at one frame.
    pulse.pose_markers.new("start").frame = 1
    pulse.pose_markers.new("end").frame = 41
    idle.pose_markers.new("breath").frame = 61
    idle.pose_markers.new("blink").frame = 61

    stack_actions(rig, (idle, swing, twist, pulse))
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
STOCKY_BONES += [
    ("socket.hand.R", (-0.45, -0.06, 0.64), (-0.45, -0.2, 0.68), "hand.R", False, False),
    ("socket.back", (0, 0.16, 0.98), (0.1, 0.17, 1.12), "chest", False, False),
]


def stocky(path):
    """The stocky figure in its rest pose, without actions of its own: the tests play the figure's
    clips on it, and Blender plays the figure's actions on its armature."""
    bpy.context.scene.render.fps, bpy.context.scene.render.fps_base = 60, 1
    rig = armature("Stocky", STOCKY_BONES)
    bone = {b[0]: b for b in STOCKY_BONES}
    groups = DEFORMING(STOCKY_BONES)
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


def sword(path):
    """A sword for the hand socket: its origin is the grip, the blade along +Y (Blender's object
    axes, which the socket bone's axes match), a guard across X, metal and cloth slots."""
    blade = mesh_object("Blade", box_mesh("blade", (0.05, 0.8, 0.012), [material("metal.base", "#a8b0b8")]), location=(0, 0.5, 0))
    mesh_object("Guard", box_mesh("guard", (0.2, 0.03, 0.03), [material("metal.trim", "#c9a46a")]), blade, location=(0, -0.4, 0))
    mesh_object("Grip", cone_mesh("grip", 8, 0.018, 0.018, 0.2, True, material("cloth", "#5a3a2a")), location=(0, 0, 0), rotation=(-90, 0, 0))
    export(path)


def tabard_mesh(rig, groups):
    """A tabard skinned to a rig of the figure's bones: a front and a back panel over the torso down
    to mid-thigh, each cut into a grid; weighted by height to chest, spine and hips, below the hips
    blending into the thigh of its side (two influences at most)."""
    cloth = material("cloth", "#8a2f2f", culled=False)

    def weights(x, z):
        if z >= 1.3:
            return {"chest": 1.0}
        if z >= 1.1:
            t = (z - 1.1) / 0.2
            return {"spine": 1 - t, "chest": t} if t > 0 else {"spine": 1.0}
        if z >= 0.95:
            t = (z - 0.95) / 0.15
            return {"hips": 1 - t, "spine": t} if t > 0 else {"hips": 1.0}
        thigh = "thigh.L" if x > 0 else "thigh.R"
        t = min((0.95 - z) / 0.35, 1) * min(abs(x) / 0.12, 1)
        return {"hips": 1 - t, thigh: t} if t > 0 else {"hips": 1.0}

    def build(bm, deform):
        for y, flip in ((-0.15, False), (0.14, True)):
            columns, rows = 6, 12
            grid = [[None] * (columns + 1) for _ in range(rows + 1)]
            for r in range(rows + 1):
                z = 0.6 + 0.85 * r / rows
                for c in range(columns + 1):
                    x = -0.14 + 0.28 * c / columns
                    vert = bm.verts.new((x, y, z))
                    for bone_name, w in weights(x, z).items():
                        vert[deform][groups.index(bone_name)] = w
                    grid[r][c] = vert
            for r in range(rows):
                for c in range(columns):
                    quad = (grid[r][c], grid[r][c + 1], grid[r + 1][c + 1], grid[r + 1][c])
                    bm.faces.new(tuple(reversed(quad)) if flip else quad)

    bm = bmesh.new()
    deform = bm.verts.layers.deform.verify()
    build(bm, deform)
    mesh = bpy.data.meshes.new("tabard")
    bm.to_mesh(mesh)
    bm.free()
    mesh.materials.append(cloth)
    obj = mesh_object("Tabard", mesh, rig)
    for group in groups:
        obj.vertex_groups.new(name=group)
    obj.modifiers.new("Armature", "ARMATURE").object = rig
    return obj


def tabard(path):
    """The tabard on a rig of the figure's bones (the body it's made on), exported with that rig: the
    engine binds its joints to a wearer's bones by name."""
    rig = armature("Figure", FIGURE_BONES)
    tabard_mesh(rig, DEFORMING(FIGURE_BONES))
    export(path, export_skins=True)


def pennant(path):
    """An item with bones of its own: a pole (its origin the grip, along +Y) with a flag on a second
    bone, and a clip named like the wearer's "swing" state, in which the flag waves (60 fps)."""
    bpy.context.scene.render.fps, bpy.context.scene.render.fps_base = 60, 1
    rig = armature("Pennant", [
        ("pole", (0, -0.1, 0), (0, 1.1, 0), None, False),
        ("flag", (0, 1.1, 0), (0.3, 1.1, 0), "pole", True),
    ])
    groups = ["pole", "flag"]

    def build(bm, deform):
        chain_tube(bm, deform, groups, [("pole", (0, -0.1, 0), (0, 1.1, 0))], [0.012, 0.012], 0, segments=6, step=0.1)
        columns, rows = 6, 3
        grid = [[None] * (columns + 1) for _ in range(rows + 1)]
        for r in range(rows + 1):
            for c in range(columns + 1):
                vert = bm.verts.new((0.35 * c / columns, 1.1 - 0.25 * r / rows, 0))
                t = min(c / 2, 1)
                if t < 1:
                    vert[deform][groups.index("pole")] = 1 - t
                if t > 0:
                    vert[deform][groups.index("flag")] = t
                grid[r][c] = vert
        for r in range(rows):
            for c in range(columns):
                bm.faces.new((grid[r][c], grid[r][c + 1], grid[r + 1][c + 1], grid[r + 1][c])).material_index = 1

    skinned_mesh("pennant", rig, groups, [material("wood", "#7a5a3a"), material("cloth", "#3a5a8a", culled=False)], build)
    rig.animation_data_create()
    swing = bpy.data.actions.new("swing")
    for frame, angle in ((1, 0), (11, 35), (25, -25), (37, 0)):
        keys(rig, swing, frame, "flag", rotation=((0, 1, 0), angle))
    stack_actions(rig, (swing,))
    export(path, **ANIMATED)


def parent_in_place(child, parent, bone=None):
    """Parents as Blender's Ctrl+P does: the child stays where it is, its parent inverse undoing the
    parent's world matrix at that moment; for a bone, the bone's tail at rest (Blender places a bone's
    children at its tail: the pose matrix moved along its Y by the bone's length)."""
    bpy.context.view_layer.update()
    if bone is None:
        inverse = parent.matrix_world.inverted()
    else:
        b = parent.data.bones[bone]
        inverse = (parent.matrix_world @ b.matrix_local @ Matrix.Translation((0, b.length, 0))).inverted()
    child.parent = parent
    if bone is not None:
        child.parent_type = "BONE"
        child.parent_bone = bone
    child.matrix_parent_inverse = inverse


def object_keys(obj, action, frame, location=None, rotation=None, scale=None):
    """Keys an object's own channels in this action: location, rotation (Euler XYZ degrees, Blender's
    default rotation mode for objects) and scale."""
    if obj.animation_data is None:
        obj.animation_data_create()
    obj.animation_data.action = action
    if location is not None:
        obj.location = location
        obj.keyframe_insert("location", frame=frame)
    if rotation is not None:
        obj.rotation_euler = [math.radians(a) for a in rotation]
        obj.keyframe_insert("rotation_euler", frame=frame)
    if scale is not None:
        obj.scale = scale
        obj.keyframe_insert("scale", frame=frame)


def stack_object_actions(obj, actions, location, rotation, scale):
    """An object's actions on NLA tracks of their own (as stack_actions does for an armature), no
    action active, the object back at its rest transform (the exporter writes the node's own
    transform as the object has it then)."""
    for action in actions:
        export_markers(action)
    obj.animation_data.action = None
    for action in actions:
        track = obj.animation_data.nla_tracks.new()
        track.name = action.name
        track.strips.new(action.name, int(action.frame_range[0]), action)
    obj.location, obj.rotation_euler, obj.scale = location, [math.radians(a) for a in rotation], scale


# The hierarchy test model's still transforms (Blender's coordinates), and the animated parent's rest.
BASE_REST = ((0, 0, 0.1), (0, 0, 25), (1.5, 1.5, 1.5))


def hierarchy(path):
    """Object parenting as the exporter gives it back (Docs/Scene.md, step 1), each top-level object a
    model of its own in the scene: an animated parent ("Base", the action "base" moving, turning and
    scaling it unevenly) above a child parented in place ("Post", a parent inverse) and a plain
    grandchild ("Tip"), both still; non-uniform scale above a turned child ("Squash" and "Tilted": a
    sheared world, which no translation, rotation and scale holds, though each local does); a mirrored
    branch ("Mirror", an empty scaled −1 along X, over "Wing" and "Feather", the feather parented in
    place, so its parent inverse is mirrored too); and a bone parent ("Charm" on the bone "hand" of
    the armature "Rig", parented in place: Blender places it by the bone's tail), the action "rig"
    turning both bones. 60 fps, keys from frame 1 (Blender's default Bezier interpolation)."""
    bpy.context.scene.render.fps, bpy.context.scene.render.fps_base = 60, 1
    stone, metal, trim = material("stone", "#6b6258"), material("metal.base", "#8a8f96"), material("metal.trim", "#c9a46a")
    cloth, skin = material("cloth", "#7b5a3a"), material("skin", "#a9805e")

    location, rotation, scale = BASE_REST
    base = mesh_object("Base", box_mesh("base", (0.7, 0.5, 0.2), [stone, cloth]), location=location, rotation=rotation, scale=scale)
    post = mesh_object("Post", cone_mesh("post", 10, 0.06, 0.05, 0.7, True, metal), location=(0.25, 0.12, 0.6), rotation=(12, 0, 0))
    parent_in_place(post, base)
    mesh_object("Tip", cone_mesh("tip", 8, 0.13, 0.0, 0.22, False, trim), post, location=(0, 0, 0.45), rotation=(0, 0, 20))

    squash = mesh_object("Squash", box_mesh("squash", (0.3, 0.3, 0.3), [stone]), location=(0.6, -0.8, 0.35), rotation=(0, 0, -15), scale=(1.6, 0.7, 1.0))
    mesh_object("Tilted", box_mesh("tilted", (0.3, 0.3, 0.3), [cloth]), squash, location=(0, 0, 0.5), rotation=(35, 0, 40))

    mirror = bpy.data.objects.new("Mirror", None)
    bpy.context.scene.collection.objects.link(mirror)
    mirror.location, mirror.scale = (-0.8, 0.2, 0), (-1, 1, 1)
    wing = mesh_object("Wing", box_mesh("wing", (0.5, 0.2, 0.15), [metal, trim]), mirror, location=(0.3, 0, 0.45), rotation=(0, 25, 35))
    feather = mesh_object("Feather", cone_mesh("feather", 12, 0.09, 0.02, 0.4, True, skin), location=(-1.05, 0.1, 0.85), rotation=(0, -30, 10))
    parent_in_place(feather, wing)

    rig = armature("Rig", [("arm", (0, 0, 0), (0, 0, 0.6), None, False), ("hand", (0, 0, 0.6), (0.25, 0, 0.85), "arm", True)], location=(0.1, 0.9, 0))
    charm = mesh_object("Charm", box_mesh("charm", (0.16, 0.16, 0.24), [trim]), location=(0.35, 0.9, 0.95), rotation=(0, 15, 0))
    parent_in_place(charm, rig, bone="hand")

    turn = bpy.data.actions.new("base")
    for frame, k in ((1, 0), (31, 1), (61, 0)):
        object_keys(base, turn, frame, location=(0.3 * k, -0.2 * k, 0.1 + 0.15 * k), rotation=(10 * k, 0, 25 + 45 * k),
                    scale=(1.5, 1.5 - 0.3 * k, 1.5 + 0.4 * k))
    stack_object_actions(base, (turn,), *BASE_REST)

    rig.animation_data_create()
    bend = bpy.data.actions.new("rig")
    for frame, k in ((1, 0), (31, 1), (61, 0)):
        keys(rig, bend, frame, "arm", rotation=((1, 0, 0), 35 * k))
        keys(rig, bend, frame, "hand", rotation=((0, 0, 1), -50 * k))
    stack_actions(rig, (bend,))
    export(path, **ANIMATED)


# Stages (Docs/Scene.md, step 3; Q10–Q13, S3-1, S3-2): exported in the scene mode, the whole timeline
# one clip named after the Blender scene with every animated object on it ("Split Animation by Object"
# off; it is on by default), with cameras and lights, the lighting units raw (Blender's own strengths).
STAGE = dict(ANIMATED, export_animation_mode="SCENE", export_anim_scene_split_object=False, export_cameras=True, export_lights=True,
             export_import_convert_lighting_mode="RAW")


def export_stage(path):
    """Exports the scene as a stage: each sun's angle (Blender's own, which its renders use for soft
    shadows) copied into the light's custom property "angle" in degrees, which the exporter writes as
    the light's extras (S3-2); then the orthographic cameras' magnifications put right (S3-1)."""
    for light in bpy.data.lights:
        if light.type == "SUN":
            light["angle"] = math.degrees(light.angle)
    export(path, **STAGE)
    if path is not None:
        fix_orthographic(path)


def fix_orthographic(path):
    """The exporter writes an orthographic camera's xmag and ymag as if its sensor fit were Auto (its
    scale across the larger side of the render size, cameras.py), but a stage camera's sensor fit is
    Vertical (Q10): its scale is the height in view whatever the aspect. Each orthographic camera's
    magnifications are written again by Blender's rule (BKE_camera_params_compute_viewplane: the
    scale spans the side its sensor fit names, Auto naming the larger) at the render size's aspect,
    and the .glb put together again as the exporter writes it (compact JSON, padded with spaces)."""
    render = bpy.context.scene.render
    width, height = render.resolution_x * render.pixel_aspect_x, render.resolution_y * render.pixel_aspect_y
    data = open(path, "rb").read()
    length = struct.unpack_from("<I", data, 12)[0]
    gltf = json.loads(data[20:20 + length])
    cameras = [c for c in gltf.get("cameras", []) if c["type"] == "orthographic"]
    if not cameras:
        return
    for camera in cameras:
        source = bpy.data.cameras[camera["name"]]
        fit = source.sensor_fit
        if fit == "AUTO":
            fit = "HORIZONTAL" if width >= height else "VERTICAL"
        half = source.ortho_scale / 2
        xmag, ymag = (half, half * height / width) if fit == "HORIZONTAL" else (half * width / height, half)
        camera["orthographic"]["xmag"], camera["orthographic"]["ymag"] = xmag, ymag
    text = json.dumps(gltf, separators=(",", ":"), allow_nan=False).encode()
    text += b" " * (-len(text) % 4)
    binary = data[20 + length:]
    open(path, "wb").write(b"glTF" + struct.pack("<III", 2, 20 + len(text) + len(binary), len(text)) + b"JSON" + text + binary)


def dolly(path):
    """A stage (Docs/Scene.md, step 3): a ground and three still objects (scenery); the empty "Dolly"
    moving and turning over 61 frames (1 s at 60 fps, the scene's whole timeline) with the perspective
    camera "Camera" on it (lens 35 mm on a 24 mm sensor, sensor fit Vertical; its own tilt and pan
    keyed), the still orthographic camera "Plan" looking down at 45° (scale 6, sensor fit Vertical),
    and the sun "Sun" (strength 2.5, angle 4°: neither the engine's default) shining from the camera's right. Keys in Blender's default
    Bezier. Exported as a stage (export_stage): one clip, "Scene", at the render size 1920 × 1080."""
    scene = bpy.context.scene
    scene.render.fps, scene.render.fps_base = 60, 1
    scene.frame_start, scene.frame_end = 1, 61
    scene.render.resolution_x, scene.render.resolution_y = 1920, 1080
    scene.render.pixel_aspect_x = scene.render.pixel_aspect_y = 1
    ground, stone = material("ground", "#9a8a68"), material("stone", "#6b6258")
    metal, cloth, trim = material("metal.base", "#8a8f96"), material("cloth", "#7b5a3a"), material("metal.trim", "#c9a46a")
    mesh_object("Ground", box_mesh("ground", (6, 4, 0.2), [ground]), location=(0, 0, -0.1))
    bm = bmesh.new()
    bm.loops.layers.uv.new("UVMap")
    bmesh.ops.create_icosphere(bm, subdivisions=2, radius=0.4, calc_uvs=True)
    rock = bpy.data.meshes.new("rock")
    bm.to_mesh(rock)
    bm.free()
    rock.materials.append(stone)
    rock.shade_smooth()
    mesh_object("Rock", rock, location=(-1.3, 0.7, 0.2), scale=(1, 0.8, 0.7))
    mesh_object("Pillar", cone_mesh("pillar", 12, 0.2, 0.2, 1.4, True, metal), location=(1.0, 1.0, 0.7))
    mesh_object("Crate", box_mesh("crate", (0.6, 0.6, 0.6), [cloth, trim]), location=(0.1, -0.4, 0.3), rotation=(0, 0, 20))

    rig = bpy.data.objects.new("Dolly", None)
    scene.collection.objects.link(rig)
    lens = bpy.data.cameras.new("Lens")
    lens.sensor_fit, lens.sensor_height, lens.lens = "VERTICAL", 24, 35
    lens.clip_start, lens.clip_end = 0.1, 40
    camera = bpy.data.objects.new("Camera", lens)
    scene.collection.objects.link(camera)
    camera.parent = rig
    camera.location = (0, 0, 1.6)
    move, look = bpy.data.actions.new("move"), bpy.data.actions.new("look")
    for frame, location, turn in ((1, (0.4, -6.0, 0), 0), (31, (-0.2, -5.6, 0.15), 6), (61, (-0.8, -5.0, 0.3), -10)):
        object_keys(rig, move, frame, location=location, rotation=(0, 0, turn))
    for frame, rotation in ((1, (78, 0, 0)), (31, (75, 0, 4)), (61, (76, 0, -6))):
        object_keys(camera, look, frame, rotation=rotation)

    plan = bpy.data.cameras.new("Plan")
    plan.type, plan.sensor_fit, plan.ortho_scale = "ORTHO", "VERTICAL", 6
    plan.clip_start, plan.clip_end = 0.5, 30
    mesh_object("Plan", plan, location=(0, -5, 6), rotation=(45, 0, 0))

    light = bpy.data.lights.new("Daylight", "SUN")
    light.energy, light.angle = 2.5, math.radians(4)
    mesh_object("Sun", light, location=(0, 0, 5), rotation=(35, 15, 30))
    export_stage(path)


# Ji'ruan's intro stage (Docs/Scene.md, step 7, phase 1; Q30, Q31; M10-11): every element of the old
# intro's SVG scene (old-game/scripts/boss-intros.js, "jiruan") remade simply, its motion keyed from the
# old CSS timeline (old-game/styles.css, "Boss intro cutscenes": each keyframe's percentage × 7 s, put
# on the nearest 60 fps frame), seen through a perspective camera on a dolly that frames the old
# viewBox (1200 × 680) as the CSS's first frame does (the art scaled 1.02 about 50 %, 62 %) and pushes
# in as bi-camera does. Placement: each SVG element is put at a chosen depth along the first frame's
# camera, at the point that camera shows where the SVG had it (svg_point), so the first frame keeps the
# old composition; the depths are the remake's own (the SVG has none). Blender's coordinates: the
# camera looks along +Y, Z up, the rift 14 m ahead of the camera's start.
JR_FPS, JR_SECONDS = 60, 7.0
JR_CAMERA = (0.0, -14.0, 1.6)        # the dolly's start: 14 m before the rift, eye height 1.6 m
JR_PITCH = 0.0                       # level: the boss's feet 12 m off fall where the old ground line was (83 %)
JR_LENS, JR_SENSOR = 61.0, 24.0      # sensor fit Vertical: tan(fov / 2) = 12 / 61
JR_WIDTH, JR_HEIGHT = 1200, 680      # the old SVG's viewBox


def jr_frame(seconds):
    """A time of the old timeline on the stage's frames (frame 1 is 0 s)."""
    return 1 + round(seconds * JR_FPS)


def jr_camera_matrix():
    return Matrix.Translation(JR_CAMERA) @ Matrix.Rotation(math.radians(90 + JR_PITCH), 4, "X")


def svg_ray(x, y):
    """The first frame's camera ray (origin, direction; Blender's world) through the SVG point (x, y):
    the CSS shows the art scaled 1.02 about (600, 421.6), and the camera's vertical extent is the
    viewBox's height."""
    sx, sy = 600 + 1.02 * (x - 600), 421.6 + 1.02 * (y - 421.6)
    t = (JR_SENSOR / 2) / JR_LENS
    local = Vector(((sx - 600) / 340 * t, (340 - sy) / 340 * t, -1))
    m = jr_camera_matrix()
    return m.translation.copy(), (m.to_3x3() @ local)


def svg_point(x, y, depth):
    """The point at this depth (metres along the view) that the first frame shows at SVG (x, y)."""
    origin, direction = svg_ray(x, y)
    forward = jr_camera_matrix().to_3x3() @ Vector((0, 0, -1))
    return origin + direction * (depth / direction.dot(forward))


def svg_ground(x, y):
    """Where the first frame's ray through SVG (x, y) meets the ground (z = 0)."""
    origin, direction = svg_ray(x, y)
    return origin + direction * (-origin.z / direction.z)


def jr_keys(obj, data_path, keys, interpolation=None):
    """Keys a channel (every component) at (seconds, value) pairs: Blender's default Bezier unless an
    interpolation is named. Keys past the timeline's end (a CSS delay pushes some there) become one key at
    the end with the value between its neighbours: the export bakes the timeline only, so Blender must
    not move on after it."""
    if obj.animation_data is None:
        obj.animation_data_create()
    if keys[-1][0] > JR_SECONDS:
        inside = [k for k in keys if k[0] <= JR_SECONDS]
        (t0, v0), (t1, v1) = inside[-1], next(k for k in keys if k[0] > JR_SECONDS)
        u = (JR_SECONDS - t0) / (t1 - t0)
        keys = inside + [(JR_SECONDS, tuple(a + (b - a) * u for a, b in zip(v0, v1)))]
    for seconds, value in keys:
        setattr(obj, data_path, value)
        obj.keyframe_insert(data_path, frame=jr_frame(seconds))
    if interpolation:
        action = obj.animation_data.action
        for curve in action.layers[0].strips[0].channelbag(action.slots[0]).fcurves:
            if curve.data_path == data_path:
                for point in curve.keyframe_points:
                    point.interpolation = interpolation


def slab_mesh(name, outline, thickness, m):
    """A flat slab: the outline (points in Blender's XZ plane) extruded by the thickness along Y."""
    count = len(outline)
    front = [(x, -thickness / 2, z) for x, z in outline]
    back = [(x, thickness / 2, z) for x, z in outline]
    faces = [tuple(range(count - 1, -1, -1)), tuple(range(count, 2 * count))]
    faces += [(i, (i + 1) % count, count + (i + 1) % count, count + i) for i in range(count)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(front + back, [], faces)
    mesh.update()
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    mesh.materials.append(m)
    return mesh


def svg_outline(points, depth, centre):
    """SVG points at a depth as an outline in the XZ plane about a centre (Blender's world)."""
    return [((p - centre).x, (p - centre).z) for p in (svg_point(x, y, depth) for x, y in points)]


def empty(name, location=(0, 0, 0), parent=None, scale=(1, 1, 1), rotation=(0, 0, 0)):
    obj = bpy.data.objects.new(name, None)
    bpy.context.scene.collection.objects.link(obj)
    obj.parent = parent
    obj.location, obj.scale = location, scale
    obj.rotation_euler = [math.radians(a) for a in rotation]
    return obj


def jiruan(path):
    """Ji'ruan's intro stage (see above). Objects: "sky" (three unlit bands), "ground", the far trunks
    "trunk-far-1".."6" and the mid trunks "trunk-mid-1".."4" at the old SVG's places (the four the rift
    lights carry an unlit rim strip "…-rim"), "trunk-ring-1".."10" closing the ring of trees out of
    view, the rift "rift" (an empty at its middle, opening by scale) over "rift-bloom" (pulsing),
    "rift-tear" and "rift-core" (Emission: they glow), the cracks "crack-1".."9" (glowing strips on the
    ground drawn out by scale), the eyes "eye-1".."8" (each two glowing slits, opening, blinking and
    narrowing by scale), the foreground "near-1".."4" (unlit silhouettes), the marks "boss" and
    "imp-1".."imp-4" where the figures stand (they carry the stand-ins' sizes), the emitter marks "mist"
    and "embers", the camera "camera" on the empty "dolly", and the sun "sun". The old CSS's moments
    are the scene's timeline markers. Exported as a stage (export_stage) at the render size 1200 × 680,
    the scene's custom property "events" holding the markers (Docs/Scene.md, step 4, "Markers")."""
    scene = bpy.context.scene
    scene.render.fps, scene.render.fps_base = JR_FPS, 1
    scene.frame_start, scene.frame_end = 1, jr_frame(JR_SECONDS)
    scene.render.resolution_x, scene.render.resolution_y = JR_WIDTH, JR_HEIGHT
    scene.render.pixel_aspect_x = scene.render.pixel_aspect_y = 1

    # The night sky behind the trees: the old gradient's lower stops as flat bands (unlit, no ink).
    mesh = bpy.data.meshes.new("sky")
    verts, faces = [], []
    bands = (("sky.low", "#22142f", -1, 7), ("sky.mid", "#1a0f27", 7, 13), ("sky.high", "#140a20", 13, 30))
    for name, colour, low, high in bands:
        base = len(verts)
        verts += [(-70, 0, low), (70, 0, low), (70, 0, high), (-70, 0, high)]
        faces.append((base, base + 1, base + 2, base + 3))
    mesh.from_pydata(verts, [], faces)
    for name, colour, _, _ in bands:
        mesh.materials.append(shader_material(name, colour, "background"))
    for index, polygon in enumerate(mesh.polygons):
        polygon.material_index = index
    mesh_object("sky", mesh, location=(0, 32, 0))["ink"] = 0

    ground = material("ground", "#0a0610")
    mesh_object("ground", box_mesh("ground", (120, 120, 0.2), [ground]), location=(0, 0, -0.1))

    # Trunks: (SVG x, width, lean, colour, rimmed) at a depth, their widths and leans the SVG's there.
    bark = {c: material("bark." + str(i + 1), c) for i, c in enumerate(("#170e22", "#1a1026", "#1d122b", "#0c0713", "#0d0815"))}
    rim = shader_material("rim", "#6a44a0", "background")

    def trunk(name, x, w, lean, colour, rimmed, depth):
        foot = svg_point(x, 680, depth)
        px = (svg_point(x + 1, 680, depth) - foot).length
        height = 18.0
        r1, r2 = w / 2 * px, w * 0.22 * px
        tilt = math.degrees(math.atan2(lean * px, 700 * px))
        obj = mesh_object(name, cone_mesh(name, 12, r1, r2, height, True, bark[colour]),
                          location=(foot.x, foot.y, height / 2 - 1), rotation=(0, tilt, 0))
        if rimmed:
            # The rift's light on the side facing it: a thin flat strip (the SVG's rim stroke).
            side = -1 if x > 600 else 1
            strip = mesh_object(name + "-rim", box_mesh(name + "-rim", (r1 * 0.08, r1 * 0.2, height), [rim]), obj,
                                location=(side * r1 * 0.78, -r1 * 0.55, 0))
            strip["ink"] = 0
    for i, spec in enumerate(((70, 60, -8, "#170e22", False), (250, 46, 10, "#1a1026", False), (420, 38, -6, "#1d122b", True),
                              (790, 40, 8, "#1d122b", True), (960, 52, -10, "#1a1026", False), (1130, 64, 6, "#170e22", False))):
        trunk("trunk-far-%d" % (i + 1), *spec, 30)
    for i, spec in enumerate(((-10, 110, 14, "#0c0713", False), (160, 74, -12, "#0d0815", True),
                              (1040, 80, 12, "#0d0815", True), (1215, 120, -14, "#0c0713", False))):
        trunk("trunk-mid-%d" % (i + 1), *spec, 9)
    # The rest of the ring of trees about the clearing, behind and beside the camera (out of its view).
    for i in range(10):
        a = math.radians(200 + i * 14)
        mesh_object("trunk-ring-%d" % (i + 1), cone_mesh("ring-%d" % (i + 1), 12, 0.45, 0.2, 18.0, True, bark["#0c0713"]),
                    location=(17 * math.cos(a), -6 + 17 * math.sin(a), 8))

    # The rift: an empty at its middle (the CSS scales it about its box's centre) over three glowing slabs.
    centre = svg_point(600, 366, 14)
    rift = empty("rift", location=centre)
    bloom_points = [(600, 120), (640, 220), (618, 300), (660, 390), (626, 470), (652, 560), (600, 612), (548, 560), (574, 470), (540, 390), (582, 300), (560, 220)]
    # Solid where the SVG's bloom was blurred and translucent: narrowed to three quarters of its width.
    bloom_points = [(600 + 0.75 * (x - 600), y) for x, y in bloom_points]
    tear_points = [(600, 136), (624, 214), (608, 266), (638, 338), (614, 404), (642, 474), (618, 542), (626, 600), (600, 592), (576, 542), (594, 474), (560, 404), (588, 338), (564, 266), (590, 214)]
    core_points = [(600, 150), (607, 214), (603, 266), (612, 338), (604, 404), (612, 474), (605, 542), (602, 588), (598, 588), (595, 542), (588, 474), (596, 404), (588, 338), (597, 266), (593, 214)]
    # bi-rift-open (ease-out): closed (hidden: a point; the CSS 0.4 × 0 at opacity 0) to 0.42 s, open at
    # 1.96 s, widened at 3.92 s, settled at 7 s. The slabs are top-level objects keyed alike about the
    # rift's middle (no child under a parent scaled to nearly nothing); the empty "rift" marks the place.
    opening = [(0, (0.001, 1, 0.001)), (0.42, (0.001, 1, 0.001)), (1.96, (1, 1, 1)), (3.92, (1.5, 1, 1.04)), (7.0, (1.25, 1, 1))]
    jr_keys(rift, "scale", opening)
    # bi-rift-pulse: the bloom 1.4 s each way, forever (its opacity, here its size), times the opening.
    pulse = [(k * 1.4, (1.0, 1, 1.0) if k % 2 == 0 else (1.12, 1, 1.04)) for k in range(6)]

    def between(keys, t):
        (t0, v0), (t1, v1) = max((k for k in keys if k[0] <= t), key=lambda k: k[0]), min((k for k in keys if k[0] >= t), key=lambda k: k[0])
        u = 0 if t1 == t0 else (t - t0) / (t1 - t0)
        return [a + (b - a) * u for a, b in zip(v0, v1)]
    bloom_keys = [(t, tuple(a * b for a, b in zip(between(opening, t), between(pulse, t)))) for t in sorted({k[0] for k in opening + pulse})]
    for name, points, colour, ahead, keys in (("rift-bloom", bloom_points, "#b48cff", 0, bloom_keys), ("rift-tear", tear_points, "#7a4cc8", -0.05, opening),
                                              ("rift-core", core_points, "#fbf6ff", -0.1, opening)):
        m = shader_material(name.replace("-", "."), colour, "emission")
        part = mesh_object(name, slab_mesh(name, svg_outline(points, 14, centre), 0.04, m), location=centre + Vector((0, ahead, 0)))
        part["ink"] = 0
        jr_keys(part, "scale", keys)

    # The cracks: each segment of the SVG's crack path a glowing strip on the ground from the rift's
    # foot, its origin at its start, drawn out by its length's scale as the dash offset (260 → 0 over
    # 1.26–2.94 s) reaches it (linearly).
    crack = shader_material("crack", "#c58cff", "emission")
    foot = Vector((centre.x, centre.y, 0))  # the rift's foot: it stands on the ground, its lower tip sunk in it
    paths = [[(600, 600), (548, 632), (492, 640)], [(600, 600), (656, 634), (722, 646)], [(600, 600), (582, 652), (590, 690)],
             [(600, 600), (640, 668)], [(548, 632), (520, 676)], [(656, 634), (690, 686)]]
    starts = {(548, 632): math.hypot(52, 32), (656, 634): math.hypot(56, 34)}
    number = 0
    for line in paths:
        along = starts.get(line[0], 0.0)
        for a, b in zip(line, line[1:]):
            # On the ground about the foot: across at 12 mm per SVG pixel, toward the camera at 50 mm.
            pa = foot + Vector(((a[0] - 600) * 0.012, -(a[1] - 600) * 0.05, 0.012))
            pb = foot + Vector(((b[0] - 600) * 0.012, -(b[1] - 600) * 0.05, 0.012))
            length = (pb - pa).length
            number += 1
            strip_mesh = box_mesh("crack-%d" % number, (length, 0.07, 0.01), [crack])
            for v in strip_mesh.vertices:
                v.co.x += length / 2
            obj = mesh_object("crack-%d" % number, strip_mesh, location=pa, rotation=(0, 0, math.degrees(math.atan2((pb - pa).y, (pb - pa).x))))
            obj["ink"] = 0
            seg = math.hypot(b[0] - a[0], b[1] - a[1])
            t0, t1 = 1.26 + 1.68 * min(along, 260) / 260, 1.26 + 1.68 * min(along + seg, 260) / 260
            jr_keys(obj, "scale", [(0, (0.001, 0.001, 1)), (t0, (0.001, 0.001, 1)), (max(t1, t0 + 1 / 60), (1, 1, 1))], interpolation="LINEAR")
            along += seg

    # The eyes in the dark: each pair of slits on its node 15 m from the camera (1 m behind the rift's
    # depth) at the SVG's place, at least 0.5 m up; bi-eye (ease-out, each its own delay): closed to
    # 0.98 s, open at 1.4, blinking at 1.68, open 1.89–4.9, narrowed to 0.35 (the old dimming) by 5.95.
    eye = shader_material("eye", "#d6b4ff", "emission")
    for i, (x, y, delay) in enumerate(((188, 402, 0), (298, 470, 0.35), (394, 356, 0.7), (834, 384, 0.2), (948, 452, 0.55), (1052, 362, 0.9), (702, 300, 1.1), (470, 286, 1.3))):
        place = svg_point(x, y, 15)
        place.z = max(place.z, 0.5)
        px = (svg_point(x + 1, y, 15) - svg_point(x, y, 15)).length
        # Both slits in the node's own mesh (no child under a parent scaled to nearly nothing).
        bm = bmesh.new()
        for side in (-1, 1):
            tri = [(side * 10 * px, 0), (side * 1 * px, -1 * px), (side * 3 * px, 3 * px)]
            if side > 0:
                tri.reverse()
            bm.from_mesh(slab_mesh("eye-%d-%d" % (i + 1, side), tri, 0.02, eye))
        mesh = bpy.data.meshes.new("eye-%d" % (i + 1))
        bm.to_mesh(mesh)
        bm.free()
        mesh.materials.append(eye)
        node = mesh_object("eye-%d" % (i + 1), mesh, location=place)
        node["ink"] = 0
        d = delay
        jr_keys(node, "scale", [(0, (0.001, 1, 0.001)), (0.98 + d, (0.001, 1, 0.001)), (1.4 + d, (1, 1, 1)), (1.68 + d, (1, 1, 0.1)),
                                (1.89 + d, (1, 1, 1)), (4.9 + d, (1, 1, 1)), (5.95 + d, (1, 1, 0.35))])

    # The foreground silhouettes (bi-jr-near), unlit, 5 m from the camera, reaching past the frame.
    near = shader_material("near", "#040208", "background")
    for i, points in enumerate(([(0, 680), (0, 520), (60, 560), (90, 610), (130, 590), (170, 640), (210, 680), (210, 760), (-80, 760), (-80, 680)],
                                [(1200, 680), (1280, 680), (1280, 760), (990, 760), (990, 680), (1030, 636), (1070, 584), (1110, 600), (1140, 548), (1200, 500)],
                                [(-80, -80), (260, -80), (260, 0), (190, 40), (140, 30), (80, 70), (0, 60), (-80, 60)],
                                [(1280, -80), (1280, 66), (1200, 66), (1120, 72), (1060, 34), (1000, 46), (930, 0), (930, -80)])):
        mid = svg_point(sum(p[0] for p in points) / len(points), sum(p[1] for p in points) / len(points), 5)
        mesh_object("near-%d" % (i + 1), slab_mesh("near-%d" % (i + 1), svg_outline(points, 5, mid), 0.05, near), location=mid)

    # Where the figures stand (the test figure faces −Y, the camera). The boss rises out of the ground
    # in the rift's light 2 m before it (bi-boss-rise: below the ground to 2.1 s, 4 % of his height low
    # at 2.94 s, in place at 7 s; scale 0.9 → 0.96 → 1.03 of his size, 1.25 times the test figure's).
    king = 1.25
    stand = (foot.x, foot.y - 2.0)
    boss = empty("boss", location=(*stand, -2.4), scale=(king * 0.9,) * 3)
    jr_keys(boss, "location", [(0, (*stand, -2.4)), (2.1, (*stand, -2.4)), (2.94, (*stand, -0.04 * 1.84 * king)), (7.0, (*stand, 0))])
    jr_keys(boss, "scale", [(0, (king * 0.9,) * 3), (2.1, (king * 0.9,) * 3), (2.94, (king * 0.96,) * 3), (7.0, (king * 1.03,) * 3)])
    # The imps (bi-imp-fly, ease-out, delays 0, 0.15, 0.35, 0.5 s): tiny inside the rift to 1.68 s, out
    # to their places (the SVG's, hovering 8.5, 13, 11 and 11 m from the camera: imp 1 before the mid trunk it would hide behind) by 3.36 s,
    # bobbing up 0.1 m, back, up 0.05 m; 0.4 of the test figure; each turned 30° outward (the CSS
    # mirrored imps 1 and 3).
    imp = 0.4
    inside = centre + Vector((0, 0.3, -0.6))
    for i, (x, y, depth, delay, turn) in enumerate(((168, 293, 8.5, 0, -30), (972, 259, 13, 0.15, 30), (324, 449, 11, 0.35, -30), (900, 463, 11, 0.5, 30))):
        place = svg_point(x, y, depth)
        node = empty("imp-%d" % (i + 1), location=inside, scale=(imp * 0.15,) * 3, rotation=(0, 0, turn))
        d = delay
        jr_keys(node, "location", [(0, inside), (1.68 + d, inside), (3.36 + d, place), (4.34 + d, place + Vector((0, 0, 0.1))),
                                   (5.32 + d, place), (7.0 + d, place + Vector((0, 0, 0.05)))])
        jr_keys(node, "scale", [(0, (imp * 0.15,) * 3), (1.68 + d, (imp * 0.15,) * 3), (3.36 + d, (imp,) * 3)])

    # Emitter marks for the sequence's particles: the mist (a low wide spawn, drifting across as
    # bi-mist does) and embers at the rift's foot (not in the old scene: for motes, if wanted).
    mist = empty("mist", location=(-0.5, foot.y - 4.0, 0.3), scale=(7, 3, 0.4))
    jr_keys(mist, "location", [(0, (-0.5, foot.y - 4.0, 0.3)), (7.0, (0.5, foot.y - 4.0, 0.3))])
    empty("embers", location=(foot.x, foot.y - 0.2, 0.1), scale=(0.8, 0.3, 0.2))

    # The camera on its dolly: the push-in (bi-camera, ease-in-out: the art 1.02 → 1.1 about 50 %, 62 %
    # and up 1.5 %) as a move 1.02 m forward (the rift grows 1.1 / 1.02) and 0.13 m down.
    dolly = empty("dolly", location=JR_CAMERA)
    jr_keys(dolly, "location", [(0, JR_CAMERA), (7.0, (JR_CAMERA[0], JR_CAMERA[1] + 1.02, JR_CAMERA[2] - 0.13))])
    lens = bpy.data.cameras.new("Lens")
    lens.sensor_fit, lens.sensor_height, lens.lens = "VERTICAL", JR_SENSOR, JR_LENS
    lens.clip_start, lens.clip_end = 0.1, 100
    camera = bpy.data.objects.new("camera", lens)
    scene.collection.objects.link(camera)
    camera.parent = dolly
    camera.rotation_euler = (math.radians(90 + JR_PITCH), 0, 0)
    scene.camera = camera

    # One sun (Q12): behind the rift, 40° up, shining toward the camera, so what faces the rift is lit
    # (the rift's glow itself is emission, not a light).
    light = bpy.data.lights.new("Riftlight", "SUN")
    light.energy, light.angle = 3.0, math.radians(3)
    mesh_object("sun", light, location=(0, 10, 12), rotation=(-50, 0, 15))

    # The old timeline's moments as the scene's timeline markers, copied into its property "events".
    for name, seconds in (("rift-opens", 0.42), ("eyes-wake", 0.98), ("cracks", 1.26), ("retinue", 1.68), ("king-rises", 2.1),
                          ("flash", 3.85), ("title-card", 4.06), ("reveal", 4.48), ("fade-out", 6.51), ("end", 7.0)):
        scene.timeline_markers.new(name, frame=jr_frame(seconds))
    export_scene_markers(scene)
    scene.frame_set(1)
    export_stage(path)


def export_scene_markers(scene):
    """Copies the scene's timeline markers into its custom property "events" (Docs/Scene.md, step 4,
    "Markers"), which the exporter's scene mode writes as the stage clip's extras: a list of {name,
    time}, the time in seconds from the scene's first frame, in frame order."""
    fps = scene.render.fps / scene.render.fps_base
    markers = sorted(scene.timeline_markers, key=lambda m: (m.frame, m.name))
    scene["events"] = [{"name": m.name, "time": (m.frame - scene.frame_start) / fps} for m in markers]


# Builders by model name (evaluate_animations.py builds the animated ones again).
BUILDERS = {"blocks": blocks, "rigged": rigged, "shapes": shapes, "stage": stage, "fringe": fringe, "glowing": glowing,
            "figure": figure, "reach": reach, "stocky": stocky, "sword": sword, "tabard": tabard, "pennant": pennant,
            "hierarchy": hierarchy, "dolly": dolly, "jiruan": jiruan}


def main():
    output = sys.argv[sys.argv.index("--") + 1]
    os.makedirs(output, exist_ok=True)
    for name, build in BUILDERS.items():
        reset()
        build(os.path.join(output, name + ".glb"))


if __name__ == "__main__":
    main()

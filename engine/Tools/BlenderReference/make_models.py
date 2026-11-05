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
    for mesh in gltf.get("meshes", []):  # a clip library (jiruan-acting) has none
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


# Ji'ruan's intro stage (Docs/Scene.md, step 7, phases 1 and 1b; Q30, Q31 as S7-1 revised it; M10-11):
# every element of the old intro's SVG scene (old-game/scripts/boss-intros.js, "jiruan") remade with real
# detail in the cel, ink and hatch style while still a proof (phase 1's layout kept), its motion keyed from the
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


def jr_lit(shadow_hex):
    """The lit tone whose shadow tone, as the engine makes it (42 % toward the ink #130e0b, in sRGB:
    a stage is instanced without a palette), is the given colour: the old SVG's fills are what the
    camera sees, and with the sun behind the rift what it sees is mostly in shadow."""
    value = int(shadow_hex.lstrip("#"), 16)
    shadow = [(value >> s) & 255 for s in (16, 8, 0)]
    lit = [max(0, min(255, round((c - 0.42 * i) / 0.58))) for c, i in zip(shadow, (0x13, 0x0E, 0x0B))]
    return "#%02x%02x%02x" % tuple(lit)


def jr_smooth(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


JR_FOOT = None  # the rift's foot (Blender's world), set by jiruan before anything stands on the ground


def jr_ground(x, y):
    """The forest floor's height: level (0) in a disc about the clearing before the rift, where the
    cracks, the boss and the rift's foot are; outside it the floor falls away (−0.35 m by 14 m off: the
    ground rises toward the rift), with a mound behind the rift and a gentle unevenness."""
    fx, fy = JR_FOOT.x, JR_FOOT.y
    d = math.hypot(x - fx, (y - (fy - 1.5)) * 0.85)
    level = jr_smooth(4.8, 7.5, d)
    fall = -0.35 * jr_smooth(6.0, 16.0, d)
    mound = 0.5 * math.exp(-(((x - fx) / 3.2) ** 2 + ((y - fy - 4.0) / 2.2) ** 2))
    bumps = 0.08 * math.sin(1.31 * x + 0.4) * math.sin(1.07 * y + 1.1) + 0.05 * math.sin(2.7 * x + 1.9 * y) + 0.03 * math.sin(4.3 * x - 3.1 * y + 0.7)
    return level * (fall + bumps) + mound * jr_smooth(4.0, 6.0, d)


class JrMesh:
    """Geometry gathered for one object, in its own space: vertices, faces and each face's slot."""

    def __init__(self):
        self.verts, self.faces, self.slots = [], [], []

    def add(self, verts, faces, slot):
        base = len(self.verts)
        self.verts += [tuple(v) for v in verts]
        self.faces += [tuple(base + i for i in f) for f in faces]
        self.slots += [slot] * len(faces)

    def tube(self, points, radii, segments, slot, rng=None, jitter=0.0, rim=None):
        """A tube along the points (radius per point; a last radius of 0 closes it at a tip), faces
        wound outward. Bark planes: few flat sides, each ring jittered and turned a little. rim =
        (direction, slot, cosine): faces whose outward side points along the direction take that slot."""
        points = [Vector(p) for p in points]
        n = len(points)
        tangents = [((points[min(i + 1, n - 1)] - points[max(i - 1, 0)])).normalized() for i in range(n)]
        normal = tangents[0].orthogonal().normalized()
        rings, base = [], len(self.verts)
        tip = radii[-1] <= 1e-6
        for i in range(n - 1 if tip else n):
            if i > 0:
                normal = (tangents[i - 1].rotation_difference(tangents[i]) @ normal).normalized()
            binormal = tangents[i].cross(normal)
            turn = rng.uniform(-0.25, 0.25) if rng else 0.0
            ring = []
            for k in range(segments):
                a = 2 * math.pi * k / segments + turn
                r = radii[i] * (1 + (rng.uniform(-jitter, jitter) if rng else 0.0))
                ring.append(len(self.verts))
                self.verts.append(tuple(points[i] + r * (math.cos(a) * normal + math.sin(a) * binormal)))
            rings.append(ring)
        for i in range(len(rings) - 1):
            for k in range(segments):
                q = (rings[i][k], rings[i][(k + 1) % segments], rings[i + 1][(k + 1) % segments], rings[i + 1][k])
                self.faces.append(q)
                self.slots.append(self._rim(q, slot, rim))
        if tip:
            end = len(self.verts)
            self.verts.append(tuple(points[-1]))
            for k in range(segments):
                f = (rings[-1][k], rings[-1][(k + 1) % segments], end)
                self.faces.append(f)
                self.slots.append(self._rim(f, slot, rim))

    def _rim(self, face, slot, rim):
        if rim is None:
            return slot
        direction, rim_slot, cosine = rim
        corners = [Vector(self.verts[i]) for i in face]
        normal = (corners[1] - corners[0]).cross(corners[2] - corners[0])
        flat = Vector((normal.x, normal.y, 0))
        if flat.length < 1e-9:
            return slot
        return rim_slot if flat.normalized().dot(direction) > cosine else slot

    def blob(self, centre, size, slot, rng, subdivisions=1, jitter=0.18):
        """A faceted lump (an icosphere jittered and scaled): foliage masses, rocks."""
        bm = bmesh.new()
        bmesh.ops.create_icosphere(bm, subdivisions=subdivisions, radius=1.0)
        verts = [Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2])) * (1 + rng.uniform(-jitter, jitter)) + Vector(centre) for v in bm.verts]
        faces = [[v.index for v in f.verts] for f in bm.faces]
        bm.free()
        self.add(verts, faces, slot)

    def leaf(self, base, direction, side, length, width, slot):
        """A leaf card: a diamond from its base along the direction, folded along its middle."""
        d, s = Vector(direction).normalized() * length, Vector(side).normalized() * width / 2
        up = d.cross(s).normalized() * width * 0.12
        b = Vector(base)
        self.add([b, b + d * 0.45 + s, b + d, b + d * 0.45 - s, b + d * 0.5 + up], [(0, 1, 4), (1, 2, 4), (2, 3, 4), (3, 0, 4)], slot)

    def blade(self, base, direction, length, width, droop, slot, steps=6):
        """A fern blade or grass leaf: a strip arching out from its base and drooping, narrowing to a tip."""
        d = Vector((direction[0], direction[1], 0)).normalized()
        side = Vector((-d.y, d.x, 0))
        verts, faces = [], []
        for i in range(steps + 1):
            s = i / steps
            p = Vector(base) + d * length * s + Vector((0, 0, length * (0.9 * s - droop * s * s)))
            w = width * math.sin(math.pi * min(0.999, 0.15 + 0.85 * s)) * (1 - s * 0.6)
            verts += [p + side * w / 2, p - side * w / 2]
            if i:
                faces.append((2 * i - 2, 2 * i - 1, 2 * i + 1, 2 * i))
        self.add(verts, faces, slot)

    def object(self, name, materials, origin=(0, 0, 0), smooth=False, ink=True, closed=False):
        """The object, its origin at the given point; closed meshes get their normals made outward."""
        o = Vector(origin)
        mesh = bpy.data.meshes.new(name)
        mesh.from_pydata([tuple(Vector(v) - o) for v in self.verts], [], self.faces)
        for m in materials:
            mesh.materials.append(m)
        for polygon, slot in zip(mesh.polygons, self.slots):
            polygon.material_index = slot
        mesh.update()
        if closed:
            bm = bmesh.new()
            bm.from_mesh(mesh)
            bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
            bm.to_mesh(mesh)
            bm.free()
        if smooth:
            mesh.shade_smooth()
        else:
            mesh.shade_flat()
        obj = mesh_object(name, mesh, location=tuple(o))
        if not ink:
            obj["ink"] = 0
        return obj


def jr_tree(mesh, rng, base, height, radius, lean, bend, slot, foliage_slot=None, rim=None, roots=5, branches=3, crown=None,
            dead=0, segments=8, rim_slot=None, branch_range=(0.45, 0.8)):
    """A tree into the mesh: a tapered, bent trunk of bark planes from below the ground, root flares
    spreading into the floor, a few branches, and foliage masses (crown: the height they start at; none
    for a bare tree) or dead, crooked branches."""
    base = Vector(base)
    lean_v = Vector((lean[0], lean[1], 0))
    bend_v = Vector((bend[0], bend[1], 0))

    def centre(t):
        return base + Vector((0, 0, -0.4 + (height + 0.4) * t)) + lean_v * t ** 1.4 + bend_v * math.sin(math.pi * t)

    rings = 16
    points = [centre(i / rings) for i in range(rings + 1)]
    radii = [radius * (1.0 - 0.6 * (i / rings)) * (1 + 0.35 * max(0, 0.12 - i / rings) / 0.12) for i in range(rings + 1)]
    rim_spec = (rim, rim_slot, 0.95) if rim is not None else None
    mesh.tube(points, radii, segments, slot, rng, jitter=0.07, rim=rim_spec)
    # Root flares: tapering into the floor, spread about the trunk.
    for r in range(roots):
        a = 2 * math.pi * (r + rng.uniform(-0.3, 0.3)) / roots
        out = Vector((math.cos(a), math.sin(a), 0))
        length = radius * rng.uniform(3.0, 4.6)
        start = base + Vector((0, 0, radius * rng.uniform(0.6, 1.1)))
        path = []
        for i in range(6):
            s = i / 5
            p = start + out * (radius * 0.6 + length * s)
            ground = jr_ground(p.x, p.y)
            p.z = ground + (start.z - base.z) * (1 - s) ** 1.6 - 0.12 * s
            path.append(p)
        mesh.tube(path, [radius * 0.42 * (1 - 0.85 * i / 5) + 0.01 for i in range(6)], 6, slot, rng, jitter=0.1)
    # Branches from the upper trunk, upward and outward; dead ones crooked and bare.
    for b in range(branches + dead):
        is_dead = b >= branches
        t = rng.uniform(*branch_range) if not is_dead else rng.uniform(0.18, 0.4)
        a = rng.uniform(0, 2 * math.pi) if not is_dead or rim is None else math.atan2(rim.y, rim.x) + rng.uniform(-0.6, 0.6)
        out = Vector((math.cos(a), math.sin(a), 0))
        start = centre(t)
        length = height * rng.uniform(0.18, 0.3) if not is_dead else rng.uniform(1.2, 2.2)
        rise = rng.uniform(0.35, 0.8) if not is_dead else rng.uniform(-0.1, 0.35)
        path, p, step = [start], start.copy(), length / 5
        direction = (out + Vector((0, 0, rise))).normalized()
        for i in range(5):
            if is_dead:
                direction = (direction + Vector((rng.uniform(-0.5, 0.5), rng.uniform(-0.5, 0.5), rng.uniform(-0.4, 0.3)))).normalized()
            else:
                direction = (direction + Vector((0, 0, 0.12))).normalized()
            p = p + direction * step
            path.append(p.copy())
        r0 = radius * (1.0 - 0.6 * t) * (0.45 if not is_dead else 0.3)
        mesh.tube(path, [r0 * (1 - i / 5) for i in range(6)], 5, slot, rng, jitter=0.12)
        if foliage_slot is not None and not is_dead:
            for k in range(rng.randint(2, 3)):
                c = path[-1] + Vector((rng.uniform(-0.6, 0.6), rng.uniform(-0.6, 0.6), rng.uniform(-0.2, 0.5)))
                s = rng.uniform(0.9, 1.6) * height / 14
                mesh.blob(c, (s * 1.4, s * 1.4, s * 0.8), foliage_slot, rng)
    if crown is not None and foliage_slot is not None:
        for k in range(6):
            t = crown + (1 - crown) * k / 6
            c = centre(t) + Vector((rng.uniform(-1.6, 1.6), rng.uniform(-1.6, 1.6), 0)) * height / 14
            s = rng.uniform(1.2, 2.2) * height / 14
            mesh.blob(c, (s * 1.5, s * 1.5, s * 0.9), foliage_slot, rng)


def jr_gradient(stops, t):
    """An SVG gradient's colour (sRGB 0–255) and opacity at t, from (offset, hex, opacity) stops."""
    for (t0, c0, a0), (t1, c1, a1) in zip(stops, stops[1:]):
        if t <= t1:
            u = 0 if t1 == t0 else (t - t0) / (t1 - t0)
            v0, v1 = int(c0.lstrip("#"), 16), int(c1.lstrip("#"), 16)
            return [((v0 >> s) & 255) * (1 - u) + ((v1 >> s) & 255) * u for s in (16, 8, 0)], a0 + (a1 - a0) * u
    v = int(stops[-1][1].lstrip("#"), 16)
    return [(v >> s) & 255 for s in (16, 8, 0)], stops[-1][2]


def jr_hex(rgb):
    return "#%02x%02x%02x" % tuple(max(0, min(255, round(c))) for c in rgb)


def jiruan(path):
    """Ji'ruan's intro stage (Docs/Scene.md, step 7: phase 1's layout, phase 1b's detail, S7-1). What it
    holds (each top-level object one model for the ink, Q2):
    - the night sky: "sky" (the old gradient in bands, unlit), "stars" (unlit), the halo "halo" (the
      old radial glow as stepped discs, unlit, behind the far trees: it grows and fades by scale), and
      three layered tree lines "treeline-1".."3" (unlit silhouettes);
    - the forest: "ground" (the floor's relief: level about the clearing, rising toward the rift, a
      mound behind it), trees "tree-far-1".."6" at the old far trunks' places, "tree-back-1".."6"
      between them, "tree-mid-1".."4" at the old mid trunks' places (the four the rift lights with an
      unlit violet rim on their rift side, as the SVG's stroke), "tree-ring-1".."10" closing the ring
      out of view, a dead "snag" by the rift; "rocks", "roots", "undergrowth-1".."3" (ferns and grass),
      "litter" (fallen leaves and twigs, no ink);
    - the rift: "rift" (an empty at its middle, opening by scale) and, keyed alike about that middle,
      under the still empty "rift-glow" (one glowing model) the shells "rift-bloom", "rift-outer",
      "rift-inner", "rift-core" (Emission: they glow), and
      "shard-1".."12" breaking off its edges and drifting out;
    - the cracks: "crack-1".."9" (glowing fissures drawn out by scale) and "plate-1".."N" (broken
      ground plates lifting along them as the cracks reach them);
    - the eyes "eye-1".."8", the foreground "near-1".."4" (unlit silhouettes: a root arch, a bush,
      overhanging branches with leaves), the marks "boss", "imp-1".."imp-4", the emitter marks "mist"
      and "embers", the camera "camera" on the empty "dolly", and the sun "sun".
    Phase 1's two rules hold: no key past 7 s, and nothing hangs under a node scaled to nearly nothing.
    The old CSS's moments are the scene's timeline markers. Exported as a stage (export_stage) at the
    render size 1200 × 680, the scene's custom property "events" holding the markers."""
    global JR_FOOT
    scene = bpy.context.scene
    scene.render.fps, scene.render.fps_base = JR_FPS, 1
    scene.frame_start, scene.frame_end = 1, jr_frame(JR_SECONDS)
    scene.render.resolution_x, scene.render.resolution_y = JR_WIDTH, JR_HEIGHT
    scene.render.pixel_aspect_x = scene.render.pixel_aspect_y = 1
    rng = __import__("random").Random(20261105)

    centre = svg_point(600, 366, 14)
    JR_FOOT = foot = Vector((centre.x, centre.y, 0))
    toward_rift = lambda p: Vector((foot.x - p.x, foot.y - p.y, 0)).normalized()  # noqa: E731

    # ---- The night sky (unlit, no ink): the old gradient (#06030b, #140a20 at 55 %, #22142f) in bands
    # by the first frame's screen height on a plane 64 m off; the view's background is its top.
    sky_stops = [(0.0, "#06030b", 1), (0.55, "#140a20", 1), (1.0, "#22142f", 1)]
    sky = JrMesh()
    sky_materials, depth = [], 64.0
    edges = [-14.0] + [z / 10 for z in range(-10, 151, 12)] + [40.0]
    for i, (z0, z1) in enumerate(zip(edges, edges[1:])):
        zm = max(-1.0, min(14.0, (z0 + z1) / 2))
        screen = 340 - (zm - 1.6) / (depth * JR_SENSOR / 2 / JR_LENS) * 340
        colour, _ = jr_gradient(sky_stops, max(0.0, min(1.0, screen / 680)))
        sky_materials.append(shader_material("sky.%d" % (i + 1), jr_hex(colour), "background"))
        sky.add([(-90, 0, z0), (90, 0, z0), (90, 0, z1), (-90, 0, z1)], [(0, 1, 2, 3)], i)
    sky.object("sky", sky_materials, origin=(0, 0, 0), ink=False).location = (0, depth - 14, 0)
    stars = JrMesh()
    star_dim, star_bright = shader_material("star.dim", "#6a44a0", "background"), shader_material("star.bright", "#d6b4ff", "background")
    for k in range(48):
        x, z = rng.uniform(-24, 24), rng.uniform(6.5, 15.5)
        s = rng.uniform(0.05, 0.12) if k % 6 else rng.uniform(0.12, 0.18)
        stars.add([(x - s, 0, z), (x, 0, z - s), (x + s, 0, z), (x, 0, z + s)], [(0, 1, 2, 3)], 1 if k % 6 == 0 else 0)
    stars.object("stars", [star_dim, star_bright], ink=False).location = (0, 49, 0)

    # The halo (bi-jr-halo: the old radial glow behind the rift, rx 460, ry 340 about (600, 390)):
    # stepped discs in the gradient's colours over the sky, 35 m off: behind the far trees (which stand
    # against it, as the SVG's far trunks did) and the trees between them (33 m), before the tree lines.
    # It grows from nothing (0.56 s) to full (4.06 s) and eases back (0.6 opacity by 7 s: here 0.88 of
    # its size).
    halo_stops = [(0.0, "#f1e6ff", 0.9), (0.22, "#b48cff", 0.62), (0.6, "#5a2a9c", 0.22), (1.0, "#1a0d2b", 0.0)]
    halo_depth = 35.0
    hc = svg_point(600, 390, halo_depth)
    rx = (svg_point(1060, 390, halo_depth) - hc).length
    ry = (svg_point(600, 50, halo_depth) - hc).length
    halo, halo_materials = JrMesh(), []
    rings = [1.0, 0.82, 0.66, 0.52, 0.4, 0.29, 0.2, 0.12]
    for i, r in enumerate(rings):
        colour, alpha = jr_gradient(halo_stops, r - 0.07)
        under, _ = jr_gradient(sky_stops, 0.62)
        mixed = [c * alpha + u * (1 - alpha) for c, u in zip(colour, under)]
        halo_materials.append(shader_material("halo.%d" % (i + 1), jr_hex(mixed), "background"))
        verts = [(0, -0.05 * i, 0)] + [(rx * r * math.cos(2 * math.pi * k / 48), -0.05 * i, ry * r * math.sin(2 * math.pi * k / 48)) for k in range(48)]
        halo.add(verts, [(0, k + 1, (k + 1) % 48 + 1) for k in range(48)], i)
    halo_obj = halo.object("halo", halo_materials, origin=(0, 0, 0), ink=False)
    halo_obj.location = hc
    jr_keys(halo_obj, "scale", [(0, (0.001, 1, 0.001)), (0.56, (0.001, 1, 0.001)), (2.24, (0.92, 1, 0.92)), (4.06, (1, 1, 1)), (7.0, (0.88, 1, 0.88))])

    # Three tree lines between the halo and the far trees: unlit silhouettes, the farthest palest.
    for i, (dist, colour, top, rough) in enumerate(((54.0, "#1b0f28", 6.5, 2.2), (47.0, "#140b1f", 5.0, 2.6), (41.0, "#0e0717", 3.6, 2.8))):
        line = JrMesh()
        xs = [-80 + 0.9 * k for k in range(179)]
        heights = []
        for x in xs:
            h = top + rough * (0.45 * math.sin(0.21 * x + i) + 0.3 * math.sin(0.57 * x + 2 * i) + 0.25 * math.sin(1.31 * x + 0.5 * i))
            if rng.random() < 0.18:
                h += rng.uniform(0.8, 2.2)  # a crown standing above the line
            heights.append(h)
        for k in range(len(xs) - 1):
            line.add([(xs[k], 0, -3), (xs[k + 1], 0, -3), (xs[k + 1], 0, heights[k + 1]), (xs[k], 0, heights[k])], [(0, 1, 2, 3)], 0)
        line.object("treeline-%d" % (i + 1), [shader_material("treeline.%d" % (i + 1), colour, "background")], ink=False).location = (0, dist - 14, 0)

    # ---- The ground: the floor's relief, smooth.
    ground_m = material("ground", jr_lit("#140c1e"))
    floor = JrMesh()
    step, x0, x1, y0, y1 = 0.6, -33.0, 33.0, -21.0, 45.0
    nx, ny = int(round((x1 - x0) / step)), int(round((y1 - y0) / step))
    verts = [(x0 + i * step, y0 + j * step, jr_ground(x0 + i * step, y0 + j * step)) for j in range(ny + 1) for i in range(nx + 1)]
    faces = [(j * (nx + 1) + i, j * (nx + 1) + i + 1, (j + 1) * (nx + 1) + i + 1, (j + 1) * (nx + 1) + i) for j in range(ny) for i in range(nx)]
    floor.add(verts, faces, 0)
    floor.object("ground", [ground_m], origin=(0, 0, 0), smooth=True)

    # ---- Trees. Materials: the old fills as the shadow side the camera sees (jr_lit), foliage darker.
    bark_fills = ("#170e22", "#1a1026", "#1d122b", "#0c0713", "#0d0815")
    bark = {c: material("bark." + str(i + 1), jr_lit(c)) for i, c in enumerate(bark_fills)}
    foliage_far = material("foliage.far", jr_lit("#120a1c"))
    foliage_near = material("foliage.near", jr_lit("#0a0612"))
    rim = shader_material("rim", "#6a44a0", "background")

    def tree(name, x, w, lean_px, colour, rimmed, depth, height, crown, branches=3, dead=0, roots=5, segments=8, foliage=None, branch_range=(0.45, 0.8)):
        at = svg_point(x, 680, depth)
        px = (svg_point(x + 1, 680, depth) - at).length
        base = Vector((at.x, at.y, jr_ground(at.x, at.y)))
        mesh = JrMesh()
        side = Vector((1 if x < 600 else -1, 0.12, 0)).normalized() if rimmed else None
        # The SVG's lean (its top's shift over the frame's 700 px), carried to the tree's height.
        lean = (1.5 * lean_px * height / 700, rng.uniform(-0.3, 0.3))
        bend = (rng.uniform(-0.25, 0.25) * w * px, rng.uniform(-0.2, 0.2))
        jr_tree(mesh, rng, base, height, w / 2 * px, lean, bend, 0, 1 if foliage is not None else None, rim=side, roots=roots,
                branches=branches, crown=crown, dead=dead, segments=segments, rim_slot=2 if rimmed else None, branch_range=branch_range)
        materials = [bark[colour], foliage if foliage is not None else bark[colour]] + ([rim] if rimmed else [])
        return mesh.object(name, materials, origin=base)

    for i, (x, w, lean, colour, rimmed) in enumerate(((70, 60, -8, "#170e22", False), (250, 46, 10, "#1a1026", False), (420, 38, -6, "#1d122b", True),
                                                      (790, 40, 8, "#1d122b", True), (960, 52, -10, "#1a1026", False), (1130, 64, 6, "#170e22", False))):
        tree("tree-far-%d" % (i + 1), x, w, lean, colour, rimmed, 30, 17.0, 0.42, branches=3, roots=3, segments=18 if rimmed else 8, foliage=foliage_far)
    for i, x in enumerate((160, 335, 505, 700, 875, 1045)):
        tree("tree-back-%d" % (i + 1), x, rng.uniform(26, 36), rng.uniform(-8, 8), "#170e22", False, 33, 15.0, 0.4, branches=2, roots=2, segments=7,
             foliage=foliage_far)
    for i, (x, w, lean, colour, rimmed) in enumerate(((-10, 110, 14, "#0c0713", False), (160, 74, -12, "#0d0815", True),
                                                      (1040, 80, 12, "#0d0815", True), (1215, 120, -14, "#0c0713", False))):
        tree("tree-mid-%d" % (i + 1), x, w, lean, colour, rimmed, 9, 13.0, 0.55, branches=3, dead=3 if rimmed else 1, roots=6, segments=20 if rimmed else 9,
             foliage=foliage_near, branch_range=(0.2, 0.42))
    for i in range(10):
        a = math.radians(200 + i * 14)
        p = Vector((17 * math.cos(a), -6 + 17 * math.sin(a), 0))
        p.z = jr_ground(p.x, p.y)
        mesh = JrMesh()
        jr_tree(mesh, rng, p, 16.0, 0.45, (0, 0), (0, 0), 0, None, roots=4, branches=0)
        mesh.object("tree-ring-%d" % (i + 1), [bark["#0c0713"]], origin=p)
    # A dead snag by the rift: a broken trunk, crooked bare branches toward the rift, its rim lit.
    snag_at = Vector((foot.x - 3.4, foot.y - 1.2, 0))
    snag_at.z = jr_ground(snag_at.x, snag_at.y)
    mesh = JrMesh()
    jr_tree(mesh, rng, snag_at, 3.2, 0.24, (0.25, 0.1), (0.1, 0), 0, None, rim=toward_rift(snag_at), roots=4, branches=0, dead=4, segments=10, rim_slot=1)
    mesh.object("snag", [bark["#1d122b"], rim], origin=snag_at)

    # ---- The floor's clutter.
    def clear(x, y):
        """Away from the clearing's middle, where the cracks run and the figures stand."""
        return math.hypot(x - foot.x, (y - foot.y + 1.6) * 0.9) > 4.0

    stone = material("stone", jr_lit("#1a1226"))
    rocks = JrMesh()
    placed = 0
    while placed < 18:
        a, r = rng.uniform(0, 2 * math.pi), rng.uniform(4.2, 11.0)
        x, y = foot.x + r * math.cos(a), foot.y - 1.5 + r * math.sin(a) * 0.9
        if not clear(x, y) or y < -11.5:
            continue
        s = rng.uniform(0.18, 0.6)
        rocks.blob((x, y, jr_ground(x, y) + s * 0.25), (s * rng.uniform(1.0, 1.6), s * rng.uniform(0.8, 1.3), s * rng.uniform(0.55, 0.85)), 0, rng, subdivisions=2, jitter=0.14)
        placed += 1
    rocks.object("rocks", [stone], origin=(foot.x, foot.y, 0))

    # Surface roots snaking over the floor from the trees' side of the clearing.
    roots = JrMesh()
    for k in range(5):
        a = rng.uniform(0, 2 * math.pi)
        start = Vector((foot.x + 9 * math.cos(a), foot.y - 1.5 + 8 * math.sin(a), 0))
        if start.y < -12:
            continue
        heading = (Vector((foot.x, foot.y, 0)) - start).normalized()
        trail, p = [], start.copy()
        for i in range(8):
            heading = (heading + Vector((rng.uniform(-0.35, 0.35), rng.uniform(-0.35, 0.35), 0))).normalized()
            p = p + heading * 0.45
            if not clear(p.x, p.y):
                break
            trail.append(Vector((p.x, p.y, jr_ground(p.x, p.y) + 0.02)))
        if len(trail) > 2:
            roots.tube(trail, [0.07 * (1 - i / len(trail)) + 0.012 for i in range(len(trail))], 5, 0, rng, jitter=0.15)
    roots.object("roots", [bark["#1a1026"]], origin=(foot.x, foot.y, 0))

    # Undergrowth: fern clumps and grass tufts about the clearing's edge and at the trees' feet.
    fern = material("fern", jr_lit("#120c1c"), culled=False)
    grass = material("grass", jr_lit("#160f22"), culled=False)
    regions = (("undergrowth-1", lambda x: x < foot.x - 1.5), ("undergrowth-2", lambda x: x > foot.x + 1.5), ("undergrowth-3", lambda x: True))
    clumps = {name: JrMesh() for name, _ in regions}
    count = 0
    while count < 46:
        a, r = rng.uniform(0, 2 * math.pi), rng.uniform(4.5, 13.0)
        x, y = foot.x + r * math.cos(a), foot.y - 1.5 + r * math.sin(a)
        if not clear(x, y) or y < -11.0:
            continue
        name = next(n for n, test in regions if test(x)) if y < foot.y + 1 else "undergrowth-3"
        base = Vector((x, y, jr_ground(x, y) - 0.02))
        if count % 3:
            for b in range(rng.randint(6, 9)):
                h = rng.uniform(0, 2 * math.pi)
                clumps[name].blade(base, (math.cos(h), math.sin(h)), rng.uniform(0.45, 0.85), rng.uniform(0.09, 0.15), rng.uniform(0.7, 1.1), 0)
        else:
            for b in range(rng.randint(8, 12)):
                h = rng.uniform(0, 2 * math.pi)
                clumps[name].blade(base + Vector((rng.uniform(-0.1, 0.1), rng.uniform(-0.1, 0.1), 0)), (math.cos(h), math.sin(h)),
                                   rng.uniform(0.25, 0.45), 0.035, rng.uniform(0.2, 0.5), 1, steps=3)
        count += 1
    for name, _ in regions:
        clumps[name].object(name, [fern, grass], origin=(foot.x, foot.y, 0))

    # Fallen leaves and twigs on the floor (no ink: too small to outline).
    leaf_dark, leaf_light = material("leaf.dark", jr_lit("#150d20"), culled=False), material("leaf.light", jr_lit("#22163a"), culled=False)
    litter = JrMesh()
    for k in range(260):
        x, y = foot.x + rng.uniform(-9, 9), foot.y + rng.uniform(-10.5, 3)
        if math.hypot(x - foot.x, y - foot.y + 2.0) < 0.9:
            continue
        h = rng.uniform(0, 2 * math.pi)
        d, s = Vector((math.cos(h), math.sin(h), 0.04)), Vector((-math.sin(h), math.cos(h), 0))
        litter.leaf((x, y, jr_ground(x, y) + 0.012), d, s, rng.uniform(0.08, 0.16), rng.uniform(0.04, 0.08), k % 3 == 0)
    twig_mesh = JrMesh()
    for k in range(40):
        x, y = foot.x + rng.uniform(-8, 8), foot.y + rng.uniform(-9.5, 2.5)
        if math.hypot(x - foot.x, y - foot.y + 2.0) < 1.0:
            continue
        h = rng.uniform(0, 2 * math.pi)
        a = Vector((x, y, jr_ground(x, y) + 0.015))
        b = a + Vector((math.cos(h), math.sin(h), 0)) * rng.uniform(0.25, 0.6)
        b.z = jr_ground(b.x, b.y) + 0.015
        litter.tube([a, (a + b) / 2 + Vector((0, 0, 0.02)), b], [0.012, 0.01, 0.006], 4, 2)
    litter.object("litter", [leaf_dark, leaf_light, bark["#1a1026"]], origin=(foot.x, foot.y, 0), ink=False)

    # ---- The rift: an empty at its middle (the CSS scales it about its box's centre, bi-rift-open) and
    # four layered shells, each a jagged lens bulging toward the camera, keyed alike about that middle:
    # the bloom (#8e5ad6, the old blurred bloom, pulsing), the outer shell (#7a4cc8, the tear's violet
    # edge), the inner shell (#d9c2ff) and the white-hot core (#fbf6ff), each nearer than the last.
    rift = empty("rift", location=centre)
    # The shells hang under one still parent: the rift is one glowing model (one halo behind all of it).
    glow = empty("rift-glow", location=centre)
    bloom_points = [(600, 112), (646, 218), (622, 300), (668, 392), (630, 470), (658, 562), (600, 618), (542, 562), (570, 470), (532, 392), (578, 300), (554, 218)]
    outer_points = [(600, 128), (628, 212), (612, 268), (644, 340), (618, 404), (648, 476), (622, 544), (632, 602), (600, 596), (572, 544), (590, 476), (554, 404),
                    (584, 340), (560, 268), (586, 212)]
    inner_points = [(600, 140), (618, 214), (607, 268), (630, 338), (610, 404), (632, 474), (612, 542), (614, 590), (586, 590), (588, 542), (580, 474), (566, 404),
                    (590, 338), (578, 268), (591, 214)]
    core_points = [(600, 152), (606, 214), (603, 266), (612, 338), (604, 404), (612, 474), (605, 542), (602, 584), (598, 584), (595, 542), (588, 474), (596, 404),
                   (588, 338), (597, 266), (594, 214)]

    def lens(points, bulge, ahead, scale_x=1.0):
        outline = [(x * scale_x, z) for x, z in svg_outline([(600 + (x - 600), y) for x, y in points], 14, centre)]
        count = len(outline)
        cx = sum(p[0] for p in outline) / count
        cz = sum(p[1] for p in outline) / count
        verts = [(x, ahead, z) for x, z in outline] + [(cx, ahead - bulge, cz), (cx, ahead + 0.02, cz)]
        front, back = count, count + 1
        faces = [(k, (k + 1) % count, front) for k in range(count)] + [((k + 1) % count, k, back) for k in range(count)]
        return verts, faces

    opening = [(0, (0.001, 1, 0.001)), (0.42, (0.001, 1, 0.001)), (1.96, (1, 1, 1)), (3.92, (1.5, 1, 1.04)), (7.0, (1.25, 1, 1))]
    jr_keys(rift, "scale", opening)
    pulse = [(k * 1.4, (1.0, 1, 1.0) if k % 2 == 0 else (1.12, 1, 1.04)) for k in range(6)]

    def between(keys, t):
        (t0, v0), (t1, v1) = max((k for k in keys if k[0] <= t), key=lambda k: k[0]), min((k for k in keys if k[0] >= t), key=lambda k: k[0])
        u = 0 if t1 == t0 else (t - t0) / (t1 - t0)
        return [a + (b - a) * u for a, b in zip(v0, v1)]
    bloom_keys = [(t, tuple(a * b for a, b in zip(between(opening, t), between(pulse, t)))) for t in sorted({k[0] for k in opening + pulse})]
    for name, points, colour, bulge, ahead, keys, scale_x in (("rift-bloom", bloom_points, "#8e5ad6", 0.12, 0.12, bloom_keys, 0.85),
                                                             ("rift-outer", outer_points, "#7a4cc8", 0.18, 0.0, opening, 1.0),
                                                             ("rift-inner", inner_points, "#d9c2ff", 0.14, -0.08, opening, 1.0),
                                                             ("rift-core", core_points, "#fbf6ff", 0.1, -0.16, opening, 1.0)):
        shell = JrMesh()
        verts, faces = lens(points, bulge, ahead, scale_x)
        shell.add(verts, faces, 0)
        obj = shell.object(name, [shader_material(name.replace("-", "."), colour, "emission")], origin=(0, 0, 0), ink=False, closed=True)
        obj.parent, obj.location = glow, (0, 0, 0)
        jr_keys(obj, "scale", keys)
    # Shards breaking off the rift's edges once it is open, drifting out and turning.
    shard_a, shard_b = shader_material("shard", "#d9c2ff", "emission"), shader_material("shard.dark", "#b48cff", "emission")
    for k in range(12):
        side = -1 if k % 2 else 1
        y_svg = 170 + k * 36
        start = svg_point(600 + side * rng.uniform(38, 58), y_svg, 14)
        drift = Vector((side * rng.uniform(0.6, 1.4), rng.uniform(-0.6, -0.1), rng.uniform(0.1, 0.7)))
        s = rng.uniform(0.07, 0.16)
        shard = JrMesh()
        shard.add([(0, 0, s), (s * 0.7, 0.02, -s * 0.4), (-s * 0.5, -0.02, -s * 0.6), (0, -s * 0.3, 0)], [(0, 1, 3), (1, 2, 3), (2, 0, 3), (0, 2, 1)], 0)
        obj = shard.object("shard-%d" % (k + 1), [shard_a if k % 3 else shard_b], origin=(0, 0, 0), ink=False, closed=True)
        obj.location = start
        t0 = 1.96 + 0.17 * k
        jr_keys(obj, "scale", [(0, (0.001,) * 3), (t0, (0.001,) * 3), (t0 + 0.25, (1, 1, 1))])
        jr_keys(obj, "location", [(0, start), (t0, start), (7.0, start + drift)])
        jr_keys(obj, "rotation_euler", [(0, (0, 0, 0)), (t0, (0, 0, 0)), (7.0, (rng.uniform(-3, 3), rng.uniform(-3, 3), rng.uniform(-3, 3)))])

    # ---- The cracks: each segment of the SVG's crack path a glowing fissure on the floor from the
    # rift's foot (its origin at its start, drawn out by its length's scale as the dash offset, 260 → 0
    # over 1.26–2.94 s, reaches it), with broken ground plates beside it lifting as it passes them.
    crack_m = shader_material("crack", "#c58cff", "emission")
    plate_m = material("plate", jr_lit("#160e20"))
    paths = [[(600, 600), (548, 632), (492, 640)], [(600, 600), (656, 634), (722, 646)], [(600, 600), (582, 652), (590, 690)],
             [(600, 600), (640, 668)], [(548, 632), (520, 676)], [(656, 634), (690, 686)]]
    starts = {(548, 632): math.hypot(52, 32), (656, 634): math.hypot(56, 34)}
    boss_spot = Vector((foot.x, foot.y - 2.0, 0))
    number = plates = 0
    for line in paths:
        along = starts.get(line[0], 0.0)
        for a, b in zip(line, line[1:]):
            pa = foot + Vector(((a[0] - 600) * 0.012, -(a[1] - 600) * 0.05, 0.012))
            pb = foot + Vector(((b[0] - 600) * 0.012, -(b[1] - 600) * 0.05, 0.012))
            length = (pb - pa).length
            heading = math.atan2((pb - pa).y, (pb - pa).x)
            number += 1
            fissure = JrMesh()
            # A glowing seam with ragged edges along local X, from its start.
            pts = 7
            top, bottom = [], []
            for i in range(pts + 1):
                s = i / pts
                w = 0.05 * (1 - 0.6 * s) + rng.uniform(-0.012, 0.012)
                top.append((length * s, w, 0))
                bottom.append((length * s, -w, 0))
            verts = top + bottom
            faces = [(i, i + 1, pts + 2 + i, pts + 1 + i) for i in range(pts)]
            fissure.add(verts, [tuple(reversed(f)) for f in faces], 0)
            obj = fissure.object("crack-%d" % number, [crack_m], origin=(0, 0, 0), ink=False)
            obj.location, obj.rotation_euler = pa, (0, 0, heading)
            seg = math.hypot(b[0] - a[0], b[1] - a[1])
            t0, t1 = 1.26 + 1.68 * min(along, 260) / 260, 1.26 + 1.68 * min(along + seg, 260) / 260
            jr_keys(obj, "scale", [(0, (0.001, 0.001, 1)), (t0, (0.001, 0.001, 1)), (max(t1, t0 + 1 / 60), (1, 1, 1))], interpolation="LINEAR")
            # A plate either side of the segment's outer part, hinged at its outer edge, lifting 12–30°
            # as the crack reaches it (none by the boss's feet).
            for side in (-1, 1):
                for s in (rng.uniform(0.45, 0.75),):
                    hinge = pa + (pb - pa) * s + Vector((-math.sin(heading), math.cos(heading), 0)) * side * rng.uniform(0.36, 0.48)
                    if (hinge - boss_spot).length < 1.1:
                        continue
                    w, d = rng.uniform(0.34, 0.5), rng.uniform(0.28, 0.4)
                    outline = [(-w / 2, 0), (w / 2, 0), (w / 2 + rng.uniform(-0.06, 0.06), d * rng.uniform(0.7, 1.0)), (rng.uniform(-0.06, 0.06), d),
                               (-w / 2 + rng.uniform(-0.06, 0.06), d * rng.uniform(0.6, 0.9))]
                    plate = JrMesh()
                    top_v = [(x, y, 0.012) for x, y in outline]
                    low_v = [(x, y, -0.012) for x, y in outline]
                    n5 = len(outline)
                    faces = [tuple(range(n5)), tuple(range(2 * n5 - 1, n5 - 1, -1))] + [(k, n5 + k, n5 + (k + 1) % n5, (k + 1) % n5) for k in range(n5)]
                    plate.add(top_v + low_v, faces, 0)
                    plates += 1
                    pobj = plate.object("plate-%d" % plates, [plate_m], origin=(0, 0, 0), closed=True)
                    pobj.location = (hinge.x, hinge.y, jr_ground(hinge.x, hinge.y) + 0.005)
                    # Its local +Y (from the hinge across the plate) toward the fissure.
                    yaw = heading + (math.pi if side > 0 else 0.0)
                    tilt = math.radians(rng.uniform(12, 30))
                    when = t0 + (t1 - t0) * s
                    jr_keys(pobj, "rotation_euler", [(0, (0, 0, yaw)), (when, (0, 0, yaw)), (min(7.0, when + 0.35), (tilt, 0, yaw))])
            along += seg

    # ---- The eyes in the dark (bi-eye): each a mesh of two glowing slits, 15 m off at the SVG's place,
    # at least 0.5 m up; opening, blinking, open, narrowed (the old dimming) by its own delay.
    eye = shader_material("eye", "#d6b4ff", "emission")
    for i, (x, y, delay) in enumerate(((188, 402, 0), (298, 470, 0.35), (394, 356, 0.7), (834, 384, 0.2), (948, 452, 0.55), (1052, 362, 0.9), (702, 300, 1.1), (470, 286, 1.3))):
        place = svg_point(x, y, 15)
        place.z = max(place.z, 0.5)
        px = (svg_point(x + 1, y, 15) - svg_point(x, y, 15)).length
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

    # ---- The foreground (bi-jr-near), unlit #040208 silhouettes 4.5–6 m from the camera, each in the
    # SVG's corner: a root arch with ferns (bottom left), a bush of leaves (bottom right), overhanging
    # branches with leaves (top left and right). At eye height the frame's bottom meets the floor only
    # 8 m off, so the lower ones stand up to about 1.2 m to show.
    near = shader_material("near", "#040208", "background")
    near.use_backface_culling = False

    def near_object(name, build, anchor):
        mesh = JrMesh()
        build(mesh)
        return mesh.object(name, [near], origin=anchor, ink=False)

    def root_arch(mesh):
        a, b = svg_point(-60, 640, 5.0), svg_point(230, 660, 5.4)
        a.z, b.z = -0.2, -0.2
        top = (a + b) / 2 + Vector((0, 0.2, 0))
        top.z = 0.92
        path = [a.lerp(top, s) for s in (0, 0.3, 0.6, 0.85)] + [top] + [top.lerp(b, s) for s in (0.2, 0.5, 0.8, 1.0)]
        path = [p + Vector((0, 0, 0.1 * math.sin(math.pi * k / 8))) for k, p in enumerate(path)]
        mesh.tube(path, [0.15, 0.14, 0.12, 0.11, 0.1, 0.095, 0.09, 0.085, 0.08], 8, 0, rng, jitter=0.1)
        for k in range(10):
            h = rng.uniform(0.4, 2.7)
            base = top.lerp(a, rng.uniform(0, 0.5)) + Vector((0, 0, 0.05))
            mesh.blade(base, (math.cos(h), -0.3 + math.sin(h) * 0.3), rng.uniform(0.5, 0.9), 0.15, 0.8, 0)
        stub = svg_point(30, 540, 5.0)
        mesh.tube([Vector((stub.x - 0.2, stub.y, 0.3)), stub, stub + Vector((0.3, 0.05, 0.3))], [0.11, 0.07, 0.0], 6, 0, rng)

    def bush(mesh):
        c = svg_point(1110, 590, 5.4)
        for k in range(150):
            base = c + Vector((rng.uniform(-0.6, 0.6), rng.uniform(-0.2, 0.2), rng.uniform(-0.5, 0.35)))
            h = rng.uniform(0, 2 * math.pi)
            d = Vector((math.cos(h) * 0.7, rng.uniform(-0.3, 0.3), math.sin(h) * 0.6 + 0.3))
            mesh.leaf(base, d, Vector((0, 1, 0)).cross(d), rng.uniform(0.12, 0.22), rng.uniform(0.05, 0.09), 0)
        for k in range(4):
            s = c + Vector((rng.uniform(-0.3, 0.3), 0, -1.0))
            mesh.tube([s, s + Vector((rng.uniform(-0.3, 0.3), 0, 0.7)), s + Vector((rng.uniform(-0.5, 0.5), 0.1, 1.3))], [0.03, 0.02, 0.0], 5, 0)

    def branch(mesh, start_svg, end_svg, droop):
        a, b = svg_point(*start_svg, 4.6), svg_point(*end_svg, 4.6)
        path = [a.lerp(b, s) + Vector((0, 0, -droop * math.sin(math.pi * s))) for s in (0, 0.25, 0.5, 0.75, 1.0)]
        mesh.tube(path, [0.11, 0.08, 0.06, 0.04, 0.0], 6, 0, rng, jitter=0.12)
        for k in range(26):
            s = rng.uniform(0.25, 1.0)
            p = a.lerp(b, s) + Vector((0, 0, -droop * math.sin(math.pi * s)))
            for j in range(3):
                h = rng.uniform(0, 2 * math.pi)
                d = Vector((math.cos(h) * 0.5, rng.uniform(-0.3, 0.3), -0.8 + 0.4 * math.sin(h)))
                mesh.leaf(p, d, Vector((0, 1, 0)).cross(d), rng.uniform(0.16, 0.3), rng.uniform(0.07, 0.12), 0)

    near_object("near-1", root_arch, svg_point(100, 640, 5.0))
    near_object("near-2", bush, svg_point(1100, 600, 5.4))
    near_object("near-3", lambda m: (branch(m, (-60, -20), (260, 40), 0.15), branch(m, (-40, 40), (150, 85), 0.1)), svg_point(100, 20, 4.6))
    near_object("near-4", lambda m: (branch(m, (1260, -20), (930, 30), 0.15), branch(m, (1240, 50), (1060, 80), 0.1)), svg_point(1100, 20, 4.6))

    # ---- Where the figures stand (the test figure faces −Y, the camera). The boss rises out of the
    # ground in the rift's light 2 m before it (bi-boss-rise; scale 0.9 → 0.96 → 1.03 of his size,
    # 1.25 times the test figure's).
    king = 1.25
    stand = (foot.x, foot.y - 2.0)
    boss = empty("boss", location=(*stand, -2.4), scale=(king * 0.9,) * 3)
    jr_keys(boss, "location", [(0, (*stand, -2.4)), (2.1, (*stand, -2.4)), (2.94, (*stand, -0.04 * 1.84 * king)), (7.0, (*stand, 0))])
    jr_keys(boss, "scale", [(0, (king * 0.9,) * 3), (2.1, (king * 0.9,) * 3), (2.94, (king * 0.96,) * 3), (7.0, (king * 1.03,) * 3)])
    # The imps (bi-imp-fly, ease-out, delays 0, 0.15, 0.35, 0.5 s): hidden (a near-zero scale: the old opacity 0) inside the rift to 1.68 s, growing as they fly out to
    # their places (the SVG's, hovering 8.5, 13, 11 and 11 m from the camera) by 3.36 s, bobbing up
    # 0.1 m, back, up 0.05 m; 0.4 of the test figure; each turned 30° outward.
    imp = 0.4
    inside = centre + Vector((0, 0.3, -0.6))
    for i, (x, y, depth, delay, turn) in enumerate(((168, 293, 8.5, 0, -30), (972, 259, 13, 0.15, 30), (324, 449, 11, 0.35, -30), (900, 463, 11, 0.5, 30))):
        place = svg_point(x, y, depth)
        node = empty("imp-%d" % (i + 1), location=inside, scale=(0.001,) * 3, rotation=(0, 0, turn))
        d = delay
        jr_keys(node, "location", [(0, inside), (1.68 + d, inside), (3.36 + d, place), (4.34 + d, place + Vector((0, 0, 0.1))),
                                   (5.32 + d, place), (7.0 + d, place + Vector((0, 0, 0.05)))])
        jr_keys(node, "scale", [(0, (0.001,) * 3), (1.68 + d, (0.001,) * 3), (3.36 + d, (imp,) * 3)])

    # Emitter marks for the sequence's particles: the mist (a low wide spawn, drifting across as
    # bi-mist does) and embers at the rift's foot.
    mist = empty("mist", location=(-0.5, foot.y - 4.0, 0.3), scale=(7, 3, 0.4))
    jr_keys(mist, "location", [(0, (-0.5, foot.y - 4.0, 0.3)), (7.0, (0.5, foot.y - 4.0, 0.3))])
    empty("embers", location=(foot.x, foot.y - 0.2, 0.1), scale=(0.8, 0.3, 0.2))

    # The camera on its dolly (bi-camera's push-in: 1.02 m forward, 0.13 m down over the 7 s).
    dolly = empty("dolly", location=JR_CAMERA)
    jr_keys(dolly, "location", [(0, JR_CAMERA), (7.0, (JR_CAMERA[0], JR_CAMERA[1] + 1.02, JR_CAMERA[2] - 0.13))])
    lens_data = bpy.data.cameras.new("Lens")
    lens_data.sensor_fit, lens_data.sensor_height, lens_data.lens = "VERTICAL", JR_SENSOR, JR_LENS
    lens_data.clip_start, lens_data.clip_end = 0.1, 100
    camera = bpy.data.objects.new("camera", lens_data)
    scene.collection.objects.link(camera)
    camera.parent = dolly
    camera.rotation_euler = (math.radians(90 + JR_PITCH), 0, 0)
    scene.camera = camera

    # One sun (Q12): behind the rift, 40° up, shining toward the camera: what faces the rift is lit.
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


# Ji'ruan's acting clips (Docs/Scene.md, step 7, phase 1b; S7-2): cutscene clips authored on the stand-in
# rig (the test figure's bones, FIGURE_BONES), keyed as poses with anticipation, overshoot and settle,
# the head and hands following the body a few frames late; bone clips relative to rest, so any rig of
# the family plays them by bone names (M7). jiruan-acting.glb holds the armature alone with these clips;
# the sequence plays them on the bound figures as NLA strips (Q16).
JR_AXES = {"X": Vector((1, 0, 0)), "Y": Vector((0, 1, 0)), "Z": Vector((0, 0, 1))}
JR_BONES = DEFORMING(FIGURE_BONES)


def jr_mirror(turns):
    """A left limb's turns for the right one: about X the same, about Y and Z the other way."""
    return [(a, d if a == "X" else -d) for a, d in turns]


def jr_body(hips=(0, 0, 0), hips_x=0, sway=0, twist=0, spine=0, chest=0, chest_z=0, neck=0, head=0, head_z=0, head_y=0,
            arm=(), arm_r=None, fore=0, fore_r=None, hand=0, hand_r=None, thigh=(), thigh_r=None, shin=0, shin_r=None, foot=0, foot_r=None):
    """A whole-body pose: turns about the armature's axes (X the figure's left: + bends a spine bone
    forward, lifts a hanging limb backward; Y its back: − swings the left arm or leg out; Z up: + turns
    toward its left), each bone's turns applied in its parent's frame; the right side mirrors the left
    unless given; the hips' offset in metres (the armature's axes)."""
    return {
        "_hips": hips,
        "hips": [("X", hips_x), ("Y", sway), ("Z", twist)],
        "spine": [("X", spine)], "chest": [("X", chest), ("Z", chest_z)], "neck": [("X", neck)],
        "head": [("X", head), ("Z", head_z), ("Y", head_y)],
        "upper_arm.L": list(arm), "upper_arm.R": list(arm_r) if arm_r is not None else jr_mirror(arm),
        "forearm.L": [("X", fore)], "forearm.R": [("X", fore if fore_r is None else fore_r)],
        "hand.L": [("X", hand)], "hand.R": [("X", hand if hand_r is None else hand_r)],
        "thigh.L": list(thigh), "thigh.R": list(thigh_r) if thigh_r is not None else jr_mirror(thigh),
        "shin.L": [("X", shin)], "shin.R": [("X", shin if shin_r is None else shin_r)],
        "foot.L": [("X", foot)], "foot.R": [("X", foot if foot_r is None else foot_r)],
    }


def jr_with(pose, **changes):
    """A pose with some bones' turns replaced (bone names with "_" for "." : upper_arm_R)."""
    out = dict(pose)
    for name, value in changes.items():
        out[name.replace("_L", ".L").replace("_R", ".R")] = value
    return out


class JrClip:
    """An action keyed pose by pose on the rig (frames from 1, 60 fps)."""

    def __init__(self, rig, name):
        self.rig, self.action, self.last = rig, bpy.data.actions.new(name), {}

    def key(self, frame, pose, bones=None, chest_scale=None):
        rig = self.rig
        rig.animation_data.action = self.action
        for name in bones or JR_BONES:
            q = Quaternion()
            for axis, degrees in pose.get(name, []):
                q = Quaternion(JR_AXES[axis], math.radians(degrees)) @ q
            rest = rig.data.bones[name].matrix_local.to_quaternion()
            local = rest.inverted() @ q @ rest
            previous = self.last.get(name)
            if previous is not None and previous.dot(local) < 0:
                local.negate()  # the short way from the last key: Blender interpolates the components
            self.last[name] = local.copy()
            pose_bone = rig.pose.bones[name]
            pose_bone.rotation_mode = "QUATERNION"
            pose_bone.rotation_quaternion = local
            pose_bone.keyframe_insert("rotation_quaternion", frame=frame)
        if bones is None or "hips" in bones:
            pose_bone = rig.pose.bones["hips"]
            pose_bone.location = rig.data.bones["hips"].matrix_local.to_3x3().inverted() @ Vector(pose["_hips"])
            pose_bone.keyframe_insert("location", frame=frame)
        if chest_scale is not None:
            pose_bone = rig.pose.bones["chest"]
            pose_bone.scale = (chest_scale[0], 1, chest_scale[1])
            pose_bone.keyframe_insert("scale", frame=frame)

    def marker(self, name, frame):
        self.action.pose_markers.new(name).frame = frame


# The king's poses.
JR_STAND = jr_body(spine=-2, chest=-5, head=4, arm=[("Y", -10), ("X", 4)], fore=-20, hand=-10, thigh=[("X", -3), ("Y", -3)], shin=6, foot=-3)
JR_STANCE = jr_body(spine=-2, chest=-6, neck=-2, head=-5, arm=[("Y", -40), ("X", 16)], fore=-90, hand=-15, thigh=[("X", -2), ("Y", -7)], shin=4, foot=-2)
JR_OPEN = jr_body(hips=(0, 0, 0.005), hips_x=-3, spine=-6, chest=-14, neck=-8, head=-17, arm=[("Y", -100), ("X", 9)], fore=-16, hand=12,
                  thigh=[("X", 0), ("Y", -8)], shin=2)
# The imps' poses.
JR_TUCK = jr_body(hips=(0, 0, -0.22), hips_x=20, spine=22, chest=32, neck=16, head=36, arm=[("X", -55), ("Y", 22)], fore=-105, hand=-25,
                  thigh=[("X", -125)], shin=145, foot=30)
JR_HOVER = jr_body(hips_x=6, spine=4, chest=6, head=-6, arm=[("Y", -52), ("X", 14)], fore=-40, hand=10,
                   thigh=[("X", -36), ("Y", -6)], thigh_r=[("X", -6), ("Y", 6)], shin=58, shin_r=34, foot=30)
JR_HUNCH = jr_body(hips=(0, 0, -0.12), hips_x=10, spine=12, chest=24, neck=-8, head=-16, arm=[("X", -32), ("Y", -8)], fore=-52, hand=-32,
                   thigh=[("X", -42), ("Y", -8)], shin=74, foot=-30)


def jr_rise(rig):
    """rise (1.4 s): crouched low, head down, arms hugged in; sinking a little deeper (anticipation),
    then unfolding upward, the chest and head lifting last and past upright (overshoot, marker
    "risen"), settling into a proud stand with the head slightly bowed. Played as the mark lifts him
    out of the light."""
    clip = JrClip(rig, "rise")
    crouch = jr_body(hips=(0, 0.08, -0.45), hips_x=30, spine=18, chest=25, neck=15, head=30, arm=[("X", -35), ("Y", 18)], fore=-80, hand=-30,
                     thigh=[("X", -115)], shin=135, foot=-40)
    clip.key(1, crouch)
    clip.key(16, jr_body(hips=(0, 0.09, -0.5), hips_x=34, spine=20, chest=28, neck=17, head=36, arm=[("X", -40), ("Y", 20)], fore=-84, hand=-32,
                         thigh=[("X", -122)], shin=142, foot=-44))
    clip.key(44, jr_body(hips=(0, 0.03, -0.18), hips_x=12, spine=8, chest=9, neck=6, head=20, arm=[("Y", -14), ("X", -8)], fore=-35, hand=-15,
                         thigh=[("X", -48), ("Y", -2)], shin=74, foot=-22))
    clip.key(64, jr_body(hips=(0, 0, 0.02), hips_x=-4, spine=-4, chest=-12, neck=-5, head=-8, arm=[("Y", -16), ("X", 8)], fore=-14, hand=-5,
                         thigh=[("X", -2), ("Y", -3)], shin=3, foot=1))
    clip.key(85, JR_STAND)
    clip.marker("risen", 64)
    return clip.action


def jr_spread(rig):
    """spread (1.2 s): from the stand the arms pull in across the chest and the knees give
    (anticipation), then fling wide and up, chest out, head back (marker "crown", 0.48 s in: played
    from 3.6 s it lands on the old flash's peak), overshooting and easing back into the held, open pose."""
    clip = JrClip(rig, "spread")
    clip.key(1, JR_STAND)
    clip.key(14, jr_body(hips=(0, 0, -0.07), hips_x=4, spine=6, chest=15, neck=6, head=20, arm=[("X", -38), ("Y", 30)], fore=-112, hand=-35,
                         thigh=[("X", -14)], shin=24, foot=-10))
    clip.key(26, jr_body(hips=(0, -0.01, 0.03), hips_x=-3, spine=-6, chest=-17, neck=-8, head=-22, arm=[("Y", -104), ("X", 12)], fore=-14, hand=18,
                         thigh=[("X", 0), ("Y", -8)], shin=2))
    clip.key(33, jr_with(jr_body(hips=(0, -0.01, 0.035), hips_x=-3, spine=-7, chest=-20, neck=-10, head=-27, arm=[("Y", -118), ("X", 18)], fore=-8,
                                 hand=24, thigh=[("X", 0), ("Y", -8)], shin=2)))
    clip.key(50, jr_body(hips=(0, 0, 0.01), hips_x=-3, spine=-6, chest=-15, neck=-8, head=-18, arm=[("Y", -102), ("X", 10)], fore=-16, hand=12,
                         thigh=[("X", 0), ("Y", -8)], shin=2))
    clip.key(73, JR_OPEN)
    clip.marker("crown", 30)
    return clip.action


def jr_settle(rig):
    """settle (1.2 s): from the open pose the arms lift a touch (anticipation), drop, come in to the
    hips (marker "stance"), the weight sinking and bouncing back into a wide, commanding stance."""
    clip = JrClip(rig, "settle")
    clip.key(1, JR_OPEN)
    clip.key(12, jr_with(JR_OPEN, upper_arm_L=[("Y", -108), ("X", 12)], upper_arm_R=jr_mirror([("Y", -108), ("X", 12)]),
                         chest=[("X", -15), ("Z", 0)]))
    clip.key(26, jr_body(hips=(0, 0, -0.02), spine=-3, chest=-8, neck=-4, head=-8, arm=[("Y", -28), ("X", -6)], fore=-30, hand=-5,
                         thigh=[("X", -4), ("Y", -8)], shin=7, foot=-3))
    clip.key(42, jr_body(hips=(0, 0, -0.03), spine=-2, chest=-6, neck=-2, head=-2, arm=[("Y", -44), ("X", 20)], fore=-96, hand=-22,
                         thigh=[("X", -6), ("Y", -7)], shin=11, foot=-5))
    clip.key(54, jr_body(hips=(0, 0, -0.012), spine=-2, chest=-6, neck=-2, head=-4, arm=[("Y", -41), ("X", 17)], fore=-90, hand=-17,
                         thigh=[("X", -3), ("Y", -7)], shin=6, foot=-3))
    clip.key(73, JR_STANCE)
    clip.marker("stance", 42)
    return clip.action


def jr_idle_king(rig):
    """idle-king (2.4 s, loops): breathing in the stance, the chest rising and swelling, the weight
    swaying side to side, the head surveying left, then right."""
    clip = JrClip(rig, "idle-king")
    inhale = dict(hips=(0, 0, 0.008), spine=-3, chest=-8, neck=-2, arm=[("Y", -42), ("X", 17)], fore=-88, hand=-14,
                  thigh=[("X", -2), ("Y", -7)], shin=4, foot=-2)
    clip.key(1, JR_STANCE, chest_scale=(1, 1))
    clip.key(37, jr_body(sway=1.2, head=-7, head_z=5, **inhale), chest_scale=(1.022, 1.03))
    clip.key(73, JR_STANCE, chest_scale=(1, 1))
    clip.key(109, jr_body(sway=-1.2, head=-7, head_z=-5, **inhale), chest_scale=(1.022, 1.03))
    clip.key(145, JR_STANCE, chest_scale=(1, 1))
    return clip.action


def jr_fly_out(rig):
    """fly-out (1.7 s): tucked in a ball, curling tighter (anticipation), then flung open, arms and legs
    wide, head thrown back as it shoots out of the rift (marker "burst"), the limbs trailing behind,
    gathering into the hover."""
    clip = JrClip(rig, "fly-out")
    clip.key(1, JR_TUCK)
    clip.key(14, jr_body(hips=(0, 0, -0.25), hips_x=22, spine=24, chest=38, neck=18, head=44, arm=[("X", -62), ("Y", 24)], fore=-110, hand=-28,
                         thigh=[("X", -132)], shin=150, foot=32))
    clip.key(26, jr_body(hips_x=-10, spine=-12, chest=-24, neck=-10, head=-28, arm=[("Y", -112), ("X", -22)], fore=-8, hand=12,
                         thigh=[("X", 22), ("Y", -35)], shin=32, foot=40))
    clip.key(40, jr_body(hips_x=-4, spine=-6, chest=-12, neck=-6, head=-14, arm=[("Y", -92), ("X", 38)], fore=-20, hand=16,
                         thigh=[("X", 38), ("Y", -24)], shin=55, foot=45))
    clip.key(64, jr_body(hips_x=6, spine=3, chest=4, head=-2, arm=[("Y", -70), ("X", 10)], fore=-45, hand=6,
                         thigh=[("X", -22), ("Y", -10)], shin=52, foot=28))
    clip.key(103, JR_HOVER)
    clip.marker("burst", 26)
    return clip.action


def jr_hover(rig):
    """hover (0.8 s, loops): treading air: the legs kicking in turn, the arms beating down and up like
    wings (the stand-in has none), the body lifting on each down-beat."""
    clip = JrClip(rig, "hover")
    up = dict(arm=[("Y", -88), ("X", -6)], fore=-30, hand=14)
    clip.key(1, JR_HOVER)
    clip.key(13, jr_body(hips=(0, 0, 0.035), hips_x=6, spine=3, chest=3, head=-8, thigh=[("X", -22), ("Y", -6)], thigh_r=[("X", -20), ("Y", 6)],
                         shin=46, foot=30, **up))
    clip.key(25, jr_body(hips_x=6, spine=4, chest=6, head=-6, arm=[("Y", -52), ("X", 14)], fore=-40, hand=10,
                         thigh=[("X", -6), ("Y", -6)], thigh_r=[("X", -36), ("Y", 6)], shin=34, shin_r=58, foot=30))
    clip.key(37, jr_body(hips=(0, 0, 0.035), hips_x=6, spine=3, chest=3, head=-8, thigh=[("X", -20), ("Y", -6)], thigh_r=[("X", -22), ("Y", 6)],
                         shin=46, foot=30, **up))
    clip.key(49, JR_HOVER)
    return clip.action


def jr_idle_imp(rig):
    """idle-imp (1.6 s, loops): hunched on bent knees, claws out, twitchy: the head snaps to one side
    and the other (five-frame snaps, held: faster ones bend more between frames than the 60 fps samples
    follow), a claw jerks up, the head cocks."""
    clip = JrClip(rig, "idle-imp")
    clip.key(1, JR_HUNCH, chest_scale=(1, 1))
    clip.key(48, jr_with(JR_HUNCH, hips=[("X", 12), ("Y", 0), ("Z", 0)], chest=[("X", 27), ("Z", 0)]), chest_scale=(1.03, 1.03))
    clip.key(97, JR_HUNCH, chest_scale=(1, 1))
    head = lambda z, y=0: {"head": [("X", -16), ("Z", z), ("Y", y)]}  # noqa: E731
    for frame, z, y in ((16, 0, 0), (21, 30, 0), (38, 26, 0), (43, -12, 0), (58, -10, 0), (71, -4, 0), (76, -4, 14), (86, -2, 12)):
        clip.key(frame, head(z, y), bones=["head"])
    for frame, lift in ((52, 0), (57, -15), (62, -14), (68, 0)):
        clip.key(frame, {"upper_arm.R": [("X", -32 + lift), ("Y", 8)], "forearm.R": [("X", -52 + lift)]}, bones=["upper_arm.R", "forearm.R"])
    return clip.action


def jiruan_acting(path):
    """Ji'ruan's acting clips (see above) on the test figure's armature alone, at 60 fps, exported as M7's
    animated models are (ANIMATED: each action its own glTF animation from 0 s, its pose markers as the
    animation's events): the king's rise, spread, settle and idle-king, the imps' fly-out, hover and
    idle-imp."""
    bpy.context.scene.render.fps, bpy.context.scene.render.fps_base = 60, 1
    rig = armature("Figure", FIGURE_BONES)
    rig.animation_data_create()
    actions = [jr_rise(rig), jr_spread(rig), jr_settle(rig), jr_idle_king(rig), jr_fly_out(rig), jr_hover(rig), jr_idle_imp(rig)]
    stack_actions(rig, actions)
    export(path, **ANIMATED)

# Builders by model name (evaluate_animations.py builds the animated ones again).
BUILDERS = {"blocks": blocks, "rigged": rigged, "shapes": shapes, "stage": stage, "fringe": fringe, "glowing": glowing,
            "figure": figure, "reach": reach, "stocky": stocky, "sword": sword, "tabard": tabard, "pennant": pennant,
            "hierarchy": hierarchy, "dolly": dolly, "jiruan": jiruan,
            "jiruan-acting": jiruan_acting}


def main():
    output = sys.argv[sys.argv.index("--") + 1]
    os.makedirs(output, exist_ok=True)
    for name, build in BUILDERS.items():
        reset()
        build(os.path.join(output, name + ".glb"))


if __name__ == "__main__":
    main()

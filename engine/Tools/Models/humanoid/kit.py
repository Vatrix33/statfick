# The modelling kit of the humanoid family (GM2 step 4): every shape is made by code (GP-4), as the old sprites were
# drawn by code: lofts through superellipse sections, tubes along paths, quad spheres shaped by functions, and the
# materials by palette slot. Blender's coordinates: Z up, the figure faces −Y, its left side is +X; metres.
import math

import bmesh
import bpy
from mathutils import Vector


def sgn(v):
    return -1.0 if v < 0 else 1.0


def superellipse(n, rx, ry, power=2.0, phase=0.0):
    """n points of a superellipse in its own plane (x, y), counter-clockwise from +x: power 2 an ellipse, more a box."""
    points = []
    for i in range(n):
        a = 2 * math.pi * (i + phase) / n
        c, s = math.cos(a), math.sin(a)
        points.append((rx * sgn(c) * abs(c) ** (2 / power), ry * sgn(s) * abs(s) ** (2 / power)))
    return points


def frames(points, side):
    """A frame (tangent, x, y) per point of a path, x as near `side` as the tangent allows: a tube's sections keep their
    turn along a bending path."""
    out = []
    count = len(points)
    for i, p in enumerate(points):
        a = points[max(i - 1, 0)]
        b = points[min(i + 1, count - 1)]
        t = (b - a).normalized()
        x = side - t * side.dot(t)
        if x.length < 1e-6:
            x = Vector((1, 0, 0)) - t * t.x
        x.normalize()
        y = t.cross(x).normalized()
        out.append((t, x, y))
    return out


def new_object(name, bm, material=None, smooth=True):
    """The bmesh as an object in the scene, with its one material."""
    mesh = bpy.data.meshes.new(name)
    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    if material is not None:
        mesh.materials.append(material)
    if smooth:
        mesh.shade_smooth()
    return obj


def loft(name, sections, n=16, cap_start="fan", cap_end="fan", material=None, smooth=True):
    """A tube through sections; each section is a dict: p (centre), x, y (unit axes), rx, ry, power (2 an ellipse, higher
    boxier), and optionally dx, dy (the centre moved in its plane), phase, and bulge (how far a fan cap's pole stands out).
    Caps: "fan" (a pole vertex), "open", or "flat" (an n-gon)."""
    bm = bmesh.new()
    rings = []
    for s in sections:
        centre = s["p"] + s["x"] * s.get("dx", 0.0) + s["y"] * s.get("dy", 0.0)
        ring = [bm.verts.new(centre + s["x"] * px + s["y"] * py)
                for px, py in superellipse(n, s["rx"], s["ry"], s.get("power", 2.0), s.get("phase", 0.0))]
        rings.append(ring)
    for a, b in zip(rings, rings[1:]):
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((a[i], a[j], b[j], b[i]))
    for which, ring, section in ((cap_start, rings[0], sections[0]), (cap_end, rings[-1], sections[-1])):
        first = ring is rings[0]
        if which == "fan":
            centre = section["p"] + section["x"] * section.get("dx", 0.0) + section["y"] * section.get("dy", 0.0)
            bulge = section.get("bulge", 0.0)
            t = section["x"].cross(section["y"])
            pole = bm.verts.new(centre + t * (-bulge if first else bulge))
            for i in range(n):
                j = (i + 1) % n
                bm.faces.new((ring[j], ring[i], pole) if first else (ring[i], ring[j], pole))
        elif which == "flat":
            bm.faces.new(ring[::-1] if first else ring)
    return new_object(name, bm, material, smooth)


def tube(name, points, radii, side=Vector((1, 0, 0)), n=16, power=2.0, cap_start="fan", cap_end="fan",
         material=None, smooth=True, bulge=(0.0, 0.0)):
    """A loft along a path; radii are (rx, ry) per point (rx along `side`), or one number; power one number or one per
    point; bulge the two fan caps' poles."""
    sections = []
    for i, (p, r, (t, x, y)) in enumerate(zip(points, radii, frames(points, side))):
        rx, ry = (r, r) if isinstance(r, (int, float)) else r
        pw = power[i] if isinstance(power, (list, tuple)) else power
        sections.append({"p": p, "x": x, "y": y, "rx": rx, "ry": ry, "power": pw})
    sections[0]["bulge"] = bulge[0]
    sections[-1]["bulge"] = bulge[1]
    return loft(name, sections, n, cap_start, cap_end, material, smooth)


def quad_sphere(cuts=7):
    """A bmesh unit sphere of quads: a cube, cut and blown up (even faces, no poles)."""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=2.0)
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=cuts, use_grid_fill=True)
    for v in bm.verts:
        v.co = v.co.normalized()
    return bm


def shaped_sphere(name, shape, cuts=7, material=None):
    """A quad sphere whose every unit direction d is moved to shape(d)."""
    bm = quad_sphere(cuts)
    for v in bm.verts:
        v.co = shape(v.co.normalized())
    return new_object(name, bm, material)


def direction(yaw, pitch):
    """A unit direction from a head's centre: yaw 0 is the face's front (−Y), positive toward the figure's left (+X);
    pitch positive is up. Degrees."""
    y, p = math.radians(yaw), math.radians(pitch)
    return Vector((math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), math.sin(p)))


def subdivide(obj, levels=2):
    modifier = obj.modifiers.new("subdivision", "SUBSURF")
    modifier.levels = levels
    modifier.render_levels = levels
    return obj


def solidify(obj, thickness, offset=-1.0):
    modifier = obj.modifiers.new("solidify", "SOLIDIFY")
    modifier.thickness = thickness
    modifier.offset = offset
    return obj


def apply_modifiers(objects):
    """Replaces each object's mesh by its evaluated one (its modifiers applied, in order)."""
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in objects:
        if not obj.modifiers:
            continue
        evaluated = obj.evaluated_get(depsgraph)
        mesh = bpy.data.meshes.new_from_object(evaluated, preserve_all_data_layers=True, depsgraph=depsgraph)
        old = obj.data
        obj.modifiers.clear()
        obj.data = mesh
        mesh.name = old.name
        bpy.data.meshes.remove(old)
        if any(p.use_smooth for p in mesh.polygons):
            mesh.shade_smooth()


def join(objects, name):
    """Pieces joined into the first of them, renamed `name` (Blender's join: their vertex groups merge by name, the
    first's modifiers and custom properties stay, their materials become one list)."""
    if len(objects) > 1:
        bpy.context.view_layer.update()            # objects removed a moment ago leave the layer only after this
        for obj in bpy.context.view_layer.objects:
            obj.select_set(False)
        for obj in objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = objects[0]
        bpy.ops.object.join()
    objects[0].name = name
    return objects[0]


def join_by_ink(objects, name):
    """A model's pieces as one object per ink flag (GM2-P19: one primitive per material and ink flag, the fewest
    draws): the inked ones as `name`, the ones the ink leaves alone (custom property ink = 0) as `name`-unlined.
    Returns them."""
    groups = [[obj for obj in objects if obj.get("ink", 1) == flag] for flag in (1, 0)]       # before a join removes any
    return [join(group, name if flag else name + "-unlined") for flag, group in zip((1, 0), groups) if group]


def lit_material(name, colour, hatch=True):
    """A cel-shaded material named for its palette slot (the engine colours it from the look's palette; `colour`, linear,
    shows only where a palette has no such slot). Without hatching (the material's extras say "hatch": 0) where strokes
    would cross a drawn line: the brows, lashes and mouth, the mail's rows."""
    material = bpy.data.materials.get(name)
    if material is None:
        material = bpy.data.materials.new(name)
        material.use_nodes = True
        principled = next(n for n in material.node_tree.nodes if n.bl_idname == "ShaderNodeBsdfPrincipled")
        principled.inputs["Base Color"].default_value = (*colour, 1)
        principled.inputs["Roughness"].default_value = 1.0
        principled.inputs["Metallic"].default_value = 0.0
        if not hatch:
            material["hatch"] = 0
    return material


def unlit_material(name, colour):
    """A flat colour, as the lizardman test's pupils (a Background shader: glTF's unlit; `colour` linear): the eyes."""
    material = bpy.data.materials.get(name)
    if material is None:
        material = bpy.data.materials.new(name)
        material.use_nodes = True
        nodes = material.node_tree.nodes
        nodes.clear()
        background = nodes.new("ShaderNodeBackground")
        background.inputs["Color"].default_value = (*colour, 1)
        output = nodes.new("ShaderNodeOutputMaterial")
        material.node_tree.links.new(background.outputs[0], output.inputs["Surface"])
    return material

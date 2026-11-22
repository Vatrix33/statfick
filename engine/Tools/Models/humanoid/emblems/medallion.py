# The nine race emblems as medallions in relief (GM2 step 10; Docs/Game/Hero.md, "The nine seals as 3D emblems", Q35-Q37,
# the user's motifs Q66, the manager's make GM2-P32), built in Blender 5.2 from the outlines in chosen.json beside this
# file: the manager's builder as the user approved its models (2026-11-20, "Yes, all nine"), its geometry, heights and
# material slots unchanged. One unit (the motifs' field radius) is U metres. A medallion: a turned body (128 sides: a plain
# back, the rim out to 1.33 units, the field set 0.145 units under the rim's top) with 40 beads round the rim, and the motif
# raised on the field: each outline a slab chamfered at its top (HEIGHT off the field by its tone; an element that lies
# over an earlier one at least STEP higher); a "lo" element a thin inlay with no ink on what is under it. Its front looks
# along the figures' front (Blender's -Y, glTF's +Z), the motif's up along +Z (glTF's +Y), the medallion's middle at the
# origin. Materials by palette slot (look.EMBLEM_SLOTS), coloured from the race's accent by palette.py's rule (GM2-P31);
# the colours here show only where a palette has none.
# The manifest's builder "emblems.medallion:emblem_model" with the emblem's name (a race's `emblem`, Q44) as its argument:
# models/humanoid/emblems/<name>.
import json
import math
import os

import bmesh
import bpy
from mathutils import Matrix, Vector

import kit as k
import make_models

HERE = os.path.dirname(os.path.abspath(__file__))

U = 0.08                                   # the field's radius in metres: the motifs' unit
HEIGHT = {"hi": 0.070, "mid": 0.050, "acc": 0.056}     # how far a tone stands off the field, in units
STEP = 0.022                               # an element over another stands this much higher
SLOT = {"hi": "emblem.high", "mid": "emblem.metal", "acc": "emblem.accent", "lo": "emblem.cut"}


def outlines():
    """The chosen motifs by emblem name (chosen.json, the user's Q66)."""
    with open(os.path.join(HERE, "chosen.json")) as f:
        return json.load(f)


def materials():
    return {"emblem.rim": k.lit_material("emblem.rim", (0.50, 0.27, 0.05)), "emblem.field": k.lit_material("emblem.field", (0.10, 0.06, 0.04)),
            "emblem.high": k.lit_material("emblem.high", (0.75, 0.65, 0.42)), "emblem.metal": k.lit_material("emblem.metal", (0.50, 0.27, 0.05)),
            "emblem.accent": k.lit_material("emblem.accent", (0.5, 0.2, 0.1)), "emblem.cut": k.lit_material("emblem.cut", (0.02, 0.012, 0.008), hatch=False)}


def inside(p, polygon):
    x, y = p
    hit = False
    j = len(polygon) - 1
    for i in range(len(polygon)):
        (xi, yi), (xj, yj) = polygon[i], polygon[j]
        if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi) + xi:
            hit = not hit
        j = i
    return hit


def overlap(a, b):
    """Whether two outlines overlap: their boxes meet and a point (every second) of one lies inside the other."""
    ax = [p[0] for p in a]; ay = [p[1] for p in a]; bx = [p[0] for p in b]; by = [p[1] for p in b]
    if max(ax) < min(bx) or max(bx) < min(ax) or max(ay) < min(by) or max(by) < min(ay):
        return False
    return any(inside(p, b) for p in a[::2]) or any(inside(p, a) for p in b[::2])


def thickness(points):
    """An outline's thickness: twice its area over its perimeter (a strip's width)."""
    area = abs(sum(x0 * y1 - x1 * y0 for (x0, y0), (x1, y1) in zip(points, points[1:] + points[:1]))) / 2
    around = sum(math.dist(p, q) for p, q in zip(points, points[1:] + points[:1]))
    return 2 * area / around if around else 0.0


def slab(name, points, low, high, bevel, material, ink):
    """The outline as a slab from `low` to `high` (units), its top and bottom edges chamfered by `bevel`."""
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "2D"
    curve.fill_mode = "BOTH"
    curve.extrude = max(1e-5, (high - low) / 2 * U - bevel * U)
    curve.bevel_depth = bevel * U
    curve.bevel_resolution = 0
    curve.offset = -bevel * U
    spline = curve.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for point, (x, y) in zip(spline.points, points):
        point.co = (x * U, y * U, 0.0, 1.0)
    spline.use_cyclic_u = True
    holder = bpy.data.objects.new(name + "-curve", curve)
    bpy.context.scene.collection.objects.link(holder)
    deps = bpy.context.evaluated_depsgraph_get()
    mesh = bpy.data.meshes.new_from_object(holder.evaluated_get(deps))
    bpy.data.objects.remove(holder)
    bpy.data.curves.remove(curve)
    for v in mesh.vertices:
        v.co.z += (high + low) / 2 * U
    mesh.materials.clear()
    mesh.materials.append(material)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    if not ink:
        obj["ink"] = 0
    return obj


def body(m):
    """The medallion's body, turned: the back, the rim, and the field set inside it; the beads round the rim."""
    profile = [(0.0, -0.10, 0), (1.22, -0.10, 0), (1.33, -0.04, 0), (1.33, 0.09, 0), (1.29, 0.145, 0), (1.15, 0.145, 0), (1.10, 0.10, 0), (1.075, 0.0, 0), (0.0, 0.0, 1)]
    count = 128
    bm = bmesh.new()
    rings = []
    for r, z, _ in profile:
        if r == 0.0:
            rings.append([bm.verts.new((0.0, 0.0, z * U))])
        else:
            rings.append([bm.verts.new((r * U * math.cos(2 * math.pi * i / count), r * U * math.sin(2 * math.pi * i / count), z * U)) for i in range(count)])
    for n in range(len(profile) - 1):
        a, b = rings[n], rings[n + 1]
        index = profile[n + 1][2]
        for i in range(count):
            j = (i + 1) % count
            if len(a) == 1:
                face = bm.faces.new((a[0], b[j], b[i]))
            elif len(b) == 1:
                face = bm.faces.new((a[i], a[j], b[0]))
            else:
                face = bm.faces.new((a[i], a[j], b[j], b[i]))
            face.material_index = index
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    obj = k.new_object("emblem-body", bm, m["emblem.rim"])
    obj.data.materials.append(m["emblem.field"])
    beads = bmesh.new()
    for i in range(40):                          # beads round the rim
        a = 2 * math.pi * i / 40
        made = bmesh.ops.create_icosphere(beads, subdivisions=2, radius=0.034 * U)
        for v in made["verts"]:
            v.co = Vector((v.co.x + 1.22 * U * math.cos(a), v.co.y + 1.22 * U * math.sin(a), v.co.z * 0.7 + 0.150 * U))
    return [obj, k.new_object("emblem-beads", beads, m["emblem.rim"])]


def emblem_model(path, emblem):
    """The emblem `emblem` (chosen.json's race) as a medallion, exported to `path` (glTF, the triangles in their canonical
    order: make_models.export). Its pieces joined per ink flag (GM2-P19): `emblem-<name>` and `emblem-<name>-unlined` (the
    cut-in inlays)."""
    spec = outlines()[emblem]
    m = materials()
    pieces = body(m)
    done = []                                   # (points, top) of the elements so far
    for index, element in enumerate(spec["elements"]):
        points = [tuple(p) for p in element["points"]]
        under = max([top for other, top in done if overlap(points, other)], default=0.0)
        if element["tone"] == "lo":
            top = under + 0.006
            pieces.append(slab(f"cut{index}", points, top - 0.004, top, 0.0, m[SLOT["lo"]], ink=0))
        else:
            top = max(HEIGHT[element["tone"]], under + STEP) if under else HEIGHT[element["tone"]]
            bevel = min(0.020, 0.24 * thickness(points), top * 0.45)
            pieces.append(slab(f"raised{index}", points, -0.03, top, bevel, m[SLOT[element["tone"]]], ink=1))
        done.append((points, top))
    turn = Matrix.Rotation(math.radians(90), 4, "X")          # the front to -Y, the motif's up to +Z
    for obj in pieces:
        obj.data.transform(turn)
        obj.data.shade_smooth()
        obj.data.set_sharp_from_angle(angle=math.radians(32))
    k.join_by_ink(pieces, f"emblem-{emblem}")
    make_models.export(path)

# The plain outfit (Q18: one plain outfit per race and sex, its colours the player's), made on a bound body (GM2 step 4).
# A tight garment is the body's own surface: the faces the bones that move them choose take the cloth's material and
# move out off the skin (paint), so nothing can show through it and it bends exactly as the body does; a body's model
# carries its plain outfit. Loose pieces (a collar, hems, boots, a belt) are shells cut from the body's faces and lifted,
# bound by the weights of the skin under them; a skirt is lofted around the body's measured outline. Folds are modelled
# (Q3). The human man's and woman's: the belted tunic over trousers (hose) with boots (the user's choice, 2026-11-16).
import math
import os

import bmesh
import bpy
from mathutils.bvhtree import BVHTree
from mathutils import Vector

import kit as k
import lighten
import rig as r

V = Vector
# A collar's band is relaxed twice: it lies where the neck runs into the shoulders, and large faces fold sharply there.
COLLAR_TAUT = 2
# How far out a collar may lie, as a share of the way from the middle to the shoulder's joint (the builds, GM2 step 9a).
COLLAR_REACH = 0.80


def ripple(a, b):
    """A fixed, uneven wave in two coordinates, about −1..1 (the folds' irregularity)."""
    return 0.55 * math.sin(a) + 0.30 * math.sin(2.3 * a + 1.7 * b + 0.8) + 0.15 * math.sin(4.1 * a - 2.9 * b + 2.1)


class Maker:
    """Cuts garments from a bound body: `p` its parameters, `body` its mesh object (weighted to `rig`), `joints` its joints
    (forms.body_forms), `m` the materials by slot (materials.py)."""

    def __init__(self, p, body, rig, joints, m):
        self.p, self.body, self.rig, self.joints, self.m = p, body, rig, joints, m
        self.names = {g.index: g.name for g in body.vertex_groups}
        self.pieces = []

    def bone(self, name):
        b = self.rig.data.bones[name]
        return V(b.head_local), V(b.tail_local)

    def along(self, name, point):
        """How far along the bone `point` lies: 0 at its head, 1 at its tail."""
        head, tail = self.bone(name)
        axis = tail - head
        return (point - head).dot(axis) / axis.length_squared

    def cut(self, bm, planes=(), heights=(), reach=0.10):
        """Cuts the mesh along planes (point, normal), each within `reach` of its point (a limb's own cut, not the trunk's
        behind it), and along level heights, so a piece's edge lies exactly on them whatever the faces' size."""
        for point, normal in planes:
            point = V(point)
            near = [f for f in bm.faces if (f.calc_center_median() - point).length < reach]
            geom = lighten.geometry(bm, near)      # in the mesh's order: the same cut on every build
            bmesh.ops.bisect_plane(bm, geom=geom, dist=0.00005, plane_co=point, plane_no=V(normal), clear_inner=False, clear_outer=False)
        for z in heights:
            bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], dist=0.00005, plane_co=V((0, 0, z)), plane_no=V((0, 0, 1)),
                                   clear_inner=False, clear_outer=False)

    def shell(self, name, keep, lift, material, planes=(), thickness=0.003, displace=None, heights=(), taut=0):
        """A piece of the body's faces `keep(centre, bone)` chooses (by their centre and the bone that moves them most),
        lifted off the skin by `lift` (and `displace`'s folds), relaxed `taut` times, given a thickness. The body is cut
        first along the planes (point, normal) and heights its edges lie on (a cut hem)."""
        bm = bmesh.new()
        bm.from_mesh(self.body.data)
        self.cut(bm, planes, heights)
        layer = bm.verts.layers.deform.verify()

        def face_bone(face):
            totals = {}
            for v in face.verts:
                for group, weight in v[layer].items():
                    totals[group] = totals.get(group, 0.0) + weight
            return self.names[max(totals, key=totals.get)] if totals else ""

        drop = [f for f in bm.faces if not keep(f.calc_center_median(), face_bone(f))]
        bmesh.ops.delete(bm, geom=drop, context="FACES")
        bm.verts.layers.deform.remove(layer)           # the piece is bound afresh, by bone names
        bm.normal_update()
        for v in bm.verts:
            offset = v.normal * lift
            if displace is not None:
                offset += displace(v.co, v.normal)
            v.co += offset
        obj = k.new_object(f"{self.p['name']}-{name}", bm, material, smooth=True)
        if taut:
            modifier = obj.modifiers.new("taut", "SMOOTH")
            modifier.iterations = taut
            modifier.factor = 0.5
            k.apply_modifiers([obj])
        if thickness:
            k.solidify(obj, thickness, offset=1.0)
            k.apply_modifiers([obj])
        obj.data.shade_smooth()
        r.bind_like(self.rig, obj, self.body)
        self.pieces.append(obj)
        return obj

    def paint(self, keep, lift, material, displace=None, planes=(), heights=()):
        """A tight garment as the body's own surface: the faces `keep(centre, bone)` chooses (the bone most of their
        vertices follow) take `material` and their vertices move out by `lift` (and by `displace`'s folds). Its edges are
        steps for hems to cover; the body is cut along `planes` and `heights` first where an edge must lie exactly (a hem
        narrower than a face)."""
        mesh = self.body.data
        if planes or heights:
            bm = bmesh.new()
            bm.from_mesh(mesh)
            self.cut(bm, planes, heights)
            bm.to_mesh(mesh)
            bm.free()
            mesh.update()
        if material.name not in mesh.materials:
            mesh.materials.append(material)
        slot = list(mesh.materials).index(material)
        dominant = []
        for v in mesh.vertices:
            best = max(v.groups, key=lambda g: g.weight, default=None)
            dominant.append(self.names[best.group] if best else "")
        moved = set()
        for polygon in mesh.polygons:
            votes = {}
            for index in polygon.vertices:
                votes[dominant[index]] = votes.get(dominant[index], 0) + 1
            if keep(V(polygon.center), max(votes, key=votes.get)):
                polygon.material_index = slot
                moved.update(polygon.vertices)
        normals = {index: V(mesh.vertices[index].normal) for index in moved}
        for index in moved:
            v = mesh.vertices[index]
            offset = normals[index] * lift
            if displace is not None:
                offset += displace(V(v.co), normals[index])
            v.co = V(v.co) + offset
        mesh.update()

    def outline(self, z, bones):
        """Points of the body's surface at a height, of the parts the given bones move: the body cut level there, each
        cut edge sampled every 4 mm (so it doesn't matter how large the faces are)."""
        bm = bmesh.new()
        bm.from_mesh(self.body.data)
        layer = bm.verts.layers.deform.verify()
        wanted = {index for index, name in self.names.items() if name in bones}
        made = bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], dist=0.00005, plane_co=V((0, 0, z)), plane_no=V((0, 0, 1)),
                                      clear_inner=False, clear_outer=False)
        points = []
        for e in made["geom_cut"]:
            if not isinstance(e, bmesh.types.BMEdge):
                continue
            a, b = e.verts
            if not all(any(group in wanted and weight > 0.3 for group, weight in v[layer].items()) for v in (a, b)):
                continue
            steps = max(1, int(e.calc_length() / 0.004))
            points += [a.co.lerp(b.co, i / steps) for i in range(steps + 1)]
        bm.free()
        return points

    def girth(self, z, bones=("hips", "thigh.L", "thigh.R", "spine", "chest")):
        """The body's half width, its front and its back at a height (the bones given only)."""
        points = self.outline(z, bones)
        return max(abs(q.x) for q in points), min(q.y for q in points), max(q.y for q in points)

    def section(self, z, n, bones=("hips", "thigh.L", "thigh.R", "spine")):
        """The body's outline at a height as n radii about its middle: the farthest point in each direction, smoothed;
        between the legs, where the outline is a hollow, a skirt hangs straight across."""
        points = self.outline(z, bones)
        cy =(min(q.y for q in points) + max(q.y for q in points)) / 2
        radii = [0.0] * n
        for q in points:
            angle = math.atan2(q.y - cy, q.x)
            index = round(angle / (2 * math.pi) * n) % n
            distance = math.hypot(q.x, q.y - cy)
            for step in (-2, -1, 0, 1, 2):
                radii[(index + step) % n] = max(radii[(index + step) % n], distance)
        for _ in range(4):
            radii = [(radii[(i - 1) % n] + 2 * radii[i] + radii[(i + 1) % n]) / 4 for i in range(n)]
        widest = max(radii)
        radii = [max(value, widest * 0.62) for value in radii]
        return cy, radii

    def skirt(self, name, top, hem, material, flare=0.030, pleats=11, depth=0.012, ease=0.014, rings=9, n=72, trim=None, band=0.030, scallop=None):
        """A skirt lofted around the body's own outline from `top` to `hem`, flaring, with pleats that deepen toward the
        hem; with `trim`, a band of that material over its last `band` metres. Bound to the hips above, giving way to each
        thigh by its side below. Its faces around at most the level's (the pleats still need about four faces each); its
        rings at the body's own heights (the body's skirt band, `rings` of them at most the level's, spaced evenly), those
        between its own top and hem: two skirts worn over each other then fold at the same heights, and the flat faces
        between their rings can't cross when a thigh swings (the manager, 2026-11-16, for the lean test)."""
        fine = self.p["detail"]["skirt"]
        n = min(n, max(fine["n"], pleats * 4))
        rings = min(rings, fine["rings"])
        bm = bmesh.new()
        loops = []
        lowest = self.joints["kneeL"].z + 0.03
        ref_top, ref_hem = self.skirt_band()
        pitch = (ref_top - ref_hem) / (rings - 1)
        heights = [top] + [ref_top - pitch * i for i in range(-8, 40) if hem + pitch * 0.35 < ref_top - pitch * i < top - pitch * 0.35] + [hem]
        rings = len(heights)
        for z in heights:
            t = (top - z) / (top - hem)
            cy, radii = self.section(max(z, lowest), n)
            ring = []
            for q in range(n):
                a = 2 * math.pi * q / n
                wave = (depth * t ** 1.2) * (math.sin(pleats * a + 0.6 * math.sin(3 * a)) * 0.75 + 0.25 * ripple(5 * a, 3 * t))
                radius = radii[q] + ease + flare * t ** 1.3 + wave
                rise = scallop[1] * (0.5 + 0.5 * math.sin(scallop[0] * a)) if scallop and z == hem else 0.0      # (a hem cut in waves)
                ring.append(bm.verts.new((radius * math.cos(a), cy + radius * math.sin(a), z + rise)))
            loops.append(ring)
        if trim is not None:
            # the hem's band: the skirt's last stretch again, a little outside it
            first = max(0, min(rings - 2, max(i for i, z in enumerate(heights) if z >= hem + band)))
            tb = bmesh.new()
            rows = []
            for ring in loops[first:]:
                rows.append([tb.verts.new(V((v.co.x, v.co.y, 0)).normalized() * 0.0030 + v.co) for v in ring])
            cut = hem + band
            for v in rows[0]:
                v.co.z = min(v.co.z, cut)
            for a_, b_ in zip(rows, rows[1:]):
                for q in range(n):
                    tb.faces.new((a_[q], a_[(q + 1) % n], b_[(q + 1) % n], b_[q]))
            bmesh.ops.recalc_face_normals(tb, faces=tb.faces[:])
            band_obj = k.new_object(f"{self.p['name']}-{name}-hem", tb, trim, smooth=True)
            k.solidify(band_obj, 0.004, offset=1.0)
            k.apply_modifiers([band_obj])
            band_obj.data.shade_smooth()
            self.skirt_weights(band_obj)
            self.pieces.append(band_obj)
        for a, b in zip(loops, loops[1:]):
            for q in range(n):
                bm.faces.new((a[q], a[(q + 1) % n], b[(q + 1) % n], b[q]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        obj = k.new_object(f"{self.p['name']}-{name}", bm, material, smooth=True)
        k.solidify(obj, 0.004, offset=-1.0)
        k.apply_modifiers([obj])
        obj.data.shade_smooth()
        self.skirt_weights(obj)
        self.pieces.append(obj)
        return obj

    def skirt_band(self):
        """The body's own skirt heights, every skirt's rings and weights are laid on: from the belt's line to the middle
        of the thigh (the tunic's skirt)."""
        return self.joints["waist"].z - 0.036, self.joints["hipL"].z - 0.165

    def skirt_weights(self, obj):
        """The hips at the top, giving way to the thighs, each by the side of the body. The heights are the body's own
        (from the belt's line to the middle of the thigh, the tunic's skirt), not each skirt's: a skirt worn over another
        (the mail's over the tunic's) then bends exactly as the one under it and nothing shows through in a lean (the
        manager, 2026-11-16)."""
        top, hem = self.skirt_band()
        groups = {name_: obj.vertex_groups.new(name=name_) for name_ in ("hips", "thigh.L", "thigh.R")}
        for v in obj.data.vertices:
            t = (top - v.co.z) / (top - hem)
            legs = min(0.85, max(0.0, (t - 0.12) / 0.5))
            left = 0.5 + 0.5 * max(-1.0, min(1.0, v.co.x / 0.06))
            groups["hips"].add([v.index], 1.0 - legs, "REPLACE")
            if legs * left > 0:
                groups["thigh.L"].add([v.index], legs * left, "REPLACE")
            if legs * (1 - left) > 0:
                groups["thigh.R"].add([v.index], legs * (1 - left), "REPLACE")
        modifier = obj.modifiers.new("armature", "ARMATURE")
        modifier.object = self.rig
        obj.parent = self.rig

    def buckle(self, z, material):
        """A round buckle on the front of the belt at height `z`, moving with the hips; smoothed once at the levels whose
        skirts are fine (the battle's stays as cut)."""
        _, front, _ = self.girth(z, bones=("hips", "spine"))
        centre = V((0, front - 0.027, z))
        obj = k.shaped_sphere(f"{self.p['name']}-buckle", lambda d: centre + V((d.x * 0.024, d.y * 0.006, d.z * 0.021)), cuts=2, material=material)
        if self.p["detail"]["skirt"]["n"] >= 48:
            k.subdivide(obj, 1)
            k.apply_modifiers([obj])
        r.bind_rigid(self.rig, obj, "hips")
        self.pieces.append(obj)


TRUNK = ("hips", "spine", "chest", "shoulder.L", "shoulder.R", "neck")
LEGS = ("thigh.L", "thigh.R", "shin.L", "shin.R", "hips")


def boots(mk, top_share=0.50):
    """Leather boots to the calf with a turned cuff and a flat sole."""
    tops = {s: mk.bone(f"shin.{s}") for s in ("L", "R")}

    def in_boot(c, bone):
        if bone.startswith("foot"):
            return True
        return bone.startswith("shin") and mk.along(bone, c) > top_share

    def sole(co, normal):
        return V((0, 0, -0.014 - co.z)) if co.z < 0.016 else V((0, 0, 0))

    planes = [(head.lerp(tail, top_share), (tail - head).normalized()) for head, tail in tops.values()]
    mk.shell("boots", in_boot, 0.007, mk.m["leather"], planes=planes, thickness=0.003, displace=sole)
    cuff = [(head.lerp(tail, top_share), (tail - head).normalized()) for head, tail in tops.values()] + \
           [(head.lerp(tail, top_share + 0.16), (tail - head).normalized()) for head, tail in tops.values()]
    mk.shell("boot-cuffs", lambda c, bone: bone.startswith("shin") and top_share < mk.along(bone, c) < top_share + 0.16, 0.015, mk.m["leather"],
             planes=cuff, thickness=0.005)


def trousers(mk, top=None, behind=False):
    """Trousers (hose) to the boots' tops, painted on the legs, creased at the knee (all round it, or with `behind` only
    behind it: breeches seen whole, where a crease in front draws a stray line). `top`: their top's height when not the
    hip's (a waistcoat's outfit has no skirt over them: they reach the waist)."""
    high = top is not None
    top = mk.joints["pelvis"].z + 0.035 if top is None else top
    knee = mk.joints["kneeL"].z

    def folds(co, normal):
        near = max(0.0, 1.0 - abs(co.z - (knee - 0.005)) / 0.075) ** 1.5
        return normal * (0.0030 * near * max(0.0, math.sin((co.z - knee) * 95)) ** 2 * (max(0.0, normal.y) if behind else 1.0))

    legs = LEGS + (("spine",) if high else ())
    mk.paint(lambda c, bone: bone in legs and c.z < top and not (bone.startswith("shin") and mk.along(bone, c) > 0.62),
             0.005, mk.m["trousers"], displace=folds, heights=((top,) if high else ()))


def top(mk, sleeve=0.72, forearm=0.0, cloth=None, trim=None, low=None):
    """The tunic's upper part: the trunk and short sleeves as one painted piece, gathered over the belt, with a collar and
    hemmed sleeves (to `sleeve` of the upper arm, or with `forearm` to that share of the forearm: long sleeves). Returns the
    belt's height. `cloth`, `trim`: its materials when not the slots `cloth` and `cloth.trim` (a shirt under a waistcoat); `low`: its lower edge's
    height, cut exactly, when no skirt hangs over it (a shirt that ends where the trousers begin)."""
    cloth = cloth or mk.m["cloth"]
    trim = trim or mk.m["cloth.trim"]
    j = mk.joints
    edge = () if low is None else (low,)
    low = j["pelvis"].z + 0.010 if low is None else low
    high = j["neck0"].z + 0.006
    belt_at = j["waist"].z - 0.030
    arms = {s: mk.bone(f"{'forearm' if forearm else 'upper_arm'}.{s}") for s in ("L", "R")}
    last = "forearm" if forearm else "upper_arm"
    if forearm:
        sleeve = forearm

    def keep(c, bone):
        if bone.startswith(last):
            return mk.along(bone, c) < sleeve
        if forearm and bone.startswith("upper_arm"):
            return True
        return bone in TRUNK and low < c.z < high

    def folds(co, normal):
        above = max(0.0, 1.0 - abs(co.z - (belt_at + 0.050)) / 0.070) ** 1.5  # cloth gathered over the belt, in broad pleats
        hang = max(0.0, min(1.0, (co.z - belt_at - 0.08) / 0.25))             # looser over the chest and back
        angle = math.atan2(co.x, co.y)
        pleat = max(0.0, math.sin(angle * 7 + 0.5 * math.sin(angle * 2))) ** 2
        return normal * (0.0050 * above * pleat + 0.002 * hang)

    sleeve_ends = [(head.lerp(tail, sleeve), (tail - head).normalized()) for head, tail in arms.values()]
    mk.paint(keep, 0.006, cloth, displace=folds, planes=sleeve_ends, heights=(high,) + edge)
    reach = COLLAR_REACH * j["shoulderL"].x
    mk.shell("collar", lambda c, bone: bone in TRUNK and high - 0.022 < c.z < high + 0.004 and abs(c.x) < reach, 0.005, trim,
             heights=(high + 0.004, high - 0.022), thickness=0.003, taut=COLLAR_TAUT)
    cuffs = [(head.lerp(tail, sleeve + 0.02), (tail - head).normalized()) for head, tail in arms.values()] + \
            [(head.lerp(tail, sleeve - 0.06), (tail - head).normalized()) for head, tail in arms.values()]
    mk.shell("sleeve-hems", lambda c, bone: bone.startswith(last) and sleeve - 0.06 < mk.along(bone, c) < sleeve + 0.02, 0.004, trim,
             planes=cuffs, thickness=0.003)
    return belt_at


def belt(mk, z, half=0.023):
    """A leather belt at height `z` with its buckle."""
    mk.shell("belt", lambda c, bone: z - half < c.z < z + half and not bone.startswith(("upper_arm", "forearm", "hand", "fingers", "thumb")),
             0.022, mk.m["leather"], heights=(z - half, z + half), thickness=0.004)
    mk.buckle(z, mk.m["buckle"])


def tunic(mk):
    """The plain outfit of the human man and woman: a belted tunic to the middle of the thigh, its skirt hemmed, over
    trousers, and boots."""
    trousers(mk)
    boots(mk)
    at = top(mk)
    hem = mk.joints["hipL"].z - 0.165
    mk.skirt("tunic-skirt", at - 0.006, hem, mk.m["cloth"], flare=0.026, pleats=9, depth=0.010, trim=mk.m["cloth.trim"])
    belt(mk, at)
    return mk.pieces


def elven(mk):
    """The plain outfit of the elves: a long-sleeved tunic to the knee, close and little gathered, a narrow belt, over
    trousers and tall boots."""
    trousers(mk)
    boots(mk, top_share=0.26)
    at = top(mk, forearm=0.86)
    hem = mk.joints["kneeL"].z + 0.060
    mk.skirt("tunic-skirt", at - 0.006, hem, mk.m["cloth"], flare=0.016, pleats=5, depth=0.006, trim=mk.m["cloth.trim"])
    belt(mk, at, half=0.014)
    return mk.pieces


def dark_elven(mk):
    """The plain outfit of the dark elves: a sleeveless tunic to the middle of the thigh under a broad belt, over trousers
    and tall boots."""
    trousers(mk)
    boots(mk, top_share=0.26)
    at = top(mk, sleeve=0.10)
    hem = mk.joints["hipL"].z - 0.150
    mk.skirt("tunic-skirt", at - 0.006, hem, mk.m["cloth"], flare=0.020, pleats=7, depth=0.008, trim=mk.m["cloth.trim"])
    belt(mk, at, half=0.036)
    return mk.pieces


def dwarven(mk):
    """The plain outfit of the dwarves: a short-sleeved tunic to the hip under a broad belt, trousers and heavy boots."""
    trousers(mk)
    boots(mk, top_share=0.40)
    at = top(mk, sleeve=0.55)
    hem = mk.joints["hipL"].z - 0.105
    mk.skirt("tunic-skirt", at - 0.006, hem, mk.m["cloth"], flare=0.030, pleats=7, depth=0.010, trim=mk.m["cloth.trim"])
    belt(mk, at, half=0.034)
    return mk.pieces


def bare_feet(mk, tufts=((-0.50, 0.22, -32), (0.00, 0.20, 3), (0.50, 0.22, 32), (-0.72, 0.34, -40), (-0.25, 0.33, -14), (0.25, 0.33, 15), (0.72, 0.34, 40),
                         (-0.50, 0.48, -26), (0.00, 0.46, 2), (0.50, 0.48, 27), (-0.72, 0.64, -36), (-0.25, 0.63, -12), (0.25, 0.63, 13), (0.72, 0.64, 36),
                         (-0.48, 0.78, -22), (0.02, 0.80, 2), (0.50, 0.78, 23))):
    """Bare feet (the hobbits'): the toes drawn as four dark lines over the foot's front (flat strips on the skin in the slot
    `brows`, without ink, as the mouth's line is), and tufts of hair on the foot's top (short curled locks in the slot
    `hair`; `tufts`: each (across the foot from the inner side -1 to the outer 1, along it from the ankle 0 to the toes 1,
    its turn outward in degrees)). Each foot's pieces move with its foot bone."""
    tree = BVHTree.FromObject(mk.body, bpy.context.evaluated_depsgraph_get())
    for tag, side in (("L", 1), ("R", -1)):
        (_, hx, hy, hz, hr), (_, bx, by, bz, br), (_, tx, ty, tz, tr) = mk.p["foot"]
        ankle = mk.joints["ankle" + tag]
        half = tr * 1.5
        # the toes: four clefts between five toes, the big toe on the inner side
        bm = bmesh.new()
        for across, reach in ((-0.50, 1.00), (-0.08, 0.92), (0.30, 0.80), (0.62, 0.64)):
            x = side * (tx + across * half)
            origin = V((x, ty + tr * 0.55, tz))
            pairs = []
            steps = 9
            for q in range(steps + 1):
                t = q / steps
                angle = math.radians(-16.0 * reach + (140.0 + 16.0 * reach) * t)        # from the toe's top down its front
                d = V((0, -math.sin(angle), math.cos(angle)))
                hit = tree.ray_cast(origin, d)[0]
                if hit is None:
                    continue
                w = 0.0019 * (0.35 + 0.65 * math.sin(math.pi * min(1.0, 0.12 + t)) ** 0.7)
                at = hit + d * 0.0012
                pairs.append((bm.verts.new(at + V((w, 0, 0))), bm.verts.new(at - V((w, 0, 0)))))
            for (a0, a1), (b0, b1) in zip(pairs, pairs[1:]):
                bm.faces.new((a0, b0, b1, a1))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        lines = k.new_object(f"{mk.p['name']}-toes-{tag}", bm, mk.m["brows"], smooth=True)
        lines["ink"] = 0
        r.bind_rigid(mk.rig, lines, f"foot.{tag}")
        mk.pieces.append(lines)
        # the tufts
        locks = []
        for n, (across, along, turn) in enumerate(tufts):
            y = (ankle.y - 0.035) + ((ty + tr * 0.2) - (ankle.y - 0.035)) * along
            width = hr * 1.2 + (half - hr * 1.2) * along
            x = side * (tx + (hx - tx) * (1 - along) + across * width * 0.80)
            hit, normal, _, _ = tree.ray_cast(V((x, y, ankle.z + 0.05)), V((0, 0, -1)))      # (from just over the instep: from higher it meets the knee)
            if hit is None or normal.z < 0.3:
                continue
            angle = math.radians(turn)
            forward = V((side * math.sin(angle), -math.cos(angle), 0))
            lean = (forward - normal * forward.dot(normal)).normalized()
            size = 1.10 + 0.3 * ((n * 7) % 5) / 4
            path = [hit - normal * 0.004, hit + normal * 0.0035 * size + lean * 0.004 * size, hit + normal * 0.0058 * size + lean * 0.012 * size,
                    hit + normal * 0.0052 * size + lean * 0.020 * size, hit + normal * 0.0026 * size + lean * 0.026 * size,
                    hit + normal * 0.0002 + lean * 0.030 * size]
            across_lock = lean.cross(normal).normalized()
            radii = [(0.0058 * size, 0.0036 * size), (0.0074 * size, 0.0040 * size), (0.0076 * size, 0.0038 * size), (0.0060 * size, 0.0032 * size),
                     (0.0036 * size, 0.0022 * size), (0.0008, 0.0008)]
            locks.append(k.tube(f"{mk.p['name']}-tuft-{tag}-{n}", path, radii, side=across_lock, n=8, material=mk.m["hair"]))
        if mk.p["detail"]["skirt"]["n"] >= 48:
            for lock in locks:
                k.subdivide(lock, 1)
            k.apply_modifiers(locks)
        for lock in locks:
            r.bind_rigid(mk.rig, lock, f"foot.{tag}")
            mk.pieces.append(lock)


def waistcoat(mk, buttons=3):
    """A waistcoat over the shirt: the trunk's faces from the shoulders to under the waist as a shell, sleeveless, open in
    a V from the middle of the chest to the neck, buttoned below it (the buttons in the slot `buckle`). Returns its hem's
    height."""
    j = mk.joints
    hem = j["pelvis"].z + 0.022
    high = j["neck0"].z - 0.012
    point = j["chest"].z - 0.035                       # the V's lowest point
    spread = 0.060 / (high - point)                    # its half width per metre of height
    reach = 0.93 * j["shoulderL"].x
    armpit = j["shoulderL"].z - 0.11                   # (the armholes' cut holds above it; below, the hips may be wider)
    slant = V((1.0, 0.0, -spread)).normalized()        # across the V's left edge (the right one mirrored)

    def keep(c, bone):
        arm = bone.startswith("upper_arm")              # (the shoulder's cap inside the armhole only; the hips may be wider than it)
        if not (bone in TRUNK or bone.startswith("thigh") or arm) or not hem < c.z < high or (abs(c.x) > reach and (arm or c.z > armpit)):
            return False
        return not (c.y < 0 and c.z > point and abs(c.x) < (c.z - point) * spread)

    planes = []
    for share in (0.25, 0.75):                         # (a cut reaches 0.10 m from its point: two along each edge of the V)
        z = point + (high - point) * share
        _, front, _ = mk.girth(z, bones=("spine", "chest"))
        planes += [(V(((z - point) * spread, front, z)), slant), (V((-(z - point) * spread, front, z)), V((-slant.x, 0, slant.z)))]
    planes += [(V((side * reach, j["shoulderL"].y, j["shoulderL"].z)), V((1, 0, 0))) for side in (1, -1)]
    mk.shell("waistcoat", keep, 0.009, mk.m["cloth"], planes=planes, heights=(hem, high), thickness=0.004)
    for n in range(buttons):
        z = hem + 0.030 + (point - 0.030 - hem - 0.030) * n / max(1, buttons - 1)
        _, front, _ = mk.girth(z, bones=("hips", "spine", "chest"))
        centre = V((0, front - 0.016, z))
        obj = k.shaped_sphere(f"{mk.p['name']}-button-{n}", lambda d, centre=centre: centre + V((d.x * 0.0095, d.y * 0.0045, d.z * 0.0095)), cuts=2,
                              material=mk.m["buckle"])
        if mk.p["detail"]["skirt"]["n"] >= 48:
            k.subdivide(obj, 1)
            k.apply_modifiers([obj])
        r.bind_rigid(mk.rig, obj, "spine" if z > j["waist"].z - 0.03 else "hips")
        mk.pieces.append(obj)
    return hem


def hobbit(mk):
    """The plain outfit of the hobbits (the old looks' "vest"): a waistcoat over a long-sleeved shirt (the slot `shirt`),
    breeches that end at the calf with a cuff, and bare feet tufted with hair (bare_feet)."""
    top(mk, forearm=0.84, cloth=mk.m["shirt"], trim=mk.m["shirt"], low=mk.joints["pelvis"].z + 0.035)   # (to the trousers' top, under the waistcoat's hem)
    trousers(mk, behind=True)
    shins = {s: mk.bone(f"shin.{s}") for s in ("L", "R")}
    cuff = [(head.lerp(tail, 0.56), (tail - head).normalized()) for head, tail in shins.values()] + \
           [(head.lerp(tail, 0.64), (tail - head).normalized()) for head, tail in shins.values()]
    mk.shell("breeches-cuffs", lambda c, bone: bone.startswith("shin") and 0.56 < mk.along(bone, c) < 0.64, 0.004, mk.m["trousers"], planes=cuff, thickness=0.004)
    waistcoat(mk)
    bare_feet(mk)
    return mk.pieces


def claws(mk, length=0.024, girth=0.0052):
    """Claws on the hands (the Half-Beasts' and the lizardmen's, "modelled on the block"): four at the end of the fingers'
    block and one on the thumb, curved toward the palm, in the slot `claws`; each moves with its fingers' or thumb's bone."""
    hand = mk.p["hand"]
    L, W, T = hand["length"], hand["width"], hand["thick"]
    for tag, side in (("L", 1), ("R", -1)):
        el, wr = mk.joints["elbow" + tag], mk.joints["wrist" + tag]
        along = (wr - el).normalized()
        front = (V((0, -1, 0)) - along * V((0, -1, 0)).dot(along)).normalized()
        flat = along.cross(front).normalized() * side
        end = wr + along * (0.99 * L) + flat * (T * 1.30)
        places = [(end + front * (W * across), along, "fingers") for across in (-0.36, -0.12, 0.12, 0.36)]
        root = wr + along * (0.16 * L)
        tip = root + front * (W * 1.36) + along * (0.36 * L) + flat * (T * 0.9)
        before = root + front * (W * 1.16) + along * (0.15 * L) + flat * (T * 0.3)
        places.append((tip, (tip - before).normalized(), "thumb"))
        for n, (at, way, bone) in enumerate(places):
            path = [at - way * 0.006, at + way * (length * 0.45) + flat * (length * 0.06), at + way * (length * 0.85) + flat * (length * 0.24),
                    at + way * length + flat * (length * 0.42)]
            claw = k.tube(f"{mk.p['name']}-claw-{tag}-{n}", path, [girth, girth * 0.82, girth * 0.48, 0.0006], side=front, n=6, material=mk.m["claws"])
            r.bind_rigid(mk.rig, claw, f"{bone}.{tag}")
            mk.pieces.append(claw)


def foot_claws(mk, length=0.032, girth=0.0080):
    """Claws on bare feet (the lizardmen's): three at the front of each foot, curved down to the ground, in the slot
    `claws`; each foot's move with its foot bone."""
    (_, hx, hy, hz, hr), (_, bx, by, bz, br), (_, tx, ty, tz, tr) = mk.p["foot"]
    for tag, side in (("L", 1), ("R", -1)):
        for n, across in enumerate((-0.62, 0.0, 0.62)):
            at = V((side * (tx + across * tr * 1.25), ty - tr * 0.55 * (1.0 - 0.3 * abs(across)), tz * 0.78))
            path = [at + V((0, 0.012, 0.004)), at + V((0, -length * 0.5, 0.004)), at + V((0, -length * 0.9, -tz * 0.34)), at + V((0, -length, -tz * 0.72))]
            claw = k.tube(f"{mk.p['name']}-footclaw-{tag}-{n}", path, [girth, girth * 0.85, girth * 0.5, 0.0008], side=V((1, 0, 0)), n=6, material=mk.m["claws"])
            r.bind_rigid(mk.rig, claw, f"foot.{tag}")
            mk.pieces.append(claw)


def belly(mk):
    """The paler plates down a lizardman's front (the slot `skin.belly`): the throat, the chest and the belly to under the
    wrap's belt, the body's own faces in another colour: broadest over the chest, narrower at the waist and up the throat,
    their edges cut straight from knot to knot."""
    j = mk.joints
    low, high = j["pelvis"].z + 0.030, j["neck1"].z + 0.060
    knots = [(low, 0.058), (j["waist"].z, 0.064), ((j["waist"].z + j["chest"].z) / 2, 0.078), (j["chest"].z, 0.088), (j["clavicle"].z, 0.074),
             (j["neck0"].z, 0.056), (high, 0.046)]

    def half(z):
        for (z0, h0), (z1, h1) in zip(knots, knots[1:]):
            if z <= z1:
                return h0 + (h1 - h0) * max(0.0, (z - z0) / (z1 - z0))
        return knots[-1][1]

    cuts = []
    for (z0, h0), (z1, h1) in zip(knots, knots[1:]):
        z = (z0 + z1) / 2
        _, front, _ = mk.girth(min(z, j["clavicle"].z), bones=("hips", "spine", "chest"))
        for side in (1, -1):
            cuts.append((V((side * (h0 + h1) / 2, front, z)), V((side * (z1 - z0), 0, -(h1 - h0))).normalized()))
    mk.paint(lambda c, bone: (bone in TRUNK or bone == "head") and low < c.z < high and abs(c.x) < half(c.z) and c.y < -0.015, 0.0, mk.m["skin.belly"],
             planes=cuts, heights=(low,))


def lizardman(mk):
    """The plain outfit of the lizardmen (the old looks' loincloth): a short wrap on a belt with a long flap hanging in
    front (the slot `cloth.trim`), a band on each upper arm; the chest, the legs and the feet bare, the paler plates down
    the front (belly; the user's Q75); claws on the hands and the feet. No flap behind: the tail is there."""
    j = mk.joints
    belly(mk)
    at = j["pelvis"].z + 0.062
    hem = j["hipL"].z - 0.105
    mk.skirt("loincloth", at - 0.004, hem, mk.m["cloth"], flare=0.020, pleats=5, depth=0.006, trim=mk.m["cloth.trim"], band=0.022)
    belt(mk, at, half=0.020)
    _, front, _ = mk.girth(at, bones=("hips", "spine"))
    top_ = V((0, front - 0.021, at - 0.012))
    length, width = 0.36, 0.060
    path = [top_, top_ + V((0, -0.014, -length * 0.4)), top_ + V((0, -0.022, -length * 0.8)), top_ + V((0, -0.024, -length))]
    flap = k.tube(f"{mk.p['name']}-flap", path, [(width, 0.0040), (width * 1.05, 0.0040), (width * 1.10, 0.0035), (width * 0.85, 0.0030)], side=V((1, 0, 0)), n=8,
                  material=mk.m["cloth.trim"])
    r.bind_rigid(mk.rig, flap, "hips")
    mk.pieces.append(flap)
    arms = {s: mk.bone(f"upper_arm.{s}") for s in ("L", "R")}
    ends = [(head.lerp(tail, 0.46), (tail - head).normalized()) for head, tail in arms.values()] + \
           [(head.lerp(tail, 0.60), (tail - head).normalized()) for head, tail in arms.values()]
    mk.shell("armbands", lambda c, bone: bone.startswith("upper_arm") and 0.46 < mk.along(bone, c) < 0.60, 0.004, mk.m["cloth.trim"], planes=ends, thickness=0.004)
    claws(mk)
    foot_claws(mk)
    return mk.pieces


def half_beast(mk):
    """The plain outfit of the Half-Beasts: a short-sleeved tunic to the middle of the thigh, its hem ragged, belted, over
    trousers and boots; claws on the hands."""
    trousers(mk)
    boots(mk)
    at = top(mk, sleeve=0.50)
    hem = mk.joints["hipL"].z - 0.150
    mk.skirt("tunic-skirt", at - 0.006, hem, mk.m["cloth"], flare=0.028, pleats=8, depth=0.010, trim=mk.m["cloth.trim"], scallop=(13, 0.013))
    belt(mk, at, half=0.026)
    claws(mk)
    return mk.pieces


def sash(mk, z, half=0.036):
    """A broad cloth sash at height `z` (the slot `cloth.trim`), knotted at the left hip, its two ends hanging there."""
    mk.shell("sash", lambda c, bone: z - half < c.z < z + half and not bone.startswith(("upper_arm", "forearm", "hand", "fingers", "thumb")),
             0.021, mk.m["cloth.trim"], heights=(z - half, z + half), thickness=0.004)
    points = mk.outline(z, ("hips", "spine"))
    at = max(points, key=lambda q: q.x * 0.80 - q.y * 0.60)           # the outline's point to the left and the front
    middle = sum((V((q.x, q.y, 0)) for q in points), V((0, 0, 0))) / len(points)
    out = V((at.x - middle.x, at.y - middle.y, 0)).normalized()
    round_ = V((-out.y, out.x, 0))
    knot = V((at.x, at.y, z)) + out * 0.030
    pieces = [k.shaped_sphere(f"{mk.p['name']}-sash-knot", lambda d: knot + out * (d.x * 0.014) + round_ * (d.y * 0.020) + V((0, 0, d.z * 0.022)), cuts=2,
                              material=mk.m["cloth.trim"])]
    for n, (aside, length, lean) in enumerate(((-0.010, 0.215, 0.030), (0.016, 0.160, 0.018))):
        start = knot + round_ * aside - V((0, 0, 0.012))
        path = [start, start + out * (lean * 0.5) + V((0, 0, -length * 0.45)), start + out * lean + round_ * (aside * 0.8) + V((0, 0, -length))]
        pieces.append(k.tube(f"{mk.p['name']}-sash-end{n}", path, [(0.017, 0.0035), (0.019, 0.0035), (0.022, 0.003)], side=round_, n=8, material=mk.m["cloth.trim"]))
    for obj in pieces:
        r.bind_rigid(mk.rig, obj, "hips")
        mk.pieces.append(obj)


def merfolk(mk):
    """The plain outfit of the Merfolk: a sleeveless wrap to above the knee, its hem cut in waves, under a broad cloth sash
    whose ends hang at the hip; trousers and low boots."""
    trousers(mk)
    boots(mk, top_share=0.58)
    at = top(mk, sleeve=0.10)
    hem = mk.joints["kneeL"].z + 0.085
    mk.skirt("tunic-skirt", at - 0.006, hem, mk.m["cloth"], flare=0.022, pleats=6, depth=0.007, trim=mk.m["cloth.trim"], scallop=(8, 0.016))
    sash(mk, at)
    return mk.pieces


def goliath(mk):
    """The plain outfit of the Goliaths: a sleeveless tunic to the middle of the thigh under a broad belt, leather bracers
    on the forearms, trousers and boots."""
    trousers(mk)
    boots(mk, top_share=0.46)
    at = top(mk, sleeve=0.08)
    hem = mk.joints["hipL"].z - 0.190
    mk.skirt("tunic-skirt", at - 0.006, hem, mk.m["cloth"], flare=0.030, pleats=7, depth=0.010, trim=mk.m["cloth.trim"])
    belt(mk, at, half=0.040)
    arms = {s: mk.bone(f"forearm.{s}") for s in ("L", "R")}
    ends = [(head.lerp(tail, 0.34), (tail - head).normalized()) for head, tail in arms.values()] + \
           [(head.lerp(tail, 0.90), (tail - head).normalized()) for head, tail in arms.values()]
    mk.shell("bracers", lambda c, bone: bone.startswith("forearm") and 0.34 < mk.along(bone, c) < 0.90, 0.006, mk.m["leather"], planes=ends, thickness=0.005)
    return mk.pieces


# The plain outfits by the name a body's parameters give ("outfit").
OUTFITS = {"tunic": tunic, "elven": elven, "dark-elven": dark_elven, "dwarven": dwarven, "hobbit": hobbit, "goliath": goliath, "merfolk": merfolk, "half-beast": half_beast, "lizardman": lizardman}

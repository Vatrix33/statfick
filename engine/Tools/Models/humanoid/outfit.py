# The plain outfit (Q18: one plain outfit per race and sex, its colours the player's), made on a bound body (GM2 step 4).
# A tight garment is the body's own surface: the faces the bones that move them choose take the cloth's material and
# move out off the skin (paint), so nothing can show through it and it bends exactly as the body does; a body's model
# carries its plain outfit. Loose pieces (a collar, hems, boots, a belt) are shells cut from the body's faces and lifted,
# bound by the weights of the skin under them; a skirt is lofted around the body's measured outline. Folds are modelled
# (Q3). The human man's and woman's: the belted tunic over trousers (hose) with boots (the user's choice, 2026-11-16).
import math

import bmesh
from mathutils import Vector

import kit as k
import lighten
import rig as r

V = Vector
# A collar's band is relaxed twice: it lies where the neck runs into the shoulders, and large faces fold sharply there.
COLLAR_TAUT = 2


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

    def skirt(self, name, top, hem, material, flare=0.030, pleats=11, depth=0.012, ease=0.014, rings=9, n=72, trim=None, band=0.030):
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
                ring.append(bm.verts.new((radius * math.cos(a), cy + radius * math.sin(a), z)))
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


def trousers(mk):
    """Trousers (hose) to the boots' tops, painted on the legs, creased behind the knee."""
    top = mk.joints["pelvis"].z + 0.035
    knee = mk.joints["kneeL"].z

    def folds(co, normal):
        near = max(0.0, 1.0 - abs(co.z - (knee - 0.005)) / 0.075) ** 1.5
        return normal * (0.0030 * near * max(0.0, math.sin((co.z - knee) * 95)) ** 2)

    mk.paint(lambda c, bone: bone in LEGS and c.z < top and not (bone.startswith("shin") and mk.along(bone, c) > 0.62),
             0.005, mk.m["trousers"], displace=folds)


def top(mk, sleeve=0.72):
    """The tunic's upper part: the trunk and short sleeves as one painted piece, gathered over the belt, with a collar and
    hemmed sleeves. Returns the belt's height."""
    j = mk.joints
    low = j["pelvis"].z + 0.010
    high = j["neck0"].z + 0.006
    belt_at = j["waist"].z - 0.030
    arms = {s: mk.bone(f"upper_arm.{s}") for s in ("L", "R")}

    def keep(c, bone):
        if bone.startswith("upper_arm"):
            return mk.along(bone, c) < sleeve
        return bone in TRUNK and low < c.z < high

    def folds(co, normal):
        above = max(0.0, 1.0 - abs(co.z - (belt_at + 0.050)) / 0.070) ** 1.5  # cloth gathered over the belt, in broad pleats
        hang = max(0.0, min(1.0, (co.z - belt_at - 0.08) / 0.25))             # looser over the chest and back
        angle = math.atan2(co.x, co.y)
        pleat = max(0.0, math.sin(angle * 7 + 0.5 * math.sin(angle * 2))) ** 2
        return normal * (0.0050 * above * pleat + 0.002 * hang)

    sleeve_ends = [(head.lerp(tail, sleeve), (tail - head).normalized()) for head, tail in arms.values()]
    mk.paint(keep, 0.006, mk.m["cloth"], displace=folds, planes=sleeve_ends, heights=(high,))
    mk.shell("collar", lambda c, bone: bone in TRUNK and high - 0.022 < c.z < high + 0.004, 0.005, mk.m["cloth.trim"],
             heights=(high + 0.004, high - 0.022), thickness=0.003, taut=COLLAR_TAUT)
    cuffs = [(head.lerp(tail, sleeve + 0.02), (tail - head).normalized()) for head, tail in arms.values()] + \
            [(head.lerp(tail, sleeve - 0.06), (tail - head).normalized()) for head, tail in arms.values()]
    mk.shell("sleeve-hems", lambda c, bone: bone.startswith("upper_arm") and sleeve - 0.06 < mk.along(bone, c) < sleeve + 0.02, 0.004, mk.m["cloth.trim"],
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


# The plain outfits by the name a body's parameters give ("outfit").
OUTFITS = {"tunic": tunic}

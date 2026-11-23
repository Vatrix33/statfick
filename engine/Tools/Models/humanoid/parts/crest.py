# The crests (Q13, Q19: the lizardmen's crests, the Merfolk's fin crest; each list with "none"): rigid pieces on the head's
# top and back, bound to the skeleton's `head` bone, a part worn as a garment (GM2-P8). Ribs and spines take the skin's
# colour, a fin's membrane the slot `crest`. Placed by the head's own shape (parts.headpiece.Head), so one definition fits
# every head of a race; made per sex on the average body with the first face (GM2-P25).
import math

import bmesh
from mathutils import Vector

import bodies
import forms
import kit as k
import materials
import rig as r
from parts.face import with_face
from parts.headpiece import Head

V = Vector


def fin(head, m, name, ribs, yaw=0.0, lean=0.0, web=True, thick=0.0034, sink=0.006, rib=None, girth=None, flat=1.0):
    """A fin standing on the head along a line from the brow over the crown to the nape: `ribs` each (the place along the
    line from 0 at the front to 1 at the back, its height in metres, its slant backward in degrees); the line at `yaw`
    degrees off the middle, the fin leaning out by `lean` degrees. Ribs in the skin's colour; with `web`, a membrane
    between them (the slot `crest`), its edge dipping between two ribs. `rib`, `girth`, `flat`: the ribs' material, their
    thickness and how flat they are (feathers: broad flat blades in the slot `crest`)."""
    def base(t):
        # the line: from pitch 52 at the front over the crown to pitch 18 at the back
        angle = 52.0 + (180.0 - 52.0 - 18.0) * t
        pitch, side = (angle, 0.0) if angle <= 90.0 else (180.0 - angle, 180.0)
        d = k.direction(side + (yaw if side == 0.0 else -yaw), pitch)
        return head.centre + (head.shape(d) - head.centre) * 1.0, d

    def tip(t, height, slant):
        foot, d = base(t)
        back = V((0, 1, 0))
        back = (back - d * back.dot(d)).normalized()
        out = (d * math.cos(math.radians(slant)) + back * math.sin(math.radians(slant))).normalized()
        if lean:
            across = d.cross(back).normalized() * (1 if yaw >= 0 else -1)
            out = (out * math.cos(math.radians(lean)) + across * math.sin(math.radians(lean))).normalized()
        return foot - d * sink, foot + out * height

    pieces = []
    feet, tips = zip(*[tip(t, height, slant) for t, height, slant in ribs])
    if web:
        bm = bmesh.new()
        lower, upper = [], []
        for i in range(len(ribs)):
            lower.append(bm.verts.new(feet[i]))
            upper.append(bm.verts.new(tips[i]))
            if i + 1 < len(ribs):
                (t0, h0, s0), (t1, h1, s1) = ribs[i], ribs[i + 1]
                for share in (0.33, 0.67):
                    dip = 1.0 - 0.20 * math.sin(math.pi * share)
                    a, b = tip(t0 + (t1 - t0) * share, (h0 + (h1 - h0) * share) * dip, s0 + (s1 - s0) * share)
                    lower.append(bm.verts.new(a))
                    upper.append(bm.verts.new(b))
        for i in range(len(lower) - 1):
            bm.faces.new((lower[i], lower[i + 1], upper[i + 1], upper[i]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        membrane = k.new_object(f"{head.name}-{name}-web", bm, m["crest"], smooth=False)
        k.solidify(membrane, thick, offset=0.0)
        pieces.append(membrane)
    side = V((1, 0, 0))
    for i, (foot, top) in enumerate(zip(feet, tips)):
        g = girth or (0.0046 if web else 0.0062)
        pieces.append(k.tube(f"{head.name}-{name}-rib{i}", [foot, foot.lerp(top, 0.35), foot.lerp(top, 0.72), top],
                             [(g * 0.7, g * 0.7 * flat), (g, g * flat), (g * 0.85, g * 0.85 * flat), (0.0014, 0.0014 * flat)] if girth else [g, g * 0.86, g * 0.7, 0.0014],
                             side=side, n=6, material=rib or m["skin"]))
    return pieces


def tall_fin(head, m):
    """A tall fin from the brow to the nape, highest over the crown."""
    return fin(head, m, "fin", [(0.02, 0.030, 20), (0.20, 0.062, 26), (0.40, 0.082, 32), (0.60, 0.078, 40), (0.80, 0.056, 50), (0.98, 0.030, 60)])


def low_fin(head, m):
    """A low, long fin swept back, from the crown down the back of the head."""
    return fin(head, m, "fin", [(0.22, 0.022, 40), (0.40, 0.040, 48), (0.58, 0.048, 54), (0.76, 0.044, 60), (0.96, 0.028, 66)])


def twin_fins(head, m):
    """Two fins side by side, leaning apart."""
    ribs = [(0.10, 0.026, 22), (0.28, 0.052, 28), (0.48, 0.064, 36), (0.68, 0.052, 46), (0.88, 0.030, 56)]
    return fin(head, m, "fin-l", ribs, yaw=24.0, lean=-14.0) + fin(head, m, "fin-r", ribs, yaw=-24.0, lean=-14.0)


def spines(head, m):
    """A row of bare spines from the brow to the nape."""
    return fin(head, m, "spines", [(0.04, 0.030, 18), (0.18, 0.046, 24), (0.33, 0.058, 30), (0.48, 0.062, 36), (0.63, 0.056, 44), (0.78, 0.044, 52), (0.93, 0.030, 60)],
               web=False)


def feathers(head, m):
    """A plume of long feathers swept back over the head (the slot `crest`)."""
    middle = [(0.16, 0.090, 52), (0.30, 0.125, 58), (0.44, 0.140, 64), (0.58, 0.125, 70), (0.72, 0.095, 76)]
    sides = [(0.24, 0.085, 56), (0.40, 0.110, 62), (0.56, 0.100, 70)]
    return fin(head, m, "plume", middle, web=False, rib=m["crest"], girth=0.015, flat=0.22) + \
        fin(head, m, "plume-l", sides, yaw=20.0, lean=-22.0, web=False, rib=m["crest"], girth=0.013, flat=0.22) + \
        fin(head, m, "plume-r", sides, yaw=-20.0, lean=-22.0, web=False, rib=m["crest"], girth=0.013, flat=0.22)


def frill(head, m):
    """A frill standing behind the head and framing it from side to side over the top: ribs in the skin's colour, a
    membrane between them (the slot `crest`)."""
    angles = [-28.0, 4.0, 34.0, 62.0, 90.0, 118.0, 146.0, 176.0, 208.0]          # round the head seen from the front: 0 its left, 90 up
    lengths = [0.060, 0.086, 0.100, 0.108, 0.112, 0.108, 0.100, 0.086, 0.060]

    def rib(angle, length):
        c, s = math.cos(math.radians(angle)), math.sin(math.radians(angle))
        d = V((c, 0.50, s)).normalized()
        foot = head.centre + (head.shape(d) - head.centre) * 0.95
        return foot, foot + V((c, 0.30, s)).normalized() * length

    bm = bmesh.new()
    lower, upper, ends = [], [], []
    for i, (angle, length) in enumerate(zip(angles, lengths)):
        foot, top = rib(angle, length)
        ends.append((foot, top))
        lower.append(bm.verts.new(foot))
        upper.append(bm.verts.new(top))
        if i + 1 < len(angles):
            for share in (0.33, 0.67):
                between = rib(angle + (angles[i + 1] - angle) * share, (length + (lengths[i + 1] - length) * share) * (1.0 - 0.18 * math.sin(math.pi * share)))
                lower.append(bm.verts.new(between[0]))
                upper.append(bm.verts.new(between[1]))
    for i in range(len(lower) - 1):
        bm.faces.new((lower[i], lower[i + 1], upper[i + 1], upper[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    membrane = k.new_object(f"{head.name}-frill-web", bm, m["crest"], smooth=False)
    k.solidify(membrane, 0.0034, offset=0.0)
    pieces = [membrane]
    for i, (foot, top) in enumerate(ends):
        pieces.append(k.tube(f"{head.name}-frill-rib{i}", [foot, foot.lerp(top, 0.5), top], [0.0052, 0.0042, 0.0014], side=V((0, 1, 0)), n=6, material=m["skin"]))
    return pieces


def horns(head, m):
    """Two horns from over the brow, swept back and up (the slot `claws`)."""
    pieces = []
    for side in (1, -1):
        foot = head.on(side * 40.0, 44.0, 0.96)
        out = k.direction(side * 40.0, 44.0)
        back, up = V((0, 1, 0)), V((0, 0, 1))
        path = [foot, foot + out * 0.030 + back * 0.012, foot + out * 0.048 + back * 0.050 + up * 0.016, foot + out * 0.050 + back * 0.100 + up * 0.046,
                foot + out * 0.040 + back * 0.140 + up * 0.090]
        pieces.append(k.tube(f"{head.name}-horn{side}", path, [0.015, 0.014, 0.011, 0.007, 0.0012], side=V((1, 0, 0)), n=8, material=m["claws"]))
    return pieces


def crown(head, m):
    """A ring of short spikes round the crown."""
    pieces = []
    for n, yaw in enumerate(range(20, 360, 40)):
        foot = head.on(yaw, 46.0, 0.97)
        out = k.direction(yaw, 46.0)
        tip = foot + (out * 0.75 + V((0, 0, 1)) * 0.55).normalized() * 0.044
        pieces.append(k.tube(f"{head.name}-crown{n}", [foot, foot.lerp(tip, 0.5), tip], [0.0070, 0.0052, 0.0012], side=V((0, 0, 1)), n=6, material=m["skin"]))
    return pieces


CRESTS = {"tall-fin": tall_fin, "low-fin": low_fin, "twin-fins": twin_fins, "spines": spines, "feathers": feathers, "frill": frill, "horns": horns, "crown": crown}


def crest_model(path, crest, body, face, level):
    """The crest `crest` on `body`'s head (manifest.json: "parts.crest:crest_model", args [crest, body, the race's and
    sex's first face], its level): bound to `head` and joined, with the body's skeleton."""
    if crest not in CRESTS:
        raise ValueError(f"no crest {crest} in parts/crest.py")
    p = with_face(body, face, level)
    rig = r.build_rig(p, forms.joints_of(p))
    parts = CRESTS[crest](Head(p), materials.make())
    k.apply_modifiers(parts)
    for part in parts:
        r.bind_rigid(rig, part, "head")
    k.join_by_ink(parts, f"{body}-{crest}")
    bodies.export_sized(path, p, export_skins=True)

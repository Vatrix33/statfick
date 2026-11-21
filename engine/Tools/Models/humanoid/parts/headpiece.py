# The small head pieces (Q24: circlets, flowers, ties, beads; hidden under a helmet; the user's list of 2026-11-18, Q61):
# each a few small rigid shapes placed on a head (yaw and pitch in degrees from the head's centre, parts of its radius
# out, as the hair's locks; or metres from the centre, parts.hair.xyz) so that they sit over any hair style, bound as a
# whole to the skeleton's `head` bone, a part worn as a garment (GM2-P8). They take the outfit's colours: its cloth, its
# trim (a circlet's gold is the trim's gold) and its leather (the look's slots `cloth`, `cloth.trim`, `leather`): no slots
# of their own.
# A piece worn on one side is on the figure's right (SIDE): the battle sees the figure from its right, and the sheet's
# three-quarter view shows that side too.
# A piece must suit every hair style, a shaved head and a mane alike, and can't know which it is worn with: so a band
# lies close to the skull, just proud of a hair style's cap (bands 1.05 of the skull's radius, caps 1.02 to 1.06), and
# shows across the brow and wherever no lock lies over it; standing off far enough to clear the fullest hair (1.12), it
# floated a centimetre off a shaved head.
import math

from mathutils import Vector

import forms
import kit as k
import materials
import rig as r
from make_models import export
from parts.face import with_face
from parts.hair import xyz

V = Vector
SIDE = -1            # the figure's right (x to its left)


class Head:
    """A head to place pieces on: its centre and its shape."""

    def __init__(self, p):
        self.centre = V(p["head"]["centre"])
        self.shape = forms.head_shape(p["head"])
        self.name = p["name"]

    def on(self, yaw, pitch, out=1.0):
        d = k.direction(yaw, pitch)
        return self.centre + (self.shape(d) - self.centre) * out

    def at(self, left, back, up):
        return self.on(*xyz(left, back, up))

    def frame(self, yaw, pitch):
        """The directions out of the head, round it (toward a greater yaw) and up along it there."""
        out = k.direction(yaw, pitch)
        round_ = k.direction(yaw + 90, 0)
        round_ = (round_ - out * round_.dot(out)).normalized()
        up = out.cross(round_)
        if up.z < 0:
            up = -up
        return out, round_, up


def blob(name, centre, axes, radii, material, cuts=3):
    """A small rounded shape about `centre`: `radii` along the three `axes`."""
    a, b, c = axes
    return k.shaped_sphere(name, lambda d: centre + a * (d.x * radii[0]) + b * (d.y * radii[1]) + c * (d.z * radii[2]), cuts=cuts, material=material)


def band(head, name, height, thick, material, front=26.0, side=22.0, back=14.0, skin=1.035, over=1.052, step=12):
    """A band round the head: on the brow's skin at the front, over the hair at the sides and the back."""
    points = []
    for yaw in list(range(0, 360, step)) + [0, step]:
        a = yaw if yaw <= 180 else 360 - yaw
        ease = (1 - math.cos(math.pi * min(a, 90.0) / 90.0)) / 2, (1 - math.cos(math.pi * max(0.0, a - 90.0) / 90.0)) / 2
        pitch = front + (side - front) * ease[0] + (back - side) * ease[1]
        out = skin + (over - skin) * forms.smooth(max(0.0, min(1.0, (a - 34.0) / 26.0)))
        points.append(head.on(yaw, pitch, out))
    return k.tube(name, points, [(height, thick)] * len(points), side=V((0, 0, 1)), n=8, material=material, bulge=(0.0, 0.0))


def circlet(head, m):
    """A thin gold circlet round the brow with a gem at its front."""
    out, round_, up = head.frame(0, 26)
    return [band(head, "circlet", 0.0042, 0.0024, m["cloth.trim"]),
            blob("circlet-gem", head.on(0, 26, 1.062), (round_, up, out), (0.0062, 0.0085, 0.0040), m["cloth"]),
            blob("circlet-setting", head.on(0, 26, 1.050), (round_, up, out), (0.0085, 0.0110, 0.0026), m["cloth.trim"])]


def flower(head, m):
    """A flower tucked over the ear: it stands off the skull as far as the ear does and rests on the ear's top, so it sits
    right on a shaved head too (only the fullest hair, a mane, hides it)."""
    out, round_, up = head.frame(SIDE * 77, 21)
    centre = head.on(SIDE * 77, 21, 1.125)
    parts = []
    for i in range(5):
        angle = math.radians(90 + i * 72)
        way = round_ * math.cos(angle) + up * math.sin(angle)
        across = out.cross(way)
        parts.append(blob(f"flower-petal{i}", centre + way * 0.0130 + out * 0.001, (way, across, out), (0.0125, 0.0085, 0.0032), m["cloth"]))
    parts.append(blob("flower-heart", centre + out * 0.0038, (round_, up, out), (0.0060, 0.0060, 0.0042), m["cloth.trim"]))
    return parts


def headband(head, m):
    """A cloth band tied round the head, its two ends hanging behind."""
    parts = [band(head, "headband", 0.0095, 0.0026, m["cloth"], front=27.0, side=21.0, back=10.0, skin=1.036, over=1.056)]
    knot = head.on(180, 10, 1.10)
    out, round_, up = head.frame(180, 10)
    parts.append(blob("headband-knot", knot, (round_, up, out), (0.011, 0.009, 0.007), m["cloth"]))
    for side, low in ((1, -0.085), (-1, -0.070)):
        path = [knot, knot + round_ * (side * 0.012) + V((0, 0.012, -0.020)), knot + round_ * (side * 0.022) + V((0, 0.016, low * 0.6)),
                knot + round_ * (side * 0.026) + V((0, 0.012, low))]
        parts.append(k.tube(f"headband-end{side}", path, [(0.008, 0.0018), (0.0085, 0.0018), (0.0085, 0.0018), (0.0070, 0.0016)], side=round_, n=6, material=m["cloth"]))
    return parts


def hair_beads(head, m):
    """A thong with three beads hanging before the ear."""
    s = SIDE
    places = [head.on(s * 60, 22, 1.10), head.at(s * 0.090, -0.040, -0.020), head.at(s * 0.094, -0.040, -0.070), head.at(s * 0.094, -0.038, -0.118)]
    parts = [k.tube("beads-thong", places, [0.0017] * 4, side=V((1, 0, 0)), n=6, material=m["leather"])]
    axes = (V((1, 0, 0)), V((0, 1, 0)), V((0, 0, 1)))
    for i, (up, material) in enumerate(((-0.036, "cloth.trim"), (-0.062, "cloth"), (-0.088, "cloth.trim"))):
        parts.append(blob(f"bead{i}", head.at(s * 0.0925, -0.040, up), axes, (0.0075, 0.0075, 0.0090), m[material]))
    parts.append(blob("beads-tip", head.at(s * 0.094, -0.038, -0.124), axes, (0.0042, 0.0042, 0.0100), m["cloth"]))
    return parts


def feather(head, m):
    """A feather tucked behind the ear: it lies back along the head and stands a little off it, its vane turned half
    outward so it shows from the side and from the front."""
    s = SIDE
    root, tip = head.at(s * 0.086, 0.028, 0.006), head.at(s * 0.106, 0.116, 0.108)
    way = (tip - root).normalized()
    outward = (V((s, 0, 0)) - way * way.x * s).normalized()
    flat = way.cross(outward).normalized()                       # the vane's width when it lies flat against the head
    across = (flat * 0.90 - outward * 0.42).normalized()         # its face to the side and a little to the front
    count = 8
    bow = [0.0, 0.004, 0.007, 0.008, 0.007, 0.005, 0.002, 0.0]    # the shaft's slight curve, away from the head
    points = [root + (tip - root) * (i / (count - 1)) + outward * bow[i] for i in range(count)]
    shares = (0.14, 0.55, 0.88, 1.0, 0.94, 0.74, 0.44, 0.10)
    thin = across.cross(way).normalized()
    return [k.tube("feather-vane", points, [(0.0200 * share, 0.0015) for share in shares], side=across, n=6, material=m["cloth"]),
            k.tube("feather-shaft", [points[0] - way * 0.014] + [q + thin * 0.0012 for q in points[:-1]], [0.0021] * count, side=across, n=5, material=m["cloth.trim"])]


def leaf_wreath(head, m):
    """A wreath of gold leaves round the head, open over the brow."""
    parts = []
    for i, yaw in enumerate(range(34, 327, 13)):
        a = yaw if yaw <= 180 else 360 - yaw
        pitch = 30.0 - 12.0 * (a / 180.0)
        out, round_, up = head.frame(yaw, pitch)
        tilt = (0.55 if i % 2 else -0.45) * (1 if yaw <= 180 else -1)
        way = (round_ * (1 if yaw <= 180 else -1) + up * tilt).normalized()
        across = out.cross(way)
        parts.append(blob(f"wreath-leaf{i}", head.on(yaw, pitch, 1.085) + up * (0.004 if i % 2 else -0.003), (way, across, out), (0.0150, 0.0058, 0.0022), m["cloth.trim"]))
    return parts


def hairpin(head, m):
    """A long pin through the hair at the back of the head, a bead at its end."""
    low, high = head.at(0.052, 0.112, 0.004), head.at(-0.050, 0.094, 0.086)
    way = (high - low).normalized()
    side = way.cross(V((0, 1, 0))).normalized()
    axes = (side, way.cross(side), way)
    return [k.tube("hairpin", [low - way * 0.012, low, high, high + way * 0.020], [0.0014, 0.0027, 0.0027, 0.0024], side=side, n=6, material=m["cloth.trim"]),
            blob("hairpin-bead", high + way * 0.030, axes, (0.0085, 0.0085, 0.0100), m["cloth"]),
            blob("hairpin-cap", high + way * 0.019, axes, (0.0050, 0.0050, 0.0030), m["cloth.trim"])]


def earrings(head, m):
    """A gold ring in each ear."""
    parts = []
    for side in (-1, 1):
        lobe = head.on(side * 88, -17, 1.035)
        centre = lobe + V((side * 0.002, 0.0, -0.0085))
        centre = lobe + V((side * 0.002, 0.0, -0.0095))
        points = [centre + V((0.0, math.cos(a) * 0.0090, math.sin(a) * 0.0090)) for a in [math.radians(90 + i * 30) for i in range(14)]]
        parts.append(k.tube(f"earring{side}", points, [0.0020] * len(points), side=V((1, 0, 0)), n=6, material=m["cloth.trim"]))
    return parts


PIECES = {"circlet": circlet, "flower": flower, "headband": headband, "hair-beads": hair_beads, "feather": feather, "leaf-wreath": leaf_wreath, "hairpin": hairpin,
          "earrings": earrings}


def headpiece_model(path, piece, body, face, level):
    """The head piece `piece` on `body`'s head (manifest.json: "parts.headpiece:headpiece_model", args [piece, body, the
    race's and sex's first face], its level): bound to `head` and joined, with the body's skeleton."""
    if piece not in PIECES:
        raise ValueError(f"no head piece {piece} in parts/headpiece.py")
    p = with_face(body, face, level)
    rig = r.build_rig(p, forms.joints_of(p))
    parts = PIECES[piece](Head(p), materials.make())
    k.apply_modifiers(parts)
    for part in parts:
        part.data.shade_smooth()
        r.bind_rigid(rig, part, "head")
    k.join_by_ink(parts, f"{body}-{piece}")
    export(path, export_skins=True)

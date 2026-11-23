# The tails (Q22: the Half-Beast's tail chosen freely, T14's beasts among them; the lizardmen's likewise): one piece
# hanging from the back of the hips, rigid in GM2 and bound to the skeleton's `hips` bone (chains for its motion come with
# the battle's animations), a part worn as a garment (GM2-P8). A furred tail takes the slot `fur`, its tip `fur.tip`; a
# lizard's the skin's colour. Placed from the body's own pelvis, so one definition fits every body; made per body (the
# builds differ in the hips' depth).
import math

from mathutils import Vector

import bodies
import forms
import kit as k
import materials
import rig as r

V = Vector


def root(p):
    """Where a tail leaves the body: the middle of the back of the pelvis, and the hips' depth there."""
    name, y, z, rx, ry = next(section for section in p["spine"] if section[0] == "pelvis")
    return V((0, y + ry * 0.80, z + 0.010)), ry


def along(start, steps):
    """A path from `start`: each step (back, up, length) adds a stretch in the body's middle plane."""
    points = [start]
    for back, up, length in steps:
        way = V((0, back, up)).normalized()
        points.append(points[-1] + way * length)
    return points


def smoothed(points, radii, steps=4):
    """A path and its radii with `steps` points to each stretch, rounded through the given ones (Catmull-Rom)."""
    out_points, out_radii = [], []
    n = len(points)
    for i in range(n - 1):
        p0, p1, p2, p3 = points[max(0, i - 1)], points[i], points[i + 1], points[min(n - 1, i + 2)]
        r0, r1, r2, r3 = radii[max(0, i - 1)], radii[i], radii[i + 1], radii[min(n - 1, i + 2)]
        for s in range(steps):
            u = s / steps
            u2, u3 = u * u, u * u * u
            out_points.append(0.5 * ((2 * p1) + (p2 - p0) * u + (2 * p0 - 5 * p1 + 4 * p2 - p3) * u2 + (3 * p1 - p0 - 3 * p2 + p3) * u3))
            out_radii.append(max(0.002, 0.5 * ((2 * r1) + (r2 - r0) * u + (2 * r0 - 5 * r1 + 4 * r2 - r3) * u2 + (3 * r1 - r0 - 3 * r2 + r3) * u3)))
    out_points.append(points[-1])
    out_radii.append(radii[-1])
    return out_points, out_radii


def furred(p, m, name, steps, radii, tip=0.0, flat=0.90, tufts=6):
    """A furred tail along `steps` (along) with a radius at each of its points, rounded; its last `tip` share (of its
    points) in `fur.tip`; `tufts` locks of fur standing off its thick end, so its outline is a brush's, not a hose's."""
    start, _ = root(p)
    path = along(start + V((0, -0.030, 0)), [(1, 0.2, 0.030)] + list(steps))
    path, radii = smoothed(path, [radii[0] * 0.8] + list(radii))
    girths = [(radius, radius * flat) for radius in radii]
    cut = len(path) - 1 - round((len(path) - 1) * tip) if tip else len(path) - 1
    pieces = [k.tube(f"{p['name']}-{name}", path[:cut + 1], girths[:cut + 1], side=V((1, 0, 0)), n=12, material=m["fur"], bulge=(0.0, 0.0 if tip else 0.012))]
    if tip:
        pieces.append(k.tube(f"{p['name']}-{name}-tip", path[cut:], [(a * 1.04, b * 1.04) for a, b in girths[cut:]], side=V((1, 0, 0)), n=12, material=m["fur.tip"],
                             bulge=(0.0, 0.012)))
    widest = max(radii)
    for n in range(tufts):
        i = min(len(path) - 3, max(2, round((len(path) - 1) * (0.42 + 0.50 * n / max(1, tufts - 1)))))
        if radii[i] < 0.022:
            continue
        way = (path[i + 1] - path[i - 1]).normalized()
        across = V((1, 0, 0))
        normal = way.cross(across).normalized()
        angle = math.radians(40.0 + 100.0 * ((n * 5) % 7) / 6.0) * (1 if n % 2 else -1) + (math.pi if n % 3 == 0 else 0.0)
        off = across * math.cos(angle) + normal * math.sin(angle)
        base = path[i] + off * (radii[i] * 0.55)
        reach = radii[i] * 1.5
        lock = [base - way * 0.004, base + (way * 0.75 + off * 0.55) * (reach * 0.55), base + (way * 0.9 + off * 0.42) * reach]
        material = m["fur.tip"] if tip and i >= cut else m["fur"]
        pieces.append(k.tube(f"{p['name']}-{name}-tuft{n}", lock, [radii[i] * 0.50, radii[i] * 0.34, 0.0015], side=across, n=6, material=material))
    return pieces


def wolf(p, m):
    """A long bushy tail hanging in a slow curve."""
    return furred(p, m, "tail", [(1, -0.5, 0.10), (1, -1.4, 0.13), (0.6, -1.6, 0.13), (0.5, -1.2, 0.12), (0.9, -0.5, 0.09)], [0.034, 0.054, 0.068, 0.066, 0.046, 0.008])


def fox(p, m):
    """A big brush of a tail with a pale tip."""
    return furred(p, m, "tail", [(1, -0.3, 0.10), (1, -1.0, 0.13), (0.7, -1.3, 0.14), (0.8, -0.8, 0.13), (1, -0.1, 0.11), (1, 0.4, 0.08)],
                  [0.034, 0.058, 0.080, 0.090, 0.078, 0.050, 0.008], tip=0.26)


def lynx(p, m):
    """A short bobbed tail with a dark tip."""
    return furred(p, m, "tail", [(1, 0.1, 0.07), (1, -0.3, 0.07), (1, -0.5, 0.05)], [0.032, 0.040, 0.036, 0.008], tip=0.34, tufts=3)


def bear(p, m):
    """A small round stub."""
    return furred(p, m, "tail", [(1, 0.2, 0.045), (1, 0.0, 0.040)], [0.036, 0.044, 0.012], tufts=0)


def tuft(p, m):
    """A thin tail ending in a tuft (the old looks' "tuft")."""
    return furred(p, m, "tail", [(1, -0.4, 0.10), (1, -1.3, 0.14), (0.7, -1.4, 0.14), (1, -0.6, 0.09), (1, 0.0, 0.08), (1, 0.3, 0.06)],
                  [0.017, 0.015, 0.014, 0.015, 0.046, 0.040, 0.006], tip=0.30, tufts=4)


def curled(p, m):
    """A bushy tail curled up over the back."""
    return furred(p, m, "tail", [(1, 0.5, 0.09), (0.8, 1.2, 0.11), (0.2, 1.4, 0.11), (-0.5, 1.0, 0.10), (-1, 0.2, 0.08), (-0.8, -0.6, 0.06)],
                  [0.034, 0.052, 0.064, 0.066, 0.056, 0.038, 0.008])


def scaled(p, m, name, steps, radii, spines=0, fin=0.0, flat=0.92):
    """A lizard's tail along `steps` in the skin's colour; `spines` spines along its top (the slot `claws`); `fin`: the
    height of a fin along its top (the slot `crest`)."""
    start, _ = root(p)
    path = along(start + V((0, -0.045, 0)), [(1, 0.1, 0.045)] + list(steps))
    path, radii = smoothed(path, [radii[0] * 0.92] + list(radii))
    pieces = [k.tube(f"{p['name']}-{name}", path, [(radius, radius * flat) for radius in radii], side=V((1, 0, 0)), n=12, material=m["skin"], bulge=(0.0, 0.010))]

    def top(i):
        way = (path[min(i + 1, len(path) - 1)] - path[max(i - 1, 0)]).normalized()
        return way, V((1, 0, 0)).cross(way).normalized()

    first, last = 4, len(path) - 3
    for s in range(spines):
        i = round(first + (last - first) * s / max(1, spines - 1))
        way, up = top(i)
        base = path[i] + up * (radii[i] * flat * 0.80)
        high = radii[i] * 0.85 + 0.012
        pieces.append(k.tube(f"{p['name']}-{name}-spine{s}", [base - up * 0.005, base + up * (high * 0.6) + way * (high * 0.25), base + up * high + way * (high * 0.6)],
                             [max(0.004, radii[i] * 0.30), max(0.003, radii[i] * 0.20), 0.0010], side=V((1, 0, 0)), n=6, material=m["claws"]))
    if fin:
        import bmesh
        bm = bmesh.new()
        lower, upper = [], []
        for i in range(first, last + 1):
            way, up = top(i)
            share = (i - first) / (last - first)
            foot = path[i] + up * (radii[i] * flat * 0.70)
            rise = fin * math.sin(math.pi * share) ** 0.6 * (1.0 if (i - first) % 3 == 0 else 0.80)
            lower.append(bm.verts.new(foot))
            upper.append(bm.verts.new(foot + up * rise + way * (rise * 0.25)))
        for i in range(len(lower) - 1):
            bm.faces.new((lower[i], lower[i + 1], upper[i + 1], upper[i]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        web = k.new_object(f"{p['name']}-{name}-fin", bm, m["crest"], smooth=False)
        k.solidify(web, 0.0032, offset=0.0)
        pieces.append(web)
    return pieces


_LIZARD = [(1, -0.25, 0.14), (1, -0.55, 0.18), (1, -0.60, 0.18), (1, -0.30, 0.16), (1, 0.05, 0.14), (1, 0.30, 0.10)]
_GIRTHS = [0.074, 0.068, 0.056, 0.042, 0.029, 0.017, 0.004]


def lizard(p, m):
    """A long thick tail tapering to a point (the old looks' "lizard")."""
    return scaled(p, m, "tail", _LIZARD, _GIRTHS)


def lizard_short(p, m):
    """A short thick tail."""
    return scaled(p, m, "tail", [(1, -0.2, 0.12), (1, -0.5, 0.14), (1, -0.4, 0.12), (1, -0.1, 0.08)], [0.076, 0.070, 0.054, 0.034, 0.006])


def lizard_whip(p, m):
    """A long thin tail, its end curled."""
    return scaled(p, m, "tail", [(1, -0.3, 0.14), (1, -0.7, 0.20), (1, -0.6, 0.20), (1, -0.1, 0.18), (1, 0.6, 0.14), (0.2, 1.0, 0.10), (-0.8, 0.5, 0.07)],
                  [0.060, 0.048, 0.036, 0.026, 0.018, 0.012, 0.008, 0.003])


def lizard_club(p, m):
    """A thick tail ending in a heavy club."""
    return scaled(p, m, "tail", [(1, -0.25, 0.14), (1, -0.55, 0.18), (1, -0.55, 0.17), (1, -0.25, 0.14), (1, 0.0, 0.09), (1, 0.1, 0.07)],
                  [0.074, 0.066, 0.052, 0.040, 0.036, 0.062, 0.030])


def lizard_spiked(p, m):
    """The long tail with a row of spines along its top."""
    return scaled(p, m, "tail", _LIZARD, _GIRTHS, spines=7)


def lizard_finned(p, m):
    """The long tail with a fin along its top."""
    return scaled(p, m, "tail", _LIZARD, _GIRTHS, fin=0.060)


TAILS = {"lizard": lizard, "lizard-short": lizard_short, "lizard-whip": lizard_whip, "lizard-club": lizard_club, "lizard-spiked": lizard_spiked,
         "lizard-finned": lizard_finned, "wolf": wolf, "fox": fox, "lynx": lynx, "bear": bear, "tuft": tuft, "curled": curled}


def tail_model(path, tail, body, face, level):
    """The tail `tail` on `body` (manifest.json: "parts.tail:tail_model", args [tail, body, a face of the body's (unused:
    the parts' builders share their arguments)], its level): bound to `hips` and joined, with the body's skeleton."""
    if tail not in TAILS:
        raise ValueError(f"no tail {tail} in parts/tail.py")
    p = bodies.params(body, level)
    rig = r.build_rig(p, forms.joints_of(p))
    parts = TAILS[tail](p, materials.make())
    k.apply_modifiers(parts)
    for part in parts:
        part.data.shade_smooth()
        r.bind_rigid(rig, part, "hips")
    k.join_by_ink(parts, f"{body}-{tail}")
    bodies.export_sized(path, p, export_skins=True)

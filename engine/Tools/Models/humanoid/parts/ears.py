# The Half-Beast's ears (Q22: its ears' shape chosen freely; T14: wolf, fox, bear and lynx among them): a pair of beast
# ears on the top of the head, rigid pieces bound to the skeleton's `head` bone, a part worn as a garment (GM2-P8). The
# ear's back takes the slot `fur`, its hollow the skin's colour, a tuft or a tip the slot `fur.tip`. Placed by the head's
# own shape (parts.headpiece.Head): they stand through any hair style.
import math

from mathutils import Vector

import bodies
import forms
import kit as k
import materials
import rig as r
from parts.face import with_face
from parts.headpiece import Head

V = Vector


def ear(head, m, name, side, yaw, pitch, height, width, depth, point=1.0, splay=0.20, tip=0.0, tuft=0.0, hollow=0.60):
    """One ear on the head's `side` at (yaw, pitch): a flattened cone `height` tall, `width` and `depth` at its foot, its
    outline narrowing by the power `point` (under 1 rounder, over 1 sharper), splayed outward by `splay`; `tip`: the share
    of its height that is its tip (in `fur.tip`); `tuft`: a tuft's length over the tip (the lynx's); `hollow`: the hollow's
    share of the ear's width."""
    out = k.direction(side * yaw, pitch)
    root = head.on(side * yaw, pitch, 0.955)
    axis = (out * 0.62 + V((0, 0, 1)) * 0.74 + V((side * splay, 0, 0))).normalized()
    face = V((0, -1, 0)) - axis * V((0, -1, 0)).dot(axis)
    face = (face + V((side * 0.35, 0, 0))).normalized()            # the hollow turned a little outward
    face = (face - axis * face.dot(axis)).normalized()
    across = axis.cross(face).normalized()

    def profile(s):
        return max(0.0, 1.0 - s) ** point * (1.0 - (1.0 - min(1.0, s * 4.0)) ** 2)

    def cone(d, wide, deep, tall, forward=0.0, low=0.0, top=1.0):
        s = low + (top - low) * (d.z + 1.0) / 2.0
        angle = math.atan2(d.y, d.x)
        ring = max(0.25, profile(s)) if s < 0.06 else profile(s)
        return root + axis * (tall * s) + across * (math.cos(angle) * wide * ring) + face * (math.sin(angle) * deep * ring + forward)

    pieces = [k.shaped_sphere(f"{head.name}-{name}", lambda d: cone(d, width, depth, height), cuts=4, material=m["fur"]),
              k.shaped_sphere(f"{head.name}-{name}-hollow", lambda d: cone(d, width * hollow, depth * 0.30, height * 0.80, forward=depth * 0.78, low=0.10, top=0.92),
                              cuts=3, material=m["skin"])]
    if tip:
        pieces.append(k.shaped_sphere(f"{head.name}-{name}-tip", lambda d: cone(d, width * 1.06, depth * 1.10, height * 1.02, low=1.0 - tip, top=1.0), cuts=3,
                                      material=m["fur.tip"]))
    if tuft:
        peak = root + axis * height
        pieces.append(k.tube(f"{head.name}-{name}-tuft", [peak - axis * 0.006, peak + axis * (tuft * 0.5) + across * (side * 0.004), peak + axis * tuft + across * (side * 0.012)],
                             [0.0042, 0.0034, 0.0008], side=across, n=6, material=m["fur.tip"]))
    return pieces


def pair(head, m, **shape):
    return ear(head, m, "ear-l", 1, **shape) + ear(head, m, "ear-r", -1, **shape)


def wolf(head, m):
    """Tall pointed ears, upright."""
    return pair(head, m, yaw=40.0, pitch=52.0, height=0.105, width=0.036, depth=0.016, point=0.85, splay=0.16)


def fox(head, m):
    """Large pointed ears, broad at the foot, with dark tips."""
    return pair(head, m, yaw=42.0, pitch=50.0, height=0.125, width=0.046, depth=0.018, point=0.95, splay=0.28, tip=0.30)


def lynx(head, m):
    """Pointed ears with a tuft at the tip."""
    return pair(head, m, yaw=40.0, pitch=52.0, height=0.090, width=0.036, depth=0.016, point=0.75, splay=0.20, tuft=0.034)


def bear(head, m):
    """Small round ears."""
    return pair(head, m, yaw=46.0, pitch=50.0, height=0.056, width=0.036, depth=0.016, point=0.36, splay=0.24, hollow=0.55)


def cat(head, m):
    """Short pointed ears, broad at the foot."""
    return pair(head, m, yaw=38.0, pitch=54.0, height=0.074, width=0.040, depth=0.016, point=0.80, splay=0.22)


def hound(head, m):
    """Long ears folded over, hanging beside the head."""
    pieces = []
    for side in (1, -1):
        start = head.on(side * 62.0, 48.0, 1.0)
        path = [start + V((0, 0, -0.006)), start + V((side * 0.020, 0.000, 0.010)), head.on(side * 84.0, 28.0, 1.10), head.on(side * 90.0, 4.0, 1.12),
                head.on(side * 90.0, -18.0, 1.10)]
        pieces.append(k.tube(f"{head.name}-ear{side}", path, [(0.020, 0.010), (0.028, 0.009), (0.034, 0.008), (0.034, 0.007), (0.018, 0.005)], side=V((0, 1, 0)), n=8,
                             material=m["fur"], bulge=(0.004, 0.010)))
    return pieces


EARS = {"wolf": wolf, "fox": fox, "lynx": lynx, "bear": bear, "cat": cat, "hound": hound}


def ears_model(path, ears, body, face, level):
    """The ears `ears` on `body`'s head (manifest.json: "parts.ears:ears_model", args [ears, body, the race's and sex's
    first face], its level): bound to `head` and joined, with the body's skeleton."""
    if ears not in EARS:
        raise ValueError(f"no ears {ears} in parts/ears.py")
    p = with_face(body, face, level)
    rig = r.build_rig(p, forms.joints_of(p))
    parts = EARS[ears](Head(p), materials.make())
    k.apply_modifiers(parts)
    for part in parts:
        part.data.shade_smooth()
        r.bind_rigid(rig, part, "head")
    k.join_by_ink(parts, f"{body}-{ears}")
    bodies.export_sized(path, p, export_skins=True)

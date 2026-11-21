# The marks (Q23: scars, freckles; each list with "none"; the user's list of 2026-11-18, Q61): flat pieces on the skin of
# a face, built by forms.build_marks and bound to the skeleton's `head` bone, a part worn as a garment (GM2-P8), in the
# slot `marking` (one mark at a time, one colour: a freckle's brown, a scar's pale). A mark lies on the skin, and the
# faces differ by millimetres where the marks are (the brow's ridge, the cheekbones, the nose), so a mark is made per
# face. Places in degrees from the head's centre (yaw from the face's front, positive to the figure's left; pitch up),
# given from the face's own eyes and mouth so a mark sits right on every face. A mark on one side is on the figure's
# right (the side the battle and the sheet show).
import math

import forms
import kit as k
import materials
import rig as r
from make_models import export
from parts.face import with_face

SIDE = -1


def freckles(h):
    """Freckles across the nose and the cheeks under the eyes."""
    eye, low = h["eye"], h["mouth"]["pitch"]
    pieces = []
    spots = [(0.0, 0.30), (3.4, 0.24), (6.6, 0.33), (9.6, 0.22), (12.8, 0.36), (16.0, 0.27), (19.4, 0.38), (22.6, 0.30), (25.6, 0.42), (28.4, 0.34),
             (5.0, 0.42), (11.0, 0.47), (14.6, 0.52), (18.0, 0.46), (21.4, 0.55), (24.6, 0.50), (8.2, 0.52), (17.0, 0.62), (22.0, 0.66), (26.6, 0.60), (12.4, 0.64)]
    top = eye["pitch"] - eye["high"] - 1.6
    for n, (yaw, share) in enumerate(spots):
        pitch = top + (low + 7.0 - top) * (share - 0.22) / 0.44 * 0.62
        size = 0.62 + 0.13 * ((n * 7) % 3)
        for side in ((1,) if yaw == 0.0 else (-1, 1)):
            wobble = 0.5 * math.sin(n * 2.3 + side)
            pieces.append({"spot": (side * yaw + wobble, pitch + 0.4 * math.cos(n * 1.7 + side), size, 6), "lift": 0.0009})
    return pieces


def scar_brow(h):
    """A scar through the right eyebrow, from the forehead down to the eye's lid."""
    eye = h["eye"]
    yaw = SIDE * (eye["yaw"] + 3.0)
    top, low = eye["pitch"] + eye["high"] + 12.5, eye["pitch"] + eye["high"] + 1.2
    points = [(yaw + SIDE * 2.4 * (1 - t) - SIDE * 0.9 * t, top + (low - top) * t, 0.95 * (0.25 + 0.75 * math.sin(math.pi * (0.1 + 0.8 * t))))
              for t in (0.0, 0.125, 0.25, 0.375, 0.5, 0.625, 0.75, 0.875, 1.0)]
    return [{"strip": points, "lift": 0.0034}]


def scar_cheek(h):
    """A long scar down the right cheek."""
    eye, mouth = h["eye"], h["mouth"]
    top, low = eye["pitch"] - eye["high"] - 4.0, mouth["pitch"] + 4.0
    points = []
    for i in range(11):
        t = i / 10
        yaw = SIDE * (36.0 - 12.0 * t + 1.6 * math.sin(math.pi * t))
        points.append((yaw, top + (low - top) * t, 1.05 * (0.25 + 0.75 * math.sin(math.pi * (0.08 + 0.84 * t)))))
    return [{"strip": points, "lift": 0.0011}]


def scar_nose(h):
    """A scar across the bridge of the nose, from under the right eye to the left cheek."""
    eye = h["eye"]
    height = eye["pitch"] - eye["high"] - 1.0
    points = []
    for i in range(19):
        t = i / 18
        yaw = SIDE * 15.0 * (1 - t) - SIDE * 12.0 * t
        points.append((yaw, height - 1.2 + 2.6 * t + 0.8 * math.sin(math.pi * t), 0.95 * (0.25 + 0.75 * math.sin(math.pi * (0.08 + 0.84 * t)))))
    return [{"strip": points, "lift": 0.0014}]


def beauty_mark(h):
    """A beauty mark above the right corner of the mouth."""
    mouth = h["mouth"]
    return [{"spot": (SIDE * (mouth["wide"] + 5.5), mouth["pitch"] + 5.2, 0.95, 8), "lift": 0.0010}]


def scar_lip(h):
    """A short scar through the right of the lips."""
    mouth = h["mouth"]
    yaw = SIDE * mouth["wide"] * 0.52
    top, low = mouth["pitch"] + 5.6, mouth["pitch"] - 5.2
    points = [(yaw + SIDE * 1.2 * (0.5 - t), top + (low - top) * t, 0.80 * (0.25 + 0.75 * math.sin(math.pi * (0.1 + 0.8 * t)))) for t in (0.0, 0.125, 0.25, 0.375, 0.5, 0.625, 0.75, 0.875, 1.0)]
    return [{"strip": points, "lift": 0.0024}]


MARKS = {"freckles": freckles, "scar-brow": scar_brow, "scar-cheek": scar_cheek, "scar-nose": scar_nose, "beauty-mark": beauty_mark, "scar-lip": scar_lip}


def mark_model(path, mark, body, face, level):
    """The mark `mark` on `body`'s head with the face `face` (manifest.json: "parts.mark:mark_model", args [mark, body,
    face], its level): bound to `head`, with the body's skeleton."""
    if mark not in MARKS:
        raise ValueError(f"no mark {mark} in parts/mark.py")
    p = with_face(body, face, level)
    rig = r.build_rig(p, forms.joints_of(p))
    parts = forms.build_marks(p, materials.make(), MARKS[mark](p["head"]))
    for part in parts:
        r.bind_rigid(rig, part, "head")
    k.join_by_ink(parts, f"{body}-{face}-{mark}")
    export(path, export_skins=True)

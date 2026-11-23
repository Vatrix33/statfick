# The marks (Q23: scars, freckles; each list with "none"; the user's list of 2026-11-18, Q61): flat pieces on the skin of
# a face, built by forms.build_marks and bound to the skeleton's `head` bone, a part worn as a garment (GM2-P8), in the
# slot `marking` (one mark at a time, one colour: a freckle's brown, a scar's pale). A mark lies on the skin, and the
# faces differ by millimetres where the marks are (the brow's ridge, the cheekbones, the nose), so a mark is made per
# face. Places in degrees from the head's centre (yaw from the face's front, positive to the figure's left; pitch up),
# given from the face's own eyes and mouth so a mark sits right on every face. A mark on one side is on the figure's
# right (the side the battle and the sheet show).
import math

import bodies
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


# ---- the Goliaths' rune patterns (T4: "rune-like markings"; Q23: optional; Q42: each Goliath preset wears one) ----------
def _stroke(a, b, half=1.30, lift=0.0013, n=6):
    """A straight stroke of a rune from a to b ((yaw, pitch) in degrees), a little thinner at its ends."""
    points = []
    for i in range(n):
        t = i / (n - 1)
        points.append((a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, half * (0.74 + 0.26 * math.sin(math.pi * t))))
    return {"strip": points, "lift": lift}


def _strokes(lines, **how):
    return [_stroke(a, b, **how) for a, b in lines]


def runes_brow(h):
    """Three runes across the forehead: a forked stave in the middle, a diamond over each brow."""
    eye = h["eye"]
    base = eye["pitch"] + eye["high"] + 13.5
    lines = [((0, base), (0, base + 17.0)), ((0, base + 8.0), (5.2, base + 15.0)), ((0, base + 8.0), (-5.2, base + 15.0))]
    for side in (-1, 1):
        x, y = side * (eye["yaw"] + 1.5), base + 8.5
        lines += [((x - 4.0, y), (x, y + 6.4)), ((x, y + 6.4), (x + 4.0, y)), ((x + 4.0, y), (x, y - 6.4)), ((x, y - 6.4), (x - 4.0, y))]
    return _strokes(lines, lift=0.0018)


def runes_cheek(h):
    """Two chevrons on each cheekbone under the eye."""
    eye = h["eye"]
    lines = []
    for side in (-1, 1):
        x = side * (eye["yaw"] + 7.0)
        for step in (0.0, 6.6):
            y = eye["pitch"] - eye["high"] - 6.0 - step
            lines += [((x - side * 7.0, y + 1.6), (x, y - 4.2)), ((x, y - 4.2), (x + side * 7.0, y + 1.6))]
    return _strokes(lines, lift=0.0016)


def runes_chin(h):
    """A stave from under the lip down the chin with two slanted bars."""
    low = h["mouth"]["pitch"] - 4.5
    lines = [((0, low), (0, low - 13.5)), ((-4.2, low - 2.6), (4.2, low - 5.6)), ((-4.2, low - 7.4), (4.2, low - 10.4))]
    return _strokes(lines, lift=0.0024)


def runes_eye(h):
    """A stave down through the right eye: from the forehead to the lid, and from under the eye down the cheek, a tick at each end."""
    eye = h["eye"]
    x = SIDE * (eye["yaw"] + 0.5)
    top, lid, under, low = eye["pitch"] + eye["high"] + 19.0, eye["pitch"] + eye["high"] + 3.4, eye["pitch"] - eye["high"] - 2.4, eye["pitch"] - eye["high"] - 19.0
    lines = [((x, top), (x, lid)), ((x, under), (x, low)), ((x - 3.6, top - 1.6), (x + 3.6, top + 1.6)), ((x - 3.6, low - 1.6), (x + 3.6, low + 1.6))]
    return _strokes(lines, lift=0.0038)


def runes_temple(h):
    """Three runes down each temple, from the brow's end to the cheekbone (on both sides: the sheet shows the figure's left,
    the battle its right)."""
    eye = h["eye"]
    lines = []
    for side in (-1, 1):
        for n, (yaw, pitch) in enumerate(((32.0, 17.0), (36.0, 5.0), (37.5, -7.0))):
            x, y = side * yaw, eye["pitch"] + pitch
            lines.append(((x, y + 4.6), (x, y - 4.6)))
            if n == 0:
                lines += [((x, y + 4.6), (x + side * 4.0, y + 1.2)), ((x, y + 0.4), (x + side * 4.0, y - 3.0))]
            elif n == 1:
                lines += [((x - side * 3.6, y + 2.6), (x + side * 3.6, y - 2.6))]
            else:
                lines += [((x, y + 4.6), (x + side * 4.0, y + 1.0)), ((x + side * 4.0, y + 1.0), (x, y - 1.6))]
    return _strokes(lines, lift=0.0017)


def runes_band(h):
    """A band across the forehead: a line from temple to temple with staves hanging from it, long and short in turn."""
    eye = h["eye"]
    y = eye["pitch"] + eye["high"] + 27.0
    xs = [-30.0 + 7.5 * i for i in range(9)]
    lines = [((xs[i], y), (xs[i + 1], y)) for i in range(8)]
    for i in range(1, 8):
        lines.append(((xs[i], y), (xs[i], y - (8.0 if i % 2 == 0 else 4.4))))
    return _strokes(lines, lift=0.0017)


# ---- the Merfolk's scale patterns (T3: "scales"; Q23: optional; Q42: each Merfolk preset wears one) -----------------------
def _scale(x, y, radius, half=0.90, lift=0.0013, n=7):
    """One scale: the lower arc of a circle about (x, y) (degrees), thinner at its ends."""
    points = []
    for i in range(n):
        t = i / (n - 1)
        a = math.radians(196.0 + 148.0 * t)
        points.append((x + radius * math.cos(a), y + radius * math.sin(a), half * (0.55 + 0.45 * math.sin(math.pi * t))))
    return {"strip": points, "lift": lift}


def _rows(x, y, counts, radius, towards=1, **how):
    """Rows of scales under each other, each row shifted half a scale: `counts` scales per row, the first row's middle at
    (x, y); `towards`: the side the rows' middles drift to (a cheek's rows follow the cheekbone outward)."""
    pieces = []
    for row, count in enumerate(counts):
        for i in range(count):
            pieces.append(_scale(x + towards * row * 0.5 * radius + (i - (count - 1) / 2) * 1.9 * radius, y - row * 1.25 * radius, radius, **how))
    return pieces


def scales_cheek(h):
    """Scales on each cheekbone: three rows narrowing downward."""
    eye = h["eye"]
    pieces = []
    for side in (-1, 1):
        pieces += _rows(side * (eye["yaw"] + 10.0), eye["pitch"] - eye["high"] - 5.5, (3, 2, 1), 3.6, towards=side, lift=0.0015)
    return pieces


def scales_brow(h):
    """Two rows of scales across the forehead."""
    eye = h["eye"]
    return _rows(0.0, eye["pitch"] + eye["high"] + 23.0, (5, 4), 4.2, towards=0, lift=0.0016)


def scales_temple(h):
    """Scales down each temple, from the brow's end to the cheekbone."""
    eye = h["eye"]
    pieces = []
    for side in (-1, 1):
        pieces += _rows(side * 35.0, eye["pitch"] + 17.0, (2, 2, 2, 1), 3.5, towards=side * 0.4, lift=0.0016)
    return pieces


def scales_nose(h):
    """A band of small scales over the bridge of the nose and under each eye."""
    eye = h["eye"]
    pieces = _rows(0.0, eye["pitch"] + 0.5, (2,), 2.6, towards=0, lift=0.0032)
    for side in (-1, 1):
        pieces += _rows(side * (eye["yaw"] + 1.5), eye["pitch"] - eye["high"] - 3.6, (3, 2), 2.9, towards=side, lift=0.0016)
    return pieces


def scales_jaw(h):
    """A row of scales along each side of the jaw, from under the ear toward the chin."""
    low = h["mouth"]["pitch"]
    pieces = []
    for side in (-1, 1):
        for i in range(5):
            t = i / 4
            pieces.append(_scale(side * (54.0 - 32.0 * t), low + 13.0 - 15.0 * t, 3.7, lift=0.0016))
            if i < 4:
                pieces.append(_scale(side * (50.0 - 32.0 * t), low + 17.6 - 15.0 * t, 3.7, lift=0.0016))
    return pieces


def scales_many(h):
    """Scales on the brow, the temples and the cheeks together."""
    return scales_brow(h) + scales_temple(h) + scales_cheek(h)


MARKS = {"scales-cheek": scales_cheek, "scales-brow": scales_brow, "scales-temple": scales_temple, "scales-nose": scales_nose, "scales-jaw": scales_jaw,
         "scales-many": scales_many, "freckles": freckles, "scar-brow": scar_brow, "scar-cheek": scar_cheek, "scar-nose": scar_nose, "beauty-mark": beauty_mark, "scar-lip": scar_lip,
         "runes-brow": runes_brow, "runes-cheek": runes_cheek, "runes-chin": runes_chin, "runes-eye": runes_eye, "runes-temple": runes_temple,
         "runes-band": runes_band}


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
    bodies.export_sized(path, p, export_skins=True)

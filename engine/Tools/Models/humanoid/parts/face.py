# The faces (Q2, Q16, Q40: about six stylised faces per race and sex, no face bones): a face is its head's features laid
# on the body's head frame (bodies/: its centre and size), built by forms.build_head, and bound as a whole to the
# skeleton's `head` bone: a part the figure wears as a garment (GM2-P8). Its features:
#   jaw, chin, brow, cheek, ear, flat   the skull's narrowing at the jaw, the chin's and the brow ridge's push, the
#                                       cheekbones', the ears' size, the face's flatness in plan (a superellipse's power)
#   nose                                how far it stands out and how wide (degrees); nostril, lips: their masses
#   eye                                 the eyes' place (yaw, pitch), their almond's half width and height, the iris's
#   browline, browthick                 the brow's points (yaw, pitch, thickness share) and its thickness (degrees)
#   mouth                               its width, height, curve, line thickness and its lips (0: a line alone)
# The human man's and woman's first faces are the user's choice of the three candidates (2026-11-16: candidate A).
import os
import bodies
import forms
import kit as k
import materials
import rig as r
from make_models import export

FACES = {
    "human-male-1": {
        "jaw": 0.33, "chin": 0.009, "brow": 0.0045, "nose": (0.028, 5.2), "cheek": 0.004, "ear": 1.0, "flat": 2.8,
        "eye": {"yaw": 16.0, "pitch": 0.0, "wide": 6.0, "high": 2.9, "iris": 2.55},
        "browline": [(7.0, 7.6, 1.0), (13.0, 9.2, 1.1), (20.0, 9.9, 0.95), (27.5, 8.6, 0.5)], "browthick": 1.45,
        "mouth": {"wide": 9.5, "pitch": -31.4, "curve": 0.35, "thick": 0.42, "lip": 0.0},
    },
    "human-female-1": {
        "jaw": 0.42, "chin": 0.005, "brow": 0.0005, "nose": (0.021, 4.0), "nostril": 0.002, "cheek": 0.005, "ear": 0.88, "flat": 2.6,
        "lips": 0.003,
        "eye": {"yaw": 16.5, "pitch": -0.5, "wide": 7.0, "high": 3.7, "iris": 3.0},
        "browline": [(8.0, 9.6, 0.9), (14.0, 11.6, 1.0), (21.0, 12.4, 0.8), (28.0, 10.4, 0.35)], "browthick": 0.95,
        "mouth": {"wide": 8.2, "pitch": -31.6, "curve": 0.3, "thick": 0.36, "lip": 1.0},
    },
    # The human's other faces (GM2 step 9a; Q16, Q40: six per race and sex). The skull is the first face's (the head's
    # frame and its flatness in plan), so every hair style and head piece made on it fits them all; what differs is the
    # jaw, the chin, the brow, the nose, the cheeks, the eyes, the brows and the mouth.
    # 2: broad and square, a heavy brow, a wide nose, a straight mouth
    "human-male-2": {
        "jaw": 0.22, "chin": 0.012, "brow": 0.0062, "nose": (0.029, 6.4), "nostril": 0.0036, "cheek": 0.003, "ear": 1.05, "flat": 2.8,
        "eye": {"yaw": 16.4, "pitch": -0.3, "wide": 5.6, "high": 2.5, "iris": 2.3},
        "browline": [(6.5, 6.9, 1.0), (13.0, 7.9, 1.15), (20.5, 8.3, 1.0), (27.5, 7.4, 0.6)], "browthick": 1.8,
        "mouth": {"wide": 10.6, "pitch": -31.8, "curve": 0.1, "thick": 0.46, "lip": 0.0},
    },
    # 3: lean and sharp, high cheekbones, a long narrow nose, narrow eyes
    "human-male-3": {
        "jaw": 0.43, "chin": 0.011, "brow": 0.003, "nose": (0.032, 4.3), "nostril": 0.0024, "cheek": 0.0058, "ear": 0.96, "flat": 2.8,
        "eye": {"yaw": 15.6, "pitch": 0.3, "wide": 6.2, "high": 2.4, "iris": 2.3},
        "browline": [(7.0, 7.0, 0.9), (13.0, 8.9, 1.0), (20.0, 10.2, 0.85), (27.0, 9.6, 0.4)], "browthick": 1.2,
        "mouth": {"wide": 8.6, "pitch": -31.2, "curve": 0.15, "thick": 0.38, "lip": 0.0},
    },
    # 4: round and open, a short nose, large eyes, a smile
    "human-male-4": {
        "jaw": 0.27, "chin": 0.0045, "brow": 0.002, "nose": (0.024, 5.8), "nostril": 0.0034, "cheek": 0.006, "ear": 1.04, "flat": 2.8,
        "eye": {"yaw": 16.6, "pitch": -0.2, "wide": 6.4, "high": 3.5, "iris": 2.9},
        "browline": [(7.5, 8.6, 0.9), (13.5, 10.6, 1.05), (20.5, 11.2, 0.9), (27.5, 9.4, 0.5)], "browthick": 1.35,
        "mouth": {"wide": 9.6, "pitch": -31.6, "curve": 0.75, "thick": 0.42, "lip": 0.0},
    },
    # 5: weathered and hawkish, a strong nose, deep-set eyes under low brows, a mouth turned down
    "human-male-5": {
        "jaw": 0.36, "chin": 0.010, "brow": 0.0068, "nose": (0.035, 4.9), "nostril": 0.003, "cheek": 0.0065, "ear": 1.0, "flat": 2.8,
        "eye": {"yaw": 15.8, "pitch": -0.6, "wide": 5.8, "high": 2.3, "iris": 2.25},
        "browline": [(6.0, 5.9, 1.1), (12.5, 7.6, 1.15), (20.0, 8.6, 0.9), (27.5, 7.9, 0.45)], "browthick": 1.65,
        "mouth": {"wide": 9.2, "pitch": -31.8, "curve": -0.25, "thick": 0.40, "lip": 0.0},
    },
    # 6: young and fine, a small straight nose, clear eyes, light brows
    "human-male-6": {
        "jaw": 0.39, "chin": 0.006, "brow": 0.002, "nose": (0.025, 4.5), "nostril": 0.0026, "cheek": 0.0045, "ear": 0.94, "flat": 2.8,
        "eye": {"yaw": 16.2, "pitch": 0.0, "wide": 6.6, "high": 3.2, "iris": 2.8},
        "browline": [(7.5, 8.2, 0.85), (13.5, 9.9, 1.0), (20.5, 10.5, 0.8), (27.5, 9.1, 0.4)], "browthick": 1.1,
        "mouth": {"wide": 8.8, "pitch": -31.3, "curve": 0.4, "thick": 0.38, "lip": 0.0},
    },
    # 2: a strong jaw and level brows, a steady mouth
    "human-female-2": {
        "jaw": 0.32, "chin": 0.008, "brow": 0.0018, "nose": (0.024, 4.6), "nostril": 0.0024, "cheek": 0.004, "ear": 0.9, "flat": 2.6,
        "lips": 0.003,
        "eye": {"yaw": 16.6, "pitch": -0.4, "wide": 6.6, "high": 3.1, "iris": 2.75},
        "browline": [(7.5, 8.6, 0.95), (14.0, 10.0, 1.05), (21.0, 10.6, 0.9), (28.0, 9.6, 0.45)], "browthick": 1.15,
        "mouth": {"wide": 8.9, "pitch": -31.8, "curve": 0.08, "thick": 0.38, "lip": 0.9},
    },
    # 3: heart-shaped, large bright eyes, high thin brows, a full smiling mouth
    "human-female-3": {
        "jaw": 0.49, "chin": 0.004, "brow": 0.0004, "nose": (0.019, 3.6), "nostril": 0.0018, "cheek": 0.007, "ear": 0.86, "flat": 2.6,
        "lips": 0.0034,
        "eye": {"yaw": 16.8, "pitch": -0.6, "wide": 7.4, "high": 4.1, "iris": 3.3},
        "browline": [(8.5, 10.6, 0.85), (14.5, 12.9, 0.95), (21.5, 13.6, 0.75), (28.0, 11.4, 0.3)], "browthick": 0.85,
        "mouth": {"wide": 7.8, "pitch": -31.4, "curve": 0.55, "thick": 0.34, "lip": 1.2},
    },
    # 4: long and fine, high cheekbones, a straight longer nose, a narrow gaze
    "human-female-4": {
        "jaw": 0.45, "chin": 0.008, "brow": 0.0008, "nose": (0.026, 3.7), "nostril": 0.0018, "cheek": 0.0060, "ear": 0.88, "flat": 2.6,
        "lips": 0.0028,
        "eye": {"yaw": 16.0, "pitch": 0.2, "wide": 7.0, "high": 3.0, "iris": 2.7},
        "browline": [(8.0, 9.0, 0.85), (14.0, 11.2, 0.95), (21.0, 12.6, 0.8), (28.5, 11.8, 0.35)], "browthick": 0.9,
        "mouth": {"wide": 8.4, "pitch": -31.5, "curve": 0.15, "thick": 0.36, "lip": 0.85},
    },
    # 5: round and soft, a small nose, wide eyes, full lips
    "human-female-5": {
        "jaw": 0.33, "chin": 0.003, "brow": 0.0004, "nose": (0.018, 4.5), "nostril": 0.0028, "cheek": 0.007, "ear": 0.9, "flat": 2.6,
        "lips": 0.0032,
        "eye": {"yaw": 17.0, "pitch": -0.8, "wide": 6.8, "high": 4.0, "iris": 3.2},
        "browline": [(8.5, 10.0, 0.95), (14.5, 11.8, 1.05), (21.0, 12.2, 0.85), (27.0, 10.6, 0.4)], "browthick": 1.0,
        "mouth": {"wide": 8.6, "pitch": -31.9, "curve": 0.45, "thick": 0.36, "lip": 1.2},
    },
    # 6: sharp and wry, a pointed chin, angled brows, a thin level mouth
    "human-female-6": {
        "jaw": 0.47, "chin": 0.0075, "brow": 0.001, "nose": (0.023, 3.6), "nostril": 0.002, "cheek": 0.006, "ear": 0.86, "flat": 2.6,
        "lips": 0.0026,
        "eye": {"yaw": 16.2, "pitch": -0.2, "wide": 7.2, "high": 3.0, "iris": 2.8},
        "browline": [(7.5, 8.4, 0.9), (13.5, 10.9, 1.0), (20.5, 12.6, 0.85), (27.5, 12.0, 0.35)], "browthick": 1.05,
        "mouth": {"wide": 8.0, "pitch": -31.5, "curve": 0.12, "thick": 0.40, "lip": 0.75},
    },
}


def elven(face, ear=(0.050, 0.35, 0.50, 0.80), tilt=7.0, narrow=0.88, frown=0.0):
    """A human face as an elf's (GM2 step 9b; "a face list per race keeps the race's nose and features"): long pointed
    ears, narrow eyes with their outer corners raised, a finer jaw and nose, higher cheekbones, thinner brows swept up."""
    h = dict(face)
    h["ear_long"] = ear
    h["jaw"] = min(0.56, face["jaw"] + 0.07)
    h["cheek"] = min(face["cheek"] + 0.0010, 0.0062)          # (higher, the cheekbone throws a speck of shadow)
    h["nose"] = (face["nose"][0] * 0.96, face["nose"][1] * 0.86)
    h["eye"] = dict(face["eye"], high=face["eye"]["high"] * narrow, tilt=tilt)
    points = face["browline"]
    last = len(points) - 1
    h["browline"] = [(yaw, pitch + 1.4 * i / last - frown * (1 - i / last), thick) for i, (yaw, pitch, thick) in enumerate(points)]
    h["browthick"] = face["browthick"] * 0.85
    return h


def dwarven(face, nose=(1.22, 1.28), nostril=1.3, brow=0.0012):
    """A human face as a dwarf's (GM2 step 9c; the old `nose: "big"`): a big broad nose, a heavy brow, a broad jaw, larger
    ears (`nose`, `nostril`, `brow`: how much; the women's less, their noses' shaded side else a dark wedge on the face)."""
    h = dict(face)
    h["nose"] = (face["nose"][0] * nose[0], face["nose"][1] * nose[1])
    h["nostril"] = face.get("nostril", 0.003) * nostril
    h["jaw"] = max(0.16, face["jaw"] - 0.06)
    h["brow"] = face["brow"] + brow
    h["ear"] = face["ear"] * 1.10
    h["browthick"] = face["browthick"] * 1.12
    return h


def hobbitish(face):
    """A human face as a hobbit's (GM2 step 9c; the old `nose: "small"`): a small nose, round cheeks and jaw, large eyes
    and ears."""
    h = dict(face)
    h["nose"] = (face["nose"][0] * 0.84, face["nose"][1] * 0.95)
    h["jaw"] = max(0.20, face["jaw"] - 0.04)
    h["cheek"] = min(face["cheek"] + 0.0008, 0.0062)
    h["chin"] = face["chin"] * 0.8
    h["ear"] = face["ear"] * 1.15
    h["eye"] = dict(face["eye"], high=face["eye"]["high"] * 1.10, iris=face["eye"]["iris"] * 1.06)
    return h


# The lizardmen's faces (GM2 step 9d): a muzzle, slit eyes at the head's sides, no ears, no nose, no brows; six per sex by
# the muzzle's length and breadth, the brow's ridge, the eyes and the jaw.
LIZARD = {"jaw": 0.30, "chin": 0.002, "brow": 0.0050, "nose": (0.0, 5.0), "nostril": 0.0, "cheek": 0.002, "ear": 1.0, "flat": 2.3, "ear_none": True, "lips": 0.0,
          "brows": False, "snout": (0.080, 46.0, -15.0, 33.0, -0.10),
          "eye": {"yaw": 31.0, "pitch": 9.0, "wide": 7.2, "high": 4.1, "iris": 3.3, "slit": True, "tilt": 8.0},
          "browline": [(7.0, 7.6, 1.0), (27.5, 8.6, 0.5)], "browthick": 1.0,
          "mouth": {"wide": 41.0, "pitch": -26.0, "curve": 4.5, "thick": 0.50, "lip": 0.0}, "nostrils": [(7.0, -6.0, 1.5)]}
_LIZARDS = [dict(),
            dict(snout=(0.094, 41.0, -15.0, 31.0, -0.13), jaw=0.34),                                      # long and narrow
            dict(snout=(0.064, 51.0, -16.0, 34.0, -0.05), jaw=0.24, brow=0.0040),                         # short and broad
            dict(brow=0.0085, eye={"yaw": 31.0, "pitch": 8.0, "wide": 6.6, "high": 3.2, "tilt": 12.0}),   # heavy-browed, narrow-eyed
            dict(snout=(0.086, 49.0, -13.0, 36.0, -0.15), chin=0.005, jaw=0.26),                          # heavy-jawed
            dict(snout=(0.072, 44.0, -15.0, 31.0, -0.08), eye={"yaw": 32.0, "pitch": 10.0, "wide": 8.0, "high": 4.8, "tilt": 5.0}, brow=0.0035)]  # large-eyed


def lizard(n, sex):
    h = dict(LIZARD, throat=True)          # (the paler lower jaw and throat, with the body's plates: the user's Q75)
    for key, value in _LIZARDS[n - 1].items():
        h[key] = dict(LIZARD["eye"], **value) if key == "eye" else value
    if sex == "female":
        length, wide, middle, high, droop = h["snout"]
        h["snout"] = (length * 0.93, wide * 0.95, middle, high * 0.96, droop)
        h["eye"] = dict(h["eye"], high=h["eye"]["high"] * 1.10, wide=h["eye"]["wide"] * 1.04)
        h["brow"] = h["brow"] * 0.7
        h["jaw"] = h["jaw"] + 0.04
    return h


def half_beast(face):
    """A human face as a Half-Beast's (GM2 step 9e): no ears of its own (its beast ears are a part, parts/ears.py), a
    broader nose, eyes a little slanted."""
    h = dict(face)
    h["ear_none"] = True
    h["nose"] = (face["nose"][0] * 1.02, face["nose"][1] * 1.12)
    h["eye"] = dict(face["eye"], tilt=3.5)
    return h


def merfolk(face, fin=(1.22, 34.0)):
    """A human face as a Merfolk's (GM2 step 9d; T3: fin ears): fins for ears (forms.ear_fins), a small flat nose, large
    round eyes, smooth cheeks."""
    h = dict(face)
    h["ear_fin"] = fin
    h["nose"] = (face["nose"][0] * 0.84, face["nose"][1] * 0.94)
    h["nostril"] = face.get("nostril", 0.003) * 0.8
    h["eye"] = dict(face["eye"], high=face["eye"]["high"] * 1.07, iris=face["eye"]["iris"] * 1.06)
    h["brow"] = face["brow"] * 0.7
    return h


def goliathan(face, nose=(1.14, 1.20), brow=0.0020, chin=1.25):
    """A human face as a Goliath's (GM2 step 9e; the old `nose: "big"`): a heavy brow ridge, a big nose, a broad jaw and a
    strong chin, brows thick and level."""
    h = dict(face)
    h["nose"] = (face["nose"][0] * nose[0], face["nose"][1] * nose[1])
    h["nostril"] = face.get("nostril", 0.003) * 1.15
    h["jaw"] = max(0.18, face["jaw"] - 0.07)
    h["brow"] = face["brow"] + brow
    h["chin"] = face["chin"] * chin
    h["browthick"] = face["browthick"] * 1.15
    return h


for _sex in ("male", "female"):
    for _n in range(1, 7):
        FACES[f"lizardman-{_sex}-{_n}"] = lizard(_n, _sex)
        FACES[f"half-beast-{_sex}-{_n}"] = half_beast(FACES[f"human-{_sex}-{_n}"])
        FACES[f"merfolk-{_sex}-{_n}"] = merfolk(FACES[f"human-{_sex}-{_n}"])
        FACES[f"goliath-{_sex}-{_n}"] = goliathan(FACES[f"human-{_sex}-{_n}"], **({} if _sex == "male" else dict(nose=(1.08, 1.10), brow=0.0010, chin=1.1)))
        FACES[f"dwarf-{_sex}-{_n}"] = dwarven(FACES[f"human-{_sex}-{_n}"], **({} if _sex == "male" else dict(nose=(1.13, 1.16), nostril=1.15, brow=0.0007)))
        FACES[f"hobbit-{_sex}-{_n}"] = hobbitish(FACES[f"human-{_sex}-{_n}"])
        FACES[f"elf-{_sex}-{_n}"] = elven(FACES[f"human-{_sex}-{_n}"])
        # the dark elves': sharper: longer ears swept further back, narrower eyes more raised, the brows drawn down inward
        FACES[f"dark-elf-{_sex}-{_n}"] = elven(FACES[f"human-{_sex}-{_n}"], ear=(0.056, 0.30, 0.60, 0.74), tilt=9.5, narrow=0.84, frown=1.0)



def with_face(body, face, level):
    """The body's parameters at the detail `level` with the face's features on its head frame."""
    if face not in FACES:
        raise ValueError(f"no face {face} in parts/face.py")
    p = bodies.params(body, level)
    p["head"] = dict(p["head"], **FACES[face])
    return p


def face_model(path, face, body, level):
    """The face `face` made on `body` (manifest.json: "parts.face:face_model", args [face, body], its level): the head and
    its flat pieces bound to `head` and joined, with the body's skeleton."""
    p = with_face(body, face, level)
    rig = r.build_rig(p, forms.joints_of(p))
    parts, _ = forms.build_head(p, materials.make())
    for part in parts:
        r.bind_rigid(rig, part, "head")
    k.join_by_ink(parts, f"{body}-{face}")
    bodies.export_sized(path, p, export_skins=True)

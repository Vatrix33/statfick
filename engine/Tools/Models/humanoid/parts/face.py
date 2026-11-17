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
}


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
    export(path, export_skins=True)

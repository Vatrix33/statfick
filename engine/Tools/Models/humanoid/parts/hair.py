# The hair styles (Q3: sculpted masses; Q19, Q40: about 16–24 per sex): a style is a cap over the skull and locks, each
# lock a tube through points given on the head (yaw and pitch in degrees, parts of the head's radius out), its radius and
# flatness; fitted to a head (forms.build_hair on the shape of a body's head with a face's features: the jaw and the
# face's flatness shape it too) and bound as a whole to the skeleton's `head` bone, a part worn as a garment (GM2-P8).
# Besides its locks a style gives the cap's lift (cap), the hairline over the brow and at the nape (brow, nape) and how
# much a lock thins toward its end (taper). The two sample styles of the first humanoid (the user's approval of the
# refined pair, 2026-11-16).
import forms
import kit as k
import materials
import rig as r
from make_models import export
from parts.face import with_face


def swept_back():
    """Short hair brushed up and back from the brow with a sweep to the figure's left, close at the sides and the nape
    (the human man's)."""
    locks = []
    for yaw, size in ((-40, 0.026), (-21, 0.031), (0, 0.034), (22, 0.030), (41, 0.025)):
        rise = 1.12 + 0.04 * (1 - abs(yaw) / 44)
        sweep = 9
        locks.append(([(yaw, 43, 1.045), (yaw * 0.95 + sweep * 0.4, 56, rise), (yaw * 0.85 + sweep, 73, rise + 0.012), (yaw * 0.5 + sweep * 1.4, 88, 1.11),
                       (180 - yaw * 0.6 - sweep, 64, 1.08)], size, 0.42))
    for side in (-1, 1):
        locks.append(([(side * 60, 31, 1.045), (side * 76, 38, 1.085), (side * 100, 40, 1.085), (side * 128, 34, 1.07)], 0.024, 0.40))
        locks.append(([(side * 70, 14, 1.045), (side * 85, 18, 1.07), (side * 106, 16, 1.07), (side * 132, 8, 1.06)], 0.021, 0.40))
    for yaw, size in ((146, 0.024), (164, 0.027), (182, 0.028), (200, 0.026), (216, 0.023)):
        locks.append(([(yaw, 44, 1.08), (yaw, 16, 1.085), (yaw, -12, 1.07), (yaw + 2, -34, 1.045)], size, 0.42))
    return {"locks": locks, "cap": 1.045, "brow": 0.50, "nape": -0.58, "taper": 0.66}


def long_tied():
    """Long hair parted in the middle, drawn back over the ears and tied low; a tail between the shoulder blades (the
    human woman's)."""
    locks = []
    for side in (-1, 1):
        for i, (start, drop) in enumerate(((7, 0), (20, 4), (33, 9), (46, 15))):
            locks.append(([(side * start, 46 - i * 2, 1.045), (side * (start + 14), 50 - drop, 1.10), (side * (start + 44), 34 - drop * 2, 1.10),
                           (side * 128, 6 - drop, 1.09), (side * 162, -26, 1.10)], 0.025, 0.46))
        locks.append(([(side * 66, 22, 1.04), (side * 82, 14, 1.07), (side * 108, 0, 1.075), (side * 140, -20, 1.08), (side * 166, -32, 1.09)], 0.020, 0.46))
    for yaw in (150, 165, 180, 195, 210):
        locks.append(([(yaw, 58, 1.09), (yaw, 26, 1.10), (180 + (yaw - 180) * 0.6, -8, 1.10), (180 + (yaw - 180) * 0.25, -30, 1.10)], 0.026, 0.46))
    locks.append(([(180, -30, 1.13), (180, -58, 1.36), (180, -74, 1.90), (180, -81, 2.60), (180, -84.5, 3.30)], 0.036, 0.60))
    locks.append(([(173, -34, 1.12), (170, -62, 1.40), (172, -77, 2.05), (176, -83, 2.95)], 0.024, 0.60))
    locks.append(([(187, -34, 1.12), (190, -62, 1.40), (188, -77, 2.05), (184, -83, 2.95)], 0.024, 0.60))
    return {"locks": locks, "cap": 1.045, "brow": 0.48, "nape": -0.66, "taper": 0.62}


STYLES = {"swept-back": swept_back, "long-tied": long_tied}


def hair_model(path, style, body, face, level):
    """The style `style` fitted to `body`'s head with the face `face` (manifest.json: "parts.hair:hair_model", args [style,
    body, face], its level): its pieces bound to `head` and joined, with the body's skeleton."""
    if style not in STYLES:
        raise ValueError(f"no hair style {style} in parts/hair.py")
    p = with_face(body, face, level)
    rig = r.build_rig(p, forms.joints_of(p))
    parts = forms.build_hair(p, forms.head_shape(p["head"]), materials.make()["hair"], STYLES[style]())
    for part in parts:
        r.bind_rigid(rig, part, "head")
    k.join_by_ink(parts, f"{body}-{style}")
    export(path, export_skins=True)

# The beards (Q19, Q40: about twelve; the user's list of 2026-11-18, Q61): a beard is a shell on the jaw, cut by its region
# (how many degrees a direction from the head's centre lies inside it: yaw from the face's front, pitch up) and rising
# from the skin at its edge to its thickness, with locks for what hangs from the chin; built by forms.build_beard and
# bound as a whole to the skeleton's `head` bone, a part worn as a garment (GM2-P8).
# One beard fits every face of a race and sex: it is made to the widest jaw, the fullest chin and the highest cheekbones
# among them (`made_to`), so on a narrower face it is only thicker, and its edge on the cheek, where the faces differ by
# two millimetres, lies on the skin.
import bodies
import forms
import kit as k
import materials
import rig as r
from make_models import export
from parts.face import FACES, with_face
from parts.hair import braid, through, xyz


def at(points, x):
    """The value at `x` of the bent line through `points` ((x, y), rising in x)."""
    if x <= points[0][0]:
        return points[0][1]
    for (x0, y0), (x1, y1) in zip(points, points[1:]):
        if x <= x1:
            return y0 + (y1 - y0) * (x - x0) / (x1 - x0)
    return points[-1][1]


CHEEK = [(0, -36.6), (9.5, -36.0), (13.5, -29.2), (22, -22.5), (40, -14.5), (60, -6.5), (76, 2.0), (86, 10.0)]     # a full beard's upper edge
UNDER = [(0, -68.0), (50, -62.0), (80, -48.0), (96, -30.0)]                                                        # how far it goes under the jaw
JAW = [(0, -53.0), (30, -47.0), (60, -35.0), (80, -19.0), (90, -5.0)]                                              # the jaw's own edge


def cheeks(yaw, pitch):
    a = abs(yaw)
    return min(at(CHEEK, a) - pitch, pitch - at(UNDER, a), 96.0 - a)


def moustache(yaw, pitch, wide=15.5, droop=0.0):
    a = abs(yaw)
    return min(wide - a, pitch + 28.7 + droop * max(0.0, a - 11.0), -21.2 - pitch)


def full(yaw, pitch):
    return max(cheeks(yaw, pitch), moustache(yaw, pitch))


def chin_patch(yaw, pitch, wide=13.0, low=-62.0):
    return min(wide - abs(yaw), -35.9 - pitch, pitch - low)


def chin_drop(places, size, count=9, flat=0.62):
    """A lock hanging from the chin through `places` (metres from the head's middle)."""
    return (through(places, count), size, flat)


def hanging(reach=-0.32, wide=0.0):
    """The places a long beard passes from the chin down before the chest (clear of it), `wide` metres to the left."""
    return [(wide * 0.3, -0.066, -0.088), (wide * 0.6, -0.104, -0.150), (wide * 0.9, -0.150, -0.232), (wide, -0.176, reach)]


def stubble():
    """A few days' growth on the lip, the cheeks and the jaw: short flecks on the skin (4.2 degrees apart, 1.1 long and
    0.42 wide each way)."""
    return {"region": full, "flecks": (4.2, 1.1, 0.42)}


def short_trimmed():
    """A short beard kept trimmed close to the jaw."""
    return {"region": full, "lift": 0.0046, "rise": 5.0, "anchors": [(0, -46)]}


def full_beard():
    """A full beard: thick on the cheeks and the jaw, rounded under the chin."""
    locks = [chin_drop([(0.0, -0.062, -0.084), (0.0, -0.082, -0.120), (0.0, -0.084, -0.146)], 0.038, count=5, flat=0.70)]
    for side in (-1, 1):
        locks.append(chin_drop([(side * 0.028, -0.052, -0.080), (side * 0.034, -0.066, -0.116), (side * 0.028, -0.068, -0.138)], 0.030, count=5, flat=0.70))
        locks.append(chin_drop([(side * 0.050, -0.026, -0.072), (side * 0.054, -0.034, -0.104), (side * 0.046, -0.038, -0.124)], 0.025, count=5, flat=0.70))
    return {"region": full, "lift": 0.012, "rise": 8.0, "anchors": [(0, -46)], "locks": locks, "taper": 0.70}


def goatee():
    """A beard on the chin alone, to a short point."""
    return {"region": chin_patch, "lift": 0.008, "rise": 5.0, "anchors": [(0, -47)],
            "locks": [chin_drop([(0.0, -0.066, -0.086), (0.0, -0.084, -0.118), (0.0, -0.088, -0.148)], 0.026, count=5)], "taper": 0.85}


def moustache_alone():
    """A full moustache over the lip, its ends drooping past the mouth's corners."""
    return {"region": lambda yaw, pitch: moustache(yaw, pitch, wide=17.5, droop=0.45), "lift": 0.0085, "rise": 2.6, "anchors": [(0, -24.8), (-12, -26.0), (12, -26.0)]}


def moustache_tuft():
    """A moustache and a small pointed tuft on the chin."""
    return {"region": lambda yaw, pitch: max(moustache(yaw, pitch, wide=16.5, droop=0.4), chin_patch(yaw, pitch, wide=7.5, low=-54.0)), "lift": 0.008, "rise": 2.6,
            "anchors": [(0, -24.8), (-12, -26.0), (12, -26.0), (0, -44)],
            "locks": [chin_drop([(0.0, -0.068, -0.086), (0.0, -0.084, -0.114), (0.0, -0.088, -0.140)], 0.018, count=5)], "taper": 0.90}


def mutton_chops():
    """Broad whiskers from the ears down the cheeks to the jaw, the chin bare."""
    return {"region": lambda yaw, pitch: min(cheeks(yaw, pitch), abs(yaw) - 26.0, pitch + 58.0), "lift": 0.011, "rise": 7.0, "anchors": [(-60, -22), (60, -22)]}


def chin_strap():
    """A narrow beard along the jaw's edge from ear to ear."""
    return {"region": lambda yaw, pitch: min(7.0 - abs(pitch - at(JAW, abs(yaw))), 93.0 - abs(yaw)), "lift": 0.005, "rise": 3.5,
            "anchors": [(yaw, at(JAW, abs(yaw))) for yaw in (-80, -55, -28, 0, 28, 55, 80)]}


def long_beard():
    """A full beard grown long: it hangs before the chest to the breastbone."""
    locks = [chin_drop(hanging(-0.325), 0.046, flat=0.62)]
    for side in (-1, 1):
        locks.append(chin_drop([(side * 0.026, -0.054, -0.082)] + hanging(-0.295, side * 0.040)[1:], 0.036, flat=0.62))
        locks.append(chin_drop([(side * 0.048, -0.030, -0.074), (side * 0.060, -0.072, -0.140), (side * 0.058, -0.118, -0.215)], 0.030, count=7, flat=0.62))
    return {"region": full, "lift": 0.012, "rise": 8.0, "anchors": [(0, -46)], "locks": locks, "taper": 0.62}


def braided_beard():
    """A full beard plaited into one thick braid from the chin."""
    places = hanging(-0.330)
    locks = [braid(through(places, 15), 0.034, flat=0.80), (through([places[-1], (0.0, -0.180, -0.372)], 3), 0.024, 0.75)]
    for side in (-1, 1):
        locks.append(chin_drop([(side * 0.030, -0.050, -0.080), (side * 0.024, -0.074, -0.118), (side * 0.010, -0.092, -0.146)], 0.030, count=5, flat=0.70))
    return {"region": full, "lift": 0.011, "rise": 8.0, "anchors": [(0, -46)], "locks": locks, "taper": 0.0}


def forked_beard():
    """A full beard parted under the chin into two long points."""
    locks = []
    for side in (-1, 1):
        locks.append(chin_drop([(side * 0.010, -0.064, -0.086), (side * 0.028, -0.100, -0.150), (side * 0.046, -0.142, -0.225), (side * 0.054, -0.164, -0.290)], 0.036, flat=0.66))
        locks.append(chin_drop([(side * 0.044, -0.034, -0.076), (side * 0.052, -0.060, -0.124), (side * 0.050, -0.090, -0.170)], 0.028, count=6, flat=0.66))
    return {"region": full, "lift": 0.012, "rise": 8.0, "anchors": [(0, -46)], "locks": locks, "taper": 0.85}


def twin_braids():
    """A full beard with two braids hanging from the chin."""
    locks = []
    for side in (-1, 1):
        places = [(side * 0.016, -0.062, -0.086), (side * 0.028, -0.104, -0.156), (side * 0.034, -0.150, -0.240), (side * 0.036, -0.172, -0.305)]
        locks.append(braid(through(places, 15), 0.026, flat=0.80))
        locks.append((through([places[-1], (side * 0.036, -0.176, -0.345)], 3), 0.019, 0.75))
    locks.append(chin_drop([(0.0, -0.062, -0.084), (0.0, -0.080, -0.116), (0.0, -0.082, -0.138)], 0.032, count=5, flat=0.70))
    return {"region": full, "lift": 0.011, "rise": 8.0, "anchors": [(0, -46)], "locks": locks, "taper": 0.0}


STYLES = {"stubble": stubble, "full": full_beard, "braided": braided_beard, "forked": forked_beard, "long": long_beard, "goatee": goatee,
          "moustache": moustache_alone, "moustache-tuft": moustache_tuft, "mutton-chops": mutton_chops, "short-trimmed": short_trimmed, "chin-strap": chin_strap,
          "twin-braids": twin_braids}


def made_to(body, face, level):
    """The body's parameters with the face a beard is made to: `face`'s features, and of every face of its race and sex
    the least narrowing of the jaw, the fullest chin and the highest cheekbones."""
    p = with_face(body, face, level)
    stem = face.rsplit("-", 1)[0] + "-"
    others = [f for name, f in FACES.items() if name.startswith(stem)]
    p["head"] = dict(p["head"], jaw=min(f["jaw"] for f in others), chin=max(f["chin"] for f in others), cheek=max(f["cheek"] for f in others))
    return p


def beard_model(path, style, body, face, level):
    """The beard `style` on `body`'s head (manifest.json: "parts.beard:beard_model", args [style, body, the race's and
    sex's first face], its level): bound to `head` and joined, with the body's skeleton."""
    if style not in STYLES:
        raise ValueError(f"no beard {style} in parts/beard.py")
    p = made_to(body, face, level)
    rig = r.build_rig(p, forms.joints_of(p))
    parts = forms.build_beard(p, materials.make(), STYLES[style]())
    for part in parts:
        r.bind_rigid(rig, part, "head")
    k.join_by_ink(parts, f"{body}-beard-{style}")
    bodies.export_sized(path, p, export_skins=True)

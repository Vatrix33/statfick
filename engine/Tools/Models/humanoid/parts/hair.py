# The hair styles (Q3: sculpted masses; Q19, Q40: about 16–24 per sex): a style is a cap over the skull and locks, each
# lock a tube through points given on the head (yaw and pitch in degrees, parts of the head's radius out), its radius and
# flatness; fitted to a head (forms.build_hair on the shape of a body's head with a face's features: the jaw and the
# face's flatness shape it too) and bound as a whole to the skeleton's `head` bone, a part worn as a garment (GM2-P8).
# Besides its locks a style gives the cap's lift (cap), the hairline over the brow and at the nape (brow, nape) and how
# much a lock thins toward its end (taper). The two sample styles of the first humanoid (the user's approval of the
# refined pair, 2026-11-16).
import bodies
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


# ---- the shared set's styles (GM2 step 9a; the user's lists of 2026-11-18, Q61: twenty per sex) ---------------------------
import math


def xyz(left, back, up):
    """A lock's point given in metres from the head's centre on the human man's head (to its left, to its back, up), as
    the (yaw, pitch, out) a lock takes: for hair that hangs clear of the skull. Another head takes it in its own size.
    The head's shape is forms.head_shape's (narrower low at the back and at the jaw), so the direction that leads to the
    place is found by steps."""
    sx, sy, sz, jaw = 0.079, 0.098, 0.121, 0.36
    target = (left, back, up)                       # Blender's axes on the head: +x its left, +y its back (it faces -y)
    length = math.sqrt(sum(c * c for c in target))

    def shape(d):
        x, y, z = d
        low, high, behind = max(0.0, -z), max(0.0, z), max(0.0, y)
        wx = (1.0 - jaw * low ** 1.3) * (1.0 - 0.06 * high)
        wy = (0.90 + 0.05 * low) if y < 0 else (1.0 - 0.45 * low ** 1.2)
        wz = 1.0 - 0.24 * behind if z < 0 else 1.0
        n = 2.8 if y < 0 else 2.15
        r = math.hypot(x, y)
        if r > 1e-6:
            boost = 1.0 / (((abs(x) / r) ** n + (abs(y) / r) ** n) ** (1.0 / n))
            x, y = x * boost, y * boost
        return (x * sx * wx, y * sy * wy, z * sz * wz)

    d = [c / length for c in target]
    for _ in range(40):
        s = shape(d)
        size = math.sqrt(sum(c * c for c in s))
        want = [c / length for c in target]
        have = [c / size for c in s]
        d = [a + 0.7 * (w - h) for a, w, h in zip(d, want, have)]
        norm = math.sqrt(sum(c * c for c in d))
        d = [c / norm for c in d]
    s = shape(d)
    return (math.degrees(math.atan2(d[0], -d[1])), math.degrees(math.asin(max(-1.0, min(1.0, d[2])))), length / math.sqrt(sum(c * c for c in s)))


def down(yaw, top, end, bulge=1.09, end_out=1.06, start=1.05, sway=0.0, spread=0.55, steps=5):
    """A lock's points from near the crown down the head at `yaw`: from the pitch `top` to `end`, standing `bulge` off the
    skull half way and `end_out` at its end, drifting `sway` degrees round the head on the way."""
    points = []
    for i in range(steps):
        t = i / (steps - 1)
        out = start * (1 - t) * (1 - 2 * t) + bulge * 4 * t * (1 - t) + end_out * t * (2 * t - 1)
        points.append((yaw * (spread + (1 - spread) * min(1.0, t * 1.6)) + sway * t * t, top + (end - top) * t, out))
    return points


def curtain(locks, low=-0.30, top=72, size=0.040, flare=1.0, back=0.0, across=(-0.066, -0.033, 0.0, 0.033, 0.066)):
    """Long hair down the back as broad flat locks that overlap (narrow ones left gaps the dark under them showed through):
    from the crown over the back of the head and down to `low` metres under the head's middle."""
    for left in across:
        yaw = 180 - left * 600
        locks.append(([(yaw, top, 1.07), (yaw, 38, 1.095), (yaw, 6, 1.10), xyz(left * flare, 0.104 + back, -0.070), xyz(left * flare * 1.06, 0.118 + back, (low - 0.07) * 0.55 - 0.03),
                       xyz(left * flare * 1.10, 0.124 + back, low)], size, 0.40))


def sides_short(locks, out=1.065, size=0.020):
    """Short hair lying back along the sides, over and behind the ears."""
    for side in (-1, 1):
        locks.append(([(side * 58, 31, 1.045), (side * 78, 36, out + 0.01), (side * 102, 36, out + 0.01), (side * 126, 30, out)], size + 0.003, 0.40))
        locks.append(([(side * 68, 13, 1.045), (side * 86, 17, out), (side * 108, 14, out), (side * 130, 6, out - 0.005)], size, 0.40))


def back_short(locks, low=-26, out=1.075, size=0.023):
    """Short hair lying down the back of the head to the nape."""
    for yaw, scale in ((146, 0.9), (163, 1.0), (180, 1.05), (197, 1.0), (214, 0.9)):
        locks.append(([(yaw, 50, out - 0.005), (yaw, 22, out), (yaw, -6, out - 0.01), (yaw + 1, low, 1.045)], size * scale, 0.42))


def crop():
    """Short all over: lying forward on top with a small lift at the brow, close at the sides and the nape."""
    locks = []
    for yaw, size in ((-40, 0.019), (-20, 0.021), (0, 0.022), (20, 0.021), (40, 0.019)):
        locks.append(([(yaw * 0.5 + 180, 76, 1.045), (yaw * 0.6, 84, 1.055), (yaw * 0.9, 64, 1.06), (yaw, 49, 1.065), (yaw, 40, 1.05)], size, 0.40))
    for yaw in (-58, 58):
        locks.append(([(yaw * 2.2, 60, 1.045), (yaw * 1.5, 66, 1.055), (yaw, 54, 1.06), (yaw * 0.95, 40, 1.048)], 0.019, 0.40))
    sides_short(locks, out=1.055, size=0.018)
    back_short(locks, out=1.06, size=0.021)
    return {"locks": locks, "cap": 1.04, "brow": 0.52, "nape": -0.52, "taper": 0.45}


def spiky():
    """Short spikes standing up and out all over the top, the front ones leaning forward; close at the sides and the nape."""
    locks = []
    rows = ((44, (-38, -14, 12, 36), 1.26, -14), (58, (-52, -26, 0, 26, 52), 1.34, -7), (74, (-72, -30, 14, 62), 1.37, 0), (84, (180,), 1.36, 0),
            (66, (128, 160, 200, 232), 1.32, 6), (44, (150, 180, 210), 1.24, 10))
    for pitch, yaws, tall, lean in rows:
        for n, yaw in enumerate(yaws):
            twist = 5 if n % 2 else -4
            locks.append(([(yaw, pitch, 1.03), (yaw + twist * 0.5, pitch + lean * 0.5, 1.0 + (tall - 1.0) * 0.6), (yaw + twist, pitch + lean, tall)], 0.028, 0.60))
    sides_short(locks, out=1.06, size=0.019)
    back_short(locks, low=-22, out=1.065)
    return {"locks": locks, "cap": 1.045, "brow": 0.52, "nape": -0.50, "taper": 0.92}


def curly():
    """Short tight curls: a rounded mass of small coils over the whole head."""
    locks = []
    rings = ((80, (0, 90, 180, 270)), (60, (-65, -32, 0, 32, 65, 115, 150, 180, 210, 245)), (41, (-52, -26, 0, 26, 52, 80, 108, 136, 164, 192, 220, 248, 276)),
             (18, (72, 98, 124, 150, 176, 202, 228, 254, 282)), (-6, (128, 152, 176, 200, 226)))
    for index, (pitch, yaws) in enumerate(rings):
        for n, yaw in enumerate(yaws):
            turn = 1 if (index + n) % 2 else -1
            locks.append(([(yaw - 6 * turn, pitch - 4, 1.05), (yaw + 3 * turn, pitch + 5, 1.14), (yaw + 9 * turn, pitch, 1.135), (yaw + 4 * turn, pitch - 7, 1.07)], 0.023, 0.80))
    return {"locks": locks, "cap": 1.06, "brow": 0.50, "nape": -0.46, "taper": 0.30}


def messy():
    """Tousled: uneven tufts going every way on top, a few fallen over the brow, ragged at the sides and the nape."""
    locks = []
    top = ((-30, 44, 14, 1.13), (-8, 46, -10, 1.10), (16, 45, 12, 1.14), (38, 46, -8, 1.10), (-46, 60, -16, 1.15), (-14, 64, 18, 1.17), (20, 62, -14, 1.15),
           (48, 60, 14, 1.13), (-60, 78, 20, 1.14), (10, 82, -22, 1.16), (70, 76, 12, 1.13), (150, 66, -14, 1.14), (200, 68, 16, 1.15), (176, 48, 10, 1.12))
    for yaw, pitch, sway, lift in top:
        locks.append(([(yaw - sway * 0.6, pitch + 9, 1.05), (yaw, pitch + 3, lift), (yaw + sway * 0.7, pitch - 5, lift - 0.01), (yaw + sway, pitch - 11, 1.07)], 0.024, 0.50))
    for yaw, end in ((-22, 27), (-4, 24), (15, 29)):                # fallen over the brow
        locks.append(([(yaw * 0.6, 60, 1.06), (yaw * 0.9, 46, 1.10), (yaw, 34, 1.085), (yaw + 3, end, 1.05)], 0.021, 0.45))
    for side in (-1, 1):
        locks.append(([(side * 56, 34, 1.045), (side * 74, 36, 1.09), (side * 96, 28, 1.095), (side * 112, 14, 1.07)], 0.023, 0.45))
        locks.append(([(side * 70, 16, 1.045), (side * 86, 14, 1.075), (side * 104, 4, 1.075), (side * 118, -10, 1.06)], 0.020, 0.45))
    for yaw, low in ((142, -24), (160, -32), (180, -27), (200, -33), (218, -23)):
        locks.append(([(yaw, 46, 1.07), (yaw + 4, 20, 1.09), (yaw - 3, -6, 1.08), (yaw + 2, low, 1.06)], 0.023, 0.45))
    return {"locks": locks, "cap": 1.05, "brow": 0.50, "nape": -0.52, "taper": 0.72}


def shaggy():
    """Unkempt, to the jaw: a ragged fringe over the brow, thick uneven locks over the ears and down the neck."""
    locks = []
    for yaw, end, sway in ((-30, 24, -4), (-12, 21, 3), (6, 25, -3), (24, 22, 5)):
        locks.append((down(yaw, 70, end, bulge=1.10, end_out=1.06, sway=sway, steps=4), 0.024, 0.45))
    for side in (-1, 1):
        for yaw, end, out in ((48, -18, 1.10), (68, -34, 1.16), (90, -42, 1.22), (112, -40, 1.20), (134, -44, 1.20)):
            locks.append((down(side * yaw, 74, end, bulge=1.115, end_out=out, sway=side * 5), 0.026, 0.46))
    for yaw, end in ((156, -46), (180, -50), (204, -45)):
        locks.append((down(yaw, 76, end, bulge=1.11, end_out=1.20, spread=1.0), 0.028, 0.46))
    return {"locks": locks, "cap": 1.05, "brow": 0.48, "nape": -0.66, "taper": 0.66}


def buzz_cut():
    """Shaved close: the hairline alone, a shade over the skull."""
    return {"locks": [], "cap": 1.018, "brow": 0.54, "nape": -0.50, "taper": 0.5}


def long_loose():
    """Long and loose past the shoulders, parted in the middle: two locks frame the face, the rest falls behind the ears
    and down the back (the man's)."""
    locks = []
    for side in (-1, 1):
        locks.append(([(side * 6, 62, 1.05), (side * 30, 46, 1.085), (side * 52, 18, 1.095), xyz(side * 0.092, -0.040, -0.075), xyz(side * 0.100, -0.034, -0.175),
                       xyz(side * 0.104, -0.026, -0.255)], 0.024, 0.46))
        locks.append(([(side * 8, 76, 1.05), (side * 44, 58, 1.09), (side * 78, 30, 1.10), xyz(side * 0.100, 0.020, -0.040), xyz(side * 0.110, 0.040, -0.150),
                       xyz(side * 0.116, 0.052, -0.270)], 0.027, 0.46))
        locks.append(([(side * 10, 86, 1.05), (side * 70, 66, 1.09), (side * 112, 36, 1.10), xyz(side * 0.082, 0.078, -0.040), xyz(side * 0.094, 0.094, -0.160),
                       xyz(side * 0.098, 0.104, -0.290)], 0.028, 0.46))
    curtain(locks, low=-0.31, size=0.046)
    return {"locks": locks, "cap": 1.045, "brow": 0.48, "nape": -0.66, "taper": 0.22}


def ponytail():
    """Brushed straight back from the brow and tied at the nape, a short tail down the neck (the man's)."""
    locks = []
    for yaw, size in ((-42, 0.023), (-21, 0.026), (0, 0.027), (21, 0.026), (42, 0.023)):
        locks.append(([(yaw, 42, 1.045), (yaw * 0.95, 58, 1.075), (yaw * 0.8, 80, 1.08), (180 - yaw * 0.7, 52, 1.08), (180 - yaw * 0.4, 10, 1.085), (180, -22, 1.09)], size, 0.42))
    for side in (-1, 1):
        locks.append(([(side * 60, 30, 1.045), (side * 86, 30, 1.075), (side * 124, 12, 1.08), (side * 160, -14, 1.085), (180, -24, 1.09)], 0.023, 0.42))
        locks.append(([(side * 70, 12, 1.045), (side * 96, 8, 1.065), (side * 130, -8, 1.07), (side * 162, -22, 1.08)], 0.020, 0.42))
    locks.append(([(180, -21, 1.13), (180, -28, 1.22)], 0.037, 0.95, "tie"))
    locks.append(([xyz(0, 0.104, -0.060), xyz(0, 0.122, -0.110), xyz(0.004, 0.126, -0.180), xyz(0.006, 0.120, -0.245)], 0.030, 0.62))
    locks.append(([xyz(0.012, 0.104, -0.064), xyz(0.020, 0.116, -0.120), xyz(0.022, 0.118, -0.200)], 0.020, 0.60))
    locks.append(([xyz(-0.012, 0.104, -0.064), xyz(-0.020, 0.116, -0.120), xyz(-0.020, 0.116, -0.190)], 0.020, 0.60))
    return {"locks": locks, "cap": 1.045, "brow": 0.50, "nape": -0.62, "taper": 0.60}


def topknot():
    """The hair drawn up from the brow, the sides and the nape into a knot on the crown."""
    locks = []
    for yaw, size in ((-44, 0.022), (-22, 0.024), (0, 0.025), (22, 0.024), (44, 0.022)):
        locks.append(([(yaw, 42, 1.045), (yaw * 0.9, 56, 1.07), (yaw * 0.6, 72, 1.075), (yaw * 0.3 + (180 if yaw == 0 else 0) * 0, 84, 1.08), (180, 82, 1.10)], size, 0.40))
    for side in (-1, 1):
        for yaw, start in ((66, 24), (92, 10), (120, -4)):
            locks.append(([(side * yaw, start, 1.04), (side * (yaw + 4), start + 26, 1.065), (side * (yaw + 16), start + 50, 1.07), (180, 80, 1.10)], 0.022, 0.40))
    for yaw in (150, 180, 210):
        locks.append(([(yaw, -22, 1.04), (yaw, 10, 1.065), (yaw, 44, 1.07), (180, 74, 1.10)], 0.023, 0.40))
    # the knot: a coil standing on the crown, and its tied end
    locks.append(([(160, 72, 1.10), (172, 84, 1.27), (196, 82, 1.34), (206, 68, 1.26), (188, 62, 1.14)], 0.034, 0.95))
    locks.append(([(184, 74, 1.30), (200, 60, 1.40), (206, 48, 1.36)], 0.020, 0.70))
    return {"locks": locks, "cap": 1.04, "brow": 0.52, "nape": -0.56, "taper": 0.25}


def side_part():
    """Neat, parted on the figure's left and combed across the top to the right; short at the sides and the nape."""
    locks = []
    for pitch, size in ((45, 0.022), (56, 0.024), (68, 0.025), (80, 0.024)):
        locks.append(([(34, pitch, 1.045), (14, pitch + 9, 1.08), (-16, pitch + 7, 1.085), (-46, pitch - 6, 1.075), (-64, pitch - 22, 1.05)], size, 0.42))
        locks.append(([(38, pitch, 1.045), (54, pitch - 8, 1.07), (70, pitch - 22, 1.06), (80, pitch - 36, 1.045)], size * 0.85, 0.42))
    locks.append(([(34, 88, 1.05), (-30, 84, 1.075), (-110, 70, 1.075), (-140, 50, 1.06)], 0.024, 0.42))
    sides_short(locks, out=1.058, size=0.019)
    back_short(locks, out=1.065)
    return {"locks": locks, "cap": 1.042, "brow": 0.52, "nape": -0.54, "taper": 0.55}


def braid(points, radius, plaits=None, flat=0.80):
    """A braid along `points` (give two points to a plait): a lock that swells and narrows plait by plait and thins a
    little toward its end."""
    count = len(points)
    shares = [(1.18 if i % 2 else 0.58) * (1.0 - 0.22 * i / (count - 1)) for i in range(count)]
    shares[0], shares[-1] = 0.75, 0.45
    return (points, radius, flat, shares)


def line(a, b, count):
    """`count` points in a straight line from the place `a` to `b`, each in metres from the head's middle (xyz)."""
    return [xyz(*[p + (q - p) * i / (count - 1) for p, q in zip(a, b)]) for i in range(count)]


def along(a, b, count, wobble=0.0):
    """`count` points from the lock point `a` to `b` in a straight run of (yaw, pitch, out), swaying `wobble` degrees."""
    return [(a[0] + (b[0] - a[0]) * i / (count - 1) + wobble * (1 if i % 2 else -1), a[1] + (b[1] - a[1]) * i / (count - 1),
             a[2] + (b[2] - a[2]) * i / (count - 1)) for i in range(count)]


def fringe():
    """Short, combed forward from the crown into a straight fringe over the brow; close at the sides and the nape."""
    locks = []
    for yaw, size in ((-46, 0.021), (-28, 0.023), (-9, 0.024), (9, 0.024), (28, 0.023), (46, 0.021)):
        locks.append(([(yaw * 0.4 + 180, 78, 1.05), (yaw * 0.5, 84, 1.065), (yaw * 0.85, 62, 1.075), (yaw, 42, 1.075), (yaw, 27, 1.055)], size, 0.46))
    sides_short(locks, out=1.058, size=0.019)
    back_short(locks, out=1.065)
    return {"locks": locks, "cap": 1.042, "brow": 0.50, "nape": -0.54, "taper": 0.30}


def slicked_back():
    """Smoothed flat to the skull, straight back from the brow and the temples to the nape."""
    locks = []
    for yaw, size in ((-44, 0.022), (-22, 0.024), (0, 0.025), (22, 0.024), (44, 0.022)):
        locks.append(([(yaw, 41, 1.04), (yaw * 0.95, 60, 1.058), (yaw * 0.8, 82, 1.06), (180 - yaw * 0.75, 54, 1.06), (180 - yaw * 0.7, 14, 1.062), (180 - yaw * 0.6, -26, 1.05)],
                      size, 0.36))
    for side in (-1, 1):
        locks.append(([(side * 60, 30, 1.04), (side * 84, 32, 1.058), (side * 118, 20, 1.06), (side * 146, -4, 1.06), (side * 158, -28, 1.048)], 0.022, 0.36))
        locks.append(([(side * 70, 12, 1.04), (side * 94, 10, 1.052), (side * 124, -4, 1.055), (side * 146, -24, 1.045)], 0.019, 0.36))
    return {"locks": locks, "cap": 1.038, "brow": 0.54, "nape": -0.56, "taper": 0.45}


def undercut():
    """The sides and the back shaved close, the top left long and thrown back and over to the figure's right."""
    locks = []
    for yaw, size, lift in ((-26, 0.026, 1.12), (-8, 0.030, 1.15), (10, 0.031, 1.16), (28, 0.027, 1.13)):
        locks.append(([(yaw, 43, 1.04), (yaw - 4, 56, lift), (yaw - 12, 74, lift + 0.01), (yaw * 0.5 - 30 + 180, 80, lift - 0.02), (180 - 24, 56, 1.09), (180 - 26, 40, 1.05)],
                      size, 0.50))
    locks.append(([(34, 50, 1.035), (30, 66, 1.08), (40, 82, 1.10), (150, 74, 1.08), (168, 52, 1.05)], 0.022, 0.46))
    locks.append(([(-36, 46, 1.035), (-52, 58, 1.09), (-74, 68, 1.10), (-128, 66, 1.08), (-150, 50, 1.05)], 0.024, 0.46))
    return {"locks": locks, "cap": 1.018, "brow": 0.54, "nape": -0.50, "taper": 0.50}


def mohawk():
    """A crest standing along the middle of the head from the brow to the nape, the sides shaved close."""
    locks = []
    crest = ((0, 44, 1.28, -10), (0, 58, 1.36, -6), (0, 73, 1.42, -2), (0, 88, 1.44, 0), (180, 76, 1.42, 2), (180, 60, 1.38, 4), (180, 42, 1.32, 6), (180, 22, 1.26, 8),
             (180, 2, 1.20, 8))
    for yaw, pitch, tall, lean in crest:
        for side in (-1, 1):
            locks.append(([(yaw + side * 7, pitch, 1.03), (yaw + side * 4, pitch + lean * 0.5, 1.0 + (tall - 1.0) * 0.6), (yaw, pitch + lean, tall)], 0.026, 0.75))
    return {"locks": locks, "cap": 1.018, "brow": 0.54, "nape": -0.50, "taper": 0.88}


def warrior_braids():
    """Braided back along the skull in four tight rows from the brow to the nape, their ends hanging as short braids."""
    locks = []
    for yaw, left in ((-38, -0.054), (-13, -0.019), (13, 0.019), (38, 0.054)):
        path = along((yaw, 43, 1.05), (yaw * 0.9, 84, 1.075), 7)[:-1] + along((180 - yaw * 0.9, 80, 1.075), (180 - yaw * 0.72, -22, 1.075), 15)
        path += line((left, 0.106, -0.072), (left * 1.12, 0.122, -0.215), 9)
        locks.append(braid(path, 0.024, flat=0.80))
    for side in (-1, 1):
        locks.append(braid(along((side * 62, 30, 1.045), (side * 152, -18, 1.068), 15), 0.021, flat=0.80))
        locks.append(([(side * 72, 12, 1.04), (side * 96, 8, 1.055), (side * 126, -8, 1.055), (side * 150, -24, 1.045)], 0.018, 0.40))
    return {"locks": locks, "cap": 1.035, "brow": 0.52, "nape": -0.56, "taper": 0.0}


def half_tied():
    """Long and loose to the shoulders, the hair of the brow and the temples drawn back and tied in a short tail at the
    back of the head."""
    locks = []
    for yaw, size in ((-40, 0.022), (-20, 0.024), (0, 0.025), (20, 0.024), (40, 0.022)):
        locks.append(([(yaw, 42, 1.045), (yaw * 0.95, 58, 1.075), (yaw * 0.8, 80, 1.08), (180 - yaw * 0.6, 60, 1.09), (180, 36, 1.11)], size, 0.42))
    for side in (-1, 1):
        locks.append(([(side * 60, 30, 1.045), (side * 88, 38, 1.08), (side * 128, 40, 1.09), (180, 36, 1.11)], 0.022, 0.42))
        locks.append(([(side * 66, 14, 1.05), (side * 84, 6, 1.085), xyz(side * 0.098, 0.024, -0.050), xyz(side * 0.108, 0.040, -0.150), xyz(side * 0.112, 0.050, -0.255)], 0.027, 0.48))
        locks.append(([(side * 104, 22, 1.08), (side * 116, 4, 1.10), xyz(side * 0.084, 0.076, -0.050), xyz(side * 0.094, 0.092, -0.160), xyz(side * 0.098, 0.102, -0.275)], 0.029, 0.48))
    locks.append(([(180, 39, 1.15), (180, 30, 1.24)], 0.037, 0.95, "tie"))
    locks.append(([(180, 30, 1.16), (180, 6, 1.22), xyz(0, 0.124, -0.060), xyz(0, 0.128, -0.130)], 0.030, 0.60))   # its short tail
    curtain(locks, low=-0.30, top=28, size=0.046)
    return {"locks": locks, "cap": 1.045, "brow": 0.50, "nape": -0.66, "taper": 0.25}


def mane():
    """Long, thick and wild: heavy locks thrown back from the brow and standing out round the head down to the shoulders."""
    locks = []
    for yaw, size in ((-44, 0.030), (-22, 0.034), (0, 0.036), (22, 0.034), (44, 0.030)):
        locks.append(([(yaw, 42, 1.05), (yaw * 1.0, 56, 1.14), (yaw * 0.9, 78, 1.18), (180 - yaw * 0.9, 58, 1.20), (180 - yaw * 0.85, 20, 1.22),
                       xyz(-0.07 * yaw / 44, 0.150, -0.060), xyz(-0.08 * yaw / 44, 0.160, -0.170)], size, 0.55))
    for side in (-1, 1):
        for yaw, back, left, low in ((62, 0.02, 0.135, -0.19), (88, 0.07, 0.140, -0.23), (118, 0.115, 0.118, -0.26)):
            sway = side * 6
            locks.append(([(side * (yaw - 6), 34, 1.05), (side * yaw + sway, 30, 1.17), (side * (yaw + 6), 2, 1.24), xyz(side * left, back, -0.070),
                           xyz(side * (left + 0.012), back + 0.012, low + 0.07), xyz(side * (left - 0.004), back + 0.016, low)], 0.034, 0.55))
        locks.append(([(side * 58, 16, 1.045), (side * 66, -4, 1.12), xyz(side * 0.104, -0.020, -0.090), xyz(side * 0.110, -0.020, -0.165)], 0.026, 0.50))
    curtain(locks, low=-0.29, top=20, size=0.046, flare=1.25, back=0.030, across=(-0.075, -0.038, 0.0, 0.038, 0.075))
    return {"locks": locks, "cap": 1.06, "brow": 0.48, "nape": -0.66, "taper": 0.45}


def receding():
    """The hairline gone far back over the brow and thin on top; longer at the sides and behind, to the collar."""
    locks = []
    for yaw in (-24, 0, 24):
        locks.append(([(yaw, 66, 1.03), (yaw * 0.8, 82, 1.045), (180 - yaw, 62, 1.05), (180 - yaw * 0.9, 34, 1.055)], 0.020, 0.34))
    for side in (-1, 1):
        locks.append(([(side * 64, 34, 1.04), (side * 84, 30, 1.075), (side * 110, 14, 1.085), (side * 132, -14, 1.085), (side * 142, -38, 1.10)], 0.024, 0.44))
        locks.append(([(side * 72, 14, 1.04), (side * 90, 6, 1.07), (side * 112, -12, 1.08), (side * 128, -36, 1.09)], 0.022, 0.44))
    for yaw, low in ((150, -44), (165, -48), (180, -50), (195, -48), (210, -44)):
        locks.append(([(yaw, 44, 1.055), (yaw, 14, 1.08), (yaw, -18, 1.09), (yaw, low, 1.16)], 0.025, 0.46))
    return {"locks": locks, "cap": 1.03, "brow": 0.84, "nape": -0.62, "taper": 0.55}


def curtains():
    """Parted in the middle and falling to the cheekbones on both sides like two curtains; short behind."""
    locks = []
    for side in (-1, 1):
        for start, yaw, end, out in ((52, 26, 2, 1.085), (64, 44, -8, 1.10), (76, 62, -14, 1.105)):
            locks.append(([(side * 5, start, 1.05), (side * yaw * 0.6, start - 8, 1.10), (side * yaw, start - 30, 1.11), (side * (yaw + 6), end + 12, out), (side * (yaw + 8), end, out - 0.025)],
                          0.025, 0.46))
        locks.append(([(side * 8, 86, 1.05), (side * 70, 62, 1.085), (side * 96, 30, 1.09), (side * 106, 4, 1.085), (side * 110, -12, 1.06)], 0.025, 0.46))
    back_short(locks, low=-30, out=1.08, size=0.025)
    for side in (-1, 1):
        locks.append(([(side * 120, 40, 1.07), (side * 128, 14, 1.085), (side * 134, -10, 1.075), (side * 136, -26, 1.05)], 0.023, 0.44))
    return {"locks": locks, "cap": 1.045, "brow": 0.48, "nape": -0.56, "taper": 0.55}


def through(places, count):
    """`count` points along the bent line through `places` (each in metres from the head's middle), evenly spread."""
    lengths = [math.dist(a, b) for a, b in zip(places, places[1:])]
    total = sum(lengths)
    points = []
    for i in range(count):
        at = total * i / (count - 1)
        for (a, b), length in zip(zip(places, places[1:]), lengths):
            if at <= length + 1e-9:
                t = at / length
                points.append(xyz(*[p + (q - p) * t for p, q in zip(a, b)]))
                break
            at -= length
    return points


def before_shoulder(side, low=-0.335):
    """The places a braid or a tail passes from under the ear over the collarbone and down before the chest (clear of it)."""
    return [(side * 0.090, 0.012, -0.088), (side * 0.100, -0.040, -0.128), (side * 0.106, -0.092, -0.180), (side * 0.106, -0.128, -0.250), (side * 0.104, -0.150, low)]


def drawn_back(locks, to=(180, -22), lift=1.075, part=False):
    """The hair of the brow, the temples and the sides drawn back over the head to one place at the back (`to`: yaw and
    pitch), where a tail, a braid or a bun begins; from a middle parting when `part`."""
    yaw_to, pitch_to = to
    for yaw, size in ((-42, 0.023), (-21, 0.026), (0, 0.027), (21, 0.026), (42, 0.023)):
        if part and yaw == 0:
            continue
        start = yaw if not part else yaw * 0.25
        path = [(start, 43 if not part else 60, 1.045), (yaw * 0.95, 60, lift), (yaw * 0.8, 80, lift + 0.005)]
        if pitch_to < 70:
            path.append((180 - yaw * 0.7, max(pitch_to + 20, 56), lift + 0.005))
        if pitch_to < 30:
            path.append((180 - yaw * 0.4, pitch_to + 30, lift + 0.01))
        locks.append((path + [(yaw_to, pitch_to, lift + 0.015)], size, 0.42))
    for side in (-1, 1):
        mid = (30 + pitch_to) / 2
        locks.append(([(side * 60, 30, 1.045), (side * 86, 30 + (mid - 30) * 0.3, lift), (side * 126, mid, lift + 0.005), (side * 160, pitch_to + (8 if pitch_to < 30 else -8), lift + 0.01),
                       (yaw_to, pitch_to, lift + 0.015)], 0.023, 0.42))
        locks.append(([(side * 70, 12, 1.045), (side * 96, 10 + (mid - 10) * 0.4, lift - 0.01), (side * 132, (pitch_to - 6) if pitch_to < 30 else mid * 0.8, lift - 0.005),
                       (side * 164, pitch_to, lift + 0.005)], 0.020, 0.42))
    if pitch_to > 20:                       # the nape's hair goes up to it
        for yaw in (152, 180, 208):
            locks.append(([(yaw, -24, 1.04), (yaw, 0, 1.065), (180 + (yaw - 180) * 0.6, pitch_to - 22, lift), (yaw_to, pitch_to, lift + 0.015)], 0.023, 0.42))


def braid_back():
    """Drawn back and plaited into one thick braid that hangs down the middle of the back (the woman's)."""
    locks = []
    drawn_back(locks, to=(180, -22))
    locks.append(([(180, -19, 1.13), (180, -27, 1.22)], 0.038, 0.95, "tie"))
    locks.append(braid(line((0, 0.106, -0.062), (0, 0.130, -0.335), 17), 0.034, flat=0.72))
    locks.append(([xyz(0, 0.130, -0.322), xyz(0, 0.130, -0.346)], 0.030, 0.95, "tie"))
    locks.append(([xyz(0, 0.130, -0.335), xyz(0.004, 0.130, -0.365), xyz(0.006, 0.128, -0.395)], 0.026, 0.70))
    return {"locks": locks, "cap": 1.045, "brow": 0.48, "nape": -0.62, "taper": 0.0}


def ringlet(start, to, count=9, wobble=0.012, turn=1):
    """A hanging curl from the place `start` down to `to` (metres from the head's middle): a lock that winds as it falls."""
    points = []
    for i in range(count):
        t = i / (count - 1)
        angle = turn * (i * math.pi * 0.75)
        points.append(xyz(start[0] + (to[0] - start[0]) * t + wobble * math.cos(angle), start[1] + (to[1] - start[1]) * t + wobble * math.sin(angle),
                          start[2] + (to[2] - start[2]) * t))
    return points


def curls():
    """Curls to the shoulders: smooth from a middle parting over the crown, then a full mass of winding curls round the
    face, over the ears and down the neck."""
    locks = []
    for side in (-1, 1):
        for yaw, pitch in ((14, 62), (34, 74), (70, 80), (120, 78)):
            locks.append(([(side * 5, min(88, pitch + 14), 1.05), (side * yaw * 0.6, pitch + 4, 1.085), (side * yaw, pitch - 16, 1.10), (side * (yaw + 12), pitch - 34, 1.11)], 0.027, 0.50))
    places = ((0.088, -0.050, 1), (0.104, -0.012, -1), (0.108, 0.030, 1), (0.094, 0.070, -1), (0.064, 0.098, 1), (0.024, 0.112, -1))
    for side in (-1, 1):
        for n, (left, back, turn) in enumerate(places):
            top = 0.035 - 0.012 * n if n < 3 else -0.005
            low = -0.190 - 0.012 * (n % 3)
            locks.append((ringlet((side * left, back, top), (side * (left + 0.012), back + 0.012, low), count=10, wobble=0.014, turn=side * turn), 0.027, 0.85))
    locks.append((ringlet((0.0, 0.112, -0.01), (0.0, 0.128, -0.215), count=10, wobble=0.014), 0.028, 0.85))
    return {"locks": locks, "cap": 1.05, "brow": 0.48, "nape": -0.66, "taper": 0.25}


def coil(centre, radius, facing=(0.0, 1.0, 0.0), count=10, turns=1.25, rise=0.0):
    """A coil of hair wound flat about `centre` (metres from the head's middle), `facing` the way given: a bun's turns."""
    fx, fy, fz = facing
    length = math.sqrt(fx * fx + fy * fy + fz * fz)
    fx, fy, fz = fx / length, fy / length, fz / length
    ux, uy, uz = (1.0, 0.0, 0.0) if abs(fx) < 0.9 else (0.0, 0.0, 1.0)
    dot = ux * fx + uy * fy + uz * fz
    ux, uy, uz = ux - dot * fx, uy - dot * fy, uz - dot * fz
    length = math.sqrt(ux * ux + uy * uy + uz * uz)
    ux, uy, uz = ux / length, uy / length, uz / length
    vx, vy, vz = fy * uz - fz * uy, fz * ux - fx * uz, fx * uy - fy * ux
    points = []
    for i in range(count):
        t = i / (count - 1)
        angle = 2 * math.pi * turns * t
        r = radius * (1.0 - 0.45 * t)
        out = rise * t
        points.append(xyz(centre[0] + r * (math.cos(angle) * ux + math.sin(angle) * vx) + out * fx, centre[1] + r * (math.cos(angle) * uy + math.sin(angle) * vy) + out * fy,
                          centre[2] + r * (math.cos(angle) * uz + math.sin(angle) * vz) + out * fz))
    return points


def bun():
    """Drawn back smooth from a middle parting and wound into a bun low on the back of the head."""
    locks = []
    drawn_back(locks, to=(180, -6))
    locks.append((coil((0.0, 0.126, -0.012), 0.040, facing=(0, 1, -0.15), count=11, turns=1.4, rise=0.016), 0.027, 0.90))
    locks.append(([xyz(0.0, 0.118, -0.012), xyz(0.0, 0.150, -0.016)], 0.034, 1.0))
    return {"locks": locks, "cap": 1.045, "brow": 0.48, "nape": -0.60, "taper": 0.15}


def high_ponytail():
    """Drawn up and tied high on the back of the head; a long tail arcs out and falls to the shoulder blades."""
    locks = []
    drawn_back(locks, to=(180, 52))
    locks.append(([(180, 53, 1.16), (180, 50, 1.34)], 0.042, 0.95, "tie"))
    for left, size in ((0.0, 0.038), (0.016, 0.026), (-0.016, 0.026)):
        locks.append(([xyz(left * 0.4, 0.082, 0.096), xyz(left * 0.8, 0.138, 0.104), xyz(left, 0.172, 0.052), xyz(left * 1.2, 0.176, -0.040), xyz(left * 1.3, 0.160, -0.150),
                       xyz(left * 1.2, 0.148, -0.250)], size, 0.70))
    return {"locks": locks, "cap": 1.045, "brow": 0.50, "nape": -0.58, "taper": 0.50}


def twin_braids():
    """Parted in the middle and plaited into two braids that hang before the shoulders."""
    locks = []
    for side in (-1, 1):
        for yaw, pitch in ((16, 60), (36, 72), (72, 78)):
            locks.append(([(side * 5, min(88, pitch + 14), 1.05), (side * yaw, pitch, 1.085), (side * (yaw + 30), pitch - 34, 1.09), (side * 96, -6, 1.085), (side * 104, -30, 1.10)],
                          0.026, 0.46))
        for yaw in (126, 152, 174):
            locks.append(([(side * 8, 86, 1.05), (side * yaw, 60, 1.08), (side * yaw, 20, 1.085), (side * (yaw - 20), -14, 1.085), (side * 110, -34, 1.11)], 0.026, 0.46))
        locks.append(([xyz(side * 0.087, 0.018, -0.070), xyz(side * 0.092, 0.011, -0.092)], 0.034, 0.95, "tie"))     # the braid's start, bound
        locks.append(braid(through(before_shoulder(side), 19), 0.029, flat=0.80))
        locks.append(([xyz(side * 0.104, -0.147, -0.322), xyz(side * 0.104, -0.152, -0.346)], 0.026, 0.95, "tie"))
        locks.append(([xyz(side * 0.104, -0.150, -0.335), xyz(side * 0.104, -0.156, -0.380)], 0.022, 0.75))
    return {"locks": locks, "cap": 1.045, "brow": 0.48, "nape": -0.60, "taper": 0.0}


def waved(start, to, count=9, sway=0.012, phase=0.0):
    """A long lock from the place `start` down to `to` (metres from the head's middle) that sways from side to side."""
    points = []
    for i in range(count):
        t = i / (count - 1)
        bend = sway * math.sin(phase + i * 1.25) * min(1.0, t * 3)
        across = (to[1] - start[1], -(to[0] - start[0]))                # level, square to the lock's run seen from above
        size = math.hypot(*across) or 1.0
        side = (1.0, 0.0) if size < 1e-6 else (across[0] / size, across[1] / size)
        if abs(to[0] - start[0]) + abs(to[1] - start[1]) < 0.02:        # a lock that falls straight down sways round the head
            radial = math.hypot(start[0], start[1]) or 1.0
            side = (-start[1] / radial, start[0] / radial)
        points.append(xyz(start[0] + (to[0] - start[0]) * t + side[0] * bend, start[1] + (to[1] - start[1]) * t + side[1] * bend, start[2] + (to[2] - start[2]) * t))
    return points


def parted_top(locks, side_yaws=((14, 62), (34, 74), (70, 80), (120, 78)), size=0.027):
    """The crown smooth from a middle parting down to the ears' height all round."""
    for side in (-1, 1):
        for yaw, pitch in side_yaws:
            locks.append(([(side * 5, min(88, pitch + 14), 1.05), (side * yaw * 0.6, pitch + 4, 1.085), (side * yaw, pitch - 16, 1.10), (side * (yaw + 12), pitch - 36, 1.105)], size, 0.50))


def wavy():
    """Long soft waves from a middle parting, loose over the shoulders and down the back."""
    locks = []
    parted_top(locks)
    places = ((0.092, -0.050, -0.250), (0.108, -0.004, -0.275), (0.104, 0.046, -0.295), (0.082, 0.088, -0.305), (0.046, 0.114, -0.315), (0.0, 0.124, -0.320))
    for side in (-1, 1):
        for n, (left, back, low) in enumerate(places):
            if left == 0.0 and side < 0:
                continue
            top = 0.030 - 0.010 * n if n < 3 else 0.0
            locks.append((waved((side * left, back, top), (side * (left + 0.014), back + 0.016, low), count=7, sway=0.017, phase=0.6 * n), 0.042, 0.40))
    return {"locks": locks, "cap": 1.05, "brow": 0.48, "nape": -0.66, "taper": 0.22}


def bob():
    """Cut level at the jaw all round, from a middle parting; it frames the face and its ends turn in."""
    locks = []
    for side in (-1, 1):
        locks.append(([(side * 5, 72, 1.05), (side * 30, 62, 1.09), (side * 50, 36, 1.11), (side * 58, 4, 1.13), (side * 60, -26, 1.16), (side * 58, -44, 1.12)], 0.030, 0.50))
        for yaw in (72, 96, 120, 144, 166):
            start = 80
            locks.append(([(side * (5 if yaw < 100 else yaw), min(88, start + 8), 1.05), (side * yaw * (0.7 if yaw < 100 else 1.0), start - 6, 1.085), (side * yaw, 32, 1.115),
                           (side * yaw, -4, 1.14), (side * yaw, -30, 1.17), (side * yaw, -46, 1.13)], 0.032, 0.50))
    locks.append(([(180, 86, 1.05), (180, 40, 1.10), (180, -4, 1.14), (180, -30, 1.17), (180, -46, 1.13)], 0.032, 0.50))
    return {"locks": locks, "cap": 1.05, "brow": 0.48, "nape": -0.66, "taper": 0.15}


def pixie():
    """A short crop with a long fringe swept across the brow to the figure's right; close at the ears and wispy at the nape."""
    locks = []
    for pitch, end, size in ((48, 22, 0.024), (58, 28, 0.026), (70, 36, 0.026), (82, 48, 0.024)):
        locks.append(([(40, pitch, 1.045), (22, pitch + 4, 1.085), (-6, pitch - 6, 1.09), (-34, end + 8, 1.08), (-52, end, 1.05)], size, 0.46))
        locks.append(([(44, pitch, 1.045), (60, pitch - 10, 1.07), (74, pitch - 26, 1.06), (82, pitch - 40, 1.045)], size * 0.85, 0.44))
    locks.append(([(40, 88, 1.05), (-40, 82, 1.075), (-104, 62, 1.075), (-128, 40, 1.055)], 0.024, 0.44))
    for side in (-1, 1):
        locks.append(([(side * 66, 26, 1.045), (side * 80, 16, 1.065), (side * 86, 2, 1.06), (side * 84, -10, 1.045)], 0.019, 0.42))
        locks.append(([(side * 96, 40, 1.055), (side * 112, 24, 1.07), (side * 124, 4, 1.065), (side * 130, -14, 1.05)], 0.021, 0.42))
    for yaw, low in ((148, -26), (164, -32), (180, -30), (196, -33), (212, -25)):
        locks.append(([(yaw, 50, 1.065), (yaw, 22, 1.075), (yaw, -6, 1.065), (yaw + 2, low, 1.05)], 0.022, 0.42))
    return {"locks": locks, "cap": 1.042, "brow": 0.50, "nape": -0.54, "taper": 0.60}


def mirrored(style):
    """A style with its left and right changed over."""
    return dict(style, locks=[([(-yaw, pitch, out) for yaw, pitch, out in lock[0]],) + tuple(lock[1:]) for lock in style["locks"]])


def side_braid():
    """Swept over to the figure's right and plaited into one braid that hangs before the right shoulder."""
    locks = []
    for pitch, size in ((46, 0.024), (58, 0.026), (70, 0.027), (82, 0.026)):
        locks.append(([(-34, pitch, 1.045), (-10, pitch + 8, 1.085), (26, pitch + 4, 1.09), (62, pitch - 22, 1.09), (90, -4, 1.09), (100, -28, 1.12)], size, 0.44))
    locks.append(([(-40, 44, 1.045), (-60, 36, 1.07), (-96, 30, 1.075), (-140, 20, 1.08), (176, -4, 1.085), (140, -24, 1.10), (112, -32, 1.13)], 0.024, 0.44))
    locks.append(([(-70, 14, 1.045), (-104, 8, 1.065), (-150, -6, 1.07), (170, -22, 1.085), (130, -32, 1.12)], 0.022, 0.44))
    for yaw in (150, 180, 210):
        locks.append(([(yaw, 80, 1.07), (yaw - 10, 44, 1.085), (yaw - 30, 8, 1.09), (126, -26, 1.12)], 0.026, 0.44))
    locks.append(([xyz(0.086, 0.020, -0.068), xyz(0.092, 0.010, -0.092)], 0.037, 0.95, "tie"))
    locks.append(braid(through(before_shoulder(1, low=-0.36), 21), 0.033, flat=0.76))
    locks.append(([xyz(0.104, -0.147, -0.346), xyz(0.104, -0.152, -0.370)], 0.029, 0.95, "tie"))
    locks.append(([xyz(0.104, -0.150, -0.360), xyz(0.104, -0.156, -0.410)], 0.025, 0.75))
    # modelled before the left shoulder and mirrored: the battle and the sheet show the figure's right (the user, 2026-11-19)
    return mirrored({"locks": locks, "cap": 1.045, "brow": 0.48, "nape": -0.60, "taper": 0.0})


def crown_braid():
    """Smooth from a middle parting, with a thick braid laid round the head like a crown."""
    locks = []
    parted_top(locks, side_yaws=((16, 66), (40, 76), (80, 82), (130, 80)), size=0.026)
    for side in (-1, 1):
        locks.append(([(side * 66, 16, 1.045), (side * 90, 8, 1.06), (side * 124, -6, 1.065), (side * 156, -22, 1.06)], 0.021, 0.42))
    for yaw in (150, 180, 210):
        locks.append(([(yaw, 30, 1.07), (yaw, 6, 1.075), (yaw, -22, 1.05)], 0.024, 0.42))
    ring = [(yaw, 40 + 6 * math.cos(math.radians(yaw)), 1.115) for yaw in range(-180, 181, 12)]
    locks.append(braid(ring, 0.030, flat=0.85))
    return {"locks": locks, "cap": 1.045, "brow": 0.48, "nape": -0.58, "taper": 0.0}


def half_up():
    """Long and loose down the back, the hair of the brow and the temples drawn back and tied behind the crown (the woman's)."""
    style = half_tied()
    style["locks"] = [lock for lock in style["locks"]]
    return style


def long_fringe():
    """Long and straight with a level fringe over the brow."""
    style = long_loose()
    locks = [lock for lock in style["locks"] if not (len(lock[0]) == 6 and abs(lock[0][0][0]) == 6)]        # without the face's own two locks
    for yaw, size in ((-38, 0.022), (-23, 0.024), (-8, 0.025), (8, 0.025), (23, 0.024), (38, 0.022)):
        locks.append(([(yaw * 0.4, 74, 1.05), (yaw * 0.8, 58, 1.085), (yaw, 40, 1.085), (yaw, 25, 1.06)], size, 0.48))
    for side in (-1, 1):
        locks.append(([(side * 10, 70, 1.05), (side * 46, 50, 1.09), (side * 60, 20, 1.10), xyz(side * 0.094, -0.030, -0.070), xyz(side * 0.102, -0.028, -0.180),
                       xyz(side * 0.106, -0.022, -0.270)], 0.026, 0.46))
    return dict(style, locks=locks, taper=0.22)


def top_bun():
    """Drawn up from the brow, the ears and the nape and wound into a bun on top of the head."""
    locks = []
    drawn_back(locks, to=(180, 80))
    locks.append((coil((0.0, 0.024, 0.150), 0.036, facing=(0, 0.25, 1), count=11, turns=1.4, rise=0.020), 0.027, 0.90))
    locks.append(([xyz(0.0, 0.024, 0.140), xyz(0.0, 0.030, 0.178)], 0.034, 1.0))
    return {"locks": locks, "cap": 1.045, "brow": 0.50, "nape": -0.56, "taper": 0.15}


def side_undercut():
    """The figure's right side shaved close; the rest thrown over to its left and falling to the jaw."""
    locks = []
    for pitch, size in ((48, 0.027), (60, 0.030), (72, 0.030), (84, 0.028)):
        locks.append(([(-44, pitch, 1.04), (-18, pitch + 10, 1.11), (20, pitch + 6, 1.12), (56, pitch - 22, 1.12), (78, -2, 1.13), (86, -30, 1.17), (88, -44, 1.14)], size, 0.50))
    for yaw in (112, 138, 162):
        locks.append(([(-60, 86, 1.05), (yaw - 40, 78, 1.09), (yaw, 36, 1.11), (yaw, -4, 1.13), (yaw, -32, 1.16), (yaw, -46, 1.13)], 0.030, 0.50))
    locks.append(([(-150, 60, 1.03), (176, 60, 1.085), (184, 20, 1.10), (182, -14, 1.11), (180, -34, 1.08)], 0.028, 0.48))
    return {"locks": locks, "cap": 1.018, "brow": 0.52, "nape": -0.52, "taper": 0.30}


def twin_buns():
    """Parted in the middle and wound into two buns high on the sides of the head."""
    locks = []
    for side in (-1, 1):
        for yaw, start in ((16, 46), (40, 44), (62, 32)):
            locks.append(([(side * (5 if yaw < 30 else yaw), start + (30 if yaw < 30 else 0), 1.05), (side * yaw, start + 14, 1.075), (side * (yaw + 30), 60, 1.08), (side * 118, 58, 1.10)], 0.024, 0.42))
        locks.append(([(side * 72, 12, 1.045), (side * 92, 24, 1.065), (side * 112, 42, 1.075), (side * 120, 54, 1.10)], 0.022, 0.42))
        for yaw in (150, 172):
            locks.append(([(side * yaw, -22, 1.04), (side * yaw, 8, 1.065), (side * (yaw - 14), 36, 1.075), (side * 124, 54, 1.10)], 0.023, 0.42))
        locks.append((coil((side * 0.070, 0.050, 0.108), 0.030, facing=(side * 0.8, 0.45, 0.6), count=10, turns=1.35, rise=0.016), 0.025, 0.90))
        locks.append(([xyz(side * 0.066, 0.046, 0.102), xyz(side * 0.090, 0.060, 0.122)], 0.030, 1.0))
    locks.append(([(180, 86, 1.05), (180, 50, 1.07), (180, 10, 1.07), (180, -22, 1.045)], 0.024, 0.42))
    return {"locks": locks, "cap": 1.045, "brow": 0.48, "nape": -0.56, "taper": 0.15}


STYLES = {"swept-back": swept_back, "long-tied": long_tied, "crop": crop, "spiky": spiky, "curly": curly, "messy": messy, "shaggy": shaggy,
          "buzz-cut": buzz_cut, "long": long_loose, "ponytail": ponytail, "topknot": topknot, "side-part": side_part,
          "fringe": fringe, "slicked-back": slicked_back, "undercut": undercut, "mohawk": mohawk, "warrior-braids": warrior_braids,
          "half-tied": half_tied, "mane": mane, "receding": receding, "curtains": curtains,
          "braid": braid_back, "curls": curls, "bun": bun, "high-ponytail": high_ponytail, "twin-braids": twin_braids,
          "wavy": wavy, "bob": bob, "pixie": pixie, "side-braid": side_braid, "crown-braid": crown_braid, "half-up": half_up, "long-fringe": long_fringe,
          "top-bun": top_bun, "side-undercut": side_undercut, "twin-buns": twin_buns}


def hair_model(path, style, body, face, level):
    """The style `style` fitted to `body`'s head with the face `face` (manifest.json: "parts.hair:hair_model", args [style,
    body, face], its level): its pieces bound to `head` and joined, with the body's skeleton."""
    if style not in STYLES:
        raise ValueError(f"no hair style {style} in parts/hair.py")
    p = with_face(body, face, level)
    rig = r.build_rig(p, forms.joints_of(p))
    made = materials.make()
    parts = forms.build_hair(p, forms.head_shape(p["head"]), made["hair"], STYLES[style](), tie=made["hair.tie"])
    for part in parts:
        r.bind_rigid(rig, part, "head")
    k.join_by_ink(parts, f"{body}-{style}")
    bodies.export_sized(path, p, export_skins=True)

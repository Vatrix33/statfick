# The battle's idle on the ready stance (GM2-P23; the clip `idle`): two seconds, a loop. Nothing in it is still: the breath
# lifts the chest and the body with it, the weight rocks onto the front foot and back (a quarter of the loop after the
# breath, so the hips go round a small oval), the rear heel works with the rock, the trunk and the head answer each
# other, and the arms come after the body. standing.py writes it; the stance loops (GM2-P21) and the guard's hold are made
# on its body. Plain Python.
import math

from poses import key, on_toes

FRAMES = 120
EVERY = 10          # a key every so many frames


def waves(frame):
    """The phase, the breath (0 out, 1 in), the rock (+ forward) and a sway at a frame of the loop."""
    p = 2 * math.pi * frame / FRAMES
    return p, (1 - math.cos(p)) / 2, math.sin(p), math.sin(p + 0.6)


def body(frame, arms=True):
    """The idle's body at a frame, as a key's (pose, hips, feet) on the ready stance. `arms`: with the one-handed arms' own
    small life (the weapon's point bobbing after the breath, the shield following the rock)."""
    p, b, r, s = waves(frame)
    after = lambda late: math.sin(p - late)
    heel = 3.5 * r
    pose = {"hips": [-1.0 * r, 1.2 * s, 0], "spine": [1.0 * b, -0.5 * s, 0], "chest": [2.6 * b - 0.6 * r, -1.5 * s, 0],
            "shoulder.L": [0, 0, 1.8 * b], "shoulder.R": [0, 0, -1.8 * b],
            "neck": [-0.6 * b, 0.4 * s, 0], "head": [-1.8 * b + 1.0 * r, 0.7 * s, 0.7 * after(1.2)]}
    if arms:
        pose.update({"upper_arm.R": [1.5 * after(0.7), 0, 0], "forearm.R": [3.0 * after(0.9), 0, 0], "hand.R": [2.0 * math.cos(p - 0.9), 0, 2.0 * after(0.9)],
                     "upper_arm.L": [2.0 * after(0.6), 0, 1.0 * after(0.6)], "forearm.L": [2.0 * after(1.2), 0, 0]})
    hips = [0.005 * s, -0.016 * r, 0.018 * b - 0.006]
    feet = {"R": [0.0, 0.0, on_toes(10 + heel) - on_toes(10), -heel, 0]}
    return pose, hips, feet


def keys(hands=None, extra=None, arms=True):
    """The idle's keys; `hands(frame)` gives the arms' grips at a frame (a stance's loop), `extra` a pose laid on every key."""
    out = []
    for frame in range(0, FRAMES + 1, EVERY):
        pose, hips, feet = body(frame % FRAMES, arms)
        pose.update(extra or {})
        out.append(key(frame, pose, hips=hips, feet=feet, hands=hands(frame % FRAMES) if hands else None))
    return out

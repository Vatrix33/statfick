# What every clip's keying shares (GM2-P23; Docs/Game/Hero.md, "How it works", "The keying kit"): the ready stance every
# battle clip starts from and returns to, a key's making from it, a clip's keys made dense along smooth curves, and the
# keying source a clip module gives. Plain Python (no Blender): sources.py writes the sources, bake.py turns them into the
# clips' data inside Blender.
#
# Angles are XYZ Euler degrees in each bone's own axes from its rest:
#   spine, chest, neck, head: X+ leans back, Y+ turns to the figure's left, Z+ bends to its left
#   upper_arm: X- swings forward, Z lowers the arm (+ on the left, - on the right), Y twists
#   forearm: X- bends the elbow; fingers: Z curls (+ on the left, - on the right)
# A key's places are in the figure's own terms: "hips" [left, back, up] metres the hips move; "feet" {"L": [left, back,
# up, toe pitch (+ is the toe up), turn(, heading)]} where each foot stands from its rest place (bake.py solves the leg
# to reach it); "hands" {"R": {"at": [left, back, up], "aim": [...], "edge": [...], "elbow": [...]}} where a hand's grip
# is, the way the weapon in it points, the way its knuckles (and a blade's edge) look, the way the elbow points (solver.py
# solves the arm), "blend" 0..1 between the pose's own arm and the solved one, or {"on": "R", "along": m, "roll": deg}
# for the second hand on the other's weapon.
import copy
import math


def on_toes(pitch):
    """How far a foot pitched toe-down by `pitch` degrees is lifted so its toe stays where it was (the foot's bone is
    0.214 m and lies 19 degrees below the level at rest)."""
    return 0.214 * (math.sin(math.radians(19 + pitch)) - math.sin(math.radians(19)))


# The ready stance every battle clip starts from and returns to (the second one, 2026-11-17: the user found the first
# "very stiff ... like training"): right-handed, the left foot well forward, the feet apart, the knees bent, the body
# turned its left side to the foe and leaning in, the weight on the balls of the feet (the rear heel off the ground), the
# shield up before the chest, the weapon cocked by the right hip with its point up at the foe. The arms' angles are the
# solver's answer for the stance's grips (the right's at [-0.27, -0.10, 0.98], the left's at [0.03, -0.44, 1.14]), kept
# as angles so every clip starts and ends in them to the digit.
READY = {
    "pose": {"hips": [-6, -24, 0], "spine": [-4, 3, 0], "chest": [-5, 3, 0], "neck": [3, 6, 0], "head": [7, 11, 0],
             "upper_arm.R": [22.848, -15.992, -10.732], "forearm.R": [-106.878, -1.135, -1.408], "hand.R": [33.308, 6.403, -10.582],
             "fingers.R": [0, 0, -85], "thumb.R": [0, 0, 30],
             "upper_arm.L": [-36.616, 11.305, -46.515], "forearm.L": [-95.558, 3.663, 29.94], "hand.L": [0.322, -31.798, -0.221],
             "fingers.L": [0, 0, 62], "thumb.L": [0, 0, -20]},
    "hips": [0.0, -0.015, -0.11],
    "feet": {"L": [0.05, -0.25, 0.0, 0, 4], "R": [-0.07, 0.21, round(on_toes(10), 4), -10, -40]},
}


def place(side, left, back, up=0.0, pitch=0.0, turn=None):
    """A foot's delta for a key that puts it at a place given from its rest place (not from the stance's): for strides."""
    stance = READY["feet"][side]
    wanted = [left, back, up, pitch, stance[4] if turn is None else turn]
    return [w - s_ for w, s_ in zip(wanted, stance)]


def key(frame, pose=None, hips=None, feet=None, hands=None, base=READY):
    """A key at `frame`: `base` (the ready stance) with `pose`'s angles, `hips`' move and `feet`'s places added to its
    own, and `hands` as given."""
    k = copy.deepcopy(base)
    k["frame"] = frame
    for name, delta in (pose or {}).items():
        current = k["pose"].get(name, [0, 0, 0])
        k["pose"][name] = [a + b for a, b in zip(current, delta)]
    if hips:
        k["hips"] = [a + b for a, b in zip(k["hips"], hips)]
    for side, delta in (feet or {}).items():
        # the first five are added to the stance's; a sixth (the leg's heading) is taken as it is
        k["feet"][side] = [a + b for a, b in zip(k["feet"][side], delta)] + list(delta[5:])
    if hands:
        k["hands"] = copy.deepcopy(hands)
    return k


def flat(value, path=()):
    """Every number of a nested key with its path."""
    if isinstance(value, dict):
        for name, inner in value.items():
            yield from flat(inner, path + (name,))
    elif isinstance(value, (list, tuple)):
        for index, inner in enumerate(value):
            yield from flat(inner, path + (index,))
    else:
        yield path, value


def put(target, path, value):
    """Sets the value at `path` in a nested key, making the dicts and lists on the way."""
    for step, following in zip(path, path[1:]):
        if isinstance(target, list):
            while len(target) <= step:
                target.append([] if isinstance(following, int) else {})
            target = target[step]
        else:
            target = target.setdefault(step, [] if isinstance(following, int) else {})
    if isinstance(target, list):
        while len(target) <= path[-1]:
            target.append(0.0)
    target[path[-1]] = value


def curve(times, values):
    """A monotone cubic through the points (no overshoot: a held pose stays held), flat at both ends."""
    n = len(times)
    slopes = [(values[i + 1] - values[i]) / (times[i + 1] - times[i]) for i in range(n - 1)]
    tangents = [0.0] * n
    for i in range(1, n - 1):
        if slopes[i - 1] * slopes[i] > 0:
            a, b = times[i] - times[i - 1], times[i + 1] - times[i]
            tangents[i] = (2 * b + a + b + 2 * a) / ((2 * b + a) / slopes[i - 1] + (b + 2 * a) / slopes[i])

    def at(t):
        i = max(0, min(n - 2, next((j for j in range(n - 1) if t < times[j + 1]), n - 2)))
        h = times[i + 1] - times[i]
        s = (t - times[i]) / h
        return ((2 * s ** 3 - 3 * s ** 2 + 1) * values[i] + (s ** 3 - 2 * s ** 2 + s) * h * tangents[i]
                + (-2 * s ** 3 + 3 * s ** 2) * values[i + 1] + (s ** 3 - s ** 2) * h * tangents[i + 1])
    return at


# Overlap: the bones that follow the hips a little late in a one-shot clip (frames), so the trunk, the neck and the head
# don't all arrive on the same frame as the hips (dense()'s `lag`; the user's manner, 2026-11-17: nothing held still). A
# solved hand keeps its keyed place and time.
LAG = {"spine": 0.5, "chest": 1.5, "shoulder": 2.0, "neck": 2.5, "head": 3.5}


def dense(keys, step=2, lag=None, after=None):
    """The keys as keys every `step` frames: every number (a bone's angle, the hips' place, a foot's, a hand's grip and
    the ways it points) follows a smooth curve in time through the keys' values, so a hand travels the path its grip was
    keyed along and the solver places the arm at every step. A hand a key leaves out is the pose's own arm there
    (blend 0), its grip taken from the nearest key that has it. `after(key)` is applied to every dense key last."""
    keys = [copy.deepcopy(k) for k in keys]
    for side in "RL":
        having = [k for k in keys if side in k.get("hands", {})]
        if not having:
            continue
        for k in keys:
            hands = k.setdefault("hands", {})
            if side not in hands:
                nearest = min(having, key=lambda other: abs(other["frame"] - k["frame"]))
                hands[side] = dict(copy.deepcopy(nearest["hands"][side]), blend=0.0)
            hands[side].setdefault("blend", 1.0)
    times = [k["frame"] for k in keys]
    channels, fixed = {}, {}
    for index, k in enumerate(keys):
        for path, value in flat({name: inner for name, inner in k.items() if name != "frame"}):
            if isinstance(value, bool) or not isinstance(value, (int, float)):
                fixed.setdefault(path, [None] * len(keys))[index] = value
            else:
                channels.setdefault(path, [None] * len(keys))[index] = float(value)
    for path, values in channels.items():
        if path[0] == "pose":                  # a bone a key doesn't name is at rest there (the stance names the others)
            values[:] = [0.0 if v is None else v for v in values]
        # a key that leaves a hand's choice out (the elbow's way, the forearm's share of a turn) holds its neighbour's
        if (path[0] != "hands" or path[2] in ("edge", "roll")) and any(v is None for v in values):
            # a hand's turn about its weapon is either keyed in every key of the clip or left to the solver in all
            raise ValueError(f"the channel {path} is not in every key: {values}")
        known = [i for i, v in enumerate(values) if v is not None]
        for i, v in enumerate(values):
            if v is None:
                values[i] = values[min(known, key=lambda j: abs(times[j] - times[i]))]
    curves = {path: curve(times, values) for path, values in channels.items()}

    def late(path):
        if not lag or path[0] != "pose":
            return 0.0
        return next((frames_ for name, frames_ in lag.items() if path[1].startswith(name)), 0.0)

    out = []
    frames = sorted(set(range(times[0], times[-1] + 1, step)) | set(times))
    for frame in frames:
        k = {"frame": frame}
        for path, at in curves.items():
            put(k, path, round(at(max(times[0], frame - late(path))), 5))
        nearest = min(range(len(keys)), key=lambda i: abs(times[i] - frame))
        for path, values in fixed.items():
            if values[nearest] is not None:
                put(k, path, values[nearest])
        out.append(after(k) if after else k)
    return out


ARMS = ["shoulder", "upper_arm", "forearm", "hand", "fingers", "thumb"]      # the bones a stance loop keys (GM2-P21)


def source(name, frames, keys, loop=False, markers=None, gear=None, fine=0, only=None, lag=None, travel=None, after=None, string=None,
           ease=None):
    """A clip's keying source (the form bake.py reads; Hero.md, "The keying kit"): its name, length in frames at 60 fps,
    whether it loops, its markers {event: frame}, its ease and its keys. `fine`: the keys made dense every so many frames
    (0: as keyed; dense()'s `lag` and `after`). `only`: a stance loop's bones (by the start of their names; the clip
    holds those alone). For the pictures only (bake.py's frames, never the clip's data): `gear` what the hands hold
    ("sword,shield" when not said; "greatsword", "dagger@hand.L,sword" ...), `travel` [[frame, metres back], ...] how the
    game moves the figure during the clip (a leap back), `string` [[frame, 0..1], ...] while 1 the bow's string follows
    the string hand and carries an arrow."""
    # A clip keyed at every frame is written "linear": Blender's in-betweens are then straight, as the engine's are between
    # the frame samples it gets (with Bezier keys at every frame Blender's auto-clamped handles bend the curve inside a
    # frame, which the engine never sees: the manager's choice, 2026-11-18, Hero.md "Decisions"). The frames are the same.
    spec = {"name": name, "frames": frames, "loop": loop, "markers": markers or {}, "ease": ease or ("linear" if fine == 1 else "bezier"),
            "keys": dense(keys, fine, lag, after) if fine else keys}
    if gear is not None:
        spec["gear"] = gear
    if travel:
        spec["travel"] = [list(point) for point in travel]
    if string:
        spec["string"] = [list(point) for point in string]
    if only:
        spec["only"] = list(only)
    return spec

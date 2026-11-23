# The arms' keying helpers (GM2-P23; Docs/Game/Hero.md, "How it works", "The keying kit"): an acting arm is keyed by its
# grip (solver.py solves the arm for it); these make a grip's key from the arm's own directions in the figure's space (x
# to its left, y to its back, z up), which is how a blow or a hold is thought of: where the upper arm points from the
# shoulder, where the forearm points, where the weapon points. Also the weapon arm's and the shield arm's ways beside the
# ready stance, and the ready stances per weapon kind (the user's Q56; GM2-P21's stance loops). Plain Python.
import math

from poses import READY, on_toes

UPPER, FORE, HAND = 0.292, 0.264, 0.067          # the man's upper arm, forearm, and the grip's distance past the wrist


def norm(v):
    length = math.sqrt(sum(c * c for c in v))
    return [c / length for c in v]


READY_TURN, READY_LEAN = -18.0, 12.0      # the ready stance's own turn (to the figure's right) and lean (forward), degrees
CENTRE = (-0.018, -0.085, 1.314)          # between the shoulders in the ready stance (measured on the posed stance)
HIPS = (0.0, -0.003, 0.844)               # the hips there


def shoulder(side, yaw=0.0, lean=0.0, hips=(0.0, 0.0, 0.0)):
    """About where a shoulder is: the trunk turned `yaw` degrees to the figure's left and leant `lean` degrees forward
    from the ready stance, the hips moved by [left, back, up]. (The solver puts the grip where it is keyed whatever this
    says; a better guess only means a straighter wrist.)"""
    sign = 1 if side == "L" else -1
    turn = math.radians(yaw + READY_TURN)
    x, y = sign * 0.201 * math.cos(turn), sign * 0.201 * math.sin(turn)
    a, b = math.radians(READY_LEAN), math.radians(READY_LEAN + lean)
    # the lean is shared down the spine, so the shoulders move as if on a shorter post than the trunk (measured on the rig)
    forward, drop = 0.30 * (math.sin(b) - math.sin(a)), 0.30 * (math.cos(a) - math.cos(b))
    return [CENTRE[0] + x + hips[0], CENTRE[1] + y - forward + hips[1], CENTRE[2] - drop + hips[2]]


def arm(at_shoulder, upper, fore, blade, wrist=0.0, twist=0.5):
    """A hand's key from the arm's directions: `upper` and `fore` the ways the upper arm and the forearm point,
    `blade` the weapon's way (made square to the forearm: a closed fist holds it so), `wrist` degrees the weapon is
    tipped on toward the forearm's line (the wrist's own bend in a cut's last part)."""
    u, e = norm(upper), norm(fore)
    dot = sum(a * b for a, b in zip(blade, e))
    a = norm([b - dot * c for b, c in zip(blade, e)])
    w = math.radians(wrist)
    aim = [math.cos(w) * p + math.sin(w) * q for p, q in zip(a, e)]
    edge = [math.cos(w) * q - math.sin(w) * p for p, q in zip(a, e)]
    wrist_at = [s + UPPER * p + FORE * q for s, p, q in zip(at_shoulder, u, e)]
    return {"at": [round(c + HAND * d, 4) for c, d in zip(wrist_at, edge)], "aim": [round(c, 4) for c in aim], "edge": [round(c, 4) for c in edge],
            "elbow": [round(c, 4) for c in u], "twist": twist}


def _perp(v, e):
    dot = sum(a * b for a, b in zip(v, e))
    return [a - dot * b for a, b in zip(v, e)]


def _cross(a, b):
    return [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]]


def roll_of(pose):
    """How the weapon stands about the forearm in an arm()-pose: degrees from the forearm's side that faces away from the
    upper arm (the outside of the elbow's bend) to the weapon's square way, about the forearm."""
    u, e = norm(pose[0]), norm(pose[1])
    a = norm(_perp(pose[2], e))
    r = norm(_perp([-c for c in u], e))
    return math.degrees(math.atan2(sum(p * q for p, q in zip(_cross(r, a), e)), sum(p * q for p, q in zip(r, a))))


def arm_rolled(at_shoulder, upper, fore, roll, wrist=0.0, twist=0.5):
    """arm(), the weapon's way given by its roll about the forearm (roll_of) instead of a direction in space: a pose
    given so can be mixed with another without the weapon flipping where its line crosses the forearm's."""
    e = norm(fore)
    r = norm(_perp([-c for c in norm(upper)], e))
    k = math.radians(roll)
    side = _cross(e, r)
    a = [math.cos(k) * p + math.sin(k) * q for p, q in zip(r, side)]
    return arm(at_shoulder, upper, fore, a, wrist=wrist, twist=twist)


def by_ways(pose, yaw=0.0, lean=0.0, more=None):
    """A hand's key given by the arm's own ways (`pose`: upper arm, forearm, weapon, wrist degrees) instead of its grip.
    Dense keys then mix the ways between keys (the weapon's as its roll about the forearm), so the arm stays an arm all
    the way: mixing two grips' places and ways apart can run the grip through the shoulder or fold the wrist for a
    frame, and mixing the weapon's direction in space flips it where it crosses the forearm's line. Every key of the
    clip that has this hand must give it so; source(..., after=ways_to_grips) turns them into grips. Keys in a row should
    have their rolls made continuous (continuous_rolls)."""
    roll = float(pose[2]) if isinstance(pose[2], (int, float)) else roll_of(pose)        # the weapon's way, or its roll itself
    spec = {"upper": list(pose[0]), "fore": list(pose[1]), "roll": roll, "wrist": float(pose[3]) if len(pose) > 3 else 0.0, "yaw": float(yaw), "lean": float(lean)}
    spec.update(more or {})
    return spec


def continuous_rolls(keys):
    """The keys with each hand's roll brought within half a turn of the key before (so a mix goes the short way round)."""
    last = {}
    for k in keys:
        for side, spec in k.get("hands", {}).items():
            if "roll" in spec and "upper" in spec:
                if side in last:
                    spec["roll"] += 360.0 * round((last[side] - spec["roll"]) / 360.0)
                last[side] = spec["roll"]
    return keys


def ways_to_grips(k):
    """A dense key's hands given by their arms' ways, as grips (the place from the shoulder's, the hips moved as the key says)."""
    moved = [a - b for a, b in zip(k.get("hips", READY["hips"]), READY["hips"])]
    for side, spec in list(k.get("hands", {}).items()):
        if "upper" in spec:
            grip = arm_rolled(shoulder(side, spec["yaw"], spec["lean"], moved), spec["upper"], spec["fore"], spec["roll"], wrist=spec["wrist"])
            grip.update({name: value for name, value in spec.items() if name not in ("upper", "fore", "roll", "wrist", "yaw", "lean")})
            # the elbow is where the upper arm's way puts it: the solver's own swing of the elbow, searched anew each
            # frame, snapped for single frames where the fist passes the shoulder (the measure's jumps)
            grip.setdefault("swing", False)
            k["hands"][side] = grip
    return k


def trunk(yaw=0.0, lean=0.0, head=None):
    """The trunk turned and leant from the ready stance, shared over hips, spine and chest; the head keeps looking ahead."""
    return {"hips": [-lean * 0.25, yaw * 0.2, 0], "spine": [-lean * 0.3, yaw * 0.3, 0], "chest": [-lean * 0.45, yaw * 0.5, 0],
            "head": [lean * 0.5, -yaw * 0.55 if head is None else head, 0], "neck": [lean * 0.2, -yaw * 0.2, 0]}


def mix(a, b, t):
    return [p + (q - p) * t for p, q in zip(a, b)]


def between(first, second, t=0.5, yaw=0.0, lean=0.0, hips=(0.0, 0.0, 0.0), side="R"):
    """An arm part of the way from one keyed arm to another: its upper arm's, forearm's and weapon's ways mixed, so the
    arm stays an arm on the way (the solver's own in-betweens mix the grip's place and ways apart, and a wrist can fold)."""
    (u1, f1, b1, w1), (u2, f2, b2, w2) = first, second
    return arm(shoulder(side, yaw, lean, hips), mix(u1, u2, t), mix(f1, f2, t), mix(b1, b2, t), wrist=w1 + (w2 - w1) * t)


# The weapon held high: the upper arm up and back (the elbow behind the ear), the forearm up and a little forward, the
# blade up and forward. With the elbow out to the side instead the forearm is turned 106 degrees about itself (the user
# saw it: "the arm twists weird"); with it back, 17. The stance's arm mixes straight into it (both elbows point back), so
# the rise needs no waypoint.
HIGH = ((-0.25, 0.45, 0.86), (0.0, 0.10, 0.99), (0.0, -0.99, 0.10), 40)
SHAKEN = ((-0.20, 0.40, 0.90), (-0.20, 0.14, 0.97), (0.22, -0.97, 0.08), 32)         # its point swung over to the left
# On the way up and down the fist passes before the right shoulder, the elbow out and down: keyed straight from the stance
# to HIGH, the grip's path runs through the shoulder itself and the arm folds and flips there for a frame. The weapon
# given by its roll about the forearm (by_ways), chosen between the stance's (3) and the raised arm's (-35) so the fist
# turns evenly on the way (with the weapon's own way in space, a roll of 52, it spun 87 degrees in four frames).
BEFORE_ROLLED = ((-0.75, 0.05, -0.66), (0.15, -0.90, 0.40), 12.0, 32)


# The right arm in the ready stance, in arm()'s terms (the elbow back, the forearm forward and a little down, the blade
# forward and up, tipped on by the wrist), for a clip that keys the hand all the way through.
LOW = ((-0.233, 0.709, -0.665), (-0.045, -0.942, -0.333), (0.171, -0.802, 0.572), 34)


ACROSS = norm((1.0, 0.0, -0.12))          # the two-handed swing's axis: a little down to the left, so the cut runs across
SECOND = -0.14                            # the left hand's place on the grip, metres from the right hand's


def cross(a, b):
    return [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]]


def in_trunk(point, yaw, lean, hips, room=0.05):
    """How deep a point lies in the trunk, roughly: an oval post from the hips to the shoulders, turned and leant with the trunk."""
    x, y, z = point
    if not HIPS[2] - 0.05 + hips[2] < z < HIPS[2] + 0.59 + hips[2]:
        return 0.0
    y -= HIPS[1] + hips[1] - (z - HIPS[2] - hips[2]) * math.tan(math.radians(READY_LEAN + lean))
    x -= HIPS[0] + hips[0]
    turn = math.radians(yaw + READY_TURN)
    side, depth = x * math.cos(turn) + y * math.sin(turn), -x * math.sin(turn) + y * math.cos(turn)
    return max(0.0, 1.0 - math.sqrt((side / (0.172 + room)) ** 2 + (depth / (0.118 + room)) ** 2))


def two_hands(aim, near, yaw=0.0, lean=0.0, hips=(0.0, 0.0, 0.0)):
    """Both hands' key for a blade pointing `aim`, its edge leading, the right hand's grip as near `near` as the arms allow:
    the upper arm's way and the wrist's bend (25 to 50 degrees) searched so the left hand can still reach its place on the
    grip, the arms clear of the trunk and the forearm not wrung."""
    aim = norm(aim)
    edge = norm(cross(ACROSS, aim))
    right, left = shoulder("R", yaw, lean, hips), shoulder("L", yaw, lean, hips)
    best = None
    for bend in (25, 30, 35, 40, 45, 50):
        w = math.radians(bend)
        fore = [math.cos(w) * e + math.sin(w) * a for e, a in zip(edge, aim)]
        for i in range(0, 181, 6):                      # the upper arm's way: every direction, in steps
            for j in range(0, 360, 8):
                polar, around = math.radians(i), math.radians(j)
                upper = [math.sin(polar) * math.cos(around), math.sin(polar) * math.sin(around), math.cos(polar)]
                grip = [s + UPPER * u + FORE * f + HAND * e for s, u, f, e in zip(right, upper, fore, edge)]
                other = [g + SECOND * a for g, a in zip(grip, aim)]
                reach = math.dist(other, left)
                folded = sum(u * f for u, f in zip(upper, fore))          # an elbow folded shut (the forearm back along the upper arm)
                # the forearm's turn: the blade leaves the fist on the thumb's side, which lies opposite the elbow's point
                # when the forearm is at ease; how far the blade is turned from there about the forearm
                chord = norm([UPPER * u + FORE * f for u, f in zip(upper, fore)])
                along = sum(u * c for u, c in zip(upper, chord))
                point = [u - along * c for u, c in zip(upper, chord)]
                ease = [-(q - sum(point[k] * fore[k] for k in range(3)) * f) for q, f in zip(point, fore)]
                thumb = [a - sum(aim[k] * fore[k] for k in range(3)) * f for a, f in zip(aim, fore)]
                size = math.sqrt(sum(c * c for c in ease)) * math.sqrt(sum(c * c for c in thumb))
                turned_ = math.degrees(math.acos(max(-1.0, min(1.0, sum(p * q for p, q in zip(ease, thumb)) / size)))) if size > 1e-9 else 0.0
                elbow = [s + UPPER * u for s, u in zip(right, upper)]
                wrist = [e + FORE * f for e, f in zip(elbow, fore)]
                middle_ = [(e + w) / 2 for e, w in zip(elbow, wrist)]
                crowded = sum(in_trunk(q, yaw, lean, hips) for q in (elbow, middle_, wrist, grip, other))
                cost = (math.dist(grip, near) ** 2 + 12 * max(0.0, reach - 0.55) ** 2 + 0.3 * max(0.0, -0.3 - folded) ** 2 + 2.0 * crowded
                        + 0.004 * max(0.0, upper[2]) + 0.00002 * abs(bend - 38)      # elbows rather down than up; a middling wrist
                        + 2.0 * max(0.0, upper[0] - 0.30) ** 2                       # the right upper arm doesn't cross the chest
                        + 0.5 * max(0.0, upper[1] - 0.55) ** 2                       # nor reach far behind
                        + 0.00004 * max(0.0, turned_ - 75.0) ** 2)                   # the forearm not wrung past what it turns
                if best is None or cost < best[0]:
                    best = (cost, bend, upper)
    _, bend, upper = best
    w = math.radians(bend)
    fore = [math.cos(w) * e + math.sin(w) * a for e, a in zip(edge, aim)]
    hand = arm(right, upper, fore, aim, wrist=bend)
    return {"R": hand, "L": {"on": "R", "along": SECOND, "elbow": [0.55, 0.35, -0.75]}}


# ---- the hands at the ready stance and beside it ----------------------------------------------------------------------
CLOSED = {"fingers.L": [0, 0, 23], "thumb.L": [0, 0, -10]}       # the left hand closed as the right is (the ready stance's left holds a strap)
# Bare fists: the fingers (one piece on this figure) folded right back onto the palm and the thumbs laid over them. Closed
# only as far as a grip (85 degrees) an empty hand reads as a limp, bent hand; around a haft the haft fills the gap.
FISTS = {"fingers.L": [0, 0, 103], "thumb.L": [0, 0, -50], "fingers.R": [0, 0, -80], "thumb.R": [0, 0, 40]}


def turned(v, yaw):
    """A direction turned with the trunk: `yaw` degrees to the figure's left about the upright."""
    c, k = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    return [v[0] * c - v[1] * k, v[0] * k + v[1] * c, v[2]]


# The shield arm in arm()'s terms: as the ready stance has it, pushed out before the body (covering while the weapon is
# drawn back), and swung out and back to the left (the counterweight of a blow).
SHIELD = ((0.586, -0.678, -0.445), (-0.936, -0.333, -0.121), (-0.10, -0.10, 0.99))
COVER = ((0.45, -0.82, -0.35), (-0.88, -0.45, 0.15), (-0.10, -0.10, 0.99))
OPEN = ((0.78, -0.25, -0.57), (-0.45, -0.80, -0.40), (0.15, -0.20, 0.97))
# The shield held square before the body (the guard): the elbow out before the chest, the forearm level across to the
# right, so the shield on it stands in the body's middle with its face to the foe (the user, 2026-11-17: "i want the
# shield to be fully in front of the character").
FRONT = ((-0.05, -0.85, -0.52), (-0.97, -0.10, 0.22), (0.0, 0.10, 0.99))


# The weapon arm in arm()'s terms beside its stance (LOW): swung forward (a runner's arm, a reach), thrown out wide to the
# right (a stagger), and hanging with the point sunk (stunned).
FORWARD_R = ((-0.15, -0.35, -0.92), (-0.02, -0.80, 0.60), (0.10, -0.55, 0.83), 20)
WIDE_R = ((-0.85, 0.15, -0.50), (-0.55, -0.55, -0.63), (-0.30, -0.75, 0.59), 20)
DROOP_R = ((-0.12, 0.10, -0.99), (0.0, -0.50, -0.87), (0.0, -0.87, 0.50), 55)
DROOP_L = ((0.30, -0.15, -0.94), (-0.35, -0.55, -0.76), (-0.30, -0.30, 0.90))


def sword_arm(yaw=0.0, lean=0.0, hips=(0.0, 0.0, 0.0), forward=0.0, wide=0.0, droop=0.0):
    """The weapon arm's grip as the ready stance has it, carried with the trunk; `forward`, `wide`, `droop` 0..1 toward
    the arm swung forward, thrown out wide, hanging (negative `forward`: drawn further back)."""
    rows = list(zip(LOW, FORWARD_R, WIDE_R, DROOP_R))
    pose = [[a + (b - a) * forward + (c - a) * wide + (e - a) * droop for a, b, c, e in zip(*vectors)] for vectors in rows[:3]]
    low, ahead, out, down = rows[3]
    return arm(shoulder("R", yaw, lean, hips), *[turned(v, yaw) for v in pose], wrist=low + (ahead - low) * forward + (out - low) * wide + (down - low) * droop)


def shield_arm(yaw=0.0, lean=0.0, hips=(0.0, 0.0, 0.0), cover=0.0, open_=0.0, droop=0.0, front=0.0):
    """The shield arm's grip, carried with the trunk: `cover` 0..1 toward pushed out, `open_` 0..1 toward swung out,
    `droop` toward hanging low, `front` toward held square before the body."""
    pose = [[a + (b - a) * cover + (c - a) * open_ + (e - a) * droop + (f - a) * front for a, b, c, e, f in zip(*vectors)]
            for vectors in zip(SHIELD, COVER, OPEN, DROOP_L, FRONT)]
    return arm(shoulder("L", yaw, lean, hips), *[turned(v, yaw) for v in pose])


def arms(yaw=0.0, lean=0.0, hips=(0.0, 0.0, 0.0), forward=0.0, wide=0.0, cover=0.0, open_=0.0):
    """Both arms carried with the trunk: the weapon's swung `forward` or thrown `wide`, the shield's pushed out or swung open."""
    return {"R": sword_arm(yaw, lean, hips, forward=forward, wide=wide), "L": shield_arm(yaw, lean, hips, cover=cover, open_=open_)}


def right(pose, yaw=0.0, lean=0.0, hips=(0.0, 0.0, 0.0), carried=False):
    """The weapon arm from its directions (arm()'s: upper arm, forearm, weapon, wrist degrees); `carried`: the directions
    are the chest's own and turn with the trunk."""
    ways = [turned(v, yaw) for v in pose[:3]] if carried else pose[:3]
    return arm(shoulder("R", yaw, lean, hips), *ways, wrist=pose[3] if len(pose) > 3 else 0)


def rear(more=0.0, turn=0.0, back=0.0, left=0.0):
    """The rear (right) foot as a key's delta: its heel `more` degrees further up than in the stance (its toe stays where
    it is), turned `turn` degrees (a pivot on the ball as the hips come round), slid `back` and `left` metres."""
    return [left, back, on_toes(10 + more) - on_toes(10), -more, turn]


def middle(yaw=0.0, lean=0.0, hips=(0.0, 0.0, 0.0), right=0.0, forward=0.0, up=0.0):
    """A place given from between the shoulders: `right`, `forward` and `up` metres from there in the figure's own space."""
    a, b = shoulder("L", yaw, lean, hips), shoulder("R", yaw, lean, hips)
    return [(a[0] + b[0]) / 2 - right, (a[1] + b[1]) / 2 - forward, (a[2] + b[2]) / 2 + up]


def drift(frame, late=0.0, frames=120):
    """A slow round of two numbers in -1..1 (a point's small circle in time), `late` radians after the idle's breath."""
    p = 2 * math.pi * frame / frames - late
    return math.sin(p), math.cos(p)


# ---- the ready stances per weapon kind (the user's Q56; the second ones, 2026-11-17: ready to fight, nothing held still) --
# Each is a loop of the arms as long as the idle (GM2-P21): `hands(frame)` gives the grips at a frame (they wander a little,
# late after the breath), `pose` what the fingers do, `gear` what the pictures put in the hands. The battle lays the loop
# over the arms of whatever else plays; a weapon's own attack starts and ends in its stance's first frame.
def _two_handed(frame):
    # the long grip low before the belly, a little to the right, the point up at the foe's face, drifting in a small round
    a, b = drift(frame, 0.9)
    return two_hands((0.08 + 0.02 * b, -0.78, 0.62 + 0.03 * a), (-0.06, -0.31 - 0.008 * b, 1.04 + 0.010 * a), 0, 0)


def _staff(frame):
    # the staff forward in the right hand, its head up toward the foe, the elbow back and the forearm level behind it;
    # the left hand out before the chest, its palm up and cupped, ready to cast
    a, b = drift(frame, 0.9)
    c, e = drift(frame, 1.6)
    return {"R": {"at": [-0.25, -0.28 - 0.008 * b, 1.03 + 0.010 * a], "aim": norm((0.12 + 0.02 * b, -0.50, 0.86 + 0.02 * a)), "edge": norm((0.0, -0.86, -0.50)),
                  "elbow": [-0.24, 0.17, -0.95]},
            "L": {"at": [0.10 + 0.008 * e, -0.48, 1.22 + 0.012 * c], "aim": norm((0.90, -0.10, 0.40)), "edge": norm((-0.10, -0.90, 0.42)), "elbow": [0.45, 0.30, -0.84]}}


def _unarmed(frame):
    # the fists up, the elbows down: the left out before the chin, the right back by the jaw; they weave a little
    a, b = drift(frame, 0.8)
    c, e = drift(frame, 2.0)
    return {"R": arm(shoulder("R"), (-0.10, -0.60, -0.79), (0.30 + 0.03 * e, -0.15 - 0.04 * c, 0.94), (0.35, 0.55, 0.76)),
            "L": arm(shoulder("L"), (0.05, -0.70 - 0.03 * a, -0.71), (-0.35 + 0.03 * b, -0.40 - 0.04 * a, 0.85), (-0.35, 0.55, 0.76))}


JAW = [0.065, 0.0, -0.03]                 # by the right corner of the jaw, in the head's frame (x to the figure's right, y up, z back)
BEHIND = [0.095, 0.0, 0.09]               # behind the ear: where the loosing hand flies to


def bow_frame(bow_):
    """The bow's own ways from its hand's key: the stave's (up the bow), the arrow's, and across (to the figure's left)."""
    stave = norm(bow_["aim"])
    arrow = norm(_perp(bow_["edge"], stave))
    return stave, arrow, _cross(stave, arrow)


def string_hand(bow_, drawn=0.0, to=None, elbow=(0.17, -0.95, -0.25)):
    """The bow's string hand: on the string's middle (0.16 m behind the bow's grip along the arrow's way), or `drawn` 0..1
    of the way from there to a place by the head (`to`, in the head's frame). Its fist lies along the string with the
    thumb's side down (thumb up, the forearm is turned 70 degrees about itself on the string and 133 at the jaw), and its
    knuckles look across the bow on the string and round toward the arrow's way as it draws: keyed, because left to the
    solver the arm's answer snapped between two frames (as measured on the solver's own answers at both ends)."""
    stave, arrow, across = bow_frame(bow_)
    knuckles = norm([a * (1.0 - 0.3 * drawn) + w * (0.75 * drawn) for a, w in zip(across, arrow)])
    spec = {"at": [p - 0.16 * w for p, w in zip(bow_["at"], arrow)], "aim": [-c for c in stave], "edge": knuckles, "elbow": list(elbow)}
    if to is not None:
        spec.update({"at_head": list(to), "follow": drawn})
    return spec


def _bow(frame):
    # the bow low and forward in the left hand, canted, an arrow on the string; the right hand on the string, ready to draw
    a, b = drift(frame, 0.9)
    bow_ = {"at": [0.12 + 0.006 * b, -0.48, 1.02 + 0.010 * a], "aim": norm((0.35, -0.38, 0.86)), "edge": norm((-0.05, -0.90 + 0.02 * a, -0.43)),
            "elbow": [0.50, 0.30, -0.81]}
    return {"L": bow_, "R": string_hand(bow_)}


def _crossbow(frame):
    # low before the right hip, pointing forward, down and in: the right hand at the grip under the stock, the forearm
    # along the stock and the butt over it; the left hand closed around the stock ahead of the grip
    a, b = drift(frame, 0.9)
    return {"R": {"at": [-0.18, -0.21 - 0.006 * b, 1.00 + 0.010 * a], "aim": norm((0.20, -0.27, 0.87)), "edge": norm((0.52, -0.77, -0.37 + 0.02 * a)),
                  "elbow": [-0.10, 0.10, -0.99], "swing": False},
            # the fist around the stock with its thumb's side to the butt: the other way round the forearm is turned 113
            # degrees about itself, this way 56 (the measure)
            "L": {"on": "R", "offset": [0.0, 0.040, 0.18], "axis": "Z", "flip": True, "elbow": [0.45, 0.30, -0.84]}}


STANCES = {
    "two-handed": {"hands": _two_handed, "pose": dict(CLOSED), "gear": "greatsword"},
    "staff": {"hands": _staff, "pose": {"fingers.L": [0, 0, -28], "thumb.L": [0, 0, 26]}, "gear": "staff"},
    "unarmed": {"hands": _unarmed, "pose": dict(FISTS), "gear": "none"},
    "bow": {"hands": _bow, "pose": dict(CLOSED, **{"fingers.R": [0, 0, 25]}), "gear": "bow@hand.L"},
    "crossbow": {"hands": _crossbow, "pose": dict(CLOSED), "gear": "crossbow"},
}

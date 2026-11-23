# The reactions (GM2-P23; Docs/Game/Hero.md, "How it works", step 4b's clip table): hit, hit critically, stumbling, dying.
# On the ready stance, in the user's manner (2026-11-17: the whole body moves, nothing held still). Plain Python; sources()
# gives their keying sources (poses.source).
from grips import arms, rear, shield_arm, sword_arm, trunk
from poses import LAG, READY, key, on_toes, place, source


def hurt():
    """A flinch: the chest thrown back and round, the arms jolted, a breath taken low, the stance again."""
    return [
        key(0),
        key(3, {**trunk(10, -16), "head": [12, -8, 0], "shoulder.L": [0, 0, 5], "shoulder.R": [0, 0, -5]}, hips=[0, 0.04, -0.012],
            hands=arms(10, -16, (0, 0.04, -0.012), wide=0.25, open_=0.5)),
        key(10, {**trunk(-3, 7), "head": [-4, 2, 0]}, hips=[0, 0.014, -0.028], hands=arms(-3, 7, (0, 0.014, -0.028), cover=0.4)),
        key(17, trunk(0, 2), hips=[0, 0.004, -0.008], hands=arms(0, 2, (0, 0.004, -0.008), cover=0.15)),
        key(24)]


def critical_hurt():
    """The blow turns the body and throws it back a step before it doubles over and gathers itself."""
    return [
        key(0),
        key(4, {**trunk(24, -24), "head": [18, 8, 0], "shoulder.L": [0, 0, 6], "shoulder.R": [0, 0, -6]}, hips=[0.012, 0.085, -0.02],
            feet={"R": [-0.01, 0.10, -0.02, 6, -6]}, hands=arms(24, -24, (0.012, 0.085, -0.02), wide=0.7, open_=0.9)),
        key(9, {**trunk(18, -14), "head": [8, 4, 0]}, hips=[0.008, 0.10, -0.05], feet={"R": [-0.01, 0.11, -0.03, 8, -6]},
            hands=arms(18, -14, (0.008, 0.10, -0.05), wide=0.5, open_=0.7)),
        key(16, {**trunk(-4, 22), "head": [-14, 0, 0]}, hips=[0.0, 0.06, -0.09], feet={"R": [-0.01, 0.10, -0.02, 6, -6]},
            hands=arms(-4, 22, (0.0, 0.06, -0.09), cover=0.3)),
        key(30, {**trunk(0, 9), "head": [-4, 0, 0]}, hips=[0, 0.025, -0.04], feet={"R": [-0.005, 0.045, 0, 0, -3]}, hands=arms(0, 9, (0, 0.025, -0.04), cover=0.2)),
        key(38, trunk(0, -1), hips=[0, 0.0, 0.006], hands=arms(0, -1, (0, 0.0, 0.006))),
        key(44)]


def stumble():
    """A lurch forward caught on the front leg, the arms thrown wide, and the body hauled back up."""
    return [
        key(0),
        key(8, {**trunk(-8, 22), "head": [12, 0, 0]}, hips=[0.015, -0.11, -0.05], feet={"L": [0.01, -0.14, 0, 0, 0], "R": rear(16, 4)},
            hands=arms(-8, 22, (0.015, -0.11, -0.05), wide=0.9, open_=1.0)),
        key(18, {**trunk(-4, 30), "head": [16, 0, 0]}, hips=[0.0, -0.125, -0.12], feet={"L": [0.01, -0.14, 0, 0, 0], "R": rear(22, 6)},
            hands=arms(-4, 30, (0.0, -0.125, -0.12), wide=0.6, open_=0.7)),
        key(32, trunk(0, 10), hips=[0, -0.045, -0.045], feet={"L": [0.005, -0.06, 0, 0, 0], "R": rear(8, 2)}, hands=arms(0, 10, (0, -0.045, -0.045), open_=0.2)),
        key(40, trunk(0, -2), hips=[0, 0.006, 0.006], hands=arms(0, -2, (0, 0.006, 0.006))),
        key(46)]


# ---- death: struck, the knees give, the body falls back and lies; the last frame is held -------------------------------
# No root motion: the feet stay about their place and the body comes down behind them, head away from the foe. From the
# knees' giving on, the trunk and the feet are given as they are to be (from rest), not from the stance.
# The fallen body lies on the ground, which is where the boots' soles are at rest (0.024 m under the feet's own): how far
# the fallen body, its fist and its heels are let down onto it, and the head laid back onto it (metres, degrees). (Keyed
# without a ground in the pictures, all of it hung 7 to 9 cm above: the clip library's worker measured it, 2026-11-18.)
SUNK, FIST, HEELS = 0.062, 0.08, 0.03
NOD, LAID = 4.0, 12.0


def absolute(pose):
    """A key's deltas for bones given as they are to be (from rest), whatever the ready stance has them at."""
    return {name: [a - b for a, b in zip(values, READY["pose"].get(name, [0, 0, 0]))] for name, values in pose.items()}


def death():
    limp_r = absolute({"upper_arm.R": [0, 0, -16], "forearm.R": [-18, 0, 0], "hand.R": [0, 0, 0]})
    kept_l = absolute({"upper_arm.L": [-10, 22, 30], "forearm.L": [-56, 0, 0], "hand.L": [0, 0, 0]})      # the shield arm stays before the body as it falls
    thrown_l = absolute({"upper_arm.L": [-26, 22, 22], "forearm.L": [-70, 0, 0], "hand.L": [0, 0, 0]})    # thrown up a little as the seat goes down
    laid_l = {**absolute({"upper_arm.L": [0, 24, 34], "forearm.L": [-60, 0, 0], "hand.L": [0, 0, 0]}), "fingers.R": [0, 0, 30]}   # and lies by it, the shield leaning on the body
    # its back, seat and heels on the ground, the head laid back onto it, the fist on it with the weapon's point sunk to it
    flat = (0.0, 0.485, -0.705 - SUNK)

    def trunk_(hips, spine, chest, neck, head):
        return absolute({"hips": hips, "spine": spine, "chest": chest, "neck": neck, "head": head})

    def feet(left, right_):
        return {"L": place("L", *left), "R": place("R", *right_)}

    # the weapon's arm on the ground along the body, the weapon on the ground beside it, its point out and to the head: the
    # one way the fist can lie there with the forearm unwrung (its point to the feet turns the forearm 145 degrees about
    # itself). How the fist is turned about the weapon is the solver's.
    def fallen(up=0.0, along=0.0):
        # the arm nearly straight and its elbow out to the side (bent more, the elbow swung in under the back)
        return {"R": {"at": [-0.34, 0.56 + along, 0.10 - FIST + up], "aim": [-0.50, 0.87, -0.045 + 0.3 * up], "untwist": 0.7, "elbow": [-0.90, -0.20, 0.20]}}

    return [
        key(0),
        # struck: the chest and head thrown back, the arms jolted loose
        key(4, {"chest": [12, 0, 0], "spine": [6, 0, 0], "neck": [4, 0, 0], "head": [10, 0, 0]}, hips=[0.0, 0.035, 0.012],
            hands={"R": sword_arm(0, -16, (0.0, 0.035, 0.012), wide=0.3), "L": shield_arm(0, -16, (0.0, 0.035, 0.012), open_=0.4)}),
        # the knees give: the hips drop and go back, the trunk curls
        key(12, {**trunk_([8, -18, 0], [-6, 2, 0], [-10, 2, 0], [0, 3, 0], [-4, 6, 0]), **limp_r, **kept_l}, hips=[0.0, 0.135, -0.175],
            feet=feet((0.04, -0.22, 0.0, 6, 8), (-0.06, 0.18, on_toes(12), -12, -36))),
        key(20, {**trunk_([28, -12, 0], [-7, 1, 0], [-12, 1, 0], [-2, 2, 0], [-8, 4, 0]), **limp_r, **thrown_l}, hips=[0.0, 0.295, -0.515 - 0.7 * SUNK],
            feet=feet((0.03, -0.20, 0.04, 24, 10), (-0.05, 0.11, 0.025, 6, -30))),
        # the seat meets the ground
        key(26, {**trunk_([48, -6, 0], [-6, 0, 0], [-10, 0, 0], [-1, 0, 0], [-6, 2, 0]), **limp_r, **thrown_l}, hips=[0.0, 0.395, -0.60 - 0.85 * SUNK],
            feet=feet((0.04, -0.24, 0.06, 44, 14), (-0.05, 0.04, 0.02, 10, -32))),
        # the back and the head come down, the weapon's arm with them
        key(34, {**trunk_([87, 0, 0], [2, 0, 0], [3, 0, 0], [3, 0, 0], [8 - NOD, 0, 0]), **laid_l}, hips=flat,
            feet=feet((0.06, -0.30, 0.07 - SUNK, 56, 24), (-0.06, -0.02, 0.02 - SUNK, 14, -36)), hands=fallen(up=0.03)),
        # a small rebound of the chest and head
        key(39, {**trunk_([85, 0, 0], [-1, 0, 0], [-3, 0, 0], [-3, 0, 0], [-5, 8, 0]), **laid_l}, hips=flat,
            feet=feet((0.07, -0.32, 0.06 - SUNK, 58, 30), (-0.07, -0.06, 0.02 - SUNK, 18, -40)), hands=fallen(up=0.05, along=-0.01)),
        # lying: the head rolled to the side, the bent leg sliding out
        key(48, {**trunk_([87, 0, 0], [1, 0, 0], [2, 0, 0], [2 + LAID / 2, 6, 0], [5 + LAID / 2, 22, 0]), **laid_l}, hips=flat,
            feet=feet((0.08, -0.33, 0.05 - SUNK - HEELS, 60, 34), (-0.08, -0.20, 0.03 - SUNK - HEELS, 34, -44)), hands=fallen()),
        # (half way through the slide, so the sliding heel is put on the ground there too: between two keys it sank 8 mm)
        key(54, {**trunk_([87, 0, 0], [1, 0, 0], [2, 0, 0], [2 + LAID / 2, 6.5, 0], [5 + LAID / 2, 23, 0]), **laid_l}, hips=flat,
            feet=feet((0.08, -0.33, 0.05 - SUNK - HEELS, 60, 34), (-0.085, -0.26, 0.04 - SUNK - HEELS, 45, -47)), hands=fallen()),
        key(60, {**trunk_([87, 0, 0], [1, 0, 0], [2, 0, 0], [2 + LAID / 2, 7, 0], [5 + LAID / 2, 24, 0]), **laid_l}, hips=flat,
            feet=feet((0.08, -0.33, 0.05 - SUNK - HEELS, 60, 34), (-0.09, -0.32, 0.05 - SUNK - HEELS, 56, -50)), hands=fallen())]


def sources():
    return [
        source("hurt", 24, hurt(), fine=1, lag=LAG),
        source("critical-hurt", 44, critical_hurt(), fine=1, lag=LAG),
        source("stumble", 46, stumble(), markers={"step": 8}, fine=1, lag=LAG),
        source("death", 60, death(), markers={"fall": 26, "down": 34}),
    ]

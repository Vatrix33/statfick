# The melee attacks (GM2-P23; Docs/Game/Hero.md, "How it works", step 4b's clip table: which weapon kind plays which).
# The second ones (2026-11-17; the first were "stiff"): every blow is the whole body's. It loads onto the rear leg and
# coils away from the foe, the rear leg drives the hips forward and down into a lunge on the front foot while the trunk
# comes round and leans in, the arm comes last, the blow goes on past the hit, and the body is drawn back a little past
# its stance before it settles. The trunk, the neck and the head follow the hips late (poses.LAG). An attacking arm is
# keyed by its grip, from the arm's own directions in the figure's space (grips.arm). An attack of a weapon kind with a
# stance loop starts and ends in that stance's first frame (GM2-P21). Plain Python; sources() gives their keying sources.
from grips import (LOW, STANCES, arm, between, by_ways, continuous_rolls, middle, mix, rear, right, roll_of, shield_arm, shoulder, sword_arm,
                   trunk, two_hands, ways_to_grips)
from poses import LAG, READY, key, source


# swing: a one-handed cut from over the right shoulder down across to the left. The blade is square to the forearm in a
# closed fist, so the cut lands late in the arc: the forearm already points down and forward, the blade at the foe.
def swing():
    load, drive, land, hit, past, hang, back, over = ((0.0, 0.05, -0.025), (0.0, -0.03, -0.045), (0.0, -0.13, -0.065), (0.0, -0.18, -0.08),
                                                      (0.0, -0.195, -0.09), (0.0, -0.17, -0.075), (0.0, -0.05, -0.025), (0.0, 0.012, 0.008))
    up = ((-0.80, 0.35, 0.45), (0.20, 0.25, 0.95), (0.10, 0.90, 0.42))
    high = ((-0.65, -0.30, 0.70), (0.10, -0.10, 0.99), (0.05, 0.95, 0.30))
    over_ = ((-0.25, -0.85, 0.45), (0.15, -0.90, 0.40), (0.0, 0.35, 0.94), 15)
    cut = ((0.0, -0.90, -0.43), (0.25, -0.82, -0.51), (0.1, -0.6, 0.8), 38)
    through = ((0.30, -0.55, -0.78), (0.62, -0.38, -0.69), (0.5, -0.8, 0.3), 30)
    lunge = {"L": [0.0, -0.20, 0.0, 0, 0], "R": rear(18, 10)}
    return [
        key(0),
        key(9, trunk(-30, -6), hips=load, feet={"R": rear(-8), "L": [0.0, 0.0, 0.012, -6, 0]}, hands={"R": right(up, -30, -6, load), "L": shield_arm(-30, -6, load, cover=1.0)}),
        key(12, trunk(-14, 2), hips=drive, feet={"L": [0.0, -0.09, 0.05, 8, 0], "R": rear(6)}, hands={"R": right(high, -14, 2, drive), "L": shield_arm(-14, 2, drive, cover=0.7)}),
        key(15, trunk(8, 12), hips=land, feet=lunge, hands={"R": right(over_, 8, 12, land), "L": shield_arm(8, 12, land, open_=0.3)}),
        key(17, trunk(22, 20), hips=hit, feet=lunge, hands={"R": right(cut, 22, 20, hit), "L": shield_arm(22, 20, hit, open_=0.8)}),
        key(20, trunk(32, 24), hips=past, feet={"L": [0.0, -0.20, 0.0, 0, 0], "R": rear(22, 14)}, hands={"R": right(through, 32, 24, past), "L": shield_arm(32, 24, past, open_=1.0)}),
        key(25, trunk(28, 20), hips=hang, feet=lunge, hands={"R": right(through, 28, 20, hang), "L": shield_arm(28, 20, hang, open_=0.85)}),
        key(33, trunk(6, 5), hips=back, feet={"L": [0.0, -0.07, 0.035, 5, 0], "R": rear(6, 3)},
            hands={"R": between(through, LOW, 0.7, 6, 5, back), "L": shield_arm(6, 5, back, open_=0.2)}),
        key(39, trunk(-3, -2), hips=over, hands={"R": right(LOW, -3, -2, over), "L": shield_arm(-3, -2, over, cover=0.15)}),
        key(46)]


# thrust: the blade driven straight at the foe from a fist drawn back by the hip, on a deep lunge. A closed fist holds a
# blade square to the forearm, thumb side up, so the arm reaches down and forward and the wrist tips the blade level.
def thrust():
    load, drive, hit, held, back, over = (0.0, 0.05, -0.03), (0.0, -0.05, -0.05), (0.0, -0.22, -0.09), (0.0, -0.215, -0.09), (0.0, -0.07, -0.03), (0.0, 0.012, 0.006)
    drawn = ((-0.20, 0.80, -0.57), (0.0, -0.60, -0.80), (0.0, -0.8, 0.6), 35)
    going = ((-0.12, 0.0, -0.99), (0.05, -0.80, -0.60), (0.0, -0.6, 0.8), 42)
    out = ((0.02, -0.80, -0.60), (0.10, -0.93, -0.35), (0.0, -0.35, 0.93), 52)
    lunge = {"L": [0.0, -0.28, 0.0, 0, 0], "R": rear(22, 10)}
    return [
        key(0),
        key(8, trunk(-26, 4), hips=load, feet={"R": rear(-8), "L": [0.0, 0.0, 0.012, -6, 0]}, hands={"R": right(drawn, -26, 4, load), "L": shield_arm(-26, 4, load, cover=1.0)}),
        key(11, trunk(-8, 10), hips=drive, feet={"L": [0.0, -0.14, 0.05, 8, 0], "R": rear(8)}, hands={"R": right(going, -8, 10, drive), "L": shield_arm(-8, 10, drive, cover=0.4)}),
        key(14, trunk(24, 22), hips=hit, feet=lunge, hands={"R": right(out, 24, 22, hit), "L": shield_arm(24, 22, hit, open_=0.9)}),
        key(19, trunk(22, 21), hips=held, feet=lunge, hands={"R": right(out, 22, 21, held), "L": shield_arm(22, 21, held, open_=1.0)}),
        key(27, trunk(6, 6), hips=back, feet={"L": [0.0, -0.10, 0.035, 5, 0], "R": rear(6, 3)},
            hands={"R": between(out, LOW, 0.65, 6, 6, back), "L": shield_arm(6, 6, back, open_=0.2)}),
        key(33, trunk(-3, -1), hips=over, hands={"R": right(LOW, -3, -1, over), "L": shield_arm(-3, -1, over, cover=0.15)}),
        key(40)]


# stab: a dagger's short upward stab from the hip, quick, on a short lunge
def stab():
    load, hit, held, back, over = (0.0, 0.03, -0.035), (0.0, -0.15, -0.075), (0.0, -0.15, -0.08), (0.0, -0.04, -0.02), (0.0, 0.01, 0.005)
    drawn = ((-0.25, 0.55, -0.80), (0.0, -0.85, -0.53), (0.0, -0.53, 0.85))
    up = ((0.10, -0.80, -0.59), (0.15, -0.97, 0.20), (0.0, 0.2, 0.98), 40)
    lunge = {"L": [0.0, -0.18, 0.0, 0, 0], "R": rear(18, 8)}
    return [
        key(0),
        key(5, trunk(-18, 6), hips=load, feet={"R": rear(-6)}, hands={"R": right(drawn, -18, 6, load), "L": shield_arm(-18, 6, load, cover=0.8)}),
        key(9, trunk(20, 16), hips=hit, feet=lunge, hands={"R": right(up, 20, 16, hit), "L": shield_arm(20, 16, hit, open_=0.7)}),
        key(13, trunk(22, 17), hips=held, feet=lunge, hands={"R": right(((0.12, -0.80, -0.59), (0.15, -0.95, 0.27), (0.0, 0.27, 0.96), 44), 22, 17, held), "L": shield_arm(22, 17, held, open_=0.8)}),
        key(20, trunk(4, 4), hips=back, feet={"L": [0.0, -0.06, 0.03, 4, 0], "R": rear(5, 2)}, hands={"R": between(up, LOW, 0.7, 4, 4, back), "L": shield_arm(4, 4, back, open_=0.15)}),
        key(25, trunk(-2, -1), hips=over, hands={"R": right(LOW, -2, -1, over), "L": shield_arm(-2, -1, over)}),
        key(30)]


# The staff's stance's left arm in arm()'s terms (read off the posed stance), reaching a little forward, hanging as it
# swings past the side, and thrown back and out behind the body (the weapon's way as its roll about the forearm: the palm
# up is -73, at ease 0)
CUPPED = ((0.082, -0.239, -0.967), (-0.283, -0.793, 0.540), (0.909, -0.101, 0.404))
REACH = ((0.10, -0.45, -0.89), (-0.25, -0.85, 0.46), (0.909, -0.101, 0.404))
PASSING = ((0.25, 0.15, -0.96), (0.05, -0.45, -0.89), -45.0)
THROWN = ((0.45, 0.75, -0.48), (0.25, 0.20, -0.95), -20.0)


def toward(a, b, t):
    """An arm part of the way from one arm()-pose to another, the weapon's way as its roll about the forearm."""
    rolls = [float(p[2]) if isinstance(p[2], (int, float)) else roll_of(p) for p in (a, b)]
    return (mix(a[0], b[0], t), mix(a[1], b[1], t), rolls[0] + (rolls[1] - rolls[0]) * t)


# strike: a staff (or a wand) brought down from over the right shoulder, from the staff's stance and back to it; the left
# hand, open, is thrown back as the staff comes down
def strike():
    held = STANCES["staff"]
    rest, stance = dict(held["pose"]), held["hands"](0)
    load, drive, hit, past, back, over = (0.0, 0.05, -0.02), (0.0, -0.04, -0.04), (0.0, -0.16, -0.07), (0.0, -0.175, -0.08), (0.0, -0.04, -0.02), (0.0, 0.012, 0.006)
    lunge = {"L": [0.0, -0.18, 0.0, 0, 0], "R": rear(18, 10)}

    # The left arm is keyed by its own ways all through (grips.by_ways): from the stance's cupped hand it reaches a little
    # as the staff goes up, swings down past the side and is thrown back and out behind the body as the staff comes down,
    # the palm turning in, and comes back the same way. (Keyed by its place with the stance's hand held as it was, the
    # wrist was folded 135 degrees for ten frames around the hit and the elbow flipped sides twice: the measure found it
    # after the clip's approval, 2026-11-18.)
    def left(pose, yaw=0.0, lean=0.0):
        return by_ways(pose, yaw, lean)

    def body(yaw, lean):
        return {**trunk(yaw, lean), **rest}

    carried = ((-0.25, 0.08, -0.97), (0.05, -0.95, 0.30), (0.13, -0.53, 0.84), 48)        # the stance's own arm, in arm()'s terms
    raised = ((-0.55, 0.20, 0.81), (0.25, 0.15, 0.96), (0.0, 1.0, 0.0), 30)
    over_ = ((-0.45, -0.50, 0.74), (0.05, -0.55, 0.83), (0.0, 0.8, 0.6), 12)
    down = ((-0.30, -0.88, -0.37), (0.0, -0.85, -0.52), (0.0, -0.52, 0.85), 42)
    low = ((-0.25, -0.80, -0.55), (0.05, -0.75, -0.66), (0.0, -0.66, 0.75), 40)
    return [
        key(0, rest, hands={"R": stance["R"], "L": left(CUPPED)}),
        key(5, body(-12, -3), hips=(0.0, 0.03, -0.012), hands={"R": between(carried, raised, 0.45, -12, -3, (0, 0.03, -0.012)), "L": left(toward(CUPPED, REACH, 0.5), -12, -3)}),
        key(10, body(-24, -8), hips=load, feet={"R": rear(-8), "L": [0.0, 0.0, 0.012, -6, 0]}, hands={"R": right(raised, -24, -8, load), "L": left(REACH, -24, -8)}),
        key(13, body(-10, 0), hips=drive, feet={"L": [0.0, -0.08, 0.05, 8, 0], "R": rear(6)}, hands={"R": right(over_, -10, 0, drive), "L": left(toward(REACH, PASSING, 0.35), -10, 0)}),
        key(16, body(8, 12), hips=(0.0, -0.12, -0.06), feet=lunge,
            hands={"R": right(((-0.40, -0.88, 0.25), (0.05, -0.99, 0.10), (0.0, 0.1, 1.0), 25), 8, 12, (0, -0.12, -0.06)), "L": left(toward(PASSING, THROWN, 0.25), 8, 12)}),
        key(18, body(18, 20), hips=hit, feet=lunge, hands={"R": right(down, 18, 20, hit), "L": left(toward(PASSING, THROWN, 0.8), 18, 20)}),
        key(21, body(24, 23), hips=past, feet=lunge, hands={"R": right(low, 24, 23, past), "L": left(THROWN, 24, 23)}),
        key(26, body(22, 20), hips=(0.0, -0.16, -0.07), feet=lunge, hands={"R": right(low, 22, 20, (0, -0.16, -0.07)), "L": left(toward(PASSING, THROWN, 0.9), 22, 20)}),
        key(35, body(5, 4), hips=back, feet={"L": [0.0, -0.06, 0.03, 4, 0], "R": rear(5, 2)},
            hands={"R": dict(stance["R"], at=[a + b for a, b in zip(stance["R"]["at"], (0.0, -0.05, -0.02))]), "L": left(toward(CUPPED, PASSING, 0.45), 5, 4)}),
        key(41, body(-3, -2), hips=over, hands={"R": stance["R"], "L": left(CUPPED, -3, -2)}),
        key(48, rest, hands={"R": stance["R"], "L": left(CUPPED)})]


def strike_ends(k):
    """The left arm's ways as a grip; the clip's first and last frames are the stance's own key to the digit, and the few
    frames by them are drawn to it (the arm on paper holds the stance's cupped hand a centimetre and a few degrees off)."""
    k = ways_to_grips(k)
    w = max(0.0, 1 - k["frame"] / 8.0, 1 - (48 - k["frame"]) / 8.0)
    held = STANCES["staff"]["hands"](0)["L"]
    if w >= 1.0:
        k["hands"]["L"] = dict(held, blend=1.0)
    elif w > 0.0:
        zero = ways_to_grips({"hands": {"L": by_ways(CUPPED)}})["hands"]["L"]
        for name in ("at", "aim", "edge"):
            k["hands"]["L"][name] = [round(p + w * (q - r), 5) for p, q, r in zip(k["hands"]["L"][name], held[name], zero[name])]
    return k


# punch: a straight right from the fists' stance; the hips and the rear foot come round behind it, the left fist is pulled
# back to the chin; the fist's thumb side is its "weapon" line, its knuckles lead
def punch():
    held = STANCES["unarmed"]
    rest, stance = dict(held["pose"]), held["hands"](0)
    load, hit, held_, back, over = (0.0, 0.03, -0.025), (0.0, -0.15, -0.06), (0.0, -0.145, -0.06), (0.0, -0.04, -0.02), (0.0, 0.01, 0.005)
    lunge = {"L": [0.0, -0.14, 0.0, 0, 0], "R": rear(24, 18)}

    def body(yaw, lean):
        return {**trunk(yaw, lean), **rest}

    guard = ((0.10, -0.45, -0.89), (-0.30, -0.25, 0.92), (-0.35, 0.55, 0.76))            # the left fist drawn back to the chin

    def left(yaw, lean, hips, t=1.0):
        lead = ((0.05, -0.70, -0.71), (-0.35, -0.40, 0.85), (-0.35, 0.55, 0.76))
        return arm(shoulder("L", yaw, lean, hips), *[[p + (q - p) * t for p, q in zip(a, b)] for a, b in zip(lead, guard)])

    cocked = ((-0.30, 0.55, -0.78), (0.10, -0.90, 0.42), (0.25, 0.35, 0.90))
    out = ((0.20, -0.96, -0.12), (0.25, -0.96, 0.08), (0.5, 0.1, 0.86))
    return [
        key(0, rest, hands=stance),
        key(5, body(-20, 2), hips=load, feet={"R": rear(-6)}, hands={"R": right(cocked, -20, 2, load), "L": left(-20, 2, load, 0.0)}),
        key(9, body(34, 14), hips=hit, feet=lunge, hands={"R": right(out, 34, 14, hit), "L": left(34, 14, hit, 1.0)}),
        key(12, body(36, 14), hips=held_, feet=lunge, hands={"R": right(((0.20, -0.96, -0.14), (0.25, -0.96, 0.06), (0.5, 0.1, 0.86)), 36, 14, held_), "L": left(36, 14, held_, 1.0)}),
        key(17, body(10, 6), hips=(0.0, -0.08, -0.035), feet={"L": [0.0, -0.09, 0.0, 0, 0], "R": rear(10, 8)},
            hands={"R": right(((-0.20, 0.0, -0.98), (0.20, -0.75, 0.63), (0.3, 0.45, 0.84)), 10, 6, (0, -0.08, -0.035)), "L": left(10, 6, (0, -0.08, -0.035), 0.6)}),
        key(23, body(2, 2), hips=back, feet={"L": [0.0, -0.04, 0.025, 3, 0]}, hands={"R": stance["R"], "L": left(2, 2, back, 0.15)}),
        key(27, body(-2, -1), hips=over, hands=stance),
        key(32, rest, hands=stance)]


OFF_OVER = ((0.45, -0.65, 0.55), (-0.08, -0.60, 0.80), (0.0, 0.8, 0.6), 10)
OFF_CUT = ((0.05, -0.85, -0.50), (-0.25, -0.80, -0.55), (-0.1, -0.6, 0.8), 38)


def off_hand_arc(k):
    """Between its keys at 11 and 13 the cutting hand went straight from over the shoulder to the cut's place, inside the
    arc an arm's length draws, so the elbow buckled for that one frame (the measure's reach 1.00, 0.85, 1.07, found after
    the clip's approval, 2026-11-18). Frame 12 is the arm half way round instead, the body where the frame has it."""
    if k["frame"] == 12:
        moved = [a - b for a, b in zip(k["hips"], READY["hips"])]
        k["hands"]["L"] = dict(between(OFF_OVER, OFF_CUT, 0.5, -2.0, 8.5, moved, side="L"), blend=1.0)
    return k


# swing-off-hand: the left hand's weapon cut down from over the left shoulder across to the right (the swing, mirrored
# and shorter); the right keeps its own weapon back
def swing_off_hand():
    load, drive, hit, past, hang, back, over = ((0.0, 0.04, -0.02), (0.0, -0.03, -0.04), (0.0, -0.14, -0.065), (0.0, -0.15, -0.075), (0.0, -0.13, -0.06),
                                                (0.0, -0.04, -0.02), (0.0, 0.01, 0.006))
    lunge = {"L": [0.0, -0.15, 0.0, 0, 0], "R": rear(16, 4)}

    def left(pose, yaw, lean, hips):
        return arm(shoulder("L", yaw, lean, hips), *pose[:3], wrist=pose[3] if len(pose) > 3 else 0)

    up = ((0.85, 0.20, 0.30), (-0.20, 0.10, 0.97), (-0.1, 0.95, -0.2))
    over_, cut = OFF_OVER, OFF_CUT
    through = ((-0.20, -0.62, -0.76), (-0.58, -0.42, -0.70), (-0.5, -0.8, 0.3), 30)
    return [
        key(0),
        key(8, trunk(22, -4), hips=load, feet={"R": rear(-6)}, hands={"L": left(up, 22, -4, load), "R": sword_arm(22, -4, load)}),
        key(11, trunk(8, 3), hips=drive, feet={"L": [0.0, -0.07, 0.045, 8, 0], "R": rear(5)}, hands={"L": left(over_, 8, 3, drive), "R": sword_arm(8, 3, drive)}),
        key(13, trunk(-12, 14), hips=hit, feet=lunge, hands={"L": left(cut, -12, 14, hit), "R": sword_arm(-12, 14, hit)}),
        key(16, trunk(-22, 18), hips=past, feet=lunge, hands={"L": left(through, -22, 18, past), "R": sword_arm(-22, 18, past)}),
        key(21, trunk(-18, 15), hips=hang, feet=lunge, hands={"L": left(through, -18, 15, hang), "R": sword_arm(-18, 15, hang)}),
        key(29, trunk(-4, 4), hips=back, feet={"L": [0.0, -0.05, 0.03, 4, 0], "R": rear(4)},
            hands={"L": shield_arm(-4, 4, back, open_=0.3), "R": sword_arm(-4, 4, back)}),
        key(35, trunk(2, -1), hips=over, hands={"L": shield_arm(2, -1, over), "R": sword_arm(2, -1, over)}),
        key(40)]


# swing-two-handed: from the two-handed stance the blade goes up over the right shoulder and is brought down with the whole
# body on a deep step, and back to the stance. The blade's edge leads: it looks the way the blade travels (the swing turns
# about the line from the figure's right to its left). (Left to the solver, the wrists came out straight and the flat
# led: the user saw it.) A closed fist holds the blade square to the hand, so with the edge leading the forearm must run
# near the way the blade travels, less what the wrist gives; each key's arm is therefore found (grips.two_hands).
def swing_two_handed():
    held = STANCES["two-handed"]
    rest, stance = dict(held["pose"]), held["hands"](0)

    def body(yaw, lean):
        return {**trunk(yaw, lean), **rest}

    def blade(aim, yaw, lean, hips, right_, forward, up):
        return two_hands(aim, middle(yaw, lean, hips, right_, forward, up), yaw, lean, hips)

    load, drive, land, hit, past, hang, back, over = ((0.0, 0.055, -0.02), (0.0, -0.02, -0.04), (0.0, -0.13, -0.07), (0.0, -0.20, -0.09), (0.0, -0.215, -0.10),
                                                      (0.0, -0.19, -0.085), (0.0, -0.05, -0.025), (0.0, 0.012, 0.008))
    lunge = {"L": [0.0, -0.24, 0.0, 0, 0], "R": rear(20, 12)}
    return [
        key(0, rest, hands=stance),
        key(5, body(-14, -3), hips=(0.0, 0.03, -0.012), hands=blade((0.02, -0.40, 0.92), -14, -3, (0.0, 0.03, -0.012), 0.10, 0.30, -0.10)),
        key(10, body(-28, -8), hips=load, feet={"R": rear(-8), "L": [0.0, 0.0, 0.012, -6, 0]}, hands=blade((-0.10, 0.55, 0.83), -28, -8, load, 0.14, 0.07, 0.33)),
        key(13, body(-12, 0), hips=drive, feet={"L": [0.0, -0.10, 0.05, 8, 0], "R": rear(6)}, hands=blade((-0.05, 0.25, 0.97), -12, 0, drive, 0.10, 0.25, 0.35)),
        key(15, body(6, 10), hips=land, feet=lunge, hands=blade((0.0, -0.45, 0.89), 6, 10, land, 0.06, 0.47, 0.06)),
        key(17, body(18, 20), hips=hit, feet=lunge, hands=blade((0.05, -0.93, 0.36), 18, 20, hit, 0.01, 0.53, -0.37)),
        key(21, body(24, 24), hips=past, feet={"L": [0.0, -0.24, 0.0, 0, 0], "R": rear(24, 15)}, hands=blade((0.10, -0.94, -0.33), 24, 24, past, -0.02, 0.45, -0.55)),
        key(27, body(20, 20), hips=hang, feet=lunge, hands=blade((0.09, -0.95, -0.25), 20, 20, hang, -0.01, 0.45, -0.52)),
        key(36, body(5, 5), hips=back, feet={"L": [0.0, -0.08, 0.035, 5, 0], "R": rear(6, 3)}, hands=blade((0.08, -0.85, 0.40), 5, 5, back, 0.08, 0.30, -0.36)),
        key(42, body(-3, -2), hips=over, hands=stance),
        key(48, rest, hands=stance)]


def sources():
    return [
        source("swing", 46, swing(), markers={"hit": 17, "step": 15}, fine=1, lag=LAG),
        source("thrust", 40, thrust(), markers={"hit": 14, "step": 13}, fine=1, lag=LAG),
        source("stab", 30, stab(), markers={"hit": 9, "step": 8}, gear="dagger", fine=1, lag=LAG),
        source("strike", 48, continuous_rolls(strike()), markers={"hit": 18, "step": 16}, gear="staff", fine=1, lag=LAG, after=strike_ends),
        source("punch", 32, punch(), markers={"hit": 9, "step": 8}, gear="none", fine=1, lag=LAG),
        source("swing-off-hand", 40, swing_off_hand(), markers={"hit": 13, "step": 12}, gear="sword,dagger@hand.L", fine=1, lag=LAG,
               after=off_hand_arc),
        source("swing-two-handed", 48, swing_two_handed(), markers={"hit": 17, "step": 15}, gear="greatsword", fine=1, lag=LAG),
    ]

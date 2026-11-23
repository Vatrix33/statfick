# The acts that aren't blows (GM2-P23; Docs/Game/Hero.md, "How it works", step 4b's clip table): casting, a class skill,
# an item, a potion, meditating, the bow and the crossbow, and victory. cast, skill, item, drink, meditate and victory are
# made on the ready stance (the same whatever the hero holds; the battle may lay a weapon's stance over the arm that isn't
# acting); bow and crossbow start and end in their own stance (GM2-P21). In the user's manner (2026-11-17): the body
# gathers before it acts, goes through, and settles. Plain Python; sources() gives their keying sources (poses.source).
from grips import (BEFORE_ROLLED, BEHIND, HIGH, JAW, LOW, SHAKEN, STANCES, arm, between, by_ways, continuous_rolls, mix, norm, rear, right,
                   shield_arm, shoulder, string_hand, sword_arm, trunk, turned, ways_to_grips)
from poses import LAG, key, on_toes, place, source


def left(pose, yaw=0.0, lean=0.0, hips=(0.0, 0.0, 0.0)):
    """The left arm from its directions, which are the chest's own and turn with the trunk."""
    return arm(shoulder("L", yaw, lean, hips), *[turned(v, yaw) for v in pose[:3]], wrist=pose[3] if len(pose) > 3 else 0)


# cast: the weapon (or the bare hand) is drawn up before the face while the body gathers on the rear leg, then thrown out
# level at the foe on a step, and held there while the spell leaves ("cast")
def cast():
    gather, up_, drive, out, held, back, over = ((0.0, 0.025, -0.03), (0.0, 0.045, -0.02), (0.0, -0.04, -0.04), (0.0, -0.12, -0.05), (0.0, -0.115, -0.05),
                                                 (0.0, -0.03, -0.015), (0.0, 0.01, 0.005))
    up = ((0.15, -0.25, -0.96), (0.60, -0.70, 0.39), (-0.25, 0.30, 0.92), 15)         # the fist before the chest, the weapon upright before the face
    aimed = ((0.0, -0.90, -0.43), (0.05, -0.97, -0.25), (0.0, -0.25, 0.97), 45)       # the arm out, the weapon at the foe
    lunge = {"L": [0.0, -0.14, 0.0, 0, 0], "R": rear(16, 8)}
    return [
        key(0),
        key(7, trunk(-10, 4), hips=gather, hands={"R": between(LOW, up, 0.5, -10, 4, gather), "L": shield_arm(-10, 4, gather, cover=0.3)}),
        key(13, trunk(-18, -2), hips=up_, feet={"R": rear(-8)}, hands={"R": right(up, -18, -2, up_, carried=True), "L": shield_arm(-18, -2, up_, cover=0.8)}),
        key(18, trunk(-20, -4), hips=(0.0, 0.05, -0.012), feet={"R": rear(-8)},
            hands={"R": right(up, -20, -4, (0.0, 0.05, -0.012), carried=True), "L": shield_arm(-20, -4, (0.0, 0.05, -0.012), cover=0.9)}),
        key(21, trunk(0, 8), hips=drive, feet={"L": [0.0, -0.07, 0.04, 6, 0], "R": rear(6)},
            hands={"R": between(up, aimed, 0.5, 0, 8, drive), "L": shield_arm(0, 8, drive, cover=0.2)}),
        key(24, trunk(18, 14), hips=out, feet=lunge, hands={"R": right(aimed, 18, 14, out), "L": shield_arm(18, 14, out, open_=0.7)}),
        key(34, trunk(16, 13), hips=held, feet=lunge,
            hands={"R": right(((0.0, -0.90, -0.44), (0.05, -0.97, -0.23), (0.0, -0.23, 0.97), 43), 16, 13, held), "L": shield_arm(16, 13, held, open_=0.8)}),
        key(42, trunk(5, 4), hips=back, feet={"L": [0.0, -0.05, 0.03, 4, 0], "R": rear(5, 2)},
            hands={"R": between(aimed, LOW, 0.6, 5, 4, back), "L": shield_arm(5, 4, back, open_=0.2)}),
        key(48, trunk(-2, -1), hips=over, hands={"R": right(LOW, -2, -1, over), "L": shield_arm(-2, -1, over)}),
        key(54)]


# skill: a gathering of strength before a special blow: a deep crouch, then the body rises onto its toes with the weapon
# thrust up overhead (the elbow back, so the forearm isn't wrung) and the shield arm thrown out, the weapon shaken twice
# while it is held ("skill"), and down into the stance again
def skill():
    crouch, rising, up, held, down, settle = (0.0, 0.01, -0.07), (0.0, 0.005, -0.02), (0.0, -0.01, 0.075), (0.0, -0.01, 0.065), (0.0, 0.0, 0.0), (0.0, 0.005, -0.03)
    toes = {"L": [0.0, 0.0, on_toes(14), -14, 0], "R": rear(12)}

    # the weapon arm is keyed by its own ways all through (grips.by_ways), so the rise and the way down stay an arm
    def step(frame, pose, yaw, lean, hips, more=None, feet=None, **shield):
        return key(frame, {**trunk(yaw, lean), **(more or {})}, hips=hips, feet=feet, hands={"R": by_ways(pose, yaw, lean), "L": shield_arm(yaw, lean, hips, **shield)})

    glad = {"head": [-10, 0, 0]}
    # half way up and down the fist is before the right shoulder, the elbow out and down (grips.BEFORE_ROLLED): mixed
    # straight from the stance's arm to the raised one, the fist passes 15 cm from the shoulder and the arm folds shut for
    # a frame
    return [
        key(0),
        step(8, LOW, -8, 12, crouch, cover=0.5),
        step(13, BEFORE_ROLLED, -2, 0, rising, open_=0.3),
        step(17, HIGH, 6, -14, up, glad, toes, open_=1.0),
        step(21, SHAKEN, 3, -13, (0.0, -0.01, 0.068), glad, toes, open_=0.85),
        step(25, HIGH, 6, -13, held, glad, toes, open_=0.95),
        step(29, SHAKEN, 4, -12, (0.0, -0.01, 0.06), glad, toes, open_=0.8),
        step(36, BEFORE_ROLLED, 2, -2, down, open_=0.35),
        step(43, LOW, 0, 5, settle, cover=0.2),
        key(50)]


# The left hand at the belt behind the hip, a thing held up before the chest, pushed out toward the foe, and a flask at the
# lips (the chest's own directions); the shield arm as the ready stance has it.
BELT = ((0.20, 0.30, -0.93), (-0.30, -0.50, -0.81), (-0.30, -0.75, 0.59), 0)
SHOWN = ((0.10, -0.45, -0.89), (-0.25, -0.75, 0.61), (0.0, 0.63, 0.78), 10)
USED = ((0.05, -0.80, -0.60), (-0.10, -0.85, 0.52), (0.0, 0.52, 0.85), 15)
LIPS = ((0.20, -0.62, -0.76), (-0.42, -0.05, 0.91), (-0.10, 0.80, 0.59), 0)
STANCE_L = ((0.586, -0.678, -0.445), (-0.936, -0.333, -0.121), (-0.10, -0.10, 0.99), 0)


def left_between(first, second, t, yaw=0.0, lean=0.0, hips=(0.0, 0.0, 0.0)):
    return left([mix(a, b, t) for a, b in zip(first[:3], second[:3])] + [first[3] + (second[3] - first[3]) * t], yaw, lean, hips)


# item: the left hand goes to the belt, brings the thing up before the chest and uses it with a short push forward ("use");
# the weapon hand keeps its stance. (The pictures show a flask in the hand throughout; the game shows the item only while it
# is used.)
def item():
    reach, show, use, held, back = (0.0, 0.01, -0.02), (0.0, 0.015, -0.005), (0.0, -0.07, -0.03), (0.0, -0.065, -0.03), (0.0, -0.01, -0.01)

    def hands(pose, yaw, lean, hips):
        return {"L": pose, "R": sword_arm(yaw, lean, hips)}

    return [
        key(0),
        key(4, trunk(4, 3), hips=(0.0, 0.005, -0.01), hands=hands(left_between(STANCE_L, BELT, 0.5, 4, 3, (0.0, 0.005, -0.01)), 4, 3, (0.0, 0.005, -0.01))),
        key(9, trunk(8, 6), hips=reach, hands=hands(left(BELT, 8, 6, reach), 8, 6, reach)),
        key(13, trunk(8, 5), hips=reach, hands=hands(left(BELT, 8, 5, reach), 8, 5, reach)),
        key(19, trunk(-4, -2), hips=show, hands=hands(left(SHOWN, -4, -2, show), -4, -2, show)),
        key(23, trunk(-14, 12), hips=use, feet={"L": [0.0, -0.06, 0.0, 0, 0], "R": rear(10, 4)}, hands=hands(left(USED, -14, 12, use), -14, 12, use)),
        key(30, trunk(-12, 11), hips=held, feet={"L": [0.0, -0.06, 0.0, 0, 0], "R": rear(10, 4)}, hands=hands(left(USED, -12, 11, held), -12, 11, held)),
        key(38, trunk(-2, 3), hips=back, hands=hands(left_between(USED, STANCE_L, 0.6, -2, 3, back), -2, 3, back)),
        key(46)]


# drink: the left hand brings a flask to the mouth: its hand follows the head from there (the flask's mouth is put to the
# lips, given in the head's own frame), the body straightens and the head goes back, a swallow or two ("drink" at the
# first), and down again. The user, 2026-11-17: "The drink animation does not put the drink to the mouth."
LIPS_AT = [0.0, 0.032, -0.100]            # just before the lips, in the head's frame (x to the figure's right, y up, z back)


def drink():
    reach, rise, tall, back = (0.0, 0.01, -0.02), (0.0, 0.015, 0.03), (0.0, 0.02, 0.05), (0.0, 0.005, -0.015)
    head = {"head": [16, 0, 0], "neck": [7, 0, 0]}

    def own(spec):
        """A key of the left hand by its own place and ways: it doesn't follow the head."""
        return dict(spec, follow=0.0)

    def to_lips(tilt):
        """The flask's mouth at the lips; `tilt` 0 as it is raised to them (its mouth up and back), 1 tipped up to pour.
        The fist's knuckles look to the figure's right (the palm to the face, the elbow out to the left and below): keyed,
        in the head's frame, because left to the solver the arm's answer jumped between two frames, twice (the user,
        2026-11-18: "During potion drink, the arm twists weird")."""
        way = norm((0.0, 0.55 - 0.80 * tilt, 0.83 + 0.14 * tilt))
        # tipped, the forearm comes up from below (its knuckles up and to the right): level across, it lay before the eyes
        knuckles = norm((0.95 - 0.40 * tilt, 0.28 + 0.52 * tilt, -0.18 * (1 - tilt)))
        return {"tip_head": list(LIPS_AT), "aim_head": way, "edge_head": knuckles, "edge": [-1.0, 0.0, 0.0], "reach": 0.094, "follow": 1.0,
                "elbow": [0.70 - 0.10 * tilt, -0.25 - 0.10 * tilt, -0.60 - 0.10 * tilt]}

    def hands(pose, yaw, lean, hips):
        return {"L": pose, "R": sword_arm(yaw, lean, hips, droop=0.25)}

    return [
        key(0),
        key(5, trunk(4, 3), hips=(0.0, 0.005, -0.01), hands=hands(own(left_between(STANCE_L, BELT, 0.5, 4, 3, (0.0, 0.005, -0.01))), 4, 3, (0.0, 0.005, -0.01))),
        key(10, trunk(8, 6), hips=reach, hands=hands(own(left(BELT, 8, 6, reach)), 8, 6, reach)),
        key(14, trunk(8, 5), hips=reach, hands=hands(own(left(BELT, 8, 5, reach)), 8, 5, reach)),
        key(20, trunk(2, -4), hips=(0.0, 0.012, 0.01), hands=hands(own(left_between(BELT, LIPS, 0.6, 2, -4, (0.0, 0.012, 0.01))), 2, -4, (0.0, 0.012, 0.01))),
        key(26, {**trunk(0, -10), "head": [6, 0, 0]}, hips=rise, hands=hands(to_lips(0.0), 0, -10, rise)),
        key(32, {**trunk(0, -14), **head}, hips=tall, hands=hands(to_lips(1.0), 0, -14, tall)),
        key(39, {**trunk(0, -13), "head": [13, 0, 0], "neck": [6, 0, 0]}, hips=(0.0, 0.02, 0.046), hands=hands(to_lips(0.8), 0, -13, (0.0, 0.02, 0.046))),
        key(46, {**trunk(0, -14), **head}, hips=tall, hands=hands(to_lips(1.0), 0, -14, tall)),
        key(51, {**trunk(0, -8), "head": [4, 0, 0]}, hips=(0.0, 0.016, 0.03), hands=hands(to_lips(0.1), 0, -8, (0.0, 0.016, 0.03))),
        key(57, trunk(1, -2), hips=(0.0, 0.01, 0.01), hands=hands(own(left_between(LIPS, BELT, 0.5, 1, -2, (0.0, 0.01, 0.01))), 1, -2, (0.0, 0.01, 0.01))),
        key(64, trunk(4, 5), hips=back, hands=hands(own(left(BELT, 4, 5, back)), 4, 5, back)),
        key(69, trunk(2, 2), hips=(0.0, 0.0, -0.005), hands=hands(own(left_between(BELT, STANCE_L, 0.6, 2, 2, (0.0, 0.0, -0.005))), 2, 2, (0.0, 0.0, -0.005))),
        key(76)]


# meditate: down on the right knee, the arms sunk, the head bowed, a long breath in which strength returns ("restore"),
# and up again. A kneel on the right knee: the knee on the ground under the hip, the shin back along the ground, the foot
# stood on its toes behind; the left foot flat ahead. (Keyed without a ground in the pictures, the knee hung 16 cm over it
# and the foot's toe was 7 cm in it: the clip library's worker measured the foot, 2026-11-18.) The feet's places are given
# from their rest places.
KNEEL_DOWN, KNEEL_BACK = 0.420, 0.64         # how far the hips go down, how far back the kneeling foot is put (metres)


def meditate():
    down = (0.0, 0.115, -KNEEL_DOWN)
    knelt = {"L": place("L", 0.05, -0.26, 0.0, 0), "R": place("R", 0.0, KNEEL_BACK, 0.05, -72, -3)}
    half = {"L": place("L", 0.05, -0.25, 0.0, 0), "R": place("R", -0.07, 0.36, 0.05, -40, -34)}

    def kneel(frame, lean, hips, breath=0.0, head=-22):
        pose = {**trunk(0, lean), "head": [head + 3 * breath, 0, 0], "neck": [-8, 0, 0], "chest": [-lean * 0.45 + 2.5 * breath, 0, 0],
                "spine": [-lean * 0.3 + 1.2 * breath, 0, 0], "shoulder.L": [0, 0, 1.5 * breath], "shoulder.R": [0, 0, -1.5 * breath]}
        return key(frame, pose, hips=hips, feet=knelt, hands={"R": sword_arm(0, lean, hips, droop=0.6), "L": shield_arm(0, lean, hips, droop=0.6)})

    return [
        key(0),
        key(10, trunk(0, 8), hips=(0.0, 0.04, -0.12), feet=half, hands={"R": sword_arm(0, 8, (0.0, 0.04, -0.12), droop=0.25), "L": shield_arm(0, 8, (0.0, 0.04, -0.12), droop=0.25)}),
        kneel(22, 16, down),
        kneel(42, 14, (0.0, 0.115, -KNEEL_DOWN + 0.004), breath=1.0),      # the breath is the chest's: the knee stays down
        kneel(60, 16, down),
        key(72, trunk(0, 9), hips=(0.0, 0.05, -0.14), feet=half, hands={"R": sword_arm(0, 9, (0.0, 0.05, -0.14), droop=0.25), "L": shield_arm(0, 9, (0.0, 0.05, -0.14), droop=0.25)}),
        key(80, trunk(0, -2), hips=(0.0, 0.0, 0.008), hands={"R": sword_arm(0, -2, (0.0, 0.0, 0.008)), "L": shield_arm(0, -2, (0.0, 0.0, 0.008))}),
        key(86)]


def from_(point, right_=0.0, forward=0.0, up=0.0):
    """A place `right_`, `forward` and `up` metres from `point` in the figure's own space."""
    return [point[0] - right_, point[1] - forward, point[2] + up]


# crossbow: from its stance the body straightens and turns its left side to the foe, the crossbow comes up to the shoulder,
# a breath of aim, the shot ("shoot") with a kick through the body, and down again
def crossbow():
    held = STANCES["crossbow"]
    rest, stance = dict(held["pose"]), held["hands"](0)
    grip = dict(stance["L"])

    def both(yaw, lean, hips, forward, up, aim, edge, elbow=(-0.50, 0.45, -0.74), right_=-0.02):
        return {"R": {"at": from_(shoulder("R", yaw, lean, hips), right_, forward, up), "aim": norm(aim), "edge": norm(edge), "elbow": list(elbow)}, "L": dict(grip)}

    sight = {"head": [-4, 13.2, -8], "neck": [-1.6, 4.8, -4]}          # the head set down and over to the stock, the eye along the bolt
    rise, tall, kick = (0.0, 0.01, 0.03), (0.0, 0.015, 0.05), (0.0, 0.035, 0.05)

    def body(yaw, lean, more=None):
        return {**trunk(yaw, lean), **rest, **(more or {})}

    aim = dict(aim=(-0.12, 0.02, 0.99), edge=(0.04, -1.0, 0.02), elbow=(-0.65, 0.35, -0.67))
    return [
        key(0, rest, hands=stance),
        key(8, body(-12, -3), hips=rise, hands=both(-12, -3, rise, 0.30, -0.12, (-0.11, 0.18, 0.98), (0.08, -0.98, -0.16))),
        key(14, body(-24, -7, sight), hips=tall, hands=both(-24, -7, tall, 0.33, 0.07, **aim)),
        key(20, body(-25, -8, sight), hips=(0.0, 0.016, 0.052), hands=both(-25, -8, (0.0, 0.016, 0.052), 0.335, 0.072, **aim)),
        key(26, body(-24, -7, sight), hips=tall, hands=both(-24, -7, tall, 0.33, 0.07, **aim)),
        key(28, body(-27, -12, {**sight, "head": [-1, 13.75, -7]}), hips=kick, hands=both(-27, -12, kick, 0.31, 0.095, (-0.12, 0.10, 0.99), (0.04, -0.99, 0.09), (-0.65, 0.35, -0.67))),
        key(34, body(-24, -6, sight), hips=(0.0, 0.01, 0.045), hands=both(-24, -6, (0.0, 0.01, 0.045), 0.33, 0.07, **aim)),
        key(44, body(-10, -2), hips=(0.0, 0.005, 0.015), hands=both(-10, -2, (0.0, 0.005, 0.015), 0.28, -0.16, (-0.11, 0.22, 0.97), (0.10, -0.96, -0.24))),
        key(52, body(2, 2), hips=(0.0, 0.0, -0.008), hands=stance),
        key(58, rest, hands=stance)]


# bow: from its stance the body straightens and turns its left side to the foe; the bow comes up near the body with the
# string hand still on the string, then the bow arm pushes out as the string hand comes straight back along the arrow's
# line to the jaw (its place there given in the head's frame), a breath of aim, the loose ("shoot": the hand flies back,
# open), and down again. The string hand's fist lies along the string and its knuckles look along the arrow all the way,
# its elbow level behind it. (The user, 2026-11-17: "The hand movement of the bow is too weird". The real bow and its
# string come with GM3.)
def bow():
    held = STANCES["bow"]
    rest, stance = dict(held["pose"]), held["hands"](0)
    turn, lean = -50, -9
    rise, tall = (0.0, 0.01, 0.03), (0.0, 0.02, 0.055)
    hooked, open_ = dict(rest), dict(rest, **{"fingers.R": [0, 0, 62], "thumb.R": [0, 0, -14]})

    def body(yaw, lean_, pose):
        return {**trunk(yaw, lean_), **pose}

    def bow_hand(yaw, lean_, hips, forward, up, right_=0.03, aim=(0.05, -0.03, 1.0), edge=(0.0, -1.0, 0.03), elbow=(0.55, 0.10, -0.83)):
        return {"at": from_(shoulder("L", yaw, lean_, hips), right_, forward, up), "aim": norm(aim), "edge": norm(edge), "elbow": list(elbow)}

    def both(bow_, drawn=0.0, to=JAW, elbow=(-0.81, 0.10, 0.58)):
        """Both hands' key: the bow's, and the string hand's on the string or drawn (grips.string_hand)."""
        return {"L": bow_, "R": string_hand(bow_, drawn, to, elbow)}

    low_elbow = (0.17, -0.95, -0.25)                  # the string arm's elbow before the chest, as the stance has it
    near = bow_hand(turn * 0.5, -4, rise, 0.40, -0.18, aim=(0.12, -0.26, 0.96), edge=(-0.03, -0.96, -0.27), elbow=(0.45, 0.30, -0.84))
    half = bow_hand(turn, lean, tall, 0.50, -0.03, aim=(0.08, -0.10, 0.99), edge=(0.0, -0.99, -0.10), elbow=(0.50, 0.20, -0.84))
    out = bow_hand(turn - 4, lean - 2, (0.0, 0.03, 0.055), 0.565, 0.06)
    steady = bow_hand(turn - 5, lean - 3, (0.0, 0.032, 0.057), 0.57, 0.062)
    after = bow_hand(turn - 7, lean - 1, (0.0, 0.02, 0.05), 0.575, 0.045, aim=(0.05, -0.12, 0.99), edge=(0.0, -0.99, -0.12))
    lowered = bow_hand(turn * 0.35, -2, (0.0, 0.008, 0.015), 0.38, -0.20, aim=(0.25, -0.33, 0.91), edge=(-0.06, -0.92, -0.38), elbow=(0.45, 0.30, -0.84))
    return [
        key(0, rest, hands=both(stance["L"], elbow=low_elbow)),
        key(9, body(turn * 0.5, -4, hooked), hips=rise, hands=both(near, 0.15, elbow=(-0.10, -0.80, -0.10))),
        key(15, body(turn, lean, hooked), hips=tall, hands=both(half, 0.45, elbow=(-0.45, -0.40, 0.20))),
        key(25, body(turn - 4, lean - 2, hooked), hips=(0.0, 0.03, 0.055), hands=both(out, 1.0)),
        key(34, body(turn - 5, lean - 3, hooked), hips=(0.0, 0.032, 0.057), hands=both(steady, 1.0)),
        key(36, body(turn - 7, lean - 1, open_), hips=(0.0, 0.02, 0.05), hands=both(after, 1.0, to=BEHIND, elbow=(-0.78, 0.25, 0.57))),
        key(44, body(turn - 4, lean, open_), hips=(0.0, 0.02, 0.05), hands=both(after, 1.0, to=BEHIND, elbow=(-0.78, 0.25, 0.55))),
        # the string hand goes back to the string the way it came (the draw's own line, which holds no snap of the arm)
        key(49, body(turn - 2, lean, hooked), hips=(0.0, 0.02, 0.045), hands=both(out, 1.0)),
        key(54, body(turn * 0.8, -6, hooked), hips=(0.0, 0.014, 0.03), hands=both(half, 0.45, elbow=(-0.45, -0.40, 0.20))),
        key(58, body(turn * 0.35, -2, hooked), hips=(0.0, 0.008, 0.015), hands=both(lowered, 0.10, elbow=(0.0, -0.90, -0.20))),
        key(64, body(2, 2, rest), hips=(0.0, 0.0, -0.008), hands=both(stance["L"], elbow=low_elbow)),
        key(70, rest, hands=both(stance["L"], elbow=low_elbow))]


# victory: a crouch, a leap with the weapon thrown up, the weapon held high and shaken, and the stance again
def victory():
    crouch, off, air, down_, land, tall, sway, back, over = ((0.0, 0.01, -0.06), (0.0, 0.0, 0.06), (0.0, 0.0, 0.17), (0.0, 0.0, 0.12), (0.0, 0.0, -0.03),
                                                             (0.0, 0.0, 0.085), (0.0, 0.0, 0.08), (0.0, -0.005, 0.03), (0.0, -0.005, -0.015))
    glad = {"head": [-12, 0, 0]}
    leap = {"L": [0.0, 0.06, 0.11, -30, 0], "R": [0.0, -0.06, 0.10, -26, 12]}
    stood = {"L": [0.0, 0.04, 0.0, 0, 0], "R": rear(-8, 0, back=-0.04)}

    # the weapon goes up with its elbow back (grips.HIGH: the user saw the first's forearm wrung, "In the victory pose, the
    # arm twists weird"), keyed by the arm's own ways all through (grips.by_ways)
    def step(frame, pose, yaw, lean, hips, more=None, feet=None, **shield):
        return key(frame, {**trunk(yaw, lean), **(more or {})}, hips=hips, feet=feet, hands={"R": by_ways(pose, yaw, lean), "L": shield_arm(yaw, lean, hips, **shield)})

    return [
        key(0),
        step(6, LOW, -4, 10, crouch, cover=0.5),
        step(10, BEFORE_ROLLED, 0, -4, off, feet={"L": [0.0, 0.0, on_toes(22), -22, 0], "R": rear(18)}, open_=0.5),
        step(14, HIGH, 4, -14, air, glad, leap, open_=1.1),
        step(18, HIGH, 4, -12, down_, glad, {"L": [0.0, 0.03, 0.05, -24, 0], "R": [0.0, -0.03, 0.05, -18, 6]}, open_=1.0),
        step(22, SHAKEN, 2, 0, land, open_=0.6),
        step(28, HIGH, 3, -13, tall, glad, stood, open_=1.0),
        step(36, SHAKEN, -3, -11, sway, glad, stood, open_=0.8),
        step(44, HIGH, 3, -13, tall, glad, stood, open_=1.0),
        step(53, BEFORE_ROLLED, 2, -5, back, feet={"L": [0.0, 0.02, 0.0, 0, 0], "R": rear(-4, 0, back=-0.02)}, open_=0.5),
        step(61, LOW, 1, 2, over, open_=0.1),
        key(68)]


def sources():
    return [
        source("cast", 54, cast(), markers={"cast": 24}, fine=1, lag=LAG),
        source("skill", 50, continuous_rolls(skill()), markers={"skill": 17}, fine=1, lag=LAG, after=ways_to_grips),
        source("item", 46, item(), markers={"use": 23}, gear="sword,flask@hand.L", fine=1, lag=LAG),
        source("drink", 76, drink(), markers={"drink": 32}, gear="sword,flask@hand.L", fine=1, lag=LAG),
        source("meditate", 86, meditate(), markers={"restore": 42}, fine=1),
        source("crossbow", 58, crossbow(), markers={"shoot": 26}, gear="crossbow", fine=1, lag=LAG),
        # the string is the hand's from the start to the loose (the pictures draw it and an arrow; the real bow comes with GM3)
        source("bow", 70, bow(), markers={"shoot": 36}, gear="bow@hand.L", fine=1, lag=LAG, string=[[0, 1.0], [34, 1.0], [36, 0.0], [70, 0.0]]),
        source("victory", 68, continuous_rolls(victory()), markers={"cheer": 14, "land": 22}, fine=1, lag=LAG, after=ways_to_grips),
    ]

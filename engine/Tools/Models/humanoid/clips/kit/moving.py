# The moving clips and the ones entered then held (GM2-P23; Docs/Game/Hero.md, "How it works", step 4b's clip table): the
# dash to a foe and back, fleeing, the guard and the evade with their holds. Every one on the ready stance but flee (its
# own runner's base). No root motion: the battle moves the figure (`travel` shows it in the pictures only). Plain Python;
# sources() gives their keying sources (poses.source).
import math

import idle
from grips import arms, rear, shield_arm, sword_arm, trunk
from poses import LAG, key, on_toes, place, source


# advance: the dash to the foe (the game moves the figure): a crouch, two driving strides, the stance again. The feet's
# places are given from their rest places (poses.place).
def advance():
    def step(frame, yaw, lean, hips, left, right_, forward, cover=0.0, open_=0.0, head=6):
        pose = {**trunk(yaw, lean), "head": [head, 0, 0]}
        return key(frame, pose, hips=hips, feet={"L": place("L", *left), "R": place("R", *right_)},
                   hands=arms(yaw, lean, hips, forward=forward, cover=cover, open_=open_))

    return [
        key(0),
        key(5, trunk(-6, 10), hips=(0, 0.02, -0.04), hands=arms(-6, 10, (0, 0.02, -0.04), forward=-0.3, cover=0.4)),
        # the front (left) foot drives back on the ground while the right swings through
        step(8, -8, 16, (0, -0.02, -0.012), (0.05, 0.02, 0.03, -10), (-0.07, -0.12, 0.10, -8, -20), 0.2, cover=0.5),
        # the right lands on its forefoot, the left leaves the ground from its toe (the heel up, the toe down)
        step(11, 8, 20, (0, -0.05, 0.02), (0.05, 0.24, 0.11, -35), (-0.07, -0.32, 0.06, -20, -10), 0.8, open_=0.3),
        # the right carries the weight back flat on the ground, the left passes it in the air
        step(14, 4, 20, (0, -0.045, -0.014), (0.05, -0.04, 0.17, -20), (-0.07, 0.0, 0.0, 0, -12), 0.3, open_=0.1),
        step(18, -8, 18, (0, -0.04, 0.005), (0.05, -0.34, 0.05, -18), (-0.07, 0.36, 0.11, -35, -20), -0.5, cover=0.6),
        # settling into the stance: both feet's toes held on the ground
        step(22, -3, 6, (0, -0.012, -0.012), (0.05, -0.28, on_toes(6), -6), (-0.07, 0.26, on_toes(20), -20, -34), -0.15, cover=0.2, head=2),
        key(28)]


# return: the hop back to its place: a crouch, both feet off the ground, a landing taken in the knees
def back_to_place():
    return [
        key(0),
        key(5, trunk(0, 10), hips=[0, -0.02, -0.055], hands=arms(0, 10, (0, -0.02, -0.055), cover=0.3)),
        key(12, {**trunk(0, -8), "head": [-6, 0, 0]}, hips=[0, 0.05, 0.075], feet={"L": [0, 0.06, 0.11, 30, 0], "R": [0, 0.04, 0.10, 34, 10]},
            hands=arms(0, -8, (0, 0.05, 0.075), forward=0.5, open_=0.6)),
        key(19, trunk(0, 12), hips=[0, 0.012, -0.07], hands=arms(0, 12, (0, 0.012, -0.07), forward=-0.2, cover=0.3)),
        key(24, trunk(0, -2), hips=[0, 0.0, 0.01], hands=arms(0, -2, (0, 0.0, 0.01))),
        key(28)]


# flee: turned about and running; a loop of two strides, keyed on its own base (not the fighting stance). The body faces
# the figure's rest back, so a foot's "back" is the runner's front here: a foot lands in front, is carried back on the
# ground, leaves it behind the body, kicks up and swings forward through the air. A foot's toe pitch is positive with the
# toe up.
RUNNER = {
    "pose": {"hips": [-11, 180, 0], "spine": [-6, 0, 0], "chest": [-14, 0, 0], "neck": [2, 0, 0], "head": [12, 0, 0],
             "upper_arm.R": [-6, 0, -26], "forearm.R": [-70, 0, 0], "fingers.R": [0, 0, -85], "thumb.R": [0, 0, 30],
             "upper_arm.L": [-22, 8, 22], "forearm.L": [-74, 0, 0], "fingers.L": [0, 0, 62], "thumb.L": [0, 0, -20]},
    "hips": [0.0, 0.010, -0.057],
    "feet": {"L": [0, 0, 0, 0, 0], "R": [0, 0, 0, 0, 0]},
}
#          ahead   up     toe pitch     (a foot's path over one cycle of 32 frames, from its landing)
RUN = [(0.30, 0.015, 10), (0.06, 0.0, 0), (-0.18, 0.034, -10), (-0.36, 0.113, -40), (-0.34, 0.30, -85),
       (-0.04, 0.26, -50), (0.26, 0.20, -15), (0.38, 0.09, 5)]


def path(t):
    """The foot's place t frames after its landing: a closed Catmull-Rom curve through RUN's eight places."""
    u = (t % 32) / 4
    i, f = int(u), u - int(u)
    p0, p1, p2, p3 = (RUN[(i + k - 1) % 8] for k in range(4))
    out = [0.5 * (2 * b + (c - a) * f + (2 * a - 5 * b + 4 * c - d) * f * f + (3 * b - a - 3 * c + d) * f ** 3)
           for a, b, c, d in zip(p0, p1, p2, p3)]
    out[1] = max(0.0, out[1])
    return out


def stride(frame):
    t = frame % 32
    swing = math.cos(2 * math.pi * t / 32)                    # 1 as the left foot lands, -1 as the right does
    bob = -0.020 * math.cos(2 * math.pi * (t - 4) / 16)       # lowest over the planted foot, highest in the air
    la, lu, lp = path(t)
    ra, ru, rp = path(t + 16)
    pose = {"upper_arm.R": [-26 * swing, 0, 0], "upper_arm.L": [26 * swing, 0, 0], "chest": [0, 5 * swing, 0], "hips": [0, -4 * swing, 0]}
    return key(frame, pose, hips=[0, 0.0, bob], feet={"L": [-0.215, la, lu, lp, 0, 180], "R": [0.215, ra, ru, rp, 0, 180]}, base=RUNNER)


# ---- guard: the body sinks behind the shield, which is brought square before it (the user, 2026-11-17: "i want the shield
# to be fully in front of the character"); the weapon is drawn back, the rear heel down. Entered, then held (a loop) until
# the hero's next turn ----
def guard(frame, t=1.0, drop=0.0, breath=0.0, rock=0.0, late=0.0):
    yaw, lean = -8 * t, 6 * t
    hips = (0.0, 0.03 * t - 0.006 * rock, -0.05 * t + drop + 0.010 * breath)
    pose = {**trunk(yaw, lean), "head": [lean * 0.5 - 4 * t - 1.2 * breath, -yaw * 0.55, 0], "chest": [-lean * 0.45 + 1.8 * breath, yaw * 0.5, 0],
            "shoulder.L": [0, 0, 1.2 * breath], "shoulder.R": [0, 0, -1.2 * breath]}
    return key(frame, pose, hips=hips, feet={"L": [0.0, -0.03 * t, 0.0, 0, 0], "R": rear(-6 * t, 0, back=0.03 * t)},
               hands={"R": sword_arm(yaw, lean, hips, forward=-0.35 * t + 0.05 * late), "L": shield_arm(yaw, lean, hips, front=t, cover=0.05 * late * t)})


def guard_hold(frame):
    p, b, r, s = idle.waves(frame % 120)                 # the last key is the first again
    return guard(frame, breath=b, rock=r, late=math.sin(p - 0.9))


# ---- evade: a leap back out of reach (the user, 2026-11-17: "i want the character to fully jump back"), landing light on
# the balls of the feet, more upright and further side-on; then a calm hold, the weight going slowly from foot to foot
# ("The Evade hold seems very twitchy" of the first, which bounced four times in two thirds of a second). No root motion:
# the clip leaps in place and the game moves the figure back between "leap" and "land" (`travel`, which only the pictures
# use: 0.85 m here; GM5) and forward again at its next turn.
def light(frame, t=1.0, bob=0.0, sway=0.0, heels=(14, 12)):
    """The evading stance. `bob`: metres the body is down; `sway`: metres to the figure's left; `heels`: how far each heel
    is up (degrees; the rear one beyond the stance's own)."""
    yaw, lean = -12 * t, -4 * t
    hips = (sway, 0.02 * t, 0.03 * t - bob)
    pose = {**trunk(yaw, lean), "hips": [lean * -0.25, yaw * 0.2, 60 * sway]}
    return key(frame, pose, hips=hips, feet={"L": [0.0, 0.03 * t, on_toes(heels[0] * t), -heels[0] * t, 0], "R": rear(heels[1] * t, 0, back=-0.03 * t)},
               hands={"R": sword_arm(yaw, lean, hips, wide=0.2 * t), "L": shield_arm(yaw, lean, hips, open_=0.3 * t)})


def evade():
    load, off, apex, down, deep = (0.0, -0.03, -0.07), (0.0, 0.06, 0.03), (0.0, 0.02, 0.14), (0.0, 0.02, 0.02), (0.0, 0.035, -0.085)
    return [
        key(0),
        # the weight goes down onto the front foot
        key(4, trunk(4, 14), hips=load, feet={"R": rear(8)}, hands=arms(4, 14, load, forward=0.2, cover=0.4)),
        # the front foot throws the body back: its toe is the last thing on the ground
        key(7, trunk(-4, -6), hips=off, feet={"L": [0.0, 0.0, on_toes(22), -22, 0], "R": [0.0, 0.10, 0.10, 0, 0]}, hands=arms(-4, -6, off, wide=0.3, open_=0.3)),
        # in the air, the legs drawn up under it, the arms out
        key(11, trunk(-6, 8), hips=apex, feet={"L": [0.0, 0.12, 0.21, -28, 0], "R": [0.0, -0.05, 0.17, -6, 0]}, hands=arms(-6, 8, apex, wide=0.55, open_=0.6)),
        # the rear foot comes down first
        key(15, trunk(-8, 6), hips=down, feet={"L": [0.0, 0.05, 0.06, -12, 0], "R": [0.0, 0.03, 0.0, 0, 0]}, hands=arms(-8, 6, down, wide=0.35, open_=0.4)),
        # and the landing is taken in the knees
        key(19, trunk(-10, 14), hips=deep, feet={"L": [0.0, 0.03, 0.0, 0, 0], "R": rear(-6, 0, back=0.02)}, hands=arms(-10, 14, deep, forward=-0.2, cover=0.5)),
        light(26, bob=-0.006),
        light(30)]


def evade_hold(frame):
    p = 2 * math.pi * (frame % 96) / 96
    return light(frame, bob=0.010 * (1 - math.cos(2 * p)) / 2, sway=0.012 * math.sin(p), heels=(14 + 4 * math.sin(p), 12 - 4 * math.sin(p)))


def sources():
    return [
        source("advance", 28, advance(), markers={"step": 11, "step2": 18}, fine=1, lag=LAG),
        source("return", 28, back_to_place(), markers={"land": 19}, fine=1, lag=LAG),
        # keyed at every frame from the stride itself, and so written "linear"
        source("flee", 32, [stride(f) for f in range(0, 33)], loop=True, markers={"step": 0, "step2": 16}, ease="linear"),
        source("guard", 14, [key(0), guard(5, 0.6, drop=-0.02), guard(9, 1.0, drop=-0.022), guard(14, late=math.sin(-0.9))], markers={"guard": 9},
               fine=1, lag=LAG),
        # the guard is entered into the hold's first frame (the arms a little behind the breath there)
        source("guard-hold", 120, [guard_hold(f) for f in range(0, 121, 10)], loop=True),
        source("evade", 30, evade(), markers={"leap": 6, "land": 15, "evade": 19}, fine=1, lag=LAG,
               travel=[[0, 0.0], [6, 0.0], [15, 0.85], [30, 0.85]]),
        source("evade-hold", 96, [evade_hold(f) for f in range(0, 97, 8)], loop=True),
    ]

# The standing clips (GM2-P23; Docs/Game/Hero.md, "How it works", step 4b's clip table): the battle's idle and the
# stunned sway on the ready stance, the relaxed stand of the sheet, the maker and the carousel (the user's Q57: "Relaxed,
# at ease"), and the ready stances per weapon kind as loops of the arms (the user's Q56, GM2-P21). Plain Python; sources()
# gives their keying sources (poses.source).
import math

import idle
from grips import STANCES, shield_arm, sword_arm
from poses import ARMS, key, on_toes, source


# stunned: a slumped sway, the head lolling, the arms sunk; a loop of a second and a half
def stunned(frame, amount, hips):
    slump = {"chest": [-10, -3, 4 * amount], "spine": [-5, -2, 0], "neck": [-8, 0, 0], "head": [-14, -4, 10 * amount], "hips": [0, 0, 2 * amount],
             "shoulder.L": [0, 0, -3], "shoulder.R": [0, 0, 3], "fingers.R": [0, 0, 12]}
    return key(frame, slump, hips=hips, hands={"R": sword_arm(-3, 9, hips, droop=0.85), "L": shield_arm(-3, 9, hips, droop=0.8)})


# At ease (the second one, 2026-11-17: the first was "too stiff as well"): the weight on the right leg and the hip out over
# it, the left leg forward and out with a soft knee and its foot turned out, the pelvis and the shoulders tilted against
# each other, the body turned a little off the front, the arms loose and unlike (the right hanging a little back, the left
# more bent and forward), no hand touching the body (a clip is shared by bodies of every build). (The rest pose's arms
# stand 58 degrees below the level; about 25 more bring them down by the sides.) Its own base, not the ready stance.
EASE = {
    "pose": {"hips": [-1, -9, 8], "spine": [1, 3, -4], "chest": [-2, 5, -5.5], "neck": [2, 1, 2], "head": [2, 5, 3],
             "shoulder.L": [0, 0, 2], "shoulder.R": [0, 0, 3],
             "upper_arm.R": [9, 0, -21], "forearm.R": [-20, 0, 0], "fingers.R": [0, 0, -22], "thumb.R": [0, 0, 8],
             "upper_arm.L": [-3, 0, 19], "forearm.L": [-27, 0, 0], "fingers.L": [0, 0, 32], "thumb.L": [0, 0, -10]},
    "hips": [-0.052, 0.0, -0.020],
    "feet": {"L": [0.075, -0.115, 0.0, 0, 28], "R": [-0.010, 0.020, 0.0, 0, -8]},
}
STAND = 240          # four seconds


def pulse(frame, start, end):
    """0 outside start..end, rising to 1 in the middle and back (a look aside, a shrug)."""
    if not start < frame < end:
        return 0.0
    return (1 - math.cos(2 * math.pi * (frame - start) / (end - start))) / 2


def stand(frame):
    """The stand at a frame: two breaths, the weight settling further onto the right leg and coming back, a look aside
    (frames 40-150), a roll of the shoulders with a deeper breath (160-225); the arms and the fingers follow late."""
    at, frame = frame, frame % STAND                 # the last key is the first again, to the digit
    p = 2 * math.pi * frame / STAND
    breath = (1 - math.cos(2 * p)) / 2
    sway, late, later = math.sin(p), math.sin(p - 0.8), math.sin(p - 1.4)
    look, roll = pulse(frame, 40, 150), pulse(frame, 160, 225)
    heel = 7 * max(0.0, sway)                       # the free foot's heel lifts as the weight goes off it
    pose = {"hips": [0, 1.0 * look, 1.2 * sway], "spine": [0.9 * breath + 0.6 * roll, 1.5 * look, -0.8 * sway],
            "chest": [2.4 * breath + 1.6 * roll, 3.0 * look, -0.9 * sway],
            "shoulder.L": [0, 0, 2.0 * breath + 3.5 * roll], "shoulder.R": [0, 0, -2.0 * breath - 3.5 * pulse(frame, 168, 233)],
            "neck": [-0.6 * breath, 6.0 * look, 0.6 * sway], "head": [-1.6 * breath - 2.0 * roll + 2.0 * look, 13.0 * look, 0.8 * sway - 2.0 * look],
            "upper_arm.L": [2.5 * late, 0, -1.2 * breath], "forearm.L": [-4.0 * later, 0, 0], "fingers.L": [0, 0, 7 * later],
            "upper_arm.R": [1.8 * later, 0, 1.2 * breath], "forearm.R": [-2.5 * math.sin(p - 2.0), 0, 0], "fingers.R": [0, 0, -6 * math.sin(p - 2.2)]}
    hips = [-0.012 * sway, 0.004 * late, 0.005 * breath - 0.004 * max(0.0, sway)]
    feet = {"L": [0.0, 0.0, on_toes(heel), -heel, 0]}
    return key(at, pose, hips=hips, feet=feet, base=EASE)


def sources():
    out = [
        # idle: two seconds; the breath, the weight rocking, the arms after the body (idle.py)
        source("idle", idle.FRAMES, idle.keys(), loop=True),
        source("stunned", 90, [stunned(0, 0, [0.0, 0.0, -0.030]), stunned(22, 1, [0.024, 0.008, -0.036]), stunned(45, 0, [0.0, 0.014, -0.024]),
                               stunned(67, -1, [-0.024, 0.008, -0.036]), stunned(90, 0, [0.0, 0.0, -0.030])], loop=True),
        source("stand", STAND, [stand(frame) for frame in range(0, STAND + 1, 10)], loop=True, gear="none"),
    ]
    # The ready stances: what the arms do while the body idles, moves or reacts in the battle's stance. Each is a loop of the
    # arms (shoulders to fingers) as long as the idle and made on the idle's body (grips.STANCES); the battle lays it over
    # the clip that plays. An attack of that weapon kind starts and ends in its first frame. The one-handed stance has
    # none: its arms are the clips' own.
    for name, held in STANCES.items():
        out.append(source("hold-" + name, idle.FRAMES, idle.keys(hands=held["hands"], extra=held["pose"], arms=False), loop=True, gear=held["gear"],
                          only=ARMS))
    return out

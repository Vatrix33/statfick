# The human bodies (GM2 step 4): the man's and the woman's average build, the base every other body is made from (Q8),
# the user's choice of the three candidates (2026-11-16: candidate A, "lean": long-limbed and slim; necks shortened at the
# user's word, "their necks are very long, reduce it"). Each a parameter set for forms.build_body and rig.bone_table:
#   spine   the torso's sections from the groin to the neck: (name, y, z, half width, half depth)
#   power   a section's squareness (2 an ellipse; 2.3 where not given)
#   arm     the left arm's points: (name, x, y, z, radius), turned to the rest pose's A-pose (58° below the horizontal)
#   hand    the mitten's length, width and thickness
#   leg     the left leg's points; foot: the heel, the ball and the toe, (name, x, y, z, radius)
#   head    the head's frame (its centre and half sizes; the face, parts/face.py, gives the rest)
#   masses  the muscle masses (forms.MASSES); outfit: the plain outfit (outfit.OUTFITS)
# Blender's coordinates: Z up, the figure faces −Y, its left is +X; metres.
from bodies import as_build, pose_arms


def male_average():
    p = {
        "spine": [("groin", 0.006, 0.880, 0.118, 0.084), ("pelvis", 0.004, 0.955, 0.170, 0.108), ("belly", -0.004, 1.060, 0.150, 0.100),
                  ("waist", 0.000, 1.150, 0.140, 0.095), ("ribs", -0.002, 1.265, 0.162, 0.112),
                  ("chest", -0.006, 1.365, 0.180, 0.122), ("clavicle", 0.004, 1.440, 0.176, 0.106),
                  ("yoke", 0.010, 1.494, 0.128, 0.086), ("neck0", 0.016, 1.526, 0.076, 0.072),
                  ("neck1", 0.010, 1.590, 0.062, 0.064)],
        "power": {"chest": 2.6, "clavicle": 2.5, "ribs": 2.4, "pelvis": 2.4},
        "arm": [("armroot", 0.140, 0.006, 1.430, 0.052), ("shoulder", 0.202, 0.008, 1.432, 0.060), ("upperarm", 0.300, 0.012, 1.360, 0.056),
                ("elbow", 0.432, 0.020, 1.252, 0.044), ("forearm", 0.512, 0.010, 1.186, 0.047),
                ("wrist", 0.634, -0.010, 1.084, 0.031)],
        "hand": {"length": 0.185, "width": 0.046, "thick": 0.019},
        "leg": [("legroot", 0.082, 0.004, 0.990, 0.076), ("hip", 0.094, 0.002, 0.900, 0.092), ("thigh", 0.102, -0.006, 0.720, 0.086),
                ("knee", 0.106, -0.012, 0.515, 0.058), ("calf", 0.109, 0.012, 0.385, 0.062),
                ("shin", 0.111, 0.010, 0.220, 0.044), ("ankle", 0.112, 0.012, 0.086, 0.036)],
        "foot": [("heel", 0.111, 0.046, 0.036, 0.034), ("ball", 0.119, -0.112, 0.034, 0.035), ("toe", 0.124, -0.190, 0.026, 0.025)],
        "head": {"centre": (0, 0.0, 1.668), "size": (0.079, 0.098, 0.121)},
        "masses": "man",
        "outfit": "tunic",
    }
    pose_arms(p, 58)
    return p


def female_average():
    p = {
        "spine": [("groin", 0.008, 0.855, 0.114, 0.084), ("pelvis", 0.006, 0.925, 0.172, 0.108), ("belly", -0.002, 1.020, 0.146, 0.094),
                  ("waist", 0.002, 1.105, 0.116, 0.082), ("ribs", -0.002, 1.205, 0.136, 0.098),
                  ("chest", -0.006, 1.300, 0.152, 0.106), ("clavicle", 0.004, 1.368, 0.148, 0.092),
                  ("yoke", 0.010, 1.418, 0.104, 0.070), ("neck0", 0.014, 1.446, 0.060, 0.058),
                  ("neck1", 0.008, 1.504, 0.050, 0.052)],
        "power": {"chest": 2.4, "clavicle": 2.4, "ribs": 2.3, "pelvis": 2.3},
        "arm": [("armroot", 0.118, 0.006, 1.356, 0.043), ("shoulder", 0.172, 0.008, 1.360, 0.048), ("upperarm", 0.262, 0.012, 1.294, 0.043),
                ("elbow", 0.386, 0.020, 1.192, 0.035), ("forearm", 0.462, 0.010, 1.128, 0.037),
                ("wrist", 0.576, -0.010, 1.032, 0.026)],
        "hand": {"length": 0.168, "width": 0.039, "thick": 0.016},
        "leg": [("legroot", 0.080, 0.004, 0.958, 0.076), ("hip", 0.092, 0.004, 0.870, 0.093), ("thigh", 0.100, -0.006, 0.695, 0.082),
                ("knee", 0.101, -0.012, 0.495, 0.053), ("calf", 0.103, 0.012, 0.370, 0.055),
                ("shin", 0.105, 0.010, 0.212, 0.038), ("ankle", 0.106, 0.012, 0.082, 0.031)],
        "foot": [("heel", 0.105, 0.042, 0.033, 0.030), ("ball", 0.112, -0.102, 0.031, 0.031), ("toe", 0.116, -0.174, 0.024, 0.022)],
        "head": {"centre": (0, 0.0, 1.584), "size": (0.074, 0.093, 0.114)},
        "masses": "woman",
        "outfit": "tunic",
    }
    pose_arms(p, 58)
    return p


# The builds (Q15): the average body's skeleton with other girths (bodies.as_build). Slender: narrow through the chest and
# the waist, long thin limbs, the masses faint. Strong: a deep chest and a thick neck, heavy shoulders, arms and legs, the
# masses full.
SLENDER_MAN = {
    "torso": {"groin": (0.94, 0.94), "pelvis": (0.92, 0.92), "belly": (0.88, 0.88), "waist": (0.88, 0.89), "ribs": (0.88, 0.88),
              "chest": (0.88, 0.87), "clavicle": (0.90, 0.90), "yoke": (0.92, 0.92), "neck0": (0.91, 0.91), "neck1": (0.92, 0.92)},
    "arm": {"armroot": 0.88, "shoulder": 0.85, "upperarm": 0.80, "elbow": 0.88, "forearm": 0.82, "wrist": 0.91},
    "leg": {"legroot": 0.93, "hip": 0.91, "thigh": 0.84, "knee": 0.91, "calf": 0.84, "shin": 0.90, "ankle": 0.94},
    "muscle": 0.4, "w": 0.88, "d": 0.87, "a": 0.82, "l": 0.85, "seat": 0.78, "hips": 0.92, "back": 0.87}
STRONG_MAN = {
    "torso": {"groin": (1.05, 1.06), "pelvis": (1.06, 1.08), "belly": (1.10, 1.12), "waist": (1.12, 1.12), "ribs": (1.12, 1.14),
              "chest": (1.10, 1.15), "clavicle": (1.09, 1.14), "yoke": (1.18, 1.16), "neck0": (1.20, 1.16), "neck1": (1.17, 1.14)},
    "arm": {"armroot": 1.12, "shoulder": 1.26, "upperarm": 1.32, "elbow": 1.16, "forearm": 1.24, "wrist": 1.10},
    "leg": {"legroot": 1.06, "hip": 1.08, "thigh": 1.17, "knee": 1.10, "calf": 1.19, "shin": 1.12, "ankle": 1.07},
    "muscle": 1.7, "w": 1.10, "d": 1.15, "a": 1.30, "l": 1.17, "seat": 1.18, "hips": 1.06, "back": 1.15}
SLENDER_WOMAN = {
    "torso": {"groin": (0.94, 0.94), "pelvis": (0.92, 0.93), "belly": (0.89, 0.89), "waist": (0.90, 0.90), "ribs": (0.89, 0.89),
              "chest": (0.89, 0.89), "clavicle": (0.91, 0.91), "yoke": (0.93, 0.93), "neck0": (0.92, 0.92), "neck1": (0.93, 0.93)},
    "arm": {"armroot": 0.90, "shoulder": 0.87, "upperarm": 0.82, "elbow": 0.89, "forearm": 0.84, "wrist": 0.92},
    "leg": {"legroot": 0.94, "hip": 0.92, "thigh": 0.85, "knee": 0.92, "calf": 0.85, "shin": 0.91, "ankle": 0.95},
    "muscle": 0.5, "w": 0.89, "d": 0.89, "a": 0.84, "l": 0.86, "bust": 0.78, "seat": 0.74, "hips": 0.92, "back": 0.89}
STRONG_WOMAN = {
    "torso": {"groin": (1.05, 1.05), "pelvis": (1.06, 1.07), "belly": (1.09, 1.10), "waist": (1.13, 1.12), "ribs": (1.11, 1.12),
              "chest": (1.10, 1.12), "clavicle": (1.10, 1.12), "yoke": (1.15, 1.14), "neck0": (1.15, 1.13), "neck1": (1.12, 1.11)},
    "arm": {"armroot": 1.10, "shoulder": 1.22, "upperarm": 1.26, "elbow": 1.14, "forearm": 1.20, "wrist": 1.08},
    "leg": {"legroot": 1.06, "hip": 1.07, "thigh": 1.16, "knee": 1.09, "calf": 1.17, "shin": 1.11, "ankle": 1.06},
    "muscle": 1.9, "w": 1.10, "d": 1.12, "a": 1.26, "l": 1.16, "bust": 1.0, "seat": 1.15, "hips": 1.06, "back": 1.12}

BODIES = {"human-male-average": male_average, "human-female-average": female_average,
          "human-male-slender": lambda: as_build(male_average(), SLENDER_MAN), "human-male-strong": lambda: as_build(male_average(), STRONG_MAN),
          "human-female-slender": lambda: as_build(female_average(), SLENDER_WOMAN),
          "human-female-strong": lambda: as_build(female_average(), STRONG_WOMAN)}

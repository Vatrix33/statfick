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
from bodies import pose_arms


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


BODIES = {"human-male-average": male_average, "human-female-average": female_average}

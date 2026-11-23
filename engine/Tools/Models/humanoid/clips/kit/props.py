# Plain stand-ins of the weapons and things the clips' pictures put in the hands besides the sample sword and shield
# (GM2-P23; the clips were approved with them, 2026-11-17/18): a greatsword, a spear, a staff, a dagger, a bow, a
# crossbow, a flask. Pictures only (bake.py's frames): never a model of the game (GM3 makes the game's items). Each in
# item space as the sample gear is (gear.py): the origin the grip, Y from the grip along the weapon, Z its edge, X across
# the flat; placed at a hand's socket. Runs inside Blender.
from mathutils import Vector

import kit as k

V = Vector
X, Y, Z = V((1, 0, 0)), V((0, 1, 0)), V((0, 0, 1))
KINDS = ("greatsword", "spear", "staff", "dagger", "bow", "crossbow", "flask")


def stand_in(m, kind):
    """The stand-in `kind`'s pieces, named `<kind>-<piece>`; `m` the materials by slot (materials.make)."""
    parts = []

    def shaft(label, start, end, radius, material, sides=8):
        parts.append(k.tube(f"{kind}-{label}", [Y * start, Y * end], [radius, radius], side=X, n=sides, cap_start="flat", cap_end="flat",
                            material=material, smooth=True))

    def blade(label, rows, material="metal.base"):
        parts.append(k.loft(f"{kind}-{label}", [{"p": Y * s, "x": Z, "y": X, "rx": rx, "ry": ry, "power": 1.0} for s, rx, ry in rows], n=8,
                            cap_start="flat", cap_end="fan", material=m[material], smooth=False))

    if kind == "greatsword":      # the right hand's grip under the guard at the origin; the left hand 0.14 m below it
        blade("blade", [(0.13, 0.034, 0.0070), (0.53, 0.031, 0.0064), (0.93, 0.026, 0.0054), (1.13, 0.012, 0.0030), (1.19, 0.001, 0.0008)])
        shaft("grip", -0.26, 0.10, 0.0150, m["leather"])
        parts.append(k.tube(f"{kind}-guard", [Y * 0.115 + Z * -0.15, Y * 0.12, Y * 0.115 + Z * 0.15], [(0.010, 0.009), (0.015, 0.013), (0.010, 0.009)],
                            side=X, n=8, power=3.0, material=m["metal.trim"]))
        parts.append(k.shaped_sphere(f"{kind}-pommel", lambda d: Y * -0.285 + V((d.x * 0.018, d.y * 0.020, d.z * 0.028)), cuts=3, material=m["metal.trim"]))
    elif kind == "spear":
        shaft("haft", -0.80, 0.95, 0.0140, m["wood"])
        blade("head", [(0.95, 0.012, 0.010), (1.02, 0.036, 0.0060), (1.20, 0.022, 0.0045), (1.30, 0.001, 0.0008)])
    elif kind == "staff":        # held a third of the way up, so its foot swings clear of the legs
        shaft("haft", -0.36, 1.04, 0.0160, m["wood"])
        parts.append(k.shaped_sphere(f"{kind}-knob", lambda d: Y * 1.09 + V((d.x * 0.034, d.y * 0.050, d.z * 0.034)), cuts=3, material=m["metal.trim"]))
        parts.append(k.shaped_sphere(f"{kind}-foot", lambda d: Y * -0.38 + V((d.x * 0.019, d.y * 0.026, d.z * 0.019)), cuts=2, material=m["metal.base"]))
    elif kind == "dagger":
        blade("blade", [(0.062, 0.017, 0.0045), (0.20, 0.014, 0.0038), (0.30, 0.006, 0.0022), (0.33, 0.001, 0.0008)])
        shaft("grip", -0.055, 0.050, 0.0125, m["leather"])
        parts.append(k.tube(f"{kind}-guard", [Y * 0.052 + Z * -0.045, Y * 0.056, Y * 0.052 + Z * 0.045], [(0.007, 0.006), (0.010, 0.009), (0.007, 0.006)],
                            side=X, n=8, power=3.0, material=m["metal.trim"]))
    elif kind == "bow":           # held in the left fist: the stave along Y (out of the thumb's side, as any weapon), the
        #                           arrow's way along Z (the knuckles' way), the string behind the stave (bake.py draws it
        #                           to the string hand while the source's `string` says so)
        limb = [Y * -0.62 + Z * -0.16, Y * -0.42 + Z * -0.03, Y * -0.20 + Z * 0.035, Y * 0.0 + Z * 0.045, Y * 0.20 + Z * 0.035, Y * 0.42 + Z * -0.03,
                Y * 0.62 + Z * -0.16]
        parts.append(k.tube(f"{kind}-stave", limb, [(0.006, 0.007), (0.010, 0.012), (0.012, 0.014), (0.014, 0.016), (0.012, 0.014), (0.010, 0.012), (0.006, 0.007)],
                            side=X, n=8, material=m["wood"]))
        string = k.tube(f"{kind}-string", [Y * -0.62 + Z * -0.16, Y * 0.62 + Z * -0.16], [0.0018, 0.0018], side=X, n=4, material=m["metal.shade"], smooth=False)
        string["ink"] = 0
        parts.append(string)
    elif kind == "crossbow":      # held by a grip under the stock: the fist's line (Y) up through the grip, the bolt's way along Z
        parts.append(k.tube(f"{kind}-stock", [Z * -0.24 + Y * 0.045, Z * -0.05 + Y * 0.040, Z * 0.42 + Y * 0.040], [(0.020, 0.030), (0.018, 0.024), (0.016, 0.018)],
                            side=X, n=8, power=3.0, material=m["wood"]))
        shaft("grip", -0.055, 0.035, 0.0135, m["wood"])
        parts.append(k.tube(f"{kind}-lath", [Z * 0.30 + X * -0.33 + Y * 0.045, Z * 0.38 + Y * 0.045, Z * 0.30 + X * 0.33 + Y * 0.045],
                            [(0.012, 0.008), (0.016, 0.012), (0.012, 0.008)], side=Y, n=8, material=m["metal.base"]))
        string = k.tube(f"{kind}-string", [Z * 0.24 + X * -0.33 + Y * 0.062, Z * 0.24 + X * 0.33 + Y * 0.062], [0.0018, 0.0018], side=Y, n=4,
                        material=m["metal.shade"], smooth=False)
        string["ink"] = 0
        parts.append(string)
    elif kind == "flask":
        parts.append(k.shaped_sphere(f"{kind}-body", lambda d: Y * 0.02 + V((d.x * 0.030, d.y * 0.040, d.z * 0.030)), cuts=3, material=m["metal.trim"]))
        shaft("neck", 0.045, 0.085, 0.011, m["metal.trim"])
        shaft("stopper", 0.085, 0.100, 0.013, m["wood"])
    else:
        raise ValueError(f"no stand-in {kind} (the stand-ins: {', '.join(KINDS)})")
    return parts

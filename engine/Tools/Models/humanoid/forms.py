# The humanoid forms (GM2 step 4; Docs/Game/Hero.md, "The humanoid rig family"): a body, a head and hair, each made of
# closed forms (lofts through sections, tubes, ellipsoids) joined by a voxel remesh into one continuous surface,
# smoothed, shaped by masses (smooth displacements) and remade as even quads; the face's flat pieces (eyes, lashes,
# brows, the mouth) laid on the head by rays. Heroic proportions (Q1, about 7.5 heads), stylised faces and mitten hands
# (Q2); hair as a cap and sculpted locks (Q3). A body's numbers are its parameter set (bodies/), a face's its own
# (parts/face.py), a hair style's its own (parts/hair.py); how finely each is made, its detail level's (levels.py, in
# p["detail"]): the dense sculpt is remade lighter by lighten.py. Blender's coordinates: Z up, the figure faces −Y, its
# left is +X; metres. The rest pose is an A-pose.
#
# Every step is deterministic under the pinned Blender at a fixed thread count (measured: the voxel remesh, the smoothing
# and the masses run after run and at any count; QuadriFlow with its fixed seed run after run at one count, so
# make-humanoid.sh pins 8), so a model builds to the same bytes (make-humanoid.sh check).
import math

import bmesh
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

import kit as k
import lighten

V = Vector
X, Y, Z = V((1, 0, 0)), V((0, 1, 0)), V((0, 0, 1))


def smooth(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3 - 2 * t)


# ---- forms and their joining ----------------------------------------------------------------------------------------
def join(objects, name):
    """One object holding every mesh of `objects` (which are removed)."""
    bm = bmesh.new()
    for obj in objects:
        bm.from_mesh(obj.data)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    for obj in objects:
        old = obj.data
        bpy.data.objects.remove(obj)
        bpy.data.meshes.remove(old)
    joined = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(joined)
    return joined


def ellipsoid(name, centre, radii, cuts=4):
    c = V(centre)
    return k.shaped_sphere(name, lambda d: c + V((d.x * radii[0], d.y * radii[1], d.z * radii[2])), cuts=cuts)


def relax(obj, iterations, factor=0.5):
    modifier = obj.modifiers.new("relax", "SMOOTH")
    modifier.iterations = iterations
    modifier.factor = factor
    k.apply_modifiers([obj])
    obj.data.shade_smooth()


def fuse(forms, name, voxel, smoothing):
    """The forms as one continuous surface: a voxel remesh, then smoothing that takes the voxels' steps out and blends
    where forms met."""
    obj = join(forms, name)
    remesh = obj.modifiers.new("remesh", "REMESH")
    remesh.mode = "VOXEL"
    remesh.voxel_size = voxel
    remesh.adaptivity = 0.0
    remesh.use_smooth_shade = True
    k.apply_modifiers([obj])
    relax(obj, smoothing)
    return obj


def mass(mesh, centre, radii, amount, direction=None, mirror=True):
    """Pushes the vertices near `centre` (inside an ellipsoid of `radii`) by `amount` (metres at the centre, falling off
    smoothly), along `direction` or, without one, away from the centre. Mirrored in X unless told not to."""
    centre = V(centre)
    centres = [centre] + ([V((-centre.x, centre.y, centre.z))] if mirror and abs(centre.x) > 1e-6 else [])
    for index, c in enumerate(centres):
        push = None
        if direction is not None:
            push = V(direction).normalized()
            if index == 1:
                push.x = -push.x
        for v in mesh.vertices:
            d = v.co - c
            if abs(d.x) >= radii[0] or abs(d.y) >= radii[1] or abs(d.z) >= radii[2]:
                continue
            t = math.sqrt((d.x / radii[0]) ** 2 + (d.y / radii[1]) ** 2 + (d.z / radii[2]) ** 2)
            if t >= 1.0:
                continue
            v.co += (push if push is not None else (d.normalized() if d.length > 1e-9 else V((0, 0, 0)))) * (amount * smooth(1.0 - t))


# ---- the body -------------------------------------------------------------------------------------------------------
def body_forms(p):
    """The body's closed forms (the torso, each arm, leg, deltoid, haunch, foot, mitten hand and thumb) and its joints by
    name (the spine's sections, each limb's points with L or R, the hands' middles): the skeleton is placed from these."""
    forms, joints = [], {}
    spine = p["spine"]
    for name, y, z, rx, ry in spine:
        joints[name] = V((0, y, z))
    rows = [{"p": V((0, y, z)), "x": X, "y": Y, "rx": rx, "ry": ry, "power": p.get("power", {}).get(name, 2.3)} for name, y, z, rx, ry in spine]
    rows[0]["bulge"] = 0.035
    rows[-1]["bulge"] = 0.02
    forms.append(k.loft("torso", rows, n=24))
    hand = p["hand"]
    for side in (1, -1):
        tag = "L" if side > 0 else "R"
        arm = [(name, V((side * x, y, z)), r) for name, x, y, z, r in p["arm"]]
        leg = [(name, V((side * x, y, z)), r) for name, x, y, z, r in p["leg"]]
        for name, co, r in arm + leg:
            joints[name + tag] = co
        forms.append(k.tube("arm" + tag, [co for _, co, _ in arm], [r for _, _, r in arm], side=Y, n=16, bulge=(0.03, 0.012)))
        forms.append(k.tube("leg" + tag, [co for _, co, _ in leg], [r for _, _, r in leg], side=X, n=16, bulge=(0.03, 0.02)))
        # the shoulder's cap and the hip's side fill where a limb leaves the torso
        sh, shoulder_r = joints["shoulder" + tag], p["arm"][1][4]
        forms.append(ellipsoid("deltoid" + tag, sh + V((side * 0.008, 0.0, -0.006)), (shoulder_r * 1.10, shoulder_r * 1.10, shoulder_r * 1.02)))
        hip, hip_r = joints["hip" + tag], p["leg"][1][4]
        forms.append(ellipsoid("haunch" + tag, hip + V((side * 0.004, 0.012, 0.035)), (hip_r * 0.98, hip_r * 1.05, 0.085)))
        # the foot
        heel, ball, toe = [(V((side * x, y, z)), r) for _, x, y, z, r in p["foot"]]
        ankle = joints["ankle" + tag]
        forms.append(k.loft("foot" + tag, [
            {"p": heel[0], "x": X, "y": Z, "rx": heel[1] * 1.05, "ry": heel[1], "power": 2.5, "bulge": 0.02},
            {"p": V((ankle.x, ankle.y - 0.02, heel[0].z + 0.012)), "x": X, "y": Z, "rx": heel[1] * 1.25, "ry": heel[1] * 1.35, "power": 2.5},
            {"p": ball[0], "x": X, "y": Z, "rx": ball[1] * 1.35, "ry": ball[1], "power": 2.6},
            {"p": toe[0], "x": X, "y": Z, "rx": toe[1] * 1.5, "ry": toe[1], "power": 2.4, "bulge": 0.02}], n=16))
        # the mitten hand (Q2): a block for the four fingers and a thumb, along the forearm's line
        el, wr = joints["elbow" + tag], joints["wrist" + tag]
        along = (wr - el).normalized()
        front = (-Y - along * (-Y).dot(along)).normalized()   # toward the figure's front, across the forearm
        flat = along.cross(front).normalized() * side         # the palm's normal, toward the body
        L, W, T = hand["length"], hand["width"], hand["thick"]
        palm = [(0.00, 0.62, 0.95), (0.22, 1.00, 1.05), (0.50, 1.00, 0.95), (0.80, 0.84, 0.70), (1.00, 0.52, 0.42)]
        curl = {0.80: 0.45, 1.00: 1.30}
        sections = [{"p": wr + along * (t * L) + flat * (T * curl.get(t, 0.0)), "x": front, "y": flat, "rx": W * w, "ry": T * h, "power": 2.6}
                    for t, w, h in palm]
        sections[0]["bulge"] = 0.004
        sections[-1]["bulge"] = 0.010
        forms.append(k.loft("hand" + tag, sections, n=16))
        root = wr + along * (0.16 * L)
        thumb = [root + front * (W * 0.55), root + front * (W * 1.16) + along * (0.15 * L) + flat * (T * 0.3),
                 root + front * (W * 1.36) + along * (0.36 * L) + flat * (T * 0.9)]
        forms.append(k.tube("thumb" + tag, thumb, [T * 0.95, T * 0.85, T * 0.62], side=flat, n=10, bulge=(0.0, 0.006)))
        if p.get("webbed"):
            # a web between the thumb and the fingers' block (the Merfolk's webbed hands, T3)
            web = [root + front * (W * 0.62) + along * (0.12 * L), root + front * (W * 0.98) + along * (0.32 * L) + flat * (T * 0.5),
                   root + front * (W * 0.90) + along * (0.56 * L) + flat * (T * 0.75)]
            forms.append(k.tube("web" + tag, web, [(T * 0.55, T * 0.90), (T * 0.48, T * 0.85), (T * 0.36, T * 0.60)], side=flat, n=10, bulge=(0.0, 0.004)))
        joints["hand" + tag] = wr + along * (0.5 * L)
    return forms, joints


def joints_of(p):
    """A body's joints alone (what its skeleton is placed from): its forms are made and dropped again."""
    forms, joints = body_forms(p)
    for obj in forms:
        mesh = obj.data
        bpy.data.objects.remove(obj)
        bpy.data.meshes.remove(mesh)
    return joints


def build_ratios(b):
    """A build's ratios to its average body (bodies.as_build; all 1 for the average): the masses' strength, their reach
    across the chest, through it, on the arm and on the leg, the bust's and the seat's fullness."""
    b = b or {}
    return [b.get(key, 1.0) for key in ("muscle", "w", "d", "a", "l", "bust", "seat")]


def build_lengths(b):
    """A body's lengths to the human's (bodies.proportioned with masses; all 1 otherwise): the legs', the arms' and the
    trunk's. A mass reaches along its limb by this much: on short legs the thigh's and the calf's would else pile up at
    the knee."""
    b = b or {}
    return [b.get(key, 1.0) for key in ("ll", "al", "tl")]


def shape_man(m, j, b=None):
    """The man's masses: named by what they stand for, placed from the joints, as strong and as far out as the build is."""
    s, w, d, a, l, _, seat = build_ratios(b)
    ll, al, tl = build_lengths(b)
    sh, el, wr = j["shoulderL"], j["elbowL"], j["wristL"]
    hip, kn, an = j["hipL"], j["kneeL"], j["ankleL"]
    chest, waist, pelvis, clav = j["chest"], j["waist"], j["pelvis"], j["clavicle"]
    # (the pectorals and the belly's middle no fuller than a third over the average's: fuller, the cleft between them
    # draws a line and a shadow through the cloth laid over it)
    mass(m, (0.080 * w, chest.y - 0.100 * d, chest.z + 0.012), (0.105 * w, 0.075, 0.090 * tl), 0.017 * min(s, 1.3), (0, -1, 0.15))      # pectorals
    mass(m, (0.090 * w, clav.y - 0.070 * d, clav.z + 0.030), (0.090, 0.040, 0.018), 0.004 * s, (0, -1, 0.3))          # collarbones
    mass(m, (0.0, waist.y - 0.094 * d, waist.z + 0.010), (0.014, 0.030, 0.120 * tl), -0.003 * min(s, 1.0), (0, -1, 0), mirror=False)  # the belly's middle
    mass(m, (0.150 * w, chest.y + 0.030 * d, chest.z - 0.060), (0.075, 0.110, 0.150 * tl), 0.012 * s, (1, 0.3, 0))        # lats
    mass(m, (0.064 * w, chest.y + 0.105 * d, chest.z + 0.020), (0.110, 0.060, 0.140 * tl), 0.010 * s, (0, 1, 0))          # shoulder blades
    mass(m, (0.0, waist.y + 0.092 * d, waist.z + 0.070), (0.036, 0.050, 0.240 * tl), -0.006 * s, (0, 1, 0))           # the spine's groove
    mass(m, (0.072 * w, clav.y + 0.030 * d, clav.z + 0.052), (0.090, 0.075, 0.060), 0.014 * min(s, 1.3), (0.2, 0.3, 1))        # trapezius
    mass(m, (0.0, waist.y - 0.088 * d, waist.z - 0.040), (0.105, 0.050, 0.150 * tl), 0.005 * s, (0, -1, 0))           # the belly's wall
    mid = sh.lerp(el, 0.50)
    mass(m, mid + V((0, -0.024 * a, 0.004)), (0.080 * al, 0.052, 0.110 * al), 0.008 * s, (0, -1, 0.2))                     # biceps
    mass(m, mid + V((0, 0.028 * a, 0.0)), (0.080 * al, 0.052, 0.120 * al), 0.006 * s, (0, 1, 0))                           # triceps
    mass(m, el.lerp(wr, 0.26), (0.075 * al, 0.060, 0.085 * al), 0.007 * s)                                             # the forearm's belly
    mass(m, (hip.x + 0.008, pelvis.y + 0.090 * d, hip.z + 0.030), (0.105, 0.075, 0.105), 0.018 * seat, (0, 1, 0.1)) # glutes
    mass(m, hip.lerp(kn, 0.40) + V((0.004, -0.044 * l, 0)), (0.088, 0.066, 0.200 * ll), 0.010 * s, (0, -1, 0))        # quadriceps
    mass(m, hip.lerp(kn, 0.55) + V((0, 0.042 * l, 0)), (0.082, 0.060, 0.180 * ll), 0.006 * s, (0, 1, 0))              # hamstrings
    mass(m, kn + V((0, -0.044 * l, 0.004)), (0.050, 0.035, 0.050 * ll), 0.006, (0, -1, 0))                        # the kneecap
    mass(m, kn.lerp(an, 0.30) + V((0.004, 0.040 * l, 0)), (0.066, 0.056, 0.130 * ll), 0.014 * s, (0.15, 1, 0))        # the calf


def shape_woman(m, j, b=None):
    """The woman's masses."""
    s, w, d, a, l, bust, seat = build_ratios(b)
    ll, al, tl = build_lengths(b)
    el, wr = j["elbowL"], j["wristL"]
    hip, kn, an = j["hipL"], j["kneeL"], j["ankleL"]
    chest, waist, pelvis, clav = j["chest"], j["waist"], j["pelvis"], j["clavicle"]
    mass(m, (0.066 * w, chest.y - 0.088 * d, chest.z - 0.022), (0.072, 0.072, 0.076), 0.030 * bust, (0.1, -1, -0.1))    # the bust
    mass(m, (0.052 * w, chest.y + 0.090 * d, chest.z + 0.020), (0.095, 0.055, 0.130 * tl), 0.006 * s, (0, 1, 0))          # shoulder blades
    mass(m, (0.0, waist.y + 0.078 * d, waist.z + 0.070), (0.030, 0.045, 0.220 * tl), -0.005 * s, (0, 1, 0))           # the spine's groove
    mass(m, (0.056 * w, clav.y + 0.025 * d, clav.z + 0.042), (0.072, 0.062, 0.050), 0.007 * s, (0.2, 0.3, 1))        # trapezius
    mass(m, el.lerp(wr, 0.26), (0.060 * al, 0.048, 0.075 * al), 0.004 * s)                                             # the forearm's belly
    mass(m, (hip.x + 0.012, pelvis.y + 0.090 * d, hip.z + 0.025), (0.105, 0.078, 0.110), 0.022 * seat, (0, 1, 0.1)) # glutes
    mass(m, (hip.x + 0.052, pelvis.y + 0.004, hip.z + 0.040), (0.075, 0.105, 0.115), 0.012 * seat, (1, 0, 0))   # the hips' breadth
    mass(m, hip.lerp(kn, 0.40) + V((0.004, -0.040 * l, 0)), (0.082, 0.060, 0.200 * ll), 0.006 * s, (0, -1, 0))        # quadriceps
    mass(m, hip.lerp(kn, 0.55) + V((0, 0.040 * l, 0)), (0.076, 0.056, 0.180 * ll), 0.005 * s, (0, 1, 0))              # hamstrings
    mass(m, kn + V((0, -0.040 * l, 0.004)), (0.045, 0.032, 0.045 * ll), 0.004, (0, -1, 0))                        # the kneecap
    mass(m, kn.lerp(an, 0.30) + V((0.004, 0.036 * l, 0)), (0.058, 0.050, 0.125 * ll), 0.010 * s, (0.15, 1, 0))        # the calf


MASSES = {"man": shape_man, "woman": shape_woman}


def relax_near(obj, points, radius, iterations):
    """Smoothing only near `points`, fading out at `radius`: blends where a limb leaves the torso."""
    group = obj.vertex_groups.new(name="blend")
    for v in obj.data.vertices:
        nearest = min((v.co - q).length for q in points)
        if nearest < radius:
            group.add([v.index], smooth(1.0 - nearest / radius), "REPLACE")
    modifier = obj.modifiers.new("blend", "SMOOTH")
    modifier.iterations = iterations
    modifier.factor = 0.5
    modifier.vertex_group = group.name
    k.apply_modifiers([obj])
    obj.data.shade_smooth()


def build_body(p, material, voxel=0.0065):
    """The body as one closed surface of even quads lying on its sculpt, at its level's counts (lighten.light_body: the
    hands remade apart, finer), in `material`; and its joints."""
    forms, joints = body_forms(p)
    obj = fuse(forms, p["name"] + "-body", voxel, smoothing=round(36 * 0.005 / voxel))
    blend = []
    for tag, side in (("L", 1), ("R", -1)):
        sh, hip = joints["shoulder" + tag], joints["hip" + tag]
        blend += [sh + V((-side * 0.02, 0, 0.01)), sh + V((-side * 0.035, -0.03, -0.075)), sh + V((-side * 0.035, 0.04, -0.075)),
                  hip + V((side * 0.02, 0, 0.03)), hip + V((-side * 0.05, 0, -0.02))]
    relax_near(obj, blend, 0.095, round(70 * 0.005 / voxel))
    MASSES[p["masses"]](obj.data, joints, p.get("build"))
    relax(obj, 10)
    detail = p["detail"]
    wrists = [(joints["wrist" + tag], joints["wrist" + tag] - joints["elbow" + tag]) for tag in "LR"]
    lighten.light_body(obj, detail["body"], wrists, hand=detail["hand"], body_limit=detail.get("body_limit", 0.012))
    obj.data.materials.append(material)
    return obj, joints


# ---- the head -------------------------------------------------------------------------------------------------------
def head_shape(h):
    """The head's surface as a function of a direction from its centre: an ellipsoid of `size` narrowed at the jaw and
    the back of the skull, its face flatter and its cheeks squarer in plan (a superellipse of power `flat`)."""
    centre, (sx, sy, sz) = V(h["centre"]), h["size"]

    def shape(d):
        x, y, z = d.x, d.y, d.z
        low, high, back = max(0.0, -z), max(0.0, z), max(0.0, y)
        wx = (1.0 - h["jaw"] * low ** 1.3) * (1.0 - 0.06 * high)
        wy = (0.90 + 0.05 * low) if y < 0 else (1.0 - 0.45 * low ** 1.2)
        wz = 1.0 - 0.24 * back if z < 0 else 1.0
        n = h.get("flat", 2.7) if y < 0 else 2.15
        r = math.hypot(x, y)
        if r > 1e-6:
            boost = 1.0 / (((abs(x) / r) ** n + (abs(y) / r) ** n) ** (1.0 / n))
            x, y = x * boost, y * boost
        point = centre + V((x * sx * wx, y * sy * wy, z * sz * wz))
        snout = h.get("snout")
        if snout and d.y < 0:
            # a muzzle (the lizardmen's): the face drawn forward about a middle pitch, narrower toward its tip;
            # `snout`: its length, its half width (degrees of yaw), its middle pitch, its half height, its droop
            length, half_yaw, middle, half_pitch, droop = snout
            u = abs(math.degrees(math.atan2(d.x, -d.y))) / half_yaw
            v = abs(math.degrees(math.asin(max(-1.0, min(1.0, d.z)))) - middle) / half_pitch
            if u < 1.0 and v < 1.0:
                w = (1.0 - u ** 2.2) * (1.0 - v ** 2.2)
                point = V((point.x * (1.0 - 0.30 * w), point.y - length * w, point.z + droop * length * w))
        return point
    return shape


def build_head(p, m, voxel=0.0032):
    """The head of `p` (p["head"]: its frame from the body and its features from the face): a skull and ears fused, the
    nose, brow, eye hollows, cheeks, chin and lips as masses, remade as quads at its level's counts (lighten.light_head:
    finer over the face); then the flat
    pieces (eye whites, irises, pupils, glints, lash lines, brows, the mouth and a woman's lips) laid on by rays from its
    centre. Returns the pieces (the head first) and the head's shape (what hair is fitted to)."""
    h = p["head"]
    centre = V(h["centre"])
    shape = head_shape(h)

    def on(yaw, pitch, lift=0.0):
        d = k.direction(yaw, pitch)
        return shape(d) + d * lift

    forms = [k.shaped_sphere("skull", shape, cuts=12)]
    e = h["ear"]
    for side in (-1, 1):
        ear = on(side * 90, -5)
        if h.get("ear_none"):
            continue                    # (the Half-Beast's and the lizardman's heads: no ears of the head's own)
        if "ear_long" in h:
            # a long, pointed ear (the elves'): the ear drawn out to a point up, back and a little out; `ear_long`: the
            # point's length and its way (out, back, up)
            reach, out_, back_, up_ = h["ear_long"]
            axis = V((side * out_, back_, up_)).normalized()

            def long_ear(d, c=ear, s=side, axis=axis, reach=reach):
                t = max(0.0, d.dot(axis))
                # (the point narrows in height and depth but keeps most of its thickness: thinner than two of the fuse's
                # voxels, it came out of the remesh as a sliver QuadriFlow refuses)
                narrow = 1.0 - 0.45 * t * t
                base = V((d.x * 0.009 * e * (1.0 - 0.12 * t * t), d.y * 0.015 * e * narrow, d.z * 0.029 * e * narrow))
                return c + V((s * 0.004, 0.006, 0.0)) + base + axis * (reach * t ** 2.2)

            forms.append(k.shaped_sphere(f"ear{side}", long_ear, cuts=5))
            continue
        if "ear_fin" in h:
            # the Merfolk's fin ears: only the ear's root is the head's own; the fin is made apart (ear_fins)
            forms.append(k.shaped_sphere(f"ear{side}", lambda d, c=ear, s=side: c + V((d.x * 0.006 + s * 0.002, d.y * 0.010 + 0.006, d.z * 0.014)), cuts=3))
            continue
        forms.append(k.shaped_sphere(f"ear{side}", lambda d, c=ear, s=side: c + V((d.x * 0.009 * e + s * 0.004, d.y * 0.015 * e + 0.006, d.z * 0.029 * e)), cuts=3))
    head = fuse(forms, p["name"] + "-head", voxel, smoothing=round(16 * 0.0026 / voxel))
    mesh = head.data
    nose_out, nose_wide = h["nose"]
    # the nose: a wedge that rises from the bridge to the tip and drops under it; its sides run out smoothly into the
    # cheeks, so only its far side and its underside are drawn
    for v in mesh.vertices:
        d = v.co - centre
        if d.y >= 0:
            continue
        dn = d.normalized()
        yaw, pitch = math.degrees(math.atan2(dn.x, -dn.y)), math.degrees(math.asin(max(-1.0, min(1.0, dn.z))))
        if not -19.5 < pitch < 9.0:
            continue
        t = (8.0 - pitch) / 21.0
        rise = (0.22 + 0.78 * t) * smooth(min(1.0, t * 3.0)) if t <= 1.0 else smooth(1.0 - (t - 1.0) / 0.26)
        half = (3.6 + nose_wide * min(t, 1.0))
        u = abs(yaw) / half
        if u < 1.0:
            v.co += V((0, -1, 0.10)) * (nose_out * rise * math.cos(u * math.pi / 2) ** 1.3)
    mass(mesh, on(6.2, -14.6, 0.004), (0.0075, 0.012, 0.0065), h.get("nostril", 0.003), (0.5, -1, 0))   # the nostril's wing
    if "snout" in h:
        ridge = k.direction(h["eye"]["yaw"], h["eye"]["pitch"] + 8.0)
        mass(mesh, on(h["eye"]["yaw"], h["eye"]["pitch"] + 8.0, -0.003), (0.030, 0.034, 0.014), h["brow"], ridge)      # the ridge over a muzzled head's eye
    else:
        mass(mesh, on(21, 9.5, -0.004), (0.034, 0.030, 0.014), h["brow"], (0, -1, 0))                   # the brow's ridge
    if "snout" in h:
        hollow = k.direction(h["eye"]["yaw"], h["eye"]["pitch"])
        mass(mesh, on(h["eye"]["yaw"], h["eye"]["pitch"], -0.002), (0.022, 0.026, 0.014), -0.0030, -hollow)   # the eye's hollow, where a muzzled head has its eyes
    else:
        mass(mesh, on(16, 0.0, -0.002), (0.020, 0.026, 0.012), -0.0040, (0, -1, 0))                     # the eye's hollow
    mass(mesh, on(31, -12, -0.004), (0.026, 0.030, 0.024), h["cheek"], (0.5, -1, 0))                    # the cheekbone
    mass(mesh, on(0, -45, -0.004), (0.026, 0.030, 0.020), h["chin"], (0, -1, -0.2), mirror=False)       # the chin
    mass(mesh, on(0, -29.0, -0.003), (0.020, 0.020, 0.007), h.get("lips", 0.0022), (0, -1, 0), mirror=False)   # the upper lip
    mass(mesh, on(0, -33.6, -0.003), (0.017, 0.020, 0.007), h.get("lips", 0.0022) * 1.3, (0, -1, 0), mirror=False)  # the lower lip
    mass(mesh, on(38, -34, -0.004), (0.028, 0.034, 0.030), -0.003, (0.5, -1, 0))                        # under the cheek
    relax(head, 5)
    detail = p["detail"]
    lighten.light_head(head, detail["head"], centre, face=detail["face"], limit=detail.get("face_limit", 0.003))
    head.data.materials.append(m["skin"])
    if h.get("throat"):
        # a paler lower jaw and throat (the lizardmen's, the slot `skin.belly`): the head's faces under the mouth's line
        head.data.materials.append(m["skin.belly"])
        under = h["mouth"]["pitch"] - 3.0
        for polygon in head.data.polygons:
            d = (V(polygon.center) - centre).normalized()
            if d.y < 0 and math.degrees(math.asin(max(-1.0, min(1.0, d.z)))) < under + 9.0 * (abs(math.degrees(math.atan2(d.x, -d.y))) / 60.0) ** 2 \
                    and abs(math.degrees(math.atan2(d.x, -d.y))) < 60.0:
                polygon.material_index = 1
    parts = [head]
    # the flat pieces, laid on the finished surface by rays from the head's centre
    tree = BVHTree.FromObject(head, bpy.context.evaluated_depsgraph_get())

    def surface(d):
        hit = tree.ray_cast(centre, d)[0]
        return hit if hit is not None else shape(d)

    def lay(name, outline, lift, material, hub=None):
        bm = bmesh.new()
        verts = []
        for yaw, pitch in outline:
            d = k.direction(yaw, pitch)
            verts.append(bm.verts.new(surface(d) + d * lift))
        if hub is not None:
            d = k.direction(*hub)
            middle = bm.verts.new(surface(d) + d * (lift * 1.5))
            for i in range(len(verts)):
                bm.faces.new((verts[i], verts[(i + 1) % len(verts)], middle))
        else:
            bm.faces.new(verts)
        parts.append(k.new_object(name, bm, material, smooth=True))

    def almond(yaw, pitch, a, b, power=1.5, n=16, turn=0.0):
        c, s_ = math.cos(math.radians(turn)), math.sin(math.radians(turn))
        return [(yaw + px * c - py * s_, pitch + px * s_ + py * c) for px, py in k.superellipse(n, a, b, power)]

    def band(points, thickness):
        return [(y, q + thickness * s) for y, q, s in points] + [(y, q - thickness * s) for y, q, s in reversed(points)]

    eye = h["eye"]
    brow = h["browline"]
    for side in (-1, 1):
        ey, ep = side * eye["yaw"], eye["pitch"]
        tilt = side * eye.get("tilt", 0.0)          # the outer corner raised by so many degrees (the elves' eyes)
        if eye.get("slit"):
            # a slit eye (the lizardmen's): the whole eye in the eyes' colour, a pupil as an upright slit
            lay(f"eye-white{side}", almond(ey, ep, eye["wide"], eye["high"], turn=tilt), 0.0010, m["eyes"], hub=(ey, ep))
            lay(f"eye-pupil{side}", almond(ey, ep, eye["wide"] * 0.15, eye["high"] * 0.90, power=2.0, n=10), 0.0018, m["pupil"], hub=(ey, ep))
            lay(f"eye-glint{side}", almond(ey - 0.30 * eye["wide"], ep + 0.36 * eye["high"], eye["wide"] * 0.11, eye["high"] * 0.17, power=2.0, n=8),
                0.0022, m["glint"], hub=(ey - 0.30 * eye["wide"], ep + 0.36 * eye["high"]))
        else:
            lay(f"eye-white{side}", almond(ey, ep, eye["wide"], eye["high"], turn=tilt), 0.0010, m["eye.white"], hub=(ey, ep))
            iy, ip = ey - side * 0.3, ep + 0.25
            lay(f"eye-iris{side}", almond(iy, ip, eye["iris"], eye["iris"] * 1.06, power=2.0), 0.0016, m["eyes"], hub=(iy, ip))
            lay(f"eye-pupil{side}", almond(iy, ip, eye["iris"] * 0.50, eye["iris"] * 0.53, power=2.0, n=10), 0.0020, m["pupil"], hub=(iy, ip))
            lay(f"eye-glint{side}", almond(iy - 0.36 * eye["iris"], ip + 0.38 * eye["iris"], eye["iris"] * 0.20, eye["iris"] * 0.22, power=2.0, n=8),
                0.0023, m["glint"], hub=(iy - 0.36 * eye["iris"], ip + 0.38 * eye["iris"]))
        lash = [(ey - side * eye["wide"] * 1.08, ep + 0.2, 0.5), (ey - side * eye["wide"] * 0.5, ep + eye["high"] * 0.92, 1.0),
                (ey, ep + eye["high"] * 1.10, 1.0), (ey + side * eye["wide"] * 0.5, ep + eye["high"] * 0.92, 1.0),
                (ey + side * eye["wide"] * 1.14, ep + 0.7, 0.5)]
        if tilt:
            c, s_ = math.cos(math.radians(tilt)), math.sin(math.radians(tilt))
            lash = [(ey + (y - ey) * c - (q - ep) * s_, ep + (y - ey) * s_ + (q - ep) * c, w) for y, q, w in lash]
        lay(f"lash{side}", band(lash, 0.55), 0.0022, m["brows"])
        if h.get("brows", True):
            lay(f"brow{side}", band([(side * y, q, s) for y, q, s in brow], h["browthick"]), 0.0028, m["brows"])
    mouth = h["mouth"]
    if "snout" in h:
        # a muzzle's mouth runs round it: a strip of many short pieces (one flat piece would cut through the muzzle),
        # and the nostrils as two small dark spots
        bm = bmesh.new()
        pairs = []
        for i in range(33):
            t = -1.0 + i / 16.0
            yaw, pitch = mouth["wide"] * t, mouth["pitch"] + mouth["curve"] * abs(t) ** 2
            half = mouth["thick"] * (1.0 - 0.6 * abs(t) ** 3)
            a, b = k.direction(yaw, pitch + half), k.direction(yaw, pitch - half)
            pairs.append((bm.verts.new(surface(a) + a * 0.0016), bm.verts.new(surface(b) + b * 0.0016)))
        for (a0, a1), (b0, b1) in zip(pairs, pairs[1:]):
            bm.faces.new((a0, b0, b1, a1))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        parts.append(k.new_object("mouth", bm, m["brows"], smooth=True))
        for yaw, pitch, size in h.get("nostrils", ()):
            for side in (-1, 1):
                lay(f"nostril{side}", almond(side * yaw, pitch, size * 0.55, size, power=2.0, n=8, turn=side * 24.0), 0.0016, m["brows"], hub=(side * yaw, pitch))
        return parts, shape
    line = [(mouth["wide"] * t, mouth["pitch"] + mouth["curve"] * (abs(t) ** 2), 1.0 - 0.6 * abs(t) ** 3) for t in (-1, -0.6, -0.25, 0, 0.25, 0.6, 1)]
    lay("mouth", band(line, mouth["thick"]), 0.0016, m["brows"])
    if mouth["lip"] > 0:
        lay("lip-lower", almond(0, mouth["pitch"] - 1.9, mouth["wide"] * 0.66, 1.5 * mouth["lip"], power=1.7), 0.0012, m["lips"], hub=(0, mouth["pitch"] - 1.9))
        upper = [(mouth["wide"] * 0.78 * t, mouth["pitch"] + 0.95 * mouth["lip"] * (1 - abs(t) ** 1.5) + (0.35 if abs(t) < 0.3 else 0.0), 1.0) for t in (-1, -0.5, -0.2, 0, 0.2, 0.5, 1)]
        lay("lip-upper", [(y, q) for y, q, _ in upper] + [(mouth["wide"] * 0.78 * t, mouth["pitch"] + 0.3) for t in (1, 0, -1)], 0.0012, m["lips"])
    if "ear_fin" in h:
        fins = ear_fins(p, on, m)
        k.apply_modifiers(fins)
        parts += fins
    return parts, shape


def ear_fins(p, on, m, ribs=((-40.0, 0.050), (0.0, 0.080), (38.0, 0.088), (72.0, 0.062))):
    """The Merfolk's fin ears (T3): on each side a fan of ribs in the skin's colour with a membrane between them (the slot
    `crest`: the fins' colour is a choice), standing out of the ear's root and back. `ear_fin` of the face: its size and
    how far back it sweeps (degrees); `ribs`: each rib's angle up from level and its length."""
    size, sweep = p["head"]["ear_fin"]
    pieces = []
    for side in (-1, 1):
        root = on(side * 90, -5) + V((side * 0.002, 0.006, 0.0))
        out = V((side * math.cos(math.radians(sweep)), math.sin(math.radians(sweep)), 0.0))        # out of the head and back
        up = V((side * 0.10, 0.06, 1.0))
        up = (up - out * up.dot(out)).normalized()
        normal = out.cross(up).normalized()

        def at(angle, length, root=root, out=out, up=up):
            return root + (out * math.cos(math.radians(angle)) + up * math.sin(math.radians(angle))) * (length * size)

        tips = [at(angle, length) for angle, length in ribs]
        outline = [root - up * (0.013 * size) - out * 0.004]
        for i, tip in enumerate(tips):
            outline.append(tip)
            if i + 1 < len(tips):
                (a0, l0), (a1, l1) = ribs[i], ribs[i + 1]
                for share in (0.3, 0.5, 0.7):                 # the membrane's edge dips between two ribs
                    dip = 1.0 - 0.22 * math.sin(math.pi * share)
                    outline.append(at(a0 + (a1 - a0) * share, (l0 + (l1 - l0) * share) * dip))
        outline.append(root + up * (0.017 * size) - out * 0.004)
        bm = bmesh.new()
        hub = bm.verts.new(root - out * 0.007)
        rim = [bm.verts.new(q) for q in outline]
        for q0, q1 in zip(rim, rim[1:]):
            bm.faces.new((hub, q0, q1))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        web = k.new_object(f"{p['name']}-fin{side}", bm, m["crest"], smooth=False)
        k.solidify(web, 0.0030, offset=0.0)
        pieces.append(web)
        for i, tip in enumerate(tips):
            pieces.append(k.tube(f"{p['name']}-fin{side}-rib{i}", [root - out * 0.008, root.lerp(tip, 0.5), tip],
                                 [0.0038 * size, 0.0030 * size, 0.0013 * size], side=normal, n=6, material=m["skin"]))
    return pieces


# ---- hair: sculpted masses (Q3) ---------------------------------------------------------------------------------------
def hairline(d, brow=0.50, nape=-0.66):
    """The lowest z a direction may have and still be under hair: high over the brow, down before the ear, lower at the
    nape."""
    if d.y < 0:
        return 0.03 + brow * min(1.0, -d.y / 0.75) ** 1.5
    return 0.03 + nape * min(1.0, d.y / 0.8)


def open_cap(p, shape, material, style):
    """The hair's cap as an open shell that meets the skin exactly on the hairline: only the faces under hair are kept,
    the vertices of their outer edge are slid up onto the hairline and laid just outside the skull, and from there one
    more ring of faces turns straight in to an open edge inside the head; so the cap's edge is the hairline at any
    fineness (a closed cap dives into the skull there, and coarse faces make that edge a stair)."""
    centre = V(p["head"]["centre"])
    brow, nape, out = style.get("brow", 0.50), style.get("nape", -0.66), style.get("cap", 1.05)

    def under(d):
        return d.z > hairline(d, brow, nape)

    bm = k.quad_sphere(style["cap_cuts"])
    drop = [f for f in bm.faces if not any(under(v.co.normalized()) for v in f.verts)]
    bmesh.ops.delete(bm, geom=drop, context="FACES")
    up = V((0, 0, 1))
    for v in bm.verts:
        d = v.co.normalized()
        if under(d):
            v.co = centre + (shape(d) - centre) * out
            continue
        low, high = 0.0, 1.0                       # along the arc from d to straight up: the first place under hair
        for _ in range(30):
            mid = (low + high) / 2
            if under((d + (up - d) * mid).normalized()):
                high = mid
            else:
                low = mid
        d = (d + (up - d) * high).normalized()
        v.co = centre + (shape(d) - centre) * 1.012
    # The cap's edge stands a hair outside the skin on the hairline; from there the cap turns straight in, to an open edge
    # well inside the head (0.95 of the skull's radius). So what shows is where the cap meets the skin, the hairline
    # exactly, and no open edge lies just under the skin, where the engine's ink (which draws an edge a little through
    # what covers it) showed it and Blender's didn't (the manager, 2026-11-16, from step 4a's close-ups).
    rim = [e for e in bm.edges if e.is_boundary]
    for elements in (bm.verts, bm.edges, bm.faces):
        elements.index_update()
    made = k.extrude_edges(bm, sorted(rim, key=lambda e: e.index))
    for v in made["geom"]:
        if isinstance(v, bmesh.types.BMVert):
            v.co = centre + (v.co - centre) * (0.95 / 1.012)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return k.new_object(p["name"] + "-hair-cap", bm, material)


def build_hair(p, shape, material, style, tie=None):
    """A hair style (parts/hair.py) on the head of `shape`, at p's level's fineness: an open cap over the skull and
    sculpted locks, each lock its own piece so the ink draws it. A lock marked "tie" (its fourth value) is what ties the
    hair, in the material `tie` (the slot `hair.tie`: its colour is a choice of its own)."""
    centre = V(p["head"]["centre"])
    style = dict(style, **p["detail"]["hair"])

    def on(yaw, pitch, out=1.0):
        d = k.direction(yaw, pitch)
        return centre + (shape(d) - centre) * out

    parts = [open_cap(p, shape, material, style)]
    for index, (points, radius, flat, *more) in enumerate(style["locks"]):
        path = [on(*q) for q in points]
        count = len(path)
        taper = style.get("taper", 0.75)
        radii = [(radius * (1.0 - taper * (i / (count - 1)) ** 1.8), radius * flat * (1.0 - taper * (i / (count - 1)) ** 1.8)) for i in range(count)]
        tied = bool(more) and more[0] == "tie"
        if more and not tied:                      # the lock's own thickness along its length, a share per point (a braid's plaits)
            radii = [(rx * share, ry * share) for (rx, ry), share in zip(radii, more[0])]
        lock = k.tube(f"{p['name']}-lock{index}", path, radii, side=Z, n=style["lock_sides"], bulge=(0.006, 0.004),
                      material=tie if tied and tie is not None else material)
        parts.append(k.subdivide(lock, style["lock_levels"]))
    k.apply_modifiers(parts)
    return parts


# ---- beards: sculpted masses on the jaw (GM2 step 9a) ----------------------------------------------------------------
def build_beard(p, m, style):
    """A beard (parts/beard.py) on the head of `p`: a shell cut from a quad sphere by the style's region, lying on the skin
    at its edge and rising to its thickness inside, and the style's locks (a hanging beard, a braid). The head it is laid
    on is built and dropped again; `p`'s face is the one the beard is made to (parts/beard.py: the race's widest jaw and
    fullest chin for that sex, so one beard fits every face)."""
    h = p["head"]
    centre = V(h["centre"])
    head_parts, shape = build_head(p, m)
    tree = BVHTree.FromObject(head_parts[0], bpy.context.evaluated_depsgraph_get())
    for part in head_parts:
        mesh = part.data
        bpy.data.objects.remove(part)
        bpy.data.meshes.remove(mesh)

    def surface(d):
        hit = tree.ray_cast(centre, d)[0]
        return hit if hit is not None else shape(d)

    def angles(d):
        return math.degrees(math.atan2(d.x, -d.y)), math.degrees(math.asin(max(-1.0, min(1.0, d.z))))

    region = style["region"]
    parts = []
    if "flecks" in style:
        # stubble: short flecks of hair laid on the skin all over the region, each a small flat piece (a shell in the
        # hair's colour, however thin, reads as a beard)
        step, long_, wide = style["flecks"]
        bm = bmesh.new()
        yaw = -100.0
        while yaw <= 100.0:
            pitch = -74.0
            while pitch <= 14.0:
                y = yaw + step * 0.38 * math.sin(yaw * 12.9898 + pitch * 78.233)
                q = pitch + step * 0.38 * math.cos(yaw * 4.1414 + pitch * 9.7373)
                if region(y, q) > 1.0:
                    turn = math.radians((yaw * 37.0 + pitch * 53.0) % 180.0)
                    along, across = (math.cos(turn), math.sin(turn)), (-math.sin(turn), math.cos(turn))
                    corners = []
                    for a, b in ((long_, 0.0), (0.0, wide), (-long_, 0.0), (0.0, -wide)):
                        d = k.direction(y + (a * along[0] + b * across[0]) / max(0.3, math.cos(math.radians(q))), q + a * along[1] + b * across[1])
                        corners.append(bm.verts.new(surface(d) + d * 0.0007))
                    bm.faces.new(corners)
                pitch += step
            yaw += step
        parts.append(k.new_object(p["name"] + "-stubble", bm, m["hair"], smooth=False))
        return parts
    lift, rise = style["lift"], style.get("rise", 6.0)
    anchors = [k.direction(*a) for a in style["anchors"]]
    bm = k.quad_sphere(style.get("cuts", p["detail"]["hair"]["cap_cuts"] * 2))
    inside = {v: region(*angles(v.co.normalized())) for v in bm.verts}
    drop = [f for f in bm.faces if not any(inside[v] > 0 for v in f.verts)]
    bmesh.ops.delete(bm, geom=drop, context="FACES")
    for v in bm.verts:
        d = v.co.normalized()
        depth = region(*angles(d))
        if depth <= 0:                               # an edge vertex: slid along the arc to the nearest anchor onto the region's edge
            anchor = max(anchors, key=lambda a: a.dot(d))
            low, high = 0.0, 1.0
            for _ in range(30):
                mid = (low + high) / 2
                if region(*angles((d + (anchor - d) * mid).normalized())) > 0:
                    high = mid
                else:
                    low = mid
            d = (d + (anchor - d) * high).normalized()
            depth = 0.0
        v.co = surface(d) + d * (0.0008 + lift * smooth(min(1.0, depth / rise)))
    rim = [e for e in bm.edges if e.is_boundary]
    for elements in (bm.verts, bm.edges, bm.faces):
        elements.index_update()
    made = k.extrude_edges(bm, sorted(rim, key=lambda e: e.index))
    for v in made["geom"]:
        if isinstance(v, bmesh.types.BMVert):
            v.co = centre + (v.co - centre) * 0.93   # the edge turns in under the skin
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    parts.append(k.new_object(p["name"] + "-beard", bm, m["hair"]))

    def on(yaw, pitch, out=1.0):
        d = k.direction(yaw, pitch)
        return centre + (shape(d) - centre) * out

    detail = p["detail"]["hair"]
    for index, (points, radius, flat, *more) in enumerate(style.get("locks", [])):
        path = [on(*q) for q in points]
        count = len(path)
        taper = style.get("taper", 0.6)
        radii = [(radius * (1.0 - taper * (i / (count - 1)) ** 1.8), radius * flat * (1.0 - taper * (i / (count - 1)) ** 1.8)) for i in range(count)]
        if more:
            radii = [(rx * share, ry * share) for (rx, ry), share in zip(radii, more[0])]
        lock = k.tube(f"{p['name']}-beard-lock{index}", path, radii, side=X, n=detail["lock_sides"], bulge=(0.006, 0.004), material=m["hair"])
        parts.append(k.subdivide(lock, detail["lock_levels"]))
    k.apply_modifiers(parts)
    return parts


# ---- marks: flat pieces on the face's skin (GM2 step 9a) -------------------------------------------------------------
def build_marks(p, m, pieces):
    """A mark (parts/mark.py) on the head of `p`, whose face is the one the mark is made for (a mark lies on the skin, so it
    is made per face): each piece of `pieces` a strip ("strip": points (yaw, pitch, half width in degrees), laid as quads
    across its line) or a spot ("spot": yaw, pitch, radius in degrees, sides), lifted `lift` off the skin. The head it is
    laid on is built and dropped again."""
    centre = V(p["head"]["centre"])
    head_parts, shape = build_head(p, m)
    tree = BVHTree.FromObject(head_parts[0], bpy.context.evaluated_depsgraph_get())
    for part in head_parts:
        mesh = part.data
        bpy.data.objects.remove(part)
        bpy.data.meshes.remove(mesh)

    def at(yaw, pitch, lift):
        d = k.direction(yaw, pitch)
        hit = tree.ray_cast(centre, d)[0]
        return (hit if hit is not None else shape(d)) + d * lift

    bm = bmesh.new()
    for piece in pieces:
        lift = piece.get("lift", 0.0010)
        if "strip" in piece:
            points = piece["strip"]
            pairs = []
            for i, (yaw, pitch, half) in enumerate(points):
                a, b = points[max(0, i - 1)], points[min(len(points) - 1, i + 1)]
                dy, dp = b[0] - a[0], b[1] - a[1]
                size = math.hypot(dy, dp) or 1.0
                ny, np_ = -dp / size, dy / size
                pairs.append((bm.verts.new(at(yaw + ny * half, pitch + np_ * half, lift)), bm.verts.new(at(yaw - ny * half, pitch - np_ * half, lift))))
            for (a0, a1), (b0, b1) in zip(pairs, pairs[1:]):
                bm.faces.new((a0, b0, b1, a1))
        else:
            yaw, pitch, radius, sides = piece["spot"]
            rim = [bm.verts.new(at(yaw + radius * math.cos(2 * math.pi * i / sides) / max(0.4, math.cos(math.radians(pitch))),
                                   pitch + radius * math.sin(2 * math.pi * i / sides), lift)) for i in range(sides)]
            bm.faces.new(rim)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    mark = k.new_object(p["name"] + "-mark", bm, m["marking"], smooth=True)
    mark["ink"] = 0                 # a mark is its colour alone: outlined, a thin scar or a freckle is all outline
    return [mark]


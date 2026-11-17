# The first humanoid's sample gear (Q9: a sword and shield with a chain shirt; Docs/Game/Hero.md, "The humanoid rig
# family"), approved by the user on 2026-11-16 once the shield was turned and stood off the arm. Samples for the first
# humanoid's approval; GM3 makes the game's items. The sword and the shield are rigid items, each built in its own
# space: the origin is the grip and the axes line up with the socket's (Docs/Animation.md, "Gear in detail"; Blender's
# axes, which a socket bone's are). The chain shirt is a garment made on each body it fits, over the plain outfit. Their
# palette slots are the gear's (look.GEAR_SLOTS), apart from the figure's: one actor palette colours both. Each is made
# at a detail level (levels.py): the battle's sword and shield are cut coarser, the battle's mail has half its rows.
# Metres.
import math

import bmesh
from mathutils import Vector

import bodies
import kit as k
import materials
import outfit
import rig as r
from make_models import export

V = Vector
X, Y, Z = V((1, 0, 0)), V((0, 1, 0)), V((0, 0, 1))


def sword(m, light, name="sword"):
    """An arming sword. Y: from the grip's middle to the point; Z: the edge; X: across the flat. `light`: the battle's
    (the rounded pieces not smoothed a second time, no fuller lines, no binding)."""
    parts = []
    smooth = (lambda obj, levels: obj) if light else k.subdivide
    blade = [(0.078, 0.0260, 0.0058), (0.30, 0.0245, 0.0054), (0.58, 0.0215, 0.0048), (0.76, 0.0170, 0.0040), (0.845, 0.0085, 0.0026), (0.880, 0.0010, 0.0008)]
    parts.append(k.loft(name + "-blade", [{"p": Y * s, "x": Z, "y": X, "rx": rx, "ry": ry, "power": 1.0} for s, rx, ry in blade], n=8,
                        cap_start="flat", cap_end="fan", material=m["metal.base"], smooth=False))
    # the fuller: a shallow groove's two edges down the blade's middle, each a thin raised line with no outline of its own
    for side in (() if light else (-1, 1)):
        for edge in (-1, 1):
            rail =k.tube(f"{name}-fuller{side}{edge}", [Y * 0.10 + Z * (edge * 0.0048) + X * (side * 0.0052), Y * 0.50 + Z * (edge * 0.0040) + X * (side * 0.0046),
                                                         Y * 0.56 + Z * (edge * 0.0006) + X * (side * 0.0042)], [0.0007, 0.0007, 0.0004], side=X, n=4,
                          material=m["metal.shade"], smooth=False)
            rail["ink"] = 0
            parts.append(rail)
    guard = k.tube(name + "-guard", [Y * 0.066 + Z * -0.098 + Y * 0.012, Y * 0.068 + Z * -0.050, Y * 0.068, Y * 0.068 + Z * 0.050, Y * 0.066 + Z * 0.098 + Y * 0.012],
                   [(0.0085, 0.0075), (0.0100, 0.0090), (0.0130, 0.0115), (0.0100, 0.0090), (0.0085, 0.0075)], side=X, n=8, power=3.0,
                   bulge=(0.003, 0.003), material=m["metal.trim"])
    parts.append(smooth(guard, 1))
    grip = k.tube(name + "-grip", [Y * -0.062, Y * -0.020, Y * 0.020, Y * 0.060], [(0.0125, 0.0105), (0.0140, 0.0118), (0.0140, 0.0118), (0.0128, 0.0108)],
                  side=X, n=10, cap_start="flat", cap_end="flat", material=m["grip"])
    parts.append(smooth(grip, 1))
    for at in (() if light else (-0.045, -0.015, 0.015, 0.045)):        # the grip's binding
        ring = k.tube(f"{name}-wrap{at}", [Y * (at - 0.0022), Y * (at + 0.0022)], [(0.0150, 0.0128), (0.0150, 0.0128)], side=X, n=10,
                      cap_start="flat", cap_end="flat", material=m["grip"], smooth=False)
        parts.append(ring)
    pommel = k.shaped_sphere(name + "-pommel", lambda d: Y * (-0.084) + V((d.x * 0.0135, d.y * 0.0150, d.z * 0.0235)), cuts=2 if light else 3,
                             material=m["metal.trim"])
    parts.append(smooth(pommel, 1))
    k.apply_modifiers(parts)
    return parts


def shield(m, light, name="shield", wide=0.50, high=0.64, curve=0.56, off=0.088):
    """A heater shield on the left forearm. Y: along the forearm; Z: out of its face; its point lies toward −X, which is
    down when the forearm lies level across the body in a guard. It stands `off` from the socket, so its bowed edges clear
    the hand. `light`: the battle's (fewer rings in the face, the boss and rivets cut coarser and not smoothed)."""
    parts = []

    def outline(scale):
        """The heater's edge in its own flat (u across, v from the top edge down to the point)."""
        points = []
        half, top = wide / 2 * scale, 0.26 * high * scale
        for i in range(9):                                # the flat top with rounded corners
            t = i / 8
            points.append((-half + 2 * half * t, -top + 0.012 * scale * math.sin(math.pi * t)))
        for i in range(1, 17):                            # the right flank curving in to the point
            t = i / 16
            points.append((half * (1 - t ** 2.3), -top + (high * scale) * t))
        for i in range(15, 0, -1):
            t = i / 16
            points.append((-half * (1 - t ** 2.3), -top + (high * scale) * t))
        return points

    def place(u, v, lift=0.0):
        """Wraps the flat shape about the forearm's axis: u bends around, the face bows outward."""
        angle = u / curve
        return V((-v, -curve * math.sin(angle), curve * math.cos(angle) - curve + off + lift))

    def plate(label, scale, lift, thickness, material, inner=None):
        """The face's plate (closed, in rings drawn to the middle) or, with `inner`, the rim's band between two outlines."""
        bm = bmesh.new()
        ring = [bm.verts.new(place(u, v, lift)) for u, v in outline(scale)]
        if inner is None:
            centre_v = 0.06 * high
            previous = ring
            for share in ((0.56, 0.14) if light else (0.78, 0.56, 0.34, 0.14)):
                current = [bm.verts.new(place(u * share / scale, centre_v + (v - centre_v) * share / scale, lift)) for u, v in outline(scale)]
                for i in range(len(ring)):
                    j = (i + 1) % len(ring)
                    bm.faces.new((previous[i], previous[j], current[j], current[i]))
                previous = current
            hub = bm.verts.new(place(0.0, centre_v, lift))
            for i in range(len(ring)):
                bm.faces.new((previous[i], previous[(i + 1) % len(ring)], hub))
        else:
            core = [bm.verts.new(place(u, v, lift)) for u, v in outline(inner)]
            for i in range(len(ring)):
                j = (i + 1) % len(ring)
                bm.faces.new((ring[i], ring[j], core[j], core[i]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        obj = k.new_object(f"{name}-{label}", bm, material, smooth=True)
        k.solidify(obj, thickness, offset=-1.0)
        return obj

    parts.append(plate("field", 0.965, 0.0, 0.012, m["wood"]))
    parts.append(plate("rim", 1.0, 0.004, 0.018, m["metal.base"], inner=0.90))
    boss = k.shaped_sphere(name + "-boss", lambda d: place(0, 0.05 * high, 0.0) + V((d.x * 0.060, d.y * 0.060, max(0.0, d.z) * 0.042 + 0.004)),
                           cuts=3 if light else 4, material=m["metal.trim"])
    parts.append(boss if light else k.subdivide(boss, 1))
    for u, v in ((-0.17, -0.10), (0.17, -0.10), (-0.135, 0.13), (0.135, 0.13), (0.0, 0.34)):     # rivets
        centre = place(u, v * high / 0.64, 0.010)
        rivet = k.shaped_sphere(f"{name}-rivet{u}{v}", lambda d, c=centre: c + V((d.x * 0.010, d.y * 0.010, d.z * 0.006)), cuts=1 if light else 2,
                                material=m["metal.trim"])
        parts.append(rivet if light else k.subdivide(rivet, 1))
    k.apply_modifiers(parts)
    return parts


def chain_shirt(mk):
    """A mail shirt to the upper thigh with short sleeves, worn over the plain outfit (made after it, on the body as the
    outfit left it): the trunk and the sleeves cut from the body's surface, a skirt of its own, its rows of rings drawn
    as thin dark ribbons (no outline, no hatching). Returns its pieces."""
    j, m = mk.joints, mk.m
    high = j["neck0"].z - 0.012
    waist = j["waist"].z - 0.030
    low = waist - 0.004
    hem = j["hipL"].z - 0.075
    arms = {s: mk.bone(f"upper_arm.{s}") for s in ("L", "R")}
    sleeve = 0.56

    def keep(c, bone):
        if bone.startswith(("forearm", "hand", "fingers", "thumb")):
            return False
        if bone.startswith("upper_arm"):
            return mk.along(bone, c) < sleeve
        return low < c.z < high

    planes = [(head.lerp(tail, sleeve), (tail - head).normalized()) for head, tail in arms.values()]
    before = len(mk.pieces)
    mk.shell("mail", keep, 0.011, m["metal.base"], planes=planes, heights=(low, high), thickness=0.003)
    # it stands clear of the outfit's own skirt under it, pleats and all (the tunic's reaches 38 mm off the body at this
    # hem's height; with a flare of 16 mm the two were 2 to 4 mm apart there and crossed in a stride: the manager, 2026-11-16)
    mk.skirt("mail-skirt", waist + 0.004, hem, m["metal.base"], flare=0.032, pleats=6, depth=0.004, ease=0.030, rings=6)
    mk.shell("mail-collar", lambda c, bone: not bone.startswith(("upper_arm", "forearm")) and high - 0.016 < c.z < high + 0.004, 0.015, m["metal.trim"],
             heights=(high + 0.004, high - 0.016), thickness=0.003, taut=outfit.COLLAR_TAUT)
    # the rows of rings: the mail's surface cut by level planes, each cut drawn as a thin dark ribbon
    made = mk.pieces[before:before + 2]
    rows = []
    for piece in made:
        zs = [v.co.z for v in piece.data.vertices]
        z = min(zs) + 0.012
        while z < max(zs) - 0.004:
            bm = bmesh.new()
            bm.from_mesh(piece.data)
            cut = bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=V((0, 0, z)), plane_no=Z, clear_inner=False, clear_outer=False)
            edges = [e for e in cut["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
            ribbon = bmesh.new()
            for e in edges:
                a, b = e.verts
                # only the outer skin: its normal looks away from the body's axis
                if (a.normal.x * a.co.x + a.normal.y * (a.co.y - 0.0)) < 0:
                    continue
                quad = [ribbon.verts.new(a.co + a.normal * 0.0007 + Z * 0.0011), ribbon.verts.new(b.co + b.normal * 0.0007 + Z * 0.0011),
                        ribbon.verts.new(b.co + b.normal * 0.0007 - Z * 0.0011), ribbon.verts.new(a.co + a.normal * 0.0007 - Z * 0.0011)]
                ribbon.faces.new(quad)
            bm.free()
            if ribbon.faces:
                bmesh.ops.recalc_face_normals(ribbon, faces=ribbon.faces[:])
                row = k.new_object(f"{mk.p['name']}-mail-row-{len(rows)}", ribbon, m["metal.shade"], smooth=False)
                row["ink"] = 0
                rows.append((row, piece))
            else:
                ribbon.free()
            z += mk.p["detail"]["rows"]
    for row, piece in rows:
        r.bind_like(mk.rig, row, piece)
    if rows:                                           # every row one piece (one draw, not thirty); the ink leaves it alone
        joined = k.join([row for row, _ in rows], f"{mk.p['name']}-mail-rows")
        joined["ink"] = 0
        mk.pieces.append(joined)
    return mk.pieces[before:]


# ---- builders (manifest.json; each at its level, levels.py, its pieces joined per ink flag) ---------------------------
def sword_item(path, level):
    """The sword alone, for socket.hand.R."""
    k.join_by_ink(sword(materials.make(), level == "battle"), "sword")
    export(path)


def shield_item(path, level):
    """The shield alone, for socket.shield.L."""
    k.join_by_ink(shield(materials.make(), level == "battle"), "shield")
    export(path)


def chain_shirt_on(path, body, level):
    """The chain shirt made on `body` (a body's name, bodies/): the body is built with its outfit, the shirt made over
    it, then only the shirt and the skeleton are kept (a garment, bound to the wearer's bones by name)."""
    figure = bodies.build(body, level)
    pieces = chain_shirt(figure["maker"])
    bodies.keep_only(figure, pieces)
    k.join_by_ink(pieces, f"{body}-chain-shirt")
    export(path)

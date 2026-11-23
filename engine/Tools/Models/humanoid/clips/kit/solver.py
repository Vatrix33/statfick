# The arms' solver (GM2-P23; Docs/Game/Hero.md, "How it works", "The keying kit"): a hand is keyed by where its grip is
# and which way the weapon in it points (a key's "hands", poses.py); the upper arm, the forearm and the hand follow. Runs
# inside Blender (mathutils), called by bake.py for every key that names a hand.
#   "hands": {"R": {"at": [left, back, up],          the grip's place (the hand's socket), armature space, metres
#                   "aim": [left, back, up],         the way the weapon points (the socket's Y: from the grip to the point)
#                   "edge": [left, back, up],        the way the blade's edge looks (the socket's Z), made square to aim
#                   "elbow": [left, back, up],       the way the elbow points (default: down, out and a little back)
#                   "twist": 0.5,                    how much of the wrist's turn the forearm takes (0..1)
#                   "swing": true,                   whether the elbow may swing from where `elbow` points it
#                   "untwist": 0, "steady": 6},      a free hand's search: how much the forearm's turn counts, how much
#                                                    staying near the key before counts
#             "L": {"on": "R", "along": -0.17, "roll": 180}}   the second hand on the weapon the other hand holds: `along`
#                                                    metres from that grip along the weapon (or `offset` in the weapon's
#                                                    frame), turned `roll` degrees about it (`axis` "Z": the fist closed
#                                                    around the weapon's Z; `flip`: turned end for end)
# A grip without `edge` (or a second hand without `roll`) leaves the hand's turn about the weapon to the solver: the turn
# that leaves the wrist straightest without wringing the forearm, the arm clear of the trunk, near the key before.
import math

from mathutils import Matrix, Vector

V = Vector


def frame3(y, z_hint):
    y = y.normalized()
    z = z_hint - y * z_hint.dot(y)
    if z.length < 1e-6:
        z = V((0, 0, 1)) - y * y.z
    z.normalize()
    x = y.cross(z)
    return Matrix((x, y, z)).transposed()


# The turn about the weapon and the elbow's swing chosen for each hand at the key before (a clip's keys are solved in
# order; bake.py empties it at each clip's start).
LAST = {}


def trunk_of(bones):
    """The trunk as the solver knows it: for the chest's and the hips' bones, the frame and the half widths of an
    oval post around the bone (side to side, front to back, and how far along the bone it runs)."""
    return [(bones["chest"].matrix.inverted(), 0.170, 0.115, -0.26, 0.30), (bones["hips"].matrix.inverted(), 0.175, 0.125, -0.12, 0.16)]


def inside(trunk, point, room):
    """How deep a point lies inside the trunk kept clear by `room` metres (0 outside; 1 on its middle line)."""
    deepest = 0.0
    for frame, wide, deep, low, high in trunk:
        local = frame @ point
        if low <= local.y <= high:
            deepest = max(deepest, 1.0 - math.sqrt((local.x / (wide + room)) ** 2 + (local.z / (deep + room)) ** 2))
    return deepest


def crowding(trunk, p):
    """How far an arm on paper lies in the trunk: its elbow, its forearm's middle and its wrist."""
    return (inside(trunk, p["elbow"], 0.050) + inside(trunk, (p["elbow"] + p["wrist"]) / 2, 0.045) + inside(trunk, p["wrist"], 0.035))


def plan(shoulder, l1, l2, grip, in_hand, hint, sign, rel, swivel=0.0):
    """The arm for a grip's frame, on paper: the hand's frame, the elbow, where the wrist comes to, how far the wrist
    is bent (radians between the forearm and the hand), how far the hand is turned about the forearm from the elbow's
    plane, how far short the arm falls and its reach (the shoulder to the wrist over the arm's length)."""
    wanted = grip @ in_hand.inverted()                 # the hand's frame that puts the socket there
    reach = wanted.translation - shoulder
    d = max(abs(l1 - l2) + 1e-4, min(l1 + l2 - 1e-4, reach.length))
    u = reach.normalized()
    n = hint - u * hint.dot(u)
    if n.length < 1e-6:
        n = V((sign, 0, 0)) - u * u.x * sign
    n.normalize()
    if swivel:                                         # the elbow swung about the line from the shoulder to the wrist
        n = n * math.cos(swivel) + u.cross(n) * math.sin(swivel)
    a = (l1 * l1 - l2 * l2 + d * d) / (2 * d)
    elbow = shoulder + u * a + n * math.sqrt(max(0.0, l1 * l1 - a * a))
    wrist_at = shoulder + u * d
    fore3 = frame3(wrist_at - elbow, n)
    q = ((fore3 @ rel).inverted() @ wanted.to_3x3()).to_quaternion()
    axis = (rel.inverted() @ V((0, 1, 0))).normalized()
    turn = 2 * math.atan2(V((q.x, q.y, q.z)).dot(axis), q.w)
    turn = (turn + math.pi) % (2 * math.pi) - math.pi
    bend = (wrist_at - elbow).angle(wanted.to_3x3() @ V((0, 1, 0)), 0.0)
    return {"wanted": wanted, "elbow": elbow, "wrist": wrist_at, "n": n, "fore3": fore3, "turn": turn, "bend": bend,
            "short": max(0.0, reach.length - l1 - l2), "reach": reach.length / (l1 + l2)}


def solve_arm(bones, rest, update, side, spec, notes=None):
    """Places an arm for a hand's key. A grip is a place and the weapon's line; how the hand is turned about that
    line is the key's (`edge`: the knuckles' way, which a blade's edge follows; `roll` for a second hand) or, when the
    key leaves it out, the turn that leaves the wrist straightest without wringing the forearm (and stays near the
    turn of the key before). `notes` (a list) gets what the measure reads: the arm's wrist bend, forearm turn, reach, turn
    about the weapon and elbow swing (degrees), how far it falls short of the grip and how far it lies in the trunk."""
    upper, fore, hand = bones[f"upper_arm.{side}"], bones[f"forearm.{side}"], bones[f"hand.{side}"]
    socket = bones[f"socket.hand.{side}"]
    update()
    shoulder = upper.matrix.translation.copy()
    l1, l2 = upper.bone.length, fore.bone.length
    sign = 1 if side == "L" else -1
    hint = V(spec.get("elbow", (0.55 * sign, 0.35, -0.75)))
    in_hand = rest[hand.name].inverted() @ rest[socket.name]          # the socket in the hand's own frame
    rel = rest[fore.name].to_3x3().inverted() @ rest[hand.name].to_3x3()
    free = False
    if "on" in spec:                                   # on the weapon the other hand holds
        base = bones[f"socket.hand.{spec['on']}"].matrix @ Matrix.Translation(V(spec.get("offset", (0, spec.get("along", 0.0), 0))))
        if spec.get("axis") == "Z":                    # the fist closed around the weapon's Z (a crossbow's stock), the thumb's side ahead
            base = base @ Matrix.Rotation(math.pi / 2, 4, "X")
        if spec.get("flip"):                           # the hand turned end for end on the haft (thumb to the butt)
            base = base @ Matrix.Rotation(math.pi, 4, "X")
        if "roll" in spec:
            base = base @ Matrix.Rotation(math.radians(spec["roll"]), 4, "Y")
        else:
            free = True
    elif "edge" in spec:
        base = frame3(V(spec["aim"]), V(spec["edge"])).to_4x4()
        base.translation = V(spec["at"])
    else:
        base = frame3(V(spec["aim"]), V(spec["at"]) - shoulder).to_4x4()
        base.translation = V(spec["at"])
        free = True

    trunk = trunk_of(bones)

    def at(angle, swivel=0.0):
        return plan(shoulder, l1, l2, base @ Matrix.Rotation(angle, 4, "Y") if angle else base, in_hand, hint, sign, rel, swivel)

    angle, swivel = 0.0, 0.0
    if free:
        before = LAST.get(side)

        def cost(candidate, swung):
            p = at(candidate, swung)
            near = 0.0 if before is None else (abs((candidate - before[0] + math.pi) % (2 * math.pi) - math.pi) + abs(swung - before[1]))
            # `untwist`: how much each degree of the forearm's turn counts beside a degree of the wrist's bend (0: only past
            # 70 degrees; a hand that must stay unwrung through a long move asks for more). `steady`: how much staying near
            # the key before counts (per radian; 6 when not said): a hand whose answer snapped from one frame to the next
            # between two equally good ones asks for more
            return (math.degrees(p["bend"]) + 0.5 * max(0.0, abs(math.degrees(p["turn"])) - 70.0) + spec.get("untwist", 0.0) * abs(math.degrees(p["turn"]))
                    + 400.0 * p["short"] + 0.25 * abs(math.degrees(swung)) + spec.get("steady", 6.0) * near + 400.0 * crowding(trunk, p))

        # the hand's turn about the weapon, and the elbow swung a little from where the key points it
        angle, swivel = min(((math.radians(turn), math.radians(swing)) for turn in range(-180, 180, 10) for swing in range(-60, 61, 15)),
                            key=lambda pair: cost(*pair))
        for _ in range(3):
            low, high = angle - math.radians(10), angle + math.radians(10)
            for _ in range(10):
                third = (high - low) / 3
                if cost(low + third, swivel) < cost(high - third, swivel):
                    high -= third
                else:
                    low += third
            angle = (low + high) / 2
            low, high = swivel - math.radians(15), swivel + math.radians(15)
            for _ in range(10):
                third = (high - low) / 3
                if cost(angle, low + third) < cost(angle, high - third):
                    high -= third
                else:
                    low += third
            swivel = (low + high) / 2
        LAST[side] = (angle, swivel)
    elif spec.get("swing", True):
        # the hand's turn is the key's (a blade's edge must lead); the elbow may still swing from where the key points it,
        # to the place that leaves the wrist straightest
        before = LAST.get(side)

        def bent(swung):
            p = at(0.0, swung)
            near = 0.0 if before is None else abs(swung - before[1])
            return (math.degrees(p["bend"]) + 0.5 * max(0.0, abs(math.degrees(p["turn"])) - 70.0) + 400.0 * p["short"] + 0.4 * abs(math.degrees(swung))
                    + 6.0 * near + 400.0 * crowding(trunk, p))

        swivel = min((math.radians(swing) for swing in range(-40, 41, 10)), key=bent)        # the key chose the elbow's way: only a little from it
        low, high = swivel - math.radians(10), swivel + math.radians(10)
        for _ in range(12):
            third = (high - low) / 3
            if bent(low + third) < bent(high - third):
                high -= third
            else:
                low += third
        swivel = (low + high) / 2
        LAST[side] = (0.0, swivel)
    else:
        LAST.pop(side, None)
    p = at(angle, swivel)
    # a bone's Z looks the way its joint points (the rest's roll: Z to the back, the elbow bends the forearm forward)
    m = frame3(p["elbow"] - shoulder, p["n"]).to_4x4()
    m.translation = shoulder
    upper.matrix = m
    update()
    # the wrist's turn about the forearm, shared with the forearm so the wrist doesn't wring
    m = (p["fore3"] @ Matrix.Rotation(p["turn"] * spec.get("twist", 0.5), 3, "Y")).to_4x4()
    m.translation = p["elbow"]
    fore.matrix = m
    update()
    m = p["wanted"].to_3x3().to_4x4()
    m.translation = hand.matrix.translation
    hand.matrix = m
    update()
    if notes is not None:
        notes.append({"side": side, "bend": math.degrees(p["bend"]), "turn": math.degrees(p["turn"]), "reach": p["reach"],
                      "about": math.degrees(angle), "swing": math.degrees(swivel), "short": p["short"], "trunk": crowding(trunk, p)})

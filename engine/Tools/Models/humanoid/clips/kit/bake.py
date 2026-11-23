# A keying source turned into a clip's data on the keying figure (GM2-P23; Docs/Game/Hero.md, "How it works", "The keying
# kit"): every key posed in the figure's own terms (bone rotations, the hips' move, where each foot stands, where each hand
# grips) and read back as plain bone rotations and the hips' place, the form clips/library.py reads. Also the clip keyed
# on the figure as the library keys it, for what is measured and shown frame by frame: the figure against the ground, and
# each frame's posed figure exported for a picture. Runs inside Blender (run.py).
import json
import math

import bmesh
import bpy
from mathutils import Euler, Matrix, Quaternion, Vector

import solver
from make_models import curves

V = Vector
# The ground is where the boots' soles are at rest. A foot is keyed by its ankle, and poses.on_toes keeps the foot bone's
# end at its height when the heel comes up; the boot's toe cap reaches past that end, so a boot pitched toe-down went under
# the ground (the clip library's worker measured it on 2026-11-18: toes up to 25 mm under, a kneeling foot 69 mm). A foot
# is raised by what its boot would dip under the ground past GROUND_SLACK, so the motion stays as keyed (the man's soles
# and knee rest from 2.2 mm under to 2 mm over: the contact band of GM2-P37 keeps exactly that on every body).
GROUND_SLACK = 0.002
# Each body part's name in the ground's report, by the bone its vertex follows most.
PART = {"upper_arm": "upper-arm", "forearm": "forearm", "hand": "hand", "fingers": "hand", "thumb": "hand", "shoulder": "upper-arm", "thigh": "thigh",
        "shin": "shin", "foot": "foot", "hips": "seat", "spine": "back", "chest": "back", "neck": "head", "head": "head"}


def part_of(groups, vertex):
    """The body part (PART, with its side) of the bone a vertex follows most; `groups` its object's groups by index."""
    if not vertex.groups:
        return "?"
    stem, _, side = groups[max(vertex.groups, key=lambda g: g.weight).group].partition(".")
    return PART.get(stem, stem) + ("." + side if side else "")


class Keyer:
    """The keying figure (figure.keying_figure) and what is worked out on it."""

    def __init__(self, figure):
        self.figure = figure
        self.rig = figure["rig"]
        self.bones = self.rig.pose.bones
        for bone in self.bones:
            bone.rotation_mode = "QUATERNION"
        self.rest = {b.name: b.bone.matrix_local.copy() for b in self.bones}
        self.boots = figure["boots"]
        names = {g.index: g.name for g in self.boots.vertex_groups}
        # a boot's vertex belongs to the side it is weighted to most
        self.boot_side = [names[max(v.groups, key=lambda g: g.weight).group][-1] if v.groups else "" for v in self.boots.data.vertices]
        self.reset()
        self.level = min(self.boots_lowest().values())          # the ground's height: the boots' soles at rest
        # every skinned piece's vertices by the body part of the bone each follows most (the ground's report)
        self.parts = []
        for obj in figure["skinned"]:
            groups = {g.index: g.name for g in obj.vertex_groups}
            self.parts.append((obj, [part_of(groups, v) for v in obj.data.vertices]))

    def update(self):
        bpy.context.view_layer.update()

    def reset(self):
        for bone in self.bones:
            bone.rotation_quaternion = Quaternion()
            bone.location = V((0, 0, 0))
        self.update()

    def solve_leg(self, side, target):
        """Sets the thigh and the shin so the ankle is at `target` (a foot's place, poses.py: from its rest place), the knee
        looking forward; then the foot (its pitch and turn)."""
        left, back, up, pitch, turn = target[:5]
        heading = math.radians(target[5]) if len(target) > 5 else 0.0     # which way the leg faces (0: the figure's front)
        thigh, shin, foot = self.bones[f"thigh.{side}"], self.bones[f"shin.{side}"], self.bones[f"foot.{side}"]
        l1, l2 = thigh.bone.length, shin.bone.length
        self.update()
        hip = thigh.matrix.translation.copy()
        ankle = self.rest[f"foot.{side}"].translation + V((left, back, up))
        reach = ankle - hip
        d = max(abs(l1 - l2) + 1e-4, min(l1 + l2 - 1e-4, reach.length))
        u = reach.normalized()
        forward = V((math.sin(heading + math.radians(turn) * 0.6), -math.cos(heading + math.radians(turn) * 0.6), 0.0)).normalized()
        n = (forward - u * forward.dot(u)).normalized()
        a = (l1 * l1 - l2 * l2 + d * d) / (2 * d)
        knee = hip + u * a + n * math.sqrt(max(0.0, l1 * l1 - a * a))
        foot_at = hip + u * d

        def frame(head, tail):
            y = (tail - head).normalized()
            z = (-n - y * (-n).dot(y)).normalized()
            x = y.cross(z)
            m = Matrix((x, y, z)).transposed().to_4x4()
            m.translation = head
            return m

        thigh.matrix = frame(hip, knee)
        self.update()
        shin.matrix = frame(knee, foot_at)
        self.update()
        rest = self.rest[f"foot.{side}"].to_3x3()
        turned = Matrix.Rotation(heading + math.radians(turn), 3, "Z") @ rest @ Matrix.Rotation(math.radians(pitch), 3, "X")
        m = turned.to_4x4()
        m.translation = foot.matrix.translation
        foot.matrix = m
        self.update()

    def boots_lowest(self):
        """Each boot's lowest point, as heights."""
        self.update()
        evaluated = self.boots.evaluated_get(bpy.context.evaluated_depsgraph_get())
        mesh = evaluated.to_mesh()
        low = {"L": 1e9, "R": 1e9}
        world = evaluated.matrix_world
        for side, vertex in zip(self.boot_side, mesh.vertices):
            if side in low:
                low[side] = min(low[side], (world @ vertex.co).z)
        evaluated.to_mesh_clear()
        return low

    def on_the_ground(self, feet):
        """Raises a foot whose boot is under the ground by more than GROUND_SLACK (its ankle's key, then the leg again)."""
        targets = {side: list(target) for side, target in feet.items()}
        for _ in range(4):
            low = self.boots_lowest()
            again = False
            for side, target in targets.items():
                under = self.level - GROUND_SLACK - low[side]
                if under > 2e-5:
                    target[2] += under
                    self.solve_leg(side, target)
                    again = True
            if not again:
                break

    def pose_key(self, key, notes=None):
        """The figure posed as a source's key says (its arms' notes into `notes`)."""
        bones = self.bones
        self.reset()
        for name, euler in key.get("pose", {}).items():
            bones[name].rotation_quaternion = Euler([math.radians(a) for a in euler], "XYZ").to_quaternion()
        if "hips" in key:
            left, back, up = key["hips"]
            # the hips' own axes: X to the figure's right, Y up, Z back
            bones["hips"].location = V((-left, up, back))
        self.update()
        for side, target in key.get("feet", {}).items():
            self.solve_leg(side, target)
        if key.get("feet"):
            self.on_the_ground(key["feet"])
        # the hands: one keyed by its own place first, then one that holds on to the other's weapon
        for side, spec in sorted(key.get("hands", {}).items(), key=lambda item: "on" in item[1]):
            blend = spec.get("blend", 1.0)
            if blend <= 0.0:
                continue
            if "at_head" in spec:
                # the grip's own place given in the head's frame (a bowstring drawn to the jaw); `follow` 0..1 mixes it with
                # the key's own place
                at = bones["head"].matrix @ V(spec["at_head"])
                spec = dict(spec)
                spec["at"] = list(V(spec["at"]).lerp(at, spec.get("follow", 1.0))) if "at" in spec else list(at)
            if "tip_head" in spec:
                # the weapon's point (a flask's mouth) at a place given in the head's own frame, the weapon's way given there
                # too: the hand follows the head. `follow` 0..1 mixes this with the key's own place and way.
                head = bones["head"].matrix
                follow = spec.get("follow", 1.0)
                aim = (head.to_3x3() @ V(spec["aim_head"])).normalized()
                at = head @ V(spec["tip_head"]) - aim * spec.get("reach", 0.092)
                spec = dict(spec)
                spec["at"] = list(V(spec["at"]).lerp(at, follow)) if "at" in spec else list(at)
                spec["aim"] = list(V(spec["aim"]).normalized().lerp(aim, follow).normalized()) if "aim" in spec else list(aim)
                if "edge_head" in spec:        # the knuckles' way given in the head's frame too (left to the solver, the arm's
                    edge = (head.to_3x3() @ V(spec["edge_head"])).normalized()      # answer jumped from one frame to the next)
                    spec["edge"] = list(V(spec["edge"]).normalized().lerp(edge, follow).normalized()) if "edge" in spec else list(edge)
            names = [f"upper_arm.{side}", f"forearm.{side}", f"hand.{side}"]
            keyed = [bones[n].rotation_quaternion.copy() for n in names]
            solved_notes = [] if notes is not None else None
            solver.solve_arm(bones, self.rest, self.update, side, spec, solved_notes)
            if notes is not None:
                notes.extend(dict(note, frame=key["frame"]) for note in solved_notes)
            if blend < 1.0:        # between the pose's own arm and the solved one (a hand coming to a grip or leaving it)
                solved = [bones[n].matrix_basis.to_quaternion() for n in names]
                for n, a, b in zip(names, keyed, solved):
                    bones[n].rotation_quaternion = a.slerp(b, blend)
                self.update()

    def bake(self, spec, notes=None):
        """The source's keys as plain rotations of every bone a key moved (and the hips' place): the clip's data
        (clips/library.py's form). `notes` (a list) gets every solved arm's measure at every key."""
        # every clip from the same start: a free hand's answer depends on the key before, never on the clip before. (A
        # clip keyed on the figure before, for the ground or the pictures, can stay assigned: Blender plays an action
        # only when the frame changes, and every key is posed by hand.)
        solver.LAST.clear()
        keys = []
        for key in spec["keys"]:
            self.pose_key(key, notes)
            rotations, locations = {}, {}
            for bone in self.bones:
                # a solved bone's rotation is read from its basis; a keyed one's as it was keyed (to the digit)
                solved = bone.name.startswith(("thigh", "shin", "foot")) or (
                    bone.name.startswith(("upper_arm", "forearm", "hand")) and bone.name[-1] in key.get("hands", {}))
                q = bone.matrix_basis.to_quaternion() if solved else bone.rotation_quaternion
                angles = [round(math.degrees(a), 3) for a in q.to_euler("XYZ")]
                if any(abs(a) > 1e-4 for a in angles) or bone.name in key.get("pose", {}):
                    rotations[bone.name] = angles
            if "hips" in key:
                locations["hips"] = [round(c, 5) for c in self.bones["hips"].location]
            keys.append({"frame": key["frame"], "rotations": rotations, "locations": locations})
        # a bone a later key leaves alone still needs its rest keyed where another key moved it
        moved = sorted({name for key in keys for name in key["rotations"]})
        for key in keys:
            for name in moved:
                key["rotations"].setdefault(name, [0.0, 0.0, 0.0])
            if any(k_["locations"] for k_ in keys):
                key["locations"].setdefault("hips", [0.0, 0.0, 0.0])
        if spec.get("only"):
            # a stance loop: only the bones it names (the arms') are in the clip; the battle lays it over whatever else plays
            for key in keys:
                key["rotations"] = {name: value for name, value in key["rotations"].items() if name.startswith(tuple(spec["only"]))}
                key["locations"] = {}
        if spec.get("loop"):
            # a loop closes to the digit: its last key is its first (the arms' solver answers the same grips within half a
            # degree after a round; more than a degree apart is a source that doesn't close, and is refused)
            first, last = keys[0], keys[-1]
            apart = max([abs(p - q) for name in first["rotations"] for p, q in zip(first["rotations"][name], last["rotations"][name])]
                        + [abs(p - q) * 1000 for name in first["locations"] for p, q in zip(first["locations"][name], last["locations"][name])])
            if apart > 1.0 or last["frame"] != spec["frames"]:
                raise ValueError("%s: a loop whose last key is %.3f from its first" % (spec["name"], apart))
            last["rotations"] = {name: list(value) for name, value in first["rotations"].items()}
            last["locations"] = {name: list(value) for name, value in first["locations"].items()}
        return {"name": spec["name"], "frames": spec["frames"], "loop": spec.get("loop", False), "markers": spec.get("markers", {}),
                "ease": spec.get("ease", "bezier"), "keys": keys}

    def key_action(self, clip):
        """Keys the clip on the figure as clips/library.py keys it (quaternions, sign-continuous, Bezier or linear), data
        frame n at Blender's frame n."""
        self.reset()
        action = bpy.data.actions.new(clip["name"])
        self.rig.animation_data_create()
        self.rig.animation_data.action = action
        previous = {}
        for key in clip["keys"]:
            for name, euler in key["rotations"].items():
                q = Euler([math.radians(a) for a in euler], "XYZ").to_quaternion()
                if name in previous and previous[name].dot(q) < 0:
                    q.negate()
                previous[name] = q
                self.bones[name].rotation_quaternion = q
                self.bones[name].keyframe_insert("rotation_quaternion", frame=key["frame"])
            for name, location in key["locations"].items():
                self.bones[name].location = V(location)
                self.bones[name].keyframe_insert("location", frame=key["frame"])
        if clip.get("ease") == "linear":
            for curve in curves(action):
                for point in curve.keyframe_points:
                    point.interpolation = "LINEAR"
        return action

    def ground_parts(self):
        """Every skinned piece's lowest point per body part (by the bone each vertex follows most) at the frame set:
        {part: (height, piece)}."""
        self.update()
        depsgraph = bpy.context.evaluated_depsgraph_get()
        low = {}
        for obj, parts in self.parts:
            evaluated = obj.evaluated_get(depsgraph)
            mesh = evaluated.to_mesh()
            for part, vertex in zip(parts, mesh.vertices):
                z = (evaluated.matrix_world @ vertex.co).z
                if z < low.get(part, (1e9, ""))[0]:
                    low[part] = (z, obj.name.replace(self.rig.name.removesuffix("-rig") + "-", ""))
            evaluated.to_mesh_clear()
        return low

    def ground(self, clip):
        """The clip against the ground frame by frame as Blender plays it (keyed as the library keys it): each frame's
        lowest point over the ground (metres; under it below 0) with its part and piece, and every part's lowest."""
        self.key_action(clip)
        rows = []
        for frame in range(0, clip["frames"] + 1):
            bpy.context.scene.frame_set(frame)
            low = self.ground_parts()
            part, (z, piece) = min(low.items(), key=lambda item: item[1][0])
            rows.append({"frame": frame, "lowest": z - self.level, "part": part, "piece": piece,
                         "parts": {name: z_ - self.level for name, (z_, _) in sorted(low.items())}})
        return rows


def held(clip, hold):
    """The clip with a stance's loop laid over it, as the battle will (GM2-P21; for a picture: the clip's own data is
    left as it is): the stance's bones follow the loop, whose time is the clip's own frame (between the loop's keys the
    arms are mixed)."""
    clip = json.loads(json.dumps(clip))
    frames = [k_["frame"] for k_ in hold["keys"]]
    length = max(1, hold["frames"])

    def at(frame):
        t = frame % length
        after = next((i for i, f in enumerate(frames) if f >= t), len(frames) - 1)
        before = max(0, after - 1)
        span = frames[after] - frames[before]
        s = (t - frames[before]) / span if span else 0.0
        out = {}
        for name, a in hold["keys"][before]["rotations"].items():
            qa = Euler([math.radians(c) for c in a], "XYZ").to_quaternion()
            qb = Euler([math.radians(c) for c in hold["keys"][after]["rotations"][name]], "XYZ").to_quaternion()
            out[name] = [round(math.degrees(c), 3) for c in qa.slerp(qb, s).to_euler("XYZ")]
        return out

    # the loop needs keys of its own where the clip has few (an idle keyed twice would lose the arms' round)
    have = {k_["frame"] for k_ in clip["keys"]}
    if len(clip["keys"]) >= 2 and clip["frames"] > 12:
        dense = []
        for first, second in zip(clip["keys"], clip["keys"][1:]):
            dense.append(first)
            gap = second["frame"] - first["frame"]
            for step in range(6, gap if gap > 12 else 0, 6):
                if gap - step < 3:
                    break
                s = step / gap
                e = s * s * (3 - 2 * s)            # an eased mix, near enough to the curve for a picture
                between = {"frame": first["frame"] + step, "rotations": {}, "locations": {}}
                for name, a in first["rotations"].items():
                    qa = Euler([math.radians(c) for c in a], "XYZ").to_quaternion()
                    qb = Euler([math.radians(c) for c in second["rotations"].get(name, a)], "XYZ").to_quaternion()
                    between["rotations"][name] = [round(math.degrees(c), 3) for c in qa.slerp(qb, e).to_euler("XYZ")]
                for name, a in first["locations"].items():
                    between["locations"][name] = [p + (q - p) * e for p, q in zip(a, second["locations"].get(name, a))]
                if between["frame"] not in have:
                    dense.append(between)
        dense.append(clip["keys"][-1])
        clip["keys"] = dense
    for key in clip["keys"]:
        key["rotations"].update(at(key["frame"]))
    return clip


def track(spec, name, frame):
    """A picture-only number over the clip (the source's `<name>`: [[frame, value], ...], straight between), 0 where the
    source has none."""
    points = spec.get(name) or [[0, 0.0]]
    if frame <= points[0][0]:
        return points[0][1]
    for (f0, v0), (f1, v1) in zip(points, points[1:]):
        if frame <= f1:
            return v0 + (v1 - v0) * (frame - f0) / (f1 - f0)
    return points[-1][1]


def prism(bm, a, b, r):
    """A square bar from a to b, r thick each way."""
    axis = (b - a).normalized()
    side = axis.orthogonal().normalized()
    up = axis.cross(side)
    ends = [[bm.verts.new(c + side * r * sx + up * r * sy) for sx, sy in ((1, 1), (-1, 1), (-1, -1), (1, -1))] for c in (a, b)]
    for i in range(4):
        bm.faces.new((ends[0][i], ends[0][(i + 1) % 4], ends[1][(i + 1) % 4], ends[1][i]))
    bm.faces.new(ends[0][::-1])
    bm.faces.new(ends[1])


def mesh_object(name, bm, materials):
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    for material in materials:
        mesh.materials.append(material)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


class Frames:
    """A clip's frames exported as posed figures for its pictures (video.py renders them): the keying figure "geared" with
    what the source's `gear` (or the caller's) puts in its hands, moved by the source's `travel`, the bow's string drawn
    by its `string`, a floor whose top is the ground."""

    def __init__(self, keyer, spec, gear):
        self.keyer, self.spec = keyer, spec
        self.gear = [entry for entry in gear.split(",") if entry and entry != "none"]
        self.low, self.high = [1e9] * 3, [-1e9] * 3       # the figure's reach over the frames exported (glTF's axes)

    def place_gear(self, made, matrix_of):
        for entry in sorted(self.gear):
            name, _, where = entry.partition("@")
            if name not in self.keyer.figure["gear"]:
                raise ValueError(f"no gear {name} for the pictures (the gear: {', '.join(sorted(self.keyer.figure['gear']))})")
            socket = "socket." + (where or ("shield.L" if name == "shield" else "hand.R"))
            matrix = matrix_of(socket)
            for obj in self.keyer.figure["gear"][name]:
                copy = obj.copy()
                copy.matrix_world = matrix
                bpy.context.scene.collection.objects.link(copy)
                made.append(copy)
            if name == "bow":
                self.drawn_bow(made, matrix)

    def drawn_bow(self, made, matrix):
        """The stand-in bow's string as the game will draw a real bow's (the user, 2026-11-18: "The bow string does not
        move"; GM3): from the limbs' tips to the string hand's grip while the hand holds it (the source's `string` track:
        1 held, 0 let go), with an arrow on it. The bow's own frame: the stave along Y, the arrow's way along Z, the
        string 0.16 m behind the grip."""
        rig, bones = self.keyer.rig, self.keyer.bones
        pull = track(self.spec, "string", self.frame)
        string = next((o for o in made if o.name.startswith("bow-string")), None)
        if string is None:
            return
        rest = V((0.0, 0.0, -0.16))
        hand = matrix.inverted() @ (rig.matrix_world @ bones["socket.hand.R"].matrix.translation)
        nock = rest.lerp(hand, max(0.0, min(1.0, pull)))
        bm = bmesh.new()
        for tip in (V((0.0, -0.62, -0.16)), V((0.0, 0.62, -0.16))):
            prism(bm, tip, nock, 0.003)
        drawn = mesh_object("bow-string-drawn", bm, string.data.materials)
        drawn.matrix_world = matrix
        drawn["ink"] = 0
        made.remove(string)
        bpy.data.objects.remove(string)
        made.append(drawn)
        if pull > 0.5:                       # an arrow on the string, its point past the stave
            stave = next((o for o in made if o.name.startswith("bow-stave")), None)
            bm = bmesh.new()
            way = (V((0.0, 0.02, 0.06)) - nock).normalized()
            prism(bm, nock, nock + way * 0.76, 0.004)
            prism(bm, nock + way * 0.76, nock + way * 0.80, 0.009)
            arrow = mesh_object("arrow", bm, stave.data.materials[:1] if stave is not None else [])
            arrow.matrix_world = matrix
            made.append(arrow)

    def floor(self):
        """A slab whose top is the ground, under the figure and behind it (where a fall lies)."""
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0)
        for vertex in bm.verts:
            vertex.co = V((vertex.co.x * 1.5, 0.45 + vertex.co.y * 3.4, self.keyer.level - 0.02 + vertex.co.z * 0.04))
        return mesh_object("floor", bm, [bpy.data.materials["wood"]])

    def export(self, frame, path):
        """The figure at `frame` (the clip keyed on it, the scene's frame set) as one static model: every skinned piece
        evaluated, the gear at its sockets, the floor; moved back by the source's `travel`."""
        self.frame = frame
        keyer = self.keyer
        moved = Matrix.Translation(V((0.0, track(self.spec, "travel", frame), 0.0)))
        bpy.context.scene.frame_set(frame)
        keyer.update()
        depsgraph = bpy.context.evaluated_depsgraph_get()
        made = []
        for obj in keyer.figure["skinned"]:
            mesh = bpy.data.meshes.new_from_object(obj.evaluated_get(depsgraph), depsgraph=depsgraph)
            copy = bpy.data.objects.new(obj.name + "-posed", mesh)
            copy.matrix_world = moved @ keyer.rig.matrix_world
            for name, value in obj.items():
                copy[name] = value
            bpy.context.scene.collection.objects.link(copy)
            made.append(copy)
        self.place_gear(made, lambda socket: moved @ keyer.rig.matrix_world @ keyer.bones[socket].matrix)
        # how far the figure reaches over the clip (glTF's axes: x, z, -y), so every frame is framed alike
        for obj in made:
            for vertex in obj.data.vertices:
                p = obj.matrix_world @ vertex.co
                for axis, value in enumerate((p.x, p.z, -p.y)):
                    self.low[axis], self.high[axis] = min(self.low[axis], value), max(self.high[axis], value)
        made.append(self.floor())
        for obj in bpy.context.view_layer.objects:
            obj.select_set(False)
        for obj in made:
            obj.select_set(True)
        bpy.ops.export_scene.gltf(filepath=path, export_format="GLB", export_yup=True, export_apply=True, export_extras=True, export_animations=False,
                                  export_skins=False, use_selection=True)
        for obj in made:
            data = obj.data
            bpy.data.objects.remove(obj)
            if data is not None and data.users == 0:
                bpy.data.meshes.remove(data)

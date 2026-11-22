# A clip keeps its contacts with the ground on a body other than the one it was keyed on (GM2-P37, "contacts kept";
# Docs/Animation.md, "A clip keeps its contacts"; Docs/Game/Hero.md, "How it works", "Contacts kept"): Blender's side of
# the engine's rule (Engine/Statventure/Animation/Contacts.cs), the same arithmetic from the same data. A clip whose action
# names "contacts" (its custom property, which the family's clip library sets and the exporter writes as the animation's
# extras: {"keyedOn": "<model>", "root": "<joint>", "chains": [["top", "middle", "end"], ...]}) has, at every frame, its
# root's location and its chains' joints' turns made again for the playing body: each group's lowest point (the body's and
# its worn garments' vertices skinned from their .glb files, as the engine skins them) put where the keyed body's is, the
# keyed clearance times the measure's ratio, as far as the keyed group is near the ground. The corrected values are keyed
# into the action at every frame (linear), so Blender then evaluates the corrected clip, as the engine plays its corrected
# channels straight between key times. Nothing is touched on the body the clip was keyed on (the same numbers move
# nothing). Used wherever Blender evaluates or renders a clip on a body: evaluate_animations.py (the clips' references) and
# Tools/Models/humanoid/render.py's pose_at (so assemble.py's).
#
# Pure numbers over the action's F-curves and the bodies' .glb files: a joint's pose channels are its rest-relative
# location and quaternion (Blender's pose basis, glTF's node over its rest, as the exporter writes them), posed through the
# .glb's hierarchy as the engine's PoseBuffer.ToPose and ModelData.WorldMatrices pose them (column vectors here).
import json
import math
import os
import struct

import numpy as np

FADE = 0.150      # ContactRule.Fade
BAND = 0.002      # ContactRule.Band
ROUNDS = 4        # ContactRule.Rounds
SETTLED = 1e-6    # ContactRule.Settled


def weight(keyed):
    """ContactRule.Weight: 1 at a keyed clearance of 0 or under, 1 - smoothstep(keyed / FADE) above, 0 from FADE on."""
    if keyed <= 0:
        return 1.0
    x = keyed / FADE
    if x >= 1:
        return 0.0
    return 1 - x * x * (3 - 2 * x)


def target(keyed, ratio):
    """ContactRule.Target: in the band as keyed, above it the band plus the ratio times the height over it (written so that
    the ratio 1 gives the keyed clearance exactly)."""
    return keyed + (ratio - 1) * max(0.0, keyed - BAND)


# --- quaternions (x, y, z, w), Hamilton products, as the engine's ---

def q_mul(a, b):
    ax, ay, az, aw = a
    bx, by, bz, bw = b
    return (aw * bx + ax * bw + ay * bz - az * by,
            aw * by - ax * bz + ay * bw + az * bx,
            aw * bz + ax * by - ay * bx + az * bw,
            aw * bw - ax * bx - ay * by - az * bz)


def q_conj(q):
    return (-q[0], -q[1], -q[2], q[3])


def q_norm(q):
    length = math.sqrt(sum(c * c for c in q))
    return tuple(c / length for c in q) if length > 1e-35 else (0.0, 0.0, 0.0, 1.0)


def q_rotate(q, v):
    u = np.array(q[:3])
    t = 2 * np.cross(u, v)
    return v + q[3] * t + np.cross(u, t)


def q_matrix(q):
    x, y, z, w = q
    return np.array([[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
                     [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
                     [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]])


def q_of_matrix(m):
    """A turn's quaternion from a rotation matrix (its columns normalised: a scale taken out)."""
    m = m / np.linalg.norm(m, axis=0)
    trace = m[0, 0] + m[1, 1] + m[2, 2]
    if trace > 0:
        s = 2 * math.sqrt(trace + 1)
        q = ((m[2, 1] - m[1, 2]) / s, (m[0, 2] - m[2, 0]) / s, (m[1, 0] - m[0, 1]) / s, s / 4)
    elif m[0, 0] > m[1, 1] and m[0, 0] > m[2, 2]:
        s = 2 * math.sqrt(1 + m[0, 0] - m[1, 1] - m[2, 2])
        q = (s / 4, (m[0, 1] + m[1, 0]) / s, (m[0, 2] + m[2, 0]) / s, (m[2, 1] - m[1, 2]) / s)
    elif m[1, 1] > m[2, 2]:
        s = 2 * math.sqrt(1 + m[1, 1] - m[0, 0] - m[2, 2])
        q = ((m[0, 1] + m[1, 0]) / s, s / 4, (m[1, 2] + m[2, 1]) / s, (m[0, 2] - m[2, 0]) / s)
    else:
        s = 2 * math.sqrt(1 + m[2, 2] - m[0, 0] - m[1, 1])
        q = ((m[0, 2] + m[2, 0]) / s, (m[1, 2] + m[2, 1]) / s, s / 4, (m[1, 0] - m[0, 1]) / s)
    return q_norm(q)


def q_axis_angle(axis, angle):
    s, c = math.sin(angle / 2), math.cos(angle / 2)
    return (axis[0] * s, axis[1] * s, axis[2] * s, c)


def q_between(a, b):
    """The smallest turn taking direction a to b."""
    a, b = a / np.linalg.norm(a), b / np.linalg.norm(b)
    c = np.cross(a, b)
    return q_norm((c[0], c[1], c[2], 1 + float(np.dot(a, b))))


# --- .glb files ---

COMPONENTS = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4, "MAT4": 16}
TYPES = {5120: np.int8, 5121: np.uint8, 5122: np.int16, 5123: np.uint16, 5125: np.uint32, 5126: np.float32}


class Glb:
    """A .glb's JSON and its accessors as arrays."""

    def __init__(self, path):
        data = open(path, "rb").read()
        length = struct.unpack_from("<I", data, 12)[0]
        self.json = json.loads(data[20:20 + length])
        offset = 20 + length
        self.bin = data[offset + 8:offset + 8 + struct.unpack_from("<I", data, offset)[0]] if offset < len(data) else b""

    def accessor(self, index):
        a = self.json["accessors"][index]
        view = self.json["bufferViews"][a["bufferView"]]
        dtype = np.dtype(TYPES[a["componentType"]])
        n, count = COMPONENTS[a["type"]], a["count"]
        start = view.get("byteOffset", 0) + a.get("byteOffset", 0)
        stride = view.get("byteStride", n * dtype.itemsize)
        rows = np.ndarray((count, n), dtype=dtype, buffer=self.bin, offset=start, strides=(stride, dtype.itemsize))
        rows = rows.astype(np.float64)
        if a.get("normalized"):
            rows /= np.iinfo(dtype).max
        return rows


class Model:
    """A .glb model's nodes (names, hierarchy, rest translation, rotation, scale) and its skinned meshes."""

    def __init__(self, path):
        self.path = path
        glb = Glb(path)
        g = glb.json
        nodes = g.get("nodes", [])
        self.names = [node.get("name", str(i)) for i, node in enumerate(nodes)]
        self.children = [list(node.get("children", [])) for node in nodes]
        self.parent = [-1] * len(nodes)
        for i, kids in enumerate(self.children):
            for c in kids:
                self.parent[c] = i
        self.t = [np.array(node.get("translation", (0.0, 0.0, 0.0)), dtype=np.float64) for node in nodes]
        self.q = [tuple(float(c) for c in node.get("rotation", (0.0, 0.0, 0.0, 1.0))) for node in nodes]
        self.s = [np.array(node.get("scale", (1.0, 1.0, 1.0)), dtype=np.float64) for node in nodes]
        self.roots = g["scenes"][g.get("scene", 0)]["nodes"] if g.get("scenes") else [i for i, p in enumerate(self.parent) if p < 0]
        joints = {j for skin in g.get("skins", []) for j in skin["joints"]}
        self.by_name = {}
        for first in (True, False):  # a joint before any other node of its name (the engine's Skeleton)
            for i, name in enumerate(self.names):
                if (i in joints) == first:
                    self.by_name.setdefault(name, i)
        self.skins = []
        for skin in g.get("skins", []):
            ibm = glb.accessor(skin["inverseBindMatrices"]).reshape(-1, 4, 4).transpose(0, 2, 1)  # column-major in the file
            self.skins.append((list(skin["joints"]), ibm))
        self.meshes = []
        for i, node in enumerate(nodes):
            if "mesh" not in node or "skin" not in node:
                continue
            for p in g["meshes"][node["mesh"]]["primitives"]:
                attrs = p["attributes"]
                if "JOINTS_0" not in attrs or "WEIGHTS_0" not in attrs:
                    continue
                self.meshes.append((node["skin"], glb.accessor(attrs["POSITION"]), glb.accessor(attrs["JOINTS_0"]).astype(np.int64),
                                    glb.accessor(attrs["WEIGHTS_0"]).astype(np.float32).astype(np.float64)))
        self.rest = self.worlds({})

    def find(self, name):
        return self.by_name.get(name, -1)

    def local(self, i, basis):
        """A node's local matrix (column vectors) with its pose channels (rest-relative location, quaternion x y z w) or its rest."""
        if i not in basis:
            t, q, s = self.t[i], self.q[i], self.s[i]
        else:
            tb, qb = basis[i]
            rest = q_norm(self.q[i])
            t = self.t[i] + q_rotate(rest, self.s[i] * tb)
            q = q_mul(rest, q_norm(qb))
            s = self.s[i]
        m = np.eye(4)
        m[:3, :3] = q_matrix(q_norm(q)) * s
        m[:3, 3] = t
        return m

    def worlds(self, basis):
        """Every node's world matrix (column vectors) in a pose: {node: (location, quaternion)} rest-relative."""
        world = [None] * len(self.names)
        stack = [(r, np.eye(4)) for r in self.roots]
        while stack:
            i, parent = stack.pop()
            world[i] = parent @ self.local(i, basis)
            stack += [(c, world[i]) for c in self.children[i]]
        return world

    def lowest_at_rest(self):
        """ModelData.LowestAtRest: the lowest skinned vertex at rest (glTF's Y), or None."""
        lowest = None
        for skin, positions, joints, weights in self.meshes:
            nodes, ibm = self.skins[skin]
            mats = np.stack([self.rest[n] @ ibm[k] for k, n in enumerate(nodes)])
            rows = mats[:, 1, :]
            ph = np.hstack([positions, np.ones((len(positions), 1))])
            y = sum(weights[:, k] * np.einsum("ij,ij->i", ph, rows[joints[:, k]]) for k in range(4))
            low = float(y.min())
            lowest = low if lowest is None or low < lowest else lowest
        return lowest


def ground_of(body, worn):
    """Actor.GroundOf: the lowest point at rest of the body and its garments, to the micrometre (0 without a mesh)."""
    lows = [y for y in [body.lowest_at_rest()] + [g.lowest_at_rest() for g in worn] if y is not None]
    return round(min(lows) * 1e6) / 1e6 if lows else 0.0


class Shape:
    """The engine's ContactShape: a body and its worn garments' vertices in groups (0 the rest; per chain c 1 + 2c its end
    and the bones it carries, 2 + 2c its top and middle), by the bone each follows most (the first of equal weights)."""

    def __init__(self, body, worn, root, chains):
        self.body = body
        self.root = body.find(root)
        self.chains = [[body.find(j) for j in chain] for chain in chains]
        if self.root < 0 or any(j < 0 for chain in self.chains for j in chain):
            raise ValueError(f"{body.path} lacks a joint of the contacts")
        group_of = [0] * len(body.names)
        for c, (top, middle, end) in enumerate(self.chains):
            below = [end]
            while below:
                n = below.pop()
                group_of[n] = 1 + 2 * c
                below += body.children[n]
            group_of[top] = group_of[middle] = 2 + 2 * c
        self.groups = 1 + 2 * len(self.chains)
        befores, nodes, parts = [], [], []
        for skin, positions, joints, weights in body.meshes:
            skin_nodes, ibm = body.skins[skin]
            first = len(nodes)
            for k, n in enumerate(skin_nodes):
                befores.append(ibm[k])
                nodes.append(n)
            parts.append((positions, joints + first, weights))
        for garment in worn:
            for skin, positions, joints, weights in garment.meshes:
                skin_nodes, ibm = garment.skins[skin]
                first = len(nodes)
                for k, gj in enumerate(skin_nodes):
                    wearer = body.find(garment.names[gj])
                    before = garment.rest[gj] @ ibm[k]
                    if wearer >= 0:
                        before = np.linalg.inv(body.rest[wearer]) @ before
                    befores.append(before)
                    nodes.append(wearer)
                parts.append((positions, joints + first, weights))
        self.befores = np.stack(befores)
        self.nodes = np.array(nodes)
        self.positions = np.vstack([np.hstack([p, np.ones((len(p), 1))]) for p, _, _ in parts])
        self.slots = np.vstack([j for _, j, _ in parts])
        self.weights = np.vstack([w for _, _, w in parts])
        most = np.argmax(self.weights, axis=1)  # the first of equal weights
        bones = self.nodes[self.slots[np.arange(len(most)), most]]
        self.group = np.array([group_of[b] if b >= 0 else 0 for b in bones])
        self.ground = ground_of(body, worn)

    def clearances(self, world):
        """Each group's lowest vertex over the ground (inf for an empty group)."""
        mats = np.stack([(world[n] if n >= 0 else np.eye(4)) @ self.befores[k] for k, n in enumerate(self.nodes)])
        rows = mats[:, 1, :]
        y = sum(self.weights[:, k] * np.einsum("ij,ij->i", self.positions, rows[self.slots[:, k]]) for k in range(4))
        result = []
        for g in range(self.groups):
            ys = y[self.group == g]
            result.append(float(ys.min()) - self.ground if len(ys) else math.inf)
        return result


# --- the action's channels ---

def curves_of(action):
    """The action's F-curves by (data path, index), and its channel bag (Blender 5's layered actions)."""
    for layer in action.layers:
        for strip in layer.strips:
            for slot in action.slots:
                bag = strip.channelbag(slot)
                if bag is not None:
                    return {(c.data_path, c.array_index): c for c in bag.fcurves}, bag
    return {}, None


def basis_at(curves, body, frame):
    """Every animated joint's pose channels at a frame: {node: (location, quaternion x y z w)}."""
    basis = {}
    for i, name in enumerate(body.names):
        loc = [curves.get((f'pose.bones["{name}"].location', k)) for k in range(3)]
        rot = [curves.get((f'pose.bones["{name}"].rotation_quaternion', k)) for k in range(4)]
        if any(loc) or any(rot):
            t = np.array([c.evaluate(frame) if c else 0.0 for c in loc])
            w, x, y, z = (rot[k].evaluate(frame) if rot[k] else (1.0 if k == 0 else 0.0) for k in range(4))
            basis[i] = (t, (x, y, z, w))
    return basis


def contacts_of(action):
    """The action's contacts {"keyedOn", "root", "chains"}, or None."""
    kept = action.get("contacts") if hasattr(action, "get") else None
    if kept is None:
        return None
    kept = kept.to_dict() if hasattr(kept, "to_dict") else dict(kept)
    return {"keyedOn": str(kept["keyedOn"]), "root": str(kept["root"]), "chains": [[str(j) for j in c] for c in kept["chains"]]}


def model_file(models, path):
    """A model's data path (models/<path>) as its .glb in a models folder (Assets/Models)."""
    if not path.startswith("models/"):
        raise ValueError(f"{path}: not a model's data path")
    return os.path.join(models, path[len("models/"):] + ".glb")


def frames_of(action):
    first, last = (int(round(f)) for f in action.frame_range)
    return first, list(range(0, last - first + 1))


def keyed_clearances(action, keyed_glb, kept=None):
    """The keyed body's clearances at every frame of the action (its contacts' groups: `kept`, the action's own by
    default), the action as keyed (unscaled)."""
    kept = kept or contacts_of(action)
    body = Model(keyed_glb)
    shape = Shape(body, [], kept["root"], kept["chains"])
    curves, _ = curves_of(action)
    first, frames = frames_of(action)
    return [shape.clearances(body.worlds(basis_at(curves, body, first + f))) for f in frames]


class KeySolve:
    """The engine's KeySolve: the rule at one key, on the pose channels `basis` (changed in place)."""

    def __init__(self, shape, basis, keyed, ratio):
        self.shape, self.body, self.basis, self.keyed, self.ratio = shape, shape.body, basis, keyed, ratio
        self.clamped = [False] * len(shape.chains)
        self.hips = 0.0
        self.moved = set()
        self.conflicted = False

    def usable(self, g):
        return not math.isinf(self.goal[g])

    def desired(self, end, hips):
        return self.goal[end] + (1 - self.weight[end]) * hips

    def run(self):
        s = self.shape
        self.world = self.body.worlds(self.basis)
        before = s.clearances(self.world)
        self.goal, self.weight = [], []
        for g in range(s.groups):
            if math.isinf(before[g]) or math.isinf(self.keyed[g]):
                self.goal.append(math.inf)
                self.weight.append(0.0)
                continue
            w = weight(self.keyed[g])
            self.weight.append(w)
            self.goal.append(before[g] + w * (target(self.keyed[g], self.ratio) - before[g]))
        # the rest floating over its goal comes down to it; under its goal it is only lifted out of the ground
        if not self.usable(0):
            first = 0.0
        elif before[0] > self.goal[0]:
            first = self.goal[0] - before[0]
        else:
            first = max(0.0, min(self.goal[0] - before[0], -before[0]))
        self.move_hips(first)
        for c, chain in enumerate(s.chains):
            end = 1 + 2 * c
            if not self.usable(end):
                continue
            lack = self.desired(end, self.hips) - (before[end] + first)
            if abs(lack) >= SETTLED:
                self.clamped[c] = self.solve(c, self.world[chain[2]][:3, 3].copy(), lack)
        # the rounds: an upper part under the ground and its goal asks the hips up; a clamped end over where it should be
        # asks them down, as far as nothing (the rest, an upper part) goes under the ground or its goal: up wins
        for _ in range(ROUNDS):
            now = s.clearances(self.world)
            up, down = 0.0, 0.0
            room = now[0] - min(self.goal[0], 0.0) if self.usable(0) else math.inf
            for c in range(len(s.chains)):
                end, upper = 1 + 2 * c, 2 + 2 * c
                if self.usable(upper):
                    floor = min(self.goal[upper], 0.0)
                    up = max(up, floor - now[upper])
                    room = min(room, now[upper] - floor)
                if self.usable(end) and self.clamped[c]:
                    down = min(down, self.desired(end, self.hips) - now[end])
            move = up if up > 0 else max(down, -max(0.0, room))
            if down < 0 and move > down:
                self.conflicted = True
            if abs(move) < SETTLED:
                move = 0.0
            lacks, places = [], []
            settled = move == 0
            for c, chain in enumerate(s.chains):
                end = 1 + 2 * c
                places.append(self.world[chain[2]][:3, 3].copy())
                lacks.append(self.desired(end, self.hips + move) - now[end] if self.usable(end) else move)
                if abs(lacks[c] - move) >= SETTLED:
                    settled = False
            if settled:
                break
            self.move_hips(move)
            for c in range(len(s.chains)):
                if abs(lacks[c] - move) >= SETTLED and self.usable(1 + 2 * c):
                    self.clamped[c] = self.solve(c, places[c], lacks[c])

    def channel(self, node):
        return self.basis.get(node, (np.zeros(3), (0.0, 0.0, 0.0, 1.0)))

    def move_hips(self, up):
        if up == 0:
            return
        root = self.shape.root
        b = self.body
        parent = self.world[b.parent[root]] if b.parent[root] >= 0 else np.eye(4)
        d = np.linalg.inv(parent)[:3, :3] @ np.array([0.0, up, 0.0])
        local = q_rotate(q_conj(q_norm(b.q[root])), d) / b.s[root]
        t, q = self.channel(root)
        self.basis[root] = (t + local, q)
        self.hips += up
        self.moved.add(root)
        self.world = b.worlds(self.basis)

    def solve(self, c, origin, up):
        top, middle, end = self.shape.chains[c]
        w = self.world
        target_point = origin + np.array([0.0, up, 0.0])
        h, k, a = w[top][:3, 3], w[middle][:3, 3], w[end][:3, 3]
        x = w[middle][:3, 0] / np.linalg.norm(w[middle][:3, 0])
        p, kh = a - k, k - h
        alpha, beta = float(np.dot(kh, p)), float(np.dot(kh, np.cross(x, p)))
        reach = math.sqrt(alpha * alpha + beta * beta)
        if reach < 1e-12:
            return True
        to_target = target_point - h
        cosine = (float(np.dot(to_target, to_target)) - float(np.dot(kh, kh)) - float(np.dot(p, p))) / 2 / reach
        clamped = False
        if cosine > 1:
            cosine, clamped = 1.0, True
        elif cosine < -1:
            cosine, clamped = -1.0, True
        straight = math.atan2(beta, alpha)
        angle = straight + (1 if -straight >= 0 else -1) * math.acos(cosine)
        bend = q_axis_angle(x, angle)
        a2 = k + q_rotate(bend, p)
        swing = q_between(a2 - h, to_target)
        parent_turn = q_of_matrix(w[self.body.parent[top]][:3, :3])
        top_turn, middle_turn, end_turn = (q_of_matrix(w[n][:3, :3]) for n in (top, middle, end))
        top_new = q_mul(swing, top_turn)
        middle_new = q_mul(q_mul(swing, bend), middle_turn)
        self.set_turn(top, parent_turn, top_new)
        self.set_turn(middle, top_new, middle_new)
        self.set_turn(end, middle_new, end_turn)
        self.world = self.body.worlds(self.basis)
        return clamped

    def set_turn(self, node, parent_world, world):
        rest = q_norm(self.body.q[node])
        basis = q_norm(q_mul(q_conj(rest), q_mul(q_conj(parent_world), world)))
        t, _ = self.channel(node)
        self.basis[node] = (t, basis)
        self.moved.add(node)


def keep(action, keyed, playing_glb, worn_glbs, ratio, kept=None):
    """Keeps the action's contacts on the playing body (its .glb and its worn garments' .glb files): the corrected root
    location and chain turns keyed into the action at every frame (linear), the action already in the body's measure
    (clip_measure.scale_locations); `keyed` the keyed body's clearances (keyed_clearances, taken before the scaling).
    Returns {"changed", "clamped", "conflicts", "frames"}; nothing is touched when nothing moves. `kept`: the contacts, the
    action's own by default (an imported action's are in its .glb's extras)."""
    kept = kept or contacts_of(action)
    body = Model(playing_glb)
    shape = Shape(body, [Model(g) for g in worn_glbs], kept["root"], kept["chains"])
    curves, bag = curves_of(action)
    first, frames = frames_of(action)
    nodes = [shape.root] + [j for chain in shape.chains for j in chain]
    results, moved = [], set()
    stats = {"changed": 0, "clamped": 0, "conflicts": 0, "frames": len(frames)}
    for f in frames:
        basis = basis_at(curves, body, first + f)
        solve = KeySolve(shape, basis, keyed[f], ratio)
        solve.run()
        stats["changed"] += 1 if solve.moved else 0
        stats["clamped"] += 1 if any(solve.clamped) else 0
        stats["conflicts"] += 1 if solve.conflicted else 0
        moved |= solve.moved
        results.append({n: solve.channel(n) for n in nodes})
    for n in sorted(moved):
        name = body.names[n]
        if n == shape.root:
            series = [[r[n][0][k] for r in results] for k in range(3)]
            path = f'pose.bones["{name}"].location'
        else:
            quats, last = [], None
            for r in results:
                x, y, z, w = r[n][1]
                q = (w, x, y, z)
                if last is not None and sum(a * b for a, b in zip(last, q)) < 0:
                    q = tuple(-v for v in q)  # the sign nearer the frame before (Blender blends the components)
                quats.append(q)
                last = q
            series = [[q[k] for q in quats] for k in range(4)]
            path = f'pose.bones["{name}"].rotation_quaternion'
        for k, values in enumerate(series):
            curve = curves.get((path, k)) or bag.fcurves.new(path, index=k)
            curve.keyframe_points.clear()
            curve.keyframe_points.add(len(values))
            for point, f, v in zip(curve.keyframe_points, frames, values):
                point.co = (first + f, v)
                point.interpolation = "LINEAR"
            curve.update()
    return stats

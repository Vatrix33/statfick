# The humanoid skeleton and its skinning (GM2-P8; Docs/Game/Hero.md, "The humanoid rig family"; step 4). One bone set
# for every humanoid body, so every clip plays on every body (clips are shared by bone names, Docs/Animation.md): the
# stand-in's 17 deforming bones (hips, spine, chest, neck, head; upper_arm, forearm, hand, thigh, shin, foot per side),
# a collar bone per side (shoulder.L/R, so a shrug or a reach lifts the shoulder), the mitten hand's block and thumb
# (fingers.L/R, thumb.L/R; Q2: no finger bones, no face bones), and the non-deforming sockets (an item's grip at the
# socket's head, its axes the socket's: Animation/Gear.cs). Each body places them from its own joints (forms.body_forms),
# so a body's .glb carries the skeleton in that body's rest. Blender's coordinates: Z up, the figure faces −Y, its left
# is +X.
#
# Bone rolls, one rule: each bone's Z as near the figure's back (+Y) as it can be, so a limb bends about its own X; a bone
# that itself lies front to back (a foot) takes Z up; a socket says its own (an item's axes: the sword's blade along the
# socket's Y, its edge along Z; the shield's face along Z).
import bmesh
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.interpolate import poly_3d_calc

V = Vector
BACK = V((0, 1, 0))
UP = V((0, 0, 1))

# The deforming bones, in the order the skeleton makes them (every body has every one).
BONES = ["hips", "spine", "chest", "neck", "head"] + [
    f"{bone}.{side}" for side in ("L", "R")
    for bone in ("shoulder", "upper_arm", "forearm", "hand", "fingers", "thumb", "thigh", "shin", "foot")]

# The sockets M7 names (Docs/Animation.md, "Gear in detail") and the shield's, on the left forearm's outer side (Q9: where
# its approval put it).
SOCKETS = ["socket.hand.L", "socket.hip.L", "socket.hand.R", "socket.hip.R", "socket.shield.L", "socket.back", "socket.head"]


def bone_table(p, j):
    """Every bone as (name, head, tail, parent, deforms[, roll: the direction its Z turns toward]), placed from a body's
    parameters `p` and joints `j`."""
    head_centre = V(p["head"]["centre"])
    sz = p["head"]["size"][2]
    hand = p["hand"]
    rows = [("hips", j["pelvis"], j["belly"], None, True),
            ("spine", j["belly"], j["ribs"], "hips", True),
            ("chest", j["ribs"], j["neck0"], "spine", True),
            ("neck", j["neck0"], j["neck1"], "chest", True),
            ("head", j["neck1"], head_centre + V((0, 0, sz)), "neck", True)]
    for tag in ("L", "R"):
        s = 1 if tag == "L" else -1
        sh, el, wr = j["shoulder" + tag], j["elbow" + tag], j["wrist" + tag]
        along = (wr - el).normalized()
        front = (V((0, -1, 0)) - along * V((0, -1, 0)).dot(along)).normalized()
        flat = along.cross(front).normalized() * s         # the palm's normal, toward the body
        mid = wr + along * (hand["length"] * 0.42)
        tip = wr + along * hand["length"]
        root = wr + along * (0.16 * hand["length"]) + front * (hand["width"] * 0.55)
        rows += [(f"shoulder.{tag}", V((s * 0.030, sh.y, sh.z + 0.030)), sh, "chest", True),
                 (f"upper_arm.{tag}", sh, el, f"shoulder.{tag}", True),
                 (f"forearm.{tag}", el, wr, f"upper_arm.{tag}", True),
                 (f"hand.{tag}", wr, mid, f"forearm.{tag}", True),
                 (f"fingers.{tag}", mid, tip, f"hand.{tag}", True),
                 (f"thumb.{tag}", root, root + front * (hand["width"] * 0.75) + along * (0.36 * hand["length"]), f"hand.{tag}", True),
                 (f"thigh.{tag}", j["hip" + tag], j["knee" + tag], "hips", True),
                 (f"shin.{tag}", j["knee" + tag], j["ankle" + tag], f"thigh.{tag}", True),
                 (f"foot.{tag}", j["ankle" + tag], V((s * p["foot"][2][1], p["foot"][2][2], p["foot"][2][3] * 0.6)), f"shin.{tag}", True),
                 # the grip, in the closed mitten: an item's Y (a blade) leaves the fist on the thumb's side, its Z (the
                 # edge) runs along the forearm
                 (f"socket.hand.{tag}", wr + along * (hand["length"] * 0.36) + flat * (hand["thick"] * 0.9),
                  wr + along * (hand["length"] * 0.36) + flat * (hand["thick"] * 0.9) + front * 0.05, f"hand.{tag}", False, along),
                 (f"socket.hip.{tag}", j["pelvis"] + V((s * 0.165, 0.0, 0.03)), j["pelvis"] + V((s * 0.165, 0.0, -0.02)), "hips", False)]
    fore = (j["wristL"] - j["elbowL"]).normalized()
    fore_front = (V((0, -1, 0)) - fore * V((0, -1, 0)).dot(fore)).normalized()
    outer = -fore.cross(fore_front).normalized()       # the forearm's outer side, away from the palm
    grip = j["elbowL"].lerp(j["wristL"], 0.50) + outer * 0.050
    # the shield: its Y runs along the forearm, its Z (the face) looks out from the arm's outer side
    rows += [("socket.shield.L", grip, grip + fore * 0.05, "forearm.L", False, outer),
             ("socket.back", j["chest"] + V((0, 0.125, 0.05)), j["chest"] + V((0, 0.125, 0.10)), "chest", False),
             ("socket.head", head_centre + V((0, 0, sz)), head_centre + V((0, 0, sz + 0.05)), "head", False)]
    assert [r[0] for r in rows if r[4]] == BONES and [r[0] for r in rows if not r[4]] == SOCKETS, "the skeleton's bones aren't BONES and SOCKETS"
    return rows


def build_rig(p, joints):
    """The skeleton of the body `p` (its armature object, named after the body), in that body's rest."""
    armature = bpy.data.armatures.new(p["name"] + "-rig")
    rig = bpy.data.objects.new(p["name"] + "-rig", armature)
    bpy.context.scene.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    for name, head, tail, parent, deforms, *roll in bone_table(p, joints):
        bone = armature.edit_bones.new(name)
        bone.head, bone.tail = head, tail
        bone.use_deform = deforms
        if parent:
            bone.parent = armature.edit_bones[parent]
            bone.use_connect = False
        if roll:
            bone.align_roll(roll[0])
        elif abs((bone.tail - bone.head).normalized().y) > 0.9:
            bone.align_roll(UP)
        else:
            bone.align_roll(BACK)
    bpy.ops.object.mode_set(mode="OBJECT")
    return rig


def bind(rig, body, limit=4):
    """The body bound to the rig by Blender's automatic weights (bone heat, which weights every vertex), smoothed, each
    vertex kept to its `limit` strongest bones (the engine skins with four, M6) and its weights summing to one."""
    for obj in bpy.context.view_layer.objects:
        obj.select_set(False)
    body.select_set(True)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    smooth_weights(body, 3)
    groups = body.vertex_groups
    for v in body.data.vertices:
        weights = sorted(((g.weight, g.group) for g in v.groups), reverse=True)
        keep = weights[:limit]
        total = sum(w for w, _ in keep)
        for w, index in weights[limit:]:
            groups[index].remove([v.index])
        for w, index in keep:
            groups[index].add([v.index], w / total, "REPLACE")


def smooth_weights(body, iterations):
    """Each vertex's weights averaged with its neighbours', a few times: softer bends at the shoulders, hips, elbows and
    knees (fewer folds for the ink to draw)."""
    mesh = body.data
    count = len(mesh.vertices)
    neighbours = [[] for _ in range(count)]
    for edge in mesh.edges:
        a, b = edge.vertices
        neighbours[a].append(b)
        neighbours[b].append(a)
    weights = [{g.group: g.weight for g in v.groups if g.weight > 0} for v in mesh.vertices]
    for _ in range(iterations):
        result = []
        for index in range(count):
            mixed = {group: w * 0.5 for group, w in weights[index].items()}
            share = 0.5 / max(1, len(neighbours[index]))
            for other in neighbours[index]:
                for group, w in weights[other].items():
                    mixed[group] = mixed.get(group, 0.0) + w * share
            result.append(mixed)
        weights = result
    groups = body.vertex_groups
    for index, mixed in enumerate(weights):
        for group, weight in mixed.items():
            if weight > 0.002:
                groups[group].add([index], weight, "REPLACE")


def bind_rigid(rig, obj, bone):
    """A piece that moves with one bone as a whole (the head and its features, hair, a buckle)."""
    group = obj.vertex_groups.new(name=bone)
    group.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
    modifier = obj.modifiers.new("armature", "ARMATURE")
    modifier.object = rig
    obj.parent = rig


def bind_like(rig, obj, body, limit=4):
    """A piece cut from the body (a garment): each vertex takes the body's weights at the nearest point of the body's
    surface, mixed from that face's corners (the `limit` strongest kept, summing to one), so it bends as the skin under
    it. (The nearest vertex's weights did before the diet; on the lighter meshes' large faces a shell then bent a little
    differently from the skin under it, and the tunic showed through the mail in a twist or a deep lean: the manager,
    2026-11-16.)"""
    bm = bmesh.new()
    bm.from_mesh(body.data)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    bm.faces.ensure_lookup_table()
    layer = bm.verts.layers.deform.verify()
    tree = BVHTree.FromBMesh(bm)
    names = {g.index: g.name for g in body.vertex_groups}
    made = {}
    for v in obj.data.vertices:
        point, _, index, _ = tree.find_nearest(v.co)
        corners = bm.faces[index].verts
        shares = poly_3d_calc([c.co for c in corners], point)
        mixed = {}
        for corner, share in zip(corners, shares):
            for group, weight in corner[layer].items():
                mixed[group] = mixed.get(group, 0.0) + weight * share
        kept = sorted(mixed.items(), key=lambda item: -item[1])[:limit]
        total = sum(weight for _, weight in kept)
        for group, weight in kept:
            if weight <= 0 or total <= 0:
                continue
            name = names[group]
            if name not in made:
                made[name] = obj.vertex_groups.new(name=name)
            made[name].add([v.index], weight / total, "REPLACE")
    bm.free()
    modifier = obj.modifiers.new("armature", "ARMATURE")
    modifier.object = rig
    obj.parent = rig

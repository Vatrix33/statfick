# Evaluates the test models' actions in Blender at the times the tests ask and writes every bone's
# pose (Docs/Animation.md, "Method"): Tests/Statventure.Tests/Golden/anim-<scene>.json. A scene with
# a "target" plays the model's action on the target model's armature instead (clips shared by bone
# names) and writes the target's bones.
# Run by Tools/Scripts/make-blender-references.sh:
#   blender -b --factory-startup --python evaluate_animations.py -- <anim-scenes.json> <golden folder> [scene ...]
#
# The models are built again by make_models.py (their source), so Blender evaluates its own actions
# (F-curves, constraints), not the exported samples. A bone's pose is written as its deformation in
# model space: its posed matrix after the inverse of its rest matrix (both in the armature object's
# world), in glTF's coordinates ((x, y, z) -> (x, z, -y), as the exporter converts); the 3x4 part, row
# by row, for column vectors. With it each bone's rest head and tail (glTF coordinates), so the tests
# can measure how far a bone's ends are off.
import json
import math
import os
import sys

import bpy
from mathutils import Matrix

sys.dont_write_bytecode = True  # no __pycache__ in the repo
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_models  # noqa: E402

TO_GLTF = Matrix(((1, 0, 0, 0), (0, 0, 1, 0), (0, -1, 0, 0), (0, 0, 0, 1)))


def numbers(values):
    return [float("%.9g" % v) for v in values]


def evaluate(scene, folder):
    make_models.reset()
    make_models.BUILDERS[scene["model"]](None)
    rig = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    if scene.get("target"):
        # Another model of the rig family plays the model's action: Blender finds each F-curve's bone
        # by name (pose.bones["name"]) and applies its channels on that bone's own rest.
        make_models.BUILDERS[scene["target"]](None)
        rig = next(o for o in bpy.data.objects if o.type == "ARMATURE" and o is not rig)
    if rig.animation_data is None:
        rig.animation_data_create()
    # The builder leaves the actions on NLA tracks for the exporter; a scene plays one as the active
    # action over nothing else.
    for track in rig.animation_data.nla_tracks:
        track.mute = True
    action = bpy.data.actions[scene["clip"]]
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    blender_scene = bpy.context.scene
    fps = blender_scene.render.fps / blender_scene.render.fps_base
    start = action.frame_range[0]
    world = rig.matrix_world
    bones = {}
    for bone in rig.data.bones:
        head = TO_GLTF @ world @ bone.head_local
        tail = TO_GLTF @ world @ bone.tail_local
        bones[bone.name] = {"head": numbers(head), "tail": numbers(tail)}
    poses = []
    for time in scene["times"]:
        frame = start + time * fps
        if abs(frame - round(frame)) < 1e-6:
            frame = round(frame)
        whole = math.floor(frame)
        blender_scene.frame_set(whole, subframe=frame - whole)
        pose = {}
        for bone in rig.pose.bones:
            deformation = TO_GLTF @ world @ bone.matrix @ bone.bone.matrix_local.inverted() @ world.inverted() @ TO_GLTF.inverted()
            pose[bone.name] = numbers([deformation[r][c] for r in range(3) for c in range(4)])
        poses.append(pose)
    path = os.path.join(folder, "anim-" + scene["name"] + ".json")
    with open(path, "w") as f:
        json.dump({"bones": bones, "times": scene["times"], "poses": poses}, f, separators=(",", ":"))
    print("wrote", path)


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    scenes = json.load(open(args[0]))["scenes"]
    wanted = set(args[2:])
    for scene in scenes:
        if not wanted or scene["name"] in wanted:
            evaluate(scene, args[1])


main()

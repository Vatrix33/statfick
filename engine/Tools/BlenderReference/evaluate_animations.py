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
from mathutils import Matrix, Quaternion

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
    blender_scene = bpy.context.scene
    fps = blender_scene.render.fps / blender_scene.render.fps_base
    # The builder leaves the actions on NLA tracks for the exporter: a scene plays one as the active
    # action over nothing else, or builds NLA tracks of its own.
    for track in list(rig.animation_data.nla_tracks):
        rig.animation_data.nla_tracks.remove(track)
    if scene.get("layers"):
        rig.animation_data.action = None
        for name in sorted({strip["clip"] for layer in scene["layers"] for strip in layer}):
            bake_rotations(bpy.data.actions[name])
        build_layers(rig, scene["layers"], fps)
        start = 0
    else:
        action = bpy.data.actions[scene["clip"]]
        rig.animation_data.action = action
        rig.animation_data.action_slot = action.slots[0]
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


def bake_rotations(action):
    """Replaces the action's quaternion curves by a key per frame holding the normalised value
    (Docs/Animation.md, "Fades of rotations"): Blender's NLA mixes the curves' raw values, which
    aren't unit length between keys, while the exported clips hold unit quaternions; baked, both
    hold the same values on frames, so the NLA rule itself can be checked exactly."""
    channelbag = action.layers[0].strips[0].channelbag(action.slots[0])
    start, end = (int(f) for f in action.frame_range)
    bones = {c.data_path for c in channelbag.fcurves if c.data_path.endswith(".rotation_quaternion")}
    for path in sorted(bones):
        curves = [channelbag.fcurves.find(path, index=i) for i in range(4)]
        frames = range(start, end + 1)
        values = [Quaternion([c.evaluate(f) for c in curves]).normalized() for f in frames]
        for i, curve in enumerate(curves):
            curve.keyframe_points.clear()
            curve.keyframe_points.add(len(values))
            for point, f, q in zip(curve.keyframe_points, frames, values):
                point.co = (f, q[i])
                point.interpolation = "LINEAR"


def build_layers(rig, layers, fps):
    """NLA tracks as the scene's layers (bottom first): each strip placed at its start (seconds times
    the frame rate), its action range the clip's part (clip time 0 is the action's first frame, as
    the exporter slides it), then scale, repeat, direction, blend type, extrapolation, blend in and
    out (after the length, which clamps them, in then out as Blender's setters do) and a fixed
    influence (an influence F-curve whose one key holds the value)."""
    for layer in layers:
        track = rig.animation_data.nla_tracks.new()
        for number, spec in enumerate(layer):
            action = bpy.data.actions[spec["clip"]]
            first = action.frame_range[0]
            strip = track.strips.new("%s.%d" % (spec["clip"], number), int(math.floor(spec["start"] * fps)), action)
            strip.action_slot = action.slots[0]
            strip.action_frame_end = first + (spec["clipEnd"] if spec.get("clipEnd") is not None else (action.frame_range[1] - first) / fps) * fps
            strip.action_frame_start = first + spec["clipStart"] * fps
            strip.scale = spec["scale"]
            strip.repeat = spec["repeat"]
            strip.frame_start_ui = spec["start"] * fps
            strip.use_reverse = spec["reversed"]
            strip.blend_type = spec["mode"]
            strip.extrapolation = spec["extrapolation"]
            strip.blend_in = spec["blendIn"] * fps
            strip.blend_out = spec["blendOut"] * fps
            if spec.get("influence") is not None:
                strip.use_animated_influence = True
                for point in strip.fcurves.find("influence").keyframe_points:
                    point.co[1] = point.handle_left[1] = point.handle_right[1] = spec["influence"]


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    scenes = json.load(open(args[0]))["scenes"]
    wanted = set(args[2:])
    for scene in scenes:
        if not wanted or scene["name"] in wanted:
            evaluate(scene, args[1])


if __name__ == "__main__":
    main()

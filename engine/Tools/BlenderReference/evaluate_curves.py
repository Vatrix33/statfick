# Builds the test F-curves as Blender keyframes and writes what Blender makes of them (Docs/Scene.md,
# "How it works", step 2): Tests/Statventure.Tests/Golden/curve-<scene>.json, per curve each key's
# handles after Blender's update (fcurve.update(): handles fixed and recalculated) and the curve's value
# at the scene's times.
# Run by Tools/Scripts/make-blender-references.sh:
#   blender -b --factory-startup --python evaluate_curves.py -- <curve-scenes.json> <golden folder> [scene ...]
#
# The scene file (written by FCurveReferenceTests.TheSceneFileIsUpToDate from Scene/FCurveScenes.cs) is in
# the engine's units: times in seconds, converted here to Blender's frames at 60 fps. Every field of a key
# is set (the Python API's keyframe_points.add leaves back, amplitude and period at 0 and the handles
# selected); a side whose position the key doesn't store starts where Blender's key insertion puts it (a
# frame to either side, at the key's value), which is what a curve of one key keeps. No key is selected,
# as the engine assumes where Blender's aligning depends on it. Handles are written in seconds, values as
# the float32 Blender computed.
import json
import os
import sys

import bpy

FPS = 60
INTERPOLATION = {"constant": "CONSTANT", "linear": "LINEAR", "bezier": "BEZIER", "sine": "SINE", "quad": "QUAD", "cubic": "CUBIC",
                 "quart": "QUART", "quint": "QUINT", "expo": "EXPO", "circ": "CIRC", "back": "BACK", "bounce": "BOUNCE", "elastic": "ELASTIC"}
EASING = {"auto": "AUTO", "in": "EASE_IN", "out": "EASE_OUT", "inOut": "EASE_IN_OUT"}
HANDLE = {"free": "FREE", "aligned": "ALIGNED", "vector": "VECTOR", "auto": "AUTO", "autoClamped": "AUTO_CLAMPED"}
EXTRAPOLATION = {"constant": "CONSTANT", "linear": "LINEAR"}
SMOOTHING = {"none": "NONE", "continuousAcceleration": "CONT_ACCEL"}


def build(channelbag, spec):
    curve = channelbag.fcurves.new('["%s"]' % spec["name"], index=0)
    curve.extrapolation = EXTRAPOLATION[spec["extrapolation"]]
    curve.auto_smoothing = SMOOTHING[spec["smoothing"]]
    keys = spec["keys"]
    curve.keyframe_points.add(len(keys))
    for point, key in zip(curve.keyframe_points, keys):
        frame = key["time"] * FPS
        point.co = (frame, key["value"])
        point.interpolation = INTERPOLATION[key["interpolation"]]
        point.easing = EASING[key["easing"]]
        point.handle_left_type = HANDLE[key["left"]]
        point.handle_right_type = HANDLE[key["right"]]
        point.handle_left = (key["leftTime"] * FPS, key["leftValue"]) if "leftTime" in key else (frame - 1, key["value"])
        point.handle_right = (key["rightTime"] * FPS, key["rightValue"]) if "rightTime" in key else (frame + 1, key["value"])
        point.back = key["back"]
        point.amplitude = key["amplitude"]
        point.period = key["period"] * FPS
        point.select_control_point = point.select_left_handle = point.select_right_handle = False
    curve.update()
    if len(curve.keyframe_points) != len(keys):
        raise RuntimeError("%s: Blender merged keys" % spec["name"])
    return curve


def evaluate(scene, folder):
    action = bpy.data.actions.new(scene["name"])
    slot = action.slots.new(id_type="OBJECT", name=scene["name"])
    strip = action.layers.new("curves").strips.new(type="KEYFRAME")
    channelbag = strip.channelbags.new(slot)
    out = []
    for spec in scene["curves"]:
        curve = build(channelbag, spec)
        handles = [[p.handle_left[0] / FPS, p.handle_left[1], p.handle_right[0] / FPS, p.handle_right[1]] for p in curve.keyframe_points]
        values = [curve.evaluate(t * FPS) for t in spec["times"]]
        out.append({"name": spec["name"], "handles": handles, "values": values})
    path = os.path.join(folder, "curve-" + scene["name"] + ".json")
    with open(path, "w") as f:
        json.dump({"curves": out}, f, separators=(",", ":"))
        f.write("\n")
    print("wrote", path)


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    scenes = json.load(open(args[0]))["scenes"]
    wanted = set(args[2:])
    for scene in scenes:
        if not wanted or scene["name"] in wanted:
            evaluate(scene, args[1])


if __name__ == "__main__":
    main()

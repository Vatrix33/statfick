# The humanoid family's clip library (Docs/Game/Hero.md, "The humanoid rig family"; M7: 60 fps, no root motion, events
# as pose markers, clips shared by bone names): every clip of a folder, <folder>/<name>.json, keyed as a Blender action
# on the human man's skeleton (the average build: clips are authored there, Hero.md) and exported as the engine's
# animated models are (make_models.ANIMATED: sampled every frame from 0 s, each clip a glTF animation, only the channels
# it keys, its markers as the animation's events). Any body of the family plays them by bone names on its own rest; a
# location (the hips') is metres keyed on the man, so each clip carries its measure (GM2-P22, "the figure's ground";
# Docs/Animation.md, "A clip's translations follow the body's size"): the action's custom property "measure", the
# exporter's animation extras, MEASURE below, the left leg's thigh, shin and foot. On a body whose leg is another length
# (the distances hip joint to knee and knee to ankle at rest, added) a location is scaled by that length over the man's,
# on the engine's side (Animation/Skeleton.cs, ClipBinding.Scale) and on Blender's wherever a clip is evaluated or
# rendered (BlenderReference/evaluate_animations.py, render.py's pose_at, so assemble.py's). A whole-body clip also keeps
# its contacts with the ground (GM2-P37, "contacts kept"; Docs/Animation.md, "A clip keeps its contacts"): the action's
# custom property "contacts" (KEYED_ON, ROOT, CHAINS below), by which the engine (Animation/Contacts.cs) and Blender's side
# (BlenderReference/clip_contacts.py) put what touches the ground on the man where it touches on every other body. The
# family's clips go in this folder (clips/; the first come with the manager's hand-over) and their library into
# Assets/Models/humanoid/clips.glb, so into models.pak.
#
# A clip's file (JSON), its keys exactly these:
#   name      the clip's name, the file's own ("idle" in clips/idle.json)
#   frames    its length in frames at 60 fps (a whole number above 0)
#   loop      whether it loops: then its last key repeats its first (at frames, every bone and value the same)
#   markers   {"<event>": frame}: its events (pose markers), each at a frame of the clip
#   keys      [{"frame": n, "rotations": {"<bone>": [x, y, z]}, "locations": {"<bone>": [x, y, z]}}], frames rising from
#             0 to at most `frames`; a rotation is XYZ Euler degrees in the bone's own axes from its rest, a location
#             metres in the bone's own axes; a bone no key names gets no channel; either map may be left out
#   ease      "bezier" (Blender's own keys: Bézier, auto-clamped handles, as inserted) or "linear" (every key linear)
# Keyed as the manager's previews key them (so the in-betweens are the same): each rotation becomes the quaternion of its
# Euler angles, taken with the sign nearer the bone's previous key (negated when their dot product is below 0), keyed as
# the pose bone's rotation_quaternion; each location as its location. Data frame n is Blender frame n + 1 (every clip of
# the repo starts at frame 1; the export slides it to 0 s), and the action's frame range is the clip's frames, so a clip
# whose last key comes early still lasts `frames`.
#
# The frame's test clip, rest-sway (GM2 step 4a; test data, not art, never in a pack: a slow sway and a breath), is in the
# tests' data, Tests/Statventure.Tests/Data/Models/humanoid/test-clips/, built by this into test-clips.glb beside it
# (manifest.json, "test-clips"): it proves the library end to end (keyed here, exported, evaluated by Blender against the
# engine's sampling, played on both bodies, in the figure mode).
import glob
import json
import math
import os

import bpy
from mathutils import Euler

import bodies
import forms
import rig as r
from make_models import ANIMATED, curves, export, stack_actions

HERE = os.path.dirname(os.path.abspath(__file__))
ENGINE = os.path.normpath(os.path.join(HERE, "..", "..", "..", ".."))
# The skeleton clips are keyed on (Hero.md: authored on the human average body).
SKELETON = "human-male-average"
# What measures a body for a clip's locations (GM2-P22): the joints of the left leg, top first. Its length at rest, the
# hip joint to the knee and the knee to the ankle, is the legs' that fold under the hips (the bodies are symmetric: the
# right leg's is the same).
MEASURE = ["thigh.L", "shin.L", "foot.L"]
# What a clip keeps touching the ground on another body (GM2-P37, "contacts kept"; Docs/Animation.md, "A clip keeps its
# contacts"): the action's custom property "contacts", the exporter's animation extras: the body it was keyed on (the man
# with his outfit: his boots are his soles), the joint whose place carries the body, and the limbs whose ends reach the
# ground, each top, middle, end (the legs; the arms, whose fists a fall puts down). Written on a clip that keys a place
# (the whole-body clips; a stance loop of the arms moves no hips and keeps none), each chain only where the clip turns
# all three of its joints (a clip doesn't gain channels: flee leaves the hands alone).
KEYED_ON = "models/humanoid/bodies/" + SKELETON
ROOT = "hips"
CHAINS = [["thigh.L", "shin.L", "foot.L"], ["thigh.R", "shin.R", "foot.R"],
          ["upper_arm.L", "forearm.L", "hand.L"], ["upper_arm.R", "forearm.R", "hand.R"]]
KEYS = {"name", "frames", "loop", "markers", "keys", "ease"}


def _vector(value, where):
    if not (isinstance(value, list) and len(value) == 3 and all(isinstance(v, (int, float)) and not isinstance(v, bool) and math.isfinite(v) for v in value)):
        raise ValueError(f"{where}: three numbers")
    return value


def load(path, bones):
    """The clip in `path`, checked: every rule of the file's form above, the bones those of `bones`. Raises ValueError
    naming the file and the first problem."""
    where = os.path.basename(path)
    with open(path) as f:
        clip = json.load(f)
    if not isinstance(clip, dict) or set(clip) != KEYS:
        raise ValueError(f"{where}: its keys must be {', '.join(sorted(KEYS))}")
    if clip["name"] != os.path.splitext(where)[0]:
        raise ValueError(f"{where}: its name is {clip['name']!r}, not the file's")
    frames = clip["frames"]
    if isinstance(frames, bool) or not isinstance(frames, int) or frames < 1:
        raise ValueError(f"{where}: frames must be a whole number above 0")
    if not isinstance(clip["loop"], bool):
        raise ValueError(f"{where}: loop must be true or false")
    if clip["ease"] not in ("bezier", "linear"):
        raise ValueError(f"{where}: ease must be \"bezier\" or \"linear\"")
    if not isinstance(clip["markers"], dict):
        raise ValueError(f"{where}: markers must be an object")
    for event, frame in clip["markers"].items():
        if isinstance(frame, bool) or not isinstance(frame, int) or not 0 <= frame <= frames:
            raise ValueError(f"{where}: marker {event!r} must be at a frame from 0 to {frames}")
    keys = clip["keys"]
    if not isinstance(keys, list) or not keys:
        raise ValueError(f"{where}: keys must be a list of at least one key")
    last = -1
    for key in keys:
        if not isinstance(key, dict) or "frame" not in key or not set(key) <= {"frame", "rotations", "locations"}:
            raise ValueError(f"{where}: a key holds frame, rotations and locations only")
        frame = key["frame"]
        if isinstance(frame, bool) or not isinstance(frame, int) or not last < frame <= frames:
            raise ValueError(f"{where}: key frames must rise from 0 to at most {frames} (found {frame!r} after {last})")
        last = frame
        for channel in ("rotations", "locations"):
            values = key.get(channel, {})
            if not isinstance(values, dict):
                raise ValueError(f"{where}: frame {frame}: {channel} must be an object")
            for bone, value in values.items():
                if bone not in bones:
                    raise ValueError(f"{where}: frame {frame}: no bone {bone!r} in the skeleton")
                _vector(value, f"{where}: frame {frame}: {channel} of {bone}")
    if clip["loop"]:
        first, end = keys[0], keys[-1]
        if first["frame"] != 0 or end["frame"] != frames:
            raise ValueError(f"{where}: a looping clip's keys must start at 0 and end at {frames}")
        for channel in ("rotations", "locations"):
            a, b = first.get(channel, {}), end.get(channel, {})
            for bone in sorted(set(a) | set(b)):
                if a.get(bone) != b.get(bone):
                    raise ValueError(f"{where}: a looping clip's last key must repeat its first: {channel} of {bone}")
    return clip


def key(rig, clip):
    """The clip as an action of `rig`."""
    action = bpy.data.actions.new(clip["name"])
    rig.animation_data.action = action
    previous = {}
    for k in clip["keys"]:
        frame = k["frame"] + 1
        for name, euler in k.get("rotations", {}).items():
            q = Euler([math.radians(a) for a in euler], "XYZ").to_quaternion()
            if name in previous and previous[name].dot(q) < 0:
                q.negate()
            previous[name] = q
            bone = rig.pose.bones[name]
            bone.rotation_mode = "QUATERNION"
            bone.rotation_quaternion = q
            bone.keyframe_insert("rotation_quaternion", frame=frame)
        for name, location in k.get("locations", {}).items():
            bone = rig.pose.bones[name]
            bone.location = location
            bone.keyframe_insert("location", frame=frame)
    if clip["ease"] == "linear":
        for curve in curves(action):
            for point in curve.keyframe_points:
                point.interpolation = "LINEAR"
    for event, frame in clip["markers"].items():
        action.pose_markers.new(event).frame = frame + 1
    action.use_frame_range = True
    action.frame_start, action.frame_end = 1, clip["frames"] + 1
    action["measure"] = MEASURE
    contacts = contacts_of(clip)
    if contacts is not None:
        action["contacts"] = contacts
    return action


def contacts_of(clip):
    """The clip's contacts (GM2-P37): for a clip that keys the root's place, the man it was keyed on, the root and each chain
    whose three joints it turns; None for one that keys no place or turns no whole chain (the frame's test clip rest-sway
    leaves the legs alone)."""
    turned = {bone for k in clip["keys"] for bone in k.get("rotations", {})}
    chains = [chain for chain in CHAINS if all(bone in turned for bone in chain)]
    if not any(ROOT in k.get("locations", {}) for k in clip["keys"]) or not chains:
        return None
    return {"keyedOn": KEYED_ON, "root": ROOT, "chains": chains}


def files(folder):
    return sorted(glob.glob(os.path.join(folder, "*.json")))


def library(path, folder=None):
    """A library (manifest.json: "clips.library:library", args [folder]): every clip of `folder` (relative to engine/;
    this folder by default) on the skeleton, exported."""
    folder = HERE if folder is None else os.path.join(ENGINE, folder)
    scene = bpy.context.scene
    scene.render.fps, scene.render.fps_base = 60, 1
    p = bodies.params(SKELETON)
    rig = r.build_rig(p, forms.joints_of(p))
    rig.animation_data_create()
    bones = set(r.BONES + r.SOCKETS)
    actions = [key(rig, load(file, bones)) for file in files(folder)]
    if not actions:
        raise ValueError(f"no clip in {folder}")
    stack_actions(rig, actions)
    export(path, **ANIMATED)

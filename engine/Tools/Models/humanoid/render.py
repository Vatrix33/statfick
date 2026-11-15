# Renders a figure (a body .glb with its garments and items) or a scene file in a look (look.py), at a size and in
# views (GM2 step 3a; Docs/Game/Hero.md, "How it works", step 3a): the mood board's and the approval sheets' renders.
# Blender's side of the look is the engine's references' own (Tools/BlenderReference/render_scenes.py, imported and run
# on a scene file written here: EEVEE's cel shading, the hatching, the glow's renders and Freestyle's ink), so a render
# is what the engine's renderer is held to; Tools/Scripts/make-humanoid.sh then composes the glow (compose_glow.py) and
# lays the ink over the shading (compose.py), as the engine draws its lines over its view.
# Run inside Blender 5.2, headless, by make-humanoid.sh render:
#   blender -b --factory-startup --python render.py -- --work <folder> --name <stem> [--look <name or file>]
#       (--model <body.glb> [--garment <glb>]... [--item <glb>@<socket>]... | --scene <scene file>)
#       [--views front,three-quarter,side,back,close-up,<degrees>] [--size WxH] [--scale s] [--elevation degrees]
#       [--focus <bone>] [--extent metres] [--margin fraction] [--perspective degrees]
#       [--clip <name> [--time seconds] [--clips <clip library .glb>]] [--scenes-only]
# Views (the figure turned under a fixed camera and sun, so each side is lit alike; the figure faces glTF's +Z): front
# (0°), three-quarter (45°: its front turned toward the screen's right), side (90°: facing the screen's right), back
# (180°), a number of degrees, and close-up (45°, framed on --focus, the "head" bone by default, else the figure's top,
# --extent high). Each view frames the figure at rest (the sandbox's figure mode frames the same way, Figure/
# FigureView.cs), so the frames of a clip share one camera. Orthographic unless --perspective gives a vertical field of
# view; the camera looks --elevation degrees down (5 by default: a face exactly edge-on is where Freestyle and the
# engine part, Docs/Renderer-3D.md, "Freestyle's limits seen"). A clip poses the figure at --time (its own or the clip
# library's, played by bone names), evaluated by Blender. With --scene, every scene of the file is rendered, the look (if given)
# replacing its background, sun, style and palette. Writes <work>/<stem>-scenes.json (the scenes as render_scenes.py
# reads them: render them again, or compare the engine's with them) and r3d-<stem>-<view>.png and -ink.png beside it;
# with --scenes-only the scene file alone (how the engine's tests hold the figure mode's framing to this one's:
# Tests/Statventure.Tests/Reference/figure-framing-scenes.json, make-humanoid.sh framing).
import json
import math
import os
import struct
import sys

import bpy
from mathutils import Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
sys.dont_write_bytecode = True  # no __pycache__ in the repo
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.normpath(os.path.join(HERE, "..", "..", "BlenderReference")))

import look as looks  # noqa: E402
import render_scenes  # noqa: E402

# glTF (x, y, z) → Blender (x, −z, y), and back.
TO_BLENDER = render_scenes.GLTF_TO_BLENDER.to_4x4()
TO_GLTF = TO_BLENDER.inverted()
VIEWS = {"front": 0, "three-quarter": 45, "side": 90, "back": 180, "close-up": 45}


def arguments(argv):
    options = {"models": [], "garments": [], "items": [], "views": "three-quarter", "size": "600x800", "scale": 1.0,
               "elevation": 5.0, "margin": 0.06}
    i = 0
    while i < len(argv):
        if argv[i] == "--scenes-only":
            options["scenes-only"] = True
            i += 1
            continue
        key, value = argv[i], argv[i + 1] if i + 1 < len(argv) else None
        if value is None:
            raise SystemExit(f"{key} needs a value")
        name = key.lstrip("-")
        if name == "model":
            options["models"].append(value)
        elif name == "garment":
            options["garments"].append(value)
        elif name == "item":
            options["items"].append(value)
        elif name in ("work", "name", "look", "scene", "views", "size", "focus", "clip", "clips"):
            options[name] = value
        elif name in ("scale", "elevation", "extent", "margin", "perspective", "time"):
            options[name] = float(value)
        else:
            raise SystemExit(f"render.py: unknown option {key}")
        i += 2
    if not options.get("work") or not options.get("name"):
        raise SystemExit("render.py: --work and --name are needed")
    if bool(options.get("scene")) == bool(options["models"]):
        raise SystemExit("render.py: a --model or a --scene")
    return options


def columns(m):
    """A mathutils matrix as the scene file's four columns."""
    return [[m[r][c] for r in range(4)] for c in range(4)]


def flat_columns(m):
    return [m[r][c] for c in range(4) for r in range(4)]


def import_glb(path):
    before = set(bpy.data.objects)
    actions_before = set(bpy.data.actions)
    bpy.ops.import_scene.gltf(filepath=path)
    # Only what the view shows (the importer also makes its bone shape, an icosphere in a collection the view layer
    # excludes, "glTF_not_exported": in the scene's objects, but not visible).
    return [o for o in bpy.data.objects if o not in before and o.visible_get()], [a for a in bpy.data.actions if a not in actions_before]


def glb_rest_worlds(path, children=None):
    """Every node's rest world matrix (glTF model space, column vectors) by name, from the file's TRS; with a dict
    `children`, each node's children's names go into it."""
    data = open(path, "rb").read()
    length = struct.unpack_from("<I", data, 12)[0]
    gltf = json.loads(data[20:20 + length])
    nodes = gltf.get("nodes", [])
    local = []
    for node in nodes:
        if "matrix" in node:
            m = node["matrix"]
            local.append(Matrix([[m[c * 4 + r] for c in range(4)] for r in range(4)]))
            continue
        t = Matrix.Translation(node.get("translation", (0, 0, 0)))
        x, y, z, w = node.get("rotation", (0, 0, 0, 1))
        from mathutils import Quaternion
        r = Quaternion((w, x, y, z)).to_matrix().to_4x4()
        s = Matrix.Diagonal((*node.get("scale", (1, 1, 1)), 1))
        local.append(t @ r @ s)
    parent = {c: i for i, n in enumerate(nodes) for c in n.get("children", [])}
    worlds = {}

    def world(i):
        if i not in worlds:
            worlds[i] = (world(parent[i]) @ local[i]) if i in parent else local[i]
        return worlds[i]
    names = [node.get("name", str(i)) for i, node in enumerate(nodes)]
    if children is not None:
        for i, node in enumerate(nodes):
            children[names[i]] = [names[c] for c in node.get("children", [])]
    return {names[i]: world(i) for i in range(len(nodes))}


def rest_pose(body):
    """Every joint at rest (no deformation). Given explicitly: Blender's importer leaves an animated armature at its
    first clip's pose a frame in, which clearing the animation data keeps."""
    data = open(body, "rb").read()
    length = struct.unpack_from("<I", data, 12)[0]
    gltf = json.loads(data[20:20 + length])
    nodes = gltf.get("nodes", [])
    return {nodes[j].get("name", str(j)): flat_columns(Matrix.Identity(4)) for skin in gltf.get("skins", []) for j in skin["joints"]}


def pose_at(body, options):
    """The joints' deformations (glTF model space; the scene file's "pose") at the clip's time, as Blender evaluates
    the clip on the body's armature (a clip library's played by bone names), or every joint at rest."""
    if not options.get("clip"):
        return rest_pose(body)
    render_scenes.reset()
    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)
    scene = bpy.context.scene
    scene.render.fps, scene.render.fps_base = 60, 1
    objects, actions = import_glb(body)
    rig = next((o for o in objects if o.type == "ARMATURE"), None)
    if rig is None:
        raise SystemExit(f"{body}: no armature to pose")
    if options.get("clips"):
        library, actions = import_glb(options["clips"])
        for o in library:
            bpy.data.objects.remove(o)
    action = next((a for a in actions if a.name == options["clip"]), None) \
        or next((a for a in actions if a.name.startswith(options["clip"] + "_")), None)
    if action is None:
        raise SystemExit(f"no clip {options['clip']} (clips: {', '.join(sorted(a.name for a in actions))})")
    if rig.animation_data is None:
        rig.animation_data_create()
    for track in list(rig.animation_data.nla_tracks):
        rig.animation_data.nla_tracks.remove(track)
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    frame = action.frame_range[0] + options.get("time", 0.0) * 60
    whole = math.floor(frame + 1e-6)
    scene.frame_set(whole, subframe=max(0.0, frame - whole))
    world = rig.matrix_world
    pose = {}
    for bone in rig.pose.bones:
        d = TO_GLTF @ world @ bone.matrix @ bone.bone.matrix_local.inverted() @ world.inverted() @ TO_BLENDER
        pose[bone.name] = flat_columns(d)
    return pose


def figure_models(options, pose):
    """The figure as the scene file's models (one group: one outline, the hatching anchored at its origin), placed
    unturned: the body, its garments (posed by the same joints), its items on their sockets (their grip the socket's
    posed world after the object axes' conversion is undone, as Animation/Gear.cs's Socket places them)."""
    body = os.path.abspath(options["models"][0])
    models = [{"file": body, "pose": pose}]
    models += [{"file": os.path.abspath(g), "pose": pose} for g in options["garments"]]
    if options["items"]:
        rest = glb_rest_worlds(body)
        object_axes_to_bone = TO_BLENDER  # C⁻¹: glTF object axes back to Blender's, as a bone's are
        for spec in options["items"]:
            path, _, socket = spec.partition("@")
            if socket not in rest:
                raise SystemExit(f"{body}: no socket {socket}")
            deformation = Matrix.Identity(4)
            if pose and socket in pose:
                p = pose[socket]
                deformation = Matrix([[p[c * 4 + r] for c in range(4)] for r in range(4)])
            models.append({"file": os.path.abspath(path), "pose": None, "local": deformation @ rest[socket] @ object_axes_to_bone})
    return models


def figure_points(models):
    """The figure's posed surface points (glTF model space), for framing it."""
    render_scenes.reset()
    points = []
    for model in models:
        objects, _ = import_glb(model["file"])
        for o in objects:
            o.animation_data_clear()
        holder = bpy.data.objects.new("placement", None)
        bpy.context.scene.collection.objects.link(holder)
        holder.matrix_world = TO_BLENDER @ model.get("local", Matrix.Identity(4)) @ TO_GLTF
        for o in objects:
            if o.parent is None:
                o.parent = holder
        if model["pose"]:
            render_scenes.pose_model(objects + [holder], holder, model["pose"])
        bpy.context.view_layer.update()
        depsgraph = bpy.context.evaluated_depsgraph_get()
        for o in objects:
            if o.type != "MESH":
                continue
            evaluated = o.evaluated_get(depsgraph)
            mesh = evaluated.to_mesh()
            points += [(TO_GLTF @ evaluated.matrix_world @ v.co) for v in mesh.vertices]
            evaluated.to_mesh_clear()
    if not points:
        raise SystemExit("the figure has no mesh")
    return points


def bone_point(body, bone):
    """A bone's middle at rest (glTF model space), or None without that bone. A joint node's tail isn't in glTF: the
    middle is halfway to its first child, or without one 0.1 m up its own Y (the bone's direction)."""
    children = {}
    rest = glb_rest_worlds(body, children)
    if bone not in rest:
        return None
    head = rest[bone].translation
    tail = rest[children[bone][0]].translation if children[bone] else (rest[bone] @ Vector((0, 0.2, 0, 1))).xyz
    return head.lerp(tail, 0.5)


def view_spec(options, name, view, look, models, points, focus):
    width, height = (int(v) for v in options["size"].lower().split("x"))
    turn = VIEWS[view] if view in VIEWS else float(view)
    rotation = Matrix.Rotation(math.radians(turn), 4, "Y")
    turned = [rotation @ p for p in points]
    xs, ys, zs = [p.x for p in turned], [p.y for p in turned], [p.z for p in turned]
    aspect = width / height
    margin = 1 + 2 * options["margin"]
    if view == "close-up":
        tall = max(ys) - min(ys)
        target = rotation @ focus if focus is not None else Vector(((min(xs) + max(xs)) / 2, max(ys) - 0.15 * tall, (min(zs) + max(zs)) / 2))
        extent = options.get("extent") or 0.3 * tall
    else:
        target = Vector(((min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2, (min(zs) + max(zs)) / 2))
        extent = options.get("extent") or max(max(ys) - min(ys), (max(xs) - min(xs)) / aspect) * margin
    elevation = math.radians(options["elevation"])
    reach = max(max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs))
    camera = {"near": 0.1, "far": 50, "up": [0, 1, 0], "target": list(target), "orthographicHeight": extent, "fieldOfViewDegrees": 40}
    if options.get("perspective"):
        fov = options["perspective"]
        distance = extent / 2 / math.tan(math.radians(fov) / 2)
        camera.update(projection="perspective", fieldOfViewDegrees=fov)
    else:
        distance = reach + 5
        camera.update(projection="orthographic")
    camera["far"] = max(50, distance + reach + 10)
    camera["position"] = list(target + distance * Vector((0, math.sin(elevation), math.cos(elevation))))
    scale = options["scale"]
    style = dict(look["style"], outlineWidth=look["style"]["outlineWidth"] * scale, innerWidth=look["style"]["innerWidth"] * scale)
    return {
        "name": f"{name}-{view}", "width": width, "height": height, "background": look["background"], "camera": camera,
        "objects": [],
        "models": [{"file": m["file"], "matrix": columns(rotation @ m.get("local", Matrix.Identity(4))), "scenery": False,
                    "pose": m["pose"], "group": "figure", "anchor": [0, 0, 0], "objects": None, "clipTime": None, "palette": None}
                   for m in models],
        "palette": look["palette"], "shadows": look["shadows"], "sun": look["sun"], "style": style, "scale": scale,
        "terrain": None, "time": 0, "particles": [],
    }


def scene_from_file(options, look):
    """Every scene of a scene file, its models' paths made absolute, the look (if given) in place of its own."""
    path = os.path.abspath(options["scene"])
    with open(path) as f:
        data = json.load(f)
    models = os.path.normpath(os.path.join(os.path.dirname(path), data["modelFolder"]))
    terrain = os.path.normpath(os.path.join(os.path.dirname(path), data["terrainFolder"]))
    for spec in data["scenes"]:
        spec["name"] = f"{options['name']}-{spec['name']}"
        for model in spec.get("models", []):
            model["file"] = os.path.join(models, model["file"])
        if look is not None:
            scale = spec.get("scale", 1)
            spec.update(background=look["background"], sun=look["sun"], palette=look["palette"], shadows=look["shadows"],
                        style=dict(look["style"], outlineWidth=look["style"]["outlineWidth"] * scale,
                                   innerWidth=look["style"]["innerWidth"] * scale))
            for model in spec.get("models", []):
                model["palette"] = None
    return data["scenes"], data.get("shapes", {}), terrain


def main():
    options = arguments(sys.argv[sys.argv.index("--") + 1:])
    work = os.path.abspath(options["work"])
    os.makedirs(work, exist_ok=True)
    look = looks.load(options["look"]) if options.get("look") else None
    terrain = "."
    shapes = {}
    if options.get("scene"):
        scenes, shapes, terrain = scene_from_file(options, look)
    else:
        look = look or looks.load("today")
        pose = pose_at(options["models"][0], options)
        models = figure_models(options, pose)
        # Framed at rest: the camera doesn't follow the pose.
        points = figure_points(figure_models(options, rest_pose(options["models"][0])))
        focus = bone_point(options["models"][0], options.get("focus") or "head")
        if options.get("focus") and focus is None:
            raise SystemExit(f"no bone {options['focus']} to frame")
        views = [v.strip() for v in options["views"].split(",") if v.strip()]
        for view in views:
            if view not in VIEWS:
                float(view)
        scenes = [view_spec(options, options["name"], view, look, models, points, focus) for view in views]
    path = os.path.join(work, options["name"] + "-scenes.json")
    # The models by their paths from the file's folder (a file to keep, not this machine's paths).
    for spec in scenes:
        for model in spec.get("models", []):
            model["file"] = os.path.relpath(model["file"], work)
    with open(path, "w") as f:
        json.dump({"modelFolder": ".", "terrainFolder": os.path.relpath(terrain, work), "shapes": shapes, "scenes": scenes}, f, indent=2)
    print("wrote", path)
    if options.get("scenes-only"):
        return
    sys.argv = [sys.argv[0], "--", path, work, os.path.join(work, "glow")]
    render_scenes.main()


if __name__ == "__main__":
    main()

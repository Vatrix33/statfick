# A hero's look assembled in Blender (GM2 step 5; GM2-P8, GM2-P9; Docs/Game/Hero.md, "Assembly in Blender" and "How it
# works", step 5): a look.hero (its JSON form: race, sex, body, parts, colours) read against the game's data as the game
# reads it, here on its own (the records' JSON forms in data folders, a later folder's entry over an earlier's as a mod's
# over the game's; the models' .glb files in a models folder, models/<path> being <models>/<path>.glb), then posed,
# framed and written as Blender's reference harness renders a scene (Tools/BlenderReference/render_scenes.py, through
# render.py's figure, views and framing), so the engine's figure built from the same look (Game/Statventure.Game/Figures/
# HeroFigure.cs) is held to Blender's picture of it, and to what this assembly holds (its models and its palette).
# The figure: the look's body model (its skeleton and plain outfit); each part the look chooses, its model for the look's
# body, bound to the body's armature by bone names as a garment ("Rendered gear", Docs/Animation.md); the gear's garments
# likewise and its rigid items on their sockets; the palette: each colour the look chooses (by slot, a swatch's name) the
# race's swatch of that name, then the gear's own colours. The style and the sun: the data's render.style entry
# render/styles/game (the game's look); the background the caller's (the view's own). A look naming what the data
# hasn't is refused, every missing thing named (as the game refuses it, GM2-P18).
# Run inside Blender 5.2, headless:
#   make-blender-references.sh (the references: Tests/Statventure.Tests/Reference/assembly-looks.json, written by the game's
#     tests, AssemblyTests, into Reference/assembly-scenes.json, then rendered into Golden/r3d-assembly-*.png):
#     blender -b --factory-startup --python assemble.py -- --list <assembly-looks.json> --scenes <scene file>
#   make-humanoid.sh assemble (a look's pictures, for approvals):
#     blender -b --factory-startup --python assemble.py -- --look-hero <look.json> --data <folder> [--data <folder>]...
#       [--models <folder>] [--background #rrggbb] --work <folder> --name <stem> [render.py's --views, --size, --scale,
#       --elevation, --focus, --extent, --margin, --perspective, --clip, --time, --clips]
#       [--garment models/<path>]... [--item models/<path>@<socket>]... [--gear-colour <slot>=#rrggbb]...
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.dont_write_bytecode = True  # no __pycache__ in the repo
sys.path.insert(0, HERE)

# The maker's categories in its order (Game/Statventure.Game.Data/Looks/LookPart.cs, the [Choices] of its category): the
# figure's parts in this order, a category that isn't one after them by name, as the game orders them.
CATEGORIES = ["face", "brows", "hair", "beard", "crest", "ears", "tail", "marks", "head-piece"]
STYLE = "render/styles/game"


class Refused(Exception):
    """A look the data can't build: every problem found."""

    def __init__(self, problems):
        super().__init__("the look can't be built: " + "; ".join(problems))
        self.problems = problems


def data_records(folders):
    """Every record's JSON form in the data folders by its data path (its path under the folder without ".json"), a later
    folder's over an earlier's."""
    records = {}
    for folder in folders:
        for root, _, files in os.walk(folder):
            for name in sorted(files):
                if name.endswith(".json"):
                    path = os.path.relpath(os.path.join(root, name), folder)[:-len(".json")].replace(os.sep, "/")
                    records[path] = os.path.join(root, name)
    return records


def record(records, path):
    if path not in records:
        return None
    with open(records[path]) as f:
        return json.load(f)


def model_file(models, path):
    """A model's data path (models/<path>) as its .glb in the models folder, or None when it isn't one."""
    if not path.startswith("models/"):
        return None
    return os.path.join(models, path[len("models/"):] + ".glb")


def resolve(look, records, models, gear=None):
    """What the look comes to: {"body", "garments" (the parts' then the gear's), "items" [(glb, socket)], "palette",
    "parts" [(category, part, model path or None)]}, or Refused with everything it names that isn't there."""
    gear = gear or {}
    problems = []

    def model(path, whose):
        glb = model_file(models, path)
        if glb is None or not os.path.isfile(glb):
            problems.append(f"{whose}: no model \"{path}\"")
        return glb

    body = record(records, f"data/looks/bodies/{look['body']}")
    body_glb = None
    if body is None:
        problems.append(f"no look body \"{look['body']}\" (data/looks/bodies/{look['body']})")
    else:
        body_glb = model(body["model"], f"the body \"{look['body']}\"")
    order = {c: i for i, c in enumerate(CATEGORIES)}
    parts = []
    for category, name in sorted(look.get("parts", {}).items(), key=lambda p: (order.get(p[0], len(CATEGORIES)), p[0])):
        part = record(records, f"data/looks/parts/{name}")
        if part is None:
            problems.append(f"no look part \"{name}\" (data/looks/parts/{name})")
            continue
        if not part.get("models"):
            parts.append((category, name, None))
            continue
        fitted = next((m for m in part["models"] if look["body"] in m["bodies"]), None)
        if fitted is None:
            # Without the body its fit is moot: the body is named missing already.
            if body is not None:
                problems.append(f"\"{name}\" has no model for the body \"{look['body']}\"")
            continue
        model(fitted["model"], f"\"{name}\"")
        parts.append((category, name, fitted["model"]))
    palette = {}
    if look.get("colours"):
        race = record(records, f"data/races/{look['race']}")
        if race is None:
            problems.append(f"no race \"{look['race']}\" (data/races/{look['race']}) for the colours")
        else:
            for slot, name in sorted(look["colours"].items()):
                swatch = next((s for s in race.get("swatches", []) if s["slot"] == slot and s["name"] == name), None)
                if swatch is None:
                    problems.append(f"the {look['race']} has no {slot} colour \"{name}\"")
                else:
                    palette[slot] = swatch["colour"]
    garments = [model_file(models, p) for _, _, p in parts if p is not None]
    for path in gear.get("garments", []):
        garments.append(model(path, "a garment"))
    items = [(model(item["model"], f"an item on {item['socket']}"), item["socket"]) for item in gear.get("items", [])]
    for slot, colour in sorted(gear.get("colours", {}).items()):
        palette[slot] = colour
    if problems:
        raise Refused(problems)
    return {"body": body_glb, "garments": garments, "items": items, "palette": palette, "parts": parts}


def style_of(records):
    """The data's render style (render/styles/game) as a look's style and sun (the same names, render.style's fields)."""
    style = record(records, STYLE)
    if style is None:
        raise Refused([f"no render style \"{STYLE}\""])
    style.pop("$types", None)
    sun = style.pop("sun")
    return style, sun


def scene_of(name, look, gear, records, models, background, view, size, pose_options):
    """One scene of render_scenes.py's format: the assembled figure in the data's style, posed (a clip at a time, or at
    rest), turned for the view and framed at rest as render.py frames a figure (the sandbox's figure mode alike)."""
    import render
    assembled = resolve(look, records, models, gear)
    style, sun = style_of(records)
    options = {"models": [assembled["body"]], "garments": assembled["garments"],
               "items": [f"{glb}@{socket}" for glb, socket in assembled["items"]],
               "size": size, "scale": 1.0, "elevation": 5.0, "margin": 0.06}
    options.update({k: v for k, v in pose_options.items() if v is not None})
    pose = render.pose_at(assembled["body"], options)
    figure = render.figure_models(options, pose)
    points = render.figure_points(render.figure_models(options, render.rest_pose(assembled["body"])))
    focus = render.bone_point(assembled["body"], options.get("focus") or "head")
    the_look = {"background": background, "style": style, "sun": sun, "palette": assembled["palette"], "shadows": {}}
    spec = render.view_spec(options, name, view, the_look, figure, points, focus)
    spec["name"] = name
    return spec


def from_list(list_path, out_path):
    """Every scene of an assembly list (AssemblyTests' Reference/assembly-looks.json) into a scene file at out_path, its
    models by their paths from the models folder, named relative to the file's folder."""
    base =os.path.dirname(os.path.abspath(list_path))
    with open(list_path) as f:
        listed = json.load(f)
    if listed.get("format") != "statventure.assembly" or listed.get("version") != 1:
        raise SystemExit(f"{list_path}: not a statventure.assembly file of version 1")
    folders = [os.path.normpath(os.path.join(base, folder)) for folder in listed["data"]]
    models = os.path.normpath(os.path.join(base, listed["models"]))
    records = data_records(folders)
    scenes = []
    for item in listed["scenes"]:
        pose = {"clip": item.get("clip"), "time": item.get("time"),
                "clips": os.path.normpath(os.path.join(base, item["clips"])) if item.get("clips") else None}
        scenes.append(scene_of(item["name"], item["look"], item.get("gear"), records, models, listed["background"], item["view"],
                               f"{item['width']}x{item['height']}", pose))
    out_dir = os.path.dirname(os.path.abspath(out_path))
    for spec in scenes:
        for model in spec["models"]:
            model["file"] = os.path.relpath(model["file"], models).replace(os.sep, "/")
    with open(out_path, "w") as f:
        json.dump({"modelFolder": os.path.relpath(models, out_dir).replace(os.sep, "/"), "terrainFolder": ".", "shapes": {}, "scenes": scenes},
                  f, indent=2)
        f.write("\n")
    print("wrote", out_path, len(scenes), "scenes")


def arguments(argv):
    options = {"data": [], "garments": [], "items": [], "gear-colours": [], "views": "three-quarter", "size": "600x800",
               "background": None, "models": os.path.normpath(os.path.join(HERE, "..", "..", "..", "Assets", "Models"))}
    i = 0
    while i < len(argv):
        key, value = argv[i], argv[i + 1] if i + 1 < len(argv) else None
        if value is None:
            raise SystemExit(f"{key} needs a value")
        name = key.lstrip("-")
        if name == "data":
            options["data"].append(value)
        elif name == "garment":
            options["garments"].append(value)
        elif name == "item":
            options["items"].append(value)
        elif name == "gear-colour":
            options["gear-colours"].append(value)
        elif name in ("list", "scenes", "look-hero", "models", "background", "work", "name", "views", "size", "focus", "clip", "clips"):
            options[name] = value
        elif name in ("scale", "elevation", "extent", "margin", "perspective", "time"):
            options[name] = float(value)
        else:
            raise SystemExit(f"assemble.py: unknown option {key}")
        i += 2
    return options


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    options = arguments(argv)
    if options.get("list"):
        if not options.get("scenes"):
            raise SystemExit("assemble.py: --list needs --scenes <scene file>")
        from_list(options["list"], options["scenes"])
        return
    if not (options.get("look-hero") and options["data"] and options.get("work") and options.get("name")):
        raise SystemExit("assemble.py: --look-hero, --data, --work and --name are needed (or --list and --scenes)")
    import render
    import render_scenes
    with open(options["look-hero"]) as f:
        look = json.load(f)
    records = data_records(options["data"])
    gear = {"garments": options["garments"],
            "items": [{"model": spec.partition("@")[0], "socket": spec.partition("@")[2]} for spec in options["items"]],
            "colours": dict(entry.split("=", 1) for entry in options["gear-colours"])}
    background = options["background"] or "#2a1f1a"
    pose = {k: options.get(k) for k in ("clip", "time", "clips", "focus", "extent", "perspective")}
    for k in ("scale", "elevation", "margin"):
        if k in options:
            pose[k] = options[k]
    work = os.path.abspath(options["work"])
    os.makedirs(work, exist_ok=True)
    scenes = [scene_of(f"{options['name']}-{view.strip()}", look, gear, records, options["models"], background, view.strip(),
                       options["size"], pose) for view in options["views"].split(",") if view.strip()]
    path = os.path.join(work, options["name"] + "-scenes.json")
    for spec in scenes:
        for model in spec["models"]:
            model["file"] = os.path.relpath(model["file"], work)
    with open(path, "w") as f:
        json.dump({"modelFolder": ".", "terrainFolder": ".", "shapes": {}, "scenes": scenes}, f, indent=2)
    print("wrote", path)
    sys.argv = [sys.argv[0], "--", path, work, os.path.join(work, "glow")]
    render_scenes.main()


if __name__ == "__main__":
    try:
        main()
    except Refused as refused:
        raise SystemExit(str(refused))

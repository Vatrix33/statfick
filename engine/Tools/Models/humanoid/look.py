# A look's numbers (GM2-P9, GM2-P11; Docs/Game/Hero.md, "The new look", "How it works", step 3a): everything a
# candidate look can set within today's renderer rules (Q7: today's rules, any numbers), read from the engine's code:
#   - "style": every number of the engine's CelStyle (Engine/Statventure/Render3D/Renderer3D.cs), by its property's name
#     in camelCase, as the scene files give them to Tools/BlenderReference/render_scenes.py: the ink (sRGB), the shadow
#     mix and band edge, the two line widths (pixels at device scale 1) and the crease angle, the hatching's spacing,
#     width, angle and strength, the glow's three numbers;
#   - "sun": the view's one sun (SunLight: the direction toward it in glTF's world, Y up; its strength as a Blender
#     sun's; its angle in degrees);
#   - "background": the view's background (sRGB);
#   - "palette": colours by palette slot (sRGB), and "shadows": a slot's own shadow tone where it isn't the style's mix
#     (Render3D/Palette.cs); a slot the palette doesn't name shows its material's base colour.
# One look file (JSON, written by `write`) drives both sides: Blender's renders (render.py, through render_scenes.py)
# and the engine's view (the sandbox's figure mode, --look). TODAY is the engine's look as it is: CelStyle.Default and
# the sun of the lizardman test its numbers were measured under (Decisions for M6; Tests/.../Render3D/Scenes3D.cs,
# "Lizardman"), unchanged; the palette empty; the engine's tests' default. EMBER_HEARTH is the game's look, the user's
# choice ("Ember, hearth").
#   python3 look.py list
#   python3 look.py write <name> [<file>]     (default: looks/<name>.json beside this file)
#   python3 look.py check                     (every look in LOOKS valid and its looks/<name>.json as written)
# Plain Python: Blender's scripts and the system's Python import it alike.
import json
import os
import sys

FORMAT = "statventure.look"
VERSION = 1

# The palette slot names the humanoid family's parts use (GM2-P7's list; the parts' steps add theirs here, one list both
# the scripts and the game read), and the gear's (GM3's slots, distinct from the figure's: one actor palette for figure
# and gear). A look's palette and shadows may name only these (a typo is refused). Step 4's: the eyes' whites, the
# outfit's leather (boots, belt) and the belt's buckle; the sample gear's metals (base, trim, the shade of its drawn
# lines), the shield's wood and the sword's grip.
SLOTS = [
    "skin", "hair", "hair.tie", "eyes", "brows", "lips", "cloth", "cloth.trim", "trousers", "crest", "marking", "claws",
    "eye.white", "leather", "buckle",
]
GEAR_SLOTS = ["metal.base", "metal.trim", "metal.shade", "wood", "grip"]
# The race emblems' six (GM2 step 10, the manager's GM2-P31): a medallion's rim and beads, its field, the motif's light
# raised parts, its metal ones, its accent and its cut-in inlays; each filled from the race's accent colour by one rule
# (emblems/palette.py; the game's Figures/Emblems.cs), not by a look or a hero's swatches.
EMBLEM_SLOTS = ["emblem.rim", "emblem.field", "emblem.high", "emblem.metal", "emblem.accent", "emblem.cut"]

# Materials of a fixed colour in every look (sRGB; no slot, so no look can recolour them): the pupils and the eyes'
# glints (unlit).
FIXED = {"pupil": "#060404", "glint": "#efe6d0"}

# The style's numbers, in the order of CelStyle's properties (the tests check this list against the engine's).
STYLE_KEYS = [
    "ink", "shadowMix", "bandEdge", "outlineWidth", "innerWidth", "creaseAngle",
    "hatchSpacing", "hatchWidth", "hatchAngle", "hatchInk", "glowWide", "glowCore", "glowBoost",
]
SUN_KEYS = ["direction", "strength", "angle"]
LOOK_KEYS = ["format", "version", "name", "background", "sun", "style", "palette", "shadows"]

TODAY = {
    "name": "today",
    # The engine's reference scenes' ground (Scenes3D: "#b8b0a0"); a candidate sets its own.
    "background": "#b8b0a0",
    # The lizardman test's sun (Scenes3D.Lizardman): from the upper right on the viewer's side, strength 3; the angle
    # SunLight's default.
    "sun": {"direction": [1, 1.1, 0.7], "strength": 3, "angle": 3},
    # CelStyle.Default.
    "style": {
        "ink": "#130e0b", "shadowMix": 0.42, "bandEdge": 0.3,
        "outlineWidth": 3.5, "innerWidth": 2, "creaseAngle": 120,
        "hatchSpacing": 6, "hatchWidth": 1, "hatchAngle": 72, "hatchInk": 0.35,
        "glowWide": 8, "glowCore": 2.5, "glowBoost": 2.2,
    },
    "palette": {},
    "shadows": {},
}

# The game's look, "Ember, hearth" (the user's choice, 2026-11-15: candidate 1 of three within the mood board's F; GM2-3,
# GM2-8), its numbers as the manager wrote them (look-ember.json; the crease angle and the glow, which it doesn't set,
# today's): a dark warm ground, the sun from the viewer's left and front, a near-black ink, shadows mixed 0.58 toward
# the ink, the band at 0.36, hatching 5 px apart. Its palette is the look's default colours (a hero's own come from its
# race's swatches). The manager's renders lit each view by a sun of its own (Docs/Game/Hero.md, "How it works", step
# 4a); the look file holds one.
EMBER_HEARTH = {
    "name": "ember-hearth",
    "background": "#2a1f1a",
    "sun": {"direction": [-0.8, 0.6, 0.75], "strength": 3, "angle": 3},
    "style": {
        "ink": "#080605", "shadowMix": 0.58, "bandEdge": 0.36,
        "outlineWidth": 3.0, "innerWidth": 1.8, "creaseAngle": 120,
        "hatchSpacing": 5, "hatchWidth": 1.2, "hatchAngle": 72, "hatchInk": 0.5,
        "glowWide": 8, "glowCore": 2.5, "glowBoost": 2.2,
    },
    "palette": {
        "skin": "#aa7a5a", "lips": "#7c4238", "hair": "#3a2416", "brows": "#130c08", "eye.white": "#d2c7b2", "eyes": "#0a0706",
        "cloth": "#74211d", "cloth.trim": "#bb8d3c", "trousers": "#292320", "leather": "#3a261b", "buckle": "#bb8d3c",
        "metal.base": "#80848c", "metal.trim": "#bb8d3c", "metal.shade": "#3a3c42", "wood": "#5a2a1e", "grip": "#3a261b",
    },
    "shadows": {},
}

# Every look by name, each written as looks/<name>.json. Candidates are added here or written as files of their own.
LOOKS = {"today": TODAY, "ember-hearth": EMBER_HEARTH}

HERE = os.path.dirname(os.path.abspath(__file__))
LOOK_FOLDER = os.path.join(HERE, "looks")


def _colour(value, where):
    if not (isinstance(value, str) and len(value) == 7 and value[0] == "#" and all(c in "0123456789abcdefABCDEF" for c in value[1:])):
        raise ValueError(f"{where}: {value!r} is not an sRGB colour #rrggbb")
    return value.lower()


def _number(value, where):
    if isinstance(value, bool) or not isinstance(value, (int, float)) or value != value or value in (float("inf"), float("-inf")):
        raise ValueError(f"{where}: {value!r} is not a number")
    return value


def _keys(value, keys, where):
    if not isinstance(value, dict):
        raise ValueError(f"{where}: not an object")
    missing = [k for k in keys if k not in value]
    unknown = [k for k in value if k not in keys]
    if missing or unknown:
        raise ValueError(f"{where}: " + "; ".join(([f"missing {', '.join(missing)}"] if missing else [])
                                               + ([f"unknown {', '.join(unknown)}"] if unknown else [])))


def validate(look):
    """The look as the file holds it (every key, nothing else, each value of its kind); raises ValueError naming the
    first problem. The engine's reader (the sandbox's FigureLook) refuses the same, the slot names apart (this list)."""
    full = dict(look, format=FORMAT, version=VERSION)
    _keys(full, LOOK_KEYS, "look")
    if not isinstance(full["name"], str) or not full["name"]:
        raise ValueError("look: name must be a text")
    _colour(full["background"], "background")
    _keys(full["sun"], SUN_KEYS, "sun")
    direction = full["sun"]["direction"]
    if not (isinstance(direction, list) and len(direction) == 3 and all(_number(v, "sun.direction") is not None for v in direction)) \
            or not any(direction):
        raise ValueError("sun.direction: three numbers, not all 0")
    if _number(full["sun"]["strength"], "sun.strength") < 0 or _number(full["sun"]["angle"], "sun.angle") < 0:
        raise ValueError("sun: strength and angle are 0 or more")
    _keys(full["style"], STYLE_KEYS, "style")
    for key in STYLE_KEYS:
        if key == "ink":
            _colour(full["style"][key], "style.ink")
        else:
            _number(full["style"][key], f"style.{key}")
    for part in ("palette", "shadows"):
        if not isinstance(full[part], dict):
            raise ValueError(f"{part}: not an object")
        for slot, colour in full[part].items():
            if slot not in SLOTS + GEAR_SLOTS + EMBLEM_SLOTS:
                raise ValueError(f"{part}: {slot!r} is not a palette slot (look.py SLOTS, GEAR_SLOTS, EMBLEM_SLOTS)")
            _colour(colour, f"{part}.{slot}")
    return full


def text(look):
    """The look file's text: its keys in a fixed order, so a look written twice is the same bytes."""
    full = validate(look)
    ordered = {k: full[k] for k in LOOK_KEYS}
    ordered["sun"] = {k: full["sun"][k] for k in SUN_KEYS}
    ordered["style"] = {k: full["style"][k] for k in STYLE_KEYS}
    ordered["palette"] = dict(sorted(full["palette"].items()))
    ordered["shadows"] = dict(sorted(full["shadows"].items()))
    return json.dumps(ordered, indent=2) + "\n"


def write(look, path):
    os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
    with open(path, "w") as f:
        f.write(text(look))
    return path


def load(name_or_path):
    """A look by its name in LOOKS, from a look file, or by the name of a file in looks/ (as the figure mode finds one)."""
    if name_or_path in LOOKS:
        return validate(LOOKS[name_or_path])
    if not os.path.exists(name_or_path) and os.path.exists(os.path.join(LOOK_FOLDER, name_or_path + ".json")):
        name_or_path = os.path.join(LOOK_FOLDER, name_or_path + ".json")
    with open(name_or_path) as f:
        look = json.load(f)
    if look.get("format") != FORMAT or look.get("version") != VERSION:
        raise ValueError(f"{name_or_path}: not a {FORMAT} file of version {VERSION}")
    return validate(look)


def check():
    """Every look in LOOKS is valid, named as its key, and its looks/<name>.json is what write gives it (the files the
    engine's tests and the sandbox read)."""
    problems = []
    for name, look in LOOKS.items():
        try:
            written = text(look)
        except ValueError as error:
            problems.append(f"{name}: {error}")
            continue
        if look["name"] != name:
            problems.append(f"{name}: named {look['name']!r}")
        path = os.path.join(LOOK_FOLDER, name + ".json")
        if not os.path.exists(path) or open(path).read() != written:
            problems.append(f"{path} is not what look.py writes for the look {name}: python3 look.py write {name}")
    return problems


def main(argv):
    if not argv or argv[0] == "list":
        for name in LOOKS:
            print(name)
        return 0
    if argv[0] == "write" and len(argv) in (2, 3):
        path = argv[2] if len(argv) == 3 else os.path.join(LOOK_FOLDER, argv[1] + ".json")
        print("wrote", write(load(argv[1]), path))
        return 0
    if argv[0] == "check":
        problems = check()
        for problem in problems:
            print(problem, file=sys.stderr)
        print("looks: " + ("ok" if not problems else f"{len(problems)} problems"))
        return 1 if problems else 0
    print("usage: look.py list | write <name or file> [<file>] | check", file=sys.stderr)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

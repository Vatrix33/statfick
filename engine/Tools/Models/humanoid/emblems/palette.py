#!/usr/bin/env python3
# The emblems' colours (the manager's GM2-P31, 2026-11-20; GM2 step 10): an emblem's six palette slots (look.EMBLEM_SLOTS)
# filled from its race's accent colour (the race record's `accent`) by one rule: the rim and the motif's metal the look's
# gold, the motif's light parts the bone, the field the accent mixed 58 % toward the ink, the accent parts the accent mixed
# 18 % toward the bone, the cut-in inlays the accent mixed 90 % toward the ink. A mix is per sRGB channel (0-255),
# a + (b - a) * t rounded half to even (Python's round, as the approval renders were coloured). The game's rule is the
# same, written once there (Game/Statventure.Game/Figures/Emblems.cs; the game's tests hold the two alike, EmblemPaletteTests,
# reading this file's GOLD, BONE, INK and RULE).
# Plain Python:
#   python3 palette.py <#rrggbb>                   the six slots' colours for that accent (JSON)
#   python3 palette.py check [<scenes.json>] [<races folder>]
#        every scene of the emblems' scene file (default: Tests/Statventure.Tests/Reference/emblem-scenes.json, written by the
#        game's tests) coloured by this rule from its race's accent (default: Assets/Data/data/races/): the game's rule and
#        this one agree on the nine races.
import json
import os
import sys

GOLD = "#bb8d3c"
BONE = "#e1d3ad"
INK = "#1a120d"

# Each slot: (what it is mixed from, toward what, how far); None: the colour itself.
RULE = {
    "emblem.rim": (GOLD, None, 0.0),
    "emblem.field": ("accent", INK, 0.58),
    "emblem.high": (BONE, None, 0.0),
    "emblem.metal": (GOLD, None, 0.0),
    "emblem.accent": ("accent", BONE, 0.18),
    "emblem.cut": ("accent", INK, 0.90),
}

HERE = os.path.dirname(os.path.abspath(__file__))
ENGINE = os.path.normpath(os.path.join(HERE, "..", "..", "..", ".."))


def rgb(text):
    if not (isinstance(text, str) and len(text) == 7 and text[0] == "#" and all(c in "0123456789abcdefABCDEF" for c in text[1:])):
        raise ValueError(f"{text!r} is not a colour #rrggbb")
    return tuple(int(text[i:i + 2], 16) for i in (1, 3, 5))


def hexa(channels):
    return "#%02x%02x%02x" % tuple(channels)


def mix(a, b, t):
    """a mixed toward b by t, per sRGB channel, rounded half to even."""
    return tuple(round(p + (q - p) * t) for p, q in zip(a, b))


def colours(accent):
    """The six slots' colours (sRGB "#rrggbb") of an emblem whose race's accent is `accent`."""
    a = rgb(accent)
    out = {}
    for slot, (base, toward, amount) in RULE.items():
        source = a if base == "accent" else rgb(base)
        out[slot] = hexa(source if toward is None else mix(source, rgb(toward), amount))
    return out


def check(scenes_path, races):
    """Every emblem scene's palette is this rule's for its race's accent; returns the problems."""
    accents = {}
    for name in sorted(os.listdir(races)):
        with open(os.path.join(races, name)) as f:
            race = json.load(f)
        if race.get("emblem"):
            accents[race["emblem"]] = race.get("accent", "#c7683f")
    with open(scenes_path) as f:
        scenes = json.load(f)["scenes"]
    problems = []
    seen = set()
    for scene in scenes:
        emblem = scene["models"][0]["file"].rsplit("/", 1)[-1].removesuffix(".glb")
        seen.add(emblem)
        if emblem not in accents:
            problems.append(f"{scene['name']}: no race has the emblem {emblem}")
        elif scene["palette"] != colours(accents[emblem]):
            problems.append(f"{scene['name']}: its palette {scene['palette']} isn't the rule's {colours(accents[emblem])}")
    for emblem in sorted(set(accents) - seen):
        problems.append(f"the emblem {emblem} has no scene")
    return problems


def main(argv):
    if len(argv) == 1 and argv[0].startswith("#"):
        print(json.dumps(colours(argv[0]), indent=2))
        return 0
    if argv[:1] == ["check"] and len(argv) <= 3:
        scenes = argv[1] if len(argv) > 1 else os.path.join(ENGINE, "Tests", "Statventure.Tests", "Reference", "emblem-scenes.json")
        races = argv[2] if len(argv) > 2 else os.path.join(ENGINE, "Assets", "Data", "data", "races")
        problems = check(scenes, races)
        for problem in problems:
            print(problem, file=sys.stderr)
        print("emblem palettes: " + ("the rule's" if not problems else f"{len(problems)} problems"))
        return 1 if problems else 0
    print("usage: palette.py <#rrggbb> | check [<scenes.json>] [<races folder>]", file=sys.stderr)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

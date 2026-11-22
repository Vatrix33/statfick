#!/usr/bin/env python3
# chosen.py : the nine motifs the user chose (Q66, 2026-11-20: one of two sketches per race, motifs.py) as data: chosen.json
# beside this file, {race: {"accent", "motif", "name", "elements": [{"points": [[x, y], ...], "tone", "outline"}]}}, the
# outlines in the unit field (sketchkit.py: x to the right, y up, the field's edge at radius 1), each point rounded to five
# decimals. The emblems' models are raised from this file (medallion.py); this script is kept for the record and for
# redrawing a motif (the manager's, GM2 step 10; the file as the repo holds it: one element a line, the same values).
# Plain Python; motifs.py's drawing kit needs Pillow (sketchkit.py draws the sketches).
#   python3 chosen.py            writes chosen.json
#   python3 chosen.py check      proves chosen.json is what it writes
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.dont_write_bytecode = True

CHOSEN = {"human": "A", "elf": "B", "dark-elf": "B", "dwarf": "A", "hobbit": "A", "lizardman": "B", "half-beast": "A", "goliath": "A", "merfolk": "A"}
ACCENT = {"human": "#c7683f", "elf": "#728577", "dark-elf": "#77607e", "dwarf": "#8b684e", "hobbit": "#858b6c", "lizardman": "#66806a", "half-beast": "#9a6a3f",
          "goliath": "#7d7a72", "merfolk": "#4c9a8a"}


def chosen():
    import motifs
    out = {}
    for race, key, name, why, elements in motifs.M:
        if CHOSEN[race] == key:
            out[race] = {"accent": ACCENT[race], "motif": key, "name": name,
                         "elements": [{"points": [[round(x, 5), round(y, 5)] for x, y in e["points"]], "tone": e["tone"], "outline": e["outline"]} for e in elements]}
    assert len(out) == 9
    return {race: out[race] for race in CHOSEN}


def text(data):
    """The file's text: a race a block, an element a line (json.dumps' values, so the numbers read back as written)."""
    lines = ["{"]
    for r, (race, spec) in enumerate(data.items()):
        lines.append(f"  {json.dumps(race)}: {{\"accent\": {json.dumps(spec['accent'])}, \"motif\": {json.dumps(spec['motif'])}, \"name\": {json.dumps(spec['name'])}, \"elements\": [")
        for e, element in enumerate(spec["elements"]):
            lines.append("    " + json.dumps(element, separators=(", ", ": ")) + ("," if e + 1 < len(spec["elements"]) else ""))
        lines.append("  ]}" + ("," if r + 1 < len(data) else ""))
    lines.append("}")
    return "\n".join(lines) + "\n"


if __name__ == "__main__":
    path = os.path.join(HERE, "chosen.json")
    written = text(chosen())
    if sys.argv[1:] == ["check"]:
        same = os.path.exists(path) and open(path).read() == written
        print("chosen.json: " + ("as chosen.py writes it" if same else "NOT what chosen.py writes"))
        sys.exit(0 if same else 1)
    with open(path, "w") as f:
        f.write(written)
    print("wrote", path)

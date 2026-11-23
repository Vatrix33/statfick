# The keying sources of the family's clips, and the record that ties them to the clips' data (GM2-P23; Docs/Game/Hero.md,
# "How it works", "The keying kit"). Every clip module (MODULES) gives its clips' sources (poses.source); this writes them,
# one JSON file a clip, for bake.py to key on the figure. keyed.json beside it records each clip's source and baked data by
# their sha256, so the kit can't drift from the data: `check` (plain Python, no Blender) proves the sources the modules
# write now are the ones the committed clips were baked from and the committed clips are the ones baked; make-humanoid.sh
# keys --check bakes them again in Blender and compares byte for byte; the engine's tests hold the committed clips to it
# (HumanoidKeyingKitTests). Plain Python, run by Tools/Scripts/make-humanoid.sh:
#   python3 -I sources.py list                                    every clip's name, in the modules' order
#   python3 -I sources.py write <folder> [clip ...]               the sources (all by default) as <folder>/<clip>.json
#   python3 -I sources.py record <sources> <clips> [clip ...]     keyed.json's entries for those clips from the sources and
#                                                                 the clips baked from them (after keys bakes into clips/)
#   python3 -I sources.py compare <sources> <baked> [clip ...]    each source's and baked clip's hash keyed.json's, and each
#                                                                 baked clip the committed one byte for byte (keys --check)
#   python3 -I sources.py check                                   keyed.json against the sources written now and the
#                                                                 committed clips (no Blender)
import hashlib
import importlib
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.dont_write_bytecode = True  # no __pycache__ in the repo

CLIPS = os.path.dirname(HERE)                 # the family's clips' data, clips/<name>.json
RECORD = os.path.join(HERE, "keyed.json")
# The clip modules, each a family of the clip table (Hero.md, step 4b): sources() gives their sources in order.
MODULES = ["standing", "moving", "reacting", "melee", "acting"]


def every():
    """Every clip's source, {name: source}, in the modules' order."""
    out = {}
    for module in MODULES:
        for spec in importlib.import_module(module).sources():
            if spec["name"] in out:
                raise SystemExit(f"{module}: a second clip named {spec['name']}")
            out[spec["name"]] = spec
    return out


def text(spec):
    """A source's or a clip's file as the kit writes it (bake.py's clips the same way: JSON, one space a level)."""
    return json.dumps(spec, indent=1)


def sha(data):
    return hashlib.sha256(data.encode() if isinstance(data, str) else data).hexdigest()


def chosen(names, wanted):
    unknown = [name for name in wanted if name not in names]
    if unknown:
        raise SystemExit(f"no clip {', '.join(unknown)} in the kit (its clips: {', '.join(names)})")
    return list(wanted) or list(names)


def read_record():
    with open(RECORD) as f:
        return json.load(f)["clips"]


def write_record(clips):
    record = {"comment": "Each clip's keying source (sources.py write) and its data (clips/<name>.json, baked by bake.py) by their sha256: "
                         "written by make-humanoid.sh keys, held by sources.py check, keys --check and the engine's HumanoidKeyingKitTests "
                         "(Docs/Game/Hero.md, \"How it works\", \"The keying kit\").",
              "clips": {name: clips[name] for name in sorted(clips)}}
    with open(RECORD, "w") as f:
        f.write(json.dumps(record, indent=1) + "\n")


def main(argv):
    command = argv[0] if argv else ""
    if command == "list":
        print("\n".join(every()))
    elif command == "write" and len(argv) >= 2:
        specs = every()
        os.makedirs(argv[1], exist_ok=True)
        for name in chosen(list(specs), argv[2:]):
            with open(os.path.join(argv[1], name + ".json"), "w") as f:
                f.write(text(specs[name]))
        print(f"wrote {len(chosen(list(specs), argv[2:]))} sources into {argv[1]}")
    elif command == "record" and len(argv) >= 3:
        record = read_record() if os.path.exists(RECORD) else {}
        for name in chosen(list(every()), argv[3:]):
            with open(os.path.join(argv[1], name + ".json"), "rb") as f:
                source = sha(f.read())
            with open(os.path.join(argv[2], name + ".json"), "rb") as f:
                clip = sha(f.read())
            record[name] = {"source": source, "clip": clip}
        write_record(record)
        print(f"recorded {len(argv[3:]) or len(record)} clips in {RECORD}")
    elif command == "compare" and len(argv) >= 3:
        record = read_record()
        problems, names = [], chosen(list(every()), argv[3:])
        for name in names:
            with open(os.path.join(argv[1], name + ".json"), "rb") as f:
                source = sha(f.read())
            with open(os.path.join(argv[2], name + ".json"), "rb") as f:
                baked = f.read()
            committed = os.path.join(CLIPS, name + ".json")
            if name not in record:
                problems.append(f"{name}: not in keyed.json")
                continue
            if source != record[name]["source"]:
                problems.append(f"{name}: its source differs from the one its data was baked from")
            if not os.path.exists(committed):
                problems.append(f"{name}: no committed clips/{name}.json")
            elif baked != open(committed, "rb").read():
                problems.append(f"{name}: baked again it differs from the committed clips/{name}.json")
            elif sha(baked) != record[name]["clip"]:
                problems.append(f"{name}: the committed clip isn't keyed.json's")
            else:
                print(f"same {name}")
        for problem in problems:
            print("Error:", problem)
        print(f"compared {len(names)} clips: " + ("all the same" if not problems else f"{len(problems)} problems"))
        sys.exit(1 if problems else 0)
    elif command == "check":
        record, specs = read_record(), every()
        problems = []
        committed = sorted(os.path.splitext(name)[0] for name in os.listdir(CLIPS) if name.endswith(".json"))
        if sorted(specs) != committed:
            problems.append(f"the kit's clips {sorted(specs)} aren't the committed clips {committed}")
        if sorted(record) != sorted(specs):
            problems.append(f"keyed.json's clips {sorted(record)} aren't the kit's {sorted(specs)}")
        for name, spec in specs.items():
            if name not in record:
                continue
            if sha(text(spec)) != record[name]["source"]:
                problems.append(f"{name}: its source differs from the one its data was baked from (make-humanoid.sh keys {name})")
            path = os.path.join(CLIPS, name + ".json")
            if os.path.exists(path) and sha(open(path, "rb").read()) != record[name]["clip"]:
                problems.append(f"{name}: clips/{name}.json isn't the one baked from its source")
        for problem in problems:
            print("Error:", problem)
        print(f"checked {len(specs)} sources: " + ("all as keyed" if not problems else f"{len(problems)} problems"))
        sys.exit(1 if problems else 0)
    else:
        raise SystemExit("usage: sources.py list | write <folder> [clip ...] | record <sources> <clips> [clip ...] | compare <sources> <baked> [clip ...] | check")


if __name__ == "__main__":
    main(sys.argv[1:])

# Builds the humanoid family's models (GM2-P9, GP-4: every model regenerable from its script): each model the manifest
# names (manifest.json beside this file), by its builder, exported as .glb where the manifest puts it (the engine's test
# models' way: make_models.export, the triangles in their canonical order, so an unchanged model is the same bytes).
# Run inside Blender 5.2, headless, by Tools/Scripts/make-humanoid.sh:
#   blender -b --factory-startup --python build.py -- <engine folder> [--check <scratch folder>] [name ...]
# --check builds into the scratch folder instead and proves each result equals what is committed: byte for byte (the
# export is deterministic: Docs/Game/Hero.md, "How it works", step 3a, measured); a difference is reported with what
# differs (Tools/BlenderReference/compare_references.py's model comparison: the JSON, and the float buffers number by
# number) and fails the run.
import importlib
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.dont_write_bytecode = True  # no __pycache__ in the repo
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.normpath(os.path.join(HERE, "..", "..", "BlenderReference")))

import make_models  # noqa: E402
from compare_references import compare_glb  # noqa: E402


def manifest():
    with open(os.path.join(HERE, "manifest.json")) as f:
        models = json.load(f)["models"]
    names = [m["name"] for m in models]
    if len(set(names)) != len(names):
        raise SystemExit("manifest.json: a model's name twice")
    return models


def builder(spec):
    module, function = spec["builder"].split(":")
    return getattr(importlib.import_module(module), function)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    engine = argv.pop(0)
    scratch = None
    if argv[:1] == ["--check"]:
        scratch = argv[1]
        argv = argv[2:]
    wanted = set(argv)
    models = [m for m in manifest() if not wanted or m["name"] in wanted]
    unknown = wanted - {m["name"] for m in models}
    if unknown:
        raise SystemExit(f"no model {', '.join(sorted(unknown))} in manifest.json")
    problems = []
    for spec in models:
        committed = os.path.join(engine, spec["out"])
        path = os.path.join(scratch, spec["out"]) if scratch else committed
        os.makedirs(os.path.dirname(path), exist_ok=True)
        make_models.reset()
        builder(spec)(path)
        if scratch is None:
            continue
        if not os.path.exists(committed):
            problems.append(f"{spec['name']}: nothing committed at {spec['out']}")
        elif open(path, "rb").read() != open(committed, "rb").read():
            problems.append(f"{spec['name']}: {spec['out']} differs from what the script makes: {compare_glb(path, committed)}")
        else:
            print(f"same {spec['out']}")
    if scratch is not None:
        for problem in problems:
            print("Error:", problem)
        print(f"checked {len(models)} models: " + ("all equal" if not problems else f"{len(problems)} differ"))
        if problems:
            sys.exit(1)


if __name__ == "__main__":
    main()

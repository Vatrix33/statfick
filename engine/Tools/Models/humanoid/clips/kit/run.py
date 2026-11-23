# The keying kit's Blender side (GM2-P23; Docs/Game/Hero.md, "How it works", "The keying kit"): the keying figure built
# once (figure.py), then each clip's source (sources.py write) keyed on it (bake.py). Run inside Blender 5.2, headless,
# with 8 threads (the figure's build follows the count, as every model of the family's), by Tools/Scripts/make-humanoid.sh:
#   blender -b --factory-startup -t 8 --python run.py -- bake <sources> <out folder> [clip ...]
#       each clip's data, <out>/<clip>.json (clips/library.py's form)
#   blender ... --python run.py -- measure <sources> <out.json> [clip ...]
#       each clip's measures (measure.py reads them): every solved arm at every key (solver.py's notes) and the clip
#       against the ground frame by frame, on the figure with his face and hair; with the baked data's sha256
#   blender ... --python run.py -- frames <sources> <out folder> <clip> [--every N] [--hold <stance clip>] [--gear <list>] [--plain]
#       the clip's frames 0, N, 2N ... (and its last) as posed figures, <out>/<clip>-<frame>.glb, with <out>/frames.json
#       (video.py renders them): the figure geared (the chain shirt, what --gear or the source's `gear` puts in the hands;
#       --plain: his plain outfit), --hold laying a stance loop over the clip as the battle will (GM2-P21; the hands then
#       hold the stance's gear)
import hashlib
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
HUMANOID = os.path.normpath(os.path.join(HERE, "..", ".."))
sys.dont_write_bytecode = True  # no __pycache__ in the repo
for folder in (HERE, HUMANOID, os.path.normpath(os.path.join(HUMANOID, "..", "..", "BlenderReference"))):
    sys.path.insert(0, folder)

import make_models  # noqa: E402

import bake  # noqa: E402
import figure  # noqa: E402


def source_of(folder, name):
    with open(os.path.join(folder, name + ".json")) as f:
        return json.load(f)


def names_in(folder, wanted):
    names = sorted(os.path.splitext(n)[0] for n in os.listdir(folder) if n.endswith(".json"))
    unknown = [name for name in wanted if name not in names]
    if unknown:
        raise SystemExit(f"no source {', '.join(unknown)} in {folder}")
    return list(wanted) or names


def text(clip):
    return json.dumps(clip, indent=1)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    command = argv[0] if argv else ""
    make_models.reset()
    if command == "bake":
        sources, out = argv[1], argv[2]
        keyer = bake.Keyer(figure.keying_figure("bare"))
        os.makedirs(out, exist_ok=True)
        for name in names_in(sources, argv[3:]):
            clip = keyer.bake(source_of(sources, name))
            with open(os.path.join(out, name + ".json"), "w") as f:
                f.write(text(clip))
            print(f"wrote {os.path.join(out, name + '.json')}: {len(clip['keys'])} keys, {len(clip['keys'][0]['rotations'])} bones")
    elif command == "measure":
        sources, out = argv[1], argv[2]
        keyer = bake.Keyer(figure.keying_figure("parts"))
        measured = {}
        for name in names_in(sources, argv[3:]):
            notes = []
            clip = keyer.bake(source_of(sources, name), notes)
            measured[name] = {"clip": hashlib.sha256(text(clip).encode()).hexdigest(), "frames": clip["frames"], "loop": clip["loop"],
                              "arms": notes, "ground": keyer.ground(clip)}
        with open(out, "w") as f:
            json.dump(measured, f)
        print(f"wrote {out}: {len(measured)} clips measured")
    elif command == "frames":
        sources, out, name = argv[1], argv[2], argv[3]
        options = {"--every": "2", "--hold": None, "--gear": None}
        rest, plain = argv[4:], False
        while rest:
            if rest[0] == "--plain":
                plain, rest = True, rest[1:]
            elif rest[0] in options and len(rest) > 1:
                options[rest[0]], rest = rest[1], rest[2:]
            else:
                raise SystemExit(f"frames: unknown option {rest[0]}")
        spec = source_of(sources, name)
        keyer = bake.Keyer(figure.keying_figure("geared"))
        clip = keyer.bake(spec)
        holds = spec
        if options["--hold"]:
            holds = source_of(sources, options["--hold"])            # the stance's hands hold its own gear
            clip = bake.held(clip, keyer.bake(holds))
        gear = options["--gear"] if options["--gear"] is not None else holds.get("gear", "sword,shield")
        if plain:
            keyer.figure["skinned"] = [o for o in keyer.figure["skinned"] if "-mail" not in o.name]
        keyer.key_action(clip)
        every = max(1, int(options["--every"]))
        last = clip["frames"]
        frames = sorted(set(range(0, last + 1, every)) | ({last} if not clip["loop"] else set()))
        if clip["loop"] and frames[-1] == last:
            frames.pop()                       # a loop's last frame is its first
        os.makedirs(out, exist_ok=True)
        pictures = bake.Frames(keyer, spec, gear)
        for frame in frames:
            pictures.export(frame, os.path.join(out, f"{name}-{frame:04d}.glb"))
        with open(os.path.join(out, "frames.json"), "w") as f:
            json.dump({"name": name, "frames": frames, "every": every, "length": last, "loop": clip["loop"], "hold": options["--hold"],
                       "gear": gear, "reach": [pictures.low, pictures.high], "ground": keyer.level}, f, indent=1)
        print(f"wrote {len(frames)} frames of {name} into {out}")
    else:
        raise SystemExit("usage: run.py -- bake <sources> <out> [clip ...] | measure <sources> <out.json> [clip ...] | frames <sources> <out> <clip> [options]")


main()

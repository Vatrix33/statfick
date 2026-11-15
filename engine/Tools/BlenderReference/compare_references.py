# Compares what `make-blender-references.sh --out <folder>` made with what is committed in the test
# project (GM2 step 3a: how a Blender build is proven against the references, Docs/Renderer-3D.md,
# "Reference harness"). Every file the harness writes is checked: the test models (Data/Models/*.glb),
# the terrain (Data/Terrain), the renders (Golden/r3d-*.png) and the evaluations (Golden/anim-*,
# nodes-*, stage-*, curve-*.json). Each is byte-equal, or its difference is measured: for a render
# the pixels that differ and by how many levels (the largest channel difference, 0-255), for an
# evaluation the largest difference of any number (absolute, and relative to the number's size), for
# a model which of its parts differ (its JSON and, number by number, its binary buffer as the
# accessors read it). Plain Python with Pillow (no Blender; Tools/Models/humanoid/build.py uses its
# model comparison from Blender's Python, which has no Pillow, so Pillow is imported only for renders):
#   python3 compare_references.py <made folder> [<test project folder>]
# Exit code 0 when every file is byte-equal, 1 when any differs or is missing.
import json
import os
import struct
import sys

KINDS = (
    ("Data/Models", lambda n: n.endswith(".glb")),
    ("Data/Terrain", lambda n: True),
    ("Golden", lambda n: n.startswith("r3d-") and n.endswith(".png")),
    ("Golden", lambda n: n.split("-")[0] in ("anim", "nodes", "stage", "curve") and n.endswith(".json")),
)


def numbers_of(value, path=""):
    """Every number in a JSON value with its path; the rest (strings, structure) as a list of keys."""
    if isinstance(value, bool) or value is None or isinstance(value, str):
        yield path, value
    elif isinstance(value, (int, float)):
        yield path, float(value)
    elif isinstance(value, list):
        for i, v in enumerate(value):
            yield from numbers_of(v, f"{path}[{i}]")
    else:
        for k in sorted(value):
            yield from numbers_of(value[k], f"{path}.{k}")


def compare_json(a, b):
    left, right = dict(numbers_of(a)), dict(numbers_of(b))
    if left.keys() != right.keys():
        return f"structure differs ({len(left.keys() ^ right.keys())} paths only on one side)"
    worst_abs, worst_rel, where, differing = 0.0, 0.0, None, 0
    for key, x in left.items():
        y = right[key]
        if isinstance(x, float) and isinstance(y, float):
            d = abs(x - y)
            if d:
                differing += 1
            if d > worst_abs:
                worst_abs, where = d, key
            scale = max(abs(x), abs(y))
            if scale and d / scale > worst_rel:
                worst_rel = d / scale
        elif x != y:
            return f"value differs at {key}: {x!r} / {y!r}"
    return f"{differing} of {len(left)} values differ; largest {worst_abs:.3g} at {where}, relative {worst_rel:.3g}"


def compare_png(path_a, path_b):
    from PIL import Image
    a, b = Image.open(path_a), Image.open(path_b)
    if a.size != b.size or a.mode != b.mode:
        return f"size or mode differs: {a.size} {a.mode} / {b.size} {b.mode}"
    channels = len(a.getbands())
    pa, pb = a.tobytes(), b.tobytes()
    step = len(pa) // (a.size[0] * a.size[1])
    differing, over2, worst = 0, 0, 0
    for i in range(0, len(pa), step):
        x, y = pa[i:i + step], pb[i:i + step]
        if x != y:
            differing += 1
            d = max(abs(x[c] - y[c]) for c in range(min(channels, step)))
            worst = max(worst, d)
            if d > 2:
                over2 += 1
    if not differing:
        return "same pixels (the file's bytes differ)"
    return f"{differing} of {a.size[0] * a.size[1]} pixels differ, by up to {worst} levels ({over2} by more than 2)"


def glb_parts(path):
    data = open(path, "rb").read()
    length = struct.unpack("<I", data[12:16])[0]
    document = json.loads(data[20:20 + length])
    rest = data[20 + length:]
    binary = rest[8:] if len(rest) >= 8 else b""
    return document, binary


def compare_glb(path_a, path_b):
    (doc_a, bin_a), (doc_b, bin_b) = glb_parts(path_a), glb_parts(path_b)
    notes = []
    if doc_a != doc_b:
        notes.append("JSON: " + compare_json(doc_a, doc_b))
    if bin_a != bin_b:
        if len(bin_a) != len(bin_b):
            notes.append(f"buffer sizes differ: {len(bin_a)} / {len(bin_b)}")
        else:
            worst = 0.0
            for accessor in doc_a.get("accessors", []):
                if accessor.get("componentType") != 5126 or "bufferView" not in accessor:
                    continue
                view = doc_a["bufferViews"][accessor["bufferView"]]
                start = view.get("byteOffset", 0) + accessor.get("byteOffset", 0)
                count = accessor["count"] * {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4, "MAT4": 16}[accessor["type"]]
                xs = struct.unpack_from(f"<{count}f", bin_a, start)
                ys = struct.unpack_from(f"<{count}f", bin_b, start)
                worst = max([worst] + [abs(x - y) for x, y in zip(xs, ys)])
            notes.append(f"buffer differs; floats by up to {worst:.3g}")
    return "; ".join(notes) or "same content (the file's bytes differ)"


def main():
    made = os.path.abspath(sys.argv[1])
    tests = os.path.abspath(sys.argv[2]) if len(sys.argv) > 2 else os.path.normpath(
        os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Tests", "Statventure.Tests"))
    same, different, missing = 0, [], []
    for folder, wanted in KINDS:
        names = set()
        for root in (made, tests):
            where = os.path.join(root, folder)
            if os.path.isdir(where):
                names |= {n for n in os.listdir(where) if wanted(n) and os.path.isfile(os.path.join(where, n))}
        for name in sorted(names):
            a, b = os.path.join(made, folder, name), os.path.join(tests, folder, name)
            label = f"{folder}/{name}"
            if not os.path.exists(a) or not os.path.exists(b):
                missing.append(f"{label}: {'not made' if not os.path.exists(a) else 'not committed'}")
                continue
            if open(a, "rb").read() == open(b, "rb").read():
                same += 1
                continue
            if name.endswith(".png"):
                different.append(f"{label}: {compare_png(a, b)}")
            elif name.endswith(".json"):
                different.append(f"{label}: {compare_json(json.load(open(a)), json.load(open(b)))}")
            elif name.endswith(".glb"):
                different.append(f"{label}: {compare_glb(a, b)}")
            else:
                different.append(f"{label}: bytes differ")
    print(f"{same} byte-equal, {len(different)} different, {len(missing)} missing")
    for line in different + missing:
        print("  " + line)
    sys.exit(0 if not different and not missing else 1)


if __name__ == "__main__":
    main()

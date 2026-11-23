# A clip's frames on a floor, as pictures and a video (GM2-P23; the manager's standing rule: a clip is measured and seen on
# a floor before anyone is shown it; Docs/Game/Hero.md, "How it works", "The keying kit"). From the posed figures run.py
# frames exported: a scene file of every frame from each view (the figure turned under a fixed camera and sun, as
# render.py's views are, and framed alike in every frame by its reach over the whole clip), which make-humanoid.sh renders
# in the game's look (render.py --scene: the references' own Blender side, the ink laid over the shading as the engine
# lays it); then each frame's views side by side with the clip's name and frame, a looping video (ffmpeg) and a strip of
# seven frames from the first view. Plain Python with Pillow, run by Tools/Scripts/make-humanoid.sh clip-video:
#   python3 -I video.py scenes <frames folder> <scenes.json> <views> <WxH>
#   python3 -I video.py tile <frames folder> <renders folder> <out folder> <views>
import json
import math
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.normpath(os.path.join(HERE, "..", "..")))
sys.dont_write_bytecode = True  # no __pycache__ in the repo

import look as looks  # noqa: E402

VIEWS = {"front": 0, "three-quarter": 45, "side": 90, "back": 180}      # render.py's: the figure turned, degrees
LOOK = "ember-hearth"
ELEVATION, MARGIN = 5.0, 0.06               # render.py's: the camera looks down 5 degrees; a margin of 6 % each side


def turn_of(view):
    return VIEWS[view] if view in VIEWS else float(view)


def turned(point, degrees):
    """A point turned about glTF's up (Y) as render.py turns a figure for a view (mathutils' Rotation about Y)."""
    c, s = math.cos(math.radians(degrees)), math.sin(math.radians(degrees))
    x, y, z = point
    return (c * x + s * z, y, -s * x + c * z)


def scenes(frames_folder, path, views, size):
    """The scene file: a scene per frame and view, its model the frame's posed figure, its camera fixed for the view."""
    with open(os.path.join(frames_folder, "frames.json")) as f:
        info = json.load(f)
    look = looks.load(LOOK)
    width, height = (int(v) for v in size.lower().split("x"))
    low, high = info["reach"]
    corners = [(x, y, z) for x in (low[0], high[0]) for y in (min(low[1], info["ground"] - 0.04), high[1]) for z in (low[2], high[2])]
    out = []
    for view in views:
        degrees = turn_of(view)
        points = [turned(p, degrees) for p in corners]
        xs, ys, zs = ([p[i] for p in points] for i in range(3))
        target = [(min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2, (min(zs) + max(zs)) / 2]
        extent = max(max(ys) - min(ys), (max(xs) - min(xs)) / (width / height)) * (1 + 2 * MARGIN)
        reach = max(max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs))
        distance = reach + 5
        e = math.radians(ELEVATION)
        camera = {"near": 0.1, "far": max(50, distance + reach + 10), "up": [0, 1, 0], "target": target, "orthographicHeight": extent,
                  "fieldOfViewDegrees": 40, "projection": "orthographic",
                  "position": [target[0], target[1] + distance * math.sin(e), target[2] + distance * math.cos(e)]}
        c, s = math.cos(math.radians(degrees)), math.sin(math.radians(degrees))
        matrix = [[c, 0, -s, 0], [0, 1, 0, 0], [s, 0, c, 0], [0, 0, 0, 1]]          # columns: the turn about Y
        for frame in info["frames"]:
            out.append({
                "name": f"{info['name']}-{frame:04d}-{view}", "width": width, "height": height, "background": look["background"], "camera": camera,
                "objects": [],
                "models": [{"file": f"{info['name']}-{frame:04d}.glb", "matrix": matrix, "scenery": False, "pose": None, "group": "figure",
                            "anchor": [0, 0, 0], "objects": None, "clipTime": None, "palette": None}],
                "palette": look["palette"], "shadows": look["shadows"], "sun": look["sun"], "style": look["style"], "scale": 1,
                "terrain": None, "time": 0, "particles": []})
    with open(path, "w") as f:
        json.dump({"modelFolder": os.path.relpath(frames_folder, os.path.dirname(path)), "terrainFolder": ".", "shapes": {}, "scenes": out}, f, indent=1)
    print(f"wrote {len(out)} scenes into {path}")


def tile(frames_folder, renders, out, views):
    """Each frame's views side by side, captioned; the video and the strip."""
    from PIL import Image, ImageDraw, ImageFont
    with open(os.path.join(frames_folder, "frames.json")) as f:
        info = json.load(f)
    name = info["name"]
    label = name + (f" under {info['hold']}" if info["hold"] else "")
    folder = os.path.join(out, f"{label.replace(' ', '-')}-frames")
    os.makedirs(folder, exist_ok=True)
    font = ImageFont.load_default(size=16)
    tiles = []
    for index, frame in enumerate(info["frames"]):
        pictures = [Image.open(os.path.join(renders, f"{name}-{name}-{frame:04d}-{view}.png")).convert("RGB") for view in views]
        width, height = pictures[0].size
        picture = Image.new("RGB", (width * len(pictures), height))
        for i, p in enumerate(pictures):
            picture.paste(p, (i * width, 0))
        ImageDraw.Draw(picture).text((10, 8), f"{label}   frame {frame} of {info['length']}   ({', '.join(views)})", font=font, fill="#b8ac98")
        picture.save(os.path.join(folder, f"f{index:04d}.png"))
        tiles.append(pictures[0])
    count = len(info["frames"])
    seconds = count * info["every"] / 60
    loops = max(0, round(4 / seconds) - 1) if info["loop"] else max(0, round(3 / (seconds + 0.5)) - 1)
    video = os.path.join(out, f"{label.replace(' ', '-')}.mp4")
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-stream_loop", str(loops), "-framerate", str(60 / info["every"]),
                    "-i", os.path.join(folder, "f%04d.png"), "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18", video], check=True)
    picks = [round((count - 1) * i / 6) for i in range(7)] if not info["loop"] else [round(count * i / 7) % count for i in range(7)]
    width, height = tiles[0].size
    strip = Image.new("RGB", (width * len(picks), height))
    for i, index in enumerate(picks):
        tile_ = tiles[index].copy()
        ImageDraw.Draw(tile_).text((10, 8), f"{label}  frame {info['frames'][index]}", font=font, fill="#b8ac98")
        strip.paste(tile_, (i * width, 0))
    strip.save(os.path.join(out, f"{label.replace(' ', '-')}-strip.png"))
    print(f"wrote {video} ({count} frames, played {loops + 1} times) and its strip")


def main(argv):
    command = argv[0] if argv else ""
    if command == "scenes" and len(argv) == 5:
        scenes(argv[1], argv[2], argv[3].split(","), argv[4])
    elif command == "tile" and len(argv) == 5:
        tile(argv[1], argv[2], argv[3], argv[4].split(","))
    else:
        raise SystemExit("usage: video.py scenes <frames folder> <scenes.json> <views> <WxH> | tile <frames folder> <renders folder> <out folder> <views>")


if __name__ == "__main__":
    main(sys.argv[1:])

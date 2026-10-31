# Makes the test terrain (Docs/Renderer-3D.md, "Terrain") in Tests/Statventure.Tests/Data/Terrain:
# a 16-bit height image, a cover image (each grid point's cover index), the same covers one-hot in
# RGBA for Blender's nodes, and the metadata the engine reads (grid, cell size, height range, the
# covers' palette slots, the water level). Deterministic; run by make-blender-references.sh.
#   python3 make_terrain.py <folder>
import json
import math
import os
import sys

from PIL import Image

WIDTH, HEIGHT, CELL = 64, 48, 0.5
LOW, HIGH, WATER = -1.0, 5.0, 0.4
COVERS = ["grass", "rock", "sand"]


def height(i, j):
    """Hills from a few crossing waves, a lake hollowed out to the south-west."""
    x, z = i * CELL, j * CELL
    h = 1.2 + 1.6 * math.sin(x * 0.21 + 0.4) * math.cos(z * 0.27 - 0.3) + 0.8 * math.sin((x + z) * 0.33)
    h += 2.6 * math.exp(-((x - 23) ** 2 + (z - 8) ** 2) / 18)  # a peak
    h -= 2.4 * math.exp(-((x - 9) ** 2 + (z - 16) ** 2) / 22)  # the lake
    return max(LOW, min(HIGH, h))


def cover(i, j, h):
    slope = abs(height(min(i + 1, WIDTH - 1), j) - height(max(i - 1, 0), j)) + abs(height(i, min(j + 1, HEIGHT - 1)) - height(i, max(j - 1, 0)))
    if h > 3.2 or slope > 1.6:
        return 1
    if h < WATER + 0.35:
        return 2
    return 0


def main():
    folder = sys.argv[1]
    os.makedirs(folder, exist_ok=True)
    heights = Image.new("I;16", (WIDTH, HEIGHT))
    covers = Image.new("L", (WIDTH, HEIGHT))
    onehot = Image.new("RGBA", (WIDTH, HEIGHT))
    for j in range(HEIGHT):
        for i in range(WIDTH):
            h = height(i, j)
            heights.putpixel((i, j), round((h - LOW) / (HIGH - LOW) * 65535))
            c = cover(i, j, h)
            covers.putpixel((i, j), c)
            onehot.putpixel((i, j), tuple(255 if k == c else 0 for k in range(4)))
    heights.save(os.path.join(folder, "test-height.png"))
    covers.save(os.path.join(folder, "test-cover.png"))
    onehot.save(os.path.join(folder, "test-cover-onehot.png"))
    meta = {"height": "test-height.png", "cover": "test-cover.png", "width": WIDTH, "depth": HEIGHT, "cellSize": CELL,
            "heightMin": LOW, "heightMax": HIGH, "covers": COVERS, "waterLevel": WATER}
    with open(os.path.join(folder, "test.json"), "w") as f:
        json.dump(meta, f, indent=2)
        f.write("\n")
    print("wrote", os.path.join(folder, "test.json"))


main()

# Lays Freestyle's ink over Blender's shading as the engine draws its lines over its view (Renderer3D.RenderInk: the ink
# colour at the line's coverage, blended in linear light into the sRGB view), for every scene render.py wrote: the
# picture the engine is held to, in one image (GM2 step 3a). The glow is composed before (compose_glow.py, which the
# engine draws before its lines too); particles, which the engine lays over the lines, a figure doesn't have.
# Plain Python with Pillow, run by Tools/Scripts/make-humanoid.sh render:
#   python3 compose.py <scenes.json> <work folder> <out folder>
import json
import os
import sys

from PIL import Image


def to_linear(c):
    c /= 255
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


LINEAR = [to_linear(i) for i in range(256)]
# Linear back to 8-bit sRGB through a fine table (4096 steps, then the nearest code).
STEPS = 4096
ENCODE = []
for k in range(STEPS + 1):
    v = k / STEPS
    s = v * 12.92 if v <= 0.0031308 else 1.055 * v ** (1 / 2.4) - 0.055
    ENCODE.append(max(0, min(255, round(s * 255))))


def compose(shading_path, ink_path, ink_hex, out_path):
    shading = Image.open(shading_path).convert("RGB")
    lines = Image.open(ink_path).convert("RGBA").getchannel("A")
    value = int(ink_hex.lstrip("#"), 16)
    ink = [LINEAR[(value >> shift) & 255] for shift in (16, 8, 0)]
    pixels = bytearray(shading.tobytes())
    coverage = lines.tobytes()
    for i, a in enumerate(coverage):
        if a == 0:
            continue
        alpha = a / 255
        for c in range(3):
            j = i * 3 + c
            mixed = LINEAR[pixels[j]] * (1 - alpha) + ink[c] * alpha
            pixels[j] = ENCODE[round(mixed * STEPS)]
    Image.frombytes("RGB", shading.size, bytes(pixels)).save(out_path)
    print("wrote", out_path)


def main():
    scenes_path, work, out = sys.argv[1:4]
    os.makedirs(out, exist_ok=True)
    with open(scenes_path) as f:
        scenes = json.load(f)["scenes"]
    for spec in scenes:
        shading = os.path.join(work, f"r3d-{spec['name']}.png")
        ink = os.path.join(work, f"r3d-{spec['name']}-ink.png")
        target = os.path.join(out, f"{spec['name']}.png")
        if os.path.exists(ink):
            compose(shading, ink, spec["style"]["ink"], target)
        else:
            Image.open(shading).save(target)
            print("wrote", target)


if __name__ == "__main__":
    main()

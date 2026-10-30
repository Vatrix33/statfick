# Lays the glow's halo into the 3D references (Docs/Renderer-3D.md, "Glow"), as the lizardman's
# compose.py made it: the glowing parts (render_scenes.py's <scene>-glow.png) blurred wide
# (premultiplied, Pillow's GaussianBlur) with the alpha boosted 2.2 times, blurred tight over that,
# all behind the models they belong to (<scene>-mask.png, their coverage) and over everything else.
#   python3 compose_glow.py <scenes.json> <golden folder> <glow folder>
import json
import os
import sys

from PIL import Image, ImageChops, ImageFilter


def main():
    scenes_path, golden, folder = sys.argv[1:4]
    with open(scenes_path) as f:
        scenes = json.load(f)["scenes"]
    for spec in scenes:
        glow_path = os.path.join(folder, f"{spec['name']}-glow.png")
        if not os.path.exists(glow_path):
            continue
        style, scale = spec["style"], spec.get("scale", 1)
        glow = Image.open(glow_path).convert("RGBA")
        mask = Image.open(os.path.join(folder, f"{spec['name']}-mask.png")).convert("RGBA").getchannel("A")
        halo = glow.convert("RGBa").filter(ImageFilter.GaussianBlur(style["glowWide"] * scale)).convert("RGBA")
        halo.putalpha(halo.getchannel("A").point(lambda a: min(255, int(a * style["glowBoost"]))))
        halo.alpha_composite(glow.convert("RGBa").filter(ImageFilter.GaussianBlur(style["glowCore"] * scale)).convert("RGBA"))
        # Behind its models: where they cover a pixel, the halo doesn't show.
        halo.putalpha(ImageChops.multiply(halo.getchannel("A"), ImageChops.invert(mask)))
        path = os.path.join(golden, f"r3d-{spec['name']}.png")
        view = Image.open(path).convert("RGBA")
        view.alpha_composite(halo)
        view.convert("RGB").save(path)
        print("wrote", path)


main()

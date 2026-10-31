# Lays the glow's halo into the 3D references (Docs/Renderer-3D.md, "Glow"), as the lizardman's
# compose.py made it: the glowing parts (render_scenes.py's <scene>-glow.png) blurred wide
# (premultiplied, Pillow's GaussianBlur) with the alpha boosted 2.2 times, blurred tight over that,
# all behind the models they belong to (<scene>-mask.png, their coverage) and over everything else.
# Then the particles (<scene>-particles.png, alone over a transparent film) over all of it, blended
# in linear light as the engine lays its particle layer (Docs/Renderer-3D.md, "Particles").
#   python3 compose_glow.py <scenes.json> <golden folder> <glow folder>
import json
import os
import sys

from PIL import Image, ImageChops, ImageFilter


def decode(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def encode(c):
    return c * 12.92 if c <= 0.0031308 else 1.055 * c ** (1 / 2.4) - 0.055


LINEAR = [decode(i / 255) for i in range(256)]


def flat(image):
    return image.get_flattened_data() if hasattr(image, "get_flattened_data") else image.getdata()


def over_in_linear(view, layer):
    """The layer (straight colour, sRGB) over the view, mixed in linear light and encoded again."""
    pixels = []
    for (vr, vg, vb, va), (lr, lg, lb, la) in zip(flat(view), flat(layer)):
        a = la / 255
        pixels.append(tuple(round(255 * encode(min(1.0, LINEAR[l] * a + LINEAR[v] * (1 - a)))) for v, l in ((vr, lr), (vg, lg), (vb, lb))) + (va,))
    result = Image.new("RGBA", view.size)
    result.putdata(pixels)
    return result


def main():
    scenes_path, golden, folder = sys.argv[1:4]
    with open(scenes_path) as f:
        scenes = json.load(f)["scenes"]
    for spec in scenes:
        glow_path = os.path.join(folder, f"{spec['name']}-glow.png")
        particles_path = os.path.join(folder, f"{spec['name']}-particles.png")
        if not os.path.exists(glow_path) and not os.path.exists(particles_path):
            continue
        path = os.path.join(golden, f"r3d-{spec['name']}.png")
        view = Image.open(path).convert("RGBA")
        if os.path.exists(glow_path):
            style, scale = spec["style"], spec.get("scale", 1)
            glow = Image.open(glow_path).convert("RGBA")
            mask = Image.open(os.path.join(folder, f"{spec['name']}-mask.png")).convert("RGBA").getchannel("A")
            halo = glow.convert("RGBa").filter(ImageFilter.GaussianBlur(style["glowWide"] * scale)).convert("RGBA")
            halo.putalpha(halo.getchannel("A").point(lambda a: min(255, int(a * style["glowBoost"]))))
            halo.alpha_composite(glow.convert("RGBa").filter(ImageFilter.GaussianBlur(style["glowCore"] * scale)).convert("RGBA"))
            # Behind its models: where they cover a pixel, the halo doesn't show.
            halo.putalpha(ImageChops.multiply(halo.getchannel("A"), ImageChops.invert(mask)))
            view.alpha_composite(halo)
            # Used up: a later run that doesn't render this scene must not lay its halo in again.
            os.remove(glow_path)
            os.remove(os.path.join(folder, f"{spec['name']}-mask.png"))
        if os.path.exists(particles_path):
            view = over_in_linear(view, Image.open(particles_path).convert("RGBA"))
            os.remove(particles_path)
        view.convert("RGB").save(path)
        print("wrote", path)


main()

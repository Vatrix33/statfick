#!/usr/bin/env python3
"""Makes the raster images of the image-scaling paint fixtures (ImageScalingPaintReferenceTests): patterns that tell
a mip level from plain bilinear sampling (one-pixel checks and stripes, fine rings), straight colour beside fully
transparent white (premultiplied filtering), translucent gradients. 8-bit RGBA PNGs without colour chunks (no gAMA,
sRGB or iCCP: Chromium draws them untouched under --force-color-profile=srgb). Deterministic; run from this folder:
python3 make-images.py"""
import math
from PIL import Image


def pattern(width, height, seed):
    image = Image.new("RGBA", (width, height))
    px = image.load()
    for y in range(height):
        for x in range(width):
            u, v = x / width, y / height
            # Ground: a two-colour gradient, opaque.
            r, g, b, a = int(40 + 180 * u), int(60 + 120 * v), int(150 - 100 * u * v + seed * 7) % 256, 255
            # One-pixel checks (left third, top half).
            if x < width // 3 and y < height // 2:
                r, g, b = ((240, 236, 220) if (x + y) % 2 == 0 else (24, 28, 40))
            # Two-pixel stripes along a diagonal (middle third, top half).
            elif x < 2 * width // 3 and y < height // 2:
                r, g, b = ((200, 40, 40) if ((x + 2 * y) // 2) % 2 == 0 else (250, 230, 120))
            # Fine antialiased rings (right third, top half).
            elif y < height // 2:
                cx, cy = 5 * width // 6, height // 4
                d = math.hypot(x + 0.5 - cx, y + 0.5 - cy)
                t = 0.5 + 0.5 * math.cos(d * 1.9)
                r, g, b = int(30 + 210 * t), int(90 + 60 * t), int(200 - 150 * t)
            # Yellow beside transparent white (the BRUTAL logo's kind), with a soft edge (left half, bottom half).
            elif x < width // 2:
                d = math.hypot(x + 0.5 - width // 4, y + 0.5 - 3 * height // 4)
                inside = max(0.0, min(1.0, (height // 6 - d) + 0.5))
                ring = d < height // 10
                if ring:
                    r, g, b, a = 255, 255, 255, 0
                else:
                    r, g, b, a = (241, 196, 15, 255) if (x // 7 + y // 7) % 2 == 0 else (255, 255, 255, 0)
                    if inside > 0 and not ring:
                        r, g, b, a = 241, 196, 15, int(round(255 * max(inside, 0.0)))
            # A translucent gradient with one-pixel lines (right half, bottom half).
            else:
                a = int(255 * (x - width // 2) / (width - width // 2))
                r, g, b = (20, 20, 20) if y % 3 == 0 else (90, 200, 120)
            px[x, y] = (r, g, b, a)
    return image


for name, size, seed in [("scaling-even", (320, 160), 0), ("scaling-odd", (201, 101), 1)]:
    pattern(*size, seed).save(f"{name}.png", optimize=True)
    print(name, size)

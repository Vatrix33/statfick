#!/usr/bin/env python3
# Shows a test render next to its reference: reference | render | difference (x4, red where > tolerance).
#   diff-images.py <name> [output.png] [tolerance]
import sys, os
from PIL import Image, ImageChops
root = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Tests", "Statventure.Tests")
name = sys.argv[1]
out = sys.argv[2] if len(sys.argv) > 2 else os.path.join(root, "Actual", name + "-diff.png")
tolerance = int(sys.argv[3]) if len(sys.argv) > 3 else 12
golden = Image.open(os.path.join(root, "Golden", name + ".png")).convert("RGBA")
actual = Image.open(os.path.join(root, "Actual", name + ".png")).convert("RGBA")
diff = ImageChops.difference(golden.convert("RGB"), actual.convert("RGB"))
amplified = diff.point(lambda v: min(255, v * 4))
mask = diff.convert("L").point(lambda v: 255 if v > tolerance else 0)
marked = amplified.copy()
marked.paste((255, 0, 0), mask=mask)
w, h = golden.size
sheet = Image.new("RGB", (w * 3 + 20, h), (40, 40, 40))
sheet.paste(golden.convert("RGB"), (0, 0))
sheet.paste(actual.convert("RGB"), (w + 10, 0))
sheet.paste(marked, (w * 2 + 20, 0))
sheet.save(out)
print(out)

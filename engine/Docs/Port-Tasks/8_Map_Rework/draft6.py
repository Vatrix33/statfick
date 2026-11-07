# Layout draft 6 (task 8, W19-W26), the approved layout of the continent: draft 5 (draft 4's layout with draft 3's rougher
# coast and the water between the Alder Vale and the rocky highlands) with the Deep Desert's island and the water between
# Duskpine and the Crimson Divide taken from draft 3. Usage: python3 draft6.py out.png (numpy and Pillow).
import json, math, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
D = os.path.dirname(os.path.abspath(__file__))
R = json.load(open(os.path.join(D, "regions.json")))
OUT = sys.argv[1]
S, W, H = 1.4, 2240, 1400
POS = {
  "alder-vale": (0, 0), "stoneveil-foothills": (0, -1700), "high-stoneveil": (-406, -3400), "stoneveil-summit": (-406, -5100),
  "sunward-coast": (2484, -1700), "cinder-isle": (7900, 490), "verdant-reach": (-2484, 490), "stormwake-hills": (-1812, 2134),
  "gloamfen-marsh": (-4296, 2134), "verdigris-jungle": (-4338, 3778), "mirrormere-basin": (-1770, 3778), "saffron-expanse": (-1770, 5422),
  "deep-desert": (714, 4330), "crimson-divide": (-1770, 7066), "bloodstone-coast": (-1770, 8710), "ancient-battlefield": (-4254, 8710),
  "duskpine-forest": (-4254, 7066),
}
NEW = {  # proposed new regions: origin, label, level band, colour
  "new-rocky": ((2484, -56), "NEW · Rocky highlands", "the one rocky region (W20)", "15–30", (150, 132, 112)),
  "new-east": ((672, 1644), "NEW · Steppe", "grassland and dry hills", "20–35", (196, 184, 120)),
  "new-west": ((-4296, 5422), "NEW · West coast delta", "river mouths, mangroves", "50–65", (92, 132, 104)),
}
TINT = {"alder-vale": (142,170,98), "stoneveil-foothills": (156,168,108), "high-stoneveil": (150,150,138), "stoneveil-summit": (206,208,210),
  "sunward-coast": (182,182,122), "cinder-isle": (98,90,84), "verdant-reach": (98,146,78), "stormwake-hills": (118,150,112), "gloamfen-marsh": (112,122,86),
  "verdigris-jungle": (62,116,62), "mirrormere-basin": (162,166,112), "saffron-expanse": (214,188,128), "deep-desert": (224,198,142), "crimson-divide": (178,98,74),
  "bloodstone-coast": (152,84,72), "ancient-battlefield": (142,126,96), "duskpine-forest": (74,98,82)}
LEVEL = {}
SHORT = {"stoneveil-foothills": "Stoneveil\nFoothills", "stoneveil-summit": "Stoneveil\nSummit", "high-stoneveil": "High\nStoneveil", "sunward-coast": "Sunward\nCoast",
  "verdant-reach": "Verdant\nReach", "stormwake-hills": "Stormwake\nHills", "gloamfen-marsh": "Gloamfen\nMarsh", "verdigris-jungle": "Verdigris\nJungle",
  "mirrormere-basin": "Mirrormere\nBasin", "saffron-expanse": "Saffron\nExpanse", "deep-desert": "Deep\nDesert", "crimson-divide": "Crimson\nDivide",
  "bloodstone-coast": "Bloodstone\nCoast", "ancient-battlefield": "Ancient\nBattlefield", "duskpine-forest": "Duskpine\nForest", "alder-vale": "Alder\nVale", "cinder-isle": "Cinder\nIsle"}
# Roads: today's roads; the Battlefield's road now turns north to Duskpine, Duskpine's leaves south; Saffron's road to the
# Deep Desert leaves higher on its east side and the Deep Desert's arrives lower on its west side.
roads = {rid: [list(map(tuple, line)) for line in r["roads"]] for rid, r in R.items()}
R["alder-vale"]["level"] = {"minimum": 1, "maximum": 15}
exits = {rid: {e["to"]: (e["x"], e["y"]) for e in r["exits"]} for rid, r in R.items()}
roads["ancient-battlefield"] = roads["ancient-battlefield"][:3] + [[(350, 500), (420, 330), (480, 160), (500, -20)]]
exits["ancient-battlefield"]["duskpine-forest"] = (500, 20)
roads["duskpine-forest"] = [[(815, 505), (700, 700), (560, 860), (500, 1020)]]
exits["duskpine-forest"]["ancient-battlefield"] = (500, 980)
roads["saffron-expanse"][2] = [(1050, 585), (1300, 330), (1620, 120)]
exits["saffron-expanse"]["deep-desert"] = (1580, 120)
roads["deep-desert"] = [[(330, 270), (220, 560), (80, 820), (-20, 900)]]
exits["deep-desert"]["saffron-expanse"] = (20, 900)
P = lambda rid, x, y: (POS[rid][0] + x * S, POS[rid][1] + y * S)
# Land rectangles: a coast region's own sea is not land (the Sunward Coast past x 1300, the Bloodstone Coast past y 850).
rects = []
for rid, (x, y) in POS.items():
    if rid == "cinder-isle": continue
    w, h = W, H
    if rid == "sunward-coast": w = 1300 * S
    if rid == "bloodstone-coast": h = 850 * S
    rects.append((rid, x, y, x + w, y + h))
for rid, ((x, y), *_ ) in NEW.items(): rects.append((rid, x, y, x + W, y + H))
STEP = 16
X0, Y0, X1, Y1 = -7200, -6100, 11200, 11000
xs = np.arange(X0, X1, STEP, dtype=np.float32); ys = np.arange(Y0, Y1, STEP, dtype=np.float32)
gx, gy = np.meshgrid(xs, ys)
def rounded(x0, y0, x1, y1, r=320):
    hx, hy = (x1 - x0) / 2, (y1 - y0) / 2
    qx = np.abs(gx - (x0 + hx)) - hx + r; qy = np.abs(gy - (y0 + hy)) - hy + r
    return np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - r
# Each region's own outline, and the ground between two regions: wherever a point lies between two regions (the sum of
# its distances to them is close to their distance apart) it is land, so neighbours grow into one another (W22).
dist = []
for rid, x0, y0, x1, y1 in rects: dist.append(rounded(x0, y0, x1, y1))
dist = np.stack(dist); sd = dist.min(axis=0); owner = np.argmin(dist, axis=0)
plain = []
for rid, x0, y0, x1, y1 in rects:
    plain.append(np.hypot(np.maximum(np.maximum(x0 - gx, gx - x1), 0), np.maximum(np.maximum(y0 - gy, gy - y1), 0)))
plain = np.stack(plain)
gapOf = lambda a, b: math.hypot(max(a[1] - b[3], b[1] - a[3], 0), max(a[2] - b[4], b[2] - a[4], 0))
pairs = [(i, j, gapOf(rects[i], rects[j])) for i in range(len(rects)) for j in range(i + 1, len(rects)) if gapOf(rects[i], rects[j]) < 1400]
# The coast's noise: value noise in octaves, like continent-engine.js's margin (up to 400 units in or out, W18).
rng = np.random.default_rng(11)
def vnoise(scale, seed):
    r = np.random.default_rng(seed).random((int((Y1 - Y0) / scale) + 3, int((X1 - X0) / scale) + 3)).astype(np.float32)
    im = Image.fromarray(r).resize((int((X1 - X0) / scale + 3) * 8, int((Y1 - Y0) / scale + 3) * 8), Image.BICUBIC)
    a = np.asarray(im); fx = (gx - X0) / scale * 8; fy = (gy - Y0) / scale * 8
    return a[np.clip(fy.astype(int), 0, a.shape[0] - 1), np.clip(fx.astype(int), 0, a.shape[1] - 1)]
n = sum(vnoise(1100 / 2 ** k, 40 + k) * 0.62 ** k for k in range(5)) / sum(0.62 ** k for k in range(5))
m = 400 * np.clip((n - np.median(n)) / 0.14, -1, 1)
detail = (vnoise(60, 51) - 0.5) * 44 + (vnoise(24, 52) - 0.5) * 18
# Capes and bays at a middle size too (about 20-40 km), so the outer coast is as broken as draft 3's.
m = m + (vnoise(300, 91) - 0.5) * 300 + (vnoise(130, 92) - 0.5) * 120
reach = 100 + 360 * vnoise(700, 81)
fill = np.full(sd.shape, -1e9, np.float32)
for i, j, gap in pairs:
    lens = plain[i] + plain[j] - gap
    fill = np.maximum(fill, reach - lens)
# Near the ground between two regions the coast never bites in (no lakes left between neighbours).
inner = np.clip((fill + 150) / 150, 0, 1)
m = m * (1 - inner) + np.maximum(m, 60) * inner
field = np.maximum(m - sd, fill) + detail
# Inland waters the user kept from draft 3 (W24): between the Alder Vale and the rocky highlands, and between Duskpine
# and the Crimson Divide: long lakes in the ground between them, their shores as ragged as the coast.
def water(cx, cy, rx, ry, seed):
    e = np.hypot((gx - cx) / rx, (gy - cy) / ry) + (vnoise(140, seed) - 0.5) * 0.7 + (vnoise(50, seed + 1) - 0.5) * 0.25
    return (1 - e) * min(rx, ry)
field = np.minimum(field, -water(2362, 760, 330, 900, 101))
# Draft 3's coast field, computed by running draft3.py (same grid and seeds) into a scratch picture.
import runpy, tempfile
_argv = sys.argv; sys.argv = ["draft3.py", os.path.join(tempfile.gettempdir(), "continent-draft-3.png")]
_d3 = runpy.run_path(os.path.join(D, "draft3.py")); sys.argv = _argv
D3 = (_d3["m"] + _d3["detail"] - _d3["sd"]).astype(np.float32)
def soft(d, feather):
    return np.clip(1 - d / feather, 0, 1) ** 2 * (3 - 2 * np.clip(1 - d / feather, 0, 1)) if False else np.clip(0.5 - d / feather, 0, 1)
# The Deep Desert's south-east coast and its island as in draft 3, moved up with the Deep Desert (532 units): draft 3's
# land there, faded into draft 6's over 400 units round it.
shift = int(round(532 / STEP))
old = np.roll(D3, -shift, 0)
cx_, cy_ = 2850, 5900 - 532
w = np.clip((1 - np.hypot((gx - cx_) / 900, (gy - cy_) / 700)) / 0.35, 0, 1)
field = field * (1 - w) + old * w
# The water between Duskpine and the Crimson Divide as in draft 3: draft 3's land and water in the ground between those
# two regions (a ragged lens round their gap), faded into draft 6's at its ends.
def plainDistance(rid):
    x0, y0, x1, y1 = next((r[1], r[2], r[3], r[4]) for r in rects if r[0] == rid)
    return np.hypot(np.maximum(np.maximum(x0 - gx, gx - x1), 0), np.maximum(np.maximum(y0 - gy, gy - y1), 0))
lens = plainDistance("duskpine-forest") + plainDistance("crimson-divide") - 244 + (vnoise(220, 111) - 0.5) * 300
w = np.clip((1100 - lens) / 400, 0, 1)
field = field * (1 - w) + D3 * w
land = field > 0
# Cinder Isle: its island, its coast natural too.
cx, cy = P("cinder-isle", 800, 500)
e = np.hypot((gx - cx) / (700 * S), (gy - cy) / (445 * S))
land |= e < 1 + (vnoise(500, 61) - 0.5) * 0.35
# Mirrormere's lake.
mr = R["mirrormere-basin"]["lake"]
lx, ly = P("mirrormere-basin", mr["x"], mr["y"])
lake = np.hypot((gx - lx) / (mr["rx"] * S), (gy - ly) / (mr["ry"] * S)) < 1
ids = [r[0] for r in rects]
tints = np.array([TINT.get(i) or NEW[i][4] for i in ids], np.float32)
# Region colours meet along a wandering line.
wx = (vnoise(500, 71) - 0.5) * 500; wy = (vnoise(500, 72) - 0.5) * 500
ow = owner.copy()
ix = np.clip(((gx + wx - X0) / STEP).astype(int), 0, gx.shape[1] - 1); iy = np.clip(((gy + wy - Y0) / STEP).astype(int), 0, gx.shape[0] - 1)
col = tints[owner[iy, ix]]
hh, ww = land.shape
lay = Image.fromarray(np.dstack([col * land[..., None], land * 255.0]).astype(np.uint8), "RGBA").filter(ImageFilter.GaussianBlur(10))
a = np.asarray(lay).astype(np.float32); soft = a[..., :3] / np.maximum(a[..., 3:4], 1) * 255
near = np.asarray(Image.fromarray(land.astype(np.uint8) * 255).filter(ImageFilter.GaussianBlur(6))).astype(np.float32) / 255
depth = np.clip(1 - near * 3, 0, 1)[..., None]
img = np.array([96, 128, 128], np.float32) * (1 - depth) + np.array([63, 91, 96], np.float32) * depth
cinder_mask = e < 1.4
soft[cinder_mask & land] = TINT["cinder-isle"]
img[land] = soft[land] * (1 + (rng.random((hh, ww, 1)) - 0.5) * 0.05)[land]
img[lake] = (92, 128, 140)
edge = np.zeros_like(land)
for dy, dx in ((0, 1), (1, 0), (0, -1), (-1, 0)): edge |= land & ~np.roll(np.roll(land, dy, 0), dx, 1)
img[edge] = (34, 26, 20)
# Hatch the new regions lightly so they read as new.
for rid, ((x, y), *_ ) in NEW.items():
    k = ids.index(rid); sel = (owner == k) & land & ~cinder_mask & (((gx + gy) / 120).astype(int) % 2 == 0)
    img[sel] = img[sel] * 0.9
im = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB")
dr = ImageDraw.Draw(im)
T = lambda x, y: ((x - X0) / STEP, (y - Y0) / STEP)
for rid, lines in roads.items():
    for line in lines: dr.line([T(*P(rid, x, y)) for x, y in line], fill=(84, 42, 22), width=3)
done = set()
for rid, ex in exits.items():
    for to, p in ex.items():
        key = tuple(sorted([rid, to]))
        if key in done: continue
        done.add(key); q = exits[to][rid]
        dr.line([T(*P(rid, *p)), T(*P(to, *q))], fill=(84, 42, 22), width=3)
sb = next(s for s in R["sunward-coast"]["settlements"] if s["boat"]); eh = R["cinder-isle"]["settlements"][0]
a0, b0 = T(*P("sunward-coast", sb["x"], sb["y"])), T(*P("cinder-isle", eh["x"], eh["y"]))
for t in np.arange(0, 1, 0.04): dr.line([(a0[0] + (b0[0] - a0[0]) * t, a0[1] + (b0[1] - a0[1]) * t), (a0[0] + (b0[0] - a0[0]) * (t + 0.02), a0[1] + (b0[1] - a0[1]) * (t + 0.02))], fill=(20, 60, 90), width=2)
F = os.path.join(D, "..", "..", "..", "..", "old-game", "objects", "fonts") + os.sep
bold, reg, ital, big = ImageFont.truetype(F + "Lato-Bold.ttf", 15), ImageFont.truetype(F + "Lato-Regular.ttf", 14), ImageFont.truetype(F + "STIXTwoText-Italic.otf", 18), ImageFont.truetype(F + "STIXTwoText-Italic.otf", 25)
def label(x, y, text, f, fill=(25, 18, 12), stroke=(244, 238, 222), sw=3, anchor="mm"):
    dr.multiline_text((x, y), text, font=f, fill=fill, stroke_width=sw, stroke_fill=stroke, anchor=anchor, align="center", spacing=0)
for rid, r in R.items():
    for s in r["settlements"]:
        x, y = T(*P(rid, s["x"], s["y"]))
        if "Castle" in s["kind"]: dr.rectangle([x - 5, y - 5, x + 5, y + 5], fill=(150, 30, 26), outline=(20, 12, 8))
        else: dr.ellipse([x - 3, y - 3, x + 3, y + 3], fill=(30, 22, 16))
for rid, (x, y) in POS.items():
    r = R[rid]; lv = LEVEL.get(rid) or "%d–%d" % (r["level"]["minimum"], r["level"]["maximum"])
    shift = {"alder-vale": 0, "duskpine-forest": -250, "crimson-divide": 250}.get(rid, 420 if rid in ("stoneveil-foothills", "verdant-reach", "saffron-expanse", "stormwake-hills", "mirrormere-basin", "verdigris-jungle", "gloamfen-marsh") else 0)
    cx_, cy_ = T(x + W / 2 + shift, y + H / 2 - 330)
    if rid == "cinder-isle": cx_, cy_ = T(x + W / 2 + 200, y + H / 2 - 200)
    label(cx_, cy_, SHORT[rid], big); label(cx_, cy_ + 36, "Lv " + lv, bold)
for rid, ((x, y), name, what, lv, _c) in NEW.items():
    cx_, cy_ = T(x + W / 2 + (300 if rid == "new-rocky" else 0), y + H / 2)
    label(cx_, cy_ - 30, name.replace(" · ", "\n"), big, fill=(120, 18, 18)); label(cx_, cy_ + 14, what, reg); label(cx_, cy_ + 32, "Lv " + lv + " (proposed)", bold)
label(*T(5600, -1100), "boat · 12.6 h", reg, fill=(20, 60, 90))
# Scale bar.
px = 100 * (40 / 3) / STEP; bx, by = 40, hh - 40
dr.rectangle([bx, by, bx + px, by + 7], fill=(244, 238, 222), outline=(20, 12, 8)); dr.rectangle([bx, by, bx + px / 2, by + 7], fill=(20, 12, 8))
label(bx + px / 2, by - 12, "100 km", bold)
dr.polygon([(ww - 60, 30), (ww - 70, 58), (ww - 50, 58)], fill=(25, 18, 12)); label(ww - 60, 74, "N", bold)
# The legend.
notes = ["DRAFT 6 (task 8, W19–W25)",
  "New regions in red, fully built.",
  "Neighbours touch: no rock or water between.",
  "Coast up to 30 km in or out (W18).",
  "Colours only tell regions apart.",
  "      castle city   ● town   — road   - - boat"]
small_font = ImageFont.truetype(F + "Lato-Regular.ttf", 12); small_bold = ImageFont.truetype(F + "Lato-Bold.ttf", 12)
bw, lh = 268, 16
lx, ly = ww - bw - 14, hh - lh * len(notes) - 22
dr.rectangle([lx - 8, ly - 7, lx + bw, ly + lh * len(notes) + 3], fill=(244, 238, 222), outline=(60, 44, 30))
for i, line in enumerate(notes): dr.text((lx, ly + i * lh), line, font=small_bold if i == 0 else small_font, fill=(25, 18, 12))
y_ = ly + (len(notes) - 1) * lh + 7; dr.rectangle([lx + 2, y_ - 4, lx + 10, y_ + 4], fill=(150, 30, 26), outline=(20, 12, 8))
im.save(OUT); print(OUT, im.size)

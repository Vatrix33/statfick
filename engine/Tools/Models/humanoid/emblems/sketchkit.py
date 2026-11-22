# sketchkit.py : drawing helpers for the emblems' motif sketches (GM2 step 10, the manager's scratch). A motif is a list of
# elements in a unit field (x to the right, y up, the field's edge at radius 1): filled outlines and strokes, each in a
# tone: "hi" (raised, light), "mid" (raised, the rim's metal), "lo" (cut in, dark), "acc" (the race's accent colour).
# The outlines are kept as data so the models can be raised from the same shapes (step 10's modelling).
import math

from PIL import Image, ImageDraw, ImageFilter

INK = (26, 18, 13)
BONE = (225, 211, 173)
BRASS = (187, 141, 60)
BRASS_DARK = (128, 92, 36)


def mix(a, b, t):
    return tuple(round(p + (q - p) * t) for p, q in zip(a, b))


def rgb(hex_):
    return tuple(int(hex_[i:i + 2], 16) for i in (1, 3, 5))


def catmull(points, closed=False, steps=12):
    """A smooth line through the points (Catmull-Rom), as many small steps."""
    pts = list(points)
    n = len(pts)
    out = []
    last = n if closed else n - 1
    for i in range(last):
        p0 = pts[(i - 1) % n] if (closed or i > 0) else pts[0]
        p1, p2 = pts[i % n], pts[(i + 1) % n]
        p3 = pts[(i + 2) % n] if (closed or i + 2 < n) else pts[-1]
        for s in range(steps):
            t = s / steps
            t2, t3 = t * t, t * t * t
            out.append(tuple(0.5 * ((2 * b) + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t2 + (-a + 3 * b - 3 * c + d) * t3)
                             for a, b, c, d in zip(p0, p1, p2, p3)))
    if not closed:
        out.append(tuple(pts[-1]))
    return out


def stroke(points, w0, w1=None, smooth=True, steps=12):
    """A line of width w0 (thinning or swelling to w1 at its end) as an outline with round ends."""
    w1 = w0 if w1 is None else w1
    line = catmull(points, False, steps) if smooth and len(points) > 2 else list(points)
    n = len(line)
    left, right = [], []
    for i, (x, y) in enumerate(line):
        ax, ay = line[max(0, i - 1)]
        bx, by = line[min(n - 1, i + 1)]
        dx, dy = bx - ax, by - ay
        size = math.hypot(dx, dy) or 1.0
        nx, ny = -dy / size, dx / size
        half = (w0 + (w1 - w0) * i / (n - 1)) / 2
        left.append((x + nx * half, y + ny * half))
        right.append((x - nx * half, y - ny * half))

    def cap(centre, start, half, steps_=6):
        cx, cy = centre
        a0 = math.atan2(start[1] - cy, start[0] - cx)
        return [(cx + half * math.cos(a0 - math.pi * k / steps_), cy + half * math.sin(a0 - math.pi * k / steps_)) for k in range(1, steps_)]

    return left + cap(line[-1], left[-1], w1 / 2) + right[::-1] + cap(line[0], right[0], w0 / 2)


def circle(x, y, r, steps=40):
    return [(x + r * math.cos(2 * math.pi * k / steps), y + r * math.sin(2 * math.pi * k / steps)) for k in range(steps)]


def ellipse(x, y, rx, ry, turn=0.0, steps=40):
    c, s = math.cos(math.radians(turn)), math.sin(math.radians(turn))
    out = []
    for k in range(steps):
        a = 2 * math.pi * k / steps
        px, py = rx * math.cos(a), ry * math.sin(a)
        out.append((x + px * c - py * s, y + px * s + py * c))
    return out


def arc(x, y, r, a0, a1, steps=24):
    return [(x + r * math.cos(math.radians(a0 + (a1 - a0) * k / steps)), y + r * math.sin(math.radians(a0 + (a1 - a0) * k / steps))) for k in range(steps + 1)]


def mirror(points):
    return [(-x, y) for x, y in points][::-1]


def fill(points, tone="hi", smooth=False, outline=True):
    return {"points": catmull(points, True) if smooth else list(points), "tone": tone, "outline": outline}


def line(points, w0, w1=None, tone="hi", smooth=True, outline=True):
    return {"points": stroke(points, w0, w1, smooth), "tone": tone, "outline": outline}


def both(element_points, **kind):
    """An element and its mirror image (kind: the arguments of fill)."""
    return [fill(element_points, **kind), fill(mirror(element_points), **kind)]


def both_lines(points, w0, w1=None, **kind):
    return [line(points, w0, w1, **kind), line([(-x, y) for x, y in points], w0, w1, **kind)]


def medallion(elements, accent, size=440, scale=3):
    """The sketch of a medallion: a rim, the field in the race's accent darkened, the motif."""
    S = size * scale
    cx = cy = S / 2
    R = S * 0.36                                   # the field's radius
    accent_rgb = rgb(accent)
    field = mix(accent_rgb, INK, 0.58)
    tones = {"hi": BONE, "mid": BRASS, "lo": mix(field, INK, 0.62), "acc": mix(accent_rgb, BONE, 0.18)}
    im = Image.new("RGB", (S, S), (42, 31, 26))
    d = ImageDraw.Draw(im)

    def disc(r, colour):
        d.ellipse((cx - r * R, cy - r * R, cx + r * R, cy + r * R), fill=colour)

    disc(1.33, INK)
    disc(1.30, BRASS)
    disc(1.19, BRASS_DARK)
    disc(1.165, BRASS)
    for k in range(36):                            # the rim's ticks
        a = 2 * math.pi * k / 36
        d.line((cx + 1.21 * R * math.cos(a), cy + 1.21 * R * math.sin(a), cx + 1.275 * R * math.cos(a), cy + 1.275 * R * math.sin(a)), fill=BRASS_DARK, width=max(2, scale * 2))
    disc(1.075, INK)
    disc(1.045, field)

    def px(points):
        return [(cx + x * R, cy - y * R) for x, y in points]

    # the motif on a layer of its own, cut to the field
    layer = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ld = ImageDraw.Draw(layer)
    shadow = Image.new("L", (S, S), 0)
    sd = ImageDraw.Draw(shadow)
    off = scale * 5
    for e in elements:
        if e["tone"] != "lo":
            sd.polygon([(x + off * 0.7, y + off) for x, y in px(e["points"])], fill=255)
    for e in elements:
        colour = tones[e["tone"]]
        pts = px(e["points"])
        if e["outline"] and e["tone"] != "lo":
            ld.polygon(pts, fill=colour + (255,), outline=INK + (255,), width=round(scale * 2.4))
        else:
            ld.polygon(pts, fill=colour + (255,))
    mask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(mask).ellipse((cx - 1.045 * R, cy - 1.045 * R, cx + 1.045 * R, cy + 1.045 * R), fill=255)
    shadow = shadow.filter(ImageFilter.GaussianBlur(scale * 1.5))
    dark = Image.new("RGB", (S, S), mix(field, INK, 0.55))
    im.paste(dark, (0, 0), Image.composite(shadow, Image.new("L", (S, S), 0), mask))
    alpha = Image.composite(layer.getchannel("A"), Image.new("L", (S, S), 0), mask)
    im.paste(layer.convert("RGB"), (0, 0), alpha)
    return im.resize((size, size), Image.LANCZOS)

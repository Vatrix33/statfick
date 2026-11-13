"""Geometry and the icons' inked style ("Engraved", the user's pick of 2026-11-13; GM1 step 3).

Units: 1/1000 em. x from the glyph's pen position, y down from the baseline (y = 0 on the baseline, ink above it has
negative y), as the outline is drawn in the text's box. Figures are built centred at (0, 0); build.py places each
where the old glyph's ink sat. Light comes from the upper right, as on the old game's map marks and battle sprites
(old-game/scripts/map-ink.js): the shadow falls to the west and the lower left.

The style follows map-ink.js's compass rose ("each point is split into a lit and a shadowed half, as on an engraved
rose") and an engraver's shade lines: a filled shape is an ink contour with its shadow half solid and its lit half
open; an open shape is a line heavier on its shadow side; thin parts and lines are solid ink.
"""
import math
from shapely import affinity
from shapely.geometry import Polygon, MultiPolygon, LineString, Point, box, GeometryCollection
from shapely.geometry.polygon import orient
from shapely.ops import unary_union

LIGHT = (1 / math.sqrt(2), -1 / math.sqrt(2))  # towards the light: up and right (y down)

# Weights in 1/1000 em, the same for every icon so they sit together as one family: the contour a little lighter
# than Lato Regular's stem (0.09 em), so an icon's line weighs about as the text's beside it.
W = {
    "contour": 56,   # the ink line round every shape (and an open shape's line on its lit side)
    "heavy": 110,    # a filled shape's line on its lit half where it must read as filled at 9-16 px: ★ beside ☆ on the
                     # favourite button, ✸ at 9 px (the user's answers, 2026-11-13)
    "shade": 60,     # what an open shape's line gains on its shadow side
    "detail": 34,    # detail lines: inked on open ground, cut out of solid ink
    "knock": 44,     # the gap a shape in front cuts round itself (the front sword of ⚔)
}

# ---------------------------------------------------------------- primitives

def P(points):
    return Polygon(points).buffer(0)

def circle(cx, cy, r):
    return Point(cx, cy).buffer(r, quad_segs=96)

def ellipse(cx, cy, rx, ry, angle=0):
    e = affinity.scale(Point(0, 0).buffer(1, quad_segs=96), rx, ry)
    return affinity.translate(affinity.rotate(e, angle, origin=(0, 0)), cx, cy)

def rect(x0, y0, x1, y1):
    return box(min(x0, x1), min(y0, y1), max(x0, x1), max(y0, y1))

def rounded(x0, y0, x1, y1, r):
    return rect(x0 + r, y0 + r, x1 - r, y1 - r).buffer(r, quad_segs=32) if r else rect(x0, y0, x1, y1)

def stroke(points, width, cap="round", join="round"):
    return LineString(points).buffer(width / 2, cap_style=cap, join_style=join, quad_segs=48, mitre_limit=10)

def bezier(p0, p1, p2, p3=None, n=48):
    """Points of a quadratic (3 points) or cubic (4 points) Bézier, both ends included."""
    out = []
    for i in range(n + 1):
        t = i / n
        if p3 is None:
            a, b, c = (1 - t) ** 2, 2 * (1 - t) * t, t ** 2
            out.append((a * p0[0] + b * p1[0] + c * p2[0], a * p0[1] + b * p1[1] + c * p2[1]))
        else:
            a, b, c, d = (1 - t) ** 3, 3 * (1 - t) ** 2 * t, 3 * (1 - t) * t ** 2, t ** 3
            out.append((a * p0[0] + b * p1[0] + c * p2[0] + d * p3[0], a * p0[1] + b * p1[1] + c * p2[1] + d * p3[1]))
    return out

def path(*segments):
    """Joins point lists, dropping repeated joints."""
    out = []
    for seg in segments:
        for p in seg:
            if not out or abs(out[-1][0] - p[0]) > 1e-9 or abs(out[-1][1] - p[1]) > 1e-9:
                out.append(p)
    return out

def arc(cx, cy, r, a0, a1, n=None):
    """Points on a circle from angle a0 to a1 (degrees; 0 east, 90 south, since y is down)."""
    n = n or max(8, int(abs(a1 - a0) / 2))
    return [(cx + r * math.cos(math.radians(a0 + (a1 - a0) * i / n)), cy + r * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]

def union(*shapes):
    return unary_union([s for s in shapes if s is not None and not s.is_empty])

def move(shape, dx, dy):
    return affinity.translate(shape, dx, dy)

def turn(shape, degrees, origin=(0, 0)):
    return affinity.rotate(shape, degrees, origin=origin)

def mirror_x(shape, x=0):
    return affinity.scale(shape, -1, 1, origin=(x, 0))

def mirror_y(shape, y=0):
    return affinity.scale(shape, 1, -1, origin=(0, y))

def halfplane(point, direction, size=5000):
    """Everything on the side of the line through `point` that `direction` points to."""
    dx, dy = direction
    length = math.hypot(dx, dy)
    dx, dy = dx / length, dy / length
    px, py = point
    tx, ty = -dy, dx
    return P([(px + tx * size, py + ty * size), (px + tx * size + dx * size, py + ty * size + dy * size),
              (px - tx * size + dx * size, py - ty * size + dy * size), (px - tx * size, py - ty * size)])

def ring(region, width):
    return region.difference(region.buffer(-width, quad_segs=48, join_style="round"))

def shade(direction):
    """How much a direction faces away from the light: 0 facing it, 1 facing away."""
    dx, dy = direction
    length = math.hypot(dx, dy) or 1
    return max(0.0, min(1.0, (1 - (dx * LIGHT[0] + dy * LIGHT[1]) / length) / 2))

# ---------------------------------------------------------------- the figure model

class Part:
    """One shape of a figure.
    black: a filled shape (★ ✦ ♜): its shadow half solid, its lit half open inside the contour. Else an open shape
      (☆ ✧ ♡): its line heavier on its shadow side.
    split: the shadow side: (point, direction towards the shadow), or (a polygon, None) for a shadow of any shape.
      A filled shape without one is solid ink.
    lines: detail pen lines, [(points, width factor)]: inked on open ground, cut out of solid ink.
    solid: solid ink whatever else (lines, small parts).
    cut: the part cuts a gap of this width round itself out of the parts drawn before it.
    minus: cut out of this part's ink once it is drawn (an arrow's open end).
    contour: a filled shape's contour, where its lit half is open (default W["contour"])."""
    def __init__(self, region, black=True, split=None, lines=(), solid=False, cut=0, minus=None, contour=None):
        self.contour = contour or W["contour"]
        self.region = region
        self.black = black
        self.split = split
        self.lines = list(lines)
        self.solid = solid
        self.cut = cut
        self.minus = minus

    def shadow(self):
        point, direction = self.split
        if direction is None:
            return self.region.intersection(point)
        return self.region.intersection(halfplane(point, direction))

def _lines(part):
    shapes = [stroke(points, W["detail"] * factor) for points, factor in part.lines]
    return union(*shapes).intersection(part.region.buffer(-W["contour"] * 0.5)) if shapes else GeometryCollection()

def engraved(part):
    region = part.region
    if part.solid or (part.black and part.split is None):
        shape = region.difference(_lines(part)) if part.lines else region
    elif part.black:
        shadow = part.shadow()
        shape = union(ring(region, part.contour), shadow)
        details = _lines(part)
        if not details.is_empty:
            shape = union(shape.difference(details.intersection(shadow)), details.difference(shadow))
    else:
        # The hole is the inside moved towards the light, so the line is thickest where it faces away from it.
        inner = region.buffer(-W["contour"], quad_segs=48)
        hole = inner.intersection(move(inner, LIGHT[0] * W["shade"], LIGHT[1] * W["shade"]))
        shape = union(region.difference(hole), _lines(part))
    if part.minus is not None:
        shape = shape.difference(part.minus)
    return shape

def render(parts):
    ink = GeometryCollection()
    for part in parts:
        if part.cut:
            ink = ink.difference(part.region.buffer(part.cut, quad_segs=48))
        ink = union(ink, engraved(part))
    return ink.buffer(0)

# ---------------------------------------------------------------- output

def svg_path(shape, tolerance=0.35):
    """SVG path data: every polygon's outer ring clockwise on screen and its holes the other way (so the non-zero
    rule fills it as the even-odd rule would), coordinates to 1/10 unit."""
    shape = shape.buffer(0)
    if shape.is_empty:
        return ""
    polygons = [shape] if isinstance(shape, Polygon) else [g for g in getattr(shape, "geoms", []) if isinstance(g, Polygon)]
    polygons.sort(key=lambda p: (round(p.bounds[1], 1), round(p.bounds[0], 1)))
    fmt = lambda v: f"{v:.1f}".rstrip("0").rstrip(".") if abs(v) >= 0.05 else "0"
    out = []
    for polygon in polygons:
        polygon = orient(polygon.simplify(tolerance, preserve_topology=True), sign=-1.0)
        for ring_ in [polygon.exterior, *polygon.interiors]:
            coords = list(ring_.coords)[:-1]
            if len(coords) >= 3:
                out.append("M" + " ".join(f"{fmt(x)} {fmt(y)}" for x, y in coords) + "Z")
    return "".join(out)

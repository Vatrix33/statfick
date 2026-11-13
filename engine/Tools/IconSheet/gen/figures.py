"""The 28 symbols' figures, each built centred at about (0, 0) at about the size of the glyph Chromium drew for it, in
1/1000 em, y down (build.py centres each on the old glyph's ink box). iconlib.render() inks them."""
import math
from iconlib import *

# ---------------------------------------------------------------- shared pieces

def star(n, r_out, r_in, rotation=-90):
    """A straight-sided star: n points at r_out, the inner corners at r_in."""
    pts = []
    for i in range(2 * n):
        r = r_out if i % 2 == 0 else r_in
        a = math.radians(rotation + i * 180 / n)
        pts.append((r * math.cos(a), r * math.sin(a)))
    return P(pts), pts[0::2]

def four_star(rv, rh, waist):
    """A four-pointed star with concave sides: tips at (0, -rv), (rh, 0), (0, rv), (-rh, 0), the sides curving in
    through points `waist` from the centre on the diagonals."""
    tips = [(0, -rv), (rh, 0), (0, rv), (-rh, 0)]
    pts = []
    for i in range(4):
        a, b = tips[i], tips[(i + 1) % 4]
        mid = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
        d = math.hypot(*mid)
        w = (mid[0] / d * waist, mid[1] / d * waist)
        control = (w[0] * 2 - mid[0], w[1] * 2 - mid[1])  # the quadratic passes through w at t = 1/2
        pts += bezier(a, control, b, n=48)[:-1]
    return P(pts), tips

def pinwheel(tips, centre=(0, 0)):
    """The compass rose's split (map-ink.js, compass()): each point's clockwise half in shadow, as (polygon, None)."""
    halves = []
    for i, (tx, ty) in enumerate(tips):
        nx, ny = tips[(i + 1) % len(tips)]
        ax, ay = tx - centre[0], ty - centre[1]
        bx, by = nx - centre[0], ny - centre[1]
        la, lb = math.hypot(ax, ay), math.hypot(bx, by)
        ax, ay, bx, by = ax / la, ay / la, bx / lb, by / lb
        mx, my = ax + bx, ay + by
        m = math.hypot(mx, my)
        halves.append(P([centre, (centre[0] + ax * 3000, centre[1] + ay * 3000), (centre[0] + mx / m * 3000, centre[1] + my / m * 3000)]))
    return (union(*halves), None)

WEST = ((0, 0), (-1, 0))  # the west half in shadow (a tall shape's west face, as the map's peaks and pillars)

def west(x=0):
    return ((x, 0), (-1, 0))

def lower_left(point=(0, 0)):
    return (point, (-LIGHT[0], -LIGHT[1]))

def diamond(r, cx=0, cy=0):
    return P([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)])

def arrowhead(tip, back, half_width):
    """A compass point used as an arrow's head: its tip, the middle of its back, half its back's width; split along
    its axis, the half facing away from the light in shadow."""
    (tx, ty), (bx, by) = tip, back
    dx, dy = tx - bx, ty - by
    length = math.hypot(dx, dy)
    nx, ny = -dy / length, dx / length
    head = P([tip, (bx + nx * half_width, by + ny * half_width), (bx - nx * half_width, by - ny * half_width)])
    side = (nx, ny) if shade((nx, ny)) >= shade((-nx, -ny)) else (-nx, -ny)
    return Part(head, black=True, split=((bx, by), side))

# ---------------------------------------------------------------- arrows

def top_semicircle_arrow():
    """↶: an arc over the top, its head at the left end pointing down."""
    cx, cy, rx, ry = 70, 150, 448, 318
    arc_part = Part(ellipse(cx, cy, rx, ry), black=False, minus=rect(-2000, cy - 10, 2000, 2000))
    # The head at the left end, on the arc's centre line, pointing down and a little out.
    end_x = cx - rx + (W["contour"] + W["shade"]) / 2
    head = arrowhead((end_x - 60, cy + 210), (end_x + 4, cy - 50), 180)
    return [arc_part, head]

def open_circle_arrow():
    """↺: a circle open at the top left, its head at the top pointing left (anticlockwise)."""
    cx, cy, r = 0, 70, 364
    gap = P([(cx, cy), (cx + 3000 * math.cos(math.radians(-90)), cy + 3000 * math.sin(math.radians(-90))),
             (cx + 3000 * math.cos(math.radians(-125)), cy + 3000 * math.sin(math.radians(-125))),
             (cx + 3000 * math.cos(math.radians(-160)), cy + 3000 * math.sin(math.radians(-160)))])
    ring_part = Part(circle(cx, cy, r), black=False, minus=gap)
    mid = cy - r + W["contour"] / 2 + 6
    head = arrowhead((cx - 230, mid), (cx + 20, mid), 190)
    return [ring_part, head]

def mirrored(parts):
    out = []
    for part in parts:
        split = part.split
        if split is not None:
            point, direction = split
            if direction is None:
                split = (mirror_x(point), None)
            else:
                split = ((-point[0], point[1]), (-direction[0], direction[1]))
                # A mirrored split must still face away from the light.
                if shade(split[1]) < shade((-split[1][0], -split[1][1])):
                    split = (split[0], (-split[1][0], -split[1][1]))
        out.append(Part(mirror_x(part.region), black=part.black, split=split, solid=part.solid, cut=part.cut,
                        minus=mirror_x(part.minus) if part.minus is not None else None,
                        lines=[([(-x, y) for x, y in pts], f) for pts, f in part.lines]))
    return out

def upwards_white_arrow():
    """⇧: an open arrow, its head the width of the glyph, its shaft a third of it."""
    w, h = 849, 784
    top, bottom = -h / 2, h / 2
    head_bottom = top + h * 0.47
    shaft = w * 0.34
    shape = P([(0, top), (w / 2, head_bottom), (shaft / 2, head_bottom), (shaft / 2, bottom), (-shaft / 2, bottom), (-shaft / 2, head_bottom), (-w / 2, head_bottom)])
    return [Part(shape, black=False)]

def down_arrowhead():
    """⌄: a chevron, its west stroke the heavier (it faces the shadow)."""
    w, h = 584, 348
    t = W["contour"] + 8
    apex = (0, h / 2 - t * 0.7)
    chevron = stroke([(-w / 2 + t / 2, -h / 2 + t / 2), apex, (w / 2 - t / 2, -h / 2 + t / 2)], t, cap="flat", join="mitre")
    # The west stroke's underside faces away from the light: a shade line along it.
    dx, dy = apex[0] - (-w / 2 + t / 2), apex[1] - (-h / 2 + t / 2)
    length = math.hypot(dx, dy)
    nx, ny = -dy / length, dx / length  # the west stroke's lower-left side
    extra = W["shade"] * 0.45
    under = move(stroke([(-w / 2 + t / 2, -h / 2 + t / 2), apex], t, cap="flat"), nx * extra, ny * extra).intersection(halfplane(apex, (-1, 0)))
    return [Part(union(chevron, under), solid=True)]

# ---------------------------------------------------------------- geometric shapes

def position_indicator():
    """⌖: a ring with a cross through it, reaching past the ring."""
    r, reach, t = 172, 245, 52
    cross = union(stroke([(-reach, 0), (reach, 0)], t, cap="flat"), stroke([(0, -reach), (0, reach)], t, cap="flat"))
    return [Part(circle(0, 0, r), black=False), Part(cross, solid=True)]

def square_with_square():
    """▣: an open square holding a filled one, split corner to corner."""
    s = 357
    inner = 168
    return [Part(rect(-s, -s, s, s), black=False),
            Part(rect(-inner, -inner, inner, inner), black=True, split=lower_left())]

def square_with_line():
    """◫: an open square halved by a vertical line."""
    s = 357
    return [Part(rect(-s, -s, s, s), black=False), Part(rect(-W["contour"] / 2 - 4, -s + 20, W["contour"] / 2 + 4, s - 20), solid=True)]

def triangle_up():
    """▲: a peak, its west face in shadow (the map's peak marks)."""
    w, h = 585, 585
    return [Part(P([(0, -h / 2), (w / 2, h / 2), (-w / 2, h / 2)]), black=True, split=WEST)]

def triangle_down():
    w, h = 585, 585
    return [Part(P([(0, h / 2), (w / 2, -h / 2), (-w / 2, -h / 2)]), black=True, split=WEST)]

def black_diamond():
    return [Part(diamond(477), black=True, split=WEST)]

def white_diamond():
    return [Part(diamond(477), black=False)]

def diamond_in_diamond():
    """◈ (as approved in the sample)."""
    return [Part(diamond(439), black=False, split=((0, 0), (-1, 0))),
            Part(diamond(162), black=True, split=((0, 0), (-1, 0)))]

def dotted_circle():
    """◌: sixteen dots, those facing away from the light a little larger (an engraver's graded dots)."""
    radius, dots = 218, 16
    shapes = []
    for i in range(dots):
        a = math.radians(-90 + i * 360 / dots)
        d = (math.cos(a), math.sin(a))
        r = 24 + 14 * shade(d)
        shapes.append(circle(radius * d[0], radius * d[1], r))
    return [Part(union(*shapes), solid=True)]

def bullseye():
    """◎: two open rings."""
    return [Part(circle(0, 0, 367), black=False), Part(circle(0, 0, 178), black=False)]

def black_star():
    """★: a five-pointed star split as the compass rose's points, its lit halves' line heavy so it reads as filled
    beside ☆ at 11-16 px (the user's answer, 2026-11-13)."""
    shape, tips = star(5, 454, 454 * 0.40)
    return [Part(shape, black=True, split=pinwheel(tips), contour=W["heavy"])]

def white_star():
    shape, tips = star(5, 454, 454 * 0.40)
    return [Part(shape, black=False)]

def trigram():
    """☰: three bars."""
    w, t, gap = 424, 76, 100
    bars = union(*(rect(-w / 2, y - t / 2, w / 2, y + t / 2) for y in (-(t + gap), 0, t + gap)))
    return [Part(bars, solid=True)]

# ---------------------------------------------------------------- chess pieces

def queen():
    """♛: a crown of five spikes with balls, over a band and a foot."""
    b = 400  # the foot's bottom; the balls' tops at about -400
    foot = rounded(-262, b - 100, 262, b, 46)
    band = rect(-282, b - 178, 282, b - 96)
    tips = [(-322, -196), (-168, -276), (0, -305), (168, -276), (322, -196)]
    # Five slender spikes fanning out from the band (thin, so solid ink), a ball on each.
    bases = [-206, -103, 0, 103, 206]
    spikes = union(*(P([(x - 58, b - 170), (tx, ty), (x + 58, b - 170)]) for x, (tx, ty) in zip(bases, tips)))
    balls = union(*(circle(x, y - 46, 50) for x, y in tips))
    lines = [([(-250, b - 100), (250, b - 100)], 1.0)]
    return [Part(union(spikes, balls), solid=True), Part(union(foot, band), black=True, split=WEST, lines=lines)]

def rook():
    """♜ (as approved in the sample)."""
    b = 360
    base = union(rect(-324, b, 324, b - 74), rect(-268, b - 74, 268, b - 136))
    body = P([(-212, b - 136), (-178, b - 470), (178, b - 470), (212, b - 136)])
    collar = rect(-252, b - 470, 252, b - 540)
    top = rect(-272, b - 540, 272, b - 719)
    for x0, x1 in [(-142, -66), (66, 142)]:
        top = top.difference(rect(x0, b - 719 - 10, x1, b - 625))
    shape = union(base, body, collar, top)
    lines = [([(-230, b - 74), (230, b - 74)], 1.0), ([(-180, b - 136), (180, b - 136)], 1.0), ([(-170, b - 470), (170, b - 470)], 1.0), ([(-230, b - 540), (230, b - 540)], 1.0)]
    return [Part(shape, black=True, split=((-20, 0), (-1, 0)), lines=lines)]

def pawn():
    """♟: a ball on a collar over a flared body and a broad foot."""
    b = 508
    foot = rounded(-316, b - 150, 316, b, 60)
    plinth = rounded(-250, b - 214, 250, b - 140, 24)
    left = path(bezier((-118, -150), (-120, 40), (-200, 160), (-236, b - 214)))
    body = P(left + [(236, b - 214)] + [(-x, y) for x, y in reversed(left)][1:])
    collar = rounded(-176, -212, 176, -146, 33)
    head = circle(0, -340, 166)
    lines = [([(-280, b - 150), (280, b - 150)], 1.0), ([(-220, b - 214), (220, b - 214)], 1.0)]
    return [Part(union(foot, plinth, body, collar), black=True, split=WEST, lines=lines),
            Part(head, black=True, split=WEST)]

# ---------------------------------------------------------------- ♡ ⚔ ⚙ (as approved in the sample)

def heart(w=860, h=748):
    top, bottom = -h / 2, h / 2
    dip = top + h * 0.2
    half = path(
        bezier((0, dip), (w * 0.02, top - h * 0.02), (w * 0.47, top - h * 0.06), (w * 0.5, top + h * 0.3)),
        bezier((w * 0.5, top + h * 0.3), (w * 0.52, top + h * 0.6), (w * 0.2, bottom - h * 0.12), (0, bottom)))
    left = [(-x, y) for x, y in reversed(half)]
    return P(left[:-1] + half[:-1])

def white_heart():
    return [Part(heart(), black=False, split=((0, 0), (-1, 0.5)))]

def sword(length=1300, blade_w=150, angle=45):
    """One sword, its tip up and turned `angle` degrees clockwise, its middle at (0, 0): blade, guard, grip, pommel."""
    L = length
    pommel_r = 64
    y_pommel = L / 2 - pommel_r
    y_guard = L / 2 - 310
    guard_half, guard_t = 190, 66
    tip = -L / 2
    bw = blade_w / 2
    blade = P([(-bw, y_guard - guard_t / 2), (-bw * 0.9, tip + 250), (0, tip), (bw * 0.9, tip + 250), (bw, y_guard - guard_t / 2)])
    guard = union(stroke([(-guard_half, y_guard + 10), (0, y_guard), (guard_half, y_guard + 10)], guard_t),
                  circle(-guard_half, y_guard + 10, guard_t * 0.64), circle(guard_half, y_guard + 10, guard_t * 0.64))
    grip = stroke([(0, y_guard + guard_t / 2), (0, y_pommel - pommel_r * 0.5)], 74, cap="flat")
    pommel = circle(0, y_pommel, pommel_r)
    return [turn(p, angle) for p in (blade, guard, grip, pommel)]

def crossed_swords():
    parts = []
    for i, angle in enumerate((-45, 45)):  # the back sword's tip to the upper left, the front one's to the upper right
        blade, guard, grip, pommel = sword(angle=angle)
        axis = (math.sin(math.radians(angle)), -math.cos(math.radians(angle)))
        normal = (-axis[1], axis[0])
        if normal[0] > 0:
            normal = (-normal[0], -normal[1])  # the half facing west in shadow
        parts.append(Part(union(blade, guard, grip, pommel), black=True, split=((0, 0), normal), cut=W["knock"] if i else 0))
    return parts

def gear():
    r_tip, r_root, teeth = 252, 194, 8
    pts = []
    for i in range(teeth):
        c = i * 360 / teeth - 90
        half_top, half_root = 11.0, 17.5  # degrees
        pts += arc(0, 0, r_root, c - 360 / teeth / 2, c - half_root, 6)
        pts += [(r_tip * math.cos(math.radians(c - half_top)), r_tip * math.sin(math.radians(c - half_top)))]
        pts += arc(0, 0, r_tip, c - half_top, c + half_top, 6)
        pts += [(r_root * math.cos(math.radians(c + half_root)), r_root * math.sin(math.radians(c + half_root)))]
        pts += arc(0, 0, r_root, c + half_root, c + 360 / teeth / 2, 6)
    body = P(pts).buffer(10, join_style="round").buffer(-10, join_style="round")
    return [Part(body.difference(circle(0, 0, 64)), black=True, split=lower_left())]

# ---------------------------------------------------------------- stars

def star4(black=True):
    """✦ (as approved in the sample): the compass rose's split points. ✧: the same star open (no lines inside it:
    at 16-18 px they ran together into a dark centre and ✧ looked like ✦)."""
    rv, rh = 286, 270
    shape, tips = four_star(rv, rh, waist=150)
    return [Part(shape, black=black, split=pinwheel(tips))]

def eight_star():
    """✸: eight straight points, split as an eight-point compass rose, its lit halves' line heavy so it isn't a faint
    dot at its 9 px (the user's answer, 2026-11-13)."""
    shape, tips = star(8, 238, 238 * 0.5)
    return [Part(shape, black=True, split=pinwheel(tips), contour=W["heavy"])]

def asterisk16():
    """✺: sixteen pointed petals from a solid heart, those facing away from the light a little heavier (an engraver's
    graded strokes). (Rays widening outwards round an empty middle read as a ring at 16 px.)"""
    rays = []
    r_mid, r_tip = 70, 245
    for i in range(16):
        a = math.radians(-90 + i * 22.5)
        d = (math.cos(a), math.sin(a))
        n = (-d[1], d[0])
        w = (52 + 30 * shade(d)) / 2
        rays.append(P([(0, 0), (d[0] * r_mid + n[0] * w, d[1] * r_mid + n[1] * w), (d[0] * r_tip, d[1] * r_tip), (d[0] * r_mid - n[0] * w, d[1] * r_mid - n[1] * w)]))
    return [Part(union(*rays), solid=True)]

FIGURES = {
    "↶": top_semicircle_arrow,
    "↺": open_circle_arrow,
    "↻": lambda: mirrored(open_circle_arrow()),
    "⇧": upwards_white_arrow,
    "⌄": down_arrowhead,
    "⌖": position_indicator,
    "▣": square_with_square,
    "▲": triangle_up,
    "▼": triangle_down,
    "◆": black_diamond,
    "◇": white_diamond,
    "◈": diamond_in_diamond,
    "◌": dotted_circle,
    "◎": bullseye,
    "◫": square_with_line,
    "★": black_star,
    "☆": white_star,
    "☰": trigram,
    "♛": queen,
    "♜": rook,
    "♟": pawn,
    "♡": white_heart,
    "⚔": crossed_swords,
    "⚙": gear,
    "✦": lambda: star4(True),
    "✧": lambda: star4(False),
    "✸": eight_star,
    "✺": asterisk16,
}

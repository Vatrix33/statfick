# motifs.py : the nine races' emblem motifs as sketches, two candidates each (GM2 step 10; the user's Q37: nine new
# motifs, each proposed as a sketch first; none of the six old seals' motifs is kept: the crowned shield and sword, the
# leaf and moon, the crescent with rapier and star, the hammer over mountain and anvil, the acorn, the slit eye in a scale;
# nor the three placeholders' paw, mountain and waves). Shapes in the unit field (sketchkit.py).
import math

from sketchkit import arc, both, both_lines, catmull, circle, ellipse, fill, line, mirror

M = []     # (race, key, name, what it shows and why, elements)


def motif(race, key, name, why):
    def keep(make):
        M.append((race, key, name, why, make()))
        return make
    return keep


# ---- human ---------------------------------------------------------------------------------------------------------------
@motif("human", "A", "The keep", "A castle keep flying a banner: builders and rulers (their passive is Rulers of the World).")
def human_keep():
    e = [fill([(-0.78, -0.60), (-0.40, -0.52), (0.0, -0.56), (0.40, -0.52), (0.78, -0.60), (0.66, -0.80), (-0.66, -0.80)], "mid", smooth=True)]
    for s in (-1, 1):                                    # the curtain walls with their merlons
        e.append(fill([(s * 0.26, -0.58), (s * 0.66, -0.58), (s * 0.66, -0.26), (s * 0.26, -0.26)], "mid"))
        for x in (0.34, 0.50, 0.66):
            e.append(fill([(s * (x - 0.07), -0.26), (s * x, -0.26), (s * x, -0.15), (s * (x - 0.07), -0.15)], "mid"))
    e.append(fill([(-0.30, -0.60), (0.30, -0.60), (0.25, 0.20), (-0.25, 0.20)], "hi"))                       # the tower
    e.append(fill([(-0.33, 0.14), (0.33, 0.14), (0.33, 0.24), (-0.33, 0.24)], "hi"))
    for x in (-0.33, -0.10, 0.13):
        e.append(fill([(x, 0.24), (x + 0.20, 0.24), (x + 0.20, 0.40), (x, 0.40)], "hi"))
    e.append(fill([(-0.10, -0.60), (-0.10, -0.36)] + arc(0.0, -0.36, 0.10, 180, 0, 10) + [(0.10, -0.60)], "lo"))    # the gate
    e.append(fill([(-0.035, -0.12), (0.035, -0.12), (0.035, 0.06), (-0.035, 0.06)], "lo"))                   # a window slit
    e.append(line([(0.03, 0.40), (0.03, 0.86)], 0.035, tone="mid", smooth=False))                            # the pole
    e.append(fill([(0.05, 0.86), (0.46, 0.80), (0.34, 0.71), (0.48, 0.60), (0.05, 0.62)], "acc"))            # the banner
    return e


@motif("human", "B", "The compass star", "An eight-pointed compass star in a ring: the folk who chart and claim the world.")
def human_star():
    e = [line(circle(0, 0, 0.56) + [circle(0, 0, 0.56)[0]], 0.07, tone="mid", smooth=False)]
    star = []
    for k in range(8):
        a = math.radians(45 * k)
        r = 0.86 if k % 2 == 0 else 0.46
        star.append((r * math.sin(a), r * math.cos(a)))
        b = math.radians(45 * k + 22.5)
        star.append((0.17 * math.sin(b), 0.17 * math.cos(b)))
    e.append(fill(star, "hi"))
    for k in range(4):                                   # each long point's shaded half
        a = math.radians(90 * k)
        b = math.radians(90 * k + 22.5)
        e.append(fill([(0, 0), (0.86 * math.sin(a), 0.86 * math.cos(a)), (0.17 * math.sin(b), 0.17 * math.cos(b))], "mid", outline=False))
    e.append(fill(circle(0, 0, 0.07), "lo"))
    return e


# ---- elf -----------------------------------------------------------------------------------------------------------------
@motif("elf", "A", "The stag", "A stag's head crowned with antlers: the old forest's lord (Sylvan Heritage).")
def elf_stag():
    e = []
    e += both_lines([(0.10, 0.08), (0.30, 0.22), (0.52, 0.40), (0.58, 0.62), (0.48, 0.86)], 0.12, 0.04, tone="mid")       # the beams
    e += both_lines([(0.24, 0.18), (0.19, 0.38), (0.24, 0.58)], 0.085, 0.03, tone="mid")                                  # the tines
    e += both_lines([(0.48, 0.36), (0.37, 0.52), (0.34, 0.72)], 0.08, 0.03, tone="mid")
    e += both_lines([(0.57, 0.56), (0.72, 0.64), (0.80, 0.80)], 0.075, 0.03, tone="mid")
    e += both([(0.18, 0.06), (0.40, 0.20), (0.58, 0.14), (0.44, 0.00), (0.26, -0.06)], tone="hi", smooth=True)            # the ears
    e += both([(0.26, 0.04), (0.40, 0.13), (0.50, 0.11), (0.40, 0.03)], tone="lo", smooth=True, outline=False)
    head = [(0.0, 0.18), (0.15, 0.14), (0.24, 0.00), (0.21, -0.20), (0.14, -0.44), (0.11, -0.62), (0.0, -0.74)]
    e.append(fill(head + mirror(head)[1:-1], "hi", smooth=True))
    e += both(ellipse(0.12, -0.10, 0.058, 0.034, 25), tone="lo", outline=False)                                           # the eyes
    e.append(fill([(-0.07, -0.60), (0.07, -0.60), (0.035, -0.70), (-0.035, -0.70)], "lo", smooth=True, outline=False))    # the nose
    return e


@motif("elf", "B", "The great tree", "An ancient oak, crown and roots: a people as old as the woods they keep.")
def elf_tree():
    e = []
    e += both_lines([(0.08, -0.50), (0.30, -0.62), (0.58, -0.60)], 0.09, 0.025, tone="mid")                               # the roots
    e += both_lines([(0.04, -0.52), (0.16, -0.72), (0.34, -0.80)], 0.07, 0.02, tone="mid")
    crown = []
    for k in range(70):                                  # a broad crown, its edge in lobes
        a = 2 * math.pi * k / 70
        r = 1.0 + 0.085 * math.cos(9 * a)
        crown.append((0.74 * r * math.cos(a), 0.34 + 0.47 * r * math.sin(a)))
    e.append(fill(crown, "hi"))
    e.append(fill([(-0.11, 0.10), (0.11, 0.10), (0.09, -0.34), (0.18, -0.58), (-0.18, -0.58), (-0.09, -0.34)], "mid", smooth=True))   # the trunk
    e += both_lines([(0.03, 0.06), (0.22, 0.26), (0.46, 0.34)], 0.075, 0.02, tone="mid")                                  # the boughs into the crown
    e += both_lines([(0.02, 0.10), (0.12, 0.38), (0.26, 0.58)], 0.065, 0.02, tone="mid")
    e.append(line([(0.0, 0.08), (0.0, 0.40), (0.02, 0.68)], 0.07, 0.02, tone="mid"))
    return e


# ---- dark elf ------------------------------------------------------------------------------------------------------------
@motif("dark-elf", "A", "The spider", "A spider at the heart of its web: patient hunters of the sunless deep.")
def dark_elf_spider():
    e = []
    for k in range(8):                                   # the web's spokes and rings
        a = math.radians(45 * k + 22.5)
        e.append(line([(0.0, 0.04), (0.98 * math.cos(a), 0.04 + 0.98 * math.sin(a))], 0.022, tone="mid", smooth=False, outline=False))
    for r in (0.34, 0.60, 0.86):
        for k in range(8):
            a0, a1 = math.radians(45 * k + 22.5), math.radians(45 * k + 67.5)
            p0, p1 = (r * math.cos(a0), 0.04 + r * math.sin(a0)), (r * math.cos(a1), 0.04 + r * math.sin(a1))
            mid_ = ((p0[0] + p1[0]) / 2 * 0.90, 0.04 + ((p0[1] + p1[1]) / 2 - 0.04) * 0.90)
            e.append(line([p0, mid_, p1], 0.02, tone="mid", outline=False))
    legs = [[(0.06, 0.20), (0.24, 0.42), (0.30, 0.66)], [(0.09, 0.15), (0.36, 0.26), (0.58, 0.40)],
            [(0.09, 0.08), (0.40, 0.04), (0.60, -0.14)], [(0.07, 0.02), (0.30, -0.22), (0.38, -0.52)]]
    for leg in legs:
        e += both_lines(leg, 0.055, 0.02, tone="hi")
    e.append(fill(ellipse(0.0, -0.14, 0.17, 0.23), "hi"))
    e.append(fill(circle(0.0, 0.13, 0.11), "hi"))
    e.append(fill([(0.0, -0.02), (0.07, -0.14), (0.0, -0.28), (-0.07, -0.14)], "acc", outline=False))
    return e


@motif("dark-elf", "B", "The black sun", "An eclipsed sun ringed with rays: the only sun their caverns know.")
def dark_elf_sun():
    e = []
    for k in range(16):
        a = math.radians(22.5 * k)
        r = 0.92 if k % 2 == 0 else 0.70
        w = math.radians(6.5)
        e.append(fill([(0.50 * math.sin(a - w), 0.50 * math.cos(a - w)), (r * math.sin(a), r * math.cos(a)), (0.50 * math.sin(a + w), 0.50 * math.cos(a + w))], "hi"))
    e.append(fill(circle(0, 0, 0.46), "mid"))
    e.append(fill(circle(0, 0, 0.37), "lo", outline=False))
    e.append(fill(arc(0, 0, 0.37, 120, 250, 16) + arc(0.10, 0.03, 0.37, 235, 135, 16), "acc", outline=False))
    return e


# ---- dwarf ---------------------------------------------------------------------------------------------------------------
@motif("dwarf", "A", "The helm and beard", "A horned helm over a braided beard: the dwarf himself, armoured (Mountainborn).")
def dwarf_helm():
    e = []
    e += both_lines([(0.38, 0.30), (0.58, 0.32), (0.72, 0.50), (0.70, 0.72)], 0.15, 0.03, tone="hi")                   # the horns
    beard = [(0.0, -0.20), (0.22, -0.14), (0.42, -0.02), (0.47, -0.30), (0.34, -0.60), (0.14, -0.80), (0.0, -0.88)]
    e.append(fill(beard + mirror(beard)[1:-1], "hi", smooth=True))
    for x in (-0.20, 0.0, 0.20):                         # the braids
        e.append(line([(x, -0.30), (x * 0.9, -0.52), (x * 0.6, -0.74)], 0.025, tone="lo", outline=False))
        for y in (-0.38, -0.50, -0.62):
            e.append(line([(x * (1 - (-(y) - 0.30) * 0.6) - 0.05, y + 0.03), (x * (1 - (-(y) - 0.30) * 0.6) + 0.05, y - 0.03)], 0.02, tone="lo", smooth=False, outline=False))
    e += both_lines([(0.02, -0.14), (0.20, -0.20), (0.40, -0.14), (0.50, -0.02)], 0.10, 0.03, tone="hi")                # the moustache
    dome = [(0.0, 0.72), (0.26, 0.64), (0.42, 0.40), (0.44, 0.10), (0.0, 0.06)]
    e.append(fill(dome + mirror(dome)[1:-1], "mid", smooth=True))
    e.append(line([(0.0, 0.72), (0.0, 0.14)], 0.07, tone="hi", smooth=False))
    e.append(fill([(-0.47, 0.02), (0.47, 0.02), (0.47, 0.15), (-0.47, 0.15)], "hi"))
    e.append(fill([(-0.065, 0.15), (0.065, 0.15), (0.045, -0.16), (-0.045, -0.16)], "hi"))
    for x in (-0.36, -0.20, 0.20, 0.36):
        e.append(fill(circle(x, 0.085, 0.028), "lo", outline=False))
    return e


@motif("dwarf", "B", "The gem", "A cut gem held in a forged ring: the wealth of the deep halls and the craft that sets it.")
def dwarf_gem():
    e = [line(circle(0, 0, 0.72) + [circle(0, 0, 0.72)[0]], 0.13, tone="mid", smooth=False)]
    for k in range(4):
        a = math.radians(90 * k + 45)
        e.append(fill(circle(0.72 * math.cos(a), 0.72 * math.sin(a), 0.075), "hi"))
    e.append(fill([(-0.46, 0.14), (-0.26, 0.40), (0.26, 0.40), (0.46, 0.14), (0.0, -0.52)], "hi"))
    e.append(fill([(-0.26, 0.40), (0.0, 0.40), (-0.13, 0.14), (-0.46, 0.14)], "acc", outline=False))
    e.append(fill([(0.13, 0.14), (0.46, 0.14), (0.0, -0.52)], "acc", outline=False))
    for seg in ([(-0.46, 0.14), (0.46, 0.14)], [(-0.26, 0.40), (-0.13, 0.14), (0.0, -0.52)], [(0.26, 0.40), (0.13, 0.14), (0.0, -0.52)],
                [(0.0, 0.40), (-0.13, 0.14)], [(0.0, 0.40), (0.13, 0.14)]):
        e.append(line(seg, 0.022, tone="lo", smooth=False, outline=False))
    return e


# ---- hobbit --------------------------------------------------------------------------------------------------------------
@motif("hobbit", "A", "The pipe", "A long pipe, its smoke rising in a curl and a ring: hearth, home and good company.")
def hobbit_pipe():
    e = []
    e.append(line([(-0.36, -0.04), (-0.46, 0.14), (-0.28, 0.30), (-0.40, 0.48), (-0.24, 0.64), (-0.08, 0.72), (0.02, 0.62), (-0.08, 0.54)], 0.085, 0.03, tone="mid"))
    e.append(line(circle(0.34, 0.56, 0.13) + [circle(0.34, 0.56, 0.13)[0]], 0.05, tone="mid", smooth=False))               # a smoke ring
    e.append(line([(-0.16, -0.50), (0.14, -0.44), (0.44, -0.26), (0.76, -0.02)], 0.10, 0.055, tone="hi"))                 # the stem
    e.append(fill([(-0.58, -0.12), (-0.60, -0.42), (-0.46, -0.62), (-0.24, -0.64), (-0.10, -0.46), (-0.12, -0.12)], "hi", smooth=True))
    e.append(fill(ellipse(-0.35, -0.10, 0.245, 0.07), "mid"))
    e.append(fill(ellipse(-0.35, -0.10, 0.165, 0.038), "lo", outline=False))
    return e


@motif("hobbit", "B", "The hare", "A hare in full leap: small, quick and lucky (Nimble Stature).")
def hobbit_hare():
    e = []
    e.append(line([(-0.84, -0.66), (-0.42, -0.62), (0.0, -0.66), (0.42, -0.62), (0.84, -0.66)], 0.04, tone="mid"))        # the ground
    for x in (-0.46, 0.06, 0.52):
        e.append(line([(x, -0.64), (x - 0.04, -0.50)], 0.035, 0.012, tone="mid", smooth=False))
        e.append(line([(x + 0.06, -0.64), (x + 0.11, -0.52)], 0.035, 0.012, tone="mid", smooth=False))
    e.append(fill([(0.50, 0.30), (0.36, 0.44), (0.10, 0.60), (-0.06, 0.60), (0.12, 0.44), (0.38, 0.26)], "hi", smooth=True))   # the far ear
    e.append(line([(-0.36, 0.02), (-0.60, -0.14), (-0.88, -0.16)], 0.20, 0.06, tone="hi"))                                # the hind legs, thrown back
    e.append(line([(-0.30, -0.06), (-0.52, -0.28), (-0.78, -0.34)], 0.16, 0.055, tone="hi"))
    e.append(line([(0.30, -0.02), (0.52, -0.20), (0.80, -0.26)], 0.12, 0.05, tone="hi"))                                  # the forelegs, reaching
    e.append(line([(0.22, -0.08), (0.40, -0.30), (0.66, -0.40)], 0.11, 0.05, tone="hi"))
    e.append(fill(circle(-0.56, 0.22, 0.085), "hi"))                                                                       # the scut
    body = [(-0.54, 0.02), (-0.50, 0.22), (-0.26, 0.38), (0.06, 0.36), (0.34, 0.26), (0.46, 0.10), (0.34, -0.08), (0.04, -0.14), (-0.30, -0.16)]
    e.append(fill(body, "hi", smooth=True))
    e.append(fill([(0.36, 0.16), (0.44, 0.32), (0.60, 0.36), (0.76, 0.26), (0.84, 0.14), (0.74, 0.06), (0.56, 0.04)], "hi", smooth=True))   # the head
    e.append(fill([(0.56, 0.30), (0.44, 0.48), (0.20, 0.70), (0.04, 0.72), (0.20, 0.52), (0.44, 0.28)], "hi", smooth=True))   # the near ear
    e.append(fill([(0.48, 0.34), (0.38, 0.48), (0.20, 0.64), (0.30, 0.50)], "lo", smooth=True, outline=False))
    e.append(fill(circle(0.64, 0.23, 0.032), "lo", outline=False))
    return e


# ---- lizardman -----------------------------------------------------------------------------------------------------------
@motif("lizardman", "A", "The lizard", "A lizard seen from above, its tail curled: quick, low to the mud (Mud Runner).")
def lizard_coil():
    e = []
    spine = catmull([(0.10, 0.60), (0.04, 0.44), (-0.12, 0.26), (-0.04, 0.02), (0.16, -0.16), (0.22, -0.40), (0.06, -0.62), (-0.22, -0.64), (-0.40, -0.46),
                     (-0.34, -0.26), (-0.20, -0.30)], False, 10)
    n = len(spine)

    def width(t):
        if t < 0.12:
            return 0.13 + (0.22 - 0.13) * t / 0.12
        if t < 0.42:
            return 0.22
        return 0.22 * max(0.0, 1 - (t - 0.42) / 0.58) ** 0.9 + 0.012

    def limb(base, knee, foot):
        e.append(line([base, knee, foot], 0.085, 0.05, tone="hi"))
        dx, dy = foot[0] - knee[0], foot[1] - knee[1]
        size = math.hypot(dx, dy)
        dx, dy = dx / size, dy / size
        for spread in (-0.55, 0.0, 0.55):
            c, k = math.cos(spread), math.sin(spread)
            e.append(line([foot, (foot[0] + 0.12 * (dx * c - dy * k), foot[1] + 0.12 * (dx * k + dy * c))], 0.035, 0.015, tone="hi", smooth=False))

    limb((-0.10, 0.28), (-0.38, 0.34), (-0.50, 0.54))                                                                     # the forelegs
    limb((-0.02, 0.24), (0.26, 0.24), (0.42, 0.40))
    limb((0.08, -0.14), (-0.20, -0.06), (-0.40, -0.14))                                                                   # the hind legs
    limb((0.22, -0.26), (0.50, -0.20), (0.62, -0.38))
    left, right = [], []
    for i, (x, y) in enumerate(spine):
        ax, ay = spine[max(0, i - 1)]
        bx, by = spine[min(n - 1, i + 1)]
        dx, dy = bx - ax, by - ay
        size = math.hypot(dx, dy) or 1.0
        half = width(i / (n - 1)) / 2
        left.append((x - dy / size * half, y + dx / size * half))
        right.append((x + dy / size * half, y - dx / size * half))
    e.append(fill(left + right[::-1], "hi"))
    e.append(fill([(0.02, 0.56), (0.02, 0.70), (0.12, 0.86), (0.20, 0.86), (0.24, 0.70), (0.18, 0.56)], "hi", smooth=True))   # the head
    e.append(fill(circle(0.06, 0.72, 0.022), "lo", outline=False))
    e.append(fill(circle(0.19, 0.72, 0.022), "lo", outline=False))
    for i in range(8, n - 14, 5):                         # the ridge down its back
        e.append(fill(ellipse(*spine[i], 0.03, 0.018), "acc", outline=False))
    return e


@motif("lizardman", "B", "The crested head", "A lizardman's head in profile, crest raised: the warrior of the marsh.")
def lizard_head():
    e = []
    for spike in ([(-0.16, 0.30), (-0.30, 0.70), (0.00, 0.36)], [(-0.32, 0.20), (-0.62, 0.52), (-0.18, 0.32)],
                  [(-0.40, 0.02), (-0.78, 0.22), (-0.34, 0.20)], [(-0.42, -0.18), (-0.78, -0.10), (-0.40, 0.04)]):
        e.append(fill(spike, "acc"))
    head = [(-0.66, -1.10), (-0.46, -0.40), (-0.42, 0.02), (-0.26, 0.28), (0.04, 0.36), (0.40, 0.22), (0.72, 0.08), (0.78, -0.04), (0.64, -0.14),
            (0.32, -0.18), (0.14, -0.30), (0.06, -0.56), (0.10, -1.10)]                                # the neck runs down under the rim
    e.append(fill(head, "hi", smooth=True))
    e.append(line([(0.74, -0.03), (0.44, -0.08), (0.16, -0.10), (0.02, -0.20)], 0.03, 0.02, tone="lo", outline=False))  # the jaw's line
    for x in (0.60, 0.48, 0.36, 0.24):
        e.append(fill([(x, -0.085), (x - 0.05, -0.085), (x - 0.025, -0.15)], "lo", outline=False))
    e.append(fill(ellipse(0.14, 0.14, 0.10, 0.065, -8), "lo", outline=False))
    e.append(fill(ellipse(0.14, 0.14, 0.022, 0.058, -8), "acc", outline=False))
    e.append(fill(circle(0.64, 0.04, 0.02), "lo", outline=False))
    for x, y in ((-0.24, -0.34), (-0.06, -0.46), (-0.28, -0.58), (-0.08, -0.72), (-0.32, -0.82)):      # scales on the neck
        e.append(line(arc(x, y, 0.10, 200, 340, 8), 0.022, tone="lo", smooth=False, outline=False))
    return e


# ---- half-beast ----------------------------------------------------------------------------------------------------------
@motif("half-beast", "A", "The claw marks", "Three claw marks raked across a disc: whatever beast is in them, it has claws (Natural Claws).")
def beast_claws():
    e = [fill(circle(0, 0, 0.74), "mid")]
    for dx in (-0.32, 0.0, 0.32):
        e.append(fill([(0.24 + dx, 0.84), (0.20 + dx, 0.30), (0.02 + dx, -0.30), (-0.26 + dx, -0.86), (-0.14 + dx, -0.28), (0.04 + dx, 0.30)], "lo", smooth=True, outline=False))
        e.append(line([(0.25 + dx, 0.82), (0.22 + dx, 0.30), (0.05 + dx, -0.28), (-0.24 + dx, -0.84)], 0.03, 0.012, tone="hi", outline=False))
    return e


@motif("half-beast", "B", "The beast's mask", "A beast's face, ears pricked and eyes narrowed: wolf, fox, bear or lynx, as the wearer is.")
def beast_mask():
    e = []
    head = [(0.0, 0.44), (0.20, 0.50), (0.34, 0.60), (0.54, 0.84), (0.60, 0.40), (0.62, 0.14), (0.52, -0.06), (0.60, -0.16), (0.44, -0.20), (0.46, -0.34),
            (0.30, -0.34), (0.22, -0.58), (0.0, -0.76)]
    e.append(fill(head + mirror(head)[1:-1], "hi"))
    e += both([(0.36, 0.52), (0.50, 0.72), (0.53, 0.44)], tone="lo", outline=False)                                     # the ears' hollows
    e += both([(0.12, 0.10), (0.26, 0.20), (0.44, 0.22), (0.34, 0.08), (0.20, 0.04)], tone="lo", smooth=True, outline=False)   # the eyes
    e += both(ellipse(0.28, 0.13, 0.03, 0.055, -20), tone="acc", outline=False)
    muzzle = [(0.0, -0.10), (0.14, -0.20), (0.18, -0.46), (0.0, -0.70)]
    e.append(fill(muzzle + mirror(muzzle)[1:-1], "mid", smooth=True))
    e.append(fill([(-0.09, -0.18), (0.09, -0.18), (0.0, -0.30)], "lo", smooth=True, outline=False))                    # the nose
    e.append(line([(0.0, -0.30), (0.0, -0.46)], 0.022, tone="lo", smooth=False, outline=False))
    e += both_lines([(0.0, -0.46), (0.08, -0.52), (0.16, -0.46)], 0.022, tone="lo", outline=False)
    e += both([(0.05, -0.50), (0.12, -0.49), (0.085, -0.62)], tone="hi")                                                # the fangs
    return e


# ---- goliath -------------------------------------------------------------------------------------------------------------
@motif("goliath", "A", "The cairn", "A cairn of stacked stones, a rune cut in its base: how the high clans mark a peak (Giant's Blood).")
def goliath_cairn():
    e = []
    stones = [(0.0, -0.60, 0.56, 0.17, "hi"), (0.05, -0.30, 0.44, 0.15, "mid"), (-0.04, -0.04, 0.34, 0.13, "hi"), (0.04, 0.19, 0.25, 0.11, "mid"),
              (-0.02, 0.38, 0.17, 0.09, "hi"), (0.02, 0.54, 0.11, 0.075, "mid")]
    for n, (x, y, rx, ry, tone) in enumerate(stones):
        pts = []
        for k in range(10):
            a = 2 * math.pi * k / 10
            wob = 1.0 + 0.10 * math.sin(k * 2.3 + n * 1.7)
            pts.append((x + rx * wob * math.cos(a), y + ry * (1.0 + 0.12 * math.cos(k * 1.9 + n)) * math.sin(a)))
        e.append(fill(pts, tone, smooth=True))
    for seg in ([(-0.16, -0.50), (-0.16, -0.70)], [(-0.16, -0.50), (-0.04, -0.60), (-0.16, -0.66)], [(0.06, -0.50), (0.06, -0.70)],
                [(0.06, -0.58), (0.20, -0.50)], [(0.06, -0.60), (0.20, -0.70)]):
        e.append(line(seg, 0.028, tone="lo", smooth=False, outline=False))
    e += both_lines([(0.50, 0.30), (0.66, 0.40), (0.82, 0.34)], 0.03, 0.012, tone="acc", outline=False)                # the wind off the peak
    e += both_lines([(0.42, 0.52), (0.58, 0.62), (0.72, 0.58)], 0.03, 0.012, tone="acc", outline=False)
    return e


@motif("goliath", "B", "The stone fist", "A great fist raised, a rune band at the wrist: strength that endures.")
def goliath_fist():
    e = []
    e.append(fill([(-0.27, -0.50), (0.27, -0.50), (0.25, -0.80), (-0.25, -0.80)], "mid"))
    for x in (-0.14, 0.0, 0.14):
        e.append(line([(x - 0.04, -0.58), (x + 0.04, -0.65), (x - 0.04, -0.72)], 0.024, tone="lo", smooth=False, outline=False))
    fist = [(-0.36, 0.42), (-0.39, 0.10), (-0.33, -0.22), (-0.24, -0.50), (0.24, -0.50), (0.33, -0.22), (0.41, 0.10), (0.38, 0.42), (0.19, 0.54), (0.0, 0.56), (-0.19, 0.54)]
    e.append(fill(fist, "hi", smooth=True))
    for x in (-0.18, 0.0, 0.19):
        e.append(line([(x, 0.52), (x * 0.95, 0.30), (x * 0.9, 0.14)], 0.028, tone="lo", outline=False))
    e.append(line([(-0.34, 0.26), (0.0, 0.31), (0.36, 0.26)], 0.024, tone="lo", outline=False))
    e.append(fill([(-0.42, 0.04), (-0.12, 0.13), (0.18, 0.07), (0.22, -0.10), (-0.08, -0.19), (-0.38, -0.14)], "hi", smooth=True))     # the thumb, folded over
    e.append(line([(0.10, 0.02), (0.16, -0.04)], 0.022, tone="lo", smooth=False, outline=False))
    return e


# ---- merfolk -------------------------------------------------------------------------------------------------------------
@motif("merfolk", "A", "The shell", "A spiral shell, whorl on whorl: the sea's own house, carried ashore.")
def merfolk_shell():
    e = []
    e += [line([(-0.84, -0.66), (-0.60, -0.56), (-0.36, -0.66), (-0.12, -0.56), (0.12, -0.66), (0.36, -0.56), (0.60, -0.66), (0.84, -0.56)], 0.05, tone="acc", outline=False)]
    outer = []
    for k in range(49):
        a = math.radians(-60 + 360 * k / 48)
        r = 0.30 + 0.44 * k / 48
        outer.append((0.04 + r * math.cos(a), 0.06 + r * math.sin(a)))
    e.append(fill(outer, "hi"))
    inner = []
    for k in range(61):
        a = math.radians(-60 - 600 * k / 60)
        r = 0.30 * (1 - k / 60) ** 0.9 + 0.01
        inner.append((0.04 + r * math.cos(a), 0.06 + r * math.sin(a)))
    e.append(line(inner, 0.035, 0.012, tone="lo", smooth=False, outline=False))
    for k in range(2, 46, 4):                             # the growth lines across the outer whorl
        a = math.radians(-60 + 360 * k / 48)
        r0, r1 = 0.30 + 0.44 * k / 48, max(0.06, 0.30 * (k / 48) ** 1.1)
        e.append(line([(0.04 + (r1 + 0.03) * math.cos(a), 0.06 + (r1 + 0.03) * math.sin(a)), (0.04 + (r0 - 0.03) * math.cos(a - 0.12), 0.06 + (r0 - 0.03) * math.sin(a - 0.12))],
                      0.02, tone="lo", smooth=False, outline=False))
    return e


@motif("merfolk", "B", "The tail fluke", "A tail fluke breaking the waves: the last you see of one who dives (Fishman Physiology).")
def merfolk_tail():
    e = []
    e.append(line([(0.0, -0.52), (-0.03, -0.20), (0.0, 0.20)], 0.27, 0.11, tone="hi"))                                    # the stalk
    lobe = [(0.0, 0.08), (0.24, 0.16), (0.52, 0.36), (0.70, 0.70), (0.44, 0.54), (0.22, 0.42), (0.0, 0.34)]
    e.append(fill(lobe + mirror(lobe)[1:-1], "hi", smooth=True))
    for y in (-0.36, -0.22, -0.08):                       # scales
        e.append(line(arc(-0.02, y, 0.10, 200, 340, 8), 0.022, tone="lo", smooth=False, outline=False))
    e += both_lines([(0.04, 0.20), (0.24, 0.30), (0.46, 0.48)], 0.02, 0.008, tone="lo", outline=False)                    # the fluke's ribs
    e += [line([(-0.90, -0.46), (-0.66, -0.36), (-0.42, -0.46), (-0.18, -0.36), (0.06, -0.46), (0.30, -0.36), (0.54, -0.46), (0.78, -0.36), (0.94, -0.44)], 0.075, tone="acc")]
    e += [line([(-0.80, -0.70), (-0.56, -0.60), (-0.32, -0.70), (-0.08, -0.60), (0.16, -0.70), (0.40, -0.60), (0.64, -0.70), (0.84, -0.62)], 0.065, tone="mid")]
    for x, y, r in ((-0.76, 0.14, 0.04), (0.80, 0.06, 0.035), (-0.60, -0.14, 0.028)):
        e.append(fill(circle(x, y, r), "acc"))
    return e

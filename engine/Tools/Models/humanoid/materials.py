# The humanoid family's materials by palette slot (GM2-P7; look.py's SLOTS for the figure, GEAR_SLOTS for the gear, apart
# because one actor palette colours both): each material is named for its slot, so the engine colours it from the look's
# palette (Render3D/Palette.cs; the colours here show only where a palette has no such slot), and the fixed colours
# (look.FIXED: the pupils, the eyes' glints) are their own colour in every look. Unlit (glTF's unlit, flat as the
# lizardman test's pupils): the eyes. Without hatching: the brows, lashes and mouth, the mail's rows and the sword's
# fuller (drawn lines). Colours are linear.
import kit as k
from look import FIXED


def linear(text):
    def channel(c):
        c /= 255
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    value = int(text.lstrip("#"), 16)
    return tuple(channel((value >> shift) & 255) for shift in (16, 8, 0))


def make():
    """Every material of the family by its slot's name (made once per scene: a second call returns the same)."""
    return {
        # the figure
        "skin": k.lit_material("skin", (0.62, 0.42, 0.30)),
        "lips": k.lit_material("lips", (0.45, 0.25, 0.2)),
        "brows": k.lit_material("brows", (0.14, 0.09, 0.06), hatch=False),
        "eye.white": k.unlit_material("eye.white", (0.80, 0.78, 0.70)),
        "eyes": k.unlit_material("eyes", (0.25, 0.15, 0.08)),
        "pupil": k.unlit_material("pupil", linear(FIXED["pupil"])),
        "glint": k.unlit_material("glint", linear(FIXED["glint"])),
        "hair": k.lit_material("hair", (0.2, 0.12, 0.07)),
        "cloth": k.lit_material("cloth", (0.35, 0.1, 0.08)),
        "cloth.trim": k.lit_material("cloth.trim", (0.6, 0.45, 0.2)),
        "trousers": k.lit_material("trousers", (0.15, 0.13, 0.12)),
        "leather": k.lit_material("leather", (0.2, 0.13, 0.08)),
        "buckle": k.lit_material("buckle", (0.6, 0.45, 0.2)),
        # the gear
        "metal.base": k.lit_material("metal.base", (0.5, 0.52, 0.55)),
        "metal.trim": k.lit_material("metal.trim", (0.6, 0.45, 0.2)),
        "metal.shade": k.lit_material("metal.shade", (0.2, 0.21, 0.23), hatch=False),
        "wood": k.lit_material("wood", (0.35, 0.1, 0.08)),
        "grip": k.lit_material("grip", (0.2, 0.13, 0.08)),
    }

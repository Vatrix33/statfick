#!/usr/bin/env python3
"""Writes the icons' outlines and their box table (Assets/UI/icons: <name>.svg and icons.json) from figures.py in the
Engraved style (iconlib.py). Each icon's box (advance, ascent, descent, line gap) is the glyph Chromium drew for the
symbol on the reference PC, read here from the repo's copies of those fonts (Tests/Statventure.Tests/Data/SystemFonts);
each figure sits where that glyph's ink sat.
   build.py <folder>            write the outlines and icons.json into <folder>
   build.py --check <folder>    build into a temporary folder and compare with <folder> byte for byte
   build.py --preview <png>     a quick picture of every icon beside the old glyph (Pillow)
   build.py --fit               each icon's ink box against the old glyph's, in 1/1000 em"""
import filecmp, json, os, sys, tempfile
from fontTools.ttLib import TTFont
from fontTools.pens.boundsPen import BoundsPen
from iconlib import render, move, svg_path
from figures import FIGURES

HERE = os.path.dirname(os.path.abspath(__file__))
FONTS = os.path.normpath(os.path.join(HERE, "..", "..", "..", "Tests", "Statventure.Tests", "Data", "SystemFonts"))

TESTFONTS = "TestFonts.SymbolFonts: the font Chromium fell back to on the reference PC (2026-10); Chromium's advance in the old game's page today is this font's"
FALLBACK = "Chromium's fallback on the reference PC: in the old game's page (Tools/IconSheet/old-game-uses.js) its advance and ink box are this font's"
# The 28 symbols: name, the font file Chromium drew the glyph with, how that was found.
ICONS = [
    ("↶", "anticlockwise-top-semicircle-arrow", "NotoSansMath-Regular.ttf", TESTFONTS),
    ("↺", "anticlockwise-open-circle-arrow", "AdwaitaSans-Regular.ttf", TESTFONTS),
    ("↻", "clockwise-open-circle-arrow", "AdwaitaSans-Regular.ttf", TESTFONTS),
    ("⇧", "upwards-white-arrow", "AdwaitaSans-Regular.ttf", FALLBACK + "; fontconfig's recorded answer agrees (Data/SystemFonts/fontconfig.json, sans-serif:charset=21e7)"),
    ("⌄", "down-arrowhead", "AdwaitaSans-Regular.ttf", FALLBACK),
    ("⌖", "position-indicator", "AdwaitaMono-Regular.ttf", TESTFONTS),
    ("▣", "white-square-containing-black-small-square", "NotoSansMath-Regular.ttf", TESTFONTS),
    ("▲", "black-up-pointing-triangle", "LiberationSans-Bold.ttf", "not a fallback: --font-ui names Arial, which fontconfig gives as Liberation Sans (bold at the battle status's weight 700), and it has the glyph"),
    ("▼", "black-down-pointing-triangle", "LiberationSans-Bold.ttf", "not a fallback: --font-ui names Arial, which fontconfig gives as Liberation Sans (bold at the battle status's weight 700), and it has the glyph"),
    ("◆", "black-diamond", "AdwaitaSans-Regular.ttf", FALLBACK),
    ("◇", "white-diamond", "AdwaitaSans-Regular.ttf", TESTFONTS),
    ("◈", "white-diamond-containing-black-small-diamond", "NotoSansMath-Regular.ttf", TESTFONTS),
    ("◌", "dotted-circle", "LiberationSerif-Regular.ttf", "not a fallback: its uses are set in \"Georgia, serif\", and serif is Chromium's Times New Roman, which fontconfig gives as Liberation Serif, which has the glyph"),
    ("◎", "bullseye", "NotoSansMath-Regular.ttf", FALLBACK),
    ("◫", "white-square-with-vertical-bisecting-line", "NotoSansMath-Regular.ttf", TESTFONTS),
    ("★", "black-star", "AdwaitaSans-Regular.ttf", FALLBACK),
    ("☆", "white-star", "AdwaitaSans-Regular.ttf", TESTFONTS),
    ("☰", "trigram-for-heaven", "AdwaitaMono-Regular.ttf", TESTFONTS),
    ("♛", "black-chess-queen", "NotoSansSymbols2-Regular.ttf", FALLBACK),
    ("♜", "black-chess-rook", "NotoSansSymbols2-Regular.ttf", TESTFONTS),
    ("♟", "black-chess-pawn", "NotoEmoji-Regular.ttf", TESTFONTS),
    ("♡", "white-heart-suit", "AdwaitaSans-Regular.ttf", TESTFONTS),
    ("⚔", "crossed-swords", "NotoEmoji-Regular.ttf", TESTFONTS),
    ("⚙", "gear", "AdwaitaMono-Regular.ttf", TESTFONTS),
    ("✦", "black-four-pointed-star", "AdwaitaMono-Regular.ttf", TESTFONTS),
    ("✧", "white-four-pointed-star", "AdwaitaMono-Regular.ttf", TESTFONTS),
    ("✸", "heavy-eight-pointed-rectilinear-black-star", "AdwaitaMono-Regular.ttf", FALLBACK),
    ("✺", "sixteen-pointed-asterisk", "AdwaitaMono-Regular.ttf", TESTFONTS),
]

_fonts = {}

def font_box(symbol, file):
    """The glyph's advance and ink bounds and the font's ascent, descent and line gap, in em, as Chromium reads
    them: Skia (Fontations) takes OS/2's typo metrics when USE_TYPO_METRICS is set, else hhea's."""
    if file not in _fonts:
        _fonts[file] = TTFont(os.path.join(FONTS, file))
    f = _fonts[file]
    upem = f["head"].unitsPerEm
    os2, hhea = f["OS/2"], f["hhea"]
    if os2.fsSelection & (1 << 7):
        ascent, descent, gap = os2.sTypoAscender, -os2.sTypoDescender, os2.sTypoLineGap
    else:
        ascent, descent, gap = hhea.ascent, -hhea.descent, hhea.lineGap
    glyph = f.getBestCmap()[ord(symbol)]
    pen = BoundsPen(f.getGlyphSet())
    f.getGlyphSet()[glyph].draw(pen)
    x0, y0, x1, y1 = pen.bounds
    name = f["name"]
    family = name.getDebugName(16) or name.getDebugName(1)
    style = name.getDebugName(17) or name.getDebugName(2)
    return {
        "advance": f["hmtx"][glyph][0] / upem, "ascent": ascent / upem, "descent": descent / upem, "lineGap": gap / upem,
        "ink": (x0 / upem * 1000, -y1 / upem * 1000, x1 / upem * 1000, -y0 / upem * 1000),  # x0, top, x1, bottom; y down
        "font": family + ("" if style == "Regular" else " " + style), "version": name.getDebugName(5),
    }

def outline(symbol, file):
    """The icon's ink, its ink box centred on the old glyph's (so it sits where that glyph sat), and the box."""
    box = font_box(symbol, file)
    x0, top, x1, bottom = box["ink"]
    shape = render(FIGURES[symbol]())
    nx0, ntop, nx1, nbottom = shape.bounds
    return move(shape, (x0 + x1) / 2 - (nx0 + nx1) / 2, (top + bottom) / 2 - (ntop + nbottom) / 2), box

def number(v):
    return float(f"{v:.10g}")

def write(folder):
    os.makedirs(folder, exist_ok=True)
    rows = []
    for symbol, name, file, how in ICONS:
        shape, box = outline(symbol, file)
        adv, asc, desc = box["advance"] * 1000, box["ascent"] * 1000, box["descent"] * 1000
        f = lambda v: f"{v:.3f}".rstrip("0").rstrip(".")
        text = (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 {f(-asc)} {f(adv)} {f(asc + desc)}">\n'
                f'<!-- {symbol} U+{ord(symbol):04X} {name.replace("-", " ").upper()}, in units of 1/1000 em: x from the pen position, '
                f'y down from the baseline (y = 0). The view box is the icon\'s box: advance {f(adv)}, ascent {f(asc)}, descent {f(desc)}. '
                f'Written by Tools/IconSheet/gen/build.py. -->\n'
                f'<path fill-rule="nonzero" d="{svg_path(shape)}"/>\n</svg>\n')
        with open(os.path.join(folder, name + ".svg"), "w", encoding="utf-8", newline="\n") as out:
            out.write(text)
        rows.append({"codePoint": f"U+{ord(symbol):04X}", "character": symbol, "name": name, "file": name + ".svg",
                     "advance": number(box["advance"]), "ascent": number(box["ascent"]), "descent": number(box["descent"]), "lineGap": number(box["lineGap"]),
                     "measured": {"font": box["font"], "file": file, "version": box["version"], "how": how}})
    about = ("The symbols the old game showed from the system's fonts, drawn by the engine as icons (GM1-7, Q12; "
             "Docs/Game/Foundation.md, \"The symbols as vector icons (GM1-7)\"). Each outline is one or more filled paths with "
             "their fill rule, in units of 1/1000 em, x from the pen position, y down from the baseline; it is filled in the "
             "text's colour. Each icon's box is the glyph Chromium drew for the symbol on the reference PC, in em: its advance, "
             "and its font's ascent, descent and line gap as Chromium reads them (OS/2's typo metrics when USE_TYPO_METRICS is "
             "set, else hhea's); \"measured\" names that font, its file in Tests/Statventure.Tests/Data/SystemFonts and how it was "
             "found. Written by Tools/IconSheet/gen/build.py with the outlines; Tools/Scripts/make-icon-sheet.sh shows them.")
    with open(os.path.join(folder, "icons.json"), "w", encoding="utf-8", newline="\n") as out:
        out.write('{\n "about": ' + json.dumps(about, ensure_ascii=False) + ',\n "icons": [\n  ' +
                  ",\n  ".join(json.dumps(r, ensure_ascii=False) for r in rows) + "\n ]\n}\n")

def check(folder):
    with tempfile.TemporaryDirectory() as fresh:
        write(fresh)
        names = sorted(os.listdir(fresh))
        committed = sorted(n for n in os.listdir(folder) if n.endswith((".svg", ".json")))
        problems = [f"{n}: not written by build.py" for n in committed if n not in names]
        problems += [f"{n}: missing" for n in names if n not in committed]
        problems += [f"{n}: differs from what build.py writes" for n in names if n in committed and not filecmp.cmp(os.path.join(fresh, n), os.path.join(folder, n), shallow=False)]
    for p in problems:
        print(p)
    print(f"{len(names)} files checked, {len(problems)} differences")
    return 1 if problems else 0

def fit():
    for symbol, name, file, _ in ICONS:
        shape, box = outline(symbol, file)
        nx0, ntop, nx1, nbottom = shape.bounds
        ox0, otop, ox1, obottom = box["ink"]
        print(f"{symbol} new {nx1 - nx0:5.0f} x {nbottom - ntop:5.0f}  old {ox1 - ox0:5.0f} x {obottom - otop:5.0f}   "
              f"dx0 {nx0 - ox0:+5.0f} dx1 {nx1 - ox1:+5.0f} dtop {ntop - otop:+5.0f} dbottom {nbottom - obottom:+5.0f}   adv {box['advance'] * 1000:.0f}")

def preview(target, px=96):
    from PIL import Image, ImageDraw, ImageFont
    k, cols = 4, 4
    cell_w, cell_h = int(px * 2.9), int(px * 1.8)
    rows_n = (len(ICONS) + cols - 1) // cols
    img = Image.new("L", (cell_w * cols * k, cell_h * rows_n * k), 236)
    d = ImageDraw.Draw(img)
    s = px * k / 1000
    for i, (symbol, name, file, _) in enumerate(ICONS):
        shape, box = outline(symbol, file)
        x0, y0 = (i % cols) * cell_w * k, (i // cols) * cell_h * k
        oy = y0 + cell_h * k * 0.66
        font = ImageFont.truetype(os.path.join(FONTS, file), px * k)
        ox = x0 + 20 * k
        d.text((ox, oy), symbol, font=font, fill=110, anchor="ls")
        ox2 = ox + box["advance"] * px * k + 24 * k
        polys = [shape] if shape.geom_type == "Polygon" else list(shape.geoms)
        for poly in sorted(polys, key=lambda p: -p.area):
            d.polygon([(ox2 + x * s, oy + y * s) for x, y in poly.exterior.coords], fill=20)
            for hole in poly.interiors:
                d.polygon([(ox2 + x * s, oy + y * s) for x, y in hole.coords], fill=236)
        for o in (ox, ox2):
            d.line([(o, oy), (o + box["advance"] * px * k, oy)], fill=170, width=k)
            d.rectangle([o, oy - box["ascent"] * px * k, o + box["advance"] * px * k, oy + box["descent"] * px * k], outline=200, width=k)
    img.resize((img.width // k, img.height // k), Image.LANCZOS).save(target)

if __name__ == "__main__":
    if sys.argv[1:2] == ["--check"]:
        sys.exit(check(sys.argv[2]))
    elif sys.argv[1:2] == ["--preview"]:
        preview(sys.argv[2])
    elif sys.argv[1:2] == ["--fit"]:
        fit()
    else:
        write(sys.argv[1])

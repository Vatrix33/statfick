#!/usr/bin/env python3
"""Writes the icon sheet's pages as Tools/PaintReference fixtures (GM1 step 3; Tools/Scripts/make-icon-sheet.sh runs it).

Each icon is shown as inline SVG beside the glyph Chromium draws for the symbol in the old game's font, in both themes:
- <name>-uses-<theme>[-<page>]: every use in uses.json (Tools/IconSheet/uses.json) at its size, weight, colour, opacity
  and ground, with the words the game sets beside it;
- <name>-small-<theme>: the uses at 13 px and under, with a copy enlarged 3x under them (added by the script);
- <name>-large-<theme>[-<page>]: each icon at 96 px over its box (the advance, ascent and descent; the baseline solid).
An icon folder holds icons.json (the box table) and the outlines it names; several folders (label=folder) show as
columns side by side, for comparing styles.
   sheet.py --uses <uses.json> --icons <label>=<folder> [--icons ...] [--symbols <characters>] --name <name> --out <fixtures.json>"""
import argparse, html, json, math, os, re

THEMES = {
    # The sheet's own frame: the old game's paper and ink in each theme (styles.css: the Gothic ink theme's parchment).
    "light": {"page": "#efe6d0", "ink": "#22160d", "muted": "#6b5a45", "rule": "#cdbd98", "large_ground": "#f3e8cc", "large_ink": "#22160d", "guide": "#b39c74"},
    "dark": {"page": "#17110d", "ink": "#ece0c4", "muted": "#a8977a", "rule": "#3a2c20", "large_ground": "#261c15", "large_ink": "#ece0c4", "guide": "#5e4c39"},
}
LARGE = 96
USE_ROWS_PER_PAGE = 46
LARGE_ROWS_PER_PAGE = 8
SMALL, ZOOM = 13, 3  # the "small" page: every use at 13 px or under, with a copy enlarged 3x

def load_icons(folder):
    table = json.load(open(os.path.join(folder, "icons.json"), encoding="utf-8"))
    icons = {}
    for icon in table["icons"]:
        text = open(os.path.join(folder, icon["file"]), encoding="utf-8").read()
        paths = re.findall(r"<path\b([^>]*)/>", text)
        icon["paths"] = [(re.search(r'fill-rule="(\w+)"', p).group(1), re.search(r'\bd="([^"]*)"', p).group(1)) for p in paths]
        icons[icon["character"]] = icon
    return icons

def svg(icon, cls="icon", style=""):
    adv, asc, desc = icon["advance"] * 1000, icon["ascent"] * 1000, icon["descent"] * 1000
    f = lambda v: f"{v:.3f}".rstrip("0").rstrip(".")
    body = "".join(f'<path fill="currentColor" fill-rule="{rule}" d="{d}"/>' for rule, d in icon["paths"])
    return (f'<svg class="{cls}" viewBox="0 {f(-asc)} {f(adv)} {f(asc + desc)}" aria-hidden="true" '
            f'style="width:{f(icon["advance"])}em;height:{f(icon["ascent"] + icon["descent"])}em;vertical-align:{f(-icon["descent"])}em;{style}">{body}</svg>')

def guide(icon, colour):
    adv, asc, desc = icon["advance"] * 1000, icon["ascent"] * 1000, icon["descent"] * 1000
    f = lambda v: f"{v:.3f}".rstrip("0").rstrip(".")
    return (f'<svg class="guide" viewBox="0 {f(-asc)} {f(adv)} {f(asc + desc)}" aria-hidden="true" '
            f'style="width:{f(icon["advance"])}em;height:{f(icon["ascent"] + icon["descent"])}em;vertical-align:{f(-icon["descent"])}em;margin-right:{f(-icon["advance"])}em">'
            f'<rect x="0" y="{f(-asc)}" width="{f(adv)}" height="{f(asc + desc)}" fill="none" stroke="{colour}" stroke-width="1" stroke-dasharray="3 3" vector-effect="non-scaling-stroke"/>'
            f'<line x1="0" y1="0" x2="{f(adv)}" y2="0" stroke="{colour}" stroke-width="1" vector-effect="non-scaling-stroke"/></svg>')

def colgroup(widths):
    return "<colgroup>" + "".join(f'<col style="width:{w}px">' for w in widths) + "</colgroup>"

def esc(text):
    return html.escape(text, quote=True)

def font_size(font):
    return float(re.search(r"(\d+(?:\.\d+)?)px", font).group(1))

CSS = """
html, body { margin: 0; overflow: hidden; }
body { font: 400 12px/1.3 Lato, sans-serif; }
.sheet { padding: 14px 16px 16px; }
h1 { margin: 0 0 4px; font: 700 15px/1.3 Lato, sans-serif; letter-spacing: .01em; }
p.note { margin: 0 0 10px; font-size: 11.5px; }
table { border-collapse: collapse; table-layout: fixed; }
th, td { box-sizing: border-box; }
th { padding: 0 8px 5px; text-align: left; font-weight: 700; font-size: 11px; text-transform: uppercase; letter-spacing: .04em; vertical-align: bottom; }
td { padding: 0 8px; vertical-align: middle; overflow: hidden; white-space: nowrap; }
td.label { font-size: 11.5px; line-height: 1.25; }
td.label small { font-size: 10.5px; }
td.symbol { font-size: 12px; }
td.symbol b { font: 400 17px "STIX Two Text", serif; }
td.cell { padding: 0 10px; }
td.cell > span { display: inline-block; line-height: normal; }
tr.first td { border-top: 1px solid var(--rule); }
.large td.symbol { white-space: normal; }
.large td.cell { padding: 10px 18px; }
.large td.cell > span { font-size: 96px; line-height: normal; }
svg.icon { overflow: visible; }
"""

def page_uses(name, theme, uses, columns, icon_sets, symbols, page_label, extra="", small=False):
    t = THEMES[theme]
    rows, heights = [], []
    previous = None
    for use in uses:
        size = font_size(use["font"])
        height = max(30, math.ceil(size * 1.75) + 8)
        look = use[theme]
        style = f'font:{esc(use["font"])};color:{look["color"]};opacity:{look["opacity"]}'
        transform = f';transform:{look["transform"]}' if look.get("transform") else ""
        text = esc(use.get("text", ""))
        def content(glyph):
            return (text + glyph) if use.get("textBefore") else (glyph + text)
        cells = [f'<td class="cell" style="background:{look["ground"]}"><span style="{style}{transform}">{content(esc(use["symbol"]))}</span></td>']
        for label in columns:
            icon = icon_sets[label].get(use["symbol"])
            cells.append(f'<td class="cell" style="background:{look["ground"]}"><span style="{style}{transform}">{content(svg(icon)) if icon else "—"}</span></td>')
        first = use["symbol"] != previous
        previous = use["symbol"]
        symbol_cell = f'<b>{esc(use["symbol"])}</b> {use["codePoint"]}' if first else ""
        size_text = re.sub(r"^normal ", "", use["font"].split(" Lato")[0].split(" Georgia")[0].split(' "STIX')[0])
        rows.append(f'<tr class="{"first" if first else ""}" style="height:{height}px"><td class="symbol">{symbol_cell}</td>'
                    f'<td class="label">{esc(use["what"])}<br><small style="color:{t["muted"]}">{esc(use["source"])} · {esc(size_text)}</small></td>' + "".join(cells) + "</tr>")
        heights.append(height)
    widths = [96, 300, 200] + [200] * len(columns)
    head = colgroup(widths) + "<tr>" + "".join(f"<th>{h}</th>" for h in ["Symbol", "Where the game shows it", "Chromium's glyph"] + columns) + "</tr>"
    theme_name = "light" if theme == "light" else "dark"
    if small:
        title = f"The smallest uses ({SMALL} px and under) · {theme_name} theme"
        note = (f"Every use at {SMALL} px or under, at 1× as on the other pages; below, the same picture enlarged {ZOOM}× "
                f"(each pixel a {ZOOM} × {ZOOM} block), to judge what the eye gets at those sizes.")
    else:
        title = f"The icons at the game's sizes and colours · {theme_name} theme{page_label}"
        note = "Each use as the old game styles it (font, colour, opacity, ground), at 1×: Chromium's own glyph from the old game's fonts, then each icon as inline SVG in the same place."
    note += ("<br>" + esc(extra) if extra else "")
    body = f'<div class="sheet"><h1>{title}</h1><p class="note" style="color:{t["muted"]}">{note}</p><table style="width:{sum(widths)}px">{head}{"".join(rows)}</table></div>'
    width = sum(widths) + 16 * 2
    # Taller than it needs: the picture is cut to the content afterwards.
    height = int((120 + sum(heights)) * 1.25)
    css = CSS + f":root {{ --rule: {t['rule']}; }} body {{ background: {t['page']}; color: {t['ink']}; }}"
    fixture = {"name": name, "width": width, "height": height, "css": css, "html": body}
    if small:
        fixture["zoom"] = ZOOM  # make-icon-sheet.sh adds the enlarged copy under the picture
    return fixture

def page_large(name, theme, symbols, columns, icon_sets, page_label, extra=""):
    t = THEMES[theme]
    rows, heights = [], []
    for s in symbols:
        any_icon = next(icon_sets[label][s] for label in columns if s in icon_sets[label])
        height = math.ceil((any_icon["ascent"] + any_icon["descent"]) * LARGE) + 22
        style = f'color:{t["large_ink"]}'
        cells = [f'<td class="cell" style="background:{t["large_ground"]}"><span style="{style}">{guide(any_icon, t["guide"])}<span style="font-family:Lato, &quot;Segoe UI&quot;, Arial, sans-serif">{esc(s)}</span></span></td>']
        for label in columns:
            icon = icon_sets[label].get(s)
            cells.append(f'<td class="cell" style="background:{t["large_ground"]}"><span style="{style}">{guide(icon, t["guide"]) + svg(icon) if icon else "—"}</span></td>')
        rows.append(f'<tr class="first" style="height:{height}px"><td class="symbol"><b>{esc(s)}</b> U+{ord(s):04X}<br><small style="color:{t["muted"]}">{esc(any_icon["name"].replace("-", " "))}<br>{esc(any_icon["measured"]["font"])}</small></td>' + "".join(cells) + "</tr>")
        heights.append(height)
    col_width = max(170, max(math.ceil(icon_sets[label][s]["advance"] * LARGE) + 48 for label in columns for s in symbols if s in icon_sets[label]))
    widths = [170] + [col_width] * (len(columns) + 1)
    head = colgroup(widths) + "<tr>" + "".join(f"<th>{h}</th>" for h in ["Symbol", "Chromium's glyph"] + columns) + "</tr>"
    title = f"The icons large (96 px) over their boxes · {'light' if theme == 'light' else 'dark'} theme{page_label}"
    note = "Dashed: the box (advance, ascent, descent) every icon keeps from the glyph Chromium drew; solid: the baseline. Chromium's glyph is the old game's fallback font's." + ("<br>" + esc(extra) if extra else "")
    body = f'<div class="sheet large"><h1>{title}</h1><p class="note" style="color:{t["muted"]}">{note}</p><table style="width:{sum(widths)}px">{head}{"".join(rows)}</table></div>'
    width = sum(widths) + 16 * 2
    height = int((120 + sum(heights)) * 1.4)
    css = CSS + f":root {{ --rule: {t['rule']}; }} body {{ background: {t['page']}; color: {t['ink']}; }}"
    return {"name": name, "width": width, "height": height, "css": css, "html": body}

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--uses", required=True)
    parser.add_argument("--icons", action="append", required=True, help="label=folder")
    parser.add_argument("--symbols", default="")
    parser.add_argument("--name", default="icons")
    parser.add_argument("--note", default="", help="a line added under each page's title")
    parser.add_argument("--out", required=True)
    args = parser.parse_args()
    columns, icon_sets = [], {}
    for item in args.icons:
        label, folder = item.split("=", 1)
        columns.append(label)
        icon_sets[label] = load_icons(folder)
    order = list(next(iter(icon_sets.values())).keys())
    symbols = [s for s in (args.symbols or "".join(order))]
    uses = [u for s in symbols for u in json.load(open(args.uses, encoding="utf-8"))["uses"] if u["symbol"] == s]
    # Every use's measured advance must be the icon's (the box comes from the glyph Chromium drew there).
    for u in uses:
        for label in columns:
            icon = icon_sets[label].get(u["symbol"])
            if icon and abs(icon["advance"] - u["chromium"]["advance"]) > 0.002:
                raise SystemExit(f"{u['symbol']} {u['use']}: Chromium's advance {u['chromium']['advance']} isn't the icon's {icon['advance']} ({label})")
    fixtures = []
    for theme in ("light", "dark"):
        pages = [uses[i:i + USE_ROWS_PER_PAGE] for i in range(0, len(uses), USE_ROWS_PER_PAGE)]
        # Keep a symbol's uses on one page.
        balanced, current = [], []
        for s in symbols:
            group = [u for u in uses if u["symbol"] == s]
            if current and len(current) + len(group) > USE_ROWS_PER_PAGE:
                balanced.append(current)
                current = []
            current += group
        if current:
            balanced.append(current)
        for i, chunk in enumerate(balanced):
            suffix = f"-{i + 1}" if len(balanced) > 1 else ""
            label = f" · page {i + 1} of {len(balanced)}" if len(balanced) > 1 else ""
            fixtures.append(page_uses(f"{args.name}-uses-{theme}{suffix}", theme, chunk, columns, icon_sets, symbols, label, args.note))
        smallest = [u for u in uses if font_size(u["font"]) <= SMALL]
        if smallest:
            fixtures.append(page_uses(f"{args.name}-small-{theme}", theme, smallest, columns, icon_sets, symbols, "", args.note, small=True))
        large_pages = [symbols[i:i + LARGE_ROWS_PER_PAGE] for i in range(0, len(symbols), LARGE_ROWS_PER_PAGE)]
        for i, chunk in enumerate(large_pages):
            suffix = f"-{i + 1}" if len(large_pages) > 1 else ""
            label = f" · page {i + 1} of {len(large_pages)}" if len(large_pages) > 1 else ""
            fixtures.append(page_large(f"{args.name}-large-{theme}{suffix}", theme, chunk, columns, icon_sets, label, args.note))
    with open(args.out, "w", encoding="utf-8") as f:
        json.dump(fixtures, f, ensure_ascii=False)
    for fixture in fixtures:
        print(fixture["name"], fixture["width"], "x", fixture["height"])

if __name__ == "__main__":
    main()

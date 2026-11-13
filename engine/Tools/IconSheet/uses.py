#!/usr/bin/env python3
"""Turns what old-game-uses.js measured in the old game into Tools/IconSheet/uses.json, the icon sheet's fixture: every
place the old game shows one of the 28 symbols, with the font, colour, ground and opacity it has there in each theme and
the advance and ink box Chromium gave it. Run by Tools/Scripts/make-icon-sheet.sh --uses.
   uses.py <probe result.json> <uses.json>"""
import json, sys

# What each use is, and the words the game sets beside the symbol in the same element (shown after it on the sheet).
LABELS = {
    "settings-button": ("the Settings button", "Settings"),
    "status-trap": ("a battle status on the stage: a trap", ""),
    "brand": ("the top bar's brand mark (always lit, Q11)", ""),
    "hero-sigil": ("the sheet's race sigil before a race is chosen (turned -11°)", ""),
    "tool-skills": ("the field tools' Skills button", ""),
    "skill-detail-empty": ("the Skills window's empty detail", ""),
    "skill-detail": ("a skill's detail", ""),
    "skill-detail-active": ("an active skill's detail", ""),
    "skill-list-empty": ("the Skills window's empty list", ""),
    "race-list-empty": ("the race list when no races loaded", ""),
    "race-option": ("a race without its seal image in the race list", ""),
    "companion-nav-training": ("a companion sheet's Training tab", "Training"),
    "enemy-sparkling": ("a sparkling enemy's name", "Bandit"),
    "tool-combat": ("the field tools' Combat button (Dev Mode)", ""),
    "action-attack": ("the Attack action in combat", "Attack"),
    "action-attack-disabled": ("the Attack action, disabled", "Attack"),
    "level-icon": ("the sheet's Level card", ""),
    "tool-classes": ("the field tools' Classes button", ""),
    "class-detail-empty": ("the Classes window's empty detail", ""),
    "class-detail": ("a class's detail", ""),
    "class-list-empty": ("the Classes window's empty list", ""),
    "class-info-list-empty": ("the class information list, empty", ""),
    "hp-icon": ("the sheet's Hit points card", ""),
    "companion-hp": ("a companion's Hit points", ""),
    "mana-icon": ("the sheet's Mana card", ""),
    "companion-mana": ("a companion's Mana", ""),
    "action-guard": ("the Guard action in combat", "Guard"),
    "action-guard-disabled": ("the Guard action, disabled", "Guard"),
    "action-run": ("the Run action in combat", "Run"),
    "action-run-disabled": ("the Run action, disabled", "Run"),
    "respec": ("the Respec button (disabled until it can be used)", "Respec"),
    "restore-button": ("Restore both to full (Dev Mode)", "Restore both to full"),
    "refresh-race": ("Refresh file in the race window", "Refresh file"),
    "refresh-spell": ("Refresh file in the spell window", "Refresh file"),
    "refresh-item": ("Refresh files in the item window", "Refresh files"),
    "refresh-class": ("Refresh files in the class window", "Refresh files"),
    "map-speed-hotkey": ("the map's speed buttons' shortcut hints", "1"),
    "companion-details": ("a companion sheet section's fold mark", ""),
    "tool-map": ("the field tools' Map button", ""),
    "defense-armor": ("the sheet's Armor card", ""),
    "companion-armor": ("a companion's Armor", ""),
    "companion-nav-equipment": ("a companion sheet's Equipment tab", "Equipment"),
    "status-buff": ("a battle status on the stage: a buff", ""),
    "status-debuff": ("a battle status on the stage: a debuff", ""),
    "status-guard": ("a battle status on the stage: Guard", ""),
    "key-item-detail": ("a key item's detail (legendary)", ""),
    "companion-nav-attributes": ("a companion sheet's Attributes tab", "Attributes"),
    "map-artifact": ("an artifact marker on the map (scaled with the map)", ""),
    "tool-passives": ("the field tools' Passives button", ""),
    "passive-detail-empty": ("the Passives window's empty detail", ""),
    "passive-detail": ("a passive's detail", ""),
    "passive-list-empty": ("the Passives window's empty list", ""),
    "defense-evasion": ("the sheet's Evasion card", ""),
    "companion-evasion": ("a companion's Evasion", ""),
    "status-mark": ("a battle status on the stage: a mark", ""),
    "tool-inventory": ("the field tools' Inventory button", ""),
    "item-detail-empty": ("the Inventory's empty detail", ""),
    "item-detail-common": ("an item's detail: common", ""),
    "item-detail-uncommon": ("an item's detail: uncommon", ""),
    "item-detail-rare": ("an item's detail: rare", ""),
    "item-detail-epic": ("an item's detail: epic", ""),
    "item-detail-legendary": ("an item's detail: legendary", ""),
    "item-detail-masterwork": ("an item's detail: masterwork", ""),
    "companion-item-detail": ("an item's detail on a companion sheet", ""),
    "owned-items-empty": ("the Inventory's empty list", ""),
    "favorite-on": ("an item's favourite button, on", ""),
    "shop-favorite": ("a favourite in a shop's list", "Favorite"),
    "map-great-hunt": ("a great hunt marker on the map (scaled with the map)", ""),
    "favorite-off": ("an item's favourite button, off", ""),
    "battle-log-pin": ("the battle log's pin button on the stage", ""),
    "enemy-unique": ("a unique enemy's name", "Bandit"),
    "tool-companions": ("the field tools' Companions button", ""),
    "companion-empty": ("the companion window with none chosen", ""),
    "companion-portrait": ("a companion's portrait (behind the race's seal when it has one)", ""),
    "tool-spellbook": ("the field tools' Spell book button", ""),
    "spell-detail-empty": ("the Spell book's empty detail", ""),
    "known-spells-empty": ("the Spell book's empty list", ""),
    "tower-glyph-fire": ("a spell in the Magic Tower's list (fire)", ""),
    "tower-glyph-frost": ("a spell in the Magic Tower's list (frost)", ""),
    "status-stun": ("a battle status on the stage: stunned", ""),
    "action-meditate": ("the Meditate action in combat", "Meditate"),
    "action-meditate-disabled": ("the Meditate action, disabled", "Meditate"),
    "field-tools-toggle": ("the Field tools button (windows narrower than 840 px)", "Field tools"),
}
# Text after the symbol, not before it.
TEXT_BEFORE = {"enemy-unique"}
# Grounds the probe can't read: the map markers' text sits on the marker's own circle (map.js 545).
GROUNDS = {"map-artifact": "#c59b3c", "map-great-hunt": "#7d2135"}

def main():
    probe = json.load(open(sys.argv[1], encoding="utf-8"))
    uses = []
    for u in probe["uses"]:
        key = u["key"]
        what, text = LABELS[key]
        light, dark = u["light"], u["dark"]
        font = probe["fonts"][light["fontKey"]]
        def theme(t):
            entry = {"color": t["color"], "ground": GROUNDS.get(key, t["ground"]), "opacity": t["opacity"]}
            if t.get("transform"):
                entry["transform"] = t["transform"]
            return entry
        for t in (light, dark):
            assert (t["fontSize"], t["fontWeight"], t["fontStyle"], t["fontFamily"]) == (light["fontSize"], light["fontWeight"], light["fontStyle"], light["fontFamily"]), key
        use = {
            "symbol": u["sym"], "codePoint": f"U+{ord(u['sym']):04X}", "use": key, "source": u["source"], "what": what,
            "font": f"{light['fontStyle']} {light['fontWeight']} {light['fontSize']} {light['fontFamily']}",
            "light": theme(light), "dark": theme(dark),
            "chromium": {"advance": round(font["advance"], 6), "ink": font["ink"]},
        }
        if text:
            use["text"] = (" " + text) if key not in TEXT_BEFORE else (text + " ")
            if key in TEXT_BEFORE:
                use["textBefore"] = True
        uses.append(use)
    out = {
        "about": "Every place the old game shows a symbol the port draws as an icon (GM1-7), as Chromium 152 computes it there in "
                 "the light theme and under html.dark-mode: the font (CSS shorthand), the colour, the ground behind it (colours "
                 "composited up the tree, a gradient taken at its first stop) and the opacity; and the advance and ink box (left, "
                 "right, ascent, descent from the pen position on the baseline, in em) Chromium's canvas measured for the symbol "
                 "in that font. Made by Tools/Scripts/make-icon-sheet.sh "
                 "--uses from the old game (Tools/IconSheet/old-game-uses.js); the icon sheet shows each use.",
        "userAgent": probe["userAgent"],
        "uses": uses,
    }
    # One use a line, so a change shows as the lines of the uses it touches.
    head = json.dumps({k: v for k, v in out.items() if k != "uses"}, ensure_ascii=False, indent=1)[:-2]
    lines = ",\n  ".join(json.dumps(u, ensure_ascii=False) for u in uses)
    with open(sys.argv[2], "w", encoding="utf-8") as f:
        f.write(head + ',\n "uses": [\n  ' + lines + "\n ]\n}\n")
    json.load(open(sys.argv[2], encoding="utf-8"))
    print("uses.json:", len(uses), "uses of", len({u["symbol"] for u in uses}), "symbols")

if __name__ == "__main__":
    main()

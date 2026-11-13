#!/usr/bin/env bash
# The icons' sheet in Chromium 152 (GM1 step 3; Docs/Game/Foundation.md, "The symbols as vector icons (GM1-7)"): every
# icon of Assets/UI/icons as inline SVG beside the glyph Chromium draws for its symbol from the old game's fonts, at
# every size, colour, opacity and ground the old game gives the symbol (Tools/IconSheet/uses.json), in the light and
# the dark theme, and each icon at 96 px over its box. PNG pages at 1x, made with Tools/PaintReference (an invisible
# window) by Tools/IconSheet/sheet.py's fixtures. On a machine with cage (the PC) it runs inside a headless cage of its
# own, so nothing shows on the screen; without it (the cloud) chromium.sh runs under Xvfb.
#   Tools/Scripts/make-icon-sheet.sh [--out <folder>] [--icons <label>=<folder> ...] [--symbols <characters>] [--name <name>] [--note <text>]
#     --out      where the PNG pages go (default: a new temporary folder, printed)
#     --icons    icon folders to show, each with its icons.json (default: Icons=Assets/UI/icons); several show side by side
#     --symbols  only these symbols (default: every icon in the first folder's icons.json, in its order)
#     --name     the pages' names: <name>-uses-light.png, <name>-large-dark.png ... (default: icons)
#     --note     a line under each page's title
#   Tools/Scripts/make-icon-sheet.sh --uses
#     measures the uses again in the old game (its headless test client) and rewrites Tools/IconSheet/uses.json.
set -euo pipefail
source "$(dirname "$0")/env.sh"
SELF="$STATVENTURE_SCRIPTS/make-icon-sheet.sh"

# Inside a headless cage of its own (wlroots' headless backend, the user's displays unset), ended when the run ends.
if [ -z "${STATVENTURE_ICON_SHEET_CAGED:-}" ] && command -v cage >/dev/null 2>&1; then
  code_file="$(mktemp)"
  log_file="$(mktemp)"
  trap 'rm -f "$code_file" "$log_file"' EXIT
  (
    unset DISPLAY WAYLAND_DISPLAY
    export WLR_BACKENDS=headless WLR_LIBINPUT_NO_DEVICES=1 XDG_SESSION_TYPE=wayland STATVENTURE_ICON_SHEET_CAGED=1
    export ICON_SHEET_CODE_FILE="$code_file" ICON_SHEET_LOG_FILE="$log_file"
    cage -- bash -c 'wlr-randr --output HEADLESS-1 --custom-mode 2400x2400 >/dev/null 2>&1 || true; sleep 1
      "$@" > "$ICON_SHEET_LOG_FILE" 2>&1; echo $? > "$ICON_SHEET_CODE_FILE"; kill -TERM $PPID 2>/dev/null' bash "$SELF" "$@"
  ) > /dev/null 2>&1 || true
  grep -v -e "^Errors from xkbcomp" -e "radv is not a conformant" -e "xcb error" "$log_file" || true
  exit "$(cat "$code_file" 2>/dev/null || echo 1)"
fi

SHEET="$STATVENTURE_SOURCE/Tools/IconSheet"
if [ "${1:-}" = "--uses" ]; then
  WORK="$(mktemp -d)"
  trap 'rm -rf "$WORK"' EXIT
  # The candidates: every installed family fontconfig lists with each symbol.
  python3 - "$SHEET/uses.json" "$SHEET/old-game-uses.js" "$WORK/probe.js" <<'PY'
import json, subprocess, sys
symbols = sorted({u["symbol"] for u in json.load(open(sys.argv[1], encoding="utf-8"))["uses"]})
candidates = {}
for s in symbols:
    out = subprocess.run(["fc-list", f":charset={ord(s):x}", "family"], capture_output=True, text=True).stdout
    candidates[s] = sorted({line.split(",")[0].strip() for line in out.splitlines() if line.strip()})
script = open(sys.argv[2], encoding="utf-8").read()
open(sys.argv[3], "w", encoding="utf-8").write("window.__iconCandidates = " + json.dumps(candidates, ensure_ascii=False) + ";\n" + script)
PY
  timeout 300 "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_OLD_GAME/test-client" --ozone-platform=x11 \
    --script="$WORK/probe.js" --call=iconProbe --report="$WORK/report.json" --timeout=280 >/dev/null 2>&1 || true
  python3 - "$WORK/report.json" "$WORK/result.json" <<'PY'
import json, sys
error = json.load(open(sys.argv[1])).get("error") or ""
start = error.find("OUT")
if start < 0:
    sys.exit("the old game's uses couldn't be measured: " + error[:3000])
data, _ = json.JSONDecoder().raw_decode(error[start + 3:])
json.dump(data, open(sys.argv[2], "w"), ensure_ascii=False)
PY
  python3 "$SHEET/uses.py" "$WORK/result.json" "$SHEET/uses.json"
  exit 0
fi

OUT="" NAME="icons" SYMBOLS="" NOTE=""
ICONS=()
while [ $# -gt 0 ]; do
  case "$1" in
    --out) OUT="$2"; shift 2 ;;
    --icons) ICONS+=("--icons" "$2"); shift 2 ;;
    --symbols) SYMBOLS="$2"; shift 2 ;;
    --name) NAME="$2"; shift 2 ;;
    --note) NOTE="$2"; shift 2 ;;
    *) echo "make-icon-sheet.sh: unknown argument $1" >&2; exit 2 ;;
  esac
done
[ ${#ICONS[@]} -gt 0 ] || ICONS=("--icons" "Icons=$STATVENTURE_SOURCE/Assets/UI/icons")
[ -n "$OUT" ] || OUT="$(mktemp -d)"
mkdir -p "$OUT"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
python3 "$SHEET/sheet.py" --uses "$SHEET/uses.json" "${ICONS[@]}" --symbols "$SYMBOLS" --name "$NAME" --note "$NOTE" --out "$WORK/fixtures.json"
mkdir -p "$WORK/shots"
timeout 600 "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_SOURCE/Tools/PaintReference" --ozone-platform=x11 \
  --fixtures="$WORK/fixtures.json" --scale=1 --out="$WORK/shots" --fonts="$STATVENTURE_OLD_GAME/objects/fonts" 2>&1 \
  | grep -v -e "radv is not a conformant" -e " capture " || true
# Each page cut to its content (its fixture is taller than it needs; PaintReference measured every element).
python3 - "$WORK/fixtures.json" "$WORK/shots" "$OUT" <<'PY'
import json, math, os, sys
from PIL import Image
for fixture in json.load(open(sys.argv[1], encoding="utf-8")):
    shot = os.path.join(sys.argv[2], f"ui-{fixture['name']}@1.png")
    if not os.path.exists(shot):
        sys.exit(f"Chromium made no picture of {fixture['name']}")
    rects = json.load(open(os.path.join(sys.argv[2], f"ui-{fixture['name']}@1.json")))
    bottom = max(y + h for _, x, y, w, h in rects[1:])
    right = max(x + w for _, x, y, w, h in rects[1:])
    if bottom > fixture["height"] - 2 or right > fixture["width"] + 0.5:
        sys.exit(f"{fixture['name']}: the content ({right:.0f} x {bottom:.0f} px) doesn't fit its page ({fixture['width']} x {fixture['height']} px)")
    image = Image.open(shot).convert("RGB")
    image = image.crop((0, 0, image.width, min(image.height, math.ceil(bottom) + 16)))
    if fixture.get("zoom"):
        # The small uses' page: the same picture enlarged under it, each pixel a block (nearest neighbour).
        k = fixture["zoom"]
        big = image.resize((image.width * k, image.height * k), Image.NEAREST)
        both = Image.new("RGB", (big.width, image.height + big.height), image.getpixel((2, 2)))
        both.paste(image, (0, 0))
        both.paste(big, (0, image.height))
        image = both
    target = os.path.join(sys.argv[3], fixture["name"] + ".png")
    image.save(target)
    print("wrote", target)
PY

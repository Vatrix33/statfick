#!/usr/bin/env bash
# Drives the current game through its screens in Chromium 152 on a screen of a device scale
# (default 1.65, this machine's) and stores each screen's markup, environment and measured layout
# as Tests/Statventure.Tests/Golden/game-ui@<scale>.json.gz and its screenshot as
# Golden/game-<state>@<scale>.png (Tools/GameReference: the test client's capture in a real X11
# window with a forced device scale factor), driven by the committed capture script
# Reference/game-capture.js. On the PC run it inside a headless compositor with its own Xwayland (it needs X11).
set -euo pipefail
source "$(dirname "$0")/env.sh"
SCALE="${1:-1.65}"
TESTS="$STATVENTURE_SOURCE/Tests/Statventure.Tests"
GAME="$STATVENTURE_OLD_GAME"
REPORT="$(mktemp -d)/report.json"
timeout 700 "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_SOURCE/Tools/GameReference" --ozone-platform=x11 --scale="$SCALE" --game-folder="$GAME" \
  --script="$TESTS/Reference/game-capture.js" --call=uiCapture --report="$REPORT" --timeout=650 2>&1 | grep -v "radv is not a conformant" || true
python3 - "$REPORT" "$TESTS/Golden/game-ui@$SCALE.json.gz" "$TESTS/Golden" "$SCALE" <<'PY'
import gzip, json, os, sys
from PIL import Image
report = json.load(open(sys.argv[1]))
scale = float(sys.argv[4])
error = report.get("error") or ""
start = error.find("OUT")
if start < 0:
    sys.exit("the capture failed: " + error[:2000])
data, _ = json.JSONDecoder().raw_decode(error[start + 3:])
with gzip.open(sys.argv[2], "wt") as out:
    json.dump(data, out, separators=(",", ":"))
profile = report.get("profile", "")
for state in data["states"]:
    source = os.path.join(profile, "ui-" + state["name"] + ".png")
    if os.path.exists(source):
        width, height = round(state["width"] * scale), round(state["height"] * scale)
        Image.open(source).convert("RGBA").crop((0, 0, width, height)).save(os.path.join(sys.argv[3], "game-" + state["name"] + "@" + sys.argv[4] + ".png"))
print("captured", [s["name"] for s in data["states"]], "with", data["userAgent"], "at", data.get("devicePixelRatio"))
PY

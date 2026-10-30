#!/usr/bin/env bash
# Drives the current game through its screens in the Stat Fick test client and stores each
# screen's markup, environment and measured layout as Tests/Statventure.Tests/Golden/game-ui.json.gz,
# with a copy of the game's stylesheet in Tests/Statventure.Tests/Data/Game/styles.css.
set -euo pipefail
source "$(dirname "$0")/env.sh"
TESTS="$STATVENTURE_SOURCE/Tests/Statventure.Tests"
GAME="$STATVENTURE_OLD_GAME"
REPORT="$(mktemp -d)/report.json"
timeout 400 "$STATVENTURE_SCRIPTS/chromium.sh" "$GAME/test-client" --script="$TESTS/Reference/game-capture.js" --call=uiCapture --report="$REPORT" --timeout=370 >/dev/null 2>&1 || true
cp "$GAME/styles.css" "$TESTS/Data/Game/styles.css"
python3 - "$REPORT" "$TESTS/Golden/game-ui.json.gz" "$TESTS/Golden" <<'PY'
import gzip, json, os, sys
from PIL import Image
report = json.load(open(sys.argv[1]))
error = report.get("error") or ""
start = error.find("OUT")
if start < 0:
    sys.exit("the capture failed: " + error[:2000])
data, _ = json.JSONDecoder().raw_decode(error[start + 3:])
with gzip.open(sys.argv[2], "wt") as out:
    json.dump(data, out, separators=(",", ":"))
# The screenshots the capture took of each state.
profile = report.get("profile", "")
for state in data["states"]:
    source = os.path.join(profile, "ui-" + state["name"] + ".png")
    if os.path.exists(source):
        Image.open(source).convert("RGBA").crop((0, 0, state["width"], state["height"])).save(os.path.join(sys.argv[3], "game-" + state["name"] + ".png"))
print("captured", [s["name"] for s in data["states"]], "with", data["userAgent"])
PY

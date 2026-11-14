#!/usr/bin/env bash
# Records how Chromium 152 composites the game's screens on a screen of a device scale (default 1.65):
# each screen's cc layers (name, compositing reasons, screen quad, opaque, why LCD text is disallowed)
# from cc's debug trace, into Tests/Statventure.Tests/Reference/scaled/game-layers@<scale>.json
# (Tools/GameReference --layers), driven by the committed capture script Reference/game-capture.js. On the PC run
# it inside a headless compositor with its own Xwayland (it needs X11).
set -euo pipefail
source "$(dirname "$0")/env.sh"
SCALE="${1:-1.65}"
TESTS="$STATVENTURE_SOURCE/Tests/Statventure.Tests"
GAME="$STATVENTURE_OLD_GAME"
REPORT="$(mktemp -d)/report.json"
timeout 900 "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_SOURCE/Tools/GameReference" --ozone-platform=x11 --scale="$SCALE" --game-folder="$GAME" \
  --script="$TESTS/Reference/game-capture.js" --call=uiCapture --report="$REPORT" --timeout=850 --layers 2>&1 | grep -v "radv is not a conformant" || true
mkdir -p "$TESTS/Reference/scaled"
python3 - "$REPORT" "$TESTS/Reference/scaled/game-layers@$SCALE.json" <<'PY'
import json, os, sys, glob
report = json.load(open(sys.argv[1]))
profile = report.get("profile", "")
screens = {}
for path in sorted(glob.glob(os.path.join(profile, "layers-ui-*.json"))):
    screens[os.path.basename(path)[len("layers-ui-"):-len(".json")]] = json.load(open(path))
if not screens:
    sys.exit("no layers recorded: " + (report.get("error") or "")[:2000])
json.dump(screens, open(sys.argv[2], "w"), indent=1)
print("layers of", list(screens), "->", sys.argv[2])
PY

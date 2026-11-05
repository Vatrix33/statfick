#!/usr/bin/env bash
# Captures the old game's boss intro card at the proof's sampled times (Docs/Scene.md, step 7, phase 2;
# Q32): Tools/GameReference runs the live old game in Chromium 152 at device scale 1 in a real X11
# window and Tools/GameReference/cutscene-card.js renders Ji'ruan's intro over a flat backdrop with its
# CSS animations paused and seeked to each time; the captures go to
# Tests/Statventure.Tests/Golden/cutscene-card-<time>.png (1200 × 680).
set -euo pipefail
source "$(dirname "$0")/env.sh"
TESTS="$STATVENTURE_SOURCE/Tests/Statventure.Tests"
REPORT="$(mktemp -d)/report.json"
timeout 300 "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_SOURCE/Tools/GameReference" --ozone-platform=x11 --scale=1 --game-folder="$STATVENTURE_OLD_GAME" \
  --script="$STATVENTURE_SOURCE/Tools/GameReference/cutscene-card.js" --call=cutsceneCardCaptures --report="$REPORT" --timeout=250 2>&1 | grep -v "radv is not a conformant" || true
python3 - "$REPORT" "$TESTS/Golden" <<'PY'
import glob, json, os, sys
from PIL import Image
report = json.load(open(sys.argv[1]))
if not report.get("passed"):
    sys.exit("the capture failed: " + (report.get("error") or "")[:2000])
for source in sorted(glob.glob(os.path.join(report["profile"], "cutscene-card-*.png"))):
    image = Image.open(source).convert("RGBA").crop((0, 0, 1200, 680))
    target = os.path.join(sys.argv[2], os.path.basename(source))
    image.save(target)
    print("reference", os.path.basename(target), image.size)
PY

#!/usr/bin/env bash
# Renders the canvas reference scenes with the current game's browser (the Stat Fick test client)
# and stores them as the reference images Tests/Statventure.Tests/Golden/css-<scene>.png.
# Run the tests once first: they write Tests/Statventure.Tests/Reference/reference-scenes.js.
set -euo pipefail
source "$(dirname "$0")/env.sh"
REF="$STATVENTURE_SOURCE/Tests/Statventure.Tests/Reference"
CLIENT="$STATVENTURE_OLD_GAME/test-client"
REPORT="$(mktemp -d)/report.json"
timeout 300 "$STATVENTURE_SCRIPTS/chromium.sh" "$CLIENT" --script="$REF/reference-scenes.js" --call=statventureReferences --report="$REPORT" --timeout=270 >/dev/null 2>&1 || true
PROFILE=$(python3 -c "import json,sys; print(json.load(open(sys.argv[1])).get('profile',''))" "$REPORT")
python3 - "$PROFILE" "$STATVENTURE_SOURCE/Tests/Statventure.Tests/Golden" <<'PY'
import sys, os, re
from PIL import Image
profile, golden = sys.argv[1], sys.argv[2]
script = open(os.path.join(os.path.dirname(golden), "Reference", "reference-scenes.js")).read()
sizes = {m.group(1): (int(m.group(2)), int(m.group(3))) for m in re.finditer(r'"name":"([a-z-]+)","width":(\d+),"height":(\d+)', script)}
for name, (w, h) in sizes.items():
    source = os.path.join(profile, "css-" + name + ".png")
    image = Image.open(source).convert("RGBA").crop((0, 0, w, h))
    image.save(os.path.join(golden, "css-" + name + ".png"))
    print("reference", name, image.size)
PY

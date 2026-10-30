#!/usr/bin/env bash
# Screenshots the UI paint fixtures in the current game's browser (the Stat Fick test client) as
# Tests/Statventure.Tests/Golden/ui-<fixture>.png.
# Run the tests once first: they write Tests/Statventure.Tests/Reference/paint-fixtures.js.
set -euo pipefail
source "$(dirname "$0")/env.sh"
TESTS="$STATVENTURE_SOURCE/Tests/Statventure.Tests"
CLIENT="$STATVENTURE_OLD_GAME/test-client"
REPORT="$(mktemp -d)/report.json"
timeout 300 "$STATVENTURE_SCRIPTS/chromium.sh" "$CLIENT" --script="$TESTS/Reference/paint-fixtures.js" --call=statventurePaintFixtures --report="$REPORT" --timeout=270 >/dev/null 2>&1 || true
PROFILE=$(python3 -c "import json,sys; print(json.load(open(sys.argv[1])).get('profile',''))" "$REPORT")
python3 - "$PROFILE" "$TESTS" <<'PY'
import sys, os, re
from PIL import Image
profile, tests = sys.argv[1], sys.argv[2]
script = open(os.path.join(tests, "Reference", "paint-fixtures.js")).read()
for m in re.finditer(r'"name":"([a-z-]+)","width":(\d+),"height":(\d+)', script):
    name, w, h = m.group(1), int(m.group(2)), int(m.group(3))
    image = Image.open(os.path.join(profile, "ui-" + name + ".png")).convert("RGBA").crop((0, 0, w, h))
    image.save(os.path.join(tests, "Golden", "ui-" + name + ".png"))
    print("reference", name, image.size)
PY

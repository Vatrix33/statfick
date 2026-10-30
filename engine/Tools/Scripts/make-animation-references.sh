#!/usr/bin/env bash
# Samples the UI animation fixtures with the current game's browser (the Stat Fick test client) and
# stores the computed values as Tests/Statventure.Tests/Golden/animation-reference.json.
# Run the tests once first: they write Tests/Statventure.Tests/Reference/animation-fixtures.js.
set -euo pipefail
source "$(dirname "$0")/env.sh"
TESTS="$STATVENTURE_SOURCE/Tests/Statventure.Tests"
CLIENT="$STATVENTURE_OLD_GAME/test-client"
REPORT="$(mktemp -d)/report.json"
timeout 300 "$STATVENTURE_SCRIPTS/chromium.sh" "$CLIENT" --script="$TESTS/Reference/animation-fixtures.js" --call=statventureAnimations --report="$REPORT" --timeout=270 >/dev/null 2>&1 || true
python3 - "$REPORT" "$TESTS/Golden/animation-reference.json" <<'PY'
import json, sys
report = json.load(open(sys.argv[1]))
error = report.get("error") or ""
start = error.find("OUT")
if start < 0:
    sys.exit("the browser script failed: " + error[:2000])
text = error[start + 3:]
data, _ = json.JSONDecoder().raw_decode(text)
json.dump(data, open(sys.argv[2], "w"), indent=0, separators=(",", ":"))
print("sampled", len(data["fixtures"]), "fixtures with", data["userAgent"])
PY

#!/usr/bin/env bash
# Runs the script fixtures in Chromium 152 (Electron 44.5.0, see chromium.sh) and writes
# the order their callbacks ran in to Tests/Statventure.Tests/Reference/script-results.json.
# Run the tests once first: they write Tests/Statventure.Tests/Reference/script-fixtures.json.
set -euo pipefail
source "$(dirname "$0")/env.sh"
TESTS="$STATVENTURE_SOURCE/Tests/Statventure.Tests"
OUT="$TESTS/Reference/script-results.json"
timeout 900 "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_SOURCE/Tools/ScriptReference" \
  --fixtures="$TESTS/Reference/script-fixtures.json" \
  --out="$OUT" \
  --fonts="$STATVENTURE_OLD_GAME/objects/fonts" 2> >(grep -v "radv is not a conformant" >&2)
echo "wrote $OUT"

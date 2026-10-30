#!/usr/bin/env bash
# Plays the input fixtures into Chromium 152 (Electron 44.5.0, see chromium.sh) as real
# input and writes what the page saw to Tests/Statventure.Tests/Reference/input-results.json.
# Run the tests once first: they write Tests/Statventure.Tests/Reference/input-fixtures.json.
# With a device scale (e.g. 1.65) Chromium runs under X11 at that scale and the results go to
# Reference/scaled/input-results@<scale>.json (no screenshots).
set -euo pipefail
source "$(dirname "$0")/env.sh"
SCALE="${1:-1}"
TESTS="$STATVENTURE_SOURCE/Tests/Statventure.Tests"
if [ "$SCALE" = 1 ]; then
  OUT="$TESTS/Reference/input-results.json"
  set -- --shots="$TESTS/Golden"
else
  mkdir -p "$TESTS/Reference/scaled"
  OUT="$TESTS/Reference/scaled/input-results@$SCALE.json"
  set -- --ozone-platform=x11 --scale="$SCALE"
fi
timeout 900 "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_SOURCE/Tools/InputReference" "$@" \
  --fixtures="$TESTS/Reference/input-fixtures.json" \
  --out="$OUT" \
  --fonts="$STATVENTURE_OLD_GAME/objects/fonts" 2> >(grep -v "radv is not a conformant" >&2)
echo "wrote $OUT"

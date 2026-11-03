#!/usr/bin/env bash
# Runs the visibility scenarios (Tools/VisibilityReference: hiding, showing and closing the window,
# a hidden page's timers, the CPU-time budget, an audible page) in Chromium 152 (Electron 44.5.0,
# see chromium.sh) and writes what the pages logged to
# Tests/Statventure.Tests/Reference/visibility-results.json. It takes about six minutes (the pages
# stay hidden for minutes). Scenario names after the script limit it to those (merge by hand).
set -euo pipefail
source "$(dirname "$0")/env.sh"
TESTS="$STATVENTURE_SOURCE/Tests/Statventure.Tests"
OUT="${VISIBILITY_OUT:-$TESTS/Reference/visibility-results.json}"
ONLY=()
[ $# -gt 0 ] && ONLY=(--only="$(IFS=,; echo "$*")")
timeout 900 "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_SOURCE/Tools/VisibilityReference" \
  --out="$OUT" "${ONLY[@]}" 2> >(grep -v "radv is not a conformant" >&2)
echo "wrote $OUT"

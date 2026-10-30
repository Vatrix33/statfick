#!/usr/bin/env bash
# Measures the UI layout fixtures in Chromium 152 on a screen of a device scale (default 1.65,
# this machine's) as Tests/Statventure.Tests/Reference/scaled/layout-<fixture>@<scale>.json: every
# element's box, client and scroll sizes and every text node's line rectangles (CSS px, unrounded).
# Chromium runs under X11 with a forced device scale factor in an invisible window
# (Tools/PaintReference --measure=layout).
# Run the tests once first: they write Tests/Statventure.Tests/Reference/layout-fixtures.json.
set -euo pipefail
source "$(dirname "$0")/env.sh"
SCALE="${1:-1.65}"
# An optional page zoom on top (Electron's zoom factor, 1.2 to the power of the zoom level).
PAGE_ZOOM="${2:-1}"
TESTS="$STATVENTURE_SOURCE/Tests/Statventure.Tests"
OUT="$(mktemp -d)"
timeout 900 "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_SOURCE/Tools/PaintReference" --ozone-platform=x11 \
  --fixtures="$TESTS/Reference/layout-fixtures.json" --scale="$SCALE" --page-zoom="$PAGE_ZOOM" --measure=layout --out="$OUT" \
  --fonts="$STATVENTURE_OLD_GAME/objects/fonts" 2>&1 | grep -v "radv is not a conformant"
mkdir -p "$TESTS/Reference/scaled"
mv "$OUT"/layout-*.json "$TESTS/Reference/scaled/"
rm -rf "$OUT"
echo "wrote Reference/scaled/layout-*@$SCALE.json"

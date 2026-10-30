#!/usr/bin/env bash
# Screenshots the UI paint fixtures in Chromium 152 on a screen of a device scale (default 1.65,
# this machine's) as Tests/Statventure.Tests/Golden/ui-<fixture>@<scale>.png, and their element
# boxes as Tests/Statventure.Tests/Reference/scaled/ui-<fixture>@<scale>.json. Chromium runs under
# X11 with a forced device scale factor in an invisible window (Tools/PaintReference).
# Run the tests once first: they write Tests/Statventure.Tests/Reference/paint-fixtures.json.
set -euo pipefail
source "$(dirname "$0")/env.sh"
SCALE="${1:-1.65}"
# An optional page zoom on top (Electron's zoom factor, 1.2 to the power of the zoom level).
PAGE_ZOOM="${2:-1}"
# Another fixture list in Reference/ (e.g. text-fixtures.json, the glyph sheets).
FIXTURES="${3:-paint-fixtures.json}"
TESTS="$STATVENTURE_SOURCE/Tests/Statventure.Tests"
OUT="$(mktemp -d)"
timeout 600 "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_SOURCE/Tools/PaintReference" --ozone-platform=x11 \
  --fixtures="$TESTS/Reference/$FIXTURES" --scale="$SCALE" --page-zoom="$PAGE_ZOOM" --out="$OUT" \
  --fonts="$STATVENTURE_OLD_GAME/objects/fonts" 2>&1 | grep -v "radv is not a conformant"
mkdir -p "$TESTS/Reference/scaled"
mv "$OUT"/*.png "$TESTS/Golden/"
mv "$OUT"/*.json "$TESTS/Reference/scaled/"
rm -rf "$OUT"
echo "wrote Golden/ui-*@$SCALE.png and Reference/scaled/ui-*@$SCALE.json"

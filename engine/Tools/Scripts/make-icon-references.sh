#!/usr/bin/env bash
# Screenshots the icon fixtures (GM1 step 7; Docs/Game/Foundation.md, "How it works", step 7) in Chromium 152: each
# fixture of Tests/Statventure.Tests/Reference/icon-fixtures.json at its own device scale (the list holds one per
# fixture and scale: where the engine's glyphs land differs between the scales), as
# Tests/Statventure.Tests/Golden/ui-<fixture>@<scale>.png. Chromium runs under X11 with a forced device scale factor
# in an invisible window (Tools/PaintReference), as make-scaled-paint-references.sh runs it; on a desktop run it inside a
# headless compositor with a large output (2400 × 2400), never on the user's screen.
# Run the tests once first: IconReferenceTests writes the list.
#   Tools/Scripts/make-icon-references.sh [scale ...]   (default: 1 1.65)
set -euo pipefail
source "$(dirname "$0")/env.sh"
TESTS="$STATVENTURE_SOURCE/Tests/Statventure.Tests"
SCALES=("$@")
[ ${#SCALES[@]} -gt 0 ] || SCALES=(1 1.65)
for SCALE in "${SCALES[@]}"; do
  OUT="$(mktemp -d)"
  # The list's fixtures for this scale.
  python3 -c "import json, sys; json.dump([f for f in json.load(open(sys.argv[1])) if f['scale'] == float(sys.argv[3])], open(sys.argv[2], 'w'))" \
    "$TESTS/Reference/icon-fixtures.json" "$OUT/fixtures.json" "$SCALE"
  mkdir -p "$OUT/shots"
  timeout 600 "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_SOURCE/Tools/PaintReference" --ozone-platform=x11 \
    --fixtures="$OUT/fixtures.json" --scale="$SCALE" --out="$OUT/shots" \
    --fonts="$STATVENTURE_OLD_GAME/objects/fonts" 2>&1 | grep -v "radv is not a conformant" || true
  count=0
  for shot in "$OUT"/shots/ui-*@"$SCALE".png; do
    [ -f "$shot" ] || continue
    mv -- "$shot" "$TESTS/Golden/"
    echo "wrote Golden/$(basename "$shot")"
    count=$((count + 1))
  done
  expected=$(python3 -c "import json, sys; print(len(json.load(open(sys.argv[1]))))" "$OUT/fixtures.json")
  rm -rf "$OUT"
  if [ "$count" -ne "$expected" ]; then
    echo "only $count of $expected fixtures captured at $SCALE" >&2
    exit 1
  fi
done

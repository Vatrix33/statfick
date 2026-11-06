#!/usr/bin/env bash
# Screenshots the editor's screens in Chromium 152 at device scales 1 and 1.65 (M11-20, P36) as
# Tests/Statventure.Tests/Golden/editor-<fixture>@<scale>.png, with Tools/PaintReference (X11, a forced device
# scale factor, an invisible window), from the fixture list the tests write (Reference/editor-fixtures.json;
# Docs/Editor.md, "How it works", step 4). Run the tests once first. Names given: only those fixtures (a step
# captures its own screens and leaves the others' goldens alone).
#   Tools/Scripts/make-editor-references.sh [fixture ...]
set -euo pipefail
source "$(dirname "$0")/env.sh"
TESTS="$STATVENTURE_SOURCE/Tests/Statventure.Tests"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
python3 - "$TESTS/Reference/editor-fixtures.json" "$OUT" "$@" <<'PY'
import json, os, sys
fixtures = json.load(open(sys.argv[1]))
wanted = sys.argv[3:]
missing = [name for name in wanted if name not in {f["name"] for f in fixtures}]
if missing:
    sys.exit("not in the fixture list: " + ", ".join(missing))
chosen = [f for f in fixtures if not wanted or f["name"] in wanted]
json.dump(chosen, open(os.path.join(sys.argv[2], "fixtures.json"), "w"))
open(os.path.join(sys.argv[2], "names"), "w").write("".join(f["name"] + "\n" for f in chosen))
PY
for scale in 1 1.65; do
  mkdir -p "$OUT/$scale"
  timeout 600 "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_SOURCE/Tools/PaintReference" --ozone-platform=x11 \
    --fixtures="$OUT/fixtures.json" --scale="$scale" --out="$OUT/$scale" \
    --fonts="$STATVENTURE_OLD_GAME/objects/fonts" 2>&1 | grep -v "radv is not a conformant" || true
  while read -r name; do
    shot="$OUT/$scale/ui-$name@$scale.png"
    [ -f "$shot" ] || { echo "Chromium made no screenshot of $name at $scale" >&2; exit 1; }
    mv -- "$shot" "$TESTS/Golden/editor-$name@$scale.png"
    echo "wrote Golden/editor-$name@$scale.png"
  done < "$OUT/names"
done

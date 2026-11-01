#!/usr/bin/env bash
# V8's math reference (Docs/Audio.md, step 6): Math.exp and ** over the engine's argument families,
# computed by Chromium 152's V8 (Electron 44.5.0 run as Node), to
# Tests/Statventure.Tests/Reference/v8-math.json (half a minute).
set -euo pipefail
source "$(dirname "$0")/env.sh"
ELECTRON="$STATVENTURE_CACHE/electron-44.5.0/node_modules/electron/dist/electron"
[ -x "$ELECTRON" ] || { echo "Electron 44.5.0 is not installed yet: run any harness through Tools/Scripts/chromium.sh once" >&2; exit 1; }
OUT="$STATVENTURE_SOURCE/Tests/Statventure.Tests/Reference/v8-math.json"
ELECTRON_RUN_AS_NODE=1 "$ELECTRON" "$STATVENTURE_SOURCE/Tools/AudioReference/v8-math.cjs" > "$OUT"
echo "wrote $OUT"

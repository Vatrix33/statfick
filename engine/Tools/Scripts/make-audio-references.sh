#!/usr/bin/env bash
# Renders the audio fixtures in Chromium 152's OfflineAudioContext (Electron 44.5.0, see chromium.sh)
# into Tests/Statventure.Tests/Golden/audio/<name>.f32.gz (Docs/Audio.md, "Method").
# Run the tests once first: they write Tests/Statventure.Tests/Reference/audio-fixtures.json.
# Extra arguments name the fixtures to render (all by default).
set -euo pipefail
source "$(dirname "$0")/env.sh"
TESTS="$STATVENTURE_SOURCE/Tests/Statventure.Tests"
ONLY=()
if [ $# -gt 0 ]; then ONLY=("--only=$(IFS=,; echo "$*")"); fi
timeout 1800 "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_SOURCE/Tools/AudioReference" \
  --fixtures="$TESTS/Reference/audio-fixtures.json" --out="$TESTS/Golden/audio" "${ONLY[@]}" \
  2> >(grep -v "radv is not a conformant\|dbus\|Fontconfig" >&2)

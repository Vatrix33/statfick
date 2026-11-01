#!/usr/bin/env bash
# The music references (Docs/Audio.md, step 6): the old game's composer in Chromium 152
# (Tools/AudioReference/music.cjs) compiles and lints every theme and performs it on a recording
# stand-in context; writes Tests/Statventure.Tests/Reference/music-scores.json and music-logs.json.
set -euo pipefail
source "$(dirname "$0")/env.sh"
timeout 1200 "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_SOURCE/Tools/AudioReference" \
  --old-game="$STATVENTURE_SOURCE/../old-game/scripts" \
  --music-references="$STATVENTURE_SOURCE/Tests/Statventure.Tests/Reference" \
  2> >(grep -v "radv is not a conformant\|dbus\|Fontconfig" >&2)

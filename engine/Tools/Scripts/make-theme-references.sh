#!/usr/bin/env bash
# The full themes' references (Docs/Audio.md, step 6), too big for the repo: every theme rendered 4
# times by the old game's music.js renderOffline in Chromium 152 (Tools/AudioReference/themes.cjs),
# kept as per-sample min/max in the build cache ($STATVENTURE_CACHE/audio-themes, about 2.5 GB; half
# an hour). STATVENTURE_THEMES=1 Tools/Scripts/test.sh --filter ThemeRenderTests compares the engine.
# Extra arguments name the themes to render (all by default).
set -euo pipefail
source "$(dirname "$0")/env.sh"
ONLY=()
if [ $# -gt 0 ]; then ONLY=("--only=$(IFS=,; echo "$*")"); fi
timeout 7200 "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_SOURCE/Tools/AudioReference" \
  --old-game="$STATVENTURE_SOURCE/../old-game/scripts" --themes-out="$STATVENTURE_CACHE/audio-themes" --runs=4 "${ONLY[@]}" \
  2> >(grep -v "radv is not a conformant\|dbus\|Fontconfig" >&2)

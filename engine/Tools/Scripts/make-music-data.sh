#!/usr/bin/env bash
# The music data (Docs/Audio.md, step 6): reads the old game's theme specs and the composer's
# instrument, percussion and room tables in Chromium 152 (Tools/AudioReference/music.cjs) and writes
# them as C# records to Engine/Statventure/Audio/Music/MusicData.g.cs.
set -euo pipefail
source "$(dirname "$0")/env.sh"
timeout 600 "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_SOURCE/Tools/AudioReference" \
  --old-game="$STATVENTURE_SOURCE/../old-game/scripts" \
  --music-data="$STATVENTURE_SOURCE/Engine/Statventure/Audio/Music/MusicData.g.cs" \
  2> >(grep -v "radv is not a conformant\|dbus\|Fontconfig" >&2)

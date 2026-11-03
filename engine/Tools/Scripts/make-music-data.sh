#!/usr/bin/env bash
# The music importer (Docs/Assets.md, step 6; Docs/Audio.md, step 6): reads the old game's theme specs and
# the composer's instrument, percussion and room tables in Chromium 152 (Tools/AudioReference/music.cjs),
# which writes each as its record's JSON form, plus the theme order, into a scratch folder in the build
# cache ($STATVENTURE_CACHE/music-data, emptied first and kept for inspection; S6-5); then the packer packs
# that folder into engine/Assets/Packs/audio.pak (unchanged entries keep their stored bytes).
set -euo pipefail
source "$(dirname "$0")/env.sh"
out="$STATVENTURE_CACHE/music-data"
rm -rf "$out"
mkdir -p "$out" "$STATVENTURE_SOURCE/Assets/Packs"
timeout 600 "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_SOURCE/Tools/AudioReference" \
  --old-game="$STATVENTURE_SOURCE/../old-game/scripts" \
  --music-data="$out" \
  2> >(grep -v "radv is not a conformant\|dbus\|Fontconfig" >&2)
dotnet build "$STATVENTURE_SOURCE/Tools/Packer" -c Release -v quiet -nologo -clp:ErrorsOnly
dotnet "$STATVENTURE_SOURCE/Tools/Packer/bin/Release/net10.0/Packer.dll" pack "$out" "$STATVENTURE_SOURCE/Assets/Packs/audio.pak"

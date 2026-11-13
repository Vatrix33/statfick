#!/usr/bin/env bash
# Makes engine/Assets/Packs/audio.pak from the repo (Docs/Assets.md, step 6; Docs/Audio.md, step 6 and "The game's
# sound effects"). The music importer reads the old game's theme specs and the composer's instrument, percussion and
# room tables in Chromium 152 (Tools/AudioReference/music.cjs), which writes each as its record's JSON form, plus the
# theme order, into a scratch folder in the build cache ($STATVENTURE_CACHE/music-data, emptied first and kept for
# inspection; S6-5); the game's sound effects, the JSON forms in engine/Assets/Audio/effects/ (GM1-P16), are copied
# in beside them as audio/effects/<name>; then the packer packs that folder into audio.pak (unchanged entries keep
# their stored bytes). Without a display (a cloud session) Chromium runs under Xvfb; on the PC from a script, inside a
# headless compositor.
set -euo pipefail
source "$(dirname "$0")/env.sh"
out="$STATVENTURE_CACHE/music-data"
rm -rf "$out"
mkdir -p "$out" "$STATVENTURE_SOURCE/Assets/Packs"
timeout 600 "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_SOURCE/Tools/AudioReference" \
  --old-game="$STATVENTURE_SOURCE/../old-game/scripts" \
  --music-data="$out" \
  2> >(grep -v "radv is not a conformant\|dbus\|Fontconfig" >&2)
if [ -e "$out/audio/effects" ]; then
  echo "the music importer wrote audio/effects/, which the game's sound effects fill" >&2
  exit 1
fi
mkdir -p "$out/audio/effects"
cp "$STATVENTURE_SOURCE"/Assets/Audio/effects/*.json "$out/audio/effects/"
dotnet build "$STATVENTURE_SOURCE/Tools/Packer" -c Release -v quiet -nologo -clp:ErrorsOnly
dotnet "$STATVENTURE_SOURCE/Tools/Packer/bin/Release/net10.0/Packer.dll" pack "$out" "$STATVENTURE_SOURCE/Assets/Packs/audio.pak"

#!/usr/bin/env bash
# The UI pack (Docs/Assets.md, step 7; Q1, S7-5, S7-6; GM1 step 1a): copies the old game's five font faces and
# their two licence texts (old-game/objects/fonts/) into a scratch folder in the build cache
# ($STATVENTURE_CACHE/ui-data, emptied first and kept for inspection) under names that follow the path rule, at
# ui/fonts/, and the port's stylesheets (engine/Assets/UI/styles/*.css, GM1-P8) at ui/styles/, then packs that folder
# into engine/Assets/Packs/ui.pak (unchanged entries keep their stored bytes). The packer's UI importer compiles
# each .css into the record at its path without the ending (ui/styles/game).
set -euo pipefail
source "$(dirname "$0")/env.sh"
fonts="$STATVENTURE_OLD_GAME/objects/fonts"
out="$STATVENTURE_CACHE/ui-data"
rm -rf "$out"
mkdir -p "$out/ui/fonts" "$out/ui/styles" "$STATVENTURE_SOURCE/Assets/Packs"
for sheet in "$STATVENTURE_SOURCE"/Assets/UI/styles/*.css; do
  if [ -f "$sheet" ]; then cp "$sheet" "$out/ui/styles/"; fi
done
while read -r from to; do
  cp "$fonts/$from" "$out/ui/fonts/$to"
done <<'LIST'
Lato-Regular.ttf lato-regular.ttf
Lato-Semibold.ttf lato-semibold.ttf
Lato-Bold.ttf lato-bold.ttf
STIXTwoText-Regular.otf stixtwotext-regular.otf
STIXTwoText-Italic.otf stixtwotext-italic.otf
Lato-OFL.txt lato-ofl.txt
STIX-OFL.txt stix-ofl.txt
LIST
dotnet build "$STATVENTURE_SOURCE/Tools/Packer" -c Release -v quiet -nologo -clp:ErrorsOnly
dotnet "$STATVENTURE_SOURCE/Tools/Packer/bin/Release/net10.0/Packer.dll" pack "$out" "$STATVENTURE_SOURCE/Assets/Packs/ui.pak"

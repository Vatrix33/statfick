#!/usr/bin/env bash
# The UI pack (Docs/Assets.md, step 7; Q1, S7-5, S7-6): copies the old game's five font faces and their two
# licence texts (old-game/objects/fonts/) into a scratch folder in the build cache ($STATVENTURE_CACHE/ui-data,
# emptied first and kept for inspection) under names that follow the path rule, at ui/fonts/, then packs that
# folder into engine/Assets/Packs/ui.pak (unchanged entries keep their stored bytes). The port's stylesheet
# and images join the folder when they are written; the packer's UI importer compiles each .css.
set -euo pipefail
source "$(dirname "$0")/env.sh"
fonts="$STATVENTURE_OLD_GAME/objects/fonts"
out="$STATVENTURE_CACHE/ui-data"
rm -rf "$out"
mkdir -p "$out/ui/fonts" "$STATVENTURE_SOURCE/Assets/Packs"
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

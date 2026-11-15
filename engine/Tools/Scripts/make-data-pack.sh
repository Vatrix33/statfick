#!/usr/bin/env bash
# The data pack (GM2-P4; Docs/Game/Hero.md, "How it works", step 1): packs engine/Assets/Data, the game's data as its
# entries' JSON forms at their paths (data/races/<id>.json: the races, written from the old game's races.json by the
# game's tests' converter, STATVENTURE_RACE_DATA=write), into engine/Assets/Packs/data.pak with the packer (Release),
# which knows the game's record kinds. The pack is made anew each time (into a new file, then put in the old one's
# place: no stored bytes taken over from it), so the same folder gives the same bytes on every machine; the game's
# tests hold the committed pack to that (RaceDataTests).
#   Tools/Scripts/make-data-pack.sh
set -euo pipefail
source "$(dirname "$0")/env.sh"
data="$STATVENTURE_SOURCE/Assets/Data"
pack="$STATVENTURE_SOURCE/Assets/Packs/data.pak"
[ -d "$data" ] || { echo "make-data-pack.sh: no folder $data" >&2; exit 1; }
mkdir -p "$(dirname "$pack")"
dotnet build "$STATVENTURE_SOURCE/Tools/Packer" -c Release -v quiet -nologo -clp:ErrorsOnly
fresh="$pack.$$.tmp"
rm -f -- "$fresh"
trap 'rm -f -- "$fresh"' EXIT
dotnet "$STATVENTURE_SOURCE/Tools/Packer/bin/Release/net10.0/Packer.dll" pack "$data" "$fresh"
mv -f -- "$fresh" "$pack"

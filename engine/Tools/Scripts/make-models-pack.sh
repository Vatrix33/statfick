#!/usr/bin/env bash
# The models pack (GM2-P4; Docs/Game/Hero.md, "How it works", step 4a): packs the committed models, engine/Assets/Models
# (the humanoid family's .glb files, made in Blender by Tools/Scripts/make-humanoid.sh build), into
# engine/Assets/Packs/models.pak with the packer (Release), whose model importer reads each .glb into the record at its
# path under models/ without the ending (Assets/Models/humanoid/bodies/human-male-average.glb is the entry
# models/humanoid/bodies/human-male-average). The files are copied into a scratch folder of this run's own (laid out
# under models/, removed after) and the pack is made anew each time (into a new file, then put in the old one's place:
# no stored bytes taken over from it), so the same models give the same bytes on every machine; the engine's tests hold
# the committed pack to that (Humanoid/HumanoidPackTests).
#   Tools/Scripts/make-models-pack.sh
set -euo pipefail
source "$(dirname "$0")/env.sh"
models="$STATVENTURE_SOURCE/Assets/Models"
pack="$STATVENTURE_SOURCE/Assets/Packs/models.pak"
[ -d "$models" ] || { echo "make-models-pack.sh: no folder $models" >&2; exit 1; }
mkdir -p "$(dirname "$pack")"
dotnet build "$STATVENTURE_SOURCE/Tools/Packer" -c Release -v quiet -nologo -clp:ErrorsOnly
scratch="$(mktemp -d "${TMPDIR:-/tmp}/statventure-models-pack.XXXXXX")"
fresh="$pack.$$.tmp"
rm -f -- "$fresh"
trap 'rm -rf -- "$scratch" "$fresh"' EXIT
cp -r "$models" "$scratch/models"
dotnet "$STATVENTURE_SOURCE/Tools/Packer/bin/Release/net10.0/Packer.dll" pack "$scratch" "$fresh"
mv -f -- "$fresh" "$pack"

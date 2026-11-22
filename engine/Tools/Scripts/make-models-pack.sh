#!/usr/bin/env bash
# The models packs (GM2-P4; the user's Q77 of GM2, a pack per race; Docs/Game/Hero.md, "How it works", step 4a and the
# race packs): packs the committed models, engine/Assets/Models (the humanoid family's .glb files, made in Blender by
# Tools/Scripts/make-humanoid.sh build), into engine/Assets/Packs/models.pak and a models-<race>.pak for each race with
# models of its own, with the packer (Release), whose model importer reads each .glb into the record at its path under
# models/ without the ending (Assets/Models/humanoid/bodies/human-male-average.glb is the entry
# models/humanoid/bodies/human-male-average).
# Which pack a file goes in (Docs/Assets.md, "Models and state machines in packs"): a file named
# <race>-<sex>-<build>.glb (sex male or female, build average, slender or strong; the race's id, which may hold a dash,
# dark-elf-male-average.glb) is its race's, in models-<race>.pak, but the human's is in models.pak; every other file (the
# gear, the clips, the machines) is in models.pak. A race's pack is written only when the race has models; one whose race
# has none any more is removed. The engine's tests hold the committed packs to this rule (Humanoid/HumanoidPackTests).
# Each pack's files are copied into a scratch folder of this run's own (laid out under models/, removed after) and each
# pack is made anew each time (into a new file, then put in the old one's place: no stored bytes taken over from it), so
# the same models give the same bytes on every machine.
#   Tools/Scripts/make-models-pack.sh
set -euo pipefail
source "$(dirname "$0")/env.sh"
models="$STATVENTURE_SOURCE/Assets/Models"
packs="$STATVENTURE_SOURCE/Assets/Packs"
[ -d "$models" ] || { echo "make-models-pack.sh: no folder $models" >&2; exit 1; }
mkdir -p "$packs"
dotnet build "$STATVENTURE_SOURCE/Tools/Packer" -c Release -v quiet -nologo -clp:ErrorsOnly
scratch="$(mktemp -d "${TMPDIR:-/tmp}/statventure-models-pack.XXXXXX")"
cleanup() {
  rm -rf -- "$scratch"
  rm -f -- "$packs"/*.pak.$$.tmp
}
trap cleanup EXIT

# The pack of a file under Assets/Models, by its name (the rule above).
pack_of() {
  local name
  name="$(basename "$1")"
  if [[ "$name" =~ ^(.+)-(male|female)-(average|slender|strong)\.glb$ ]] && [ "${BASH_REMATCH[1]}" != human ]; then
    echo "models-${BASH_REMATCH[1]}"
  else
    echo models
  fi
}

while IFS= read -r -d '' file; do
  relative="${file#"$models"/}"
  pack="$(pack_of "$relative")"
  mkdir -p "$scratch/$pack/models/$(dirname "$relative")"
  cp -- "$file" "$scratch/$pack/models/$relative"
done < <(find "$models" -type f -print0)

for folder in "$scratch"/*/; do
  pack="$(basename "$folder")"
  fresh="$packs/$pack.pak.$$.tmp"
  rm -f -- "$fresh"
  dotnet "$STATVENTURE_SOURCE/Tools/Packer/bin/Release/net10.0/Packer.dll" pack "$scratch/$pack" "$fresh"
  mv -f -- "$fresh" "$packs/$pack.pak"
done
# A race's pack whose race has no models any more.
for old in "$packs"/models-*.pak; do
  [ -f "$old" ] || continue
  [ -d "$scratch/$(basename "$old" .pak)" ] || { rm -f -- "$old"; echo "removed $old (no models of its race)"; }
done

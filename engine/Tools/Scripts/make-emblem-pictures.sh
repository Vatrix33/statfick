#!/usr/bin/env bash
# The race emblems' pictures made again from the emblems' models (GM2 step 10; Docs/Game/Hero.md, "How it works", step
# 10), on the PC (Blender 5.2: Tools/Scripts/blender.sh):
# 1. the game's tests write the scenes, Tests/Statventure.Tests/Reference/emblem-scenes.json: each race's emblem
#    (Assets/Models/humanoid/emblems/<name>.glb) in its race's colours (GM2-P31) and the game's look from the pictures'
#    camera, 640 x 640, over black and over white (EmblemReferenceTests, STATVENTURE_EMBLEM_SCENES=write);
# 2. make-blender-references.sh renders them into Tests/Statventure.Tests/Golden/r3d-emblem-*.png and -ink.png;
# 3. the game's tests make the pictures from those renders: the ground made transparent, the lines laid over, scaled down
#    to every size the screens draw an emblem at (GM2-P34: a Lanczos filter of three lobes), engine/Assets/UI/emblems/
#    <emblem>-<size>.png (EmblemPicturesTests, STATVENTURE_EMBLEM_PICTURES=write);
# 4. make-ui-pack.sh packs them into Assets/Packs/ui.pak at ui/emblems/.
# The same models give the same bytes on every run (the renders, the pictures and the pack; EmblemPicturesTests holds
# the committed pictures to the committed renders pixel for pixel wherever the tests run). Step 4 uses the shared build
# cache: with other workers beside, run this under the shared lock.
#   Tools/Scripts/make-emblem-pictures.sh
set -euo pipefail
source "$(dirname "$0")/env.sh"
cd "$STATVENTURE_SOURCE"
"$STATVENTURE_SCRIPTS/build.sh" Debug >/dev/null
STATVENTURE_EMBLEM_SCENES=write dotnet test Tests/Statventure.Game.Tests --no-build -v quiet -nologo \
  --filter "FullyQualifiedName~EmblemReferenceTests.TheSceneFileIsUpToDate"
# The scene file may name other emblems now (a race's emblem added or renamed): render what it names.
names=$(python3 -c 'import json, sys; print(" ".join(s["name"] for s in json.load(open(sys.argv[1]))["scenes"]))' Tests/Statventure.Tests/Reference/emblem-scenes.json)
# shellcheck disable=SC2086
"$STATVENTURE_SCRIPTS/make-blender-references.sh" $names
rm -f Assets/UI/emblems/*.png
STATVENTURE_EMBLEM_PICTURES=write dotnet test Tests/Statventure.Game.Tests --no-build -v quiet -nologo \
  --filter "FullyQualifiedName~EmblemPicturesTests.ThePicturesAreWhatTheRendersMake"
"$STATVENTURE_SCRIPTS/make-ui-pack.sh"

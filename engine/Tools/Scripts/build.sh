#!/usr/bin/env bash
# Builds Statventure: compiles the engine shaders, then the solution, then lays out the game folder
# ($STATVENTURE_GAME): the game's program (Game/Statventure.Game, since GM1 step 2) with the committed packs in packs/
# beside it (Docs/Assets.md, step 9; S9-A).
#   Tools/Scripts/build.sh [Debug|Release]
set -euo pipefail
source "$(dirname "$0")/env.sh"
CONFIG="${1:-Debug}"
cd "$STATVENTURE_SOURCE"
dotnet build Tools/ShaderCompiler -c Release -v quiet -nologo >/dev/null
dotnet Tools/ShaderCompiler/bin/Release/net10.0/ShaderCompiler.dll Engine/Statventure/Shaders/Source Engine/Statventure/Shaders/Bin
dotnet build Statventure.slnx -c "$CONFIG" -v quiet -nologo
# The game folder: the game as the build just made it (framework-dependent), then packs/ made the committed packs
# exactly: a pack no longer committed goes, a changed one is copied beside its place and renamed over it, so a game
# running from the folder keeps reading the old one until it reloads.
dotnet publish Game/Statventure.Game -c "$CONFIG" --no-build -o "$STATVENTURE_GAME" -v quiet -nologo
# The sandbox stood in for the game there until GM1 step 2: its program files and its own Data/ (the tests' fonts, the
# proof cutscene and its models) go; nothing else of the folder is touched.
rm -f -- "$STATVENTURE_GAME"/Statventure.Sandbox "$STATVENTURE_GAME"/Statventure.Sandbox.dll \
  "$STATVENTURE_GAME"/Statventure.Sandbox.pdb "$STATVENTURE_GAME"/Statventure.Sandbox.deps.json \
  "$STATVENTURE_GAME"/Statventure.Sandbox.runtimeconfig.json
rm -rf -- "$STATVENTURE_GAME"/Data/Fonts "$STATVENTURE_GAME"/Data/Cutscenes "$STATVENTURE_GAME"/Data/Models
[ ! -d "$STATVENTURE_GAME/Data" ] || rmdir --ignore-fail-on-non-empty -- "$STATVENTURE_GAME/Data"
mkdir -p "$STATVENTURE_GAME/packs"
for pak in "$STATVENTURE_GAME"/packs/*.pak; do
  [ -f "$pak" ] || continue
  [ -f "Assets/Packs/$(basename "$pak")" ] || rm -f -- "$pak"
done
for pak in Assets/Packs/*.pak; do
  [ -f "$pak" ] || continue
  target="$STATVENTURE_GAME/packs/$(basename "$pak")"
  cmp -s "$pak" "$target" && continue
  cp -- "$pak" "$target.$$.tmp"
  mv -f -- "$target.$$.tmp" "$target"
done

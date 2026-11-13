#!/usr/bin/env bash
# Runs the Statventure Editor (M11, Docs/Editor.md) from the repo, in the dev setup (P5): the game's packs are
# engine/Assets/Packs, which it can save into. A Release build, as the other run scripts. Arguments go to the
# editor (--player-folder <folder>: the player's game folder it uses, by default this machine's; a check runs it
# with a scratch folder).
#   Tools/Scripts/editor.sh [--player-folder <folder>]
set -euo pipefail
source "$(dirname "$0")/env.sh"
cd "$STATVENTURE_SOURCE"
for project in Tools/Editor Game/Statventure.Game.Data; do
  if ! output="$(dotnet build "$project" -c Release -v quiet -nologo 2>&1)"; then
    echo "$output" >&2
    exit 1
  fi
done
exec dotnet Tools/Editor/bin/Release/net10.0/Statventure.Editor.dll --repo "$STATVENTURE_SOURCE" "$@"

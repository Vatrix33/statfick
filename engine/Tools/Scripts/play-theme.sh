#!/usr/bin/env bash
# Plays the game's music through the default output device, to listen to it (Release build).
#   Tools/Scripts/play-theme.sh                       lists the themes
#   Tools/Scripts/play-theme.sh alder-vale [--volume 0-100]
# While it plays, type another theme's id to change to it, or a volume; Enter alone quits.
set -euo pipefail
source "$(dirname "$0")/env.sh"
cd "$STATVENTURE_SOURCE"
if ! output="$(dotnet build Tools/PlayTheme -c Release -v quiet -nologo 2>&1)"; then
  echo "$output" >&2
  exit 1
fi
dotnet Tools/PlayTheme/bin/Release/net10.0/PlayTheme.dll "$@"

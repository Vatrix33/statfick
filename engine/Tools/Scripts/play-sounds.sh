#!/usr/bin/env bash
# Plays the game's sound effects through the default output device, to listen to them (Release build; Docs/Audio.md,
# "The game's sound effects"; GM1's eight, Docs/Game/Foundation.md, "The sounds"):
#   Tools/Scripts/play-sounds.sh [name ...] [--volume 0-100] [--candidates <folder>]
# Prints each sound's name, plays it, pauses and goes on; with no names it plays every effect in audio.pak, by name.
# The volume is the effects volume, 60 by default with the master at 100: what the game gives on a first start (Q22).
# From a script, send only --volume 0 (or set ALSOFT_DRIVERS=null). --candidates plays the patches in a folder's
# <name>.json files (audio.effect JSON forms) instead of audio.pak's, as a step does to have new sounds approved by ear;
# a name then plays each of its candidates (ui-press: ui-press-a, ui-press-b …).
set -euo pipefail
source "$(dirname "$0")/env.sh"
arguments=()
while [ $# -gt 0 ]; do
  case "$1" in
    --candidates)
      [ $# -ge 2 ] || { echo "--candidates takes a folder of patches (<name>.json)" >&2; exit 1; }
      arguments+=(--candidates "$(realpath -- "$2")")
      shift 2;;
    *)
      arguments+=("$1")
      shift;;
  esac
done
cd "$STATVENTURE_SOURCE"
if ! output="$(dotnet build Tools/PlayTheme -c Release -v quiet -nologo 2>&1)"; then
  echo "$output" >&2
  exit 1
fi
dotnet Tools/PlayTheme/bin/Release/net10.0/PlayTheme.dll --effect "${arguments[@]}"

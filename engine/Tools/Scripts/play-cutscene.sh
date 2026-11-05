#!/usr/bin/env bash
# Plays a cutscene in the sandbox's window (Docs/Scene.md, step 7, phase 2; Q33): Ji'ruan's intro, M10's proof.
#   Tools/Scripts/play-cutscene.sh jiruan [--volume 0-100] [--capture file.png --frames N]
# Escape or the card's Skip button skips it, R plays it again, F11 toggles full screen; reduced motion as the
# system sets it (the old still card). With sound: his theme and the three sounds (S7-9–S7-11) through the default
# device; --volume sets the master volume (100 by default; from a script, send only 0). --capture writes the
# window's frame N to the file and quits. A Release build: in Debug the audio thread can't keep up with the theme
# (underruns, heard as crackles).
set -euo pipefail
source "$(dirname "$0")/env.sh"
cd "$STATVENTURE_SOURCE"
name="${1:?which cutscene (jiruan)}"
shift
if ! output="$(dotnet build Samples/Statventure.Sandbox -c Release -v quiet -nologo 2>&1)"; then
  echo "$output" >&2
  exit 1
fi
dotnet Samples/Statventure.Sandbox/bin/Release/net10.0/Statventure.Sandbox.dll --cutscene "$name" "$@"

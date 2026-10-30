#!/usr/bin/env bash
# Makes the 3D test models and references with Blender 5.2 (Docs/Renderer-3D.md):
# 1. Tools/BlenderReference/make_models.py builds the test models and exports them as .glb into
#    Tests/Statventure.Tests/Data/Models (the script is their source);
# 2. Tools/BlenderReference/render_scenes.py renders the test scenes with EEVEE into
#    Tests/Statventure.Tests/Golden/r3d-*.png.
# Run the tests once first: they write Tests/Statventure.Tests/Reference/r3d-scenes.json.
# Extra arguments name the scenes to render (all by default). Blender comes from $STATVENTURE_BLENDER,
# else `blender` on the PATH, else the Flatpak org.blender.Blender.
set -euo pipefail
source "$(dirname "$0")/env.sh"
TESTS="$STATVENTURE_SOURCE/Tests/Statventure.Tests"
if [ -n "${STATVENTURE_BLENDER:-}" ]; then
  BLENDER=("$STATVENTURE_BLENDER")
elif command -v blender >/dev/null; then
  BLENDER=(blender)
else
  BLENDER=(flatpak run org.blender.Blender)
fi
# Absolute paths: the Flatpak starts in its own folder and has a private /tmp. A Python error fails
# the run (--python-exit-code); only what the scripts wrote and errors are shown.
run() {
  local log
  if ! log=$("${BLENDER[@]}" -b --factory-startup --python-exit-code 1 --python "$@" 2>&1); then
    echo "$log" >&2
    exit 1
  fi
  echo "$log" | grep -E "^wrote|Error" || true
}
run "$STATVENTURE_SOURCE/Tools/BlenderReference/make_models.py" -- "$TESTS/Data/Models"
run "$STATVENTURE_SOURCE/Tools/BlenderReference/render_scenes.py" -- "$TESTS/Reference/r3d-scenes.json" "$TESTS/Golden" "$@"

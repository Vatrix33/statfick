#!/usr/bin/env bash
# Renders the 3D test scenes with Blender 5.2 (EEVEE) into Tests/Statventure.Tests/Golden/r3d-*.png,
# the references the engine's 3D renderer is compared with (Docs/Renderer-3D.md).
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
# Absolute paths: the Flatpak starts in its own folder and has a private /tmp.
"${BLENDER[@]}" -b --factory-startup --python "$STATVENTURE_SOURCE/Tools/BlenderReference/render_scenes.py" -- \
  "$TESTS/Reference/r3d-scenes.json" "$TESTS/Golden" "$@"

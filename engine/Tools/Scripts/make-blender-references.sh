#!/usr/bin/env bash
# Makes the 3D test models and references with Blender 5.2 (Docs/Renderer-3D.md):
# 1. Tools/BlenderReference/make_terrain.py writes the test terrain (Data/Terrain) and make_models.py
#    builds the test models and exports them as .glb into Tests/Statventure.Tests/Data/Models (the
#    scripts are their source);
# 2. Tools/BlenderReference/render_scenes.py renders the test scenes with EEVEE into
#    Tests/Statventure.Tests/Golden/r3d-*.png (and r3d-*-ink.png, Freestyle's lines), and into
#    Actual/blender-glow the glowing parts, their models' coverage and the particles alone;
# 3. Tools/BlenderReference/compose_glow.py lays the glow's halo into them (Pillow, as the lizardman's
#    compose.py), then the particles over all (in linear light), using those files up;
# 4. Tools/BlenderReference/evaluate_animations.py evaluates the animated models' actions at the
#    animation scenes' times (Docs/Animation.md) into Tests/Statventure.Tests/Golden/anim-*.json.
# Run the tests once first: they write Tests/Statventure.Tests/Reference/r3d-scenes.json and
# anim-scenes.json. Extra arguments name the scenes to render or evaluate (all by default). Blender comes from $STATVENTURE_BLENDER,
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
python3 "$STATVENTURE_SOURCE/Tools/BlenderReference/make_terrain.py" "$TESTS/Data/Terrain" >/dev/null
run "$STATVENTURE_SOURCE/Tools/BlenderReference/make_models.py" -- "$TESTS/Data/Models"
run "$STATVENTURE_SOURCE/Tools/BlenderReference/render_scenes.py" -- "$TESTS/Reference/r3d-scenes.json" "$TESTS/Golden" "$TESTS/Actual/blender-glow" "$@"
python3 "$STATVENTURE_SOURCE/Tools/BlenderReference/compose_glow.py" "$TESTS/Reference/r3d-scenes.json" "$TESTS/Golden" "$TESTS/Actual/blender-glow"
run "$STATVENTURE_SOURCE/Tools/BlenderReference/evaluate_animations.py" -- "$TESTS/Reference/anim-scenes.json" "$TESTS/Golden" "$@"

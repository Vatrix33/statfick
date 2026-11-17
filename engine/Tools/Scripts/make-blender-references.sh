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
# 5. The sandbox's figure mode's scenes (Reference/figure-scenes.json, GM2 step 3a: the frame's sample in looks, as
#    the figure mode draws it) are rendered as the test scenes are, into Golden/r3d-figure-*.png; so are the humanoid
#    family's (Reference/humanoid-scenes.json, GM2 step 4a: the game's committed models, engine/Assets/Models, assembled
#    and in the game's look), into Golden/r3d-humanoid-*.png.
# Run the tests once first: they write Tests/Statventure.Tests/Reference/r3d-scenes.json,
# anim-scenes.json, figure-scenes.json and humanoid-scenes.json. Extra arguments name the scenes to render or evaluate (all by default).
#
# --out <folder> (GM2 step 3a): everything is made into <folder> instead, laid out as the test project
# (<folder>/Data/Models, Data/Terrain, Golden, Actual/blender-glow, with the scene files copied into
# <folder>/Reference so the models' and terrain's folders resolve there); nothing committed is
# written. Tools/BlenderReference/compare_references.py <folder> then compares it with what is
# committed (how a new Blender is proven: Docs/Renderer-3D.md, "Reference harness").
#
# Blender: Tools/Scripts/blender.sh (Blender 5.2 only, headless; $STATVENTURE_BLENDER, the pinned 5.2.0 by default).
set -euo pipefail
source "$(dirname "$0")/env.sh"
TESTS="$STATVENTURE_SOURCE/Tests/Statventure.Tests"
OUT="$TESTS"
if [ "${1:-}" = "--out" ]; then
  [ -n "${2:-}" ] || { echo "--out needs a folder" >&2; exit 2; }
  mkdir -p "$2"
  OUT="$(cd "$2" && pwd)"
  shift 2
  mkdir -p "$OUT/Reference" "$OUT/Data/Models/humanoid" "$OUT/Data/Terrain" "$OUT/Golden"
  for scenes in r3d-scenes anim-scenes node-scenes stage-scenes curve-scenes figure-scenes humanoid-scenes; do
    [ -f "$TESTS/Reference/$scenes.json" ] && cp "$TESTS/Reference/$scenes.json" "$OUT/Reference/"
  done
  # The figure scenes' model is the humanoid frame's sample (Tools/Models/humanoid, make-humanoid.sh), not made here.
  cp "$TESTS/Data/Models/humanoid/"*.glb "$OUT/Data/Models/humanoid/" 2>/dev/null || true
  # The humanoid scenes' models are the committed ones (made by make-humanoid.sh, not here): the copied file names them
  # by their folder, engine/Assets/Models.
  if [ -f "$OUT/Reference/humanoid-scenes.json" ]; then
    python3 - "$OUT/Reference/humanoid-scenes.json" "$STATVENTURE_SOURCE/Assets/Models" <<'PY'
import json, sys
path, models = sys.argv[1], sys.argv[2]
data = json.load(open(path))
data["modelFolder"] = models
json.dump(data, open(path, "w"), indent=2)
PY
  fi
fi
source "$STATVENTURE_SCRIPTS/blender.sh"
python3 "$STATVENTURE_SOURCE/Tools/BlenderReference/make_terrain.py" "$OUT/Data/Terrain" >/dev/null
blender_run "$STATVENTURE_SOURCE/Tools/BlenderReference/make_models.py" -- "$OUT/Data/Models"
blender_run "$STATVENTURE_SOURCE/Tools/BlenderReference/render_scenes.py" -- "$OUT/Reference/r3d-scenes.json" "$OUT/Golden" "$OUT/Actual/blender-glow" "$@"
python3 "$STATVENTURE_SOURCE/Tools/BlenderReference/compose_glow.py" "$OUT/Reference/r3d-scenes.json" "$OUT/Golden" "$OUT/Actual/blender-glow"
if [ -f "$OUT/Reference/figure-scenes.json" ]; then
  blender_run "$STATVENTURE_SOURCE/Tools/BlenderReference/render_scenes.py" -- "$OUT/Reference/figure-scenes.json" "$OUT/Golden" "$OUT/Actual/blender-glow" "$@"
  python3 "$STATVENTURE_SOURCE/Tools/BlenderReference/compose_glow.py" "$OUT/Reference/figure-scenes.json" "$OUT/Golden" "$OUT/Actual/blender-glow"
fi
if [ -f "$OUT/Reference/humanoid-scenes.json" ]; then
  blender_run "$STATVENTURE_SOURCE/Tools/BlenderReference/render_scenes.py" -- "$OUT/Reference/humanoid-scenes.json" "$OUT/Golden" "$OUT/Actual/blender-glow" "$@"
  python3 "$STATVENTURE_SOURCE/Tools/BlenderReference/compose_glow.py" "$OUT/Reference/humanoid-scenes.json" "$OUT/Golden" "$OUT/Actual/blender-glow"
fi
blender_run "$STATVENTURE_SOURCE/Tools/BlenderReference/evaluate_animations.py" -- "$OUT/Reference/anim-scenes.json" "$OUT/Golden" "$@"
blender_run "$STATVENTURE_SOURCE/Tools/BlenderReference/evaluate_curves.py" -- "$OUT/Reference/curve-scenes.json" "$OUT/Golden" "$@"

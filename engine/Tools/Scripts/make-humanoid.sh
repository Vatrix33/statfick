#!/usr/bin/env bash
# The humanoid family's Blender scripts (GM2-P9; Tools/Models/humanoid/; Docs/Game/Hero.md, "How it works", step 3a), in
# Blender 5.2, headless (Tools/Scripts/blender.sh: $STATVENTURE_BLENDER, the pinned 5.2.0 by default; never the Flatpak's
# 5.3). On the PC only (no Blender in the cloud: the tests use what is committed).
#   make-humanoid.sh build [name ...]    builds the models manifest.json names (all by default) into their places
#   make-humanoid.sh check [name ...]    builds them into a scratch folder and proves each equals what is committed
#                                        (byte for byte); and that every look in look.py is valid and looks/today.json
#                                        is what look.py writes. Fails on any difference.
#   make-humanoid.sh clips <folder> <library.glb>
#                                        keys every clip file of <folder> (<name>.json, the form clips/library.py gives)
#                                        on the human man's skeleton and exports them as a clip library (to try clips
#                                        before they join the family's clips/ and the manifest's library)
#   make-humanoid.sh render --out <folder> --name <stem> [render.py's options]
#                                        renders a figure or a scene file in a look (render.py: --look, --model,
#                                        --garment, --item, --scene, --views, --size, --scale, --elevation, --focus,
#                                        --extent, --margin, --perspective, --clip, --time, --clips) into
#                                        <folder>/<stem>-<view>.png (the ink over the shading, as the engine draws
#                                        them), with the scene file and the separate layers in <folder>/layers/.
#   make-humanoid.sh look list | write <name or file> [<file>] | check
#                                        look.py's command line (a look's numbers as the file both sides read).
# Blender runs with 8 threads, always (GM2 step 4a, the manager's decision of 2026-11-16): QuadriFlow's remesh and the
# order of the edges Blender makes (which the weights' smoothing sums over) follow the thread count, so another count
# builds other meshes and weights; 8 is what the committed models were built with (the PC's default). The models are
# committed and rebuilt only on the PC with the pinned Blender: the cloud and Windows use what is committed.
set -euo pipefail
source "$(dirname "$0")/env.sh"
HUMANOID="$STATVENTURE_SOURCE/Tools/Models/humanoid"
BLENDER_THREADS=8
command="${1:-}"
[ -n "$command" ] && shift
case "$command" in
  look)
    exec python3 -I "$HUMANOID/look.py" "$@"
    ;;
  build)
    source "$STATVENTURE_SCRIPTS/blender.sh"
    blender_run "$HUMANOID/build.py" -- "$STATVENTURE_SOURCE" "$@"
    ;;
  check)
    python3 -I "$HUMANOID/look.py" check
    source "$STATVENTURE_SCRIPTS/blender.sh"
    scratch="$(mktemp -d "${TMPDIR:-/tmp}/statventure-humanoid-check.XXXXXX")"
    trap 'rm -rf "$scratch"' EXIT
    blender_run "$HUMANOID/build.py" -- "$STATVENTURE_SOURCE" --check "$scratch" "$@"
    ;;
  clips)
    [ $# -eq 2 ] || { echo "clips needs <folder> <library.glb>" >&2; exit 2; }
    folder="$(cd "$1" && pwd)"
    mkdir -p "$(dirname "$2")"
    library="$(cd "$(dirname "$2")" && pwd)/$(basename "$2")"
    source "$STATVENTURE_SCRIPTS/blender.sh"
    blender_run "$HUMANOID/build.py" -- "$STATVENTURE_SOURCE" --library "$folder" "$library"
    ;;
  render)
    out="" name=""
    pass=()
    while [ $# -gt 0 ]; do
      case "$1" in
        --out) out="$2"; shift 2 ;;
        --name) name="$2"; pass+=("$1" "$2"); shift 2 ;;
        *) pass+=("$1"); shift ;;
      esac
    done
    [ -n "$out" ] && [ -n "$name" ] || { echo "render needs --out <folder> and --name <stem>" >&2; exit 2; }
    mkdir -p "$out/layers"
    out="$(cd "$out" && pwd)"
    source "$STATVENTURE_SCRIPTS/blender.sh"
    blender_run "$HUMANOID/render.py" -- --work "$out/layers" "${pass[@]}"
    python3 "$STATVENTURE_SOURCE/Tools/BlenderReference/compose_glow.py" "$out/layers/$name-scenes.json" "$out/layers" "$out/layers/glow"
    python3 "$HUMANOID/compose.py" "$out/layers/$name-scenes.json" "$out/layers" "$out"
    ;;
  framing)
    # render.py's cameras for the frame's sample, which the sandbox's figure mode must frame alike (the engine's tests,
    # Sandbox/FigureFramingTests): Tests/Statventure.Tests/Reference/figure-framing*-scenes.json.
    source "$STATVENTURE_SCRIPTS/blender.sh"
    reference="$STATVENTURE_SOURCE/Tests/Statventure.Tests/Reference"
    sample="$STATVENTURE_SOURCE/Tests/Statventure.Tests/Data/Models/humanoid/sample.glb"
    blender_run "$HUMANOID/render.py" -- --work "$reference" --name figure-framing --model "$sample" --scenes-only \
      --views front,three-quarter,side,back,close-up,30 --size 300x400
    blender_run "$HUMANOID/render.py" -- --work "$reference" --name figure-framing-perspective --model "$sample" --scenes-only \
      --views close-up,front --size 320x240 --perspective 30 --focus upper --elevation 12 --margin 0.1
    # A picture so narrow its width frames the figure; a camera far enough to set its far plane.
    blender_run "$HUMANOID/render.py" -- --work "$reference" --name figure-framing-narrow --model "$sample" --scenes-only \
      --views front,side --size 100x800
    blender_run "$HUMANOID/render.py" -- --work "$reference" --name figure-framing-far --model "$sample" --scenes-only \
      --views three-quarter --size 320x240 --perspective 20 --extent 30
    ;;
  *)
    echo "usage: make-humanoid.sh build [name ...] | check [name ...] | clips <folder> <library.glb> | render --out <folder> --name <stem> [options] | look ... | framing" >&2
    exit 2
    ;;
esac

#!/usr/bin/env bash
# The humanoid family's Blender scripts (GM2-P9; Tools/Models/humanoid/; Docs/Game/Hero.md, "How it works", step 3a), in
# Blender 5.2, headless (Tools/Scripts/blender.sh: $STATVENTURE_BLENDER, the pinned 5.2.0 by default; never the Flatpak's
# 5.3). On the PC only (no Blender in the cloud: the tests use what is committed).
#   make-humanoid.sh build [name ...]    builds the models manifest.json names (all by default) into their places
#   make-humanoid.sh check [name ...]    builds them into a scratch folder and proves each equals what is committed
#                                        (byte for byte); and that every look in look.py is valid and looks/today.json
#                                        is what look.py writes, the emblems' chosen.json is what emblems/chosen.py
#                                        writes and their scenes (Reference/emblem-scenes.json) are coloured by
#                                        emblems/palette.py's rule (GM2 step 10); with no name also `keys --check` and
#                                        `measure --check` (the clips' keying kit). Fails on any difference.
#   make-humanoid.sh keys [--check | --out <folder>] [clip ...]
#                                        the clips' keying kit (GM2-P23; clips/kit/, Docs/Game/Hero.md, "How it works",
#                                        "The keying kit"): every clip's keying source written (clips/kit/sources.py) and
#                                        baked on the keying figure (clips/kit/run.py) into clips/<clip>.json, its hashes
#                                        recorded in clips/kit/keyed.json (all clips by default); --out bakes into a
#                                        folder instead (to try a change); --check bakes into scratch and proves every
#                                        source is the one keyed.json names and every clip the committed one, byte for byte
#   make-humanoid.sh measure [--record | --check | --series <clip> <L|R> <from> <to> | --parts <clip> <frame,...>] [clip ...]
#                                        each clip measured as the kit keys it (clips/kit/measure.py): its arms' worst
#                                        wrist bend and forearm turn, jumps, grips out of reach or in the trunk, every
#                                        frame against the ground, and whether it is within the family's bounds
#                                        (clips/kit/bounds.json); --record writes clips/kit/measures.json, --check proves
#                                        it is what is measured now; --series an arm key by key, --parts every body part's
#                                        lowest at those frames
#   make-humanoid.sh clip-video --out <folder> <clip> [--every N] [--hold <stance clip>] [--gear <list>] [--plain]
#                         [--views side,three-quarter] [--size WxH]
#                                        the clip on a floor (clips/kit/video.py): every Nth frame (2 by default) of the
#                                        man in the chain shirt with what its source (or --gear) puts in his hands
#                                        (--plain: his plain outfit), --hold laying a stance loop over it, rendered from
#                                        each view in the game's look as `render` renders (600x700 a view by default):
#                                        <folder>/<clip>.mp4, <clip>-strip.png and the frames
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
#   make-humanoid.sh assemble --out <folder> --name <stem> --look-hero <look.json> --data <folder> [--data <folder>]...
#                         [--models <folder>] [--background #rrggbb] [--garment models/<path>]... [--item models/<path>@<socket>]...
#                         [--gear-colour <slot>=#rrggbb]... [render.py's --views, --size, --scale, --elevation, --focus,
#                         --extent, --margin, --perspective, --clip, --time, --clips]
#                                        a hero's look (look.hero's JSON form) assembled from the game's data as the game
#                                        builds it (assemble.py, GM2 step 5: the data folders' records, a later folder's
#                                        over an earlier's; the models from Assets/Models by default; the data's
#                                        render/styles/game), rendered as `render` renders a figure: <folder>/<stem>-<view>.png
#   make-humanoid.sh look list | write <name or file> [<file>] | check
#                                        look.py's command line (a look's numbers as the file both sides read).
# Blender runs with 8 threads, always (GM2 step 4a, the manager's decision of 2026-11-16): QuadriFlow's remesh and the
# order of the edges Blender makes (which the weights' smoothing sums over) follow the thread count, so another count
# builds other meshes and weights; 8 is what the committed models were built with (the PC's default). The models are
# committed and rebuilt only on the PC with the pinned Blender: the cloud and Windows use what is committed.
set -euo pipefail
source "$(dirname "$0")/env.sh"
HUMANOID="$STATVENTURE_SOURCE/Tools/Models/humanoid"
KIT="$HUMANOID/clips/kit"
BLENDER_THREADS=8
command="${1:-}"
[ -n "$command" ] && shift

# keys_check / measure_run: the keying kit's runs (a scratch folder of their own, removed after).
keys_check() {
  local scratch
  scratch="$(mktemp -d "${TMPDIR:-/tmp}/statventure-keys.XXXXXX")"
  python3 -I "$KIT/sources.py" write "$scratch/sources" "$@"
  blender_run "$KIT/run.py" -- bake "$scratch/sources" "$scratch/baked" "$@"
  python3 -I "$KIT/sources.py" compare "$scratch/sources" "$scratch/baked" "$@" || { rm -rf "$scratch"; return 1; }
  rm -rf "$scratch"
}
measure_run() {   # <measure.py's command> <its arguments after the measures' file ...> -- [clip ...]
  local scratch what="$1" args=()
  shift
  while [ $# -gt 0 ] && [ "$1" != "--" ]; do args+=("$1"); shift; done
  [ $# -gt 0 ] && shift
  scratch="$(mktemp -d "${TMPDIR:-/tmp}/statventure-measure.XXXXXX")"
  python3 -I "$KIT/sources.py" write "$scratch/sources"
  blender_run "$KIT/run.py" -- measure "$scratch/sources" "$scratch/measured.json" "$@"
  python3 -I "$KIT/measure.py" "$what" "$scratch/measured.json" "${args[@]}" || { rm -rf "$scratch"; return 1; }
  rm -rf "$scratch"
}

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
    # The emblems' outlines as chosen.py writes them from the sketches, and their scenes coloured by palette.py's rule
    # (GM2 step 10: the game's tests write the scenes with the game's rule).
    python3 -I "$HUMANOID/emblems/chosen.py" check
    python3 -I "$HUMANOID/emblems/palette.py" check
    source "$STATVENTURE_SCRIPTS/blender.sh"
    scratch="$(mktemp -d "${TMPDIR:-/tmp}/statventure-humanoid-check.XXXXXX")"
    trap 'rm -rf "$scratch"' EXIT
    blender_run "$HUMANOID/build.py" -- "$STATVENTURE_SOURCE" --check "$scratch" "$@"
    if [ $# -eq 0 ]; then
      # the clips' keying kit: the clips baked again from their sources byte for byte, and measured as recorded
      keys_check
      measure_run check --
    fi
    ;;
  keys)
    mode=write out=""
    case "${1:-}" in
      --check) mode=check; shift ;;
      --out) [ $# -ge 2 ] || { echo "keys --out needs a folder" >&2; exit 2; }; mode=out; out="$2"; shift 2 ;;
    esac
    source "$STATVENTURE_SCRIPTS/blender.sh"
    if [ "$mode" = check ]; then
      keys_check "$@"
      exit 0
    fi
    scratch="$(mktemp -d "${TMPDIR:-/tmp}/statventure-keys.XXXXXX")"
    trap 'rm -rf "$scratch"' EXIT
    python3 -I "$KIT/sources.py" write "$scratch/sources" "$@"
    if [ "$mode" = out ]; then
      # a try: the sources and the clips baked from them, the repo's clips and keyed.json left alone
      mkdir -p "$out"
      out="$(cd "$out" && pwd)"
      cp -r "$scratch/sources" "$out/"
      blender_run "$KIT/run.py" -- bake "$scratch/sources" "$out" "$@"
    else
      blender_run "$KIT/run.py" -- bake "$scratch/sources" "$HUMANOID/clips" "$@"
      python3 -I "$KIT/sources.py" record "$scratch/sources" "$HUMANOID/clips" "$@"
    fi
    ;;
  measure)
    source "$STATVENTURE_SCRIPTS/blender.sh"
    case "${1:-}" in
      --record) measure_run record -- ;;
      --check) measure_run check -- ;;
      --series) [ $# -eq 5 ] || { echo "measure --series needs <clip> <L|R> <from> <to>" >&2; exit 2; }
        measure_run series "$2" "$3" "$4" "$5" -- "$2" ;;
      --parts) [ $# -eq 3 ] || { echo "measure --parts needs <clip> <frame,...>" >&2; exit 2; }
        measure_run parts "$2" "$3" -- "$2" ;;
      *) measure_run report "$@" -- "$@" ;;
    esac
    ;;
  clip-video)
    out="" clip="" views="side,three-quarter" size="600x700"
    pass=()
    while [ $# -gt 0 ]; do
      case "$1" in
        --out) out="$2"; shift 2 ;;
        --views) views="$2"; shift 2 ;;
        --size) size="$2"; shift 2 ;;
        --every|--hold|--gear) pass+=("$1" "$2"); shift 2 ;;
        --plain) pass+=("$1"); shift ;;
        -*) echo "clip-video: unknown option $1" >&2; exit 2 ;;
        *) clip="$1"; shift ;;
      esac
    done
    [ -n "$out" ] && [ -n "$clip" ] || { echo "clip-video needs --out <folder> and a clip" >&2; exit 2; }
    mkdir -p "$out"
    out="$(cd "$out" && pwd)"
    source "$STATVENTURE_SCRIPTS/blender.sh"
    work="$(mktemp -d "${TMPDIR:-/tmp}/statventure-clip-video.XXXXXX")"
    trap 'rm -rf "$work"' EXIT
    python3 -I "$KIT/sources.py" write "$work/sources"
    blender_run "$KIT/run.py" -- frames "$work/sources" "$work/frames" "$clip" "${pass[@]}"
    python3 -I "$KIT/video.py" scenes "$work/frames" "$work/frames/scenes.json" "$views" "$size"
    mkdir -p "$work/layers"
    blender_run "$HUMANOID/render.py" -- --work "$work/layers" --name "$clip" --scene "$work/frames/scenes.json" --look ember-hearth
    python3 "$STATVENTURE_SOURCE/Tools/BlenderReference/compose_glow.py" "$work/layers/$clip-scenes.json" "$work/layers" "$work/layers/glow"
    python3 "$HUMANOID/compose.py" "$work/layers/$clip-scenes.json" "$work/layers" "$work/renders" >/dev/null
    python3 -I "$KIT/video.py" tile "$work/frames" "$work/renders" "$out" "$views"
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
  assemble)
    out="" name=""
    pass=()
    while [ $# -gt 0 ]; do
      case "$1" in
        --out) out="$2"; shift 2 ;;
        --name) name="$2"; pass+=("$1" "$2"); shift 2 ;;
        *) pass+=("$1"); shift ;;
      esac
    done
    [ -n "$out" ] && [ -n "$name" ] || { echo "assemble needs --out <folder> and --name <stem>" >&2; exit 2; }
    mkdir -p "$out/layers"
    out="$(cd "$out" && pwd)"
    source "$STATVENTURE_SCRIPTS/blender.sh"
    blender_run "$HUMANOID/assemble.py" -- --work "$out/layers" "${pass[@]}"
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
    echo "usage: make-humanoid.sh build [name ...] | check [name ...] | clips <folder> <library.glb> | render --out <folder> --name <stem> [options] | assemble --out <folder> --name <stem> --look-hero <file> --data <folder> [options] | keys [--check | --out <folder>] [clip ...] | measure [--record | --check | --series ... | --parts ...] [clip ...] | clip-video --out <folder> <clip> [options] | look ... | framing" >&2
    exit 2
    ;;
esac

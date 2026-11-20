#!/usr/bin/env bash
# Plays a figure in a look in the sandbox's figure mode (GM2-P14; Docs/Game/Hero.md, "How it works", step 3a): the
# engine's own view of the figure playing its clips in turn, the playing view an approval shows.
#   Tools/Scripts/play-figure.sh <body.glb> [--clips <glb>] [--play <clip>[+<clip>][:times],...] [--look <name or file>]
#       [--garment <glb>]... [--item <glb>@<socket>]... [--view front|three-quarter|side|back|close-up|<degrees>]
#       [--size WxH] [--elevation degrees] [--margin fraction] [--extent metres] [--focus <bone>] [--perspective degrees]
#       [--dump <folder> [--fps N] [--times t,...] [--scene-file <file>] [--label]]
# In a window of --size (600x800 by default; keys: Space play/pause, Right/Left a frame, Down/Up the next/previous
# clip, A/D turn 15°, 1-5 the views, F11 full screen, Escape quits), no sound. --play (GM2 step 4b) plays the named
# clips in turn instead of every clip of the library once: "idle:2,swing:3" plays the idle twice then the swing three
# times; "idle+hold-bow" lays the second clip over the first (a layer above that replaces only what it keys, as the
# stance loops of the arms over the idle). --label names the clip on a dump's frames. --dump writes the frames instead
# (frame-NNNN.png at --size and frames.txt; one cycle of every clip at --fps, 30 by default, or the --times given) and,
# when ffmpeg is installed, the video <folder>/playing.mp4 from them (H.264); on a machine with cage (the PC) a dump
# runs inside a headless cage of its own with an output of --size (no window on the user's screen) and a scratch
# per-user folder. A look by name is Tools/Models/humanoid/looks/<name>.json (today's by default). A Release build.
set -euo pipefail
source "$(dirname "$0")/env.sh"
SELF="$STATVENTURE_SCRIPTS/play-figure.sh"
original=("$@")
body="${1:?which body .glb}"
shift
body="$(cd "$(dirname "$body")" && pwd)/$(basename "$body")"
args=(--figure "$body")
dump="" fps=30 size=600x800
while [ $# -gt 0 ]; do
  case "$1" in
    --dump) dump="$2"; mkdir -p "$dump"; dump="$(cd "$dump" && pwd)"; args+=(--dump "$dump"); shift 2 ;;
    --fps) fps="$2"; args+=(--fps "$2"); shift 2 ;;
    --size) size="$2"; args+=(--size "$2"); shift 2 ;;
    --look | --clips | --garment | --scene-file)
      value="$2"
      # Files by absolute path (the sandbox runs from its own folder); a look's name as it is.
      if [ -e "$value" ] || [ "$1" = --scene-file ]; then value="$(cd "$(dirname "$value")" && pwd)/$(basename "$value")"; fi
      args+=("$1" "$value"); shift 2 ;;
    --item)
      file="${2%@*}" socket="${2##*@}"
      args+=(--item "$(cd "$(dirname "$file")" && pwd)/$(basename "$file")@$socket"); shift 2 ;;
    *) args+=("$1"); shift ;;
  esac
done

# A dump inside a headless cage of its own (wlroots' headless backend, the user's displays unset), its output the
# picture's size, with a scratch per-user folder; ended when the run ends. The window on the cage's Xwayland (X11): cage
# gives it the whole output, where a Wayland window there draws its own title bar inside it (GLFW's libdecor, as cage
# offers no server-side decorations), 37 pixels less picture.
if [ -n "$dump" ] && [ -z "${STATVENTURE_FIGURE_CAGED:-}" ] && command -v cage >/dev/null 2>&1; then
  code_file="$(mktemp)" log_file="$(mktemp)" user_folder="$(mktemp -d)"
  trap 'rm -rf "$code_file" "$log_file" "$user_folder"' EXIT
  (
    unset DISPLAY WAYLAND_DISPLAY
    export WLR_BACKENDS=headless WLR_LIBINPUT_NO_DEVICES=1 XDG_SESSION_TYPE=wayland STATVENTURE_FIGURE_CAGED=1
    export XDG_DATA_HOME="$user_folder" ALSOFT_DRIVERS=null FIGURE_CODE_FILE="$code_file" FIGURE_LOG_FILE="$log_file" FIGURE_SIZE="$size"
    cage -- bash -c 'wlr-randr --output HEADLESS-1 --custom-mode "$FIGURE_SIZE" >/dev/null 2>&1 || true; sleep 1
      WAYLAND_DISPLAY=no-wayland XDG_SESSION_TYPE=x11 "$@" > "$FIGURE_LOG_FILE" 2>&1; echo $? > "$FIGURE_CODE_FILE"; kill -TERM $PPID 2>/dev/null' bash "$SELF" "${original[@]}"
  ) > /dev/null 2>&1 || true
  grep -v -e "^Errors from xkbcomp" -e "radv is not a conformant" -e "xcb error" "$log_file" || true
  exit "$(cat "$code_file" 2>/dev/null || echo 1)"
fi

cd "$STATVENTURE_SOURCE"
# The engine's shaders first (build.sh's two lines: a clone or a worktree has none compiled until then).
if ! output="$(dotnet build Tools/ShaderCompiler -c Release -v quiet -nologo 2>&1 \
  && dotnet Tools/ShaderCompiler/bin/Release/net10.0/ShaderCompiler.dll Engine/Statventure/Shaders/Source Engine/Statventure/Shaders/Bin 2>&1 \
  && dotnet build Samples/Statventure.Sandbox -c Release -v quiet -nologo 2>&1)"; then
  echo "$output" >&2
  exit 1
fi
dotnet Samples/Statventure.Sandbox/bin/Release/net10.0/Statventure.Sandbox.dll "${args[@]}"
if [ -n "$dump" ]; then
  if command -v ffmpeg >/dev/null; then
    ffmpeg -loglevel error -y -framerate "$fps" -i "$dump/frame-%04d.png" -vf "pad=ceil(iw/2)*2:ceil(ih/2)*2" \
      -c:v libx264 -pix_fmt yuv420p -crf 16 "$dump/playing.mp4"
    echo "wrote $dump/playing.mp4"
  else
    echo "no ffmpeg here: the frames are in $dump (frame-NNNN.png at $fps fps)"
  fi
fi

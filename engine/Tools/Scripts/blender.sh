# Sourced (after env.sh) by the scripts that run Blender: make-blender-references.sh and make-humanoid.sh. Blender comes
# from $STATVENTURE_BLENDER (env.sh takes the pinned 5.2.0 in the build cache when it is there, GM2-P10), else `blender`
# on the PATH, else the Flatpak org.blender.Blender. It must be Blender 5.2 (every reference and ported rule is 5.2's;
# the PC's Flatpak is 5.3 since 2026-11-13): another version is refused unless STATVENTURE_BLENDER_ANY_VERSION=1 (for
# measuring a move only). Always headless (-b, and no display handed to it), always --factory-startup.
#   blender_run <absolute script.py> [-- arguments]   (a Python error fails the run; shows what the script wrote and errors)
# A caller that sets BLENDER_THREADS runs Blender with that many threads (-t; make-humanoid.sh pins it: the humanoid
# family's remeshing follows the thread count).
if [ -n "${STATVENTURE_BLENDER:-}" ]; then
  BLENDER=("$STATVENTURE_BLENDER")
elif command -v blender >/dev/null; then
  BLENDER=(blender)
else
  BLENDER=(flatpak run org.blender.Blender)
fi
BLENDER_VERSION=$(env -u DISPLAY -u WAYLAND_DISPLAY "${BLENDER[@]}" -b --factory-startup --version 2>/dev/null | head -n 1)
if [[ "$BLENDER_VERSION" != "Blender 5.2."* ]] && [ "${STATVENTURE_BLENDER_ANY_VERSION:-}" != 1 ]; then
  echo "Blender 5.2 is needed (found: ${BLENDER_VERSION:-none} from ${BLENDER[*]}); set STATVENTURE_BLENDER (GM2-P10)" >&2
  exit 1
fi
echo "using $BLENDER_VERSION (${BLENDER[*]})"
# Absolute paths: the Flatpak starts in its own folder and has a private /tmp.
blender_run() {
  local log
  if ! log=$(env -u DISPLAY -u WAYLAND_DISPLAY "${BLENDER[@]}" -b --factory-startup ${BLENDER_THREADS:+-t "$BLENDER_THREADS"} --python-exit-code 1 --python "$@" 2>&1); then
    echo "$log" >&2
    exit 1
  fi
  echo "$log" | grep -E "^(wrote|same|checked)|Error" || true
}

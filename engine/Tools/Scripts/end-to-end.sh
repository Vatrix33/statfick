#!/usr/bin/env bash
# The packages end to end, as a player and a modder use them (M12 step 8; Docs/Packaging.md, "How it works", step 8):
#   Tools/Scripts/end-to-end.sh [--publish] --version <v> [--work <folder>]
#   Tools/Scripts/end-to-end.sh [--work <folder>] <stat-fick-<v>-linux.tar.gz> <statventure-editor-<v>-linux.tar.gz>
# With --version, the four archives of that version in release/: made first with --publish (publish.sh, which checks
# them), else checked first with check-packages.sh (the Windows two checked, the Linux two run); with two archives
# named, those (the caller's check). Then the two Linux archives unpacked and their programs started headless (cage on
# wlroots' headless backend at 2400 x 2400, else Xvfb; the user's displays unset), on a scratch per-user folder,
# OpenAL Soft on its null device: the stand-in's splash, page, credits, crashes (the crash file and the crash window),
# the editor's first start ("Where is Stat Fick?"), --game, the remembered folder, a game of another version refused,
# a mod made by the editor against the unpacked game (its in-process flow, GameFolderModTests, built from this tree with
# the packages' version) and mounted by the packaged stand-in, both programs' logs (one per run, the version and commit
# first; after eleven runs ten kept). One line per expectation, "ok" or "FAIL"; it fails if any fails. Its pictures and
# logs stay in <work>/run/ (by default $STATVENTURE_CACHE/end-to-end/; <work>/build and <work>/other-build keep its two
# builds for the next run). Linux only, never the player's real folders (compared before and after). Not part of
# test.sh, and release.sh doesn't run it (Q27: the release runs no tests); run it before a release. Needs python3 with
# Pillow, and cage with wlr-randr (or xvfb-run).
set -euo pipefail
source "$(dirname "$0")/env.sh"
usage() {
  echo "usage: end-to-end.sh [--publish] --version <v> [--work <folder>] | end-to-end.sh [--work <folder>] <stat-fick linux archive> <statventure-editor linux archive>" >&2
  exit 2
}
publish=0
version=""
work="$STATVENTURE_CACHE/end-to-end"
archives=()
while [ $# -gt 0 ]; do
  case "$1" in
    --publish) publish=1; shift ;;
    --version) version="${2:-}"; [ -n "$version" ] || usage; shift 2 ;;
    --work) work="${2:-}"; [ -n "$work" ] || usage; shift 2 ;;
    -*) usage ;;
    *) archives+=("$(cd "$(dirname "$1")" && pwd -P)/$(basename "$1")"); shift ;;
  esac
done
if [ -n "$version" ]; then
  [ ${#archives[@]} -eq 0 ] || usage
  if [ "$publish" = 1 ]; then
    "$STATVENTURE_SCRIPTS/publish.sh" --version "$version"
  else
    "$STATVENTURE_SCRIPTS/check-packages.sh" --version "$version"
  fi
  archives=("$STATVENTURE_REPO/release/stat-fick-$version-linux.tar.gz" "$STATVENTURE_REPO/release/statventure-editor-$version-linux.tar.gz")
else
  [ "$publish" = 0 ] && [ ${#archives[@]} -eq 2 ] || usage
fi
cd "$STATVENTURE_SOURCE"
# The shaders the engine embeds, for the in-process flow's build (as build.sh and publish.sh compile them).
dotnet build Tools/ShaderCompiler -c Release -v quiet -nologo >/dev/null
dotnet Tools/ShaderCompiler/bin/Release/net10.0/ShaderCompiler.dll Engine/Statventure/Shaders/Source Engine/Statventure/Shaders/Bin >/dev/null
mkdir -p "$work"
exec python3 -I "$STATVENTURE_SOURCE/Tools/Packaging/end_to_end.py" --work "$(cd "$work" && pwd -P)" "${archives[@]}"

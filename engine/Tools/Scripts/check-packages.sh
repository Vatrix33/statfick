#!/usr/bin/env bash
# Checks the packages players download (M12, Q28; Docs/Packaging.md, "How it works", step 3), from their archives:
#   Tools/Scripts/check-packages.sh [--no-run] <archive>...      each archive against the rules below
#   Tools/Scripts/check-packages.sh [--no-run] --version <v>     the four archives of that version in release/
#   Tools/Scripts/check-packages.sh --self-test <archive>...     breaks each archive one rule at a time: each must fail
#   Tools/Scripts/check-packages.sh --listing <archive>          prints the archive's listing (for Tools/Packaging/listings/)
# publish.sh runs it on what it makes; the release script runs it before a release. Not part of test.sh (Q28).
# The rules: the archive's name (Q1, Q4) and its one folder <program>-<version>/; every path in it, file or folder, and
# the executable bit, as Tools/Packaging/listings/ lists them (nothing missing, nothing extra); tar entries owned by 0;
# the game's packs/ the committed packs byte for byte; NOTICE.txt and licenses/ Tools/Packaging's texts; on Windows
# the Visual C++ runtime's DLLs vc-runtime.txt pins (Q9); the program ELF x86-64, or PE32+ x86-64 with the GUI
# subsystem (Q6), single-file (Q5) with the files inside it the listing names, its own assemblies' symbols embedded
# with paths from the repo's root (Q7); every native library's needs met in the folder (through $ORIGIN on Linux) or
# by the system, glibc 2.34 and GCC 12's libstdc++ at most; every file covered by licenses.txt with its texts there.
# The Linux packages are also run (not with --no-run): unpacked, started headless (cage, else xvfb-run; the user's
# displays unset) on a scratch per-user folder, OpenAL Soft on its null device: the game draws and quits with
# --capture, the editor given a wrong argument prints its usage and ends with 2; the player's real folders unchanged.
# Each archive's size is reported. Needs python3 and objdump (binutils).
set -euo pipefail
source "$(dirname "$0")/env.sh"
arguments=()
while [ $# -gt 0 ]; do
  case "$1" in
    --version)
      version="${2:?--version needs a version}"
      shift 2
      for package in stat-fick-"$version"-linux.tar.gz stat-fick-"$version"-windows.zip \
                     statventure-editor-"$version"-linux.tar.gz statventure-editor-"$version"-windows.zip; do
        arguments+=("$STATVENTURE_REPO/release/$package")
      done
      ;;
    *) arguments+=("$1"); shift ;;
  esac
done
mkdir -p "$STATVENTURE_CACHE"
CHECK_PACKAGES_WORK="$STATVENTURE_CACHE" exec python3 -I "$STATVENTURE_SOURCE/Tools/Packaging/check_packages.py" "${arguments[@]}"

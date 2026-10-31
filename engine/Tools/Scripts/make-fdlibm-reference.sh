#!/usr/bin/env bash
# Builds Blink's fdlibm (Chromium 152's third_party/fdlibm/ieee754.cc, from the reference sources
# in the cache) with Tools/FdlibmReference's stand-in headers and writes its exp and pow results to
# Tests/Statventure.Tests/Reference/fdlibm.json (Docs/Audio.md). Needs g++.
set -euo pipefail
source "$(dirname "$0")/env.sh"
SOURCE="$STATVENTURE_CACHE/reference-src/chromium-152-source/webaudio/fdlibm_ieee754.cc"
OUT="$STATVENTURE_SOURCE/Tests/Statventure.Tests/Reference/fdlibm.json"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
cp "$SOURCE" "$WORK/ieee754.cc"
# No contraction into fused multiply-adds: Chromium's x86-64 build has none (no -mfma).
g++ -O2 -std=c++20 -ffp-contract=off -w -I"$STATVENTURE_SOURCE/Tools/FdlibmReference/shim" \
  "$WORK/ieee754.cc" "$STATVENTURE_SOURCE/Tools/FdlibmReference/reference.cc" -o "$WORK/reference"
"$WORK/reference" > "$OUT"
echo "wrote $OUT"

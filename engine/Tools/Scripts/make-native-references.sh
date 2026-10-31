#!/usr/bin/env bash
# Native references for the audio port (Docs/Audio.md), built with g++/gcc:
#  - Blink's fdlibm (Chromium 152's third_party/fdlibm/ieee754.cc, from the reference sources in the
#    cache) with Tools/NativeReference's stand-in headers: exp and pow results, to
#    Tests/Statventure.Tests/Reference/fdlibm.json;
#  - this system's glibc exp2f and log2f (what Chromium here calls): result hashes, to
#    Tests/Statventure.Tests/Reference/libm.json (about a minute).
set -euo pipefail
source "$(dirname "$0")/env.sh"
SOURCE="$STATVENTURE_CACHE/reference-src/chromium-152-source/webaudio/fdlibm_ieee754.cc"
TOOL="$STATVENTURE_SOURCE/Tools/NativeReference"
OUT="$STATVENTURE_SOURCE/Tests/Statventure.Tests/Reference"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
cp "$SOURCE" "$WORK/ieee754.cc"
# No contraction into fused multiply-adds: Chromium's x86-64 build has none (no -mfma).
g++ -O2 -std=c++20 -ffp-contract=off -w -I"$TOOL/shim" "$WORK/ieee754.cc" "$TOOL/fdlibm.cc" -o "$WORK/fdlibm"
"$WORK/fdlibm" > "$OUT/fdlibm.json"
echo "wrote $OUT/fdlibm.json"
gcc -O2 -ffp-contract=off -DGLIBC_VERSION_STRING="\"$(ldd --version | head -1 | awk '{print $NF}')\"" "$TOOL/libm.c" -o "$WORK/libm" -lm
"$WORK/libm" > "$OUT/libm.json"
echo "wrote $OUT/libm.json"

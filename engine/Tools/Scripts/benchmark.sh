#!/usr/bin/env bash
# Times the UI engine on the game's captured screens (Release build).
#   Tools/Scripts/benchmark.sh [state ...]    e.g. benchmark.sh sheet combat shop
set -euo pipefail
source "$(dirname "$0")/env.sh"
cd "$STATVENTURE_SOURCE"
dotnet build Tools/UiBenchmark -c Release -v quiet -nologo -clp:ErrorsOnly
dotnet Tools/UiBenchmark/bin/Release/net10.0/UiBenchmark.dll "$@"

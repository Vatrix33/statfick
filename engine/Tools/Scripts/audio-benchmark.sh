#!/usr/bin/env bash
# Times the audio engine live (Release build): every theme played as the game plays it at 48 kHz,
# each render quantum timed against the real-time budget (Docs/Audio.md, step 7).
#   Tools/Scripts/audio-benchmark.sh [theme ...]
set -euo pipefail
source "$(dirname "$0")/env.sh"
cd "$STATVENTURE_SOURCE"
dotnet build Tools/AudioBenchmark -c Release -v quiet -nologo -clp:ErrorsOnly
dotnet Tools/AudioBenchmark/bin/Release/net10.0/AudioBenchmark.dll "$@"

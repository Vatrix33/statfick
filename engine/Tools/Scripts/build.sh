#!/usr/bin/env bash
# Builds Statventure: compiles the engine shaders, then the solution.
#   Tools/Scripts/build.sh [Debug|Release]
set -euo pipefail
source "$(dirname "$0")/env.sh"
CONFIG="${1:-Debug}"
cd "$STATVENTURE_SOURCE"
dotnet build Tools/ShaderCompiler -c Release -v quiet -nologo >/dev/null
dotnet Tools/ShaderCompiler/bin/Release/net10.0/ShaderCompiler.dll Engine/Statventure/Shaders/Source Engine/Statventure/Shaders/Bin
dotnet build Statventure.slnx -c "$CONFIG" -v quiet -nologo

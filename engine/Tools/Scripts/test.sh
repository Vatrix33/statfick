#!/usr/bin/env bash
# Builds and runs the engine tests (headless GPU tests included). Extra arguments go to dotnet test,
# e.g. --filter "FullyQualifiedName~Canvas".
set -euo pipefail
source "$(dirname "$0")/env.sh"
"$(dirname "$0")/build.sh" Debug
cd "$STATVENTURE_SOURCE"
dotnet test Tests/Statventure.Tests --no-build -v quiet -nologo "$@"

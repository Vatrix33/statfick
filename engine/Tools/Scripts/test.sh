#!/usr/bin/env bash
# Builds and runs the tests (headless GPU tests included): the engine's and the editor's (Tests/Statventure.Tests), then
# the game's (Tests/Statventure.Game.Tests, since GM1 step 2: GM1-P4), one after the other; it fails if either does. The
# suite's count is the two projects' sum. Extra arguments go to both dotnet test runs, e.g.
# --filter "FullyQualifiedName~Canvas" (a project with no test the filter names runs none and passes).
set -euo pipefail
source "$(dirname "$0")/env.sh"
"$(dirname "$0")/build.sh" Debug
cd "$STATVENTURE_SOURCE"
status=0
dotnet test Tests/Statventure.Tests --no-build -v quiet -nologo "$@" || status=$?
dotnet test Tests/Statventure.Game.Tests --no-build -v quiet -nologo "$@" || status=$?
exit "$status"

#!/usr/bin/env bash
# The opt-in full-theme check (Docs/Audio.md, step 6): builds Release and runs ThemeRenderTests
# (STATVENTURE_THEMES=1) against the Chromium renders Tools/Scripts/make-theme-references.sh keeps in
# the build cache: every theme's offline render in four shuffled summing orders, about 25 minutes.
set -euo pipefail
source "$(dirname "$0")/env.sh"
cd "$STATVENTURE_SOURCE"
dotnet build Tests/Statventure.Tests -c Release -v quiet -nologo -clp:ErrorsOnly
STATVENTURE_THEMES=1 dotnet test Tests/Statventure.Tests -c Release --no-build -v quiet -nologo --filter "FullyQualifiedName~ThemeRenderTests" "$@"

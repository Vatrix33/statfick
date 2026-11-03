#!/usr/bin/env bash
# Captures what Electron 44.5.0 (Chromium 152, the old game's; see chromium.sh) gives as
# app.getPath("documents") on Linux for each case of Tools/DocumentsReference (the user-dirs file,
# HOME, XDG_CONFIG_HOME), the old game's default folder for character files, and beside it what
# .NET's SpecialFolder.MyDocuments gives in the same environment (Tools/DocumentsReference/
# dotnet-documents.cs), into Tests/Statventure.Tests/Data/Storage/documents-reference.json.
# About a minute.
set -euo pipefail
source "$(dirname "$0")/env.sh"
OUT="${DOCUMENTS_OUT:-$STATVENTURE_SOURCE/Tests/Statventure.Tests/Data/Storage/documents-reference.json}"
PROBE="$STATVENTURE_CACHE/documents-reference-dotnet"
dotnet build "$STATVENTURE_SOURCE/Tools/DocumentsReference/dotnet-documents.cs" -o "$PROBE" -v q -nologo >&2
mkdir -p "$(dirname "$OUT")"
timeout 600 "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_SOURCE/Tools/DocumentsReference" \
  --out="$OUT" --dotnet="$PROBE/dotnet-documents" 2> >(grep -v "radv is not a conformant" >&2)
echo "wrote $OUT"

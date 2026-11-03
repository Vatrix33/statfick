#!/usr/bin/env bash
# git's text conversion for packs (Docs/Assets.md, "Reviewing packs"; Q14): with it, `git diff`,
# `git log -p` and `git show` show a pack as its dump. Each clone enables it once, from the repo's root:
#   git config diff.pak.textconv engine/Tools/Packer/textconv.sh
# (../../../.gitattributes names the driver for *.pak.) git gives the pack's path; the dump goes to
# standard output. The packer is built first (Release; a no-op when it is current).
set -euo pipefail
pack="$(realpath "$1")"
source "$(dirname "$0")/../Scripts/env.sh"
dotnet build "$STATVENTURE_SOURCE/Tools/Packer" -c Release -v quiet -nologo >/dev/null
exec dotnet "$STATVENTURE_SOURCE/Tools/Packer/bin/Release/net10.0/Packer.dll" dump "$pack"

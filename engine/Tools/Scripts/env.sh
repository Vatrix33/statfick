# Source this before building: paths come from where the repo is; the .NET SDK and the caches live
# outside it (~/.cache/stat-fick-build, or $STATVENTURE_CACHE). A git-ignored env.local.sh next to
# this file can override anything for one machine.
STATVENTURE_SCRIPTS="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export STATVENTURE_SCRIPTS
export STATVENTURE_SOURCE="$(cd "$STATVENTURE_SCRIPTS/../.." && pwd)"
export STATVENTURE_REPO="$(cd "$STATVENTURE_SOURCE/.." && pwd)"
export STATVENTURE_OLD_GAME="${STATVENTURE_OLD_GAME:-$STATVENTURE_REPO/old-game}"
export STATVENTURE_CACHE="${STATVENTURE_CACHE:-$HOME/.cache/stat-fick-build}"
# Where the runtime build goes (only what the game needs to run).
export STATVENTURE_GAME="${STATVENTURE_GAME:-$STATVENTURE_REPO/runtime}"
# A .NET SDK in the cache (dotnet-install.sh) wins over the system's.
if [ -x "$STATVENTURE_CACHE/dotnet/dotnet" ]; then
  export DOTNET_ROOT="$STATVENTURE_CACHE/dotnet"
  export PATH="$DOTNET_ROOT:$PATH"
fi
# Node for the Chromium harnesses: one in the cache if there is one, else the system's.
for node in "$STATVENTURE_CACHE"/node-v*-linux-x64/bin; do
  [ -x "$node/node" ] && export PATH="$node:$PATH" && break
done
export NUGET_PACKAGES="${NUGET_PACKAGES:-$STATVENTURE_CACHE/nuget}"
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-$STATVENTURE_CACHE/dotnet-home}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
if [ -f "$STATVENTURE_SCRIPTS/env.local.sh" ]; then
  # shellcheck disable=SC1091
  source "$STATVENTURE_SCRIPTS/env.local.sh"
fi

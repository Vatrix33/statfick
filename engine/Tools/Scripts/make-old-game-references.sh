#!/usr/bin/env bash
# The old game's oracle (GM2-P13; Docs/Game/Hero.md, "How it works", step 2): runs the old game's own code on each
# module's case list (Tools/OldGameOracle/modules/<module>.cjs) and writes what it gives into
# Tests/Statventure.Game.Tests/Reference/old-game/<module>.json, committed, so the port's tests compare with it where
# there is no Node or Chromium. The pure modules run in Node (the cache's, found by env.sh), the rules inside app.js in
# the old game's headless test client (Chromium 152, Tools/Scripts/chromium.sh old-game/test-client --script --call),
# the results brought out through its save hook. The old game is only read. On a machine with cage (the PC) it runs
# inside a headless cage of its own, so nothing shows on the screen; without it (the cloud) chromium.sh runs under Xvfb.
# The same old game and case lists give the same files, byte for byte.
#   Tools/Scripts/make-old-game-references.sh [module ...]     (default: every module)
set -euo pipefail
source "$(dirname "$0")/env.sh"
SELF="$STATVENTURE_SCRIPTS/make-old-game-references.sh"
ORACLE="$STATVENTURE_SOURCE/Tools/OldGameOracle"
OUT="$STATVENTURE_SOURCE/Tests/Statventure.Game.Tests/Reference/old-game"

if [ $# -eq 0 ]; then
  set -- $(cd "$ORACLE/modules" && ls *.cjs | sed 's/\.cjs$//')
fi
for module in "$@"; do
  [ -f "$ORACLE/modules/$module.cjs" ] || { echo "make-old-game-references.sh: no module $module in $ORACLE/modules" >&2; exit 2; }
done
command -v node >/dev/null || { echo "make-old-game-references.sh: no node (the cache's node-v*-linux-x64, or the system's)" >&2; exit 1; }
where() { node -e 'process.stdout.write(require(process.argv[1]).where)' "$ORACLE/modules/$1.cjs"; }

# The client modules run inside a headless cage of their own (wlroots' headless backend, the user's displays unset),
# ended when the run ends.
needs_client=""
for module in "$@"; do [ "$(where "$module")" = client ] && needs_client=1; done
if [ -n "$needs_client" ] && [ -z "${STATVENTURE_ORACLE_CAGED:-}" ] && command -v cage >/dev/null 2>&1; then
  code_file="$(mktemp)"
  log_file="$(mktemp)"
  trap 'rm -f "$code_file" "$log_file"' EXIT
  (
    unset DISPLAY WAYLAND_DISPLAY
    export WLR_BACKENDS=headless WLR_LIBINPUT_NO_DEVICES=1 XDG_SESSION_TYPE=wayland STATVENTURE_ORACLE_CAGED=1
    export ORACLE_CODE_FILE="$code_file" ORACLE_LOG_FILE="$log_file"
    cage -- bash -c '"$@" > "$ORACLE_LOG_FILE" 2>&1; echo $? > "$ORACLE_CODE_FILE"; kill -TERM $PPID 2>/dev/null' bash "$SELF" "$@"
  ) > /dev/null 2>&1 || true
  cat "$log_file"
  exit "$(cat "$code_file" 2>/dev/null || echo 1)"
fi

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
mkdir -p "$OUT"
for module in "$@"; do
  if [ "$(where "$module")" = node ]; then
    node "$ORACLE/oracle.cjs" node "$module" "$STATVENTURE_OLD_GAME" "$OUT/$module.json"
    continue
  fi
  # The test client makes its temporary profile (and the saved results) under TMPDIR: the run's own folder.
  mkdir -p "$WORK/$module/tmp"
  node "$ORACLE/oracle.cjs" client-script "$module" "$STATVENTURE_OLD_GAME" "$WORK/$module/script.js"
  if ! TMPDIR="$WORK/$module/tmp" "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_OLD_GAME/test-client" \
      --script="$WORK/$module/script.js" --call=oracleMain --report="$WORK/$module/report.json" --timeout=1800 \
      > "$WORK/$module/client.log" 2>&1; then
    echo "make-old-game-references.sh: the old game's test client failed on $module:" >&2
    node -e 'try { const r = require(process.argv[1]); console.error(r.error || r); } catch { }' "$WORK/$module/report.json" >&2
    grep -v -e "^Errors from xkbcomp" -e "xcb error" "$WORK/$module/client.log" | tail -20 >&2
    exit 1
  fi
  saved="$(node -e 'process.stdout.write(require(process.argv[1]).profile)' "$WORK/$module/report.json")/desktop-test-character.json"
  node "$ORACLE/oracle.cjs" client-result "$module" "$STATVENTURE_OLD_GAME" "$saved" "$OUT/$module.json"
done

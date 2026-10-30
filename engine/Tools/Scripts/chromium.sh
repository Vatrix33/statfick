#!/usr/bin/env bash
# Runs an Electron app folder in Chromium 152, the old game's browser: Electron 44.5.0 (pinned by
# old-game/desktop/package-lock.json), installed from npm into the cache the first time.
#   Tools/Scripts/chromium.sh <app folder> [arguments ...]
# The app folders are the harnesses in Tools/ (InputReference, PaintReference, GameReference,
# ScriptReference) and the old game's test client (old-game/test-client). Without a display (a cloud
# session) it runs under Xvfb.
set -euo pipefail
source "$(dirname "$0")/env.sh"
VERSION=44.5.0
DIR="$STATVENTURE_CACHE/electron-$VERSION"
ELECTRON="$DIR/node_modules/electron/dist/electron"
if [ ! -x "$ELECTRON" ]; then
  mkdir -p "$DIR"
  (cd "$DIR" && npm install --no-save --no-audit --no-fund --no-update-notifier --loglevel=error "electron@$VERSION" >&2)
  # npm may skip install scripts: Electron's own step downloads the binary (from GitHub releases).
  [ -x "$ELECTRON" ] || (cd "$DIR/node_modules/electron" && node install.js >&2)
fi
APP="$1"
shift
if [ -z "${DISPLAY:-}" ] && [ -z "${WAYLAND_DISPLAY:-}" ] && command -v xvfb-run >/dev/null; then
  exec xvfb-run -a -s "-screen 0 2560x1600x24" "$ELECTRON" "$APP" --no-sandbox "$@"
fi
exec "$ELECTRON" "$APP" --no-sandbox "$@"

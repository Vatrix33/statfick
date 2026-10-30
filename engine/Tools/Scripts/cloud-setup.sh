#!/usr/bin/env bash
# Setup script for the Claude Code cloud environment (claude.ai/code: the environment's
# "Setup script" field; paste this file or use "Choose file"). It runs as root on Ubuntu 24.04
# before a session starts, and what it installs is cached with the environment (it must finish in
# about five minutes). The default "Trusted" network access is enough.
#   - .NET 10 SDK (Ubuntu's own package)
#   - Vulkan on the CPU (Mesa's lavapipe: the cloud has no GPU; the tests pass on it)
#   - fontconfig, Xvfb and the libraries Electron needs (reference harnesses; Chromium runs under Xvfb)
#   - Pillow (the reference scripts crop screenshots with it)
#   - Electron 44.5.0 (Chromium 152) in the cache Tools/Scripts/chromium.sh uses
set -uo pipefail
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq
apt-get install -y -qq --no-install-recommends \
  dotnet-sdk-10.0 \
  mesa-vulkan-drivers libvulkan1 vulkan-tools \
  fontconfig xvfb xauth \
  libgtk-3-0t64 libnss3 libasound2t64 libgbm1 libxss1 libxtst6 libatk-bridge2.0-0t64 libcups2t64 \
  libdrm2 libxkbcommon0 libxcomposite1 libxdamage1 libxrandr2 libpango-1.0-0 libcairo2 \
  python3-pil >/dev/null || echo "setup: some packages failed to install"
ELECTRON_DIR="$HOME/.cache/stat-fick-build/electron-44.5.0"
mkdir -p "$ELECTRON_DIR"
(
  cd "$ELECTRON_DIR" &&
    npm install --no-save --no-audit --no-fund --no-update-notifier --loglevel=error electron@44.5.0 &&
    { [ -x node_modules/electron/dist/electron ] || (cd node_modules/electron && node install.js); }
) || echo "setup: Electron 44.5.0 wasn't installed (Tools/Scripts/chromium.sh installs it when needed)"
dotnet --version || true
exit 0

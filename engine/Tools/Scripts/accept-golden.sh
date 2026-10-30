#!/usr/bin/env bash
# Accepts reviewed test renders: copies Tests/Statventure.Tests/Actual/<name>.png over the reference
# images in Golden/. With names, only those; without, all of them.
set -euo pipefail
source "$(dirname "$0")/env.sh"
cd "$STATVENTURE_SOURCE/Tests/Statventure.Tests"
mkdir -p Golden
if [ $# -gt 0 ]; then
  for name in "$@"; do mv "Actual/$name.png" "Golden/$name.png"; echo "accepted $name"; done
else
  for file in Actual/*.png; do [ -e "$file" ] || continue; mv "$file" Golden/; echo "accepted $(basename "$file" .png)"; done
fi

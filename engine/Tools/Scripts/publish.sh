#!/usr/bin/env bash
# Makes the packages players download (M12, Docs/Packaging.md, "How it works", step 3) into release/ at the repo's
# root, then checks each with check-packages.sh:
#   Tools/Scripts/publish.sh [--version <v>] [--no-check] [<package> ...]
# The packages are stat-fick-linux, stat-fick-windows, statventure-editor-linux and statventure-editor-windows (all
# four unless named): stat-fick-<v>-linux.tar.gz and the like, each holding the folder <program>-<v>/ (Q4). The version
# is a release's, 2026.11.8.1 (Q1), or without --version "dev" (a build that isn't a release). Self-contained publishes
# for Linux and Windows, both made here (Q15): under a minute once the packages are cached; the work folder is
# release/.publish while it runs.
# The Windows packages need the Visual C++ runtime's DLLs (Q9), taken once from Microsoft's redistributable into the
# build cache ($STATVENTURE_CACHE/vc-runtime/<version>/; Tools/Packaging/vc-runtime.txt pins it): curl and cabextract.
# Nothing here reads or writes the player's per-user folders; build.sh's dev layout ($STATVENTURE_GAME) isn't touched.
set -euo pipefail
source "$(dirname "$0")/env.sh"
version=dev
check=1
packages=()
while [ $# -gt 0 ]; do
  case "$1" in
    --version) version="${2:?--version needs a version}"; shift 2 ;;
    --no-check) check=0; shift ;;
    -*) echo "usage: publish.sh [--version <v>] [--no-check] [<package> ...]" >&2; exit 2 ;;
    *) packages+=("$1"); shift ;;
  esac
done
[ ${#packages[@]} -gt 0 ] || packages=(stat-fick-linux stat-fick-windows statventure-editor-linux statventure-editor-windows)
cd "$STATVENTURE_SOURCE"

# The Visual C++ runtime's DLLs in the build cache (Q9), taken from the pinned redistributable when missing; prints
# their folder.
vc_runtime() {
  local list=Tools/Packaging/vc-runtime.txt runtime url sha folder ready=1 kind name cab hash work
  runtime="$(awk '$1 == "version" { print $2 }' "$list")"
  url="$(awk '$1 == "url" { print $2 }' "$list")"
  sha="$(awk '$1 == "sha256" { print $2 }' "$list")"
  folder="$STATVENTURE_CACHE/vc-runtime/$runtime"
  while read -r kind name cab hash; do
    [ "$kind" = file ] || continue
    [ -f "$folder/$name" ] && [ "$(sha256sum "$folder/$name" | cut -c1-64)" = "$hash" ] || ready=0
  done < "$list"
  if [ "$ready" = 0 ]; then
    if ! command -v cabextract >/dev/null; then
      echo "publish: the Windows packages need cabextract (the Visual C++ runtime comes from Microsoft's redistributable)" >&2
      return 1
    fi
    echo "publish: the Visual C++ runtime $runtime from $url" >&2
    mkdir -p "$STATVENTURE_CACHE"
    work="$(mktemp -d "$STATVENTURE_CACHE/vc-runtime.XXXXXX")"
    curl -fsSL -o "$work/vc_redist.x64.exe" "$url"
    if [ "$(sha256sum "$work/vc_redist.x64.exe" | cut -c1-64)" != "$sha" ]; then
      echo "publish: the redistributable downloaded isn't the one vc-runtime.txt pins" >&2
      rm -rf "$work"
      return 1
    fi
    # The redistributable is a bundle whose payloads are cabinets: the runtime's DLLs are in one of them, named
    # <dll>_amd64; whichever holds them, each is taken only if its SHA-256 is the pinned one.
    mkdir "$work/bundle" "$work/files"
    cabextract -q -d "$work/bundle" "$work/vc_redist.x64.exe" >/dev/null 2>&1 || true
    for payload in "$work"/bundle/*; do
      cabextract -q -d "$work/files" -F '*_amd64' "$payload" >/dev/null 2>&1 || true
    done
    mkdir -p "$folder"
    while read -r kind name cab hash; do
      [ "$kind" = file ] || continue
      if [ ! -f "$work/files/$cab" ] || [ "$(sha256sum "$work/files/$cab" | cut -c1-64)" != "$hash" ]; then
        echo "publish: the redistributable holds no $cab with the SHA-256 vc-runtime.txt pins" >&2
        rm -rf "$work"
        return 1
      fi
      cp "$work/files/$cab" "$folder/$name"
    done < "$list"
    rm -rf "$work"
  fi
  echo "$folder"
}

# The shaders the engine embeds, as build.sh compiles them (a clean tree has none).
dotnet build Tools/ShaderCompiler -c Release -v quiet -nologo >/dev/null
dotnet Tools/ShaderCompiler/bin/Release/net10.0/ShaderCompiler.dll Engine/Statventure/Shaders/Source Engine/Statventure/Shaders/Bin
runtime_folder=()
for package in "${packages[@]}"; do
  if [[ "$package" == *-windows ]]; then
    folder="$(vc_runtime)"
    runtime_folder=(--vc-runtime "$folder")
    break
  fi
done
# The archives are dated at the commit's time, so one commit gives the same archives.
dotnet run Tools/Packaging/Publish.cs -- --engine "$STATVENTURE_SOURCE" --version "$version" \
  --time "$(git log -1 --format=%ct)" "${runtime_folder[@]}" "${packages[@]}"
[ "$check" = 1 ] || exit 0
archives=()
for package in "${packages[@]}"; do
  case "$package" in
    *-linux) archives+=("$STATVENTURE_REPO/release/${package%-linux}-$version-linux.tar.gz") ;;
    *-windows) archives+=("$STATVENTURE_REPO/release/${package%-windows}-$version-windows.zip") ;;
  esac
done
"$STATVENTURE_SCRIPTS/check-packages.sh" "${archives[@]}"

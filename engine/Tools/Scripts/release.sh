#!/usr/bin/env bash
# Makes a release (M12-7; Docs/Packaging.md, "How it works", step 7): the four archives of the commit checked out,
# uploaded as a draft release on the private GitHub repo, which the user reads, may edit and publishes:
#   Tools/Scripts/release.sh [--dry-run]
#   Tools/Scripts/release.sh --self-test     its tests (Tools/Packaging/release_test.py: scratch repositories, stand-ins)
# 1. Refuses unless the work tree is clean (nothing changed, staged or not, no untracked file; git-ignored files such as
#    release/ don't count) and its commit is on GitHub's main (origin's main, fetched now); runs no tests (Q27).
# 2. The version (Q1, Q2): the commit's UTC date (its committer date, the archives' time too) and the release of that
#    date, 2026.11.8.1, then 2026.11.8.2: one more than the highest v<date>.<n> among GitHub's tags (git ls-remote) and
#    its releases' tags, drafts included (gh release list: a draft's tag is made only when it is published, so until
#    then the draft holds its number). Never zero-padded; local tags don't count.
# 3. publish.sh --version <v> --no-check for the four, then check-packages.sh --version <v> on them (Q28).
# 4. The release's text: Tools/Packaging/release-text.md with its placeholders filled ({{version}} and the archives'
#    names, {{stat-fick-linux}}, {{stat-fick-windows}}, {{statventure-editor-linux}}, {{statventure-editor-windows}}),
#    written to release/release-text-<v>.md; a placeholder left unfilled is refused.
# 5. gh release create v<v> --draft --target <commit> --title "Stat Fick <v>" --notes-file <the text> <the four>
#    (Q26), from the repo's root; gh prints the draft's address. Publishing the draft makes the tag at the commit.
# --dry-run does all of it but step 5's upload, and prints the command and the text instead. gh must be logged in
# either way (step 2 reads the releases); git asks for no password (it fails instead).
set -euo pipefail
source "$(dirname "$0")/env.sh"
usage() { echo "usage: release.sh [--dry-run] | release.sh --self-test" >&2; exit 2; }
refuse() { echo "release: $*" >&2; exit 1; }
dry_run=0
case "$*" in
  "") ;;
  --dry-run) dry_run=1 ;;
  --self-test) exec python3 -I "$STATVENTURE_SOURCE/Tools/Packaging/release_test.py" ;;
  *) usage ;;
esac
cd "$STATVENTURE_REPO"
command -v gh >/dev/null || refuse "needs gh, GitHub's command line, logged in to the repo"
# git talking to GitHub: never a prompt or a password window (a missing login fails).
remote_git() { GIT_TERMINAL_PROMPT=0 GIT_ASKPASS=/bin/false SSH_ASKPASS= git "$@"; }
# A command as it would be typed (arguments with spaces or quotes in single quotes).
quoted() {
  local words=() word
  for word in "$@"; do
    if [[ "$word" =~ ^[A-Za-z0-9._/=:@%+,-]+$ ]]; then words+=("$word"); else words+=("'${word//\'/\'\\\'\'}'"); fi
  done
  echo "${words[*]}"
}

# 1. A clean tree whose commit is on GitHub's main (Q27).
dirty="$(git status --porcelain)"
if [ -n "$dirty" ]; then
  head -n 20 <<< "$dirty" >&2
  refuse "the work tree isn't clean (above): commit or remove the changes first"
fi
commit="$(git rev-parse HEAD)"
remote_git fetch --quiet origin +refs/heads/main:refs/remotes/origin/main || refuse "couldn't fetch GitHub's main (origin)"
git merge-base --is-ancestor "$commit" refs/remotes/origin/main \
  || refuse "the commit $commit isn't on GitHub's main: push it to main first"

# 2. The version (Q1, Q2).
time="$(git log -1 --format=%ct "$commit")"
date="$(date -u -d "@$time" +%Y.%-m.%-d)"
tags="$(remote_git ls-remote --tags --refs origin)" || refuse "couldn't read GitHub's tags (origin)"
releases="$(gh release list --limit 1000 --json tagName --jq '.[].tagName')" \
  || refuse "couldn't read GitHub's releases (gh release list): is gh logged in?"
pattern="^v${date//./\\.}\\.([1-9][0-9]*)\$"
last=0
while read -r name; do
  name="${name##*refs/tags/}"
  if [[ "$name" =~ $pattern ]] && (( BASH_REMATCH[1] > last )); then last="${BASH_REMATCH[1]}"; fi
done <<< "$tags"$'\n'"$releases"
version="$date.$((last + 1))"
echo "release: $version, the commit $commit ($(date -u -d "@$time" '+%Y-%m-%d %H:%M:%S UTC')), on GitHub's main"

# 3. The four archives, made and checked (Q28).
"$STATVENTURE_SCRIPTS/publish.sh" --version "$version" --no-check || refuse "publish.sh failed"
"$STATVENTURE_SCRIPTS/check-packages.sh" --version "$version" || refuse "check-packages.sh failed"
packages=(stat-fick-linux stat-fick-windows statventure-editor-linux statventure-editor-windows)
archive_of() {
  case "$1" in
    *-linux) echo "${1%-linux}-$version-linux.tar.gz" ;;
    *-windows) echo "${1%-windows}-$version-windows.zip" ;;
  esac
}
archives=()
for package in "${packages[@]}"; do
  archive="release/$(archive_of "$package")"
  [ -f "$archive" ] || refuse "publish.sh made no $archive"
  archives+=("$archive")
done

# 4. The release's text (Q12, Q13): the template with the version and the archives' names.
text="$(cat "$STATVENTURE_SOURCE/Tools/Packaging/release-text.md")"
text="${text//"{{version}}"/$version}"
for package in "${packages[@]}"; do
  text="${text//"{{$package}}"/$(archive_of "$package")}"
done
if [[ "$text" =~ \{\{[^}]*\}\} ]]; then
  refuse "the release's text (Tools/Packaging/release-text.md) has a placeholder nothing fills: ${BASH_REMATCH[0]}"
fi
notes="release/release-text-$version.md"
printf '%s\n' "$text" > "$notes"

# 5. The draft (Q26).
upload=(gh release create "v$version" --draft --target "$commit" --title "Stat Fick $version" --notes-file "$notes"
        "${archives[@]}")
if [ "$dry_run" = 1 ]; then
  echo "release: dry run, so no draft; the upload would be (from $STATVENTURE_REPO):"
  echo "  $(quoted "${upload[@]}")"
  echo "release: the release's text ($notes):"
  cat "$notes"
  exit 0
fi
"${upload[@]}" || refuse "gh release create failed; a draft it left on GitHub holds v$version's number: delete it there before trying again"
echo "release: the draft v$version is on GitHub (above): read it, edit its text there if needed, then publish it;"
echo "release: publishing it makes the tag v$version at $commit"

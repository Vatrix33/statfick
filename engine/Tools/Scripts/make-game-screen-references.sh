#!/usr/bin/env bash
# Screenshots the game's screens (GM1-P17; Docs/Game/Foundation.md, "How it works", step 8) in Chromium 152 at device
# scales 1 and 1.65 as Tests/Statventure.Tests/Golden/game-screen-<fixture>@<scale>.png, with Tools/PaintReference (X11,
# a forced device scale factor, an invisible window, the game's five font faces), from the fixture list the game's tests
# write (Tests/Statventure.Tests/Reference/game-screen-fixtures.json: Tests/Statventure.Game.Tests/Ui/GameScreenFixtures.cs).
# Every fixture is captured as a page's own main frame (mainFrame), as the game's document is, never in PaintReference's
# iframe, where the top layer and the fixed dialogs would merge into the page's layer and keep LCD text
# (Docs/Compositing-Port.md, "The iframe and the main frame"); a fixture without it is refused.
# Run the game's tests once first. Names given: only those fixtures (a step captures its own screens and leaves the
# others' goldens alone). One Chromium run per fixture and scale (a run at 1.65 under a headless compositor stalled after
# its first fixture: GM1 step 1a). From a headless compositor, make its output large (2400 x 2400) first.
#   Tools/Scripts/make-game-screen-references.sh [fixture ...]
set -euo pipefail
source "$(dirname "$0")/env.sh"
TESTS="$STATVENTURE_SOURCE/Tests/Statventure.Tests"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
# A fixture names the document's sheets in their order ("ui/styles/game" ...): their committed sources
# (Assets/UI/styles/<name>.css, which ui.pak holds compiled) follow its own css, without their @font-face rules
# (PaintReference gives the game's five faces with gameFonts).
python3 - "$TESTS/Reference/game-screen-fixtures.json" "$OUT" "$STATVENTURE_SOURCE/Assets/UI/styles" "$@" <<'PY'
import json, os, re, sys
fixtures = json.load(open(sys.argv[1]))
styles = sys.argv[3]
wanted = sys.argv[4:]
missing = [name for name in wanted if name not in {f["name"] for f in fixtures}]
if missing:
    sys.exit("not in the fixture list: " + ", ".join(missing))
iframed = [f["name"] for f in fixtures if f.get("mainFrame") is not True]
if iframed:
    sys.exit("not main-frame fixtures (the game's document is a main frame): " + ", ".join(iframed))
for f in fixtures:
    if wanted and f["name"] not in wanted:
        continue
    sheets = [open(os.path.join(styles, s.removeprefix("ui/styles/") + ".css"), encoding="utf-8").read() for s in f.pop("sheets")]
    f["css"] = "\n".join([f["css"]] + [re.sub(r"@font-face\s*\{[^}]*\}", "", sheet) for sheet in sheets])
    # The markup at each scale: the scale's own where the engine placed the top layer's pieces otherwise (htmlByScale), or
    # chose another picture (the race emblems' pictures, GM2 step 10).
    by_scale = f.pop("htmlByScale", {})
    # An image the document reads from ui.pak (src="ui/emblems/human-124.png") is its committed file
    # (engine/Assets/UI/emblems/human-124.png), which ui.pak holds as it is, by its absolute file url.
    def files(html):
        def resolve(m):
            path = os.path.join(os.path.dirname(styles), m.group(1)[len("ui/"):])
            if not os.path.isfile(path):
                sys.exit(f"{f['name']}: the image {m.group(1)} has no file {path}")
            return 'src="file://' + os.path.abspath(path) + '"'
        return re.sub(r'src="(ui/[^"]+\.png)"', resolve, html)
    for scale in ("1", "1.65"):
        os.makedirs(os.path.join(sys.argv[2], scale), exist_ok=True)
        json.dump([dict(f, html=files(by_scale.get(scale, f["html"])))], open(os.path.join(sys.argv[2], scale, f["name"] + ".json"), "w"))
PY
for scale in 1 1.65; do
  for list in "$OUT/$scale"/*.json; do
    name="$(basename "$list" .json)"
    mkdir -p "$OUT/shots/$scale"
    timeout 300 "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_SOURCE/Tools/PaintReference" --ozone-platform=x11 \
      --fixtures="$list" --scale="$scale" --out="$OUT/shots/$scale" \
      --fonts="$STATVENTURE_OLD_GAME/objects/fonts" 2>&1 | grep -v "radv is not a conformant" || true
    shot="$OUT/shots/$scale/ui-$name@$scale.png"
    [ -f "$shot" ] || { echo "Chromium made no screenshot of $name at $scale" >&2; exit 1; }
    mv -- "$shot" "$TESTS/Golden/game-screen-$name@$scale.png"
    echo "wrote Golden/game-screen-$name@$scale.png"
  done
done

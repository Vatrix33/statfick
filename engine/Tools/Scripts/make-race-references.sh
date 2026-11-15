#!/usr/bin/env bash
# The race oracle (GM2 step 1; Docs/Game/Hero.md, "How it works", step 1): runs Tests/Statventure.Game.Tests/Reference/
# race-references.js with its case list (race-cases.json, put before it as RACE_CASES) in the old game's test client
# (Chromium 152 through chromium.sh, headless: offscreen, muted, a fresh profile in a scratch folder), and writes what it
# measured beside them: race-fold.json (the fold and the attributes' modifiers of every race of races.json and of the
# crafted races at the listed levels and enemy counts, in step 2's case form: { module, sources, cases }) and
# race-rules.json (normalizeRaceDefinition's cases, the catalogue's refusals, the accents, the normalized races). The
# sources' SHA-256 are written with them, so the tests see when the old game changed under them. On the PC run it inside
# a headless compositor (the test client's window is offscreen, but Electron still needs a display).
#   Tools/Scripts/make-race-references.sh
set -euo pipefail
source "$(dirname "$0")/env.sh"
here="$STATVENTURE_SOURCE/Tests/Statventure.Game.Tests/Reference"
work="$(mktemp -d "$STATVENTURE_CACHE/race-references.XXXXXX")"
trap 'rm -rf "$work"' EXIT
python3 - "$here/race-cases.json" "$here/race-references.js" "$work/script.js" <<'EOF'
import sys
cases, script, out = sys.argv[1:]
with open(out, "w", encoding="utf-8") as f:
    f.write("const RACE_CASES = " + open(cases, encoding="utf-8").read().strip() + ";\n")
    f.write(open(script, encoding="utf-8").read())
EOF
TMPDIR="$work" "$STATVENTURE_SCRIPTS/chromium.sh" "$STATVENTURE_OLD_GAME/test-client" --script="$work/script.js" \
  --call=raceReferences --report="$work/report.json" --timeout=900 >"$work/run.log" 2>&1 || { cat "$work/run.log" >&2; exit 1; }
python3 - "$work/report.json" "$STATVENTURE_OLD_GAME" "$here" <<'EOF'
import hashlib, json, os, sys
report, old_game, here = sys.argv[1:]
report = json.load(open(report, encoding="utf-8"))
if not report.get("passed"):
    sys.exit("the test client failed: " + report.get("error", "?"))
measure = json.load(open(os.path.join(report["profile"], "desktop-test-character.json"), encoding="utf-8"))["measure"]
sources = {path: hashlib.sha256(open(os.path.join(old_game, path), "rb").read()).hexdigest() for path in ("objects/races.json", "scripts/app.js")}
def line(value):
    return json.dumps(value, ensure_ascii=True, separators=(",", ":"))
with open(os.path.join(here, "race-fold.json"), "w", encoding="utf-8", newline="\n") as f:
    f.write('{"module":"app.js: calculateRaceEffectsUncached, raceStatModifier","sources":' + line(sources) + ',"cases":[\n')
    f.write(",\n".join(line(case) for case in measure["fold"]))
    f.write("\n]}\n")
rules = {"module": "app.js: normalizeRaceDefinition, applyRaceText, raceAccent", "sources": sources,
         "normalize": measure["normalize"], "catalogue": measure["catalogue"], "accents": measure["accents"],
         "races": measure["races"], "crafted": measure["crafted"]}
with open(os.path.join(here, "race-rules.json"), "w", encoding="utf-8", newline="\n") as f:
    json.dump(rules, f, ensure_ascii=True, indent=1)
    f.write("\n")
print(f"race-fold.json: {len(measure['fold'])} cases; race-rules.json: {len(measure['normalize'])} races normalized, "
      f"{len(measure['catalogue'])} catalogues, {len(measure['accents'])} accents")
EOF

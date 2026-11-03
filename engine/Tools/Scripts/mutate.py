#!/usr/bin/env python3
"""Checks that tests can fail: breaks one rule at a time and runs the tests that should notice.

    Tools/Scripts/mutate.py mutations.json

mutations.json is a list of {"label", "file", "old", "new", "filter"}: `file` relative to engine/, `old`
the exact text (it must occur once), `new` what replaces it, `filter` a `dotnet test --filter` for the tests
that cover the rule. Each mutation is applied, the tests are built and run, and the file is put back.

Prints one line each: "killed" (the tests failed, as they should), "SURVIVED" (no test noticed: either
explain why nothing can differ or close the gap), "BUILD FAILED" or "SKIP" (the mutation itself was
invalid, so it shows nothing). Writes the results next to the list. Keep the lists outside the repo, in
~/.cache/stat-fick-build/claude-work/ (a session's scratch folder is lost when the session restarts).
"""
import json
import os
import subprocess
import sys

ENGINE = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ENV = "source Tools/Scripts/env.sh && "


def shell(command):
    return subprocess.run(["bash", "-c", ENV + command], cwd=ENGINE, capture_output=True, text=True).stdout


def main(list_path):
    results = []
    for mutation in json.load(open(list_path)):
        path = os.path.join(ENGINE, mutation["file"])
        original = open(path).read()
        count = original.count(mutation["old"])
        if count != 1:
            outcome = f"SKIP: the text occurs {count} times"
        else:
            open(path, "w").write(original.replace(mutation["old"], mutation["new"]))
            try:
                built = shell("dotnet build Tests/Statventure.Tests -v q -nologo 2>&1 | grep -E 'rror\\(s\\)'")
                if " 0 Error(s)" not in built:
                    outcome = "BUILD FAILED"
                else:
                    # grep -a: a test's output can hold raw control bytes, and plain grep then calls it binary
                    # and prints nothing, which would read as a survivor.
                    tested = shell(f"timeout 600 dotnet test Tests/Statventure.Tests --no-build -nologo --filter '{mutation['filter']}' 2>&1 | grep -aE 'Passed!|Failed!'").strip()
                    outcome = ("killed: " if "Failed!" in tested else "SURVIVED: ") + tested
            finally:
                open(path, "w").write(original)
        results.append({"label": mutation["label"], "outcome": outcome})
        print(mutation["label"], "->", outcome, flush=True)
    json.dump(results, open(list_path + ".results.json", "w"), indent=1)
    # Leave the build matching the restored sources.
    shell("dotnet build Tests/Statventure.Tests -v q -nologo")


if __name__ == "__main__":
    main(sys.argv[1])

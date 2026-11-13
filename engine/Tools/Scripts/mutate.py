#!/usr/bin/env python3
"""Checks that tests can fail: breaks one rule at a time and runs the tests that should notice.

    Tools/Scripts/mutate.py mutations.json

mutations.json is a list of {"label", "file", "old", "new", "filter"[, "project"]}: `file` relative to engine/,
`old` the exact text (it must occur once), `new` what replaces it, `filter` a `dotnet test --filter` for the tests
that cover the rule, `project` the test project they are in (relative to engine/; Tests/Statventure.Tests unless
given, Tests/Statventure.Game.Tests for the game's: GM1-P4). Each mutation is applied, its project is built and its
tests run, and the file is put back.
A mutation of a shader source (a file under Engine/Statventure/Shaders/Source) compiles the shaders first,
as build.sh does (forced, so file times don't matter), and again once the file is put back: the engine embeds
Shaders/Bin/*.spv, which building the test project alone leaves as they were, so the tests would run the
unmutated shader. A shader that doesn't compile is "BUILD FAILED".

Prints one line each: "killed" (the tests failed, as they should), "SURVIVED" (no test noticed: either
explain why nothing can differ or close the gap), "BUILD FAILED" or "SKIP" (the mutation itself was
invalid, so it shows nothing). Writes the results next to the list. Keep the lists outside the repo, in
~/.cache/stat-fick-build/claude-work/ (a session's scratch folder is lost when the session restarts).

The player's real folders are watched (user decision 2026-11-05, Docs/Engine-Plan.md, "Decision on test runs
and the player's folders"): this machine's game folder (whole) and Documents folder (its top level, where
character files go) are compared before and after each mutation. The tests point them into a scratch folder
of their own (Tests/Statventure.Tests/Infrastructure/PerUserFolders.cs), so a change means something got past
that: a line starting "!!! WROTE TO THE PLAYER'S REAL FOLDERS", the result's "real folders" entry and a count
at the end. A folder that doesn't exist is a state like any other (missing before and after: unchanged).
"""
import json
import os
import subprocess
import sys

ENGINE = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ENV = "source Tools/Scripts/env.sh && "
DEFAULT_PROJECT = "Tests/Statventure.Tests"
LOUD = "!!! WROTE TO THE PLAYER'S REAL FOLDERS"
SHADER_SOURCES = os.path.join("Engine", "Statventure", "Shaders", "Source")


def shell(command):
    return subprocess.run(["bash", "-c", ENV + command], cwd=ENGINE, capture_output=True, text=True).stdout


def is_shader(file):
    """Whether a mutation's file is a shader source (compiled to SPIR-V by build.sh, not by dotnet build)."""
    return os.path.normpath(file).startswith(SHADER_SOURCES + os.sep)


def compile_shaders():
    """build.sh's shader step, every stage rebuilt: True when all compiled."""
    compiled = shell("dotnet build Tools/ShaderCompiler -c Release -v quiet -nologo >/dev/null 2>&1; "
                     f"dotnet Tools/ShaderCompiler/bin/Release/net10.0/ShaderCompiler.dll '{SHADER_SOURCES}' "
                     "Engine/Statventure/Shaders/Bin --force 2>&1")
    return " 0 failed" in compiled


def game_folder():
    """This machine's game folder, as Engine/Statventure/Storage/GameFolder.cs finds it."""
    if os.name == "nt":
        return os.path.join(os.environ.get("APPDATA", ""), "Stat Fick")
    data = os.environ.get("XDG_DATA_HOME", "")
    if not data.startswith("/"):
        data = os.path.join(os.path.expanduser("~"), ".local", "share")
    return os.path.join(data, "stat-fick")


def documents_folder():
    """This machine's Documents folder, as Engine/Statventure/Storage/DocumentsFolder.cs finds it for an
    ordinary user-dirs file (the last XDG_DOCUMENTS_DIR line giving "$HOME/…" or "/…", else the home folder);
    None when that is the home folder itself, whose top level changes all the time."""
    if os.name == "nt":
        return os.path.join(os.environ.get("USERPROFILE", ""), "Documents")
    home = os.environ.get("HOME")
    if not home:
        return None
    config = os.environ.get("XDG_CONFIG_HOME") or os.path.join(home, ".config")
    found = None
    try:
        lines = open(os.path.join(config, "user-dirs.dirs"), encoding="utf-8", errors="replace").read().splitlines()
    except OSError:
        lines = []
    for line in lines:
        name, equals, value = line.strip().partition("=")
        value = value.strip()
        if name.strip() != "XDG_DOCUMENTS_DIR" or not equals or not value.startswith('"'):
            continue
        value = value[1:].split('"')[0].replace("\\", "")
        if value == "$HOME" or value.startswith("$HOME/"):
            found = home + value[5:]
        elif value.startswith("/"):
            found = value
    found = (found or home).rstrip("/") or "/"
    return None if os.path.realpath(found) == os.path.realpath(home) else found


def snapshot(folder, whole):
    """What is in a folder: each entry's path, kind, size and modification time (the whole tree, or the top
    level); None when the folder isn't there."""
    if folder is None or not os.path.isdir(folder):
        return None
    entries = {}
    for root, folders, files in os.walk(folder):
        for name in folders + files:
            path = os.path.join(root, name)
            try:
                info = os.lstat(path)
            except OSError:
                continue
            entries[os.path.relpath(path, folder)] = (name in folders, info.st_size, info.st_mtime_ns)
        if not whole:
            break
    return entries


def watched():
    return [(game_folder(), True), (documents_folder(), False)]


def changes(before, after):
    """The differences between two snapshots of the watched folders, in words (empty when none)."""
    found = []
    for (folder, _), old, new in zip(watched(), before, after):
        if old == new:
            continue
        words = [] if old is not None and new is not None else ["made" if old is None else "removed"]
        old, new = old or {}, new or {}
        added = sorted(set(new) - set(old))
        removed = sorted(set(old) - set(new))
        changed = sorted(p for p in set(old) & set(new) if old[p] != new[p])
        for sign, paths in (("+", added), ("-", removed), ("~", changed)):
            words += [sign + p for p in paths[:10]] + (["…"] if len(paths) > 10 else [])
        found.append(f"{folder}: " + " ".join(words))
    return "; ".join(found)


def main(list_path):
    results = []
    projects = []
    for folder, whole in watched():
        if folder is not None:
            print(f"watching {folder} ({'whole' if whole else 'top level'}; {'there' if os.path.isdir(folder) else 'missing'})", flush=True)
    for mutation in json.load(open(list_path)):
        before = [snapshot(folder, whole) for folder, whole in watched()]
        path = os.path.join(ENGINE, mutation["file"])
        project = mutation.get("project", DEFAULT_PROJECT)
        if project not in projects:
            projects.append(project)
        original = open(path).read()
        count = original.count(mutation["old"])
        if count != 1:
            outcome = f"SKIP: the text occurs {count} times"
        else:
            open(path, "w").write(original.replace(mutation["old"], mutation["new"]))
            shader = is_shader(mutation["file"])
            try:
                if shader and not compile_shaders():
                    outcome = "BUILD FAILED (shaders)"
                elif " 0 Error(s)" not in shell(f"dotnet build '{project}' -v q -nologo 2>&1 | grep -E 'rror\\(s\\)'"):
                    outcome = "BUILD FAILED"
                else:
                    # grep -a: a test's output can hold raw control bytes, and plain grep then calls it binary
                    # and prints nothing, which would read as a survivor.
                    tested = shell(f"timeout 600 dotnet test '{project}' --no-build -nologo --filter '{mutation['filter']}' 2>&1 | grep -aE 'Passed!|Failed!'").strip()
                    outcome = ("killed: " if "Failed!" in tested else "SURVIVED: ") + tested
            finally:
                open(path, "w").write(original)
                if shader:
                    compile_shaders()
        result = {"label": mutation["label"], "outcome": outcome}
        print(mutation["label"], "->", outcome, flush=True)
        wrote = changes(before, [snapshot(folder, whole) for folder, whole in watched()])
        if wrote:
            result["real folders"] = f"{LOUD}: {wrote}"
            print(f"{LOUD}: {mutation['label']}: {wrote}", flush=True)
        results.append(result)
    json.dump(results, open(list_path + ".results.json", "w"), indent=1)
    wrote = [r["label"] for r in results if "real folders" in r]
    if wrote:
        print(f"{LOUD}: {len(wrote)} mutation(s): " + ", ".join(wrote), flush=True)
    # Leave the builds matching the restored sources.
    for project in projects or [DEFAULT_PROJECT]:
        shell(f"dotnet build '{project}' -v q -nologo")


if __name__ == "__main__":
    main(sys.argv[1])

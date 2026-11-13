#!/usr/bin/env python3
"""The packages end to end (M12 step 8; Docs/Packaging.md, "How it works", step 8). Run through
Tools/Scripts/end-to-end.sh, which says how; Linux only (the Windows packages: Engine-Plan.md, "Windows check", item 9).

The two Linux archives unpacked into a scratch folder and their programs started as a player starts them, each in a
headless display (cage on wlroots' headless backend with a 2400 x 2400 output, else Xvfb; the user's displays unset),
on a scratch per-user folder (XDG_DATA_HOME, and the editor's --player-folder the same per-user folder), OpenAL Soft
on its null device; then every expectation below held, each a line "ok" or "FAIL" with its name, and the check fails
if any fails. Nothing of the player's real folders is read or written; they are compared before and after.

The stand-in (stat-fick): the splash first and the page after it (--capture-splash, --capture), the credits view
(--about), a crash on the main thread and on a thread of its own (--crash, --crash-thread: the crash file, the crash
window's picture, exit 1, nothing of .NET's own), the mod the editor made mounted from the per-user mods folder (its log
names it); eleven runs in all, the oldest log then removed and ten kept. The editor (statventure-editor): its first
start asking "Where is Stat Fick?", --game naming the unpacked game (its data library loaded, the versions the same),
the remembered folder (editor/game.json), a game of another version refused on the command line (exit 2, both versions
named) and as the remembered folder (the screen saying why); eleven runs in all, ten logs kept. Every run's log: one per
run, its first line the program, the archives' version and the commit, the splash shown before anything else. The mod:
made in-process by the editor's test GameFolderModTests (step 14's flow and the game's own kind, through the UI) with
--game on the unpacked game, built from this tree with the packages' version so the versions match, which also draws
the editor's About dialog (the editor has no option of its own for it).
"""
import datetime
import json
import os
import re
import shutil
import signal
import subprocess
import sys
import tarfile
import time

from PIL import Image

ENGINE = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
sys.path.insert(0, os.path.join(ENGINE, "Tools", "Packaging"))
import check_packages  # noqa: E402  (its archive names and the real folders' state)

MODE = "2400x2400"          # the headless output (the editor's screens are captured at this size too)
RUN_SECONDS = 240           # one program's run, at most (lavapipe draws 2400 x 2400 frames on the CPU)
KEPT = 10                   # logs kept per program (M12-8, Q21)
RUNS = KEPT + 1             # each program started this often: the oldest log removed at the last start
TEST = "Statventure.Tests.Editor.EndToEnd.GameFolderModTests.AModMadeAgainstTheGameFolderNamedWithGameLoadsInThatGame"
GROUND = (0x1a, 0x1c, 0x1a)         # the dark ground of the splash, the credits, the crash window and the editor (P4)
PANES = (0x29, 0x2c, 0x28)          # the editor's panes
PARCHMENT = (0xd5, 0xc3, 0x9b)      # the game's page (GM1 step 2's: the Gothic ink theme's parchment)
BRUTAL_YELLOW = (0xf1, 0xc4, 0x0f)  # the BRUTAL logo's yellow
EMBER = (0xd7, 0x7a, 0x50)          # the editor's accent: a focused field's border, the main button


class Flow:
    def __init__(self, work, version):
        self.work = work
        self.version = version
        self.results = []
        self.facts = []
        self.runs = 0

    def check(self, name, ok, detail=""):
        self.results.append((name, bool(ok)))
        print(("ok    " if ok else "FAIL  ") + name + (f" ({detail})" if detail else ""), flush=True)
        return bool(ok)

    def fact(self, text):
        self.facts.append(text)
        print("      " + text, flush=True)


# --- Running a program headless --------------------------------------------------------------------------------------

def headless(command, env, folder):
    """Runs a command in a headless display of its own (cage with a MODE output, else Xvfb at MODE), its output and exit
    code kept in `folder`. (exit code or None, output, seconds)."""
    env = {k: v for k, v in env.items() if k not in ("DISPLAY", "WAYLAND_DISPLAY")}
    output, code = os.path.join(folder, "output.txt"), os.path.join(folder, "exit-code.txt")
    env.update(E2E_OUTPUT=output, E2E_CODE=code, E2E_MODE=MODE)
    if shutil.which("cage"):
        env.update(WLR_BACKENDS="headless", WLR_LIBINPUT_NO_DEVICES="1", XDG_SESSION_TYPE="wayland")
        # The output made MODE first (cage makes its window the output's size), the command's end ending cage.
        inner = ('wlr-randr --output HEADLESS-1 --custom-mode "$E2E_MODE" >/dev/null 2>&1; sleep 0.3; '
                 '"$@" > "$E2E_OUTPUT" 2>&1; echo $? > "$E2E_CODE"; kill -TERM $PPID')
        line = ["cage", "--", "sh", "-c", inner, "sh"]
    elif shutil.which("xvfb-run"):
        line = ["xvfb-run", "-a", "-s", f"-screen 0 {MODE}x24", "sh", "-c",
                '"$@" > "$E2E_OUTPUT" 2>&1; echo $? > "$E2E_CODE"', "sh"]
    else:
        return None, "no headless display: cage or xvfb-run is needed", 0
    start = time.monotonic()
    with open(os.path.join(folder, "display.txt"), "wb") as display:
        process = subprocess.Popen(line + command, env=env, stdout=display, stderr=subprocess.STDOUT, start_new_session=True)
        try:
            process.wait(timeout=RUN_SECONDS)
        except subprocess.TimeoutExpired:
            os.killpg(process.pid, signal.SIGKILL)
            process.wait()
            return None, f"it didn't end within {RUN_SECONDS} s", RUN_SECONDS
    seconds = time.monotonic() - start
    text = open(output, encoding="utf-8", errors="replace").read() if os.path.exists(output) else ""
    if not os.path.exists(code):
        return None, "the headless display ended before it: " + text[-500:], seconds
    return int(open(code).read().strip() or "-1"), text, seconds


class Programs:
    """The unpacked programs and the scratch per-user folder they share (as on a player's machine)."""

    def __init__(self, flow, run_folder, game, editor):
        self.flow = flow
        self.run_folder = run_folder
        self.game_folder = game
        self.stand_in = os.path.join(game, "stat-fick")
        self.editor = os.path.join(editor, "statventure-editor")
        self.data_home = os.path.join(run_folder, "player")
        self.player = os.path.join(self.data_home, "stat-fick")
        self.logs = os.path.join(self.player, "logs")
        self.captures = os.path.join(run_folder, "captures")
        os.makedirs(self.data_home)
        os.makedirs(self.captures)
        self.env = dict(os.environ, XDG_DATA_HOME=self.data_home, ALSOFT_DRIVERS="null")
        self.count = {"stat-fick": 0, "statventure-editor": 0}
        self.first_log = {}
        self.commits = set()

    def capture(self, name):
        return os.path.join(self.captures, name + ".png")

    def logs_of(self, program):
        if not os.path.isdir(self.logs):
            return set()
        pattern = re.compile(rf"^{re.escape(program)}-\d{{4}}-\d\d-\d\d_\d\d-\d\d-\d\d(-\d+)?\.log$")
        return {name for name in os.listdir(self.logs) if pattern.match(name)}

    def crash_files(self):
        if not os.path.isdir(self.logs):
            return set()
        return {name for name in os.listdir(self.logs) if re.match(r"^crash-\d{4}-\d\d-\d\d_\d\d-\d\d-\d\d(-\d+)?\.txt$", name)}

    def start(self, name, program, arguments, expect_code=0, log=True):
        """Starts the program (stat-fick or statventure-editor) with `arguments`; checks its exit code and, for a run
        that opens its window (`log`), its one new log: the first line (the program, the version, the commit), the
        splash before anything else. Gives (output, the log's text or "", the log's name)."""
        flow = self.flow
        self.flow.runs += 1
        folder = os.path.join(self.run_folder, "runs", f"{flow.runs:02d}-{name}")
        os.makedirs(folder)
        before, crashes = self.logs_of(program), self.crash_files()
        executable = self.stand_in if program == "stat-fick" else self.editor
        code, output, seconds = headless([executable] + arguments, self.env, folder)
        flow.check(f"{name}: ended with {expect_code}", code == expect_code,
                   f"{code}: {output[-600:]}" if code != expect_code else f"{seconds:.1f} s")
        after = self.logs_of(program)
        new = sorted(after - before)
        if not log:
            flow.check(f"{name}: no log (it ends before its window)", not new and self.crash_files() == crashes, ", ".join(new))
            return output, "", None
        self.count[program] += 1
        if not flow.check(f"{name}: one new log, {program}-<date>_<time>.log", len(new) == 1, ", ".join(new) or "none"):
            return output, "", None
        name_of_log = new[0]
        self.first_log.setdefault(program, name_of_log)
        text = open(os.path.join(self.logs, name_of_log), encoding="utf-8", errors="replace").read()
        lines = text.splitlines()
        header = re.match(rf"^\s*[0-9.]+ INFO  \[Program\] {re.escape(program)} (\S+), commit ([0-9a-f]{{40}})$", lines[0] if lines else "")
        flow.check(f"{name}: the log's first line names {program} {flow.version} and its commit",
                   header is not None and header.group(1) == flow.version, lines[0].strip() if lines else "empty")
        if header:
            self.commits.add(header.group(2))
        flow.check(f"{name}: the log's next lines the start time and the system",
                   len(lines) > 2 and re.search(r"\[Program\] started \d{4}-\d\d-\d\d \d\d:\d\d:\d\d [+-]\d\d:\d\d$", lines[1])
                   and re.search(r"\[Program\] .+ X64, \.NET \d+\.\d+\.\d+$", lines[2]))
        splash = text.find("[Application] the splash shown (2 s)")
        loaded = min([i for i in (text.find("[Game]"), text.find("[Editor]"), text.find("[Application] captured")) if i >= 0] or [len(text)])
        if "--capture-splash" not in arguments:
            flow.check(f"{name}: the splash shown for its 2 s before the program's own", 0 <= splash < loaded)
        return output, text, name_of_log


# --- Pictures ---------------------------------------------------------------------------------------------------------

def colours(path):
    """A picture's colours by count (most first) and its size; ([], None) when there is none."""
    if not os.path.isfile(path):
        return [], None
    image = Image.open(path).convert("RGB")
    found = image.getcolors(image.width * image.height) or []
    return sorted(((count, colour) for count, colour in found), reverse=True), image.size


def share(found, test):
    total = sum(count for count, _ in found) or 1
    return sum(count for count, colour in found if test(colour)) / total


def near(colour, target, by=6):
    return all(abs(a - b) <= by for a, b in zip(colour, target))


def picture(flow, name, path, what, test):
    found, size = colours(path)
    if not found:
        return flow.check(f"{name}: {what}", False, f"no picture at {path}")
    return flow.check(f"{name}: {what}", test(found), f"{size[0]} x {size[1]}, most {found[0][1]} {found[0][0]}")


# --- The flow ---------------------------------------------------------------------------------------------------------

def unpack(archive, into):
    os.makedirs(into)
    with tarfile.open(archive, "r:gz") as tar:
        tar.extractall(into, filter="data")


def other_version(version):
    """A release version that isn't `version`: the next release of its day, or for a dev build today's first."""
    if version == "dev":
        return datetime.datetime.now(datetime.timezone.utc).strftime("%Y.%-m.%-d") + ".1"
    year, month, day, n = version.split(".")
    return f"{year}.{month}.{day}.{int(n) + 1}"


def dotnet(arguments, log, env=None):
    """A dotnet command from the engine folder (MSBuild's nodes and server not kept), its output in `log`."""
    env = dict(env or os.environ, MSBUILDDISABLENODEREUSE="1", DOTNET_CLI_USE_MSBUILD_SERVER="0")
    with open(log, "w", encoding="utf-8") as out:
        return subprocess.run(["dotnet"] + arguments, cwd=ENGINE, env=env, stdout=out, stderr=subprocess.STDOUT).returncode


def stand_in_runs(flow, p):
    """The stand-in's own expectations (its runs 1 to 4)."""
    output, log, _ = p.start("stand-in start", "stat-fick", ["--capture-splash", p.capture("stand-in-splash"), "--capture", p.capture("stand-in-page")])
    picture(flow, "stand-in start", p.capture("stand-in-splash"), "the splash's first frame: the BRUTAL logo on the dark ground",
            lambda f: near(f[0][1], GROUND) and share(f, lambda c: near(c, BRUTAL_YELLOW, 12)) > 0.001
            and share(f, lambda c: min(c) > 240) > 0.003)
    picture(flow, "stand-in start", p.capture("stand-in-page"), "--capture after the splash: the page",
            lambda f: near(f[0][1], PARCHMENT))
    splash, captured = log.find("the splash shown (2 s)"), log.rfind("[Application] captured")
    flow.check("stand-in start: the page captured after the splash's 2 s", 0 <= splash < captured)
    gpu = re.search(r"\[Graphics\] GPU (.+)", log)
    flow.fact(f"the GPU: {gpu.group(1) if gpu else 'not logged'}")

    output, log, _ = p.start("stand-in credits", "stat-fick", ["--about", "--capture", p.capture("stand-in-credits")])
    flow.check("stand-in credits: the credits view opened (--about)", "[Game] the credits view opened" in log)
    picture(flow, "stand-in credits", p.capture("stand-in-credits"), "the credits view: light text on the dark ground",
            lambda f: near(f[0][1], GROUND) and share(f, lambda c: min(c) > 200) > 0.002)

    for kind, where, message in (("--crash", "on the main thread", "the game crashed on purpose (--crash)"),
                                 ("--crash-thread", 'on the thread "Game crash"',
                                  "the game crashed on purpose on a thread of its own (--crash-thread)")):
        name = "stand-in " + kind[2:]
        crashes = p.crash_files()
        output, log, log_name = p.start(name, "stat-fick", [kind, "--capture", p.capture(name.replace(" ", "-"))], expect_code=1)
        new = sorted(p.crash_files() - crashes)
        if flow.check(f"{name}: one new crash file, crash-<date>_<time>.txt", len(new) == 1, ", ".join(new) or "none"):
            path = os.path.join(p.logs, new[0])
            lines = open(path, encoding="utf-8", errors="replace").read().splitlines()
            flow.check(f"{name}: the crash file's first line the program, version and commit",
                       bool(lines) and re.fullmatch(rf"stat-fick {re.escape(flow.version)}, commit [0-9a-f]{{40}}", lines[0]),
                       lines[0] if lines else "empty")
            flow.check(f"{name}: the crash file says when and where ({where})",
                       len(lines) > 1 and re.fullmatch(rf"crashed \d{{4}}-\d\d-\d\d \d\d:\d\d:\d\d [+-]\d\d:\d\d {re.escape(where)}", lines[1]),
                       lines[1] if len(lines) > 1 else "")
            flow.check(f"{name}: the crash file names the run's log and holds the exception",
                       len(lines) > 3 and lines[3] == f"log {os.path.join(p.logs, log_name or '?')}"
                       and f"System.InvalidOperationException: {message}" in lines)
            order = [log.find(f"crash file {path}"), log.find("the crash window opens"),
                     log.find(f"captured {p.capture(name.replace(' ', '-'))}"), log.find("the crash window closed")]
            flow.check(f"{name}: the log: the crash file, then the crash window opened, captured and closed",
                       all(i >= 0 for i in order) and order == sorted(order))
        flow.check(f"{name}: nothing of .NET's own (no \"Unhandled exception\")", "Unhandled exception" not in output)
        picture(flow, name, p.capture(name.replace(" ", "-")), "the crash window: the dark ground, the path's field and Close in ember",
                lambda f: near(f[0][1], GROUND) and share(f, lambda c: near(c, EMBER, 8)) > 0.0002)


def editor_runs(flow, p, other_game, other):
    """The editor's own expectations (its runs 1 to 4, and the refusal, which opens no window)."""
    game = p.game_folder
    v = flow.version
    output, log, _ = p.start("editor first start", "statventure-editor", ["--player-folder", p.player, "--capture", p.capture("editor-where")])
    flow.check("editor first start: no game known, \"Where is Stat Fick?\" (the log)", "[Editor] no game's folder known: \"Where is Stat Fick?\"" in log)
    picture(flow, "editor first start", p.capture("editor-where"), "\"Where is Stat Fick?\": the page on the dark ground with its ember accents",
            lambda f: near(f[0][1], GROUND) and share(f, lambda c: near(c, EMBER, 8)) > 0.0005)

    library = os.path.join(game, "Statventure.Game.Data.dll")
    output, log, _ = p.start("editor --game", "statventure-editor", ["--game", game, "--player-folder", p.player, "--capture", p.capture("editor-game")])
    loaded = re.search(rf"\[Editor\] the game's data library {re.escape(library)}: (\S+), commit ([0-9a-f]{{40}}), (\d+) record kinds", log)
    flow.check(f"editor --game: the game's data library loaded from the game's folder, its version {v} (the log)",
               loaded is not None and loaded.group(1) == v, loaded.group(0) if loaded else output[:300])
    if loaded:
        p.commits.add(loaded.group(2))
    flow.check("editor --game: the editor on the game (the log)", f"[Editor] the game in {game}\n" in log + "\n")
    picture(flow, "editor --game", p.capture("editor-game"), "the editor's shell (its panes)", lambda f: near(f[0][1], PANES))

    os.makedirs(os.path.join(p.player, "editor"), exist_ok=True)
    remembered = os.path.join(p.player, "editor", "game.json")

    def remember(folder):
        with open(remembered, "w", encoding="utf-8") as f:
            json.dump({"version": 1, "folder": folder}, f)

    remember(other_game)
    output, log, _ = p.start("editor remembers another version", "statventure-editor", ["--player-folder", p.player, "--capture", p.capture("editor-other-version")])
    said = f"[Editor] \"Where is Stat Fick?\": {other_game} holds Stat Fick {other}, and this editor works only with Stat Fick {v}"
    flow.check("editor remembers another version: \"Where is Stat Fick?\" saying both versions (the log)", said in log)
    picture(flow, "editor remembers another version", p.capture("editor-other-version"), "\"Where is Stat Fick?\" again",
            lambda f: near(f[0][1], GROUND) and share(f, lambda c: near(c, EMBER, 8)) > 0.0005)

    # (--capture too, so an editor that took the game would end by itself rather than wait for the time limit.)
    output, _, _ = p.start("editor --game of another version", "statventure-editor",
                           ["--game", other_game, "--player-folder", p.player, "--capture", p.capture("editor-not-refused")], expect_code=2, log=False)
    flow.check("editor --game of another version: refused naming both versions, with the usage",
               f"Statventure Editor: {other_game} holds Stat Fick {other}, and this editor works only with Stat Fick {v}\n" in output
               and "usage: statventure-editor [--repo <engine folder> | --game <game folder>]" in output, output.strip()[-400:])

    remember(game)
    output, log, _ = p.start("editor remembers the game", "statventure-editor", ["--player-folder", p.player, "--capture", p.capture("editor-remembered")])
    loaded = re.search(rf"\[Editor\] the game's data library {re.escape(library)}: (\S+), commit ([0-9a-f]{{40}}), (\d+) record kinds", log)
    flow.check(f"editor remembers the game: its data library loaded, {v}, the kinds counted (the log)",
               loaded is not None and loaded.group(1) == v, loaded.group(0) if loaded else "")
    if loaded:
        p.commits.add(loaded.group(2))
    flow.check("editor remembers the game: the editor on it (the log)", f"[Editor] the game in {game}\n" in log + "\n")
    picture(flow, "editor remembers the game", p.capture("editor-remembered"), "the editor's shell", lambda f: near(f[0][1], PANES))


def mod_in_process(flow, p, build, logs):
    """The mod made by the editor in-process against the unpacked game (GameFolderModTests, built with the packages'
    version), then mounted by the packaged stand-in."""
    env = dict(os.environ, STATVENTURE_END_TO_END_GAME=p.game_folder, STATVENTURE_END_TO_END_VERSION=flow.version,
               STATVENTURE_END_TO_END_MODS=os.path.join(p.player, "mods"), STATVENTURE_END_TO_END_ABOUT=p.capture("editor-about"))
    log = os.path.join(logs, "in-process-test.log")
    code = dotnet(["test", "Tests/Statventure.Tests", "--no-build", "-c", "Debug", "--artifacts-path", build, "-v", "quiet",
                   "-nologo", "--filter", f"FullyQualifiedName={TEST}"], log, env)
    text = open(log, encoding="utf-8", errors="replace").read()
    flow.check("the mod: the editor's flow against the unpacked game passed (GameFolderModTests, one test)",
               code == 0 and re.search(r"Passed!\s+-\s+Failed:\s+0, Passed:\s+1, Skipped:\s+0, Total:\s+1", text), text.strip()[-600:])
    mod = os.path.join(p.player, "mods", "ember-rite.pak")
    flow.check("the mod: ember-rite.pak in the per-user folder's mods/", os.path.isfile(mod))
    picture(flow, "the editor's About (in-process)", p.capture("editor-about"), "the credits dialog over the editor",
            lambda f: len(f) > 100 and share(f, lambda c: min(c) > 200) > 0.002)

    output, log_text, _ = p.start("stand-in with the mod", "stat-fick", ["--capture", p.capture("stand-in-mod")])
    flow.check("stand-in with the mod: its log names the mod mounted from mods/", f"[Data] mod {mod}\n" in log_text + "\n")
    picture(flow, "stand-in with the mod", p.capture("stand-in-mod"), "the page drawn (the mod's sounds loaded)", lambda f: near(f[0][1], PARCHMENT))


def rotation(flow, p):
    """Each program started until it has run RUNS times: then KEPT logs, its first one gone; the other program's and the
    crash files untouched."""
    for program, arguments in (("stat-fick", ["--capture", p.capture("stand-in-again")]),
                               ("statventure-editor", ["--player-folder", p.player, "--capture", p.capture("editor-again")])):
        label = "stand-in" if program == "stat-fick" else "editor"
        while p.count[program] < RUNS - 1:
            p.start(f"{label} again ({p.count[program] + 1})", program, arguments)
        flow.check(f"{label}: after {RUNS - 1} runs {RUNS - 1} logs, the first one there",
                   len(p.logs_of(program)) == RUNS - 1 and p.first_log.get(program) in p.logs_of(program), len(p.logs_of(program)))
        crashes, others = p.crash_files(), p.logs_of("statventure-editor" if program == "stat-fick" else "stat-fick")
        _, _, last = p.start(f"{label} again ({RUNS})", program, arguments)
        kept = p.logs_of(program)
        flow.check(f"{label}: after {RUNS} runs {KEPT} logs kept, the oldest gone, the newest there",
                   len(kept) == KEPT and p.first_log.get(program) not in kept and last in kept,
                   f"{len(kept)} kept; the first {p.first_log.get(program)}")
        flow.check(f"{label}: the crash files and the other program's logs untouched",
                   p.crash_files() == crashes and p.logs_of("statventure-editor" if program == "stat-fick" else "stat-fick") == others)


def main(argv):
    work, archives, version = None, [], None
    i = 0
    while i < len(argv):
        if argv[i] == "--work":
            work = os.path.abspath(argv[i + 1])
            i += 2
        else:
            archives.append(os.path.abspath(argv[i]))
            i += 1
    names = [check_packages.ARCHIVE_NAME.match(os.path.basename(a)) for a in archives]
    if work is None or len(archives) != 2 or not all(names) or {n.group(1) for n in names} != {"stat-fick", "statventure-editor"} \
            or {n.group(3) for n in names} != {"linux.tar.gz"} or len({n.group(2) for n in names}) != 1:
        sys.exit("usage: end_to_end.py --work <folder> <stat-fick-<v>-linux.tar.gz> <statventure-editor-<v>-linux.tar.gz>")
    version = names[0].group(2)
    game_archive, editor_archive = sorted(archives, key=lambda a: "statventure-editor" in os.path.basename(a))
    flow = Flow(work, version)
    run_folder = os.path.join(work, "run")
    logs = os.path.join(run_folder, "build-logs")
    shutil.rmtree(run_folder, ignore_errors=True)
    os.makedirs(logs)
    started = time.monotonic()
    real_before = check_packages.real_folders()
    print(f"end to end: {os.path.basename(game_archive)} and {os.path.basename(editor_archive)}, in {run_folder}", flush=True)
    for archive in (game_archive, editor_archive):
        size = os.path.getsize(archive)
        flow.fact(f"{os.path.basename(archive)}: {size / 1048576:.1f} MB ({size} bytes)")

    # The programs unpacked as a player unpacks them.
    unpack(game_archive, os.path.join(run_folder, "game"))
    unpack(editor_archive, os.path.join(run_folder, "editor"))
    game = os.path.join(run_folder, "game", f"stat-fick-{version}")
    editor = os.path.join(run_folder, "editor", f"statventure-editor-{version}")
    if not flow.check(f"the archives unpacked: stat-fick-{version}/stat-fick and statventure-editor-{version}/statventure-editor",
                      os.path.isfile(os.path.join(game, "stat-fick")) and os.path.isfile(os.path.join(editor, "statventure-editor"))):
        return 1
    p = Programs(flow, run_folder, game, editor)

    # A game of another version: a copy of the unpacked game whose data library is built with another version (the
    # library's project as the publish builds it, -p:Version; the editor reads its version and never loads it).
    other = other_version(version)
    other_build = os.path.join(work, "other-build")
    built = dotnet(["build", "Game/Statventure.Game.Data/Statventure.Game.Data.csproj", "-c", "Release", f"-p:Version={other}",
                    "--artifacts-path", other_build, "-v", "quiet", "-nologo"], os.path.join(logs, "other-version.log"))
    other_game = os.path.join(run_folder, "other-game", f"stat-fick-{other}")
    shutil.copytree(game, other_game)
    other_library = os.path.join(other_build, "bin", "Statventure.Game.Data", "release", "Statventure.Game.Data.dll")
    if flow.check(f"the game of another version: the data library built as {other}", built == 0 and os.path.isfile(other_library)):
        shutil.copyfile(other_library, os.path.join(other_game, "Statventure.Game.Data.dll"))

    # The editor's in-process flow, built from this tree with the packages' version (its own artifacts, the repo's
    # builds untouched), so the editor and the unpacked game's library are of one version as a player's are.
    build = os.path.join(work, "build")
    version_property = [] if version == "dev" else [f"-p:Version={version}"]
    built = dotnet(["build", "Tests/Statventure.Tests", "-c", "Debug", *version_property, "--artifacts-path", build, "-v", "quiet",
                    "-nologo"], os.path.join(logs, "tests-build.log"))
    flow.check(f"the editor's in-process flow built with the version {version}", built == 0)

    # Each part in turn; one that stops (an exception of the flow's own) is a failure, and the rest still runs.
    for part, call in (("the stand-in", lambda: stand_in_runs(flow, p)), ("the editor", lambda: editor_runs(flow, p, other_game, other)),
                       ("the mod", lambda: mod_in_process(flow, p, build, logs)), ("the logs kept", lambda: rotation(flow, p))):
        try:
            call()
        except Exception as error:  # noqa: BLE001 (any of the flow's own problems is reported, not raised)
            flow.check(f"{part}: the flow went through", False, f"{type(error).__name__}: {error}")

    flow.check("one commit in every log, crash file and the game's library", len(p.commits) == 1, ", ".join(sorted(p.commits)))
    flow.check("the player's real folders unchanged (this machine's game folder and the Documents folder's top level)",
               check_packages.real_folders() == real_before)
    for program in ("stat-fick", "statventure-editor"):
        for name in sorted(p.logs_of(program))[-1:]:
            first = open(os.path.join(p.logs, name), encoding="utf-8").readline().strip()
            flow.fact(f"the newest log {name}: {first}")
    flow.fact(f"commit {', '.join(sorted(p.commits))}; {flow.runs} program runs; captures in {p.captures}")
    failed = [name for name, ok in flow.results if not ok]
    print(f"end to end: {len(flow.results) - len(failed)} of {len(flow.results)} expectations held in "
          f"{time.monotonic() - started:.0f} s" + (f"; FAILED: {len(failed)}" if failed else "; ALL PASSED"), flush=True)
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

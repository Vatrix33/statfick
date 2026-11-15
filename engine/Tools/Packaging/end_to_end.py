#!/usr/bin/env python3
"""The packages end to end (M12 step 8, made the game's by GM1 step 11; Docs/Packaging.md, "How it works", step 8, and
Docs/Game/Foundation.md, "How it works", step 11). Run through Tools/Scripts/end-to-end.sh, which says how; Linux only
(the Windows packages: Engine-Plan.md, "Windows check").

The two Linux archives unpacked into a scratch folder and their programs started as a player starts them, each in a
headless display (cage on wlroots' headless backend with a 2400 x 2400 output, else Xvfb; the user's displays unset),
on a scratch per-user folder (XDG_DATA_HOME, and the editor's --player-folder the same per-user folder), OpenAL Soft
on its null device; then every expectation below held, each a line "ok" or "FAIL" with its name, and the check fails
if any fails. Nothing of the player's real folders is read or written; they are compared before and after.

The game (stat-fick), in the order a player and a modder meet it: a first start (the splash, then the start screen with
Continue greyed, no mods, no theme started); the settings screen with the first start's values and no mods (--open
settings); About (--about); a crash on the main thread and on a thread of its own (--crash, --crash-thread: the crash
file, the crash window's picture, exit 1, nothing of .NET's own); a second start while the first runs (it ends with 0,
the first brings its window forward). Then the editor's mod mounted from the per-user mods folder ([Mods] ... loaded, the
file system's [Data] mod line) and shown in the mod list. Then what a player's own runs leave, made in-process by the
game's test PlayerFolderTests through the game's real parts (a character in its slot, a setting changed, the
character's file, a mod of a kind the game doesn't know): with that mod and one whose index has a byte flipped, both
left out (named in the log, in the start screen's line and in the mod list) and the good mod still loaded, Continue
enabled; Continue run (--run continue: the slot opened and written back on quit); the character file loaded into a new
slot (--run load-character=<file>); the good mod switched off in settings.json (not checked, not mounted, shown as off).
The editor (statventure-editor): its first start asking "Where is Stat Fick?", --game naming the unpacked game (its data
library loaded, the versions the same), the remembered folder (editor/game.json), a game of another version refused on
the command line (exit 2, both versions named) and as the remembered folder (the screen saying why). Every run's log:
one per run, its first line the program, the archives' version and the commit, the splash shown before anything else;
each program's eleventh run leaves ten logs, the oldest gone; no game run starts a theme ([Audio] theme). The editor's
mod: made in-process by the editor's test GameFolderModTests (step 14's flow and the game's own kind, through the UI)
with --game on the unpacked game, built from this tree with the packages' version so the versions match, which also
draws the editor's About dialog (the editor has no option of its own for it). The game never runs open-game-folder
here (it would open a file manager on the user's screen), and its character file lies in the run's scratch folder.
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
RUNS = KEPT + 1             # each program started at least this often: the oldest log removed at that start
TEST = "Statventure.Tests.Editor.EndToEnd.GameFolderModTests.AModMadeAgainstTheGameFolderNamedWithGameLoadsInThatGame"
GAME_TEST = "Statventure.Game.Tests.PlayerFolderTests.ACharacterASettingACharacterFileAndAModTheGameLeavesOutForThePackagedGame"
FIRST_FRAMES = 120          # the first of two starts on one folder draws this many frames (the second asks it meanwhile)
GROUND = (0x1a, 0x1c, 0x1a)         # the dark ground of the splash, the crash window and the editor (P4)
PANES = (0x29, 0x2c, 0x28)          # the editor's panes
STONE = (0x11, 0x0c, 0x09)          # the game's page: the game's document in its first start's dark theme (GM1 step 8: html.dark-mode's --paper)
ABOUT_GROUND = (0x25, 0x1c, 0x16)   # the game's windows' dialog in the first start's dark theme (GM1 step 9; About's, the settings screen's)
DARKENED = 14                       # a window's backdrop darkens the page under it: every channel at most this
BRUTAL_YELLOW = (0xf1, 0xc4, 0x0f)  # the BRUTAL logo's yellow
EMBER = (0xd7, 0x7a, 0x50)          # the editor's accent: a focused field's border, the main button
# A mod row's stripe in the dark theme (settings.css: --sage loaded, --soft off, --blood left out; the Gothic theme's),
# measured in the packaged game's pictures on 2026-11-15: a bar of one colour, 3 px wide, the row's height.
LOADED_STRIPE = (0x9c, 0xbc, 0xa1)
OFF_STRIPE = (0xa8, 0x97, 0x7a)
LEFT_OUT_STRIPE = (0xb0, 0x3a, 0x2e)
STRIPE_RUN = 60                     # a stripe's least height (px): a row is taller; nothing else on the screen is a bar of those colours
FIRST_VALUES = "master 100%, music 100%, effects 60%, theme Dark, zoom 100%, full screen off, Dev Mode off"  # Q22, GM1-4, GM1-12
FORWARDED = "Stat Fick is already running for this player: its window was asked to come forward."
# What the game's test makes (PlayerFolderTests): the character, its slot, the setting, the files.
CHARACTER, SLOT, CHARACTER_FILE, ODD_MOD = "Ser Alrik", "ser-alrik", "stat-fick-character.json", "odd-charms.pak"
CHANGED_VALUES = FIRST_VALUES.replace("effects 60%", "effects 40%")
GOOD_MOD, BROKEN_MOD = "ember-rite.pak", "broken-index.pak"
LOADED = "[Mods] ember-rite.pak: loaded (Ember Rite 1.0 by Vatrix)"
BROKEN_PROBLEM = "the index doesn't match its hash (damaged)"
ODD_PROBLEM = "charms/lucky: a record of type e2e.charm, which isn't a record type known here"
LEFT_OUT_LINE = "2 mods were left out: broken-index and Odd Charms. Settings › Mods tells why."


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

def launch(command, env, folder):
    """Starts a command in a headless display of its own (cage with a MODE output, else Xvfb at MODE), its output and exit
    code kept in `folder`; the handle for `finish`, or a reason it couldn't start."""
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
        return "no headless display: cage or xvfb-run is needed"
    display = open(os.path.join(folder, "display.txt"), "wb")
    process = subprocess.Popen(line + command, env=env, stdout=display, stderr=subprocess.STDOUT, start_new_session=True)
    return process, display, output, code, time.monotonic()


def finish(handle):
    """Waits for a launched command (killed after RUN_SECONDS). (exit code or None, output, seconds)."""
    if isinstance(handle, str):
        return None, handle, 0
    process, display, output, code, start = handle
    try:
        process.wait(timeout=max(1, RUN_SECONDS - (time.monotonic() - start)))
    except subprocess.TimeoutExpired:
        os.killpg(process.pid, signal.SIGKILL)
        process.wait()
        display.close()
        return None, f"it didn't end within {RUN_SECONDS} s", RUN_SECONDS
    display.close()
    seconds = time.monotonic() - start
    text = open(output, encoding="utf-8", errors="replace").read() if os.path.exists(output) else ""
    if not os.path.exists(code):
        return None, "the headless display ended before it: " + text[-500:], seconds
    return int(open(code).read().strip() or "-1"), text, seconds


def headless(command, env, folder):
    """Runs a command in a headless display of its own and waits for it: (exit code or None, output, seconds)."""
    return finish(launch(command, env, folder))


LOG_LINE = re.compile(r"^\s*[0-9.]+ (INFO|WARN|ERROR|DEBUG)\s+\[([^\]]+)\] (.*)$")


def lines_of(log):
    """A log's lines as (level, category, message), in order."""
    found = []
    for line in log.splitlines():
        match = LOG_LINE.match(line)
        if match:
            found.append(match.groups())
    return found


def messages(log, category):
    """The messages of `category` in a log, in order."""
    return [message for _, c, message in lines_of(log) if c == category]


class Programs:
    """The unpacked programs and the scratch per-user folder they share (as on a player's machine)."""

    def __init__(self, flow, run_folder, game, editor):
        self.flow = flow
        self.run_folder = run_folder
        self.game_folder = game
        self.game = os.path.join(game, "stat-fick")
        self.editor = os.path.join(editor, "statventure-editor")
        self.data_home = os.path.join(run_folder, "player")
        self.player = os.path.join(self.data_home, "stat-fick")
        self.logs = os.path.join(self.player, "logs")
        self.mods = os.path.join(self.player, "mods")
        self.saves = os.path.join(self.player, "saves")
        self.captures = os.path.join(run_folder, "captures")
        self.files = os.path.join(run_folder, "files")   # the character file's and the odd mod's (never the real Documents)
        os.makedirs(self.data_home)
        os.makedirs(self.captures)
        self.env = dict(os.environ, XDG_DATA_HOME=self.data_home, ALSOFT_DRIVERS="null")
        self.count = {"stat-fick": 0, "statventure-editor": 0}
        self.first_log = {}
        self.commits = set()
        self.game_logs = {}   # every game run's log text by its name, for the checks over all of them

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

    def folder(self, name):
        self.flow.runs += 1
        folder = os.path.join(self.run_folder, "runs", f"{self.flow.runs:02d}-{name}")
        os.makedirs(folder)
        return folder

    def executable(self, program):
        return self.game if program == "stat-fick" else self.editor

    def start(self, name, program, arguments, expect_code=0, log=True, handle=None, before=None):
        """Starts the program (stat-fick or statventure-editor) with `arguments` (or waits for one `launch`ed as
        `handle`, its logs and crash files before it `before`); checks its exit code and, for a run that opens its
        window (`log`), its one new log: the first line (the program, the version, the commit), the splash before
        anything else; at the program's 10th and 11th run, the logs kept. Gives (output, the log's text or "", the log's
        name)."""
        flow = self.flow
        other = "statventure-editor" if program == "stat-fick" else "stat-fick"
        if handle is None:
            before = (self.logs_of(program), self.crash_files(), self.logs_of(other))
            code, output, seconds = headless([self.executable(program)] + arguments, self.env, self.folder(name))
        else:
            code, output, seconds = finish(handle)
        logs, crashes, others = before
        flow.check(f"{name}: ended with {expect_code}", code == expect_code,
                   f"{code}: {output[-600:]}" if code != expect_code else f"{seconds:.1f} s")
        after = self.logs_of(program)
        new = sorted(after - logs)
        if not log:
            flow.check(f"{name}: no log (it ends before its window)", not new and self.crash_files() == crashes, ", ".join(new))
            return output, "", None
        self.count[program] += 1
        if not flow.check(f"{name}: one new log, {program}-<date>_<time>.log", len(new) == 1, ", ".join(new) or "none"):
            return output, "", None
        name_of_log = new[0]
        self.first_log.setdefault(program, name_of_log)
        text = open(os.path.join(self.logs, name_of_log), encoding="utf-8", errors="replace").read()
        if program == "stat-fick":
            self.game_logs[name_of_log] = text
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
        label = "game" if program == "stat-fick" else "editor"
        if self.count[program] == RUNS - 1:
            flow.check(f"{label}: after {RUNS - 1} runs {RUNS - 1} logs, the first one there",
                       len(after) == RUNS - 1 and self.first_log.get(program) in after, len(after))
        elif self.count[program] == RUNS:
            flow.check(f"{label}: after {RUNS} runs {KEPT} logs kept, the oldest gone, the newest there",
                       len(after) == KEPT and self.first_log.get(program) not in after and name_of_log in after,
                       f"{len(after)} kept; the first {self.first_log.get(program)}")
            flow.check(f"{label}: at its {RUNS}th run the crash files and the other program's logs untouched",
                       self.crash_files() == crashes and self.logs_of(other) == others)
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


def judged(flow, name, path, what, test, detail=lambda: ""):
    """An expectation on a picture judged by a function of its path (its regions' lettering, its stripes)."""
    if not os.path.isfile(path):
        return flow.check(f"{name}: {what}", False, f"no picture at {path}")
    return flow.check(f"{name}: {what}", test(path), detail())


def picture(flow, name, path, what, test):
    found, size = colours(path)
    if not found:
        return flow.check(f"{name}: {what}", False, f"no picture at {path}")
    return flow.check(f"{name}: {what}", test(found), f"{size[0]} x {size[1]}, most {found[0][1]} {found[0][0]}")


def light_text(found):
    """The share of light lettering (the buttons' and the credits' in the dark theme)."""
    return share(found, lambda c: min(c) > 180)


def window_over_start(found):
    """A window of the game's over the start screen (the settings screen, About): the page darkened by its backdrop over
    half the picture, the dialog's ground over a twelfth (the start screen alone: 11 % that dark)."""
    return share(found, lambda c: max(c) <= DARKENED) > 0.5 and share(found, lambda c: near(c, ABOUT_GROUND)) > 0.08


def stripes(path, colour):
    """How many mod rows a picture shows striped in `colour`: the bars of that colour exactly, at least STRIPE_RUN px tall,
    in the column holding the most of it."""
    if not os.path.isfile(path):
        return 0
    image = Image.open(path).convert("RGB")
    bands = image.split()
    mask = bands[0].point(lambda v: 255 if v == colour[0] else 0)
    for band, value in zip(bands[1:], colour[1:]):
        mask = Image.composite(band.point(lambda v, value=value: 255 if v == value else 0), Image.new("L", image.size, 0), mask)
    columns = list(mask.resize((image.width, 1), Image.Resampling.BOX).getdata())
    x = max(range(image.width), key=lambda i: columns[i])
    if columns[x] == 0:
        return 0
    column = list(mask.crop((x, 0, x + 1, image.height)).getdata())
    rows, run = 0, 0
    for value in column + [0]:
        if value:
            run += 1
            continue
        rows += run >= STRIPE_RUN
        run = 0
    return rows


def text_bands(image, box, bright=90):
    """The lines of lettering in `box` of a picture (left, top, right, bottom): each (top, bottom, leftmost, rightmost,
    peak brightness, warm), a band of rows holding pixels whose every channel is over `bright`, top to bottom. `warm`:
    the share of its lettering whose red is over its blue by 40 (gold or ember ink, not the light grey or cream)."""
    left, top, right, bottom = box
    region = image.crop(box)
    pixels = region.load()
    rows = []
    for y in range(region.height):
        lit = [(x, pixels[x, y]) for x in range(region.width) if min(pixels[x, y]) > bright]
        rows.append(lit)
    bands, start = [], None
    for y, lit in enumerate(rows + [[]]):
        if lit and start is None:
            start = y
        elif not lit and start is not None:
            letters = [p for row in rows[start:y] for p in row]
            xs = [x for x, _ in letters]
            peak = max(min(c) for _, c in letters)
            warm = sum(1 for _, c in letters if c[0] - c[2] > 40) / len(letters)
            bands.append((top + start, top + y, left + min(xs), left + max(xs), peak, warm))
            start = None
    return bands


def start_dialog(path):
    """The start screen's dialog in a picture of the game's window: its buttons' labels (the first six bands of lettering
    a line high and centred in the middle of the picture, in their order: Continue, New character, Load, Settings, Quit,
    About; the header's lines start further left, a button's edges are under a line high) and the lines under them (the
    left-out line); None without the six labels."""
    if not os.path.isfile(path):
        return None
    image = Image.open(path).convert("RGB")
    middle = image.width // 2
    bands = text_bands(image, (middle - 260, image.height // 2 - 420, middle + 260, image.height // 2 + 420))
    labels = [b for b in bands if b[2] >= middle - 120 and b[3] <= middle + 120 and 5 <= b[1] - b[0] <= 20]
    if len(labels) < 6:
        return None
    labels = labels[:6]
    under = [b for b in bands if b[0] > labels[-1][1] and b[1] - b[0] >= 5]
    return {"labels": labels, "under": under}


def start_screen(path):
    """The start screen in the first start's dark theme: the stone page most, the dialog's six buttons in the middle, no
    top bar's lettering along the top."""
    found, _ = colours(path)
    if not found or not near(found[0][1], STONE):
        return False
    image = Image.open(path).convert("RGB")
    return start_dialog(path) is not None and not text_bands(image, (0, 0, image.width, 120), bright=180)


def character_page(path):
    """The character's page: the stone page most, the top bar's lettering along the top ("Stat Fick", the clock, Menu), no
    dialog in the middle."""
    found, _ = colours(path)
    if not found or not near(found[0][1], STONE):
        return False
    image = Image.open(path).convert("RGB")
    return bool(text_bands(image, (0, 0, image.width, 120), bright=180)) and start_dialog(path) is None


def lettering(image, band):
    """A band's lettering colour: the most common of its brightest pixels (within 12 levels of its peak: the ink's core,
    the LCD text's coloured fringes aside)."""
    top, bottom, left, right, peak = band[:5]
    found = [c for c in image.crop((left, top, right + 1, bottom)).getdata() if min(c) >= peak - 12]
    return max(set(found), key=found.count) if found else None


def look(path):
    """The start screen's look in a picture, as the user approved it in its captures: the dialog's ground (the most common
    colour around its buttons), each button label's lettering (Continue first) and the lettering of each line under them
    (the left-out line); None without the start screen's dialog."""
    dialog = start_dialog(path)
    if dialog is None:
        return None
    image = Image.open(path).convert("RGB")
    middle, labels = image.width // 2, dialog["labels"]
    box = image.crop((middle - 230, labels[0][0] - 30, middle + 230, labels[-1][1] + 30))
    ground = max(box.getcolors(box.width * box.height))[1]
    return {"ground": ground, "labels": [lettering(image, b) for b in labels], "under": [lettering(image, b) for b in dialog["under"]]}


GOLDEN = os.path.join(ENGINE, "Tests", "Statventure.Tests", "Golden")
_approved = {}


def approved():
    """The approved look's colours, read from Chromium's captures of the start screen the user approved (GM1 step 9; the
    game's screen fixtures, the dark theme at 1): the dialog's ground and its labels' lettering (start-first-dark),
    Continue's lettering greyed (start-first-dark) and enabled (start-continue-dark), the left-out line's ink
    (start-left-out-one-dark)."""
    if not _approved:
        first, enabled, left_out = (look(os.path.join(GOLDEN, f"game-screen-start-{name}-dark@1.png")) for name in ("first", "continue", "left-out-one"))
        _approved.update(ground=first["ground"], label=first["labels"][1], greyed=first["labels"][0], enabled=enabled["labels"][0],
                         left_out=left_out["under"][0])
    return _approved


def grounds(path):
    """A window's picture's two grounds: the dialog's (the most common colour in a 400 × 600 px box at the middle, the
    darkest left out: a slider's track, a field) and the page's behind the window's backdrop (the most common colour of an
    80 px strip down the left edge)."""
    image = Image.open(path).convert("RGB")
    middle = image.crop((image.width // 2 - 200, image.height // 2 - 300, image.width // 2 + 200, image.height // 2 + 300))
    edge = image.crop((0, 0, 80, image.height))
    dialog = [(count, colour) for count, colour in middle.getcolors(middle.width * middle.height) if max(colour) > DARKENED]
    return max(dialog)[1], max(edge.getcolors(edge.width * edge.height))[1]


def order(colour):
    """A colour's channels' order (red against green, green against blue, red against blue): a dark ground within M12's 6
    levels of another can still have red and blue the other way round."""
    r, g, b = colour
    return ((r > g) - (r < g), (g > b) - (g < b), (r > b) - (r < b))


_approved_windows = {}


def approved_windows():
    """The approved windows' grounds, read from Chromium's captures of the fixtures the user approved (the dark theme at
    1): the settings window's dialog and the darkened page behind it (`settings-first-start-dark`, GM1 step 10), About's
    dialog (`about-top-dark`, step 9; its window is the fixture's width, so the page behind it is the settings one's)."""
    if not _approved_windows:
        settings = grounds(os.path.join(GOLDEN, "game-screen-settings-first-start-dark@1.png"))
        about = grounds(os.path.join(GOLDEN, "game-screen-about-top-dark@1.png"))
        _approved_windows.update(settings=settings[0], about=about[0], behind=settings[1])
    return _approved_windows


def window_look(flow, name, path, window):
    """A window of the game's (`window`: "settings" or "about") in the look approved for it: its dialog's ground and the
    page behind its backdrop, each within M12's 6 levels of Chromium's and the page's channels in the same order."""
    if not os.path.isfile(path):
        return flow.check(f"{name}: the window's grounds", False, f"no picture at {path}")
    want, (dialog, behind) = approved_windows(), grounds(path)
    flow.check(f"{name}: the window's dialog on the approved ground {want[window]}", near(dialog, want[window]), f"{dialog}")
    flow.check(f"{name}: the page behind the window darkened as approved {want['behind']} (its channels in that order)",
               near(behind, want["behind"]) and order(behind) == order(want["behind"]), f"{behind}")


def approved_look(flow, name, path, continued, left_out):
    """The start screen in the look the user approved (each colour within M12's 6 levels of Chromium's capture): the
    dialog's ground, the labels' lettering, Continue's lettering greyed or enabled (`continued`), the left-out line's ink
    (`left_out`: whether the line is there)."""
    want, seen = approved(), look(path)
    if not flow.check(f"{name}: the start screen's dialog found (its six buttons)", seen is not None):
        return
    flow.check(f"{name}: the dialog's ground the approved one {want['ground']}", near(seen["ground"], want["ground"]), f"{seen['ground']}")
    flow.check(f"{name}: the buttons' labels (New character to About) in the approved lettering {want['label']}",
               all(c is not None and near(c, want["label"]) for c in seen["labels"][1:]), f"{seen['labels'][1:]}")
    state, colour = ("enabled", want["enabled"]) if continued else ("greyed", want["greyed"])
    flow.check(f"{name}: Continue drawn {state} (its lettering the approved {colour})", seen["labels"][0] is not None and near(seen["labels"][0], colour),
               f"{seen['labels'][0]}")
    if left_out:
        flow.check(f"{name}: the left-out line under the buttons in the approved ink {want['left_out']}",
                   bool(seen["under"]) and all(c is not None and near(c, want["left_out"]) for c in seen["under"]), f"{seen['under']}")
    else:
        flow.check(f"{name}: no line under the buttons", not seen["under"], f"{seen['under']}")


def settings_picture(flow, name, path, rows):
    """The settings window over the start screen and its mod rows by their stripes: `rows` (loaded, off, left out)."""
    picture(flow, name, path, "the settings window over the start screen (the page darkened, the dialog's ground)", window_over_start)
    window_look(flow, name, path, "settings")
    found = tuple(stripes(path, colour) for colour in (LOADED_STRIPE, OFF_STRIPE, LEFT_OUT_STRIPE))
    words = ", ".join(f"{n} {state}" for n, state in zip(rows, ("loaded", "off", "left out")) if n) or "no row"
    flow.check(f"{name}: the mod list's rows by their stripes: {words}", found == rows, f"{found} (loaded, off, left out)")


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


def one_test(flow, name, project, test, build, log, env):
    """One test of a test project built in `build` (the packages' version), run with the flow's variables."""
    code = dotnet(["test", project, "--no-build", "-c", "Debug", "--artifacts-path", build, "-v", "quiet", "-nologo",
                   "--filter", f"FullyQualifiedName={test}"], log, env)
    text = open(log, encoding="utf-8", errors="replace").read()
    return flow.check(name, code == 0 and re.search(r"Passed!\s+-\s+Failed:\s+0, Passed:\s+1, Skipped:\s+0, Total:\s+1", text), text.strip()[-600:])


def start_screen_lines(flow, name, log, continued, left_out):
    """The start screen's lines (GM1-P21): shown, then Continue's state (`continued`: the slot, or None for greyed), then
    the left-out line (`left_out`'s text, or none)."""
    game = messages(log, "Game")
    at = game.index("the start screen shown") if "the start screen shown" in game else -1
    want = "Continue greyed" if continued is None else f"Continue opens {continued}"
    flow.check(f"{name}: the start screen shown, " + ("Continue greyed" if continued is None else f"Continue enabled, opening {continued}") + " (the log)",
               at >= 0 and game[at + 1:at + 2] == [want], " | ".join(game[at:at + 3]) if at >= 0 else "no start screen")
    said = [m for m in game if m.startswith("the left-out line: ")]
    if left_out is None:
        flow.check(f"{name}: no mod left out, no left-out line (the log)", not said and not any(l == "WARN" and c == "Mods" for l, c, _ in lines_of(log)), " | ".join(said))
    else:
        flow.check(f"{name}: the start screen's line names the mods left out (the log)",
                   said == [f"the left-out line: {left_out}"] and game.index(said[0]) == at + 2, " | ".join(said) or "none")


def settings_lines(flow, name, log, values, mods):
    """The settings screen's lines (GM1-P21): opened, its values, then its mod list (`mods`: the rows' lines, or none)."""
    settings = messages(log, "Settings")
    at = settings.index("the settings screen opened") if "the settings screen opened" in settings else -1
    flow.check(f"{name}: the settings screen opened with " + ("the first start's values" if values == FIRST_VALUES else "the values kept") + " (the log)",
               at >= 0 and settings[at + 1:at + 2] == [values], " | ".join(settings[at:at + 2]) if at >= 0 else "not opened")
    want = ["the mod list: none"] if not mods else [f"mod {row}" for row in mods]
    flow.check(f"{name}: the mod list " + ("empty" if not mods else "showing " + ", ".join(row.split(":")[0] for row in mods)) + " (the log)",
               at >= 0 and settings[at + 2:] == want, " | ".join(settings[at + 2:]) if at >= 0 else "")


def mods_lines(flow, name, log, expected, mounted):
    """The mod check's lines at the start (each mod's, in mounting order, its level) and the file system's [Data] mod line
    after them for each mod `mounted`, and none for the others."""
    found = [(level, f"[Mods] {message}") for level, category, message in lines_of(log) if category == "Mods"]
    flow.check(f"{name}: each mod checked in mounting order: " + "; ".join(f"{text.split(':')[0][7:]} {'WARN' if level == 'WARN' else ''}".strip() for level, text in expected) + " (the log)",
               found == expected, " | ".join(f"{l} {t}" for l, t in found))
    parsed = lines_of(log)
    data = [message[4:] for _, category, message in parsed if category == "Data" and message.startswith("mod ")]
    last_mods = max((i for i, (_, c, _) in enumerate(parsed) if c == "Mods"), default=-1)
    first_data = min((i for i, (_, c, m) in enumerate(parsed) if c == "Data" and m.startswith("mod ")), default=len(parsed))
    flow.check(f"{name}: [Data] mod only for the mods mounted, after the check's lines (the log)",
               data == list(mounted) and last_mods < first_data, " | ".join(data) or "none")


def game_runs(flow, p):
    """The game's first runs: a first start, the settings screen, About, the two crashes, a second start (runs 1 to 6)."""
    output, log, _ = p.start("game first start", "stat-fick", ["--capture-splash", p.capture("game-splash"), "--capture", p.capture("game-start")])
    picture(flow, "game first start", p.capture("game-splash"), "the splash's first frame: the BRUTAL logo on the dark ground",
            lambda f: near(f[0][1], GROUND) and share(f, lambda c: near(c, BRUTAL_YELLOW, 12)) > 0.001
            and share(f, lambda c: min(c) > 240) > 0.003)
    judged(flow, "game first start", p.capture("game-start"), "--capture after the splash: the start screen on the stone page (its six buttons, no top bar)", start_screen)
    approved_look(flow, "game first start", p.capture("game-start"), continued=False, left_out=False)
    splash, captured = log.find("the splash shown (2 s)"), log.rfind("[Application] captured")
    flow.check("game first start: the start screen captured after the splash's 2 s", 0 <= splash < captured)
    start_screen_lines(flow, "game first start", log, None, None)
    flow.check("game first start: no mods, nothing checked or mounted (the log)",
               not messages(log, "Mods") and not [m for m in messages(log, "Data") if m.startswith("mod ")])
    flow.check("game first start: no theme started (no [Audio] theme line)", not [m for m in messages(log, "Audio") if m.startswith("theme ")])
    gpu = re.search(r"\[Graphics\] GPU (.+)", log)
    flow.fact(f"the GPU: {gpu.group(1) if gpu else 'not logged'}")

    output, log, _ = p.start("game settings", "stat-fick", ["--open", "settings", "--capture", p.capture("game-settings")])
    settings_lines(flow, "game settings", log, FIRST_VALUES, [])
    settings_picture(flow, "game settings", p.capture("game-settings"), (0, 0, 0))

    output, log, _ = p.start("game about", "stat-fick", ["--about", "--capture", p.capture("game-about")])
    flow.check("game about: the credits view opened (--about)", "[Game] the credits view opened" in log)
    # GM1 step 9: About is a window of the game's over the start screen, in the first start's dark theme: its dialog's
    # ground over a fifth of the picture (28 % on 2026-11-14; the start screen alone 7.6 %) and the credits' light
    # lettering (236, 224, 196) over 0.15 % (0.37 %; the start screen alone 0.014 %).
    picture(flow, "game about", p.capture("game-about"), "About's window: the credits' light lettering on its dark ground",
            lambda f: window_over_start(f) and share(f, lambda c: near(c, ABOUT_GROUND)) > 0.2 and light_text(f) > 0.0015)
    window_look(flow, "game about", p.capture("game-about"), "about")

    for kind, where, message in (("--crash", "on the main thread", "the game crashed on purpose (--crash)"),
                                 ("--crash-thread", 'on the thread "Game crash"',
                                  "the game crashed on purpose on a thread of its own (--crash-thread)")):
        name = "game " + kind[2:]
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

    second_start(flow, p)


def second_start(flow, p):
    """A second start on the same per-user folder while the first runs (GM1-P14): it ends with 0 saying the first was
    asked forward, and the first logs the request and ends as it would."""
    before = (p.logs_of("stat-fick"), p.crash_files(), p.logs_of("statventure-editor"))
    first = launch([p.game, "--capture", p.capture("game-first-of-two"), "--frames", str(FIRST_FRAMES)], p.env, p.folder("game first of two"))
    # The first's start screen shown (its log says so), then the second started.
    shown, deadline = False, time.monotonic() + RUN_SECONDS
    while not shown and time.monotonic() < deadline and not isinstance(first, str) and first[0].poll() is None:
        for name in p.logs_of("stat-fick") - before[0]:
            shown = shown or "[Game] the start screen shown" in open(os.path.join(p.logs, name), encoding="utf-8", errors="replace").read()
        time.sleep(0.05)
    flow.check("game first of two: its start screen shown before the second starts (the log)", shown)
    second_before = (p.logs_of("stat-fick"), p.crash_files(), p.logs_of("statventure-editor"))
    output, _, _ = p.start("game second of two", "stat-fick", ["--capture", p.capture("game-second-of-two")], log=False, before=second_before,
                           handle=launch([p.game, "--capture", p.capture("game-second-of-two")], p.env, p.folder("game second of two")))
    flow.check("game second of two: it said the first was asked to come forward", output.strip() == FORWARDED, output.strip()[-300:])
    output, log, _ = p.start("game first of two", "stat-fick", [], handle=first, before=before)
    asked = [m for m in messages(log, "Game") if m.startswith("another start asked for the window: ")]
    flow.check("game first of two: it took the request, its window brought forward (the log)",
               len(asked) == 1 and "focused" in asked[0] and log.find(asked[0]) > log.find("the start screen shown"), " | ".join(asked) or "none")
    flow.check("game first of two: it ran on and captured after the request (the log)",
               log.find("[Application] captured") > log.find("another start asked for the window"))


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


def editor_mod(flow, p, build, logs):
    """The mod made by the editor in-process against the unpacked game (GameFolderModTests, built with the packages'
    version), then checked and mounted by the packaged game and shown in its mod list."""
    env = dict(os.environ, STATVENTURE_END_TO_END_GAME=p.game_folder, STATVENTURE_END_TO_END_VERSION=flow.version,
               STATVENTURE_END_TO_END_MODS=p.mods, STATVENTURE_END_TO_END_ABOUT=p.capture("editor-about"))
    one_test(flow, "the mod: the editor's flow against the unpacked game passed (GameFolderModTests, one test)",
             "Tests/Statventure.Tests", TEST, build, os.path.join(logs, "editor-test.log"), env)
    mod = os.path.join(p.mods, GOOD_MOD)
    flow.check("the mod: ember-rite.pak in the per-user folder's mods/", os.path.isfile(mod))
    picture(flow, "the editor's About (in-process)", p.capture("editor-about"), "the credits dialog over the editor",
            lambda f: len(f) > 100 and share(f, lambda c: min(c) > 200) > 0.002)

    output, log, _ = p.start("game with the mod", "stat-fick", ["--capture", p.capture("game-mod")])
    mods_lines(flow, "game with the mod", log, [("INFO", LOADED)], [mod])
    start_screen_lines(flow, "game with the mod", log, None, None)
    judged(flow, "game with the mod", p.capture("game-mod"), "the start screen drawn", start_screen)
    approved_look(flow, "game with the mod", p.capture("game-mod"), continued=False, left_out=False)

    output, log, _ = p.start("game with the mod: settings", "stat-fick", ["--open", "settings", "--capture", p.capture("game-mod-settings")])
    settings_lines(flow, "game with the mod: settings", log, FIRST_VALUES, ["ember-rite.pak: Loaded"])
    settings_picture(flow, "game with the mod: settings", p.capture("game-mod-settings"), (1, 0, 0))


def slot_times(p, slot):
    """A slot's save and backup files' modification times (ns), None when missing."""
    return tuple(os.stat(f).st_mtime_ns if os.path.exists(f) else None for f in (os.path.join(p.saves, slot + ".dat"), os.path.join(p.saves, slot + ".dat.bak")))


def player_runs(flow, p, build, logs):
    """What a player's own runs leave (the game's in-process test), then the packaged game on it: the broken mods left out
    and named, Continue enabled and run, the slot written back on quit, the character file loaded, a mod switched off."""
    env = dict(os.environ, STATVENTURE_END_TO_END_PLAYER=p.player, STATVENTURE_END_TO_END_FILES=p.files, STATVENTURE_END_TO_END_VERSION=flow.version)
    one_test(flow, "the player's folder: a character, a setting, a character file and a mod made through the game's parts (PlayerFolderTests, one test)",
             "Tests/Statventure.Game.Tests", GAME_TEST, build, os.path.join(logs, "game-test.log"), env)
    character_file = os.path.join(p.files, CHARACTER_FILE)
    flow.check("the player's folder: the slot saved with its backup, the character file in the run's own folder",
               all(slot_times(p, SLOT)) and os.path.isfile(character_file) and os.path.isfile(os.path.join(p.files, ODD_MOD)))

    # Two broken mods beside the good one: a kind the game doesn't know (the test's), a byte of an index flipped.
    shutil.copyfile(os.path.join(p.files, ODD_MOD), os.path.join(p.mods, ODD_MOD))
    broken = bytearray(open(os.path.join(p.mods, GOOD_MOD), "rb").read())
    broken[-1] ^= 0xFF   # the index ends the pack: its last byte is the last entry's hash, so the index's own hash fails
    open(os.path.join(p.mods, BROKEN_MOD), "wb").write(bytes(broken))
    checked = [("WARN", f"[Mods] {BROKEN_MOD}: left out: {BROKEN_PROBLEM}"), ("INFO", LOADED), ("WARN", f"[Mods] {ODD_MOD}: left out: {ODD_PROBLEM}")]
    rows = [f"{BROKEN_MOD}: Left out: {BROKEN_PROBLEM}", f"{GOOD_MOD}: Loaded", f"{ODD_MOD}: Left out: {ODD_PROBLEM}"]
    good = os.path.join(p.mods, GOOD_MOD)

    saved = slot_times(p, SLOT)
    output, log, _ = p.start("game with a character and broken mods", "stat-fick", ["--capture", p.capture("game-continue-enabled")])
    mods_lines(flow, "game with a character and broken mods", log, checked, [good])
    start_screen_lines(flow, "game with a character and broken mods", log, SLOT, LEFT_OUT_LINE)
    enabled = p.capture("game-continue-enabled")
    judged(flow, "game with a character and broken mods", enabled, "the start screen drawn", start_screen)
    approved_look(flow, "game with a character and broken mods", enabled, continued=True, left_out=True)
    flow.check("game with a character and broken mods: the slot not written (nothing opened)", slot_times(p, SLOT) == saved)

    output, log, _ = p.start("game settings with broken mods", "stat-fick", ["--open", "settings", "--capture", p.capture("game-broken-settings")])
    settings_lines(flow, "game settings with broken mods", log, CHANGED_VALUES, rows)
    settings_picture(flow, "game settings with broken mods", p.capture("game-broken-settings"), (1, 0, 2))

    saved = slot_times(p, SLOT)
    output, log, _ = p.start("game --run continue", "stat-fick", ["--run", "continue", "--capture", p.capture("game-continued")])
    session = messages(log, "Session")
    flow.check(f"game --run continue: Continue opened {SLOT} (the log)",
               session == [f"opened {SLOT}"] and log.find(f"Continue opens {SLOT}") < log.find(f"opened {SLOT}") and "continue: not possible" not in log,
               " | ".join(session) or "nothing opened")
    judged(flow, "game --run continue", p.capture("game-continued"), "the character's page (its top bar), not the start screen", character_page)
    now = slot_times(p, SLOT)
    flow.check(f"game --run continue: the slot written back on quit ({SLOT}.dat newer, the save before kept as its backup)",
               None not in now and now[0] > saved[0] and now[1] == saved[0], f"{saved} -> {now}")

    output, log, _ = p.start("game --run load-character", "stat-fick", ["--run", f"load-character={character_file}", "--capture", p.capture("game-loaded")])
    session = messages(log, "Session")
    flow.check(f"game --run load-character: the character file loaded into a new slot, {SLOT}-2 (the log)",
               session == [f"{character_file} loaded into {SLOT}-2"] and f"[Ui] status (Info): Loaded {CHARACTER_FILE}" in log, " | ".join(session) or "nothing loaded")
    settings = json.load(open(os.path.join(p.player, "settings.json"), encoding="utf-8"))
    flow.check(f"game --run load-character: {SLOT}-2 saved and the last character, the file's folder the first recent place (settings.json)",
               all(slot_times(p, SLOT + "-2")) and settings.get("lastCharacter") == f"{SLOT}-2" and (settings.get("recentPlaces") or [None])[0] == p.files,
               f"lastCharacter {settings.get('lastCharacter')}, recentPlaces {settings.get('recentPlaces')}")
    judged(flow, "game --run load-character", p.capture("game-loaded"), "the character's page (its top bar)", character_page)

    # The good mod switched off in settings.json (Q3; what the mod list's switch writes), at the next start.
    settings = json.load(open(os.path.join(p.player, "settings.json"), encoding="utf-8"))
    settings["mods"] = {GOOD_MOD: {"on": False}}
    with open(os.path.join(p.player, "settings.json"), "w", encoding="utf-8") as f:
        json.dump(settings, f, indent=2)
    output, log, _ = p.start("game with the mod switched off", "stat-fick", ["--open", "settings", "--capture", p.capture("game-mod-off")])
    mods_lines(flow, "game with the mod switched off", log, [checked[0], ("INFO", f"[Mods] {GOOD_MOD}: off"), checked[2]], [])
    start_screen_lines(flow, "game with the mod switched off", log, f"{SLOT}-2", LEFT_OUT_LINE)
    settings_lines(flow, "game with the mod switched off", log, CHANGED_VALUES, [rows[0], f"{GOOD_MOD}: Off", rows[2]])
    settings_picture(flow, "game with the mod switched off", p.capture("game-mod-off"), (0, 1, 2))


def rotation(flow, p):
    """Each program started until it has run RUNS times (the game has by now): at its 10th and 11th run `start` checks
    the logs kept."""
    for program, arguments in (("stat-fick", ["--capture", p.capture("game-again")]),
                               ("statventure-editor", ["--player-folder", p.player, "--capture", p.capture("editor-again")])):
        label = "game" if program == "stat-fick" else "editor"
        while p.count[program] < RUNS:
            p.start(f"{label} again ({p.count[program] + 1})", program, arguments)


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

    # The in-process parts, built from this tree with the packages' version (their own artifacts, the repo's builds
    # untouched): the editor's, so the editor and the unpacked game's library are of one version as a player's are; the
    # game's, so the program that makes the player's folder is the packages'.
    build = os.path.join(work, "build")
    version_property = [] if version == "dev" else [f"-p:Version={version}"]
    for project, what in (("Tests/Statventure.Tests", "the editor's"), ("Tests/Statventure.Game.Tests", "the game's")):
        built = dotnet(["build", project, "-c", "Debug", *version_property, "--artifacts-path", build, "-v", "quiet", "-nologo"],
                       os.path.join(logs, os.path.basename(project) + "-build.log"))
        flow.check(f"{what} in-process flow built with the version {version}", built == 0)

    # Each part in turn; one that stops (an exception of the flow's own) is a failure, and the rest still runs.
    for part, call in (("the game", lambda: game_runs(flow, p)), ("the editor", lambda: editor_runs(flow, p, other_game, other)),
                       ("the editor's mod", lambda: editor_mod(flow, p, build, logs)),
                       ("the player's runs", lambda: player_runs(flow, p, build, logs)),
                       ("the logs kept", lambda: rotation(flow, p))):
        try:
            call()
        except Exception as error:  # noqa: BLE001 (any of the flow's own problems is reported, not raised)
            flow.check(f"{part}: the flow went through", False, f"{type(error).__name__}: {error}")

    themes = {name: [m for m in messages(text, "Audio") if m.startswith("theme ")] for name, text in p.game_logs.items()}
    flow.check(f"no game run started a theme (no [Audio] theme line in its {len(themes)} logs; Q15)",
               len(themes) >= RUNS and not any(themes.values()), "; ".join(f"{n}: {t}" for n, t in themes.items() if t))
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

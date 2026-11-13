"""release.sh --self-test (Docs/Packaging.md, "How it works", step 7): the release script's rules, each in a scratch
git repository with a bare one as its GitHub. The repository holds copies of this tree's release.sh, env.sh and
release-text.md, and stand-ins for publish.sh, check-packages.sh and test.sh; a stand-in gh is first on PATH (with an
empty gh configuration, so even the real one would be logged out). Every stand-in records its call (arguments and
folder; gh's release create also the text it was given and which archives were there). Nothing reaches GitHub; nothing
is built (one test answers git from a scratch HTTP server on 127.0.0.1). Prints each test as ok or FAILED with what
went wrong; ends with 1 if any failed.
    python3 -I release_test.py [<test name> ...]"""
import json
import os
import pty
import re
import select
import shutil
import signal
import subprocess
import sys
import tempfile
import time

ENGINE = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SCRIPTS = os.path.join(ENGINE, "Tools", "Scripts")
TEMPLATE = os.path.join(ENGINE, "Tools", "Packaging", "release-text.md")
PACKAGES = ["stat-fick-linux", "stat-fick-windows", "statventure-editor-linux", "statventure-editor-windows"]
LIST_CALL = ["release", "list", "--limit", "1000", "--json", "tagName", "--jq", ".[].tagName"]
# A day that isn't the run's (a version from the day the script runs would show), a time near midnight UTC.
DAY = "2026-10-21T21:30:00+00:00"
VERSION_DAY = "2026.10.21"
# What the release's text must say (Q12: what a readme would have; Q13, Q14, Q15; the system needs from step 3).
REQUIRED = [
    "The game is being ported to the Statventure engine", "nothing to play yet",
    "the game and its editor", "for making mods",
    "./stat-fick-{version}/stat-fick", "`statventure-editor` in `statventure-editor-{version}`",
    "tar -xzf stat-fick-{version}-linux.tar.gz", "Extract All", "run `Stat Fick.exe` in its folder",
    "the editor likewise, `Statventure Editor.exe`", "holding `stat-fick` or `Stat Fick.exe` and `packs/`",
    "unsigned", "SmartScreen", "**More info**", "**Run anyway**",
    "no version details", "in the folder's name, in the editor's title and in the logs",
    '"Where is Stat Fick?"', "on its first start", "It remembers the folder", "only the game of its own version",
    "`~/.local/share/stat-fick`", "`$XDG_DATA_HOME/stat-fick`", "`%APPDATA%\\Stat Fick`",
    "`saves/`", "`mods/`", "`logs/`", "`crash-….txt`", "`settings.json`", "`editor/`",
    "Vulkan 1.3", "x86-64", "glibc 2.34", "GCC 12's libstdc++", "Ubuntu 22.04", "Debian 12", "Fedora 36",
    "not RHEL 9", "Windows 10 or 11, 64-bit", "The Visual C++ runtime comes with the programs",
    "`stat-fick-{version}`", "`statventure-editor-{version}`", "unpacks beside the old one",
    "Built with BRUTAL", "`NOTICE.txt` and `licenses/`",
]

STAND_IN = """#!{python} -I
import json, os, sys
name = {name!r}
args = sys.argv[1:]
record = {{"tool": name, "args": args, "cwd": os.getcwd()}}
env = os.environ
fail = env.get("RELEASE_TEST_FAIL", "").split(",")
code = 0
if name == "gh":
    if args == {list_call!r}:
        if "gh-list" in fail:
            print("gh: not logged in", file=sys.stderr)
            code = 1
        else:
            for tag in env.get("RELEASE_TEST_RELEASES", "").split(","):
                if tag:
                    print(tag)
    elif args[:2] == ["release", "create"]:
        notes = args[args.index("--notes-file") + 1] if "--notes-file" in args else None
        record["notes"] = open(notes, encoding="utf-8").read() if notes and os.path.isfile(notes) else None
        record["there"] = [a for a in args[2:] if os.path.isfile(a)]
        if "gh-create" in fail:
            print("gh: upload failed", file=sys.stderr)
            code = 1
        else:
            print("https://github.com/scratch/scratch/releases/tag/untagged-0123456789")
    else:
        print("gh stand-in: an unexpected call: " + " ".join(args), file=sys.stderr)
        code = 1
elif name == "publish.sh":
    version = args[args.index("--version") + 1]
    if "publish" in fail:
        code = 1
    else:
        release = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "release")
        os.makedirs(release, exist_ok=True)
        for package in {packages!r}:
            program, platform = package.rsplit("-", 1)
            archive = f"{{program}}-{{version}}-{{platform}}" + (".tar.gz" if platform == "linux" else ".zip")
            if package != env.get("RELEASE_TEST_LEAVE_OUT"):
                open(os.path.join(release, archive), "w").write("stand-in " + archive)
elif name == "check-packages.sh":
    code = 1 if "check" in fail else 0
with open(env["RELEASE_TEST_LOG"], "a", encoding="utf-8") as log:
    log.write(json.dumps(record) + "\\n")
sys.exit(code)
"""


def git_env(work):
    env = {k: v for k, v in os.environ.items()
           if not k.startswith(("GIT_", "GH_", "GITHUB_")) and k not in ("SSH_ASKPASS",)}
    env.update({
        "GIT_CONFIG_GLOBAL": os.devnull, "GIT_CONFIG_NOSYSTEM": "1", "GIT_TERMINAL_PROMPT": "0",
        "GIT_AUTHOR_NAME": "Release Test", "GIT_AUTHOR_EMAIL": "release-test@example.invalid",
        "GIT_COMMITTER_NAME": "Release Test", "GIT_COMMITTER_EMAIL": "release-test@example.invalid",
        "GH_CONFIG_DIR": os.path.join(work, "gh-config"), "STATVENTURE_CACHE": os.path.join(work, "cache"),
        "LC_ALL": "C", "TZ": "Pacific/Kiritimati",  # UTC+14: a local date is a day off the UTC one
    })
    return env


class Scratch:
    """A scratch repository (`repo`) with the release script's files committed, pushed to a bare one (`github`) as
    its main; `commit()` adds commits at given dates; the stand-ins record into `log`."""

    def __init__(self, work, template=None):
        self.work = work
        self.github = os.path.join(work, "github.git")
        self.repo = os.path.join(work, "repo")
        self.log = os.path.join(work, "calls.jsonl")
        self.bin = os.path.join(work, "bin")
        self.env = git_env(work)
        self.env["PATH"] = self.bin + os.pathsep + os.environ.get("PATH", "")
        self.env["RELEASE_TEST_LOG"] = self.log
        os.makedirs(os.path.join(work, "gh-config"))
        os.makedirs(self.bin)
        self.stand_in(os.path.join(self.bin, "gh"), "gh")
        self.git("init", "-q", "--bare", "-b", "main", self.github, cwd=work)
        self.git("init", "-q", "-b", "main", self.repo, cwd=work)
        scripts = os.path.join(self.repo, "engine", "Tools", "Scripts")
        packaging = os.path.join(self.repo, "engine", "Tools", "Packaging")
        os.makedirs(scripts)
        os.makedirs(packaging)
        for name in ("release.sh", "env.sh"):
            shutil.copy2(os.path.join(SCRIPTS, name), scripts)
        if template is None:
            shutil.copy2(TEMPLATE, packaging)
        else:
            open(os.path.join(packaging, "release-text.md"), "w", encoding="utf-8").write(template)
        for name in ("publish.sh", "check-packages.sh", "test.sh"):
            self.stand_in(os.path.join(scripts, name), name)
        open(os.path.join(self.repo, ".gitignore"), "w").write("/release/\n")
        open(os.path.join(self.repo, "readme.txt"), "w").write("scratch\n")
        self.git("remote", "add", "origin", self.github)

    def stand_in(self, path, name):
        open(path, "w").write(STAND_IN.format(python=sys.executable, name=name, list_call=LIST_CALL,
                                              packages=PACKAGES))
        os.chmod(path, 0o755)

    def git(self, *args, cwd=None, date=None, author_date=None):
        env = dict(self.env)
        if date:
            env["GIT_COMMITTER_DATE"] = date
            env["GIT_AUTHOR_DATE"] = author_date or date
        done = subprocess.run(["git", *args], cwd=cwd or self.repo, env=env, capture_output=True, text=True)
        if done.returncode != 0:
            raise RuntimeError(f"git {' '.join(args)}: {done.stderr.strip()}")
        return done.stdout.strip()

    def commit(self, date=DAY, author_date=None, push=True):
        """Commits everything (or an empty commit) at the date; pushes it to GitHub's main unless told not to."""
        self.git("add", "-A")
        self.git("commit", "-q", "--allow-empty", "-m", f"at {date}", date=date, author_date=author_date)
        if push:
            self.git("push", "-q", "origin", "main")
            self.git("fetch", "-q", "origin")
        return self.git("rev-parse", "HEAD")

    def tag_on_github(self, *names):
        head = self.git("rev-parse", "HEAD")
        for name in names:
            self.git("tag", name, head, cwd=self.github)

    def run(self, *args, releases=(), fail=(), leave_out=None):
        """Runs the repository's release.sh from another folder; returns (exit code, output, calls)."""
        if os.path.exists(self.log):
            os.remove(self.log)
        env = dict(self.env)
        env["RELEASE_TEST_RELEASES"] = ",".join(releases)
        env["RELEASE_TEST_FAIL"] = ",".join(fail)
        if leave_out:
            env["RELEASE_TEST_LEAVE_OUT"] = leave_out
        script = os.path.join(self.repo, "engine", "Tools", "Scripts", "release.sh")
        done = subprocess.run([script, *args], cwd=self.work, env=env, capture_output=True, text=True, timeout=120)
        calls = []
        if os.path.exists(self.log):
            calls = [json.loads(line) for line in open(self.log, encoding="utf-8")]
        RUN_CALLS.extend(calls)
        return done.returncode, done.stdout + done.stderr, calls


def archives(version):
    out = []
    for package in PACKAGES:
        program, platform = package.rsplit("-", 1)
        out.append(f"release/{program}-{version}-{platform}" + (".tar.gz" if platform == "linux" else ".zip"))
    return out


def filled(version, template=None):
    text = open(TEMPLATE, encoding="utf-8").read() if template is None else template
    text = text.replace("{{version}}", version)
    for package, archive in zip(PACKAGES, archives(version)):
        text = text.replace("{{" + package + "}}", archive[len("release/"):])
    return text.rstrip("\n") + "\n"


def tools(calls):
    return [(c["tool"], c["args"]) for c in calls]


def expect_release(s, code, output, calls, version, dry_run=True):
    """The problems with a run that should have made `version` (a dry run, or the draft through gh)."""
    problems = []
    if code != 0:
        problems.append(f"ended with {code}: {output.strip()[-600:]}")
        return problems
    commit = s.git("rev-parse", "HEAD")
    names = archives(version)
    expected = [("gh", LIST_CALL), ("publish.sh", ["--version", version, "--no-check"]),
                ("check-packages.sh", ["--version", version])]
    create = ["release", "create", f"v{version}", "--draft", "--target", commit, "--title", f"Stat Fick {version}",
              "--notes-file", f"release/release-text-{version}.md", *names]
    if not dry_run:
        expected.append(("gh", create))
    if tools(calls) != expected:
        problems.append(f"the calls were {tools(calls)}, not {expected}")
    if f"release: {version}, the commit {commit}" not in output:
        problems.append(f"the output doesn't name {version} and the commit: {output.strip()[:300]}")
    notes = os.path.join(s.repo, "release", f"release-text-{version}.md")
    text = open(notes, encoding="utf-8").read() if os.path.isfile(notes) else None
    if text != filled(version):
        problems.append(f"the text written isn't the template filled with {version}")
    if dry_run:
        line = ("  gh release create v{v} --draft --target {c} --title 'Stat Fick {v}' --notes-file "
                "release/release-text-{v}.md {a}").format(v=version, c=commit, a=" ".join(names))
        if line not in output.splitlines():
            problems.append(f"the dry run doesn't print the upload as {line!r}: {output.strip()[-900:]}")
        if text and text not in output:
            problems.append("the dry run doesn't print the text")
        if "untagged-0123456789" in output:
            problems.append("the dry run printed a draft's address")
    else:
        made = calls[-1]
        if made["cwd"] != s.repo:
            problems.append(f"gh ran in {made['cwd']}, not the repository's root")
        if made.get("notes") != filled(version):
            problems.append("gh wasn't given the template filled as the text")
        if made.get("there") != [f"release/release-text-{version}.md", *names]:
            problems.append(f"gh was given files that weren't there: {made.get('there')}")
        if "untagged-0123456789" not in output:
            problems.append("gh's draft address isn't printed")
    return problems


def expect_refused(code, output, calls, words):
    problems = []
    if code == 0:
        problems.append("not refused")
    if words not in output:
        problems.append(f"the refusal doesn't say {words!r}: {output.strip()[-400:]}")
    made = [c for c in calls if c["tool"] != "gh" or c["args"] != LIST_CALL]
    if made:
        problems.append(f"it went on after refusing: {tools(made)}")
    return problems


# The tests. Each gets a fresh scratch folder and returns its problems.

def test_first_release_of_a_date(work):
    """No release of the commit's date yet: .1. Tags of other dates, of other forms, and not on GitHub don't count."""
    s = Scratch(work)
    s.commit()
    s.tag_on_github("old-game-2026-10-16", "v2026.10.20.1", "v2026.10.20.2", "v2026.10.211.4", "v2026.10.2.7",
                    "v2026.10.21", "v2026.10.21.0", "v2026.10.21.05", "v2026.10.21.6-rc", "x2026.10.21.3",
                    "v2026.10.21.x", "v2026.10.21.3.1", "v2026-10-21.4", "v2026.10.21a8", "rc-v2026.10.21.4")
    s.git("tag", f"v{VERSION_DAY}.9")  # local only
    code, output, calls = s.run("--dry-run", releases=["old-game-2026-10-16", "v2026.10.20.2", "v2026.10.22.3"])
    return expect_release(s, code, output, calls, f"{VERSION_DAY}.1")


def test_second_release_of_a_date(work):
    """The date's first release tagged on GitHub: .2."""
    s = Scratch(work)
    s.commit()
    s.tag_on_github(f"v{VERSION_DAY}.1")
    code, output, calls = s.run("--dry-run", releases=[f"v{VERSION_DAY}.1"])
    return expect_release(s, code, output, calls, f"{VERSION_DAY}.2")


def test_the_highest_number_not_the_count(work):
    """.1, .9 and .10 tagged: .11 (one more than the highest, by number)."""
    s = Scratch(work)
    s.commit()
    s.tag_on_github(f"v{VERSION_DAY}.1", f"v{VERSION_DAY}.9", f"v{VERSION_DAY}.10")
    code, output, calls = s.run("--dry-run")
    return expect_release(s, code, output, calls, f"{VERSION_DAY}.11")


def test_a_draft_holds_its_number(work):
    """A draft not yet published (no tag on GitHub) still holds its number: .1 and .2 in drafts give .3."""
    s = Scratch(work)
    s.commit()
    code, output, calls = s.run("--dry-run", releases=["old-game-2026-10-16", f"v{VERSION_DAY}.2",
                                                        f"v{VERSION_DAY}.1"])
    return expect_release(s, code, output, calls, f"{VERSION_DAY}.3")


def test_tags_and_drafts_together(work):
    """A tag .3 and a draft .4: .5; a draft .2 under a tag .3: .4."""
    problems = []
    for tags, drafts, expected in (([".3"], [".4"], ".5"), ([".3"], [".2"], ".4")):
        folder = os.path.join(work, expected[1:])
        os.makedirs(folder)
        s = Scratch(folder)
        s.commit()
        s.tag_on_github(*[f"v{VERSION_DAY}{t}" for t in tags])
        code, output, calls = s.run("--dry-run", releases=[f"v{VERSION_DAY}{d}" for d in drafts])
        problems += expect_release(s, code, output, calls, f"{VERSION_DAY}{expected}")
    return problems


def test_the_commits_utc_date(work):
    """The commit's date in UTC, whatever its own time zone and the machine's (TZ is UTC+14 here)."""
    problems = []
    for date, expected in (("2026-10-22T00:30:00+01:00", "2026.10.21.1"), ("2026-10-21T22:30:00-03:00", "2026.10.22.1")):
        folder = os.path.join(work, expected)
        os.makedirs(folder)
        s = Scratch(folder)
        s.commit(date=date)
        code, output, calls = s.run("--dry-run")
        problems += expect_release(s, code, output, calls, expected)
    return problems


def test_the_committers_date(work):
    """The commit's date is its committer date (the archives' time too), not its author's."""
    s = Scratch(work)
    s.commit(date="2026-10-21T12:00:00+00:00", author_date="2026-10-19T12:00:00+00:00")
    code, output, calls = s.run("--dry-run")
    return expect_release(s, code, output, calls, "2026.10.21.1")


def test_no_zero_padding(work):
    """2026-01-05: 2026.1.5.1 (Q1), in the archives' names too."""
    s = Scratch(work)
    s.commit(date="2026-01-05T09:00:00+00:00")
    s.tag_on_github("v2026.01.05.1", "v2026.1.05.1")
    code, output, calls = s.run("--dry-run")
    return expect_release(s, code, output, calls, "2026.1.5.1")


def test_an_older_commit_of_main(work):
    """A commit GitHub's main has moved on from is still on it: released, at its own date."""
    s = Scratch(work)
    s.commit(date="2026-10-20T10:00:00+00:00")
    s.commit()
    s.git("checkout", "-q", "HEAD~1")
    code, output, calls = s.run("--dry-run")
    return expect_release(s, code, output, calls, "2026.10.20.1")


def test_ignored_files_dont_count(work):
    """A git-ignored file (release/'s old archives) leaves the tree clean."""
    s = Scratch(work)
    s.commit()
    os.makedirs(os.path.join(s.repo, "release"))
    open(os.path.join(s.repo, "release", "stat-fick-2026.10.1.1-linux.tar.gz"), "w").write("old")
    code, output, calls = s.run("--dry-run")
    return expect_release(s, code, output, calls, f"{VERSION_DAY}.1")


def test_the_draft(work):
    """Not a dry run: gh release create v<v> --draft --target <commit> --title "Stat Fick <v>" --notes-file <text>
    and the four archives, from the repository's root, after the publish and the check; gh's address printed."""
    s = Scratch(work)
    s.commit()
    s.tag_on_github(f"v{VERSION_DAY}.1")
    code, output, calls = s.run(releases=[f"v{VERSION_DAY}.1"])
    problems = expect_release(s, code, output, calls, f"{VERSION_DAY}.2", dry_run=False)
    if "would" in output:
        problems.append("a real run says it would")
    return problems


def test_refused_a_changed_file(work):
    s = Scratch(work)
    s.commit()
    open(os.path.join(s.repo, "readme.txt"), "a").write("changed\n")
    return expect_refused(*s.run("--dry-run"), "the work tree isn't clean")


def test_refused_a_staged_change(work):
    s = Scratch(work)
    s.commit()
    open(os.path.join(s.repo, "readme.txt"), "a").write("changed\n")
    s.git("add", "readme.txt")
    return expect_refused(*s.run("--dry-run"), "the work tree isn't clean")


def test_refused_an_untracked_file(work):
    s = Scratch(work)
    s.commit()
    open(os.path.join(s.repo, "engine", "new.txt"), "w").write("new\n")
    return expect_refused(*s.run(), "the work tree isn't clean")


def test_refused_a_commit_not_pushed(work):
    s = Scratch(work)
    s.commit()
    s.commit(push=False)
    return expect_refused(*s.run("--dry-run"), "isn't on GitHub's main")


def test_refused_a_commit_on_another_branch(work):
    """Pushed, but to another branch of GitHub's than main."""
    s = Scratch(work)
    s.commit()
    s.commit(push=False)
    s.git("push", "-q", "origin", "HEAD:refs/heads/feature")
    s.git("fetch", "-q", "origin")
    return expect_refused(*s.run(), "isn't on GitHub's main")


def test_refused_when_githubs_main_moved_back(work):
    """The local origin/main still holds the commit, GitHub's main no longer does: GitHub's is fetched and asked."""
    s = Scratch(work)
    s.commit()
    s.commit()
    s.git("update-ref", "refs/heads/main", "HEAD~1", cwd=s.github)
    return expect_refused(*s.run("--dry-run"), "isn't on GitHub's main")


def test_refused_without_github(work):
    s = Scratch(work)
    s.commit()
    s.git("remote", "set-url", "origin", os.path.join(work, "gone.git"))
    return expect_refused(*s.run("--dry-run"), "couldn't fetch GitHub's main")


def test_refused_without_githubs_releases(work):
    """gh can't list the releases (logged out): refused before anything is made."""
    s = Scratch(work)
    s.commit()
    return expect_refused(*s.run("--dry-run", fail=["gh-list"]), "couldn't read GitHub's releases")


def test_a_failed_publish_stops_it(work):
    s = Scratch(work)
    s.commit()
    code, output, calls = s.run(fail=["publish"])
    problems = [] if code != 0 else ["not stopped"]
    if [c["tool"] for c in calls] != ["gh", "publish.sh"]:
        problems.append(f"it went on: {tools(calls)}")
    if "publish.sh failed" not in output:
        problems.append(f"it doesn't say so: {output.strip()[-300:]}")
    return problems


def test_a_failed_check_stops_it(work):
    s = Scratch(work)
    s.commit()
    code, output, calls = s.run(fail=["check"])
    problems = [] if code != 0 else ["not stopped"]
    if [c["tool"] for c in calls] != ["gh", "publish.sh", "check-packages.sh"]:
        problems.append(f"it went on: {tools(calls)}")
    if "check-packages.sh failed" not in output:
        problems.append(f"it doesn't say so: {output.strip()[-300:]}")
    return problems


def test_an_archive_missing_stops_it(work):
    problems = []
    for package in PACKAGES:
        folder = os.path.join(work, package)
        os.makedirs(folder)
        s = Scratch(folder)
        s.commit()
        code, output, calls = s.run(leave_out=package)
        if code == 0 or any(c["args"][:2] == ["release", "create"] for c in calls):
            problems.append(f"{package} left out, not stopped: {tools(calls)}")
        if "publish.sh made no" not in output:
            problems.append(f"{package} left out, it doesn't say so: {output.strip()[-300:]}")
    return problems


def test_a_failed_upload(work):
    s = Scratch(work)
    s.commit()
    code, output, calls = s.run(fail=["gh-create"])
    problems = [] if code != 0 else ["ended with 0"]
    if "gh release create failed" not in output or f"v{VERSION_DAY}.1" not in output:
        problems.append(f"it doesn't say so: {output.strip()[-300:]}")
    return problems


def test_a_placeholder_nothing_fills(work):
    template = open(TEMPLATE, encoding="utf-8").read() + "\nAlso {{versoin}}.\n"
    s = Scratch(work, template=template)
    s.commit()
    code, output, calls = s.run()
    problems = [] if code != 0 else ["not refused"]
    if "a placeholder nothing fills: {{versoin}}" not in output:
        problems.append(f"it doesn't say so: {output.strip()[-300:]}")
    if any(c["args"][:2] == ["release", "create"] for c in calls):
        problems.append("it went on to the upload")
    return problems


def test_every_placeholder_filled(work):
    """A template using each placeholder once, on lines of their own: each line filled."""
    template = "\n".join(["{{version}}", *["{{" + p + "}}" for p in PACKAGES]]) + "\n"
    s = Scratch(work, template=template)
    s.commit()
    code, output, calls = s.run("--dry-run")
    version = f"{VERSION_DAY}.1"
    expected = "\n".join([version, *[a[len("release/"):] for a in archives(version)]]) + "\n"
    notes = os.path.join(s.repo, "release", f"release-text-{version}.md")
    text = open(notes, encoding="utf-8").read() if os.path.isfile(notes) else None
    return [] if code == 0 and text == expected else [f"ended with {code}, the text {text!r}: {output.strip()[-300:]}"]


def test_the_release_text(work):
    """The template says what a readme would have (Q12), the port under way (Q13; since GM1 step 2 the game's own
    program, no longer the sandbox standing in), SmartScreen (Q14), the missing version
    details (Q15), the player's folders, the system needs; uses only the five placeholders, each at least once."""
    problems = []
    template = open(TEMPLATE, encoding="utf-8").read()
    version = "2026.11.8.1"
    text = filled(version)
    for phrase in REQUIRED:
        phrase = phrase.replace("{version}", version)
        if phrase not in text:
            problems.append(f"the text doesn't say {phrase!r}")
    for placeholder in ["version", *PACKAGES]:
        if "{{" + placeholder + "}}" not in template:
            problems.append(f"the template doesn't use {{{{{placeholder}}}}}")
    if "{{" in text or "}}" in text:
        problems.append("the template has a placeholder of another name")
    for archive in archives(version):
        if f"`{archive[len('release/'):]}`" not in text:
            problems.append(f"the text doesn't name {archive}")
    return problems


# git's prompt for a login on the terminal (not its message that it couldn't ask: "could not read Username for").
PROMPT = re.compile(r"(?<!read )(Username|Password) for '")
# A server that answers every request with 401 and a Basic challenge: git asks for a user name.
CHALLENGE = """import http.server, sys
class Handler(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        self.send_response(401)
        self.send_header("WWW-Authenticate", 'Basic realm="scratch"')
        self.send_header("Content-Length", "0")
        self.end_headers()
    def log_message(self, *args):
        pass
server = http.server.HTTPServer(("127.0.0.1", 0), Handler)
print(server.server_address[1], flush=True)
server.serve_forever()
"""


def run_on_a_terminal(argv, env, cwd, seconds=20):
    """Runs a program with a terminal of its own (a pty as its controlling terminal); returns (exit code or None if it
    was stopped, output). Stopped once it asks for a user name or a password, or after `seconds`."""
    pid, fd = pty.fork()
    if pid == 0:
        try:
            os.chdir(cwd)
            os.execve(argv[0], argv, env)
        finally:
            os._exit(127)
    output = b""
    code = None
    deadline = time.monotonic() + seconds
    while time.monotonic() < deadline:
        ready, _, _ = select.select([fd], [], [], 0.1)
        if ready:
            try:
                data = os.read(fd, 4096)
            except OSError:
                data = b""
            output += data
            if PROMPT.search(output.decode("utf-8", "replace")):
                break
            if data:
                continue
        done, status = os.waitpid(pid, os.WNOHANG)
        if done:
            code = os.waitstatus_to_exitcode(status)
            break
    if code is None:
        try:
            os.killpg(pid, signal.SIGKILL)
        except ProcessLookupError:
            pass
        os.waitpid(pid, 0)
    os.close(fd)
    return code, output.decode("utf-8", "replace")


def test_git_asks_for_no_password(work):
    """GitHub asking for a login (a scratch server's 401) makes the fetch fail: no prompt on the terminal, no askpass
    program called (the desktop's would show a window)."""
    s = Scratch(work)
    s.commit()
    server = subprocess.Popen([sys.executable, "-I", "-c", CHALLENGE], stdout=subprocess.PIPE, text=True)
    try:
        port = server.stdout.readline().strip()
        s.git("remote", "set-url", "origin", f"http://127.0.0.1:{port}/statfick.git")
        asked = os.path.join(work, "asked")
        askpass = os.path.join(work, "askpass")
        open(askpass, "w").write(f"#!/bin/sh\necho \"$*\" >> '{asked}'\necho scratch\n")
        os.chmod(askpass, 0o755)
        env = {k: v for k, v in s.env.items() if k != "GIT_TERMINAL_PROMPT"}
        env.update({"GIT_ASKPASS": askpass, "SSH_ASKPASS": askpass, "RELEASE_TEST_RELEASES": "", "RELEASE_TEST_FAIL": ""})
        script = os.path.join(s.repo, "engine", "Tools", "Scripts", "release.sh")
        code, output = run_on_a_terminal([script, "--dry-run"], env, work)
    finally:
        server.kill()
        server.wait()
    problems = []
    if code is None:
        problems.append(f"stopped waiting: {output.strip()[-300:]!r}")
    elif code == 0:
        problems.append("not refused")
    if PROMPT.search(output):
        problems.append("git asked on the terminal")
    if os.path.exists(asked):
        problems.append("git called the askpass program: " + open(asked).read().strip())
    if code is not None and "couldn't fetch GitHub's main" not in output:
        problems.append(f"the refusal doesn't say so: {output.strip()[-300:]!r}")
    return problems


# An ssh for git (GIT_SSH_COMMAND) reaching the scratch GitHub on this machine: runs the command git asks the host to
# run (git-upload-pack '<path>'), recording each call with its SSH_ASKPASS; fails from the call RELEASE_TEST_SSH_FAIL
# on (counting from 1).
SSH = """#!/bin/sh
log="$RELEASE_TEST_SSH_LOG"
echo "SSH_ASKPASS=[${SSH_ASKPASS-}]" >> "$log"
calls=$(wc -l < "$log")
if [ -n "${RELEASE_TEST_SSH_FAIL-}" ] && [ "$calls" -ge "$RELEASE_TEST_SSH_FAIL" ]; then
  echo "ssh: Connection reset by peer" >&2
  exit 255
fi
for command; do :; done
exec sh -c "$command"
"""


def over_ssh(s, work, fail=None):
    """Points the scratch repository's origin at its GitHub through the ssh stand-in; returns the run's environment
    additions and the ssh log's path."""
    ssh = os.path.join(work, "ssh")
    open(ssh, "w").write(SSH)
    os.chmod(ssh, 0o755)
    log = os.path.join(work, "ssh.log")
    s.git("remote", "set-url", "origin", f"scratch-github:{s.github}")
    s.env.update({"GIT_SSH_COMMAND": ssh, "RELEASE_TEST_SSH_LOG": log})
    if fail:
        s.env["RELEASE_TEST_SSH_FAIL"] = str(fail)
    return log


def test_ssh_asks_for_no_password(work):
    """Over ssh (GitHub's other address form): released all the same, and every call to GitHub made with SSH_ASKPASS
    emptied (ssh's own password window on a desktop) though the machine has one set."""
    s = Scratch(work)
    s.commit()
    s.tag_on_github(f"v{VERSION_DAY}.1")
    log = over_ssh(s, work)
    s.env["SSH_ASKPASS"] = os.path.join(work, "desktop-askpass")
    code, output, calls = s.run("--dry-run")
    problems = expect_release(s, code, output, calls, f"{VERSION_DAY}.2")
    seen = open(log).read().splitlines() if os.path.exists(log) else []
    if len(seen) != 2:
        problems.append(f"GitHub reached {len(seen)} times, not twice (the fetch and the tags)")
    if any(line != "SSH_ASKPASS=[]" for line in seen):
        problems.append("ssh was given an askpass program: " + "; ".join(seen))
    return problems


def test_refused_without_githubs_tags(work):
    """GitHub's main fetched, then its tags can't be read: refused before anything is made."""
    s = Scratch(work)
    s.commit()
    over_ssh(s, work, fail=2)
    return expect_refused(*s.run("--dry-run"), "couldn't read GitHub's tags")


def test_refused_without_gh(work):
    """No gh: refused at once (only bash and dirname on PATH)."""
    s = Scratch(work)
    s.commit()
    farm = os.path.join(work, "farm")
    os.makedirs(farm)
    for tool in ("bash", "dirname"):
        os.symlink(shutil.which(tool), os.path.join(farm, tool))
    script = os.path.join(s.repo, "engine", "Tools", "Scripts", "release.sh")
    env = dict(s.env, PATH=farm)
    done = subprocess.run([script, "--dry-run"], cwd=work, env=env, capture_output=True, text=True, timeout=60)
    output = done.stdout + done.stderr
    if done.returncode == 0 or "needs gh" not in output:
        return [f"ended with {done.returncode}: {output.strip()[-300:]}"]
    return []


def test_usage(work):
    s = Scratch(work)
    s.commit()
    problems = []
    for args in (["--draft"], ["--dry-run", "--dry-run"], ["now"]):
        code, output, calls = s.run(*args)
        if code != 2 or "usage: release.sh" not in output or calls:
            problems.append(f"{args}: ended with {code}, calls {tools(calls)}: {output.strip()[-200:]}")
    return problems


TESTS = [(name[len("test_"):], f) for name, f in globals().items() if name.startswith("test_")]
RUN_CALLS = []  # every stand-in call of the test running


def main(names):
    failed = 0
    ran = 0
    root = tempfile.mkdtemp(prefix="release-test-")
    try:
        for name, test in TESTS:
            if names and name not in names:
                continue
            ran += 1
            work = os.path.join(root, name)
            os.makedirs(work)
            RUN_CALLS.clear()
            try:
                problems = test(work)
            except Exception as error:  # a scratch repository that couldn't be made
                problems = [f"{type(error).__name__}: {error}"]
            # The release runs no tests (Q27).
            if any(c["tool"] == "test.sh" for c in RUN_CALLS):
                problems.append("test.sh was run")
            if problems:
                failed += 1
                print(f"FAILED {name}:")
                for problem in problems:
                    print("    " + problem)
            else:
                print(f"ok {name}")
    finally:
        shutil.rmtree(root, ignore_errors=True)
    print(f"release.sh's tests: {ran - failed} of {ran} passed")
    return 1 if failed or ran == 0 else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

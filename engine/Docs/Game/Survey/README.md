# The survey behind the game plan (2026-11-12)

Four files describing the repo as it stood on 2026-11-12 at `29d110d` (`main`, M12 done, 3720 tests), written that day
by four read-only workers, one per file, for the planning that wrote `Docs/Game-Plan.md` (GM1–GM18); nothing in the
repo was changed for them. The plan was built on them, its facts checked against the repo the same day. They were
kept outside the repo, in `~/.cache/stat-fick-build/claude-work/port-plan/` on the user's PC, and were copied here
unchanged on 2026-11-12 so that every machine, the cloud's sessions too, can read them (nothing in them needed fixing
for the move: they hold no links and no path naming their own folder).

**A snapshot, not kept current.** Nothing here is updated after 2026-11-12. Where a file differs from the repo or from
`Docs/Game-Plan.md`, the repo and the plan win. The line numbers they give into other files ("EP 1446", `app.js`
17–149) are those of `29d110d`. Paths are relative to the repo's root unless a file says otherwise
(`old-game-systems.md`'s are relative to `old-game/`); where a file names the root as an absolute path,
`/run/media/system/M.2_2TB/Stat Fick Repo/`, that is the repo's place on the user's PC.

## The files

**`old-game-systems.md`** (1119 lines; its worker read `old-game/`, counting from the scripts and JSON themselves and
marking what comes only from `old-game/Docs/Notes.md` "(Notes)"): the old game system by system. An overview (at a
glance, how it starts, how the code is organised, its state and save format, screens, rendering, time and randomness,
the test client as the reference harness, what the engine already has); then 31 systems, each with its files, rules,
data and port notes (the character sheet, races, classes, spells, items, the forge and reforge, combat rules,
enemies, their AI and the bosses' mechanics, loot, the battle's presentation, cutscenes, shops and towns, interiors,
alchemy, the Adventurers Guild, world rules, encounters, the map, companions, the Church of Talos, Hearth Home,
Limitless, music, sprites, settings, the interface shell, saves and data loading, the desktop shell and test client);
every screen in a numbered table (§3, the numbers the plan cites); the dependency graph, what must exist before what
(§4, which the plan's grouping follows); sizes; and the questions a planner would ask that the code can't answer.

**`engine-for-the-port.md`** (1508 lines; its worker read `engine/CLAUDE.md`, the repo's `CLAUDE.md` and every
`engine/Docs/*.md` but `Port-Tasks/`, the four largest design docs, `Editor.md`, `Scene.md`, `Packaging.md` and
`Assets.md`, each by a reader of its own, the source tree only to confirm names): what Statventure gives the port and
how a milestone runs. How the project runs a milestone (the milestone list as it was, how one starts, the design doc's
shape, "How each step is finished" verbatim, the asking rule, the manager and the workers, where the work happens);
what each engine milestone, M0–M12, provides, and the sandbox; what is left for the port (the standing rules, "At port
time", "What M7 … M11 leave for the port", M12 and the stand-in data library); the reference method (the harnesses,
what is compared and to what limit, the fonts, compositing, the player's folders, what can't be checked against
Chromium); the open engine items (the small tasks, M12's findings, "Windows check", the accepted differences); and the
conventions (docs, decision ids, branches, test infrastructure, folders, the mod format, pack paths, programs).

**`port-tasks-state.md`** (714 lines; its worker read `Docs/Port-Tasks/` (the README, every task's text, the drafts'
structure with spot checks), `Docs/Engine-Plan.md`'s "How to continue", "At port time" and decision sections for
tasks 2–14, and the old game's notes on tasks 1–12): the fourteen port tasks. A table (each task's state, dates,
decision ids and record); per task, what was built and where, and what the port carries over, rebuilds or remakes as
art; the general task, every enemy revamped with classes and spells; the dependencies and ordering constraints across
tasks; sizes; and the questions the docs leave to the port or the user. It opens with the warning on the reused
decision prefixes (G, P, M).

**`content-inventory.md`** (591 lines; its worker read `old-game/` and the engine docs, counting from the files, JSON
parsed and JavaScript tables read as text, naming the doc where a number comes from one): the content and assets the
content milestones are sized from, with no milestone recommendations. Content counts, the old game beside what the
approved drafts add (classes, spells, enemies, items, consumables, races, regions, music, sound …); the data schemas;
where each content table lives (file and lines); the art (how it is made today, by hand or generated, what counts as
placeholder, the engine's new look, every kind of visual the port must produce); audio; saves; open questions; and an
appendix on the other files (`objects/`, `music/`, `index.html`, `desktop/`, the test client).

## Corrections

`Docs/Game-Plan.md`, "Cross-cutting", "Corrections to the survey", records what was found against the repo on
2026-11-12 (the plan uses the repo's figures); in one line each:
1. `old-game/classes/newc/` holds 15 `.txt` drafts, a template and an `IGNORE/` folder of 6 files (not 14 drafts, an
   IGNORE file and a template).
2. The old game's `styles.css` is 4184 lines (4080 elsewhere is a count from `a32c5cb`).
3. The old checks failing on the old code too are 12 entries and 2 intermittent ones (2026-11-11), not 11.
4. The class files hold 260 distinct `type` strings, 139 of them inside `effects` lists (the survey's 131 counted
   differently).
5. `ARTIFACT_SHAPES` draws 49 objects, while the artifact records name 50 shapes and 76 glyphs.
6. `Docs/Assets.md` counted 129 class files and 169 enemy files (2026-11-02); the repo has 130 and 174 (settled
   2026-11-12 in `Docs/Assets.md`).
7. Nygheir's file has no `phaseTransitions` entry (one file listed him among the bosses with a phase transition).

The survey also found three contradictions in the engine docs, settled on 2026-11-12 (`Docs/Notes.md`): M9's milestone
line named a JSON class loader M9 didn't build (`old-game-systems.md`, §2.30, repeats it; the class record and its
loader come with the port, `Docs/Assets.md`, Q27); `Docs/Editor.md`'s Status still had G3 open after M12-9 answered
it; and correction 6.

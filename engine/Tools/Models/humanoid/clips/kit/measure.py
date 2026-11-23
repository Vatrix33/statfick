# A clip measured before anyone is shown it (GM2-P23; the manager's standing rule since the user found flaws in clips
# approved by eye, 2026-11-18; Docs/Game/Hero.md, "How it works", "The keying kit"). From what run.py measure writes:
#   the arms   every solved arm at every key (solver.py): the worst wrist bend (degrees between the forearm and the hand)
#              and forearm turn (how far the hand is turned about the forearm from the elbow's plane; the user sees one
#              past about 90 as a twist) per arm, the frames past 85; the jumps between neighbouring keys (two frames apart
#              at most) of the turn, the bend and the elbow's swing (a jump is seen as the arm snapping or twisting for a
#              frame: one of the turn or the bend past 25, of the swing past 30, is listed); a grip out of the arm's reach
#              ("short", metres) and an arm in the trunk (solver.crowding past 0.02)
#   the ground every frame as Blender plays the clip on the man (his body, plain outfit, face and hair): the lowest point
#              over the ground (where his boots' soles are at rest; mm, under it below 0), its part and piece, and every
#              part's lowest at chosen frames
# Angles are whole degrees as the solver's notes have always been read (the manager's twist.sh: the hand-over's
# twist-*.txt), the ground in mm. measures.json (committed) holds every clip's summary with its data's sha256; bounds.json
# the family's bounds, the worst the 34 approved clips measure: a new clip is held to them (report says which it passes),
# and the engine's tests hold the committed clips to them (HumanoidKeyingKitTests). Plain Python, run by
# Tools/Scripts/make-humanoid.sh measure:
#   python3 -I measure.py report <measured.json> [clip ...]                  the arms' and the ground's lines, and whether
#                                                                            each is within the bounds (fails if one isn't)
#   python3 -I measure.py record <measured.json>                             measures.json from every clip's measure
#   python3 -I measure.py check <measured.json>                              measures.json is what was measured, and every
#                                                                            clip within the bounds
#   python3 -I measure.py series <measured.json> <clip> <L|R> <from> <to>    an arm's bend, turn, reach, turn about the
#                                                                            weapon and swing key by key
#   python3 -I measure.py parts <measured.json> <clip> <frame,frame,...>     every part's lowest at those frames
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
MEASURES = os.path.join(HERE, "measures.json")
BOUNDS = os.path.join(HERE, "bounds.json")
PAST = 85                    # a forearm turned past this is counted (the user sees about 90 as a twist)
JUMP = {"turn": 25, "bend": 25, "swing": 30}         # a change between neighbouring keys past these is listed
NEIGHBOURS = 2               # keys at most this many frames apart are neighbours
TRUNK = 0.02                 # an arm deeper than this in the trunk is counted


def wrap(d):
    return (d + 180) % 360 - 180


def summary(measured):
    """A clip's measures (run.py measure's entry) summed up: what measures.json holds."""
    arms, rows = {}, {}
    short, trunk = {}, {}
    for note in measured["arms"]:
        side, frame = note["side"], note["frame"]
        bend, turn, swing = round(note["bend"]), round(note["turn"]), round(note["swing"])
        arm = arms.setdefault(side, {"bend": 0, "bendFrame": 0, "turn": 0, "turnFrame": 0, "past": []})
        if bend > arm["bend"]:
            arm["bend"], arm["bendFrame"] = bend, frame
        if abs(turn) > abs(arm["turn"]):
            arm["turn"], arm["turnFrame"] = turn, frame
        if abs(turn) > PAST:
            arm["past"].append(frame)
        rows.setdefault(side, []).append((frame, bend, turn, swing))
        if note["short"] > 0:
            short.setdefault(side, []).append((frame, round(note["short"], 3)))
        if note["trunk"] > TRUNK:
            trunk.setdefault(side, []).append((frame, round(note["trunk"], 2)))
    out = {"clip": measured["clip"], "arms": {}, "jumps": [], "largestJump": {"turn": 0, "bend": 0, "swing": 0}, "short": {}, "trunk": {}}
    for side in sorted(arms):
        arm = arms[side]
        past = arm.pop("past")
        out["arms"][side] = dict(arm, past=[len(past), past[0], past[-1]] if past else [0])
        series = sorted(rows[side])
        for a, b in zip(series, series[1:]):
            if b[0] - a[0] > NEIGHBOURS:
                continue
            change = {"turn": wrap(b[2] - a[2]), "bend": b[1] - a[1], "swing": b[3] - a[3]}
            for name, value in change.items():
                out["largestJump"][name] = max(out["largestJump"][name], abs(value))
            if any(abs(value) > JUMP[name] for name, value in change.items()):
                out["jumps"].append(dict(change, side=side, at=[a[0], b[0]]))
    for name, found in (("short", short), ("trunk", trunk)):
        for side in sorted(found):
            frames = found[side]
            out[name][side] = {"frames": len(frames), "first": frames[0][0], "last": frames[-1][0], "worst": max(v for _, v in frames)}
    ground = measured["ground"]
    worst = min(ground, key=lambda row: row["lowest"])
    out["ground"] = {"lowest": round(worst["lowest"] * 1000, 2), "frame": worst["frame"], "part": worst["part"], "piece": worst["piece"],
                     "last": round(ground[-1]["lowest"] * 1000, 2), "lastPart": ground[-1]["part"],
                     "under": sum(1 for row in ground if row["lowest"] < -0.0025),
                     # every body part's lowest over the clip (mm): what lies on the ground, and what comes near it
                     "parts": {part: round(min(row["parts"][part] for row in ground) * 1000, 2) for part in sorted(ground[0]["parts"])}}
    return out


def lines(name, s):
    """The clip's report: the manager's twist.sh and ground.sh lines."""
    out = [name]
    for side, arm in s["arms"].items():
        past = arm["past"]
        out.append(f"   {side}: wrist bent at most {arm['bend']} (f{arm['bendFrame']}), turned at most {arm['turn']:+d} (f{arm['turnFrame']})"
                   + (f", past {PAST} on {past[0]} frames f{past[1]}-f{past[2]}" if past[0] else ""))
    for side in sorted({j["side"] for j in s["jumps"]}):
        jumps = [j for j in s["jumps"] if j["side"] == side]
        out.append(f"   {side}: JUMPS " + ", ".join(f"f{j['at'][0]}-f{j['at'][1]} (turn {j['turn']:+d}, bend {j['bend']:+d}, swing {j['swing']:+d})" for j in jumps))
    for name_, what in (("short", "short"), ("trunk", "in the trunk")):
        for side, found in s[name_].items():
            out.append(f"   {side} {what}: {found['frames']} frames f{found['first']}-f{found['last']}, worst {found['worst']:.3f}")
    g = s["ground"]
    out.append(f"   ground: lowest {g['lowest']:+.2f} mm at f{g['frame']} ({g['part']}, {g['piece']}); last frame {g['last']:+.2f} mm ({g['lastPart']}); "
               f"under by more than 2.5 mm on {g['under']} frames")
    return out


def worst(summaries):
    """The family's worst over clips' summaries: what bounds.json holds."""
    arms = [arm for s in summaries.values() for arm in s["arms"].values()]
    return {"bend": max((a["bend"] for a in arms), default=0), "turn": max((abs(a["turn"]) for a in arms), default=0),
            "jump": {name: max(s["largestJump"][name] for s in summaries.values()) for name in ("turn", "bend", "swing")},
            "short": max([f["worst"] for s in summaries.values() for f in s["short"].values()] + [0]),
            "trunk": max([f["worst"] for s in summaries.values() for f in s["trunk"].values()] + [0]),
            "under": -min(s["ground"]["lowest"] for s in summaries.values())}


def beyond(s, bounds):
    """What of a clip's summary is past the family's bounds."""
    one = worst({"clip": s})
    out = []
    for name in ("bend", "turn", "short", "trunk", "under"):
        if one[name] > bounds[name]:
            out.append(f"{name} {one[name]} past {bounds[name]}")
    for name in ("turn", "bend", "swing"):
        if one["jump"][name] > bounds["jump"][name]:
            out.append(f"a jump of the {name} {one['jump'][name]} past {bounds['jump'][name]}")
    return out


def load(path):
    with open(path) as f:
        return json.load(f)


def main(argv):
    command = argv[0] if argv else ""
    if command in ("report", "record", "check") and len(argv) >= 2:
        measured = load(argv[1])
        names = argv[2:] if command == "report" and len(argv) > 2 else list(measured)
        summaries = {name: summary(measured[name]) for name in names}
        if command == "report":
            bounds, failed = load(BOUNDS), 0
            for name, s in summaries.items():
                print("\n".join(lines(name, s)))
                past = beyond(s, bounds)
                failed += bool(past)
                print("   bounds: " + ("within the family's (bounds.json)" if not past else "PAST the family's: " + "; ".join(past)))
            sys.exit(1 if failed else 0)
        elif command == "record":
            with open(MEASURES, "w") as f:
                f.write(json.dumps({"comment": "Every clip's measures (measure.py, from make-humanoid.sh measure) with its data's sha256: held to "
                                               "bounds.json by the engine's HumanoidKeyingKitTests (Docs/Game/Hero.md, \"How it works\", \"The keying kit\").",
                                    "clips": {name: summaries[name] for name in sorted(summaries)}}, indent=1) + "\n")
            print(f"recorded {len(summaries)} clips' measures in {MEASURES}; the family's worst: {json.dumps(worst(summaries))}")
        else:
            recorded = load(MEASURES)["clips"]
            problems = [f"{name}: measured {json.dumps(summaries.get(name))}, recorded {json.dumps(recorded.get(name))}"
                        for name in sorted(set(recorded) | set(summaries)) if recorded.get(name) != summaries.get(name)]
            bounds = load(BOUNDS)
            problems += [f"{name}: past the family's bounds: " + "; ".join(beyond(s, bounds)) for name, s in summaries.items() if beyond(s, bounds)]
            for problem in problems:
                print("Error:", problem)
            print(f"checked {len(summaries)} clips' measures: " + ("all as recorded and within the bounds" if not problems else f"{len(problems)} problems"))
            sys.exit(1 if problems else 0)
    elif command == "series" and len(argv) == 6:
        measured, name, side, first, last = load(argv[1]), argv[2], argv[3], int(argv[4]), int(argv[5])
        notes = [n for n in measured[name]["arms"] if n["side"] == side and first <= n["frame"] <= last]
        print(f"{name} {side}: " + " | ".join(f"f{n['frame']} b{round(n['bend'])} t{round(n['turn']):+d} r{n['reach']:.2f} a{round(n['about']):+d} "
                                              f"s{round(n['swing']):+d}" for n in notes))
    elif command == "parts" and len(argv) == 4:
        measured, name = load(argv[1]), argv[2]
        for frame in (int(f) for f in argv[3].split(",")):
            row = measured[name]["ground"][frame]
            print(f"{name} f{frame} " + "  ".join(f"{part} {height * 1000:+.1f}" for part, height in sorted(row["parts"].items(), key=lambda item: item[1])))
    else:
        raise SystemExit("usage: measure.py report <measured.json> [clip ...] | record <measured.json> | check <measured.json> | "
                         "series <measured.json> <clip> <L|R> <from> <to> | parts <measured.json> <clip> <frames>")


if __name__ == "__main__":
    main(sys.argv[1:])

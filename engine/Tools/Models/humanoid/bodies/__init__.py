# The bodies (Docs/Game/Hero.md, "The humanoid rig family": 54, nine races × two sexes × three builds, heroic
# proportions, Q1, Q14, Q15): a module per race, each holding its bodies' parameter sets in BODIES by name
# ("<race>-<sex>-<build>", e.g. human-male-average). A body's model is its skeleton (rig.py, placed from the body's
# joints), its surface (forms.py) bound to it, and its plain outfit (outfit.py, Q18: one per race and sex, so the body
# carries it). Its parts (parts/) and the gear made on it are built on the same parameters. Every model is built at a
# detail level (levels.py: "full" or "battle"), which the manifest names; its pieces are joined per ink flag
# (kit.join_by_ink, GM2-P19).
import importlib
import math

import bpy

import forms
import kit as k
import levels
import materials
import outfit
import rig as r
from make_models import export


def params(name, level=None):
    """The parameter set of the body `name` (its race's module, bodies/<race>.py) at the detail `level` (levels.py; None:
    the skeleton's numbers alone, no surface can be made from them)."""
    module = name.rsplit("-", 2)[0].replace("-", "_")          # "dark-elf-male-average": bodies/dark_elf.py
    race = importlib.import_module(f"bodies.{module}")
    if name not in race.BODIES:
        raise ValueError(f"no body {name} in bodies/{module}.py")
    if level is not None and level not in levels.LEVELS:
        raise ValueError(f"no detail level {level} in levels.py")
    p = race.BODIES[name]()
    p["name"] = name
    if level is not None:
        p["detail"] = levels.LEVELS[level]
    return p


def pose_arms(p, degrees):
    """Turns the arm's points about the shoulder so the arm hangs `degrees` below the horizontal: the rest pose's
    A-pose."""
    points = p["arm"]
    sx, sz = points[1][1], points[1][3]
    ex, ez = points[3][1] - sx, points[3][3] - sz                 # shoulder to elbow now
    turn = math.radians(degrees) - math.atan2(-ez, ex)
    c, s_ = math.cos(turn), math.sin(turn)
    out = list(points[:2])
    for n, x, y, z, rad in points[2:]:
        dx, dz = x - sx, z - sz
        out.append((n, sx + dx * c + dz * s_, y, sz - dx * s_ + dz * c, rad))
    p["arm"] = out


def as_build(p, shape):
    """A body's parameters as another build of it (Q15: slender, strong; Hero.md, "the slender and strong builds made from
    it by the script and refined"): the same skeleton, every joint where the average has it, so the clips, the grips and
    the head's parts fit as on the average; other girths. `shape`: "torso" {section: (width, depth) factors}, "arm" and
    "leg" {point: radius factor}, and the ratios the masses and the sockets follow (forms.build_ratios, rig.bone_table):
    "muscle", "w", "d", "a", "l", "bust", "seat", "hips", "back"."""
    p = dict(p)
    p["spine"] = [(n, y, z, rx * shape["torso"].get(n, (1.0, 1.0))[0], ry * shape["torso"].get(n, (1.0, 1.0))[1]) for n, y, z, rx, ry in p["spine"]]
    p["arm"] = [(n, x, y, z, radius * shape["arm"].get(n, 1.0)) for n, x, y, z, radius in p["arm"]]
    p["leg"] = [(n, x, y, z, radius * shape["leg"].get(n, 1.0)) for n, x, y, z, radius in p["leg"]]
    before = p.get("build", {})          # a race's own ratios (proportioned with masses), which the build's multiply
    p["build"] = {key: shape.get(key, 1.0) * before.get(key, 1.0) for key in ("muscle", "w", "d", "a", "l", "bust", "seat", "hips", "back", "ll", "al", "tl") if key in shape or key in before}
    return p


def proportioned(p, legs=1.0, torso=1.0, shoulders=1.0, hips=1.0, girth=1.0, arms=1.0, neck=1.0, head=1.0, hands=1.0, feet=1.0, size=1.0, masses=False):
    """A body's parameters with other proportions: a race's body from the human's (Q1: the races' differences kept by
    the same ratios). `legs`: the legs' length from the ankle up; `torso`: the trunk's length from the groin up (the arms
    and the head go up with it); `shoulders`, `hips`: the widths; `girth`: the limbs' and the trunk's thickness; `arms`:
    the arms' length from the shoulder; `neck`: the neck's thickness; `head`, `hands`, `feet`: their size (`feet` may be (length, girth)). `size`: the whole
    figure's size (the old game's `size` of a race): the body and everything made on it is built as proportioned and made
    this much larger or smaller as a whole when it is exported (export_sized), so every measure of the builders holds.
    `masses`: the muscles' masses follow the widths and the girth (as a build's do; left out for the elves, whose bodies
    were approved without)."""
    p = dict(p)
    ankle = next(z for n, x, y, z, r in p["leg"] if n == "ankle")
    hip = next(z for n, x, y, z, r in p["leg"] if n == "hip")
    groin = p["spine"][0][2]
    rise = (hip - ankle) * (legs - 1.0)

    def up(z):
        return z + rise + (z - groin) * (torso - 1.0)

    wide = {"groin": hips, "pelvis": hips, "belly": (hips + shoulders) / 2, "waist": (hips + shoulders) / 2, "ribs": shoulders, "chest": shoulders,
            "clavicle": shoulders, "yoke": shoulders, "neck0": neck, "neck1": neck}
    deep = {"neck0": neck, "neck1": neck}
    p["spine"] = [(n, y, up(z), rx * wide[n], ry * deep.get(n, girth)) for n, y, z, rx, ry in p["spine"]]
    p["leg"] = [(n, x * hips, y, ankle + (z - ankle) * legs if z > ankle else z, r * girth) for n, x, y, z, r in p["leg"]]
    long, thick = feet if isinstance(feet, tuple) else (feet, feet)       # (a foot's length and its girth apart, for big bare feet)
    p["foot"] = [(n, x * hips, y * long, z * thick, r * thick) for n, x, y, z, r in p["foot"]]   # (its height with its girth: the sole stays on the ground)
    sx, sy, sz = p["arm"][1][1:4]
    moved = (sx * shoulders, sy, up(sz))
    arm = []
    for n, x, y, z, r in p["arm"]:
        if n in ("armroot", "shoulder"):
            arm.append((n, x * shoulders, y, up(z), r * girth))
        else:
            arm.append((n, moved[0] + (x - sx) * arms, moved[1] + (y - sy) * arms, moved[2] + (z - sz) * arms, r * girth))
    p["arm"] = arm
    cx, cy, cz = p["head"]["centre"]
    skull = p["head"]["size"]
    p["head"] = dict(p["head"], centre=(cx, cy, up(cz) + skull[2] * (head - 1.0)), size=tuple(s * head for s in skull))
    p["hand"] = {key: value * hands for key, value in p["hand"].items()}
    p["size"] = p.get("size", 1.0) * size
    if masses:
        p["build"] = dict(p.get("build", {}), w=shoulders, d=girth, a=girth, l=girth, hips=hips, back=girth, ll=legs, al=arms, tl=torso)
    return p


def export_sized(path, p, **options):
    """Exports the scene's model at the body's size (`size` of its parameters; 1: as built)."""
    size = p.get("size", 1.0)
    if size != 1.0:
        from mathutils import Matrix
        scale = Matrix.Scale(size, 4)
        for obj in bpy.context.scene.objects:
            if obj.type in ("MESH", "ARMATURE"):
                obj.data.transform(scale)
    export(path, **options)


def build(name, level):
    """The body `name` at the detail `level` in the scene: its surface bound to its skeleton and dressed in its plain
    outfit. Returns {"p", "body", "rig", "joints", "maker"} (the maker, outfit.Maker, cuts more garments from the dressed
    body)."""
    p = params(name, level)
    m = materials.make()
    body, joints = forms.build_body(p, m["skin"])
    rig = r.build_rig(p, joints)
    r.bind(rig, body)
    maker = outfit.Maker(p, body, rig, joints, m)
    outfit.OUTFITS[p["outfit"]](maker)
    return {"p": p, "body": body, "rig": rig, "joints": joints, "maker": maker}


def keep_only(figure, pieces):
    """Leaves only `pieces` and the skeleton in the scene (a garment made on the body is exported with the skeleton it is
    bound to)."""
    for obj in [figure["body"]] + [o for o in figure["maker"].pieces if o not in pieces]:
        mesh = obj.data
        bpy.data.objects.remove(obj)
        bpy.data.meshes.remove(mesh)


def body_model(path, name, level):
    """The body's model (manifest.json: "bodies:body_model", args [name], its level): skeleton, surface and plain outfit,
    joined."""
    figure = build(name, level)
    k.join_by_ink([figure["body"]] + figure["maker"].pieces, name)
    export_sized(path, figure["p"], export_skins=True)

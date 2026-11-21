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
    race = importlib.import_module(f"bodies.{name.split('-')[0]}")
    if name not in race.BODIES:
        raise ValueError(f"no body {name} in bodies/{name.split('-')[0]}.py")
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
    p["build"] = {key: shape[key] for key in ("muscle", "w", "d", "a", "l", "bust", "seat", "hips", "back") if key in shape}
    return p


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
    export(path, export_skins=True)

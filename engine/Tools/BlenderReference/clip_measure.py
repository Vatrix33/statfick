# A clip's translations follow the body's size (GM2-P22, "the figure's ground"; Docs/Animation.md, "A clip's
# translations follow the body's size"), Blender's side of the engine's rule (Animation/Skeleton.cs, ClipBinding.Scale):
# a clip whose action names a measure (its custom property "measure", a chain of joints top first, which a family's clip
# library sets and the exporter writes as the animation's extras: the humanoid's left thigh, shin and foot) has every
# bone's location scaled, on a body other than the one it was keyed on, by the chain's length at rest there over its
# length on the body it was keyed on (the distances from joint to joint, added). Exactly 1 on the keyed body, whose chain
# is the same numbers: nothing is touched there. Used wherever Blender evaluates or renders a clip on a body:
# evaluate_animations.py (the clips' references) and Tools/Models/humanoid/render.py's pose_at (so assemble.py's).
import math


def measure_of(action):
    """The action's measure (a list of joint names), or None."""
    measure = action.get("measure") if hasattr(action, "get") else None
    if measure is None:
        return None
    return [str(name) for name in measure]


def chain_length(points):
    """The length of a chain of points: the distances from each to the next, added, in double precision."""
    return sum(math.dist(points[k], points[k + 1]) for k in range(len(points) - 1))


def armature_chain(rig, chain):
    """A chain's length at rest on an armature object (its bones' heads in the armature's space), or None when it lacks a
    joint of it or the chain has fewer than two."""
    if chain is None or len(chain) < 2 or any(name not in rig.data.bones for name in chain):
        return None
    return chain_length([tuple(rig.data.bones[name].head_local) for name in chain])


def glb_chain(worlds, chain):
    """A chain's length at rest from a .glb's nodes' rest worlds by name (render.py's glb_rest_worlds), or None."""
    if chain is None or len(chain) < 2 or any(name not in worlds for name in chain):
        return None
    return chain_length([tuple(worlds[name].translation) for name in chain])


def ratio(keyed, playing):
    """What a location keyed on a body whose chain is `keyed` long is multiplied by on one whose chain is `playing` long:
    1 when either isn't known (no measure, a body without the chain)."""
    if keyed is None or playing is None or not keyed > 0 or not playing > 0:
        return 1.0
    return playing / keyed


def location_curves(action):
    """The action's F-curves of bones' locations (Blender 5's layered actions: its first layer's strip's channel bag)."""
    curves = []
    for layer in action.layers:
        for strip in layer.strips:
            for slot in action.slots:
                bag = strip.channelbag(slot)
                if bag is not None:
                    curves += [c for c in bag.fcurves if c.data_path.startswith("pose.bones[") and c.data_path.endswith(".location")]
    return curves


def scale_locations(action, factor):
    """Every bone location of the action times `factor`: each key's value and both its handles' (a Bézier curve whose
    control points are scaled is the curve scaled, exactly), the handles not recalculated. Nothing when it is 1."""
    if factor == 1.0:
        return
    for curve in location_curves(action):
        for point in curve.keyframe_points:
            point.co[1] *= factor
            point.handle_left[1] *= factor
            point.handle_right[1] *= factor

# The figure the clips are keyed on (GM2-P23; Docs/Game/Hero.md, "How it works", "The keying kit"): the human man, average
# build (clips/library.py's SKELETON: every clip is authored on him), at the game's full level, built by the family's own
# builders (bodies.build, rig.py, outfit.py) in Blender's scene. What the keying reads of him is his skeleton at rest and
# his boots (outfit.boots): the ground is where their soles are at rest, and bake.py keeps a planted boot on it. Proven
# against the figure the approved clips were keyed on (the manager's scratch, 2026-11-16/17): the skeleton's rest and the
# boots' every vertex and weight bit for bit the same, and the 34 clips baked again byte for byte (Hero.md, "The keying
# kit"). Runs inside Blender.
import bpy

import bodies
import forms
import gear
import materials
import rig as r
from clips import library
from parts import face, hair

import props

LEVEL = "full"
FACE, HAIR = "human-male-1", "swept-back"          # the man's approved face and hair (his preset's; the clips' pictures)
DRESS = ("bare", "parts", "geared")


def keying_figure(dress="bare"):
    """The man in the scene: {"rig", "boots", "skinned" (every mesh that follows the skeleton), "gear" {kind: [objects]}}.
    `dress`: "bare" his body and plain outfit (what the keying needs), "parts" with his face and hair too (what the ground's
    measure sees, a head that lies on the ground in death among it), "geared" with the chain shirt over them and the sample
    sword and shield and the stand-ins (props.py) ready to be put in his hands (the pictures; a kind's objects out of the
    scene until a frame places copies of them)."""
    if dress not in DRESS:
        raise ValueError(f"no dress {dress!r} (the dresses: {', '.join(DRESS)})")
    made = bodies.build(library.SKELETON, LEVEL)
    rig = made["rig"]
    boots = [o for o in made["maker"].pieces if o.name.endswith("-boots")]
    if len(boots) != 1:
        raise ValueError(f"{library.SKELETON}: {len(boots)} boots in his outfit (the ground is their soles at rest)")
    skinned = [made["body"]] + list(made["maker"].pieces)
    kinds = {}
    if dress != "bare":
        p = face.with_face(library.SKELETON, FACE, LEVEL)
        m = materials.make()
        pieces, _ = forms.build_head(p, m)
        pieces += forms.build_hair(p, forms.head_shape(p["head"]), m["hair"], hair.STYLES[HAIR](), tie=m["hair.tie"])
        for piece in pieces:
            r.bind_rigid(rig, piece, "head")
        skinned += pieces
    if dress == "geared":
        skinned += gear.chain_shirt(made["maker"])
        m = materials.make()
        kinds = {"sword": gear.sword(m, False), "shield": gear.shield(m, False)}
        kinds.update({kind: props.stand_in(m, kind) for kind in props.KINDS})
        for objects in kinds.values():
            for obj in objects:
                for collection in obj.users_collection:
                    collection.objects.unlink(obj)
    bpy.context.view_layer.update()
    return {"rig": rig, "boots": boots[0], "skinned": skinned, "gear": kinds}

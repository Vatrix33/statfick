# The humanoid skeleton (GM2-P8; Docs/Game/Hero.md, "The humanoid rig family"): step 4 builds it here. What it holds, as
# the design fixes it: the stand-in's 17 deforming bones as the base (hips, spine, chest, neck, head; upper_arm,
# forearm, hand, thigh, shin, foot per side), per hand a thumb bone and one bone for the mitten's block (Q2), no face
# bones (Q2), chains for keyed secondary motion where a part needs them, the sockets below; every body has every bone,
# so every clip plays on every body. Bones are given as make_models.armature takes them:
# (name, head, tail, parent, connected[, deform[, roll in degrees]]), Blender's coordinates, the figure facing −Y.
from make_models import armature  # noqa: F401  (the rig's builder: armature(name, bones))

# The sockets M7 names (Docs/Animation.md, "Gear in detail"; non-deforming bones, an item's grip at the head, its axes
# the bone's); the shield's socket is added where its approval puts it (Q9).
SOCKETS = ["socket.hand.R", "socket.hand.L", "socket.back", "socket.head", "socket.hip.L", "socket.hip.R"]

# The skeleton's bones (step 4).
BONES = []

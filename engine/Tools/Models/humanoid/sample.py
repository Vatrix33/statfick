# The frame's sample (GM2 step 3a): a cube figure, a few boxes on two bones, which proves the frame end to end (built by
# build.py from the manifest, checked by --check, rendered by render.py in a look, played by the sandbox's figure mode,
# compared with Blender in the engine's tests: Tests/Statventure.Tests/Sandbox/). Not art and not shipped: its output is
# test data (Tests/Statventure.Tests/Data/Models/humanoid/sample.glb).
#
# Blender's coordinates (Z up); it faces −Y, as the engine's test figure does (glTF's +Z). Two bones, "lower" (the hips,
# the legs' block) and "upper" (the chest, the torso and the head with a nose that shows where it faces); the boxes
# rigid on their bone; materials in palette slots (trousers, cloth, skin). Two clips at 60 fps, exported as the engine's
# animated test models are (make_models.ANIMATED): "sway" (1 s, the chest bending left and right) and "bow" (0.75 s, the
# chest bowing forward while the hips dip).
import bmesh
import bpy
from mathutils import Matrix

import make_models
from make_models import ANIMATED, armature, export, keys, material, stack_actions

BONES = [
    ("lower", (0, 0, 0), (0, 0, 0.9), None, False),
    ("upper", (0, 0, 0.9), (0, 0, 1.5), "lower", True),
]

# (bone, centre, size, material index)
BOXES = [
    ("lower", (0, 0, 0.45), (0.36, 0.22, 0.9), 0),
    ("upper", (0, 0, 1.2), (0.5, 0.28, 0.6), 1),
    ("upper", (0, 0, 1.69), (0.26, 0.26, 0.3), 2),
    ("upper", (0, -0.16, 1.67), (0.06, 0.08, 0.08), 2),
]


def cube_figure(path):
    bpy.context.scene.render.fps, bpy.context.scene.render.fps_base = 60, 1
    rig = armature("Sample", BONES)
    groups = [b[0] for b in BONES]
    materials = [material("trousers", "#4a5a6a"), material("cloth", "#8a4a3a"), material("skin", "#c49a74")]

    bm = bmesh.new()
    deform = bm.verts.layers.deform.verify()
    for bone, centre, size, index in BOXES:
        made = bmesh.ops.create_cube(bm, size=1, matrix=Matrix.Translation(centre) @ Matrix.Diagonal((*size, 1)))
        for vert in made["verts"]:
            vert[deform][groups.index(bone)] = 1.0
        faces = {f for v in made["verts"] for f in v.link_faces}
        for face in faces:
            face.material_index = index
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new("sample")
    bm.to_mesh(mesh)
    bm.free()
    for m in materials:
        mesh.materials.append(m)
    body = make_models.mesh_object("Body", mesh, rig)
    for group in groups:
        body.vertex_groups.new(name=group)
    body.modifiers.new("Armature", "ARMATURE").object = rig
    rig.animation_data_create()

    sway = bpy.data.actions.new("sway")
    for frame, bend in ((1, 0), (16, 12), (31, 0), (46, -12), (61, 0)):
        keys(rig, sway, frame, "upper", rotation=((0, 0, 1), bend))
    bow = bpy.data.actions.new("bow")
    for frame, k in ((1, 0), (23, 1), (46, 0)):
        keys(rig, bow, frame, "upper", rotation=((1, 0, 0), 30 * k))
        keys(rig, bow, frame, "lower", location=(0, -0.04 * k, 0))
    bow.pose_markers.new("low").frame = 23

    stack_actions(rig, (sway, bow))
    export(path, **ANIMATED)

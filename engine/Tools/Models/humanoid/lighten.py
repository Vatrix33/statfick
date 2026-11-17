# The figures' diet (GM2-P20; the user's approval of the lighter figures, 2026-11-16): the approved forms are sculpted as
# before (dense: a voxel fusion of the forms, smoothed, the masses pushed in and out: forms.py), and what a model gets is
# the sculpt remade as fewer, even quads that lie on it, at its detail level's counts (levels.py).
#  - The body: an even remesh needs small faces everywhere just to keep the mitten hands (the thumbs collapse first), so
#    the hands are remade on their own, each with the faces a hand needs, and stitched to the lighter arms a little above
#    the wrists. (Scaling the hands up inside one remesh of the whole body was tried first: whether a thumb survived was
#    luck, and a thin-thumbed body dragged the whole mesh's face count up.)
#  - The head: the same about the head's centre, the front half and the ears scaled up, the back of the skull not.
#  - Every new vertex is put back onto the dense sculpt, so the light mesh is the same surface, sampled; and it is
#    measured against the sculpt: when anything but the hands, or the face, strays too far (a foot's front, the nose's
#    tip rounded off), the remesh is done again with more faces until it holds.
# (Also tried and dropped: pulling single vertices out to collapsed tips, and bevelling or refining sharp folds: both add
# stray ink lines.) QuadriFlow follows Blender's thread count: make-humanoid.sh runs Blender with 8 threads, always.
import bmesh
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

V = Vector


def smoothstep(x):
    x = max(0.0, min(1.0, x))
    return x * x * (3 - 2 * x)


def surface(mesh):
    """A mesh's surface for nearest-point questions (and the bmesh it was made from, to free)."""
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    tree = BVHTree.FromBMesh(bm)
    return tree, bm


def quadriflow(obj, faces, seed=0):
    """`obj` remade as about `faces` even quads (QuadriFlow, a fixed seed), its materials kept; refuses a remesh that did
    nothing (QuadriFlow gives up on some shapes silently)."""
    bpy.context.view_layer.update()                # an object linked a moment ago is in the layer only after this
    for other in bpy.context.view_layer.objects:
        other.select_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    materials = list(obj.data.materials)
    before = len(obj.data.polygons)
    done = bpy.ops.object.quadriflow_remesh(use_mesh_symmetry=False, use_preserve_sharp=False, use_preserve_boundary=False,
                                            preserve_attributes=False, smooth_normals=True, mode="FACES", target_faces=faces, seed=seed)
    if "FINISHED" not in done or (len(obj.data.polygons) == before and abs(before - faces) > 0.05 * faces):
        raise RuntimeError(f"the remesh of {obj.name} did nothing ({done}, {before} faces, {faces} asked)")
    for material in materials:
        if material.name not in obj.data.materials:
            obj.data.materials.append(material)
    obj.data.shade_smooth()
    obj.select_set(False)


def remade(obj, settings, limit, seed=0):
    """`obj`'s dense mesh remade as even quads in a warped space, each new vertex put onto the dense sculpt. `settings`
    are tried in turn, each (faces, into, back, watch): `into` warps a point and `back` undoes it, `watch(point)` names
    the sculpt's points that must stay within `limit` metres of the light surface. The first setting that holds is kept
    (the last one if none does)."""
    sculpt = obj.data.copy()
    tree, keep = surface(sculpt)
    every = [v.co.copy() for v in sculpt.vertices]
    for attempt, (faces, into, back, watch) in enumerate(settings):
        if attempt:
            old = obj.data
            obj.data = sculpt.copy()
            bpy.data.meshes.remove(old)
        for v in obj.data.vertices:
            v.co = into(v.co)
        obj.data.update()
        quadriflow(obj, faces, seed)
        for v in obj.data.vertices:
            v.co = tree.find_nearest(back(v.co))[0]
        obj.data.update()
        light, bm = surface(obj.data)
        worst = max((light.find_nearest(co)[3] for co in every if watch(co)), default=0.0)
        bm.free()
        if worst <= limit:
            break
    obj.data.shade_smooth()
    keep.free()
    bpy.data.meshes.remove(sculpt)
    return obj


def zone(bm, wrist, axis, start, radius=0.085):
    """The faces of a hand's neighbourhood: by the forearm's line, from `start` metres along it (0 is the wrist)."""
    faces = []
    for f in bm.faces:
        d = f.calc_center_median() - wrist
        t = d.dot(axis)
        if t > start and (d - axis * t).length < radius:
            faces.append(f)
    return faces


def geometry(bm, faces):
    """The faces of `bm` with their vertices and edges, for a bmesh operator, in the mesh's own order (a set's order
    follows the elements' addresses, which change from run to run, and a cut's new elements with it: the wrists came out
    triangulated differently on every build). The indices are brought up to date first: an operator leaves its new
    elements without one."""
    for elements in (bm.verts, bm.edges, bm.faces):
        elements.index_update()
    verts = sorted({v for f in faces for v in f.verts}, key=lambda v: v.index)
    edges = sorted({e for f in faces for e in f.edges}, key=lambda e: e.index)
    return verts + edges + sorted(faces, key=lambda f: f.index)


def light_body(obj, faces, wrists, hand=800, seed=0, arm_cut=-0.050, hand_cut=-0.038, body_limit=0.012):
    """The body remade as about `faces` even quads lying on the sculpt, its hands as about `hand` quads each. `wrists` is
    a list of (wrist, along the forearm) pairs. The body is remade whole (what becomes of its hands doesn't matter), with
    10 % more faces at a time while anything but the hands is more than `body_limit` off the sculpt (few faces can lose a
    foot's front), and cut off at `arm_cut` along each forearm; each hand is taken from the sculpt from a little higher
    up, closed, remade alone (scaled up: the remesher's own grid is coarse on something this small), put onto the sculpt
    and cut at `hand_cut`; the two open rings are then bridged, so the body is closed."""
    sculpt = obj.data.copy()
    tree, keep = surface(sculpt)
    every = [v.co.copy() for v in sculpt.vertices]
    wrists = [(V(w), V(a).normalized()) for w, a in wrists]

    def of_hand(co, start=0.0):
        return any((co - w).dot(a) > start and ((co - w) - a * (co - w).dot(a)).length < 0.085 for w, a in wrists)

    rest = [co for co in every if not of_hand(co, -0.03)]
    for attempt, more in enumerate((1.0, 1.1, 1.21, 1.33, 1.46, 1.61, 1.77)):
        if attempt:
            old = obj.data
            obj.data = sculpt.copy()
            bpy.data.meshes.remove(old)
        quadriflow(obj, round(faces * more), seed)
        for v in obj.data.vertices:
            v.co = tree.find_nearest(v.co)[0]
        obj.data.update()
        lit, bm = surface(obj.data)
        off = max(lit.find_nearest(co)[3] for co in rest)
        bm.free()
        if off <= body_limit:
            break
    body = bmesh.new()
    body.from_mesh(obj.data)
    for wrist, axis in wrists:
        bmesh.ops.bisect_plane(body, geom=geometry(body, zone(body, wrist, axis, -0.14)), dist=0.00005, plane_co=wrist + axis * arm_cut, plane_no=axis,
                               clear_outer=True, clear_inner=False)
        # the hand: the sculpt from 8 cm above the wrist, closed there, remade alone, put onto the sculpt, cut
        piece = bmesh.new()
        piece.from_mesh(sculpt)
        bmesh.ops.bisect_plane(piece, geom=geometry(piece, zone(piece, wrist, axis, -0.16)), dist=0.00005, plane_co=wrist + axis * -0.08, plane_no=axis,
                               clear_inner=True, clear_outer=False)
        kept = set(zone(piece, wrist, axis, -0.081))
        bmesh.ops.delete(piece, geom=[f for f in piece.faces if f not in kept], context="FACES")
        bmesh.ops.delete(piece, geom=[e for e in piece.edges if e.is_wire], context="EDGES")      # what the cuts left behind
        bmesh.ops.delete(piece, geom=[v for v in piece.verts if not v.link_faces], context="VERTS")
        bmesh.ops.holes_fill(piece, edges=[e for e in piece.edges if e.is_boundary])
        bmesh.ops.triangulate(piece, faces=[f for f in piece.faces if len(f.verts) > 4])
        bmesh.ops.recalc_face_normals(piece, faces=piece.faces[:])
        for v in piece.verts:
            v.co = wrist + (v.co - wrist) * 4.0
        mesh = bpy.data.meshes.new("hand")
        piece.to_mesh(mesh)
        piece.free()
        temp = bpy.data.objects.new("hand", mesh)
        bpy.context.scene.collection.objects.link(temp)
        quadriflow(temp, hand, seed)
        for v in temp.data.vertices:
            v.co = tree.find_nearest(wrist + (v.co - wrist) / 4.0)[0]
        light = bmesh.new()
        light.from_mesh(temp.data)
        bmesh.ops.bisect_plane(light, geom=light.verts[:] + light.edges[:] + light.faces[:], dist=0.00005, plane_co=wrist + axis * hand_cut, plane_no=axis,
                               clear_inner=True, clear_outer=False)
        made = bpy.data.meshes.new("hand-light")
        light.to_mesh(made)
        light.free()
        body.from_mesh(made)                       # joined into the body's mesh
        old = temp.data
        bpy.data.objects.remove(temp)
        bpy.data.meshes.remove(old)
        bpy.data.meshes.remove(made)
        # the two open rings by this wrist, bridged
        body.edges.ensure_lookup_table()
        rings = [e for e in body.edges if e.is_boundary and arm_cut - 0.004 < ((e.verts[0].co + e.verts[1].co) / 2 - wrist).dot(axis) < hand_cut + 0.004
                 and (((e.verts[0].co + e.verts[1].co) / 2 - wrist) - axis * ((e.verts[0].co + e.verts[1].co) / 2 - wrist).dot(axis)).length < 0.085]
        bmesh.ops.bridge_loops(body, edges=rings)
    bmesh.ops.recalc_face_normals(body, faces=body.faces[:])
    body.to_mesh(obj.data)
    body.free()
    obj.data.update()
    obj.data.shade_smooth()
    keep.free()
    bpy.data.meshes.remove(sculpt)
    return obj


def light_head(obj, faces, centre, front=(0.0, -1.0, -0.2), face=1.9, limit=0.003, seed=0):
    """The head: every point moved out from the head's centre by a factor that is `face` over the front half and the
    ears and falls to 1 at the back of the skull (the direction kept, so the way back is exact). The front is watched (the
    nose's tip may not be rounded off by more than `limit`); when it doesn't hold, the head gets more faces."""
    centre, front = V(centre), V(front).normalized()

    def factor(d):
        return 1.0 + (face - 1.0) * smoothstep((d.dot(front) + 0.75) / 0.6)

    def into(co):
        r = co - centre
        return centre + r * factor(r.normalized())

    def back(co):
        r = co - centre
        return centre + r / factor(r.normalized())

    def watch(co):
        return (co - centre).normalized().dot(front) > 0.25

    settings = [(round(faces * more), into, back, watch) for more in (1.0, 1.1, 1.21, 1.33, 1.46, 1.61)]
    return remade(obj, settings, limit, seed)

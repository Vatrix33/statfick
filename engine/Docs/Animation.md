# Animation (M7)

Animation moves the game's Blender models: clips made in Blender and exported in the .glb files,
sampled as glTF specifies, shared between models of one rig family by bone names, stacked in layers
and cross-faded as Blender's NLA does, with events at marked frames, state machines that pick the
clips, and gear that follows the bones. It fills the `ModelPose` that M6's renderer draws
(`Docs/Renderer-3D.md`, "GPU skinning").

## Decisions (user, 2026-10-31)

Asked before the milestone started; the answers are also in `Docs/Engine-Plan.md`, "Decisions for M7".

- **Clip set** (what the system and the real models must cover): every pose the old battle stage
  plays, per rig, with the melee swing split into weapon families.
  - Humanoids (players, companions, humanoid enemies): idle and stunned loops; advance and return
    (the lunge), flee; attacks: one-handed swing (sword, axe, mace, club, hammer), two-handed swing
    (greatsword, greataxe, two-handed hammer), thrust (spear, trident, rapier), dagger stab,
    staff/wand strike, unarmed punch, off-hand swing; shooting: bow (bow, longbow, greatbow),
    crossbow; cast, skill, item, drink, meditate; guard and evade (enter, then hold); hurt,
    critical hurt, stumble; death, victory.
  - Quadrupeds (wolf, bear, boar, stag, ram): idle, bite/pounce, horn charge, hurt, stumble,
    stunned, death, victory. Griffon and dragon: the quadruped set and a wing attack. Wisp and core:
    idle (float), attack pulse, hurt, death.
  - Bosses: their rig's set plus phase change, transform, an intro entrance and their specials
    (asked per boss at port time).
- **No root motion.** Clips animate in place (the model's origin stays; hips may bob, hop or fall).
  The game moves whole models for lunges, knock-backs and fleeing, as the old game moves its
  sprites, so a lunge reaches any target whatever the distance.
- **Clip events** (a hit landing, a bow's release, a footstep) are pose markers in each Blender
  action. Blender's glTF exporter writes no markers, but it writes an action's custom properties as
  the glTF animation's extras (checked in its 5.2 source, `blender/exp/animation/action.py`); the
  export script copies the markers into such a property and the engine reads them.
- **State machines are data:** per rig a graph of states (clip, speed, loop), transitions (conditions
  on parameters, triggers, cross-fade time) and any-state entries (hurt, death). M7 builds the
  format, the runtime and a C# builder; the packs carry it (M9), the editor edits it (M11); the game
  sets parameters and triggers.
- **Layers and cross-fades follow Blender's NLA:** layers stack like NLA tracks; each replaces only
  the channels its clip keys, by its influence; additive layers combine on top as NLA's Combine mode
  does; a cross-fade is a linear influence ramp (an NLA strip's blend in/out). Tested against
  Blender's NLA evaluation. Clips are exported with the exporter's "Force keeping channels for bones"
  off: by default it writes every bone into every clip (constant ones as two keys), with it off a
  clip's glTF channels are the ones its action keys (checked in
  `blender/exp/animation/sampled/armature/keyframes.py`).
- **Clip data is glTF's sampled animation:** clips are animated at 60 fps in Blender (the game's frame
  rate) and exported sampled every frame (the exporter's default, which bakes constraints, IK and
  drivers into keys); the engine interpolates as glTF specifies. Checked against Blender's own
  evaluation of the actions: exactly at frames, within a measured tolerance between them.
- **Clips are shared by bone names**, as Blender applies an action to another armature: one clip set
  per rig family (humanoid, quadruped, griffon, dragon, wisp, core); models of a family share bone
  names and differ in proportions (races, sexes, builds); a clip plays on any of them with each
  channel taken relative to that bone's rest pose. Bosses and odd enemies can add clips of their own.
- **Gear:** rigid items (weapons, shields, helmets, quivers) are their own models attached to socket
  bones in the rig (hand.R, hand.L, back, head …); armour and clothing that bends are meshes skinned
  to the wearer's skeleton by bone names.
- **Secondary motion** (capes, tails, hair, banners, wings) is keyed in each clip; no simulation.
- **Bones only:** no shape keys (glTF morph targets); a jaw or eyelids are bones.
- **Model approval (port time):** the user approves the first model of each rig (the first humanoid
  with gear and its full clip set, as renders and a playing view; then the first quadruped, griffon,
  dragon, wisp and core) before the rest of that rig is made. M7 itself animates test models only.

## Method

As in M6 (`Docs/Renderer-3D.md`, "Method"), Blender 5.2 is the reference:
1. **Reference first.** The test models and their actions are built by
   `Tools/BlenderReference/make_models.py` (the script is their source) and exported with their
   clips. The tests describe animation scenes (which clips play on which model, in which layers,
   when, how fast, with what influence) and write them to `Tests/Statventure.Tests/Reference/
   anim-scenes.json`; a Blender script rebuilds the models with their original actions (from
   `make_models.py`, not from the .glb, so Blender evaluates its own F-curves), sets up the same NLA
   tracks and strips, evaluates them at the listed times and writes every bone's pose into
   `Tests/Statventure.Tests/Golden/anim-<scene>.json`. Both are committed, so the tests run where
   there's no Blender.
2. **Port the rule** from Blender's source (`~/.cache/stat-fick-build/reference-src/blender-anim/`,
   Blender 5.2.0: `blenkernel/intern/anim_sys.cc` for the NLA, `nla.cc` for strip time,
   `armature.cc` for pose channels) and the glTF specification (`reference-src/gltf/
   Specification.adoc`, Appendix C), rather than tuning numbers until poses match.
3. **Test against the reference:** poses are compared as each bone's deformation in model space
   (its posed transform after the inverse of its rest transform, the same quantity the M6 pose
   references use), which doesn't depend on how either side orients a bone's own axes. Exact (to
   float precision) where both sides evaluate the same keys; within a measured tolerance where
   Blender evaluates its curves between the exported frames. Every rule gets a test that fails when
   the rule is broken (checked by breaking it).

## Rules from Blender and glTF (to port)

- **Sampling (glTF 2.0, Appendix C):** times in seconds from the animation's start; before the first
  and after the last key the output is clamped; a time equal to a key uses its value as is. STEP:
  the key before. LINEAR: per-component lerp, except rotations: slerp with a = arccos(|q_k · q_k+1|)
  and the next quaternion multiplied by the dot product's sign (the short way). CUBICSPLINE: three
  values per key (in-tangent a, value v, out-tangent b), v_t = (2t³ − 3t² + 1) v_k + t_d (t³ − 2t² + t)
  b_k + (−2t³ + 3t²) v_k+1 + t_d (t³ − t²) a_k+1, rotations normalised after. `weights` channels
  target morph targets, which the engine doesn't have (decision: bones only).
- **Pose channels** (`armature.cc`, `BKE_pchan_to_mat4`): a bone's pose is location, rotation and
  scale relative to its rest pose; its matrix is T(location) · R · S, the quaternion normalised before
  use (the stored value isn't). The rest-relative ("basis") value of a glTF channel is the node's
  local transform after the inverse of its rest transform; Blender's exporter writes local = rest ·
  basis (`blender/exp/animation/sampled/armature/sampler.py`, `correction_matrix_local`).
- **NLA blending** (`anim_sys.cc`): tracks are evaluated bottom first, each channel starting from its
  default (location 0, the unit quaternion, scale 1: the rest pose). A strip changes only the
  channels its action keys (the blend domain); the others pass through. Replace: lower·(1 − i) +
  strip·i per component, quaternions included (normalised only when the pose is built). Combine:
  locations lower + (strip − 0)·i; scales (proportional) lower · (strip / 1)^i; quaternions
  normalise(lower) · normalise(strip)^i (`pow_qt_fl_normalized`). Influence 0 copies the lower values.
- **Strip influence** (`nlastrip_get_influence`): linear ramps: (t − start) / blend-in while t ≤ start
  + blend-in, then (end − t) / blend-out from end − blend-out on, else 1 (unless the influence is set).
- **Strip time** (`nla.cc`, `nlastrip_get_frame_actionclip`): action frame = action start + fmod(t −
  start, action length · scale) / scale; at the strip's very end with a whole number of repeats it is
  the action's end (not its start). Reversed strips count back from the end.

## Steps

1. **Clips from glTF.** `GltfImport` reads the animations (channels, samplers; STEP, LINEAR,
   CUBICSPLINE) into clips; sampling per Appendix C into a `ModelPose`. A test figure with a rig like
   the game's (hips, spine, head, arms with hand sockets, legs) and actions animated at 60 fps with
   eased keys (and one using an IK constraint, baked by the export), exported sampled with unkeyed
   channels dropped. The Blender evaluation harness. Tests: poses at frames exact, between frames
   within the measured tolerance; hand-written files for STEP, CUBICSPLINE, clamping and channels on
   plain nodes.
2. **Rest-relative clips shared by bone names.** Clips hold rest-relative channels bound by node
   name; any model of the family plays them on its own rest pose. A second figure with other
   proportions and the same bone names; Blender plays the first figure's actions on it.
3. **Layers and cross-fades (Blender's NLA).** Tracks of strips (clip, start, speed, repeats or
   loop, hold, blend in/out, influence, Replace or Combine); keyed channels only; the engine's
   cross-fade is a strip blending in over the one below. Reference: the same NLA tracks in Blender.
4. **Events.** Pose markers exported as animation extras; events fired as playing strips pass them
   (loops, speed, blending). Firing rules Blender doesn't define are asked first.
5. **State machines.** The graph as data (parameters, layers, states, transitions, any-state, exit
   times, cross-fades), the runtime that turns it into NLA strips, a C# builder; transition logic
   unit-tested, a scripted run's strips checked against Blender.
6. **Gear.** Socket nodes; items drawn at a socket's posed transform; clothing skinned to the
   wearer's skeleton by bone names, deformed as Blender's Armature modifier deforms a mesh bound to
   another armature. Rendered references of an animated, geared figure (shading and ink) at a few
   times.
7. **Animated views.** Animators driven by the UI's frame clock (the same time as CSS and Web
   Animations, so battle choreography stays in step), a playback rate (the old game's battle pace),
   views redrawn only when a pose changed; a playing figure in a `surface` element against direct
   renders; cost per figure measured.

## Status

Started 2026-10-31 (decisions asked and recorded; nothing built yet).

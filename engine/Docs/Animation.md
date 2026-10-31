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
- **Fades of rotations** (asked during step 3): the engine blends the clips' unit quaternions by
  Blender's component rule. Blender's NLA mixes its quaternion F-curves' raw values, which aren't
  unit length between an action's keys (each component is interpolated: halfway through the test
  swing's first turn they're 0.79 long), so mid-fade, where both clips turn the same bone, Blender's
  NLA preview can differ from the game by a few degrees (4° on the test swing's 0.15 s fade). The
  layer tests bake the reference actions' rotation curves to a unit key per frame before Blender
  evaluates them, and check the NLA rule exactly. (Not exporting the raw lengths as extras, not
  slerp fades.)
- **Clip events** (asked during step 4): every acting strip fires its markers (influence above 0,
  also while fading in or out; held strips fire nothing); events are queued while the frame's
  animations update (in time order, every marker a long frame passes, each loop included) and
  dispatched in the same frame step as CSS animation events, also awaitable as Tasks; jumping a
  strip's time (seeking, restarting) fires nothing. A marker's name is the event, nothing more
  (the game's data decides what each name does).
- **State machines in detail** (asked during step 5): a machine has layers, each with its own
  states and transitions on its own NLA tracks (Replace or Combine per layer; a layer can be empty
  and play nothing). Changing state cross-fades within the layer; the outgoing clip fades out
  fully, so channels only it keyed go back to what the layers below give (Blender's NLA transition
  rule). Conditions: bool, int, float and trigger parameters (a trigger is used up by the
  transition that uses it, otherwise kept until used or reset); a transition's conditions compare
  them (==, !=, <, >, ≤, ≥) and must all hold, optionally only after the state's clip has played to
  a point (its end or a share of it); any-state transitions are checked before the current state's;
  the first that holds wins; a transition restarts the state its layer is already in only if its
  "may restart" flag is set (off by default). A transition starting during another cross-fade on
  the same layer fades over the current blend. A state's speed is a number or a float parameter's
  value, times the animator's rate. Cross-fade times are seconds of the animator's clock. The game
  also hears each layer's state entered (when its transition starts) and left (when the outgoing
  clip has faded out), delivered and awaitable like clip events. M7 defines the graph as data
  records built with a C# builder; M9 adds the pack form and loader, M11 the editor.
- **Clips during state machine fades** (asked during step 5): both play through (the outgoing
  keeps playing as it fades out, the incoming plays as it fades in), mixed by Blender's transition
  rule; Blender's transition strips apply that rule to held poses, and the tests check the rule
  against them with held clips. (Not frozen as Blender's transitions.)
- **Gear in detail** (asked during step 6): sockets are dedicated non-deforming bones named
  `socket.<slot>` (socket.hand.R, socket.hand.L, socket.back, socket.head, socket.hip.L …) parented to
  the bone that carries them; an item's origin is its grip and its axes line up with the socket's;
  clips may key a socket. Clothing worn by a body of other proportions deforms as Blender's
  Armature modifier deforms it (by the wearer's bones relative to the wearer's rest), so it fits
  only the body it was made on: garments are made per body type. Items may have their own bones and
  clips: when the wearer's state machine enters a state, the item plays its clip of the same name
  (if it has one) on the same clock.
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

## How it's done

- **Clips** (`Animation/AnimationClip.cs`): `GltfImport` reads each glTF animation into
  `ModelData.Animations` (`ModelAnimation`: channels with their node, path, interpolation, key times
  and values as the file stores them; morph target weights skipped). `AnimationClip.From` groups the
  channels by node into `AnimationTrack`s named after the node. A channel on a skin joint (a Blender
  bone) is turned into its rest-relative value at load (`Relative`): t_b = S⁻¹·R⁻¹·(t − t_rest),
  q_b = q_rest⁻¹·q, s_b = s / s_rest (cubic tangents through the linear part only); this commutes
  with sampling, as the rest is fixed. Other nodes' channels stay their local transform (Blender's
  objects have no rest). `AnimationCurve.Evaluate` samples Appendix C in double precision.
- **Skeletons and poses** (`Animation/Skeleton.cs`): a `Skeleton` finds a model's nodes by name
  (joints before other nodes of the same name); a `ClipBinding` binds a clip's tracks to them (tracks
  for missing nodes are left out); `PoseBuffer` holds each node's channel values and which are set
  (Blender's blend domain), rotations as four free numbers; `ToPose` builds the `ModelPose`: a bone's
  local transform is rest · T(t_b)·R(q_b normalised)·S(s_b), composed as translation, rotation and
  scale (exact while the rest scale is uniform; Blender's bones have none); unset channels are rest.

- **Layers** (`Animation/AnimationStack.cs`): an `AnimationStack` holds `AnimationLayer`s (NLA
  tracks, bottom first), each a time-ordered list of `AnimationStrip`s (Blender's strip settings in
  seconds: start, the clip's part, scale, repeat, reversed, blend in and out, a fixed influence or
  none, Replace or Combine, extrapolation). `AnimationLayer.StripAt` ports nlastrips_ctime_get_strip
  (ranges with both ends; Hold reaching back only on a layer's first strip; between strips the
  earlier one at its end unless it extrapolates nothing; outside a strip it's evaluated at its
  clamped start or end, so a held strip with a blend there has influence 0);
  `AnimationStrip.InfluenceAt` the linear ramps with Blender's setters' limits (blend-in at most the
  length, blend-out at most what the blend-in leaves); `ClipTimeAt` nlastrip_get_frame_actionclip.
  `Evaluate` clears the buffer to the defaults (rest), then per layer samples the acting strip's
  clip into a scratch buffer and blends it in where it's set: Replace per component, Combine with
  locations added, scales times (s / 1)^i and normalised quaternions times the upper's power. Times
  within 10⁻⁶ s count as one (`AnimationStrip.Epsilon`; Blender compares float frames, clip lengths
  come as float32 seconds). Sampled rotations are taken with w ≥ 0 before blending (glTF keeps no
  sign; Blender's keys within a half turn of rest have w ≥ 0). `AnimationStack.Add` puts a strip on
  a new top layer: a cross-fade is such a strip with a blend-in.
- **Events** (`ModelAnimation.Events`, `AnimationClip.Events`, `AnimationStack.CollectEvents`,
  `Animator`): the export script (`make_models.py`'s `export_markers`, called by `stack_actions`)
  writes each action's pose markers as its custom property `events`, a list of `{name, time}` (time
  in seconds from the action's first frame, where the exporter's slide to zero puts clip time 0),
  which the exporter writes as the animation's extras; `GltfImport` reads them from the JSON (BRUTAL
  drops extras), sorted by time (stable). `CollectEvents(from, to)` finds, per strip, each marker
  of its part once per pass (cycle; reversed strips pass it backwards) at Start + pass · period +
  offset, inside the strip's range (so never while held), where the layer's acting strip is this
  one and its influence is above 0; the interval is (from, to] (times within the epsilon count as
  one, so an event at a frame's time belongs to that frame); sorted by time, events within the
  epsilon of a run's first as one moment ordered by layer (bottom first), pass and marker. An
  `Animator` holds a stack and its clock (`Time`, `Rate`): `Advance(seconds)` queues the events
  passed, `Seek` jumps without, `DispatchEvents` delivers them to `Event` and to `Next(name)` tasks
  (continuations asynchronous: microtasks under a document). A document runs `IUiFrameClient`s
  (`UiDocument.AddFrameClient`): advanced by the time since their last frame right after the CSS
  animation timing update, their events dispatched right after the frame's CSS animation events
  (before frame callbacks), each delivery as game code (`EnterScript`/`ExitScript`: its microtasks
  run after it).
- **Fade layers** (`FadeLayer`, `FadeEntry`; `AnimationStack.Layers` holds `StackLayer`s: NLA
  `AnimationLayer`s and fade layers): entries oldest first, each a strip (or nothing) fading in
  linearly from `FadeStart` over `FadeDuration`. Evaluation is Blender's transition rule
  (nlastrip_evaluate_transition) applied in turn: each side is a copy of the layers below with
  its strip blended on (its mode; channels it doesn't key keep the lower values), and the running
  mix moves toward it by its weight on every channel either side sets (`PoseBuffer.MixToward`,
  Replace's component mix). Strips are held outside their ranges both ways and play through the
  fade. An entry's share is its weight times what each later entry hasn't covered (`ShareAt`); its
  markers fire while the share is above 0.
- **State machines** (`StateMachineDefinition.cs`: records and `StateMachineBuilder`;
  `StateMachine.cs`): the definition is validated (names resolve, conditions fit their parameters,
  every transition has conditions or an exit time, exit times only from states with clips). The
  runtime puts a fade layer per machine layer on top of the stack, each starting in its entry state
  at full weight. `Update(from, to)`, layer by layer: a changed speed parameter re-times the current
  strip at `from` keeping its place; then transitions in time order: candidates are the any-state
  transitions (not into the current state unless they may restart it), then the state's own; all
  conditions must hold (they're constant within an update); a transition with an exit time is due
  at the exact moment its state reaches it if that falls in the interval, any other (or one whose
  exit time had passed before its conditions held) at `to`, never before the last transition taken;
  the earliest wins, ties by priority; triggers it tests are reset; the new state becomes a fade
  entry (its clip as a strip from that moment: scale 1 / speed, looping forever or played once and
  held, the layer's blend mode) with an entered notice. `Prune(to)` drops entries a later fully
  faded-in entry covers, each with a left notice at the moment it was first covered (that entry's
  fade end). The `Animator` takes a machine: each `Advance` runs `Update`, collects clip events,
  then `Prune`, and queues notices and events together (at one moment: left, entered, clip events);
  `StateChanged`, `Entered(layer, state)` and `Left(layer, state)` deliver and await them like clip
  events.
- **Gear** (`Animation/Gear.cs`): `Socket` finds a `socket.<slot>` node; an item's transform is
  C⁻¹ (glTF object axes back to Blender's, (x, y, z) → (x, −z, y)), then the socket's posed world,
  then the wearer's transform: Blender's exporter keeps a bone's axes in its joint node but converts
  objects', so an item whose axes line up with its socket in Blender needs the conversion undone.
  `Garment` binds a clothing model's joints to the wearer's nodes by name and gives each matched
  joint its rest world times the wearer bone's deformation (rest⁻¹ · posed): Blender's Armature
  modifier on the wearer (on the body it was made on, glTF's skinning; on another body no refit);
  `Model.Draw(..., world: ...)` draws it from those matrices. `ItemAnimation` mirrors the wearer's
  machine layers into fade layers of the item's own stack: each wearer entry gets an item entry with
  the same fade, playing the item's clip named like the state (start, scale and repeat copied from
  the wearer's strip each time, so speed changes follow) or nothing. Gear is drawn with the wearer's
  owner (one model: one outline, one glow mask) and its hatch anchor (`Model.Draw(..., anchor: ...)`).
- **Animated views** (`Animation/AnimatedView.cs`): an `Actor` is a model with its animator,
  transform, palette, items (`ActorItem`: a model on a socket, optionally an `ItemAnimation`) and
  garments (`ActorGarment`); `Refresh` evaluates its pose (and its items') at the animator's time
  and says whether any channel value, its transform or its gear changed; `Draw` adds its draw items
  (the figure, items at their sockets, garments from their world matrices, all with the actor's
  owner and hatch anchor). An `AnimatedView` (an `IUiFrameClient` holding a `View3D`) advances its
  actors' animators each frame by the time since its last frame times its `Rate` (the battle pace;
  the old game's "slow" stretches durations by 1.5, a rate of 1 / 1.5) in the frame's animation
  timing update, so clips run on the clock CSS and Web Animations run on; it sets the view's items
  only when something changed (a held clip or a rate of 0 draws nothing new), and dispatches the
  animators' events and notices with the frame's animation events. The host shows the view in a
  `surface` element as any view (`SurfaceViews`).
- **Known differences from Blender's NLA:** fades of hand-keyed rotations (the user's decision
  above: Blender mixes raw, non-unit curve values between keys); rotations more than a half turn
  from rest (Blender blends the stored sign, the engine w ≥ 0; a Combine power of such a quaternion
  turns the long way in Blender); Blender's float32 quaternion power quantises rotations under about
  0.03° (the engine computes in double); constraints are baked into clips, so layering clips with IK
  blends the baked results where Blender would solve the IK again on the blended pose; a plain node's
  (object's) channel that some strip keys but none sets at the moment goes to Blender's defaults
  (location 0) in Blender and to its rest in the engine (game rigs animate bones).

## Reference harness

- **Test models with actions** (`Tools/BlenderReference/make_models.py`): `figure.glb`, a humanoid
  rig like the game's (hips, spine, chest, neck, head; upper arm, forearm, hand, thigh, shin, foot
  per side; a body of tubes skinned with two influences at most), with "idle" (2 s loop: the hips'
  location, the chest's rotation and scale, arm and head rotations), "swing" (0.6 s, fast eased
  rotations; a pose marker "hit") and "twist" (a 270° turn about a bone's axis; the head's keys
  CONSTANT, exported as STEP; the neck nods in the idle too); `stocky.glb`, a stockier model of the
  same rig family without actions (the same bone names, other proportions, the upper arms rolled
  30°, no neck: the head hangs on the chest; a beard bone no clip moves); `reach.glb`, a two-bone arm bent by an IK constraint toward a keyed
  target bone (only the target is keyed; the arm's rotations come from the solver, baked by the
  export). Scene frame rate 60 fps, keys from frame 1. Exported with `ANIMATED`: skins, animations
  in "ACTIONS" mode (each action on an NLA track of its own becomes one glTF animation), always
  sampled, every frame, "Force keeping channels for bones" off, slide to zero (each clip starts at
  0 s).
- **Blender's evaluation** (`Tools/BlenderReference/evaluate_animations.py`, run by
  `make-blender-references.sh` after the renders; scene names as extra arguments limit it): reads
  `Reference/anim-scenes.json` (written by `AnimationReferenceTests.TheSceneFileIsUpToDate` from
  `Animation/AnimationScenes.cs`), builds each scene's model again with `make_models.py`'s builder
  (imported as a module; a path of None skips the export), mutes the NLA tracks, makes the scene's
  action the active one and evaluates it at each time (`frame_set` with a subframe; frame = the
  action's first frame + time × 60). A scene with a `target` builds that model too and plays the
  model's action on the target's armature (its slot assigned explicitly); Blender finds each
  F-curve's bone by name (`pose.bones["name"]`) and applies the channels on that bone's own rest. Writes `Golden/anim-<scene>.json`: each bone's rest head and
  tail and, per time, its deformation in model space (posed after the inverse of rest, the armature
  object's world included) converted to glTF's coordinates (C·D·C⁻¹ with C: (x, y, z) → (x, z, −y);
  joint nodes keep Blender's bone axes, so a joint's glTF world is C times its Blender world).
- **Layered scenes** (`AnimationScenes`: `Layers`, lists of `StripScene`s): the harness removes the
  builder's NLA tracks and builds the scene's (`build_layers`: each strip created at its start frame,
  its action range from the clip's part, then scale, repeat, `frame_start_ui` to its exact start
  (`frame_start` would change its length), direction, blend type, extrapolation, blend in and out
  after the length, and a fixed influence by setting the one key of the influence F-curve that
  `use_animated_influence` creates, which otherwise holds 0). Before that, `bake_rotations` replaces
  every used action's quaternion curves by a unit key per frame (the "Fades of rotations" decision).
  Scene time 0 is scene frame 0. The test compares only the times where every acting strip samples
  its clip on a frame (within 10⁻⁴ of one), at least a third of the scene's; Replace stacks within
  the on-frame tolerance, joints a Combine strip rotates also within Blender's float32 quantum
  (2·√(2·2⁻²⁴) ≈ 6.9·10⁻⁴ rad times the point's distance from the joint: acosf near w = 1).
  `STATVENTURE_ANIM_DETAIL=1` prints every larger error.
- **Gear scenes** (`AnimationScenes`: `Items`, `Clothing`): the harness builds the items and parents
  each of their root objects to its socket bone as an artist would (Blender places a bone's children
  at its tail, so the parent inverse moves back by the bone's length; the root's own transform in
  the item is its `matrix_basis`, as `matrix_world` isn't updated until the depsgraph runs), and the
  tabard bound to the wearer's armature by its Armature modifier (made on the figure, also on the
  stocky armature: Blender's rule). Written per time: each item root's world as a glTF object
  (C · M · C⁻¹) and the tabard's evaluated vertices; `GearReferenceTests` compares item origins and
  axes and every tabard vertex (CPU skinning with the garment's matrices, matched by rest position).
- **Rendered gear** (`Scenes3D` "gear-swing", `GearedFigure`): placed models now take a placement
  matrix, node world matrices instead of a pose (written to the scene file as each joint's
  deformation) and a group; `render_scenes.py` joins a group's models for the glow mask and the
  ink and anchors their hatching at the group's first model's origin, and clears imported animation
  data (the importer's active action would override the scene's pose).
- **Times** (`AnimationScenes.TimesFor`): every other frame, the middle of every frame, a quarter
  into every fifth, and one time before the start and one after the end (clamping).
- **Comparison** (`AnimationReferenceTests.Compare`): on frames, every bone's rest head and tail
  through the engine's and Blender's deformations within 2·10⁻⁵ m (measured: under 6·10⁻⁷ m).
  Between frames Blender evaluates its curves and the engine interpolates samples h apart, which
  errs by at most h²/8 · f'': each bone's head and tail in its parent's posed frame (moved only by
  its own channels) may be off by 1.5/8 of the larger second difference of those points at the
  frames around, plus 2·10⁻⁵ m. Points in model space would mix a chain's bones, whose curvatures
  can cancel (an IK chain's end follows its smooth target while each bone's rotation bends), so the
  measure is per bone.
- **Hand-written files** (`AnimationClipTests`): what Blender's sampled export doesn't produce and
  the formulas themselves, against values worked out by hand: STEP holding, slerp the short way and
  exactly (24° a fifth of the way through 120°, which a normalised lerp misses by 2°), CUBICSPLINE
  with tangents times the segment's duration and rotations normalised, clamping to the first value
  (not its in-tangent), snorm16 rotations, a joint resting turned and scaled 2, a plain node's
  absolute channel, channels for nodes a model lacks.

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

Started 2026-10-31 (decisions asked and recorded); done 2026-10-31.

- **Step 1, clips from glTF (2026-10-31, 584 tests): done.** `ModelAnimation` in `GltfImport`,
  `AnimationClip`/`AnimationCurve`/`AnimationTrack`, `Skeleton`/`PoseBuffer`/`ClipBinding`;
  `figure.glb` and `reach.glb` with actions, `evaluate_animations.py`; scenes `figure-idle`,
  `figure-swing`, `figure-twist`, `reach-reach` and `AnimationClipTests`. Measured: on frames every
  bone within 0.0006 mm of Blender (the baked IK included); between frames up to 3.6 mm in the
  parent's frame (the swing's forearm, 9 mm at the hand in model space), at most 0.73 of the
  interpolation allowance. Found on the way: the exporter writes every quaternion with w ≥ 0, so
  samples change sign where a local rotation passes 180° (the swing's upper arm, the IK arm), which
  only the short-way slerp follows; Blender's IK solution moves like the square root of the
  target's motion where the chain starts fully stretched, which no sampling follows between frames
  (the test arm's target now starts within reach); judging between-frame errors by points in model
  space failed the IK arm, whose bones bend more than its end. Checked by breaking: slerp the long
  way, a normalised lerp, STEP interpolated, cubic tangents unscaled or rotations unnormalised,
  joint channels kept absolute, plain nodes made relative, the basis before the rest, translations
  not divided by the rest scale, no clamping and an off segment search each fail a test.
- **Step 2, clips shared by bone names (2026-10-31, 586 tests): done.** The binding by name and the
  channels on each model's own rest came with step 1; this step checks them against Blender:
  `stocky.glb` and the scenes `stocky-idle` and `stocky-swing` (the figure's actions on the stocky
  armature). On frames within 0.0005 mm, between frames at most 0.72 of the allowance. Checked by
  breaking: channels played as absolute local transforms (a naive by-name playback) pass every
  same-model scene and fail both stocky scenes; ignoring the target's rest rotation (its roll) fails
  them all.
- **Step 3, layers and cross-fades (2026-10-31, 592 tests): done.** User decision during the step:
  fades blend unit samples (Blender's NLA mixes raw, non-unit quaternion curve values between keys:
  4° apart mid-fade on the test swing; exporting the lengths or slerp fades were not chosen).
  `AnimationStack`, `AnimationLayer`, `AnimationStrip`, `ClipBinding.NodesDriving`; the figure's
  fourth action "pulse"; scenes `nla-replace` (an idle under a swing blending in and out),
  `nla-combine` (an idle and a pulse added over a held swing at fixed influences), `nla-timing`
  (parts, half and double speed, fractional and reversed repeats, holds both ways, blends on held
  strips, a part's end at a strip's end), `stocky-nla`; `AnimationStackTests` (blend limits, a
  cross-fade). Measured: Replace stacks within 0.001 mm of Blender, Combine within half its float32
  quantum (0.4 mm at the head where Blender drops a 0.01° rotation). Found on the way: Blender's
  quaternion curves aren't unit between keys (the decision above); its float32 power drops tiny
  rotations; frame classification and strip ends need a tolerance (float32 clip lengths); test
  clips that end where they start hide the strip-end rule, and blends only on unheld strips hide
  where held strips are evaluated (both scenes extended). Checked by breaking: Combine mixed like
  Replace, its rotation order swapped, scales added, locations mixed, influence ignored; Replace
  rotations slerped, smoothstep ramps, every channel replaced, Hold Forward reaching back, no Hold
  before a first strip, reversal ignored, the scale inverted, no strip-end case, the influence at
  the unclamped time, rotations taken with w ≤ 0 each fail a scene.
- **Step 4, events (2026-10-31, 609 tests): done.** User decisions: every acting strip fires,
  queued like CSS animation events (awaitable), names only. Markers exported as extras, read into
  clips; `CollectEvents`, `Animator`, `IUiFrameClient` in `UiDocument`'s frame steps. The figure's
  actions carry markers (the swing's whoosh and hit, two at one frame in the idle, the pulse's first
  and last frame); Blender's markers are written to the single-clip references and the clips'
  events must be them (`ClipEventsAreTheActionsMarkers`); `AnimationEventTests` covers the rules.
  Found on the way: a check that held strips don't fire was redundant (events lie inside the strip,
  where it's never held), and so is the end-of-strip check unless one step spans the end; events "at
  one time" differ by float32 noise, so same-moment ordering needs the epsilon. Checked by
  breaking: influence 0 firing, events at the previous frame's time firing again, reversed strips
  passing markers forwards, unsorted events, same-moment passes or layers out of order, markers past
  a fractional last pass, clip events before CSS animation events, deliveries not run as game code
  and seeking that fires each fail a test.
- **Step 5, state machines (2026-10-31, 622 tests): done.** User decisions (two rounds and two
  follow-ups): layers of states, Blender's transition rule for cross-fades, parameters with exit
  times, fades over the current blend, speeds from numbers or parameters, state notices, data
  records now, fades in seconds, restarts per transition (off by default), triggers kept until used,
  both clips playing through fades. `FadeLayer`, `StackLayer`, `PoseBuffer.CopyFrom`/`MixToward`,
  `StateMachineDefinition`/`Builder`, `StateMachine`, the `Animator`'s notices. Blender: transition
  strips made headless with the NLA editor's operator (the factory startup has a window; its largest
  area turned into an NLA editor), scenes `nla-transition` (Replace sides, the outgoing half a swing
  held mid-swing) and `nla-transition-combine`: within 0.001 mm and within the Combine quantum.
  `StateMachineTests`: validation, entry states, triggers at the update's time, exit times at their
  exact moment (also inside a frame), triggers kept, priority, restarts, all conditions, speed
  parameters re-timing, interrupted fades with every clip playing, notice order and awaiting.
  Found on the way: an outgoing clip held at its last frame was the rest pose, which hid the
  channel-union rule in the Replace scene (now held mid-swing); every exit fell on a frame, which
  hid exact exit moments (a 1.25× swing now exits at 29.8 frames); a transition due at the update's
  time could be placed just before a just-taken exit (now never before it), and notices need a
  stable sort; an entry is left as soon as any later entry is fully in (an interrupted fade's oldest
  side goes first). Checked by breaking: mixing only the incoming side's channels, sides not over the
  layers below, smoothstep fades, unheld strips, shares ignoring later entries, triggers never used
  up, the state's own transitions first, any-state transitions always restarting, exit transitions
  at the update's time, speeds taking effect at the update's end, any condition sufficing, left
  notices at the update's time, entered before left and clip events collected before the
  transitions each fail a test.
- **Step 6, gear (2026-10-31, 631 tests): done.** User decisions: dedicated `socket.<slot>` bones,
  clothing deformed as Blender's Armature modifier (garments per body type), items with bones and
  clips of their own played with the wearer's states. `Socket`, `Garment`, `ItemAnimation`,
  `StateMachine.StateNameOf`, `Model.Draw` world and anchor parameters; test models: socket bones on
  both figures, `sword.glb`, `tabard.glb`, `pennant.glb`; Blender scenes `gear-figure` and
  `gear-stocky` (items within 0.0012 mm, garment vertices within 0.0002 mm) and the rendered
  `gear-swing` (shading and ink match). Found on the way: the exporter converts objects' axes but not
  bones', so items need C⁻¹ at their socket; in the harness a fresh object's `matrix_world` is stale
  (use its basis); the importer's active action overrode the render scene's pose (the M6 models had
  no animations); a character with its gear is one model (M6's outline rule), so gear takes the
  wearer's owner and hatch anchor, and the harness groups them; at 30° the raised arm's top face
  was a sliver (turned to 40°); the flag needed the sun behind it before the hatch anchor could be
  tested. Checked by breaking: items without the axis conversion, garments by glTF's bind (passes
  on the figure, fails on the stocky one), the garment deformation in the other order, item clips
  ignoring the wearer's speed or fades, gear as a model of its own and gear hatched from its own
  origin each fail a test.
- **Step 7, animated views (2026-10-31, 635 tests): done.** `Actor`, `ActorItem`, `ActorGarment`,
  `AnimatedView`, `ItemAnimation.Evaluate`. `AnimatedViewTests`: the view is drawn on every frame a
  pose changes and not after a clip holds its last frame, again when the figure moves, not while the
  rate is 0; its picture is the figure at its clock's time byte for byte; its clock is the document's
  (an element animation started with it reads the same time each frame, and at the slow pace 1 /
  1.5 of it); the cost is measured. Measured on this PC (Ryzen, RX 9070): the frame's animation work
  (advancing, transitions, events, poses, draw items) 45-75 µs per geared figure (sword and tabard,
  two machine layers), 1.1-1.8 ms a frame for 24; a 1080p frame of the 24 figures 1.5 ms on the GPU
  (`STATVENTURE_MEASURE=1` runs that measurement). A 2016-class GPU is untested (as in M6). Checked
  by breaking: items set every frame, a moved figure not seen, the view's rate ignored and the pose
  a frame late each fail a test.
- **M7 done (2026-10-31, 635 tests).**

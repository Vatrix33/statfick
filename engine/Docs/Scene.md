# Scene and sequencer (M10)

The scene is a tree of nodes that places everything a 3D view shows: figures, stage objects, cameras,
the sun and particle emitters. The sequencer plays cutscenes on it: a sequence is a versioned record
of tracks (clips made in Blender, curves, cues for figures, cameras, light, particles, sound, music
and the UI) played on the document's frame clock, skippable to its end state, with a reduced variant
for reduced motion. M10 builds both and remakes one boss intro, Ji'ruan's, as a proof.

## Decisions (user, 2026-11-04)

Asked before the milestone started; every answer the recommended option except the scope (M10-2). In
full, with the options not chosen, in `Docs/Engine-Plan.md`, "Decisions for M10". What they leave
open is under "Open questions" below.

- **Authoring (M10-1):** a cutscene is a sequence record with tracks. The 3D motion (camera moves,
  objects and figures) is made in Blender and exported as glTF clips (an animated camera node too),
  which the sequence references; timing, title cards, music cues, effects and skip rules are
  sequencer tracks, authored in the editor (M11) or built in C# as state machines are.
- **Scope (M10-2, not the recommendation):** the engine (nodes, the sequencer, every track kind,
  tweening, the Blender checks, a test sequence using every track) and Ji'ruan's intro now, as a
  proof. The other 11 boss intros, the phase and special cinematics, the 12 spell vignettes and the
  banners are made at port time.
- **Skipping (M10-3):** skippable by default; a skip jumps to the end state (every track evaluated at
  its end: final transforms, the title card hidden, the music cue applied); a sequence can be marked
  unskippable; vignettes are sequences marked non-blocking; the game binds the button and key and
  decides when its combat-animations setting suppresses a sequence.
- **Reduced motion (M10-4):** a sequence may carry a reduced variant (a shorter duration or a cut
  list), played when the system's reduced-motion setting asks; one without plays as it is.
- **Nodes (M10-5):** a node tree (parent and children, local translation, rotation and scale, a world
  matrix) by Blender's object parenting rule as the glTF importer reads it; actors, cameras, lights,
  particle emitters and plain models attach to nodes; the animated view becomes a scene with a root;
  bone sockets stay for gear.
- **Tweening (M10-6):** Blender's F-curves (Bezier with auto and vector handles, linear, constant) and
  Blender's easing set (sine, quad, cubic, quart, quint, expo, circ, back, bounce, elastic; in, out,
  in-out), checked against Blender's evaluation with the existing harness.
- **Clock (M10-7):** real time on the frame clock at the sequence's own rate (1 by default),
  independent of the battle pace; paused while the document is hidden; the figures a sequence
  animates follow its rate.
- **Audio cues (M10-8):** scheduled on the audio clock from the sequence's start, sample-accurate,
  through M8's scheduling; a skip or a pause cancels pending cues.
- **Text and screen tracks (M10-9):** a track names UI elements and sets their text, classes and
  attributes at cue times; the look and motion stay in the game's stylesheet, animated by the UI as
  the old game's CSS keyframes did.
- **Data form (M10-10):** a versioned record through the M9 serializer, in `world.pak` under
  `world/cutscenes/<name>`, plus a C# builder like the state machines'.
- **The proof (M10-11):** Ji'ruan's intro: the engine's test figure stands in for the boss and the
  imps (a placeholder until the real models), the stage and camera are built in Blender, the rest is
  in the sequence.

Decided earlier (`Docs/Engine-Plan.md`, "User decisions" and M6–M9): banners and cutscenes are remade
in Blender (2026-10-16); every 3D view is a `surface` element, redrawn only when something changed
(M6); clips are glTF's sampled animation at 60 fps with no root motion, the game moving whole figures
(M7); clip events are pose markers copied into the animations' extras by the export script (M7);
sound effects are synthesized patches played with a pan and volume from the caller, the music
switches with music.js's fades (M8); data is versioned records in packs, `.glb` files imported one
entry each into `models.pak` (M9).

## What exists (checked against the code, 2026-11-04)

Nothing of M10 yet: no node tree, no sequence record or player, no keyed curve with handles or
easing, no audio scheduled at a time.

- **Figures and their view** (`Animation/AnimatedView.cs`): an `Actor` is a model with its
  `Animator`, one `Transform` matrix (model to world; setting it redraws), a palette, items on sockets
  and garments, and `Sounds` (clip event to effect). `Refresh()` evaluates its pose at its animator's
  time and says whether anything changed; `Draw` adds its draw items under one owner (one outline,
  one glow mask, the hatching anchored at its origin). An `AnimatedView` holds a `View3D` and a flat
  list of actors; as a frame client it advances every actor's animator by the frame's time times its
  `Rate` (the battle pace), sets the view's items only when something changed, and dispatches the
  animators' events with the frame's animation events. No nodes, parents, cameras, lights or
  particles in it: the view's camera, sun and particles are set on the `View3D` directly. Its users:
  `AnimatedViewTests` and `EffectTests` (no engine code, no sandbox).
- **Animators** (`Animation/Animator.cs`): a stack (and its state machine) on a clock: `Time`, `Rate`
  (0 holds it), `Advance(seconds)` (transitions, clip events passed, notices), `Seek(time)` (a jump
  that fires nothing), `DispatchEvents`, `Event`, `StateChanged`, awaitable `Next(name)`,
  `Entered`, `Left`; also a frame client itself. Strips (`AnimationStack.cs`) have Blender's settings
  (start, part, scale, repeat, reversed, blend in and out, influence, Replace or Combine,
  extrapolation); `AnimationStack.Add` puts a strip on a new top layer (a cross-fade).
- **Clips** (`Animation/AnimationClip.cs`, `Skeleton.cs`): `AnimationCurve` is a glTF channel's
  sampler (STEP, LINEAR, CUBICSPLINE by Appendix C, in double precision); the name is taken.
  `AnimationClip.From` groups channels by node name; a joint's channels are made rest-relative, a
  plain node's (a Blender object's) stay its local transform. `Skeleton` finds a model's nodes by
  name, `ClipBinding` binds a clip to them; both work on one `ModelData`'s nodes, not on anything
  outside a model.
- **Views** (`Render3D/View3D.cs`): a scene's items (`DrawItem`s), particles (a list of `Particle`s
  already evaluated), `Camera`, background, `Sun`, `Style` and `Time` (the water's waves); renders
  only when a property was set, the camera's settings differ, the size changed or it was invalidated;
  picking by owner.
- **Cameras** (`Render3D/Camera.cs`): position, target and up (`View` is `CreateLookAt`), perspective
  by a vertical field of view or orthographic by the height in view, near and far, reversed depth;
  the vertical extent stays fixed when the aspect changes (Blender's sensor fit vertical). No
  rotation or matrix input: a camera node's world matrix has to become a position, a target and an up.
- **Light and style** (`Render3D/Renderer3D.cs`): one `SunLight` per view (direction toward the sun,
  strength as Blender's, angle 3° for soft shadows) and a `CelStyle`; no point or spot lights. A
  `DrawItem` has no opacity: models are drawn opaque (only particles fade, and glow).
- **Particles** (`Render3D/Particles.cs`): `ParticleEffect`, `ParticleShape`, `ParticleSpawn` and
  `ParticleRange` are plain C# records (no `[Record]`: not in any pack); a `ParticleEmitter` (effect,
  seed, start, burst, rate, duration) gives the particles alive at any time from their age
  (`At(time, palette)`: closed form, deterministic, rewindable, as M6 decided); its `Path` is a
  `Func<float, Vector3>` (a fixed point or a moving object), so it isn't data either.
- **Surface elements** (`UI/SurfaceViews.cs`): `Show(element, view)`, `Hide`, `ViewOf`; views of laid
  out elements update before painting at their content box's size and the document's zoom; `Pick`
  maps a viewport point through the element's transforms.
- **The glTF importer's nodes** (`Render3D/GltfImport.cs`, `ModelData.cs`): `ModelNode` records (name,
  parent, children, local matrix and its translation, rotation and scale, mesh, skin, `Ink`), the
  scene's roots, and `WorldMatrices(pose)` (local × parent, from the roots). **Cameras and lights are
  not read:** BRUTAL's glTF schema has them (`Gltf.Cameras` with perspective `Yfov`, `AspectRatio`,
  `Znear`, `Zfar` and orthographic `Xmag`, `Ymag`; `Node.Camera`; `Gltf.LightData` and a node's light
  for `KHR_lights_punctual`), but `GltfImport` drops them, so a camera node arrives as a plain node.
  The model record (`model` version 1, `model.node` version 1) has no field for them: adding one is a
  new version with a frozen copy of version 1 and an upgrade (M9 Q10, Q13).
- **Reduced motion** (`Platform/SystemSettings.cs`): `ReducedMotion` is GTK's animations off (Linux)
  or client-area animations off (Windows), as Chromium takes it. `UiHost` reads it once when it starts
  and passes it to the document's media environment (`SetViewport(width, height, reducedMotion)`), so
  `prefers-reduced-motion` media queries see it; a change while the game runs isn't seen.
- **The frame clock** (`UI/Dom/UiDocument.cs`, `UiDocument.Scripting.cs`): frame clients
  (`AddFrameClient`) are advanced right after the CSS animation timing update by the time since their
  last frame (nothing on their first) and their events dispatched right after the frame's CSS
  animation events, as game code. Web Animations (`element.Animate`) and CSS animations run on the
  document timeline (Blink's AnimationClock). The engine has `animation-play-state` (CSS) and, for
  script animations, `Pause`, `Play`, `CurrentTime` and `StartTime`, but no playback rate (left out
  in M5).
- **Hidden documents** (`UiDocument.Visibility.cs`): a minimized window's document gets no frames, so
  frame clients aren't advanced while hidden, and on the first frame after it is shown again they are
  advanced by the whole hidden time at once: an `AnimatedView`'s figures jump forward, as the CSS and
  script animations do (their timeline is real time, as Chromium's). M10-7's pause while hidden needs
  the sequencer to drop that gap. (Not written in `Docs/Animation.md` or `Docs/Scripting.md` so far.)
- **Audio** (`Audio/AudioSystem.cs`, `Audio/Music/Soundtrack.cs`, `Audio/Effects/SoundEffects.cs`):
  the game's calls are queued and applied by the audio thread before its next quantum, at the graph's
  time then: `Music.Play(id)` (the old bus held and ramped to 0 over 0.55 s, the new one from 0 to 1
  over 0.8 s, its first step 60 ms later; the theme playing does nothing), `Music.Stop()`,
  `Effects.Play(id, volume, pan, pitch)` (a voice started at the graph's current time). **Nothing
  plays at a given time, and the audio clock can't be read from the game** (`AudioGraph.CurrentTime`
  and `Soundtrack.Schedule()`, the per-quantum note scheduler, are internal). Inside, the graph is
  sample-accurate (sources' `start(when)`, automation at any time), so M10-8 needs a public call
  that schedules at a time, the clock to schedule against and a way to cancel. `AudioSystem.Pause()`
  freezes the graph (the application does it while the window is minimized), so the audio clock
  stops with a hidden document. Ji'ruan's theme `great-imp-jiruan` is in `audio.pak`.
- **Records and packs** (M9; `Docs/Assets.md`): `[Record(name, version)]`, `[Field(n)]`, unions by
  `[Subtype(tag, type)]` (Q12; no engine type uses one yet, only the serializer's tests), layout
  signatures and fixtures per type in `Tests/Statventure.Tests/Data/Records/` (one file or folder per
  type), `VirtualFileSystem.Load<T>(path)`, `Application.Data`; a loose `.json` that starts with
  `"$types"` packs as a record without an importer (S3-1); `world.pak` is mounted when present, and
  none is committed (Q1: no game content yet). `StateMachineBuilder` (fluent, by name, `Build()`
  validates) is the builder to copy.
- **The Blender harness** (`Tools/BlenderReference/`, run by `Tools/Scripts/make-blender-references.sh`
  on the PC; its outputs committed): `make_models.py` builds and exports the test models
  (`export(path, **ANIMATED)`: skins, actions mode, sampled every frame at 60 fps, slide to zero,
  extras on; `export_markers` copies pose markers into the action's custom property `events`);
  `render_scenes.py` renders `Reference/r3d-scenes.json`'s scenes with EEVEE and Freestyle (models
  placed by matrices, poses as joint deformations, the camera from look-at axes, the sun, particles
  as quads); `evaluate_animations.py` rebuilds models from `make_models.py`'s builder and writes what
  Blender evaluates at given times (poses, NLA stacks, items parented to bones with their parent
  inverse); `compose_glow.py` and `make_terrain.py`. Nothing evaluates a plain F-curve, a hierarchy
  of objects or a camera yet.
- **Blender's source in the cache** (`~/.cache/stat-fick-build/reference-src/blender-anim/`, 5.2.0):
  `blenkernel/intern/fcurve.cc` (F-curve evaluation: the key search with a 0.0001-frame threshold,
  constant, linear, Bezier through `BKE_fcurve_correct_bezpart` and `findzero`/`solve_cubic`, every
  easing through `BLI_easing_*`, extrapolation, handle recalculation) is there; the auto handles
  themselves (`BKE_nurb_handle_calc_ex`, `blenkernel/intern/curve.cc`) and the easing functions
  (`blenlib/intern/easing.cc`) are not yet; nor is the glTF exporter. From `fcurve.cc`: Blender's
  "automatic" easing is ease out for back, bounce and elastic and ease in for the others; back takes
  an overshoot, elastic an amplitude and a period, all per key.

## The old game's cutscenes (facts)

Read from `old-game/scripts/boss-intros.js`, `combat.js` (lines 790–910, 1251, 6074–6098),
`tier10-cinematics.js`, `index.html` (600–685) and `styles.css` (from "Boss intro cutscenes", line
1589; the tests' copy has the same section).

- **The kinds.**
  1. *Boss intros* (`BossIntros`, the `INTROS` table): 12, one per Great Hunt boss whose enemy file
     names an `introCinematic` (Ji'ruan, Juila, Tankina, Saffron, Gaian, Anatemis, Ur-Mat, Ool, Ornn,
     the Deep Sea King, Zik Lun, Nygheir), 7.0–8.4 s (Ji'ruan 7.0). Each entry: `duration`,
     `location`, `overture`, `label`, `title` (two parts, the second in an `<em>`), `description`,
     `imps` (how many retinue figures) and `retinue` (enemy names whose battle looks they wear), and
     `scene()`, an inline SVG drawn around the boss's own battle sprite.
  2. *Phase cinematics*: 7 more entries of the same table played through the same player when a
     boss's phase names one (`tankina-phase`, `anatemis-golden`, `anatemis-zenith`, `ur-mat-lucid`,
     `ool-phylactery`, `ornn-phase`, `nygheir-phase`; 7.2–7.8 s), showing the boss's look from before
     the change beside the new one (`before: true`).
  3. *Special cinematics*: `boss-special-cinematic` (three themes: storm-talos, false-dreamer,
     forge-god; 3.4 s, reduced 1.2 s) and Anatemis's star cinematic (4.8 s, reduced 1.5 s, also ended
     by its curtain's `animationend`). Both are fallbacks for a phase without a `cinematic` key; in
     today's enemy files every phase with a cutscene names a phase cinematic, so neither is reached.
  4. *Spell vignettes* (`TierTenCinematics`): 12, one per tier-10 spell (a symbol, a name and a
     caption over the battle), 1.9 s (reduced 0.65 s with every animation off), non-blocking: the
     spell's results apply at once, `role=status`, no skip button; the end is a timer "so a hidden
     tab cannot trap the UI".
  - The shop, tower and guild *banners* (`scene-banners.js`) are animated interior scenes, not
    cutscenes; remade in Blender at port time with the rest.
- **Playing and skipping** (`combat.js`): a boss intro plays once when the encounter starts, never
  when a saved battle resumes; playing one first stops any playing (`stopBossIntro(false)`); the
  overlay is shown, `is-playing` added after a forced layout (so the CSS animations restart), the
  skip button focused, and a timer of the duration ends it. While it plays the AI is held: the enemy
  timer waits until 100 ms after `bossIntroUntil` (`scheduleAI`, line 1251). The skip button
  (`#boss-intro-skip`) and Escape on the overlay end it at once (`stopBossIntro()`: hidden,
  `is-playing` removed, the battle drawn, the AI scheduled, focus back to the combat sheet button or
  the view toggle). A battle that ends (or a flight) stops it too.
- **Combat animations off** (`GameSettings`' `combatAnimations` false, "Turn off spell cinematics,
  boss transitions and battle UI animations"): no intro, phase cinematic or vignette plays, and
  turning it off stops the one playing.
- **Reduced motion** (`matchMedia("(prefers-reduced-motion: reduce)")`): a boss intro or phase
  cinematic lasts 2400 ms instead of its duration, and the stylesheet turns every animation in it off
  (`animation: none !important`), hides the overture and the retinue and shows the boss with its
  glow: a still title card for 2.4 s (its progress bar stays empty). Some intros add rules of their own
  (Nygheir's phase hides its memories and old look). Not a shortened animation: other content.
- **A minimized window:** the end is a timer, which keeps running (throttled to whole seconds) while
  the page is hidden, so the old game's intro ends while minimized; the CSS animations jump forward
  on restore. The engine's sequence waits instead (M10-7).
- **Music:** cutscenes have no sound of their own. The battle's theme (Ji'ruan's
  `great-imp-jiruan`) starts with the encounter (`Music.sync()` right after `playBossIntro()`), so it
  fades in under the intro.
- **The markup** (`index.html` 678–685): `#boss-intro-cinematic` (`role=dialog`, `aria-modal`, labelled
  by the title and described by the description; `data-boss` set to the key, `--bi-duration` to the
  duration), holding `.boss-intro-scene` (the SVG and the sprites), `.boss-intro-flash`,
  `.boss-intro-vignette`, `.boss-intro-top` (`#boss-intro-location`, the skip button "Skip ↗"),
  `.boss-intro-caption` (`#boss-intro-overture`, `#boss-intro-label`, `#boss-intro-title`,
  `#boss-intro-description`) and `.boss-intro-progress` (a `span`). The letterbox is the overlay's
  `::before` and `::after`: bars `clamp(26px, 7vh, 74px)` tall in `#030206`. Ji'ruan's colours: accent
  `#c58cff`, ink `#f3eaff`, background `#05030a`; the title in STIX Two Text, the rest in Lato.
  Its texts: "THE GREAT ALDERWOOD · GREAT HUNT"; "The void opened beneath the Alderwood, and the imps
  came crawling after it."; "LORD OF THE IMP COURT"; "Great Imp " + *Ji'ruan*; "Crown of the Void —
  while his court stands, no blade can reach him."
- **Ji'ruan's intro, element by element** (7.0 s; the timeline's animations are `var(--bi-duration)`
  long with fill `both`, each timing function applied between each pair of keyframes; times in
  seconds from the start):

| Element | What it does |
| --- | --- |
| Overlay (`bi-curtain`, ease-in-out) | fades in 0–0.35, stays to 6.51, fades out by 7.0 |
| Letterbox bars | still, the whole time |
| Camera (`.bi-art`, `bi-camera`, ease-in-out) | the whole scene scales 1.02 → 1.1 and rises 1.5 % about (50 %, 62 %): a slow push-in over 0–7.0 |
| Trunks (far, mid), ground, near silhouettes | still (moved only by the push-in); some trunks rimmed in the rift's light |
| Rift (`bi-rift-open`, ease-out) | closed (scale 0.4 × 0) and hidden to 0.42, visible by 0.84, open (1 × 1) at 1.96, widened to 1.5 × 1.04 at 3.92, settles at 1.25 × 1 (opacity 0.9) at 7.0; its bloom pulses 0.55 ↔ 1 opacity, 1.4 s each way, forever |
| Halo (`bi-halo`, ease-out) | hidden to 0.56, 0.85 at 2.24, full at 4.06, 0.6 at 7.0 |
| Cracks in the ground (`bi-cracks`, ease-out) | hidden to 1.26, appear by 1.54, drawn out along their length until 2.94, then stay (0.9) |
| Eight eyes in the dark (`bi-eye`, ease-out, each delayed 0–1.3 s) | hidden to 0.98, open by 1.4, blink (squashed to 0.1 tall) at 1.68, open 1.89–4.9, dim to 0.35 by 5.95 (each plus its delay) |
| Mist (`bi-mist`, ease-in-out) | drifts −3 % → +3 % across, opacity 0.7 → 1, over 0–7.0 |
| Four imps (`bi-imp-fly`, ease-out; delays 0, 0.15, 0.35, 0.5 s) | hidden and tiny (scale 0.15) at the centre to 1.68, visible by 2.1, at their places by 3.36 (imp 1 at −36 vw, −14 vh; 2 at 31 vw, −19 vh; 3 at −23 vw, 9 vh; 4 at 25 vw, 11 vh; 1 and 3 mirrored), bob up 2 vh at 4.34, back at 5.32, up 1 vh at 7.0 (each plus its delay) |
| Boss (`bi-boss-rise`, ease-out) | hidden, 16 % low, scale 0.9, a black silhouette with a violet glow, to 2.1; visible, 4 % low, 0.96 at 2.94; still a silhouette at 3.92; lit (brightness 1.15) at 4.48; in place, 1.03, normal brightness at 7.0 |
| Flash (`bi-flash`, ease-out; a radial white-violet light, screen blend) | none to 3.85, 0.95 at 4.13, gone by 4.9 |
| Vignette | still |
| Location (top left, opacity 0.78) and skip button (top right) | still, the whole time |
| Overture (`bi-overture`, ease-in-out) | hidden 10 px low to 0.49, shown 0.91–2.66, gone by 3.22 |
| Label, title (+0.12 s), description (+0.3 s) (`bi-card`, ease-out) | hidden 18 px low (letter-spacing 0.02 em) to 4.06, in place by 4.76, stay |
| Progress bar (`bi-progress`, linear) | grows from nothing to the full width over 0–7.0 |
| End | a 7000 ms timer hides the overlay; the AI goes on 100 ms later |

## Method

Three kinds of rule, three kinds of check:

1. **What Blender defines, against Blender 5.2** (as M6 and M7): F-curve evaluation (keys, handles,
   interpolation, easing, extrapolation), Blender's object parenting as the exported hierarchy gives
   it back, a stage's clip on its objects, the camera's place and field of view and the sun's
   direction, and the stage's look at chosen moments. A test describes the case (curves, a
   hierarchy, a stage and its times) in a committed JSON file under `Tests/Statventure.Tests/Reference/`;
   a Blender script rebuilds it from its own source (curves as Blender keyframes; hierarchies,
   cameras and stages from the script that exported them, not from the `.glb`, so Blender evaluates
   its own F-curves) and writes what Blender evaluates (`Golden/*.json`) or renders
   (`Golden/r3d-*.png`, `render_scenes.py`). Both are committed, so the tests run without Blender
   (the cloud). The rule is ported from Blender's source (`fcurve.cc`, `curve.cc`, `easing.cc`; the
   glTF exporter's camera and light conversion), not fitted. Exact where both sides compute the same
   values in the same precision; where the engine samples a clip Blender evaluates as curves, within
   M7's between-frame allowance; renders compared as M6 compares them.
2. **The sequencer's own rules**, which nothing outside defines: tests for each (cue order, the end
   state on a skip, pause by the game and by a hidden document, the rate, seeking, the reduced
   variant's mapping, bindings, unskippable and non-blocking, the end), and the record's round trips
   in both forms with its layout signatures and fixtures (M9's guards).
3. **Sound and UI through their own references:** audio cues by offline renders against a
   hand-scheduled graph (the same patches and themes started directly at the sample times the
   sequence should give them: equal sample for sample; a cancelled cue leaves the rest equal and adds
   nothing); text and screen tracks through what the UI already matches with Chromium (M4's CSS
   animation fixtures, M5's Web Animations, the 22 game screens), the sequencer's tests checking
   which attribute, class and text changes land in which frame and what the end state is. The proof's
   title card can also be compared with the old game's in Chromium (Q32).

Every new test is shown able to fail by breaking each rule it covers (`Tools/Scripts/mutate.py`).
Steps that make Blender references run on the user's PC (the cloud has no Blender).

## Design (proposed)

The design the steps work from; names are working names. "(Qn)" points to the open question that
settles the point.

### The node tree and the scene (step 1)

- **A node** has a name, a parent and children, a local translation, rotation (a unit quaternion)
  and scale (glTF's T·R·S; in System.Numerics' row vectors S·R·T) and a world matrix (local × the
  parent's world), recomputed lazily when a local transform above it changed. Paths name nodes from a
  given node (`stage/rift`). Optional: a node shown or hidden with its subtree (Q23).
- **Attachments:** an `Actor` (M7's figure; its model matrix is its node's world matrix, its gear
  stays on its sockets), a placed model (a `Model` with its palette, scenery flag, optional pose and
  its ink group), a camera and a sun light (step 3), a particle emitter (step 5) and possibly terrain
  (Q3). A node can carry several.
- **The scene** (what `AnimatedView` becomes, Q1): a root node, the `View3D` it fills, and a frame
  client of the document: each frame it advances the actors no sequence holds by the frame's time
  times its `Rate` (the battle pace, as now), brings world matrices up to date, and gives the view new
  draw items only when a pose, a node, an attachment or the list changed (as `AnimatedView` does
  today), the view's camera and sun from their nodes, and its particles; then it dispatches the
  animators' events with the frame's animation events.
- **Models in the scene:** instancing a model under a node makes a scene node for each glTF node
  (names and hierarchy kept) with the meshes as placed models (Q2); a skinned figure stays one
  `Actor` (its bones aren't scene nodes). A clip of the model (a Blender object's action, absolute
  local channels) binds to those scene nodes by name, as `ClipBinding` binds to a model's nodes, and
  sets their local transforms.
- **Blender's rule as glTF gives it back:** glTF's node rule is world = parent's world · T·R·S; what
  Blender adds (an object's parent inverse, a bone parent placing a child at the bone's tail, the
  axis conversion of objects) the exporter bakes into the child's local transform. Step 1 checks the
  cases where the two could part: parent inverses, a bone parent, non-uniform scale above a turned
  child (a sheared world), a mirrored branch, an animated parent above a still child.

### Curves (step 2)

- **`FCurve`** (Blender's name; `AnimationCurve` is the glTF sampler's): keys in time order, each with
  a time, a value, the interpolation to the next key (constant, linear, Bezier, or an easing type:
  sine, quad, cubic, quart, quint, expo, circ, back, bounce, elastic), the easing mode (in, out,
  in-out; Blender's automatic resolved per type as `fcurve.cc` does), back's overshoot and elastic's
  amplitude and period, and two handles (time and value) with their types (free, aligned, vector,
  auto, auto-clamped: Blender's default for new keys; Q6); per curve the extrapolation (constant,
  linear) and Blender's auto smoothing (none, continuous acceleration). Modifiers (Q7).
- **Evaluation** as `fcurve_eval_keyframes_interpolate` and `_extrapolate`: the key search with its
  threshold, a time on a key giving the key's value, Bezier segments corrected
  (`BKE_fcurve_correct_bezpart`) and solved for the time (`findzero`), easings as `BLI_easing_*` of
  (time, begin, change, duration). Auto handles as `BKE_fcurve_handles_recalc` computes them.
  Precision (Q5) and time unit (Q4).
- **Tracks use one curve per component**, as Blender's F-curves have an array index: location x, y,
  z; rotation (Q8); scale x, y, z; and single values (a camera's field of view, the sun's strength,
  an effect's volume).

### The sequence record (step 4; layout Q14)

Proposed records, `scene.*` (in `world.pak` at `world/cutscenes/<name>`, M10-10):

- **`scene.sequence`:** the name; the duration (seconds); `skippable` (true by default) and
  `blocking` (true by default; a vignette false) (Q20); the roles it needs, each a name and a kind
  (actor, node, model, element) that the game binds when it plays it (Q15); the stages it brings
  (model paths, instanced under the scene's root when it plays: Q2, Q14); its markers (name, time;
  Q22); its tracks, in order (a union of track kinds); its reduced variant (none, or Q19's form).
- **Track kinds** (tagged subtypes of `scene.track`, M9 Q12):
  - *clip:* a stage's clip on that stage's nodes, with strip settings as `AnimationStrip` has them
    (start, part, scale, repeat or hold);
  - *transform:* a node and curves for its channels;
  - *visibility:* a node and cues (time, shown) (Q23);
  - *animation:* an actor role, NLA strips on a top layer of its stack (clip, start, part, scale,
    repeat, blend in and out, Replace or Combine), its mark node, parameter and trigger cues (Q16);
  - *palette:* a model or actor and cues (time, palette) (Q24);
  - *camera:* cuts (time, camera node) and a field-of-view curve (Q11);
  - *light:* cues (time, light node as the view's sun) and strength and angle curves (Q12);
  - *particles:* cues (time, effect, node to emit from, seed, burst, rate, duration) (Q25);
  - *audio:* cues (time; an effect with volume, pan and pitch, a music theme, or the music's stop);
  - *ui:* cues (time, element, action: set text, add or remove a class, set or remove an attribute);
  - *event:* cues (time, name), delivered to game code.
- **State and moments:** every track gives a state at any timeline time τ (a curve's value, the last
  cue at or before τ, a clip's or strip's pose at τ), so the end state is the tracks evaluated at the
  duration and seeking is evaluation; cues that are moments (events, sounds, particle starts, clip
  events of the figures) happen when the clock passes them.

### The builder (step 4)

A `SequenceBuilder` as `StateMachineBuilder` is: fluent, everything by name, `Build()` validating
(roles and nodes named exist in the stages or roles, keys in order, cue times within the duration,
the variant within the timeline). For example (names to be settled in the step):

```csharp
var intro = new SequenceBuilder("jiruan", duration: 7.0)
    .Role("boss", RoleKind.Actor).Role("imp", RoleKind.Actor).Role("card", RoleKind.Element)
    .Stage("models/stages/jiruan")
    .Clip("jiruan", "Scene", start: 0)
    .Animation("boss", mark: "jiruan/mark.boss", a => a.Strip("idle", start: 0, loop: true))
    .Music(0, "great-imp-jiruan")
    .Ui("card", ui => ui.Text(0, "#boss-intro-label", "LORD OF THE IMP COURT").AddClass(0, "is-playing").SetAttribute(7.0, "hidden", ""))
    .Reduced(r => r.Hold(at: 6.5, length: 2.4))
    .Build();
```

### The player (steps 4–6)

- **Playing:** the scene plays a sequence with its bindings and gives a player: a frame client of the
  scene's document. `Rate` (1 by default; independent of the scene's battle pace, M10-7), `Pause()`
  and `Resume()` (the game's), `Seek(time)` (a jump: no cue fires, as `Animator.Seek`), `Skip()` (to
  the end state; refused when unskippable, Q20), `Time`, `Duration`, whether it plays the reduced
  variant, `Finished` (a task completing at the end, saying whether it was skipped), cue events
  delivered as game code and awaitable as clip events are (`Next(name)`).
- **Its clock:** advanced by the frame's time times the rate, not while paused, and not while the
  document is hidden: the gap the first frame after showing would bring is dropped (What exists,
  "Hidden documents"). The timeline time τ is the clock mapped through the reduced variant's cut list
  when it plays it (Q19).
- **The reduced variant** is chosen when it starts, from the document's media environment (what
  `prefers-reduced-motion` sees, so the 3D and the stylesheet's reduced-motion rules agree).
- **Each frame**, in the animation timing update: the clock moves; every state track is evaluated at
  τ into its nodes, actors, camera, sun, palettes and UI elements (Q17 for the order); the actors it
  holds are advanced by the change in τ (so their clip events fire; their own clock otherwise waits);
  the moments passed are queued and dispatched with the frame's animation events (Q17); particles are
  evaluated at τ; audio cues are kept scheduled ahead on the audio clock (step 6).
- **The end:** at τ = duration the end state is reached and stays (Q21), `Finished` completes, the
  actors it held go back to the scene's clock.
- **A skip:** τ jumps to the duration: state tracks evaluated there; event cues as Q18 says; pending
  audio cues cancelled, music as Q28 says; particles as Q26 says.
- **Each track kind's playback** is a class of its own behind one interface, so steps 5 and 6 add
  playback without editing the player.

### Audio cues (step 6)

- The audio system gets a clock the game can read and calls with a time on it: an effect played at a
  time, the music switched (with music.js's fades) or stopped at a time, each giving a handle that
  cancels it while it hasn't started (its sources stopped before their start, its timers dropped).
  The audio thread builds the voice as now but starts it at that sample (the graph's `start(when)` and
  automation already are sample-accurate).
- A cue at timeline time τ_c is due at the anchor (Q27) plus (τ_c − τ at the anchor) / rate; a
  pause, a seek, a rate change or a skip cancels the pending cues and schedules them again from the
  new time (a pause on resuming). The sounds themselves aren't stretched or pitched by the rate.
- A minimized window pauses the audio system (M8) and so its clock, as the sequence pauses (M10-7).

### UI tracks (step 6)

- Elements are found through the bindings (Q15); cues apply in the frame's animation timing update
  with the other tracks (Q17), so their style changes show in that frame, and a class added there
  starts its CSS animations at that frame's time (as the old game's `is-playing` did on the next
  frame).
- The state of each element is the last cue at or before τ: the end state of Ji'ruan's intro is the
  host hidden.
- Pausing, a hidden document, a rate other than 1 and seeking reach CSS animations only as Q29 says.

### The Blender export of a cutscene stage (steps 3, 7)

- **The source** is a Blender script per stage (as `make_models.py` is the test models' source; for
  the game's cutscenes at port time, where the sources live is the port's question): meshes in
  palette-slot materials with the `ink` and `hatch` custom properties (M6); empties as marks where
  figures stand (`mark.boss`, `mark.imp.1` …), animated for whole-figure motion (M7: no root motion,
  the game moves whole figures); cameras; a sun; all animated on the scene's timeline at 60 fps;
  timeline markers naming moments (Q22).
- **The export:** M7's `ANIMATED` settings plus cameras and lights, the animation mode (Q13), the
  lighting units (Q12); the script copies the timeline markers into extras as `export_markers` does
  for pose markers (Q22). Blender's cameras and glTF's both look down their local −Z, but the
  exporter's axis conversion turns objects' axes, so it corrects cameras and lights (to check in its
  5.2 source, as M7 checked the item axes, C⁻¹).
- **The import:** the `.glb` into `models.pak` as every model (M9 Q20), with its cameras and lights
  (Q9). When a sequence plays, its stages are instanced under the scene's root, its clip track plays
  the stage's clip on them, its camera and light tracks pick their nodes, and the bound actors stand
  on their marks.

### The proof: Ji'ruan's intro (step 7)

- **3D** (the old SVG scene and sprites): a stage built in Blender (Q31): the trunks, the rift opening
  and pulsing in its glow, the cracks, the eyes, the mist, the ground and near silhouettes, marks for
  the boss and four imps (the test figure `figure.glb` on each, in palettes of their own), a camera
  pushing in over the 7 s; the reveal (Q24) and fades (Q23).
- **UI** (as the old markup and stylesheet): the overlay with its letterbox, flash, vignette,
  location, skip button, caption and progress bar, from `old-game/index.html` 678–685 and the tests'
  copy of `styles.css` ("Boss intro cutscenes", without the SVG scene's and sprites' rules), with a
  `surface` element in `.boss-intro-scene` showing the scene.
- **The sequence:** the stage's clip from 0; the boss and imps bound to their marks with idle strips;
  UI cues (at 0: the texts, `data-boss`, the host shown and `is-playing`; at 7.0: hidden); the music
  cue (`great-imp-jiruan` at 0, as the old game starts the boss's theme with the battle); an event
  cue at the end for the game (the AI hold's end); the reduced variant (the old game's 2.4 s still
  card, Q19).
- Where its files live (Q30), how it's checked (Q32), and watching it (Q33).

## Steps

Each step ends as every step does (`Docs/Engine-Plan.md`, "How each step is finished"). Steps marked
as able to run side by side touch no file in common except the docs (each its own Progress line and
Status entry here; where two update another design doc, each its own section).

Order: **1 and 2** side by side; then **3** (after 1) beside **4** (after 1 and 2); then **5** (after
3 and 4) beside **6** (after 4; it can start beside 3); **7** last (after 3–6). Steps 1, 2, 3 and 7
make Blender references, so they run on the user's PC; 4, 5 and 6 can run anywhere (6 needs no
Chromium: its references are engine renders of hand-scheduled graphs).

1. **The node tree and the scene.** Independent of every other step.
   - Creates `Engine/Statventure/Scene/` (nodes with local transforms, world matrices, paths and
     attachments; the scene: root, instancing a model's hierarchy, the frame client that fills its
     `View3D`; clips of a model bound to scene nodes), `Tests/Statventure.Tests/Scene/` (world matrices
     against Blender, lazy updates, instancing, the view redrawn only on change as `AnimatedViewTests`
     checks today).
   - Changes `Engine/Statventure/Animation/AnimatedView.cs` (the actors on nodes; Q1),
     `Tests/Statventure.Tests/Animation/AnimatedViewTests.cs` and `Audio/EffectTests.cs` (they build
     actors), `Tools/BlenderReference/make_models.py` (a hierarchy test model: parent inverses, a bone
     parent, non-uniform scale above a turned child, a mirrored branch, an animated parent),
     `Tools/BlenderReference/evaluate_animations.py` (node scenes: each object's world matrix at the
     times), `Tests/Statventure.Tests/Data/Models/` and `Golden/` (the export and Blender's values),
     `Docs/Animation.md` ("Animated views").
   - Questions: Q1–Q3.
2. **F-curves and easing.** Independent (of 1 too).
   - Creates `Engine/Statventure/Scene/FCurve.cs` (keys, handles and their types, interpolation and
     easing, extrapolation, auto handles; the curve records `scene.curve`, `scene.key` with their
     layouts and fixtures in `Tests/Statventure.Tests/Data/Records/`), `Tools/BlenderReference/evaluate_curves.py`
     (the test's curves made as Blender keyframes, `FCurve.evaluate` at the times, Blender's handle
     positions after its recalculation), `Tests/Statventure.Tests/Scene/FCurve*Tests.cs`,
     `Reference/curve-scenes.json`, `Golden/curve-*.json`.
   - Changes `Tools/Scripts/make-blender-references.sh` (one line).
   - Fetches into `reference-src/blender-anim/`: `blenkernel/intern/curve.cc`, `blenlib/intern/easing.cc`
     and `BLI_easing.h` (5.2.0).
   - Tests: every interpolation and easing mode both ways with their parameters, every handle type
     and smoothing mode, extrapolation both ways, keys at the ends, against Blender at many times
     (on keys, between, beyond the ends) and Blender's handle positions.
   - Questions: Q4–Q8.
3. **Cameras and lights from glTF.** After 1; beside 2 and 4.
   - Changes `Engine/Statventure/Render3D/GltfImport.cs` (perspective and orthographic cameras,
     `KHR_lights_punctual` suns, the nodes that carry them), `Render3D/ModelData.cs` (Q9: new versions
     of `model` and `model.node` with frozen version-1 copies and upgrades),
     `Tests/Statventure.Tests/Data/Records/` (their layouts and fixtures), `Engine/Statventure/Scene/`
     (camera and sun attachments: a camera node's world matrix as the view's position, target and up
     with its field of view; a light node's −Z as the sun's direction), `make_models.py` (a stage test
     model: an animated perspective camera, an orthographic one, a sun, exported with cameras and
     lights in Q13's mode), `evaluate_animations.py` (each camera's world matrix and field of view and
     the sun's direction at the times), `Tests/Statventure.Tests/Render3D/GltfImportTests.cs`,
     `Tests/Statventure.Tests/Scene/`, `Docs/Renderer-3D.md` and `Docs/Assets.md` (their sections).
   - Checks: the cameras and sun against Blender's (on frames exactly, between frames within M7's
     allowance); a Blender render through the imported camera against the engine's
     (`render_scenes.py` takes the camera from the scene file as now).
   - Questions: Q9–Q13.
4. **The sequence record, builder and player core.** After 1 and 2; beside 3.
   - Creates `Engine/Statventure/Scene/Sequence.cs` (the records: the sequence, roles, every track
     kind's record, so steps 5 and 6 change no layout; cues, the reduced variant, the flags;
     `Load(files, path)` checking it as S5-1 checks models), `SequenceBuilder.cs`, `SequencePlayer.cs`
     (the clock, rate, pause, the hidden gap, seek, skip, end, the cut list, bindings; playback of
     the clip, transform, visibility, animation, palette and event tracks), the test sequence's first
     part (`Tests/Statventure.Tests/Scene/TestSequence.Core.cs`; the test sequence is a partial class,
     one file per step), `Tests/Statventure.Tests/Scene/Sequence*Tests.cs`, the `scene.*` layouts and
     fixtures.
   - Changes `Docs/Assets.md` (`world/cutscenes/` and the `scene.*` types).
   - Questions: Q14–Q24, and Q25 and Q28 (their answers are fields of the record).
5. **Camera, light and particle tracks.** After 3 and 4; beside 6.
   - Creates their playback in `Engine/Statventure/Scene/` (cuts and the field of view; the view's sun
     from a light node with its strength and angle; emitters started at cues on nodes, following
     them) and `TestSequence.Camera.cs`.
   - Changes `Engine/Statventure/Render3D/Particles.cs` (the effect, its shapes, spawns and ranges as
     records; an emitter's path from a node), `Tests/Statventure.Tests/Render3D/ParticleTests.cs`,
     `Tests/Statventure.Tests/Data/Records/` (their layouts and fixtures), `Docs/Renderer-3D.md`
     ("Particles").
   - Questions: Q25, Q26.
6. **Audio and UI tracks.** After 4; beside 3 and 5.
   - Changes `Engine/Statventure/Audio/AudioSystem.cs` (the clock, scheduled calls),
     `Audio/Effects/SoundEffects.cs` (a play at a time, cancelling), `Audio/Music/Soundtrack.cs` (a
     switch or stop at a time, cancelling), `Tests/Statventure.Tests/Audio/` (offline renders of
     scheduled plays against hand-scheduled graphs, cancelling, rates), `Docs/Audio.md`; if Q29 asks
     for it, `Engine/Statventure/UI/Style/AnimationEngine.cs` and `Docs/Scripting.md`.
   - Creates their playback in `Engine/Statventure/Scene/`, `TestSequence.Sound.cs`, and UI track
     tests through a `UiDocument` (the frames cues land on, the end state on a skip, pausing and
     hiding).
   - Questions: Q27–Q29.
7. **Ji'ruan's intro, the proof.** Last: after 3–6.
   - Creates the stage's Blender script (in `Tools/BlenderReference/`), its export, the sequence, the
     title card's markup and stylesheet in a test, the tests that play it (Q32), and, if Q33 says so,
     a sandbox mode to watch it (`Samples/Statventure.Sandbox`).
   - Changes `render_scenes.py` (the stage at chosen times) and, for Chromium captures (Q32),
     `Tools/GameReference` or a harness of its own.
   - Questions: Q30–Q33.

## Open questions

Details, layout and scope the decisions leave open, each with options (the recommended one first,
with its reason). Ask each before the step named (the record's questions before step 4, since the
record holds every track kind); the answers go into `Docs/Engine-Plan.md` ("Decisions for M10") and
here.

**Before step 1:**
- **Q1. `AnimatedView` and `Actor.Transform`.** M10-5 makes the animated view a scene. (a) The scene
  replaces `AnimatedView` (the class goes, its rate becomes the scene's for figures no sequence holds)
  and an actor is placed only by its node (`Actor.Transform` goes). Recommended: one way to place
  a figure, so a sequence and the battle can't place it two ways. (b) `AnimatedView` stays as a thin
  wrapper over a scene for the port's battle code (`Actors`, `Rate`), `Actor.Transform` an alias for
  its node's local matrix. (c) Both kept side by side, the scene for cutscenes only.
- **Q2. What a model becomes in the scene, and its ink.** (a) Instancing makes a scene node per glTF
  node; each top-level object (with its children) is a model of its own for the outline, the glow
  mask and picking, as Blender's own render of the stage draws a Freestyle contour per object.
  Recommended: the stage then looks as it does in Blender, which is the reference. (b) The whole
  `.glb` is one model (one outline, as M6's placed test models), its nodes scene nodes only to be
  moved. (c) A custom property on Blender objects (`group`) says which objects are drawn as one
  model.
- **Q3. Terrain in the scene.** (a) Terrain attaches to a node too (by its translation, as
  `Terrain.Draw` takes it), so the map is a scene like the battle and a cutscene can fly over a
  region. Recommended: one path for every 3D view (M6's rule for views). (b) Not an attachment: the
  map keeps its own view code.

**Before step 2:**
- **Q4. The curves' time unit.** (a) Seconds, as clips, animators and the sequence's clock; the
  harness converts Blender's frames at 60 fps. Recommended: one unit in the engine and the editor.
  (b) Frames at 60 fps, as Blender keys them.
- **Q5. Precision.** (a) Double precision, checked against Blender within float32 rounding (Blender
  evaluates in float), as M7 samples clips in double. Recommended: the values feed transforms, where
  float rounding isn't visible, and the bound is stated, not fitted. (b) Blender's float32 arithmetic
  ported operation for operation, equal to the last bit (as M8 matched Chromium's audio).
- **Q6. What a key stores of its handles.** (a) Their types always, their positions only for free
  and aligned handles; auto, auto-clamped and vector handles are computed at load by Blender's rule,
  so a key moved in the editor re-smooths its neighbours as in Blender. Recommended: the curve then
  can't hold handles that contradict their types. (b) Every handle's position as computed, the
  engine only evaluating (the editor recomputes them).
- **Q7. F-curve modifiers.** (a) None: interpolation, easing, handles, extrapolation and auto
  smoothing only (the decision's set); a looping motion is a clip or strip that repeats.
  Recommended: the decision's scope, and the old intros' loops (the rift's pulse) are Blender clips.
  (b) Also Blender's Cycles modifier (curves that repeat, with or without offset). (c) Cycles and
  Noise.
- **Q8. Rotations in a sequence's curves.** (a) Euler angles in XYZ order, Blender's default rotation
  mode for objects, turned into a quaternion when the node is set. Recommended: what an animator keys
  on an object in Blender. (b) Quaternion components (Blender's quaternion mode), normalised when
  used. (c) Either, by a mode on the track.

**Before step 3:**
- **Q9. Where cameras and lights go in the data.** (a) In the model record: camera and light lists
  and a camera and a light index on nodes, so `model` and `model.node` get version 2 (frozen
  version-1 copies, upgrades, fixtures). Recommended: one `.glb`, one importer, one record (Q20 of M9).
  (b) A separate stage record for cutscene `.glb`s (nodes, cameras, lights, clips) beside `model`, the
  model record unchanged.
- **Q10. The field of view and Blender's sensor fit.** (a) The exporter's `yfov` as it is (Blender
  computes it from the lens, the sensor and its fit for the render's aspect at export), the engine
  keeping the vertical extent as now; cutscene cameras use sensor fit vertical; checked at another
  aspect against Blender. Recommended: the engine's cameras already behave as sensor fit vertical. (b)
  The lens, sensor and fit exported as extras and the field of view computed per view as Blender's
  fit "auto" would (a wider window shows more at the sides, a taller one less above and below).
- **Q11. A changing field of view and camera cuts.** (a) Blender animates the cameras' motion; a
  change of the field of view is a curve on the sequence's camera track, and cuts are camera-track
  cues naming camera nodes. Recommended: glTF's core can't animate a camera's field of view. (b) The
  lens keyed in Blender and exported through the exporter's `KHR_animation_pointer` support (to check
  in its 5.2 source), read by the importer; cuts by Blender's camera-bound timeline markers exported
  as extras. (c) One camera per shot with a fixed lens, cuts in the sequence.
- **Q12. Lights.** (a) One sun per view, as the renderer draws: its direction from its node, its
  strength Blender's own (the exporter's lighting units set to raw, `export_import_convert_lighting_mode`,
  to check), its angle a custom property in extras (glTF has no sun size); other light types refused
  at import. Recommended: no renderer change. (b) Also point and spot lights (a renderer change: how
  cel shading takes several lights, a new look question). (c) No lights from glTF: the sun stays the
  scene's setting, changed by sequence curves.
- **Q13. The stage's animation export mode.** (a) The scene mode: the stage's whole timeline is one
  clip with every animated object and camera on it, as the artist sees it on Blender's timeline.
  Recommended: one shot, one clip, no bookkeeping of which action goes where. (b) The actions mode
  (M7's): one clip per action, each placed by its own clip track. (c) NLA tracks as clips.

**Before step 4 (the record holds every track kind, so Q25 and Q28, whose answers are fields of it,
are asked before it too):**
- **Q14. The record's layout.** (a) As proposed (Design, "The sequence record"): the `scene.*` types,
  every track kind declared in step 4 (so steps 5 and 6 change no layout), cue times in seconds, stage
  models referred to by path in `models.pak` (`models/stages/<name>`). Recommended. (b) With the
  user's changes.
- **Q15. How a sequence names what it moves.** (a) Roles: the sequence lists the roles it needs
  (actors such as `boss`, `imp.1`; UI hosts such as `card`; nodes the game provides) and the game
  binds them when it plays it; nodes of its own stages by their Blender names under the stage; UI
  elements by id within a bound host element. Recommended: one sequence can serve any boss of a kind,
  and the old game's intros share one overlay. (b) Global names only: node paths from the scene's
  root and element ids in the document. (c) Roles for actors only, everything else global.
- **Q16. How figures take part.** (a) A bound figure stands on a mark node of the stage (whole-figure
  motion from the stage's clip), its own clips play as NLA strips on a top layer of its stack over its
  state machine (blending in and out by Blender's rule), parameter and trigger cues hand it back to
  its machine; its clock follows the sequence while it is bound. Recommended: M7's no-root-motion rule
  and its machines both keep working. (b) Strips only. (c) Parameter and trigger cues only (the
  machine picks every clip).
- **Q17. Cue order and when cues act.** (a) State tracks are applied in the frame's animation timing
  update, in track order; moments are queued in time order, those within 10⁻⁶ s of each other
  (`AnimationStrip.Epsilon`) in record order (track, then cue), and delivered after the figures' clip
  events, each as game code. Recommended: M7's order for events, and a UI change shows in the same
  frame. (b) Sequence cues before the figures' clip events. (c) UI cues as a task before the frame.
- **Q18. Event cues at a skip.** (a) Delivered at the skip, in order, each marked as skipped, so state
  the game sets on cues ends the same whether a cutscene was watched or skipped. Recommended. (b) Not
  delivered (as seeking fires no clip event, M7). (c) Each cue says whether it fires on a skip.
- **Q19. The reduced variant's form.** (a) A cut list: spans of the timeline (from, to), each played at
  its own length, a span whose ends are equal holding that moment for its length; moments in cut
  parts treated as at a skip (Q18); the UI follows the stylesheet's reduced-motion rules, as in the old
  game. Ji'ruan's: one span holding the moment the card stands (about 6.5 s) for 2.4 s. Recommended:
  covers both a shorter and a still variant with one set of tracks. (b) A second set of tracks (a
  whole other timeline). (c) A duration only, the timeline played faster to fit it.
- **Q20. Blocking, unskippable, ending.** (a) The engine plays any number of sequences at once and
  doesn't stop one for another; `blocking` is for the game (it holds its AI while a blocking one plays,
  as `bossIntroUntil` did); unskippable means `Skip()` does nothing and returns false, while the game
  can still end it to its end state (`Finish()`, for a battle that ends). Recommended: the old game's
  rules are the game's (M10-3: the game binds the skip). (b) One blocking sequence at a time: playing
  one ends the one playing (to its end state, as `stopBossIntro(false)`). (c) As (a), and a blocking
  sequence also makes the document under it inert.
- **Q21. After the end.** (a) The end state stays (camera, nodes, figures on their marks, UI) until the
  game changes it; the figures' clocks go back to the scene's. Recommended: one rule with the skip's
  end state, and the game decides what comes next (the old game hid its overlay). (b) Everything a
  sequence moved goes back to how it was before it played.
- **Q22. Timeline markers from Blender.** (a) The stage's script copies its timeline markers into the
  stage clip's extras (as `export_markers` copies pose markers), and a cue may sit at a marker by name
  instead of a time, following it when the stage is retimed and exported again. Recommended: the
  artist's timing stays the source. (b) Cue times only; markers stay in Blender.
- **Q23. Fades and visibility of 3D objects** (the old scene fades the rift, halo, eyes and imps). (a)
  Models stay opaque: things appear by scale, by moving into view, by visibility cues (a node shown or
  hidden with its subtree) and as particles and glow; fades stay UI overlays (the halo as a gradient
  over the surface). Recommended: no renderer change, and ink-drawn art cuts rather than fades. (b)
  Per-model opacity in the renderer (a new look question: how ink, glow and hatching fade).
- **Q24. Ji'ruan's reveal** (a black silhouette with a violet glow, then lit; also other bosses' at
  port time). (a) A palette cue: the figure's palette swapped from an all-ink one to its own at 4.48 s,
  under the UI flash. Recommended: no renderer change. (b) A per-model tint the renderer blends (a new
  track kind and shader input; a look question). (c) Left out of the proof.

**Before step 5 (Q25 before step 4: the track names its effect):**
- **Q25. Particle effects' data.** (a) Records of their own, named by path from the track, in
  `models.pak` (`models/effects/<name>`), shared by battles and cutscenes. Recommended: one effect,
  one place to edit it (M11). (b) The same in `world.pak` (`world/effects/<name>`). (c) Inline in the
  track.
- **Q26. Particles at a skip and after the end.** (a) Evaluated at the end time like every track
  (particles are computed from their age, so those alive then show and a burst that died shows
  nothing); after the end the sequence's emitters play on, on the scene's clock, until their particles
  die. Recommended: particles were made rewindable for this. (b) A skip removes all of the sequence's
  particles. (c) A skip leaves running bursts to play out from where they were.

**Before step 6 (Q28 before step 4: its option (c) is a field of the cue):**
- **Q27. Where the sequence starts on the audio clock.** (a) At the audio time of the quantum in which
  its first call is applied (the audio thread's "now" for the frame that started it); every cue is
  sent ahead with its time from there. Recommended: sample-accurate between cues, within a quantum
  plus the output's ~20 ms of the picture, and cutscenes of seconds need no drift correction. (b) The
  same, corrected for the output's latency so the sound lines up with the frame on screen. (c)
  Re-anchored each frame from the frame clock (follows the picture over long sequences, but cues land
  a quantum early or late as frames jitter).
- **Q28. A music cue at a skip** (before its time, or cut by the reduced variant). (a) Applied at the
  skip with music.js's usual fades (the old theme out over 0.55 s, the new in over 0.8 s).
  Recommended: the game's only way to change themes, and a hard start would click. (b) The new theme
  at full level at once. (c) Dropped by a skip unless the cue says to keep it.
- **Q29. The UI's CSS motion when the sequence pauses, is hidden, runs at another rate or seeks.** (a)
  The player pauses and resumes the CSS animations of the elements its UI tracks name with it, also
  when the document is hidden (the engine applying the pause at that moment, where Blink would at the
  next frame); a rate other than 1 and seeking don't reach CSS (a sequence with UI motion plays at rate
  1). Recommended: M10-9 keeps the motion in CSS and M10-7 pauses while hidden; this keeps the title
  card with the picture at the least engine change. (b) The engine gives CSS animations a playback
  rate and seeking (Web Animations' `playbackRate` and `currentTime` on CSS animations, left out in
  M5), so the UI follows the sequence in everything. (c) The UI runs on the document's clock as in the
  old game: a paused or hidden sequence's title card runs on (and jumps forward after a minimize).

**Before step 7:**
- **Q30. Where the proof's files live.** (a) As test data: the stage's Blender script in
  `Tools/BlenderReference/`, its export in `Tests/Statventure.Tests/Data/Models/`, the sequence built
  in C# in the test (its JSON form a committed fixture), the markup and stylesheet in the test; nothing
  in `world.pak` or `models.pak`. Recommended: its figure is a placeholder, and test data never ships
  (M9 Q1). (b) As the first game content: the sequence in `engine/Assets/Packs/world.pak` as
  `world/cutscenes/jiruan` and the stage in `models.pak`, replaced when the real models come. (c) As
  (a), plus the `.blend` file saved under `engine/Assets/`.
- **Q31. What the stand-in stage holds.** (a) Every element of the old scene remade simply in the art
  style: far, mid and near trunks, the rift opening and pulsing with its glow, the cracks, eight eyes
  blinking, mist as puff particles, the ground, marks for the boss and four imps, the camera pushing
  in; the UI parts as the old markup. Recommended: the proof then shows every track kind on a real
  scene. (b) A minimal stage (ground, rift, marks, camera), since the real art comes at port time.
  (c) As (a), shown to the user as renders for approval before its tests are written.
- **Q32. How the proof is checked.** (a) The stage's frames at chosen times against Blender renders
  of the same moments; the title card's frames against Chromium captures of the old game's intro at
  the same times (its CSS animations seeked in the harness); skip and reduced motion by the
  sequencer's tests. Recommended: both halves against their references, as everything else. (b)
  Rule tests plus the engine's frames committed as goldens for the user to look at. (c) As (a)
  without the Chromium part.
- **Q33. Watching it.** (a) The sandbox gets a mode that plays the proof in a window (with the skip
  button, Escape and reduced motion), as `play-theme.sh` was added so the user could listen.
  Recommended: the user sees the result, not only tests. (b) No sandbox mode.

## How it works

Filled in as the steps are done.

## Status

- **Nothing implemented yet** (2026-11-04). The decisions are recorded, the design is proposed,
  seven steps are proposed with their dependencies, and the open questions above are to be asked
  before the steps that need them.

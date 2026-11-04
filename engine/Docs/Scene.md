# Scene and sequencer (M10)

The scene is a tree of nodes that places everything a 3D view shows: figures, stage objects,
terrain, cameras, the sun and particle emitters. The sequencer plays cutscenes on it: a sequence is a
versioned record of tracks (clips made in Blender, curves, cues for figures, cameras, light,
particles, sound, music and the UI) played on the document's frame clock, skippable to its end state,
with a reduced variant for reduced motion. M10 builds both and remakes one boss intro, Ji'ruan's, as a
proof.

## Decisions (user, 2026-11-04)

Asked before the milestone started; every answer the recommended option except the scope (M10-2). In
full, with the options not chosen, in `Docs/Engine-Plan.md`, "Decisions for M10". The 33 details they
left open were asked after this doc's first draft and are under "Step details" below.

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
- **Audio cues (M10-8):** scheduled on the audio clock from the sequence's start, sample-accurate; a
  skip or a pause cancels pending cues. (The decision said "through M8's scheduling", but M8 has no
  public scheduling: the game's calls start at the audio thread's next quantum and the audio clock is
  internal. The graph inside is sample-accurate, so step 6 adds a play at a time, a clock the game can
  read and cancelling on top of it: What exists, "Audio".)
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
- **Terrain** (`Render3D/Terrain.cs`): `Terrain.Draw(items, translation, camera, viewportHeight, …)`
  takes a translation only (and the camera for its level of detail); the map's view is set up by its
  own code.
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
- **Hidden documents and the hidden-time jump** (`UiDocument.Visibility.cs`): a minimized window's
  document gets no frames, so frame clients aren't advanced while hidden, and on the first frame
  after it is shown again they are advanced by the whole hidden time at once: an `AnimatedView`'s
  figures (and any `Animator` added as a frame client) jump forward, as the CSS and script
  animations do (their timeline is real time, as Chromium's). M10-7's pause while hidden needs the
  sequencer to drop that gap (Design, "The player"). Nothing wrote this down before
  (`Docs/Animation.md`, `Docs/Scripting.md`); it stays so for the figures no sequence holds.
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
  `[Subtype(tag, type)]` (M9 Q12; no engine type uses one yet, only the serializer's tests), layout
  signatures and fixtures per type in `Tests/Statventure.Tests/Data/Records/` (one file or folder per
  type), `VirtualFileSystem.Load<T>(path)`, `Application.Data`; a loose `.json` that starts with
  `"$types"` packs as a record without an importer (S3-1); `world.pak` is mounted when present, and
  none is committed (M9 Q1: no game content yet). `StateMachineBuilder` (fluent, by name, `Build()`
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
   glTF exporter's camera and light conversion), not fitted. Curves are evaluated in double precision
   and checked against Blender within float32 rounding (Q5); where the engine samples a clip Blender
   evaluates as curves, within M7's between-frame allowance; renders compared as M6 compares them.
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
   title card is also compared with the old game's in Chromium (Q32).

Every new test is shown able to fail by breaking each rule it covers (`Tools/Scripts/mutate.py`).
Steps that make Blender references run on the user's PC (the cloud has no Blender).

## Design

The design the steps work from (the user's step details of 2026-11-04 included); names are working
names. "(Qn)" points to the step detail that settled the point.

### The node tree and the scene (step 1)

- **A node** has a name, a parent and children, a local translation, rotation (a unit quaternion)
  and scale (glTF's T·R·S; in System.Numerics' row vectors S·R·T) and a world matrix (local × the
  parent's world), recomputed lazily when a local transform above it changed. Paths name nodes from a
  given node (`stage/rift`). A node can be hidden, which hides its subtree (Q23).
- **Attachments:** an `Actor` (M7's figure, placed only by its node: its model matrix is its node's
  world matrix, `Actor.Transform` goes; its gear stays on its sockets) (Q1), a placed model (a `Model`
  with its palette, scenery flag and optional pose; each its own model for the ink), terrain (by its
  node's translation, as `Terrain.Draw` takes it, so the map is a scene too) (Q3), a camera and a sun
  light (step 3), a particle emitter (step 5). A node can carry several.
- **The scene** replaces `AnimatedView` (the class goes) (Q1): a root node, the `View3D` it fills, and
  a frame client of the document: each frame it advances the actors no sequence holds by the frame's
  time times its `Rate` (the battle pace, as `AnimatedView`'s rate was), brings world matrices up to
  date, and gives the view new draw items only when a pose, a node, an attachment or the list changed
  (as `AnimatedView` did), the view's camera and sun from their nodes, and its particles; then it
  dispatches the animators' events with the frame's animation events.
- **Models in the scene** (Q2): instancing a model under a node makes a scene node for each glTF node
  (names and hierarchy kept); each top-level object with its children is a model of its own for the
  outline, the glow mask and picking (as Blender's own render of the stage draws a Freestyle contour
  per object); a skinned figure stays one `Actor` (its bones aren't scene nodes). A clip of the model
  (Blender objects' animation, absolute local channels) binds to those scene nodes by name, as
  `ClipBinding` binds to a model's nodes, and sets their local transforms.
- **Blender's rule as glTF gives it back:** glTF's node rule is world = parent's world · T·R·S; what
  Blender adds (an object's parent inverse, a bone parent placing a child at the bone's tail, the
  axis conversion of objects) the exporter bakes into the child's local transform. Step 1 checks the
  cases where the two could part: parent inverses, a bone parent, non-uniform scale above a turned
  child (a sheared world), a mirrored branch, an animated parent above a still child.

### Curves (step 2)

- **`FCurve`** (Blender's name; `AnimationCurve` is the glTF sampler's): keys in time order, times in
  seconds (Q4), each with a value, the interpolation to the next key (constant, linear, Bezier, or an
  easing type: sine, quad, cubic, quart, quint, expo, circ, back, bounce, elastic), the easing mode
  (in, out, in-out; Blender's automatic resolved per type as `fcurve.cc` does), back's overshoot and
  elastic's amplitude and period, and two handles with their types (free, aligned, vector, auto,
  auto-clamped: Blender's default for new keys). A key stores its handle types always and the
  positions only of free and aligned handles; auto, auto-clamped and vector handles are computed at
  load by Blender's rule (Q6). Per curve the extrapolation (constant, linear) and Blender's auto
  smoothing (none, continuous acceleration). No modifiers (Q7): a looping motion is a clip or strip
  that repeats.
- **Evaluation** as `fcurve_eval_keyframes_interpolate` and `_extrapolate`, in double precision,
  checked against Blender within float32 rounding (Q5): the key search with its threshold, a time on a
  key giving the key's value, Bezier segments corrected (`BKE_fcurve_correct_bezpart`) and solved for
  the time (`findzero`), easings as `BLI_easing_*` of (time, begin, change, duration); auto handles as
  `BKE_fcurve_handles_recalc` computes them.
- **Tracks use one curve per component**, as Blender's F-curves have an array index: location x, y,
  z; rotation as Euler angles in XYZ order, Blender's default mode for objects, turned into a
  quaternion when the node is set (Q8); scale x, y, z; and single values (a camera's field of view,
  the sun's strength and angle).

### The sequence record (step 4; layout as proposed, Q14)

Records `scene.*`, in `world.pak` at `world/cutscenes/<name>` (M10-10); every track kind is declared in
step 4, so steps 5 and 6 change no layout:

- **`scene.sequence`:** the name; the duration (seconds); `skippable` (true by default) and
  `blocking` (true by default; a vignette false; a flag for the game, Q20); the roles it needs, each a
  name and a kind (actor, node, model, element), which the game binds when it plays it (Q15); the
  stages it brings, models by path in `models.pak` (`models/stages/<name>`), instanced under the
  scene's root when it plays (Q2, Q14); its markers (name, time), from the stage's export (Q22); its
  tracks, in order (a union of track kinds); its reduced variant (none, or a cut list, Q19).
- **Track kinds** (tagged subtypes of `scene.track`, M9 Q12):
  - *clip:* a stage's clip on that stage's nodes, with strip settings as `AnimationStrip` has them
    (start, part, scale, repeat or hold);
  - *transform:* a node and curves for its channels;
  - *visibility:* a node and cues (time, shown) (Q23);
  - *animation:* an actor role, its mark node, NLA strips on a top layer of its stack (clip, start,
    part, scale, repeat, blend in and out, Replace or Combine), parameter and trigger cues (Q16);
  - *palette:* a model or actor and cues (time, palette) (Q24);
  - *camera:* cuts (time, camera node) and a field-of-view curve (Q11);
  - *light:* cues (time, light node as the view's sun) and strength and angle curves (Q12);
  - *particles:* cues (time, effect by path, `models/effects/<name>` (Q25), node to emit from, seed,
    burst, rate, duration);
  - *audio:* cues (time; an effect with volume, pan and pitch, a music theme, or the music's stop);
  - *ui:* cues (time, element, action: set text, add or remove a class, set or remove an attribute);
  - *event:* cues (time, name), delivered to game code.
- **Times:** in seconds on the sequence's timeline; a cue may instead sit at a marker by name and
  follow it when the stage is retimed and exported again (Q22).
- **Names** (Q15): roles for what the game provides (actors such as `boss`, `imp.1`; UI hosts such as
  `card`; nodes); the nodes of its own stages by their Blender names under the stage; UI elements by
  id within a bound host element.
- **State and moments:** every track gives a state at any timeline time τ (a curve's value, the last
  cue at or before τ, a clip's or strip's pose at τ), so the end state is the tracks evaluated at the
  duration and seeking is evaluation; cues that are moments (events, sounds, particle starts, clip
  events of the figures) happen when the clock passes them.

### The builder (step 4)

A `SequenceBuilder` as `StateMachineBuilder` is: fluent, everything by name, `Build()` validating
(roles and nodes named exist in the stages or roles, keys in order, cue times within the duration,
markers named exist, the variant within the timeline). For example (names to be settled in the
step):

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

- **Playing:** the scene plays a sequence with its bindings and gives a player, a frame client of the
  scene's document. Any number play at once, and playing one doesn't stop another (Q20). `Rate` (1
  by default; independent of the scene's battle pace, M10-7), `Pause()` and `Resume()` (the game's),
  `Seek(time)` (a jump: no cue fires, as `Animator.Seek`), `Skip()` (to the end state; on an
  unskippable sequence it does nothing and returns false) and `Finish()` (ends any sequence, an
  unskippable one too, to its end state, as a skip: for a battle that ends) (Q20); `Time`,
  `Duration`, whether it plays the reduced variant, `Blocking` (for the game, which holds its AI while
  a blocking one plays, as the old `bossIntroUntil`), `Finished` (a task completing at the end, saying
  whether it was skipped), cue events delivered as game code and awaitable as clip events are
  (`Next(name)`).
- **Its clock:** advanced by the frame's time times the rate, not while paused, and not while the
  document is hidden: the player drops the gap the first frame after showing would bring (the
  hidden-time jump, What exists). The timeline time τ is the clock mapped through the reduced
  variant's cut list when it plays it.
- **The reduced variant** (Q19) is chosen when it starts, from the document's media environment (what
  `prefers-reduced-motion` sees, so the 3D and the stylesheet's reduced-motion rules agree). It is a
  cut list: spans of the timeline (from, to), each played at its own length; a span whose ends are
  equal holds that moment for its length; moments in the parts cut out are treated as at a skip
  (Q18). The UI follows the stylesheet's reduced-motion rules, as in the old game. Ji'ruan's: one span
  holding the moment the card stands (about 6.5 s, set in step 7) for 2.4 s.
- **Each frame** (Q17), in the animation timing update: the clock moves; the state tracks are applied
  in track order at τ to their nodes, actors, camera, sun, palettes and UI elements; the actors it
  holds are advanced by the change in τ (so their clip events fire; their own clock otherwise waits);
  the moments passed are queued in time order, those within 10⁻⁶ s of each other
  (`AnimationStrip.Epsilon`) in record order (track, then cue), and delivered after the figures' clip
  events, each as game code; particles are evaluated at τ; audio cues are kept scheduled ahead on the
  audio clock (step 6).
- **Figures** (Q16): a bound figure stands on its mark node (whole-figure motion comes from the
  stage's clip: M7's no root motion); its own clips play as NLA strips on a top layer of its stack
  over its state machine, blending in and out by Blender's rule; parameter and trigger cues hand it
  back to its machine; while bound, its clock follows the sequence.
- **The end** (Q21): at τ = duration the end state is reached and stays (camera, nodes, figures on
  their marks, UI) until the game changes it; `Finished` completes; the figures it held go back to the
  scene's clock.
- **A skip:** τ jumps to the duration and the state tracks are evaluated there; event cues not yet
  delivered are delivered at the skip, in order, each marked as skipped (Q18); pending audio cues are
  cancelled, and a music cue not yet applied is applied at the skip with music.js's usual fades (Q28);
  particles are evaluated at the end time like every track, and running emitters play on, on the
  scene's clock, until their particles die (Q26).
- **Each track kind's playback** is a class of its own behind one interface, so steps 5 and 6 add
  playback without editing the player.

### Audio cues (step 6)

- The audio system gets a clock the game can read and calls with a time on it: an effect played at a
  time, the music switched (with music.js's fades) or stopped at a time, each giving a handle that
  cancels it while it hasn't started (its sources stopped before their start, its timers dropped).
  The audio thread builds the voice as now but starts it at that sample (the graph's `start(when)` and
  automation already are sample-accurate).
- The anchor (Q27): the sequence starts on the audio clock at the audio time of the quantum in which
  its first call is applied (the audio thread's "now" for the frame that started it); every cue is
  sent ahead with its time from there: due at the anchor plus (τ_c − τ at the anchor) / rate. A pause,
  a seek, a rate change or a skip cancels the pending cues and sends them again from the new time (a
  pause on resuming). The sounds themselves aren't stretched or pitched by the rate.
- A minimized window pauses the audio system (M8) and so its clock, as the sequence pauses (M10-7).

### UI tracks (step 6)

- Elements are found through the bindings, by id within the bound host (Q15); cues apply in the
  frame's animation timing update with the other state tracks (Q17), so their style changes show in
  that frame, and a class added there starts its CSS animations at that frame's time (as the old
  game's `is-playing` did on the next frame).
- The state of each element is the last cue at or before τ: the end state of Ji'ruan's intro is the
  host hidden.
- CSS motion (Q29): the player pauses and resumes the CSS animations of the elements its UI tracks
  name with it, also when the document is hidden (the engine applying the pause at that moment, where
  Blink would at the next frame); a sequence with UI tracks plays at rate 1 only, and seeking doesn't
  reach CSS animations.

### The Blender export of a cutscene stage (steps 3, 7)

- **The source** is a Blender script per stage (as `make_models.py` is the test models' source; for
  the game's cutscenes at port time, where the sources live is the port's question): meshes in
  palette-slot materials with the `ink` and `hatch` custom properties (M6); empties as marks where
  figures stand (`mark.boss`, `mark.imp.1` …), animated for whole-figure motion; cameras with sensor
  fit Vertical (Q10); a sun with its angle as a custom property (Q12); all animated on the scene's
  timeline at 60 fps; timeline markers naming moments (Q22). A camera's motion is keyed in Blender;
  a change of its field of view is a curve on the sequence's camera track, and cuts are camera-track
  cues (Q11).
- **The export:** M7's `ANIMATED` settings plus cameras and lights, in the scene mode: the stage's
  whole timeline is one clip with every animated object and camera on it (Q13); the lighting units
  raw, so the sun's strength is Blender's own (`export_import_convert_lighting_mode`, to check in the
  step) (Q12); the script copies the timeline markers into the stage clip's extras as `export_markers`
  copies pose markers (Q22). Blender's cameras and glTF's both look down their local −Z, but the
  exporter's axis conversion turns objects' axes, so it corrects cameras and lights (to check in its
  5.2 source, as M7 checked the item axes, C⁻¹).
- **The import** (Q9): the `.glb` into `models.pak` as every model (M9 Q20), its cameras and lights in
  the model record: camera and light lists and a camera and a light index on nodes, as version 2 of
  `model` and `model.node` (frozen version-1 copies, upgrades, fixtures). A camera's field of view is
  the exporter's vertical one, the engine keeping the vertical extent as now (Q10). One sun per view:
  its direction from its node, its angle from the extras; other light types are refused at import
  (Q12). When a sequence plays, its stages are instanced under the scene's root, its clip track plays
  the stage's clip on them, its camera and light tracks pick their nodes, and the bound figures stand
  on their marks.

### The proof: Ji'ruan's intro (step 7)

- **3D** (the old SVG scene and sprites; Q31): every element of the old scene remade simply in the art
  style: far, mid and near trunks, the rift opening and pulsing with its glow, the cracks, eight eyes
  blinking, mist as puff particles, the ground, marks for the boss and four imps (the test figure
  `figure.glb` on each, in palettes of their own), the camera pushing in over the 7 s. Models stay
  opaque (Q23): things appear by scale, by moving into view, by visibility cues and as particles and
  glow; the old scene's fades (the halo, the overlay) are UI overlays (the halo a gradient over the
  surface). The reveal (Q24): a palette cue swaps the boss's palette from an all-ink one to his own at
  4.48 s, under the UI flash. **The stage is shown to the user as Blender renders for approval before
  its tests are written** (Q31).
- **UI** (as the old markup and stylesheet): the overlay with its letterbox, flash, vignette,
  location, skip button, caption and progress bar, from `old-game/index.html` 678–685 and the tests'
  copy of `styles.css` ("Boss intro cutscenes", without the SVG scene's and sprites' rules), with a
  `surface` element in `.boss-intro-scene` showing the scene.
- **The sequence:** the stage's clip from 0; the boss and imps bound to their marks with idle strips;
  UI cues (at 0: the texts, `data-boss`, the host shown and `is-playing`; at 7.0: hidden); the music
  cue (`great-imp-jiruan` at 0, as the old game starts the boss's theme with the battle); an event
  cue at the end for the game (the AI hold's end); the reduced variant (the old game's 2.4 s still
  card).
- **Files** (Q30): test data only: the stage's Blender script in `Tools/BlenderReference/`, its export
  in `Tests/Statventure.Tests/Data/Models/`, the sequence built in C# in the test (its JSON form a
  committed fixture), the markup and stylesheet in the test; nothing in `world.pak` or `models.pak`.
- **Checks** (Q32): the stage's frames at sampled times against Blender renders of the same moments;
  the title card's frames against Chromium captures of the old game's intro at the same times (its CSS
  animations seeked in the harness); skip and reduced motion by the sequencer's tests.
- **Watching it** (Q33): a sandbox mode plays the proof in a window, with the skip button, Escape and
  reduced motion.

## Steps

Each step ends as every step does (`Docs/Engine-Plan.md`, "How each step is finished"). Steps marked
as able to run side by side touch no file in common except the docs (each its own Progress line and
Status entry here; where two update another design doc, each its own section).

Order: **1 and 2** side by side; then **3** (after 1) beside **4** (after 1 and 2); then **5** (after
3 and 4) beside **6** (after 4; it can start beside 3); **7** last (after 3–6). Steps 1, 2, 3 and 7
make Blender references, so they run on the user's PC; 4, 5 and 6 can run anywhere (6 needs no
Chromium: its references are engine renders of hand-scheduled graphs).

1. **The node tree and the scene.** Independent of every other step.
   - Creates `Engine/Statventure/Scene/` (nodes with local transforms, world matrices, paths, hiding
     and attachments, terrain among them; the scene: root, instancing a model's hierarchy with each
     top-level object its own model, the frame client that fills its `View3D`; clips of a model bound
     to scene nodes), `Tests/Statventure.Tests/Scene/` (world matrices against Blender, lazy updates,
     instancing, the view redrawn only on change as `AnimatedViewTests` checked).
   - Removes `Engine/Statventure/Animation/AnimatedView.cs`'s `AnimatedView` and `Actor.Transform`
     (`Actor` stays, placed by its node; Q1); moves `Tests/Statventure.Tests/Animation/AnimatedViewTests.cs`
     onto the scene and changes `Audio/EffectTests.cs` (they build actors).
   - Changes `Tools/BlenderReference/make_models.py` (a hierarchy test model: parent inverses, a bone
     parent, non-uniform scale above a turned child, a mirrored branch, an animated parent),
     `Tools/BlenderReference/evaluate_animations.py` (node scenes: each object's world matrix at the
     times), `Tests/Statventure.Tests/Data/Models/` and `Golden/` (the export and Blender's values),
     `Docs/Animation.md` ("Animated views") and `Docs/Engine-Plan.md`'s "What M7 leaves for the port".
   - Details: Q1–Q3.
2. **F-curves and easing.** Independent (of 1 too).
   - Creates `Engine/Statventure/Scene/FCurve.cs` (keys, handles and their types, interpolation and
     easing, extrapolation, auto handles computed at load; the curve records `scene.curve`,
     `scene.key` with their layouts and fixtures in `Tests/Statventure.Tests/Data/Records/`),
     `Tools/BlenderReference/evaluate_curves.py` (the test's curves made as Blender keyframes,
     `FCurve.evaluate` at the times, Blender's handle positions after its recalculation),
     `Tests/Statventure.Tests/Scene/FCurve*Tests.cs`, `Reference/curve-scenes.json`,
     `Golden/curve-*.json`.
   - Changes `Tools/Scripts/make-blender-references.sh` (one line).
   - Fetches into `reference-src/blender-anim/`: `blenkernel/intern/curve.cc`, `blenlib/intern/easing.cc`
     and `BLI_easing.h` (5.2.0).
   - Tests: every interpolation and easing mode both ways with their parameters, every handle type
     and smoothing mode, extrapolation both ways, keys at the ends, against Blender at many times
     (on keys, between, beyond the ends) within float32 rounding, and Blender's handle positions.
   - Details: Q4–Q8.
3. **Cameras and lights from glTF.** After 1; beside 2 and 4.
   - Changes `Engine/Statventure/Render3D/GltfImport.cs` (perspective and orthographic cameras,
     `KHR_lights_punctual` suns, other light types refused, the nodes that carry them),
     `Render3D/ModelData.cs` (version 2 of `model` and `model.node` with frozen version-1 copies and
     upgrades), `Tests/Statventure.Tests/Data/Records/` (their layouts and fixtures),
     `Engine/Statventure/Scene/` (camera and sun attachments: a camera node's world matrix as the
     view's position, target and up with its field of view; a light node's −Z as the sun's direction,
     its angle from the extras), `make_models.py` (a stage test model: an animated perspective camera
     with sensor fit Vertical, an orthographic one, a sun, exported with cameras and lights in the
     scene mode, raw lighting units), `evaluate_animations.py` (each camera's world matrix and field of
     view and the sun's direction at the times), `Tests/Statventure.Tests/Render3D/GltfImportTests.cs`,
     `Tests/Statventure.Tests/Scene/`, `Docs/Renderer-3D.md` and `Docs/Assets.md` (their sections).
   - Checks: the cameras and sun against Blender's (on frames exactly, between frames within M7's
     allowance), at the render's aspect and another; a Blender render through the imported camera
     against the engine's (`render_scenes.py` takes the camera from the scene file as now).
   - Details: Q9–Q13.
4. **The sequence record, builder and player core.** After 1 and 2; beside 3.
   - Creates `Engine/Statventure/Scene/Sequence.cs` (the records: the sequence, roles, markers, every
     track kind's record, cues, the cut list, the flags; `Load(files, path)` checking it as S5-1 checks
     models), `SequenceBuilder.cs`, `SequencePlayer.cs` (the clock, rate, pause, the hidden gap, seek,
     skip, finish, end, the cut list, bindings; playback of the clip, transform, visibility,
     animation, palette and event tracks), the test sequence's first part
     (`Tests/Statventure.Tests/Scene/TestSequence.Core.cs`; the test sequence is a partial class, one
     file per step), `Tests/Statventure.Tests/Scene/Sequence*Tests.cs`, the `scene.*` layouts and
     fixtures.
   - Changes `Docs/Assets.md` (`world/cutscenes/`, `models/stages/` and the `scene.*` types).
   - Details: Q14–Q24, and Q25 and Q28 (the particle track names its effect; music cues need no field
     of their own).
5. **Camera, light and particle tracks.** After 3 and 4; beside 6.
   - Creates their playback in `Engine/Statventure/Scene/` (cuts and the field of view; the view's sun
     from a light node with its strength and angle; emitters started at cues on nodes, following
     them; particles at a skip) and `TestSequence.Camera.cs`.
   - Changes `Engine/Statventure/Render3D/Particles.cs` (the effect, its shapes, spawns and ranges as
     records, loaded from `models/effects/<name>`; an emitter's path from a node),
     `Tests/Statventure.Tests/Render3D/ParticleTests.cs`, `Tests/Statventure.Tests/Data/Records/`
     (their layouts and fixtures), `Docs/Renderer-3D.md` ("Particles") and `Docs/Assets.md`
     (`models/effects/`).
   - Details: Q25, Q26.
6. **Audio and UI tracks.** After 4; beside 3 and 5.
   - Changes `Engine/Statventure/Audio/AudioSystem.cs` (the clock, scheduled calls),
     `Audio/Effects/SoundEffects.cs` (a play at a time, cancelling), `Audio/Music/Soundtrack.cs` (a
     switch or stop at a time, cancelling), `Tests/Statventure.Tests/Audio/` (offline renders of
     scheduled plays against hand-scheduled graphs, cancelling, rates), `Docs/Audio.md`;
     `Engine/Statventure/UI/Style/AnimationEngine.cs` and `UI/Dom/UiDocument.Visibility.cs` (the
     player's pause of CSS animations, applied at the moment the document is hidden) and
     `Docs/Scripting.md`.
   - Creates their playback in `Engine/Statventure/Scene/` (rate 1 only for sequences with UI tracks),
     `TestSequence.Sound.cs`, and UI track tests through a `UiDocument` (the frames cues land on, the
     end state on a skip, pausing and hiding with the CSS animations held).
   - Details: Q27–Q29.
7. **Ji'ruan's intro, the proof.** Last: after 3–6.
   - First the stage: its Blender script (in `Tools/BlenderReference/`) and export, and Blender renders
     at a few moments **shown to the user for approval; the tests come after the approval** (Q31).
   - Then the sequence, the title card's markup and stylesheet in a test, the tests that play it
     (frames at sampled times against Blender renders; the title card against Chromium captures of
     the old game's intro, its CSS animations seeked; skip and reduced motion), and a sandbox mode
     that plays it (`Samples/Statventure.Sandbox`).
   - Changes `render_scenes.py` (the stage at chosen times) and `Tools/GameReference` or a harness
     of its own (the old intro captured at chosen times).
   - Details: Q30–Q33.

## Step details (user, 2026-11-04, asked after the design doc)

Every answer was the recommended option except Q31. Also in `Docs/Engine-Plan.md`, "Decisions for
M10", "Step details".

**Step 1 (the node tree and the scene):**
- **Q1. `AnimatedView` and `Actor.Transform`:** the scene replaces `AnimatedView` (the class goes; its
  rate becomes the scene's for the figures no sequence holds) and an actor is placed only by its node
  (`Actor.Transform` goes). Why: one way to place a figure, so a sequence and the battle can't place
  it two ways. (Not `AnimatedView` kept as a thin wrapper with `Transform` an alias for the node's
  local matrix; not both kept side by side, the scene for cutscenes only.)
- **Q2. Models in the scene and their ink:** instancing makes a scene node per glTF node; each
  top-level object with its children is a model of its own for the outline, the glow mask and
  picking. Why: Blender's own render of the stage, the reference, draws a Freestyle contour per
  object. (Not the whole `.glb` as one model; not a `group` custom property choosing.)
- **Q3. Terrain:** terrain attaches to a node too (by its translation), so the map is a scene like the
  battle and a cutscene can fly over a region. Why: one path for every 3D view. (Not left out, the map
  keeping its own view code.)

**Step 2 (F-curves and easing):**
- **Q4. Time unit:** seconds, as clips, animators and the sequence's clock; the harness converts
  Blender's frames at 60 fps. Why: one unit in the engine and the editor. (Not frames at 60 fps.)
- **Q5. Precision:** double precision, checked against Blender within float32 rounding (Blender
  evaluates in float), as M7 samples clips in double. Why: the values feed transforms, where float
  rounding isn't visible, and the bound is stated, not fitted. (Not Blender's float32 arithmetic
  ported to the last bit.)
- **Q6. Handles:** a key stores its handle types always and the positions only of free and aligned
  handles; auto, auto-clamped and vector handles are computed at load by Blender's rule, so a key
  moved in the editor re-smooths its neighbours as in Blender. Why: a curve can't hold handles that
  contradict their types. (Not every handle's position stored, the engine only evaluating.)
- **Q7. Modifiers:** none: interpolation, easing, handles, extrapolation and auto smoothing only; a
  looping motion is a clip or strip that repeats. Why: the decision's set, and the old intros' loops
  (the rift's pulse) become Blender clips. (Not Blender's Cycles modifier; not Cycles and Noise.)
- **Q8. Rotations:** Euler angles in XYZ order, Blender's default mode for objects, turned into a
  quaternion when the node is set. Why: what an animator keys on an object in Blender. (Not
  quaternion components; not either by a mode on the track.)
- Asked during step 2 (2026-11-04), all three answered with the recommended option:
- **S2-1. The records' layout:** `scene.key` version 1: `time` (seconds, required), `value`
  (required), `interpolation` (constant, linear, bezier, sine, quad, cubic, quart, quint, expo, circ,
  back, bounce, elastic; default bezier), `easing` (auto, in, out, inOut; default auto), the `left`
  and `right` handle types (free, aligned, vector, auto, autoClamped; default autoClamped),
  `leftTime`, `leftValue`, `rightTime`, `rightValue` (absolute positions, as Blender's BezTriple
  holds them, present exactly for a free or aligned side), `back` (1.70158), `amplitude` (0.8) and
  `period` (seconds, Blender's 4.1 frames; 0 means 0.3 of the segment): Blender's defaults for an
  inserted key. `scene.curve` version 1: `keys` (required), `extrapolation` (constant, linear;
  default constant), `smoothing` (none, continuousAcceleration; default continuousAcceleration,
  Blender 5.2's default for new F-curves). The curve names nothing it drives: the track does (step 4).
  Why: Blender's numbers carry over one to one and its handle rules apply to the same values. (Not
  handle positions as offsets from their key; not smoothing none by default.)
- **S2-2. The curve API:** the record is the curve: `FCurve` (`scene.curve`) and `FCurveKey`
  (`scene.key`), immutable; `Evaluate(seconds)`; the auto, auto-clamped and vector handles are
  resolved once, on first use, and cached (a copy made with `with` resolves its own); the resolved
  handles readable for the tests and the editor; helpers to make keys (linear, constant, eased, free,
  aligned). Why: a track holds its curves and evaluates them, with no build step. (Not data records
  with a separate evaluator, as `StateMachineDefinition` and `StateMachine`.)
- **S2-3. Curves Blender's rules don't cover:** refused when made or loaded, naming the problem: no
  keys; keys not increasing at least 0.01 frame (1/6000 s, Blender's merge threshold) apart; a free
  or aligned side without its position; an auto or vector side with one. What Blender's update
  (`fcurve.update()`) fixes in handles is fixed the same way: crossed handles swapped, a handle past
  its key clamped to it, both sides aligned lined up as for an unselected key (the left turned to the
  right). Why: Blender never holds such a curve after an edit, so there is nothing to match, and a
  silent fix would hide a data mistake. (Not Blender's update in full, sorting, merging and an empty
  curve evaluating to 0; not also refusing the handles Blender fixes.)

**Step 3 (cameras and lights from glTF):**
- **Q9. Where cameras and lights go:** in the model record: camera and light lists and a camera and a
  light index on nodes, as version 2 of `model` and `model.node` (frozen version-1 copies, upgrades,
  fixtures). Why: one `.glb`, one importer, one record (M9 Q20). (Not a separate stage record beside
  `model`.)
- **Q10. Field of view and sensor fit:** the exporter's vertical field of view (`yfov`) as it is, the
  engine keeping the vertical extent as now; cutscene cameras are made with sensor fit Vertical;
  checked at another aspect against Blender. Why: the engine's cameras already behave as sensor fit
  vertical. (Not the lens, sensor and fit exported as extras with the field of view computed per view
  as Blender's "auto" fit.)
- **Q11. A changing field of view and cuts:** Blender animates the cameras' motion; a change of the
  field of view is a curve on the sequence's camera track, and cuts are camera-track cues naming
  camera nodes. Why: glTF's core can't animate a camera's field of view. (Not the lens exported through
  `KHR_animation_pointer` with cuts from Blender's camera-bound markers; not one fixed-lens camera per
  shot.)
- **Q12. Lights:** one sun per view, as the renderer draws: its direction from its node, its strength
  Blender's own (the exporter's lighting units raw), its angle a custom property in extras (glTF has
  no sun size); other light types are refused at import. Why: no renderer change. (Not point and spot
  lights too; not no lights from glTF.)
- **Q13. Export mode:** the scene mode: the stage's whole timeline is one clip with every animated
  object and camera on it. Why: one shot, one clip, no bookkeeping of which action goes where. (Not
  the actions mode with a clip track per action; not NLA tracks as clips.)

**Step 4 (the sequence record, builder and player core):**
- **Q14. The record's layout:** as proposed (Design, "The sequence record"): the `scene.*` types,
  every track kind declared in step 4, cue times in seconds, stage models by path in `models.pak`
  (`models/stages/<name>`). (Not changed.)
- **Q15. Naming:** roles the game binds when it plays a sequence (actors, UI hosts, nodes it
  provides); the nodes of the sequence's own stages by their Blender names under the stage; UI
  elements by id within a bound host element. Why: one sequence can serve any boss of a kind, and the
  old game's intros share one overlay. (Not global names only; not roles for actors only.)
- **Q16. Figures:** a bound figure stands on a mark node of the stage (whole-figure motion from the
  stage's clip); its own clips play as NLA strips on a top layer of its stack over its state machine
  (blending in and out by Blender's rule); parameter and trigger cues hand it back to its machine; its
  clock follows the sequence while it is bound. Why: M7's no-root-motion rule and its machines both
  keep working. (Not strips only; not parameter and trigger cues only.)
- **Q17. Cue order:** state tracks are applied in the frame's animation timing update, in track
  order; moments are queued in time order, those within 10⁻⁶ s of each other in record order (track,
  then cue), and delivered after the figures' clip events, each as game code. Why: M7's order for
  events, and a UI change shows in the same frame. (Not sequence cues before the clip events; not UI
  cues as a task before the frame.)
- **Q18. Event cues at a skip:** delivered at the skip, in order, each marked as skipped. Why: state
  the game sets on cues ends the same whether a cutscene was watched or skipped. (Not left
  undelivered, as seeking fires no clip event; not a flag per cue.)
- **Q19. The reduced variant:** a cut list: spans of the timeline (from, to), each played at its own
  length; a span whose ends are equal holds that moment; moments in cut parts are treated as at a
  skip; the UI follows the stylesheet's reduced-motion rules. Why: one set of tracks covers both a
  shorter and a still variant. (Not a second set of tracks; not a duration only, the timeline played
  faster.)
- **Q20. Blocking, unskippable, ending:** any number of sequences play at once and none stops
  another; `blocking` is a flag for the game (which holds its AI while a blocking one plays);
  unskippable means `Skip()` does nothing and returns false, while `Finish()` still ends it to its end
  state. Why: the old game's rules are the game's (M10-3). (Not one blocking sequence at a time; not a
  blocking sequence making the document under it inert.)
- **Q21. After the end:** the end state stays (camera, nodes, figures on their marks, UI) until the
  game changes it; the figures' clocks go back to the scene's. Why: one rule with the skip's end state,
  and the game decides what comes next. (Not everything restored to how it was before.)
- **Q22. Timeline markers:** the stage's script copies its timeline markers into the stage clip's
  extras, and a cue may sit at a marker by name, following it when the stage is retimed. Why: the
  artist's timing stays the source. (Not cue times only.)
- **Q23. Fades of 3D objects:** models stay opaque; things appear by scale, by moving into view, by
  visibility cues (a node hidden with its subtree) and as particles and glow; fades are UI overlays.
  Why: no renderer change, and ink-drawn art cuts rather than fades. (Not per-model opacity in the
  renderer.)
- **Q24. Ji'ruan's reveal:** a palette cue swaps the figure's palette from an all-ink one to its own
  (at 4.48 s, under the UI flash). Why: no renderer change. (Not a per-model tint in the renderer; not
  left out of the proof.)
- **Open (found in step 1): a clock for a stage's clips outside a sequence.** An instance's clips are
  applied at a time the caller gives (`ModelInstance.Apply`); nothing advances them on its own. The
  sequence's clip track gives that time while it plays; if the game wants a stage moving outside a
  cutscene (a waving banner on the battle stage), which clock and rate drive it (the scene's battle
  pace, real time) is to be asked with this step.

**Step 5 (camera, light and particle tracks):**
- **Q25. Particle effects' data:** records of their own, named by path from the track, in
  `models.pak` at `models/effects/<name>`, shared by battles and cutscenes. Why: one effect, one place
  to edit it (M11). (Not in `world.pak`; not inline in the track.)
- **Q26. Particles at a skip and after the end:** evaluated at the end time like every track (those
  alive then show, a burst that died shows nothing); after the end the sequence's running emitters
  play on, on the scene's clock, until their particles die. Why: particles were made rewindable for
  this. (Not all removed at a skip; not running bursts left to play out from where they were.)
- **Open (found in step 1): the battle pace for particles and the water.** The scene advances its
  figures by the frame's time times its `Rate` (the battle pace) but leaves the view's particles and
  its clock (`View3D.Time`, the water's waves) as they are set; once emitters hang on nodes, whether
  the pace applies to them and to the water (or only to the figures) is to be asked with this step.

**Step 6 (audio and UI tracks):**
- **Q27. The audio anchor:** the audio time of the quantum in which the sequence's first call is
  applied; every cue is sent ahead with its time from there. Why: sample-accurate between cues,
  within a quantum plus the output's ~20 ms of the picture, and cutscenes of seconds need no drift
  correction. (Not also corrected for the output's latency; not re-anchored each frame.)
- **Q28. A music cue at a skip:** applied at the skip with music.js's usual fades (the old theme out
  over 0.55 s, the new in over 0.8 s). Why: the game's only way to change themes, and a hard start
  would click. (Not the new theme at full level at once; not dropped unless the cue says to keep it.)
- **Q29. CSS motion:** the player pauses and resumes the CSS animations of the elements its UI tracks
  name with it, also when the document is hidden (applied at that moment, where Blink would at the
  next frame); a sequence with UI tracks plays at rate 1 only; seeking doesn't reach CSS. Why: M10-9
  keeps the motion in CSS and M10-7 pauses while hidden; this keeps the title card with the picture at
  the least engine change. (Not a playback rate and seeking for CSS animations; not the UI left on the
  document's clock.)

**Step 7 (the proof):**
- **Q30. Its files:** test data only: the stage's Blender script in `Tools/BlenderReference/`, its
  export in `Tests/Statventure.Tests/Data/Models/`, the sequence built in C# in the test (its JSON form
  a committed fixture), the markup and stylesheet in the test; nothing in `world.pak` or `models.pak`.
  Why: its figure is a placeholder, and test data never ships (M9 Q1). (Not the first game content in
  the packs; not the `.blend` file saved too.)
- **Q31. The stand-in stage, not the recommendation:** every element of the old scene remade simply in
  the art style (far, mid and near trunks, the rift opening and pulsing with its glow, the cracks,
  eight eyes blinking, mist as puff particles, the ground, marks for the boss and four imps, the camera
  pushing in), **shown to the user as Blender renders for approval before its tests are written**.
  (The recommendation was the same stage without the approval; not a minimal stage.)
- **Q32. Its checks:** Blender renders at sampled times for the 3D; Chromium captures of the old
  game's title card at the same times (its CSS animations seeked) for the UI; skip and reduced motion
  by the sequencer's tests. Why: both halves against their references, as everything else. (Not rule
  tests plus engine goldens for the user to look at; not without the Chromium part.)
- **Q33. Watching it:** a sandbox mode plays the proof in a window (with the skip button, Escape and
  reduced motion). Why: the user sees the result, not only tests, as `play-theme.sh` was added for
  listening. (Not no sandbox mode.)

## How it works

Filled in as the steps are done.

### Step 1: the node tree and the scene

Q1–Q3 answered by the user on 2026-11-04, each as recommended: the scene replaces `AnimatedView` and
`Actor.Transform` goes (actors stand on nodes); each top-level object of an instanced model is a model
of its own (outline, glow mask, picking); terrain attaches to a node too. Code in
`Engine/Statventure/Scene/` (namespace `Statventure.Scene`; `Animation/AnimatedView.cs` is gone).

- **Nodes** (`SceneNode`): a name, a parent, children in order, a local translation, rotation and
  scale (`Local` = S·R·T in System.Numerics' row vectors, glTF's T·R·S) and `World` = `Local` × the
  parent's `World`, so a non-uniform scale above a turned child gives the sheared world glTF's rule
  gives. A world matrix is worked out when read and again only after a local transform at or above
  the node changed or the node moved to another parent (a stale flag, set down the subtree and
  stopping at nodes already stale; setting a value equal to the current one changes nothing).
  `Add(child)` moves a node with its subtree (keeping its local transform; never under itself or its
  descendants; a scene's root never under anything), `Remove()` takes it out (its world is then its own
  tree's). `Path` joins the names from the root with `/` (the root's is empty); `Find(path)` follows
  first children of each name, `Child(name)`, `Descendant(name)` (depth first) and `Descendants()`
  (a node before its children, children in order). A Blender name containing `/` can't be found by
  path (only by `Descendant`).
- **Attachments** (`SceneAttachment`; `SceneNode.Attach`, `Detach`; a node carries any number, an
  attachment one node): `Actor` (M7's figure, moved here: its model space is its node's, `Transform`
  is gone, its gear unchanged; picking gives the actor back), `PlacedModel` (a model as one model:
  palette, scenery, a `ModelPose`; picking gives it back), `ModelInstance` (below), `PlacedTerrain`
  (terrain at its node's world translation, Q3; rotation and scale don't turn or size it, as
  `Terrain.Draw` takes a translation), `SceneCamera` (glTF's camera: the node's world origin is the
  view's position, one unit along its −Z the target, its +Y the up; projection, field of view,
  orthographic height, near and far its own) and `SceneSun` (glTF's directional light: toward the
  sun is the node's +Z; strength and angle its own). A property set to a new value, or
  `Changed()` after an edit in place (a pose), tells the scene.
- **Instancing** (`SceneNode.Instance(model, palette, scenery)`, Q2): a scene node per model node the
  model's scene reaches (names, hierarchy, translation, rotation and scale; a node the file gives as a
  matrix decomposed), the model's roots under the instancing node; `Roots` are the top-level objects,
  `NodeOf(index)`, `NodeNamed(name)`. It draws through `Model.Draw` with every node's world matrix
  from the scene nodes (skinned meshes follow their joints' nodes), then gives each item its
  top-level object's node as its group (the outline, the glow mask, picking) and that node's origin as
  its hatching anchor; nodes taken out of the scene aren't drawn. `Instance(ModelData)` makes the
  nodes without drawing (a stage's marks and joints, the tests).
- **Clips on scene nodes:** `ModelInstance.Skeleton` is the model's nodes by name, so a clip of the
  model binds to the instance as `ClipBinding` binds to a model (`Clip(name)`), and an
  `AnimationStack` on it plays Blender's NLA on the nodes. `Apply(clip, time)` and
  `Apply(stack, time)` set the nodes a strip sets now from the pose (an object's channels absolute, a
  bone's on its rest, as M7's poses); a node set by the previous call that no strip sets now goes back
  to its rest (the model's own transform), as unset channels are rest for a figure. A clip bound to
  another model's nodes is refused. Nothing advances such a clip on its own: its player gives the time
  (the sequencer's clip track, step 4).
- **The scene** (`SceneView`, a frame client, as `AnimatedView` was): `Root`, `View`, `Rate` (the
  battle pace for the figures), `Camera` and `Sun` (attachments whose nodes the view's camera and sun
  follow; null leaves the view's own), `Actors` (in the tree's order). `Advance(seconds)` advances every
  figure's animator by the time times `Rate`, then `Refresh()`: the camera's settings written into the
  view's camera (the view compares them, so it renders only when they changed) and the sun set when it
  differs; then new draw items only if a node changed (a local transform, the tree, an attachment
  added, taken off or changed), a figure's pose or gear changed (`Actor.Refresh`), the view's style
  changed (it gives the shadow tones the palettes don't), or, with terrain, the view's camera or the
  height of its last picture (the terrain's level of detail; before the first picture the finest).
  Attachments draw, and figures advance and dispatch their events, in the tree's order (depth first, a
  node's attachments before its children's). The view's particles and its clock (`View3D.Time`, the
  water) are left as they are set: particles on nodes are step 5's.
- **Hiding** (Q23): `SceneNode.Hidden` hides the node and its subtree, `Shown` says whether neither it
  nor a node above it is hidden. What hangs on a hidden node or below it isn't drawn (an instanced
  object's nodes included), so nothing there is picked; figures there go on animating and their events
  come, and a camera or sun there still places the view.
- **Internal choices** (the design left them open; accepted by the M10 manager as internal design,
  2026-11-04): (1) the camera and sun attachments come in step 1, by glTF's conventions only, step 3
  adding their import and their check against Blender; (2) particle emitters on nodes are step 5's,
  and the scene leaves the view's particles and clock alone (the battle pace for them is an open item
  under step 5's details); (3) a stage's clips have no clock of their own, the caller gives the time
  (an open item under step 4's details); (4) picking gives back the actor itself (M7's actor had a
  plain object as its owner), a placed model or terrain itself, and an instanced object's top-level
  node; (5) a node a stack no longer sets goes back to its rest, as a figure's unset channels are rest.
- **Blender's parenting as the exporter gives it back** (checked, `SceneReferenceTests`): the
  exporter writes each node's local transform as its parent's world inverted times its own world
  (`nodes.py`, `__gather_trans_rot_scale`; sampled per frame the same way, `sampling_cache.py`), with a
  joint's axes kept as the bone's (an object's glTF world is C·M·C⁻¹, a bone's C·M) and an object on a
  bone placed relative to the bone's head (Blender puts it at the tail: the offset is baked in), so
  parent inverses, bone parents and mirrored parents come back as plain locals. It writes a skin for
  an armature even without a skinned mesh. A sheared world composes from unsheared locals; a parent
  inverse that is itself sheared (a parent scaled unevenly and turned when the child was parented to
  it) makes a sheared local, which the exporter's decomposition into translation, rotation and scale
  can't keep (read in its source, not tried), so stages avoid it (the test model's parents are
  uniformly scaled when parented in place).
- **The reference** (`make_models.py`'s `hierarchy`, `Data/Models/hierarchy.glb`): "Base" (its action
  "base" moving, turning and scaling it unevenly) over "Post" (parented in place: a parent inverse)
  over "Tip"; "Squash" (scaled 1.6 × 0.7 × 1) over the turned "Tilted"; the empty "Mirror" (scale −1 in
  X) over "Wing" over "Feather" (parented in place, so its parent inverse mirrors too); the armature
  "Rig" ("arm", "hand"; its action "rig") with "Charm" on the bone "hand" (parented in place, by the
  tail). `evaluate_animations.py` reads `Reference/node-scenes.json` (written by the tests) beside the
  animation scenes, builds the model again, makes the scene's actions active on their objects (every
  NLA track removed) and writes every object's and bone's world matrix in glTF's coordinates at the
  times (`Golden/nodes-<scene>.json`). The test plays both clips as layers of a stack on the
  instance. On the clips' frames every node's origin and unit axes within M7's on-frame tolerance
  (measured 0.0007 mm); between frames each node in its parent's frame within M7's allowance (measured
  at most 0.68 of it, Base mid-frame).
- **Rendered** (the scene's draw items against Blender's renders, as M6 compares): `SceneRenderTests`
  draws `gear-swing` as an `Actor` on a turned node (sword and pennant on sockets, the pennant's clip
  with the figure's state, the tabard) and `terrain-map` as a `PlacedTerrain` and a `PlacedModel` on
  nodes with the view's camera and sun from nodes, both against their existing goldens; the new scene
  `scene-objects` (`Scenes3D`, `ModelObject3D.Objects`) is the hierarchy model instanced through the
  scene, turned and scaled, each top-level object its own model: `render_scenes.py`'s objects mode
  (`add_model_objects`) shades each top-level object in materials of its own hatched from its origin and
  bakes each into one object for Freestyle's contour, after `flatten_transforms` has put each mesh's
  world matrix into its vertices (clearing parents keeping transforms would lose a sheared world: the
  first reference drew "Tilted" with the wrong shape). Blender's glTF importer leaves an object a clip
  animates at that clip's pose for the scene's frame (1, a frame into a clip it starts at 0): the
  objects mode sets the clips' start frame before clearing the animation data (Base was 2 cm off).

### Step 2: F-curves and easing (`Engine/Statventure/Scene/FCurve*.cs`)

Blender 5.2.0's rules, ported from `blenkernel/intern/fcurve.cc` (evaluation, the handles' update),
`curve.cc` (`calchandleNurb_intern`, `BKE_nurb_handle_smooth_fcurve`), `blenlib/intern/easing.cc`
and `math_solvers.cc` (the tridiagonal solver), in double precision (Q5), times in seconds (Q4).

- **The records and the API** (S2-1, S2-2; `FCurve.cs`): `FCurveKey` is `scene.key` version 1 (time,
  value, `KeyInterpolation` constant to elastic, `KeyEasing` auto, in, out, in-out, the `HandleType`s
  of both sides (free, aligned, vector, auto, auto-clamped), the positions `leftTime`, `leftValue`,
  `rightTime`, `rightValue` of free and aligned sides only, `back`, `amplitude`, `period` in seconds;
  defaults Blender's for an inserted key: Bezier, auto easing, auto-clamped, 1.70158, 0.8, 4.1 frames).
  `FCurve` is `scene.curve` version 1 (`keys`, `extrapolation` constant or linear, `smoothing` none or
  continuous acceleration, the default as Blender 5.2's new F-curves). The record is the curve:
  immutable, `Evaluate(seconds)`, `Handles` (each key's handles as used, seconds), `Start`, `End`,
  equal by content. Helpers make keys: `FCurveKey.Constant`, `Linear`, `Bezier` (computed handles),
  `Ease` (an easing type with its parameters), `Free`, `Aligned`. `FCurve.Load(files, path)` reads one
  from the game's data (a `DataFileException` naming the pack and entry or the file when it breaks
  the rules). A sequence's track (step 4) holds its curves, one per component.
- **The rules a curve keeps** (S2-3): its constructor and the `init` of `Keys` (so also a `with` copy
  and the serializer, which makes it through that constructor) throw an `ArgumentException` naming
  the key and the problem: no keys; keys not increasing at least 0.01 frame (1/6000 s, Blender's merge
  on update) apart; a free or aligned side without its position, an auto or vector side with one; and,
  by S2-3's reason (Blender can't hold them either), numbers that aren't finite, a negative amplitude
  (Blender's are kept at 0 or more) and enum values that aren't members (internal design, accepted
  2026-11-04). `Load` turns it into the
  `DataFileException`; a sequence's loader (step 4) does the same around its own read.
- **Handles** (`FCurveHandles.cs`), resolved once on first use and cached (a `with` copy drops the
  cache), as Blender's `fcurve.update()` leaves them, in frames (Blender's thresholds are frames, and
  aligned handles keep their length in the (frame, value) plane, which isn't free of the time unit):
  a side whose position isn't stored starts where Blender's insertion puts it (a frame off, at the
  key's value: what a one-key curve keeps, as Blender recalculates nothing below two keys); crossed
  handles are swapped, a handle past its key clamped to it (`sort_time_fcurve`); then, with two keys
  or more, `BKE_fcurve_handles_recalc`: every handle kept 0.001 frame off its key's time; auto sides
  along the average of the two neighbouring chords' slopes (an end key mirrors its one neighbour),
  1/2.5614 of the interval long in time with each side at most 5 times the other without smoothing,
  a third of it with smoothing; auto-clamped sides level at a peak or dip (the key locked), otherwise
  kept within the neighbour's value with the other side turned to line up; vector sides a third of
  the way to the neighbour; an aligned side turned to line up with the other keeping its own length
  (both aligned: the left turns, as for an unselected key); the end keys of a constant-extrapolated
  curve with both sides auto made level and locked. Continuous acceleration then solves each run of
  unlocked auto keys between other keys for a continuous second derivative (the right handles'
  value offsets in a tridiagonal system; auto-clamped keys limited to neither reverse nor overshoot,
  the limits locked and released as Blender does; a run's end kept, with the correction for a
  handle other than a third long, or solved for zero acceleration when it is vector or the curve's
  auto end; results written to auto and vector sides, an aligned other side lined up, a curve's end
  mirrored).
- **Evaluation** (`FCurveEvaluation.cs`, `FCurveEasing.cs`), at the frame seconds × 60. At or before
  the first key and at or after the last: the key's value if it is constant or the curve's
  extrapolation is; the line to its neighbour if it is linear (one key: its value); otherwise the
  slope of its outer handle (the first key's left, the last key's right; a one-key curve's free
  handle on its time: its value). Between: the key search (a key within 0.0001 frame of the frame
  gives its value; it shows on constant segments), then the earlier key's interpolation: constant;
  linear; Bezier (all four values within FLT_EPSILON: the first; handles reaching past the segment
  shortened to it, `BKE_fcurve_correct_bezpart`; the parameter where the segment's time is the frame;
  the value there); the easings as `BLI_easing_*` (automatic: out for back, bounce and elastic, in for
  the others; elastic's period 0 is 0.3 of the segment, 0.45 in-out; an amplitude of 0 or below the
  change takes the change, blended in near the segment's end as `USE_ELASTIC_BLEND`).
- **Where the port leaves Blender's code** (not its results; internal design, accepted 2026-11-04): the parameter of a Bezier segment is
  found by Newton's method in a shrinking bracket, not by `solve_cubic`. After the correction the
  segment's time never decreases, so the root in (0, 1) is the only one and both find it; but the
  cubic formula from double coefficients can lose it: where the time runs linearly (continuous
  acceleration's handles at thirds) the coefficients c2 and c3 are rounding residues, and with c3
  exactly 0 the quadratic branch's cancellation errs by up to half the segment (Blender's float
  coefficients keep residues about 10⁻⁷ that hide this). Not ported, as nothing can reach them:
  the branches for a failed search, the second on-key test (within 10⁻⁸ frame of the key after,
  which the search always compares with its threshold), the search's first comparisons with the end
  keys (the search compares every key next to the frame), and the guards for zero lengths (keys are
  0.01 frame apart, handles 0.001 frame off their keys). Left out: the discrete and integer value
  flags (a curve here is a number) and modifiers (Q7).
- **Checked against Blender** (`Tools/BlenderReference/evaluate_curves.py`, run by
  `make-blender-references.sh`; `Tests/Statventure.Tests/Scene/FCurveScenes.cs`, written to
  `Reference/curve-scenes.json` by `FCurveReferenceTests.TheSceneFileIsUpToDate`): 134 curves in 9
  scenes (interpolation, easing (every type in every mode), parameters, handles (every type and mix,
  both smoothing modes), smoothing, extrapolation, spacing, coincident handles, the threshold), 595
  keys at odd spacing, 21,068 times on keys, between them, near the threshold on both sides and
  beyond the ends. Four smoothing cases were found among random curves (by comparing the engine's
  handles with and without a rule of the solver, `claude-work/m10/explore.py`), where no hand-made
  case reached the rule: "released" (the solver locks an auto-clamped handle at its limit and then
  releases it; internal design, accepted 2026-11-04) and "mixed-flat", "mixed-rising",
  "mixed-falling" (auto and auto-clamped keys mixed, so the clamps act on a curve's free end and on
  a level interval: an auto-clamped key beside an equal value is otherwise always locked). The script builds each curve as Blender keyframes (every field set: the Python
  API's `keyframe_points.add` leaves back, amplitude and period 0 and the handles selected; no key
  selected), runs `fcurve.update()`, and writes each key's handles and `fcurve.evaluate` at each time
  (`Golden/curve-<scene>.json`). Blender holds the numbers as float32, so the test makes the engine's
  curve from the same float32 numbers and evaluates it at the float32 frame Blender was given; what is
  left is Blender's float32 arithmetic, bounded by 16 roundings of the sizes involved: the segment's
  values (keys, handles, result; elastic's amplitude) plus the slope times X + 3L frames (X the
  largest key frame, L the segment: how far the time's rounding moves the curve); outside the keys
  the line's slope off by X/h of itself (h its handle's length). Handles within 16 roundings of their
  coordinates' sizes (times 8 with smoothing, the system's growth of rounding). Measured: values at
  most 0.66 of the allowance (an extrapolation 5 frames before a key at frame 600), handles at most
  0.28.
- **Rules the tests check without Blender** (`FCurveTests`, `FCurveRecordTests`): every refusal with
  its message, also through `with` and the JSON form; equality by content; a `with` copy's own
  handles; the helpers; both forms' round trips bit for bit (handles and values too); keys written
  with only time and value read as Blender's defaults; loading from a pack and a loose folder, and
  the refusal naming the file. Layouts and the fixture: `Data/Records/Layouts/scene.curve.txt`,
  `scene.key.txt`, `Fixtures/scene.curve/every-field`.
- **Corrections to this doc's design text** (found in the source): Blender 5.2's default smoothing
  for new F-curves is continuous acceleration (the design didn't say); the end keys' interpolation,
  though nothing follows the last key, decides the extrapolation; Blender's thresholds are in frames
  (search 0.0001, handle clearance 0.001, merge 0.01) and aligned handles depend on the time unit, so
  the curve works in frames inside; the elastic period is a time (seconds here; Blender's default 4.1
  frames); the smoothing needs `math_solvers.cc` besides `fcurve.cc`, `curve.cc` and `easing.cc`;
  `solve_cubic` can't be ported to double as it is (above).

## Status

- Steps 3–7: not done yet (2026-11-04). The decisions and the 33 step details are recorded, the
  design is set, and seven steps are proposed with their dependencies.
- **Step 1, the node tree and the scene (2026-11-04, 1930 tests on the GPU and on lavapipe): done**
  on its worker branch. Q1–Q3 answered as recommended. `Scene/` (`SceneNode`, `SceneAttachment` with
  `PlacedModel`, `PlacedTerrain`, `SceneCamera`, `SceneSun`, `ModelInstance`, `Actor` moved from
  `Animation/AnimatedView.cs`, `SceneView`); `AnimatedView` and `AnimatedViewTests` gone, the M7 tests
  moved to `Tests/Scene/SceneViewTests.cs` (with the "Speed" measurement); `SceneNodeTests`,
  `SceneReferenceTests` (the hierarchy model against Blender), `SceneRenderTests` (two existing
  goldens through the scene), the render scene `scene-objects`; `make_models.py` (`hierarchy`),
  `evaluate_animations.py` (node scenes), `render_scenes.py` (objects mode). Filtered tests (Scene,
  Animation, Render3D) pass: 257. Left to later steps as the design has it: cameras and lights from
  glTF and their check against Blender (step 3; the attachments follow glTF's conventions now),
  particle emitters on nodes and the view's particles (step 5), and a clock for stage clips outside a
  sequence (none: the sequencer gives it, step 4); hiding (Q23) is done here. Mutations: 65 (`~/.cache/stat-fick-build/claude-work/m10/step1-mutations.json`, made by `make-step1-mutations.py` beside it): 62 fail a test (two of them after gaps were closed: a node re-added after its world was read with no parent, and something new attached to a node already shown); one, a node allowed under itself, hangs the test (walking up to the root never ends; `mutate.py` reads the timeout's missing result line as a survivor); two survive and can't differ in results, only in work: marking a subtree stale without stopping at nodes already stale, and marking an already stale node again.
- **Step 2, F-curves and easing (2026-11-04, 1975 tests on the GPU and on lavapipe): done** on its
  worker branch ("How it works", step 2); three details asked on the way, all as recommended
  (S2-1–S2-3). Mutations: 157 (`~/.cache/stat-fick-build/claude-work/m10/step2-mutations.json`, made by
  `make-step2-mutations.py` beside it), 145 killed; 12 survive, none able to differ beyond float32 rounding: equivalent, the clearance's one-ulp step (one double ulp of a clamped handle's time), the both-free early return (nothing below it changes a free side), the second 5× limit (it can only act when the first didn't), an interior auto-clamped peak's lock and runs ending at locked keys (its clamps hold its offset at 0 either way; only a zero's sign changes), a two-key run's early return (solving gives the chord the handle rule gave), the output's guard for computed sides (without cycles every caller passes one), elastic's amplitude-0 branch (f·0 is 0 either way) and its end test (a segment is never evaluated at its own end: that frame is the next key's); below float32's resolution, the flat-segment shortcut (values within FLT_EPSILON); and two of the solver's heuristics no curve among 40,000 random auto-clamped ones tells apart: locking the overshooting handles that go the right way first (only a zero's sign changes) and releasing a handle at most twice (none is released twice). Not checked: nothing left
  against Blender; the curve isn't used by anything yet (step 4's tracks).

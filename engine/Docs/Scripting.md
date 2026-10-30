# Script APIs (M5 #31)

What game code schedules work with, ported from Chromium 152 so the port can mirror the old
JavaScript (user decision 2026-10-25: Chromium's event loop, C# async/await on it). Sources read:
`dom_timer.cc`, `frame_request_callback_collection.cc`, `page_animator.cc`, `local_frame_view.cc`,
`media_query_list.cc`, `media_query_matcher.cc`, `event_target.cc`, `resize_observer*.cc`,
`document_animations.cc`, `pending_animations.cc`, `animation.cc` (all in
`~/.cache/stat-fick-build/reference-src/chromium-152-source/eventloop/`).

## Files

| File | What |
| --- | --- |
| `Engine/Statventure/UI/Dom/UiEventLoop.cs` | Timers, tasks, microtasks; the loop is the thread's SynchronizationContext while game code runs |
| `Engine/Statventure/UI/Dom/UiDocument.Scripting.cs` | The document's forwards (SetTimeout …), frame callbacks, matchMedia, the resize event, the frame's resize observer steps, `UiErrorEvent` |
| `Engine/Statventure/UI/Dom/UiResizeObserver.cs` | ResizeObserver, its entries and sizes |
| `Engine/Statventure/UI/Dom/UiAnimation.cs` | `element.Animate`, `Keyframe`, `AnimateOptions`, the animation object (`UiAnimation`), its effect's timing and keyframes (`ScriptEffect`), `GetComputedStyle` |
| `Engine/Statventure/UI/Style/AnimationEngine.cs` (script animations section) | The timeline (servicing, commit of pending plays and pauses), compositing script animations after CSS animations, interpolation records |
| `Engine/Statventure/UI/Style/ComputedStyle.cs` (`TimingFunction.Evaluate`) | gfx's CubicBezier solver and StepsTimingFunction, unclamped (keyframe easings after an overshooting effect easing) |
| `Engine/Statventure/UI/Dom/UiDocument.cs` (`Update`) | The frame's order of steps |
| `Engine/Statventure/UI/Dom/UiEvents.cs` (`EventListeners.Invoke`) | A microtask checkpoint after each listener the engine calls |
| `Tools/ScriptReference/` | Electron harness: runs scenarios in Chromium 152, logs callbacks tagged with frames |
| `Tools/Scripts/make-script-references.sh` | Writes `Tests/Statventure.Tests/Reference/script-results.json` |
| `Tests/Statventure.Tests/Ui/ScriptFixtures.cs` | Each scenario in JavaScript and its C# twin |
| `Tests/Statventure.Tests/Ui/AnimationScriptFixtures.cs` | The Web Animations scenarios |
| `Tests/Statventure.Tests/Ui/ScriptReferenceTests.cs` | Runs the twins on the engine, compares the logs (`SCRIPT_LOG_DIR=/abs/dir` dumps the engine's) |
| `Tests/Statventure.Tests/Ui/ScriptApiTests.cs` | C#-only behaviour: invalid input, await, other threads, Delay |

## Rules ported

**Event loop.**
- A task (a timer, a host post, `RunScript`) runs, then a microtask checkpoint. A checkpoint also
  follows every callback the engine calls when no game code is running (listeners, frame callbacks,
  media query listeners, resize observers): `EnterScript`/`ExitScript` count the depth, the outermost
  exit runs the checkpoint. A script's own `Click()` dispatches all listeners first, microtasks after;
  real input runs them after each listener.
- C# continuations (`await`, `ContinueWith` on the current context's scheduler) are posted to the
  loop: microtasks on the loop's thread, tasks from other threads. `Task.Yield()` is one microtask,
  as `await null`.
- Timers (DOMTimer): nesting level counted from 1; above 6 and under 4 ms, 4 ms. An interval gets
  one level deeper each time and is clamped at level 7. No 1 ms minimum. Ids are shared by timeouts
  and intervals.
- The host's clock drives the loop: `Update(time)` first runs every timer due by `time` in due order,
  each at its own time, then tasks posted from other threads.

**A frame** (`UiDocument.Update`, Blink's PageAnimator::ServiceScriptedAnimations then the lifecycle):
timers due; posted UI tasks; input timers; scroll animations; the animation timing update and a
microtask checkpoint; the window's `resize` event (when the CSS viewport size changed since the last
frame); `scroll` events; media query list `change` events (each list whose result flipped, in the
order they were made); animation and transition events; frame callbacks; late tasks
(selectionchange); style, animations, boxes, layout; resize observers (which lay out again); the
commit of pending script animations (Blink's PendingAnimations after paint: their start time is this
frame's); a microtask checkpoint (the frame is a task). A nested `Update` during the frame (and
`UpdateLayout`) only does style and layout.

**Frame callbacks:** the callbacks requested before the frame run in order with the frame's time in
ms; one cancelled by an earlier callback of the same frame doesn't run; one requested during the
frame waits for the next.

**ResizeObserver** (resize_observer*.cc, LocalFrameView::NotifyResizeObservers):
- An observation starts at an impossible size, so the first delivery reports every target, 0×0 too.
- Each round gathers, per observer in creation order, the targets whose observed size (LayoutUnits,
  CSS px) changed and that are deeper than the previous round's shallowest (depth = elements up to
  the root); shallower ones are skipped. Each observer with targets is called with entries in
  observation order; then style and layout run again.
- When only skipped targets are left: the window gets an `error` event ("ResizeObserver loop
  completed with undelivered notifications."), and a new frame is scheduled to deliver them.
- Entries: content rect x,y = padding left/top; sizes over the zoom as LayoutUnits; the device pixel
  content box is snapped from the box's paint offset (SnapSizeToPixel). Inline boxes and elements
  without a box report 0. SVG elements report their bounding box (paint offset 0).
- Observing a target again with the same box does nothing; with another box starts over. Targets
  keep their observers alive (weak list in the document, as Blink's controller).

**matchMedia:** `Matches` reads the current environment; the change event is per list, when its
result differs from the one last reported. A list with listeners stays alive when game code drops it
(Blink's HasPendingActivity); one without is held weakly.

**Web Animations** (user decisions 2026-10-30: the core of `element.animate`, keyframes as
dictionaries of CSS text; animation.cc, pending_animations.cc, timing*.cc, keyframe_effect_model.cc,
interpolation_effect.cc, animation_clock.cc, gfx cubic_bezier.cc and timing_function.cc):
- `el.Animate([new Keyframe { ["transform"] = "translateY(0)", Offset = 0.3, Easing = "ease-in" }, …],
  new AnimateOptions { Duration, Delay, EndDelay, Iterations, Direction, Fill, Easing })` (or a
  duration in ms) makes an animation and plays it. Property names are CSS names or camelCase;
  values that don't parse are left out; bad offsets, easings, durations or iteration counts throw
  `ArgumentException` (TypeError).
- The animation object follows Blink's procedures with the playback rate 1: hold time and start
  time; play from the start when idle, finished or before the start; pause and play pending until
  the frame's commit (after layout; the start time is the frame's timeline time, `Ready` completes
  in a task after the frame); finish() at once (Finished completes now, the finish event with the
  next frame's animation events); cancel() (pending Ready and Finished cancelled, a cancel event);
  seeking with `CurrentTime`/`StartTime`; the finished state updated in every frame's timing update,
  its notification in a microtask (Finished, then the finish event queued with the frame's
  animation events). `Ready` and `Finished` are made when first read and replaced when reset, as
  Blink's promise properties. Events only come when a listener (`OnFinish`, `OnCancel`,
  `AddEventListener`) is there.
- The document timeline (Blink's AnimationClock): during a frame's rendering steps the frame's
  time; in tasks between frames the last frame's time advanced by whole 1/60 s steps.
- Effect timing: Timing::CalculateTimings (phases with Blink's 1 µs tolerance, active time, fill,
  iterations, directions, the effect easing with the before flag for steps).
- Keyframes: computed offsets (a missing last one is 1, a missing first 0, the rest spaced),
  per-property groups with neutral keyframes at 0 and 1 when missing (the underlying value, with
  the easing of the last keyframe at 0), interpolation records reaching out to infinity at both ends
  (so an overshooting easing extrapolates), each with its first keyframe's easing.
- Compositing: CSS transitions, CSS animations, then script animations in creation order
  (`replace`), `!important` declarations over all of them. A script animation keeps running while
  its element is hidden or removed (it still finishes).
- The frame's timing update marks animated elements for a new style sample, so `GetComputedStyle()`
  in a frame callback sees the frame's values (Blink's SetNeedsAnimationStyleRecalc).

**C# notes:** `Finished`/`Ready` are `Task<UiAnimation>`; a rejection with AbortError is a
cancelled task (`await` throws `OperationCanceledException`). `await` of a task that has already
completed continues at once (JavaScript's `.then` always waits a microtask); a continuation always
returns to the loop (on its thread a microtask, from another thread a task before the next frame).

## Tests

`ScriptReferenceTests` runs each fixture as the harness runs its JavaScript: the page laid out at
time 0, the scenario as a 0 ms timer right after that frame (it first registers a frame counter),
frames at 60 Hz, harness requests (`__click`, `__resize`) done between frames. Only lines of frames
before the last are compared (the harness stops reading somewhere in the last).

Adding a scenario: write it in JavaScript and as a C# twin in `ScriptFixtures.cs` (or
`AnimationScriptFixtures.cs`; `ScriptContext` has `Log`, `Then` for promise-like reactions, `Frames`,
`Fixed`, `Js`, `Click`, `Resize`), run `Tools/Scripts/test.sh` once (it writes
`Reference/script-fixtures.json`), then `Tools/Scripts/make-script-references.sh` (replays every
scenario, about a minute; run it twice and check the results are the same), then the tests again.

Fixtures: `microtasks`, `listeners`, `timers`, `matchmedia`, `frame-order`, `resize-observer`,
`resize-observer-depth`, `resize-observer-window`, `animate-playback`, `animate-sampling` (the
effect seeked to chosen times: offsets, easings, timing, directions, fills, the game's battle
keyframes), `animate-control`, `animate-composite`. All 13 match Chromium.

Capture gotchas: timers with a delay are real time in Chromium, so fixtures use 0 ms timers only
(and the harness starts the scenario right after a frame, giving them ~16 ms before the next);
scenario code runs in its own block (a `const` clashed with the harness's). Chromium's offscreen
frame times jitter by about ±0.1 ms: playback logs whole frames and values to two decimals, with
durations chosen so no value or end falls within half a millisecond of a rounding edge or a frame
(90 ms put 16.67/90 = 0.1852 on the edge and failed one capture in three); exact values come from
seeking a paused animation. Capture at least twice and compare.

## Known differences left

- Composited animations (opacity, transform, filter) get their start time from cc in Chromium; the
  engine uses the main-thread commit time (the references show the same frame).
- `Animate` needs an element in a document; no pseudo-element targets, no playbackRate.
- The clock for CSS animations in tasks between frames is the next frame's (as before);
  script animations use Blink's clock there.

## Status

#31 done (2026-10-30, 491 tests); M5 finished (2026-10-30, 493 tests).

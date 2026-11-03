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
| `Engine/Statventure/UI/Dom/UiDocument.Visibility.cs` | Hidden documents: `Hidden`, `VisibilityState`, `SetHidden`, `Audible`, `Unload`, `NextWakeUp`, the first contentful paint, the update without a frame |
| `Engine/Statventure/UI/Dom/UiTimerThrottling.cs` | Blink's throttling of a hidden page's timers: the wake-up and CPU-time budget pools, the timer kinds, the scheduler's timed changes |
| `Engine/Statventure/UI/Dom/UiEventLoop.cs` (`RunThrottledUntil`, `NextWakeUp`, `RunTimer`) | Timers run when the throttling allows; a late interval skips the beats it missed (TimerBase) |
| `Engine/Statventure/Audio/Output/Audibility.cs` | Whether the sound is audible, as Chromium's audio service decides it (Docs/Audio.md, "Audibility") |
| `Tools/VisibilityReference/`, `Tools/Scripts/make-visibility-references.sh` | Electron harness: hides, shows and closes pages, and keeps them hidden for minutes; writes `Reference/visibility-results.json` |
| `Tests/Statventure.Tests/Ui/VisibilityReferenceTests.cs` | The visibility scenarios' C# twins against Chromium's logs |
| `Tests/Statventure.Tests/Ui/HiddenDocumentTests.cs` | The rules the references don't reach alone (the pools as their sources define them, no frames, unloading, the grace periods, contentful paints) |

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

## Hidden documents (2026-11-03)

The window host's side of a page's life in its window, as Chromium 152 (Electron 44.5.0, the old
game's) runs it; the user's decisions are in Engine-Plan.md, "Decisions for the window's
visibility". Sources (`~/.cache/stat-fick-build/reference-src/chromium-152-source/scheduler/`):
`page_scheduler_impl.cc`, `frame_scheduler_impl.cc`, `task_queue_throttler.cc`,
`wake_up_budget_pool.cc`, `cpu_time_budget_pool.cc`, `dom_timer.cc`, `timer.cc`, `paint_timing.cc`,
`first_meaningful_paint_detector.cc`, `audio_stream_monitor.cc`, `output_stream.cc`,
`audio_power_monitor.cc`, `message_pump.cc`.

**Visibility.** A minimized window's document is hidden (GLFW's iconify, X11 and Windows; Wayland
never reports it, so there it stays visible, the user's decision). The host passes the window
system's events in order: minimizing gives the window's blur (element blur, focusout, the window's
blur, a plain Event) and then the visibility change; restoring gives the visibility change and then
the window's focus (as the window manager sends them; Chromium's log shows the same order). The
change (`UiDocument.SetHidden`, Blink's Document::DidChangeVisibilityState): `Hidden` and
`VisibilityState` change first, then `visibilitychange` and `webkitvisibilitychange` at the document,
bubbling, not cancelable, trusted. The host makes the change at its update's time: a hidden
document's clock waits at its last wake-up, so it first runs what its throttling allowed until then
(otherwise a late interval would replay the beats it missed as if it had been visible).

**No frames while hidden.** `Update(time)` on a hidden document runs only its tasks: throttled
timers, posted tasks, the document's own tasks; no frame steps (no frame callbacks, animation
events, resize observers, painting). The application loop keeps calling its update while the
window is minimized, without drawing, and sleeps until the next wake-up the host asks for
(`UiDocument.NextWakeUp`: the next timer the throttling allows or the throttling's next change;
`Window.RequestWakeUp`) or an event; a task posted from another thread wakes it
(`Window.PostEmptyEvent`).

**Timer kinds** (DOMTimer): deeper than nesting level 6, high nesting; else a 0 ms timer is
immediate (never throttled); else low nesting. An interval becomes high nesting at level 7.

**Throttling** (ComputeThrottlingType): only while the page is hidden and not playing audio. Audio
playing means audible now or within 30 s (PageSchedulerImpl::kRecentAudioDelay); the host passes
the sound's audibility (`UiHost.Audio` → `UiDocument.Audible`). Then:
- Low-nesting timers wake through the hidden pool: at most once a second, on the second.
- High-nesting timers wake through the intensive pool: once a second until the grace period after
  hiding is over, then once a minute. The grace period is 60 s once the page had its first
  contentful paint (and its first meaningful paint, which without network loads is the same), else
  5 minutes (GetIntensiveWakeUpThrottlingGracePeriod). The intensive pool wakes at least a minute
  after its last wake-up on the second, or on the minute's tick, whichever comes first
  (AllowLowerAlignmentIfNoRecentWakeUp): a pool quiet for a minute wakes on the next second.
- A wake-up (WakeUpBudgetPool): ticks are `interval` apart through the alignment origin
  (`EventLoop.WakeUpAlignment`; Blink aligns to the monotonic clock's zero, arbitrary to a page);
  a pool never woken wakes on the next tick; a wake-up lets the tasks due within 3 ms run.
- The CPU-time budget (CPUTimeBudgetPool with the background settings): enabled 10 s after hiding;
  throttled timer tasks charge their run time (measured; tests inject it); the budget starts at
  1 s, refills at 1% of the time while enabled, holds at most 3 s; while it is negative tasks wait
  until it would have refilled. (Blink's quirk kept: the refill counts from the last checkpoint,
  which only throttled runs move.)
- A task runs at the latest of the times its pools allow; the scheduler's own changes (the CPU
  budget's start, the grace period's end, the end of recent audio) happen at their times first.
- A late interval (TimerBase::RunInternal) skips the beats it missed: its next beat is the first
  on its own grid after now + interval / 20.

**Unloading** (`UiDocument.Unload`, Document::DispatchUnloadEvents without `beforeunload`, the user's
decision): `pagehide` (a `UiPageTransitionEvent`, persisted false, bubbling, cancelable), then if
the document was visible it turns hidden with `visibilitychange` and `webkitvisibilitychange`, then
`unload`; then its timers stop and it takes no more updates or tasks. `UiHost.Unload` does it;
`UiHost.Dispose` does it if it wasn't done (the window closing ends the loop, the application's
`Unload` disposes the host). The window's and the document's listeners are one in the engine, so
`pagehide` and `unload` reach the document's listeners (Chromium sends them at the window with the
document as the target).

**The first contentful paint** (`UiDocument.FirstContentfulPaint`): the time of the first painted
frame with text, a drawn image, SVG or a surface (the painter reports it).

**References** (`Tools/VisibilityReference`, `make-visibility-references.sh`, about six minutes):
each scenario a window of its own, shown, background throttling on as in the old game; hiding
stands in for minimizing (minimizing needs a window manager: with openbox under Xvfb it gave the
same lines). `events` (hide, show, close: every visibility, page and focus event with its
properties), `timers` (an interval, 0 ms and 250 ms chains, one-shots made while hidden, a new
chain after a quiet minute; 260 s hidden), `cpu` (an interval busy for 150 ms, 40 s hidden),
`audible` (an oscillator stopped when hidden, 50 s hidden). The page logs through console messages,
so lines written while it unloads arrive. `VisibilityReferenceTests` replays each as the host
would and compares: the events exactly (window and document targets merged, no beforeunload); the
timers' firings by kind and count, each no earlier than Chromium's and at most 9 ms before it
(Chromium's throttled wake-ups are "flexible, no sooner" with base's 8 ms leeway on Linux, plus the
page's rounding); the CPU budget's wait within the overhead Chromium also charges and the page
can't see (0.25 ms per charged run allowed, each millisecond of debt waits 100 ms); the audible
page's 30 s within the 0.6 s Chromium takes to hear the silence.

**Checked in a real window** (2026-11-03, Xvfb with openbox, the sandbox's `--log-visibility`):
minimizing blurs and hides the document, its 250 ms interval then runs on whole seconds,
restoring shows it and focuses the window and the late interval runs once, then on its beat;
closing sends pagehide, visibilitychange and unload.

**Known differences (hidden documents):**
- Wayland: GLFW doesn't report minimizing there, so a minimized window's document stays visible
  (the user's decision; check on the PC with the sandbox's `--log-visibility`).
- Chromium runs a throttled wake-up up to 8 ms late (its leeway); the engine on time.
- Tasks of one wake-up all start at its time (the engine's clock doesn't move during a task); a
  task's measured cost still goes to the CPU budget.
- At a µs tie between the grace period's end and a wake-up the engine changes the throttling first.
- The sound pauses at once when the window is minimized (M8), so the engine's 30 s of recent audio
  start at once; Chromium hears an old game's music stop a few hundred ms after it stopped it.
- No beforeunload, freeze or resume (the user's decision; no back-forward cache or page freezing on
  desktop Chromium either).

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

#31 done (2026-10-30, 491 tests); M5 finished (2026-10-30, 493 tests). Hidden documents (the small
task "send `visibilitychange` and `pagehide`") done 2026-11-03 (1046 tests).

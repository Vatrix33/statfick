# Audio (M8)

The game's sound: the old game's music, rebuilt as a game synthesizer whose output matches Chromium
152's Web Audio renders of the old scripts sample for sample, plus synthesized sound effects, played
through OpenAL Soft at the device's rate.

## Decisions (user, 2026-10-31)

Asked before the milestone started; also in `Docs/Engine-Plan.md`, "Decisions for M8".

- **Fidelity:** sample for sample, within float rounding. Chromium 152's Web Audio processing is
  ported (the oscillators' band-limited wavetables, the biquad formulas, the AudioParam automation
  timeline, 128-frame render quanta, the convolver, the compressor, the stereo panner, buffer
  sources) and offline renders of every theme are compared with Chromium's (OfflineAudioContext).
- **A game synthesizer:** the engine offers instruments, voices, a sequencer, effects and buses
  shaped for the game; the old music scripts are rewritten onto it, producing the same sound. (A
  Web Audio-like public API was not chosen.)
- **Private Blink nodes underneath:** the ported processing nodes stay internal; each instrument
  builds the same small graph per note as the old scripts did, so renders match Chromium exactly.
  The game sees themes, cross-fades, effects and bus volumes.
- **Music data:** generated C# data records (a script reads the old JavaScript data in the
  harness's Chromium and writes them into the engine's source); M9 adds the pack form, M11 the editor.
- **The old score engine is dropped:** every one of music.js's 24 original scores is replaced at
  load by a remastered theme from music-composer.js (38 themes and the regional battle
  arrangements); only the composer, its themes and music.js's master chain (master gain, limiter,
  theme fades) are ported. So no DelayNode and no `Math.random` (the composer's randomness is its
  own seeded mulberry32).
- **Sound effects:** synthesized from the same nodes (one-shot patches as data), on an effects bus,
  triggered by the game or by clip events (Docs/Animation.md); no audio files. Which sounds the
  game gets is decided at port time.
- **Output:** at the audio device's own rate, as Chromium's AudioContext runs, in 128-frame quanta,
  about 20 ms queued ahead.

## What the old game uses (surveyed 2026-10-31)

- **music.js:** the master chain: a gain of 0.58 × the volume setting (1 for exports) into a
  DynamicsCompressor (threshold −18 dB, knee 18, ratio 3, attack 6 ms, release 220 ms) into the
  destination; theme selection (`desiredTheme`: the region's theme, a battle's regional arrangement
  `combat@<region>` or a boss theme); switching: the old bus `cancelAndHoldAtTime(now)` then a linear
  ramp to 0 over 0.55 s, the new performance's bus from 0 to 1 over 0.8 s; a 45 ms timer scheduling
  steps 0.2 s ahead; stopping when the document is hidden; `renderOffline(theme, rate)` (one pass
  of the form plus 4.5 s of tail, steps at 0.05 + n · stepSeconds) gives the references. The volume
  is one slider, 0–100 %, saved in localStorage.
- **music-composer.js** (`compile`, `perform`, `lint`): themes compiled from specs (forms,
  sections, layers, grooves, palettes) into timed steps; a performance builds per-note graphs:
  oscillators (sine, triangle, sawtooth, square and custom periodic waves from harmonic tables),
  noise and plucked buffers (looping, playbackRate), biquads (lowpass, highpass, bandpass, peaking,
  lowshelf, with Q), gains with setValueAtTime, linear and exponential ramps and setTargetAtTime,
  low-frequency oscillators modulating gains and a frequency, stereo panners, a convolver room per
  theme (an impulse response generated from seeded noise), sends and EQ.
- **music-themes.js:** the theme specs (38, plus the regional battle variations by `arrange`) and
  per-theme gains.

## Method

As M3–M5 did with Chromium:
1. **Reference first.** A harness (`Tools/AudioReference`, an Electron app run by
   `Tools/Scripts/chromium.sh`) renders in Chromium 152's OfflineAudioContext: small fixtures (a
   graph in JavaScript and its C# twin, a fraction of a second each) for each node and rule, and
   the old game's themes through `Music.renderOffline`. Renders are committed as references, so the
   tests run anywhere.
2. **Port the rule** from Blink's source (`~/.cache/stat-fick-build/reference-src/chromium-152-source/webaudio/`:
   `modules/webaudio` handlers and `platform/audio` DSP, the x86 vector math, Chromium's pffft),
   in the same float operations and order, rather than tuning until renders match.
3. **Test against the reference** within float rounding (the bound worked out per rule from float32
   precision, not fitted); every rule gets a test that fails when the rule is broken.

## Steps

1. **Harness and the render core:** `Tools/AudioReference` and its capture script, the reference
   file format; the private graph (render quanta of 128 frames, pulling inputs, channel
   up/down-mixing as Blink's AudioBus, summing), AudioParam with Blink's automation timeline (every
   method, a-rate and k-rate, audio-rate inputs to parameters), gain, buffer sources (loop,
   playbackRate, offset, start/stop), offline rendering. Fixtures for each.
2. **Oscillators and periodic waves:** Blink's band-limited wavetables (sine, square, sawtooth,
   triangle, custom harmonics), frequency and detune, start/stop.
3. **Biquad filters:** the coefficient formulas of each type the composer uses (and the rest Blink
   has), parameter automation, the filter state.
4. **Convolver and stereo panner:** Blink's Reverb (normalization, partitioned FFT convolution with
   pffft, the background stage threading reproduced deterministically), the panner's equal-power law.
5. **Dynamics compressor:** Blink's compressor kernel (pre-delay, detector, knee curve, makeup gain).
6. **The game synthesizer:** the composer ported onto the private nodes (instruments, rooms,
   performances, the sequencer), the master chain and theme fades; the theme data generated as C#
   records from music-themes.js; every theme's offline render compared with Chromium's.
7. **Output:** OpenAL Soft streaming at the device's rate, 128-frame quanta, about 20 ms queued, an
   audio thread rendering ahead, buses (master, music, effects) with volumes, device changes and no
   device.
8. **Sound effects:** one-shot patches as data on the effects bus, triggered from game code and clip
   events; tests against Chromium renders of the same patches.

## How it works

### The render core (step 1)

All of it is internal to the engine (`Engine/Statventure/Audio/`); the game will see the synthesizer
of step 6, not these nodes.

- **`AudioGraph.cs`:** `AudioGraph` is the context: a float sample rate (as Blink keeps it), a frame
  clock, the destination (an offline destination: explicit channel count, speakers). `RenderQuantum`
  is OfflineAudioDestinationHandler::RenderIfNotSuspended: the pre-render task finishing sources past
  their end (HandleStoppableSourceNodes, which also finishes sources nothing pulls), the destination's
  input pulled into the bus, the clock advanced, finished sources released and their `Ended`
  callbacks run. `RenderOffline(length)` is an OfflineAudioContext's render. `AudioNode` is Blink's
  AudioHandler: `ProcessIfNecessary` (once per quantum; inputs pulled; silent inputs past the node's
  tail and latency silence its outputs and only advance its parameters, ProcessOnlyAudioParams),
  channel count, mode (max, clamped-max, explicit) and interpretation. `AudioNodeInput` passes one
  connection through in max mode, else zeroes its summing bus and sums each connection into it;
  `AudioNodeOutput` renders into the puller's bus when it's the only connection of matching
  channels (in place), and announces channel count changes to the inputs it feeds.
- **`AudioBus.cs`:** Blink's AudioChannel and AudioBus with the silent flag's rules (zeroing only
  what isn't silent, sums from silent channels skipped, into silent channels copied), speaker and
  discrete mixing for mono and stereo (other layouts are refused: the game is stereo), the gain
  copies; `VectorMath` the element-wise kernels as the x86 SSE code computes them (no fused
  multiply-adds; min and max in SSE's operand order).
- **`AudioParam.cs`:** AudioParamHandler with its automation timeline line for line: every method
  (setValueAtTime, linear and exponential ramps, setTargetAtTime, setValueCurveAtTime,
  cancelScheduledValues, cancelAndHoldAtTime, the value setter), the event list's insertion, overlap
  errors, pruning and clamping of late events to the current time, a-rate values per sample and
  k-rate values at the control rate (rate / 128), the x86 SSE paths of the linear ramp (float lanes),
  setTarget (four steps from one exponential) and the value curve, each float and double conversion
  where Blink has it, inputs to parameters summed in (mixed down to mono; k-rate takes the first
  sample), NaN replaced by the default value, the range applied.
- **`Sources.cs`:** `AudioBuffer`; `AudioScheduledSourceNode` (start, stop, the quantum's playing
  part with times rounded at 1024 times the rate, the start's fraction of a frame, finishing and
  ended callbacks); `AudioBufferSourceNode` (offset, duration, loops and loop points, reverse,
  playback rate and detune as k-rate parameters, the buffer's own rate, the fast copying path and
  the interpolated one, stopping finite sources nothing pulls); `GainNode`.
- **`Fdlibm.cs`:** fdlibm's exp and pow as Blink builds them (`third_party/fdlibm`): Blink computes
  its exponentials, the discrete time constant, detune and exponential ramps with them, and the C
  library's give other results in about 6-7 % of such inputs (an ulp off). Checked bit for bit against
  Blink's own fdlibm, built natively by `Tools/Scripts/make-fdlibm-reference.sh` from
  `Tools/FdlibmReference` (Chromium 152's V8 can't be the reference: its `Math.exp` is no longer
  fdlibm, node 24's still is, and V8's `Math.pow` is the C library's).

**Single-threaded:** Blink renders on an audio thread and applies the main thread's changes at the
next quantum; here a change takes effect at once, and a source's `Ended` runs after the quantum in
which it finished (Blink's main thread sees it some time later, at no fixed point).

**Left out because they never change the output:** Blink's copies of the graph for the audio thread
(rendering connections), disabled outputs of nodes whose inputs are all gone, and the tail-processing
list keeping such nodes alive (a node with no input connections outputs silence either way).

**Chromium's summing order is not fixed.** An input sums its connections in the order of a hash set
of pointers, which changes from run to run: seven renders of a three-term mix gave two different
results, seven of a four-input mix six. Fixtures summing three or more terms are compared within
2n · u · M (n terms of total magnitude M, u = 2⁻²⁴: two orders' rounding errors plus one stage
after); with two terms the order doesn't matter. The engine sums in connection order.

**Denormals:** Chromium renders with denormals flushed to zero; the engine doesn't, so samples may
differ by up to the smallest normal float (1.2 · 10⁻³⁸), the comparison's floor.

**The reference:** `Tools/AudioReference` (Electron 44.5.0 = Chromium 152) renders each fixture's
script in an OfflineAudioContext and writes planar little-endian float32, gzipped, to
`Tests/Statventure.Tests/Golden/audio/<name>.f32.gz` (`Tools/Scripts/make-audio-references.sh
[names]`). The fixtures (`Tests/Statventure.Tests/Audio/AudioFixtures.cs`) hold each script and its
C# twin; the tests write `Reference/audio-fixtures.json` for the harness. A script may call
`ctx.suspend(t).then(...)` (the twin's `Suspend`) to change the graph while rendering, as the game's
scheduler and theme fades do. Noise comes from the composer's mulberry32 in both languages.

## Status

- **Step 1, harness and render core: done (2026-10-31, 681 tests).** 39 fixtures (buffer sources:
  start between frames, offsets and durations, loop points, whole-buffer loops, rate and detune,
  k-rate automation, buffer rates, reverse, stereo to mono; gains; the timeline: linear,
  exponential, target, curves, cancelling, a curve's end held, cancel-and-hold during each kind of
  automation, theme fades while rendering, first events between frames, a single future event,
  events clamped to now, past events in reverse; inputs to parameters: audio-rate, stereo, k-rate,
  NaN; mixing: many inputs, explicit and clamped-max channel counts, discrete; fan-out; silent
  inputs with automation running) match Chromium bit for bit, the two summing three or more terms
  within their summing-order bound. Unit tests for what renders can't show (ended timing, sources
  nothing pulls, script errors, disconnecting, a render ending mid-quantum) and the fdlibm reference.
  62 mutations of the rules: 51 fail a test; the other 11 change nothing a render shows:
  optimizations (silence propagation, all-past events, the gain's k-rate path), double-precision
  steps whose differences vanish in the float result (the interpolation formula's form, the ramp
  increment's rounding, the association in the held ramp values, the C library's exp and pow at two
  call sites; fdlibm itself is checked bit for bit), and two values always overwritten before use
  (the exponential ramp's snap to its end, a set value current within its frame).
- **Chromium quirk kept:** `cancelAndHoldAtTime` during a `setTargetAtTime` followed by a ramp
  starts the ramp from 0, not from the held value (param-cancel-hold-target; the game's fades hold
  linear ramps, which work as expected).
- **Next: step 2, oscillators and periodic waves.** Blink's x86 oscillator kernel
  (`modules/webaudio/cpu/x86/oscillator_kernel_sse2.cc`) is now in the reference sources too; the
  oscillator's detune uses the C library's `exp2` (glibc on Linux), unlike the rest.

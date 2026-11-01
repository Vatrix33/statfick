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
- **The FFT (asked during step 2):** Chromium 152 computes Web Audio's FFTs (periodic wave tables,
  the convolver) with the RustFFT 6.4.1 crate, which on this PC's CPU (AVX2 + FMA) runs its AVX/FMA
  algorithms; the engine ports exactly those, bit for bit, emulating AVX and FMA on CPUs without
  them. (Not RustFFT's scalar path or pffft within a rounding bound.)
- **The convolver's partitioning (asked during step 4):** always the offline structure (stage FFTs
  doubling from 128 to 32768 points, all on the audio thread), as the references render it; not
  Chromium's live structure (2048-point stages, later ones on a background thread). The big
  stages' cost is measured in step 7.
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
   `modules/webaudio` handlers and `platform/audio` DSP, the x86 vector math and kernels; the
   RustFFT 6.4.1 crate in `~/.cache/stat-fick-build/reference-src/rustfft-6.4.1/`; glibc 2.43's
   float functions in `.../reference-src/glibc-2.43/`),
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
   the Rust FFT, the background stage threading reproduced deterministically), the panner's
   equal-power law.
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
  Blink's own fdlibm, built natively by `Tools/Scripts/make-native-references.sh` from
  `Tools/NativeReference` (Chromium 152's V8 can't be the reference: its `Math.exp` is no longer
  fdlibm, node 24's still is, and V8's `Math.pow` is the C library's).

**Single-threaded:** Blink renders on an audio thread and applies the main thread's changes at the
next quantum; here a change takes effect at once, and a source's `Ended` runs after the quantum in
which it finished (Blink's main thread sees it some time later, at no fixed point).

**Left out because they never change the output:** Blink's copies of the graph for the audio thread
(rendering connections). (Disabled outputs were left out at first too, but they do change the
output: ported in step 4, see "Connection counting".)

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

### Oscillators and periodic waves (step 2)

- **`RustFft.cs`:** Chromium 152 doesn't use pffft for Web Audio: the `WebAudioRustFft` feature is
  stable, so Blink's FFTFrame calls `rustfft_ffi.rs` (RustFFT 6.4.1). Its planner picks the AVX/FMA
  algorithms on this CPU (user decision, "The FFT"): for f32 powers of two, a 256- or 512-point AVX
  butterfly (512 when log2 of the length is a multiple of 3), then MixedRadix 16/8/4/2xn stages
  (`plan_mixed_radix`). Ported: `V8`, one AVX register of four complex floats with the intrinsics
  the code uses (adds and multiplies lane by lane, `fmaddsub`/`fmsubadd` fused, the shuffles as bit
  moves; AVX and FMA instructions when the CPU has them, the same lane arithmetic otherwise);
  RustFFT's column butterflies 2/4/8/16/32 with their rotations and twiddles, Butterfly128/256/512Avx,
  MixedRadix4xn and 8xnAvx (the lengths Web Audio needs: 1024 = 256 × 4, 2048 = 256 × 8, 4096 = 512
  × 8, 8192 = 256 × 8 × 4, 16384 = 256 × 8 × 8); twiddles in double with the C library's cos and sin,
  rounded to float. `RealFft` is Blink's even-size real FFT around a half-size complex one (its
  packing in num-complex's unfused float arithmetic; DC in real[0], Nyquist in imag[0]; the inverse
  scaled by 1 / half size).
- **`Libm.cs`:** glibc 2.43's exp2f and log2f, which Blink calls for detune (std::exp2) and pitch
  ranges (log2f): not correctly rounded (about 20 000 and 45 000 floats differ from a double result
  rounded). x86-64 glibc picks variants built with FMA on CPUs that have it; ported with those fused
  steps, though the unfused SSE2 variants give the same float for every input (counted by the native
  reference), so the CPU's choice doesn't matter. Checked against the system's glibc on all 2³²
  floats (`Tools/Scripts/make-native-references.sh` hashes them; `LibmTests` checks every 4099th,
  all of them with `STATVENTURE_EXHAUSTIVE=1`). On Linux .NET's MathF calls glibc too; the port makes
  Windows give the same results.
- **`PeriodicWave.cs`:** PeriodicWaveImpl: 2048-point tables up to 24 kHz, 4096 up to 88.2 kHz, else
  16384; one table per third of an octave (36 at 48 kHz), each the inverse FFT of the harmonics a
  fundamental in its range keeps below Nyquist (culled by pow(2, −cents/1200)), normalized by the
  first table's peak (unless disabled); the four basic shapes from their Fourier series; the pair
  of tables around a frequency and the factor between them.
- **`Oscillator.cs`:** OscillatorHandler with the SSE2 kernels (`oscillator_kernel_sse2.cc`,
  fetched into the reference sources): phase increments per sample when frequency or detune is
  automated (detune's multipliers, frequency clamped to Nyquist), else one frequency per quantum;
  the k-rate vector path (four float read positions, wrapped in float), its scalar tail and the
  a-rate kernel (positions in double), linear interpolation between table samples and between the
  two ranges' tables, 3- and 5-point Lagrange interpolation for increments under 0.3 and 0.16;
  start between frames; the shape changed while playing; stopping oscillators nothing pulls.
  Blink's quirks kept: k-rate parameters are read twice a quantum; a note starting mid-quantum takes
  the quantum's increments from its first frame; the k-rate path advances its position by n · incr
  as a float product.

**Reading the tables out:** at 16 384, 32 768 and 131 072 Hz an 8 Hz oscillator steps exactly one
sample of the second range's table per frame (no interpolation), and 32 Hz every fourth sample of
the eighth range's; these fixtures (`osc-tables-*`) show Chromium's tables for every shape, one
table size and FFT plan per rate. (Rust can't be installed here to build RustFFT natively, so the
renders are the FFT's reference.)

### Biquad filters (step 3)

- **`Biquad.cs`:** Blink's Biquad (platform/audio/biquad.cc) per channel: the Audio EQ Cookbook
  coefficients of all eight types with fdlibm's sin and cos and its float expf for the decibels
  (`pow10`; Q is in dB for lowpass and highpass), their edge cases (cutoff 0 or Nyquist, Q of 0 or
  below), normalized by multiplying with 1/a0; the direct form I recursion in double over float
  samples, with coefficients per frame while a parameter moves. Chromium renders with denormals
  flushed (FTZ and DAZ): here a denormal input reads as zero and a result below the smallest
  normal float becomes zero, so filter decays match to the bit (a fixture checks it exactly).
  `TailFrame` ports the tail analysis (real, complex and repeated poles, the root finder, until the
  response is below 1/32768, at most 30 s): it decides which quantum a filter whose input has
  fallen silent stops in. libc++'s complex arithmetic is reproduced; its `hypot` is the C
  library's, here .NET's (an ulp at most, which could only move the stop at an exact tie).
- **`BiquadFilterNode`:** BiquadFilterHandler: the four parameters checked for sample-accurate
  values (all a-rate by default; per-frame coefficients when one moves within the quantum, once
  when they hold still), else recomputed only when a value changed (Blink reads k-rate values twice
  then); the input pulled into the output's own bus (in place); a type change or a change of the
  input's channel count clears the filters' state; while silent, the parameters still advance.
- **`Fdlibm.cs`:** now also sin, cos (with their kernels and the argument reduction up to 2¹⁹ · π/2),
  log and expf, checked against Blink's fdlibm by the native reference.

**Summing order through filters:** Chromium's run-to-run summing order (step 1) matters more ahead
of low filters: three notes summed into the composer's master EQ (a 34 Hz highpass, a 140 Hz shelf)
gave differences of 2 · 10⁻⁵ after the filters, not one ulp, because their poles near 1 carry the
last-bit difference along. Fixtures keep to two terms per sum (exact in either order); the theme
comparisons (step 6) need a bound from Chromium's own spread (several renders of the same theme).

### Connection counting (step 4)

Blink counts each node's connections (one per input connection, one while a source plays). After
the quantum in which a source finishes, it releases its count; a node left without any disables its
outputs, and so, down the line, does every node fed only by disabled outputs, except nodes that ring
(biquads, convolvers, compressors), which first finish their tail (the tail-processing list). A
disabled output stays connected but its input doesn't hear or count it: channel counts follow the
enabled connections only, so a stereo convolver whose stereo source has finished rings on in mono,
and a node whose last input is disabled zeroes its output (silent) a quantum earlier than one still
fed silence. A new connection into a disabled node enables it and those after it again. A convolver
starts disabled until something connects into it. Blink disables a node that finished its tail on
the main thread, some time later; the engine does it at once (the only part not at a fixed point).
Garbage collection in Chromium also disconnects nodes no script holds (`AudioNode::Dispose`), at no
fixed time; the engine has no such step (the game's nodes are let go explicitly).

### The convolver and the stereo panner (step 4)

- **`Convolver.cs`:** Blink's Reverb, ReverbConvolver, ReverbConvolverStage, FFTConvolver,
  DirectConvolver and the accumulation buffer: per response channel (at least two convolvers), a
  64-frame direct stage (vector_math::Conv, each output summed in kernel order) and FFT stages of
  128, 256, ... 32768 points (the offline structure, user decision), each staggered by a render
  phase and delayed to its place, overlap-added; the FFT frames are Blink's on the Rust FFT (64- to
  16384-point complex plans: Butterfly64Avx added). The response is normalized by its RMS power
  (Vsvesq's AVX summing order: Chromium's copy of the response sits at a 32-byte boundary, which the
  renders confirm), calibrated to −58 dB at 44.1 kHz with fdlibm's powf. The output is stereo unless
  input and response are both mono; the tail is the response's length. Responses of 4 channels
  aren't supported (the engine's buffers are mono or stereo).
- **`StereoPanner.cs`:** StereoPannerHandler and StereoPanner: the equal-power law with fdlibm's cos
  and sin in double, per sample while the pan moves, else once a quantum; mono inputs spread, stereo
  ones folded toward the panned side.

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
- **Step 2, oscillators and periodic waves: done (2026-10-31, 707 tests).** User decision on the
  way: the FFT is RustFFT's AVX path (Chromium 152's Web Audio FFT). 18 fixtures (the tables read out at three
  rates for every shape and a custom wave; each shape playing; custom waves with and without
  normalization; frequency swept; detune automated alone and with frequency; constant detune and a
  note starting mid-quantum during a sweep; LFOs at 2.5 and 0.4 Hz (3- and 5-point), one on a gain;
  frequency modulated by another oscillator, across zero; a negative frequency; near Nyquist, detuned
  past it; a shape change while playing; detuned above Nyquist and back; a steady note starting
  between frames after the first quantum; 22.05 and 96 kHz) match Chromium bit for bit. Unit tests:
  an oscillator nothing pulls finishing, errors, the forward real FFT against a direct DFT (the
  convolver's, compared with Chromium in step 4), exp2f and log2f against glibc. 36 mutations of
  the rules: 31 fail a test; the other 5 are exact identities (1/1024 and 2/2048, halving before or
  after rounding, a float or a double quotient of floats) or glibc's fused steps, which change no
  float result. Blink quirk worth knowing: an automated oscillator ignores its start's fraction of
  a frame (the k-rate frequency it would scale by is 0 there).
- **Step 3, biquad filters: done (2026-11-01, 733 tests).** 23 fixtures (each type on a noise burst ringing
  out until its tail passes; frequency, Q and detune automated; gain automated on a peak and a
  shelf; scheduled events constant within quanta, a constant detune and values changed while
  rendering, cutoff 0 and Nyquist, Q below 0; the edges of peaking, allpass and notch at Q 0 and
  the shelves at the ends; stereo, a channel count change and a type change; the input removed
  while ringing; the composer's master EQ on notes and an LFO on a cutoff; k-rate parameters;
  automation while silent; a decay into the denormals and a denormal input through a 120 dB peak,
  compared exactly) match Chromium bit for bit. 31 mutations: 24 fail a test; the rest change only
  doubles' last bits that vanish in the float result (the recursion's association, dividing by a0,
  the C library's sin), lengthen tails by a frame (below a quantum), or are optimizations (in-place
  pulling, coefficients once when constant).
- **Step 4, the convolver and the stereo panner: done (2026-11-01, 742 tests).** User decision on
  the way: the offline partitioning. Connection counting and disabled outputs ported (found when
  two fixtures failed: a stereo room rings on in mono once its stereo source has finished). 12
  fixtures (mono, stereo and long rooms up to and past 32768-point stages, stereo inputs through
  stereo and mono rooms, unnormalized, silence past the tail between two sounds; mono and stereo
  panning, constant and swept, at 0 and the ends; notes panned into a room; a node disabled and
  enabled again) match Chromium bit for bit. 23 mutations: 19 fail a test; the rest change no
  sample (the stages' render-phase stagger only moves when their FFTs run; a convolver starting
  disabled and disabling past the first node only change silent flags; disabling a node after its
  tail happens on Chromium's main thread at no fixed time).
- **Next: step 5, the dynamics compressor.**

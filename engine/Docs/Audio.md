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
- **The port adds sound effects (user decision 2026-11-03):** the finished port has proper sound
  effects. The old game has none (music only), so the port makes them as new work: it asks which
  sounds the game gets and makes them as effect patches. The first one asked for: a small sound
  when a rune is slotted (Docs/Engine-Plan.md, "Decisions for M8").
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
- **Step 6 (asked 2026-11-01, before its code):**
  - *Theme data:* records of the authored specs as `MusicThemes.all()` returns them (55 themes, the
    regional battle arrangements expanded, loudness trims applied); `compile()` and `lint()` ported
    and run at load, lint as a test.
  - *Instruments, percussion, rooms:* generated records too; an instrument's kind picks the C# code
    for its per-note graph; modes and chord qualities are constants beside the compiler.
  - *Theme checks:* committed fixtures for every instrument kind, percussion voice, texture, room,
    the master chain and fades; the 55 full themes in an opt-in test (`STATVENTURE_THEMES=1`)
    against four Chromium renders per theme kept in the build cache (first rule: each engine
    sample inside their per-sample min/max widened by their largest spread; changed, see below).
    The full renders are too big to commit (67 minutes, 1.5 GB of float32 per render, gzip 93 %);
    Chromium's own renders differ on almost every sample by up to 2e-4 (its summing order).
  - *exp and pow:* V8 15.2's `Math.exp` is LLVM libc's correctly rounded exp (Chromium 152's pin),
    ported line for line. `Math.pow` and `**` are `std::pow`, the system glibc 2.43 pow (its FMA
    variant on this CPU); re-asked once that was found: glibc's `e_pow.c` is ported as `__pow_fma`
    runs it. Both checked against V8's results.
  - *Theme API:* string ids (`"alder-vale"`, `"combat@alder-vale"`, boss ids), `Play(id)` with
    music.js's fades, `Stop()`; choosing the theme (`desiredTheme`) comes with the game port.
  - *Voice cleanup:* at the end of the render quantum in which a voice's first source ended; the
    fixtures make Chromium do the same by suspending its render every quantum.
  - *Fixture sums (asked during step 6):* fixtures whose notes sum three or more inputs at a node
    pass if Chromium's render lies inside the per-sample min/max of 16 engine renders with the node
    inputs summed in shuffled orders (a seeded test hook), widened by their largest spread (first 4;
    re-asked when 4 undersampled thunder's 5 summed bursts and the gong's 6 partials). The
    float-rounding bound doesn't survive the IIR filters after such sums (a choir note: Chromium's
    own renders 6.4e-7 apart).
  - *The full themes' rule (asked after the first run):* the engine renders each theme in 4
    shuffled summing orders too; at every sample its range and Chromium's 4-run range must come
    within the larger of the two spreads (the first rule, one engine render inside Chromium's range,
    failed 6 of 55 themes by summing order alone).
- **Step 7 (asked 2026-11-01, before its code):**
  - *Streaming:* an audio thread feeds an OpenAL streaming source queued buffers (4 × 5 ms).
  - *Output path untouched:* direct channels, HRTF off, OpenAL's limiter off, the device's rate.
  - *Volumes:* music = `Soundtrack.Volume` (before the limiter, as music.js); effects on the
    effects bus; master last on the mix; 40 ms ramps.
  - *Headroom:* none; past ±1 the system clips, as with Chromium.
  - *Device changes:* follow the default device, keep the graph's rate (OpenAL resamples).
  - *No device:* keep rendering in real time, silently.
  - *Minimized:* pause all audio, resume where it was (not the old game's restart).
  - *Threading:* game calls queued, applied before the next quantum; no locks while rendering.
  - *Theme starts (asked after measuring):* the 7 rooms' impulse responses and convolver kernels
    are prepared once by a worker when the audio system starts and shared (a theme start had cost
    12–23 ms on the audio thread, about the whole buffer).
- **Step 8 (asked 2026-11-02, before its code):**
  - *Patches:* layers of tones (a wave or harmonics, a frequency sweep) and noise bursts, an
    optional filter, each with its delay, attack, level and decay.
  - *Room:* dry, straight to the effects bus.
  - *Clip events:* a table per actor from event name to patch; `Effects.Play` for game code.
  - *Variation:* optional pitch and level ranges per patch, from a seeded mulberry32.
  - *Position:* pan and volume from the caller (set on the actor for its table).
  - *Voice limits:* per patch (4 by default) and 32 overall; past one, the oldest fades over 5 ms.

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
   the old game's themes through `Music.renderOffline`. Fixture renders are committed as references,
   so the tests run anywhere; the full themes' renders are too big and stay in the build cache
   (step 6's decisions).
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

### The dynamics compressor (step 5)

- **`DynamicsCompressor.cs`:** DynamicsCompressorHandler and DynamicsCompressor: five k-rate
  parameters; a 6 ms look-ahead; per block of 32 frames, the gain the detector wants (fdlibm's
  asinf), the adaptive release (a polynomial over four zones, its constants folded in float as
  Blink's constexpr floats are) or the attack (fdlibm's powf), then per frame the detector, the gain
  warped by sin, the soft knee (an exponential whose curvature is found in 15 bisection steps) and
  the ratio's slope above it, the makeup gain (powf(1/saturate(1), 0.6)), the reduction metered.
  The decibel conversions are Blink's audio_utilities, on the C library's powf and log10f: glibc
  2.43's powf (FMA variant; on x = 10 the unfused one gives the same floats) and log10f
  (CORE-MATH, correctly rounded), ported into `Libm.cs` and checked on all 2³² floats. The knee's
  `exp` and the warp's `sin` are the C library's in double, rounded to float: fdlibm's give the
  same float for every float argument (checked natively), so the port uses those. Also fdlibm's
  asin, checked by the native reference. The tail (5 × 0.325 s) and latency (the look-ahead)
  decide when a silent compressor stops; its state then freezes until the next sound.

### The game synthesizer (step 6)

`Engine/Statventure/Audio/Music/` and three math files; the game sees `Soundtrack`, as `AudioSystem.Music` (step 7).

- **JavaScript's numbers (`JsMath.cs`, `LlvmLibc.cs`, `Libm.Pow.cs`):** the composer's arithmetic is
  JavaScript doubles, so its `Math.exp`, `**`/`Math.pow` and `Math.round` are V8 15.2's.
  `Math.exp` is LLVM libc's correctly rounded exp (`src/__support/math/exp.h` at 3ea89f43, the
  revision Chromium 152 pins; V8's `base::ieee754::exp` calls it), ported with its double pass,
  double-double pass and 128-bit dyadic floats (`DyadicFloat128`, with the multiword
  `quick_mul_hi` that drops the low words' product), built as Chromium builds it for x86-64: no
  FMA, so `multiply_add` is a product and a sum and exact products are Dekker's. `**` and
  `Math.pow` are V8's `math::pow` (`src/numbers/ieee754.cc`: NaN exponents, ±∞ exponents of ±1,
  `x²` and `√(x + 0)` special cases), then `std::pow` (the flag `use_std_math_pow` is on), which in
  Electron on Linux is the system glibc's pow: glibc 2.43's `e_pow.c` as x86-64's FMA variant
  `__ieee754_pow_fma` computes it. Which multiply-adds GCC fused was read from the system
  libm.so.6's machine code (the ifunc resolver picks it on CPUs with FMA and AVX2): every
  single-use `a·b + c`, except in the subnormal branch of the special case, where `scale · tmp`
  has a second use in another block and stays a rounded product. `Math.round` is V8's: the ceiling,
  less one when that is more than half away.
- **The data (`ThemeData.cs`, `MusicLibrary.cs`; since M9 step 6 in `audio.pak`):** records for
  themes, sections, layers, harmonies, doublings, instruments, percussion voices and rooms, each
  property named after the JavaScript key it holds (`t` → `Type`, `dbl` → `Doublings`, a drums
  layer's `p` → `Drums`, in the spec's order, which decides the order of random draws). They are
  read from `engine/Assets/Packs/audio.pak` through a `MusicLibrary` (Docs/Assets.md, step 6), which
  the audio system, the soundtrack and the composer are given. The pack is made by
  `Tools/Scripts/make-music-data.sh`, the music importer: the harness's Chromium loads
  music-composer.js and music-themes.js, and `Tools/AudioReference/music.cjs` writes
  `MusicThemes.all()` (55 themes: 17 regions, the battle theme, its 17 regional arrangements already
  expanded by `arrange`, 20 boss themes, the loudness trims applied) and the composer's 34
  instruments, 21 percussion voices and 7 rooms as their records' JSON forms, plus the theme order,
  then the packer packs them; an unknown key stops it, so nothing is dropped. (Until M9 step 6 the
  script wrote them as C#, `MusicData.g.cs`.)
- **The score (`Score.cs`):** `compile` and `lint` ported: scale degrees, chord numerals with their
  qualities and sevenths, bars (an empty last one dropped), chord lines (held "." chords lengthen
  the entry before; `%` repeats a bar), melodies with ties, rests and accents, sections with `like`
  (one level, key by key), the form and loop point. JavaScript's semantics kept where they show:
  numbers are doubles, `Math.round` is V8's, a held chord shares its chord object (entries merge by
  identity), note steps may be fractional (and then never play), notes by step keep insertion
  order. All themes compile when `Soundtrack` is first used.
- **The composer (`GraphBuilder.cs`, `Composer.cs`):** `Performance` is music-composer.js's
  `perform`: the theme's EQ (34 Hz highpass, 140 Hz shelf, 2.6 kHz presence), dry gain, room
  (`impulse`: smoothed seeded noise decaying over the room's length, darkening, with seven early
  reflections) behind a send, a channel per instrument (and per percussion voice and texture) with
  its send level; the per-note graphs of each instrument kind (bowed and brass voices into a
  lowpass, with a resonance, tremolo and vibrato; a choir's formant bank; organ and wind periodic
  waves, the flute's breath noise and chiff; Karplus-Strong plucks (`pluckBuffer`, cached per
  instrument and note, cleared past 220) at a playback rate through body and highpass filters;
  mallet partials below the clamped Nyquist; glass with tremolo; a sine sub), the percussion
  (pitch sweeps, noise bursts, metal partials, swells, thunder), the textures, and the sequencer
  (`step`: the form looping after its end, swing, dynamics across a section, bar filters, voice-led
  pads, drones, arpeggio/bass/pulse patterns with held steps, strums, melodies and lines with
  doublings and harmonies, drums with rolls and flams, hits tuned to degrees or chords, drips).
  Every random draw (mulberry32 seeded with the theme id's FNV hash), double computation and Web
  Audio call is in the script's order, through `GraphBuilder`: one call each for node creation,
  `param.value =`, the automation calls, connections, starts and stops, types, buffers, loops,
  periodic waves and `onended` cleanups. The builder also holds the per-context kit (two seconds of
  noise, periodic waves by harmonics, plucks, rooms), as the composer's `WeakMap` per context did.
  A cleanup disconnects a finished voice's nodes at the end of the quantum in which its first
  source ended (step 6's decision).
- **The soundtrack (`Soundtrack.cs`):** music.js for the composer's themes: the master gain (0.58 ×
  the volume; 1 for exports) into the limiter (−18 dB, knee 18, ratio 3, attack 6 ms, release
  220 ms); `Play(id)` (sync: the old bus held and ramped to 0 over 0.55 s and disconnected 1.8 s
  later, the new performance's bus from 0 to 1 over 0.8 s, its first step 60 ms ahead; the theme
  playing does nothing); `Stop()`; `Volume` (a 40 ms ramp); `Schedule()`, to be called before each
  render quantum (by `AudioSystem`, step 7): due timers at the first quantum boundary at or after their
  time, then every step starting within 0.2 s (music.js's 45 ms timer schedules the same notes; a
  note's nodes being made earlier or later changes nothing heard). music.js's old score engine's
  delay and hall are left out: no theme feeds them (the master fixtures include them in Chromium
  and match). `RenderOffline(id)` is music.js's `renderOffline`.
- **How it is checked:** the math against the system's pow (`Reference/libm.json`, native) and
  against V8 run by Electron as Node (`Reference/v8-math.json`, `make-v8-math-reference.sh`): five
  argument families of 4 Mi each, drawn from mulberry32 the same way in C, JavaScript and C#
  (random doubles, near 1 with large exponents, negative bases, specials, the composer's own
  arguments; for exp also every room response's arguments at 44.1 and 48 kHz), plus exp's slow
  passes on their own. The score and composer against the old composer in Chromium
  (`Reference/music-scores.json`, `music-logs.json`, `make-music-references.sh`): every theme's
  compiled score as a digest, lint's findings (none in the themes; small specs with clashes in
  `Reference/lint-specs.json`), and the whole performance of every theme (renderOffline's steps
  plus 64 past the loop point; at 44.1 kHz, six themes also at 48 kHz) on a recording stand-in
  context, the same lines as `GraphBuilder`'s log, hashed per step: 2.7 million calls, all equal.
  Rendered fixtures (`MusicFixtures.cs`, quanta fixtures: Chromium suspends every quantum so its
  cleanups land where the engine's do; `at(time, fn)` and `quantum(fn)` hooks): every instrument
  kind, every percussion voice, the textures, an ensemble looping past its end, strums and lines, a
  long room at 44.1 kHz, the master chain and switching with fades and a volume change. Those
  without sums of three or more match exactly; the others pass the shuffled-sums rule
  (`AudioGraph.SummingOrder`, a test-only seeded key per node output: inputs sum in key order, as
  Chromium's pointer hash sets order them; 16 orders). The full themes: `test-themes.sh` (Release,
  about 20 minutes) against the build cache's references from `make-theme-references.sh` (four
  renders through music.js's own renderOffline with a suspend every quantum, 2.6 GB): the engine
  renders each theme in four shuffled orders, and at every sample the two sides' ranges must come
  within the larger spread.

### The output (step 7)

`Engine/Statventure/Audio/AudioSystem.cs`, `Audio/Output/AudioOutput.cs`; `Application.Audio`.

- **`AudioSystem` (public):** an audio graph at the output's rate (the device's own, read from
  OpenAL's `ALC_FREQUENCY`; 48 kHz without a device), a master bus (`MasterVolume`, the last gain on
  the mix) with the effects bus (`EffectsVolume`) and the music (`Music`: the `Soundtrack`, music.js's
  master gain and limiter, into the master bus) on it. Every volume ramps as music.js's setVolume
  does (hold the current value, 40 ms linear). The game's calls (`Music.Play`, `Stop`, the volumes)
  are queued and applied by the audio thread before its next quantum, then the soundtrack schedules
  its steps, then the quantum renders; nothing locks while rendering. The audio thread renders
  whole buffers whenever the output can take one; a rendering error is logged once and silence
  follows. `Pause`/`Resume`; `Application` pauses all audio while its window is minimized (step 7's
  decision) and creates the system unless `ApplicationOptions.Audio` is off.
- **`OpenAlOutput`:** OpenAL Soft 1.25.2 (the BRUTAL package's natives): the default device, a
  context with HRTF and OpenAL's output limiter off, a streaming source with direct channels (left
  to left, right to right; on more speakers the others stay silent), 4 queued buffers of whole
  quanta of about 5 ms (256 frames at 44.1 and 48 kHz, 512 at 96 kHz), stereo float32 at the graph's
  rate. OpenAL's buffer-completed event wakes the thread. Its default-device event, or a lost
  connection, reopens the device on the new default, the source carrying on with its queue; the
  graph keeps its rate and OpenAL resamples if the new device's differs. Without a device the buffers are taken at the real-time pace (20 ms
  ahead of a clock) and dropped, and a device is looked for every second. Pausing pauses the
  source: OpenAL Soft declicks, fading the next 64 frames out, and on resuming plays again from the
  paused place with a 64-frame fade-in.
- **Rooms prepared ahead (step 7's decision):** a theme start built its room on the audio thread
  (12–23 ms in one quantum, about the whole buffer). Now a worker thread makes the 7 rooms'
  responses and convolver kernels when the system starts (`PreparedRoom`, `Reverb.Prepared`: each
  response channel's stage kernels, which never change), and every convolver of that room shares
  them, keeping only its own running state; then a second of three themes is rendered into a
  throwaway graph so the JIT has compiled the composer before the first theme change (a cold first
  start had cost about 60 ms). A theme start now costs about 3–7 ms on the audio thread.
- **No garbage on the audio thread's big FFTs:** the FFT's scratch arrays (2 × length floats, 128 KB
  for the 32768-point convolver stage, so large-object-heap ones) come from the shared array pool;
  every one is written before it is read, so no value changes. Full collections during a minute of
  music fell from about 90 to 3.
- **Measured** (`Tools/Scripts/audio-benchmark.sh`, Release, 48 kHz, every theme played live with
  its steps scheduled 0.2 s ahead): 71 minutes of music at a mean of 0.23 ms per quantum of the
  2.67 ms budget (under 9 %), 99th percentile 1.32 ms, 99.9th 2.08 ms, worst 12.9 ms, 478 of 1.6
  million quanta over budget (before the two changes above: 99th percentile 2.2 ms, worst 35–60 ms,
  9927 over). The big convolver stages take up to about 5 ms in one quantum; the worst cases are
  theme starts, within the 21 ms queued. On this PC's headset through PipeWire (`--live`, silent
  master): no underruns over repeated 12–30 s runs with theme switches every 4 s (one, its time not
  logged, in an earlier run before the rooms' worker became a single low-priority thread).
- **How it is checked:** a loopback device (OpenAL mixing into memory, no hardware) gets random
  samples peaking past ±1 and gives them back bit for bit at 48 and 44.1 kHz (so no panning, HRTF,
  limiter or resampling), and pauses and resumes as described; the audio thread's buffers equal the
  same calls rendered directly; a call lands at the next quantum; pausing freezes the graph; without
  a device the frames taken stay a fixed amount ahead of a clock; the default device moves the graph
  and paces it (never far ahead of the clock; under load it may fall behind, so how close it stays
  is measured, not asserted); rooms shared
  sound as rooms made on the spot (two themes in the hall cross-fading); and the `output-buses`
  fixture matches Chromium: music.js's chain into a master bus, a tone on an effects bus, the
  master, effects and music volumes ramped while the music plays. Also OpenAL's reported HRTF
  status (direct channels bypass HRTF, so the sound alone can't show it), left and right in the
  buffers, the buffer sizes, and the reopening path on the same device.
- **Listening:** `Tools/Scripts/play-theme.sh [theme] [--volume 0-100]` (`Tools/PlayTheme`, on the public API
  only, the music from the repo's `audio.pak`) lists the themes or plays one, looping, through the default device; while it plays, type another
  theme to change to it with the game's fades, or a volume; Enter alone quits. It needs a real
  terminal: with no input it quits at once. A theme started right after launch logs one underrun
  (the JIT on first use, while the warm-up still runs), before the first note and so not heard; a
  theme started later (1.5 s in, in the `--live` runs) doesn't.
- **Heard, but not run on Windows:** the user listened with `play-theme.sh` on 2026-11-02: all fine. The noise
  layers many themes open with (`tex`, `swell`, from the old game's theme data) sound a little like a wave; a minor
  nitpick the user chose to keep, so they stay. None of the audio has run on Windows (the package
  ships OpenAL Soft for it; all the math is ported so results should match Linux's).
- **Not checked automatically:** moving to a really different device or rate (it would switch this
  PC's default output; OpenAL resamples since buffers keep the graph's rate), a device lost or
  appearing (the code paths exist, no test machine loses one), and `Application` pausing on a real
  minimize (needs a window manager; the pause itself is tested). Since 2026-11-03 a minimized
  window is also told by GLFW's iconify (X11, Windows; the framebuffer's zero size only told it on
  Windows); a real minimize under Xvfb with openbox now goes through the loop (Docs/Scripting.md,
  "Hidden documents"); Wayland never reports one, so there the sound doesn't pause.

### Audibility (2026-11-03)

Whether the sound is audible, as Chromium 152's audio service decides it for a page's output
stream, so the window's document knows (Blink doesn't throttle a hidden page that is audible or
was in the last 30 s: Docs/Scripting.md, "Hidden documents"; the user's decision to port it by
the output's level). `Audio/Output/Audibility.cs`, ported from `media/base/audio_power_monitor.cc`,
`services/audio/output_stream.cc` (AudibilityHelperImpl) and `output_controller.cc`:
- The power monitor scans what goes to the device: per channel an exponentially weighted average
  of the squared samples from the shared average (sample weight 1 − exp(−1 / (rate · 10 ms))), the
  channels' results averaged and clamped to 0..1; a non-finite channel keeps the old average. The
  reading in dBFS is 10 · log10 of it, −∞ below 1e-10.
- Polled 15 times a second on the audio thread's clock: audible at once at −72.24719896 dBFS or
  more; silent only after 100 ms of polls below (the glitch tolerance). A stream that starts
  (`Start`) is silent and isn't "recently audible" (the forced silence start); one that pauses
  (`Stop`) is silent at once and its power monitor starts over.
- `AudioSystem.Audible` is the last poll's answer; the audio thread scans each buffer it writes,
  polls between buffers, stops polling when paused and starts again when resumed. `UiHost.Audio`
  passes it to the document each update.
- Tests (`Audio/AudibilityTests.cs`): the time constant, the channel average, the threshold on both
  sides, the glitch tolerance, starting and stopping, and the audio system audible while playing a
  theme and silent once paused (an output taking buffers at the real-time pace).

### Sound effects (step 8)

`Engine/Statventure/Audio/Effects/`; the game sees `AudioSystem.Effects` and `Actor.Sounds`.

- **Patches (`EffectPatch.cs`):** records: a patch has layers, a level, a voice limit (4 by default)
  and optional pitch and level ranges; a layer is a tone (sine, square, sawtooth, triangle, or
  harmonics as sine terms) with an optional exponential frequency sweep, or looping noise, through
  an optional filter (any of Blink's types, its frequency also sweeping, with Q and gain), with a
  delay, attack, level and length. The envelope is the composer's percussion one: from 0.0001 linearly
  to the level over the attack, then a target of 0 with a time constant of a quarter of the
  length; the layer stops 50 ms after its length. Sweeps take 0.6 of the length by default, as the
  composer's. Frequencies are scaled by the play's pitch (noise itself isn't) and clamped to 30 Hz …
  Nyquist − 200 Hz, as the composer clamps them. M8 made patches as records in code; since M9 step 6
  they are also data (`audio/effects/<name>` in `audio.pak`, the record `audio.effect`), which the
  audio system registers under their names when it starts (Docs/Assets.md, S6-4); M11 edits them.
  Which sounds the game gets is decided at port time.
- **`SoundEffects`:** `Register(id, patch)` (on the game's thread; a harmonics layer's periodic wave is
  made there, not on the audio thread) and `Play(id, volume, pan, pitch)`, queued and applied before
  the next quantum like the music's calls. A play draws the patch's pitch, then its level, then
  each noise layer's start offset (0–1.5 s into the composer's two seconds of noise) from a
  mulberry32 (`Seed` starts it anew, for replays), and builds a voice: layers into a gain into a
  stereo panner into the effects bus, dry. Past the patch's limit, or 32 voices overall, the
  oldest voice (of that patch, or overall) holds its gain, ramps it to 0 over 5 ms and stops its sources,
  no longer counting. The layer that stops last cleans the voice up, its nodes disconnected at the
  end of that quantum, as the composer's notes are.
- **`ActorSounds` (`Actor.Sounds`):** a table from clip event name to effect, with a pan and volume
  the game sets; the actor plays the effect when its animator delivers the event (the markers
  exported from Blender, on the game's thread); events not in the table play nothing.
- **How it is checked:** 12 fixtures against Chromium, with the same graph stated in JavaScript from
  the same patch JSON (the old game has no effects to take it from, so what they show is that
  the engine's nodes render the graph as Chromium's do, including the defaults, written separately
  on each side): tones and sweeps, the other waves, harmonics through a sweeping filter, noise through
  four filter types, a ringing filter cut by the cleanup, noise looping past its end, clamping, the
  caller's volume, pan and pitch, per-play ranges, a patch's limit and the overall one. Unit tests:
  refused calls, voices released, the limits, a stolen voice's sources stopping, repeatable seeds,
  no range meaning no variation, effects through their bus (its volume and the master's scale them,
  the music's doesn't), and an actor's table on the figure's real markers, sample for sample as
  a direct play. Measured (`audio-benchmark.sh --effects`, Release, 48 kHz): 32 two-layer voices with
  a new one every 21 ms add 0.13 ms to a quantum's mean of 0.26 ms, on a battle theme.

### The clock and scheduled calls (M10 step 6, 2026-11-04)

For the sequencer's audio cues (Docs/Scene.md, M10-8, Q27, S6-B, S6-C); the game may use them too.
`Audio/AudioSystem.cs` (`AudioSystem.Time`, `ScheduledSound`, `AudioSchedule`), `Audio/Effects/SoundEffects.cs`,
`Audio/Music/Soundtrack.cs`.

- **The clock:** `AudioSystem.Time` is seconds on the audio clock: the start of the next quantum the audio
  thread renders, the earliest time a call made now takes effect at (as Web Audio's `currentTime`). The
  audio thread publishes it after taking the calls before each quantum; it stands still while the sound
  is paused (a minimized window). It is ahead of what is heard by what is queued (about 20 ms) and the
  device's latency, which nothing corrects (Q27).
- **Calls at a time:** `Effects.PlayAt(time, id, volume, pan, pitch)`, `Music.PlayAt(time, themeId)`,
  `Music.StopAt(time)`, each queued like every call and giving a `ScheduledSound` (`Time`, `State`:
  pending, done or cancelled as the audio thread last left it, `Cancel()`, itself queued: it drops the call
  if its time hasn't come when the audio thread takes it). The checks are the immediate calls' (an unknown
  effect or theme, numbers that aren't finite), plus a finite time. Each player (the effects, the music)
  keeps its calls in time order, equal times in the order they came; before each quantum, after the queued
  calls, those whose time is before the next quantum's start (as the graph's clock reads it: a time on a
  quantum's start falls in that quantum) are applied, each at its time, a time already past at the
  quantum's start.
- **An effect at a time** is the play `Play` makes, made in the quantum its time falls in and started at
  its time (S6-C): the limits are counted, the oldest voice stolen (its fade from that time) and the
  pitch, level and noise offsets drawn then, in the order the voices start; until then it counts toward
  nothing. `Play` is the same at the quantum's start.
- **The music at a time** is music.js's switch with "now" its time, in the quantum it falls in: the old bus
  held at that time and ramped to 0 over 0.55 s and let go at the first quantum 1.8 s after, the new
  performance's bus from 0 at it to 1 0.8 s later, its first step 60 ms after it, the steps then scheduled
  0.2 s ahead as always; the theme playing then does nothing; a stop retires the bus at its time. Once
  applied, a switch or stop at a time is what the game asked for as far as a reload is concerned (S9-D:
  a theme switched to restarts when its music changed, one stopped stays stopped); a switch keeps the
  compiled theme of the library it was made with, so a reload before its time doesn't change it.
- **A sequence's calls** (Q27) don't go through these: its playback posts one call to the audio thread
  that anchors every cue to that quantum's start and schedules them through the same schedules, and
  makes its later decisions (which cues happened, which to send again, the end's music cue at a skip) on
  the audio thread (Docs/Scene.md, "How it works", step 6).
- **How it is checked** (`Audio/ScheduledSoundTests.cs`, offline on the audio system's mixer): the clock
  after each quantum and standing still while the audio thread is paused; an effect at times inside quanta
  and on a quantum's start renders sample for sample as the voice's graph built by hand at that time (the
  effect fixtures' graph written again in C#), two at one time in the order asked; at a quantum's start as
  the play made for that quantum; a past time as a play now; the voice made in its quantum (counts before
  and after, the third of a two-voice patch stealing the first at its time, draws in the order of the
  starts, the overall limit's stealing); a cancelled play leaving the rest sample for sample (its draws not
  made) and its states; a switch and a stop inside quanta while a theme fades in, as music.js's switch
  written out by hand at those times; a switch at a quantum's start as the switch made for it, one to the
  theme playing doing nothing, one cancelled; a reload after a switch at a time, a switch now (a skip) and
  a stop at a time; through the audio system with the game's themes.
- **Found:** Blink's `cancelAndHoldAtTime` at the current time during a linear ramp whose first event is
  past prunes that event (`InsertEvent`'s removal of old events) and holds a wrong value, so music.js's
  switch made at a quantum's start while the old theme is still in its 0.8 s fade-in cuts the old theme
  within a quantum instead of fading it out; a switch at a time inside a quantum fades it. Ported line for
  line in step 1, the M8 fixtures switch only after a fade-in finished, so this isn't checked against
  Chromium; the engine's path is unchanged.

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
- **Step 5, the dynamics compressor: done (2026-11-01, 750 tests).** 5 fixtures (the defaults on
  loud noise then a soft tone; music.js's master chain on chords; k-rate parameters ramped with a
  hard knee, the hardest ratio, no attack and the longest release on a mono input; the tail at a
  low rate; silence past the tail and look-ahead freezing a slow release, then a second burst)
  match Chromium bit for bit, but the master chain, whose three chords sum in Chromium's changing
  order (an ulp apart). 18 mutations: 15 fail a test; the other 3 swap fdlibm's and glibc's
  versions of a function where both are near correctly rounded and agree on every input the
  renders reach (each function is checked bit for bit on its own), or fold a release constant in
  double (an ulp that vanishes in the float rates).
- **Step 6, the game synthesizer: done (2026-11-01, 974 tests).** User decisions on the way
  (Decisions, "Step 6"): theme data as the authored specs, instruments as records, fixtures plus
  opt-in full themes, the JavaScript math ported (exp from LLVM libc; pow re-asked once V8 turned
  out to call glibc's), string theme ids, cleanups at the quantum's end, the shuffled-sums rule for
  fixtures (16 orders, re-asked when 4 undersampled). The math: glibc's pow, V8's `**` and exp equal
  their references on 5 × 4 Mi arguments each and every room response's arguments. The composer:
  all 55 themes compile to the old composer's scores (digest, lint), three small specs cover the
  chord forms and clashes the themes don't have, and all 61 performances (55 at 44.1 kHz, 6 at 48
  kHz, 64 steps past the loop) make the old composer's 2.7 million Web Audio calls with the same
  bits, step by step. 30 synthesizer fixtures match Chromium: 12 exactly, 18 by the shuffled-sums
  rule (also against each of 4 separate Chromium runs). All 55 full themes pass the opt-in check (`test-themes.sh`, 21.5 minutes; Chromium's own 4-run
  spread per theme 2.3e-5 to 5.6e-4); the first rule (one engine render inside Chromium's range)
  had failed 6 of them by summing order alone. 47 mutations: 39 fail a
  test; the other 8 change nothing a test or a listener can see: five equivalents (a tiny exponent
  gives 1.0 either way, a split constant of 2²⁷ splits exactly too, lint's clash distance can't be
  0, pads re-centre after the first placement, a bus already faded to 0 can be disconnected
  early), one only slows exp (the subnormal rounding test always passes on to the 128-bit pass),
  and two below observable precision (pow's fused reduction differs only within 2⁻³⁵ of a
  half-integer; the low bits of the double-double pass's 1/720).
- **Found on the way:** V8 15.2's `math::pow` calls `std::pow` (the flag `use_std_math_pow` is on
  outside AIX), so `**` is the system C library's pow, not LLVM libc's (which isn't correctly
  rounded at this revision either); music.js's delay and hall get no input from the composer's
  themes and change no sample; the engine's Debug build renders a theme about 10 times slower than
  Release (Release: about 8 times faster than real time offline, with every note scheduled up front).
- **Step 7, the output: done (2026-11-02, 990 tests).** User decisions on the way (Decisions,
  "Step 7"): queued buffers, an untouched output path, music.js's volume with effects and master
  buses, no limiter on the mix, following the default device, silence without one, pausing when
  minimized, calls applied at a quantum, rooms prepared at start. Measuring also led to the FFT's
  scratch arrays coming from a pool (no value changes). All 55 full themes still pass the opt-in check (20 minutes), run again since the FFT and
  convolver changed underneath them. 19 mutations: 17 fail a test;
  the other 2 are the order of queued calls and scheduling within a quantum (music.js has no
  defined order either, and it only shows if a step's turn falls in that very quantum) and not
  sharing the kernels (speed only).
- **Found on the way:** OpenAL Soft passes direct-channel float samples through bit for bit, peaks past
  ±1 included; it declicks a source's pause and resume over 64 frames; direct channels bypass HRTF;
  this PC's default sink is a USB headset at 48 kHz; .NET's raised thread priorities may have no
  effect on Linux without privileges, so the workers are kept few and low rather than relying on
  the audio thread's priority; test scratch files are lost when a session restarts (the mutation
  runner now lives in `~/.cache/stat-fick-build/claude-work/audio/`).
- **Step 8, sound effects: done (2026-11-02, 1013 tests).** User decisions (Decisions, "Step 8"): layers
  of tones and noise, dry, a table per actor, optional seeded ranges, pan and volume from the
  caller, limits per patch and overall. 12 fixtures match Chromium, 10 exactly and the 2 limit ones
  by the shuffled-sums rule, also against 4 separate Chromium runs. 32 mutations all fail a test (three
  survived at first and showed real gaps: the test parser supplied its own defaults, nothing played
  the default limit, nothing saw a stolen voice's sources keep running). The opt-in theme check wasn't
  run again: the synthesizer's renders didn't change since step 7's run.
- **Found on the way:** a timing test failed once in three whole-suite runs on lavapipe. It assumed the
  graph stays close to the wall clock, but the device's consumption paces it, so under load it
  legitimately falls behind; the pace tests now assert what always holds (7 suite runs in a row
  pass). Rewinding the source on a reopen only dropped queued audio: the source carries on, so
  that's gone. Fixture parsers must leave a missing key to the record's default, or defaults go untested.
- **Audibility (2026-11-03):** added for hidden documents (above, "Audibility").
- **The music in `audio.pak` (2026-11-03, M9 step 6; Docs/Assets.md, step 6):** `MusicData.g.cs` is gone; the
  themes, instruments, percussion voices, rooms and effect patches are records in `engine/Assets/Packs/audio.pak`,
  read through a `MusicLibrary` that `AudioSystem`, `Soundtrack`, the composer, `PlayTheme` and `AudioBenchmark`
  are given (`Soundtrack.Themes` is now the instance's `Audio.Music.Themes`). Before the file went, every
  pack-loaded record was compared with it, field for field and bit for bit. Every music test passes unchanged on
  the pack-loaded data: the 61 performances' exact call logs, the scores and lint, the 30 synthesizer fixtures, the
  effect fixtures (whose patches are now read as the records' JSON form); the opt-in full themes too
  (`test-themes.sh`, 55 of 55, 21 minutes); a theme start costs the same as before (`audio-benchmark.sh --start`).
- **M8 is done.** All 8 steps, and the user has listened and is happy with it (2026-11-02). Next: M9 only on the user's go, asking its open questions first
  (`Docs/Engine-Plan.md`, "Open questions for later milestones").
- **The clock and scheduled calls (2026-11-04, M10 step 6):** added for the sequencer's audio cues (above,
  "The clock and scheduled calls"; Docs/Scene.md, S6-B, S6-C). The synthesizer's renders didn't change (the
  immediate calls are applied as before; only calls at a time are new), so the opt-in theme check wasn't
  run again.
- **Open (found 2026-11-04, M10 step 6):** music.js's switch made exactly at a quantum's start while the old theme
  is still in its 0.8 s fade-in cuts the old theme within a quantum instead of fading it (Blink's
  `cancelAndHoldAtTime` rule as ported line for line in step 1; "The clock and scheduled calls", "Found"). Not
  checked against Chromium: to check against a Chromium render whether music.js does the same, and if not, find
  the rule (Docs/Engine-Plan.md, "Small tasks left from earlier milestones").

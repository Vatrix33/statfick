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

## Status

Started 2026-10-31 (decisions asked and recorded; nothing built yet).

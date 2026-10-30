/* The remastered soundtrack engine. Themes are written as compact scores
   (scale degrees, chord numerals and step patterns, see music-themes.js) and
   performed live with Web Audio: ensemble strings, brass, formant choirs,
   plucked strings modelled by Karplus-Strong, mallets, bells, a percussion
   section and a convolution room per theme. Boss themes still use the older
   engine in music.js. */
(() => {
  "use strict";

  /* ---------------- Scores ---------------- */
  const MODES = {
    ionian: [0, 2, 4, 5, 7, 9, 11], dorian: [0, 2, 3, 5, 7, 9, 10], phrygian: [0, 1, 3, 5, 7, 8, 10],
    lydian: [0, 2, 4, 6, 7, 9, 11], mixolydian: [0, 2, 4, 5, 7, 9, 10], aeolian: [0, 2, 3, 5, 7, 8, 10],
    locrian: [0, 1, 3, 5, 6, 8, 10], harmonic: [0, 2, 3, 5, 7, 8, 11], hijaz: [0, 1, 4, 5, 7, 8, 10],
    hungarian: [0, 2, 3, 6, 7, 8, 11],
  };
  const QUALITIES = { M: [0, 4, 7], m: [0, 3, 7], dim: [0, 3, 6], aug: [0, 4, 8], sus2: [0, 2, 7], sus4: [0, 5, 7], 5: [0, 7, 12] };

  // A scale degree such as "5", "b3'" or "#7," becomes semitones above the tonic.
  function degree(token, scale) {
    const match = /^([b#]?)([1-7])([',]*)$/.exec(token);
    if (!match) throw new Error("Bad degree: " + token);
    const accidental = match[1] === "b" ? -1 : match[1] === "#" ? 1 : 0;
    const octave = [...match[3]].reduce((sum, mark) => sum + (mark === "'" ? 12 : -12), 0);
    return scale[Number(match[2]) - 1] + accidental + octave;
  }
  // Chord numerals take their quality from the mode unless a suffix names it: "1", "b6", "5M", "4m7", "1sus4".
  function chord(token, scale) {
    const match = /^([b#]?)([1-7])(M|m|dim|aug|sus2|sus4|5)?(7)?$/.exec(token);
    if (!match) throw new Error("Bad chord: " + token);
    const index = Number(match[2]) - 1, accidental = match[1] === "b" ? -1 : match[1] === "#" ? 1 : 0;
    const root = scale[index] + accidental;
    const stack = steps => scale[(index + steps) % 7] + (index + steps >= 7 ? 12 : 0) - scale[index];
    let tones = match[3] ? [...QUALITIES[match[3]]] : accidental ? [0, 4, 7] : [0, stack(2), stack(4)];
    // A seventh follows the mode; a named quality takes the usual one (dominant, minor or diminished).
    if (match[4]) tones = [...tones.slice(0, 3), match[3] === "dim" ? 9 : match[3] || accidental ? 10 : stack(6)];
    return { name: token, root, tones };
  }
  function bars(text) { return text.split("|").map(bar => bar.trim()).filter((bar, index, all) => bar || index < all.length - 1); }

  function compileChords(text, scale, steps) {
    const timeline = [];
    let previous = null, previousBar = null;
    bars(text).forEach((bar, barIndex) => {
      const tokens = bar === "%" ? previousBar : bar.split(/\s+/);
      previousBar = tokens;
      tokens.forEach((token, index) => {
        const current = token === "." ? previous : chord(token, scale);
        const start = barIndex * steps + Math.round(index * steps / tokens.length);
        const end = barIndex * steps + Math.round((index + 1) * steps / tokens.length);
        if (timeline.length && timeline.at(-1).chord === current && token === ".") timeline.at(-1).length += end - start;
        else timeline.push({ start, length: end - start, chord: current });
        previous = current;
      });
    });
    return timeline;
  }
  // Melodies: "5/2 6/1 b7/1 | 1'/4" with durations in grid steps, "_" rests and "-" ties. A "!" marks an accent.
  function compileLine(text, scale, steps, label) {
    const events = [];
    bars(text).forEach((bar, barIndex) => {
      let position = 0;
      for (const token of bar.split(/\s+/).filter(Boolean)) {
        const match = /^([^/]+)\/(\d+(?:\.\d+)?)(!?)$/.exec(token);
        if (!match) throw new Error(`Bad note "${token}" in ${label}`);
        const length = Number(match[2]);
        const at = barIndex * steps + position;
        if (match[1] === "-") { if (events.length) events.at(-1).length += length; }
        else if (match[1] !== "_") events.push({ at, length, pitch: degree(match[1], scale), accent: Boolean(match[3]) });
        position += length;
      }
      if (Math.abs(position - steps) > 1e-6) throw new Error(`${label}: bar ${barIndex + 1} has ${position} steps, expected ${steps}`);
    });
    return events;
  }

  // Resolves sections (with "like" inheritance), the form and the loop point.
  function compile(spec) {
    const scale = MODES[spec.mode];
    if (!scale) throw new Error("Unknown mode " + spec.mode);
    const [beats, sub] = spec.meter;
    const steps = beats * sub;
    const sections = {};
    const resolve = name => {
      if (sections[name]) return sections[name];
      const raw = spec.sections[name];
      if (!raw) throw new Error(`${spec.id}: missing section ${name}`);
      const base = raw.like ? spec.sections[raw.like] : null;
      const merged = { ...base, ...raw, layers: raw.layers || base?.layers || [] };
      const chords = compileChords(merged.chords, scale, steps);
      const barCount = chords.length ? Math.round((chords.at(-1).start + chords.at(-1).length) / steps) : 0;
      const total = barCount * steps;
      const chordAt = new Array(total);
      for (const entry of chords) for (let i = entry.start; i < entry.start + entry.length; i++) chordAt[i] = entry;
      const melody = merged.mel ? compileLine(merged.mel, scale, steps, `${spec.id}/${name}/mel`) : [];
      const layers = merged.layers.map((layer, index) => {
        const compiled = { ...layer };
        if (layer.notes) compiled.events = compileLine(layer.notes, scale, steps, `${spec.id}/${name}/layer${index}`);
        if (layer.t === "mel") compiled.events = melody;
        compiled.byStep = new Map();
        for (const event of compiled.events || []) {
          if (!compiled.byStep.has(event.at)) compiled.byStep.set(event.at, []);
          compiled.byStep.get(event.at).push(event);
        }
        if (compiled.events && layer.t === "line" && compiled.events.length) compiled.span = Math.ceil(Math.max(...compiled.events.map(e => e.at + e.length)) / steps) * steps;
        return compiled;
      });
      sections[name] = { name, bars: barCount, steps: total, chords, chordAt, melody, layers, dyn: merged.dyn ?? 1, dynTo: merged.dynTo ?? merged.dyn ?? 1 };
      return sections[name];
    };
    const timeline = [];
    let cursor = 0;
    for (const name of spec.form) {
      const section = resolve(name);
      timeline.push({ section, start: cursor });
      cursor += section.steps;
    }
    const loopIndex = spec.loop ?? 0;
    return {
      id: spec.id, title: spec.title, spec, scale, tonic: spec.tonic, steps, sub, beats,
      stepSeconds: 60 / spec.tempo / sub, timeline, totalSteps: cursor, loopStep: timeline[loopIndex].start,
      room: spec.room || "hall", gain: spec.gain ?? 1, swing: spec.swing || 0, variant: true,
    };
  }

  /* Strong-beat melody notes that clash with the chord, for authoring. */
  function lint(compiled) {
    const issues = [];
    for (const { section } of compiled.timeline) {
      for (const event of section.melody) {
        if (event.at % compiled.sub !== 0 || event.length < compiled.sub) continue;
        const entry = section.chordAt[event.at];
        if (!entry) continue;
        const pc = ((event.pitch - entry.chord.root) % 12 + 12) % 12;
        const tones = entry.chord.tones.map(t => ((t % 12) + 12) % 12);
        if (tones.includes(pc)) continue;
        const clash = tones.some(t => Math.abs(((pc - t + 6) % 12 + 12) % 12 - 6) === 1);
        if (clash) issues.push(`${compiled.id}/${section.name} bar ${Math.floor(event.at / compiled.steps) + 1}: degree ${pc} over ${entry.chord.name}`);
      }
    }
    return [...new Set(issues)];
  }

  /* ---------------- Sound ---------------- */
  const hz = midi => 440 * 2 ** ((midi - 69) / 12);
  function mulberry(seed) {
    let state = seed >>> 0;
    return () => {
      state = (state + 0x6D2B79F5) >>> 0;
      let t = state;
      t = Math.imul(t ^ (t >>> 15), t | 1);
      t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
  }
  const hash = text => [...String(text)].reduce((value, char) => Math.imul(value ^ char.charCodeAt(0), 16777619) >>> 0, 2166136261);

  // Instrument definitions. Levels are calibrated so v=1 sits at a similar loudness.
  const I = {
    strings: { kind: "bowed", voices: [-9, 0, 8], cutoff: 2700, attack: 0.2, release: 0.5, vib: [5.1, 7, 0.35], level: 0.05, pan: -0.25, send: 0.55 },
    violins: { kind: "bowed", voices: [-7, 6], cutoff: 3600, attack: 0.1, release: 0.4, vib: [5.6, 10, 0.25], level: 0.07, pan: -0.35, send: 0.5 },
    cellos: { kind: "bowed", voices: [-6, 6], cutoff: 1500, attack: 0.14, release: 0.45, vib: [4.8, 6, 0.35], level: 0.055, pan: 0.2, send: 0.45 },
    contrabass: { kind: "bowed", voices: [-5, 5], cutoff: 800, attack: 0.12, release: 0.4, level: 0.06, pan: 0.05, send: 0.3 },
    spiccato: { kind: "bowed", voices: [-7, 7], cutoff: 2900, attack: 0.008, decay: 0.07, release: 0.08, level: 0.075, pan: -0.15, send: 0.4 },
    lowSpiccato: { kind: "bowed", voices: [-6, 6], cutoff: 1400, attack: 0.008, decay: 0.08, release: 0.09, level: 0.07, pan: 0.1, send: 0.35 },
    tremolo: { kind: "bowed", voices: [-8, 0, 8], cutoff: 2500, attack: 0.15, release: 0.4, trem: 12, level: 0.05, pan: -0.2, send: 0.55 },
    fiddle: { kind: "bowed", voices: [-3, 4], cutoff: 4200, peak: [2600, 5], attack: 0.07, release: 0.3, vib: [5.9, 16, 0.18], level: 0.09, pan: 0.12, send: 0.4 },
    pad: { kind: "bowed", voices: [-11, 0, 10], cutoff: 1000, attack: 0.7, release: 1.4, level: 0.035, pan: 0, send: 0.7 },
    drone: { kind: "bowed", voices: [-7, 0, 6], cutoff: 520, attack: 1.4, release: 2, level: 0.045, pan: 0, send: 0.6 },
    horns: { kind: "brass", voices: [-5, 5], low: 260, high: 1700, attack: 0.1, release: 0.4, vib: [4.8, 5, 0.4], level: 0.1, pan: 0.3, send: 0.6 },
    trombones: { kind: "brass", voices: [-6, 6], low: 180, high: 1050, attack: 0.08, release: 0.35, level: 0.08, pan: 0.15, send: 0.5 },
    trumpet: { kind: "brass", voices: [-3, 3], low: 400, high: 3200, attack: 0.05, release: 0.3, vib: [5.4, 8, 0.3], level: 0.07, pan: 0.2, send: 0.5 },
    choir: { kind: "choir", vowel: [730, 1090, 2440], voices: [-13, -2, 11], attack: 0.45, release: 0.9, vib: [5.0, 11, 0.3], level: 0.17, pan: 0, send: 0.75 },
    choirOo: { kind: "choir", vowel: [330, 780, 2240], voices: [-12, 0, 12], attack: 0.55, release: 1.0, vib: [4.8, 9, 0.35], level: 0.24, pan: 0, send: 0.75 },
    sopranos: { kind: "choir", vowel: [880, 1240, 2850], voices: [-10, 0, 9], attack: 0.4, release: 0.9, vib: [5.4, 14, 0.25], level: 0.12, pan: -0.15, send: 0.8 },
    organ: { kind: "organ", harmonics: [0, 1, 0.55, 0.06, 0.38, 0, 0.24, 0, 0.16, 0, 0.08], attack: 0.04, release: 0.35, level: 0.045, pan: 0, send: 0.7 },
    flute: { kind: "wind", harmonics: [0, 1, 0.22, 0.07, 0.03], breath: 0.16, attack: 0.08, release: 0.2, vib: [5, 9, 0.3], level: 0.095, pan: 0.15, send: 0.55 },
    whistle: { kind: "wind", harmonics: [0, 1, 0.34, 0.14, 0.07], breath: 0.1, attack: 0.04, release: 0.14, vib: [6, 12, 0.22], level: 0.075, pan: 0.2, send: 0.45 },
    oboe: { kind: "wind", harmonics: [0, 1, 0.85, 0.62, 0.5, 0.36, 0.28, 0.18, 0.12, 0.08], cutoff: 2800, breath: 0.02, attack: 0.05, release: 0.16, vib: [5.4, 9, 0.25], level: 0.055, pan: 0.15, send: 0.5 },
    duduk: { kind: "wind", harmonics: [0, 1, 0.66, 0.44, 0.24, 0.12, 0.06], cutoff: 1700, breath: 0.07, attack: 0.09, release: 0.25, vib: [5, 13, 0.3], level: 0.08, pan: 0.1, send: 0.6 },
    clarinet: { kind: "wind", harmonics: [0, 1, 0.05, 0.5, 0.04, 0.3, 0.03, 0.15], cutoff: 2000, breath: 0.04, attack: 0.06, release: 0.18, vib: [4.6, 5, 0.4], level: 0.075, pan: 0.2, send: 0.55 },
    harp: { kind: "pluck", bright: 0.72, damp: 0.3, decay: 3.4, length: 3, level: 0.22, pan: -0.35, send: 0.55 },
    lute: { kind: "pluck", bright: 0.5, damp: 0.45, decay: 1.7, length: 1.8, body: 260, level: 0.2, pan: 0.3, send: 0.4 },
    oud: { kind: "pluck", bright: 0.42, damp: 0.48, decay: 1.25, length: 1.5, body: 220, level: 0.22, pan: 0.3, send: 0.45 },
    guitar: { kind: "pluck", bright: 0.55, damp: 0.4, decay: 2.2, length: 2.2, body: 200, level: 0.18, pan: 0.3, send: 0.45 },
    harpsichord: { kind: "pluck", bright: 0.98, damp: 0.2, decay: 1.7, length: 1.7, highpass: 220, level: 0.13, pan: -0.25, send: 0.5 },
    pizz: { kind: "pluck", bright: 0.32, damp: 0.5, decay: 0.55, length: 0.7, body: 180, level: 0.26, pan: 0.15, send: 0.45 },
    celesta: { kind: "mallet", partials: [[1, 1, 2.2], [2, 0.16, 0.9], [3.01, 0.08, 0.4], [4.04, 0.11, 0.25]], level: 0.09, pan: -0.3, send: 0.65 },
    marimba: { kind: "mallet", partials: [[1, 1, 0.75], [3.98, 0.14, 0.13], [9.9, 0.04, 0.05]], level: 0.17, pan: 0.25, send: 0.4 },
    kalimba: { kind: "mallet", partials: [[1, 1, 1.3], [5.4, 0.2, 0.09], [11.2, 0.05, 0.03]], level: 0.13, pan: -0.2, send: 0.5 },
    bell: { kind: "mallet", partials: [[0.5, 0.45, 6], [1, 1, 4.5], [1.19, 0.38, 3.5], [1.5, 0.3, 3], [2, 0.55, 2.6], [2.51, 0.22, 1.8], [3.01, 0.18, 1.4], [4.2, 0.1, 1]], level: 0.05, pan: 0.1, send: 0.85 },
    glass: { kind: "glass", attack: 0.28, release: 0.9, trem: 4.2, level: 0.05, pan: 0.2, send: 0.75 },
    sub: { kind: "sub", attack: 0.03, release: 0.25, level: 0.13, pan: 0, send: 0 },
  };
  // Each percussion voice: a pitch sweep, a noise burst, or partials. Levels are per hit.
  const P = {
    taiko: { body: [95, 44, 0.55], noise: [600, 0.12], click: 0.25, level: 0.13, send: 0.5 },
    war: { body: [70, 34, 0.9], noise: [380, 0.25], click: 0.15, level: 0.16, send: 0.6 },
    frame: { body: [120, 78, 0.28], noise: [1300, 0.07], level: 0.09, send: 0.4 },
    tom: { body: [150, 96, 0.32], noise: [1800, 0.06], level: 0.1, send: 0.45 },
    dum: { body: [92, 66, 0.3], noise: [900, 0.04], level: 0.11, send: 0.35 },
    tek: { high: [4200, 2.2, 0.045], tone: [620, 0.02], level: 0.08, send: 0.3 },
    snare: { high: [3800, 0.9, 0.17], tone: [186, 0.08], level: 0.1, send: 0.45 },
    muffled: { high: [1900, 1.1, 0.12], tone: [160, 0.07], level: 0.09, send: 0.5 },
    hat: { high: [8200, 0.7, 0.04], level: 0.035, send: 0.2 },
    shaker: { high: [6200, 0.8, 0.07], attack: 0.018, level: 0.04, send: 0.25 },
    clack: { high: [2600, 4, 0.03], tone: [900, 0.02], level: 0.07, send: 0.3 },
    log: { log: true, level: 0.1, send: 0.4 },
    crash: { high: [3200, 0.6, 1.7], level: 0.05, send: 0.6 },
    swell: { swell: true, level: 0.05, send: 0.7 },
    gong: { metal: [[1, 1, 5], [1.48, 0.55, 4], [2.05, 0.42, 3.4], [2.62, 0.3, 2.6], [3.31, 0.2, 2], [4.2, 0.12, 1.4]], base: 58, noise: [420, 1.8], level: 0.08, send: 0.8 },
    anvil: { metal: [[1, 1, 0.9], [2.76, 0.55, 0.5], [5.4, 0.3, 0.25], [8.93, 0.18, 0.14]], base: 1180, level: 0.035, send: 0.55 },
    zill: { metal: [[1, 1, 1.4], [2.32, 0.5, 0.8], [3.7, 0.3, 0.5]], base: 2900, level: 0.02, send: 0.5 },
    toll: { metal: [[0.5, 0.5, 7], [1, 1, 5], [1.19, 0.45, 4], [1.5, 0.3, 3.5], [2, 0.5, 3], [2.52, 0.2, 2]], base: 196, level: 0.06, send: 0.9 },
    thunder: { thunder: true, level: 0.2, send: 0.7 },
    boom: { body: [52, 28, 1.6], noise: [260, 0.4], level: 0.2, send: 0.7 },
    timp: { timp: true, level: 0.12, send: 0.6 },
  };
  const ROOMS = {
    chamber: { seconds: 1.4, dark: 0.45, wet: 0.24 }, hall: { seconds: 2.4, dark: 0.55, wet: 0.3 },
    forest: { seconds: 1.8, dark: 0.35, wet: 0.26 }, open: { seconds: 1.6, dark: 0.6, wet: 0.2 },
    cathedral: { seconds: 4.2, dark: 0.6, wet: 0.36 }, cavern: { seconds: 3.4, dark: 0.75, wet: 0.36 },
    mountain: { seconds: 4.8, dark: 0.5, wet: 0.34 },
  };

  // Per-context resources: noise, periodic waves, plucked-string tables and rooms.
  const resources = new WeakMap();
  function kit(ctx) {
    if (resources.has(ctx)) return resources.get(ctx);
    const noise = ctx.createBuffer(1, ctx.sampleRate * 2, ctx.sampleRate);
    const data = noise.getChannelData(0), random = mulberry(9001);
    for (let i = 0; i < data.length; i++) data[i] = random() * 2 - 1;
    const entry = { noise, waves: new Map(), plucks: new Map(), rooms: new Map() };
    resources.set(ctx, entry);
    return entry;
  }
  function periodic(ctx, harmonics) {
    const store = kit(ctx).waves, key = harmonics.join(",");
    if (!store.has(key)) store.set(key, ctx.createPeriodicWave(new Float32Array(harmonics.length), Float32Array.from(harmonics)));
    return store.get(key);
  }
  function impulse(ctx, roomName) {
    const store = kit(ctx).rooms;
    if (store.has(roomName)) return store.get(roomName);
    const room = ROOMS[roomName] || ROOMS.hall, rate = ctx.sampleRate;
    const length = Math.ceil(room.seconds * rate), buffer = ctx.createBuffer(2, length, rate);
    for (let channel = 0; channel < 2; channel++) {
      const samples = buffer.getChannelData(channel), random = mulberry(77 + channel * 991 + hash(roomName));
      let smooth = 0;
      for (let i = 0; i < length; i++) {
        const t = i / rate, progress = t / room.seconds;
        const coefficient = 1 - room.dark * Math.min(1, progress * 1.6) * 0.92;
        smooth += coefficient * ((random() * 2 - 1) - smooth);
        samples[i] = smooth * Math.exp(-6.9 * progress) * Math.min(1, t / 0.012);
      }
      // A few early reflections in the first 70 ms give the room a shape.
      for (let k = 0; k < 7; k++) {
        const at = Math.floor((0.008 + random() * 0.065) * rate);
        samples[at] += (random() < 0.5 ? -1 : 1) * (0.5 - k * 0.05);
      }
    }
    store.set(roomName, buffer);
    return buffer;
  }
  // Karplus-Strong: a noise burst circulating in a damped delay line of one period.
  function pluckBuffer(ctx, name, def, midi) {
    const store = kit(ctx).plucks, key = name + ":" + midi;
    if (store.has(key)) return store.get(key);
    if (store.size > 220) store.clear();
    const rate = ctx.sampleRate, frequency = hz(midi);
    const period = Math.max(2, Math.floor(rate / frequency - def.damp));
    const actual = rate / (period + def.damp);
    const length = Math.ceil(def.length * rate);
    const buffer = ctx.createBuffer(1, length, rate), y = buffer.getChannelData(0);
    const random = mulberry(hash(key));
    let low = 0;
    for (let i = 0; i < period; i++) { low += def.bright * ((random() * 2 - 1) - low); y[i] = low; }
    const loss = Math.pow(0.001, 1 / (def.decay * actual));
    for (let i = period; i < length; i++) y[i] = loss * ((1 - def.damp) * y[i - period] + def.damp * (y[i - period - 1] ?? 0));
    let dc = 0, peak = 0;
    for (let i = 0; i < length; i++) { dc += 0.002 * (y[i] - dc); y[i] -= dc; peak = Math.max(peak, Math.abs(y[i])); }
    // A 2 ms onset ramp keeps the pick sharp without a raw click; the tail fades over 40 ms.
    const fade = Math.floor(rate * 0.04), onset = Math.floor(rate * 0.002);
    for (let i = 0; i < length; i++) y[i] = y[i] / (peak || 1) * Math.min(1, (length - i) / fade, (i + 1) / onset);
    const entry = { buffer, rate: frequency / actual };
    store.set(key, entry);
    return entry;
  }

  /* ---------------- Performance ---------------- */
  function perform(ctx, compiled, destination, options = {}) {
    const resource = kit(ctx);
    const random = mulberry(hash(compiled.id) ^ (options.seed || 0));
    const nyquist = ctx.sampleRate / 2 - 200;
    const clampHz = value => Math.max(30, Math.min(nyquist, value));
    const output = ctx.createGain();
    output.connect(destination);
    const lowCut = ctx.createBiquadFilter(), lowShelf = ctx.createBiquadFilter(), presence = ctx.createBiquadFilter();
    lowCut.type = "highpass"; lowCut.frequency.value = 34; lowCut.Q.value = 0.7;
    lowShelf.type = "lowshelf"; lowShelf.frequency.value = 140; lowShelf.gain.value = compiled.spec.eq?.low ?? -3;
    presence.type = "peaking"; presence.frequency.value = 2600; presence.Q.value = 0.8; presence.gain.value = compiled.spec.eq?.presence ?? 2;
    lowCut.connect(lowShelf); lowShelf.connect(presence); presence.connect(output);
    const dry = ctx.createGain();
    dry.gain.value = compiled.gain;
    dry.connect(lowCut);
    const room = ROOMS[compiled.room] || ROOMS.hall;
    const reverb = ctx.createConvolver();
    reverb.buffer = impulse(ctx, compiled.room);
    const wet = ctx.createGain();
    wet.gain.value = room.wet * compiled.gain * 2.2;
    const send = ctx.createGain();
    send.connect(reverb); reverb.connect(wet); wet.connect(lowCut);
    const channels = new Map();
    const channel = (name, sendLevel) => {
      const key = name + ":" + sendLevel;
      if (!channels.has(key)) {
        const input = ctx.createGain(), toRoom = ctx.createGain();
        toRoom.gain.value = sendLevel;
        input.connect(dry); input.connect(toRoom); toRoom.connect(send);
        channels.set(key, input);
      }
      return channels.get(key);
    };
    const cleanup = (first, nodes) => { first.onended = () => nodes.forEach(node => { try { node.disconnect(); } catch { /* already gone */ } }); };
    const panner = value => { const node = ctx.createStereoPanner(); node.pan.value = Math.max(-1, Math.min(1, value)); return node; };

    function envelope(gain, when, length, peak, def) {
      const attack = Math.min(def.attack, Math.max(0.005, length * 0.7));
      gain.setValueAtTime(0.0001, when);
      gain.linearRampToValueAtTime(peak, when + attack);
      const end = when + Math.max(length, attack + 0.01);
      if (def.decay) gain.setTargetAtTime(peak * 0.3, when + attack, def.decay);
      else gain.linearRampToValueAtTime(peak * 0.8, end);
      gain.setTargetAtTime(0, end, Math.max(0.02, def.release / 3));
      return end + def.release * 1.7 + 0.05;
    }
    function vibrato(def, when, stop, targets, nodes) {
      if (!def.vib) return;
      const [rate, depth, delay] = def.vib;
      const lfo = ctx.createOscillator(), amount = ctx.createGain();
      lfo.frequency.value = rate * (0.92 + random() * 0.16);
      amount.gain.setValueAtTime(0, when);
      amount.gain.setValueAtTime(0, when + delay);
      amount.gain.linearRampToValueAtTime(depth, when + delay + 0.35);
      lfo.connect(amount);
      targets.forEach(param => amount.connect(param));
      lfo.start(when); lfo.stop(stop);
      nodes.push(lfo, amount);
    }

    function play(name, midi, when, length, velocity, pan) {
      const def = I[name];
      if (!def) throw new Error("Unknown instrument " + name);
      const frequency = hz(midi);
      const out = channel(name, def.send ?? 0.5);
      const pan2 = panner((pan ?? def.pan ?? 0) + (random() - 0.5) * 0.12);
      pan2.connect(out);
      const peak = def.level * velocity;
      const nodes = [pan2];
      const keyTrack = Math.min(1.7, Math.max(0.55, (frequency / 330) ** 0.35));

      if (def.kind === "pluck") {
        const { buffer, rate } = pluckBuffer(ctx, name, def, midi);
        const source = ctx.createBufferSource(), gain = ctx.createGain();
        source.buffer = buffer; source.playbackRate.value = rate;
        gain.gain.value = peak;
        let head = source;
        if (def.body) { const body = ctx.createBiquadFilter(); body.type = "peaking"; body.frequency.value = def.body; body.gain.value = 5; body.Q.value = 1.2; head.connect(body); head = body; nodes.push(body); }
        if (def.highpass) { const hp = ctx.createBiquadFilter(); hp.type = "highpass"; hp.frequency.value = def.highpass; head.connect(hp); head = hp; nodes.push(hp); }
        head.connect(gain); gain.connect(pan2);
        const ring = Math.min(def.length, Math.max(length * 3, 0.4));
        gain.gain.setValueAtTime(peak, when + ring - 0.12);
        gain.gain.linearRampToValueAtTime(0.0001, when + ring);
        source.start(when); source.stop(when + ring + 0.02);
        nodes.push(source, gain); cleanup(source, nodes);
        return;
      }
      if (def.kind === "mallet") {
        const sum = ctx.createGain();
        sum.gain.value = peak; sum.connect(pan2); nodes.push(sum);
        // The longest-ringing partial cleans up the whole note.
        let longest = null, longestDecay = 0;
        for (const [ratio, amount, decay] of def.partials) {
          const partial = frequency * ratio;
          if (partial >= nyquist) continue;
          const osc = ctx.createOscillator(), gain = ctx.createGain();
          osc.frequency.value = partial;
          gain.gain.setValueAtTime(0.0001, when);
          gain.gain.linearRampToValueAtTime(amount, when + 0.004);
          gain.gain.setTargetAtTime(0, when + 0.004, decay / 4.5);
          osc.connect(gain); gain.connect(sum);
          osc.start(when); osc.stop(when + decay + 0.1);
          nodes.push(osc, gain);
          if (decay > longestDecay) { longest = osc; longestDecay = decay; }
        }
        if (longest) cleanup(longest, nodes);
        return;
      }
      if (def.kind === "glass") {
        const sum = ctx.createGain(), trem = ctx.createGain(), lfo = ctx.createOscillator(), depth = ctx.createGain();
        sum.gain.value = 0;
        const stop = envelope(sum.gain, when, length, peak, def);
        trem.gain.value = 1; lfo.frequency.value = def.trem; depth.gain.value = 0.25;
        lfo.connect(depth); depth.connect(trem.gain);
        sum.connect(trem); trem.connect(pan2);
        const oscs = [[1, 1], [2, 0.12], [3, 0.05]].map(([ratio, amount]) => {
          const osc = ctx.createOscillator(), gain = ctx.createGain();
          osc.frequency.value = frequency * ratio; gain.gain.value = amount;
          osc.connect(gain); gain.connect(sum); osc.start(when); osc.stop(stop); nodes.push(osc, gain);
          return osc;
        });
        lfo.start(when); lfo.stop(stop);
        nodes.push(sum, trem, lfo, depth); cleanup(oscs[0], nodes);
        return;
      }
      if (def.kind === "sub") {
        const osc = ctx.createOscillator(), gain = ctx.createGain();
        osc.frequency.value = frequency;
        const stop = envelope(gain.gain, when, length, peak, def);
        osc.connect(gain); gain.connect(pan2); osc.start(when); osc.stop(stop);
        nodes.push(osc, gain); cleanup(osc, nodes);
        return;
      }

      // Sustained voices share an envelope after their own tone shaping.
      const amp = ctx.createGain();
      amp.gain.value = 0;
      const stop = envelope(amp.gain, when, length, peak, def);
      amp.connect(pan2);
      nodes.push(amp);
      const oscillators = [];
      const detunes = [];
      if (def.kind === "bowed" || def.kind === "brass") {
        const filter = ctx.createBiquadFilter();
        filter.type = "lowpass";
        filter.Q.value = def.kind === "brass" ? 1.1 : 0.6;
        if (def.kind === "brass") {
          const high = clampHz(def.high * keyTrack * (0.55 + 0.6 * velocity));
          filter.frequency.setValueAtTime(def.low, when);
          filter.frequency.exponentialRampToValueAtTime(high, when + 0.06 + def.attack * 0.6);
          filter.frequency.exponentialRampToValueAtTime(Math.max(def.low * 1.2, high * 0.7), when + 0.5);
        } else filter.frequency.value = clampHz(def.cutoff * keyTrack * (0.7 + 0.45 * velocity));
        let head = filter;
        if (def.peak) { const peakNode = ctx.createBiquadFilter(); peakNode.type = "peaking"; peakNode.frequency.value = def.peak[0]; peakNode.gain.value = def.peak[1]; peakNode.Q.value = 1.3; head.connect(peakNode); head = peakNode; nodes.push(peakNode); }
        if (def.trem) {
          const trem = ctx.createGain(), lfo = ctx.createOscillator(), depth = ctx.createGain();
          trem.gain.value = 0.7; lfo.frequency.value = def.trem * (0.95 + random() * 0.1); depth.gain.value = 0.3;
          lfo.connect(depth); depth.connect(trem.gain); head.connect(trem); head = trem;
          lfo.start(when); lfo.stop(stop); nodes.push(trem, lfo, depth);
        }
        head.connect(amp);
        nodes.push(filter);
        for (const cents of def.voices) {
          const osc = ctx.createOscillator();
          osc.type = "sawtooth";
          osc.frequency.value = frequency;
          const detune = cents + (random() - 0.5) * 4;
          if (def.kind === "brass") { osc.detune.setValueAtTime(detune - 28, when); osc.detune.linearRampToValueAtTime(detune, when + 0.07); }
          else osc.detune.value = detune;
          osc.connect(filter); oscillators.push(osc); detunes.push(osc.detune);
        }
      } else if (def.kind === "choir") {
        const mix = ctx.createGain(), sum = ctx.createGain(), air = ctx.createBiquadFilter();
        mix.gain.value = 1; sum.gain.value = 1;
        air.type = "lowpass"; air.frequency.value = 4200;
        def.vowel.forEach((formant, index) => {
          const band = ctx.createBiquadFilter(), level = ctx.createGain();
          band.type = "bandpass"; band.frequency.value = formant; band.Q.value = [5, 7, 9][index];
          level.gain.value = [1, 0.6, 0.25][index];
          mix.connect(band); band.connect(level); level.connect(sum); nodes.push(band, level);
        });
        sum.connect(air); air.connect(amp); nodes.push(mix, sum, air);
        for (const cents of def.voices) {
          const osc = ctx.createOscillator();
          osc.type = "sawtooth"; osc.frequency.value = frequency; osc.detune.value = cents + (random() - 0.5) * 5;
          osc.connect(mix); oscillators.push(osc); detunes.push(osc.detune);
        }
      } else if (def.kind === "organ") {
        const filter = ctx.createBiquadFilter();
        filter.type = "lowpass"; filter.frequency.value = clampHz(5200);
        filter.connect(amp); nodes.push(filter);
        for (const cents of [0, 3]) {
          const osc = ctx.createOscillator();
          osc.setPeriodicWave(periodic(ctx, def.harmonics)); osc.frequency.value = frequency; osc.detune.value = cents;
          osc.connect(filter); oscillators.push(osc); detunes.push(osc.detune);
        }
      } else if (def.kind === "wind") {
        const filter = ctx.createBiquadFilter();
        filter.type = "lowpass"; filter.frequency.value = clampHz((def.cutoff || 5200) * keyTrack);
        filter.connect(amp); nodes.push(filter);
        const osc = ctx.createOscillator();
        osc.setPeriodicWave(periodic(ctx, def.harmonics)); osc.frequency.value = frequency;
        osc.connect(filter); oscillators.push(osc); detunes.push(osc.detune);
        if (def.breath) {
          const breath = ctx.createBufferSource(), band = ctx.createBiquadFilter(), level = ctx.createGain();
          breath.buffer = resource.noise; breath.loop = true;
          band.type = "bandpass"; band.frequency.value = clampHz(frequency * 1.02); band.Q.value = 9;
          level.gain.value = def.breath * 3;
          breath.connect(band); band.connect(level); level.connect(filter);
          breath.start(when, random() * 1.5); breath.stop(stop);
          // A short chiff of air at the start of the note.
          const chiff = ctx.createBufferSource(), chiffBand = ctx.createBiquadFilter(), chiffGain = ctx.createGain();
          chiff.buffer = resource.noise; chiffBand.type = "highpass"; chiffBand.frequency.value = 1800;
          chiffGain.gain.setValueAtTime(def.breath * 0.6, when); chiffGain.gain.setTargetAtTime(0, when + 0.01, 0.02);
          chiff.connect(chiffBand); chiffBand.connect(chiffGain); chiffGain.connect(amp);
          chiff.start(when, random()); chiff.stop(when + 0.12);
          nodes.push(breath, band, level, chiff, chiffBand, chiffGain);
        }
      }
      vibrato(def, when, stop, detunes, nodes);
      oscillators.forEach(osc => { osc.start(when); osc.stop(stop); });
      nodes.push(...oscillators);
      cleanup(oscillators[0], nodes);
    }

    function hit(name, when, velocity, midi) {
      const def = P[name];
      if (!def) throw new Error("Unknown percussion " + name);
      const out = channel("perc:" + name, def.send ?? 0.5);
      const gain = ctx.createGain();
      gain.gain.value = def.level * velocity;
      gain.connect(out);
      const nodes = [gain];
      let first = null;
      const noiseBurst = (type, frequency, q, length, level, attack = 0.002) => {
        const source = ctx.createBufferSource(), filter = ctx.createBiquadFilter(), env = ctx.createGain();
        source.buffer = resource.noise;
        filter.type = type; filter.frequency.value = clampHz(frequency); filter.Q.value = q;
        env.gain.setValueAtTime(0.0001, when);
        env.gain.linearRampToValueAtTime(level, when + attack);
        env.gain.setTargetAtTime(0, when + attack, length / 4);
        source.connect(filter); filter.connect(env); env.connect(gain);
        source.start(when, random() * 1.5); source.stop(when + length + 0.05);
        nodes.push(source, filter, env); first = first || source;
      };
      const sweep = (from, to, length, level, type = "sine") => {
        const osc = ctx.createOscillator(), env = ctx.createGain();
        osc.type = type;
        osc.frequency.setValueAtTime(from, when);
        osc.frequency.exponentialRampToValueAtTime(to, when + length * 0.6);
        env.gain.setValueAtTime(0.0001, when);
        env.gain.linearRampToValueAtTime(level, when + 0.004);
        env.gain.setTargetAtTime(0, when + 0.004, length / 4);
        osc.connect(env); env.connect(gain);
        osc.start(when); osc.stop(when + length + 0.05);
        nodes.push(osc, env); first = first || osc;
      };
      if (def.body) {
        sweep(def.body[0], def.body[1], def.body[2], 1);
        if (def.noise) noiseBurst("lowpass", def.noise[0], 0.7, def.noise[1], 0.6);
        if (def.click) noiseBurst("highpass", 2400, 0.7, 0.012, def.click);
      }
      if (def.high) noiseBurst(name === "snare" || name === "muffled" ? "bandpass" : "highpass", def.high[0], def.high[1], def.high[2], 1, def.attack || 0.002);
      if (def.tone) sweep(def.tone[0], def.tone[0] * 0.85, def.tone[1], 0.6, "triangle");
      if (def.log) {
        const pitch = hz(midi ?? 57);
        sweep(pitch, pitch * 0.97, 0.45, 1);
        sweep(pitch * 2.7, pitch * 2.6, 0.12, 0.25);
      }
      if (def.timp) {
        const pitch = hz(midi ?? 41);
        [[1, 1, 1.8], [1.5, 0.45, 1.0], [1.98, 0.25, 0.6], [2.44, 0.12, 0.4]].forEach(([ratio, level, length]) => sweep(pitch * ratio * 1.02, pitch * ratio, length, level));
        noiseBurst("lowpass", 320, 0.7, 0.12, 0.5);
      }
      if (def.metal) {
        const base = midi ? hz(midi) : def.base;
        def.metal.forEach(([ratio, level, length]) => {
          const osc = ctx.createOscillator(), env = ctx.createGain();
          osc.frequency.value = Math.min(nyquist, base * ratio * (1 + (random() - 0.5) * 0.004));
          env.gain.setValueAtTime(0.0001, when);
          env.gain.linearRampToValueAtTime(level, when + (name === "gong" ? 0.06 : 0.003));
          env.gain.setTargetAtTime(0, when + 0.06, length / 4.5);
          osc.connect(env); env.connect(gain); osc.start(when); osc.stop(when + length + 0.1);
          nodes.push(osc, env); first = first || osc;
        });
        if (def.noise) noiseBurst("bandpass", def.noise[0], 0.8, def.noise[1], 0.35, 0.05);
      }
      if (def.swell) {
        const length = midi || 1.5;
        const source = ctx.createBufferSource(), filter = ctx.createBiquadFilter(), env = ctx.createGain();
        source.buffer = resource.noise; source.loop = true;
        filter.type = "highpass"; filter.frequency.value = 2600;
        env.gain.setValueAtTime(0.0001, when);
        env.gain.exponentialRampToValueAtTime(1, when + length);
        env.gain.setTargetAtTime(0, when + length, 0.18);
        source.connect(filter); filter.connect(env); env.connect(gain);
        source.start(when); source.stop(when + length + 1);
        nodes.push(source, filter, env); first = source;
      }
      if (def.thunder) {
        for (let k = 0; k < 5; k++) {
          const at = when + random() * 0.9, source = ctx.createBufferSource(), filter = ctx.createBiquadFilter(), env = ctx.createGain();
          source.buffer = resource.noise; source.loop = true;
          filter.type = "lowpass"; filter.frequency.value = 140 + random() * 160;
          env.gain.setValueAtTime(0.0001, at);
          env.gain.linearRampToValueAtTime(0.5 + random() * 0.5, at + 0.08 + random() * 0.3);
          env.gain.setTargetAtTime(0, at + 0.4, 0.6 + random() * 0.6);
          source.connect(filter); filter.connect(env); env.connect(gain);
          source.start(at, random()); source.stop(at + 4);
          nodes.push(source, filter, env); first = first || source;
        }
      }
      if (first) cleanup(first, nodes);
    }

    // Continuous beds of wind, rain, surf, embers and leaves for one section.
    function texture(kind, when, length, level) {
      const out = channel("tex:" + kind, 0.4);
      const source = ctx.createBufferSource(), filter = ctx.createBiquadFilter(), gain = ctx.createGain(), lfo = ctx.createOscillator(), depth = ctx.createGain();
      source.buffer = resource.noise; source.loop = true;
      const settings = {
        wind: ["bandpass", 520, 0.8, 0.07, 0.6], rain: ["highpass", 3200, 0.5, 0.31, 0.12], surf: ["lowpass", 760, 0.6, 0.085, 0.85],
        embers: ["bandpass", 2400, 2, 1.7, 0.7], leaves: ["bandpass", 3600, 1.2, 0.23, 0.55], drips: ["bandpass", 1500, 6, 0.6, 0.9],
      }[kind];
      filter.type = settings[0]; filter.frequency.value = settings[1]; filter.Q.value = settings[2];
      lfo.frequency.value = settings[3]; depth.gain.value = level * settings[4];
      gain.gain.setValueAtTime(0.0001, when);
      gain.gain.linearRampToValueAtTime(level, when + Math.min(2, length * 0.3));
      gain.gain.setValueAtTime(level, when + length - Math.min(1.5, length * 0.25));
      gain.gain.linearRampToValueAtTime(0.0001, when + length);
      lfo.connect(depth); depth.connect(gain.gain);
      if (kind === "wind") { const sweepDepth = ctx.createGain(); sweepDepth.gain.value = 260; lfo.connect(sweepDepth); sweepDepth.connect(filter.frequency); }
      source.connect(filter); filter.connect(gain); gain.connect(out);
      source.start(when, random() * 1.5); source.stop(when + length + 0.05); lfo.start(when); lfo.stop(when + length + 0.05);
      cleanup(source, [source, filter, gain, lfo, depth]);
    }

    /* ---------------- Sequencing ---------------- */
    const voicings = new Map();
    const chordPitch = (entry, symbol, octave) => {
      const base = compiled.tonic + entry.chord.root + octave * 12;
      const tones = entry.chord.tones;
      switch (symbol) {
        case "1": return base;
        case "3": return base + tones[1];
        case "5": return base + tones[2];
        case "7": return base + (tones[3] ?? 12);
        case "8": return base + 12;
        case "T": return base + tones[1] + 12;
        case "F": return base + tones[2] + 12;
        case "L": return base - 12;
        case "P": return base + tones[2] - 12;
        default: return null;
      }
    };
    // Pads move each voice to the nearest tone of the next chord.
    function voiceLead(key, entry, octave, count) {
      const center = compiled.tonic + octave * 12 + 4;
      const classes = entry.chord.tones.slice(0, count).map(t => (entry.chord.root + t) % 12);
      while (classes.length < count) classes.push(classes[classes.length % entry.chord.tones.length]);
      const previous = voicings.get(key);
      let best = null, bestCost = Infinity;
      for (let rotation = 0; rotation < classes.length; rotation++) {
        const order = classes.slice(rotation).concat(classes.slice(0, rotation));
        const notes = [];
        let last = center - 9;
        for (const pc of order) {
          let pitch = compiled.tonic - (compiled.tonic % 12) + octave * 12 + pc - 12;
          while (pitch <= last) pitch += 12;
          notes.push(pitch); last = pitch;
        }
        const shift = Math.round((center - (notes[0] + notes.at(-1)) / 2) / 12) * 12;
        const placed = notes.map(n => n + shift);
        const cost = previous ? placed.reduce((sum, n, i) => sum + Math.abs(n - (previous[i] ?? n)), 0) : Math.abs((placed[0] + placed.at(-1)) / 2 - center);
        if (cost < bestCost) { bestCost = cost; best = placed; }
      }
      voicings.set(key, best);
      return best;
    }
    const position = global => global < compiled.totalSteps ? global : compiled.loopStep + (global - compiled.loopStep) % (compiled.totalSteps - compiled.loopStep);
    const only = options.only ? new Set(options.only) : null;

    function step(global, when) {
      const at = position(global);
      let index = compiled.timeline.length - 1;
      while (index > 0 && compiled.timeline[index].start > at) index--;
      const { section, start } = compiled.timeline[index];
      const local = at - start;
      const stepSeconds = compiled.stepSeconds;
      const time = when + (compiled.swing && local % 2 === 1 ? compiled.swing * stepSeconds : 0);
      const entry = section.chordAt[local];
      const barStep = local % compiled.steps;
      const bar = Math.floor(local / compiled.steps);
      const dyn = section.dyn + (section.dynTo - section.dyn) * (local / Math.max(1, section.steps - 1));
      const human = (spread = 0.009) => time + (random() - 0.5) * spread;
      const vel = (v, accent = false) => v * dyn * (0.93 + random() * 0.14) * (accent ? 1.22 : 1);
      for (const layer of section.layers) {
        if (only && !only.has(layer.role || layer.t)) continue;
        if (layer.bars && !layer.bars.includes(bar % (layer.cycle || section.bars))) continue;
        const v = layer.v ?? 0.7, octave = layer.o ?? 0;
        switch (layer.t) {
          case "pad": {
            if (!entry || entry.start !== local) break;
            const notes = voiceLead(layer, entry, octave, layer.n || 3);
            const length = entry.length * stepSeconds * (layer.legato ?? 1.04);
            notes.forEach((pitch, i) => play(layer.i, pitch, human(0.02), length, vel(v) * (i === 0 ? 1 : 0.85), (layer.pan ?? I[layer.i].pan) + (i - (notes.length - 1) / 2) * (layer.spread ?? 0.35)));
            break;
          }
          case "drone": {
            if (local % (layer.every || section.steps) !== 0) break;
            const length = (layer.every || section.steps) * stepSeconds * 1.05;
            for (const d of layer.deg || ["1", "5"]) play(layer.i, compiled.tonic + degree(d, compiled.scale) + octave * 12, time, length, vel(v), layer.pan);
            break;
          }
          case "arp": case "bass": case "pulse": {
            if (!entry) break;
            const pattern = layer.p;
            const symbol = pattern[local % pattern.length];
            if (symbol === "." || symbol === "-") break;
            let length = 1;
            while (pattern[(local + length) % pattern.length] === "-" && length < pattern.length) length++;
            const pitch = chordPitch(entry, symbol, octave);
            if (pitch == null) break;
            const downbeat = barStep % compiled.sub === 0 ? 1 : 0.86;
            play(layer.i, pitch, human(0.006), length * stepSeconds * (layer.legato ?? 0.95), vel(v * downbeat), layer.pan);
            if (layer.dbl) play(layer.dbl.i, pitch + (layer.dbl.o || 0) * 12, human(0.006), length * stepSeconds * 0.95, vel(layer.dbl.v ?? v * 0.6), layer.dbl.pan);
            break;
          }
          case "strum": {
            if (!entry) break;
            const symbol = layer.p[local % layer.p.length];
            if (symbol !== "d" && symbol !== "u" && symbol !== "D") break;
            const notes = [chordPitch(entry, "1", octave), chordPitch(entry, "5", octave), chordPitch(entry, "8", octave), chordPitch(entry, "T", octave)];
            const ordered = symbol === "u" ? notes.reverse() : notes;
            ordered.forEach((pitch, i) => play(layer.i, pitch, time + i * 0.018, stepSeconds * 2, vel(v * (symbol === "D" ? 1.15 : symbol === "u" ? 0.7 : 0.9)), layer.pan));
            break;
          }
          case "mel": case "line": {
            const span = layer.t === "line" ? layer.span || section.steps : section.steps;
            const events = layer.byStep.get(layer.t === "line" ? local % span : local);
            if (!events) break;
            for (const event of events) {
              const pitch = compiled.tonic + event.pitch + octave * 12;
              const length = event.length * stepSeconds * (layer.legato ?? 0.98);
              play(layer.i, pitch, human(0.01), length, vel(v, event.accent), layer.pan);
              for (const extra of layer.dbl || []) play(extra.i, pitch + (extra.o || 0) * 12 + (extra.shift || 0), human(0.012), length, vel(extra.v ?? v * 0.5, event.accent), extra.pan);
              if (layer.harm) {
                const steps = layer.harm.steps ?? -2;
                const index = compiled.scale.findIndex(s => ((event.pitch % 12) + 12) % 12 === s);
                if (index >= 0) {
                  const target = index + steps;
                  const octaves = Math.floor(target / 7);
                  const harmony = event.pitch - compiled.scale[index] + compiled.scale[((target % 7) + 7) % 7] + octaves * 12;
                  play(layer.harm.i, compiled.tonic + harmony + (layer.harm.o ?? octave) * 12, human(0.012), length, vel(layer.harm.v ?? v * 0.5, event.accent), layer.harm.pan);
                }
              }
            }
            break;
          }
          case "drums": {
            for (const [name, pattern] of Object.entries(layer.p)) {
              const symbol = pattern[local % pattern.length];
              if (symbol === "." || symbol == null) continue;
              const strength = symbol === "X" ? 1.25 : symbol === "o" ? 0.45 : 1;
              const tuning = !entry ? undefined : name === "timp" ? chordPitch(entry, "1", -2 + (layer.timpOct || 0)) : name === "log" ? chordPitch(entry, symbol === "5" ? "5" : "1", layer.logOct ?? -1) : undefined;
              if (symbol === "r") {
                for (let k = 0; k < 4; k++) hit(name, time + k * stepSeconds / 4, v * (0.35 + k * 0.08), tuning);
              } else if (symbol === "f") {
                hit(name, time - 0.025, v * 0.45, tuning); hit(name, time, v, tuning);
              } else hit(name, human(0.004), vel(v * strength), tuning);
            }
            break;
          }
          case "hit": {
            if (barStep !== (layer.at || 0)) break;
            const tuning = layer.deg ? compiled.tonic + degree(layer.deg, compiled.scale) + octave * 12 : layer.i === "timp" ? chordPitch(entry, "1", -2) : undefined;
            hit(layer.i, time, vel(v), layer.i === "swell" ? (layer.len || compiled.steps) * stepSeconds : tuning);
            break;
          }
          case "tex": {
            if (local !== 0) break;
            texture(layer.kind, time, section.steps * stepSeconds + 0.6, v * 0.06);
            break;
          }
          case "drips": {
            if (random() > (layer.chance ?? 0.08)) break;
            play(layer.i || "kalimba", compiled.tonic + 24 + compiled.scale[Math.floor(random() * 7)] + octave * 12, time, 0.4, v * 0.5, (random() - 0.5) * 1.4);
            break;
          }
          default: throw new Error("Unknown layer " + layer.t);
        }
      }
    }
    return { bus: output, stepSeconds: compiled.stepSeconds, totalSteps: compiled.totalSteps, loopStep: compiled.loopStep, step };
  }

  window.MusicComposer = { MODES, compile, lint, perform, instruments: I, percussion: P, rooms: ROOMS };
})();

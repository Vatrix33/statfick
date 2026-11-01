"use strict";
// The music side of the audio harness (Docs/Audio.md, step 6). In the harness's Chromium page it loads
// the old game's music-composer.js and music-themes.js and
//  - with --music-data=<file.cs>: writes the theme specs (MusicThemes.all()) and the composer's
//    instrument, percussion and room tables as C# records (Engine/.../Audio/Music/MusicData.g.cs);
//  - with --music-references=<folder>: writes music-scores.json (each theme's compiled score as a
//    digest of its timeline, sections, chords, notes and layers, and lint's findings) and
//    music-logs.json (the real composer performing each theme, renderOffline's way plus 64 steps
//    past the loop point, on a stand-in context that records every Web Audio call as one line:
//    hashes per step, see Engine/.../Audio/Music/GraphBuilder.cs for the lines); and lint's findings
//    and compiled digests for the small specs of Reference/lint-specs.json, which have clashes (the
//    themes have none) and the chord forms no theme uses.
const fs = require("fs");
const path = require("path");

// ---------------- Runs in the page ----------------
function pageMain(options) {
  const view = new DataView(new ArrayBuffer(8));
  class Fnv {
    constructor() { this.hi = 0xcbf29ce4; this.lo = 0x84222325; }
    byte(b) {
      this.lo = (this.lo ^ b) >>> 0;
      const t = this.lo * 0x1b3, carry = Math.floor(t / 4294967296);
      const hi = (Math.imul(this.hi, 0x1b3) >>> 0) + carry + ((this.lo << 8) >>> 0);
      this.lo = t >>> 0; this.hi = hi >>> 0;
    }
    floats(array) {
      const bytes = new Uint8Array(array.buffer, array.byteOffset, array.byteLength);
      for (let i = 0; i < bytes.length; i++) this.byte(bytes[i]);
      return this;
    }
    text(s) { const bytes = new TextEncoder().encode(s); for (let i = 0; i < bytes.length; i++) this.byte(bytes[i]); return this; }
    hex() { return this.hi.toString(16).padStart(8, "0") + this.lo.toString(16).padStart(8, "0"); }
    low() { return this.lo.toString(16).padStart(8, "0"); }
  }
  const F = v => { view.setFloat32(0, v, true); return view.getUint32(0, true).toString(16).padStart(8, "0"); };
  const T = v => { view.setFloat64(0, v, true); return view.getUint32(4, true).toString(16).padStart(8, "0") + view.getUint32(0, true).toString(16).padStart(8, "0"); };

  // A stand-in AudioContext recording each call the composer makes, as GraphBuilder records them.
  function recordingContext(sampleRate, log) {
    let nextNode = 1, nextBuffer = 0, nextWave = 0, collector = null;
    const hashed = new Set();
    const param = ref => ({
      __ref: ref,
      set value(v) { log.push(`${ref}=${F(v)}`); },
      setValueAtTime(v, t) { log.push(`${ref}.set(${F(v)},${T(t)})`); return this; },
      linearRampToValueAtTime(v, t) { log.push(`${ref}.lin(${F(v)},${T(t)})`); return this; },
      exponentialRampToValueAtTime(v, t) { log.push(`${ref}.exp(${F(v)},${T(t)})`); return this; },
      setTargetAtTime(v, t, c) { log.push(`${ref}.target(${F(v)},${T(t)},${T(c)})`); return this; },
    });
    const bufferLine = b => {
      if (hashed.has(b)) return `b${b.__id}`;
      hashed.add(b);
      const hash = new Fnv();
      for (const channel of b.__data) hash.floats(channel);
      return `b${b.__id} #${hash.hex()}`;
    };
    const node = (kind, params) => {
      const id = nextNode++, ref = `n${id}`;
      log.push(`${ref}=${kind}`);
      const n = {
        __ref: ref,
        connect(target) { log.push(`${ref}>${target.__ref}`); return target; },
        disconnect() { if (collector) collector.push(ref); },
        start(when, offset) { log.push(offset === undefined ? `${ref}.start(${T(when)})` : `${ref}.start(${T(when)},${T(offset)})`); },
        stop(when) { log.push(`${ref}.stop(${T(when)})`); },
        set type(v) { log.push(`${ref}.type=${v}`); },
        set buffer(b) { log.push(`${ref}.buffer=${bufferLine(b)}`); },
        set loop(v) { log.push(`${ref}.loop=${v}`); },
        // cleanup(): the handler only disconnects its nodes; calling it here lists them.
        set onended(handler) { collector = []; handler(); log.push(`${ref}.onended=${collector.join(",")}`); collector = null; },
        setPeriodicWave(w) { log.push(`${ref}.wave=w${w.__id}`); },
      };
      for (const p of params) n[p] = param(`${ref}.${p}`);
      return n;
    };
    return {
      sampleRate: Math.fround(sampleRate),
      destination: { __ref: "n0" },
      createGain: () => node("gain", ["gain"]),
      createOscillator: () => node("osc", ["frequency", "detune"]),
      createBiquadFilter: () => node("biquad", ["frequency", "Q", "gain", "detune"]),
      createBufferSource: () => node("source", ["playbackRate", "detune"]),
      createConvolver: () => node("convolver", []),
      createStereoPanner: () => node("panner", ["pan"]),
      createBuffer(channels, length) {
        const id = nextBuffer++;
        log.push(`b${id}=${channels}x${length}`);
        const data = [...Array(channels)].map(() => new Float32Array(length));
        return { __id: id, __data: data, numberOfChannels: channels, length, getChannelData: c => data[c] };
      },
      createPeriodicWave(real, imag) {
        const id = nextWave++;
        log.push(`w${id}=wave ${imag.length} ${new Fnv().floats(imag).hex()}`);
        return { __id: id };
      },
    };
  }

  // A compiled score as text, the same way MusicScoreTests prints the engine's.
  function digest(compiled) {
    const lines = [];
    lines.push(`theme ${compiled.id} steps=${T(compiled.steps)} sub=${T(compiled.sub)} beats=${T(compiled.beats)} tonic=${T(compiled.tonic)} ` +
      `stepSeconds=${T(compiled.stepSeconds)} total=${T(compiled.totalSteps)} loop=${T(compiled.loopStep)} room=${compiled.room} gain=${T(compiled.gain)} swing=${T(compiled.swing)}`);
    lines.push(`scale ${compiled.scale.map(T).join(",")}`);
    const printed = new Set();
    for (const { section, start } of compiled.timeline) {
      lines.push(`at ${T(start)} ${section.name}`);
      if (printed.has(section)) continue;
      printed.add(section);
      lines.push(`section ${section.name} bars=${T(section.bars)} steps=${T(section.steps)} dyn=${T(section.dyn)} dynTo=${T(section.dynTo)}`);
      const noteLine = e => `note ${T(e.at)} ${T(e.length)} ${T(e.pitch)} ${e.accent ? 1 : 0}`;
      for (const entry of section.chords)
        lines.push(entry.chord ? `chord ${T(entry.start)} ${T(entry.length)} ${entry.chord.name} ${T(entry.chord.root)} ${entry.chord.tones.map(T).join(",")}`
          : `chord ${T(entry.start)} ${T(entry.length)} -`);
      const chordAt = [];
      for (let i = 0; i < section.steps; i++) chordAt.push(section.chordAt[i] ? String(section.chords.indexOf(section.chordAt[i])) : "-");
      lines.push(`chordAt ${chordAt.join(",")}`);
      for (const e of section.melody) lines.push(noteLine(e));
      section.layers.forEach((layer, index) => {
        lines.push(`layer ${index} ${layer.t} events=${layer.events ? layer.events.length : "-"} span=${layer.span === undefined ? "-" : T(layer.span)}`);
        for (const e of layer.events || []) lines.push(noteLine(e));
        lines.push(`bystep ${[...layer.byStep.keys()].map(T).join(",")}`);
      });
    }
    return lines;
  }

  const result = {};
  if (options.data) {
    result.data = { themes: window.MusicThemes.all(), instruments: window.MusicComposer.instruments, percussion: window.MusicComposer.percussion, rooms: window.MusicComposer.rooms };
  }
  if (options.references) {
    const scores = {}, logs = {};
    for (const spec of window.MusicThemes.all()) {
      const compiled = window.MusicComposer.compile(spec);
      const lines = digest(compiled);
      scores[spec.id] = { digest: new Fnv().text(lines.join("\n") + "\n").hex(), lines: lines.length, lint: window.MusicComposer.lint(compiled) };
      for (const rate of options.rates) {
        if (rate !== 44100 && !options.fullRateThemes.includes(spec.id)) continue;
        const log = [];
        const ctx = recordingContext(rate, log);
        const performance = window.MusicComposer.perform(ctx, compiled, ctx.destination);
        const segments = [];
        let start = 0, total = new Fnv(), count = 0;
        const close = () => {
          const segment = new Fnv();
          for (let i = start; i < log.length; i++) { segment.text(log[i] + "\n"); total.text(log[i] + "\n"); }
          segments.push(segment.low());
          count += log.length - start;
          log.length = 0;
        };
        close();
        for (let index = 0; index < compiled.totalSteps + 64; index++) {
          performance.step(index, 0.05 + index * compiled.stepSeconds);
          close();
        }
        logs[`${spec.id}@${rate}`] = { lines: count, hash: total.hex(), segments: segments.join(" ") };
      }
    }
    // Small specs with clashes and chord forms the real themes don't have (Reference/lint-specs.json).
    scores.lintSpecs = Object.fromEntries(options.lintSpecs.map(spec => {
      const compiled = window.MusicComposer.compile(spec), lines = digest(compiled);
      return [spec.id, { digest: new Fnv().text(lines.join("\n") + "\n").hex(), lines: lines.length, lint: window.MusicComposer.lint(compiled) }];
    }));
    result.scores = scores;
    result.logs = logs;
  }
  return result;
}

// ---------------- Runs in Node ----------------
const cs = {
  num(n, where) {
    if (typeof n !== "number" || !Number.isFinite(n)) throw new Error(`${where}: not a finite number: ${n}`);
    return Object.is(n, -0) ? "-0.0" : String(n);
  },
  int(n, where) {
    if (!Number.isInteger(n)) throw new Error(`${where}: not an integer: ${n}`);
    return String(n);
  },
  str(s, where) {
    if (typeof s !== "string") throw new Error(`${where}: not a string: ${s}`);
    return JSON.stringify(s);
  },
  nums(a, where) { if (!Array.isArray(a)) throw new Error(`${where}: not an array`); return "[" + a.map((n, i) => cs.num(n, `${where}[${i}]`)).join(", ") + "]"; },
  ints(a, where) { if (!Array.isArray(a)) throw new Error(`${where}: not an array`); return "[" + a.map((n, i) => cs.int(n, `${where}[${i}]`)).join(", ") + "]"; },
  strs(a, where) { if (!Array.isArray(a)) throw new Error(`${where}: not an array`); return "[" + a.map((s, i) => cs.str(s, `${where}[${i}]`)).join(", ") + "]"; },
  bool(b, where) { if (typeof b !== "boolean") throw new Error(`${where}: not a boolean: ${b}`); return String(b); },
};

// JavaScript key → [C# property, writer]; a key missing here is an error, so nothing is dropped.
function record(object, fields, where, indent) {
  const parts = [];
  for (const [key, value] of Object.entries(object)) {
    const field = fields[key];
    if (!field) throw new Error(`${where}: unknown key "${key}"`);
    const [name, write] = typeof field === "function" ? field(value) : field;
    parts.push(`${name} = ${write(value, `${where}.${key}`, indent)}`);
  }
  return parts;
}

const layerFields = {
  t: ["Type", cs.str], i: ["Instrument", cs.str], o: ["Octave", cs.num], v: ["Velocity", cs.num],
  p: value => typeof value === "string" ? ["Pattern", cs.str] : ["Drums", (p, where) => {
    if (typeof p !== "object" || Array.isArray(p)) throw new Error(`${where}: a drums pattern must be an object`);
    return "[" + Object.entries(p).map(([k, s]) => `new(${cs.str(k, where)}, ${cs.str(s, `${where}.${k}`)})`).join(", ") + "]";
  }],
  deg: value => Array.isArray(value) ? ["Degrees", cs.strs] : ["Degree", cs.str],
  n: ["Voices", cs.int], every: ["Every", cs.int], bars: ["Bars", cs.ints], cycle: ["Cycle", cs.int], chance: ["Chance", cs.num],
  len: ["Length", cs.num], at: ["At", cs.int], kind: ["Kind", cs.str], role: ["Role", cs.str], notes: ["Notes", cs.str],
  pan: ["Pan", cs.num], spread: ["Spread", cs.num], legato: ["Legato", cs.num], timpOct: ["TimpaniOctave", cs.num], logOct: ["LogOctave", cs.num],
  harm: ["Harmony", (h, where) => `new() { ${record(h, { i: ["Instrument", cs.str], steps: ["Steps", cs.num], v: ["Velocity", cs.num], o: ["Octave", cs.num], pan: ["Pan", cs.num] }, where).join(", ")} }`],
  dbl: ["Doublings", (list, where) => {
    if (!Array.isArray(list)) throw new Error(`${where}: dbl must be a list (the composer iterates it)`);
    return "[" + list.map((d, i) => `new() { ${record(d, { i: ["Instrument", cs.str], o: ["Octave", cs.num], v: ["Velocity", cs.num], pan: ["Pan", cs.num], shift: ["Shift", cs.num] }, `${where}[${i}]`).join(", ")} }`).join(", ") + "]";
  }],
};

function sectionCs(section, where, indent) {
  const pad = " ".repeat(indent);
  const parts = record(section, {
    chords: ["Chords", cs.str], mel: ["Melody", cs.str], dyn: ["Dynamics", cs.num], dynTo: ["DynamicsTo", cs.num], like: ["Like", cs.str],
    layers: ["Layers", (layers, w) => "\n" + pad + "    [\n" + layers.map((layer, i) =>
      `${pad}        new() { ${record(layer, layerFields, `${w}[${i}]`).join(", ")} },\n`).join("") + pad + "    ]"],
  }, where);
  return `new() { ${parts.join(", ")} }`;
}

function themeCs(theme) {
  const where = theme.id;
  const parts = record(theme, {
    id: ["Id", cs.str], title: ["Title", cs.str], tempo: ["Tempo", cs.num], meter: ["Meter", cs.ints], tonic: ["Tonic", cs.num],
    mode: ["Mode", cs.str], room: ["Room", cs.str], gain: ["Gain", cs.num], swing: ["Swing", cs.num],
    eq: ["Eq", (eq, w) => `new() { ${record(eq, { low: ["Low", cs.num], presence: ["Presence", cs.num] }, w).join(", ")} }`],
    sections: ["Sections", (sections, w) => "new Dictionary<string, SectionSpec>\n            {\n" +
      Object.entries(sections).map(([name, s]) => `                [${cs.str(name, w)}] = ${sectionCs(s, `${w}.${name}`, 16)},\n`).join("") + "            }"],
    form: ["Form", cs.strs], loop: ["Loop", cs.int], motif: ["Motif", cs.str],
  }, where);
  return "        new()\n        {\n" + parts.map(p => `            ${p},\n`).join("") + "        },\n";
}

function dataCs(data) {
  const instrumentFields = {
    kind: ["Kind", cs.str], voices: ["Voices", cs.nums], cutoff: ["Cutoff", cs.num], attack: ["Attack", cs.num], release: ["Release", cs.num],
    vib: ["Vibrato", cs.nums], level: ["Level", cs.num], pan: ["Pan", cs.num], send: ["Send", cs.num], decay: ["Decay", cs.num],
    trem: ["Tremolo", cs.num], peak: ["Peak", cs.nums], low: ["Low", cs.num], high: ["High", cs.num], vowel: ["Vowel", cs.nums],
    harmonics: ["Harmonics", cs.nums], breath: ["Breath", cs.num], bright: ["Bright", cs.num], damp: ["Damp", cs.num],
    length: ["Length", cs.num], body: ["Body", cs.num], highpass: ["Highpass", cs.num],
    partials: ["Partials", (list, w) => "[" + list.map((p, i) => cs.nums(p, `${w}[${i}]`)).join(", ") + "]"],
  };
  const percussionFields = {
    body: ["Body", cs.nums], noise: ["Noise", cs.nums], click: ["Click", cs.num], level: ["Level", cs.num], send: ["Send", cs.num],
    high: ["High", cs.nums], tone: ["Tone", cs.nums], attack: ["Attack", cs.num], log: ["Log", cs.bool], swell: ["Swell", cs.bool],
    metal: ["Metal", (list, w) => "[" + list.map((p, i) => cs.nums(p, `${w}[${i}]`)).join(", ") + "]"], base: ["Base", cs.num],
    thunder: ["Thunder", cs.bool], timp: ["Timpani", cs.bool],
  };
  let out = "// <auto-generated> by Tools/Scripts/make-music-data.sh from the old game's music-themes.js\n" +
    "// (MusicThemes.all()) and music-composer.js (MusicComposer.instruments, percussion, rooms). Don't edit.\n" +
    "// </auto-generated>\n\nnamespace Statventure.Audio;\n\ninternal static class MusicData\n{\n";
  out += "    public static readonly IReadOnlyDictionary<string, InstrumentSpec> Instruments = new Dictionary<string, InstrumentSpec>\n    {\n";
  for (const [name, def] of Object.entries(data.instruments))
    out += `        [${cs.str(name)}] = new() { ${record(def, instrumentFields, `instrument ${name}`).join(", ")} },\n`;
  out += "    };\n\n    public static readonly IReadOnlyDictionary<string, PercussionSpec> Percussion = new Dictionary<string, PercussionSpec>\n    {\n";
  for (const [name, def] of Object.entries(data.percussion))
    out += `        [${cs.str(name)}] = new() { ${record(def, percussionFields, `percussion ${name}`).join(", ")} },\n`;
  out += "    };\n\n    public static readonly IReadOnlyDictionary<string, RoomSpec> Rooms = new Dictionary<string, RoomSpec>\n    {\n";
  for (const [name, room] of Object.entries(data.rooms)) {
    for (const key of Object.keys(room)) if (!["seconds", "dark", "wet"].includes(key)) throw new Error(`room ${name}: unknown key ${key}`);
    out += `        [${cs.str(name)}] = new(${cs.num(room.seconds, name)}, ${cs.num(room.dark, name)}, ${cs.num(room.wet, name)}),\n`;
  }
  out += "    };\n\n    /// <summary>MusicThemes.all(): the region themes, the battle theme and its regional arrangements, the boss themes.</summary>\n";
  out += "    public static readonly ThemeSpec[] Themes =\n    [\n";
  for (const theme of data.themes) out += themeCs(theme);
  out += "    ];\n}\n";
  return out.replace(/ = \n/g, " =\n");
}

async function run(window, argument) {
  const scripts = argument("--old-game");
  for (const file of ["music-composer.js", "music-themes.js"])
    await window.webContents.executeJavaScript(fs.readFileSync(path.join(scripts, file), "utf8") + "\n;0");
  const dataOut = argument("--music-data"), referencesOut = argument("--music-references");
  const lintSpecs = referencesOut ? JSON.parse(fs.readFileSync(path.join(referencesOut, "lint-specs.json"), "utf8")) : [];
  const options = { data: Boolean(dataOut), references: Boolean(referencesOut), rates: [44100, 48000], lintSpecs,
    fullRateThemes: ["alder-vale", "combat", "great-imp-jiruan", "deep-desert", "verdigris-jungle", "anatemis-white-star"] };
  const result = await window.webContents.executeJavaScript(`(${pageMain.toString()})(${JSON.stringify(options)})`, true);
  if (dataOut) {
    fs.writeFileSync(dataOut, dataCs(result.data));
    console.log("wrote", dataOut);
  }
  if (referencesOut) {
    fs.writeFileSync(path.join(referencesOut, "music-scores.json"), JSON.stringify(result.scores, null, 1) + "\n");
    fs.writeFileSync(path.join(referencesOut, "music-logs.json"), JSON.stringify(result.logs, null, 1) + "\n");
    console.log("wrote", path.join(referencesOut, "music-scores.json"), "and music-logs.json");
  }
}

module.exports = { run };

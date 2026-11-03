"use strict";
// The music side of the audio harness (Docs/Audio.md, step 6). In the harness's Chromium page it loads
// the old game's music-composer.js and music-themes.js and
//  - with --music-data=<folder>: writes the theme specs (MusicThemes.all()) and the composer's
//    instrument, percussion and room tables as their records' JSON forms, one file each, plus the
//    theme order (audio/themes/<name>.json …, audio/theme-order.json), which make-music-data.sh packs
//    into engine/Assets/Packs/audio.pak (Docs/Assets.md, step 6);
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
// The records' JSON forms (Docs/Assets.md, "Records" and step 6): what RecordSerializer.ToJson writes, so
// `packer extract` of audio.pak gives these files back byte for byte: "$types" first (the root's type,
// then the others in the order a depth-first walk in field order meets them), the fields in field
// order under the records' own names (Q22), a null left out, a non-nullable default written, indented
// by two spaces, a final line end.
const value = {
  num(n, where) {
    if (typeof n !== "number" || !Number.isFinite(n)) throw new Error(`${where}: not a finite number: ${n}`);
    return n;
  },
  int(n, where) {
    if (!Number.isInteger(n) || n < -2147483648 || n > 2147483647) throw new Error(`${where}: not a 32-bit integer: ${n}`);
    return n;
  },
  str(s, where) {
    if (typeof s !== "string") throw new Error(`${where}: not a string: ${s}`);
    return s;
  },
  bool(b, where) { if (typeof b !== "boolean") throw new Error(`${where}: not a boolean: ${b}`); return b; },
  list(item) {
    return (a, where, types) => {
      if (!Array.isArray(a)) throw new Error(`${where}: not an array`);
      return a.map((x, i) => item(x, `${where}[${i}]`, types));
    };
  },
};
value.nums = value.list(value.num);
value.ints = value.list(value.int);
value.strs = value.list(value.str);
value.numLists = value.list(value.nums);

// A record type: its name and its fields in field order, each [JavaScript key, JSON name, writer] or
// [JavaScript key, chooser] (one key, two fields: a layer's p and deg), optionally with the default a
// non-nullable field has when the key is absent. A key no field takes is an error, so nothing is dropped.
function recordType(name, fields) {
  const writer = (object, where, types) => {
    if (typeof object !== "object" || object === null || Array.isArray(object)) throw new Error(`${where}: not an object`);
    if (!types.includes(name)) types.push(name);
    const byKey = new Map(fields.map(f => [f.key, f]));
    for (const key of Object.keys(object)) if (!byKey.has(key)) throw new Error(`${where}: unknown key "${key}"`);
    const out = {};
    for (const field of fields) {
      const present = Object.prototype.hasOwnProperty.call(object, field.key);
      if (!present) {
        if (field.default !== undefined) out[field.name] = field.default;
        continue;
      }
      const raw = object[field.key];
      const [name2, write] = field.choose ? field.choose(raw) : [field.name, field.write];
      if (field.choose && name2 !== field.name) continue; // written by its twin field
      out[name2] = write(raw, `${where}.${field.key}`, types);
    }
    return out;
  };
  return writer;
}
const f = (key, name, write, extra = {}) => ({ key, name, write, ...extra });
// A key two fields share (p: a pattern or drums; deg: a degree or degrees): each field writes it only when the chooser picks it.
const either = (key, name, choose) => ({ key, name, choose });

const drums = (p, where) => {
  if (typeof p !== "object" || p === null || Array.isArray(p)) throw new Error(`${where}: a drums pattern must be an object`);
  const out = {};
  for (const [k, s] of Object.entries(p)) out[value.str(k, where)] = value.str(s, `${where}.${k}`);
  return out;
};
const pOf = v => typeof v === "string" ? ["pattern", value.str] : ["drums", drums];
const degOf = v => Array.isArray(v) ? ["degrees", value.strs] : ["degree", value.str];

const harmony = recordType("audio.harmony", [f("i", "instrument", value.str), f("steps", "steps", value.num), f("v", "velocity", value.num), f("o", "octave", value.num), f("pan", "pan", value.num)]);
const doubling = recordType("audio.doubling", [f("i", "instrument", value.str), f("o", "octave", value.num), f("v", "velocity", value.num), f("pan", "pan", value.num), f("shift", "shift", value.num)]);
const layer = recordType("audio.layer", [
  f("t", "type", value.str), f("i", "instrument", value.str), f("o", "octave", value.num), f("v", "velocity", value.num),
  either("p", "pattern", pOf), either("p", "drums", pOf), either("deg", "degree", degOf), either("deg", "degrees", degOf),
  f("n", "voices", value.int), f("every", "every", value.int), f("bars", "bars", value.ints), f("cycle", "cycle", value.int), f("chance", "chance", value.num),
  f("len", "length", value.num), f("at", "at", value.int), f("kind", "kind", value.str), f("role", "role", value.str), f("notes", "notes", value.str),
  f("pan", "pan", value.num), f("spread", "spread", value.num), f("legato", "legato", value.num), f("timpOct", "timpaniOctave", value.num), f("logOct", "logOctave", value.num),
  f("harm", "harmony", harmony),
  f("dbl", "doublings", (list, where, types) => {
    if (!Array.isArray(list)) throw new Error(`${where}: dbl must be a list (the composer iterates it)`);
    return value.list(doubling)(list, where, types);
  }),
]);
const section = recordType("audio.section", [
  f("chords", "chords", value.str), f("mel", "melody", value.str), f("dyn", "dynamics", value.num), f("dynTo", "dynamicsTo", value.num), f("like", "like", value.str),
  f("layers", "layers", value.list(layer)),
]);
const eq = recordType("audio.eq", [f("low", "low", value.num), f("presence", "presence", value.num)]);
const theme = recordType("audio.theme", [
  f("id", "id", value.str), f("title", "title", value.str), f("tempo", "tempo", value.num), f("meter", "meter", value.ints), f("tonic", "tonic", value.num),
  f("mode", "mode", value.str), f("room", "room", value.str), f("gain", "gain", value.num), f("swing", "swing", value.num), f("eq", "eq", eq),
  f("sections", "sections", (sections, where, types) => {
    if (typeof sections !== "object" || sections === null || Array.isArray(sections)) throw new Error(`${where}: not an object`);
    const out = {};
    for (const [name, s] of Object.entries(sections)) out[name] = section(s, `${where}.${name}`, types);
    return out;
  }),
  f("form", "form", value.strs), f("loop", "loop", value.int), f("motif", "motif", value.str),
]);
const instrument = recordType("audio.instrument", [
  f("kind", "kind", value.str), f("voices", "voices", value.nums), f("cutoff", "cutoff", value.num), f("attack", "attack", value.num), f("release", "release", value.num),
  f("vib", "vibrato", value.nums), f("level", "level", value.num), f("pan", "pan", value.num), f("send", "send", value.num), f("decay", "decay", value.num),
  f("trem", "tremolo", value.num), f("peak", "peak", value.nums), f("low", "low", value.num), f("high", "high", value.num), f("vowel", "vowel", value.nums),
  f("harmonics", "harmonics", value.nums), f("breath", "breath", value.num), f("bright", "bright", value.num), f("damp", "damp", value.num),
  f("length", "length", value.num), f("body", "body", value.num), f("highpass", "highpass", value.num), f("partials", "partials", value.numLists),
]);
const percussion = recordType("audio.percussion", [
  f("body", "body", value.nums), f("noise", "noise", value.nums), f("click", "click", value.num), f("level", "level", value.num), f("send", "send", value.num),
  f("high", "high", value.nums), f("tone", "tone", value.nums), f("attack", "attack", value.num), f("log", "log", value.bool, { default: false }),
  f("swell", "swell", value.bool, { default: false }), f("metal", "metal", value.numLists), f("base", "base", value.num),
  f("thunder", "thunder", value.bool, { default: false }), f("timp", "timpani", value.bool, { default: false }),
]);
const room = recordType("audio.room", [f("seconds", "seconds", value.num), f("dark", "dark", value.num), f("wet", "wet", value.num)]);
const themeOrder = recordType("audio.theme-order", [f("themes", "themes", value.strs)]);

// A name's path name by the music's rule (S6-1; MusicNames in Engine/.../Audio/Music/MusicLibrary.cs): a
// capital becomes '_' and the small letter, '@' becomes '.'; a name the rule or the path rule can't store stops it.
function pathName(name, where) {
  let out = "";
  for (const c of name) {
    if (/[a-z0-9-]/.test(c)) out += c;
    else if (/[A-Z]/.test(c)) out += "_" + c.toLowerCase();
    else if (c === "@") out += ".";
    else throw new Error(`${where}: the name "${name}" can't be stored as a data path ('${c}')`);
  }
  const stem = out.split(".")[0];
  if (out.length === 0 || out.length > 255 || out.endsWith(".") || /^(con|prn|aux|nul|com[0-9]|lpt[0-9])$/.test(stem))
    throw new Error(`${where}: the name "${name}" can't be stored as a data path ("${out}" breaks the path rule)`);
  return out;
}

// The JSON text of a record: "$types" first, then its fields; -0 kept (JSON.stringify would write 0).
function recordJson(write, object, where) {
  const types = [];
  const fields = write(object, where, types);
  const document = { $types: Object.fromEntries(types.map(t => [t, 1])), ...fields };
  const negativeZero = "\u0000-0\u0000";
  const text = JSON.stringify(document, (key, v) => Object.is(v, -0) ? negativeZero : v, 2);
  return text.split(JSON.stringify(negativeZero)).join("-0") + "\n";
}

// Every theme, instrument, percussion voice and room as its record's JSON form in <folder>/audio/…, and
// the theme order (Q21; Docs/Assets.md, step 6). The packer packs the folder into audio.pak.
function writeData(data, folder) {
  const files = new Map();
  const add = (sub, name, write, object, where) => {
    const file = path.join(folder, "audio", sub, pathName(name, where) + ".json");
    if (files.has(file)) throw new Error(`${where}: "${name}" and "${files.get(file)}" have one path`);
    files.set(file, name);
    fs.mkdirSync(path.dirname(file), { recursive: true });
    fs.writeFileSync(file, recordJson(write, object, where));
  };
  for (const [name, def] of Object.entries(data.instruments)) add("instruments", name, instrument, def, `instrument ${name}`);
  for (const [name, def] of Object.entries(data.percussion)) add("percussion", name, percussion, def, `percussion ${name}`);
  for (const [name, def] of Object.entries(data.rooms)) add("rooms", name, room, def, `room ${name}`);
  for (const spec of data.themes) add("themes", spec.id, theme, spec, `theme ${spec.id}`);
  fs.writeFileSync(path.join(folder, "audio", "theme-order.json"),
    recordJson(themeOrder, { themes: data.themes.map(t => value.str(t.id, "theme id")) }, "theme order"));
  return files.size + 1;
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
    const count = writeData(result.data, dataOut);
    console.log("wrote", count, "records' JSON forms into", dataOut);
  }
  if (referencesOut) {
    fs.writeFileSync(path.join(referencesOut, "music-scores.json"), JSON.stringify(result.scores, null, 1) + "\n");
    fs.writeFileSync(path.join(referencesOut, "music-logs.json"), JSON.stringify(result.logs, null, 1) + "\n");
    console.log("wrote", path.join(referencesOut, "music-scores.json"), "and music-logs.json");
  }
}

module.exports = { run };

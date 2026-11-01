"use strict";
// V8's math reference (Docs/Audio.md, step 6): Math.exp and ** as Chromium 152's V8 15.2 computes them,
// run by Electron as Node (the same V8; ** reaches the system's glibc pow through std::pow), over the
// argument families of the engine's Tests/.../Audio/JsMathTests.cs (MathArguments), drawn from
// mulberry32 in the same order: each family's FNV-1a hash of the results' bits (NaN as one canonical
// NaN) and its first 8 results. Prints JSON. Run by Tools/Scripts/make-v8-math-reference.sh.
const COUNT = 1 << 22;
let state = 0;
const next = () => {
  state = (state + 0x6D2B79F5) >>> 0;
  let t = state;
  t = Math.imul(t ^ (t >>> 15), t | 1);
  t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
  return (t ^ (t >>> 14)) >>> 0;
};
const uniform = () => next() / 4294967296;
const view = new DataView(new ArrayBuffer(8));
const fromWords = (hi, lo) => { view.setUint32(4, hi, true); view.setUint32(0, lo, true); return view.getFloat64(0, true); };
const randomBits = () => { const hi = next(); const lo = next(); return fromWords(hi, lo); };

class Hash {
  constructor() { this.hi = 0xcbf29ce4; this.lo = 0x84222325; this.first = []; }
  add(result) {
    let lo, hi;
    if (Number.isNaN(result)) { hi = 0x7ff80000; lo = 0; }
    else { view.setFloat64(0, result, true); lo = view.getUint32(0, true); hi = view.getUint32(4, true); }
    if (this.first.length < 8) this.first.push(hi.toString(16).padStart(8, "0") + lo.toString(16).padStart(8, "0"));
    for (let i = 0; i < 8; i++) {
      const byte = i < 4 ? (lo >>> (8 * i)) & 0xff : (hi >>> (8 * (i - 4))) & 0xff;
      this.lo = (this.lo ^ byte) >>> 0;
      // × 0x100000001b3 mod 2^64: × 0x1b3 plus the low word shifted up by 40 bits.
      const t = this.lo * 0x1b3;
      const carry = Math.floor(t / 4294967296);
      const hiNext = (Math.imul(this.hi, 0x1b3) >>> 0) + carry + ((this.lo << 8) >>> 0);
      this.lo = t >>> 0;
      this.hi = hiNext >>> 0;
    }
  }
  json() { return { first: this.first, hash: this.hi.toString(16).padStart(8, "0") + this.lo.toString(16).padStart(8, "0") }; }
}

const specialX = [0, -0, Infinity, -Infinity, NaN, 1, -1, 2, -2, 0.5, 5e-324, -5e-324, 2.2250738585072014e-308,
  1.7976931348623157e308, -1.7976931348623157e308, fromWords(0x3fefffff, 0xffffffff), fromWords(0x3ff00000, 1), 10, -10, 3];
const specialY = [0, -0, Infinity, -Infinity, NaN, 0.5, -0.5, 1, -1, 2, -2, 3, -3, fromWords(0x3b900000, 0), fromWords(0xbb900000, 0),
  fromWords(0x44500000, 0), fromWords(0xc4500000, 0), 1075, -1075, 0.35, 1e-300];
const special = list => { const index = next() % (list.length + 4); return index < list.length ? list[index] : randomBits(); };

const powFamilies = ["random", "nearOne", "negative", "special", "composer"];
const pow = {};
powFamilies.forEach((name, family) => {
  state = ((family + 1) * 7919) >>> 0;
  const hash = new Hash();
  for (let i = 0; i < COUNT; i++) {
    let x, y;
    if (family === 0) {
      const hi = next() & 0x7fefffff, lo = next();
      x = fromWords(hi >>> 0, lo);
      y = (uniform() * 2 - 1) * 8;
    } else if (family === 1) {
      x = 0.5 + uniform() * 1.5;
      y = (uniform() * 2 - 1) * 2048;
    } else if (family === 2) {
      x = -(1e-3 + uniform() * 1e3);
      const coin = uniform(), u = uniform();
      y = coin < 0.5 ? Math.floor(u * 81) - 40 : (u * 2 - 1) * 40;
    } else if (family === 3) {
      x = special(specialX);
      y = special(specialY);
    } else {
      const m = (next() % 700) - 300;
      if (i % 3 === 0) { x = 2; y = (m - 69) / 12; }
      else if (i % 3 === 1) { x = (20 + uniform() * 20000) / 330; y = 0.35; }
      else { const d = 0.3 + uniform() * 4.7, a = 20 + uniform() * 20000; x = 0.001; y = 1 / (d * a); }
    }
    hash.add(x ** y);
  }
  pow[name] = hash.json();
});

const expFamilies = ["range", "bits", "small", "subnormal", "rooms"];
const exp = {};
expFamilies.forEach((name, family) => {
  const hash = new Hash();
  if (name === "rooms") {
    for (const rate of [44100, 48000])
      for (const seconds of [1.4, 2.4, 1.8, 1.6, 4.2, 3.4, 4.8]) {
        const length = Math.ceil(seconds * rate);
        for (let i = 0; i < length; i++) { const t = i / rate, progress = t / seconds; hash.add(Math.exp(-6.9 * progress)); }
      }
  } else {
    state = ((family + 1) * 104729) >>> 0;
    for (let i = 0; i < COUNT; i++) {
      let x;
      if (family === 0) x = -745 + uniform() * 1455;
      else if (family === 1) x = randomBits();
      else if (family === 2) { const e = next() % 61; x = uniform() * 2 - 1; for (let k = 0; k < e; k++) x *= 0.5; }
      else x = -745.2 + uniform() * 37;
      hash.add(Math.exp(x));
    }
  }
  exp[name] = hash.json();
});

process.stdout.write(JSON.stringify({ v8: process.versions.v8, exp, pow }) + "\n");

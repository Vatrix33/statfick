/* Remastered region and battle themes, performed by music-composer.js.

   Notation
   - Melodies are scale degrees of the theme's mode: "5/2" is the fifth for two
     grid steps, "b3'" a flat third an octave up, "7," a seventh an octave down,
     "_" a rest, "-" ties into the previous note, "!" accents. Bars are split by "|".
   - Chords are numerals in the mode ("1", "6", "5M" for a major fifth, "b2"
     for a borrowed flat second, "4m7"). "." holds a chord, "%" repeats a bar.
   - Pattern strings give one character per grid step: chord tones 1 3 5 7 8,
     T (upper third), F (upper fifth), L (low root), P (low fifth), "-" holds,
     "." rests. Drum patterns use x, X (accent), o (ghost), r (roll), f (flam).
   - meter [beats, sub]: 6/8 is [2, 3], 3/4 is [3, 2], 4/4 is [4, 2] or [4, 4].

   Every battle outside a boss fight plays the shared battle theme in the
   region's own arrangement: key, mode, tempo, instruments, groove and a quote
   of the region's melody all change, the battle's shape does not. */
(() => {
  "use strict";

  /* ---------------- Region themes ---------------- */
  const regions = [];
  const region = spec => regions.push(spec);

  region({
    id: "alder-vale", title: "Alder Vale · Hearth and Hollow",
    tempo: 72, meter: [2, 3], tonic: 62, mode: "dorian", room: "hall", gain: 1,
    sections: {
      intro: {
        chords: "1 | 7 | 4 | 1", dyn: 0.7,
        layers: [
          { t: "arp", i: "lute", o: -1, v: 0.75, p: "158T85" },
          { t: "drone", i: "drone", o: -2, v: 0.6, deg: ["1", "5"] },
          { t: "tex", kind: "wind", v: 0.7 },
        ],
      },
      A: {
        chords: "1 | 7 | 4 | 1 | 1 | 3 | 7 | 5",
        mel: "5/2 6/1 5/2 4/1 | 2/3 7,/2 1/1 | 1/2 6,/1 4,/2 5,/1 | 3/2 2/1 1/3 | 5/2 1'/1 7/2 5/1 | 7/3 5/2 3/1 | 4/2 2/1 7,/2 2/1 | 5/6",
        layers: [
          { t: "mel", i: "fiddle", v: 0.85 },
          { t: "arp", i: "lute", o: -1, v: 0.7, p: "158T85" },
          { t: "bass", i: "cellos", o: -2, v: 0.6, p: "1--5--" },
          { t: "pad", i: "strings", o: 0, v: 0.35, n: 3 },
          { t: "drums", v: 0.7, p: { frame: "x..o.." } },
        ],
      },
      B: {
        chords: "3 | 7 | 1 | 5 | 3 | 4 | 5 | 1", dyn: 1.05,
        mel: "7/2 1'/1 7/2 5/1 | 4/3 2/2 4/1 | 3/2 5/1 1'/3 | 7/2 6/1 5/3 | 5/2 7/1 3'/3 | 1'/2 6/1 4/2 6/1 | 7/3 6/2 5/1 | 1'/3 _/3",
        layers: [
          { t: "mel", i: "flute", o: 0, v: 0.85, harm: { i: "fiddle", steps: -2, v: 0.4 } },
          { t: "arp", i: "harp", o: 0, v: 0.6, p: "15T8T5" },
          { t: "bass", i: "cellos", o: -2, v: 0.6, p: "1--5-1" },
          { t: "pad", i: "strings", o: 0, v: 0.45, n: 3 },
          { t: "drums", v: 0.8, p: { frame: "X..x.o", shaker: ".x..x." } },
        ],
      },
      A2: {
        like: "A", dyn: 1.12,
        layers: [
          { t: "mel", i: "fiddle", v: 0.9, dbl: [{ i: "whistle", o: 1, v: 0.35 }] },
          { t: "arp", i: "lute", o: -1, v: 0.75, p: "158T85" },
          { t: "arp", i: "harp", o: 1, v: 0.35, p: "..5..8" },
          { t: "bass", i: "cellos", o: -2, v: 0.7, p: "1--5--" },
          { t: "pad", i: "strings", o: 0, v: 0.5, n: 3 },
          { t: "drums", v: 0.85, p: { frame: "X..x.o", shaker: ".xx.xx" } },
        ],
      },
      bridge: {
        chords: "b6 | 4 | 7 | 5", dyn: 0.75, dynTo: 0.6,
        layers: [
          { t: "pad", i: "strings", o: 0, v: 0.55, n: 3 },
          { t: "arp", i: "harp", o: 0, v: 0.5, p: "1.5.8." },
          { t: "hit", i: "toll", deg: "1", o: -1, v: 0.6, bars: [0] },
          { t: "drone", i: "drone", o: -2, v: 0.5, deg: ["1"] },
        ],
      },
    },
    form: ["intro", "A", "B", "A2", "bridge"], loop: 1,
    motif: "5/2 6/1 5/2 4/1 | 2/3 7,/2 1/1",
  });

  region({
    id: "stoneveil-foothills", title: "Stoneveil · Cairns of the Ascent",
    tempo: 74, meter: [4, 2], tonic: 57, mode: "aeolian", room: "hall",
    sections: {
      intro: {
        chords: "1 | 6 | 7 | 1", dyn: 0.7,
        layers: [
          { t: "drone", i: "drone", o: -1, v: 0.6, deg: ["1", "5"] },
          { t: "arp", i: "harp", o: 0, v: 0.55, p: "1538T853" },
          { t: "tex", kind: "wind", v: 0.8 },
          { t: "hit", i: "timp", v: 0.6, bars: [0, 2] },
        ],
      },
      A: {
        chords: "1 | 6 | 3 | 7 | 1 | 4 | 6 7 | 1",
        mel: "1/3 2/1 3/2 5/2 | 6/4 1'/2 6/2 | 5/4 3/2 5/2 | 4/2 2/2 7,/4 | 1'/3 7/1 1'/2 5/2 | 6/4 4/2 6/2 | 1'/2 6/2 7/2 5/2 | 1'/8",
        layers: [
          { t: "mel", i: "horns", o: 0, v: 0.9 },
          { t: "arp", i: "harp", o: 0, v: 0.5, p: "1538T853" },
          { t: "bass", i: "cellos", o: -2, v: 0.6, p: "1---5---" },
          { t: "pad", i: "choir", o: 0, v: 0.4, n: 3 },
          { t: "drums", v: 0.6, p: { timp: "x.......", frame: "....x..o" } },
        ],
      },
      B: {
        chords: "6 | 3 | 7 | 1 | 6 | 3 | 4 | 5", dyn: 1.08,
        mel: "3'/4 1'/2 6/2 | 5'/4 3'/2 1'/2 | 4'/3 3'/1 2'/2 7/2 | 1'/6 _/2 | 3'/2 4'/2 3'/2 1'/2 | 5'/4 4'/2 3'/2 | 4'/4 3'/2 2'/2 | 7/6 5/2",
        layers: [
          { t: "mel", i: "violins", o: 0, v: 0.85, dbl: [{ i: "horns", o: -1, v: 0.35 }] },
          { t: "arp", i: "harp", o: 0, v: 0.5, p: "15T8T5T8" },
          { t: "bass", i: "cellos", o: -2, v: 0.65, p: "1---1-5-" },
          { t: "pad", i: "choir", o: 0, v: 0.55, n: 3 },
          { t: "drums", v: 0.75, p: { timp: "x...x...", war: "x......." } },
        ],
      },
      A2: {
        like: "A", dyn: 1.12,
        layers: [
          { t: "mel", i: "horns", o: 0, v: 0.95, dbl: [{ i: "trumpet", o: 1, v: 0.3 }] },
          { t: "arp", i: "harp", o: 0, v: 0.55, p: "1538T853" },
          { t: "bass", i: "cellos", o: -2, v: 0.7, p: "1---5---" },
          { t: "pad", i: "strings", o: 0, v: 0.5, n: 3 },
          { t: "pad", i: "choir", o: 0, v: 0.4, n: 3 },
          { t: "drums", v: 0.8, p: { timp: "x...x...", war: "x.......", snare: "......x." } },
        ],
      },
      bridge: {
        chords: "6 | 4 | 6 | 7", dyn: 0.7, dynTo: 0.55,
        layers: [
          { t: "pad", i: "choir", o: 0, v: 0.6, n: 3 },
          { t: "drone", i: "drone", o: -1, v: 0.5, deg: ["1"] },
          { t: "tex", kind: "wind", v: 0.9 },
          { t: "hit", i: "toll", deg: "1", o: -1, v: 0.6, bars: [0] },
        ],
      },
    },
    form: ["intro", "A", "B", "A2", "bridge"], loop: 1,
  });

  region({
    id: "high-stoneveil", title: "High Stoneveil · Where the Sky Thins",
    tempo: 62, meter: [3, 2], tonic: 59, mode: "aeolian", room: "mountain",
    sections: {
      intro: {
        chords: "1 | 6 | 3 | 7", dyn: 0.65,
        layers: [
          { t: "drone", i: "drone", o: -2, v: 0.6, deg: ["1", "5"] },
          { t: "arp", i: "celesta", o: 1, v: 0.45, p: "1.5.8." },
          { t: "tex", kind: "wind", v: 1.1 },
        ],
      },
      A: {
        chords: "1 | 6 | 3 | 7 | 1 | 4 | 6 | 5",
        mel: "5/3 1'/2 7/1 | 6/4 3/2 | 5/2 3/2 1/2 | 2/4 7,/2 | 1'/3 2'/2 1'/1 | 7/2 6/2 4/2 | 3/4 7,/2 | 5/6",
        layers: [
          { t: "mel", i: "flute", o: 0, v: 0.9 },
          { t: "arp", i: "celesta", o: 1, v: 0.4, p: "1.5.8." },
          { t: "pad", i: "sopranos", o: 1, v: 0.45, n: 3 },
          { t: "drone", i: "contrabass", o: -2, v: 0.5, deg: ["1"], every: 12 },
          { t: "tex", kind: "wind", v: 0.7 },
        ],
      },
      B: {
        chords: "6 | 7 | 1 | 1 | 6 | 7 | 5 | 1", dyn: 1.05,
        mel: "6/2 1'/2 3'/2 | 2'/4 7/2 | 1'/6 | _/2 5/2 3/2 | 3'/3 2'/2 1'/1 | 7/4 5/2 | 5/2 7/2 2'/2 | 1'/6",
        layers: [
          { t: "mel", i: "violins", o: 0, v: 0.85, dbl: [{ i: "flute", o: 1, v: 0.3 }] },
          { t: "arp", i: "glass", o: 1, v: 0.35, p: "1....." },
          { t: "pad", i: "sopranos", o: 1, v: 0.55, n: 3 },
          { t: "bass", i: "cellos", o: -2, v: 0.5, p: "1-----" },
          { t: "hit", i: "toll", deg: "1", o: -1, v: 0.45, bars: [0, 4] },
        ],
      },
      A2: {
        like: "A", dyn: 1.1,
        layers: [
          { t: "mel", i: "horns", o: 0, v: 0.85, dbl: [{ i: "flute", o: 1, v: 0.4 }] },
          { t: "arp", i: "celesta", o: 1, v: 0.45, p: "1.5.8." },
          { t: "pad", i: "sopranos", o: 1, v: 0.55, n: 3 },
          { t: "pad", i: "strings", o: 0, v: 0.35, n: 3 },
          { t: "drone", i: "contrabass", o: -2, v: 0.55, deg: ["1"], every: 12 },
          { t: "drums", v: 0.5, p: { war: "x.....", timp: "......" } },
        ],
      },
      bridge: {
        chords: "4 | 6 | 4 | 5", dyn: 0.6, dynTo: 0.45,
        layers: [
          { t: "pad", i: "sopranos", o: 1, v: 0.5, n: 3 },
          { t: "arp", i: "glass", o: 1, v: 0.35, p: "1....5" },
          { t: "tex", kind: "wind", v: 1.2 },
          { t: "hit", i: "gong", v: 0.5, bars: [0] },
        ],
      },
    },
    form: ["intro", "A", "B", "A2", "bridge"], loop: 1,
  });

  region({
    id: "stoneveil-summit", title: "Stoneveil Summit · Roof of the World",
    tempo: 56, meter: [4, 2], tonic: 52, mode: "phrygian", room: "mountain",
    sections: {
      intro: {
        chords: "1 | 2 | 1 | 7", dyn: 0.7,
        layers: [
          { t: "pad", i: "choirOo", o: 0, v: 0.55, n: 3 },
          { t: "drone", i: "drone", o: -1, v: 0.6, deg: ["1", "5"] },
          { t: "tex", kind: "wind", v: 1.3 },
          { t: "hit", i: "gong", v: 0.7, bars: [0] },
        ],
      },
      A: {
        chords: "1 | 2 | 1 | 7 | 6 | 2 | 7 | 1",
        mel: "1/5 2/1 1/2 | 3/4 2/4 | 5/6 3/2 | 4/4 3/2 2/2 | 3/4 1/4 | 6/3 5/1 4/4 | 4/6 2/2 | 1/8",
        layers: [
          { t: "mel", i: "horns", o: 1, v: 0.9 },
          { t: "pad", i: "choirOo", o: 0, v: 0.5, n: 3 },
          { t: "drone", i: "contrabass", o: -1, v: 0.55, deg: ["1"], every: 16 },
          { t: "drums", v: 0.55, p: { war: "x.......", timp: "....x..." } },
          { t: "tex", kind: "wind", v: 0.9 },
        ],
      },
      B: {
        chords: "6 | 7 | 1 | 1 | 6 | 2 | 7 | 1", dyn: 1.12,
        mel: "3/4 6/4 | 7/4 4/4 | 5/6 7/2 | 1'/8 | 1'/4 7/2 6/2 | 6/4 4/4 | 4/3 3/1 2/4 | 1/8",
        layers: [
          { t: "mel", i: "trombones", o: 1, v: 0.9, dbl: [{ i: "horns", o: 1, v: 0.5 }] },
          { t: "pad", i: "choir", o: 0, v: 0.6, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.6, p: "1---5---" },
          { t: "drums", v: 0.75, p: { war: "x...x...", timp: "r......." } },
          { t: "hit", i: "gong", v: 0.55, bars: [0, 4] },
        ],
      },
      A2: {
        like: "A", dyn: 1.15,
        layers: [
          { t: "mel", i: "horns", o: 1, v: 0.95, dbl: [{ i: "trombones", o: 0, v: 0.45 }] },
          { t: "pad", i: "choir", o: 0, v: 0.55, n: 3 },
          { t: "pad", i: "sopranos", o: 1, v: 0.35, n: 3 },
          { t: "drone", i: "contrabass", o: -1, v: 0.6, deg: ["1"], every: 16 },
          { t: "drums", v: 0.85, p: { war: "x...x...", timp: "....x.xr" } },
        ],
      },
      bridge: {
        chords: "2 | 1 | 2 | 1", dyn: 0.6, dynTo: 0.45,
        layers: [
          { t: "pad", i: "choirOo", o: 0, v: 0.55, n: 3 },
          { t: "tex", kind: "wind", v: 1.4 },
          { t: "hit", i: "gong", v: 0.6, bars: [0] },
        ],
      },
    },
    form: ["intro", "A", "B", "A2", "bridge"], loop: 1,
  });

  region({
    id: "verdant-reach", title: "Verdant Reach · Under Elder Boughs",
    tempo: 88, meter: [3, 2], tonic: 64, mode: "dorian", room: "forest",
    sections: {
      intro: {
        chords: "1 | 4 | 1 | 7", dyn: 0.7,
        layers: [
          { t: "arp", i: "harp", o: -1, v: 0.6, p: "15T8T5" },
          { t: "arp", i: "kalimba", o: 1, v: 0.35, p: "8....5" },
          { t: "tex", kind: "leaves", v: 0.8 },
        ],
      },
      A: {
        chords: "1 | 4 | 1 | 7 | 3 | 7 | 4 | 5",
        mel: "5/2 1'/2 7/1 5/1 | 6/3 5/1 4/2 | 3/2 5/2 1'/2 | 2'/3 1'/1 7/2 | 1'/2 7/2 5/2 | 7/4 4/2 | 6/2 4/2 3/2 | 2/6",
        layers: [
          { t: "mel", i: "flute", o: 0, v: 0.9 },
          { t: "arp", i: "harp", o: -1, v: 0.55, p: "15T8T5" },
          { t: "bass", i: "cellos", o: -2, v: 0.5, p: "1-----" },
          { t: "pad", i: "strings", o: -1, v: 0.4, n: 3 },
          { t: "drums", v: 0.45, p: { shaker: ".x.x.x" } },
        ],
      },
      B: {
        chords: "3 | 7 | 4 | 1 | 3 | 7 | 4 | 4", dyn: 1.05,
        mel: "3'/3 2'/1 1'/2 | 2'/4 1'/2 | 1'/2 6/2 4/2 | 5/6 | 5/2 7/2 1'/2 | 2'/3 3'/1 2'/2 | 1'/4 6/2 | 4/6",
        layers: [
          { t: "mel", i: "oboe", o: 0, v: 0.9, harm: { i: "flute", steps: -2, v: 0.35 } },
          { t: "arp", i: "kalimba", o: 0, v: 0.45, p: "1.5.8." },
          { t: "arp", i: "harp", o: -1, v: 0.45, p: "1..5.." },
          { t: "bass", i: "pizz", o: -2, v: 0.6, p: "1...5." },
          { t: "pad", i: "strings", o: -1, v: 0.45, n: 3 },
          { t: "drums", v: 0.5, p: { shaker: ".x.x.x", frame: "x....." } },
        ],
      },
      A2: {
        like: "A", dyn: 1.1,
        layers: [
          { t: "mel", i: "flute", o: 0, v: 0.9, dbl: [{ i: "fiddle", o: -1, v: 0.4 }] },
          { t: "arp", i: "harp", o: -1, v: 0.55, p: "15T8T5" },
          { t: "arp", i: "kalimba", o: 1, v: 0.35, p: "....8." },
          { t: "bass", i: "cellos", o: -2, v: 0.55, p: "1-----" },
          { t: "pad", i: "strings", o: -1, v: 0.5, n: 3 },
          { t: "pad", i: "choir", o: 0, v: 0.3, n: 3 },
          { t: "drums", v: 0.55, p: { shaker: ".x.x.x", frame: "x..o.." } },
        ],
      },
      bridge: {
        chords: "4 | 3 | 4 | 5", dyn: 0.65, dynTo: 0.5,
        layers: [
          { t: "pad", i: "choir", o: 0, v: 0.45, n: 3 },
          { t: "arp", i: "kalimba", o: 1, v: 0.4, p: "1....5" },
          { t: "tex", kind: "leaves", v: 0.9 },
        ],
      },
    },
    form: ["intro", "A", "B", "A2", "bridge"], loop: 1,
  });

  region({
    id: "stormwake-hills", title: "Stormwake · Rain on the Ramparts",
    tempo: 84, meter: [4, 4], tonic: 60, mode: "aeolian", room: "cathedral",
    sections: {
      intro: {
        chords: "1 | 6 | 7 | 5", dyn: 0.7,
        layers: [
          { t: "pad", i: "tremolo", o: 0, v: 0.5, n: 3 },
          { t: "tex", kind: "rain", v: 1.1 },
          { t: "hit", i: "thunder", v: 0.8, bars: [1] },
          { t: "drone", i: "drone", o: -2, v: 0.5, deg: ["1"] },
        ],
      },
      A: {
        chords: "1 | 6 | 7 | 5 | 1 | 4 | 6 | 7",
        mel: "5/6 4/2 3/8 | 1/8 3/8 | 2/6 4/2 7,/8 | 2/8 7,/8 | 1'/6 7/2 5/8 | 6/8 4/8 | 3/6 1/2 6,/8 | 2/12 _/4",
        layers: [
          { t: "mel", i: "trombones", o: 0, v: 0.85, dbl: [{ i: "horns", o: 0, v: 0.5 }] },
          { t: "pulse", i: "spiccato", o: 0, v: 0.45, p: "1.5.3.5.1.5.3.5." },
          { t: "pad", i: "tremolo", o: 0, v: 0.4, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.6, p: "1-------1---5---" },
          { t: "drums", v: 0.6, p: { timp: "x.......x.......", muffled: "....x.......x..." } },
          { t: "tex", kind: "rain", v: 0.8 },
        ],
      },
      B: {
        chords: "6 | 3 | 7 | 1 | 6 | 3 | 4 | 5", dyn: 1.08,
        mel: "3'/8 1'/8 | 7/6 5/2 3/8 | 4/8 2/8 | 5/8 3/8 | 1'/6 3'/2 1'/8 | 7/8 5/8 | 6/6 5/2 4/8 | 2/8 7,/8",
        layers: [
          { t: "mel", i: "violins", o: 0, v: 0.85, dbl: [{ i: "cellos", o: -1, v: 0.45 }] },
          { t: "pad", i: "organ", o: 0, v: 0.5, n: 3 },
          { t: "pad", i: "choir", o: 0, v: 0.35, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.6, p: "1-------5-------" },
          { t: "drums", v: 0.7, p: { timp: "x.......x.....rr" } },
          { t: "hit", i: "toll", deg: "1", o: -1, v: 0.55, bars: [0, 4] },
          { t: "tex", kind: "rain", v: 0.7 },
        ],
      },
      A2: {
        like: "A", dyn: 1.12,
        layers: [
          { t: "mel", i: "horns", o: 0, v: 0.9, dbl: [{ i: "trombones", o: -1, v: 0.5 }, { i: "violins", o: 1, v: 0.3 }] },
          { t: "pulse", i: "spiccato", o: 0, v: 0.5, p: "1.5.3.5.1.5.3.5." },
          { t: "pad", i: "organ", o: 0, v: 0.4, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.65, p: "1-------1---5---" },
          { t: "drums", v: 0.75, p: { timp: "x.......x.......", muffled: "....x.......x.x.", war: "x..............." } },
          { t: "hit", i: "thunder", v: 0.7, bars: [3] },
          { t: "tex", kind: "rain", v: 0.8 },
        ],
      },
      bridge: {
        chords: "4 | 6 | 4 | 5", dyn: 0.65, dynTo: 0.5,
        layers: [
          { t: "pad", i: "organ", o: 0, v: 0.5, n: 3 },
          { t: "hit", i: "toll", deg: "1", o: -1, v: 0.55, bars: [0, 2] },
          { t: "tex", kind: "rain", v: 1.1 },
          { t: "hit", i: "thunder", v: 0.6, bars: [2] },
        ],
      },
    },
    form: ["intro", "A", "B", "A2", "bridge"], loop: 1,
  });

  region({
    id: "gloamfen-marsh", title: "Gloamfen · Lanterns in the Mire",
    tempo: 60, meter: [3, 2], tonic: 53, mode: "aeolian", room: "cavern",
    sections: {
      intro: {
        chords: "1 | 6 | 4 | 5", dyn: 0.65,
        layers: [
          { t: "drone", i: "drone", o: -1, v: 0.6, deg: ["1", "5"] },
          { t: "pad", i: "glass", o: 1, v: 0.45, n: 3 },
          { t: "drips", i: "kalimba", o: 1, v: 0.5, chance: 0.12 },
          { t: "tex", kind: "wind", v: 0.7 },
        ],
      },
      A: {
        chords: "1 | 6 | 4 | 5 | 1 | 7 | 6 | 5",
        mel: "1/3 3/2 5/1 | 6/4 1/2 | 4/3 6/2 4/1 | 5/4 7/2 | 1'/3 7/1 5/2 | 7/4 5/2 | 4/2 3/2 1/2 | 5/6",
        layers: [
          { t: "mel", i: "clarinet", o: 1, v: 0.9 },
          { t: "arp", i: "pizz", o: 0, v: 0.5, p: "1..5.3" },
          { t: "pad", i: "glass", o: 1, v: 0.35, n: 3 },
          { t: "drone", i: "contrabass", o: -1, v: 0.5, deg: ["1"], every: 12 },
          { t: "drums", v: 0.45, p: { muffled: "x....." } },
          { t: "drips", i: "kalimba", o: 1, v: 0.4, chance: 0.08 },
        ],
      },
      B: {
        chords: "6 | 3 | 7 | 1 | 6 | 3 | 4 | 5", dyn: 1.05,
        mel: "1'/3 6/2 4/1 | 3'/4 1'/2 | 7/2 5/2 4/2 | 5/6 | 6/2 1'/2 3'/2 | 1'/4 7/2 | 6/3 4/3 | 5/6",
        layers: [
          { t: "mel", i: "oboe", o: 1, v: 0.85, harm: { i: "clarinet", steps: -2, v: 0.4 } },
          { t: "pad", i: "choirOo", o: 0, v: 0.5, n: 3 },
          { t: "arp", i: "pizz", o: 0, v: 0.45, p: "1..5.." },
          { t: "bass", i: "contrabass", o: -2, v: 0.55, p: "1-----" },
          { t: "drips", i: "kalimba", o: 1, v: 0.4, chance: 0.08 },
        ],
      },
      A2: {
        like: "A", dyn: 1.1,
        layers: [
          { t: "mel", i: "clarinet", o: 1, v: 0.9, dbl: [{ i: "oboe", o: 1, v: 0.3 }] },
          { t: "arp", i: "pizz", o: 0, v: 0.5, p: "1..5.3" },
          { t: "pad", i: "glass", o: 1, v: 0.35, n: 3 },
          { t: "pad", i: "choirOo", o: 0, v: 0.4, n: 3 },
          { t: "drone", i: "contrabass", o: -1, v: 0.55, deg: ["1"], every: 12 },
          { t: "drums", v: 0.5, p: { muffled: "x.....", tom: "....o." } },
        ],
      },
      bridge: {
        chords: "6 | 4 | 6 | 5", dyn: 0.6, dynTo: 0.45,
        layers: [
          { t: "pad", i: "glass", o: 1, v: 0.45, n: 3 },
          { t: "drone", i: "drone", o: -1, v: 0.5, deg: ["1"] },
          { t: "drips", i: "kalimba", o: 1, v: 0.5, chance: 0.14 },
          { t: "tex", kind: "wind", v: 0.8 },
        ],
      },
    },
    form: ["intro", "A", "B", "A2", "bridge"], loop: 1,
  });

  region({
    id: "verdigris-jungle", title: "Verdigris · The Emerald Vault",
    tempo: 100, meter: [4, 4], tonic: 55, mode: "dorian", room: "forest",
    sections: {
      intro: {
        chords: "1 | 4 | 1 | 7", dyn: 0.75,
        layers: [
          { t: "pulse", i: "marimba", o: 0, v: 0.5, p: "1.5.8.5.1.5.8.5." },
          { t: "drums", v: 0.6, p: { log: "x..x..x.x.x..x..", shaker: "x.x.x.x.x.x.x.x." } },
          { t: "tex", kind: "leaves", v: 0.9 },
        ],
      },
      A: {
        chords: "1 | 4 | 1 | 7 | 1 | 4 | 3 | 5",
        mel: "5/6 4/2 3/4 1/4 | 6/6 5/2 4/8 | 3/4 5/4 1'/8 | 7/6 6/2 5/8 | 5/4 1'/4 5/4 3/4 | 4/6 6/2 1'/8 | 1'/8 7/8 | 5/16",
        layers: [
          { t: "mel", i: "duduk", o: 1, v: 0.9 },
          { t: "pulse", i: "marimba", o: 0, v: 0.45, p: "1.5.8.5.1.5.8.5." },
          { t: "bass", i: "pizz", o: -1, v: 0.6, p: "1.....1.5......." },
          { t: "pad", i: "strings", o: 0, v: 0.35, n: 3 },
          { t: "drums", v: 0.6, p: { log: "x..x..x.x.x..x..", shaker: "x.x.x.x.x.x.x.x.", clack: "....x.......x..." } },
        ],
      },
      B: {
        chords: "3 | 4 | 5 | 1 | 3 | 4 | 7 | 1", dyn: 1.05,
        mel: "3'/8 1'/8 | 1'/6 6/2 4/8 | 5/4 7/4 2'/8 | 1'/16 | 7/4 1'/4 3'/8 | 2'/6 1'/2 6/8 | 1'/8 7/8 | 5/16",
        layers: [
          { t: "mel", i: "flute", o: 1, v: 0.85, dbl: [{ i: "duduk", o: 0, v: 0.4 }] },
          { t: "pad", i: "choir", o: 0, v: 0.5, n: 3 },
          { t: "arp", i: "kalimba", o: 1, v: 0.4, p: "8...5...T...5..." },
          { t: "bass", i: "pizz", o: -1, v: 0.6, p: "1.....1.5......." },
          { t: "drums", v: 0.65, p: { log: "x..x..x.x.x..x..", shaker: "x.x.x.x.x.x.x.x.", tom: "x.......x......." } },
        ],
      },
      A2: {
        like: "A", dyn: 1.12,
        layers: [
          { t: "mel", i: "duduk", o: 1, v: 0.9, dbl: [{ i: "flute", o: 1, v: 0.35 }] },
          { t: "pulse", i: "marimba", o: 0, v: 0.5, p: "1.5.8.5.1.5.8.5." },
          { t: "bass", i: "pizz", o: -1, v: 0.65, p: "1.....1.5......." },
          { t: "pad", i: "choir", o: 0, v: 0.4, n: 3 },
          { t: "drums", v: 0.7, p: { log: "x..x..x.x.x..x..", shaker: "xxx.xxx.xxx.xxx.", clack: "....x.......x.x.", taiko: "x.......x......." } },
        ],
      },
      bridge: {
        chords: "4 | 1 | 4 | 5", dyn: 0.7, dynTo: 0.55,
        layers: [
          { t: "drums", v: 0.6, p: { log: "x..x..x.x.x..x..", shaker: "x.x.x.x.x.x.x.x." } },
          { t: "pad", i: "choir", o: 0, v: 0.45, n: 3 },
          { t: "tex", kind: "leaves", v: 1 },
        ],
      },
    },
    form: ["intro", "A", "B", "A2", "bridge"], loop: 1,
  });

  region({
    id: "mirrormere-basin", title: "Mirrormere · The Still Water",
    tempo: 72, meter: [3, 4], tonic: 65, mode: "lydian", room: "hall",
    sections: {
      intro: {
        chords: "6 | 5 | 1 | 2", dyn: 0.65,
        layers: [
          { t: "arp", i: "harp", o: -1, v: 0.55, p: "158T8515T8T5" },
          { t: "tex", kind: "surf", v: 0.8 },
          { t: "pad", i: "pad", o: 0, v: 0.45, n: 3 },
        ],
      },
      A: {
        chords: "6 | 5 | 1 | 2 | 6 | 3 | 5 | 1",
        mel: "6/6 5/3 3/3 | 5/6 7/3 5/3 | 3/6 1/3 5/3 | 2/9 4/3 | 1'/6 6/3 3/3 | 7/6 5/3 3/3 | 5/6 2/3 7,/3 | 1/12",
        layers: [
          { t: "mel", i: "flute", o: 0, v: 0.9 },
          { t: "arp", i: "harp", o: -1, v: 0.5, p: "158T8515T8T5" },
          { t: "pad", i: "strings", o: -1, v: 0.4, n: 3 },
          { t: "bass", i: "cellos", o: -2, v: 0.5, p: "1-----------" },
          { t: "tex", kind: "surf", v: 0.5 },
        ],
      },
      B: {
        chords: "2 | 6 | 3 | 5 | 2 | 6 | 2 | 5", dyn: 1.05,
        mel: "2'/6 7/3 4/3 | 1'/6 6/6 | 7/6 5/3 3/3 | 5/12 | 4/6 2/3 4/3 | 6/6 1'/6 | 7/3 6/3 4/6 | 5/12",
        layers: [
          { t: "mel", i: "fiddle", o: 0, v: 0.85, harm: { i: "flute", steps: 2, v: 0.3 } },
          { t: "arp", i: "celesta", o: 0, v: 0.4, p: "1..5..8..5.." },
          { t: "arp", i: "harp", o: -1, v: 0.45, p: "1.5.8.T.8.5." },
          { t: "pad", i: "strings", o: -1, v: 0.45, n: 3 },
          { t: "bass", i: "cellos", o: -2, v: 0.5, p: "1-----5-----" },
        ],
      },
      A2: {
        like: "A", dyn: 1.1,
        layers: [
          { t: "mel", i: "fiddle", o: 0, v: 0.9, dbl: [{ i: "flute", o: 1, v: 0.35 }] },
          { t: "arp", i: "harp", o: -1, v: 0.55, p: "158T8515T8T5" },
          { t: "arp", i: "celesta", o: 1, v: 0.3, p: "......8....." },
          { t: "pad", i: "strings", o: -1, v: 0.5, n: 3 },
          { t: "bass", i: "cellos", o: -2, v: 0.55, p: "1-----------" },
          { t: "drums", v: 0.4, p: { frame: "x..........." } },
        ],
      },
      bridge: {
        chords: "6 | 2 | 6 | 5", dyn: 0.6, dynTo: 0.45,
        layers: [
          { t: "pad", i: "pad", o: 0, v: 0.5, n: 3 },
          { t: "arp", i: "glass", o: 1, v: 0.35, p: "1.....5....." },
          { t: "tex", kind: "surf", v: 0.9 },
        ],
      },
    },
    form: ["intro", "A", "B", "A2", "bridge"], loop: 1,
  });

  region({
    id: "saffron-expanse", title: "Saffron · The Caravan's Mirage",
    tempo: 92, meter: [4, 4], tonic: 62, mode: "hijaz", room: "open",
    sections: {
      intro: {
        chords: "1 | 2 | 1 | 7", dyn: 0.7,
        layers: [
          { t: "drone", i: "drone", o: -2, v: 0.55, deg: ["1", "5"] },
          { t: "strum", i: "oud", o: -1, v: 0.55, p: "d.....d.u.d....." },
          { t: "drums", v: 0.6, p: { dum: "x.....x...x.....", tek: "...x.x...x...x.x" } },
          { t: "tex", kind: "wind", v: 0.7 },
        ],
      },
      A: {
        chords: "1 | 2 | 1 | 7 | 4 | 2 | 1 | 1",
        mel: "5/4 6/2 5/2 3/8 | 2/8 4/4 2/4 | 1/6 2/2 3/8 | 4/4 2/4 7,/8 | 4/6 6/2 1'/8 | 7/4 6/4 4/8 | 5/4 6/2 5/2 3/8 | 1/16",
        layers: [
          { t: "mel", i: "duduk", o: 0, v: 0.9 },
          { t: "strum", i: "oud", o: -1, v: 0.5, p: "d.....d.u.d....." },
          { t: "bass", i: "pizz", o: -1, v: 0.55, p: "1.....1...5....." },
          { t: "pad", i: "strings", o: 0, v: 0.35, n: 3 },
          { t: "drums", v: 0.6, p: { dum: "x.....x...x.....", tek: "...x.x...x...x.x", zill: "x..............." } },
        ],
      },
      B: {
        chords: "4 | 7 | 2 | 1 | 4 | 7 | 2 | 1", dyn: 1.06,
        mel: "1'/6 2'/2 1'/8 | 7/6 6/2 4/8 | 6/4 4/4 2/8 | 3/8 5/8 | 4'/6 3'/2 1'/8 | 2'/6 1'/2 7/8 | 6/4 7/4 6/4 4/4 | 5/12 _/4",
        layers: [
          { t: "mel", i: "oboe", o: 0, v: 0.85, dbl: [{ i: "duduk", o: -1, v: 0.45 }] },
          { t: "strum", i: "oud", o: -1, v: 0.5, p: "d.....d.u.d....." },
          { t: "pad", i: "strings", o: 0, v: 0.45, n: 3 },
          { t: "bass", i: "pizz", o: -1, v: 0.55, p: "1.....1...5....." },
          { t: "drums", v: 0.7, p: { dum: "x.....x...x...x.", tek: "...x.x.x.x.x.x.x", zill: "x.......x......." } },
        ],
      },
      A2: {
        like: "A", dyn: 1.12,
        layers: [
          { t: "mel", i: "duduk", o: 0, v: 0.9, dbl: [{ i: "oboe", o: 1, v: 0.3 }] },
          { t: "strum", i: "oud", o: -1, v: 0.55, p: "d.....d.u.d...u." },
          { t: "bass", i: "pizz", o: -1, v: 0.6, p: "1.....1...5....." },
          { t: "pad", i: "strings", o: 0, v: 0.45, n: 3 },
          { t: "drone", i: "drone", o: -2, v: 0.4, deg: ["1"] },
          { t: "drums", v: 0.75, p: { dum: "x.....x...x...x.", tek: "...x.x.x.x.x.x.x", zill: "x.......x......." } },
        ],
      },
      bridge: {
        chords: "2 | 1 | 2 | 1", dyn: 0.65, dynTo: 0.5,
        layers: [
          { t: "drone", i: "drone", o: -2, v: 0.5, deg: ["1", "5"] },
          { t: "strum", i: "oud", o: -1, v: 0.45, p: "d..............." },
          { t: "tex", kind: "wind", v: 0.9 },
          { t: "drums", v: 0.45, p: { zill: "x..............." } },
        ],
      },
    },
    form: ["intro", "A", "B", "A2", "bridge"], loop: 1,
  });

  region({
    id: "deep-desert", title: "Deep Desert · Stones Beneath the Sand",
    tempo: 60, meter: [4, 2], tonic: 60, mode: "hijaz", room: "cathedral",
    sections: {
      intro: {
        chords: "1 | 2 | 1 | 7", dyn: 0.65,
        layers: [
          { t: "pad", i: "choirOo", o: -1, v: 0.55, n: 3 },
          { t: "drone", i: "drone", o: -2, v: 0.55, deg: ["1", "5"] },
          { t: "hit", i: "gong", v: 0.6, bars: [0] },
          { t: "drums", v: 0.5, p: { frame: "x.......", zill: "....x..." } },
        ],
      },
      A: {
        chords: "1 | 2 | 1 | 7 | 4 | 2 | 7 | 1",
        mel: "5/3 6/1 5/2 3/2 | 2/4 4/2 6/2 | 3/3 2/1 1/4 | 7,/4 2/2 4/2 | 4/3 6/1 1'/4 | 6/4 4/2 2/2 | 4/4 2/2 7,/2 | 1/8",
        layers: [
          { t: "mel", i: "duduk", o: 0, v: 0.9 },
          { t: "pad", i: "choirOo", o: -1, v: 0.45, n: 3 },
          { t: "arp", i: "harp", o: -1, v: 0.4, p: "1...5..." },
          { t: "drone", i: "contrabass", o: -2, v: 0.5, deg: ["1"], every: 16 },
          { t: "drums", v: 0.55, p: { frame: "x..x....", zill: "....x..." } },
        ],
      },
      B: {
        chords: "4 | 7 | 2 | 1 | 4 | 7 | 2 | 1", dyn: 1.08,
        mel: "1'/4 6/4 | 2'/4 7/2 6/2 | 4/3 6/1 2'/4 | 1'/8 | 6/3 5/1 4/4 | 4/4 2/4 | 2/2 4/2 6/4 | 5/4 3/4",
        layers: [
          { t: "mel", i: "oboe", o: 0, v: 0.85, dbl: [{ i: "duduk", o: -1, v: 0.45 }] },
          { t: "pad", i: "choir", o: -1, v: 0.5, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.55, p: "1---5---" },
          { t: "drums", v: 0.65, p: { frame: "x..x..x.", tom: "....x...", zill: "..x...x." } },
          { t: "hit", i: "gong", v: 0.5, bars: [0, 4] },
        ],
      },
      A2: {
        like: "A", dyn: 1.12,
        layers: [
          { t: "mel", i: "duduk", o: 0, v: 0.9, dbl: [{ i: "choirOo", o: 0, v: 0.35 }] },
          { t: "pad", i: "choir", o: -1, v: 0.45, n: 3 },
          { t: "arp", i: "harp", o: -1, v: 0.45, p: "1...5.8." },
          { t: "drone", i: "contrabass", o: -2, v: 0.55, deg: ["1"], every: 16 },
          { t: "drums", v: 0.7, p: { frame: "x..x..x.", tom: "....x...", zill: "..x...x." } },
        ],
      },
      bridge: {
        chords: "2 | 1 | 2 | 1", dyn: 0.6, dynTo: 0.45,
        layers: [
          { t: "pad", i: "choirOo", o: -1, v: 0.5, n: 3 },
          { t: "hit", i: "gong", v: 0.6, bars: [0] },
          { t: "tex", kind: "wind", v: 0.9 },
        ],
      },
    },
    form: ["intro", "A", "B", "A2", "bridge"], loop: 1,
  });

  region({
    id: "crimson-divide", title: "Crimson Divide · Where the Sand Bleeds",
    tempo: 66, meter: [2, 3], tonic: 64, mode: "harmonic", room: "hall",
    sections: {
      intro: {
        chords: "1 | 6 | 4 | 5", dyn: 0.7,
        layers: [
          { t: "arp", i: "oud", o: -1, v: 0.55, p: "1.5.8." },
          { t: "drone", i: "drone", o: -2, v: 0.5, deg: ["1", "5"] },
          { t: "tex", kind: "wind", v: 0.8 },
          { t: "drums", v: 0.5, p: { dum: "x.....", tek: "...x.x" } },
        ],
      },
      A: {
        chords: "1 | 6 | 4 | 5 | 1 | 6 | 4 5 | 1",
        mel: "5/2 6/1 5/2 3/1 | 1/3 3/3 | 4/2 3/1 1/3 | 2/3 7,/2 5,/1 | 1'/2 7/1 1'/2 5/1 | 6/3 3/3 | 1'/2 6/1 7/3 | 1/6",
        layers: [
          { t: "mel", i: "fiddle", o: 0, v: 0.9 },
          { t: "arp", i: "oud", o: -1, v: 0.5, p: "1.5.8." },
          { t: "arp", i: "harpsichord", o: 0, v: 0.35, p: "...35." },
          { t: "bass", i: "cellos", o: -2, v: 0.5, p: "1--5--" },
          { t: "drums", v: 0.55, p: { dum: "x.....", tek: "...x.x" } },
        ],
      },
      B: {
        chords: "6 | b7 | 1 | 1 | 4 | 6 | 5 | 5", dyn: 1.08,
        mel: "3'/3 1'/2 6/1 | 2'/3 b7/2 5/1 | 1'/3 5/3 | 3/6 | 6/2 1'/1 3'/3 | 1'/3 6/3 | 7/3 2'/3 | 5/6",
        layers: [
          { t: "mel", i: "violins", o: 0, v: 0.85, harm: { i: "fiddle", steps: -2, v: 0.4 } },
          { t: "pad", i: "choir", o: 0, v: 0.5, n: 3 },
          { t: "arp", i: "harpsichord", o: 0, v: 0.4, p: "1.5.8." },
          { t: "bass", i: "cellos", o: -2, v: 0.55, p: "1--5--" },
          { t: "drums", v: 0.6, p: { dum: "x..x..", tek: ".x...x" } },
          { t: "hit", i: "toll", deg: "1", o: -1, v: 0.5, bars: [0, 4] },
        ],
      },
      A2: {
        like: "A", dyn: 1.12,
        layers: [
          { t: "mel", i: "fiddle", o: 0, v: 0.9, dbl: [{ i: "violins", o: 1, v: 0.3 }] },
          { t: "arp", i: "oud", o: -1, v: 0.55, p: "1.5.8." },
          { t: "arp", i: "harpsichord", o: 0, v: 0.4, p: "...35." },
          { t: "pad", i: "choir", o: 0, v: 0.4, n: 3 },
          { t: "bass", i: "cellos", o: -2, v: 0.55, p: "1--5--" },
          { t: "drums", v: 0.65, p: { dum: "x..x..", tek: ".xx.xx" } },
        ],
      },
      bridge: {
        chords: "6 | 4 | 6 | 5", dyn: 0.6, dynTo: 0.45,
        layers: [
          { t: "pad", i: "choir", o: 0, v: 0.5, n: 3 },
          { t: "hit", i: "toll", deg: "1", o: -1, v: 0.55, bars: [0] },
          { t: "tex", kind: "wind", v: 0.9 },
        ],
      },
    },
    form: ["intro", "A", "B", "A2", "bridge"], loop: 1,
  });

  region({
    id: "bloodstone-coast", title: "Bloodstone · Nocturne Keep",
    tempo: 96, meter: [3, 2], tonic: 55, mode: "harmonic", room: "cathedral",
    sections: {
      intro: {
        chords: "1 | 4 | 5 | 1", dyn: 0.7,
        layers: [
          { t: "bass", i: "pizz", o: -1, v: 0.55, p: "1....." },
          { t: "strum", i: "harpsichord", o: 0, v: 0.45, p: "..d.d." },
          { t: "tex", kind: "surf", v: 0.8 },
          { t: "hit", i: "toll", deg: "1", o: 0, v: 0.6, bars: [0] },
        ],
      },
      A: {
        chords: "1 | 4 | 5 | 1 | 6 | 4 | 5 | 1",
        mel: "5/3 3/2 1/1 | 4/3 6/2 4/1 | 7,/2 2/2 5/2 | 3/4 _/2 | 6/3 5/2 3/1 | 4/2 6/2 1'/2 | 7/2 2'/2 7/2 | 1'/6",
        layers: [
          { t: "mel", i: "fiddle", o: 1, v: 0.9 },
          { t: "bass", i: "pizz", o: -1, v: 0.55, p: "1....." },
          { t: "strum", i: "harpsichord", o: 0, v: 0.45, p: "..d.d." },
          { t: "pad", i: "strings", o: 0, v: 0.35, n: 3 },
        ],
      },
      B: {
        chords: "6 | b7 | 1 | 5 | 6 | b2 | 5 | 5", dyn: 1.08,
        mel: "1'/4 6/2 | b7/4 2'/2 | 3'/3 2'/1 1'/2 | 2'/6 | 3'/2 1'/2 6/2 | b2'/4 6/2 | 2'/2 7/2 2'/2 | 5/6",
        layers: [
          { t: "mel", i: "violins", o: 1, v: 0.85, dbl: [{ i: "organ", o: 0, v: 0.35 }] },
          { t: "pad", i: "organ", o: 0, v: 0.45, n: 3 },
          { t: "pad", i: "choir", o: 0, v: 0.35, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.5, p: "1-----" },
          { t: "hit", i: "toll", deg: "1", o: 0, v: 0.5, bars: [0, 4] },
        ],
      },
      A2: {
        like: "A", dyn: 1.12,
        layers: [
          { t: "mel", i: "fiddle", o: 1, v: 0.9, dbl: [{ i: "violins", o: 1, v: 0.3 }] },
          { t: "bass", i: "pizz", o: -1, v: 0.6, p: "1....." },
          { t: "strum", i: "harpsichord", o: 0, v: 0.5, p: "..d.d." },
          { t: "pad", i: "strings", o: 0, v: 0.45, n: 3 },
          { t: "pad", i: "organ", o: -1, v: 0.3, n: 3 },
          { t: "drums", v: 0.45, p: { timp: "x....." } },
        ],
      },
      bridge: {
        chords: "6 | 4 | b2 | 5", dyn: 0.65, dynTo: 0.5,
        layers: [
          { t: "pad", i: "organ", o: 0, v: 0.5, n: 3 },
          { t: "hit", i: "toll", deg: "1", o: 0, v: 0.55, bars: [0, 2] },
          { t: "tex", kind: "surf", v: 0.9 },
        ],
      },
    },
    form: ["intro", "A", "B", "A2", "bridge"], loop: 1,
  });

  region({
    id: "ancient-battlefield", title: "Ancient Battlefield · March of the Lost",
    tempo: 66, meter: [4, 4], tonic: 50, mode: "aeolian", room: "cathedral",
    sections: {
      intro: {
        chords: "1 | 6 | 1 | 5", dyn: 0.7,
        layers: [
          { t: "drums", v: 0.6, p: { muffled: "x.....xxx.......", war: "x..............." } },
          { t: "drone", i: "drone", o: -1, v: 0.55, deg: ["1", "5"] },
          { t: "tex", kind: "wind", v: 0.9 },
          { t: "hit", i: "toll", deg: "1", o: 0, v: 0.6, bars: [0, 2] },
        ],
      },
      A: {
        chords: "1 | 6 | 3 | 7 | 1 | 4 | 6 | 5",
        mel: "1/6 1/2 3/4 5/4 | 4/6 3/2 1/8 | 3/6 3/2 5/4 1'/4 | 7/12 _/4 | 5/6 5/2 1'/4 7/4 | 6/6 5/2 4/8 | 3/6 4/2 6/8 | 5/16",
        layers: [
          { t: "mel", i: "trombones", o: 1, v: 0.9, dbl: [{ i: "cellos", o: 0, v: 0.4 }] },
          { t: "pad", i: "choirOo", o: 1, v: 0.45, n: 3 },
          { t: "bass", i: "contrabass", o: -1, v: 0.55, p: "1-------5-------" },
          { t: "drums", v: 0.6, p: { muffled: "x.....xxx.......", war: "x.......x......." } },
        ],
      },
      B: {
        chords: "6 | 7 | 1 | 1 | 4 | 6 | 5 | 1", dyn: 1.08,
        mel: "6/6 1'/2 3'/8 | 2'/8 7/8 | 1'/12 _/4 | 5/6 6/2 5/8 | 4/6 6/2 1'/8 | 3'/8 1'/8 | 7/8 5/8 | 1'/16",
        layers: [
          { t: "mel", i: "horns", o: 1, v: 0.9 },
          { t: "pad", i: "choir", o: 1, v: 0.55, n: 3 },
          { t: "bass", i: "contrabass", o: -1, v: 0.6, p: "1-------5-------" },
          { t: "drums", v: 0.7, p: { muffled: "x.....xxx...x...", war: "x.......x......." } },
          { t: "hit", i: "toll", deg: "1", o: 0, v: 0.5, bars: [0, 4] },
        ],
      },
      A2: {
        like: "A", dyn: 1.12,
        layers: [
          { t: "mel", i: "trombones", o: 1, v: 0.95, dbl: [{ i: "horns", o: 1, v: 0.45 }, { i: "trumpet", o: 2, v: 0.2 }] },
          { t: "pad", i: "choir", o: 1, v: 0.45, n: 3 },
          { t: "pad", i: "cellos", o: 0, v: 0.35, n: 3 },
          { t: "bass", i: "contrabass", o: -1, v: 0.6, p: "1-------5-------" },
          { t: "drums", v: 0.75, p: { muffled: "x.....xxx...x.x.", war: "x.......x......." } },
        ],
      },
      bridge: {
        chords: "6 | 4 | 6 | 5", dyn: 0.6, dynTo: 0.45,
        layers: [
          { t: "pad", i: "choirOo", o: 1, v: 0.5, n: 3 },
          { t: "hit", i: "toll", deg: "1", o: 0, v: 0.55, bars: [0, 2] },
          { t: "tex", kind: "wind", v: 1 },
          { t: "drums", v: 0.4, p: { muffled: "x..............." } },
        ],
      },
    },
    form: ["intro", "A", "B", "A2", "bridge"], loop: 1,
  });

  region({
    id: "duskpine-forest", title: "Duskpine · The Cold Canopy",
    tempo: 64, meter: [3, 2], tonic: 61, mode: "aeolian", room: "hall",
    sections: {
      intro: {
        chords: "1 | 6 | 3 | 7", dyn: 0.65,
        layers: [
          { t: "arp", i: "celesta", o: 1, v: 0.45, p: "1.5.8." },
          { t: "pad", i: "tremolo", o: -1, v: 0.4, n: 3 },
          { t: "tex", kind: "wind", v: 1 },
        ],
      },
      A: {
        chords: "1 | 6 | 3 | 7 | 1 | 4 | 6 | 5",
        mel: "5/3 3/2 1/1 | 6/3 1'/3 | 7/3 5/2 3/1 | 2/4 _/2 | 1'/2 7/2 5/2 | 6/3 4/3 | 3/2 1'/2 6/2 | 5/6",
        layers: [
          { t: "mel", i: "flute", o: 0, v: 0.9 },
          { t: "arp", i: "celesta", o: 1, v: 0.4, p: "1.5.8." },
          { t: "arp", i: "harp", o: -1, v: 0.4, p: "1....." },
          { t: "pad", i: "tremolo", o: -1, v: 0.35, n: 3 },
          { t: "bass", i: "cellos", o: -2, v: 0.45, p: "1-----" },
        ],
      },
      B: {
        chords: "6 | 7 | 1 | 1 | 6 | 7 | 4 | 5", dyn: 1.06,
        mel: "3'/3 1'/3 | 2'/3 7/3 | 1'/6 | _/2 5/2 3/2 | 1'/3 7/1 6/2 | 7/3 6/1 4/2 | 6/2 1'/2 3'/2 | 2'/6",
        layers: [
          { t: "mel", i: "horns", o: 0, v: 0.85, dbl: [{ i: "violins", o: 1, v: 0.3 }] },
          { t: "pad", i: "strings", o: -1, v: 0.45, n: 3 },
          { t: "arp", i: "harp", o: -1, v: 0.4, p: "1..5.." },
          { t: "bass", i: "cellos", o: -2, v: 0.5, p: "1-----" },
          { t: "drums", v: 0.45, p: { timp: "x....." } },
        ],
      },
      A2: {
        like: "A", dyn: 1.1,
        layers: [
          { t: "mel", i: "flute", o: 0, v: 0.9, dbl: [{ i: "violins", o: 0, v: 0.4 }] },
          { t: "arp", i: "celesta", o: 1, v: 0.45, p: "1.5.8." },
          { t: "arp", i: "harp", o: -1, v: 0.45, p: "1..5.." },
          { t: "pad", i: "tremolo", o: -1, v: 0.4, n: 3 },
          { t: "bass", i: "cellos", o: -2, v: 0.5, p: "1-----" },
          { t: "hit", i: "toll", deg: "1", o: -1, v: 0.4, bars: [0] },
        ],
      },
      bridge: {
        chords: "4 | 6 | 4 | 5", dyn: 0.6, dynTo: 0.45,
        layers: [
          { t: "pad", i: "tremolo", o: -1, v: 0.45, n: 3 },
          { t: "arp", i: "celesta", o: 1, v: 0.35, p: "1....." },
          { t: "tex", kind: "wind", v: 1.1 },
        ],
      },
    },
    form: ["intro", "A", "B", "A2", "bridge"], loop: 1,
  });

  region({
    id: "sunward-coast", title: "Sunward Coast · Saltwind Road",
    tempo: 92, meter: [2, 3], tonic: 57, mode: "mixolydian", room: "open",
    sections: {
      intro: {
        chords: "1 | 7 | 4 | 1", dyn: 0.7,
        layers: [
          { t: "strum", i: "guitar", o: 0, v: 0.5, p: "d..d.u" },
          { t: "tex", kind: "surf", v: 0.9 },
          { t: "drums", v: 0.5, p: { frame: "x..o.." } },
        ],
      },
      A: {
        chords: "1 | 7 | 4 | 1 | 1 | 7 | 4 5 | 1",
        mel: "5/2 3/1 1/2 3/1 | 4/3 2/2 7,/1 | 6/2 4/1 1/3 | 3/3 5/3 | 1'/2 7/1 1'/2 5/1 | 7/3 4/2 2/1 | 6/2 4/1 5/3 | 1/6",
        layers: [
          { t: "mel", i: "fiddle", o: 1, v: 0.9 },
          { t: "strum", i: "guitar", o: 0, v: 0.45, p: "d..d.u" },
          { t: "bass", i: "cellos", o: -1, v: 0.5, p: "1--5--" },
          { t: "pad", i: "pad", o: 0, v: 0.35, n: 3 },
          { t: "drums", v: 0.55, p: { frame: "x..o.o", shaker: ".x..x." } },
          { t: "tex", kind: "surf", v: 0.5 },
        ],
      },
      B: {
        chords: "4 | 1 | 7 | 4 | 4 | 1 | 2 | 5", dyn: 1.06,
        mel: "1'/3 6/2 4/1 | 5/3 3/3 | 4/2 7/1 2'/3 | 1'/6 | 6/2 1'/1 6/3 | 5/2 3/1 1/3 | 2/2 4/1 6/3 | 5/6",
        layers: [
          { t: "mel", i: "whistle", o: 1, v: 0.85, harm: { i: "fiddle", steps: -2, v: 0.4 } },
          { t: "strum", i: "guitar", o: 0, v: 0.45, p: "d..d.u" },
          { t: "arp", i: "harp", o: 0, v: 0.35, p: "1.5.8." },
          { t: "bass", i: "cellos", o: -1, v: 0.5, p: "1--5--" },
          { t: "drums", v: 0.6, p: { frame: "X..x.o", shaker: ".xx.xx" } },
        ],
      },
      A2: {
        like: "A", dyn: 1.12,
        layers: [
          { t: "mel", i: "fiddle", o: 1, v: 0.9, dbl: [{ i: "whistle", o: 1, v: 0.35 }] },
          { t: "strum", i: "guitar", o: 0, v: 0.5, p: "d..d.u" },
          { t: "bass", i: "cellos", o: -1, v: 0.55, p: "1--5--" },
          { t: "pad", i: "strings", o: 0, v: 0.4, n: 3 },
          { t: "drums", v: 0.65, p: { frame: "X..x.o", shaker: ".xx.xx" } },
        ],
      },
      bridge: {
        chords: "4 | 7 | 4 | 5", dyn: 0.65, dynTo: 0.5,
        layers: [
          { t: "pad", i: "pad", o: 0, v: 0.5, n: 3 },
          { t: "arp", i: "harp", o: 0, v: 0.4, p: "1.....5....." },
          { t: "tex", kind: "surf", v: 1 },
        ],
      },
    },
    form: ["intro", "A", "B", "A2", "bridge"], loop: 1,
  });

  region({
    id: "cinder-isle", title: "Cinder Isle · Heart of the Mountain",
    tempo: 80, meter: [4, 4], tonic: 48, mode: "phrygian", room: "cavern",
    sections: {
      intro: {
        chords: "1 | 2 | 1 | 7", dyn: 0.75,
        layers: [
          { t: "drums", v: 0.65, p: { taiko: "X.....x.x.......", anvil: "........x......." } },
          { t: "drone", i: "drone", o: 0, v: 0.55, deg: ["1", "5"] },
          { t: "tex", kind: "embers", v: 0.8 },
          { t: "hit", i: "gong", v: 0.6, bars: [0] },
        ],
      },
      A: {
        chords: "1 | 2 | 1 | 7 | 6 | 2 | 7 | 1",
        mel: "5/6 3/2 1/8 | 2/6 4/2 6/8 | 3/6 2/2 1/8 | 7,/8 2/8 | 1/6 3/2 6/8 | 4/8 2/8 | 4/6 2/2 7,/8 | 1/16",
        layers: [
          { t: "mel", i: "trombones", o: 1, v: 0.9, dbl: [{ i: "horns", o: 1, v: 0.45 }] },
          { t: "pulse", i: "lowSpiccato", o: 1, v: 0.5, p: "1.1.5.1.1.1.5.8." },
          { t: "pad", i: "choir", o: 1, v: 0.4, n: 3 },
          { t: "bass", i: "contrabass", o: 0, v: 0.55, p: "1---------------" },
          { t: "drums", v: 0.65, p: { taiko: "X.....x.x.......", anvil: "........x......." } },
          { t: "tex", kind: "embers", v: 0.5 },
        ],
      },
      B: {
        chords: "6 | 7 | 2 | 1 | 6 | 3 | 2 | 1", dyn: 1.08,
        mel: "1'/8 6/8 | 2'/8 7/8 | 6/6 4/2 2/8 | 5/16 | 3'/8 1'/8 | 7/6 5/2 3/8 | 4/8 6/8 | 1/16",
        layers: [
          { t: "mel", i: "horns", o: 1, v: 0.9, dbl: [{ i: "choir", o: 1, v: 0.35 }] },
          { t: "pulse", i: "lowSpiccato", o: 1, v: 0.5, p: "1.1.5.1.1.1.5.8." },
          { t: "pad", i: "choir", o: 1, v: 0.5, n: 3 },
          { t: "bass", i: "contrabass", o: 0, v: 0.6, p: "1-------5-------" },
          { t: "drums", v: 0.75, p: { taiko: "X.....x.x...x...", anvil: "....x.......x...", timp: "x.......x......." } },
          { t: "hit", i: "gong", v: 0.5, bars: [0, 4] },
        ],
      },
      A2: {
        like: "A", dyn: 1.14,
        layers: [
          { t: "mel", i: "trombones", o: 1, v: 0.95, dbl: [{ i: "horns", o: 1, v: 0.5 }, { i: "violins", o: 2, v: 0.25 }] },
          { t: "pulse", i: "lowSpiccato", o: 1, v: 0.55, p: "1.1.5.1.1.1.5.8." },
          { t: "pad", i: "choir", o: 1, v: 0.45, n: 3 },
          { t: "bass", i: "contrabass", o: 0, v: 0.6, p: "1---------------" },
          { t: "drums", v: 0.8, p: { taiko: "X.....x.x...x.x.", anvil: "....x.......x..." } },
        ],
      },
      bridge: {
        chords: "2 | 1 | 2 | 1", dyn: 0.65, dynTo: 0.5,
        layers: [
          { t: "drone", i: "drone", o: 0, v: 0.55, deg: ["1", "5"] },
          { t: "tex", kind: "embers", v: 0.9 },
          { t: "hit", i: "gong", v: 0.55, bars: [0] },
          { t: "drums", v: 0.5, p: { taiko: "x..............." } },
        ],
      },
    },
    form: ["intro", "A", "B", "A2", "bridge"], loop: 1,
  });


  /* ---------------- The battle theme ---------------- */
  const combat = {
    id: "combat", title: "Battle · Steel and Ruin",
    tempo: 138, meter: [4, 4], tonic: 62, mode: "harmonic", room: "hall", gain: 1,
    palette: { lead: "horns", lead2: "violins", ost: "lowSpiccato", ost2: "spiccato", pad: "choir", bass: "contrabass", quote: "trumpet", hits: "taiko" },
    grooves: {
      intro: { war: "X...............", timp: "............rrrr" },
      A: { taiko: "X.....x.X.....x.", snare: "....x.......x..o", frame: "..x...x...x...xx" },
      B: { taiko: "X..x..x.X..x..x.", snare: "....x.......x.x.", timp: "x.......x.......", hat: "x.x.x.x.x.x.x.x." },
      C: { war: "X.......x.......", timp: "x...x...x...x.xr", frame: "..x...x...x...x." },
    },
    sections: {
      intro: {
        chords: "1 | 5", dyn: 0.85,
        layers: [
          { t: "pulse", i: "@ost", o: -1, v: 0.8, p: "1151115811511581" },
          { t: "hit", i: "boom", v: 0.8, bars: [0] },
          { t: "hit", i: "swell", v: 0.7, bars: [1], len: 16 },
          { t: "drums", v: 0.9, p: "@intro" },
        ],
      },
      A: {
        chords: "1 | 6 | 4 | 5 | 1 | 6 | b2 | 5",
        mel: "5/6 4/2 3/4 1/4 | 3/6 4/2 6/4 4/4 | 4/4 6/4 1'/6 6/2 | 7/8 2'/4 5/4 | 1'/6 7/2 1'/4 5/4 | 3'/6 2'/2 1'/4 6/4 | b2'/6 1'/2 6/4 4/4 | 7/4 2'/4 5/8",
        layers: [
          { t: "mel", i: "@lead", v: 0.9 },
          { t: "pulse", i: "@ost", o: -1, v: 0.75, p: "1151115811511581" },
          { t: "pulse", i: "@ost2", o: 0, v: 0.5, p: "..5...3...5...8." },
          { t: "bass", i: "@bass", o: -2, v: 0.55, p: "1-------1-------" },
          { t: "drums", v: 0.85, p: "@A" },
        ],
      },
      B: {
        chords: "6 | 4 | 1 | 5 | 6 | 4 | 6 5 | 5", dyn: 1.08,
        mel: "1'/4 3'/4 1'/4 6/4 | 1'/6 6/2 4/8 | 3/4 5/4 1'/4 2'/2 1'/2 | 7/8 5/4 2'/4 | 1'/4 3'/4 4'/4 3'/4 | 2'/6 1'/2 6/8 | 1'/4 6/4 7/4 2'/4 | 5/12 7/4",
        layers: [
          { t: "mel", i: "@lead2", v: 0.9, dbl: [{ i: "@lead", o: -1, v: 0.35 }] },
          { t: "pulse", i: "@ost", o: -1, v: 0.75, p: "1151115811511581" },
          { t: "pad", i: "@pad", o: 0, v: 0.55, n: 3 },
          { t: "bass", i: "@bass", o: -2, v: 0.6, p: "1-------5-------" },
          { t: "hit", i: "@hits", v: 0.7, bars: [0, 4] },
          { t: "drums", v: 0.9, p: "@B" },
        ],
      },
      A2: {
        like: "A", dyn: 1.12,
        layers: [
          { t: "mel", i: "@lead", v: 0.95, dbl: [{ i: "@lead2", o: 1, v: 0.4 }] },
          { t: "pulse", i: "@ost", o: -1, v: 0.8, p: "1151115811511581" },
          { t: "pulse", i: "@ost2", o: 0, v: 0.55, p: "5.3.5.3.5.3.8.5." },
          { t: "pad", i: "@pad", o: 0, v: 0.45, n: 3 },
          { t: "bass", i: "@bass", o: -2, v: 0.6, p: "1-------1-------" },
          { t: "drums", v: 0.95, p: "@B" },
        ],
      },
      C: {
        chords: "1 | 6 | 4 | 5 | 1 | 6 | 4 | 5", dyn: 0.95, dynTo: 1.15,
        layers: [
          { t: "line", i: "@quote", v: 0.85, role: "quote" },
          { t: "pulse", i: "@ost", o: -1, v: 0.7, p: "1.11.11.1.11.15." },
          { t: "pad", i: "@pad", o: 0, v: 0.6, n: 3 },
          { t: "bass", i: "@bass", o: -2, v: 0.6, p: "1---------------" },
          { t: "hit", i: "swell", v: 0.6, bars: [7], len: 16 },
          { t: "drums", v: 0.85, p: "@C" },
        ],
      },
    },
    form: ["intro", "A", "B", "A2", "C"], loop: 1,
    quote: "5/6 4/2 3/4 1/4 | 3/6 4/2 6/8 | 6/4 4/4 6/4 1'/4 | 5/8 2'/4 5/4",
  };

  // Each region's arrangement of the battle theme. The quote is the region's
  // own melody, reshaped to the battle's four-bar progression (1 6 4 5).
  const variations = {
    "alder-vale": {
      title: "Battle · Alder Vale", tonic: 62, mode: "aeolian", tempo: 132,
      palette: { lead: "fiddle", lead2: "violins", ost2: "lute", pad: "strings", quote: "whistle", hits: "frame" },
      grooves: { A: { frame: "X..x..x.X..x..x.", snare: "....x.......x...", shaker: "x.x.x.x.x.x.x.x." } },
      extra: { A: [{ t: "arp", i: "lute", o: 0, v: 0.45, p: "1.5.8.5.1.5.8.5." }] },
      quote: "5/4 6/2 5/4 4/2 3/4 | 3/6 1/4 6,/6 | 4/4 6/4 1'/4 6/4 | 5/8 2/8",
    },
    "stoneveil-foothills": {
      title: "Battle · Stoneveil Foothills", tonic: 57, mode: "aeolian", tempo: 128,
      palette: { lead: "horns", lead2: "trumpet", ost2: "harp", pad: "choir", quote: "horns", hits: "timp" },
      grooves: { A: { war: "X.......x.......", timp: "x...x...x...x...", snare: "....x.......x..o" } },
      extra: { intro: [{ t: "tex", kind: "wind", v: 0.7 }] },
      quote: "1/6 2/2 3/4 5/4 | 6/6 1'/2 6/8 | 4/6 6/2 1'/8 | 5/12 _/4",
    },
    "high-stoneveil": {
      title: "Battle · High Stoneveil", tonic: 59, mode: "aeolian", tempo: 124, room: "mountain",
      palette: { lead: "horns", lead2: "violins", ost2: "celesta", pad: "sopranos", quote: "flute", hits: "war" },
      grooves: { A: { war: "X.......x.......", snare: "....x.......x...", timp: "x.......x......." } },
      extra: { intro: [{ t: "tex", kind: "wind", v: 1 }], C: [{ t: "tex", kind: "wind", v: 0.6 }] },
      quote: "5/8 1'/4 7/4 | 6/8 3/8 | 1'/8 6/8 | 5/8 2'/8",
    },
    "stoneveil-summit": {
      title: "Battle · Stoneveil Summit", tonic: 52, mode: "aeolian", tempo: 112, room: "mountain", leadOctave: 1,
      palette: { lead: "trombones", lead2: "horns", ost2: "pizz", pad: "choirOo", quote: "horns", hits: "gong" },
      grooves: {
        A: { war: "X.......X.......", timp: "....x.......x..r" },
        B: { war: "X...x...X...x...", timp: "x.......x.......", snare: "....x.......x..." },
      },
      extra: { intro: [{ t: "tex", kind: "wind", v: 1.2 }] },
      quote: "1/10 b2/2 1/4 | 3/8 1/8 | 6/8 4/8 | 5/16",
    },
    "verdant-reach": {
      title: "Battle · Verdant Reach", tonic: 64, mode: "aeolian", tempo: 136, room: "forest",
      palette: { lead: "violins", lead2: "flute", ost2: "harp", pad: "strings", quote: "oboe", hits: "frame" },
      grooves: { A: { frame: "X..x..x.X..x..x.", shaker: "x.x.x.x.x.x.x.x.", tom: "....x.......x..." } },
      extra: { B: [{ t: "arp", i: "kalimba", o: 1, v: 0.35, p: "8...5...T...5..." }] },
      quote: "5/4 1'/4 7/4 5/4 | 6/8 1'/8 | 6/4 4/4 1'/8 | 2'/8 5/8",
    },
    "stormwake-hills": {
      title: "Battle · Stormwake Hills", tonic: 60, mode: "harmonic", tempo: 140, room: "cathedral",
      palette: { lead: "horns", lead2: "violins", ost2: "organ", pad: "choir", quote: "trumpet", hits: "thunder" },
      extra: {
        A: [{ t: "tex", kind: "rain", v: 0.6 }], B: [{ t: "tex", kind: "rain", v: 0.6 }, { t: "pad", i: "organ", o: 0, v: 0.3, n: 3 }],
        A2: [{ t: "tex", kind: "rain", v: 0.6 }], C: [{ t: "tex", kind: "rain", v: 0.6 }, { t: "hit", i: "toll", deg: "1", o: -1, v: 0.5, bars: [0, 4] }],
      },
      quote: "5/6 4/2 3/8 | 1/8 3/8 | 4/8 6/8 | 5/16",
    },
    "gloamfen-marsh": {
      title: "Battle · Gloamfen Marsh", tonic: 65, mode: "aeolian", tempo: 120, room: "cavern",
      palette: { lead: "clarinet", lead2: "oboe", ost2: "pizz", pad: "glass", quote: "duduk", hits: "tom" },
      grooves: {
        A: { muffled: "....x.......x...", tom: "x.....x.x.......", war: "x..............." },
        B: { muffled: "....x.......x.x.", tom: "x..x..x.x..x....", war: "x.......x......." },
      },
      extra: { A: [{ t: "drips", i: "kalimba", o: 1, v: 0.4, chance: 0.06 }], C: [{ t: "drips", i: "kalimba", o: 1, v: 0.4, chance: 0.06 }] },
      quote: "1/6 3/4 5/6 | 6/8 1/8 | 4/8 6/8 | 5/12 2/4",
    },
    "verdigris-jungle": {
      title: "Battle · Verdigris Jungle", tonic: 55, mode: "aeolian", tempo: 134, room: "forest", leadOctave: 1,
      palette: { lead: "duduk", lead2: "flute", ost: "marimba", ost2: "kalimba", pad: "choir", quote: "duduk", hits: "taiko" },
      grooves: {
        A: { log: "x..x..x.x.x..x..", shaker: "x.x.x.x.x.x.x.x.", clack: "....x.......x..." },
        B: { log: "x..x..x.x.x..x..", shaker: "xxx.xxx.xxx.xxx.", taiko: "x.......x.......", clack: "....x.......x.x." },
      },
      quote: "5/6 4/2 3/4 1/4 | 6/6 5/2 3/8 | 4/4 6/4 1'/8 | 5/8 2'/8",
    },
    "mirrormere-basin": {
      title: "Battle · Mirrormere Basin", tonic: 65, mode: "aeolian", tempo: 126,
      palette: { lead: "fiddle", lead2: "flute", ost2: "harp", pad: "strings", quote: "celesta", hits: "timp" },
      grooves: { A: { frame: "x.....x.x.......", tom: "....x.......x...", timp: "x.......x......." } },
      extra: { C: [{ t: "tex", kind: "surf", v: 0.6 }] },
      quote: "3/8 5/4 1'/4 | 6/8 3/8 | 4/8 1'/8 | 5/16",
    },
    "saffron-expanse": {
      title: "Battle · Saffron Expanse", tonic: 62, mode: "harmonic", tempo: 130, room: "open",
      palette: { lead: "duduk", lead2: "oboe", ost2: "oud", pad: "strings", quote: "duduk", hits: "dum" },
      grooves: {
        A: { dum: "X..x..x.X..x..x.", tek: "..x.x..x..x.x..x", zill: "x.......x......." },
        B: { dum: "X..x..x.X..x..x.", tek: ".xx.xx.x.xx.xx.x", zill: "x...x...x...x...", taiko: "x.......x......." },
        C: { dum: "X.......x.......", tek: "..x...x...x...x.", zill: "x..............." },
      },
      quote: "5/4 6/2 5/2 3/8 | 3/8 b2/2 1/6 | 4/6 6/2 1'/8 | 7/8 5/8",
    },
    "deep-desert": {
      title: "Battle · Deep Desert", tonic: 60, mode: "harmonic", tempo: 118, room: "cathedral",
      palette: { lead: "duduk", lead2: "violins", ost2: "harp", pad: "choirOo", quote: "oboe", hits: "gong" },
      grooves: {
        A: { frame: "X..x..x.X..x..x.", tom: "....x.......x...", zill: "x.......x......." },
        B: { frame: "X..x..x.X..x..x.", tom: "x...x...x...x...", zill: "x...x...x...x...", war: "x.......x......." },
      },
      quote: "5/6 6/2 5/4 3/4 | 1/6 b2/2 1/8 | 4/6 6/2 1'/8 | 5/16",
    },
    "crimson-divide": {
      title: "Battle · Crimson Divide", tonic: 64, mode: "harmonic", tempo: 134,
      palette: { lead: "fiddle", lead2: "violins", ost2: "harpsichord", pad: "choir", quote: "fiddle", hits: "toll" },
      grooves: { A: { dum: "X..x..x.X..x..x.", tek: "..x.x..x..x.x..x", snare: "....x.......x..." } },
      extra: { A: [{ t: "arp", i: "oud", o: 0, v: 0.4, p: "1.5.8.5.1.5.8.5." }] },
      quote: "5/4 6/2 5/2 3/8 | 3/8 1/8 | 1'/8 6/8 | 7/8 2'/8",
    },
    "bloodstone-coast": {
      title: "Battle · Bloodstone Coast", tonic: 55, mode: "harmonic", tempo: 136, room: "cathedral", leadOctave: 1,
      palette: { lead: "fiddle", lead2: "organ", ost2: "harpsichord", pad: "choir", quote: "violins", hits: "toll" },
      extra: { B: [{ t: "pad", i: "organ", o: 0, v: 0.3, n: 3 }], C: [{ t: "tex", kind: "surf", v: 0.6 }] },
      quote: "5/6 3/4 1/6 | 3/4 1/4 6,/8 | 4/6 6/4 4/6 | 5/16",
    },
    "ancient-battlefield": {
      title: "Battle · Ancient Battlefield", tonic: 62, mode: "aeolian", tempo: 120, room: "cathedral",
      palette: { lead: "trombones", lead2: "horns", ost2: "pizz", pad: "choirOo", quote: "trumpet", hits: "war" },
      grooves: {
        A: { snare: "x.xxx.x.x.xxx.x.", war: "X.......x......." },
        B: { snare: "x.xxx.x.x.xxx.xr", war: "X...x...X...x...", timp: "x.......x......." },
        C: { muffled: "x.....xxx.......", war: "X.......x......." },
      },
      extra: { C: [{ t: "hit", i: "toll", deg: "1", o: -1, v: 0.5, bars: [0, 4] }] },
      quote: "1/6 1/2 3/4 5/4 | 3/6 3/2 1/8 | 4/6 4/2 6/8 | 5/16",
    },
    "duskpine-forest": {
      title: "Battle · Duskpine Forest", tonic: 61, mode: "aeolian", tempo: 130,
      palette: { lead: "horns", lead2: "violins", ost2: "celesta", pad: "tremolo", quote: "flute", hits: "taiko" },
      extra: { intro: [{ t: "tex", kind: "wind", v: 1 }], C: [{ t: "tex", kind: "wind", v: 0.7 }] },
      quote: "5/6 3/4 1/6 | 6/8 1'/8 | 6/8 4/8 | 5/8 2'/8",
    },
    "sunward-coast": {
      title: "Battle · Sunward Coast", tonic: 57, mode: "aeolian", tempo: 138, room: "open",
      palette: { lead: "fiddle", lead2: "whistle", ost2: "guitar", pad: "strings", quote: "whistle", hits: "frame" },
      grooves: { A: { frame: "X..x..x.X..x..x.", shaker: "x.x.x.x.x.x.x.x.", snare: "....x.......x..." } },
      extra: { C: [{ t: "tex", kind: "surf", v: 0.7 }] },
      quote: "5/4 3/2 1/4 3/6 | 3/8 6/8 | 4/4 6/4 1'/8 | 5/8 2'/8",
    },
    "cinder-isle": {
      title: "Battle · Cinder Isle", tonic: 60, mode: "aeolian", tempo: 136, room: "cavern",
      palette: { lead: "trombones", lead2: "horns", ost2: "pizz", pad: "choir", quote: "trumpet", hits: "taiko" },
      grooves: {
        A: { taiko: "X.x...x.X.x...x.", anvil: "....x.......x...", snare: "....x.......x..." },
        B: { taiko: "X.x.x.x.X.x.x.x.", anvil: "....x.......x.x.", timp: "x.......x......." },
      },
      extra: { intro: [{ t: "tex", kind: "embers", v: 0.8 }], C: [{ t: "tex", kind: "embers", v: 0.6 }] },
      quote: "5/6 3/2 1/8 | 3/6 b2/2 1/8 | 4/8 6/8 | 5/16",
    },
  };

  /* ---------------- Arranging ---------------- */
  function arrange(base, variation = {}, id = base.id) {
    const palette = { ...base.palette, ...variation.palette };
    const grooves = { ...base.grooves, ...variation.grooves };
    const resolve = layer => {
      if (!layer) return layer;
      const copy = { ...layer };
      if (typeof copy.i === "string" && copy.i.startsWith("@")) copy.i = palette[copy.i.slice(1)];
      if (Array.isArray(copy.dbl)) copy.dbl = copy.dbl.map(resolve).filter(extra => extra.i);
      else if (copy.dbl) copy.dbl = resolve(copy.dbl);
      if (copy.harm) copy.harm = resolve(copy.harm);
      if (copy.t === "drums" && typeof copy.p === "string") copy.p = grooves[copy.p.slice(1)];
      if (copy.role === "quote") copy.notes = variation.quote || base.quote;
      if ((copy.t === "mel" || copy.t === "line") && variation.leadOctave) copy.o = (copy.o || 0) + variation.leadOctave;
      return copy;
    };
    const sections = {};
    for (const [name, section] of Object.entries(base.sections)) {
      const layers = (section.layers || base.sections[section.like]?.layers || []).map(resolve).filter(layer => layer.t === "drums" || layer.t === "tex" || layer.i);
      sections[name] = { ...section, layers: [...layers, ...(variation.extra?.[name] || [])] };
    }
    const { palette: _p, grooves: _g, quote: _q, ...rest } = base;
    return {
      ...rest, id, title: variation.title || base.title,
      tonic: variation.tonic ?? base.tonic, mode: variation.mode || base.mode, tempo: variation.tempo || base.tempo,
      room: variation.room || base.room, gain: variation.gain ?? base.gain, sections,
    };
  }

  /* ---------------- Boss themes ----------------
     Remastered boss themes replace the original score of the same id. */
  const bosses = [];
  // Great Imp Ji'ruan: a slow, heavy 3/4 in harmonic minor for a void-crowned imp king. A bell
  // tolls over a drone, a wordless choir sings his theme over spiccato strings, and the climax
  // turns to the augmented third and the Neapolitan chord with sopranos, brass and war drums.
  bosses.push({
    id: "great-imp-jiruan", title: "Great Imp Ji'ruan · Crown of the Void",
    tempo: 92, meter: [3, 2], tonic: 58, mode: "harmonic", room: "cathedral", gain: 1,
    sections: {
      intro: {
        chords: "1 | b2 | 1 | 5M", dyn: 0.7, dynTo: 0.95,
        layers: [
          { t: "drone", i: "drone", o: -2, v: 0.7, deg: ["1", "5"] },
          { t: "pad", i: "choirOo", o: -1, v: 0.55, n: 3 },
          { t: "hit", i: "toll", deg: "1", o: -1, v: 0.75, bars: [0, 2] },
          { t: "hit", i: "swell", v: 0.6, bars: [3], len: 6 },
          { t: "tex", kind: "wind", v: 0.5 },
          { t: "drums", v: 0.8, p: { timp: "x.....................rr" } },
        ],
      },
      A: {
        chords: "1 | 6 | 4 | 5 | 1 | 6 | b2 | 5",
        mel: "5/2 1'/3 7/1 | 6/4 1'/2 | 4/2 6/2 1'/2 | 7/4 2'/2 | 5/2 1'/3 3'/1 | 3'/2 1'/2 6/2 | b2'/2 6/2 4/2 | 7/2 2'/2 7/2",
        layers: [
          { t: "mel", i: "choir", v: 0.85, dbl: [{ i: "horns", o: -1, v: 0.4 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.75, p: "L5L5L8" },
          { t: "pulse", i: "spiccato", o: 0, v: 0.4, p: "..5.8." },
          { t: "bass", i: "contrabass", o: -2, v: 0.6, p: "1-----" },
          { t: "pad", i: "strings", o: -1, v: 0.35, n: 3 },
          { t: "hit", i: "toll", deg: "1", o: -1, v: 0.55, bars: [0, 4] },
          { t: "drums", v: 0.85, p: { taiko: "X.....x.....", timp: "x...x.x...x.", war: "X..........." } },
        ],
      },
      B: {
        chords: "6 | 3 | 4 | 5 | 6 | 3 | b2 | 5M", dyn: 1.1,
        mel: "3'/3 1'/1 6/2 | 7/2 3'/2 5'/2 | 4'/3 3'/1 1'/2 | 2'/4 7/2 | 3'/2 6/2 1'/2 | 7/3 5/1 3/2 | b2'/3 1'/1 6/2 | 7/4 5/2",
        layers: [
          { t: "mel", i: "sopranos", v: 0.85, dbl: [{ i: "trumpet", o: -1, v: 0.35 }, { i: "choir", o: -1, v: 0.4 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.8, p: "L5L8L5" },
          { t: "pad", i: "organ", o: -1, v: 0.45, n: 3 },
          { t: "bass", i: "trombones", o: -2, v: 0.55, p: "1--5--" },
          { t: "bass", i: "contrabass", o: -2, v: 0.55, p: "1-----" },
          { t: "hit", i: "crash", v: 0.6, bars: [0, 4] },
          { t: "hit", i: "boom", v: 0.7, bars: [0] },
          { t: "drums", v: 0.95, p: { war: "X.....X.....", taiko: "x.x.x.x.x.x.", snare: "...x.....x.x", timp: "x.x.x.x.x.rr" } },
        ],
      },
      bridge: {
        chords: "1 | b2 | 6 | 5M", dyn: 0.65, dynTo: 1,
        layers: [
          { t: "line", i: "organ", o: 0, v: 0.7, notes: "1/2 3/2 5/2 | b2'/4 1'/2 | 6/2 5/2 3/2 | 7,/6" },
          { t: "pad", i: "choirOo", o: -1, v: 0.5, n: 3 },
          { t: "drone", i: "drone", o: -2, v: 0.55, deg: ["1"] },
          { t: "hit", i: "toll", deg: "1", o: -1, v: 0.65, bars: [0, 2] },
          { t: "hit", i: "swell", v: 0.65, bars: [3], len: 6 },
          { t: "drums", v: 0.7, p: { timp: "x...........x.........rr" } },
        ],
      },
      A2: {
        like: "A", dyn: 1.15,
        layers: [
          { t: "mel", i: "choir", v: 0.9, dbl: [{ i: "horns", o: -1, v: 0.45 }, { i: "sopranos", o: 1, v: 0.3 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.8, p: "L5L5L8" },
          { t: "pulse", i: "spiccato", o: 0, v: 0.5, p: "35835T" },
          { t: "bass", i: "contrabass", o: -2, v: 0.65, p: "1-----" },
          { t: "bass", i: "trombones", o: -2, v: 0.4, p: "1--5--" },
          { t: "pad", i: "organ", o: -1, v: 0.4, n: 3 },
          { t: "hit", i: "toll", deg: "1", o: -1, v: 0.6, bars: [0, 4] },
          { t: "drums", v: 0.95, p: { war: "X.....x.....", taiko: "x.x.x.x.x.x.", timp: "x...x.x...x." } },
        ],
      },
    },
    form: ["intro", "A", "B", "bridge", "A2"], loop: 1,
    motif: "5/2 1'/3 7/1 | 6/4 5/2",
  });

  // Juila the Grand Forest Matriarch: an ethereal woodland hymn. Harp and a hushed choir open
  // the glade, a lone soprano sings the hymn, the full choir swells over tremolo strings, and
  // the climax rises on drums and brass before a quiet interlude of the turning seasons.
  bosses.push({
    id: "juila-grand-forest-matriach", title: "Juila · The Canopy Remembers",
    tempo: 84, meter: [3, 2], tonic: 57, mode: "aeolian", room: "cathedral", gain: 1,
    sections: {
      intro: {
        chords: "1 | 6 | 4 | 5M", dyn: 0.65, dynTo: 0.85,
        layers: [
          { t: "arp", i: "harp", o: 0, v: 0.65, p: "15T8T5" },
          { t: "pad", i: "choirOo", o: -1, v: 0.45, n: 3 },
          { t: "arp", i: "celesta", o: 1, v: 0.3, p: "..5..8" },
          { t: "drone", i: "drone", o: -2, v: 0.45, deg: ["1", "5"] },
          { t: "tex", kind: "wind", v: 0.45 },
          { t: "hit", i: "swell", v: 0.5, bars: [3], len: 6 },
        ],
      },
      A: {
        chords: "1 | 6 | 4 | 5M | 1 | 3 | 4 | 5M",
        mel: "5/3 1'/2 3'/1 | 3'/4 1'/2 | 1'/2 6/2 4/2 | 5/4 2'/2 | 1'/2 3'/2 5'/2 | 5'/4 3'/2 | 4'/2 3'/2 1'/2 | 2'/4 5/2",
        layers: [
          { t: "mel", i: "sopranos", v: 0.8, dbl: [{ i: "flute", o: 0, v: 0.25 }] },
          { t: "arp", i: "harp", o: 0, v: 0.6, p: "15T8T5" },
          { t: "pad", i: "strings", o: -1, v: 0.35, n: 3 },
          { t: "pad", i: "choirOo", o: -1, v: 0.3, n: 3 },
          { t: "bass", i: "cellos", o: -2, v: 0.5, p: "1-----" },
          { t: "drums", v: 0.45, p: { frame: "x....." } },
        ],
      },
      B: {
        chords: "6 | 7 | 1 | 1 | 6 | 7 | 5M | 5M", dyn: 1.05,
        mel: "3'/3 1'/1 6/2 | 2'/4 7/2 | 1'/3 3'/1 5'/2 | 5'/4 3'/2 | 6'/3 5'/1 3'/2 | 4'/2 2'/2 7/2 | 2'/3 5/1 2'/2 | 5'/6",
        layers: [
          { t: "mel", i: "choir", v: 0.85, dbl: [{ i: "sopranos", o: 1, v: 0.3 }, { i: "violins", o: 0, v: 0.3 }] },
          { t: "arp", i: "harp", o: 0, v: 0.55, p: "1358T5" },
          { t: "pulse", i: "spiccato", o: 0, v: 0.35, p: "..5.8." },
          { t: "pad", i: "tremolo", o: -1, v: 0.4, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.55, p: "1-----" },
          { t: "hit", i: "swell", v: 0.55, bars: [7], len: 6 },
          { t: "drums", v: 0.75, p: { timp: "x.....x.....", frame: "x..x.." } },
        ],
      },
      C: {
        chords: "1 | 6 | 7 | 3 | 4 | 1 | 5M | 5M", dyn: 1.2,
        mel: "1'/2 5'/2 3'/2 | 6'/3 5'/1 3'/2 | 4'/3 2'/1 7/2 | 3'/4 5'/2 | 4'/2 6'/2 4'/2 | 3'/3 1'/1 5/2 | 2'/2 5'/2 2'/2 | 5'/6",
        layers: [
          { t: "mel", i: "choir", v: 0.95, dbl: [{ i: "sopranos", o: 1, v: 0.45 }, { i: "horns", o: -1, v: 0.45 }, { i: "violins", o: 0, v: 0.35 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.75, p: "L5L5L8" },
          { t: "pulse", i: "spiccato", o: 0, v: 0.45, p: "35835T" },
          { t: "arp", i: "harp", o: 1, v: 0.4, p: "15T8T5" },
          { t: "bass", i: "contrabass", o: -2, v: 0.65, p: "1-----" },
          { t: "bass", i: "trombones", o: -2, v: 0.45, p: "1--5--" },
          { t: "hit", i: "crash", v: 0.55, bars: [0, 4] },
          { t: "drums", v: 0.95, p: { taiko: "X.....x.....", timp: "x.x.x.x.x.rr", war: "X..........." } },
        ],
      },
      bridge: {
        chords: "4 | 1 | 6 | 5M", dyn: 0.6, dynTo: 0.85,
        layers: [
          { t: "line", i: "flute", o: 0, v: 0.65, notes: "4/2 6/2 1'/2 | 5/4 3/2 | 6/2 1'/2 3'/2 | 2'/6" },
          { t: "arp", i: "harp", o: 0, v: 0.55, p: "1.5.8." },
          { t: "arp", i: "celesta", o: 1, v: 0.3, p: "...5.." },
          { t: "pad", i: "choirOo", o: -1, v: 0.45, n: 3 },
          { t: "drone", i: "drone", o: -2, v: 0.45, deg: ["1"] },
          { t: "tex", kind: "wind", v: 0.4 },
        ],
      },
      A2: {
        like: "A", dyn: 1.12,
        layers: [
          { t: "mel", i: "sopranos", v: 0.85, dbl: [{ i: "choir", o: -1, v: 0.45 }, { i: "violins", o: 0, v: 0.3 }] },
          { t: "arp", i: "harp", o: 0, v: 0.6, p: "15T8T5" },
          { t: "pulse", i: "spiccato", o: 0, v: 0.35, p: "..5.8." },
          { t: "pad", i: "strings", o: -1, v: 0.45, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.55, p: "1-----" },
          { t: "drums", v: 0.7, p: { timp: "x.....x.....", frame: "x..x.." } },
        ],
      },
    },
    form: ["intro", "A", "B", "C", "bridge", "A2"], loop: 1,
    motif: "5/3 1'/2 3'/1 | 3'/4 1'/2",
  });

  // Tankina, Disciple of Talos: a storm ritual (D phrygian). Rain and thunder open it; a low choir
  // chants the rite on its repeated notes over war drums and crackling percussion; the brass rises
  // with the storm, and at the height sopranos and trumpets cry out over thunder.
  bosses.push({
    id: "tankina-disciple-of-talos", title: "Tankina · The Gathering Storm",
    tempo: 100, meter: [4, 2], tonic: 50, mode: "phrygian", room: "cathedral", gain: 1,
    sections: {
      intro: {
        chords: "1 | 2 | 1 | 7", dyn: 0.65, dynTo: 0.9,
        layers: [
          { t: "drone", i: "drone", o: -2, v: 0.55, deg: ["1", "5"] },
          { t: "tex", kind: "rain", v: 0.6 },
          { t: "pad", i: "choirOo", o: -1, v: 0.45, n: 3 },
          { t: "hit", i: "thunder", v: 0.7, bars: [0, 2] },
          { t: "drums", v: 0.7, p: { taiko: "X.......X...x...", clack: "......x......x.." } },
        ],
      },
      A: {
        chords: "1 | 2 | 1 | 7 | 1 | 2 | 6 | 1",
        mel: "1/2 1/2 3/2 1/2 | 2/2 2/2 4/2 2/2 | 3/2 1/2 5,/2 1/2 | 7,/4 2/4 | 5/2 5/2 3/2 1/2 | 6/2 4/2 2/2 4/2 | 3/2 1/2 6,/4 | 1/8",
        layers: [
          { t: "mel", i: "choir", o: -1, v: 0.85, dbl: [{ i: "trombones", o: -1, v: 0.35 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.7, p: "L.5.L.5." },
          { t: "bass", i: "contrabass", o: -2, v: 0.6, p: "1---1---" },
          { t: "tex", kind: "rain", v: 0.35 },
          { t: "hit", i: "thunder", v: 0.5, bars: [4] },
          { t: "drums", v: 0.85, p: { taiko: "X...x...X...x.x.", clack: "..x...x...x...x.", tek: ".x.x.x.x" } },
        ],
      },
      B: {
        chords: "6 | 7 | 1 | 2 | 6 | 7 | 2 | 1", dyn: 1.08,
        mel: "1'/4 6/2 3/2 | 7/4 5/2 4/2 | 1'/4 3'/2 5'/2 | 6'/2 4'/2 2'/4 | 3'/4 1'/2 6/2 | 2'/4 7/2 5/2 | 4'/2 2'/2 6/4 | 1'/8",
        layers: [
          { t: "mel", i: "horns", o: -1, v: 0.95, dbl: [{ i: "choir", o: -1, v: 0.45 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.8, p: "L5L5L8L5" },
          { t: "pad", i: "tremolo", o: -1, v: 0.35, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.65, p: "1-1-1-1-" },
          { t: "hit", i: "thunder", v: 0.6, bars: [0, 4] },
          { t: "drums", v: 0.95, p: { war: "X...x...X...x...", taiko: "x.x.x.x.", snare: "......xr", clack: "..x...x." } },
        ],
      },
      C: {
        chords: "1 | 2 | 6 | 7 | 1 | 2 | 7 | 1", dyn: 1.18,
        mel: "5'/2 1''/2 5'/2 3'/2 | 4'/2 2'/2 4'/2 6'/2 | 1''/4 6'/4 | 7'/4 5'/4 | 3''/2 1''/2 5'/2 1''/2 | 2''/4 6'/4 | 7'/2 5'/2 4'/2 2'/2 | 1''/8",
        layers: [
          { t: "mel", i: "sopranos", o: -1, v: 0.85, dbl: [{ i: "trumpet", o: -1, v: 0.45 }, { i: "horns", o: -2, v: 0.55 }, { i: "choir", o: -2, v: 0.45 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.9, p: "L5L8L5L8" },
          { t: "pad", i: "tremolo", o: -1, v: 0.45, n: 3 },
          { t: "bass", i: "trombones", o: -2, v: 0.55, p: "1---5---" },
          { t: "bass", i: "contrabass", o: -2, v: 0.75, p: "1-1-1-1-" },
          { t: "hit", i: "thunder", v: 0.75, bars: [0, 4] },
          { t: "hit", i: "crash", v: 0.5, bars: [0] },
          { t: "drums", v: 1.05, p: { war: "X.x.X.x.", taiko: "x.x.x.x.", snare: "..x...xr", tek: "x.x.x.x." } },
        ],
      },
      A2: {
        like: "A", dyn: 1.1,
        layers: [
          { t: "mel", i: "choir", o: -1, v: 0.9, dbl: [{ i: "horns", o: -1, v: 0.45 }, { i: "trombones", o: -1, v: 0.35 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.8, p: "L5L5L8L5" },
          { t: "bass", i: "contrabass", o: -2, v: 0.65, p: "1-1-1-1-" },
          { t: "tex", kind: "rain", v: 0.4 },
          { t: "hit", i: "thunder", v: 0.55, bars: [0, 4] },
          { t: "drums", v: 0.95, p: { war: "X...x...X...x...", taiko: "x.x.x.x.", clack: "..x...x." } },
        ],
      },
    },
    form: ["intro", "A", "B", "C", "A2"], loop: 1,
    motif: "1/2 1/2 3/2 1/2 | 2/2 2/2 4/2 2/2",
  });

  // Tankina, Chosen of Talos: the rite turned wild. The same chant, in D hijaz (phrygian dominant)
  // and nearly half again as fast: thunder and timpani rolls, brass hammering the chant, sopranos
  // screaming it an octave higher, a breath where only the choir and the thunder remain.
  bosses.push({
    id: "tankina-chosen-of-talos", title: "Tankina · Chosen of the Storm",
    tempo: 138, meter: [4, 2], tonic: 50, mode: "hijaz", room: "cathedral", gain: 1,
    sections: {
      intro: {
        chords: "1 | 2 | 1 | 2", dyn: 0.8, dynTo: 1.1,
        layers: [
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.75, p: "LLLLLLLL" },
          { t: "bass", i: "trombones", o: -2, v: 0.55, p: "1-------" },
          { t: "tex", kind: "rain", v: 0.6 },
          { t: "hit", i: "thunder", v: 0.9, bars: [0, 2] },
          { t: "hit", i: "swell", v: 0.6, bars: [3], len: 8 },
          { t: "drums", v: 1, p: { timp: "x.x.x.x.rrrrrrrr", war: "X.......X......." } },
        ],
      },
      A: {
        chords: "1 | 2 | 1 | 7 | 4 | 2 | 1 | 1", dyn: 1.1,
        mel: "1/1 1/1 3/2 1/1 2/1 1/2 | 2/1 2/1 4/2 2/1 1/1 2/2 | 3/2 5/2 3/2 1/2 | 7,/2 2/2 4/2 2/2 | 4/2 6/2 1'/2 6/2 | 6/2 4/2 2/2 4/2 | 3/1 2/1 1/2 5,/2 1/2 | 1/8",
        layers: [
          { t: "mel", i: "horns", v: 1, dbl: [{ i: "trombones", o: -1, v: 0.6 }, { i: "choir", o: 0, v: 0.5 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.9, p: "L5L5L8L5" },
          { t: "pulse", i: "spiccato", o: 0, v: 0.45, p: "35835T85" },
          { t: "bass", i: "contrabass", o: -2, v: 0.75, p: "1-1-1-1-" },
          { t: "hit", i: "thunder", v: 0.7, bars: [0, 4] },
          { t: "drums", v: 1.1, p: { war: "X.x.X.x.X.x.X.x.", taiko: "x.x.x.x.", snare: "....x..r", tek: "x.x.x.x." } },
        ],
      },
      B: {
        like: "A", dyn: 1.2,
        layers: [
          { t: "mel", i: "sopranos", o: 1, v: 0.85, dbl: [{ i: "trumpet", o: 0, v: 0.45 }, { i: "horns", o: -1, v: 0.6 }, { i: "choir", o: -1, v: 0.5 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.95, p: "L5L8L5L8" },
          { t: "pad", i: "tremolo", o: -1, v: 0.45, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.8, p: "1-1-1-1-" },
          { t: "hit", i: "thunder", v: 0.8, bars: [0, 2, 4, 6] },
          { t: "hit", i: "crash", v: 0.55, bars: [0, 4] },
          { t: "drums", v: 1.15, p: { war: "X.x.X.x.", taiko: "x.x.x.x.", snare: "..x...xr", timp: "x...x...x...x.rr" } },
        ],
      },
      breakdown: {
        chords: "1 | 2 | 7 | 1", dyn: 0.75, dynTo: 1.05,
        layers: [
          { t: "line", i: "choir", o: -1, v: 0.85, notes: "1/2 1/2 3/2 1/2 | 2/2 2/2 4/2 2/2 | 7,/4 2/4 | 1/8" },
          { t: "drone", i: "drone", o: -2, v: 0.55, deg: ["1"] },
          { t: "tex", kind: "rain", v: 0.6 },
          { t: "hit", i: "thunder", v: 0.85, bars: [0, 2] },
          { t: "drums", v: 0.8, p: { timp: "x.......x.....rr" } },
        ],
      },
      A2: { like: "A", dyn: 1.22 },
    },
    form: ["intro", "A", "B", "breakdown", "A2"], loop: 1,
    motif: "1/1 1/1 3/2 1/1 2/1 1/2 | 2/1 2/1 4/2 2/1 1/1 2/2",
  });

  // Archdevil Saffron: a royal processional with a hellish undertone (D hijaz). Brass fanfares and
  // timpani crown him, horns and strings carry a stately melody over the organ, and beneath the
  // pomp a low choir chants in the dark; the court returns in full splendor.
  bosses.push({
    id: "archdevil-saffron", title: "Archdevil Saffron · Court of the Burning Throne",
    tempo: 88, meter: [4, 2], tonic: 50, mode: "hijaz", room: "cathedral", gain: 1,
    sections: {
      intro: {
        chords: "1 | 2 | 1 | 1", dyn: 0.8, dynTo: 1.05,
        layers: [
          { t: "line", i: "trumpet", o: 0, v: 0.85, notes: "5/2 5/1 5/1 1'/4 | 6/2 4/2 2/4 | 5/2 5/1 5/1 1'/4 | 1'/8" },
          { t: "drone", i: "organ", o: -1, v: 0.55, deg: ["1", "5"] },
          { t: "pad", i: "choirOo", o: -2, v: 0.35, n: 3 },
          { t: "hit", i: "swell", v: 0.5, bars: [3], len: 8 },
          { t: "drums", v: 0.95, p: { timp: "x.......x.....rr", war: "X.......X......." } },
        ],
      },
      A: {
        chords: "1 | 2 | 4 | 1 | 7 | 4 | 2 | 1",
        mel: "5/2 5/1 5/1 1'/4 | 6/2 4/2 2/4 | 4/2 6/2 1'/2 6/2 | 5/4 3/4 | 4/2 2/2 7,/4 | 6/2 4/2 1/4 | 2/2 4/2 6/2 4/2 | 1/8",
        layers: [
          { t: "mel", i: "horns", o: 0, v: 0.95, dbl: [{ i: "violins", o: 0, v: 0.45 }, { i: "trombones", o: -1, v: 0.35 }] },
          { t: "pad", i: "organ", o: -1, v: 0.4, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.6, p: "1---5---" },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.5, p: "L.5.L.5." },
          { t: "drums", v: 0.95, p: { timp: "x...x...", snare: "......xr", war: "X.......", zill: "....x..." } },
        ],
      },
      B: {
        chords: "4 | 2 | 1 | 7 | 4 | 2 | 1 | 1", dyn: 0.92,
        mel: "1/2 6,/2 1/2 4,/2 | 2/4 6,/2 4,/2 | 1/2 5,/2 3/4 | 2/2 7,/2 4,/4 | 4/4 6/4 | 6/2 4/2 2/2 4/2 | 3/2 5,/2 1/4 | 1/8",
        layers: [
          { t: "mel", i: "choir", o: -1, v: 0.9, dbl: [{ i: "trombones", o: -1, v: 0.45 }, { i: "duduk", o: 0, v: 0.35 }] },
          { t: "drone", i: "drone", o: -2, v: 0.55, deg: ["1"] },
          { t: "pad", i: "organ", o: -1, v: 0.35, n: 3 },
          { t: "tex", kind: "embers", v: 0.4 },
          { t: "drums", v: 0.85, p: { war: "X.......X...x...", frame: "....x.......x.x.", timp: "x.......x......." } },
        ],
      },
      A2: {
        like: "A", dyn: 1.15,
        layers: [
          { t: "mel", i: "trumpet", o: 0, v: 0.75, dbl: [{ i: "horns", o: 0, v: 0.7 }, { i: "sopranos", o: 0, v: 0.45 }, { i: "trombones", o: -1, v: 0.45 }] },
          { t: "pad", i: "organ", o: -1, v: 0.45, n: 3 },
          { t: "pad", i: "choir", o: -1, v: 0.35, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.65, p: "1-1-5-1-" },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.6, p: "L5L5L8L5" },
          { t: "hit", i: "crash", v: 0.45, bars: [0, 4] },
          { t: "drums", v: 1.05, p: { timp: "x...x...", snare: "..x...xr", war: "X...x...", zill: "....x..." } },
        ],
      },
    },
    form: ["intro", "A", "B", "A2"], loop: 1,
    motif: "5/2 5/1 5/1 1'/4 | 6/2 4/2 2/4",
  });

  // Anatemis, the White Star: a celestial hymn, solemn and cold (D dorian, 66 bpm). Sopranos sing
  // the hymn over celesta, harp and glass, an organ holds the vault of the sky, and wind crosses the
  // summit.
  bosses.push({
    id: "anatemis-white-star", title: "Anatemis · Hymn of the White Star",
    tempo: 66, meter: [4, 2], tonic: 50, mode: "dorian", room: "cathedral", gain: 1,
    sections: {
      intro: {
        chords: "1 | 7 | 4 | 1", dyn: 0.6, dynTo: 0.85,
        layers: [
          { t: "arp", i: "celesta", o: 1, v: 0.55, p: "15851585" },
          { t: "pad", i: "glass", o: 1, v: 0.4, n: 3 },
          { t: "drone", i: "organ", o: -2, v: 0.4, deg: ["1", "5"] },
          { t: "tex", kind: "wind", v: 0.5 },
        ],
      },
      A: {
        chords: "1 | 7 | 4 | 1 | 3 | 7 | 5 | 5",
        mel: "1/2 5/2 1'/4 | 7/4 5/4 | 4/2 6/2 1'/4 | 1'/2 7/2 5/4 | 3'/4 1'/2 7/2 | 5/4 7,/4 | 5/2 2'/2 1'/4 | 5/8",
        layers: [
          { t: "mel", i: "sopranos", o: 0, v: 0.9, dbl: [{ i: "celesta", o: 1, v: 0.35 }] },
          { t: "arp", i: "harp", o: 0, v: 0.5, p: "1.5.8.5." },
          { t: "pad", i: "organ", o: -1, v: 0.35, n: 3 },
          { t: "bass", i: "cellos", o: -2, v: 0.5, p: "1-------" },
          { t: "pulse", i: "glass", o: 1, v: 0.2, p: "....5..." },
        ],
      },
      B: {
        chords: "3 | 7 | 1 | 5 | 3 | 4 | 5 | 1", dyn: 1.05,
        mel: "1'/4 3'/4 | 2'/4 7/4 | 1'/4 5/4 | 5/4 1'/4 | 3'/4 5'/4 | 4'/4 2'/4 | 1'/4 7/4 | 1'/8",
        layers: [
          { t: "mel", i: "choir", o: -1, v: 0.9, dbl: [{ i: "sopranos", o: 0, v: 0.35 }] },
          { t: "pad", i: "organ", o: -1, v: 0.45, n: 3 },
          { t: "pad", i: "strings", o: -1, v: 0.3, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.55, p: "1---5---" },
          { t: "hit", i: "toll", deg: "1", o: 0, v: 0.45, bars: [0, 4] },
          { t: "drums", v: 0.6, p: { timp: "x......." } },
        ],
      },
      A2: {
        like: "A", dyn: 1.12,
        layers: [
          { t: "mel", i: "sopranos", o: 0, v: 0.9, dbl: [{ i: "violins", o: 0, v: 0.4 }, { i: "celesta", o: 1, v: 0.3 }] },
          { t: "arp", i: "harp", o: 0, v: 0.5, p: "1.5.8.5." },
          { t: "pad", i: "organ", o: -1, v: 0.45, n: 3 },
          { t: "pad", i: "choirOo", o: -1, v: 0.35, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.55, p: "1---5---" },
          { t: "hit", i: "swell", v: 0.4, bars: [7], len: 8 },
          { t: "drums", v: 0.7, p: { timp: "x.......", } },
        ],
      },
    },
    form: ["intro", "A", "B", "A2"], loop: 1,
    motif: "1/2 5/2 1'/4 | 7/4 5/4",
  });

  // The Golden Star: the hymn becomes a radiant march (D major, 104 bpm). Trumpets and horns carry
  // it over snare and timpani, the choir and strings answer in the trio.
  bosses.push({
    id: "anatemis-golden-star", title: "Anatemis · March of the Golden Star",
    tempo: 104, meter: [4, 2], tonic: 50, mode: "ionian", room: "cathedral", gain: 1,
    sections: {
      intro: {
        chords: "1 | 5", dyn: 0.85, dynTo: 1.0,
        layers: [
          { t: "line", i: "trumpet", o: 0, v: 0.8, notes: "1/1 1/1 1/2 5/4 | 5/2 7/2 2'/4" },
          { t: "pad", i: "strings", o: -1, v: 0.45, n: 3 },
          { t: "drums", v: 0.9, p: { timp: "x.......", snare: "....xxrr" } },
        ],
      },
      A: {
        chords: "1 | 4 | 5 | 1 | 6 | 4 | 5 | 1",
        mel: "1/2 5/2 1'/4 | 1'/4 6/4 | 5/2 7/2 2'/4 | 1'/8 | 6/2 1'/2 3'/4 | 4'/2 2'/2 1'/4 | 2'/2 7/2 5/4 | 1'/8",
        layers: [
          { t: "mel", i: "trumpet", o: 0, v: 0.85, dbl: [{ i: "horns", o: -1, v: 0.5 }, { i: "violins", o: 0, v: 0.35 }] },
          { t: "pad", i: "strings", o: -1, v: 0.4, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.6, p: "1---5---" },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.5, p: "L.5.L.5." },
          { t: "drums", v: 0.95, p: { snare: "x.xxx.xx", timp: "x...x...", crash: "........" } },
        ],
      },
      B: {
        chords: "4 | 1 | 5 | 6 | 4 | 1 | 5 | 5", dyn: 0.95,
        mel: "4/4 6/4 | 5/4 3/4 | 5/2 6/2 7/4 | 1'/4 6/4 | 2'/4 1'/4 | 1'/4 5/4 | 7/4 2'/4 | 5/8",
        layers: [
          { t: "mel", i: "choir", o: -1, v: 0.9, dbl: [{ i: "horns", o: -1, v: 0.4 }] },
          { t: "pad", i: "organ", o: -1, v: 0.4, n: 3 },
          { t: "arp", i: "harp", o: 0, v: 0.4, p: "15851585" },
          { t: "bass", i: "cellos", o: -2, v: 0.55, p: "1-5-1-5-" },
          { t: "drums", v: 0.8, p: { snare: "x...x.xr", timp: "x......." } },
        ],
      },
      A2: {
        like: "A", dyn: 1.12,
        layers: [
          { t: "mel", i: "trumpet", o: 0, v: 0.8, dbl: [{ i: "horns", o: -1, v: 0.55 }, { i: "sopranos", o: 0, v: 0.35 }, { i: "trombones", o: -1, v: 0.4 }] },
          { t: "pad", i: "strings", o: -1, v: 0.45, n: 3 },
          { t: "pad", i: "choir", o: -1, v: 0.35, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.65, p: "1-1-5-1-" },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.55, p: "L5L5L5L5" },
          { t: "hit", i: "crash", v: 0.45, bars: [0, 4] },
          { t: "drums", v: 1.0, p: { snare: "x.xxx.xx", timp: "x...x..." } },
        ],
      },
    },
    form: ["intro", "A", "B", "A2"], loop: 1,
    motif: "1/2 5/2 1'/4 | 1'/4 6/4",
  });

  // The Zenith of Dragonkind: the hymn's overwhelming finale (D major, 128 bpm). Full choir and
  // organ, brass and strings, timpani rolls and crashes: the heavens answer.
  bosses.push({
    id: "anatemis-zenith", title: "Anatemis · Zenith of Dragonkind",
    tempo: 128, meter: [4, 2], tonic: 50, mode: "ionian", room: "cathedral", gain: 1,
    sections: {
      intro: {
        chords: "5 | 5", dyn: 0.95, dynTo: 1.1,
        layers: [
          { t: "pad", i: "choir", o: -1, v: 0.6, n: 3 },
          { t: "pad", i: "tremolo", o: -1, v: 0.5, n: 3 },
          { t: "hit", i: "swell", v: 0.55, bars: [1], len: 8 },
          { t: "drums", v: 1.0, p: { timp: "rrrrrrrr" } },
        ],
      },
      A: {
        chords: "1 | 4 | 5 | 1 | 6 | 4 | 5 | 1",
        mel: "1/2 5/2 1'/4 | 1'/4 6/4 | 5/2 7/2 2'/4 | 1'/8 | 6/2 1'/2 3'/4 | 4'/2 2'/2 1'/4 | 2'/2 7/2 5/4 | 1'/8",
        layers: [
          { t: "mel", i: "choir", o: 0, v: 0.95, dbl: [{ i: "trumpet", o: 0, v: 0.45 }, { i: "sopranos", o: 1, v: 0.3 }, { i: "trombones", o: -1, v: 0.45 }] },
          { t: "pad", i: "organ", o: -1, v: 0.5, n: 3 },
          { t: "pad", i: "tremolo", o: -1, v: 0.4, n: 3 },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.6, p: "L5L5L8L5" },
          { t: "bass", i: "contrabass", o: -2, v: 0.65, p: "1-1-1-1-" },
          { t: "hit", i: "crash", v: 0.45, bars: [0, 4] },
          { t: "drums", v: 1.05, p: { timp: "x.x.x.rr", war: "X...X...", snare: "..x...x." } },
        ],
      },
      B: {
        chords: "4 | 1 | 5 | 6 | 4 | 1 | 5 | 5", dyn: 1.0,
        mel: "4/4 6/4 | 5/4 3/4 | 5/2 6/2 7/4 | 1'/4 6/4 | 2'/4 1'/4 | 1'/4 5/4 | 7/4 2'/4 | 5/8",
        layers: [
          { t: "mel", i: "horns", o: -1, v: 0.85, dbl: [{ i: "violins", o: 0, v: 0.5 }, { i: "choir", o: -1, v: 0.4 }] },
          { t: "pad", i: "organ", o: -1, v: 0.45, n: 3 },
          { t: "pulse", i: "spiccato", o: 0, v: 0.5, p: "15851585" },
          { t: "bass", i: "contrabass", o: -2, v: 0.6, p: "1-5-1-5-" },
          { t: "drums", v: 0.95, p: { timp: "x...x.rr", war: "X......." } },
        ],
      },
      A2: {
        like: "A", dyn: 1.15,
      },
    },
    form: ["intro", "A", "B", "A2"], loop: 1,
    motif: "1/2 5/2 1'/4 | 1'/4 6/4",
  });

  // Ur-Mat, the False Dreamer: a dream waltz (E harmonic minor, 3/4). A music box of celesta and
  // harp turns alone in the dark, then the dream swells: dark organ, a low choir and strings
  // carry the waltz, and the music box returns over all of it.
  bosses.push({
    id: "ur-mat-false-dreamer", title: "Ur-Mat · The Dream Waltz",
    tempo: 84, meter: [3, 2], tonic: 52, mode: "harmonic", room: "cathedral", gain: 1,
    sections: {
      intro: {
        chords: "1 | 4 | 5M | 1", dyn: 0.6, dynTo: 0.8,
        layers: [
          { t: "arp", i: "celesta", o: 1, v: 0.7, p: "158T85" },
          { t: "drone", i: "choirOo", o: -1, v: 0.35, deg: ["1"] },
          { t: "pulse", i: "glass", o: 2, v: 0.25, p: "..5..." },
        ],
      },
      A: {
        chords: "1 | 4 | 5M | 1 | 6 | 4 | 5M | 5M",
        mel: "5/2 3/2 1/2 | 4/2 6/2 1'/2 | 7/3 2'/1 7/2 | 1'/6 | 3'/2 1'/2 6/2 | 1'/3 7/1 6/2 | 5/2 7/2 2'/2 | 5/6",
        layers: [
          { t: "mel", i: "celesta", o: 1, v: 0.85, dbl: [{ i: "harp", o: 0, v: 0.4 }] },
          { t: "bass", i: "cellos", o: -2, v: 0.55, p: "L....." },
          { t: "pulse", i: "pizz", o: 0, v: 0.45, p: "..3.5." },
          { t: "pad", i: "strings", o: -1, v: 0.3, n: 3 },
        ],
      },
      B: {
        chords: "6 | 4 | 1 | 5M | 6 | 4 | 5M | 1", dyn: 1.05,
        mel: "3/4 1'/2 | 6/4 4/2 | 5/4 3/2 | 7/4 5/2 | 1'/2 6/2 3/2 | 1'/2 4/2 6/2 | 2'/4 7/2 | 1'/6",
        layers: [
          { t: "mel", i: "choir", o: -1, v: 0.9, dbl: [{ i: "cellos", o: -1, v: 0.4 }, { i: "violins", o: 0, v: 0.35 }] },
          { t: "pad", i: "organ", o: -1, v: 0.45, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.6, p: "L...P." },
          { t: "pulse", i: "harp", o: 0, v: 0.4, p: "1.3.5." },
          { t: "drums", v: 0.75, p: { timp: "x....." } },
        ],
      },
      A2: {
        like: "A", dyn: 1.12,
        layers: [
          { t: "mel", i: "celesta", o: 1, v: 0.8, dbl: [{ i: "violins", o: 0, v: 0.45 }, { i: "sopranos", o: 0, v: 0.3 }] },
          { t: "pad", i: "organ", o: -1, v: 0.4, n: 3 },
          { t: "pad", i: "choirOo", o: -1, v: 0.35, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.6, p: "L....." },
          { t: "pulse", i: "pizz", o: 0, v: 0.45, p: "..3.5." },
          { t: "hit", i: "swell", v: 0.4, bars: [7], len: 6 },
          { t: "drums", v: 0.8, p: { timp: "x....." } },
        ],
      },
    },
    form: ["intro", "A", "B", "A2"], loop: 1,
    motif: "5/2 3/2 1/2 | 4/2 6/2 1'/2",
  });

  // Lucid Change: the dream shatters and the waltz races (152 bpm). Shards of glass and celesta,
  // tremolo strings and driving spiccato under the same melody, a darker turn through the
  // Neapolitan, and timpani like a heart that will not last.
  bosses.push({
    id: "ur-mat-lucid-change", title: "Ur-Mat · Lucid Change",
    tempo: 152, meter: [3, 2], tonic: 52, mode: "harmonic", room: "cathedral", gain: 1,
    sections: {
      intro: {
        chords: "5M | 5M", dyn: 0.9, dynTo: 1.05,
        layers: [
          { t: "pad", i: "tremolo", o: -1, v: 0.55, n: 3 },
          { t: "pulse", i: "glass", o: 2, v: 0.4, p: "5.3.1." },
          { t: "hit", i: "swell", v: 0.5, bars: [1], len: 6 },
          { t: "drums", v: 0.95, p: { timp: "rrrrrr" } },
        ],
      },
      A: {
        chords: "1 | 4 | 5M | 1 | 6 | 4 | 5M | 5M",
        mel: "5/2 3/2 1/2 | 4/2 6/2 1'/2 | 7/3 2'/1 7/2 | 1'/6 | 3'/2 1'/2 6/2 | 1'/3 7/1 6/2 | 5/2 7/2 2'/2 | 5/6",
        layers: [
          { t: "mel", i: "violins", o: 0, v: 0.85, dbl: [{ i: "celesta", o: 1, v: 0.4 }, { i: "choir", o: -1, v: 0.35 }] },
          { t: "pad", i: "tremolo", o: -1, v: 0.45, n: 3 },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.6, p: "L5L5L5" },
          { t: "bass", i: "contrabass", o: -2, v: 0.6, p: "1.1.5." },
          { t: "pulse", i: "glass", o: 2, v: 0.25, p: "...5.." },
          { t: "drums", v: 1.0, p: { timp: "x.x.rr", war: "X....." } },
        ],
      },
      B: {
        chords: "6 | 5M | 4 | 5M | 6 | b2 | 5M | 1", dyn: 1.05,
        mel: "1'/2 6/2 3/2 | 7/2 5/2 2'/2 | 6/2 4/2 1'/2 | 5/2 7/2 2'/2 | 3'/2 1'/2 6/2 | 4/2 6/2 b2'/2 | 7/2 2'/2 5/2 | 1'/6",
        layers: [
          { t: "mel", i: "trumpet", o: 0, v: 0.75, dbl: [{ i: "violins", o: 0, v: 0.5 }, { i: "trombones", o: -1, v: 0.4 }] },
          { t: "pad", i: "organ", o: -1, v: 0.45, n: 3 },
          { t: "pulse", i: "spiccato", o: 0, v: 0.55, p: "158158" },
          { t: "bass", i: "contrabass", o: -2, v: 0.65, p: "1.1.1." },
          { t: "hit", i: "crash", v: 0.4, bars: [0, 4] },
          { t: "drums", v: 1.05, p: { timp: "x.x.x.", war: "X..X..", snare: "..x..x" } },
        ],
      },
      A2: {
        like: "A", dyn: 1.15,
        layers: [
          { t: "mel", i: "violins", o: 0, v: 0.85, dbl: [{ i: "sopranos", o: 1, v: 0.3 }, { i: "trumpet", o: 0, v: 0.35 }, { i: "celesta", o: 1, v: 0.35 }] },
          { t: "pad", i: "tremolo", o: -1, v: 0.5, n: 3 },
          { t: "pad", i: "organ", o: -1, v: 0.4, n: 3 },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.65, p: "L5L8L5" },
          { t: "bass", i: "contrabass", o: -2, v: 0.65, p: "1.1.5." },
          { t: "drums", v: 1.05, p: { timp: "x.x.rr", war: "X..X..", snare: "....x." } },
        ],
      },
    },
    form: ["intro", "A", "B", "A2"], loop: 1,
    motif: "5/2 3/2 1/2 | 4/2 6/2 1'/2",
  });

  // Ancient Lich Ool: a requiem of the dead (D aeolian, 58 bpm). A funeral bell tolls, a deep
  // choir sings the old Dies Irae over organ and low strings, and a muffled funeral drum walks
  // beneath; the organ answers with a lament while ghostly sopranos hover above it.
  bosses.push({
    id: "ancient-lich-ool", title: "Ancient Lich Ool · Requiem of the Dead",
    tempo: 58, meter: [4, 2], tonic: 50, mode: "aeolian", room: "cathedral", gain: 1,
    sections: {
      intro: {
        chords: "1 | 6 | 4 | 5M", dyn: 0.62, dynTo: 0.85,
        layers: [
          { t: "hit", i: "toll", deg: "1", o: -1, v: 0.75, bars: [0, 1, 2, 3] },
          { t: "drone", i: "organ", o: -2, v: 0.5, deg: ["1", "5"] },
          { t: "pad", i: "choirOo", o: -2, v: 0.4, n: 3 },
          { t: "tex", kind: "wind", v: 0.55 },
          { t: "drums", v: 0.7, p: { muffled: "X......." } },
        ],
      },
      A: {
        chords: "1 | 7 . 1 . | 6 | 7 . 1 . | 4 . 7 . | 7 . 6 . | 4 . 5M . | 1",
        mel: "3/3 2/1 3/2 1/2 | 2/2 7,/2 1/4 | 3/2 3/2 4/2 3/2 | 2/2 1/2 7,/2 1/2 | 2/2 3/2 2/2 1/2 | 7,/2 1/2 6,/4 | 1/2 2/2 #7,/2 5,/2 | 1/8",
        layers: [
          { t: "mel", i: "choir", o: -1, v: 0.95, dbl: [{ i: "cellos", o: -1, v: 0.4 }] },
          { t: "pad", i: "organ", o: -1, v: 0.42, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.6, p: "1-------" },
          { t: "hit", i: "toll", deg: "1", o: -1, v: 0.65, bars: [0, 4] },
          { t: "drums", v: 0.8, p: { muffled: "X...x...", timp: "x......." } },
        ],
      },
      B: {
        chords: "6 | 3 | 7 | 1 | 6 | 4 | 5M | 5M", dyn: 0.9,
        mel: "1'/4 6/4 | 5/4 3/4 | 4/2 2/2 7,/4 | 1/6 2/1 3/1 | 4/2 3/2 1/4 | 6,/2 7,/2 1/2 4/2 | #7,/2 2/2 5,/4 | 2/4 #7,/4",
        layers: [
          { t: "mel", i: "organ", o: 0, v: 0.8, dbl: [{ i: "sopranos", o: 1, v: 0.3 }] },
          { t: "pad", i: "tremolo", o: -1, v: 0.38, n: 3 },
          { t: "drone", i: "choirOo", o: -2, v: 0.45, deg: ["1"] },
          { t: "bass", i: "cellos", o: -2, v: 0.5, p: "1---5---" },
          { t: "hit", i: "toll", deg: "5", o: -1, v: 0.55, bars: [0, 4] },
          { t: "tex", kind: "wind", v: 0.35 },
          { t: "drums", v: 0.7, p: { muffled: "X......." } },
        ],
      },
      A2: {
        like: "A", dyn: 1.12,
        layers: [
          { t: "mel", i: "choir", o: -1, v: 0.95, dbl: [{ i: "trombones", o: -1, v: 0.4 }, { i: "sopranos", o: 0, v: 0.35 }, { i: "cellos", o: -1, v: 0.35 }] },
          { t: "pad", i: "organ", o: -1, v: 0.5, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.65, p: "1---1---" },
          { t: "hit", i: "toll", deg: "1", o: -1, v: 0.7, bars: [0, 2, 4, 6] },
          { t: "hit", i: "boom", v: 0.45, bars: [0] },
          { t: "drums", v: 0.9, p: { muffled: "X...x...", timp: "x.....rr" } },
        ],
      },
    },
    form: ["intro", "A", "B", "A2"], loop: 1,
    motif: "3/3 2/1 3/2 1/2 | 2/2 7,/2 1/4",
  });

  // Ool made mortal: the requiem breaks into a frantic dirge (D harmonic minor, 132 bpm). The same
  // Dies Irae is hurled out over tremolo strings and driving spiccato, war drums and timpani
  // pound, and the funeral bell is struck again and again.
  bosses.push({
    id: "ancient-lich-ool-mortal", title: "Ancient Lich Ool · The Frantic Dirge",
    tempo: 132, meter: [4, 2], tonic: 50, mode: "harmonic", room: "cathedral", gain: 1,
    sections: {
      intro: {
        chords: "5 | 5", dyn: 0.9, dynTo: 1.05,
        layers: [
          { t: "hit", i: "toll", deg: "5", o: -1, v: 0.75, bars: [0, 1] },
          { t: "pad", i: "tremolo", o: -1, v: 0.55, n: 3 },
          { t: "hit", i: "swell", v: 0.5, bars: [1], len: 8 },
          { t: "drums", v: 0.95, p: { timp: "rrrrrrrr", war: "X......." } },
        ],
      },
      A: {
        chords: "1 | 5 . 1 . | 1 | 5 1 5 1 | 6 | 4 | 5 | 1",
        mel: "3/2 2/1 3/1 1/4 | 2/2 7,/2 1/4 | 3/2 3/2 4/2 3/2 | 2/2 1/2 7,/2 1/2 | 6/2 5/1 6/1 1'/4 | 6/2 5/1 4/1 2/4 | 7,/2 2/2 5/2 7/2 | 1'/4 1/4",
        layers: [
          { t: "mel", i: "choir", o: 0, v: 0.9, dbl: [{ i: "trombones", o: -1, v: 0.45 }, { i: "violins", o: 0, v: 0.35 }] },
          { t: "pad", i: "tremolo", o: -1, v: 0.45, n: 3 },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.6, p: "L5L5L5L5" },
          { t: "bass", i: "contrabass", o: -2, v: 0.6, p: "1-1-1-1-" },
          { t: "hit", i: "toll", deg: "1", o: -1, v: 0.6, bars: [0, 2, 4, 6] },
          { t: "drums", v: 1.0, p: { war: "X.x.X.x.", timp: "x...x.rr", snare: "..x...x." } },
        ],
      },
      B: {
        chords: "1 | b2 | 1 | b2 | 6 | 4 | 5 | 5", dyn: 0.95,
        mel: "5/4 3/4 | b2/4 4/4 | 1/4 3/4 | 6/4 4/4 | 1'/4 6/4 | 2'/4 1'/4 | 7/4 2'/4 | 5/8",
        layers: [
          { t: "mel", i: "choir", o: -1, v: 0.9, dbl: [{ i: "horns", o: -1, v: 0.4 }] },
          { t: "pad", i: "organ", o: -1, v: 0.4, n: 3 },
          { t: "pulse", i: "spiccato", o: 0, v: 0.55, p: "15851585" },
          { t: "bass", i: "contrabass", o: -2, v: 0.6, p: "1-1-5-1-" },
          { t: "hit", i: "toll", deg: "5", o: -1, v: 0.55, bars: [0, 4] },
          { t: "drums", v: 0.95, p: { taiko: "X..x.X..", timp: "x.......", hat: "x.x.x.x." } },
        ],
      },
      A2: {
        like: "A", dyn: 1.15,
        layers: [
          { t: "mel", i: "choir", o: 0, v: 0.95, dbl: [{ i: "sopranos", o: 1, v: 0.35 }, { i: "trombones", o: -1, v: 0.5 }, { i: "horns", o: 0, v: 0.45 }] },
          { t: "pad", i: "tremolo", o: -1, v: 0.5, n: 3 },
          { t: "pad", i: "organ", o: -1, v: 0.4, n: 3 },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.65, p: "L5L5L8L5" },
          { t: "bass", i: "contrabass", o: -2, v: 0.65, p: "1-1-1-1-" },
          { t: "hit", i: "toll", deg: "1", o: -1, v: 0.65, bars: [0, 2, 4, 6] },
          { t: "hit", i: "crash", v: 0.45, bars: [0, 4] },
          { t: "drums", v: 1.05, p: { war: "X.x.X.x.", timp: "x.x.x.rr", snare: "..x..xx." } },
        ],
      },
    },
    form: ["intro", "A", "B", "A2"], loop: 1,
    motif: "3/2 2/1 3/1 1/4 | 2/2 7,/2 1/4",
  });

  // Gaian, Avatar of the Land: the heart of the jungle (E dorian). Earth drums beat like a heart,
  // a low choir drones, marimba and kalimba weave under a duduk's melody, and the strings rise.
  bosses.push({
    id: "gaian-avatar-of-the-land", title: "Gaian · Heart of the Jungle",
    tempo: 84, meter: [4, 2], tonic: 52, mode: "dorian", room: "forest", gain: 1,
    sections: {
      intro: {
        chords: "1 | 1 | 7 | 1", dyn: 0.7, dynTo: 0.95,
        layers: [
          { t: "drone", i: "choirOo", o: -1, v: 0.6, deg: ["1", "5"] },
          { t: "tex", kind: "leaves", v: 0.5 },
          { t: "pulse", i: "kalimba", o: 1, v: 0.45, p: "1.5.8.5." },
          { t: "drums", v: 0.85, p: { taiko: "X.x.....", frame: "......x." } },
        ],
      },
      A: {
        chords: "1 | 4 | 7 | 1 | 1 | 4 | 5 | 1",
        mel: "5/2 4/2 5/4 | 6/4 4/4 | 2/2 4/2 2/4 | 1/8 | 5/2 4/2 5/2 7/2 | 1'/4 6/4 | 5/2 4/2 2/4 | 1/8",
        layers: [
          { t: "mel", i: "duduk", o: 0, v: 0.9, dbl: [{ i: "choirOo", o: -1, v: 0.35 }] },
          { t: "pulse", i: "marimba", o: 0, v: 0.6, p: "15851585" },
          { t: "pulse", i: "kalimba", o: 1, v: 0.35, p: ".3.5.3.8" },
          { t: "bass", i: "contrabass", o: -2, v: 0.55, p: "1---5---" },
          { t: "tex", kind: "leaves", v: 0.3 },
          { t: "drums", v: 0.95, p: { taiko: "X.x.....X.x.....", frame: "....x.......x.x.", shaker: ".x.x.x.x" } },
        ],
      },
      B: {
        chords: "4 | 5 | 1 | 7 | 4 | 5 | 7 | 1", dyn: 1.1,
        mel: "1'/4 6/4 | 5/2 7/2 5/4 | 1'/2 3'/2 1'/2 5/2 | 7/8 | 1'/4 6/4 | 5/2 7/2 2'/4 | 2'/4 7/4 | 1'/8",
        layers: [
          { t: "mel", i: "violins", o: 0, v: 0.85, dbl: [{ i: "horns", o: -1, v: 0.45 }, { i: "choir", o: -1, v: 0.4 }] },
          { t: "pulse", i: "marimba", o: 0, v: 0.65, p: "15851585" },
          { t: "pad", i: "strings", o: -1, v: 0.4, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.6, p: "1-1-5-1-" },
          { t: "hit", i: "boom", v: 0.45, bars: [0, 4] },
          { t: "drums", v: 1.05, p: { taiko: "X.x.....X.x...x.", tom: "....x.x.", frame: "..x...x.", shaker: ".x.x.x.x" } },
        ],
      },
      A2: {
        like: "A", dyn: 1.12,
        layers: [
          { t: "mel", i: "duduk", o: 0, v: 0.9, dbl: [{ i: "violins", o: 0, v: 0.4 }, { i: "choirOo", o: -1, v: 0.45 }] },
          { t: "pulse", i: "marimba", o: 0, v: 0.65, p: "15851585" },
          { t: "pulse", i: "kalimba", o: 1, v: 0.4, p: ".3.5.3.8" },
          { t: "bass", i: "contrabass", o: -2, v: 0.6, p: "1-1-5-1-" },
          { t: "drums", v: 1.05, p: { taiko: "X.x.....X.x...x.", tom: "....x.x.", shaker: ".x.x.x.x" } },
        ],
      },
    },
    form: ["intro", "A", "B", "A2"], loop: 1,
    motif: "5/2 4/2 5/4 | 6/4 4/4",
  });

  // Gaian severed from his Cores: the heartbeat falters. Gaps in the earth drums, a thin choir,
  // water dripping in the dark, and the duduk's melody broken into fragments.
  bosses.push({
    id: "gaian-severed", title: "Gaian · The Severed Heart",
    tempo: 76, meter: [4, 2], tonic: 52, mode: "dorian", room: "forest", gain: 1,
    sections: {
      A: {
        chords: "1 | 7 | 4 | 1", dyn: 0.75,
        layers: [
          { t: "drone", i: "choirOo", o: -1, v: 0.45, deg: ["1"] },
          { t: "tex", kind: "drips", v: 0.6 },
          { t: "line", i: "duduk", o: 0, v: 0.7, notes: "5/2 4/2 5/4 | 2/8 | 6/4 4/4 | 1/8" },
          { t: "drums", v: 0.75, p: { taiko: "X.x.............x.......X.......", frame: "..........x....." } },
        ],
      },
      B: {
        chords: "1 | 4 | 7 | 1", dyn: 0.85,
        layers: [
          { t: "drone", i: "drone", o: -2, v: 0.5, deg: ["1", "5"] },
          { t: "tex", kind: "drips", v: 0.5 },
          { t: "pulse", i: "kalimba", o: 1, v: 0.3, p: "1.......5.......", },
          { t: "line", i: "violins", o: 0, v: 0.5, notes: "5/4 4/4 | 2/8 | 4/4 2/4 | 1/8" },
          { t: "drums", v: 0.8, p: { taiko: "X.x.......x.....", tom: "............x..." } },
        ],
      },
    },
    form: ["A", "B"], loop: 0,
    motif: "5/2 4/2 5/4 | 2/8",
  });

  // Ornn, God of the Forge: a giants' work-song (C minor). Hammer and anvil keep time on the
  // first and third beats, the bellows breathe under it and embers crackle; a low choir answers
  // the trombones' call, and the chant climbs as the work goes on.
  bosses.push({
    id: "ornn-god-of-the-forge", title: "Ornn · Work-Song of the First Forge",
    tempo: 92, meter: [4, 2], tonic: 48, mode: "aeolian", room: "cavern", gain: 1,
    sections: {
      intro: {
        chords: "1 | 1 | 6 | 7", dyn: 0.7, dynTo: 0.95,
        layers: [
          { t: "drone", i: "drone", o: -1, v: 0.55, deg: ["1", "5"] },
          { t: "tex", kind: "wind", v: 0.45 },
          { t: "tex", kind: "embers", v: 0.6 },
          { t: "drums", v: 0.85, p: { anvil: "x...x...", war: "X.......", clack: "......x." } },
        ],
      },
      A: {
        chords: "1 | 3 | 4 | 5 | 1 | 4 | 7 | 1",
        mel: "1/2 1/2 3/2 1/2 | 5/4 4/2 3/2 | 4/2 4/2 3/2 1/2 | 2/8 | 5/2 5/2 4/2 3/2 | 4/4 3/2 2/2 | 2/2 4/2 2/2 7,/2 | 1/8",
        layers: [
          { t: "mel", i: "choir", o: -1, v: 0.85, dbl: [{ i: "trombones", o: -1, v: 0.45 }] },
          { t: "bass", i: "contrabass", o: -2, v: 0.6, p: "1---5---" },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.55, p: "L...L.5." },
          { t: "tex", kind: "embers", v: 0.4 },
          { t: "drums", v: 0.95, p: { anvil: "x...x...", war: "X...x...", clack: "..x...x.", shaker: ".x.x.x.x" } },
        ],
      },
      B: {
        chords: "1 | 7 | 4 | 5 | 6 | 3 | 7 | 1", dyn: 1.08,
        mel: "5/4 1'/4 | 7/2 6/2 5/4 | 6/2 4/2 1'/2 6/2 | 5/8 | 1'/4 7/2 6/2 | 5/2 4/2 3/4 | 4/2 2/2 7,/2 2/2 | 1/8",
        layers: [
          { t: "mel", i: "horns", o: -1, v: 0.9, dbl: [{ i: "choir", o: -1, v: 0.55 }, { i: "trombones", o: -2, v: 0.35 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.7, p: "L.5.L.5." },
          { t: "pad", i: "strings", o: -1, v: 0.35, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.65, p: "1-1-5-1-" },
          { t: "tex", kind: "wind", v: 0.3 },
          { t: "drums", v: 1, p: { anvil: "x.x.x.x.", war: "X...X...", taiko: "....x...", clack: "..x...x." } },
        ],
      },
      A2: {
        like: "A", dyn: 1.12,
        layers: [
          { t: "mel", i: "choir", o: -1, v: 0.9, dbl: [{ i: "horns", o: -1, v: 0.45 }, { i: "trombones", o: -1, v: 0.4 }] },
          { t: "bass", i: "contrabass", o: -2, v: 0.65, p: "1-1-5-1-" },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.65, p: "L.5.L.5." },
          { t: "tex", kind: "embers", v: 0.45 },
          { t: "hit", i: "boom", v: 0.45, bars: [0, 4] },
          { t: "drums", v: 1.05, p: { anvil: "x...x...", war: "X...x...", taiko: "x...x...", clack: "..x...x.", shaker: ".x.x.x.x" } },
        ],
      },
    },
    form: ["intro", "A", "B", "A2"], loop: 1,
    motif: "1/2 1/2 3/2 1/2 | 5/4 4/2 3/2",
  });

  // Ornn when the forge erupts: the work-song turned savage. The same chant in C harmonic minor,
  // far faster, anvils on every beat, war drums and timpani, the brass hammering the call and
  // sopranos screaming the answer; a breakdown leaves only the anvils, the choir and the fire.
  bosses.push({
    id: "ornn-forge-erupts", title: "Ornn · The Forge Erupts",
    tempo: 132, meter: [4, 2], tonic: 48, mode: "harmonic", room: "cavern", gain: 1,
    sections: {
      intro: {
        chords: "1 | 6 | 1 | 5", dyn: 0.85, dynTo: 1.1,
        layers: [
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.75, p: "LLLLLLLL" },
          { t: "bass", i: "trombones", o: -2, v: 0.55, p: "1-------" },
          { t: "tex", kind: "embers", v: 0.7 },
          { t: "hit", i: "boom", v: 0.8, bars: [0, 2] },
          { t: "hit", i: "swell", v: 0.6, bars: [3], len: 8 },
          { t: "drums", v: 1, p: { anvil: "xxxxxxxx", timp: "x.x.x.x.rrrrrrrr", war: "X.......X......." } },
        ],
      },
      A: {
        chords: "1 | 1 | 4 | 5 | 1 | 4 | 7 | 1", dyn: 1.1,
        mel: "1/1 1/1 3/2 1/1 1/1 3/2 | 5/4 4/2 3/2 | 4/1 4/1 4/2 3/2 1/2 | 2/4 7,/4 | 5/1 5/1 5/2 4/2 3/2 | 4/4 3/2 2/2 | 2/2 4/2 2/2 7,/2 | 1/8",
        layers: [
          { t: "mel", i: "horns", v: 1, dbl: [{ i: "trombones", o: -1, v: 0.6 }, { i: "choir", o: 0, v: 0.5 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.9, p: "L5L5L8L5" },
          { t: "pulse", i: "spiccato", o: 0, v: 0.45, p: "35835385" },
          { t: "bass", i: "contrabass", o: -2, v: 0.75, p: "1-1-1-1-" },
          { t: "hit", i: "boom", v: 0.6, bars: [0, 4] },
          { t: "drums", v: 1.1, p: { anvil: "x.x.x.x.", war: "X.x.X.x.X.x.X.x.", taiko: "x.x.x.x.", snare: "....x..r" } },
        ],
      },
      B: {
        like: "A", dyn: 1.2,
        layers: [
          { t: "mel", i: "sopranos", o: 1, v: 0.85, dbl: [{ i: "trumpet", o: 0, v: 0.45 }, { i: "horns", o: -1, v: 0.6 }, { i: "choir", o: -1, v: 0.5 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.95, p: "L5L8L5L8" },
          { t: "pad", i: "tremolo", o: -1, v: 0.45, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.8, p: "1-1-1-1-" },
          { t: "hit", i: "boom", v: 0.7, bars: [0, 2, 4, 6] },
          { t: "hit", i: "crash", v: 0.55, bars: [0, 4] },
          { t: "drums", v: 1.15, p: { anvil: "xxxxxxxx", war: "X.x.X.x.", taiko: "x.x.x.x.", timp: "x...x...x...x.rr" } },
        ],
      },
      breakdown: {
        chords: "1 | 6 | 5 | 1", dyn: 0.8, dynTo: 1.05,
        layers: [
          { t: "line", i: "choir", o: -1, v: 0.85, notes: "1/2 1/2 3/2 1/2 | 5/4 4/2 3/2 | 2/4 7,/4 | 1/8" },
          { t: "drone", i: "drone", o: -2, v: 0.55, deg: ["1"] },
          { t: "tex", kind: "embers", v: 0.75 },
          { t: "tex", kind: "wind", v: 0.4 },
          { t: "drums", v: 0.85, p: { anvil: "x...x...", timp: "x.......x.....rr" } },
        ],
      },
      A2: { like: "A", dyn: 1.22 },
    },
    form: ["intro", "A", "B", "breakdown", "A2"], loop: 1,
    motif: "1/1 1/1 3/2 1/1 1/1 3/2 | 5/4 4/2 3/2",
  });

  // The Deep Sea King: a regal sea shanty in 6/8 (D dorian). Surf and a rolling wave rhythm in the
  // low strings carry a swaggering fiddle tune, the duelist's line; brass fanfares crown the king;
  // in the duel the fiddle races in runs against brass and drums, then a lone fiddle on the tide.
  bosses.push({
    id: "deep-sea-king", title: "Deep Sea King · The Tide's Crown",
    tempo: 66, meter: [2, 3], tonic: 50, mode: "dorian", room: "hall", gain: 1,
    sections: {
      intro: {
        chords: "1 | 7 | 1 | 5m", dyn: 0.65, dynTo: 0.9,
        layers: [
          { t: "drone", i: "drone", o: -2, v: 0.5, deg: ["1", "5"] },
          { t: "tex", kind: "surf", v: 0.6 },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.5, p: "L.5L.5" },
          { t: "line", i: "horns", o: -1, v: 0.7, notes: "5/3 1'/3 | 7/6 | 5/3 3'/3 | 2'/6" },
          { t: "hit", i: "swell", v: 0.5, bars: [3], len: 6 },
          { t: "drums", v: 0.6, p: { timp: "x.....", frame: "x..x.." } },
        ],
      },
      A: {
        chords: "1 | 7 | 1 | 5m | 1 | 7 | 3 4 | 1",
        mel: "5/2 1'/1 1'/2 2'/1 | 2'/2 1'/1 7/2 5/1 | 1'/2 5/1 3/2 5/1 | 5/3 5/2 4/1 | 5/2 1'/1 1'/2 2'/1 | 2'/2 3'/1 2'/2 1'/1 | 7/2 1'/1 6/2 5/1 | 1'/6",
        layers: [
          { t: "mel", i: "fiddle", v: 0.85, dbl: [{ i: "cellos", o: -1, v: 0.3 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.65, p: "L.5L.5" },
          { t: "arp", i: "harp", o: 0, v: 0.4, p: "158T85" },
          { t: "bass", i: "contrabass", o: -2, v: 0.6, p: "1..5.." },
          { t: "tex", kind: "surf", v: 0.3 },
          { t: "drums", v: 0.7, p: { frame: "X..x..", shaker: "x.xx.x" } },
        ],
      },
      B: {
        chords: "3 | 7 | 4 | 1 | 3 | 7 | 5m | 1", dyn: 1.05,
        mel: "3'/2 3'/1 5'/3 | 4'/2 3'/1 2'/3 | 1'/2 2'/1 4'/3 | 3'/3 1'/3 | 3'/2 3'/1 5'/3 | 7'/2 5'/1 4'/3 | 5'/2 3'/1 2'/2 1'/1 | 1'/6",
        layers: [
          { t: "mel", i: "horns", o: -1, v: 0.95, dbl: [{ i: "trumpet", o: 0, v: 0.4 }, { i: "trombones", o: -1, v: 0.45 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.75, p: "L5.L5." },
          { t: "pad", i: "choirOo", o: -1, v: 0.35, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.7, p: "1..1.." },
          { t: "hit", i: "crash", v: 0.5, bars: [0, 4] },
          { t: "drums", v: 0.9, p: { taiko: "X..x..", frame: "x.xx.x", timp: "x....." } },
        ],
      },
      C: {
        chords: "1 | 4 | 1 | 4 | 7 | 3 | 5m | 1", dyn: 1.15,
        mel: "1'/1 2'/1 3'/1 5'/1 3'/1 1'/1 | 4'/1 6'/1 4'/1 2'/1 1'/1 2'/1 | 5'/1 3'/1 1'/1 5/1 1'/1 3'/1 | 4'/3 6'/3 | 5'/1 3'/1 2'/1 7/1 2'/1 3'/1 | 3'/2 1'/1 7/2 5/1 | 5/1 7/1 1'/1 2'/1 3'/1 2'/1 | 1'/3 5/3",
        layers: [
          { t: "mel", i: "fiddle", v: 0.9, dbl: [{ i: "violins", o: 0, v: 0.35 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.85, p: "L5LL5L" },
          { t: "bass", i: "trombones", o: -2, v: 0.5, p: "1..5.." },
          { t: "bass", i: "contrabass", o: -2, v: 0.75, p: "1.11.1" },
          { t: "hit", i: "boom", v: 0.6, bars: [0, 4] },
          { t: "drums", v: 1, p: { taiko: "X.xX.x", snare: "..x..r", frame: "x.xx.x", timp: "x..x.." } },
        ],
      },
      bridge: {
        chords: "1 | 7 | 3 | 5m", dyn: 0.6, dynTo: 0.85,
        layers: [
          { t: "line", i: "fiddle", o: 0, v: 0.7, notes: "1'/3 5/3 | 2'/3 7/3 | 3'/3 1'/3 | 5/6" },
          { t: "drone", i: "drone", o: -2, v: 0.5, deg: ["1"] },
          { t: "tex", kind: "surf", v: 0.6 },
          { t: "pad", i: "choirOo", o: -1, v: 0.4, n: 3 },
          { t: "drums", v: 0.5, p: { frame: "x....." } },
        ],
      },
      A2: {
        like: "A", dyn: 1.12,
        layers: [
          { t: "mel", i: "fiddle", v: 0.9, dbl: [{ i: "horns", o: -1, v: 0.45 }, { i: "cellos", o: -1, v: 0.3 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.75, p: "L5.L5." },
          { t: "arp", i: "harp", o: 0, v: 0.4, p: "158T85" },
          { t: "bass", i: "contrabass", o: -2, v: 0.7, p: "1..5.." },
          { t: "hit", i: "crash", v: 0.45, bars: [0] },
          { t: "drums", v: 0.9, p: { taiko: "X..x..", frame: "x.xx.x", shaker: "x.xx.x" } },
        ],
      },
    },
    form: ["intro", "A", "B", "C", "bridge", "A2"], loop: 1,
    motif: "5/2 1'/1 1'/2 2'/1 | 2'/2 1'/1 7/2 5/1",
  });

  // Zik Lun, the armored Hulk: the original siege-march, remastered. Plated footsteps (timpani and
  // war drums) and anvil blows open it; trombones carry the march over low strings; the horns
  // take it up an octave; then the choir, and at last sopranos and trumpets crown the climb.
  // D harmonic minor with the Neapolitan (E-flat minor) as in the original score.
  bosses.push({
    id: "zik-lun-armored-hulk", title: "Zik Lun · The Mountain Breaks",
    tempo: 92, meter: [4, 2], tonic: 50, mode: "harmonic", room: "hall", gain: 1,
    sections: {
      intro: {
        chords: "1 | 1 | b2m | 5M", dyn: 0.75, dynTo: 1,
        layers: [
          { t: "drone", i: "drone", o: -2, v: 0.55, deg: ["1"] },
          { t: "bass", i: "contrabass", o: -2, v: 0.6, p: "1---1---" },
          { t: "hit", i: "boom", v: 0.8, bars: [0, 2] },
          { t: "hit", i: "swell", v: 0.55, bars: [3], len: 8 },
          { t: "tex", kind: "wind", v: 0.4 },
          { t: "drums", v: 0.9, p: { timp: "x...x...", war: "X.......", anvil: "..x.....", tom: "...x...x" } },
        ],
      },
      A: {
        chords: "1 | 1 | 6 | 5M | 1 | b2m | 6 | 5M",
        mel: "1/4 3/2 5/2 | 1/2 3/2 5/4 | 6/3 3/2 1/3 | 7,/2 2/2 5/4 | 1'/3 5/2 3'/3 | b2'/2 #3'/2 b2'/2 6/2 | 6/3 3/2 1/3 | 7/2 5/2 2/4",
        layers: [
          { t: "mel", i: "trombones", v: 0.9, dbl: [{ i: "cellos", o: 0, v: 0.35 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.7, p: "L.5.L.5." },
          { t: "bass", i: "contrabass", o: -2, v: 0.65, p: "1---1---" },
          { t: "pad", i: "choirOo", o: 0, v: 0.3, n: 3 },
          { t: "hit", i: "boom", v: 0.6, bars: [0] },
          { t: "drums", v: 0.95, p: { war: "X...x...", anvil: "..x.....", tom: "...x...x", timp: "x.......x......." } },
        ],
      },
      B: {
        chords: "4m | 6 | b7M | 5M | 1 | b2m | 5M | 1", dyn: 1.08,
        mel: "4'/3 1'/2 6/3 | 6'/2 4'/2 3'/4 | b7'/3 4'/2 2'/3 | 5'/2 7'/2 2''/4 | 1''/2 5'/2 3'/2 1'/2 | b2''/2 6'/2 #3'/2 b2'/2 | 7'/2 5'/2 2'/2 7/2 | 1''/3 5/2 1/3",
        layers: [
          { t: "mel", i: "horns", o: -1, v: 0.95, dbl: [{ i: "trombones", o: -1, v: 0.45 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.8, p: "L5L5L8L5" },
          { t: "pulse", i: "spiccato", o: 0, v: 0.35, p: "..5...8." },
          { t: "bass", i: "contrabass", o: -2, v: 0.7, p: "1---1---" },
          { t: "hit", i: "crash", v: 0.5, bars: [0, 4] },
          { t: "drums", v: 1, p: { war: "X...x...", anvil: "..x...x.", tom: "...x...x", snare: "......xr", timp: "x.......x.....rr" } },
        ],
      },
      C: {
        chords: "1 | 6 | 4m | 5M | 1 | b2m | b7M | 5M", dyn: 1.15,
        mel: "1'/3 3'/2 5'/3 | 6'/2 3'/2 1'/2 6/2 | 4'/2 6'/2 1''/2 6'/2 | 7'/2 5'/2 2'/2 7/2 | 1''/3 5'/2 3'/3 | b2''/2 #3''/2 b2''/2 6'/2 | b7'/2 2''/2 4''/2 2''/2 | 7'/3 5'/2 2'/3",
        layers: [
          { t: "mel", i: "horns", o: -1, v: 1, dbl: [{ i: "choir", o: -1, v: 0.5 }, { i: "trombones", o: -2, v: 0.45 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.85, p: "L5L5L8L5" },
          { t: "pad", i: "tremolo", o: -1, v: 0.4, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.75, p: "1-1-1-1-" },
          { t: "hit", i: "boom", v: 0.7, bars: [0, 4] },
          { t: "drums", v: 1.05, p: { war: "X.x.X.x.", taiko: "x.x.x.x.", anvil: "..x...x.", snare: "....x..r", timp: "x.......x.....rr" } },
        ],
      },
      D: {
        chords: "4m | 6 | b7M | 5M | 1 | b2m | 5M | 1", dyn: 1.2,
        mel: "4''/3 1''/2 6'/3 | 6''/2 4''/2 3''/2 6'/2 | b7''/3 4''/2 2''/3 | 5''/2 2''/2 7'/2 5'/2 | 1'''/2 5''/2 3''/2 1''/2 | b2'''/2 6''/2 #3''/2 b2''/2 | 7''/2 5''/2 2''/2 7'/2 | 1'''/3 5'/2 1'/3",
        layers: [
          { t: "mel", i: "sopranos", o: -2, v: 0.85, dbl: [{ i: "trumpet", o: 0, v: 0.45 }, { i: "horns", o: -1, v: 0.6 }, { i: "choir", o: -1, v: 0.45 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.9, p: "L5L8L5L8" },
          { t: "pulse", i: "spiccato", o: 0, v: 0.45, p: "35835T85" },
          { t: "bass", i: "contrabass", o: -2, v: 0.8, p: "1-1-1-1-" },
          { t: "bass", i: "trombones", o: -2, v: 0.5, p: "1---5---" },
          { t: "hit", i: "crash", v: 0.6, bars: [0, 4] },
          { t: "hit", i: "boom", v: 0.8, bars: [0, 4] },
          { t: "drums", v: 1.1, p: { war: "X.x.X.x.", taiko: "x.x.x.x.", anvil: "x.x...x.", snare: "..x...xr", timp: "x...x...x...x.rr" } },
        ],
      },
    },
    form: ["intro", "A", "B", "C", "D"], loop: 1,
    motif: "1/4 3/2 5/2 | 1/2 3/2 5/4",
  });

  // Nygheir the Wounded: a lament for an old dragon on an unburied battlefield. A duduk sings
  // the wound motive (the fifth sighing onto the minor sixth) over a drone, a tolling bell and a
  // slow taiko heartbeat; the strings and a wordless choir remember the war, then the duduk returns.
  bosses.push({
    id: "nygheir-the-wounded", title: "Nygheir · Old Wounds",
    tempo: 64, meter: [4, 2], tonic: 52, mode: "aeolian", room: "mountain", gain: 1,
    sections: {
      intro: {
        chords: "1 | 6 | 4 | 5M", dyn: 0.6, dynTo: 0.85,
        layers: [
          { t: "drone", i: "drone", o: -2, v: 0.6, deg: ["1", "5"] },
          { t: "pad", i: "choirOo", o: -1, v: 0.45, n: 3 },
          { t: "tex", kind: "wind", v: 0.55 },
          { t: "hit", i: "toll", deg: "1", o: -1, v: 0.55, bars: [0, 2] },
          { t: "line", i: "duduk", o: 0, v: 0.65, notes: "_/4 5/2 6/2 | 3'/6 1'/2 | 6/4 4/4 | 7,/8" },
          { t: "drums", v: 0.55, p: { taiko: "X..x............" } },
        ],
      },
      A: {
        chords: "1 | 6 | 4 | 5M | 1 | 3 | 4 | 5M",
        mel: "5/3 6/1 5/2 3/2 | 1'/4 7/2 6/2 | 6/3 5/1 4/2 3/2 | 2/4 #7,/4 | 5/3 6/1 5/2 1'/2 | 7/4 5/2 3/2 | 4/3 3/1 2/2 1/2 | #7,/4 2/2 _/2",
        layers: [
          { t: "mel", i: "duduk", v: 0.85, dbl: [{ i: "cellos", o: -1, v: 0.35 }] },
          { t: "arp", i: "harp", o: -1, v: 0.4, p: "1.5.8.5." },
          { t: "pad", i: "strings", o: -1, v: 0.35, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.55, p: "1-------" },
          { t: "hit", i: "toll", deg: "1", o: -1, v: 0.4, bars: [0, 4] },
          { t: "drums", v: 0.65, p: { taiko: "X..x............", timp: "x..............." } },
        ],
      },
      B: {
        chords: "6 | 7 | 1 | 1 | 4 | 6 | 5M | 5M", dyn: 1.05,
        mel: "3'/4 1'/2 7/2 | 7/4 2'/2 4'/2 | 5'/4 3'/2 1'/2 | 1'/6 _/2 | 6/2 1'/2 3'/2 4'/2 | 3'/4 1'/2 6/2 | #7/4 2'/4 | 5/8",
        layers: [
          { t: "mel", i: "violins", v: 0.75, dbl: [{ i: "choir", o: -1, v: 0.45 }, { i: "cellos", o: -2, v: 0.3 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.55, p: "L.5.L.5." },
          { t: "pad", i: "tremolo", o: -1, v: 0.35, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.6, p: "1-------" },
          { t: "bass", i: "trombones", o: -2, v: 0.35, p: "1---5---" },
          { t: "hit", i: "swell", v: 0.5, bars: [7], len: 8 },
          { t: "drums", v: 0.8, p: { taiko: "X..x....x..x....", timp: "x.......x.....rr", war: "X..............." } },
        ],
      },
      bridge: {
        chords: "4 | 1 | 6 | 5M", dyn: 0.6, dynTo: 0.85,
        layers: [
          { t: "line", i: "duduk", o: 0, v: 0.75, notes: "6/4 4/4 | 3/6 _/2 | 1'/4 7/2 6/2 | #7,/8" },
          { t: "drone", i: "drone", o: -2, v: 0.55, deg: ["1"] },
          { t: "pad", i: "choirOo", o: -1, v: 0.45, n: 3 },
          { t: "tex", kind: "wind", v: 0.5 },
          { t: "hit", i: "toll", deg: "1", o: -1, v: 0.5, bars: [0, 2] },
          { t: "drums", v: 0.55, p: { taiko: "X..x............" } },
        ],
      },
      A2: {
        like: "A", dyn: 1.12,
        layers: [
          { t: "mel", i: "duduk", v: 0.85, dbl: [{ i: "horns", o: -1, v: 0.4 }, { i: "cellos", o: -1, v: 0.3 }] },
          { t: "arp", i: "harp", o: -1, v: 0.4, p: "1.5.8.5." },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.5, p: "L.5.L.5." },
          { t: "pad", i: "strings", o: -1, v: 0.4, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.6, p: "1-------" },
          { t: "hit", i: "toll", deg: "1", o: -1, v: 0.45, bars: [0, 4] },
          { t: "drums", v: 0.75, p: { taiko: "X..x....x..x....", timp: "x..............." } },
        ],
      },
    },
    form: ["intro", "A", "B", "bridge", "A2"], loop: 1,
    motif: "5/3 6/1 5/2 3/2 | 1'/4 7/2 6/2",
  });

  // Nygheir the Scarred: the same wound motive turned to fury. Harmonic minor, double the tempo:
  // timpani and embers under his phase cutscene, a boom as his scars burst, then horns and
  // trombones roar the motive over driving low strings and war drums, sopranos and trumpet
  // scream it higher, and the duduk's lament flickers once through the fire before the end.
  bosses.push({
    id: "nygheir-the-scarred", title: "Nygheir · Burning Scars",
    tempo: 132, meter: [4, 2], tonic: 52, mode: "harmonic", room: "mountain", gain: 1,
    sections: {
      intro: {
        chords: "1 | b2M | 1 | 5M", dyn: 0.7, dynTo: 1.1,
        layers: [
          { t: "drone", i: "drone", o: -2, v: 0.6, deg: ["1"] },
          { t: "tex", kind: "embers", v: 0.6 },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.6, p: "LLLLLLLL" },
          { t: "bass", i: "trombones", o: -2, v: 0.5, p: "1-------" },
          { t: "hit", i: "boom", v: 0.95, bars: [2], at: 4 },
          { t: "hit", i: "crash", v: 0.6, bars: [2], at: 4 },
          { t: "hit", i: "swell", v: 0.6, bars: [3], len: 8 },
          { t: "drums", v: 0.95, p: { timp: "x.x.x.x.rrrrrrrr", war: "X.......X......." } },
        ],
      },
      A: {
        chords: "1 | 6 | 4 | 5M | 1 | 6 | b2M | 5M", dyn: 1.05,
        mel: "5/3 6/1 5/2 3/2 | 1'/6 6/2 | 6/3 5/1 4/2 3/2 | 2/4 7,/4 | 5/3 6/1 5/2 1'/2 | 1'/6 6/2 | 4/4 6/2 4/2 | 7,/4 2/2 _/2",
        layers: [
          { t: "mel", i: "horns", v: 0.95, dbl: [{ i: "trombones", o: -1, v: 0.55 }, { i: "choir", o: 0, v: 0.45 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.85, p: "L5L5L8L5" },
          { t: "pulse", i: "spiccato", o: 0, v: 0.45, p: "35835T85" },
          { t: "bass", i: "contrabass", o: -2, v: 0.7, p: "1-1-1-1-" },
          { t: "tex", kind: "embers", v: 0.4 },
          { t: "hit", i: "crash", v: 0.55, bars: [0, 4] },
          { t: "drums", v: 1, p: { war: "X...x...X...x...", taiko: "x.x.x.x.x.x.x.x.", snare: "....x.......x..x", timp: "x.......x.....rr" } },
        ],
      },
      B: {
        chords: "6 | 7 | 1 | 1 | 4 | 6 | 5M | 5M", dyn: 1.15,
        mel: "3'/4 1'/2 3'/2 | 7/4 2'/2 4'/2 | 5'/4 3'/2 1'/2 | 1'/6 _/2 | 6/2 1'/2 3'/2 4'/2 | 3'/4 1'/2 6/2 | 7/4 2'/4 | 5/8",
        layers: [
          { t: "mel", i: "sopranos", o: 1, v: 0.85, dbl: [{ i: "choir", o: -1, v: 0.55 }, { i: "trumpet", o: -1, v: 0.4 }, { i: "violins", o: 0, v: 0.35 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.9, p: "L5L8L5L8" },
          { t: "pad", i: "tremolo", o: -1, v: 0.45, n: 3 },
          { t: "bass", i: "trombones", o: -2, v: 0.6, p: "1---5---" },
          { t: "bass", i: "contrabass", o: -2, v: 0.7, p: "1-1-1-1-" },
          { t: "hit", i: "boom", v: 0.75, bars: [0, 4] },
          { t: "hit", i: "crash", v: 0.6, bars: [0] },
          { t: "drums", v: 1.05, p: { war: "X.x.X.x.X.x.X.x.", taiko: "x.x.x.x.x.x.x.x.", snare: "..x...x...x..xrr", anvil: "x.......x......." } },
        ],
      },
      breakdown: {
        chords: "1 | b2M | 1 | 5M", dyn: 0.75, dynTo: 1.05,
        layers: [
          { t: "line", i: "duduk", o: 0, v: 0.75, notes: "5/3 6/1 5/2 3/2 | 4/8 | 5/3 6/1 5/2 1'/2 | 7,/8" },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.7, p: "LLLLLLLL" },
          { t: "drone", i: "drone", o: -2, v: 0.5, deg: ["1"] },
          { t: "tex", kind: "embers", v: 0.55 },
          { t: "hit", i: "swell", v: 0.65, bars: [3], len: 8 },
          { t: "drums", v: 0.8, p: { timp: "x.x.x.x.x.x.rrrr", taiko: "X......." } },
        ],
      },
      A2: {
        like: "A", dyn: 1.2,
        layers: [
          { t: "mel", i: "horns", v: 1, dbl: [{ i: "trombones", o: -1, v: 0.6 }, { i: "sopranos", o: 1, v: 0.35 }, { i: "choir", o: 0, v: 0.45 }] },
          { t: "pulse", i: "lowSpiccato", o: -1, v: 0.9, p: "L5L5L8L5" },
          { t: "pulse", i: "spiccato", o: 0, v: 0.5, p: "35835T85" },
          { t: "pad", i: "tremolo", o: -1, v: 0.4, n: 3 },
          { t: "bass", i: "contrabass", o: -2, v: 0.75, p: "1-1-1-1-" },
          { t: "hit", i: "boom", v: 0.7, bars: [0] },
          { t: "hit", i: "crash", v: 0.6, bars: [0, 4] },
          { t: "drums", v: 1.1, p: { war: "X.x.X.x.X.x.X.x.", taiko: "x.x.x.x.x.x.x.x.", snare: "....x.......x.rr", anvil: "x.......x.......", timp: "x.......x.....rr" } },
        ],
      },
    },
    form: ["intro", "A", "B", "breakdown", "A2"], loop: 1,
    motif: "5/3 6/1 5/2 3/2 | 1'/6 6/2",
  });

  // Loudness trims, measured offline (EBU R128), so every theme plays at the level
  // of the original soundtrack: regions near -17.5 LUFS, battles near -17.
  const LOUDNESS = {
    "alder-vale": 2.24, "ancient-battlefield": 2.14, "bloodstone-coast": 2.14, "cinder-isle": 2.24,
    "combat": 1.72, "combat@alder-vale": 1.72, "combat@ancient-battlefield": 1.66, "combat@bloodstone-coast": 1.7,
    "combat@cinder-isle": 1.74, "combat@crimson-divide": 1.6, "combat@deep-desert": 1.72, "combat@duskpine-forest": 1.74,
    "combat@gloamfen-marsh": 2.07, "combat@high-stoneveil": 1.8, "combat@mirrormere-basin": 1.62, "combat@saffron-expanse": 2.37,
    "combat@stoneveil-foothills": 1.8, "combat@stoneveil-summit": 1.72, "combat@stormwake-hills": 1.6, "combat@sunward-coast": 1.84,
    "combat@verdant-reach": 1.84, "combat@verdigris-jungle": 1.76, "crimson-divide": 2.29, "deep-desert": 3.16,
    "duskpine-forest": 2.63, "gloamfen-marsh": 2.66, "high-stoneveil": 2.43, "mirrormere-basin": 2.09,
    "saffron-expanse": 2.88, "stoneveil-foothills": 2.32, "stoneveil-summit": 2.11, "stormwake-hills": 1.78,
    "sunward-coast": 2.19, "verdant-reach": 2.63, "verdigris-jungle": 2.57,
    // Boss themes sit at the battle level.
    "great-imp-jiruan": 2.08,
    "ancient-lich-ool": 3.26, "ancient-lich-ool-mortal": 1.41,
    "ur-mat-false-dreamer": 3.14, "ur-mat-lucid-change": 1.2,
    "anatemis-white-star": 3.45, "anatemis-golden-star": 2.18, "anatemis-zenith": 1.33,
    "juila-grand-forest-matriach": 2.44,
    // Nygheir's lament sits a little under the battle level and his fury a little over it.
    "nygheir-the-wounded": 2.92, "nygheir-the-scarred": 1.31,
    "zik-lun-armored-hulk": 1.47, "deep-sea-king": 2.0,
    // Tankina's rite sits at the battle level; the Chosen's wild storm a little over it.
    "tankina-disciple-of-talos": 1.75, "tankina-chosen-of-talos": 1.22,
    // Ornn's work-song sits at the battle level; the eruption a little over it.
    "ornn-god-of-the-forge": 2.06, "ornn-forge-erupts": 1.08,
    // Gaian's heartbeat sits at the battle level; the severed heart, quieter, a little under it.
    "gaian-avatar-of-the-land": 2.47, "gaian-severed": 4.6,
    // Saffron's court sits at the battle level.
    "archdevil-saffron": 2.03,
  };
  const trimmed = spec => ({ ...spec, gain: (spec.gain ?? 1) * (LOUDNESS[spec.id] ?? 1) });

  window.MusicThemes = {
    all: () => [...regions, arrange(combat), ...Object.entries(variations).map(([regionId, variation]) => arrange(combat, variation, "combat@" + regionId)), ...bosses].map(trimmed),
    regions: () => regions.map(spec => spec.id),
  };
})();

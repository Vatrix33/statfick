// Web Audio soundtrack: scores and playback live together in this file.
(() => {
  "use strict";

  const STORAGE_KEY = "stat-fick-sound-volume";
  const button = document.getElementById("settings-open-button");
  const settings = document.getElementById("settings-modal");
  const volumeSlider = document.getElementById("sound-volume");
  const volumeLabel = document.getElementById("sound-volume-value");
  const volumeHelp = document.getElementById("sound-volume-help");
  const trackLabel = document.getElementById("settings-current-track");
  const AudioContextClass = window.AudioContext || window.webkitAudioContext;
  const score = (title, kind, tempo, meter, harmony, route, phrases) => {
    const lead = phrases.map(line => line.split(/\s+/).map(note => note === "-" ? null : Number(note)));
    if (route.length !== lead.length || lead.some(bar => bar.length !== meter || bar.some(note => note != null && !Number.isFinite(note))) || route.some(chord => !harmony[chord])) {
      throw new Error(`Invalid music score: ${title}`);
    }
    return { title, kind, tempo, meter, harmony, route, lead };
  };
  const scores = {
    "alder-vale": score(
      "Alder Vale · Beyond the Gate", "pastoral", 112, 6,
      { D: [50, 54, 57], A: [45, 49, 52], Bm: [47, 50, 54], G: [43, 47, 50], Em: [52, 55, 59], Fsm: [54, 57, 61] },
      "D A Bm G D Em A A Bm Fsm G D Em Bm G A".split(" "), [
        "74 - 78 81 - 78", "76 - 74 73 74 -", "71 - 74 78 - 76", "74 - 71 69 71 -",
        "74 76 78 - 81 78", "76 - 73 74 - 76", "78 81 83 - 81 78", "76 - 74 73 - -",
        "83 - 81 78 76 -", "81 78 76 - 74 -", "78 - 76 74 71 74", "76 - 78 81 - -",
        "74 76 78 81 83 81", "78 - 76 74 73 74", "76 - 78 76 74 73", "74 - 73 76 73 -"
      ]
    ),
    "stoneveil-foothills": score(
      "Stoneveil · The Long Ascent", "highlands", 84, 8,
      { Dm: [50, 53, 57], C: [48, 52, 55], G: [43, 47, 50], Bb: [46, 50, 53], Am: [45, 48, 52], A: [45, 49, 52] },
      "Dm C G Dm Bb C G A Dm Bb C G Bb Am A Dm".split(" "), [
        "62 - - 65 69 - 65 -", "67 - - 65 64 - - -", "62 - 67 - 69 - 67 -", "65 - - 62 60 - - -",
        "69 - - 70 74 - 70 -", "72 - 69 - 67 - - -", "67 - 69 74 - 71 69 -", "65 - - 64 61 - - -",
        "74 - - 72 69 - 65 -", "70 - 69 - 65 - - -", "72 - - 74 76 - 74 -", "71 - 69 - 67 - - -",
        "74 - - 77 74 - 70 -", "72 - 69 67 65 - - -", "73 - 76 - 73 69 - -", "74 - - 69 62 - - -"
      ]
    ),
    "high-stoneveil": score(
      "High Stoneveil · Where the Sky Thins", "summit", 70, 7,
      { Bm: [47, 50, 54], G: [43, 47, 50], D: [50, 54, 57], A: [45, 49, 52], Em: [52, 55, 59], Fsh: [42, 46, 49], C: [48, 52, 55] },
      "Bm G D A Em G Fsh Bm Bm C G D Em A Fsh Bm".split(" "), [
        "71 - - 74 - 78 -", "74 - - 71 - 69 -", "74 - 78 - 81 - -", "76 - - 73 - 71 -",
        "71 - 74 - 79 - -", "78 - - 74 - 71 -", "73 - 76 - 78 - -", "71 - - 66 - 71 -",
        "83 - - 81 - 78 -", "79 - - 76 - 72 -", "78 - 74 - 71 - -", "81 - - 78 - 74 -",
        "79 - 83 - 86 - -", "81 - - 76 - 73 -", "78 - 76 - 73 - -", "71 - - 74 - 71 -"
      ]
    ),
    "verdant-reach": score(
      "Verdant Reach · Lanterns in the Leaves", "forest", 104, 10,
      { C: [48, 52, 55, 59], D: [50, 54, 57], Em: [52, 55, 59], G: [43, 47, 50], Am: [45, 48, 52], F: [53, 57, 60] },
      "C D Em G Am C D Em F C Am G D Em F C".split(" "), [
        "76 - - 79 - 83 - 79 - -", "78 - 81 - 78 - 74 - - -", "79 - - 83 - 86 - 83 - -", "81 - 79 - 76 - 74 - - -",
        "72 - 76 - 79 - 76 - - -", "76 - 79 83 - 86 - 83 - -", "81 - 78 - 76 - 74 - - -", "79 - - 76 - 74 - 71 - -",
        "84 - 81 - 79 - 76 - - -", "83 - 79 76 - 72 - 76 - -", "81 - 84 - 79 - 76 - - -", "79 - 76 - 74 - 71 - - -",
        "78 - 81 - 86 - 81 - - -", "83 - 79 - 76 - 74 - - -", "81 - 84 81 - 79 - 76 - -", "76 - - 79 - 83 - 76 - -"
      ]
    ),
    "sunward-coast": score(
      "Sunward Coast · Saltwind Road", "seaside", 108, 9,
      { D: [50, 54, 57], Bm: [47, 50, 54], G: [43, 47, 50], A: [45, 49, 52], Em: [52, 55, 59], Fsm: [54, 57, 61] },
      "D Bm G A D Em G A Bm Fsm G A Em Bm G D".split(" "), [
        "74 - 78 - 81 - 78 76 -", "71 - 74 - 78 - 76 74 -", "79 - 76 - 74 - 71 74 -", "76 - 73 - 76 - 81 76 -",
        "81 - 78 - 74 - 78 81 -", "83 - 79 - 76 - 74 76 -", "79 - 83 - 86 - 83 79 -", "81 - 76 - 73 - 76 78 -",
        "86 - 83 - 78 - 74 78 -", "85 - 81 - 78 - 73 78 -", "83 - 79 - 74 - 71 74 -", "81 - 85 - 88 - 85 81 -",
        "79 - 83 - 86 - 83 79 -", "78 - 74 - 71 - 74 78 -", "79 - 76 - 74 - 71 74 -", "74 - 78 - 81 - 74 69 -"
      ]
    ),
    "cinder-isle": score(
      "Cinder Isle · Heart of the Mountain", "volcanic", 126, 11,
      { Cm: [48, 51, 55], Ab: [44, 48, 51], Bb: [46, 50, 53], G: [43, 47, 50], Fm: [41, 44, 48], Eb: [51, 55, 58], Ddim: [50, 53, 56] },
      "Cm Ab Bb G Cm Fm Ab G Eb Bb Fm G Cm Ddim Ab G".split(" "), [
        "72 - 75 - 79 - 82 - 79 75 -", "80 - 75 - 72 - 68 - 72 75 -", "77 - 82 - 77 - 74 - 70 74 -", "79 - 74 - 71 - 67 - 71 74 -",
        "84 - 79 - 75 - 72 - 75 79 -", "80 - 84 - 80 - 75 - 72 75 -", "80 - 75 - 72 - 68 - 72 75 -", "79 - 83 - 86 - 83 - 79 74 -",
        "87 - 82 - 79 - 75 - 79 82 -", "86 - 82 - 77 - 74 - 77 82 -", "84 - 80 - 75 - 72 - 75 80 -", "83 - 79 - 74 - 71 - 74 79 -",
        "84 - 87 - 91 - 87 - 84 79 -", "86 - 82 - 77 - 74 - 77 82 -", "80 - 84 - 87 - 84 - 80 75 -", "79 - 83 - 86 - 83 - 79 72 -"
      ]
    ),
    "stormwake-hills": score(
      "Stormwake Hills · Rain on the Ramparts", "tempest", 96, 8,
      { Em: [40, 43, 47], C: [48, 52, 55], D: [50, 54, 57], G: [43, 47, 50], Am: [45, 48, 52], B: [47, 51, 54] },
      "Em C D Em G D Am B Em G C D Am C B Em".split(" "), [
        "76 - 79 - 83 81 79 -", "72 - 76 - 79 - 76 -", "74 - 78 - 81 78 74 -", "76 - 71 - 79 - 76 -",
        "79 - 83 - 86 83 79 -", "81 - 78 - 74 - 78 -", "76 - 72 - 69 72 76 -", "75 - 78 - 83 78 75 -",
        "88 - 86 83 79 - 83 -", "86 - 83 - 79 83 86 -", "84 - 79 - 76 - 79 -", "81 - 78 74 78 - 81 -",
        "84 - 81 - 76 72 76 -", "79 - 76 72 76 - 79 -", "83 - 78 - 75 78 83 -", "76 - 79 - 83 79 76 -"
      ]
    ),
    "gloamfen-marsh": score(
      "Gloamfen Marsh · Lanterns on the Causeway", "marsh", 74, 8,
      { Dm: [50, 53, 57], Gm: [43, 46, 50], Bb: [46, 50, 53], A: [45, 49, 52], F: [41, 45, 48], C: [48, 52, 55] },
      "Dm Gm Bb A Dm F Gm A Dm C Bb Gm F Bb A Dm".split(" "), [
        "74 - - 77 76 - 72 -", "70 - 74 - 77 - 74 -", "70 - - 74 77 - 79 -", "73 - 76 - 69 - 73 -",
        "74 - 77 - 81 - 77 -", "72 - - 76 79 - 76 -", "70 - 74 77 - 74 - -", "73 - - 76 81 - 76 -",
        "86 - - 81 77 - 74 -", "79 - 76 - 72 - 76 -", "82 - - 77 74 - 70 -", "77 - 74 - 70 - 74 -",
        "76 - 72 - 69 - 72 -", "77 - 74 - 70 - 74 -", "73 - 76 81 - 76 - -", "74 - - 69 62 - 74 -"
      ]
    ),
    "verdigris-jungle": score(
      "Verdigris Jungle · Beneath the Emerald Vault", "jungle", 118, 8,
      { Em: [52, 55, 59], Am: [45, 48, 52], C: [48, 52, 55], G: [43, 47, 50], D: [50, 54, 57], Bm: [47, 50, 54] },
      "Em Am C Bm Em G D Bm Am C Em D C G Bm Em".split(" "), [
        "76 - 79 83 - 79 76 -", "72 76 - 79 - 76 72 -", "76 - 79 - 83 86 - -", "74 78 - 81 - 78 74 -",
        "79 - 83 86 - 83 79 -", "81 79 - 76 - 74 76 -", "78 - 81 - 86 81 - -", "83 78 - 74 - 71 74 -",
        "84 - 79 76 - 72 76 -", "83 79 - 76 - 79 83 -", "88 - 83 79 - 76 79 -", "86 81 - 78 - 74 78 -",
        "84 - 88 84 - 79 76 -", "83 79 - 74 - 79 83 -", "86 - 83 78 - 74 71 -", "88 83 - 79 - 76 64 -"
      ]
    ),
    "mirrormere-basin": score(
      "Mirrormere Basin · The Last Water", "lakeside", 88, 9,
      { F: [41, 45, 48], C: [48, 52, 55], Dm: [50, 53, 57], Bb: [46, 50, 53], Am: [45, 48, 52], Gm: [43, 46, 50] },
      "F C Dm Bb F Am Gm C Dm Bb F C Gm Am Bb F".split(" "), [
        "72 - - 76 - 79 - 76 -", "76 - 79 - 83 - 79 - -", "77 - - 81 - 84 - 81 -", "74 - 77 - 81 - 77 - -",
        "72 - 76 - 79 - 84 - -", "76 - - 81 - 84 - 81 -", "74 - 77 - 82 - 77 - -", "76 - 79 - 83 - 79 - -",
        "84 - - 81 - 77 - 81 -", "86 - 82 - 77 - 74 - -", "84 - 79 - 76 - 72 - -", "83 - 79 - 76 - 79 - -",
        "82 - 79 - 74 - 77 - -", "81 - 84 - 81 - 76 - -", "77 - 81 - 86 - 81 - -", "84 - 79 - 76 - 72 - -"
      ]
    ),
    "saffron-expanse": score(
      "Saffron Expanse · The Caravan's Mirage", "desert", 100, 8,
      { Dm: [50, 53, 57], Eb: [51, 55, 58], C: [48, 52, 55], Bb: [46, 50, 53], A: [45, 49, 52], Gm: [43, 46, 50] },
      "Dm Eb Dm C Bb C Dm A Dm Eb Gm A Bb C A Dm".split(" "), [
        "74 - 77 78 - 81 77 -", "75 - 79 82 - 79 75 -", "74 - 77 81 - 78 74 -", "72 - 76 79 - 76 72 -",
        "70 - 74 77 - 74 70 -", "72 - 76 79 - 76 72 -", "74 77 78 - 81 - 77 -", "73 - 76 81 - 76 73 -",
        "86 - 82 81 - 77 74 -", "87 - 82 79 - 75 79 -", "82 - 79 74 - 70 74 -", "85 - 81 76 - 73 76 -",
        "82 - 77 74 - 70 74 -", "84 - 79 76 - 72 76 -", "81 - 85 81 - 76 73 -", "86 - 81 77 - 74 62 -"
      ]
    ),
    "deep-desert": score(
      "Deep Desert · Stones Beneath the Sand", "pyramid", 76, 7,
      { Cm: [48, 51, 55], Db: [49, 53, 56], Ab: [44, 48, 51], G: [43, 47, 50], Fm: [41, 44, 48], Eb: [51, 55, 58], Bdim: [47, 50, 53] },
      "Cm Db Cm G Ab Fm G Cm Eb Db Ab G Fm Bdim G Cm".split(" "), [
        "72 - - 75 79 - -", "73 - 77 - 80 - -", "72 - 75 79 - 75 -", "71 - - 74 79 - -",
        "68 - 72 - 75 - -", "68 - 72 77 - 72 -", "67 - - 71 74 - -", "72 - 75 79 - 72 -",
        "87 - - 82 79 - -", "85 - 80 - 77 - -", "84 - 80 75 - 72 -", "83 - - 79 74 - -",
        "80 - 84 80 - 75 -", "83 - 77 - 74 - -", "79 - - 74 71 - -", "84 - 79 75 - 72 -"
      ]
    ),
    "crimson-divide": score(
      "Crimson Divide · The Red Horizon", "crimson", 94, 10,
      { Dm: [50, 53, 57], Bb: [46, 50, 53], F: [41, 45, 48], C: [48, 52, 55], Gm: [43, 46, 50], A: [45, 49, 52], Eb: [51, 55, 58] },
      "Dm Bb Gm A Dm F Eb A Dm Bb F C Gm Eb A Dm".split(" "), [
        "74 - 77 - 81 - 77 75 - -", "70 - 74 - 77 - 74 70 - -", "70 - 74 - 79 - 77 74 - -", "73 - 76 - 81 - 76 73 - -",
        "74 77 81 - 84 - 81 77 - -", "77 - 81 84 - 81 - 77 - -", "75 - 79 - 82 79 75 - - -", "73 - 76 81 - 85 81 76 - -",
        "86 - 81 - 77 - 74 77 - -", "82 - 77 - 74 70 74 - - -", "84 - 81 77 - 81 84 - - -", "83 - 79 76 - 79 83 - - -",
        "82 - 79 74 - 77 82 - - -", "87 - 82 79 - 75 79 - - -", "85 - 81 76 - 73 76 - - -", "86 - 81 77 - 74 62 - - -"
      ]
    ),
    "bloodstone-coast": score(
      "Bloodstone Coast · Nocturne's Tide", "blood-coast", 82, 9,
      { Cm: [48, 51, 55], Ab: [44, 48, 51], Fm: [41, 44, 48], G: [43, 47, 50], Eb: [51, 55, 58], Ddim: [50, 53, 56] },
      "Cm Ab Fm G Cm Eb Ddim G Ab Cm Fm G Eb Ab G Cm".split(" "), [
        "72 - - 75 - 79 - 75 -", "68 - 72 - 75 - 72 - -", "65 - - 68 72 - 68 - -", "67 - 71 - 74 - 71 - -",
        "72 - 75 79 - 84 - 79 -", "75 - - 79 - 82 - 79 -", "74 - 77 - 80 - 77 - -", "71 - - 74 79 - 74 - -",
        "80 - 75 - 72 - 68 - -", "84 - - 79 75 - 72 - -", "77 - 80 - 84 - 80 - -", "83 - 79 - 74 - 71 - -",
        "87 - - 82 79 - 75 - -", "84 - 80 - 75 - 72 - -", "86 - 83 - 79 - 74 - -", "84 - - 79 75 - 72 - -"
      ]
    ),
    "ancient-battlefield": score(
      "Ancient Battlefield · March of the Lost", "battlefield", 78, 8,
      { Dm: [50, 53, 57], Bb: [46, 50, 53], Gm: [43, 46, 50], A: [45, 49, 52], F: [41, 45, 48], Eb: [51, 55, 58] },
      "Dm Bb Gm A Dm F Eb A Dm Bb Gm A F Eb A Dm".split(" "), [
        "62 - 65 - 69 - 65 -", "58 - 62 - 65 - 62 -", "55 - 58 - 62 - 58 -", "57 - 61 - 64 - 61 -",
        "62 65 - 69 74 - 69 -", "65 - 69 - 72 - 69 -", "63 - 67 - 70 - 67 -", "61 - 64 - 69 - 64 -",
        "74 - 69 - 65 - 62 -", "70 - 65 - 62 - 58 -", "67 - 62 - 58 - 55 -", "69 - 73 - 76 - 73 -",
        "77 - 72 - 69 - 65 -", "75 - 70 - 67 - 63 -", "73 - 69 - 64 - 61 -", "74 - 69 - 65 - 62 -"
      ]
    ),
    "duskpine-forest": score(
      "Duskpine Forest · The Cold Canopy", "dark-forest", 72, 7,
      { Em: [52, 55, 59], C: [48, 52, 55], G: [43, 47, 50], Bm: [47, 50, 54], Am: [45, 48, 52], D: [50, 54, 57], Fsh: [42, 46, 49] },
      "Em C G Bm Em Am D Bm C G Em Am D Fsh Bm Em".split(" "), [
        "76 - 79 - 83 - 79", "72 - 76 - 79 - 76", "71 - 74 - 79 - 74", "71 - 74 - 78 - 74",
        "79 - 83 - 88 - 83", "81 - 76 - 72 - 76", "78 - 81 - 86 - 81", "78 - 74 - 71 - 74",
        "84 - 79 - 76 - 72", "83 - 79 - 74 - 71", "88 - 83 - 79 - 76", "84 - 81 - 76 - 72",
        "86 - 81 - 78 - 74", "82 - 78 - 73 - 70", "83 - 78 - 74 - 71", "76 - 79 - 83 - 76"
      ]
    ),
    "combat": score(
      "Battle · Steel and Resolve", "skirmish", 162, 8,
      { Dm: [50, 53, 57], Bb: [46, 50, 53], F: [53, 57, 60], C: [48, 52, 55], Gm: [43, 46, 50], A: [45, 49, 52] },
      "Dm Bb F C Gm Bb A A Dm F Gm Bb Dm C A Dm".split(" "), [
        "74 - 77 81 77 - 74 -", "70 - 74 77 74 - 70 -", "69 72 77 - 81 77 72 -", "76 - 79 83 79 - 76 -",
        "70 - 74 77 82 - 77 -", "74 - 77 81 77 74 70 -", "73 76 81 - 85 81 76 -", "73 - 76 81 76 - 73 -",
        "86 - 84 81 77 81 84 -", "81 - 84 88 84 - 81 -", "82 79 77 - 74 77 79 -", "82 - 81 77 74 - 70 -",
        "74 77 81 - 86 81 77 -", "76 - 79 83 79 76 72 -", "73 76 81 - 85 81 76 -", "74 - 69 74 77 - 74 -"
      ]
    ),
    "ur-mat-false-dreamer": score(
      "Ur-Mat · The Dream That Devours", "false-dreamer", 94, 6,
      { Bm: [35, 38, 42], C: [36, 40, 43], Fsh: [30, 34, 37], Em: [40, 43, 47], G: [31, 35, 38], Ddim: [38, 41, 44], Am: [33, 36, 40] },
      "Bm C Bm Fsh Em C Ddim Fsh Bm G C Fsh Em Am Ddim Bm Bm C Em Fsh G Ddim C Fsh Em C Bm Ddim G C Fsh Bm".split(" "), [
        "71 - 72 78 - 74", "72 - 76 79 - 78", "71 74 - 78 77 -", "70 - 73 78 - -",
        "76 - 79 83 - 78", "79 - 76 72 - 71", "77 80 - 83 80 -", "78 - 73 70 - -",
        "83 - 84 90 - 86", "86 - 83 79 - 78", "84 88 - 91 88 -", "82 - 85 90 - 85",
        "88 - 91 95 - 90", "93 89 - 88 84 -", "89 - 92 95 - 92", "83 - 78 74 - -",
        "83 84 86 90 86 -", "84 88 91 - 88 83", "88 - 91 95 91 88", "90 85 82 - 85 90",
        "91 - 86 83 79 83", "89 92 - 95 92 89", "96 - 91 88 84 83", "94 - 90 85 82 85",
        "100 - 95 91 88 91", "96 95 91 - 88 84", "95 - 90 86 83 84", "101 - 98 95 92 89",
        "98 - 95 91 86 83", "96 - 91 88 84 83", "94 90 85 - 82 78", "83 - 78 74 72 71"
      ]
    ),
    "ornn-god-of-the-forge": score(
      "Ornn · Hammer of the First Flame", "forge-god", 116, 8,
      { Cm: [36, 39, 43], Ab: [32, 36, 39], Eb: [39, 43, 46], Bb: [34, 38, 41], Fm: [41, 44, 48], G: [31, 35, 38], Ddim: [38, 41, 44] },
      "Cm Eb Ab Bb Cm Fm G G Ab Eb Fm G Cm Bb G Cm Cm Ab Eb Bb Fm Ddim G Cm Ab Fm Eb Bb Cm Ab G Cm".split(" "), [
        "60 - 67 72 - 67 63 -", "67 - 70 - 75 70 67 -", "68 - 72 75 - 72 68 -", "70 - 65 62 - 65 70 -",
        "72 - 67 - 75 72 67 -", "77 75 72 - 68 72 77 -", "71 - 74 79 - 74 71 -", "67 - 71 - 74 71 67 -",
        "80 - 75 72 - 75 80 -", "79 - 82 - 87 82 79 -", "84 - 80 77 - 80 84 -", "83 79 74 - 71 74 79 -",
        "84 - 79 75 - 72 75 79", "82 77 74 - 77 82 86 -", "83 - 86 91 - 86 83 -", "84 - 79 - 75 72 67 -",
        "84 87 91 - 87 84 79 -", "87 - 84 80 - 84 87 -", "91 87 82 - 79 82 87 -", "89 - 86 82 - 77 82 86",
        "89 92 96 - 92 89 84 -", "89 - 92 95 - 92 89 -", "91 86 83 - 79 83 86 -", "96 - 91 87 84 - 87 91",
        "92 - 87 84 - 80 84 87", "96 92 89 - 84 89 92 -", "99 - 94 91 - 87 91 94", "98 94 89 - 86 89 94 -",
        "96 - 99 103 - 99 96 -", "99 - 96 92 - 87 84 -", "95 - 91 86 - 83 79 -", "96 - 91 - 84 79 72 -"
      ]
    ),
    "great-imp-jiruan": score(
      "Great Imp Ji'ruan · Court of Ash", "boss", 150, 7,
      { Dm: [50, 53, 57], Eb: [51, 55, 58], Cm: [48, 51, 55], Bb: [46, 50, 53], Gm: [43, 46, 50], A: [45, 49, 52], Ebdim: [51, 54, 57] },
      "Dm Eb Dm A Cm Eb Dm A Gm Eb Cm A Dm Eb A Dm Eb Gm Dm Ebdim Cm Eb Bb A Dm Ebdim Gm A Dm Eb A Dm".split(" "), [
        "74 - 75 77 - 81 77", "75 - 77 79 - 82 79", "86 84 82 - 81 77 -", "73 - 76 81 - 76 -",
        "72 - 75 79 - 84 79", "82 79 77 - 75 79 -", "74 75 77 81 77 - 75", "73 - 76 81 - 73 -",
        "79 82 - 86 82 79 -", "87 84 82 - 79 75 -", "84 82 79 75 72 - 75", "81 - 76 73 76 81 -",
        "86 84 82 81 77 - 74", "87 - 84 82 79 75 -", "85 81 76 - 73 76 -", "86 - 81 77 74 - -",
        "87 - 89 91 - 94 91", "86 82 79 - 82 86 -", "86 87 89 93 89 - 86", "87 84 81 78 75 - 78",
        "84 - 87 91 - 87 84", "82 79 75 - 79 82 -", "82 86 89 - 86 82 77", "85 - 81 76 73 76 -",
        "86 87 89 93 89 87 86", "87 - 84 81 78 81 -", "82 86 89 94 89 86 -", "85 81 76 73 76 81 -",
        "86 - 89 93 89 86 82", "87 84 82 79 75 79 -", "85 81 76 - 73 76 -", "86 - 81 77 74 - -"
      ]
    ),
    "juila-grand-forest-matriach": score(
      "Juila · The Canopy Remembers", "forest-matriarch", 116, 12,
      { Am: [45, 48, 52], F: [41, 45, 48], Dm: [50, 53, 57], E: [40, 44, 47], C: [48, 52, 55], G: [43, 47, 50], Bb: [46, 50, 53] },
      "Am F Dm E Am C G E Dm F C E Am Bb Dm E Am G F E Dm Bb E Am".split(" "), [
        "76 - - 79 - 77 76 - 72 - 69 -", "77 - 76 - 72 - 69 - 72 - 76 -", "74 - 77 - 81 - 77 - 74 - 69 -", "76 - 80 - 83 - 80 - 76 - 71 -",
        "76 - - 79 - 84 81 - 79 - 76 -", "79 - 76 - 72 - 76 - 79 - 84 -", "74 - 79 - 83 - 79 - 74 - 71 -", "80 - 83 - 88 - 83 - 80 - 76 -",
        "86 - 81 - 77 - 74 - 77 - 81 -", "84 - 81 - 77 - 72 - 77 - 81 -", "88 - 84 - 79 - 76 - 79 - 84 -", "88 - 83 - 80 - 76 - 80 - 83 -",
        "88 - 91 - 88 84 - 81 - 84 - -", "89 - 93 - 89 86 - 82 - 86 - -", "86 - 89 - 93 89 - 86 - 81 - -", "88 - 92 - 95 92 - 88 - 83 - -",
        "88 - 91 - 96 93 - 88 - 84 - -", "91 - 86 - 83 79 - 83 - 86 - -", "89 - 84 - 81 77 - 81 - 84 - -", "92 - 95 - 92 88 - 83 - 80 - -",
        "93 - 89 - 86 81 - 86 - 89 - -", "94 - 89 - 86 82 - 86 - 89 - -", "92 - 88 - 83 80 - 83 - 88 - -", "88 - 91 - 96 93 - 88 - 76 - -"
      ]
    ),
    "nygheir-the-wounded": score(
      "Nygheir · Scars Beneath Stone", "wounded-dragon", 134, 9,
      { Em: [40, 43, 47], F: [41, 45, 48], C: [36, 40, 43], B: [35, 39, 42], G: [43, 47, 50], D: [38, 42, 45], Am: [45, 48, 52] },
      "Em F Em B C G Am B Em F C B Em F G B Am C F B Em C B Em".split(" "), [
        "76 - 77 - 76 71 - 67 -", "77 - 79 - 77 72 - 69 -", "76 - 74 - 71 67 - 64 -", "75 - 78 - 75 71 - 66 -",
        "72 - 76 - 79 76 - 72 -", "74 - 79 - 83 79 - 74 -", "76 - 72 - 69 72 - 76 -", "75 78 - 81 - 78 75 - -",
        "88 - 89 - 88 83 - 79 -", "89 - 91 - 89 84 - 81 -", "84 - 88 - 91 88 - 84 -", "87 90 - 87 - 83 78 - -",
        "88 89 91 - 88 84 83 - -", "89 - 93 - 89 84 81 - -", "91 - 88 86 83 - 79 - -", "87 - 90 94 - 90 87 - -",
        "93 - 88 84 81 - 84 88 -", "91 - 88 84 79 - 84 88 -", "89 93 - 96 - 93 89 - -", "87 90 94 - 90 87 83 - -",
        "95 - 93 91 88 - 84 83 -", "91 - 88 84 79 - 76 79 -", "90 - 87 83 78 - 75 78 -", "88 89 91 - 88 83 76 - -"
      ]
    ),
    "anatemis-white-star": score(
      "Anatemis · Crown of the White Star", "star-dragon", 146, 8,
      { Bm: [47, 50, 54], G: [43, 47, 50], D: [50, 54, 57], A: [45, 49, 52], Em: [40, 43, 47], Fsh: [42, 46, 49], C: [48, 52, 55], B: [47, 51, 54] },
      "Bm G D A Bm Em G Fsh Bm G D A Em G Fsh Bm Bm C G A Bm Em Fsh B Bm G D A Em Fsh B Bm".split(" "), [
        "71 - 74 - 78 76 74 -", "79 - 78 - 74 - 71 -", "74 - 78 81 - 78 74 -", "76 - 73 - 69 73 76 -",
        "83 - 81 - 78 - 74 -", "79 - 76 - 71 76 79 -", "78 - 74 - 71 - 67 -", "73 - 76 78 - 76 73 -",
        "83 - 86 - 83 81 78 -", "86 - 83 79 - 78 74 -", "81 - 86 - 90 86 81 -", "85 - 81 - 76 81 85 -",
        "88 - 84 79 - 76 79 -", "86 - 83 - 79 83 86 -", "85 - 81 - 78 81 85 -", "83 - 86 90 - 86 83 -",
        "95 - 90 - 86 90 95 -", "96 - 92 - 88 92 96 -", "91 - 86 - 83 86 91 -", "93 - 88 85 - 81 85 -",
        "95 - 90 - 86 90 95 -", "91 - 88 83 - 79 83 -", "97 - 93 - 90 93 97 -", "99 - 94 90 - 87 90 -",
        "95 97 99 - 102 99 95 -", "98 - 95 91 - 86 91 -", "93 95 98 - 102 98 93 -", "97 - 93 88 - 85 88 -",
        "100 - 95 - 91 95 100 -", "97 - 93 90 - 85 90 -", "99 - 95 - 90 95 99 -", "95 - 90 - 83 - 71 -"
      ]
    ),
    "zik-lun-armored-hulk": score(
      "Zik Lun · The Mountain Breaks", "juggernaut", 92, 8,
      { Dm: [38, 41, 45], Bb: [34, 38, 41], C: [36, 40, 43], A: [33, 37, 40], Eb: [39, 42, 46], Gm: [43, 46, 50] },
      "Dm Dm Bb A Dm Eb Bb A Gm Bb C A Dm Eb A Dm Dm Bb Gm A Dm Eb C A Gm Bb C A Dm Eb A Dm".split(" "), [
        "50 - - - 53 - 57 -", "50 - 53 - 57 - - -", "58 - - 53 - 50 - -", "49 - 52 - 57 - - -",
        "62 - - 57 - 65 - -", "63 - 66 - 63 - 58 -", "58 - - 53 - 50 - -", "61 - 57 - 52 - - -",
        "67 - - 62 - 58 - -", "70 - 67 - 65 - - -", "72 - - 67 - 64 - -", "69 - 73 - 76 - - -",
        "74 - 69 - 65 - 62 -", "75 - 70 - 66 - 63 -", "73 - 69 - 64 - 61 -", "74 - - 57 - 50 - -",
        "62 - - 65 - 69 - -", "70 - 65 - 62 - 58 -", "67 - 70 - 74 - 70 -", "73 - 69 - 64 - 61 -",
        "74 - - 69 - 65 - -", "75 - 78 - 75 - 70 -", "72 - 76 - 79 - 76 -", "73 - - 69 - 64 - -",
        "79 - - 74 - 70 - -", "82 - 79 - 77 - 70 -", "84 - - 79 - 76 - -", "81 - 76 - 73 - 69 -",
        "86 - 81 - 77 - 74 -", "87 - 82 - 78 - 75 -", "85 - 81 - 76 - 73 -", "86 - - 69 - 62 - -"
      ]
    )
  };
  scores["tankina-disciple-of-talos"] = score(
    "Tankina · Blood Beneath the Thunder", "storm-talos", 148, 8,
    { Em: [40, 43, 47], F: [41, 45, 48], B: [35, 39, 42], Cm: [36, 39, 43], Am: [45, 48, 52], D: [38, 42, 45], Bb: [34, 38, 41], Ed: [40, 43, 46] },
    "Em F Em B Cm F Ed B Em Am D B Cm F Bb B Am Em F B Cm Am Ed B Em F Cm B Am Bb B Em".split(" "), [
      "76 77 - 71 - 76 79 -", "77 - 81 77 - 72 69 -", "76 - 79 83 - 79 76 71", "75 78 - 83 - 78 75 -",
      "79 - 75 72 - 79 82 -", "81 77 - 72 69 - 77 -", "76 - 79 82 - 79 76 -", "78 75 - 71 - 66 71 -",
      "88 89 - 83 - 88 91 -", "93 - 88 84 - 81 84 -", "90 86 - 81 - 78 86 -", "87 - 90 95 - 90 87 83",
      "91 87 - 84 - 79 87 -", "89 - 93 89 - 84 81 -", "94 89 - 86 - 82 86 -", "90 87 - 83 - 78 87 -",
      "81 - - 84 88 - 93 -", "79 - 76 - 71 76 - -", "81 - - 77 72 - 69 -", "78 - 75 71 - 66 - -",
      "84 - 79 75 - 72 75 -", "88 - - 84 81 - 76 -", "82 79 - 76 - 70 76 -", "83 78 - 75 - 71 78 -",
      "100 101 - 95 - 100 103 -", "101 - 105 101 - 96 93 -", "103 99 - 96 - 91 99 -", "99 - 102 107 - 102 99 95",
      "105 100 - 96 - 93 96 -", "106 - 101 98 - 94 98 -", "102 99 - 95 - 90 87 -", "100 95 - 91 - 88 83 -"
    ]
  );
  // The summit is the upper reach of the same mountain; keep its high-altitude theme.
  scores["stoneveil-summit"] = scores["high-stoneveil"];
  // Remastered region and battle themes replace the original scores of the same name.
  // Boss themes keep the original engine below.
  if (window.MusicComposer && window.MusicThemes) {
    for (const spec of window.MusicThemes.all()) {
      try { scores[spec.id] = window.MusicComposer.compile(spec); }
      catch (error) { console.error("Music theme failed to compile:", spec.id, error); }
    }
  }

  let volume = 1;
  try {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved !== null && saved.trim() && Number.isFinite(Number(saved))) volume = Math.max(0, Math.min(1, Number(saved)));
    else if (localStorage.getItem("stat-fick-music-enabled") === "false") volume = 0;
  } catch { /* Private browsing may deny storage. */ }
  let enabled = volume > 0;
  let context = null;
  let master = null;
  let delay = null;
  let hall = null;
  let activeBus = null;
  let activeTheme = null;
  let clock = null;
  let nextNoteTime = 0;
  let step = 0;
  let noise = null;
  let audioReady = false;
  let offlineRendering = false;
  let activePerformance = null;

  // Ordinary battles play the region's own arrangement of the battle theme.
  function desiredTheme() {
    const region = typeof worldState !== "undefined" ? worldState?.regionId : null;
    if (window.Combat?.active()) {
      const theme = window.Combat.musicTheme?.() || "combat";
      if (theme === "combat" && scores["combat@" + region]) return "combat@" + region;
      // A boss theme that failed to compile falls back to the battle theme.
      return scores[theme] ? theme : "combat";
    }
    return scores[region] ? region : "alder-vale";
  }

  function paint() {
    const available = Boolean(AudioContextClass);
    const track = scores[desiredTheme()]?.title || scores["alder-vale"].title;
    if (volumeSlider) {
      volumeSlider.disabled = !available;
      volumeSlider.value = String(Math.round(volume * 100));
      volumeSlider.setAttribute("aria-valuetext", Math.round(volume * 100) + "%");
    }
    if (volumeLabel) volumeLabel.textContent = Math.round(volume * 100) + "%";
    if (volumeHelp) volumeHelp.textContent = available ? "Adjust regional and battle music. 0% is silent." : "Sound is unavailable in this browser.";
    if (trackLabel) trackLabel.textContent = "Current theme: " + track;
  }

  function setupAudio(suppliedContext = null) {
    if (context || (!AudioContextClass && !suppliedContext)) return;
    context = suppliedContext || new AudioContextClass();
    master = context.createGain();
    // Exports retain their original loudness, independent of player preferences.
    master.gain.value = 0.58 * (offlineRendering ? 1 : volume);
    const limiter = context.createDynamicsCompressor();
    limiter.threshold.value = -18;
    limiter.knee.value = 18;
    limiter.ratio.value = 3;
    limiter.attack.value = 0.006;
    limiter.release.value = 0.22;
    master.connect(limiter);
    limiter.connect(context.destination);
    delay = context.createDelay(0.6);
    delay.delayTime.value = 0.23;
    const feedback = context.createGain();
    feedback.gain.value = 0.12;
    const wet = context.createGain();
    wet.gain.value = 0.18;
    delay.connect(feedback);
    feedback.connect(delay);
    delay.connect(wet);
    wet.connect(master);
    // A short synthetic hall gives brass, strings and choir room without audio assets.
    hall = context.createConvolver();
    const hallBuffer = context.createBuffer(2, Math.ceil(context.sampleRate * 1.35), context.sampleRate);
    for (let channel = 0; channel < 2; channel++) {
      const samples = hallBuffer.getChannelData(channel);
      for (let i = 0; i < samples.length; i++) {
        const seconds = i / context.sampleRate;
        samples[i] = (Math.random() * 2 - 1) * Math.exp(-seconds * 4.2);
      }
    }
    hall.buffer = hallBuffer;
    const hallFilter = context.createBiquadFilter();
    hallFilter.type = "lowpass";
    hallFilter.frequency.value = 3400;
    const hallLevel = context.createGain();
    hallLevel.gain.value = 0.16;
    hall.connect(hallFilter);
    hallFilter.connect(hallLevel);
    hallLevel.connect(master);
    noise = context.createBuffer(1, Math.ceil(context.sampleRate * 0.5), context.sampleRate);
    const samples = noise.getChannelData(0);
    for (let i = 0; i < samples.length; i++) samples[i] = Math.random() * 2 - 1;
    audioReady = true;
  }

  function frequency(midi) { return 440 * 2 ** ((midi - 69) / 12); }

  const voices = {
    flute: { wave: "sine", cutoff: 2400, attack: 0.075, partial: 0.12, ratio: 2 },
    harp: { wave: "triangle", cutoff: 3000, attack: 0.006, partial: 0.17, ratio: 2, decay: true },
    bell: { wave: "sine", cutoff: 5000, attack: 0.004, partial: 0.32, ratio: 2.01, decay: true },
    horn: { wave: "sawtooth", cutoff: 620, attack: 0.10 },
    pad: { wave: "triangle", cutoff: 1100, attack: 0.12 },
    wood: { wave: "triangle", cutoff: 1500, attack: 0.007, partial: 0.14, ratio: 3, decay: true },
    strings: { wave: "sawtooth", cutoff: 1050, attack: 0.045 },
    dark: { wave: "sawtooth", cutoff: 1550, attack: 0.016 },
    bass: { wave: "triangle", cutoff: 600, attack: 0.012 },
    brass: { wave: "sawtooth", cutoff: 1500, attack: 0.028, sustain: true, partial: 0.32, ratio: 1, detune: 7, partialWave: "sawtooth" },
    choir: { wave: "sine", cutoff: 2300, attack: 0.16, sustain: true, partial: 0.38, ratio: 1, detune: -5, partialWave: "triangle" },
    warStrings: { wave: "sawtooth", cutoff: 1150, attack: 0.012, partial: 0.22, ratio: 1, detune: -7, partialWave: "sawtooth" },
    warBass: { wave: "sawtooth", cutoff: 530, attack: 0.008, partial: 0.28, ratio: 0.5, partialWave: "sine" },
    fiddle: { wave: "sawtooth", cutoff: 1900, attack: 0.075, sustain: true, partial: 0.19, ratio: 1, detune: 6, partialWave: "sawtooth" },
    kalimba: { wave: "sine", cutoff: 4300, attack: 0.003, partial: 0.36, ratio: 2.76, decay: true },
    reed: { wave: "square", cutoff: 1250, attack: 0.035, partial: 0.11, ratio: 2, partialWave: "sine" },
    guitar: { wave: "sawtooth", cutoff: 2400, attack: 0.006, partial: 0.19, ratio: 0.5, partialWave: "triangle", decay: true },
    glass: { wave: "sine", cutoff: 5500, attack: 0.008, partial: 0.26, ratio: 3.02, decay: true },
    organ: { wave: "triangle", cutoff: 1200, attack: 0.04, sustain: true, partial: 0.34, ratio: 2, partialWave: "sine" },
    trombone: { wave: "sawtooth", cutoff: 720, attack: 0.09, sustain: true, partial: 0.20, ratio: 1, detune: -8, partialWave: "sawtooth" },
    ritualChoir: { wave: "sine", cutoff: 1800, attack: 0.19, sustain: true, swell: true, partial: 0.58, ratio: 1, detune: 8, partialWave: "triangle" },
    violins: { wave: "sawtooth", cutoff: 2050, attack: 0.045, sustain: true, partial: 0.21, ratio: 1, detune: -7, partialWave: "sawtooth" },
    lowStrings: { wave: "sawtooth", cutoff: 820, attack: 0.09, sustain: true, swell: true, partial: 0.24, ratio: 0.5, partialWave: "triangle" },
    hornSection: { wave: "sawtooth", cutoff: 1450, attack: 0.095, sustain: true, swell: true, partial: 0.29, ratio: 1, detune: 9, partialWave: "sawtooth" }
  };

  function tone(midi, when, length, volume, voice, bus) {
    const preset = voices[voice];
    const oscillator = context.createOscillator();
    const filter = context.createBiquadFilter();
    const envelope = context.createGain();
    oscillator.type = preset.wave;
    oscillator.frequency.value = frequency(midi);
    filter.type = "lowpass";
    filter.frequency.value = preset.cutoff;
    if (preset.swell && length > 0.18) {
      filter.frequency.setValueAtTime(Math.max(180, preset.cutoff * 0.48), when);
      filter.frequency.linearRampToValueAtTime(preset.cutoff, when + Math.min(length * 0.65, 0.8));
    }
    const attack = Math.min(preset.attack, length * 0.3);
    envelope.gain.setValueAtTime(0.0001, when);
    envelope.gain.exponentialRampToValueAtTime(volume, when + attack);
    if (preset.decay && length > attack + 0.03) envelope.gain.exponentialRampToValueAtTime(Math.max(0.0001, volume * 0.22), when + length * 0.75);
    if (preset.sustain && length > attack + 0.09) envelope.gain.linearRampToValueAtTime(volume * 0.76, when + length - 0.08);
    envelope.gain.exponentialRampToValueAtTime(0.0001, when + length);
    oscillator.connect(filter);
    let partial = null;
    let partialGain = null;
    if (preset.partial) {
      partial = context.createOscillator();
      partialGain = context.createGain();
      partial.type = preset.partialWave || "sine";
      partial.frequency.value = frequency(midi) * preset.ratio;
      partial.detune.value = preset.detune || 0;
      partialGain.gain.value = preset.partial;
      partial.connect(partialGain);
      partialGain.connect(filter);
      partial.start(when);
      partial.stop(when + length + 0.02);
    }
    filter.connect(envelope);
    envelope.connect(bus);
    oscillator.start(when);
    oscillator.stop(when + length + 0.02);
    oscillator.onended = () => {
      oscillator.disconnect();
      partial?.disconnect();
      partialGain?.disconnect();
      filter.disconnect();
      envelope.disconnect();
    };
  }

  function texture(when, length, volume, kind, bus) {
    const source = context.createBufferSource();
    const filter = context.createBiquadFilter();
    const envelope = context.createGain();
    source.buffer = noise;
    source.loop = true;
    filter.type = kind === "wind" ? "lowpass" : "highpass";
    filter.frequency.value = kind === "wind" ? 480 : 3400;
    envelope.gain.setValueAtTime(0.0001, when);
    envelope.gain.exponentialRampToValueAtTime(volume, when + length * 0.3);
    envelope.gain.exponentialRampToValueAtTime(0.0001, when + length);
    source.connect(filter);
    filter.connect(envelope);
    envelope.connect(bus);
    source.start(when);
    source.stop(when + length + 0.01);
    source.onended = () => { source.disconnect(); filter.disconnect(); envelope.disconnect(); };
  }

  function drum(when, kind, bus) {
    if (kind === "impact") {
      drum(when, "war", bus);
      drum(when, "crash", bus);
      drum(when + 0.055, "tom", bus);
      return;
    }
    if (kind === "gong") {
      tone(62, when, 1.5, 0.023, "bell", bus);
      tone(68, when + 0.015, 1.2, 0.018, "glass", bus);
      drum(when, "crash", bus);
      return;
    }
    if (kind === "anvil") {
      tone(82, when, 0.36, 0.016, "glass", bus);
      tone(89, when, 0.21, 0.008, "bell", bus);
      return;
    }
    if (kind === "kick" || kind === "tom" || kind === "war") {
      const oscillator = context.createOscillator();
      const envelope = context.createGain();
      oscillator.type = "sine";
      const length = kind === "war" ? 0.30 : 0.21;
      oscillator.frequency.setValueAtTime(kind === "tom" ? 165 : kind === "war" ? 105 : 115, when);
      oscillator.frequency.exponentialRampToValueAtTime(kind === "tom" ? 80 : kind === "war" ? 38 : 46, when + length - 0.03);
      envelope.gain.setValueAtTime(kind === "tom" ? 0.055 : kind === "war" ? 0.16 : 0.10, when);
      envelope.gain.exponentialRampToValueAtTime(0.0001, when + length);
      oscillator.connect(envelope);
      envelope.connect(bus);
      oscillator.start(when);
      oscillator.stop(when + length + 0.01);
      oscillator.onended = () => { oscillator.disconnect(); envelope.disconnect(); };
      return;
    }
    const source = context.createBufferSource();
    const filter = context.createBiquadFilter();
    const envelope = context.createGain();
    const length = kind === "crash" ? 1.1 : kind === "snare" ? 0.14 : 0.055;
    source.buffer = noise;
    source.loop = kind === "crash";
    filter.type = "highpass";
    filter.frequency.value = kind === "crash" ? 1250 : kind === "snare" ? 1100 : 5100;
    envelope.gain.setValueAtTime(kind === "crash" ? 0.035 : kind === "snare" ? 0.035 : 0.012, when);
    envelope.gain.exponentialRampToValueAtTime(0.0001, when + length);
    source.connect(filter);
    filter.connect(envelope);
    envelope.connect(bus);
    source.start(when, Math.random() * 0.2);
    source.stop(when + length);
    source.onended = () => { source.disconnect(); filter.disconnect(); envelope.disconnect(); };
  }

  function playStep(when) {
    const score = scores[activeTheme];
    const pulse = step % score.meter;
    const bar = Math.floor(step / score.meter) % score.route.length;
    const loop = Math.floor(step / (score.meter * score.route.length));
    const chord = score.harmony[score.route[bar]];
    const tick = 30 / score.tempo;
    const phrase = score.lead[bar];
    const melody = phrase[pulse];
    const secondHalf = bar >= 8;
    const voice = (pitch, duration, volume, instrument) => tone(pitch, when, Math.max(0.035, duration * tick), volume, instrument, activeBus);
    const melodyDuration = () => {
      let spaces = 1;
      while (pulse + spaces < score.meter && phrase[pulse + spaces] == null) spaces++;
      return Math.min(4.8, spaces - 0.14);
    };
    if (score.kind === "pastoral") {
      if (pulse === 0) {
        for (const pitch of chord) voice(pitch + 12, 5.7, 0.005, "strings");
        voice(chord[0] - 12, 2.5, 0.027, "bass");
        if (bar === 0 || bar === 8) drum(when, "tom", activeBus);
      }
      if (pulse === 3) voice(chord[2] - 12, 2.3, 0.019, "bass");
      voice(chord[[0, 1, 2, 1, 0, 2][pulse]] + 12, 0.72, 0.014, "harp");
      if (pulse === 0 || pulse === 3) voice(chord[pulse === 0 ? 0 : 2] + 24, 0.8, 0.009, "kalimba");
      if (melody != null) {
        voice(melody, melodyDuration(), 0.028, secondHalf ? "fiddle" : "flute");
        if (bar >= 12 && pulse === 0) voice(melody - 12, 2.6, 0.011, "flute");
      }
      if (secondHalf && (pulse === 1 || pulse === 4)) voice(chord[pulse === 1 ? 1 : 2] + 19, 1.2, 0.008, "wood");
      if (bar >= 4 && (pulse === 0 || pulse === 3)) drum(when, "hat", activeBus);
    } else if (score.kind === "highlands") {
      if (pulse === 0) {
        voice(chord[0], 7.7, 0.013, "organ");
        voice(chord[2], 7.7, 0.007, "pad");
        voice(chord[0] - 12, 2.4, 0.045, "warBass");
        texture(when, tick * 7.7, 0.0026, "wind", activeBus);
        drum(when, "tom", activeBus);
      }
      if (pulse === 4) {
        voice(chord[2] - 12, 2, 0.027, "bass");
        drum(when, secondHalf ? "war" : "tom", activeBus);
      }
      if ([0, 3, 5, 7].includes(pulse)) voice(chord[[0, 0, 1, 2][[0, 3, 5, 7].indexOf(pulse)]] + 12, 0.85, 0.013, "warStrings");
      if (melody != null) {
        voice(melody, melodyDuration(), 0.031, "horn");
        if (bar >= 12 && pulse === 0) voice(melody + 12, 2.8, 0.012, "bell");
      }
      if (pulse === 6 && bar % 2 === 1) voice(chord[1] + 24, 1.4, 0.011, "bell");
      if (secondHalf && (pulse === 2 || pulse === 6)) drum(when, "snare", activeBus);
    } else if (score.kind === "summit") {
      if (pulse === 0) {
        voice(chord[0] - 12, 6.8, 0.025, "lowStrings");
        voice(chord[0], 6.8, 0.009, "ritualChoir");
        voice(chord[2] + 12, 5.8, 0.006, "choir");
        texture(when, tick * 6.8, 0.0032, "wind", activeBus);
      }
      if (pulse === 3) voice(chord[2] - 12, 2.8, 0.018, "bass");
      if ([0, 2, 5].includes(pulse)) voice(chord[[0, 1, 2][[0, 2, 5].indexOf(pulse)]] + 24, 1.25, 0.008, "glass");
      if (melody != null) {
        voice(melody, melodyDuration(), 0.025, secondHalf ? "hornSection" : "flute");
        if (secondHalf && pulse === 0) voice(melody - 12, 3.1, 0.008, "horn");
      }
      if (bar % 4 === 3 && pulse === 6) drum(when, "gong", activeBus);
    } else if (score.kind === "forest") {
      if (pulse === 0) {
        for (const pitch of chord) voice(pitch + 12, 9.5, 0.005, "choir");
        voice(chord[0] - 12, 4.4, 0.015, "bass");
        texture(when, tick * 6.5, 0.0018, "leaves", activeBus);
      }
      const pluck = { 0: 0, 2: 2, 5: 1, 7: 2, 9: 0 }[pulse];
      if (pluck != null) voice(chord[pluck] + 24, 1.2, 0.012, "kalimba");
      if (pulse === 4 || pulse === 8) voice(chord[pulse === 4 ? 1 : 2] + 36, 1.8, 0.007, "glass");
      if (melody != null) {
        voice(melody, melodyDuration(), 0.020, secondHalf ? "flute" : "reed");
        if (bar >= 12 && pulse === 0) voice(melody - 12, 3.5, 0.008, "wood");
      }
      if (bar >= 8 && pulse === 5) voice(chord[0], 3.5, 0.008, "organ");
    } else if (score.kind === "seaside") {
      // Salt wind, rolling surf, and a bright road tune shared by the ports.
      if (pulse === 0) {
        for (const pitch of chord) voice(pitch + 12, 8.6, 0.005, "strings");
        voice(chord[0] - 12, 4.2, 0.022, "bass");
        texture(when, tick * 8.5, 0.0017, "wind", activeBus);
      }
      if ([0, 2, 4, 6, 8].includes(pulse)) voice(chord[[0, 2, 1, 2, 0][[0, 2, 4, 6, 8].indexOf(pulse)]] + 24, 0.9, 0.016, "harp");
      if (pulse === 4) voice(chord[2] - 12, 2.3, 0.017, "bass");
      if (pulse === 6 && bar % 2 === 1) voice(chord[1] + 36, 1.5, 0.009, "bell");
      if (melody != null) {
        voice(melody, melodyDuration(), 0.028, secondHalf ? "fiddle" : "flute");
        if (secondHalf && pulse === 0) voice(melody - 12, 2.5, 0.008, "flute");
      }
      if (pulse === 0 && bar % 4 === 0) drum(when, "tom", activeBus);
    } else if (score.kind === "volcanic") {
      // Uneven ash-footsteps under a restrained, molten brass melody.
      if (pulse === 0) {
        voice(chord[0] - 12, 10.5, 0.032, "lowStrings");
        for (const pitch of chord) voice(pitch + 12, 10.3, secondHalf ? 0.009 : 0.005, "ritualChoir");
        texture(when, tick * 10.5, 0.0022, "wind", activeBus);
        if (bar % 4 === 0) drum(when, "gong", activeBus);
      }
      if ([0, 3, 5, 8].includes(pulse)) voice(chord[[0, 1, 2, 0][[0, 3, 5, 8].indexOf(pulse)]] + 12, 0.75, secondHalf ? 0.018 : 0.012, "warStrings");
      if (pulse === 0 || pulse === 5 || pulse === 9) voice(chord[0] - 12, 1.5, secondHalf ? 0.055 : 0.039, "warBass");
      if (melody != null) {
        voice(melody, melodyDuration(), secondHalf ? 0.034 : 0.025, secondHalf ? "hornSection" : "reed");
        if (secondHalf && pulse === 0) voice(melody - 12, 2.5, 0.011, "ritualChoir");
      }
      if (pulse === 0 || pulse === 7) drum(when, secondHalf ? "war" : "tom", activeBus);
      if (secondHalf && pulse === 4) drum(when, "snare", activeBus);
    } else if (score.kind === "tempest") {
      // Rolling thunder under a restless minor-key motif, with rain-like plucks.
      if (pulse === 0) {
        voice(chord[0] - 12, 7.7, 0.028, "lowStrings");
        voice(chord[1] + 12, 7.6, 0.007, "organ");
        texture(when, tick * 7.8, 0.003, "wind", activeBus);
        if (bar % 4 === 0) drum(when, "tom", activeBus);
      }
      if ([0, 2, 4, 6].includes(pulse)) voice(chord[[0, 2, 1, 2][pulse / 2]] + 24, .7, 0.013, "glass");
      if (pulse === 4) voice(chord[0] - 12, 2.3, 0.028, "bass");
      if (melody != null) voice(melody, melodyDuration(), secondHalf ? 0.032 : 0.025, secondHalf ? "horn" : "reed");
      if (secondHalf && (pulse === 0 || pulse === 4)) drum(when, pulse === 0 ? "war" : "tom", activeBus);
    } else if (score.kind === "marsh") {
      // Hollow reeds, slow steps on a raised causeway, and distant marsh bells.
      if (pulse === 0) {
        voice(chord[0] - 12, 7.5, 0.019, "lowStrings");
        voice(chord[1] + 12, 7.3, 0.007, "organ");
        texture(when, tick * 7.8, 0.0018, "wind", activeBus);
      }
      if ([0, 3, 5].includes(pulse)) voice(chord[[0, 2, 1][[0, 3, 5].indexOf(pulse)]] + 24, 1.25, 0.008, "glass");
      if (pulse === 4) voice(chord[0] - 12, 2, 0.019, "bass");
      if (melody != null) voice(melody, melodyDuration(), 0.022, secondHalf ? "horn" : "reed");
      if (bar % 4 === 3 && pulse === 7) drum(when, "tom", activeBus);
    } else if (score.kind === "jungle") {
      // Quick wooden pulses beneath a broad, warm canopy melody.
      if (pulse === 0) {
        for (const pitch of chord) voice(pitch + 12, 7.4, 0.005, "strings");
        voice(chord[0] - 12, 2.5, 0.024, "bass");
        texture(when, tick * 5.8, 0.0015, "leaves", activeBus);
      }
      if ([0, 2, 3, 5, 7].includes(pulse)) voice(chord[[0, 1, 2, 1, 0][[0, 2, 3, 5, 7].indexOf(pulse)]] + 24, 0.65, 0.011, "wood");
      if (pulse === 4) voice(chord[2] - 12, 2, 0.02, "bass");
      if (melody != null) voice(melody, melodyDuration(), 0.026, secondHalf ? "fiddle" : "flute");
      if (secondHalf && pulse === 6) voice(chord[1] + 36, 1.2, 0.008, "kalimba");
      if (pulse === 0 || pulse === 4) drum(when, "tom", activeBus);
    } else if (score.kind === "lakeside") {
      // Bright water gives way to the first dry, open stretches of desert.
      if (pulse === 0) {
        for (const pitch of chord) voice(pitch + 12, 8.7, 0.005, "strings");
        voice(chord[0] - 12, 4.1, 0.019, "bass");
      }
      if ([0, 2, 5, 7].includes(pulse)) voice(chord[[0, 2, 1, 2][[0, 2, 5, 7].indexOf(pulse)]] + 24, .9, 0.014, secondHalf ? "kalimba" : "harp");
      if (pulse === 4 || pulse === 8) voice(chord[pulse === 4 ? 1 : 2] + 36, 1.4, 0.008, "glass");
      if (melody != null) voice(melody, melodyDuration(), 0.026, secondHalf ? "reed" : "flute");
      if (secondHalf && pulse === 0) voice(chord[1] + 12, 3.3, 0.008, "wood");
    } else if (score.kind === "desert") {
      // Plucked caravan rhythm and a reedy, winding melody over open sand.
      if (pulse === 0) {
        voice(chord[0] - 12, 7.7, 0.018, "bass");
        voice(chord[0] + 12, 7.6, 0.006, "pad");
        texture(when, tick * 7.7, 0.0018, "wind", activeBus);
        if (bar % 4 === 0) drum(when, "tom", activeBus);
      }
      if ([0, 2, 3, 5, 7].includes(pulse)) voice(chord[[0, 2, 1, 2, 0][[0, 2, 3, 5, 7].indexOf(pulse)]] + 24, 0.65, 0.016, "harp");
      if (pulse === 4) voice(chord[2] - 12, 2.2, 0.016, "bass");
      if (melody != null) voice(melody, melodyDuration(), secondHalf ? 0.030 : 0.025, "reed");
      if (secondHalf && pulse === 0) voice(melody - 12, 2.2, 0.009, "flute");
      if (pulse === 6 && bar % 2 === 1) drum(when, "hat", activeBus);
    } else if (score.kind === "pyramid") {
      // Quiet, ancient stone: low choir, sparse metal and distant ritual drums.
      if (pulse === 0) {
        voice(chord[0] - 12, 6.7, 0.028, "lowStrings");
        voice(chord[1] + 12, 6.6, 0.007, "ritualChoir");
        texture(when, tick * 6.7, 0.0026, "wind", activeBus);
        if (bar % 4 === 0) drum(when, "gong", activeBus);
      }
      if (pulse === 3) voice(chord[2] - 12, 2.7, 0.015, "warBass");
      if (pulse === 1 || pulse === 4) voice(chord[pulse === 1 ? 0 : 2] + 24, 1.2, 0.008, "bell");
      if (melody != null) voice(melody, melodyDuration(), secondHalf ? 0.027 : 0.021, secondHalf ? "horn" : "flute");
      if (secondHalf && pulse === 6) drum(when, "tom", activeBus);
    } else if (score.kind === "crimson") {
      // A dry caravan pulse darkens into low choir and tolling metal past the red boundary.
      if (pulse === 0) {
        voice(chord[0] - 12, 9.5, secondHalf ? 0.031 : 0.021, secondHalf ? "lowStrings" : "bass");
        voice(chord[1] + 12, 9.3, secondHalf ? 0.010 : 0.005, secondHalf ? "ritualChoir" : "strings");
        texture(when, tick * 9.5, 0.0021, "wind", activeBus);
        if (bar % 4 === 0) drum(when, secondHalf ? "gong" : "tom", activeBus);
      }
      if ([0, 2, 4, 6, 8].includes(pulse)) voice(chord[[0, 2, 1, 2, 0][pulse / 2]] + 24, 0.78, secondHalf ? 0.010 : 0.015, secondHalf ? "glass" : "harp");
      if (pulse === 5) voice(chord[2] - 12, 2.4, secondHalf ? 0.027 : 0.016, secondHalf ? "warBass" : "bass");
      if (melody != null) voice(melody, melodyDuration(), secondHalf ? 0.031 : 0.026, secondHalf ? "horn" : "reed");
      if (secondHalf && pulse === 0) voice(melody - 12, 3.2, 0.010, "ritualChoir");
      if (secondHalf && pulse === 7) drum(when, "tom", activeBus);
    } else if (score.kind === "blood-coast") {
      // Slow tidal bass, glassy overtones and a solemn fortress choir.
      if (pulse === 0) {
        voice(chord[0] - 12, 8.5, 0.029, "lowStrings");
        voice(chord[1] + 12, 8.2, 0.010, "ritualChoir");
        texture(when, tick * 8.5, 0.0027, "wind", activeBus);
        if (bar % 4 === 0) drum(when, "gong", activeBus);
      }
      if (pulse === 3 || pulse === 6) voice(chord[pulse === 3 ? 2 : 0] - 12, 2.4, 0.021, "warBass");
      if (pulse === 1 || pulse === 4 || pulse === 7) voice(chord[[1, 0, 2][(pulse - 1) / 3]] + 24, 1.0, 0.009, "glass");
      if (melody != null) voice(melody, melodyDuration(), secondHalf ? 0.034 : 0.025, secondHalf ? "ritualChoir" : "reed");
      if (secondHalf && pulse === 0) voice(chord[2] + 12, 3.5, 0.009, "organ");
      if (secondHalf && pulse === 5) drum(when, "tom", activeBus);
    } else if (score.kind === "battlefield") {
      // A ghostly processional: distant drums and corroded brass never settle.
      if (pulse === 0) {
        voice(chord[0] - 12, 7.5, 0.033, "warBass");
        for (const pitch of chord) voice(pitch + 12, 7.2, 0.007, "lowStrings");
        texture(when, tick * 7.6, 0.0028, "wind", activeBus);
        if (bar % 4 === 0) drum(when, "gong", activeBus);
      }
      if (pulse === 0 || pulse === 4) drum(when, "tom", activeBus);
      if (pulse === 3 || pulse === 7) voice(chord[2] - 12, 0.8, 0.017, "warBass");
      if (melody != null) voice(melody, melodyDuration(), secondHalf ? 0.030 : 0.024, secondHalf ? "brass" : "horn");
      if (secondHalf && pulse === 6) voice(chord[1] + 24, 1.3, 0.008, "bell");
    } else if (score.kind === "dark-forest") {
      // Sparse wooden tones over a cold, shifting choir.
      if (pulse === 0) {
        voice(chord[0] - 12, 6.7, 0.022, "bass");
        voice(chord[1] + 12, 6.4, 0.008, "pad");
        texture(when, tick * 6.8, 0.0032, "wind", activeBus);
      }
      if (pulse === 2 || pulse === 5) voice(chord[pulse === 2 ? 2 : 0] + 12, 0.85, 0.012, "wood");
      if (pulse === 4) voice(chord[2] + 24, 1.4, 0.009, "kalimba");
      if (melody != null) voice(melody, melodyDuration(), secondHalf ? 0.029 : 0.025, secondHalf ? "reed" : "flute");
      if (secondHalf && pulse === 0 && bar % 4 === 0) drum(when, "gong", activeBus);
    } else if (score.kind === "skirmish") {
      if (pulse === 0) {
        for (const pitch of chord) voice(pitch + 12, 3.1, secondHalf ? 0.011 : 0.008, "brass");
        if (bar === 0 || bar === 8 || bar === 12) drum(when, "crash", activeBus);
      }
      voice(chord[[0, 2, 1, 2, 0, 2, 1, 2][pulse]] + 12, 0.58, 0.014, "warStrings");
      if (pulse % 2 === 0) voice(chord[pulse === 6 ? 2 : 0] - 12, 1.25, 0.057, "warBass");
      if (pulse === 0 || pulse === 4) for (const pitch of chord) voice(pitch + 24, 0.76, 0.008, "guitar");
      if (melody != null) {
        voice(melody, melodyDuration(), 0.030, secondHalf ? "guitar" : "brass");
        if (bar >= 12 && pulse === 0) voice(melody - 12, 1.7, 0.017, "brass");
      }
      if (pulse === 0 || pulse === 4) drum(when, "kick", activeBus);
      if (pulse === 2 || pulse === 6) drum(when, "snare", activeBus);
      if ((bar === 7 || bar === 15) && pulse >= 5) drum(when, "tom", activeBus);
      if (pulse % (secondHalf ? 1 : 2) === 0) drum(when, "hat", activeBus);
    } else if (score.kind === "forest-matriarch") {
      // A whispered woodland hymn grows into a choir-led assault: Juila's
      // entourage answers the flute motive before the whole canopy awakens.
      const awakened = bar >= 12, climax = bar >= 18;
      if (pulse === 0) {
        for (const pitch of chord) voice(pitch + 12, 11.5, awakened ? 0.010 : 0.005, "ritualChoir");
        voice(chord[0], 11.5, awakened ? 0.019 : 0.012, "lowStrings");
        texture(when, tick * 10.8, awakened ? 0.003 : 0.002, "leaves", activeBus);
        if (bar === 0 || bar === 12 || bar === 20) drum(when, "gong", activeBus);
      }
      if ([0, 3, 6, 9].includes(pulse)) voice(chord[[0, 2, 1, 2][[0, 3, 6, 9].indexOf(pulse)]] + 24, 1.2, awakened ? 0.017 : 0.014, "harp");
      if ([2, 5, 8, 11].includes(pulse)) voice(chord[[1, 2, 1, 0][[2, 5, 8, 11].indexOf(pulse)]] + 36, .7, 0.008, "kalimba");
      if (pulse === 0 || pulse === 6) {
        voice(chord[0] - 12, awakened ? 5.2 : 4.0, awakened ? 0.072 : 0.035, "warBass");
        if (awakened) drum(when, climax ? "impact" : "war", activeBus);
        else drum(when, "tom", activeBus);
      }
      if (awakened && [0, 2, 4, 6, 8, 10].includes(pulse)) voice(chord[pulse === 4 || pulse === 10 ? 2 : 0] + 12, .65, climax ? 0.018 : 0.012, "violins");
      if (melody != null) {
        voice(melody, melodyDuration(), awakened ? 0.034 : 0.024, climax ? "hornSection" : awakened ? "violins" : "flute");
        if (awakened && (pulse === 0 || pulse === 6)) voice(melody - 12, 3.2, 0.012, "ritualChoir");
      }
      if (awakened && (pulse === 3 || pulse === 9)) drum(when, "snare", activeBus);
      if (bar % 4 === 3 && pulse === 11) drum(when, awakened ? "crash" : "gong", activeBus);
    } else if (score.kind === "wounded-dragon") {
      // The broken-wing motive falls by a semitone; halfway through, brass,
      // choir and a faster string ostinato rise with Nygheir's second wind.
      const awakened = bar >= 12, climax = bar >= 18;
      if (pulse === 0) {
        voice(chord[0] - 12, 8.7, awakened ? 0.04 : 0.03, "lowStrings");
        for (const pitch of chord) voice(pitch + 12, 8.6, awakened ? 0.012 : 0.006, "ritualChoir");
        if (bar === 0 || bar === 12 || bar === 18) drum(when, "gong", activeBus);
        else drum(when, "war", activeBus);
        if (bar % 4 === 0) texture(when, tick * 8.6, 0.0027, "wind", activeBus);
      }
      if ([0, 4, 6].includes(pulse)) voice(chord[pulse === 6 ? 2 : 0] - 12, 2.1, awakened ? 0.078 : 0.055, "warBass");
      if ([0, 2, 4, 6, 7].includes(pulse)) voice(chord[[0, 1, 2, 0, 2][[0, 2, 4, 6, 7].indexOf(pulse)]] + 12, awakened ? 0.9 : 0.6, awakened ? 0.019 : 0.012, "warStrings");
      if (melody != null) {
        voice(melody, melodyDuration(), awakened ? 0.041 : 0.032, climax ? "hornSection" : awakened ? "violins" : "horn");
        if (awakened && (pulse === 0 || pulse === 4)) voice(melody - 12, 2.7, 0.02, "hornSection");
      }
      if (awakened && (pulse === 0 || pulse === 6)) voice(chord[2] + 24, 2.3, climax ? 0.014 : 0.009, "ritualChoir");
      if (pulse === 0 || pulse === 6) drum(when, awakened ? "impact" : "war", activeBus);
      if (pulse === 3 || pulse === 8) drum(when, awakened ? "snare" : "tom", activeBus);
      if (awakened && pulse % 2 === 0) drum(when, "hat", activeBus);
    } else if (score.kind === "star-dragon") {
      // A descending white-star figure becomes an ascending gold chorale, then
      // a fully voiced final ascent. Wide bells distinguish it from the earth dragons.
      const form = Math.floor(bar / 8);
      if (pulse === 0) {
        voice(chord[0] - 12, 7.7, form === 2 ? 0.038 : 0.027, "lowStrings");
        for (const pitch of chord) voice(pitch + 12, 7.7, form >= 2 ? 0.016 : 0.008, "ritualChoir");
        drum(when, bar % 8 === 0 ? "gong" : form === 0 ? "war" : "impact", activeBus);
      }
      if ([0, 3, 5].includes(pulse)) voice(chord[pulse === 5 ? 2 : 0] - 12, 1.8, form >= 2 ? 0.086 : 0.062, "warBass");
      if ([1, 2, 4, 6, 7].includes(pulse)) voice(chord[[1, 2, 4, 6, 7].indexOf(pulse) % 3] + 12, 0.62, form >= 1 ? 0.018 : 0.011, "warStrings");
      if (pulse === 0 || pulse === 4) voice(chord[2] + 36, form >= 2 ? 2.1 : 1.4, form >= 2 ? 0.016 : 0.009, "glass");
      if (melody != null) {
        voice(melody, melodyDuration(), form >= 2 ? 0.041 : 0.032, form === 0 ? "hornSection" : form === 1 ? "violins" : "brass");
        if (form >= 1 && pulse === 0) voice(melody - 12, 3.4, form >= 2 ? 0.025 : 0.015, "ritualChoir");
      }
      if (form >= 1 && pulse === 4) drum(when, "snare", activeBus);
      if (form >= 2 && pulse % 2 === 0) drum(when, "hat", activeBus);
      if (bar % 8 === 7 && pulse === 7) drum(when, "impact", activeBus);
    } else if (score.kind === "juggernaut") {
      // Zik Lun: plated footsteps and anvil blows become a siege-march, then
      // the brass and low choir open up as the mountain-sized troll advances.
      const phase = Math.floor(bar / 8);
      const transition = bar % 8 === 7;
      if (pulse === 0) {
        for (const pitch of chord) voice(pitch + 24, 7.5, phase >= 2 ? 0.010 : 0.006, "ritualChoir");
        voice(chord[0] + 12, 7.6, 0.016, "lowStrings");
        if (bar === 0 || bar === 8 || bar === 16 || bar === 24) drum(when, "impact", activeBus);
        else drum(when, "war", activeBus);
        if (bar === 0 || bar === 16) texture(when, tick * 7, 0.003, "wind", activeBus);
      }
      if (pulse === 0 || pulse === 4) voice(chord[0] - 12, 3.1, 0.078, "warBass");
      if (pulse === 4) {
        drum(when, "war", activeBus);
        if (phase >= 1) for (const pitch of chord) voice(pitch + 12, 3.4, phase >= 2 ? 0.019 : 0.013, "hornSection");
      }
      if (pulse === 2 || (phase >= 1 && pulse === 6)) drum(when, "anvil", activeBus);
      if (pulse === 3 || pulse === 7) drum(when, "tom", activeBus);
      if ([0, 3, 4, 7].includes(pulse)) voice(chord[pulse === 3 || pulse === 7 ? 2 : 0] + 12, phase >= 2 ? 1.25 : 0.72, phase >= 2 ? 0.018 : 0.012, "warStrings");
      if (phase >= 2 && (pulse === 1 || pulse === 5)) voice(chord[pulse === 1 ? 1 : 2] + 24, 1.6, 0.012, "violins");
      if (melody != null) {
        voice(melody, melodyDuration(), phase >= 2 ? 0.043 : 0.037, phase >= 2 ? "hornSection" : "trombone");
        if (phase >= 1 && pulse === 0) voice(melody - 12, 3.3, 0.019, "trombone");
        if (phase === 3 && pulse === 0) voice(melody, 3.5, 0.013, "ritualChoir");
      }
      if (transition && pulse === 6) drum(when, "snare", activeBus);
      if (transition && pulse === 7) drum(when, "impact", activeBus);
    } else if (score.kind === "storm-talos") {
      // An eight-bar ritual/assault/lament/apotheosis form. Syncopated 3+3+2
      // strings drive the sword; pipe organ and choir frame the blood altar;
      // high bells and thunder impacts announce Talos and Gorthok.
      const form = Math.floor(bar / 8), cadence = bar % 8 === 7;
      if (pulse === 0) {
        for (const pitch of chord) voice(pitch + 12, 7.5, form === 2 ? .010 : .014, "organ");
        for (const pitch of chord) voice(pitch + 24, 7.7, form === 3 ? .017 : .007, "ritualChoir");
        voice(chord[0] - 12, 7.5, .031, "lowStrings");
        drum(when, bar % 8 === 0 ? "impact" : "war", activeBus);
        if (bar % 8 === 0) texture(when, tick * 7.8, .004, "wind", activeBus);
      }
      if ([0, 3, 6].includes(pulse)) {
        voice(chord[pulse === 3 ? 2 : 0] + 12, 1.8, form === 2 ? .013 : .027, "warStrings");
        voice(chord[0] - 12, 1.8, .051, "warBass");
        if (form !== 2) drum(when, "war", activeBus);
      }
      if (form !== 2 && [2, 5, 7].includes(pulse)) drum(when, pulse === 7 ? "snare" : "tom", activeBus);
      if ([1, 4].includes(pulse)) voice(chord[pulse === 1 ? 1 : 2] + 24, 1.3, form === 3 ? .020 : .010, "violins");
      if (pulse === 0 || pulse === 6 && form >= 1) voice(chord[2] + 36, 3.7, .009, "bell");
      if (form >= 1 && form !== 2 && pulse === 3) for (const pitch of chord) voice(pitch + 12, 3.5, .017, "trombone");
      if (melody != null) {
        voice(melody, melodyDuration(), form === 2 ? .024 : .039, form === 2 ? "reed" : "hornSection");
        if (form === 3) voice(melody - 12, melodyDuration(), .010, "ritualChoir");
      }
      if (cadence && pulse === 7) drum(when, "impact", activeBus);
    } else if (score.kind === "false-dreamer") {
      // A chromatic ritual: a glass eye motif fractures into an organ/choir
      // climax, deliberately unlike the forge's straight, heroic march.
      const form = Math.floor(bar / 8);
      if (pulse === 0) {
        voice(chord[0] - 12, 5.8, .030, "lowStrings");
        for (const pitch of chord) voice(pitch + 24, 5.7, form >= 2 ? .016 : .009, "ritualChoir");
        voice(chord[0] + 12, 5.8, form >= 1 ? .022 : .011, "organ");
        if (bar % 8 === 0) drum(when, "gong", activeBus);
      }
      if (pulse === 0 || pulse === 3) {
        voice(chord[pulse === 3 ? 2 : 0] - 12, 2.5, form >= 2 ? .075 : .045, "warBass");
        if (form >= 1) drum(when, pulse === 0 ? "war" : "tom", activeBus);
      }
      if (pulse === 2 || pulse === 5) voice(chord[pulse === 2 ? 1 : 2] + 36, 2.6, .013, "glass");
      if (melody != null) {
        voice(melody, melodyDuration(), form >= 2 ? .035 : .026, form === 0 ? "reed" : "violins");
        if (form >= 2 && pulse === 0) voice(melody - 12, 4.7, .020, "ritualChoir");
      }
      if (form >= 2) voice(chord[[0, 1, 2, 1, 2, 0][pulse]] + 12, .75, .018, "warStrings");
      if (form === 3 && pulse === 3) drum(when, "impact", activeBus);
      if (bar % 8 === 7 && pulse === 5) { drum(when, "gong", activeBus); texture(when, tick * 2, .003, "wind", activeBus); }
    } else if (score.kind === "forge-god") {
      // Brass, rising string countermelodies and syncopated hammer blows open
      // into a blazing, full-register forge chorale in the final section.
      const form = Math.floor(bar / 8);
      if (pulse === 0) {
        for (const pitch of chord) voice(pitch + 12, 7.7, form >= 2 ? .016 : .008, "hornSection");
        voice(chord[0] - 12, 7.7, .024, "lowStrings");
        drum(when, bar % 8 === 0 ? "impact" : "war", activeBus);
        if (form >= 2) for (const pitch of chord) voice(pitch + 24, 7.6, .012, "ritualChoir");
      }
      if ([0, 3, 6].includes(pulse)) { drum(when, "anvil", activeBus); voice(chord[pulse === 3 ? 2 : 0] - 12, 1.8, .064, "warBass"); }
      if (pulse === 4) drum(when, "snare", activeBus);
      if (melody != null) {
        voice(melody, melodyDuration(), form >= 2 ? .040 : .034, form === 0 ? "trombone" : "hornSection");
        if (form === 3 && pulse === 0) voice(melody + 12, 3, .012, "violins");
      }
      if ([1, 2, 4, 5, 7].includes(pulse)) voice(chord[[1, 2, 0, 1, 2][[1, 2, 4, 5, 7].indexOf(pulse)]] + 24, .7, form >= 1 ? .015 : .008, "warStrings");
      if (form >= 2 && pulse === 6) voice(chord[2] + 36, 1.4, .011, "bell");
      if (bar % 8 === 7 && pulse === 7) drum(when, "impact", activeBus);
    } else if (score.kind === "boss") {
      // Ji'ruan: crooked 7/8 ritual and glass "imp" figures. The retinue
      // swells from furtive summons into a high choral, string-led climax.
      const phase = Math.floor(bar / 8);
      const transition = bar % 8 === 7;
      if (pulse === 0) {
        voice(chord[0], 6.8, 0.019, "organ");
        for (const pitch of chord) voice(pitch + 12, 6.7, phase >= 2 ? 0.012 : 0.007, "ritualChoir");
        if (bar === 0 || bar === 8 || bar === 16 || bar === 24) drum(when, "gong", activeBus);
      }
      voice(chord[[0, 0, 1, 0, 2, 1, 0][pulse]] + 12, phase >= 2 ? 0.84 : 0.62, phase >= 2 ? 0.020 : 0.014, phase >= 2 ? "violins" : "dark");
      if ([0, 3, 5].includes(pulse)) {
        voice((pulse === 5 ? chord[2] : chord[0]) - 12, 1.7, phase >= 2 ? 0.069 : 0.052, "warBass");
        drum(when, pulse === 3 ? "tom" : "war", activeBus);
      }
      if (melody != null) {
        voice(melody, melodyDuration(), phase >= 2 ? 0.035 : 0.027, phase === 0 ? "reed" : "violins");
        if (phase >= 1 && (pulse === 0 || pulse === 3)) voice(melody - 12, 2.3, phase >= 2 ? 0.022 : 0.015, "hornSection");
        if (phase === 3 && pulse === 0) voice(melody - 24, 3.5, 0.015, "ritualChoir");
      }
      if (pulse === 2 || pulse === 6) voice(chord[pulse === 2 ? 1 : 2] + 36, phase >= 1 ? 1.8 : 1.2, 0.015, "glass");
      if (phase >= 2 && (pulse === 1 || pulse === 4)) voice(chord[pulse === 1 ? 2 : 1] + 24, 1.25, 0.012, "violins");
      if (phase >= 1 && pulse === 4) drum(when, "snare", activeBus);
      if (transition && pulse >= 5) drum(when, "tom", activeBus);
      if (transition && pulse === 6) drum(when, "gong", activeBus);
      if (pulse % (phase >= 2 || loop % 2 ? 1 : 2) === 0) drum(when, "hat", activeBus);
    }
  }

  function schedule() {
    if (!context || !activeBus || context.state !== "running") return;
    if (nextNoteTime < context.currentTime - 0.5) nextNoteTime = context.currentTime + 0.04;
    while (nextNoteTime < context.currentTime + 0.2) {
      if (activePerformance) {
        activePerformance.step(step, nextNoteTime);
        nextNoteTime += activePerformance.stepSeconds;
      } else {
        playStep(nextNoteTime);
        nextNoteTime += 30 / scores[activeTheme].tempo;
      }
      step++;
    }
  }

  // Render the same score and instruments as live playback into an AudioBuffer.
  // Used by tests/music-export-browser.html to produce standalone audio files.
  async function renderOffline(themeId, sampleRate = 44100, options = {}) {
    if (context) throw new Error("Offline export requires a fresh audio context");
    const selected = scores[themeId];
    const OfflineContextClass = window.OfflineAudioContext || window.webkitOfflineAudioContext;
    if (!selected || !OfflineContextClass) throw new Error(`Cannot render soundtrack theme: ${themeId}`);
    if (selected.variant) {
      // One full pass of the form, from the intro to the loop point, plus the room's tail.
      const offline = new OfflineContextClass(2, Math.ceil((selected.totalSteps * selected.stepSeconds + 4.5) * sampleRate), sampleRate);
      try {
        offlineRendering = true;
        setupAudio(offline);
        const performance = window.MusicComposer.perform(context, selected, master, options);
        for (let index = 0; index < selected.totalSteps; index++) performance.step(index, 0.05 + index * selected.stepSeconds);
        return await offline.startRendering();
      } finally {
        context = master = delay = hall = activeBus = noise = null;
        activeTheme = null;
        nextNoteTime = step = 0;
        audioReady = false;
        offlineRendering = false;
      }
    }
    const ticks = selected.meter * selected.route.length;
    const tickLength = 30 / selected.tempo;
    const offline = new OfflineContextClass(2, Math.ceil((ticks * tickLength + 1.6) * sampleRate), sampleRate);
    try {
      offlineRendering = true;
      setupAudio(offline);
      activeBus = context.createGain();
      activeBus.gain.value = 1;
      activeBus.connect(master);
      activeBus.connect(delay);
      activeBus.connect(hall);
      activeTheme = themeId;
      for (step = 0; step < ticks; step++) playStep(0.05 + step * tickLength);
      return await offline.startRendering();
    } finally {
      context = master = delay = hall = activeBus = noise = null;
      activeTheme = null;
      nextNoteTime = step = 0;
      audioReady = false;
      offlineRendering = false;
    }
  }

  function retire(bus) {
    if (!bus || !context) return;
    const now = context.currentTime;
    if (typeof bus.gain.cancelAndHoldAtTime === "function") bus.gain.cancelAndHoldAtTime(now);
    else {
      const currentGain = bus.gain.value;
      bus.gain.cancelScheduledValues(now);
      bus.gain.setValueAtTime(currentGain, now);
    }
    bus.gain.linearRampToValueAtTime(0, now + 0.55);
    setTimeout(() => bus.disconnect(), 1800);
  }

  function stop() {
    clearInterval(clock);
    clock = null;
    retire(activeBus);
    activeBus = null;
    activeTheme = null;
    activePerformance = null;
  }

  function sync() {
    if (offlineRendering) return;
    paint();
    if (!enabled || document.hidden || !context || context.state !== "running") {
      if (activeTheme) stop();
      return;
    }
    const desired = desiredTheme();
    if (desired === activeTheme) return;
    retire(activeBus);
    if (scores[desired].variant) {
      // Remastered themes bring their own instruments and room.
      activePerformance = window.MusicComposer.perform(context, scores[desired], master);
      activeBus = activePerformance.bus;
      activeBus.gain.setValueAtTime(0, context.currentTime);
      activeBus.gain.linearRampToValueAtTime(1, context.currentTime + 0.8);
    } else {
      activePerformance = null;
      activeBus = context.createGain();
      activeBus.gain.setValueAtTime(0, context.currentTime);
      activeBus.gain.linearRampToValueAtTime(1, context.currentTime + 0.55);
      activeBus.connect(master);
      activeBus.connect(delay);
      activeBus.connect(hall);
    }
    activeTheme = desired;
    step = 0;
    nextNoteTime = context.currentTime + 0.06;
    if (!clock) clock = setInterval(schedule, 45);
    schedule();
  }

  function unlock() {
    if (!enabled || !AudioContextClass || document.hidden) return;
    try {
      setupAudio();
      const resumed = context.resume();
      if (resumed?.then) resumed.then(sync).catch(() => {});
      else sync();
    } catch { /* Audio devices can be unavailable; the rest of the sheet stays usable. */ }
  }

  function setVolume(value) {
    if (!Number.isFinite(value)) return;
    volume = Math.max(0, Math.min(1, value));
    enabled = volume > 0;
    try { localStorage.setItem(STORAGE_KEY, String(volume)); } catch { /* Optional preference. */ }
    if (master && !offlineRendering) {
      const now = context.currentTime;
      master.gain.cancelScheduledValues(now);
      master.gain.setValueAtTime(master.gain.value, now);
      master.gain.linearRampToValueAtTime(0.58 * volume, now + 0.04);
    }
    paint();
    if (enabled) unlock(); else stop();
  }

  function closeSettings() {
    if (!settings || settings.hidden) return;
    settings.hidden = true;
    button?.setAttribute("aria-expanded", "false");
    if (typeof updateModalState === "function") updateModalState();
    else document.body.classList.toggle("modal-open", Boolean(document.querySelector(".modal-layer:not([hidden])")));
    button?.focus();
  }
  button?.addEventListener("click", () => {
    if (!settings) return;
    settings.hidden = false;
    button.setAttribute("aria-expanded", "true");
    if (typeof updateModalState === "function") updateModalState();
    else document.body.classList.add("modal-open");
    paint();
    (volumeSlider?.disabled ? settings.querySelector(".modal-close") : volumeSlider)?.focus();
    unlock();
  });
  settings?.querySelectorAll("[data-settings-close]").forEach(control => control.addEventListener("click", closeSettings));
  volumeSlider?.addEventListener("input", () => setVolume(Number(volumeSlider.value) / 100));
  // Settings can be layered above the map, inventory or combat pickers. Handle
  // its keys first so Escape doesn't close an underlying window as well.
  document.addEventListener("keydown", event => {
    if (!settings || settings.hidden) return;
    if (event.key === "Escape") {
      event.preventDefault(); event.stopImmediatePropagation(); closeSettings();
    } else if (event.key === "Tab") {
      const controls = [...settings.querySelectorAll("button:not(:disabled), input:not(:disabled), select:not(:disabled)")].filter(control => !control.classList.contains("modal-backdrop"));
      const first = controls[0], last = controls.at(-1);
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
    }
  }, true);
  document.addEventListener("pointerdown", unlock);
  document.addEventListener("keydown", event => {
    if (!event.repeat) unlock();
  });
  document.addEventListener("visibilitychange", () => {
    if (document.hidden) stop(); else if (context?.state === "running") sync();
  });
  // Covers region changes from saves, scripts, and future travel paths as well as the explicit hooks.
  setInterval(sync, 500);
  paint();
  window.Music = {
    sync,
    setVolume,
    renderOffline,
    // "theme" names the music family (a region, "combat" or a boss); "variation" is the region arrangement of a battle.
    state: () => {
      const [theme, variation = null] = desiredTheme().split("@");
      return { enabled, volume, theme, variation, track: desiredTheme(), playing: Boolean(activeTheme), audioReady };
    },
    scoreInfo: () => Object.fromEntries(Object.entries(scores).map(([id, entry]) => [id, entry.variant ? {
      title: entry.title, kind: "remaster", tempo: entry.spec.tempo, meter: entry.spec.meter.join("x"),
      bars: entry.totalSteps / entry.steps, seconds: Math.round(entry.totalSteps * entry.stepSeconds), sections: entry.spec.form.join(" ")
    } : {
      title: entry.title, kind: entry.kind, tempo: entry.tempo, meter: entry.meter,
      bars: entry.route.length, melody: entry.lead.map(bar => bar.join(",")).join("|")
    }])),
    lint: () => Object.values(scores).filter(entry => entry.variant).flatMap(entry => window.MusicComposer.lint(entry))
  };
})();

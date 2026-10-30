/* Sprite catalogue: the eight looks of each playable race, a custom sprite for
   every enemy, and the public Sprites API used by the battle stage, the race
   picker and the classic combat cards. */
(() => {
  "use strict";
  const K = window.SpriteKit;
  const { lighten, darken } = K;

  /* ---------------- Playable races ---------------- */
  const tunic = (color, pants = "#5d5446", extra = {}) => ({ top: "tunic", color, pants, ...extra });
  const dress = (color, extra = {}) => ({ top: "dress", color, pants: "#5d5446", ...extra });
  const RACES = {
    human: {
      base: {},
      male: [
        { label: "Wayfarer", skin: "#e9b896", hair: { style: "crop", color: "#5a3a24" }, beard: { style: "stubble", color: "#5a3a24" }, eyes: { color: "#4a3020" }, outfit: tunic("#5f7f4f") },
        { label: "Sellsword", skin: "#c98e64", hair: { style: "spiky", color: "#2b2420" }, eyes: { color: "#2b1d14", fierce: true }, outfit: tunic("#4f6b8f", "#4a4036") },
        { label: "Smith", skin: "#8a5a3c", hair: { style: "curly", color: "#1f1a17" }, beard: { style: "full", color: "#1f1a17" }, eyes: { color: "#2b1d14" }, outfit: tunic("#8f4f3a", "#4f463a", { pattern: "stripes" }) },
        { label: "Courtier", skin: "#f1c9a8", hair: { style: "swept", color: "#d8b46a" }, eyes: { color: "#3f6fa8" }, outfit: tunic("#7a4f6b", "#3f3a46") },
      ],
      female: [
        { label: "Ranger", skin: "#f0c3a2", hair: { style: "long", color: "#8a3f24" }, eyes: { color: "#3f7a4a" }, lashes: true, outfit: dress("#3f7f7a") },
        { label: "Duelist", skin: "#c98e64", hair: { style: "highponytail", color: "#231c1a", tie: "#c43a4a" }, eyes: { color: "#2b1d14", fierce: true }, lashes: true, outfit: tunic("#a8643a", "#4a4036") },
        { label: "Healer", skin: "#7a4f35", hair: { style: "curls", color: "#2b1d17" }, eyes: { color: "#2b1d14" }, lashes: true, outfit: dress("#c9a24a") },
        { label: "Scholar", skin: "#f6d6be", hair: { style: "braid", color: "#e0c27a" }, eyes: { color: "#3f6fa8" }, lashes: true, freckles: true, outfit: tunic("#6b5a8f", "#4a4036") },
      ],
    },
    elf: {
      base: { ears: "long", eyes: { style: "narrow" }, build: { legs: 26, torso: 27, shoulders: 10.5, hips: 9.5 } },
      male: [
        { label: "Sentinel", skin: "#f3d9c2", hair: { style: "long", color: "#dfe3e8" }, eyes: { style: "narrow", color: "#4f8f7a" }, outfit: tunic("#4f7f5a", "#4f5a46") },
        { label: "Archer", skin: "#ecd0b4", hair: { style: "ponytail", color: "#e8d08a", tie: "#4f7f5a" }, eyes: { style: "narrow", color: "#3f7a9f" }, outfit: tunic("#6b8f6a", "#46503c") },
        { label: "Druid", skin: "#e8c6a4", hair: { style: "swept", color: "#a5532f" }, eyes: { style: "narrow", color: "#5f8f3f" }, outfit: tunic("#3f6f6f", "#3f463c") },
        { label: "Bladedancer", skin: "#f5dfcc", hair: { style: "crop", color: "#2f2a3a" }, eyes: { style: "narrow", color: "#8f6fbf" }, outfit: tunic("#7a8f4a", "#463f36") },
      ],
      female: [
        { label: "Oracle", skin: "#f6e0cc", hair: { style: "long", color: "#f2efe6" }, eyes: { style: "narrow", color: "#5f9fbf" }, lashes: true, headwear: { style: "circlet", color: "#e0b54a", gem: "#7fd0c0" }, outfit: dress("#e8e2cf", { trim: "#c9a24a", color2: "#c9c0a8" }) },
        { label: "Huntress", skin: "#efd2b6", hair: { style: "wavy", color: "#e3be62" }, eyes: { style: "narrow", color: "#4f8f4f" }, lashes: true, outfit: tunic("#5f7f3f", "#4a4436") },
        { label: "Wildheart", skin: "#e6c3a0", hair: { style: "braid", color: "#b8602f", tie: "#4f7f5a" }, eyes: { style: "narrow", color: "#7a8f3f" }, lashes: true, headwear: { style: "flower", color: "#f0a8c0" }, outfit: dress("#6f8f5a") },
        { label: "Moonblade", skin: "#f4dccb", hair: { style: "highponytail", color: "#2f4f4f", tie: "#9fb6d4" }, eyes: { style: "narrow", color: "#6f7fbf" }, lashes: true, outfit: tunic("#3f4f6f", "#3a3a46") },
      ],
    },
    "dark-elf": {
      base: { ears: "long", eyes: { style: "narrow" }, build: { legs: 26, torso: 27, shoulders: 10.5, hips: 9.5 } },
      male: [
        { label: "Shade", skin: "#8f86a8", hair: { style: "long", color: "#eceaf2" }, eyes: { style: "narrow", color: "#c93a3a" }, outfit: tunic("#3f2f4f", "#2b2633") },
        { label: "Blademaster", skin: "#7a7096", hair: { style: "ponytail", color: "#c9c2dc", tie: "#7a2f3a" }, eyes: { style: "narrow", color: "#d0603a", fierce: true }, outfit: tunic("#2f2a3a", "#26222d") },
        { label: "Warlock", skin: "#9a8fb5", hair: { style: "spiky", color: "#f2f0f6" }, eyes: { style: "narrow", color: "#b04aff" }, outfit: tunic("#4f2f5f", "#2b2633") },
        { label: "Stalker", skin: "#6e6a8a", hair: { style: "crop", color: "#1f1c26" }, eyes: { style: "narrow", color: "#e8c04a" }, scar: true, outfit: tunic("#3a3f46", "#26282d") },
      ],
      female: [
        { label: "Matron", skin: "#968cb0", hair: { style: "long", color: "#f2f0f6" }, eyes: { style: "narrow", color: "#c93a3a" }, lashes: true, headwear: { style: "circlet", color: "#c9c2dc", gem: "#c93a3a" }, outfit: dress("#3f2f4f") },
        { label: "Spider Priestess", skin: "#7e7499", hair: { style: "wavy", color: "#d9cfe8" }, eyes: { style: "narrow", color: "#b04aff" }, lashes: true, outfit: dress("#2f2a3a", { color2: "#4f2f5f" }) },
        { label: "Night Archer", skin: "#a59bbf", hair: { style: "highponytail", color: "#1f1c26", tie: "#b04aff" }, eyes: { style: "narrow", color: "#e8c04a" }, lashes: true, outfit: tunic("#3a3f46", "#26282d") },
        { label: "Duskweaver", skin: "#857a9f", hair: { style: "bob", color: "#b8a8d8" }, eyes: { style: "narrow", color: "#7fd0ff" }, lashes: true, outfit: tunic("#4f2f5f", "#2b2633") },
      ],
    },
    dwarf: {
      base: { build: { legs: 17, torso: 25, head: 21, shoulders: 13.5, hips: 11.5, arm: 19, armW: 9.2, legW: 10 }, nose: "big" },
      male: [
        { label: "Forgemaster", skin: "#e2a682", hair: { style: "bald" }, beard: { style: "braided", color: "#a5532f", tie: "#c9a24a" }, eyes: { color: "#3a2a20" }, browColor: "#7a3a1f", outfit: tunic("#6b4a2f", "#4a3b2f") },
        { label: "Miner", skin: "#d49a74", hair: { style: "crop", color: "#6b4a2f" }, beard: { style: "forked", color: "#6b4a2f" }, eyes: { color: "#2b1d14" }, outfit: tunic("#5f6f7a", "#4a3b2f") },
        { label: "Elder", skin: "#e8b896", hair: { style: "long", color: "#b8b4ae" }, beard: { style: "long", color: "#c9c5bf" }, eyes: { color: "#3f6fa8" }, browColor: "#9a958e", outfit: tunic("#4f6b4f", "#46403a") },
        { label: "Brewer", skin: "#b07a56", hair: { style: "topknot", color: "#231c1a" }, beard: { style: "full", color: "#231c1a" }, eyes: { color: "#2b1d14" }, outfit: tunic("#8f3f3a", "#4a3b2f") },
      ],
      female: [
        { label: "Runesmith", skin: "#e8b08c", hair: { style: "twinbraids", color: "#b8602f", tie: "#c9a24a" }, eyes: { color: "#4f6b3a" }, lashes: true, freckles: true, outfit: tunic("#6b4a2f", "#4a3b2f") },
        { label: "Shieldmaiden", skin: "#d8a07a", hair: { style: "braid", color: "#e0c27a", tie: "#8a3b3b" }, eyes: { color: "#3f6fa8" }, lashes: true, outfit: tunic("#4f6b8f", "#46403a") },
        { label: "Alewife", skin: "#c48a64", hair: { style: "bun", color: "#4a2f1f" }, eyes: { color: "#2b1d14" }, lashes: true, blush: "#d86a6a", outfit: dress("#8f4f3a") },
        { label: "Prospector", skin: "#ecc0a0", hair: { style: "curls", color: "#8a3f24" }, eyes: { color: "#5a3a24" }, lashes: true, outfit: tunic("#7a7a4f", "#4a3b2f") },
      ],
    },
    hobbit: {
      base: { build: { legs: 15, torso: 21, head: 19.5, shoulders: 10, hips: 9.5, arm: 17, armW: 7.2, legW: 8 }, feet: "big-bare", footHair: "#6b4a2f", blush: "#e07a6a", nose: "small" },
      male: [
        { label: "Gardener", skin: "#f0c3a2", hair: { style: "curly", color: "#7a4f2a" }, footHair: "#7a4f2a", eyes: { color: "#3f6fa8" }, outfit: { top: "vest", color: "#5f7f3f", pants: "#8a6a4a" } },
        { label: "Wanderer", skin: "#e2b08a", hair: { style: "curly", color: "#c9a24a" }, footHair: "#c9a24a", eyes: { color: "#4a3020" }, outfit: { top: "vest", color: "#a8643a", pants: "#5f6a4a" } },
        { label: "Cook", skin: "#c99470", hair: { style: "curly", color: "#2f2420" }, footHair: "#2f2420", eyes: { color: "#2b1d14" }, outfit: { top: "vest", color: "#7a4f6b", pants: "#6b5a46" } },
        { label: "Burglar", skin: "#f4d0b4", hair: { style: "messy", color: "#8a3f24" }, footHair: "#8a3f24", eyes: { color: "#4f7f3f" }, outfit: { top: "vest", color: "#3f5f6b", pants: "#6b5a46" } },
      ],
      female: [
        { label: "Baker", skin: "#f6d0b6", hair: { style: "curls", color: "#b8602f" }, footHair: "#b8602f", eyes: { color: "#4f7f3f" }, lashes: true, outfit: dress("#c9a24a") },
        { label: "Tinker", skin: "#e6b896", hair: { style: "bun", color: "#7a4f2a" }, footHair: "#7a4f2a", eyes: { color: "#3f6fa8" }, lashes: true, outfit: { top: "vest", color: "#5f6f7a", pants: "#8a6a4a" } },
        { label: "Herbalist", skin: "#c48c68", hair: { style: "curly", color: "#2f2420" }, footHair: "#2f2420", eyes: { color: "#2b1d14" }, lashes: true, headwear: { style: "flower", color: "#f6d06b" }, outfit: dress("#6f8f5a") },
        { label: "Storyteller", skin: "#f8dcc6", hair: { style: "braid", color: "#e0c27a" }, footHair: "#e0c27a", eyes: { color: "#5f4f8f" }, lashes: true, outfit: dress("#8f4f6b") },
      ],
    },
    lizardman: {
      base: { head: "lizard", ears: "none", hair: null, eyes: { style: "slit", color: "#e8c04a" }, tail: { style: "lizard" }, feet: "claws", hands: "claws", nose: "none", brows: false },
      male: [
        { label: "Marshguard", skin: "#5f8a4a", tail: { style: "lizard", color: "#5f8a4a" }, crest: { style: "spines", color: "#c9533a" }, outfit: { top: "loincloth", color: "#8a6a4a", color2: "#5a3d26", pants: null } },
        { label: "Tidecaller", skin: "#3f7f7a", tail: { style: "lizard", color: "#3f7f7a" }, crest: { style: "fin", color: "#e8c04a" }, outfit: { top: "loincloth", color: "#4f6b8f", color2: "#3a3f46", pants: null } },
        { label: "Dunerunner", skin: "#c9a46a", tail: { style: "lizard", color: "#c9a46a" }, crest: { style: "frill", color: "#a8463a" }, eyes: { style: "slit", color: "#ff9a3a" }, outfit: { top: "loincloth", color: "#7a5a3a", color2: "#5a3d26", pants: null } },
        { label: "Emberscale", skin: "#a8463a", tail: { style: "lizard", color: "#a8463a" }, crest: { style: "spines", color: "#2f2420" }, eyes: { style: "slit", color: "#ffd23a" }, outfit: { top: "loincloth", color: "#3a3f46", color2: "#26282d", pants: null } },
      ],
      female: [
        { label: "Jadecrest", skin: "#5fa87a", tail: { style: "lizard", color: "#5fa87a" }, crest: { style: "feathers", color: "#e8c04a" }, eyes: { style: "slit", color: "#ff9a3a" }, outfit: { top: "loincloth", color: "#c9a24a", color2: "#8a6a3a", pants: null } },
        { label: "Azurefrill", skin: "#4f7fb8", tail: { style: "lizard", color: "#4f7fb8" }, crest: { style: "frill", color: "#f0a8c0" }, eyes: { style: "slit", color: "#e8c04a" }, outfit: { top: "loincloth", color: "#e2d6bb", color2: "#a99c7c", pants: null } },
        { label: "Sunplume", skin: "#c9a24a", tail: { style: "lizard", color: "#c9a24a" }, crest: { style: "feathers", color: "#c9533a" }, eyes: { style: "slit", color: "#7fd0a0" }, outfit: { top: "loincloth", color: "#3f7f7a", color2: "#2f5f5a", pants: null } },
        { label: "Violetfin", skin: "#7a5ca8", tail: { style: "lizard", color: "#7a5ca8" }, crest: { style: "fin", color: "#7fd0c0" }, eyes: { style: "slit", color: "#ffd23a" }, outfit: { top: "loincloth", color: "#8f4f6b", color2: "#5a2f46", pants: null } },
      ],
    },
  };

  function deepMerge(base, extra) {
    const out = { ...base };
    for (const [key, value] of Object.entries(extra || {})) {
      out[key] = value && typeof value === "object" && !Array.isArray(value) && base?.[key] && typeof base[key] === "object" ? { ...base[key], ...value } : value;
    }
    return out;
  }

  function playableSpec(raceId, gender = "male", variant = 0) {
    const race = RACES[raceId];
    if (!race) return null;
    const list = race[gender === "female" ? "female" : "male"];
    const look = list[Math.max(0, Math.min(list.length - 1, Math.floor(Number(variant) || 0)))];
    const spec = deepMerge(race.base, look);
    if (look.eyes) spec.eyes = { ...(race.base.eyes || {}), ...look.eyes };
    return { rig: "humanoid", race: raceId, sex: gender === "female" ? "female" : "male", size: raceId === "dwarf" ? 0.86 : raceId === "hobbit" ? 0.74 : raceId === "elf" || raceId === "dark-elf" ? 1.04 : 1, ...spec };
  }

  /* ---------------- Enemy families ---------------- */
  const H = (extra) => ({ rig: "humanoid", ...extra });
  const human = (gender, variant, extra = {}) => ({ ...playableSpec("human", gender, variant), ...extra });
  const goblin = extra => H({ race: "goblin", size: 0.78, build: { legs: 18, torso: 20, head: 19, shoulders: 9.5, hips: 8.5, arm: 18, armW: 7, legW: 7.5 }, skin: "#7fa35a", ears: "goblin", nose: "long", eyes: { style: "beady", color: "#e8d24a" }, mouth: "grin", outfit: { top: "loincloth", color: "#7a5a3a" }, feet: "bare", hair: null, ...extra });
  const orc = extra => H({ race: "orc", size: 1.12, build: { legs: 24, torso: 30, head: 20, shoulders: 14, hips: 11.5, arm: 24, armW: 10, legW: 10.5 }, skin: "#6f8a4f", head: "orc", mouth: "tusks", ears: "pointy", nose: "snout", eyes: { color: "#c94a2a", fierce: true }, outfit: { top: "loincloth", color: "#6b4a2f" }, feet: "boots", ...extra });
  const troll = extra => H({ race: "troll", size: 1.36, build: { legs: 25, torso: 32, head: 18, shoulders: 15, hips: 12, arm: 30, armW: 10.5, legW: 11, hunch: 8, headForward: 4 }, skin: "#7f8f6a", head: "troll", mouth: "teeth", ears: "pointy", nose: "long", eyes: { style: "beady", color: "#ffd23a" }, outfit: { top: "loincloth", color: "#5a4a35" }, feet: "claws", hands: "claws", hair: { style: "shaggy", color: "#4a5a3a" }, ...extra });
  const giant = extra => H({ race: "giant", size: 1.75, build: { legs: 26, torso: 31, head: 20, shoulders: 14.5, hips: 12, arm: 25, armW: 10.5, legW: 11 }, skin: "#d49a74", nose: "big", eyes: { color: "#3a2a20", fierce: true }, outfit: { top: "loincloth", color: "#7a5a3a" }, feet: "boots", ...extra });
  const devil = extra => H({ race: "devil", size: 1.18, build: { legs: 24, torso: 29, head: 19, shoulders: 13, hips: 11, arm: 24, armW: 9, legW: 9.5 }, skin: "#b8402f", ears: "pointy", horns: { style: "devil", color: "#2f2420" }, tail: { style: "devil", color: "#8a2a1f" }, eyes: { style: "glow", color: "#ffd23a" }, mouth: "fangs", lower: "goat", fur: "#3a2420", outfit: { top: "none" }, ...extra });
  const imp = extra => H({ race: "impid", size: 0.62, build: { legs: 16, torso: 18, head: 20, shoulders: 9, hips: 8, arm: 16, armW: 6.5, legW: 7 }, skin: "#c4483a", ears: "pointy", horns: { style: "small", color: "#2f2420" }, tail: { style: "imp", color: "#8a2a1f" }, wings: { style: "bat", color: "#7a2420", span: 0.75 }, eyes: { style: "glow", color: "#ffd23a" }, mouth: "fangs", feet: "claws", hands: "claws", outfit: { top: "none" }, hair: null, ...extra });
  const skeleton = extra => H({ race: "undead", material: "bone", head: "skull", ears: "none", hair: null, nose: "none", brows: false, eyes: { style: "skull", color: "#7fd0ff" }, build: { legs: 24, torso: 26, head: 19, shoulders: 11, hips: 9, arm: 22, armW: 6.5, legW: 7 }, outfit: { top: "none", pants: null }, feet: "bare", ...extra });
  const vampire = (gender, extra) => H({ race: "undead", sex: gender, skin: "#e6dcd6", ears: "pointy", eyes: { style: "narrow", color: "#d0203a" }, mouth: "fangs", lashes: gender === "female", cape: "#6e1424", outfit: tunic("#2b2226", "#1f1a1c", { trim: "#a8263b" }), ...extra });
  const wraith = extra => H({ race: "undead", lower: "float", head: "round", ears: "none", hair: null, nose: "none", brows: false, eyes: { style: "void", color: "#ffe08a" }, skin: "#1f1a22", headwear: { style: "hood", color: "#8a7a5f" }, outfit: { top: "robe", color: "#8a7a5f" }, hands: "claws", ...extra });
  const armoredUndead = (eye, extra) => skeleton({ eyes: { style: "skull", color: eye }, size: 1.12, build: { legs: 24, torso: 28, head: 19, shoulders: 13, hips: 10.5, arm: 23, armW: 8, legW: 8.5 }, cape: extra?.cape, ...extra });
  const satyr = extra => H({ race: "satyr", lower: "goat", fur: "#7a5a3c", skin: "#d8a07a", ears: "pointy", horns: { style: "goat", color: "#d9cdb4" }, beard: { style: "goatee", color: "#6b4a2f" }, hair: { style: "curly", color: "#6b4a2f" }, eyes: { color: "#8a5a1a" }, tail: { style: "tuft", color: "#7a5a3c" }, outfit: { top: "none" }, ...extra });
  const dryad = extra => H({ race: "dryad", sex: "female", skin: "#9ab86a", hair: { style: "leaves", color: "#5f9f4a" }, ears: "pointy", eyes: { style: "narrow", color: "#3f7a2a" }, lashes: true, outfit: { top: "leafdress", color: "#6f9f4a", color2: "#4f7f3a", trim: "#b5df73", pants: null }, feet: "bare", headwear: { style: "flower", color: "#f0a8c0" }, ...extra });
  const lamia = extra => H({ race: "lamia", sex: "female", lower: "snaketail", ears: "pointy", eyes: { style: "slit", color: "#e8c04a" }, lashes: true, outfit: { top: "scales", color: "#c9a24a", color2: "#8a6a2a" }, ...extra });
  const merfolk = extra => H({ race: "merfolk", lower: "fishtail", skin: "#8fbfc8", tailColor: "#3f8f9f", ears: "fin", eyes: { color: "#1f4f6f" }, hair: { style: "long", color: "#2f6f7a" }, outfit: { top: "scales", color: "#3f8f9f", color2: "#2f6f7a" }, ...extra });
  const harpy = extra => H({ race: "harpy", sex: "female", arms: "wings", lower: "bird", fur: "#8a6a4a", wingColor: "#8a6a4a", skin: "#e8c4a0", ears: "none", eyes: { color: "#b8741a", fierce: true }, lashes: true, hair: { style: "wavy", color: "#6b4a2f" }, crest: { style: "feathers", color: "#a8843a" }, outfit: { top: "feathers", color: "#8a6a4a", color2: "#6b4f35", trim: "#b8946a" }, ...extra });
  const treant = extra => H({ race: "treant", material: "bark", arms: "branches", lower: "roots", leafColor: "#7fbf5a", hair: { style: "leaves", color: "#6fae4a" }, ears: "none", nose: "none", mouth: "line", eyes: { style: "glow", color: "#d8ff8a" }, outfit: { top: "none", pants: null }, size: 1.3, build: { legs: 22, torso: 32, head: 20, shoulders: 14, hips: 12, arm: 26, armW: 10, legW: 12 }, ...extra });
  const elemental = (material, extra) => H({ race: "elemental", material, ears: "none", hair: null, nose: "none", brows: false, mouth: "line", eyes: { style: "glow", color: K.MATERIAL_SKINS[material]?.detail || "#ffffff" }, outfit: { top: "none", pants: null }, feet: "bare", build: { legs: 24, torso: 30, head: 18, shoulders: 14, hips: 11, arm: 25, armW: 10, legW: 10.5 }, size: 1.2, ...extra });
  const lizard = (variant, gender, extra) => ({ ...playableSpec("lizardman", gender, variant), ...extra });
  const wolf = extra => ({ rig: "quadruped", type: "wolf", ...extra });
  const bear = extra => ({ rig: "quadruped", type: "bear", len: 62, bodyH: 30, legLen: 18, headR: 14, ...extra });
  const griffon = extra => ({ rig: "griffon", ...extra });
  const dragon = extra => ({ rig: "dragon", size: 2.1, ...extra });
  const wisp = extra => ({ rig: "wisp", ...extra });

  const ENEMIES = {
    // Goblins
    "Goblin": goblin({ outfit: { top: "loincloth", color: "#7a5a3a" } }),
    "Goblin Archer": goblin({ skin: "#8aa860", headwear: { style: "hood", color: "#5f6b3f" } }),
    "Goblin Warrior": goblin({ skin: "#73995a", hair: { style: "mohawk", color: "#2f2420" }, eyes: { style: "beady", color: "#ff9a3a" } }),
    "Goblin Boss": goblin({ size: 0.9, skin: "#6f8f4a", headwear: { style: "bandana", color: "#a8263b" }, mouth: "tusks" }),
    "Highland Goblin": goblin({ skin: "#8f9f80", hair: { style: "shaggy", color: "#e8e0d0" }, outfit: { top: "fur", color: "#7b5634" } }),
    "Goblin Raider": goblin({ skin: "#6a8f4f", hair: { style: "mohawk", color: "#a8263b" }, scar: true, eyes: { style: "beady", color: "#ff5a3a" } }),
    "Goblin Warlord": goblin({ size: 0.98, skin: "#5f7f3f", mouth: "tusks", build: { legs: 19, torso: 23, head: 19, shoulders: 11, hips: 9.5, arm: 19, armW: 8, legW: 8.5 } }),
    // Bandits and other humans
    "Bandit": human("male", 1, { headwear: { style: "bandana", color: "#8a3b3b" }, beard: { style: "stubble", color: "#2b2420" } }),
    "Bandit Archer": human("female", 0, { headwear: { style: "hood", color: "#5a4a35" } }),
    "Highway Bandit": human("male", 0, { accessory: "eyepatch", beard: { style: "full", color: "#5a3a24" } }),
    "Highway Bandit Archer": human("female", 1, { headwear: { style: "brimhat", color: "#5a4a35", band: "#8a3b3b" } }),
    "Bandit Captain": human("male", 2, { headwear: { style: "tricorn", color: "#2f2a33", band: "#c9a24a" }, cape: "#7a2f3a" }),
    "Coastal Corsair": human("male", 3, { headwear: { style: "bandana", color: "#3f6fa8" }, accessory: "earring", beard: { style: "goatee", color: "#b8946a" } }),
    "Rainbound Marauder": human("male", 1, { headwear: { style: "hood", color: "#46505a" }, scar: true }),
    "Dune Raider": human("female", 2, { headwear: { style: "bandana", color: "#d8b67a" }, cape: "#b8946a" }),
    "Caravan Bandit": human("male", 2, { headwear: { style: "bandana", color: "#c9a24a" }, beard: { style: "full", color: "#1f1a17" } }),
    "Battlefield Scavenger": human("female", 3, { headwear: { style: "hood", color: "#4a463f" }, scar: true }),
    "Rogue Vampire Hunter": human("male", 0, { headwear: { style: "brimhat", color: "#26222d", band: "#8a1f2f" }, cape: "#2b2226", scar: true, beard: { style: "stubble", color: "#3a2a20" } }),
    // Orcs
    "Orc": orc({}),
    "Forest Orc": orc({ skin: "#4f6f3a", hair: { style: "shaggy", color: "#2f3a24" } }),
    "Orc Berserker": orc({ skin: "#7a8a4a", hair: { style: "mohawk", color: "#a8263b" }, scar: true }),
    "Orc Chieftain": orc({ size: 1.2, skin: "#6a7f45", headwear: { style: "skullcrown", color: "#e8e0cf", gem: "#ff6a3a" }, beard: { style: "braided", color: "#2f2420", tie: "#c9a24a" } }),
    // Her Apostles wear the same vestments: storm-dark plate, blood-red tabard, a storm-dark cape.
    "Apostle of Talos": {
      ...orc({ skin: "#5f7a6a", eyes: { style: "glow", color: "#bcd2ff" }, cape: "#2e3240", capeTrim: "#7a1a1a", headwear: { style: "storm-crown", color: "#3e3e46", gem: "#bcd2ff" },
        outfit: { top: "storm-plate", color: "#2e3240", color2: "#7a1a1a", trim: "#c9a24a", pants: "#22242c" }, outfitIsArmor: true }),
      storms: { thunderstorm: { static: "#bcd2ff", staticLevel: 1 }, tempest: { static: "#ffffff", staticLevel: 2, aura: "#cfe0ff" } },
    },
    "Thunder Boar Gorthok": { rig: "quadruped", type: "boar", size: 1.5, fur: "#4a4046", belly: "#6f646a", maneColor: "#2a2428", lightning: true, eyeGlow: true, eyes: "#ffe66b", len: 62, bodyH: 30, legLen: 18, headR: 15 },
    // Tankina, the storm priestess-warrior of Talos: storm-dark plate under a blood-red tabard, a horned
    // storm crown, lightning scars and braids crackling with static that grows with the storm.
    "Tankina, Disciple of Talos": {
      ...orc({ sex: "female", size: 1.3, skin: "#6f8f6f", lashes: true, hair: { style: "braid", color: "#1f2230", tie: "#ffe66b" }, eyes: { style: "glow", color: "#ffe66b" }, mouth: "tusks",
        headwear: { style: "storm-crown", color: "#4a4a52", gem: "#ffe66b" }, outfit: { top: "storm-plate", color: "#2e3240", color2: "#7a1a1a", trim: "#e0c060", pants: "#22242c" }, outfitIsArmor: true, cape: "#5a1414", capeTrim: "#e0c060",
        lightningScars: "#bfe0ff", static: "#ffe66b", staticLevel: 1 }),
      // Chosen of Talos: the storm burns in her.
      phases: { 1: { aura: "#ffe66b", staticLevel: 3, eyes: { style: "glow", color: "#ffffff" }, lightningScars: "#ffffff" } },
      storms: {
        rain: { staticLevel: 1 },
        storm: { staticLevel: 2, aura: "#9fc8ff" },
        thunderstorm: { staticLevel: 3, aura: "#cfe0ff", eyes: { style: "glow", color: "#ffffff" } },
        tempest: { staticLevel: 3, aura: "#ffffff", eyes: { style: "glow", color: "#ffffff" }, lightningScars: "#ffffff" },
      },
    },
    // Trolls
    "Hill Troll": troll({}),
    "Elder Troll": troll({ skin: "#8f9a8a", hair: { style: "shaggy", color: "#e8e4dc" }, beard: { style: "long", color: "#e8e4dc" }, size: 1.4 }),
    // Zik Lun's warband shares his war-paint red and bone: the warlord under a horned war-helm with
    // trophies and his standard on his back, a helmed Shieldbearer and a War-drummer.
    "Zik Lun, the armored Hulk": troll({ size: 1.85, build: { legs: 25, torso: 34, head: 18, shoulders: 18, hips: 13, arm: 31, armW: 12.5, legW: 12.5, hunch: 7, headForward: 4 }, skin: "#6f7f6a", hair: null, eyes: { style: "beady", color: "#ff5a3a" },
      helm: { color: "#55565a", horn: "#d8ccb0", trim: "#8a2a1a" }, warpaint: "#8a2a1a", sash: "#7a2416", trophies: { color: "#d8ccb0" }, standard: { cloth: "#7a2416", trim: "#d8ccb0" } }),
    "Troll Shieldbearer": troll({ size: 1.42, build: { legs: 25, torso: 33, head: 18, shoulders: 16, hips: 12.5, arm: 30, armW: 11.5, legW: 12, hunch: 6, headForward: 4 }, skin: "#768670", hair: null, eyes: { style: "beady", color: "#ffb04a" },
      helm: { color: "#5e5f63", horn: "#c9bca0", trim: "#8a2a1a" }, warpaint: "#8a2a1a", sash: "#7a2416" }),
    "Troll War-drummer": troll({ size: 1.36, skin: "#7f8f6a", hair: { style: "mohawk", color: "#8a2a1a" }, eyes: { style: "beady", color: "#ffd23a" }, warpaint: "#8a2a1a",
      trophies: { color: "#d8ccb0" }, drum: { color: "#6b3a22", skin: "#d8c8a0", emblem: "#8a2a1a" } }),
    "Thunder Troll": troll({ skin: "#6a7a8f", eyes: { style: "glow", color: "#ffe66b" }, hair: { style: "shaggy", color: "#2a2d3a" } }),
    "Bog Troll": troll({ skin: "#5a6a3f", hair: { style: "shaggy", color: "#3f4f2a" }, material: null, eyes: { style: "beady", color: "#c9f08f" } }),
    "Crag Troll": troll({ material: "stone", hair: { style: "shaggy", color: "#5c564f" }, eyes: { style: "glow", color: "#ffb04a" }, size: 1.45 }),
    // Giants
    "Young Giant": giant({ size: 1.42, hair: { style: "messy", color: "#8a5a2a" }, hands: "claws" }),
    "Sand Giant": giant({ skin: "#c99a6a", headwear: { style: "bandana", color: "#d8b67a" }, beard: { style: "full", color: "#5a3a24" } }),
    // Ornn, the smith-god: a forge-cracked giant in a scorched leather apron and Godforged
    // pauldrons, molten beard and flaming hair; his tempers show on his plate and hammer.
    "Ornn, God of the Forge": {
      ...giant({ size: 1.9, skin: "#d0885a", material: null, eyes: { style: "glow", color: "#ffb04a" }, beard: { style: "long", color: "#ff8a3d" }, hair: { style: "flame", color: "#ff8a3d" },
        outfit: { top: "smith-apron", color: "#8a5a38", trim: "#e0b060", belt: "#3a2418", pants: "#4a3a2e" }, outfitIsArmor: true,
        pauldrons: { color: "#b08a4a", trim: "#ffd27a" }, forgeCracks: "#ffb04a", embers: "#ff8a3d", emberLevel: 1, aura: "#ff8a3d" }),
      phases: { 1: { emberLevel: 3, forgeCracks: "#ffe2a8", aura: "#ffb04a" } },
      tempers: {
        "molten-edge": { moltenWeapon: "#ffb04a", emberLevel: 2 },
        "hardened-plate": { pauldrons: { color: "#a8a8b4", trim: "#ffffff", sheen: "#dfe8ff" } },
        "spiked-plate": { pauldrons: { color: "#b08a4a", trim: "#ffd27a", spikes: "#ff8a3d" } },
      },
      reforging: { emberLevel: 3, forgeCracks: "#ffe2a8" },
    },
    "Stone Giant": giant({ material: "stone", eyes: { style: "glow", color: "#e8c75e" }, beard: { style: "full", color: "#5c564f" } }),
    "Peak Giant": giant({ skin: "#a8b0b8", hair: { style: "long", color: "#e8e4dc" }, beard: { style: "long", color: "#e8e4dc" }, outfit: { top: "fur", color: "#8a7f72" } }),
    "Glacier Giant": giant({ size: 1.9, material: "ice", beard: { style: "long", color: "#e8f8ff" }, horns: { style: "crystal", color: "#d8f4ff" } }),
    "Stormforged Giant": giant({ size: 1.9, material: "lightning", beard: { style: "forked", color: "#2c3058" }, eyes: { style: "glow", color: "#ffe66b" } }),
    // Devils and demons
    // Saffron's court: each Servant wears his saffron-gold sash and his brand on the brow.
    "Great Devil Servant - Archer": devil({ skin: "#a8382a", sash: "#c8902a", brand: "#ffb347", eyes: { style: "glow", color: "#ffb347" } }),
    "Great Devil Servant - Protector": devil({ skin: "#9a2f24", horns: { style: "ram", color: "#2f2420" }, sash: "#c8902a", brand: "#ffb347", eyes: { style: "glow", color: "#ffb347" } }),
    "Great Devil Servant - Reaver": devil({ skin: "#b8402f", horns: { style: "long", color: "#2f2420" }, sash: "#c8902a", brand: "#ffb347", eyes: { style: "glow", color: "#ffb347" } }),
    // Archdevil Saffron: a towering archdevil in black and saffron gold, the Seventh Cycle's seven
    // sigils glowing on his breastplate, a burning crown between curling horns and ember-veined
    // wings; every sacrifice (Infernal Power) makes his hellfire burn brighter.
    "Archdevil Saffron": {
      ...devil({ size: 1.55, skin: "#c8602a", horns: { style: "long", color: "#1d1414" }, wings: { style: "bat", color: "#3a1210", span: 1.3, veins: "#ff8a3d" }, hair: { style: "long", color: "#1d1414" }, eyes: { style: "glow", color: "#ffe08a" },
        headwear: { style: "hellfire-crown", color: "#e0b54a", gem: "#ff4a2e", flame: "#ff5a2a", blaze: 1 },
        outfit: { top: "infernal-robe", color: "#1e1416", color2: "#2e2a30", trim: "#e0b54a", sigils: "#ffb347" }, outfitIsArmor: true, sigilLevel: 1, aura: "#ff8a3d" }),
      infernal: {
        1: { sigilLevel: 2, headwear: { style: "hellfire-crown", color: "#e0b54a", gem: "#ff4a2e", flame: "#ff5a2a", blaze: 2 } },
        2: { sigilLevel: 3, headwear: { style: "hellfire-crown", color: "#e0b54a", gem: "#ff4a2e", flame: "#ff7a2a", blaze: 3 }, aura: "#ffb347" },
        3: { sigilLevel: 3, headwear: { style: "hellfire-crown", color: "#e0b54a", gem: "#ffe08a", flame: "#ffb347", blaze: 3 }, aura: "#ffd27a", eyes: { style: "glow", color: "#ffffff" }, wings: { style: "bat", color: "#3a1210", span: 1.3, veins: "#ffd27a" } },
      },
    },
    // A gigantic imp mutated by the void: brawny arms, goat legs, small rotting wings,
    // plates of void-iron grown into his arms and chest and a jagged black horn-crown.
    "Great Imp Ji'ruan": imp({ size: 1.42, skin: "#5a3566", build: { legs: 21, torso: 25, head: 18, shoulders: 15, hips: 10.5, arm: 23, armW: 10.5, legW: 11 }, lower: "goat", goatBuild: "brute", fur: "#3b2a44",
      wings: { style: "decaying", color: "#4a3a52", span: 0.85 }, horns: { style: "jagged-crown", color: "#141018", glow: "#c58cff" }, eyes: { style: "glow", color: "#c58cff" }, mouth: "fangs",
      tail: { style: "imp", color: "#3a2442" }, voidVeins: "#c58cff", plates: { color: "#262030", trim: "#6a5580" } }),
    "Lesser Imp": imp({}),
    // Ji'ruan's retinue, touched by the same void: violet skin, glowing cracks, ragged wings.
    "Void Imp": imp({ size: 0.7, skin: "#6a3d78", wings: { style: "bat", color: "#3a2448", span: 0.8 }, horns: { style: "small", color: "#141018" }, eyes: { style: "glow", color: "#c58cff" }, tail: { style: "imp", color: "#3a2442" }, voidVeins: "#c58cff" }),
    "Void Stalker": imp({ size: 0.82, material: "shadow", wings: { style: "bat", color: "#2a1f3a", span: 0.9 }, eyes: { style: "glow", color: "#b08cff" } }),
    "Corrupted Wolf": wolf({ size: 1.0, fur: "#3f3046", belly: "#5a4a63", corruption: true, eyeGlow: true, eyes: "#ff4a4a" }),
    "Corrupted Bear": bear({ size: 1.15, fur: "#3a2d40", belly: "#56465e", corruption: true, eyeGlow: true, eyes: "#ff4a4a" }),
    "Corrupted Griffon": griffon({ size: 1.15, fur: "#3f3046", feather: "#5a4a63", wing: "#2a1f33", beak: "#8a7a8f", corruption: true, eyeGlow: true, eyes: "#ff4a4a" }),
    // Undead
    "Undead Thrall": skeleton({ outfit: { top: "none", pants: null } }),
    "Undead Hulk": H({ race: "undead", size: 1.4, skin: "#8a9a7a", head: "orc", mouth: "teeth", eyes: { style: "glow", color: "#c9f08f" }, ears: "round", hands: "claws", feet: "claws", scar: true, hair: null, outfit: { top: "rags", color: "#5a5446", pants: "#4a463f" }, build: { legs: 24, torso: 32, head: 18, shoulders: 16, hips: 12, arm: 28, armW: 11, legW: 11, hunch: 10, headForward: 4 } }),
    "Bloodsand Vampire": vampire("male", { hair: { style: "swept", color: "#1f1a1c" } }),
    "Sand Wraith": wraith({ skin: "#3a2f22", headwear: { style: "hood", color: "#b8946a" }, outfit: { top: "robe", color: "#b8946a" }, eyes: { style: "void", color: "#ffd23a" } }),
    "Vampire Stalker": vampire("female", { hair: { style: "bob", color: "#1f1a1c" } }),
    "Tomb Guardian": armoredUndead("#ffd23a", {}),
    "Dust Revenant": wraith({ skin: "#2a2620", headwear: { style: "hood", color: "#8a8478" }, outfit: { top: "robe", color: "#8a8478" }, eyes: { style: "void", color: "#9fe8a0" } }),
    "Vampire Hexer": vampire("female", { hair: { style: "long", color: "#3a1f2a" }, headwear: { style: "circlet", color: "#c9c2dc", gem: "#d0203a" } }),
    "Crimson Vampire Knight": vampire("male", { hair: { style: "swept", color: "#8a1f2f" }, size: 1.08 }),
    "Bloodstone Reaver": armoredUndead("#ff3a3a", { material: null, head: "round", skin: "#c9b8b0", eyes: { style: "glow", color: "#ff3a3a" }, mouth: "fangs", nose: "small", ears: "pointy" }),
    "Sanguine Oracle": vampire("female", { hair: { style: "wavy", color: "#a8263b" }, headwear: { style: "circlet", color: "#e0b54a", gem: "#ff3a5a" } }),
    "Bloodrock Sentinel": armoredUndead("#ff3a3a", { cape: "#6e1424" }),
    // Ool's grave-knights: green-eyed armored dead with tabards bearing his sigil.
    "Gravebound Legionnaire": armoredUndead("#7cff9a", { cape: "#2f3a2a", tabard: { color: "#1e2420", trim: "#5a6a4a", sigil: "#7cff9a" } }),
    "Duskpine Wraith": wraith({ skin: "#141a2a", headwear: { style: "hood", color: "#2f3f5f" }, outfit: { top: "robe", color: "#2f3f5f" }, eyes: { style: "void", color: "#9fe0ff" } }),
    "Nocturne Castellan": armoredUndead("#b08cff", { cape: "#2b2240", size: 1.18 }),
    "Nightwood Vampire": vampire("male", { hair: { style: "long", color: "#1f1a1c" }, headwear: { style: "crown", color: "#2a2428", gem: "#d0203a" }, size: 1.08 }),
    "Blood Exile": armoredUndead("#ff6a3a", { cape: "#4a0f14" }),
    // Ancient Lich Ool: a towering lich in tattered grave-robes, a crown of bone on his skull and
    // green soul-fire burning in his open ribcage; it gutters to an ember once his phylactery breaks.
    "Ancient Lich Ool": {
      ...skeleton({ size: 1.5, lower: "float", eyes: { style: "skull", color: "#7cff9a" }, headwear: { style: "bone-crown", color: "#d8cfb8", gem: "#7cff9a" },
        outfit: { top: "grave-robe", color: "#1f2620", color2: "#2e3628", trim: "#6a7a52", sigil: "#7cff9a" }, outfitIsArmor: true, soulFire: "#7cff9a", aura: "#7cff9a",
        build: { legs: 25, torso: 28, head: 19, shoulders: 12.5, hips: 9.5, arm: 24, armW: 6.5, legW: 7, hunch: 4 } }),
      mortal: { soulDimmed: true, aura: null, eyes: { style: "skull", color: "#5a8a62" }, headwear: { style: "bone-crown", color: "#b8ae98", gem: "#3a5a40" } },
    },
    // His phylactery: a shard of dark crystal in a cage of bones on a grave-stone, his soul burning in it.
    "Phylactery of Ool": { rig: "core", style: "phylactery", size: 1.35, color: "#1c2628", heart: "#7cff9a", bone: "#d8cfb8", stone: "#4a4a46" },
    "Pale Monarch": skeleton({ size: 1.25, lower: "float", eyes: { style: "skull", color: "#d8e8ff" }, headwear: { style: "crown", color: "#e0e4ea", gem: "#9fb6d4" }, outfit: { top: "robe", color: "#e8e4dc", trim: "#9fb6d4" }, cape: "#8a8f9a" }),
    // Satyrs, dryads, lamias, merfolk, sirens, harpies
    "Satyr Scout": satyr({}),
    "Satyr Warrior": satyr({ fur: "#5a3f2a", skin: "#c48a64", horns: { style: "ram", color: "#d9cdb4" }, size: 1.05 }),
    "Satyr Grovekeeper": satyr({ headwear: { style: "flower", color: "#f6d06b" }, beard: { style: "long", color: "#8a7a6a" }, hair: { style: "curly", color: "#8a7a6a" } }),
    "Dryad Warden": dryad({}),
    "Dryad Matriarch": dryad({ size: 1.1, skin: "#8aa86a", horns: { style: "antlers", color: "#7a5a3a" }, hair: { style: "leaves", color: "#4f8f3a" } }),
    // The tree-mother: bark plates over a dryad's body, a trunk that spreads into roots, great
    // moss-hung antlers. Her foliage, crown, blossoms and aura follow the season of her fight.
    "Juila the Grand Forest Matriarch": {
      ...dryad({ size: 1.5, skin: "#8ea46a", build: { legs: 24, torso: 29, head: 20, shoulders: 12.5, hips: 11, arm: 24, armW: 8, legW: 9.5 }, lower: "trunk", barkColor: "#5a4632", moss: "#6f9a4a",
        horns: { style: "great-antlers", color: "#6b4a2f", moss: "#6f9a4a", blossoms: "#f6b8d0" }, hair: { style: "leaves", color: "#4f8f3a" }, headwear: { style: "flower", color: "#f6a0c8" },
        eyes: { style: "glow", color: "#d8ff8a" }, outfit: { top: "leafdress", color: "#5f8f42", color2: "#3f6a30", trim: "#b5df73", pants: null }, feet: null, aura: "#b8f59a" }),
      seasons: {
        spring: { hair: { style: "leaves", color: "#6fb04a" }, headwear: { style: "flower", color: "#ffb3d1" }, horns: { style: "great-antlers", color: "#6b4a2f", moss: "#7fb05a", blossoms: "#ffc4dc" }, outfit: { top: "leafdress", color: "#6fa04a", color2: "#4f8a3a", trim: "#ffc4dc", pants: null }, aura: "#ffd6e6" },
        summer: { hair: { style: "leaves", color: "#3f7f2a" }, headwear: { style: "flower", color: "#ffd36b" }, horns: { style: "great-antlers", color: "#6b4a2f", moss: "#4f8a3a", blossoms: "#ffe08a" }, outfit: { top: "leafdress", color: "#3f7a32", color2: "#2f5f28", trim: "#ffe08a", pants: null }, aura: "#ffe9a0" },
        autumn: { hair: { style: "leaves", color: "#c8662a" }, headwear: { style: "flower", color: "#e8a23a" }, horns: { style: "great-antlers", color: "#5a3e2a", moss: "#8a7a3a", blossoms: "#d8582a" }, outfit: { top: "leafdress", color: "#9a5a2a", color2: "#6a3a1e", trim: "#f0b04a", pants: null }, aura: "#ffb070" },
        winter: { hair: { style: "leaves", color: "#a8bcc4" }, headwear: { style: "flower", color: "#e8f4ff" }, horns: { style: "great-antlers", color: "#5a4a40", moss: "#8a9a9a", blossoms: "#f4fbff", frost: true }, outfit: { top: "leafdress", color: "#5a6f72", color2: "#3f5052", trim: "#dff0ff", pants: null }, aura: "#cfe8ff", eyes: { style: "glow", color: "#cfefff" } },
      },
    },
    "Grove Matriarch": dryad({ size: 1.12, skin: "#86a866", horns: { style: "antlers", color: "#6b4a2f" }, hair: { style: "leaves", color: "#4f8f3a" }, headwear: { style: "flower", color: "#f6c0d8" }, eyes: { style: "glow", color: "#d8ff8a" } }),
    "Blossom Sprite": wisp({ size: 0.7, color: "#f2b8cc", leaves: "#a8e07a", flower: "#fff0f6", glow: "#ffd6e6" }),
    "Harvest Wisp": wisp({ size: 0.86, kind: "ghost", color: "#9a5a2a", leaves: "#e0782a", lantern: "#ffb24a", eyeGlow: true, eyes: "#ffcf6a", glow: "#ff9a4a" }),
    "Rime Wolf": wolf({ size: 1.02, fur: "#c8d6de", belly: "#f4fbff", eyeGlow: true, eyes: "#bfe6ff", mane: true, maneColor: "#e8f4fa" }),
    "Sunhorn Stag": { rig: "quadruped", type: "stag", size: 1.05, fur: "#c8963e", belly: "#f2dca0", hooves: true, antlerColor: "#ffe08a", antlerGlow: "#ffd36b", eyeGlow: true, eyes: "#fff2a8" },
    "Sunscale Lamia": lamia({ skin: "#d8a874", tailColor: "#d0a23a", bellyColor: "#f2dca0", hair: { style: "long", color: "#3a2a1f" }, headwear: { style: "circlet", color: "#e0b54a", gem: "#ff8a3d" } }),
    "Mossback Lamia": lamia({ skin: "#a8b87a", tailColor: "#4f7f3a", bellyColor: "#b8cf8a", hair: { style: "leaves", color: "#3f6f2a" }, outfit: { top: "scales", color: "#4f7f3a", color2: "#3a5f2a" } }),
    "Mountain Lamia": lamia({ skin: "#c8ccd4", tailColor: "#6a7a8f", bellyColor: "#c9d0d8", hair: { style: "braid", color: "#e8f0ff" }, eyes: { style: "slit", color: "#7fd0ff" }, outfit: { top: "scales", color: "#6a7a8f", color2: "#4a5a6f" } }),
    "Merfolk Spearfisher": merfolk({}),
    "Reed Merfolk": merfolk({ skin: "#8fb898", tailColor: "#4f8f5a", hair: { style: "shaggy", color: "#3f5f2a" } }),
    "Merfolk Reef Guard": merfolk({ tailColor: "#2f6f8f", hair: { style: "crop", color: "#1f4f5f" }, size: 1.08 }),
    "Merfolk Tidecaller": merfolk({ sex: "female", lashes: true, hair: { style: "wavy", color: "#3f9fa8" }, headwear: { style: "circlet", color: "#e8e0d0", gem: "#7fd0ff" } }),
    "Merfolk Steamcaller": merfolk({ sex: "female", lashes: true, tailColor: "#5f7f9f", hair: { style: "bun", color: "#c9d0d8" } }),
    // The Deep Sea King: a regal duelist of the deep under a coral crown, in shell-and-pearl armor
    // with a kelp-hung, gold-edged cape and a long royal tail.
    "Deep Sea King": merfolk({ size: 1.6, build: { legs: 24, torso: 30, head: 17, shoulders: 15.5, hips: 11, arm: 28, armW: 9.5, legW: 10, hunch: 0, headForward: 2 }, skin: "#7fb0bc", tailColor: "#1f5f7f", bellyColor: "#9fd0d8", tailStyle: "royal", tailBand: "#c9a54a",
      hair: { style: "long", color: "#e8e4dc" }, beard: { style: "long", color: "#e8e4dc" }, eyes: { color: "#9fe0ff" }, headwear: { style: "coral-crown", color: "#f0907a", band: "#c9a54a", gem: "#f4ecdc" },
      outfit: { top: "shell", color: "#d8c8b0", color2: "#b8a080", trim: "#c9a54a" }, outfitIsArmor: true, cape: "#1f4f6f", capeTrim: "#c9a54a", kelp: "#3f7a4a" }),
    "Tide Siren": merfolk({ sex: "female", lashes: true, tailColor: "#3fa8a0", skin: "#a8d8d0", hair: { style: "long", color: "#2fa8a0" }, outfit: { top: "scales", color: "#e8d8c0", color2: "#c9b8a0" }, headwear: { style: "flower", color: "#f6c0d0" } }),
    "Harpy Skirmisher": harpy({}),
    "Harpy Huntress": harpy({ fur: "#7a7268", wingColor: "#7a7268", hair: { style: "highponytail", color: "#3a2f22" }, outfit: { top: "feathers", color: "#7a7268", color2: "#5a544c", trim: "#a8a094" } }),
    "Harpy Matriarch": harpy({ size: 1.15, fur: "#5a4a3a", wingColor: "#5a4a3a", crest: { style: "feathers", color: "#f2ead8" }, headwear: { style: "crown", color: "#c9a24a", gem: "#3f8f5a" } }),
    "Ash Harpy": harpy({ fur: "#5f5a58", wingColor: "#5f5a58", hair: { style: "wavy", color: "#2a2626" }, crest: { style: "feathers", color: "#ff8a3d" }, eyes: { style: "glow", color: "#ff8a3d" }, outfit: { top: "feathers", color: "#5f5a58", color2: "#3f3a38", trim: "#ff8a3d" } }),
    "Storm Harpy": harpy({ fur: "#4f5a6f", wingColor: "#4f5a6f", hair: { style: "wavy", color: "#d8e0ff" }, crest: { style: "feathers", color: "#ffe66b" }, eyes: { style: "glow", color: "#ffe66b" }, outfit: { top: "feathers", color: "#4f5a6f", color2: "#363f52", trim: "#bcd2ff" } }),
    "Sirocco Harpy": harpy({ fur: "#c9a46a", wingColor: "#c9a46a", skin: "#c48a64", hair: { style: "wavy", color: "#8a5a2a" }, outfit: { top: "feathers", color: "#c9a46a", color2: "#a8843a", trim: "#f2dca0" } }),
    "Harpy Sentinel": harpy({ size: 1.1, fur: "#e8e0d0", wingColor: "#e8e0d0", hair: { style: "long", color: "#f2ead8" }, crest: { style: "feathers", color: "#c9a24a" }, headwear: { style: "circlet", color: "#c9a24a", gem: "#7fb4ff" }, outfit: { top: "feathers", color: "#e8e0d0", color2: "#c9c0a8", trim: "#c9a24a" } }),
    // Treants
    "Young Treant": treant({ size: 1.15 }),
    "Elder Treant": treant({ size: 1.55, hair: { style: "leaves", color: "#4f7f3a" }, leafColor: "#5f8f3a", beard: { style: "long", color: "#6f9a4a" }, eyes: { style: "glow", color: "#ffd86b" } }),
    "Jungle Treant": treant({ size: 1.9, hair: { style: "leaves", color: "#3f9f4a" }, leafColor: "#3f9f4a", eyes: { style: "glow", color: "#fff27a" }, headwear: { style: "flower", color: "#ff6a8a" } }),
    // Beasts
    "Juvenile Wolf": wolf({ size: 0.72, fur: "#9a9590" }),
    "Adult Wolf": wolf({ size: 0.92, fur: "#8a7f72" }),
    "Highland Wolf": wolf({ size: 0.98, fur: "#c9c4bc", belly: "#f2efe8" }),
    "Dire Wolf": wolf({ size: 1.2, fur: "#4f4a46", belly: "#7a746e", eyeGlow: true, eyes: "#ff5a3a", mane: true }),
    "Duskpine Wolf": wolf({ size: 1.12, fur: "#2f3446", belly: "#4f5a6f", frost: true, eyeGlow: true, eyes: "#9fe0ff" }),
    "Boreal Direwolf": wolf({ size: 1.3, fur: "#e2e6ea", belly: "#ffffff", frost: true, mane: true, maneColor: "#c4ccd4", eyeGlow: true, eyes: "#7fd0ff" }),
    "Juvenile Bear": bear({ size: 0.78, fur: "#8a5a36" }),
    "Adult Bear": bear({ size: 1.05, fur: "#6b4527" }),
    "Mountain Bear": bear({ size: 1.28, fur: "#4f453f", mane: true, maneColor: "#2f2724", scars: true }),
    "Juvenile Griffon": griffon({ size: 0.78, fur: "#d8b07a", feather: "#fbf4e6" }),
    "Adult Griffon": griffon({ size: 1.0 }),
    "Highland Griffon": griffon({ size: 1.08, fur: "#9a958e", feather: "#f2efe8", wing: "#8a8580", beak: "#d8b45a" }),
    "Great Griffon": griffon({ size: 1.3, fur: "#c99a3a", feather: "#fff4d8", wing: "#b8862a", crest: "#f2cf73" }),
    "High Griffon": griffon({ size: 1.45, fur: "#e8dcc0", feather: "#ffffff", wing: "#d8c8a0", beak: "#f2cf73", crown: "#f2cf73", crest: "#f2cf73" }),
    // The Vulcanic Ram: obsidian hide cracked with magma, a fleece of flame and molten horns.
    "Vulcanic Ram": { rig: "quadruped", type: "ram", size: 1.6, fur: "#2a1e1c", belly: "#3a2622", magma: true, molten: "#ffb04a", hooves: true, hornColor: "#3a2a26", eyeGlow: true, eyes: "#ffe2a8", len: 56, bodyH: 26, legLen: 20, headR: 14 },
    // Dragons
    "Mountain Wyrm": dragon({ form: "wyrm", size: 1.75, scale: "#7a6a5a", belly: "#c9b896", rocks: "#8f877c", horn: "#d9cdb4" }),
    // An ancient, heavy earth dragon under stone slabs, still carrying the spears, arrows, sword and
    // banner of the army that scarred him. His three old wounds show their state in the fight; when
    // they burst (phase two) every scar and crack burns like molten rock.
    "Nygheir the Wounded": {
      ...dragon({ size: 2.4, len: 90, bodyH: 42, legLen: 21, headR: 18.5, legW: 1.2, headLift: 1.25, headReach: 0.64, neckW: 1.45, wingSpan: 1.2, scale: "#6e5636", belly: "#b8975e", membrane: "#7a6448", wing: "#4e3c26",
        horn: "#d8ccb0", plates: "#756a5c", hornCrown: true, relics: true, tattered: true, scarredEye: true, wounds: 3, eyeGlow: true, eyes: "#ffb04a" }),
      phases: { 1: { burning: "#ff7a2e", eyes: "#ffd27a", aura: "#ff8a3d" } },
    },
    "Shore Wyrm": dragon({ form: "wyrm", size: 1.75, scale: "#2f8f8f", belly: "#c9e8d8", fins: "#7fd0c0", frill: "#7fd0c0" }),
    "Ember Drake": dragon({ form: "drake", size: 1.45, len: 64, bodyH: 24, headR: 12, scale: "#d0602a", belly: "#ffd08a", wingFire: true, eyes: "#ffe08a", wingSpan: 0.8 }),
    "Cliff Wyrm": dragon({ form: "wyrm", size: 1.85, scale: "#6a7078", belly: "#b8bcc0", rocks: "#5c564f", horn: "#e8e0cf" }),
    "Black Dragon": dragon({ scale: "#2a2630", belly: "#5a4a63", membrane: "#4a3a52", eyeGlow: true, eyes: "#b08cff", horn: "#8a7a8f" }),
    "Blue Dragon": dragon({ scale: "#2f5f9f", belly: "#a8d0f0", membrane: "#4f7fbf", horn: "#e8f0ff" }),
    "Bronze Dragon": dragon({ scale: "#9a6a3a", belly: "#e0c48a", membrane: "#b8864f", horn: "#f2e6c4", rocks: "#8f877c" }),
    "Crimson Dragon": dragon({ scale: "#8f1f2f", belly: "#e08a8a", membrane: "#a8263b", eyeGlow: true, eyes: "#ff3a5a", horn: "#2a1416" }),
    "Emerald Dragon": dragon({ scale: "#2f8f4f", belly: "#c8f0a8", membrane: "#4fa86a", leaves: "#7fcf6a", horn: "#f2e6c4" }),
    "Golden Dragon": dragon({ scale: "#d8a83a", belly: "#fff0c0", membrane: "#e8c060", horn: "#ffffff", eyes: "#ffffff", eyeGlow: true }),
    "Green Dragon": dragon({ scale: "#5f9f6a", belly: "#e0f4d8", membrane: "#8fcf9a", whiskers: "#e0f4d8", horn: "#e8e0cf" }),
    "Red Dragon": dragon({ scale: "#b8302a", belly: "#f2c48a", membrane: "#d0503a", horn: "#efe6d0" }),
    "Silver Dragon": dragon({ scale: "#b8c0cc", belly: "#f2f4f8", membrane: "#d0d8e4", horn: "#ffffff", eyes: "#7fb4ff" }),
    "Violet Dragon": dragon({ scale: "#5f3f7a", belly: "#c8b0e0", membrane: "#7a5a9a", eyeGlow: true, eyes: "#9fff9a", horn: "#d8cfe8" }),
    "White Dragon": dragon({ scale: "#e2ecf2", belly: "#ffffff", membrane: "#c8dcea", frost: true, horn: "#bfe8ff", eyes: "#3f8fd8" }),
    "Yellow Dragon": dragon({ scale: "#e0c43a", belly: "#fff4b8", membrane: "#f0d860", lightning: true, horn: "#2a2630", eyes: "#2a2630" }),
    "Canopy Wyrm": dragon({ form: "wyrm", size: 1.95, scale: "#3f7f3a", belly: "#c8e8a0", leaves: "#6fbf4a", frill: "#9fdf6a" }),
    "Ancient Frost Dragon": dragon({ size: 2.4, ancient: true, scale: "#c8dcea", belly: "#ffffff", membrane: "#a8c8e0", frost: true, eyeGlow: true, eyes: "#7fd0ff", horn: "#e8f8ff", spikeColor: "#9fd0f0", aura: "#bff0ff" }),
    "Tempest Dragon": dragon({ size: 2.4, ancient: true, scale: "#3f4a6a", belly: "#a8b8d8", membrane: "#5a6a8f", lightning: true, eyeGlow: true, eyes: "#ffe66b", horn: "#d8e0ff", aura: "#ffe66b" }),
    "Crownfire Dragon": dragon({ size: 2.45, ancient: true, scale: "#8f1f14", belly: "#ffb04a", membrane: "#c8401f", crownfire: true, wingFire: true, eyeGlow: true, eyes: "#ffe08a", horn: "#2a1410", aura: "#ff8a3d" }),
    // Anatemis: a colossal star-dragon of white scales, constellations shining through them, aurora
    // wings and a crown of light. Her Mirror Scales gleam while they hold. The Golden Star turns
    // her gold under a solar corona; the Zenith blazes white-gold with a star burning in her chest,
    // swelling as each supernova gathers.
    "Anatemis, the White Star": {
      ...dragon({ size: 2.6, ancient: true, celestial: true, scale: "#eef3ff", belly: "#ffffff", membrane: "#d8e6ff", wing: "#b8c8e8", eyeGlow: true, eyes: "#fff2c0", horn: "#f4f0e0", spikeColor: "#d6e6ff", aura: "#d6e6ff",
        constellations: "#bcd8ff", aurora: ["#9fe8ff", "#c8b0ff", "#9fffd8"], lightCrown: "#fff6d8" }),
      phases: {
        1: { scale: "#f0d890", belly: "#fff4d0", membrane: "#ffe8a8", wing: "#e0b860", horn: "#fff0c0", spikeColor: "#ffe08a", eyes: "#ffffff", aura: "#ffd27a", constellations: "#fff0b0", aurora: ["#ffd27a", "#ffb04a", "#fff0b0"], lightCrown: "#ffe8a0", corona: "#ffcc5a" },
        2: { scale: "#fff8e8", belly: "#ffffff", membrane: "#fff0d0", wing: "#f0d8a0", horn: "#ffffff", spikeColor: "#fff0c0", eyes: "#ffffff", aura: "#fff4d8", constellations: "#ffffff", aurora: ["#ffffff", "#ffe8a0", "#d8f0ff"], lightCrown: "#ffffff", corona: "#fff0b0", heartStar: "#fffbe8" },
      },
    },
    // Elementals, spirits and constructs
    "Water Spirit": wisp({ size: 0.7, color: "#5eaadb", glow: "#7fd0ff" }),
    "Fire Spirit": wisp({ size: 0.72, kind: "flame", color: "#f28a3a", glow: "#ff8a3d", eyes: "#5a1a0a" }),
    "Hemoghost": wisp({ size: 0.85, kind: "ghost", color: "#a8263b", drips: "#6e1424", eyeGlow: true, eyes: "#ffd8d8", glow: "#ff3a5a" }),
    "Greater Water Spirit": wisp({ size: 0.95, radius: 17, color: "#3f8fd0", crown: "#bfe8ff", glow: "#7fd0ff" }),
    "Earth Spirit": wisp({ size: 0.85, kind: "rock", color: "#8f7a5a", orbit: "#6f6252", eyes: "#2a1f14" }),
    "Nature Spirit": wisp({ size: 0.8, color: "#72ae5c", leaves: "#9fdf6a", flower: "#f6c0d0", glow: "#b8f59a" }),
    "Raging Water Spirit": wisp({ size: 1.0, radius: 17, color: "#2f6fbf", spikes: true, angry: true, glow: "#7fd0ff" }),
    "Greater Earth Spirit": wisp({ size: 1.05, radius: 18, kind: "rock", color: "#7a6a52", orbit: "#5c564f", angry: true, crown: "#c9a24a" }),
    "Radiant Sentinel": wisp({ size: 1.05, radius: 17, color: "#f2e6c4", mask: "#e0b54a", halo: "#ffe9a0", eyeGlow: true, eyes: "#fff6cf", glow: "#ffe9a0" }),
    "Greater Nature Spirit": wisp({ size: 1.0, radius: 18, color: "#5f9f4a", leaves: "#9fdf6a", flower: "#ff9ac0", crown: "#c9e08a", glow: "#b8f59a" }),
    "Earth Elemental": elemental("stone", { size: 1.3 }),
    "Fire Elemental": elemental("fire", { lower: "float", hair: { style: "flame", color: "#ffb04a" }, outfit: { top: "none", color: "#c8461d" } }),
    "Ice Elemental": elemental("ice", { horns: { style: "crystal", color: "#e8faff" } }),
    "Lightning Elemental": elemental("lightning", { lower: "float", outfit: { top: "none", color: "#2c3058" }, hair: { style: "spiky", color: "#ffe66b" } }),
    "Water Elemental": elemental("water", { lower: "fishtail", tailColor: "#3780b3", hair: { style: "long", color: "#7fc4ef" } }),
    "Wind Elemental": elemental("wind", { lower: "float", outfit: { top: "none", color: "#94cbb0" }, hair: { style: "swept", color: "#e8fff4" } }),
    "Steelguard Golem": elemental("metal", { size: 1.35, build: { legs: 24, torso: 32, head: 16, shoulders: 16, hips: 13, arm: 26, armW: 12, legW: 12.5 }, eyes: { style: "glow", color: "#ffb04a" } }),
    "Magma Elemental": elemental("magma", { size: 1.45, horns: { style: "crystal", color: "#3d2b27" } }),
    "Oasis Guardian": elemental("water", { size: 1.25, hair: { style: "leaves", color: "#5f9f4a" }, lower: "float", outfit: { top: "none", color: "#3780b3" } }),
    "Fen Spirit": wisp({ size: 0.95, kind: "ghost", color: "#5f7a4f", lantern: "#d8ff8a", eyeGlow: true, eyes: "#d8ff8a", glow: "#b8f59a" }),
    // The Jungle Cores: heartwood seeds whose hearts glow through cracks in the bark.
    "Jungle Core": { rig: "core", style: "seed", size: 0.95, color: "#6a4a2e", heart: "#d8ff8a", leaf: "#6fbf4a" },
    "Storm Elemental": elemental("lightning", { size: 1.45, lower: "float", outfit: { top: "none", color: "#2c3058" }, hair: { style: "flame", color: "#bcd2ff" }, aura: "#ffe66b" }),
    // Gaian: a colossus of mossy stone with roots for legs, the land's veins glowing through him
    // (dark when he is severed from his Cores), a canopy crown of leaves and blossoms.
    "Gaian, Avatar of the Land": {
      ...elemental("stone", { size: 2.05, lower: "roots", fur: "#4a3420", beard: { style: "long", color: "#5f8f3a" }, eyes: { style: "glow", color: "#d8ff8a" }, aura: "#b8f59a",
        headwear: { style: "canopy-crown", color: "#4f8a32", blossom: "#f4a8c8" }, moss: "#5f8f3a", veins: "#b8ff7a", outfitIsArmor: true,
        build: { legs: 26, torso: 32, head: 18, shoulders: 15.5, hips: 12, arm: 26, armW: 11, legW: 11.5 } }),
      severed: { veinsDim: true, aura: null, eyes: { style: "glow", color: "#6a7a5a" } },
    },
    "Frost Elemental": elemental("ice", { size: 1.45, horns: { style: "crystal", color: "#ffffff" }, lower: "float", outfit: { top: "none", color: "#82bcd8" } }),
    "Pyramid Sentinel": elemental("sandstone", { size: 1.4, headwear: { style: "crown", color: "#e0b54a", gem: "#3f8fd8" }, eyes: { style: "glow", color: "#7fd0ff" } }),
    // Lizardfolk
    "Lava Stalker": lizard(3, "male", { skin: "#c8461d", tail: { style: "lizard", color: "#c8461d" }, eyes: { style: "slit", color: "#ffd23a" }, crest: { style: "spines", color: "#2f2420" } }),
    "Mire Stalker": lizard(0, "male", { skin: "#5a6a3f", tail: { style: "lizard", color: "#5a6a3f" }, crest: { style: "frill", color: "#8a7a3a" } }),
    "Canopy Stalker": lizard(0, "female", { skin: "#3f9f4a", tail: { style: "lizard", color: "#3f9f4a" }, crest: { style: "feathers", color: "#e8c04a" } }),
    // Dark elves
    // Ur-Mat, the False Dreamer: a gaunt dark-elf archmage in layered violet robes strewn with
    // starlight, eyes closed in his dream and a third eye of dream-light on his brow, a crown of
    // floating dream-shards and glyphs drifting about him; the glyph of the law he has written
    // hangs above him. Lucid Change opens his eyes and sets his crown ablaze.
    "Ur-Mat, the False Dreamer": {
      ...playableSpec("dark-elf", "male", 2), size: 1.2, eyes: { style: "closed" }, thirdEye: "#e0c8ff",
      headwear: { style: "dream-crown", color: "#d8c8ff", glow: "#b48cff" },
      outfit: { top: "dream-robe", color: "#231a38", color2: "#3a2a5c", trim: "#c9b0ff", stars: "#f4ecff" }, outfitIsArmor: true, cape: "#2a1f45", glyphs: "#b48cff", aura: "#b08cff",
      phases: { 1: { eyes: { style: "glow", color: "#f4ecff" }, thirdEye: "#ffffff", glyphs: "#f4ecff", aura: "#e0c8ff", headwear: { style: "dream-crown", color: "#f4ecff", glow: "#ffffff", blaze: true } } },
    },
  };

  // Non-playable races chosen in Dev Mode borrow a representative enemy look.
  const RACE_FALLBACKS = { goblin: "Goblin", impid: "Lesser Imp", wolf: "Adult Wolf", bear: "Adult Bear", griffon: "Adult Griffon", orc: "Orc", demon: "Great Imp Ji'ruan", devil: "Great Devil Servant - Reaver", elemental: "Earth Elemental", giant: "Young Giant", dragon: "Red Dragon", troll: "Hill Troll", siren: "Tide Siren", harpy: "Harpy Skirmisher", satyr: "Satyr Scout", lamia: "Sunscale Lamia", undead: "Undead Thrall", treant: "Young Treant", dryad: "Dryad Warden", merfolk: "Merfolk Spearfisher" };

  /* ---------------- Rendering ---------------- */
  function itemFor(id) {
    if (!id || typeof getItem !== "function") return null;
    const item = getItem(id);
    return item && !item.unavailable ? item : null;
  }
  // Natural weapons are drawn as the claws or horns their beasts always had.
  const NATURAL_VISUALS = {
    "beast-claws": { kind: "beastclaws", name: "Beast Claws", unique: null },
    "vulcanic-ram-horns": { kind: "horns", name: "Vulcanic Ram Horns", unique: null },
    "stardragon-claws": { kind: "beastclaws", name: "Stardragon Claws", unique: "stardragon-claws" },
  };
  // Equipment ids become visuals for the humanoid rig.
  function equipmentVisuals(equipment = {}) {
    const primary = itemFor(equipment.primaryHand), off = itemFor(equipment.offHand), armor = itemFor(equipment.armor);
    const pv = K.itemVisual(primary), ov = K.itemVisual(off), av = K.itemVisual(armor);
    const twoHandedSame = pv && ov && primary.id === off.id && Number(primary.handed) === 2;
    const natural = NATURAL_VISUALS[equipment.natural];
    const naturalVisual = natural && !(pv && pv.slot === "weapon") ? { slot: "weapon", id: "natural-" + equipment.natural, handed: 2, material: "bone", ornate: natural.unique ? 5 : 0, rarity: natural.unique ? "masterwork" : "uncommon", ...natural } : null;
    return { primary: pv && pv.slot === "weapon" ? pv : naturalVisual, offhand: twoHandedSame ? null : ov && (ov.slot === "shield" || ov.slot === "weapon") ? ov : null, armor: av && av.slot === "armor" ? av : null };
  }

  const renderCache = new Map();
  function render(spec, equipment = null) {
    if (!spec) return null;
    const key = JSON.stringify(spec) + "|" + JSON.stringify(equipment || {});
    if (renderCache.has(key)) return renderCache.get(key);
    let result;
    if (spec.rig === "quadruped") result = K.quadruped(spec);
    else if (spec.rig === "griffon") result = K.griffon(spec);
    else if (spec.rig === "dragon") result = K.dragon(spec);
    else if (spec.rig === "wisp") result = K.wisp(spec);
    else if (spec.rig === "core") result = K.core(spec);
    else result = K.humanoid({ ...spec, equipment: equipment ? equipmentVisuals(equipment) : {} });
    result = { ...result, size: spec.size || 1, rig: spec.rig || "humanoid", aura: spec.aura || null };
    if (renderCache.size > 400) renderCache.clear();
    renderCache.set(key, result);
    return result;
  }

  function svgFor(result, options = {}) {
    if (!result) return "";
    const box = options.portrait ? result.portrait : result.box;
    const label = options.label ? `<title>${escapeText(options.label)}</title>` : "";
    return `<svg class="sprite-svg${options.portrait ? " sprite-portrait" : ""}" viewBox="${K.n(box.x)} ${K.n(box.y)} ${K.n(box.w)} ${K.n(box.h)}" ${options.portrait ? "" : `width="${K.n(box.w)}" height="${K.n(box.h)}"`} preserveAspectRatio="xMidYMax meet" ${options.label ? 'role="img"' : 'aria-hidden="true"'} focusable="false">${label}${result.markup}</svg>`;
  }
  const escapeText = value => String(value).replace(/[&<>"']/g, char => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[char]);

  function normalizeAppearance(appearance, fallbackSeed = "") {
    const gender = appearance?.gender === "female" ? "female" : appearance?.gender === "male" ? "male" : null;
    const variant = Number.isInteger(appearance?.variant) && appearance.variant >= 0 && appearance.variant < 4 ? appearance.variant : null;
    if (gender && variant != null) return { gender, variant };
    const seed = K.hash(fallbackSeed || "stat-fick");
    return { gender: gender || (seed % 2 ? "female" : "male"), variant: variant ?? seed % 4 };
  }

  /* An actor description from the battle or the character sheet:
     { raceId, appearance, equipment, enemyName, seed, gender } */
  function specFor(info) {
    if (info.enemyName && ENEMIES[info.enemyName]) {
      // A seasonal look (Juila) swaps in that season's colours, a later phase (Nygheir) its own look,
      // and old wounds show how many are open.
      const base = ENEMIES[info.enemyName];
      let spec = info.season && base.seasons?.[info.season] ? { ...base, ...base.seasons[info.season] } : base;
      if (info.phase && base.phases?.[info.phase]) spec = { ...spec, ...base.phases[info.phase] };
      // Tankina's side glows brighter as her storm grows.
      if (info.storm && base.storms?.[info.storm]) spec = { ...spec, ...base.storms[info.storm] };
      // A smith shows the temper he holds, and stokes his embers while he works.
      if (info.temper && base.tempers?.[info.temper]) spec = { ...spec, ...base.tempers[info.temper] };
      if (info.reforging && base.reforging) spec = { ...spec, ...base.reforging };
      if (info.severed && base.severed) spec = { ...spec, ...base.severed };
      if (info.infernal && base.infernal?.[info.infernal]) spec = { ...spec, ...base.infernal[info.infernal] };
      // A lich's soul-fire gutters once his phylactery breaks; the phylactery shows its cracks.
      if (info.soulDimmed && base.mortal) spec = { ...spec, ...base.mortal };
      if (info.cracks && spec.style === "phylactery") spec = { ...spec, cracks: info.cracks };
      // A star-dragon's mirror gleams while it holds; her heart-star swells as a supernova gathers.
      if (info.mirror && spec.constellations) spec = { ...spec, mirror: true };
      if (info.nova && spec.heartStar) spec = { ...spec, nova: true };
      // A dreamer wears the glyph of the law he has written.
      if (info.law) spec = { ...spec, lawGlyph: info.law };
      if (info.openWounds && spec.wounds) spec = { ...spec, openWounds: info.openWounds };
      return { spec, equipment: spec.rig && spec.rig !== "humanoid" ? null : info.equipment || null };
    }
    const raceId = String(info.raceId || "human");
    if (RACES[raceId]) {
      const look = normalizeAppearance(info.appearance || (info.gender ? { gender: info.gender } : null), info.seed || info.name || raceId);
      return { spec: playableSpec(raceId, look.gender, look.variant), equipment: info.equipment || null };
    }
    const fallback = ENEMIES[RACE_FALLBACKS[raceId]] || ENEMIES.Goblin;
    return { spec: fallback, equipment: fallback.rig && fallback.rig !== "humanoid" ? null : info.equipment || null };
  }

  window.Sprites = {
    races: Object.keys(RACES),
    hasPlayableLooks: raceId => Boolean(RACES[raceId]),
    lookLabel(raceId, gender, variant) { return RACES[raceId]?.[gender === "female" ? "female" : "male"]?.[variant]?.label || "Adventurer"; },
    enemyNames: () => Object.keys(ENEMIES),
    normalizeAppearance,
    // Sprite markup for an actor, plus geometry the stage uses for placement.
    forActor(info, options = {}) {
      const { spec, equipment } = specFor(info);
      const result = render(spec, equipment);
      return { ...result, svg: svgFor(result, options), spec };
    },
    portrait(info, label = "") {
      const { spec, equipment } = specFor(info);
      return svgFor(render(spec, equipment), { portrait: true, label });
    },
    playablePreview(raceId, gender, variant, equipment = null) {
      const spec = playableSpec(raceId, gender, variant);
      return svgFor(render(spec, equipment));
    },
    renderSpec: (spec, equipment = null) => svgFor(render(spec, equipment)),
    itemVisual: item => K.itemVisual(item),
  };
})();
// The race sigil can offer a look once sprites are available.
if (typeof updateRaceHero === "function") updateRaceHero();

"use strict";
/* The sheet modules' case lists (GM2 step 2), made in Node: sheets in the port's words (actor.sheet), the races of
   objects/races.json, and hand-made totals (stubs) for the sources GM2 doesn't have yet. Random spreads come from
   mulberry32 with fixed seeds, so the lists are the same on every run. */
const KEYS = ["strength", "vitality", "mind", "dexterity", "intelligence", "wisdom", "speed", "luck"];
const each = make => Object.fromEntries(KEYS.map((key, index) => [key, make(key, index)]));

function mulberry32(seed) {
  let a = seed >>> 0;
  return () => {
    a = (a + 0x6d2b79f5) >>> 0;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

// The new hero (defaultCharacter: Level 1, 10 in each attribute, 5 gold, a class point, HP 31, Mana 20; new-hero.cjs).
const newHero = () => ({
  race: null, level: 1, experience: 0, freePoints: 0, classPoints: 1,
  attributes: each(() => 10), spentPoints: each(() => 0),
  bonuses: { ...each(() => 0), armor: 0, evasion: 0 },
  hp: 31, mana: 20, purse: { silver: 0, gold: 5, platinum: 0 },
});

/* A crafted race in the old file's form (as objects/races.json writes one), id "stub-race", from the knobs a case
   names: its modifiers (whole numbers, as every race's), the passive's effects of the types the sheet reads. The page
   normalizes it with the old normalizeRaceDefinition, the port's tests with step 1's converter (OldRaceFile). */
function raceOf(knobs) {
  const effects = [];
  if (knobs.statPerLevel) effects.push({ type: "all-stats-flat-per-level-up", amount: knobs.statPerLevel });
  if (knobs.hpPerLevels) effects.push({ type: "max-hp-per-levels", amount: knobs.hpPerLevels.amount, interval: knobs.hpPerLevels.interval });
  if (knobs.manaPerLevels) effects.push({ type: "max-mana-per-levels", amount: knobs.manaPerLevels.amount, interval: knobs.manaPerLevels.interval });
  if (knobs.maxHpPercent != null) effects.push({ type: "max-hp-percent", amount: knobs.maxHpPercent });
  for (const [target, amount] of Object.entries(knobs.statPermanentPercent || {})) effects.push({ type: "stat-permanent-percent", target, amount });
  for (const [target, amount] of Object.entries(knobs.defensePermanentPercent || {})) effects.push({ type: "defense-permanent-percent", target, amount });
  for (const [target, amount] of Object.entries(knobs.defensePerEnemy || {})) effects.push({ type: "defense-flat-per-enemy", target, amount });
  for (const [target, amount] of Object.entries(knobs.defenseCap || {})) effects.push({ type: "defense-cap", target, amount });
  if (knobs.healingDisabled) effects.push({ type: "healing-disabled" });
  return {
    id: "stub-race", name: "Stub race", playable: knobs.playable !== false, icon: null, statModifiers: { ...knobs.statModifier },
    passive: { id: "stub-passive", name: "Stub passive", condition: "Always active", description: "A hand-made passive.", effects },
  };
}

function generator(seed) {
  const random = mulberry32(seed);
  const int = (low, high) => low + Math.floor(random() * (high - low + 1));
  const pick = list => list[Math.floor(random() * list.length)];
  const chance = p => random() < p;
  const percent = () => pick([0, 0, 0, 5, 10, -10, 12.5, -7.25, 33.3, 0.1, 2.75, 150, -40, 1 / 3]);
  // A sheet as play (and Dev Mode) can leave it: levels, attributes raised by points and levels, bonuses, resources.
  function sheet({ race = null, level = int(1, 100), dev = false } = {}) {
    const spentPoints = each(() => chance(0.5) ? int(0, 12) : 0);
    const attributes = each(key => 10 + (level - 1) + spentPoints[key] + (dev && chance(0.2) ? int(-25, 40) : 0));
    const required = 100 * level;
    return {
      race, level,
      experience: pick([0, int(0, required), int(0, 30 * required), required, 0.5, int(1, 9) + 0.25]),
      freePoints: chance(0.5) ? int(0, 25) : 0, classPoints: int(0, 6),
      attributes, spentPoints,
      bonuses: { ...each(() => chance(0.25) ? percent() : 0), armor: chance(0.3) ? percent() : 0, evasion: chance(0.3) ? percent() : 0 },
      hp: pick([0, 1, int(0, 600), 100000, int(5, 60)]), mana: pick([0, int(0, 400), 100000, int(3, 40)]),
      purse: { silver: int(0, 99), gold: int(0, 500), platinum: int(0, 3) },
    };
  }
  const someKeys = make => Object.fromEntries(KEYS.filter(() => chance(0.4)).map(key => [key, make()]));
  const stubs = {
    race: () => raceOf({
      statModifier: someKeys(() => pick([-3, -2, -1, 1, 2, 3, 4])),
      ...(chance(0.3) ? { statPerLevel: pick([1, 2]) } : {}),
      ...(chance(0.5) ? { hpPerLevels: { amount: pick([1, 2, 3]), interval: pick([1, 5, 10]) } } : {}),
      ...(chance(0.5) ? { manaPerLevels: { amount: pick([1, 2]), interval: pick([1, 5]) } } : {}),
      ...(chance(0.4) ? { maxHpPercent: pick([10, -10, 7.5, 25]) } : {}),
      ...(chance(0.4) ? { statPermanentPercent: someKeys(() => pick([15, -5, 2.5])) } : {}),
      ...(chance(0.5) ? { defensePermanentPercent: { armor: pick([0, 5, 8, -20]), evasion: pick([0, -10, 15, 3.3]) } } : {}),
      ...(chance(0.4) ? { defensePerEnemy: { armor: pick([0, 1]), evasion: pick([2, 3]) } } : {}),
      ...(chance(0.25) ? { defenseCap: pick([{ evasion: 0 }, { armor: 40 }, { armor: 12, evasion: 9 }, { evasion: -5 }]) } : {}),
      ...(chance(0.2) ? { healingDisabled: true } : {}),
    }),
    artifacts: () => ({
      statFlat: someKeys(() => pick([3, -4, 12, 1.5, -20])), statPercent: someKeys(() => pick([10, -15, 4.5])),
      statPermanentPercent: someKeys(() => pick([5, -5, 20])),
      maxHpFlat: pick([0, 15, -10, 2.5]), maxHpPercent: pick([0, 10, -5, 12.5]), maxManaFlat: pick([0, 8, 0.5]), maxManaPercent: pick([0, 15, -20]),
    }),
    classes: () => ({
      statPermanentPercent: someKeys(() => pick([5, 10, -3])),
      defensePermanentPercent: { armor: pick([0, 10, 4]), evasion: pick([0, 6, -12]) },
      maxHpPercent: pick([0, 10, 3.5]), maxManaPercent: pick([0, 20, -10]),
      defenseFlat: { armor: pick([0, 3, 7, 2.5]), evasion: pick([0, 2, 5, 1.25]) },
      ...(chance(0.3) ? { defenseCap: pick([{ armor: 3 }, { evasion: 10 }, { armor: 0, evasion: 50 }]) } : {}),
    }),
    equipment: () => ({
      armor: pick([0, 12, 45, 3]), ...(chance(0.5) ? { armorPerStrength: pick([0.1, 0.25]) } : {}), evasionDebuff: pick([0, 2, 7.6, 15]), evasionFlat: pick([0, 3, 1.5]), evasionPercent: pick([0, 10, -25, 7.5]),
      hpPercent: pick([0, 5, 12.5]),
      ...(chance(0.3) ? { defenseCap: pick([{ armor: 20 }, { evasion: 5 }]) } : {}),
      ...(chance(0.4) ? { speedLock: true } : {}),
    }),
    battle: () => ({
      ...(chance(0.4) ? { resourceMaximum: pick([{ hp: 77 }, { mana: 13 }, { hp: 250, mana: 0 }]) } : {}),
      permanentStatPercent: someKeys(() => pick([10, -20, 5.5])),
      permanentDefensePercent: { armor: pick([0, 25, -10]), evasion: pick([0, 15, -50]) },
      statPlus: someKeys(() => pick([3, -2, 7])),
      defensePlus: { armor: pick([0, 1, -3]), evasion: pick([0, 2]) },
    }),
  };
  return { random, int, pick, chance, sheet, stubs };
}

// A spread of sheets for every race of the file, the new hero for each, and the checks' own (Level 40, no points).
function raceSheets(old, seed, perRace) {
  const g = generator(seed);
  const list = [];
  for (const race of old.json("objects/races.json")) {
    list.push({ sheet: { ...newHero(), race: race.id }, devMode: false });
    list.push({ sheet: { ...newHero(), race: race.id, level: 40 }, devMode: false });
    for (let i = 0; i < perRace; i++) {
      const dev = g.chance(0.3);
      list.push({ sheet: g.sheet({ race: race.id, dev }), devMode: dev, ...(race.id === "lizardman" && g.chance(0.5) ? { enemyCount: g.int(1, 4) } : {}) });
    }
  }
  return list;
}

// Sheets with hand-made totals for each source GM2 doesn't have yet, alone and together (raceId names the stub race).
function stubSheets(seed, count) {
  const g = generator(seed);
  const list = [];
  const sources = ["race", "artifacts", "classes", "equipment", "battle"];
  for (const source of [...sources, "all"]) {
    for (let i = 0; i < count; i++) {
      const dev = g.chance(0.3);
      const stubs = {};
      for (const name of source === "all" ? sources : [source]) stubs[name] = g.stubs[name]();
      const race = stubs.race ? "stub-race" : null;
      const battle = Boolean(stubs.battle);
      list.push({ sheet: g.sheet({ race, dev }), devMode: dev, stubs, ...(battle && g.chance(0.5) ? { enemyCount: g.int(0, 5) } : {}) });
    }
  }
  // The edges: Peace and Balance's lock against lowered Speed (a flat amount and percentages), maxima pushed below 0.
  const lowered = { statFlat: { speed: -12, luck: -3 }, statPercent: { speed: -30 }, statPermanentPercent: { speed: -15 }, maxHpFlat: -200, maxManaFlat: -150 };
  for (const speedLock of [true, false]) {
    list.push({ sheet: g.sheet({}), devMode: false, stubs: { artifacts: lowered, equipment: { armor: 4, speedLock } } });
    list.push({ sheet: g.sheet({}), devMode: true, stubs: { artifacts: { statFlat: { speed: 6 }, statPercent: { speed: 10 } }, equipment: { speedLock }, battle: { permanentStatPercent: { speed: -40 } } } });
  }
  list.push({ sheet: g.sheet({}), devMode: false, stubs: { artifacts: { maxHpPercent: -250, maxManaPercent: -300 } } });
  // HP's and Mana's formulas below 0 under flat amounts: the formula is held at 0 before they are added.
  const low = { ...g.sheet({ dev: true }), hp: 3, mana: 3 };
  low.attributes = { ...low.attributes, vitality: -30, strength: -10, mind: -20, intelligence: -15, wisdom: -10 };
  list.push({ sheet: low, devMode: true, stubs: { artifacts: { maxHpFlat: 40, maxManaFlat: 30 } } });
  list.push({ sheet: g.sheet({}), devMode: false, stubs: { classes: { maxHpPercent: -180, maxManaPercent: -120 } } });
  // The worn equipment's caps below what Armor and Evasion come to.
  list.push({ sheet: g.sheet({}), devMode: false, stubs: { equipment: { armor: 30, defenseCap: { armor: 12, evasion: 4 } } } });
  return list;
}

// Sheets with no race (the new hero before the carousel, or a race the data doesn't have), and the edges.
function plainSheets(seed, count) {
  const g = generator(seed);
  const list = [{ sheet: newHero(), devMode: false }, { sheet: newHero(), devMode: true }];
  for (let i = 0; i < count; i++) {
    const dev = g.chance(0.4);
    list.push({ sheet: g.sheet({ dev }), devMode: dev });
  }
  // Limitless (GM15's cap of 200).
  for (let i = 0; i < 6; i++) list.push({ sheet: g.sheet({ level: g.int(95, 200) }), devMode: g.chance(0.5), limitless: true });
  // At and around the cap, with and without the EXP.
  for (const level of [99, 100]) for (const experience of [0, 5000, 20016, 20015, 1e6]) for (const devMode of [false, true])
    list.push({ sheet: { ...newHero(), level, experience }, devMode });
  // Bonuses that bring Armor or Evasion below 0, attributes at 0 and below, resources out of range.
  const edge = newHero();
  list.push({ sheet: { ...edge, bonuses: { ...edge.bonuses, armor: -300, evasion: -250 } }, devMode: true });
  list.push({ sheet: { ...edge, attributes: { ...edge.attributes, vitality: 0, mind: -5, dexterity: -30 } }, devMode: true });
  list.push({ sheet: { ...edge, hp: -7, mana: -1 }, devMode: false });
  list.push({ sheet: { ...edge, bonuses: { ...edge.bonuses, vitality: -100, mind: -150 } }, devMode: true });
  // Armor and Evasion in the thousands (no cap but the sources').
  list.push({ sheet: { ...edge, bonuses: { ...edge.bonuses, armor: 30000, evasion: 30000 } }, devMode: true });
  // HP's and Mana's formulas below 0 (Dev Mode's attributes far below 0).
  list.push({ sheet: { ...edge, attributes: { ...edge.attributes, vitality: -30, strength: -10, mind: -20, intelligence: -15, wisdom: -10 } }, devMode: true });
  list.push({ sheet: { ...edge, attributes: { ...edge.attributes, vitality: -3, mind: -4, intelligence: -1 }, hp: 5, mana: 5 }, devMode: true });
  return list;
}

module.exports = { KEYS, each, newHero, mulberry32, generator, raceOf, raceSheets, stubSheets, plainSheets };

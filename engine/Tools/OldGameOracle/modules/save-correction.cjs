/* A loaded character's numbers corrected (GM2 step 6; the user's Q45, 2026-11-16: out-of-range numbers corrected
   silently, as the old game): the old normalizeCharacter (old-game/scripts/app.js 1399-1563) on sheets in the port's
   words (actor.sheet's fields, as a save or a character file holds them), given to it in the old save's keys (raceId,
   level, exp, availablePoints, perkPoints with startingPerkPointGranted, stats, manualAllocations, permanentBonuses,
   permanentDefenseBonuses, currentHp, currentMana, currencies) and its result read back in the port's words. Only what
   the port's record can hold is asked: whole numbers where the record's fields are whole, any double (the non-finite
   ones too) for the EXP and the bonuses, any text or none for the race. The edges by hand, then a seeded spread. */
module.exports = {
  where: "client",
  sources: ["scripts/app.js", "scripts/limitless-engine.js"],
  cases() {
    const { each, newHero, mulberry32 } = require("../shared/sheet-cases.cjs");
    const hero = newHero();
    const sheet = edit => ({
      ...hero, ...edit,
      attributes: { ...hero.attributes, ...edit.attributes }, spentPoints: { ...hero.spentPoints, ...edit.spentPoints },
      bonuses: { ...hero.bonuses, ...edit.bonuses }, purse: { ...hero.purse, ...edit.purse },
    });
    const min = -2147483648, max = 2147483647;
    const edges = [
      {},
      // The level into 1 to the cap; the EXP 0 at the cap, else 0 or more, a number that isn't finite 0.
      ...[min, -5, 0, 1, 2, 50, 99, 100, 101, 150, 200, max].map(level => ({ level, experience: 77.25 })),
      ...[-1, -0.5, -5e-324, 5e-324, 0.1, 1e300, NaN, Infinity, -Infinity].map(experience => ({ level: 12, experience })),
      { level: 100, experience: NaN }, { level: 0, experience: -3 },
      // The race through slugify.
      ...["human", "dark-elf", "Dark Elf", "  GOLIATH  ", "half_beast", "Half--Beast!", "-a--b-", "!!!", "", "İnce",
        "Kelvin K", " elf　", "\u0085elf\u0085", "﻿dwarf", "日本", "x😀y", "ΟΔΥΣ",
        "Clan 7 of 9"].map(race => ({ race })),
      // Points, coins, HP and Mana 0 or more; the attributes as they are.
      { freePoints: -3 }, { freePoints: min }, { freePoints: max }, { classPoints: -1 }, { classPoints: 0 }, { classPoints: min },
      { hp: -10 }, { hp: 0 }, { hp: min }, { hp: 99999 }, { mana: -1 }, { mana: min }, { mana: 5000 },
      { purse: { silver: -1, gold: -5, platinum: min } }, { purse: { silver: max, gold: 0, platinum: 3 } },
      { spentPoints: each((key, i) => [-1, 0, 3, min, max, -12, 1, -2][i]) },
      { attributes: each((key, i) => [-50, 0, min, max, 10, -1, 999, 7][i]) },
      { bonuses: { strength: NaN, vitality: Infinity, mind: -Infinity, dexterity: -150, intelligence: 12.5, wisdom: -0.25, speed: 1e300, luck: -1e300, armor: NaN, evasion: -Infinity } },
      { bonuses: { armor: Infinity, evasion: 33.3 } },
      // Everything out of range at once.
      {
        race: " Lizard Man ", level: -7, experience: -Infinity, freePoints: -1, classPoints: -2, hp: -3, mana: -4,
        attributes: each(() => -9), spentPoints: each(() => -5), bonuses: { strength: NaN, armor: -Infinity },
        purse: { silver: -1, gold: -1, platinum: -1 },
      },
    ];
    const random = mulberry32(606);
    const int = (low, high) => low + Math.floor(random() * (high - low + 1));
    const pick = list => list[Math.floor(random() * list.length)];
    const spread = Array.from({ length: 40 }, () => ({
      race: pick([null, "human", "goliath", "Dark Elf", "", "elf "]),
      level: pick([int(-20, 0), int(1, 100), int(99, 101), int(100, 300)]),
      experience: pick([0, int(-500, 5000), int(0, 20000) + 0.5, -0.25, NaN, Infinity]),
      freePoints: int(-20, 40), classPoints: int(-5, 10),
      attributes: each(() => int(-30, 150)), spentPoints: each(() => int(-6, 20)),
      bonuses: { ...each(() => pick([0, 0, 12.5, -40, NaN, Infinity, -Infinity, 1 / 3])), armor: pick([0, 8, NaN, -10]), evasion: pick([0, -Infinity, 15]) },
      hp: int(-100, 400), mana: int(-100, 300),
      purse: { silver: int(-200, 200), gold: int(-50, 5000), platinum: int(-5, 5) },
    }));
    return [...edges, ...spread].map(sheet);
  },
  run(input) {
    // The oracle gives the non-finite numbers as text (JSON has no form for them).
    const number = value => typeof value === "string" ? Number(value) : value;
    const keys = ["strength", "vitality", "mind", "dexterity", "intelligence", "wisdom", "speed", "luck"];
    const each = make => Object.fromEntries(keys.map(key => [key, make(key)]));
    const saved = {
      raceId: input.race, level: input.level, exp: number(input.experience), availablePoints: input.freePoints,
      perkPoints: input.classPoints, startingPerkPointGranted: true,
      stats: each(key => input.attributes[key]), manualAllocations: each(key => input.spentPoints[key]),
      permanentBonuses: each(key => number(input.bonuses[key])),
      permanentDefenseBonuses: { armor: number(input.bonuses.armor), evasion: number(input.bonuses.evasion) },
      currentHp: input.hp, currentMana: input.mana,
      currencies: { silver: input.purse.silver, gold: input.purse.gold, platinum: input.purse.platinum },
    };
    const c = normalizeCharacter(saved);
    return {
      race: c.raceId, level: c.level, experience: c.exp, freePoints: c.availablePoints, classPoints: c.perkPoints,
      attributes: each(key => c.stats[key]), spentPoints: each(key => c.manualAllocations[key]),
      bonuses: { ...each(key => c.permanentBonuses[key]), armor: c.permanentDefenseBonuses.armor, evasion: c.permanentDefenseBonuses.evasion },
      hp: c.currentHp, mana: c.currentMana,
      purse: { silver: c.currencies.silver, gold: c.currencies.gold, platinum: c.currencies.platinum },
    };
  },
};

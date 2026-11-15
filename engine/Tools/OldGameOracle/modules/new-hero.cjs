/* The new hero's sheet (GM2 step 2): the old defaultCharacter's fields the actor record holds (old-game/scripts/app.js
   1327–1374: no race, Level 1, no EXP, no free points, the starting class point, 10 in each attribute, nothing spent, no
   bonuses, HP and Mana at the maxima of those attributes, 5 gold), in the port's words. */
module.exports = {
  where: "client",
  sources: ["scripts/app.js"],
  cases() {
    return [{}];
  },
  async run() {
    const hero = defaultCharacter();
    const keys = ["strength", "vitality", "mind", "dexterity", "intelligence", "wisdom", "speed", "luck"];
    const each = make => Object.fromEntries(keys.map(key => [key, make(key)]));
    return {
      race: hero.raceId, level: hero.level, experience: hero.exp, freePoints: hero.availablePoints, classPoints: hero.perkPoints,
      attributes: each(key => hero.stats[key]), spentPoints: each(key => hero.manualAllocations[key]),
      bonuses: { ...each(key => hero.permanentBonuses[key]), armor: hero.permanentDefenseBonuses.armor, evasion: hero.permanentDefenseBonuses.evasion },
      hp: hero.currentHp, mana: hero.currentMana,
      purse: { silver: hero.currencies.silver, gold: hero.currencies.gold, platinum: hero.currencies.platinum },
    };
  },
};

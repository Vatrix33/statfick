/* CombatRules' levelling curves (old-game/scripts/combat-engine.js 89–108, a pure module; GM2 step 2): the EXP a level
   needs, the same-level victories it takes and an enemy's EXP, for every level 1–200 (Limitless reaches 200) and whole
   levels out of that range (the old functions hold them at 1; the curve stops rising at 99). */
module.exports = {
  where: "node",
  sources: ["scripts/combat-engine.js"],
  cases() {
    const levels = [];
    for (let level = 1; level <= 200; level++) levels.push(level);
    levels.push(0, -1, -50, 201, 250, 1000, 100000);
    return levels.map(level => ({ level }));
  },
  run({ level }, old) {
    const rules = old.require("scripts/combat-engine.js");
    return {
      experienceRequired: rules.experienceRequired(level),
      sameLevelVictories: rules.sameLevelVictories(level),
      enemyExperience: rules.enemyExperience(level),
    };
  },
};

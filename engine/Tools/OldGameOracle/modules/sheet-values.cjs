/* What the character sheet shows for a sheet (GM2 step 2; old-game/scripts/app.js): the base and current attributes
   (displayedBaseStat, currentStatsUncached), the permanent bonuses' fields (effectivePermanentBonus,
   effectivePermanentDefenseBonus), max HP and Mana (calculateMaxHpWithArtifacts, calculateMaxManaWithEffects), Armor
   and Evasion's base, modified and current values, HP and Mana held to their maxima, the EXP target and the cap, the
   level-up button and the Level icon's flash (updateDerivedValues, 1762–1837), the controls Dev Mode and the free
   points unlock (updateEditingControls, 1718–1748), and the EXP card's texts. For the new hero, sheets with no race,
   every race of the file at a spread of levels and attributes, Limitless's cap, the edges, and hand-made totals for the
   sources GM2 doesn't have (shared/sheet-page.js, shared/sheet-cases.cjs). */
module.exports = {
  where: "client",
  uses: ["sheet-page"],
  sources: ["scripts/app.js", "scripts/combat.js", "scripts/combat-engine.js", "scripts/limitless-engine.js", "objects/races.json", "index.html"],
  cases(old) {
    const make = require("../shared/sheet-cases.cjs");
    return [...make.plainSheets(101, 40), ...make.raceSheets(old, 202, 4), ...make.stubSheets(303, 10)];
  },
  async run(input) {
    sheetSet(input);
    return sheetValues();
  },
};

/* The races' sheet side (GM2 steps 1 and 2; step 1's race oracle, folded in here at step 2): what the old game folds of a
   race's passive for the sheet, calculateRaceEffectsUncached (old-game/scripts/app.js 1101–1164, the totals of
   emptyRaceEffects, 1058–1078, every field; the passives by their ids, their texts being the race records'), and its
   per-attribute modifier, raceStatModifier (1084–1091: the race's modifier plus all-stats-flat-per-level-up times
   level − 1). For every race of objects/races.json (its order) and the crafted races of cases/race-cases.json (after
   them, normalized by the old normalizeRaceDefinition as the 29th, 30th …), at each level and enemy count of its "fold"
   groups (levels 0–101, 150, 200; enemy counts 0, 1, 3 and −1). */
module.exports = {
  where: "client",
  sources: ["objects/races.json", "scripts/app.js"],
  setup(old) {
    return { crafted: old.cases("race-cases.json").crafted };
  },
  cases(old) {
    const list = [];
    const { fold, crafted } = old.cases("race-cases.json");
    // The ids as the old normalizeRaceDefinition makes them (slugify(id || name)).
    const slug = value => value.toLowerCase().trim().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "");
    const ids = [...old.json("objects/races.json"), ...crafted].map(race => slug(race.id || race.name));
    for (const race of ids)
      for (const group of fold)
        for (const level of group.levels) list.push({ race, level, enemyCount: group.enemyCount });
    return list;
  },
  async run(input, setup) {
    if (!setup.added) {
      if (raceCatalog.length !== 28) throw new Error("expected the 28 races of races.json, found " + raceCatalog.length);
      raceCatalog = [...raceCatalog, ...setup.crafted.map((raw, index) => normalizeRaceDefinition(raw, 28 + index))];
      setup.added = true;
    }
    if (!raceCatalog.some(race => race.id === input.race)) throw new Error("the old game has no race " + input.race);
    // The page's own state, set as the sheet holds it (the fold reads these globals).
    selectedRaceId = input.race;
    level = input.level;
    enemyCount = input.enemyCount;
    const totals = calculateRaceEffectsUncached();
    totals.passives = totals.passives.map(passive => passive.id);
    const statModifier = Object.fromEntries(statDefinitions.map(({ key }) => [key, raceStatModifier(key)]));
    return { statModifier, totals };
  },
};

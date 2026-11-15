/* The race oracle (GM2 step 1; engine/Docs/Game/Hero.md, "How it works", step 1): run in the old game's test client by
   Tools/Scripts/make-race-references.sh, which puts the case list (race-cases.json) before it as RACE_CASES. It measures
   the old game's own functions in app.js, on the races of objects/races.json and the case list's crafted races:
   calculateRaceEffectsUncached and raceStatModifier at each level and enemy count, normalizeRaceDefinition on raw races,
   applyRaceText's refusals of a whole file, raceAccent of each id. Nothing is drawn or saved but the measure, which goes
   out through the test preload's save hook (Docs/Notes.md, 2026-11-11/12). No rule here draws Math.random. */
async function raceReferences() {
  for (let i = 0; i < 600 && raceCatalog.length === 0; i++) await new Promise(resolve => setTimeout(resolve, 50));
  if (raceCatalog.length !== 28) throw new Error("expected the 28 races of races.json, found " + raceCatalog.length);
  // Numbers JSON can't hold as the strings the port's references use.
  const finite = (_key, value) => typeof value === "number" && !Number.isFinite(value)
    ? (Number.isNaN(value) ? "NaN" : value > 0 ? "Infinity" : "-Infinity") : value;
  const plain = value => JSON.parse(JSON.stringify(value, finite));
  const outcome = run => {
    try { return { value: plain(run()) }; } catch (error) { return { error: String(error && error.message) }; }
  };
  const saved = { raceCatalog, selectedRaceId, level, enemyCount };
  const fileRaces = raceCatalog;
  const crafted = RACE_CASES.crafted.map((raw, index) => normalizeRaceDefinition(raw, fileRaces.length + index));
  const fold = [];
  try {
    raceCatalog = [...fileRaces, ...crafted];
    for (const race of raceCatalog) {
      for (const group of RACE_CASES.fold) {
        for (const caseLevel of group.levels) {
          selectedRaceId = race.id;
          level = caseLevel;
          enemyCount = group.enemyCount;
          const totals = calculateRaceEffectsUncached();
          const statModifier = Object.fromEntries(statDefinitions.map(({ key }) => [key, raceStatModifier(key)]));
          fold.push({
            input: { race: race.id, level: caseLevel, enemyCount: group.enemyCount },
            output: { statModifier, totals: plain({ ...totals, passives: totals.passives.map(passive => passive.id) }) },
          });
        }
      }
    }
  } finally {
    ({ raceCatalog, selectedRaceId, level, enemyCount } = saved);
  }
  const normalize = RACE_CASES.normalize.map(entry => ({ name: entry.name, index: entry.index, ...outcome(() => normalizeRaceDefinition(entry.raw, entry.index)) }));
  // applyRaceText refuses before it changes anything; a case that it took would change the game, so none is listed.
  const catalogue = RACE_CASES.catalogue.map(entry => {
    const before = raceCatalog;
    const result = outcome(() => { applyRaceText(entry.text, "file"); return null; });
    if (raceCatalog !== before) throw new Error("a catalogue case was taken: " + entry.name);
    return { name: entry.name, ...result };
  });
  const accents = Object.fromEntries([...fileRaces.map(race => race.id), "an-unknown-race"].map(id => [id, raceAccent(id)]));
  const races = fileRaces.map(race => plain(race));
  const measure = { fold, normalize, catalogue, accents, races, crafted: crafted.map(race => plain(race)) };
  await window.statFickDesktop.saveCharacter(JSON.stringify({ stats: {}, measure }));
  return fold.length;
}

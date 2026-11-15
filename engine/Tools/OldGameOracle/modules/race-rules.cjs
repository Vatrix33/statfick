/* The races' loading rules (GM2 step 1's race oracle, folded in here at step 2; old-game/scripts/app.js): the
   normalizeRaceDefinition cases of cases/race-cases.json (2926–2954: each raw race's normalized form, or the error it
   throws), applyRaceText's refusals of a whole file (its "catalogue" cases; each must be refused, as a taken one would
   change the game), raceAccent of every race of races.json and of an unknown id (2956–2958), the 28 races as the page
   holds them, and the case list's crafted races normalized. One case per part, in that order. */
module.exports = {
  where: "client",
  sources: ["objects/races.json", "scripts/app.js"],
  setup(old) {
    const { normalize, catalogue, crafted } = old.cases("race-cases.json");
    return { normalize, catalogue, crafted };
  },
  cases() {
    return ["normalize", "catalogue", "accents", "races", "crafted"].map(part => ({ part }));
  },
  async run({ part }, setup) {
    if (raceCatalog.length !== 28) throw new Error("expected the 28 races of races.json, found " + raceCatalog.length);
    // Numbers JSON can't hold as strings (the results' rule), here inside values that are compared whole.
    const plain = value => JSON.parse(JSON.stringify(value, (key, v) => typeof v === "number" && !Number.isFinite(v) ? String(v) : v));
    const outcome = run => {
      try { return { value: plain(run()) }; } catch (error) { return { error: String(error && error.message) }; }
    };
    if (part === "normalize")
      return setup.normalize.map(entry => ({ name: entry.name, index: entry.index, ...outcome(() => normalizeRaceDefinition(entry.raw, entry.index)) }));
    if (part === "catalogue")
      return setup.catalogue.map(entry => {
        const before = raceCatalog;
        const result = outcome(() => { applyRaceText(entry.text, "file"); return null; });
        if (raceCatalog !== before) throw new Error("a catalogue case was taken: " + entry.name);
        return { name: entry.name, ...result };
      });
    if (part === "accents")
      return Object.fromEntries([...raceCatalog.map(race => race.id), "an-unknown-race"].map(id => [id, raceAccent(id)]));
    if (part === "races") return raceCatalog.map(race => plain(race));
    if (part === "crafted") return setup.crafted.map((raw, index) => plain(normalizeRaceDefinition(raw, raceCatalog.length + index)));
    throw new Error("no part " + part);
  },
};

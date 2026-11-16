/* The old game's 72 looks (GM2 step 6; Docs/Game/Hero.md, "The presets"): sprite-catalog.js's RACES, each race's four
   looks per sex, as the old game draws them (Sprites.forActor's spec: the race's base and the look merged, with the rig,
   race, sex and battle size playableSpec adds) and their labels (Sprites.lookLabel), in the catalogue's order. The
   presets are these looks rebuilt: their race, sex, label and place in the list now (the maker's look.preset records,
   waiting outside data.pak until their bodies' models land: GM2-P18), every other field for the steps that give them
   their parts and colours. The sprite scripts load in Node with a window of their own. */
const SCRIPTS = ["scripts/sprite-kit.js", "scripts/sprite-equipment.js", "scripts/sprite-humanoid.js", "scripts/sprite-creatures.js", "scripts/sprite-catalog.js"];

function sprites(old) {
  globalThis.window = globalThis;
  for (const script of SCRIPTS) old.require(script);
  return globalThis.Sprites;
}

module.exports = {
  where: "node",
  sources: SCRIPTS,
  cases(old) {
    const cases = [];
    for (const race of sprites(old).races)
      for (const sex of ["male", "female"])
        for (let index = 0; index < 4; index++) cases.push({ race, sex, index });
    return cases;
  },
  run(input, old) {
    const Sprites = sprites(old);
    return {
      label: Sprites.lookLabel(input.race, input.sex, input.index),
      spec: Sprites.forActor({ raceId: input.race, appearance: { gender: input.sex, variant: input.index } }).spec,
    };
  },
};

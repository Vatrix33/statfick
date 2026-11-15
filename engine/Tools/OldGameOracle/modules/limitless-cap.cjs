/* LimitlessRules' level cap (old-game/scripts/limitless-engine.js 10, 29, a pure module; GM2 step 2): 100, 200 while
   Limitless is on (GM15 builds Limitless; GM2 keeps the seam), with the constants the level-up reads. */
module.exports = {
  where: "node",
  sources: ["scripts/limitless-engine.js"],
  cases() {
    return [{ active: false }, { active: true }];
  },
  run({ active }, old) {
    const rules = old.require("scripts/limitless-engine.js");
    return { cap: rules.cap(active), baseCap: rules.BASE_CAP, limitlessCap: rules.LIMITLESS_CAP, pointsPerLevel: rules.POINTS_PER_LEVEL };
  },
};

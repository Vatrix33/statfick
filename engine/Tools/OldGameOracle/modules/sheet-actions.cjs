/* What the sheet's actions do (GM2 step 2; old-game/scripts/app.js): a free point spent and Dev Mode's "−" (the
   steppers, 1696–1713), the respec (2102–2114), the level up (2122–2139, with healResourcesOnLevelUp, 2029–2035), HP
   and Mana by hand (adjustResource, 2008–2019), Full HP, Full Mana and "Restore both to full" (2021–2027, 2058–2064),
   Dev Mode's fields (the attributes, the permanent bonuses, Armor's and Evasion's, the purse, the EXP form; 1676–1694,
   2037–2046, 2066–2090) and choosing a race (selectRace, 3038–3068, as the old game does it). Each case: the sheet as
   the page holds it once shown (HP and Mana held to their maxima: "settled"), then the sheet after the old handler ran
   ("after"; a refused action leaves it as it was), and for a race its status line. The buttons a case presses are
   enabled first, so the handler's own checks decide. */
module.exports = {
  where: "client",
  uses: ["sheet-page"],
  sources: ["scripts/app.js", "scripts/combat.js", "scripts/combat-engine.js", "scripts/limitless-engine.js", "objects/races.json", "index.html"],
  cases(old) {
    const make = require("../shared/sheet-cases.cjs");
    const g = make.generator(404);
    const races = old.json("objects/races.json");
    const playable = races.filter(race => race.playable !== false).map(race => race.id);
    const enemyOnly = races.filter(race => race.playable === false).map(race => race.id);
    const keys = make.KEYS;
    // Starting sheets in three pools, each action drawn from each: no race (plain and the edges), the hand-made totals
    // (stubs), every race of the data; as the values' lists make them (other seeds).
    const pools = [make.plainSheets(505, 12), make.stubSheets(707, 4), make.raceSheets(old, 606, 1)];
    const list = [];
    const add = (base, action, devMode = base.devMode) => list.push({ ...base, devMode, action });
    const fromEach = (count, makeCase) => {
      for (const pool of pools) for (let i = 0; i < count; i++) makeCase(JSON.parse(JSON.stringify(g.pick(pool))));
    };
    fromEach(20, base => {
      if (g.chance(0.4)) base.sheet.freePoints = 0;
      add(base, { kind: g.pick(["spend", "spend", "lower"]), attribute: g.pick(keys) }, g.chance(0.5));
    });
    fromEach(10, base => {
      if (g.chance(0.25)) base.sheet.spentPoints = make.each(() => 0);
      add(base, { kind: "respec" });
    });
    fromEach(30, base => {
      const required = 100 * base.sheet.level;
      if (g.chance(0.6)) base.sheet.experience = g.pick([0, required * 10, required * 40, 1e9, 1e5 + 0.5]);
      if (g.chance(0.5)) { base.sheet.hp = g.int(0, 9); base.sheet.mana = g.int(0, 9); }
      add(base, { kind: "levelUp" }, g.chance(0.5));
    });
    // The level up at the caps, below and above 100 with Limitless (no class point above 100), a race whose maxima grow.
    for (const level of [98, 99, 100, 101, 150, 199, 200]) for (const limitless of [false, true]) for (const devMode of [false, true])
      list.push({ sheet: { ...make.newHero(), level, experience: 1e7 }, devMode, ...(limitless ? { limitless } : {}), action: { kind: "levelUp" } });
    for (const race of [null, "human", "elf", "goliath", "dwarf", "undead"])
      for (const level of [4, 9, 44, 99]) list.push({ sheet: { ...make.newHero(), race, level, experience: 1e7, hp: 1, mana: 0 }, devMode: false, action: { kind: "levelUp" } });
    fromEach(16, base => {
      add(base, { kind: "changeResource", resource: g.pick(["hp", "mana"]), amount: g.pick([1, 5, 40, -1, -7, -1000, 1000, 100000]) });
    });
    fromEach(10, base => {
      if (g.chance(0.5)) { base.sheet.hp = g.int(0, 20); base.sheet.mana = g.int(0, 10); }
      add(base, g.chance(0.33) ? { kind: "restoreBoth" } : { kind: "restoreResource", resource: g.pick(["hp", "mana"]) }, true);
    });
    // HP and Mana below 0 when shown: held at 0 before the action.
    for (const action of [{ kind: "addExperience", value: 5 }, { kind: "spend", attribute: "vitality" }])
      list.push({ sheet: { ...make.newHero(), freePoints: 2, hp: -7, mana: -1 }, devMode: true, action });
    // A race that can't heal (a crafted one; undead among the data's): every action that heals.
    const noHealing = { race: make.raceOf({ healingDisabled: true, hpPerLevels: { amount: 1, interval: 2 } }) };
    for (const action of [{ kind: "levelUp" }, { kind: "changeResource", resource: "hp", amount: 5 }, { kind: "changeResource", resource: "hp", amount: -5 },
      { kind: "changeResource", resource: "mana", amount: 5 }, { kind: "restoreResource", resource: "hp" }, { kind: "restoreResource", resource: "mana" }, { kind: "restoreBoth" }])
      list.push({ sheet: { ...g.sheet({ race: "stub-race" }), experience: 1e7, hp: 10, mana: 3 }, devMode: true, stubs: noHealing, action });
    fromEach(8, base => add(base, { kind: "setAttribute", attribute: g.pick(keys), value: g.pick([15, 0, -3, 12.7, 1000, 10, -0.5]) }, true));
    fromEach(8, base => add(base, { kind: "setBonus", attribute: g.pick(keys), value: g.pick([12.5, -10, 0, 100.25, 7, 1 / 3]) }, true));
    // The bonus fields with every source's percentage in (the attribute's field leaves the race's in: the old quirk).
    const percents = { race: make.raceOf({ statPermanentPercent: { strength: 15 }, defensePermanentPercent: { armor: 8, evasion: -10 } }),
      artifacts: { statPermanentPercent: { strength: 5 } }, classes: { statPermanentPercent: { strength: 3 }, defensePermanentPercent: { armor: 4, evasion: 6 } } };
    for (const action of [{ kind: "setBonus", attribute: "strength", value: 30 }, { kind: "setDefenseBonus", defense: "armor", value: 20 }, { kind: "setDefenseBonus", defense: "evasion", value: -5 }])
      list.push({ sheet: g.sheet({ race: "stub-race" }), devMode: true, stubs: percents, action });
    fromEach(8, base => add(base, { kind: "setDefenseBonus", defense: g.pick(["armor", "evasion"]), value: g.pick([12.5, -10, 0, 33.3, 8]) }, true));
    fromEach(5, base => add(base, { kind: "setPurse", currency: g.pick(["silver", "gold", "platinum"]), value: g.pick([40, -5, 3.9, 0, 123456]) }, true));
    fromEach(7, base => add(base, { kind: "addExperience", value: g.pick([50, 0, -5, 0.5, 1e5, 2.25]) }, g.chance(0.75)));
    // Choosing a race: from none, again, another (Dev Mode only), an enemy-only one (Dev Mode only), one the data
    // doesn't have, and a hand-made one (playable or not).
    for (const devMode of [false, true]) {
      for (const race of [...playable, ...enemyOnly.slice(0, 6)]) list.push({ sheet: { ...make.newHero(), hp: 1, mana: 1 }, devMode, action: { kind: "chooseRace", race } });
      for (const [from, to] of [["human", "human"], ["human", "elf"], ["goliath", "hobbit"], ["dwarf", "troll"], ["elf", "no-such-race"]])
        list.push({ sheet: { ...g.sheet({ race: from }), hp: 1 }, devMode, action: { kind: "chooseRace", race: to } });
      list.push({ sheet: make.newHero(), devMode, action: { kind: "chooseRace", race: "no-such-race" } });
      for (const stub of [{ playable: true, maxHpPercent: 10, healingDisabled: true, statModifier: { vitality: 3 } }, { playable: false, hpPerLevels: { amount: 2, interval: 5 }, statModifier: { mind: -2 } }].map(make.raceOf)) {
        const base = g.sheet();
        list.push({ sheet: { ...base, race: null, hp: 1, mana: 1 }, devMode, stubs: { race: stub }, action: { kind: "chooseRace", race: "stub-race" } });
        // Chosen again; from a race the data doesn't have (locked all the same); to one the data doesn't have.
        list.push({ sheet: { ...base, race: "stub-race", hp: 1, mana: 1 }, devMode, stubs: { race: stub }, action: { kind: "chooseRace", race: "stub-race" } });
        list.push({ sheet: { ...base, race: "lost-race", hp: 100000, mana: 100000 }, devMode, stubs: { race: stub }, action: { kind: "chooseRace", race: "stub-race" } });
        list.push({ sheet: { ...base, race: "stub-race" }, devMode, stubs: { race: stub }, action: { kind: "chooseRace", race: "no-such-race" } });
      }
    }
    return list;
  },
  async run(input) {
    sheetSet(input);
    sheetValues();
    const settled = sheetSnapshot();
    const action = input.action;
    const click = selector => { const button = document.querySelector(selector); button.disabled = false; button.click(); };
    const type = (selector, value) => { const field = document.querySelector(selector); field.disabled = false; field.value = String(value); field.dispatchEvent(new Event("input", { bubbles: true })); };
    let status = null;
    switch (action.kind) {
      case "spend": click(`#stats-grid .stat-stepper[data-action="increase"][data-stat="${action.attribute}"]`); break;
      case "lower": click(`#stats-grid .stat-stepper[data-action="decrease"][data-stat="${action.attribute}"]`); break;
      case "respec": click("#respec-button"); break;
      case "levelUp": click("#level-up-button"); break;
      case "changeResource": adjustResource(action.resource, action.amount); break;
      case "restoreResource": restoreResource(action.resource); break;
      case "restoreBoth": click("#restore-button"); break;
      case "setAttribute": type(`#stat-${action.attribute}`, action.value); break;
      case "setBonus": type(`#stat-permanent-bonus-${action.attribute}`, action.value); break;
      case "setDefenseBonus": type(`.defense-bonus-input[data-defense="${action.defense}"]`, action.value); break;
      case "setPurse": type(`.currency-input[data-currency="${action.currency}"]`, action.value); break;
      case "addExperience": {
        document.getElementById("exp-input").value = String(action.value);
        document.getElementById("exp-form").dispatchEvent(new Event("submit", { cancelable: true }));
        break;
      }
      case "chooseRace": {
        selectRace(action.race);
        const line = document.getElementById("race-status");
        status = { text: line.textContent, state: [...line.classList].find(name => name.startsWith("is-")) || null };
        break;
      }
      default: throw new Error("no action " + action.kind);
    }
    return { settled, after: sheetSnapshot(), ...(status ? { status } : {}) };
  },
};

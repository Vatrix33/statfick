/* Port task 11, New Races (2026-11-11): Half-Beast, Goliath and playable Merfolk, their placeholder seals and looks, and
   their recruits. The design answers T1–T17 are in engine/Docs/Engine-Plan.md, "Decisions for task 11";
   old-game/Docs/Notes.md, "New Races", says where each rule lives. The three racial classes are in the port's drafts
   (T1), not in this game. One entry per part; runNewRaceChecks runs them all. */
const raceChecks = {
  counter() {
    let checks = 0;
    return { assert: (condition, message) => { if (!condition) throw Error(message); checks++; }, count: () => checks };
  },
  NEW: ["half-beast", "goliath", "merfolk"],
  // A hero of a race at a level, with classes and an optional weapon in hand; nothing else changed.
  hero({ raceId, level: heroLevel = 40, classLevels: classes = {}, weapon = null } = {}) {
    Combat.restore(null);
    const base = defaultCharacter();
    applyCharacter(normalizeCharacter({ ...base, raceId, level: heroLevel, inventory: weapon ? [weapon] : [], equipment: { ...base.equipment, ...(weapon ? { primaryHand: weapon } : {}) }, classLevels: { ...base.classLevels, ...classes } }));
    updateDerivedValues();
  },
  // Combat's own functions, from a second copy of combat.js with a hook (as the earlier tasks' checks do).
  async hooks() {
    if (window.__raceChecks) return window.__raceChecks;
    Combat.restore(null);
    const source = await (await fetch("scripts/combat.js")).text();
    const marker = "  window.Combat = {";
    const script = document.createElement("script");
    script.textContent = source.replace(marker, "  window.__raceChecks = { actorById, values, attackLoadout, weaponAttackValues, getBattle: () => battle };\n" + marker);
    document.head.append(script);
    await waitForSourceFiles();
    if (window.BattleStage) window.BattleStage.enabled = () => false;
    if (window.GameSettings) window.GameSettings.enemyDelay = () => 1e9;
    return window.__raceChecks;
  },
  // The player's unarmed attack in a fresh battle against a goblin: its loadout and its attack values.
  async unarmed(h, setup) {
    raceChecks.hero(setup);
    await Combat.startEncounter(["Goblin"], "Task 11 checks");
    const player = h.actorById("player");
    const loadout = h.attackLoadout(player);
    const attack = h.weaponAttackValues(player, loadout.primary);
    Combat.restore(null);
    return { loadout, attack };
  },
};

async function checkNewRaceData() {
  const { assert, count } = raceChecks.counter();
  const race = id => raceCatalog.find(entry => entry.id === id);
  // T1: playable, each with its icon; the six old races stay playable, so nine in all.
  assert(raceChecks.NEW.every(id => race(id)?.playable === true && race(id).icon === "objects/race-" + id + ".svg"), "Half-Beast, Goliath and Merfolk are playable, each with its seal");
  assert(raceCatalog.filter(entry => entry.playable).length === 9, "Nine playable races: the six and the three new");
  const sum = id => Object.values(race(id).statModifiers).reduce((total, value) => total + value, 0);
  // T9: balanced feral.
  assert(JSON.stringify(race("half-beast").statModifiers) === JSON.stringify({ strength: 1, vitality: 1, mind: -1, dexterity: 1, intelligence: -2, wisdom: 1, speed: 1, luck: -2 }) && sum("half-beast") === 0, "Half-Beast's stats are the balanced feral spread, adding up to 0");
  // T11: mighty and slow.
  assert(JSON.stringify(race("goliath").statModifiers) === JSON.stringify({ strength: 3, vitality: 3, mind: -2, dexterity: -2, intelligence: -1, wisdom: 0, speed: -2, luck: 1 }) && sum("goliath") === 0, "Goliath's stats are the mighty and slow spread, adding up to 0");
  // T15, T8: the Merfolk keep the enemies' stats and passive.
  assert(JSON.stringify(race("merfolk").statModifiers) === JSON.stringify({ strength: 1, vitality: 3, mind: 1, dexterity: 2, intelligence: -2, wisdom: 0, speed: -1, luck: -2 }), "Merfolk keep the stats they always had");
  const fish = race("merfolk").passive;
  assert(fish.id === "fishman-physiology" && fish.effects.some(effect => effect.element === "water" && effect.amount === -20) && fish.effects.some(effect => effect.element === "fire" && effect.amount === 10), "Merfolk keep Fishman Physiology");
  // T10, T12: the two new passives.
  assert(race("half-beast").passive.id === "natural-claws" && race("goliath").passive.id === "giants-blood", "Half-Beast has Natural Claws and Goliath Giant's Blood");
  // Each new race has an accent of its own in the race list.
  assert(raceChecks.NEW.every(id => raceAccent(id) !== raceAccent("unknown-race")), "Each new race has its own accent colour");
  // T5: the seals are placeholders, marked so for the port's remake.
  for (const id of raceChecks.NEW) {
    const text = await (await fetch("objects/race-" + id + ".svg")).text();
    assert(text.includes("<svg") && text.includes("PLACEHOLDER ART"), id + "'s seal exists and is marked as placeholder art");
  }
  return { checks: count() };
}

async function checkGiantsBlood() {
  const { assert, count } = raceChecks.counter();
  raceChecks.hero({ raceId: "goliath", level: 40 });
  const effects = calculateRaceEffects();
  assert(effects.maxHpPercent === 10 && effects.defensePermanentPercent.armor === 5 && effects.defensePermanentPercent.evasion === -10, "Giant's Blood: maximum HP +10%, Armor +5%, Evasion -10%");
  const stats = currentStats();
  const base = calculateMaxHp(stats) + calculateArtifactEffects().maxHpFlat + effects.maxHpFlat;
  assert(calculateMaxHpWithArtifacts(stats) === Math.floor(base * 1.1), "A Goliath's maximum HP is 10% above its base");
  raceChecks.hero({ raceId: "human", level: 40 });
  assert(calculateRaceEffects().maxHpPercent === 0, "Other races get no maximum HP percent from their race");
  // A Goliath recruit gets it in battle values too: against a Dwarf (the same Vitality bonus, +3, and no HP passive)
  // with the same allotted stats, its maximum HP is 10% higher.
  const goliath = normalizeCharacter({ ...defaultCharacter(), raceId: "goliath", level: 40 });
  const dwarf = normalizeCharacter({ ...defaultCharacter(), raceId: "dwarf", level: 40, stats: { ...goliath.stats } });
  const hpGoliath = Combat.previewValues(goliath).maxHp, hpDwarf = Combat.previewValues(dwarf).maxHp;
  assert(hpGoliath === Math.floor(hpDwarf * 1.1) || hpGoliath === Math.floor(hpDwarf * 1.1) + 1, "A Goliath companion has 10% more maximum HP than a Dwarf with the same Vitality");
  return { checks: count() };
}

async function checkNaturalClaws() {
  const { assert, count } = raceChecks.counter();
  const h = await raceChecks.hooks();
  // T13: claws are the Half-Beast's unarmed attacks: unarmed, 1 AP, named Claws.
  const beast = await raceChecks.unarmed(h, { raceId: "half-beast", level: 40 });
  assert(beast.loadout.primary.unarmed === true && beast.loadout.primary.name === "Claws" && beast.loadout.cost === 1, "A Half-Beast's unarmed attack is Claws, an unarmed 1-AP attack");
  const human = await raceChecks.unarmed(h, { raceId: "human", level: 40 });
  assert(human.loadout.primary.unarmed === true && human.loadout.primary.name === "Unarmed", "Other races still fight Unarmed");
  // T10: the claws grow with level: 1 base power every 10 levels.
  raceChecks.hero({ raceId: "half-beast", level: 30 });
  assert(calculateRaceEffects().unarmedPowerFlat === 3, "At Level 30 the claws have 3 more base power");
  raceChecks.hero({ raceId: "half-beast", level: 100 });
  assert(calculateRaceEffects().unarmedPowerFlat === 10, "At Level 100 the claws have 10 more base power");
  const low = await raceChecks.unarmed(h, { raceId: "half-beast", level: 30 });
  const high = await raceChecks.unarmed(h, { raceId: "half-beast", level: 100 });
  assert(high.attack.power > low.attack.power, "The same Half-Beast's claws hit harder at a higher level");
  const humanLow = await raceChecks.unarmed(h, { raceId: "human", level: 30 });
  const humanHigh = await raceChecks.unarmed(h, { raceId: "human", level: 100 });
  assert(humanHigh.attack.power === humanLow.attack.power, "A human's bare fists don't grow with level");
  // T13: every unarmed bonus applies to the claws (the Monk's here).
  const monk = await raceChecks.unarmed(h, { raceId: "half-beast", level: 40, classLevels: { monk: 1 } });
  assert(monk.attack.power > beast.attack.power, "The Monk's unarmed bonus makes the claws hit harder");
  // With a weapon in hand the weapon is used, not the claws.
  const sword = ForgeRules.encode(ForgeRules.generate({ type: "longsword", level: 40, rarity: "rare", source: "shop" }));
  const armed = await raceChecks.unarmed(h, { raceId: "half-beast", level: 40, weapon: sword });
  assert(!armed.loadout.primary.unarmed && armed.loadout.cost === 2, "A Half-Beast with a weapon in hand attacks with the weapon");
  return { checks: count() };
}

async function checkNewRaceLooks() {
  const { assert, count } = raceChecks.counter();
  // T5: four male and four female looks each, with their own names.
  for (const id of raceChecks.NEW) {
    assert(Sprites.hasPlayableLooks(id), id + " has playable looks");
    for (const gender of ["male", "female"]) {
      const labels = [0, 1, 2, 3].map(variant => Sprites.lookLabel(id, gender, variant));
      assert(labels.every(label => label && label !== "Adventurer") && new Set(labels).size === 4, id + " has four " + gender + " looks with their own names");
    }
  }
  const spec = (raceId, gender = "male", variant = 0) => Sprites.forActor({ raceId, appearance: { gender, variant } }).spec;
  // T4: a Goliath stands a head taller than a human.
  assert(spec("goliath").size > spec("human").size * 1.1, "A Goliath is clearly taller than a human");
  // T3: playable Merfolk walk on legs with fin ears; the merfolk enemies keep their fishtails.
  assert(spec("merfolk").lower !== "fishtail" && spec("merfolk").ears === "fin", "Playable Merfolk have legs and fin ears");
  assert(Sprites.forActor({ enemyName: "Reed Merfolk", raceId: "merfolk" }).spec.lower === "fishtail", "The merfolk enemies keep their fishtails");
  // T14, T10: Half-Beasts show their beast (ears and tail), claws for hands.
  assert([0, 1, 2, 3].every(variant => spec("half-beast", "male", variant).tail?.style && spec("half-beast", "female", variant).tail?.style), "Every Half-Beast look has a beast's tail");
  assert([0, 1, 2, 3].every(variant => ["beast", "beast-round"].includes(spec("half-beast", "male", variant).ears) && ["beast", "beast-round"].includes(spec("half-beast", "female", variant).ears)) && spec("half-beast").hands === "claws", "Half-Beasts have claws and beast ears");
  // The beast ears are drawn: the same figure without ears looks different, pointed or round.
  for (const variant of [0, 2]) {
    const look = spec("half-beast", "male", variant);
    assert(Sprites.renderSpec(look) !== Sprites.renderSpec({ ...look, ears: "none" }), "The beast ears show (look " + variant + ")");
  }
  assert(Sprites.playablePreview("goliath", "female", 3).includes("<svg"), "The looks draw");
  return { checks: count() };
}

async function checkNewRaceRecruits() {
  const { assert, count } = raceChecks.counter();
  raceChecks.hero({ raceId: "human", level: 40 });
  const recruitable = raceCatalog.filter(entry => entry.playable);
  // T7, T16: each new race can be a recruit, named from its own lists.
  for (const id of raceChecks.NEW) {
    const index = recruitable.findIndex(entry => entry.id === id);
    const saved = Math.random;
    Math.random = () => (index + 0.5) / recruitable.length;
    let offers;
    try { offers = Companions.makeOffers({ count: 1 }); } finally { Math.random = saved; }
    assert(offers.length === 1 && offers[0].character.raceId === id, "A recruit can be a " + id);
  }
  return { checks: count() };
}

async function checkNewRacePicker() {
  const { assert, count } = raceChecks.counter();
  raceChecks.hero({ raceId: "human", level: 40 });
  renderRaces();
  const buttons = [...document.querySelectorAll("#race-list [data-race-id]")];
  for (const id of raceChecks.NEW) {
    const button = buttons.find(entry => entry.dataset.raceId === id);
    assert(button && button.querySelector('img[src="objects/race-' + id + '.svg"]'), "The race list offers " + id + " with its seal");
  }
  return { checks: count() };
}

async function runNewRaceChecks() {
  const results = {};
  for (const check of [checkNewRaceData, checkGiantsBlood, checkNaturalClaws, checkNewRaceLooks, checkNewRaceRecruits, checkNewRacePicker]) {
    results[check.name] = await check();
  }
  Combat.restore(null);
  return results;
}

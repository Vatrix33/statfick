/* Port task 5, New Masterworks (2026-11-06): Sparkling enemies, Treasure Seeker, Moneyshot, Lament, Noble Hero, the
   Hexbane Coat and the Shadow Crystal drop. The design answers M1–M26 are in engine/Docs/Engine-Plan.md, "Decisions for
   task 5, New Masterworks"; old-game/Docs/Notes.md, "New Masterworks", says where each rule lives. One entry per part. */
const masterworkChecks = {
  counter() {
    let checks = 0;
    return { assert: (condition, message) => { if (!condition) throw Error(message); checks++; }, count: () => checks };
  },
  // Math.random answering from a list (then from the fallback), for rolls in a known order.
  dice(values, fallback = 0.5) {
    const queue = [...values];
    return () => queue.length ? queue.shift() : fallback;
  },
  // Combat's own functions, from a second copy of combat.js with a hook (as the Artifact Rework checks do).
  async hooks() {
    if (window.__masterworkChecks) return window.__masterworkChecks;
    Combat.restore(null);
    const source = await (await fetch("scripts/combat.js")).text();
    const marker = "  window.Combat = {";
    const script = document.createElement("script");
    script.textContent = source.replace(marker, "  window.__masterworkChecks = { actorById, hp, setHp, mana, setMana, values, getBattle: () => battle, attack, strike, cast, hurt, awardDefeatedEnemyExperience, sparklingMasterworkPool, rollSparklingMasterworks, incomingDamageFactor, attackLoadout, spellEffects, refresh, stageSnapshot, definitions: () => enemyDefinitions };\n" + marker);
    document.head.append(script);
    await waitForSourceFiles();
    if (window.BattleStage) window.BattleStage.enabled = () => false;
    if (window.GameSettings) window.GameSettings.enemyDelay = () => 1e9;
    // The hook copy loads its enemy definitions on its first battle.
    await Combat.startEncounter(["Goblin"], "Task 5 checks"); Combat.restore(null);
    return window.__masterworkChecks;
  },
  hero({ level: heroLevel = 40, inventory: pack = [], equipment: slots = {}, spells = [], companion = null } = {}) {
    Combat.restore(null);
    applyCharacter(normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: heroLevel, inventory: [...pack], equipment: { ...defaultCharacter().equipment, ...slots }, knownSpells: spells }));
    companions = companion ? [{ id: "companion-masterwork-test", name: "Mira", gender: "female", character: { ...defaultCharacter(), raceId: "human", level: heroLevel, inventory: [...(companion.inventory || [])], equipment: { ...defaultCharacter().equipment, ...(companion.equipment || {}) } } }] : [];
  },
  async battle(h, enemies, options = {}, sparkling = []) {
    masterworkChecks.hero(options);
    if (!await Combat.startEncounter(enemies, "Task 5 test", options.objective || null, sparkling)) throw Error("The task 5 test battle starts");
    const battle = h.getBattle();
    return { battle, player: h.actorById("player"), foes: battle.actors.filter(actor => actor.team === "enemy") };
  },
  forged: (key, atLevel = 40) => ForgeRules.encode(ForgeRules.generateMasterwork({ key, level: atLevel })),
  // A forged item's effects, swapped for a moment (getItem hands out one cached object per id).
  withoutEffect(itemId, kind, action) {
    const item = getItem(itemId), saved = item.effects;
    item.effects = saved.filter(effect => effect.kind !== kind);
    try { return action(); } finally { item.effects = saved; }
  },
};

// M2–M4, M9: who can be Sparkling and how likely: 0.1% × (1 + Luck/500) up to double; a Cull's target inside its
// circle 2% × the same; never where a boss fights, never an object; rolled when an encounter appears and saved with it.
async function checkSparklingChance() {
  const { assert, count } = masterworkChecks.counter();
  const h = await masterworkChecks.hooks();
  await masterworkChecks.battle(h, ["Goblin"]); Combat.restore(null);
  const random = Math.random;
  try {
    for (const base of [0, 40, 400, 900]) {
      stats.luck = base;
      const luck = Math.max(0, currentStats().luck), expected = 0.1 * Math.min(2, 1 + luck / 500);
      assert(Math.abs(Combat.sparklingChance("Goblin") - expected) < 1e-9, `At Luck ${luck} the chance is ${expected}% (${Combat.sparklingChance("Goblin")})`);
      assert(Math.abs(Combat.sparklingChance("Goblin", "Goblin") - expected * 20) < 1e-9, `At Luck ${luck} a Cull's target inside its circle has ${expected * 20}%`);
    }
    stats.luck = 400;
    assert(Combat.sparklingChance("Goblin") > 0.1 && Combat.sparklingChance("Goblin") < 0.2, "Luck raises it a little");
    stats.luck = 2000;
    assert(Combat.sparklingChance("Goblin") === 0.2 && Combat.sparklingChance("Goblin", "Goblin") === 4, "At most double: 0.2%, and 4% for a Cull's target");
    // The roll: each enemy on its own, against its chance.
    stats.luck = 0;
    const threshold = Combat.sparklingChance("Goblin") / 100;
    Math.random = masterworkChecks.dice([threshold * 0.99, threshold * 1.01, threshold * 0.5]);
    assert(JSON.stringify(Combat.rollSparkling(["Goblin", "Goblin", "Orc"])) === "[0,2]", "Each enemy rolls its own chance");
    Math.random = () => 0;
    assert(!Combat.rollSparkling(["Tankina, Disciple of Talos", "Apostle of Talos", "Apostle of Talos"]).length, "Nobody sparkles where a boss fights, not even the boss's aids");
    assert(!Combat.rollSparkling(["Ancient Lich Ool"]).length && !Combat.rollSparkling(["Phylactery of Ool"]).length, "Bosses and objects never sparkle");
    assert(!Combat.rollSparkling(["No Such Enemy"]).length, "An unknown enemy never sparkles");
    // The Cull's target inside the circle (M4): Guild.cullTargetHere names it there, and only there.
    const point = { x: 1000, y: 500 };
    worldState.regionId = "alder-vale"; WorldRules.setActiveRegion("alder-vale");
    worldState.guild.quest = { version: 2, type: "cull", enemy: "Goblin", zone: "Plains", point, radius: 40, required: 5, progress: 0, complete: false, regionId: "alder-vale", originRegionId: "alder-vale", originCastleId: "alderkeep", reward: { silver: 100, reputation: 10 } };
    worldState.position = { x: point.x + 10, y: point.y };
    assert(Guild.cullTargetHere() === "Goblin", "Inside the circle the Cull's target is known");
    Math.random = masterworkChecks.dice([0.019, 0.019]);
    assert(JSON.stringify(Combat.rollSparkling(["Goblin", "Orc"])) === "[0]", "Inside the circle the target rolls 2%, others 0.1%");
    worldState.position = { x: point.x + 60, y: point.y };
    assert(Guild.cullTargetHere() === null, "Outside the circle it is not");
    Math.random = () => 0.019;
    assert(!Combat.rollSparkling(["Goblin"]).length, "Outside the circle the target rolls the normal 0.1%");
    worldState.guild.quest.complete = true; worldState.position = { ...point };
    assert(Guild.cullTargetHere() === null, "A finished Cull no longer raises it");
    worldState.guild.quest = null;
    // When an encounter appears, its Sparkling enemies are rolled and kept with it, also in a save.
    Combat.restore(null); currentHp = 100; worldState.pendingEncounter = null; worldState.pendingRegionTransition = null; worldState.encounterTravelMinutes = 0;
    worldState.position = { x: 1000, y: 500 };
    Math.random = () => 0;
    assert(WorldMap.checkTravelEncounter(WorldRules.ENCOUNTER_MINUTES), "A travel encounter appears");
    Math.random = random;
    const pending = worldState.pendingEncounter;
    assert(pending && JSON.stringify(pending.sparkling) === JSON.stringify(pending.enemies.map((_, index) => index)), `Its Sparkling enemies are rolled when it appears (${JSON.stringify(pending?.sparkling)})`);
    const saved = normalizeCharacter({ ...defaultCharacter(), worldState: { ...copyForSave(worldState), pendingEncounter: { ...pending, sparkling: [0, 0, 7, -1, "x"] } } });
    assert(JSON.stringify(saved.worldState.pendingEncounter.sparkling) === "[0]", `A save keeps only valid ones (${JSON.stringify(saved.worldState.pendingEncounter.sparkling)})`);
    worldState.pendingEncounter = null;
  } finally { Math.random = random; worldState.pendingEncounter = null; if (worldState.guild) worldState.guild.quest = null; }
  return count();
}
const copyForSave = value => JSON.parse(JSON.stringify(value));

// M5, M6: a Sparkling enemy is named and shown so, its attributes ×1.4 and then its maximum HP doubled; never in a boss's
// battle; Empowered stacks; a saved battle keeps it; escaping sees its Speed; a Cull counts it.
async function checkSparklingEnemies() {
  const { assert, count } = masterworkChecks.counter();
  const h = await masterworkChecks.hooks();
  let { battle, foes } = await masterworkChecks.battle(h, ["Orc", "Orc"], {}, [1]);
  const [plain, shining] = foes;
  assert(!plain.sparkling && plain.name === "Orc 1" && shining.sparkling && shining.name === "Sparkling Orc 2", `The second Orc sparkles (${foes.map(foe => foe.name)})`);
  const definition = h.definitions().find(enemy => enemy.name === "Orc");
  for (const [key, value] of Object.entries(definition.character.stats)) assert(shining.character.stats[key] === Math.floor(value * 1.4) && plain.character.stats[key] === value, `Its ${key} is ×1.4`);
  // Maximum HP: what those attributes give, doubled.
  const twin = { ...shining, id: "sparkling-twin", sparkling: false, resourceMax: undefined };
  const undoubled = h.values(twin, battle).maxHp;
  assert(Math.abs(shining.resourceMax.hp - undoubled * 2) <= 1 && h.hp(shining) === shining.resourceMax.hp, `Its maximum HP is doubled on top (${undoubled} → ${shining.resourceMax.hp})`);
  assert(shining.resourceMax.hp > plain.resourceMax.hp * 2.5, "So it has far more HP than a plain one");
  // How it shows: stage snapshot, classic card, encounter screen.
  const snapshot = h.stageSnapshot([]);
  assert(snapshot.actors.find(actor => actor.id === shining.id)?.sparkling === true && !snapshot.actors.find(actor => actor.id === plain.id)?.sparkling, "The battle stage marks it");
  const card = [...document.querySelectorAll(".combat-enemy")].find(button => button.dataset.combatTarget === shining.id);
  assert(card?.classList.contains("is-sparkling") && ![...document.querySelectorAll(".combat-enemy.is-sparkling")].some(button => button.dataset.combatTarget === plain.id), "Its card is marked Sparkling");
  // A saved battle keeps it.
  Combat.restore(Combat.serialize());
  battle = h.getBattle();
  assert(battle.actors.find(actor => actor.id === shining.id)?.sparkling === true && battle.actors.find(actor => actor.id === shining.id).resourceMax.hp === shining.resourceMax.hp, "A restored battle keeps the Sparkling enemy");
  // Never in a battle with a boss, never an object, never out of range.
  ({ foes } = await masterworkChecks.battle(h, ["Tankina, Disciple of Talos", "Apostle of Talos", "Apostle of Talos"], {}, [0, 1, 2]));
  assert(!foes.some(foe => foe.sparkling || /Sparkling/.test(foe.name)), "Not where a boss fights");
  ({ foes } = await masterworkChecks.battle(h, ["Orc"], {}, [3, -1, "0"]));
  assert(!foes[0].sparkling, "Indexes outside the roster do nothing");
  // Empowered and Sparkling together: ×1.3 then ×1.4.
  ({ foes } = await masterworkChecks.battle(h, ["Orc"], { objective: "hunt" }, [0]));
  assert(foes[0].name === "Empowered Sparkling Orc" && foes[0].character.stats.strength === Math.floor(Math.floor(definition.character.stats.strength * 1.3) * 1.4), `An Empowered target can sparkle too (${foes[0].name})`);
  // A Cull counts a Sparkling target as its target.
  ({ foes } = await masterworkChecks.battle(h, ["Orc"], {}, [0]));
  worldState.guild.quest = { version: 2, type: "cull", enemy: "Orc", zone: "Plains", point: { ...worldState.position }, radius: 40, required: 5, progress: 0, complete: false, regionId: worldState.regionId, originRegionId: worldState.regionId, originCastleId: "alderkeep", reward: { silver: 100, reputation: 10 } };
  Guild.onEnemyDefeated(foes[0]);
  assert(worldState.guild.quest.progress === 1, "A Sparkling target counts for its Cull");
  worldState.guild.quest = null;
  // Escaping before the fight sees its Speed ×1.4.
  masterworkChecks.hero();
  const normal = await Combat.attemptTravelEscape(["Orc"], 25), quick = await Combat.attemptTravelEscape(["Orc"], 25, null, [0]);
  assert(quick.fastestEnemySpeed > normal.fastestEnemySpeed, `A Sparkling enemy is harder to escape (${normal.fastestEnemySpeed} → ${quick.fastestEnemySpeed} Speed)`);
  Combat.restore(null);
  return count();
}

// M5: the encounter screen lists a Sparkling enemy on its own line, led by a gold "Sparkling".
async function checkSparklingEncounterScreen() {
  const { assert, count } = masterworkChecks.counter();
  await masterworkChecks.hooks();
  masterworkChecks.hero();
  worldState.regionId = "alder-vale"; WorldRules.setActiveRegion("alder-vale"); worldState.position = { x: 1000, y: 500 };
  WorldMap.setAmbientEnabled(false); WorldMap.open();
  try {
    worldState.pendingEncounter = { zone: "Plains", enemies: ["Goblin", "Goblin", "Goblin"], objective: null, sparkling: [1] };
    WorldMap.render();
    const rows = [...document.querySelectorAll("#map-encounter-enemies li")];
    const shining = rows.filter(row => row.classList.contains("is-sparkling"));
    assert(rows.length === 2 && shining.length === 1, `One plain line and one Sparkling line (${rows.map(row => row.textContent)})`);
    assert(shining[0].querySelector(".sparkling-tag")?.textContent === "Sparkling" && /Goblin/.test(shining[0].textContent) && /×1/.test(shining[0].textContent), "The Sparkling line names it");
    assert(rows.find(row => !row.classList.contains("is-sparkling"))?.textContent.includes("×2"), "The others are counted on their own line");
    worldState.pendingEncounter = { zone: "Plains", enemies: ["Goblin"], objective: null, sparkling: [] };
    WorldMap.render();
    assert(!document.querySelector("#map-encounter-enemies .is-sparkling, #map-encounter-enemies .sparkling-tag"), "Without one, nothing sparkles");
    // Fight: the battle has the Sparkling enemies the screen showed.
    worldState.pendingEncounter = { zone: "Plains", enemies: ["Goblin", "Goblin"], objective: null, sparkling: [1] };
    WorldMap.render();
    document.getElementById("map-encounter-fight").click();
    for (let i = 0; i < 50 && !Combat.active(); i++) await new Promise(resolve => setTimeout(resolve, 50));
    const fought = Combat.serialize()?.actors.filter(actor => actor.team === "enemy") || [];
    assert(fought.length === 2 && !fought[0].sparkling && fought[1].sparkling && fought[1].name === "Sparkling Goblin 2", `Fight starts the battle with them (${fought.map(actor => actor.name)})`);
    Combat.restore(null);
  } finally { worldState.pendingEncounter = null; document.querySelector("#map-modal [data-map-close]")?.click(); }
  return count();
}

// M7, M8, M10–M12: 11 times the EXP; two masterwork rolls, 70% and 5% plus the masterwork Luck bonus; every masterwork in
// the pool, Moneyshot and Treasure Seeker five times as likely; forged at your level.
async function checkSparklingRewards() {
  const { assert, count } = masterworkChecks.counter();
  const h = await masterworkChecks.hooks();
  const random = Math.random;
  try {
    // EXP: the same Orc, plain and Sparkling.
    const earned = async sparkling => {
      const { battle, foes } = await masterworkChecks.battle(h, ["Orc"], {}, sparkling);
      Math.random = () => 0.999;
      h.setHp(foes[0], 0); h.awardDefeatedEnemyExperience();
      Math.random = random;
      return { exp: battle.experienceEarned, money: battle.moneyEarned };
    };
    const plain = await earned([]), shining = await earned([0]);
    assert(shining.exp === plain.exp * 11 && shining.money === plain.money, `11 times the EXP, the same silver (${plain.exp} → ${shining.exp}; ${plain.money} → ${shining.money})`);
    // The pool: every masterwork weapon, armor and artifact; Moneyshot and Treasure Seeker weighted 5.
    const pool = h.sparklingMasterworkPool();
    const ids = pool.map(entry => entry.id);
    for (const key of ForgeRules.MASTERWORK_IDS) assert(ids.includes("masterwork-" + key), `${key} is in the pool`);
    const artifacts = items.filter(item => item.category === "artifact" && item.rarity === "masterwork").map(item => item.id);
    assert(artifacts.length === 5 && artifacts.every(id => ids.includes(id)), "Every masterwork artifact is in the pool");
    assert(pool.length === ForgeRules.MASTERWORK_IDS.length + 5 && !ids.some(id => /key-item/.test(id)), `Nothing else (${pool.length})`);
    for (const key of ["moneyshot", "lament", "noble-hero", "hexbane-coat"]) assert(ForgeRules.MASTERWORK_IDS.includes(key), `${key} is a masterwork`);
    const weight = id => pool.find(entry => entry.id === id)?.weight;
    assert(weight("masterwork-moneyshot") === 5 && weight("artifact-treasure-seeker-artifact") === 5 && pool.filter(entry => entry.weight === 1).length === pool.length - 2, "Moneyshot and Treasure Seeker weigh 5, every other masterwork 1");
    const total = pool.reduce((sum, entry) => sum + entry.weight, 0);
    assert(Math.abs(5 / total - 0.111) < 0.01, `Each of the two is about 11% of the drops (${(500 / total).toFixed(1)}%)`);
    // The rolls: 70% and 5%, raised by 0.05 × Luck; the first roll then the pick, the second roll then its pick.
    const { battle, foes } = await masterworkChecks.battle(h, ["Orc"], { level: 30 }, [0]);
    stats.luck = 0;
    const luckBonus = Math.max(0, currentStats().luck) * 0.05;
    const roll = values => { Math.random = masterworkChecks.dice(values); try { return h.rollSparklingMasterworks(foes[0]); } finally { Math.random = random; } };
    // The game rolls both chances first, then picks a masterwork for each that hit.
    assert(roll([(70 + luckBonus) / 100 - 0.001, 0.999, 0]).length === 1, "Under 70% (plus Luck): one masterwork");
    assert(roll([(70 + luckBonus) / 100 + 0.001, 0.999]).length === 0, "Over it: none");
    assert(roll([0.999, (5 + luckBonus) / 100 - 0.001, 0]).length === 1, "The 5% roll alone gives one");
    assert(roll([0.999, (5 + luckBonus) / 100 + 0.001]).length === 0, "Over 5% (plus Luck): none");
    assert(roll([0, 0, (5 + luckBonus) / 100 - 0.001, 0.999]).length === 2, "Both rolls: two");
    stats.luck = 200;
    const raised = Math.max(0, currentStats().luck) * 0.05;
    assert(raised > 5 && roll([(70 + raised) / 100 - 0.001, 0.999, 0]).length === 1 && roll([0.999, (5 + raised) / 100 - 0.001, 0]).length === 1, `Luck raises both rolls by ${raised} points`);
    assert(roll([(70 + raised) / 100 + 0.001, 0.999]).length === 0 && roll([0.999, (5 + raised) / 100 + 0.001]).length === 0, "…and by no more");
    stats.luck = 0;
    assert(!h.rollSparklingMasterworks(foes.map(foe => ({ ...foe, sparkling: false }))[0]).length, "A plain enemy rolls none");
    // The pick: weighted by the pool, the first entry at 0 and the last at the end.
    assert(JSON.stringify(roll([0, 0.999, 0])) === JSON.stringify([pool[0].id]) && JSON.stringify(roll([0, 0.999, 0.9999])) === JSON.stringify([pool.at(-1).id]), "The pick walks the weighted pool");
    // Into the loot, forged at your level (Level 30), not the enemy's: every roll hits, both picks take the first entry.
    const firstKey = ForgeRules.masterworkTemplateKey(pool[0].id), forgedFirst = new RegExp(`^weapon-masterwork-${firstKey}-30-`);
    const lootOf = async sparkling => {
      const fight = await masterworkChecks.battle(h, ["Orc"], { level: 30 }, sparkling);
      h.setHp(fight.foes[0], 0);
      Math.random = () => 0;
      try { h.awardDefeatedEnemyExperience(); } finally { Math.random = random; }
      return fight.battle.lootItems;
    };
    const shiningLoot = await lootOf([0]), plainLoot = await lootOf([]);
    assert(shiningLoot.filter(id => forgedFirst.test(id)).length === 2 && !plainLoot.some(id => forgedFirst.test(id)), `A Sparkling enemy's masterworks go into the loot, forged at your level (${shiningLoot.filter(id => /masterwork/.test(id))})`);
  } finally { Math.random = random; Combat.restore(null); }
  return count();
}

// M13–M17: Treasure Seeker: on the level ramp (+15% / 0.15 Luck at Level 1 to +100% / 1 at 80); money from battles,
// quests and sales raised once for the party (sales never above the shop price); Luck per Platinum held.
async function checkTreasureSeeker() {
  const { assert, count } = masterworkChecks.counter();
  const h = await masterworkChecks.hooks();
  const id = "artifact-treasure-seeker-artifact";
  const at = level => getItem(`${id}-lv${level}`);
  assert(at(1).effects.map(effect => effect.amount).join() === "15,0.15" && getItem(id).effects.map(effect => effect.amount).join() === "100,1" && at(100).effects.map(effect => effect.amount).join() === "121.5,1.22", "Its numbers ramp from 15% and 0.15 at Level 1 to 100% and 1 at Level 80, rising after");
  assert(/15% more money/.test(at(1).description) && /by 0\.15 for every Platinum/.test(at(1).description) && at(1).priceSilver === 2000 && getItem(id).priceSilver === 36000, "Its card shows the numbers at its level");
  assert(getItem(id).rarity === "masterwork" && getItem(id).shopAvailable === false && getItem(id).automaticDrop === false && getItem(id).placeholderArt === true, "A masterwork artifact sold nowhere, placeholder art");
  assert(ForgeArt.artifact(getItem(id)) !== ForgeArt.artifact(getItem("artifact-second-wind-artifact")), "It has its own look");
  // Luck: +1 per whole Platinum held (Level 80), +0.15 at Level 1.
  masterworkChecks.hero({ inventory: [id], equipment: { artifact1: id } });
  currencies = ShopRules.walletFromValue(255000);
  assert(calculateArtifactEffects().statFlat.luck === 25, `25.5 Platinum held: +25 Luck (${calculateArtifactEffects().statFlat.luck})`);
  masterworkChecks.hero({ inventory: [`${id}-lv1`], equipment: { artifact1: `${id}-lv1` } });
  currencies = ShopRules.walletFromValue(1000000);
  assert(calculateArtifactEffects().statFlat.luck === 15, `A Level 1 copy: 100 Platinum give +15 Luck (${calculateArtifactEffects().statFlat.luck})`);
  masterworkChecks.hero();
  assert(calculateArtifactEffects().statFlat.luck === 0 && treasureMoneyPercent() === 0, "Without it nothing changes");
  // The party's best copy counts, once.
  masterworkChecks.hero({ inventory: [`${id}-lv1`], equipment: { artifact1: `${id}-lv1` }, companion: { inventory: [id], equipment: { artifact1: id } } });
  assert(treasureMoneyPercent() === 100, "A companion's copy counts; the best one in the party");
  masterworkChecks.hero({ inventory: [id], equipment: { artifact1: id }, companion: { inventory: [id], equipment: { artifact1: id } } });
  assert(treasureMoneyPercent() === 100, "Two copies don't add up");
  // Battle silver ×2.
  const silver = async wear => {
    const { battle, foes } = await masterworkChecks.battle(h, ["Orc"], wear ? { inventory: [id], equipment: { artifact1: id } } : {});
    h.setHp(foes[0], 0); const random = Math.random; Math.random = () => 0.999;
    try { h.awardDefeatedEnemyExperience(); } finally { Math.random = random; }
    return battle.moneyEarned;
  };
  const plainSilver = await silver(false), richSilver = await silver(true);
  assert(richSilver === plainSilver * 2 || richSilver === plainSilver * 2 - 1, `Battle silver doubles (${plainSilver} → ${richSilver})`);
  Combat.restore(null);
  // Sales: doubled, never above the shop price.
  const sword = items.find(item => item.shopAvailable !== false && item.category !== "artifact" && Number.isFinite(item.priceSilver) && item.priceSilver >= 100 && item.playerSellable !== false);
  masterworkChecks.hero();
  const plainSale = ShopLayout.salePrice(sword);
  masterworkChecks.hero({ inventory: [id], equipment: { artifact1: id } });
  const richSale = ShopLayout.salePrice(sword);
  assert(richSale === Math.min(sword.priceSilver, plainSale * 2) && richSale > plainSale, `Sales double (${plainSale} → ${richSale}, shop price ${sword.priceSilver})`);
  assert(ShopLayout.salePrice(sword, 50) === sword.priceSilver && ShopLayout.salePrice(sword, 50, 0) === Math.floor(Math.floor(sword.priceSilver / 2) * 1.5), "A sale never pays more than the shop price");
  const wallet = ShopRules.walletValue(currencies);
  inventory.push(sword.id);
  const sold = ShopLayout.sellFromPack([{ id: sword.id, quantity: 1 }], () => true);
  assert(sold.price === richSale && ShopRules.walletValue(currencies) - wallet === richSale, "Selling pays the raised price");
  // Quest rewards.
  const reward = { silver: 1000, reputation: 10 };
  const pay = () => {
    worldState.regionId = "alder-vale"; WorldRules.setActiveRegion("alder-vale");
    const keep = WorldRules.settlementsForRegion("alder-vale").find(place => place.id === "alderkeep");
    worldState.position = { x: keep.x, y: keep.y }; Guild.onPositionChange();
    worldState.guild.quest = { version: 2, type: "travel", title: "Test", progress: 1, complete: true, reward, regionId: "alder-vale", originRegionId: "alder-vale", originCastleId: "alderkeep", destination: { regionId: "alder-vale", settlementId: "alderkeep" } };
    const before = ShopRules.walletValue(currencies);
    Guild.useCounter(); Guild.close?.();
    return ShopRules.walletValue(currencies) - before;
  };
  masterworkChecks.hero();
  const plainPay = pay();
  masterworkChecks.hero({ inventory: [id], equipment: { artifact1: id } });
  const richPay = pay();
  assert(plainPay === 1000 && richPay === 2000, `Quest rewards double (${plainPay} → ${richPay})`);
  masterworkChecks.hero({ inventory: [`${id}-lv1`], equipment: { artifact1: `${id}-lv1` } });
  assert(pay() === 1150, "A Level 1 copy adds 15%");
  companions = [];
  return count();
}

// M16, M18: Moneyshot: an Outlaw longbow; every attack with it counts, hits and misses; each fifth of a battle deals
// +50% (+200% instead when critical) and echoes 5% of the damage to every other enemy, their defenses applying.
async function checkMoneyshot() {
  const { assert, count } = masterworkChecks.counter();
  const h = await masterworkChecks.hooks();
  const bow = masterworkChecks.forged("moneyshot");
  const item = getItem(bow);
  assert(item.name.endsWith("Moneyshot") && item.forged.type === "longbow" && item.forged.parts[0].maker === "outlaw" && item.effects.some(effect => effect.kind === "fifth-attack-boost" && effect.amount === 50 && effect.every === 5 && effect.criticalAmount === 200 && effect.echoPercent === 5), "An Outlaw longbow with its fifth-attack effect");
  const random = Math.random;
  const volley = async (dice, attacks = 5) => {
    const { battle, player, foes } = await masterworkChecks.battle(h, ["Orc", "Orc", "Orc"], { inventory: [bow], equipment: { primaryHand: bow } });
    stats.dexterity = 400; foes.forEach(foe => h.setHp(foe, 1e7));
    const dealt = [], others = [];
    for (let i = 0; i < attacks; i++) {
      battle.current = "player"; battle.ap = 9;
      const before = foes.map(foe => h.hp(foe));
      Math.random = dice(i);
      try { h.attack(player, foes[0]); } finally { Math.random = random; }
      dealt.push(before[0] - h.hp(foes[0])); others.push(foes.slice(1).map((foe, index) => before[index + 1] - h.hp(foe)));
    }
    return { dealt, others, player, foes, battle };
  };
  // Hits, no criticals (0.5 hits; the crit roll needs less than the chance): the fifth deals 50% more and echoes.
  const plain = await volley(() => () => 0.5);
  const base = plain.dealt[0];
  assert(plain.dealt.slice(0, 4).every(amount => amount === base) && Math.abs(plain.dealt[4] - base * 1.5) <= 1, `Every fifth attack deals 50% more (${plain.dealt})`);
  assert(plain.others.slice(0, 4).every(list => list.every(amount => amount === 0)), "The others take nothing from the first four");
  const echoes = plain.others[4];
  assert(echoes.every(amount => amount > 0 && amount <= Math.ceil(plain.dealt[4] * 0.05)), `The fifth echoes up to 5% of its damage to each other enemy (${echoes} of ${plain.dealt[4]})`);
  assert(echoes.every(amount => amount < Math.floor(plain.dealt[4] * 0.05)), `Their defenses reduce the echo (${echoes} under ${Math.floor(plain.dealt[4] * 0.05)})`);
  assert(plain.player.fifthAttackCount === 5, "Five attacks counted");
  // Their defenses apply: an enemy with more Armor takes less of the echo.
  // Misses count: two misses, then three hits, the fifth attack is boosted.
  const missing = await volley(i => () => i < 2 ? 0.9999 : 0.5);
  assert(missing.dealt[0] === 0 && missing.dealt[1] === 0 && Math.abs(missing.dealt[4] - missing.dealt[2] * 1.5) <= 1, `Misses count toward the fifth (${missing.dealt})`);
  // A critical fifth: +200% instead of +50%.
  const critical = await volley(() => () => 0);
  assert(Math.abs(critical.dealt[4] - critical.dealt[0] * 3) <= 2, `A critical fifth attack deals 200% more instead (${critical.dealt})`);
  // The count starts over each battle and goes on past five.
  const ten = await volley(() => () => 0.5, 10);
  assert(Math.abs(ten.dealt[9] - ten.dealt[0] * 1.5) <= 1 && ten.dealt[5] === ten.dealt[0], `The tenth is boosted too, the sixth isn't (${ten.dealt})`);
  const fresh = await volley(() => () => 0.5, 1);
  assert(fresh.player.fifthAttackCount === 1, "A new battle starts the count over");
  // Another longbow has none of it.
  const other = ForgeRules.encode(ForgeRules.generate({ type: "longbow", level: 40, source: "shop", random: () => 0.5 }));
  const { battle, player, foes } = await masterworkChecks.battle(h, ["Orc", "Orc"], { inventory: [other], equipment: { primaryHand: other } });
  foes.forEach(foe => h.setHp(foe, 1e7));
  const shots = [];
  for (let i = 0; i < 5; i++) { battle.current = "player"; battle.ap = 9; const before = h.hp(foes[0]); Math.random = () => 0.5; try { h.attack(player, foes[0]); } finally { Math.random = random; } shots.push(before - h.hp(foes[0])); }
  assert(new Set(shots).size === 1 && h.hp(foes[1]) === 1e7, "Another longbow's fifth attack is like the rest");
  Combat.restore(null);
  return count();
}

// M19, M20: Lament: an Elven wand with a Greater water gem; a single-target spell at an enemy or an ally can take a
// second target for 50% more Mana, chosen with a click; the second target gets the targeted effects only, with its own
// miss roll. 2% from the Tide Siren.
async function checkLament() {
  const { assert, count } = masterworkChecks.counter();
  const h = await masterworkChecks.hooks();
  const wand = masterworkChecks.forged("lament");
  const item = getItem(wand);
  assert(item.forged.type === "wand" && item.forged.parts[0].maker === "elven" && /aquamarine/.test(item.forged.parts[0].material) && item.forged.parts[0].tier === 3, `An Elven wand with a Greater water gem (${JSON.stringify(item.forged.parts[0])})`);
  assert(h.definitions().find(enemy => enemy.name === "Tide Siren").drops.some(drop => drop.itemId === "masterwork-lament" && drop.chance === 2), "The Tide Siren drops it at 2%");
  const random = Math.random;
  const bolt = getSpell("water-bolt-water-damage");
  const setup = async (wear = true, enemies = ["Orc", "Orc", "Orc"], spells = ["water-bolt-water-damage"]) => {
    const fight = await masterworkChecks.battle(h, enemies, wear ? { inventory: [wand], equipment: { primaryHand: wand }, spells } : { spells });
    h.setMana(fight.player, 5000); fight.foes.forEach(foe => h.setHp(foe, 1e6)); fight.battle.current = "player"; fight.battle.ap = 9; fight.battle.targetId = fight.foes[0].id;
    return fight;
  };
  try {
    Math.random = () => 0.5;
    // Two targets for 50% more Mana.
    let { player, foes } = await setup();
    let manaBefore = h.mana(player);
    assert(h.cast(player, foes[0], bolt), "A plain cast");
    const plainCost = manaBefore - h.mana(player);
    ({ player, foes } = await setup());
    manaBefore = h.mana(player);
    assert(h.cast(player, foes[0], bolt, null, { extraTargetId: foes[1].id }), "A cast with a second target");
    assert(manaBefore - h.mana(player) === Math.floor(plainCost * 1.5), `It costs 50% more Mana (${plainCost} → ${manaBefore - h.mana(player)})`);
    assert(h.hp(foes[0]) < 1e6 && h.hp(foes[1]) < 1e6 && h.hp(foes[2]) === 1e6, "Both targets are struck, the third is not");
    // Its own miss roll: the first hit, the second missed.
    ({ player, foes } = await setup());
    let calls = 0;
    Math.random = () => { calls++; return 0.5; };
    h.cast(player, foes[0], bolt, null, { extraTargetId: foes[1].id });
    Math.random = random;
    assert(calls >= 2, "The second target rolls on its own");
    // The second target's own miss roll: one that spells always miss is missed, while the first is struck.
    ({ player, foes } = await setup());
    foes[1].effects.push({ key: "test-blur", name: "Blur", type: "incoming-spell-miss-chance", amount: 100, expiresTurn: 99 });
    h.cast(player, foes[0], bolt, null, { extraTargetId: foes[1].id });
    assert(h.hp(foes[0]) < 1e6 && h.hp(foes[1]) === 1e6 && h.getBattle().log.some(entry => /\(Lament\) misses/.test(entry)), "The second target can be missed on its own roll");
    // Refused: the same target, the wrong side, a fallen one, an area spell, without Lament.
    ({ player, foes } = await setup());
    Math.random = () => 0.5;
    assert(!h.cast(player, foes[0], bolt, null, { extraTargetId: foes[0].id }), "Not the same target twice");
    assert(!h.cast(player, foes[0], bolt, null, { extraTargetId: "player" }), "Not an ally for a spell at an enemy");
    h.setHp(foes[2], 0);
    assert(!h.cast(player, foes[0], bolt, null, { extraTargetId: foes[2].id }), "Not a fallen enemy");
    const wave = spells.find(spell => h.spellEffects(spell).every(effect => !effect.recipient || effect.recipient === "all-enemies") && h.spellEffects(spell).some(effect => effect.recipient === "all-enemies" && effect.type === "damage"));
    masterworkChecks.hero({ inventory: [wand], equipment: { primaryHand: wand }, spells: [wave.id], companion: {} });
    await Combat.startEncounter(["Orc", "Orc"], "Lament area");
    companions = [];
    let area = h.getBattle(), caster = h.actorById("player"), friend = area.actors.find(actor => actor.companion);
    h.setMana(caster, 5000); area.current = "player"; area.ap = 9;
    assert(!h.cast(caster, area.actors.find(actor => actor.team === "enemy"), wave, null, { extraTargetId: friend.id }), `Not an area spell (${wave.name}), whoever the second target`);
    assert(h.cast(caster, area.actors.find(actor => actor.team === "enemy"), wave), "The same spell without a second target casts");
    ({ player, foes } = await setup(false));
    assert(!h.cast(player, foes[0], bolt, null, { extraTargetId: foes[1].id }), "Not without Lament");
    // A heal at an ally: a second ally.
    const heal = spells.find(spell => h.spellEffects(spell).length === 1 && h.spellEffects(spell)[0].recipient === "friendly-target" && /heal/.test(h.spellEffects(spell)[0].type));
    masterworkChecks.hero({ inventory: [wand], equipment: { primaryHand: wand }, spells: [heal.id], companion: {} });
    await Combat.startEncounter(["Orc"], "Lament heal");
    companions = [];
    const battle = h.getBattle(), you = h.actorById("player"), ally = battle.actors.find(actor => actor.companion);
    h.setMana(you, 5000); h.setHp(you, 5); battle.current = "player"; battle.ap = 9;
    h.setHp(ally, 0);
    assert(!h.cast(you, you, heal, null, { extraTargetId: ally.id }), "Not a fallen ally");
    h.setHp(ally, 5);
    assert(h.cast(you, you, heal, null, { extraTargetId: ally.id }) && h.hp(you) > 5 && h.hp(ally) > 5, `${heal.name} heals two allies`);
    // The second target gets only the targeted effects: a spell with a self effect doesn't give it twice.
    const mixed = spells.find(spell => { const effects = h.spellEffects(spell); return effects.some(effect => effect.recipient === "self") && effects.some(effect => effect.type === "damage" && (!effect.recipient || effect.recipient === "target")) && !effects.some(effect => /all-|random/.test(effect.recipient || "")); });
    assert(mixed, "A spell with a self effect is there to try");
    if (mixed) {
      ({ player, foes } = await setup(true, ["Orc", "Orc"], [mixed.id]));
      const selfBefore = JSON.stringify(player.effects.map(effect => effect.type));
      const linesBefore = h.getBattle().log.length;
      h.cast(player, foes[0], mixed, null, { extraTargetId: foes[1].id });
      const lamentLines = h.getBattle().log.slice(linesBefore).filter(entry => /\(Lament\)/.test(entry));
      assert(lamentLines.length && lamentLines.every(entry => entry.includes("Enemy 2")), `Lament's lines are all about the second target (${lamentLines})`);
      const selfEffects = player.effects.length;
      ({ player, foes } = await setup(true, ["Orc", "Orc"], [mixed.id]));
      h.cast(player, foes[0], mixed);
      assert(selfEffects === player.effects.length, `${mixed.name}'s self effect comes once (${selfBefore})`);
    }
    // The picker: a Lament button; then the second target's click casts on both.
    ({ player, foes } = await setup());
    h.refresh();
    document.getElementById("combat-spell-button").click();
    const lamentButton = document.querySelector(`[data-combat-lament="${bolt.id}"]`);
    assert(lamentButton && !lamentButton.disabled && /Lament: \+1 target \(\+50% Mana\)/.test(lamentButton.textContent), "The spell list offers Lament's second target");
    lamentButton.click();
    assert(document.getElementById("combat-ability-picker").hidden && /choose a second target/.test(document.getElementById("combat-action-status").textContent), "It waits for the second target");
    const before = [h.hp(foes[0]), h.hp(foes[1])];
    assert(Combat.chooseTargetId(foes[1].id) && h.hp(foes[0]) < before[0] && h.hp(foes[1]) < before[1], "Clicking the second target casts on both");
    assert(h.getBattle().targetId === foes[0].id, "The chosen target stays the first one");
    // Without Lament, no such button.
    ({ player, foes } = await setup(false));
    h.refresh();
    document.getElementById("combat-spell-button").click();
    assert(!document.querySelector("[data-combat-lament]"), "Without Lament the spell list has no such choice");
    document.querySelector("#combat-ability-picker .modal-close")?.click();
  } finally { Math.random = random; companions = []; Combat.restore(null); }
  return count();
}

// M21, M22: Noble Hero: a Roundtable crossbow; 25% less damage, then the same amount again, which counts as a weapon
// attack (on-hit effects); 1% from every demon and devil but the bosses.
async function checkNobleHero() {
  const { assert, count } = masterworkChecks.counter();
  const h = await masterworkChecks.hooks();
  const bow = masterworkChecks.forged("noble-hero");
  const item = getItem(bow);
  assert(item.forged.type === "crossbow" && item.forged.parts[0].maker === "roundtable" && item.effects.some(effect => effect.kind === "second-strike" && effect.amount === 25), "A Roundtable crossbow with its second strike");
  const random = Math.random;
  const shoot = async (worn = [], strip = false) => {
    // The Orc's gear is forged with the dice when it spawns: the same dice each time.
    Math.random = () => 0.5;
    let fight;
    try { fight = await masterworkChecks.battle(h, ["Orc"], { inventory: [bow, ...worn], equipment: { primaryHand: bow, artifact1: worn[0] || null } }); } finally { Math.random = random; }
    const { battle, player, foes } = fight;
    h.setHp(foes[0], 1e7); h.setHp(player, 10); battle.current = "player"; battle.ap = 9;
    Math.random = () => 0.5;
    try { strip ? masterworkChecks.withoutEffect(bow, "second-strike", () => h.attack(player, foes[0])) : h.attack(player, foes[0]); } finally { Math.random = random; }
    return { total: 1e7 - h.hp(foes[0]), healed: h.hp(player) - 10, log: battle.log.slice(-3) };
  };
  const without = await shoot([], true), noble = await shoot();
  const first = Math.floor(without.total * 0.75);
  assert(Math.abs(noble.total - first * 2) <= 2, `25% less, then the same again: ${without.total} → ${noble.total}`);
  assert(noble.log.some(entry => /strikes Enemy 1 again/.test(entry)), "The log shows the second strike");
  // Exactly the same amount, also when a damage bonus counted in the first (Greenskin Tooth against an Orc).
  const tooth = await shoot(["artifact-greenskin-tooth-artifact"]);
  const amounts = tooth.log.map(entry => (/: (\d+) damage/.exec(entry) || [])[1]).filter(Boolean).map(Number);
  assert(amounts.length === 2 && amounts[0] === amounts[1] && amounts[0] > 0, `The second strike repeats the first exactly (${amounts})`);
  // The second strike counts as a weapon attack: Leech Hook heals for it too.
  const leech = "artifact-leech-hook-artifact";
  const leeched = await shoot([leech]), leechedPlain = await shoot([leech], true);
  assert(leeched.healed > Math.floor(leechedPlain.healed * 0.75) * 1.5, `The second strike heals through Leech Hook too (${leechedPlain.healed} → ${leeched.healed})`);
  // A miss strikes nothing, twice.
  const { battle, player, foes } = await masterworkChecks.battle(h, ["Orc"], { inventory: [bow], equipment: { primaryHand: bow } });
  h.setHp(foes[0], 1e7); battle.current = "player"; battle.ap = 9; Math.random = () => 0.9999;
  try { h.attack(player, foes[0]); } finally { Math.random = random; }
  assert(h.hp(foes[0]) === 1e7, "A miss has no second strike");
  // Drops: every demon and devil but the bosses.
  const definitions = h.definitions(), drops = enemy => (enemy.drops || []).filter(drop => drop.itemId === "masterwork-noble-hero");
  for (const enemy of definitions) {
    const expected = ["demon", "devil"].includes(enemy.character.raceId) && !enemy.unique;
    assert(drops(enemy).length === (expected ? 1 : 0) && (!expected || drops(enemy)[0].chance === 1), `${enemy.name} ${expected ? "drops it at 1%" : "doesn't drop it"}`);
  }
  assert(definitions.filter(enemy => drops(enemy).length).length >= 11, "Every non-boss demon and devil");
  Combat.restore(null);
  return count();
}

// M23, M26: the Hexbane Coat: a Boreal Bolt coat; spell damage taken −10%; 25% of an enemy spell's damage strikes its
// caster back (its defenses apply; a reflection is never reflected); 1% from every non-boss undead, demon and devil.
async function checkHexbaneCoat() {
  const { assert, count } = masterworkChecks.counter();
  const h = await masterworkChecks.hooks();
  const coat = masterworkChecks.forged("hexbane-coat", 50);
  const item = getItem(coat);
  assert(item.forged.type === "coat" && item.forged.category === "clothing" && item.forged.parts[0].maker === "boreal-bolt", "A Boreal Bolt coat");
  const random = Math.random;
  try {
    const { battle, player, foes } = await masterworkChecks.battle(h, ["Orc", "Orc"], { level: 50, inventory: [coat], equipment: { armor: coat } });
    h.setHp(player, 1e6); foes.forEach(foe => h.setHp(foe, 1e6));
    // The ward: 10% less spell damage, weapons as before.
    const factor = (kind, element) => h.incomingDamageFactor(player, foes[0], kind, element);
    const warded = factor("spell", "fire"), unwarded = masterworkChecks.withoutEffect(coat, "incoming-spell-damage-percent", () => factor("spell", "fire"));
    assert(Math.abs(warded - unwarded * 0.9) < 1e-9 || Math.abs(warded - (unwarded - 0.1)) < 1e-9, `Spell damage taken −10% (${unwarded} → ${warded})`);
    assert(factor("melee", "physical") === masterworkChecks.withoutEffect(coat, "incoming-spell-damage-percent", () => factor("melee", "physical")), "Weapon damage as before");
    // The reflection: a fire spell from the first Orc.
    Math.random = () => 0.5;
    const spellHit = () => { const before = [h.hp(player), h.hp(foes[0]), h.hp(foes[1])]; h.hurt(player, 400, "fire", false, true, foes[0], "spell"); return [before[0] - h.hp(player), before[1] - h.hp(foes[0]), before[2] - h.hp(foes[1])]; };
    const [taken, back, bystander] = spellHit();
    assert(taken > 0 && back > 0 && back <= Math.ceil(taken * 0.25) && bystander === 0, `25% of the spell's damage strikes its caster back, defenses applying (${taken} taken, ${back} back)`);
    assert(battle.log.some(entry => /Hexbane Coat turns \d+ damage back/.test(entry)), "The log says so");
    foes[0].resistances = { ...(foes[0].resistances || {}), fire: 50 };
    const [resistedTaken, resistedBack] = spellHit();
    assert(resistedBack > 0 && resistedBack <= Math.ceil(resistedTaken * 0.125) + 1, `The caster's Fire resistance halves what comes back (${resistedTaken} taken, ${resistedBack} back)`);
    foes[0].resistances.fire = 0;
    const [, noBack] = masterworkChecks.withoutEffect(coat, "spell-reflect-percent", spellHit);
    assert(noBack === 0, "Without its effect, nothing comes back");
    const weaponBefore = h.hp(foes[0]);
    h.hurt(player, 400, "physical", false, false, foes[0], "melee");
    assert(h.hp(foes[0]) === weaponBefore, "A weapon hit isn't reflected");
    // A reflection is never reflected again: the caster wearing a Hexbane Coat too.
    foes[0].character.inventory.push(coat); foes[0].character.equipment.armor = coat;
    const lines = battle.log.length;
    h.hurt(player, 400, "fire", false, true, foes[0], "spell");
    assert(battle.log.slice(lines).filter(entry => /Hexbane Coat turns/.test(entry)).length === 1, "Only one reflection");
    // An ally's spell isn't reflected.
    const allyBefore = h.hp(player);
    h.hurt(foes[1], 400, "fire", false, true, foes[0], "spell");
    assert(h.hp(player) === allyBefore, "An enemy's own coat doesn't hit back at its ally... nor at you");
  } finally { Math.random = random; }
  // Drops: 1% from every non-boss undead, demon and devil, not objects.
  const definitions = h.definitions(), drops = enemy => (enemy.drops || []).filter(drop => drop.itemId === "masterwork-hexbane-coat");
  for (const enemy of definitions) {
    const expected = ["undead", "demon", "devil"].includes(enemy.character.raceId) && !enemy.unique && !enemy.inert;
    assert(drops(enemy).length === (expected ? 1 : 0) && (!expected || drops(enemy)[0].chance === 1), `${enemy.name} ${expected ? "drops it at 1%" : "doesn't drop it"}`);
  }
  assert(definitions.filter(enemy => drops(enemy).length).length === 29, `29 enemies drop it (${definitions.filter(enemy => drops(enemy).length).length})`);
  // Its look, and Doctor's Orders', wear their own colours (placeholder art).
  const doctor = getItem(masterworkChecks.forged("doctors-orders", 50));
  const tone = color => SpriteKit?.paint ? SpriteKit.paint(color).toLowerCase() : color;
  const art = ForgeArt.weapon(item, "large").toLowerCase(), doctorArt = ForgeArt.weapon(doctor, "large").toLowerCase();
  assert(art.includes("#9aa3ad") || art.includes(tone("#9aa3ad")), "The coat is silver-grey");
  assert(doctorArt.includes("#ece8de") || doctorArt.includes(tone("#ece8de")), "Doctor's Orders is white");
  Combat.restore(null);
  return count();
}

// M24: the Shadow Crystal: 0.05% from every enemy that gives loot, bosses too; not objects; never from a summon.
async function checkShadowCrystal() {
  const { assert, count } = masterworkChecks.counter();
  const h = await masterworkChecks.hooks();
  const definitions = h.definitions();
  for (const enemy of definitions) {
    const crystal = (enemy.drops || []).filter(drop => drop.itemId === "key-item-shadow-crystal");
    assert(crystal.length === (enemy.inert ? 0 : 1) && (enemy.inert || crystal[0].chance === 0.05), `${enemy.name} ${enemy.inert ? "drops none" : "drops it at 0.05%"}`);
  }
  assert(definitions.some(enemy => enemy.unique && enemy.drops.some(drop => drop.itemId === "key-item-shadow-crystal")), "Bosses too");
  const random = Math.random;
  try {
    const { battle, foes } = await masterworkChecks.battle(h, ["Goblin"]);
    h.setHp(foes[0], 0);
    Math.random = () => 0.0004;
    h.awardDefeatedEnemyExperience();
    assert(battle.lootItems.includes("key-item-shadow-crystal"), "Under 0.05% it drops");
    const second = await masterworkChecks.battle(h, ["Goblin"]);
    h.setHp(second.foes[0], 0);
    Math.random = () => 0.0006;
    h.awardDefeatedEnemyExperience();
    assert(!second.battle.lootItems.includes("key-item-shadow-crystal"), "Over it, none");
  } finally { Math.random = random; Combat.restore(null); }
  return count();
}

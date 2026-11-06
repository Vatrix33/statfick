async function runTalosBossChecks() {
  const assert = (condition, message) => { if (!condition) throw new Error(message); };
  const $ = id => document.getElementById(id), wait = ms => new Promise(resolve => setTimeout(resolve, ms));
  const clone = value => structuredClone(value);
  const bossId = "tankina-disciple-of-talos-level-44", boarId = "thunder-boar-gorthok-level-40";
  const forgeMasterwork = key => ForgeRules.encode(ForgeRules.generateMasterwork({ key, level: 100, random: () => 0.5 }));
  const claw = forgeMasterwork("claw-of-beowulf"), hide = forgeMasterwork("stormhide-cuirass"), shield = forgeMasterwork("transformer");
  const boltId = "thunderbolt-of-talos-lightning-unique";
  WorldMap.setAmbientEnabled(false);
  function fresh(weapon = null, armor = null, offHand = null) {
    Combat.restore(null); const c = defaultCharacter(); c.raceId = "human"; c.level = 100;
    for (const key of Object.keys(c.stats)) c.stats[key] = 100;
    c.stats.speed = 1000; c.stats.luck = 0; c.currentHp = 100000; c.currentMana = 100000;
    c.inventory = [weapon, armor, offHand].filter(Boolean); c.equipment.primaryHand = weapon;
    c.equipment.offHand = getItem(weapon)?.handed === 2 ? weapon : offHand; c.equipment.armor = armor;
    c.knownSpells = ["spell-bolt-basic-damage", "lightning-bolt-lightning-damage", "chain-lightning-lightning-damage", boltId];
    applyCharacter(normalizeCharacter(c));
  }
  const site = WorldRules.getRegion("stormwake-hills").sites[0];
  const moveToChurch = () => { worldState.regionId = "stormwake-hills"; worldState.position = { x: site.x, y: site.y }; WorldMap.render(); };
  const unlocked = () => worldState.defeatedSiteBosses?.includes(bossId);
  fresh(); moveToChurch();
  assert(!$('map-church').hidden && $('map-class-respec').hidden && $('map-stat-respec').hidden, "Church is reachable, but services start locked");
  assert(document.querySelector('[data-map-site="church-of-talos"]') && WorldRules.settlementAt(site, worldState.regionId) === null, "Church has its own destination and does not grant town services");
  assert(await ChurchOfTalos.challenge(), "Church starts its fixed encounter");
  $('combat-sheet-button').click();
  let state = Combat.serialize(), boss = state.actors.find(actor => actor.enemyDefinitionId === bossId);
  assert(boss?.character.level === 44 && boss.character.raceId === "orc" && boss.character.classLevels.vampire === 4 && boss.character.classLevels["raid-boss"] === 1, "Tankina loads with her requested level, race and classes");
  assert(state.actors.filter(actor => actor.enemyDefinitionId === "apostle-of-talos-level-38").length === 2, "Exactly two melee apostles begin combat");
  assert(boss.dropTable.find(drop => drop.itemId === "key-item-blood-crystal").chance === 3, "Default Vampire drops do not overwrite Tankina's custom three-percent chance");
  assert(Combat.musicTheme() === "tankina-disciple-of-talos" && Music.scoreInfo()["tankina-disciple-of-talos"].kind === "remaster" && Music.scoreInfo()["tankina-disciple-of-talos"].bars >= 32, "Boss uses its distinct, fully arranged soundtrack");
  const opening = performance.now(), decision = Combat.previewAiDecision(boss.id);
  assert(decision?.kind && performance.now() - opening < 1500, "Tankina's AI is responsive at battle start");
  boss.character.currentHp = Math.floor(boss.resourceMax.hp * .5); const caps = clone(boss.resourceMax);
  state.current = null; state.actors.forEach(actor => { actor.counter = actor.id === boss.id ? 1000 : 0; });
  Combat.restore(state); Combat.onSheetChange(); await wait(40);
  state = Combat.serialize(); boss = state.actors.find(actor => actor.enemyDefinitionId === bossId);
  assert(boss.phaseIndex === 1 && boss.name === "Tankina, Chosen of Talos" && boss.phaseSpellDamagePercent === 30, "Half HP activates the Chosen form and thirty-percent spell damage");
  assert(boss.resourceMax.hp === caps.hp && boss.resourceMax.mana === caps.mana, "Phase change leaves battle resource caps fixed");
  const boar = state.actors.find(actor => actor.summonDefinitionId === boarId);
  assert(boar?.character.level === 40 && boar.character.raceId === "orc" && boar.maxHpPercent === 40 && boar.team === "enemy", "Phase summons the requested, HP-boosted Gorthok");
  assert(!$('boss-intro-cinematic').hidden && $('boss-intro-cinematic').dataset.boss === "tankina-phase", "The Talos phase has its own cutscene");
  $('boss-intro-skip').click();
  state.current = null; state.actors.forEach(actor => { actor.counter = actor.id === boss.id ? 1000 : 0; });
  Combat.restore(state); Combat.onSheetChange(); await wait(40);
  assert(Combat.serialize().actors.filter(actor => actor.summonDefinitionId === boarId).length === 1 && $('boss-special-cinematic').hidden, "Restore/another turn does not repeat the phase or summon");
  assert(!unlocked() && ChurchOfTalos.serviceReason("classes"), "Services remain locked during combat");

  async function fixture(weapon = null, armor = null, offHand = null) {
    fresh(weapon, armor, offHand); assert(await Combat.startEncounter(["Goblin"]), "Gear fixture starts");
    $('combat-sheet-button').click(); const result = Combat.serialize(); result.current = "player"; result.ap = 5;
    const target = result.actors.find(actor => actor.team === "enemy");
    target.resourceMax = { hp: 100000, mana: 1000 }; target.character.currentHp = 100000; target.character.currentMana = 1000;
    target.character.classLevels = {}; target.effects = []; target.baseArmor = 0; target.resistances = {}; target.phaseTransitions = null;
    target.character.equipment = defaultCharacter().equipment; target.character.stats.speed = 1; target.character.stats.dexterity = 0; target.character.stats.luck = 0;
    result.targetId = target.id; return result;
  }
  function attack(state, roll = .1) {
    state.current = "player"; state.ap = 5; Combat.restore(state); $('combat-view-toggle').click();
    const random = Math.random; Math.random = () => roll;
    try { document.querySelector('[data-action="attack"]').click(); } finally { Math.random = random; }
    $('combat-sheet-button').click(); return Combat.serialize();
  }
  state = await fixture(claw);
  let result = attack(state), target = result.actors.find(actor => actor.team === "enemy"), mark = target.effects.find(effect => effect.name === "Beowulf's Mark");
  const firstHit = 100000 - target.character.currentHp;
  assert(firstHit > 0 && mark?.amount === 20 && mark.attackKinds.join() === "melee,ranged" && mark.expiresTurn === target.turns + 2, "Claw's successful proc applies a two-turn melee/ranged mark");
  result = attack(result); target = result.actors.find(actor => actor.team === "enemy");
  const secondHit = 100000 - firstHit - target.character.currentHp;
  assert(Math.abs(secondHit - Math.floor(firstHit * 1.2)) <= 1 && target.effects.filter(effect => effect.name === "Beowulf's Mark").length === 1, "Mark increases weapon damage twenty percent and refreshes without stacking");
  result = attack(await fixture(claw), .6);
  assert(!result.actors.find(actor => actor.team === "enemy").effects.some(effect => effect.name === "Beowulf's Mark"), "Proc roll above 35% does not mark");
  fresh(null, hide); const withHide = Combat.previewValues(normalizeCharacter(backupPayload().character));
  const hideKinds = getItem(hide).effects; getItem(hide).effects = hideKinds.filter(effect => effect.kind !== "evasion-flat");
  let plainEvasion; try { plainEvasion = Combat.previewValues(normalizeCharacter(backupPayload().character)).evasion; } finally { getItem(hide).effects = hideKinds; }
  assert(withHide.evasion === plainEvasion + 10, "Stormhide grants ten evasion on the main and companion calculations");

  function castFromFixture(state, spellId) {
    state.current = "player"; state.ap = 5; Combat.restore(state); $('combat-view-toggle').click();
    const random = Math.random; Math.random = () => .1;
    try { $('combat-spell-button').click(); const choice = document.querySelector(`[data-combat-ability="${spellId}"]`); assert(choice && !choice.disabled, "Spell is castable: " + spellId); choice.click(); }
    finally { Math.random = random; }
    $('combat-sheet-button').click(); return Combat.serialize();
  }
  state = await fixture(); target = state.actors.find(actor => actor.team === "enemy");
  let ward = clone(state); const wardTarget = ward.actors.find(actor => actor.id === target.id);
  wardTarget.character.inventory = [hide]; wardTarget.character.equipment.armor = hide;
  // Equalize armor to isolate Lightning resistance from the armor's ordinary reduction.
  target.character.inventory = [hide]; target.character.equipment.armor = hide;
  const hideEffects = getItem(hide).effects; getItem(hide).effects = hideEffects.filter(effect => effect.kind !== "incoming-spell-damage-percent");
  let unprotected;
  try { unprotected = castFromFixture(state, "lightning-bolt-lightning-damage"); } finally { getItem(hide).effects = hideEffects; }
  const protectedResult = castFromFixture(ward, "lightning-bolt-lightning-damage");
  const normalDamage = 100000 - unprotected.actors.find(actor => actor.id === target.id).character.currentHp;
  const wardDamage = 100000 - protectedResult.actors.find(actor => actor.id === target.id).character.currentHp;
  assert(Math.abs(wardDamage - Math.floor(normalDamage * .5)) <= 1, "Stormhide halves incoming Lightning spell damage");

  state = await fixture(); target = state.actors.find(actor => actor.team === "enemy");
  target.character.inventory = [shield]; target.character.equipment.offHand = shield;
  result = castFromFixture(state, "spell-bolt-basic-damage");
  assert(!result.actors.find(actor => actor.id === target.id).effects.some(effect => effect.name.endsWith("Transformer")), "Basic spells cannot trigger Transformer");
  result = castFromFixture(state, "chain-lightning-lightning-damage"); target = result.actors.find(actor => actor.id === target.id);
  let buff = target.effects.find(effect => effect.name.endsWith("Transformer"));
  assert(buff?.amount === 30 && buff.expiresTurn === target.turns + 3 && buff.type === "all-stats-percent", "Tier 3 non-Basic damage grants thirty-percent basic stats for three turns");
  const targetCaps = clone(target.resourceMax);
  result = castFromFixture(result, "lightning-bolt-lightning-damage"); target = result.actors.find(actor => actor.id === target.id);
  assert(target.effects.filter(effect => effect.name.endsWith("Transformer")).length === 1 && target.effects.find(effect => effect.name.endsWith("Transformer")).amount === 30 && JSON.stringify(target.resourceMax) === JSON.stringify(targetCaps), "Further hits keep the stronger bonus without stacking or changing max HP/Mana");
  for (let i = 0; i < 3; i++) {
    result.current = null; result.actors.forEach(actor => { actor.counter = actor.id === target.id ? 1000 : 0; });
    Combat.restore(result); Combat.onSheetChange(); await wait(15); result = Combat.serialize();
  }
  assert(!result.actors.find(actor => actor.id === target.id).effects.some(effect => effect.name.endsWith("Transformer")), "Transformer expires after three wearer turns");

  fresh(); state = await fixture();
  Combat.restore(state); $('combat-view-toggle').click(); Combat.castFromSheet(getSpell(boltId)); result = Combat.serialize(); $('combat-sheet-button').click();
  assert(result.ap === 5, "The player cannot cast Tankina/Gorthok's exclusive spell");

  fresh(); moveToChurch(); assert(await ChurchOfTalos.challenge(), "Repeat boss fixture starts"); $('combat-sheet-button').click();
  state = Combat.serialize(); state.actors.filter(actor => actor.team === "enemy").forEach(actor => { actor.character.currentHp = 0; }); state.current = null;
  const random = Math.random; Math.random = () => 0;
  try { Combat.restore(state); Combat.onSheetChange(); await wait(40); } finally { Math.random = random; }
  const loot = Combat.serialize().lootItems;
  assert(unlocked() && loot.some(id => id.startsWith("armor-masterwork-transformer-")) && loot.includes("key-item-blood-crystal") && !loot.some(id => /claw-of-beowulf|stormhide/.test(id)), "Victory unlocks the Church and shows successful chanced drops in the loot window (the Claw now comes from wolves, the Cuirass from the Storm Reliquary)");
  $('combat-loot-keep').click(); Combat.restore(null); WorldMap.open(); WorldMap.render();
  assert(!$('map-class-respec').hidden && !$('map-stat-respec').hidden, "Both respec buttons appear at the Church after victory");
  assert(normalizeCharacter(backupPayload().character).worldState.defeatedSiteBosses.includes(bossId), "Church unlock survives save normalization");
  classLevels = { vampire: 4, warrior: 5 }; perkPoints = 7; permanentBonuses.strength = 12;
  classProgress.statChoiceBonuses = { "warrior:choice": { stat: "strength", amount: 5 } }; classProgress.oneHandedAttacks = 123;
  itemsSold = 200; defensiveActionsUsed = 30; classKills.monster = 42;
  currencies = ShopRules.walletFromValue(site.servicePriceSilver * 3);
  const confirm = window.confirm; window.confirm = () => false;
  try { assert(!ChurchOfTalos.respec("classes") && perkPoints === 7 && ShopRules.walletValue(currencies) === site.servicePriceSilver * 3, "Cancelled offering makes no changes"); }
  finally { window.confirm = confirm; }
  window.confirm = () => true;
  try {
    assert(ChurchOfTalos.respec("classes") && Object.keys(classLevels).length === 0 && perkPoints === 16, "Class respec refunds every spent perk point");
    assert(permanentBonuses.strength === 7 && itemsSold === 200 && defensiveActionsUsed === 30 && classKills.monster === 42 && classProgress.oneHandedAttacks === 123, "Respec removes tracked class choices, preserving manual bonuses and progression counters");
    assert(classProgress.fulfilledClassOtherRequirements.includes("vampire") && otherClassRequirementMet(getClass("vampire")), "Consumed item requirements remain fulfilled");
    inventory = inventory.filter(id => id !== "key-item-blood-crystal"); assert(consumeClassAcquisitionRequirement(getClass("vampire")) === "", "Reacquiring an item-unlocked class does not consume another item");
    assert(ShopRules.walletValue(currencies) === site.servicePriceSilver * 2, "Class respec costs one offering");
    stats.strength += 7; manualAllocations.strength = 7; availablePoints = 3; const strengthBefore = stats.strength;
    assert(ChurchOfTalos.respec("stats") && stats.strength === strengthBefore - 7 && availablePoints === 10 && totalManualAllocations() === 0, "Stat respec refunds only manually allocated points");
    assert(!ChurchOfTalos.respec("stats") && ShopRules.walletValue(currencies) === site.servicePriceSilver, "Empty respec cannot charge again");
    worldState.regionId = "alder-vale"; WorldMap.render(); assert($('map-class-respec').hidden && ChurchOfTalos.serviceReason("classes"), "Services cannot be used elsewhere");
  } finally { window.confirm = confirm; }
  const saved = normalizeCharacter(backupPayload().character);
  assert(saved.classProgress.fulfilledClassOtherRequirements.includes("vampire") && saved.worldState.defeatedSiteBosses.includes(bossId), "Respec requirement history and boss unlock persist together");
  Combat.restore(null); fresh(); moveToChurch(); WorldMap.open(); $('map-church').click(); await wait(700);
  await runTankinaChecks();
}

// Tankina rework (2026-10-11): the gathering storm, her Apostles' Call the Storm, her look, intro,
// phase cutscene and storm-ritual music, the Church's Storm Reliquary, and her three items as forged
// masterworks: the Claw of Beowulf (now from wolves), the Stormhide Cuirass (sold in the
// Reliquary) and the Transformer.
async function runTankinaChecks() {
  let checks = 0;
  const assert = (condition, message) => { if (!condition) throw Error(message); checks++; };
  const $ = id => document.getElementById(id);
  document.getElementById("church-close")?.click();
  Combat.restore(null);
  const source = await (await fetch("scripts/combat.js")).text();
  const marker = "  window.Combat = {";
  const script = document.createElement("script");
  script.textContent = source.replace(marker, "  window.__tankinaChecks = { actorById, hp, values, setHp, enemyAction, endTurn, isControlledTurn, currentActor, getBattle: () => battle, stageLook, cast, feedStorm, stormState };\n" + marker);
  document.head.append(script);
  await waitForSourceFiles();
  const hooks = window.__tankinaChecks;
  if (window.BattleStage) window.BattleStage.enabled = () => false;
  if (window.GameSettings) window.GameSettings.enemyDelay = () => 1e9;

  // Her sheet: the storm in five stages, a phase cutscene, and the Crystal and Transformer as her only drops.
  const raw = await (await fetch("enemies/" + encodeURIComponent("Tankina, Disciple of Talos") + ".json")).json();
  assert(raw.aiProfile === "tankina" && raw.introCinematic === "tankina", "Tankina has her own AI and intro");
  assert(raw.storm.stages.map(stage => stage.id + "@" + stage.at).join() === "drizzle@0,rain@2,storm@4,thunderstorm@7,tempest@10", "The storm grows from a light drizzle to an extreme thunderstorm");
  assert(raw.phaseTransitions[0].thresholdHpPercent === 50 && raw.phaseTransitions[0].cinematic === "tankina-phase" && !raw.phaseTransitions[0].animation, "Her phase stays at 50% HP with its own cutscene");
  assert(raw.drops.map(drop => drop.itemId + ":" + drop.chance).sort().join() === "key-item-blood-crystal:3,masterwork-cherubim:5,masterwork-transformer:8", "She drops the Transformer (8%) and the Blood Crystal, and Cherubim (5%, task 7); the Claw and the Cuirass are gone from her drops");
  const apostle = await (await fetch("enemies/" + encodeURIComponent("Apostle of Talos") + ".json")).json();
  assert(apostle.aiProfile === "talos-apostle" && apostle.knownSpells.includes("call-the-storm-lightning-unique") && getSpell("call-the-storm-lightning-unique")?.element === "lightning", "Her Apostles can Call the Storm");
  for (const name of ["Juvenile Wolf", "Adult Wolf", "Highland Wolf", "Dire Wolf", "Duskpine Wolf", "Boreal Direwolf"]) {
    const wolf = await (await fetch("enemies/" + encodeURIComponent(name) + ".json")).json();
    assert(wolf.drops?.some(drop => drop.itemId === "masterwork-claw-of-beowulf" && drop.chance === 2), name + " drops the Claw of Beowulf at 2%");
  }
  // Her three items are forged masterworks, each with its fixed maker, type and effect.
  const designs = { "claw-of-beowulf": ["zweihander", "boreal-bolt"], "stormhide-cuirass": ["leather-armor", "church-of-talos"], transformer: ["kite-shield", "church-of-talos"] };
  for (const [key, [type, maker]] of Object.entries(designs)) {
    const piece = getItem(ForgeRules.encode(ForgeRules.generateMasterwork({ key, level: 47, random: Math.random })));
    assert(piece?.rarity === "masterwork" && piece.forged.type === type && piece.forged.parts[0].maker === maker && piece.forged.level === 47 && piece.name.endsWith(ForgeRules.MASTERWORKS[key].name), key + " is a forged " + type + " with a " + maker + " main part");
  }
  const template = id => getItem(id)?.effects || [];
  assert(template("masterwork-claw-of-beowulf").some(effect => effect.kind === "mark-on-hit" && effect.chance === 35 && effect.amount === 20 && effect.durationTurns === 2), "The Claw keeps its mark: 35% on hit, +20% melee and ranged damage taken for 2 turns");
  assert(template("masterwork-stormhide-cuirass").some(effect => effect.kind === "evasion-flat" && effect.amount === 10) && template("masterwork-stormhide-cuirass").some(effect => effect.kind === "incoming-spell-damage-percent" && effect.element === "lightning" && effect.amount === -50) && ForgeRules.ARMORS["leather-armor"].evasionPercent === 0, "The Cuirass keeps +10 Evasion with no Evasion penalty, and halves Lightning spell damage");
  assert(template("masterwork-transformer").some(effect => effect.kind === "all-stats-on-nonbasic-spell-damage-per-tier" && effect.amount === 10 && effect.durationTurns === 3), "The Transformer keeps +10% basic stats per spell tier for 3 turns");
  const migrated = normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: 30, inventory: ["weapon-claw-of-beowulf-meele", "armor-stormhide-cuirass-light", "armor-transformer-shield"] }).inventory.map(getItem);
  assert(migrated.map(item => item?.forged?.masterwork).join() === "claw-of-beowulf,stormhide-cuirass,transformer", "Old copies of the three items become their forged masterworks");

  // A sturdy lightning caster at the Church.
  const F = ForgeRules;
  const blade = F.encode(F.generate({ type: "longsword", level: 47, source: "shop", random: () => 0.5 }));
  const plate = F.encode(F.generateArmor({ type: "full-plate", level: 47, source: "shop", random: () => 0.5 }));
  const c = defaultCharacter();
  Object.assign(c, { raceId: "human", level: 47, inventory: [blade, plate], equipment: { ...defaultEquipment, primaryHand: blade, armor: plate }, knownSpells: ["lightning-bolt-lightning-damage"] });
  c.stats = { strength: 90, vitality: 600, mind: 200, dexterity: 70, intelligence: 120, wisdom: 25, speed: 110, luck: 30 };
  applyCharacter(normalizeCharacter(c));
  currentHp = 1e6; currentMana = 1e6;
  const site = WorldRules.getRegion("stormwake-hills").sites[0];
  worldState.regionId = "stormwake-hills"; worldState.position = { x: site.x, y: site.y }; worldState.defeatedSiteBosses = [];
  assert(!ChurchOfTalos.store.available(), "The Storm Reliquary is closed while Tankina stands");
  assert(await ChurchOfTalos.challenge(), "Her fight starts");
  const intro = $("boss-intro-cinematic");
  assert(!intro.hidden && intro.dataset.boss === "tankina" && intro.querySelector(".bi-boss svg") && intro.querySelectorAll(".bi-tk-rain").length > 20, "She is revealed at the storm altar in her intro");
  $("boss-intro-skip").click();
  assert(Combat.musicTheme() === "tankina-disciple-of-talos" && ["tankina-disciple-of-talos", "tankina-chosen-of-talos"].every(id => MusicThemes.all().some(theme => theme.id === id)), "Her storm ritual plays");
  const battle = hooks.getBattle(), tankina = battle.actors.find(actor => actor.enemyDefinitionId === "tankina-disciple-of-talos-level-44");
  const apostles = battle.actors.filter(actor => actor.enemyDefinitionId === "apostle-of-talos-level-38"), player = () => hooks.actorById("player");
  const heal = () => { currentHp = hooks.values(player()).maxHp; currentMana = 1e6; };
  const step = () => { if (hooks.isControlledTurn()) { heal(); hooks.endTurn(); } else hooks.enemyAction(); };
  const until = (condition, limit = 300) => { for (let i = 0; i < limit && !condition() && Combat.active(); i++) step(); return condition(); };

  // The storm starts as a light drizzle that gives her nothing yet.
  assert(until(() => battle.storm && hooks.stormState()), "The storm begins with the fight");
  assert(hooks.stormState().stage.id === "drizzle" && !tankina.effects.some(effect => effect.storm) && hooks.stageLook(tankina).storm === "drizzle", "It begins as a light drizzle that grants nothing");
  const drizzleLook = Sprites.forActor(hooks.stageLook(tankina)).svg, drizzleSpeed = hooks.values(tankina).speed;

  // Anyone's lightning feeds it: the hero's Lightning Bolt counts once, an Apostle's Call the Storm twice.
  const before = battle.storm.intensity;
  hooks.cast(player(), tankina, getSpell("lightning-bolt-lightning-damage"));
  assert(battle.storm.intensity === before + 1, "The hero's lightning spell feeds her storm");
  assert(until(() => hooks.currentActor()?.id === apostles[0].id), "An Apostle's turn comes");
  const beforeCall = battle.storm.intensity;
  assert(hooks.cast(apostles[0], apostles[0], getSpell("call-the-storm-lightning-unique")) && battle.storm.intensity === beforeCall + 2, "Call the Storm counts twice");
  assert(hooks.stormState().state.stageIndex >= 1 && battle.log.some(entry => String(entry).startsWith("The storm grows: ")), "The drizzle turns to rain and more");
  const grown = hooks.stormState().stage, attackBonus = grown.allies.find(entry => entry.type === "outgoing-attack-damage-percent").amount;
  assert(tankina.effects.some(effect => effect.storm && effect.type === "outgoing-attack-damage-percent" && effect.amount === attackBonus) && apostles.every(actor => hooks.hp(actor) <= 0 || actor.effects.some(effect => effect.storm)) && !player().effects.some(effect => effect.storm), "The storm strengthens her and her Apostles, never the heroes");

  // At its height, an extreme thunderstorm.
  hooks.feedStorm(10);
  assert(hooks.stormState().stage.id === "tempest" && !hooks.stormState().next, "The storm reaches an extreme thunderstorm and grows no further");
  assert(tankina.effects.filter(effect => effect.storm).some(effect => effect.type === "outgoing-attack-damage-percent" && effect.amount === 28) && tankina.effects.filter(effect => effect.storm).length >= 4 && hooks.values(tankina).speed > drizzleSpeed, "The extreme storm grants her its full strength, replacing the weaker stages");
  assert(hooks.stageLook(tankina).storm === "tempest" && Sprites.forActor(hooks.stageLook(tankina)).svg !== drizzleLook, "Her look crackles with the storm");
  const snapshot = Combat.serialize();
  assert(snapshot.storm?.stageIndex === 4 && snapshot.storm.intensity >= 13, "The storm is saved with the battle");

  // At half health, Talos answers: the phase cutscene, Gorthok, and the wild second theme.
  tankina.character.currentHp = Math.floor(hooks.values(tankina).maxHp * 0.45);
  assert(until(() => tankina.phaseIndex >= 1), "She becomes the Chosen of Talos");
  assert(!intro.hidden && intro.dataset.boss === "tankina-phase", "The phase plays its own cutscene");
  $("boss-intro-skip").click();
  const gorthok = battle.actors.find(actor => actor.summonDefinitionId === "thunder-boar-gorthok-level-40");
  assert(tankina.name === "Tankina, Chosen of Talos" && gorthok && Combat.musicTheme() === "tankina-chosen-of-talos", "Gorthok answers the storm, and the rite turns wild");
  assert(until(() => gorthok.turns >= 1 || hooks.hp(gorthok) <= 0) && (hooks.hp(gorthok) <= 0 || gorthok.effects.some(effect => effect.storm)), "Gorthok fights in the storm's strength too");

  // Her Apostles call the storm on their own.
  const called = () => apostles.some(actor => (actor.ai?.history || []).some(entry => entry.key === "call-the-storm-lightning-unique"));
  assert(until(called, 400), "An Apostle calls the storm in battle: " + JSON.stringify({ active: Combat.active(), apostles: apostles.map(actor => ({ hp: hooks.hp(actor), turns: actor.turns, history: (actor.ai?.history || []).map(entry => entry.key) })), log: battle.log.slice(-5) }));
  for (const actor of battle.actors.filter(actor => actor.team === "enemy")) hooks.setHp(actor, 0);
  until(() => !Combat.active(), 20);
  Combat.restore(null);

  // After her fall: the Storm Reliquary, with the Church's lightning casters, lightning spells and the Cuirass.
  worldState.defeatedSiteBosses = ["tankina-disciple-of-talos-level-44"];
  worldState.regionId = "stormwake-hills"; worldState.position = { x: site.x, y: site.y };
  WorldMap.render();
  assert(!$("map-church-store").hidden && ChurchOfTalos.store.available(), "The Storm Reliquary opens after Tankina's fall");
  currencies = ShopRules.walletFromValue(300000);
  assert(ChurchOfTalos.store.open() && !$("church-store-modal").hidden && !$("church-store-scene").hidden, "The Reliquary opens with its storm chapel");
  const stock = ChurchOfTalos.store.stock();
  assert(stock.tower.length === 6 && stock.tower.every(entry => { const item = getItem(entry.id); return item?.forged?.caster && item.forged.maker === "church-of-talos" && F.STONES[item.forged.parts[0].material].element === "lightning" && item.forged.level === 47; }), "It sells six Church of Talos lightning staffs and wands, forged at the hero's level");
  assert(stock.spells.length === 8 && stock.spells.every(id => getSpell(id)?.element === "lightning" && getSpell(id).shopAvailable !== false), "It teaches eight lightning spells");
  const cuirass = getItem(ChurchOfTalos.store.cuirass());
  assert(ChurchOfTalos.store.cuirassPrice === 200000 && cuirass?.forged?.masterwork === "stormhide-cuirass" && cuirass.forged.level === 47 && cuirass.soldAt?.includes("Storm Reliquary") && document.querySelector('#church-store-list [data-shop-entry="' + cuirass.id + '"]'), "The Stormhide Cuirass is on offer for 20 Platinum, forged at the hero's level");
  const wallet = ShopRules.walletValue(currencies);
  assert(ChurchOfTalos.store.buy(cuirass.id) && inventory.includes(cuirass.id) && ShopRules.walletValue(currencies) === wallet - 200000 && ChurchOfTalos.store.cuirass() === cuirass.id, "Buying the Cuirass costs 20 Platinum, and it stays on offer");
  const spellId = stock.spells.find(id => !knownSpells.includes(id) && Shop.spellPrice(getSpell(id)) <= ShopRules.walletValue(currencies));
  assert(spellId && ChurchOfTalos.store.learn(spellId) && knownSpells.includes(spellId), "A lightning spell can be learned there");
  const caster = stock.tower.find(entry => getItem(entry.id).priceSilver <= ShopRules.walletValue(currencies));
  assert(caster && ChurchOfTalos.store.buy(caster.id) && !ChurchOfTalos.store.buy(caster.id), "Each caster sells once");
  const savedStock = normalizeCharacter(backupPayload().character).shopState.locations["church-of-talos"];
  assert(savedStock.tower.length === 6 && savedStock.relics[0].id === cuirass.id, "The Reliquary's stock is saved");
  const spellsBefore = stock.spells.join(), castersBefore = stock.tower.map(entry => entry.id).join();
  worldState.minutes += 360; Shop.onTimeChange();
  const turned = ChurchOfTalos.store.stock();
  assert(turned.spells.join() !== spellsBefore || turned.tower.map(entry => entry.id).join() !== castersBefore, "Its selection turns over with the shops' stock");
  ChurchOfTalos.store.close();
  return checks;
}

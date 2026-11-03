async function runCinderBossChecks() {
  const assert = (condition, message) => { if (!condition) throw new Error(message); };
  const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
  const clone = value => structuredClone(value);
  const urId = "ur-mat-false-dreamer-unique-level-90", ornnId = "ornn-god-of-the-forge-unique-level-48", ramId = "vulcanic-ram-level-50";
  const keening = ForgeRules.encode(ForgeRules.generateMasterwork({ key: "keening", level: 100, random: () => 0.5 })), sunder = ForgeRules.encode(ForgeRules.generateMasterwork({ key: "sunder", level: 100, random: () => 0.5 })), hammer = ForgeRules.encode(ForgeRules.generateMasterwork({ key: "anvil-hammer", level: 100, random: () => 0.5 })), plate = ForgeRules.encode(ForgeRules.generateMasterwork({ key: "godforged-plate", level: 100, random: () => 0.5 })), wraith = "artifact-wraithguard-artifact";
  const hammerTemplate = "masterwork-anvil-hammer", forgedHammer = loot => loot.some(id => id.startsWith("weapon-masterwork-anvil-hammer-")), isForgedPlate = id => id.startsWith("armor-masterwork-godforged-plate-");
  const charge = getSpell("vulcanic-charge-fire-unique");
  assert(charge?.actionPoints === 5 && charge.tier === 6 && charge.requiredCasterEnemyId === ramId && !charge.shopAvailable && !charge.catalogueAvailable, "Vulcanic Charge loads as an exclusive, five-AP Tier 6 spell");
  for (const id of [wraith]) {
    const item = getItem(id);
    assert(item?.rarity === "masterwork" && !item.shopAvailable && !item.automaticDrop, "Unique boss gear is loaded and excluded from shops/automatic drops: " + id);
  }
  assert([keening, sunder].every(id => getItem(id)?.rarity === "masterwork" && !getItem(id).shopAvailable && getItem(id).forged?.masterwork), "Keening and Sunder load as forged masterworks that no shop sells");
  assert([hammer, plate].every(id => getItem(id)?.rarity === "masterwork" && !getItem(id).shopAvailable), "Ornn's forged masterworks load and are not sold in shops");
  assert(getItem(plate).armorValue > getItem(ForgeRules.encode(ForgeRules.generateArmor({ type: "full-plate", level: 100, source: "shop", rarity: "legendary", random: () => 0.5 }))).armorValue, "Godforged Plate has more armor than any other full plate");
  const scores = Music.scoreInfo();
  assert(scores["ornn-god-of-the-forge"].kind === "remaster" && scores["ornn-forge-erupts"].kind === "remaster" && scores["ur-mat-false-dreamer"].kind === "remaster" && scores["ur-mat-lucid-change"].kind === "remaster" && scores["ornn-god-of-the-forge"].bars >= 24, "Both bosses have distinct, fully arranged themes");
  const fresh = (weapon, artifact, armor) => {
    Combat.restore(null);
    const c = defaultCharacter();
    c.level = 100; c.raceId = "human";
    for (const key of Object.keys(c.stats)) c.stats[key] = 100;
    c.stats.speed = 500; c.stats.luck = 0;
    c.inventory = [weapon, artifact, armor].filter(Boolean);
    c.equipment.primaryHand = weapon || null;
    c.equipment.offHand = getItem(weapon)?.handed === 2 ? weapon : null;
    c.equipment.armor = armor || null; c.equipment.artifact1 = artifact || null;
    c.currentHp = 100000; c.currentMana = 100000;
    applyCharacter(normalizeCharacter(c));
  };
  fresh();
  worldState.regionId = "cinder-isle"; WorldRules.setActiveRegion(worldState.regionId);
  const town = WorldRules.getRegion("cinder-isle").settlements[0];
  worldState.position = { x: town.x, y: town.y };
  const available = new Set();
  for (let i = 0; i < 30; i++) {
    worldState.guild.offers = []; Guild.render(); document.getElementById("guild-quest").click();
    const hunts = worldState.guild.offers.filter(offer => offer.type === "great-hunt");
    assert(hunts.length === 1 && ["ur-mat-false-dreamer", "ornn-god-of-the-forge"].includes(hunts[0].uniqueId), "Cinder Isle offers one local Great Hunt");
    assert(hunts[0].regionId === "cinder-isle" && WorldRules.zoneAt(hunts[0].point, hunts[0].regionId) === hunts[0].zone, "Hunt marker lies inside its intended volcanic zone");
    available.add(hunts[0].uniqueId);
  }
  assert(available.size === 2, "Both Cinder Isle Great Hunts can be offered");

  async function start(name) {
    assert(await Combat.startEncounter([name], "Cinder boss test", "great-hunt"), "Encounter starts: " + name);
    document.getElementById("combat-sheet-button").click(); // suspend automatic AI while inspecting
    return Combat.serialize();
  }
  async function beginOwnTurn(state, actor) {
    state.current = null; state.actors.forEach(entry => { entry.counter = entry.id === actor.id ? 1000 : 0; });
    Combat.restore(state); Combat.onSheetChange(); await wait(60);
    return Combat.serialize();
  }
  let state = await start("Ur-Mat, the False Dreamer"), boss = state.actors.find(actor => actor.enemyDefinitionId === urId);
  assert(boss?.character.raceId === "dark-elf" && boss.character.level === 90 && boss.character.classLevels.sharmat === 3 && boss.character.classLevels["raid-boss"] === 1, "Ur-Mat has his requested race, level and classes");
  assert(Object.values(boss.character.classLevels).reduce((a, b) => a + b, 0) === 90 && boss.character.knownSpells.length === 21, "Ur-Mat spends his perk budget and every configured spell loads");
  assert(boss.turnStartManaPercent === 5 && boss.character.activeSkills["sharmat:lucid-change"].charges === 1, "Ur-Mat has regeneration and one reserved Lucid Change charge");
  assert(Combat.musicTheme() === "ur-mat-false-dreamer", "Ur-Mat selects his theme");
  state.current = boss.id; state.ap = 8; Combat.restore(state);
  let began = performance.now(), decision = Combat.previewAiDecision(boss.id);
  assert(decision && decision.skillKey !== "sharmat:lucid-change" && performance.now() - began < 1500, "Ur-Mat's opening AI is responsive and reserves Lucid Change");
  state = Combat.serialize(); boss = state.actors.find(actor => actor.enemyDefinitionId === urId);
  boss.character.currentMana = 0;
  state = await beginOwnTurn(state, boss); boss = state.actors.find(actor => actor.enemyDefinitionId === urId);
  assert(boss.character.currentMana === Math.floor(boss.resourceMax.mana * .05) && boss.phaseIndex === 0, "Ur-Mat restores exactly five percent Mana on his ordinary turn");
  boss.character.currentHp = Math.floor(boss.resourceMax.hp * .15); boss.character.currentMana = 0;
  boss.effects.push({ type: "stun", amount: 1, expiresTurn: boss.turns + 5 });
  const originalCaps = clone(boss.resourceMax);
  state = await beginOwnTurn(state, boss); boss = state.actors.find(actor => actor.enemyDefinitionId === urId);
  const cinematic = document.getElementById("boss-intro-cinematic");
  assert(boss.phaseIndex === 1 && boss.character.activeSkills["sharmat:lucid-change"].active && boss.character.activeSkills["sharmat:lucid-change"].charges === 0, "At fifteen percent Ur-Mat activates Lucid Change exactly once");
  boss.effects = boss.effects.filter(effect => effect.type !== "stun");
  assert(boss.character.currentHp === originalCaps.hp && boss.character.currentMana === originalCaps.mana && JSON.stringify(boss.resourceMax) === JSON.stringify(originalCaps), "Phase-triggered Lucid Change restores full HP and Mana, exempt from the enemy healing reduction");
  assert(!cinematic.hidden && cinematic.dataset.boss === "ur-mat-lucid" && !document.getElementById("combat-view").inert, "Lucid Change opens its skippable cutscene without locking the interface");
  document.getElementById("boss-intro-skip").click();
  assert(cinematic.hidden && Combat.serialize().actors.find(actor => actor.id === boss.id).phaseFlashUntil === 0, "Skipping ends the pause");
  state = await beginOwnTurn(Combat.serialize(), boss); boss = state.actors.find(actor => actor.enemyDefinitionId === urId);
  assert(boss.phaseIndex === 1 && boss.character.currentHp === originalCaps.hp - Math.floor(originalCaps.hp * .1) && cinematic.hidden, "Later turns pay Lucid Change's ten-percent HP downside and do not repeat the phase");
  fresh(); state = await start("Ornn, God of the Forge"); boss = state.actors.find(actor => actor.enemyDefinitionId === ornnId);
  assert(boss?.character.level === 48 && boss.character.raceId === "giant" && Object.values(boss.character.classLevels).reduce((a, b) => a + b, 0) === 48, "Ornn is a level 48 Giant with a legal perk budget");
  assert(Combat.previewValues(boss.character).evasion === 0 && Combat.musicTheme() === "ornn-god-of-the-forge", "Ornn's plate locks evasion and selects his theme");
  assert(boss.dropTable.find(drop => drop.itemId === hammerTemplate)?.guaranteedOnFirstKill && boss.dropTable.find(drop => drop.itemId === hammerTemplate)?.chance === 12 && boss.dropTable.find(drop => drop.itemId === "masterwork-godforged-plate")?.chance === 8, "Ornn has first-kill hammer protection and the requested later drop chances");
  boss.character.currentHp = Math.floor(boss.resourceMax.hp * .5);
  state = await beginOwnTurn(state, boss); boss = state.actors.find(actor => actor.enemyDefinitionId === ornnId);
  const ram = state.actors.find(actor => actor.summonDefinitionId === ramId);
  assert(boss.phaseIndex === 1 && ram?.character.level === 50 && ram.character.raceId === "elemental" && ram.team === "enemy" && ram.character.knownSpells.includes(charge.id), "Ornn summons his level 50 Elemental ally at half HP");
  assert(!document.getElementById("boss-intro-cinematic").hidden && document.getElementById("boss-intro-cinematic").dataset.boss === "ornn-phase", "The forge summon has its own cutscene");
  document.getElementById("boss-intro-skip").click();
  state = await beginOwnTurn(Combat.serialize(), boss);
  assert(state.actors.filter(actor => actor.summonDefinitionId === ramId).length === 1 && cinematic.hidden, "The Ram is not summoned repeatedly or after restoring combat");
  state.current = ram.id; state.ap = 5; Combat.restore(state);
  assert(Combat.previewAiDecision(ram.id)?.kind, "The Ram can forecast its exclusive attack spell");
  fresh(); assert(await Combat.startEncounter(["Vulcanic Ram"], "Charge regression"), "The Ram can also be tested independently");
  state = Combat.serialize(); const chargeActor = state.actors.find(actor => actor.enemyDefinitionId === ramId);
  chargeActor.team = "player"; chargeActor.companion = true;
  companions.push({ id: chargeActor.id, name: chargeActor.name, character: chargeActor.character });
  const targetTemplate = clone(chargeActor); targetTemplate.companion = false; targetTemplate.summoned = false; targetTemplate.team = "enemy"; targetTemplate.character.knownSpells = []; targetTemplate.character.classLevels = {}; targetTemplate.character.equipment = defaultCharacter().equipment;
  targetTemplate.resourceMax = { hp: 100000, mana: 1000 }; targetTemplate.character.currentHp = 100000; targetTemplate.effects = []; targetTemplate.baseArmor = 0;
  for (let i = 0; i < 2; i++) state.actors.push({ ...clone(targetTemplate), id: "charge-target-" + i, name: "Charge Target " + i });
  state.current = chargeActor.id; state.ap = 5; Combat.restore(state); document.getElementById("combat-view-toggle").click();
  const random = Math.random; Math.random = () => 0;
  try { document.getElementById("combat-spell-button").click(); document.querySelector(`[data-combat-ability="${charge.id}"]`)?.click(); }
  finally { Math.random = random; }
  state = Combat.serialize();
  assert(state.ap === 0 && state.actors.filter(actor => actor.id.startsWith("charge-target-")).every(actor => actor.character.currentHp < 100000) && state.log.filter(line => line.includes("Vulcanic Charge deals") && line.includes("fire")).length === 2, "Vulcanic Charge hits all enemies, adds Fire damage and spends five AP");
  companions = []; fresh();

  async function weaponBattle(weapon, artifact, armor) {
    fresh(weapon, artifact, armor);
    assert(await Combat.startEncounter(["Goblin", "Goblin"], "Unique gear regression"), "Gear regression starts");
    const fixture = Combat.serialize(); fixture.current = "player"; fixture.ap = 5;
    fixture.actors.filter(actor => actor.team === "enemy").forEach(actor => {
      actor.resourceMax = { hp: 100000, mana: 1000 }; actor.character.currentHp = 100000; actor.character.currentMana = 300;
      actor.character.classLevels = {}; actor.effects = []; actor.baseArmor = 0; actor.resistances = {}; actor.character.equipment = defaultCharacter().equipment;
      actor.character.stats.speed = 1; actor.character.stats.dexterity = 0; actor.character.stats.luck = 0;
    });
    return fixture;
  }
  function attack(fixture, roll = 0) {
    Combat.restore(fixture); document.getElementById("combat-view-toggle").click();
    const random = Math.random; Math.random = () => roll;
    try { document.querySelector('[data-action="attack"]').click(); } finally { Math.random = random; }
    return Combat.serialize();
  }
  state = await weaponBattle(keening, wraith);
  const immune = state.actors.filter(actor => actor.team === "enemy")[1];
  immune.baseArmor = 100000; immune.resistances = { physical: 100 }; immune.effects.push({ type: "incoming-attack-damage-percent", amount: -100, expiresTurn: 100 }, { type: "hp-barrier", amount: 100000, expiresTurn: 100 });
  let result = attack(state), enemies = result.actors.filter(actor => actor.team === "enemy");
  const dealt = 100000 - enemies[0].character.currentHp;
  assert(dealt > 0 && 100000 - enemies[1].character.currentHp === Math.floor(dealt * .4), "Keening echoes 40% of the actual hit through reductions and barriers: " + JSON.stringify({ dealt, echoed: 100000 - enemies[1].character.currentHp, log: result.log.slice(-5) }));
  const protection = result.actors.find(actor => actor.id === "player").effects.filter(effect => effect.key === "wraithguard:protection");
  assert(protection.length === 1 && protection[0].amount === -60, "Wraithguard grants sixty-percent protection from a Masterwork melee hit");
  result = attack(result);
  assert(result.actors.find(actor => actor.id === "player").effects.filter(effect => effect.key === "wraithguard:protection").length === 1, "Wraithguard refreshes instead of stacking");
  const playerActor = result.actors.find(actor => actor.id === "player");
  result = await beginOwnTurn(result, playerActor);
  assert(!result.actors.find(actor => actor.id === "player").effects.some(effect => effect.key === "wraithguard:protection"), "Wraithguard expires on the wearer's next turn");
  state = await weaponBattle(sunder); currentMana = 0;
  for (let i = 0; i < 2; i++) {
    const ally = clone(state.actors[1]); ally.id = "mana-ally-" + i; ally.team = "player"; ally.character.currentMana = 0;
    state.actors.push(ally);
  }
  const summon = clone(state.actors.at(-1)); summon.id = "mana-summon"; summon.summoned = true; state.actors.push(summon);
  result = attack(state);
  assert(result.actors.find(actor => actor.team === "enemy").character.currentMana === 270 && currentMana === 10 && result.actors.filter(actor => actor.id.startsWith("mana-ally-")).every(actor => actor.character.currentMana === 10) && result.actors.find(actor => actor.id === "mana-summon").character.currentMana === 0, "Sunder divides ten-percent Mana drain across the living party, ignoring summons");
  state = await weaponBattle(hammer, null, plate);
  permanentDefenseBonuses.evasion = 1000;
  state.actors[0].effects.push({ type: "defense-flat", target: "evasion", amount: 1000, expiresTurn: 100 }); state.actors[0].stance = "evade";
  result = attack(state);
  assert(result.log.some(line => line.includes("Anvil Hammer deals") && line.includes("from Armor")), "Anvil Hammer adds its armor-based damage");
  assert(Combat.previewValues(normalizeCharacter({ ...defaultCharacter(), equipment: { ...defaultCharacter().equipment, armor: plate }, inventory: [plate], permanentDefenseBonuses: { evasion: 500, armor: 0 } })).evasion === 0, "Godforged Plate ignores positive evasion bonuses on the character and companion sheet");
  Combat.restore(null);

  fresh();
  async function claimHammer(roll, keep = true) {
    let fixture = await start("Ornn, God of the Forge");
    fixture.actors.filter(actor => actor.team === "enemy").forEach(actor => { actor.character.currentHp = 0; }); fixture.current = null;
    const random = Math.random; Math.random = () => roll;
    try { Combat.restore(fixture); Combat.onSheetChange(); await wait(60); } finally { Math.random = random; }
    const loot = Combat.serialize().lootItems;
    document.getElementById(keep ? "combat-loot-keep" : "combat-loot-discard").click(); Combat.restore(null);
    return loot;
  }
  assert(forgedHammer(await claimHammer(.99, false)) && classProgress.firstKillDrops.includes(hammerTemplate) && normalizeCharacter({ ...defaultCharacter(), classProgress }).classProgress.firstKillDrops.includes(hammerTemplate), "The first kill guarantees Anvil Hammer even if discarded, and the kill record persists");
  assert(!forgedHammer(await claimHammer(.99)) && !inventory.some(isForgedPlate), "Later hammer drops and Godforged Plate remain chance-based");
  assert(forgedHammer(await claimHammer(0)) && inventory.some(isForgedPlate), "Both Ornn items can drop on successful rolls");
  Combat.restore(null);
  await runOrnnChecks();
  Combat.restore(null);
  await runUrMatChecks();
  Combat.restore(null);
}

// Ornn rework (2026-10-11): reforging at the anvil (tempers, cracked metal, the Ram guarding the
// anvil), his look, intro, phase cutscene and work-song music, and the Anvil Hammer and Godforged
// Plate as forged masterworks.
async function runOrnnChecks() {
  let checks = 0;
  const assert = (condition, message) => { if (!condition) throw Error(message); checks++; };
  const $ = id => document.getElementById(id);
  Combat.restore(null);
  const source = await (await fetch("scripts/combat.js")).text();
  const marker = "  window.Combat = {";
  const script = document.createElement("script");
  script.textContent = source.replace(marker, "  window.__ornnChecks = { actorById, hp, values, setHp, enemyAction, endTurn, isControlledTurn, currentActor, getBattle: () => battle, stageLook, reforgeState, setTemper, redirectedGuardTarget, attack, actionPointMaximum };\n" + marker);
  document.head.append(script);
  await waitForSourceFiles();
  const hooks = window.__ornnChecks;
  if (window.BattleStage) window.BattleStage.enabled = () => false;
  if (window.GameSettings) window.GameSettings.enemyDelay = () => 1e9;
  const F = ForgeRules;

  // His sheet: reforging, the phase cutscene, and his own forged Hammer and Plate.
  const raw = await (await fetch("enemies/" + encodeURIComponent("Ornn, God of the Forge") + ".json")).json();
  assert(raw.aiProfile === "ornn" && raw.introCinematic === "ornn" && raw.phaseTransitions[0].thresholdHpPercent === 50 && raw.phaseTransitions[0].cinematic === "ornn-phase", "Ornn has his own AI, intro and phase cutscene");
  assert(raw.reforge.everyTurns === 3 && raw.reforge.spoilPercent === 15 && raw.reforge.spoilApLoss === 2 && raw.reforge.guard === "vulcanic-ram-level-50" && raw.reforge.tempers.map(temper => temper.id).join() === "molten-edge,hardened-plate,spiked-plate", "He reforges every three turns: Molten Edge, Hardened Plate or Spiked Plate, cracked by 15% of his health");
  const hammerDrop = raw.drops.find(drop => drop.itemId === "masterwork-anvil-hammer"), plateDrop = raw.drops.find(drop => drop.itemId === "masterwork-godforged-plate");
  assert(hammerDrop?.chance === 12 && hammerDrop.guaranteedOnFirstKill && plateDrop?.chance === 8 && raw.drops.length === 2, "He drops the Anvil Hammer (12%, sure on the first kill) and the Godforged Plate (8%)");
  const worn = [raw.equipment.primaryHand, raw.equipment.armor].map(getItem);
  assert(worn[0]?.forged?.masterwork === "anvil-hammer" && worn[0].forged.type === "mace" && worn[0].forged.parts[0].maker === "hearthforge" && worn[1]?.forged?.masterwork === "godforged-plate" && worn[1].forged.parts[0].maker === "hearthforge" && raw.noDropItems.includes(worn[0].id) && raw.noDropItems.includes(worn[1].id), "He wields forged copies of his Hammer and Plate, which never drop as ordinary gear");
  const hammerTemplate = getItem("masterwork-anvil-hammer"), plateTemplate = getItem("masterwork-godforged-plate");
  assert(hammerTemplate?.effects.some(effect => effect.kind === "bonus-damage-from-armor-percent" && effect.amount === 15) && plateTemplate?.effects.some(effect => effect.kind === "defense-cap" && effect.target === "evasion" && effect.amount === 0), "The Hammer keeps its armor-to-damage effect and the Plate locks Evasion at 0");
  const plate = getItem(F.encode(F.generateMasterwork({ key: "godforged-plate", level: 60, random: () => 0.5 }))), legendary = getItem(F.encode(F.generateArmor({ type: "full-plate", level: 60, source: "shop", rarity: "legendary", random: () => 0.5 })));
  assert(plate.armorValue > legendary.armorValue, "The Godforged Plate has more armor than any other full plate");
  const migrated = normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: 30, inventory: ["weapon-anvil-hammer-meele", "armor-godforged-plate-heavy"], classProgress: { ...defaultCharacter().classProgress, firstKillDrops: ["weapon-anvil-hammer-meele"] } });
  assert(migrated.inventory.map(id => getItem(id)?.forged?.masterwork).join() === "anvil-hammer,godforged-plate" && migrated.classProgress.firstKillDrops.includes("masterwork-anvil-hammer"), "Old copies become the forged masterworks, and a past first-kill Hammer still counts");

  // A sturdy hero on the Lava Fields.
  const blade = F.encode(F.generate({ type: "longsword", level: 51, source: "shop", random: () => 0.5 }));
  const mail = F.encode(F.generateArmor({ type: "full-plate", level: 51, source: "shop", random: () => 0.5 }));
  const c = defaultCharacter();
  Object.assign(c, { raceId: "human", level: 51, inventory: [blade, mail], equipment: { ...defaultEquipment, primaryHand: blade, armor: mail } });
  c.stats = { strength: 120, vitality: 900, mind: 25, dexterity: 80, intelligence: 22, wisdom: 25, speed: 60, luck: 30 };
  applyCharacter(normalizeCharacter(c));
  currentHp = 1e6;
  worldState.regionId = "cinder-isle";
  assert(await Combat.startEncounter(["Ornn, God of the Forge"], "Ornn test", "great-hunt"), "His fight starts");
  const intro = $("boss-intro-cinematic");
  assert(!intro.hidden && intro.dataset.boss === "ornn" && intro.querySelector(".bi-boss svg") && intro.querySelectorAll(".bi-or-spark").length > 10, "He looks up from his anvil in his intro");
  $("boss-intro-skip").click();
  assert(Combat.musicTheme() === "ornn-god-of-the-forge" && ["ornn-god-of-the-forge", "ornn-forge-erupts"].every(id => MusicThemes.all().some(theme => theme.id === id)), "His work-song plays");
  const battle = hooks.getBattle(), ornn = battle.actors.find(actor => actor.enemyDefinitionId === "ornn-god-of-the-forge-unique-level-48"), player = () => hooks.actorById("player");
  const look = Sprites.forActor(hooks.stageLook(ornn)).svg;
  assert(look.includes(SpriteKit.tone("#8a5a38")) && look.includes(SpriteKit.tone("#b08a4a")), "He wears his smith's apron and Godforged pauldrons");
  const heal = () => { currentHp = hooks.values(player()).maxHp; };
  const step = () => { if (hooks.isControlledTurn()) { heal(); hooks.endTurn(); } else hooks.enemyAction(); };
  const until = (condition, limit = 300) => { for (let i = 0; i < limit && !condition() && Combat.active(); i++) step(); return condition(); };

  // At the end of his first turn he heats the metal; the heroes see what he is forging.
  assert(until(() => battle.reforge?.working), "He turns to his anvil after his first turn");
  const working = battle.reforge.working, threshold = battle.reforge.threshold;
  assert(threshold === Math.floor(hooks.values(ornn).maxHp * 0.15) && battle.log.some(entry => String(entry).includes("turns to his anvil and heats the metal")), "He heats the metal, and 15% of his health cracks it");
  assert(Combat.serialize() && hooks.stageLook(ornn).reforging === true, "His embers flare while he works");
  // Enough damage cracks it: no temper, and he loses AP on his next turn.
  battle.reforge.damage = threshold;
  assert(until(() => hooks.currentActor()?.id === ornn.id && !battle.reforge.working), "His next turn comes");
  assert(!battle.reforge.temper && battle.reforge.spoiled === 1 && battle.log.some(entry => String(entry).includes("The metal cracks")) && battle.ap === hooks.actionPointMaximum(ornn) - 2, "The cracked metal spoils the temper and costs him 2 AP");
  // Left alone, the next temper holds.
  battle.reforge.nextTurn = ornn.turns;
  assert(until(() => battle.reforge.working) && until(() => battle.reforge.temper), "He forges again, and this time the temper holds");
  const temper = battle.reforge.temper;
  assert(ornn.effects.some(effect => effect.temper) && hooks.stageLook(ornn).temper === temper && battle.log.some(entry => String(entry).includes("quenches the glowing metal")), "The temper sets and shows on him");

  // Molten Edge sears and burns; Spiked Plate bites back at blades.
  // (His anvil rests meanwhile, so the tempers set here hold.)
  Object.assign(battle.reforge, { working: null, nextTurn: 1e9 });
  hooks.setTemper(hooks.reforgeState(), "molten-edge");
  assert(ornn.effects.some(effect => effect.type === "molten-strike") && !ornn.effects.some(effect => effect.type === "melee-retaliation-percent"), "One temper replaces the last");
  assert(until(() => hooks.currentActor()?.id === ornn.id), "His turn comes");
  const seared = battle.log.length;
  for (let i = 0; i < 8 && !battle.log.slice(seared).some(entry => String(entry).includes("Molten Edge sears")); i++) { battle.ap = 8; heal(); hooks.attack(ornn, player()); }
  assert(battle.log.slice(seared).some(entry => String(entry).includes("Molten Edge sears")) && player().effects.some(effect => effect.name === "Molten Burn"), "Molten Edge adds Fire damage and leaves a burn");
  hooks.setTemper(hooks.reforgeState(), "spiked-plate");
  assert(until(() => hooks.isControlledTurn()), "The hero's turn comes");
  const spiked = battle.log.length;
  for (let i = 0; i < 8 && !battle.log.slice(spiked).some(entry => String(entry).includes("Spiked Plate turns")); i++) { battle.ap = 5; hooks.attack(player(), ornn); }
  assert(battle.log.slice(spiked).some(entry => String(entry).includes("Spiked Plate turns")), "Spiked Plate turns melee damage back as fire");

  // At half health the forge erupts: the cutscene, the Ram and the savage work-song.
  ornn.character.currentHp = Math.floor(hooks.values(ornn).maxHp * 0.45);
  assert(until(() => ornn.phaseIndex >= 1), "The forge erupts at half health");
  assert(!intro.hidden && intro.dataset.boss === "ornn-phase", "The eruption plays its own cutscene");
  $("boss-intro-skip").click();
  const ram = battle.actors.find(actor => actor.summonDefinitionId === "vulcanic-ram-level-50");
  assert(ram && Combat.musicTheme() === "ornn-forge-erupts", "The Vulcanic Ram bursts out, and the work-song turns savage");
  // The Ram guards the anvil while he works.
  battle.reforge.nextTurn = ornn.turns;
  assert(until(() => battle.reforge.working || hooks.hp(ram) <= 0), "He works at the anvil again");
  if (hooks.hp(ram) > 0) {
    assert(ornn.effects.some(effect => effect.type === "guardian-ward" && effect.guardianId === ram.id) && hooks.redirectedGuardTarget(player(), ornn, "melee")?.id === ram.id, "The Ram takes the blows meant for him while he works");
    assert(until(() => !battle.reforge.working) && !ornn.effects.some(effect => effect.reforgeWard), "The guard ends when his work is done");
  }
  hooks.setHp(ornn, 0);
  until(() => !Combat.active(), 20);
  assert(!Combat.active() || battle.reforge.ended, "His fall ends the forging");
  Combat.restore(null);
  return checks;
}

// Ur-Mat rework (2026-10-13): the dream laws (seven laws in a fixed cycle from a random start,
// rewritten every third turn, two at once after Lucid Change), his sorcerer's AI, look, intro,
// Lucid Change cutscene and dream waltz, and Keening and Sunder as forged masterworks.
async function runUrMatChecks() {
  let checks = 0;
  const assert = (condition, message) => { if (!condition) throw Error(message); checks++; };
  const $ = id => document.getElementById(id);
  Combat.restore(null);
  const source = await (await fetch("scripts/combat.js")).text();
  const marker = "  window.Combat = {";
  const script = document.createElement("script");
  script.textContent = source.replace(marker, "  window.__urMatChecks = { actorById, hp, values, enemyAction, endTurn, isControlledTurn, currentActor, getBattle: () => battle, stageLook, stageSnapshot, dreamState, applyLawsTo, actionAllowed, healHp, directDamage, hurt, cast, aiCandidates, advance };\n" + marker);
  document.head.append(script);
  await waitForSourceFiles();
  const hooks = window.__urMatChecks;
  if (window.BattleStage) window.BattleStage.enabled = () => false;
  if (window.GameSettings) window.GameSettings.enemyDelay = () => 1e9;

  // His sheet: the dream laws, his own AI, intro and Lucid Change cutscene; he carries nothing.
  const raw = await (await fetch("enemies/" + encodeURIComponent("Ur-Mat, the False Dreamer") + ".json")).json();
  const laws = raw.dreamLaws;
  assert(raw.aiProfile === "ur-mat" && raw.introCinematic === "ur-mat" && raw.phaseTransitions[0].thresholdHpPercent === 15 && raw.phaseTransitions[0].unskippable && raw.phaseTransitions[0].cinematic === "ur-mat-lucid" && raw.phaseTransitions[0].activateSkill === "sharmat:lucid-change", "Ur-Mat has his own AI and intro, and Lucid Change wakes him at 15% with its own cutscene");
  assert(laws.everyTurns === 3 && laws.randomStart && laws.doubleAfterPhase && laws.laws.map(law => law.id).join() === "blades-of-glass,rebounding-words,inverted-mercy,slowed-time,echoing-spells,sealed-names,dreamers-ward", "Seven dream laws in a fixed cycle, rewritten every third turn from a random start, two at once after Lucid Change");
  assert(!raw.inventory.length && Object.values(raw.equipment).every(slot => slot === null) && Object.values(raw.classLevels).reduce((a, b) => a + b, 0) === 90 && raw.classLevels.summoner < 3 && raw.classLevels.dreamweaver === 5, "He casts bare-handed, with no Overprotective and a Dreamweaver's training");
  assert(raw.maxHpPercent === 500 && raw.stats.intelligence === 100 && raw.stats.wisdom === 100 && raw.stats.speed === 110 && raw.stats.mind === 352, "Tuned for about 10% wins by a prepared Level 93 party: 500% HP, Intelligence and Wisdom 100, Speed 110, his vast Mind kept");
  const drop = id => raw.drops.find(entry => entry.itemId === id)?.chance;
  assert(drop("masterwork-keening") === 10 && drop("masterwork-sunder") === 10 && drop("artifact-wraithguard-artifact") === 10 && drop("key-item-gem-of-false-dreams") === 5 && raw.drops.length === 4, "He drops Keening, Sunder and Wraithguard at 10% and the Gem of False Dreams at 5%");
  const keening = getItem(ForgeRules.encode(ForgeRules.generateMasterwork({ key: "keening", level: 90, random: () => 0.5 }))), sunder = getItem(ForgeRules.encode(ForgeRules.generateMasterwork({ key: "sunder", level: 90, random: () => 0.5 })));
  assert(keening?.forged?.type === "longsword" && keening.forged.parts[0].maker === "elven" && keening.effects.some(effect => effect.kind === "unreducible-cleave-percent" && effect.amount === 40), "Keening is an Elven longsword whose hits echo 40% onto every other enemy");
  assert(sunder?.forged?.type === "greathammer" && sunder.forged.parts[0].maker === "hearthforge" && sunder.effects.some(effect => effect.kind === "party-mana-drain-on-hit-percent" && effect.amount === 10), "Sunder is a Hearthforge greathammer that drains 10% Mana on every hit");
  const migrated = normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: 60, inventory: ["weapon-keening-meele", "weapon-sunder-meele"] });
  assert(migrated.inventory.map(id => getItem(id)?.forged?.masterwork).join() === "keening,sunder", "Old Keening and Sunder become the forged masterworks");

  // A sturdy hero in the Caldera.
  const blade = ForgeRules.encode(ForgeRules.generate({ type: "longsword", level: 93, source: "shop", random: () => 0.5 }));
  const c = defaultCharacter();
  Object.assign(c, { raceId: "human", level: 93, inventory: [blade], equipment: { ...defaultEquipment, primaryHand: blade }, knownSpells: ["fire-bolt-fire-damage", "lesser-heal-holy-support"] });
  c.stats = { strength: 120, vitality: 400000, mind: 60, dexterity: 80, intelligence: 60, wisdom: 60, speed: 60, luck: 30 };
  applyCharacter(normalizeCharacter(c));
  currentHp = 1e9; currentMana = 1e6;
  worldState.regionId = "cinder-isle";
  assert(await Combat.startEncounter(["Ur-Mat, the False Dreamer"], "Ur-Mat test", "great-hunt"), "His fight starts");
  const intro = $("boss-intro-cinematic");
  assert(!intro.hidden && intro.dataset.boss === "ur-mat" && intro.querySelector(".bi-boss svg") && intro.querySelectorAll(".bi-um-glyph").length >= 6, "The Caldera falls into his dream in his intro");
  $("boss-intro-skip").click();
  assert(Combat.musicTheme() === "ur-mat-false-dreamer" && ["ur-mat-false-dreamer", "ur-mat-lucid-change"].every(id => MusicThemes.all().some(theme => theme.id === id)), "His dream waltz plays");
  const battle = hooks.getBattle(), player = () => hooks.actorById("player");
  const urMat = battle.actors.find(actor => actor.enemyDefinitionId === "ur-mat-false-dreamer-unique-level-90");
  const lawIds = laws.laws.map(law => law.id), current = () => hooks.dreamState();
  assert(current() && current().laws.length === 1 && battle.log.some(entry => String(entry).includes("rewrites the dream")), "He writes the first law of the dream as the battle begins");
  const badge = hooks.stageSnapshot([]).season;
  assert(badge?.dream && badge.id === current().laws[0].id && badge.name === current().laws[0].name && hooks.stageLook(urMat).law === current().laws[0].id, "The badge and the glyph above him show the law in force");
  assert(Sprites.forActor(hooks.stageLook(urMat)).svg.includes(SpriteKit.tone("#231a38")), "He wears his starlit dream-robes");

  // The cycle: a new law every third of his turns, foretold a turn ahead, in the fixed order.
  const heal = () => { currentHp = hooks.values(player()).maxHp; };
  const step = () => { heal(); if (hooks.isControlledTurn()) hooks.endTurn(); else hooks.enemyAction(); };
  const until = (condition, limit = 400) => { for (let i = 0; i < limit && !condition() && Combat.active(); i++) step(); return condition(); };
  const first = battle.dream.index;
  assert(until(() => battle.dream.foretold), "The next law is foretold");
  assert(urMat.turns - battle.dream.startTurn === 2 && battle.log.slice(-40).some(entry => String(entry).includes("The dream stirs")), "It is foretold at his second turn under the law");
  assert(until(() => battle.dream.index !== first) && battle.dream.index === first + 1, "At his third turn he writes the next law of the cycle");
  assert(current().laws[0].id === lawIds[(first + 1) % lawIds.length], "The cycle keeps its order");

  // Each law, set in force by hand.
  // A forced law holds until these checks are done: he does not rewrite it meanwhile.
  const force = id => { Object.assign(battle.dream, { index: lawIds.indexOf(id), startTurn: urMat.turns + 1000, foretold: false }); battle.dream.serial += 1; for (const actor of battle.actors) hooks.applyLawsTo(actor); };
  force("blades-of-glass");
  assert(player().effects.some(effect => effect.dreamLaw && effect.type === "outgoing-attack-damage-percent" && effect.amount === -50 && effect.attackKinds.includes("melee")), "Blades of Glass halves the heroes' weapon damage");
  force("rebounding-words");
  const before = hooks.hp(player());
  hooks.hurt(urMat, 1000, "fire", false, true, player(), "spell");
  assert(before - hooks.hp(player()) > 0 && battle.log.slice(-6).some(entry => String(entry).includes("The words rebound")), "Rebounding Words turns part of a hero's spell back on the caster");
  heal();
  force("inverted-mercy");
  currentHp = 1000;
  const gained = hooks.healHp(player(), 400);
  assert(gained === 200 && currentHp === 1150 && battle.log.slice(-3).some(entry => String(entry).includes("Inverted Mercy wounds")), "Inverted Mercy halves healing and wounds the healed for a quarter of it");
  heal();
  force("slowed-time");
  assert(player().effects.some(effect => effect.dreamLaw && effect.type === "stat-percent" && effect.target === "speed" && effect.amount === -25), "Slowed Time takes a quarter of the heroes' Speed");
  const heroTurns = player().turns;
  until(() => hooks.isControlledTurn() && player().turns > heroTurns);
  assert(battle.log.slice(-8).some(entry => String(entry).includes("is slowed by the dream and loses 1 AP")), "Slowed Time costs every hero an AP each turn: " + JSON.stringify({ law: current()?.laws.map(law => law.id), current: hooks.currentActor()?.id, log: battle.log.slice(-10) }));
  force("sealed-names");
  assert(!hooks.actionAllowed(player(), "skill") && hooks.actionAllowed(player(), "spell"), "Sealed Names forbids class skills but not spells");
  force("dreamers-ward");
  assert(["incoming-weapon-damage-percent", "incoming-spell-damage-percent"].every(type => urMat.effects.some(effect => effect.dreamLaw && effect.type === type && effect.amount === -30)), "Dreamer's Ward shields him from 30% of all damage");
  force("echoing-spells");
  const bolt = getSpell("dark-bolt-dark-damage");
  battle.current = urMat.id; battle.ap = 8;
  assert(hooks.cast(urMat, player(), bolt) && battle.log.slice(-6).some(entry => String(entry).includes("echoes through the dream")), "Echoing Spells strikes his damaging spells a second time");
  assert(!hooks.aiCandidates(urMat).some(candidate => candidate.key === "sharmat:lucid-change"), "He never reaches for Lucid Change himself");

  // Lucid Change: no blow can skip it; he wakes at full strength and two laws hold at once.
  until(() => hooks.isControlledTurn());
  hooks.directDamage(urMat, 1e9, false, "fire", player());
  assert(urMat.phaseIndex === 1 && hooks.hp(urMat) === hooks.values(urMat).maxHp, "No blow can skip Lucid Change: crossing 15% wakes him at full health");
  assert(!intro.hidden && intro.dataset.boss === "ur-mat-lucid" && intro.querySelector(".bi-boss-before") && intro.querySelectorAll(".bi-um-crack").length === 1, "The dream cracks and shatters in its own cutscene");
  $("boss-intro-skip").click();
  assert(Combat.musicTheme() === "ur-mat-lucid-change" && hooks.stageLook(urMat).phase === 1, "The waltz shatters and races, and his eyes open");
  until(() => battle.dream.doubled);
  assert(current().laws.length === 2 && current().laws[1].id === lawIds[(battle.dream.index + 1) % lawIds.length] && hooks.stageSnapshot([]).season.name.includes(" and "), "Awake, he holds two laws at once");
  hooks.directDamage(urMat, 1e9, false, "fire", player());
  until(() => !Combat.active(), 30);
  assert(hooks.hp(urMat) <= 0 && battle.dream.ended && !player().effects.some(effect => effect.dreamLaw), "When he falls, the dream ends and its laws unravel");
  Combat.restore(null);
  return checks;
}

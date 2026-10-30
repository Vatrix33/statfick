async function runGaianBossChecks() {
  let checks = 0;
  const assert = (ok, message) => { if (!ok) throw Error(message); checks++; };
  const close = (actual, expected) => Math.abs(actual - expected) < .00001;
  const $ = id => document.getElementById(id), clone = value => structuredClone(value);
  Combat.restore(null); WorldMap.setAmbientEnabled(false);
  const source = await (await fetch("scripts/combat.js")).text(), script = document.createElement("script");
  script.textContent = source.replace("  window.Combat = {", "  window.__gaianChecks = { actorById, values, hp, mana, setHp, withActor, hurt, directDamage, applyCombatDamage, attack, refresh, useSkill, skillsOf, incomingDamageFactor, weaponAttackValues, attackLoadout, estimatedWeaponStrike, estimatedDamage, estimatedSpellDamage, redirectedGuardTarget, triggerLowHealthEquipment, awardDefeatedEnemyExperience, validateEnemy };\n  window.Combat = {");
  document.head.append(script); await waitForSourceFiles();
  const h = window.__gaianChecks;
  const hammer = getItem(ForgeRules.encode(ForgeRules.generateMasterwork({ key: "jackhammer", level: 100, random: () => 0.5 }))), barrier = getItem(ForgeRules.encode(ForgeRules.generateMasterwork({ key: "soulbound-barrier", level: 100, random: () => 0.5 }))), titan = getItem("artifact-titanfall-artifact");
  const forgedLoot = (loot, key) => loot.filter(id => id.startsWith("weapon-masterwork-" + key + "-") || id.startsWith("armor-masterwork-" + key + "-")).length;
  assert([hammer, barrier, titan].every(item => item?.rarity === "masterwork" && !item.shopAvailable) && !titan.automaticDrop, "All three Masterwork drops load without entering shop stock");
  assert(artifactEffectLabel(hammer.effects[0]).includes("20%") && artifactEffectLabel(barrier.effects[1]).includes("2%") && artifactEffectLabel(titan.effects[0]).includes("Once per combat"), "Equipment detail labels describe the actual new mechanics");
  const main = defaultCharacter(); main.raceId = "human"; main.level = 100;
  main.stats.strength = 200; main.stats.vitality = 1000; main.stats.mind = 1000; main.stats.intelligence = 200; main.stats.speed = 2000; main.stats.luck = 0;
  main.currentHp = 100000; main.currentMana = 100000;
  applyCharacter(normalizeCharacter(main));
  worldState.regionId = "verdigris-jungle"; WorldRules.setActiveRegion(worldState.regionId);
  const town = WorldRules.settlementsForRegion(worldState.regionId).find(city => city.guildServices?.includes("quest") || city.kind === "Castle city");
  assert(town, "The Jungle has an Adventurers Guild");
  worldState.position = { x: town.x, y: town.y }; Guild.render(); $("guild-quest").click();
  const questIndex = worldState.guild.offers.findIndex(offer => offer.uniqueId === "gaian-avatar-of-the-land"), quest = worldState.guild.offers[questIndex];
  assert(quest?.offeredAtLevel === 53 && quest.regionId === "verdigris-jungle" && quest.zone === "Old Growth" && WorldRules.zoneAt(quest.point, quest.regionId) === "Old Growth", "Gaian's level-53 Great Hunt appears at a valid Old Growth marker");
  document.querySelector(`[data-guild-quest="${questIndex}"]`).click(); worldState.position = clone(quest.point);
  const objective = Guild.objectiveAt(worldState.position);
  assert(objective?.enemies.length === 3, "The map encounter preview lists Gaian and both Cores");
  assert(await Combat.startEncounter(objective.enemies, "Gaian test", objective.kind), "The Jungle encounter starts"); $("combat-sheet-button").click();
  let state = Combat.serialize();
  const bossId = state.actors.find(actor => actor.enemyDefinitionId === "gaian-avatar-of-the-land-unique-level-53")?.id;
  let boss = h.actorById(bossId), cores = state.actors.filter(actor => actor.enemyDefinitionId === "jungle-core-level-50").map(actor => h.actorById(actor.id));
  assert(boss?.character.level === 53 && boss.character.classLevels["raid-boss"] === 1 && cores.length === 2 && cores.every(core => core.character.level === 50 && !core.summoned), "Gaian and exactly two ordinary Level 50 Elemental Cores enter battle");
  assert(h.values(boss).maxHp > h.values(cores[0]).maxHp && h.values(boss).vitality > h.values(boss).strength, "Gaian's build prioritizes HP and Vitality");
  const protection = boss.livingAllyProtection; boss.livingAllyProtection = null;
  const baseline = h.incomingDamageFactor(boss); boss.livingAllyProtection = protection;
  assert(close(h.incomingDamageFactor(boss), baseline * .5), "Two living Jungle Cores grant exactly 50% damage protection");
  const predicted = h.estimatedDamage(boss, 1000, "physical", false, false, h.actorById("player"), "melee"), before = h.hp(boss);
  h.hurt(boss, 1000, "physical", false, false, h.actorById("player"), "melee");
  assert(before - h.hp(boss) === predicted, "Actual protected damage matches the AI forecast");
  h.setHp(cores[0], 0); assert(close(h.incomingDamageFactor(boss), baseline * .75), "Killing one Core immediately reduces protection to 25%");
  h.setHp(cores[1], 0); assert(close(h.incomingDamageFactor(boss), baseline), "Killing both Cores removes their protection");
  state = Combat.serialize(); Combat.restore(state); boss = h.actorById(bossId);
  assert(close(h.incomingDamageFactor(boss), baseline), "Combat reload preserves the boss's protection mechanic and defeated Cores");
  const random = Math.random;
  state.actors.filter(actor => actor.team === "enemy").forEach(actor => { actor.character.currentHp = 0; }); Combat.restore(state);
  try { Math.random = () => .999; h.awardDefeatedEnemyExperience(); } finally { Math.random = random; }
  assert(!forgedLoot(Combat.serialize().lootItems, "jackhammer") && !forgedLoot(Combat.serialize().lootItems, "soulbound-barrier") && !Combat.serialize().lootItems.includes(titan.id), "Failed drop rolls grant none of the unique items automatically");
  Combat.restore(null); applyCharacter(normalizeCharacter(main));
  assert(await Combat.startEncounter(["Gaian, Avatar of the Land"]), "Direct boss encounters start with the same entourage"); $("combat-sheet-button").click();
  state = Combat.serialize(); assert(state.actors.filter(actor => actor.team === "enemy").length === 3, "Direct encounters automatically include exactly two Cores");
  for (const actor of state.actors.filter(actor => actor.team === "enemy")) {
    const aiState = clone(state); aiState.current = actor.id; aiState.ap = actor.enemyDefinitionId.includes("gaian") ? 8 : 5; Combat.restore(aiState);
    const start = performance.now(), decision = Combat.previewAiDecision(actor.id);
    assert(decision && performance.now() - start < 1000, `${actor.name} chooses an AI action without stalling`);
  }
  state.actors.filter(actor => actor.team === "enemy").forEach(actor => { actor.character.currentHp = 0; }); Combat.restore(state);
  try { Math.random = () => 0; h.awardDefeatedEnemyExperience(); } finally { Math.random = random; }
  assert(forgedLoot(Combat.serialize().lootItems, "jackhammer") === 1 && forgedLoot(Combat.serialize().lootItems, "soulbound-barrier") === 1 && Combat.serialize().lootItems.filter(id => id === titan.id).length === 1, "Successful drop rolls award each unique item once");
  h.refresh(); assert(!$("combat-loot-modal").hidden && ["Jackhammer", "Soulbound Barrier", "Titanfall"].every(name => $("combat-loot-modal").textContent.includes(name)), "All three rewards appear in the normal keep-or-discard loot window");
  Combat.restore(null);

  const fighter = clone(main); fighter.inventory = [hammer.id, barrier.id, titan.id];
  fighter.equipment = { ...defaultEquipment, primaryHand: hammer.id, offHand: hammer.id, armor: barrier.id };
  applyCharacter(normalizeCharacter(fighter));
  const maxHp = calculateMaxHpWithArtifacts(currentStats()), sheetArmor = calculateEquipmentDefense(currentStats()).armor;
  const barrierEffects = barrier.effects; barrier.effects = barrierEffects.filter(effect => effect.kind !== "armor-from-max-hp-percent");
  let plainArmor; try { plainArmor = calculateEquipmentDefense(currentStats()).armor; } finally { barrier.effects = barrierEffects; }
  assert(sheetArmor - plainArmor >= Math.floor(maxHp * .25) && sheetArmor - plainArmor <= Math.floor(maxHp * .25) * 1.6, "Soulbound Barrier grants 25% maximum HP as Armor (raised by the garb armor bonuses) on the character sheet");
  const itemPower = Math.floor(calculateItemPower(hammer)), armorBefore = calculateArmorForWeapon(currentStats());
  const hammerEffects = hammer.effects; hammer.effects = hammerEffects.filter(effect => effect.kind !== "weapon-power-from-armor-percent");
  let plainPower; try { plainPower = Math.floor(calculateItemPower(hammer)); } finally { hammer.effects = hammerEffects; }
  assert(itemPower - plainPower >= Math.floor(.2 * armorBefore) - 1 && itemPower - plainPower <= Math.ceil(.2 * armorBefore * 1.5) && calculateArmorForWeapon(currentStats()) === armorBefore, "Jackhammer adds 20% current Armor to displayed power without reducing Armor");
  assert(await Combat.startEncounter(["Goblin", "Goblin", "Goblin"]), "Equipment mechanics fixture starts"); $("combat-sheet-button").click();
  const fixture = Combat.serialize(); fixture.current = "player"; fixture.ap = 5;
  fixture.actors.filter(actor => actor.team === "enemy").forEach(actor => {
    actor.character.classLevels = {}; actor.character.raceId = "human"; actor.character.stats = { ...defaultStats };
    actor.character.equipment = { ...defaultEquipment }; actor.character.inventory = []; actor.character.activeSkills = {};
    actor.effects = []; actor.resistances = {}; actor.baseArmor = 0; actor.resourceMax = { hp: 100000, mana: 1000 }; actor.character.currentHp = 100000; actor.character.currentMana = 1000;
  });
  const reset = () => { applyCharacter(normalizeCharacter(fighter)); Combat.restore(clone(fixture)); return { player: h.actorById("player"), enemy: h.actorById("enemy-1"), other: h.actorById("enemy-2"), third: h.actorById("enemy-3") }; };
  let actors = reset(); const caps = clone(actors.player.resourceMax), normalArmor = h.values(actors.player).armor;
  assert(close(h.incomingDamageFactor(actors.player, actors.enemy, "spell", "fire"), .5), "Soulbound Barrier reduces spell damage by 50% at full health");
  h.setHp(actors.player, caps.hp * .5);
  assert(close(h.incomingDamageFactor(actors.player, actors.enemy, "spell", "fire"), .75), "At half health the spell protection is exactly 25%");
  h.setHp(actors.player, caps.hp * .25);
  assert(close(h.incomingDamageFactor(actors.player, actors.enemy, "spell", "fire"), .875), "At quarter health the spell protection is 12.5%");
  assert(h.values(actors.player).armor === normalArmor && h.values(actors.player).maxHp === caps.hp, "Losing HP does not shrink the maximum-HP Armor bonus or battle resource cap");
  assert(h.incomingDamageFactor(actors.player, actors.enemy, "melee") === 1, "The spell ward does not reduce melee damage");
  actors = reset(); const previousPower = h.weaponAttackValues(actors.player, hammer).power;
  actors.player.effects.push({ type: "defense-percent", target: "armor", amount: 50, expiresTurn: 99 });
  assert(h.weaponAttackValues(actors.player, hammer).power > previousPower && Math.floor(calculateItemPower(hammer)) === h.weaponAttackValues(actors.player, hammer).power, "Jackhammer updates both sheet and combat power when Armor changes");
  const expected = h.estimatedWeaponStrike(actors.player, actors.enemy, hammer).normalDamage;
  actors.third.baseArmor = 1000;
  const expectedSplash = h.estimatedDamage(actors.third, expected * .1, "physical", false, false, actors.player, "bonus");
  let rolls = [0, .999];
  try { Math.random = () => rolls.shift() ?? .999; assert(h.attack(actors.player, actors.enemy), "The two-handed hammer can attack"); } finally { Math.random = random; }
  assert(100000 - h.hp(actors.enemy) === expected && 100000 - h.hp(actors.other) === Math.floor(expected * .1), "A damaging hit splashes 10% to each other undefended enemy");
  assert(100000 - h.hp(actors.third) === expectedSplash && expectedSplash < Math.floor(expected * .1), "Splash damage respects each additional target's Armor");

  actors = reset(); equipment.artifact1 = titan.id;
  h.applyCombatDamage(actors.player, 0); h.setHp(actors.player, caps.hp * .5); h.triggerLowHealthEquipment(actors.player);
  assert(!Combat.serialize().actors.some(actor => actor.summoned), "Titanfall does not trigger on zero damage or at exactly 50% HP");
  h.applyCombatDamage(actors.player, 1);
  let golem = Combat.serialize().actors.find(actor => actor.summonDefinitionId === "steelguard-golem-level-35");
  assert(golem?.summoned && golem.summonerId === "player" && golem.team === "player" && golem.character.level === 35 && golem.character.raceId === "elemental", "Crossing below half HP summons a friendly Level 35 Steelguard Golem");
  assert(h.values(h.actorById(golem.id)).armor > 100 && golem.character.classLevels.guardian === 7, "The Golem has a high-Armor Guardian protection build");
  state = Combat.serialize(); Combat.restore(state); actors.player = h.actorById("player"); golem = h.actorById(golem.id);
  h.setHp(actors.player, caps.hp); h.applyCombatDamage(actors.player, caps.hp * .6); equipment.artifact1 = null; h.refresh(); equipment.artifact1 = titan.id; h.triggerLowHealthEquipment(actors.player);
  assert(Combat.serialize().actors.filter(actor => actor.summonDefinitionId === "steelguard-golem-level-35").length === 1, "Reloading, healing and re-equipping cannot repeatedly trigger Titanfall in one battle");
  state = Combat.serialize(); state.current = golem.id; state.ap = 5; Combat.restore(state);
  golem = h.actorById(golem.id); const bond = h.skillsOf(golem).find(skill => skill.id === "aegis-bond");
  assert(bond && h.useSkill(golem, classSkillKey(bond), h.actorById("player")), "The summoned Golem can use its ally-protecting skill");
  assert(h.redirectedGuardTarget(h.actorById("enemy-1"), h.actorById("player"), "melee") === golem, "The Golem redirects attacks from a protected ally");
  assert(JSON.stringify(h.actorById("player").resourceMax) === JSON.stringify(caps), "The artifact summon never changes the wearer's locked HP/Mana maximums");
  Combat.restore(null);
  const wounded = clone(fighter); wounded.equipment.artifact1 = titan.id; wounded.currentHp = Math.floor(maxHp * .4);
  applyCharacter(normalizeCharacter(wounded)); assert(await Combat.startEncounter(["Goblin"]), "A new battle with Titanfall and a wounded wearer starts"); $("combat-sheet-button").click();
  assert(Combat.serialize().actors.filter(actor => actor.summonDefinitionId === "steelguard-golem-level-35").length === 1, "Titanfall can trigger again in a new battle, including when starting below half HP");
  Combat.restore(null); applyCharacter(normalizeCharacter(main));
  const member = normalizeCharacter({ ...clone(fighter), equipment: { ...fighter.equipment, artifact1: titan.id } });
  companions = [{ id: "companion-titanfall-test", name: "Titanfall Companion", gender: "female", character: member }];
  assert(await Combat.startEncounter(["Goblin"]), "Companion artifact fixture starts"); $("combat-sheet-button").click();
  const ally = h.actorById("companion-titanfall-test"), allyCaps = clone(ally.resourceMax);
  h.applyCombatDamage(ally, allyCaps.hp * .6);
  assert(Combat.serialize().actors.some(actor => actor.summonDefinitionId === "steelguard-golem-level-35" && actor.summonerId === ally.id) && JSON.stringify(ally.resourceMax) === JSON.stringify(allyCaps), "Companions receive their own artifact summon while retaining their battle maximums");
  Combat.restore(null); companions = [];
  checks += await runGaianReworkChecks();
  return { passed: true, checks };
}

// Gaian rework (2026-10-11): regrowing heart-cores (a lone fallen Core regrows, both down sever
// him from the land), his look, intro and heart-of-the-jungle music, and the Jackhammer and the
// Soulbound Barrier as forged masterworks.
async function runGaianReworkChecks() {
  let checks = 0;
  const assert = (condition, message) => { if (!condition) throw Error(message); checks++; };
  const $ = id => document.getElementById(id);
  Combat.restore(null);
  const source = await (await fetch("scripts/combat.js")).text();
  const marker = "  window.Combat = {";
  const script = document.createElement("script");
  script.textContent = source.replace(marker, "  window.__gaianRework = { actorById, hp, values, setHp, enemyAction, endTurn, isControlledTurn, currentActor, getBattle: () => battle, stageLook, heartCoreState, incomingDamageFactor, actionPointMaximum };\n" + marker);
  document.head.append(script);
  await waitForSourceFiles();
  const hooks = window.__gaianRework;
  if (window.BattleStage) window.BattleStage.enabled = () => false;
  if (window.GameSettings) window.GameSettings.enemyDelay = () => 1e9;
  const F = ForgeRules;

  // His sheet: the heart-cores, his own forged gear and his drops.
  const raw = await (await fetch("enemies/" + encodeURIComponent("Gaian, Avatar of the Land") + ".json")).json();
  assert(raw.aiProfile === "gaian" && raw.introCinematic === "gaian" && !raw.phaseTransitions, "Gaian has his own AI and intro, and no phase");
  assert(raw.heartCores.enemyId === "jungle-core-level-50" && raw.heartCores.regrowTurns === 3 && raw.heartCores.severed.turns === 2 && raw.heartCores.severed.damageTakenPercent === 30 && raw.heartCores.severed.apLoss === 2 && raw.livingAllyProtection.amount === 25, "A fallen Core regrows after 3 of his turns; both down sever him for 2 turns (+30% damage taken, 2 fewer AP); each living Core shields 25%");
  assert(raw.drops.map(drop => drop.itemId + ":" + drop.chance).sort().join() === "artifact-titanfall-artifact:5,masterwork-jackhammer:12,masterwork-soulbound-barrier:8", "He drops the Jackhammer (12%), the Soulbound Barrier (8%) and Titanfall (5%)");
  const hammer = getItem(raw.equipment.primaryHand);
  assert(hammer?.forged?.masterwork === "jackhammer" && hammer.forged.type === "greathammer" && hammer.forged.parts[0].maker === "elven" && raw.noDropItems.includes(hammer.id), "He wields a forged Jackhammer, Elven at its heart, which never drops as ordinary gear");
  const barrier = getItem(F.encode(F.generateMasterwork({ key: "soulbound-barrier", level: 53, random: () => 0.5 })));
  assert(barrier.forged.type === "garb" && barrier.forged.parts[0].maker === "institute-of-magic" && barrier.effects.some(effect => effect.kind === "armor-from-max-hp-percent" && effect.amount === 25) && barrier.effects.some(effect => effect.kind === "missing-hp-spell-ward" && effect.amount === 50), "The Soulbound Barrier is Institute of Magic garb with its armor-from-HP and spell ward");
  const migrated = normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: 30, inventory: ["weapon-jackhammer-meele", "armor-soulbound-barrier-clothing"] }).inventory.map(id => getItem(id)?.forged?.masterwork);
  assert(migrated.join() === "jackhammer,soulbound-barrier", "Old copies become the forged masterworks");

  // A sturdy hero in the Old Growth.
  const blade = F.encode(F.generate({ type: "longsword", level: 56, source: "shop", random: () => 0.5 }));
  const mail = F.encode(F.generateArmor({ type: "full-plate", level: 56, source: "shop", random: () => 0.5 }));
  const c = defaultCharacter();
  Object.assign(c, { raceId: "human", level: 56, inventory: [blade, mail], equipment: { ...defaultEquipment, primaryHand: blade, armor: mail } });
  c.stats = { strength: 120, vitality: 1500, mind: 25, dexterity: 80, intelligence: 22, wisdom: 25, speed: 60, luck: 30 };
  applyCharacter(normalizeCharacter(c));
  currentHp = 1e6;
  worldState.regionId = "verdigris-jungle";
  assert(await Combat.startEncounter(["Gaian, Avatar of the Land", "Jungle Core", "Jungle Core"], "Gaian test", "great-hunt"), "His fight starts");
  const intro = $("boss-intro-cinematic");
  assert(!intro.hidden && intro.dataset.boss === "gaian" && intro.querySelector(".bi-boss svg") && intro.querySelectorAll(".bi-imp svg").length === 2, "The Cores pulse awake and he rises in his intro");
  $("boss-intro-skip").click();
  assert(Combat.musicTheme() === "gaian-avatar-of-the-land" && ["gaian-avatar-of-the-land", "gaian-severed"].every(id => MusicThemes.all().some(theme => theme.id === id)), "The heart of the jungle beats");
  const battle = hooks.getBattle(), gaian = battle.actors.find(actor => actor.enemyDefinitionId === "gaian-avatar-of-the-land-unique-level-53");
  const cores = battle.actors.filter(actor => actor.enemyDefinitionId === "jungle-core-level-50"), player = () => hooks.actorById("player");
  const healthy = Sprites.forActor(hooks.stageLook(gaian)).svg;
  assert(healthy.includes("#b8ff7a") || healthy.includes(SpriteKit.tone("#b8ff7a")), "The land's veins glow in him");
  const heal = () => { currentHp = hooks.values(player()).maxHp; };
  const step = () => { if (hooks.isControlledTurn()) { heal(); hooks.endTurn(); } else hooks.enemyAction(); };
  const until = (condition, limit = 300) => { for (let i = 0; i < limit && !condition() && Combat.active(); i++) step(); return condition(); };
  const atHisTurn = () => hooks.currentActor()?.id === gaian.id;

  // A lone fallen Core regrows after three of his turns.
  assert(until(() => battle.heartCores && atHisTurn()), "His first turn comes");
  hooks.setHp(cores[0], 0);
  const fellAt = gaian.turns;
  assert(until(() => !atHisTurn()) && until(atHisTurn) && battle.heartCores.fallen[cores[0].id] === fellAt, "The fallen Core is noted");
  const order = Combat.serialize() && hooks.heartCoreState();
  assert(order && Object.keys(battle.heartCores.fallen).length === 1, "One Core is regrowing");
  assert(until(() => hooks.hp(cores[0]) > 0 || gaian.turns > fellAt + 3) && gaian.turns === fellAt + 3 && hooks.hp(cores[0]) === hooks.values(cores[0]).maxHp && battle.log.some(entry => String(entry).includes("regrows from the roots")), "It regrows at full strength three of his turns after it fell");

  // Both down at once sever him: more damage taken, fewer AP, the dark veins and the faltering heart.
  const baseline = hooks.incomingDamageFactor(gaian, player(), "melee");
  for (const core of cores) hooks.setHp(core, 0);
  assert(until(() => battle.heartCores.severedUntil != null), "Bringing both Cores down severs him");
  assert(battle.heartCores.severings === 1 && gaian.effects.some(effect => effect.severed && effect.type === "incoming-weapon-damage-percent" && effect.amount === 30) && battle.log.some(entry => String(entry).includes("is Severed from the land")), "Severed, he takes 30% more damage");
  assert(hooks.incomingDamageFactor(gaian, player(), "melee") > baseline * 1.5, "With both Cores down and Severed, he is far easier to hurt");
  assert(Combat.musicTheme() === "gaian-severed" && hooks.stageLook(gaian).severed === true && Sprites.forActor(hooks.stageLook(gaian)).svg !== healthy, "His heartbeat falters and his veins go dark");
  assert(until(atHisTurn) && battle.ap === hooks.actionPointMaximum(gaian) - 2 && battle.log.some(entry => String(entry).includes("is cut off from the land")), "Severed, he has 2 fewer AP");
  const severedEnds = battle.heartCores.severedUntil;
  assert(until(() => battle.heartCores.severedUntil == null) && gaian.turns === severedEnds + 1 && cores.every(core => hooks.hp(core) > 0) && !gaian.effects.some(effect => effect.severed), "After two of his turns both Cores regrow and he is whole again");
  assert(Combat.musicTheme() === "gaian-avatar-of-the-land" && battle.log.some(entry => String(entry).includes("The roots knit together")), "The heartbeat steadies");
  // His fall ends it.
  hooks.setHp(gaian, 0);
  until(() => !Combat.active(), 20);
  Combat.restore(null);
  return checks;
}

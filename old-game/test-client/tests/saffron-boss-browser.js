async function runSaffronBossChecks() {
  let checks = 0;
  const assert = (ok, message) => { if (!ok) throw Error(message); checks++; };
  const $ = id => document.getElementById(id), clone = value => structuredClone(value);
  Combat.restore(null); WorldMap.setAmbientEnabled(false);
  const source = await (await fetch("scripts/combat.js")).text();
  const script = document.createElement("script");
  script.textContent = source.replace("  window.Combat = {", "  window.__saffronChecks = { actorById, values, hp, mana, hurt, directDamage, applyCombatDamage, attack, cast, strike, refresh, useSkill, skillsOf, redirectedGuardTarget, weaponAttackValues, attackLoadout, estimatedWeaponStrike, estimatedSpellDamage, equipmentChargeBonuses, equipmentChargeMultiplier, processEquipmentReactions, awardDefeatedEnemyExperience, validateEnemy };\n  window.Combat = {");
  document.head.append(script); await waitForSourceFiles();
  const h = window.__saffronChecks;
  const wand = getItem(ForgeRules.encode(ForgeRules.generateMasterwork({ key: "hellish-rebuke", level: 100, random: () => 0.5 }))), plate = getItem(ForgeRules.encode(ForgeRules.generateMasterwork({ key: "seventh-cycle", level: 100, random: () => 0.5 })));
  const forgedLoot = (loot, key) => loot.filter(id => id.startsWith("weapon-masterwork-" + key + "-") || id.startsWith("armor-masterwork-" + key + "-")).length;
  assert(wand?.rarity === "masterwork" && plate?.rarity === "masterwork", "Both equipment catalogues load the new Masterwork items");
  assert(artifactEffectLabel(wand.effects[1]).includes("50 Mana") && artifactEffectLabel(plate.effects[0]).includes("200%"), "Item detail effect labels describe retaliation and the seventh-charge burst");
  const main = defaultCharacter(); main.raceId = "human"; main.level = 100;
  main.stats.strength = 200; main.stats.vitality = 1000; main.stats.mind = 1000; main.stats.intelligence = 200; main.stats.speed = 2000; main.stats.luck = 0;
  main.currentHp = 100000; main.currentMana = 100000;
  applyCharacter(normalizeCharacter(main));
  const fire = getSpell("fire-bolt-fire-damage"), basic = getSpell("spell-bolt-basic-damage");
  const fireCost = calculateSpellManaCost(fire), basicCost = calculateSpellManaCost(basic);
  inventory = [wand.id]; equipment = { ...defaultEquipment, primaryHand: wand.id };
  const withRebuke = { fire: calculateSpellManaCost(fire), basic: calculateSpellManaCost(basic) }, rebukeEffects = wand.effects;
  wand.effects = rebukeEffects.filter(effect => !(effect.kind === "spell-cost-percent" && effect.element === "fire"));
  let plainCosts; try { plainCosts = { fire: calculateSpellManaCost(fire), basic: calculateSpellManaCost(basic) }; } finally { wand.effects = rebukeEffects; }
  assert(withRebuke.fire < plainCosts.fire && withRebuke.fire >= Math.floor(fireCost * .8) - Math.ceil(fireCost * .5) && withRebuke.basic === plainCosts.basic, "Hellish Rebuke reduces Fire Mana cost, and does not discount other elements beyond its forged handle");
  applyCharacter(normalizeCharacter(main));

  worldState.regionId = "deep-desert"; WorldRules.setActiveRegion(worldState.regionId);
  const town = WorldRules.settlementsForRegion(worldState.regionId).find(city => city.guildServices?.includes("quest"));
  assert(town, "The Deep Desert has a guild for the new hunt");
  worldState.position = { x: town.x, y: town.y }; Guild.render(); $("guild-quest").click();
  const questIndex = worldState.guild.offers.findIndex(offer => offer.uniqueId === "archdevil-saffron"), quest = worldState.guild.offers[questIndex];
  assert(quest && quest.offeredAtLevel === 66 && quest.regionId === "deep-desert" && quest.zone === "Pyramid Sanctum" && WorldRules.zoneAt(quest.point, quest.regionId) === "Pyramid Sanctum", "The local guild offers a level-66 Great Hunt at a valid pyramid marker");
  document.querySelector(`[data-guild-quest="${questIndex}"]`).click(); worldState.position = clone(quest.point);
  const objective = Guild.objectiveAt(worldState.position);
  assert(objective?.enemies.length === 4, "The map encounter preview includes Saffron and all three servants");
  assert(await Combat.startEncounter(objective.enemies, "Pyramid test", objective.kind), "The pyramid encounter starts");
  $("combat-sheet-button").click();
  let state = Combat.serialize();
  const saffron = state.actors.find(actor => actor.enemyDefinitionId === "archdevil-saffron-unique-level-66");
  const servants = state.actors.filter(actor => actor.enemyDefinitionId?.startsWith("great-devil-servant-"));
  assert(saffron && servants.length === 3 && state.actors.filter(actor => actor.team === "enemy").length === 4 && servants.every(actor => !actor.summoned && actor.character.level === 56 && actor.character.raceId === "devil"), "The entourage appears once and counts as ordinary level-56 Devils, not summons");
  assert(saffron.character.level === 66 && saffron.character.classLevels["raid-boss"] === 1 && getItem(saffron.character.equipment.primaryHand)?.forged?.masterwork === "hellish-rebuke" && getItem(saffron.character.equipment.armor)?.forged?.masterwork === "seventh-cycle", "Saffron is a level-66 Devil Raid Boss wielding both new items");
  const protector = servants.find(actor => actor.name.endsWith("Protector")), reaver = servants.find(actor => actor.name.endsWith("Reaver")), archer = servants.find(actor => actor.name.endsWith("Archer"));
  assert(h.values(h.actorById(protector.id)).armor > h.values(h.actorById(reaver.id)).armor && h.attackLoadout(h.actorById(archer.id)).primary.type === "ranged", "Protector has the highest armor and the archer uses a ranged build");
  assert(h.weaponAttackValues(h.actorById(reaver.id), h.attackLoadout(h.actorById(reaver.id)).primary).power > h.weaponAttackValues(h.actorById(protector.id), h.attackLoadout(h.actorById(protector.id)).primary).power, "The reaver's melee power exceeds the protector's");
  state.current = protector.id; state.ap = 5; Combat.restore(state);
  const guardian = h.actorById(protector.id), boss = h.actorById(saffron.id);
  const bond = h.skillsOf(guardian).find(skill => skill.id === "aegis-bond");
  assert(h.useSkill(guardian, classSkillKey(bond), boss), "The protector can actively shield Saffron with Aegis Bond");
  assert(h.redirectedGuardTarget(h.actorById("player"), boss, "melee") === guardian, "Attacks on the protected boss redirect to the armored servant");
  state = Combat.serialize(); state.current = saffron.id; state.ap = 8; Combat.restore(state);
  const start = performance.now(), choice = Combat.previewAiDecision(saffron.id);
  assert(choice && performance.now() - start < 1000, "Saffron's caster AI selects an action without stalling");
  state = Combat.serialize(); state.actors.filter(actor => actor.team === "enemy").forEach(actor => { actor.character.currentHp = 0; }); Combat.restore(state);
  const random = Math.random;
  try { Math.random = () => .999; h.awardDefeatedEnemyExperience(); } finally { Math.random = random; }
  assert(!forgedLoot(Combat.serialize().lootItems, "hellish-rebuke") && !forgedLoot(Combat.serialize().lootItems, "seventh-cycle"), "Neither Masterwork item is a guaranteed equipment drop");
  Combat.restore(null); applyCharacter(normalizeCharacter(main));
  assert(await Combat.startEncounter(["Archdevil Saffron"]), "Direct boss encounters also start"); $("combat-sheet-button").click();
  state = Combat.serialize(); assert(state.actors.filter(actor => actor.team === "enemy").length === 4, "A direct Saffron encounter automatically brings his three servants");
  state.actors.filter(actor => actor.team === "enemy").forEach(actor => { actor.character.currentHp = 0; }); Combat.restore(state);
  try { Math.random = () => 0; h.awardDefeatedEnemyExperience(); } finally { Math.random = random; }
  assert(forgedLoot(Combat.serialize().lootItems, "hellish-rebuke") === 1 && forgedLoot(Combat.serialize().lootItems, "seventh-cycle") === 1, "Successful rolls produce one of each Masterwork drop");
  h.refresh(); assert(!$("combat-loot-modal").hidden && $("combat-loot-modal").textContent.includes("Hellish Rebuke") && $("combat-loot-modal").textContent.includes("Seventh Cycle"), "Both drops appear in the normal keep-or-discard loot window");
  Combat.restore(null);

  const fighter = clone(main), sword = getItem("weapon-forged-longsword-30-hmnul-dwrei-hfman-s0");
  fighter.inventory = [sword.id, wand.id, plate.id]; fighter.equipment = { ...defaultEquipment, primaryHand: sword.id, armor: plate.id };
  fighter.knownSpells = [fire.id, "minor-firewall-fire-damage", "reinforce-basic-support"];
  applyCharacter(normalizeCharacter(fighter));
  assert(await Combat.startEncounter(["Goblin", "Goblin"]), "Equipment mechanics fixture starts"); $("combat-sheet-button").click();
  const fixture = Combat.serialize(); fixture.current = "player"; fixture.ap = 5;
  fixture.actors.filter(actor => actor.team === "enemy").forEach(actor => {
    actor.character.classLevels = {}; actor.character.raceId = "human"; actor.character.stats = { ...defaultStats };
    actor.character.equipment = { ...defaultEquipment }; actor.character.inventory = []; actor.character.activeSkills = {};
    actor.effects = []; actor.resistances = {}; actor.baseArmor = 0; actor.resourceMax = { hp: 100000, mana: 1000 }; actor.character.currentHp = 100000; actor.character.currentMana = 1000;
  });
  const reset = () => { applyCharacter(normalizeCharacter(fighter)); Combat.restore(clone(fixture)); return { player: h.actorById("player"), enemy: h.actorById("enemy-1"), other: h.actorById("enemy-2") }; };
  let actors = reset(); const caps = clone(actors.player.resourceMax);
  h.applyCombatDamage(actors.player, 0); actors.player.barrierHp = 20; h.applyCombatDamage(actors.player, 10); actors.player.barrierHp = 0;
  assert(h.equipmentChargeMultiplier(actors.player) === 1, "Zero damage and fully absorbed damage grant no Seventh Cycle charges");
  for (let i = 0; i < 3; i++) h.applyCombatDamage(actors.player, 1);
  assert(h.equipmentChargeMultiplier(actors.player) === 1.3, "Three damaging hits grant exactly +30% next-action damage");
  for (let i = 0; i < 8; i++) h.applyCombatDamage(actors.player, 1);
  assert(h.equipmentChargeBonuses(actors.player)[0].stacks === 7 && h.equipmentChargeMultiplier(actors.player) === 3, "Charges cap at seven, where +200% replaces the ordinary per-charge bonus");
  const chargedSave = Combat.serialize(); Combat.restore(chargedSave); actors.player = h.actorById("player"); actors.enemy = h.actorById("enemy-1");
  assert(h.equipmentChargeMultiplier(actors.player) === 3 && JSON.stringify(actors.player.resourceMax) === JSON.stringify(caps), "Combat reload preserves charges without changing HP or Mana caps");
  const forecast = h.estimatedWeaponStrike(actors.player, actors.enemy, sword).normalDamage, before = h.hp(actors.enemy);
  let rolls = [0, .999];
  try { Math.random = () => rolls.shift() ?? .999; assert(h.attack(actors.player, actors.enemy), "Charged attack is usable"); } finally { Math.random = random; }
  assert(before - h.hp(actors.enemy) === forecast && h.equipmentChargeMultiplier(actors.player) === 1, "Actual charged attack damage matches AI forecasting and spends charges once");
  actors = reset(); h.applyCombatDamage(actors.player, 1);
  assert(h.cast(actors.player, actors.player, getSpell("reinforce-basic-support")) && h.equipmentChargeMultiplier(actors.player) === 1.1, "Support spells do not consume damage charges or gain stronger support effects");
  actors = reset(); for (let i = 0; i < 7; i++) h.applyCombatDamage(actors.player, 1);
  const area = getSpell("minor-firewall-fire-damage"), expected = h.estimatedSpellDamage(actors.player, actors.enemy, area), firstHp = h.hp(actors.enemy), secondHp = h.hp(actors.other);
  assert(h.cast(actors.player, actors.enemy, area), "Charged area spell is usable");
  assert(firstHp - h.hp(actors.enemy) === expected && secondHp - h.hp(actors.other) === expected && h.equipmentChargeMultiplier(actors.player) === 1, "One charge expenditure boosts all targets of the next damaging spell");
  actors = reset(); h.applyCombatDamage(actors.player, 1); equipment.armor = null; h.refresh(); equipment.armor = plate.id;
  assert(h.equipmentChargeMultiplier(actors.player) === 1, "Removing Seventh Cycle clears its charges rather than banking them for re-equipment");
  actors = reset(); h.applyCombatDamage(actors.player, 1);
  const noAp = Combat.serialize(); noAp.ap = 0; Combat.restore(noAp);
  assert(!h.attack(h.actorById("player"), h.actorById("enemy-1")) && h.equipmentChargeMultiplier(h.actorById("player")) === 1.1, "An unavailable attack does not spend damage charges");
  actors = reset(); h.applyCombatDamage(actors.player, 1); currentMana = 0;
  assert(!h.cast(actors.player, actors.enemy, fire) && h.equipmentChargeMultiplier(actors.player) === 1.1, "A spell rejected for insufficient Mana does not spend damage charges");

  const reactionReset = () => { const result = reset(); equipment.primaryHand = wand.id; inventory = [wand.id, plate.id]; return result; };
  actors = reactionReset();
  h.directDamage(actors.player, 100, false, "physical", actors.enemy); h.refresh();
  assert(Combat.serialize().reactions.length === 1 && $("combat-reaction-modal").hidden, "A paid retaliation is queued without interrupting the paused character sheet");
  const pending = Combat.serialize(); pending.current = actors.enemy.id; pending.ap = 5; Combat.restore(pending); $("combat-view-toggle").click();
  assert(!$("combat-reaction-modal").hidden && $("combat-reaction-description").textContent.includes("20"), "Returning to combat shows the restored optional 20%-damage reaction");
  const paused = Combat.serialize(); await new Promise(resolve => setTimeout(resolve, 1000));
  assert(Combat.serialize().current === paused.current && Combat.serialize().ap === paused.ap && Combat.serialize().log.length === paused.log.length, "Enemy AI does not act while the player is choosing retaliation");
  const declinedMana = currentMana; $("combat-reaction-decline").click(); $("combat-sheet-button").click();
  assert(currentMana === declinedMana && !Combat.serialize().reactions.length && $("combat-reaction-modal").hidden, "Declining costs no Mana and releases the reaction pause");
  actors = reactionReset(); actors.enemy.character.inventory = [wand.id]; actors.enemy.character.equipment.primaryHand = wand.id;
  h.directDamage(actors.player, 100, false, "physical", actors.enemy); h.refresh(); $("combat-view-toggle").click();
  const manaBefore = currentMana, hpBefore = h.hp(actors.enemy), apBefore = Combat.serialize().ap;
  $("combat-reaction-use").click(); $("combat-sheet-button").click();
  assert(currentMana === manaBefore - 50 && hpBefore - h.hp(actors.enemy) === 20 && Combat.serialize().ap === apBefore, "Retaliating spends exactly 50 Mana, no AP, and returns 20% damage as Fire");
  assert(!Combat.serialize().reactions.length && h.equipmentChargeMultiplier(actors.player) === 1.1, "Retaliation cannot bounce between wands or consume/benefit from Seventh Cycle charges");
  actors = reactionReset(); currentMana = 49; h.directDamage(actors.player, 100, false, "physical", actors.enemy); h.refresh();
  assert(!Combat.serialize().reactions.length, "Insufficient Mana does not offer an unusable retaliation");
  actors = reactionReset(); h.directDamage(actors.player, 100, false, "physical", actors.enemy); equipment.primaryHand = sword.id; h.refresh();
  assert(!Combat.serialize().reactions.length && $("combat-reaction-modal").hidden, "Unequipping the wand safely cancels a pending reaction");
  actors = reactionReset(); h.directDamage(actors.player, 100, false, "physical", actors.enemy); h.refresh(); $("combat-view-toggle").click(); $("combat-reaction-close").click(); $("combat-sheet-button").click();
  assert(!Combat.serialize().reactions.length && $("combat-reaction-modal").hidden, "Closing the reaction window counts as declining, never a permanent battle lock");
  actors = reset(); equipment.armor = null;
  actors.enemy.character.inventory = [wand.id]; actors.enemy.character.equipment.primaryHand = wand.id;
  h.directDamage(actors.enemy, 100, false, "physical", actors.player);
  const npcMana = h.mana(actors.enemy), playerHp = currentHp;
  h.processEquipmentReactions();
  assert(h.mana(actors.enemy) === npcMana - 50 && currentHp < playerHp && !Combat.serialize().reactions.length, "Enemy AI uses a worthwhile affordable Hellish Rebuke without requiring player input");
  actors = reset(); actors.enemy.character.inventory = [wand.id]; actors.enemy.character.equipment.primaryHand = wand.id; actors.enemy.character.currentMana = 55;
  h.directDamage(actors.enemy, 100, false, "physical", actors.player); h.processEquipmentReactions();
  assert(h.mana(actors.enemy) === 55 && !Combat.serialize().reactions.length, "Enemy AI preserves scarce Mana instead of spending its last reserves on weak retaliation");
  Combat.restore(null); applyCharacter(normalizeCharacter(fighter));
  const member = normalizeCharacter({ ...clone(fighter), equipment: { ...defaultEquipment, primaryHand: wand.id, armor: plate.id } });
  companions = [{ id: "companion-rebuke-test", name: "Rebuke Companion", gender: "female", character: member }];
  assert(await Combat.startEncounter(["Goblin"]), "Companion reaction fixture starts"); $("combat-sheet-button").click();
  const ally = h.actorById("companion-rebuke-test"), foe = h.actorById("enemy-1");
  h.directDamage(ally, 100, false, "physical", foe); h.refresh(); $("combat-view-toggle").click();
  assert(!$("combat-reaction-modal").hidden && $("combat-reaction-title").textContent.includes("Rebuke Companion"), "Companions receive the same optional retaliation prompt as the main character");
  const allyMana = h.mana(ally), mainMana = currentMana; $("combat-reaction-use").click(); $("combat-sheet-button").click();
  assert(h.mana(ally) === allyMana - 50 && currentMana === mainMana && !Combat.serialize().reactions.length, "A companion's reaction spends their own Mana, not the player's");
  Combat.restore(null);
  checks += await runSaffronReworkChecks();
  return { passed: true, checks };
}

// Archdevil Saffron rework (2026-10-12): the infernal sacrifice (a Servant below 35% is drained to
// heal him and add Infernal Power; a rift returns it three of his turns later; Servants killed
// outright stay dead), his court's AI, look, intro and royal music, and Hellish Rebuke (a caster
// masterwork) and Seventh Cycle as forged masterworks.
async function runSaffronReworkChecks() {
  let checks = 0;
  const assert = (condition, message) => { if (!condition) throw Error(message); checks++; };
  const $ = id => document.getElementById(id);
  Combat.restore(null);
  const source = await (await fetch("scripts/combat.js")).text();
  const marker = "  window.Combat = {";
  const script = document.createElement("script");
  script.textContent = source.replace(marker, "  window.__saffronRework = { actorById, hp, values, setHp, enemyAction, endTurn, isControlledTurn, currentActor, getBattle: () => battle, stageLook, sacrificeState, awardDefeatedEnemyExperience };\n" + marker);
  document.head.append(script);
  await waitForSourceFiles();
  const hooks = window.__saffronRework;
  if (window.BattleStage) window.BattleStage.enabled = () => false;
  if (window.GameSettings) window.GameSettings.enemyDelay = () => 1e9;
  const F = ForgeRules;

  // His sheet, his court and his gear.
  const raw = await (await fetch("enemies/Archdevil%20Saffron.json")).json();
  assert(raw.aiProfile === "saffron" && raw.introCinematic === "saffron" && !raw.phaseTransitions, "Saffron has his own AI and intro, and no phase");
  assert(raw.infernalSacrifice.belowPercent === 35 && raw.infernalSacrifice.healPercent === 10 && raw.infernalSacrifice.powerPercent === 15 && raw.infernalSacrifice.returnTurns === 3 && raw.infernalSacrifice.servants.length === 3, "He drains a Servant below 35% (heal 10%, +15% spell power), and a rift returns it after 3 of his turns");
  for (const name of ["Protector", "Reaver", "Archer"]) {
    const servant = await (await fetch("enemies/" + encodeURIComponent("Great Devil Servant - " + name) + ".json")).json();
    assert(servant.aiProfile === "devil-servant" && servant.maxHpPercent === 300, name + " fights as a sturdy court Servant");
  }
  assert(raw.drops.map(drop => drop.itemId + ":" + drop.chance).sort().join() === "masterwork-hellish-rebuke:12,masterwork-seventh-cycle:6", "He drops Hellish Rebuke (12%) and Seventh Cycle (6%)");
  const wand = getItem(raw.equipment.primaryHand), mail = getItem(raw.equipment.armor);
  assert(wand?.forged?.masterwork === "hellish-rebuke" && wand.forged.type === "wand" && wand.forged.parts[0].maker === "hellmade" && wand.forged.parts[0].material === "ruby" && wand.forged.parts[0].tier === 3 && wand.rarity === "masterwork", "He wields a forged Hellish Rebuke: a spell wand with a Greater Hellmade Ruby");
  assert(mail?.forged?.masterwork === "seventh-cycle" && mail.forged.type === "chainmail" && mail.forged.parts[0].maker === "hellmade" && raw.noDropItems.includes(wand.id) && raw.noDropItems.includes(mail.id), "He wears a forged Seventh Cycle chainmail; neither copy drops as ordinary gear");
  assert(F.encode(F.decode(wand.id)) === wand.id && getItem("masterwork-hellish-rebuke")?.rarity === "masterwork", "Caster masterworks round-trip and have a template");
  const migrated = normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: 40, inventory: ["weapon-hellish-rebuke-magic-wand", "armor-seventh-cycle-heavy"] }).inventory.map(id => getItem(id)?.forged?.masterwork);
  assert(migrated.join() === "hellish-rebuke,seventh-cycle", "Old copies become the forged masterworks");

  // A sturdy hero in the Pyramid Sanctum.
  const blade = F.encode(F.generate({ type: "longsword", level: 69, source: "shop", random: () => 0.5 }));
  const plate = F.encode(F.generateArmor({ type: "full-plate", level: 69, source: "shop", random: () => 0.5 }));
  const c = defaultCharacter();
  Object.assign(c, { raceId: "human", level: 69, inventory: [blade, plate], equipment: { ...defaultEquipment, primaryHand: blade, armor: plate } });
  c.stats = { strength: 60, vitality: 3000, mind: 25, dexterity: 80, intelligence: 22, wisdom: 25, speed: 40, luck: 30 };
  applyCharacter(normalizeCharacter(c));
  currentHp = 1e7;
  worldState.regionId = "deep-desert";
  assert(await Combat.startEncounter(["Archdevil Saffron", "Great Devil Servant - Protector", "Great Devil Servant - Reaver", "Great Devil Servant - Archer"], "Saffron test", "great-hunt"), "His fight starts");
  const intro = $("boss-intro-cinematic");
  assert(!intro.hidden && intro.dataset.boss === "saffron" && intro.querySelector(".bi-boss svg") && intro.querySelectorAll(".bi-imp svg").length === 3 && intro.querySelectorAll(".bi-sf-brazier").length === 4, "The braziers ignite, his three Servants step out and he descends in his intro");
  $("boss-intro-skip").click();
  assert(Combat.musicTheme() === "archdevil-saffron" && MusicThemes.all().some(theme => theme.id === "archdevil-saffron"), "His royal court theme plays");
  const battle = hooks.getBattle(), saffron = battle.actors.find(actor => actor.enemyDefinitionId === "archdevil-saffron-unique-level-66");
  const reaver = battle.actors.find(actor => actor.enemyDefinitionId === "great-devil-servant-reaver-level-56"), archer = battle.actors.find(actor => actor.enemyDefinitionId === "great-devil-servant-archer-level-56");
  const calm = Sprites.forActor(hooks.stageLook(saffron)).svg;
  assert(calm.includes("#ffb347") && calm.includes("#e0b54a") || calm.includes(SpriteKit.tone("#e0b54a")), "He wears his black-and-gold robe with the seven sigils and the burning crown");
  const heal = () => { currentHp = hooks.values(hooks.actorById("player")).maxHp; };
  const step = () => { if (hooks.isControlledTurn()) { heal(); hooks.endTurn(); } else hooks.enemyAction(); };
  const until = (condition, limit = 300) => { for (let i = 0; i < limit && !condition() && Combat.active(); i++) step(); return condition(); };
  const atHisTurn = () => hooks.currentActor()?.id === saffron.id;

  // A Servant below 35% is drained at the start of his turn.
  assert(until(() => !atHisTurn()), "The battle is under way");
  saffron.character.currentHp = Math.floor(hooks.values(saffron).maxHp * 0.6);
  const before = hooks.hp(saffron), exp = battle.experienceEarned || 0;
  reaver.character.currentHp = Math.floor(hooks.values(reaver).maxHp * 0.3);
  assert(until(() => battle.sacrifice?.sacrifices >= 1), "He feeds on the wounded Servant");
  const drainedAt = saffron.turns;
  assert(hooks.hp(reaver) === 0 && reaver.sacrificed && battle.sacrifice.consumed[reaver.id] === drainedAt && battle.log.some(entry => String(entry).includes("drains") && String(entry).includes("into the hellfire")), "The Reaver is drained into the hellfire");
  assert(hooks.hp(saffron) >= before + Math.floor(hooks.values(saffron).maxHp * 0.1) - 1 && saffron.effects.some(effect => effect.infernal && effect.type === "spell-power-percent" && effect.amount === 15), "He heals 10% and gains Infernal Power (+15% spell power)");
  hooks.awardDefeatedEnemyExperience();
  assert((battle.experienceEarned || 0) === exp, "A drained Servant gives the heroes nothing");
  const order = Combat.serialize() && hooks.sacrificeState();
  assert(order && hooks.stageLook(saffron).infernal === 1 && Sprites.forActor(hooks.stageLook(saffron)).svg !== calm, "His hellfire burns brighter");
  // A Servant killed outright stays dead.
  hooks.setHp(archer, 0);
  // The rift returns the drained Reaver three of his turns later.
  assert(until(() => hooks.hp(reaver) > 0 || saffron.turns > drainedAt + 3) && saffron.turns === drainedAt + 3 && hooks.hp(reaver) === hooks.values(reaver).maxHp && !reaver.sacrificed && !reaver.experienceAwarded && battle.log.some(entry => String(entry).includes("A hell-rift tears open")), "A hell-rift returns the Reaver at full strength three of his turns later");
  assert(until(() => saffron.turns >= drainedAt + 5) && hooks.hp(archer) === 0, "The Archer, killed outright, stays dead");
  hooks.setHp(saffron, 0);
  until(() => !Combat.active(), 30);
  Combat.restore(null);
  return checks;
}

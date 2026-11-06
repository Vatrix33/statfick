async function runAnatemisChecks() {
  const assert = (condition, message) => { if (!condition) throw new Error(message); };
  const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
  const bossId = "anatemis-white-star-unique-level-100";
  const weaponId = "weapon-stardragon-claws-meele";
  const character = defaultCharacter();
  character.raceId = "human";
  character.level = 100;
  applyCharacter(normalizeCharacter(character));
  worldState.regionId = "high-stoneveil";
  WorldRules.setActiveRegion(worldState.regionId);
  worldState.position = { x: 740, y: 510 };
  Guild.render();
  worldState.guild.boards = {};
  const offer = Guild.boardFor().offers.find(entry => entry.uniqueId === "anatemis-white-star");
  assert(offer?.regionId === "stoneveil-summit" && offer.originRegionId === "high-stoneveil" && offer.originCastleId === "cairnrest", "Anatemis is offered at Cairnrest for the Summit");
  assert(offer.zone === "Frozen Crown" && WorldRules.zoneAt(offer.point, offer.regionId) === "Frozen Crown", "the Hunt marker is inside the Frozen Crown");
  Guild.accept(Guild.boardFor().offers.indexOf(offer));
  assert(worldState.guild.greatHunt?.uniqueId === "anatemis-white-star", "the Great Hunt can be accepted in High Stoneveil");
  worldState.regionId = "stoneveil-summit";
  WorldRules.setActiveRegion(worldState.regionId);
  worldState.position = { ...offer.point };
  assert(Guild.objectiveAt(offer.point)?.enemy === "Anatemis, the White Star", "the Summit marker launches the correct boss");
  assert(Guild.markers().some(marker => marker.label.includes("Anatemis")), "the Summit map shows the hunt marker");

  const claws = ForgeRules.naturalWeapon("stardragon-claws");
  assert(claws?.rarity === "masterwork" && claws.handed === 2 && claws.level === 100 && claws.droppable === false && !getItem(weaponId), "Stardragon Claws are a natural Level 100 masterwork attack, never an item");
  assert(await Combat.startEncounter(["Anatemis, the White Star"], "Anatemis test", "great-hunt"), "Anatemis encounter starts");
  document.getElementById("boss-intro-skip").click();
  let state = Combat.serialize();
  let boss = state.actors.find(actor => actor.enemyDefinitionId === bossId);
  assert(boss?.character.level === 100 && boss.character.raceId === "dragon" && boss.character.classLevels["raid-boss"] >= 1, "Anatemis is a level 100 Dragon Raid Boss");
  assert(boss.naturalWeapon === "stardragon-claws" && !boss.character.equipment.primaryHand, "Anatemis fights with her natural Stardragon Claws");
  const knownElements = new Set(boss.character.knownSpells.map(getSpell).filter(Boolean).map(spell => spell.element));
  assert(SPELL_ELEMENTS.every(element => knownElements.has(element)), "Anatemis knows spells from every element");
  assert(boss.phaseTransitions.length === 2 && boss.phaseTransitions[0].thresholdHpPercent === 50 && boss.phaseTransitions[1].thresholdHpPercent === 15, "both ordered phase thresholds load");
  assert(Combat.musicTheme() === "anatemis-white-star", "the unique boss theme plays");
  assert(!state.lootItems.includes(weaponId), "his claws do not enter loot at battle start");
  if (state.current !== "player") {
    const opening = Combat.serialize(), playerHp = currentHp;
    const started = performance.now();
    assert(Combat.previewAiDecision(state.current)?.kind, "Anatemis can select an opening action");
    assert(performance.now() - started < 5000, `Anatemis's opening AI decision does not freeze combat (${Math.round(performance.now() - started)} ms)`);
    for (let attempt = 0; attempt < 12 && Combat.active() && Combat.serialize().log.length === opening.log.length; attempt++) await wait(500);
    assert(!Combat.active() || Combat.serialize().log.length > opening.log.length, "Anatemis executes an opening action");
    currentHp = playerHp;
    Combat.restore(opening);
    document.getElementById("combat-view-toggle").click();
    state = Combat.serialize();
    boss = state.actors.find(actor => actor.enemyDefinitionId === bossId);
  }
  const maximum = boss.resourceMax.hp;
  const goldThreshold = Math.floor(maximum * .5);
  boss.character.currentHp = goldThreshold;
  state.current = null;
  state.actors.forEach(actor => { actor.counter = actor === boss ? 1000 : 0; });
  Combat.restore(state);
  Combat.onSheetChange();
  await wait(70);
  state = Combat.serialize();
  boss = state.actors.find(actor => actor.enemyDefinitionId === bossId);
  const cinematic = document.getElementById("boss-intro-cinematic");
  assert(boss.phaseIndex === 1 && boss.name === "Anatemis, the Golden Star", "Anatemis reaches Golden Star at half HP");
  const dragonkinRegeneration = Math.floor(Math.floor(maximum * .03) / 5);
  assert(boss.character.currentHp === Math.min(maximum, goldThreshold + Math.floor(maximum * .1) + dragonkinRegeneration) && boss.resourceMax.hp === maximum, `phase two heals the full 10% plus reduced Dragonkin turn-start regeneration of 0.6% without changing maximum HP (actual ${boss.character.currentHp}, max ${maximum})`);
  assert(boss.character.permanentBonuses.intelligence === -55 && boss.character.permanentBonuses.wisdom === -55 && boss.character.permanentDefenseBonuses.armor === 30, "phase two grants magic and armor bonuses");
  assert(!cinematic.hidden && cinematic.dataset.boss === "anatemis-golden" && cinematic.querySelector("#boss-intro-title")?.textContent.includes("Golden Star"), "the Golden Star cinematic appears");
  assert(!document.getElementById("combat-view").inert, "the cinematic does not lock the combat view");
  document.getElementById("boss-intro-skip").click();
  assert(cinematic.hidden && boss.phaseIndex === 1, "the Golden Star cinematic is skippable");

  state = Combat.serialize();
  boss = state.actors.find(actor => actor.enemyDefinitionId === bossId);
  const zenithThreshold = Math.floor(maximum * .15);
  boss.character.currentHp = zenithThreshold;
  state.current = null;
  state.actors.forEach(actor => { actor.counter = actor === boss ? 1000 : 0; });
  Combat.restore(state);
  Combat.onSheetChange();
  await wait(70);
  state = Combat.serialize();
  boss = state.actors.find(actor => actor.enemyDefinitionId === bossId);
  assert(boss.phaseIndex === 2 && boss.name === "Anatemis, the Zenith of Dragonkind", "Anatemis reaches Zenith at 15% HP");
  assert(boss.character.currentHp === Math.min(maximum, zenithThreshold + Math.floor(maximum * .25) + dragonkinRegeneration) && boss.resourceMax.hp === maximum, `phase three heals the full 25% plus reduced Dragonkin turn-start regeneration of 0.6% without changing maximum HP (actual ${boss.character.currentHp}, max ${maximum})`);
  const sheetBonuses = { strength: -70, vitality: 0, mind: 0, dexterity: -70, intelligence: -85, wisdom: -85, speed: -85, luck: 0 };
  assert(Object.keys(boss.character.stats).every(key => boss.character.permanentBonuses[key] === sheetBonuses[key] + 25 + (key === "intelligence" || key === "wisdom" ? 30 : 0)), "phase three grants 25% to all basic stats");
  assert(!cinematic.hidden && cinematic.dataset.boss === "anatemis-zenith" && cinematic.querySelector("#boss-intro-title")?.textContent.includes("Zenith"), "the separate Zenith cinematic appears");
  document.getElementById("boss-intro-skip").click();
  assert(cinematic.hidden, "the Zenith cinematic is skippable");
  state = Combat.serialize();
  boss = state.actors.find(actor => actor.enemyDefinitionId === bossId);
  boss.character.currentHp = 1;
  state.current = null;
  state.actors.forEach(actor => { actor.counter = actor === boss ? 1000 : 0; });
  Combat.restore(state);
  Combat.onSheetChange();
  await wait(60);
  assert(Combat.serialize().actors.find(actor => actor.enemyDefinitionId === bossId).phaseIndex === 2 && cinematic.hidden, "the final phase cannot replay");
  Combat.restore(null);

  Guild.onEnemyDefeated({ guildObjective: "great-hunt", name: "Anatemis, the White Star" });
  assert(worldState.guild.greatHunt.complete, "defeating the boss completes the Hunt objective");
  worldState.regionId = "high-stoneveil";
  WorldRules.setActiveRegion(worldState.regionId);
  worldState.position = { x: 740, y: 510 };
  Guild.render();
  Guild.useCounter();
  assert(worldState.guild.greatHuntsCompleted.includes("anatemis-white-star") && !worldState.guild.greatHunt, "Cairnrest's counter turns in the completed Hunt");
  Guild.activate("trophies");
  document.querySelector('[data-guild-rematch="anatemis-white-star"]').click();
  assert(worldState.guild.greatHuntRematch?.regionId === "stoneveil-summit", "Cairnrest can place a Summit rematch marker");
  await runAnatemisReworkChecks();
}

// Anatemis rework (2026-10-16): a trial per form (Mirror Scales, Sunfall, Supernova), each form
// keeping the trials before it; her AI, look, intro, phase cutscenes and celestial hymn.
async function runAnatemisReworkChecks() {
  let checks = 0;
  const assert = (condition, message) => { if (!condition) throw Error(message); checks++; };
  const $ = id => document.getElementById(id);
  Combat.restore(null);
  const source = await (await fetch("scripts/combat.js")).text();
  const marker = "  window.Combat = {";
  const script = document.createElement("script");
  script.textContent = source.replace(marker, "  window.__anatemisChecks = { actorById, hp, values, enemyAction, endTurn, isControlledTurn, currentActor, getBattle: () => battle, stageLook, stageSnapshot, starState, hurt, directDamage };\n" + marker);
  document.head.append(script);
  await waitForSourceFiles();
  const hooks = window.__anatemisChecks;
  if (window.BattleStage) window.BattleStage.enabled = () => false;
  if (window.GameSettings) window.GameSettings.enemyDelay = () => 1e9;

  // Her sheet: the three trials, her own AI, intro and cutscenes.
  const raw = await (await fetch("enemies/" + encodeURIComponent("Anatemis, the White Star") + ".json")).json();
  const trials = raw.starTrials;
  assert(raw.aiProfile === "anatemis" && raw.introCinematic === "anatemis" && raw.phaseTransitions.map(phase => phase.cinematic).join() === "anatemis-golden,anatemis-zenith", "Anatemis has her own AI, intro and a cutscene for each change of form");
  assert(trials.mirror.reflectPercent === 25 && trials.mirror.breakPercent === 20 && trials.mirror.reformTurns === 3, "Mirror Scales reflect 25% of spell damage until weapon blows deal 20% of her HP, and reform 3 of her turns later");
  assert(trials.sunfall.fromPhase === 1 && trials.sunfall.damagePercent === 50 && trials.sunfall.deflectPercent === 8, "The Golden Star's Sunfall strikes a marked hero for 50% of their HP unless she takes 8% of hers first");
  assert(trials.supernova.fromPhase === 2 && trials.supernova.chargeTurns === 3 && trials.supernova.damagePercent === 60 && trials.supernova.collapsePercent === 15 && trials.supernova.staggerAp === 2, "The Zenith's supernova gathers over 3 turns and burns every hero for 60% unless 15% of her HP collapses it");
  assert(raw.maxHpPercent === 110 && raw.permanentBonuses.intelligence === -85 && raw.permanentBonuses.speed === -85 && raw.permanentBonuses.strength === -70, "Tuned for about 5% wins by a prepared Level 100 party");

  // A sturdy, quick hero on the Frozen Crown.
  const c = defaultCharacter();
  Object.assign(c, { raceId: "human", level: 100, knownSpells: ["fire-bolt-fire-damage"] });
  c.stats = { strength: 120, vitality: 400000, mind: 60, dexterity: 80, intelligence: 60, wisdom: 60, speed: 600, luck: 30 };
  applyCharacter(normalizeCharacter(c));
  currentHp = 1e9;
  worldState.regionId = "stoneveil-summit";
  assert(await Combat.startEncounter(["Anatemis, the White Star"], "Anatemis test", "great-hunt"), "Her fight starts");
  const intro = $("boss-intro-cinematic");
  assert(!intro.hidden && intro.dataset.boss === "anatemis" && intro.querySelector(".bi-boss svg") && intro.querySelectorAll(".bi-an-star").length > 40 && intro.querySelector(".bi-an-fall"), "A star falls onto the Frozen Crown in her intro");
  $("boss-intro-skip").click();
  assert(Combat.musicTheme() === "anatemis-white-star" && ["anatemis-white-star", "anatemis-golden-star", "anatemis-zenith"].every(id => MusicThemes.all().some(theme => theme.id === id)), "Her hymn plays, with a version for each form");
  const battle = hooks.getBattle(), player = () => hooks.actorById("player");
  const star = battle.actors.find(actor => actor.enemyDefinitionId === "anatemis-white-star-unique-level-100");
  const heal = () => { currentHp = hooks.values(player()).maxHp; };
  const step = () => { heal(); if (hooks.isControlledTurn()) hooks.endTurn(); else hooks.enemyAction(); };
  const until = (condition, limit = 400) => { for (let i = 0; i < limit && !condition() && Combat.active(); i++) step(); return condition(); };
  const max = hooks.values(star).maxHp;
  assert(until(() => battle.star) && battle.star.mirrorUp && hooks.stageSnapshot([]).order?.id === "mirror" && hooks.stageLook(star).mirror === true, "Her Mirror Scales gleam from the start");
  assert(Sprites.forActor(hooks.stageLook(star)).svg.includes("#9fe8ff"), "Constellations and aurora wings mark the White Star");

  // The White Star: spells come back at their caster; blades shatter the mirror, and it reforms.
  const before = hooks.hp(player());
  hooks.hurt(star, 1000, "fire", false, true, player(), "spell");
  assert(before - hooks.hp(player()) > 0 && battle.log.slice(-4).some(entry => String(entry).includes("The Mirror Scales throw")), "A spell against her is partly thrown back at its caster");
  battle.star.mirrorDamage = Math.floor(max * 0.2) - 1;
  hooks.hurt(star, 5000, "physical", false, false, player(), "melee");
  assert(!battle.star.mirrorUp && battle.star.mirrorShatters === 1 && battle.log.slice(-4).some(entry => String(entry).includes("shatter")), "Weapon blows shatter the Mirror Scales");
  assert(until(() => battle.star.mirrorUp) && battle.star.mirrorReforms === 1 && star.turns - battle.star.mirrorBrokenAt !== 0, "Three of her turns later the Mirror Scales reform");

  // The Golden Star: Sunfall, kept with the mirror.
  until(() => hooks.isControlledTurn());
  hooks.directDamage(star, 1e9, false, "holy", player());
  assert(star.phaseIndex === 1 && !intro.hidden && intro.dataset.boss === "anatemis-golden" && intro.querySelector(".bi-boss-before") && intro.querySelector(".bi-an-sun"), "At half health the Golden Star rises in her own cutscene");
  $("boss-intro-skip").click();
  assert(Combat.musicTheme() === "anatemis-golden-star", "The hymn becomes a radiant march");
  assert(until(() => battle.star.sunfallTarget) && player().effects.some(effect => effect.type === "sunfall-mark") && hooks.stageSnapshot([]).order?.id === "sunfall", "She marks a hero for Sunfall");
  const marked = hooks.hp(player()), markedTurn = star.turns;
  battle.star.sunfallDamage = 0;
  assert(until(() => star.turns > markedTurn) && battle.star.sunfalls === 1 && battle.log.slice(-12).some(entry => String(entry).includes("Sunfall strikes")), "At her next turn the sun falls on the marked hero");
  battle.star.sunfallDamage = Math.ceil(max * 0.08);
  const deflectTurn = star.turns;
  assert(until(() => star.turns > deflectTurn) && battle.star.deflections === 1, "Striking her for 8% of her HP turns the Sunfall aside");
  assert(battle.star.mirrorUp || battle.star.mirrorBrokenAt != null, "Her Mirror Scales remain in the Golden form");

  // The Zenith: the supernova, with Sunfall and the mirror still in force.
  until(() => hooks.isControlledTurn());
  hooks.directDamage(star, 1e9, false, "holy", player());
  assert(star.phaseIndex === 2 && !intro.hidden && intro.dataset.boss === "anatemis-zenith" && intro.querySelector(".bi-an-bloom"), "At 15% the Zenith of Dragonkind ignites in her own cutscene");
  $("boss-intro-skip").click();
  assert(Combat.musicTheme() === "anatemis-zenith", "The hymn's overwhelming finale plays");
  assert(until(() => battle.star.novaStart != null) && hooks.stageSnapshot([]).order?.id === "supernova" && hooks.stageLook(star).nova, "She begins to gather a supernova, and her heart-star swells");
  hooks.directDamage(star, Math.ceil(max * 0.16), false, "holy", player());
  assert(battle.star.novaCollapses === 1 && battle.star.novaStart == null && star.staggerAp >= 2, "Enough damage collapses the supernova and staggers her");
  assert(until(() => battle.star.novaStart != null), "She gathers the next one");
  battle.star.novaStart = star.turns - 3;
  const health = hooks.hp(player()), novaTurn = star.turns;
  assert(until(() => battle.star.detonations === 1) && battle.log.slice(-20).some(entry => String(entry).includes("The supernova strikes")), "Left alone, the supernova detonates and burns every hero");
  hooks.directDamage(star, 1e9, false, "holy", player());
  until(() => !Combat.active(), 30);
  assert(hooks.hp(star) <= 0 && battle.star.ended && !player().effects.some(effect => effect.type === "sunfall-mark"), "When she falls, her trials end");
  Combat.restore(null);
  return checks;
}

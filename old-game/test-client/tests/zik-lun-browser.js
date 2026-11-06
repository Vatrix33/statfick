async function runZikLunChecks() {
  let checks = 0;
  const assert = (condition, message) => { if (!condition) throw Error(message); checks++; };
  Combat.restore(null);
  // Deterministic test hooks exist only in this fresh, invisible renderer.
  const source = await (await fetch("scripts/combat.js")).text();
  const marker = "  window.Combat = {";
  assert(source.includes(marker), "Combat test entry point exists");
  const script = document.createElement("script");
  script.textContent = source.replace(marker, "  window.__zikChecks = { actorById, hp, values, setHp, enemyAction, endTurn, isControlledTurn, currentActor, getBattle: () => battle, stageLook, spellEffects, aiCandidates, runAiCandidate, largerHpWeaponBonus };\n" + marker);
  document.head.append(script);
  await waitForSourceFiles();
  const hooks = window.__zikChecks;
  if (window.BattleStage) window.BattleStage.enabled = () => false;
  if (window.GameSettings) window.GameSettings.enemyDelay = () => 1e9;
  const bossName = "Zik Lun, the armored Hulk";

  // The Puny Smasher and the Warsuit are retired; old copies become epic forged pieces.
  assert(!ForgeRules.isTemplateId?.("weapon-puny-smasher-meele") && ForgeRules.LEGACY_ITEMS["weapon-puny-smasher-meele"] === "forge-weapon-2-greathammer~epic" && ForgeRules.LEGACY_ITEMS["armor-great-steel-plated-warsuit-heavy"] === "forge-armor-full-plate~epic", "The retired hammer and warsuit migrate to epic forged pieces");
  const veteran = defaultCharacter();
  Object.assign(veteran, { raceId: "human", level: 24, inventory: ["weapon-puny-smasher-meele", "armor-great-steel-plated-warsuit-heavy"], equipment: { ...defaultEquipment, primaryHand: "weapon-puny-smasher-meele", offHand: "weapon-puny-smasher-meele", armor: "armor-great-steel-plated-warsuit-heavy" } });
  const migrated = normalizeCharacter(veteran);
  const hammer = getItem(migrated.equipment.primaryHand), plate = getItem(migrated.equipment.armor);
  assert(hammer?.forged?.type === "greathammer" && hammer.rarity === "epic" && plate?.forged?.type === "full-plate" && plate.rarity === "epic" && migrated.inventory.length === 2, "An old save's Puny Smasher and Warsuit become an epic Greathammer and Full Plate");
  // Giantslayer is a forged masterwork now: a Roundtable greatsword whose bite grows with its prey.
  const giantslayer = getItem("masterwork-giantslayer");
  const bane = giantslayer?.effects.find(effect => effect.kind === "damage-and-pierce-vs-larger-max-hp");
  assert(giantslayer?.rarity === "masterwork" && bane?.stepPercent === 20 && bane.amount === 10 && bane.maxAmount === 60 && bane.armorPiercePercent === 20, "Giantslayer: +10% damage per 20% more maximum HP, up to +60%, with 20% Armor Pierce");
  const forgedBlade = getItem(ForgeRules.encode(ForgeRules.generateMasterwork({ key: "giantslayer", level: 30, random: () => 0.5 })));
  assert(forgedBlade?.forged?.type === "greatsword" && forgedBlade.forged.parts[0].maker === "roundtable" && forgedBlade.name.endsWith("Giantslayer"), "It is forged as a greatsword whose main part is always Roundtable");
  assert(ForgeRules.LEGACY_ITEMS["weapon-giantslayer-meele"] === "masterwork-giantslayer" && getItem(normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: 24, inventory: ["weapon-giantslayer-meele"] }).inventory[0])?.name?.endsWith("Giantslayer"), "An old Giantslayer becomes the forged masterwork");
  const sized = hp => ({ resourceMax: { hp } }), bite = hp => hooks.largerHpWeaponBonus(sized(100), sized(hp), giantslayer);
  assert(bite(115).damagePercent === 0 && bite(115).armorPiercePercent === 0 && bite(120).damagePercent === 10 && bite(120).armorPiercePercent === 20 && bite(165).damagePercent === 30 && bite(400).damagePercent === 60, "The bonus grows by a step for every 20% more maximum HP and stops at +60%");

  // His sheet: a regular Great Hunt boss leading a warband.
  const raw = await (await fetch("enemies/" + encodeURIComponent(bossName) + ".json")).json();
  assert(!raw.classLevels["raid-boss"] && raw.aiProfile === "zik-lun" && raw.introCinematic === "zik-lun" && raw.knownSpells.join() === "war-horn-basic-unique", "No Raid Boss class; his own AI, intro and War Horn");
  assert(raw.equipment.primaryHand === "forge-weapon-2-greathammer" && raw.equipment.armor === "forge-armor-full-plate~rare" && raw.drops.length === 1 && raw.drops[0].itemId === "masterwork-giantslayer" && raw.drops[0].chance === 8, "Generic forged gear; only Giantslayer is in his drop table, at 8%");
  const band = raw.warband;
  assert(band.guard === "troll-shieldbearer-level-20" && band.drummer === "troll-war-drummer-level-20" && ["charge", "hold", "regroup"].every(id => band.orders[id]) && band.vengeance.maxStacks === 5 && band.hornEveryTurns === 3 && band.loseHeart.length === 2, "His warband has three orders, vengeance, a horn and loses heart");
  const horn = getSpell("war-horn-basic-unique");
  assert(horn?.catalogueAvailable === false && hooks.spellEffects(horn).some(effect => effect.type === "summon-friendly" && effect.enemyId === "hill-troll-level-14" && effect.maxActive === 2), "The War Horn calls Hill Trolls, at most two at once");

  // His Great Hunt: three zones, his warband as the roster.
  const scout = defaultCharacter();
  scout.raceId = "human"; scout.level = 24;
  applyCharacter(normalizeCharacter(scout));
  worldState.regionId = "stoneveil-foothills";
  WorldRules.setActiveRegion(worldState.regionId);
  worldState.position = { x: 1040, y: 550 };
  worldState.guild.greatHuntsCompleted.push("nygheir-the-wounded");
  Guild.render();
  const originalRandom = Math.random, seenZones = new Set();
  try {
    for (const value of [0.05, 0.4, 0.8]) {
      worldState.guild.boards = {};
      Math.random = () => value;
      const candidate = Guild.boardFor().offers.find(entry => entry.uniqueId === "zik-lun-armored-hulk");
      assert(candidate && WorldRules.zoneAt(candidate.point, candidate.regionId) === candidate.zone, "Each Great Hunt marker lands in its stated zone");
      seenZones.add(candidate.zone);
    }
  } finally { Math.random = originalRandom; }
  assert(seenZones.size === 3, "Zik Lun can appear in all three zones");
  const offer = Guild.boardFor().offers.find(entry => entry.uniqueId === "zik-lun-armored-hulk");
  Guild.accept(Guild.boardFor().offers.indexOf(offer));
  worldState.position = { ...offer.point };
  const objective = Guild.objectiveAt(worldState.position);
  assert(objective?.enemies.join() === [bossName, "Troll Shieldbearer", "Troll War-drummer"].join(), "His roster is his warband: a Shieldbearer and a War-drummer");

  // A sturdy, slow hero so every order can be watched.
  const F = ForgeRules;
  const sword = F.encode(F.generate({ type: "longsword", level: 25, source: "shop", random: () => 0.5 }));
  const armor = F.encode(F.generateArmor({ type: "full-plate", level: 25, source: "shop", random: () => 0.5 }));
  const hero = defaultCharacter();
  Object.assign(hero, { raceId: "human", level: 25, inventory: [sword, armor], equipment: { ...defaultEquipment, primaryHand: sword, armor } });
  hero.stats = { strength: 80, vitality: 400, mind: 25, dexterity: 60, intelligence: 22, wisdom: 25, speed: 20, luck: 30 };
  applyCharacter(normalizeCharacter(hero));
  currentHp = 1e6;
  assert(await Combat.startEncounter(objective.enemies, "Zik test", objective.kind), "His Great Hunt starts");
  const intro = document.getElementById("boss-intro-cinematic");
  assert(!intro.hidden && intro.dataset.boss === "zik-lun" && intro.querySelector("#boss-intro-title").textContent === bossName && intro.querySelector(".bi-boss svg") && intro.querySelectorAll(".bi-imp svg").length === 2, "His battle opens with his intro and both of his guards");
  document.getElementById("boss-intro-skip").click();
  assert(intro.hidden, "The intro can be skipped");

  const battle = hooks.getBattle(), byName = name => battle.actors.find(actor => actor.name === name);
  const zik = byName(bossName), guard = byName("Troll Shieldbearer"), drummer = byName("Troll War-drummer"), player = () => hooks.actorById("player");
  assert(zik.unique && zik.character.raceId === "troll" && zik.character.level === 24 && getItem(zik.character.equipment.primaryHand)?.forged?.type === "greathammer" && getItem(zik.character.equipment.armor)?.forged?.type === "full-plate", "Zik Lun carries a forged Greathammer and Full Plate");
  assert(zik.dropTable.length === 1 && zik.dropTable[0].itemId === "masterwork-giantslayer" && zik.dropTable[0].chance === 8, "He only rolls Giantslayer, at 8%");
  assert(Combat.musicTheme() === "zik-lun-armored-hulk" && Music.state().theme === "zik-lun-armored-hulk" && MusicThemes.all().some(theme => theme.id === "zik-lun-armored-hulk"), "His remastered siege-march plays");
  const art = Sprites.forActor(hooks.stageLook(zik)).svg;
  assert(art.includes(SpriteKit.tone("#7a2416")) && art.includes('class="sp-cape"') && Sprites.forActor(hooks.stageLook(drummer)).svg.includes(SpriteKit.tone("#6b3a22")), "The warlord carries his warband's standard and colours; the drummer his drum");

  const step = () => { if (hooks.isControlledTurn()) { currentHp = hooks.values(player()).maxHp; hooks.endTurn(); } else hooks.enemyAction(); };
  const until = (condition, limit = 400) => { for (let i = 0; i < limit && !condition() && Combat.active(); i++) step(); return condition(); };
  const orderEffects = actor => actor.effects.filter(effect => effect.warbandOrder);
  const amount = (actor, type) => orderEffects(actor).filter(effect => effect.type === type).reduce((sum, effect) => sum + effect.amount, 0);

  // Turn 1: Charge! with the drums: the warband hits harder and hunts the hero.
  assert(until(() => zik.turns >= 1), "Zik Lun takes his first turn");
  assert(battle.warband.order === "charge" && battle.warband.preyId === "player" && battle.log.some(entry => String(entry).includes(`${bossName} bellows "Charge!" and points at`) && String(entry).includes("war drums")), "He opens with Charge!, beaten louder on the war drums");
  assert(amount(guard, "outgoing-attack-damage-percent") === 15 && amount(player(), "incoming-attack-damage-percent") === 15, "The drums make Charge! half again as strong");

  // Turn 2: badly hurt, he orders Hold the Line and blows the war horn.
  zik.character.currentHp = Math.floor(hooks.values(zik).maxHp * 0.55);
  assert(until(() => zik.turns >= 2), "His second turn comes");
  assert(battle.warband.order === "hold" && zik.effects.some(effect => effect.type === "guardian-ward" && effect.guardianId === guard.id) && amount(drummer, "defense-percent") === 45, "Hurt, he orders Hold the Line: the warband's Armor rises and the Shieldbearer guards him");
  assert(until(() => !hooks.currentActor() || hooks.currentActor().id !== zik.id), "He finishes his turn");
  const called = () => battle.actors.filter(actor => actor.summoned && actor.summonDefinitionId === "hill-troll-level-14");
  assert(called().length === 1 && battle.log.some(entry => String(entry).includes("summons Hill Troll with War Horn")), "On his second turn he blows the war horn and a Hill Troll answers");
  const strikes = {};
  for (const entry of zik.ai?.history || []) if (entry.kind === "attack") strikes[entry.turn] = (strikes[entry.turn] || 0) + 1;
  assert(Object.values(strikes).every(count => count <= 1), "He swings his hammer at most once a turn");
  assert(until(() => hooks.isControlledTurn()), "The hero's turn comes");
  const swing = hooks.aiCandidates(player()).find(candidate => candidate.kind === "attack" && candidate.target === zik);
  const guardHp = hooks.hp(guard);
  try { Math.random = () => 0.3; hooks.runAiCandidate(player(), swing || { kind: "attack", target: zik }); } finally { Math.random = originalRandom; }
  assert(battle.log.some(entry => String(entry).includes("blocks the attack meant for")) && hooks.hp(guard) <= guardHp, "Holding the line, the Shieldbearer takes the blow meant for him");

  // Turn 3: two of them below half HP: Regroup! cleanses and heals them, unreduced.
  for (const actor of [guard, drummer]) actor.character.currentHp = Math.floor(hooks.values(actor).maxHp * 0.4);
  const before = hooks.hp(drummer);
  assert(until(() => zik.turns >= 3), "His third turn comes");
  assert(battle.warband.order === "regroup" && hooks.hp(drummer) >= before + Math.floor(hooks.values(drummer).maxHp * 0.12) - 1, "With his trolls hurt he orders Regroup!, and they recover 12% HP");

  // A fallen troll feeds his Vengeance; without the drummer the drums fall silent.
  hooks.setHp(drummer, 0);
  assert(until(() => zik.turns >= 4), "His fourth turn comes");
  const vengeance = zik.effects.filter(effect => String(effect.key || "").startsWith("warband-vengeance"));
  assert(vengeance.some(effect => effect.type === "outgoing-attack-damage-percent" && effect.amount === 10) && vengeance.some(effect => effect.type === "stat-percent" && effect.target === "speed" && effect.amount === 6) && battle.log.some(entry => String(entry).includes("roars over the fallen Troll War-drummer: Vengeance (1)")), "The War-drummer's fall gives him Vengeance (1)");
  const lastOrder = battle.log.filter(entry => String(entry).includes(`${bossName} bellows`)).at(-1);
  assert(battle.warband.scale === 1 && lastOrder && !String(lastOrder).includes("war drums"), "With the drummer gone the drums fall silent");
  assert(orderEffects(called()[0]).length > 0, "The troll his horn called takes up his orders too");

  // If he falls first, his warband loses heart.
  hooks.setHp(zik, 0);
  assert(until(() => battle.warband.ended), "His fall ends the warband");
  const survivors = battle.actors.filter(actor => actor !== zik && hooks.hp(actor) > 0 && actor.team === zik.team);
  assert(survivors.length >= 2 && survivors.every(actor => actor.effects.some(effect => effect.name === "Lost Heart" && effect.type === "outgoing-attack-damage-percent" && effect.amount === -30) && !orderEffects(actor).length), "The trolls left fight on weakened, with no orders");
  assert(battle.log.some(entry => String(entry).includes(`With ${bossName} fallen, his warband loses heart.`)), "The battle log says they lose heart");
  Combat.restore(null);
  return checks;
}

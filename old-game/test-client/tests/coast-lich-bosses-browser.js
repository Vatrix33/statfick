async function runCoastLichBossChecks() {
  let checks = 0;
  const assert = (condition, message) => { if (!condition) throw Error(message); checks++; };
  const $ = id => document.getElementById(id), clone = value => structuredClone(value);
  Combat.restore(null);
  const source = await (await fetch("scripts/combat.js")).text();
  const marker = "  window.Combat = {";
  assert(source.includes(marker), "Isolated combat test entry point exists");
  const script = document.createElement("script");
  script.textContent = source.replace(marker, "  window.__coastLichChecks = { actorById, values, applyCombatDamage, directDamage, applyUniqueWeaponHitEffects, estimatedWeaponStrike, strike, summonFriendly, expire, awardDefeatedEnemyExperience, validateEnemy, opponents };\n" + marker);
  document.head.append(script);
  await waitForSourceFiles();
  const h = window.__coastLichChecks;
  const trident = getItem("masterwork-royal-trident"), staff = getItem(ForgeRules.encode(ForgeRules.generateMasterwork({ key: "staff-of-the-guild", level: 100, random: () => 0.5 }))), dagger = getItem(ForgeRules.encode(ForgeRules.generateMasterwork({ key: "heartless-enricher", level: 100, random: () => 0.5 }))), rain = getItem("artifact-arrowtip-rain-artifact");
  assert([rain].every(item => item?.rarity === "masterwork" && item.shopAvailable === false && item.automaticDrop === false) && [staff, dagger, trident].every(item => item?.rarity === "masterwork" && item.shopAvailable === false) && staff.forged?.masterwork === "staff-of-the-guild" && dagger.forged?.masterwork === "heartless-enricher", "All unique equipment is loaded and excluded from shops and automatic drops");
  assert(artifactEffectLabel(staff.effects.find(effect => effect.summonOnly)).includes("Summon spells only") && !staff.effects.some(effect => effect.minTier) && staff.effects.some(effect => effect.kind === "summon-empowerment" && effect.amount === 40 && effect.manaPercent === 40 && effect.damagePercent === 30), "The Staff of the Guild is made for summoners: cheaper summons that rise 40% stronger and strike 30% harder");
  const main = defaultCharacter(); main.raceId = "human"; main.level = 100; main.stats.speed = 1000; main.currentHp = 10000; main.currentMana = 10000;
  applyCharacter(normalizeCharacter(main));

  const questAt = (regionId, huntId) => {
    worldState.regionId = regionId; WorldRules.setActiveRegion(regionId);
    const city = WorldRules.settlementsForRegion(regionId).find(place => place.kind === "Castle city");
    worldState.position = { x: city.x, y: city.y }; worldState.guild.quest = null; worldState.guild.greatHunt = null; worldState.guild.boards = {};
    Guild.render();
    const hunt = Guild.boardFor().offers.find(offer => offer.uniqueId === huntId);
    assert(hunt, huntId + " is offered by its local guild");
    assert(WorldRules.zoneAt(hunt.point, hunt.regionId) === hunt.zone, huntId + " has a valid battle marker in the correct zone");
    return hunt;
  };
  const seaQuest = questAt("sunward-coast", "deep-sea-king");
  assert(seaQuest.offeredAtLevel === 32 && seaQuest.regionId === "sunward-coast" && seaQuest.zone === "Beach", "King hunt is level 32 on the Sunward beach");
  const oolQuest = questAt("duskpine-forest", "ancient-lich-ool");
  assert(oolQuest.offeredAtLevel === 78 && oolQuest.originRegionId === "duskpine-forest" && oolQuest.regionId === "ancient-battlefield" && oolQuest.zone === "Mass Graves", "Ool is hunted in the Battlefield and accepted/completed at Ravenwatch");
  worldState.regionId = "sunward-coast"; WorldRules.setActiveRegion(worldState.regionId); worldState.position = { x: 560, y: 500 };
  worldState.guild.boards = {}; Guild.render();
  assert(!Guild.boardFor().offers.some(offer => offer.uniqueId === "ancient-lich-ool"), "Ool's hunt does not leak into other regions");

  assert(await Combat.startEncounter(["Deep Sea King"]), "Deep Sea King fight starts");
  $("combat-sheet-button").click();
  let state = Combat.serialize(), king = state.actors.find(actor => actor.team === "enemy");
  assert(king.character.level === 32 && king.character.raceId === "merfolk" && king.character.classLevels["raid-boss"] === 1, "King's level, race and Raid Boss class are correct");
  assert(king.noDropItems.includes(king.character.equipment.armor), "Only this King's normal rare light armor is excluded from loot");
  king.character.currentHp = 0;
  Combat.restore(state);
  const random = Math.random;
  try { Math.random = () => 0; h.awardDefeatedEnemyExperience(); } finally { Math.random = random; }
  const seaLoot = Combat.serialize().lootItems;
  assert(seaLoot.some(id => id === trident.id || id.startsWith("weapon-masterwork-royal-trident-")) && !seaLoot.includes(rain.id) && !seaLoot.includes(king.character.equipment.armor), "Successful rolls drop the Royal Trident (Arrowtip Rain now comes from merfolk) but never the King's armor");
  Combat.restore(null);
  assert(await Combat.startEncounter(["Ancient Lich Ool"], "Ool test", "great-hunt"), "Ool encounter starts");
  $("combat-sheet-button").click();
  state = Combat.serialize();
  const ool = state.actors.find(actor => actor.enemyDefinitionId === "ancient-lich-ool-unique-level-78");
  const legion = state.actors.filter(actor => actor.summonDefinitionId === "gravebound-legionnaire-level-68");
  assert(ool.character.level === 78 && ool.character.raceId === "undead" && ool.character.classLevels.lich === 5 && ool.character.classLevels["raid-boss"] === 1, "Ool is an Undead Lich and Raid Boss");
  assert(legion.length === 3 && legion.every(actor => actor.summoned && actor.summonerId === ool.id && actor.team === "enemy"), "Exactly three Legionnaires start as Ool's owned summons");
  for (const actor of legion) {
    const ordinary = h.values({ ...clone(actor), resourceMax: null });
    assert(actor.resourceMax.hp === Math.floor(ordinary.maxHp * 1.4) && actor.resourceMax.mana === Math.floor(ordinary.maxMana * 1.4), "Staff grants each starting summon 40% HP and Mana: " + JSON.stringify({ actual: actor.resourceMax, ordinary: { hp: ordinary.maxHp, mana: ordinary.maxMana } }));
    assert(actor.character.currentHp === actor.resourceMax.hp && actor.character.currentMana === actor.resourceMax.mana, "Starting summons have fully restored, increased resources");
    assert(actor.effects.some(effect => effect.type === "outgoing-attack-damage-percent" && effect.amount === 30 && !effect.attackKinds?.length), "Staff grants all summoned damage, not just melee damage, a 30% increase");
  }
  Combat.restore(state);
  assert(h.opponents(h.actorById("player")).some(actor => actor.id === ool.id), "His phylactery, not Overprotective, keeps Ool alive: the heroes can reach him while his Legionnaires stand");
  const resumed = Combat.serialize(); Combat.restore(resumed);
  assert(Combat.serialize().actors.filter(actor => actor.summoned && actor.summonerId === ool.id).length === 3, "Reloading a fight preserves summon ownership without duplicating the starting entourage");
  const started = performance.now(), decision = Combat.previewAiDecision(ool.id);
  assert(decision?.kind && performance.now() - started < 1000, "Ool can choose an action without a performance stall");
  state = Combat.serialize(); state.actors.filter(actor => actor.team === "enemy").forEach(actor => { actor.character.currentHp = 0; }); Combat.restore(state);
  try { Math.random = () => 0; h.awardDefeatedEnemyExperience(); } finally { Math.random = random; }
  const lichLoot = Combat.serialize().lootItems;
  assert(lichLoot.some(id => id.startsWith("weapon-masterwork-staff-of-the-guild-") && !Combat.serialize().actors.some(actor => (actor.noDropItems || []).includes(id))) && lichLoot.some(id => id.startsWith("weapon-masterwork-heartless-enricher-")) && lichLoot.includes("key-item-phylactery-core"), "Successful Ool drop rolls yield his staff, dagger and Lich acquisition item");
  assert(!lichLoot.some(id => /^weapon-forged-mace-/.test(id)), "Starting summoned Legionnaires do not create normal enemy loot");
  Combat.restore(null);

  // A plain caster isolates the staff from class, race and other discounts.
  const caster = defaultCharacter(); caster.raceId = "human";
  const plainSpells = [5, 6, 8].map(tier => spells.find(spell => spell.tier === tier && !isSummonSpell(spell)));
  const summonFive = getSpell("summon-undead-hulk-death-summon"), summonNine = getSpell("summon-pale-monarch-death-summon");
  applyCharacter(normalizeCharacter(caster));
  const costs = [...plainSpells, summonFive, summonNine].map(calculateSpellManaCost);
  caster.inventory = [staff.id]; caster.equipment = { ...defaultEquipment, primaryHand: staff.id, offHand: staff.id };
  applyCharacter(normalizeCharacter(caster));
  const staffEffects = staff.effects, withStaff = [...plainSpells, summonFive, summonNine].map(calculateSpellManaCost);
  staff.effects = staffEffects.filter(effect => !(effect.kind === "spell-cost-percent" && effect.summonOnly));
  let withoutDiscount; try { withoutDiscount = [...plainSpells, summonFive, summonNine].map(calculateSpellManaCost); } finally { staff.effects = staffEffects; }
  [...plainSpells, summonFive, summonNine].forEach((spell, index) => {
    assert(withStaff[index] <= costs[index] && (isSummonSpell(spell) ? withStaff[index] < withoutDiscount[index] : withStaff[index] === withoutDiscount[index]), "The staff's own discount applies to summon spells only (its forged parts discount every spell): " + spell.name);
  });
  const enhancedSpell = getSpell("summon-imp-retinue-dark-summon");
  assert(await Combat.startEncounter(["Goblin", "Goblin", "Goblin"]), "Equipment mechanics fixture starts");
  $("combat-sheet-button").click();
  const fixture = Combat.serialize(); fixture.current = "player"; fixture.ap = 5;
  fixture.actors.filter(actor => actor.team === "enemy").forEach(actor => {
    actor.character.classLevels = {}; actor.character.raceId = "human"; actor.character.stats = { ...defaultStats };
    actor.character.equipment = { ...defaultEquipment }; actor.character.inventory = [];
    actor.effects = []; actor.resistances = {}; actor.resourceMax = { hp: 10000, mana: 1000 }; actor.character.currentHp = 10000; actor.character.currentMana = 1000;
    actor.turns = 0; actor.baseArmor = 0;
  });
  const reset = () => { Combat.restore(clone(fixture)); return { player: h.actorById("player"), target: h.actorById("enemy-1"), other: h.actorById("enemy-2") }; };
  let actors = reset();
  assert(h.summonFriendly(actors.player, enhancedSpell.effects[0], "Equipment stack check"), "Staff can empower newly created summons as well as starting summons");
  let summoned = Combat.serialize().actors.find(actor => actor.summoned);
  assert(summoned.resourceMax.hp === Math.floor(Combat.previewValues(summoned.character).maxHp * 2 * 1.4), "Staff's 40% HP bonus multiplies the summon spell's existing doubled HP");
  const preservedCaps = clone(summoned.resourceMax), save = Combat.serialize(); Combat.restore(save);
  summoned = Combat.serialize().actors.find(actor => actor.summoned);
  assert(JSON.stringify(summoned.resourceMax) === JSON.stringify(preservedCaps), "Enhanced summon HP/Mana caps survive saves unchanged");

  actors = reset(); actors.target.character.inventory = [trident.id]; actors.target.character.equipment = { ...defaultEquipment, primaryHand: trident.id, offHand: trident.id };
  const basic = h.values(actors.target), cap = clone(actors.target.resourceMax);
  h.applyCombatDamage(actors.target, 0);
  assert(!actors.target.effects.length, "Zero damage cannot trigger Royal Trident");
  h.applyCombatDamage(actors.target, 1);
  assert(actors.target.effects.filter(effect => effect.type === "all-stats-percent").length === 1 && h.values(actors.target).strength > basic.strength, "Taking damage activates one +5% all-stats buff");
  h.expire(actors.target); h.directDamage(actors.target, 1, false);
  const refreshed = actors.target.effects.find(effect => effect.type === "all-stats-percent");
  assert(refreshed.amount === 10 && refreshed.expiresTurn === actors.target.turns + 2 && actors.target.effects.filter(effect => effect.type === "all-stats-percent").length === 1, "A second hit adds a stack (+10%) and refreshes the two-turn buff");
  h.directDamage(actors.target, 1, false); h.directDamage(actors.target, 1, false);
  assert(actors.target.effects.find(effect => effect.type === "all-stats-percent").amount === 15, "The Royal Trident stops stacking at +15%");
  assert(JSON.stringify(actors.target.resourceMax) === JSON.stringify(cap), "Trident stat changes never alter maximum HP or Mana in battle");
  h.expire(actors.target); h.expire(actors.target);
  assert(!actors.target.effects.some(effect => effect.type === "all-stats-percent"), "The Trident buff expires after two personal turns");
  const bow = getItem("weapon-forged-longbow-30-hmque-elque-hmelv-s0");
  actors = reset(); inventory = [rain.id]; equipment = { ...defaultEquipment, artifact1: rain.id };
  const otherBefore = actors.other.character.currentHp, primaryBefore = actors.target.character.currentHp;
  h.applyUniqueWeaponHitEffects(actors.player, actors.target, bow, 100, 1);
  // A plain Arrowtip Rain is a Level 80 copy since the Artifact Rework (port task 4, A11, A16): 25%.
  assert(actors.other.character.currentHp === otherBefore - 25 && h.actorById("enemy-3").character.currentHp === 9975, "Ranged damage splashes exactly 25% onto each other enemy");
  assert(actors.target.character.currentHp === primaryBefore, "Arrowtip Rain does not damage the main target twice");
  h.applyUniqueWeaponHitEffects(actors.player, actors.target, trident, 100, 1); h.applyUniqueWeaponHitEffects(actors.player, actors.target, bow, 0, 1);
  assert(actors.other.character.currentHp === otherBefore - 25, "Melee and zero-damage attacks do not trigger Arrowtip Rain");

  actors = reset(); actors.target.baseArmor = 10000;
  const pierce = h.estimatedWeaponStrike(actors.player, actors.target, dagger, .25, true);
  const armored = h.estimatedWeaponStrike(actors.player, actors.target, dagger, .25, false);
  actors.target.baseArmor = 0;
  const noArmor = h.estimatedWeaponStrike(actors.player, actors.target, dagger, .25, true);
  assert(pierce.normalDamage === noArmor.normalDamage && armored.normalDamage < pierce.normalDamage, "Dagger's off-hand forecasts ignore all armor, while its primary-hand damage does not");
  actors.target.baseArmor = 10000;
  try { Math.random = () => .2; h.strike(actors.player, actors.target, dagger, .25, true); } finally { Math.random = random; }
  assert(actors.target.character.currentHp === 10000 - pierce.normalDamage, "Actual off-hand damage matches its 100% Armor Pierce forecast");
  Combat.restore(null);
  checks += await runDeepSeaKingChecks();
  checks += await runOolChecks();
  return { passed: true, checks };
}

// Deep Sea King rework (2026-10-11): the royal duel, his look, intro and music, and Arrowtip Rain
// moving to ordinary merfolk.
async function runDeepSeaKingChecks() {
  let checks = 0;
  const assert = (condition, message) => { if (!condition) throw Error(message); checks++; };
  Combat.restore(null);
  const source = await (await fetch("scripts/combat.js")).text();
  const marker = "  window.Combat = {";
  const script = document.createElement("script");
  script.textContent = source.replace(marker, "  window.__kingChecks = { actorById, hp, values, setHp, enemyAction, endTurn, isControlledTurn, currentActor, getBattle: () => battle, stageLook, opponents, royalDuelDamagePercent };\n" + marker);
  document.head.append(script);
  await waitForSourceFiles();
  const hooks = window.__kingChecks;
  if (window.BattleStage) window.BattleStage.enabled = () => false;
  if (window.GameSettings) window.GameSettings.enemyDelay = () => 1e9;

  // His sheet: a dueling Raid Boss who fights alone; Arrowtip Rain now comes from merfolk.
  const raw = await (await fetch("enemies/Deep%20Sea%20King.json")).json();
  assert(raw.classLevels["raid-boss"] === 1 && raw.aiProfile === "deep-sea-king" && raw.introCinematic === "deep-sea-king" && !raw.roster, "A Raid Boss with his own AI and intro, fighting alone");
  assert(raw.royalDuel.durationTurns === 2 && raw.royalDuel.outsideDamagePercent === -50 && raw.royalDuel.triumph.healPercent === 10 && raw.royalDuel.honor.length === 3, "His royal duel: two turns, others at half strength, a fallen champion heals him, a surviving one is honored");
  assert(!raw.drops.some(drop => drop.itemId === "artifact-arrowtip-rain-artifact"), "He no longer drops Arrowtip Rain");
  // The Royal Trident is a forged masterwork: a spear whose main part is always Hellmade.
  const template = getItem("masterwork-royal-trident"), tridentEffect = template?.effects.find(effect => effect.kind === "all-stats-on-damage");
  assert(template?.rarity === "masterwork" && tridentEffect?.amount === 5 && tridentEffect.maxStacks === 3 && tridentEffect.durationTurns === 2, "Royal Trident: every hit taken adds +5% to all basic stats, up to +15%, for 2 turns");
  const wielded = getItem(raw.equipment.primaryHand);
  assert(wielded?.forged?.type === "spear" && wielded.forged.parts[0].maker === "hellmade" && wielded.name.endsWith("Royal Trident") && raw.noDropItems.includes(raw.equipment.primaryHand), "He wields a forged Royal Trident, Hellmade at its heart, which never drops as ordinary gear");
  // Port task 7 adds one Exotic to each of these bosses' tables (it rolls only while Limitless is on).
  assert(raw.drops.length === 2 && raw.drops[0].itemId === "masterwork-royal-trident" && raw.drops[0].chance === 12 && raw.drops[1].itemId === "masterwork-fletcher" && raw.drops[1].chance === 4, "He drops the Royal Trident at 12%, and Fletcher (4%)");
  assert(ForgeRules.LEGACY_ITEMS["weapon-royal-trident-meele"] === "masterwork-royal-trident" && getItem(normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: 35, inventory: ["weapon-royal-trident-meele"] }).inventory[0])?.name?.endsWith("Royal Trident"), "An old Royal Trident becomes the forged masterwork");
  for (const name of ["Merfolk Spearfisher", "Reed Merfolk", "Merfolk Reef Guard", "Merfolk Steamcaller", "Merfolk Tidecaller"]) {
    const merfolk = await (await fetch("enemies/" + encodeURIComponent(name) + ".json")).json();
    assert(merfolk.drops?.some(drop => drop.itemId === "artifact-arrowtip-rain-artifact" && drop.chance === 1), name + " drops Arrowtip Rain at 1%");
  }

  // A hero and one companion, both sturdy, so the duel can be watched.
  const F = ForgeRules;
  const blade = F.encode(F.generate({ type: "longsword", level: 35, source: "shop", random: () => 0.5 }));
  const plate = F.encode(F.generateArmor({ type: "full-plate", level: 35, source: "shop", random: () => 0.5 }));
  const sturdy = () => { const c = defaultCharacter(); Object.assign(c, { raceId: "human", level: 35, inventory: [blade, plate], equipment: { ...defaultEquipment, primaryHand: blade, armor: plate } }); c.stats = { strength: 90, vitality: 420, mind: 25, dexterity: 70, intelligence: 22, wisdom: 25, speed: 110, luck: 30 }; return normalizeCharacter(c); };
  applyCharacter(sturdy());
  currentHp = 1e6;
  companions = [{ id: "companion-king-test", name: "Mira", gender: "female", character: sturdy() }];
  companions[0].character.currentHp = 1e6;
  worldState.regionId = "sunward-coast";
  assert(await Combat.startEncounter(["Deep Sea King"], "King test", "great-hunt"), "His fight starts");
  const intro = document.getElementById("boss-intro-cinematic");
  assert(!intro.hidden && intro.dataset.boss === "deep-sea-king" && intro.querySelector("#boss-intro-title").textContent === "Deep Sea King" && intro.querySelector(".bi-boss svg") && intro.querySelectorAll(".bi-dk-wave").length > 4, "He rises from the waves in his intro");
  document.getElementById("boss-intro-skip").click();
  assert(Combat.musicTheme() === "deep-sea-king" && MusicThemes.all().some(theme => theme.id === "deep-sea-king"), "His sea shanty plays");
  const battle = hooks.getBattle(), king = battle.actors.find(actor => actor.enemyDefinitionId === "deep-sea-king-unique-level-32");
  const mira = battle.actors.find(actor => actor.id === "companion-king-test"), player = () => hooks.actorById("player");
  const look = Sprites.forActor(hooks.stageLook(king)).svg;
  assert(look.includes(SpriteKit.tone("#d8c8b0")) && look.includes(SpriteKit.tone("#f0907a")), "He wears his shell armor and coral crown, not the brigandine's plates");

  const heal = () => { for (const actor of [player(), mira].filter(actor => hooks.hp(actor) > 0)) { if (actor.id === "player") currentHp = hooks.values(actor).maxHp; else actor.character.currentHp = hooks.values(actor).maxHp; } };
  const step = () => { if (hooks.isControlledTurn()) { heal(); hooks.endTurn(); } else hooks.enemyAction(); };
  const until = (condition, limit = 400) => { for (let i = 0; i < limit && !condition() && Combat.active(); i++) step(); return condition(); };

  // He names his champion at the end of his first turn: whoever hurt him most.
  assert(until(() => battle.royalDuel && !battle.royalDuel.ended), "The royal duel begins with the fight");
  battle.royalDuel.damageBy = { [mira.id]: 900, player: 100 };
  assert(until(() => battle.royalDuel.championId), "He names a champion");
  assert(battle.royalDuel.championId === mira.id && battle.log.some(entry => String(entry).includes("levels the Royal Trident at Mira")), "He challenges the companion who hurt him most");
  assert(hooks.opponents(king).map(actor => actor.id).join() === mira.id, "In the duel he can reach only his champion");
  assert(hooks.royalDuelDamagePercent(king, player()) === -50 && hooks.royalDuelDamagePercent(king, mira) === 0, "Other blows lose half their force; the champion's do not");
  assert(Combat.active(), "A king dueling a companion still stands: the battle goes on (the old victory bug)");
  const duelStart = king.turns;
  assert(until(() => king.turns >= duelStart + 1 && hooks.currentActor()?.id !== king.id) && Combat.active(), "He fights his first duel turn");
  const struck = (king.ai?.history || []).filter(entry => entry.turn === duelStart + 1 && entry.kind === "attack");
  assert(struck.length <= 1 && struck.every(entry => !entry.target || entry.target === mira.id), "In a duel he strikes once a turn, only at his champion");

  // A fallen champion lifts him: he heals and gains Triumph.
  king.character.currentHp = Math.floor(hooks.values(king).maxHp * 0.5);
  const before = hooks.hp(king);
  hooks.setHp(mira, 0);
  assert(until(() => battle.royalDuel.triumphs >= 1), "The fallen champion is noticed");
  assert(hooks.hp(king) >= before + Math.floor(hooks.values(king).maxHp * 0.1) - 1 && king.effects.some(effect => effect.name === "Triumph (1)" && effect.amount === 10), "He heals 10% and gains Triumph (1)");

  // The next duel goes to the hero; surviving it earns the King's honor.
  assert(until(() => battle.royalDuel.championId === "player"), "He challenges the hero next");
  const second = king.turns;
  assert(until(() => !battle.royalDuel.championId || king.turns > second + 2), "The second duel ends");
  assert(player().effects.some(effect => effect.name === "Honored by the King" && effect.type === "outgoing-attack-damage-percent" && effect.amount === 15) && battle.log.some(entry => String(entry).includes("honors the champion")), "Surviving the duel, the hero is honored");

  // His fall ends the duel, and the battle with it.
  hooks.setHp(king, 0);
  assert(until(() => !Combat.active(), 20) || !Combat.active(), "His fall ends the battle");
  companions = [];
  Combat.restore(null);
  return checks;
}

// Ancient Lich Ool rework (2026-10-12): the phylactery (dust and re-forming, the Legion's guard,
// mortal and weakened once broken), his look, intro, shatter cutscene and requiem music.
async function runOolChecks() {
  let checks = 0;
  const assert = (condition, message) => { if (!condition) throw Error(message); checks++; };
  const $ = id => document.getElementById(id);
  Combat.restore(null);
  const source = await (await fetch("scripts/combat.js")).text();
  const marker = "  window.Combat = {";
  const script = document.createElement("script");
  script.textContent = source.replace(marker, "  window.__oolChecks = { actorById, hp, values, enemyAction, endTurn, isControlledTurn, currentActor, getBattle: () => battle, stageLook, stageSnapshot, redirectedGuardTarget, directDamage, actionAllowed, aiProfileOf, checkOutcome, opponents };\n" + marker);
  document.head.append(script);
  await waitForSourceFiles();
  const hooks = window.__oolChecks;
  if (window.BattleStage) window.BattleStage.enabled = () => false;
  if (window.GameSettings) window.GameSettings.enemyDelay = () => 1e9;

  // His sheet: the phylactery, his own AI, intro and cutscene; no Overprotective any more.
  const raw = await (await fetch("enemies/" + encodeURIComponent("Ancient Lich Ool") + ".json")).json();
  const vesselRaw = await (await fetch("enemies/" + encodeURIComponent("Phylactery of Ool") + ".json")).json();
  assert(raw.aiProfile === "ool" && raw.introCinematic === "ool" && raw.unlivingCaster === true && raw.classLevels.summoner < 3, "Ool has his own AI and intro, casts despite Unliving, and is no longer shielded by Overprotective");
  const config = raw.phylactery;
  assert(config.enemyId === vesselRaw.id && config.reformRounds === 2 && config.reformHpPercent === 50 && config.guards.join() === "gravebound-legionnaire-level-68" && config.guardAiProfile === "grave-guard" && config.cinematic === "ool-phylactery"
    && config.broken.effects.some(effect => effect.type === "spell-power-percent" && effect.amount === -20), "His phylactery: re-forms after 2 rounds at 50% HP, guarded by his Legionnaires, and costs him 20% spell power when broken");
  assert(raw.maxHpPercent === 900 && vesselRaw.maxHpPercent === 3000, "Tuned for about 20% wins by a prepared Level 81 party: Ool at 900% HP, his phylactery at 3000%");
  assert(vesselRaw.inert === true && raw.startingAllies.some(entry => entry.enemyId === vesselRaw.id && entry.count === 1) && raw.drops.some(drop => drop.itemId === "key-item-phylactery-core" && drop.chance === 20), "The phylactery is an inert object on the field, and the Phylactery Core still drops at 20%");

  // A sturdy hero in the Mass Graves.
  const blade = ForgeRules.encode(ForgeRules.generate({ type: "longsword", level: 80, source: "shop", random: () => 0.5 }));
  const mail = ForgeRules.encode(ForgeRules.generateArmor({ type: "full-plate", level: 80, source: "shop", random: () => 0.5 }));
  const c = defaultCharacter();
  Object.assign(c, { raceId: "human", level: 80, inventory: [blade, mail], equipment: { ...defaultEquipment, primaryHand: blade, armor: mail } });
  c.stats = { strength: 120, vitality: 3000, mind: 25, dexterity: 80, intelligence: 22, wisdom: 25, speed: 60, luck: 30 };
  applyCharacter(normalizeCharacter(c));
  currentHp = 1e7;
  worldState.regionId = "ancient-battlefield";
  const begin = async () => { Combat.restore(null); currentHp = 1e7; assert(await Combat.startEncounter(["Ancient Lich Ool"], "Ool test", "great-hunt"), "His fight starts"); };
  await begin();
  const intro = $("boss-intro-cinematic");
  assert(!intro.hidden && intro.dataset.boss === "ool" && intro.querySelector(".bi-boss svg") && intro.querySelectorAll(".bi-imp").length === 4 && intro.querySelectorAll(".bi-oo-wisp").length > 10, "Soul-fire rises from the graves, his Legionnaires and phylactery appear, and he rises in his intro");
  $("boss-intro-skip").click();
  assert(Combat.musicTheme() === "ancient-lich-ool" && ["ancient-lich-ool", "ancient-lich-ool-mortal"].every(id => MusicThemes.all().some(theme => theme.id === id)), "His requiem plays");
  let battle = hooks.getBattle();
  const player = () => hooks.actorById("player");
  let ool = battle.actors.find(actor => actor.enemyDefinitionId === "ancient-lich-ool-unique-level-78");
  let vessel = battle.actors.find(actor => actor.enemyDefinitionId === "phylactery-of-ool-level-78");
  const legion = () => battle.actors.filter(actor => actor.summonDefinitionId === "gravebound-legionnaire-level-68" && hooks.hp(actor) > 0);
  assert(vessel && vessel.inert && legion().length === 3 && hooks.aiProfileOf(legion()[0]) === "grave-guard", "His phylactery stands on the field beside three grave-knights who fight as its guards");
  assert(hooks.actionAllowed(ool, "spell") && hooks.opponents(player()).some(actor => actor.id === ool.id), "He casts his spells, and the heroes can reach him while his Legionnaires stand");
  const look = Sprites.forActor(hooks.stageLook(ool)).svg;
  assert(look.includes(SpriteKit.tone("#1f2620")) && Sprites.forActor(hooks.stageLook(vessel)).svg.includes(SpriteKit.tone("#1c2628")), "He wears his grave-robes, and the phylactery is a dark crystal in its cage");
  assert(hooks.stageSnapshot([]).order?.id === "phylactery" && hooks.stageSnapshot([]).order.prey === "3 Legionnaires", "The badge shows the phylactery and its guard");

  // The Legion throws itself before single blows meant for the phylactery; area attacks reach it.
  // (A Legionnaire's taunt may pull any attack aside first; hold the dice so only the guard acts.)
  const random = Math.random;
  let guard, areaTarget;
  try { Math.random = () => 0.999; guard = hooks.redirectedGuardTarget(player(), vessel, "melee"); areaTarget = hooks.redirectedGuardTarget(player(), vessel, "melee", true); } finally { Math.random = random; }
  assert(guard && guard.summonDefinitionId === "gravebound-legionnaire-level-68" && battle.log.some(entry => String(entry).includes("throws itself before the blow meant for")), "A Legionnaire takes a single blow meant for the phylactery");
  assert(areaTarget?.id === vessel.id, "An area attack still reaches the phylactery");

  const heal = () => { currentHp = hooks.values(player()).maxHp; };
  const step = () => { if (hooks.isControlledTurn()) { heal(); hooks.endTurn(); } else hooks.enemyAction(); };
  const until = (condition, limit = 300) => { for (let i = 0; i < limit && !condition() && Combat.active(); i++) step(); return condition(); };
  let vesselActed = false;
  for (let i = 0; i < 30 && Combat.active(); i++) { if (hooks.currentActor()?.id === vessel.id) vesselActed = true; step(); }
  assert(!vesselActed && vessel.turns > 0, "The phylactery never takes a turn of its own; its turns only count the rounds");

  // At 0 HP he crumbles to dust: no reward yet, and he re-forms two rounds later at half health.
  hooks.directDamage(ool, 1e8, false, "holy", player());
  hooks.checkOutcome();
  assert(hooks.hp(ool) <= 0 && Combat.active() && !ool.experienceAwarded && battle.phylactery.dustSince != null && battle.log.some(entry => String(entry).includes("crumbles to dust")), "He crumbles to dust and awards nothing while his phylactery stands");
  const dust = hooks.stageSnapshot([]).order;
  assert(dust?.id === "dust" && dust.turnsLeft === 2 && dust.unit === "round", "The badge counts the rounds until he re-forms");
  const dustAt = vessel.turns;
  assert(until(() => hooks.hp(ool) > 0), "He re-forms");
  assert(vessel.turns - dustAt === 2 && hooks.hp(ool) === Math.floor(hooks.values(ool).maxHp * 0.5) && battle.phylactery.reforms === 1 && battle.log.some(entry => String(entry).includes("re-forms from his dust")), "Two rounds later he re-forms with half his health");

  // Breaking the phylactery: the cutscene, Mortal (-20% spell power), the dirge and a dimmed soul-fire.
  const spellPower = () => ool.effects.filter(effect => effect.type === "spell-power-percent").reduce((sum, effect) => sum + effect.amount, 0);
  hooks.directDamage(vessel, 1e8, false, "holy", player());
  assert(battle.phylactery.broken && ool.effects.some(effect => effect.mortal && effect.unremovable) && spellPower() === -20 && battle.log.some(entry => String(entry).includes("The phylactery shatters")), "The phylactery shatters: he is mortal, and his spells lose 20% power");
  assert(!intro.hidden && intro.dataset.boss === "ool-phylactery" && intro.querySelector(".bi-boss-before") && intro.querySelector(".bi-oo-soul"), "His soul screams out of the broken crystal in its own cutscene");
  $("boss-intro-skip").click();
  assert(Combat.musicTheme() === "ancient-lich-ool-mortal" && hooks.stageSnapshot([]).order?.id === "mortal" && hooks.stageLook(ool).soulDimmed === true, "The requiem breaks into a frantic dirge, and his soul-fire gutters");
  hooks.directDamage(ool, 1e8, false, "holy", player());
  hooks.checkOutcome();
  assert(hooks.hp(ool) <= 0 && ool.experienceAwarded && battle.phylactery.ended, "Mortal, he dies for good and awards his defeat");

  // Dust when the phylactery breaks: he stays dust.
  await begin();
  $("boss-intro-skip").click();
  battle = hooks.getBattle();
  ool = battle.actors.find(actor => actor.enemyDefinitionId === "ancient-lich-ool-unique-level-78");
  vessel = battle.actors.find(actor => actor.enemyDefinitionId === "phylactery-of-ool-level-78");
  hooks.directDamage(ool, 1e8, false, "holy", player());
  hooks.checkOutcome();
  assert(battle.phylactery.dustSince != null && !ool.experienceAwarded, "He is dust");
  hooks.directDamage(vessel, 1e8, false, "holy", player());
  hooks.checkOutcome();
  $("boss-intro-skip").click();
  assert(battle.phylactery.ended && ool.experienceAwarded && battle.log.some(entry => String(entry).includes("His dust never stirs again")), "Broken while he is dust, the phylactery takes him with it");
  for (let i = 0; i < 40 && Combat.active(); i++) step();
  assert(hooks.hp(ool) <= 0, "He never re-forms");
  Combat.restore(null);
  return checks;
}

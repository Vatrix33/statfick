async function runJiruanReworkChecks() {
  let checks = 0;
  const assert = (condition, message) => { if (!condition) throw Error(message); checks++; };
  Combat.restore(null);
  // Deterministic test hooks exist only in this fresh, invisible renderer.
  const source = await (await fetch("scripts/combat.js")).text();
  const marker = "  window.Combat = {";
  assert(source.includes(marker), "Combat test entry point exists");
  const script = document.createElement("script");
  script.textContent = source.replace(marker, "  window.__jiruanChecks = { actorById, cast, values, hp, mana, opponents, enemyAction, endTurn, isControlledTurn, currentActor, getBattle: () => battle, validateEnemy, addEnemyLoot, getRules: () => rules };\n" + marker);
  document.head.append(script);
  await waitForSourceFiles();
  const hooks = window.__jiruanChecks;
  if (window.BattleStage) window.BattleStage.enabled = () => false;
  if (window.GameSettings) window.GameSettings.enemyDelay = () => 1e9;

  // His sheet: Level 18, +30% HP and +50% Mana instead of the Raid Boss class, Summoner's protection.
  const rawBoss = await (await fetch("enemies/" + encodeURIComponent("Great Imp Ji'ruan.json"))).json();
  assert(rawBoss.level === 18 && rawBoss.maxHpPercent === 30 && rawBoss.maxManaPercent === 50, "Ji'ruan is Level 18 with 30% more HP and 50% more Mana");
  assert(!rawBoss.classLevels["raid-boss"] && rawBoss.classLevels.summoner >= 3, "No Raid Boss class; Summoner grants Overprotective");
  assert(rawBoss.aiProfile === "jiruan", "Ji'ruan has his own battle AI");
  assert(rawBoss.knownSpells.every(id => getSpell(id)?.element === "dark"), "His spell book holds only dark spells");
  // Port task 7 adds one Exotic to each of these bosses' tables (it rolls only while Limitless is on).
  assert(JSON.stringify(rawBoss.drops) === JSON.stringify([{ itemId: "masterwork-impshield", chance: 10 }, { itemId: "masterwork-fletcher", chance: 4 }]), "He drops the Impshield masterwork 10% of the time, and Fletcher 4%");
  let rejected = false;
  try { hooks.validateEnemy({ ...rawBoss, aiProfile: "not-a-profile" }); } catch (error) { rejected = error.message.includes("AI profile"); }
  assert(rejected, "Unknown AI profiles are rejected");

  const character = defaultCharacter();
  character.raceId = "human"; character.level = 20; character.stats.speed = 1; character.stats.vitality = 400; character.currentHp = 10000; character.currentMana = 10000;
  applyCharacter(normalizeCharacter(character));
  assert(await Combat.startEncounter(["Great Imp Ji'ruan"]), "Ji'ruan encounter starts");
  const intro = document.getElementById("boss-intro-cinematic");
  assert(rawBoss.introCinematic === "jiruan" && !intro.hidden && intro.querySelector("#boss-intro-title").textContent === "Great Imp Ji'ruan" && intro.querySelector(".bi-boss svg"), "His battle opens with his intro cutscene and title card");
  document.getElementById("boss-intro-skip").click();
  assert(intro.hidden, "The intro can be skipped");
  const battle = hooks.getBattle(), boss = battle.actors.find(actor => actor.team === "enemy" && !actor.summoned);
  const base = hooks.values({ ...structuredClone(boss), resourceMax: null, maxHpPercent: 0 }).maxHp;
  assert(boss.resourceMax.hp === Math.floor(base * 1.3), "Maximum HP is exactly 30% higher");
  const rapier = getItem(boss.character.equipment.primaryHand);
  assert(rapier?.forged?.type === "rapier" && rapier.rarity === "rare", "He wields a forged rare Rapier");

  // His first turn: Double Summon and two Imp Retinues fill the court with four Void Imps.
  for (let step = 0; step < 40 && hooks.currentActor()?.id !== boss.id; step++) {
    if (hooks.isControlledTurn()) hooks.endTurn();
    else hooks.enemyAction();
  }
  assert(hooks.currentActor()?.id === boss.id && boss.turns === 1, "Ji'ruan reaches his first turn");
  for (let step = 0; step < 12 && hooks.currentActor()?.id === boss.id && boss.turns === 1; step++) hooks.enemyAction();
  const imps = battle.actors.filter(actor => actor.summoned && actor.summonerId === boss.id && hooks.hp(actor) > 0);
  assert(imps.length === 4 && imps.every(actor => actor.summonDefinitionId === "void-imp-monster-level-12"), "His opening turn summons four Void Imps");
  assert(imps.every(actor => actor.resourceMax.hp === Combat.previewValues(actor.character).maxHp * 2), "Retinue Imps keep their doubled HP");
  assert(imps.every(actor => actor.effects.some(effect => effect.type === "outgoing-attack-damage-percent" && effect.amount === 30)), "Retinue Imps keep their 30% attack bonus");
  const playerActor = hooks.actorById("player");
  assert(!hooks.opponents(playerActor).includes(boss), "While his Imps live, Ji'ruan cannot be targeted");
  hooks.addEnemyLoot(["masterwork-impshield"]);
  const dropped = getItem(battle.lootItems.at(-1));
  assert(dropped?.forged?.masterwork === "impshield" && dropped.forged.level === 20 && dropped.category === "armor", "A dropped Impshield is forged at the player's level");
  imps.forEach(actor => { actor.character.currentHp = 0; });
  assert(hooks.opponents(playerActor).includes(boss), "With his retinue gone he is exposed");
  Combat.restore(null);

  // The Impshield: forged masterwork kite shield, Abyss main part, both effects work.
  const shieldId = ForgeRules.encode(ForgeRules.generateMasterwork({ key: "impshield", level: 20, random: () => 0.5 }));
  const shield = getItem(shieldId);
  assert(shield?.rarity === "masterwork" && shield.forged.type === "kite-shield" && shield.forged.parts[0].maker === "abyss" && shield.forged.parts[0].material === "mythilium", "The Impshield is an Abyss Mythilium kite shield");
  assert(shield.effects.some(effect => effect.kind === "incoming-race-damage-percent" && effect.amount === -15 && effect.raceIds.includes("impid") && effect.raceIds.includes("demon")), "It reduces damage from Impids and Demons by 15%");
  assert(shield.effects.some(effect => effect.kind === "absorb-element-spell-chance" && effect.element === "dark" && effect.chance === 5), "It may absorb dark spells");
  const holder = defaultCharacter();
  holder.raceId = "human"; holder.level = 20; holder.stats.speed = 1; holder.inventory = [shieldId]; holder.equipment = { ...defaultEquipment, offHand: shieldId }; holder.currentHp = 10000; holder.currentMana = 1;
  applyCharacter(normalizeCharacter(holder));
  assert(await Combat.startEncounter(["Great Imp Ji'ruan"]), "Shield fixture starts");
  const caster = hooks.getBattle().actors.find(actor => actor.team === "enemy" && !actor.summoned), wearer = hooks.actorById("player");
  const bolt = getSpell("dark-bolt-dark-damage"), hpBefore = hooks.hp(wearer), manaBefore = hooks.mana(wearer);
  const roll = Math.random; Math.random = () => 0;
  try { hooks.getBattle().current = caster.id; hooks.getBattle().ap = 5; assert(hooks.cast(caster, wearer, bolt), "Dark Bolt is cast at the shield bearer"); }
  finally { Math.random = roll; }
  assert(hooks.hp(wearer) === hpBefore && hooks.mana(wearer) > manaBefore, "An absorbed Dark Bolt deals no damage and restores the Mana it cost");
  Combat.restore(null);
  return { passed: true, checks };
}

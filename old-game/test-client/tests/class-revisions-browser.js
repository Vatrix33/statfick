async function runClassRevisionChecks() {
  const assert = (condition, message) => { if (!condition) throw new Error(message); };
  const archivedId = "consumable-archived-fire-bolt-fire-damage";
  const archived = getItem(archivedId);
  assert(archived?.name === "Archived: Fire Bolt" && archived.rarity === "rare" && archived.shopAvailable === false, "Archived spells are rare, named, non-shop consumables");
  assert(getClass("cartographer").levels[4].effects[0].combatOnly && getClass("courier").levels[2].effects[0].combatOnly, "Cartographer and Courier final skills are combat skills");
  assert(getClass("stormcaller").levels[2].effects[0].effect.maximum === 20, "Storm Rhythm supports 20 charges");
  assert(getClass("poisoner").levels[2].effects[0].effect.percentMaxHp === 3 && getClass("poisoner").levels[5].effects[0].percentMaxHp === 2, "Poisoner uses 3% plus 2% maximum-HP poison");
  assert(getClass("plaguebinder").levels[5].effects[0].onActivateEffects[0].percentMaxHp === 6, "Pestilent Cloud uses 6% maximum-HP poison");

  function fresh(classes = {}, known = []) {
    Combat.restore(null);
    const character = defaultCharacter(); character.raceId = "human"; character.level = 65;
    applyCharacter(normalizeCharacter(character));
    classLevels = classes; knownSpells = known; stats.speed = 150; stats.mind = 90;
    currentHp = calculateMaxHpWithArtifacts(currentStats());
    currentMana = calculateMaxManaWithEffects(currentStats());
    updateDerivedValues();
  }
  async function start(enemy = "Goblin Boss") {
    assert(await Combat.startEncounter([enemy], "Class revision test"), `${enemy} combat starts`);
    const state = Combat.serialize();
    state.current = "player"; state.ap = 5;
    state.targetId = state.actors.find(actor => actor.team === "enemy").id;
    Combat.restore(state);
  }
  function skill(key) {
    const button = document.querySelector(`[data-skill="${key}"]`);
    assert(button && !button.disabled, `${key} is usable`);
    button.click();
  }
  function ability(kind, id) {
    document.getElementById(kind === "spell" ? "combat-spell-button" : "combat-item-button").click();
    const button = document.querySelector(`[data-combat-ability="${id}"]`);
    assert(button && !button.disabled, `${kind} ${id} is usable`);
    button.click();
  }

  fresh({ archivist: 3 }, ["water-bolt-water-damage"]);
  await start();
  const victory = Combat.serialize();
  victory.active = false; victory.result = "Victory — test"; victory.lootResolved = false;
  victory.observedSpells = ["fire-bolt-fire-damage"]; victory.lootItems = [];
  const originalPrompt = window.prompt;
  window.prompt = () => "1";
  Combat.restore(victory);
  await new Promise(resolve => setTimeout(resolve, 0));
  const keepLoot = document.getElementById("combat-loot-keep");
  assert(keepLoot && !keepLoot.closest("[hidden]"), "Victory loot is available for Field Notes");
  keepLoot.click();
  window.prompt = originalPrompt;
  assert(inventory.includes(archivedId) && !knownSpells.includes("fire-bolt-fire-damage"), "Field Notes grants the Archived item without teaching the spell");
  Combat.restore(null);
  await start();
  const manaBeforeArchive = currentMana;
  const enemyHpBeforeArchive = Combat.serialize().actors.find(actor => actor.team === "enemy").character.currentHp;
  ability("item", archivedId);
  const archivedBattle = Combat.serialize();
  assert(!inventory.includes(archivedId) && currentMana === manaBeforeArchive, "Archived item is consumed without Mana cost");
  assert(archivedBattle.actors.find(actor => actor.team === "enemy").character.currentHp < enemyHpBeforeArchive, "Archived item casts its spell");

  fresh({ cartographer: 5, courier: 3 });
  await start();
  skill("cartographer:charted-course"); skill("courier:haste-the-route");
  const supportEffects = Combat.serialize().actors.find(actor => actor.id === "player").effects;
  assert(supportEffects.some(effect => effect.name === "Charted Course" && effect.target === "evasion") && supportEffects.some(effect => effect.name === "Haste the Route" && effect.target === "speed"), "Both revised combat skills apply their buffs");

  fresh({ "element-siphon": 5 }, ["water-bolt-water-damage"]);
  await start();
  const siphonBattle = Combat.serialize();
  siphonBattle.actors.find(actor => actor.id === "player").lastDamageElement = "fire";
  Combat.restore(siphonBattle);
  skill("element-siphon:siphon-charge");
  assert(Combat.serialize().actors.find(actor => actor.id === "player").nextSpellElement === "fire", "Siphon Charge stores the last incoming element");
  ability("spell", "water-bolt-water-damage");
  const shifted = Combat.serialize();
  assert(shifted.actors.find(actor => actor.team === "enemy").lastDamageElement === "fire" && !shifted.actors.find(actor => actor.id === "player").nextSpellElement, "The next spell uses the stored element exactly once");

  fresh({ plaguebinder: 6 });
  await start();
  skill("plaguebinder:pestilent-cloud");
  assert(Combat.serialize().actors.find(actor => actor.team === "enemy").effects.some(effect => effect.type === "class-poison" && effect.percentMaxHp === 6), "Pestilent Cloud applies percentage poison");
  const poisonTurn = Combat.serialize();
  const poisonedEnemy = poisonTurn.actors.find(actor => actor.team === "enemy");
  const startingEnemyHp = poisonedEnemy.character.currentHp;
  const expectedPoisonDamage = Math.max(1, Math.floor(poisonedEnemy.resourceMax.hp * 0.06));
  poisonedEnemy.counter = 999;
  poisonTurn.actors.find(actor => actor.id === "player").counter = 0;
  Combat.restore(poisonTurn);
  document.querySelector('[data-action="end"]').click();
  assert(Combat.serialize().actors.find(actor => actor.team === "enemy").character.currentHp === startingEnemyHp - expectedPoisonDamage, "Pestilent Cloud deals 6% maximum HP on the enemy turn");

  fresh({ poisoner: 6 }); equipment.primaryHand = "weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0";
  await start();
  const originalRandom = Math.random; Math.random = () => 0;
  document.querySelector('[data-action="attack"]').click();
  Math.random = originalRandom;
  const poisonTarget = Combat.serialize().actors.find(actor => actor.team === "enemy");
  assert(poisonTarget.effects.some(effect => effect.type === "class-poison" && effect.percentMaxHp === 5), `Poisoner weapon hit applies cumulative 5% maximum-HP poison: ${JSON.stringify(poisonTarget.effects)}; bonuses ${calculateClassEffects().poisonOnHitPercentMaxHp}`);

  fresh({ gambit: 3 }); equipment.primaryHand = "weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0";
  await start();
  const originalMissRandom = Math.random; Math.random = () => 0.999999;
  document.querySelector('[data-action="attack"]').click();
  assert(Combat.serialize().ap === 4, "Second Chance refunds 1 AP on the first missed Attack");
  document.querySelector('[data-action="attack"]').click();
  Math.random = originalMissRandom;
  assert(Combat.serialize().ap === 2, "Second Chance refunds only once per turn");

  fresh({ gambit: 5 }); equipment.primaryHand = "weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0";
  await start();
  const originalGambitRandom = Math.random; Math.random = () => 0;
  skill("gambit:double-down");
  Math.random = originalGambitRandom;
  assert(Combat.serialize().ap === 4, "Double Down refunds 2 AP after a successful 3-AP wager");

  fresh({ stormcaller: 3 }, ["lightning-bolt-lightning-damage"]);
  await start();
  const stormBattle = Combat.serialize();
  const player = stormBattle.actors.find(actor => actor.id === "player");
  player.stormCharges = 19; player.lastStormElement = "wind";
  Combat.restore(stormBattle);
  ability("spell", "lightning-bolt-lightning-damage");
  assert(Combat.serialize().actors.find(actor => actor.id === "player").stormCharges === 20, "Alternating spell reaches 20 Storm charges");
  Combat.restore(null);
  return true;
}

async function runPartyLootChecks() {
  const assert = (condition, message) => { if (!condition) throw new Error(message); };
  const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
  const swordId = "masterwork-stunted-sword", stuntedPiece = ForgeRules.encode(ForgeRules.generateMasterwork({ key: "stunted-sword", level: 30, random: () => 0.5 })), ordinaryId = "weapon-forged-longsword-30-abdam-hmque-abrei-s0";
  const sword = getItem(swordId), ordinary = getItem(ordinaryId);
  assert(sword?.rarity === "masterwork" && sword.handed === 1 && !sword.shopAvailable && sword.masterworkTemplate === "stunted-sword", "Stunted Sword is a drop-only Masterwork sword");
  assert(getItem(stuntedPiece)?.forged.type === "longsword" && getItem(stuntedPiece).forged.parts[0].material === "mythilium", "Stunted Sword is forged as a Mythilium longsword");
  assert(artifactEffectLabel(sword.effects[0]).includes("Goblin"), "The sword's race bonus renders correctly");
  const rareId = "weapon-forged-longsword-30-hflum-hfiro-rtoak-s1", epicId = "weapon-forged-longsword-30-rthel-abbla-rtrei-s0";
  assert(getItem(rareId)?.rarity === "rare" && getItem(epicId)?.rarity === "epic", "Loot fixtures load");
  const commonId = "weapon-forged-club-30-olque-olque-oldri-s0", legendaryId = "masterwork-golden-apex";
  const guaranteedId = "masterwork-quasar-jet", failedDropId = "weapon-forged-mace-30-elmoo-elque-rtoak-s0";
  async function lootTrial(playerClasses, companionClasses, randomValue, enemyClasses = {}, bladeClaimed = false) {
    const character = defaultCharacter();
    character.raceId = "human";
    character.classLevels = playerClasses;
    if (bladeClaimed) character.classProgress.claimedGuaranteedDrops = [guaranteedId];
    if (companionClasses) character.companions = [{ id: "companion-loot-test", name: "Loot Tester", character: { ...defaultCharacter(), raceId: "human", classLevels: companionClasses } }];
    applyCharacter(normalizeCharacter(character));
    assert(await Combat.startEncounter(["Goblin"]), "Loot encounter starts");
    const state = Combat.serialize(), enemy = state.actors.find(actor => actor.team === "enemy");
    enemy.character.currentHp = 0;
    enemy.character.classLevels = enemyClasses;
    enemy.character.inventory = [rareId, ordinaryId, epicId];
    enemy.dropTable = [
      { itemId: rareId, chance: 50, minimum: 2, maximum: 2 },
      ...[commonId, ordinaryId, epicId, legendaryId, swordId].map(itemId => ({ itemId, chance: 50 })),
      { itemId: guaranteedId, chance: 15, guaranteedUntilClaimed: true },
      { itemId: "armor-forged-garb-30-hmwoo-bblin-imste-s0", chance: 100 },
      { itemId: failedDropId, chance: 10 }
    ];
    state.current = null;
    const originalRandom = Math.random;
    Math.random = () => randomValue;
    try { Combat.restore(state); Combat.onSheetChange(); await wait(50); }
    finally { Math.random = originalRandom; }
    assert(!document.getElementById("combat-loot-modal").hidden, "Loot selection opens");
    const loot = Combat.serialize().lootItems;
    Combat.restore(Combat.serialize());
    assert(Combat.serialize().lootItems.length === loot.length, "Restoring generated loot cannot reroll or duplicate it again");
    await wait(0);
    document.getElementById("combat-loot-keep").click();
    assert(inventory.filter(id => id === rareId).length === loot.filter(id => id === rareId).length, "Keeping loot preserves its quantity");
    Combat.restore(null);
    return id => loot.filter(entry => entry === id || ForgeRules.masterworkTemplateKey(id) && getItem(entry)?.forged?.masterwork === ForgeRules.masterworkTemplateKey(id)).length;
  }
  const doubled = await lootTrial({ "heavy-knight": 1 }, { jester: 1 }, .19999);
  assert(doubled(rareId) === 5 && doubled(ordinaryId) === 3 && doubled(epicId) === 3 && doubled(commonId) === 2 && doubled(legendaryId) === 2 && doubled(swordId) === 2, "Split party classes double successful chance drops of every rarity, including unique items, once");
  assert(doubled(guaranteedId) === 1 && doubled("armor-forged-garb-30-hmwoo-bblin-imste-s0") === 1 && doubled(failedDropId) === 0, "Guaranteed loot is not duplicated and failed chance rolls cannot create loot");
  assert((await lootTrial({ "heavy-knight": 1, jester: 1 }, null, .1))(rareId) === 5, "Both classes on one party member qualify");
  assert((await lootTrial({ "heavy-knight": 1 }, { jester: 1 }, .2))(rareId) === 3, "The bonus stops at the 20% roll boundary");
  assert((await lootTrial({ "heavy-knight": 1 }, null, 0))(rareId) === 3, "One class alone does not double drops");
  assert((await lootTrial({}, null, 0, { "heavy-knight": 1, jester: 1 }))(rareId) === 3, "Enemy classes cannot activate the party loot bonus");
  assert((await lootTrial({ "heavy-knight": 1, jester: 1 }, null, .1, {}, true))(guaranteedId) === 2, "Quasar Jet can duplicate on its later chance-based drops");
  assert((await lootTrial({ "heavy-knight": 1, scavenger: 3 }, { jester: 1 }, .1))("consumable-camping-supplies") === 2, "Chance-based Salvage Sense drops also qualify");

  applyCharacter(normalizeCharacter({ ...defaultCharacter(), raceId: "human" }));
  const goblins = ["Goblin", "Goblin Archer", "Goblin Warrior", "Goblin Boss", "Goblin Raider", "Goblin Warlord", "Highland Goblin"];
  assert(await Combat.startEncounter(goblins), "All goblin variants load");
  assert(Combat.serialize().actors.filter(actor => actor.team === "enemy").every(actor => actor.dropTable.filter(drop => drop.itemId === swordId).length === 1 && actor.dropTable.find(drop => drop.itemId === swordId).chance === 5), "Every goblin has exactly one 5% Stunted Sword drop");
  Combat.restore(null);

  async function strikeDamage(weaponId, targetRace, startHp = 10000) {
    const character = defaultCharacter();
    character.raceId = "human";
    character.inventory = [weaponId];
    character.equipment.primaryHand = weaponId;
    applyCharacter(normalizeCharacter(character));
    assert(await Combat.startEncounter(["Goblin"]), "Weapon damage encounter starts");
    const state = Combat.serialize(), enemy = state.actors.find(actor => actor.team === "enemy");
    state.current = "player"; state.ap = 5;
    enemy.resourceMax.hp = 10000; enemy.character.currentHp = startHp;
    enemy.character.raceId = targetRace; enemy.baseArmor = 0;
    enemy.character.classLevels = {}; enemy.character.inventory = [];
    enemy.character.equipment = { ...defaultEquipment }; enemy.resistances = {};
    Combat.restore(state);
    document.getElementById("combat-view-toggle").click();
    const originalRandom = Math.random;
    Math.random = () => 0;
    try { document.querySelector('[data-action="attack"]').click(); }
    finally { Math.random = originalRandom; }
    const damage = startHp - Combat.serialize().actors.find(actor => actor.team === "enemy").character.currentHp;
    Combat.restore(null);
    return damage;
  }
  const healthy = await strikeDamage(stuntedPiece, "human"), woundedHuman = await strikeDamage(stuntedPiece, "human", 2500), woundedGoblin = await strikeDamage(stuntedPiece, "goblin", 2500);
  assert(healthy > 0 && await strikeDamage(stuntedPiece, "goblin") === healthy, "Healthy goblins take ordinary Stunted Sword damage");
  assert(Math.abs(woundedHuman - healthy * 1.25) <= 1 && Math.abs(woundedGoblin - healthy * 1.5) <= 1, "Stunted Sword deals 25% more to targets at or below 30% HP, 50% more to such goblins");
  assert(await strikeDamage(ordinaryId, "goblin") === await strikeDamage(ordinaryId, "human"), "Other swords deal goblins ordinary damage");
}

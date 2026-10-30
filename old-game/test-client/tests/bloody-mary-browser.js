async function runBloodyMaryChecks() {
  let checks = 0;
  const assert = (condition, message) => { if (!condition) throw Error(message); checks++; };
  const $ = id => document.getElementById(id), clone = value => structuredClone(value);
  const maryId = "weapon-bloody-mary-meele", heartId = "key-item-blood-crystal";
  const stockEntry = () => Shop.stockFor(WorldRules.settlementsForRegion("duskpine-forest")[0]).blacksmith.find(entry => entry.id === maryId);
  Combat.restore(null); WorldMap.setAmbientEnabled(false);
  const main = defaultCharacter(); main.raceId = "human"; main.worldState.regionId = "duskpine-forest";
  main.worldState.position = { x: 815, y: 505 }; main.inventory = Array(4).fill(heartId);
  main.currencies = ShopRules.walletFromValue(1000000);
  applyCharacter(normalizeCharacter(main));
  const mary = getItem(maryId), spear = { basePower: 14, primary: { ratio: 0.9 }, secondary: { ratio: 0.6 } };
  assert(mary?.rarity === "masterwork" && mary.basePower === spear.basePower && mary.primary.ratio === spear.primary.ratio && mary.secondary.ratio === spear.secondary.ratio, "Bloody Mary loads with Masterwork rarity and legendary spear power/scaling");
  Shop.open("blacksmith");
  const stock = Shop.stockFor(WorldRules.settlementsForRegion("duskpine-forest")[0]);
  assert(stockEntry()?.quantity === 1 && stock.blacksmith.length === 8, "Ravenwatch always stocks Bloody Mary inside its normal eight-item limit");
  assert(stock.blacksmith.filter(entry => getItem(entry.id).category === "weapon").length >= 2 && stock.blacksmith.filter(entry => getItem(entry.id).category === "armor").length >= 2, "The reserved offer preserves weapon and armor minimums");
  let button = document.querySelector(`[data-buy-item="${maryId}"]`), card = button.closest(".shop-card");
  assert(button.disabled && card.textContent.includes("5 × Blood Crystal") && card.textContent.includes("Masterwork"), "The shop shows the item price and blocks a purchase with only four hearts despite abundant money");
  button.disabled = false; button.click();
  assert(inventory.length === 4 && !inventory.includes(maryId) && ShopRules.walletValue(currencies) === 1000000 && stockEntry().quantity === 1, "The transaction itself rejects insufficient hearts without consuming anything");
  inventory.push(heartId, heartId); currencies = ShopRules.walletFromValue(0); Shop.render();
  button = document.querySelector(`[data-buy-item="${maryId}"]`);
  assert(!button.disabled, "Six hearts and zero coins are sufficient to purchase Bloody Mary");
  button.click();
  assert(inventory.filter(id => id === heartId).length === 1 && inventory.filter(id => id === maryId).length === 1 && ShopRules.walletValue(currencies) === 0 && stockEntry().quantity === 0, "Buying spends exactly five hearts, grants one spear, and never spends or requires money");
  assert(document.querySelector(`[data-buy-item="${maryId}"]`).disabled, "The sold-out offer cannot be purchased twice");
  const saved = normalizeCharacter(backupPayload().character); applyCharacter(saved); Shop.open("blacksmith");
  assert(stockEntry().quantity === 0 && inventory.filter(id => id === heartId).length === 1 && inventory.includes(maryId), "The item, payment and sold-out stock survive save restoration");
  worldState.minutes += 360; Shop.onTimeChange();
  assert(stockEntry().quantity === 1, "The regional offer replenishes at the normal six-hour stock refresh");
  const ordinaryEntry = Shop.stockFor(WorldRules.settlementsForRegion("duskpine-forest")[0]).blacksmith.find(entry => entry.id !== maryId);
  const ordinaryItem = getItem(ordinaryEntry.id);
  currencies = ShopRules.walletFromValue(ordinaryItem.priceSilver); Shop.render();
  document.querySelector(`[data-buy-item="${ordinaryItem.id}"]`).click();
  assert(inventory.includes(ordinaryItem.id) && ShopRules.walletValue(currencies) === 0 && inventory.filter(id => id === heartId).length === 1, "Ordinary wares still cost coins and never consume trade items");
  $("shop-sell-mode").click();
  const sellMary = document.querySelector(`[data-sell-item="${maryId}"]`);
  assert(sellMary && !sellMary.disabled, "Bloody Mary can still be sold like ordinary equipment");
  sellMary.click();
  assert(!inventory.includes(maryId) && ShopRules.walletValue(currencies) === ShopRules.salePrice(mary.priceSilver) && inventory.filter(id => id === heartId).length === 1, "Resale pays its normal half-value coin price without returning spent crystals");
  showItemDetails(mary);
  assert(itemDetailPanel.querySelector(".spell-detail-cost strong").textContent === "5 × Blood Crystal" && itemDetailPanel.textContent.includes("Crit Chance ×1.5"), "The item details show its true barter price and conditional effect");
  Shop.close();
  for (const region of Object.values(WorldRules.regions)) {
    worldState.regionId = region.id; WorldRules.setActiveRegion(region.id);
    for (const settlement of WorldRules.settlementsForRegion(region.id)) {
      if (region.id === "duskpine-forest" && settlement.id === "ravenwatch") continue;
      const other = Shop.stockFor(settlement);
      assert(!other.blacksmith.some(entry => entry.id === maryId) && !other.general.some(entry => entry.id === maryId), "Bloody Mary is absent from other shops: " + settlement.name);
    }
  }
  worldState.regionId = "alder-vale"; WorldRules.setActiveRegion("alder-vale"); worldState.position = { x: 420, y: 490 };
  const alder = WorldRules.settlements[0]; Shop.stockFor(alder);
  shopState.locations[alder.id].blacksmith[0] = { id: maryId, quantity: 1 };
  assert(!Shop.stockFor(alder).blacksmith.some(entry => entry.id === maryId), "Invalid old stock cannot leak the regional item into another town");

  const source = await (await fetch("scripts/combat.js")).text();
  const script = document.createElement("script");
  script.textContent = source.replace("  window.Combat = {", "  window.__maryChecks = { actorById, incomingDamageFactor, estimatedWeaponStrike, strike, hurt, weaponAttackValues, weaponCritChanceAgainst };\n  window.Combat = {");
  document.head.append(script); await waitForSourceFiles(); const h = window.__maryChecks;
  const fighter = defaultCharacter(); fighter.raceId = "human"; fighter.level = 100; fighter.stats.speed = 1000; fighter.stats.luck = 100; fighter.stats.vitality = 1000;
  fighter.currentHp = 100000; fighter.currentMana = 100000; fighter.inventory = [maryId];
  fighter.equipment.primaryHand = maryId; fighter.equipment.offHand = maryId;
  applyCharacter(normalizeCharacter(fighter));
  assert(await Combat.startEncounter(["Goblin", "Goblin", "Goblin"]), "Combat fixture starts");
  $("combat-sheet-button").click();
  const fixture = Combat.serialize(); fixture.current = "player"; fixture.ap = 5;
  fixture.actors.filter(actor => actor.team === "enemy").forEach(actor => {
    actor.character.classLevels = {}; actor.character.equipment = { ...defaultEquipment }; actor.character.inventory = [];
    actor.effects = []; actor.resistances = {}; actor.resourceMax = { hp: 10000, mana: 1000 }; actor.character.currentHp = 10000; actor.character.currentMana = 1000; actor.baseArmor = 0;
  });
  const reset = () => { applyCharacter(normalizeCharacter(fighter)); Combat.restore(clone(fixture)); return { player: h.actorById("player"), target: h.actorById("enemy-1") }; };
  let { player, target } = reset();
  const base = h.weaponAttackValues(player, mary).crit;
  assert(h.estimatedWeaponStrike(player, target, mary).critChance === base, "Ordinary enemies do not receive the conditional crit bonus");
  for (const classId of ["monster", "vampire", "vampire-lord"]) {
    target.character.classLevels = { [classId]: 1 };
    assert(h.estimatedWeaponStrike(player, target, mary).critChance === Math.min(40, Math.floor(base * 1.5)), "AI forecasts gain 50% relative crit chance against " + classId);
  }
  target.character.classLevels = { monster: 1, vampire: 1 };
  assert(h.weaponCritChanceAgainst(target, mary, 20) === 30 && h.weaponCritChanceAgainst(target, mary, 35) === 40 && h.weaponCritChanceAgainst(target, mary, 0) === 0, "The bonus does not stack for overlapping classes, respects the normal cap, and cannot restore disabled crits");
  const random = Math.random; let rolls = [0, (base + Math.min(40, Math.floor(base * 1.5))) / 200];
  try { Math.random = () => rolls.shift() ?? .9; h.strike(player, target, mary); } finally { Math.random = random; }
  assert(Combat.serialize().log.some(line => line.includes("(critical)")), "The actual attack roll uses the same target-specific crit bonus as AI forecasts");
  ({ player, target } = reset());
  assert(Math.abs(h.incomingDamageFactor(player, target, "spell", "blood") - .85) < 1e-10, "The equipped two-handed spear grants exactly 15% Blood spell reduction, counted once");
  assert(h.incomingDamageFactor(player, target, "spell", "fire") === 1 && h.incomingDamageFactor(player, target, "melee", "blood") === 1, "The ward does not reduce other elements or non-spell Blood attacks");
  const beforeHp = currentHp; h.hurt(player, 100, "blood", false, true, target, "spell"); const protectedDamage = beforeHp - currentHp;
  ({ player, target } = reset()); equipment = { ...defaultEquipment };
  const unprotectedHp = currentHp; h.hurt(player, 100, "blood", false, true, target, "spell"); const normalDamage = unprotectedHp - currentHp;
  assert(protectedDamage === Math.floor(normalDamage * .85), `Actual Blood spell damage is reduced by 15% while equipped (${protectedDamage} vs ${normalDamage})`);
  assert(h.incomingDamageFactor(player, target, "spell", "blood") === 1, "The ward ends immediately when the spear is unequipped");
  Combat.restore(null);
  return { passed: true, checks };
}

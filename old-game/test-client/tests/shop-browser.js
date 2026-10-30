async function runShopChecks() {
  const assert = (value, message) => { if (!value) throw new Error(message); };
  Combat.restore(null);
  applyCharacter(normalizeCharacter({ ...defaultCharacter(), editingEnabled: false }));
  assert(ShopRules.walletValue(currencies) === 500, "New characters start with 5 Gold");
  assert(document.getElementById("item-add-button").hidden && document.getElementById("spell-add-button").hidden, "Free catalogue buttons are hidden outside Dev-mode");
  assert(items.filter(item => !item.archivedSpellId).length === 219 && items.filter(item => item.archivedSpellId).length === spells.length && items.every(item => Number.isInteger(item.priceSilver) && item.priceSilver >= 0), "Every catalogue item and generated Archived spell has a price");
  const requestedArmorCounts = {
    uncommon: { clothing: 4, light: 4, heavy: 3, shield: 3 },
    rare: { clothing: 3, light: 3, heavy: 3, shield: 2 },
    epic: { clothing: 2, light: 3, heavy: 2, shield: 2 },
    legendary: { clothing: 2, light: 2, heavy: 1, shield: 2 },
  };
  for (const [rarity, typeCounts] of Object.entries(requestedArmorCounts)) for (const [type, expected] of Object.entries(typeCounts)) {
    assert(itemCatalog.armor.filter(item => item.rarity === rarity && item.type === type).length === expected, `${rarity} armor has ${expected} ${type} variant${expected === 1 ? "" : "s"}`);
  }
  for (const ids of [
    ["armor-forged-robe-30-bbwoo-ellin-elwoo-s0", "armor-forged-robe-30-elsth-hmslk-imste-s0", "armor-forged-robe-30-elgth-immag-bbiro-s0", "armor-forged-robe-30-hmvoi-hmdra-hmwoo-s0", "armor-forged-robe-30-bbwst-imraw-ellin-s0"],
    ["armor-forged-leather-armor-30-elraw-bbraw-hfraw-s0", "armor-forged-leather-armor-30-absca-elboi-elsca-s0", "armor-forged-brigandine-30-elwyv-hflin-elbla-s0", "armor-forged-leather-armor-30-hfhel-abwyv-hfsha-s0", "armor-forged-scale-armor-30-hfabo-hmgvt-hmbor-s0"],
    ["armor-forged-full-plate-30-rtiro-dwraw-rtste-s0", "armor-forged-full-plate-30-hfdam-hmsca-hmste-s0", "armor-forged-full-plate-30-rtmoo-rtmag-rtdws-s0", "armor-forged-full-plate-30-abvul-rtmag-dwste-s0", "armor-forged-full-plate-30-rtsta-rtwoo-hmdrk-s0"],
    ["armor-forged-buckler-30-rtoak-rtste-hfoak-s0", "armor-forged-kite-shield-30-rtbir-hfiro-ctque-s0", "armor-forged-kite-shield-30-ctmoo-hmste-ctoak-s0", "armor-forged-kite-shield-30-cthel-ctdam-hmwyv-s0", "armor-forged-tower-shield-30-rtabo-rtiro-hfman-s0"],
  ]) {
    const progression = ids.map(getItem);
    assert(progression.every(Boolean) && progression.every((item, index) => index === 0 || item.armorValue > progression[index - 1].armorValue) && progression.every((item, index) => index === 0 || item.priceSilver > progression[index - 1].priceSilver), `${progression.map(item => item?.name || "missing").join(" → ")} has rising protection and price`);
  }
  for (const [rarity, expected] of Object.entries({ uncommon: 14, rare: 9, epic: 6, legendary: 3 })) {
    assert(itemCatalog.artifacts.filter(item => item.rarity === rarity).length === expected, `Artifact catalogue contains ${expected} ${rarity} artifacts after the new additions`);
  }
  assert(new Set(itemCatalog.artifacts.flatMap(item => item.effects.map(effect => effect.kind))).size >= 16, "Artifacts use a broad range of at least sixteen mechanical effect types");
  const savedArtifactEquipment = equipment;
  const testSword = getItem("weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0"), testBow = getItem("weapon-forged-shortbow-30-bboak-bbste-elelv-s0");
  equipment = { ...defaultEquipment, artifact1: "artifact-duelists-coin-artifact" };
  let artifactTotals = calculateArtifactEffects();
  assert(artifactWeaponModifier(testSword, artifactTotals, "weapon-hit-flat") === 8 && artifactWeaponModifier(testBow, artifactTotals, "weapon-hit-flat") === 0, "Melee artifact effects do not affect ranged weapons");
  const waterSpell = getSpell("water-bolt-water-damage"), fireSpell = getSpell("fire-bolt-fire-damage");
  equipment = { ...defaultEquipment };
  const baseWaterCost = calculateSpellManaCost(waterSpell), baseWaterPower = calculateSpellValue(waterSpell), baseFireCost = calculateSpellManaCost(fireSpell);
  equipment.artifact1 = "artifact-tidal-pearl-artifact";
  assert(calculateSpellManaCost(waterSpell) < baseWaterCost && calculateSpellValue(waterSpell) > baseWaterPower && calculateSpellManaCost(fireSpell) === baseFireCost, "Elemental artifact modifiers affect only their matching spells");
  equipment = { ...defaultEquipment };
  const baseMaximumMana = calculateMaxManaWithEffects(currentStats());
  equipment.artifact1 = "artifact-arcane-reservoir-artifact";
  assert(calculateMaxManaWithEffects(currentStats()) > baseMaximumMana, "Maximum-Mana artifact effects are applied");
  equipment.artifact1 = "artifact-deadeye-prism-artifact";
  artifactTotals = calculateArtifactEffects();
  assert(artifactWeaponModifier(testBow, artifactTotals, "weapon-crit-flat") === 5 && artifactWeaponModifier(testSword, artifactTotals, "weapon-crit-flat") === 0, "Weapon-type critical chance modifiers are targeted correctly");
  equipment = savedArtifactEquipment;
  for (const [upgradedId, originalId] of [
    ["weapon-forged-longsword-30-abdam-hmque-abrei-s0", "weapon-forged-longsword-30-dwiro-hfoak-elque-s0"],
    ["weapon-forged-dagger-30-hmdam-hfste-dwoak-s0", "weapon-forged-dagger-30-hfiro-hmque-elque-s1"],
    ["weapon-forged-greatsword-30-dwdws-hfiro-hfrei-s0", "weapon-forged-greatsword-30-hfste-hfque-hfoak-s0"],
    ["weapon-forged-mace-30-abdam-hfrei-hfoak-s0", "weapon-forged-mace-30-hmste-rtste-hmque-s0"],
    ["weapon-forged-greataxe-30-hfdam-bbiro-elque-s0", "weapon-forged-greataxe-30-rtste-rtste-hmque-s0"],
    ["weapon-forged-longbow-30-hmbir-hfdam-rtsth-s0", "weapon-forged-shortbow-30-hmque-hmste-hmelv-s0"],
    ["weapon-forged-warbow-30-elbir-abdam-rtsth-s0", "weapon-forged-longbow-30-hfoak-hfste-elelv-s0"]
  ]) {
    const upgraded = getItem(upgradedId), original = getItem(originalId);
    const scalingUpgrade = original.secondary
      ? Math.abs(upgraded.primary.multiplier - original.primary.multiplier - 0.1) < 1e-9 && upgraded.secondary?.stat === original.secondary.stat && Math.abs(upgraded.secondary.multiplier - original.secondary.multiplier - 0.1) < 1e-9
      : Math.abs(upgraded.primary.multiplier - original.primary.multiplier - 0.2) < 1e-9 && upgraded.secondary === null;
    assert(upgraded && original && upgraded.rarity === "uncommon" && upgraded.handed === original.handed && upgraded.type === original.type && upgraded.basePower === original.basePower + 2 && upgraded.primary.stat === original.primary.stat && scalingUpgrade, upgradedId + " distributes its improved scaling according to its predecessor");
  }
  for (const weaponKind of ["sword", "dagger", "greatsword", "mace", "axe"]) {
    const damascus = getItem(`weapon-damascus-${weaponKind}-meele`);
    const dwarven = getItem(`weapon-dwarven-steel-${weaponKind}-meele`);
    assert(dwarven?.rarity === "uncommon" && dwarven.handed === damascus.handed && dwarven.type === damascus.type && dwarven.basePower === damascus.basePower + 1 && dwarven.primary.stat === damascus.primary.stat && Math.abs(dwarven.primary.multiplier - damascus.primary.multiplier + 0.1) < 1e-9 && JSON.stringify(dwarven.secondary) === JSON.stringify(damascus.secondary) && dwarven.priceSilver > damascus.priceSilver, `Dwarven Steel ${weaponKind} trades primary scaling for base power`);
  }
  for (const [rareId, uncommonId, commonId] of [
    ["weapon-forged-longsword-30-hflum-hfiro-rtoak-s1", "weapon-forged-longsword-30-abdam-hmque-abrei-s0", "weapon-forged-longsword-30-dwiro-hfoak-elque-s0"],
    ["weapon-forged-dagger-30-abbla-hfste-rtoak-s0", "weapon-forged-dagger-30-hmdam-hfste-dwoak-s0", "weapon-forged-dagger-30-hfiro-hmque-elque-s1"],
    ["weapon-forged-greatsword-30-hmbla-hmdam-hfrei-s0", "weapon-forged-greatsword-30-dwdws-hfiro-hfrei-s0", "weapon-forged-greatsword-30-hfste-hfque-hfoak-s0"],
    ["weapon-forged-mace-30-elmoo-elque-rtoak-s0", "weapon-forged-mace-30-abdam-hfrei-hfoak-s0", "weapon-forged-mace-30-hmste-rtste-hmque-s0"],
    ["weapon-forged-greataxe-30-dwbla-hfiro-hmbir-s0", "weapon-forged-greataxe-30-hfdam-bbiro-elque-s0", "weapon-forged-greataxe-30-rtste-rtste-hmque-s0"],
    ["weapon-forged-compoundbow-30-rtpin-bbman-elmag-s0", "weapon-forged-longbow-30-hmbir-hfdam-rtsth-s0", "weapon-forged-shortbow-30-hmque-hmste-hmelv-s0"],
    ["weapon-forged-crossbow-30-elman-abrei-rtsth-s0", "weapon-forged-warbow-30-elbir-abdam-rtsth-s0", "weapon-forged-longbow-30-hfoak-hfste-elelv-s0"]
  ]) {
    const rare = getItem(rareId), uncommon = getItem(uncommonId), common = getItem(commonId);
    const secondaryStepMatches = common.secondary
      ? rare.secondary?.stat === uncommon.secondary?.stat && Math.abs((rare.secondary.multiplier - uncommon.secondary.multiplier) - (uncommon.secondary.multiplier - common.secondary.multiplier)) < 1e-9
      : rare.secondary === null && uncommon.secondary === null;
    assert(rare && uncommon && common && rare.rarity === "rare" && rare.handed === uncommon.handed && rare.type === uncommon.type && rare.basePower - uncommon.basePower === uncommon.basePower - common.basePower && rare.primary.stat === uncommon.primary.stat && Math.abs((rare.primary.multiplier - uncommon.primary.multiplier) - (uncommon.primary.multiplier - common.primary.multiplier)) < 1e-9 && secondaryStepMatches, rareId + " repeats its Common-to-Uncommon upgrade strength");
  }
  for (const weaponKind of ["sword", "dagger", "greatsword", "mace", "axe"]) {
    const standard = getItem(`weapon-black-steel-${weaponKind}-meele`);
    const manasteel = getItem(`weapon-manasteel-${weaponKind}-meele`);
    const lumium = getItem(`weapon-lumium-${weaponKind}-meele`);
    const lumiumScaling = standard.secondary
      ? lumium.secondary?.stat === standard.secondary.stat && Math.abs(lumium.secondary.multiplier - standard.secondary.multiplier - 0.1) < 1e-9 && lumium.primary.multiplier === standard.primary.multiplier
      : lumium.secondary === null && Math.abs(lumium.primary.multiplier - standard.primary.multiplier - 0.1) < 1e-9;
    assert(manasteel?.rarity === "rare" && manasteel.basePower === standard.basePower - 3 && manasteel.primary.stat === standard.primary.stat && manasteel.primary.multiplier === standard.primary.multiplier && JSON.stringify(manasteel.secondary) === JSON.stringify(standard.secondary) && manasteel.effects?.length === 1 && manasteel.effects[0].kind === "spell-cost-percent" && manasteel.effects[0].amount === 10 && manasteel.priceSilver > standard.priceSilver, `Manasteel ${weaponKind} trades substantial base power for spell efficiency`);
    assert(lumium?.rarity === "rare" && lumium.basePower === standard.basePower - 1 && lumium.primary.stat === standard.primary.stat && lumiumScaling && lumium.priceSilver > standard.priceSilver, `Lumium ${weaponKind} trades base power for secondary scaling`);
  }
  const spellCostTest = getSpell("spell-bolt-basic-damage"), savedEquipmentForManaWeapons = equipment;
  equipment = { ...defaultEquipment };
  const normalSpellCost = calculateSpellManaCost(spellCostTest);
  equipment.primaryHand = "weapon-forged-longsword-30-hmlum-hmque-rtoak-s0";
  const oneManasteelCost = calculateSpellManaCost(spellCostTest);
  equipment.offHand = "weapon-forged-dagger-30-hmlum-hmbir-elgma-s0";
  const twoManasteelCost = calculateSpellManaCost(spellCostTest);
  equipment = { ...defaultEquipment, primaryHand: "weapon-forged-greatsword-30-hmlum-dwdam-hmque-s0", offHand: "weapon-forged-greatsword-30-hmlum-dwdam-hmque-s0" };
  const twoHandedManasteelCost = calculateSpellManaCost(spellCostTest);
  equipment = savedEquipmentForManaWeapons;
  assert(oneManasteelCost === Math.floor(normalSpellCost * 0.9) && twoManasteelCost === Math.floor(normalSpellCost * 0.8) && twoHandedManasteelCost === oneManasteelCost, "Manasteel reductions apply per physical weapon without double-counting a two-handed weapon");
  const casterWeaponCounts = {};
  for (const item of items.filter(entry => ["magic-wand", "spell-staff"].includes(entry.type))) {
    casterWeaponCounts[`${item.type}:${item.rarity}`] = (casterWeaponCounts[`${item.type}:${item.rarity}`] || 0) + 1;
    const powerEffect = item.effects.find(effect => effect.kind === "spell-power-percent");
    const costEffect = item.effects.find(effect => effect.kind === "spell-cost-percent");
    const isWand = item.type === "magic-wand";
    assert(item.handed === (isWand ? 1 : 2), `${item.name} uses its intended equipment slots`);
    assert(powerEffect && costEffect && (isWand ? powerEffect.amount > 0 && costEffect.amount < 0 : powerEffect.amount < 0 && costEffect.amount > 0), `${item.name} applies the correct spell power and Mana cost tradeoff`);
    assert(item.basePower <= (isWand ? 6 : 7) && item.primary.multiplier <= 0.4, `${item.name} remains a weak melee weapon`);
  }
  for (const type of ["magic-wand", "spell-staff"]) for (const [rarity, expected] of Object.entries({ common: 3, uncommon: 3, rare: 3, epic: 2, legendary: 1 })) {
    assert(casterWeaponCounts[`${type}:${rarity}`] === expected, `${type} has ${expected} ${rarity} variant${expected === 1 ? "" : "s"}`);
  }
  const savedCasterEquipment = equipment;
  equipment = { ...defaultEquipment };
  const casterBasePower = calculateSpellValue(spellCostTest), casterBaseCost = calculateSpellManaCost(spellCostTest);
  equipment.primaryHand = "weapon-forged-wand-30-eljad1-hfsta-imgry";
  const wandSpellPower = calculateSpellValue(spellCostTest), wandSpellCost = calculateSpellManaCost(spellCostTest);
  equipment = { ...defaultEquipment, primaryHand: "weapon-forged-staff-30-ablap2-dwsta-imblk", offHand: "weapon-forged-staff-30-ablap2-dwsta-imblk" };
  const staffSpellPower = calculateSpellValue(spellCostTest), staffSpellCost = calculateSpellManaCost(spellCostTest);
  equipment = savedCasterEquipment;
  assert(wandSpellPower > casterBasePower && wandSpellCost > casterBaseCost, "Magic Wands increase both spell power and Mana cost");
  assert(staffSpellPower < casterBasePower && staffSpellCost < casterBaseCost, "Spell Staffs reduce both spell power and Mana cost without double-counting their off-hand occupancy");
  for (const [epicId, rareId, uncommonId] of [
    ["weapon-forged-longsword-30-rthel-abbla-rtrei-s0", "weapon-forged-longsword-30-hflum-hfiro-rtoak-s1", "weapon-forged-longsword-30-abdam-hmque-abrei-s0"],
    ["weapon-forged-dagger-30-hfhel-hmbir-dwbir-s0", "weapon-forged-dagger-30-abbla-hfste-rtoak-s0", "weapon-forged-dagger-30-hmdam-hfste-dwoak-s0"],
    ["weapon-forged-greatsword-30-ababy-bbque-hfegw-s0", "weapon-forged-greatsword-30-hmbla-hmdam-hfrei-s0", "weapon-forged-greatsword-30-dwdws-hfiro-hfrei-s0"],
    ["weapon-forged-mace-30-elvul-dwmoo-abrei-s0", "weapon-forged-mace-30-elmoo-elque-rtoak-s0", "weapon-forged-mace-30-abdam-hfrei-hfoak-s0"],
    ["weapon-forged-greataxe-30-hfhel-hfvul-hfoak-s0", "weapon-forged-greataxe-30-dwbla-hfiro-hmbir-s0", "weapon-forged-greataxe-30-hfdam-bbiro-elque-s0"],
    ["weapon-forged-longbow-30-hmegw-dwvul-hmdra-s0", "weapon-forged-compoundbow-30-rtpin-bbman-elmag-s0", "weapon-forged-longbow-30-hmbir-hfdam-rtsth-s0"],
    ["weapon-forged-ballistabow-30-rtegw-elgma-rtwoo-s0", "weapon-forged-crossbow-30-elman-abrei-rtsth-s0", "weapon-forged-warbow-30-elbir-abdam-rtsth-s0"]
  ]) {
    const epic = getItem(epicId), rare = getItem(rareId), uncommon = getItem(uncommonId);
    const secondaryStepMatches = rare.secondary
      ? epic.secondary?.stat === rare.secondary.stat && Math.abs((epic.secondary.multiplier - rare.secondary.multiplier) - (rare.secondary.multiplier - uncommon.secondary.multiplier)) < 1e-9
      : epic.secondary === null;
    assert(epic && rare && uncommon && epic.rarity === "epic" && epic.handed === rare.handed && epic.type === rare.type && epic.basePower - rare.basePower === rare.basePower - uncommon.basePower && epic.primary.stat === rare.primary.stat && Math.abs((epic.primary.multiplier - rare.primary.multiplier) - (rare.primary.multiplier - uncommon.primary.multiplier) - 0.1) < 1e-9 && secondaryStepMatches, epicId + " repeats the prior upgrade with an additional 0.1 primary scaling");
  }
  for (const weaponKind of ["sword", "dagger", "greatsword", "mace", "axe"]) {
    const standard = getItem(`weapon-vulcan-alloy-${weaponKind}-meele`);
    const hellsteel = getItem(`weapon-hellsteel-${weaponKind}-meele`);
    const voidmetal = getItem(`weapon-voidmetal-${weaponKind}-meele`);
    const hellsteelBaseLoss = weaponKind === "axe" ? 2 : 1;
    const hellsteelSecondaryMatches = standard.secondary ? hellsteel.secondary?.stat === standard.secondary.stat && Math.abs(hellsteel.secondary.multiplier - standard.secondary.multiplier + 0.1) < 1e-9 : hellsteel.secondary === null;
    const voidmetalSecondaryMatches = standard.secondary ? voidmetal.secondary?.stat === standard.secondary.stat && Math.abs(voidmetal.secondary.multiplier - standard.secondary.multiplier - 0.1) < 1e-9 : voidmetal.secondary?.stat === "luck" && voidmetal.secondary.multiplier === 0.1;
    assert(hellsteel?.rarity === "epic" && hellsteel.basePower === standard.basePower - hellsteelBaseLoss && hellsteel.primary.stat === standard.primary.stat && Math.abs(hellsteel.primary.multiplier - standard.primary.multiplier - 0.1) < 1e-9 && hellsteelSecondaryMatches && hellsteel.priceSilver > standard.priceSilver, `Hellsteel ${weaponKind} follows its alternate Epic progression`);
    assert(voidmetal?.rarity === "epic" && voidmetal.basePower === standard.basePower - 1 && voidmetal.primary.stat === standard.primary.stat && Math.abs(voidmetal.primary.multiplier - standard.primary.multiplier + 0.1) < 1e-9 && voidmetalSecondaryMatches && voidmetal.priceSilver > standard.priceSilver, `Voidmetal ${weaponKind} follows its alternate Epic progression`);
  }
  for (const [legendaryId, epicId, rareId] of [
    ["weapon-forged-longsword-30-hmnul-dwrei-hfman-s0", "weapon-forged-longsword-30-rthel-abbla-rtrei-s0", "weapon-forged-longsword-30-hflum-hfiro-rtoak-s1"],
    ["weapon-forged-dagger-30-hmnul-bbman-elque-s0", "weapon-forged-dagger-30-hfhel-hmbir-dwbir-s0", "weapon-forged-dagger-30-abbla-hfste-rtoak-s0"],
    ["weapon-forged-greatsword-30-rtsta-bbdam-hmbir-s0", "weapon-forged-greatsword-30-ababy-bbque-hfegw-s0", "weapon-forged-greatsword-30-hmbla-hmdam-hfrei-s0"],
    ["weapon-forged-mace-30-dwnul-bboak-hmque-s0", "weapon-forged-mace-30-elvul-dwmoo-abrei-s0", "weapon-forged-mace-30-elmoo-elque-rtoak-s0"],
    ["weapon-forged-greataxe-30-rtsta-rtsta-abman-s0", "weapon-forged-greataxe-30-hfhel-hfvul-hfoak-s0", "weapon-forged-greataxe-30-dwbla-hfiro-hmbir-s0"],
    ["weapon-forged-compoundbow-30-hflif-hfbla-rtwoo-s0", "weapon-forged-longbow-30-hmegw-dwvul-hmdra-s0", "weapon-forged-compoundbow-30-rtpin-bbman-elmag-s0"],
    ["weapon-forged-warbow-30-bbabo-hmque-bbelv-s0", "weapon-forged-ballistabow-30-rtegw-elgma-rtwoo-s0", "weapon-forged-crossbow-30-elman-abrei-rtsth-s0"]
  ]) {
    const legendary = getItem(legendaryId), epic = getItem(epicId), rare = getItem(rareId);
    const secondaryStepMatches = epic.secondary
      ? legendary.secondary?.stat === epic.secondary.stat && Math.abs((legendary.secondary.multiplier - epic.secondary.multiplier) - (epic.secondary.multiplier - rare.secondary.multiplier)) < 1e-9
      : legendary.secondary === null;
    assert(legendary && epic && rare && legendary.rarity === "legendary" && legendary.handed === epic.handed && legendary.type === epic.type && legendary.basePower - epic.basePower === epic.basePower - rare.basePower && legendary.primary.stat === epic.primary.stat && Math.abs((legendary.primary.multiplier - epic.primary.multiplier) - (epic.primary.multiplier - rare.primary.multiplier)) < 1e-9 && secondaryStepMatches, legendaryId + " continues the Epic-tier upgrade step");
  }
  assert(items.every(item => item.inventoryAction !== "learn-random-spell" && !item.id.startsWith("consumable-spell-tome-")), "All spell-tome items were removed");
  const keyItemIds = ["key-item-gem-of-false-dreams", "key-item-vial-of-dragonblood", "key-item-blood-crystal"];
  assert(keyItemIds.every(id => getItem(id)?.category === "key-item" && getItem(id).shopAvailable === false && getItem(id).playerSellable === false), "Key items remain unavailable for buying and selling");
  assert(spells.length === 74 && knownSpells.length === 0, "The expanded spell catalogue loads with no spells initially learned");
  assert(ShopRules.salePrice(151) === 75, "Selling rounds half-price down to whole Silver");

  const castle = WorldRules.settlements.find(entry => entry.id === "alderkeep");
  worldState.position = WorldRules.point(castle); worldState.destination = null; WorldMap.render();
  assert(!document.getElementById("map-blacksmith").disabled && !document.getElementById("map-item-shop").disabled && !document.getElementById("map-spell-shop").disabled, "All three shops are available in a settlement");
  assert(!document.getElementById("map-rest").hidden && document.getElementById("map-camp").hidden, "Only currently usable rest and camp actions are shown");

  const beforeRest = worldState.minutes;
  document.getElementById("map-rest").click();
  assert(worldState.minutes === beforeRest + 600 && ShopRules.walletValue(currencies) === 475, "Town rest advances 10 hours and charges the castle lodging price");
  worldState.minutes = 480; currencies = { silver: 0, gold: 5, platinum: 0 }; shopState = { version: 3, refreshWindow: -1, locations: {} };
  Shop.open("blacksmith");
  let castleStock = Shop.stockFor(castle);
  assert(castleStock.blacksmith.length === 8 && new Set(castleStock.blacksmith.map(entry => entry.id)).size === 8, "Castle blacksmiths stock 8 different items");
  assert(castleStock.blacksmith.every(entry => ["weapon", "armor"].includes(getItem(entry.id)?.category)), "Blacksmith stock only contains weapons and armor");
  assert(castleStock.blacksmith.filter(entry => getItem(entry.id)?.category === "weapon").length >= 2 && castleStock.blacksmith.filter(entry => getItem(entry.id)?.category === "armor").length >= 2, "Blacksmiths guarantee at least 2 weapons and 2 armor pieces");
  assert(castleStock.general.length === 8 && castleStock.general.every(entry => !["weapon", "armor"].includes(getItem(entry.id)?.category)), "Castle General Stores stock 8 non-equipment items");
  assert(castleStock.general.every(entry => getItem(entry.id)?.category !== "artifact"), "Artifacts do not appear in General Store stock");
  assert([...castleStock.blacksmith, ...castleStock.general].every(entry => !keyItemIds.includes(entry.id)), "Key items never appear in shop stock");
  assert(castleStock.spells.length === 10, "Castle spell shops stock ten spells");
  const artifactId = items.find(item => item.category === "artifact").id;
  shopState = { version: 3, refreshWindow: ShopRules.refreshWindow(worldState.minutes), locations: { alderkeep: { blacksmith: [], general: [{ id: artifactId, quantity: 1 }], spells: [] } } };
  castleStock = Shop.stockFor(castle);
  assert(castleStock.general.every(entry => getItem(entry.id)?.category !== "artifact"), "Old saved shop stock rerolls without artifacts");
  shopState = { version: 4, refreshWindow: ShopRules.refreshWindow(worldState.minutes), locations: { alderkeep: { blacksmith: [], general: [{ id: artifactId, quantity: 1 }], spells: [] } } };
  castleStock = Shop.stockFor(castle);
  assert(castleStock.general.every(entry => getItem(entry.id)?.category !== "artifact"), "Saved shop stock containing an artifact is sanitized");
  const artifact = getItem(artifactId);
  showItemDetails(artifact);
  assert(document.getElementById("item-detail-panel").textContent.includes(ShopRules.formatPrice(artifact.priceSilver)) && document.getElementById("item-detail-panel").textContent.includes(ShopRules.formatPrice(ShopRules.salePrice(artifact.priceSilver))), "Artifact details show both shop and sell prices");
  inventory.push(artifactId);
  Shop.open("general"); document.getElementById("shop-sell-mode").click();
  const artifactSaleButton = document.querySelector('[data-sell-item="' + artifactId + '"]');
  assert(artifactSaleButton, "Artifacts can be sold to the General Store");
  const beforeArtifactSale = ShopRules.walletValue(currencies);
  artifactSaleButton.click();
  assert(!inventory.includes(artifactId) && ShopRules.walletValue(currencies) === beforeArtifactSale + ShopRules.salePrice(artifact.priceSilver), "Selling an artifact pays half its listed price");
  Shop.close();
  currencies = { silver: 0, gold: 5, platinum: 0 };
  Shop.open("blacksmith");

  let swordStockEntry = castleStock.blacksmith.find(entry => entry.id === "weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0");
  if (!swordStockEntry) {
    castleStock.blacksmith[0] = { id: "weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0", quantity: 1 };
    swordStockEntry = castleStock.blacksmith[0];
  } else swordStockEntry.quantity = 1;
  Shop.render();
  document.querySelector('[data-buy-item="weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0"]').click();
  assert(inventory.includes("weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0") && ShopRules.walletValue(currencies) === 350 && swordStockEntry.quantity === 0, "Buying from the blacksmith deducts exact coin and consumes stock");
  equipment.primaryHand = "weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0"; inventory.push("weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0");
  document.getElementById("shop-sell-mode").click();
  document.querySelector('[data-sell-item="weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0"]').click();
  assert(inventory.filter(id => id === "weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0").length === 1 && equipment.primaryHand === "weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0" && ShopRules.walletValue(currencies) === 425, "Selling pays half price while protecting the equipped copy");

  if (spells.length) {
  const shopSpell = spells[0];
  currencies = ShopRules.walletFromValue(shopSpell.priceSilver + 500);
  const stockedSpellIndex = castleStock.spells.indexOf(shopSpell.id);
  if (stockedSpellIndex < 0) castleStock.spells[0] = shopSpell.id;
  Shop.open("spells"); Shop.render();
  document.querySelector('[data-buy-spell="' + shopSpell.id + '"]').click();
  assert(knownSpells.includes(shopSpell.id) && ShopRules.walletValue(currencies) === 500, "Buying a spell deducts coin and teaches it");
  Shop.close();
  document.getElementById("spellbook-open-button").click();
  const originalConfirm = window.confirm;
  window.confirm = () => false;
  document.querySelector('[data-unlearn-spell="' + shopSpell.id + '"]').click();
  assert(knownSpells.includes(shopSpell.id), "Declining unlearn confirmation keeps the spell");
  window.confirm = () => true;
  document.querySelector('[data-unlearn-spell="' + shopSpell.id + '"]').click();
  window.confirm = originalConfirm;
  assert(!knownSpells.includes(shopSpell.id) && !backupPayload().character.knownSpells.includes(shopSpell.id), "Unlearning removes the spell from the character and backup");
  assert(!document.querySelector('[data-unlearn-spell="' + shopSpell.id + '"]') && document.getElementById("available-spells-list").textContent.includes(shopSpell.name), "Unlearned spells leave the known list and return to the available catalogue");
  document.querySelector('#spellbook-modal [data-close-modal="spellbook-modal"]').click();
  }

  const originalItemStock = ShopRules.itemStock;
  ShopRules.itemStock = (entries, count) => [...entries.filter(entry => entry.category === "consumable"), ...entries.filter(entry => entry.category !== "consumable")].slice(0, count);
  const town = WorldRules.settlements.find(entry => entry.id === "millhaven");
  worldState.position = WorldRules.point(town); shopState.locations = {};
  let townStock = Shop.stockFor(town);
  assert(townStock.blacksmith.length === 5 && townStock.general.length === 5 && townStock.spells.length === 7, "Minor towns stock their normal goods and seven spells");
  assert(townStock.blacksmith.filter(entry => getItem(entry.id)?.category === "weapon").length >= 2 && townStock.blacksmith.filter(entry => getItem(entry.id)?.category === "armor").length >= 2, "Minor-town blacksmiths guarantee at least 2 weapons and 2 armor pieces");
  assert(townStock.general.filter(entry => getItem(entry.id)?.category === "consumable").every(entry => entry.quantity >= 2 && entry.quantity <= 4), "Town consumables stock 2–4 copies");
  worldState.position = WorldRules.point(castle); shopState.locations = {};
  castleStock = Shop.stockFor(castle);
  assert(castleStock.general.filter(entry => getItem(entry.id)?.category === "consumable").every(entry => entry.quantity >= 3 && entry.quantity <= 6), "Castle consumables stock 3–6 copies");
  ShopRules.itemStock = originalItemStock;

  for (const [testLevel, expectedRarity] of [[1, "common"], [16, "uncommon"], [31, "rare"], [61, "epic"], [91, "legendary"]]) {
    level = testLevel; shopState = { version: 3, refreshWindow: -1, locations: {} };
    const stock = Shop.stockFor(castle).general.map(entry => getItem(entry.id));
    assert(stock.some(item => item.potionResource === "hp" && item.rarity === expectedRarity), "The General Store guarantees an appropriate Health potion at level " + testLevel);
    assert(stock.some(item => item.potionResource === "mana" && item.rarity === expectedRarity), "The General Store guarantees an appropriate Mana potion at level " + testLevel);
  }
  level = 1;

  const previousWindow = shopState.refreshWindow;
  worldState.minutes = (previousWindow + 1) * 360;
  Shop.onTimeChange();
  assert(shopState.refreshWindow === previousWindow + 1 && Object.keys(shopState.locations).length === 0, "All shop stocks reroll on six-hour boundaries");

  const potionIds = [
    "consumable-potion-of-lesser-health", "consumable-potion-of-health", "consumable-potion-of-greater-health", "consumable-potion-of-superior-health", "consumable-potion-of-ultimate-health",
    "consumable-potion-of-lesser-mana", "consumable-potion-of-mana", "consumable-potion-of-greater-mana", "consumable-potion-of-superior-mana", "consumable-potion-of-ultimate-mana"
  ];
  assert(potionIds.every(id => getItem(id)?.category === "consumable"), "All ten restorative potions load into the item catalogue");
  assert(JSON.stringify(potionIds.map(id => getItem(id).effects[0]).map(effect => [effect.amount, effect.maxPercent])) === JSON.stringify([[25,10],[50,15],[75,15],[100,20],[200,25],[10,5],[20,10],[30,15],[40,20],[50,20]]), "Health and Mana potion restoration follows the five-rarity progression");
  assert(itemCatalog.consumables.length === 14, "The complete consumables.json catalogue loads without a spell-tome file");
  assert(CombatRules.resourceRestore(25, 100, 10) === 35 && CombatRules.resourceRestore(20, 87, 10) === 28, "Potions combine flat and percentage restoration and round down");
  Combat.restore(null);
  return { passed: true, itemCount: items.length, spellCount: spells.length, tomeCount: 0, purse: ShopRules.walletValue(defaultCurrencies) };
}

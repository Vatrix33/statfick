/* Port task 9, the Potion Rework (2026-11-07): the Apothecary, potions, poisons, ingredients, foraging, brewing, recipes,
   the Alchemist and the Assimilator, Collect quests. The design answers P1–P31 are in engine/Docs/Engine-Plan.md,
   "Decisions for task 9"; the approved content is engine/Docs/Port-Tasks/9_Potion_Rework/draft.md; old-game/Docs/Notes.md,
   "Potion Rework", says where each rule lives. One entry per part. */
const potionChecks = {
  counter() {
    let checks = 0;
    return { assert: (condition, message) => { if (!condition) throw Error(message); checks++; }, count: () => checks };
  },
  wait: ms => new Promise(resolve => setTimeout(resolve, ms)),
  async withRandom(random, action) {
    const saved = Math.random;
    Math.random = random;
    try { return await action(); } finally { Math.random = saved; }
  },
  // Combat's own functions, from a second copy of combat.js with a hook (as the earlier tasks' checks do).
  async hooks() {
    if (window.__potionChecks) return window.__potionChecks;
    Combat.restore(null);
    const source = await (await fetch("scripts/combat.js")).text();
    const marker = "  window.Combat = {";
    const script = document.createElement("script");
    script.textContent = source.replace(marker, "  window.__potionChecks = { actorById, hp, setHp, values, getBattle: () => battle, useItem, applyEffects, applyCombatDamage, hurt, strike, directDamage, resistance, expire, actionPointMaximum, awardDefeatedEnemyExperience, applyNewClassWeaponEffects, estimatedWeaponStrike, attackLoadout, endTurn, currentActor, isControlledTurn, debuffImmune, controlImmune, eatIngredient, useSkill, skillsOf, classSkillKey, renderPicker: () => { pickerKind = \"item\"; renderPicker(); }, definitions: () => enemyDefinitions };\n" + marker);
    document.head.append(script);
    await waitForSourceFiles();
    if (window.BattleStage) window.BattleStage.enabled = () => false;
    if (window.GameSettings) window.GameSettings.enemyDelay = () => 1e9;
    await Combat.startEncounter(["Goblin"], "Task 9 checks"); Combat.restore(null);
    return window.__potionChecks;
  },
  // A hero at a level, with a pack, classes, money and up to two companions; standing at a place.
  hero({ level: heroLevel = 40, inventory: pack = [], classLevels: classes = {}, money = 0, companions: party = [], alchemy: record = null, worldState: world = null, weapon = null } = {}) {
    Combat.restore(null);
    const base = defaultCharacter();
    applyCharacter(normalizeCharacter({ ...base, raceId: "human", level: heroLevel, inventory: [...pack, ...(weapon ? [weapon] : [])], equipment: { ...base.equipment, ...(weapon ? { primaryHand: weapon } : {}) }, classLevels: { ...base.classLevels, ...classes }, ...(record ? { alchemy: record } : {}), ...(world ? { worldState: world } : {}) }));
    currencies.silver = money; currencies.gold = 0; currencies.platinum = 0;
    companions = party.map((member, index) => ({ id: "companion-potion-test-" + index, name: ["Mira", "Oren"][index], gender: "female",
      character: normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: member.level || heroLevel, inventory: [...(member.inventory || [])], classLevels: { ...defaultCharacter().classLevels, ...(member.classLevels || {}) } }, { companion: true }) }));
  },
  // A world standing in a settlement (or a point) of a region.
  at(regionId, place) {
    return { continentVersion: WorldRules.CONTINENT_VERSION, regionId, position: { x: place.x, y: place.y }, minutes: 600, visitedRegions: [regionId], caravans: [] };
  },
  place(regionId, kind) { return WorldRules.settlementsForRegion(regionId).find(entry => entry.kind === kind); },
  async battle(h, enemies, options = {}) {
    potionChecks.hero(options);
    if (!await Combat.startEncounter(enemies, "Task 9 test")) throw Error("The task 9 test battle starts");
    const battle = h.getBattle();
    return { battle, player: h.actorById("player"), foes: battle.actors.filter(actor => actor.team === "enemy"), allies: battle.actors.filter(actor => actor.team === "player" && actor.id !== "player") };
  },
  // Make an actor the one whose turn it is, with AP to spend.
  turnOf(h, actor, ap = 5) { const battle = h.getBattle(); battle.current = actor.id; battle.ap = ap; },
  item: name => items.find(entry => entry.name === name),
  ingredient: name => AlchemyRules.INGREDIENTS.find(entry => entry.name === name)?.id,
  count: id => inventory.filter(owned => owned === id).length,
};

// Sections 2, 3, 4, 8.1, 14 (counts): exactly 40 potions and 12 poisons by rarity, 56 ingredients each with a source
// and a recipe use, 62 three-ingredient recipes, the two Lesser recipes known from the start (P4), prices by rarity.
async function checkAlchemyContent() {
  const { assert, count } = potionChecks.counter(), A = AlchemyRules;
  await waitForSourceFiles();
  const potions = items.filter(item => item.category === "consumable" && item.potion && item.alchemy);
  const poisons = items.filter(item => item.category === "consumable" && item.poison);
  const tally = list => Object.fromEntries(A.RARITIES.map(rarity => [rarity, list.filter(item => item.rarity === rarity).length]));
  assert(JSON.stringify(tally(potions)) === JSON.stringify({ common: 8, uncommon: 12, rare: 10, epic: 6, legendary: 4 }), "40 new potions: 8 common, 12 uncommon, 10 rare, 6 epic, 4 legendary: " + JSON.stringify(tally(potions)));
  assert(JSON.stringify(tally(poisons)) === JSON.stringify({ common: 2, uncommon: 4, rare: 4, epic: 2, legendary: 0 }), "12 poisons: 2 common, 4 uncommon, 4 rare, 2 epic");
  assert(new Set([...potions, ...poisons].map(item => item.name)).size === 52 && [...potions, ...poisons].every(item => item.description && item.priceSilver > 0 && item.effects.length), "Each has its own name, a description, a price and effects");
  const healthMana = items.filter(item => item.potionResource);
  assert(healthMana.length === 10 && healthMana.every(item => item.potion && item.outsideBattle), "The ten Health and Mana Potions stay, as potions usable outside battle too");
  assert(A.INGREDIENTS.length >= 50 && A.INGREDIENTS.length === 56 && new Set(A.INGREDIENTS.map(entry => entry.name)).size === 56, "56 distinct ingredients (at least 50)");
  assert(A.INGREDIENTS.every(entry => getItem(entry.id)?.category === "ingredient" && entry.description), "Every ingredient is an item with a description");
  // Every recipe: three ingredients, a known product; every potion and poison has one; no two alike.
  assert(A.RECIPES.length === 62 && A.RECIPES.every(recipe => recipe.ingredients.length === 3 && new Set(recipe.ingredients).size === 3 && recipe.ingredients.every(A.isIngredientId) && getItem(recipe.id)), "62 recipes, each three different ingredients (as approved)");
  assert([...potions, ...poisons, ...healthMana].every(item => A.recipeFor(item.id)), "Every potion and poison has a recipe");
  assert(new Set(A.RECIPES.map(recipe => [...recipe.ingredients].sort().join("|"))).size === 62, "No two recipes share their three ingredients");
  // Every ingredient is in a recipe and has a source (foraged, dropped or a boss's).
  const D = AlchemyData;
  for (const entry of A.INGREDIENTS) {
    assert(A.recipesUsing(entry.id).length > 0, entry.name + " goes into a recipe");
    const foraged = Object.values(D.FORAGE).some(table => Object.values(table).some(list => list.some(found => found.id === entry.id))) || D.RIVER.includes(entry.id);
    const dropped = Object.values(D.DROPS).some(list => list.some(found => found.id === entry.id)) || Object.values(D.BOSS_DROPS).some(list => list.includes(entry.id));
    assert(foraged || dropped, entry.name + " can be found somewhere");
  }
  // Every forage zone is a zone of its region; every enemy (but Ool's phylactery) has ingredients.
  for (const [regionId, table] of Object.entries(D.FORAGE)) {
    const zones = new Set(Object.keys(EncounterRules.regionRules(regionId).tables || {}));
    for (const zone of Object.keys(table)) assert(zones.has(zone), `${zone} is a zone of ${regionId}`);
  }
  const h = await potionChecks.hooks();
  const enemies = h.definitions();
  for (const enemy of enemies) if (!enemy.inert) assert(A.enemyIngredients(enemy.id).length > 0, enemy.name + " drops an ingredient");
  assert([...Object.keys(D.DROPS), ...Object.keys(D.BOSS_DROPS)].every(id => enemies.some(enemy => enemy.id === id)), "Every drop table names a real enemy");
  // Prices rise with rarity (the averages), ingredients cost less than the potion they make (brewing pays).
  const average = (list, rarity) => { const chosen = list.filter(item => item.rarity === rarity); return chosen.reduce((sum, item) => sum + item.priceSilver, 0) / Math.max(1, chosen.length); };
  for (const list of [potions, A.INGREDIENTS]) for (let index = 1; index < 5; index++) if (list === potions || A.INGREDIENTS.some(entry => entry.rarity === A.RARITIES[index])) assert(average(list, A.RARITIES[index]) > average(list, A.RARITIES[index - 1]), `Prices rise from ${A.RARITIES[index - 1]} to ${A.RARITIES[index]}`);
  for (const recipe of A.RECIPES) assert(recipe.ingredients.reduce((sum, id) => sum + A.ingredient(id).priceSilver, 0) < getItem(recipe.id).priceSilver, `Brewing ${getItem(recipe.id).name} costs less than buying it`);
  // Known from the start: the two Lesser recipes, also for an old save (P4, section 13).
  potionChecks.hero({});
  assert(JSON.stringify([...alchemy.known].sort()) === JSON.stringify(["consumable-potion-of-lesser-health", "consumable-potion-of-lesser-mana"]), "A new character knows the two Lesser recipes");
  return { checks: count() };
}

// Sections 1, 6, P3, P13, P26, P28: an Apothecary in every settlement (O), castle and town banners, Health and Mana
// Potions of your band and below always and unlimited, random potions and poisons, ingredients, selling at half price;
// the General Store sells and buys no potions.
async function checkApothecaryShop() {
  const { assert, count } = potionChecks.counter();
  await waitForSourceFiles();
  const castle = potionChecks.place("alder-vale", "Castle city"), town = potionChecks.place("alder-vale", "Town");
  potionChecks.hero({ level: 40, money: 200000, worldState: potionChecks.at("alder-vale", castle) });
  document.getElementById("map-open-button").click();
  await potionChecks.wait(150);
  for (const regionId of Object.keys(WorldRules.regions)) for (const place of WorldRules.settlementsForRegion(regionId).filter(entry => WorldRules.canAccessSettlement(entry))) {
    worldState.regionId = regionId; WorldRules.setActiveRegion(regionId); worldState.position = { x: place.x, y: place.y };
    WorldMap.render();
    const button = document.getElementById("map-apothecary");
    assert(!button.hidden && !button.disabled, "Every settlement has an Apothecary: " + place.name);
  }
  assert(document.getElementById("map-apothecary").dataset.mapHotkey === "O", "The Apothecary's key is O (P26)");
  // Castle and town banners; both placeholder art (P2).
  assert(SceneBanners.variantFor("apothecary", castle) === "apothecary-castle" && SceneBanners.variantFor("apothecary", town) === "apothecary-town", "Castle cities and towns have their own Apothecary banner");
  assert(SceneBanners.placeholderArt("apothecary-castle") && SceneBanners.placeholderArt("apothecary-town") && SceneBanners.markup("apothecary", castle, "alder-vale").length > 1000, "Both banners draw, as placeholder art (P2)");
  potionChecks.hero({ level: 40, money: 200000, worldState: potionChecks.at("alder-vale", castle) });
  Apothecary.open();
  assert(!document.getElementById("apothecary-modal").hidden && document.getElementById("apothecary-scene").dataset.variant === "apothecary-castle", "The Apothecary opens with the castle banner");
  // P3: at Level 40 (band rare) the common, uncommon and rare Health and Mana pairs, unlimited.
  const standing = Apothecary.standingPotions().map(item => item.name);
  assert(standing.length === 6 && ["Potion of Lesser Health", "Potion of Greater Mana"].every(name => standing.includes(name)) && !standing.some(name => /Superior|Ultimate/.test(name)), "Your band's Health and Mana pair and the weaker ones: " + standing.join(", "));
  const lesser = potionChecks.item("Potion of Lesser Health");
  const before = ShopRules.walletValue(currencies);
  for (let index = 0; index < 12; index++) document.querySelector(`[data-apothecary-buy="${lesser.id}"]`).click();
  assert(potionChecks.count(lesser.id) === 12 && ShopRules.walletValue(currencies) === before - 12 * lesser.priceSilver && document.querySelector(`[data-apothecary-buy="${lesser.id}"]`) && !document.querySelector(`[data-apothecary-buy="${lesser.id}"]`).disabled, "They never run out");
  const stock = Apothecary.stock();
  assert(stock.potions.length === 8 && stock.potions.every(entry => getItem(entry.id)?.alchemy && entry.quantity >= 3 && entry.quantity <= 6), "A castle city's Apothecary also has 8 random potions and poisons, 3–6 of each");
  // The ingredient menu: every common ingredient, and the region's rarer ones (castle 5).
  Apothecary.setMenu("ingredients");
  const commons = AlchemyRules.INGREDIENTS.filter(entry => entry.rarity === "common").map(entry => entry.id);
  const rarer = stock.ingredients.filter(entry => AlchemyRules.ingredient(entry.id).rarity !== "common");
  const regional = Apothecary.regionIngredients("alder-vale");
  assert(commons.every(id => stock.ingredients.some(entry => entry.id === id && entry.quantity === 10)), "Every common ingredient, 10 of each");
  assert(rarer.length === Math.min(5, regional.filter(id => AlchemyRules.ingredient(id).rarity !== "common").length) && rarer.every(entry => regional.includes(entry.id)), "And the region's own rarer ones (P13)");
  // The same stock on reopening; a new one after the 6-hour restock.
  const snapshot = JSON.stringify(Apothecary.stock());
  Apothecary.close(); Apothecary.open();
  assert(JSON.stringify(Apothecary.stock()) === snapshot, "The stock stays until the restock");
  // Selling at half price (P28), and the General Store no longer sells or buys potions.
  Apothecary.setMenu("sell");
  const vigor = potionChecks.item("Draught of Vigor"), sale = ShopRules.walletValue(currencies);
  inventory.push(vigor.id); Apothecary.render();
  document.querySelector(`[data-apothecary-sell="${vigor.id}"]`).click();
  assert(potionChecks.count(vigor.id) === 0 && ShopRules.walletValue(currencies) === sale + Math.floor(vigor.priceSilver / 2), "The Apothecary buys potions at half price");
  Apothecary.close();
  const general = Shop.stockFor(castle).general.map(entry => getItem(entry.id));
  assert(general.length && !general.some(item => item.potion || item.poison || item.category === "ingredient"), "The General Store sells no potions, poisons or ingredients");
  Shop.open("general"); document.getElementById("shop-sell-mode").click();
  assert(!document.querySelector(`[data-sell-item="${lesser.id}"]`), "Nor does it buy them");
  document.querySelector("[data-shop-close]").click();
  worldState.minutes += 360; Shop.onTimeChange();
  Apothecary.open();
  assert(JSON.stringify(Apothecary.stock()) !== snapshot, "Six hours later the Apothecary has restocked");
  Apothecary.close();
  // A town's: 5 random potions and poisons, 3 rarer ingredients, the town banner.
  potionChecks.hero({ level: 40, money: 0, worldState: potionChecks.at("alder-vale", town) });
  Apothecary.open();
  assert(Apothecary.stock().potions.length === 5 && document.getElementById("apothecary-scene").dataset.variant === "apothecary-town", "A town's Apothecary: 5 random potions and poisons, its own banner");
  Apothecary.close();
  return { checks: count() };
}

// Section 8, P14–P16: three unknown recipes per Apothecary, ten times the potion's price, the same until the restock;
// bought, a recipe is known for good, never offered again and can't be bought twice; fewer when fewer remain.
async function checkRecipeOffers() {
  const { assert, count } = potionChecks.counter();
  await waitForSourceFiles();
  const castle = potionChecks.place("alder-vale", "Castle city");
  potionChecks.hero({ level: 40, money: 2000000, worldState: potionChecks.at("alder-vale", castle) });
  Apothecary.open(); Apothecary.setMenu("recipes");
  const offered = Apothecary.stock().recipes;
  assert(offered.length === 3 && new Set(offered).size === 3 && offered.every(id => !alchemy.known.includes(id) && AlchemyRules.recipeFor(id)), "Three recipes you don't know");
  const first = offered[0], price = getItem(first).priceSilver * 10, before = ShopRules.walletValue(currencies);
  document.querySelector(`[data-apothecary-recipe="${first}"]`).click();
  assert(alchemy.known.includes(first) && ShopRules.walletValue(currencies) === before - price, "A recipe costs ten times its potion and is learned at once (P14)");
  assert(!document.querySelector(`[data-apothecary-recipe="${first}"]`) && document.querySelectorAll("[data-apothecary-recipe]").length === 2, "A bought recipe isn't offered again");
  const after = ShopRules.walletValue(currencies);
  Apothecary.buyRecipe(first);
  assert(ShopRules.walletValue(currencies) === after && alchemy.known.filter(id => id === first).length === 1, "A known recipe can't be bought twice");
  Apothecary.close(); Apothecary.open(); Apothecary.setMenu("recipes");
  assert(JSON.stringify(Apothecary.stock().recipes) === JSON.stringify(offered) && ShopRules.walletValue(currencies) === after, "The offer stays the same until the restock, and nothing is charged twice");
  worldState.minutes += 360; Shop.onTimeChange();
  Apothecary.close(); Apothecary.open(); Apothecary.setMenu("recipes");
  assert(Apothecary.stock().recipes.length === 3 && !Apothecary.stock().recipes.includes(first), "After the restock, three new ones, never a known one (P15)");
  // The offers lean toward your level band (P16): at Level 40 (rare) rare recipes come up most often.
  const rarities = {};
  await potionChecks.withRandom(Math.random, () => {
    for (let index = 0; index < 400; index++) for (const id of AlchemyRules.recipeOffers({ known: AlchemyRules.STARTING_RECIPES, level: 40, productRarity: product => getItem(product).rarity })) rarities[getItem(id).rarity] = (rarities[getItem(id).rarity] || 0) + 1;
  });
  assert(rarities.rare > rarities.common && rarities.rare > rarities.epic && rarities.legendary > 0, "Recipes lean toward your level band's rarity, others now and then: " + JSON.stringify(rarities));
  // With two unknown recipes left, two are offered.
  alchemy.known = AlchemyRules.RECIPES.map(recipe => recipe.id).slice(2);
  worldState.minutes += 360; Shop.onTimeChange();
  Apothecary.close(); Apothecary.open(); Apothecary.setMenu("recipes");
  assert(document.querySelectorAll("[data-apothecary-recipe]").length === 2, "With two left, only two are offered");
  Apothecary.close();
  return { checks: count() };
}

// Section 7, P31: three boxes, any order, a known recipe; nothing used up by an incomplete, unknown or impossible
// combination; brewing costs nothing else; the potion waits on the cauldron to be collected; picking a recipe fills the
// boxes when you have the ingredients.
async function checkBrewing() {
  const { assert, count } = potionChecks.counter(), I = potionChecks.ingredient;
  await waitForSourceFiles();
  const castle = potionChecks.place("alder-vale", "Castle city");
  const mint = I("Meadowmint"), berry = I("Redcap Berry"), reed = I("Riverreed"), dew = I("Spirit Dew");
  potionChecks.hero({ level: 20, money: 500, inventory: [mint, mint, berry, reed, dew], worldState: potionChecks.at("alder-vale", castle) });
  Apothecary.open(); Apothecary.setMenu("brewing");
  const money = ShopRules.walletValue(currencies), lesser = "consumable-potion-of-lesser-health";
  assert(document.getElementById("apothecary-cauldron").querySelectorAll("[data-cauldron-slot]").length === 3 && !document.getElementById("apothecary-cauldron").hidden, "The cauldron with three boxes");
  assert([...document.querySelectorAll("[data-apothecary-pick]")].map(button => button.dataset.apothecaryPick).sort().join() === [...alchemy.known].sort().join(), "Your known recipes are listed beside it");
  // Incomplete: nothing used.
  Apothecary.placeIngredient(mint); Apothecary.placeIngredient(berry);
  Apothecary.brew();
  assert(potionChecks.count(mint) === 2 && potionChecks.count(berry) === 1 && !Apothecary.result(), "Two boxes brew nothing and use nothing");
  // Unknown combination: nothing used.
  Apothecary.placeIngredient(dew);
  Apothecary.brew();
  assert(potionChecks.count(dew) === 1 && !Apothecary.result() && /match/.test(document.getElementById("apothecary-status").textContent), "A combination that is no known recipe uses nothing, and says so");
  // A box can be emptied and refilled; any order works (reed, berry, mint for Lesser Health).
  document.querySelector('[data-cauldron-clear]').click();
  Apothecary.placeIngredient(reed); Apothecary.placeIngredient(berry); Apothecary.placeIngredient(mint);
  assert(/Lesser Health/.test(document.querySelector(".cauldron-result").textContent), "The cauldron recognises the recipe before brewing");
  Apothecary.brew();
  assert(Apothecary.result() === lesser && potionChecks.count(mint) === 1 && potionChecks.count(berry) === 0 && potionChecks.count(reed) === 0, "Brewing uses exactly the three");
  assert(ShopRules.walletValue(currencies) === money && alchemy.brewed === 1, "Brewing costs nothing else, and it is counted");
  assert(potionChecks.count(lesser) === 0 && document.querySelector("[data-cauldron-collect]") && !document.querySelector("[data-cauldron-collect]").hidden, "The potion waits on the cauldron");
  document.querySelector("[data-cauldron-collect]").click();
  assert(potionChecks.count(lesser) === 1 && !Apothecary.result(), "Collect puts it in your pack");
  // Duplicates can't use more than you own.
  Apothecary.placeIngredient(mint); Apothecary.placeIngredient(mint);
  assert(Apothecary.slots().filter(id => id === mint).length === 1, "A box can't take a copy you don't have");
  // A known recipe you can't afford in ingredients: the boxes stay; one you can: they fill.
  document.querySelector('[data-cauldron-clear]').click();
  Apothecary.pickRecipe(lesser);
  assert(Apothecary.slots().every(slot => slot === null) && /lack/.test(document.getElementById("apothecary-status").textContent), "Picking a recipe without its ingredients says what's missing");
  inventory.push(berry, reed);
  Apothecary.pickRecipe(lesser);
  assert(Apothecary.slots().filter(Boolean).length === 3, "Picking a recipe with its ingredients fills the boxes");
  // A recipe that exists but isn't known can't be brewed.
  const vigor = AlchemyRules.recipeFor("consumable-draught-of-vigor");
  inventory.push(...vigor.ingredients);
  document.querySelector('[data-cauldron-clear]').click();
  vigor.ingredients.forEach(id => Apothecary.placeIngredient(id));
  Apothecary.brew();
  assert(!Apothecary.result() && vigor.ingredients.every(id => potionChecks.count(id) >= 1), "An unknown recipe can't be brewed");
  // A finished potion left on the cauldron is collected when the window closes.
  document.querySelector('[data-cauldron-clear]').click();
  Apothecary.pickRecipe(lesser); Apothecary.brew();
  Apothecary.close();
  assert(potionChecks.count(lesser) === 2, "Closing the window collects a finished potion");
  return { checks: count() };
}

// Sections 2.2, 2.3, P6–P9: potions in battle. Buffs on yourself or an ally (1 AP), held once and refreshed, different
// potions adding up, ending on time; every new kind of effect does what it says (immunities, barrier, AP, rising again,
// can't fall, the toll, resistances, crit, pierce, damage to elites, cures); Health, Mana and cures outside battle.
async function checkPotionsInBattle() {
  const { assert, count } = potionChecks.counter(), h = await potionChecks.hooks(), P = potionChecks.item;
  const pack = ["Draught of Vigor", "Draught of Vigor", "Elixir of Might", "Elixir of Kinship", "Draught of Haste", "Potion of Steadfastness", "Elixir of the Unbroken", "Bulwark Potion",
    "Undying Draught", "Phoenix Tonic", "Elixir of Ascendance", "Emberward Tonic", "Emberward Tonic", "Prismatic Ward", "Potion of Precision", "Duelist's Draught", "Piercing Draught", "Giantsbane Draught",
    "Antidote", "Clarity Draught", "Purging Elixir", "Mending Draught"].map(name => P(name).id);
  // A forged one-handed weapon that can land critical hits.
  let sword = null;
  for (let seed = 1; !sword && seed < 200; seed++) {
    let state = seed;
    const random = () => (state = (state * 9301 + 49297) % 233280) / 233280;
    const candidate = getItem(ForgeRules.encode(ForgeRules.generate({ level: 60, source: "shop", random, casterShare: 0 })));
    if (candidate?.category === "weapon" && candidate.handed === 1 && normalizeWeaponType?.(candidate.type) !== "ranged" && (candidate.minimumStrength || 0) <= 10) sword = candidate.id;
  }
  let { battle, player, foes, allies } = await potionChecks.battle(h, ["Stone Giant", "Stone Giant"], { level: 60, inventory: pack, companions: [{ level: 60 }], weapon: sword });
  const ally = allies[0], foe = foes[0];
  const use = (actor, target, name) => { potionChecks.turnOf(h, actor); return h.useItem(actor, target, P(name)); };
  const effectsNamed = (actor, name) => actor.effects.filter(effect => effect.name === name);
  // Drunk on yourself: 1 AP; again: refreshed, not stacked; a different potion adds up (P7).
  potionChecks.turnOf(h, player);
  assert(h.useItem(player, player, P("Draught of Vigor")) && battle.ap === 4, "A potion costs 1 AP");
  const vigor = effectsNamed(player, "Draught of Vigor");
  assert(vigor.length === 1 && vigor[0].type === "stat-percent" && vigor[0].target === "strength" && vigor[0].amount === 10, "Draught of Vigor: +10% Strength");
  player.turns += 1;
  use(player, player, "Draught of Vigor");
  assert(effectsNamed(player, "Draught of Vigor").length === 1 && effectsNamed(player, "Draught of Vigor")[0].expiresTurn === player.turns + 3, "Drunk again it refreshes its 3 turns, not stacking");
  use(player, player, "Elixir of Might");
  assert(effectsNamed(player, "Elixir of Might").length === 1 && player.effects.filter(effect => effect.type === "stat-percent" && effect.target === "strength").length === 2, "A different potion on the same stat adds up");
  // Handed to an ally (P9), and Kinship reaches everyone.
  assert(use(player, ally, "Emberward Tonic") && effectsNamed(ally, "Emberward Tonic").length === 1 && !effectsNamed(player, "Emberward Tonic").length, "A buff potion can be handed to an ally");
  h.setHp(player, 10); h.setHp(ally, 10);
  use(player, player, "Elixir of Kinship");
  assert(h.hp(player) > 10 && h.hp(ally) > 10 && effectsNamed(ally, "Elixir of Kinship").some(effect => effect.type === "all-stats-percent" && effect.amount === 15), "Elixir of Kinship heals and strengthens every ally");
  // Durations: the effect is gone once its turns have passed.
  const strength = () => h.values(player).strength;
  const boosted = strength();
  for (let index = 0; index < 3; index++) h.expire(player);
  assert(!effectsNamed(player, "Draught of Vigor").length && strength() < boosted, "When its turns are over the buff ends");
  // Draught of Haste: one more AP each turn.
  const ap = h.actionPointMaximum(player);
  use(player, player, "Draught of Haste");
  assert(h.actionPointMaximum(player) === ap + 1, "Draught of Haste: +1 AP each turn");
  // Steadfastness: no stun, no AP loss; Unbroken: no negative effects.
  use(player, player, "Potion of Steadfastness");
  h.applyEffects(foe, player, [{ type: "stun", amount: 1, durationTurns: 1, recipient: "target" }, { type: "root-ap", amount: 2, recipient: "target" }], 0, "basic", "Test blow");
  assert(!player.effects.some(effect => effect.type === "stun") && !player.rootedAp, "Potion of Steadfastness: immune to stuns and AP loss");
  use(player, ally, "Elixir of the Unbroken");
  h.applyEffects(foe, ally, [{ type: "stat-percent", target: "strength", amount: -20, durationTurns: 2, recipient: "target" }], 0, "basic", "Test curse");
  assert(!ally.effects.some(effect => effect.name === "Test curse"), "Elixir of the Unbroken: no negative effect takes hold");
  // Bulwark: a barrier of 15% max HP, the rest fading when it ends.
  player.barrierHp = 0;
  use(player, player, "Bulwark Potion");
  const barrier = Math.floor(h.values(player).maxHp * 0.15);
  assert(player.barrierHp === barrier, "Bulwark Potion: a barrier of 15% of max HP");
  for (let index = 0; index < 3; index++) h.expire(player);
  assert(!player.barrierHp, "What's left of it fades when it ends");
  // Undying: held at 1 HP; healed 20% as it ends. Phoenix Tonic: rises once at 30%.
  use(player, player, "Undying Draught");
  h.setHp(player, 50); h.applyCombatDamage(player, 100000, foe);
  assert(h.hp(player) === 1, "Undying Draught: lethal damage leaves 1 HP");
  h.expire(player); h.expire(player);
  assert(h.hp(player) > 1 && !player.effects.some(effect => effect.type === "undying"), "When it ends its drinker heals");
  use(player, ally, "Phoenix Tonic");
  h.setHp(ally, 50); h.applyCombatDamage(ally, 100000, foe);
  assert(h.hp(ally) === Math.floor(h.values(ally).maxHp * 0.3) && !ally.effects.some(effect => effect.type === "rise-again-percent"), "Phoenix Tonic: rises once at 30% of max HP");
  h.applyCombatDamage(ally, 100000, foe);
  assert(h.hp(ally) === 0, "Only once");
  // Ascendance: +35% to every attribute, then the toll: −15% for 2 turns.
  const plain = h.values(player).strength;
  use(player, player, "Elixir of Ascendance");
  assert(h.values(player).strength > plain, "Elixir of Ascendance raises every attribute");
  for (let index = 0; index < 3; index++) h.expire(player);
  assert(player.effects.some(effect => effect.type === "all-stats-percent" && effect.amount === -15) && h.values(player).strength < plain, "Then takes its toll");
  h.expire(player); h.expire(player);
  assert(!player.effects.some(effect => effect.type === "all-stats-percent" && effect.amount === -15), "The toll ends after 2 turns");
  // Resistances: Emberward (fire), Prismatic (every element).
  const fire = h.resistance(player, "fire"), ice = h.resistance(player, "ice");
  use(player, player, "Emberward Tonic");
  assert(h.resistance(player, "fire") === fire + 20 && h.resistance(player, "ice") === ice, "Emberward Tonic: +20% Fire resistance only");
  use(player, player, "Prismatic Ward");
  assert(h.resistance(player, "ice") === ice + 20 && h.resistance(player, "fire") === fire + 40, "Prismatic Ward: +20% to every element");
  // Crit chance, crit damage, armor pierce and damage to elites, in the forecast and in the real blow.
  const weapon = h.attackLoadout(player).primary;
  const forecast = () => h.estimatedWeaponStrike(player, foe, weapon);
  h.setHp(foe, h.values(foe).maxHp);
  const base = forecast();
  // A strike whose dice land between the first-roll crit chances without and with the potion (crits build momentum).
  const strikeCrits = async () => {
    h.setHp(foe, h.values(foe).maxHp); player.critMisses = 0;
    const from = battle.log.length;
    const roll = (CombatRules.critChanceAfterMisses(base.critChance, 0) + CombatRules.critChanceAfterMisses(base.critChance + 10, 0)) / 200;
    await potionChecks.withRandom(() => roll, async () => { h.strike(player, foe, weapon); });
    return battle.log.slice(from).some(line => /\(critical\)/.test(String(line)));
  };
  assert(!await strikeCrits(), "Without the potion that roll is no critical hit");
  use(player, player, "Potion of Precision");
  assert(base.critChance > 0 && base.critChance < 90 && forecast().critChance === base.critChance + 10, "Potion of Precision: +10% critical hit chance");
  assert(await strikeCrits(), "With it the same roll is a critical hit");
  const blow = (critical, target = foe) => { h.setHp(target, h.values(target).maxHp); return h.hurt(target, 200, "basic", critical, false, player, "melee"); };
  const critBefore = blow(true);
  use(player, player, "Duelist's Draught");
  h.setHp(foe, h.values(foe).maxHp);
  assert(blow(true) > critBefore && forecast().criticalDamage > base.criticalDamage, "Duelist's Draught: more critical damage");
  const unpierced = blow(false);
  use(player, player, "Piercing Draught");
  assert(h.values(foe).armor > 0 && blow(false) > unpierced, "Piercing Draught: weapon attacks ignore some Armor");
  const plainFoe = blow(false);
  use(player, player, "Giantsbane Draught");
  assert(blow(false) === plainFoe, "Giantsbane doesn't help against an ordinary enemy");
  foe.unique = true;
  assert(blow(false) > plainFoe, "Giantsbane Draught: more damage to bosses, Empowered and Sparkling enemies");
  foe.unique = false;
  // Cures: Antidote takes a poison first; Clarity two; Purging everything, and nothing new until the next turn.
  h.setHp(ally, h.values(ally).maxHp); ally.alive = true;
  ally.effects = ally.effects.filter(effect => effect.type !== "debuff-immune");
  ally.effects.push({ key: "test-poison", name: "Test poison", type: "class-poison", amount: 5, percentMaxHp: 0, expiresTurn: ally.turns + 3 },
    { key: "test-weak", name: "Test weakness", type: "stat-percent", target: "strength", amount: -10, expiresTurn: ally.turns + 3 },
    { key: "test-slow", name: "Test slowness", type: "stat-percent", target: "speed", amount: -10, expiresTurn: ally.turns + 3 },
    { key: "test-frail", name: "Test frailty", type: "defense-percent", target: "armor", amount: -10, expiresTurn: ally.turns + 3 });
  use(player, ally, "Antidote");
  assert(!ally.effects.some(effect => effect.name === "Test poison") && ally.effects.filter(effect => /^Test/.test(effect.name)).length === 3, "Antidote removes one negative effect, a poison first");
  use(player, ally, "Clarity Draught");
  assert(ally.effects.filter(effect => /^Test/.test(effect.name)).length === 1, "Clarity Draught removes two");
  use(player, ally, "Purging Elixir");
  h.applyEffects(foe, ally, [{ type: "stat-percent", target: "speed", amount: -30, durationTurns: 2, recipient: "target" }], 0, "basic", "Test again");
  assert(!ally.effects.some(effect => /^Test/.test(effect.name)), "Purging Elixir removes every negative effect and keeps new ones off");
  // Mending Draught: 4% of max HP at the start of each of 4 turns.
  h.setHp(player, 1);
  use(player, player, "Mending Draught");
  h.expire(player);
  assert(h.hp(player) === 1 + Math.max(1, Math.floor(h.values(player).maxHp * 0.04)), "Mending Draught heals 4% of max HP each turn");
  Combat.restore(null);
  // Outside battle (P6): Health and Mana Potions on yourself or a companion; a cure has nothing to do.
  potionChecks.hero({ level: 30, inventory: [P("Potion of Health").id, P("Potion of Mana").id, P("Antidote").id], companions: [{ level: 30 }] });
  currentHp = 1; currentMana = 1;
  assert(potionOutsideReason(P("Antidote"), "player") === "Nothing to cure outside battle", "A cure can't be used outside battle");
  assert(usePotionOutsideBattle(P("Potion of Health").id, "player") && currentHp > 1 && !inventory.includes(P("Potion of Health").id), "A Health Potion heals you outside battle");
  companions[0].character.currentMana = 0;
  assert(usePotionOutsideBattle(P("Potion of Mana").id, companions[0].id) && companions[0].character.currentMana > 0, "A Mana Potion restores a companion outside battle");
  inventory.push(P("Potion of Health").id); currentHp = calculateMaxHpWithArtifacts(currentStats());
  assert(potionOutsideReason(P("Potion of Health"), "player") && !usePotionOutsideBattle(P("Potion of Health").id, "player") && inventory.includes(P("Potion of Health").id), "At full HP it isn't wasted");
  assert(potionOutsideReason(P("Draught of Vigor"), "player") === "Only in battle", "Buff potions only in battle");
  return { checks: count() };
}

// Section 3, P8: poisons are thrown at an enemy (1 AP; Plague Bomb at all), poison damage is Nature damage that Nature
// resistance lowers, the same poison refreshes, and bosses take every poison at half strength.
async function checkPoisons() {
  const { assert, count } = potionChecks.counter(), h = await potionChecks.hooks(), P = potionChecks.item;
  const pack = ["Toxic Vial", "Toxic Vial", "Wyrmbane Venom", "Wyrmbane Venom", "Corrosive Flask", "Paralytic Toxin", "Paralytic Toxin", "Plague Bomb", "Hexing Vial", "Sluggard's Dust"].map(name => P(name).id);
  // A sturdy giant for the damage, and two goblins (giants can't be stunned): one stands in for a boss.
  const { battle, player, foes } = await potionChecks.battle(h, ["Stone Giant", "Goblin", "Goblin"], { level: 60, inventory: pack });
  const [foe, boss, third] = foes;
  boss.unique = true;
  const turn = () => potionChecks.turnOf(h, player);
  // Thrown at an enemy: never at yourself.
  turn();
  assert(!h.useItem(player, player, P("Toxic Vial")) && potionChecks.count(P("Toxic Vial").id) === 2, "A poison can't be thrown at yourself");
  assert(h.useItem(player, foe, P("Toxic Vial")) && battle.ap === 4, "It is thrown at an enemy for 1 AP");
  const poison = foe.effects.find(effect => effect.type === "class-poison" && effect.name === "Toxic Vial");
  assert(poison && poison.amount === 6 && poison.percentMaxHp === 2 && poison.itemPoison && poison.expiresTurn === foe.turns + 3, "Toxic Vial: 6 + 2% of max HP each turn for 3 turns");
  // The tick: Nature damage, lowered by Nature resistance.
  const before = h.hp(foe);
  h.expire(foe);
  const plainTick = before - h.hp(foe);
  assert(plainTick === Math.floor((6 + Math.floor(h.values(foe).maxHp * 0.02)) * Math.max(0, 1 - h.resistance(foe, "nature") / 100)), "Each tick deals its Nature damage: " + plainTick);
  foe.resistances = { ...(foe.resistances || {}), nature: 50 };
  const resisted = h.hp(foe);
  h.expire(foe);
  assert(resisted - h.hp(foe) < plainTick, "Nature resistance lowers it");
  foe.resistances.nature = 0;
  // Thrown again: refreshed, not doubled.
  turn(); h.useItem(player, foe, P("Toxic Vial"));
  assert(foe.effects.filter(effect => effect.name === "Toxic Vial" && effect.type === "class-poison").length === 1 && foe.effects.find(effect => effect.name === "Toxic Vial").expiresTurn === foe.turns + 3, "The same poison again refreshes it");
  // A boss takes it at half strength (P8): damage, reductions, chances and durations halved, rounded up.
  turn(); h.useItem(player, boss, P("Wyrmbane Venom"));
  const half = boss.effects.find(effect => effect.type === "class-poison");
  assert(half && half.amount === 10 && half.percentMaxHp === 2 && half.expiresTurn === boss.turns + 2 && /half strength/.test(half.name), "On a boss, Wyrmbane Venom is 10 + 2% for 2 turns (half of 20 + 4% for 4)");
  turn(); h.useItem(player, foe, P("Wyrmbane Venom"));
  assert(foe.effects.find(effect => effect.name === "Wyrmbane Venom").amount === 20, "Full strength on an ordinary enemy");
  turn(); h.useItem(player, boss, P("Corrosive Flask"));
  const corroded = boss.effects.find(effect => effect.type === "defense-percent");
  assert(corroded.amount === -10 && corroded.expiresTurn === boss.turns + 2, "Corrosive Flask's −20% Armor for 3 turns is −10% for 2 turns on a boss (halves rounded up)");
  // Paralytic Toxin: a 35% stun chance (18% on a boss) and 1 AP lost.
  await potionChecks.withRandom(() => 0.3, async () => { turn(); h.useItem(player, third, P("Paralytic Toxin")); });
  assert(third.effects.some(effect => effect.type === "stun") && third.rootedAp === 1, "Paralytic Toxin: stunned on a 35% roll, and 1 AP lost");
  await potionChecks.withRandom(() => 0.3, async () => { turn(); h.useItem(player, boss, P("Paralytic Toxin")); });
  assert(!boss.effects.some(effect => effect.type === "stun") && boss.rootedAp === 1, "On a boss the chance is halved: a 30% roll misses");
  // Plague Bomb: every enemy.
  turn(); h.useItem(player, foe, P("Plague Bomb"));
  assert(foes.every(enemy => enemy.effects.some(effect => /Plague Bomb/.test(effect.name) && effect.type === "class-poison")), "Plague Bomb poisons every enemy");
  // Hexing Vial lowers every resistance; Sluggard's Dust slows.
  const fire = h.resistance(third, "fire"), speed = h.values(third).speed;
  turn(); h.useItem(player, third, P("Hexing Vial"));
  turn(); h.useItem(player, third, P("Sluggard's Dust"));
  assert(h.resistance(third, "fire") === fire - 20 && h.values(third).speed < speed, "Hexing Vial: −20% resistance to every element; Sluggard's Dust: slower");
  // A debuff-immune enemy shrugs a poison off.
  third.effects.push({ key: "test-immune", name: "Test ward", type: "debuff-immune", amount: 1, expiresTurn: third.turns + 5 });
  third.effects = third.effects.filter(effect => effect.type !== "class-poison");
  inventory.push(P("Toxic Vial").id);
  turn(); h.useItem(player, third, P("Toxic Vial"));
  assert(!third.effects.some(effect => effect.type === "class-poison"), "An enemy immune to debuffs isn't poisoned");
  return { checks: count() };
}

// Section 10, P19–P21, P29, P30: the Assimilator eats an ingredient in battle (1 AP) for a buff from its potions:
// a third as strong at Level 1, half at 4, two-thirds and the special effects at 6; the same ingredient refreshes,
// different ones add up; 25% kept at Level 5; Devour (Level 7) costs no AP and lasts twice as long; a poison-only
// ingredient poisons with each weapon hit; only an Assimilator sees ingredients in the battle's item menu.
async function checkAssimilator() {
  const { assert, count } = potionChecks.counter(), h = await potionChecks.hooks(), I = potionChecks.ingredient;
  const glass = I("Glass Sand"), mint = I("Meadowmint"), venom = I("Lamia Venom"), feather = I("Stormfeather");
  // Without the class: no ingredients to eat.
  let { battle, player, foes } = await potionChecks.battle(h, ["Stone Giant"], { level: 60, inventory: [glass] });
  potionChecks.turnOf(h, player);
  assert(!h.useItem(player, player, getItem(glass)), "Only an Assimilator can eat an ingredient");
  h.renderPicker();
  assert(!document.querySelector(`#combat-ability-list [data-combat-ability="${glass}"]`), "and only an Assimilator sees ingredients in the item menu");
  // Level 1: a third.
  ({ battle, player, foes } = await potionChecks.battle(h, ["Stone Giant"], { level: 60, inventory: [glass, glass, mint, venom, feather, feather], classLevels: { assimilator: 1 } }));
  potionChecks.turnOf(h, player);
  h.renderPicker();
  const eat = document.querySelector(`#combat-ability-list [data-combat-ability="${glass}"]`);
  assert(eat && eat.textContent === "Eat", "An Assimilator's item menu offers ingredients to eat");
  assert(h.useItem(player, player, getItem(glass)) && battle.ap === 4 && potionChecks.count(glass) === 1, "Eating costs 1 AP and uses the ingredient up");
  const expected = AlchemyRules.assimilation(glass, 1, getItem);
  const eaten = player.effects.filter(effect => effect.name === "Glass Sand (eaten)");
  assert(eaten.length === expected.effects.length && eaten.length === 2 && expected.factor === 1 / 3, "Glass Sand: its potions' two strongest kinds, at a third");
  const crit = eaten.find(effect => effect.type === "crit-damage-percent");
  assert(crit && crit.amount === 10 && crit.expiresTurn === player.turns + 3, "Duelist's +30% critical damage becomes +10% for 3 turns (an uncommon ingredient)");
  const lasts = id => AlchemyRules.assimilation(id, 1, getItem).durationTurns;
  assert(lasts(I("Meadowmint")) === 2 && lasts(feather) === 3 && lasts(I("Dragon Blood")) === 4 && lasts(I("Starfall Dust")) === 4, "2 turns for a common ingredient, 3 for a rare one, 4 for epic and legendary");
  // The same ingredient refreshes; another adds up.
  player.turns += 1;
  potionChecks.turnOf(h, player); h.useItem(player, player, getItem(glass));
  assert(player.effects.filter(effect => effect.name === "Glass Sand (eaten)").length === 2 && player.effects.find(effect => effect.name === "Glass Sand (eaten)").expiresTurn === player.turns + 3, "The same ingredient again refreshes its buff (P21)");
  potionChecks.turnOf(h, player); h.useItem(player, player, getItem(mint));
  assert(player.effects.some(effect => effect.name === "Meadowmint (eaten)") && player.effects.some(effect => effect.name === "Glass Sand (eaten)"), "A different ingredient adds up");
  // Lamia Venom is only in poisons: weapon hits carry Deathcap Essence at a third.
  potionChecks.turnOf(h, player); h.useItem(player, player, getItem(venom));
  const carried = player.effects.find(effect => effect.type === "weapon-poison");
  assert(carried && carried.poisonId === "consumable-deathcap-essence", "A poison-only ingredient puts its poison on your weapon");
  const foe = foes[0];
  h.applyNewClassWeaponEffects(player, foe, h.attackLoadout(player).primary, 10, []);
  assert(foe.effects.some(effect => effect.type === "class-poison" && effect.amount === Math.round(35 / 3)), "Each weapon hit poisons at a third of Deathcap Essence's strength");
  // Level 4: half and one turn longer; Level 6: two-thirds and the special effects.
  assert(AlchemyRules.assimilation(glass, 4, getItem).factor === 0.5 && AlchemyRules.assimilation(glass, 4, getItem).durationTurns === 4, "Level 4: half as strong, a turn longer");
  assert(!AlchemyRules.assimilation(feather, 5, getItem).effects.some(effect => effect.type === "turn-start-ap") && AlchemyRules.assimilation(feather, 6, getItem).effects.some(effect => effect.type === "turn-start-ap"), "Level 6: special effects such as Haste's AP");
  assert(Math.abs(AlchemyRules.assimilation(glass, 6, getItem).factor - 2 / 3) < 1e-9, "Level 6: two-thirds as strong");
  // Level 5: a 25% chance the ingredient isn't used up.
  ({ battle, player } = await potionChecks.battle(h, ["Stone Giant"], { level: 60, inventory: [glass, glass], classLevels: { assimilator: 5 } }));
  await potionChecks.withRandom(() => 0.2, async () => { potionChecks.turnOf(h, player); h.useItem(player, player, getItem(glass)); });
  assert(potionChecks.count(glass) === 2, "Level 5: a 25% roll keeps the ingredient");
  await potionChecks.withRandom(() => 0.3, async () => { potionChecks.turnOf(h, player); h.useItem(player, player, getItem(glass)); });
  assert(potionChecks.count(glass) === 1, "otherwise it is used up");
  // Level 7: Devour: no AP, twice as long, once per battle.
  ({ battle, player } = await potionChecks.battle(h, ["Stone Giant"], { level: 60, inventory: [glass, glass], classLevels: { assimilator: 7 } }));
  potionChecks.turnOf(h, player);
  const devour = h.skillsOf(player).find(skill => skill.id === "devour");
  assert(devour && h.useSkill(player, h.classSkillKey(devour)) && player.effects.some(effect => effect.type === "devour-ready"), "Devour readies the next ingredient");
  await potionChecks.withRandom(() => 0.9, async () => { h.useItem(player, player, getItem(glass)); });
  const devoured = player.effects.find(effect => effect.name === "Glass Sand (eaten)");
  assert(battle.ap === 5 && devoured.expiresTurn === player.turns + 8 && !player.effects.some(effect => effect.type === "devour-ready"), "Devoured: no AP, and twice as long (4 × 2 turns)");
  assert(!h.useSkill(player, h.classSkillKey(devour)), "Devour once per battle");
  return { checks: count() };
}

// Sections 4, 5, P10–P12, P26, P30: Forage (D) costs an hour and the zone's encounter roll for an hour of travel,
// finds the zone's own ingredients (the river's beside one) 45% of the time, plus the Alchemist's bonus; not in towns,
// on roads or in the uncharted regions; Keen Harvest makes the next one sure. Enemies drop their own ingredients.
async function checkForaging() {
  const { assert, count } = potionChecks.counter(), I = potionChecks.ingredient;
  await waitForSourceFiles();
  const castle = potionChecks.place("alder-vale", "Castle city");
  // Deep Forest of the Alder Vale (a wild zone).
  const deep = { x: 0, y: 0 };
  let found = null;
  for (let y = 40; y < 960 && !found; y += 20) for (let x = 40; x < 1560 && !found; x += 20) if (WorldRules.zoneAt({ x, y }, "alder-vale") === "Deep Forest" && !WorldRules.nearRiver({ x, y }, "alder-vale")) found = { x, y };
  potionChecks.hero({ level: 20, worldState: { ...potionChecks.at("alder-vale", found), position: found } });
  document.getElementById("map-open-button").click();
  await potionChecks.wait(150);
  WorldMap.render();
  const button = document.getElementById("map-forage");
  assert(!button.hidden && !button.disabled && button.dataset.mapHotkey === "D", "Forage (D) in a wild zone");
  assert(JSON.stringify(AlchemyRules.forageTable("alder-vale", "Deep Forest").map(entry => entry.id).sort()) === JSON.stringify([I("Spirit Dew"), I("Oakbark"), I("Treant Heartwood"), I("Nightshade")].sort()), "Its table: the zone's own ingredients");
  // An hour, the encounter roll for an hour of travel, and a find on a low roll.
  const minutes = worldState.minutes, rolls = [];
  const roll = EncounterRules.roll;
  EncounterRules.roll = (zone, context) => { rolls.push({ zone, context }); return null; };
  try {
    await potionChecks.withRandom(() => 0.1, async () => { WorldMap.forage(); });
  } finally { EncounterRules.roll = roll; }
  assert(worldState.minutes === minutes + 60, "Foraging takes an hour");
  assert(rolls.length === 1 && rolls[0].zone === "Deep Forest" && Math.abs(rolls[0].context.encounterChanceMultiplier - 60 / WorldRules.ENCOUNTER_MINUTES) < 1e-9, "and rolls the zone's encounter chance for an hour of travel");
  const pack = inventory.filter(id => AlchemyRules.isIngredientId(id));
  assert(pack.length >= 1 && pack.every(id => AlchemyRules.forageTable("alder-vale", "Deep Forest").some(entry => entry.id === id)) && alchemy.foraged === pack.length, "A find goes into your pack and counts as foraged");
  await potionChecks.withRandom(() => 0.5, async () => { EncounterRules.roll = () => null; try { WorldMap.forage(); } finally { EncounterRules.roll = roll; } });
  assert(inventory.filter(id => AlchemyRules.isIngredientId(id)).length === pack.length && /nothing/.test(document.getElementById("map-status").textContent), "On a 50% roll nothing is found (45% to find)");
  // The Alchemist's bonus: Level 2 finds on that same roll (55%).
  potionChecks.hero({ level: 20, classLevels: { alchemist: 2 }, worldState: { ...potionChecks.at("alder-vale", found), position: found } });
  WorldMap.render();
  await potionChecks.withRandom(() => 0.5, async () => { EncounterRules.roll = () => null; try { WorldMap.forage(); } finally { EncounterRules.roll = roll; } });
  assert(inventory.some(id => AlchemyRules.isIngredientId(id)), "An Alchemist (Level 2) finds more often");
  // Keen Harvest (Alchemist 5): the next forage is sure.
  potionChecks.hero({ level: 20, classLevels: { alchemist: 5 }, worldState: { ...potionChecks.at("alder-vale", found), position: found } });
  const skill = calculateClassEffects().skills.find(entry => entry.id === "keen-harvest");
  activateSkill(skill);
  assert(alchemy.keenHarvestReady, "Keen Harvest readies the next forage");
  await potionChecks.withRandom(() => 0.99, async () => { EncounterRules.roll = () => null; try { WorldMap.forage(); } finally { EncounterRules.roll = roll; } });
  assert(inventory.some(id => AlchemyRules.isIngredientId(id)) && !alchemy.keenHarvestReady, "and it is sure to find something");
  // Not in a town, on a road or in uncharted land; by a river the river's own two join.
  potionChecks.hero({ level: 20, worldState: potionChecks.at("alder-vale", castle) });
  WorldMap.render();
  assert(document.getElementById("map-forage").hidden, "No foraging in a town");
  assert(!AlchemyRules.canForage("alder-vale", "Road") && !AlchemyRules.canForage("uncharted-steppe", "Uncharted land") && !AlchemyRules.canForage("alder-vale", "Open Sea"), "Nor on a road, at sea or in the uncharted regions");
  assert(!AlchemyRules.canForage("alder-vale", "Town", true) && !AlchemyRules.canForage("alder-vale", "Road", true), "not even where a river runs through a town or by a road");
  const plains = AlchemyRules.forageTable("alder-vale", "Plains - near Town", true).map(entry => entry.id);
  assert(plains.includes(I("Riverreed")) && plains.includes(I("Clearwater Moss")) && plains.includes(I("Meadowmint")), "By a river, Riverreed and Clearwater Moss join the zone's table");
  // Rarer ingredients are rarer finds: weights by rarity.
  const table = AlchemyRules.forageTable("alder-vale", "Deep Forest");
  assert(table.find(entry => entry.id === I("Spirit Dew")).weight > table.find(entry => entry.id === I("Treant Heartwood")).weight, "Common finds outweigh uncommon ones");
  document.querySelector("#map-modal .modal-close")?.click();
  return { checks: count() };
}

// Sections 4.3, P12: about a quarter of kills drop one of the enemy's own ingredients; Empowered and Sparkling enemies
// half the time, twice; bosses always one of their two and maybe both.
async function checkIngredientDrops() {
  const { assert, count } = potionChecks.counter(), h = await potionChecks.hooks();
  const loot = async (enemies, random, setup = () => {}) => {
    const { battle, foes } = await potionChecks.battle(h, enemies, { level: 60 });
    setup(foes);
    for (const foe of foes) h.setHp(foe, 0);
    battle.lootItems = [];
    await potionChecks.withRandom(random, async () => { h.awardDefeatedEnemyExperience(); });
    return battle.lootItems.filter(id => AlchemyRules.isIngredientId(id));
  };
  const goblin = h.definitions().find(enemy => enemy.name === "Goblin").id;
  const own = AlchemyRules.enemyIngredients(goblin);
  const low = await loot(["Goblin"], () => 0.2);
  assert(low.length === 1 && own.includes(low[0]), "A Goblin drops one of its own ingredients on a 25% roll");
  assert(!(await loot(["Goblin"], () => 0.3)).length, "and nothing on a 30% roll");
  const elite = await loot(["Goblin"], () => 0.4, foes => { foes[0].empowered = true; });
  assert(elite.length === 2, "An Empowered enemy: 50%, rolled twice");
  const boss = h.definitions().find(enemy => enemy.name === "Great Imp Ji'ruan");
  const bossLoot = AlchemyRules.enemyDrops(boss.id, {}, () => 0.9);
  assert(bossLoot.length === 1 && AlchemyData.BOSS_DROPS[boss.id].includes(bossLoot[0]), "A boss always drops one of its two");
  assert(AlchemyRules.enemyDrops(boss.id, {}, () => 0.1).length === 2, "and on a 35% roll both");
  return { checks: count() };
}

// Sections 9, 10, P18, P19, P30: the Alchemist (Level 20, 10 potions brewed; 5 levels) and the Assimilator (Level 10,
// 25 ingredients foraged; 7 levels), built like the other classes; Ingredient Lore shows every potion an ingredient goes
// into without revealing unknown recipes; the Assimilator's Nature resistance.
async function checkAlchemyClasses() {
  const { assert, count } = potionChecks.counter(), h = await potionChecks.hooks(), I = potionChecks.ingredient;
  await waitForSourceFiles();
  const alchemist = getClass("alchemist"), assimilator = getClass("assimilator");
  assert(alchemist && alchemist.maxLevel === 5 && alchemist.requirements.level === 20 && alchemist.requirements.other?.type === "potions-brewed" && alchemist.requirements.other.count === 10, "Alchemist: 5 levels, Level 20 and 10 potions brewed (P18)");
  assert(assimilator && assimilator.maxLevel === 7 && assimilator.requirements.level === 10 && assimilator.requirements.other?.type === "ingredients-foraged" && assimilator.requirements.other.count === 25, "Assimilator: 7 levels, Level 10 and 25 ingredients foraged (P19)");
  // Built like the others (P30): Level 1 a passive, a skill as the capstone, one gain per level.
  const kinds = definition => definition.levels.map(rank => rank.effects.some(effect => effect.type === "skill") ? "skill" : rank.effects.some(effect => effect.type === "passive") ? "passive" : "bonus");
  assert(kinds(alchemist).join() === "passive,bonus,bonus,bonus,skill" && kinds(assimilator)[0] === "passive" && kinds(assimilator).at(-1) === "skill", "A passive at Level 1 and a skill as the capstone");
  // Requirements: met by the counts.
  potionChecks.hero({ level: 20, alchemy: { brewed: 9, foraged: 24 } });
  assert(!otherClassRequirementMet(alchemist) && !otherClassRequirementMet(assimilator), "9 brewed and 24 foraged aren't enough");
  alchemy.brewed = 10; alchemy.foraged = 25;
  assert(otherClassRequirementMet(alchemist) && otherClassRequirementMet(assimilator), "10 brewed and 25 foraged are");
  // Ingredient Lore: only with the Alchemist; known recipes show their other ingredients, unknown ones don't.
  const mint = I("Meadowmint");
  potionChecks.hero({ level: 20, inventory: [mint] });
  assert(!/Ingredient Lore/.test(itemDetailMarkup(getItem(mint))), "Without the Alchemist, no lore");
  potionChecks.hero({ level: 20, inventory: [mint], classLevels: { alchemist: 1 } });
  const lore = itemDetailMarkup(getItem(mint));
  assert(/Ingredient Lore/.test(lore) && /Potion of Lesser Health/.test(lore) && /Draught of Vigor/.test(lore) && /Mending Draught/.test(lore), "The Alchemist sees every potion Meadowmint goes into");
  assert(/Potion of Lesser Health<\/strong> · with Redcap Berry and Riverreed/.test(lore), "A known recipe shows its other ingredients");
  assert(/Draught of Vigor<\/strong> · <em>recipe unknown<\/em>/.test(lore) && !/Wolf Fang/.test(lore.split("Ingredient Lore")[1]), "An unknown recipe shows only that it exists");
  // The Assimilator's Level 3: +10% Nature resistance (poisons included).
  const { player } = await potionChecks.battle(h, ["Goblin"], { level: 30, classLevels: { assimilator: 2 } });
  const plain = h.resistance(player, "nature");
  const { player: hardened } = await potionChecks.battle(h, ["Goblin"], { level: 30, classLevels: { assimilator: 3 } });
  assert(h.resistance(hardened, "nature") === plain + 10, "Assimilator Level 3: +10% Nature resistance");
  return { checks: count() };
}

// Section 11, P22–P25: a Collect quest asks for an ingredient of the guild's region or a neighbour's, at or below your
// level band's rarity, in a quantity by rarity; it pays like Travel, Escort, Search or Deliver by that rarity (never
// like Cull or Hunt); progress counts any copies in the pack; it is never completed by itself, only handed in at the
// guild that offered it, where the ingredients leave the pack and the silver and reputation are paid.
async function checkCollectQuests() {
  const { assert, count } = potionChecks.counter();
  await waitForSourceFiles();
  const goTo = (regionId, settlementId) => {
    worldState.regionId = regionId; WorldRules.setActiveRegion(regionId);
    const place = WorldRules.settlementsForRegion(regionId).find(entry => entry.id === settlementId);
    worldState.position = { x: place.x, y: place.y }; worldState.destination = null; worldState.route = [];
    Guild.onPositionChange();
    return place;
  };
  Combat.restore(null);
  const character = defaultCharacter();
  character.raceId = "human"; character.level = 20; character.worldState.guild.reputation = 500;
  applyCharacter(normalizeCharacter(character));
  WorldMap.setAmbientEnabled(false);
  goTo("alder-vale", "alderkeep");
  assert(GuildQuestRules.TYPES.includes("collect") && GuildQuestRules.NAMES.collect === "Collect", "Collect is a quest type");
  // Many offers: always a nearby ingredient, never rarer than the band, the quantity and pay by rarity.
  const nearby = new Set(["alder-vale", ...GuildQuestRules.adjacentRegions("alder-vale")].flatMap(id => Alchemy.regionIngredients(id)));
  const level = GuildQuestRules.questLevel(20, "alder-vale");
  for (let index = 0; index < 40; index++) {
    const offer = Guild.makeOffer("collect"), ingredient = AlchemyRules.ingredient(offer.ingredientId);
    assert(offer.type === "collect" && nearby.has(offer.ingredientId), "The ingredient is found in this region or a neighbour's: " + ingredient?.name);
    assert(["common", "uncommon"].includes(ingredient.rarity), "At Level 20 (uncommon band) no rarer ingredient is asked for");
    assert(offer.required === { common: 6, uncommon: 4, rare: 3, epic: 2, legendary: 1 }[ingredient.rarity], "6 of a common ingredient, 4 uncommon, 3 rare, 2 epic, 1 legendary");
    const pays = GuildQuestRules.reward(GuildQuestRules.COLLECT_PAYS_AS[ingredient.rarity], level, calculateClassEffects());
    assert(offer.reward.silver === pays.silver && offer.reward.reputation === pays.reputation, "It pays like " + GuildQuestRules.COLLECT_PAYS_AS[ingredient.rarity]);
  }
  // At Lakespire, with rare and epic ingredients in reach, a Level 20 hero is still asked for none of them.
  goTo("mirrormere-basin", "lakespire");
  const lakeside = new Set(["mirrormere-basin", ...GuildQuestRules.adjacentRegions("mirrormere-basin")].flatMap(id => Alchemy.regionIngredients(id)));
  assert([...lakeside].some(id => ["rare", "epic"].includes(AlchemyRules.ingredient(id).rarity)), "Rarer ingredients are found around Lakespire");
  for (let index = 0; index < 120; index++) {
    const offer = Guild.makeOffer("collect");
    assert(["common", "uncommon"].includes(AlchemyRules.ingredient(offer.ingredientId).rarity), "Around Lakespire too, none above the band");
  }
  goTo("alder-vale", "alderkeep");
  // The rarity cap with every ingredient a candidate: never above the band.
  const everything = AlchemyRules.INGREDIENTS.map(entry => entry.id);
  for (let index = 0; index < 200; index++) {
    const pick = AlchemyRules.collectPick({ candidates: everything, maxRarity: "uncommon", level: 20 });
    assert(["common", "uncommon"].includes(AlchemyRules.ingredient(pick.id).rarity), "With every ingredient possible, none above the band is asked for");
  }
  assert(["common", "uncommon", "rare", "epic", "legendary"].every(rarity => GuildQuestRules.collectReward(rarity, level, {}).silver < GuildQuestRules.rewardTable(level, {}).cull.silver), "Never as much as Cull or Hunt");
  assert(GuildQuestRules.COLLECT_PAYS_AS.common === "travel" && GuildQuestRules.COLLECT_PAYS_AS.uncommon === "escort" && GuildQuestRules.COLLECT_PAYS_AS.rare === "search" && GuildQuestRules.COLLECT_PAYS_AS.epic === "deliver", "Common like Travel, uncommon Escort, rare Search, epic and legendary Deliver (P22)");
  // Take one: on the board, the notice and Accept.
  WorldMap.open(); WorldMap.render();
  document.getElementById("map-guild").click();
  const offer = Guild.makeOffer("collect");
  const board = Guild.boardFor(); board.offers = board.offers.filter(entry => entry.type !== "collect"); board.offers.push(offer);
  document.querySelector('#guild-banner [data-guild-spot="board"]').dispatchEvent(new MouseEvent("click", { bubbles: true }));
  document.querySelector(`[data-guild-notice="${board.offers.length - 1}"]`).click();
  document.querySelector("#guild-accept").click();
  const quest = worldState.guild.quest;
  assert(quest?.type === "collect" && quest.ingredientId === offer.ingredientId, "A Collect quest can be accepted");
  // Progress counts the pack; enough copies don't complete it; elsewhere they can't be handed in.
  inventory.push(...Array(quest.required).fill(quest.ingredientId));
  assert(!quest.complete && Guild.markers().some(marker => /Return to Alderkeep/.test(marker.label)), "Holding enough doesn't complete it; the map points back to the guild");
  const host = WorldRules.settlementAt(worldState.position, worldState.regionId);
  goTo("verdant-reach", "greenwatch");
  assert(!Guild.handInReady(quest, WorldRules.settlementAt(worldState.position, worldState.regionId)), "Another guild doesn't take it (P23)");
  goTo("alder-vale", "alderkeep");
  inventory.splice(inventory.indexOf(quest.ingredientId), 1);
  assert(!Guild.handInReady(quest, host), "Without enough copies it can't be handed in");
  inventory.push(quest.ingredientId, quest.ingredientId);
  // Handed in at its guild: the ingredients leave the pack (one extra stays), silver and reputation are paid.
  const silver = ShopRules.walletValue(currencies), reputation = worldState.guild.reputation;
  Guild.useCounter();
  assert(!worldState.guild.quest && inventory.filter(id => id === quest.ingredientId).length === 1, "Handing it in takes exactly the copies asked for");
  assert(ShopRules.walletValue(currencies) >= silver + quest.reward.silver && worldState.guild.reputation === reputation + quest.reward.reputation, "and pays its silver and reputation");
  Guild.close(); WorldMap.close?.();
  // A save keeps the quest's ingredient.
  const saved = WorldRules.normalize({ ...JSON.parse(JSON.stringify(worldState)), guild: { ...JSON.parse(JSON.stringify(worldState.guild)), quest: { ...offer, taken: false } } });
  assert(saved.guild.quest?.type === "collect" && saved.guild.quest.ingredientId === offer.ingredientId && saved.guild.quest.required === offer.required, "Collect quests save and load");
  assert(!WorldRules.normalize({ ...JSON.parse(JSON.stringify(worldState)), guild: { quest: { ...offer, ingredientId: null } } }).guild.quest, "A Collect quest without its ingredient is dropped");
  return { checks: count() };
}

// Section 13: everything new saves and loads; older saves load with the starting recipes only and keep their potions.
async function checkAlchemySaves() {
  const { assert, count } = potionChecks.counter();
  await waitForSourceFiles();
  const castle = potionChecks.place("alder-vale", "Castle city");
  const vigor = "consumable-draught-of-vigor", mint = potionChecks.ingredient("Meadowmint"), health = "consumable-potion-of-health";
  potionChecks.hero({ level: 40, money: 100000, inventory: [vigor, mint, health], alchemy: { known: [...AlchemyRules.STARTING_RECIPES, vigor], brewed: 7, foraged: 31, keenHarvestReady: true }, worldState: potionChecks.at("alder-vale", castle) });
  Apothecary.open();
  const stock = JSON.stringify(Apothecary.stock());
  Apothecary.close();
  const payload = backupPayload();
  applyCharacter(normalizeCharacter(defaultCharacter()));
  assert(!alchemy.known.includes(vigor) && alchemy.brewed === 0, "A fresh character has none of it");
  applyCharacter(normalizeCharacter(payload.character));
  assert(alchemy.known.includes(vigor) && alchemy.brewed === 7 && alchemy.foraged === 31 && alchemy.keenHarvestReady === true, "Known recipes, the counts and Keen Harvest come back");
  assert(inventory.includes(vigor) && inventory.includes(mint) && inventory.includes(health), "Potions and ingredients stay in the pack");
  Apothecary.open();
  assert(JSON.stringify(Apothecary.stock()) === stock, "The Apothecary's stock and recipe offers come back");
  Apothecary.close();
  // An older save: no alchemy record, Health Potions in the pack.
  const old = JSON.parse(JSON.stringify(payload.character));
  delete old.alchemy;
  applyCharacter(normalizeCharacter(old));
  assert(JSON.stringify([...alchemy.known].sort()) === JSON.stringify([...AlchemyRules.STARTING_RECIPES].sort()) && alchemy.brewed === 0, "An older save starts with the two Lesser recipes, not every recipe");
  assert(inventory.includes(health), "and keeps its Health Potions");
  // Unknown recipe ids are dropped; the starting ones are always there.
  applyCharacter(normalizeCharacter({ ...old, alchemy: { known: ["consumable-nonsense", vigor], brewed: -4 } }));
  assert(alchemy.known.includes(vigor) && !alchemy.known.includes("consumable-nonsense") && AlchemyRules.STARTING_RECIPES.every(id => alchemy.known.includes(id)) && alchemy.brewed === 0, "A broken record is cleaned on load");
  // In battle: potion buffs and eaten ingredients are in the battle's saved state.
  const h = await potionChecks.hooks();
  const { player } = await potionChecks.battle(h, ["Goblin"], { level: 40, inventory: [vigor] });
  potionChecks.turnOf(h, player); h.useItem(player, player, getItem(vigor));
  const battleSave = Combat.serialize();
  Combat.restore(null); Combat.restore(battleSave);
  assert(h.getBattle()?.actors.find(actor => actor.id === "player")?.effects.some(effect => effect.name === "Draught of Vigor"), "An active potion buff survives saving in battle");
  Combat.restore(null);
  return { checks: count() };
}

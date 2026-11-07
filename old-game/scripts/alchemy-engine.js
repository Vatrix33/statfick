/* The Potion Rework's rules (port task 9; decisions P1–P31 in engine/Docs/Engine-Plan.md, "Decisions for task 9"; the
   approved content is engine/Docs/Port-Tasks/9_Potion_Rework/draft.md). Pure rules, no page: recipes, foraging, the
   ingredients enemies drop, the Apothecary's stock and recipe offers, Collect quests' ingredients, an Assimilator's
   raw-ingredient buffs and the bosses' half-strength poisons. Data in alchemy-data.js (loaded before this); the potions
   and poisons themselves are consumables (objects/consumables.json: "potion" or "poison", "alchemy" for the new ones). */
(root => {
  "use strict";
  const D = root.AlchemyData;
  const RARITIES = ["common", "uncommon", "rare", "epic", "legendary"];
  const rank = rarity => Math.max(0, RARITIES.indexOf(rarity));
  const INGREDIENTS = new Map(D.INGREDIENTS.map(entry => [entry.id, entry]));
  const RECIPES = new Map(D.RECIPES.map(entry => [entry.id, entry]));
  const isIngredientId = id => typeof id === "string" && INGREDIENTS.has(id);
  const unit = random => { const value = Number(random()); return Number.isFinite(value) ? Math.min(1 - Number.EPSILON, Math.max(0, value)) : 0; };

  // Forage (P10, P11, P31): one hour, the zone's encounter roll; finds something 45% of the time.
  const FORAGE = Object.freeze({ MINUTES: 60, FIND_PERCENT: 45, WEIGHTS: Object.freeze({ common: 10, uncommon: 5, rare: 2, epic: 1, legendary: 0.5 }) });
  // Zones where nothing can be gathered (P11): settlements, roads, sites, water and the regions empty until the port.
  const NO_FORAGE = new Set(["Town", "Road", "Open Sea", "Open Water", "Uncharted land", "Vampire Citadel"]);
  // Enemy drops (P12): a quarter of kills; Empowered and Sparkling half, twice; bosses one of their two and maybe both.
  const DROPS = Object.freeze({ CHANCE: 25, ELITE_CHANCE: 50, ELITE_ROLLS: 2, BOSS_SECOND: 35 });
  // The Apothecary (P3, P13, P14, P15, P16): random potions and poisons, ingredients, three recipes at ten times the price.
  const APOTHECARY = Object.freeze({
    POTIONS: Object.freeze({ castle: 8, town: 5 }), POTION_QUANTITY: Object.freeze({ castle: [3, 6], town: [2, 4] }),
    COMMON_QUANTITY: 10, REGIONAL: Object.freeze({ castle: 5, town: 3 }),
    REGIONAL_QUANTITY: Object.freeze({ uncommon: [2, 5], rare: [2, 5], epic: [1, 2], legendary: [1, 1] }),
    RECIPES: 3, RECIPE_PRICE_FACTOR: 10,
  });
  // Collect quests (P22–P25): how many, and the reward factor, by the ingredient's rarity.
  const COLLECT = Object.freeze({
    QUANTITY: Object.freeze({ common: 6, uncommon: 4, rare: 3, epic: 2, legendary: 1 }),
    FACTOR: Object.freeze({ common: 1, uncommon: 1.1, rare: 1.2, epic: 1.35, legendary: 1.35 }),
  });
  // Raw Assimilation (P20, P21, P30): strength by Assimilator level, duration by the ingredient's rarity.
  const ASSIMILATION = Object.freeze({
    STRENGTH: Object.freeze([[6, 2 / 3], [4, 1 / 2], [1, 1 / 3]]), LASTS: Object.freeze({ common: 2, uncommon: 3, rare: 3, epic: 4, legendary: 4 }),
    LONGER_FROM: 4, SPECIAL_FROM: 6, KINDS: 2,
  });

  const ingredient = id => INGREDIENTS.get(id) || null;
  const recipeFor = productId => RECIPES.get(productId) || null;
  const recipesUsing = ingredientId => D.RECIPES.filter(recipe => recipe.ingredients.includes(ingredientId));
  // The recipe three ingredients make, whatever their order (P31: "the order doesn't matter").
  function matchRecipe(ids) {
    if (!Array.isArray(ids) || ids.length !== 3 || ids.some(id => !isIngredientId(id))) return null;
    const key = [...ids].sort().join("|");
    return D.RECIPES.find(recipe => [...recipe.ingredients].sort().join("|") === key) || null;
  }
  // Known recipes: valid product ids, the two starting ones always among them (P4).
  function normalizeKnown(saved) {
    const known = new Set(D.STARTING_RECIPES);
    for (const id of Array.isArray(saved) ? saved : []) if (RECIPES.has(id)) known.add(id);
    return [...known];
  }

  /* ---------- Foraging ---------- */
  const baseZone = zone => String(zone || "").replace(/ - near Town$/, "");
  // A zone's forage table: its own ingredients, and the rivers' two where a river runs by; "seldom" halves a weight.
  function forageTable(regionId, zone, nearRiver = false) {
    if (NO_FORAGE.has(baseZone(zone))) return [];
    const weights = new Map();
    const add = (id, seldom) => {
      const entry = ingredient(id);
      if (!entry) return;
      const weight = FORAGE.WEIGHTS[entry.rarity] * (seldom ? 0.5 : 1);
      weights.set(id, Math.max(weights.get(id) || 0, weight));
    };
    for (const entry of D.FORAGE[regionId]?.[baseZone(zone)] || []) add(entry.id, entry.seldom);
    if (nearRiver || baseZone(zone) === "River") for (const id of D.RIVER) add(id, false);
    return [...weights].map(([id, weight]) => ({ id, weight }));
  }
  const canForage = (regionId, zone, nearRiver = false) => forageTable(regionId, zone, nearRiver).length > 0;
  // One forage: { found, id, quantity }. findPercent adds the Alchemist's bonus; rareWeight scales rare and rarer
  // weights (Keen Harvest); sure always finds (Keen Harvest); extraOne gives one more of a common or uncommon find.
  function forage({ regionId, zone, nearRiver = false, findBonus = 0, rareWeight = 1, sure = false, extraOne = false, random = Math.random } = {}) {
    const table = forageTable(regionId, zone, nearRiver);
    if (!table.length) return { found: false, id: null, quantity: 0 };
    const chance = Math.min(100, FORAGE.FIND_PERCENT + Math.max(0, Number(findBonus) || 0));
    if (!sure && unit(random) * 100 >= chance) return { found: false, id: null, quantity: 0 };
    const weighted = table.map(entry => ({ ...entry, weight: entry.weight * (rank(ingredient(entry.id).rarity) >= 2 ? Math.max(0, Number(rareWeight) || 1) : 1) }));
    const total = weighted.reduce((sum, entry) => sum + entry.weight, 0);
    let roll = unit(random) * total, chosen = weighted.at(-1);
    for (const entry of weighted) { roll -= entry.weight; if (roll < 0) { chosen = entry; break; } }
    const rarity = ingredient(chosen.id).rarity;
    let quantity = rarity === "common" ? 1 + Math.floor(unit(random) * 2) : 1;
    if (extraOne && rank(rarity) <= 1) quantity += 1;
    return { found: true, id: chosen.id, quantity };
  }

  /* ---------- Enemy drops ---------- */
  function pickWeighted(entries, random) {
    const weighted = entries.map(entry => ({ id: entry.id, weight: FORAGE.WEIGHTS[ingredient(entry.id)?.rarity || "common"] * (entry.seldom ? 0.5 : 1) }));
    const total = weighted.reduce((sum, entry) => sum + entry.weight, 0);
    let roll = unit(random) * total;
    for (const entry of weighted) { roll -= entry.weight; if (roll < 0) return entry.id; }
    return weighted.at(-1)?.id || null;
  }
  // The ingredients one defeated enemy leaves (P12).
  function enemyDrops(enemyId, { elite = false } = {}, random = Math.random) {
    const boss = D.BOSS_DROPS[enemyId];
    if (boss) {
      const first = Math.floor(unit(random) * boss.length), result = [boss[first]];
      if (unit(random) * 100 < DROPS.BOSS_SECOND) result.push(boss[1 - first]);
      return result;
    }
    const own = D.DROPS[enemyId] || [];
    if (!own.length) return [];
    const result = [];
    for (let roll = 0; roll < (elite ? DROPS.ELITE_ROLLS : 1); roll++) if (unit(random) * 100 < (elite ? DROPS.ELITE_CHANCE : DROPS.CHANCE)) result.push(pickWeighted(own, random));
    return result.filter(Boolean);
  }
  const enemyIngredients = enemyId => [...new Set([...(D.BOSS_DROPS[enemyId] || []), ...(D.DROPS[enemyId] || []).map(entry => entry.id)])];

  /* ---------- A region's own ingredients (the Apothecary's regional stock, Collect quests) ---------- */
  // Foraged anywhere in the region, or dropped by the enemies met there (their ids from the encounter tables).
  function regionIngredients(regionId, enemyIds = []) {
    const ids = new Set();
    for (const entries of Object.values(D.FORAGE[regionId] || {})) for (const entry of entries) ids.add(entry.id);
    for (const enemyId of enemyIds) for (const id of (D.DROPS[enemyId] || []).map(entry => entry.id)) ids.add(id);
    return [...ids].filter(isIngredientId);
  }
  const between = ([low, high], random) => low + Math.floor(unit(random) * (high - low + 1));
  // The ingredient menu (P13): every common ingredient, then a few of the region's rarer ones.
  function ingredientStock({ castle = false, regional = [], random = Math.random } = {}) {
    const commons = D.INGREDIENTS.filter(entry => entry.rarity === "common").map(entry => ({ id: entry.id, quantity: APOTHECARY.COMMON_QUANTITY }));
    const rarer = regional.map(ingredient).filter(entry => entry && entry.rarity !== "common");
    const picked = [];
    const pool = [...rarer];
    while (picked.length < APOTHECARY.REGIONAL[castle ? "castle" : "town"] && pool.length) picked.push(pool.splice(Math.floor(unit(random) * pool.length), 1)[0]);
    picked.sort((a, b) => rank(a.rarity) - rank(b.rarity) || a.name.localeCompare(b.name));
    return [...commons, ...picked.map(entry => ({ id: entry.id, quantity: between(APOTHECARY.REGIONAL_QUANTITY[entry.rarity], random) }))];
  }
  // The recipe menu (P14–P16): up to three unknown recipes, leaning toward the level band's rarity.
  function recipeOffers({ known = [], level = 1, productRarity = () => "common", random = Math.random } = {}) {
    const knownSet = new Set(known);
    const pool = D.RECIPES.filter(recipe => !knownSet.has(recipe.id)).map(recipe => ({ id: recipe.id, rarity: productRarity(recipe.id) }));
    return root.ShopRules.itemStock(pool, Math.min(APOTHECARY.RECIPES, pool.length), level, () => unit(random)).map(entry => entry.id);
  }
  const recipePrice = productPrice => Math.max(0, Math.round(Number(productPrice) || 0)) * APOTHECARY.RECIPE_PRICE_FACTOR;

  /* ---------- Collect quests ---------- */
  // An ingredient of the guild's region or a neighbour's, at or below the level band's rarity (P25), and how many.
  function collectPick({ candidates = [], maxRarity = "common", level = 1, random = Math.random } = {}) {
    const pool = [...new Set(candidates)].map(ingredient).filter(entry => entry && rank(entry.rarity) <= rank(maxRarity));
    if (!pool.length) return null;
    const [chosen] = root.ShopRules.itemStock(pool, 1, level, () => unit(random));
    return chosen ? { id: chosen.id, quantity: COLLECT.QUANTITY[chosen.rarity] } : null;
  }
  const collectFactor = rarity => COLLECT.FACTOR[rarity] ?? 1;

  /* ---------- Poisons on bosses (P8) ---------- */
  // Half strength: damage, reductions and chances halved, durations halved; rounded up (away from zero).
  const halfUp = value => Math.sign(value) * Math.ceil(Math.abs(value) / 2);
  function halved(effects) {
    return effects.map(effect => {
      const next = { ...effect };
      if (Number.isFinite(next.amount) && next.type !== "root-ap") next.amount = halfUp(next.amount);
      if (Number.isFinite(next.percentMaxHp)) next.percentMaxHp = halfUp(next.percentMaxHp);
      if (Number.isFinite(next.durationTurns)) next.durationTurns = Math.max(1, halfUp(next.durationTurns));
      return next;
    });
  }

  /* ---------- Raw Assimilation (P20, P21, P30) ---------- */
  const BUFFS = new Set(["stat-percent", "all-stats-percent", "defense-percent", "spell-power-percent", "spell-cost-percent", "incoming-weapon-damage-percent",
    "incoming-spell-damage-percent", "incoming-spell-miss-chance", "healing-received-percent", "element-resistance-percent", "crit-chance-percent", "crit-damage-percent",
    "armor-pierce-percent", "outgoing-attack-damage-percent", "elite-damage-percent", "weapon-hit-percent", "turn-start-restore-percent", "heal", "restore-mana", "barrier-percent"]);
  const SPECIALS = new Set(["turn-start-ap", "debuff-immune", "control-immune", "rise-again-percent", "undying", "cure"]);
  const PROTECTIVE = new Set(["incoming-weapon-damage-percent", "incoming-spell-damage-percent"]);
  const strengthAt = level => (ASSIMILATION.STRENGTH.find(([from]) => level >= from) || [1, 0])[1];
  // A buff effect's kind ("two Strength bonuses count once") and its size.
  function kindOf(effect) {
    if (effect.type === "heal" || effect.type === "turn-start-restore-percent" && effect.hp) return "restore:hp";
    if (effect.type === "restore-mana" || effect.type === "turn-start-restore-percent" && effect.mana) return "restore:mana";
    return [effect.type, effect.target || "", [...(effect.elements || [])].sort().join("+")].join(":");
  }
  function sizeOf(effect) {
    if (effect.type === "heal" || effect.type === "restore-mana") return Math.max(0, Number(effect.maxPercent) || 0);
    if (effect.type === "turn-start-restore-percent") return (Number(effect.hp || effect.mana) || 0) * Math.max(1, Number(effect.durationTurns) || 1);
    return Math.abs(Number(effect.amount) || 0);
  }
  const isBuff = effect => BUFFS.has(effect.type) && (PROTECTIVE.has(effect.type) ? Number(effect.amount) < 0 : effect.type === "heal" || effect.type === "restore-mana" || effect.type === "turn-start-restore-percent" || Number(effect.amount) > 0);
  // What eating one ingredient gives at an Assimilator level: { effects, durationTurns, poisonId, kinds }.
  function assimilation(ingredientId, classLevel, productOf, { devour = false } = {}) {
    const entry = ingredient(ingredientId), level = Math.max(0, Math.floor(Number(classLevel) || 0));
    if (!entry || level < 1) return null;
    const factor = strengthAt(level), special = level >= ASSIMILATION.SPECIAL_FROM;
    const durationTurns = (ASSIMILATION.LASTS[entry.rarity] + (level >= ASSIMILATION.LONGER_FROM ? 1 : 0)) * (devour ? 2 : 1);
    const products = recipesUsing(ingredientId).map(recipe => productOf(recipe.id)).filter(Boolean);
    const kinds = new Map();
    for (const product of products.filter(item => !item.poison)) for (const effect of product.effects || []) {
      if (!isBuff(effect) && !(special && SPECIALS.has(effect.type))) continue;
      const key = kindOf(effect), size = sizeOf(effect), weight = rank(product.rarity);
      const held = kinds.get(key);
      if (!held || size > held.size || size === held.size && weight > held.weight) kinds.set(key, { key, effect, size, weight: Math.max(weight, held?.weight ?? 0), product: product.id });
      else held.weight = Math.max(held.weight, weight);
    }
    const kept = [...kinds.values()].sort((a, b) => b.weight - a.weight || b.size - a.size).slice(0, ASSIMILATION.KINDS);
    const scale = value => { const scaled = Math.round(Math.abs(value) * factor); return Math.sign(value) * Math.max(1, scaled); };
    const effects = kept.map(({ effect, size }) => {
      if (effect.type === "heal" || effect.type === "restore-mana" || effect.type === "turn-start-restore-percent") {
        const perTurn = Math.max(1, Math.round(size * factor / durationTurns));
        return { type: "turn-start-restore-percent", amount: 0, [kindOf(effect) === "restore:hp" ? "hp" : "mana"]: perTurn, durationTurns };
      }
      const next = { ...effect, durationTurns };
      delete next.recipient;
      if (effect.type === "cure") { delete next.durationTurns; return next; }
      if (["turn-start-ap", "debuff-immune", "control-immune", "undying"].includes(effect.type)) next.amount = 1;
      else next.amount = scale(effect.amount);
      if (Number.isFinite(effect.healPercent)) next.healPercent = scale(effect.healPercent);
      if (Array.isArray(effect.aftermath)) next.aftermath = effect.aftermath.map(after => ({ ...after, amount: scale(after.amount) }));
      return next;
    });
    // An ingredient only in poisons: the strongest poison's effect on your weapon hits, at the same scale.
    let poisonId = null;
    if (!effects.length) {
      const poisons = products.filter(item => item.poison).sort((a, b) => rank(b.rarity) - rank(a.rarity));
      if (poisons.length) { poisonId = poisons[0].id; effects.push({ type: "weapon-poison", amount: Math.round(factor * 1000) / 1000, poisonId, durationTurns }); }
    }
    return { effects, durationTurns, poisonId, factor, kinds: kept.map(entry => entry.key) };
  }
  // A poison's effects scaled for a weapon hit (an Assimilator's poison-only ingredient).
  function scaledPoison(effects, factor) {
    return effects.map(effect => {
      const next = { ...effect };
      if (Number.isFinite(next.amount) && next.type !== "root-ap") next.amount = Math.sign(next.amount) * Math.max(1, Math.round(Math.abs(next.amount) * factor));
      if (Number.isFinite(next.percentMaxHp)) next.percentMaxHp = Math.max(1, Math.round(next.percentMaxHp * factor));
      if (next.recipient === "all-enemies") next.recipient = "target";
      return next;
    });
  }

  root.AlchemyRules = Object.freeze({
    RARITIES, FORAGE, NO_FORAGE, DROPS, APOTHECARY, COLLECT, ASSIMILATION,
    INGREDIENTS: D.INGREDIENTS, RECIPES: D.RECIPES, STARTING_RECIPES: D.STARTING_RECIPES,
    isIngredientId, ingredient, recipeFor, recipesUsing, matchRecipe, normalizeKnown,
    baseZone, forageTable, canForage, forage, enemyDrops, enemyIngredients, regionIngredients, ingredientStock, recipeOffers, recipePrice,
    collectPick, collectFactor, halved, assimilation, strengthAt, scaledPoison,
  });
})(globalThis);

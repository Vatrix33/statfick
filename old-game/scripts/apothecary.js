/* The Apothecary (port task 9, the Potion Rework; decisions P1–P31 in engine/Docs/Engine-Plan.md, "Decisions for task 9").
   In every settlement, its button (O) beside the General Store's (P26). One window in five menus, switched without leaving:
   - Potions: the Health and Mana Potions of your level band and every weaker pair, always and unlimited (P3), then a
     random selection of the new potions and poisons leaning toward your level band (castle cities 8 kinds, towns 5).
   - Ingredients: every common ingredient, and a few of the region's own rarer ones (P13).
   - Brewing: the cauldron in the middle, three ingredient boxes above it, a Brew button, the result on the cauldron, your
     known recipes listed on the right (P31). Brewing costs nothing but the three ingredients; any order.
   - Recipes: three recipes you don't know, ten times their potion's price (P14–P16).
   - Sell: potions, poisons and ingredients at half price (P28).
   Stock turns over with the other shops every 6 game hours (shopState.locations["apothecary:<settlement id>"]).
   The banners (scene-banners.js), the cauldron drawing and the item icons (forge-art.js) are PLACEHOLDER ART (P2). */
(() => {
  const $ = id => document.getElementById(id);
  const modal = $("apothecary-modal");
  if (!modal || !window.AlchemyRules) return;
  const A = window.AlchemyRules;
  const list = $("apothecary-list"), detail = $("apothecary-detail"), cauldron = $("apothecary-cauldron");
  const MENUS = {
    potions: { title: "On the shelves", description: castle => `Health and Mana Potions are always in stock. ${castle ? 8 : 5} other potions and poisons, restocked every 6 hours.` },
    ingredients: { title: "Ingredients", description: castle => `Every common ingredient, and ${castle ? 5 : 3} of this region's rarer ones.` },
    brewing: { title: "Your recipes", description: () => "Place three ingredients in the boxes and brew. Brewing costs nothing but the ingredients; their order doesn't matter." },
    recipes: { title: "Recipes for sale", description: () => "Three recipes you don't know yet. A recipe is yours for good once bought." },
    sell: { title: "Your pack", description: () => "The apothecary buys potions, poisons and ingredients at half price." },
  };
  let place = null, menu = "potions", previewId = null;
  // The cauldron: three boxes, the finished potion waiting to be collected, and the brewing animation.
  let slots = [null, null, null], result = null, brewTimer = null;

  const layout = () => window.ShopLayout;
  const blocked = () => worldState.pendingEncounter || window.Combat?.active() || window.WorldMap?.isTravelling();
  const currentPlace = () => WorldRules.settlementAt(worldState.position);
  const isCastle = () => place?.kind === "Castle city";
  const rarityRank = rarity => Math.max(0, A.RARITIES.indexOf(rarity));
  const ownedCount = id => inventory.filter(owned => owned === id).length;
  const unit = () => { const value = Number(Math.random()); return Number.isFinite(value) ? Math.min(1 - Number.EPSILON, Math.max(0, value)) : 0; };

  function setStatus(message, state = "") {
    $("apothecary-status").textContent = message;
    $("apothecary-status").className = "shop-status" + (state ? " is-" + state : "");
  }
  function syncPurse() {
    document.querySelectorAll(".currency-input").forEach(input => { input.value = currencies[input.dataset.currency]; });
    $("apothecary-purse").textContent = ShopRules.formatPrice(ShopRules.walletValue(currencies));
  }
  function pay(price) {
    const remaining = ShopRules.spend(currencies, price);
    if (!remaining) return false;
    currencies = remaining;
    return true;
  }
  function afterChange() {
    syncPurse();
    renderOwnedItems(); renderAvailableItems(); updateDerivedValues(); renderClasses();
    saveCharacter();
    render();
    window.WorldMap?.render();
    window.Guild?.render?.();
  }

  /* ---------- What things are ---------- */
  const STAT_NAMES = { strength: "Strength", vitality: "Vitality", mind: "Mind", dexterity: "Dexterity", intelligence: "Intelligence", wisdom: "Wisdom", speed: "Speed", luck: "Luck" };
  const elementName = element => element ? element[0].toUpperCase() + element.slice(1) : "";
  const signed = value => (value > 0 ? "+" : "−") + Math.abs(value);
  // One effect in words (potions, poisons and eaten ingredients share them).
  function describeEffect(effect) {
    const amount = Number(effect.amount) || 0;
    switch (effect.type) {
      case "stat-percent": return `${signed(amount)}% ${STAT_NAMES[effect.target] || effect.target}`;
      case "all-stats-percent": return `${signed(amount)}% to every attribute`;
      case "defense-percent": return `${signed(amount)}% ${effect.target === "evasion" ? "Evasion" : "Armor"}`;
      case "spell-power-percent": return `${signed(amount)}% spell power`;
      case "spell-cost-percent": return `spells cost ${amount}% less Mana`;
      case "incoming-weapon-damage-percent": return `${signed(amount)}% weapon damage taken`;
      case "incoming-spell-damage-percent": return `${signed(amount)}% spell damage taken`;
      case "incoming-spell-miss-chance": return `spells aimed at you miss ${amount}% of the time`;
      case "healing-received-percent": return `${signed(amount)}% healing received`;
      case "element-resistance-percent": return `${signed(amount)}% ${effect.elements?.length ? effect.elements.map(elementName).join(" and ") : "every element's"} resistance`;
      case "crit-chance-percent": return `${signed(amount)}% critical hit chance`;
      case "crit-damage-percent": return `${signed(amount)}% critical damage`;
      case "armor-pierce-percent": return `weapon attacks ignore ${amount}% of Armor`;
      case "outgoing-attack-damage-percent": return `${signed(amount)}% weapon damage dealt`;
      case "elite-damage-percent": return `${signed(amount)}% damage to bosses, Empowered and Sparkling enemies`;
      case "weapon-hit-percent": return `${signed(amount)}% hit chance`;
      case "turn-start-restore-percent": return effect.hp ? `heals ${effect.hp}% of max HP each turn` : `restores ${effect.mana}% of max Mana each turn`;
      case "barrier-percent": return `a barrier of ${amount}% of max HP`;
      case "turn-start-ap": return `+${amount} AP each turn`;
      case "debuff-immune": return "immune to negative effects";
      case "control-immune": return "immune to stuns and AP loss";
      case "rise-again-percent": return `rises again once with ${amount}% of max HP after falling`;
      case "undying": return "can't fall below 1 HP";
      case "cure": return amount >= 99 ? "removes every negative effect" : `removes ${amount} negative effect${amount === 1 ? "" : "s"}`;
      case "heal": return `heals ${effect.maxPercent}% of max HP`;
      case "restore-mana": return `restores ${effect.maxPercent}% of max Mana`;
      case "weapon-poison": return `weapon hits carry ${getItem(effect.poisonId)?.name || "a poison"} at ${Math.round(amount * 100)}% strength`;
      case "poison-dot": return `${amount} plus ${effect.percentMaxHp}% of max HP Nature damage each turn`;
      case "stun-chance": return `${amount}% chance to stun`;
      case "root-ap": return `−${amount} AP next turn`;
      case "devour-ready": return "Devour ready";
      default: return effect.type;
    }
  }
  const describeEffects = effects => (effects || []).map(describeEffect).join(", ");
  // The Alchemist's Ingredient Lore (P30): every potion and poison an ingredient goes into; unknown recipes stay hidden.
  const hasLore = () => calculateClassEffects().passives.some(passive => passive.id === "ingredient-lore");
  function ingredientUses(ingredientId) {
    return A.recipesUsing(ingredientId).map(recipe => {
      const product = getItem(recipe.id), known = alchemy.known.includes(recipe.id);
      return { id: recipe.id, name: product?.name || recipe.id, rarity: product?.rarity || "common", known, others: known ? recipe.ingredients.filter(id => id !== ingredientId).map(id => A.ingredient(id)?.name || id) : null };
    });
  }
  // Where an ingredient is found (its forage zones and the enemies that drop it), for its details.
  function ingredientSources(ingredientId) {
    const zones = [];
    for (const [regionId, table] of Object.entries(window.AlchemyData.FORAGE)) for (const [zone, entries] of Object.entries(table)) {
      if (entries.some(entry => entry.id === ingredientId)) zones.push(`${WorldRules.getRegion(regionId)?.name || regionId}: ${zone}`);
    }
    if (window.AlchemyData.RIVER.includes(ingredientId)) zones.unshift("every river");
    return zones;
  }
  const forageBonus = () => { const effects = calculateClassEffects(); return { find: effects.forageFindPercent || 0, extraOne: Boolean(effects.forageExtraOne) }; };
  // The enemies of a region (from its encounter tables), as ids.
  function regionEnemyIds(regionId) {
    const rules = window.EncounterRules?.regionRules(regionId);
    if (!rules || window.WorldRules.getRegion(regionId)?.uncharted) return [];
    const names = Object.keys(rules.tables || {}).flatMap(zone => EncounterRules.enemiesForZone(zone, regionId));
    return window.Combat?.enemyIdsNamed?.(names) || [];
  }
  const regionIngredients = regionId => A.regionIngredients(regionId, regionEnemyIds(regionId));

  /* ---------- Stock ---------- */
  const stockKey = () => "apothecary:" + place.id;
  const bandRank = () => rarityRank(ShopRules.levelBand(level).rarity);
  // Health and Mana Potions of your band and every weaker one, always on the shelf (P3).
  const standingPotions = () => items.filter(item => item?.category === "consumable" && item.potionResource && rarityRank(item.rarity) <= bandRank())
    .sort((a, b) => rarityRank(a.rarity) - rarityRank(b.rarity) || String(a.potionResource).localeCompare(String(b.potionResource)));
  const alchemyPool = () => items.filter(item => item?.category === "consumable" && item.alchemy);
  function validStock(saved) {
    const entries = value => Array.isArray(value) && value.every(entry => entry && typeof entry.id === "string" && getItem(entry.id) && Number.isInteger(entry.quantity) && entry.quantity >= 0);
    return Boolean(saved && entries(saved.potions) && entries(saved.ingredients) && Array.isArray(saved.recipes) && saved.recipes.every(id => A.recipeFor(id))
      && saved.potions.every(entry => getItem(entry.id)?.alchemy) && saved.ingredients.every(entry => A.isIngredientId(entry.id)) && saved.potions.length);
  }
  function generate() {
    const castle = isCastle(), [low, high] = A.APOTHECARY.POTION_QUANTITY[castle ? "castle" : "town"];
    const potions = ShopRules.itemStock(alchemyPool(), A.APOTHECARY.POTIONS[castle ? "castle" : "town"], level, unit)
      .sort((a, b) => rarityRank(a.rarity) - rarityRank(b.rarity) || Number(Boolean(a.poison)) - Number(Boolean(b.poison)))
      .map(item => ({ id: item.id, quantity: low + Math.floor(unit() * (high - low + 1)) }));
    return {
      potions,
      ingredients: A.ingredientStock({ castle, regional: regionIngredients(worldState.regionId), random: unit }),
      recipes: A.recipeOffers({ known: alchemy.known, level, productRarity: id => getItem(id)?.rarity || "common", random: unit }),
    };
  }
  function stock() {
    Shop.normalize();
    const locations = shopState.locations ||= {};
    const saved = locations[stockKey()]?.apothecary;
    if (validStock(saved)) return saved;
    locations[stockKey()] = { apothecary: generate() };
    saveCharacter();
    return locations[stockKey()].apothecary;
  }
  // The recipes on offer that are still unknown (a bought one is never offered again; fewer remain then).
  const offeredRecipes = () => stock().recipes.filter(id => !alchemy.known.includes(id));
  const recipePrice = id => A.recipePrice(getItem(id)?.priceSilver);

  /* ---------- Rows ---------- */
  function itemRow(item, { quantity = null, unlimited = false, action = "buy" } = {}) {
    const soldOut = !unlimited && quantity <= 0, poor = ShopRules.walletValue(currencies) < item.priceSilver;
    const label = soldOut ? "Sold out" : poor ? "Cannot afford" : "Buy";
    const meta = itemRarityTag(item.rarity) + " " + escapeHtml(item.category === "ingredient" ? "Ingredient" : item.poison ? "Poison" : "Potion") + (unlimited ? " · always in stock" : " · ×" + quantity + " available") + " · ×" + ownedCount(item.id) + " owned";
    return layout().row({ id: item.id, art: layout().itemArt(item), name: item.name, meta, price: ShopRules.formatPrice(item.priceSilver), className: itemRarityClass(item) + (soldOut ? " is-sold-out" : ""), previewed: item.id === previewId,
      actions: `<button type="button" data-apothecary-${action}="${escapeHtml(item.id)}"${soldOut || poor ? " disabled" : ""}>${label}</button>` });
  }
  function recipeIngredientsText(recipe) {
    return recipe.ingredients.map(id => { const have = ownedCount(id); return escapeHtml(A.ingredient(id)?.name || id) + (have ? ` <b class="recipe-have">×${have}</b>` : ' <b class="recipe-missing">×0</b>'); }).join(" · ");
  }
  function recipeOfferRow(id) {
    const product = getItem(id), price = recipePrice(id), poor = ShopRules.walletValue(currencies) < price;
    return layout().row({ id, art: layout().itemArt(product), name: "Recipe: " + product.name, meta: itemRarityTag(product.rarity) + " " + (product.poison ? "Poison" : "Potion") + " recipe", price: ShopRules.formatPrice(price),
      className: itemRarityClass(product), previewed: id === previewId, actions: `<button type="button" data-apothecary-recipe="${escapeHtml(id)}"${poor ? " disabled" : ""}>${poor ? "Cannot afford" : "Buy"}</button>` });
  }
  function knownRecipeRow(id) {
    const product = getItem(id), recipe = A.recipeFor(id), ready = recipe.ingredients.every(part => ownedCount(part) >= recipe.ingredients.filter(other => other === part).length);
    return layout().row({ id, art: layout().itemArt(product), name: product.name, meta: itemRarityTag(product.rarity) + " " + recipeIngredientsText(recipe), className: itemRarityClass(product) + (ready ? " is-ready" : ""), previewed: id === previewId,
      actions: `<button type="button" data-apothecary-pick="${escapeHtml(id)}">${ready ? "Use" : "View"}</button>` });
  }

  /* ---------- The cauldron ---------- */
  const filled = () => slots.filter(Boolean);
  const matching = () => filled().length === 3 ? A.matchRecipe(slots) : null;
  function cauldronMarkup() {
    const recipe = matching(), knownMatch = recipe && alchemy.known.includes(recipe.id), product = result ? getItem(result) : knownMatch ? getItem(recipe.id) : null;
    const boxes = slots.map((id, index) => {
      const item = id ? getItem(id) : null;
      return `<button type="button" class="cauldron-slot${item ? " is-filled " + itemRarityClass(item) : ""}" data-cauldron-slot="${index}" aria-label="${item ? "Remove " + escapeHtml(item.name) + " from box " + (index + 1) : "Box " + (index + 1) + ": empty"}">`
        + (item ? `<span class="cauldron-slot-art">${layout().itemArt(item)}</span><small>${escapeHtml(item.name)}</small>` : `<small>Box ${index + 1}</small>`) + "</button>";
    }).join("");
    const field = result
      ? `<div class="cauldron-result is-ready ${itemRarityClass(product)}"><span class="cauldron-result-art">${layout().itemArt(product)}</span><strong>${escapeHtml(product.name)}</strong><small>Ready to collect</small></div>`
      : knownMatch ? `<div class="cauldron-result is-recognized ${itemRarityClass(product)}"><span class="cauldron-result-art">${layout().itemArt(product)}</span><strong>${escapeHtml(product.name)}</strong><small>These make ${escapeHtml(product.name)}</small></div>`
      : filled().length === 3 ? `<div class="cauldron-result is-unknown"><strong>No known recipe</strong><small>These three make nothing you know</small></div>`
      : `<div class="cauldron-result"><small>${filled().length ? "Add " + (3 - filled().length) + " more" : "Three ingredients"}</small></div>`;
    const pack = [...new Set(inventory.filter(id => A.isIngredientId(id)))].map(getItem).filter(Boolean).sort((a, b) => rarityRank(a.rarity) - rarityRank(b.rarity) || a.name.localeCompare(b.name));
    const free = id => ownedCount(id) - slots.filter(slot => slot === id).length;
    const chips = pack.map(item => `<button type="button" class="cauldron-chip ${itemRarityClass(item)}" data-cauldron-add="${escapeHtml(item.id)}"${free(item.id) <= 0 || result ? " disabled" : ""}><span>${layout().itemArt(item)}</span>${escapeHtml(item.name)} ×${free(item.id)}</button>`).join("");
    const pot = `<svg class="cauldron-pot" viewBox="0 0 200 150" aria-hidden="true"><path d="M30 46H170Q172 120 100 134Q28 120 30 46Z" fill="#2c2a28" stroke="#121010" stroke-width="3"/><ellipse cx="100" cy="46" rx="70" ry="13" fill="#3e3c3a" stroke="#121010" stroke-width="3"/><ellipse class="cauldron-brew" cx="100" cy="47" rx="60" ry="9"/><path d="M52 132L40 148M148 132L160 148" stroke="#121010" stroke-width="5"/></svg>`;
    return `<div class="cauldron-boxes">${boxes}</div><div class="cauldron-body">${pot}${field}</div>`
      + `<div class="cauldron-actions"><button type="button" class="cauldron-brew-button" data-cauldron-brew${result || filled().length < 3 ? " disabled" : ""}>Brew</button>`
      + `<button type="button" data-cauldron-collect${result ? "" : " hidden"}>Collect</button><button type="button" data-cauldron-clear${filled().length && !result ? "" : " disabled"}>Clear</button></div>`
      + `<div class="cauldron-pack"><h4>Your ingredients</h4>${chips || '<p class="shop-empty">You carry no ingredients.</p>'}</div>`;
  }
  function placeIngredient(id, index = slots.indexOf(null)) {
    if (result || index < 0 || index > 2 || !A.isIngredientId(id)) return;
    if (ownedCount(id) - slots.filter((slot, at) => slot === id && at !== index).length <= 0) { setStatus("You have no more " + (A.ingredient(id)?.name || "of that") + ".", "error"); return; }
    slots[index] = id;
    const recipe = matching();
    setStatus(recipe && alchemy.known.includes(recipe.id) ? `${A.ingredient(id).name} placed. These make ${getItem(recipe.id).name}.` : `${A.ingredient(id).name} placed in box ${index + 1}.`, recipe && alchemy.known.includes(recipe.id) ? "success" : "");
    render();
  }
  function pickRecipe(id) {
    const recipe = A.recipeFor(id);
    if (!recipe || !alchemy.known.includes(id) || result) return;
    previewId = id;
    const counts = new Map();
    recipe.ingredients.forEach(part => counts.set(part, (counts.get(part) || 0) + 1));
    const missing = [...counts].filter(([part, need]) => ownedCount(part) < need).map(([part]) => A.ingredient(part)?.name || part);
    if (missing.length) { setStatus("You lack " + missing.join(", ") + " for " + getItem(id).name + ".", "error"); render(); return; }
    slots = [...recipe.ingredients];
    setStatus(`The boxes hold the ingredients for ${getItem(id).name}.`, "success");
    render();
  }
  // Brew (P31): three boxes filled, the ingredients in the pack, a known recipe; only then are the three used up.
  function brew() {
    if (blocked() || modal.hidden || menu !== "brewing" || result) return;
    if (filled().length < 3) { setStatus("Fill all three boxes first.", "error"); return; }
    const counts = new Map();
    slots.forEach(id => counts.set(id, (counts.get(id) || 0) + 1));
    const lacking = [...counts].filter(([id, need]) => ownedCount(id) < need).map(([id]) => A.ingredient(id)?.name || id);
    if (lacking.length) { setStatus("You don't have enough " + lacking.join(", ") + ".", "error"); return; }
    const recipe = matching();
    if (!recipe || !alchemy.known.includes(recipe.id)) { setStatus("These ingredients don't match a recipe you know. Nothing was used.", "error"); return; }
    for (const id of slots) inventory.splice(inventory.indexOf(id), 1);
    result = recipe.id; slots = [null, null, null];
    alchemy.brewed += 1;
    cauldron.classList.add("is-brewing");
    clearTimeout(brewTimer);
    brewTimer = setTimeout(() => cauldron.classList.remove("is-brewing"), 1200);
    setStatus(`The cauldron bubbles: ${getItem(recipe.id).name} is ready to collect.`, "success");
    afterChange();
  }
  function collect() {
    if (!result) return false;
    const product = getItem(result);
    inventory.push(result);
    result = null;
    setStatus(product.name + " goes into your pack.", "success");
    afterChange();
    return true;
  }

  /* ---------- Buying and selling ---------- */
  function buy(id) {
    if (blocked() || modal.hidden || currentPlace()?.id !== place?.id) return;
    const item = getItem(id);
    const standing = menu === "potions" && standingPotions().some(entry => entry.id === id);
    const entry = standing ? null : stock()[menu === "ingredients" ? "ingredients" : "potions"].find(candidate => candidate.id === id);
    if (!item || !standing && (!entry || entry.quantity <= 0)) { setStatus("That is not on the shelves.", "error"); return; }
    if (!pay(item.priceSilver)) { setStatus("You do not have enough coin for " + item.name + ".", "error"); return; }
    if (entry) entry.quantity -= 1;
    inventory.push(item.id);
    setStatus(item.name + " purchased for " + ShopRules.formatPrice(item.priceSilver) + ".", "success");
    afterChange();
  }
  function buyRecipe(id) {
    if (blocked() || modal.hidden || menu !== "recipes" || !offeredRecipes().includes(id) || alchemy.known.includes(id)) return;
    const price = recipePrice(id);
    if (!pay(price)) { setStatus("You do not have enough coin for that recipe.", "error"); return; }
    alchemy.known.push(id);
    setStatus("You learn to brew " + getItem(id).name + ". It is in your recipe list.", "success");
    afterChange();
  }
  const sellable = item => Boolean(item && (item.potion || item.poison || item.category === "ingredient"));
  function sell(id, all = false) {
    if (blocked() || modal.hidden || menu !== "sell") return;
    const entry = layout().sellableEntries(sellable).find(candidate => candidate.id === id);
    if (!entry) return;
    const totals = layout().sellFromPack([{ id, quantity: all ? entry.quantity : 1 }], sellable);
    if (!totals.quantity) { setStatus("That cannot be sold here.", "error"); return; }
    setStatus("Sold " + totals.quantity + (totals.quantity === 1 ? " item" : " items") + " for " + ShopRules.formatPrice(totals.price) + ".", "success");
    afterChange();
  }

  /* ---------- Window ---------- */
  function renderDetail() {
    const item = getItem(previewId);
    if (!item) { detail.innerHTML = '<div class="shop-empty">Hover over an entry to see it in full.</div>'; return; }
    let note = "";
    if (menu === "recipes") note = "Recipe price " + escapeHtml(ShopRules.formatPrice(recipePrice(item.id))) + " · teaches you to brew it from " + A.recipeFor(item.id).ingredients.map(id => escapeHtml(A.ingredient(id)?.name || id)).join(", ");
    else if (menu === "brewing" && A.recipeFor(item.id)) note = "Recipe: " + A.recipeFor(item.id).ingredients.map(id => escapeHtml(A.ingredient(id)?.name || id) + " (×" + ownedCount(id) + ")").join(", ");
    else if (menu === "sell") note = "Sells for " + escapeHtml(ShopRules.formatPrice(layout().salePrice(item))) + " each";
    else note = "Price " + escapeHtml(ShopRules.formatPrice(item.priceSilver));
    detail.innerHTML = layout().itemDetail(item, note);
    detail.scrollTop = 0;
  }
  function render() {
    if (modal.hidden || !place) return;
    if (currentPlace()?.id !== place.id) { close(); return; }
    $("apothecary-eyebrow").textContent = place.name.toUpperCase() + " · APOTHECARY";
    window.SceneBanners?.show($("apothecary-scene"), "apothecary", place, worldState.regionId);
    syncPurse();
    modal.querySelectorAll("[data-apothecary-menu]").forEach(button => { button.setAttribute("aria-pressed", String(button.dataset.apothecaryMenu === menu)); button.classList.toggle("is-active", button.dataset.apothecaryMenu === menu); });
    $("apothecary-description").textContent = MENUS[menu].description(isCastle());
    const nextRefresh = (shopState.refreshWindow + 1) * 360;
    $("apothecary-refresh").textContent = menu === "brewing" ? "Known recipes: " + alchemy.known.length + " of " + A.RECIPES.length : "Stock refreshes at Day " + WorldRules.day(nextRefresh) + " · " + WorldRules.time(nextRefresh);
    $("apothecary-list-title").textContent = MENUS[menu].title;
    $("apothecary-workspace").dataset.mode = menu === "brewing" ? "brewing" : menu === "sell" ? "sell" : "buy";
    const brewing = menu === "brewing";
    cauldron.hidden = !brewing;
    $("apothecary-detail-panel").hidden = brewing;
    const current = stock();
    let rows = [];
    if (menu === "potions") rows = [...standingPotions().map(item => itemRow(item, { unlimited: true })), ...current.potions.map(entry => itemRow(getItem(entry.id), { quantity: entry.quantity }))];
    else if (menu === "ingredients") rows = current.ingredients.map(entry => itemRow(getItem(entry.id), { quantity: entry.quantity }));
    else if (menu === "recipes") rows = offeredRecipes().map(recipeOfferRow);
    else if (menu === "brewing") rows = alchemy.known.filter(id => getItem(id)).sort((a, b) => rarityRank(getItem(a).rarity) - rarityRank(getItem(b).rarity) || getItem(a).name.localeCompare(getItem(b).name)).map(knownRecipeRow);
    else rows = layout().sellableEntries(sellable).map(entry => layout().sellRowFor(entry, entry.id === previewId));
    const empty = { potions: "Nothing on the shelves.", ingredients: "No ingredients today.", recipes: "You know every recipe on offer here.", brewing: "You know no recipes yet.", sell: "You carry no potions, poisons or ingredients." }[menu];
    list.innerHTML = rows.join("") || `<div class="shop-empty">${empty}</div>`;
    if (menu === "sell") list.querySelectorAll("[data-sell-item]").forEach(button => { button.dataset.apothecarySell = button.dataset.sellItem; button.removeAttribute("data-sell-item"); });
    if (menu === "sell") list.querySelectorAll("[data-sell-all-item]").forEach(button => { button.dataset.apothecarySellAll = button.dataset.sellAllItem; button.removeAttribute("data-sell-all-item"); });
    if (brewing) cauldron.innerHTML = cauldronMarkup();
    else renderDetail();
  }
  function setMenu(next) {
    if (!MENUS[next]) return;
    menu = next; previewId = null; setStatus(""); render();
  }
  function open() {
    if (blocked()) return;
    place = currentPlace();
    if (!place) return;
    menu = "potions"; previewId = null; slots = [null, null, null];
    modal.hidden = false;
    setStatus(""); render(); updateModalState();
    modal.querySelector(".modal-close").focus();
  }
  function close() {
    // A finished potion left on the cauldron goes into the pack.
    if (result) collect();
    slots = [null, null, null];
    modal.hidden = true; updateModalState();
    $("map-apothecary")?.focus();
  }

  layout().bindPreview(list, id => { if (id === previewId) return; previewId = id; layout().markPreview(list, id); if (menu !== "brewing") renderDetail(); });
  list.addEventListener("click", event => {
    const target = event.target.closest("button");
    if (!target || target.disabled) return;
    if (target.dataset.apothecaryBuy) buy(target.dataset.apothecaryBuy);
    else if (target.dataset.apothecaryRecipe) buyRecipe(target.dataset.apothecaryRecipe);
    else if (target.dataset.apothecaryPick) pickRecipe(target.dataset.apothecaryPick);
    else if (target.dataset.apothecarySell) sell(target.dataset.apothecarySell);
    else if (target.dataset.apothecarySellAll) sell(target.dataset.apothecarySellAll, true);
  });
  cauldron.addEventListener("click", event => {
    const target = event.target.closest("button");
    if (!target || target.disabled || blocked()) return;
    if (target.dataset.cauldronAdd) placeIngredient(target.dataset.cauldronAdd);
    else if (target.dataset.cauldronSlot != null) {
      const index = Number(target.dataset.cauldronSlot);
      if (slots[index] && !result) { const removed = A.ingredient(slots[index])?.name; slots[index] = null; setStatus(removed + " taken out of box " + (index + 1) + "."); render(); }
    } else if (target.hasAttribute("data-cauldron-brew")) brew();
    else if (target.hasAttribute("data-cauldron-collect")) collect();
    else if (target.hasAttribute("data-cauldron-clear")) { slots = [null, null, null]; setStatus("The boxes are empty."); render(); }
  });
  modal.querySelectorAll("[data-apothecary-menu]").forEach(button => button.addEventListener("click", () => setMenu(button.dataset.apothecaryMenu)));
  modal.querySelectorAll("[data-apothecary-close]").forEach(button => button.addEventListener("click", close));
  modal.addEventListener("keydown", event => { if (event.key === "Escape") { event.preventDefault(); close(); } });
  $("map-apothecary")?.addEventListener("click", open);

  window.Apothecary = { open, close, render, setMenu, stock: () => place ? stock() : null, slots: () => [...slots], result: () => result, placeIngredient, pickRecipe, brew, collect, standingPotions, regionIngredients };
  window.Alchemy = { describeEffect, describeEffects, ingredientUses, ingredientSources, hasLore, forageBonus, regionEnemyIds, regionIngredients };
})();

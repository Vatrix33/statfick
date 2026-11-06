/* Settlement shops: the blacksmith, the tailor and the general store (the Tailor and the reforge menus came with
   port task 3, the Shop Rework, 2026-11-06: clothing moved from the Blacksmith to the Tailor, and both reforge). An interior scene fills each
   shop window. Buying lists the stock on the right and shows the hovered entry in full on
   the left; selling mirrors that, with your pack on the left. Castle cities' Magic Towers
   keep their staffs, wands and spells in the same refreshing stock (see magic-tower.js). */
(() => {
  const $ = id => document.getElementById(id);
  const modal = $("shop-modal"), stockList = $("shop-stock-list"), detail = $("shop-detail"), status = $("shop-status");
  const STOCK_VERSION = 25;
  const OVERTIER_SPELL_CHANCE = 0.1;
  // Blacksmith shelves: forged melee and ranged weapons, then forged armor without clothing (castle 4, town 3:
  // user decision R8). Regional offers take armor places. The Tailor's shelves hold clothing only (R7).
  const SMITH = { castle: { total: 9, weapons: 5 }, town: { total: 6, weapons: 3 } };
  const TAILOR = { castle: 6, town: 4 };
  const GENERAL = { castle: 8, town: 5 };
  // A Magic Tower sells forged staffs and wands and teaches spells; it stands only in castle cities.
  const TOWER = { casters: 6, spells: 12 };
  const SHOPS = {
    blacksmith: { name: "BLACKSMITH", label: "Blacksmith", title: "Arms and <em>armor</em>" },
    general: { name: "GENERAL STORE", label: "General Store", title: "Goods for the <em>road</em>" },
    tailor: { name: "TAILOR", label: "Tailor", title: "Cloth and <em>thread</em>" },
    hearth: { name: "HEARTH HOME", label: "Hearth Home", title: "The forge of <em>Hearthforge</em>" },
  };
  /* Hearth Home (port task 3, R14): on Cinder Isle's volcano, open once Ornn has fallen there. Hearthforge
     weapons and armor (a castle city's Blacksmith's count) at 10% off, and the only anvil for masterworks,
     reforging at 15% off. It buys nothing. */
  const HEARTH = { id: "hearth-home", bossId: "ornn-god-of-the-forge-unique-level-48", weapons: 5, armor: 4, buyFactor: 0.9, reforgeDiscount: 15 };
  const hearthOpen = () => Boolean(worldState.defeatedSiteBosses?.includes(HEARTH.bossId));
  const hearthHere = () => { const site = WorldRules.siteAt(worldState.position, worldState.regionId); return site?.id === HEARTH.id && hearthOpen() ? site : null; };
  const priceFor = item => kind === "hearth" ? Math.round(item.priceSilver * HEARTH.buyFactor) : item.priceSilver;
  function hearthStock() {
    normalizeState();
    const saved = shopState.locations[HEARTH.id];
    const hearthMade = item => item?.forged && !item.forged.masterwork && item.forged.parts?.[0]?.maker === "hearthforge";
    if (saved && Array.isArray(saved.hearth) && saved.hearth.length === HEARTH.weapons + HEARTH.armor && saved.hearth.every(entry => hearthMade(getItem(entry.id)) && Number.isInteger(entry.quantity) && entry.quantity >= 0 && entry.quantity <= 1)) return saved;
    const distinct = (count, make) => { const made = []; for (let attempt = 0; made.length < count && attempt < count * 10 + 10; attempt++) { const item = getItem(ForgeRules.encode(make())); if (hearthMade(item) && !made.some(entry => entry.id === item.id || entry.name === item.name)) made.push(item); } return made; };
    const weapons = distinct(HEARTH.weapons, () => ForgeRules.generate({ level, source: "shop", random: unitRandom, casterShare: 0, maker: "hearthforge" }));
    const armor = distinct(HEARTH.armor, () => ForgeRules.generateArmor({ level, source: "shop", random: unitRandom, maker: "hearthforge", types: smithArmorTypes() }));
    shopState.locations[HEARTH.id] = { hearth: [...weapons, ...armor].map(item => ({ id: item.id, quantity: 1 })) };
    saveCharacter();
    return shopState.locations[HEARTH.id];
  }
  const isClothing = item => item?.category === "armor" && item.type === "clothing";
  const smithArmorTypes = () => ForgeRules.ARMOR_IDS.filter(id => ForgeRules.ARMORS[id].category !== "clothing");
  const clothingTypes = () => ForgeRules.ARMOR_IDS.filter(id => ForgeRules.ARMORS[id].category === "clothing");
  const tailorCount = settlement => window.ForgeRules ? TAILOR[isCastle(settlement) ? "castle" : "town"] : 0;
  let kind = "general", mode = "buy", place = null, previewId = null, anvil = null;
  const unitRandom = () => {
    const value = Number(Math.random());
    return Number.isFinite(value) ? Math.min(1 - Number.EPSILON, Math.max(0, value)) : 0;
  };
  const isCastle = settlement => settlement?.kind === "Castle city";

  function currentPlace() {
    return kind === "hearth" ? hearthHere() : WorldRules.settlementAt(worldState.position);
  }

  function setStatus(message, state = "") {
    status.textContent = message;
    status.className = "shop-status" + (state ? " is-" + state : "");
  }

  function syncPurse() {
    document.querySelectorAll(".currency-input").forEach(input => { input.value = currencies[input.dataset.currency]; });
    $("shop-purse").textContent = ShopRules.formatPrice(ShopRules.walletValue(currencies));
  }

  function normalizeState() {
    const windowId = ShopRules.refreshWindow(worldState.minutes);
    if (!shopState || typeof shopState !== "object") shopState = {};
    if (shopState.version !== STOCK_VERSION || shopState.refreshWindow !== windowId) shopState = { version: STOCK_VERSION, refreshWindow: windowId, locations: {} };
    if (!shopState.locations || typeof shopState.locations !== "object") shopState.locations = {};
    return windowId;
  }

  /* ---------- Stock ---------- */
  function smithPlan(settlement) { return SMITH[isCastle(settlement) ? "castle" : "town"]; }
  function generalCount(settlement) { return GENERAL[isCastle(settlement) ? "castle" : "town"]; }
  function forgedWeaponCount(settlement) {
    return window.ForgeRules ? smithPlan(settlement).weapons : 0;
  }
  // Armor fills the rest of the smith's shelves after weapons and regional offers.
  function forgedArmorCount(settlement, regionalOfferCount = 0) {
    return window.ForgeRules ? Math.max(0, smithPlan(settlement).total - forgedWeaponCount(settlement) - regionalOfferCount) : 0;
  }
  // Masterworks traded in this settlement (Bloody Mary in Ravenwatch), forged at the character's level.
  function masterworkOffers(settlement) {
    if (!window.ForgeRules || !settlement) return [];
    return ForgeRules.MASTERWORK_IDS.filter(key => {
      const offer = ForgeRules.MASTERWORKS[key].shopOffer;
      return offer && offer.regionId === worldState.regionId && offer.settlementId === settlement.id;
    });
  }
  function towerCasterCount(settlement) {
    return window.ForgeRules && isCastle(settlement) ? TOWER.casters : 0;
  }
  function towerSpellCount(settlement) {
    return isCastle(settlement) ? TOWER.spells : 0;
  }

  function generateStock(settlement) {
    normalizeState();
    const castle = isCastle(settlement);
    const availableItems = items.filter(item => purchasableItem(item, settlement));
    const desiredRarity = ShopRules.levelBand(level).rarity;
    const blacksmithPool = availableItems.filter(item => (item.category === "weapon" || item.category === "armor") && !isClothing(item));
    const generalPool = availableItems.filter(item => item.category !== "weapon" && item.category !== "armor");
    const regionalOffers = [...blacksmithPool.filter(item => item.shopOffer), ...masterworkOffers(settlement).map(key => getItem(ForgeRules.encode(ForgeRules.generateMasterwork({ key, level, random: unitRandom }))))];
    // Weapons, armor, staffs and wands are forged fresh at the character's level; their rune
    // slots stay empty. Vampiric pieces are sold only in the vampire castle town.
    const allowVampiric = settlement.requiresClass === "vampire";
    const forgeDistinct = (count, make) => {
      const made = [];
      for (let attempt = 0; made.length < count && attempt < count * 10 + 10; attempt++) {
        const forgedItem = getItem(ForgeRules.encode(make()));
        // Two pieces that differ only in a hidden part would read as duplicates, so names stay distinct too.
        if (forgedItem && !made.some(entry => entry.id === forgedItem.id || entry.name === forgedItem.name)) made.push(forgedItem);
      }
      return made;
    };
    const forgedWeapons = forgeDistinct(forgedWeaponCount(settlement), () => ForgeRules.generate({ level, source: "shop", random: unitRandom, allowVampiric, casterShare: 0 }));
    const forgedArmor = forgeDistinct(forgedArmorCount(settlement, regionalOffers.length), () => ForgeRules.generateArmor({ level, source: "shop", random: unitRandom, allowVampiric, types: smithArmorTypes() }));
    const forgedClothing = forgeDistinct(tailorCount(settlement), () => ForgeRules.generateArmor({ level, source: "shop", random: unitRandom, allowVampiric, types: clothingTypes() }));
    const smithReserved = [...regionalOffers, ...forgedWeapons, ...forgedArmor];
    const smithIds = new Set(smithReserved.map(item => item.id));
    const selectedBlacksmith = [...smithReserved, ...ShopRules.itemStock(blacksmithPool.filter(item => !smithIds.has(item.id) && !item.shopOffer), smithPlan(settlement).total - smithReserved.length, level)];
    const guaranteedPotions = ["hp", "mana"].map(resource => generalPool.find(item => item.potionResource === resource && item.rarity === desiredRarity)).filter(Boolean);
    const generalIds = new Set(guaranteedPotions.map(item => item.id));
    const selectedGeneral = [...guaranteedPotions, ...ShopRules.itemStock(generalPool.filter(item => !generalIds.has(item.id)), generalCount(settlement) - guaranteedPotions.length, level)];
    const selectedSpells = towerSpellCount(settlement) ? ShopRules.spellStock(spells.filter(normallyPurchasableSpell), towerSpellCount(settlement), level) : [];
    const overtierSpells = spells.filter(overtierPurchasableSpell);
    if (selectedSpells.length && overtierSpells.length && unitRandom() < OVERTIER_SPELL_CHANCE) {
      const overtierSpell = ShopRules.spellStock(overtierSpells, 1, level)[0];
      if (overtierSpell && (overtierSpell.tier !== 10 || unitRandom() < 0.03)) {
        selectedSpells[selectedSpells.length - 1] = overtierSpell;
      }
    }
    const stockEntries = selectedItems => selectedItems.map(item => ({ id: item.id, quantity: item.category === "consumable" ? randomQuantity(castle ? 3 : 2, castle ? 6 : 4) : 1 }));
    const casters = towerCasterCount(settlement) ? forgeDistinct(towerCasterCount(settlement), () => ForgeRules.generate({ level, source: "shop", random: unitRandom, allowVampiric, casterShare: 1 })) : [];
    shopState.locations[settlement.id] = {
      blacksmith: stockEntries(selectedBlacksmith),
      tailor: stockEntries(forgedClothing),
      general: stockEntries(selectedGeneral),
      tower: stockEntries(casters),
      spells: selectedSpells.map(spell => spell.id),
    };
    saveCharacter();
  }

  function randomQuantity(minimum, maximum) {
    return minimum + Math.floor(unitRandom() * (maximum - minimum + 1));
  }

  function purchasableItem(item, settlement = place) {
    return Boolean(item && item.category !== "artifact" && item.category !== "rune" && (item.catalogueAvailable !== false || item.forged) && item.shopAvailable !== false && Number.isFinite(item.priceSilver)
      && (!item.shopOffer || item.shopOffer.regionId === worldState.regionId && item.shopOffer.settlementId === settlement?.id));
  }

  function spellTierMinimumLevel(spell) {
    const tier = Math.max(1, Math.trunc(Number(spell?.tier) || 1));
    return Math.max(1, (tier - 1) * 10);
  }

  function shopEligibleSpell(spell) {
    return Boolean(spell && spell.shopAvailable !== false && Number.isFinite(spell.priceSilver));
  }

  function normallyPurchasableSpell(spell) {
    return shopEligibleSpell(spell) && level >= spellTierMinimumLevel(spell);
  }

  function overtierPurchasableSpell(spell) {
    const minimumLevel = spellTierMinimumLevel(spell);
    return shopEligibleSpell(spell) && minimumLevel > level && minimumLevel <= level + 10;
  }

  function purchasableSpell(spell) {
    return normallyPurchasableSpell(spell) || overtierPurchasableSpell(spell);
  }

  function validItemEntries(entries, expectedCount, predicate, maximumConsumables, settlement) {
    if (!Array.isArray(entries) || entries.length !== expectedCount) return false;
    const ids = new Set();
    return entries.every(entry => {
      if (!entry || typeof entry.id !== "string" || ids.has(entry.id) || !Number.isInteger(entry.quantity) || entry.quantity < 0) return false;
      ids.add(entry.id);
      const item = getItem(entry.id);
      const maximumQuantity = item?.category === "consumable" ? maximumConsumables : 1;
      return purchasableItem(item, settlement) && predicate(item) && entry.quantity <= maximumQuantity;
    });
  }

  function validStock(savedStock, settlement) {
    if (!savedStock || typeof savedStock !== "object") return false;
    const castle = isCastle(settlement);
    const availableItems = items.filter(item => purchasableItem(item, settlement));
    const blacksmithPool = availableItems.filter(item => (item.category === "weapon" || item.category === "armor") && !isClothing(item));
    const generalPool = availableItems.filter(item => item.category !== "weapon" && item.category !== "armor");
    const offeredMasterworks = masterworkOffers(settlement);
    const regionalOfferCount = blacksmithPool.filter(item => item.shopOffer).length + offeredMasterworks.length;
    const expectedBlacksmith = Math.min(smithPlan(settlement).total, new Set(blacksmithPool.map(item => item.id)).size + offeredMasterworks.length + forgedWeaponCount(settlement) + forgedArmorCount(settlement, regionalOfferCount));
    const expectedGeneral = Math.min(generalCount(settlement), new Set(generalPool.map(item => item.id)).size);

    const maximumConsumables = castle ? 6 : 4;
    if (!validItemEntries(savedStock.blacksmith, expectedBlacksmith, item => (item.category === "weapon" || item.category === "armor") && !item.forged?.caster && !isClothing(item), maximumConsumables, settlement)) return false;
    if (!validItemEntries(savedStock.tailor, tailorCount(settlement), item => isClothing(item) && Boolean(item.forged) && !item.forged.masterwork, maximumConsumables, settlement)) return false;
    if (blacksmithPool.some(item => item.shopOffer && !savedStock.blacksmith.some(entry => entry.id === item.id))) return false;
    if (offeredMasterworks.some(key => !savedStock.blacksmith.some(entry => getItem(entry.id)?.forged?.masterwork === key))) return false;
    const forgedStock = savedStock.blacksmith.map(entry => getItem(entry.id)).filter(item => item?.forged && !item.forged.masterwork);
    if (forgedStock.filter(item => item.category === "weapon").length !== forgedWeaponCount(settlement) || forgedStock.filter(item => item.category === "armor").length !== forgedArmorCount(settlement, regionalOfferCount)) return false;
    if (!validItemEntries(savedStock.general, expectedGeneral, item => item.category !== "weapon" && item.category !== "armor", maximumConsumables, settlement)) return false;
    if (!validItemEntries(savedStock.tower, towerCasterCount(settlement), item => Boolean(item.forged?.caster), maximumConsumables, settlement)) return false;
    const availableSpells = spells.filter(normallyPurchasableSpell);
    const overtierSpells = spells.filter(overtierPurchasableSpell);
    const expectedSpells = Math.min(towerSpellCount(settlement), new Set(availableSpells.map(spell => spell.id)).size);
    if (!Array.isArray(savedStock.spells) || savedStock.spells.length !== expectedSpells || new Set(savedStock.spells).size !== savedStock.spells.length) return false;
    const overtierIds = new Set(overtierSpells.map(spell => spell.id));
    const availableIds = new Set(availableSpells.map(spell => spell.id));
    return savedStock.spells.every(id => typeof id === "string" && (availableIds.has(id) || overtierIds.has(id))) &&
      savedStock.spells.filter(id => overtierIds.has(id)).length <= 1;
  }

  function stockFor(settlement) {
    if (!settlement?.id) return { blacksmith: [], tailor: [], general: [], tower: [], spells: [] };
    if (settlement.id === HEARTH.id) return hearthStock();
    normalizeState();
    const savedStock = shopState.locations[settlement.id];
    if (!validStock(savedStock, settlement)) generateStock(settlement);
    return shopState.locations[settlement.id];
  }

  function spellPrice(spell) {
    return calculateSpellPurchasePrice(spell);
  }

  /* ---------- Shared list and detail layout (also used by the Magic Tower) ---------- */
  // A small picture for a list row: forged pieces, runes and general goods are drawn.
  function itemArt(item, size = "small") {
    if (!item || !window.ForgeArt) return "";
    if (item.forged) return ForgeArt.weapon(item, size);
    if (["rune", "consumable", "artifact"].includes(item.category)) return ForgeArt.goods(item, size);
    return "";
  }
  // One line of a shop list. Hovering or focusing it shows the entry in the detail panel.
  function row({ id, art = "", name, meta = "", price = "", actions = "", className = "", previewed = false, extra = "" }) {
    return '<article class="shop-row ' + className + (previewed ? ' is-previewed' : '') + '" data-shop-entry="' + escapeHtml(id) + '" tabindex="0">' +
      '<span class="shop-row-art" aria-hidden="true">' + art + '</span>' +
      '<span class="shop-row-main"><strong>' + escapeHtml(name) + '</strong><small>' + meta + '</small>' + extra + '</span>' +
      (price ? '<b class="shop-row-price">' + escapeHtml(price) + '</b>' : '') +
      '<span class="shop-row-actions">' + actions + '</span></article>';
  }
  function bindPreview(list, onPreview) {
    const pick = event => {
      const entry = event.target.closest("[data-shop-entry]");
      if (entry && list.contains(entry)) onPreview(entry.dataset.shopEntry);
    };
    list.addEventListener("mouseover", pick);
    list.addEventListener("focusin", pick);
  }
  function markPreview(list, id) {
    list.querySelectorAll("[data-shop-entry]").forEach(entry => entry.classList.toggle("is-previewed", entry.dataset.shopEntry === id));
  }
  // The full item description, with a drawing for goods that the item panel does not draw itself.
  function itemDetail(item, note = "") {
    if (!item) return "";
    const art = !item.forged && item.category !== "rune" ? itemArt(item, "large") : "";
    return (note ? '<p class="shop-detail-note">' + note + '</p>' : '') + (art ? '<div class="forged-art goods-art" aria-hidden="true">' + art + '</div>' : '') + itemDetailMarkup(item);
  }
  function salePrice(item, bonus = Math.max(0, toNumber(calculateClassEffects().salePricePercent, 0)), treasure = treasureMoneyPercent()) {
    const base = ShopRules.salePrice(item?.priceSilver);
    const price = Math.max(0, Math.floor(base * (1 + bonus / 100)));
    // Treasure Seeker raises sales too, but never above the shop's own price for the item (task 5, M13).
    return treasure > 0 ? Math.min(Math.max(0, Math.floor(toNumber(item?.priceSilver, 0))), Math.floor(price * (1 + treasure / 100))) : price;
  }
  function saleTotals(entries) {
    const bonus = entries.length ? Math.max(0, toNumber(calculateClassEffects().salePricePercent, 0)) : 0;
    const treasure = entries.length ? treasureMoneyPercent() : 0;
    return entries.reduce((total, entry) => ({ quantity: total.quantity + entry.quantity, price: total.price + entry.quantity * salePrice(getItem(entry.id), bonus, treasure) }), { quantity: 0, price: 0 });
  }
  // Unequipped copies in the pack that a shop accepts, one entry per item.
  function sellableEntries(accepts) {
    const equippedCounts = requiredEquipmentCounts(equipment);
    const grouped = new Map();
    inventory.forEach(id => grouped.set(id, (grouped.get(id) || 0) + 1));
    return [...grouped.entries()].map(([id, quantity]) => ({ id, quantity: quantity - (equippedCounts[id] || 0) })).filter(entry => {
      const item = getItem(entry.id);
      return entry.quantity > 0 && accepts(item) && item.playerSellable !== false && Number.isFinite(item.priceSilver);
    });
  }
  // Removes the requested copies from the pack and pays for them; returns the totals sold.
  function sellFromPack(requestedEntries, accepts) {
    const available = new Map(sellableEntries(accepts).map(entry => [entry.id, entry.quantity]));
    const requested = new Map(requestedEntries.map(entry => [entry.id, Math.min(available.get(entry.id) || 0, Math.max(0, Math.trunc(entry.quantity)))]));
    const entries = [...requested].filter(([, quantity]) => quantity > 0).map(([id, quantity]) => ({ id, quantity }));
    const totals = saleTotals(entries);
    if (!totals.quantity) return totals;
    const remaining = new Map(entries.map(entry => [entry.id, entry.quantity]));
    inventory = inventory.filter(id => {
      const count = remaining.get(id) || 0;
      if (!count) return true;
      remaining.set(id, count - 1);
      return false;
    });
    currencies = ShopRules.walletFromValue(ShopRules.walletValue(currencies) + totals.price);
    itemsSold += totals.quantity;
    renderOwnedItems(); renderAvailableItems(); updateDerivedValues();
    renderClasses();
    return totals;
  }
  function sellRowFor(entry, previewed = false) {
    const item = getItem(entry.id);
    if (!item) return "";
    const meta = itemRarityTag(item.rarity) + ' ' + escapeHtml(itemCategoryLabel(item.category)) + itemTypeMetadata(item) + (isItemFavorite(item.id) ? ' · <b class="shop-favorite-status">★ Favorite</b>' : '') + ' · ×' + entry.quantity + ' sellable';
    return row({ id: item.id, art: itemArt(item), name: item.name, meta, price: ShopRules.formatPrice(salePrice(item)) + ' each', className: itemRarityClass(item), previewed,
      actions: '<button type="button" data-sell-item="' + escapeHtml(item.id) + '">Sell one</button><button type="button" data-sell-all-item="' + escapeHtml(item.id) + '">Sell all (' + entry.quantity + ')</button>' });
  }
  function itemTypeMetadata(item) {
    return item.category === "weapon" || item.category === "armor" || item.category === "rune" ? ' · ' + escapeHtml(itemKindLabel(item)) + (item.forged ? ' · Lv ' + item.forged.level : '') : "";
  }

  /* ---------- Blacksmith and general store ---------- */
  function itemBelongsHere(item, transaction) {
    if (!item || item.category === "rune") return false;
    const equipmentItem = item.category === "weapon" || item.category === "armor";
    // Staffs and wands are bought at the Magic Tower, but any smith buys them back; clothing is the Tailor's.
    return kind === "blacksmith" ? equipmentItem && !isClothing(item) : kind === "tailor" ? isClothing(item) : kind === "hearth" ? transaction === "buy" && equipmentItem && !isClothing(item) : kind === "general" ? !equipmentItem && (transaction === "sell" || item.category !== "artifact") : false;
  }

  function sellableInventory() {
    return sellableEntries(item => itemBelongsHere(item, "sell"));
  }

  function stockDescription() {
    if (kind === "blacksmith") return `${smithPlan(place).total} weapons and armor pieces are on the racks.` + (isCastle(place) ? " Staffs and wands are sold at the Magic Tower." : "") + " Clothing is the Tailor's.";
    if (kind === "tailor") return `${tailorCount(place)} pieces of clothing hang on the rails.`;
    if (kind === "hearth") return `${HEARTH.weapons + HEARTH.armor} Hearthforge weapons and armor pieces at 10% off. Its anvil reforges at 15% off and is the only one that takes masterworks.`;
    return `${generalCount(place)} wares for the road are on the shelves.`;
  }

  function render() {
    if (modal.hidden || !place) return;
    const current = currentPlace();
    if (!current || current.id !== place.id) { close(); return; }
    const stock = stockFor(place), shop = SHOPS[kind];
    $("shop-eyebrow").textContent = (kind === "hearth" ? "CINDER ISLE" : place.name.toUpperCase()) + " · " + shop.name;
    $("shop-title").innerHTML = shop.title;
    modal.querySelector(".shop-dialog").dataset.shop = kind;
    window.SceneBanners?.show($("shop-banner"), kind, place, worldState.regionId);
    $("shop-description").textContent = stockDescription();
    const nextRefresh = (shopState.refreshWindow + 1) * 360;
    $("shop-refresh").textContent = "Stock refreshes at Day " + WorldRules.day(nextRefresh) + " · " + WorldRules.time(nextRefresh);
    syncPurse();
    const reforging = kind !== "general";
    if (kind === "hearth" && mode === "sell") mode = "buy";
    $("shop-sell-mode").hidden = kind === "hearth";
    if (!reforging && mode === "reforge") mode = "buy";
    for (const [id, which] of [["shop-buy-mode", "buy"], ["shop-sell-mode", "sell"], ["shop-reforge-mode", "reforge"]]) {
      $(id).classList.toggle("is-active", mode === which);
      $(id).setAttribute("aria-pressed", String(mode === which));
    }
    $("shop-reforge-mode").hidden = !reforging;
    $("shop-workspace").dataset.mode = mode;
    if (mode === "reforge") {
      $("shop-sell-unfavorited").hidden = true;
      $("shop-list-title").textContent = "On the anvil";
      anvilFor(kind).render();
      return;
    }
    const entries = mode === "sell" ? sellableInventory() : stock[kind];
    const bulkEntries = mode === "sell" ? entries.filter(entry => !isItemFavorite(entry.id)) : [];
    const bulkTotals = saleTotals(bulkEntries);
    const bulkButton = $("shop-sell-unfavorited");
    bulkButton.hidden = mode !== "sell";
    bulkButton.disabled = !bulkEntries.length;
    bulkButton.textContent = bulkEntries.length ? "Sell all non-favorites · " + ShopRules.formatPrice(bulkTotals.price) : "Sell all non-favorites";
    bulkButton.title = bulkEntries.length ? "Sell " + bulkTotals.quantity + " non-favorited items for " + ShopRules.formatPrice(bulkTotals.price) + ". Equipped copies and items this shop cannot buy are kept." : "No eligible non-favorited items to sell here.";
    if (!entries.some(entry => entry.id === previewId)) previewId = entries[0]?.id || null;
    $("shop-list-title").textContent = mode === "sell" ? "Your pack" : kind === "blacksmith" ? "On the racks" : kind === "tailor" ? "On the rails" : "On the shelves";
    stockList.innerHTML = entries.length ? entries.map(entry => mode === "sell" ? sellRowFor(entry, entry.id === previewId) : buyRow(entry)).join("") : '<div class="shop-empty">' + (mode === "sell" ? "You have nothing this shop will buy." : "No stock is available.") + '</div>';
    renderDetail(entries);
  }

  function buyRow(entry) {
    const item = getItem(entry.id);
    if (!item) return "";
    const soldOut = entry.quantity <= 0;
    const quantity = item.category === "consumable" ? " · ×" + entry.quantity + " available" : soldOut ? "" : " · One available";
    const disabled = soldOut || !canAffordItem(item);
    const label = soldOut ? "Sold out" : disabled ? "Cannot afford" : "Buy";
    return row({ id: item.id, art: itemArt(item), name: item.name, price: purchasePriceText(item), className: itemRarityClass(item) + (soldOut ? ' is-sold-out' : ''), previewed: item.id === previewId,
      meta: itemRarityTag(item.rarity) + ' ' + escapeHtml(itemCategoryLabel(item.category)) + itemTypeMetadata(item) + quantity,
      actions: '<button type="button" data-buy-item="' + escapeHtml(item.id) + '"' + (disabled ? " disabled" : "") + '>' + label + '</button>' });
  }

  function renderDetail(entries = null) {
    const list = entries || (mode === "sell" ? sellableInventory() : stockFor(place)[kind]);
    const entry = list.find(candidate => candidate.id === previewId), item = getItem(previewId);
    if (!entry || !item) { detail.innerHTML = '<div class="shop-empty">' + (list.length ? "Hover over an entry to see it in full." : mode === "sell" ? "Nothing to sell here." : "Nothing on offer.") + '</div>'; return; }
    const note = mode === "sell"
      ? "Sells for " + escapeHtml(ShopRules.formatPrice(salePrice(item))) + " each · ×" + entry.quantity + " in your pack"
      : entry.quantity <= 0 ? "Sold out" : "Price " + escapeHtml(purchasePriceText(item)) + (item.category === "consumable" ? " · ×" + entry.quantity + " in stock" : "");
    detail.innerHTML = itemDetail(item, note);
    detail.scrollTop = 0;
  }

  function preview(id) {
    if (mode === "reforge" || id === previewId) return;
    previewId = id;
    markPreview(stockList, id);
    renderDetail();
  }

  function pay(price) {
    const remaining = ShopRules.spend(currencies, price);
    if (!remaining) return false;
    currencies = remaining;
    syncPurse();
    return true;
  }
  function tradeItemCount(item) {
    const id = item.shopOffer?.costItemId;
    return id ? inventory.filter(owned => owned === id).length - (requiredEquipmentCounts(equipment)[id] || 0) : 0;
  }
  function canAffordItem(item) {
    return item.shopOffer ? tradeItemCount(item) >= item.shopOffer.costQuantity : ShopRules.walletValue(currencies) >= priceFor(item);
  }
  function purchasePriceText(item) {
    return kind === "hearth" && !item.shopOffer ? ShopRules.formatPrice(priceFor(item)) : itemPurchasePriceLabel(item);
  }
  function payForItem(item) {
    if (!item.shopOffer) return pay(priceFor(item));
    if (!canAffordItem(item)) return false;
    let remaining = item.shopOffer.costQuantity;
    inventory = inventory.filter(id => {
      if (id !== item.shopOffer.costItemId || remaining <= 0) return true;
      remaining--; return false;
    });
    if (!inventory.includes(item.shopOffer.costItemId)) favoriteItems = favoriteItems.filter(id => id !== item.shopOffer.costItemId);
    return true;
  }
  const busy = () => worldState.pendingEncounter || window.Combat?.active() || window.WorldMap?.isTravelling();

  function buyItem(itemId) {
    const current = currentPlace();
    if (modal.hidden || mode !== "buy" || !current || current.id !== place?.id || busy()) return;
    const stock = stockFor(place), entry = stock[kind]?.find(candidate => candidate.id === itemId), item = getItem(itemId);
    if (!entry || entry.quantity <= 0 || !purchasableItem(item, place) || !itemBelongsHere(item, "buy") || !payForItem(item)) { setStatus(item?.shopOffer ? `This purchase requires ${purchasePriceText(item)}.` : "You do not have enough coin for that purchase.", "error"); return; }
    entry.quantity -= 1;
    inventory.push(item.id);
    renderOwnedItems(); renderAvailableItems(); updateDerivedValues();
    setStatus(item.name + " purchased for " + purchasePriceText(item) + ".", "success");
    saveCharacter(); render(); window.WorldMap?.render();
  }

  function completeSale(requestedEntries) {
    const current = currentPlace();
    if (modal.hidden || mode !== "sell" || !current || current.id !== place?.id || window.Combat?.active() || window.WorldMap?.isTravelling()) return;
    const totals = sellFromPack(requestedEntries, item => itemBelongsHere(item, "sell"));
    if (!totals.quantity) { setStatus("Those items cannot be sold here.", "error"); return; }
    setStatus("Sold " + totals.quantity + (totals.quantity === 1 ? " item" : " items") + " for " + ShopRules.formatPrice(totals.price) + ".", "success");
    saveCharacter(); render(); window.WorldMap?.render();
  }

  function sellItem(itemId) {
    completeSale([{ id: itemId, quantity: 1 }]);
  }

  function sellAllCopies(itemId) {
    const entry = sellableInventory().find(candidate => candidate.id === itemId), item = getItem(itemId);
    if (!entry || !item) return;
    const totals = saleTotals([entry]);
    const favoriteWarning = isItemFavorite(itemId) ? "This item is favorited. " : "";
    if (!window.confirm(favoriteWarning + "Sell all " + entry.quantity + " unequipped copies of " + item.name + " for " + ShopRules.formatPrice(totals.price) + "? Equipped copies will be kept.")) return;
    completeSale([entry]);
  }

  function sellAllNonFavorites() {
    const entries = sellableInventory().filter(entry => !isItemFavorite(entry.id));
    const totals = saleTotals(entries);
    if (!totals.quantity) return;
    if (!window.confirm("Sell all " + totals.quantity + " non-favorited items to the " + SHOPS[kind].label + " for " + ShopRules.formatPrice(totals.price) + "? Favorites, equipped copies, and items this shop cannot buy will be kept.")) return;
    completeSale(entries);
  }

  function open(shopKind) {
    if (busy()) return;
    kind = SHOPS[shopKind] ? shopKind : "general";
    place = currentPlace();
    if (!place) return;
    mode = "buy"; previewId = null;
    Object.values(anvils).forEach(entry => entry.reset());
    modal.hidden = false;
    setStatus(""); render(); updateModalState();
    modal.querySelector(".modal-close").focus();
  }

  function close() {
    modal.hidden = true; updateModalState();
    (kind === "blacksmith" ? $("map-blacksmith") : kind === "tailor" ? $("map-tailor") : kind === "hearth" ? $("map-hearth-home") : $("map-item-shop"))?.focus();
  }
  // The Blacksmith's and the Tailor's reforge menus (port task 3): the anvil on the right, the list on the left.
  const anvils = {};
  function anvilFor(shopKind) {
    const key = shopKind === "tailor" ? "tailor" : shopKind === "hearth" ? "hearth" : "smith";
    if (!anvils[key]) anvils[key] = window.Reforge.create({
      anvil: key, discount: key === "hearth" ? HEARTH.reforgeDiscount : 0, list: stockList, stage: detail, setStatus,
      available: () => !modal.hidden && mode === "reforge" && !busy() && currentPlace()?.id === place?.id,
      onChange: () => { renderOwnedItems(); renderAvailableItems(); updateDerivedValues(); window.Companions?.render(true); syncPurse(); saveCharacter(); window.WorldMap?.render(); },
    });
    return anvils[key];
  }

  bindPreview(stockList, preview);
  stockList.addEventListener("click", event => {
    const buy = event.target.closest("[data-buy-item]");
    if (buy) { buyItem(buy.dataset.buyItem); return; }
    const one = event.target.closest("[data-sell-item]");
    if (one) { sellItem(one.dataset.sellItem); return; }
    const all = event.target.closest("[data-sell-all-item]");
    if (all) sellAllCopies(all.dataset.sellAllItem);
  });
  $("map-blacksmith").addEventListener("click", () => open("blacksmith"));
  $("map-item-shop").addEventListener("click", () => open("general"));
  $("map-tailor").addEventListener("click", () => open("tailor"));
  $("shop-reforge-mode").addEventListener("click", () => { mode = "reforge"; previewId = null; setStatus(""); anvilFor(kind).reset(); render(); });
  $("shop-buy-mode").addEventListener("click", () => { mode = "buy"; previewId = null; setStatus(""); render(); });
  $("shop-sell-mode").addEventListener("click", () => { mode = "sell"; previewId = null; setStatus(""); render(); });
  $("shop-sell-unfavorited").addEventListener("click", sellAllNonFavorites);
  modal.querySelectorAll("[data-shop-close]").forEach(button => button.addEventListener("click", close));
  modal.addEventListener("keydown", event => { if (event.key === "Escape") { event.preventDefault(); close(); } });

  window.ShopLayout = { row, bindPreview, markPreview, itemArt, itemDetail, itemTypeMetadata, salePrice, saleTotals, sellableEntries, sellFromPack, sellRowFor };
  window.Shop = {
    open, close, render, anvil: () => mode === "reforge" ? anvilFor(kind) : null, isClothing, hearth: { open: hearthOpen, stock: hearthStock, price: item => Math.round(item.priceSilver * HEARTH.buyFactor), bossId: HEARTH.bossId },
    onTimeChange() { const previous = shopState.refreshWindow; normalizeState(); if (previous !== shopState.refreshWindow) { if (!modal.hidden) render(); window.MagicTower?.render(); saveCharacter(); } },
    onCatalogChange() {
      if (!spells.length) Object.values(shopState.locations || {}).forEach(stock => { stock.spells = []; });
      if (!modal.hidden) render();
      window.MagicTower?.render();
    },
    onCharacterApplied() { normalizeState(); if (!modal.hidden) close(); },
    stockFor, purchasableItem, purchasableSpell, spellPrice, normalize: normalizeState,
  };
})();

/* A fixed wilderness boss arena; not a town or Guild quest. Once Tankina has fallen, the
   Church offers respecs and opens the Storm Reliquary, its own store. */
(() => {
  "use strict";
  const $ = id => document.getElementById(id);
  const BOSS_ID = "tankina-disciple-of-talos-level-44";
  const ENEMY_ROSTER = ["Tankina, Disciple of Talos", "Apostle of Talos", "Apostle of Talos"];
  const modal = document.createElement("div");
  modal.id = "church-of-talos-modal"; modal.className = "modal-layer"; modal.hidden = true;
  modal.innerHTML = `<section class="spellbook-dialog church-panel" role="dialog" aria-modal="true" aria-labelledby="church-title">
    <header class="dialog-header"><div><p class="eyebrow">STORMCREST · LEVEL 44</p><h2 id="church-title">Church of <em>Talos</em></h2></div><button class="modal-close" id="church-close" type="button" aria-label="Close Church of Talos">×</button></header>
    <div class="church-content"><p id="church-description"></p><button class="backup-button" id="church-challenge" type="button">Challenge Tankina · Level 44</button>
    <p id="church-service-description" hidden>Leave an offering to reclaim your spent points. Reallocate them on your character sheet. Previously fulfilled “other” class requirements remain fulfilled; character-level and class prerequisites still apply.</p>
    <div class="map-actions" id="church-services" hidden><button class="backup-button" id="church-class-respec" type="button">Respec Classes</button><button class="backup-button" id="church-stat-respec" type="button">Respec Stats</button><button class="backup-button" id="church-open-store" type="button">Storm Reliquary</button></div>
    <p id="church-status" role="status" aria-live="polite"></p></div>
  </section>`;
  document.body.appendChild(modal);
  let starting = false;
  const site = () => WorldRules.siteAt(worldState.position, worldState.regionId);
  const available = () => site()?.id === "church-of-talos" && !Combat.active() && !WorldMap.isTravelling() && !worldState.pendingEncounter && !worldState.pendingRegionTransition && currentHp > 0;
  const unlocked = () => worldState.defeatedSiteBosses?.includes(BOSS_ID);
  const price = () => site()?.servicePriceSilver || 12000;
  const spentClasses = () => Object.values(classLevels).reduce((sum, rank) => sum + Math.max(0, toInteger(rank, 0)), 0);
  function serviceReason(kind) {
    if (!available() || starting) return "You must be at the Church, outside combat and travel.";
    if (!unlocked()) return "Defeat Tankina to unlock this service.";
    if (ShopRules.walletValue(currencies) < price()) return "Not enough money for the offering.";
    if ((kind === "classes" ? spentClasses() : totalManualAllocations()) === 0) return "No spent points to refund.";
    return "";
  }
  function render() {
    $("church-description").textContent = unlocked() ? "The storm altar is quiet. Its offerings can reshape your training, and the Storm Reliquary sells the Church's staffs, wands and lightning spells. You may also challenge Tankina again." : "Tankina, Disciple of Talos, guards the storm altar with two Apostles of Talos. Every lightning spell cast before her altar, even your own, feeds a gathering storm, and the storm makes her cult stronger. At the height of the fight, Talos answers her.";
    $("church-challenge").disabled = !available() || starting;
    $("church-open-store").disabled = !storeOpen();
    $("church-services").hidden = !unlocked(); $("church-service-description").hidden = !unlocked();
    for (const [id, kind] of [["church-class-respec", "classes"], ["church-stat-respec", "stats"]]) {
      $(id).disabled = Boolean(serviceReason(kind));
      $(id).title = serviceReason(kind);
      $(id).textContent = (kind === "classes" ? "Respec Classes" : "Respec Stats") + " · " + ShopRules.formatPrice(price());
    }
  }
  function close() { modal.hidden = true; updateModalState(); $("map-church").focus({ preventScroll: true }); }
  function refreshSheet() {
    document.querySelectorAll(".currency-input").forEach(input => { input.value = currencies[input.dataset.currency]; });
    for (const definition of equipmentSlotDefinitions) {
      const item = getItem(equipment[definition.key]);
      if (definition.key === "offHand" && item?.handed === 2 && equipment.primaryHand === item.id) continue;
      if (item && !canEquipItem(item, definition.key)) {
        for (const slot of equipmentSlotDefinitions) if (equipment[slot.key] === item.id) equipment[slot.key] = null;
      }
    }
    createStatCards(); renderClasses(); renderSkills(); renderPassives(); renderOwnedItems(); updateDerivedValues();
    WorldMap.render(); render(); saveCharacter();
  }
  function respec(kind) {
    if (!["classes", "stats"].includes(kind) || serviceReason(kind)) return false;
    const cost = price(), points = kind === "classes" ? spentClasses() : totalManualAllocations();
    if (!window.confirm(`Offer ${ShopRules.formatPrice(cost)} to refund ${points} ${kind === "classes" ? "perk" : "stat"} points? ${kind === "classes" ? "All classes will be removed. Fulfilled other requirements and learned spells are retained." : "Only manually allocated stat points are reset."}`)) return false;
    const choiceRefunds = { ...(classProgress.statChoiceBonuses || {}) };
    if (kind === "classes") for (const definition of classCatalog) for (const rank of definition.levels) {
      if (rank.level > toInteger(classLevels[definition.id], 0)) continue;
      for (const effect of rank.effects || []) {
        if (effect.type !== "choose-stat-permanent-percent") continue;
        const key = `${definition.id}:${rank.level}:${effect.id || rank.effects.indexOf(effect)}`;
        if (choiceRefunds[key]) continue;
        // Legacy saves predate the choice ledger. Ask which stat was chosen;
        // never guess and subtract an unrelated manually entered bonus.
        const choices = (effect.options || []).filter(stat => bonusDefinitions.includes(stat));
        const answer = window.prompt(`${definition.name} level ${rank.level}: which stat received the +${effect.amount}% choice bonus? This older save did not record the selection. Enter ${choices.join(" or ")}; Cancel keeps your character unchanged.`, choices[0]);
        const selected = choices.find(stat => slugify(stat) === slugify(answer || ""));
        if (!selected) { $("church-status").textContent = "Offering cancelled: the previous class bonus selection is needed to reset it safely."; return false; }
        choiceRefunds[key] = { stat: selected, amount: toNumber(effect.amount, 0) };
      }
    }
    if (serviceReason(kind)) return false;
    const wallet = ShopRules.spend(currencies, cost);
    if (!wallet) return false;
    currencies = wallet;
    if (kind === "classes") {
      classProgress.fulfilledClassOtherRequirements = [...new Set([...(classProgress.fulfilledClassOtherRequirements || []), ...Object.keys(classLevels).filter(id => classLevels[id] > 0 && getClass(id)?.requirements.other && slugify(getClass(id).requirements.other.type || getClass(id).requirements.other) !== "unobtainable")])];
      for (const bonus of Object.values(choiceRefunds)) if (bonusDefinitions.includes(bonus.stat)) permanentBonuses[bonus.stat] -= bonus.amount;
      classProgress.statChoiceBonuses = {};
      classLevels = {}; perkPoints += points;
      for (const state of Object.values(activeSkills)) state.active = false;
      pendingClassSpellChoice = null;
    } else {
      for (const { key } of statDefinitions) { stats[key] -= manualAllocations[key]; manualAllocations[key] = 0; }
      availablePoints += points;
    }
    $("church-status").textContent = `Offering accepted. ${points} ${kind === "classes" ? "perk" : "stat"} points are available to reallocate.`;
    $("map-status").textContent = $("church-status").textContent;
    refreshSheet(); return true;
  }
  async function challenge() {
    if (!available() || starting) return false;
    starting = true; render();
    try {
      const result = await Combat.startEncounter([...ENEMY_ROSTER], "At the Church of Talos, the thunder cult answers your challenge.");
      if (result) { modal.hidden = true; $("map-modal").hidden = true; updateModalState(); }
      return Boolean(result);
    } catch (error) { $("church-status").textContent = error.message; return false; }
    finally { starting = false; render(); WorldMap.render(); }
  }
  /* ---------- The Storm Reliquary ---------- */
  // The Church's store: its own staffs and wands, set with storm sapphires and forged at your
  // level; a rotating selection of lightning spells; and the Stormhide Cuirass, a masterwork forged
  // at your level, for a fortune. Its stock turns over with the settlements' shops.
  const STORE_KEY = "church-of-talos", STORE = { casters: 6, spells: 8, overtierChance: 0.1 };
  const CUIRASS_KEY = "stormhide-cuirass", CUIRASS_PRICE = 20 * ShopRules.SILVER_PER_PLATINUM;
  const storeModal = $("church-store-modal"), storeList = $("church-store-list"), storeDetail = $("church-store-detail");
  const STORE_MENUS = {
    arms: { title: "Staffs, wands and relics", description: "Staffs and wands set with the Church's storm sapphires, forged at your level, and the Stormhide Cuirass, which the Church parts with only for a fortune." },
    spells: { title: "Lightning spells", description: "Lightning spells the priests of Talos will teach. Their selection changes with the stock." },
  };
  let storeMenu = "arms", storePreview = null;
  const storeOpen = () => available() && unlocked();
  const unitRandom = () => Math.min(1 - Number.EPSILON, Math.max(0, Number(Math.random()) || 0));
  const lightning = spell => [spell?.element].flat().includes("lightning");
  const tierLevel = spell => Math.max(1, (Math.max(1, Math.trunc(Number(spell?.tier) || 1)) - 1) * 10);
  const teachable = spell => Boolean(spell && lightning(spell) && spell.shopAvailable !== false && Number.isFinite(spell.priceSilver));
  const ordinarySpell = spell => teachable(spell) && level >= tierLevel(spell);
  const overtierSpell = spell => teachable(spell) && tierLevel(spell) > level && tierLevel(spell) <= level + 10;
  const churchCaster = item => Boolean(item?.forged?.caster && item.forged.maker === "church-of-talos" && ForgeRules.STONES[item.forged.parts[0].material]?.element === "lightning");
  function makeStoreStock() {
    const casters = [];
    for (let attempt = 0; casters.length < STORE.casters && attempt < STORE.casters * 10; attempt++) {
      const item = getItem(ForgeRules.encode(ForgeRules.generate({ level, source: "shop", random: unitRandom, casterShare: 1, element: "lightning", maker: "church-of-talos" })));
      // Every Church caster is a Sapphire Staff or Wand; they differ in their gem's cut and their handle.
      if (churchCaster(item) && !casters.some(entry => entry.id === item.id)) casters.push(item);
    }
    const chosen = ShopRules.spellStock(spells.filter(ordinarySpell), STORE.spells, level, unitRandom);
    const overtier = spells.filter(overtierSpell);
    if (chosen.length && overtier.length && unitRandom() < STORE.overtierChance) chosen[chosen.length - 1] = ShopRules.spellStock(overtier, 1, level, unitRandom)[0];
    const cuirass = getItem(ForgeRules.encode(ForgeRules.generateMasterwork({ key: CUIRASS_KEY, level, random: unitRandom })));
    return { relics: cuirass ? [{ id: cuirass.id, quantity: 1 }] : [], tower: casters.map(item => ({ id: item.id, quantity: 1 })), spells: chosen.filter(Boolean).map(spell => spell.id) };
  }
  function validStoreStock(stock) {
    if (!stock || !Array.isArray(stock.tower) || !Array.isArray(stock.spells) || stock.tower.length !== STORE.casters) return false;
    if (!Array.isArray(stock.relics) || stock.relics.length !== 1 || getItem(stock.relics[0].id)?.forged?.masterwork !== CUIRASS_KEY) return false;
    if (!stock.tower.every(entry => churchCaster(getItem(entry.id)) && Number.isInteger(entry.quantity) && entry.quantity >= 0 && entry.quantity <= 1)) return false;
    const expected = Math.min(STORE.spells, spells.filter(ordinarySpell).length);
    return stock.spells.length === expected && new Set(stock.spells).size === expected
      && stock.spells.every(id => ordinarySpell(getSpell(id)) || overtierSpell(getSpell(id))) && stock.spells.filter(id => overtierSpell(getSpell(id))).length <= 1;
  }
  // The shops' refresh resets every location's stock, the Reliquary's included.
  function storeStock() {
    window.Shop?.onTimeChange();
    if (!validStoreStock(shopState.locations[STORE_KEY])) { shopState.locations[STORE_KEY] = makeStoreStock(); saveCharacter(); }
    return shopState.locations[STORE_KEY];
  }
  function storeStatus(message, state = "") {
    $("church-store-status").textContent = message;
    $("church-store-status").className = "shop-status" + (state ? " is-" + state : "");
  }
  function storePay(price) {
    const remaining = ShopRules.spend(currencies, price);
    if (!remaining) return false;
    currencies = remaining;
    return true;
  }
  function afterPurchase() {
    document.querySelectorAll(".currency-input").forEach(input => { input.value = currencies[input.dataset.currency]; });
    renderEquipmentSlots(); renderOwnedItems(); renderAvailableItems(); updateDerivedValues();
    saveCharacter(); renderStore(); WorldMap.render();
  }
  function buyStoreItem(id) {
    if (!storeOpen() || storeModal.hidden) return false;
    const item = getItem(id), stock = storeStock(), relic = stock.relics.some(candidate => candidate.id === id);
    if (!item) return false;
    // The Cuirass is always on offer; each staff and wand sells once.
    const entry = relic ? null : stock.tower.find(candidate => candidate.id === id);
    if (!relic && (!entry || entry.quantity <= 0)) return false;
    const price = relic ? CUIRASS_PRICE : item.priceSilver;
    if (!storePay(price)) { storeStatus("You do not have enough coin for that purchase.", "error"); return false; }
    if (entry) entry.quantity -= 1;
    inventory.push(item.id);
    storeStatus(item.name + " purchased for " + ShopRules.formatPrice(price) + ".", "success");
    afterPurchase(); return true;
  }
  function learnStoreSpell(id) {
    if (!storeOpen() || storeModal.hidden) return false;
    const spell = getSpell(id);
    if (!spell || !storeStock().spells.includes(id) || knownSpells.includes(id) || !(ordinarySpell(spell) || overtierSpell(spell))) return false;
    const price = Shop.spellPrice(spell);
    if (!storePay(price)) { storeStatus("You do not have enough coin to learn that spell.", "error"); return false; }
    knownSpells.push(spell.id);
    renderKnownSpells(); renderAvailableSpells(); renderClasses();
    storeStatus(spell.name + " learned for " + ShopRules.formatPrice(price) + ".", "success");
    afterPurchase(); return true;
  }
  function storeEntries() {
    const layout = ShopLayout, wallet = ShopRules.walletValue(currencies), stock = storeStock();
    if (storeMenu === "spells") return stock.spells.map(id => {
      const spell = getSpell(id);
      if (!spell) return null;
      const known = knownSpells.includes(id), price = Shop.spellPrice(spell), disabled = known || wallet < price;
      return { id, markup: layout.row({ id, name: spell.name, price: ShopRules.formatPrice(price), className: "spell-element-" + escapeHtml(spell.element) + (known ? " is-sold-out" : ""), previewed: id === storePreview,
        art: '<span class="tower-spell-glyph" aria-hidden="true">ϟ</span>', meta: escapeHtml(spellElementLabel(spell.element)) + " · " + escapeHtml(spellTierLabel(spell.tier)) + " · " + escapeHtml(spell.type),
        actions: '<button type="button" data-church-store-learn="' + escapeHtml(id) + '"' + (disabled ? " disabled" : "") + ">" + (known ? "Known" : disabled ? "Cannot afford" : "Learn") + "</button>" }) };
    }).filter(Boolean);
    const rows = [];
    for (const relic of stock.relics) {
      const cuirass = getItem(relic.id);
      if (cuirass) rows.push({ id: cuirass.id, markup: layout.row({ id: cuirass.id, art: layout.itemArt(cuirass), name: cuirass.name, price: ShopRules.formatPrice(CUIRASS_PRICE), className: itemRarityClass(cuirass), previewed: storePreview === cuirass.id,
        meta: itemRarityTag(cuirass.rarity) + " Armor · " + escapeHtml(cuirass.forged.typeName) + " · Lv " + cuirass.forged.level + " · Always on offer",
        actions: '<button type="button" data-church-store-buy="' + escapeHtml(cuirass.id) + '"' + (wallet < CUIRASS_PRICE ? " disabled" : "") + ">" + (wallet < CUIRASS_PRICE ? "Cannot afford" : "Buy") + "</button>" }) });
    }
    for (const entry of stock.tower) {
      const item = getItem(entry.id);
      if (!item) continue;
      const soldOut = entry.quantity <= 0, disabled = soldOut || wallet < item.priceSilver;
      rows.push({ id: item.id, markup: layout.row({ id: item.id, art: layout.itemArt(item), name: item.name, price: ShopRules.formatPrice(item.priceSilver), className: itemRarityClass(item) + (soldOut ? " is-sold-out" : ""), previewed: item.id === storePreview,
        meta: itemRarityTag(item.rarity) + " Weapon · " + escapeHtml(item.forged.typeName) + " · Lv " + item.forged.level + " · " + escapeHtml(item.forged.tierName) + " " + escapeHtml(ForgeRules.STONES[item.forged.parts[0].material].name) + ", " + escapeHtml(ForgeRules.CASTER_HANDLES[item.forged.parts[1].material].name) + (soldOut ? "" : " · One available"),
        actions: '<button type="button" data-church-store-buy="' + escapeHtml(item.id) + '"' + (disabled ? " disabled" : "") + ">" + (soldOut ? "Sold out" : disabled ? "Cannot afford" : "Buy") + "</button>" }) });
    }
    return rows;
  }
  function renderStoreDetail() {
    let markup = "";
    if (storeMenu === "spells") { const spell = getSpell(storePreview); if (spell) markup = '<p class="shop-detail-note">' + (knownSpells.includes(spell.id) ? "Already known" : "Price " + escapeHtml(ShopRules.formatPrice(Shop.spellPrice(spell)))) + "</p>" + spellDetailMarkup(spell); }
    else { const item = getItem(storePreview); if (item) markup = ShopLayout.itemDetail(item, "Price " + escapeHtml(ShopRules.formatPrice(item.forged?.masterwork === CUIRASS_KEY ? CUIRASS_PRICE : item.priceSilver))); }
    storeDetail.innerHTML = markup || '<div class="shop-empty">Hover over an entry to see it in full.</div>';
    storeDetail.scrollTop = 0;
  }
  function renderStore() {
    if (storeModal.hidden) return;
    if (!storeOpen()) { closeStore(); return; }
    window.SceneBanners?.show($("church-store-scene"), "church", { id: STORE_KEY, name: "Church of Talos" }, worldState.regionId);
    $("church-store-purse").textContent = ShopRules.formatPrice(ShopRules.walletValue(currencies));
    storeModal.querySelectorAll("[data-church-store-menu]").forEach(button => { const active = button.dataset.churchStoreMenu === storeMenu; button.classList.toggle("is-active", active); button.setAttribute("aria-pressed", String(active)); });
    $("church-store-description").textContent = STORE_MENUS[storeMenu].description;
    const nextRefresh = (shopState.refreshWindow + 1) * 360;
    $("church-store-refresh").textContent = "Stock refreshes at Day " + WorldRules.day(nextRefresh) + " · " + WorldRules.time(nextRefresh);
    $("church-store-list-title").textContent = STORE_MENUS[storeMenu].title;
    const entries = storeEntries();
    if (!entries.some(entry => entry.id === storePreview)) storePreview = entries[0]?.id || null;
    storeList.innerHTML = entries.length ? entries.map(entry => entry.markup).join("") : '<div class="shop-empty">Nothing is on offer right now.</div>';
    renderStoreDetail();
  }
  function openStore() {
    if (!storeOpen()) return false;
    modal.hidden = true;
    storeModal.hidden = false; storeMenu = "arms"; storePreview = null;
    storeStatus(""); renderStore(); updateModalState();
    storeModal.querySelector(".modal-close").focus();
    return true;
  }
  function closeStore() {
    storeModal.hidden = true; updateModalState();
    (modal.hidden ? $("map-church-store") : $("church-open-store"))?.focus({ preventScroll: true });
  }
  let storeBound = false;
  function bindStore() {
    if (storeBound || !window.ShopLayout) return;
    storeBound = true;
    ShopLayout.bindPreview(storeList, id => { if (id === storePreview) return; storePreview = id; ShopLayout.markPreview(storeList, id); renderStoreDetail(); });
  }
  storeModal.addEventListener("click", event => {
    const menuButton = event.target.closest("[data-church-store-menu]");
    if (menuButton) { storeMenu = menuButton.dataset.churchStoreMenu; storePreview = null; storeStatus(""); renderStore(); return; }
    const buy = event.target.closest("[data-church-store-buy]");
    if (buy) { buyStoreItem(buy.dataset.churchStoreBuy); return; }
    const learn = event.target.closest("[data-church-store-learn]");
    if (learn) learnStoreSpell(learn.dataset.churchStoreLearn);
  });
  storeModal.querySelectorAll("[data-church-store-close]").forEach(button => button.addEventListener("click", closeStore));
  storeModal.addEventListener("keydown", event => { if (event.key === "Escape") { event.preventDefault(); closeStore(); } });
  for (const id of ["church-open-store", "map-church-store"]) $(id).addEventListener("click", () => { bindStore(); openStore(); });

  $("map-church").addEventListener("click", () => { if (!available()) return; $("church-status").textContent = ""; modal.hidden = false; render(); updateModalState(); $("church-challenge").focus(); });
  $("church-close").addEventListener("click", close);
  $("church-challenge").addEventListener("click", challenge);
  for (const id of ["map-class-respec", "church-class-respec"]) $(id).addEventListener("click", () => respec("classes"));
  for (const id of ["map-stat-respec", "church-stat-respec"]) $(id).addEventListener("click", () => respec("stats"));
  modal.addEventListener("click", event => { if (event.target === modal) close(); });
  modal.addEventListener("keydown", event => {
    if (event.key === "Escape") { event.preventDefault(); event.stopPropagation(); close(); }
    if (event.key === "Tab") {
      const buttons = [...modal.querySelectorAll("button:not([disabled])")].filter(button => button.getClientRects().length);
      const first = buttons[0], last = buttons.at(-1);
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
      if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
    }
  });
  window.ChurchOfTalos = { enemyRoster: () => [...ENEMY_ROSTER], serviceReason, respec, challenge,
    store: { open: () => { bindStore(); return openStore(); }, close: closeStore, stock: storeStock, buy: buyStoreItem, learn: learnStoreSpell, available: storeOpen, cuirass: () => storeStock().relics[0]?.id || null, cuirassPrice: CUIRASS_PRICE },
    onVictory(state) {
    if (!state?.actors.some(actor => actor.enemyDefinitionId === BOSS_ID && !actor.summoned && actor.character.currentHp <= 0)) return;
    worldState.defeatedSiteBosses = [...new Set([...(worldState.defeatedSiteBosses || []), BOSS_ID])];
    $("map-status").textContent = "Tankina has fallen. Class and stat respec offerings and the Storm Reliquary are now open at the Church of Talos.";
  } };
  WorldMap.render();
})();

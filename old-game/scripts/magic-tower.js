/* The Magic Tower of a castle city, in five menus: forged staffs and wands, spells, runes,
   rune fitting and selling runes. The buying menus and the rune sale use the shop layout
   (shop.js); fitting happens in a magic circle. Click its centre to choose a forged weapon
   or armor piece with rune slots, click an empty slot on the ring to place one of your
   runes, then pay the runecarvers to fit them. A piece with fitted runes can have all of
   them removed again; the runes return to your pack. */
(() => {
  const $ = id => document.getElementById(id);
  const modal = $("magic-tower-modal");
  if (!modal || !window.ForgeRules) return;
  const F = window.ForgeRules;
  const list = $("magic-tower-list"), detail = $("magic-tower-detail"), circle = $("magic-tower-circle"), popup = $("magic-tower-popup");
  const MENUS = {
    casters: { title: "Staffs and wands", description: "Forged spell staffs and wands, made at your level." },
    spells: { title: "Spells", description: "Spells the tower's masters can teach you." },
    runes: { title: "Runes", description: "Every rune, carved to order. Runes go to your pack until they are fitted." },
    fitting: { title: "Rune fitting", description: "Fitting costs a quarter of each rune's price. Removing a piece's runes costs three times their prices and returns them to your pack." },
    sell: { title: "Your runes", description: "The runecarvers buy runes back at half price." },
  };
  let place = null, menu = "casters", previewId = null;
  // The rune circle: the piece in the middle, runes placed but not yet fitted, and what the left list shows.
  let centerId = null, pending = [], choosing = null;

  const currentPlace = () => WorldRules.settlementAt(worldState.position);
  const towerHere = () => { const found = currentPlace(); return found?.kind === "Castle city" ? found : null; };
  const blocked = () => worldState.pendingEncounter || window.Combat?.active() || window.WorldMap?.isTravelling();
  const layout = () => window.ShopLayout;

  function setStatus(message, state = "") {
    $("magic-tower-status").textContent = message;
    $("magic-tower-status").className = "shop-status" + (state ? " is-" + state : "");
  }
  function syncPurse() {
    document.querySelectorAll(".currency-input").forEach(input => { input.value = currencies[input.dataset.currency]; });
    $("magic-tower-purse").textContent = ShopRules.formatPrice(ShopRules.walletValue(currencies));
  }
  const wallet = () => ShopRules.walletValue(currencies);
  function pay(price) {
    const remaining = ShopRules.spend(currencies, price);
    if (!remaining) return false;
    currencies = remaining;
    return true;
  }
  function afterChange() {
    syncPurse();
    renderEquipmentSlots(); renderOwnedItems(); renderAvailableItems(); updateDerivedValues();
    saveCharacter();
    render();
    window.WorldMap?.render();
  }
  const runeTextFor = (item, runeId) => item?.forged?.armor ? F.armorRuneText(runeId) : F.runeText(runeId);
  const ownedCount = id => inventory.filter(owned => owned === id).length;

  /* ---------- Buying: staffs and wands, spells, runes ---------- */
  function stock() { return Shop.stockFor(place); }
  function casterRow(entry) {
    const item = getItem(entry.id);
    if (!item) return "";
    const soldOut = entry.quantity <= 0, disabled = soldOut || wallet() < item.priceSilver;
    return layout().row({ id: item.id, art: layout().itemArt(item), name: item.name, price: ShopRules.formatPrice(item.priceSilver), className: itemRarityClass(item) + (soldOut ? " is-sold-out" : ""), previewed: item.id === previewId,
      meta: itemRarityTag(item.rarity) + " Weapon · " + escapeHtml(item.forged.typeName) + " · Lv " + item.forged.level + (soldOut ? "" : " · One available"),
      actions: '<button type="button" data-tower-buy-caster="' + escapeHtml(item.id) + '"' + (disabled ? " disabled" : "") + ">" + (soldOut ? "Sold out" : disabled ? "Cannot afford" : "Buy") + "</button>" });
  }
  function spellRow(id) {
    const spell = getSpell(id);
    if (!spell) return "";
    const known = knownSpells.includes(spell.id), price = Shop.spellPrice(spell), disabled = known || wallet() < price;
    return layout().row({ id: spell.id, name: spell.name, price: ShopRules.formatPrice(price), className: "spell-element-" + escapeHtml(spell.element) + (known ? " is-sold-out" : ""), previewed: spell.id === previewId,
      art: '<span class="tower-spell-glyph" aria-hidden="true">✧</span>',
      meta: escapeHtml(spellElementLabel(spell.element)) + " · " + escapeHtml(spellTierLabel(spell.tier)) + " · " + escapeHtml(spell.type),
      actions: '<button type="button" data-tower-learn="' + escapeHtml(spell.id) + '"' + (disabled ? " disabled" : "") + ">" + (known ? "Known" : disabled ? "Cannot afford" : "Learn") + "</button>" });
  }
  function runeRow(runeId) {
    const item = getItem(F.runeItemId(runeId)), price = item.priceSilver, owned = ownedCount(item.id);
    return layout().row({ id: item.id, art: layout().itemArt(item), name: item.name, price: ShopRules.formatPrice(price), className: itemRarityClass(item), previewed: item.id === previewId,
      meta: itemRarityTag(item.rarity) + " " + escapeHtml(item.kindName) + (owned ? " · ×" + owned + " owned" : ""),
      extra: '<span class="tower-rune-effects">Weapons: ' + escapeHtml(item.weaponText) + "<br>Armor: " + escapeHtml(item.armorText) + "</span>",
      actions: '<button type="button" data-tower-buy-rune="' + runeId + '"' + (wallet() < price ? " disabled" : "") + ">" + (wallet() < price ? "Cannot afford" : "Buy") + "</button>" });
  }
  function buyCaster(id) {
    if (blocked() || !towerHere()) return;
    const entry = stock().tower.find(candidate => candidate.id === id), item = getItem(id);
    if (!entry || entry.quantity <= 0 || !item || !Shop.purchasableItem(item, place)) return;
    if (!pay(item.priceSilver)) { setStatus("You do not have enough coin for that purchase.", "error"); return; }
    entry.quantity -= 1;
    inventory.push(item.id);
    setStatus(item.name + " purchased for " + ShopRules.formatPrice(item.priceSilver) + ".", "success");
    afterChange();
  }
  function learnSpell(id) {
    if (blocked() || !towerHere()) return;
    const spell = getSpell(id);
    if (!spell || !stock().spells.includes(id) || !Shop.purchasableSpell(spell) || knownSpells.includes(id)) return;
    const price = Shop.spellPrice(spell);
    if (!pay(price)) { setStatus("You do not have enough coin to learn that spell.", "error"); return; }
    knownSpells.push(spell.id);
    renderKnownSpells(); renderAvailableSpells(); renderClasses();
    setStatus(spell.name + " learned for " + ShopRules.formatPrice(price) + ".", "success");
    afterChange();
  }
  function buyRune(runeId) {
    if (blocked() || !towerHere() || !F.RUNES[runeId]) return;
    const price = F.runePrice(runeId);
    if (!pay(price)) { setStatus(`${F.RUNES[runeId].name} costs ${ShopRules.formatPrice(price)}.`, "error"); return; }
    inventory.push(F.runeItemId(runeId));
    setStatus(`${F.RUNES[runeId].name} bought for ${ShopRules.formatPrice(price)}. It waits in your pack until it is fitted.`, "success");
    afterChange();
  }

  /* ---------- Selling runes ---------- */
  const isRune = item => item?.category === "rune";
  function sellRunes(entries) {
    if (blocked() || !towerHere()) return;
    const totals = layout().sellFromPack(entries, isRune);
    if (!totals.quantity) { setStatus("Those runes cannot be sold.", "error"); return; }
    setStatus("Sold " + totals.quantity + (totals.quantity === 1 ? " rune" : " runes") + " for " + ShopRules.formatPrice(totals.price) + ".", "success");
    afterChange();
  }
  function sellAllCopies(id) {
    const entry = layout().sellableEntries(isRune).find(candidate => candidate.id === id), item = getItem(id);
    if (!entry || !item) return;
    const totals = layout().saleTotals([entry]);
    if (!window.confirm((isItemFavorite(id) ? "This rune is favorited. " : "") + "Sell all " + entry.quantity + " copies of " + item.name + " for " + ShopRules.formatPrice(totals.price) + "?")) return;
    sellRunes([entry]);
  }
  function sellAllNonFavorites() {
    const entries = layout().sellableEntries(isRune).filter(entry => !isItemFavorite(entry.id)), totals = layout().saleTotals(entries);
    if (!totals.quantity) return;
    if (!window.confirm("Sell all " + totals.quantity + " non-favorited runes for " + ShopRules.formatPrice(totals.price) + "? Favorites are kept.")) return;
    sellRunes(entries);
  }

  /* ---------- Rune fitting ---------- */
  // Forged weapons and armor with rune slots, filled or not, one entry per distinct piece.
  function slottedItems() {
    const counts = new Map();
    inventory.forEach(id => counts.set(id, (counts.get(id) || 0) + 1));
    return [...counts.entries()].map(([id, quantity]) => ({ item: getItem(id), quantity })).filter(({ item }) => item?.forged?.slots > 0);
  }
  const EQUIPPED_SLOTS = ["primaryHand", "offHand", "armor"];
  const equippedIn = id => EQUIPPED_SLOTS.filter(slot => equipment[slot] === id);
  // Swaps one owned copy for the re-runed piece; equipped slots follow when they must.
  function replacePiece(oldId, newId) {
    const index = inventory.indexOf(oldId);
    if (index < 0) return false;
    const required = requiredEquipmentCounts(equipment)[oldId] || 0;
    inventory[index] = newId;
    if (inventory.filter(id => id === oldId).length < required) for (const slot of EQUIPPED_SLOTS) if (equipment[slot] === oldId) equipment[slot] = newId;
    if (favoriteItems.includes(oldId)) {
      if (!inventory.includes(oldId)) favoriteItems = favoriteItems.filter(id => id !== oldId);
      if (!favoriteItems.includes(newId)) favoriteItems.push(newId);
    }
    return true;
  }
  const centerItem = () => centerId && inventory.includes(centerId) ? getItem(centerId) : null;
  const placedRunes = () => pending.filter(Boolean);
  // Runes you own that fit the chosen slot alongside the fitted and already placed ones.
  function applicableRunes(item) {
    const spec = { ...item.forged, runes: [...item.forged.runes, ...placedRunes()] };
    return F.RUNE_IDS.filter(runeId => {
      const owned = ownedCount(F.runeItemId(runeId)) - placedRunes().filter(id => id === runeId).length;
      return owned > 0 && !F.runeConflict(spec, runeId);
    });
  }
  function fitPrice() { return placedRunes().reduce((sum, runeId) => sum + F.runeFitPrice(runeId), 0); }
  function removalPrice(item) { return item.forged.runes.reduce((sum, runeId) => sum + F.runeRemovalPrice(runeId), 0); }

  function chooseCenter(id) {
    const item = getItem(id);
    if (!item?.forged?.slots || !inventory.includes(id)) return;
    centerId = id; pending = Array(item.forged.slots - item.forged.runes.length).fill(null); choosing = null;
    hidePopup(); setStatus(""); render();
  }
  function chooseSlot(index) {
    const item = centerItem();
    if (!item) return;
    if (pending[index]) { pending[index] = null; choosing = null; render(); return; }
    choosing = { slot: index };
    render();
  }
  function placeRune(runeId) {
    const item = centerItem();
    if (!item || choosing?.slot == null || !applicableRunes(item).includes(runeId)) return;
    pending[choosing.slot] = runeId;
    choosing = null;
    setStatus(F.RUNES[runeId].name + " placed. Fit it to bind it to the " + item.forged.typeName.toLowerCase() + ".");
    render();
  }
  function fitRunes() {
    if (blocked() || !towerHere()) return;
    const item = centerItem(), runes = placedRunes();
    if (!item || !runes.length) return;
    const price = fitPrice();
    let newId = item.id;
    for (const runeId of runes) newId = newId && F.withRune(newId, runeId);
    if (!newId || runes.some(runeId => ownedCount(F.runeItemId(runeId)) < runes.filter(id => id === runeId).length)) { setStatus("Those runes no longer fit.", "error"); return; }
    if (!pay(price)) { setStatus("Fitting costs " + ShopRules.formatPrice(price) + ".", "error"); return; }
    for (const runeId of runes) inventory.splice(inventory.indexOf(F.runeItemId(runeId)), 1);
    replacePiece(item.id, newId);
    // A finished piece leaves the circle.
    centerId = null; pending = []; choosing = null;
    setStatus(runes.length + (runes.length === 1 ? " rune" : " runes") + " fitted for " + ShopRules.formatPrice(price) + ". It is now the " + getItem(newId).name + ".", "success");
    afterChange();
  }
  function removeAllRunes() {
    if (blocked() || !towerHere()) return;
    const item = centerItem();
    if (!item?.forged.runes.length) return;
    const runes = [...item.forged.runes], price = removalPrice(item);
    if (!window.confirm("Remove all " + runes.length + " runes from the " + item.name + " for " + ShopRules.formatPrice(price) + "? The runes return to your pack.")) return;
    let newId = item.id;
    for (const runeId of runes) newId = newId && F.withoutRune(newId, runeId);
    if (!newId) return;
    if (!pay(price)) { setStatus("Removing these runes costs " + ShopRules.formatPrice(price) + ".", "error"); return; }
    replacePiece(item.id, newId);
    for (const runeId of runes) inventory.push(F.runeItemId(runeId));
    // Unfitting keeps the piece in the circle.
    centerId = newId; pending = Array(getItem(newId).forged.slots).fill(null); choosing = null;
    setStatus(runes.length + (runes.length === 1 ? " rune was" : " runes were") + " removed for " + ShopRules.formatPrice(price) + " and returned to your pack.", "success");
    afterChange();
  }

  function slotPositions(count) {
    return Array.from({ length: count }, (_, index) => {
      const angle = -Math.PI / 2 + index * Math.PI * 2 / count;
      return { x: 50 + Math.cos(angle) * 37, y: 50 + Math.sin(angle) * 37 };
    });
  }
  function circleMarkup() {
    const item = centerItem();
    const ticks = Array.from({ length: 48 }, (_, index) => { const a = index / 48 * Math.PI * 2, r1 = index % 4 ? 182 : 174; return `M${(Math.cos(a) * r1).toFixed(1)} ${(Math.sin(a) * r1).toFixed(1)}L${(Math.cos(a) * 190).toFixed(1)} ${(Math.sin(a) * 190).toFixed(1)}`; }).join("");
    const star = [0, 1].map(offset => [0, 1, 2].map(index => { const a = -Math.PI / 2 + offset * Math.PI / 3 + index * Math.PI * 2 / 3; return `${(Math.cos(a) * 128).toFixed(1)} ${(Math.sin(a) * 128).toFixed(1)}`; }).join("L")).map(points => "M" + points + "Z").join("");
    const svg = '<svg class="rune-circle-art" viewBox="-200 -200 400 400" aria-hidden="true"><g class="rune-circle-spin"><circle r="190"/><circle r="170"/><path d="' + ticks + '"/></g><g class="rune-circle-inner"><circle r="128"/><path d="' + star + '"/><circle r="84"/></g></svg>';
    let slots = "";
    if (item) {
      const fitted = item.forged.runes, positions = slotPositions(item.forged.slots);
      positions.forEach((position, index) => {
        const style = `left:${position.x}%;top:${position.y}%`;
        if (index < fitted.length) {
          const runeId = fitted[index];
          slots += '<div class="rune-slot is-fitted" style="' + style + '" title="' + escapeHtml(F.RUNES[runeId].name + " · " + runeTextFor(item, runeId)) + '">' + ForgeArt.rune(runeId, "small") + "</div>";
          return;
        }
        const pendingIndex = index - fitted.length, placed = pending[pendingIndex];
        const label = placed ? F.RUNES[placed].name + " (placed, not fitted). Click to take it back." : "Empty rune slot. Click to choose a rune.";
        slots += '<button type="button" class="rune-slot' + (placed ? " is-placed" : " is-empty") + (choosing?.slot === pendingIndex ? " is-choosing" : "") + '" style="' + style + '" data-tower-slot-index="' + pendingIndex + '" aria-label="' + escapeHtml(label) + '" title="' + escapeHtml(label) + '">' + (placed ? ForgeArt.rune(placed, "small") : '<span aria-hidden="true">+</span>') + "</button>";
      });
    }
    const centre = '<button type="button" class="rune-center' + (item ? " has-item " + itemRarityClass(item) : "") + (choosing === "items" ? " is-choosing" : "") + '" data-tower-center aria-label="' + (item ? "Change the piece in the circle: " + escapeHtml(item.name) : "Choose a weapon or armor piece") + '">' + (item ? ForgeArt.weapon(item, "small") : '<span>Choose a piece</span>') + "</button>";
    const remove = item?.forged.runes.length ? '<button type="button" class="rune-circle-action is-remove" data-tower-remove-all' + (wallet() < removalPrice(item) ? ' disabled title="Cannot afford"' : "") + ">Remove all runes · " + escapeHtml(ShopRules.formatPrice(removalPrice(item))) + "</button>" : "";
    const fit = placedRunes().length ? '<button type="button" class="rune-circle-action is-fit" data-tower-fit' + (wallet() < fitPrice() ? ' disabled title="Cannot afford"' : "") + ">Fit " + (placedRunes().length === 1 ? "rune" : placedRunes().length + " runes") + " · " + escapeHtml(ShopRules.formatPrice(fitPrice())) + "</button>" : "";
    const caption = item ? '<p class="rune-circle-caption ' + itemRarityClass(item) + '"><strong>' + escapeHtml(item.name) + "</strong><span>" + escapeHtml(item.forged.typeName) + " · Lv " + item.forged.level + " · " + item.forged.runes.length + " of " + item.forged.slots + " rune slots fitted</span></p>" : '<p class="rune-circle-caption"><span>Click the field in the middle of the circle to choose a weapon or armor piece.</span></p>';
    return '<div class="rune-circle-top">' + remove + "</div>" + caption + '<div class="rune-circle">' + svg + centre + slots + "</div>" + '<div class="rune-circle-bottom">' + fit + "</div>";
  }
  function fittingList() {
    const item = centerItem();
    if (choosing === "items") {
      const pieces = slottedItems();
      $("magic-tower-list-title").textContent = "Weapons and armor with rune slots";
      if (!pieces.length) return '<div class="shop-empty">None of your forged weapons or armor has a rune slot. Pieces forged at level 40 and above carry them; rarely, lower ones do too.</div>';
      return pieces.map(({ item: piece, quantity }) => {
        const glyphs = runeIcon().repeat(piece.forged.runes.length) + runeIcon(false).repeat(piece.forged.slots - piece.forged.runes.length);
        return layout().row({ id: piece.id, art: layout().itemArt(piece), name: piece.name, className: itemRarityClass(piece) + " tower-piece-row" + (piece.id === centerId ? " is-previewed" : ""),
          meta: itemRarityTag(piece.rarity) + " " + escapeHtml(piece.forged.typeName) + " · Lv " + piece.forged.level + (quantity > 1 ? " · ×" + quantity : "") + (equippedIn(piece.id).length ? ' · <b class="magic-tower-equipped">Equipped</b>' : ""),
          extra: '<span class="magic-tower-glyphs" aria-label="' + piece.forged.runes.length + " of " + piece.forged.slots + ' rune slots filled">' + glyphs + "</span>",
          actions: '<button type="button" data-tower-choose="' + escapeHtml(piece.id) + '">Choose</button>' });
      }).join("");
    }
    if (choosing?.slot != null && item) {
      const runes = applicableRunes(item);
      $("magic-tower-list-title").textContent = "Runes for this slot";
      if (!runes.length) return '<div class="shop-empty">You own no rune that fits this slot. Buy runes in the Runes menu.</div>';
      return runes.map(runeId => {
        const rune = getItem(F.runeItemId(runeId)), owned = ownedCount(rune.id) - placedRunes().filter(id => id === runeId).length;
        return layout().row({ id: rune.id, art: layout().itemArt(rune), name: rune.name, price: "Fit " + ShopRules.formatPrice(F.runeFitPrice(runeId)), className: itemRarityClass(rune),
          meta: itemRarityTag(rune.rarity) + " " + escapeHtml(rune.kindName) + " · ×" + owned + " owned", extra: '<span class="tower-rune-effects">' + escapeHtml(runeTextFor(item, runeId)) + "</span>",
          actions: '<button type="button" data-tower-place="' + runeId + '">Place</button>' });
      }).join("");
    }
    $("magic-tower-list-title").textContent = "Rune fitting";
    return '<div class="shop-empty">' + (item ? "Click an empty slot on the ring to choose a rune, or the middle of the circle to change the piece." : "Click the field in the middle of the circle to choose a weapon or armor piece.") + "</div>";
  }

  /* ---------- Hover pop-up for the pieces in the fitting list ---------- */
  function showPopup(id, anchor) {
    const item = getItem(id);
    if (!item?.forged || !anchor) { hidePopup(); return; }
    const stats = forgedItemStats(item).slice(0, 6);
    const runes = item.forged.runes.map(runeId => "<li>" + runeIcon() + " " + escapeHtml(F.RUNES[runeId].name) + " · " + escapeHtml(runeTextFor(item, runeId)) + "</li>").join("") + Array(item.forged.slots - item.forged.runes.length).fill("<li>" + runeIcon(false) + " Empty rune slot</li>").join("");
    popup.className = "tower-popup " + itemRarityClass(item);
    popup.innerHTML = '<p class="eyebrow">' + escapeHtml(ForgeRules.MAKERS[item.forged.maker].name) + " · Level " + item.forged.level + "</p><strong>" + escapeHtml(item.name) + "</strong>" +
      '<div class="tower-popup-badges">' + itemRarityTag(item.rarity) + "<span>" + escapeHtml(item.forged.typeName) + "</span>" + (equippedIn(item.id).length ? '<b class="magic-tower-equipped">Equipped</b>' : "") + "</div>" +
      '<dl class="tower-popup-stats">' + stats.map(stat => "<div><dt>" + escapeHtml(stat.label) + "</dt><dd>" + escapeHtml(stat.value) + "</dd></div>").join("") + "</dl>" +
      '<ul class="tower-popup-runes">' + runes + "</ul>";
    popup.hidden = false;
    const dialog = modal.querySelector(".magic-tower-dialog").getBoundingClientRect(), row = anchor.getBoundingClientRect();
    const left = Math.min(dialog.width - popup.offsetWidth - 12, row.right - dialog.left + 12), top = Math.max(12, Math.min(dialog.height - popup.offsetHeight - 12, row.top - dialog.top));
    popup.style.left = Math.max(12, left) + "px"; popup.style.top = top + "px";
  }
  function hidePopup() { popup.hidden = true; }

  /* ---------- Rendering ---------- */
  function detailFor(id) {
    if (menu === "spells") { const spell = getSpell(id); return spell ? '<p class="shop-detail-note">' + (knownSpells.includes(id) ? "Already known" : "Price " + escapeHtml(ShopRules.formatPrice(Shop.spellPrice(spell)))) + "</p>" + spellDetailMarkup(spell) : ""; }
    const item = getItem(id);
    if (!item) return "";
    if (menu === "sell") { const entry = layout().sellableEntries(isRune).find(candidate => candidate.id === id); return layout().itemDetail(item, "Sells for " + escapeHtml(ShopRules.formatPrice(layout().salePrice(item))) + " each · ×" + (entry?.quantity || 0) + " in your pack"); }
    if (menu === "runes") return layout().itemDetail(item, "Price " + escapeHtml(ShopRules.formatPrice(item.priceSilver)) + (ownedCount(item.id) ? " · ×" + ownedCount(item.id) + " in your pack" : ""));
    return layout().itemDetail(item, "Price " + escapeHtml(ShopRules.formatPrice(item.priceSilver)));
  }
  function renderDetail() {
    detail.innerHTML = previewId ? detailFor(previewId) || '<div class="shop-empty">Hover over an entry to see it in full.</div>' : '<div class="shop-empty">Nothing here yet.</div>';
    detail.scrollTop = 0;
  }
  function entriesFor() {
    if (menu === "casters") return stock().tower.map(entry => ({ id: entry.id, markup: casterRow(entry) }));
    if (menu === "spells") return stock().spells.map(id => ({ id, markup: spellRow(id) }));
    if (menu === "runes") return F.RUNE_IDS.map(runeId => ({ id: F.runeItemId(runeId), markup: runeRow(runeId) }));
    return layout().sellableEntries(isRune).map(entry => ({ id: entry.id, markup: layout().sellRowFor(entry, entry.id === previewId) }));
  }

  function render() {
    if (modal.hidden) return;
    const here = towerHere();
    if (!here || here.id !== place?.id) { close(); return; }
    $("magic-tower-eyebrow").textContent = place.name.toUpperCase() + " · MAGIC TOWER";
    window.SceneBanners?.show($("magic-tower-scene"), "tower", place, worldState.regionId);
    syncPurse();
    modal.querySelectorAll("[data-tower-menu]").forEach(button => { const active = button.dataset.towerMenu === menu; button.classList.toggle("is-active", active); button.setAttribute("aria-pressed", String(active)); });
    $("magic-tower-description").textContent = MENUS[menu].description;
    const nextRefresh = (shopState.refreshWindow + 1) * 360;
    $("magic-tower-refresh").textContent = ["casters", "spells"].includes(menu) ? "Stock refreshes at Day " + WorldRules.day(nextRefresh) + " · " + WorldRules.time(nextRefresh) : "";
    const workspace = $("magic-tower-workspace"), fitting = menu === "fitting";
    workspace.dataset.mode = fitting ? "fitting" : menu === "sell" ? "sell" : "buy";
    $("magic-tower-detail-panel").hidden = fitting;
    circle.hidden = !fitting;
    const bulk = $("magic-tower-sell-unfavorited");
    bulk.hidden = menu !== "sell";
    if (fitting) {
      if (centerId && !inventory.includes(centerId)) { centerId = null; pending = []; choosing = null; }
      list.innerHTML = fittingList();
      circle.innerHTML = circleMarkup();
      return;
    }
    hidePopup();
    const entries = entriesFor().filter(entry => entry.markup);
    if (!entries.some(entry => entry.id === previewId)) previewId = entries[0]?.id || null;
    $("magic-tower-list-title").textContent = MENUS[menu].title;
    if (menu === "sell") {
      const bulkEntries = layout().sellableEntries(isRune).filter(entry => !isItemFavorite(entry.id)), totals = layout().saleTotals(bulkEntries);
      bulk.disabled = !bulkEntries.length;
      bulk.textContent = bulkEntries.length ? "Sell all non-favorites · " + ShopRules.formatPrice(totals.price) : "Sell all non-favorites";
    }
    list.innerHTML = entries.length ? entries.map(entry => entry.markup).join("") : '<div class="shop-empty">' + (menu === "sell" ? "You carry no runes." : "Nothing is on offer right now.") + "</div>";
    renderDetail();
  }

  function preview(id) {
    if (menu === "fitting") {
      if (choosing === "items") showPopup(id, [...list.querySelectorAll("[data-shop-entry]")].find(entry => entry.dataset.shopEntry === id));
      return;
    }
    if (id === previewId) return;
    previewId = id;
    layout().markPreview(list, id);
    renderDetail();
  }

  function open() {
    if (blocked()) return;
    place = towerHere();
    if (!place) return;
    modal.hidden = false; menu = "casters"; previewId = null; centerId = null; pending = []; choosing = null;
    setStatus(""); render(); updateModalState();
    modal.querySelector(".modal-close").focus();
  }
  function close() {
    modal.hidden = true; hidePopup(); updateModalState();
    $("map-magic-tower")?.focus();
  }

  if (window.ShopLayout) ShopLayout.bindPreview(list, preview);
  list.addEventListener("mouseleave", hidePopup);
  modal.addEventListener("click", event => {
    const target = event.target;
    const menuButton = target.closest("[data-tower-menu]");
    if (menuButton) { menu = menuButton.dataset.towerMenu; previewId = null; choosing = null; hidePopup(); setStatus(""); render(); return; }
    const caster = target.closest("[data-tower-buy-caster]");
    if (caster) { buyCaster(caster.dataset.towerBuyCaster); return; }
    const learn = target.closest("[data-tower-learn]");
    if (learn) { learnSpell(learn.dataset.towerLearn); return; }
    const rune = target.closest("[data-tower-buy-rune]");
    if (rune) { buyRune(rune.dataset.towerBuyRune); return; }
    const sellOne = target.closest("[data-sell-item]");
    if (sellOne) { sellRunes([{ id: sellOne.dataset.sellItem, quantity: 1 }]); return; }
    const sellAll = target.closest("[data-sell-all-item]");
    if (sellAll) { sellAllCopies(sellAll.dataset.sellAllItem); return; }
    if (target.closest("[data-tower-center]")) { choosing = choosing === "items" ? null : "items"; hidePopup(); render(); return; }
    const slot = target.closest("[data-tower-slot-index]");
    if (slot) { chooseSlot(Number(slot.dataset.towerSlotIndex)); return; }
    const choose = target.closest("[data-tower-choose]") || (choosing === "items" && target.closest(".tower-piece-row"));
    if (choose) { chooseCenter(choose.dataset.towerChoose || choose.dataset.shopEntry); return; }
    const placeButton = target.closest("[data-tower-place]") || (choosing?.slot != null && target.closest("[data-shop-entry]"));
    if (placeButton) { placeRune(placeButton.dataset.towerPlace || F.runeOfItem(placeButton.dataset.shopEntry)); return; }
    if (target.closest("[data-tower-fit]")) { fitRunes(); return; }
    if (target.closest("[data-tower-remove-all]")) { removeAllRunes(); return; }
  });
  $("magic-tower-sell-unfavorited").addEventListener("click", sellAllNonFavorites);
  modal.querySelectorAll("[data-tower-close]").forEach(button => button.addEventListener("click", close));
  modal.addEventListener("keydown", event => { if (event.key === "Escape") { event.preventDefault(); close(); } });
  $("map-magic-tower")?.addEventListener("click", open);

  window.MagicTower = { open, close, render, available: () => Boolean(towerHere()), menu: name => { if (MENUS[name]) { menu = name; previewId = null; choosing = null; render(); } } };
})();

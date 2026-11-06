/* The reforge anvil (port task 3, the Shop Rework, 2026-11-06), shared by the Blacksmith and the Tailor
   (shop.js), the Institute of Magic and Hearth Home (sites.js). Like the Magic Tower's rune circle: an anvil
   with a free slot; clicking it lists the pieces this anvil can take on the left (your pack, equipped
   pieces included, and the gear your companions wear: R4). A piece on the anvil shows its main part at the
   top and its two other parts below; hovering a part shows everything about it. Click one of the two to list
   every part that may take its place; "Reforge" on the right pays and swaps them. The rules and prices are
   ForgeRules' (reforgeOptions, reforgeCost, reforge); a place may give a discount (Hearth Home 15%).
   The anvil drawing is placeholder art (R1). */
(() => {
  "use strict";
  const F = () => window.ForgeRules;
  const EQUIPPED_SLOTS = ["primaryHand", "offHand", "armor"];
  const rarityName = rarity => rarity ? rarity[0].toUpperCase() + rarity.slice(1) : "";

  // Every piece this anvil can take: the player's pack (one entry per item) and each companion's worn gear.
  function pieces(anvil) {
    const result = [], counts = new Map();
    inventory.forEach(id => counts.set(id, (counts.get(id) || 0) + 1));
    for (const [id, quantity] of counts) {
      const spec = F().decode(id);
      if (spec && !F().reforgeReason(spec, anvil)) result.push({ owner: "player", id, quantity, label: equippedSlotsOf(equipment, id).length ? "Your pack · equipped" : "Your pack" });
    }
    for (const companion of companions) {
      const worn = [...new Set(EQUIPPED_SLOTS.map(slot => companion.character.equipment[slot]).filter(Boolean))];
      for (const id of worn) {
        const spec = F().decode(id);
        if (spec && !F().reforgeReason(spec, anvil)) result.push({ owner: companion.id, id, quantity: 1, label: companion.name + " · worn" });
      }
    }
    return result;
  }
  const equippedSlotsOf = (gear, id) => EQUIPPED_SLOTS.filter(slot => gear[slot] === id);
  // Swaps one copy of the piece for its reforged version: in the pack (equipped slots follow when every
  // copy is needed there, as the Tower's rune fitting does) or on a companion.
  function replacePiece(owner, oldId, newId) {
    if (owner === "player") {
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
    const companion = companions.find(entry => entry.id === owner);
    if (!companion) return false;
    const gear = companion.character.equipment, slots = equippedSlotsOf(gear, oldId);
    if (!slots.length) return false;
    for (const slot of slots) gear[slot] = newId;
    const index = companion.character.inventory.indexOf(oldId);
    if (index >= 0) companion.character.inventory[index] = newId;
    return true;
  }
  const discounted = (cost, discount) => Math.round(cost * (1 - Math.max(0, discount) / 100));

  function create({ anvil, discount = 0, list, stage, setStatus, onChange, available = () => true }) {
    let piece = null, parts = [null, null, null], choosing = null, hovered = null;
    const spec = () => piece ? F().decode(piece.id) : null;
    const current = () => { const base = spec(); return base ? base.parts.map((part, index) => index === 0 ? part : parts[index] || part) : []; };
    const result = () => piece ? F().reforge(piece.id, current(), anvil) : null;
    const cost = () => { const outcome = result(); return outcome?.id ? discounted(outcome.cost, discount) : 0; };
    function reset() { piece = null; parts = [null, null, null]; choosing = null; hovered = null; }
    // A part's full description, as the hover card shows it.
    function partInfo(base, index, part) {
      const sample = { ...base, parts: base.parts.map((entry, at) => at === index ? part : entry) };
      const info = F().describePart(sample, index);
      if (!info) return "";
      return `<strong>${escapeHtml(info.material)}</strong><span>${escapeHtml(info.shape)} by ${escapeHtml(info.maker)}${info.rarity ? " · " + escapeHtml(rarityName(info.rarity)) : ""}${info.element ? " · " + escapeHtml(spellElementLabel?.(info.element) || info.element) : ""}</span>${info.effects ? `<em>${escapeHtml(info.effects)}</em>` : ""}${index === 0 ? "<small>The main part: it never changes.</small>" : ""}`;
    }
    function stageMarkup() {
      const art = `<svg class="reforge-anvil-art" viewBox="0 0 220 120" aria-hidden="true"><path d="M30 30h150l-12 20h-28v26l22 26H58l22-26V50H52Z" fill="#3a3632" stroke="#0d0907" stroke-width="3"/><path d="M30 30h150" stroke="#8a8e94" stroke-width="4"/><ellipse cx="110" cy="112" rx="70" ry="6" fill="#0d0907" opacity=".45"/></svg>`;
      if (!piece) return `<div class="reforge-stage is-empty">${art}<button type="button" class="reforge-slot" data-reforge-slot aria-pressed="${choosing === "item"}">${choosing === "item" ? "Choose a piece from the list on the left" : "Place a piece on the anvil"}</button><p class="reforge-note">Reforging keeps the main part, level, rune slots and runes, and replaces the two other parts. Each changed part costs three times its rarity's part price${discount ? `, ${discount}% off here` : ""}.</p></div>`;
      const base = spec(), item = getItem(piece.id), outcome = result(), preview = outcome?.id ? getItem(outcome.id) : null;
      const partButton = index => {
        const part = current()[index];
        if (!part) return "";
        const changed = index > 0 && parts[index] && !(parts[index].maker === base.parts[index].maker && parts[index].material === base.parts[index].material);
        const info = F().describePart({ ...base, parts: current() }, index);
        return `<button type="button" class="reforge-part${index === 0 ? " is-main" : ""}${changed ? " is-changed" : ""}${choosing === index ? " is-choosing" : ""}" data-reforge-part="${index}" ${index === 0 ? 'aria-disabled="true"' : `aria-pressed="${choosing === index}"`}><small>${index === 0 ? "Main part" : "Part " + (index + 1)}${info.rarity ? " · " + escapeHtml(rarityName(info.rarity)) : ""}</small><strong>${escapeHtml(info.material)}</strong><span>${escapeHtml(info.maker)}</span><span class="reforge-part-card" role="tooltip">${partInfo({ ...base, parts: current() }, index, part)}</span></button>`;
      };
      const price = cost(), canPay = ShopRules.walletValue(currencies) >= price;
      const reason = !outcome?.id ? outcome?.reason || "" : !outcome.changed ? "Choose a new part first." : !canPay ? "You can't afford it." : "";
      return `<div class="reforge-stage">${art}<div class="reforge-piece"><p class="reforge-piece-name">${itemRarityTag(item.rarity)} ${escapeHtml(item.name)} <small>${escapeHtml(piece.label)}</small></p>${partButton(0)}<div class="reforge-others">${partButton(1)}${partButton(2)}</div>${preview && outcome.changed ? `<p class="reforge-result">Becomes ${itemRarityTag(preview.rarity)} <strong>${escapeHtml(preview.name)}</strong></p>` : ""}</div><aside class="reforge-actions"><p class="reforge-cost"><span>Cost</span><strong>${escapeHtml(ShopRules.formatPrice(price))}</strong>${discount ? `<small>${discount}% off here</small>` : ""}</p><button type="button" class="reforge-button" id="reforge-button" ${reason ? `disabled title="${escapeHtml(reason)}"` : ""}>Reforge</button><button type="button" class="reforge-clear" data-reforge-clear>Take it off the anvil</button>${reason && outcome?.changed !== false ? `<small class="reforge-reason">${escapeHtml(reason)}</small>` : ""}</aside></div>`;
    }
    function listMarkup() {
      if (choosing === "item") {
        const entries = pieces(anvil);
        return entries.length ? entries.map(entry => { const item = getItem(entry.id); return ShopLayout.row({ id: entry.owner + "|" + entry.id, art: ShopLayout.itemArt(item), name: item.name, meta: itemRarityTag(item.rarity) + " " + escapeHtml(entry.label) + (entry.quantity > 1 ? " · ×" + entry.quantity : ""), className: itemRarityClass(item), actions: `<button type="button" data-reforge-piece="${escapeHtml(entry.owner + "|" + entry.id)}">Place</button>` }); }).join("")
          : '<div class="shop-empty">Nothing you or your companions carry can be reforged here.</div>';
      }
      if (piece && (choosing === 1 || choosing === 2)) {
        const base = spec(), currentParts = current();
        return F().reforgeOptions(base, choosing).map(option => {
          const sample = { ...base, parts: currentParts.map((part, at) => at === choosing ? option : part) };
          const info = F().describePart(sample, choosing), selected = option.maker === currentParts[choosing].maker && option.material === currentParts[choosing].material;
          const changeCost = discounted(F().reforgeCost(base, sample.parts) - F().reforgeCost(base, currentParts.map((part, at) => at === choosing ? base.parts[at] : part)), discount);
          return `<article class="shop-row reforge-option${selected ? " is-previewed" : ""}" data-reforge-option="${choosing}|${escapeHtml(option.maker)}|${escapeHtml(option.material)}" tabindex="0" title="${escapeHtml(info.effects || "")}"><span class="shop-row-main"><strong>${escapeHtml(info.material)}</strong><small>${info.rarity ? itemRarityTag(info.rarity) + " " : ""}${escapeHtml(info.maker)}${info.effects ? " · " + escapeHtml(info.effects) : ""}</small></span><b class="shop-row-price">${selected ? "Chosen" : escapeHtml(ShopRules.formatPrice(changeCost))}</b><span class="shop-row-actions"><button type="button" data-reforge-choose="${choosing}|${escapeHtml(option.maker)}|${escapeHtml(option.material)}">${selected ? "Keep" : "Choose"}</button></span></article>`;
        }).join("");
      }
      return `<div class="shop-empty">${piece ? "Click part 2 or part 3 on the anvil to see every part that can take its place." : "Click the anvil's free slot to choose a piece."}</div>`;
    }
    function render() {
      stage.innerHTML = stageMarkup();
      list.innerHTML = listMarkup();
    }
    function place(key) {
      const [owner, id] = key.split("|");
      if (!pieces(anvil).some(entry => entry.owner === owner && entry.id === id)) return;
      piece = pieces(anvil).find(entry => entry.owner === owner && entry.id === id);
      parts = [null, null, null]; choosing = null;
      render();
    }
    function choose(key) {
      const [indexText, maker, material] = key.split("|"), index = Number(indexText), base = spec();
      if (!base || ![1, 2].includes(index)) return;
      const option = F().reforgeOptions(base, index).find(entry => entry.maker === maker && entry.material === material);
      if (!option) return;
      parts[index] = option; choosing = null;
      render();
    }
    function commit() {
      if (!piece || !available()) return false;
      const outcome = result();
      if (!outcome?.id || !outcome.changed) { setStatus(outcome?.reason || "Choose a new part first.", "error"); return false; }
      const price = discounted(outcome.cost, discount), remaining = ShopRules.spend(currencies, price);
      if (!remaining) { setStatus("You can't afford the reforge.", "error"); return false; }
      if (!replacePiece(piece.owner, piece.id, outcome.id)) { setStatus("That piece is no longer there.", "error"); reset(); render(); return false; }
      currencies = remaining;
      const before = getItem(piece.id)?.name, after = getItem(outcome.id)?.name;
      piece = { ...piece, id: outcome.id }; parts = [null, null, null]; choosing = null;
      setStatus(`${before} was reforged into ${after} for ${ShopRules.formatPrice(price)}.`, "success");
      onChange?.(); render();
      return true;
    }
    function click(event) {
      const slot = event.target.closest("[data-reforge-slot]"), part = event.target.closest("[data-reforge-part]"), pick = event.target.closest("[data-reforge-piece]");
      const option = event.target.closest("[data-reforge-choose]"), clear = event.target.closest("[data-reforge-clear]");
      if (slot) { choosing = "item"; render(); }
      if (part && part.dataset.reforgePart !== "0") { const index = Number(part.dataset.reforgePart); choosing = choosing === index ? null : index; render(); }
      if (pick) place(pick.dataset.reforgePiece);
      if (option) choose(option.dataset.reforgeChoose);
      if (clear) { reset(); render(); }
      if (event.target.closest("#reforge-button")) commit();
    }
    stage.addEventListener("click", click);
    list.addEventListener("click", click);
    return { render, reset, place, choose, commit, state: () => ({ piece, parts: [...parts], choosing, cost: cost() }), anvil, discount };
  }
  window.Reforge = { create, pieces, replacePiece };
})();

/* Detailed member browsers reuse the main sheet's calculations and requirements.
   They only rebuild on explicit edits, never on the world's background clock. */
(() => {
  "use strict";
  const $ = id => document.getElementById(id);
  const esc = value => escapeHtml(String(value ?? ""));
  const selectedClasses = new Map();
  let sheetEntry = null, picker = null, returnFocus = null;
  document.body.insertAdjacentHTML("beforeend", `
    <div class="modal-layer companion-picker-layer" id="companion-picker-modal" hidden>
      <button class="modal-backdrop" type="button" data-comp-picker-close aria-label="Close selection"></button>
      <section class="spellbook-dialog companion-picker-dialog" role="dialog" aria-modal="true" aria-labelledby="companion-picker-title">
        <header class="dialog-header compact-dialog-header"><div><p class="eyebrow" id="companion-picker-owner"></p><h2 id="companion-picker-title">Equipment</h2></div><button class="modal-close" type="button" data-comp-picker-close aria-label="Close selection">×</button></header>
        <div class="companion-picker-search"><label for="companion-picker-search">Search by name</label><input id="companion-picker-search" type="search" placeholder="Type a name…" autocomplete="off"></div>
        <div class="companion-picker-grid"><div id="companion-picker-list" class="companion-picker-list"></div><aside id="companion-picker-detail" class="spell-detail-panel companion-item-detail"></aside></div>
      </section>
    </div>`);
  const modal = $("companion-picker-modal");
  const counts = ids => ids.reduce((result, id) => { result[id] = (result[id] || 0) + 1; return result; }, {});
  const locked = () => Boolean(window.Combat?.active());
  function candidateEquipment(c, slot, item) {
    const result = { ...c.equipment, [slot]: item?.id || null };
    if (slot === "primaryHand") result.offHand = item?.handed === 2 ? item.id : getItem(c.equipment.primaryHand)?.handed === 2 ? null : result.offHand;
    return result;
  }
  function equipmentReason(c, slot, item) {
    if (locked()) return "Equipment is read-only during combat";
    if (slot === "offHand" && getItem(c.equipment.primaryHand)?.handed === 2) return "Both hands are occupied by your two-handed weapon";
    if (item && !window.Companions.allowedSlots(item, c).includes(slot)) return "This item cannot be equipped in this slot";
    // Above Level 100 only with Limitless on (task 7, L10).
    if (item && !LimitlessRules.wearable(item, limitless.active)) return "Level " + LimitlessRules.itemLevel(item) + " required: it can only be worn while Limitless is on";
    const owned = counts(c.inventory);
    if (Object.entries(requiredEquipmentCounts(candidateEquipment(c, slot, item))).some(([id, quantity]) => (owned[id] || 0) < quantity)) return "All owned copies are already equipped in other slots";
    return "";
  }
  function itemStats(c, item) {
    const computed = Combat.previewSheetDetails(c, item).itemStats;
    if (item.category === "weapon") return [
      { label: "Base power", value: formatValue(item.basePower || 0) },
      { label: "Primary scaling", value: itemStatLabel(item.primary) },
      { label: "Secondary scaling", value: itemStatLabel(item.secondary) },
      ...computed
    ];
    return item.category === "artifact" ? (item.effects || []).map(effect => ({ label: "Effect", value: artifactEffectLabel(effect) })) : computed;
  }
  const statRows = rows => `<dl class="spell-stat-details item-stat-details">${rows.map(row => `<div><dt title="${esc(row.title || row.label)}">${esc(row.label)}</dt><dd>${esc(row.value)}</dd></div>`).join("")}</dl>`;
  function itemDetail(c, item, quantity = 1) {
    return `<div class="spell-detail item-detail ${itemRarityClass(item)}"><div class="spell-detail-heading"><div><p class="eyebrow">ITEM DETAIL · ${quantity} OWNED</p><h3>${esc(item.name)}</h3></div><span class="spell-detail-symbol" aria-hidden="true">◫</span></div>
      <div class="spell-badges"><span>${esc(itemCategoryLabel(item.category))}</span><span>${esc(itemTypeLabel(item.type))}</span><span>${esc(itemRarityLabel(item.rarity))}</span></div>${limitlessNote(item)}
      <div class="spell-detail-cost"><span>Value</span><strong>${esc(ShopRules.formatPrice(item.priceSilver || 0))}</strong></div>
      ${item.shopAvailable === false ? '<p class="companion-detail-note">' + escapeHtml(item.soldAt ? "Sold at " + item.soldAt : "Not sold by shops") + '</p>' : ""}
      ${statRows(itemStats(c, item))}<div class="spell-description"><p>${esc(item.description || "No description provided.")}</p></div></div>`;
  }
  function spellDetail(c, spell) {
    const calculated = Combat.previewSheetDetails(c, null, spell).spell;
    return `<div class="spell-detail spell-element-${esc(spell.element)}"><div class="spell-detail-heading"><div><p class="eyebrow">SPELL DETAIL</p><h3>${esc(spell.name)}</h3></div><span class="spell-tier-symbol">${esc(spellTierLabel(spell.tier))}</span></div>
      <div class="spell-badges"><span>${esc(spellElementLabel(spell.element))}</span><span>${esc(spell.type)}</span></div>
      ${statRows([{ label: "Power", value: calculated.power }, { label: "Mana cost", value: calculated.mana }, { label: "Action points", value: calculated.ap }, { label: "Primary scaling", value: spellStatLabel(spell.primary) }, { label: "Secondary scaling", value: spellStatLabel(spell.secondary) }])}
      <div class="spell-description"><p>${esc(calculated.description)}</p></div></div>`;
  }
  function close() {
    if (modal.hidden) return;
    modal.hidden = true; picker = null;
    updateModalState();
    if (returnFocus?.isConnected) returnFocus.focus({ preventScroll: true });
    else $("companions-modal").querySelector(".modal-close")?.focus({ preventScroll: true });
  }
  function open(mode, slot = null, selected = null, trigger = null) {
    if (!sheetEntry) return;
    picker = { mode, slot, selected, entryId: sheetEntry.id };
    returnFocus = trigger || document.activeElement;
    $("companion-picker-search").value = "";
    $("companion-picker-owner").textContent = sheetEntry.name;
    $("companion-picker-title").textContent = mode === "equipment" ? equipmentSlotDefinitions.find(entry => entry.key === slot)?.label || "Equipment" : mode === "give" ? "Give an item" : mode === "learn" ? "Learn a spell" : mode === "spells" ? "Spell book" : "Inventory";
    modal.hidden = false; renderPicker(); updateModalState(); $("companion-picker-search").focus();
  }
  function pickerEntries() {
    const c = sheetEntry.character;
    if (["learn", "spells"].includes(picker.mode)) {
      const ids = picker.mode === "spells" ? c.knownSpells : [...$("companion-spell-choice").options].map(option => option.value).filter(Boolean);
      return ids.map(getSpell).filter(Boolean).map(item => ({ item, quantity: 1 }));
    }
    const owned = counts(picker.mode === "give" ? inventory : c.inventory);
    return Object.entries(owned).map(([id, quantity]) => ({ item: getItem(id), quantity })).filter(({ item, quantity }) => {
      if (!item) return false;
      if (picker.mode === "equipment") return Companions.allowedSlots(item, c).includes(picker.slot);
      if (picker.mode === "give") return quantity > (requiredEquipmentCounts(equipment)[item.id] || 0);
      return true;
    });
  }
  function renderPicker() {
    if (!picker || sheetEntry?.id !== picker.entryId) return close();
    const search = $("companion-picker-search").value.trim().toLowerCase();
    const entries = pickerEntries().filter(({ item }) => item.name.toLowerCase().includes(search)).sort((a, b) => a.item.name.localeCompare(b.item.name));
    if (!entries.some(({ item }) => item.id === picker.selected)) picker.selected = entries[0]?.item.id || null;
    const isSpell = ["learn", "spells"].includes(picker.mode);
    $("companion-picker-list").innerHTML = entries.map(({ item, quantity }) => `<button type="button" class="companion-catalog-card ${isSpell ? `spell-element-${esc(item.element)}` : itemRarityClass(item)} ${picker.selected === item.id ? "is-selected" : ""}" data-comp-picker-select="${esc(item.id)}" aria-pressed="${picker.selected === item.id}"><strong>${esc(item.name)}${isSpell ? "" : ` <span>×${quantity}</span>`}</strong><small>${isSpell ? `${esc(spellElementLabel(item.element))} · ${esc(spellTierLabel(item.tier))}` : `${esc(itemTypeLabel(item.type))} · ${esc(itemRarityLabel(item.rarity))}`}</small></button>`).join("") || '<p class="companion-list-empty">No eligible entries match your search.</p>';
    renderPickerDetail(entries.find(({ item }) => item.id === picker.selected), isSpell);
  }
  function renderPickerDetail(group, isSpell) {
    const detail = $("companion-picker-detail"), c = sheetEntry.character;
    if (!group) { detail.innerHTML = '<p class="companion-list-empty">Select an entry to inspect its full details.</p>'; return; }
    const { item, quantity } = group;
    let reason = locked() ? "Changes are unavailable during combat" : "", label = "", action = "";
    if (picker.mode === "equipment") { reason = equipmentReason(c, picker.slot, item); label = "Equip in " + equipmentSlotDefinitions.find(slot => slot.key === picker.slot).label; action = "equip"; }
    if (picker.mode === "give") { label = "Give one to companion"; action = "give"; }
    if (picker.mode === "learn") { label = editingEnabled ? "Teach spell · Dev Mode" : "Learn spell"; action = "learn"; }
    if (picker.mode === "inventory") {
      label = "Take one into your inventory"; action = "take";
      if (quantity <= (requiredEquipmentCounts(c.equipment)[item.id] || 0)) reason = "Unequip a copy before transferring it";
    }
    detail.innerHTML = (isSpell ? spellDetail(c, item) : itemDetail(c, item, quantity)) +
      (action ? `<div class="companion-picker-actions"><button type="button" data-comp-picker-action="${action}" ${reason ? "disabled" : ""}>${esc(label)}</button>${reason ? `<p>${esc(reason)}</p>` : ""}</div>` : "");
    if (picker.mode === "learn") detail.querySelector(".spell-detail").insertAdjacentHTML("beforeend", `<div class="spell-detail-cost"><span>Learning price</span><strong>${esc(ShopRules.formatPrice(Companions.spellPriceFor(c, item)))}</strong></div>`);
  }
  function renderClasses(entry, section, selected = null) {
    const c = entry.character, options = Combat.previewClassOptions(c);
    const visible = options.filter(option => option.rank || option.met);
    const filter = section.querySelector("[data-comp-class-search]")?.value || "";
    let id = selected || selectedClasses.get(entry.id);
    if (!visible.some(option => option.definition.id === id)) id = visible.find(option => option.rank)?.definition.id || visible[0]?.definition.id;
    if (id) selectedClasses.set(entry.id, id);
    const choice = options.find(option => option.definition.id === id);
    const reason = locked() ? "Training is unavailable during combat" : choice?.reason || (!choice ? "Select a class to train." : "Spend a perk point to unlock the next feature.");
    const card = option => `<button type="button" class="companion-catalog-card class-entry ${option.definition.id === id ? "is-selected" : ""}" data-class-color="${esc(option.definition.color || "")}" data-comp-class-select="${esc(option.definition.id)}" aria-pressed="${option.definition.id === id}"><strong>${esc(option.definition.name)}</strong><small>Level ${option.rank} / ${option.definition.maxLevel}</small></button>`;
    section.innerHTML = `<div class="companion-subheading"><div><p class="eyebrow">CLASSES</p><h4>Training</h4></div><span>${c.perkPoints} perk point${c.perkPoints === 1 ? "" : "s"}</span></div>
      <label class="companion-class-search"><span>Search classes</span><input type="search" data-comp-class-search value="${esc(filter)}" placeholder="Class name…"></label>
      <div class="companion-picker-actions companion-class-actions">
        <div><strong>${esc(choice?.definition.name || "Class training")}</strong><p id="companion-class-action-reason">${esc(reason)}</p></div>
        <button type="button" id="companion-class-up" aria-describedby="companion-class-action-reason" ${!choice || choice.reason || locked() ? "disabled" : ""}>${choice?.rank ? "Level up class" : "Acquire class"} · 1 perk point</button>
        <select id="companion-class-choice" hidden><option value="">Choose class</option>${options.filter(option => (option.rank || option.met) && option.rank < option.definition.maxLevel).map(option => `<option value="${esc(option.definition.id)}" ${option.definition.id === id ? "selected" : ""}>${esc(option.definition.name)}</option>`).join("")}</select>
      </div>
      <div class="companion-class-browser"><section><h5>Available classes</h5><div data-comp-class-column>${visible.filter(option => !option.rank).sort((a,b) => a.definition.name.localeCompare(b.definition.name)).map(card).join("") || '<p class="companion-list-empty">No new classes meet the requirements.</p>'}</div></section><section><h5>Learned classes</h5><div data-comp-class-column>${visible.filter(option => option.rank).sort((a,b) => a.definition.name.localeCompare(b.definition.name)).map(card).join("") || '<p class="companion-list-empty">No classes acquired.</p>'}</div></section>
      <section class="companion-class-detail spell-detail-panel">${choice ? `<div class="spell-detail class-detail" data-class-color="${esc(choice.definition.color || "")}"><div class="spell-detail-heading"><div><p class="eyebrow">CLASS DETAIL</p><h3>${esc(choice.definition.name)}</h3></div></div><div class="spell-badges"><span>Level ${choice.rank} / ${choice.definition.maxLevel}</span></div><p class="companion-detail-note">${esc(choice.requirements)}</p><div class="class-level-list">${choice.definition.levels.map(rank => `<div class="class-level-row ${rank.level <= choice.rank ? "is-unlocked" : "is-locked"}"><div><span class="class-level-number">Level ${rank.level}</span><span class="class-level-state">${rank.level <= choice.rank ? "Unlocked" : "Locked"}</span></div><p>${esc(classLevelDescription(rank))}</p>${classLevelAbilityDetails(rank)}</div>`).join("")}</div></div>` : '<p class="companion-list-empty">Select a class to see every feature.</p>'}</section></div>`;
    applyClassFilter(section, filter);
  }
  function applyClassFilter(section, query) {
    const search = query.trim().toLowerCase();
    section.querySelectorAll("[data-comp-class-select]").forEach(button => { button.hidden = !button.textContent.toLowerCase().includes(search); });
  }
  function refreshEquipment(entry, panel) {
    const c = entry.character, snapshot = Combat.previewSheetDetails(c);
    const equippedCounts = requiredEquipmentCounts(c.equipment), ownedCounts = counts(c.inventory);
    for (const button of panel.querySelectorAll("[data-comp-take]")) {
      const unavailable = (ownedCounts[button.dataset.compTake] || 0) <= (equippedCounts[button.dataset.compTake] || 0);
      button.disabled = locked() || unavailable;
      button.title = unavailable ? "Unequip a copy before transferring it" : "Transfer one unequipped copy";
    }
    for (const card of panel.querySelectorAll("[data-companion-equipment]")) {
      const slot = card.dataset.companionEquipment, item = getItem(c.equipment[slot]);
      const occupied = slot === "offHand" && getItem(c.equipment.primaryHand)?.handed === 2;
      const select = card.querySelector("select"); select.hidden = true;
      let actions = card.querySelector(".companion-gear-actions");
      if (!actions) { actions = document.createElement("div"); actions.className = "companion-gear-actions"; card.append(actions); }
      actions.innerHTML = `<button type="button" data-comp-browse-slot="${esc(slot)}" ${occupied ? "disabled" : ""}>${locked() ? "Inspect eligible equipment" : item ? "Change equipment" : "Equip item"}</button>${item ? `<button type="button" data-comp-inspect-item="${esc(item.id)}">Details</button>` : ""}${item && !occupied ? `<button type="button" data-comp-clear-slot="${esc(slot)}" ${locked() ? "disabled" : ""}>Unequip</button>` : ""}`;
      card.querySelector(".companion-gear-detail").innerHTML = item ? `<div class="companion-gear-item ${itemRarityClass(item)}"><strong>${esc(item.name)}</strong><small>${esc(itemTypeLabel(item.type))} · ${esc(itemRarityLabel(item.rarity))}${occupied ? " · Occupies both hands" : ""}</small>${statRows(itemStats(c, item))}<p class="companion-gear-description" title="${esc(item.description)}">${esc(item.description)}</p></div>` : '<span class="companion-gear-empty">Empty slot</span>';
    }
    for (const stat of statDefinitions) {
      const card = panel.querySelector(`[data-comp-current-stat="${stat.key}"]`)?.closest(".companion-stat");
      if (!card) continue;
      card.querySelector(".companion-stat-value > span:first-child > strong").textContent = snapshot.baseStats[stat.key];
      card.querySelector(`[data-comp-bonus="permanentBonuses"]`).title = `Effective permanent bonus: ${snapshot.permanent[stat.key]}% (includes classes, ancestry, equipment and active skills). Editable field is the manual bonus.`;
      let total = card.querySelector(".companion-effective-bonus");
      if (!total) { total = document.createElement("small"); total.className = "companion-effective-bonus"; card.append(total); }
      total.textContent = `Permanent total ${formatValue(snapshot.permanent[stat.key])}%`;
    }
    for (const key of defenseBonusDefinitions) {
      const card = panel.querySelector(`[data-comp-defense-value="${key}"]`)?.closest(".companion-defense-card");
      let total = card?.querySelector(".companion-effective-bonus");
      if (card && !total) { total = document.createElement("small"); total.className = "companion-effective-bonus"; card.append(total); }
      if (total) total.textContent = `Permanent total ${formatValue(snapshot.permanentDefense[key])}%`;
    }
    const classSection = panel.querySelector(".companion-class-section");
    if (classSection) renderClasses(entry, classSection);
  }
  function mount(entry, panel) {
    if (picker && picker.entryId !== entry.id) close();
    sheetEntry = entry;
    // Keep browsing possible in combat, but disable every editing control.
    panel.querySelector("fieldset").disabled = false;
    if (locked()) panel.querySelectorAll("button, input, select").forEach(control => { control.disabled = true; });
    refreshEquipment(entry, panel);
    const classSection = panel.querySelector("#companion-section-training > .companion-training-grid > section");
    classSection.classList.add("companion-class-section"); renderClasses(entry, classSection);
    for (const button of panel.querySelectorAll("[data-comp-take]")) {
      const item = getItem(button.dataset.compTake);
      const card = button.closest("article");
      card.querySelector("div").insertAdjacentHTML("beforeend", `<button type="button" data-comp-inspect-item="${esc(item.id)}">Full item details</button>`);
      if (entry.character.inventory.filter(id => id === item.id).length <= (requiredEquipmentCounts(entry.character.equipment)[item.id] || 0)) { button.disabled = true; button.title = "Unequip a copy before transferring it"; }
    }
    for (const control of [["companion-give-item", "give", "Browse your inventory"], ["companion-spell-choice", "learn", "Browse available spells"]]) {
      const select = $(control[0]); select.hidden = true;
      select.parentElement.insertAdjacentHTML("beforeend", `<button type="button" data-comp-browse="${control[1]}">${control[2]}</button>`);
      const action = $(control[1] === "give" ? "companion-give" : "companion-learn-spell"); action.hidden = true;
    }
    panel.querySelector("#companion-section-equipment > .companion-subheading").insertAdjacentHTML("beforeend", '<button type="button" data-comp-browse="inventory">Open inventory</button>');
    const spellSection = panel.querySelector(".companion-training-lower > section:last-child");
    spellSection.querySelector(".companion-subheading").insertAdjacentHTML("beforeend", '<button type="button" data-comp-browse="spells">Open spell book</button>');
    for (const button of spellSection.querySelectorAll("[data-comp-unlearn]")) {
      const spell = getSpell(button.dataset.compUnlearn); if (!spell) continue;
      const info = Combat.previewSheetDetails(entry.character, null, spell).spell;
      button.closest("article").querySelector("div").insertAdjacentHTML("beforeend", `<p class="companion-spell-numbers">Power ${info.power} · Mana ${info.mana} · ${info.ap} AP</p><button type="button" data-comp-inspect-spell="${esc(spell.id)}">Full spell details</button>`);
    }
    if (picker) renderPicker();
  }
  $("companion-sheet").addEventListener("click", event => {
    const button = event.target.closest("button"); if (!button || button.disabled || !sheetEntry) return;
    if (button.dataset.compBrowseSlot) open("equipment", button.dataset.compBrowseSlot, sheetEntry.character.equipment[button.dataset.compBrowseSlot], button);
    else if (button.dataset.compInspectItem) open("inventory", null, button.dataset.compInspectItem, button);
    else if (button.dataset.compInspectSpell) open("spells", null, button.dataset.compInspectSpell, button);
    else if (button.dataset.compBrowse) open(button.dataset.compBrowse, null, null, button);
    else if (button.dataset.compClassSelect) {
      const id = button.dataset.compClassSelect;
      renderClasses(sheetEntry, button.closest(".companion-class-section"), id);
      $("companion-sheet").querySelector(`[data-comp-class-select="${id}"]`)?.focus({ preventScroll: true });
    } else if (button.dataset.compClearSlot && !locked()) {
      const select = $("companion-sheet").querySelector(`[data-comp-gear="${button.dataset.compClearSlot}"]`);
      select.value = ""; select.dispatchEvent(new Event("change", { bubbles: true }));
    }
  });
  $("companion-sheet").addEventListener("input", event => {
    if (event.target.matches("[data-comp-class-search]")) applyClassFilter(event.target.closest(".companion-class-section"), event.target.value);
  });
  modal.querySelectorAll("[data-comp-picker-close]").forEach(button => button.addEventListener("click", close));
  $("companion-picker-search").addEventListener("input", renderPicker);
  modal.addEventListener("click", event => {
    const button = event.target.closest("button"); if (!button || button.disabled || !picker || !sheetEntry) return;
    if (button.dataset.compPickerSelect) { picker.selected = button.dataset.compPickerSelect; renderPicker(); return; }
    const action = button.dataset.compPickerAction; if (!action || locked()) return;
    const id = picker.selected, c = sheetEntry.character;
    if (action === "equip") {
      if (equipmentReason(c, picker.slot, getItem(id))) return renderPicker();
      const select = $("companion-sheet").querySelector(`[data-comp-gear="${picker.slot}"]`);
      select.value = id; select.dispatchEvent(new Event("change", { bubbles: true })); close();
    } else if (action === "give" || action === "learn") {
      $(action === "give" ? "companion-give-item" : "companion-spell-choice").value = id;
      $(action === "give" ? "companion-give" : "companion-learn-spell").click(); close();
    } else if (action === "take") {
      $("companion-sheet").querySelector(`[data-comp-take="${id}"]`)?.click(); renderPicker();
    }
  });
  document.addEventListener("keydown", event => {
    if (modal.hidden || !$("settings-modal").hidden) return;
    if (event.key === "Escape") { event.preventDefault(); event.stopImmediatePropagation(); close(); }
    if (event.key === "Tab") {
      const controls = [...modal.querySelectorAll("button:not(:disabled), input:not(:disabled)")].filter(control => !control.classList.contains("modal-backdrop"));
      if (event.shiftKey && document.activeElement === controls[0]) { event.preventDefault(); controls.at(-1)?.focus(); }
      else if (!event.shiftKey && document.activeElement === controls.at(-1)) { event.preventDefault(); controls[0]?.focus(); }
    }
  }, true);
  window.CompanionSheetDetails = { mount, refreshEquipment, close };
})();

/* Limitless (port task 7, decisions L1–L28): the character sheet's Limitless button, turning it on and off for you and
   your companions, its unlock on Anatemis's first defeat and the pop-up that announces it. The rules are in
   limitless-engine.js; enemies, drops and Exotic items read `limitless.active` where they act. */
(() => {
  "use strict";
  const $ = id => document.getElementById(id);
  const button = $("limitless-toggle");
  let pendingNotice = false;
  const notice = document.createElement("div");
  notice.id = "limitless-notice"; notice.className = "modal-layer"; notice.hidden = true;
  notice.innerHTML = `<section class="spellbook-dialog limitless-dialog" role="dialog" aria-modal="true" aria-labelledby="limitless-notice-title">
    <header class="dialog-header"><div><p class="eyebrow">A NEW SYSTEM</p><h2 id="limitless-notice-title"><em class="exotic-text">Limitless</em> unlocked</h2></div><button class="modal-close" id="limitless-notice-close" type="button" aria-label="Close">×</button></header>
    <p class="limitless-notice-copy">You have surpassed the limits of ordinary progression. A new challenge awaits those who wish to go further. Activate Limitless at the top of your character sheet to continue growing stronger, face increasingly powerful enemies, and discover equipment beyond the known boundaries of rarity.</p>
    <button class="backup-button" id="limitless-notice-ok" type="button">Continue</button>
  </section>`;
  document.body.appendChild(notice);
  const playerSheet = () => ({ level, exp, stats, manualAllocations, availablePoints, limitless });
  const writePlayer = sheet => { level = sheet.level; exp = sheet.exp; availablePoints = sheet.availablePoints; };
  // Gear above Level 100 comes off while Limitless is off (L10, L25); it stays in the pack.
  function removeUnwearable(slots) {
    const removed = [];
    for (const [slot, id] of Object.entries(slots || {})) {
      if (!id || LimitlessRules.wearable(getItem(id), false)) continue;
      slots[slot] = null; removed.push(id);
    }
    return removed;
  }
  function render() {
    if (!button) return;
    button.hidden = !limitless.unlocked;
    button.classList.toggle("is-active", limitless.active);
    button.setAttribute("aria-pressed", String(limitless.active));
    button.textContent = limitless.active ? "Limitless · On" : "Limitless · Off";
    const fighting = Boolean(window.Combat?.active());
    button.disabled = fighting;
    button.title = fighting ? "Limitless can't be switched during a battle." : limitless.active
      ? "Limitless is on: Level cap 200, enemies scale to your level, Exotic items work. Turn it off to go back to Level 100."
      : "Turn Limitless on: Level cap 200, stronger enemies, doubled masterwork drops and Exotic items.";
    document.body.classList.toggle("is-limitless", limitless.active);
  }
  // Turns Limitless on or off (L2, L4, L20, L21): only once unlocked, never during a battle.
  function setActive(on) {
    if (!limitless.unlocked || window.Combat?.active() || limitless.active === Boolean(on)) return false;
    const removed = [];
    if (on) {
      limitless.active = true;
      const sheet = playerSheet(); LimitlessRules.restore(sheet); writePlayer(sheet);
      for (const companion of companions) { companion.character.limitless ||= LimitlessRules.normalize(null, true); LimitlessRules.restore(companion.character); }
    } else {
      const sheet = playerSheet(); LimitlessRules.keepAside(sheet); writePlayer(sheet);
      for (const companion of companions) {
        companion.character.limitless ||= LimitlessRules.normalize(null, true);
        LimitlessRules.keepAside(companion.character);
        removed.push(...removeUnwearable(companion.character.equipment));
      }
      limitless.active = false;
      removed.push(...removeUnwearable(equipment));
    }
    createStatCards(); updateDerivedValues(); renderClasses(); renderEquipmentSlots(); renderOwnedItems();
    window.Companions?.render?.();
    render(); saveCharacter();
    const status = document.getElementById("inventory-status");
    if (status && removed.length) status.textContent = `Limitless is off: ${removed.length} piece${removed.length === 1 ? "" : "s"} above Level 100 went back to the pack.`;
    return true;
  }
  // Anatemis's first defeat unlocks it (1.1); the pop-up waits until the loot is settled.
  function onVictory(state) {
    if (limitless.unlocked || !state?.actors.some(actor => actor.enemyDefinitionId === LimitlessRules.ANATEMIS_ID && !actor.summoned && actor.character.currentHp <= 0)) return false;
    limitless.unlocked = true; pendingNotice = true;
    render(); saveCharacter();
    return true;
  }
  function showPendingNotice() {
    if (!pendingNotice) return false;
    pendingNotice = false;
    notice.hidden = false; updateModalState();
    $("limitless-notice-ok").focus();
    return true;
  }
  const closeNotice = () => { notice.hidden = true; updateModalState(); };
  $("limitless-notice-close").addEventListener("click", closeNotice);
  $("limitless-notice-ok").addEventListener("click", closeNotice);
  notice.addEventListener("keydown", event => { if (event.key === "Escape") { event.preventDefault(); closeNotice(); } });
  button?.addEventListener("click", () => setActive(!limitless.active));
  window.Limitless = { render, setActive, onVictory, showPendingNotice, isActive: () => limitless.active, isUnlocked: () => limitless.unlocked, removeUnwearable };
  render();
})();

/* Hearth Home (port task 3, the Shop Rework, 2026-11-06; user decisions R14, R15): Hearthforge's forge on the
   south-west edge of Cinder Isle's volcano. Ornn, God of the Forge, no longer a Great Hunt, waits there; his
   defeat opens the forge (its store and anvil are the shop window's "hearth" kind, shop.js), and he can be
   challenged again, like Tankina at the Church of Talos. Its scene and map art are placeholders (R1). */
(() => {
  "use strict";
  const $ = id => document.getElementById(id);
  const SITE_ID = "hearth-home", BOSS_ID = "ornn-god-of-the-forge-unique-level-48", ROSTER = ["Ornn, God of the Forge"];
  const modal = document.createElement("div");
  modal.id = "hearth-home-modal"; modal.className = "modal-layer"; modal.hidden = true;
  modal.innerHTML = `<section class="spellbook-dialog church-panel hearth-panel" role="dialog" aria-modal="true" aria-labelledby="hearth-home-title">
    <header class="dialog-header"><div><p class="eyebrow">CINDER ISLE · LEVEL 48</p><h2 id="hearth-home-title">Hearth <em>Home</em></h2></div><button class="modal-close" id="hearth-home-close" type="button" aria-label="Close Hearth Home">×</button></header>
    <div class="church-content"><p id="hearth-home-description"></p><button class="backup-button" id="hearth-home-challenge" type="button">Challenge Ornn · Level 48</button>
    <button class="backup-button" id="hearth-home-enter" type="button">Enter the forge</button>
    <p id="hearth-home-status" role="status" aria-live="polite"></p></div>
  </section>`;
  document.body.appendChild(modal);
  let starting = false;
  const site = () => WorldRules.siteAt(worldState.position, worldState.regionId);
  const available = () => site()?.id === SITE_ID && !Combat.active() && !WorldMap.isTravelling() && !worldState.pendingEncounter && !worldState.pendingRegionTransition && currentHp > 0;
  const unlocked = () => Boolean(worldState.defeatedSiteBosses?.includes(BOSS_ID));
  function render() {
    $("hearth-home-description").textContent = unlocked()
      ? "The forge burns for you now. Hearthforge sells its weapons and armor here at 10% off, and its anvil reforges at 15% off: the only anvil that takes masterworks. Ornn will take your challenge again whenever you ask."
      : "Ornn, God of the Forge, works the great anvil at the volcano's edge and lets no one into Hearthforge's forge. Defeat him to open it.";
    $("hearth-home-challenge").disabled = !available() || starting;
    $("hearth-home-enter").disabled = !available() || !unlocked();
    $("hearth-home-enter").title = unlocked() ? "Buy Hearthforge weapons and armor, and reforge" : "Defeat Ornn to open the forge.";
  }
  function open() {
    if (!available()) return;
    modal.hidden = false; $("hearth-home-status").textContent = ""; render(); updateModalState();
    $("hearth-home-challenge").focus();
  }
  function close() { modal.hidden = true; updateModalState(); $("map-hearth-home")?.focus({ preventScroll: true }); }
  async function challenge() {
    if (!available() || starting) return false;
    starting = true; render();
    try {
      const result = await Combat.startEncounter([...ROSTER], "At Hearth Home, Ornn sets down his hammer and takes up your challenge.");
      if (result) { modal.hidden = true; $("map-modal").hidden = true; updateModalState(); }
      return Boolean(result);
    } catch (error) { $("hearth-home-status").textContent = error.message; return false; }
    finally { starting = false; render(); WorldMap.render(); }
  }
  function enter() {
    if (!available() || !unlocked()) return;
    modal.hidden = true; updateModalState();
    window.Shop?.open("hearth");
  }
  $("hearth-home-close").addEventListener("click", close);
  $("hearth-home-challenge").addEventListener("click", challenge);
  $("hearth-home-enter").addEventListener("click", enter);
  modal.addEventListener("keydown", event => { if (event.key === "Escape") { event.preventDefault(); close(); } });
  $("map-hearth-home")?.addEventListener("click", open);
  window.HearthHome = {
    open, close, challenge, enter, unlocked, available, siteId: SITE_ID, bossId: BOSS_ID, enemyRoster: () => [...ROSTER],
    onVictory(state) {
      if (!state?.actors.some(actor => actor.enemyDefinitionId === BOSS_ID && !actor.summoned && actor.character.currentHp <= 0)) return;
      worldState.defeatedSiteBosses = [...new Set([...(worldState.defeatedSiteBosses || []), BOSS_ID])];
      $("map-status").textContent = "Ornn has fallen. Hearth Home's forge is open to you.";
    },
  };
})();

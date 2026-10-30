/* Game-wide preferences: independent of character saves and safe without storage. */
(() => {
  "use strict";
  const key = "stat-fick-game-settings";
  let preferences = { battlePace: "fast", combatAnimations: true, battleUi: "stage", battleSpeed: "normal" };
  try {
    const saved = JSON.parse(localStorage.getItem(key));
    if (saved?.battlePace === "slow") preferences.battlePace = "slow";
    if (saved?.combatAnimations === false) preferences.combatAnimations = false;
    if (saved?.battleUi === "classic") preferences.battleUi = "classic";
    if (saved?.battleSpeed === "instant") preferences.battleSpeed = "instant";
  } catch { /* Defaults also work with unavailable or malformed storage. */ }
  const pace = document.getElementById("battle-pace");
  const animations = document.getElementById("combat-animations");
  const interfaceSelect = document.getElementById("battle-interface");
  function paint() {
    if (pace) pace.value = preferences.battlePace;
    if (animations) animations.checked = preferences.combatAnimations;
    if (interfaceSelect) interfaceSelect.value = preferences.battleUi;
    document.body.classList.toggle("combat-animations-off", !preferences.combatAnimations);
    document.body.dataset.battleUi = preferences.battleUi;
  }
  function update(next) {
    preferences = { ...preferences, ...next };
    try { localStorage.setItem(key, JSON.stringify(preferences)); } catch { /* Optional persistence. */ }
    paint();
    window.dispatchEvent(new CustomEvent("statfick:settingschange", { detail: { ...preferences } }));
  }
  window.GameSettings = {
    state: () => ({ ...preferences }),
    setBattlePace(value) { if (["fast", "slow"].includes(value)) update({ battlePace: value }); },
    setCombatAnimations(value) { if (typeof value === "boolean") update({ combatAnimations: value }); },
    setBattleInterface(value) { if (["stage", "classic"].includes(value)) update({ battleUi: value }); },
    setBattleSpeed(value) { if (["normal", "instant"].includes(value)) update({ battleSpeed: value }); },
    enemyDelay(firstAction = false) { return preferences.battlePace === "slow" ? (firstAction ? 2400 : 1800) : 650; }
  };
  pace?.addEventListener("change", () => window.GameSettings.setBattlePace(pace.value));
  animations?.addEventListener("change", () => window.GameSettings.setCombatAnimations(animations.checked));
  interfaceSelect?.addEventListener("change", () => window.GameSettings.setBattleInterface(interfaceSelect.value));
  paint();
})();

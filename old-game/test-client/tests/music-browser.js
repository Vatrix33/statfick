async function runMusicChecks() {
  const assert = (value, message) => { if (!value) throw new Error(message); };
  const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
  const button = document.getElementById("settings-open-button");
  const modal = document.getElementById("settings-modal");
  const slider = document.getElementById("sound-volume");
  const label = document.getElementById("sound-volume-value");
  const changeVolume = value => { slider.value = String(value); slider.dispatchEvent(new Event("input", { bubbles: true })); };

  assert(!document.getElementById("music-toggle") && button.textContent.includes("Settings"), "Settings replaces the mute button");
  button.click();
  assert(!modal.hidden && button.getAttribute("aria-expanded") === "true" && document.activeElement === slider, "Settings opens and focuses the volume slider");
  assert(getComputedStyle(modal).zIndex === "60" && modal.querySelector(".settings-dialog").getBoundingClientRect().width <= 440, "Settings uses a compact window above other menus");
  changeVolume(37);
  assert(Music.state().volume === .37 && label.textContent === "37%" && slider.getAttribute("aria-valuetext") === "37%", "Dragging the slider immediately updates the volume and percentage");
  assert(localStorage.getItem("stat-fick-sound-volume") === "0.37", "Volume is saved immediately");
  assert(Music.state().audioReady, "The native audio graph initializes from settings");
  changeVolume(0);
  assert(!Music.state().enabled && !Music.state().playing, "Zero volume silences and stops playback");
  changeVolume(83);
  assert(Music.state().enabled && Music.state().volume === .83, "Raising volume restores sound");

  const lastSetting = document.getElementById("combat-animations");
  lastSetting.focus();
  lastSetting.dispatchEvent(new KeyboardEvent("keydown", { key: "Tab", bubbles: true, cancelable: true }));
  assert(document.activeElement === modal.querySelector(".modal-close"), "Tab stays inside settings");
  document.activeElement.dispatchEvent(new KeyboardEvent("keydown", { key: "Tab", shiftKey: true, bubbles: true, cancelable: true }));
  assert(document.activeElement === lastSetting, "Shift-Tab stays inside settings");
  slider.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape", bubbles: true, cancelable: true }));
  assert(modal.hidden && document.activeElement === button && button.getAttribute("aria-expanded") === "false", "Escape closes settings and returns focus");
  button.click(); modal.querySelector(".modal-backdrop").click();
  assert(modal.hidden && !document.body.classList.contains("modal-open"), "Backdrop closes settings without leaving scrolling locked");

  applyCharacter(normalizeCharacter({ ...defaultCharacter(), raceId: "human", stats: { ...defaultStats, speed: 200 } }));
  WorldMap.setAmbientEnabled(false);
  document.getElementById("map-open-button").click();
  button.click();
  slider.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape", bubbles: true, cancelable: true }));
  assert(modal.hidden && !document.getElementById("map-modal").hidden && document.body.classList.contains("modal-open"), "Closing settings keeps an underlying map open");
  document.querySelector("#map-modal [data-map-close]").click();

  for (const region of Object.keys(WorldRules.regions)) {
    worldState.regionId = region; Music.sync();
    assert(Music.state().volume === .83, "Region changes preserve volume: " + region);
  }
  worldState.regionId = "alder-vale"; WorldRules.setActiveRegion(worldState.regionId); Music.sync();
  assert(await Combat.startEncounter(["Goblin"]), "Normal battle starts");
  assert(Music.state().theme === "combat" && Music.state().volume === .83, "Battle music uses the same volume");
  document.getElementById("combat-sheet-button").click();
  assert(Music.state().theme === "combat", "Battle music persists on the character sheet");
  Combat.restore(null);
  assert(await Combat.startEncounter(["Great Imp Ji'ruan"]), "Boss battle starts");
  Music.sync();
  assert(Music.state().theme === "great-imp-jiruan" && document.getElementById("settings-current-track").textContent.includes("Ji'ruan"), "Settings shows the active boss theme");
  Combat.restore(null);

  async function reopenedVolume() {
    const frame = document.createElement("iframe");
    frame.hidden = true; frame.src = "index.html?volume-check=" + Date.now(); document.body.appendChild(frame);
    try {
      for (let i = 0; i < 600 && !frame.contentWindow?.Music; i++) await wait(50);
      assert(frame.contentWindow?.Music, "Reopened game loads");
      frame.contentWindow.WorldMap.setAmbientEnabled(false);
      return frame.contentWindow.Music.state().volume;
    } finally { frame.remove(); }
  }
  assert(await reopenedVolume() === .83, "Volume survives reopening the game");
  localStorage.removeItem("stat-fick-sound-volume"); localStorage.setItem("stat-fick-music-enabled", "false");
  assert(await reopenedVolume() === 0, "Old mute preference migrates to zero volume");
  localStorage.setItem("stat-fick-sound-volume", "0.62");
  assert(await reopenedVolume() === .62, "New volume overrides the legacy mute preference");
  Music.setVolume(.62);
  button.click();
  await wait(100);
}

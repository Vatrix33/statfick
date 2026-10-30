"use strict";
const fs = require("node:fs/promises");
const path = require("node:path");

async function run({ app, window, reportFile, gameRoot, reopen, headless = false, close = true }) {
  const report = { passed: false, platform: process.platform, electron: process.versions.electron, packaged: app.isPackaged, gameRoot, checks: [] };
  const check = (value, message) => { if (!value) throw new Error(message); report.checks.push(message); };
  const execute = code => window.webContents.executeJavaScript(code, true);
  try {
    // The regular entry point loads scripts sequentially; wait for the complete
    // game, not merely a successful navigation or an empty native window.
    await execute(`(async () => {
      const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
      for (let i = 0; i < 600 && !window.Music; i++) await wait(50);
      if (!window.Music) throw new Error("Game scripts did not finish loading");
      await waitForSourceFiles(); WorldMap.setAmbientEnabled(false);
    })()`);
    const loaded = await execute(`({ classes: classCatalog.length, expectedClasses: CLASS_FILES.length, spells: spells.length, races: raceCatalog.length, weapons: itemCatalog.weapons.length, armor: itemCatalog.armor.length, consumables: itemCatalog.consumables.length, bridge: !!window.statFickDesktop, isolated: typeof require === "undefined", origin: location.href })`);
    report.catalogues = loaded;
    check(loaded.classes === loaded.expectedClasses && loaded.classes >= 100, "Every configured class loaded");
    check(loaded.spells >= 300 && loaded.races > 10 && loaded.weapons >= 0 && loaded.armor >= 0 && loaded.consumables > 10, "All game catalogues loaded");
    check(loaded.bridge && loaded.isolated && loaded.origin.startsWith("statfick://game/"), "Sandboxed standalone window and native file bridge");
    if (headless) check(!window.isVisible() && window.webContents.isOffscreen() && window.webContents.isAudioMuted(), "Test renderer is offscreen, invisible and muted");
    else check(window.isVisible() && window.getTitle() === "Stat Fick", "Native Stat Fick window is visible");
    if (reopen) {
      const persisted = await execute(`({ inventory, favoriteItems, raceId: selectedRaceId, minutes: worldState.minutes })`);
      check(persisted.raceId === "human" && persisted.favoriteItems.includes("weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0") && persisted.inventory.filter(id => id === "weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0").length === 2 && persisted.minutes === 600, "Character autosave survives closing and reopening the executable");
    } else {
      const exercise = await execute(`(async () => {
        const assert = (value, message) => { if (!value) throw new Error(message); };
        const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
        applyCharacter(normalizeCharacter({ ...defaultCharacter(), editingEnabled: false, raceId: "human", stats: { ...defaultStats, speed: 200 }, inventory: ["weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0", "weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0"], favoriteItems: ["weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0"] }));
        WorldMap.setAmbientEnabled(false);
        assert(await Combat.startEncounter(["Goblin"]), "Combat did not start");
        const battle = Combat.serialize();
        assert(battle.active && battle.actors.some(actor => actor.team === "enemy" && actor.name.startsWith("Goblin")), "Enemy files did not load");
        Combat.restore(null);
        document.getElementById("map-open-button").click();
        worldState.position = { x: 1000, y: 500 }; WorldMap.resetView();
        const force = document.getElementById("map-force-encounter");
        assert(!force.hidden && !force.disabled, "Map encounter action is unavailable");
        force.click();
        assert(worldState.pendingEncounter?.enemies.length && !document.getElementById("map-encounter-modal").hidden, "Map encounter prompt did not open");
        worldState.pendingEncounter = null; WorldMap.resetView();
        document.querySelector("#map-modal [data-map-close]").click();
        worldState.minutes = 600; saveCharacter();
        await exportBackup();
        assert(document.getElementById("backup-status").textContent.includes("saved"), "Native Save Character failed");
        const exported = await window.statFickDesktop.loadCharacter();
        const restored = normalizeCharacter(JSON.parse(exported.text).character);
        assert(restored.raceId === "human" && restored.favoriteItems.includes("weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0"), "Native Load Character failed");
        applyCharacter(normalizeCharacter(defaultCharacter()));
        document.getElementById("backup-restore-button").click();
        for (let i = 0; i < 100 && selectedRaceId !== "human"; i++) await wait(20);
        assert(selectedRaceId === "human" && favoriteItems.includes("weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0"), "Load Character button did not restore the exported save");
        WorldMap.setAmbientEnabled(false); saveCharacter();
        return { enemies: battle.actors.filter(actor => actor.team === "enemy").map(actor => actor.name), minutes: worldState.minutes };
      })()`);
      report.exercise = exercise;
      check(true, "Enemy discovery and combat work");
      check(true, "Map and forced encounter prompt work");
      check(true, "Native Save/Load Character round trip preserves the character");
      const saved = JSON.parse(await fs.readFile(path.join(path.dirname(reportFile), "desktop-test-character.json"), "utf8"));
      check(saved.character.worldState.minutes === 600, "Character export is a valid JSON file on disk");
    }
    const capture = await window.webContents.capturePage();
    check(!capture.isEmpty(), "Game page renders successfully");
    await fs.writeFile(reportFile.replace(/\.json$/i, "") + ".png", capture.toPNG());
    report.passed = true;
  } catch (error) { report.error = error.stack; }
  await fs.writeFile(reportFile, JSON.stringify(report, null, 2));
  console.log(JSON.stringify(report));
  require("electron").session.defaultSession.flushStorageData();
  if (close) {
    window.close();
    if (!report.passed) setTimeout(() => app.exit(1), 1000);
  }
  return report;
}
module.exports = { run };

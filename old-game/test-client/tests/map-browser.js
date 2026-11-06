/* Run inside a fresh browser profile after the app has loaded. */
async function runMapChecks() {
  const assert = (value, message) => { if (!value) throw new Error(message); };
  const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
  const $ = id => document.getElementById(id);
  const click = id => $(id).dispatchEvent(new MouseEvent("click", { bubbles: true }));
  const pointClick = (x, y) => {
    const p = new DOMPoint(x, y).matrixTransform($("regional-map").getScreenCTM());
    $("regional-map").dispatchEvent(new MouseEvent("click", { bubbles: true, clientX: p.x, clientY: p.y }));
  };
  applyCharacter(normalizeCharacter({ ...defaultCharacter(), classLevels: { warrior: 5 } }));
  WorldMap.setAmbientEnabled(false);
  editingEnabled = false; exp = CombatRules.experienceRequired(level); updateDerivedValues();
  assert($("level-icon").classList.contains("has-level-up"), "The Level icon flashes when enough EXP is available");
  exp -= 1; updateDerivedValues();
  assert(!$("level-icon").classList.contains("has-level-up"), "The Level icon stops flashing below the EXP requirement");
  editingEnabled = true; exp = 0; updateDerivedValues();
  assert(!$("level-icon").classList.contains("has-level-up"), "Dev-mode does not create a false level-up alert");
  click("map-open-button");
  assert(!$("map-modal").hidden && document.body.classList.contains("modal-open"), "Map opens as modal");
  assert($("map-clock").textContent === "08:00", "Start time is 08:00");
  assert(getItem("consumable-camping-supplies")?.mapAction === "camp", "Camping Supplies load into the item catalogue");
  assert($("map-location").textContent.includes("Alderkeep"), "Character starts in castle city");
  assert($("map-zone").textContent === "Town", "Current zone appears above the map");
  assert($("map-zone-level").textContent === "Safe", "Current zone shows its encounter recommendation");
  assert(WorldRules.zoneAt({ x: 420, y: 490 }) === "Town", "Town zone");
  assert(WorldRules.zoneAt({ x: 500, y: 462 }) === "Road", "Road zone");
  assert(WorldRules.zoneAt({ x: 1125, y: 101.25 }) === "Road", "Millhaven road continues north beyond the region");
  assert(WorldRules.zoneAt({ x: 143.75, y: 826.25 }) === "Road", "Briarfield road continues west beyond the region");
  assert(WorldRules.terrainAt({ x: 1138, y: 101.25 }) === "road", "Road hitboxes include the slightly wider edge");
  assert(WorldRules.terrainAt({ x: 1139, y: 101.25 }) !== "road", "Road hitbox increase remains narrow");
  assert(WorldRules.zoneAt({ x: 1110, y: 300 }) === "Plains - near Town", "Near-town plains zone");
  assert(WorldRules.zoneAt({ x: 1000, y: 500 }) === "Plains", "Plains zone");
  assert(WorldRules.zoneAt({ x: 1345, y: 512 }) === "Forest", "Forest zone");
  assert(WorldRules.zoneAt({ x: 365, y: 213 }) === "Deep Forest", "Deep Forest zone");
  assert(EncounterRules.chances.Town === 0 && EncounterRules.chances.Road === 0, "Towns and roads are encounter-safe");
  assert(EncounterRules.chances["Plains - near Town"] === 15 && EncounterRules.chances.Plains === 30 && EncounterRules.chances.River === 20 && EncounterRules.chances.Forest === 45 && EncounterRules.chances["Deep Forest"] === 60, "Travel zones expose their reduced encounter chances");
  assert(EncounterRules.profiles["Plains - near Town"].minimum === 1 && EncounterRules.profiles["Deep Forest"].maximum === 15, "Regional encounters progress from level 1 near towns to level 15 in the Deep Forest");
  const zoneRiverPoint = Array.from({ length: 900 }, (_, index) => ({ x: 650 + index, y: 500 })).find(point => WorldRules.terrainAt(point) === "river");
  assert(zoneRiverPoint && WorldRules.zoneAt(zoneRiverPoint) === "River", "River zone");
  worldState.minutes = 539;
  const hourlyStats = currentStats(), hourlyMaxHp = calculateMaxHpWithArtifacts(hourlyStats), hourlyMaxMana = calculateMaxManaWithEffects(hourlyStats);
  currentHp = 1; currentMana = 1; advanceWorldTime(1);
  assert(currentHp === 1 + Math.max(1, Math.floor(hourlyMaxHp * .05)), "Hourly HP recovery");
  assert(currentMana === 1 + Math.max(1, Math.floor(hourlyMaxMana * .05)), "Hourly Mana recovery");
  worldState.minutes = 480;
  assert(document.querySelectorAll("g[data-map-town]").length === 4, "One city and three towns");
  assert(document.querySelector("g[data-map-town]").getBoundingClientRect().width > 0, "Map geometry exists");
  click("map-fit");
  const view = () => ({ width: $("regional-map").viewBox.baseVal.width, height: $("regional-map").viewBox.baseVal.height });
  const fitted = view();
  assert(fitted.width >= 1599 && fitted.height >= 999, "Fit region shows the whole map");
  click("map-zoom-in");
  assert(view().width < fitted.width, "Zoom enlarges map");
  click("map-fit");
  assert(!$("map-select-character"), "Travel no longer requires a separate character-selection control");
  pointClick(700, 490);
  assert(worldState.destination?.x > 690 && WorldMap.isTravelling(), "One map click sets a destination and immediately starts travel");
  assert(WorldRules.routeTravelMinutes(worldState.position, worldState.route) <= WorldRules.travelMinutes(worldState.position, worldState.destination) + 1e-6, "Travel automatically chooses a route no slower than direct travel");
  assert(!$("map-follow-roads"), "Follow Roads control is removed");
  await wait(400);
  click("map-pause");
  assert(worldState.position.x > 420 && worldState.position.x < 700, "Character moves toward marker");
  assert(worldState.minutes > 480, "Travel advances the world clock");
  const paused = JSON.stringify(worldState);
  await wait(200);
  assert(JSON.stringify(worldState) === paused, "Pausing stops clock and movement");
  assert(WorldRules.speedAt({ x: 430, y: 487 }) === 7, "Road travel uses 7 km/h");
  assert(WorldRules.speedAt({ x: 1000, y: 500 }) === 5, "Ordinary land uses 5 km/h");
  assert(WorldRules.speedAt({ x: 1000, y: 500 }, 1) === 6, "Traveler bonus adds to terrain speed");
  const riverPoint = Array.from({ length: 900 }, (_, index) => ({ x: 650 + index, y: 500 })).find(point => WorldRules.terrainAt(point) === "river");
  assert(riverPoint && WorldRules.speedAt(riverPoint) === 2, "Off-road river crossing uses 2 km/h");
  click("map-pause");
  document.querySelector("[data-map-close]").click();
  const closed = JSON.stringify(worldState);
  await wait(200);
  assert(JSON.stringify(worldState) === closed && $("map-modal").hidden, "Closing pauses travel");
  const savedJourney = backupPayload();
  applyCharacter(normalizeCharacter(defaultCharacter()));
  applyCharacter(normalizeCharacter(savedJourney.character));
  assert(JSON.stringify(worldState) === closed, "Loading preserves a paused destination and location");

  // A short actual trip across midnight resets daily skills exactly at the boundary.
  worldState.minutes = 1439;
  activeSkills = { "warrior:battle-cry": { active: true, lastUsedDay: todayKey() } };
  const midnightStart = { ...worldState.position };
  click("map-open-button");
  pointClick(midnightStart.x + 35, midnightStart.y);
  await wait(400);
  click("map-pause");
  assert(WorldRules.day(worldState.minutes) === 2, "Travel crosses midnight");
  assert(Object.keys(activeSkills).length === 0 && todayKey() === "world-day-2", "Daily skills reset");
  assert(WorldRules.lighting(worldState.minutes).darkness === .66, "Midnight is night");
  assert(WorldRules.lighting(360).darkness === 0 && WorldRules.lighting(1079).darkness === 0, "Daylight from 06:00 to 18:00");
  assert(WorldRules.lighting(1140).darkness > 0 && WorldRules.lighting(1140).darkness < .66, "Dusk is gradual");

  // Arrival caps elapsed time to the actual remaining distance.
  const shortTrip = WorldRules.normalize({ position: { x: 1000, y: 500 }, destination: { x: 1000 + WorldRules.UNITS_PER_KM, y: 500 } });
  const result = WorldRules.step(shortTrip, 500);
  assert(result.arrived && Math.abs(result.elapsed - 12) < 1e-8 && !shortTrip.destination && shortTrip.position.x === 1000 + WorldRules.UNITS_PER_KM, "Arrival does not overshoot");
  assert(Math.abs(shortTrip.distanceTraveledKm - 1) < 1e-8, "Travel distance is recorded in kilometers");
  worldState.position = { x: 700, y: 500 }; worldState.destination = null;
  WorldMap.resetView();
  assert($("map-rest").disabled, "Resting unavailable in plains");
  const beforeRest = worldState.minutes;
  $("map-rest").dispatchEvent(new MouseEvent("click"));
  assert(worldState.minutes === beforeRest, "Rest handler rejects wilderness");
  inventory = inventory.filter(id => id !== "consumable-camping-supplies");
  WorldMap.resetView();
  assert($("map-camp").disabled, "Camping requires supplies");
  inventory.push("consumable-camping-supplies");
  worldState.minutes = 1300; currentHp = 1; currentMana = 0;
  activeSkills = { "warrior:battle-cry": { active: true, lastUsedDay: todayKey() } };
  WorldMap.resetView(); click("map-camp");
  assert(worldState.minutes === 1780 && !inventory.includes("consumable-camping-supplies"), "Camping takes 8 hours and consumes supplies");
  assert(currentHp === calculateMaxHpWithArtifacts(currentStats()) && currentMana === calculateMaxManaWithEffects(currentStats()), "Camping fully restores resources");
  assert(Object.keys(activeSkills).length === 0, "Camping across midnight resets daily abilities");
  worldState.position = { x: 1110, y: 225 }; worldState.minutes = 1380;
  currentHp = 1; currentMana = 0;
  activeSkills = { "warrior:battle-cry": { active: true, lastUsedDay: todayKey() } };
  WorldMap.resetView(); click("map-rest");
  assert(worldState.minutes === 1980 && $("map-clock").textContent === "09:00", "10-hour rest crosses midnight");
  assert(currentHp === calculateMaxHpWithArtifacts(currentStats()), "Rest fills HP after buff reset");
  assert(currentMana === calculateMaxManaWithEffects(currentStats()), "Rest fills Mana");
  assert(Object.keys(activeSkills).length === 0, "Rest resets daily abilities at midnight");
  const refreshedKey = todayKey();
  activeSkills = { "warrior:battle-cry": { active: false, lastUsedDay: refreshedKey } };
  click("map-rest");
  assert(worldState.minutes === 2580 && activeSkills["warrior:battle-cry"].lastUsedDay === refreshedKey, "Rest within a day preserves spent daily uses");
  worldState.minutes = 1980;
  const saved = backupPayload();
  assert(saved.character.worldState.position.x === 1110 && saved.character.worldState.minutes === 1980, "Backup includes location and clock");
  applyCharacter(normalizeCharacter(defaultCharacter()));
  assert(worldState.minutes === 480 && worldState.position.x === 420, "New character defaults");
  applyCharacter(normalizeCharacter(saved.character));
  assert(worldState.minutes === 1980 && worldState.position.x === 1110, "Load restores location and clock");
  assert(!$("new-day-button"), "Character sheet no longer has a New Day button");
  assert(!$("restore-button").hidden && !document.querySelector('[data-restore-resource="hp"]').hidden, "Recovery controls show in Dev Mode");
  click("editing-toggle");
  assert($("restore-button").hidden && $("combat-open-button").hidden && $("exp-form").hidden && document.querySelector('[data-restore-resource="hp"]').hidden && document.querySelector('[data-restore-resource="mana"]').hidden, "Recovery, manual EXP, and manual Combat controls hide outside Dev Mode");
  const protectedExp = exp;
  $("exp-input").value = "50"; $("exp-form").dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
  assert(exp === protectedExp, "EXP cannot be added while Dev Mode is off");
  click("editing-toggle");
  assert(!$("exp-form").hidden, "Manual EXP controls show in Dev Mode");
  const migrated = normalizeCharacter({ activeSkills: { legacy: { lastUsedDay: "2026-09-09" } } });
  assert(migrated.activeSkills.legacy.lastUsedDay === "world-day-1", "Old daily cooldowns migrate conservatively");
  const malformed = WorldRules.normalize({ minutes: -1, position: { x: 9000, y: NaN }, destination: { x: "bad", y: 1 } });
  assert(malformed.minutes === 0 && malformed.position.x === 1580 && malformed.destination === null, "Invalid save data is normalized");

  // Travel/rest cannot alter an active battle, even from the character sheet.
  document.querySelector("[data-map-close]").click();
  click("combat-open-button");
  for (let i = 0; i < 100 && $("combat-start").disabled; i++) await wait(30);
  $("combat-enemy-quantity").value = 1;
  $("combat-setup-form").requestSubmit();
  assert(Combat.active(), "Combat setup still works with world clock");
  click("combat-sheet-button"); click("map-open-button");
  const locked = JSON.stringify(worldState);
  pointClick(500, 500); click("map-rest");
  assert(JSON.stringify(worldState) === locked && !advanceWorldTime(600), "Travel/rest blocked during combat");
  WorldMap.setAmbientEnabled(true);
  await wait(2200);
  WorldMap.setAmbientEnabled(false);
  assert(JSON.stringify(worldState) === locked, "Combat pauses passive world time");
  Combat.restore(null);
  worldState.position = { x: 1345, y: 512 }; worldState.destination = { x: 1400, y: 560 }; worldState.encounterTravelMinutes = WorldRules.ENCOUNTER_MINUTES - 1;
  const originalRandom = Math.random; Math.random = () => 0;
  assert(WorldMap.checkTravelEncounter(1), "The last minute of the travel interval triggers an eligible Forest encounter check");
  assert(!$("map-encounter-modal").hidden && !Combat.active(), "A travel encounter shows its enemy prompt before combat");
  const rolledEnemies = [...worldState.pendingEncounter.enemies];
  const encounterCounts = rolledEnemies.reduce((counts, name) => ({ ...counts, [name]: (counts[name] || 0) + 1 }), {});
  assert(Object.entries(encounterCounts).every(([name, count]) => $("map-encounter-enemies").textContent.includes(name) && (count === 1 || $("map-encounter-enemies").textContent.includes("×" + count))), "The prompt identifies the rolled enemies and quantity");
  click("map-encounter-fight");
  for (let index = 0; index < 150 && !Combat.active(); index += 1) await wait(20);
  Math.random = originalRandom;
  assert(Combat.active() && Combat.serialize().actors.slice(1).map(actor => actor.name.replace(/^Sparkling /, "").replace(/ \d+$/, "")).join(",") === rolledEnemies.join(","), "Fight starts with the prompted enemy roster");
  assert(!worldState.pendingEncounter, "Starting combat clears the saved pending encounter");
  assert(worldState.encounterTravelMinutes === 0, "Completed encounter intervals reset and persist independently of travel pauses");
  Combat.restore(null);
  click("map-open-button");
  worldState.position = { x: 1345, y: 512 }; worldState.destination = { x: 1400, y: 560 }; worldState.encounterTravelMinutes = WorldRules.ENCOUNTER_MINUTES - 1;
  Math.random = () => 0;
  assert(WorldMap.checkTravelEncounter(1), "A second deterministic encounter is available for the Run choice");
  click("map-encounter-run");
  for (let index = 0; index < 150 && worldState.pendingEncounter; index += 1) await wait(20);
  Math.random = originalRandom;
  assert(!Combat.active() && !worldState.pendingEncounter && $("map-encounter-modal").hidden, "A successful pre-combat Run dismisses the encounter without starting combat");
  assert(worldState.destination && $("map-status").textContent.includes("Escaped the encounter"), "Escaping preserves the paused route and reports success");
  worldState.guild.quest = { type: "hunt", enemy: "Goblin", zone: "Forest", point: { x: 1345, y: 512 }, progress: 0, complete: false, reward: { silver: 1, reputation: 1, rateVersion: 2 } };
  worldState.position = { ...worldState.guild.quest.point }; worldState.destination = { x: 1400, y: 560 };
  assert(WorldMap.checkGuildObjective() && worldState.pendingEncounter.objective === "hunt", "A marked Hunt preserves its objective in the encounter prompt");
  Math.random = () => 0;
  click("map-encounter-run");
  for (let index = 0; index < 150 && worldState.pendingEncounter; index += 1) await wait(20);
  Math.random = originalRandom;
  assert(worldState.guild.avoidObjective === "hunt" && !WorldMap.checkGuildObjective(), "Escaping a marked Hunt suppresses an immediate retrigger at the same marker");
  const encounterTime = worldState.minutes;
  WorldMap.setAmbientEnabled(true);
  worldState.pendingEncounter = { zone: "Forest", enemies: ["Goblin"], objective: null };
  WorldMap.render();
  await wait(2200);
  WorldMap.setAmbientEnabled(false);
  assert(worldState.minutes === encounterTime, "Passive world time pauses while a travel encounter awaits a decision");
  worldState.pendingEncounter = null; worldState.guild.quest = null; worldState.guild.avoidObjective = null;
  const normalizedEncounter = WorldRules.normalize({ pendingEncounter: { zone: "Forest", enemies: ["Goblin", "Goblin"] } });
  assert(normalizedEncounter.pendingEncounter?.enemies.length === 2, "Pending encounter choices survive save normalization");
  const malformedObjective = WorldRules.normalize({ guild: { artifactChallenge: { enemy: "Goblin", artifactId: "artifact-test", zone: "Forest" }, artifactOffers: ["same", "same"], avoidObjective: "artifact" } });
  assert(!malformedObjective.guild.artifactChallenge && malformedObjective.guild.artifactOffers.length === 1 && !malformedObjective.guild.avoidObjective, "Malformed Guild markers and duplicate offers are removed during save normalization");
  worldState = WorldRules.normalize(); WorldMap.resetView();
  click("map-open-button");
  saveCharacter();
  return { passed: true, time: $("map-clock").textContent, location: $("map-location").textContent };
}

async function runForcedEncounterChecks() {
  const assert = (value, message) => { if (!value) throw new Error(message); };
  const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
  const $ = id => document.getElementById(id);
  // Dispatch directly as well as checking visibility, so stale/hidden controls
  // cannot bypass the handler's safe-zone and busy-state guards.
  const click = id => $(id).dispatchEvent(new MouseEvent("click", { bubbles: true }));
  applyCharacter(normalizeCharacter({ ...defaultCharacter(), editingEnabled: false, stats: { ...defaultStats, speed: 200 } }));
  WorldMap.setAmbientEnabled(false);
  click("map-open-button");
  const button = $("map-force-encounter");
  assert(button && $("combat-open-button").hidden, "Force encounter is available without enabling Dev Mode");
  const originalGenerate = EncounterRules.generate, originalRoll = EncounterRules.roll, originalEscape = Combat.attemptTravelEscape;
  const generated = [];
  EncounterRules.generate = (zone, context) => {
    generated.push({ zone, ...context });
    return originalGenerate(zone, context, () => 0);
  };
  EncounterRules.roll = () => { throw new Error("Forced encounters must bypass the travel probability roll"); };
  const move = (position, regionId = "alder-vale") => {
    WorldMap.pause(false);
    worldState = WorldRules.normalize({ ...worldState, position, regionId, destination: null, route: [], pendingEncounter: null, pendingRegionTransition: null });
    WorldMap.resetView();
  };
  try {
    for (const [zone, position] of [["Town", { x: 420, y: 490 }], ["Road", { x: 500, y: 462 }]]) {
      move(position);
      assert($("map-zone").textContent === zone && button.hidden && button.disabled, zone + " hides the encounter button");
      click("map-force-encounter");
      assert(!worldState.pendingEncounter && generated.length === 0, zone + " cannot generate a forced encounter");
    }
    move({ x: 1000, y: 500 });
    assert(!button.hidden && !button.disabled, "Hostile Plains expose an enabled encounter button");
    const time = worldState.minutes, interval = worldState.encounterTravelMinutes;
    const svg = $("regional-map"), target = new DOMPoint(1100, 550).matrixTransform(svg.getScreenCTM());
    svg.dispatchEvent(new MouseEvent("click", { bubbles: true, clientX: target.x, clientY: target.y }));
    assert(WorldMap.isTravelling(), "Travel can be interrupted by a forced encounter");
    const destination = JSON.stringify(worldState.destination);
    click("map-force-encounter");
    const pending = JSON.stringify(worldState.pendingEncounter);
    assert(generated[0].zone === "Plains" && generated[0].regionId === "alder-vale" && generated[0].level === level, "Encounter generation uses the current zone, region and player level");
    assert(worldState.pendingEncounter.enemies.length && !worldState.pendingEncounter.objective && !Combat.active() && !$("map-encounter-modal").hidden, "A normal encounter opens the Fight/Run prompt instead of immediately starting combat");
    assert(worldState.minutes === time && worldState.encounterTravelMinutes === interval, "Forcing an encounter does not advance time or consume travel intervals");
    assert(!WorldMap.isTravelling() && JSON.stringify(worldState.destination) === destination, "The encounter pauses travel and preserves the route");
    assert(button.hidden && button.disabled, "The button is unavailable while an encounter is pending");
    click("map-force-encounter");
    assert(generated.length === 1 && JSON.stringify(worldState.pendingEncounter) === pending, "Repeated clicks cannot replace a pending encounter");
    let escapeBonus;
    Combat.attemptTravelEscape = async (enemies, bonus) => { escapeBonus = bonus; return { success: true, chance: 100 }; };
    click("map-encounter-run");
    for (let count = 0; count < 100 && worldState.pendingEncounter; count++) await wait(20);
    assert(escapeBonus === 25 && !worldState.pendingEncounter && !Combat.active() && !WorldMap.isTravelling(), "Run uses the normal pre-combat bonus and keeps travel paused");
    assert(!button.hidden && !button.disabled, "The button returns after escaping");

    click("map-force-encounter");
    const enemies = [...worldState.pendingEncounter.enemies];
    click("map-encounter-fight");
    for (let count = 0; count < 150 && (worldState.pendingEncounter || !Combat.active()); count++) await wait(20);
    assert(Combat.active() && !worldState.pendingEncounter, "Fight starts the forced encounter");
    assert(Combat.serialize().actors.filter(actor => actor.team === "enemy").map(actor => actor.name.replace(/ \d+$/, "")).join(",") === enemies.join(","), "Combat contains exactly the enemies promised by the prompt");
    click("combat-sheet-button"); click("map-open-button");
    const callsInCombat = generated.length;
    click("map-force-encounter");
    assert(button.hidden && button.disabled && generated.length === callsInCombat, "Active combat blocks new forced encounters");
    Combat.restore(null);

    move({ x: 350, y: 865 }, "high-stoneveil");
    level = 50; WorldMap.render();
    assert($("map-zone").textContent === "Lower Slopes" && !button.hidden, "Encounter button works in a different region");
    click("map-force-encounter");
    assert(generated.at(-1).regionId === "high-stoneveil" && generated.at(-1).level === 50 && worldState.pendingEncounter.enemies.length, "Regional forced encounters use the correct level-sensitive pool");

    move({ x: 1000, y: 500 });
    currentHp = 0; WorldMap.render();
    const blockedCalls = generated.length;
    click("map-force-encounter");
    assert(button.hidden && button.disabled && generated.length === blockedCalls, "A defeated character cannot force an encounter");
    currentHp = 1;
    worldState.pendingRegionTransition = { regionId: "alder-vale", exitId: WorldRules.getRegion("alder-vale").exits[0].id };
    WorldMap.render(); click("map-force-encounter");
    assert(button.hidden && button.disabled && generated.length === blockedCalls, "A pending region transition blocks encounters");
    worldState.pendingRegionTransition = null; WorldMap.render();
    $("map-modal").querySelector("[data-map-close]").click();
    click("map-force-encounter");
    assert(generated.length === blockedCalls, "A closed map cannot trigger a stale encounter button");
  } finally {
    EncounterRules.generate = originalGenerate;
    EncounterRules.roll = originalRoll;
    Combat.attemptTravelEscape = originalEscape;
    applyCharacter(normalizeCharacter(defaultCharacter()));
    WorldMap.setAmbientEnabled(false);
  }
  return { passed: true };
}

async function runForcedRematchChecks() {
  let checks = 0;
  const assert = (value, message) => { if (!value) throw new Error(message); checks++; };
  const $ = id => document.getElementById(id), clone = value => structuredClone(value);
  const click = id => $(id).dispatchEvent(new MouseEvent("click", { bubbles: true }));
  const until = async (condition, message) => {
    for (let count = 0; count < 250 && !condition(); count++) await new Promise(resolve => setTimeout(resolve, 20));
    assert(condition(), message);
  };
  const zik = { uniqueId: "zik-lun-armored-hulk", enemy: "Zik Lun, the armored Hulk", regionId: "stoneveil-foothills", zone: "Plains", point: { x: 400, y: 880 } };
  const nygheir = { uniqueId: "nygheir-the-wounded", enemy: "Nygheir the Wounded", regionId: "stoneveil-foothills", zone: "Mountainfoot", point: { x: 300, y: 170 } };
  const main = defaultCharacter();
  main.raceId = "human"; main.level = 100; main.editingEnabled = false;
  main.stats.vitality = 1000; main.stats.mind = 1000; main.stats.speed = 2000; main.stats.luck = 0;
  main.currentHp = 100000; main.currentMana = 100000;
  main.worldState.regionId = zik.regionId; main.worldState.position = clone(zik.point);
  main.worldState.guild.greatHuntsCompleted = [zik.uniqueId, nygheir.uniqueId];
  main.worldState.guild.greatHuntRematches = [clone(zik), clone(nygheir)];
  Combat.restore(null); applyCharacter(normalizeCharacter(main)); WorldMap.setAmbientEnabled(false);
  const originalGenerate = EncounterRules.generate, originalRoll = EncounterRules.roll, originalEscape = Combat.attemptTravelEscape;
  const originalAnimations = GameSettings.state().combatAnimations;
  GameSettings.setCombatAnimations(false);
  const generated = [], escapes = [];
  EncounterRules.generate = (zone, context) => { generated.push({ zone, ...context }); return ["Goblin"]; };
  EncounterRules.roll = () => { throw new Error("Forced rematches must not roll for an encounter"); };
  Combat.attemptTravelEscape = async (enemies, bonus, objective) => { escapes.push({ enemies: [...enemies], bonus, objective }); return { success: true, chance: 100 }; };
  const move = (position, regionId = zik.regionId) => {
    WorldMap.pause(false);
    worldState = WorldRules.normalize({ ...worldState, regionId, position, destination: null, route: [], pendingEncounter: null, pendingRegionTransition: null });
    WorldRules.setActiveRegion(regionId); Guild.onPositionChange(); WorldMap.resetView(); WorldMap.open();
  };
  const force = expected => {
    WorldMap.render();
    assert(!$("map-force-encounter").hidden && !$("map-force-encounter").disabled, "An active marker allows forcing its boss");
    const time = worldState.minutes, interval = worldState.encounterTravelMinutes, generatedBefore = generated.length;
    click("map-force-encounter");
    const pending = worldState.pendingEncounter;
    assert(pending?.objective === "great-hunt-rematch" && pending.enemies[0] === expected.enemy && pending.zone === expected.zone, "Force Encounter selects the nearby rematch boss, not regional enemies");
    assert(!Combat.active() && !$("map-encounter-modal").hidden && $("map-encounter-modal").textContent.includes(expected.enemy), "The boss is named in the standard Fight/Run prompt");
    assert(generated.length === generatedBefore && worldState.minutes === time && worldState.encounterTravelMinutes === interval, "Forcing a boss skips the random pool and does not advance time");
    return clone(pending);
  };
  const fight = async expected => {
    click("map-encounter-fight");
    await until(() => Combat.active() && !worldState.pendingEncounter, "Fight starts the selected boss rematch");
    const boss = Combat.serialize().actors.find(actor => actor.guildObjective);
    assert(boss?.guildRematchId === expected.uniqueId && !boss.empowered, "Combat retains the selected boss identity without ordinary-hunt empowerment");
    click("combat-sheet-button");
  };
  const win = async () => {
    const battle = Combat.serialize();
    battle.actors.filter(actor => actor.team === "enemy").forEach(actor => { actor.character.currentHp = 0; });
    Combat.restore(battle); Combat.onSheetChange();
    await until(() => !Combat.active(), "Defeating the roster ends the rematch");
    assert(Combat.serialize().result.startsWith("Victory"), "The rematch records a victory");
    Combat.restore(null); WorldMap.open();
  };
  const run = async () => {
    click("map-encounter-run");
    await until(() => !worldState.pendingEncounter, "Run dismisses the rematch prompt");
    assert(escapes.at(-1).bonus === 25 && escapes.at(-1).objective === "great-hunt-rematch", "Boss rematches retain the standard pre-combat escape bonus");
  };
  try {
    move(zik.point);
    const pending = force(zik);
    assert(pending.enemies.length === 3 && pending.enemies.slice(1).join() === "Troll Shieldbearer,Troll War-drummer", "The boss's complete entourage is included");
    click("map-force-encounter");
    assert(JSON.stringify(worldState.pendingEncounter) === JSON.stringify(pending), "Repeated clicks cannot replace a pending rematch");
    await fight(zik);
    await win();
    assert(worldState.guild.avoidedGreatHunts.includes(zik.uniqueId) && !Guild.objectiveAt(zik.point) && !WorldMap.checkGuildObjective(), "Victory suppresses automatic repeat fights while standing on the marker");
    assert(worldState.guild.greatHuntRematches.length === 2 && Guild.markers().some(marker => marker.label.includes(zik.enemy)), "Victory preserves both active markers");
    applyCharacter(normalizeCharacter(backupPayload().character)); WorldMap.open();
    assert(!Guild.objectiveAt(zik.point) && Guild.rematchAt(zik.point, true)?.uniqueId === zik.uniqueId, "A saved avoided marker remains available for a deliberate retry");

    // Overlapping ordinary objectives must neither replace nor receive credit
    // for an explicitly requested rematch, even after a previous victory.
    worldState.guild.quest = { type: "hunt", enemy: "Goblin", regionId: zik.regionId, zone: zik.zone, point: clone(zik.point), complete: false, progress: 0, reward: { silver: 0, reputation: 0, rateVersion: 2 } };
    worldState.guild.artifactChallenge = { enemy: "Goblin", regionId: zik.regionId, zone: zik.zone, point: clone(zik.point), artifactId: "artifact-hares-anklet-artifact" };
    assert(Guild.objectiveAt(zik.point)?.kind === "hunt", "Automatic objectives retain their existing priority");
    force(zik); await fight(zik); await win();
    assert(!worldState.guild.quest.complete && worldState.guild.artifactChallenge, "Rematch victory does not complete an overlapping hunt or award its artifact");
    worldState.guild.quest = null; worldState.guild.artifactChallenge = null;
    assert(!WorldMap.checkGuildObjective(), "Manual retries do not remove automatic-repeat protection");

    worldState.guild.greatHuntRematches.find(marker => marker.uniqueId === nygheir.uniqueId).point = clone(zik.point);
    assert(Guild.rematchAt(zik.point, true)?.uniqueId === nygheir.uniqueId && Guild.objectiveAt(zik.point)?.uniqueId === nygheir.uniqueId, "Overlapping rematches prefer the same non-avoided boss for manual and automatic entry");
    force(nygheir); await run();
    assert(escapes.at(-1).enemies[0] === nygheir.enemy && worldState.guild.avoidedGreatHunts.includes(nygheir.uniqueId) && worldState.guild.avoidedGreatHunts.includes(zik.uniqueId), "Run suppresses the selected overlapping boss without changing another marker");
    force(zik); await run();
    assert(!WorldMap.checkGuildObjective() && worldState.guild.greatHuntRematches.length === 2, "An explicit retry of an avoided boss keeps all markers and automatic safeguards");

    const road = WorldRules.getRegion(zik.regionId).roads.flat().find(point => WorldRules.zoneAt(point, zik.regionId) === "Road");
    assert(road, "The fixture finds a safe road point");
    worldState.guild.greatHuntRematches = [{ ...clone(zik), point: clone(road) }];
    move(road);
    assert($("map-zone").textContent === "Road", "The safe-zone fixture is on a road");
    force(zik); await run();
    worldState.guild.greatHuntRematches = []; worldState.guild.greatHuntRematch = null; worldState.guild.avoidedGreatHunts = [];
    WorldMap.render(); click("map-force-encounter");
    assert($("map-force-encounter").hidden && $("map-force-encounter").disabled && !worldState.pendingEncounter && !generated.length, "A road without an active rematch stays encounter-safe");

    worldState.guild.greatHuntRematches = [clone(zik)];
    move({ x: zik.point.x + 26, y: zik.point.y }); click("map-force-encounter");
    assert(!worldState.pendingEncounter.objective && worldState.pendingEncounter.enemies[0] === "Goblin" && generated.length === 1, "Outside the marker radius, Force Encounter still selects normal regional enemies");
    worldState.pendingEncounter = null;
    move({ x: 1000, y: 500 }, "alder-vale"); click("map-force-encounter");
    assert(!worldState.pendingEncounter.objective && generated.at(-1).regionId === "alder-vale" && generated.length === 2, "A marker in another region cannot hijack normal encounters");

    worldState.pendingEncounter = null; worldState.guild.greatHuntRematches = [];
    move(zik.point); click("map-force-encounter");
    assert(!worldState.pendingEncounter.objective && generated.length === 3, "Deactivated boss markers revert to normal regional encounters");
  } finally {
    Combat.restore(null); EncounterRules.generate = originalGenerate; EncounterRules.roll = originalRoll; Combat.attemptTravelEscape = originalEscape;
    GameSettings.setCombatAnimations(originalAnimations);
    applyCharacter(normalizeCharacter(defaultCharacter())); WorldMap.setAmbientEnabled(false);
  }
  return { passed: true, checks };
}

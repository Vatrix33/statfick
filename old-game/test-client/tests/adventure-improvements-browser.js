/* Run only in the isolated headless test profile. */
async function runMapImprovementChecks() {
  let checks = 0;
  const assert = (ok, message) => { if (!ok) throw Error(message); checks++; };
  const $ = id => document.getElementById(id), wait = ms => new Promise(resolve => setTimeout(resolve, ms));
  const key = (value, options = {}, target = document) => target.dispatchEvent(new KeyboardEvent("keydown", { key: value, bubbles: true, cancelable: true, ...options }));
  const main = defaultCharacter(); main.raceId = "human"; main.level = 30; main.editingEnabled = false;
  main.stats.vitality = 1000; main.stats.mind = 1000; main.stats.speed = 2000;
  applyCharacter(normalizeCharacter(main)); Combat.restore(null); WorldMap.setAmbientEnabled(false);
  const originalRoll = EncounterRules.roll, originalEscape = Combat.attemptTravelEscape;
  EncounterRules.roll = () => null;
  const move = (point, regionId = "alder-vale") => {
    WorldMap.pause(false);
    worldState = WorldRules.normalize({ ...worldState, regionId, position: point, destination: null, route: [], pendingEncounter: null, pendingRegionTransition: null });
    WorldRules.setActiveRegion(regionId); Guild.onPositionChange(); WorldMap.resetView();
  };
  try {
    key("m"); assert(!$("map-modal").hidden, "M opens the map from the character sheet");
    key("M", { repeat: true }); assert(!$("map-modal").hidden, "Holding M does not repeatedly toggle the map");
    key("m", { ctrlKey: true }); assert(!$("map-modal").hidden, "Modified shortcuts are not intercepted");
    key("m", {}, $("map-track-enemy")); assert(!$("map-modal").hidden, "Typing in a map selection cannot close it");

    move({ x: 1000, y: 500 });
    const point = { x: 445, y: 490 };
    worldState.guild.quest = { type: "search", regionId: "alder-vale", zone: "Town", point, progress: 0, complete: false, reward: { silver: 0, reputation: 0, rateVersion: 2 } };
    WorldMap.render();
    let marker = $("map-guild-markers").querySelector(".map-guild-marker");
    assert(marker, "A quest supplies an interactive map marker");
    for (let frame = 0; frame < 10; frame++) WorldMap.render();
    assert(marker === $("map-guild-markers").querySelector(".map-guild-marker"), "Clock and travel rendering preserve marker hit targets");
    // Port task 8: the markers sit in their region's group on the continent map.
    const centre = new DOMPoint(point.x, point.y).matrixTransform($("map-guild-markers").getScreenCTM()), hitRadius = marker.querySelector("circle").getBoundingClientRect().width / 2, screen = { x: centre.x + hitRadius * 0.9, y: centre.y };
    const hit = document.elementFromPoint(screen.x, screen.y);
    assert(hit?.closest(".map-guild-marker") === marker, "The full transparent marker hitbox receives actual pointer hits");
    hit.dispatchEvent(new PointerEvent("pointerdown", { bubbles: true, pointerType: "mouse", pointerId: 41, button: 0, buttons: 1, clientX: screen.x, clientY: screen.y }));
    await wait(200);
    assert(!$("regional-map").classList.contains("is-mouse-steering"), "A held marker press remains a marker click, not terrain steering");
    hit.dispatchEvent(new PointerEvent("pointerup", { bubbles: true, pointerType: "mouse", pointerId: 41, button: 0, clientX: screen.x, clientY: screen.y }));
    hit.dispatchEvent(new MouseEvent("click", { bubbles: true, detail: 1, clientX: screen.x, clientY: screen.y }));
    assert(WorldMap.isTravelling() && worldState.destination.x === WorldRules.toContinent("alder-vale", point).x && worldState.destination.y === WorldRules.toContinent("alder-vale", point).y, "Clicking a quest marker travels to its exact coordinates instead of a nearby town or terrain point");
    await wait(130); WorldMap.pause(false);
    assert(marker.isConnected, "Travel frames do not destroy the clicked marker");
    marker.dispatchEvent(new KeyboardEvent("keydown", { key: "Enter", bubbles: true, cancelable: true }));
    assert(WorldMap.isTravelling(), "Quest markers still support Enter activation"); WorldMap.pause(false);
    worldState.guild.quest = null;

    const bindings = [
      ["map-pause", " "], ["map-cancel", "x"], ["map-force-encounter", "e"], ["map-camp", "c"], ["map-rest", "r"],
      ["map-blacksmith", "b"], ["map-item-shop", "g"], ["map-spell-shop", "s"], ["map-guild", "a"], ["map-church", "t"],
      ["map-class-respec", "l"], ["map-stat-respec", "p"], ["map-boat", "v"], ["map-search", "f"], ["map-track-quarry", "q"],
      ["map-zoom-in", "+"], ["map-zoom-out", "-"], ["map-fit", "0"],
    ];
    for (const [id, shortcut] of bindings) {
      const button = $(id), hidden = button.hidden, disabled = button.disabled;
      assert(button.dataset.mapHotkey && button.getAttribute("aria-keyshortcuts") && button.title.includes("Shortcut:"), id + " exposes its shortcut visually and accessibly");
      button.hidden = false; button.disabled = false;
      let invoked = 0;
      const intercept = event => { event.stopImmediatePropagation(); invoked++; };
      button.addEventListener("click", intercept, true);
      key(shortcut); assert(invoked === 1, id + " is activated by its documented hotkey");
      button.disabled = true; key(shortcut); assert(invoked === 1, id + " cannot bypass its disabled state");
      button.disabled = false; button.hidden = true; key(shortcut); assert(invoked === 1, id + " cannot bypass its hidden state");
      button.removeEventListener("click", intercept, true); button.hidden = hidden; button.disabled = disabled;
    }
    WorldMap.render();
    assert([...$("map-modal").querySelectorAll("section button")].every(button => button.getAttribute("aria-keyshortcuts")), "Every map-mode button, including settlement links and close, has a shortcut");
    key("1"); assert(WorldMap.isTravelling() && worldState.destination.x === WorldRules.toContinent("alder-vale", { x: 420, y: 490 }).x, "Number keys route to their matching settlement");
    key(" "); assert(!WorldMap.isTravelling(), "Space pauses travel");
    key(" "); assert(WorldMap.isTravelling(), "Space resumes travel");
    key("x"); assert(!WorldMap.isTravelling() && !worldState.destination, "X clears the route");
    const width = $("regional-map").viewBox.baseVal.width;
    key("+"); assert($("regional-map").viewBox.baseVal.width < width, "Plus zooms in");
    key("0");
    const fitted = $("regional-map").viewBox.baseVal;
    assert(fitted.width >= 1599 && fitted.height >= 999, "Zero fits the whole region inside the current viewport");

    move({ x: 420, y: 490 });
    for (const [shortcut, shop] of [["b", "blacksmith"], ["g", "general store"], ["s", "spell shop"]]) {
      key(shortcut); assert(!$("shop-modal").hidden && $("shop-eyebrow").textContent.toLowerCase().includes(shop), shortcut + " opens the correct town shop");
      key("m"); assert(!$("map-modal").hidden && !$("shop-modal").hidden, "Map shortcuts do not interrupt another dialog");
      $("shop-modal").querySelector("[data-shop-close]").click();
    }
    key("a"); assert(!$("guild-modal").hidden, "A opens the Adventurers Guild"); $("guild-modal").querySelector("[data-guild-close]").click();
    const beforeRest = worldState.minutes; key("r"); assert(worldState.minutes === beforeRest + 600, "R rests at an available town");
    inventory.push("consumable-camping-supplies"); WorldMap.render();
    const beforeCamp = worldState.minutes; key("c"); assert(worldState.minutes === beforeCamp + 480 && !inventory.includes("consumable-camping-supplies"), "C camps and consumes supplies");

    move({ x: 1000, y: 500 }); key("e");
    assert(worldState.pendingEncounter && !$("map-encounter-modal").hidden, "E opens the existing encounter prompt");
    const pending = JSON.stringify(worldState.pendingEncounter);
    key("m"); assert($("map-modal").hidden && $("map-encounter-modal").hidden && JSON.stringify(worldState.pendingEncounter) === pending, "M can close the map without dismissing or rerolling a pending encounter");
    key("m"); assert(!$("map-encounter-modal").hidden, "Reopening with M restores the pending choice");
    let escapeBonus;
    Combat.attemptTravelEscape = async (enemies, bonus) => { escapeBonus = bonus; return { success: true, chance: 100 }; };
    key("b"); assert($("shop-modal").hidden, "An encounter blocks ordinary map shortcuts");
    key("r"); for (let attempt = 0; attempt < 100 && worldState.pendingEncounter; attempt++) await wait(20);
    assert(!worldState.pendingEncounter && escapeBonus === 25, "R runs from the encounter instead of resting and retains its bonus");
    key("e"); key("f");
    for (let attempt = 0; attempt < 200 && !Combat.active(); attempt++) await wait(20);
    assert(Combat.active(), "F starts the encounter fight"); key("m"); assert($("map-modal").hidden, "M does not open a map over combat"); Combat.restore(null); WorldMap.open();

    const exit = WorldRules.exitById("north-road", "alder-vale");
    move(exit.point); worldState.pendingRegionTransition = { regionId: "alder-vale", exitId: exit.id }; WorldMap.render();
    key("Backspace"); assert(!worldState.pendingRegionTransition && worldState.regionId === "alder-vale", "Backspace turns back at a border");
    worldState.position = { ...exit.point }; worldState.pendingRegionTransition = { regionId: "alder-vale", exitId: exit.id }; WorldMap.render();
    key("Enter"); assert(worldState.regionId === exit.targetRegionId, "Enter confirms travel into the next region");
    const port = WorldRules.getRegion("sunward-coast").settlements.find(place => place.boat);
    move(port, "sunward-coast"); key("v"); assert(worldState.regionId === port.boat.regionId, "V uses the available boat crossing");
    if (window.statFickDesktop?.captureTestScreen) await statFickDesktop.captureTestScreen("map-shortcuts", 1440, 1000);
    key("m"); assert($("map-modal").hidden, "M closes the map after region changes");
  } finally {
    EncounterRules.roll = originalRoll; Combat.attemptTravelEscape = originalEscape; Combat.restore(null);
    applyCharacter(normalizeCharacter(defaultCharacter())); WorldMap.setAmbientEnabled(false);
  }
  return { passed: true, checks };
}

async function runCompanionImportChecks() {
  let checks = 0;
  const assert = (ok, message) => { if (!ok) throw Error(message); checks++; };
  const $ = id => document.getElementById(id), wait = ms => new Promise(resolve => setTimeout(resolve, ms));
  const until = async (predicate, message) => { for (let attempt = 0; attempt < 200 && !predicate(); attempt++) await wait(20); assert(predicate(), message); };
  const main = defaultCharacter(); main.raceId = "human"; main.level = 40; main.editingEnabled = false;
  main.stats.vitality = 1000; main.stats.mind = 1000; main.stats.speed = 2000;
  applyCharacter(normalizeCharacter(main)); Combat.restore(null); WorldMap.setAmbientEnabled(false); Companions.open();
  // Companion spell training lazily opens the local stock. Seed that existing
  // cache first so the preservation check compares the same initialized state.
  Shop.stockFor(WorldRules.settlementAt(worldState.position, worldState.regionId));
  const mainSnapshot = () => { const c = structuredClone(backupPayload().character); delete c.companions; delete c.recruitOffers; delete c.combatState; return JSON.stringify(c); };
  const before = mainSnapshot();
  const file = async (text, name = "Elara.json") => {
    const data = new DataTransfer(); data.items.add(new File([typeof text === "string" ? text : JSON.stringify(text)], name, { type: "application/json" }));
    $("companion-import-file").files = data.files;
    $("companion-import-file").dispatchEvent(new Event("change", { bubbles: true })); await wait(50);
  };
  try {
    $("companion-import").click(); await wait(80);
    assert(!companions.length && $("companion-import-panel").hidden && !$("companion-import").disabled, "Cancelling native file selection leaves the roster untouched");
    for (const invalid of ["{", "[]", "{\"stats\":{}}", "{\"items\":[]}"]) {
      await file(invalid);
      assert(!companions.length && $("companion-import-panel").hidden && /not.*valid/i.test($("companion-status").textContent), "Malformed and non-character files are rejected without changing the party");
    }
    await file("x".repeat(10 * 1024 * 1024 + 1));
    assert(!companions.length && /10 MB/.test($("companion-status").textContent), "Browser file imports honor the native ten-megabyte limit");

    const saved = defaultCharacter(); saved.name = "Elara Ashfall"; saved.gender = "female"; saved.raceId = "elf"; saved.level = 18;
    saved.exp = 123; saved.perkPoints = 4; saved.availablePoints = 7;
    saved.stats.strength = 30; saved.stats.vitality = 60; saved.stats.mind = 60; saved.manualAllocations.strength = 3;
    saved.currentHp = 60; saved.currentMana = 30; saved.permanentBonuses.strength = 12;
    saved.classLevels = { warrior: 5 }; saved.classProgress.fulfilledClassOtherRequirements = ["warrior"];
    saved.activeSkills = { "warrior:battle-cry": { active: true, lastUsedDay: todayKey(), charges: 2, usedThisCombat: true, usesThisCombat: 1, remainingUses: 2 } };
    const sword = getItem("weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0"), dagger = getItem("weapon-forged-dagger-30-hfiro-elque-rtoak-s0"), armor = getItem("armor-forged-leather-armor-30-elraw-bbraw-hfraw-s0");
    assert(sword && dagger && armor, "The import fixture uses actual available equipment");
    saved.inventory = [sword.id, dagger.id, armor.id, sword.id]; saved.favoriteItems = [sword.id];
    saved.equipment.primaryHand = sword.id; saved.equipment.offHand = dagger.id; saved.equipment.armor = armor.id;
    saved.knownSpells = spells.filter(spell => spell.tier === 1).slice(0, 3).map(spell => spell.id);
    saved.companions = [{ id: "nested", name: "Do not import", character: defaultCharacter() }];
    saved.recruitOffers = structuredClone(saved.companions); saved.combatState = { version: 1, active: true, actors: [] };
    saved.worldState.regionId = "stoneveil-summit"; saved.worldState.pendingEncounter = { zone: "Summit", enemies: ["Anatemis, the White Star"] };
    saved.worldState.guild.reputation = 999; saved.shopState = { version: 1, refreshWindow: 8, locations: { wrong: { items: [], spells: [] } } };
    const payload = { format: "emberfall-stat-block", version: 22, character: saved };
    assert(await statFickDesktop.saveCharacter(JSON.stringify(payload)), "A private native test save is available for import");
    $("companion-import").click(); await until(() => !$("companion-import-panel").hidden, "Native file selection opens an import preview, not the main character loader");
    assert(!companions.length && mainSnapshot() === before && $("companion-import-name").value === saved.name && $("companion-import-summary").textContent.includes("Level 18"), "Preview preserves the current character and offers the saved character's name and build summary");
    $("companion-import-name").value = "Edited preview name"; Companions.render();
    assert($("companion-import-name").value === "Edited preview name", "Routine companion rendering does not reset an edited import name");
    $("companion-import-cancel").click(); assert(!companions.length && $("companion-import-panel").hidden, "Cancelling the preview adds nothing");

    await file(payload); assert(!$("companion-import-panel").hidden, "The browser file-input path supports wrapped character backups");
    $("companion-import-name").value = "   "; $("companion-import-panel").dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
    assert(!companions.length && $("companion-status").textContent.includes("Enter a name"), "A blank companion name cannot be committed");
    $("companion-import-name").value = "Elara";
    if (window.statFickDesktop?.captureTestScreen) {
      await statFickDesktop.captureTestScreen("companion-import-preview-mobile", 390, 844);
      await statFickDesktop.captureTestScreen("companion-import-preview", 1440, 1000);
    }
    $("companion-import-confirm").click();
    assert(companions.length === 1 && companions[0].name === "Elara" && companions[0].gender === "female", "Confirm adds a renamed companion with its own identity");
    const imported = companions[0], c = imported.character;
    assert(c.level === saved.level && c.exp === saved.exp && c.perkPoints === saved.perkPoints && c.availablePoints === saved.availablePoints && c.raceId === saved.raceId, "Level, ancestry, EXP and unspent points survive import");
    assert(JSON.stringify(c.stats) === JSON.stringify(saved.stats) && c.manualAllocations.strength === 3 && c.permanentBonuses.strength === 12, "Base stats, allocated points, and bonuses survive import");
    assert(c.currentHp === 60 && c.currentMana === 30 && c.classLevels.warrior === 5 && c.classProgress.fulfilledClassOtherRequirements.includes("warrior"), "Resources, classes, and personal requirement progress survive import");
    assert(JSON.stringify(c.inventory) === JSON.stringify(saved.inventory) && JSON.stringify(c.equipment) === JSON.stringify(saved.equipment) && JSON.stringify(c.knownSpells) === JSON.stringify(saved.knownSpells) && c.favoriteItems.includes(sword.id), "Equipment, quantities, favorite items and known spells survive import");
    assert(!c.companions.length && !c.recruitOffers.length && !c.combatState && !c.worldState.pendingEncounter && c.worldState.guild.reputation === 0 && !c.editingEnabled && !c.activeSkills["warrior:battle-cry"].active, "Imports cannot bring nested parties, map quests, active combat, Dev Mode or active combat buffs");
    assert(c.activeSkills["warrior:battle-cry"].charges === 2, "Persistent skill charges remain intact");
    assert(c.activeSkills["warrior:battle-cry"].lastUsedDay === saved.activeSkills["warrior:battle-cry"].lastUsedDay && !c.activeSkills["warrior:battle-cry"].usedThisCombat && !c.activeSkills["warrior:battle-cry"].usesThisCombat && c.activeSkills["warrior:battle-cry"].remainingUses == null, "Daily cooldowns persist while combat-only usage and charges are cleared");
    const beforeFields = JSON.parse(before), afterFields = JSON.parse(mainSnapshot());
    const changedFields = Object.keys(beforeFields).filter(field => JSON.stringify(beforeFields[field]) !== JSON.stringify(afterFields[field]));
    assert(!changedFields.length, "Import does not replace the main character or its purse; changed fields: " + changedFields.join(", "));
    const equipped = document.querySelector(`[data-companion-equipment="primaryHand"]`);
    assert(equipped?.textContent.includes(sword.name) && $("companion-sheet").textContent.includes("Warrior"), "Imported gear and class features are usable in the companion sheet");
    const normalized = normalizeCharacter(backupPayload().character);
    assert(normalized.companions[0].id === imported.id && normalized.companions[0].character.currentHp === 60, "Imported companion identity and resources persist in character saves");

    await file(payload); $("companion-import-name").value = "Elara II"; $("companion-import-confirm").click();
    assert(companions.length === 2 && companions[1].id !== imported.id && companions[1].character !== c, "Repeated imports create independent companion records and IDs");
    $("companions-modal").querySelector("[data-companions-close]").click();
    assert(await Combat.startEncounter(["Goblin"]), "Combat starts with imported companions");
    let battle = Combat.serialize();
    assert(battle.actors.filter(actor => actor.companion).length === 2 && battle.actors.some(actor => actor.id === imported.id), "Imported companions join the party's combat roster");
    $("combat-sheet-button").click(); Companions.open(imported.id);
    assert($("companion-import").disabled, "Active combat blocks importing new companions");
    await file(payload); assert(companions.length === 2 && $("companion-import-panel").hidden, "A stale file-input event cannot bypass the combat lock");
    $("companions-modal").querySelector("[data-companions-close]").click();
    battle.current = imported.id; battle.ap = 5; Combat.restore(battle); $("combat-view-toggle").click();
    document.querySelector('#combat-actions [data-action="guard"]').click();
    assert(Combat.serialize().ap === 3 && Combat.serialize().actors.find(actor => actor.id === imported.id).effects.some(effect => effect.name === "Guard"), "Imported companion turns remain player-controlled");
    Combat.restore(null); Companions.open();

    const legacy = { level: 6, startingPerkPointGranted: true, perkPoints: 3 };
    for (const stat of statDefinitions) legacy[stat.key] = 20;
    await file(legacy, "Legacy Hero.json"); $("companion-import-confirm").click();
    assert(companions.length === 3 && companions[2].character.stats.strength === 20 && companions[2].name === "Legacy Hero", "Legacy flat-stat saves can be imported and use the filename as a suggested name");
    assert($("companion-import").disabled, "The import control respects the three-companion cap");
    await file(payload); assert(companions.length === 3 && $("companion-import-panel").hidden, "A stale file selection cannot bypass a full roster");
    applyCharacter(normalizeCharacter(backupPayload().character));
    assert(companions.length === 3 && companions[0].id === imported.id && companions[0].character.equipment.primaryHand === sword.id, "A complete save/load round trip retains all imported companions");
  } finally {
    Combat.restore(null); applyCharacter(normalizeCharacter(defaultCharacter())); WorldMap.setAmbientEnabled(false);
  }
  return { passed: true, checks };
}

async function runGreatHuntRewardChecks() {
  let checks = 0;
  const assert = (ok, message) => { if (!ok) throw Error(message); checks++; }, $ = id => document.getElementById(id);
  const main = defaultCharacter(); main.raceId = "human"; main.level = 23; main.editingEnabled = false;
  const town = WorldRules.getRegion("stoneveil-foothills").settlements.find(place => place.kind === "Castle city");
  main.worldState.regionId = "stoneveil-foothills"; main.worldState.position = { x: town.x, y: town.y };
  applyCharacter(normalizeCharacter(main)); Combat.restore(null); WorldMap.setAmbientEnabled(false); WorldMap.open();
  try {
    $("map-guild").click(); worldState.guild.boards = {};
    const index = Guild.boardFor().offers.findIndex(offer => offer.type === "great-hunt"), offer = Guild.boardFor().offers[index];
    assert(offer && offer.offeredAtLevel === 23, "The local Guild offers the level-23 Great Hunt");
    const oldReputation = Math.round((8 + 23 * 1.5) * 2.4 / 2);
    assert(offer.reward.reputation === oldReputation * 3 && offer.reward.rateVersion === 3, "New Great Hunt Reputation is exactly 200 percent higher");
    assert(offer.reward.silver === Math.round((100 + 23 * 35) * 2.4), "Great Hunt money rewards are unchanged");
    // The Guild Rework (port task 2, G18): regular quests pay their type's place in the reward ranking.
    const table = GuildQuestRules.rewardTable(GuildQuestRules.questLevel(level, worldState.regionId), calculateClassEffects());
    for (const regular of Guild.boardFor().offers.filter(entry => entry.type !== "great-hunt")) {
      assert(regular.reward.reputation === table[regular.type].reputation, "Regular quests pay their type's Reputation");
    }
    Guild.accept(index);
    assert(worldState.guild.greatHunt.reward.reputation === offer.reward.reputation && document.querySelector(".map-guild-marker"), "Accepting the enhanced Great Hunt retains its reward and map marker");
    worldState.guild.greatHunt.complete = true; Guild.render();
    const beforeMoney = ShopRules.walletValue(currencies), beforeReputation = worldState.guild.reputation;
    Guild.useCounter();
    assert(!worldState.guild.greatHunt && worldState.guild.reputation === beforeReputation + oldReputation * 3 && ShopRules.walletValue(currencies) === beforeMoney + offer.reward.silver, "Completing the quest pays the displayed triple Reputation and original money exactly once");

    const legacy = backupPayload().character;
    legacy.worldState.guild.quest = { ...structuredClone(offer), complete: true, reward: { silver: offer.reward.silver, reputation: oldReputation, rateVersion: 2 } };
    applyCharacter(normalizeCharacter(legacy));
    assert(worldState.guild.greatHunt.reward.reputation === oldReputation * 3 && worldState.guild.greatHunt.complete, "Already accepted or completed-but-unclaimed Great Hunts receive the new reward on load");
    const roundTrip = normalizeCharacter(backupPayload().character);
    assert(roundTrip.worldState.guild.greatHunt.reward.reputation === oldReputation * 3, "Repeated character normalization never triples the reward again");
  } finally { applyCharacter(normalizeCharacter(defaultCharacter())); WorldMap.setAmbientEnabled(false); }
  return { passed: true, checks };
}

async function runCombatRewardChecks() {
  let checks = 0;
  const assert = (ok, message) => { if (!ok) throw Error(message); checks++; }, $ = id => document.getElementById(id);
  const until = async (predicate, message) => { for (let attempt = 0; attempt < 200 && !predicate(); attempt++) await new Promise(resolve => setTimeout(resolve, 20)); assert(predicate(), message); };
  // Level 99: below the level cap, where EXP is still paid (port task 7, L18, L33).
  const main = defaultCharacter(); main.raceId = "human"; main.level = 99; main.editingEnabled = false;
  main.stats.vitality = 1000; main.stats.mind = 1000; main.stats.speed = 2000; main.stats.luck = 0;
  applyCharacter(normalizeCharacter(main)); Combat.restore(null); WorldMap.setAmbientEnabled(false);
  const startingExp = exp, startingMoney = ShopRules.walletValue(currencies), originalRoll = CombatRules.roll;
  try {
    assert(await Combat.startEncounter(["Goblin", "Goblin"]), "A reward fixture starts normally"); $("combat-sheet-button").click();
    let battle = Combat.serialize();
    battle.actors.filter(actor => actor.team === "enemy").forEach(actor => { actor.character.currentHp = 0; });
    Combat.restore(battle); Combat.onSheetChange();
    await until(() => !$("combat-loot-modal").hidden, "Victory opens the enhanced rewards screen"); battle = Combat.serialize();
    assert(battle.experienceEarned === 2 * CombatRules.enemyExperience(1) && battle.moneyEarned === 6, "The battle totals match both defeated enemies");
    assert($("combat-loot-exp").textContent === `${formatValue(battle.experienceEarned)} EXP` && $("combat-loot-money").textContent === ShopRules.formatPrice(battle.moneyEarned), "The rewards screen visibly shows the exact EXP and money totals");
    assert(exp === startingExp + battle.experienceEarned && ShopRules.walletValue(currencies) === startingMoney + battle.moneyEarned, "The displayed rewards were already paid once");
    if (window.statFickDesktop?.captureTestScreen) await statFickDesktop.captureTestScreen("combat-rewards", 1440, 1000);
    const saved = normalizeCharacter(backupPayload().character), awardedExp = exp, awardedMoney = ShopRules.walletValue(currencies);
    applyCharacter(saved); await until(() => !$("combat-loot-modal").hidden, "Pending rewards survive save/load");
    assert(exp === awardedExp && ShopRules.walletValue(currencies) === awardedMoney && $("combat-loot-exp").textContent.includes(String(battle.experienceEarned)), "Restoring the rewards screen never awards EXP or money twice");
    $("combat-loot-discard").click();
    assert(exp === awardedExp && ShopRules.walletValue(currencies) === awardedMoney && !inventory.length, "Discarding items does not discard the battle's EXP or money");

    stats.speed = 0; stats.luck = 0; updateDerivedValues(); Combat.restore(null);
    const chances = []; CombatRules.roll = chance => { chances.push(chance); return false; };
    const travelEscape = await Combat.attemptTravelEscape(["Anatemis, the White Star"], 25);
    assert(travelEscape.fastestEnemySpeed > 125 && travelEscape.chance === 30 && !travelEscape.success, "The pre-combat Run bonus applies above the five-percent escape floor");
    assert(await Combat.startEncounter(["Goblin"]), "An in-combat escape fixture starts"); $("combat-sheet-button").click();
    battle = Combat.serialize(); battle.current = "player"; battle.ap = 5;
    const enemy = battle.actors.find(actor => actor.team === "enemy"); enemy.character.stats.speed = 2000;
    Combat.restore(battle); $("combat-view-toggle").click();
    document.querySelector('#combat-actions [data-action="run"]').click();
    assert(chances.at(-1) === 5 && Combat.serialize().ap === 4 && Combat.serialize().log.some(line => line.includes("fails to escape (5% chance)")), "Run in combat spends its AP and rolls at least five percent against an overwhelmingly fast enemy");
    $("combat-sheet-button").click(); battle = Combat.serialize();
    battle.actors.filter(actor => actor.team === "enemy").forEach(actor => { actor.character.currentHp = 0; actor.character.inventory = []; actor.dropTable = []; });
    Combat.restore(battle); Combat.onSheetChange(); await until(() => !$("combat-loot-modal").hidden, "A fight with no item drops still shows its rewards");
    assert(!Combat.serialize().lootItems.length && $("combat-loot-list").textContent.includes("no usable items") && $("combat-loot-exp").textContent.includes("EXP"), "EXP and money remain visible even without item drops");
  } finally { CombatRules.roll = originalRoll; Combat.restore(null); applyCharacter(normalizeCharacter(defaultCharacter())); WorldMap.setAmbientEnabled(false); }
  return { passed: true, checks };
}

async function runAdventureImprovementsChecks() {
  return { passed: true, map: await runMapImprovementChecks(), imports: await runCompanionImportChecks(), guild: await runGreatHuntRewardChecks(), combat: await runCombatRewardChecks() };
}

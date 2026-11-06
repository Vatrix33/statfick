async function runGuildInformationChecks() {
  let checks = 0;
  const assert = (condition, message) => { if (!condition) throw Error(message); checks++; };
  const $ = id => document.getElementById(id), panel = () => $("guild-panel");
  const tick = () => new Promise(resolve => setTimeout(resolve, 0));
  const waitFor = async condition => { for (let attempt = 0; attempt < 200 && !condition(); attempt++) await tick(); assert(condition(), "Information view finished loading"); };
  const click = selector => { const button = panel().querySelector(selector); assert(button && !button.disabled, "Available information control: " + selector); button.click(); };
  const menu = () => click('[data-guild-information-page="menu"]');
  // The Guild Rework (port task 2): the hooded man in the guild hall opens the information menu.
  const hooded = () => document.querySelector('#guild-banner [data-guild-spot="hooded"]');
  const askHoodedMan = () => hooded()?.dispatchEvent(new MouseEvent("click", { bubbles: true }));
  const openInfo = async () => { askHoodedMan(); await waitFor(() => panel().querySelector('[data-guild-information-kind="class"]')); };
  const openGuild = () => { WorldMap.render(); $("map-guild").click(); };
  Combat.restore(null); WorldMap.setAmbientEnabled(false);
  const main = defaultCharacter(); main.raceId = "human"; main.stats.luck = 100; main.perkPoints = 0; main.worldState.guild.reputation = 500;
  applyCharacter(normalizeCharacter(main));
  $("map-open-button").click(); openGuild();
  assert(hooded() && hooded().getAttribute("aria-disabled") === "false", "Information is available from the hooded man at a full Guild");
  await openInfo();
  assert(worldState.guild.reputation === 500, "Opening the service does not spend Reputation");
  worldState.guild.information.offers.class = ["warrior", "mage", "bard"];
  click('[data-guild-information-kind="class"]');
  const classesOffered = [...worldState.guild.information.offers.class];
  assert(classesOffered.length === 3 && new Set(classesOffered).size === 3, "Exactly three distinct random class choices are offered");
  const eligible = GuildInformationRules.playerClasses(classCatalog, raceCatalog);
  assert(classesOffered.every(id => eligible.some(definition => definition.id === id)) && !classesOffered.some(id => ["raid-boss", "monster", "beast", "great-beast"].includes(id)), "Only player-obtainable classes appear");
  assert(perkPoints === 0 && classesOffered.every(id => !classRequirementsMet(getClass(id))) && !classesOffered.some(id => ["warrior", "mage", "bard"].includes(id)), "Fulfilled classes, including a stat-based requirement, are excluded even with zero perk points; stale offers are replaced");
  assert(panel().querySelectorAll("[data-guild-information-select]").length === 3 && !panel().querySelector(".guild-intel-details"), "Class requirements remain hidden until one class is chosen");
  menu(); click('[data-guild-information-kind="class"]');
  assert(JSON.stringify(classesOffered) === JSON.stringify(worldState.guild.information.offers.class), "Returning to the menu does not freely reroll pending choices");
  const savedPending = normalizeCharacter(backupPayload().character);
  applyCharacter(savedPending); openGuild(); await openInfo(); click('[data-guild-information-kind="class"]');
  assert(JSON.stringify(classesOffered) === JSON.stringify(worldState.guild.information.offers.class), "Pending choices survive saving and character restoration");
  menu(); worldState.guild.information.offers.class = ["champion", "oathsworn", "scholar"];
  click('[data-guild-information-kind="class"]');
  const before = { inventory: [...inventory], classes: { ...classLevels }, points: perkPoints };
  worldState.guild.quest = { version: 2, type: "cull", enemy: "Goblin", zone: "Plains", required: 5, radius: 40, point: { x: 1000, y: 500 }, regionId: "alder-vale", originRegionId: "alder-vale", originCastleId: "alderkeep", progress: 0, complete: false, reward: { silver: 20, reputation: 5, rateVersion: 3 } };
  click('[data-guild-information-select="champion"]');
  assert(worldState.guild.reputation === 435 && worldState.guild.information.revealed.class.includes("champion") && !worldState.guild.information.offers.class.length, "Selecting one class charges exactly 65 Reputation and records only that class");
  assert(panel().textContent.includes("40") && panel().textContent.includes("Slayer Level 5") && panel().textContent.includes("Gladiator Level 5"), "A future class reveals its level and alternative prerequisite requirements");
  assert(!worldState.guild.information.revealed.class.includes("scholar") && !classLevels.champion && perkPoints === before.points && JSON.stringify(inventory) === JSON.stringify(before.inventory), "Buying intel does not grant a class or item, spend a perk point, or reveal unselected entries");
  assert(worldState.guild.quest?.enemy === "Goblin", "Information purchases preserve the active quest");
  Guild.render(); assert(panel().textContent.includes("REVEALED CLASS REQUIREMENTS"), "Refreshing the Guild does not collapse the information details");
  menu(); click('[data-guild-information-kind="item"]');
  const itemOffers = [...worldState.guild.information.offers.item];
  const sources = await Combat.enemyDropSources();
  const pool = GuildInformationRules.itemPool(items, sources, classCatalog);
  assert(itemOffers.length === 5 && new Set(itemOffers).size === 5 && itemOffers.every(id => pool.some(entry => entry.item.id === id)), "Exactly five distinct real unique/class-unlock drops are offered");
  assert(pool.some(entry => entry.item.id === "key-item-blood-crystal" && entry.sources.length > 5), "Class-unlock intel includes Vampire drops automatically added by the combat loader");
  assert(!panel().textContent.includes("Base drop chance"), "Item drop sources remain hidden before selection");
  menu();
  const chooseItem = id => {
    worldState.guild.information.offers.item = [id, ...pool.map(entry => entry.item.id).filter(candidate => candidate !== id && !worldState.guild.information.revealed.item.includes(candidate)).slice(0, 4)];
    click('[data-guild-information-kind="item"]'); click(`[data-guild-information-select="${id}"]`);
  };
  chooseItem("masterwork-royal-trident");
  const tridentSource = pool.find(entry => entry.item.id === "masterwork-royal-trident").sources.find(source => source.enemyName === "Deep Sea King");
  const tridentChance = GuildInformationRules.dropChance(getItem("masterwork-royal-trident"), tridentSource, currentStats().luck, calculateClassEffects().uniqueDropChanceFlat);
  assert(worldState.guild.reputation === 370 && panel().textContent.includes("Deep Sea King") && panel().textContent.includes("Base drop chance: 12%") && panel().textContent.includes(`bonuses: ${formatValue(tridentChance)}%`), "Unique-item intel reveals the actual enemy, base chance and current Luck-adjusted chance: " + panel().textContent);
  const trident = getItem("masterwork-royal-trident");
  assert(panel().textContent.includes(itemDescription(trident)) && panel().textContent.includes(artifactEffectLabel(trident.effects[0])) && panel().textContent.includes("Primary scaling"), "Item information shows the full description, actual effects and weapon stats");
  assert(panel().textContent.includes(WorldRules.getRegion("sunward-coast").name) && panel().textContent.includes("Beach") && panel().textContent.includes("Great Hunt"), "Boss item information shows the region, zone and Great Hunt access");
  menu(); chooseItem("key-item-phylactery-core");
  assert(worldState.guild.reputation === 305 && panel().textContent.includes("Ancient Lich Ool") && panel().textContent.includes("20%") && panel().textContent.includes("Class-unlock item"), "Lich acquisition item can be revealed with its correct source and chance");
  assert(panel().textContent.includes("Class unlock") && panel().textContent.includes("Lich") && panel().textContent.includes("Mass Graves") && panel().textContent.includes(WorldRules.getRegion("ancient-battlefield").name) && panel().textContent.includes("Accept the quest at the Guild in " + WorldRules.getRegion("duskpine-forest").name), "Class key information explains its use and distinguishes the boss location from the quest Guild");
  menu(); chooseItem("masterwork-quasar-jet");
  assert(panel().textContent.includes("Anatemis, the White Star") && panel().textContent.includes("10%"), "Intel names the boss and the drop chance of her treasure");
  assert(panel().textContent.includes("Frozen Crown") && panel().textContent.includes(WorldRules.getRegion("stoneveil-summit").name) && panel().textContent.includes("Accept the quest at the Guild in " + WorldRules.getRegion("high-stoneveil").name), "Anatemis's battle location is the summit, not the region where his quest is accepted");
  const records = structuredClone(worldState.guild.information.revealed);
  const saved = normalizeCharacter(backupPayload().character);
  assert(JSON.stringify(saved.worldState.guild.information.revealed) === JSON.stringify(records), "All purchased intel survives character export normalization");
  applyCharacter(saved); worldState.guild.reputation = 0; openGuild();
  assert(hooded().getAttribute("aria-disabled") === "false", "Known intel remains accessible with zero Reputation");
  await openInfo();
  assert(panel().querySelector('[data-guild-information-kind="class"]').disabled && panel().querySelector('[data-guild-information-kind="item"]').disabled, "Insufficient Reputation blocks new purchases without blocking free reviews");
  click('[data-guild-information-page="archive"]');
  assert(panel().querySelectorAll("[data-guild-information-known]").length === 4, "Saved class and item intel can be reviewed together");
  click('[data-guild-information-known="masterwork-royal-trident"]');
  assert(worldState.guild.reputation === 0 && panel().textContent.includes("Deep Sea King") && panel().textContent.includes(artifactEffectLabel(trident.effects[0])) && panel().textContent.includes("Beach"), "Reviewing purchased intel costs nothing and also shows the new effects and locations");
  menu(); click('[data-guild-information-page="guild"]');
  assert($("guild-dock").hidden && !$("guild-modal").hidden && worldState.guild.quest?.enemy === "Goblin", "Returning to the Guild closes the hooded man's panel and keeps the active quest");
  $("guild-modal").querySelector("[data-guild-close]").click();
  worldState.regionId = "cinder-isle"; WorldRules.setActiveRegion(worldState.regionId);
  const town = WorldRules.settlementsForRegion(worldState.regionId)[0]; worldState.position = { x: town.x, y: town.y }; worldState.guild.reputation = 500;
  openGuild(); assert(!hooded(), "Town guilds have no hooded man and no Information service");
  Guild.activate("hooded"); assert(!panel().querySelector('[data-guild-information-kind="class"]') && worldState.guild.reputation === 500, "A missing service cannot be invoked programmatically to spend Reputation");
  $("guild-modal").querySelector("[data-guild-close]").click();
  worldState.regionId = "alder-vale"; WorldRules.setActiveRegion(worldState.regionId); worldState.position = { x: 420, y: 490 }; openGuild();
  const original = Combat.enemyDropSources;
  try {
    Combat.enemyDropSources = async () => { throw Error("Unavailable test source"); };
    await openInfo();
    assert(panel().querySelector('[data-guild-information-kind="item"]').disabled && !panel().querySelector('[data-guild-information-kind="class"]').disabled, "Source-load failures leave class intel usable without charging for unavailable item intel");
    click('[data-guild-information-page="guild"]');
    let finish; Combat.enemyDropSources = () => new Promise(resolve => { finish = resolve; });
    askHoodedMan();
    $("guild-modal").querySelector("[data-guild-close]").click();
    applyCharacter(normalizeCharacter(defaultCharacter())); finish(sources); await tick();
    assert($("guild-modal").hidden && worldState.guild.reputation === 0 && !worldState.guild.information.revealed.item.length, "An old asynchronous request cannot affect a newly loaded character");
  } finally { Combat.enemyDropSources = original; }
  const details = await runGuildInformationDetailChecks();
  return { passed: true, checks, details };
}

async function runGuildInformationDetailChecks() {
  const assert = (condition, message) => { if (!condition) throw Error(message); };
  const $ = id => document.getElementById(id), panel = () => $("guild-panel");
  const tick = () => new Promise(resolve => setTimeout(resolve, 0));
  Combat.restore(null); WorldMap.setAmbientEnabled(false);
  const c = defaultCharacter(); c.raceId = "human"; c.worldState.guild.reputation = 500;
  const known = ["masterwork-transformer", "artifact-second-wind-artifact", "key-item-blood-crystal"];
  c.worldState.guild.information = { offers: { class: [], item: [] }, revealed: { class: [], item: known } };
  applyCharacter(normalizeCharacter(c)); $("map-open-button").click(); WorldMap.render(); $("map-guild").click();
  const originalSources = Combat.enemyDropSources;
  async function archive() {
    document.querySelector('#guild-banner [data-guild-spot="hooded"]').dispatchEvent(new MouseEvent("click", { bubbles: true }));
    for (let i = 0; i < 200 && !panel().querySelector('[data-guild-information-page="archive"]'); i++) await tick();
    const button = panel().querySelector('[data-guild-information-page="archive"]'); assert(button, "Archive loads"); button.click();
  }
  function choose(id) {
    const button = panel().querySelector(`[data-guild-information-known="${id}"]`); assert(button, "Saved item info is available: " + id); button.click();
    const item = getItem(id);
    assert(panel().textContent.includes(itemDescription(item)), "Full item description appears: " + id);
    for (const effect of item.effects || []) assert(panel().textContent.includes(artifactEffectLabel(effect)), "Actual effect appears: " + id);
    assert(worldState.guild.reputation === 500, "All archive reviews are free");
  }
  try {
    await archive(); choose(known[0]);
    assert(panel().textContent.includes("Tankina, Disciple of Talos") && panel().textContent.includes("Church of Talos") && panel().textContent.includes(WorldRules.getRegion("stormwake-hills").name) && panel().textContent.includes("Site challenge"), "Fixed-site boss location is shown without inventing a Great Hunt");
    assert(panel().textContent.includes("Scaling") && panel().textContent.includes("Armor"), "Armor/shield information includes its stats");
    await statFickDesktop.captureTestScreen('guild-item-armor-information', 1440, 1000);
    panel().querySelector('[data-guild-information-page="menu"]').click(); await archive(); choose(known[1]);
    assert(panel().textContent.includes("Nygheir the Wounded") && panel().textContent.includes("Mountainfoot") && panel().textContent.includes(WorldRules.getRegion("stoneveil-foothills").name), "Artifact details show the boss's encounter area");
    await statFickDesktop.captureTestScreen('guild-item-artifact-information', 1440, 1000);
    panel().querySelector('[data-guild-information-page="menu"]').click(); await archive(); choose(known[2]);
    assert(panel().textContent.includes("Vampire Stalker") && panel().textContent.includes(WorldRules.getRegion("crimson-divide").name) && panel().textContent.includes("Regional encounter"), "Class-key information covers ordinary regional enemies as well as bosses");
    panel().querySelector('[data-guild-information-page="menu"]').click();
    Combat.enemyDropSources = async () => [];
    await archive(); choose(known[1]);
    assert(panel().textContent.includes("No current enemy drop source") && panel().textContent.includes("WHAT IT DOES"), "A retired enemy source does not hide purchased item effects");
  } finally { Combat.enemyDropSources = originalSources; $("guild-modal").querySelector("[data-guild-close]").click(); }
  return { passed: true, itemTypes: ["armor", "artifact", "class-key"], sourceTypes: ["site", "great-hunt", "regional", "missing-source"] };
}

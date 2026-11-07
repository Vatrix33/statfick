/* Port task 2, the Guild Rework (2026-11-06): one entry per rule. The design answers G1–G34 are in
   engine/Docs/Engine-Plan.md, "Decisions for task 2, Guild Rework"; old-game/Docs/Notes.md, "Guild Rework",
   says where each rule lives. runGuildReworkChecks runs them all. */
const guildRework = {
  $: id => document.getElementById(id),
  assert(condition, label) { if (!condition) throw new Error(label); },
  wait: ms => new Promise(resolve => setTimeout(resolve, ms)),
  setup({ level: playerLevel = 12, regionId = "alder-vale", settlementId = "alderkeep", reputation = 500, minutes = 480 } = {}) {
    Combat.restore(null);
    const character = defaultCharacter();
    character.raceId = "human"; character.level = playerLevel; character.worldState.guild.reputation = reputation; character.worldState.minutes = minutes;
    applyCharacter(normalizeCharacter(character));
    WorldMap.setAmbientEnabled(false);
    return guildRework.goTo(regionId, settlementId);
  },
  goTo(regionId, settlementId) {
    worldState.regionId = regionId; WorldRules.setActiveRegion(regionId);
    const place = WorldRules.settlementsForRegion(regionId).find(entry => entry.id === settlementId);
    worldState.position = { x: place.x, y: place.y }; worldState.destination = null; worldState.route = [];
    Guild.onPositionChange();
    return place;
  },
  open() {
    if (guildRework.$("map-modal")?.hidden !== false) WorldMap.open();
    WorldMap.render();
    if (guildRework.$("guild-modal").hidden) guildRework.$("map-guild").click();
  },
  close() { if (!guildRework.$("guild-modal").hidden) guildRework.$("guild-modal").querySelector("[data-guild-close]").click(); },
  spot(kind) { document.querySelector(`#guild-banner [data-guild-spot="${kind}"]`).dispatchEvent(new MouseEvent("click", { bubbles: true })); },
  // Pins an offer of one type on today's board and accepts it through the notice and Accept button.
  take(type) {
    const offer = Guild.makeOffer(type);
    guildRework.assert(offer, `A ${type} offer can be made here`);
    // A board never holds two offers of one type: the new one takes that type's place.
    const board = Guild.boardFor(); board.offers = board.offers.filter(entry => entry.type !== type); board.offers.push(offer);
    guildRework.spot("board");
    document.querySelector(`[data-guild-notice="${board.offers.length - 1}"]`).click();
    document.querySelector("#guild-accept").click();
    return worldState.guild.quest;
  },
  confirm(answer, action) { const original = window.confirm; window.confirm = () => answer; try { return action(); } finally { window.confirm = original; } },
};

/* G7, G8: a board per guild and day: castle 4 offers, town 3, each a different type, Bounty Broker one
   more; kept all day (also when the level changes); redrawn on a new day; accepted quests untouched. */
async function runGuildBoardChecks() {
  const { assert, setup, open, close, goTo } = guildRework;
  setup({ level: 12 });
  open();
  const board = Guild.boardFor(), standard = board.offers.filter(offer => offer.type !== "great-hunt");
  assert(standard.length === 4 && new Set(standard.map(offer => offer.type)).size === 4, "A castle guild's board offers four quests of different types");
  assert(standard.every(offer => GuildQuestRules.TYPES.includes(offer.type) && offer.version === 2 && offer.title), "Every offer is one of the standard types (six, and Collect since port task 9), with a title");
  const ids = JSON.stringify(board.offers.map(offer => [offer.type, offer.title]));
  close(); level = 14; open();
  assert(JSON.stringify(Guild.boardFor().offers.map(offer => [offer.type, offer.title])) === ids, "The board stays the same all day, also after a level change");
  close();
  goTo("high-stoneveil", "cairnrest"); level = 50; open();
  const town = Guild.boardFor().offers.filter(offer => offer.type !== "great-hunt");
  assert(town.length === 3 && new Set(town.map(offer => offer.type)).size === 3, "A town guild's board offers three quests of different types");
  close();
  goTo("alder-vale", "alderkeep"); level = 12;
  const accepted = { version: 2, type: "hunt", enemy: "Goblin", zone: "Plains", point: { x: 1000, y: 500 }, regionId: "alder-vale", originRegionId: "alder-vale", originCastleId: "alderkeep", progress: 0, complete: false, reward: { silver: 1, reputation: 1, rateVersion: 3 } };
  worldState.guild.quest = structuredClone(accepted);
  advanceWorldTime(1440); open();
  const next = Guild.boardFor();
  assert(next.day === WorldRules.day(worldState.minutes) && JSON.stringify(next.offers.map(offer => [offer.type, offer.title])) !== ids, "A new day draws a new board");
  assert(worldState.guild.quest?.type === "hunt" && worldState.guild.quest.point.x === 1000, "The daily refresh leaves an accepted quest alone");
  assert(Object.keys(worldState.guild.boards).every(id => worldState.guild.boards[id].day === next.day), "Boards of earlier days are dropped");
  close();
  setup({ level: 12 });
  classLevels["bounty-broker"] = 2; updateDerivedValues?.();
  open();
  assert(Guild.boardFor().offers.filter(offer => offer.type !== "great-hunt").length === 5, "Bounty Broker adds one offer to the board");
  close();
  return true;
}

/* G4, G6: one standard quest at a time; a Great Hunt beside it, one at a time; a taken notice is gone. */
async function runGuildQuestLimitChecks() {
  const { assert, setup, open, close, spot, take } = guildRework;
  setup({ level: 24, regionId: "stoneveil-foothills", settlementId: "crownspire" });
  open();
  const quest = take("hunt");
  assert(quest?.type === "hunt", "A standard quest is accepted from its notice");
  const board = Guild.boardFor();
  assert(board.offers.at(-1).taken && !document.querySelector(`[data-guild-notice="${board.offers.length - 1}"]`), "A taken notice leaves the board");
  board.offers.push(Guild.makeOffer("search"));
  spot("board"); document.querySelector(`[data-guild-notice="${board.offers.length - 1}"]`).click();
  assert(document.querySelector("#guild-accept").disabled, "A second standard quest cannot be accepted while one is active");
  Guild.accept(board.offers.length - 1);
  assert(worldState.guild.quest.type === "hunt", "Accepting anyway changes nothing");
  const hunts = board.offers.map((offer, index) => [offer, index]).filter(([offer]) => offer.type === "great-hunt");
  assert(hunts.length === 1, "The board offers a local Great Hunt when one is eligible");
  document.querySelector(`[data-guild-notice="${hunts[0][1]}"]`).click();
  assert(!document.querySelector("#guild-accept").disabled, "A Great Hunt can be accepted beside the standard quest");
  document.querySelector("#guild-accept").click();
  assert(worldState.guild.greatHunt?.uniqueId === hunts[0][0].uniqueId && worldState.guild.quest.type === "hunt", "The Great Hunt is active next to the standard quest");
  const second = structuredClone(hunts[0][0]); second.uniqueId = hunts[0][0].uniqueId === "nygheir-the-wounded" ? "zik-lun-armored-hulk" : "nygheir-the-wounded"; second.taken = false; board.offers.push(second);
  Guild.accept(board.offers.length - 1);
  assert(worldState.guild.greatHunt.uniqueId === hunts[0][0].uniqueId, "Only one Great Hunt can be active at a time");
  close();
  return true;
}

/* G18: rewards rank Hunt > Cull > Deliver > Search > Escort > Travel at every level and with bonuses, by
   the quest level (the player's level held to the guild region's range); Great Hunts above all. */
async function runGuildRewardChecks() {
  const { assert, setup, open, close } = guildRework;
  const ranked = ["travel", "escort", "search", "deliver", "cull", "hunt"];
  for (const bonuses of [{}, { questMoneyPercent: 20, questReputationPercent: 50 }]) for (let questLevel = 1; questLevel <= 100; questLevel++) {
    const table = GuildQuestRules.rewardTable(questLevel, bonuses);
    for (let index = 1; index < ranked.length; index++) assert(table[ranked[index]].silver > table[ranked[index - 1]].silver && table[ranked[index]].reputation > table[ranked[index - 1]].reputation, `${ranked[index]} pays more than ${ranked[index - 1]} at level ${questLevel}`);
    const great = GuildQuestRules.greatHuntReward(1, questLevel, bonuses);
    assert(great.silver > table.hunt.silver && great.reputation > table.hunt.reputation, `A Great Hunt pays more than a Hunt at level ${questLevel}`);
  }
  assert(GuildQuestRules.greatHuntReward(18, 15).silver === Math.round((100 + 18 * 35) * 2.4) && GuildQuestRules.greatHuntReward(18, 30).silver === Math.round((100 + 30 * 35) * 2.4) && GuildQuestRules.greatHuntReward(18, 15).reputation === 3 * Math.round((8 + 18 * 1.5) * 2.4 / 2), "A Great Hunt pays at the boss's level or the quest level, whichever is higher");
  const base = GuildQuestRules.rewardTable(30);
  assert(base.travel.silver === Math.round((100 + 30 * 35) * 1) && base.hunt.silver === Math.round((100 + 30 * 35) * 1.8) && base.cull.silver === Math.round((100 + 30 * 35) * 1.55), "Silver follows today's formula times the type's factor");
  assert(GuildQuestRules.questLevel(60, "alder-vale") === 15 && GuildQuestRules.questLevel(3, "stoneveil-foothills") === 10 && GuildQuestRules.questLevel(15, "stoneveil-foothills") === 15, "The quest level is the player's level held to the guild region's range");
  setup({ level: 60 });
  open();
  const offers = Guild.boardFor().offers.filter(offer => offer.type !== "great-hunt");
  const table = GuildQuestRules.rewardTable(15, calculateClassEffects());
  // Port task 9: a Collect quest pays as the type its ingredient's rarity stands beside (P22).
  const expected = offer => offer.type === "collect" ? GuildQuestRules.collectReward(AlchemyRules.ingredient(offer.ingredientId).rarity, 15, calculateClassEffects()) : table[offer.type];
  assert(offers.every(offer => offer.level === 15 && offer.reward.silver === expected(offer).silver && offer.reward.reputation === expected(offer).reputation), "Offers pay their type's reward at the region-held quest level");
  const travel = Guild.makeOffer("travel");
  assert(travel.reward.silver === table.travel.silver, "A Travel quest's destination does not change its reward");
  close();
  return true;
}

/* Hunt keeps its rules: an empowered local enemy at a point in its zone; defeating it completes. */
async function runGuildHuntChecks() {
  const { assert, setup, open, close, take } = guildRework;
  setup({ level: 8 });
  open();
  const quest = take("hunt");
  assert(WorldRules.zoneAt(quest.point, "alder-vale") === quest.zone && EncounterRules.enemiesForZone(quest.zone, "alder-vale").includes(quest.enemy), "The Hunt target lives in the marked zone");
  close();
  worldState.position = { ...quest.point };
  assert(Guild.objectiveAt(worldState.position)?.kind === "hunt", "Reaching the point starts the empowered Hunt fight");
  const escape = await Combat.attemptTravelEscape([quest.enemy], 25), empowered = await Combat.attemptTravelEscape([quest.enemy], 25, "hunt");
  assert(empowered.fastestEnemySpeed >= escape.fastestEnemySpeed, "The Hunt target is empowered");
  Guild.onEnemyDefeated({ name: "Empowered " + quest.enemy, guildObjective: "hunt", character: {} });
  assert(worldState.guild.quest.complete, "Defeating the target completes the Hunt");
  return true;
}

/* G11, G12, G13: Cull: 5 + 1 per 20 levels (at most 10), a 6 km circle in a zone of the target, only
   kills of the target inside it count, half of the encounters inside are the target. */
async function runGuildCullChecks() {
  const { assert, setup, open, close, take } = guildRework;
  assert([[1, 5], [19, 5], [20, 6], [59, 7], [99, 9], [100, 10], [140, 10]].every(([questLevel, count]) => GuildQuestRules.cullRequired(questLevel) === count), "Cull asks 5, plus 1 per 20 levels, at most 10");
  assert(GuildQuestRules.CULL.radius * 2 / WorldRules.UNITS_PER_KM === 6, "The Cull circle is 6 km across");
  setup({ level: 12 });
  open();
  const quest = take("cull");
  assert(quest.required === GuildQuestRules.cullRequired(quest.level) && quest.radius === GuildQuestRules.CULL.radius, "The quest carries its count and circle");
  assert(WorldRules.zoneAt(quest.point, "alder-vale") === quest.zone && EncounterRules.enemiesForZone(quest.zone, "alder-vale").includes(quest.enemy), "The circle lies in a zone where the target lives");
  assert(quest.description.includes(String(quest.required)) && quest.description.includes(quest.zone), "The notice names the count and the zone");
  close();
  const areas = Guild.areas();
  assert(areas.length === 1 && areas[0].radius === quest.radius && areas[0].point.x === quest.point.x, "The circle is shown on the map");
  WorldMap.render();
  assert(document.querySelectorAll("#map-guild-areas circle").length === 1, "The map draws the circle");
  const inside = { ...quest.point }, outside = { x: quest.point.x + quest.radius + 5, y: quest.point.y };
  const kill = (name, position, extra = {}) => { worldState.position = position; Guild.onEnemyDefeated({ name, character: {}, ...extra }); };
  kill(quest.enemy + " 1", outside);
  assert(quest.progress === 0, "Kills outside the circle do not count");
  kill("Not " + quest.enemy, inside);
  assert(quest.progress === 0, "Other enemies do not count");
  kill(quest.enemy, inside, { summoned: true });
  assert(quest.progress === 0, "Summoned copies do not count");
  kill(quest.enemy + " 2", inside);
  assert(quest.progress === 1, "A kill of the target inside the circle counts at once");
  assert(normalizeCharacter(backupPayload().character).worldState.guild.quest.progress === 1, "Cull progress is saved");
  worldState.position = inside;
  const ordinary = ["Some Enemy"];
  assert(Guild.adjustEncounter(ordinary, quest.zone, () => 0.49).includes(quest.enemy), "Half the encounters inside the circle are the target");
  assert(Guild.adjustEncounter(ordinary, quest.zone, () => 0.5) === ordinary, "The other half stay as rolled");
  worldState.position = outside;
  assert(Guild.adjustEncounter(ordinary, quest.zone, () => 0) === ordinary, "Outside the circle encounters are unchanged");
  assert(Guild.adjustEncounter([], quest.zone, () => 0).length === 0, "No encounter stays no encounter");
  // The map's Force encounter inside the circle goes through the same rule. The target is set to an enemy
  // the zone's own roll would not bring with this random value, so only the Cull rule can bring it.
  const usual = EncounterRules.generate(quest.zone, { level, regionId: "alder-vale" }, () => 0.01);
  const other = EncounterRules.enemiesForZone(quest.zone, "alder-vale").find(name => !usual.includes(name));
  assert(other, "The zone has an enemy its pinned roll leaves out");
  quest.enemy = other;
  worldState.position = inside; WorldMap.render();
  const random = Math.random; Math.random = () => 0.01;
  try { guildRework.$("map-force-encounter").click(); } finally { Math.random = random; }
  assert(worldState.pendingEncounter?.enemies.includes(quest.enemy), "Force encounter inside the circle can meet the target");
  worldState.pendingEncounter = null; guildRework.$("map-encounter-modal").hidden = true; WorldMap.render();
  Math.random = () => 0.01;
  try { WorldMap.checkTravelEncounter(WorldRules.ENCOUNTER_MINUTES); } finally { Math.random = random; }
  assert(worldState.pendingEncounter?.enemies.includes(quest.enemy), "Travel encounters inside the circle can meet the target too");
  worldState.pendingEncounter = null; guildRework.$("map-encounter-modal").hidden = true; WorldMap.render();
  for (let count = quest.progress; count < quest.required; count++) kill(quest.enemy, inside);
  assert(quest.complete && !Guild.areas().length, "Reaching the count completes the Cull and ends its circle");
  worldState.position = inside;
  assert(Guild.adjustEncounter(ordinary, quest.zone, () => 0) === ordinary, "A completed Cull no longer changes encounters");
  return true;
}

/* Deliver (G9, G34): a guild in an adjacent region; hand the letters over at its counter, then return
   to the issuing counter; a journey for Courier. */
async function runGuildDeliverChecks() {
  const { assert, setup, open, close, spot, take, goTo } = guildRework;
  setup({ level: 12 });
  open();
  const quest = take("deliver");
  assert(GuildQuestRules.adjacentRegions("alder-vale").includes(quest.destination.regionId) && GuildQuestRules.isGuild(WorldRules.settlementsForRegion(quest.destination.regionId).find(place => place.id === quest.destination.settlementId)), "Deliver goes to a guild in an adjacent region");
  assert(quest.description.includes("recommended Level"), "The notice names the destination's recommended level");
  for (let index = 0; index < 20; index++) {
    const offer = Guild.makeOffer("deliver"), place = WorldRules.settlementsForRegion(offer.destination.regionId).find(entry => entry.id === offer.destination.settlementId);
    assert(GuildQuestRules.adjacentRegions("alder-vale").includes(offer.destination.regionId) && GuildQuestRules.isGuild(place), "Every Deliver goes to a guild in an adjacent region");
  }
  assert(classEffectConditionMet({ condition: "guild-travel-quest" }), "Courier's Dispatch Guard holds while carrying a Deliver quest");
  spot("counter");
  assert(!quest.progress && worldState.guild.quest === quest, "The issuing counter neither registers nor pays an undelivered Deliver");
  close();
  goTo(quest.destination.regionId, quest.destination.settlementId);
  assert(!quest.progress, "Reaching the destination alone does not register the delivery");
  open(); spot("counter");
  assert(quest.progress === 1 && worldState.guild.quest === quest, "The destination's counter takes the letters without completing the quest");
  close();
  setup({ level: 12 }); worldState.guild.quest = structuredClone(quest);
  const before = worldState.guild.reputation, travels = worldState.guild.completedTravelQuests;
  open(); spot("counter");
  assert(!worldState.guild.quest && worldState.guild.reputation === before + quest.reward.reputation && worldState.guild.completedTravelQuests === travels + 1, "Returning to the issuing counter pays it, and it counts as a travel quest");
  close();
  setup({ level: 70, regionId: "duskpine-forest", settlementId: "ravenwatch" });
  open();
  assert(!Guild.makeOffer("deliver") && !Guild.boardFor().offers.some(offer => offer.type === "deliver"), "No Deliver is offered without a guild in an adjacent region");
  close();
  return true;
}

/* Search (G14, G15, G20): a spot in the guild's region clear of settlements and exits; Search starts a
   fight whose pre-fight escape is half as likely; winning or fleeing completes it, a defeat does not. */
async function runGuildSearchChecks() {
  const { assert, setup, open, close, take, wait, $ } = guildRework;
  setup({ level: 8 });
  open();
  const quest = take("search");
  close();
  const region = WorldRules.getRegion("alder-vale");
  assert(quest.regionId === "alder-vale" && WorldRules.zoneAt(quest.point, "alder-vale") === quest.zone, "The Search spot is in a zone of the guild's region");
  for (const offer of [quest, ...Array.from({ length: 30 }, () => Guild.makeOffer("search"))]) {
    assert(WorldRules.zoneAt(offer.point, "alder-vale") === offer.zone, "Every Search spot lies in its zone");
    assert(WorldRules.settlementsForRegion("alder-vale").every(place => WorldRules.distance(place, offer.point) > 90) && region.exits.every(exit => WorldRules.distance(exit.point, offer.point) > 110), "Search spots keep clear of settlements and region exits");
  }
  // No zone of today's maps lies near a region exit, so a stand-in exit is put where spots are drawn (with
  // the quest set aside, so its own marker's clearance doesn't hide the rule).
  const standIn = { id: "check-exit", point: { ...quest.point } };
  region.exits.push(standIn); worldState.guild.quest = null;
  try {
    for (let index = 0; index < 40; index++) {
      const offer = Guild.makeOffer("search");
      assert(!offer || WorldRules.distance(offer.point, standIn.point) > 110, "Search spots keep clear of a region exit");
    }
  } finally { region.exits.splice(region.exits.indexOf(standIn), 1); worldState.guild.quest = quest; }
  WorldMap.render();
  assert($("map-search").hidden, "No Search button away from the spot");
  const nearby = Array.from({ length: 72 }, (_, step) => ({ x: quest.point.x + Math.cos(step / 12 * Math.PI) * (70 + step), y: quest.point.y + Math.sin(step / 12 * Math.PI) * (70 + step) }))
    .find(position => WorldRules.zoneAt(position, "alder-vale") === quest.zone && WorldRules.distance(position, quest.point) > 60);
  assert(nearby, "A point of the same zone lies beyond the spot's reach");
  worldState.position = nearby; WorldMap.render();
  assert($("map-search").hidden, "No Search button elsewhere in the same zone");
  worldState.position = { ...quest.point }; WorldMap.render();
  assert(!$("map-search").hidden, "The Search button appears at the spot");
  assert(!quest.complete, "Arriving does not complete the search");
  $("map-search").click();
  assert(worldState.pendingEncounter?.objective === "search" && worldState.pendingEncounter.enemies.length, "Searching starts a fight");
  const enemies = worldState.pendingEncounter.enemies;
  const normal = await Combat.attemptTravelEscape(enemies, 25), searched = await Combat.attemptTravelEscape(enemies, 25, "search");
  assert(Math.abs(searched.chance - normal.chance * 0.5) < 1e-9, "Escaping a search fight before it starts is half as likely");
  Guild.onBattleFinished("Defeat — your party has fallen.", "search");
  assert(!quest.complete, "A defeat leaves the search open");
  Guild.avoidObjective("search");
  assert(quest.complete, "Fleeing the fight completes the search");
  quest.complete = false; worldState.pendingEncounter = null; WorldMap.render();
  assert(await Combat.startEncounter(enemies, "Search check", "search"), "The search fight starts");
  const battle = Combat.serialize(); battle.actors.filter(actor => actor.team === "enemy").forEach(actor => { actor.character.currentHp = 0; });
  Combat.restore(battle); Combat.onSheetChange();
  for (let i = 0; i < 50 && Combat.active(); i++) await wait(20);
  assert(!Combat.active() && quest.complete, "Winning the fight completes the search");
  $("combat-loot-discard")?.click();
  return true;
}

/* Escort (G14–G17): 80/10/10 ambushes rolled once on accepting, at 25–75% of the outbound trip, on roads
   too, from the nearest existing bandit group; escaping them is 70% less likely; a defeat brings them back
   later; reaching the destination registers only once they are dealt with; the issuing counter pays. */
async function runGuildEscortChecks() {
  const { assert, setup, open, close, spot, take, goTo } = guildRework;
  const counts = [[0, 1], [0.799, 1], [0.8, 0], [0.899, 0], [0.9, 2], [0.999, 2]];
  assert(counts.every(([roll, count]) => GuildQuestRules.ambushCount(() => roll) === count), "One roll gives one ambush 80%, none 10%, two 10%");
  for (let index = 0; index < 200; index++) {
    const plan = GuildQuestRules.ambushPlan(1000, Math.random, 1 + index % 2);
    assert(plan.every(ambush => ambush.at >= 250 && ambush.at <= 750), "Ambushes strike between 25% and 75% of the trip");
    if (plan.length === 2) assert(plan[1].at - plan[0].at >= 200, "A second ambush comes at least 20% of the trip later");
  }
  assert(GuildQuestRules.ambushPlan(1000, Math.random, 0).length === 0, "The no-ambush outcome plans nothing");
  assert(JSON.stringify(GuildQuestRules.ambushGroup(1)) === JSON.stringify(["Bandit", "Bandit Archer"]) && GuildQuestRules.ambushGroup(12)[0] === "Highway Bandit" && GuildQuestRules.ambushGroup(30)[0] === "Dune Raider" && GuildQuestRules.ambushGroup(90)[0] === "Caravan Bandit", "The ambush is the nearest existing bandit group by level");
  setup({ level: 12 });
  open();
  for (let index = 0; index < 20; index++) {
    const offer = Guild.makeOffer("escort");
    assert(offer.destination.settlementId !== "alderkeep" && GuildQuestRules.adjacentRegions("alder-vale").includes(offer.destination.regionId) && !offer.ambushes?.length, "Escorts go to settlements in adjacent regions; nothing is rolled before accepting");
  }
  const random = Math.random; Math.random = () => 0.5;
  let quest;
  try { quest = take("escort"); } finally { Math.random = random; }
  const trip = GuildQuestRules.tripMinutes("alder-vale", { x: 420, y: 490 }, quest.destination.regionId, quest.destination.point);
  assert(quest.destination && quest.destination.settlementId !== "alderkeep" && GuildQuestRules.adjacentRegions("alder-vale").includes(quest.destination.regionId), "Escort goes to another settlement, in an adjacent region");
  assert(Math.abs(quest.tripMinutes - trip) < 1e-6 && quest.ambushes.length === 1 && Math.abs(quest.ambushes[0].at - trip * 0.5) < 1e-6, "Accepting rolls the ambushes once, inside the outbound trip");
  close();
  quest.ambushes = [{ at: 100, done: false }]; quest.travelled = 0;
  const road = { x: 620, y: 392 };
  worldState.position = road;
  assert(WorldRules.zoneAt(road, "alder-vale") === "Road" && EncounterRules.encounterChance("Road", { level, regionId: "alder-vale" }) === 0, "Roads have no ordinary encounters");
  assert(!Guild.objectiveAt(road), "No ambush before its point of the trip");
  Guild.onTravel(60); Guild.onTravel(60);
  assert(quest.travelled === 120, "The outbound trip's travel minutes are counted");
  const ambush = Guild.objectiveAt(road);
  assert(ambush?.kind === "escort-ambush" && JSON.stringify(ambush.enemies) === JSON.stringify(GuildQuestRules.ambushGroup(GuildQuestRules.ambushLevel(level, "alder-vale"))), "The bandits strike on the road once the trip reaches their point");
  const normal = await Combat.attemptTravelEscape(ambush.enemies, 25), bandits = await Combat.attemptTravelEscape(ambush.enemies, 25, "escort-ambush");
  assert(Math.abs(bandits.chance - normal.chance * 0.3) < 1e-9, "Escaping the bandits before the fight is 70% less likely");
  Guild.onBattleFinished("Defeat — your party has fallen.", "escort-ambush");
  assert(!quest.ambushes[0].done && quest.ambushes[0].at > quest.travelled && !Guild.objectiveAt(road), "After a defeat the bandits strike again later on the way");
  quest.ambushes[0].at = quest.travelled;
  Guild.onBattleFinished("Victory — the enemies are defeated.", "escort-ambush");
  assert(quest.ambushes[0].done && !Guild.objectiveAt(road), "Winning deals with the ambush, and it does not strike twice");
  quest.ambushes = [{ at: 1e9, done: false }];
  goTo(quest.destination.regionId, quest.destination.settlementId);
  assert(!quest.progress && Guild.objectiveAt(worldState.position)?.kind === "escort-ambush", "Arriving before the ambush makes it strike on arrival");
  Guild.avoidObjective("escort-ambush");
  assert(quest.ambushes[0].done && quest.progress === 1, "Fleeing deals with it too, and the arrival then counts");
  open(); spot("counter");
  assert(worldState.guild.quest === quest, "The destination's counter does not pay the Escort");
  close();
  setup({ level: 12 }); worldState.guild.quest = structuredClone(quest);
  const before = worldState.guild.reputation;
  open(); spot("counter");
  assert(!worldState.guild.quest && worldState.guild.reputation === before + quest.reward.reputation, "The issuing counter pays the Escort");
  close();
  return true;
}

/* Travel (G10): another settlement, in an adjacent region when there is one; no fight is forced; the
   arrival registers, the issuing counter completes it. */
async function runGuildTravelChecks() {
  const { assert, setup, open, close, spot, take, goTo } = guildRework;
  setup({ level: 12 });
  open();
  for (let index = 0; index < 20; index++) {
    const offer = Guild.makeOffer("travel");
    assert(offer.destination.settlementId !== "alderkeep" && GuildQuestRules.adjacentRegions("alder-vale").includes(offer.destination.regionId), "Every Travel goes to a settlement in an adjacent region");
  }
  const quest = take("travel");
  close();
  for (const x of [500, 700, 900, 1100]) { worldState.position = { x, y: 400 }; Guild.onTravel(300); assert(!Guild.objectiveAt(worldState.position), "Travel forces no fight on the way"); }
  goTo(quest.destination.regionId, quest.destination.settlementId);
  assert(quest.progress === 1 && !quest.complete, "Reaching the destination registers it");
  setup({ level: 12, regionId: "stoneveil-foothills", settlementId: "crownspire" }); worldState.guild.quest = structuredClone(quest);
  open(); spot("counter");
  assert(worldState.guild.quest, "Another guild's counter does not complete it");
  close();
  setup({ level: 12 }); worldState.guild.quest = structuredClone(quest);
  assert(worldState.guild.quest && !worldState.guild.quest.complete, "Returning alone does not complete it");
  const before = worldState.guild.reputation;
  open(); spot("counter");
  assert(!worldState.guild.quest && worldState.guild.reputation === before + quest.reward.reputation, "The issuing counter completes it");
  close();
  setup({ level: 12, regionId: "duskpine-forest", settlementId: "ravenwatch" });
  assert(!Guild.makeOffer("travel") && !Guild.makeOffer("escort"), "No Travel or Escort without another reachable settlement");
  return true;
}

/* G19: a destination in another region is marked on the road (or harbour) leading there; in its region the
   settlement is marked; the way back is marked the same way. */
async function runGuildMarkerChecks() {
  const { assert, setup, goTo } = guildRework;
  setup({ level: 12 });
  const crownspire = WorldRules.settlementsForRegion("stoneveil-foothills").find(place => place.id === "crownspire");
  worldState.guild.quest = { version: 2, type: "travel", destination: { regionId: "stoneveil-foothills", settlementId: "crownspire", name: "Crownspire", point: { x: crownspire.x, y: crownspire.y } }, regionId: "stoneveil-foothills", originRegionId: "alder-vale", originCastleId: "alderkeep", progress: 0, complete: false, reward: { silver: 1, reputation: 1, rateVersion: 3 } };
  const exit = WorldRules.getRegion("alder-vale").exits.find(entry => entry.targetRegionId === "stoneveil-foothills");
  let markers = Guild.markers();
  assert(markers.length === 1 && markers[0].point.x === exit.point.x && markers[0].point.y === exit.point.y && markers[0].label.includes("Stoneveil Foothills"), "The road toward the destination's region carries the marker");
  goTo("stoneveil-foothills", "southwatch");
  markers = Guild.markers();
  assert(markers.length === 1 && markers[0].point.x === crownspire.x && markers[0].point.y === crownspire.y, "In its region the destination itself is marked");
  worldState.guild.quest.progress = 1;
  markers = Guild.markers();
  const back = WorldRules.getRegion("stoneveil-foothills").exits.find(entry => entry.targetRegionId === "alder-vale");
  assert(markers.length === 1 && markers[0].point.x === back.point.x && markers[0].label.includes("Alderkeep"), "The way back is marked toward the issuing guild");
  setup({ level: 30, regionId: "sunward-coast", settlementId: "westport" });
  worldState.guild.quest = { version: 2, type: "deliver", destination: { regionId: "cinder-isle", settlementId: "emberhaven", name: "Emberhaven", point: { x: 0, y: 0 } }, regionId: "cinder-isle", originRegionId: "sunward-coast", originCastleId: "westport", progress: 0, complete: false, reward: { silver: 1, reputation: 1, rateVersion: 3 } };
  const harbour = WorldRules.settlementsForRegion("sunward-coast").find(place => place.boat?.regionId === "cinder-isle");
  markers = Guild.markers();
  assert(markers.length === 1 && markers[0].point.x === harbour.x && markers[0].point.y === harbour.y, "A destination across the sea is marked at the harbour");
  return true;
}

/* G24: the counter pays every finished quest at once, shows a rising note with the gold and Reputation,
   and never pays twice. */
async function runGuildCounterChecks() {
  const { assert, setup, open, close, spot, take, $ } = guildRework;
  setup({ level: 12 });
  open();
  const quest = take("hunt");
  quest.complete = true;
  const money = ShopRules.walletValue(currencies), fame = worldState.guild.reputation;
  spot("counter");
  assert(!worldState.guild.quest && ShopRules.walletValue(currencies) === money + quest.reward.silver && worldState.guild.reputation === fame + quest.reward.reputation, "The counter pays a finished quest at once");
  const note = $("guild-reward-note");
  assert(!note.hidden && note.classList.contains("is-shown") && note.textContent.includes(ShopRules.formatPrice(quest.reward.silver)) && note.textContent.includes(`${quest.reward.reputation} Reputation`), "A note shows the gold and Reputation");
  assert(getComputedStyle(note).pointerEvents === "none", "The note does not block clicks");
  spot("counter");
  assert(ShopRules.walletValue(currencies) === money + quest.reward.silver && worldState.guild.reputation === fame + quest.reward.reputation, "Clicking the counter again pays nothing more");
  assert(!$("guild-dock").hidden && $("guild-dock-title").textContent === "Guild counter" && $("guild-artifacts"), "The counter's panel offers the artifacts");
  $("guild-artifacts").click();
  assert(worldState.guild.artifactOffers.length === 3 && document.querySelectorAll("[data-guild-artifact]").length === 3, "The artifact menu shows three choices");
  document.querySelector("[data-guild-artifact]").click();
  assert(worldState.guild.artifactChallenge && worldState.guild.reputation === fame + quest.reward.reputation - 70, "Choosing an artifact keeps its cost and challenge");
  close();
  return true;
}

/* G2, G3, G21–G23, G25: the room is the menu: castle guild with board, counter, adventurers, hooded man and
   trophies; town guild without the hooded man; trophies only after a Great Hunt here; hover glow. */
async function runGuildInteriorChecks() {
  const { assert, setup, open, close, spot, goTo, $ } = guildRework;
  setup({ level: 24, regionId: "stoneveil-foothills", settlementId: "crownspire" });
  open();
  const scene = $("guild-banner"), kinds = () => [...scene.querySelectorAll("[data-guild-spot]")].map(element => element.dataset.guildSpot).sort();
  assert(scene.dataset.variant === "guild-hall" && JSON.stringify(kinds()) === JSON.stringify(["board", "counter", "hooded", "tavern", "trophies"]), "A castle city's guild hall has the board, counter, adventurers, hooded man and trophies");
  assert(SceneBanners.placeholderArt("guild-hall") && SceneBanners.placeholderArt("guild-town"), "Both interiors are marked as placeholder art");
  const box = kind => scene.querySelector(`[data-guild-spot="${kind}"] .scene-spot-hit`).getBBox();
  assert(box("board").x + box("board").width / 2 < 640 && box("counter").x + box("counter").width / 2 < 640 && box("tavern").x + box("tavern").width / 2 > 640 && box("hooded").x > 640, "The guild is on the left and the tavern on the right");
  const trophies = scene.querySelector('[data-guild-spot="trophies"]');
  assert(trophies.getAttribute("aria-disabled") === "true", "The trophies wait for a Great Hunt completed here");
  for (const [kind, title] of [["board", "Bulletin board"], ["counter", "Guild counter"], ["tavern", "Tavern · Recruits"]]) {
    spot(kind);
    assert(!$("guild-dock").hidden && $("guild-dock-title").textContent === title, `${title} opens in the docked panel`);
  }
  spot("hooded");
  assert($("guild-dock-title").textContent === "The hooded man", "The hooded man opens the information menu");
  for (let i = 0; i < 50 && $("guild-panel").textContent.includes("Loading"); i++) await guildRework.wait(20);
  assert($("guild-panel").textContent.includes("GUILD INFORMATION"), "The information menu is the existing one");
  const glow = scene.querySelector('[data-guild-spot="board"] .scene-spot-glow');
  assert(getComputedStyle(glow).opacity === "0" && glow.getAttribute("filter")?.startsWith("url(#"), "The outline glow is hidden until hovered");
  const sheet = [...document.styleSheets].flatMap(styles => { try { return [...styles.cssRules]; } catch { return []; } }).map(rule => rule.cssText).join("\n");
  assert(/\.scene-spot:is\(:hover, :focus-visible\):not\(\.is-disabled\) :is\(\.scene-spot-glow, \.scene-spot-label\) \{ opacity: 1; \}/.test(sheet), "Hover or keyboard focus lights the outline and the name");
  const escape = () => $("guild-modal").dispatchEvent(new KeyboardEvent("keydown", { key: "Escape", bubbles: true }));
  escape();
  assert($("guild-dock").hidden && !$("guild-modal").hidden, "Escape closes the panel first");
  escape();
  assert($("guild-modal").hidden, "Escape again leaves the guild");
  worldState.guild.greatHuntsCompleted.push("zik-lun-armored-hulk");
  open();
  assert(scene.querySelector('[data-guild-spot="trophies"]').getAttribute("aria-disabled") === "false", "A completed Great Hunt here makes the trophies clickable");
  spot("trophies");
  assert($("guild-dock-title").textContent === "Great Hunts completed" && document.querySelector('[data-guild-rematch="zik-lun-armored-hulk"]'), "The trophies open the Great Hunts completed with their rematch markers");
  close();
  goTo("high-stoneveil", "cairnrest"); level = 50;
  open();
  assert(scene.dataset.variant === "guild-town" && JSON.stringify(kinds()) === JSON.stringify(["board", "counter", "tavern", "trophies"]), "A town guild has no hooded man");
  spot("tavern");
  assert(Guild.recruitsFor().length === 2, "A town guild's tavern has two recruits");
  close();
  return true;
}

/* G5, G28, G30, G31 and task 2, section 14: castle 5 recruits, town 2; level = the region's midpoint, never
   above yours; cost 30 + 2 × level + 25 per companion; one melee weapon and one body armor rolled at the
   region's level; every stat and class point left to spend. */
async function runGuildRecruitChecks() {
  const { assert, setup, open, close, spot, $ } = guildRework;
  assert(GuildQuestRules.recruitLevel(30, "alder-vale") === 8 && GuildQuestRules.recruitLevel(5, "stoneveil-foothills") === 5 && GuildQuestRules.recruitLevel(60, "duskpine-forest") === 60 && GuildQuestRules.recruitLevel(80, "duskpine-forest") === 68, "Recruits come at the region's midpoint level, never above yours");
  assert(GuildQuestRules.recruitCost(10, 0) === 50 && GuildQuestRules.recruitCost(50, 0) === 130 && GuildQuestRules.recruitCost(90, 2) === 260, "Recruits cost 30 + 2 × level, plus 25 per companion");
  const odds = level => ForgeRules.rarityOdds(level).slice(0, 5), expected = (level, roll) => { const values = odds(level), total = values.reduce((a, b) => a + b, 0); let left = roll * total; for (let index = 0; index < 5; index++) { left -= values[index]; if (left < 0) return ForgeRules.RARITIES[index]; } return "legendary"; };
  assert([0.1, 0.5, 0.8, 0.97].every(roll => GuildQuestRules.gearRarity("alder-vale", () => roll) === expected(8, roll) && GuildQuestRules.gearRarity("duskpine-forest", () => roll) === expected(68, roll)), "Gear rarity rolls like the Blacksmith's stock at the region's level");
  setup({ level: 30, reputation: 200 });
  open(); spot("tavern");
  const offers = Guild.recruitsFor();
  assert(offers.length === 5 && document.querySelectorAll("[data-guild-recruit]").length === 5, "A castle city's tavern offers five recruits");
  for (const offer of offers) {
    const c = offer.character, weapon = getItem(c.equipment.primaryHand), armor = getItem(c.equipment.armor);
    assert(c.level === 8, "The recruit has the region's level");
    assert(weapon?.category === "weapon" && weapon.type === "meele" && !weapon.isBow && c.inventory.filter(id => getItem(id)?.category === "weapon").length === 1, "Exactly one melee weapon");
    assert(armor?.category === "armor" && armor.type !== "shield" && c.inventory.filter(id => getItem(id)?.category === "armor").length === 1, "Exactly one body armor");
    assert(statDefinitions.every(stat => c.stats[stat.key] === defaultCharacter().stats[stat.key] + 7) && c.availablePoints === 35 && c.perkPoints === 8 && !Object.values(c.classLevels).some(Boolean), "Every stat and class point is left to spend");
  }
  for (const offer of Companions.makeOffers({ count: 30, level: 20, regionId: "stormwake-hills" })) {
    const c = offer.character, weapon = getItem(c.equipment.primaryHand), armor = getItem(c.equipment.armor);
    assert(weapon?.type === "meele" && !weapon.isBow && armor?.category === "armor" && armor.type !== "shield" && c.level === 20 && c.availablePoints === 95 && c.perkPoints === 20, "Every recruit has one melee weapon, body armor and unspent points");
  }
  const chosen = offers[0], cost = GuildQuestRules.recruitCost(8, 0);
  document.querySelector(`[data-guild-recruit="${chosen.id}"]`).click();
  assert(companions.length === 1 && companions[0].id === chosen.id && worldState.guild.reputation === 200 - cost && companions[0].character.availablePoints === 35 && companions[0].character.perkPoints === 8, "Recruiting spends the cost and keeps the points unspent");
  const saved = normalizeCharacter(backupPayload().character);
  assert(saved.companions[0].character.availablePoints === 35 && saved.companions[0].character.perkPoints === 8, "A recruit's unspent points are saved");
  assert(saved.companions[0].character.equipment.primaryHand === chosen.character.equipment.primaryHand && saved.companions[0].character.equipment.armor === chosen.character.equipment.armor, "A recruit's gear is saved");
  assert(!saved.recruitOffers.length && $("guild-dock").hidden, "The other offers go once someone is hired, as before");
  spot("tavern");
  const next = Guild.recruitsFor();
  assert(next.length === 5 && next.every(offer => !offers.some(old => old.id === offer.id)) && normalizeCharacter(backupPayload().character).recruitOffers.every(offer => offer.guildId === "alderkeep"), "The next visit draws new recruits, saved with their guild");
  assert(document.querySelector(`[data-guild-recruit="${next[0].id}"]`).textContent.includes(String(GuildQuestRules.recruitCost(8, 1))), "The next recruit costs 25 more");
  worldState.guild.reputation = 10; Guild.render();
  assert(document.querySelector("[data-guild-recruit]").disabled, "Not enough Reputation blocks recruiting");
  worldState.guild.reputation = 999; companions.push(...Companions.makeOffers({ count: 2, level: 8 })); Guild.render();
  assert(document.querySelector("[data-guild-recruit]").disabled && companions.length === 3, "A full party blocks recruiting");
  close();
  return true;
}

/* G26, G29: the map's Quests card shows the active quests at any time and can give them up. */
async function runGuildQuestCardChecks() {
  const { assert, setup, confirm, $ } = guildRework;
  setup({ level: 12 });
  WorldMap.open(); WorldMap.render();
  assert($("map-quests") && $("map-quests").getAttribute("aria-keyshortcuts") === "J", "The map has a Quests button on J");
  worldState.guild.quest = { version: 2, type: "hunt", title: "Bounty on an Empowered Goblin", enemy: "Goblin", zone: "Plains", point: { x: 1000, y: 500 }, regionId: "alder-vale", originRegionId: "alder-vale", originCastleId: "alderkeep", progress: 0, complete: false, reward: { silver: 100, reputation: 7, rateVersion: 3 } };
  worldState.guild.greatHunt = { version: 2, type: "great-hunt", uniqueId: "great-imp-jiruan", title: "Great Hunt: Great Imp Ji'ruan", enemy: "Great Imp Ji'ruan", zone: "Deep Forest", point: { x: 365, y: 213 }, regionId: "alder-vale", originRegionId: "alder-vale", originCastleId: "alderkeep", progress: 0, complete: false, reward: { silver: 1000, reputation: 99, rateVersion: 3 } };
  worldState.position = { x: 800, y: 700 };
  $("map-quests").click();
  assert(!$("guild-quests-modal").hidden && $("guild-quests-list").textContent.includes("Bounty on an Empowered Goblin") && $("guild-quests-list").textContent.includes("Great Imp Ji'ruan"), "The card shows the quest and the Great Hunt anywhere on the map");
  confirm(false, () => document.querySelector('#guild-quests-list [data-guild-give-up="quest"]').click());
  assert(worldState.guild.quest, "Declining the confirmation keeps the quest");
  confirm(true, () => document.querySelector('#guild-quests-list [data-guild-give-up="quest"]').click());
  assert(!worldState.guild.quest && worldState.guild.greatHunt && !$("guild-quests-list").textContent.includes("Goblin") && !normalizeCharacter(backupPayload().character).worldState.guild.quest, "Giving up away from the guild removes the quest and saves it");
  confirm(true, () => document.querySelector('#guild-quests-list [data-guild-give-up="greatHunt"]').click());
  assert(!worldState.guild.greatHunt, "A Great Hunt can be given up there too");
  $("guild-quests-modal").querySelector("[data-guild-quests-close]").click();
  return true;
}

/* G32: the class menu's Class information button lists only the classes whose information was bought and
   shows the page that was bought. */
async function runGuildClassInformationChecks() {
  const { assert, setup, $ } = guildRework;
  setup({ level: 12 });
  const bought = GuildInformationRules.playerClasses(classCatalog, raceCatalog).find(definition => !classRequirementsMet(definition));
  assert(bought, "A class with unmet requirements exists to buy information about");
  worldState.guild.information.revealed.class = [bought.id];
  $("classes-open-button").click();
  assert($("class-info-button") && !$("class-info-button").hidden && $("class-info-button").textContent === "Class information", "The class menu has a Class information button");
  $("class-info-button").click();
  const entries = [...document.querySelectorAll("#class-info-list [data-class-info-id]")].map(entry => entry.dataset.classInfoId);
  assert(!$("class-info-panel").hidden && JSON.stringify(entries) === JSON.stringify([bought.id]), "Only classes whose information was bought are listed");
  const detail = $("class-detail-panel").textContent;
  assert(detail.includes("REVEALED CLASS REQUIREMENTS") && detail.includes(bought.name) && !$("class-detail-panel").querySelector(".class-level-row"), "The tab shows the bought requirements page, not the class's levels");
  const another = GuildInformationRules.playerClasses(classCatalog, raceCatalog).find(definition => definition.id !== bought.id);
  worldState.guild.information.revealed.class.push(another.id); renderClasses();
  assert(document.querySelectorAll("#class-info-list [data-class-info-id]").length === 2, "Newly bought information appears in the tab");
  $("class-info-button").click();
  assert($("class-info-panel").hidden && !$("class-list").closest("section").hidden, "The button goes back to the classes");
  $("classes-modal").hidden = true; updateModalState();
  return true;
}

/* G33 and task 2, section 22: old kinds of active quests are dropped with a note; Hunt and Search are kept;
   a Great Hunt in the old quest slot moves to its own; new quest state survives saving. */
async function runGuildSaveChecks() {
  const { assert, setup, open, close, $ } = guildRework;
  for (const type of ["clear", "escort", "travel"]) {
    const saved = WorldRules.normalize({ guild: { quest: { type, enemy: "Goblin", required: 3, townId: "millhaven", point: { x: 1000, y: 500 }, reward: { silver: 100, reputation: 11 } } } });
    assert(!saved.guild.quest && saved.guild.notice, `An old ${type} quest is dropped with a note`);
  }
  for (const type of ["hunt", "search"]) {
    const saved = WorldRules.normalize({ guild: { quest: { type, enemy: "Goblin", zone: "Plains", point: { x: 1000, y: 500 }, reward: { silver: 100, reputation: 11, rateVersion: 2 } } } });
    assert(saved.guild.quest?.type === type && saved.guild.quest.version === 2 && !saved.guild.notice, `An old ${type} quest is kept`);
  }
  const moved = WorldRules.normalize({ guild: { quest: { type: "great-hunt", uniqueId: "great-imp-jiruan", enemy: "Great Imp Ji'ruan", zone: "Deep Forest", point: { x: 365, y: 213 }, reward: { silver: 1, reputation: 3, rateVersion: 3 } } } });
  assert(!moved.guild.quest && moved.guild.greatHunt?.uniqueId === "great-imp-jiruan", "A Great Hunt in the old quest slot moves to its own slot");
  setup({ level: 12 });
  worldState.guild.notice = WorldRules.normalize({ guild: { quest: { type: "clear", reward: { silver: 1, reputation: 1 } } } }).guild.notice;
  open();
  assert($("guild-status").textContent === worldState.guild.notice || $("guild-status").textContent.includes("withdrawn"), "The guild shows the note");
  assert(!worldState.guild.notice, "The note is shown once");
  close();
  open();
  const quest = guildRework.take("escort");
  quest.travelled = 42;
  const restored = normalizeCharacter(backupPayload().character).worldState.guild;
  assert(restored.quest.type === "escort" && restored.quest.destination.settlementId === quest.destination.settlementId && restored.quest.travelled === 42 && restored.quest.tripMinutes === quest.tripMinutes && JSON.stringify(restored.quest.ambushes) === JSON.stringify(quest.ambushes), "An Escort's destination, trip and ambushes are saved");
  assert(restored.boards.alderkeep?.offers.length === worldState.guild.boards.alderkeep.offers.length && restored.boards.alderkeep.offers.at(-1).taken, "Today's board and its taken notice are saved");
  const nextDay = normalizeCharacter({ ...backupPayload().character, worldState: { ...backupPayload().character.worldState, minutes: worldState.minutes + 1440 } }).worldState.guild;
  assert(!Object.keys(nextDay.boards).length && nextDay.quest?.type === "escort", "A saved board is dropped on a later day; the quest is not");
  close();
  return true;
}

async function runGuildReworkChecks() {
  for (const check of [runGuildBoardChecks, runGuildQuestLimitChecks, runGuildRewardChecks, runGuildHuntChecks, runGuildCullChecks, runGuildDeliverChecks, runGuildSearchChecks, runGuildEscortChecks, runGuildTravelChecks, runGuildMarkerChecks, runGuildCounterChecks, runGuildInteriorChecks, runGuildRecruitChecks, runGuildQuestCardChecks, runGuildClassInformationChecks, runGuildSaveChecks]) await check();
  worldState = WorldRules.normalize(); Combat.restore(null); WorldMap.resetView(); saveCharacter();
  return true;
}

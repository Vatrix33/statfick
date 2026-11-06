/* Adventurers Guild quest rules (port task 2, the Guild Rework, 2026-11-06): the six standard quest
   types, their rewards and the numbers the guild, the map and the recruits share. Pure functions over
   WorldRules and EncounterRules, so the browser checks can call them directly. The user's design answers
   (G1–G34) are in engine/Docs/Engine-Plan.md, "Decisions for task 2, Guild Rework". */
(function (root) {
  "use strict";
  // Ranked from the lowest reward to the highest (G18); Great Hunts sit above all of them.
  const TYPES = ["hunt", "cull", "deliver", "search", "escort", "travel"];
  const RANKED = ["travel", "escort", "search", "deliver", "cull", "hunt"];
  const FACTORS = { hunt: 1.8, cull: 1.55, deliver: 1.35, search: 1.2, escort: 1.1, travel: 1 };
  const NAMES = { hunt: "Hunt", cull: "Cull", deliver: "Deliver", search: "Search", escort: "Escort", travel: "Travel", "great-hunt": "Great Hunt" };
  // Quests that are a journey to another place and back (Courier counts them, G34).
  const JOURNEYS = ["deliver", "escort", "travel"];
  const GREAT_HUNT_FACTOR = 2.4, GREAT_HUNT_REPUTATION_TIMES = 3;
  const OFFERS = { castle: 4, town: 3 };            // standard quests per board and day (G8)
  const RECRUITS = { castle: 5, town: 2 };          // recruits to choose from (G5)
  const CULL = { base: 5, perLevels: 20, max: 10, radius: 40, targetShare: 0.5 }; // G11–G13; 40 units = 3 km
  const SEARCH = { reach: 60, clearance: { settlement: 90, site: 90, exit: 110, marker: 80 } };
  // Pre-fight escape only (G14): the final chance after the +25 bonus is multiplied by these.
  const ESCAPE_FACTORS = { search: 0.5, "escort-ambush": 0.3 };
  // One ambush 80%, none 10%, two 10%: three exclusive outcomes from one roll (task 2, section 6).
  const AMBUSH = { outcomes: [{ below: 0.8, count: 1 }, { below: 0.9, count: 0 }, { below: 1, count: 2 }], from: 0.25, to: 0.75, gap: 0.2, retry: [0.25, 0.5] };
  // The existing bandit-only groups of the encounter tables, by their enemies' level (G17: unchanged).
  const AMBUSH_GROUPS = [
    { level: 4, enemies: ["Bandit", "Bandit Archer"] },
    { level: 10, enemies: ["Highway Bandit", "Highway Bandit Archer"] },
    { level: 15, enemies: ["Bandit Captain", "Highway Bandit Archer"] },
    { level: 36, enemies: ["Dune Raider", "Dune Raider"] },
    { level: 44, enemies: ["Caravan Bandit"] },
  ];

  const W = () => root.WorldRules, E = () => root.EncounterRules;
  const clamp = (value, low, high) => Math.min(high, Math.max(low, value));
  const unit = random => { const value = Number(random()); return Number.isFinite(value) ? clamp(value, 0, 1 - Number.EPSILON) : 0; };
  const between = (random, low, high) => low + (high - low) * unit(random);

  /* ---------- Levels ---------- */
  // A region's level range: its own, or (the Alder Vale has none) the span of its encounter zones.
  function regionLevelRange(regionId) {
    const region = W().getRegion(regionId);
    if (region?.levelRange) return { minimum: region.levelRange.minimum, maximum: region.levelRange.maximum };
    const profiles = Object.values(E().regionRules(W().normalizeRegionId(regionId)).profiles || {}).filter(profile => !profile.safe);
    if (!profiles.length) return { minimum: 1, maximum: 100 };
    return { minimum: Math.min(...profiles.map(profile => profile.minimum)), maximum: Math.max(...profiles.map(profile => profile.maximum)) };
  }
  const rangeText = range => range.minimum === range.maximum ? `Level ${range.minimum}` : `Level ${range.minimum}–${range.maximum}`;
  // The quest level: the player's level held to the issuing guild's region (G18).
  function questLevel(playerLevel, regionId) {
    const range = regionLevelRange(regionId);
    return clamp(Math.max(1, Math.floor(Number(playerLevel) || 1)), range.minimum, range.maximum);
  }
  const cullRequired = level => Math.min(CULL.max, CULL.base + Math.floor(Math.max(1, level) / CULL.perLevels));

  /* ---------- Rewards (G18) ---------- */
  // Every type at one level; each type pays at least 1 more silver and 1 more Reputation than the type
  // below it, so rounding never ties two ranks.
  function rewardTable(level, bonuses = {}) {
    const money = 1 + (Number(bonuses.questMoneyPercent) || 0) / 100, fame = 1 + (Number(bonuses.questReputationPercent) || 0) / 100;
    const table = {};
    let previous = null;
    for (const type of RANKED) {
      const factor = FACTORS[type];
      let silver = Math.max(0, Math.round((100 + level * 35) * factor * money));
      let reputation = Math.max(0, Math.round(Math.max(5, (8 + level * 1.5) * factor) / 2 * fame));
      if (previous) { silver = Math.max(silver, previous.silver + 1); reputation = Math.max(reputation, previous.reputation + 1); }
      table[type] = previous = { silver, reputation, rateVersion: 3 };
    }
    return table;
  }
  const reward = (type, level, bonuses) => ({ ...rewardTable(level, bonuses)[type] });
  // Great Hunts: x2.4 money and three times x2.4 Reputation, at the boss's level or the quest level,
  // whichever is higher, so they always pay more than a Hunt at the same guild (x1.8 at the quest level).
  function greatHuntReward(bossLevel, level, bonuses = {}) {
    const money = 1 + (Number(bonuses.questMoneyPercent) || 0) / 100, fame = 1 + (Number(bonuses.questReputationPercent) || 0) / 100;
    const at = Math.max(bossLevel, level);
    const silver = Math.round((100 + at * 35) * GREAT_HUNT_FACTOR * money);
    const reputation = GREAT_HUNT_REPUTATION_TIMES * Math.max(0, Math.round(((8 + at * 1.5) * GREAT_HUNT_FACTOR / 2) * fame));
    return { silver, reputation, rateVersion: 3 };
  }

  /* ---------- Places ---------- */
  const isGuild = place => Boolean(place) && (place.kind === "Castle city" || Boolean(place.guildServices?.includes("quest")));
  const isCastleGuild = place => place?.kind === "Castle city";
  const guildsIn = regionId => W().settlementsForRegion(regionId).filter(place => isGuild(place) && W().canAccessSettlement(place));
  const settlementsIn = regionId => W().settlementsForRegion(regionId).filter(place => W().canAccessSettlement(place));
  // Regions one road or boat away (G9), each with how it's reached from here.
  function links(regionId) {
    const id = W().normalizeRegionId(regionId), region = W().getRegion(id), result = [];
    for (const exit of region.exits || []) if (exit.targetRegionId && W().regions[exit.targetRegionId]) result.push({ regionId: exit.targetRegionId, kind: "exit", exit });
    for (const place of region.settlements || []) if (place.boat?.regionId && W().regions[place.boat.regionId]) result.push({ regionId: place.boat.regionId, kind: "boat", settlement: place });
    return result;
  }
  const adjacentRegions = regionId => [...new Set(links(regionId).map(link => link.regionId))].filter(id => id !== W().normalizeRegionId(regionId));
  // The first step from one region toward another (breadth-first over roads and boats), or null.
  function firstStep(fromRegionId, toRegionId) {
    const start = W().normalizeRegionId(fromRegionId), goal = W().normalizeRegionId(toRegionId);
    if (start === goal) return null;
    const seen = new Set([start]), queue = links(start).map(link => ({ first: link, regionId: link.regionId }));
    while (queue.length) {
      const next = queue.shift();
      if (seen.has(next.regionId)) continue;
      seen.add(next.regionId);
      if (next.regionId === goal) return next.first;
      for (const link of links(next.regionId)) if (!seen.has(link.regionId)) queue.push({ first: next.first, regionId: link.regionId });
    }
    return null;
  }
  // Travel minutes on land from a point in one region to a point in the same or an adjacent region,
  // along the fastest routes (a boat crossing passes no land and counts nothing).
  function tripMinutes(fromRegionId, fromPoint, toRegionId, toPoint) {
    const rules = W(), from = rules.normalizeRegionId(fromRegionId), to = rules.normalizeRegionId(toRegionId);
    const leg = (regionId, a, b) => {
      const route = rules.routeBetween(a, b, 0, regionId), minutes = rules.routeTravelMinutes(a, route, 0, regionId);
      return Number.isFinite(minutes) ? minutes : rules.distance(a, b) / rules.UNITS_PER_KM / 4 * 60;
    };
    if (from === to) return leg(from, fromPoint, toPoint);
    const link = links(from).find(entry => entry.regionId === to);
    if (!link) return leg(from, fromPoint, fromPoint) + leg(to, toPoint, toPoint);
    if (link.kind === "exit") return leg(from, fromPoint, link.exit.point) + leg(to, link.exit.targetPoint || rules.getRegion(to).start, toPoint);
    const landing = rules.settlementsForRegion(to).find(place => place.id === link.settlement.boat.settlementId) || rules.getRegion(to).start;
    return leg(from, fromPoint, link.settlement) + leg(to, landing, toPoint);
  }

  /* ---------- Escort ambushes (G16, G17) ---------- */
  // One roll decides how many ambushes; each strikes at a share of the outbound trip's travel time.
  function ambushCount(random = Math.random) {
    const roll = unit(random);
    return AMBUSH.outcomes.find(outcome => roll < outcome.below).count;
  }
  function ambushPlan(trip, random = Math.random, count = ambushCount(random)) {
    const total = Math.max(1, Number(trip) || 1);
    if (count === 1) return [{ at: total * between(random, AMBUSH.from, AMBUSH.to), done: false }];
    if (count === 2) {
      const first = between(random, AMBUSH.from, AMBUSH.to - AMBUSH.gap), second = between(random, first + AMBUSH.gap, AMBUSH.to);
      return [{ at: total * first, done: false }, { at: total * second, done: false }];
    }
    return [];
  }
  // After a defeat the bandits strike again, later on the way (G15).
  const ambushRetry = (travelled, trip, random = Math.random) => travelled + Math.max(1, trip) * between(random, AMBUSH.retry[0], AMBUSH.retry[1]);
  // The nearest group by level; on a tie the lower one.
  function ambushGroup(level) {
    return [...AMBUSH_GROUPS].sort((a, b) => Math.abs(a.level - level) - Math.abs(b.level - level) || a.level - b.level)[0].enemies.slice();
  }
  const ambushLevel = (playerLevel, regionId) => questLevel(playerLevel, regionId);
  // The pre-fight escape chance for a quest encounter (G14).
  const escapeChance = (chance, objective) => ESCAPE_FACTORS[objective] ? chance * ESCAPE_FACTORS[objective] : chance;

  /* ---------- Notices ---------- */
  const plural = name => name.endsWith("Wolf") ? name.replace(/Wolf$/, "Wolves") : /(s|x|ch|sh)$/.test(name) ? name + "es" : name.endsWith("y") && !/[aeiou]y$/.test(name) ? name.slice(0, -1) + "ies" : name + "s";
  const pluralFor = (name, count) => count === 1 ? name : plural(name);
  function title(quest) {
    const place = quest.destination?.name || "";
    const zone = String(quest.zone || "").replace(" - near Town", "");
    if (quest.type === "hunt") return `Bounty on an Empowered ${quest.enemy}`;
    if (quest.type === "cull") return `${plural(quest.enemy)} of the ${zone}`;
    if (quest.type === "deliver") return `Sealed Letters for ${place}`;
    if (quest.type === "search") return `Strange Signs in the ${zone}`;
    if (quest.type === "escort") return `A Caravan to ${place}`;
    if (quest.type === "travel") return `The Road to ${place}`;
    if (quest.type === "great-hunt") return `Great Hunt: ${quest.enemy}`;
    return NAMES[quest.type] || "Contract";
  }

  /* ---------- Recruits (G28, G30, G31) ---------- */
  const regionMidpoint = regionId => { const range = regionLevelRange(regionId); return Math.round((range.minimum + range.maximum) / 2); };
  const recruitLevel = (playerLevel, regionId) => Math.max(1, Math.min(Math.max(1, Math.floor(Number(playerLevel) || 1)), regionMidpoint(regionId)));
  const recruitCost = (recruitLevelValue, companionCount) => 30 + 2 * Math.max(1, recruitLevelValue) + 25 * Math.max(0, companionCount);
  // A rarity rolled like the Blacksmith's stock at the region's level (never masterwork).
  function gearRarity(regionId, random = Math.random) {
    const forge = root.ForgeRules;
    if (!forge) return "common";
    const odds = forge.rarityOdds(regionMidpoint(regionId)).slice(0, 5);
    const total = odds.reduce((sum, value) => sum + Math.max(0, value), 0);
    let roll = unit(random) * total;
    for (let index = 0; index < odds.length; index++) { roll -= Math.max(0, odds[index]); if (roll < 0) return forge.RARITIES[index]; }
    return forge.RARITIES[odds.length - 1];
  }

  root.GuildQuestRules = {
    TYPES, RANKED, FACTORS, NAMES, JOURNEYS, OFFERS, RECRUITS, CULL, SEARCH, ESCAPE_FACTORS, AMBUSH, AMBUSH_GROUPS,
    GREAT_HUNT_FACTOR, GREAT_HUNT_REPUTATION_TIMES,
    regionLevelRange, rangeText, questLevel, cullRequired, rewardTable, reward, greatHuntReward,
    isGuild, isCastleGuild, guildsIn, settlementsIn, links, adjacentRegions, firstStep, tripMinutes,
    ambushCount, ambushPlan, ambushRetry, ambushGroup, ambushLevel, escapeChance,
    plural, pluralFor, title, regionMidpoint, recruitLevel, recruitCost, gearRarity,
  };
  if (typeof module !== "undefined" && module.exports) module.exports = root.GuildQuestRules;
})(globalThis);

/* Map units: 40 per 3 km, so a region's 1600 × 1000 units span 120 × 75 km. */
(function (root) {
  const settlements = [
    { id: "alderkeep", name: "Alderkeep", kind: "Castle city", x: 420, y: 490 },
    { id: "millhaven", name: "Millhaven", kind: "Town", x: 1110, y: 225 },
    { id: "willowford", name: "Willowford", kind: "Town", x: 1190, y: 795 },
    { id: "briarfield", name: "Briarfield", kind: "Town", x: 295, y: 835 }
  ];
  const clamp = (n, a, b) => Math.min(b, Math.max(a, n));
  const number = (n, fallback) => typeof n === "number" && Number.isFinite(n) ? n : fallback;
  const timeSpeeds = Object.freeze([0.5, 1, 1.5, 2]);
  // Regions were tripled in size (40 × 25 km became 120 × 75 km) without moving anything:
  // each map unit now covers three times the ground. Travel time, encounter spacing and
  // saved distances scale with it.
  const UNITS_PER_KM = 40 / 3, MAP_SCALE = 3, ENCOUNTER_MINUTES = 90;
  const normalizeTimeSpeed = value => timeSpeeds.includes(value) ? value : 1;
  // Regular speed: thirty-six minutes per second when travelling (the regions grew threefold,
  // so the clock runs three times faster and a crossing takes as long as before), one minute
  // per two seconds when idle. Terrain speeds remain in in-game km/h.
  const realTimeMinutes = (seconds, speed = 1, travelling = false) => Math.max(0, number(seconds, 0)) * normalizeTimeSpeed(speed) * (travelling ? 36 : 0.5);
  const validPoint = value => value && typeof value === "object" && Number.isFinite(value.x) && Number.isFinite(value.y);
  // Older saves can have the retired generic dragon in an encounter or quest.
  const currentEnemyName = name => name === "Lesser Dragon" ? "Red Dragon" : name;
  const point = p => ({ x: clamp(number(p?.x, 420), 20, 1580), y: clamp(number(p?.y, 490), 20, 980) });
  const regionalPoint = (value, fallback) => ({
    x: clamp(number(value?.x, fallback.x), 20, 1580),
    y: clamp(number(value?.y, fallback.y), 20, 980),
  });
  function pendingEncounter(value) {
    if (!value || typeof value !== "object" || !Array.isArray(value.enemies)) return null;
    const enemies = value.enemies.filter(name => typeof name === "string" && name.trim()).map(name => currentEnemyName(name.trim())).slice(0, 50);
    const zone = typeof value.zone === "string" && value.zone.trim() ? value.zone.trim() : "Wilderness";
    const objectiveKinds = ["hunt", "artifact", "great-hunt", "great-hunt-rematch", "search", "escort-ambush"];
    return enemies.length ? { zone, enemies, objective: objectiveKinds.includes(value.objective) ? value.objective : null } : null;
  }
  function questReward(quest) {
    const version = number(quest.reward?.rateVersion, 0);
    const savedReputation = Math.max(0, Math.floor(number(quest.reward?.reputation, 0)));
    const previousReputation = version >= 2 ? savedReputation : Math.round(savedReputation / 2);
    const greatHunt = quest.type === "great-hunt";
    return {
      silver: Math.max(0, Math.floor(number(quest.reward?.silver, 0))),
      reputation: previousReputation * (greatHunt && version < 3 ? 3 : 1),
      rateVersion: greatHunt || version >= 3 ? 3 : 2,
    };
  }
  // Enemies whose names were corrected; saved quests, rematches and encounters follow.
  const RENAMED_ENEMIES = { "Juila the Grand Forest Matriach": "Juila the Grand Forest Matriarch" };
  function renameEnemies(saved) {
    if (!saved || typeof saved !== "object") return saved;
    let text = JSON.stringify(saved);
    if (!Object.keys(RENAMED_ENEMIES).some(name => text.includes(name))) return saved;
    for (const [before, after] of Object.entries(RENAMED_ENEMIES)) text = text.split(JSON.stringify(before).slice(1, -1)).join(JSON.stringify(after).slice(1, -1));
    return JSON.parse(text);
  }
  function normalize(saved) {
    saved = renameEnemies(saved);
    const rawGuild = saved?.guild && typeof saved.guild === "object" ? saved.guild : {};
    const currentDay = day(saved?.minutes ?? 480);
    const text = (value, limit = 200) => typeof value === "string" && value.trim() ? value.trim().slice(0, limit) : null;
    const questDestination = value => value && typeof value === "object" && text(value.settlementId) && validPoint(value.point) ? {
      regionId: normalizeRegionId(value.regionId), settlementId: text(value.settlementId), name: text(value.name) || text(value.settlementId), point: point(value.point),
    } : null;
    // Quests of the Guild Rework (port task 2) carry version 2; Hunt and Search kept their kind.
    const QUEST_TYPES = ["hunt", "cull", "deliver", "search", "escort", "travel", "great-hunt", "clear"];
    const normalizeQuest = quest => quest && typeof quest === "object" && QUEST_TYPES.includes(quest.type) ? {
      ...quest,
      version: quest.version === 2 ? 2 : 1,
      title: text(quest.title),
      description: typeof quest.description === "string" ? quest.description.replaceAll("Lesser Dragon", "Red Dragon") : "",
      enemy: typeof quest.enemy === "string" && quest.enemy.trim() ? currentEnemyName(quest.enemy.trim()) : null,
      zone: typeof quest.zone === "string" && quest.zone.trim() ? quest.zone.trim() : null,
      regionId: normalizeRegionId(quest.regionId),
      originRegionId: quest.originRegionId ? normalizeRegionId(quest.originRegionId) : null,
      originCastleId: typeof quest.originCastleId === "string" && quest.originCastleId.trim() ? quest.originCastleId.trim() : "alderkeep",
      townId: typeof quest.townId === "string" && quest.townId.trim() ? quest.townId.trim() : null,
      uniqueId: typeof quest.uniqueId === "string" && quest.uniqueId.trim() ? quest.uniqueId.trim() : null,
      offeredAtLevel: Math.max(1, Math.floor(number(quest.offeredAtLevel, 1))),
      level: Math.max(1, Math.floor(number(quest.level, number(quest.offeredAtLevel, 1)))),
      offeredDay: Math.max(0, Math.floor(number(quest.offeredDay, 0))),
      progress: Math.max(0, Math.floor(number(quest.progress, 0))), complete: quest.complete === true,
      required: Math.max(1, Math.floor(number(quest.required, 1))),
      radius: Math.max(1, number(quest.radius, 40)),
      reward: questReward(quest),
      point: validPoint(quest.point) ? point(quest.point) : null,
      destination: questDestination(quest.destination),
      recommended: text(quest.recommended, 40),
      ambushes: Array.isArray(quest.ambushes) ? quest.ambushes.filter(entry => entry && Number.isFinite(entry.at)).slice(0, 2).map(entry => ({ at: Math.max(0, entry.at), done: entry.done === true })) : [],
      travelled: Math.max(0, number(quest.travelled, 0)),
      tripMinutes: Math.max(1, number(quest.tripMinutes, 1)),
      taken: quest.taken === true,
    } : null;
    // The standard quest and the Great Hunt are held apart (one of each at a time). Older saves kept a
    // Great Hunt in the quest slot; an active quest of a retired kind (Clear, the old Escort to a map
    // point, the old Travel within the region) is dropped with a note (G33).
    const RETIRED = { clear: "Clear", escort: "Escort", travel: "Travel" };
    let notice = text(rawGuild.notice, 400);
    let greatHunt = normalizeQuest(rawGuild.greatHunt);
    if (greatHunt?.type !== "great-hunt") greatHunt = null;
    // Ornn left the Great Hunts for Hearth Home (port task 3, R15): an active hunt for him is withdrawn with a note.
    const RETIRED_HUNTS = ["ornn-god-of-the-forge"];
    const retiredHunt = hunt => hunt?.type === "great-hunt" && RETIRED_HUNTS.includes(hunt.uniqueId);
    let quest = normalizeQuest(rawGuild.quest);
    if (quest?.type === "great-hunt") { greatHunt ||= quest; quest = null; }
    if (retiredHunt(greatHunt)) {
      notice = "Ornn, God of the Forge, is no longer a Great Hunt: your hunt for him was withdrawn (no reward, no cost). He now waits at Hearth Home, on Cinder Isle's volcano.";
      greatHunt = null;
    }
    else if (quest && quest.version !== 2) {
      if (RETIRED[quest.type]) {
        notice = `Your ${RETIRED[quest.type]} contract from before the Guild changed its contracts was withdrawn. It paid nothing and cost nothing; the bulletin board has new quests.`;
        quest = null;
      } else quest = { ...quest, version: 2 };
    }
    if (quest?.type === "clear") quest = null;
    if (quest) quest.taken = false;
    if (greatHunt) greatHunt.taken = false;
    // Each guild's board lasts for the in-game day it was drawn on (G7).
    const boards = {};
    if (rawGuild.boards && typeof rawGuild.boards === "object" && !Array.isArray(rawGuild.boards)) {
      for (const [guildId, board] of Object.entries(rawGuild.boards).slice(0, 40)) {
        if (!text(guildId) || !board || Math.floor(number(board.day, 0)) !== currentDay || !Array.isArray(board.offers)) continue;
        const offers = [];
        for (const offer of board.offers.map(normalizeQuest).filter(Boolean)) {
          if (offer.version !== 2 && offer.type !== "great-hunt" || offer.type === "clear") continue;
          if (!offers.some(candidate => candidate.type === offer.type && (offer.type !== "great-hunt" || candidate.uniqueId === offer.uniqueId))) offers.push(offer);
          if (offers.length === 12) break;
        }
        boards[guildId] = { day: currentDay, offers };
      }
    }
    const rawChallenge = rawGuild.artifactChallenge;
    const artifactChallenge = rawChallenge && typeof rawChallenge.enemy === "string" && rawChallenge.enemy.trim() && typeof rawChallenge.artifactId === "string" && rawChallenge.artifactId.trim() && typeof rawChallenge.zone === "string" && rawChallenge.zone.trim() && validPoint(rawChallenge.point) ? {
      enemy: currentEnemyName(rawChallenge.enemy.trim()), artifactId: rawChallenge.artifactId.trim(), regionId: normalizeRegionId(rawChallenge.regionId), zone: rawChallenge.zone.trim(), point: point(rawChallenge.point)
    } : null;
    const completedGreatHunts = Array.isArray(rawGuild.greatHuntsCompleted)
      ? [...new Set(rawGuild.greatHuntsCompleted.filter(id => typeof id === "string" && id.trim()).map(id => id.trim()))]
      : [];
    const normalizeRematch = rematch => rematch && typeof rematch.uniqueId === "string" && rematch.uniqueId.trim() && typeof rematch.enemy === "string" && rematch.enemy.trim() && typeof rematch.zone === "string" && rematch.zone.trim() && validPoint(rematch.point) ? {
      uniqueId: rematch.uniqueId.trim(), enemy: rematch.enemy.trim(), zone: rematch.zone.trim(), regionId: normalizeRegionId(rematch.regionId), point: point(rematch.point)
    } : null;
    // Migrate the former single marker without moving it. An explicit empty
    // list means deactivated; the compatibility field must not resurrect it.
    const rawRematches = Array.isArray(rawGuild.greatHuntRematches) ? rawGuild.greatHuntRematches : [rawGuild.greatHuntRematch];
    const greatHuntRematches = [];
    for (const rematch of rawRematches.map(normalizeRematch).filter(Boolean).filter(entry => !RETIRED_HUNTS.includes(entry.uniqueId))) {
      if (!greatHuntRematches.some(entry => entry.uniqueId === rematch.uniqueId)) greatHuntRematches.push(rematch);
      if (greatHuntRematches.length >= 100) break;
    }
    const greatHuntRematch = greatHuntRematches.at(-1) || null;
    const requestedAvoidance = ["hunt", "artifact", "great-hunt", "great-hunt-rematch"].includes(rawGuild.avoidObjective) ? rawGuild.avoidObjective : null;
    const avoidObjective = requestedAvoidance === "hunt" && quest?.type === "hunt" && !quest.complete
      ? "hunt"
      : requestedAvoidance === "artifact" && artifactChallenge
        ? "artifact"
        : requestedAvoidance === "great-hunt" && greatHunt && !greatHunt.complete
          ? "great-hunt"
        : null;
    const avoidedGreatHunts = [...new Set([
      ...(Array.isArray(rawGuild.avoidedGreatHunts) ? rawGuild.avoidedGreatHunts : []),
      ...(requestedAvoidance === "great-hunt-rematch" && greatHuntRematch ? [greatHuntRematch.uniqueId] : [])
    ])].filter(id => greatHuntRematches.some(rematch => rematch.uniqueId === id));
    const destination = validPoint(saved?.destination) ? point(saved.destination) : null;
    const route = destination && Array.isArray(saved?.route)
      ? saved.route.filter(validPoint).slice(0, 512).map(point)
      : [];
    if (destination && (!route.length || Math.hypot(route.at(-1).x - destination.x, route.at(-1).y - destination.y) > 1e-7)) route.push({ ...destination });
    const regionId = normalizeRegionId(saved?.regionId);
    const pendingRegionTransition = saved?.pendingRegionTransition && typeof saved.pendingRegionTransition === "object"
      && typeof saved.pendingRegionTransition.exitId === "string"
      && exitById(saved.pendingRegionTransition.exitId, regionId)
      ? { regionId, exitId: saved.pendingRegionTransition.exitId }
      : null;
    // Saves from before the regions grew counted kilometres on the smaller maps.
    const kmFactor = number(saved?.mapScale, 1) >= MAP_SCALE ? 1 : MAP_SCALE;
    return {
      mapScale: MAP_SCALE,
      minutes: clamp(number(saved?.minutes, 480), 0, 1000000000),
      timeSpeed: normalizeTimeSpeed(saved?.timeSpeed),
      distanceTraveledKm: Math.max(0, number(saved?.distanceTraveledKm, 0)) * kmFactor,
      lastRestKm: Math.max(0, number(saved?.lastRestKm, 0)) * kmFactor,
      encounterTravelMinutes: clamp(number(saved?.encounterTravelMinutes, 0), 0, ENCOUNTER_MINUTES - 0.000001),
      pendingEncounter: pendingEncounter(saved?.pendingEncounter),
      pendingRegionTransition,
      defeatedSiteBosses: [...new Set((Array.isArray(saved?.defeatedSiteBosses) ? saved.defeatedSiteBosses : []).filter(id => ["tankina-disciple-of-talos-level-44", "ornn-god-of-the-forge-unique-level-48"].includes(id)))],
      guild: {
        reputation: Math.max(0, Math.floor(number(rawGuild.reputation, 0))),
        completedQuests: Math.max(0, Math.floor(number(rawGuild.completedQuests, 0))),
        completedTravelQuests: Math.max(0, Math.floor(number(rawGuild.completedTravelQuests, 0))),
        quest,
        greatHunt,
        boards,
        notice,
        artifactOffers: Array.isArray(rawGuild.artifactOffers) ? [...new Set(rawGuild.artifactOffers.filter(id => typeof id === "string" && id.trim()).map(id => id.trim()))].slice(0, 3) : [],
        artifactChallenge,
        artifactNextDay: Math.max(0, Math.floor(number(rawGuild.artifactNextDay, 0))),
        greatHuntsCompleted: completedGreatHunts,
        greatHuntRematch,
        greatHuntRematches,
        avoidedGreatHunts,
        avoidObjective,
        information: root.GuildInformationRules?.normalize(rawGuild.information) || { offers: { class: [], item: [] }, revealed: { class: [], item: [] } },
      },
      regionId,
      position: regionalPoint(saved?.position, getRegion(regionId).start),
      destination,
      route,
    };
  }
  const day = minutes => Math.floor(Math.max(0, number(minutes, 0)) / 1440) + 1;
  const time = minutes => {
    const value = ((Math.floor(number(minutes, 0)) % 1440) + 1440) % 1440;
    return `${String(Math.floor(value / 60)).padStart(2, "0")}:${String(value % 60).padStart(2, "0")}`;
  };
  const distance = (a, b) => Math.hypot(b.x - a.x, b.y - a.y);
  let settlementAccessResolver = () => false;
  const setSettlementAccessResolver = resolver => { settlementAccessResolver = typeof resolver === "function" ? resolver : () => false; };
  const canAccessSettlement = place => Boolean(place && (!place.requiresClass || settlementAccessResolver(place.requiresClass)));
  const landmarkAt = (position, regionId = activeRegionId) => settlementsForRegion(regionId).find(place => distance(place, position) <= 32) || null;
  const siteAt = (position, regionId = activeRegionId) => (getRegion(regionId).sites || []).find(site => distance(site, position) <= (site.radius || 32)) || null;
  const settlementAt = (position, regionId = activeRegionId) => {
    const place = landmarkAt(position, regionId);
    return canAccessSettlement(place) ? place : null;
  };
  const line = (a, b, count = 12) => Array.from({ length: count + 1 }, (_, index) => ({
    x: a.x + (b.x - a.x) * index / count,
    y: a.y + (b.y - a.y) * index / count
  }));
  const quadratic = (a, control, b, count = 28) => Array.from({ length: count + 1 }, (_, index) => {
    const t = index / count, inverse = 1 - t;
    return { x: inverse * inverse * a.x + 2 * inverse * t * control.x + t * t * b.x, y: inverse * inverse * a.y + 2 * inverse * t * control.y + t * t * b.y };
  });
  const cubic = (a, first, second, b, count = 42) => Array.from({ length: count + 1 }, (_, index) => {
    const t = index / count, inverse = 1 - t;
    return { x: inverse ** 3 * a.x + 3 * inverse * inverse * t * first.x + 3 * inverse * t * t * second.x + t ** 3 * b.x, y: inverse ** 3 * a.y + 3 * inverse * inverse * t * first.y + 3 * inverse * t * t * second.y + t ** 3 * b.y };
  });
  const join = (...parts) => parts.flatMap((part, index) => index ? part.slice(1) : part);
  const roads = [
    join(quadratic({ x: 420, y: 490 }, { x: 540, y: 458 }, { x: 620, y: 392 }), quadratic({ x: 620, y: 392 }, { x: 700, y: 326 }, { x: 856, y: 350 }), line({ x: 856, y: 350 }, { x: 1110, y: 225 })),
    quadratic({ x: 420, y: 490 }, { x: 360, y: 631 }, { x: 295, y: 835 }),
    join(quadratic({ x: 420, y: 490 }, { x: 617, y: 671 }, { x: 845, y: 745 }), quadratic({ x: 845, y: 745 }, { x: 1073, y: 819 }, { x: 1190, y: 795 })),
    quadratic({ x: 1110, y: 225 }, { x: 1135, y: 100 }, { x: 1120, y: -20 }),
    quadratic({ x: 295, y: 835 }, { x: 150, y: 810 }, { x: -20, y: 850 })
  ];
  const river = join(
    cubic({ x: 860, y: -30 }, { x: 790, y: 130 }, { x: 980, y: 230 }, { x: 838, y: 350 }),
    cubic({ x: 838, y: 350 }, { x: 696, y: 470 }, { x: 694, y: 538 }, { x: 855, y: 645 }),
    cubic({ x: 855, y: 645 }, { x: 1016, y: 752 }, { x: 788, y: 858 }, { x: 916, y: 1040 })
  );
  const forests = [
    { id: "great-alderwood", x: 365, y: 213, rx: 270, ry: 155, deep: { rx: 125, ry: 72 } },
    { id: "foxglove-copse", x: 1345, y: 512, rx: 145, ry: 115 }
  ];
  const alder = {
    id: "alder-vale",
    name: "The Alder Vale",
    eyebrow: "THE ALDER VALE · REGIONAL MAP",
    title: "The roads <em>ahead</em>",
    start: { x: 420, y: 490 },
    settlements,
    roads,
    river,
    forests,
    exits: [
      { id: "north-road", name: "Road to Stoneveil", point: { x: 1120, y: 20 }, retreat: { x: 1120, y: 68 }, targetRegionId: "stoneveil-foothills", targetPoint: { x: 1120, y: 948 }, prompt: "Travel north to the Stoneveil Foothills?", destinationName: "Stoneveil Foothills" },
      { id: "west-road", name: "Road to the Verdant Reach", point: { x: 20, y: 850 }, retreat: { x: 68, y: 844 }, targetRegionId: "verdant-reach", targetPoint: { x: 1532, y: 500 }, prompt: "Travel west to the Verdant Reach?", destinationName: "The Verdant Reach" },
    ],
  };
  const stoneveil = root.WorldRegionData?.stoneveil || null;
  const highStoneveil = root.WorldRegionData?.highStoneveil || null;
  const stoneveilSummit = root.WorldRegionData?.stoneveilSummit || null;
  const verdantReach = root.WorldRegionData?.verdantReach || null;
  const sunwardCoast = root.WorldRegionData?.sunwardCoast || null;
  const cinderIsle = root.WorldRegionData?.cinderIsle || null;
  const stormwakeHills = root.WorldRegionData?.stormwakeHills || null;
  const gloamfenMarsh = root.WorldRegionData?.gloamfenMarsh || null;
  const verdigrisJungle = root.WorldRegionData?.verdigrisJungle || null;
  const mirrormereBasin = root.WorldRegionData?.mirrormereBasin || null;
  const saffronExpanse = root.WorldRegionData?.saffronExpanse || null;
  const deepDesert = root.WorldRegionData?.deepDesert || null;
  const crimsonDivide = root.WorldRegionData?.crimsonDivide || null;
  const bloodstoneCoast = root.WorldRegionData?.bloodstoneCoast || null;
  const ancientBattlefield = root.WorldRegionData?.ancientBattlefield || null;
  const duskpineForest = root.WorldRegionData?.duskpineForest || null;
  const regions = Object.freeze({
    [alder.id]: alder,
    ...(stoneveil ? { [stoneveil.id]: stoneveil } : {}),
    ...(highStoneveil ? { [highStoneveil.id]: highStoneveil } : {}),
    ...(stoneveilSummit ? { [stoneveilSummit.id]: stoneveilSummit } : {}),
    ...(verdantReach ? { [verdantReach.id]: verdantReach } : {}),
    ...(sunwardCoast ? { [sunwardCoast.id]: sunwardCoast } : {}),
    ...(cinderIsle ? { [cinderIsle.id]: cinderIsle } : {}),
    ...(stormwakeHills ? { [stormwakeHills.id]: stormwakeHills } : {}),
    ...(gloamfenMarsh ? { [gloamfenMarsh.id]: gloamfenMarsh } : {}),
    ...(verdigrisJungle ? { [verdigrisJungle.id]: verdigrisJungle } : {}),
    ...(mirrormereBasin ? { [mirrormereBasin.id]: mirrormereBasin } : {}),
    ...(saffronExpanse ? { [saffronExpanse.id]: saffronExpanse } : {}),
    ...(deepDesert ? { [deepDesert.id]: deepDesert } : {}),
    ...(crimsonDivide ? { [crimsonDivide.id]: crimsonDivide } : {}),
    ...(bloodstoneCoast ? { [bloodstoneCoast.id]: bloodstoneCoast } : {}),
    ...(ancientBattlefield ? { [ancientBattlefield.id]: ancientBattlefield } : {}),
    ...(duskpineForest ? { [duskpineForest.id]: duskpineForest } : {}),
  });
  let activeRegionId = alder.id;
  function normalizeRegionId(regionId) { return typeof regionId === "string" && regions[regionId] ? regionId : alder.id; }
  function getRegion(regionId = activeRegionId) { return regions[normalizeRegionId(regionId)]; }
  function setActiveRegion(regionId) { activeRegionId = normalizeRegionId(regionId); return getRegion(); }
  function activeRegion() { return getRegion(activeRegionId); }
  function settlementsForRegion(regionId = activeRegionId) { return getRegion(regionId).settlements || []; }
  function nearestSettlement(position, regionId = activeRegionId) {
    const region = getRegion(regionId), origin = regionalPoint(position, region.start);
    return region.settlements.filter(canAccessSettlement).reduce((nearest, settlement) => !nearest || distance(origin, settlement) < distance(origin, nearest) ? settlement : nearest, null);
  }
  const cachedTerrainSegments = new Map();
  function terrainSegments(regionId) {
    const id = normalizeRegionId(regionId);
    if (cachedTerrainSegments.has(id)) return cachedTerrainSegments.get(id);
    const region = getRegion(id);
    const segments = paths => paths.flatMap(path => path.slice(1).map((end, index) => {
      const start = path[index], dx = end.x - start.x, dy = end.y - start.y;
      return { x: start.x, y: start.y, dx, dy, lengthSquared: dx * dx + dy * dy,
        minX: Math.min(start.x, end.x), maxX: Math.max(start.x, end.x),
        minY: Math.min(start.y, end.y), maxY: Math.max(start.y, end.y) };
    }));
    const result = { roads: segments(region.roads || []), river: segments(region.river ? [region.river] : []) };
    cachedTerrainSegments.set(id, result);
    return result;
  }
  function nearSegments(position, segments, radius) {
    const limit = radius * radius;
    for (const segment of segments) {
      if (position.x < segment.minX - radius || position.x > segment.maxX + radius || position.y < segment.minY - radius || position.y > segment.maxY + radius) continue;
      const t = segment.lengthSquared ? clamp(((position.x - segment.x) * segment.dx + (position.y - segment.y) * segment.dy) / segment.lengthSquared, 0, 1) : 0;
      const dx = position.x - (segment.x + segment.dx * t), dy = position.y - (segment.y + segment.dy * t);
      if (dx * dx + dy * dy <= limit) return true;
    }
    return false;
  }
  function terrainAt(position, regionId = activeRegionId) {
    if (!isLand(position, regionId)) return "sea";
    const id = normalizeRegionId(regionId), segments = terrainSegments(id);
    if (nearSegments(position, segments.roads, id === "alder-vale" ? 13 : 16)) return "road";
    if (nearSegments(position, segments.river, 27)) return "river";
    return "land";
  }
  const insideEllipse = (position, area, rx = area.rx, ry = area.ry) => ((position.x - area.x) / rx) ** 2 + ((position.y - area.y) / ry) ** 2 <= 1;
  // A bridge over a lake (port task 3: the Institute of Magic's) is land as wide as its road.
  function onBridge(position, region) {
    for (const path of region.bridges || []) for (let index = 1; index < path.length; index++) {
      const a = path[index - 1], b = path[index], dx = b.x - a.x, dy = b.y - a.y, length = dx * dx + dy * dy || 1;
      const t = Math.max(0, Math.min(1, ((position.x - a.x) * dx + (position.y - a.y) * dy) / length));
      if (Math.hypot(position.x - (a.x + dx * t), position.y - (a.y + dy * t)) <= 12) return true;
    }
    return false;
  }
  function isLand(position, regionId = activeRegionId) {
    const region = getRegion(regionId);
    if (region.lake && insideEllipse(position, region.lake)) return Boolean(region.island && insideEllipse(position, region.island) || onBridge(position, region));
    const shape = region.landShape;
    if (!shape) return true;
    if (shape.type === "coast") return position.x <= shape.shoreX;
    if (shape.type === "south-coast") return position.y <= shape.shoreY;
    if (shape.type === "ellipse") return insideEllipse(position, shape);
    return true;
  }
  function zoneAt(position, regionId = activeRegionId) {
    const region = getRegion(regionId);
    if (!isLand(position, region.id)) return region.lake ? "Open Water" : "Open Sea";
    const landmark = landmarkAt(position, region.id);
    if (landmark && !canAccessSettlement(landmark)) return "Vampire Citadel";
    if (settlementAt(position, region.id)) return "Town";
    const site = siteAt(position, region.id);
    if (site) return site.name;
    const terrain = terrainAt(position, region.id);
    if (terrain === "road") return "Road";
    if (region.id === "stoneveil-foothills") {
      let zone = terrain === "river"
        ? "River"
        : position.y <= number(region.mountainfoot?.y, 315)
          ? "Mountainfoot"
          : (region.hillAreas || []).some(area => insideEllipse(position, area)) ? "Hill-Plains" : "Plains";
      if (region.settlements.some(place => distance(place, position) <= 120)) zone += " - near Town";
      return zone;
    }
    if (region.id === "high-stoneveil") {
      if (position.y > 745) return "Lower Slopes";
      if (position.x < 430 || position.x > 1190) return position.y < 335 && position.x > 1190 ? "Dragonspine" : "Cliffside";
      if (position.y < 295) return "Frozen Heights";
      return "High Pass";
    }
    if (region.id === "stoneveil-summit") {
      if (position.y > 735) return "Windcut Ascent";
      if (position.x < 440) return "Giant Steps";
      if (position.x > 1160) return "Dragon Eyries";
      if (position.y < 330) return "Frozen Crown";
      return "Cloudbreak Ridge";
    }
    if (region.id === "verdant-reach") {
      if (insideEllipse(position, region.forestLayers.deep)) return "Deep Forest";
      if (insideEllipse(position, region.forestLayers.forest)) return "Forest";
      if (insideEllipse(position, region.forestLayers.outer)) return "Outer Forest";
      return "Plains";
    }
    if (region.id === "stormwake-hills") {
      if (insideEllipse(position, { x: 1160, y: 680, rx: 230, ry: 145 })) return "Floodplain";
      if (insideEllipse(position, region.stormcrest)) return "Stormcrest";
      if (region.hillAreas.some(area => insideEllipse(position, area))) return "Thunder Hills";
      return "Rainwashed Downs";
    }
    if (region.id === "gloamfen-marsh") {
      if (insideEllipse(position, region.rockyRise)) return "Rocky Rise";
      if (insideEllipse(position, region.deepMire)) return "Deep Mire";
      if (insideEllipse(position, region.reedwater)) return "Reedwater";
      return "Open Mire";
    }
    if (region.id === "verdigris-jungle") {
      if (insideEllipse(position, region.oldGrowth)) return "Old Growth";
      if (insideEllipse(position, region.highCanopy)) return "High Canopy";
      if (insideEllipse(position, region.denseCanopy)) return "Dense Jungle";
      if (insideEllipse(position, region.outerCanopy)) return "Outer Jungle";
      return "Jungle Verge";
    }
    if (region.id === "mirrormere-basin") {
      if (position.y >= 820) return "Desert Fringe";
      if (insideEllipse(position, region.marsh)) return "Reed Marsh";
      if (position.y >= 680) return "Dry Steppe";
      if (position.x < 425) return "Windcut Bluffs";
      return "Lake Shore";
    }
    if (region.id === "saffron-expanse") {
      if (insideEllipse(position, region.oasis)) return "Oasis Grove";
      if (insideEllipse(position, region.saltPans)) return "Salt Pans";
      if (insideEllipse(position, region.glassDunes)) return "Glass Dunes";
      if (position.y >= 590 || position.x >= 1080) return "Shifting Dunes";
      return "Sunburnt Flats";
    }
    if (region.id === "deep-desert") {
      if (insideEllipse(position, region.pyramid)) return "Pyramid Sanctum";
      if (insideEllipse(position, region.necropolis)) return "Buried Necropolis";
      if (insideEllipse(position, region.obsidianFlats)) return "Obsidian Flats";
      if (position.x >= 600 || position.y >= 630) return "Howling Dunes";
      return "Outer Wastes";
    }
    if (region.id === "crimson-divide") {
      if (position.y < region.crimsonBoundary) {
        if (insideEllipse(position, region.escarpment)) return "Sunscorched Escarpment";
        return position.y < 250 ? "Caravan Dunes" : "Open Desert";
      }
      if (insideEllipse(position, region.bloodglass)) return "Bloodglass Flats";
      return position.y >= 820 ? "Heart of the Red Waste" : "Crimson Dunes";
    }
    if (region.id === "bloodstone-coast") {
      if (insideEllipse(position, region.bloodShore)) return "Blood Coast";
      if (region.rockFields.some(area => insideEllipse(position, area))) return position.x < 720 ? "Western Crags" : "Giant Rock Spires";
      return position.y < 430 ? "Redwind Dunes" : "Sanguine Wastes";
    }
    if (region.id === "ancient-battlefield") {
      if (insideEllipse(position, region.trenches)) return "Bloodied Trenches";
      if (insideEllipse(position, region.bonePits)) return "Bone Pits";
      if (insideEllipse(position, region.massGraves)) return "Mass Graves";
      if (position.x > 1170) return "Scavenger Verge";
      if (position.x < 420) return "Silent Westfield";
      return "Shattered Causeway";
    }
    if (region.id === "duskpine-forest") {
      if (insideEllipse(position, region.frostBog)) return "Frostbound Bog";
      if (insideEllipse(position, region.shadowHollow)) return "Shadowroot Hollow";
      if (insideEllipse(position, region.deepBoreal)) return "Deep Boreal";
      if (position.x > 1130) return "Frostpine Fringe";
      return "Blackpine Woods";
    }
    if (region.id === "sunward-coast") {
      if (insideEllipse(position, region.saltMarsh)) return "Salt Marsh";
      if (position.x >= 1150) return "Beach";
      if (position.x >= 950) return "Dunes";
      if (position.x >= 590) return "Windswept Plains";
      return "Coastal Plains";
    }
    if (region.id === "cinder-isle") {
      const volcano = region.volcano;
      if (insideEllipse(position, volcano, 100, 85)) return "Caldera";
      if (insideEllipse(position, volcano, 225, 180)) return "Lava Fields";
      if (insideEllipse(position, volcano)) return "Ashen Slope";
      const island = region.landShape;
      return ((position.x - island.x) / island.rx) ** 2 + ((position.y - island.y) / island.ry) ** 2 > .65 ? "Black Sand Shore" : "Ember Scrub";
    }
    if (terrain === "river") return "River";
    const greatForest = forests[0];
    if (insideEllipse(position, greatForest, greatForest.deep.rx, greatForest.deep.ry)) return "Deep Forest";
    if (forests.some(forest => insideEllipse(position, forest))) return "Forest";
    if (settlements.some(place => distance(place, position) <= 110)) return "Plains - near Town";
    return "Plains";
  }
  function speedAt(position, bonus = 0, regionId = activeRegionId) {
    const terrain = terrainAt(position, regionId);
    const flat = typeof bonus === "object" ? number(bonus?.all, 0) : number(bonus, 0);
    const river = typeof bonus === "object" && terrain === "river" ? number(bonus?.river, 0) : 0;
    const id = normalizeRegionId(regionId), zone = terrain === "land" && ["sunward-coast", "cinder-isle", "stormwake-hills", "gloamfen-marsh", "verdigris-jungle", "mirrormere-basin", "saffron-expanse", "deep-desert", "crimson-divide", "bloodstone-coast", "ancient-battlefield", "duskpine-forest"].includes(id) ? zoneAt(position, id) : null;
    const base = terrain === "road" ? 7 : terrain === "river" ? 2 : terrain === "sea" ? .1
      : id === "stoneveil-summit" ? 3 : id === "high-stoneveil" ? 4
        : id === "gloamfen-marsh" ? (zone === "Deep Mire" || zone === "Reedwater" ? 2 : zone === "Rocky Rise" ? 4 : 3)
        : id === "verdigris-jungle" ? (zone === "Old Growth" ? 2 : zone === "Dense Jungle" || zone === "High Canopy" ? 3 : 4)
        : zone === "Salt Marsh" || zone === "Lava Fields" || zone === "Salt Pans" || zone === "Obsidian Flats" || zone === "Pyramid Sanctum" || zone === "Sunscorched Escarpment" || zone === "Bloodglass Flats" || zone === "Giant Rock Spires" || zone === "Western Crags" || zone === "Bloodied Trenches" || zone === "Frostbound Bog" ? 3
        : zone === "Caldera" ? 2 : zone === "Dunes" || zone === "Beach" || zone === "Ashen Slope" || zone === "Thunder Hills" || zone === "Windcut Bluffs" || zone === "Dry Steppe" || zone === "Desert Fringe" ? 4
          : zone === "Floodplain" || zone === "Reed Marsh" || zone === "Stormcrest" ? 3
            : zone === "Shifting Dunes" || zone === "Glass Dunes" || zone === "Howling Dunes" || zone === "Buried Necropolis" || zone === "Caravan Dunes" || zone === "Crimson Dunes" || zone === "Heart of the Red Waste" || zone === "Mass Graves" || zone === "Bone Pits" || zone === "Deep Boreal" || zone === "Shadowroot Hollow" ? 4 : 5;
    return Math.max(0.1, base + Math.max(0, flat) + Math.max(0, river));
  }
  function travelMinutes(a, b, speedBonus = 0, regionId = activeRegionId) {
    const totalDistance = distance(a, b);
    if (!totalDistance) return 0;
    const dx = (b.x - a.x) / totalDistance, dy = (b.y - a.y) / totalDistance;
    let travelled = 0, minutes = 0;
    while (travelled < totalDistance - 1e-8) {
      const segment = Math.min(5, totalDistance - travelled);
      const midpoint = { x: a.x + dx * (travelled + segment / 2), y: a.y + dy * (travelled + segment / 2) };
      if (getRegion(regionId).lake && !isLand(midpoint, regionId)) return Infinity;
      minutes += segment / UNITS_PER_KM / speedAt(midpoint, speedBonus, regionId) * 60;
      travelled += segment;
    }
    return minutes;
  }
  const cachedRoadGraphs = new Map();
  function roadGraph(regionId = activeRegionId) {
    const normalizedRegionId = normalizeRegionId(regionId);
    if (cachedRoadGraphs.has(normalizedRegionId)) return cachedRoadGraphs.get(normalizedRegionId);
    const nodes = [], edges = [], nodeIds = new Map();
    const nodeId = rawPoint => {
      const roadPoint = point(rawPoint), key = roadPoint.x.toFixed(5) + ":" + roadPoint.y.toFixed(5);
      if (nodeIds.has(key)) return nodeIds.get(key);
      const index = nodes.length;
      nodes.push(roadPoint); edges.push([]); nodeIds.set(key, index);
      return index;
    };
    const connect = (first, second) => {
      if (first === second) return;
      const length = distance(nodes[first], nodes[second]);
      if (!edges[first].some(edge => edge.to === second)) edges[first].push({ to: second, length });
      if (!edges[second].some(edge => edge.to === first)) edges[second].push({ to: first, length });
    };
    getRegion(normalizedRegionId).roads.forEach(path => {
      let previous = null;
      path.forEach(rawPoint => {
        const current = nodeId(rawPoint);
        if (previous !== null) connect(previous, current);
        previous = current;
      });
    });
    const graph = { nodes, edges };
    cachedRoadGraphs.set(normalizedRegionId, graph);
    return graph;
  }
  // Compare the straight trip with every useful road entrance and exit. Dijkstra
  // minimizes travel minutes, not distance: roads are faster, while an off-road
  // river crossing is slower. The maximum-speed bound safely skips road nodes
  // that cannot beat the direct trip even under ideal conditions.
  function routeBetween(startValue, endValue, speedBonus = 0, regionId = activeRegionId) {
    const start = point(startValue), end = point(endValue);
    const directRoute = [{ ...end }];
    if (distance(start, end) < 1e-7) return directRoute;
    const directMinutes = travelMinutes(start, end, speedBonus, regionId);
    const graph = roadGraph(regionId), count = graph.nodes.length;
    if (!count) return directRoute;
    const flatSpeedBonus = typeof speedBonus === "object" ? number(speedBonus?.all, 0) : number(speedBonus, 0);
    const riverSpeedBonus = typeof speedBonus === "object" ? number(speedBonus?.river, 0) : 0;
    const fastestMinutesPerUnit = 1.5 / Math.max(0.1, Math.max(7 + Math.max(0, flatSpeedBonus), 2 + Math.max(0, flatSpeedBonus) + Math.max(0, riverSpeedBonus)));
    const lowerBound = (first, second) => distance(first, second) * fastestMinutesPerUnit;
    const useful = graph.nodes.map(node => lowerBound(start, node) + lowerBound(node, end) < directMinutes - 1e-7);
    const costs = Array(count).fill(Infinity), previous = Array(count).fill(-1), visited = new Set();
    graph.nodes.forEach((node, index) => {
      if (!useful[index]) return;
      const entrance = travelMinutes(start, node, speedBonus, regionId);
      if (entrance + lowerBound(node, end) < directMinutes - 1e-7) costs[index] = entrance;
    });
    let best = directMinutes, exit = -1;
    while (visited.size < count) {
      let current = -1, currentCost = Infinity;
      for (let index = 0; index < count; index += 1) {
        if (!visited.has(index) && costs[index] < currentCost) { current = index; currentCost = costs[index]; }
      }
      if (current < 0 || currentCost >= best) break;
      visited.add(current);
      if (currentCost + lowerBound(graph.nodes[current], end) >= best - 1e-7) continue;
      const finish = currentCost + travelMinutes(graph.nodes[current], end, speedBonus, regionId);
      if (finish < best - 1e-7) { best = finish; exit = current; }
      graph.edges[current].forEach(edge => {
        if (!useful[edge.to] || visited.has(edge.to)) return;
        const edgeMinutes = travelMinutes(graph.nodes[current], graph.nodes[edge.to], speedBonus, regionId);
        const nextCost = currentCost + edgeMinutes;
        if (nextCost + lowerBound(graph.nodes[edge.to], end) < best - 1e-7 && nextCost < costs[edge.to]) {
          costs[edge.to] = nextCost;
          previous[edge.to] = current;
        }
      });
    }
    if (exit < 0) return directRoute;
    const nodePath = [];
    for (let current = exit; current >= 0; current = previous[current]) nodePath.push(graph.nodes[current]);
    nodePath.reverse();
    const route = [...nodePath, end].reduce((result, routePoint) => {
      if (!result.length || distance(result.at(-1), routePoint) > 1e-7) result.push({ ...routePoint });
      return result;
    }, []);
    while (route.length > 1 && distance(start, route[0]) < 1e-7) route.shift();
    return route.length && routeTravelMinutes(start, route, speedBonus, regionId) < directMinutes - 1e-7 ? route : directRoute;
  }
  function routeDistance(startValue, routeValue) {
    let cursor = point(startValue), total = 0;
    for (const rawPoint of Array.isArray(routeValue) ? routeValue : []) {
      if (!validPoint(rawPoint)) continue;
      const next = point(rawPoint);
      total += distance(cursor, next); cursor = next;
    }
    return total;
  }
  function routeTravelMinutes(startValue, routeValue, speedBonus = 0, regionId = activeRegionId) {
    let cursor = point(startValue), total = 0;
    for (const rawPoint of Array.isArray(routeValue) ? routeValue : []) {
      if (!validPoint(rawPoint)) continue;
      const next = point(rawPoint);
      total += travelMinutes(cursor, next, speedBonus, regionId); cursor = next;
    }
    return total;
  }
  function prepareRoute(state) {
    if (!validPoint(state.destination)) { state.destination = null; state.route = []; return []; }
    state.destination = point(state.destination);
    let route = Array.isArray(state.route) ? state.route.filter(validPoint).slice(0, 512).map(point) : [];
    if (!route.length || distance(route.at(-1), state.destination) > 1e-7) route = [{ ...state.destination }];
    while (route.length && distance(state.position, route[0]) < 1e-7) route.shift();
    state.route = route;
    return route;
  }
  function stepToward(state, target, available, speedBonus) {
    const origin = { ...state.position }, totalDistance = distance(origin, target);
    if (!totalDistance) return { elapsed: 0, travelled: 0, reached: true };
    const dx = (target.x - origin.x) / totalDistance, dy = (target.y - origin.y) / totalDistance;
    let travelled = 0, elapsed = 0;
    while (available > 1e-8 && travelled < totalDistance - 1e-8) {
      const segment = Math.min(5, totalDistance - travelled);
      const midpoint = { x: origin.x + dx * (travelled + segment / 2), y: origin.y + dy * (travelled + segment / 2) };
      const speed = speedAt(midpoint, speedBonus, state.regionId), segmentMinutes = segment / UNITS_PER_KM / speed * 60;
      const used = Math.min(available, segmentMinutes);
      travelled += segment * used / segmentMinutes;
      elapsed += used; available -= used;
      if (used < segmentMinutes) break;
    }
    state.position.x = origin.x + dx * travelled;
    state.position.y = origin.y + dy * travelled;
    return { elapsed, travelled, reached: totalDistance - travelled < 1e-7 };
  }
  function step(state, minutes, speedBonus = 0) {
    if (!state.destination) { state.route = []; return { elapsed: 0, arrived: false }; }
    let route = prepareRoute(state), available = Math.max(0, number(minutes, 0)), elapsed = 0, travelled = 0;
    while (route.length && available > 1e-8) {
      const leg = stepToward(state, route[0], available, speedBonus);
      elapsed += leg.elapsed; travelled += leg.travelled; available -= leg.elapsed;
      if (!leg.reached) break;
      state.position = { ...route[0] }; route.shift();
    }
    state.distanceTraveledKm = Math.max(0, number(state.distanceTraveledKm, 0)) + travelled / UNITS_PER_KM;
    const arrived = route.length === 0 && distance(state.position, state.destination) < 1e-7;
    if (arrived) { state.position = { ...state.destination }; state.destination = null; state.route = []; }
    return { elapsed, arrived };
  }
  function lighting(minutes) {
    const hour = (((number(minutes, 0) % 1440) + 1440) % 1440) / 60;
    if (hour >= 6 && hour < 18) return { darkness: 0, warmth: 0, label: "Daylight" };
    if (hour >= 18 && hour < 20) {
      const progress = (hour - 18) / 2;
      return { darkness: progress * 0.66, warmth: Math.sin(progress * Math.PI) * 0.24, label: "Dusk" };
    }
    if (hour >= 5 && hour < 6) return { darkness: (6 - hour) * 0.66, warmth: 0.08, label: "Dawn" };
    return { darkness: 0.66, warmth: 0, label: "Night" };
  }
  function exitById(exitId, regionId = activeRegionId) { return (getRegion(regionId).exits || []).find(exit => exit.id === exitId) || null; }
  function exitAt(position, regionId = activeRegionId) { return (getRegion(regionId).exits || []).find(exit => distance(exit.point, position) <= 34) || null; }
  const api = { UNITS_PER_KM, MAP_SCALE, ENCOUNTER_MINUTES, RENAMED_ENEMIES, settlements, forests, regions, normalizeRegionId, getRegion, setActiveRegion, activeRegion, settlementsForRegion, nearestSettlement, setSettlementAccessResolver, canAccessSettlement, landmarkAt, siteAt, normalize, timeSpeeds, normalizeTimeSpeed, realTimeMinutes, day, time, point, distance, settlementAt, isLand, terrainAt, zoneAt, speedAt, travelMinutes, routeBetween, routeDistance, routeTravelMinutes, exitAt, exitById, step, lighting };
  root.WorldRules = api;
  if (typeof module !== "undefined" && module.exports) module.exports = api;
})(globalThis);

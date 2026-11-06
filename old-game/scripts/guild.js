/* Adventurers Guild contracts and challenge state are stored with the world map. */
(() => {
  const $ = id => document.getElementById(id);
  const modal = $("guild-modal");
  const guild = () => worldState.guild;
  const rematches = () => guild().greatHuntRematches;
  function syncRematchCompatibility() { guild().greatHuntRematch = rematches().at(-1) || null; }
  function avoidRematch(id = null) {
    const objective = id ? null : rematchAt(worldState.position, true);
    const rematchId = id || (objective?.kind === "great-hunt-rematch" ? objective.uniqueId : null);
    const rematch = rematchId ? rematches().find(entry => entry.uniqueId === rematchId) : rematches().find(entry => !guild().avoidedGreatHunts.includes(entry.uniqueId) && entry.regionId === worldState.regionId && WorldRules.distance(worldState.position, entry.point) <= 45);
    if (rematch && !guild().avoidedGreatHunts.includes(rematch.uniqueId)) guild().avoidedGreatHunts.push(rematch.uniqueId);
  }
  const ARTIFACT_REPUTATION_COST = 70;
  const INFORMATION_REPUTATION_COST = GuildInformationRules.cost;
  let informationView = null, informationLoading = false, informationRequest = 0, informationEnemies = [];
  let informationEnemyLocations = new Map();
  const castle = () => {
    const place = WorldRules.settlementAt(worldState.position, worldState.regionId);
    return place?.kind === "Castle city" || place?.guildServices?.includes("quest") ? place : null;
  };
  const pick = entries => entries[Math.floor(Math.random() * entries.length)];
  function shuffle(entries) {
    const copy = [...entries];
    for (let index = copy.length - 1; index > 0; index--) {
      const other = Math.floor(Math.random() * (index + 1));
      [copy[index], copy[other]] = [copy[other], copy[index]];
    }
    return copy;
  }
  const regionalEnemies = (zone, regionId = "alder-vale") => EncounterRules.enemiesForZone(zone, regionId);
  const zones = [
    { name: "Plains - near Town", points: [{ x: 1110, y: 305 }, { x: 1190, y: 700 }, { x: 390, y: 795 }], enemies: regionalEnemies("Plains - near Town") },
    { name: "Plains", points: [{ x: 1000, y: 500 }, { x: 1500, y: 800 }, { x: 1050, y: 570 }], enemies: regionalEnemies("Plains") },
    { name: "River", points: [{ x: 820, y: 590 }], enemies: regionalEnemies("River") },
    { name: "Forest", points: [{ x: 560, y: 180 }, { x: 1345, y: 512 }, { x: 180, y: 210 }], enemies: regionalEnemies("Forest") },
    { name: "Deep Forest", points: [{ x: 365, y: 213 }, { x: 415, y: 190 }], enemies: regionalEnemies("Deep Forest") },
  ];
  const stoneveilGreatHuntZones = [
    { name: "Plains", points: [{ x: 400, y: 880 }, { x: 1450, y: 850 }] },
    { name: "Hill-Plains", points: [{ x: 280, y: 580 }, { x: 1360, y: 330 }] },
    { name: "Mountainfoot", points: [{ x: 300, y: 170 }, { x: 1430, y: 160 }] },
  ];
  const regionalZones = {
    "alder-vale": zones,
    "stoneveil-foothills": stoneveilGreatHuntZones.map(zone => ({ ...zone, enemies: regionalEnemies(zone.name, "stoneveil-foothills") })),
    "high-stoneveil": [
      { name: "Lower Slopes", points: [{ x: 350, y: 865 }, { x: 1250, y: 870 }] },
      { name: "Cliffside", points: [{ x: 245, y: 565 }, { x: 1360, y: 575 }] },
      { name: "High Pass", points: [{ x: 950, y: 445 }, { x: 480, y: 540 }] },
      { name: "Frozen Heights", points: [{ x: 455, y: 205 }, { x: 1060, y: 185 }] },
      { name: "Dragonspine", points: [{ x: 1370, y: 180 }, { x: 1450, y: 245 }] },
    ].map(zone => ({ ...zone, enemies: regionalEnemies(zone.name, "high-stoneveil") })),
    "verdant-reach": [
      { name: "Plains", points: [{ x: 1510, y: 850 }, { x: 1470, y: 150 }] },
      { name: "Outer Forest", points: [{ x: 1140, y: 250 }, { x: 1000, y: 755 }] },
      { name: "Forest", points: [{ x: 750, y: 600 }, { x: 360, y: 610 }] },
      { name: "Deep Forest", points: [{ x: 475, y: 325 }, { x: 355, y: 280 }] },
    ].map(zone => ({ ...zone, enemies: regionalEnemies(zone.name, "verdant-reach") })),
    "sunward-coast": [
      { name: "Coastal Plains", points: [{ x: 300, y: 300 }, { x: 315, y: 755 }] },
      { name: "Windswept Plains", points: [{ x: 710, y: 700 }, { x: 780, y: 395 }] },
      { name: "Salt Marsh", points: [{ x: 930, y: 850 }, { x: 850, y: 865 }] },
      { name: "Dunes", points: [{ x: 1080, y: 295 }, { x: 1050, y: 440 }] },
      { name: "Beach", points: [{ x: 1215, y: 370 }, { x: 1200, y: 875 }] },
    ].map(zone => ({ ...zone, enemies: regionalEnemies(zone.name, "sunward-coast") })),
    "cinder-isle": [
      { name: "Black Sand Shore", points: [{ x: 380, y: 790 }, { x: 400, y: 235 }] },
      { name: "Ember Scrub", points: [{ x: 555, y: 410 }, { x: 560, y: 750 }] },
      { name: "Ashen Slope", points: [{ x: 800, y: 300 }, { x: 790, y: 510 }] },
      { name: "Lava Fields", points: [{ x: 1130, y: 600 }, { x: 1180, y: 410 }] },
      { name: "Caldera", points: [{ x: 1030, y: 455 }] },
    ].map(zone => ({ ...zone, enemies: regionalEnemies(zone.name, "cinder-isle") })),
    "stormwake-hills": [
      { name: "Rainwashed Downs", points: [{ x: 1450, y: 790 }, { x: 1040, y: 195 }] },
      { name: "Floodplain", points: [{ x: 1190, y: 740 }, { x: 1370, y: 645 }] },
      { name: "Thunder Hills", points: [{ x: 580, y: 740 }, { x: 1390, y: 315 }] },
      { name: "Stormcrest", points: [{ x: 380, y: 275 }, { x: 280, y: 235 }] },
    ].map(zone => ({ ...zone, enemies: regionalEnemies(zone.name, "stormwake-hills") })),
    "gloamfen-marsh": [
      { name: "Open Mire", points: [{ x: 1300, y: 850 }, { x: 1400, y: 450 }] },
      { name: "Reedwater", points: [{ x: 1310, y: 245 }] },
      { name: "Deep Mire", points: [{ x: 790, y: 580 }] },
      { name: "Rocky Rise", points: [{ x: 310, y: 555 }] },
      { name: "Abyssal Rift", points: [{ x: 380, y: 885 }] },
    ].map(zone => ({ ...zone, enemies: regionalEnemies(zone.name, "gloamfen-marsh") })),
    "verdigris-jungle": [
      { name: "Jungle Verge", points: [{ x: 1550, y: 920 }] },
      { name: "Outer Jungle", points: [{ x: 1180, y: 720 }] },
      { name: "Dense Jungle", points: [{ x: 600, y: 650 }] },
      { name: "High Canopy", points: [{ x: 1300, y: 300 }] },
      { name: "Old Growth", points: [{ x: 475, y: 290 }] },
    ].map(zone => ({ ...zone, enemies: regionalEnemies(zone.name, "verdigris-jungle") })),
    "mirrormere-basin": [
      { name: "Lake Shore", points: [{ x: 1430, y: 280 }, { x: 1400, y: 400 }] },
      { name: "Reed Marsh", points: [{ x: 1285, y: 700 }, { x: 1390, y: 650 }] },
      { name: "Windcut Bluffs", points: [{ x: 170, y: 320 }, { x: 185, y: 595 }] },
      { name: "Dry Steppe", points: [{ x: 360, y: 780 }, { x: 1500, y: 780 }] },
      { name: "Desert Fringe", points: [{ x: 1400, y: 905 }, { x: 380, y: 920 }] },
    ].map(zone => ({ ...zone, enemies: regionalEnemies(zone.name, "mirrormere-basin") })),
    "saffron-expanse": [
      { name: "Oasis Grove", points: [{ x: 425, y: 355 }, { x: 330, y: 390 }] },
      { name: "Sunburnt Flats", points: [{ x: 360, y: 135 }, { x: 820, y: 420 }] },
      { name: "Salt Pans", points: [{ x: 1260, y: 295 }, { x: 1360, y: 330 }] },
      { name: "Shifting Dunes", points: [{ x: 770, y: 770 }, { x: 420, y: 830 }] },
      { name: "Glass Dunes", points: [{ x: 1340, y: 870 }, { x: 1430, y: 760 }] },
    ].map(zone => ({ ...zone, enemies: regionalEnemies(zone.name, "saffron-expanse") })),
    "deep-desert": [
      { name: "Outer Wastes", points: [{ x: 220, y: 680 }, { x: 370, y: 470 }] },
      { name: "Howling Dunes", points: [{ x: 1160, y: 240 }, { x: 675, y: 825 }] },
      { name: "Obsidian Flats", points: [{ x: 1320, y: 700 }, { x: 1440, y: 650 }] },
      { name: "Buried Necropolis", points: [{ x: 820, y: 710 }, { x: 605, y: 610 }] },
      { name: "Pyramid Sanctum", points: [{ x: 820, y: 500 }, { x: 850, y: 430 }] },
    ].map(zone => ({ ...zone, enemies: regionalEnemies(zone.name, "deep-desert") })),
    "crimson-divide": [
      { name: "Caravan Dunes", points: [{ x: 340, y: 165 }, { x: 1240, y: 160 }] },
      { name: "Open Desert", points: [{ x: 340, y: 380 }, { x: 1040, y: 410 }] },
      { name: "Sunscorched Escarpment", points: [{ x: 1270, y: 345 }, { x: 1390, y: 370 }] },
      { name: "Crimson Dunes", points: [{ x: 1160, y: 655 }, { x: 610, y: 610 }] },
      { name: "Bloodglass Flats", points: [{ x: 350, y: 745 }, { x: 225, y: 770 }] },
      { name: "Heart of the Red Waste", points: [{ x: 1190, y: 895 }, { x: 590, y: 900 }] },
    ].map(zone => ({ ...zone, enemies: regionalEnemies(zone.name, "crimson-divide") })),
    "bloodstone-coast": [
      { name: "Redwind Dunes", points: [{ x: 440, y: 100 }, { x: 1300, y: 155 }] },
      { name: "Western Crags", points: [{ x: 330, y: 280 }, { x: 420, y: 400 }] },
      { name: "Sanguine Wastes", points: [{ x: 700, y: 510 }, { x: 400, y: 550 }] },
      { name: "Giant Rock Spires", points: [{ x: 1090, y: 360 }, { x: 1190, y: 465 }] },
      { name: "Blood Coast", points: [{ x: 750, y: 775 }, { x: 960, y: 780 }] },
    ].map(zone => ({ ...zone, enemies: regionalEnemies(zone.name, "bloodstone-coast") })),
    "ancient-battlefield": [
      { name: "Scavenger Verge", points: [{ x: 1450, y: 330 }] },
      { name: "Bone Pits", points: [{ x: 1090, y: 240 }] },
      { name: "Shattered Causeway", points: [{ x: 900, y: 455 }] },
      { name: "Mass Graves", points: [{ x: 580, y: 250 }] },
      { name: "Bloodied Trenches", points: [{ x: 1000, y: 760 }] },
      { name: "Silent Westfield", points: [{ x: 230, y: 700 }] },
    ].map(zone => ({ ...zone, enemies: regionalEnemies(zone.name, "ancient-battlefield") })),
    "duskpine-forest": [
      { name: "Frostpine Fringe", points: [{ x: 1450, y: 320 }] },
      { name: "Blackpine Woods", points: [{ x: 720, y: 720 }] },
      { name: "Deep Boreal", points: [{ x: 600, y: 260 }] },
      { name: "Frostbound Bog", points: [{ x: 315, y: 760 }] },
      { name: "Shadowroot Hollow", points: [{ x: 1260, y: 740 }] },
    ].map(zone => ({ ...zone, enemies: regionalEnemies(zone.name, "duskpine-forest") })),
  };
  const zonesForRegion = (regionId = worldState.regionId) => regionalZones[regionId] || zones;
  const enemyLevels = EncounterRules.enemyLevels;
  const greatHunts = [
    {
      id: "gaian-avatar-of-the-land", enemy: "Gaian, Avatar of the Land", level: 53, regionId: "verdigris-jungle",
      zone: "Old Growth", zones: [regionalZones["verdigris-jungle"][4]],
      roster: ["Gaian, Avatar of the Land", "Jungle Core", "Jungle Core"],
      description: "Challenge Gaian, Avatar of the Land, in the Jungle's Old Growth. Two Jungle Cores shield him, each cutting his incoming damage by 25%, and a fallen Core regrows after three of his turns. Bring both down together and he is Severed from the land: exposed and slowed until they regrow."
    },
    {
      id: "archdevil-saffron", enemy: "Archdevil Saffron", level: 66, regionId: "deep-desert",
      zone: "Pyramid Sanctum", zones: [regionalZones["deep-desert"][4]],
      roster: ["Archdevil Saffron", "Great Devil Servant - Protector", "Great Devil Servant - Reaver", "Great Devil Servant - Archer"],
      description: "Challenge Archdevil Saffron in the Deep Desert's Pyramid Sanctum, an infernal archmage served by three Great Devil Servants: a protector, a reaver and an archer. When a Servant falls below a third of its health he drains it into the hellfire to heal and grow stronger, and a rift returns it three of his turns later. Kill his Servants outright, and they stay dead."
    },
    {
      id: "deep-sea-king", enemy: "Deep Sea King", level: 32, regionId: "sunward-coast",
      zone: "Beach", zones: [regionalZones["sunward-coast"][4]],
      description: "Challenge the Deep Sea King on the Sunward Coast's Beach. He fights one champion at a time in royal duels, always the hero who hurt him most; every other blow against him loses half its force. A fallen champion only makes him stronger."
    },
    {
      id: "ancient-lich-ool", enemy: "Ancient Lich Ool", level: 78,
      offerRegionId: "duskpine-forest", regionId: "ancient-battlefield",
      zone: "Mass Graves", zones: [regionalZones["ancient-battlefield"][3]],
      description: "Accept the Hunt at Ravenwatch, then confront Ancient Lich Ool in the Ancient Battlefield's Mass Graves. His soul is bound to a phylactery on the field: while it stands he cannot truly die, crumbling to dust and re-forming two rounds later. His three Gravebound Legionnaires take every single blow aimed at it, though area attacks still reach it. Break it, and he is mortal and weakened. Return to Ravenwatch for your reward."
    },
    {
      id: "ur-mat-false-dreamer", enemy: "Ur-Mat, the False Dreamer", level: 90, regionId: "cinder-isle",
      zone: "Caldera", zones: [regionalZones["cinder-isle"][4]],
      description: "Face Ur-Mat, the False Dreamer, in the volcanic Caldera: the most powerful mortal sorcerer, who dreams the battle itself. Every third turn he rewrites its law, in a fixed cycle of seven: blades of glass, rebounding words, inverted mercy, slowed time, echoing spells, sealed names and the dreamer's ward. At the brink of death Lucid Change wakes him at full strength, holding two laws at once."
    },
    // Ornn, God of the Forge, left the Great Hunts for Hearth Home (port task 3, R14, R15; hearth-home.js).
    {
      id: "great-imp-jiruan",
      enemy: "Great Imp Ji'ruan",
      level: 18,
      regionId: "alder-vale",
      zone: "Deep Forest",
      description: "Hunt Great Imp Ji'ruan, a unique summoner haunting the heart of the Great Alderwood."
    },
    {
      id: "zik-lun-armored-hulk",
      enemy: "Zik Lun, the armored Hulk",
      level: 24,
      regionId: "stoneveil-foothills",
      zones: stoneveilGreatHuntZones,
      zone: "Mountainfoot",
      roster: ["Zik Lun, the armored Hulk", "Troll Shieldbearer", "Troll War-drummer"],
      description: "Hunt Zik Lun, the armored Hulk, and his warband in the Stoneveil Foothills. He bellows orders to his Shieldbearer and War-drummer, calls more trolls with his war horn and rages when his trolls fall. He may be found in the Plains, Hill-Plains or Mountainfoot."
    },
    {
      id: "nygheir-the-wounded",
      enemy: "Nygheir the Wounded",
      level: 23,
      regionId: "stoneveil-foothills",
      zones: [stoneveilGreatHuntZones[2]],
      zone: "Mountainfoot",
      description: "Hunt Nygheir the Wounded, an ancient Earth Dragon sleeping at the Foot of the Mountain. Critical hits tear open his old wounds: he bleeds and takes more damage, but every open wound feeds his rage. Broken to a quarter of his strength, his scars burst into fire."
    },
    {
      id: "juila-grand-forest-matriach",
      enemy: "Juila the Grand Forest Matriarch",
      level: 22,
      regionId: "verdant-reach",
      zones: [regionalZones["verdant-reach"][3]],
      zone: "Deep Forest",
      roster: ["Juila the Grand Forest Matriarch", "Grove Matriarch", "Grove Matriarch"],
      description: "Hunt Juila the Grand Forest Matriarch and her two Grove Matriarchs in the heart of the Verdant Reach. She turns the seasons as she fights, and each season brings its own spirit."
    },
    {
      id: "anatemis-white-star",
      enemy: "Anatemis, the White Star",
      level: 100,
      offerRegionId: "high-stoneveil",
      regionId: "stoneveil-summit",
      zones: [{ name: "Frozen Crown", points: [{ x: 700, y: 160 }, { x: 960, y: 190 }] }],
      zone: "Frozen Crown",
      description: "Accept the Hunt at Cairnrest, then ascend to the Frozen Crown of Stoneveil Summit and face Anatemis, the White Star, the strongest being alive. She tests you with a trial in each of her three forms, and each form keeps the trials before it: her Mirror Scales throw your spells back until blades shatter them; as the Golden Star she calls down Sunfall on a marked hero unless you strike her hard enough to turn it aside; as the Zenith of Dragonkind she gathers a supernova that burns every hero unless you collapse it first. Return to Cairnrest for your reward."
    }
  ];
  const greatHuntById = id => greatHunts.find(entry => entry.id === id);
  const completedGreatHuntsHere = () => guild().greatHuntsCompleted
    .map(greatHuntById).filter(entry => (entry?.offerRegionId || entry?.regionId) === worldState.regionId);
  function zonePoint(zone, regionId = "alder-vale") {
    const anchors = zone.points.filter(point => WorldRules.zoneAt(point, regionId) === zone.name);
    const anchor = pick(anchors.length ? anchors : zone.points) || WorldRules.point();
    for (let attempt = 0; attempt < 16; attempt++) {
      const candidate = WorldRules.point({ x: anchor.x + (Math.random() - .5) * 65, y: anchor.y + (Math.random() - .5) * 65 });
      if (WorldRules.zoneAt(candidate, regionId) === zone.name) return candidate;
    }
    return anchor;
  }
  function greatHuntLocation(definition) {
    const regionId = definition.regionId || "alder-vale";
    const zone = pick(definition.zones || zones.filter(entry => entry.name === definition.zone));
    return { regionId, zone: zone.name, point: zonePoint(zone, regionId) };
  }
  /* ---------- Port task 2, the Guild Rework (2026-11-06) ----------
     Six standard quest types on a daily bulletin board per guild, one standard quest and one Great Hunt
     at a time, hand-in at the counter, recruits at the tavern, the hooded man's information and the
     trophy wall, all reached by clicking the guild's interior. Rules: guild-quest-engine.js; design
     answers G1–G34: engine/Docs/Engine-Plan.md, "Decisions for task 2, Guild Rework". */
  const Q = () => window.GuildQuestRules;
  const bonuses = () => calculateClassEffects();
  const today = () => WorldRules.day(worldState.minutes);
  const hasService = (host, name) => Boolean(host) && (!host.guildServices || host.guildServices.includes(name));
  const regionName = regionId => WorldRules.getRegion(regionId)?.name || "another region";
  const placeOf = (regionId, settlementId) => WorldRules.settlementsForRegion(regionId).find(place => place.id === settlementId) || null;
  const originOf = quest => placeOf(quest.originRegionId || quest.regionId || "alder-vale", quest.originCastleId || "alderkeep");
  const titleOf = quest => quest.title || Q().title(quest);
  const isJourney = quest => Q().JOURNEYS.includes(quest?.type);
  const rewardText = reward => `${ShopRules.formatPrice(reward.silver)} · ${reward.reputation} Reputation`;
  let dock = null, selectedNotice = null, noteTimer = null;
  const setStatus = message => { $("guild-status").textContent = message; };

  function enemyFor(zone) {
    const candidates = zone.enemies.filter(name => (enemyLevels[name] || 1) <= Math.max(2, level + 1));
    const choices = candidates.length ? candidates : zone.enemies;
    if (!choices.length) return null;
    const distance = Math.min(...choices.map(name => Math.abs((enemyLevels[name] || 1) - level)));
    return pick(choices.filter(name => Math.abs((enemyLevels[name] || 1) - level) <= distance + 1));
  }
  const questZones = () => {
    const local = zonesForRegion();
    if (["sunward-coast", "cinder-isle", "stormwake-hills", "gloamfen-marsh", "verdigris-jungle", "mirrormere-basin", "saffron-expanse", "deep-desert", "crimson-divide", "bloodstone-coast", "ancient-battlefield", "duskpine-forest"].includes(worldState.regionId)) {
      const suitable = local.filter(zone => (EncounterRules.profileFor(zone.name, worldState.regionId)?.minimum || 1) <= level + 3);
      return suitable.length ? suitable : local.slice(0, 1);
    }
    return worldState.regionId === "alder-vale" ? local.slice(0, level < 4 ? 3 : level < 8 ? 4 : local.length) : local;
  };
  const available = () => Boolean(castle()) && !window.Combat?.active() && !window.WorldMap?.isTravelling() && !worldState.pendingEncounter;
  function artifactPoolForLevel() {
    const artifacts = items.filter(item => item.category === "artifact" && item.rarity !== "masterwork");
    const targetRarity = ShopRules.levelBand(level).rarity;
    const targetRank = ShopRules.rarities.indexOf(targetRarity);
    const eligibleRarities = ShopRules.rarities
      .filter(rarity => artifacts.filter(item => String(item.rarity).toLowerCase() === rarity).length >= 3)
      .sort((left, right) => Math.abs(ShopRules.rarities.indexOf(left) - targetRank) - Math.abs(ShopRules.rarities.indexOf(right) - targetRank));
    const rarity = eligibleRarities[0] || targetRarity;
    return { rarity, artifacts: artifacts.filter(item => String(item.rarity).toLowerCase() === rarity) };
  }

  const informationState = () => guild().information ||= GuildInformationRules.normalize();
  function informationAvailable() {
    const services = castle()?.guildServices;
    return !modal.hidden && available() && (!services || services.includes("information"));
  }
  function resetInformationView() { informationView = null; informationLoading = false; informationRequest++; }
  function informationPool(kind) {
    const known = informationState().revealed[kind];
    return kind === "class"
      ? GuildInformationRules.playerClasses(classCatalog, raceCatalog).filter(definition => !known.includes(definition.id) && !classLevels[definition.id] && !classRequirementsMet(definition))
      : GuildInformationRules.itemPool(items, informationEnemies, classCatalog).filter(entry => !known.includes(entry.item.id));
  }
  function informationBackButton() {
    return '<button type="button" data-guild-information-page="menu">Back to Information</button>';
  }
  function renderClassInformation(id) {
    const definition = getClass(id);
    if (!definition) return "<p>This class is no longer in the catalogue.</p>";
    const requirements = definition.requirements;
    const classList = entries => entries.map(([classId, rank]) => `${getClass(classId)?.name || classId} Level ${rank}`);
    const required = classList(Object.entries(requirements.classes));
    const alternatives = classList(Object.entries(requirements.anyClasses || {}));
    const excluded = (requirements.excludes || []).map(classId => getClass(classId)?.name || classId);
    return `<div class="class-detail" data-class-color="${escapeHtml(definition.color || "")}"><p class="eyebrow">REVEALED CLASS REQUIREMENTS</p><h3>${escapeHtml(definition.name)}</h3><dl class="guild-intel-details"><dt>Character level</dt><dd>${requirements.level}</dd><dt>Required classes</dt><dd>${escapeHtml(required.join(" and ") || "None")}</dd>${alternatives.length ? `<dt>One of these classes</dt><dd>${escapeHtml(alternatives.join(" or "))}</dd>` : ""}<dt>Other requirements</dt><dd>${escapeHtml(otherClassRequirementLabel(definition) || "None")}</dd>${excluded.length ? `<dt>Cannot be combined with</dt><dd>${escapeHtml(excluded.join(", "))}</dd>` : ""}</dl><p>Acquisition also costs one perk point. This information does not grant the class.</p></div>`;
  }
  function renderItemInformation(id) {
    const entry = GuildInformationRules.itemPool(items, informationEnemies, classCatalog).find(candidate => candidate.item.id === id);
    const item = getItem(id);
    if (!item) return "<p>This item is no longer in the catalogue.</p>";
    const sources = entry?.sources || [];
    const stats = item.category === "weapon"
      ? [{ label: "Base power", value: formatValue(item.basePower || 0) }, { label: "Primary scaling", value: itemStatCodeLabel(item.primary) }, { label: "Secondary scaling", value: itemStatCodeLabel(item.secondary) }, ...equipmentItemStats(item)]
      : item.category === "armor" ? equipmentItemStats(item)
      : (item.effects || []).map(effect => ({ label: "Effect", value: artifactEffectLabel(effect) }));
    const unlocks = classCatalog.filter(definition => definition.requirements.other?.type === "inventory-item" && definition.requirements.other.itemId === id);
    if (unlocks.length) stats.push({ label: "Class unlock", value: unlocks.map(definition => definition.name).join(", ") });
    const details = stats.length ? `<dl class="guild-intel-details" aria-label="Item stats and effects">${stats.map(stat => `<dt${stat.title ? ` title="${escapeHtml(stat.title)}"` : ""}>${escapeHtml(stat.label)}</dt><dd>${escapeHtml(stat.value)}</dd>`).join("")}</dl>` : "";
    const luck = currentStats().luck, bonus = calculateClassEffects().masterworkDropChanceFlat;
    const records = sources.map(source => {
      const adjusted = GuildInformationRules.dropChance(item, source, luck, bonus);
      const guarantee = source.guaranteedUntilClaimed ? "Guaranteed until first collected; afterwards, the chance below applies."
        : source.guaranteedOnFirstKill ? "Guaranteed on the first kill; afterwards, the chance below applies." : "";
      const locations = informationEnemyLocations.get(source.enemyName) || [];
      const whereabouts = locations.length ? locations.map(location => {
        const access = location.kind === "great-hunt" ? ` · Great Hunt: ${location.encounterName}` : location.kind === "site" ? " · Site challenge" : " · Regional encounter";
        const quest = location.offerRegionId && location.offerRegionId !== location.regionId ? ` · Accept the quest at the Guild in ${location.offerRegionName}` : location.kind === "great-hunt" ? " · Accept its Great Hunt at the local Guild" : "";
        return `<span class="guild-intel-location">${escapeHtml(location.regionName)} · ${escapeHtml(location.zone)}${escapeHtml(access + quest)}</span>`;
      }).join("") : '<span class="guild-intel-location">No known regional encounter location.</span>';
      return `<article class="guild-intel-source"><strong>${escapeHtml(source.enemyName)} <small>Level ${source.enemyLevel}</small></strong>${whereabouts}<span>Base drop chance: ${formatValue(source.chance)}%${adjusted !== source.chance ? ` · With your current bonuses: ${formatValue(adjusted)}%` : ""}</span>${guarantee ? `<span>${escapeHtml(guarantee)}</span>` : ""}</article>`;
    }).join("");
    return `<div class="item-detail ${itemRarityClass(item)}"><p class="eyebrow">REVEALED ITEM INFORMATION</p><h3>${escapeHtml(item.name)}</h3><p>${escapeHtml(itemRarityLabel(item.rarity))}${unlocks.length ? " · Class-unlock item" : ""}</p><p class="eyebrow">WHAT IT DOES</p><p class="spell-description">${escapeHtml(itemDescription(item))}</p>${details}<p class="eyebrow">WHERE TO FIND IT</p>${records || '<p>No current enemy drop source is available for this item.</p>'}${isMasterworkEquipment(item) ? '<p>Masterwork drop bonuses are recalculated from your current Luck and class effects.</p>' : ""}</div>`;
  }
  function renderInformationView() {
    const state = informationState(), panel = $("guild-panel"), canBuy = guild().reputation >= INFORMATION_REPUTATION_COST;
    if (informationLoading) { panel.innerHTML = '<p role="status">Loading Guild information…</p>'; return; }
    if (informationView.page === "detail") {
      panel.innerHTML = (informationView.kind === "class" ? renderClassInformation(informationView.id) : renderItemInformation(informationView.id)) + informationBackButton();
    } else if (informationView.page === "archive") {
      const records = ["class", "item"].flatMap(kind => state.revealed[kind].map(id => {
        const subject = kind === "class" ? getClass(id) : getItem(id);
        return subject ? `<button type="button" class="guild-choice" data-guild-information-known="${escapeHtml(id)}" data-information-kind="${kind}"><strong>${escapeHtml(subject.name)}</strong><small>${kind === "class" ? "Class requirements" : "Item effects and drop locations"} · Already revealed · Free to review</small></button>` : "";
      })).join("");
      panel.innerHTML = '<p class="eyebrow">KNOWN INFORMATION</p>' + (records || '<p>No information revealed yet.</p>') + informationBackButton();
    } else if (informationView.page === "offers") {
      const kind = informationView.kind, pool = informationPool(kind);
      panel.innerHTML = `<p class="eyebrow">CHOOSE ONE ${kind === "class" ? "CLASS" : "ITEM"} · ${INFORMATION_REPUTATION_COST} REPUTATION</p><p>Only the chosen entry is revealed. Reputation is spent when you select it.</p>` + state.offers[kind].map(id => {
        const entry = pool.find(candidate => (kind === "class" ? candidate.id : candidate.item.id) === id);
        if (!entry) return "";
        const subject = kind === "class" ? entry : entry.item;
        const label = kind === "class" ? "Reveal requirements" : `Reveal effects, enemy location and drop chance · ${itemRarityLabel(subject.rarity)}${entry.classUnlock ? " · Class-unlock item" : ""}`;
        return `<button type="button" class="guild-choice" data-guild-information-select="${escapeHtml(id)}" data-information-kind="${kind}" ${canBuy ? "" : "disabled"}><strong>${escapeHtml(subject.name)}</strong><small>${escapeHtml(label)}</small></button>`;
      }).join("") + informationBackButton();
    } else {
      const classCount = informationPool("class").length, itemCount = informationPool("item").length;
      panel.innerHTML = `<p class="eyebrow">GUILD INFORMATION</p><p>Spend ${INFORMATION_REPUTATION_COST} Reputation to reveal one entry. You can review purchased information at no additional cost.</p><button type="button" class="guild-choice" data-guild-information-kind="class" ${canBuy && classCount ? "" : "disabled"}><strong>Class</strong><span>Choose one of ${Math.min(3, classCount)} random player-obtainable classes whose requirements you do not yet fulfill.</span><small>${classCount ? `${classCount} unrevealed classes` : "No unrevealed classes with unmet requirements"}</small></button><button type="button" class="guild-choice" data-guild-information-kind="item" ${canBuy && itemCount ? "" : "disabled"}><strong>Item</strong><span>Choose one of ${Math.min(5, itemCount)} random unique or class-unlock items and reveal its effects, enemy locations and drop chances.</span><small>${itemCount ? `${itemCount} unrevealed items` : "All available item drop sources are already known"}</small></button><button type="button" class="guild-choice" data-guild-information-page="archive"><strong>Known information</strong><small>${state.revealed.class.length + state.revealed.item.length} entries · Free to review</small></button><button type="button" data-guild-information-page="guild">Back to Guild</button>`;
    }
  }
  async function openInformation() {
    if (!informationAvailable()) return;
    const owner = guild(), request = ++informationRequest;
    dock = "hooded"; informationView = { page: "menu" }; informationLoading = true; render();
    let enemies = [], failed = false;
    try { enemies = await window.Combat.enemyDropSources(); }
    catch { failed = true; }
    if (request !== informationRequest || owner !== guild() || modal.hidden) return;
    informationEnemies = enemies;
    const siteRosters = { "church-of-talos": () => window.ChurchOfTalos?.enemyRoster?.() || [], "hearth-home": () => window.HearthHome?.enemyRoster?.() || [] };
    const siteEncounters = Object.values(WorldRules.regions).flatMap(region => (region.sites || [])
      .filter(site => siteRosters[site.id])
      .map(site => ({ kind: "site", regionId: region.id, zone: site.name, roster: siteRosters[site.id]() })));
    informationEnemyLocations = GuildInformationRules.enemyLocations(WorldRules.regions, EncounterRules, [...greatHunts, ...siteEncounters]);
    if (failed) $("guild-status").textContent = "Enemy drop sources could not be loaded. Class information is still available.";
    informationLoading = false; render();
  }
  function offerInformation(kind) {
    if (!informationAvailable() || informationLoading || !["class", "item"].includes(kind) || guild().reputation < INFORMATION_REPUTATION_COST) return;
    const pool = informationPool(kind), state = informationState(), count = Math.min(kind === "class" ? 3 : 5, pool.length);
    if (!count) return;
    const idOf = candidate => kind === "class" ? candidate.id : candidate.item.id;
    if (state.offers[kind].length !== count || state.offers[kind].some(id => !pool.some(candidate => idOf(candidate) === id))) state.offers[kind] = shuffle(pool).slice(0, count).map(idOf);
    informationView = { page: "offers", kind }; render(); saveCharacter();
  }
  function revealInformation(kind, id) {
    if (!informationAvailable() || informationLoading || informationView?.page !== "offers" || informationView.kind !== kind || !["class", "item"].includes(kind) || guild().reputation < INFORMATION_REPUTATION_COST) return;
    const state = informationState();
    if (!state.offers[kind].includes(id) || !informationPool(kind).some(candidate => (kind === "class" ? candidate.id : candidate.item.id) === id)) return;
    guild().reputation -= INFORMATION_REPUTATION_COST;
    state.revealed[kind].push(id); state.offers[kind] = [];
    informationView = { page: "detail", kind, id };
    $("guild-status").textContent = `Information revealed for ${INFORMATION_REPUTATION_COST} Reputation. Saved in Known information.`;
    render(); saveCharacter();
  }
  function onRegionChange(previousRegionId) {
    if (previousRegionId !== worldState.regionId) {
      // Clear only the departed region: some bosses (Anatemis) have their
      // rematch placed by a Guild in another region, before the player arrives.
      guild().greatHuntRematches = rematches().filter(rematch => rematch.regionId !== previousRegionId);
      guild().avoidedGreatHunts = guild().avoidedGreatHunts.filter(id => rematches().some(rematch => rematch.uniqueId === id));
      syncRematchCompatibility();
      if (guild().avoidObjective === "great-hunt-rematch") guild().avoidObjective = null;
    }
    onPositionChange();
  }
  function rematchAt(position, includeAvoided = false) {
    const nearby = rematches().filter(rematch => rematch.regionId === worldState.regionId && WorldRules.distance(position, rematch.point) <= 25);
    // Manual retries may reuse an avoided marker. Prefer an available marker
    // first so automatic entry and an explicit retry select the same boss.
    const rematch = nearby.find(entry => !guild().avoidedGreatHunts.includes(entry.uniqueId)) || (includeAvoided ? nearby[0] : null);
    if (!rematch) return null;
    const definition = greatHuntById(rematch.uniqueId);
    return { kind: "great-hunt-rematch", uniqueId: rematch.uniqueId, enemy: rematch.enemy, enemies: definition?.roster || [rematch.enemy], zone: rematch.zone };
  }
  /* ---------- Generating quests ---------- */
  // Points that a Search spot keeps clear of: settlements, sites, region exits and other markers (G20).
  function searchPoint(zone, regionId) {
    const region = WorldRules.getRegion(regionId), clearance = Q().SEARCH.clearance;
    const blockers = [
      ...WorldRules.settlementsForRegion(regionId).map(place => [place, clearance.settlement]),
      ...(region.sites || []).map(site => [site, clearance.site]),
      ...(region.exits || []).map(exit => [exit.point, clearance.exit]),
      ...markers().map(marker => [marker.point, clearance.marker]),
    ];
    for (let attempt = 0; attempt < 40; attempt++) {
      const candidate = zonePoint(zone, regionId);
      if (WorldRules.zoneAt(candidate, regionId) === zone.name && blockers.every(([place, radius]) => WorldRules.distance(candidate, place) > radius)) return candidate;
    }
    return null;
  }
  const destinationOf = (place, regionId) => ({ regionId, settlementId: place.id, name: place.name, point: { x: place.x, y: place.y } });
  // Deliver: a guild one region away. Escort and Travel: any settlement other than this guild's,
  // in an adjacent region when there is one (G9, G10).
  function journeyTargets(host, guildsOnly) {
    const here = worldState.regionId;
    const away = Q().adjacentRegions(here).flatMap(regionId => (guildsOnly ? Q().guildsIn(regionId) : Q().settlementsIn(regionId)).map(place => destinationOf(place, regionId)));
    if (guildsOnly || away.length) return away;
    return Q().settlementsIn(here).filter(place => place.id !== host.id).map(place => destinationOf(place, here));
  }
  function questOffer(type, host) {
    const regionId = worldState.regionId, questLevel = Q().questLevel(level, regionId);
    const base = { version: 2, type, level: questLevel, offeredAtLevel: level, offeredDay: today(), progress: 0, complete: false, reward: Q().reward(type, questLevel, bonuses()), regionId, originRegionId: regionId, originCastleId: host.id };
    const zones = questZones().filter(zone => zone.enemies.length);
    let quest = null;
    if (type === "hunt" || type === "cull") {
      const zone = pick(zones), enemy = zone && enemyFor(zone);
      if (!enemy) return null;
      if (type === "hunt") quest = { ...base, zone: zone.name, enemy, point: zonePoint(zone, regionId), description: `Hunt down an empowered ${enemy} in the ${zone.name}. It is stronger than others of its kind and waits at the marked spot.` };
      else {
        const required = Q().cullRequired(questLevel);
        quest = { ...base, zone: zone.name, enemy, required, radius: Q().CULL.radius, point: zonePoint(zone, regionId), description: `Defeat ${required} ${Q().pluralFor(enemy, required)} inside the marked circle in the ${zone.name}, about 6 km across. While this quest is active, half of all encounters there are ${Q().plural(enemy)}. Only kills inside the circle count.` };
      }
    } else if (type === "search") {
      for (const zone of shuffle(zones)) {
        const point = searchPoint(zone, regionId);
        if (point) { quest = { ...base, zone: zone.name, point, description: `Investigate the marked spot in the ${zone.name}. Use Search there: whatever lurks will attack you, and escaping it is half as likely.` }; break; }
      }
      if (!quest) return null;
    } else {
      const targets = journeyTargets(host, type === "deliver");
      if (!targets.length) return null;
      const destination = pick(targets), recommended = Q().rangeText(Q().regionLevelRange(destination.regionId));
      const where = `${destination.name} (${regionName(destination.regionId)}, recommended ${recommended})`;
      const description = type === "deliver" ? `Carry sealed letters to the Adventurers Guild in ${where}. Hand them over at its counter, then return to the guild counter in ${host.name}.`
        : type === "escort" ? `Escort a caravan to ${where}, then return to the guild counter in ${host.name}. Bandits will likely ambush you on the way out, even on the roads, and escaping them is 70% less likely.`
          : `Travel to ${where}, then return to the guild counter in ${host.name}. No fight is forced on you.`;
      quest = { ...base, regionId: destination.regionId, destination, recommended, description };
    }
    return { ...quest, title: Q().title(quest) };
  }
  function greatHuntOffer(definition, host) {
    const location = greatHuntLocation(definition);
    const offer = {
      version: 2, type: "great-hunt", uniqueId: definition.id, offeredAtLevel: definition.level, level: definition.level, offeredDay: today(), progress: 0, complete: false,
      reward: Q().greatHuntReward(definition.level, Q().questLevel(level, worldState.regionId), bonuses()),
      regionId: location.regionId, originRegionId: definition.offerRegionId || location.regionId, originCastleId: host?.id,
      zone: location.zone, enemy: definition.enemy, point: location.point, description: definition.description,
    };
    return { ...offer, title: Q().title(offer) };
  }
  // Each guild draws its board on the first visit of an in-game day and keeps it all day (G7, G8).
  function drawBoard(host) {
    const count = Q().OFFERS[Q().isCastleGuild(host) ? "castle" : "town"] + Math.max(0, Math.floor(bonuses().guildQuestChoiceFlat));
    const offers = [];
    for (const type of shuffle(Q().TYPES)) {
      if (offers.length >= count) break;
      const offer = questOffer(type, host);
      if (offer) offers.push(offer);
    }
    const completed = new Set(guild().greatHuntsCompleted), active = guild().greatHunt?.uniqueId;
    const eligible = greatHunts.filter(entry => (entry.offerRegionId || entry.regionId) === worldState.regionId && level >= entry.level && !completed.has(entry.id) && entry.id !== active);
    if (eligible.length) offers.push(greatHuntOffer(pick(eligible), host));
    return offers;
  }
  function boardFor(host) {
    const boards = guild().boards ||= {}, day = today();
    for (const [id, board] of Object.entries(boards)) if (board.day !== day) delete boards[id];
    if (!boards[host.id]) { boards[host.id] = { day, offers: drawBoard(host) }; saveCharacter(); }
    return boards[host.id];
  }

  /* ---------- Accepting, handing in, giving up ---------- */
  function acceptReason(offer) {
    if (!offer || offer.taken) return "This notice has already been taken.";
    if (offer.type === "great-hunt") return guild().greatHunt ? "You already carry a Great Hunt. Finish it or give it up first." : "";
    return guild().quest ? "You already carry a quest. Finish it or give it up first; Great Hunts can be taken beside it." : "";
  }
  function accept(index) {
    const host = castle();
    if (!available() || !host || !hasService(host, "quest")) return;
    const offer = boardFor(host).offers[index];
    if (acceptReason(offer)) return;
    if (offer.type === "great-hunt") {
      const definition = greatHuntById(offer.uniqueId);
      if (!definition || (definition.offerRegionId || definition.regionId) !== worldState.regionId || offer.regionId !== definition.regionId || (offer.originRegionId || offer.regionId) !== worldState.regionId || level < definition.level) return;
      if (guild().avoidObjective === "great-hunt") guild().avoidObjective = null;
      guild().greatHunt = { ...structuredClone(offer), taken: false };
    } else {
      const quest = { ...structuredClone(offer), taken: false };
      if (quest.type === "escort") {
        // The ambushes are rolled once, on accepting (G16).
        quest.tripMinutes = Math.max(1, Q().tripMinutes(worldState.regionId, { x: host.x, y: host.y }, quest.destination.regionId, quest.destination.point));
        quest.ambushes = Q().ambushPlan(quest.tripMinutes);
        quest.travelled = 0;
      }
      if (guild().avoidObjective === "hunt") guild().avoidObjective = null;
      guild().quest = quest;
    }
    offer.taken = true; selectedNotice = null;
    setStatus(`Quest accepted: ${titleOf(offer)}. Follow its marker on the map; the Quests button there shows it at any time.`);
    render(); window.WorldMap?.render(); saveCharacter();
  }
  // Ready to hand in at this counter? Journeys only at the guild that issued them, after the outbound
  // visit; Great Hunts in the region that offered them; Hunt, Cull and Search at any guild, as before.
  function handInReady(quest, host) {
    if (!quest || !host) return false;
    if (quest.type === "great-hunt") return quest.complete && (quest.originRegionId || quest.regionId) === worldState.regionId;
    if (isJourney(quest)) return quest.progress >= 1 && quest.originCastleId === host.id && (quest.originRegionId || quest.regionId) === worldState.regionId;
    return quest.complete;
  }
  function pay(quest) {
    const reward = quest.reward;
    guild().completedQuests = Math.max(0, Number(guild().completedQuests) || 0) + 1;
    if (isJourney(quest)) guild().completedTravelQuests = Math.max(0, Number(guild().completedTravelQuests) || 0) + 1;
    guild().reputation += reward.reputation;
    currencies = ShopRules.walletFromValue(ShopRules.walletValue(currencies) + reward.silver);
    document.querySelectorAll(".currency-input").forEach(input => { input.value = currencies[input.dataset.currency]; });
    if (quest.type === "hunt" && guild().avoidObjective === "hunt") guild().avoidObjective = null;
    if (quest.type === "great-hunt") {
      if (guild().avoidObjective === "great-hunt") guild().avoidObjective = null;
      if (quest.uniqueId && !guild().greatHuntsCompleted.includes(quest.uniqueId)) guild().greatHuntsCompleted.push(quest.uniqueId);
    }
  }
  // The counter: hands letters over for a Deliver quest and pays every finished quest at once (G24).
  function useCounter() {
    const host = castle();
    if (!available() || !host) return;
    const quest = guild().quest, paid = [];
    if (quest?.type === "deliver" && !quest.progress && quest.destination?.settlementId === host.id && quest.destination.regionId === worldState.regionId) {
      quest.progress = 1;
      setStatus(`The letters are handed over in ${host.name}. Return to the guild counter in ${originOf(quest)?.name || "the issuing guild"} for your reward.`);
    }
    for (const slot of ["quest", "greatHunt"]) {
      const entry = guild()[slot];
      if (!handInReady(entry, host)) continue;
      pay(entry); paid.push(entry); guild()[slot] = null;
    }
    if (paid.length) {
      const total = paid.reduce((sum, entry) => ({ silver: sum.silver + entry.reward.silver, reputation: sum.reputation + entry.reward.reputation }), { silver: 0, reputation: 0 });
      setStatus(`${paid.map(titleOf).join(" and ")} completed. Earned ${rewardText(total)}.`);
      showRewardNote(total);
    }
    openDock("counter");
    window.WorldMap?.render(); saveCharacter();
  }
  // Quests can be looked at and given up anywhere (G26, G29).
  function giveUp(slot) {
    const quest = guild()[slot];
    if (!quest || window.Combat?.active() || !window.confirm(`Give up "${titleOf(quest)}"? Its progress and reward will be lost.`)) return false;
    if (quest.type === "hunt" && guild().avoidObjective === "hunt") guild().avoidObjective = null;
    if (quest.type === "great-hunt" && guild().avoidObjective === "great-hunt") guild().avoidObjective = null;
    guild()[slot] = null;
    const message = `Quest given up: ${titleOf(quest)}.`;
    setStatus(message); if ($("guild-quests-status")) $("guild-quests-status").textContent = message;
    render(); renderQuestCard(); window.WorldMap?.render(); saveCharacter();
    return true;
  }

  /* ---------- Quest state on the road ---------- */
  const pendingAmbush = quest => quest?.type === "escort" && !quest.progress ? (quest.ambushes || []).find(ambush => !ambush.done) || null : null;
  const atDestination = quest => Boolean(quest?.destination) && worldState.regionId === quest.destination.regionId && WorldRules.settlementAt(worldState.position, worldState.regionId)?.id === quest.destination.settlementId;
  const ambushDue = quest => { const ambush = pendingAmbush(quest); return Boolean(ambush) && (quest.travelled >= ambush.at || atDestination(quest)); };
  // Escort counts the outbound trip's travel minutes for its ambushes (G16).
  function onTravel(minutes) {
    const quest = guild().quest;
    if (quest?.type === "escort" && !quest.progress && Number.isFinite(minutes) && minutes > 0) quest.travelled = Math.max(0, Number(quest.travelled) || 0) + minutes;
  }
  function settleSearch() { const quest = guild().quest; if (quest?.type === "search" && !quest.complete) quest.complete = true; }
  function settleAmbush() { const ambush = pendingAmbush(guild().quest); if (ambush) ambush.done = true; }
  // Winning or fleeing settles a Search or Escort fight; a defeat leaves it open (G15).
  function onBattleFinished(result, objective) {
    const text = String(result || "");
    if (objective === "search" && /^(Victory|Escaped)/.test(text)) settleSearch();
    if (objective === "escort-ambush") {
      if (/^(Victory|Escaped)/.test(text)) settleAmbush();
      else if (/^Defeat/.test(text)) {
        const quest = guild().quest, ambush = pendingAmbush(quest);
        if (ambush) ambush.at = Q().ambushRetry(quest.travelled, quest.tripMinutes);
      }
    }
    if (["search", "escort-ambush"].includes(objective)) { onPositionChange(); window.WorldMap?.render(); }
  }
  // Inside a Cull circle, half of all encounters are the target in one of its usual groups (G13).
  function adjustEncounter(enemies, zone, random = Math.random) {
    const quest = guild().quest;
    if (!Array.isArray(enemies) || !enemies.length || quest?.type !== "cull" || quest.complete || worldState.regionId !== quest.regionId || !quest.point) return enemies;
    if (WorldRules.distance(worldState.position, quest.point) > quest.radius || random() >= Q().CULL.targetShare) return enemies;
    return EncounterRules.generateIncluding(quest.zone || zone, quest.enemy, { level, regionId: worldState.regionId });
  }
  const escapeChance = (chance, objective) => Q().escapeChance(chance, objective);

  /* ---------- The guild window ---------- */
  const DOCK_TITLES = { board: "Bulletin board", counter: "Guild counter", artifacts: "Artifacts", tavern: "Tavern · Recruits", hooded: "The hooded man", trophies: "Great Hunts completed" };
  function spotReason(kind, host) {
    if (kind === "tavern" && !hasService(host, "recruit")) return "No recruits here.";
    if (kind === "hooded" && !hasService(host, "information")) return "No one here sells information.";
    if (kind === "trophies" && !completedGreatHuntsHere().length) return "Complete a Great Hunt offered here to see its trophies.";
    return "";
  }
  function syncSpots(host) {
    $("guild-banner").querySelectorAll("[data-guild-spot]").forEach(spot => {
      const reason = spotReason(spot.dataset.guildSpot, host);
      spot.setAttribute("aria-disabled", String(Boolean(reason)));
      spot.setAttribute("tabindex", reason ? "-1" : "0");
      spot.classList.toggle("is-disabled", Boolean(reason));
      let title = spot.querySelector("title");
      if (!title) { title = document.createElementNS("http://www.w3.org/2000/svg", "title"); spot.prepend(title); }
      title.textContent = reason || spot.getAttribute("aria-label");
    });
  }
  function render() {
    const host = castle();
    if (host) {
      $("guild-eyebrow").textContent = `${host.name.toUpperCase()} · ADVENTURERS GUILD`;
      window.SceneBanners?.show($("guild-banner"), "guild", host, worldState.regionId);
      syncSpots(host);
    }
    $("guild-reputation").textContent = guild().reputation;
    $("guild-dock").hidden = !dock;
    $("guild-modal").querySelector(".guild-hall").dataset.dock = dock || "";
    if (dock) { $("guild-dock-title").textContent = DOCK_TITLES[dock] || ""; renderDock(host); }
    else $("guild-panel").innerHTML = "";
    if (!modal.hidden) updateModalState();
  }
  function renderDock(host) {
    if (dock === "board") renderBoard(host);
    else if (dock === "counter") renderCounter(host);
    else if (dock === "artifacts") renderArtifacts();
    else if (dock === "tavern") renderRecruits(host);
    else if (dock === "hooded") renderInformationView();
    else if (dock === "trophies") renderTrophies();
  }
  function openDock(kind) {
    if (kind !== "hooded") resetInformationView();
    dock = kind; render();
    $("guild-dock").scrollTop = 0;
  }
  function closeDock() { resetInformationView(); dock = null; selectedNotice = null; render(); }
  function activate(kind) {
    const host = castle();
    if (!available() || !host || spotReason(kind, host)) return;
    if (kind === "counter") useCounter();
    else if (kind === "hooded") openInformation();
    else openDock(kind);
  }
  function open() {
    if (!available()) return;
    resetInformationView(); dock = null; selectedNotice = null;
    modal.hidden = false;
    setStatus(guild().notice || "");
    guild().notice = null;
    render(); updateModalState();
    ($("guild-banner").querySelector('[data-guild-spot="board"]') || modal.querySelector(".modal-close"))?.focus();
  }
  function close() { resetInformationView(); dock = null; selectedNotice = null; modal.hidden = true; updateModalState(); $("map-guild").focus(); }

  /* Quest texts shared by the board, the counter and the map's quest card. */
  function placeText(quest) {
    if (isJourney(quest)) return `${quest.destination?.name || "Unknown"} · ${regionName(quest.destination?.regionId)} · recommended ${quest.recommended || Q().rangeText(Q().regionLevelRange(quest.destination?.regionId))}`;
    return `${quest.zone || "Unknown"} · ${regionName(quest.regionId)}`;
  }
  function progressText(quest) {
    const origin = originOf(quest)?.name || "the issuing guild";
    if (quest.type === "hunt") return quest.complete ? "Target defeated · hand it in at any guild counter" : `Defeat the empowered ${quest.enemy} at the marked spot in the ${quest.zone}.`;
    if (quest.type === "cull") return `Defeat ${Q().plural(quest.enemy)}: ${Math.min(quest.progress, quest.required)} / ${quest.required}${quest.complete ? " · hand it in at any guild counter" : " · inside the marked circle"}`;
    if (quest.type === "search") return quest.complete ? "Investigated · hand it in at any guild counter" : `Use Search at the marked spot in the ${quest.zone}.`;
    if (quest.type === "deliver") return quest.progress ? `Letters delivered · return to the guild counter in ${origin}` : `Hand the letters over at the guild counter in ${quest.destination.name}.`;
    if (quest.type === "escort") return quest.progress ? `Caravan delivered · return to the guild counter in ${origin}` : `Bring the caravan to ${quest.destination.name}.`;
    if (quest.type === "travel") return quest.progress ? `Destination reached · return to the guild counter in ${origin}` : `Reach ${quest.destination.name}.`;
    if (quest.type === "great-hunt") return quest.complete ? `Defeated · hand it in at a guild counter in ${regionName(quest.originRegionId || quest.regionId)}` : `Defeat ${quest.enemy} in the ${quest.zone} of ${regionName(quest.regionId)}.`;
    return "";
  }
  function specialText(quest) {
    if (quest.type === "hunt") return "The target is empowered: 30% stronger than others of its kind.";
    if (quest.type === "cull") return "Half of all encounters inside the circle are the target. Only kills inside the circle count.";
    if (quest.type === "deliver") return "Hand the letters over at the destination guild's counter, then come back here.";
    if (quest.type === "search") return "Searching starts a fight; escaping it is half as likely. Winning or escaping completes the search.";
    if (quest.type === "escort") return "Bandits are likely to ambush you on the way out, even on roads: most often once, sometimes not at all, sometimes twice. Escaping them is 70% less likely.";
    if (quest.type === "travel") return "No fight is forced: just get there and back.";
    if (quest.type === "great-hunt") return "A Great Hunt can be carried beside a standard quest, one at a time.";
    return "";
  }
  function targetText(quest) {
    if (quest.type === "cull") return `${quest.required} ${Q().pluralFor(quest.enemy, quest.required)}`;
    if (["hunt", "great-hunt"].includes(quest.type)) return quest.type === "hunt" ? `Empowered ${quest.enemy}` : quest.enemy;
    return "";
  }
  function activeQuestMarkup(slot, quest, withGiveUp = true) {
    return `<article class="guild-active-quest is-type-${quest.type}"><div><small>${escapeHtml(Q().NAMES[quest.type])} · ${slot === "greatHunt" ? "Great Hunt" : "Active quest"}</small><strong>${escapeHtml(titleOf(quest))}</strong><span>${escapeHtml(progressText(quest))}</span><span class="guild-active-place">${escapeHtml(placeText(quest))}</span><span class="guild-active-reward">${escapeHtml(rewardText(quest.reward))}</span></div>${withGiveUp ? `<button type="button" data-guild-give-up="${slot}">Give up</button>` : ""}</article>`;
  }
  function renderBoard(host) {
    const board = boardFor(host), offers = board.offers;
    if (selectedNotice == null || !offers[selectedNotice] || offers[selectedNotice].taken) selectedNotice = offers.findIndex(offer => !offer.taken);
    const active = [["quest", guild().quest], ["greatHunt", guild().greatHunt]].filter(([, quest]) => quest);
    const notices = offers.map((offer, index) => offer.taken ? "" : `<button type="button" class="guild-notice is-type-${offer.type}${index === selectedNotice ? " is-selected" : ""}" data-guild-notice="${index}" aria-pressed="${index === selectedNotice}"><small>${escapeHtml(Q().NAMES[offer.type])}</small><strong>${escapeHtml(titleOf(offer))}</strong><span>${escapeHtml(rewardText(offer.reward))}</span></button>`).join("");
    const offer = offers[selectedNotice], reason = acceptReason(offer);
    const rows = offer ? [["Type", Q().NAMES[offer.type]], ["Objective", offer.description], ["Place", placeText(offer)], ...(targetText(offer) ? [[offer.type === "cull" ? "Kills" : "Target", targetText(offer)]] : []), ["Gold", ShopRules.formatPrice(offer.reward.silver)], ["Reputation", String(offer.reward.reputation)], ["Rules", specialText(offer)]] : [];
    const detail = offer ? `<article class="guild-notice-detail is-type-${offer.type}"><p class="eyebrow">${escapeHtml(Q().NAMES[offer.type].toUpperCase())} · QUEST LEVEL ${offer.level || offer.offeredAtLevel}</p><h4>${escapeHtml(titleOf(offer))}</h4><button type="button" id="guild-accept" data-guild-accept="${selectedNotice}" ${reason ? `disabled title="${escapeHtml(reason)}"` : ""}>Accept quest</button>${reason ? `<p class="guild-accept-reason">${escapeHtml(reason)}</p>` : ""}<dl class="guild-intel-details">${rows.map(([label, value]) => `<dt>${escapeHtml(label)}</dt><dd>${escapeHtml(value)}</dd>`).join("")}</dl></article>` : "";
    $("guild-panel").innerHTML = `<div class="guild-board">${active.length ? `<div class="guild-board-active"><p class="eyebrow">ACCEPTED</p>${active.map(([slot, quest]) => activeQuestMarkup(slot, quest)).join("")}</div>` : ""}<div class="guild-board-layout"><div class="guild-notices" aria-label="Today's notices"><p class="eyebrow">TODAY'S NOTICES · DAY ${board.day}</p>${notices || '<p class="guild-board-empty">No more notices today. New ones go up tomorrow.</p>'}</div>${detail}</div></div>`;
  }
  function renderCounter(host) {
    const lines = [["quest", guild().quest], ["greatHunt", guild().greatHunt]].filter(([, quest]) => quest).map(([slot, quest]) => {
      const ready = handInReady(quest, host);
      return `<li class="${ready ? "is-ready" : ""}"><strong>${escapeHtml(titleOf(quest))}</strong><span>${escapeHtml(ready ? "Ready to hand in" : progressText(quest))}</span></li>`;
    }).join("");
    const challenge = guild().artifactChallenge, day = today();
    const reason = guild().reputation < ARTIFACT_REPUTATION_COST ? `Requires ${ARTIFACT_REPUTATION_COST} Reputation.` : day < guild().artifactNextDay ? `Available again on Day ${guild().artifactNextDay}.` : "";
    const artifacts = !hasService(host, "artifacts") ? "" : challenge
      ? `<p class="guild-challenge-note">Artifact challenge: defeat the empowered ${escapeHtml(challenge.enemy)} at the marked point in ${escapeHtml(challenge.zone)} to claim ${escapeHtml(getItem(challenge.artifactId)?.name || "the artifact")}.</p>`
      : `<button type="button" class="guild-choice" id="guild-artifacts" ${reason ? `disabled title="${escapeHtml(reason)}"` : ""}><strong>Artifacts</strong><span>Choose one of three artifacts of your level's rarity; an empowered guardian then holds it until you defeat it.</span><small>${escapeHtml(reason || `${ARTIFACT_REPUTATION_COST} Reputation · Level-matched rarity · 7-day cooldown`)}</small></button>`;
    $("guild-panel").innerHTML = `<div class="guild-counter-panel"><p>The clerk checks your contracts. Finished quests are paid the moment you step up to the counter.</p>${lines ? `<ul class="guild-counter-quests">${lines}</ul>` : "<p>You carry no quest. Take one from the bulletin board.</p>"}${artifacts}</div>`;
  }
  function offerArtifacts() {
    const host = castle();
    if (!available() || !hasService(host, "artifacts") || guild().reputation < ARTIFACT_REPUTATION_COST || today() < guild().artifactNextDay || guild().artifactChallenge) return;
    const pool = artifactPoolForLevel();
    if (guild().artifactOffers.length !== 3 || guild().artifactOffers.some(id => !pool.artifacts.some(item => item.id === id))) guild().artifactOffers = shuffle(pool.artifacts).slice(0, 3).map(item => item.id);
    openDock("artifacts"); saveCharacter();
  }
  function renderArtifacts() {
    const pool = artifactPoolForLevel();
    $("guild-panel").innerHTML = `<p class="eyebrow">CHOOSE A ${escapeHtml(pool.rarity.toUpperCase())} ARTIFACT · ${ARTIFACT_REPUTATION_COST} REPUTATION</p><div class="guild-artifact-choices">${guild().artifactOffers.map(id => { const item = getItem(id); return item ? `<button class="guild-choice ${itemRarityClass(item)}" data-guild-artifact="${escapeHtml(id)}" type="button"><strong>${escapeHtml(item.name)}</strong><span>${escapeHtml(item.description)}</span><small>${escapeHtml(itemRarityLabel(item.rarity))}${(item.effects || []).length ? " · " + escapeHtml(item.effects.map(artifactEffectLabel).join(" · ")) : ""}</small></button>` : ""; }).join("")}</div><button type="button" data-guild-dock="counter">Back to the counter</button>`;
  }
  function takeArtifact(id) {
    const artifact = getItem(id);
    const pool = artifactPoolForLevel();
    if (!available() || !hasService(castle(), "artifacts") || guild().reputation < ARTIFACT_REPUTATION_COST || guild().artifactNextDay > today() || guild().artifactChallenge || !guild().artifactOffers.includes(id) || artifact?.category !== "artifact" || !pool.artifacts.some(item => item.id === id)) return;
    const regionId = worldState.regionId, zone = pick(questZones()), enemy = enemyFor(zone);
    guild().reputation -= ARTIFACT_REPUTATION_COST;
    guild().artifactNextDay = today() + 7;
    guild().artifactChallenge = { regionId, zone: zone.name, point: zonePoint(zone, regionId), enemy, artifactId: id };
    guild().artifactOffers = [];
    setStatus(`An empowered ${enemy} now guards ${artifact.name} in ${zone.name}.`);
    openDock("counter"); window.WorldMap?.render(); saveCharacter();
  }

  /* Recruits at the tavern (G5, G28, G30, G31): their number by guild, their level by region. */
  const recruitCount = host => Q().RECRUITS[Q().isCastleGuild(host) ? "castle" : "town"];
  const recruitCost = offer => Q().recruitCost(offer?.character?.level || 1, companions.length);
  function recruitsFor(host) {
    const count = recruitCount(host), recruitLevel = Q().recruitLevel(level, worldState.regionId);
    if (!window.Companions || !window.ForgeRules || !raceCatalog.some(race => race.playable)) return [];
    if (recruitOffers.length !== count || recruitOffers.some(offer => offer.guildId !== host.id || offer.character.level !== recruitLevel)) {
      recruitOffers = window.Companions.makeOffers({ count, level: recruitLevel, regionId: worldState.regionId }).map(offer => ({ ...offer, guildId: host.id }));
      saveCharacter();
    }
    return recruitOffers;
  }
  function recruitReason(offer) {
    if (companions.length >= 3) return "Your party already has three companions.";
    if (guild().reputation < recruitCost(offer)) return `Requires ${recruitCost(offer)} Reputation.`;
    return "";
  }
  function renderRecruits(host) {
    const offers = recruitsFor(host);
    const card = offer => {
      const c = offer.character, race = raceCatalog.find(entry => entry.id === c.raceId), reason = recruitReason(offer);
      const gear = ["primaryHand", "armor"].map(slot => getItem(c.equipment[slot])).filter(Boolean).map(item => `<li class="${itemRarityClass(item)}"><span>${escapeHtml(item.name)}</span><small>${escapeHtml(itemRarityLabel(item.rarity))} · ${escapeHtml(itemTypeLabel(item.type))}</small></li>`).join("");
      const stats = statDefinitions.map(stat => `<span><small>${escapeHtml(stat.short)}</small>${c.stats[stat.key]}</span>`).join("");
      return `<article class="guild-recruit-card"><header><strong>${escapeHtml(offer.name)}</strong><small>${escapeHtml(race?.name || "Adventurer")} · Level ${c.level} · No class yet</small></header><div class="guild-recruit-stats">${stats}</div><p class="guild-recruit-points">${c.availablePoints} stat points and ${c.perkPoints} class points to spend</p><ul class="guild-recruit-gear">${gear}</ul><button type="button" data-guild-recruit="${escapeHtml(offer.id)}" ${reason ? `disabled title="${escapeHtml(reason)}"` : ""}>Recruit · ${recruitCost(offer)} Reputation</button>${reason ? `<small class="guild-accept-reason">${escapeHtml(reason)}</small>` : ""}</article>`;
    };
    $("guild-panel").innerHTML = `<p class="eyebrow">${offers.length} ADVENTURER${offers.length === 1 ? "" : "S"} LOOKING FOR WORK · PARTY ${companions.length} / 3</p><p>Recruits arrive with one melee weapon and one armor piece, and with every stat and class point still to spend on their sheet.</p><div class="guild-recruits">${offers.map(card).join("") || "<p>No one is looking for work here.</p>"}</div>`;
  }
  function takeRecruit(id) {
    const host = castle();
    if (!available() || !hasService(host, "recruit")) return;
    const offer = recruitOffers.find(entry => entry.id === id && entry.guildId === host.id);
    if (!offer || recruitReason(offer)) return;
    const cost = recruitCost(offer);
    const member = structuredClone(offer); delete member.guildId;
    if (!window.Companions.add(member)) return;
    // As before the rework, the other offers go once someone is hired; the next visit draws new faces.
    guild().reputation -= cost; recruitOffers = [];
    closeDock();
    setStatus(`${offer.name} joined your party for ${cost} Reputation. Spend their points on their companion sheet.`);
    saveCharacter();
  }
  function renderTrophies() {
    const entries = completedGreatHuntsHere();
    $("guild-panel").innerHTML = `<p class="eyebrow">GREAT HUNTS COMPLETED</p><p>Rematch markers stay active until you deactivate them or leave the boss's region. After a fight, leave the marker area and return to fight again.</p>${entries.map(entry => {
      const active = rematches().some(rematch => rematch.uniqueId === entry.id);
      return `<article class="guild-great-hunt-record"><div><strong>${escapeHtml(entry.enemy)}</strong><span>Level ${entry.level} · ${escapeHtml(WorldRules.getRegion(entry.regionId).name)}</span></div><button type="button" data-guild-rematch="${escapeHtml(entry.id)}" aria-pressed="${active}">${active ? "Deactivate marker" : "Place rematch marker"}</button></article>`;
    }).join("") || "<p>No completed Great Hunts were found.</p>"}`;
  }
  function toggleGreatHuntRematch(id) {
    const definition = greatHuntById(id);
    if (!available() || !definition || (definition.offerRegionId || definition.regionId) !== worldState.regionId || !guild().greatHuntsCompleted.includes(id)) return;
    if (rematches().some(rematch => rematch.uniqueId === id)) {
      guild().greatHuntRematches = rematches().filter(rematch => rematch.uniqueId !== id);
      guild().avoidedGreatHunts = guild().avoidedGreatHunts.filter(avoided => avoided !== id);
      setStatus(`The rematch marker for ${definition.enemy} has been deactivated.`);
    } else {
      const location = greatHuntLocation(definition);
      rematches().push({ uniqueId: id, enemy: definition.enemy, ...location });
      setStatus(`A rematch marker for ${definition.enemy} has been placed in ${location.zone} of ${WorldRules.getRegion(location.regionId).name}. It stays active until you deactivate it or leave that region.`);
    }
    syncRematchCompatibility();
    openDock("trophies"); window.WorldMap?.render(); saveCharacter();
  }
  // A parchment note rises from the counter with the reward, then fades (G24).
  function showRewardNote(total) {
    const note = $("guild-reward-note"), counterSpot = $("guild-banner").querySelector('[data-guild-spot="counter"] .scene-spot-hit'), hall = modal.querySelector(".guild-hall");
    note.innerHTML = `<span class="guild-reward-coin" aria-hidden="true"></span><strong>${escapeHtml(ShopRules.formatPrice(total.silver))}</strong><span class="guild-reward-seal" aria-hidden="true"></span><strong>${total.reputation} Reputation</strong>`;
    if (counterSpot && hall) {
      const box = counterSpot.getBoundingClientRect(), frame = hall.getBoundingClientRect();
      note.style.left = `${Math.round(box.left - frame.left + box.width / 2)}px`;
      note.style.top = `${Math.round(Math.max(96, box.top - frame.top - 24))}px`;
    }
    note.hidden = false; note.classList.remove("is-shown"); void note.offsetWidth; note.classList.add("is-shown");
    clearTimeout(noteTimer); noteTimer = setTimeout(() => { note.hidden = true; note.classList.remove("is-shown"); }, 3000);
  }

  /* ---------- The map's quest card (G29) ---------- */
  const questsModal = $("guild-quests-modal");
  function renderQuestCard() {
    if (!questsModal || questsModal.hidden) return;
    const active = [["quest", guild().quest], ["greatHunt", guild().greatHunt]].filter(([, quest]) => quest);
    const challenge = guild().artifactChallenge;
    $("guild-quests-list").innerHTML = (active.map(([slot, quest]) => activeQuestMarkup(slot, quest, !window.Combat?.active())).join("") || '<p class="guild-board-empty">You carry no quest. Visit an Adventurers Guild and its bulletin board.</p>')
      + (challenge ? `<p class="guild-challenge-note">Artifact challenge: defeat the empowered ${escapeHtml(challenge.enemy)} in ${escapeHtml(challenge.zone)} (${escapeHtml(regionName(challenge.regionId))}).</p>` : "");
  }
  function openQuests() {
    if (!questsModal) return;
    questsModal.hidden = false; $("guild-quests-status").textContent = "";
    renderQuestCard(); updateModalState(); questsModal.querySelector(".modal-close").focus();
  }
  function closeQuests() { if (!questsModal) return; questsModal.hidden = true; updateModalState(); $("map-quests")?.focus(); }

  /* ---------- Map hooks ---------- */
  function canSearch() {
    const quest = guild().quest;
    return Boolean(quest && quest.type === "search" && worldState.regionId === (quest.regionId || "alder-vale") && !quest.complete && quest.point && WorldRules.zoneAt(worldState.position, worldState.regionId) === quest.zone && WorldRules.distance(worldState.position, quest.point) <= Q().SEARCH.reach && !worldState.pendingEncounter && !window.Combat?.active() && !window.WorldMap?.isTravelling());
  }
  // Searching starts a fight with the spot's usual enemies (task 2, section 5).
  function search() {
    if (!canSearch()) return null;
    const quest = guild().quest, zone = zonesForRegion().find(entry => entry.name === quest.zone);
    const enemies = EncounterRules.generate(quest.zone, { level, regionId: worldState.regionId });
    const roster = enemies.length ? enemies : [zone && enemyFor(zone)].filter(Boolean);
    return roster.length ? { enemies: roster, zone: quest.zone, objective: "search" } : null;
  }
  function onPositionChange() {
    guild().avoidedGreatHunts = guild().avoidedGreatHunts.filter(id => rematches().some(rematch => rematch.uniqueId === id && rematch.regionId === worldState.regionId && WorldRules.distance(worldState.position, rematch.point) <= 45));
    if (guild().avoidObjective) {
      const objective = guild().avoidObjective;
      const source = objective === "hunt" ? guild().quest : objective === "great-hunt" ? guild().greatHunt : objective === "artifact" ? guild().artifactChallenge : guild().greatHuntRematch;
      if (!source?.point || worldState.regionId !== (source.regionId || "alder-vale") || WorldRules.distance(worldState.position, source.point) > 45) guild().avoidObjective = null;
    }
    const quest = guild().quest;
    // Escort and Travel register the outbound visit on arrival; an Escort's ambush strikes first.
    if ((quest?.type === "travel" || quest?.type === "escort") && !quest.progress && atDestination(quest) && !pendingAmbush(quest)) quest.progress = 1;
  }
  function onEnemyDefeated(actor) {
    const loot = [];
    const quest = guild().quest, name = String(actor.name || "").replace(/^Empowered /, "").replace(/ \d+$/, "");
    // Cull: only the target, only inside its circle (G12).
    if (quest?.type === "cull" && !quest.complete && worldState.regionId === quest.regionId && quest.point && WorldRules.distance(worldState.position, quest.point) <= quest.radius && actor.character && name === quest.enemy && !actor.summoned) {
      quest.progress += 1;
      if (quest.progress >= quest.required) quest.complete = true;
    }
    const objective = actor.guildObjective;
    if (objective === "hunt" && quest?.type === "hunt") quest.complete = true;
    if (objective === "great-hunt" && guild().greatHunt) guild().greatHunt.complete = true;
    if (objective === "great-hunt-rematch" && rematches().length) {
      // Keep the marker fixed, but do not immediately restart the encounter
      // while the party is still standing at its location after the battle.
      avoidRematch(actor.guildRematchId);
    }
    if (objective === "artifact" && guild().artifactChallenge) {
      const id = guild().artifactChallenge.artifactId;
      if (getItem(id)) loot.push(id);
      guild().artifactChallenge = null;
      if (guild().avoidObjective === "artifact") guild().avoidObjective = null;
    }
    if (objective) window.WorldMap?.render();
    return loot;
  }
  function objectiveAt(position) {
    const quest = guild().quest, hunt = guild().greatHunt;
    if (quest?.type === "hunt" && worldState.regionId === (quest.regionId || "alder-vale") && !quest.complete && guild().avoidObjective !== "hunt" && quest.point && quest.enemy && quest.zone && WorldRules.distance(position, quest.point) <= 25) return { kind: "hunt", enemy: quest.enemy, zone: quest.zone };
    if (hunt && !hunt.complete && worldState.regionId === (hunt.regionId || "alder-vale") && guild().avoidObjective !== "great-hunt" && hunt.point && hunt.enemy && hunt.zone && WorldRules.distance(position, hunt.point) <= 25) {
      const definition = greatHuntById(hunt.uniqueId);
      return { kind: "great-hunt", enemy: hunt.enemy, enemies: definition?.roster || [hunt.enemy], zone: hunt.zone };
    }
    // Escort bandits strike wherever the trip has reached, roads included (G16, G17).
    if (ambushDue(quest)) {
      const enemies = Q().ambushGroup(Q().ambushLevel(level, worldState.regionId));
      return { kind: "escort-ambush", enemy: enemies[0], enemies, zone: WorldRules.zoneAt(position, worldState.regionId) };
    }
    const challenge = guild().artifactChallenge;
    if (challenge?.point && worldState.regionId === (challenge.regionId || "alder-vale") && guild().avoidObjective !== "artifact" && WorldRules.distance(position, challenge.point) <= 25) return { kind: "artifact", enemy: challenge.enemy, zone: challenge.zone };
    return rematchAt(position);
  }
  // A destination in another region is marked on the road (or harbour) leading there (G19).
  function towardMarker(regionId, label) {
    const step = Q().firstStep(worldState.regionId, regionId);
    if (!step) return null;
    return step.kind === "exit"
      ? { point: { ...step.exit.point }, label: `Toward ${regionName(regionId)} · ${label}`, kind: "quest" }
      : { point: { x: step.settlement.x, y: step.settlement.y }, label: `Boat toward ${regionName(regionId)} · ${label}`, kind: "quest" };
  }
  function markers() {
    const result = [], quest = guild().quest, hunt = guild().greatHunt, here = worldState.regionId;
    if (quest && !quest.complete) {
      if (["hunt", "cull", "search"].includes(quest.type) && quest.point && here === (quest.regionId || "alder-vale")) {
        result.push({ point: quest.point, label: quest.type === "hunt" ? `Empowered ${quest.enemy}` : quest.type === "cull" ? `Cull · ${Q().plural(quest.enemy)} ${Math.min(quest.progress, quest.required)}/${quest.required}` : `Search · ${quest.zone}`, kind: "quest" });
      }
      if (isJourney(quest)) {
        const origin = originOf(quest);
        const target = quest.progress
          ? origin && { regionId: quest.originRegionId || quest.regionId, point: { x: origin.x, y: origin.y }, label: `Return to ${origin.name}` }
          : { regionId: quest.destination.regionId, point: quest.destination.point, label: `${Q().NAMES[quest.type]} · ${quest.destination.name}` };
        if (target) {
          const marker = here === target.regionId ? { point: target.point, label: target.label, kind: "quest" } : towardMarker(target.regionId, target.label);
          if (marker) result.push(marker);
        }
      }
    }
    if (hunt && !hunt.complete && hunt.point && here === (hunt.regionId || "alder-vale")) result.push({ point: hunt.point, label: `Great Hunt · ${hunt.enemy}`, kind: "great-hunt" });
    if (guild().artifactChallenge && here === (guild().artifactChallenge.regionId || "alder-vale")) result.push({ point: guild().artifactChallenge.point, label: `Artifact guardian · ${guild().artifactChallenge.enemy}`, kind: "artifact" });
    for (const rematch of rematches()) if (here === rematch.regionId) result.push({ point: rematch.point, label: `Great Hunt rematch · ${rematch.enemy}`, kind: "great-hunt" });
    return result;
  }
  // The Cull circle, shown on the map while the quest is active (G12).
  function areas() {
    const quest = guild().quest;
    return quest?.type === "cull" && !quest.complete && quest.point && worldState.regionId === quest.regionId ? [{ point: quest.point, radius: quest.radius, label: `Cull area · ${Q().plural(quest.enemy)}` }] : [];
  }
  function avoidObjective(kind, rematchId = null) {
    if (kind === "great-hunt-rematch") avoidRematch(rematchId);
    else if (["hunt", "artifact", "great-hunt"].includes(kind)) guild().avoidObjective = kind;
    // Fleeing settles a Search or Escort fight (G15).
    else if (kind === "search") settleSearch();
    else if (kind === "escort-ambush") settleAmbush();
    if (["search", "escort-ambush"].includes(kind)) onPositionChange();
    saveCharacter();
  }

  /* ---------- Events ---------- */
  $("map-guild").addEventListener("click", open);
  modal.querySelectorAll("[data-guild-close]").forEach(button => button.addEventListener("click", close));
  modal.addEventListener("keydown", event => {
    if (event.key !== "Escape") return;
    event.preventDefault();
    if (dock) closeDock(); else close();
  });
  $("guild-dock-close").addEventListener("click", closeDock);
  $("guild-banner").addEventListener("click", event => {
    const spot = event.target.closest("[data-guild-spot]");
    if (spot && spot.getAttribute("aria-disabled") !== "true") activate(spot.dataset.guildSpot);
    else if (!spot && dock) closeDock();
  });
  $("guild-banner").addEventListener("keydown", event => {
    const spot = event.target.closest("[data-guild-spot]");
    if (!spot || !["Enter", " "].includes(event.key)) return;
    event.preventDefault();
    if (spot.getAttribute("aria-disabled") !== "true") activate(spot.dataset.guildSpot);
  });
  $("guild-panel").addEventListener("click", event => {
    const notice = event.target.closest("[data-guild-notice]"), acceptButton = event.target.closest("[data-guild-accept]"), giveUpButton = event.target.closest("[data-guild-give-up]");
    const artifactButton = event.target.closest("[data-guild-artifact]"), recruitButton = event.target.closest("[data-guild-recruit]"), rematchButton = event.target.closest("[data-guild-rematch]"), dockButton = event.target.closest("[data-guild-dock]");
    if (notice) { selectedNotice = Number(notice.dataset.guildNotice); render(); }
    if (acceptButton) accept(Number(acceptButton.dataset.guildAccept));
    if (giveUpButton) giveUp(giveUpButton.dataset.guildGiveUp);
    if (event.target.closest("#guild-artifacts")) offerArtifacts();
    if (artifactButton) takeArtifact(artifactButton.dataset.guildArtifact);
    if (recruitButton) takeRecruit(recruitButton.dataset.guildRecruit);
    if (rematchButton) toggleGreatHuntRematch(rematchButton.dataset.guildRematch);
    if (dockButton) openDock(dockButton.dataset.guildDock);
    const informationKind = event.target.closest("[data-guild-information-kind]"), informationChoice = event.target.closest("[data-guild-information-select]"), knownInformation = event.target.closest("[data-guild-information-known]"), informationPage = event.target.closest("[data-guild-information-page]");
    if (informationKind) offerInformation(informationKind.dataset.guildInformationKind);
    if (informationChoice) revealInformation(informationChoice.dataset.informationKind, informationChoice.dataset.guildInformationSelect);
    if (informationAvailable() && !informationLoading) {
      if (knownInformation && ["class", "item"].includes(knownInformation.dataset.informationKind) && informationState().revealed[knownInformation.dataset.informationKind].includes(knownInformation.dataset.guildInformationKnown)) {
        informationView = { page: "detail", kind: knownInformation.dataset.informationKind, id: knownInformation.dataset.guildInformationKnown }; render();
      }
      if (informationPage) {
        const page = informationPage.dataset.guildInformationPage;
        if (page === "guild") closeDock();
        else if (["menu", "archive"].includes(page)) { informationView = { page }; render(); }
      }
    }
  });
  if (questsModal) {
    questsModal.querySelectorAll("[data-guild-quests-close]").forEach(button => button.addEventListener("click", closeQuests));
    questsModal.addEventListener("keydown", event => { if (event.key === "Escape") { event.preventDefault(); closeQuests(); } });
    $("guild-quests-list").addEventListener("click", event => { const button = event.target.closest("[data-guild-give-up]"); if (button) giveUp(button.dataset.guildGiveUp); });
  }
  $("map-quests")?.addEventListener("click", openQuests);
  window.Guild = {
    render, open, close, activate, openDock, closeDock, boardFor: () => castle() ? boardFor(castle()) : null, accept, useCounter, giveUp, handInReady,
    // A fresh offer of one type at this guild (the browser checks pin it on today's board).
    makeOffer: type => castle() ? type === "great-hunt" ? null : questOffer(type, castle()) : null,
    onCharacterApplied() { resetInformationView(); dock = null; modal.hidden = true; if (questsModal) questsModal.hidden = true; onPositionChange(); render(); },
    onPositionChange, onRegionChange, onEnemyDefeated, onBattleFinished, onTravel, objectiveAt, rematchAt, markers, areas, canSearch, search, adjustEncounter, escapeChance, avoidObjective,
    openQuests, closeQuests, classInformation: id => renderClassInformation(id), recruitsFor: () => castle() ? recruitsFor(castle()) : [],
  };
})();

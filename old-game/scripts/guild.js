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
  const recruitCost = () => 50 + companions.length * 25;
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
    {
      id: "ornn-god-of-the-forge", enemy: "Ornn, God of the Forge", level: 48, regionId: "cinder-isle",
      zone: "Lava Fields", zones: [regionalZones["cinder-isle"][3]],
      description: "Challenge Ornn, God of the Forge, in the Lava Fields. Every few turns he reforges his gear at the anvil: a molten edge, hardened plate or a spiked shell. Deal him 15% of his health before his next turn and the metal cracks. At half health the Vulcanic Ram erupts from his forge and guards the anvil while he works."
    },
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
  const pluralEnemy = (name, count) => count === 1 ? name : name.endsWith("Wolf") ? name.replace("Wolf", "Wolves") : name.endsWith("s") ? name : name + "s";
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
  function enemyFor(zone) {
    const candidates = zone.enemies.filter(name => (enemyLevels[name] || 1) <= Math.max(2, level + 1));
    const available = candidates.length ? candidates : zone.enemies;
    const distance = Math.min(...available.map(name => Math.abs((enemyLevels[name] || 1) - level)));
    return pick(available.filter(name => Math.abs((enemyLevels[name] || 1) - level) <= distance + 1));
  }
  function rewards(type) {
    const factor = type === "hunt" ? 1.8 : type === "escort" ? 1.4 : 1;
    const previousReputation = Math.max(5, Math.round((8 + level * 1.5) * factor));
    const bonuses = calculateClassEffects();
    return {
      silver: Math.max(0, Math.round((100 + level * 35) * factor * (1 + bonuses.questMoneyPercent / 100))),
      reputation: Math.max(0, Math.round((previousReputation / 2) * (1 + bonuses.questReputationPercent / 100))),
      rateVersion: 2
    };
  }
  function greatHuntOffer(definition) {
    const bonuses = calculateClassEffects(), factor = 2.4;
    const reward = {
      silver: Math.max(0, Math.round((100 + definition.level * 35) * factor * (1 + bonuses.questMoneyPercent / 100))),
      reputation: 3 * Math.max(0, Math.round(((8 + definition.level * 1.5) * factor / 2) * (1 + bonuses.questReputationPercent / 100))),
      rateVersion: 3
    };
    const location = greatHuntLocation(definition);
    return {
      type: "great-hunt",
      uniqueId: definition.id,
      offeredAtLevel: definition.level,
      progress: 0,
      complete: false,
      reward,
      regionId: location.regionId,
      originRegionId: definition.offerRegionId || location.regionId,
      originCastleId: castle()?.id,
      zone: location.zone,
      enemy: definition.enemy,
      point: location.point,
      description: definition.description
    };
  }
  function questOffer(type) {
    const regionId = worldState.regionId, home = castle(), localZones = questZones();
    const reward = rewards(type), base = { type, offeredAtLevel: level, progress: 0, complete: false, reward, regionId, originCastleId: home.id };
    if (type === "clear") {
      const zone = pick(localZones.slice(0, 4));
      const enemy = enemyFor(zone), required = Math.min(6, 2 + Math.floor(level / 5));
      return { ...base, enemy, required, description: `Clear ${required} ${pluralEnemy(enemy, required)} from ${WorldRules.getRegion(regionId).name}.` };
    }
    if (type === "hunt") {
      const zone = pick(localZones), enemy = enemyFor(zone), point = zonePoint(zone, regionId);
      return { ...base, zone: zone.name, enemy, point, description: `Hunt an empowered ${enemy} in ${zone.name}.` };
    }
    if (type === "search") {
      const zone = pick(localZones), point = zonePoint(zone, regionId);
      return { ...base, zone: zone.name, point, description: `Search the marked ${zone.name} zone.` };
    }
    if (type === "travel") {
      const town = pick(WorldRules.settlementsForRegion(regionId).filter(place => place.id !== home.id));
      return { ...base, townId: town.id, point: { x: town.x, y: town.y }, description: `Travel to ${town.name}, then return to the Guild in ${home.name}.` };
    }
    const zone = pick(localZones.slice(1).length ? localZones.slice(1) : localZones), point = zonePoint(zone, regionId);
    return { ...base, zone: zone.name, point, description: `Escort a caravan to the marked point in ${zone.name}, then return to ${home.name}.` };
  }
  const rewardText = reward => `${ShopRules.formatPrice(reward.silver)} · ${reward.reputation} Reputation`;
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
    if (!informationAvailable() || $("guild-information").disabled) return;
    const owner = guild(), request = ++informationRequest;
    informationView = { page: "menu" }; informationLoading = true; render();
    let enemies = [], failed = false;
    try { enemies = await window.Combat.enemyDropSources(); }
    catch { failed = true; }
    if (request !== informationRequest || owner !== guild() || modal.hidden) return;
    informationEnemies = enemies;
    const siteEncounters = Object.values(WorldRules.regions).flatMap(region => (region.sites || [])
      .filter(site => site.id === "church-of-talos")
      .map(site => ({ kind: "site", regionId: region.id, zone: site.name, roster: window.ChurchOfTalos?.enemyRoster?.() || [] })));
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
  function render() {
    const state = guild(), quest = state.quest, day = WorldRules.day(worldState.minutes);
    const host = castle();
    const services = host?.guildServices;
    if (host) {
      $("guild-eyebrow").textContent = `${host.name.toUpperCase()} · ADVENTURERS GUILD`;
      window.SceneBanners?.show($("guild-banner"), "guild", host, worldState.regionId);
    }
    const currentRecruitCost = recruitCost();
    $("guild-reputation").textContent = state.reputation;
    $("guild-recruit").disabled = state.reputation < currentRecruitCost || companions.length >= 3 || !window.Companions || !raceCatalog.some(race => race.playable) || !window.ForgeRules;
    $("guild-recruit").title = companions.length >= 3 ? "Your party already has three companions." : state.reputation < currentRecruitCost ? `Requires ${currentRecruitCost} Reputation.` : `Choose one of three recruits for ${currentRecruitCost} Reputation.`;
    $("guild-recruit").querySelector("small").textContent = companions.length >= 3 ? "Party full · 3 / 3" : `${currentRecruitCost} Reputation · Choose one of three`;
    const knownInformation = informationState().revealed;
    $("guild-information").disabled = informationLoading || state.reputation < INFORMATION_REPUTATION_COST && !knownInformation.class.length && !knownInformation.item.length;
    $("guild-information").title = state.reputation < INFORMATION_REPUTATION_COST ? "Requires 65 Reputation for new intel; known information is free to review." : "Choose class requirements or item drop sources.";
    $("guild-artifacts").disabled = state.reputation < ARTIFACT_REPUTATION_COST || day < state.artifactNextDay || Boolean(state.artifactChallenge);
    $("guild-artifacts").querySelector("small").textContent = state.artifactChallenge ? "Challenge active" : day < state.artifactNextDay ? `Available on Day ${state.artifactNextDay}` : `${ARTIFACT_REPUTATION_COST} Reputation · Level-matched rarity · 7-day cooldown`;
    $("guild-recruit").hidden = Boolean(services) && !services.includes("recruit");
    $("guild-information").hidden = Boolean(services) && !services.includes("information");
    $("guild-artifacts").hidden = Boolean(services) && !services.includes("artifacts");
    $("guild-quest").hidden = Boolean(services) && !services.includes("quest");
    $("guild-great-hunts").hidden = !completedGreatHuntsHere().length || Boolean(services) && !services.includes("great-hunts");
    $("guild-quest").disabled = Boolean(quest);
    if (quest) {
      const home = WorldRules.settlementsForRegion(quest.originRegionId || quest.regionId || "alder-vale").find(place => place.id === (quest.originCastleId || "alderkeep"));
      const progress = quest.type === "clear" ? ` · ${quest.progress}/${quest.required}` : quest.progress ? ` · Outbound objective reached; return to ${home?.name || "the issuing Guild"}` : "";
      const challenge = state.artifactChallenge ? `<p class="guild-challenge-note">Artifact challenge: empowered ${escapeHtml(state.artifactChallenge.enemy)} in ${escapeHtml(state.artifactChallenge.zone)}.</p>` : "";
      const questName = quest.type === "great-hunt" ? `Great Hunt · Level ${quest.offeredAtLevel}` : quest.type;
      const wrongGreatHuntRegion = quest.type === "great-hunt" && (quest.originRegionId || quest.regionId) !== worldState.regionId;
      const completeLabel = quest.complete ? wrongGreatHuntRegion ? `Return to ${WorldRules.getRegion(quest.originRegionId || quest.regionId).name} to complete` : "Complete quest" : "Return after completing the objective";
      $("guild-panel").innerHTML = `<p class="eyebrow">ACTIVE QUEST</p><h3>${escapeHtml(questName)}</h3><p>${escapeHtml(quest.description)}${escapeHtml(progress)}</p><p>Reward: ${escapeHtml(rewardText(quest.reward))}</p><div class="guild-quest-actions"><button type="button" id="guild-complete" ${quest.complete && !wrongGreatHuntRegion ? "" : "disabled"}>${escapeHtml(completeLabel)}</button><button type="button" id="guild-give-up">Give up quest</button></div>${challenge}`;
    } else if (state.artifactChallenge) {
      const challenge = state.artifactChallenge;
      $("guild-panel").innerHTML = `<p class="eyebrow">ARTIFACT CHALLENGE ACTIVE</p><h3>${escapeHtml(getItem(challenge.artifactId)?.name || "Artifact")}</h3><p>Defeat the empowered ${escapeHtml(challenge.enemy)} at the marked point in ${escapeHtml(challenge.zone)} to claim it.</p>`;
    } else $("guild-panel").innerHTML = `<p>Choose Quest to see ${2 + Math.max(0, Math.floor(calculateClassEffects().guildQuestChoiceFlat))} available contracts. Only one contract can be active at a time.</p>`;
    if (informationView) renderInformationView();
    if (!modal.hidden) updateModalState();
  }
  function open() {
    if (!available()) return;
    resetInformationView();
    modal.hidden = false; $("guild-status").textContent = ""; render(); updateModalState(); $("guild-quest").focus();
  }
  function close() { resetInformationView(); modal.hidden = true; updateModalState(); $("map-guild").focus(); }
  function offerQuests() {
    if (!available() || guild().quest) return;
    resetInformationView();
    const types = WorldRules.settlementsForRegion(worldState.regionId).length < 2
      ? ["clear", "hunt", "search", "escort"] : ["clear", "hunt", "search", "travel", "escort"];
    const offerCount = Math.min(types.length, 2 + Math.max(0, Math.floor(calculateClassEffects().guildQuestChoiceFlat)));
    let regularOffers = guild().offers.filter(offer => offer.type !== "great-hunt");
    if (regularOffers.length !== offerCount || new Set(regularOffers.map(offer => offer.type)).size !== offerCount || regularOffers.some(offer => offer.offeredAtLevel !== level || (offer.regionId || "alder-vale") !== worldState.regionId || !types.includes(offer.type))) {
      regularOffers = shuffle(types).slice(0, offerCount).map(questOffer);
    }
    const completed = new Set(guild().greatHuntsCompleted);
    const eligibleGreatHunts = greatHunts.filter(entry => (entry.offerRegionId || entry.regionId) === worldState.regionId && level >= entry.level && !completed.has(entry.id));
    const chosenGreatHunt = eligibleGreatHunts.length ? pick(eligibleGreatHunts) : null;
    const uniqueOffers = chosenGreatHunt ? [greatHuntOffer(chosenGreatHunt)] : [];
    guild().offers = [...regularOffers, ...uniqueOffers];
    $("guild-panel").innerHTML = `<p class="eyebrow">CHOOSE ONE QUEST</p>${guild().offers.map((offer, index) => `<button class="guild-choice${offer.type === "great-hunt" ? " is-great-hunt" : ""}" data-guild-quest="${index}" type="button"><strong>${escapeHtml(offer.type === "great-hunt" ? `Great Hunt · Level ${offer.offeredAtLevel}` : offer.type)}</strong><span>${escapeHtml(offer.description)}</span><small>${escapeHtml(rewardText(offer.reward))}</small></button>`).join("")}`;
    saveCharacter();
  }
  function takeQuest(index) {
    if (!available() || guild().quest || !guild().offers[index]) return;
    const offer = guild().offers[index];
    if (offer.type === "great-hunt") {
      const definition = greatHuntById(offer.uniqueId);
      if (!definition || (definition.offerRegionId || definition.regionId) !== worldState.regionId || offer.regionId !== definition.regionId || (offer.originRegionId || offer.regionId) !== worldState.regionId || level < definition.level) return;
    }
    if (["hunt", "great-hunt"].includes(guild().avoidObjective)) guild().avoidObjective = null;
    guild().quest = structuredClone(guild().offers[index]); guild().offers = [];
    $("guild-status").textContent = "Quest accepted. Follow its marker on the regional map.";
    render(); window.WorldMap?.render(); saveCharacter();
  }
  function completeQuest() {
    if (!available() || !guild().quest?.complete) return;
    if (guild().quest.type === "great-hunt" && (guild().quest.originRegionId || guild().quest.regionId) !== worldState.regionId) return;
    const reward = guild().quest.reward;
    guild().completedQuests = Math.max(0, Number(guild().completedQuests) || 0) + 1;
    if (["travel", "escort"].includes(guild().quest.type)) guild().completedTravelQuests = Math.max(0, Number(guild().completedTravelQuests) || 0) + 1;
    guild().reputation += reward.reputation;
    currencies = ShopRules.walletFromValue(ShopRules.walletValue(currencies) + reward.silver);
    document.querySelectorAll(".currency-input").forEach(input => { input.value = currencies[input.dataset.currency]; });
    if (guild().quest.type === "hunt" && guild().avoidObjective === "hunt") guild().avoidObjective = null;
    if (guild().quest.type === "great-hunt") {
      if (guild().avoidObjective === "great-hunt") guild().avoidObjective = null;
      if (guild().quest.uniqueId && !guild().greatHuntsCompleted.includes(guild().quest.uniqueId)) guild().greatHuntsCompleted.push(guild().quest.uniqueId);
    }
    guild().quest = null; guild().offers = [];
    $("guild-status").textContent = `Quest completed. Earned ${rewardText(reward)}.`;
    render(); window.WorldMap?.render(); saveCharacter();
  }
  function giveUpQuest() {
    if (!available() || !guild().quest || !window.confirm("Give up this quest? Its progress and reward will be lost.")) return;
    if (guild().quest.type === "hunt" && guild().avoidObjective === "hunt") guild().avoidObjective = null;
    if (guild().quest.type === "great-hunt" && guild().avoidObjective === "great-hunt") guild().avoidObjective = null;
    guild().quest = null; guild().offers = [];
    $("guild-status").textContent = "Quest given up. You can choose another contract.";
    render(); window.WorldMap?.render(); saveCharacter();
  }
  function offerArtifacts() {
    if (!available() || $("guild-artifacts").disabled) return;
    resetInformationView();
    const pool = artifactPoolForLevel();
    if (guild().artifactOffers.length !== 3 || guild().artifactOffers.some(id => !pool.artifacts.some(item => item.id === id))) guild().artifactOffers = shuffle(pool.artifacts).slice(0, 3).map(item => item.id);
    $("guild-panel").innerHTML = `<p class="eyebrow">CHOOSE A ${escapeHtml(pool.rarity.toUpperCase())} ARTIFACT · ${ARTIFACT_REPUTATION_COST} REPUTATION</p>${guild().artifactOffers.map(id => { const item = getItem(id); return `<button class="guild-choice" data-guild-artifact="${escapeHtml(id)}" type="button"><strong>${escapeHtml(item.name)}</strong><span>${escapeHtml(item.description)}</span><small>${escapeHtml(item.rarity)}</small></button>`; }).join("")}`;
    saveCharacter();
  }
  function offerRecruits() {
    if (!available() || $("guild-recruit").hidden || $("guild-recruit").disabled) return;
    resetInformationView();
    const cost = recruitCost();
    if (recruitOffers.length !== 3 || recruitOffers.some(offer => offer.character.level !== Math.max(1, level - 10))) recruitOffers = window.Companions.makeOffers();
    $("guild-panel").innerHTML = `<p class="eyebrow">CHOOSE A COMPANION · ${cost} REPUTATION</p>${recruitOffers.map(offer => `<button class="guild-choice" data-guild-recruit="${escapeHtml(offer.id)}" type="button"><strong>${escapeHtml(offer.name)}</strong><span>${escapeHtml(raceCatalog.find(race => race.id === offer.character.raceId)?.name || "Adventurer")} · Level ${offer.character.level} · ${escapeHtml(getItem(offer.character.equipment.primaryHand)?.name || "Unarmed")} · ${escapeHtml(getItem(offer.character.equipment.armor)?.name || "No armor")}</span></button>`).join("")}`;
    saveCharacter();
  }
  function takeRecruit(id) {
    const cost = recruitCost();
    if (!available() || $("guild-recruit").hidden || guild().reputation < cost || companions.length >= 3) return;
    const offer = recruitOffers.find(entry => entry.id === id);
    if (!offer || !window.Companions.add(offer)) return;
    guild().reputation -= cost; recruitOffers = [];
    $("guild-status").textContent = `${offer.name} joined your party for ${cost} Reputation.`;
    render(); saveCharacter();
  }
  function takeArtifact(id) {
    const artifact = getItem(id);
    const pool = artifactPoolForLevel();
    if (!available() || guild().reputation < ARTIFACT_REPUTATION_COST || guild().artifactNextDay > WorldRules.day(worldState.minutes) || guild().artifactChallenge || !guild().artifactOffers.includes(id) || artifact?.category !== "artifact" || !pool.artifacts.some(item => item.id === id)) return;
    const regionId = worldState.regionId, zone = pick(questZones()), enemy = enemyFor(zone);
    guild().reputation -= ARTIFACT_REPUTATION_COST;
    guild().artifactNextDay = WorldRules.day(worldState.minutes) + 7;
    guild().artifactChallenge = { regionId, zone: zone.name, point: zonePoint(zone, regionId), enemy, artifactId: id };
    guild().artifactOffers = [];
    $("guild-status").textContent = `An empowered ${enemy} now guards ${artifact.name} in ${zone.name}.`;
    render(); window.WorldMap?.render(); saveCharacter();
  }
  function showCompletedGreatHunts() {
    resetInformationView();
    if (!available()) return;
    const entries = completedGreatHuntsHere();
    if (!entries.length) return;
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
      $("guild-status").textContent = `The rematch marker for ${definition.enemy} has been deactivated.`;
    } else {
      const location = greatHuntLocation(definition);
      rematches().push({ uniqueId: id, enemy: definition.enemy, ...location });
      $("guild-status").textContent = `A rematch marker for ${definition.enemy} has been placed in ${location.zone} of ${WorldRules.getRegion(location.regionId).name}. It stays active until you deactivate it or leave that region.`;
    }
    syncRematchCompatibility();
    showCompletedGreatHunts(); window.WorldMap?.render(); saveCharacter();
  }
  function canSearch() {
    const quest = guild().quest;
    return Boolean(quest && quest.type === "search" && worldState.regionId === (quest.regionId || "alder-vale") && !quest.complete && quest.point && WorldRules.zoneAt(worldState.position, worldState.regionId) === quest.zone && WorldRules.distance(worldState.position, quest.point) <= 60 && !worldState.pendingEncounter && !window.Combat?.active() && !window.WorldMap?.isTravelling());
  }
  function search() {
    if (!canSearch()) return false;
    const quest = guild().quest;
    quest.complete = true; saveCharacter(); window.WorldMap?.render(); return true;
  }
  function onPositionChange() {
    guild().avoidedGreatHunts = guild().avoidedGreatHunts.filter(id => rematches().some(rematch => rematch.uniqueId === id && rematch.regionId === worldState.regionId && WorldRules.distance(worldState.position, rematch.point) <= 45));
    if (guild().avoidObjective) {
      const objective = guild().avoidObjective;
      const point = objective === "hunt" || objective === "great-hunt"
        ? guild().quest?.point
        : guild().avoidObjective === "artifact"
          ? guild().artifactChallenge?.point
          : guild().greatHuntRematch?.point;
      const regionId = objective === "hunt" || objective === "great-hunt"
        ? guild().quest?.regionId || "alder-vale"
        : objective === "artifact"
          ? guild().artifactChallenge?.regionId || "alder-vale"
          : guild().greatHuntRematch?.regionId || "alder-vale";
      if (!point || worldState.regionId !== regionId || WorldRules.distance(worldState.position, point) > 45) guild().avoidObjective = null;
    }
    const quest = guild().quest;
    if (!quest || quest.complete || worldState.regionId !== (quest.regionId || "alder-vale")) return;
    if (quest.type === "travel" && WorldRules.settlementAt(worldState.position, worldState.regionId)?.id === quest.townId) quest.progress = 1;
    if (quest.type === "escort" && quest.point && WorldRules.distance(worldState.position, quest.point) <= 25) quest.progress = 1;
    if ((quest.type === "travel" || quest.type === "escort") && quest.progress && castle()?.id === (quest.originCastleId || "alderkeep")) quest.complete = true;
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
  function onEnemyDefeated(actor) {
    const loot = [];
    const quest = guild().quest;
    if (quest?.type === "clear" && worldState.regionId === (quest.regionId || "alder-vale") && actor.character && actor.name.replace(/^Empowered /, "").replace(/ \d+$/, "") === quest.enemy && !actor.summoned) {
      quest.progress += 1;
      if (quest.progress >= quest.required) quest.complete = true;
    }
    const objective = actor.guildObjective;
    if (objective === "hunt" && quest?.type === "hunt") quest.complete = true;
    if (objective === "great-hunt" && quest?.type === "great-hunt") quest.complete = true;
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
  function rematchAt(position, includeAvoided = false) {
    const nearby = rematches().filter(rematch => rematch.regionId === worldState.regionId && WorldRules.distance(position, rematch.point) <= 25);
    // Manual retries may reuse an avoided marker. Prefer an available marker
    // first so automatic entry and an explicit retry select the same boss.
    const rematch = nearby.find(entry => !guild().avoidedGreatHunts.includes(entry.uniqueId)) || (includeAvoided ? nearby[0] : null);
    if (!rematch) return null;
    const definition = greatHuntById(rematch.uniqueId);
    return { kind: "great-hunt-rematch", uniqueId: rematch.uniqueId, enemy: rematch.enemy, enemies: definition?.roster || [rematch.enemy], zone: rematch.zone };
  }
  function objectiveAt(position) {
    const quest = guild().quest;
    if (quest?.type === "hunt" && worldState.regionId === (quest.regionId || "alder-vale") && !quest.complete && guild().avoidObjective !== "hunt" && quest.point && quest.enemy && quest.zone && WorldRules.distance(position, quest.point) <= 25) return { kind: "hunt", enemy: quest.enemy, zone: quest.zone };
    if (quest?.type === "great-hunt" && !quest.complete && worldState.regionId === (quest.regionId || "alder-vale") && guild().avoidObjective !== "great-hunt" && quest.point && quest.enemy && quest.zone && WorldRules.distance(position, quest.point) <= 25) {
      const definition = greatHuntById(quest.uniqueId);
      return { kind: "great-hunt", enemy: quest.enemy, enemies: definition?.roster || [quest.enemy], zone: quest.zone };
    }
    const challenge = guild().artifactChallenge;
    if (challenge?.point && worldState.regionId === (challenge.regionId || "alder-vale") && guild().avoidObjective !== "artifact" && WorldRules.distance(position, challenge.point) <= 25) return { kind: "artifact", enemy: challenge.enemy, zone: challenge.zone };
    return rematchAt(position);
  }
  function markers() {
    const result = [], quest = guild().quest;
    if (quest && !quest.complete && quest.point && worldState.regionId === (quest.regionId || "alder-vale") && (!quest.progress || quest.type === "hunt" || quest.type === "great-hunt" || quest.type === "search")) result.push({ point: quest.point, label: quest.type === "hunt" ? `Empowered ${quest.enemy}` : quest.type === "great-hunt" ? `Great Hunt · ${quest.enemy}` : quest.type === "search" ? `Search · ${quest.zone}` : quest.type === "travel" ? "Travel destination" : "Escort destination", kind: quest.type === "great-hunt" ? "great-hunt" : "quest" });
    if (guild().artifactChallenge && worldState.regionId === (guild().artifactChallenge.regionId || "alder-vale")) result.push({ point: guild().artifactChallenge.point, label: `Artifact guardian · ${guild().artifactChallenge.enemy}`, kind: "artifact" });
    for (const rematch of rematches()) if (worldState.regionId === rematch.regionId) result.push({ point: rematch.point, label: `Great Hunt rematch · ${rematch.enemy}`, kind: "great-hunt" });
    return result;
  }
  $("map-guild").addEventListener("click", open);
  modal.querySelectorAll("[data-guild-close]").forEach(button => button.addEventListener("click", close));
  modal.addEventListener("keydown", event => { if (event.key === "Escape") { event.preventDefault(); close(); } });
  $("guild-quest").addEventListener("click", offerQuests);
  $("guild-artifacts").addEventListener("click", offerArtifacts);
  $("guild-recruit").addEventListener("click", offerRecruits);
  $("guild-great-hunts").addEventListener("click", showCompletedGreatHunts);
  $("guild-information").addEventListener("click", openInformation);
  $("guild-panel").addEventListener("click", event => {
    const questButton = event.target.closest("[data-guild-quest]"), artifactButton = event.target.closest("[data-guild-artifact]"), recruitButton = event.target.closest("[data-guild-recruit]"), rematchButton = event.target.closest("[data-guild-rematch]");
    if (questButton) takeQuest(Number(questButton.dataset.guildQuest));
    if (artifactButton) takeArtifact(artifactButton.dataset.guildArtifact);
    if (recruitButton) takeRecruit(recruitButton.dataset.guildRecruit);
    if (rematchButton) toggleGreatHuntRematch(rematchButton.dataset.guildRematch);
    if (event.target.closest("#guild-complete")) completeQuest();
    if (event.target.closest("#guild-give-up")) giveUpQuest();
    const informationKind = event.target.closest("[data-guild-information-kind]"), informationChoice = event.target.closest("[data-guild-information-select]"), knownInformation = event.target.closest("[data-guild-information-known]"), informationPage = event.target.closest("[data-guild-information-page]");
    if (informationKind) offerInformation(informationKind.dataset.guildInformationKind);
    if (informationChoice) revealInformation(informationChoice.dataset.informationKind, informationChoice.dataset.guildInformationSelect);
    if (informationAvailable() && !informationLoading) {
      if (knownInformation && ["class", "item"].includes(knownInformation.dataset.informationKind) && informationState().revealed[knownInformation.dataset.informationKind].includes(knownInformation.dataset.guildInformationKnown)) {
        informationView = { page: "detail", kind: knownInformation.dataset.informationKind, id: knownInformation.dataset.guildInformationKnown }; render();
      }
      if (informationPage) {
        const page = informationPage.dataset.guildInformationPage;
        if (page === "guild") resetInformationView();
        else if (["menu", "archive"].includes(page)) informationView = { page };
        render();
      }
    }
  });
  window.Guild = { render, onCharacterApplied() { resetInformationView(); modal.hidden = true; onPositionChange(); render(); }, onPositionChange, onRegionChange, onEnemyDefeated, objectiveAt, rematchAt, markers, canSearch, search, avoidObjective(kind, rematchId = null) { if (kind === "great-hunt-rematch") avoidRematch(rematchId); else if (["hunt", "artifact", "great-hunt"].includes(kind)) guild().avoidObjective = kind; saveCharacter(); } };
})();

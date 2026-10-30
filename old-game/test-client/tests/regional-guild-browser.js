async function runRegionalGuildChecks() {
  const $ = id => document.getElementById(id);
  const assert = (condition, message) => { if (!condition) throw new Error(message); };
  const castles = [
    ["alder-vale", "alderkeep"],
    ["stoneveil-foothills", "crownspire"],
    ["verdant-reach", "greenwatch"],
    ["sunward-coast", "westport"],
  ];
  const character = defaultCharacter();
  character.raceId = "human";
  character.level = 24;
  character.worldState.guild.reputation = 500;
  applyCharacter(normalizeCharacter(character));
  WorldMap.setAmbientEnabled(false);
  for (const [regionId, castleId] of castles) {
    const castle = WorldRules.settlementsForRegion(regionId).find(place => place.id === castleId);
    assert(castle?.kind === "Castle city", `${castleId} is a castle city`);
    worldState.regionId = regionId;
    WorldRules.setActiveRegion(regionId);
    worldState.position = { x: castle.x, y: castle.y };
    worldState.guild.quest = null;
    worldState.guild.offers = [];
    worldState.guild.artifactChallenge = null;
    worldState.guild.artifactOffers = [];
    worldState.guild.artifactNextDay = 0;
    WorldMap.render();
    assert(!$("map-guild").hidden && !$("map-guild").disabled, `Guild button appears in ${castle.name}`);
    $("map-guild").click();
    assert(!$("guild-modal").hidden && Number($( "guild-reputation").textContent) === worldState.guild.reputation, `${castle.name} uses the shared Guild balance`);
    assert($("guild-eyebrow").textContent.startsWith(castle.name.toUpperCase()), `${castle.name} appears in the Guild heading`);
    $("guild-quest").click();
    const regular = worldState.guild.offers.filter(offer => offer.type !== "great-hunt");
    const localGreatHunts = worldState.guild.offers.filter(offer => offer.type === "great-hunt");
    const expectedGreatHunts = regionId === "alder-vale" ? ["great-imp-jiruan"] : regionId === "stoneveil-foothills" ? ["zik-lun-armored-hulk", "nygheir-the-wounded"] : regionId === "verdant-reach" ? ["juila-grand-forest-matriach"] : [];
    assert(localGreatHunts.length === Math.min(1, expectedGreatHunts.length) && localGreatHunts.every(offer => expectedGreatHunts.includes(offer.uniqueId)), `${castle.name} offers one local Great Hunt`);
    assert(regular.length === 2 && new Set(regular.map(offer => offer.type)).size === 2, `${castle.name} offers two different contracts`);
    for (const offer of regular) {
      assert(offer.regionId === regionId && offer.originCastleId === castleId, `${offer.type} is issued by ${castle.name}`);
      if (offer.zone && offer.point) assert(WorldRules.zoneAt(offer.point, regionId) === offer.zone, `${offer.type} marker lies in the specified ${regionId} zone`);
      if (offer.type === "clear") assert(["Plains", "Hill-Plains", "Mountainfoot", "Forest", "Deep Forest", "Outer Forest", "River", "Plains - near Town", "Coastal Plains", "Windswept Plains", "Salt Marsh", "Dunes", "Beach"].some(zone => EncounterRules.enemiesForZone(zone, regionId).includes(offer.enemy)), "Clear chooses a local enemy");
      if (offer.type === "travel") assert(WorldRules.settlementsForRegion(regionId).some(place => place.id === offer.townId && place.id !== castleId), "Travel targets a different settlement in the same region");
    }
    const index = worldState.guild.offers.indexOf(regular[0]);
    document.querySelector(`[data-guild-quest="${index}"]`).click();
    assert(worldState.guild.quest?.regionId === regionId && WorldRules.normalize(worldState).guild.quest.regionId === regionId, `${castle.name} contract survives save normalization`);
    worldState.guild.quest.complete = true;
    Guild.render();
    const before = worldState.guild.reputation;
    $("guild-complete").click();
    assert(!worldState.guild.quest && worldState.guild.reputation > before, `${castle.name} pays completed contracts`);

    $("guild-artifacts").click();
    assert(worldState.guild.artifactOffers.length === 3, `${castle.name} offers three artifacts`);
    document.querySelector("[data-guild-artifact]").click();
    const challenge = worldState.guild.artifactChallenge;
    assert(challenge?.regionId === regionId && WorldRules.zoneAt(challenge.point, regionId) === challenge.zone, `${castle.name} places an artifact guardian locally`);
    assert(Guild.markers().some(marker => marker.kind === "artifact") && WorldRules.normalize(worldState).guild.artifactChallenge.regionId === regionId, `${castle.name} guardian marker survives save normalization`);
    $("guild-modal").querySelector("[data-guild-close]").click();
    worldState.position = { ...challenge.point };
    assert(Guild.objectiveAt(worldState.position)?.kind === "artifact", `${castle.name} artifact guardian is reachable`);
    worldState.position = { x: castle.x, y: castle.y };

    const town = WorldRules.settlementsForRegion(regionId).find(place => place.id !== castleId);
    worldState.guild.quest = { type: "travel", regionId, originCastleId: castleId, townId: town.id, point: { x: town.x, y: town.y }, progress: 0, complete: false, reward: { silver: 0, reputation: 0, rateVersion: 2 } };
    worldState.position = { x: town.x, y: town.y };
    Guild.onPositionChange();
    assert(worldState.guild.quest.progress === 1 && !worldState.guild.quest.complete, `${castle.name} Travel records the outbound visit`);
    worldState.position = { x: castle.x, y: castle.y };
    Guild.onPositionChange();
    assert(worldState.guild.quest.complete, `${castle.name} Travel completes on return to its issuing Guild`);
    worldState.guild.quest = null;
    worldState.guild.artifactChallenge = null;
  }
  worldState.regionId = "alder-vale";
  WorldRules.setActiveRegion(worldState.regionId);
  worldState.position = { x: 1110, y: 225 };
  WorldMap.render();
  assert($( "map-guild").hidden, "ordinary towns still have no Adventurers Guild");
  Combat.restore(null);
  WorldMap.pause(false);
  return true;
}

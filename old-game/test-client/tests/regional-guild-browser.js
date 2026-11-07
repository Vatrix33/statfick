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
    // The Guild Rework (port task 2): the day's board, accepted from its notice, paid at the counter.
    worldState.guild.boards = {}; worldState.guild.greatHunt = null;
    const board = Guild.boardFor();
    const regular = board.offers.filter(offer => offer.type !== "great-hunt");
    const localGreatHunts = board.offers.filter(offer => offer.type === "great-hunt");
    const expectedGreatHunts = regionId === "alder-vale" ? ["great-imp-jiruan"] : regionId === "stoneveil-foothills" ? ["zik-lun-armored-hulk", "nygheir-the-wounded"] : regionId === "verdant-reach" ? ["juila-grand-forest-matriach"] : [];
    assert(localGreatHunts.length === Math.min(1, expectedGreatHunts.length) && localGreatHunts.every(offer => expectedGreatHunts.includes(offer.uniqueId)), `${castle.name} offers one local Great Hunt`);
    assert(regular.length === 4 && new Set(regular.map(offer => offer.type)).size === 4, `${castle.name} offers four different contracts`);
    for (const offer of regular) {
      assert(offer.originRegionId === regionId && offer.originCastleId === castleId, `${offer.type} is issued by ${castle.name}`);
      if (["hunt", "cull", "search"].includes(offer.type)) assert(offer.regionId === regionId && WorldRules.zoneAt(offer.point, regionId) === offer.zone, `${offer.type} marker lies in the specified ${regionId} zone`);
      if (["hunt", "cull"].includes(offer.type)) assert(EncounterRules.enemiesForZone(offer.zone, regionId).includes(offer.enemy), `${offer.type} chooses a local enemy`);
      if (["deliver", "escort", "travel"].includes(offer.type)) assert(GuildQuestRules.adjacentRegions(regionId).includes(offer.destination.regionId) && offer.destination.settlementId !== castleId, `${offer.type} goes to an adjacent region`);
    }
    Guild.accept(board.offers.indexOf(regular[0]));
    assert(worldState.guild.quest?.originRegionId === regionId && WorldRules.normalize(worldState).guild.quest.originRegionId === regionId, `${castle.name} contract survives save normalization`);
    worldState.guild.quest.complete = true; worldState.guild.quest.progress = 1;
    // Port task 9: a Collect contract is paid only for the ingredients in the pack (P24).
    if (worldState.guild.quest.type === "collect") inventory.push(...Array(worldState.guild.quest.required).fill(worldState.guild.quest.ingredientId));
    const before = worldState.guild.reputation;
    document.querySelector('#guild-banner [data-guild-spot="counter"]').dispatchEvent(new MouseEvent("click", { bubbles: true }));
    assert(!worldState.guild.quest && worldState.guild.reputation > before, `${castle.name} pays completed contracts at its counter`);

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

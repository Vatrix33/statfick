async function runCoastIslandChecks() {
  const assert = (condition, message) => { if (!condition) throw new Error(message); };
  const coast = WorldRules.getRegion("sunward-coast"), isle = WorldRules.getRegion("cinder-isle");
  assert(coast.levelRange.minimum === 15 && coast.levelRange.maximum === 30 && isle.levelRange.minimum === 25 && isle.levelRange.maximum === 40, "Both regions have their requested level ranges");
  assert(coast.settlements.length === 3 && coast.settlements.filter(place => place.kind === "Castle city").length === 1 && isle.settlements.length === 1 && isle.settlements[0].guildServices.includes("quest"), "Coastal and island settlements have their intended guilds");
  assert(WorldRules.getRegion("stoneveil-foothills").exits.find(exit => exit.id === "east-road")?.targetRegionId === "sunward-coast", "Stoneveil's east road leads to the coast");
  assert(coast.exits.length === 1 && coast.exits[0].targetRegionId === "stoneveil-foothills" && isle.exits.length === 0, "Only the coastal west road crosses a map border");
  assert(coast.settlements.find(place => place.id === "seabridge").boat.settlementId === "emberhaven" && isle.settlements[0].boat.settlementId === "seabridge", "The two ports connect in both directions");
  assert(!WorldRules.isLand({ x: 1450, y: 500 }, coast.id) && !WorldRules.isLand({ x: 40, y: 500 }, isle.id), "The ocean cannot be crossed on foot");
  for (const [regionId, levels] of [[coast.id, [15, 20, 25, 30]], [isle.id, [25, 30, 35, 40]]]) {
    const regional = EncounterRules.regionRules(regionId);
    for (const [zone, profile] of Object.entries(regional.profiles)) {
      if (profile.safe) continue;
      assert(regional.chances[zone] > 0 && EncounterRules.enemiesForZone(zone, regionId).length > 0, `${regionId} ${zone} has encounters`);
      for (const level of levels) {
        const group = EncounterRules.generate(zone, { regionId, level }, () => .45);
        assert(group.length && group.every(enemy => EncounterRules.enemyLevels[enemy]), `${regionId} ${zone} generates valid enemies at level ${level}`);
      }
    }
  }

  const character = defaultCharacter();
  character.raceId = "human";
  character.level = 25;
  character.worldState.regionId = coast.id;
  character.worldState.position = { x: 1270, y: 690 };
  applyCharacter(normalizeCharacter(character));
  WorldMap.setAmbientEnabled(false);
  WorldMap.open();
  const boat = document.getElementById("map-boat");
  assert(!boat.hidden && !boat.disabled && Music.state().theme === coast.id, "Seabridge offers boat travel with its own theme");
  const departureTime = worldState.minutes;
  boat.click();
  assert(worldState.regionId === isle.id && worldState.minutes === departureTime + 540 && WorldRules.settlementAt(worldState.position, isle.id)?.id === "emberhaven", "Boat travel reaches Emberhaven after nine hours");
  assert(!boat.hidden && Music.state().theme === isle.id, "Emberhaven offers the return boat and island music");
  document.getElementById("map-guild").click();
  assert(document.getElementById("guild-recruit").hidden && document.getElementById("guild-information").hidden && !document.getElementById("guild-quest").hidden && !document.getElementById("guild-artifacts").hidden, "Emberhaven has reduced guild services");
  document.querySelector("[data-guild-close]").click();
  boat.click();
  assert(worldState.regionId === coast.id && worldState.minutes === departureTime + 1080 && WorldRules.settlementAt(worldState.position, coast.id)?.id === "seabridge", "Return boat reaches Seabridge after nine more hours");
  worldState.position = { x: 560, y: 500 };
  WorldMap.render();
  assert(boat.hidden, "Boat travel is unavailable away from a port");
  document.querySelector("[data-map-close]")?.click();
  WorldMap.pause(false);
}

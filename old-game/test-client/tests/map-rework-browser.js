/* Port task 8, Map Rework (2026-11-06/07): one continent, natural land, routes, the boat trip, the climb's fog and
   clouds, the caravan and the horse carriage. The design answers W1–W35 are in engine/Docs/Engine-Plan.md, "Decisions
   for task 8, Map Rework"; old-game/Docs/Notes.md, "Map Rework", says where each rule lives. One entry per part. */
const mapReworkChecks = {
  counter() {
    let checks = 0;
    return { assert: (condition, message) => { if (!condition) throw Error(message); checks++; }, count: () => checks };
  },
  wait: ms => new Promise(resolve => setTimeout(resolve, ms)),
  // A character with a world state, the map open (or not).
  async start(state, { open = true, level = 40, money = 0 } = {}) {
    await waitForSourceFiles();
    Combat.restore(null);
    applyCharacter(normalizeCharacter({ ...defaultCharacter(), raceId: "human", level, worldState: state }));
    currencies.silver = money; currencies.gold = 0; currencies.platinum = 0;
    WorldMap.setAmbientEnabled(false);
    if (open && document.getElementById("map-modal").hidden) document.getElementById("map-open-button").click();
    if (!open && !document.getElementById("map-modal").hidden) document.querySelector("[data-map-close]").click();
    await mapReworkChecks.wait(200);
  },
  continentState: (extra = {}) => ({ continentVersion: WorldRules.CONTINENT_VERSION, regionId: "alder-vale", position: { x: 420, y: 490 }, minutes: 600, visitedRegions: ["alder-vale"], caravans: ["alderkeep"], ...extra }),
  places(regionId) {
    const region = WorldRules.getRegion(regionId);
    return [...(region.settlements || []).map(place => ["settlement " + place.id, place]), ...(region.sites || []).map(site => ["site " + site.id, site]), ...(region.exits || []).map(exit => ["exit " + exit.id, exit.point])];
  },
};

// W3, W6, W19–W26: every region where draft 6 put it, the three empty regions, the land baked from draft 6, and every
// road, place, site and exit on land; Cinder Isle about 250 km out, level with the Verdant Reach.
async function checkContinentLayout() {
  const { assert, count } = mapReworkChecks.counter(), C = ContinentRules, W = WorldRules;
  const draft6 = { "alder-vale": [0, 0], "stoneveil-foothills": [0, -1700], "high-stoneveil": [-406, -3400], "stoneveil-summit": [-406, -5100], "sunward-coast": [2484, -1700],
    "cinder-isle": [7900, 490], "verdant-reach": [-2484, 490], "stormwake-hills": [-1812, 2134], "gloamfen-marsh": [-4296, 2134], "verdigris-jungle": [-4338, 3778],
    "mirrormere-basin": [-1770, 3778], "saffron-expanse": [-1770, 5422], "deep-desert": [714, 4330], "crimson-divide": [-1770, 7066], "bloodstone-coast": [-1770, 8710],
    "ancient-battlefield": [-4254, 8710], "duskpine-forest": [-4254, 7066], "uncharted-highlands": [2484, -56], "uncharted-steppe": [672, 1644], "uncharted-delta": [-4296, 5422] };
  assert(C.IDS.length === 20 && Object.keys(W.regions).length === 20, "Twenty regions: the seventeen and the three new");
  for (const [id, [x, y]] of Object.entries(draft6)) assert(C.ORIGINS[id].x === x && C.ORIGINS[id].y === y, "Draft 6 places " + id);
  assert(C.SCALE === 1.4 && C.CW === 2240 && C.CH === 1400, "Every region whole, 1.4 times larger (W3)");
  for (const id of ["uncharted-highlands", "uncharted-steppe", "uncharted-delta"]) {
    const region = W.getRegion(id);
    assert(region.uncharted && region.name === "Uncharted land" && !region.settlements.length && !region.roads.length && !region.exits.length, id + " is empty until the port (W27)");
  }
  const shape = C.field();
  assert(shape.cols === ContinentShape.COLS && shape.rows === ContinentShape.ROWS && shape.land.length === shape.cols * shape.rows, "The baked land decodes to its whole grid");
  let landCells = 0; for (let i = 0; i < shape.land.length; i += 7) if (shape.land[i] > 0) landCells++;
  assert(landCells > 35000 && landCells < 55000, "The baked land covers the continent, not the whole grid: " + landCells);
  // Every road (and each crossing between regions), place, site and exit stands on land (W17: never cut).
  let points = 0;
  for (const id of C.IDS) {
    const region = W.getRegion(id), lake = region.lake;
    const inLake = p => lake && ((p.x - lake.x) / lake.rx) ** 2 + ((p.y - lake.y) / lake.ry) ** 2 <= 1;
    for (const path of region.roads || []) for (let k = 1; k < path.length; k++) for (let t = 0; t <= 1; t += 0.25) {
      const p = { x: path[k - 1].x + (path[k].x - path[k - 1].x) * t, y: path[k - 1].y + (path[k].y - path[k - 1].y) * t };
      assert(W.isLand(p, id) || inLake(p), `A road of ${id} stands on land at ${Math.round(p.x)},${Math.round(p.y)}`); points++;
    }
    for (const [what, p] of mapReworkChecks.places(id)) { assert(W.isLand(p, id), `${id}'s ${what} stands on land`); points++; }
  }
  for (const link of C.built.links) for (let t = 0; t <= 1; t += 0.1) { assert(W.passableAt({ x: link.path[0].x + (link.path[1].x - link.path[0].x) * t, y: link.path[0].y + (link.path[1].y - link.path[0].y) * t }), "The road between " + link.key + " crosses land"); points++; }
  assert(points > 8000, "All roads, places and crossings were looked at: " + points);
  assert(C.built.links.length === 15, "Every road between regions is kept (W19): " + C.built.links.length);
  assert(C.linkBetween("ancient-battlefield", "duskpine-forest") && W.exitById("north-road", "ancient-battlefield")?.targetRegionId === "duskpine-forest" && W.exitById("south-road", "duskpine-forest"), "The Battlefield's road leaves north to Duskpine (W19)");
  assert(W.exitById("east-road", "saffron-expanse")?.targetRegionId === "deep-desert" && W.exitById("west-road", "deep-desert")?.targetRegionId === "saffron-expanse", "Saffron's road leaves east to the Deep Desert (W19)");
  // Cinder Isle: an island about 250 km from the mainland, level with the Verdant Reach (W21, W22).
  const isle = C.RECTS["cinder-isle"], verdant = C.RECTS["verdant-reach"];
  assert(isle.y0 === verdant.y0, "Cinder Isle is level with the Verdant Reach");
  let nearest = Infinity;
  for (let row = 0; row < shape.rows; row += 2) for (let col = 0; col < shape.cols; col += 2) {
    const c = { x: shape.x0 + col * shape.step, y: shape.y0 + row * shape.step };
    if (shape.land[row * shape.cols + col] > 0 && C.regionAt(c) !== "cinder-isle") nearest = Math.min(nearest, C.rectDistance(isle, c));
  }
  const offshoreKm = (nearest + 140) / W.UNITS_PER_KM;
  // Draft 6 (approved): about 217 km from the nearest mainland cape (the rocky highlands'), about 250 from Seabridge's shore.
  assert(offshoreKm > 200 && offshoreKm < 290, "Cinder Isle lies about 250 km offshore: " + offshoreKm.toFixed(0));
  // No land is farther from its region's own map than a region's points can reach.
  for (let row = 0; row < shape.rows; row += 3) for (let col = 0; col < shape.cols; col += 3) {
    if (shape.land[row * shape.cols + col] <= 0) continue;
    const c = { x: shape.x0 + col * shape.step, y: shape.y0 + row * shape.step }, id = C.regionAt(c), p = C.toLocal(id, c);
    assert(Math.max(-p.x, p.x - 1600, -p.y, p.y - 1000, 0) <= C.REACH, "Land past a region's reach at " + Math.round(c.x) + "," + Math.round(c.y));
  }
  return { checks: count() };
}

// W13, W17, W20, W26, W30: walking reaches every place on the mainland (round water where a straight line is blocked);
// off the roads only through entered regions; nothing parts neighbours with no road between them; Cinder Isle only by boat.
async function checkContinentRoutes() {
  const { assert, count } = mapReworkChecks.counter(), C = ContinentRules, W = WorldRules;
  const alderkeep = W.toContinent("alder-vale", { x: 420, y: 490 });
  for (const id of C.IDS) {
    if (id === "cinder-isle" || W.getRegion(id).uncharted) continue;
    for (const [what, p] of mapReworkChecks.places(id)) {
      if (what.startsWith("exit")) continue;
      const end = W.toContinent(id, p), route = W.continentRoute(alderkeep, end, 0);
      assert(route.length && Number.isFinite(W.continentRouteMinutes(alderkeep, route, 0)), `A way on foot leads from Alderkeep to ${id}'s ${what}`);
    }
  }
  const emberhaven = W.toContinent("cinder-isle", W.settlementsForRegion("cinder-isle")[0]);
  assert(!W.continentRoute(alderkeep, emberhaven, 0).length, "No way on foot leads to Cinder Isle");
  // Nocturne Keep: the natural coast put water between it and its road; the way goes round it.
  const keep = W.toContinent("bloodstone-coast", { x: 1240, y: 794 }), road = W.toContinent("bloodstone-coast", { x: 729, y: 341 });
  assert(!Number.isFinite(W.continentTravelMinutes(road, keep, 0)), "Water lies straight between Nocturne Keep and its road");
  const round = W.continentRoute(road, keep, 0);
  assert(round.length >= 2 && Number.isFinite(W.continentRouteMinutes(road, round, 0)), "The way to Nocturne Keep goes round the water");
  // W20: Alder Vale and Stormwake (no road between them) touch; you can walk across.
  assert(Number.isFinite(W.continentTravelMinutes(W.toContinent("alder-vale", { x: 300, y: 980 }), W.toContinent("stormwake-hills", { x: 300, y: 30 }), 0)), "Neighbours with no road between them touch (W20)");
  // W30: off the roads only through entered regions.
  const lakespire = W.toContinent("mirrormere-basin", W.settlementsForRegion("mirrormere-basin").find(p => p.id === "lakespire"));
  const viaRoads = W.continentRoute(alderkeep, lakespire, 0, { entered: ["alder-vale", "mirrormere-basin"] });
  const across = W.continentRoute(alderkeep, lakespire, 0, { entered: ["alder-vale", "uncharted-steppe", "stormwake-hills", "mirrormere-basin"] });
  const km = route => W.continentRouteDistance(alderkeep, route) / W.UNITS_PER_KM;
  assert(km(viaRoads) > 400 && km(across) < 320, `Unentered land is crossed only on roads, entered land straight (W30): ${km(viaRoads).toFixed(0)} vs ${km(across).toFixed(0)} km`);
  const regionsOf = route => new Set(route.flatMap((p, i) => { const a = i ? route[i - 1] : alderkeep; return [0.25, 0.5, 0.75].map(t => C.regionAt({ x: a.x + (p.x - a.x) * t, y: a.y + (p.y - a.y) * t })); }));
  assert(!regionsOf(viaRoads).has("uncharted-steppe"), "The road route stays out of the unentered steppe");
  // W3: distances 1.4 times longer; W4: the same encounters a trip (their spacing stretched).
  assert(Math.abs(W.LOCAL_UNITS_PER_KM * 1.4 - W.UNITS_PER_KM) < 1e-9 && Math.abs(W.ENCOUNTER_MINUTES - 126) < 1e-9, "Distances and encounter spacing grow by 1.4 (W3, W4)");
  return { checks: count() };
}

// W12, W15, W28: a new game, an old save, the empty regions (no encounters, their header, the music playing on) and a
// save standing where the land was cut.
async function checkContinentSaves() {
  const { assert, count } = mapReworkChecks.counter(), W = WorldRules;
  const fresh = W.normalize(undefined);
  assert(fresh.continentVersion === W.CONTINENT_VERSION && fresh.visitedRegions.join() === "alder-vale" && fresh.caravans.join() === "alderkeep", "A new game has entered the Alder Vale and opened Alderkeep's caravan");
  assert(fresh.carriage.owned === false && fresh.carriage.active === false, "A new game has no carriage");
  const old = W.normalize({ regionId: "verdant-reach", position: { x: 900, y: 500 }, minutes: 600, destination: { x: 1300, y: 520 }, route: [{ x: 1300, y: 520 }] });
  assert(old.visitedRegions.length === 20 && old.caravans.length === W.castleCityIds().length, "An old save sees the whole continent and has every caravan (W15)");
  const destination = W.toContinent("verdant-reach", { x: 1300, y: 520 });
  assert(Math.abs(old.destination.x - destination.x) < 1e-6 && Math.abs(old.destination.y - destination.y) < 1e-6, "An old save's destination moves onto the continent");
  // A save standing in water (the coast cut there) is moved to land nearby.
  const wet = { x: 2362, y: 760 };
  assert(!W.passableAt(wet), "The water between the Alder Vale and the highlands is water");
  const moved = W.normalize({ continentVersion: W.CONTINENT_VERSION, ...W.fromContinent(wet), minutes: 600 });
  assert(W.passableAt(W.standing(moved)) && W.distance(W.standing(moved), wet) < 1000, "A save standing in water is moved to land nearby");
  // The empty regions (W28).
  const steppe = W.normalize({ continentVersion: W.CONTINENT_VERSION, regionId: "uncharted-steppe", position: { x: 800, y: 500 }, minutes: 600, visitedRegions: ["alder-vale"], lastChartedRegionId: "alder-vale" });
  assert(steppe.visitedRegions.includes("uncharted-steppe") && steppe.lastChartedRegionId === "alder-vale", "Standing in the steppe enters it and keeps the Alder Vale's music");
  assert(W.zoneAt({ x: 800, y: 500 }, "uncharted-steppe") === "Uncharted land" && EncounterRules.encounterChance("Uncharted land", { level: 40, regionId: "uncharted-steppe" }) === 0 && !EncounterRules.enemiesForZone("Uncharted land", "uncharted-steppe").length, "The empty regions have no encounters");
  await mapReworkChecks.start(steppe);
  assert(document.getElementById("map-region-eyebrow").textContent === "UNCHARTED LAND" && !/LEVEL/.test(document.getElementById("map-region-eyebrow").textContent), "The header reads Uncharted land with no levels (W28)");
  assert(Music.state().track === "alder-vale", "The last region's music plays on in uncharted land (W28): " + Music.state().track);
  assert([...document.querySelectorAll("#map-landscape .map-uncharted-name")].some(node => node.textContent === "Uncharted"), "The map marks the empty region Uncharted (W29)");
  // The Guild's information never places enemies in the empty regions.
  const locations = GuildInformationRules.enemyLocations(WorldRules.regions, EncounterRules);
  assert(![...locations.values()].flat().some(entry => WorldRules.getRegion(entry.regionId).uncharted), "No enemy is said to live in uncharted land");
  // A loaded journey keeps the route it was saved with while it can be walked, and is planned again when it can't.
  const home = W.toContinent("alder-vale", { x: 420, y: 490 }), goal = W.toContinent("alder-vale", { x: 1100, y: 520 });
  const kept = W.continentRoute(home, goal, 0, { entered: ["alder-vale"] }).map((p, i, all) => i === all.length - 1 ? p : { x: p.x + 0.5, y: p.y });
  assert(Number.isFinite(W.continentRouteMinutes(home, kept)), "The route to keep can be walked");
  await mapReworkChecks.start(mapReworkChecks.continentState({ destination: goal, route: kept }), { open: false });
  assert(JSON.stringify(worldState.route) === JSON.stringify(kept), "A loaded journey keeps its walkable route");
  const sea = { x: home.x, y: -7000 };
  assert(!W.passableAt(sea), "The far north is sea");
  await mapReworkChecks.start(mapReworkChecks.continentState({ destination: goal, route: [sea, goal] }), { open: false });
  assert(worldState.route.length && Number.isFinite(W.continentRouteMinutes(W.standing(worldState), worldState.route)) && !worldState.route.some(p => p.y < -6000), "A loaded route across the sea is planned again");
  // W12: regions never entered lie under fog; the fog is rebuilt as regions are entered.
  const fogA = MapContinent.fogImage(["alder-vale"]), fogB = MapContinent.fogImage(W.normalize(undefined).visitedRegions.concat("verdant-reach"));
  assert(fogA.key !== fogB.key, "The fog follows the regions entered (W12)");
  return { checks: count() };
}

// W14: crossing a border crossfades the regions' themes over four seconds, the old one playing on as it fades.
async function checkRegionCrossfade() {
  const { assert, count } = mapReworkChecks.counter();
  await mapReworkChecks.start(mapReworkChecks.continentState(), { open: false });
  const slider = document.getElementById("sound-volume");
  slider.value = "40"; slider.dispatchEvent(new Event("input", { bubbles: true }));
  await mapReworkChecks.wait(300);
  Music.sync();
  await mapReworkChecks.wait(300);
  assert(Music.state().playing && Music.state().track === "alder-vale", "The Alder Vale's theme plays");
  worldState.regionId = "verdant-reach"; WorldRules.setActiveRegion("verdant-reach"); Music.sync();
  const state = Music.state();
  assert(state.track === "verdant-reach" && state.crossfadeSeconds === 4 && state.crossfading.includes("alder-vale"), "The Alder Vale's theme fades out over four seconds as the Verdant Reach's fades in");
  await mapReworkChecks.wait(4400);
  assert(!Music.state().crossfading.length, "The old theme is gone after four seconds");
  worldState.regionId = "alder-vale"; WorldRules.setActiveRegion("alder-vale");
  slider.value = "0"; slider.dispatchEvent(new Event("input", { bubbles: true }));
  return { checks: count() };
}

// W11, W31, W35: the boat trip on the map, 12.6 hours, the waves, a skip, and the carriage waiting at Seabridge.
async function checkBoatTrip() {
  const { assert, count } = mapReworkChecks.counter(), W = WorldRules;
  for (const [regionId, id] of [["sunward-coast", "seabridge"], ["cinder-isle", "emberhaven"]]) assert(W.settlementsForRegion(regionId).find(p => p.id === id).boat.minutes === 756, "The boat takes 12.6 hours (W11)");
  await mapReworkChecks.start(mapReworkChecks.continentState({ regionId: "sunward-coast", position: { x: 1270, y: 690 }, visitedRegions: ["alder-vale", "sunward-coast"], carriage: { owned: true, active: true } }));
  const minutes = worldState.minutes;
  document.getElementById("map-boat").click();
  await mapReworkChecks.wait(1200);
  const journey = WorldMap.journey(), boat = document.getElementById("map-voyage");
  assert(journey && journey.element === "map-voyage" && journey.duration === 5000 && !boat.hasAttribute("hidden"), "The boat sails across the map for five seconds (W14, W31)");
  assert(worldState.regionId === "sunward-coast" && worldState.minutes === minutes, "The trip is applied at its end");
  assert(document.getElementById("map-clock").textContent !== W.time(minutes), "The clock runs on during the trip");
  document.getElementById("map-modal").dispatchEvent(new KeyboardEvent("keydown", { key: "Escape", bubbles: true }));
  await mapReworkChecks.wait(100);
  assert(!WorldMap.journey() && boat.hasAttribute("hidden") && !document.getElementById("map-modal").hidden, "Esc skips the trip without closing the map");
  assert(worldState.regionId === "cinder-isle" && W.settlementAt(worldState.position, worldState.regionId)?.id === "emberhaven" && worldState.minutes === minutes + 756, "You arrive at Emberhaven 12.6 hours later");
  assert(worldState.carriage.parkedAt === "seabridge" && !worldState.carriage.active && document.getElementById("map-carriage").disabled, "The carriage waits at Seabridge (W10, W35)");
  document.getElementById("map-boat").click();
  await mapReworkChecks.wait(300);
  document.getElementById("regional-map").dispatchEvent(new MouseEvent("click", { bubbles: true }));
  await mapReworkChecks.wait(100);
  assert(!WorldMap.journey() && worldState.regionId === "sunward-coast" && worldState.carriage.parkedAt === null && !document.getElementById("map-carriage").disabled, "A click skips the trip back; the carriage is there again");
  return { checks: count() };
}

// W32: the climb's fog (from the High Stoneveil) and clouds (on the Summit), and none in the lowlands.
async function checkMountainAtmosphere() {
  const { assert, count } = mapReworkChecks.counter();
  const at = async (regionId, position) => { await mapReworkChecks.start(mapReworkChecks.continentState({ regionId, position, visitedRegions: ["alder-vale", "stoneveil-foothills", "high-stoneveil", "stoneveil-summit"] })); return WorldMap.altitude(); };
  const vale = await at("alder-vale", { x: 420, y: 490 }), foot = await at("stoneveil-foothills", { x: 1040, y: 550 }), high = await at("high-stoneveil", { x: 800, y: 380 }), summit = await at("stoneveil-summit", { x: 820, y: 200 });
  assert(vale.fog === 0 && vale.clouds === 0 && foot.fog === 0, "No fog in the lowlands or the Foothills");
  assert(foot.height > 0 && high.height > foot.height && summit.height > high.height, "The height rises going north through the Stoneveil");
  assert(high.fog > 0.2 && high.clouds === 0, "Fog lies below the High Stoneveil; no clouds yet");
  assert(summit.fog > high.fog && summit.clouds > 0.5, "On the Summit the fog is thicker and clouds drift below");
  return { checks: count() };
}

// W7, W8, W14, W16: the caravan between visited castle cities: its button, list, fares, times, trip and arrival.
async function checkCaravan() {
  const { assert, count } = mapReworkChecks.counter(), W = WorldRules;
  // Fares and times from road distance.
  for (const [a, b] of [["alderkeep", "greenwatch"], ["alderkeep", "lakespire"], ["westport", "ravenwatch"], ["miragehold", "nocturne-keep"]]) {
    const trip = W.caravanTrip(a, b);
    assert(trip && trip.fare === Math.round(trip.km * 2) && trip.minutes === Math.round(trip.km / 21 * 60), `${a} to ${b}: 2 silver and 1/21 hour a km of road (W7, W8)`);
  }
  assert(W.caravanTrip("alderkeep", "greenwatch").km < 140 && W.caravanTrip("westport", "ravenwatch").km > 1300, "Neighbours are near, the far ends far");
  // The button: only in castle cities, and only once a second caravan is open.
  await mapReworkChecks.start(mapReworkChecks.continentState({ caravans: ["alderkeep"] }), { money: 5000 });
  const button = document.getElementById("map-caravan");
  assert(!button.hidden && button.disabled && /another castle city/.test(button.title), "One open caravan: the button waits for a second");
  await mapReworkChecks.start(mapReworkChecks.continentState({ regionId: "alder-vale", position: { x: 1110, y: 225 }, caravans: ["alderkeep", "greenwatch"] }), { money: 5000 });
  assert(button.hidden, "No caravan in a town");
  await mapReworkChecks.start(mapReworkChecks.continentState({ caravans: ["alderkeep", "greenwatch", "lakespire"], visitedRegions: ["alder-vale", "verdant-reach", "mirrormere-basin"] }), { money: 800 });
  assert(!button.hidden && !button.disabled, "Two open caravans: the button is ready");
  button.click();
  await mapReworkChecks.wait(200);
  const rows = [...document.querySelectorAll("#map-caravan-list li[data-to]")];
  assert(rows.map(row => row.dataset.to).join() === "greenwatch,lakespire", "The list holds the other open caravans, nearest first");
  assert(rows[0].textContent.includes("119 km") || rows[0].textContent.includes("km of road"), "Each row shows its road distance");
  assert(rows[1].querySelector("button").disabled, "A fare you cannot pay cannot be chosen");
  rows[1].dispatchEvent(new PointerEvent("pointerover", { bubbles: true }));
  assert(document.getElementById("map-caravan-route").getAttribute("d").split("L").length > 5, "Pointing at a destination shows its road on the map (W16)");
  const money = ShopRules.walletValue(currencies), minutes = worldState.minutes, trip = W.caravanTrip("alderkeep", "greenwatch");
  rows[0].querySelector("button").click();
  await mapReworkChecks.wait(400);
  const journey = WorldMap.journey();
  assert(journey && journey.element === "map-caravan-wagon" && journey.duration >= 4000 && journey.duration < 4100, "The nearest trip runs about four seconds (W14)");
  assert(ShopRules.walletValue(currencies) === money - trip.fare, "The fare is paid when you set out");
  document.getElementById("map-modal").dispatchEvent(new KeyboardEvent("keydown", { key: "Escape", bubbles: true }));
  await mapReworkChecks.wait(100);
  assert(worldState.regionId === "verdant-reach" && W.settlementAt(worldState.position, worldState.regionId)?.id === "greenwatch" && worldState.minutes === minutes + trip.minutes, "You arrive at Greenwatch the trip's hours later");
  const far = W.caravanTrip("westport", "ravenwatch");
  assert(4000 + 4000 * Math.min(1, (far.km - 110) / 1440) > 7900, "The farthest trip runs about eight seconds");
  return { checks: count() };
}

// W9, W10, W33–W35: the horse carriage: bought once, its speeds, its ground, its switch and its planning.
async function checkCarriage() {
  const { assert, count } = mapReworkChecks.counter(), W = WorldRules;
  assert(W.CARRIAGE.PRICE_SILVER === 50000, "The carriage costs 50,000 silver (W9)");
  // Speeds: twice on a road, half again off it.
  const road = W.getRegion("alder-vale").roads[0][6], plains = { x: 420, y: 640 };
  assert(W.terrainAt(road, "alder-vale") === "road" && W.speedAt(road, { carriage: 1 }, "alder-vale") === 2 * W.speedAt(road, 0, "alder-vale"), "Twice as fast on a road");
  assert(W.speedAt(plains, { carriage: 1 }, "alder-vale") === 1.5 * W.speedAt(plains, 0, "alder-vale"), "Half again as fast on open land");
  // Ground (W10, W33).
  const ground = (regionId, zone) => {
    for (let y = 20; y < 1000; y += 25) for (let x = 20; x < 1600; x += 25) if (W.zoneAt({ x, y }, regionId) === zone) return W.carriageGround({ x, y }, regionId).ok;
    throw Error("No " + zone + " found in " + regionId);
  };
  for (const [regionId, zone] of [["alder-vale", "Plains"], ["stormwake-hills", "Thunder Hills"], ["stormwake-hills", "Rainwashed Downs"], ["mirrormere-basin", "Dry Steppe"], ["saffron-expanse", "Salt Pans"],
    ["sunward-coast", "Beach"], ["deep-desert", "Outer Wastes"], ["crimson-divide", "Open Desert"], ["bloodstone-coast", "Sanguine Wastes"], ["uncharted-steppe", "Uncharted land"]])
    assert(ground(regionId, zone), `The carriage crosses ${zone}`);
  for (const [regionId, zone] of [["alder-vale", "Forest"], ["verdigris-jungle", "Dense Jungle"], ["gloamfen-marsh", "Deep Mire"], ["saffron-expanse", "Shifting Dunes"], ["bloodstone-coast", "Giant Rock Spires"],
    ["stoneveil-foothills", "Mountainfoot"], ["stormwake-hills", "Floodplain"], ["mirrormere-basin", "Desert Fringe"], ["ancient-battlefield", "Silent Westfield"], ["alder-vale", "River"]])
    assert(!ground(regionId, zone), `The carriage keeps out of ${zone}`);
  assert(!W.carriageGround({ x: 820, y: 40 }, "high-stoneveil").ok && W.terrainAt({ x: 820, y: 30 }, "high-stoneveil") === "road" && !W.carriageGround({ x: 820, y: 30 }, "high-stoneveil").ok, "Not on the Stoneveil's mountain roads (W10)");
  assert(!W.carriageGround({ x: 300, y: 500 }, "cinder-isle").ok, "Not on Cinder Isle (W10)");
  // Bought once at a General Store.
  await mapReworkChecks.start(mapReworkChecks.continentState(), { money: 120000 });
  assert(document.getElementById("map-carriage").hidden, "No switch before the carriage is bought");
  document.getElementById("map-item-shop").click();
  await mapReworkChecks.wait(300);
  const buy = document.querySelector("[data-buy-carriage]");
  assert(buy && buy.textContent === "Buy", "The General Store offers the carriage");
  buy.click();
  await mapReworkChecks.wait(100);
  assert(worldState.carriage.owned && ShopRules.walletValue(currencies) === 70000, "Bought for 50,000 silver");
  const again = document.querySelector("[data-buy-carriage]");
  assert(again.disabled && again.textContent === "Owned", "It is bought only once, though there is coin for another");
  again.disabled = false; again.click();
  await mapReworkChecks.wait(100);
  assert(ShopRules.walletValue(currencies) === 70000, "A second purchase charges nothing");
  document.querySelector("[data-shop-close]").click();
  await mapReworkChecks.wait(100);
  const toggle = document.getElementById("map-carriage");
  assert(!toggle.hidden && toggle.getAttribute("aria-pressed") === "false", "Its switch shows at the map's top right");
  toggle.click();
  assert(worldState.carriage.active && toggle.getAttribute("aria-pressed") === "true", "The switch turns it on");
  // W34: with it on, a click plans the carriage's own way, or says there is none.
  const here = W.standing(worldState), forest = W.toContinent("alder-vale", { x: 300, y: 250 });
  assert(!W.continentRoute(here, forest, { carriage: 1 }, { entered: worldState.visitedRegions }).length && W.continentRoute(here, forest, 0, { entered: worldState.visitedRegions }).length, "No carriage way into the forest, a way on foot");
  const along = W.getRegion("alder-vale").roads[0], a = W.toContinent("alder-vale", along[6]), b = W.toContinent("alder-vale", along[9]);
  const fast = W.continentTravelMinutes(a, b, { carriage: 1 }), slow = W.continentTravelMinutes(a, b, 0);
  assert(Math.abs(fast * 2 - slow) < 1e-6 * slow, `Along a road the carriage takes half the time: ${fast.toFixed(1)} vs ${slow.toFixed(1)} minutes`);
  // Steered into rough ground it switches itself off.
  WorldRules.placeAt(worldState, W.toContinent("alder-vale", { x: 330, y: 300 }));
  worldState.carriage.active = true;
  worldState.destination = forest; worldState.route = [forest];
  WorldMap.render();
  document.getElementById("map-character").dispatchEvent(new MouseEvent("click", { bubbles: true }));
  await mapReworkChecks.wait(1500);
  WorldMap.pause(false);
  assert(!worldState.carriage.active && /Rough ground for a carriage/.test(document.getElementById("map-status").textContent), "On rough ground it switches itself off, saying why");
  return { checks: count() };
}

// W17, W22, W26, W29: the map's painting: each region's picture over its territory, its own map always its own, the
// lines between regions wandering, the empty regions as parchment, and the coastline.
async function checkContinentPainting() {
  const { assert, count } = mapReworkChecks.counter(), C = ContinentRules;
  const saffron = C.RECTS["saffron-expanse"], deep = { x: (saffron.x0 + saffron.x1) / 2, y: (saffron.y0 + saffron.y1) / 2 };
  assert(C.cover(deep, "saffron-expanse") === 1 && C.cover(deep, "crimson-divide") === 0, "Deep in its own map a region's picture is its own");
  // Between Saffron and Crimson the line where the pictures meet wanders east to west (W26: not a straight line).
  const lineAt = x => { for (let y = saffron.y1 - 400; y < saffron.y1 + 700; y += 10) if (C.paintedAt({ x, y }) === "crimson-divide") return y; return NaN; };
  const ys = [-1600, -1200, -800, -400, 0].map(x => lineAt(x));
  assert(ys.every(Number.isFinite) && Math.max(...ys) - Math.min(...ys) > 120, "The line between Saffron's and Crimson's pictures wanders: " + ys.join(","));
  // A region keeps its picture round its own roads and places even where a neighbour's reaches in.
  // (The High Stoneveil's road down to the Foothills, and the Sunward Coast's in from the Foothills, where a neighbour's
  // picture would reach in.)
  for (const [id, p] of [["high-stoneveil", { x: 802, y: 978 }], ["sunward-coast", { x: 42, y: 493 }]]) {
    assert(C.cover(C.toC(id, p), id) > 0.99, `${id}'s picture keeps its road at ${p.x},${p.y}`);
  }
  // The empty regions are parchment on the backdrop.
  const back = MapContinent.backdrop(), steppe = C.RECTS["uncharted-steppe"], centre = { x: (steppe.x0 + steppe.x1) / 2, y: (steppe.y0 + steppe.y1) / 2 };
  const pixel = back.canvas.getContext("2d").getImageData(Math.round((centre.x - back.x0) / back.CELL), Math.round((centre.y - back.y0) / back.CELL), 1, 1).data;
  assert(pixel[3] === 255 && pixel[0] > 200 && pixel[1] > 190 && pixel[2] > 160, "The empty regions are parchment (W29): " + [...pixel].join(","));
  const region = MapTerrain.world(WorldRules.getRegion("deep-desert"));
  assert(region.extent.x0 < 0 && region.extent.y0 < -400 && region.extent.y1 > 1400, "A region's world reaches past its own map to its coast");
  assert(back.coast instanceof Path2D, "The coastline is drawn from the land");
  return { checks: count() };
}

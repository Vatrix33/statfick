/* Port task 6, New Areas (2026-11-06): the Abyssal Rift, a demon/abyss zone in Gloamfen Marsh's south-west, and its
   six enemies. The design answers N1–N10 are in engine/Docs/Engine-Plan.md, "Decisions for task 6, New Areas";
   old-game/Docs/Notes.md, "New Areas", says where each rule lives. One entry per group of rules. */
const riftRules = {
  REGION: "gloamfen-marsh", ZONE: "Abyssal Rift", AREA: { x: 380, y: 885, rx: 215, ry: 88 },
  // The approved draft (N9): level, race, the marsh creature a corrupted one comes from, its weapons, spells and the
  // classes that carry its way of fighting.
  ENEMIES: {
    "Corrupted Bog Troll": { level: 47, raceId: "demon", base: "Bog Troll", primaryHand: "forge-weapon-2-greataxe+axe", offHand: null, natural: null, attackElement: null, spells: [], style: ["brawler", "guardian"] },
    "Corrupted Mire Stalker": { level: 50, raceId: "demon", base: "Mire Stalker", primaryHand: "forge-weapon-1-dagger", offHand: "forge-weapon-1-dagger", natural: null, attackElement: "dark", spells: [], style: ["ambusher", "hunter"] },
    "Corrupted Mossback Lamia": { level: 53, raceId: "demon", base: "Mossback Lamia", primaryHand: "forge-weapon-2-spear+lance", offHand: null, natural: null, attackElement: null, spells: ["dark-bolt-dark-damage", "dark-siphon-dark-damage", "abyssal-bolt-dark-damage", "night-mantle-dark-support"], style: ["warlock"] },
    "Abyssal Imp": { level: 46, raceId: "impid", base: null, primaryHand: null, offHand: null, natural: "beast-claws", attackElement: null, spells: ["dark-bolt-dark-damage", "hellstride-dark-support"], style: ["rogue", "warlock"] },
    "Rift Fiend": { level: 50, raceId: "demon", base: null, primaryHand: null, offHand: null, natural: "beast-claws", attackElement: null, spells: [], style: ["warrior", "berserker", "brawler"] },
    "Abyss Witch": { level: 54, raceId: "demon", base: null, primaryHand: null, offHand: null, natural: null, attackElement: null, spells: ["abyssal-bolt-dark-damage", "dark-siphon-dark-damage", "spellstop-dark-support", "night-mantle-dark-support"], style: ["warlock", "spellcaster", "mage"] },
  },
  GROUPS: [
    [45, 9, "Abyssal Imp+Abyssal Imp"], [47, 9, "Corrupted Bog Troll"], [48, 7, "Corrupted Bog Troll+Abyssal Imp"],
    [50, 8, "Corrupted Mire Stalker"], [50, 8, "Rift Fiend"], [52, 7, "Rift Fiend+Abyssal Imp+Abyssal Imp"],
    [53, 8, "Corrupted Mossback Lamia"], [54, 7, "Abyss Witch"], [55, 6, "Corrupted Mossback Lamia+Rift Fiend"],
  ],
  counter() {
    let checks = 0;
    return { assert: (condition, message) => { if (!condition) throw Error(message); checks++; }, count: () => checks };
  },
  inside: (point, area) => ((point.x - area.x) / area.rx) ** 2 + ((point.y - area.y) / area.ry) ** 2 <= 1,
  // A character of this level standing at this point of the marsh.
  stand(point, playerLevel = 50) {
    Combat.restore(null);
    const character = defaultCharacter();
    character.raceId = "human"; character.level = playerLevel;
    applyCharacter(normalizeCharacter(character));
    WorldMap.setAmbientEnabled(false);
    worldState.regionId = riftRules.REGION; WorldRules.setActiveRegion(riftRules.REGION);
    worldState.position = { ...point }; worldState.destination = null; worldState.route = [];
    worldState.pendingEncounter = null;
    Guild.onPositionChange();
  },
  // The zones as they were before the Rift: the same rules with the Rift taken out for a moment.
  withoutRift(action) {
    const region = WorldRules.getRegion(riftRules.REGION), area = region.abyssalRift;
    delete region.abyssalRift;
    try { return action(); } finally { region.abyssalRift = area; }
  },
  // Combat's own functions, from a second copy of combat.js with a hook (as the Artifact Rework checks do).
  async hooks() {
    if (window.__riftChecks) return window.__riftChecks;
    Combat.restore(null);
    const source = await (await fetch("scripts/combat.js")).text();
    const marker = "  window.Combat = {";
    const script = document.createElement("script");
    script.textContent = source.replace(marker, "  window.__riftChecks = { actorById, hp, setHp, getBattle: () => battle, attack, resistance, validateEnemy, rollEnemyArtifact, estimatedWeaponStrike, attackLoadout };\n" + marker);
    document.head.append(script);
    await waitForSourceFiles();
    if (window.BattleStage) window.BattleStage.enabled = () => false;
    if (window.GameSettings) window.GameSettings.enemyDelay = () => 1e9;
    return window.__riftChecks;
  },
  // An enemy's template weapon ("forge-weapon-2-greataxe+axe") is forged at its level when it spawns, as one of the
  // template's kinds ("weapon-forged-greataxe-46-…").
  forgedFrom(template, forged) {
    if (template === null || forged === null) return template === forged;
    const kinds = String(template).replace(/^forge-weapon-\d+-/, "").split("+");
    return kinds.includes(String(forged).match(/^weapon-forged-([a-z-]+?)-\d+-/)?.[1]);
  },
  async battle(h, enemies, playerLevel = 50) {
    Combat.restore(null);
    applyCharacter(normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: playerLevel }));
    if (!await Combat.startEncounter(enemies, "Abyssal Rift test")) throw Error("The Rift test battle starts");
    const battle = h.getBattle();
    return { battle, player: h.actorById("player"), foes: battle.actors.filter(actor => actor.team === "enemy") };
  },
};

// N1–N3, N10: the zone's place and shape, what it took from the marsh, its levels, encounter chance and travel.
async function checkRiftZone() {
  const { assert, count } = riftRules.counter();
  const { REGION, ZONE, AREA } = riftRules;
  const region = WorldRules.getRegion(REGION);
  assert(JSON.stringify(region.abyssalRift) === JSON.stringify(AREA), "The Rift lies in the marsh's south-west at (380, 885), 215 by 88");
  assert(WorldRules.zoneAt({ x: 380, y: 885 }, REGION) === ZONE, "The Rift's heart is the Abyssal Rift");
  for (const [point, inside] of [[{ x: 380 - 214, y: 885 }, true], [{ x: 380 + 214, y: 885 }, true], [{ x: 380, y: 885 - 87 }, true], [{ x: 380, y: 885 + 74 }, true],
    [{ x: 380 - 220, y: 885 }, false], [{ x: 380 + 220, y: 885 }, false], [{ x: 380, y: 885 - 92 }, false]]) {
    assert((WorldRules.zoneAt(point, REGION) === ZONE) === inside, `(${point.x}, ${point.y}) is ${inside ? "inside" : "outside"} the Rift`);
  }
  // Every point of the marsh: inside the Rift it took only open mire (no town, road or other zone moved); outside
  // it nothing changed.
  const before = riftRules.withoutRift(() => {
    const zones = [];
    for (let y = 0; y <= 1000; y += 8) for (let x = 0; x <= 1600; x += 8) zones.push(WorldRules.zoneAt({ x, y }, REGION));
    return zones;
  });
  let index = 0, taken = 0;
  for (let y = 0; y <= 1000; y += 8) for (let x = 0; x <= 1600; x += 8) {
    const was = before[index++], now = WorldRules.zoneAt({ x, y }, REGION);
    if (riftRules.inside({ x, y }, AREA)) { assert(was === "Open Mire" && now === ZONE, `(${x}, ${y}) was Open Mire (${was}) and is the Rift (${now})`); taken++; }
    else assert(now === was, `(${x}, ${y}) keeps its zone ${was} (now ${now})`);
  }
  assert(taken > 900, `The scan covered the Rift (${taken} points)`);
  for (const place of WorldRules.settlementsForRegion(REGION)) assert(!riftRules.inside(place, { ...AREA, rx: AREA.rx + 60, ry: AREA.ry + 60 }), `${place.name} lies well outside the Rift`);
  // Levels 45–55 (N3), a 50% chance (the Deep Mire's is 45%), less when far above it as everywhere.
  const profile = EncounterRules.profileFor(ZONE, REGION);
  assert(profile?.minimum === 45 && profile.maximum === 55 && !profile.safe, "The Rift is a Level 45–55 zone, not a safe one");
  assert(EncounterRules.encounterChance(ZONE, { level: 50, regionId: REGION }) === 50, "The Rift's encounter chance is 50%");
  assert(EncounterRules.encounterChance(ZONE, { level: 65, regionId: REGION }) === 35 && EncounterRules.encounterChance(ZONE, { level: 75, regionId: REGION }) === 20, "Ten and twenty Levels above the Rift, its chance falls as other zones' do");
  const chances = { "Open Mire": 29, "Reedwater": 35, "Deep Mire": 45, "Rocky Rise": 38 };
  for (const [zone, chance] of Object.entries(chances)) assert(EncounterRules.encounterChance(zone, { level: 40, regionId: REGION }) === chance, `${zone} keeps its ${chance}% chance`);
  // Travel through it is the open mire's pace.
  assert(WorldRules.speedAt({ x: 380, y: 885 }, 0, REGION) === 3 && WorldRules.speedAt({ x: 380, y: 885 }, 0, REGION) === riftRules.withoutRift(() => WorldRules.speedAt({ x: 380, y: 885 }, 0, REGION)), "Crossing the Rift is as slow as the open mire it replaced");
  // N10: the marsh's range takes the Rift in: the map header and the guild quests' level.
  assert(region.levelRange.minimum === 35 && region.levelRange.maximum === 55 && region.eyebrow === "GLOAMFEN MARSH · LEVEL 35–55", "Gloamfen Marsh is a Level 35–55 region");
  assert(GuildQuestRules.questLevel(60, REGION) === 55 && GuildQuestRules.questLevel(53, REGION) === 53 && GuildQuestRules.questLevel(20, REGION) === 35, "Marsh guild quests go up to Level 55");
  return count();
}

// N3, N9: the nine encounter groups, their levels and weights, and the enemies a travel encounter brings.
async function checkRiftEncounters() {
  const { assert, count } = riftRules.counter();
  const { REGION, ZONE, ENEMIES, GROUPS } = riftRules;
  const table = (EncounterRules.gloamfenTables[ZONE] || []).map(entry => [entry.level, entry.weight, entry.create(() => 0).join("+")]);
  assert(JSON.stringify(table) === JSON.stringify(GROUPS), `The Rift has the nine approved groups (${JSON.stringify(table)})`);
  for (const value of [0.3, 0.7, 0.999]) assert(JSON.stringify((EncounterRules.gloamfenTables[ZONE] || []).map(entry => entry.create(() => value).join("+"))) === JSON.stringify(GROUPS.map(group => group[2])), "Every group is fixed, not rolled");
  assert(JSON.stringify(EncounterRules.enemiesForZone(ZONE, REGION).sort()) === JSON.stringify(Object.keys(ENEMIES).sort()), "The Rift's enemies are the six new ones");
  for (const [name, expected] of Object.entries(ENEMIES)) assert(EncounterRules.enemyLevels[name] === expected.level, `${name} is listed at Level ${expected.level}`);
  for (const zone of ["Open Mire", "Reedwater", "Deep Mire", "Rocky Rise"]) {
    assert(!EncounterRules.enemiesForZone(zone, REGION).some(name => name in ENEMIES), `${zone} brings none of the Rift's enemies`);
  }
  // The groups nearest the player's level, held to 45–55.
  const sampled = level => {
    const seen = new Set();
    for (let i = 0; i < 400; i++) { const roll = (i * 0.6180339887) % 1; seen.add(EncounterRules.select(ZONE, { level, regionId: REGION }, () => roll).level); }
    return [...seen].sort((a, b) => a - b).join();
  };
  assert(sampled(30) === "45,47", `Far below it, the Rift's lowest groups come (${sampled(30)})`);
  assert(sampled(80) === "53,54,55", `Far above it, its highest groups come (${sampled(80)})`);
  assert(sampled(50) === "48,50,52", `At Level 50 the groups of Levels 48–52 come (${sampled(50)})`);
  // A travel encounter in the Rift is one of its groups.
  riftRules.stand({ x: 380, y: 885 });
  const random = Math.random;
  try {
    Math.random = () => 0;
    assert(WorldMap.checkTravelEncounter(WorldRules.ENCOUNTER_MINUTES), "Travel through the Rift meets an encounter");
    const enemies = worldState.pendingEncounter?.enemies || [];
    assert(enemies.length && enemies.every(name => name in ENEMIES), `The encounter is a Rift group (${enemies.join(", ")})`);
  } finally { Math.random = random; worldState.pendingEncounter = null; }
  return count();
}

// N5, N6, N7, N9: the six enemies as approved: levels, races, classes within their caps, gear, spells, no special loot.
async function checkRiftEnemies() {
  const { assert, count } = riftRules.counter();
  const h = await riftRules.hooks();
  const index = await (await fetch("enemies/index.json")).json();
  const random = Math.random;
  try {
    for (const [name, expected] of Object.entries(riftRules.ENEMIES)) {
      assert(index.includes(name + ".json"), `${name} is in the enemy index`);
      const raw = await (await fetch("enemies/" + encodeURIComponent(name + ".json"))).json();
      assert(raw.name === name && raw.level === expected.level && raw.raceId === expected.raceId, `${name} is a Level ${expected.level} ${expected.raceId}`);
      const classes = Object.entries(raw.classLevels);
      assert(classes.reduce((sum, [, levels]) => sum + levels, 0) === expected.level, `${name}'s class levels add up to its level`);
      for (const [id, levels] of classes) assert(getClass(id) && levels >= 1 && levels <= getClass(id).maxLevel, `${name}'s ${id} ${levels} is a real class within its cap (${getClass(id)?.maxLevel})`);
      for (const id of expected.style) assert(raw.classLevels[id] > 0, `${name} fights as a ${id}`);
      assert(Object.values(raw.stats).length === 8 && Object.values(raw.stats).every(value => value > 30), `${name} has all eight stats`);
      assert(!raw.unique && !(raw.drops || []).length && !(raw.noDropItems || []).length, `${name} carries no special loot (N7)`);
      const spells = raw.knownSpells || [];
      assert(JSON.stringify(spells) === JSON.stringify(expected.spells), `${name} knows the drafted spells (${spells.join(", ")})`);
      assert(spells.every(id => getSpell(id)?.element === "dark"), `${name}'s spells are all Dark`);
      // In battle: the same, armed as drafted.
      const { foes } = await riftRules.battle(h, [name]);
      const foe = foes[0];
      assert(foes.length === 1 && foe.character.level === expected.level && foe.character.raceId === expected.raceId, `${name} enters battle at Level ${expected.level}`);
      const { primaryHand, offHand } = foe.character.equipment;
      assert(riftRules.forgedFrom(expected.primaryHand, primaryHand) && riftRules.forgedFrom(expected.offHand, offHand) && (foe.naturalWeapon || null) === expected.natural, `${name} fights with its drafted weapons (${primaryHand}, ${offHand}, ${foe.naturalWeapon})`);
      assert((foe.attackElement || null) === expected.attackElement, `${name}'s strikes are ${expected.attackElement || "its weapon's"}`);
      assert(!foe.dropTable.length && !foe.unique, `${name} has only the usual loot`);
      Math.random = () => 0;
      assert(String(h.rollEnemyArtifact(foe) || "").startsWith("artifact-"), `${name} can drop an artifact like any enemy (task 4's 1%)`);
      Math.random = random;
      // The corrupted ones are their marsh creature turned demon (N6): its classes and gear, Holy hurts more, Dark not at all.
      if (expected.base) {
        const base = await (await fetch("enemies/" + encodeURIComponent(expected.base + ".json"))).json();
        assert(base.raceId !== "demon" && base.level < expected.level, `${name} is a stronger ${expected.base}`);
        const natureClasses = ["druid", "botanomancer"];
        for (const id of Object.keys(base.classLevels).filter(id => !natureClasses.includes(id))) assert(raw.classLevels[id] >= base.classLevels[id], `${name} keeps the ${expected.base}'s ${id}`);
        assert(JSON.stringify(raw.equipment) === JSON.stringify(base.equipment) && JSON.stringify(raw.inventory) === JSON.stringify(base.inventory), `${name} carries the ${expected.base}'s gear and nothing more`);
        assert(!Object.keys(raw.classLevels).some(id => natureClasses.includes(id)) && !spells.some(id => getSpell(id)?.element === "nature"), `${name} has no nature classes or spells left`);
      }
      // (Their classes add a little on top: the Monk's 5% off every spell, a warlock's Dark ward.)
      else assert(!raw.inventory.length && Object.values(raw.equipment).every(slot => slot === null), `${name} carries no items (its claws or spells are its weapons)`);
      if (expected.raceId === "demon") assert(h.resistance(foe, "dark", true) >= 100 && h.resistance(foe, "holy", true) <= -70, `${name} takes no Dark spell damage and more from Holy (${h.resistance(foe, "dark", true)}, ${h.resistance(foe, "holy", true)})`);
    }
  } finally { Math.random = random; Combat.restore(null); }
  return count();
}

// N9: the Corrupted Mire Stalker's strikes are Dark, and its AI foresees them so; an unknown element is refused; a saved
// battle keeps it.
async function checkRiftDarkStrikes() {
  const { assert, count } = riftRules.counter();
  const h = await riftRules.hooks();
  const random = Math.random;
  const strike = (battle, foe, player, resistances) => {
    player.resistances = resistances; h.setHp(player, 1e6);
    battle.current = foe.id; battle.ap = 9;
    const before = h.hp(player);
    h.attack(foe, player);
    return before - h.hp(player);
  };
  // Each strike opens a fresh battle: an ambusher's first strike hits harder than its next.
  const firstStrike = async (name, resistances) => { const { battle, player, foes } = await riftRules.battle(h, [name]); return strike(battle, foes[0], player, resistances); };
  try {
    Math.random = () => 0;
    const plain = await firstStrike("Corrupted Mire Stalker", {});
    const resisted = await firstStrike("Corrupted Mire Stalker", { dark: 100 });
    const halved = await firstStrike("Corrupted Mire Stalker", { dark: 50 });
    assert(plain > 0 && resisted === 0, `Dark resistance stops the Stalker's strike (${plain} → ${resisted})`);
    assert(Math.abs(halved - plain / 2) <= 1, `Half Dark resistance halves it (${plain} → ${halved})`);
    // The enemy AI's forecast of the strike knows it is Dark, as it does each strike's own element.
    const forecast = async (name, resistances, critical = false) => {
      const { player, foes } = await riftRules.battle(h, [name]);
      player.resistances = resistances;
      const result = h.estimatedWeaponStrike(foes[0], player, h.attackLoadout(foes[0]).primary);
      return critical ? result.criticalDamage : result.normalDamage;
    };
    for (const critical of [false, true]) {
      const open = await forecast("Corrupted Mire Stalker", {}, critical), guarded = await forecast("Corrupted Mire Stalker", { dark: 100 }, critical);
      assert(open > 0 && guarded === 0, `The AI foresees Dark resistance stopping the Stalker's ${critical ? "critical" : "normal"} strike (${open} → ${guarded})`);
      const kin = await forecast("Mire Stalker", {}, critical), kinGuarded = await forecast("Mire Stalker", { dark: 100 }, critical);
      assert(kin > 0 && kinGuarded === kin, `The AI foresees the Mire Stalker's ${critical ? "critical" : "normal"} strike untouched by Dark resistance (${kin} → ${kinGuarded})`);
    }
    // A battle saved and restored keeps the Dark strikes.
    let { battle, player, foes } = await riftRules.battle(h, ["Corrupted Mire Stalker"]);
    Combat.restore(Combat.serialize());
    battle = h.getBattle(); player = h.actorById("player"); foes = battle.actors.filter(actor => actor.team === "enemy");
    assert(foes[0].attackElement === "dark" && strike(battle, foes[0], player, { dark: 100 }) === 0, "A restored battle keeps the Stalker's Dark strikes");
    // Its uncorrupted kin and the other Rift enemies strike with their weapons, which Dark resistance doesn't touch.
    for (const name of ["Mire Stalker", "Corrupted Bog Troll", "Rift Fiend"]) {
      const open = await firstStrike(name, {}), guarded = await firstStrike(name, { dark: 100 });
      assert(open > 0 && guarded === open, `${name}'s strikes are not Dark (${open} → ${guarded})`);
    }
    // The definition is checked when enemies load.
    const raw = await (await fetch("enemies/" + encodeURIComponent("Corrupted Mire Stalker.json"))).json();
    assert(h.validateEnemy(structuredClone(raw)).attackElement === "dark", "A Dark strike element loads");
    assert(h.validateEnemy({ ...structuredClone(raw), attackElement: undefined }).attackElement === null, "No strike element means the weapon's");
    let refused = "";
    try { h.validateEnemy({ ...structuredClone(raw), attackElement: "shadowfire" }); } catch (error) { refused = error.message; }
    assert(/unknown attack element shadowfire/.test(refused), `An unknown strike element is refused (${refused})`);
  } finally { Math.random = random; Combat.restore(null); }
  return count();
}

// N4, N9: placeholder looks from the existing kits: the corrupted ones darkened with red eyes, the demons their own.
async function checkRiftLooks() {
  const { assert, count } = riftRules.counter();
  const svgs = new Set();
  for (const [name, expected] of Object.entries(riftRules.ENEMIES)) {
    assert(Sprites.enemyNames().includes(name), `${name} has its own look`);
    const look = Sprites.forActor({ enemyName: name, raceId: expected.raceId });
    assert(look.svg !== Sprites.forActor({ raceId: expected.raceId }).svg, `${name} doesn't fall back to the ${expected.raceId} look`);
    svgs.add(look.svg);
    if (expected.base) {
      const base = Sprites.forActor({ enemyName: expected.base }).spec;
      assert(look.spec.race === base.race && look.spec.lower === base.lower && look.spec.head === base.head, `${name} is drawn as a ${expected.base}`);
      assert(look.spec.eyes?.style === "glow" && look.spec.eyes.color === "#ff4a4a" && base.eyes?.color !== "#ff4a4a", `${name} has the corrupted red eyes`);
      assert(look.spec.skin !== base.skin, `${name}'s body is darkened`);
    }
  }
  assert(svgs.size === 6, "The six looks differ");
  const spec = name => Sprites.forActor({ enemyName: name }).spec;
  assert(spec("Corrupted Bog Troll").voidVeins === "#9a5cc8", "The Corrupted Bog Troll has cracked violet veins");
  assert(spec("Abyssal Imp").race === "impid" && spec("Abyssal Imp").wings && spec("Abyssal Imp").horns, "The Abyssal Imp is drawn as a winged, horned imp");
  assert(spec("Rift Fiend").race === "devil" && spec("Rift Fiend").size > 1.3 && spec("Rift Fiend").horns?.style === "long" && spec("Rift Fiend").eyes?.color === "#ff4a4a", "The Rift Fiend is a large horned demon with red eyes");
  assert(spec("Abyss Witch").outfit?.top === "robe" && spec("Abyss Witch").aura && spec("Abyss Witch").eyes?.color === "#ff4a4a", "The Abyss Witch is a robed demon with a violet glow and red eyes");
  return count();
}

// N1, N4: the map shows the Rift (a placeholder painting and its label), and the map panel names the zone.
async function checkRiftMap() {
  const { assert, count } = riftRules.counter();
  const { REGION, ZONE, AREA } = riftRules;
  const spec = MapRegions.spec(REGION), region = WorldRules.getRegion(REGION);
  const label = spec.labels.find(entry => entry.text === "ABYSSAL RIFT");
  assert(label && riftRules.inside(label, AREA), "The map labels the Abyssal Rift inside it");
  // The painting: dark and red-violet at the heart, the open mire's water and scrub gone; elsewhere untouched.
  const T = MapTerrain.makeToolkit(7), withRift = spec.prepare(region, T), without = { ...withRift, rift: undefined };
  const field = (x, y, context) => { const out = { color: [0, 0, 0], h: 0, water: 0, forest: 0, pine: 0, rock: 0, wet: 0 }; spec.field(x, y, out, T, context, region); return { ...out, color: [...out.color] }; };
  const brightness = color => color[0] + color[1] + color[2];
  for (const point of [{ x: 380, y: 885 }, { x: 330, y: 880 }, { x: 440, y: 895 }]) {
    const rift = field(point.x, point.y, withRift), mire = field(point.x, point.y, without);
    assert(brightness(rift.color) < brightness(mire.color) * 0.75 && rift.color[0] > rift.color[1] && rift.color[2] > rift.color[1], `The Rift is painted dark red-violet at (${point.x}, ${point.y}) (${rift.color.map(Math.round)} over ${mire.color.map(Math.round)})`);
    assert(rift.forest <= mire.forest * 0.2 + 1e-9 && rift.water <= Math.max(0.1, mire.water * 0.3) && rift.wet <= mire.wet * 0.2 + 1e-9, `No scrub, pools or wet ground in the Rift at (${point.x}, ${point.y})`);
  }
  // The mire's pools inside the Rift dry up: every spot of its inner part where the open mire had a pool has none.
  let pools = 0;
  for (let y = AREA.y - AREA.ry * 0.6; y <= AREA.y + AREA.ry * 0.6; y += 3) for (let x = AREA.x - AREA.rx * 0.6; x <= AREA.x + AREA.rx * 0.6; x += 3) {
    if (!riftRules.inside({ x, y }, { ...AREA, rx: AREA.rx * 0.6, ry: AREA.ry * 0.6 })) continue;
    const mire = field(x, y, without);
    if (mire.water < 0.5) continue;
    pools++;
    const rift = field(x, y, withRift);
    assert(rift.water < 0.5 && rift.water <= mire.water * 0.3, `The pool at (${x}, ${y}) is gone in the Rift (${rift.water.toFixed(2)} over ${mire.water.toFixed(2)})`);
  }
  assert(pools >= 20, `The open mire had pools there to dry up (${pools} spots)`);
  for (const point of [{ x: 790, y: 580 }, { x: 310, y: 555 }, { x: 1310, y: 245 }, { x: 765, y: 770 }]) {
    assert(JSON.stringify(field(point.x, point.y, withRift)) === JSON.stringify(field(point.x, point.y, without)), `The map is unchanged away from the Rift (${point.x}, ${point.y})`);
  }
  // The map panel at the Rift.
  riftRules.stand({ x: 380, y: 885 });
  try {
    WorldMap.open(); WorldMap.render();
    const text = id => document.getElementById(id).textContent;
    assert(text("map-zone") === ZONE && text("map-zone-level") === "Lv 45–55", `The map names the zone and its levels (${text("map-zone")} · ${text("map-zone-level")})`);
    assert(text("map-location") === "Abyssal Rift · 3 km/h", `The map shows where you are (${text("map-location")})`);
    assert(text("map-region-eyebrow") === "GLOAMFEN MARSH · LEVEL 35–55", "The map header gives the marsh's new range");
  } finally { document.querySelector("#map-modal [data-map-close]")?.click(); }
  return count();
}

// N1: the marsh's guilds send hunters into the Rift once they can face it.
async function checkRiftGuild() {
  const { assert, count } = riftRules.counter();
  const { REGION, ZONE, ENEMIES } = riftRules;
  const mirewatch = WorldRules.settlementsForRegion(REGION).find(place => place.id === "mirewatch");
  const offers = playerLevel => {
    riftRules.stand(mirewatch, playerLevel);
    return Array.from({ length: 160 }, () => Guild.makeOffer("hunt")).filter(Boolean);
  };
  // A Hunt's target is a Rift enemy near the hunter's level, as in every zone.
  const targets = { 46: "Abyssal Imp,Corrupted Bog Troll", 50: "Corrupted Mire Stalker,Rift Fiend", 55: "Abyss Witch,Corrupted Mossback Lamia" };
  for (const [playerLevel, names] of Object.entries(targets)) {
    const rift = offers(Number(playerLevel)).filter(offer => offer.zone === ZONE);
    assert(rift.length >= 10, `A Level ${playerLevel} hunter at Mirewatch is sent into the Rift (${rift.length} of 160 Hunts)`);
    assert(rift.every(offer => offer.enemy in ENEMIES && offer.regionId === REGION && WorldRules.zoneAt(offer.point, REGION) === ZONE), "Every Rift Hunt targets a Rift enemy at a spot in the Rift");
    const named = [...new Set(rift.map(offer => offer.enemy))].sort().join();
    assert(named === names, `At Level ${playerLevel} the Rift Hunts name ${names} (${named})`);
  }
  assert(offers(60).every(offer => offer.level === 55), "A Level 60 hunter's marsh quests are Level 55 (N10)");
  assert(!offers(41).some(offer => offer.zone === ZONE), "Below Level 42 no Hunt goes into the Rift");
  assert(offers(42).some(offer => offer.zone === ZONE), "From Level 42 Hunts go into the Rift");
  return count();
}

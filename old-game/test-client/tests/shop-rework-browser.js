// Port task 3, the Shop Rework (2026-11-06): one entry per group of the user's decisions R1–R27
// (engine/Docs/Engine-Plan.md, "Decisions for task 3, Shop Rework").
const shopReworkWait = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));
function shopReworkAssert() {
  let checks = 0;
  const assert = (condition, message) => { if (!condition) throw Error(message); checks++; };
  return { assert, count: () => checks };
}
// Combat's own functions, exposed by loading a second copy of combat.js with a hook (as the Ornn checks do).
const shopReworkCombatHooks = async () => {
  if (window.__shopReworkChecks) return window.__shopReworkChecks;
  Combat.restore(null);
  const source = await (await fetch("scripts/combat.js")).text();
  const marker = "  window.Combat = {";
  const script = document.createElement("script");
  script.textContent = source.replace(marker, "  window.__shopReworkChecks = { actorById, hp, values, setHp, getBattle: () => battle, healingFactor, healingAmount, healHp, hurt, attack, cast, useItem, advance, expire, applyTimedEffects, actionPointMaximum, currentActor, triggerControlTraps };\n" + marker);
  document.head.append(script);
  await waitForSourceFiles();
  if (window.BattleStage) window.BattleStage.enabled = () => false;
  if (window.GameSettings) window.GameSettings.enemyDelay = () => 1e9;
  return window.__shopReworkChecks;
};

// R11–R13, R24, R25, R27: Doctor's Orders in battle.
async function checkDoctorsOrdersHealing() {
  const { assert, count } = shopReworkAssert();
  const h = await shopReworkCombatHooks();
  const robe = ForgeRules.encode(ForgeRules.generateMasterwork({ key: "doctors-orders", level: 30 }));
  const item = getItem(robe);
  assert(item?.forged?.masterwork && item.type === "clothing" && item.forged.type === "robe" && ForgeRules.MASTERWORKS["doctors-orders"].placeholderArt === true && item.forged.parts[0].maker === "institute-of-magic" && item.forged.masterwork === "doctors-orders" && item.name.endsWith("Doctor's Orders"), "Doctor's Orders is an Institute of Magic masterwork robe");
  assert(["healing-given-percent:50", "healing-received-percent:25"].every(text => item.effects.some(effect => `${effect.kind}:${effect.amount}` === text)), "It carries +50% healing given and +25% healing received");
  const start = async wearing => {
    Combat.restore(null);
    applyCharacter(normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: 30, inventory: [robe, "consumable-potion-of-lesser-health", "consumable-potion-of-lesser-health"] }));
    if (wearing) equipment.armor = robe;
    assert(await Combat.startEncounter(["Goblin", "Goblin"], "Doctor's Orders test"), "The test battle starts");
    const battle = h.getBattle();
    return { battle, player: h.actorById("player"), goblins: battle.actors.filter(actor => actor.team === "enemy") };
  };
  // The factors (R12, R13, R25, R27).
  let { battle, player, goblins } = await start(true);
  const [goblin, other] = goblins;
  assert(h.healingFactor(player, player) === 1.5, "A heal the wearer gives themselves takes only the larger bonus, ×1.5");
  assert(h.healingFactor(player, goblin) === 1.25 && h.healingFactor(player, null) === 1.25, "Heals the wearer receives from anyone else are 25% stronger");
  assert(h.healingFactor(goblin, player) === 1.5, "Heals the wearer gives others are 50% stronger");
  assert(h.healingFactor(goblin, other) === 1 && h.healingFactor(goblin, goblin) === 1, "Heals without a wearer are unchanged");
  const twin = JSON.parse(JSON.stringify(goblin)); twin.id = "twin"; twin.character.equipment.armor = robe; twin.character.inventory = [robe];
  const twinRobe = JSON.parse(JSON.stringify(goblin)); twinRobe.id = "twin-robe"; twinRobe.character.equipment.armor = robe; twinRobe.character.inventory = [robe];
  assert(h.healingFactor(twin, twinRobe) === 1.875, "One wearer healing another takes both bonuses, ×1.875");
  // A potion the wearer drinks (an item they use): ×1.5 over the same potion without the robe.
  const potionGain = async wearing => {
    ({ battle, player } = await start(wearing));
    battle.current = "player"; battle.ap = 5; h.setHp(player, 1);
    assert(h.useItem(player, player, getItem("consumable-potion-of-lesser-health")), "The potion is drunk");
    return h.hp(player) - 1;
  };
  const plainPotion = await potionGain(false), robePotion = await potionGain(true);
  assert(plainPotion > 0 && robePotion === Math.floor(plainPotion * 1.5), `The wearer's own potion heals ×1.5 (${plainPotion} → ${robePotion})`);
  // Lifesteal and regeneration the wearer causes count as given (R24): ×1.5 on themselves.
  ({ battle, player, goblins } = await start(true));
  h.setHp(player, 1);
  assert(h.values(player).maxHp > 40 && h.healHp(player, 20, battle, player) === 30, "The wearer's own lifesteal or regeneration heals ×1.5");
  h.setHp(player, 1);
  assert(h.healHp(player, 20, battle, goblins[0]) === 25, "A heal from someone else heals the wearer ×1.25");
  // A heal-over-time remembers its caster: on the wearer, their own ticks heal ×1.5, someone else's ×1.25.
  const tick = caster => {
    player.effects = player.effects.filter(effect => effect.name !== "Test Bloom");
    h.applyTimedEffects(player, "test-hot", "Test Bloom", [{ type: "turn-start-restore-percent", hp: 40, durationTurns: 3 }], caster);
    const base = Math.max(1, Math.floor(h.values(player).maxHp * 40 / 100));
    h.setHp(player, 1); h.expire(player);
    return { base, gain: h.hp(player) - 1 };
  };
  const own = tick(player);
  assert(player.effects.find(effect => effect.name === "Test Bloom")?.healerId === "player" && own.gain === Math.floor(own.base * 1.5) && own.gain !== Math.floor(own.base * 1.25), "The wearer's own heal-over-time ticks heal ×1.5");
  const theirs = tick(goblins[0]);
  assert(theirs.gain === Math.floor(theirs.base * 1.25), "Someone else's heal-over-time ticks heal the wearer ×1.25");
  // The healing-given and healing-received effects read on the item card.
  const text = item.effects.map(artifactEffectLabel).join(" | ");
  assert(/heal you cause in battle/.test(text) && /\+50%/.test(text) && /Healing you receive in battle \+25%/.test(text), "The item card names both effects: " + text);
  Combat.restore(null);
  return { passed: true, checks: count(), plainPotion, robePotion };
}

// R24: heals the wearer causes, through the game's own paths: a racial regeneration, a team-healing passive the
// wearer owns (on a companion), and a spell's lifesteal.
async function checkDoctorsOrdersSources() {
  const { assert, count } = shopReworkAssert();
  const h = await shopReworkCombatHooks();
  const robe = ForgeRules.encode(ForgeRules.generateMasterwork({ key: "doctors-orders", level: 30 }));
  const start = async (wearing, changes = {}, party = []) => {
    Combat.restore(null);
    applyCharacter(normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: 30, inventory: [robe], ...changes }));
    if (wearing) equipment.armor = robe;
    companions = party;
    assert(await Combat.startEncounter(["Goblin"], "Doctor's Orders sources"), "The battle starts");
    return { battle: h.getBattle(), player: h.actorById("player") };
  };
  // A troll's regeneration at the start of its turn is the wearer's own heal: ×1.5.
  const regeneration = async wearing => {
    const { player } = await start(wearing);
    selectedRaceId = "troll";
    h.setHp(player, 1); h.expire(player);
    const gain = h.hp(player) - 1;
    selectedRaceId = "human";
    return gain;
  };
  const plainRegen = await regeneration(false), robeRegen = await regeneration(true);
  assert(plainRegen > 0 && robeRegen === Math.floor(plainRegen * 1.5), `Racial regeneration heals the wearer ×1.5 (${plainRegen} → ${robeRegen})`);
  // Heroic Inspiration (Hero 2) restores the whole side each turn; the wearer's share on a companion is ×1.5.
  const ally = () => ({ id: "companion-doctor-test", name: "Mira", gender: "female", character: { ...defaultCharacter(), raceId: "human", level: 30 } });
  const inspiration = async wearing => {
    const { battle } = await start(wearing, { classLevels: { hero: 2 } }, [ally()]);
    const companion = battle.actors.find(actor => actor.companion || actor.id === "companion-doctor-test");
    assert(companion, "The companion fights beside you");
    h.setHp(companion, 1); h.expire(companion);
    return h.hp(companion) - 1;
  };
  const plainTeam = await inspiration(false), robeTeam = await inspiration(true);
  assert(plainTeam > 0 && robeTeam === Math.floor(h.values(h.getBattle().actors.find(actor => actor.id === "companion-doctor-test")).maxHp * 7.5 / 100), `The wearer's team passive heals a companion ×1.5 (${plainTeam} → ${robeTeam})`);
  companions = [];
  // Drain Blood's lifesteal: half the damage, then the wearer's +50%.
  const drain = async wearing => {
    const { battle, player } = await start(wearing, { knownSpells: ["drain-blood-damage"] });
    const goblin = battle.actors.find(actor => actor.team === "enemy");
    battle.current = "player"; battle.ap = 5; h.setHp(player, 1); currentMana = 999;
    const random = Math.random; Math.random = () => 0.5;
    try { assert(h.cast(player, goblin, getSpell("drain-blood-damage")), "Drain Blood is cast"); } finally { Math.random = random; }
    const damage = Number(battle.log.findLast(entry => /deals \d+ .*damage to/.test(entry))?.match(/deals (\d+)/)?.[1]);
    return { damage, gain: h.hp(player) - 1 };
  };
  const plainDrain = await drain(false), robeDrain = await drain(true);
  assert(plainDrain.gain === Math.floor(plainDrain.damage * 0.5) && robeDrain.gain === Math.floor(Math.floor(robeDrain.damage * 0.5) * 1.5) && robeDrain.gain > 0, `A spell's lifesteal heals the wearer ×1.5 (${JSON.stringify([plainDrain, robeDrain])})`);
  Combat.restore(null);
  return { passed: true, checks: count(), plainRegen, robeRegen, plainTeam, robeTeam };
}

// R17–R21, R26: the five new traps.
async function checkNewTraps() {
  const { assert, count } = shopReworkAssert();
  const h = await shopReworkCombatHooks();
  const traps = { "consumable-tripwire": ["Tripwire", "common", 250], "consumable-smoke-trap": ["Smoke Trap", "uncommon", 900], "consumable-frost-glyph": ["Frost Glyph", "rare", 2800], "consumable-weighted-net": ["Weighted Net", "epic", 6500], "consumable-gravity-well": ["Gravity Well", "legendary", 16000] };
  for (const [id, [name, rarity, price]] of Object.entries(traps)) {
    const trap = getItem(id);
    assert(trap?.name === name && trap.rarity === rarity && trap.priceSilver === price && trap.category === "consumable" && trap.shopAvailable !== false, `${name} is a ${rarity} trap for ${price} silver`);
  }
  const start = async (itemIds, enemies = ["Goblin", "Goblin"], party = []) => {
    Combat.restore(null);
    applyCharacter(normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: 30, inventory: itemIds }));
    companions = party;
    assert(await Combat.startEncounter(enemies, "Trap test"), "The trap battle starts");
    companions = [];
    const battle = h.getBattle(), player = h.actorById("player");
    for (const id of itemIds) {
      battle.current = "player"; battle.ap = 5;
      assert(h.useItem(player, player, getItem(id)), `${getItem(id).name} is armed`);
    }
    return { battle, player, goblins: battle.actors.filter(actor => actor.team === "enemy") };
  };
  const turnOf = (battle, actor) => { for (let index = 0; index < 40; index++) { const next = h.advance(); if (next?.id === actor.id) return battle.ap; } throw Error("No turn for " + actor.id); };
  const strike = (battle, attacker, target) => { battle.current = attacker.id; battle.ap = 5; h.attack(attacker, target); };
  const armed = (actor, type) => actor.effects.some(effect => effect.type === type);
  // Tripwire: a melee attack costs 1 AP on the attacker's next turn; it adds to a stagger (R26).
  let { battle, player, goblins } = await start(["consumable-tripwire"]);
  let [goblin, other] = goblins;
  strike(battle, goblin, player);
  assert(!armed(player, "trap-tripwire") && goblin.trapApLoss === 1 && !other.trapApLoss, "Tripwire triggers on the melee attacker and is spent");
  goblin.staggerAp = 2;
  assert(turnOf(battle, goblin) === h.actionPointMaximum(goblin) - 3 && battle.log.some(entry => entry.includes("caught in the traps and loses 1 AP")), "Its AP loss adds to a stagger on the next turn");
  ({ battle, player, goblins } = await start(["consumable-tripwire"], ["Goblin Archer"]));
  strike(battle, goblins[0], player);
  assert(armed(player, "trap-tripwire") && !goblins[0].trapApLoss, "A ranged attack does not trip it");
  // Weighted Net: a melee attack stuns the attacker for its next turn; stun immunity holds.
  ({ battle, player, goblins } = await start(["consumable-weighted-net"]));
  [goblin] = goblins;
  strike(battle, goblin, player);
  assert(!armed(player, "trap-weighted-net") && goblin.effects.some(effect => effect.type === "stun"), "Weighted Net stuns the melee attacker");
  const turnsBefore = goblin.turns;
  for (let index = 0; index < 40 && goblin.turns === turnsBefore; index++) h.advance();
  assert(battle.log.some(entry => entry.includes("stunned and loses their turn")), "The stunned attacker loses its next turn: " + battle.log.slice(-6).join(" / "));
  ({ battle, player, goblins } = await start(["consumable-weighted-net"]));
  [goblin] = goblins; goblin.character.raceId = "satyr";
  strike(battle, goblin, player);
  assert(!armed(player, "trap-weighted-net") && !goblin.effects.some(effect => effect.type === "stun") && battle.log.some(entry => entry.includes("tears free")), "A stun-immune attacker tears free");
  // Smoke Trap: any attack gives the whole side +20% Evasion until the owner's next turn.
  ({ battle, player, goblins } = await start(["consumable-smoke-trap"], ["Goblin", "Goblin"], [{ id: "companion-smoke-test", name: "Mira", gender: "female", character: { ...defaultCharacter(), raceId: "human", level: 30 } }]));
  [goblin] = goblins;
  const ally = battle.actors.find(actor => actor.team === "player" && actor.id !== "player" && !actor.summoned);
  const evasion = h.values(player).evasion;
  strike(battle, goblin, player);
  const smoked = player.effects.find(effect => effect.smokeOwnerId === "player");
  assert(!armed(player, "trap-smoke") && smoked?.type === "defense-percent" && smoked.target === "evasion" && smoked.amount === 20 && h.values(player).evasion > evasion, "Smoke Trap gives +20% Evasion when attacked");
  assert(ally?.effects.some(effect => effect.smokeOwnerId === "player" && effect.amount === 20) && !goblin.effects.some(effect => effect.smokeOwnerId), "Its allies share the smoke; the enemy doesn't");
  turnOf(battle, player);
  assert(!player.effects.some(effect => effect.smokeOwnerId) && !ally.effects.some(effect => effect.smokeOwnerId), "The smoke lifts at the owner's next turn");
  // R28: if the owner falls first, each ally keeps it until their own next turn.
  ({ battle, player, goblins } = await start(["consumable-smoke-trap"], ["Goblin", "Goblin"], [{ id: "companion-smoke-test", name: "Mira", gender: "female", character: { ...defaultCharacter(), raceId: "human", level: 30 } }]));
  const mira = battle.actors.find(actor => actor.team === "player" && actor.id !== "player" && !actor.summoned);
  strike(battle, goblins[0], player);
  h.setHp(player, 0); h.expire(goblins[1]);
  assert(mira.effects.some(effect => effect.smokeOwnerId === "player"), "A fallen owner's smoke stays on an ally through others' turns");
  h.expire(mira);
  assert(!mira.effects.some(effect => effect.smokeOwnerId), "It lifts at that ally's own next turn");
  ({ battle, player, goblins } = await start(["consumable-smoke-trap"]));
  h.hurt(player, 5, "fire", false, true, goblins[0], "spell");
  assert(!armed(player, "trap-smoke") && player.effects.some(effect => effect.smokeOwnerId === "player"), "A damaging spell is an attack too");
  // Frost Glyph: a damaging spell slows the caster by 25% for 2 turns; melee does not trigger it.
  ({ battle, player, goblins } = await start(["consumable-frost-glyph"]));
  [goblin] = goblins;
  strike(battle, goblin, player);
  assert(armed(player, "trap-frost-glyph"), "Frost Glyph ignores weapon attacks");
  const speed = h.values(goblin).speed;
  h.hurt(player, 5, "fire", false, true, goblin, "spell");
  const slow = goblin.effects.find(effect => effect.type === "stat-percent" && effect.target === "speed");
  assert(!armed(player, "trap-frost-glyph") && slow?.amount === -25 && slow.expiresTurn === goblin.turns + 2 && h.values(goblin).speed < speed, "A damaging spell costs the caster 25% Speed for 2 turns");
  // R29: a spell a barrier soaks entirely still triggers it.
  ({ battle, player, goblins } = await start(["consumable-frost-glyph"]));
  player.barrierHp = 100000;
  const hpBefore = h.hp(player);
  h.hurt(player, 5, "fire", false, true, goblins[0], "spell");
  assert(h.hp(player) === hpBefore && !armed(player, "trap-frost-glyph") && goblins[0].effects.some(effect => effect.type === "stat-percent" && effect.target === "speed" && effect.amount === -25), "An absorbed spell hit triggers it too");
  // Gravity Well: any attack costs every enemy 2 AP on its next turn.
  ({ battle, player, goblins } = await start(["consumable-gravity-well"]));
  [goblin, other] = goblins;
  strike(battle, goblin, player);
  assert(!armed(player, "trap-gravity-well") && goblin.trapApLoss === 2 && other.trapApLoss === 2, "Gravity Well costs every enemy 2 AP");
  assert(turnOf(battle, other) === h.actionPointMaximum(other) - 2, "The AP is lost on that enemy's next turn");
  ({ battle, player, goblins } = await start(["consumable-gravity-well"]));
  h.hurt(player, 5, "fire", false, true, goblins[0], "spell");
  assert(!armed(player, "trap-gravity-well") && goblins.every(foe => foe.trapApLoss === 2), "A spell hit sets it off too");
  // Tripwire and Gravity Well together add up (R26).
  ({ battle, player, goblins } = await start(["consumable-tripwire"]));
  player.effects.push({ key: "trap:test-well", name: "Gravity Well", type: "trap-gravity-well", amount: 2, cap: 0, remainingTriggers: 1, expiresTurn: Number.MAX_SAFE_INTEGER });
  [goblin] = goblins;
  strike(battle, goblin, player);
  assert(goblin.trapApLoss === 3, "Tripwire and Gravity Well cost 3 AP together");
  // They count against the trap limit like the old traps.
  ({ battle, player } = await start(["consumable-tripwire"]));
  battle.current = "player"; battle.ap = 5;
  inventory.push("consumable-smoke-trap");
  assert(!h.useItem(player, player, getItem("consumable-smoke-trap")) && !armed(player, "trap-smoke"), "A second trap is blocked at the base trap limit");
  // The General Store stocks them, rarer ones more often at higher levels (R22).
  assert(Object.keys(traps).every(id => Shop.purchasableItem(getItem(id), WorldRules.getRegion("stoneveil-foothills").settlements[0])), "The General Store can stock all five");
  assert(ShopRules.rarityWeight("legendary", 95) > ShopRules.rarityWeight("legendary", 20) && ShopRules.rarityWeight("common", 10) > ShopRules.rarityWeight("common", 95), "Rarer traps weigh more at higher levels");
  Combat.restore(null);
  return { passed: true, checks: count() };
}

// R2, R3, R5, R6, R16: what the anvils take, which parts may replace which, and what it costs.
async function checkReforgeRules() {
  const { assert, count } = shopReworkAssert();
  const F = ForgeRules;
  const rarityOf = (spec, part) => F.isArmorSpec(spec) ? F.ARMOR_MATERIALS[part.material].rarity : F.MATERIALS[part.material].rarity;
  const index = rarity => F.RARITIES.indexOf(rarity);
  assert(["common:360", "uncommon:1800", "rare:7200", "epic:18000", "legendary:36000", "masterwork:72000"].every(text => { const [rarity, silver] = text.split(":"); return F.REFORGE_COST_TIMES * F.RARITY_PRICE[rarity] === Number(silver); }), "A changed part costs 3× its rarity's part price");
  // Weapons: every shop-made part up to the main part's rarity, never Outlaw or Vampiric.
  for (const rarity of ["uncommon", "rare", "epic", "legendary"]) {
    const sword = F.generate({ type: "longsword", level: 40, rarity, source: "shop" });
    const main = index(rarityOf(sword, sword.parts[0]));
    for (const at of [1, 2]) {
      const options = F.reforgeOptions(sword, at);
      assert(options.length > 3 && options.every(option => option.shape === sword.parts[at].shape && index(rarityOf(sword, option)) <= main && !["outlaw", "vampiric"].includes(option.maker)), `A ${rarity} sword's part ${at + 1} may become any shop-made part up to its main part's rarity`);
      assert(options.some(option => option.maker === sword.parts[at].maker && option.material === sword.parts[at].material), "The current part is among them");
      assert(new Set(options.map(option => index(rarityOf(sword, option)))).size === main + 1, "Every rarity up to the main part's is offered");
    }
    assert(F.reforgeOptions(sword, 0).length === 0, "The main part never changes");
  }
  const sword = F.generate({ type: "longsword", level: 40, rarity: "epic", source: "shop" }), id = F.encode(sword);
  const pick = (spec, at, test) => F.reforgeOptions(spec, at).find(option => !(option.maker === spec.parts[at].maker && option.material === spec.parts[at].material) && test(option));
  const commonPart = pick(sword, 1, option => rarityOf(sword, option) === "common"), rarePart = pick(sword, 2, option => rarityOf(sword, option) === "rare");
  let result = F.reforge(id, [sword.parts[0], commonPart, sword.parts[2]], "smith");
  let next = F.decode(result.id);
  assert(result.changed && result.cost === 360 && next.parts[1].material === commonPart.material && next.parts[0].material === sword.parts[0].material && next.level === sword.level && next.slots === sword.slots, "One changed common part costs 360 silver; main part, level and slots stay");
  result = F.reforge(id, [sword.parts[0], commonPart, rarePart], "smith");
  assert(result.cost === 360 + 7200, "Two changed parts cost each by its new rarity");
  result = F.reforge(id, sword.parts, "smith");
  assert(!result.changed && result.cost === 0, "Unchanged parts are free");
  const otherMain = F.generate({ type: "longsword", level: 40, rarity: "legendary", source: "shop" }).parts[0];
  assert(F.decode(F.reforge(id, [otherMain, commonPart, sword.parts[2]], "smith").id).parts[0].material === sword.parts[0].material, "A different main part is ignored");
  const outlawPart = { shape: sword.parts[1].shape, maker: "outlaw", material: sword.parts[1].material };
  assert(F.reforge(id, [sword.parts[0], outlawPart, sword.parts[2]], "smith").reason, "An Outlaw part is refused");
  const tooRare = { ...sword.parts[1], ...F.partOptions(sword.parts[1].shape, 4, { allowOutlaw: false })[0] };
  assert(F.reforge(id, [sword.parts[0], tooRare, sword.parts[2]], "smith").reason, "A part above the main part's rarity is refused");
  // Which anvil takes what.
  const armor = F.generateArmor({ level: 40, source: "shop", types: ["full-plate"] });
  const clothing = F.generateArmor({ level: 40, source: "shop", types: F.ARMOR_IDS.filter(type => F.ARMORS[type].category === "clothing") });
  const staff = F.generate({ type: "staff", level: 40, rarity: "epic", source: "shop", casterShare: 1 });
  const club = F.generate({ type: "club", level: 40, rarity: "rare", source: "loot" });
  const hammer = F.generateMasterwork({ key: "anvil-hammer", level: 48 });
  const reason = (spec, anvil) => F.reforgeReason(spec, anvil);
  assert(!reason(sword, "smith") && !reason(armor, "smith") && reason(clothing, "smith") && reason(staff, "smith"), "The Blacksmith reforges weapons and armor, not clothing or casters");
  assert(!reason(clothing, "tailor") && reason(sword, "tailor") && reason(armor, "tailor"), "The Tailor reforges clothing only");
  assert(!reason(staff, "institute") && reason(sword, "institute") && reason(clothing, "institute"), "The Institute reforges staffs and wands only");
  assert(!reason(sword, "hearth") && !reason(armor, "hearth") && !reason(hammer, "hearth") && reason(hammer, "smith") && reason(hammer, "tailor"), "Only Hearth Home reforges masterworks");
  assert(club.type === "club" && reason(club, "smith") && reason(club, "hearth"), "A weapon no smith makes parts for (the club) can't be reforged");
  // Armor: no Vampiric or Outlaw parts (they make light armor, clothing and shields), up to the main part's rarity.
  for (const type of ["full-plate", "leather-armor", "kite-shield", "robe"]) {
    const piece = F.generateArmor({ level: 60, source: "shop", types: [type], rarity: "legendary" }), main = index(rarityOf(piece, piece.parts[0]));
    assert([1, 2].every(at => { const options = F.reforgeOptions(piece, at); return options.length > 2 && options.every(option => !["outlaw", "vampiric"].includes(option.maker) && index(rarityOf(piece, option)) <= main); }), `A ${type}'s parts follow the same pool`);
  }
  // A fixed maker forges only its own pieces (Hearth Home's and the Institute's stock).
  for (let roll = 0; roll < 20; roll++) {
    assert(F.generate({ level: 40, source: "shop", casterShare: 0, maker: "hearthforge" }).parts[0].maker === "hearthforge", "Hearthforge weapons have a Hearthforge main part");
    assert(F.generateArmor({ level: 40, source: "shop", maker: "hearthforge", types: F.ARMOR_IDS.filter(type => F.ARMORS[type].category !== "clothing") }).parts[0].maker === "hearthforge", "Hearthforge armor has a Hearthforge main part");
    assert(F.generateArmor({ level: 40, source: "shop", maker: "institute-of-magic", types: F.ARMOR_IDS.filter(type => F.ARMORS[type].category === "clothing") }).parts[0].maker === "institute-of-magic", "Institute clothing has an Institute main part");
    assert(F.generate({ level: 40, source: "shop", casterShare: 1, maker: "institute-of-magic" }).parts[0].maker === "institute-of-magic", "Institute staffs and wands have an Institute main part");
  }
  // Masterworks (R5): Mythilium parts by the masterwork makers, up to masterwork rarity; main part and fixed maker stay.
  const masterOptions = F.reforgeOptions(hammer, 1);
  assert(masterOptions.some(option => option.material === "mythilium") && masterOptions.every(option => option.maker !== "outlaw"), "A masterwork's other parts can be Mythilium, never Outlaw");
  const mythPart = masterOptions.find(option => option.material === "mythilium" && !(option.maker === hammer.parts[1].maker && option.material === hammer.parts[1].material));
  result = F.reforge(F.encode(hammer), [hammer.parts[0], mythPart, hammer.parts[2]], "hearth");
  next = F.decode(result.id);
  assert(result.id && next.masterwork === "anvil-hammer" && next.parts[0].maker === hammer.parts[0].maker && next.parts[0].material === hammer.parts[0].material && JSON.stringify(next.runes) === JSON.stringify(hammer.runes), "A reforged masterwork keeps its main part, fixed maker and runes");
  assert(result.changed && result.cost === 72000 && next.parts[1].material === "mythilium", "A Mythilium part costs 72,000 silver");
  // Staffs and wands (R6, R16): the handle capped by the jewel's or gem's tier; any ink; costs by the handle's rarity.
  for (const [tier, cap] of [[1, "uncommon"], [2, "epic"], [3, "legendary"]]) {
    const spec = F.decode(F.encode({ ...staff, parts: [{ ...staff.parts[0], tier }, staff.parts[1], staff.parts[2]] }));
    const handles = F.reforgeOptions(spec, 1);
    assert(handles.length && handles.every(option => index(F.CASTER_HANDLES[option.material].rarity) <= index(cap)) && handles.some(option => F.CASTER_HANDLES[option.material].rarity === cap), `A tier-${tier} stone's handle goes up to ${cap}`);
  }
  const inks = F.reforgeOptions(staff, 2);
  assert(new Set(inks.map(option => option.material)).size === Object.keys(F.INKS).length, "The inscription can take any ink");
  const handle = F.reforgeOptions(staff, 1).find(option => option.material !== staff.parts[1].material && F.CASTER_HANDLES[option.material].rarity === "common");
  const ink = inks.find(option => option.material !== staff.parts[2].material);
  result = F.reforge(F.encode(staff), [staff.parts[0], handle, ink], "institute");
  assert(result.id && result.cost === 360 + 360, "A common handle costs 360; the new inscription costs by the handle's rarity (common: 360)");
  result = F.reforge(F.encode(staff), [staff.parts[0], staff.parts[1], ink], "institute");
  assert(result.cost === 3 * F.RARITY_PRICE[F.CASTER_HANDLES[staff.parts[1].material].rarity], "A new inscription alone costs 3× the current handle's rarity price");
  return { passed: true, checks: count() };
}

// R4, R7, R8: the Blacksmith, the Tailor and the anvil window.
async function checkShopsAndTailor() {
  const { assert, count } = shopReworkAssert();
  const $ = id => document.getElementById(id);
  Combat.restore(null);
  applyCharacter(normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: 40, currencies: ShopRules.walletFromValue(5000000) }));
  WorldMap.setAmbientEnabled(false); WorldMap.open();
  const region = WorldRules.getRegion("stoneveil-foothills");
  const at = settlement => { worldState.regionId = region.id; worldState.position = { x: settlement.x, y: settlement.y }; WorldMap.render(); return WorldRules.settlementAt(worldState.position); };
  for (const [name, total, weapons, tailor] of [["Crownspire", 9, 5, 6], ["Stonefoot", 6, 3, 4]]) {
    const place = at(region.settlements.find(settlement => settlement.name === name));
    for (let draw = 0; draw < 6; draw++) {
      delete shopState.locations[place.id];
      assert(!Shop.stockFor(place).blacksmith.some(entry => Shop.isClothing(getItem(entry.id))), `${name}'s Blacksmith never stocks clothing`);
    }
    const stock = Shop.stockFor(place);
    const smithItems = stock.blacksmith.map(entry => getItem(entry.id));
    const offers = smithItems.filter(item => item.shopOffer || item.forged?.masterwork).length;
    assert(smithItems.length === total && smithItems.filter(item => item.category === "weapon" && !item.shopOffer && !item.forged?.masterwork).length === weapons, `${name}'s Blacksmith keeps ${weapons} weapons of ${total} pieces`);
    assert(smithItems.filter(item => item.category === "armor" && !item.shopOffer && !item.forged?.masterwork).length === total - weapons - offers && !smithItems.some(Shop.isClothing), `${name}'s Blacksmith has ${total - weapons} armor pieces and no clothing`);
    assert(stock.tailor.length === tailor && stock.tailor.every(entry => Shop.isClothing(getItem(entry.id))), `${name}'s Tailor stocks ${tailor} clothing pieces`);
    assert(!$("map-tailor").hidden && $("map-tailor").textContent.includes("Tailor"), `${name} has a Tailor on the map`);
  }
  // Clothing is sold only to the Tailor; the Blacksmith buys the rest.
  const robe = ForgeRules.encode(ForgeRules.generateArmor({ level: 40, source: "shop", types: ["robe"] }));
  const sword = ForgeRules.encode(ForgeRules.generate({ type: "longsword", level: 40, rarity: "rare", source: "shop" }));
  const spare = ForgeRules.encode(ForgeRules.generate({ type: "mace", level: 40, rarity: "rare", source: "shop" }));
  inventory.push(robe, sword, spare); equipment.primaryHand = sword;
  $("map-blacksmith").click(); await shopReworkWait(50);
  $("shop-sell-mode").click(); await shopReworkWait(50);
  assert(!document.querySelector(`[data-sell-item="${robe}"]`) && document.querySelector(`[data-sell-item="${spare}"]`), "The Blacksmith doesn't buy clothing");
  Shop.close();
  $("map-tailor").click(); await shopReworkWait(50);
  assert($("shop-title").textContent.includes("thread") && !$("shop-reforge-mode").hidden, "The Tailor's shop opens with a reforge anvil");
  $("shop-sell-mode").click(); await shopReworkWait(50);
  assert(document.querySelector(`[data-sell-item="${robe}"]`) && !document.querySelector(`[data-sell-item="${spare}"]`), "The Tailor buys clothing only");
  Shop.close();
  assert(["tailor", "institute", "hearth"].every(scene => SceneBanners.placeholderArt(scene)), "The Tailor's, Institute's and Hearth Home's scenes are marked placeholder art (R1)");
  $("map-item-shop").click(); await shopReworkWait(50);
  assert($("shop-reforge-mode").hidden, "The General Store has no anvil");
  Shop.close();
  // The anvil (R4): your pack, equipped pieces included, and the gear your companions wear.
  const companionSword = ForgeRules.encode(ForgeRules.generate({ type: "longsword", level: 40, rarity: "uncommon", source: "shop" }));
  const companionCharacter = { ...defaultCharacter(), raceId: "human", inventory: [companionSword], equipment: { ...defaultCharacter().equipment, primaryHand: companionSword } };
  companions = [{ id: "companion-anvil-test", name: "Brann", gender: "male", character: companionCharacter }];
  const pieces = Reforge.pieces("smith");
  assert(pieces.some(entry => entry.owner === "player" && entry.id === sword && /equipped/.test(entry.label)) && pieces.some(entry => entry.owner === "companion-anvil-test" && entry.id === companionSword), "The anvil lists your pack (equipped pieces too) and your companions' worn gear");
  assert(!pieces.some(entry => entry.id === robe) && Reforge.pieces("tailor").some(entry => entry.id === robe), "Each anvil lists only what it reforges");
  $("map-blacksmith").click(); await shopReworkWait(50);
  $("shop-reforge-mode").click(); await shopReworkWait(50);
  const anvil = Shop.anvil();
  anvil.place("player|" + sword);
  const spec = ForgeRules.decode(sword), option = ForgeRules.reforgeOptions(spec, 1).find(entry => entry.material !== spec.parts[1].material);
  anvil.choose(`1|${option.maker}|${option.material}`);
  const price = anvil.state().cost, before = ShopRules.walletValue(currencies);
  assert(price === ForgeRules.reforgeCost(spec, [spec.parts[0], option, spec.parts[2]]), "The Blacksmith charges the full reforge price");
  $("reforge-button").click(); await shopReworkWait(50);
  const reforged = equipment.primaryHand;
  assert(before - ShopRules.walletValue(currencies) === price && reforged !== sword && inventory.includes(reforged) && !inventory.includes(sword) && ForgeRules.decode(reforged).parts[1].material === option.material, "Reforging pays and swaps the equipped piece in place");
  anvil.reset(); anvil.place("companion-anvil-test|" + companionSword);
  const companionSpec = ForgeRules.decode(companionSword), companionOption = ForgeRules.reforgeOptions(companionSpec, 2).find(entry => entry.material !== companionSpec.parts[2].material);
  anvil.choose(`2|${companionOption.maker}|${companionOption.material}`);
  assert(anvil.commit() && companions[0].character.equipment.primaryHand !== companionSword && ForgeRules.decode(companions[0].character.equipment.primaryHand).parts[2].material === companionOption.material, "A companion's worn piece is reforged on them");
  Shop.close(); companions = [];
  return { passed: true, checks: count() };
}

// R9, R10, R11, R23: the Institute of Magic on Mirrormere's lake.
async function checkInstituteOfMagic() {
  const { assert, count } = shopReworkAssert();
  const $ = id => document.getElementById(id);
  Combat.restore(null);
  applyCharacter(normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: 34, currencies: ShopRules.walletFromValue(50000000) }));
  WorldMap.setAmbientEnabled(false); WorldMap.open();
  const region = WorldRules.getRegion("mirrormere-basin"), site = region.sites.find(entry => entry.id === "institute-of-magic");
  assert(site && Math.hypot(site.x - region.lake.x, site.y - region.lake.y) < 5 && WorldRules.isLand(site, region.id), "The Institute stands on an island in the middle of the lake");
  const bridge = region.bridges?.[0];
  assert(bridge && bridge.every(point => WorldRules.isLand(point, region.id)) && bridge[0].x > site.x && Math.abs(bridge[0].y - site.y) < 20, "A bridge runs west to it from the ring road on Lakespire's side");
  const length = bridge.slice(1).reduce((sum, point, at) => sum + Math.hypot(point.x - bridge[at].x, point.y - bridge[at].y), 0) / WorldRules.UNITS_PER_KM;
  assert(length > 30 && length < 45, `The bridge is about 37 km long (${length.toFixed(1)} km)`);
  assert(!WorldRules.isLand({ x: site.x, y: site.y + 150 }, region.id), "The rest of the lake is still water");
  worldState.regionId = region.id; worldState.position = { x: site.x, y: site.y }; WorldMap.render();
  assert(!$("map-institute").hidden && $("map-institute").textContent.includes("Institute") && $("map-magic-tower").hidden, "At the Institute the map offers it, not a Magic Tower");
  document.dispatchEvent(new KeyboardEvent("keydown", { key: "i", bubbles: true }));
  await shopReworkWait(100);
  assert(MagicTower.venue() === "institute" && !$("magic-tower-modal").hidden, "Its key, I, opens the Institute");
  const visible = [...document.querySelectorAll("[data-tower-menu]")].filter(button => !button.hidden).map(button => button.dataset.towerMenu).sort().join();
  assert(visible === "casters,clothing,fitting,reforge,spells", "It sells staffs, wands, clothing and spells, fits and removes runes and reforges; no runes for sale (" + visible + ")");
  const stock = MagicTower.instituteStock();
  const casters = stock.tower.map(entry => getItem(entry.id)), clothes = stock.clothing.map(entry => getItem(entry.id));
  assert(casters.length === 6 && casters.every(item => item.forged.parts[0].maker === "institute-of-magic"), "Six Institute-made staffs and wands");
  const doctor = clothes.find(item => item.forged?.masterwork === "doctors-orders");
  assert(clothes.length === 7 && clothes.filter(item => item !== doctor).every(item => Shop.isClothing(item) && item.forged.parts[0].maker === "institute-of-magic"), "Six Institute-made clothing pieces and Doctor's Orders");
  assert(doctor && doctor.forged.level === 34 && MagicTower.prices.item(doctor, "clothing") === 30 * ShopRules.SILVER_PER_PLATINUM, "Doctor's Orders is always there, forged at your level, for 30 Platinum");
  assert(["casters:0.85", "clothing:0.85", "spells:0.8", "fitting:0.8", "removal:0.8"].every(text => { const [key, factor] = text.split(":"); return MagicTower.prices.factor(key) === Number(factor); }), "Its discounts: staffs, wands and clothing 15%, spells, rune fitting and removal 20%");
  assert(MagicTower.prices.item(casters[0], "casters") === Math.round(casters[0].priceSilver * 0.85), "Staffs and wands cost 15% less");
  MagicTower.menu("reforge"); await shopReworkWait(50);
  assert(MagicTower.anvil()?.anvil === "institute", "Its anvil reforges casters");
  MagicTower.close();
  // A castle's Magic Tower is unchanged.
  const lakespire = region.settlements[0];
  worldState.position = { x: lakespire.x, y: lakespire.y }; WorldMap.render();
  assert(!$("map-magic-tower").hidden && $("map-institute").hidden, "Lakespire keeps its Magic Tower");
  $("map-magic-tower").click(); await shopReworkWait(100);
  const towerMenus = [...document.querySelectorAll("[data-tower-menu]")].filter(button => !button.hidden).map(button => button.dataset.towerMenu).sort().join();
  assert(MagicTower.venue() === "tower" && towerMenus === "casters,fitting,runes,sell,spells" && MagicTower.prices.factor("fitting") === 1, "The Tower keeps its menus and prices");
  MagicTower.close();
  return { passed: true, checks: count() };
}

// R14, R15, R23: Hearth Home and Ornn.
async function checkHearthHome() {
  const { assert, count } = shopReworkAssert();
  const $ = id => document.getElementById(id);
  Combat.restore(null);
  applyCharacter(normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: 50, currencies: ShopRules.walletFromValue(50000000) }));
  WorldMap.setAmbientEnabled(false); WorldMap.open();
  const region = WorldRules.getRegion("cinder-isle"), site = region.sites.find(entry => entry.id === "hearth-home");
  const roadEnds = region.roads.map(road => road.at(-1));
  assert(site && roadEnds.some(end => Math.hypot(end.x - site.x, end.y - site.y) <= site.radius), "Hearth Home stands where Cinder Isle's road ends");
  assert(WorldRules.zoneAt({ x: site.x, y: site.y }, region.id) === "Hearth Home" && EncounterRules.cinderProfiles["Hearth Home"]?.safe && EncounterRules.cinderChances["Hearth Home"] === 0, "It is a safe zone");
  assert(EncounterRules.mirrormereProfiles["Institute of Magic"]?.safe && EncounterRules.mirrormereChances["Institute of Magic"] === 0, "So is the Institute");
  worldState.regionId = region.id; worldState.position = { x: site.x, y: site.y }; worldState.defeatedSiteBosses = [];
  worldState.guild.greatHuntsCompleted.push("ornn-god-of-the-forge");
  WorldMap.render();
  assert(!$("map-hearth-home").hidden && $("map-hearth-home").textContent.includes("Hearth Home"), "Its button shows at the site");
  document.dispatchEvent(new KeyboardEvent("keydown", { key: "h", bubbles: true }));
  await shopReworkWait(50);
  assert(!$("hearth-home-modal").hidden && $("hearth-home-enter").disabled && !$("hearth-home-challenge").disabled, "Its key, H, opens it; the forge is shut until Ornn falls there; a completed Great Hunt does not count");
  Shop.open("hearth"); await shopReworkWait(50);
  assert($("shop-modal").hidden, "The store won't open while locked");
  // Challenging Ornn starts his fight; winning opens the forge, and he can be fought again.
  let roster = null;
  const startEncounter = Combat.startEncounter;
  Combat.startEncounter = async names => { roster = names; return false; };
  await HearthHome.challenge();
  Combat.startEncounter = startEncounter;
  assert(roster?.join() === "Ornn, God of the Forge", "The challenge is Ornn alone (his Ram comes at half health)");
  HearthHome.onVictory({ actors: [{ enemyDefinitionId: "ornn-god-of-the-forge-unique-level-48", summoned: false, character: { currentHp: 0 } }] });
  assert(worldState.defeatedSiteBosses.includes("ornn-god-of-the-forge-unique-level-48") && HearthHome.unlocked(), "His defeat opens the forge");
  HearthHome.close(); HearthHome.open();
  assert(!$("hearth-home-enter").disabled && !$("hearth-home-challenge").disabled, "The forge opens and Ornn can be challenged again");
  $("hearth-home-enter").click(); await shopReworkWait(50);
  assert(!$("shop-modal").hidden && $("shop-eyebrow").textContent === "CINDER ISLE · HEARTH HOME" && $("shop-sell-mode").hidden, "The store opens; it buys nothing");
  const stock = Shop.hearth.stock().hearth.map(entry => getItem(entry.id));
  assert(stock.length === 9 && stock.filter(item => item.category === "weapon").length === 5 && stock.every(item => item.forged.parts[0].maker === "hearthforge" && !Shop.isClothing(item)), "Five Hearthforge weapons and four armor pieces");
  assert(stock.every(item => Shop.hearth.price(item) === Math.round(item.priceSilver * 0.9)), "Sold at 10% off");
  // Its anvil takes masterworks, 15% off.
  const hammer = ForgeRules.encode(ForgeRules.generateMasterwork({ key: "anvil-hammer", level: 48 }));
  inventory.push(hammer);
  $("shop-reforge-mode").click(); await shopReworkWait(50);
  const anvil = Shop.anvil();
  assert(anvil.anvil === "hearth" && anvil.discount === 15 && Reforge.pieces("hearth").some(entry => entry.id === hammer) && !Reforge.pieces("smith").some(entry => entry.id === hammer), "Hearth Home's anvil alone takes masterworks");
  anvil.place("player|" + hammer);
  const spec = ForgeRules.decode(hammer), option = ForgeRules.reforgeOptions(spec, 1).find(entry => entry.material !== spec.parts[1].material);
  anvil.choose(`1|${option.maker}|${option.material}`);
  assert(anvil.state().cost === Math.round(ForgeRules.reforgeCost(spec, [spec.parts[0], option, spec.parts[2]]) * 0.85), "Reforging costs 15% less here");
  Shop.close();
  // Ornn left the Great Hunts: Emberhaven's board offers only Ur-Mat now.
  const town = region.settlements[0];
  worldState.position = { x: town.x, y: town.y }; level = 95; worldState.guild.greatHuntsCompleted = [];
  for (let index = 0; index < 20; index++) {
    worldState.guild.boards = {}; Guild.render();
    assert(!Guild.boardFor().offers.some(offer => offer.uniqueId === "ornn-god-of-the-forge"), "No board offers Ornn as a Great Hunt");
  }
  return { passed: true, checks: count() };
}

// R15: old saves.
async function checkOrnnInOldSaves() {
  const { assert, count } = shopReworkAssert();
  const hunt = { type: "great-hunt", uniqueId: "ornn-god-of-the-forge", enemy: "Ornn, God of the Forge", level: 48, regionId: "cinder-isle", zone: "Lava Fields", point: { x: 900, y: 500 }, reward: 1000, version: 2 };
  const day = WorldRules.day(0);
  const saved = { regionId: "cinder-isle", position: { x: 500, y: 500 }, minutes: 0, defeatedSiteBosses: ["ornn-god-of-the-forge-unique-level-48", "someone-else"],
    guild: { greatHunt: hunt, greatHuntsCompleted: ["ornn-god-of-the-forge"], greatHuntRematches: [{ uniqueId: "ornn-god-of-the-forge", enemy: "Ornn, God of the Forge", regionId: "cinder-isle", zone: "Lava Fields", point: { x: 900, y: 500 } }, { uniqueId: "deep-sea-king", enemy: "Deep Sea King", regionId: "sunward-coast", zone: "Beach", point: { x: 900, y: 500 } }],
      quest: { type: "escort", version: 1, enemy: "Goblin", regionId: "alder-vale", zone: "Road", point: { x: 1, y: 1 }, reward: 5 },
      boards: { emberhaven: { day, offers: [hunt, { ...hunt, uniqueId: "ur-mat-false-dreamer", enemy: "Ur-Mat, the False Dreamer" }] } } } };
  const state = WorldRules.normalize(saved);
  assert(!state.guild.greatHunt && /Ornn, God of the Forge, is no longer a Great Hunt/.test(state.guild.notice), "An active Ornn Great Hunt is withdrawn with a note");
  assert(/contract from before the Guild changed/.test(state.guild.notice), "An old contract's note is kept beside it");
  assert(!state.guild.greatHuntRematches.some(entry => entry.uniqueId === "ornn-god-of-the-forge") && state.guild.greatHuntRematches.some(entry => entry.uniqueId === "deep-sea-king"), "His rematch markers are removed, others kept");
  assert(!(state.guild.boards.emberhaven?.offers || []).some(offer => offer.uniqueId === "ornn-god-of-the-forge"), "A board drawn today no longer offers him");
  assert(state.defeatedSiteBosses.join() === "ornn-god-of-the-forge-unique-level-48", "His Hearth Home defeat is kept");
  return { passed: true, checks: count() };
}

// The guild's information names Ornn's drops at Hearth Home, a site challenge, not a Great Hunt.
async function checkOrnnInGuildInformation() {
  const { assert, count } = shopReworkAssert();
  const $ = id => document.getElementById(id), panel = () => $("guild-panel");
  const tick = () => new Promise(resolve => setTimeout(resolve, 0));
  Combat.restore(null); WorldMap.setAmbientEnabled(false);
  const c = defaultCharacter(); c.raceId = "human"; c.worldState.guild.reputation = 500;
  c.worldState.guild.information = { offers: { class: [], item: [] }, revealed: { class: [], item: ["masterwork-anvil-hammer"] } };
  applyCharacter(normalizeCharacter(c)); $("map-open-button").click(); WorldMap.render(); $("map-guild").click();
  document.querySelector('#guild-banner [data-guild-spot="hooded"]').dispatchEvent(new MouseEvent("click", { bubbles: true }));
  for (let index = 0; index < 400 && !panel().querySelector('[data-guild-information-page="archive"]'); index++) await tick();
  panel().querySelector('[data-guild-information-page="archive"]').click();
  panel().querySelector('[data-guild-information-known="masterwork-anvil-hammer"]').click();
  const text = panel().textContent;
  assert(text.includes("Ornn, God of the Forge") && text.includes("Hearth Home") && text.includes("Site challenge") && !/Great Hunt/.test(text.slice(text.indexOf("Ornn"))), "The Anvil Hammer's source is Ornn's site challenge at Hearth Home");
  return { passed: true, checks: count() };
}

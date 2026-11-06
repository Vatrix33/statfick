// Port task 3, the Shop Rework (2026-11-06): one entry per group of the user's decisions R1–R27
// (engine/Docs/Engine-Plan.md, "Decisions for task 3, Shop Rework").
const shopReworkWait = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));
function shopReworkAssert() {
  let checks = 0;
  const assert = (condition, message) => { if (!condition) throw Error(message); checks++; };
  return { assert, count: () => checks };
}
// Combat's own functions, exposed by loading a second copy of combat.js with a hook (as the Ornn checks do).
async function shopReworkCombatHooks() {
  if (window.__shopReworkChecks) return window.__shopReworkChecks;
  Combat.restore(null);
  const source = await (await fetch("scripts/combat.js")).text();
  const marker = "  window.Combat = {";
  const script = document.createElement("script");
  script.textContent = source.replace(marker, "  window.__shopReworkChecks = { actorById, hp, values, setHp, getBattle: () => battle, healingFactor, healingAmount, healHp, hurt, attack, useItem, advance, expire, applyTimedEffects, actionPointMaximum, currentActor, triggerControlTraps };\n" + marker);
  document.head.append(script);
  await waitForSourceFiles();
  if (window.BattleStage) window.BattleStage.enabled = () => false;
  if (window.GameSettings) window.GameSettings.enemyDelay = () => 1e9;
  return window.__shopReworkChecks;
}

// R11–R13, R24, R25, R27: Doctor's Orders in battle.
async function checkDoctorsOrdersHealing() {
  const { assert, count } = shopReworkAssert();
  const h = await shopReworkCombatHooks();
  const robe = ForgeRules.encode(ForgeRules.generateMasterwork({ key: "doctors-orders", level: 30 }));
  const item = getItem(robe);
  assert(item?.forged?.masterwork && item.type === "clothing" && item.forged.parts[0].maker === "institute-of-magic" && item.forged.masterwork === "doctors-orders" && item.name.endsWith("Doctor's Orders"), "Doctor's Orders is an Institute of Magic masterwork robe");
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
  // A heal-over-time remembers its caster: the wearer's ticks on an enemy-side target are ×1.5 (then the enemy's −80%).
  const target = goblins[0];
  h.applyTimedEffects(target, "test-hot", "Test Bloom", [{ type: "turn-start-restore-percent", hp: 10, durationTurns: 3 }], player);
  assert(target.effects.find(effect => effect.name === "Test Bloom")?.healerId === "player", "A heal-over-time records who cast it");
  const maximum = h.values(target).maxHp;
  h.setHp(target, 1); h.expire(target);
  assert(h.hp(target) - 1 === Math.floor(Math.floor(Math.max(1, Math.floor(maximum * 10 / 100)) * 1.5) / 5), "Its ticks carry the caster's +50%");
  // The healing-given and healing-received effects read on the item card.
  const text = item.effects.map(artifactEffectLabel).join(" | ");
  assert(/heal you cause in battle/.test(text) && /\+50%/.test(text) && /Healing you receive in battle \+25%/.test(text), "The item card names both effects: " + text);
  Combat.restore(null);
  return { passed: true, checks: count(), plainPotion, robePotion };
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
  const start = async (itemIds, enemies = ["Goblin", "Goblin"]) => {
    Combat.restore(null);
    applyCharacter(normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: 30, inventory: itemIds }));
    assert(await Combat.startEncounter(enemies, "Trap test"), "The trap battle starts");
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
  ({ battle, player, goblins } = await start(["consumable-smoke-trap"]));
  [goblin] = goblins;
  const evasion = h.values(player).evasion;
  strike(battle, goblin, player);
  const smoked = player.effects.find(effect => effect.smokeOwnerId === "player");
  assert(!armed(player, "trap-smoke") && smoked?.type === "defense-percent" && smoked.target === "evasion" && smoked.amount === 20 && h.values(player).evasion > evasion, "Smoke Trap gives +20% Evasion when attacked");
  turnOf(battle, player);
  assert(!player.effects.some(effect => effect.smokeOwnerId), "The smoke lifts at the owner's next turn");
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
  // Gravity Well: any attack costs every enemy 2 AP on its next turn.
  ({ battle, player, goblins } = await start(["consumable-gravity-well"]));
  [goblin, other] = goblins;
  strike(battle, goblin, player);
  assert(!armed(player, "trap-gravity-well") && goblin.trapApLoss === 2 && other.trapApLoss === 2, "Gravity Well costs every enemy 2 AP");
  assert(turnOf(battle, other) === h.actionPointMaximum(other) - 2, "The AP is lost on that enemy's next turn");
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
  assert(ShopRules.rarityWeight("legendary", 95) > ShopRules.rarityWeight("legendary", 20) && ShopRules.rarityWeight("common", 10) > ShopRules.rarityWeight("common", 95), "Rarer traps weigh more at higher levels");
  Combat.restore(null);
  return { passed: true, checks: count() };
}

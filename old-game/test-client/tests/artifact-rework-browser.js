// Port task 4, the Artifact Rework (2026-11-06): one entry per group of the user's decisions A1–A32
// (engine/Docs/Engine-Plan.md, "Decisions for task 4, Artifact Rework").
const artifactWait = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));
function artifactAssert() {
  let checks = 0;
  const assert = (condition, message) => { if (!condition) throw Error(message); checks++; };
  return { assert, count: () => checks };
}
const artifactId = name => "artifact-" + name.toLowerCase().replace(/'/g, "").replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "") + "-artifact";
// Combat's own functions, from a second copy of combat.js with a hook (as the Ornn and Shop Rework checks do).
const artifactHooks = async () => {
  if (window.__artifactChecks) return window.__artifactChecks;
  Combat.restore(null);
  const source = await (await fetch("scripts/combat.js")).text();
  const marker = "  window.Combat = {";
  const script = document.createElement("script");
  script.textContent = source.replace(marker, "  window.__artifactChecks = { actorById, hp, mana, values, setHp, setMana, getBattle: () => battle, healingFactor, healHp, hurt, directDamage, applyCombatDamage, attack, cast, useItem, advance, expire, applyTimedEffects, actionPointMaximum, artifactDamagePercent, artifactEffectsOf, noteFall, summonFriendly, resistance, incomingDamageFactor, endTurn, awardDefeatedEnemyExperience };\n" + marker);
  document.head.append(script);
  await waitForSourceFiles();
  if (window.BattleStage) window.BattleStage.enabled = () => false;
  if (window.GameSettings) window.GameSettings.enemyDelay = () => 1e9;
  return window.__artifactChecks;
};
// A battle with these artifacts worn by you (and a companion wearing others, if given).
async function artifactBattle(h, worn = [], { enemies = ["Goblin", "Goblin"], level = 30, companion = null, changes = {} } = {}) {
  Combat.restore(null);
  applyCharacter(normalizeCharacter({ ...defaultCharacter(), raceId: "human", level, inventory: [...worn], ...changes }));
  equipment.artifact1 = worn[0] || null; equipment.artifact2 = worn[1] || null;
  companions = companion ? [{ id: "companion-artifact-test", name: "Mira", gender: "female", character: { ...defaultCharacter(), raceId: "human", level, inventory: [...(companion.worn || [])], equipment: { ...defaultCharacter().equipment, artifact1: companion.worn?.[0] || null, artifact2: companion.worn?.[1] || null } } }] : [];
  const started = await Combat.startEncounter(enemies, "Artifact test");
  companions = [];
  if (!started) throw Error("The artifact test battle starts");
  const battle = h.getBattle();
  return { battle, player: h.actorById("player"), ally: battle.actors.find(actor => actor.companion), foes: battle.actors.filter(actor => actor.team === "enemy") };
}

// Weapon hits: Leech Hook and Vampire's Fang, Thornknot Brooch, the on-hit effects, Phantom Step (A28, A30).
async function checkArtifactWeaponHits() {
  const { assert, count } = artifactAssert();
  const h = await artifactHooks();
  const random = Math.random;
  try {
    // Leech Hook: weapon hits heal 3% of the damage dealt.
    let { battle, player, foes } = await artifactBattle(h, [artifactId("Leech Hook")]);
    Math.random = () => 0;
    battle.current = "player"; battle.ap = 9; h.setHp(player, 1);
    const before = foes[0].character.currentHp;
    h.attack(player, foes[0]);
    const dealt = before - foes[0].character.currentHp;
    assert(dealt > 0 && h.hp(player) - 1 === Math.floor(dealt * 3 / 100), `Leech Hook heals 3% of the damage (${dealt} → ${h.hp(player) - 1})`);
    // Thornknot Brooch: melee attackers take 8% of the damage back.
    ({ battle, player, foes } = await artifactBattle(h, [artifactId("Thornknot Brooch")]));
    foes[0].character.stats.strength = 400; h.setHp(player, 100000);
    battle.current = foes[0].id; battle.ap = 9;
    const goblinHp = h.hp(foes[0]), playerHp = h.hp(player);
    h.attack(foes[0], player);
    const taken = playerHp - h.hp(player);
    assert(taken > 20 && goblinHp - h.hp(foes[0]) > 0 && battle.log.some(entry => entry.includes("Thornknot Brooch")), `Thornknot Brooch turns part of a melee hit back (${taken} taken, ${goblinHp - h.hp(foes[0])} returned)`);
    // Burning Brand: 20% on a weapon hit; Frostbite Charm: 15%; both roll (Math.random 0 means every roll succeeds).
    ({ battle, player, foes } = await artifactBattle(h, [artifactId("Burning Brand"), artifactId("Frostbite Charm")]));
    battle.current = "player"; battle.ap = 9;
    h.attack(player, foes[0]);
    const burn = foes[0].effects.find(effect => effect.type === "turn-start-max-hp-loss-percent");
    const slow = foes[0].effects.find(effect => effect.type === "stat-percent" && effect.target === "speed");
    assert(burn?.amount === 3 && burn.expiresTurn === foes[0].turns + 2 && slow?.amount === -15 && slow.expiresTurn === foes[0].turns + 2, "Burning Brand burns for 3% for 2 turns; Frostbite Charm slows by 15% for 2 turns");
    Math.random = () => 0.99;
    ({ battle, player, foes } = await artifactBattle(h, [artifactId("Burning Brand"), artifactId("Frostbite Charm")]));
    battle.current = "player"; battle.ap = 9;
    h.attack(player, foes[0]);
    assert(!foes[0].effects.some(effect => effect.type === "turn-start-max-hp-loss-percent" || effect.type === "stat-percent"), "They only trigger on their chance");
    // Shatter Stone: −5% Armor per hit, up to 3 stacks.
    Math.random = () => 0;
    ({ battle, player, foes } = await artifactBattle(h, [artifactId("Shatter Stone")]));
    for (let hit = 0; hit < 4; hit++) { battle.current = "player"; battle.ap = 9; h.attack(player, foes[0]); }
    const shred = foes[0].effects.find(effect => effect.type === "defense-percent" && effect.target === "armor");
    assert(shred?.amount === -15 && shred.stacks === 3, "Shatter Stone stacks to −15% Armor and no further");
    // Thunderhead Crown: 15% stun, stun immunity holds; Venomglass Vial: 4% poison for 3 turns, refreshed not stacked.
    ({ battle, player, foes } = await artifactBattle(h, [artifactId("Thunderhead Crown"), artifactId("Venomglass Vial")]));
    battle.current = "player"; battle.ap = 9;
    h.attack(player, foes[0]); h.attack(player, foes[0]);
    assert(foes[0].effects.some(effect => effect.type === "stun") && foes[0].effects.filter(effect => effect.type === "class-poison").length === 1, "Thunderhead Crown stuns; Venomglass poisons once, refreshed");
    const poison = foes[0].effects.find(effect => effect.type === "class-poison");
    assert(poison.percentMaxHp === 4 && poison.expiresTurn === foes[0].turns + 3, "The poison is 4% of maximum HP for 3 turns");
    ({ battle, player, foes } = await artifactBattle(h, [artifactId("Thunderhead Crown")]));
    foes[0].character.raceId = "satyr";
    battle.current = "player"; battle.ap = 9;
    h.attack(player, foes[0]);
    assert(!foes[0].effects.some(effect => effect.type === "stun") && battle.log.some(entry => entry.includes("resists the stun")), "A stun-immune target resists");
    // Phantom Step Pin: an evaded attack readies +25% on the next weapon attack, spent hit or miss (A30).
    ({ battle, player, foes } = await artifactBattle(h, [artifactId("Phantom Step Pin")]));
    Math.random = () => 0.9999;
    battle.current = foes[0].id; battle.ap = 9;
    h.attack(foes[0], player);
    assert(player.phantomStepBonus === 25, "Evading an attack readies +25% on the next weapon attack");
    battle.current = "player"; battle.ap = 9;
    h.attack(player, foes[1]);
    assert(!player.phantomStepBonus, "It is spent on the next weapon attack, even a miss");
    return { passed: true, checks: count() };
  } finally { Math.random = random; Combat.restore(null); }
}

// Damage and defense: the "+X% damage" artifacts on everything you deal (A20), resistances, Warding Cord and
// Second Chance Locket on any enemy damage (A27), Warden's Bell (heroes only, A18), Banner of Kings (A23, A32).
async function checkArtifactDamageAndDefense() {
  const { assert, count } = artifactAssert();
  const h = await artifactHooks();
  // The damage bonuses, read the way hurt() and directDamage() read them.
  let { battle, player, foes } = await artifactBattle(h, [artifactId("Greenskin Tooth"), artifactId("Executioner's Mark")]);
  const [goblin] = foes;
  assert(h.artifactDamagePercent(player, goblin) === 12, "Greenskin Tooth: +12% against a goblin");
  h.setHp(goblin, 1);
  assert(h.artifactDamagePercent(player, goblin) === 32, "Executioner's Mark adds +20% below 30% HP");
  assert(h.artifactDamagePercent(goblin, player) === 0, "Enemies don't get your bonuses");
  ({ battle, player, foes } = await artifactBattle(h, [artifactId("Silver Nail"), artifactId("Dragonbane Scale")], { enemies: ["Goblin"] }));
  assert(h.artifactDamagePercent(player, foes[0]) === 0, "Silver Nail and Dragonbane Scale don't touch a goblin");
  foes[0].character.raceId = "undead";
  assert(h.artifactDamagePercent(player, foes[0]) === 12, "Silver Nail: +12% against undead");
  foes[0].character.raceId = "dragon";
  assert(h.artifactDamagePercent(player, foes[0]) === 15, "Dragonbane Scale: +15% against dragons");
  ({ battle, player, foes } = await artifactBattle(h, [artifactId("Giantslayer Torc"), artifactId("Spirit Lantern")], { enemies: ["Goblin", "Steelguard Golem"], level: 5 }));
  const [smallGoblin, golem] = foes;
  assert(h.values(golem).maxHp > h.values(player).maxHp && h.values(smallGoblin).maxHp <= h.values(player).maxHp, "The golem is bigger than you, the goblin isn't");
  assert(h.artifactDamagePercent(player, smallGoblin) === 0 && h.artifactDamagePercent(player, golem) === 30, "Spirit Lantern: +15% against an elemental; Giantslayer Torc: +15% against a bigger foe");
  // Tide-turner Compass: below 50% HP, +20% damage and Evasion +10.
  ({ battle, player, foes } = await artifactBattle(h, [artifactId("Tide-turner Compass")]));
  const evasion = h.values(player).evasion;
  assert(h.artifactDamagePercent(player, foes[0]) === 0, "Tide-turner does nothing above half HP");
  h.setHp(player, Math.floor(player.resourceMax.hp * 0.4));
  assert(h.artifactDamagePercent(player, foes[0]) === 20 && h.values(player).evasion === evasion + 10, "Below half HP: +20% damage and Evasion +10");
  // A20: traps, thorns and item damage too (directDamage carries the bonus).
  ({ battle, player, foes } = await artifactBattle(h, [artifactId("Greenskin Tooth")], { enemies: ["Goblin"] }));
  h.setHp(foes[0], 10000);
  const goblinHp = h.hp(foes[0]);
  const trapDealt = h.directDamage(foes[0], 50, false, "physical", player);
  assert(trapDealt === 56 && goblinHp - h.hp(foes[0]) === 56, `Direct damage you cause (traps, thorns) gets the bonus too (${trapDealt})`);
  // Resistances: Ashen Ward (fire −12%), Spellward Mirror (spell damage taken −10%).
  ({ battle, player, foes } = await artifactBattle(h, [artifactId("Ashen Ward"), artifactId("Spellward Mirror")]));
  assert(h.resistance(player, "fire", true) - h.resistance(player, "water", true) === 12, "Ashen Ward: Fire resistance +12");
  assert(Math.abs(h.incomingDamageFactor(player, foes[0], "spell", "water") - h.incomingDamageFactor(player, foes[0], "melee", "water") * 0.9) < 1e-9, "Spellward Mirror: spell damage taken −10%");
  // Warding Cord: the first hit (any enemy damage, A27) −25%, once.
  ({ battle, player, foes } = await artifactBattle(h, [artifactId("Warding Cord")]));
  h.setHp(player, 1000);
  assert(h.applyCombatDamage(player, 100, foes[0]) === 75 && h.applyCombatDamage(player, 100, foes[0]) === 100, "Warding Cord softens only the first hit, by 25%");
  ({ battle, player, foes } = await artifactBattle(h, [artifactId("Warding Cord")]));
  h.setHp(player, 1000);
  assert(h.applyCombatDamage(player, 100, null) === 75, "Damage over time counts as a hit");
  // Second Chance Locket: once per battle, damage that would defeat you leaves 1 HP (A27).
  ({ battle, player, foes } = await artifactBattle(h, [artifactId("Second Chance Locket")]));
  h.setHp(player, 50);
  h.applyCombatDamage(player, 500, foes[0]);
  assert(h.hp(player) === 1, "Second Chance Locket leaves you at 1 HP");
  h.applyCombatDamage(player, 500, foes[0]);
  assert(h.hp(player) === 0, "Only once per battle");
  // Warden's Bell: an ally (not the wearer) below 30% gets a 15% barrier, once; heroes only (A18).
  ({ battle, player, foes } = await artifactBattle(h, [artifactId("Warden's Bell")], { companion: { worn: [] } }));
  const ally = battle.actors.find(actor => actor.companion);
  h.applyCombatDamage(ally, h.hp(ally) - Math.floor(ally.resourceMax.hp * 0.2), foes[0]);
  assert(ally.barrierHp === Math.floor(ally.resourceMax.hp * 0.15), "Warden's Bell shields an ally who drops below 30%");
  ally.barrierHp = 0; h.setHp(ally, ally.resourceMax.hp); h.applyCombatDamage(ally, Math.floor(ally.resourceMax.hp * 0.9), foes[0]);
  assert(!ally.barrierHp, "Only the first time in a battle");
  h.applyCombatDamage(player, h.hp(player) - Math.floor(player.resourceMax.hp * 0.2), foes[0]);
  assert(!player.barrierHp, "Not its wearer");
  // Banner of Kings: you and every ally, summons too (A23), +12% damage and Armor, all battle long (A32).
  ({ battle, player, foes } = await artifactBattle(h, [artifactId("Banner of Kings")], { companion: { worn: [] } }));
  const mira = battle.actors.find(actor => actor.companion);
  assert(h.artifactDamagePercent(mira, foes[0]) === 12 && h.artifactDamagePercent(player, foes[0]) === 12, "Banner of Kings: +12% damage for you and every ally");
  h.summonFriendly(player, { type: "summon-friendly", enemyId: "steelguard-golem-level-35" }, "Test summon");
  const summon = battle.actors.find(actor => actor.summoned && actor.team === "player");
  assert(summon && h.artifactDamagePercent(summon, foes[0]) === 12, "A summon shares it");
  const banner = battle.kingsBanner;
  const armorWith = h.values(summon).armor;
  battle.kingsBanner = null;
  const armorWithout = h.values(summon).armor;
  battle.kingsBanner = banner;
  assert(armorWithout > 0 && armorWith === Math.ceil(armorWithout * 1.12) || armorWith > armorWithout && Math.abs(armorWith / armorWithout - 1.12) < 0.02, `and Armor +12% (${armorWithout} → ${armorWith})`);
  h.setHp(player, 0); h.noteFall(player);
  assert(h.artifactDamagePercent(mira, foes[0]) === 12, "It lasts while its wearer is down");
  Combat.restore(null);
  return { passed: true, checks: count() };
}

// Turn start and battle start: Morning Dew Vial, Mana Drip Stone, Cleansing Bead, Starfall Orb (A19), Warcry Horn,
// Ironskin Phial, Rallying Banner Pin (summons too, A23), Hourglass of Haste.
async function checkArtifactTurnsAndBattleStart() {
  const { assert, count } = artifactAssert();
  const h = await artifactHooks();
  const random = Math.random;
  try {
    let { battle, player, foes } = await artifactBattle(h, [artifactId("Morning Dew Vial"), artifactId("Mana Drip Stone")]);
    const maximum = h.values(player);
    h.setHp(player, 1); h.setMana(player, 0); h.expire(player);
    assert(h.hp(player) - 1 === Math.max(1, Math.floor(maximum.maxHp * 2 / 100)) && h.mana(player) === Math.max(1, Math.floor(maximum.maxMana * 2 / 100)), "Morning Dew restores 2% HP and Mana Drip 2% Mana at the start of your turn");
    ({ battle, player, foes } = await artifactBattle(h, [artifactId("Cleansing Bead")]));
    h.applyTimedEffects(player, "test-curse", "Test Curse", [{ type: "stat-percent", target: "strength", amount: -20, durationTurns: 5 }]);
    Math.random = () => 0.5;
    h.expire(player);
    assert(player.effects.some(effect => effect.name === "Test Curse"), "Cleansing Bead's 20% chance can miss");
    Math.random = () => 0.1;
    h.expire(player);
    assert(!player.effects.some(effect => effect.name === "Test Curse"), "and removes a debuff when it lands");
    // Starfall Orb: Holy damage of 15% maximum Mana to a random enemy, as a spell hit (A19).
    ({ battle, player, foes } = await artifactBattle(h, [artifactId("Starfall Orb")], { enemies: ["Goblin"] }));
    h.setHp(foes[0], 100000);
    const before = h.hp(foes[0]);
    h.expire(player);
    const starDamage = before - h.hp(foes[0]);
    const expected = Math.max(1, Math.floor(h.values(player).maxMana * 15 / 100));
    assert(starDamage > 0 && battle.log.some(entry => entry.includes("falling star")), `Starfall Orb strikes an enemy (${starDamage} of ${expected} power)`);
    foes[0].resistances = { ...(foes[0].resistances || {}), holy: 50 };
    const resisted = h.hp(foes[0]); h.expire(player);
    assert(resisted - h.hp(foes[0]) < starDamage, "Holy resistance lowers it");
    // Warcry Horn: weapon power +15% for the first 2 turns; Ironskin Phial: an 8% barrier.
    ({ battle, player, foes } = await artifactBattle(h, [artifactId("Warcry Horn"), artifactId("Ironskin Phial")]));
    const warcry = player.effects.find(effect => effect.type === "outgoing-attack-damage-percent");
    assert(warcry?.amount === 15 && warcry.expiresTurn - player.turns <= 3 && warcry.expiresTurn === 3, "Warcry Horn: +15% weapon damage through your second turn");
    assert(player.barrierHp === Math.floor(h.values(player).maxHp * 8 / 100), "Ironskin Phial: a barrier of 8% maximum HP");
    // Rallying Banner Pin: you and every ally +6% attributes for 2 turns.
    ({ battle, player, foes } = await artifactBattle(h, [artifactId("Rallying Banner Pin")], { companion: { worn: [] } }));
    const ally = battle.actors.find(actor => actor.companion);
    assert([player, ally].every(actor => actor.effects.some(effect => effect.type === "all-stats-percent" && effect.amount === 6 && effect.expiresTurn === 3)), "Rallying Banner Pin: +6% attributes for you and your allies");
    assert(!foes.some(foe => foe.effects.some(effect => effect.type === "all-stats-percent")), "not the enemies");
    // Hourglass of Haste: +1 AP on the wearer's first turn only.
    ({ battle, player, foes } = await artifactBattle(h, [artifactId("Hourglass of Haste")]));
    let firstTurnAp = null;
    for (let index = 0; index < 40 && firstTurnAp == null; index++) { const next = index === 0 && battle.current === "player" ? player : h.advance(); if (next?.id === "player") firstTurnAp = battle.ap; }
    assert(firstTurnAp === h.actionPointMaximum(player) + 1, `Hourglass of Haste: +1 AP on your first turn (${firstTurnAp})`);
    let laterAp = null;
    for (let index = 0; index < 40 && laterAp == null; index++) { const next = h.advance(); if (next?.id === "player") laterAp = battle.ap; }
    assert(laterAp === h.actionPointMaximum(player), "not on later turns");
    return { passed: true, checks: count() };
  } finally { Math.random = random; Combat.restore(null); }
}

// Spells and healing: Echo Bell, Stormcatcher Coil, Forked Lightning Rod (A24, A31), Bloodmoon Chalice, Triage Pouch and
// Mender's Thimble with Doctor's Orders (A17, A21).
async function checkArtifactSpellsAndHealing() {
  const { assert, count } = artifactAssert();
  const h = await artifactHooks();
  const random = Math.random;
  const caster = { knownSpells: ["fire-bolt-fire-damage", "minor-firewall-fire-damage"] };
  const castAt = (battle, player, target, spellId) => { battle.current = "player"; battle.ap = 9; currentMana = 999; return h.cast(player, target, getSpell(spellId)); };
  try {
    // Echo Bell: 10% chance a spell costs no Mana.
    let { battle, player, foes } = await artifactBattle(h, [artifactId("Echo Bell")], { changes: caster });
    Math.random = () => 0.5;
    assert(castAt(battle, player, foes[0], "fire-bolt-fire-damage") && currentMana < 999, "Echo Bell's 10% can miss");
    Math.random = () => 0.05;
    ({ battle, player, foes } = await artifactBattle(h, [artifactId("Echo Bell")], { changes: caster }));
    assert(castAt(battle, player, foes[0], "fire-bolt-fire-damage") && currentMana === 999 && battle.log.some(entry => entry.includes("costs no Mana")), "and makes the spell free when it lands");
    // Stormcatcher Coil: an enemy spell that damages you restores 4% Mana.
    ({ battle, player, foes } = await artifactBattle(h, [artifactId("Stormcatcher Coil")]));
    h.setMana(player, 0); h.setHp(player, 1000);
    h.hurt(player, 30, "fire", false, true, foes[0], "spell");
    assert(h.mana(player) === Math.floor(h.values(player).maxMana * 4 / 100), "Stormcatcher Coil restores 4% Mana from a hostile spell");
    h.setMana(player, 0); h.hurt(player, 30, "physical", false, false, foes[0], "melee");
    assert(h.mana(player) === 0, "not from a weapon hit");
    // Forked Lightning Rod: one strike per cast for 25% of the largest damage, on an enemy the spell didn't hit.
    Math.random = () => 0;
    ({ battle, player, foes } = await artifactBattle(h, [artifactId("Forked Lightning Rod")], { enemies: ["Goblin", "Goblin", "Goblin"], changes: caster }));
    foes.forEach(foe => h.setHp(foe, 100000));
    castAt(battle, player, foes[0], "fire-bolt-fire-damage");
    const firstHit = 100000 - h.hp(foes[0]), forked = foes.slice(1).map(foe => 100000 - h.hp(foe)).filter(Boolean);
    assert(firstHit > 0 && forked.length === 1 && forked[0] <= Math.floor(firstHit * 25 / 100) && battle.log.some(entry => entry.includes("forks to")), `A single-target spell forks once to another enemy (${firstHit} → ${forked})`);
    ({ battle, player, foes } = await artifactBattle(h, [artifactId("Forked Lightning Rod")], { enemies: ["Goblin", "Goblin"], changes: caster }));
    foes.forEach(foe => h.setHp(foe, 100000));
    castAt(battle, player, foes[0], "minor-firewall-fire-damage");
    assert(!battle.log.some(entry => entry.includes("forks to")), "A spell that hit every enemy has no one left to fork to");
    // Bloodmoon Chalice: weapons and spells heal 10% of the damage.
    ({ battle, player, foes } = await artifactBattle(h, [artifactId("Bloodmoon Chalice")], { enemies: ["Goblin"], changes: caster }));
    h.setHp(foes[0], 100000); h.setHp(player, 1);
    castAt(battle, player, foes[0], "fire-bolt-fire-damage");
    const spellDamage = 100000 - h.hp(foes[0]);
    assert(spellDamage > 0 && h.hp(player) - 1 === Math.floor(spellDamage * 10 / 100), `Bloodmoon Chalice heals 10% of spell damage (${spellDamage} → ${h.hp(player) - 1})`);
    // Triage Pouch (received ×1.10) and Mender's Thimble (given ×1.08) multiply on top of everything (A17, A21).
    ({ battle, player, foes } = await artifactBattle(h, [artifactId("Triage Pouch"), artifactId("Mender's Thimble")], { companion: { worn: [] } }));
    const mira = battle.actors.find(actor => actor.companion);
    assert(Math.abs(h.healingFactor(player, player) - 1.08 * 1.10) < 1e-9, "A self-heal takes both, multiplied (A21)");
    assert(Math.abs(h.healingFactor(player, foes[0]) - 1.10) < 1e-9 && Math.abs(h.healingFactor(mira, player) - 1.08) < 1e-9, "Received and given apart");
    const robe = ForgeRules.encode(ForgeRules.generateMasterwork({ key: "doctors-orders", level: 30 }));
    ({ battle, player, foes } = await artifactBattle(h, [artifactId("Triage Pouch"), artifactId("Mender's Thimble")], { changes: { inventory: [artifactId("Triage Pouch"), artifactId("Mender's Thimble"), robe] } }));
    equipment.armor = robe;
    assert(Math.abs(h.healingFactor(player, player) - 1.5 * 1.08 * 1.10) < 1e-9, "With Doctor's Orders: the robe's self-heal ×1.5, times both artifacts");
    return { passed: true, checks: count() };
  } finally { Math.random = random; Combat.restore(null); }
}

// Falls and rewards: Battle Hymn Chime and Soulharvest Sickle (A22), Phoenix Plume (A18, A29), Bandit's Coin and
// Studious Quill (A25).
async function checkArtifactFallsAndRewards() {
  const { assert, count } = artifactAssert();
  const h = await artifactHooks();
  // Battle Hymn Chime: whenever an enemy falls to your side, 5% HP and 5% Mana, whoever defeated it.
  let { battle, player, foes, ally } = await artifactBattle(h, [artifactId("Battle Hymn Chime")], { companion: { worn: [] } });
  h.setHp(player, 1); h.setMana(player, 0);
  h.directDamage(foes[0], 100000, false, "physical", ally);
  const maximum = h.values(player);
  assert(h.hp(player) - 1 === Math.max(1, Math.floor(maximum.maxHp * 5 / 100)) && h.mana(player) === Math.floor(maximum.maxMana * 5 / 100), "Battle Hymn Chime restores 5% HP and Mana when an ally's blow fells an enemy");
  ({ battle, player, foes, ally } = await artifactBattle(h, [artifactId("Soulharvest Sickle")]));
  h.setHp(player, 1);
  foes[0].effects.push({ key: "test-burn", name: "Test Burn", type: "turn-start-max-hp-loss-percent", amount: 100, expiresTurn: foes[0].turns + 5 });
  h.expire(foes[0]);
  assert(h.hp(foes[0]) === 0 && h.hp(player) - 1 === Math.max(1, Math.floor(h.values(player).maxHp * 15 / 100)), "Soulharvest Sickle restores 15% HP, even when burning fells the enemy");
  // Phoenix Plume: the first hero to fall rises at 30% at the start of the wearer's next turn.
  ({ battle, player, foes, ally } = await artifactBattle(h, [artifactId("Phoenix Plume")], { companion: { worn: [] } }));
  h.directDamage(ally, 100000, false, "physical", foes[0]);
  assert(h.hp(ally) === 0 && battle.phoenix?.player?.fallenId === ally.id, "Phoenix Plume claims the first hero who falls");
  h.expire(player);
  assert(h.hp(ally) === Math.floor(h.values(ally).maxHp * 30 / 100), "who rises at 30% HP at the start of the wearer's turn");
  h.directDamage(ally, 100000, false, "physical", foes[0]); h.expire(player);
  assert(h.hp(ally) === 0, "only once per battle");
  ({ battle, player, foes, ally } = await artifactBattle(h, [artifactId("Phoenix Plume")], { companion: { worn: [] } }));
  h.summonFriendly(player, { type: "summon-friendly", enemyId: "steelguard-golem-level-35" }, "Test summon");
  const golem = battle.actors.find(actor => actor.summoned && actor.team === "player");
  h.directDamage(golem, 1e7, false, "physical", foes[0]);
  assert(!battle.phoenix?.player, "Summons don't count (A18)");
  // The wearer falling first rises when their turn comes round (A29).
  h.directDamage(player, 1e7, false, "physical", foes[0]);
  assert(h.hp(player) === 0 && battle.phoenix.player.fallenId === "player", "The wearer can be the first to fall");
  let rose = false;
  for (let index = 0; index < 40 && !rose; index++) { const next = h.advance(); if (next?.id === "player") rose = h.hp(player) > 0; }
  assert(rose && h.hp(player) === Math.floor(h.values(player).maxHp * 30 / 100), "and rises at 30% when their turn comes, taking it");
  // Bandit's Coin: the party's silver, whoever wears it; Studious Quill: the wearer's EXP (A25).
  const reward = async (playerWorn, companionWorn) => {
    const { battle, player, foes, ally } = await artifactBattle(h, playerWorn, { enemies: ["Steelguard Golem"], level: 10, companion: { worn: companionWorn } });
    companions = [{ id: ally.id, name: ally.name, character: ally.character }];
    const silver = ShopRules.walletValue(currencies), playerExp = exp, allyExp = ally.character.exp;
    h.directDamage(foes[0], 1e7, false, "physical", player);
    h.awardDefeatedEnemyExperience();
    companions = [];
    return { silver: ShopRules.walletValue(currencies) - silver, playerExp: exp - playerExp, allyExp: ally.character.exp - allyExp };
  };
  const plain = await reward([], []), coin = await reward([], [artifactId("Bandit's Coin")]), quill = await reward([], [artifactId("Studious Quill")]);
  assert(plain.silver === 35 * 3 && coin.silver === Math.ceil(35 * 3 * 1.15), `Bandit's Coin on a companion raises the party's silver by 15% (${plain.silver} → ${coin.silver})`);
  assert(quill.allyExp === Math.floor(plain.allyExp * 1.1) && quill.playerExp === plain.playerExp && plain.playerExp > 0, `Studious Quill raises only its wearer's EXP (${plain.allyExp} → ${quill.allyExp})`);
  Combat.restore(null);
  return { passed: true, checks: count() };
}

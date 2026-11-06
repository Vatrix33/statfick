/* Port task 7, New Game Plus (2026-11-06): Limitless, the Exotic rarity, Exium and the five Exotics. The design answers
   L1–L28 are in engine/Docs/Engine-Plan.md, "Decisions for task 7, New Game Plus"; old-game/Docs/Notes.md,
   "Limitless and the Exotics", says where each rule lives. One entry per part. */
const limitlessChecks = {
  counter() {
    let checks = 0;
    return { assert: (condition, message) => { if (!condition) throw Error(message); checks++; }, count: () => checks };
  },
  // Math.random answering from a list (then from the fallback), for rolls in a known order.
  dice(values, fallback = 0.5) {
    const queue = [...values];
    return () => queue.length ? queue.shift() : fallback;
  },
  // Combat's own functions, from a second copy of combat.js with a hook (as the earlier tasks' checks do).
  async hooks() {
    if (window.__limitlessChecks) return window.__limitlessChecks;
    Combat.restore(null);
    const source = await (await fetch("scripts/combat.js")).text();
    const marker = "  window.Combat = {";
    const script = document.createElement("script");
    script.textContent = source.replace(marker, "  window.__limitlessChecks = { actorById, hp, setHp, values, getBattle: () => battle, attack, strike, hurt, healHp, applyCombatDamage, applyUniqueWeaponHitEffects, awardDefeatedEnemyExperience, rollExoticExtras, rollSparklingMasterworks, sparklingMasterworkPool, refresh, definitions: () => enemyDefinitions, EXOTIC_DROPS, skillsOf, classSkillKey, skillStrikes, oneStrikeATurn };\n" + marker);
    document.head.append(script);
    await waitForSourceFiles();
    if (window.BattleStage) window.BattleStage.enabled = () => false;
    if (window.GameSettings) window.GameSettings.enemyDelay = () => 1e9;
    // The hook copy loads its enemy definitions on its first battle.
    await Combat.startEncounter(["Goblin"], "Task 7 checks"); Combat.restore(null);
    return window.__limitlessChecks;
  },
  // A hero (Level 100 unless told), Limitless unlocked and on or off, with gear and up to three companions.
  hero({ level: heroLevel = 100, on = false, unlocked = true, inventory: pack = [], equipment: slots = {}, companions: party = [], stats: extra = {} } = {}) {
    Combat.restore(null);
    const base = defaultCharacter();
    applyCharacter(normalizeCharacter({ ...base, raceId: "human", level: heroLevel, stats: { ...base.stats, ...extra }, inventory: [...pack], equipment: { ...base.equipment, ...slots }, limitless: { unlocked, active: on } }));
    companions = party.map((member, index) => ({ id: "companion-limitless-test-" + index, name: ["Mira", "Oren", "Tess"][index], gender: "female",
      character: normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: member.level || heroLevel, inventory: [...(member.inventory || [])], equipment: { ...defaultCharacter().equipment, ...(member.equipment || {}) } }, { companion: true, limitlessActive: on }) }));
  },
  async battle(h, enemies, options = {}, sparkling = []) {
    limitlessChecks.hero(options);
    if (!await Combat.startEncounter(enemies, "Task 7 test", null, sparkling)) throw Error("The task 7 test battle starts");
    const battle = h.getBattle();
    return { battle, player: h.actorById("player"), foes: battle.actors.filter(actor => actor.team === "enemy"), allies: battle.actors.filter(actor => actor.team === "player" && actor.id !== "player") };
  },
  forged: (key, atLevel = 100, random = Math.random) => ForgeRules.encode(ForgeRules.generateMasterwork({ key, level: atLevel, random })),
  // A forged item's effects, swapped for a moment (getItem hands out one cached object per id).
  withoutEffect(itemId, kind, action) {
    const item = getItem(itemId), saved = item.effects;
    item.effects = saved.filter(effect => effect.kind !== kind);
    try { return action(); } finally { item.effects = saved; }
  },
  async withRandom(random, action) {
    const saved = Math.random;
    Math.random = random;
    try { return await action(); } finally { Math.random = saved; }
  },
};

// Section 5 and 6, L11, L12, L23, L24, L28: the Exotic rarity, Exium and how the five are forged; never in Sparkling's
// rolls or a shop; ids that save and load (Cherubim's four rune slots included).
async function checkExoticForging() {
  const { assert, count } = limitlessChecks.counter();
  await waitForSourceFiles();
  const F = ForgeRules;
  assert(JSON.stringify(F.EXOTIC_IDS) === JSON.stringify(["fletcher", "tough-out", "winged-hussar", "cherubim", "connected-balancer"]), "Five Exotics");
  assert(F.EXOTIC_IDS.every(key => !F.MASTERWORK_IDS.includes(key)), "The masterwork list leaves them out (Sparkling's rolls, shops and the Guild read it)");
  assert(F.RARITIES.at(-1) === "exotic" && F.RARITIES.at(-2) === "masterwork" && F.RARITY_PRICE.exotic === F.RARITY_PRICE.masterwork, "Exotic is the rarity above masterwork, priced like one");
  const exium = F.MATERIALS.exium, mythilium = F.MATERIALS.mythilium;
  assert(exium.rarity === "exotic" && !exium.natural && ["finalPct", "hit", "crit", "pierce"].every(key => exium.effect[key] === mythilium.effect[key] + 3), "Exium: Mythilium's values +3 each (L11), never on generated weapons");
  const expected = {
    fletcher: ["dagger", "abyss", "weapon"], "tough-out": ["tower-shield", "hearthforge", "armor"], "winged-hussar": ["longsword", "roundtable", "weapon"],
    cherubim: ["lance", "church-of-talos", "weapon"], "connected-balancer": ["brigandine", "boreal-bolt", "armor"],
  };
  for (const [key, [type, maker, category]] of Object.entries(expected)) {
    const id = limitlessChecks.forged(key, 150, () => 0.1), item = getItem(id);
    assert(item?.rarity === "exotic" && item.category === category && item.forged.type === type && item.forged.parts[0].maker === maker, `${key}: an Exotic ${type} by ${maker}`);
    assert(item.forged.level === 150 && F.encode(F.decode(id)) === id, `${key}: forged at the finder's level and saved by its id`);
    assert(item.forged.parts[0].material === (category === "weapon" ? "exium" : "mythilium"), `${key}: its main part is ${category === "weapon" ? "Exium" : "Mythilium"}`);
    assert(/^An exotic /.test(item.description) && getItem("masterwork-" + key)?.rarity === "exotic", `${key}: described as Exotic, with a template for drop tables`);
    assert(itemRarityTag(item.rarity).includes("Exotic"), `${key}: tagged Exotic`);
  }
  // The weapons' other parts: Exium a quarter of the time (L28), otherwise any fitting material.
  let exiumParts = 0, others = 0;
  for (let index = 0; index < 1200; index++) for (const part of F.generateMasterwork({ key: "fletcher", level: 120 }).parts.slice(1)) { others++; if (part.material === "exium") exiumParts++; }
  assert(Math.abs(exiumParts / others - 0.25) < 0.04 && F.generateMasterwork({ key: "connected-balancer", level: 120, random: () => 0 }).parts.every(part => part.material === "mythilium"), `Other parts: Exium about a quarter of the time (${(exiumParts / others).toFixed(3)}); armor Exotics stay Mythilium`);
  assert(F.reforgeOptions(F.generateMasterwork({ key: "winged-hussar", level: 120, random: () => 0.9 }), 1).some(option => option.material === "exium")
    && !F.reforgeOptions(F.generateMasterwork({ key: "stunted-sword", level: 120, random: () => 0.9 }), 1).some(option => option.material === "exium"), "Reforging offers Exium for an Exotic weapon, never for a masterwork");
  // Ordinary pieces and masterworks never carry Exium.
  const sword = F.encode(F.generateMasterwork({ key: "stunted-sword", level: 50, random: () => 0.9 }));
  assert(F.decode(sword) && !F.decode(sword.replace(/-(..)myt-/, "-$1exi-")), "A masterwork can't carry Exium");
  const plain = F.encode(F.generate({ type: "longsword", level: 50, source: "shop", random: () => 0.5 }));
  assert(F.decode(plain) && !F.decode(plain.replace(/^(weapon-forged-longsword-50-)(..)(...)/, "$1$2exi")), "Nor an ordinary forged weapon");
  // Cherubim: four rune slots, fixed; Holy; +12% final power and +12 Armor Pierce per rune (L23); runes mix in (L24).
  const lance = F.generateMasterwork({ key: "cherubim", level: 100, random: () => 0.9 });
  for (let index = 0; index < 30; index++) assert(F.generateMasterwork({ key: "cherubim", level: 1 + index * 6 }).slots === 4, "Cherubim always has four slots");
  const lanceId = F.encode(lance);
  assert(!F.decode(lanceId.replace("-s4", "-s3")) && !F.decode(lanceId.replace("-s4", "-s2")), "Its id must say four slots");
  const fletcher = F.encode(F.generateMasterwork({ key: "fletcher", level: 100, random: () => 0.9 }));
  assert(!F.decode(fletcher.replace(/-s\d/, "-s4")), "Other weapons have three slots at most");
  const runeSets = [[], ["heft"], ["heft", "precision"], ["heft", "precision", "vital-hit"], ["empower", "precision", "vital-hit", "cinder"]];
  const plainTotals = runes => runes.reduce((sum, rune) => ({ finalPct: sum.finalPct + (F.RUNES[rune].effect.finalPct || 0), pierce: sum.pierce + (F.RUNES[rune].effect.pierce || 0) }), { finalPct: 0, pierce: 0 });
  const bare = getItem(lanceId);
  for (const runes of runeSets) {
    const item = getItem(F.encode({ ...lance, runes })), own = plainTotals(runes);
    assert(item.finalPowerPercent === bare.finalPowerPercent + own.finalPct + 12 * runes.length, `${runes.length} rune(s): +${12 * runes.length}% final power (${item.finalPowerPercent})`);
    assert(item.armorPierce === Math.min(100, bare.armorPierce + own.pierce + 12 * runes.length), `${runes.length} rune(s): +${12 * runes.length} Armor Pierce (${item.armorPierce})`);
    assert(item.element === "holy", `${runes.length} rune(s): it deals Holy damage`);
  }
  const fire = getItem(F.encode({ ...lance, runes: ["cinder"] }));
  assert(fire.element === "holy" && fire.elementShare?.element === "fire" && fire.elementShare.percent === 50, "An elemental rune mixes in its element at 50% (L24)");
  assert(getItem(F.encode({ ...lance, runes: ["heft"] })).armorPierce > getItem(F.encode(F.decode(F.encode({ ...lance, runes: [] })))).armorPierce, "Removing a rune removes its bonus");
  // Never generated or offered: Sparkling's pool and the shops' masterwork offers leave them out.
  const h = await limitlessChecks.hooks();
  assert(!h.sparklingMasterworkPool().some(entry => /fletcher|tough-out|winged-hussar|cherubim|connected-balancer/.test(entry.id)), "Sparkling's pool has no Exotic");
  assert(!F.EXOTIC_IDS.some(key => F.MASTERWORKS[key].shopOffer || F.MASTERWORKS[key].soldAt), "No shop sells one");
  return count();
}

// 7.1, L26: Fletcher's hits take 20% of the target's Armor for 1 turn (40% on a critical hit, not both), each hit its own.
async function checkFletcher() {
  const { assert, count } = limitlessChecks.counter();
  const h = await limitlessChecks.hooks();
  const dagger = limitlessChecks.forged("fletcher", 100);
  assert(getItem(dagger).effects.some(effect => effect.kind === "armor-break-on-hit" && effect.amount === 20 && effect.critAmount === 40 && effect.durationTurns === 1), "Fletcher's effect: 20%, 40% on a crit, 1 turn");
  const { battle, player, foes } = await limitlessChecks.withRandom(() => 0.5, () => limitlessChecks.battle(h, ["Orc"], { on: true, inventory: [dagger], equipment: { primaryHand: dagger } }));
  const foe = foes[0], weapon = getItem(dagger), armor = () => h.values(foe).armor, breaks = () => foe.effects.filter(effect => String(effect.key).startsWith("exotic-armor-break:"));
  const before = armor();
  assert(before > 10, `The Orc has Armor (${before})`);
  h.applyUniqueWeaponHitEffects(player, foe, weapon, 10, 1, false);
  assert(breaks().length === 1 && breaks()[0].amount === -20 && breaks()[0].expiresTurn === foe.turns + 1 && Math.abs(armor() - Math.floor(before * 0.8)) <= 1, `A hit: −20% Armor for 1 turn (${before} → ${armor()})`);
  foe.effects = foe.effects.filter(effect => !breaks().includes(effect));
  h.applyUniqueWeaponHitEffects(player, foe, weapon, 10, 1, true);
  assert(breaks().length === 1 && breaks()[0].amount === -40 && Math.abs(armor() - Math.floor(before * 0.6)) <= 1, `A critical hit: −40%, not −60% (${armor()})`);
  h.applyUniqueWeaponHitEffects(player, foe, weapon, 10, 1, false);
  assert(breaks().length === 2 && Math.abs(armor() - Math.floor(before * 0.4)) <= 1, `Each hit adds its own (L26): −40% and −20% (${armor()})`);
  for (let index = 0; index < 4; index++) h.applyUniqueWeaponHitEffects(player, foe, weapon, 10, 1, true);
  assert(armor() === 0, "Armor never goes below 0");
  // The armor comes back when they end (their turn's expiry).
  foe.turns += 1; foe.effects = foe.effects.filter(effect => effect.expiresTurn > foe.turns);
  assert(breaks().length === 0 && armor() === before, "After 1 turn the Armor is back");
  // A real attack passes on whether it was critical.
  h.setHp(foe, 1e7); battle.current = "player"; battle.ap = 9;
  await limitlessChecks.withRandom(() => 0, () => h.strike(player, foe, weapon));
  assert(breaks().length === 1 && breaks()[0].amount === -40, "A critical attack breaks 40%");
  foe.effects = foe.effects.filter(effect => !breaks().includes(effect));
  await limitlessChecks.withRandom(limitlessChecks.dice([0, 0.999]), () => h.strike(player, foe, weapon));
  assert(breaks().length === 1 && breaks()[0].amount === -20, "A plain hit breaks 20%");
  foe.effects = foe.effects.filter(effect => !breaks().includes(effect));
  await limitlessChecks.withRandom(() => 0.9999, () => h.strike(player, foe, weapon));
  assert(breaks().length === 0, "A miss breaks nothing");
  // A debuff-immune target (a Satyr) resists it, like the other on-hit Armor breaks.
  const satyr = await limitlessChecks.withRandom(() => 0.5, () => limitlessChecks.battle(h, ["Satyr Warrior"], { on: true, inventory: [dagger], equipment: { primaryHand: dagger } }));
  h.applyUniqueWeaponHitEffects(satyr.player, satyr.foes[0], weapon, 10, 1, true);
  assert(!satyr.foes[0].effects.some(effect => String(effect.key).startsWith("exotic-armor-break:")), "A debuff-immune target resists it");
  Combat.restore(null);
  return count();
}

// 7.2: Tough-Out: damage that would bring its bearer to 0 HP has a 25% chance to leave them at 1 HP, each time.
async function checkToughOut() {
  const { assert, count } = limitlessChecks.counter();
  const h = await limitlessChecks.hooks();
  const shield = limitlessChecks.forged("tough-out", 100);
  assert(getItem(shield).type === "shield" && getItem(shield).effects.some(effect => effect.kind === "lethal-survive-chance" && effect.amount === 25), "A shield with a 25% chance");
  const { player, foes, allies } = await limitlessChecks.withRandom(() => 0.5, () => limitlessChecks.battle(h, ["Orc"], { on: true, inventory: [shield], equipment: { offHand: shield }, companions: [{}] }));
  const hit = async (amount, roll, attacker = foes[0], start = 50) => { h.setHp(player, start); await limitlessChecks.withRandom(limitlessChecks.dice([roll], 0.99), () => h.applyCombatDamage(player, amount, attacker)); return h.hp(player); };
  assert(await hit(500, 0.2499) === 1, "A lethal blow and a roll under 25%: 1 HP");
  assert(await hit(500, 0.25) === 0, "A roll of 25% or more: the blow lands");
  assert(await hit(50, 0.1) === 1, "Exactly lethal counts too");
  assert(await hit(500, 0.1, null) === 1, "Damage with no source (over time) counts");
  assert(await hit(500, 0.1, allies[0]) === 0, "Damage from an ally doesn't");
  assert(await hit(20, 0) === 30, "Damage that isn't lethal: no roll, nothing changes");
  assert(await hit(500, 0.1, foes[0], 1) === 1, "At 1 HP it can save them again");
  h.setHp(player, 0);
  assert(h.applyCombatDamage(player, 500, foes[0]) === 0 && h.hp(player) === 0, "Never at 0 HP");
  // An enemy's attack, through the whole damage path.
  h.setHp(player, 5);
  await limitlessChecks.withRandom(limitlessChecks.dice([0.1], 0.1), () => h.hurt(player, 5000, "physical", false, false, foes[0], "melee"));
  assert(h.hp(player) === 1, "An enemy's blow, every rule applying, leaves 1 HP");
  // Without the shield: no saves.
  const plain = await limitlessChecks.withRandom(() => 0.5, () => limitlessChecks.battle(h, ["Orc"], { on: true }));
  h.setHp(plain.player, 50);
  await limitlessChecks.withRandom(() => 0, () => h.applyCombatDamage(plain.player, 500, plain.foes[0]));
  assert(h.hp(plain.player) === 0, "Without Tough-Out the blow lands");
  Combat.restore(null);
  return count();
}

// 7.3, L14: Winged Hussar: +15% Evasion while held; +1% damage for every 4 Evasion, its own +15% counted.
async function checkWingedHussar() {
  const { assert, count } = limitlessChecks.counter();
  const h = await limitlessChecks.hooks();
  const sword = limitlessChecks.forged("winged-hussar", 100);
  const item = getItem(sword);
  assert(item.forged.type === "longsword" && item.effects.some(effect => effect.kind === "evasion-percent" && effect.amount === 15) && item.effects.some(effect => effect.kind === "damage-per-evasion" && effect.amount === 1 && effect.evasion === 4), "+15% Evasion, +1% damage per 4 Evasion");
  const options = { on: true, inventory: [sword], equipment: { primaryHand: sword }, stats: { dexterity: 300, speed: 300 } };
  const { battle, player, foes } = await limitlessChecks.withRandom(() => 0.5, () => limitlessChecks.battle(h, ["Orc"], options));
  const evasion = h.values(player).evasion;
  const plain = limitlessChecks.withoutEffect(sword, "evasion-percent", () => h.values(player).evasion);
  assert(Math.abs(evasion - Math.floor(plain * 1.15)) <= 1, `Evasion +15% while held (${plain} → ${evasion})`);
  assert(calculateEquipmentDefense(currentStats()).evasionPercent >= 15, "The sheet counts it too");
  // Damage: the same attack with and without the Evasion bonus.
  const foe = foes[0];
  const attack = async strip => {
    h.setHp(foe, 1e7); battle.current = "player"; battle.ap = 9; foe.effects = [];
    await limitlessChecks.withRandom(limitlessChecks.dice([0, 0.999], 0.5), () => strip ? limitlessChecks.withoutEffect(sword, "damage-per-evasion", () => h.strike(player, foe, item)) : h.strike(player, foe, item));
    return 1e7 - h.hp(foe);
  };
  const without = await attack(true), withBonus = await attack(false), percent = Math.floor(evasion / 4);
  assert(percent > 20 && Math.abs(withBonus / without - (1 + percent / 100)) < 0.02, `+${percent}% damage at ${evasion} Evasion (${without} → ${withBonus})`);
  // Not held: no Evasion bonus.
  const bare = await limitlessChecks.withRandom(() => 0.5, () => limitlessChecks.battle(h, ["Orc"], { ...options, equipment: {} }));
  assert(h.values(bare.player).evasion < evasion, "Only while it is held");
  Combat.restore(null);
  return count();
}

// 7.5, L19: Connected Balancer: damage and healing its wearer receives is cut by half; the half taken off is shared
// equally by every other living ally, and lost when there is none; a shared part never passes through one again.
async function checkConnectedBalancer() {
  const { assert, count } = limitlessChecks.counter();
  const h = await limitlessChecks.hooks();
  const coat = limitlessChecks.forged("connected-balancer", 100), coat2 = limitlessChecks.forged("connected-balancer", 100, () => 0.3);
  assert(getItem(coat).forged.type === "brigandine" && getItem(coat).effects.some(effect => effect.kind === "shared-burden" && effect.amount === 50), "A brigandine sharing 50%");
  const setup = async (party, extra = {}) => {
    const fight = await limitlessChecks.withRandom(() => 0.5, () => limitlessChecks.battle(h, ["Orc"], { on: true, inventory: [coat], equipment: { armor: coat }, companions: party, ...extra }));
    for (const actor of [fight.player, ...fight.allies]) h.setHp(actor, 1000);
    return fight;
  };
  const lost = actor => 1000 - h.hp(actor);
  // Two allies: half kept, the other half split equally.
  let fight = await setup([{}, {}]);
  h.applyCombatDamage(fight.player, 101, fight.foes[0]);
  assert(lost(fight.player) === 51 && fight.allies.every(ally => lost(ally) === 25), `101 damage: the wearer takes 51, each of two allies 25 (${lost(fight.player)}, ${fight.allies.map(lost)})`);
  // One ally: it takes the whole half.
  fight = await setup([{}]);
  h.applyCombatDamage(fight.player, 100, fight.foes[0]);
  assert(lost(fight.player) === 50 && lost(fight.allies[0]) === 50, "With one ally it takes the whole half");
  // A fallen ally doesn't share; nobody left: the half is lost.
  h.setHp(fight.allies[0], 0); h.setHp(fight.player, 1000);
  h.applyCombatDamage(fight.player, 100, fight.foes[0]);
  assert(lost(fight.player) === 50 && h.hp(fight.allies[0]) === 0, "No living ally: the wearer takes half and the rest is lost");
  fight = await setup([]);
  h.applyCombatDamage(fight.player, 100, fight.foes[0]);
  assert(lost(fight.player) === 50, "Alone: half, the rest lost");
  // Damage over time (no source) too.
  fight = await setup([{}]);
  h.applyCombatDamage(fight.player, 60, null);
  assert(lost(fight.player) === 30 && lost(fight.allies[0]) === 30, "Damage over time is shared too");
  // An ally wearing one too: what it is given is not split again, and the wearer gets nothing back.
  fight = await setup([{ inventory: [coat2], equipment: { armor: coat2 } }, {}]);
  h.applyCombatDamage(fight.player, 100, fight.foes[0]);
  assert(lost(fight.player) === 50 && fight.allies.every(ally => lost(ally) === 25), `No loop: a shared part is never shared again (${lost(fight.player)}, ${fight.allies.map(lost)})`);
  h.setHp(fight.player, 1000); for (const ally of fight.allies) h.setHp(ally, 1000);
  h.applyCombatDamage(fight.allies[0], 100, fight.foes[0]);
  assert(lost(fight.allies[0]) === 50 && lost(fight.player) === 25 && lost(fight.allies[1]) === 25, "The ally's own coat shares what it is hit with");
  // A share can bring an ally down.
  fight = await setup([{}]);
  h.setHp(fight.allies[0], 10);
  h.applyCombatDamage(fight.player, 100, fight.foes[0]);
  assert(h.hp(fight.allies[0]) === 0 && lost(fight.player) === 50, "A share can knock an ally out");
  // Healing: half to the wearer, the other half split; each ally's own healing rules apply.
  fight = await setup([{}, {}]);
  assert([fight.player, ...fight.allies].every(actor => h.values(actor).maxHp > 60), "Room to heal");
  for (const actor of [fight.player, ...fight.allies]) h.setHp(actor, 10);
  const gained = h.healHp(fight.player, 40, h.getBattle(), fight.player);
  assert(gained === 20 && h.hp(fight.player) === 30 && fight.allies.every(ally => h.hp(ally) === 20), `A heal of 40: 20 to the wearer, 10 to each ally (${h.hp(fight.player)}, ${fight.allies.map(h.hp)})`);
  // Without the coat nothing is shared.
  limitlessChecks.withoutEffect(coat, "shared-burden", () => {
    for (const actor of [fight.player, ...fight.allies]) h.setHp(actor, 1000);
    h.applyCombatDamage(fight.player, 100, fight.foes[0]);
    assert(lost(fight.player) === 100 && fight.allies.every(ally => lost(ally) === 0), "Without it the wearer takes it all");
  });
  // Summons count as allies (L19).
  fight = await setup([{}]);
  const summon = { ...structuredClone(fight.allies[0]), id: "summon-limitless-test", summoned: true, companion: undefined };
  h.getBattle().actors.push(summon); h.setHp(summon, 1000);
  h.setHp(fight.player, 1000); h.setHp(fight.allies[0], 1000);
  h.applyCombatDamage(fight.player, 100, fight.foes[0]);
  assert(lost(fight.allies[0]) === 25 && lost(summon) === 25, "A summon shares it like a hero");
  Combat.restore(null);
  return count();
}

// 7.1–7.5, 8, L15: where the Exotics drop, only while Limitless is on; Luck raises them, Limitless's ×2 doesn't; Connected
// Balancer from any Sparkling or Empowered enemy; Anatemis's reward at Level 200.
async function checkExoticDrops() {
  const { assert, count } = limitlessChecks.counter();
  const h = await limitlessChecks.hooks();
  const sources = {
    fletcher: [["Great Imp Ji'ruan", "Deep Sea King", "Archdevil Saffron"], 4], "tough-out": [["Ornn, God of the Forge", "Gaian, Avatar of the Land", "Ancient Lich Ool"], 5],
    "winged-hussar": [["Juila the Grand Forest Matriarch", "Nygheir the Wounded", "Zik Lun, the armored Hulk"], 4], cherubim: [["Tankina, Disciple of Talos", "Ur-Mat, the False Dreamer"], 5],
  };
  const definitions = h.definitions();
  for (const [key, [names, chance]] of Object.entries(sources)) {
    const holders = definitions.filter(enemy => (enemy.drops || []).some(drop => drop.itemId === "masterwork-" + key));
    assert(JSON.stringify(holders.map(enemy => enemy.name).sort()) === JSON.stringify([...names].sort()), `${key} drops from ${names.join(", ")} (${holders.map(enemy => enemy.name)})`);
    assert(holders.every(enemy => enemy.drops.find(drop => drop.itemId === "masterwork-" + key).chance === chance), `${key}: ${chance}%`);
  }
  assert(!definitions.some(enemy => (enemy.drops || []).some(drop => drop.itemId === "masterwork-connected-balancer")), "No drop table names Connected Balancer");
  // A boss's table: on, the 4% rolls (Luck added, not doubled); off, it never does.
  // roll: the chance (in %) that should just pass or just fail, from the hero's own Luck bonus.
  const drop = async (on, roll, luck = 0) => {
    const fight = await limitlessChecks.withRandom(() => 0.5, () => limitlessChecks.battle(h, ["Great Imp Ji'ruan"], { on, level: 120 - (on ? 0 : 20), stats: { luck } }));
    const boss = fight.foes.find(foe => foe.enemyDefinitionId === "great-imp-jiruan-unique-level-18");
    h.setHp(boss, 0); fight.battle.lootItems = [];
    const bonus = Math.max(0, currentStats().luck) * 0.05 + Math.max(0, calculateClassEffects().masterworkDropChanceFlat || 0);
    await limitlessChecks.withRandom(limitlessChecks.dice([], roll(bonus) / 100), () => h.awardDefeatedEnemyExperience());
    return { bonus, items: fight.battle.lootItems.filter(id => getItem(id)?.forged?.masterwork === "fletcher").map(id => getItem(id)) };
  };
  const won = await drop(true, bonus => 4 + bonus - 0.01);
  assert(won.items.length === 1 && won.items[0].rarity === "exotic" && won.items[0].forged.level === 120, "On: a roll under 4% (and the Luck bonus) drops Fletcher, forged at your level");
  assert((await drop(true, bonus => 4 + bonus + 0.01)).items.length === 0, "Its 4% is not doubled like a masterwork's");
  assert((await drop(false, () => 0)).items.length === 0, "Off: it never drops");
  const lucky = await drop(true, bonus => 4 + bonus - 0.01, 400);
  assert(lucky.items.length === 1 && lucky.bonus >= 20 && (await drop(true, bonus => 4 + bonus + 0.01, 400)).items.length === 0, `Luck raises it like a masterwork's: +0.05% per point (${lucky.bonus.toFixed(2)}%)`);
  // Connected Balancer: 4% from any Sparkling or Empowered enemy, only while Limitless is on.
  const id = ForgeRules.masterworkTemplateId("connected-balancer");
  limitlessChecks.hero({ on: true });
  const bonus = Math.max(0, currentStats().luck) * 0.05 + Math.max(0, calculateClassEffects().masterworkDropChanceFlat || 0);
  const extras = (actor, on, roll) => { limitless.active = on; return limitlessChecks.withRandom(limitlessChecks.dice([], roll), () => h.rollExoticExtras(actor)); };
  const under = (4 + bonus - 0.01) / 100, over = (4 + bonus + 0.01) / 100;
  assert(JSON.stringify(await extras({ sparkling: true }, true, under)) === JSON.stringify([id]) && JSON.stringify(await extras({ empowered: true }, true, under)) === JSON.stringify([id]), "A Sparkling or Empowered enemy: 4% (and the Luck bonus)");
  assert(!(await extras({ sparkling: true }, true, over)).length && !(await extras({}, true, 0)).length && !(await extras({ sparkling: true }, false, 0)).length, "Not above it, not from other enemies, not while off");
  // Anatemis at Level 200 with Limitless on: one Exotic of all five, and a second at 1% on its own.
  const anatemis = { enemyDefinitionId: LimitlessRules.ANATEMIS_ID };
  const reward = (on, heroLevel, rolls) => { limitless.active = on; level = heroLevel; return limitlessChecks.withRandom(limitlessChecks.dice(rolls, 0.5), () => h.rollExoticExtras(anatemis)); };
  for (const [index, key] of ForgeRules.EXOTIC_IDS.entries()) {
    const loot = await reward(true, 200, [index / 5 + 0.01, 0.5]);
    assert(loot.length === 1 && loot[0] === ForgeRules.masterworkTemplateId(key), `The reward can be ${key}`);
  }
  const two = await reward(true, 200, [0.01, 0.0099, 0.01]);
  assert(two.length === 2 && two[0] === two[1], "A second at 1%, which may be the same");
  assert((await reward(true, 200, [0.01, 0.0101])).length === 1, "Not above 1%");
  assert(!(await reward(true, 199, [0, 0])).length, "Not below Level 200");
  assert(!(await reward(false, 200, [0, 0])).length, "Not while Limitless is off");
  // The Guild's revealed odds follow the same rules (an Exotic's are 0% while off; a masterwork's double while on).
  const odds = GuildInformationRules.dropChance, exotic = getItem("masterwork-fletcher"), masterwork = getItem("masterwork-impshield");
  assert(odds(exotic, { chance: 4 }, 0, 0, false) === 0 && odds(exotic, { chance: 4 }, 0, 0, true) === 4 && odds(exotic, { chance: 4 }, 100, 0, true) === 9, "The Guild shows an Exotic's odds only while on, Luck added");
  assert(odds(masterwork, { chance: 10 }, 0, 0, true) === 20 && odds(masterwork, { chance: 10 }, 0, 0, false) === 10, "And a masterwork's doubled while on");
  Combat.restore(null);
  return count();
}

// 4, 5.2, L10, L25, L27: the Exotic look, the sheet while Limitless is on, gear above Level 100 while it's off (a red border
// and "Level N required"), Exotics at Level 100 or below still worn and working while it's off.
async function checkLimitlessLooks() {
  const { assert, count } = limitlessChecks.counter();
  await waitForSourceFiles();
  const high = ForgeRules.encode(ForgeRules.generate({ type: "longsword", level: 150, source: "shop", random: () => 0.5 }));
  const low = ForgeRules.encode(ForgeRules.generate({ type: "longsword", level: 100, source: "shop", random: () => 0.5 }));
  const exotic = limitlessChecks.forged("winged-hussar", 90);
  limitlessChecks.hero({ on: false, inventory: [high, low, exotic], equipment: { primaryHand: exotic } });
  assert(itemRarityClass(getItem(high)).includes("needs-limitless") && limitlessNote(getItem(high)).includes("Level 150 required"), "Off: a Level-150 piece is marked, \"Level 150 required\"");
  assert(!itemRarityClass(getItem(low)).includes("needs-limitless") && !limitlessNote(getItem(low)), "A Level-100 piece isn't");
  assert(!canEquipItem(getItem(high), "primaryHand") && canEquipItem(getItem(low), "primaryHand"), "Off: it can't be put on, Level 100 can");
  assert(equipment.primaryHand === exotic && canEquipItem(getItem(exotic), "primaryHand") && calculateEquipmentDefense(currentStats()).evasionPercent >= 15, "An Exotic at Level 90 stays worn and working while off (L25)");
  openInventory();
  const entry = [...document.querySelectorAll("#owned-items-list .inventory-entry")].find(node => node.dataset.itemId === high);
  assert(entry?.classList.contains("needs-limitless") && entry.textContent.includes("Level 150 required"), "The pack shows it");
  const border = getComputedStyle(entry);
  assert(border.borderTopColor === border.borderLeftColor && /rgb\(2(10|36), (58|95), (58|95)\)/.test(border.borderTopColor), `With a red border (${border.borderTopColor})`);
  assert(itemDetailMarkup(getItem(high)).includes("Level 150 required"), "Its detail says it too");
  document.querySelector("#inventory-modal .modal-close")?.click();
  limitless.active = true;
  assert(!itemRarityClass(getItem(high)).includes("needs-limitless") && canEquipItem(getItem(high), "primaryHand"), "On: no mark, and it can be worn");
  // The Exotic wave: green and purple, moving, on rarity tags and items.
  document.body.insertAdjacentHTML("beforeend", '<span id="exotic-probe" class="rarity-tag rarity-exotic">Exotic</span>');
  const tag = document.getElementById("exotic-probe"), style = getComputedStyle(tag);
  assert(style.animationName.includes("exotic-tone"), `The Exotic colour moves (${style.animationName})`);
  tag.remove();
  // The sheet while Limitless is on: the level and stat numbers in the wave.
  Limitless.render();
  assert(document.body.classList.contains("is-limitless") && getComputedStyle(document.getElementById("level-value")).animationName.includes("exotic-wave"), "On: the level number in the wave");
  limitless.active = false; Limitless.render();
  assert(!document.body.classList.contains("is-limitless") && getComputedStyle(document.getElementById("level-value")).animationName === "none", "Off: the normal sheet");
  // Winged Hussar's crossguard is a pair of wings: its drawing differs from a plain longsword's with the same parts.
  const hussar = getItem(exotic), plainLook = ForgeArt.weapon({ ...hussar, id: hussar.id + "-plain", forged: { ...hussar.forged, masterwork: "keening" } });
  assert(ForgeArt.weapon(hussar) !== plainLook, "Winged Hussar has its winged crossguard");
  Combat.restore(null);
  return count();
}

// 1, L17, L20, L21, 9.2: unlocked by Anatemis's first defeat (old saves start locked), the pop-up, the button, never
// switched in battle, saved with the character.
async function checkLimitlessUnlock() {
  const { assert, count } = limitlessChecks.counter();
  const h = await limitlessChecks.hooks();
  const old = normalizeCharacter({ ...defaultCharacter(), level: 100, exp: 0 });
  delete old.limitless;
  const loaded = normalizeCharacter(old);
  assert(loaded.limitless.unlocked === false && loaded.limitless.active === false, "An old save starts locked and off");
  assert(normalizeCharacter({ ...defaultCharacter(), limitless: { unlocked: false, active: true } }).limitless.active === false, "Locked means off");
  limitlessChecks.hero({ unlocked: false });
  const button = document.getElementById("limitless-toggle");
  Limitless.render();
  assert(button.hidden && !Limitless.setActive(true) && !limitless.active, "Locked: no button, and it can't be turned on");
  const fallen = { actors: [{ enemyDefinitionId: LimitlessRules.ANATEMIS_ID, summoned: false, character: { currentHp: 0 } }] };
  assert(!Limitless.onVictory({ actors: [{ enemyDefinitionId: "goblin", character: { currentHp: 0 } }] }) && !Limitless.onVictory({ actors: [{ ...fallen.actors[0], character: { currentHp: 5 } }] }), "Another victory, or Anatemis standing, unlocks nothing");
  assert(Limitless.onVictory(fallen) && limitless.unlocked && !limitless.active, "Anatemis's defeat unlocks it, still off");
  assert(!button.hidden && button.textContent.includes("Off") && button.getAttribute("aria-pressed") === "false", "The button appears, showing Off");
  const notice = document.getElementById("limitless-notice");
  assert(notice.hidden && Limitless.showPendingNotice() && !notice.hidden && /Limitless\s*unlocked/.test(notice.textContent), "The pop-up waits for the loot, then announces it");
  document.getElementById("limitless-notice-ok").click();
  assert(notice.hidden && !Limitless.showPendingNotice(), "Once");
  assert(!Limitless.onVictory(fallen), "A later defeat doesn't announce it again");
  button.click();
  assert(limitless.active && button.textContent.includes("On") && button.classList.contains("is-active") && document.body.classList.contains("is-limitless"), "The button turns it on and shows it");
  // Saved and loaded with the character.
  saveCharacter();
  const saved = normalizeCharacter(JSON.parse(localStorage.getItem(STORAGE_KEY)));
  assert(saved.limitless.unlocked && saved.limitless.active, "Saved on");
  // Never in battle, either way (L20, L21).
  await limitlessChecks.withRandom(() => 0.5, () => Combat.startEncounter(["Goblin"], "Task 7 test"));
  Limitless.render();
  assert(button.disabled && !Limitless.setActive(false) && limitless.active, "In battle the button is disabled and it stays on");
  Combat.restore(null); Limitless.render();
  assert(!button.disabled && Limitless.setActive(false) && !limitless.active, "After the battle it can be turned off");
  saveCharacter();
  assert(normalizeCharacter(JSON.parse(localStorage.getItem(STORAGE_KEY))).limitless.active === false, "Saved off");
  return count();
}

// 2.1, 3, L2, L4, L10, L18, L25: levels past 100 (no perk points), turning it off and on (back to the Level-100 values
// and forward again, any number of times, companions too), gear above 100 taken off, EXP only below the cap.
async function checkLimitlessLevels() {
  const { assert, count } = limitlessChecks.counter();
  const h = await limitlessChecks.hooks();
  const high = ForgeRules.encode(ForgeRules.generate({ type: "longsword", level: 140, source: "shop", random: () => 0.5 }));
  const lowExotic = limitlessChecks.forged("fletcher", 100);
  limitlessChecks.hero({ on: true, inventory: [high, lowExotic], equipment: { primaryHand: high, offHand: lowExotic }, companions: [{ level: 100 }] });
  const button = document.getElementById("level-up-button");
  const snapshot = () => JSON.stringify({ level, stats: { ...stats }, manualAllocations: { ...manualAllocations }, availablePoints });
  const step = (key, times) => { for (let index = 0; index < times; index++) document.querySelector(`.stat-stepper[data-stat="${key}"][data-action="increase"]`).click(); };
  // Two points spent at Level 100 (Strength) stay when it's turned off; only those spent above 100 are taken back.
  availablePoints += 2; createStatCards(); step("strength", 2);
  const level100 = snapshot(), perks = perkPoints;
  // 35 levels: +1 to every attribute and 5 free points each, no perk points.
  for (let index = 0; index < 35; index++) { exp = CombatRules.experienceRequired(level); button.click(); }
  assert(level === 135 && perkPoints === perks && availablePoints === JSON.parse(level100).availablePoints + 175, `Level 135, 175 more free points, no perk point (${level}, ${perkPoints})`);
  // Spend ten of them, in Vitality.
  step("vitality", 10);
  const level135 = snapshot();
  assert(stats.vitality === JSON.parse(level100).stats.vitality + 45 && JSON.parse(level100).manualAllocations.strength >= 2, "35 levels and ten points in Vitality; two in Strength from Level 100");
  // A companion past 100 too.
  const companion = companions[0].character;
  companion.level = 120; for (const key of LimitlessRules.STAT_KEYS) companion.stats[key] += 20; companion.availablePoints += 100;
  const companion120 = JSON.stringify(companion.stats);
  exp = 77;
  // Off: back to the Level-100 values; gear above 100 to the pack; an Exotic at 100 stays (L25).
  assert(Limitless.setActive(false), "It turns off");
  assert(snapshot() === level100 && exp === 0, "Off: Level 100 with exactly the Level-100 values");
  assert(companion.level === 100 && JSON.parse(JSON.stringify(companion.stats)).strength === JSON.parse(companion120).strength - 20, "The companion too");
  assert(equipment.primaryHand === null && inventory.includes(high) && equipment.offHand === lowExotic, "The Level-140 sword goes to the pack, the Level-100 Exotic stays on");
  // Nothing is gained at the cap while off (L18): the button is disabled, and refuses even if pressed.
  exp = CombatRules.experienceRequired(level); updateDerivedValues();
  assert(button.disabled, "At Level 100 while off the level-up button is disabled");
  button.disabled = false; button.click();
  assert(level === 100, "No level-up while off");
  const fight = await limitlessChecks.withRandom(() => 0.5, () => Combat.startEncounter(["Goblin"], "Task 7 test"));
  const battle = h.getBattle(); exp = 0;
  for (const foe of battle.actors.filter(actor => actor.team === "enemy")) h.setHp(foe, 0);
  h.awardDefeatedEnemyExperience();
  assert(fight && exp === 0, "No EXP at Level 100 while off");
  Combat.restore(null);
  // On again: everything back, any number of times.
  assert(Limitless.setActive(true) && snapshot() === level135 && exp === 77, "On: Level 135, its points and its EXP back");
  assert(JSON.stringify(companion.stats) === companion120 && companion.level === 120, "The companion's too");
  for (let round = 0; round < 3; round++) {
    Limitless.setActive(false);
    assert(snapshot() === level100, `Off again (round ${round + 1}): the same Level-100 values`);
    Limitless.setActive(true);
    assert(snapshot() === level135, `On again (round ${round + 1}): the same Level-135 values`);
  }
  // EXP while on below 200.
  await limitlessChecks.withRandom(() => 0.5, () => Combat.startEncounter(["Goblin"], "Task 7 test"));
  for (const foe of h.getBattle().actors.filter(actor => actor.team === "enemy")) h.setHp(foe, 0);
  const before = exp; h.awardDefeatedEnemyExperience();
  assert(exp > before, "On: EXP again");
  Combat.restore(null);
  // Loading: Level 100 without Limitless keeps no banked EXP (L18); the cap follows the switch.
  assert(normalizeCharacter({ ...defaultCharacter(), level: 100, exp: 5000 }).exp === 0, "An old save's EXP banked at 100 is cleared");
  assert(normalizeCharacter({ ...defaultCharacter(), level: 150, limitless: { unlocked: true, active: true } }).level === 150 && normalizeCharacter({ ...defaultCharacter(), level: 150 }).level === 100, "Above 100 only with Limitless on");
  assert(normalizeCharacter({ ...defaultCharacter(), level: 260, limitless: { unlocked: true, active: true } }).level === 200, "Never above 200");
  // Level 200: the cap.
  level = 199; exp = CombatRules.experienceRequired(level); button.click();
  exp = CombatRules.experienceRequired(level); button.click();
  assert(level === 200, "Level 200 is the cap");
  return count();
}

// 2.2–2.4, L3, L5, L6, L7, L29: enemies rise to your level (+1 per attribute per level, no free points), never down;
// every attribute ×1.25 once; bosses get the classes that bring them to 100; nothing while Limitless is off.
async function checkEnemyScaling() {
  const { assert, count } = limitlessChecks.counter();
  const h = await limitlessChecks.hooks();
  const definition = name => h.definitions().find(enemy => enemy.name === name);
  const fight = async (names, on, heroLevel) => {
    const result = await limitlessChecks.withRandom(() => 0.5, () => limitlessChecks.battle(h, names, { on, level: heroLevel }));
    return result.foes;
  };
  const goblin = definition("Goblin");
  const [plain] = await fight(["Goblin"], false, 100);
  assert(plain.character.level === goblin.character.level && plain.character.stats.strength === goblin.character.stats.strength, "Off: the Goblin as it is");
  const [risen] = await fight(["Goblin"], true, 150);
  const lift = 150 - goblin.character.level;
  assert(risen.character.level === 150, "On: it rises to your level");
  assert(LimitlessRules.STAT_KEYS.every(key => risen.character.stats[key] === Math.floor((goblin.character.stats[key] + lift) * 1.25)), "+1 to each attribute per level, then ×1.25 (no free points)");
  // Once: a saved battle loads without scaling again.
  const serialized = Combat.serialize();
  Combat.restore(serialized);
  const again = h.getBattle().actors.find(actor => actor.team === "enemy");
  assert(JSON.stringify(again.character.stats) === JSON.stringify(risen.character.stats) && again.character.level === 150, "Applied once: a loaded battle keeps the same values");
  // Above you: its own level, ×1.25 only (L5).
  const dragon = definition("Ancient Frost Dragon");
  const [above] = await fight(["Ancient Frost Dragon"], true, Math.max(1, dragon.character.level - 30));
  assert(above.character.level === dragon.character.level && above.character.stats.strength === Math.floor(dragon.character.stats.strength * 1.25), "An enemy above you keeps its level, ×1.25");
  // Bosses: the classes that bring them to 100 (L6, L29), only while on; never a hero class.
  const sum = levels => Object.values(levels || {}).reduce((total, value) => total + value, 0);
  for (const [id, classes] of Object.entries(LimitlessRules.BOSS_CLASSES)) {
    const boss = h.definitions().find(enemy => enemy.id === id);
    assert(boss?.unique, `${id} is a boss`);
    const merged = { ...boss.character.classLevels, ...classes };
    assert(sum(merged) === 100, `${boss.name}: 100 class levels with Limitless (${sum(merged)})`);
    for (const [classId, value] of Object.entries(classes)) {
      const cls = classCatalog.find(entry => entry.id === classId);
      assert(cls && value <= cls.maxLevel && value >= (boss.character.classLevels?.[classId] || 0), `${boss.name}: ${classId} ${value} is a real class within its maximum, never lowered`);
      assert(!["hero", "demon-lord", "raid-boss"].includes(classId), `${boss.name}: no hero or raid-boss class`);
    }
  }
  const saffron = definition("Archdevil Saffron");
  const [boss] = (await fight(["Archdevil Saffron"], true, 150)).filter(actor => actor.enemyDefinitionId === saffron.id);
  assert(sum(boss.character.classLevels) === 100 && boss.character.classLevels.warlock === 7, "On: Saffron fights with her 100 class levels");
  const [bossOff] = (await fight(["Archdevil Saffron"], false, 100)).filter(actor => actor.enemyDefinitionId === saffron.id);
  assert(sum(bossOff.character.classLevels) === sum(saffron.character.classLevels) && !bossOff.character.classLevels.warlock, "Off: her own classes only");
  const [goblinOn] = await fight(["Goblin"], true, 150);
  assert(JSON.stringify(goblinOn.character.classLevels || {}) === JSON.stringify(goblin.character.classLevels || {}), "A regular enemy gets no classes");
  // Her court rises too.
  const court = await fight(["Archdevil Saffron"], true, 150);
  assert(court.every(actor => actor.character.level >= 150), "Every enemy in the battle rises, her servants too");
  Combat.restore(null);
  return count();
}

// 2.5, L8: with Limitless on a masterwork's base chance doubles before the Luck bonus, and Sparkling's two rolls double.
async function checkMasterworkDropsDoubled() {
  const { assert, count } = limitlessChecks.counter();
  const h = await limitlessChecks.hooks();
  const drop = async (on, roll) => {
    const fight = await limitlessChecks.withRandom(() => 0.5, () => limitlessChecks.battle(h, ["Great Imp Ji'ruan"], { on, level: 100 }));
    const boss = fight.foes.find(foe => foe.enemyDefinitionId === "great-imp-jiruan-unique-level-18");
    h.setHp(boss, 0); fight.battle.lootItems = [];
    const bonus = Math.max(0, currentStats().luck) * 0.05 + Math.max(0, calculateClassEffects().masterworkDropChanceFlat || 0);
    await limitlessChecks.withRandom(limitlessChecks.dice([], roll(bonus) / 100), () => h.awardDefeatedEnemyExperience());
    return fight.battle.lootItems.some(id => getItem(id)?.forged?.masterwork === "impshield");
  };
  assert(await drop(true, bonus => 20 + bonus - 0.01) && !await drop(true, bonus => 20 + bonus + 0.01), "On: Impshield's 10% is 20%, then the Luck bonus");
  assert(await drop(false, bonus => 10 + bonus - 0.01) && !await drop(false, bonus => 10 + bonus + 0.01), "Off: 10% again");
  // Sparkling's rolls: 70% → 100%, 5% → 10%.
  limitlessChecks.hero({ on: true });
  const sparkle = async (on, rolls) => {
    limitless.active = on;
    const bonus = Math.max(0, currentStats().luck) * 0.05 + Math.max(0, calculateClassEffects().masterworkDropChanceFlat || 0);
    return (await limitlessChecks.withRandom(limitlessChecks.dice(rolls(bonus), 0.5), () => h.rollSparklingMasterworks({ sparkling: true }))).length;
  };
  assert(await sparkle(true, bonus => [0.9999, (10 + bonus - 0.01) / 100]) === 2, "On: the 70% roll always lands, the 5% is 10%");
  assert(await sparkle(true, bonus => [0.9999, (10 + bonus + 0.01) / 100]) === 1, "Not above 10%");
  assert(await sparkle(false, bonus => [(70 + bonus + 0.01) / 100, (5 + bonus - 0.01) / 100]) === 1, "Off: 70% and 5% again");
  Combat.restore(null);
  return count();
}

// L30, L31: a boss's damaging skill uses up its one strike a turn like an attack or a spell (one that hits every enemy
// weighs like an area spell); the burst and gamble skills stay usable.
async function checkBossStrikeLimit() {
  const { assert, count } = limitlessChecks.counter();
  const h = await limitlessChecks.hooks();
  const find = id => {
    for (const cls of classCatalog) for (const found of JSON.stringify(cls).includes(`"id":"${id}"`) ? [cls] : []) {
      const walk = node => node && typeof node === "object" ? (node.type === "skill" && node.id === id ? node : Object.values(node).map(walk).find(Boolean)) : null;
      const skill = walk(found);
      if (skill) return skill;
    }
    return null;
  };
  const expected = { "final-blow": 1, worldshaker: 1, "psychic-lance": 1, "chain-overload": 3, "pestilent-cloud": 3, rampage: 3, "double-down": 1, rage: 0, execute: 0, "magnum-opus": 0 };
  for (const [id, strikes] of Object.entries(expected)) {
    const skill = find(id);
    assert(skill, `${id} exists`);
    assert(h.skillStrikes(skill, 3) === strikes, `${id} counts as ${strikes} strike(s) (${h.skillStrikes(skill, 3)})`);
  }
  // Gaian with Limitless: Worldshaker (Geomancer) is held back once his two strikes are spent.
  const fight = await limitlessChecks.withRandom(() => 0.5, () => limitlessChecks.battle(h, ["Gaian, Avatar of the Land"], { on: true, level: 120 }));
  const gaian = fight.foes.find(foe => foe.enemyDefinitionId === "gaian-avatar-of-the-land-unique-level-53");
  const skills = h.skillsOf(gaian), shaker = skills.find(skill => skill.id === "worldshaker"), calm = skills.find(skill => !h.skillStrikes(skill, 1) && skill.id !== "mana-overload");
  assert(shaker && calm, "Gaian has Worldshaker with Limitless on");
  const candidates = [{ kind: "skill", key: h.classSkillKey(shaker), skill: shaker }, { kind: "skill", key: h.classSkillKey(calm), skill: calm }];
  const allowed = history => { gaian.ai = { ...(gaian.ai || {}), history: history.map(kind => ({ kind, key: kind === "skill" ? h.classSkillKey(shaker) : "attack", turn: gaian.turns })) }; return h.oneStrikeATurn(gaian, candidates, [], 2).candidates.map(candidate => candidate.skill.id); };
  assert(allowed(["attack"]).includes("worldshaker"), "One strike spent: Worldshaker can still come");
  assert(JSON.stringify(allowed(["attack", "attack"])) === JSON.stringify([calm.id]), "Two spent: Worldshaker is held back, other skills aren't");
  assert(!allowed(["skill", "skill"]).includes("worldshaker"), "A damaging skill used counts toward the limit");
  // L31: the burst and gamble skills are not on the bosses' don't-use lists.
  const source = await (await fetch("scripts/combat.js")).text();
  const profiles = source.slice(source.indexOf("const AI_PROFILES = {"), source.indexOf("const AI_PROFILES = {") + 20000);
  assert(!/oneStrikeATurn\(actor, candidates, \["mana-overload", "duel", "all-or-nothing", "perfect-form"\], 2\)[^\n]*arcane-chorus/.test(profiles), "Nothing new on the don't-use lists");
  Combat.restore(null);
  return count();
}

// L10 for companions: while Limitless is off they can't put on gear above Level 100 either (the sheet's slot list, its
// picker, a forced choice); with it on they can.
async function checkCompanionGear() {
  const { assert, count } = limitlessChecks.counter();
  await waitForSourceFiles();
  const $ = id => document.getElementById(id);
  const high = ForgeRules.encode(ForgeRules.generate({ type: "longsword", level: 150, source: "shop", random: () => 0.5 }));
  limitlessChecks.hero({ on: false, companions: [{ level: 100, inventory: [high] }] });
  const member = companions[0];
  Companions.open(member.id);
  let select = $("companion-sheet").querySelector('[data-comp-gear="primaryHand"]');
  assert(select && ![...select.options].some(option => option.value === high), "Off: the slot's list leaves the Level-150 sword out");
  select.add(new Option("forced", high)); select.value = high; select.dispatchEvent(new Event("change", { bubbles: true }));
  assert(member.character.equipment.primaryHand !== high, "Choosing it anyway does nothing");
  $("companion-sheet").querySelector('[data-comp-browse-slot="primaryHand"]').click();
  $("companion-picker-list").querySelector(`[data-comp-picker-select="${high}"]`)?.click();
  const detail = $("companion-picker-detail"), equip = detail.querySelector('[data-comp-picker-action="equip"]');
  assert(equip?.disabled && detail.querySelector(".companion-picker-actions p")?.textContent.includes("Level 150 required: it can only be worn while Limitless is on"), "The picker's equip button is disabled and says why");
  equip.disabled = false; equip.click();
  assert(member.character.equipment.primaryHand !== high, "Its equip button does nothing");
  CompanionSheetDetails.close();
  assert(Limitless.setActive(true), "Limitless on");
  Companions.open(member.id);
  select = $("companion-sheet").querySelector('[data-comp-gear="primaryHand"]');
  assert([...select.options].some(option => option.value === high), "On: the list offers it");
  select.value = high; select.dispatchEvent(new Event("change", { bubbles: true }));
  assert(member.character.equipment.primaryHand === high, "And the companion wears it");
  return count();
}

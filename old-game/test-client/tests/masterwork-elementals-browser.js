// The Masterwork Elementals (port task 1, 2026-11-05; old-game/Docs/Notes.md, "Masterwork Elementals").
// Each rule has its own entry, so each can be shown failing on its own; runMasterworkElementalChecks runs them all.
const elementalTools = () => {
  const assert = (ok, message) => { if (!ok) throw new Error(message); };
  const F = ForgeRules;
  let seed = 4321;
  const random = () => { seed = (seed * 16807) % 2147483647; return seed / 2147483647; };
  const forge = (key, level = 50) => F.generateMasterwork({ key, level, random });
  const forgeWith = (key, level, test) => { for (let i = 0; i < 400; i++) { const spec = forge(key, level); if (test(spec)) return spec; } throw new Error(`No ${key} matching the test`); };
  // The combat internals the checks drive (combat.js loaded again with them exposed, as other checks do).
  const hooks = async () => {
    if (window.__elementals) return window.__elementals;
    const source = await (await fetch("scripts/combat.js")).text(), script = document.createElement("script");
    script.textContent = source.replace("  window.Combat = {", "  window.__elementals = { actorById, strike, values, withActor, shiftTurnMeter, triggerDamageTakenEquipment, forecastSpellCost, getBattle: () => battle };\n  window.Combat = {");
    document.head.append(script); await waitForSourceFiles();
    return window.__elementals;
  };
  // A fight against tough foes that can't dodge; the wielder holds the given items.
  const fight = async (items, slots, foes = ["Goblin"]) => {
    const h = await hooks();
    Combat.restore(null);
    const c = defaultCharacter();
    Object.assign(c, { raceId: "human", level: 60, editingEnabled: false, inventory: items.map(item => item.id), equipment: { ...defaultEquipment, ...slots } });
    c.stats = { strength: 80, vitality: 120, mind: 60, dexterity: 80, intelligence: 80, wisdom: 40, speed: 40, luck: 20 };
    applyCharacter(normalizeCharacter(c)); currentHp = 1e6; currentMana = 1e5;
    assert(await Combat.startEncounter(foes, "Masterwork Elementals test"), "A fight starts");
    const enemies = h.getBattle().actors.filter(actor => actor.team === "enemy");
    for (const foe of enemies) {
      foe.character.currentHp = 1e7; foe.resourceMax = { hp: 1e7, mana: 1e5 }; foe.character.currentMana = 1e5;
      foe.effects.push({ key: "test:clumsy", name: "Clumsy", type: "defense-flat", target: "evasion", amount: -1e6, expiresTurn: 1e9 });
    }
    return { h, player: h.actorById("player"), enemies };
  };
  const withRandom = (value, run) => { const original = Math.random; Math.random = () => value; try { return run(); } finally { Math.random = original; } };
  const KEYS = ["plasma-blade", "tidepiercer", "first-comet", "peace-and-balance", "kugelblitz", "avalanche"];
  return { assert, F, forge, forgeWith, fight, withRandom, KEYS };
};

async function runMasterworkElementalItemChecks() {
  const { assert, F, forge, KEYS } = elementalTools();
  const expected = {
    "plasma-blade": ["Plasma Blade", "wand", "hellmade"], tidepiercer: ["Tidepiercer", "lance", "dwarven"], "first-comet": ["First Comet", "mace", "hearthforge"],
    "peace-and-balance": ["Peace and Balance", "quarterstaff", "elven"], kugelblitz: ["Kugelblitz", "shortbow", "church-of-talos"], avalanche: ["Avalanche", "brigandine", "roundtable"],
  };
  for (const key of KEYS) {
    const def = F.MASTERWORKS[key], [name, type, maker] = expected[key];
    assert(def && def.name === name && def.type === type && def.mainMaker === maker, `${name} is a ${type} whose main part is ${maker}'s`);
    assert(def.placeholderArt === true && def.story && def.story.length > 40, `${name}'s look is marked as a placeholder, and it has a story`);
    for (const level of [1, 50, 100]) {
      const spec = forge(key, level), id = F.encode(spec), item = F.item(id);
      assert(F.encode(F.decode(id)) === id && item.rarity === "masterwork" && item.name.endsWith(name) && item.forged.parts[0].maker === maker, `${name} at level ${level} forges, saves and loads`);
    }
  }
  // Tidepiercer's lance has a crossguard instead of a pommel; Peace and Balance is a two-part quarterstaff.
  assert(forge("tidepiercer").parts.map(part => part.shape).join() === "spearhead,crossguard,long-handle", "Tidepiercer has a crossguard instead of a pommel");
  const staff = forge("peace-and-balance");
  assert(staff.parts[2] === null && /-x-s\d/.test(F.encode(staff)), "Peace and Balance has two parts, as every quarterstaff");
  // The Plasma Blade casts as a fire wand and attacks as a longsword: Intelligence, then Dexterity, all Fire.
  const blade = F.item(F.encode(forge("plasma-blade", 70))), sword = F.WEAPONS.longsword, t = F.levelCurve(70);
  const lerp = (a, b) => a + (b - a) * t, round2 = value => Math.round(value * 100) / 100;
  assert(blade.forged.caster && blade.forged.parts[0].material === "ruby" && blade.forged.parts[0].tier === 3 && blade.effects.some(effect => effect.kind === "spell-power-percent"), "The Plasma Blade casts as a wand with a Greater Ruby");
  assert(blade.basePower === Math.max(1, Math.round(lerp(sword.power[0], sword.power[1]))) && blade.handed === 1 && blade.critBase === F.CRIT.longsword[0] && blade.critDamage === F.CRIT.longsword[1], "Its attacks have a longsword's power and crit");
  assert(blade.primary.key === "intelligence" && blade.primary.ratio === round2(lerp(sword.primary[1], sword.primary[2])) && blade.secondary.key === "dexterity" && blade.secondary.ratio === round2(lerp(sword.secondary[1], sword.secondary[2])), "It scales with Intelligence, then Dexterity, at a longsword's ratios");
  assert(blade.element === "fire" && !blade.elementShare && normalizeWeaponType(blade.type) === "melee", "Its attacks deal only Fire damage, in melee");
  assert(forgedRunesMarkup(blade).includes("cannot hold runes"), "It has no rune slots");
  const stats = forgedItemStats(blade);
  assert(stats.some(stat => stat.label === "Attacks as" && stat.value === "Longsword") && stats.some(stat => stat.label === "Damage" && stat.value === "Fire"), "Its details say it attacks as a longsword, dealing Fire");
}

async function runMasterworkElementalRuneChecks() {
  const { assert, F, forge, forgeWith } = elementalTools();
  const runes = { tidepiercer: ["humidity", "water"], "first-comet": ["basalt", "earth"], "peace-and-balance": ["breeze", "wind"], kugelblitz: ["charge", "lightning"] };
  for (const [key, [rune, element]] of Object.entries(runes)) {
    const spec = forgeWith(key, 90, candidate => candidate.slots > 0), id = F.encode(spec), item = F.item(id);
    assert(F.fixedRunes(spec).join() === rune && !spec.runes.includes(rune) && !id.includes(F.RUNES[rune].code), `${F.MASTERWORKS[key].name} always has the ${F.RUNES[rune].name}, part of the masterwork and not in its slots`);
    assert(item.elementShare?.element === element && item.elementShare.percent === 50, `${F.MASTERWORKS[key].name} deals half its damage as ${element}`);
    assert(F.withoutRune(id, rune) === null, "The fixed rune can't be removed");
    assert(F.runeConflict(spec, "cinder") === "A rune of this kind is already slotted", "No second damage-element rune");
    assert(F.runeConflict(spec, "tarot") === "A rune of this kind is already slotted" && F.runeConflict(spec, Object.keys(F.RUNES).find(id => F.RUNES[id].group !== "element")) === "", "Its rolled slots still take other runes");
  }
  const armor = forgeWith("avalanche", 90, candidate => candidate.slots > 0), plain = { ...armor, masterwork: undefined };
  assert(F.fixedRunes(armor).join() === "still" && !armor.runes.includes("still"), "Avalanche always has the Ice resistance rune");
  assert(F.armorTotals(armor).resist.ice - F.armorTotals(plain).resist.ice === F.ARMOR_RUNE_RESIST, "It adds the rune's Ice resistance");
  assert(F.runeConflict(armor, "still") === "This rune is already slotted" && F.runeConflict(armor, "cinder") === "", "Its slots take other element runes, not a second Ice one");
  const slotted = F.decode(F.withRune(F.encode(armor), "cinder"));
  assert(slotted && F.fixedRunes(slotted).join() === "still" && slotted.runes.join() === "cinder", "Slotting another rune keeps the fixed one");
  const markup = forgedRunesMarkup(F.item(F.encode(armor)));
  assert(markup.includes("Rune of Still · fixed") || /fixed/.test(markup), "The details list the fixed rune");
  assert(forge("plasma-blade").slots === 0 && !F.fixedRunes(forge("plasma-blade")).length, "The Plasma Blade has no runes");
}

async function runTidepiercerChecks() {
  const { assert, F, forge } = elementalTools();
  const KEYS = ["finalPct", "basePowerPct", "primaryPct", "secondaryPct", "hit", "crit", "pierce", "spellCost", "pricePct"];
  for (let i = 0; i < 40; i++) {
    const spec = forge("tidepiercer", 1 + i * 2), item = F.item(F.encode(spec));
    let parts = 0;
    spec.parts.forEach((part, index) => { parts += (F.MATERIALS[part.material].effect.pierce || 0) * (index === 0 ? 2 : 1) + (index > 0 ? F.MAKERS[part.maker].effect.pierce || 0 : 0); });
    for (const runeId of spec.runes) parts += F.RUNES[runeId].effect.pierce || 0;
    const matching = F.matchingBonusText(spec) ? F.MATCHING_BONUS[F.matchingMaker(spec)].pierce || 0 : 0;
    assert(item.armorPierce === Math.min(100, parts + matching + 50), `Tidepiercer's armor pierce is its parts' plus 50, up to 100% (${item.armorPierce} vs ${parts + matching})`);
  }
  assert(F.item(F.encode(forge("tidepiercer"))).effects.some(effect => effect.kind === "armor-pierce-bonus" && effect.amount === 50), "Its details name the +50% armor pierce");
}

async function runFirstCometChecks() {
  const { assert, F, forge, fight, withRandom } = elementalTools();
  const comet = F.item(F.encode(forge("first-comet", 60)));
  const { h, player, enemies: [goblin] } = await fight([comet], { primaryHand: comet.id });
  const stunned = () => goblin.effects.some(effect => effect.type === "stun" && effect.key === `equipment:${comet.id}:stun` && effect.expiresTurn === goblin.turns + 1);
  const hp = goblin.character.currentHp;
  withRandom(0.999, () => h.strike(player, goblin, comet));
  assert(goblin.character.currentHp < hp && !stunned(), "A hit whose 25% roll fails doesn't stun");
  withRandom(0.2, () => h.strike(player, goblin, comet));
  assert(stunned(), "A hit whose 25% roll succeeds stuns for 1 turn");
  Combat.restore(null);
}

async function runPeaceAndBalanceChecks() {
  const { assert, F, forge, fight } = elementalTools();
  const staff = F.item(F.encode(forge("peace-and-balance", 60))), other = F.item(F.encode(F.generate({ type: "quarterstaff", level: 60, source: "shop", random: () => 0.5 })));
  for (const [weapon, locked] of [[staff, true], [other, false]]) {
    const { h, player, enemies: [goblin] } = await fight([weapon], { primaryHand: weapon.id });
    const base = h.values(player).speed;
    player.effects.push({ key: "test:slow", name: "Slowed", type: "stat-percent", target: "speed", amount: -30, expiresTurn: 1e9 });
    const slowed = h.values(player).speed;
    player.effects.push({ key: "test:sap", name: "Sapped", type: "stat-permanent-percent", target: "speed", amount: -20, expiresTurn: 1e9 });
    const sapped = h.values(player).speed;
    player.effects.push({ key: "test:haste", name: "Hasted", type: "stat-percent", target: "speed", amount: 50, expiresTurn: 1e9 });
    const hasted = h.values(player).speed;
    player.counter = 500; h.shiftTurnMeter(player, -300);
    const counter = player.counter;
    goblin.counter = 500; h.shiftTurnMeter(goblin, -300);
    if (locked) {
      assert(slowed === base, `Peace and Balance: a Speed debuff doesn't lower its wielder's Speed (${slowed} vs ${base})`);
      assert(sapped === base, `Nor does a lasting Speed reduction (${sapped} vs ${base})`);
      assert(hasted > base, "Speed increases still work");
      assert(counter === 500, "Its wielder's turn meter isn't pushed back");
      player.counter = 500; h.shiftTurnMeter(player, 200);
      assert(player.counter === 700, "It can still be pushed forward");
    } else {
      assert(slowed < base && sapped < slowed && counter === 200, "Without it, Speed debuffs and turn-meter pushes work as before");
    }
    assert(goblin.counter === 200, "Other actors' turn meters are pushed back as before");
    Combat.restore(null);
  }
  // Out of combat, the wielder's own lowering effects don't count either.
  const sheet = held => {
    const c = defaultCharacter();
    Object.assign(c, { raceId: "human", level: 60, inventory: [staff.id], equipment: { ...defaultEquipment, primaryHand: held ? staff.id : null }, permanentBonuses: { ...defaultCharacter().permanentBonuses, speed: -40 } });
    applyCharacter(normalizeCharacter(c));
    return currentStats().speed;
  };
  const held = sheet(true), unheld = sheet(false);
  assert(held > unheld, `A negative Speed bonus on the sheet doesn't count while it is held (${held} vs ${unheld})`);
}

async function runKugelblitzChecks() {
  const { assert, F, forge, fight, withRandom } = elementalTools();
  const bow = F.item(F.encode(forge("kugelblitz", 60)));
  const { h, player, enemies } = await fight([bow], { primaryHand: bow.id }, ["Goblin", "Goblin", "Goblin"]);
  const [target, ...others] = enemies;
  const before = others.map(foe => foe.character.currentHp);
  withRandom(0.5, () => h.strike(player, target, bow));
  assert(others.every((foe, i) => foe.character.currentHp < before[i]), "Every other enemy takes the splash");
  const mark = foe => foe.effects.filter(effect => effect.key.startsWith(`equipment:${bow.id}:spell-cost-mark`));
  assert(enemies.every(foe => mark(foe).length === 1 && mark(foe)[0].type === "spell-cost-percent" && mark(foe)[0].amount === -50), "Every enemy it damaged is marked: spells cost 50% more");
  const spell = getSpell("spell-bolt-basic-damage");
  const base = h.withActor(target, () => calculateSpellManaCost(spell));
  assert(base > 0 && h.forecastSpellCost(target, spell) === Math.floor(base * 1.5), `A marked enemy's spell costs 50% more Mana (${h.forecastSpellCost(target, spell)} vs ${base})`);
  withRandom(0.5, () => h.strike(player, target, bow));
  assert(enemies.every(foe => mark(foe).length === 1 && mark(foe)[0].amount === -50), "A new hit refreshes the mark, it never stacks");
  Combat.restore(null);
}

async function runAvalancheChecks() {
  const { assert, F, forge, fight } = elementalTools();
  const armor = F.item(F.encode(forge("avalanche", 60)));
  const { h, player, enemies } = await fight([armor], { armor: armor.id }, ["Goblin", "Goblin"]);
  const slowed = foe => foe.effects.filter(effect => effect.key.startsWith(`equipment:${armor.id}:avalanche`));
  for (let i = 0; i < 6; i++) h.triggerDamageTakenEquipment(player, 5, enemies[0]);
  assert(player.avalancheCounters?.[armor.id] === 6 && enemies.every(foe => !slowed(foe).length), "Six hits taken give six counters and no slow");
  h.triggerDamageTakenEquipment(player, 5, enemies[0]);
  assert(player.avalancheCounters[armor.id] === 0, "At 7 the counters reset");
  assert(enemies.every(foe => slowed(foe).length === 1 && slowed(foe)[0].type === "stat-percent" && slowed(foe)[0].target === "speed" && slowed(foe)[0].amount === -30), "At 7 every enemy loses 30% Speed");
  const speed = h.values(enemies[0]).speed;
  for (let i = 0; i < 7; i++) h.triggerDamageTakenEquipment(player, 5, enemies[0]);
  assert(enemies.every(foe => slowed(foe).length === 1) && h.values(enemies[0]).speed === speed, "Seven more hits refresh the slow, never stack it");
  Combat.restore(null);
}

async function runMasterworkElementalDropChecks() {
  const { assert } = elementalTools();
  const expected = {
    "plasma-blade": ["Fire Elemental", "Fire Spirit", "Magma Elemental", "Vulcanic Ram"],
    tidepiercer: ["Water Elemental", "Water Spirit", "Greater Water Spirit", "Raging Water Spirit", "Oasis Guardian"],
    "first-comet": ["Earth Elemental", "Earth Spirit", "Greater Earth Spirit", "Steelguard Golem", "Pyramid Sentinel"],
    "peace-and-balance": ["Wind Elemental"], kugelblitz: ["Lightning Elemental", "Storm Elemental"], avalanche: ["Ice Elemental", "Frost Elemental"],
  };
  const names = await (await fetch("enemies/index.json")).json(), found = {};
  for (const file of names) {
    const enemy = await (await fetch("enemies/" + encodeURIComponent(file))).json();
    for (const drop of enemy.drops || []) {
      const key = Object.keys(expected).find(id => drop.itemId === "masterwork-" + id);
      if (!key) continue;
      assert(drop.chance === 5, `${enemy.name} drops it at 5%`);
      assert(enemy.raceId === "elemental" && !enemy.unique, `${enemy.name} is an elemental, not a boss`);
      (found[key] ||= []).push(enemy.name);
    }
  }
  for (const [key, list] of Object.entries(expected)) assert((found[key] || []).sort().join() === [...list].sort().join(), `${key} drops from exactly ${list.join(", ")} (found ${(found[key] || []).join(", ")})`);
}

async function runMasterworkElementalChecks() {
  for (const check of [runMasterworkElementalItemChecks, runMasterworkElementalRuneChecks, runTidepiercerChecks, runFirstCometChecks, runPeaceAndBalanceChecks, runKugelblitzChecks, runAvalancheChecks, runMasterworkElementalDropChecks]) await check();
}

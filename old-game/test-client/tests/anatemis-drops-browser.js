// Anatemis's treasures (rework 2026-10-16): Quasar Jet (the Starscale Blade reforged) and the
// Golden Apex are forged masterworks, dropped at 10% and 8%.
async function runAnatemisDropChecks() {
  const assert = (condition, message) => { if (!condition) throw new Error(message); };
  const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
  const forged = key => getItem(ForgeRules.encode(ForgeRules.generateMasterwork({ key, level: 100, random: () => 0.5 })));
  const jet = forged("quasar-jet"), apex = forged("golden-apex");
  assert(jet?.name.endsWith("Quasar Jet") && jet.rarity === "masterwork" && jet.handed === 1 && jet.shopAvailable === false && jet.forged.type === "longsword" && jet.forged.parts[0].maker === "roundtable" && jet.forged.parts[1].shape === "pommel", "Quasar Jet is a Roundtable longsword with a pommel instead of a crossguard");
  assert(jet.effects.some(effect => effect.kind === "always-hit") && jet.effects.some(effect => effect.kind === "critical-chain-damage-percent" && effect.amount === 30 && effect.maxAmount === 300), "Quasar Jet never misses, and its critical hits charge the next attack");
  assert(apex?.name.endsWith("Golden Apex") && apex.rarity === "masterwork" && apex.handed === 2 && apex.forged.parts[0].maker === "institute-of-magic" && apex.forged.parts[0].material === "diamond" && apex.effects.some(effect => effect.kind === "prism-basic-damage"), "The Golden Apex is an Institute staff with a Greater Diamond and a prism");
  const migrated = normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: 80, inventory: ["weapon-starscale-blade-meele", "weapon-golden-apex-spell-staff"] });
  assert(migrated.inventory.map(id => getItem(id)?.forged?.masterwork).join() === "quasar-jet,golden-apex", "An old Starscale Blade becomes Quasar Jet, an old Golden Apex the forged staff");

  const character = defaultCharacter();
  character.raceId = "human"; character.level = 100;
  applyCharacter(normalizeCharacter(character));
  assert(await Combat.startEncounter(["Anatemis, the White Star"], "Drop test", "great-hunt"), "Anatemis encounter starts");
  document.getElementById("boss-intro-skip").click();
  const state = Combat.serialize(), boss = state.actors.find(actor => actor.enemyDefinitionId === "anatemis-white-star-unique-level-100");
  const drop = id => boss.dropTable.find(entry => entry.itemId === id);
  assert(drop("masterwork-quasar-jet")?.chance === 10 && !drop("masterwork-quasar-jet").guaranteedUntilClaimed && drop("masterwork-golden-apex")?.chance === 8, "She drops Quasar Jet at 10% (no guarantee) and the Golden Apex at 8%");
  boss.character.currentHp = 0; state.current = null;
  const originalRandom = Math.random;
  Math.random = () => 0;
  try { Combat.restore(state); Combat.onSheetChange(); await wait(100); } finally { Math.random = originalRandom; }
  const loot = Combat.serialize().lootItems;
  assert(loot.some(id => id.startsWith("weapon-masterwork-quasar-jet-")) && loot.some(id => id.startsWith("weapon-masterwork-golden-apex-")), "Successful rolls drop both, forged at the finder's level");
  document.getElementById("combat-loot-discard").click();
  Combat.restore(null);

  // Quasar Jet in a fight: it never misses, and critical hits charge the next blow.
  const source = await (await fetch("scripts/combat.js")).text(), script = document.createElement("script");
  script.textContent = source.replace("  window.Combat = {", "  window.__drops = { actorById, strike, cast, getBattle: () => battle };\n  window.Combat = {");
  document.head.append(script); await waitForSourceFiles();
  const h = window.__drops;
  const wielder = defaultCharacter();
  Object.assign(wielder, { raceId: "human", level: 100, inventory: [jet.id, apex.id], equipment: { ...defaultEquipment, primaryHand: jet.id }, knownSpells: ["spell-bolt-basic-damage"] });
  wielder.stats = { strength: 120, vitality: 300, mind: 60, dexterity: 80, intelligence: 80, wisdom: 40, speed: 60, luck: 30 };
  applyCharacter(normalizeCharacter(wielder)); currentHp = 1e6; currentMana = 1e5;
  assert(await Combat.startEncounter(["Goblin"], "Quasar test"), "A fight starts");
  const goblin = h.getBattle().actors.find(actor => actor.team === "enemy");
  goblin.character.currentHp = 1e7; goblin.resourceMax = { hp: 1e7, mana: 100 };
  goblin.effects.push({ key: "test:evasion", name: "Untouchable", type: "defense-flat", target: "evasion", amount: 100000, expiresTurn: 1e9 });
  Math.random = () => 0;
  try { h.strike(h.actorById("player"), goblin, jet); h.strike(h.actorById("player"), goblin, jet); } finally { Math.random = originalRandom; }
  const log = h.getBattle().log.slice(-8);
  assert(!log.some(entry => String(entry).includes("misses")) && log.some(entry => String(entry).includes("releases its gathered light: +30%")), "Quasar Jet never misses, and a critical hit charges the next blow with +30%");
  assert(h.actorById("player").critChain === 2, "Two critical hits in a row build a two-step charge");
  Math.random = () => 0.999;
  try { h.strike(h.actorById("player"), goblin, jet); } finally { Math.random = originalRandom; }
  assert(h.actorById("player").critChain === 0 && h.getBattle().log.slice(-4).some(entry => String(entry).includes("+60%")), "The next hit spends the full charge (+60%), and a hit that is not critical resets it");
  Combat.restore(null);

  // The Golden Apex's prism: Basic damage splits into four quarters of different elements.
  equipment = { ...defaultEquipment, primaryHand: apex.id, offHand: apex.id };
  assert(await Combat.startEncounter(["Goblin"], "Prism test"), "The prism fight starts");
  const battle = h.getBattle(), foe = battle.actors.find(actor => actor.team === "enemy");
  foe.character.currentHp = 1e7; foe.resourceMax = { hp: 1e7, mana: 100 };
  battle.current = "player"; battle.ap = 5;
  assert(h.cast(h.actorById("player"), foe, getSpell("spell-bolt-basic-damage")), "A Basic spell is cast with the Golden Apex");
  const prismLine = battle.log.slice(-6).find(entry => String(entry).includes("splits the blow into light"));
  assert(prismLine && prismLine.split(",").length === 4 && / basic/.test(prismLine), "The Golden Apex splits Basic damage into four quarters, one of them still Basic: " + prismLine);
  Combat.restore(null);
}

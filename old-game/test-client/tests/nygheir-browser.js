async function runNygheirChecks() {
  let checks = 0;
  const assert = (condition, message) => { if (!condition) throw Error(message); checks++; };
  const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
  Combat.restore(null);
  // Deterministic test hooks exist only in this fresh, invisible renderer.
  const source = await (await fetch("scripts/combat.js")).text();
  const marker = "  window.Combat = {";
  assert(source.includes(marker), "Combat test entry point exists");
  const script = document.createElement("script");
  script.textContent = source.replace(marker, "  window.__nygheirChecks = { actorById, hp, values, setHp, enemyAction, endTurn, isControlledTurn, currentActor, getBattle: () => battle, stageLook, spellEffects, tearOldWound, aiCandidates, runAiCandidate };\n" + marker);
  document.head.append(script);
  await waitForSourceFiles();
  const hooks = window.__nygheirChecks;
  if (window.BattleStage) window.BattleStage.enabled = () => false;
  if (window.GameSettings) window.GameSettings.enemyDelay = () => 1e9;

  // His Great Hunt is still offered at the Foot of the Mountain.
  const scout = defaultCharacter();
  scout.raceId = "human"; scout.level = 24;
  applyCharacter(normalizeCharacter(scout));
  worldState.regionId = "stoneveil-foothills";
  WorldRules.setActiveRegion(worldState.regionId);
  worldState.position = { x: 1040, y: 550 };
  Guild.render();
  const originalRandom = Math.random;
  try {
    const offered = [];
    for (const value of [0, 0.999]) {
      Math.random = () => value;
      worldState.guild.boards = {};
      const hunts = Guild.boardFor().offers.filter(offer => offer.type === "great-hunt");
      assert(hunts.length === 1, "Only one Stoneveil Great Hunt appears at a time");
      offered.push(hunts[0].uniqueId);
      if (hunts[0].uniqueId === "nygheir-the-wounded") assert(hunts[0].zone === "Mountainfoot" && WorldRules.zoneAt(hunts[0].point, hunts[0].regionId) === "Mountainfoot", "Nygheir's marker is at the Foot of the Mountain");
    }
    assert(new Set(offered).size === 2 && offered.includes("nygheir-the-wounded") && offered.includes("zik-lun-armored-hulk"), "Both eligible Stoneveil hunts can be offered");
  } finally { Math.random = originalRandom; }

  // His sheet: a heavy, slow Raid Boss with old wounds, his own AI, intro and phase cutscene.
  const raw = await (await fetch("enemies/Nygheir the Wounded.json")).json();
  assert(raw.classLevels["raid-boss"] === 1 && raw.classLevels.guardian === 5 && !raw.classLevels.rogue && raw.permanentBonuses.speed === -25, "A Raid Boss, armored as a Guardian and slowed by age");
  assert(raw.aiProfile === "nygheir" && raw.introCinematic === "nygheir" && raw.knownSpells.join() === "stonecrust-earth-unique", "His own AI, intro and Stonecrust");
  assert(raw.oldWounds.count === 3 && raw.oldWounds.bleedPercent === 2 && raw.oldWounds.damageTakenPercent === 10 && raw.oldWounds.rageDamagePercent === 12 && raw.oldWounds.burstAtPhase === 1, "Three old wounds that bleed, weaken and enrage");
  const phase = raw.phaseTransition;
  assert(phase.thresholdHpPercent === 25 && phase.healMaxHpPercent === 40 && phase.unskippable === true && phase.cinematic === "nygheir-phase" && phase.knownSpells.includes("dragonbreath-erode-earth-damage"), "At 25% HP he heals 40% and becomes an earth caster with a dragonbreath, in a phase that cannot be skipped");
  const stonecrust = getSpell("stonecrust-earth-unique");
  assert(stonecrust?.element === "earth" && stonecrust.catalogueAvailable === false && hooks.spellEffects(stonecrust).some(effect => effect.type === "seal-wound"), "Stonecrust is his own earth spell that seals a wound");
  const artifact = getItem("artifact-second-wind-artifact");
  assert(artifact?.rarity === "masterwork" && artifact.shopAvailable === false && artifact.automaticDrop === false && artifact.effects[0].thresholdPercent === 25, "Second Wind loads as a unique, drop-only Masterwork artifact");
  assert(BossIntros.get("nygheir") && BossIntros.get("nygheir-phase")?.before, "Both of his cutscenes exist");

  // A sturdy hero, so the fight lasts long enough to watch.
  const F = ForgeRules;
  const sword = F.encode(F.generate({ type: "longsword", level: 26, source: "shop", random: () => 0.5 }));
  const plate = F.encode(F.generateArmor({ type: "full-plate", level: 26, source: "shop", random: () => 0.5 }));
  const hero = defaultCharacter();
  Object.assign(hero, { raceId: "human", level: 26, inventory: [sword, plate], equipment: { ...defaultEquipment, primaryHand: sword, armor: plate } });
  hero.stats = { strength: 90, vitality: 320, mind: 25, dexterity: 70, intelligence: 22, wisdom: 25, speed: 90, luck: 40 };
  applyCharacter(normalizeCharacter(hero));
  currentHp = 1e6;

  // The battle opens with him waking at the Mountainfoot.
  assert(await Combat.startEncounter(["Nygheir the Wounded"], "Nygheir test", "great-hunt"), "Nygheir's encounter starts");
  const intro = document.getElementById("boss-intro-cinematic");
  assert(!intro.hidden && intro.dataset.boss === "nygheir" && intro.querySelector("#boss-intro-title").textContent === "Nygheir the Wounded" && intro.querySelector(".bi-boss svg") && intro.querySelectorAll(".bi-ny-boulder").length > 5, "His battle opens with his intro, his own sprite and the mound he wakes under");
  document.getElementById("boss-intro-skip").click();
  assert(intro.hidden && !document.getElementById("nygheir-cinematic"), "The intro can be skipped, and the old phase cinematic is gone");
  document.getElementById("combat-sheet-button").click();
  assert(document.getElementById("combat-view").hidden, "The character sheet remains accessible when combat starts");
  document.getElementById("combat-view-toggle").click();
  assert(!document.getElementById("combat-view").hidden, "The combat view can be reopened immediately");

  const battle = hooks.getBattle(), boss = battle.actors.find(actor => actor.enemyDefinitionId === "nygheir-the-wounded-unique-level-23");
  const player = () => hooks.actorById("player");
  assert(boss?.unique && boss.character.raceId === "dragon" && boss.character.level === 23 && boss.element === "earth", "Nygheir is a unique level 23 Earth Dragon");
  assert(boss.maxHpPercent === 136 && boss.character.permanentDefenseBonuses.evasion === -80 && boss.character.knownSpells.join() === "stonecrust-earth-unique", "His wounded phase has his HP bonus, evasion penalty and only Stonecrust");
  assert(hooks.values(boss).speed < hooks.values(player()).speed, "Old and heavy, he is slower than a quick hero");
  // Port task 5 adds the Shadow Crystal (0.05%) to every enemy, task 7 Winged Hussar (4%, only while Limitless is on); his own drops are unchanged.
  const ownDrops = boss.dropTable.filter(drop => !["key-item-shadow-crystal", "masterwork-winged-hussar"].includes(drop.itemId));
  assert(ownDrops.length === 2 && ownDrops[0].itemId === "key-item-vial-of-dragonblood" && ownDrops[0].chance === 1 && ownDrops[1].itemId === artifact.id && ownDrops[1].chance === 12 && boss.dropTable.some(drop => drop.itemId === "masterwork-winged-hussar" && drop.chance === 4) && boss.dropTable.length === 4, "His drops are unchanged, and Winged Hussar (4%) joins them");
  assert(boss.experienceRewardMultiplier === 1.4 && Combat.musicTheme() === "nygheir-the-wounded" && Music.state().theme === "nygheir-the-wounded" && MusicThemes.all().some(theme => theme.id === "nygheir-the-wounded"), "He grants extra EXP and his composed lament plays");

  // His look: stone slabs, lodged weapons, and three scars that show what the fight does to them.
  const look = hooks.stageLook(boss), art = state => Sprites.forActor({ ...look, ...state }).svg;
  assert(look.phase === undefined && look.openWounds === undefined && art({}).length > 5000, "He starts with every old wound closed");
  assert(art({ openWounds: 2 }) !== art({}) && art({ openWounds: 2 }).includes("#b3261c"), "Open wounds bleed on his sprite");
  assert(art({ phase: 1 }).includes("#ff7a2e") && art({ phase: 1 }).includes("sp-glow"), "In his second phase every scar burns");

  // A critical hit tears an old wound: he bleeds and takes more damage, but rages.
  try {
    Math.random = () => 0.001;
    const attack = hooks.aiCandidates(player()).find(candidate => candidate.kind === "attack" && candidate.target === boss);
    assert(attack, "The hero can attack him");
    while (!hooks.isControlledTurn()) hooks.enemyAction();
    hooks.runAiCandidate(player(), hooks.aiCandidates(player()).find(candidate => candidate.kind === "attack" && candidate.target === boss));
  } finally { Math.random = originalRandom; }
  assert(boss.openWounds === 1 && battle.log.some(entry => String(entry).includes("tears open one of Nygheir the Wounded's old wounds (1/3)")), "A critical hit tears open an old wound");
  const woundEffect = type => boss.effects.filter(effect => effect.oldWound && effect.type === type).reduce((sum, effect) => sum + effect.amount, 0);
  assert(woundEffect("incoming-weapon-damage-percent") === 10 && woundEffect("outgoing-attack-damage-percent") === 12 && woundEffect("turn-start-max-hp-loss-percent") === 2 && !woundEffect("spell-power-percent"), "An open wound bleeds, weakens him and drives his claws, not his spells");
  hooks.tearOldWound(boss);
  assert(boss.openWounds === 2 && boss.effects.some(effect => effect.oldWound && effect.name === "Old Wounds (2/3)") && hooks.stageLook(boss).openWounds === 2, "Wounds add up and show on his sprite");

  // With two wounds open he crusts one over with Stonecrust, and never strikes more than twice a turn.
  const step = () => { if (hooks.isControlledTurn()) { currentHp = hooks.values(player()).maxHp; hooks.endTurn(); } else hooks.enemyAction(); };
  const until = (condition, limit = 400) => { for (let i = 0; i < limit && !condition() && Combat.active(); i++) step(); return condition(); };
  const sealed = () => (boss.ai?.history || []).some(entry => entry.key === "stonecrust-earth-unique");
  assert(until(sealed), "He casts Stonecrust on his open wounds");
  assert(boss.openWounds === 1 && battle.log.some(entry => String(entry).includes("Stonecrust: stone crusts over one of Nygheir the Wounded's wounds")), "Stonecrust seals a wound");
  const strikesByTurn = (from = 0) => {
    const struck = {};
    for (const entry of boss.ai?.history || []) {
      if (entry.turn < from) continue;
      const spell = entry.kind === "spell" ? getSpell(entry.key) : null;
      if (entry.kind === "attack" || spell && hooks.spellEffects(spell).some(effect => effect.type === "damage")) struck[entry.turn] = (struck[entry.turn] || 0) + 1;
    }
    return struck;
  };
  assert(until(() => boss.turns >= 4) && Object.values(strikesByTurn()).every(count => count <= 2), "He strikes at most twice in one of his turns");

  // At a quarter of his HP the phase starts at once, even mid-blow, and plays "Old wounds remember".
  const maximum = hooks.values(boss).maxHp, floor = Math.max(1, Math.floor(maximum * 0.25));
  hooks.setHp(boss, 0);
  assert(boss.phaseIndex === 1 && boss.name === "Nygheir the Scarred" && hooks.hp(boss) === Math.min(maximum, floor + Math.floor(maximum * 0.4)), "A killing blow above the threshold cannot skip his second phase, which restores 40% HP");
  assert(boss.woundsBurst && boss.openWounds === 3 && boss.effects.some(effect => effect.oldWound && effect.name === "Burning Scars (3/3)") && !boss.effects.some(effect => effect.oldWound && effect.type === "turn-start-max-hp-loss-percent"), "Every old wound bursts and burns, and no longer bleeds");
  assert(battle.log.some(entry => String(entry).includes("Old wounds remember: Nygheir the Scarred rises")), "The battle log announces his rising");
  assert(!intro.hidden && intro.dataset.boss === "nygheir-phase" && intro.querySelector("#boss-intro-title").textContent === "Nygheir the Scarred", "Phase two plays its own cutscene and title card");
  assert(intro.querySelector(".bi-boss-before svg") && intro.querySelector(".bi-boss:not(.bi-boss-before) svg") && intro.querySelectorAll(".bi-ny-memory").length === 3, "The cutscene shows his real sprite before and after, and three memories of the old battle");
  assert(intro.querySelector(".bi-boss-before svg").outerHTML.includes("#b3261c") && intro.querySelector(".bi-boss:not(.bi-boss-before) svg").outerHTML.includes("#ff7a2e"), "Before, his wounds bleed; after, they burn");
  assert(Combat.musicTheme() === "nygheir-the-scarred" && Music.state().theme === "nygheir-the-scarred", "His lament turns to fury");
  assert(hooks.stageLook(boss).phase === 1, "He burns in battle too");
  assert(boss.character.permanentBonuses.intelligence === 10 && boss.character.permanentDefenseBonuses.armor === 20 && boss.character.permanentDefenseBonuses.evasion === -80 && boss.character.knownSpells.includes("faultline-earth-damage"), "Phase two brings earth magic and armor, but no evasion");
  document.getElementById("boss-intro-skip").click();
  assert(intro.hidden, "The phase cutscene can be skipped");

  // Risen as an earth caster, he strikes once a turn and lets the ground settle between quakes.
  const phaseTurn = boss.turns + 1;
  assert(until(() => boss.turns >= phaseTurn + 5), "He fights on in his second phase");
  const late = strikesByTurn(phaseTurn);
  assert(Object.values(late).every(count => count <= 1), "In phase two he strikes once a turn");
  const quakeTurns = (boss.ai?.history || []).filter(entry => entry.turn >= phaseTurn && entry.kind === "spell" && hooks.spellEffects(getSpell(entry.key) || { effects: [] }).some(effect => effect.type === "damage" && effect.recipient === "all-enemies")).map(entry => entry.turn);
  assert(quakeTurns.every((turn, index) => index === 0 || turn - quakeTurns[index - 1] >= 2), "He never casts a party-wide quake two turns running");
  assert(!(boss.ai?.history || []).some(entry => entry.turn >= phaseTurn && entry.key === "stonecrust-earth-unique"), "Burst wounds cannot be sealed");

  // The phase never repeats.
  hooks.setHp(boss, Math.floor(maximum * 0.1));
  assert(boss.phaseIndex === 1 && intro.hidden && boss.character.permanentBonuses.intelligence === 10, "The transformation does not repeat on later low-HP turns");
  Combat.restore(null);

  // Second Wind still heals its wearer at the start of a low-HP turn.
  const wearer = defaultCharacter();
  wearer.raceId = "human";
  wearer.inventory = [artifact.id];
  wearer.equipment.artifact1 = artifact.id;
  applyCharacter(normalizeCharacter(wearer));
  assert(await Combat.startEncounter(["Goblin"], "Second Wind test"), "Artifact test encounter starts");
  let state = Combat.serialize();
  const wearerMaximum = state.actors.find(actor => actor.id === "player").resourceMax.hp;
  const wearerLow = Math.max(1, Math.floor(wearerMaximum * .2));
  currentHp = wearerLow;
  state.current = null;
  state.actors.forEach(actor => { actor.counter = actor.id === "player" ? 1000 : 0; });
  Combat.restore(state);
  Combat.onSheetChange();
  await wait(60);
  // A plain Second Wind is a Level 80 copy since the Artifact Rework (port task 4, A11): 9%.
  assert(currentHp === Math.min(wearerMaximum, wearerLow + Math.max(1, Math.floor(wearerMaximum * .09))), `Second Wind heals at the start of a low-HP turn (actual ${currentHp}, start ${wearerLow}, max ${wearerMaximum})`);
  Combat.restore(null);
  return checks;
}

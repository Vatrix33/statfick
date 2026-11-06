async function runJuilaChecks() {
  let checks = 0;
  const assert = (condition, message) => { if (!condition) throw Error(message); checks++; };
  Combat.restore(null);
  // Deterministic test hooks exist only in this fresh, invisible renderer.
  const source = await (await fetch("scripts/combat.js")).text();
  const marker = "  window.Combat = {";
  assert(source.includes(marker), "Combat test entry point exists");
  const script = document.createElement("script");
  script.textContent = source.replace(marker, "  window.__juilaChecks = { actorById, hp, enemyAction, endTurn, isControlledTurn, currentActor, getBattle: () => battle, validateEnemy, seasonState, stageLook, spellEffects, resolveEncounterRoster };\n" + marker);
  document.head.append(script);
  await waitForSourceFiles();
  const hooks = window.__juilaChecks;
  if (window.BattleStage) window.BattleStage.enabled = () => false;
  if (window.GameSettings) window.GameSettings.enemyDelay = () => 1e9;

  // Her Great Hunt is still offered at Greenwatch, in the Deep Forest.
  const hunter = defaultCharacter();
  hunter.raceId = "human"; hunter.level = 22;
  applyCharacter(normalizeCharacter(hunter));
  worldState.regionId = "verdant-reach";
  WorldRules.setActiveRegion(worldState.regionId);
  const castle = WorldRules.settlementsForRegion("verdant-reach").find(place => place.id === "greenwatch");
  worldState.position = { x: castle.x, y: castle.y };
  worldState.guild.boards = {};
  Guild.render();
  const hunts = Guild.boardFor().offers.filter(offer => offer.type === "great-hunt");
  assert(hunts.length === 1 && hunts[0].uniqueId === "juila-grand-forest-matriach", "Verdant Reach offers Juila's Great Hunt");
  assert(hunts[0].zone === "Deep Forest" && WorldRules.zoneAt(hunts[0].point, "verdant-reach") === "Deep Forest", "Juila's marker is in the Deep Forest");

  // Her sheet: a Raid Boss with nature magic, her own AI and intro, and the cycle of seasons.
  const raw = await (await fetch("enemies/" + encodeURIComponent("Juila the Grand Forest Matriarch.json"))).json();
  assert(raw.level === 22 && raw.raceId === "dryad" && raw.classLevels["raid-boss"] === 1 && raw.classLevels.botanomancer === 9, "Juila is a Level 22 Dryad Botanomancer and Raid Boss");
  assert(raw.aiProfile === "juila" && raw.introCinematic === "juila" && !raw.spellPowerAura, "Her own AI and intro; the old spell-power aura is gone");
  assert(raw.name === "Juila the Grand Forest Matriarch" && raw.formerNames?.includes("Juila the Grand Forest Matriach"), "Her name is spelled Matriarch and keeps the old spelling as a former name");
  assert(raw.knownSpells.every(id => getSpell(id)?.element === "nature"), "Her spell book holds only nature spells");
  const wheel = getSpell("wheel-of-the-year-nature-unique");
  assert(raw.knownSpells.includes(wheel?.id) && !wheel.catalogueAvailable && !wheel.shopAvailable && wheel.requiredCasterEnemyId === raw.id && ["spring", "summer", "autumn", "winter"].every(id => wheel.seasonVariants?.[id]?.effects?.length), "She knows Wheel of the Year, her own spell with a gift for each season");
  assert(raw.equipment.primaryHand === "forge-weapon-2-staff~rare@nature", "She carries a generic forged Nature staff");
  assert(JSON.stringify(raw.drops) === JSON.stringify([{ itemId: "masterwork-rootbark", chance: 8 }]) && !raw.equipment.armor, "She drops the Rootbark masterwork 8% of the time");
  const cycle = raw.seasonCycle;
  assert(cycle?.turns === 2 && cycle.seasons.map(season => season.id).join() === "spring,summer,autumn,winter", "Four seasons turn every two of her turns");
  assert(cycle.seasons.map(season => season.spirit).join() === "blossom-sprite-level-20,sunhorn-stag-level-20,harvest-wisp-level-20,rime-wolf-level-20", "Each season calls its own spirit");
  assert(cycle.seasons.every(season => (season.foes || []).every(effect => effect.amount < 0 || effect.type === "turn-start-max-hp-loss-percent") && (season.allies || []).every(effect => effect.amount > 0 || effect.hp > 0 || effect.type.startsWith("incoming-"))), "Seasons only strengthen her side or hinder the heroes");
  let rejected = false;
  try { hooks.validateEnemy({ ...raw, seasonCycle: { turns: 0, seasons: [] } }); } catch (error) { rejected = error.message.includes("season cycle"); }
  assert(rejected, "Invalid season cycles are rejected");

  // Branch of the Forest is retired: old copies become forged epic Nature staffs.
  assert(getItem("weapon-branch-of-the-forest-spell-staff")?.rarity !== "masterwork", "Branch of the Forest is no longer a masterwork");
  const save = { inventory: ["weapon-branch-of-the-forest-spell-staff"], equipment: { primaryHand: "weapon-branch-of-the-forest-spell-staff", offHand: "weapon-branch-of-the-forest-spell-staff" }, favorites: [] };
  ForgeRules.migrateLegacyItems(save, 22, "juila-check");
  const migrated = getItem(save.inventory[0]);
  assert(/^weapon-forged-staff-/.test(save.inventory[0]) && save.equipment.primaryHand === save.inventory[0] && migrated.rarity === "epic" && migrated.effects.some(effect => effect.element === "nature"), "Old Branch of the Forest copies become an epic forged Nature staff");
  // Rootbark: the Great Forest Plate reworked into masterwork Scale Armor with an Elven main part.
  const rootbarkId = ForgeRules.encode(ForgeRules.generateMasterwork({ key: "rootbark", level: 25, random: () => 0.4 }));
  const rootbark = getItem(rootbarkId);
  assert(rootbark?.rarity === "masterwork" && rootbark.forged.type === "scale-armor" && rootbark.forged.parts[0].maker === "elven" && /Rootbark/.test(rootbark.name), "Rootbark is masterwork Scale Armor forged by Elven hands");
  assert(rootbark.resistances.nature >= 15 && rootbark.effects.some(effect => effect.kind === "turning-seasons" && effect.seasons.length === 4), "Rootbark resists Nature and turns with the seasons");
  const oldSave = { inventory: ["armor-great-forest-plate-light"], equipment: { armor: "armor-great-forest-plate-light" }, favorites: [] };
  ForgeRules.migrateLegacyItems(oldSave, 25, "rootbark-check");
  assert(/^armor-masterwork-rootbark-/.test(oldSave.inventory[0]) && oldSave.equipment.armor === oldSave.inventory[0], "Old Great Forest Plates become Rootbark");
  assert(!items.some(item => item.id === "armor-great-forest-plate-light"), "The Great Forest Plate is gone from the catalogue");

  // Old saves name her with the old spelling; those encounters still find her.
  const oldRoster = await hooks.resolveEncounterRoster(["Juila the Grand Forest Matriach", "Grove Matriarch"]);
  assert(oldRoster && oldRoster[0].name === "Juila the Grand Forest Matriarch", "Encounters saved with her old name still resolve");
  const migratedWorld = WorldRules.normalize({ guild: { greatHuntRematches: [{ uniqueId: "juila-grand-forest-matriach", enemy: "Juila the Grand Forest Matriach", regionId: "verdant-reach", zone: "Deep Forest", point: { x: 400, y: 300 } }] } });
  assert(!JSON.stringify(migratedWorld).includes("Matriach\""), "Saved world state takes her corrected name");
  // The fight: her intro, then Juila and two Grove Matriarchs.
  const character = defaultCharacter();
  character.raceId = "human"; character.level = 25; character.stats.speed = 20; character.stats.vitality = 8000; character.currentHp = 1000000; character.currentMana = 10000;
  character.inventory = [rootbarkId]; character.equipment = { ...character.equipment, armor: rootbarkId };
  applyCharacter(normalizeCharacter(character));
  assert(await Combat.startEncounter(["Juila the Grand Forest Matriarch", "Grove Matriarch", "Grove Matriarch"], "Juila test", "great-hunt"), "Juila's encounter starts");
  const intro = document.getElementById("boss-intro-cinematic");
  assert(!intro.hidden && intro.dataset.boss === "juila" && intro.querySelector("#boss-intro-title").textContent === "Juila the Grand Forest Matriarch" && intro.querySelectorAll(".bi-imp svg").length === 2, "Her battle opens with her intro, title card and two Matriarchs");
  document.getElementById("boss-intro-skip").click();
  assert(intro.hidden, "The intro can be skipped");
  const battle = hooks.getBattle(), boss = battle.actors.find(actor => actor.team === "enemy" && !actor.summoned && actor.enemyDefinitionId === raw.id);
  assert(battle.actors.filter(actor => actor.enemyDefinitionId === "grove-matriarch-level-20").length === 2, "Two Grove Matriarchs stand with her");
  const staff = getItem(boss.character.equipment.primaryHand);
  assert(staff?.forged?.type === "staff" && staff.rarity === "rare" && staff.effects.some(effect => effect.element === "nature"), "Her staff is forged rare and made for nature spells");
  assert(Combat.musicTheme() === "juila-grand-forest-matriach" && MusicThemes.all().some(theme => theme.id === "juila-grand-forest-matriach"), "Her composed theme plays");

  const step = () => { if (hooks.isControlledTurn()) hooks.endTurn(); else hooks.enemyAction(); };
  const until = (condition, limit = 400) => { for (let i = 0; i < limit && !condition() && Combat.active(); i++) step(); return condition(); };
  assert(until(() => battle.season), "The seasons begin with the first turn");
  const spirits = () => battle.actors.filter(actor => actor.seasonSpirit && hooks.hp(actor) > 0).map(actor => actor.summonDefinitionId);
  const heroes = () => battle.actors.filter(actor => actor.team !== boss.team && hooks.hp(actor) > 0);
  assert(hooks.seasonState().season.id === "spring" && spirits().join() === "blossom-sprite-level-20", "Spring Bloom begins and calls a Blossom Sprite");
  assert(boss.effects.some(effect => effect.season && effect.type === "turn-start-restore-percent") && heroes().every(actor => !actor.effects.some(effect => effect.season)), "Spring strengthens only her side");
  assert(hooks.stageLook(boss).season === "spring" && Sprites.forActor(hooks.stageLook(boss)).svg.length > 1000, "She wears the season in battle");
  assert(until(() => battle.season.foretold === "summer"), "The next season is foretold a turn ahead");
  assert(battle.log.some(entry => String(entry).includes("High Summer will come")), "The omen appears in the battle log");
  assert(until(() => hooks.seasonState()?.season.id === "summer"), "High Summer follows Spring");
  assert(boss.turns - battle.season.startTurn === 0 && spirits().join() === "sunhorn-stag-level-20", "The Blossom Sprite fades and a Sunhorn Stag answers the summer");
  assert(boss.effects.some(effect => effect.season && effect.type === "spell-power-percent"), "Summer empowers her side's spells");
  assert(until(() => hooks.seasonState()?.season.id === "autumn"), "Autumn Withering follows");
  assert(heroes().every(actor => actor.effects.some(effect => effect.season && effect.type === "turn-start-max-hp-loss-percent")), "Autumn withers every hero");
  assert(until(() => hooks.seasonState()?.season.id === "winter"), "Deep Winter follows");
  assert(heroes().some(actor => actor.rootedAp > 0 || actor.effects.some(effect => effect.type === "rooted")) && spirits().join() === "rime-wolf-level-20", "Winter roots a hero and calls a Rime Wolf");
  assert(until(() => battle.log.some(entry => String(entry).includes("binding roots and loses")), 4000), "A rooted hero loses Action Points on their turn");
  // Wheel of the Year: once in each season, with that season's gift.
  const wheelLog = name => battle.log.filter(entry => String(entry).includes("turns the Wheel of the Year: " + name)).length;
  assert(wheelLog("Bloom") >= 1 && wheelLog("Blaze") === 1 && wheelLog("Harvest") === 1 && wheelLog("Hoarfrost") === 1, "She turns the Wheel of the Year once in every season, with its season's gift");
  assert(battle.log.some(entry => String(entry).includes("Wheel of the Year: Hoarfrost roots")), "Hoarfrost roots the heroes");
  // The hero's Rootbark turns with its own seasons, every two of the hero's turns.
  assert(until(() => battle.log.some(entry => String(entry).includes("Rootbark turns to Summer")), 4000), "Rootbark turns to Summer on its wearer");
  assert(until(() => battle.log.some(entry => String(entry).includes("Rootbark turns to Autumn")), 4000) && hooks.actorById("player").effects.some(effect => effect.wearerSeason && effect.type === "melee-retaliation-percent"), "In Autumn, Rootbark turns blows back");
  // A matriarch more than a striker: never two damage spells in one of her turns.
  const struck = {};
  for (const entry of boss.ai?.history || []) if (entry.kind === "spell" && hooks.spellEffects(getSpell(entry.key) || { effects: [] }).some(effect => effect.type === "damage")) struck[entry.turn] = (struck[entry.turn] || 0) + 1;
  assert(Object.values(struck).every(count => count <= 1), "Juila casts at most one damage spell a turn");

  // When she falls, the seasons fall still.
  boss.character.currentHp = 0;
  until(() => battle.season.ended, 20);
  assert(battle.season.ended && battle.actors.every(actor => !actor.effects.some(effect => effect.season)) && !spirits().length, "Her death ends the seasons and their spirits");
  assert(Combat.musicTheme() !== "juila-grand-forest-matriach", "The boss theme ends when Juila falls");
  Combat.restore(null);
  return checks;
}

async function runTalosEdgeChecks() {
  const $ = id => document.getElementById(id), wait = ms => new Promise(resolve => setTimeout(resolve, ms));
  const assert = (condition, message) => { if (!condition) throw Error(message); };
  const bossId = "tankina-disciple-of-talos-level-44", boarId = "thunder-boar-gorthok-level-40", boltId = "thunderbolt-of-talos-lightning-unique";
  WorldMap.setAmbientEnabled(false);
  function fresh() {
    Combat.restore(null); const c = defaultCharacter(); c.level = 100; c.raceId = "human";
    for (const key of Object.keys(c.stats)) c.stats[key] = 100;
    c.stats.speed = 1000; c.currentHp = 100000; c.currentMana = 100000; applyCharacter(normalizeCharacter(c));
  }
  fresh(); GameSettings.setCombatAnimations(false);
  assert(await Combat.startEncounter(["Tankina, Disciple of Talos", "Apostle of Talos", "Apostle of Talos"]), "Animation-off encounter starts"); $('combat-sheet-button').click();
  let state = Combat.serialize(), boss = state.actors.find(actor => actor.enemyDefinitionId === bossId);
  boss.character.currentHp = Math.floor(boss.resourceMax.hp * .5); state.current = null;
  state.actors.forEach(actor => { actor.counter = actor.id === boss.id ? 1000 : 0; });
  Combat.restore(state); Combat.onSheetChange(); await wait(35);
  state = Combat.serialize(); boss = state.actors.find(actor => actor.enemyDefinitionId === bossId);
  assert(boss.phaseIndex === 1 && boss.phaseSpellDamagePercent === 30 && state.actors.some(actor => actor.summonDefinitionId === boarId), "Disabling animation never skips gameplay changes");
  assert($('boss-special-cinematic').hidden && boss.phaseFlashUntil === 0, "Animations-off suppresses the Talos cinematic and combat flash");
  GameSettings.setCombatAnimations(true);

  async function controlledCaster(name) {
    fresh(); assert(await Combat.startEncounter([name, "Goblin"]), "Exclusive caster fixture starts"); $('combat-sheet-button').click();
    const result = Combat.serialize(), caster = result.actors.find(actor => actor.name === name);
    const target = result.actors.find(actor => actor.name === "Goblin");
    caster.team = "player"; caster.companion = true; caster.character.classLevels = {}; caster.effects = [];
    caster.character.knownSpells = [boltId]; caster.character.currentMana = 1000; caster.resourceMax.mana = 1000;
    caster.character.currentHp = caster.resourceMax.hp;
    companions.push({ id: caster.id, name: caster.name, character: caster.character });
    target.resourceMax = { hp: 100000, mana: 1000 }; target.character.currentHp = 100000; target.character.classLevels = {};
    target.character.equipment = defaultCharacter().equipment; target.baseArmor = 0; target.effects = []; target.resistances = {};
    result.current = caster.id; result.ap = 5; result.targetId = target.id; return result;
  }
  function cast(state) {
    Combat.restore(state); $('combat-view-toggle').click();
    const random = Math.random; Math.random = () => .5;
    try {
      $('combat-spell-button').click(); const choice = document.querySelector(`[data-combat-ability="${boltId}"]`);
      assert(choice && !choice.disabled, "Designated caster can select Thunderbolt of Talos"); choice.click();
    } finally { Math.random = random; }
    $('combat-sheet-button').click(); return Combat.serialize();
  }
  state = await controlledCaster("Thunder Boar Gorthok");
  let result = cast(state), caster = result.actors.find(actor => actor.enemyDefinitionId === boarId);
  assert(result.ap === 1 && caster.character.currentMana === 968 && result.actors.find(actor => actor.name === "Goblin").character.currentHp < 100000, "Gorthok can cast the shared exclusive spell for 4 AP and 32 Mana: " + JSON.stringify({ ap: result.ap, mana: caster.character.currentMana, hp: result.actors.find(actor => actor.name === "Goblin").character.currentHp, current: result.current, log: result.log.slice(-4) }));
  state = await controlledCaster("Tankina, Disciple of Talos");
  const plain = cast(state), normalDamage = 100000 - plain.actors.find(actor => actor.name === "Goblin").character.currentHp;
  const chosen = structuredClone(state); chosen.actors.find(actor => actor.enemyDefinitionId === bossId).phaseSpellDamagePercent = 30;
  result = cast(chosen); const chosenDamage = 100000 - result.actors.find(actor => actor.name === "Goblin").character.currentHp;
  assert(normalDamage > 0 && Math.abs(chosenDamage - Math.floor(normalDamage * 1.3)) <= 1, "Chosen phase increases actual spell damage by thirty percent, not spell scaling or mana cost");

  fresh(); const c = normalizeCharacter(backupPayload().character), site = WorldRules.getRegion("stormwake-hills").sites[0];
  c.worldState.regionId = "stormwake-hills"; c.worldState.position = { x: site.x, y: site.y }; c.worldState.defeatedSiteBosses = [bossId];
  c.classLevels = { warrior: 5, barbarian: 7 }; c.perkPoints = 2; c.permanentBonuses.strength = 17;
  c.classProgress.statChoiceBonuses = {}; c.currencies = ShopRules.walletFromValue(site.servicePriceSilver * 2);
  const greatsword = ForgeRules.encode(ForgeRules.generateMasterwork({ key: "claw-of-beowulf", level: 100, random: () => 0.5 })); c.inventory = [greatsword]; c.equipment.primaryHand = greatsword; c.equipment.offHand = greatsword;
  applyCharacter(c); WorldMap.render();
  const confirm = window.confirm, prompt = window.prompt; window.confirm = () => true; window.prompt = () => null;
  try {
    assert(!ChurchOfTalos.respec("classes") && classLevels.barbarian === 7 && ShopRules.walletValue(currencies) === site.servicePriceSilver * 2, "Legacy bonus-choice cancellation makes no changes and does not spend money");
    window.prompt = () => "Strength";
    assert(ChurchOfTalos.respec("classes") && perkPoints === 14 && permanentBonuses.strength === 12, "Legacy class choice is refunded without guessing its selected stat");
    assert(equipment.primaryHand === greatsword && equipment.offHand === greatsword, "Respec retains a valid two-handed loadout");
    assert([...document.querySelectorAll('.currency-input')].every(input => Number(input.value) === currencies[input.dataset.currency]), "Offering immediately updates the displayed wallet");
  } finally { window.confirm = confirm; window.prompt = prompt; }
  Combat.restore(null);
}

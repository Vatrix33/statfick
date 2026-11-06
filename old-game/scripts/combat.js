/* Uses the character sheet's calculation functions for every combat participant. */
(() => {
  const $ = id => document.getElementById(id);
  const copy = value => structuredClone(value);
  const rules = CombatRules;
  let battle = null, view = false, evaluating = null, evaluationBattle = null, playerContext = null;
  let calculationFrame = null, phaseHealingDepth = 0;
  let enemyDefinitions = [], timer = null, refreshing = false, pendingRefresh = false;
  const setup = $("combat-setup");
  const abilityPicker = $("combat-ability-picker");
  const lootModal = $("combat-loot-modal");
  const reactionModal = $("combat-reaction-modal");
  const equipmentActionDamage = new Map();
  let resolvingEquipmentReaction = false;
  const starCinematic = $("anatemis-cinematic");
  const specialCinematic = $("boss-special-cinematic");
  const bossIntro = $("boss-intro-cinematic");
  let phaseCinematicTimer = null, phaseCinematicActorId = null;
  let bossIntroTimer = null, bossIntroUntil = 0;
  let pickerKind = null, weatherSpellId = null, pendingApex = null;
  // Lament's spell waiting for its second target (port task 5, M19).
  let pendingLament = null;
  // A reflected spell (the Hexbane Coat) can't be reflected again.
  let reflectingSpell = false;
  // The animated battlefield replays what happened; it never changes the rules.
  // Forecasts run on copies or while evaluating and are never reported.
  let stageHit = null, stageQuiet = false;
  const onStage = actor => Boolean(battle && actor && !evaluating && battle.actors.includes(actor));
  function stage(type, detail = {}) { if (battle && !evaluating) window.BattleStage?.event(type, detail); }
  function stageEquipment(actor, item, extra = {}) { if (item?.rarity === "masterwork" && onStage(actor)) stage("equipment", { actorId: actor.id, itemId: item.id, ...extra }); }
  const actorLabel = actor => actor?.team === "enemy" ? "Enemy " + actor.id.replace(/^enemy-/, "") : actor?.name || "Participant";
  const actorById = (id, state = battle) => state?.actors.find(actor => actor.id === id);
  const player = () => actorById("player");
  const hp = actor => actor.id === "player" ? actor.predictedHp ?? currentHp : actor.character.currentHp;
  const mana = actor => actor.id === "player" ? actor.predictedMana ?? currentMana : actor.character.currentMana;
  const living = (state = battle) => (state?.actors || []).filter(actor => hp(actor) > 0);
  // Forecasts read the same combat snapshot thousands of times. Cache only
  // within a synchronous, read-only calculation; never across actual actions.
  function withCalculationFrame(callback) {
    const previous = calculationFrame;
    calculationFrame = new WeakMap();
    try { return callback(); }
    finally { calculationFrame = previous; }
  }
  function cacheCalculation(key, calculate, actor = evaluating || player(), state = evaluationBattle || battle) {
    if (!calculationFrame || !actor || typeof actor !== "object" || !state || typeof state !== "object") return calculate();
    let actors = calculationFrame.get(state);
    if (!actors) { actors = new WeakMap(); calculationFrame.set(state, actors); }
    let results = actors.get(actor);
    if (!results) { results = new Map(); actors.set(actor, results); }
    if (results.has(key)) return results.get(key);
    const result = calculate();
    results.set(key, result);
    return result;
  }
  function hasClassPassiveEffect(actor, effectType) {
    return cacheCalculation(`class-passive:${effectType}`, () => calculateHasClassPassiveEffect(actor, effectType), actor);
  }
  function calculateHasClassPassiveEffect(actor, effectType) {
    const levels = actor.id === "player" ? classLevels : actor.character.classLevels;
    return classCatalog.some((definition) => (definition.levels || [])
      .filter((rank) => rank.level <= toInteger(levels?.[definition.id], 0))
      .flatMap((rank) => rank.effects || [])
      .filter((effect) => effect.type === "passive")
      .flatMap((passive) => [...(passive.effects || []), ...(passive.effect ? [passive.effect] : [])])
      .some((effect) => effect.type === effectType));
  }
  function protectedByOwnedSummon(actor, state = battle) {
    return hasClassPassiveEffect(actor, "untargetable-while-own-summon-alive") && living(state).some((candidate) => candidate.summoned && candidate.summonerId === actor.id);
  }
  const hostiles = (actor, state = battle) => living(state).filter(other => other.team !== actor.team);
  function activeDuel(actor, state = battle) {
    const effect = actor?.effects?.find(entry => entry.type === "duel-lock" && entry.expiresTurn > actor.turns);
    const partner = effect ? actorById(effect.partnerId, state) : null;
    if (!partner || hp(partner) <= 0 || partner.team === actor.team) return null;
    const reciprocal = partner.effects?.some(entry => entry.type === "duel-lock" && entry.partnerId === actor.id && entry.expiresTurn > partner.turns);
    return reciprocal ? { effect, partner } : null;
  }
  const opponents = (actor, state = battle) => {
    const candidates = hostiles(actor, state).filter(other => !protectedByOwnedSummon(other, state));
    const royal = royalDuelPartner(actor, state);
    if (royal) return candidates.filter(other => other.id === royal.id);
    const duel = activeDuel(actor, state);
    if (duel) return candidates.filter(other => other.id === duel.partner.id);
    return candidates.filter(other => {
      const protectedDuel = activeDuel(other, state);
      return !protectedDuel || protectedDuel.partner.id === actor.id;
    });
  };
  const currentActor = () => actorById(battle?.current);
  const isActive = () => Boolean(battle?.active);
  const isPlayerTurn = () => isActive() && battle.current === "player" && currentHp > 0;
  const isControlledTurn = () => isActive() && currentActor()?.team === "player" && !currentActor()?.summoned && hp(currentActor()) > 0;
  function resourceMaximum(kind) {
    const state = evaluationBattle || battle;
    if (!state?.active) return null;
    const actor = evaluating || actorById("player", state);
    const maximum = actor?.resourceMax?.[kind];
    return Number.isFinite(maximum) ? maximum : null;
  }
  const currentEffects = () => {
    const state = evaluationBattle || battle;
    if (!state?.active) return [];
    return (evaluating || actorById("player", state))?.effects || [];
  };
  // Peace and Balance: an effect lowering its wielder's Speed doesn't count (the current actor's gear),
  // and nothing pushes its wielder's turn meter back.
  const lowersLockedSpeed = (key, amount) => key === "speed" && amount < 0 && wieldsTurnAndSpeedLock();
  function shiftTurnMeter(actor, amount) {
    if (amount < 0 && withActor(actor, wieldsTurnAndSpeedLock)) return;
    actor.counter = Math.min(999, Math.max(0, actor.counter + amount));
  }
  function statPercent(key) {
    return currentEffects().filter(effect => ((effect.type === "stat-percent" && effect.target === key) || effect.type === "all-stats-percent") && !lowersLockedSpeed(key, effect.amount)).reduce((sum, effect) => sum + effect.amount, 0);
  }
  function permanentStatPercent(key) {
    // (Peace and Balance's lock for these is applied where the sheet adds them up, in currentStatsUncached.)
    return currentEffects().filter(effect => effect.type === "stat-permanent-percent" && effect.target === key).reduce((sum, effect) => sum + effect.amount, 0);
  }
  function permanentDefensePercent(key) {
    return currentEffects().filter(effect => effect.type === "defense-permanent-percent" && effect.target === key).reduce((sum, effect) => sum + effect.amount, 0);
  }
  function statValue(key, base) {
    const percent = statPercent(key);
    if (!percent) return base;
    const adjusted = base * (1 + (percent / 100));
    return Math.max(0, percent > 0 ? Math.ceil(adjusted) : Math.floor(adjusted));
  }
  function defense(key, base) {
    const effects = currentEffects();
    const artifactBonus = artifactDefenseBonus(evaluating, key);
    const percent = effects.filter(effect => effect.type === "defense-percent" && effect.target === key).reduce((sum, effect) => sum + effect.amount, 0) + artifactBonus.percent;
    const flat = effects.filter(effect => effect.type === "defense-flat" && effect.target === key).reduce((sum, effect) => sum + effect.amount, 0) + artifactBonus.flat;
    const stance = effects.find(effect => effect.type === "stance");
    const stanceBonus = evaluating && battle?.active ? passiveEffectsOf(evaluating).filter(effect => effect.type === "stance-bonus-percent").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0) + withActor(evaluating, () => calculateClassEffects().stanceBonusPercent) + living().filter(ally => ally.team === evaluating.team).flatMap(passiveEffectsOf).filter(effect => effect.type === "party-stance-bonus-percent").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0) : 0;
    const stanceFactor = !stance ? 1 : (stance.name === "Guard" ? key === "armor" : key === "evasion") ? 1.5 + stanceBonus / 100 : 0.5;
    const adjusted = percent > 0 ? Math.ceil(base * (1 + percent / 100)) : Math.floor(base * (1 + percent / 100));
    return Math.max(0, Math.floor(adjusted * stanceFactor) + flat);
  }

  // Synchronous context only: no rendering or asynchronous work inside this callback.
  // The player's live state is restored even if a malformed enemy effect throws.
  function withActor(actor, callback, state = battle) {
    const savedTraining = { knownSpells, perkPoints, trapsUsed, pendingClassSpellChoice, completedClassChoices };
    const saved = { ...savedTraining, selectedRaceId, level, stats, permanentBonuses, permanentDefenseBonuses, equipment, inventory, currencies, classLevels, activeSkills, unarmedAttacks, summonSpellsUsed, summonSpellsUsedByElement, bloodSupplies, itemsSold, soloSurvivorVictories, multiKillCombats, classKills, raceKills, classProgress, defensiveActionsUsed, flawlessVictories, enemyCount };
    const previous = evaluating, previousBattle = evaluationBattle, previousPlayerContext = playerContext;
    if (!previous) playerContext = saved;
    try {
      evaluating = actor; evaluationBattle = state;
      enemyCount = state?.active ? hostiles(actor, state).length : 0;
      if (actor.id !== "player") {
        const c = actor.character;
        knownSpells = c.knownSpells || []; perkPoints = c.perkPoints || 0; trapsUsed = c.trapsUsed || 0;
        pendingClassSpellChoice = c.pendingClassSpellChoice || null; completedClassChoices = c.completedClassChoices || [];
        selectedRaceId = c.raceId; level = c.level; stats = c.stats;
        permanentBonuses = c.permanentBonuses; permanentDefenseBonuses = c.permanentDefenseBonuses;
        equipment = c.equipment; inventory = c.inventory; currencies = c.currencies; classLevels = c.classLevels; activeSkills = c.activeSkills; unarmedAttacks = c.unarmedAttacks; summonSpellsUsed = c.summonSpellsUsed; summonSpellsUsedByElement = c.summonSpellsUsedByElement || {}; bloodSupplies = c.bloodSupplies; itemsSold = c.itemsSold; soloSurvivorVictories = c.soloSurvivorVictories; multiKillCombats = c.multiKillCombats; classKills = c.classKills || {}; raceKills = c.raceKills || {}; classProgress = c.classProgress || { oneHandedAttacks: 0, summonCasts: {}, lootEnemiesCollected: 0 }; defensiveActionsUsed = c.defensiveActionsUsed || 0; flawlessVictories = c.flawlessVictories || 0;
      } else {
        if (previous && playerContext) {
          ({ knownSpells, perkPoints, trapsUsed, pendingClassSpellChoice, completedClassChoices } = playerContext);
          ({ selectedRaceId, level, stats, permanentBonuses, permanentDefenseBonuses, equipment, inventory, currencies, classLevels, unarmedAttacks, summonSpellsUsed, summonSpellsUsedByElement, bloodSupplies, itemsSold, soloSurvivorVictories, multiKillCombats, classKills, raceKills, classProgress, defensiveActionsUsed, flawlessVictories } = playerContext);
          activeSkills = playerContext.activeSkills;
        }
        if (actor.skillSnapshot) activeSkills = actor.skillSnapshot;
      }
      return callback();
    } finally {
      ({ knownSpells, perkPoints, trapsUsed, pendingClassSpellChoice, completedClassChoices } = savedTraining);
      ({ selectedRaceId, level, stats, permanentBonuses, permanentDefenseBonuses, equipment, inventory, currencies, classLevels, activeSkills, unarmedAttacks, summonSpellsUsed, summonSpellsUsedByElement, bloodSupplies, itemsSold, soloSurvivorVictories, multiKillCombats, classKills, raceKills, classProgress, defensiveActionsUsed, flawlessVictories, enemyCount } = saved);
      evaluating = previous; evaluationBattle = previousBattle;
      playerContext = previousPlayerContext;
    }
  }
  function racePassiveEffectsOf(actor, state = battle) {
    return withActor(actor, () => getSelectedRace()?.passive?.effects || [], state);
  }
  const raceHasEffect = (actor, type, state = battle) => racePassiveEffectsOf(actor, state).some(effect => effect.type === type);
  const healingDisabled = (actor, state = battle) => raceHasEffect(actor, "healing-disabled", state);
  const debuffImmune = (actor, state = battle) => raceHasEffect(actor, "debuff-immunity", state);
  function values(actor, state = battle) {
    // A single stat evaluation is read-only too: share class/party lookups
    // within it, but discard them before healing, expiry, or another action.
    if (!calculationFrame) return withCalculationFrame(() => values(actor, state));
    return cacheCalculation("actor-values", () => calculateValues(actor, state), actor, state);
  }
  function calculateValues(actor, state = battle) {
    const playerLevel = level;
    return withActor(actor, () => {
      const attributes = currentStats();
      const hostileEffects = hostiles(actor, state).flatMap(source => passivesOf(source, state).flatMap(passive => {
        const sourceLevel = source.id === "player" ? playerLevel : source.character.level;
        const targetLevel = actor.id === "player" ? playerLevel : actor.character.level;
        return sourceLevel > targetLevel ? passive.targetEffects || [] : [];
      }));
      for (const effect of hostileEffects) {
        if (effect.type === "stat-percent" && effect.target in attributes && !lowersLockedSpeed(effect.target, toNumber(effect.amount, 0))) {
          attributes[effect.target] = Math.max(0, Math.floor(attributes[effect.target] * (1 + toNumber(effect.amount, 0) / 100)));
        }
      }
      const partyStatEffects = living(state)
        .filter(ally => ally.team === actor.team)
        .flatMap(ally => [...passiveEffectsOf(ally, state), ...withActor(ally, () => calculateClassEffects().partyStatPercentModifiers, state)])
        .filter(effect => effect.type === "party-stat-percent");
      for (const effect of partyStatEffects) for (const target of effect.targets || [effect.target]) {
        if (!(target in attributes) || lowersLockedSpeed(target, toNumber(effect.amount, 0))) continue;
        const adjusted = attributes[target] * (1 + toNumber(effect.amount, 0) / 100);
        attributes[target] = Math.max(0, effect.amount > 0 ? Math.ceil(adjusted) : Math.floor(adjusted));
      }
      for (const ally of living(state).filter(ally => ally.team === actor.team && ally.id !== actor.id)) {
        for (const effect of passiveEffectsOf(ally, state).filter(effect => effect.type === "share-artifact-stats-percent")) {
          const relics = withActor(ally, () => calculateArtifactEffects(), state);
          for (const key of bonusDefinitions) attributes[key] += Math.floor((toNumber(relics.statFlat[key], 0) + attributes[key] * (toNumber(relics.statPercent[key], 0) + toNumber(relics.statPermanentPercent[key], 0)) / 100) * toNumber(effect.amount, 0) / 100);
        }
      }
      const auraPercent = living(state).reduce((sum, source) => {
        if (source.id === actor.id) return sum;
        const type = source.team === actor.team ? "allied-base-stats-percent" : "enemy-base-stats-percent";
        return sum + passiveEffectsOf(source, state).filter(effect => effect.type === type).reduce((total, effect) => total + toNumber(effect.amount, 0), 0);
      }, 0);
      if (auraPercent) for (const key of Object.keys(attributes)) {
        const adjusted = attributes[key] * (1 + auraPercent / 100);
        attributes[key] = Math.max(0, auraPercent > 0 ? Math.ceil(adjusted) : Math.floor(adjusted));
      }
      for (const effect of passiveEffectsOf(actor, state).filter((entry) => entry.type === "combat-all-stats-percent-per-turn")) {
        const turns = Math.min(Math.max(0, toInteger(actor.turns, 0)), Math.max(0, toInteger(effect.maximumTurns, 0)));
        const percent = turns * toNumber(effect.amount, 0);
        for (const key of Object.keys(attributes)) {
          const adjusted = attributes[key] * (1 + percent / 100);
          attributes[key] = Math.max(0, percent >= 0 ? Math.ceil(adjusted) : Math.floor(adjusted));
        }
      }
      const envy = racePassiveEffectsOf(actor, state).find(effect => effect.type === "conditional-stats-percent-vs-higher-max-hp");
      if (envy) {
        const ownMaximum = calculateMaxHpWithArtifacts(attributes);
        const hasLargerEnemy = hostiles(actor, state).some(other => withActor(other, () => calculateMaxHpWithArtifacts(currentStats()), state) > ownMaximum);
        if (hasLargerEnemy) for (const key of envy.targets || []) {
          if (key in attributes) attributes[key] = Math.max(0, Math.ceil(attributes[key] * (1 + toNumber(envy.amount, 0) / 100)));
        }
      }
      const classes = calculateClassEffects(), race = calculateRaceEffects();
      const calculatedMaxHp = calculateMaxHpWithArtifacts(attributes);
      const enemyMaxHp = Number.isFinite(actor.resourceMax?.hp)
        ? calculatedMaxHp
        : Math.floor(calculatedMaxHp * (1 + Math.max(0, toNumber(actor.maxHpPercent, 0)) / 100) * (actor.sparkling ? SPARKLING.maxHpMultiplier : 1));
      const equipmentStats = calculateEquipmentDefense({ ...attributes, maxHp: enemyMaxHp });
      const bonuses = bonusValues(permanentDefenseBonuses, defenseBonusDefinitions);
      const innateArmor = (Number.isFinite(actor.baseArmor) ? actor.baseArmor : BASE_ARMOR) + naturalArmor(actor);
      const base = { armor: innateArmor + equipmentStats.armor, evasion: Math.max(0, Math.floor((Math.floor(calculateEvasion(attributes)) - equipmentStats.evasionDebuff + equipmentStats.evasionFlat) * (1 + equipmentStats.evasionPercent / 100))) };
      // Pilgrim's Blessing (whole party, after a long walk) and Dispatch Guard (own guild quest).
      const partyDefense = { flat: 0, percent: 0 };
      for (const ally of living(state).filter(ally => ally.team === actor.team)) for (const effect of passiveEffectsOf(ally, state)) {
        const journey = /^journey-party-defense-(flat|percent)$/.exec(effect.type), quest = /^quest-defense-(flat|percent)$/.exec(effect.type);
        if (journey && worldState.distanceTraveledKm - worldState.lastRestKm >= toNumber(effect.kilometers, 60)) partyDefense[journey[1]] += toNumber(effect.amount, 0);
        if (quest && ally.id === actor.id && ["travel", "escort", "deliver"].includes(worldState.guild.quest?.type) && !worldState.guild.quest.complete) partyDefense[quest[1]] += toNumber(effect.amount, 0);
      }
      const defenses = Object.fromEntries(defenseBonusDefinitions.map(key => {
        const hostile = type => hostileEffects.filter(effect => effect.type === type && effect.target === key).reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
        const calculated = defense(key, Math.floor(base[key] * (1 + (bonuses[key] + classes.defensePermanentPercent[key] + race.defensePermanentPercent[key] + permanentDefensePercent(key) + partyDefense.percent) / 100)) + classes.defenseFlat[key] + race.defenseFlat[key]);
        return [key, Math.max(0, Math.floor(calculated * (1 + hostile("defense-percent") / 100)) + hostile("defense-flat") + partyDefense.flat)];
      }));
      for (const key of defenseBonusDefinitions) defenses[key] = Math.min(defenses[key], equippedDefenseCap(key), classes.defenseCap[key], race.defenseCap[key]);
      const calculatedMaxMana = calculateMaxManaWithEffects(attributes);
      const enemyMaxMana = Number.isFinite(actor.resourceMax?.mana)
        ? calculatedMaxMana
        : Math.floor(calculatedMaxMana * (1 + Math.max(0, toNumber(actor.maxManaPercent, 0)) / 100));
      const result = { ...attributes, ...defenses, maxHp: enemyMaxHp, maxMana: enemyMaxMana };
      actor.maxHp = result.maxHp;
      actor.maxMana = result.maxMana;
      return result;
    }, state);
  }
  /* Enemies who wear neither armor nor a shield have hides, scales or bark instead.
     Calibrated like forged armor, so fights against them end about 1.2x faster than
     before the weapon rework: forged weapons outclass the old ones most around level 8,
     so young beasts get the thickest hides there; from level 18 on, the Accuracy
     contest does most of the work and natural armor settles at 20. */
  const NATURAL_ARMOR = 20, NATURAL_ARMOR_EARLY = 40, NATURAL_ARMOR_PEAK = 8, NATURAL_ARMOR_SPREAD = 10;
  function naturalArmorForLevel(level) {
    const early = Math.max(0, 1 - Math.abs(Math.max(1, level) - NATURAL_ARMOR_PEAK) / NATURAL_ARMOR_SPREAD);
    return Math.round(NATURAL_ARMOR + NATURAL_ARMOR_EARLY * early);
  }
  // Set once when the enemy appears, so gear lost or changed mid-battle does not grow a hide.
  function spawnNaturalArmor(character) {
    const worn = [character?.equipment?.armor, character?.equipment?.offHand].map(id => id && getItem(id)).some(item => item?.category === "armor");
    return worn || !character ? 0 : naturalArmorForLevel(toInteger(character.level, 1));
  }
  function naturalArmor(actor) {
    return actor?.team === "enemy" ? Math.max(0, toNumber(actor.naturalArmor, 0)) : 0;
  }
  function defenseBreakdown(actor, state = battle) {
    const current = values(actor, state);
    const baseAndModified = withActor(actor, () => {
      const attributes = currentStats();
      const equipmentStats = calculateEquipmentDefense({ ...attributes, maxHp: current.maxHp });
      const baseArmor = (Number.isFinite(actor.baseArmor) ? actor.baseArmor : BASE_ARMOR) + naturalArmor(actor);
      const baseEvasion = Math.floor(calculateEvasion(attributes));
      return {
        armor: { base: baseArmor, modified: baseArmor + equipmentStats.armor },
        evasion: {
          base: baseEvasion,
          modified: Math.max(0, Math.floor((baseEvasion - equipmentStats.evasionDebuff + equipmentStats.evasionFlat) * (1 + equipmentStats.evasionPercent / 100)))
        }
      };
    }, state);
    return {
      armor: { ...baseAndModified.armor, current: current.armor },
      evasion: { ...baseAndModified.evasion, current: current.evasion }
    };
  }
  function skillsOf(actor, state = battle) { return withActor(actor, () => calculateClassEffects().skills, state); }
  function passivesOf(actor, state = battle) { return withActor(actor, () => calculateClassEffects().passives, state); }
  function actionPointMaximum(actor, state = battle) { return Math.max(0, 5 + withActor(actor, () => Math.floor(calculateClassEffects().actionPointsFlat), state)); }
  function actionAllowed(actor, action, state = battle) {
    if (action === "end") return true;
    // An undead spellcaster bound to his art (a lich: "unlivingCaster") casts despite Unliving.
    if (action === "spell" && raceHasEffect(actor, "spellcasting-disabled", state) && !actor.unlivingCaster) return false;
    if (action === "skill" && actor.effects?.some(effect => effect.type === "skills-sealed" && effect.expiresTurn > actor.turns)) return false;
    if (action === "run" && actor.effects?.some(effect => effect.type === "flee-disabled" && effect.expiresTurn > actor.turns)) return false;
    const restrictions = withActor(actor, () => calculateClassEffects().actionRestrictions, state);
    return !restrictions.length || restrictions.every((allowed) => allowed.includes(action));
  }
  function skillStates(actor) { return actor.id === "player" ? (actor.skillSnapshot || activeSkills) : actor.character.activeSkills; }
  const actorClassLevel = (actor, id) => actor.id === "player" ? toInteger(classLevels[id], 0) : toInteger(actor.character.classLevels[id], 0);
  const actorRace = actor => slugify(actor.id === "player" ? selectedRaceId : actor.character.raceId);
  function kiMaximum(actor) {
    const percent = passiveEffectsOf(actor).filter(effect => effect.type === "ki-pool-percent-level").reduce((sum, effect) => sum + Math.max(0, toNumber(effect.amount, 0)), 0);
    if (!percent) return 0;
    const actorLevel = actor.id === "player" ? level : actor.character.level;
    return Math.max(1, Math.floor(actorLevel * percent / 100));
  }
  function kiOf(actor) {
    const maximum = kiMaximum(actor);
    return Math.max(0, Math.min(maximum, toInteger(actor.ki, maximum)));
  }
  const actorBloodSupplies = actor => actor.id === "player" ? bloodSupplies : actor.character.bloodSupplies;
  const actorCurrencies = actor => actor.id === "player" ? currencies : actor.character.currencies;
  function setActorCurrencies(actor, value) {
    if (actor.id === "player") {
      currencies = value;
      document.querySelectorAll(".currency-input").forEach(input => { input.value = currencies[input.dataset.currency]; });
    } else actor.character.currencies = value;
  }
  const actorSummonSpellsUsed = actor => actor.id === "player" ? summonSpellsUsed : actor.character.summonSpellsUsed;
  const actorSummonSpellsUsedByElement = actor => actor.id === "player" ? summonSpellsUsedByElement : (actor.character.summonSpellsUsedByElement ||= {});
  function setActorSummonSpellsUsed(actor, value) {
    if (actor.id === "player") summonSpellsUsed = Math.max(0, toInteger(value, 0));
    else actor.character.summonSpellsUsed = Math.max(0, toInteger(value, 0));
  }
  function incrementActorSummonSpellsUsed(actor, element, count = 1) {
    const amount = Math.max(0, toInteger(count, 0));
    setActorSummonSpellsUsed(actor, actorSummonSpellsUsed(actor) + amount);
    const usedByElement = actorSummonSpellsUsedByElement(actor);
    const key = String(element || "basic").toLowerCase();
    usedByElement[key] = Math.max(0, toInteger(usedByElement[key], 0)) + amount;
  }
  function setActorBloodSupplies(actor, value) {
    if (actor.id === "player") bloodSupplies = Math.max(0, toInteger(value, 0));
    else actor.character.bloodSupplies = Math.max(0, toInteger(value, 0));
  }
  function cooldownDay(state) { return Math.max(0, toInteger(String(state?.lastUsedDay || "").replace(/^world-day-/, ""), 0)); }
  function skillReady(actor, skill, phaseActivation = false) {
    if (!phaseActivation && (actor.phaseTransitions || []).some(phase => phase.activateSkill === classSkillKey(skill))) return false;
    const state = skillStates(actor)[classSkillKey(skill)];
    if (skill.onActivateEffects?.some(effect => effect.type === "attune-last-hit-element") && !actor.lastDamageElement) return false;
    if (skill.requiredLoadout === "fencer" && !fencerReady(actor)) return false;
    if (skill.onActivateEffects?.some(effect => effect.type === "stormburst") && !toInteger(actor.stormCharges, 0)) return false;
    if (skill.onActivateEffects?.some(effect => effect.type === "blood-transfusion") && hp(actor) <= 1) return false;
    if (skill.onActivateEffects?.some(effect => ["restore-summons-percent", "buff-summons-defense-flat", "buff-summons-defense-percent"].includes(effect.type)) && !living().some(ally => ally.team === actor.team && ally.summoned)) return false;
    if (state?.active) return false;
    if (skill.oncePerTurn && actor.skillTurnUses?.[classSkillKey(skill)] === actor.turns) return false;
    if (Math.max(0, toInteger(skill.kiCost, 0)) > kiOf(actor)) return false;
    if (skill.requiredSorceryPoints && Math.max(0, toInteger(actor.sorceryPoints, 0)) < Math.max(1, toInteger(skill.requiredSorceryPoints, 1))) return false;
    if (Math.max(0, toInteger(skill.moneyCostSilver, 0)) > ShopRules.walletValue(actorCurrencies(actor))) return false;
    if (skill.cooldown === "none") return true;
    if (skill.requiredResource === "blood-supplies" && actorBloodSupplies(actor) < Math.max(1, toInteger(skill.minimumResource, 1))) return false;
    if (skill.cooldown === "combat") return toInteger(state?.usesThisCombat, state?.usedThisCombat ? 1 : 0) < Math.max(1, toInteger(skill.usesPerCombat, 1));
    if (skill.cooldown === "item") {
      const owned = actor.id === "player" ? inventory : actor.character.inventory;
      return toInteger(state?.charges, 0) > 0 || owned.includes(skill.requiredItemId);
    }
    if (!state?.lastUsedDay) return true;
    return WorldRules.day(worldState.minutes) - cooldownDay(state) >= Math.max(1, toInteger(skill.cooldownDays, 1));
  }
  const unarmedWeapon = { id: "combat-unarmed", name: "Unarmed", type: "melee", handed: 1, element: "physical", unarmed: true };
  /* Beasts fight with natural claws or horns: enemies named so in their files, and anyone
     with the Beast class. A natural attack costs 2 AP like a weapon attack and hits like a
     forged weapon of the attacker's level (ForgeRules.naturalWeapon). */
  const naturalWeaponKind = actor => actor?.naturalWeapon || (actor && hasClass(actor, "beast") ? "beast-claws" : null);
  function naturalWeaponOf(actor) {
    const kind = naturalWeaponKind(actor);
    if (!kind || !window.ForgeRules) return null;
    const actorLevel = actor.id === "player" ? level : toInteger(actor.character?.level, 1);
    return cacheCalculation(`natural-weapon:${kind}:${actorLevel}`, () => window.ForgeRules.naturalWeapon(kind, actorLevel), actor);
  }
  function weaponSlotsOf(actor) {
    return cacheCalculation("weapon-slots", () => calculateWeaponSlots(actor), actor);
  }
  function calculateWeaponSlots(actor) {
    return withActor(actor, () => {
      const primary = getItem(equipment.primaryHand);
      const offHand = getItem(equipment.offHand);
      return {
        primary: primary?.category === "weapon" ? primary : null,
        offHand: offHand?.category === "weapon" ? offHand : null
      };
    });
  }
  function goldenApexEquipped(actor) {
    const slots = weaponSlotsOf(actor);
    return [slots.primary, slots.offHand].some(item => (item?.effects || []).some(effect => effect.kind === "golden-apex-choice"));
  }
  function attackLoadout(actor) {
    const slots = weaponSlotsOf(actor);
    const primary = slots.primary || slots.offHand || naturalWeaponOf(actor) || unarmedWeapon;
    const offHand = slots.primary && slots.offHand && slots.primary.handed === 1 && slots.offHand.handed === 1 ? slots.offHand : null;
    return { primary, offHand, cost: primary.unarmed ? 1 : 2 };
  }
  function casterAllowed(actor, spell) {
    const ids = [spell.requiredCasterEnemyId, ...(spell.requiredCasterEnemyIds || [])].filter(Boolean);
    return !ids.length || ids.some(id => [actor.enemyDefinitionId, actor.summonDefinitionId].includes(id));
  }
  function spellsOf(actor) { return cacheCalculation("known-spells", () => (actor.id === "player" ? knownSpells : actor.character.knownSpells).map(getSpell).filter(spell => spell && casterAllowed(actor, spell)), actor); }
  function forecastSpellAp(actor, spell) {
    return cacheCalculation(`spell-ap:${spell.id}`, () => withActor(actor, () => calculateSpellActionPoints(spell)), actor);
  }
  function forecastSpellCost(actor, spell) {
    return cacheCalculation(`spell-cost:${spell.id}`, () => {
      const successive = actor.lastSpellId === spell.id ? Math.max(0, toInteger(actor.spellCastStreak, 0)) : 0;
      const increase = Math.max(0, ...spellEffects(spell).map(effect => toNumber(effect.successiveCastManaPercent, 0)));
      return Math.max(0, Math.floor(withActor(actor, () => calculateSpellManaCost(spell)) * (1 - timedSpellPercent(actor, "spell-cost-percent", spell) / 100) * (1 + successive * increase / 100)));
    }, actor);
  }
  function forecastSpellPower(actor, spell) {
    return cacheCalculation(`spell-power:${spell.id}`, () => {
      const successive = actor.lastSpellId === spell.id ? Math.max(0, toInteger(actor.spellCastStreak, 0)) : 0;
      const increase = Math.max(0, ...spellEffects(spell).map(effect => toNumber(effect.successiveCastDamagePercent, 0)));
      const storm = Math.max(0, toInteger(actor.stormCharges, 0)) * passiveEffectsOf(actor).filter(effect => effect.type === "storm-alternation-charge").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
      const penalty = actor.effects.find(effect => effect.type === "next-spell-penalty");
      return Math.max(0, Math.floor(withActor(actor, () => calculateSpellValue(spell)) * (1 + (timedSpellPercent(actor, "spell-power-percent", spell) + storm) / 100) * (1 + successive * increase / 100) * focusedPowerMultiplier(actor) * (1 - Math.min(100, Math.max(0, toNumber(penalty?.amount, 0))) / 100)));
    }, actor);
  }
  function forecastSpellElements(actor, spell) {
    return cacheCalculation(`spell-elements:${spell.id}`, () => actor.nextSpellElement ? [actor.nextSpellElement] : withActor(actor, () => effectiveSpellElements(spell)), actor);
  }
  function consumablesOf(actor) {
    const owned = actor.id === "player" ? inventory : actor.character.inventory;
    return [...new Set(owned)].map(getItem).filter(item => item?.category === "consumable" && !item.mapAction && !item.inventoryAction);
  }
  function nextPhaseFor(actor) {
    const phases = actor?.phaseTransitions || (actor?.phaseTransition ? [actor.phaseTransition] : []);
    return phases[Math.max(0, toInteger(actor?.phaseIndex, 0))] || null;
  }
  function phaseDamageFloor(actor) {
    // Anatemis's phases, and any phase marked unskippable, cannot be skipped by a single
    // enormous hit or instant death: crossing the threshold starts the phase at once.
    if (!actor || !battle?.actors.includes(actor)) return 0;
    const phase = nextPhaseFor(actor), maximum = actor.resourceMax?.hp;
    if (actor.enemyDefinitionId !== "anatemis-white-star-unique-level-100" && !phase?.unskippable) return 0;
    return phase && Number.isFinite(maximum) ? Math.max(1, Math.floor(maximum * phase.thresholdHpPercent / 100)) : 0;
  }
  function setHp(actor, value) {
    const before = hp(actor), floor = value < before ? phaseDamageFloor(actor) : 0;
    const adjusted = floor ? Math.max(floor, value) : value;
    if (actor.id === "player") { if (actor.predictedHp != null) actor.predictedHp = adjusted; else currentHp = adjusted; }
    else actor.character.currentHp = adjusted;
    if (floor && before > floor && hp(actor) <= floor) startNextPhase(actor);
    // Inverted Mercy (a dream law): every heal on its subject also wounds them for part of it.
    if (!mercyDepth && adjusted > before && before > 0 && battle?.actors.includes(actor)) {
      const mercy = actor.effects.find(effect => effect.type === "inverted-mercy" && effect.expiresTurn > actor.turns);
      const wound = mercy ? Math.floor((hp(actor) - before) * toNumber(mercy.hp, 0) / 100) : 0;
      if (wound > 0) {
        mercyDepth += 1;
        try { setHp(actor, hp(actor) - wound); log(`${mercy.name} wounds ${actorLabel(actor)} for ${wound}.`); } finally { mercyDepth -= 1; }
      }
    }
    if (!stageQuiet && hp(actor) !== before && onStage(actor)) stage("hp", { actorId: actor.id, before, after: hp(actor), max: actor.resourceMax?.hp, critical: Boolean(stageHit?.critical), element: stageHit?.element || null });
  }
  /* Doctor's Orders (port task 3, the Shop Rework; user decisions R12, R13, R24, R25, R27): every heal its wearer
     causes (spells, skills, items, heal-over-time ticks they cast, team passives they own, their own lifesteal and
     regeneration) is 50% stronger, and every heal the wearer receives in battle 25% stronger. A heal the wearer gives
     themselves takes only the larger bonus (×1.5); one wearer healing another takes both (×1.875). "healer" is the
     actor who causes the heal, null when no one does, or HEAL_SETTLED when the caller already applied the bonuses. */
  const HEAL_SETTLED = Symbol("healing bonuses applied");
  let chainStrikeDepth = 0;
  function equippedHealingPercent(actor, kind) {
    if (!actor || actor.inert) return 0;
    return withActor(actor, () => equippedDefenseItems().flatMap(item => item.effects || []).filter(effect => effect.kind === kind).reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0));
  }
  function healingFactor(recipient, healer = null) {
    if (healer === HEAL_SETTLED || !recipient) return 1;
    const received = 1 + equippedHealingPercent(recipient, "healing-received-percent") / 100;
    const given = healer ? 1 + equippedHealingPercent(healer, "healing-given-percent") / 100 : 1;
    const robe = healer && healer.id === recipient.id ? Math.max(given, received) : given * received;
    // Triage Pouch and Mender's Thimble (port task 4, A17, A21): each artifact's bonus multiplies on top, self-heals too.
    return robe * artifactProduct(recipient, "healing-received-percent") * (healer ? artifactProduct(healer, "healing-given-percent") : 1);
  }
  // Enemy HP restoration is reduced by 80%, except during a boss phase transition.
  function healingAmount(actor, amount, healer = null) {
    const mercy = (actor?.effects || []).find(effect => effect.type === "inverted-mercy" && effect.expiresTurn > actor.turns);
    const wholeAmount = Math.max(0, Math.floor(amount * healingFactor(actor, healer) * (mercy ? Math.max(0, 1 + toNumber(mercy.amount, 0) / 100) : 1)));
    return actor.team === "enemy" && phaseHealingDepth === 0 ? Math.floor(wholeAmount / 5) : wholeAmount;
  }
  function healHp(actor, amount, state = battle, healer = null) {
    const current = hp(actor);
    const gain = Math.max(0, Math.min(values(actor, state).maxHp - current, healingAmount(actor, amount, healer)));
    setHp(actor, current + gain);
    return gain;
  }
  /* The Artifact Rework (port task 4, 2026-11-06; decisions A16–A32 in engine/Docs/Engine-Plan.md): the artifacts'
     battle rules. An actor's artifact effects come from the two artifact slots of whoever wears them (you or a
     companion); they are cached by the worn ids. */
  const artifactEffectCache = new Map();
  function artifactEffectsOf(actor, kind) {
    if (!actor || actor.inert || actor.id !== "player" && !actor.character) return [];
    const ids = withActor(actor, () => [equipment?.artifact1, equipment?.artifact2]).filter(Boolean);
    if (!ids.length) return [];
    const key = ids.join("|");
    if (!artifactEffectCache.has(key)) artifactEffectCache.set(key, ids.map(id => getItem(id)).filter(item => item?.category === "artifact")
      .flatMap(item => (item.effects || []).map(effect => ({ ...effect, itemName: item.name }))));
    return artifactEffectCache.get(key).filter(effect => effect.kind === kind);
  }
  const artifactAmount = (actor, kind) => artifactEffectsOf(actor, kind).reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
  const artifactProduct = (actor, kind) => artifactEffectsOf(actor, kind).reduce((product, effect) => product * (1 + toNumber(effect.amount, 0) / 100), 1);
  const isHero = actor => Boolean(actor) && !actor.summoned && (actor.id === "player" || Boolean(actor.companion));
  // The "+X% damage" artifacts (A20): everything the wearer deals. Banner of Kings lasts the whole battle (A32).
  function artifactDamagePercent(attacker, target) {
    if (!attacker || !target || attacker.team === target.team) return 0;
    let percent = toNumber(battle?.kingsBanner?.[attacker.team]?.damage, 0);
    for (const effect of artifactEffectsOf(attacker, "damage-percent-vs-race")) if ((effect.raceIds || []).includes(actorRace(target))) percent += toNumber(effect.amount, 0);
    for (const effect of artifactEffectsOf(attacker, "damage-percent-vs-low-hp")) if (hp(target) < values(target).maxHp * effect.thresholdPercent / 100) percent += toNumber(effect.amount, 0);
    const larger = artifactAmount(attacker, "damage-percent-vs-larger-max-hp");
    if (larger && values(target).maxHp > values(attacker).maxHp) percent += larger;
    for (const effect of artifactEffectsOf(attacker, "low-hp-fury")) if (hp(attacker) < values(attacker).maxHp * effect.thresholdPercent / 100) percent += toNumber(effect.amount, 0);
    return percent;
  }
  // First Light Pin (A26): the wearer's first attack or spell that deals damage; every hit of that action shares it.
  function firstLightPercent(attacker, attackKind) {
    if (!attacker || !battle || battle.current !== attacker.id || !["melee", "ranged", "spell"].includes(attackKind)) return 0;
    if (attacker.firstLightSpent && attacker.firstLightAction !== battle.actionSerial) return 0;
    return artifactAmount(attacker, "first-strike-damage-percent");
  }
  // Tide-turner Compass's Evasion (A20) and Banner of Kings' Armor, read where defense is added up (no values() here).
  function artifactDefenseBonus(actor, key) {
    if (!actor || !battle?.active || !battle.actors.includes(actor)) return { flat: 0, percent: 0 };
    let flat = 0, percent = 0;
    if (key === "armor") percent += toNumber(battle.kingsBanner?.[actor.team]?.armor, 0);
    if (key === "evasion") for (const effect of artifactEffectsOf(actor, "low-hp-fury")) {
      const maximum = toNumber(actor.resourceMax?.hp, 0);
      if (maximum > 0 && hp(actor) > 0 && hp(actor) < maximum * effect.thresholdPercent / 100) flat += toNumber(effect.evasion, 0);
    }
    return { flat, percent };
  }
  // A fall: Battle Hymn Chime and Soulharvest Sickle restore their wearers (A22); Phoenix Plume claims the first hero (A18, A29).
  function noteFall(target) {
    if (!battle?.actors.includes(target) || hp(target) > 0 || target.fallNoted) return;
    target.fallNoted = true;
    for (const wearer of living().filter(actor => actor.team !== target.team)) for (const effect of artifactEffectsOf(wearer, "kill-restore-percent")) {
      const maximum = values(wearer);
      const healed = healingDisabled(wearer) ? 0 : healHp(wearer, Math.max(1, Math.floor(maximum.maxHp * effect.amount / 100)), battle, wearer);
      const restored = Math.min(maximum.maxMana - mana(wearer), Math.floor(maximum.maxMana * toNumber(effect.mana, 0) / 100));
      if (restored > 0) setMana(wearer, mana(wearer) + restored);
      if (healed || restored > 0) log(`${effect.itemName}: ${actorLabel(wearer)} restores ${[healed ? healed + " HP" : "", restored > 0 ? restored + " Mana" : ""].filter(Boolean).join(" and ")}.`);
    }
    if (!isHero(target)) return;
    battle.phoenix ||= {};
    if (Object.values(battle.phoenix).some(entry => entry.fallenId === target.id && entry.pending)) return;
    for (const wearer of battle.actors.filter(actor => actor.team === target.team && isHero(actor))) {
      const plume = artifactEffectsOf(wearer, "party-revive")[0];
      if (!plume || battle.phoenix[wearer.id]) continue;
      battle.phoenix[wearer.id] = { fallenId: target.id, percent: toNumber(plume.amount, 30), pending: true, itemName: plume.itemName };
      log(`${plume.itemName} smoulders for ${actorLabel(target)}.`);
      break;
    }
  }
  const phoenixWaitsFor = (actor, state = battle) => state === battle && Boolean(battle?.phoenix?.[actor.id]?.pending) && battle.phoenix[actor.id].fallenId === actor.id && hp(actor) <= 0;
  function risePhoenix(wearerId) {
    const entry = battle?.phoenix?.[wearerId], fallen = entry?.pending ? actorById(entry.fallenId) : null;
    if (!entry?.pending) return;
    entry.pending = false;
    if (!fallen || hp(fallen) > 0) return;
    setHp(fallen, Math.max(1, Math.floor(values(fallen).maxHp * entry.percent / 100)));
    fallen.alive = true; fallen.fallNoted = false;
    log(`${entry.itemName}: ${actorLabel(fallen)} rises again at ${hp(fallen)} HP.`);
  }
  function artifactTurnStart(actor) {
    if (hp(actor) <= 0) return;
    const maximum = values(actor);
    const heal = artifactAmount(actor, "turn-start-heal-percent");
    if (heal && !healingDisabled(actor)) {
      const gain = healHp(actor, Math.max(1, Math.floor(maximum.maxHp * heal / 100)), battle, actor);
      if (gain) log(`${actorLabel(actor)} restores ${actor.team === "player" ? gain + " " : ""}HP with ${artifactEffectsOf(actor, "turn-start-heal-percent")[0].itemName}.`);
    }
    const manaPercent = artifactAmount(actor, "turn-start-mana-percent");
    const restored = manaPercent ? Math.min(maximum.maxMana - mana(actor), Math.max(1, Math.floor(maximum.maxMana * manaPercent / 100))) : 0;
    if (restored > 0) { setMana(actor, mana(actor) + restored); log(`${actorLabel(actor)} restores ${restored} Mana with ${artifactEffectsOf(actor, "turn-start-mana-percent")[0].itemName}.`); }
    for (const effect of artifactEffectsOf(actor, "turn-start-cleanse-chance")) {
      const debuffs = actor.effects.filter(isRemovableDebuff);
      if (!debuffs.length || !rules.roll(effect.amount)) continue;
      const removed = debuffs[Math.floor(Math.random() * debuffs.length)];
      actor.effects = actor.effects.filter(existing => existing !== removed);
      log(`${effect.itemName} washes ${removed.name || "a debuff"} from ${actorLabel(actor)}.`);
      break;
    }
    // Starfall Orb (A19): a Holy spell hit sized by maximum Mana, not by spell power.
    for (const effect of artifactEffectsOf(actor, "turn-start-star")) {
      const foes = living().filter(foe => foe.team !== actor.team && !foe.inert);
      if (!foes.length) break;
      const struck = foes[Math.floor(Math.random() * foes.length)];
      const damage = hurt(struck, Math.max(1, Math.floor(maximum.maxMana * effect.amount / 100)), effect.element || "holy", false, true, actor, "spell");
      log(`${effect.itemName}: a falling star deals ${damage} Holy damage to ${actorLabel(struck)}.`);
    }
  }
  function triggerArtifactBattleStart(actor) {
    if (hp(actor) <= 0) return;
    for (const effect of artifactEffectsOf(actor, "battle-start-weapon-power-percent")) {
      // Before anyone's first turn, so +1 makes it last the wearer's first N turns.
      applyTimedEffects(actor, `artifact:${actor.id}:warcry`, effect.itemName, [{ type: "outgoing-attack-damage-percent", amount: effect.amount, attackKinds: ["melee", "ranged"], durationTurns: effect.durationTurns + 1 }]);
      log(`${effect.itemName}: ${actorLabel(actor)}'s weapon power +${formatValue(effect.amount)}% for ${effect.durationTurns} turns.`);
    }
    const barrier = artifactAmount(actor, "battle-start-barrier-percent");
    if (barrier) {
      actor.barrierHp = Math.max(toNumber(actor.barrierHp, 0), Math.floor(values(actor).maxHp * barrier / 100));
      log(`${artifactEffectsOf(actor, "battle-start-barrier-percent")[0].itemName} wraps ${actorLabel(actor)} in a ${actor.barrierHp} HP barrier.`);
    }
    for (const effect of artifactEffectsOf(actor, "battle-start-party-stats-percent")) {
      for (const ally of living().filter(entry => entry.team === actor.team)) applyTimedEffects(ally, `artifact:${actor.id}:rally`, effect.itemName, [{ type: "all-stats-percent", amount: effect.amount, durationTurns: effect.durationTurns + 1 }]);
      log(`${effect.itemName}: ${actorLabel(actor)} and every ally gain +${formatValue(effect.amount)}% to all attributes for ${effect.durationTurns} turns.`);
    }
    for (const effect of artifactEffectsOf(actor, "party-damage-armor-percent")) {
      battle.kingsBanner ||= {};
      const banner = battle.kingsBanner[actor.team] ||= { damage: 0, armor: 0 };
      banner.damage += toNumber(effect.amount, 0); banner.armor += toNumber(effect.armor, 0);
      log(`${effect.itemName} rises over ${actorLabel(actor)}'s side: +${formatValue(effect.amount)}% damage and +${formatValue(effect.armor)}% Armor for the battle.`);
    }
  }
  function setMana(actor, value) {
    const before = onStage(actor) ? mana(actor) : null;
    if (actor.id === "player") { if (actor.predictedMana != null) actor.predictedMana = value; else currentMana = value; } else actor.character.currentMana = value;
    if (before != null && !stageQuiet && mana(actor) !== before) stage("mana", { actorId: actor.id, before, after: mana(actor), max: actor.resourceMax?.mana, visible: actor.team === "player" });
  }
  const lichManaBarrier = target => hasClassPassiveEffect(target, "lich-mana-barrier") && !phylacteryOf(target);
  function applyCombatDamage(target, rawAmount, attacker = null) {
    let amount = Math.max(0, Math.floor(rawAmount));
    const currentHealth = hp(target);
    if (!amount || currentHealth <= 0) return 0;
    // Any damage an enemy causes counts as a hit (A27); damage with no source (damage over time) too.
    const hostile = !attacker || attacker.team !== target.team;
    if (hostile && !target.firstHitGuarded && battle?.actors.includes(target)) {
      const guard = artifactEffectsOf(target, "first-hit-guard-percent")[0];
      if (guard) {
        target.firstHitGuarded = true;
        amount = Math.max(0, Math.floor(amount * (1 - Math.min(100, guard.amount) / 100)));
        log(`${guard.itemName} softens the first blow against ${actorLabel(target)}.`);
        if (!amount) return 0;
      }
    }
    if (target.barrierHp > 0) {
      const absorbed = Math.min(amount, target.barrierHp);
      target.barrierHp -= absorbed;
      amount -= absorbed;
      if (!amount) return 0;
    }
    if (hostile && amount >= currentHealth && !target.deathSaveUsed && !phaseDamageFloor(target) && battle?.actors.includes(target)) {
      const save = artifactEffectsOf(target, "death-save")[0];
      if (save) {
        target.deathSaveUsed = true;
        amount = Math.max(0, currentHealth - Math.max(1, toInteger(save.amount, 1)));
        log(`${save.itemName}: ${actorLabel(target)} clings on at ${currentHealth - amount} HP.`);
      }
    }
    if (!lichManaBarrier(target) || mana(target) <= 0) {
      const dealt = Math.min(Math.max(0, currentHealth - phaseDamageFloor(target)), amount);
      setHp(target, currentHealth - dealt);
      ringWardensBell(target);
      triggerDamageTakenEquipment(target, dealt, attacker);
      creditRoyalDuelDamage(target, attacker, dealt);
      creditReforgeDamage(target, attacker, dealt);
      creditStarDamage(target, attacker, dealt);
      return dealt;
    }
    const beforeBarrier = Math.min(amount, Math.max(0, currentHealth - Math.max(1, phaseDamageFloor(target))));
    const diverted = Math.min(mana(target), amount - beforeBarrier);
    const remaining = Math.min(phaseDamageFloor(target) ? 0 : 1, amount - beforeBarrier - diverted);
    setHp(target, currentHealth - beforeBarrier - remaining);
    ringWardensBell(target);
    setMana(target, mana(target) - diverted);
    if (diverted) log(`${actorLabel(target)}'s Immortal Body absorbs ${diverted} damage with Mana.`);
    creditStarDamage(target, attacker, beforeBarrier + remaining);
    triggerDamageTakenEquipment(target, beforeBarrier + diverted + remaining, attacker);
    return beforeBarrier + diverted + remaining;
  }
  // Warden's Bell (A18): the first time each hero other than the wearer drops below the threshold, a barrier.
  function ringWardensBell(target) {
    if (!isHero(target) || target.wardensBellRung || hp(target) <= 0 || !battle?.actors.includes(target)) return;
    const maximum = toNumber(target.resourceMax?.hp, 0) || values(target).maxHp;
    for (const wearer of living().filter(actor => actor.team === target.team && actor.id !== target.id && isHero(actor))) {
      const bell = artifactEffectsOf(wearer, "ally-low-hp-barrier").find(effect => hp(target) < maximum * effect.thresholdPercent / 100);
      if (!bell) continue;
      target.wardensBellRung = true;
      target.barrierHp = Math.max(toNumber(target.barrierHp, 0), Math.floor(maximum * bell.amount / 100));
      log(`${wearer === target ? "" : actorLabel(wearer) + "'s "}${bell.itemName} rings: ${actorLabel(target)} gains a ${target.barrierHp} HP barrier.`);
      return;
    }
  }
  function triggerDamageTakenEquipment(target, dealt, attacker = null) {
    if (dealt <= 0 || hp(target) <= 0) return;
    // A two-handed weapon fills both hands; it still reacts once per hit.
    const held = [...new Map(withActor(target, equippedWeaponItems).map(item => [item.id, item])).values()];
    for (const item of held) for (const effect of item.effects || []) {
      if (effect.kind !== "all-stats-on-damage") continue;
      // With "maxStacks" (the Royal Trident) each hit taken adds a stack; the bonus lasts while hits keep coming.
      const key = `equipment:${item.id}:damage-buff`, current = target.effects.find(entry => entry.key === `${key}:all-stats-percent:all:all`);
      const stacks = effect.maxStacks > 1 ? Math.min(effect.maxStacks, (current ? Math.round(current.amount / effect.amount) : 0) + 1) : 1;
      applyTimedEffects(target, key, effect.maxStacks > 1 ? `${item.name} (${stacks})` : item.name, [{ type: "all-stats-percent", amount: effect.amount * stacks, durationTurns: effect.durationTurns }]);
      stageEquipment(target, item);
    }
    for (const item of withActor(target, equippedDefenseItems)) for (const effect of item.effects || []) {
      if (effect.kind === "avalanche-counters") { addAvalancheCounter(target, item, effect); continue; }
      if (effect.kind !== "damage-charge") continue;
      const charges = target.equipmentCharges ||= {};
      charges[item.id] = Math.min(effect.maximum, Math.max(0, toInteger(charges[item.id], 0)) + 1);
      stageEquipment(target, item, { charges: charges[item.id], maximum: effect.maximum });
    }
    triggerLowHealthEquipment(target);
    if (!attacker || hp(attacker) <= 0 || attacker.team === target.team || resolvingEquipmentReaction || !isActive()) return;
    const item = withActor(target, () => equippedWeaponItems().find(entry => entry.effects?.some(effect => effect.kind === "mana-retaliation")));
    const effect = item?.effects.find(entry => entry.kind === "mana-retaliation");
    if (effect && mana(target) >= effect.manaCost && (battle.reactions ||= []).length < 32) {
      battle.reactions.push({ actorId: target.id, attackerId: attacker.id, itemId: item.id, damage: dealt });
    }
  }
  // Avalanche (a Masterwork Elemental): each damage taken adds a counter; at the maximum every enemy loses
  // Speed for a while (a new trigger refreshes it) and the counters reset. They start at 0 each battle.
  function addAvalancheCounter(target, item, effect) {
    const counters = target.avalancheCounters ||= {};
    const next = Math.max(0, toInteger(counters[item.id], 0)) + 1;
    counters[item.id] = next >= effect.maximum ? 0 : next;
    stageEquipment(target, item, { charges: counters[item.id], maximum: effect.maximum });
    if (next < effect.maximum) return;
    for (const foe of opponents(target).filter(foe => hp(foe) > 0)) {
      applyTimedEffects(foe, `equipment:${item.id}:avalanche`, item.name, [{ type: "stat-percent", target: "speed", amount: -effect.amount, durationTurns: effect.durationTurns }]);
    }
    log(`${item.name} comes down: every enemy loses ${effect.amount}% Speed for ${effect.durationTurns} turns.`);
  }
  function equipmentChargeBonuses(actor) {
    if (!actor?.equipmentCharges || !Object.values(actor.equipmentCharges).some(amount => amount > 0)) return [];
    return cacheCalculation("equipment-charge-bonuses", () => withActor(actor, () => equippedDefenseItems().flatMap(item => (item.effects || [])
      .filter(effect => effect.kind === "damage-charge")
      .map(effect => {
        const stacks = Math.min(effect.maximum, Math.max(0, toInteger(actor.equipmentCharges[item.id], 0)));
        return { item, stacks, maximum: effect.maximum, percent: stacks === effect.maximum ? effect.fullBonusPercent : stacks * effect.amount };
      }).filter(entry => entry.stacks > 0))), actor);
  }
  function equipmentChargeMultiplier(actor) {
    return 1 + equipmentChargeBonuses(actor).reduce((sum, charge) => sum + charge.percent, 0) / 100;
  }
  function withEquipmentCharge(actor, callback, damaging = true) {
    if (!damaging || equipmentActionDamage.has(actor.id)) return callback();
    const charges = equipmentChargeBonuses(actor);
    // Capture once per action: every strike/repeat/area target uses this bonus.
    // Charges gained during that action must remain for the following action.
    for (const charge of charges) {
      delete actor.equipmentCharges[charge.item.id];
      stageEquipment(actor, charge.item, { charges: 0, released: charge.stacks });
      log(`${actorLabel(actor)} releases ${charge.item.name}: +${charge.percent}% damage (${charge.stacks} charges).`);
    }
    equipmentActionDamage.set(actor.id, 1 + charges.reduce((sum, charge) => sum + charge.percent, 0) / 100);
    try { return callback(); } finally { equipmentActionDamage.delete(actor.id); }
  }
  function equipmentReaction(record) {
    if (!isActive() || !record) return null;
    const actor = actorById(record.actorId), attacker = actorById(record.attackerId), item = getItem(record.itemId);
    const effect = item?.effects?.find(entry => entry.kind === "mana-retaliation");
    if (!actor || !attacker || hp(actor) <= 0 || hp(attacker) <= 0 || actor.team === attacker.team || !effect || mana(actor) < effect.manaCost || !(record.damage > 0) || !withActor(actor, () => equippedWeaponItems().some(entry => entry.id === item.id))) return null;
    return { actor, attacker, item, effect, damage: record.damage, power: record.damage * effect.amount / 100 };
  }
  function hasPlayerEquipmentReaction() {
    return Boolean(battle?.reactions?.some(record => {
      const reaction = equipmentReaction(record);
      return reaction?.actor.team === "player" && !reaction.actor.summoned;
    }));
  }
  function performEquipmentReaction(reaction) {
    setMana(reaction.actor, mana(reaction.actor) - reaction.effect.manaCost);
    stageEquipment(reaction.actor, reaction.item, { targetId: reaction.attacker.id });
    resolvingEquipmentReaction = true;
    try {
      const amount = hurt(reaction.attacker, reaction.power, reaction.effect.element, false, true, reaction.actor, "reaction");
      log(`${actorLabel(reaction.actor)} spends ${reaction.effect.manaCost} Mana on ${reaction.item.name}, returning ${amount} ${spellElementLabel(reaction.effect.element)} damage to ${actorLabel(reaction.attacker)}.`);
    } finally { resolvingEquipmentReaction = false; }
  }
  function processEquipmentReactions() {
    if (!isActive()) return;
    while (battle.reactions?.length) {
      const reaction = equipmentReaction(battle.reactions[0]);
      if (reaction?.actor.team === "player" && !reaction.actor.summoned) break;
      battle.reactions.shift();
      if (!reaction) continue;
      const predicted = estimatedDamage(reaction.attacker, reaction.power, reaction.effect.element, false, true, reaction.actor, "reaction");
      const lethal = predicted >= hp(reaction.attacker);
      if (predicted > 0 && (lethal || predicted >= reaction.effect.manaCost * .1 && mana(reaction.actor) - reaction.effect.manaCost >= values(reaction.actor).maxMana * .1)) performEquipmentReaction(reaction);
    }
  }
  function renderEquipmentReaction() {
    const reaction = equipmentReaction(battle?.reactions?.[0]);
    const show = view && reaction?.actor.team === "player" && !reaction.actor.summoned;
    const wasHidden = reactionModal.hidden;
    reactionModal.hidden = !show;
    if (show) {
      $("combat-reaction-title").textContent = reaction.item.name + " · " + reaction.actor.name;
      $("combat-reaction-description").textContent = `${reaction.actor.name} took ${reaction.damage} damage. Return ${formatValue(reaction.power)} ${spellElementLabel(reaction.effect.element)} power to ${reaction.attacker.name}?`;
      $("combat-reaction-cost").textContent = `${reaction.effect.manaCost} Mana · ${mana(reaction.actor)} available · 0 AP`;
      $("combat-reaction-use").textContent = `Retaliate · ${reaction.effect.manaCost} Mana`;
      if (wasHidden) $("combat-reaction-use").focus({ preventScroll: true });
    }
    if (wasHidden !== reactionModal.hidden) updateModalState();
  }
  function chooseEquipmentReaction(use) {
    const reaction = equipmentReaction(battle?.reactions?.[0]);
    if (!reaction) { if (isActive()) refresh(); return; }
    if (reaction.actor.team !== "player" || reaction.actor.summoned || !view) return;
    battle.reactions.shift();
    reactionModal.hidden = true;
    if (use) performEquipmentReaction(reaction);
    else log(`${actorLabel(reaction.actor)} declines ${reaction.item.name}.`);
    updateModalState(); refresh();
  }
  function log(message) {
    battle.log.push(message);
    stage("log", { message });
    if (battle.log.length > 200) battle.log.shift();
  }
  function expire(actor, state = battle) {
    // A Smoke Trap's cover lifts when its owner's next turn begins; if the owner has fallen, at each ally's own next
    // turn (R28).
    const ownerGone = ownerId => { const owner = actorById(ownerId, state); return !owner || hp(owner) <= 0; };
    const smokeLifts = (holder, ownerId) => ownerId === actor.id || holder === actor && ownerGone(ownerId);
    for (const other of state?.actors || []) if (other.effects.some(effect => effect.smokeOwnerId)) other.effects = other.effects.filter(effect => !effect.smokeOwnerId || !smokeLifts(other, effect.smokeOwnerId));
    actor.previousTurnAttackTargetIds = [...new Set(actor.currentTurnAttackTargetIds || [])];
    actor.currentTurnAttackTargetIds = [];
    actor.turns += 1;
    if (hp(actor) > 0) {
      const lowHpHealing = withActor(actor, () => equippedArtifactItems().flatMap(item => item.effects
        .filter(effect => effect.kind === "low-hp-turn-start-heal-percent")
        .map(effect => ({ ...effect, itemName: item.name }))), state);
      if (lowHpHealing.length && !healingDisabled(actor, state)) {
        const maximum = values(actor, state).maxHp;
        for (const effect of lowHpHealing) {
          if (hp(actor) >= maximum * effect.thresholdPercent / 100) continue;
          const restored = healHp(actor, Math.max(1, Math.floor(maximum * effect.amount / 100)), state, actor);
          if (restored <= 0) continue;
          if (state === battle) log(`${actorLabel(actor)} recovers ${restored} HP with ${effect.itemName}.`);
        }
      }
    }
    if (state === battle && hp(actor) > 0) for (const effect of passiveEffectsOf(actor).filter(entry => entry.type === "ki-regen-turns")) {
      if (actor.turns > 1 && actor.turns % Math.max(1, toInteger(effect.turns, 2)) === 0 && kiOf(actor) < kiMaximum(actor)) {
        actor.ki = Math.min(kiMaximum(actor), kiOf(actor) + Math.max(1, toInteger(effect.amount, 1)));
        log(`${actorLabel(actor)} restores Ki (${actor.ki}/${kiMaximum(actor)}).`);
      }
    }
    if (state === battle) {
      if (hp(actor) > 0 && actor.turnStartManaPercent > 0) {
        const maximum = values(actor, state).maxMana;
        const restored = Math.min(maximum - mana(actor), Math.floor(maximum * actor.turnStartManaPercent / 100));
        if (restored > 0) { setMana(actor, mana(actor) + restored); log(`${actorLabel(actor)} restores ${restored} Mana.`); }
      }
      if (hp(actor) > 0) {
        const teamRestore = living(state).filter(owner => owner.team === actor.team)
          .flatMap(owner => passiveEffectsOf(owner, state).map(effect => ({ owner, effect })))
          .filter(({ effect }) => effect.type === "team-turn-start-restore-percent");
        if (teamRestore.length) {
          const maximum = values(actor, state);
          // Each owner's share carries that owner's healing bonuses (Doctor's Orders, R24).
          const hpPercent = teamRestore.reduce((sum, { owner, effect }) => sum + toNumber(effect.hp, 0) * healingFactor(actor, owner), 0);
          const manaPercent = teamRestore.reduce((sum, { effect }) => sum + toNumber(effect.mana, 0), 0);
          if (hpPercent && !healingDisabled(actor, state)) healHp(actor, Math.max(1, Math.floor(maximum.maxHp * hpPercent / 100)), state, HEAL_SETTLED);
          if (manaPercent) setMana(actor, Math.min(maximum.maxMana, mana(actor) + Math.max(1, Math.floor(maximum.maxMana * manaPercent / 100))));
        }
      }
      for (const effect of racePassiveEffectsOf(actor, state).filter(entry => entry.type === "turn-start-restore-percent")) {
        const blockedElement = String(effect.unlessDamagedByElement || "").toLowerCase();
        const lookback = Math.max(0, toInteger(effect.lookbackTurns, 0));
        const recentlyDamaged = blockedElement && (actor.damageHistory || []).some(entry => entry.element === blockedElement && actor.turns - entry.turn <= lookback);
        if (!recentlyDamaged && !healingDisabled(actor, state) && hp(actor) > 0 && effect.hp) {
          const maximum = values(actor, state).maxHp;
          const gain = healHp(actor, Math.max(1, Math.floor(maximum * toNumber(effect.hp, 0) / 100)), state, actor);
          const passiveName = withActor(actor, () => getSelectedRace()?.passive?.name, state) || "a racial passive";
          log(`${actorLabel(actor)} restores ${actor.team === "player" ? gain + " " : ""}HP with ${passiveName}.`);
        }
      }
      const classEffects = withActor(actor, () => calculateClassEffects(), state);
      for (const passive of classEffects.passives) {
        const effects = [...(passive.effects || []), ...(passive.effect ? [passive.effect] : [])];
        const passiveId = `${passive.classId}:${passive.id}`;
        const upgrades = classEffects.passiveUpgrades.filter((upgrade) => upgrade.passiveId === passiveId);
        for (const effect of effects) {
          if (effect.type === "turn-start-restore-percent" && hp(actor) > 0) {
            const maximum = values(actor);
            if (effect.hp && !healingDisabled(actor, state)) healHp(actor, Math.max(1, Math.floor(maximum.maxHp * toNumber(effect.hp, 0) / 100)), state, actor);
            if (effect.mana) setMana(actor, Math.min(maximum.maxMana, mana(actor) + Math.max(1, Math.floor(maximum.maxMana * toNumber(effect.mana, 0) / 100))));
          }
          if (effect.type === "blood-supply-turn-restore" && actorBloodSupplies(actor) > 0 && hp(actor) > 0) {
            const maximum = values(actor);
            const hpPercent = toNumber(effect.hp, 0) + upgrades.reduce((sum, upgrade) => sum + toNumber(upgrade.hp, 0), 0);
            const manaPercent = toNumber(effect.mana, 0) + upgrades.reduce((sum, upgrade) => sum + toNumber(upgrade.mana, 0), 0);
            setActorBloodSupplies(actor, actorBloodSupplies(actor) - 1);
            if (hpPercent && !healingDisabled(actor, state)) healHp(actor, Math.max(1, Math.floor(maximum.maxHp * hpPercent / 100)), state, actor);
            if (manaPercent) setMana(actor, Math.min(maximum.maxMana, mana(actor) + Math.max(1, Math.floor(maximum.maxMana * manaPercent / 100))));
          }
        }
      }
      for (const definition of classCatalog) for (const rank of definition.levels || []) {
        if (rank.level > actorClassLevel(actor, definition.id)) continue;
        for (const effect of rank.effects || []) {
          if (effect.type !== "turn-start-restore-percent" || hp(actor) <= 0) continue;
          const maximum = values(actor, state);
          if (effect.hp && !healingDisabled(actor, state)) healHp(actor, Math.max(1, Math.floor(maximum.maxHp * toNumber(effect.hp, 0) / 100)), state, actor);
          if (effect.mana) setMana(actor, Math.min(maximum.maxMana, mana(actor) + Math.max(1, Math.floor(maximum.maxMana * toNumber(effect.mana, 0) / 100))));
        }
      }
      for (const effect of actor.effects.filter(effect => effect.type === "turn-start-max-hp-loss-percent" && actor.turns >= (effect.startsTurn || 0))) {
        const loss = hp(actor) > 0 ? Math.max(1, Math.floor(values(actor).maxHp * effect.amount / 100)) : 0;
        applyCombatDamage(actor, loss);
        log(`${actorLabel(actor)} loses ${actor.team === "player" ? loss + " " : ""}HP to ${effect.name}.`);
        if (hp(actor) <= 0) noteFall(actor);
      }
      for (const effect of actor.effects.filter(effect => state === battle && effect.type === "class-poison" && effect.expiresTurn >= actor.turns && hp(actor) > 0)) {
        const source = actorById(effect.ownerId);
        const poison = Math.max(0, toNumber(effect.amount, 0)) + Math.floor(values(actor).maxHp * Math.max(0, toNumber(effect.percentMaxHp, 0)) / 100);
        const dealt = directDamage(actor, effect.percentMaxHp && poison === 0 ? 1 : poison, false, "nature", source);
        if (dealt) log(`${actorLabel(actor)} suffers ${dealt} poison damage.`);
      }
      for (const effect of actor.effects.filter(effect => effect.type === "turn-start-restore-percent")) {
        const maximum = values(actor);
        // A heal-over-time remembers who cast it (Doctor's Orders, R24).
        if (effect.hp && !healingDisabled(actor, state)) healHp(actor, Math.max(1, Math.floor(maximum.maxHp * toNumber(effect.hp, 0) / 100)), state, effect.healerId ? actorById(effect.healerId, state) : null);
        if (effect.mana) setMana(actor, Math.min(maximum.maxMana, mana(actor) + Math.max(1, Math.floor(maximum.maxMana * toNumber(effect.mana, 0) / 100))));
      }
      for (const effect of actor.effects.filter(effect => effect.expiresTurn <= actor.turns && effect.type === "delayed-hp-loss-percent")) {
        const loss = hp(actor) > 0 ? Math.max(1, Math.floor(hp(actor) * effect.amount / 100)) : 0;
        applyCombatDamage(actor, loss);
        log(`${actorLabel(actor)} loses ${actor.team === "player" ? loss + " " : ""}HP as ${effect.name} ends.`);
        if (hp(actor) <= 0) noteFall(actor);
      }
      // Artifacts at the start of the wearer's turn (port task 4); Phoenix Plume raises another fallen hero now (A29).
      if (battle?.phoenix?.[actor.id]?.pending && battle.phoenix[actor.id].fallenId !== actor.id && hp(actor) > 0) risePhoenix(actor.id);
      artifactTurnStart(actor);
    }
    actor.effects = actor.effects.filter(effect => effect.expiresTurn > actor.turns);
    const states = skillStates(actor);
    for (const [key, turn] of Object.entries(actor.skillExpiry)) {
      if (turn <= actor.turns) {
        const skill = skillsOf(actor, state).find((candidate) => classSkillKey(candidate) === key);
        for (const effect of skill?.onExpireEffects || []) {
          if (effect.type === "set-resources-flat") {
            if (effect.hp != null) {
              const nextHp = Math.max(0, toInteger(effect.hp, 0));
              if (nextHp > hp(actor)) {
                if (!healingDisabled(actor, state)) healHp(actor, nextHp - hp(actor), state, actor);
              } else setHp(actor, nextHp);
            }
            if (effect.mana != null) setMana(actor, Math.max(0, toInteger(effect.mana, 0)));
          } else if (effect.type === "stun" && !debuffImmune(actor, state) && !withActor(actor, () => calculateClassEffects().stunImmune, state)) {
            actor.effects = actor.effects.filter(existing => existing.type !== "stun");
            actor.effects.push({ key: `${key}:expire-stun`, name: skill?.name || "Skill backlash", type: "stun", amount: 1, expiresTurn: actor.turns + Math.max(1, toInteger(effect.durationTurns, 1)) });
          }
        }
        if (states[key]) states[key].active = false;
        delete actor.skillExpiry[key];
      }
    }
  }
  function startNextPhase(actor, state = battle) {
    const phase = nextPhaseFor(actor);
    if (!phase || hp(actor) <= 0) return false;
    const maximum = actor.resourceMax?.hp ?? values(actor, state).maxHp;
    if (hp(actor) > maximum * phase.thresholdHpPercent / 100) return false;
    actor.phaseIndex = Math.max(0, toInteger(actor.phaseIndex, 0)) + 1;
    actor.name = phase.name;
    if (state === battle && (oldWoundsOf(actor)?.burstAtPhase ?? Infinity) <= actor.phaseIndex) burstOldWounds(actor);
    if (state === battle) stage("phase", { actorId: actor.id });
    for (const [key, amount] of Object.entries(phase.statBonuses || {})) {
      actor.character.permanentBonuses[key] = toNumber(actor.character.permanentBonuses[key], 0) + amount;
    }
    for (const [key, amount] of Object.entries(phase.defenseBonuses || {})) {
      actor.character.permanentDefenseBonuses[key] = toNumber(actor.character.permanentDefenseBonuses[key], 0) + amount;
    }
    actor.character.knownSpells = [...new Set([...actor.character.knownSpells, ...(phase.knownSpells || [])])];
    actor.phaseSpellDamagePercent = toNumber(actor.phaseSpellDamagePercent, 0) + toNumber(phase.spellDamagePercent, 0);
    // Only immediate transition effects bypass the reduction. Turn-start
    // regeneration and later skill effects run after this scope has ended.
    phaseHealingDepth += 1;
    try {
      if (phase.healMaxHpPercent > 0) healHp(actor, Math.max(1, Math.floor(maximum * phase.healMaxHpPercent / 100)), state);
      if (state === battle) {
        if (phase.activateSkill && !useSkill(actor, phase.activateSkill, actor, null, true)) throw new Error("Boss phase skill could not activate: " + phase.activateSkill);
        for (const summon of phase.summons || []) summonFriendly(actor, { ...summon, type: "summon-friendly" }, actor.name);
        log(phase.announcement || `${actor.name} tears away its broken scales. The earth shudders as it rises anew!`);
        if (phase.cinematic) playPhaseIntro(actor, phase.cinematic);
        else if (["false-dreamer", "forge-god", "storm-talos"].includes(phase.animation)) playSpecialCinematic(actor, phase.animation);
        else if (actor.enemyDefinitionId === "anatemis-white-star-unique-level-100") playPhaseCinematic(actor);
        else actor.phaseFlashUntil = window.GameSettings?.state().combatAnimations === false ? 0 : Date.now() + 1100;
        // A boss whose theme follows its phase changes music now.
        window.Music?.sync();
      }
    } finally {
      phaseHealingDepth -= 1;
    }
    return true;
  }
  function stopPhaseCinematic(resume = true) {
    const wasPlaying = [starCinematic, specialCinematic].some(cinematic => cinematic && !cinematic.hidden);
    clearTimeout(phaseCinematicTimer); phaseCinematicTimer = null;
    for (const cinematic of [starCinematic, specialCinematic].filter(Boolean)) {
      cinematic.hidden = true;
      cinematic.classList.remove("is-playing");
    }
    document.body.classList.remove("anatemis-cinematic-active");
    const actor = actorById(phaseCinematicActorId);
    if (actor) actor.phaseFlashUntil = 0;
    phaseCinematicActorId = null;
    if (wasPlaying && resume) {
      try { render(); } finally { scheduleAI(); }
      (view ? $("combat-sheet-button") : $("combat-view-toggle"))?.focus({ preventScroll: true });
    }
  }
  // Anatemis's phases play her own star cinematic.
  function playPhaseCinematic(actor) {
    stopPhaseCinematic(false);
    if (window.GameSettings?.state().combatAnimations === false) { actor.phaseFlashUntil = 0; return; }
    const cinematic = starCinematic, skipButton = $("anatemis-cinematic-skip");
    if (!cinematic || !skipButton) { actor.phaseFlashUntil = Date.now() + 1100; return; }
    const zenith = actor.phaseIndex >= 2;
    cinematic.dataset.phase = zenith ? "zenith" : "gold";
    $("anatemis-cinematic-phase").textContent = zenith ? "PHASE III · THE ZENITH AWAKENS" : "PHASE II · THE STAR TURNS GOLD";
    $("anatemis-cinematic-title").textContent = actor.name;
    $("anatemis-cinematic-description").textContent = zenith
      ? "The last light becomes all light. Dragonkind reaches its zenith."
      : "The white star burns gold, and the mountain trembles beneath its wings.";
    const reducedMotion = window.matchMedia?.("(prefers-reduced-motion: reduce)").matches;
    const duration = reducedMotion ? 1500 : 4800;
    actor.phaseFlashUntil = Date.now() + duration;
    phaseCinematicActorId = actor.id;
    document.body.classList.add("anatemis-cinematic-active");
    cinematic.hidden = false;
    // Arm cleanup before any focus or layout work can fail.
    phaseCinematicTimer = setTimeout(() => stopPhaseCinematic(), duration);
    // Restart the sequence if this encounter follows another in the same page session.
    void cinematic.offsetWidth;
    cinematic.classList.add("is-playing");
    skipButton.focus({ preventScroll: true });
  }
  /* Great Hunt bosses may open their battle with a cutscene and title card (boss-intros.js).
     It plays once when the encounter starts, never when a saved battle resumes, and holds
     the AI until it ends or is skipped. */
  function stopBossIntro(resume = true) {
    const wasPlaying = bossIntro && !bossIntro.hidden;
    clearTimeout(bossIntroTimer); bossIntroTimer = null; bossIntroUntil = 0;
    if (bossIntro) { bossIntro.hidden = true; bossIntro.classList.remove("is-playing"); }
    if (wasPlaying && resume && isActive()) {
      try { render(); } finally { scheduleAI(); }
      (view ? $("combat-sheet-button") : $("combat-view-toggle"))?.focus({ preventScroll: true });
    }
  }
  // A cutscene's retinue: one enemy name or a list of them (Zik Lun's two different guards).
  function introRetinue(key) {
    const retinue = [window.BossIntros?.get(key)?.retinue].flat().filter(Boolean).map(name => enemyDefinitions.find(definition => definition.name === name)).filter(Boolean)
      .map(definition => ({ raceId: definition.character.raceId, equipment: { ...armEnemy(copy(definition.character)).character.equipment, natural: definition.naturalWeapon || null }, enemyName: definition.name }));
    return retinue.length > 1 ? retinue : retinue[0] || null;
  }
  function playBossIntro() {
    stopBossIntro(false);
    if (!bossIntro || !window.BossIntros || window.GameSettings?.state().combatAnimations === false) return false;
    const definitionOf = actor => enemyDefinitions.find(definition => definition.id === actor.enemyDefinitionId);
    const boss = battle.actors.find(actor => actor.team === "enemy" && !actor.summoned && definitionOf(actor)?.introCinematic);
    if (!boss) return false;
    const key = definitionOf(boss).introCinematic;
    const intro = window.BossIntros.render(bossIntro, key, { boss: stageLook(boss), imp: introRetinue(key) });
    if (!intro) return false;
    const duration = window.matchMedia?.("(prefers-reduced-motion: reduce)").matches ? 2400 : intro.duration;
    bossIntroUntil = Date.now() + duration;
    bossIntro.hidden = false;
    bossIntroTimer = setTimeout(() => stopBossIntro(), duration);
    void bossIntro.offsetWidth;
    bossIntro.classList.add("is-playing");
    $("boss-intro-skip")?.focus({ preventScroll: true });
    return true;
  }
  /* A boss phase may name its own cutscene ("cinematic", boss-intros.js). It plays through the
     boss-intro player, drawn around the boss's look from before the change (every old wound
     open) and after it, and holds the AI the same way. */
  function playPhaseIntro(actor, key) {
    stopBossIntro(false);
    actor.phaseFlashUntil = 0;
    if (!bossIntro || !window.BossIntros || window.GameSettings?.state().combatAnimations === false) return false;
    const look = stageLook(actor), before = { ...look };
    // The look from before the change: the previous form (Anatemis's Golden Star before her Zenith).
    if (look.phase > 1) before.phase = look.phase - 1; else delete before.phase;
    if (oldWoundsOf(actor)) before.openWounds = oldWoundsOf(actor).count;
    delete before.soulDimmed;
    const intro = window.BossIntros.render(bossIntro, key, { boss: look, before, imp: introRetinue(key) });
    if (!intro) return false;
    const duration = window.matchMedia?.("(prefers-reduced-motion: reduce)").matches ? 2400 : intro.duration;
    bossIntroUntil = Date.now() + duration;
    bossIntro.hidden = false;
    bossIntroTimer = setTimeout(() => stopBossIntro(), duration);
    void bossIntro.offsetWidth;
    bossIntro.classList.add("is-playing");
    $("boss-intro-skip")?.focus({ preventScroll: true });
    return true;
  }
  function playSpecialCinematic(actor, theme) {
    stopPhaseCinematic(false);
    if (window.GameSettings?.state().combatAnimations === false) { actor.phaseFlashUntil = 0; return; }
    if (!specialCinematic) { actor.phaseFlashUntil = Date.now() + 1100; return; }
    specialCinematic.dataset.theme = theme;
    $("boss-special-cinematic-location").textContent = theme === "storm-talos" ? "CHURCH OF TALOS · STORMWAKE HILLS" : "CINDER ISLE · GREAT HUNT";
    $("boss-special-cinematic-name").textContent = actor.name;
    $("boss-special-cinematic-title").textContent = theme === "storm-talos" ? "Chosen by the Storm" : theme === "false-dreamer" ? "Lucid Change" : "The Forge Awakens";
    $("boss-special-cinematic-description").textContent = theme === "storm-talos" ? "Blood anoints the altar. Lightning crowns its champion. Gorthok answers the thunder." : theme === "false-dreamer" ? "The world is his dream. The dream refuses to end." : "From molten iron and living stone, the Vulcanic Ram answers its maker.";
    const duration = window.matchMedia?.("(prefers-reduced-motion: reduce)").matches ? 1200 : 3400;
    actor.phaseFlashUntil = Date.now() + duration;
    phaseCinematicActorId = actor.id;
    specialCinematic.hidden = false;
    phaseCinematicTimer = setTimeout(() => stopPhaseCinematic(), duration);
    void specialCinematic.offsetWidth;
    specialCinematic.classList.add("is-playing");
    $("boss-special-cinematic-skip").focus({ preventScroll: true });
  }
  function advance(state = battle) {
    // A fallen Phoenix Plume wearer keeps their place in the turn order, to rise when their turn comes (A29).
    state.actors.forEach(actor => { actor.alive = hp(actor) > 0 || phoenixWaitsFor(actor, state); });
    for (let skipped = 0; skipped <= state.actors.length; skipped += 1) {
      // Reuse one snapshot while comparing speeds. End the frame before a
      // phase change or turn-start effects mutate this battle (or forecast).
      const next = withCalculationFrame(() => rules.nextTurn(state.actors, actor => values(actor, state).speed * (1 + passiveEffectsOf(actor, state).filter(effect => effect.type === "turn-meter-gain-percent").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0) / 100)));
      if (!next) { state.current = null; state.ap = 0; return null; }
      if (phoenixWaitsFor(next, state)) risePhoenix(next.id);
      startNextPhase(next, state);
      const stunnedBeforeExpiry = next.effects.some(effect => effect.type === "stun" && effect.expiresTurn > next.turns);
      expire(next, state);
      const stunned = stunnedBeforeExpiry || next.effects.some(effect => effect.type === "stun" && effect.expiresTurn > next.turns);
      if (hp(next) <= 0) { next.alive = false; continue; }
      if (next.summonDurationTurns && next.turns > next.summonDurationTurns) {
        setHp(next, 0); next.alive = false;
        if (state === battle) notifySummonLost(next);
        if (state === battle) log(`${actorLabel(next)} returns from where it was summoned.`);
        continue;
      }
      if (state === battle && battle.season?.ended !== true) {
        seasonTurn(next);
        if (hp(next) <= 0) { next.alive = false; continue; }
      }
      if (state === battle) wearerSeasonTurn(next);
      if (state === battle && battle.warband?.ended !== true) warbandTurn(next);
      if (state === battle && battle.royalDuel?.ended !== true) royalDuelTurn(next);
      if (state === battle && battle.storm?.ended !== true) stormTurn(next);
      if (state === battle && battle.reforge?.ended !== true) reforgeTurn(next);
      if (state === battle && battle.heartCores?.ended !== true) heartCoreTurn(next);
      if (state === battle && battle.sacrifice?.ended !== true) sacrificeTurn(next);
      if (state === battle && battle.phylactery?.ended !== true) phylacteryTurn(next);
      if (state === battle && battle.dream?.ended !== true) dreamTurn(next);
      if (state === battle && battle.star?.ended !== true) starTurn(next);
      // An inert actor (a lich's phylactery) never acts: its turns only count the rounds.
      if (next.inert) continue;
      if (stunned) {
        next.lastStunnedTurn = next.turns;
        if (state === battle) stage("stunned", { actorId: next.id });
        if (state === battle) log(`${actorLabel(next)} is stunned and loses their turn.`);
        continue;
      }
      state.current = next.id; state.ap = actionPointMaximum(next, state);
      if (state === battle && next.rootedAp) {
        state.ap = Math.max(0, state.ap - next.rootedAp);
        log(`${actorLabel(next)} struggles against the binding roots and loses ${next.rootedAp} AP.`);
        next.rootedAp = 0;
      }
      if (state === battle && next.staggerAp) {
        state.ap = Math.max(0, state.ap - next.staggerAp);
        log(`${actorLabel(next)} ${next.staggerReason || "reels"} and loses ${next.staggerAp} AP.`);
        next.staggerAp = 0; next.staggerReason = null;
      }
      if (state === battle && next.trapApLoss) {
        state.ap = Math.max(0, state.ap - next.trapApLoss);
        log(`${actorLabel(next)} is caught in the traps and loses ${next.trapApLoss} AP.`);
        next.trapApLoss = 0;
      }
      // Hourglass of Haste: extra AP on the wearer's first turn of the battle.
      const haste = state === battle && next.turns === 1 ? artifactAmount(next, "first-turn-ap") : 0;
      if (haste) { state.ap += haste; log(`${actorLabel(next)} gains ${haste} AP from the Hourglass of Haste.`); }
      if (state === battle) stage("turn", { actorId: next.id, controlled: next.team === "player" && !next.summoned });
      return next;
    }
    state.current = null; state.ap = 0; return null;
  }
  function predict() {
    if (!isActive()) return [];
    // The cloned future changes every turn. Never let the read-only UI frame
    // retain calculations across buff expiry, stun skips, or summon expiry.
    const previous = calculationFrame;
    calculationFrame = null;
    try {
      const future = copy(battle);
      const futurePlayer = actorById("player", future);
      futurePlayer.predictedHp = currentHp;
      futurePlayer.predictedMana = currentMana;
      futurePlayer.skillSnapshot = copy(activeSkills);
      const turns = [];
      for (let i = 0; i < 10; i++) {
        const actor = advance(future);
        if (!actor) break;
        turns.push({ id: actor.id, name: actor.name });
      }
      return turns;
    } finally { calculationFrame = previous; }
  }
  function returnDefeatedPartyToTown() {
    let regionId = WorldRules.normalizeRegionId(worldState.regionId);
    let town = WorldRules.nearestSettlement(worldState.position, regionId);
    const visited = new Set([regionId]);
    while (!town && WorldRules.getRegion(regionId).safeRetreatRegionId) {
      regionId = WorldRules.normalizeRegionId(WorldRules.getRegion(regionId).safeRetreatRegionId);
      if (visited.has(regionId)) break;
      visited.add(regionId);
      town = WorldRules.nearestSettlement(WorldRules.getRegion(regionId).start, regionId);
    }
    if (!town) return null;
    const previousRegionId = worldState.regionId;
    WorldRules.setActiveRegion(regionId);
    worldState.regionId = regionId;
    worldState.position = { x: town.x, y: town.y };
    worldState.destination = null;
    worldState.route = [];
    worldState.pendingEncounter = null;
    worldState.pendingRegionTransition = null;
    worldState.encounterTravelMinutes = 0;
    window.WorldMap?.pause(false);
    window.Guild?.onRegionChange(previousRegionId);
    $("map-status").textContent = `After your defeat, the party was returned to ${town.name}. Rest here to recover.`;
    return town;
  }
  function finish(result) {
    stage("finish", { result });
    stopPhaseCinematic(false);
    stopBossIntro(false);
    battle.active = false; battle.result = result; battle.ap = 0;
    battle.reactions = [];
    reactionModal.hidden = true;
    equipmentActionDamage.clear();
    updateModalState();
    if (result.startsWith("Victory")) { window.ChurchOfTalos?.onVictory(battle); window.HearthHome?.onVictory(battle); }
    if (result.startsWith("Escaped")) {
      const actor = battle.actors.find(actor => actor.guildObjective);
      if (actor) window.Guild?.avoidObjective(actor.guildObjective, actor.guildRematchId);
    }
    clearTimeout(timer); timer = null;
    log(result);
    for (const actor of battle.actors) {
      actor.effects = [];
      actor.equipmentCharges = {};
      actor.equipmentSummons = {};
      for (const state of Object.values(skillStates(actor))) state.active = false;
      actor.skillExpiry = {};
    }
    enemyCount = 0;
    if (result.startsWith("Defeat")) {
      const town = returnDefeatedPartyToTown();
      if (town) {
        battle.result += ` You were returned to ${town.name}.`;
        log(`The party is carried back to ${town.name}.`);
      }
    }
    // Search and Escort fights are settled by winning or fleeing; a defeat leaves them open (task 2, G15).
    const objectiveActor = battle.actors.find(actor => actor.guildObjective);
    if (objectiveActor) window.Guild?.onBattleFinished?.(result, objectiveActor.guildObjective);
    window.Music?.sync();
  }
  // Standard catalogue weapons become forged weapons from one level below the enemy to
  // three above; non-droppable markers follow the weapon they named.
  function armEnemy(character, noDropItems = []) {
    const marked = copy(noDropItems || []);
    if (!window.ForgeRules || !character) return { character, noDropItems: marked };
    character.noDropItems = marked;
    window.ForgeRules.armCharacter(character, character.level, getItem);
    const mapped = character.noDropItems;
    delete character.noDropItems;
    return { character, noDropItems: mapped };
  }
  // Enemies drop each non-unique piece of equipment they carry with a 40% chance.
  const EQUIPMENT_DROP_CHANCE = 40;
  function rollEquipmentDrop(id) {
    const item = getItem(id);
    return !item || !["weapon", "armor", "artifact"].includes(item.category) || isMasterworkEquipment(item) || rules.roll(EQUIPMENT_DROP_CHANCE);
  }
  function itemCanDrop(id) {
    const item = getItem(id);
    return Boolean(item && item.droppable !== false);
  }
  function itemCanDropNormally(id) {
    const item = getItem(id);
    return Boolean(item && item.droppable !== false && item.automaticDrop !== false);
  }
  function addEnemyLoot(itemIds, chancedDrop = false) {
    const partyClasses = [classLevels, ...companions.map(companion => companion.character.classLevels)];
    const doubleDrops = chancedDrop && ["heavy-knight", "jester"].every(id => partyClasses.some(levels => toInteger(levels?.[id], 0) > 0));
    // Masterwork weapons and armor are forged, and masterwork artifacts fixed (A5), at your level when they drop.
    const forgeDrop = id => window.ForgeRules?.masterworkTemplateKey(id) ? window.ForgeRules.encode(window.ForgeRules.generateMasterwork({ key: window.ForgeRules.masterworkTemplateKey(id), level })) : masterworkArtifactIdAt(id, level);
    for (const id of itemIds) {
      battle.lootItems.push(forgeDrop(id));
      if (doubleDrops && rules.roll(20)) battle.lootItems.push(forgeDrop(id));
    }
  }
  function renderLoot() {
    if (!battle || battle.lootResolved || !battle.result.startsWith("Victory")) return false;
    const loot = Array.isArray(battle.lootItems) ? battle.lootItems.filter(itemCanDrop) : [];
    const quantities = loot.reduce((counts, id) => ({ ...counts, [id]: (counts[id] || 0) + 1 }), {});
    $("combat-loot-money").textContent = ShopRules.formatPrice(Math.max(0, toInteger(battle.moneyEarned, 0)));
    $("combat-loot-exp").textContent = `${formatValue(Math.max(0, toInteger(battle.experienceEarned, 0)))} EXP`;
    $("combat-loot-list").innerHTML = Object.entries(quantities).map(([id, quantity]) => {
      const item = getItem(id);
      return `<article class="combat-loot-item ${itemRarityClass(item)}"><div><strong>${escapeHtml(item.name)}</strong><span>${escapeHtml(item.rarity)} · ${escapeHtml(item.category)}</span></div><b>×${quantity}</b></article>`;
    }).join("") || '<p class="combat-loot-empty">The enemies carried no usable items.</p>';
    lootModal.hidden = false;
    updateModalState();
    $("combat-loot-keep").focus();
    return true;
  }
  function resolveLoot(keepItems) {
    if (!battle || battle.lootResolved || lootModal.hidden) return;
    if (keepItems) {
      const kept = battle.lootItems.filter(itemCanDrop);
      inventory.push(...kept);
      classProgress.claimedGuaranteedDrops ||= [];
      for (const actor of battle.actors.filter(entry => entry.team === "enemy")) for (const drop of actor.dropTable || []) {
        if (drop.guaranteedUntilClaimed && kept.some(id => lootMatchesDrop(id, drop.itemId)) && !classProgress.claimedGuaranteedDrops.includes(drop.itemId)) classProgress.claimedGuaranteedDrops.push(drop.itemId);
      }
      classProgress.lootEnemiesCollected += battle.actors.filter(actor => actor.team === "enemy" && !actor.summoned && hp(actor) <= 0 && actor.lootAvailable).length;
    }
    battle.lootResolved = true;
    lootModal.hidden = true;
    offerArchivedSpell();
    renderOwnedItems(); renderAvailableItems(); updateDerivedValues();
    updateModalState(); switchView(false); saveCharacter();
  }
  function offerArchivedSpell() {
    if (!battle?.result.startsWith("Victory") || !passiveEffectsOf(player()).some(effect => effect.type === "archive-enemy-spells")) return;
    const highestTier = Math.max(0, ...knownSpells.map(getSpell).filter(Boolean).map(spell => spell.tier));
    const choices = [...new Set(battle.observedSpells || [])].map(getSpell).filter(spell => spell && spell.tier <= highestTier);
    if (!choices.length) return;
    const answer = window.prompt(`Field Notes: choose one observed spell to archive (tier ${highestTier} limit):\n${choices.map((spell, index) => `${index + 1}. ${spell.name} · Tier ${spell.tier}`).join("\n")}`, "1");
    const index = Number(answer) - 1;
    const selected = Number.isInteger(index) && index >= 0 && index < choices.length ? choices[index] : choices.find(spell => spell.name.toLowerCase() === String(answer || "").toLowerCase());
    if (!selected) return;
    const item = getItem(`consumable-archived-${selected.id}`);
    if (!item) return;
    inventory.push(item.id);
    renderOwnedItems(); renderAvailableItems();
    log(`Field Notes creates ${item.name}.`);
  }
  // A looted piece answers a drop entry: the same item, or a piece forged from that masterwork template.
  const lootMatchesDrop = (id, itemId) => id === itemId || Boolean(ForgeRules.masterworkTemplateKey(itemId)) && getItem(id)?.forged?.masterwork === ForgeRules.masterworkTemplateKey(itemId)
    || Boolean(getItem(itemId)?.masterworkArtifactBaseId) && getItem(id)?.masterworkArtifactBaseId === getItem(itemId).masterworkArtifactBaseId;
  function awardDefeatedEnemyExperience() {
    if (!battle) return 0;
    let awarded = 0;
    for (const actor of battle.actors) {
      if (actor.team !== "enemy" || actor.summoned || actor.experienceAwarded || hp(actor) > 0 || phylacteryHolds(actor)) continue;
      actor.experienceAwarded = true;
      const rewardMultiplier = withActor(actor, () => calculateClassEffects().experienceRewardMultiplier);
      const reward = Math.max(0, Math.floor(rules.enemyExperience(actor.character?.level) * rewardMultiplier * (actor.empowered ? 1.2 : 1) * (actor.sparkling ? SPARKLING.experienceMultiplier : 1) * Math.max(0, toNumber(actor.experienceRewardMultiplier, 1))));
      // Bandit's Coin raises the party's silver, whoever wears it (A25).
      const moneyBonus = calculateClassEffects().enemyMoneyPercent + battle.actors.filter(isHero).reduce((sum, hero) => sum + artifactAmount(hero, "money-percent"), 0);
      // Treasure Seeker raises it once more, for the party's best copy (task 5, M13).
      const money = Math.max(1, Math.ceil(Math.max(1, toInteger(actor.character?.level, 1)) * 3 * (1 + moneyBonus / 100) * (1 + treasureMoneyPercent() / 100) * Math.max(0, toNumber(actor.moneyRewardMultiplier, 1))));
      battle.experienceEarned = Math.max(0, toInteger(battle.experienceEarned, 0)) + reward;
      battle.moneyEarned = Math.max(0, toInteger(battle.moneyEarned, 0)) + money;
      const lootStart = battle.lootItems.length;
      addEnemyLoot(actor.character.inventory.filter(id => itemCanDropNormally(id) && !(actor.noDropItems || []).includes(id)).filter(rollEquipmentDrop));
      const salvageChance = living().filter(ally => ally.team === "player").flatMap(passiveEffectsOf).filter(effect => effect.type === "salvage-supply-chance").reduce((best, effect) => Math.max(best, toNumber(effect.amount, 0)), 0);
      if (salvageChance && rules.roll(salvageChance)) addEnemyLoot(["consumable-camping-supplies"], salvageChance < 100);
      for (const drop of actor.dropTable || []) {
        const droppedItem = getItem(drop.itemId);
        const luckBonus = isMasterworkEquipment(droppedItem) ? masterworkLuckBonus() : 0;
        const guaranteed = drop.guaranteedOnFirstKill && !classProgress.firstKillDrops?.includes(drop.itemId) || drop.guaranteedUntilClaimed && !classProgress.claimedGuaranteedDrops?.includes(drop.itemId) && !battle.lootItems.some(id => lootMatchesDrop(id, drop.itemId));
        if (drop.guaranteedOnFirstKill) {
          classProgress.firstKillDrops ||= [];
          if (!classProgress.firstKillDrops.includes(drop.itemId)) classProgress.firstKillDrops.push(drop.itemId);
        }
        if ((actor.noDropItems || []).includes(drop.itemId) || !itemCanDrop(drop.itemId) || !guaranteed && !rules.roll(drop.chance + luckBonus)) continue;
        const minimum = Math.max(1, toInteger(drop.minimum, 1));
        const maximum = Math.max(minimum, toInteger(drop.maximum, minimum));
        const quantity = minimum + Math.floor(Math.random() * (maximum - minimum + 1));
        addEnemyLoot(Array(quantity).fill(drop.itemId), !guaranteed && drop.chance < 100);
      }
      addEnemyLoot(rollSparklingMasterworks(actor), true);
      // Studious Quill raises only its wearer's EXP (A25).
      exp += Math.floor(reward * (1 + artifactAmount(player(), "exp-percent") / 100));
      currencies = ShopRules.walletFromValue(ShopRules.walletValue(currencies) + money);
      document.querySelectorAll(".currency-input").forEach(input => { input.value = currencies[input.dataset.currency]; });
      for (const companion of companions) if (companion.character.currentHp > 0) companion.character.exp += Math.floor(reward * (1 + artifactAmount(actorById(companion.id), "exp-percent") / 100));
      awarded += reward;
      log(`${actor.name} grants ${reward} EXP and ${ShopRules.formatPrice(money)}.`);
      addEnemyLoot((window.Guild?.onEnemyDefeated(actor) || []).filter(itemCanDrop));
      const artifact = rollEnemyArtifact(actor);
      if (artifact) addEnemyLoot([artifact]);
      actor.lootAvailable = battle.lootItems.length > lootStart;
    }
    return awarded;
  }
  /* The Artifact Rework (port task 4, A12–A14): every defeated enemy but a boss (the unique ones) has a 1% chance to
     drop an artifact of any rarity from uncommon up, weighted by its level the way shop stock is. */
  const ENEMY_ARTIFACT_DROP_CHANCE = 1;
  function rollEnemyArtifact(actor) {
    if (!actor || actor.unique || actor.inert || actor.summoned || !rules.roll(ENEMY_ARTIFACT_DROP_CHANCE)) return null;
    return artifactForEnemyLevel(Math.max(1, toInteger(actor.character?.level, 1)));
  }
  function artifactForEnemyLevel(enemyLevel) {
    const pool = items.filter(item => item.category === "artifact" && item.rarity !== "masterwork" && item.droppable !== false);
    const weighted = ["uncommon", "rare", "epic", "legendary"].map(rarity => ({ rarity, weight: ShopRules.rarityWeight(rarity, enemyLevel), choices: pool.filter(item => item.rarity === rarity) })).filter(entry => entry.choices.length);
    const total = weighted.reduce((sum, entry) => sum + entry.weight, 0);
    let roll = Math.random() * total;
    const chosen = weighted.find(entry => (roll -= entry.weight) < 0) || weighted.at(-1);
    return chosen ? chosen.choices[Math.floor(Math.random() * chosen.choices.length)].id : null;
  }
  /* Sparkling enemies (port task 5, M2–M12): any regular enemy can turn up Sparkling, never in a battle with a boss. The
     map rolls it when an encounter appears (rollSparkling, so the encounter screen shows it); startRoster makes it
     stronger (all eight attributes ×1.4, then its maximum HP doubled); it gives 11 times the EXP and rolls 70% and 5%
     for a masterwork from all of them, Moneyshot and Treasure Seeker five times as likely as any other. */
  const SPARKLING = { chance: 0.1, cullChance: 2, luckDivisor: 500, maxLuckFactor: 2, statMultiplier: 1.4, maxHpMultiplier: 2, experienceMultiplier: 11, masterworkChances: [70, 5], favoredWeight: 5 };
  const SPARKLING_FAVORED = ["masterwork-moneyshot", "artifact-treasure-seeker-artifact"];
  function enemyDefinitionNamed(name) {
    return enemyDefinitions.find(enemy => enemy.name === name) || enemyDefinitions.find(enemy => (enemy.formerNames || []).includes(name)) || null;
  }
  // One enemy's chance: 0.1%, or 2% for the Cull's target inside its circle (M4), times (1 + Luck / 500) up to double (M3, M9).
  function sparklingChance(name, cullTarget = null) {
    const factor = Math.min(SPARKLING.maxLuckFactor, 1 + Math.max(0, toNumber(currentStats().luck, 0)) / SPARKLING.luckDivisor);
    return (cullTarget && name === cullTarget ? SPARKLING.cullChance : SPARKLING.chance) * factor;
  }
  // The encounter's Sparkling enemies, as indexes into its names: none where a boss fights, never an object (M2).
  function rollSparkling(enemyNames) {
    if (!Array.isArray(enemyNames) || !enemyNames.length) return [];
    const definitions = enemyNames.map(enemyDefinitionNamed);
    if (definitions.some(definition => !definition || definition.unique)) return [];
    const cullTarget = window.Guild?.cullTargetHere?.() || null;
    return enemyNames.flatMap((name, index) => !definitions[index].inert && rules.roll(sparklingChance(name, cullTarget)) ? [index] : []);
  }
  function masterworkLuckBonus() {
    return Math.max(0, toNumber(currentStats().luck, 0)) * 0.05 + Math.max(0, toNumber(calculateClassEffects().masterworkDropChanceFlat, 0));
  }
  // Every masterwork weapon, armor and artifact in the game (M10), Moneyshot and Treasure Seeker weighted ×5 (M11).
  function sparklingMasterworkPool() {
    const forged = (window.ForgeRules?.MASTERWORK_IDS || []).map(key => window.ForgeRules.masterworkTemplateId(key));
    const artifacts = items.filter(item => item.category === "artifact" && item.rarity === "masterwork" && !/-lv\d+$/.test(item.id)).map(item => item.id);
    return [...forged, ...artifacts].map(id => ({ id, weight: SPARKLING_FAVORED.includes(id) ? SPARKLING.favoredWeight : 1 }));
  }
  function sparklingMasterwork(random = Math.random) {
    const pool = sparklingMasterworkPool(), total = pool.reduce((sum, entry) => sum + entry.weight, 0);
    let roll = random() * total;
    return (pool.find(entry => (roll -= entry.weight) < 0) || pool.at(-1))?.id || null;
  }
  // Two separate rolls, 70% and 5%, each raised by the masterwork Luck bonus (M8, M12).
  function rollSparklingMasterworks(actor) {
    if (!actor?.sparkling) return [];
    const bonus = masterworkLuckBonus();
    return SPARKLING.masterworkChances.filter(chance => rules.roll(chance + bonus)).map(() => sparklingMasterwork()).filter(Boolean);
  }
  function recordSoloSurvivorVictory() {
    if (!battle || battle.soloVictoryRecorded) return false;
    const survivors = living().filter(actor => actor.team === "player");
    if (survivors.length !== 1 || survivors[0].summoned) return false;
    const survivor = survivors[0];
    if (survivor.id === "player") soloSurvivorVictories += 1;
    else survivor.character.soloSurvivorVictories = Math.max(0, toInteger(survivor.character.soloSurvivorVictories, 0)) + 1;
    battle.soloVictoryRecorded = true;
    log(`${actorLabel(survivor)} earns a victory with no living ally remaining.`);
    renderClasses();
    return true;
  }
  function recordFlawlessVictory() {
    if (!battle || battle.flawlessVictoryRecorded) return;
    battle.flawlessVictoryRecorded = true;
    for (const actor of living().filter(entry => entry.team === "player" && !entry.summoned && !entry.damageTaken)) {
      if (actor.id === "player") flawlessVictories += 1;
      else actor.character.flawlessVictories = Math.max(0, toInteger(actor.character.flawlessVictories, 0)) + 1;
      log(`${actorLabel(actor)} finishes combat without taking damage.`);
    }
    renderClasses();
  }
  function recordBoundFamiliarGrowth() {
    for (const owner of living().filter(actor => !actor.summoned && passiveEffectsOf(actor).some(effect => effect.type === "persistent-summon"))) {
      if (!living().some(actor => actor.boundFamiliar && actor.summonerId === owner.id)) continue;
      const progress = owner.id === "player" ? classProgress : owner.character.classProgress;
      if (!progress?.boundFamiliar) continue;
      progress.boundFamiliar.victories = Math.min(10, Math.max(0, toInteger(progress.boundFamiliar.victories, 0)) + 1);
      log(`${actorLabel(owner)}'s bound familiar grows stronger after surviving combat.`);
    }
  }
  function checkOutcome() {
    if (!isActive()) return;
    awardDefeatedEnemyExperience();
    if (!living().some(actor => actor.team === "player" && !actor.summoned)) finish("Defeat — your party has fallen.");
    // Victory needs every enemy down, not merely out of reach: a fighter locked in a protected
    // duel with someone else is out of the player's reach but still standing.
    else if (!hostiles(player()).length) {
      const earned = Math.max(0, toInteger(battle.experienceEarned, 0));
      const money = Math.max(0, toInteger(battle.moneyEarned, 0));
      recordSoloSurvivorVictory();
      recordFlawlessVictory();
      recordBoundFamiliarGrowth();
      finish(`Victory — all enemies defeated.${earned ? ` ${earned} EXP earned.` : ""}${money ? ` ${ShopRules.formatPrice(money)} found.` : ""}`);
      renderLoot();
    }
  }
  function syncEnemyCount() { enemyCount = isActive() ? opponents(player()).length : 0; }
  function refresh() {
    if (refreshing) return;
    refreshing = true;
    try {
      for (const actor of battle?.actors || []) if (actor.equipmentCharges) {
        const equipped = new Set(withActor(actor, () => equippedDefenseItems().map(item => item.id)));
        for (const id of Object.keys(actor.equipmentCharges)) if (!equipped.has(id)) delete actor.equipmentCharges[id];
      }
      processEquipmentReactions();
      stageQuiet = true;
      try {
        for (const actor of battle?.actors || []) {
          if (actor.id === "player") continue;
          const v = values(actor);
          setHp(actor, Math.max(0, Math.min(hp(actor), v.maxHp)));
          setMana(actor, Math.max(0, Math.min(mana(actor), v.maxMana)));
        }
      } finally { stageQuiet = false; }
      checkOutcome(); syncEnemyCount();
      if (isActive() && !battle.current) advance();
      updateDerivedValues(); renderKnownSpells(); renderSkills(); renderPassives();
      window.Companions?.render();
      render(); saveCharacter();
    } finally { refreshing = false; }
    scheduleAI();
  }
  function scheduleAI() {
    clearTimeout(timer); timer = null;
    if (isActive() && view && battle.current && !isControlledTurn() && !hasPlayerEquipmentReaction()) {
      const actor = currentActor();
      const firstAction = !toInteger(actor?.turnActionCount, 0);
      const delay = window.BattleStage?.enabled() ? window.BattleStage.enemyDelay(firstAction) : window.GameSettings?.enemyDelay(firstAction) ?? 650;
      timer = setTimeout(enemyAction, Math.max(delay, (actor?.phaseFlashUntil || 0) - Date.now() + 100, bossIntroUntil - Date.now() + 100));
    }
  }
  function onSheetChange() {
    if (refreshing || evaluating || !isActive() || pendingRefresh) return;
    pendingRefresh = true;
    queueMicrotask(() => { pendingRefresh = false; refresh(); });
  }
  function switchView(show, renderImmediately = true) {
    view = show;
    if (!show) window.TierTenCinematics?.stop();
    document.body.classList.toggle("in-combat-view", show);
    $("combat-view").hidden = !show;
    if (show) {
      document.querySelectorAll(".modal-layer").forEach(modal => { modal.hidden = true; });
      updateModalState();
    }
    if (renderImmediately) {
      if (show && battle?.reactions?.length) refresh();
      else render();
    }
    scheduleAI(); window.scrollTo(0, 0);
  }
  function recordTurnAction(actor, kind, manaSpent = 0) {
    actor.lastActionKind = kind;
    if (!toInteger(actor.turnActionCount, 0) && actor.predictions?.length) {
      for (const prediction of actor.predictions) {
        const owner = actorById(prediction.ownerId);
        if (!owner || hp(owner) <= 0) continue;
        const correct = prediction.guess === kind || prediction.guess === "defend" && ["guard", "evade"].includes(kind);
        const upgrade = correct ? passiveEffectsOf(owner).find(effect => effect.type === "prediction-upgrade") : null;
        const defenseBonus = correct ? toNumber(prediction.defensePercent, 75) + toNumber(upgrade?.defensePercentFlat, 0) : 10;
        const duration = correct ? Math.max(1, toInteger(prediction.durationTurns, 2)) : 1;
        applyTimedEffects(owner, "diviner:foretell", "Foretell", [{ type: "defense-percent", target: "armor", amount: defenseBonus, durationTurns: duration }, { type: "defense-percent", target: "evasion", amount: defenseBonus, durationTurns: duration }]);
        if (correct && upgrade?.incomingDamagePercent) applyTimedEffects(owner, "diviner:foresight", "Perfect Foresight", [{ type: "incoming-attack-damage-percent", amount: toNumber(upgrade.incomingDamagePercent, 0), durationTurns: duration }]);
        log(`${actorLabel(owner)} ${correct ? "correctly" : "incorrectly"} foretold ${actorLabel(actor)}'s ${kind} action and gains ${defenseBonus}% Armor and Evasion.`);
      }
      actor.predictions = [];
    }
    actor.turnActionCount = Math.max(0, toInteger(actor.turnActionCount, 0)) + 1;
    if (kind === "spell") {
      actor.turnSpellCount = Math.max(0, toInteger(actor.turnSpellCount, 0)) + 1;
      actor.turnSpellManaSpent = Math.max(0, toInteger(actor.turnSpellManaSpent, 0)) + Math.max(0, toInteger(manaSpent, 0));
    }
  }
  function finalizeTurn(actor) {
    if (!actor) return;
    const pureSpells = passiveEffectsOf(actor).filter(effect => effect.type === "single-spell-turn-mana-refund-percent");
    if (actor.turnActionCount === 1 && actor.turnSpellCount === 1 && actor.turnSpellManaSpent > 0 && pureSpells.length) {
      const percent = pureSpells.reduce((sum, effect) => sum + Math.max(0, toNumber(effect.amount, 0)), 0);
      const restored = Math.min(values(actor).maxMana - mana(actor), Math.floor(actor.turnSpellManaSpent * percent / 100));
      if (restored > 0) {
        setMana(actor, mana(actor) + restored);
        log(`${actorLabel(actor)} restores ${actor.team === "player" ? restored + " " : ""}Mana with Pure Spells.`);
      }
    }
    actor.turnActionCount = 0;
    actor.turnSpellCount = 0;
    actor.turnSpellManaSpent = 0;
  }
  function endTurn() {
    if (!isActive()) return;
    finalizeTurn(currentActor());
    if (battle.royalDuel && !battle.royalDuel.ended) royalDuelTurnEnd(currentActor());
    if (battle.reforge && !battle.reforge.ended) reforgeTurnEnd(currentActor());
    log(`${actorLabel(currentActor())} ends their turn.`);
    const next = advance();
    if (next) log(`${next.id === "player" ? "Your" : actorLabel(next) + "'s"} turn — ${battle.ap} AP.`);
    else log("No counters can advance. Increase a participant’s Speed on the character sheet to continue.");
    refresh();
  }
  function spend(actor, cost) {
    if (!isActive() || battle.current !== actor.id || hp(actor) <= 0 || battle.ap < cost) return false;
    battle.ap -= cost; return true;
  }
  function skillMatchesSpell(actor, skill, spell) {
    const required = [skill.requiredSpellElement, ...(skill.requiredSpellElements || [])].filter(Boolean).map(element => String(element).toLowerCase());
    return !required.length || withActor(actor, () => effectiveSpellElements(spell)).some(element => required.includes(element));
  }
  function focusedPowerMultiplier(actor) {
    const bonus = skillsOf(actor).filter(skill => skillStates(actor)[classSkillKey(skill)]?.active)
      .reduce((sum, skill) => sum + Math.max(0, toNumber(skill.offensePowerPercent, 0)), 0);
    return 1 + bonus / 100;
  }
  function consumeSkill(actor, field, context = null) {
    for (const skill of skillsOf(actor)) {
      const key = classSkillKey(skill), state = skillStates(actor)[key];
      const contextMatches = !context || field === "durationAttacks"
        ? !context || weaponMatches(context, skill.requiredWeaponType)
        : skillMatchesSpell(actor, skill, context);
      if (skill[field] && state?.active && contextMatches) {
        state.remainingUses = (state.remainingUses ?? skill[field]) - 1;
        if (state.remainingUses <= 0) state.active = false;
      }
    }
  }
  const elementList = element => [...new Set((Array.isArray(element) ? element : [element]).filter(Boolean).map(value => String(value).toLowerCase()))];
  const elementLabel = element => elementList(element).join("/") || "basic";
  // While a rune-split weapon strikes, its two elements share the damage instead of
  // using the weaker resistance: 50% fire on a fire-immune target loses only that half.
  // The striking weapon's own critical damage bonus rides along the same way.
  let elementShareContext = null, activeWeaponCritDamage = 0;
  const weaponElement = weapon => weapon.elementShare ? [weapon.element || "physical", weapon.elementShare.element] : weapon.element || "physical";
  // A weapon strike's element: the striker's own (port task 6: the Corrupted Mire Stalker's Dark), else its weapon's.
  const strikeElement = (actor, weapon) => actor?.attackElement || weaponElement(weapon);
  function withWeaponElement(weapon, run, target = null) {
    const previous = elementShareContext, previousCrit = activeWeaponCritDamage;
    elementShareContext = weapon?.elementShare ? { base: weapon.element || "physical", element: weapon.elementShare.element, percent: weapon.elementShare.percent } : null;
    activeWeaponCritDamage = toNumber(weapon?.critDamage, 0) + critTargetEffects(weapon, target).reduce((sum, effect) => sum + toNumber(effect.damage, 0), 0);
    try { return run(); } finally { elementShareContext = previous; activeWeaponCritDamage = previousCrit; }
  }
  // Melee attacks face the target's full Evasion, ranged attacks half of it.
  const evasionAgainst = (evasion, weaponType) => Math.max(0, toNumber(evasion, 0)) * (weaponType === "ranged" ? 0.5 : 1);
  // Resistances granted by held weapons, such as Bloody Mary's against Dark, Death and Blood.
  function weaponResistance(actor, element) {
    return cacheCalculation(`weapon-resist:${element}`, () => {
      const slots = weaponSlotsOf(actor), held = [...new Map([slots.primary, slots.offHand].filter(Boolean).map(item => [item.id, item])).values()];
      return held.flatMap(item => item.effects || []).filter(effect => effect.kind === "element-resistance" && (effect.elements || []).includes(element)).reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
    }, actor);
  }
  // Resistances granted by worn forged armor (materials, makers and elemental runes).
  function armorResistance(actor, element) {
    return cacheCalculation(`armor-resist:${element}`, () => withActor(actor, () => equippedDefenseItems().reduce((sum, item) => sum + toNumber(item.resistances?.[element], 0), 0)), actor);
  }
  function equippedArmorTotal(actor, key) {
    return cacheCalculation(`armor-total:${key}`, () => withActor(actor, () => equippedDefenseItems().reduce((sum, item) => sum + toNumber(item[key], 0), 0)), actor);
  }
  function resistance(target, element, spell = false) {
    const resistances = elementList(element).map(singleElement => {
      let amount = toNumber(target.resistances?.[singleElement], 0) + armorResistance(target, singleElement) + weaponResistance(target, singleElement)
        + artifactEffectsOf(target, "element-resistance-percent").filter(effect => effect.element === singleElement).reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
      if (spell) withActor(target, () => {
        for (const effect of getSelectedRace()?.passive.effects || []) {
          const protectedElement = String(effect.element || "").toLowerCase();
          if ((effect.type || effect.kind) === "incoming-spell-damage-percent" && (!protectedElement || protectedElement === "all" || protectedElement === singleElement)) amount -= toNumber(effect.amount, 0);
        }
        const classEffects = calculateClassEffects();
        amount -= toNumber(classEffects.incomingSpellDamagePercentByElement[singleElement], 0) + toNumber(classEffects.incomingSpellDamagePercent, 0);
      });
      if (spell) for (const effect of target.effects.filter(entry => entry.type === "incoming-spell-damage-percent")) {
        const elements = effect.elements || (effect.element ? [effect.element] : []);
        const excluded = effect.excludedElements || [];
        if ((!elements.length || elements.includes(singleElement)) && !excluded.includes(singleElement)) amount -= toNumber(effect.amount, 0);
      }
      return amount;
    });
    const list = elementList(element);
    if (!spell && elementShareContext && list.length === 2 && list.includes(elementShareContext.base) && list.includes(elementShareContext.element)) {
      const share = elementShareContext.percent / 100;
      return resistances[list.indexOf(elementShareContext.base)] * (1 - share) + resistances[list.indexOf(elementShareContext.element)] * share;
    }
    return resistances.length ? Math.min(...resistances) : 0;
  }
  const passiveEffectsOf = (actor, state = battle) => cacheCalculation("passive-effects", () => passivesOf(actor, state).flatMap(passive => passive.effects || (passive.effect ? [passive.effect] : [])), actor, state);
  function chargedSkillEffect(actor, effectType) {
    if (!actor) return null;
    const skill = skillsOf(actor).find(candidate => skillStates(actor)[classSkillKey(candidate)]?.active && (candidate.effects || []).some(effect => effect.type === effectType));
    return skill ? { key: classSkillKey(skill), effect: skill.effects.find(effect => effect.type === effectType), name: skill.name } : null;
  }
  function untouchedBonus(actor, effect) {
    const cutoff = values(actor).maxHp * Math.max(1, toNumber(effect.cutoffPercent, 50)) / 100;
    return Math.max(0, toNumber(effect.maxAmount, 0)) * Math.max(0, 1 - Math.max(0, toNumber(actor.damageTaken, 0)) / Math.max(1, cutoff));
  }
  const hasClass = (actor, id) => actorClassLevel(actor, id) > 0;
  // Stunted Sword: more damage against targets at or below a share of their HP, more still against some races.
  const woundedTargetPercent = (weapon, target) => (weapon.effects || [])
    .filter(effect => effect.kind === "damage-percent-vs-wounded" && hp(target) <= values(target).maxHp * toNumber(effect.hpPercent, 30) / 100)
    .reduce((sum, effect) => sum + toNumber(effect.raceBonus?.[actorRace(target)], toNumber(effect.amount, 0)), 0);
  // Bloody Mary: extra crit chance (in points) and crit damage against chosen classes or races.
  const critTargetEffects = (weapon, target) => target ? (weapon?.effects || []).filter(effect => effect.kind === "crit-vs-targets"
    && ((effect.classIds || []).some(id => hasClass(target, id)) || (effect.raceIds || []).includes(actorRace(target)))) : [];
  const weaponRaceDamagePercent = (weapon, target) => (weapon.effects || [])
    .filter(effect => effect.kind === "damage-percent-vs-race" && (effect.raceIds || []).includes(actorRace(target)))
    .reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
  const damagePercentAgainstTarget = (attacker, target) => {
    if (!attacker) return 0;
    const effects = withActor(attacker, () => calculateClassEffects());
    const classBonus = Object.entries(effects.damagePercentByClass).filter(([classId]) => hasClass(target, classId)).reduce((sum, [, amount]) => sum + toNumber(amount, 0), 0);
    return classBonus + toNumber(effects.damagePercentByRace[actorRace(target)], 0);
  };
  function incomingDamageFactor(target, attacker = null, attackKind = "", element = "") {
    const targetClassEffects = withActor(target, () => calculateClassEffects());
    let percent = targetClassEffects.incomingDamagePercent + toNumber(targetClassEffects.incomingAttackDamagePercentByKind[normalizeWeaponType(attackKind)], 0);
    if (attacker) percent += Object.entries(targetClassEffects.incomingDamagePercentByClass).filter(([classId]) => hasClass(attacker, classId)).reduce((sum, [, amount]) => sum + toNumber(amount, 0), 0);
    if (attacker) percent += toNumber(targetClassEffects.incomingDamagePercentByRace?.[actorRace(attacker)], 0);
    const activeStance = target.effects.find(effect => effect.type === "stance")?.name?.toLowerCase();
    if (activeStance) percent += passiveEffectsOf(target).filter(effect => effect.type === "stance-incoming-damage-percent" && (!effect.stance || effect.stance === activeStance)).reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
    if (attacker && attackKind === "spell") percent += passiveEffectsOf(target).filter(effect => effect.type === "incoming-spell-damage-percent-vs-race" && (effect.raceIds || []).map(slugify).includes(actorRace(attacker))).reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
    if (target.summoned && ["wolf", "bear", "griffon"].includes(actorRace(target))) percent += living().filter(ally => ally.team === target.team && hp(ally) > 0).flatMap(passiveEffectsOf).filter(effect => effect.type === "animal-summon-incoming-damage-percent").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
    if (attacker) percent += withActor(target, () => equippedDefenseItems().flatMap(item => item.effects || []).filter(effect => effect.kind === "incoming-race-damage-percent" && (effect.raceIds || []).map(slugify).includes(actorRace(attacker))).reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0));
    if (attackKind === "spell") {
      const wards = withActor(target, () => equippedDefenseItems().flatMap(item => item.effects || [])
        .filter(effect => effect.kind === "incoming-spell-damage-percent" && (!effect.element || elementList(element).includes(effect.element))), battle);
      if (wards.length) percent += Math.min(...wards.map(effect => toNumber(effect.amount, 0)));
      percent += artifactAmount(target, "incoming-spell-damage-percent");
      percent += withActor(target, () => equippedWeaponItems().flatMap(item => item.effects || [])
        .filter(effect => effect.kind === "incoming-spell-damage-percent" && (!effect.element || elementList(element).includes(effect.element)))
        .reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0));
    }
    for (const effect of passiveEffectsOf(target)) {
      if (effect.type === "incoming-spell-damage-percent-per-sorcery-points" && attackKind === "spell") {
        const pointsPerStep = Math.max(1, toInteger(effect.points, 2));
        percent -= Math.floor(Math.max(0, toInteger(target.sorceryPoints, 0)) / pointsPerStep) * Math.max(0, toNumber(effect.amount, 0));
      }
    }
    for (const effect of target.effects) {
      if (effect.type === "incoming-attack-damage-percent" && (!effect.attackKinds?.length || effect.attackKinds.includes(attackKind))) percent += toNumber(effect.amount, 0);
      if (effect.type === "incoming-race-damage-percent" && attacker && (effect.raceIds || []).map(slugify).includes(actorRace(attacker))) percent += toNumber(effect.amount, 0);
      if (effect.type === "incoming-spell-damage-from-target-percent" && attackKind === "spell" && attacker?.id === effect.sourceActorId) percent += toNumber(effect.amount, 0);
    }
    percent += living().filter(ally => ally.team === target.team).flatMap(ally => ally.effects.filter(effect => effect.type === "team-incoming-damage-percent" && effect.expiresTurn > ally.turns)).reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
    if (attacker) for (const effect of attacker.effects) {
      if (effect.type === "outgoing-attack-damage-percent" && (!effect.attackKinds?.length || effect.attackKinds.includes(attackKind))) percent += toNumber(effect.amount, 0);
    }
    percent += royalDuelDamagePercent(target, attacker);
    const coreProtection = target.livingAllyProtection;
    const coreCount = coreProtection ? living().filter(ally => ally.id !== target.id && ally.team === target.team && ally.enemyDefinitionId === coreProtection.enemyId).length : 0;
    const coreFactor = Math.max(0, 1 - coreCount * toNumber(coreProtection?.amount, 0) / 100);
    const hpWard = attackKind === "spell" ? withActor(target, () => equippedDefenseItems().flatMap(item => item.effects || [])
      .filter(effect => effect.kind === "missing-hp-spell-ward")
      .reduce((best, effect) => Math.max(best, Math.max(0, effect.amount - Math.max(0, 1 - hp(target) / Math.max(1, values(target).maxHp)) * 100 / effect.missingHpPercentPerPoint)), 0)) : 0;
    return Math.max(0, 1 + percent / 100) * coreFactor * Math.max(0, 1 - hpWard / 100);
  }
  function consumeIncomingHitEffects(target, attackKind) {
    const applies = effect => effect.type === "incoming-attack-damage-percent" && (!effect.attackKinds?.length || effect.attackKinds.includes(attackKind)) ||
      effect.type === "incoming-spell-damage-percent" && attackKind === "spell" ||
      effect.type === "incoming-ranged-armor-percent" && attackKind === "ranged" ||
      effect.type === "incoming-weapon-damage-percent" && ["melee", "ranged"].includes(attackKind);
    for (const effect of target.effects.filter(effect => effect.remainingHits > 0 && applies(effect))) {
      effect.remainingHits -= 1;
    }
    target.effects = target.effects.filter(effect => effect.remainingHits == null || effect.remainingHits > 0);
  }
  function tryRaceDeathSave(target, element = "physical") {
    // An inert object (a phylactery) has no will to defy death.
    if (hp(target) > 0 || target.inert) return false;
    const effect = racePassiveEffectsOf(target).find(entry => entry.type === "death-save-decay");
    if (!effect || elementList(element).some(activeElement => (effect.excludedElements || []).includes(activeElement))) return false;
    target.raceState ||= {};
    const chance = Math.max(0, toNumber(target.raceState.deathSaveChance, effect.initialChance));
    if (!rules.roll(chance)) return false;
    setHp(target, 1);
    target.raceState.deathSaveChance = Math.max(0, chance - Math.max(0, toNumber(effect.reduction, 0)));
    log(`${actorLabel(target)} defies death (${formatValue(chance)}% chance).`);
    return true;
  }
  function recordCombatKill(attacker, target) {
    if (!battle || !attacker || !target || target.summoned || attacker.team === target.team || target.defeatCredited || hp(target) > 0) return;
    target.defeatCredited = true;
    const creditedActor = attacker.summoned && attacker.summonerId ? actorById(attacker.summonerId) || attacker : attacker;
    creditedActor.combatKills = Math.max(0, toInteger(creditedActor.combatKills, 0)) + 1;
    const defeatedClassLevels = target.id === "player" ? classLevels : target.character?.classLevels || {};
    const creditedClassKills = creditedActor.id === "player" ? classKills : (creditedActor.character.classKills ||= {});
    for (const [classId, classLevel] of Object.entries(defeatedClassLevels)) {
      if (toInteger(classLevel, 0) > 0) creditedClassKills[classId] = Math.max(0, toInteger(creditedClassKills[classId], 0)) + 1;
    }
    const creditedRaceKills = creditedActor.id === "player" ? raceKills : (creditedActor.character.raceKills ||= {});
    const defeatedRace = actorRace(target);
    if (defeatedRace) creditedRaceKills[defeatedRace] = Math.max(0, toInteger(creditedRaceKills[defeatedRace], 0)) + 1;
    const momentum = passiveEffectsOf(creditedActor).filter(effect => effect.type === "kill-turn-advance").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
    if (momentum) creditedActor.counter = Math.min(999, Math.max(0, creditedActor.counter + momentum));
    for (const owner of living().filter(actor => actor.team !== target.team && passiveEffectsOf(actor).some(effect => effect.type === "spread-affliction-on-kill"))) {
      const recipient = hostiles(owner).find(candidate => candidate.id !== target.id);
      const affliction = target.effects.find(effect => ["class-poison", "defense-flat", "defense-percent", "next-spell-penalty"].includes(effect.type) && (!effect.type.startsWith("defense-") || effect.amount < 0));
      if (recipient && affliction) recipient.effects.push({ ...affliction, key: `contagion:${owner.id}:${recipient.id}`, expiresTurn: recipient.turns + Math.max(1, affliction.expiresTurn - target.turns) });
    }
    if (creditedActor.id === "player") renderClasses();
    if (creditedActor.combatKills < 3 || creditedActor.multiKillCombatRecorded) return;
    creditedActor.multiKillCombatRecorded = true;
    if (creditedActor.id === "player") multiKillCombats += 1;
    else creditedActor.character.multiKillCombats = Math.max(0, toInteger(creditedActor.character.multiKillCombats, 0)) + 1;
    log(`${actorLabel(creditedActor)} has defeated at least 3 enemies in one combat.`);
    renderClasses();
  }
  function notifySummonLost(summoned) {
    if (!summoned.summoned || summoned.summonLossRecorded) return;
    summoned.summonLossRecorded = true;
    const owner = actorById(summoned.summonerId);
    if (!owner || hp(owner) <= 0) return;
    for (const effect of passiveEffectsOf(owner).filter(effect => effect.type === "summon-death-resource-percent")) {
      const maximum = values(owner);
      if (!healingDisabled(owner)) healHp(owner, Math.floor(maximum.maxHp * toNumber(effect.hp, 0) / 100), battle, owner);
      setMana(owner, Math.min(maximum.maxMana, mana(owner) + Math.floor(maximum.maxMana * toNumber(effect.mana, 0) / 100)));
    }
  }
  function dropStandards(owner) {
    for (const standard of battle?.actors || []) if (standard.standardAura && standard.summonerId === owner.id && hp(standard) > 0) {
      setHp(standard, 0);
      standard.alive = false;
      log(`${actorLabel(owner)} falls; their Battle Standard collapses.`);
    }
  }
  function directDamage(target, amount, reducible = true, element = "physical", attacker = null, bypassBarriers = false, exact = false) {
    const adjusted = Math.floor(Math.max(0, amount) * (reducible ? incomingDamageFactor(target) : 1) * (exact ? 1 : 1 + artifactDamagePercent(attacker, target) / 100));
    const dealt = bypassBarriers ? Math.min(adjusted, Math.max(0, hp(target) - phaseDamageFloor(target))) : applyCombatDamage(target, adjusted, attacker);
    if (bypassBarriers && dealt) { setHp(target, hp(target) - dealt); triggerDamageTakenEquipment(target, dealt, attacker); creditRoyalDuelDamage(target, attacker, dealt); creditReforgeDamage(target, attacker, dealt); creditStarDamage(target, attacker, dealt); }
    restoreManaFromElementalArmor(target, element, dealt);
    if (dealt > 0) target.damageTaken = Math.max(0, toNumber(target.damageTaken, 0)) + dealt;
    if (dealt > 0) {
      target.damageHistory ||= [];
      for (const activeElement of elementList(element)) target.damageHistory.push({ element: activeElement, turn: target.turns });
      target.damageHistory = target.damageHistory.slice(-30);
    }
    if (hp(target) <= 0 && !tryRaceDeathSave(target, element)) {
      logFall(target);
      dropStandards(target);
      notifySummonLost(target);
      recordCombatKill(attacker, target);
    }
    else if (dealt > 0) triggerLowHealthPassives(target);
    syncEnemyCount();
    return dealt;
  }
  // Masterwork armor and shields show their unique effect when it applies.
  function stageDefenseItems(target, attacker, element, spell, dealt) {
    if (!onStage(target) || dealt < 0) return;
    const elements = elementList(element);
    for (const item of withActor(target, equippedDefenseItems)) {
      if (item.rarity !== "masterwork") continue;
      const kinds = (item.effects || []).map(effect => effect.kind);
      const raceEffect = (item.effects || []).find(effect => effect.kind === "incoming-race-damage-percent");
      const spellEffects = (item.effects || []).filter(effect => effect.kind === "incoming-spell-damage-percent");
      if (raceEffect && attacker && (raceEffect.raceIds || [raceEffect.raceId]).filter(Boolean).map(slugify).includes(actorRace(attacker))) stageEquipment(target, item);
      else if (spell && spellEffects.some(effect => elements.some(candidate => [effect.element, ...(effect.elements || [])].includes(candidate)))) stageEquipment(target, item);
      else if (spell && kinds.includes("missing-hp-spell-ward")) stageEquipment(target, item);
      else if (kinds.includes("defense-cap") && dealt > 0) stageEquipment(target, item);
    }
  }
  function restoreManaFromElementalArmor(target, element, dealt) {
    if (dealt <= 0 || hp(target) <= 0) return;
    const elements = elementList(element);
    const restorations = withActor(target, () => equippedDefenseItems().flatMap(item => (item.effects || [])
      .filter(effect => effect.kind === "restore-mana-on-elemental-hit-percent" && elements.includes(effect.element))
      .map(effect => ({ ...effect, itemName: item.name }))));
    if (!restorations.length) return;
    const maximum = values(target).maxMana;
    const strongest = restorations.reduce((best, effect) => effect.amount > best.amount ? effect : best);
    const restored = Math.min(maximum - mana(target), Math.max(1, Math.floor(maximum * strongest.amount / 100)));
    if (restored <= 0) return;
    setMana(target, mana(target) + restored);
    stageEquipment(target, withActor(target, equippedDefenseItems).find(item => item.name === strongest.itemName));
    log(`${actorLabel(target)} restores ${restored} Mana with ${strongest.itemName}.`);
  }
  function consumeTrap(target, type) {
    const trap = target?.effects.find(effect => effect.type === type);
    if (trap) {
      trap.remainingTriggers = Math.max(1, toInteger(trap.remainingTriggers, 1)) - 1;
      if (trap.remainingTriggers <= 0) target.effects = target.effects.filter(effect => effect !== trap);
    }
    return trap;
  }
  function trapDamageAgainst(trap, target, rawDamage) {
    const boosted = Math.floor(Math.max(0, rawDamage) * (1 + toNumber(trap.damagePercent, 0) / 100));
    const armorPierce = Math.max(0, Math.min(100, toNumber(trap.armorPiercePercent, 0)));
    return rules.damage(boosted, values(target).armor * (1 - armorPierce / 100));
  }
  function triggerRetaliationTrap(target, attacker, type) {
    const trap = consumeTrap(target, type);
    if (!trap) return 0;
    const rawDamage = rules.trapDamage(values(attacker).maxHp, trap.cap, trap.amount);
    const damage = directDamage(attacker, trapDamageAgainst(trap, attacker, rawDamage), true, "physical", target);
    log(`${trap.name} triggers against ${actorLabel(attacker)} and deals ${damage} damage.`);
    return damage;
  }
  function triggerWeaponTraps(attacker, target, weaponType) {
    if (!attacker || !target || attacker.team === target.team) return;
    if (weaponType === "melee") {
      const springOwner = [target, ...battle.actors].find((actor, index, actors) => actors.indexOf(actor) === index && actor.team === target.team && actor.effects.some(effect => effect.type === "trap-spring"));
      if (springOwner) triggerRetaliationTrap(springOwner, attacker, "trap-spring");
    }
    triggerRetaliationTrap(target, attacker, "trap-sentry");
    triggerControlTraps(target, attacker, weaponType === "melee" ? "melee" : "ranged");
  }
  /* The Shop Rework's traps (port task 3; user decisions R17–R21, R26), armed on their owner like the Spring Trap
     and spent when they trigger. "attackKind" is "melee", "ranged" or "spell" (a spell that damages the owner).
     Tripwire: a melee attack costs the attacker 1 AP on its next turn. Smoke Trap: any attack gives the owner's
     side +20% Evasion until the owner's next turn (R28: a fallen owner's smoke lifts at each ally's next turn).
     Frost Glyph: a spell hit, even one a barrier soaks (R29), costs the caster 25% Speed for 2 turns. Weighted Net: a melee attack stuns the attacker for 1 turn (stun immunity holds). Gravity Well: any
     attack costs every enemy 2 AP on its next turn. AP losses from traps add to each other and to staggers and
     roots (R26). */
  function triggerControlTraps(owner, attacker, attackKind) {
    if (!owner || !attacker || owner.team === attacker.team || hp(owner) <= 0) return;
    const loseAp = (foe, amount) => { foe.trapApLoss = Math.max(0, toInteger(foe.trapApLoss, 0)) + Math.max(0, toInteger(amount, 0)); };
    if (attackKind === "melee") {
      const tripwire = consumeTrap(owner, "trap-tripwire");
      if (tripwire && hp(attacker) > 0) { loseAp(attacker, tripwire.amount); log(`${tripwire.name} trips ${actorLabel(attacker)}: ${tripwire.amount} AP lost on its next turn.`); }
      const net = consumeTrap(owner, "trap-weighted-net");
      if (net && hp(attacker) > 0) {
        if (debuffImmune(attacker) || withActor(attacker, () => calculateClassEffects().stunImmune)) log(`${actorLabel(attacker)} tears free of ${net.name}.`);
        else {
          attacker.effects = attacker.effects.filter(existing => existing.type !== "stun");
          attacker.effects.push({ key: `trap:${owner.id}:net-stun`, name: "Stunned", type: "stun", amount: 1, expiresTurn: attacker.turns + 1 });
          log(`${net.name} entangles ${actorLabel(attacker)}: stunned for 1 turn.`);
        }
      }
    }
    if (attackKind === "spell") {
      const glyph = consumeTrap(owner, "trap-frost-glyph");
      if (glyph && hp(attacker) > 0) {
        const duration = Math.max(1, toInteger(glyph.durationTurns, 2));
        if (debuffImmune(attacker)) log(`${actorLabel(attacker)} shrugs off ${glyph.name}.`);
        else {
          applyTimedEffects(attacker, `trap:${owner.id}:frost-glyph`, glyph.name, [{ type: "stat-percent", target: "speed", amount: -Math.abs(toNumber(glyph.amount, 25)), durationTurns: duration }]);
          log(`${glyph.name} freezes ${actorLabel(attacker)}: Speed −${Math.abs(toNumber(glyph.amount, 25))}% for ${duration} turns.`);
        }
      }
    }
    const smoke = consumeTrap(owner, "trap-smoke");
    if (smoke) {
      const allies = living().filter(ally => ally.team === owner.team);
      for (const ally of allies) {
        ally.effects = ally.effects.filter(existing => existing.smokeOwnerId !== owner.id);
        ally.effects.push({ key: `trap:${owner.id}:smoke`, name: smoke.name, type: "defense-percent", target: "evasion", amount: Math.abs(toNumber(smoke.amount, 20)), smokeOwnerId: owner.id, expiresTurn: Number.MAX_SAFE_INTEGER });
      }
      log(`${smoke.name} bursts around ${actorLabel(owner)}: ${allies.map(actorLabel).join(", ")} gain +${Math.abs(toNumber(smoke.amount, 20))}% Evasion until ${actorLabel(owner)}'s next turn.`);
    }
    const well = consumeTrap(owner, "trap-gravity-well");
    if (well) {
      const foes = living().filter(foe => foe.team !== owner.team && !foe.inert);
      for (const foe of foes) loseAp(foe, well.amount);
      log(`${well.name} collapses: every enemy loses ${well.amount} AP on its next turn.`);
    }
  }
  function hurt(target, power, element, critical = false, spell = false, attacker = null, attackKind = "", armorPierceOverride = 0, unreducible = false, spellTier = 0) {
    const mark = attacker && target.effects.find(effect => effect.type === "hunter-mark" && effect.ownerId === attacker.id && skillStates(attacker)[effect.skillKey]?.active);
    const attackerEffects = attacker ? withActor(attacker, () => calculateClassEffects()) : null;
    const spellDamagePercent = spell ? (attackerEffects?.spellDamagePercentModifiers || []).filter(modifier => {
      const restricted = [modifier.element, ...(modifier.elements || [])].filter(Boolean);
      return !restricted.length || restricted.some(candidate => elementList(element).includes(candidate));
    }).reduce((sum, modifier) => sum + toNumber(modifier.amount, 0), 0) : 0;
    let randomDamagePercent = 0;
    if (attacker) for (const effect of passiveEffectsOf(attacker).filter(effect => effect.type === "random-damage-percent")) {
      const luck = values(attacker).luck;
      if (!rules.roll(luck * Math.max(0, toNumber(effect.chancePerLuck, 0)))) continue;
      const minimum = luck * Math.max(0, toNumber(effect.minimumPerLuck, 0));
      const maximum = luck * Math.max(minimum / Math.max(1, luck), toNumber(effect.maximumPerLuck, 0));
      randomDamagePercent += minimum + Math.random() * Math.max(0, maximum - minimum);
    }
    const tacticalMark = attacker && target.effects.find(effect => effect.type === "tactical-mark" && effect.team === attacker.team);
    const eminenceBonus = attacker ? passiveEffectsOf(attacker).filter(effect => effect.type === "damage-percent-by-untouched").reduce((sum, effect) => sum + untouchedBonus(attacker, effect), 0) : 0;
    const canCharge = attacker && ["melee", "ranged", "spell"].includes(attackKind);
    const worldSaver = canCharge ? chargedSkillEffect(attacker, "next-damage-unreducible") : null;
    const worldEnder = canCharge ? chargedSkillEffect(attacker, "next-damage-overload") : null;
    const terrorBonus = attacker && hp(target) < values(target).maxHp / 2 ? passiveEffectsOf(attacker).filter(effect => effect.type === "damage-percent-vs-low-hp" && hp(target) < values(target).maxHp * toNumber(effect.threshold, 50) / 100).reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0) : 0;
    const reaverBonus = attacker && hp(target) < values(target).maxHp * .35 ? withActor(attacker, () => classCatalog.find(entry => entry.id === "reaver")?.levels.filter(rank => rank.level <= toInteger(classLevels.reaver, 0)).flatMap(rank => rank.effects).filter(effect => effect.type === "damage-percent-vs-low-hp").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0) || 0) : 0;
    const situationalBonus = attacker ? passiveEffectsOf(attacker).reduce((sum, effect) => sum + (effect.type === "damage-percent-vs-stunned" && (target.effects.some(active => active.type === "stun") || target.lastStunnedTurn === target.turns) ? toNumber(effect.amount, 0) : 0) + (effect.type === "damage-percent-vs-quest-target" && [worldState.guild.quest?.enemy, worldState.guild.greatHunt?.enemy].includes(target.name.replace(/^(?:(?:Empowered|Sparkling) )+/, "").replace(/ \d+$/, "")) ? toNumber(effect.amount, 0) : 0), 0) : 0;
    const standardBonus = attacker ? living().filter(ally => ally.team === attacker.team && ally.standardAura).reduce((sum, ally) => sum + toNumber(ally.standardAura, 0), 0) : 0;
    const actionChargeMultiplier = attackKind === "reaction" ? 1 : equipmentActionDamage.get(attacker?.id) || 1;
    // Artifacts (port task 4): the "+X% damage" ones (A20) and First Light Pin (A26). A Forked Lightning strike is plain:
    // its size is already a share of a finished hit (A24).
    const firstLight = chainStrikeDepth ? 0 : firstLightPercent(attacker, attackKind);
    const artifactPercent = chainStrikeDepth ? 0 : artifactDamagePercent(attacker, target) + firstLight;
    const markedPower = chainStrikeDepth ? power : power * actionChargeMultiplier * (1 + (spell ? toNumber(attacker?.phaseSpellDamagePercent, 0) : 0) / 100) * (1 + toNumber(mark?.damagePercent, 0) / 100) * (1 + toNumber(tacticalMark?.damagePercent, 0) / 100) * (1 + damagePercentAgainstTarget(attacker, target) / 100) * (1 + (toNumber(attackerEffects?.outgoingDamagePercent, 0) + spellDamagePercent + randomDamagePercent + eminenceBonus + terrorBonus + reaverBonus + situationalBonus + standardBonus + artifactPercent) / 100) * (1 + toNumber(worldEnder?.effect?.damagePercent, 0) / 100);
    const armorPierce = Math.max(0, Math.min(100, toNumber(armorPierceOverride, 0) + toNumber(worldEnder?.effect?.armorPiercePercent, 0) + (["melee", "ranged"].includes(attackKind) ? toNumber(attackerEffects?.weaponArmorPiercePercent, 0) + toNumber(attackerEffects?.weaponArmorPiercePercentByType?.[attackKind], 0) : 0)));
    const rangedArmorPercent = attackKind === "ranged" ? target.effects.filter(effect => effect.type === "incoming-ranged-armor-percent").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0) : 0;
    const effectiveArmor = values(target).armor * (1 + rangedArmorPercent / 100);
    const criticalBonusPercent = toNumber(attackerEffects?.criticalDamagePercent, 0) + (["melee", "ranged"].includes(attackKind) ? activeWeaponCritDamage : 0) + artifactAmount(attacker, "crit-damage-percent");
    // Spells ignore armor; only elemental resistance protects against them.
    // The Golden Apex's prism: Basic damage splits into four quarters, three of them random other elements.
    const prism = !worldSaver && !unreducible && elementList(element).length === 1 && elementList(element)[0] === "basic" && attacker && withActor(attacker, equippedWeaponItems).some(item => (item.effects || []).some(effect => effect.kind === "prism-basic-damage"));
    const prismElements = prism ? ["basic", ...SPELL_ELEMENTS.filter(candidate => candidate !== "basic").sort(() => Math.random() - 0.5).slice(0, 3)] : null;
    const prismParts = prismElements ? prismElements.map(part => Math.floor(rules.damage(markedPower / prismElements.length, spell ? 0 : effectiveArmor * (1 - armorPierce / 100), resistance(target, part, spell), critical, criticalBonusPercent) * incomingDamageFactor(target, attacker, attackKind, part))) : null;
    if (prismParts) log(`The Golden Apex splits the blow into light: ${prismParts.map((part, index) => `${part} ${prismElements[index]}`).join(", ")}.`);
    let amount = prismParts ? prismParts.reduce((sum, part) => sum + part, 0) : worldSaver || unreducible ? Math.floor(Math.max(0, markedPower) * rules.criticalMultiplier(critical, criticalBonusPercent)) : Math.floor(rules.damage(markedPower, spell ? 0 : effectiveArmor * (1 - armorPierce / 100), resistance(target, element, spell), critical, criticalBonusPercent) * incomingDamageFactor(target, attacker, attackKind, element));
    if (spell && !worldSaver && !unreducible) {
      for (const effect of passiveEffectsOf(target).filter(effect => effect.type === "incoming-spell-extra-reduction-chance")) {
        if (rules.roll(toNumber(effect.chance, 0))) amount = Math.floor(amount * (1 - Math.max(0, toNumber(effect.amount, 0)) / 100));
      }
    }
    if (amount > 0 && !worldSaver && !unreducible && attacker && attacker.team !== target.team && (attackKind === "ranged" || attackKind === "spell")) {
      const rune = consumeTrap(target, "trap-spell-rune");
      if (rune) {
        const reduction = rules.runeReduction(amount, rune.amount);
        amount = reduction.damage;
        const reflected = directDamage(attacker, trapDamageAgainst(rune, attacker, reduction.prevented), true, "physical", target);
        log(`${rune.name} prevents ${reduction.prevented} damage and deals ${reflected} damage to ${actorLabel(attacker)}.`);
      }
    }
    stageHit = { critical, element };
    try { amount = applyCombatDamage(target, amount, attacker); } finally { stageHit = null; }
    if (firstLight && amount > 0) { attacker.firstLightSpent = true; attacker.firstLightAction = battle.actionSerial; }
    if (amount > 0 && attacker && attacker.team !== target.team) {
      // Stormcatcher Coil: an enemy's spell that damages the wearer restores Mana.
      const stormcatcher = spell ? artifactAmount(target, "spell-hit-mana-percent") : 0;
      if (stormcatcher && hp(target) > 0) {
        const maximum = values(target).maxMana, restored = Math.min(maximum - mana(target), Math.floor(maximum * stormcatcher / 100));
        if (restored > 0) { setMana(target, mana(target) + restored); log(`${actorLabel(target)} catches ${restored} Mana from the spell.`); }
      }
      // The Hexbane Coat (port task 5, M26): 25% of an enemy spell's damage strikes its caster back in the spell's element,
      // its defenses applying; a reflection is never reflected again.
      const hexbane = spell && !reflectingSpell && hp(attacker) > 0 ? withActor(target, () => equippedDefenseItems().flatMap(item => item.effects || [])
        .filter(effect => effect.kind === "spell-reflect-percent").reduce((best, effect) => Math.max(best, toNumber(effect.amount, 0)), 0), battle) : 0;
      if (hexbane > 0) {
        reflectingSpell = true;
        try {
          const back = hurt(attacker, amount * hexbane / 100, element, false, true, target, "spell");
          if (back) log(`${actorLabel(target)}'s Hexbane Coat turns ${back} damage back on ${actorLabel(attacker)}.`);
        } finally { reflectingSpell = false; }
      }
      // Bloodmoon Chalice: weapons and spells heal their wearer for a share.
      const chalice = ["melee", "ranged", "spell"].includes(attackKind) ? artifactAmount(attacker, "damage-lifesteal-percent") : 0;
      if (chalice && hp(attacker) > 0 && !healingDisabled(attacker)) {
        const healed = healHp(attacker, Math.floor(amount * chalice / 100), battle, attacker);
        if (healed) log(`${actorLabel(attacker)} drinks ${attacker.team === "player" ? healed + " " : ""}HP through the Bloodmoon Chalice.`);
      }
      // Forked Lightning Rod: remember each spell target's damage for the strike after the cast (A31).
      if (attackKind === "spell" && attacker.forkHits && !chainStrikeDepth) attacker.forkHits.set(target.id, Math.max(attacker.forkHits.get(target.id) || 0, amount));
    }
    stageDefenseItems(target, attacker, element, spell, amount);
    restoreManaFromElementalArmor(target, element, amount);
    if (amount > 0 && hp(target) > 0 && spell && spellTier > 0 && elementList(element).some(candidate => SPELL_ELEMENTS.includes(candidate) && candidate !== "basic")) {
      for (const item of withActor(target, equippedDefenseItems)) for (const effect of item.effects || []) {
        if (effect.kind !== "all-stats-on-nonbasic-spell-damage-per-tier") continue;
        const key = `equipment:${item.id}:transformer`;
        const previous = target.effects.find(active => String(active.key || "").startsWith(key + ":") && active.expiresTurn > target.turns);
        const bonus = Math.max(toNumber(previous?.amount, 0), effect.amount * spellTier);
        applyTimedEffects(target, key, item.name, [{ type: "all-stats-percent", amount: bonus, durationTurns: effect.durationTurns }]);
        stageEquipment(target, item);
        log(`${actorLabel(target)} gains +${bonus}% basic stats from ${item.name} for ${effect.durationTurns} turns.`);
      }
    }
    if (amount > 0 && attacker && attacker.team !== target.team) {
      const lastElement = elementList(element).find(candidate => SPELL_ELEMENTS.includes(candidate));
      if (lastElement) target.lastDamageElement = lastElement;
    }
    if (amount > 0 && spell && attacker && attacker.team !== target.team) {
      const elemental = elementList(element).some(key => !["physical", "basic", "holy", "dark", "nature", "blood", "death"].includes(key));
      if (elemental) for (const effect of passiveEffectsOf(target).filter(effect => effect.type === "mana-from-elemental-spell-damage")) setMana(target, Math.min(values(target).maxMana, mana(target) + Math.floor(amount * toNumber(effect.amount, 0) / 100)));
      for (const effect of passiveEffectsOf(target).filter(effect => effect.type === "reflect-spell-damage-percent")) directDamage(attacker, Math.floor(amount * toNumber(effect.amount, 0) / 100), false, element, target);
    }
    if (amount > 0) target.damageTaken = Math.max(0, toNumber(target.damageTaken, 0)) + amount;
    if (amount > 0) {
      if (worldSaver) {
        skillStates(attacker)[worldSaver.key].active = false;
        applyTimedEffects(attacker, worldSaver.key + ":protection", worldSaver.name, [{ type: "team-incoming-damage-percent", amount: -35, durationTurns: 4 }]);
        log(`${actorLabel(attacker)} shields their team with ${worldSaver.name} for 4 turns.`);
      }
      if (worldEnder) {
        skillStates(attacker)[worldEnder.key].active = false;
        const burned = Math.min(mana(target), Math.max(0, Math.floor(amount * toNumber(worldEnder.effect.manaBurnPercent, 0) / 100)));
        setMana(target, mana(target) - burned);
        if (burned) log(`${worldEnder.name} burns ${burned} Mana from ${actorLabel(target)}.`);
      }
      if (tacticalMark?.turnAdvance && attacker && ["melee", "ranged", "spell"].includes(attackKind)) {
        for (const ally of living().filter(candidate => candidate.team === attacker.team && candidate.id !== attacker.id)) ally.counter = Math.min(999, ally.counter + tacticalMark.turnAdvance);
        log(`${actorLabel(attacker)} advances allied turns with Focus Fire.`);
      }
      consumeIncomingHitEffects(target, attackKind);
      target.damageHistory ||= [];
      for (const activeElement of elementList(element)) target.damageHistory.push({ element: activeElement, turn: target.turns });
      target.damageHistory = target.damageHistory.slice(-30);
      if (["melee", "ranged"].includes(attackKind)) {
        for (const effect of passiveEffectsOf(target).filter(entry => entry.type === "armor-percent-on-weapon-hit")) {
          const key = `passive:${effect.type}`;
          const existing = target.effects.find(entry => entry.key === key);
          const nextAmount = Math.min(Math.max(0, toNumber(effect.maximum, 100)), toNumber(existing?.amount, 0) + Math.max(0, toNumber(effect.amount, 0)));
          if (existing) existing.amount = nextAmount;
          else target.effects.push({ key, name: "Ramping Defense", type: "defense-percent", target: "armor", amount: nextAmount, expiresTurn: Number.MAX_SAFE_INTEGER });
        }
      }
    }
    if (hp(target) <= 0 && !tryRaceDeathSave(target, element)) {
      logFall(target);
      dropStandards(target);
      notifySummonLost(target);
      recordCombatKill(attacker, target);
    } else triggerLowHealthPassives(target);
    if (attacker && attacker.team !== target.team && attackKind === "spell") triggerRetaliationTrap(target, attacker, "trap-sentry");
    // Any spell hit counts, even one a barrier soaks entirely (R29).
    if (attacker && attacker.team !== target.team && attackKind === "spell") triggerControlTraps(target, attacker, "spell");
    mirrorScales(target, attacker, amount, spell, attackKind);
    // A dream law may turn part of a spell's damage back on its caster.
    const rebound = spell && attacker && attacker.team !== target.team && amount > 0 && !reboundDepth && hp(attacker) > 0 ? lawAmount(attacker, "spell-rebound-percent") : 0;
    if (rebound > 0) {
      reboundDepth += 1;
      try {
        const back = directDamage(attacker, Math.max(1, Math.floor(amount * rebound / 100)), false, element, null);
        if (back > 0) log(`The words rebound: ${actorLabel(attacker)} takes ${back} damage.`);
      } finally { reboundDepth -= 1; }
    }
    syncEnemyCount();
    if (hp(target) <= 0 && attacker && ["melee", "ranged", "spell"].includes(attackKind)) refreshHunterMark(attacker, target);
    return amount;
  }
  function refreshHunterMark(attacker, target) {
    const mark = target.effects.find(effect => effect.type === "hunter-mark" && effect.ownerId === attacker.id);
    if (!mark) return;
    const state = skillStates(attacker)[mark.skillKey];
    if (state) { state.active = false; state.lastUsedDay = ""; }
    target.effects = target.effects.filter(effect => effect !== mark);
    log(`${mark.name} refreshes after ${actorLabel(target)} is defeated.`);
  }
  function weaponAttackValues(actor, weapon) {
    if (!weapon.unarmed) return withActor(actor, () => {
      const classEffects = calculateClassEffects();
      const alwaysCrit = !classEffects.weaponCritDisabled && classEffects.skills.some((skill) => activeSkills[classSkillKey(skill)]?.active && weaponMatches(weapon, skill.requiredWeaponType) && skill.effects.some((effect) => effect.type === "weapon-crit-always"));
      const timedHit = actor.effects.filter(effect => effect.type === "weapon-hit-flat").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
      return {
        hit: calculateWeaponHitChance(weapon) + timedHit,
        crit: calculateWeaponCritChance(weapon),
        critRating: weaponCritRating(weapon).total,
        power: Math.floor(calculateItemPower(weapon, (weapon.effects || []).some(effect => effect.kind === "weapon-power-from-armor-percent") ? values(actor) : currentStats())),
        alwaysCrit,
        alwaysHit: classEffects.weaponHitAlways || (weapon.effects || []).some(effect => effect.kind === "always-hit")
      };
    });
    const actorValues = values(actor);
    const classEffects = withActor(actor, calculateClassEffects);
    const alwaysCrit = !classEffects.weaponCritDisabled && classEffects.skills.some((skill) => skillStates(actor)[classSkillKey(skill)]?.active && weaponMatches(weapon, skill.requiredWeaponType) && skill.effects.some((effect) => effect.type === "weapon-crit-always"));
    const extraScaling = classEffects.unarmedScalingModifiers.reduce((sum, modifier) => sum + toNumber(actorValues[modifier.stat], 0) * modifier.amount, 0);
    const timedHit = actor.effects.filter(effect => effect.type === "weapon-hit-flat").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
    const hit = classEffects.weaponHitAlways ? 100 : Math.floor(Math.max(70, 30 + actorValues.strength + classEffects.unarmedHitFlat + timedHit) * (1 + classEffects.unarmedHitPercent / 100));
    return {
      hit,
      crit: classEffects.weaponCritDisabled ? 0 : rules.weaponCritChance(actorValues.luck, classEffects.weaponCritFlat + toNumber(classEffects.weaponCritFlatByType.melee, 0) + toNumber(classEffects.weaponCritFlatByType.unarmed, 0) + withActor(actor, () => calculateArtifactEffects().weaponCritFlat)),
      critRating: 5 + Math.max(0, toNumber(actorValues.luck, 0)) * 0.15 + classEffects.weaponCritFlat + toNumber(classEffects.weaponCritFlatByType.melee, 0) + toNumber(classEffects.weaponCritFlatByType.unarmed, 0) + withActor(actor, () => calculateArtifactEffects().weaponCritFlat),
      power: Math.max(1, Math.floor((1 + classEffects.unarmedPowerFlat + (0.3 * actorValues.strength) + extraScaling) * (1 + (classEffects.unarmedPowerPercent + classEffects.weaponPowerPercent + toNumber(classEffects.weaponPowerPercentByType.melee, 0)) / 100) * classEffects.weaponPowerMultiplier * 0.5)),
      alwaysCrit,
      alwaysHit: classEffects.weaponHitAlways
    };
  }
  function weaponMatches(weapon, requirement) {
    if (!requirement || requirement === "all") return true;
    if (requirement === "unarmed") return weapon.unarmed === true || weapon.unarmedBonuses === true;
    if (requirement === "one-handed") return weapon.handed === 1 && !weapon.unarmed;
    const type = normalizeWeaponType(weapon.type);
    if (requirement === "bow") return weapon.isBow === true || type === "ranged" && /bow/i.test(weapon.name) || ["bow", "shortbow", "longbow"].includes(type);
    return type === normalizeWeaponType(requirement);
  }
  function fencerReady(actor) {
    const slots = weaponSlotsOf(actor);
    return slots.primary?.handed === 1 && /sword|rapier/i.test(slots.primary.name) && !slots.offHand;
  }
  function applyAttackHitEffects(actor, target, weapon, amount, critical, activeAttackSkills) {
    for (const skill of activeAttackSkills) for (const effect of skill.onHitEffects || []) {
      if (!weaponMatches(weapon, effect.weaponType)) continue;
      if (effect.type === "lifesteal-percent") {
        if (healingDisabled(actor)) continue;
        const gain = Math.floor(amount * Math.max(0, toNumber(effect.amount, 0)) / 100);
        healHp(actor, gain, battle, actor);
      } else if (effect.type === "target-all-stats-percent") {
        applyTimedEffects(target, classSkillKey(skill) + ":target", skill.name, [{ type: "all-stats-percent", amount: effect.amount, durationTurns: effect.durationTurns }]);
      } else if (effect.type === "blood-supplies-flat") {
        setActorBloodSupplies(actor, actorBloodSupplies(actor) + toInteger(effect.amount, 0));
      } else if (effect.type === "bonus-damage-max-mana-percent") {
        const bonusElements = effect.elements?.length ? effect.elements : effect.element || "physical";
        const bonus = hurt(target, values(actor).maxMana * Math.max(0, toNumber(effect.amount, 0)) / 100, bonusElements, false, false, actor, "bonus");
        log(`${skill.name} deals ${bonus} additional ${elementLabel(bonusElements)} damage.`);
      } else if ((effect.type === "stun" || effect.type === "stun-chance" && rules.roll(toNumber(effect.amount, 0))) && !debuffImmune(target) && !withActor(target, () => calculateClassEffects().stunImmune)) {
        target.effects = target.effects.filter(existing => existing.type !== "stun");
        target.effects.push({ key: classSkillKey(skill) + ":stun", name: skill.name, type: "stun", amount: 1, expiresTurn: target.turns + Math.max(1, toInteger(effect.durationTurns, 1)) });
      } else if (effect.type === "steal-equipped-weapon-on-crit" && critical) {
        const targetEquipment = target.id === "player" ? equipment : target.character.equipment;
        const targetInventory = target.id === "player" ? inventory : target.character.inventory;
        const stolenId = targetEquipment.primaryHand && getItem(targetEquipment.primaryHand)?.category === "weapon" ? targetEquipment.primaryHand : targetEquipment.offHand && getItem(targetEquipment.offHand)?.category === "weapon" ? targetEquipment.offHand : null;
        if (stolenId && itemCanDrop(stolenId)) {
          const index = targetInventory.indexOf(stolenId);
          if (index >= 0) targetInventory.splice(index, 1);
          for (const slot of ["primaryHand", "offHand"]) if (targetEquipment[slot] === stolenId) targetEquipment[slot] = null;
          (actor.id === "player" ? inventory : actor.character.inventory).push(stolenId);
          log(`${actorLabel(actor)} steals ${getItem(stolenId)?.name || "a weapon"} from ${actorLabel(target)}.`);
        }
      }
    }
  }
  function applyNewClassWeaponEffects(actor, target, weapon, amount, activeSkillsForAttack) {
    if (hp(target) <= 0) return;
    const passives = passiveEffectsOf(actor);
    const bonuses = withActor(actor, calculateClassEffects);
    const toxin = passives.filter(effect => effect.type === "poison-on-hit").reduce((sum, effect) => sum + toNumber(effect.damage, 0), bonuses.poisonOnHitDamage);
    const toxinPercent = bonuses.poisonOnHitPercentMaxHp;
    const coating = activeSkillsForAttack.flatMap(skill => skill.effects).find(effect => effect.type === "enhanced-poison-on-hit");
    if ((toxin || toxinPercent || coating) && !debuffImmune(target)) {
      const damage = toxin + toNumber(coating?.damage, 0);
      const percentMaxHp = toxinPercent + toNumber(coating?.percentMaxHp, 0);
      target.effects = target.effects.filter(effect => effect.type !== "class-poison" || effect.ownerId !== actor.id);
      target.effects.push({ key: `poison:${actor.id}`, name: "Poisoned", type: "class-poison", ownerId: actor.id, amount: damage, percentMaxHp, healingPenalty: bonuses.poisonHealingReductionPercent, expiresTurn: target.turns + Math.max(1, toInteger(coating?.durationTurns, 2)) });
      if (coating?.hitPenalty) applyTimedEffects(target, "noxious-coating", "Noxious Coating", [{ type: "weapon-hit-flat", amount: -Math.abs(coating.hitPenalty), durationTurns: coating.durationTurns || 2 }]);
      if (coating?.hitPercentPenalty) applyTimedEffects(target, "noxious-coating", "Noxious Coating", [{ type: "weapon-hit-percent", amount: -Math.abs(coating.hitPercentPenalty), durationTurns: coating.durationTurns || 2 }]);
    }
    for (const effect of passives.filter(effect => effect.type === "hex-on-hit")) if (!debuffImmune(target)) applyTimedEffects(target, "hexblade", "Cursed Edge", [effect.armorPercent != null
      ? { type: "defense-percent", target: "armor", amount: -Math.abs(toNumber(effect.armorPercent, 0)), durationTurns: effect.durationTurns || 2 }
      : { type: "defense-flat", target: "armor", amount: -Math.abs(toNumber(effect.armor, 0)), durationTurns: effect.durationTurns || 2 }]);
    if (passives.some(effect => effect.type === "dispel-on-hit")) {
      const removable = target.effects.find(effect => !effect.unremovable && toNumber(effect.amount, 0) > 0 && ["stat-percent", "all-stats-percent", "defense-flat", "defense-percent", "spell-power-percent", "weapon-power-percent", "incoming-damage-percent"].includes(effect.type));
      if (removable) { target.effects = target.effects.filter(effect => effect !== removable); log(`${actorLabel(actor)} unravels ${removable.name}.`); }
    }
    const nullSkill = activeSkillsForAttack.flatMap(skill => skill.effects).find(effect => effect.type === "null-next-spell-percent");
    if (nullSkill) target.effects.push({ key: `nullblade:${actor.id}`, name: "Null Strike", type: "next-spell-penalty", amount: Math.abs(toNumber(nullSkill.amount, 0)), expiresTurn: Number.MAX_SAFE_INTEGER });
    const echoPassive = actor.echoTurn !== actor.turns ? bonuses.echoWeaponHitPercent : 0;
    const echoSkill = activeSkillsForAttack.flatMap(skill => skill.effects).filter(effect => effect.type === "echo-next-attack-percent").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
    if (amount > 0 && (echoSkill || echoPassive)) {
      actor.echoTurn = actor.turns;
      const echoed = hurt(target, amount * (echoPassive + echoSkill) / 100, weapon.element || "physical", false, false, actor, normalizeWeaponType(weapon.type));
      if (echoed) log(`${actorLabel(actor)}'s spectral echo deals ${echoed} damage.`);
    }
  }
  function applyMeleeSpellEffects(actor, target, effects) {
    for (const effect of effects) {
      const damage = hurt(target, effect.amount, effect.element || "basic", false, true, actor, "melee", effect.armorPiercePercent);
      log(`${effect.name} deals ${damage} additional ${effect.element || "basic"} damage to ${actorLabel(target)}.`);
      if (effect.lifeSteal && !healingDisabled(actor)) {
        const healing = healHp(actor, Math.max(0, Math.floor(damage * effect.lifeSteal)), battle, actor);
        log(`${actorLabel(actor)} drains ${actor.team === "player" ? healing + " " : ""}HP.`);
      }
      if (effect.statDebuff && hp(target) > 0) {
        applyTimedEffects(target, effect.key + ":debuff", effect.name, [{ type: "stat-percent", ...effect.statDebuff }]);
      }
      if (effect.stunChance && hp(target) > 0 && rules.roll(effect.stunChance) && !debuffImmune(target) && !withActor(target, () => calculateClassEffects().stunImmune)) {
        target.effects = target.effects.filter(existing => existing.type !== "stun");
        target.effects.push({ key: effect.key + ":stun", name: effect.name, type: "stun", amount: 1, expiresTurn: target.turns + Math.max(1, toInteger(effect.stunDurationTurns, 1)) });
        log(`${effect.name} stuns ${actorLabel(target)}.`);
      }
    }
  }
  function redirectedTauntTarget(actor, target) {
    if (!target || target.team === actor.team) return target;
    const taunters = opponents(actor).filter(candidate => candidate.id !== target.id && candidate.effects.some(effect => effect.type === "taunt-chance"));
    const taunter = taunters.sort((left, right) => values(right).armor - values(left).armor)[0];
    if (!taunter) return target;
    const chance = taunter.effects.filter(effect => effect.type === "taunt-chance").reduce((maximum, effect) => Math.max(maximum, toNumber(effect.amount, 0)), 0);
    if (!rules.roll(chance)) return target;
    log(`${actorLabel(taunter)} draws ${actorLabel(actor)}'s attention.`);
    return taunter;
  }
  function redirectedGuardTarget(actor, target, attackKind, area = false) {
    target = redirectedTauntTarget(actor, target);
    if (!target || target.team === actor.team || !["melee", "ranged", "spell"].includes(normalizeWeaponType(attackKind))) return target;
    const eligible = living().filter(candidate => candidate.team === target.team && candidate.id !== target.id && opponents(actor).includes(candidate));
    const legion = phylacteryGuardFor(target, eligible, area);
    if (legion) {
      log(`${actorLabel(legion)} throws itself before the blow meant for ${actorLabel(target)}.`);
      return legion;
    }
    const ward = target.effects.find(effect => effect.type === "guardian-ward" && effect.expiresTurn > target.turns);
    const swornGuardian = ward ? eligible.find(candidate => candidate.id === ward.guardianId) : null;
    if (swornGuardian) {
      log(`${actorLabel(swornGuardian)} blocks the attack meant for ${actorLabel(target)} with ${ward.name}.`);
      return swornGuardian;
    }
    const interceptors = eligible.map(candidate => ({
      candidate,
      chance: passiveEffectsOf(candidate).filter(effect => effect.type === "intercept-ally-attack-chance").reduce((best, effect) => Math.max(best, toNumber(effect.amount, 0)), 0)
    })).filter(entry => entry.chance > 0).sort((left, right) => right.chance - left.chance || values(right.candidate).armor - values(left.candidate).armor);
    const interceptor = interceptors[0];
    if (!interceptor || !rules.roll(interceptor.chance)) return target;
    log(`${actorLabel(interceptor.candidate)} intercepts the attack meant for ${actorLabel(target)}.`);
    return interceptor.candidate;
  }
  function largerHpWeaponBonus(actor, target, weapon) {
    const ownMaximum = actor.resourceMax?.hp ?? values(actor).maxHp;
    const targetMaximum = target.resourceMax?.hp ?? values(target).maxHp;
    const larger = Math.max(0, (targetMaximum / Math.max(1, ownMaximum) - 1) * 100);
    return (weapon.effects || []).filter(effect => effect.kind === "damage-and-pierce-vs-larger-max-hp").reduce((bonus, effect) => {
      // A growing bonus (Giantslayer) adds a step for every "stepPercent" more maximum HP, up to
      // "maxAmount"; the fixed form needs "thresholdPercent" more and gives its whole amount.
      const steps = effect.stepPercent > 0 ? Math.floor(larger / effect.stepPercent + 1e-9) : larger + 1e-9 >= toNumber(effect.thresholdPercent, 0) ? 1 : 0;
      if (steps < 1) return bonus;
      const damage = effect.stepPercent > 0 ? Math.min(toNumber(effect.maxAmount, Infinity), steps * effect.amount) : effect.amount;
      return { damagePercent: bonus.damagePercent + damage, armorPiercePercent: bonus.armorPiercePercent + effect.armorPiercePercent };
    }, { damagePercent: 0, armorPiercePercent: 0 });
  }
  function applyUniqueWeaponHitEffects(actor, target, weapon, dealt, damageMultiplier) {
    const melee = normalizeWeaponType(weapon.type) === "melee";
    if (melee) {
      const protection = withActor(actor, () => equippedArtifactItems().flatMap(item => item.effects || []).filter(effect => effect.kind === "melee-hit-guard-per-rarity"));
      const rank = Math.max(0, ["common", "uncommon", "rare", "epic", "legendary", "masterwork"].indexOf(weapon.rarity));
      const amount = protection.reduce((best, effect) => Math.max(best, Math.min(100, effect.amount * (rank + 1))), 0);
      if (amount) {
        actor.effects = actor.effects.filter(effect => effect.key !== "wraithguard:protection");
        actor.effects.push({ key: "wraithguard:protection", name: "Wraithguard", type: "incoming-attack-damage-percent", amount: -amount, expiresTurn: actor.turns + 1 });
      }
    }
    if (!melee && normalizeWeaponType(weapon.type) === "ranged" && dealt > 0) {
      const splash = rangedSplashPercent(actor);
      if (splash) for (const other of opponents(actor).filter(other => other.id !== target.id)) {
        const echoed = directDamage(other, dealt * splash / 100, true, weapon.element || "physical", actor);
        if (echoed) log(`Arrowtip Rain deals ${echoed} damage to ${actorLabel(other)}.`);
      }
    }
    // Every enemy this attack damaged, the splash included (Kugelblitz marks them all).
    const struck = new Set(dealt > 0 ? [target.id] : []);
    for (const effect of weapon.effects || []) {
      if (effect.kind === "mark-on-hit" && hp(target) > 0 && rules.roll(effect.chance)) {
        stageEquipment(actor, weapon, { targetId: target.id });
        applyTimedEffects(target, `equipment:${weapon.id}:mark`, "Beowulf's Mark", [{ type: "incoming-attack-damage-percent", attackKinds: ["melee", "ranged"], amount: effect.amount, durationTurns: effect.durationTurns }]);
        log(`${actorLabel(target)} is marked by ${weapon.name} for ${effect.durationTurns} turns.`);
      } else if (effect.kind === "bonus-damage-from-armor-percent" && dealt > 0 && hp(target) > 0) {
        stageEquipment(actor, weapon, { targetId: target.id });
        const bonus = hurt(target, values(actor).armor * effect.amount / 100 * damageMultiplier, "physical", false, false, actor, "bonus");
        log(`${weapon.name} deals ${bonus} additional damage from Armor.`);
      } else if (effect.kind === "unreducible-cleave-percent" && dealt > 0) {
        stageEquipment(actor, weapon, { targetId: target.id, cleave: true });
        for (const other of hostiles(actor).filter(other => other.id !== target.id)) {
          const echoed = directDamage(other, dealt * effect.amount / 100, false, "physical", actor, true);
          if (echoed) log(`${weapon.name} echoes ${echoed} unreducible damage to ${actorLabel(other)}.`);
        }
      } else if (effect.kind === "cleave-percent" && dealt > 0) {
        stageEquipment(actor, weapon, { targetId: target.id, cleave: true });
        for (const other of [...hostiles(actor)].filter(other => other.id !== target.id)) {
          const echoed = hurt(other, dealt * effect.amount / 100, weapon.element || "physical", false, false, actor, "bonus");
          if (echoed) { struck.add(other.id); log(`${weapon.name} splashes ${echoed} damage to ${actorLabel(other)}.`); }
        }
      } else if (effect.kind === "stun-on-hit-chance" && dealt > 0 && hp(target) > 0 && rules.roll(effect.chance)) {
        // First Comet: the game's stun rules (stun immunity holds), 1 turn.
        if (debuffImmune(target) || withActor(target, () => calculateClassEffects().stunImmune)) { log(`${actorLabel(target)} resists the stun from ${weapon.name}.`); continue; }
        stageEquipment(actor, weapon, { targetId: target.id });
        target.effects = target.effects.filter(existing => existing.type !== "stun");
        target.effects.push({ key: `equipment:${weapon.id}:stun`, name: "Stunned", type: "stun", amount: 1, expiresTurn: target.turns + 1 });
        log(`${weapon.name} stuns ${actorLabel(target)}.`);
      } else if (effect.kind === "spell-cost-mark-on-damage" && struck.size) {
        // Kugelblitz: every enemy it damaged pays more Mana for spells until the end of its next turn (refreshed, not stacked).
        const marked = [...struck].map(id => actorById(id)).filter(enemy => enemy && hp(enemy) > 0);
        for (const enemy of marked) applyTimedEffects(enemy, `equipment:${weapon.id}:spell-cost-mark`, `${weapon.name}'s Mark`, [{ type: "spell-cost-percent", amount: -effect.amount, durationTurns: effect.durationTurns }]);
        if (marked.length) log(`${weapon.name} marks ${marked.map(actorLabel).join(", ")}: spells cost ${effect.amount}% more Mana.`);
      } else if (effect.kind === "party-mana-drain-on-hit-percent") {
        const drained = Math.min(mana(target), Math.floor(mana(target) * effect.amount / 100));
        if (!drained) continue;
        stageEquipment(actor, weapon, { targetId: target.id });
        const party = living().filter(ally => ally.team === actor.team && !ally.summoned);
        if (!party.length) party.push(actor);
        setMana(target, mana(target) - drained);
        const share = Math.floor(drained / party.length), remainder = drained % party.length;
        party.forEach((ally, index) => setMana(ally, Math.min(values(ally).maxMana, mana(ally) + share + Number(index < remainder))));
        log(`${weapon.name} drains ${drained} Mana and shares it among the party.`);
      }
    }
  }
  // Abyss armor returns part of a melee hit to the attacker; Vampiric armor heals its wearer.
  function applyArmorHitEffects(actor, target, weaponType, amount) {
    if (amount <= 0) return;
    const thorns = weaponType === "melee" ? equippedArmorTotal(target, "thorns") : 0;
    if (thorns > 0 && hp(actor) > 0) {
      const returned = directDamage(actor, amount * thorns / 100, true, "physical", target);
      if (returned) log(`${actorLabel(actor)} takes ${returned} damage from ${actorLabel(target)}'s spiked armor.`);
    }
    const lifesteal = equippedArmorTotal(actor, "lifesteal");
    if (lifesteal > 0 && hp(actor) > 0) {
      const healed = healHp(actor, amount * lifesteal / 100, battle, actor);
      if (healed) log(`${actorLabel(actor)} drinks ${healed} HP through Vampiric armor.`);
    }
    applyArtifactWeaponHit(actor, target, weaponType, amount);
  }
  // The artifacts that act on a weapon hit (port task 4): Thornknot Brooch on the defender; Leech Hook, Vampire's
  // Fang and the on-hit effects on the attacker. Debuff and stun immunity hold.
  function applyArtifactWeaponHit(actor, target, weaponType, amount) {
    const thorns = weaponType === "melee" ? artifactEffectsOf(target, "melee-thorns-percent") : [];
    for (const effect of thorns) {
      if (hp(actor) <= 0) break;
      const returned = directDamage(actor, amount * effect.amount / 100, true, "physical", target);
      if (returned) log(`${actorLabel(actor)} takes ${returned} damage from ${actorLabel(target)}'s ${effect.itemName}.`);
    }
    const leech = artifactAmount(actor, "weapon-lifesteal-percent");
    if (leech && hp(actor) > 0 && !healingDisabled(actor)) {
      const healed = healHp(actor, Math.floor(amount * leech / 100), battle, actor);
      if (healed) log(`${actorLabel(actor)} drains ${actor.team === "player" ? healed + " " : ""}HP through ${artifactEffectsOf(actor, "weapon-lifesteal-percent")[0].itemName}.`);
    }
    if (hp(target) <= 0) return;
    const immune = debuffImmune(target);
    for (const effect of artifactEffectsOf(actor, "on-hit-burn")) {
      if (immune || !rules.roll(effect.chance)) continue;
      target.effects = target.effects.filter(entry => entry.key !== `artifact-burn:${actor.id}`);
      target.effects.push({ key: `artifact-burn:${actor.id}`, name: effect.itemName, type: "turn-start-max-hp-loss-percent", amount: effect.amount, expiresTurn: target.turns + effect.durationTurns });
      log(`${effect.itemName} sets ${actorLabel(target)} burning.`);
    }
    for (const effect of artifactEffectsOf(actor, "on-hit-slow")) {
      if (immune || !rules.roll(effect.chance)) continue;
      applyTimedEffects(target, `artifact-slow:${actor.id}`, effect.itemName, [{ type: "stat-percent", target: "speed", amount: -Math.abs(effect.amount), durationTurns: effect.durationTurns }]);
      log(`${effect.itemName} slows ${actorLabel(target)}.`);
    }
    for (const effect of artifactEffectsOf(actor, "on-hit-armor-shred")) {
      if (immune) continue;
      const key = `artifact-shred:${actor.id}`, previous = target.effects.find(entry => entry.key === key);
      const stacks = Math.min(Math.max(1, toInteger(effect.maxStacks, 3)), toInteger(previous?.stacks, 0) + 1);
      target.effects = target.effects.filter(entry => entry !== previous);
      target.effects.push({ key, name: effect.itemName, type: "defense-percent", target: "armor", amount: -Math.abs(effect.amount) * stacks, stacks, expiresTurn: target.turns + effect.durationTurns });
      log(`${effect.itemName} cracks ${actorLabel(target)}'s Armor (${stacks}×).`);
    }
    for (const effect of artifactEffectsOf(actor, "on-hit-stun-chance")) {
      if (!rules.roll(effect.amount)) continue;
      if (immune || withActor(target, () => calculateClassEffects().stunImmune)) { log(`${actorLabel(target)} resists the stun from ${effect.itemName}.`); continue; }
      target.effects = target.effects.filter(existing => existing.type !== "stun");
      target.effects.push({ key: `artifact-stun:${actor.id}`, name: "Stunned", type: "stun", amount: 1, expiresTurn: target.turns + 1 });
      log(`${effect.itemName} stuns ${actorLabel(target)}.`);
    }
    for (const effect of artifactEffectsOf(actor, "on-hit-poison")) {
      if (immune) continue;
      // Refreshed, not stacked: one poison per wearer.
      target.effects = target.effects.filter(entry => entry.key !== `artifact-poison:${actor.id}`);
      target.effects.push({ key: `artifact-poison:${actor.id}`, name: effect.itemName, type: "class-poison", ownerId: actor.id, amount: 0, percentMaxHp: effect.amount, healingPenalty: 0, expiresTurn: target.turns + effect.durationTurns });
      log(`${effect.itemName} poisons ${actorLabel(target)}.`);
    }
  }
  function rangedSplashPercent(actor) {
    return cacheCalculation("ranged-splash-percent", () => withActor(actor, () => equippedArtifactItems().flatMap(item => item.effects || []).filter(effect => effect.kind === "ranged-splash-percent").reduce((total, effect) => total + effect.amount, 0)), actor);
  }
  function offHandArmorPierce(weapon, offHand) {
    return offHand ? (weapon.effects || []).filter(effect => effect.kind === "offhand-armor-pierce-percent").reduce((total, effect) => total + effect.amount, 0) : 0;
  }
  function weaponCritChanceAgainst(target, weapon, baseChance, rating = null) {
    const defense = equippedArmorTotal(target, "critDefense");
    if (defense > 0 && rating != null && baseChance > 0) baseChance = rules.critChanceFromRating(rating - defense);
    const bonus = (weapon.effects || []).filter(effect => effect.kind === "weapon-crit-percent-vs-class" && effect.classIds.some(id => hasClass(target, id)))
      .reduce((sum, effect) => sum + effect.amount, 0);
    const points = critTargetEffects(weapon, target).reduce((sum, effect) => sum + toNumber(effect.chance, 0), 0);
    return Math.min(100, Math.max(0, Math.floor(baseChance * (1 + bonus / 100)) + (baseChance > 0 ? points : 0)));
  }
  function strike(actor, target, weapon, damageMultiplier = 1, offHand = false, hitPercent = 0) {
    if (!offHand && !equipmentActionDamage.has(actor.id) && equipmentChargeBonuses(actor).length) {
      return withEquipmentCharge(actor, () => strike(actor, target, weapon, damageMultiplier, offHand, hitPercent));
    }
    if (onStage(actor)) stage("strike", { actorId: actor.id, targetId: target.id, weaponId: weapon.id, weaponKind: normalizeWeaponType(weapon.type), ranged: normalizeWeaponType(weapon.type) === "ranged", unarmed: Boolean(weapon.unarmed), offHand, element: weapon.elementShare?.element || weapon.element || null });
    const targetEvasion = values(target).evasion;
    const attackValues = weaponAttackValues(actor, weapon);
    const weaponType = normalizeWeaponType(weapon.type);
    // Moneyshot (port task 5, M16, M18): every attack with it counts, hits and misses; each fifth of a battle is boosted.
    const fifth = offHand ? null : (weapon.effects || []).find(effect => effect.kind === "fifth-attack-boost");
    if (fifth) actor.fifthAttackCount = Math.max(0, toInteger(actor.fifthAttackCount, 0)) + 1;
    const boosted = Boolean(fifth) && actor.fifthAttackCount % Math.max(1, toInteger(fifth.every, 5)) === 0;
    // Noble Hero (port task 5, M21): 25% less damage, and each hit strikes again for the same amount.
    const secondStrike = offHand ? null : (weapon.effects || []).find(effect => effect.kind === "second-strike");
    const mark = target.effects.find(effect => effect.type === "hunter-mark" && effect.ownerId === actor.id && skillStates(actor)[effect.skillKey]?.active);
    const targetHitEffects = withActor(target, () => calculateClassEffects().incomingWeaponHitPercentByType);
    const weatherHitPercent = actor.effects.filter(effect => effect.type === "weapon-hit-percent" && (!effect.weaponType || normalizeWeaponType(effect.weaponType) === weaponType)).reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
    const rangedHitPercent = weaponType === "ranged" ? target.effects.filter(effect => effect.type === "incoming-ranged-hit-percent").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0) : 0;
    const incomingHitPercent = toNumber(targetHitEffects.all, 0) + toNumber(targetHitEffects[weaponType], 0) + rangedHitPercent;
    const hit = rules.weaponHitChance((attackValues.hit + toNumber(mark?.hitFlat, 0)) * (1 + (weatherHitPercent + toNumber(mark?.hitPercent, 0) + hitPercent) / 100) * (1 + incomingHitPercent / 100), evasionAgainst(targetEvasion, weaponType));
    const critChance = weaponCritChanceAgainst(target, weapon, attackValues.crit, attackValues.critRating);
    const weaponLabel = `${weapon.name}${offHand ? " (off-hand)" : ""}`;
    const guaranteedHit = attackValues.alwaysHit || target.effects.some((effect) => effect.type === "incoming-weapon-hit-always");
    const incomingDamagePercent = target.effects.filter((effect) => effect.type === "incoming-weapon-damage-percent").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
    const firstTurnDamagePercent = (target.turns === 0 ? passiveEffectsOf(actor).filter(effect => effect.type === "weapon-damage-before-first-turn-percent").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0) : 0) + (!offHand && actor.turns === 1 && !actor.firstStrikeUsed ? passiveEffectsOf(actor).filter(effect => effect.type === "first-attack-damage-percent").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0) : 0);
    const activeAttackSkills = offHand ? [] : skillsOf(actor).filter((skill) => skill.durationAttacks && skillStates(actor)[classSkillKey(skill)]?.active && weaponMatches(weapon, skill.requiredWeaponType));
    const meleeSpellEffects = !offHand && weaponType === "melee" ? actor.effects.filter(effect => effect.type === "next-melee-attack") : [];
    const unarmedBonusEffects = weapon.unarmed || weapon.unarmedBonuses ? actor.effects.filter(effect => effect.type === "unarmed-bonus-damage") : [];
    const conditionalPower = activeAttackSkills.flatMap(skill => skill.effects || []).filter(effect =>
      effect.type === "weapon-power-percent-vs-race" && (effect.raceIds || [effect.raceId]).filter(Boolean).map(slugify).includes(actorRace(target)) ||
      effect.type === "weapon-power-percent-vs-class" && (effect.classIds || [effect.classId]).filter(Boolean).some(classId => hasClass(target, slugify(classId))) ||
      effect.type === "weapon-power-percent-by-untouched"
    ).reduce((sum, effect) => sum + (effect.type === "weapon-power-percent-by-untouched" ? untouchedBonus(actor, effect) : toNumber(effect.amount, 0)), 0);
    const actorPassives = passiveEffectsOf(actor);
    const passivePower = (actor.previousTurnAttackTargetIds || []).includes(target.id) ? actorPassives.filter(effect => effect.type === "repeat-target-damage-percent").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0) : 0;
    const slowerTargetPower = values(actor).speed > values(target).speed ? (weapon.effects || []).filter(effect => effect.kind === "damage-percent-vs-slower").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0) : 0;
    const largerHpBonus = largerHpWeaponBonus(actor, target, weapon);
    const starscale = (weapon.effects || []).find(effect => effect.kind === "bonus-basic-attack-percent");
    const landed = guaranteedHit || rules.roll(hit);
    if (landed) {
      const critical = attackValues.alwaysCrit || rules.critRoll(critChance, actor.critMisses);
      // Crit momentum: hits that could crit but did not raise the next hit's chance.
      if (!attackValues.alwaysCrit && critChance > 0) actor.critMisses = critical ? 0 : Math.max(0, toInteger(actor.critMisses, 0)) + 1;
      // Quasar Jet: each critical hit charges the next attack; a run of critical hits charges it further.
      const chain = (weapon.effects || []).find(effect => effect.kind === "critical-chain-damage-percent");
      const chainBonus = chain ? Math.min(toNumber(chain.maxAmount, Infinity), toInteger(actor.critChain, 0) * toNumber(chain.amount, 0)) : 0;
      if (chain) actor.critChain = critical ? Math.min(Math.ceil(toNumber(chain.maxAmount, 300) / Math.max(1, toNumber(chain.amount, 30))), toInteger(actor.critChain, 0) + 1) : 0;
      if (chainBonus > 0) log(`${weapon.name} releases its gathered light: +${chainBonus}% damage.`);
      let brawlerBonus = 0;
      if (weaponType === "melee") for (const effect of actorPassives.filter(entry => entry.type === "melee-random-bonus-damage")) {
        if (!rules.roll(toNumber(effect.chance, 0))) continue;
        const minimum = Math.max(0, toInteger(effect.minimum, 0));
        const maximum = Math.max(minimum, toInteger(effect.maximum, minimum));
        brawlerBonus += minimum + Math.floor(Math.random() * (maximum - minimum + 1));
      }
      const strikePower = (attackValues.power + brawlerBonus) * damageMultiplier * (1 + (incomingDamagePercent + firstTurnDamagePercent + conditionalPower + passivePower + slowerTargetPower + weaponRaceDamagePercent(weapon, target) + woundedTargetPercent(weapon, target) + largerHpBonus.damagePercent + chainBonus) / 100);
      const armorPierce = largerHpBonus.armorPiercePercent + toNumber(starscale?.armorPiercePercent, 0) + offHandArmorPierce(weapon, offHand) + toNumber(weapon.armorPierce, 0);
      // A boosted Moneyshot attack: +50% damage, +200% instead if it is a critical hit.
      const boost = (boosted ? 1 + toNumber(critical ? fifth.criticalAmount : fifth.amount, 0) / 100 : 1) * (secondStrike ? 1 - toNumber(secondStrike.amount, 0) / 100 : 1);
      const amount = withWeaponElement(weapon, () => hurt(target, strikePower * boost, strikeElement(actor, weapon), critical, false, actor, weaponType, armorPierce), target);
      log(`${actorLabel(actor)} attacks ${actorLabel(target)}${actor.team === "player" ? " with " + weaponLabel : ""}: ${amount} damage${critical ? " (critical)" : ""}.`);
      if (boosted) {
        log(`${weapon.name}'s fifth attack pays out: +${formatValue(toNumber(critical ? fifth.criticalAmount : fifth.amount, 0))}% damage.`);
        // …and echoes 5% of what it dealt to every other enemy, their defenses applying (like Kugelblitz's cleave).
        if (amount > 0) for (const other of [...hostiles(actor)].filter(other => other.id !== target.id && hp(other) > 0)) {
          const echoed = hurt(other, amount * toNumber(fifth.echoPercent, 0) / 100, weapon.element || "physical", false, false, actor, "bonus");
          if (echoed) log(`${weapon.name} echoes ${echoed} damage to ${actorLabel(other)}.`);
        }
      }
      applyArmorHitEffects(actor, target, weaponType, amount);
      if (critical && amount > 0 && hp(target) > 0) tearOldWound(target);
      if (weapon.rarity === "masterwork" && amount > 0) {
        const bonuses = (weapon.effects || []).map(effect => effect.kind);
        if (largerHpBonus.damagePercent > 0 || weaponRaceDamagePercent(weapon, target) > 0 || woundedTargetPercent(weapon, target) > 0 || offHand && offHandArmorPierce(weapon, offHand) > 0 || critical && bonuses.includes("weapon-crit-percent-vs-class") && weaponCritChanceAgainst(target, weapon, 10) > 10 || critical && critTargetEffects(weapon, target).length) stageEquipment(actor, weapon, { targetId: target.id });
      }
      applyUniqueWeaponHitEffects(actor, target, weapon, amount, damageMultiplier);
      if (starscale && hp(target) > 0) {
        stageEquipment(actor, weapon, { targetId: target.id });
        const bonus = hurt(target, strikePower * starscale.amount / 100, "basic", false, false, actor, "bonus", armorPierce);
        log(`${weapon.name} deals ${bonus} additional Basic damage to ${actorLabel(target)}.`);
      }
      applyAttackHitEffects(actor, target, weapon, amount, critical, activeAttackSkills);
      applyNewClassWeaponEffects(actor, target, weapon, amount, activeAttackSkills);
      // Noble Hero's second strike: the same amount again, no new hit or critical roll, and it counts as a weapon attack:
      // what a weapon hit triggers, it triggers too (M21).
      if (secondStrike && amount > 0 && hp(target) > 0 && isActive()) {
        const again = directDamage(target, amount, false, strikeElement(actor, weapon), actor, false, true);
        log(`${weapon.name} strikes ${actorLabel(target)} again: ${again} damage.`);
        if (again > 0) {
          applyArmorHitEffects(actor, target, weaponType, again);
          applyUniqueWeaponHitEffects(actor, target, weapon, again, damageMultiplier);
          applyAttackHitEffects(actor, target, weapon, again, false, activeAttackSkills);
          applyNewClassWeaponEffects(actor, target, weapon, again, activeAttackSkills);
        }
      }
      applyMeleeSpellEffects(actor, target, meleeSpellEffects);
      applyMeleeSpellEffects(actor, target, unarmedBonusEffects);
      if (weaponType === "melee" && hp(actor) > 0) for (const effect of target.effects.filter(entry => entry.type === "melee-retaliation")) {
        const reflected = hurt(actor, effect.amount, effect.element || "fire", false, true, target, "spell");
        log(`${effect.name} retaliates for ${reflected} ${effect.element || "fire"} damage.`);
      }
      // A molten weapon (Ornn's Molten Edge) sears what it strikes: Fire damage on top, and a burn.
      if (amount > 0 && hp(target) > 0) for (const effect of actor.effects.filter(entry => entry.type === "molten-strike" && entry.expiresTurn > actor.turns)) {
        const seared = hurt(target, Math.max(1, Math.round(amount * effect.amount / 100)), "fire", false, true, actor, "spell");
        if (hp(target) > 0 && toNumber(effect.burnPercent, 0) > 0 && !debuffImmune(target)) {
          target.effects = target.effects.filter(entry => entry.key !== `molten-burn:${actor.id}`);
          target.effects.push({ key: `molten-burn:${actor.id}`, name: "Molten Burn", type: "turn-start-max-hp-loss-percent", amount: effect.burnPercent, expiresTurn: target.turns + effect.burnTurns });
        }
        log(`${effect.name} sears ${actorLabel(target)} for ${seared} fire damage${hp(target) > 0 && effect.burnPercent ? " and leaves a burn" : ""}.`);
      }
      // A share of the blow comes back (Rootbark's autumn).
      if (weaponType === "melee" && amount > 0 && hp(actor) > 0) for (const effect of target.effects.filter(entry => entry.type === "melee-retaliation-percent")) {
        const reflected = hurt(actor, Math.max(1, Math.round(amount * effect.amount / 100)), effect.element || "nature", false, true, target, "spell");
        log(`${effect.name} turns ${reflected} ${effect.element || "nature"} damage back on ${actorLabel(actor)}.`);
      }
    } else {
      if (onStage(target)) stage("miss", { actorId: target.id, attackerId: actor.id });
      log(`${actorLabel(actor)} misses ${actorLabel(target)}${actor.team === "player" ? " with " + weaponLabel : ""}.`);
      const phantom = actor.team !== target.team && hp(target) > 0 ? artifactEffectsOf(target, "evade-next-attack-percent")[0] : null;
      if (phantom) { target.phantomStepBonus = Math.max(toNumber(target.phantomStepBonus, 0), phantom.amount); log(`${phantom.itemName}: ${actorLabel(target)}'s next weapon attack deals +${formatValue(phantom.amount)}% damage.`); }
      const riposte = passiveEffectsOf(target).find(effect => effect.type === "fencer-riposte");
      if (riposte && fencerReady(target) && target.riposteTurn !== target.turns && hp(target) > 0) {
        target.riposteTurn = target.turns;
        const counterWeapon = attackLoadout(target).primary;
        const counter = withWeaponElement(counterWeapon, () => hurt(actor, weaponAttackValues(target, counterWeapon).power * toNumber(riposte.amount, 0) / 100, weaponElement(counterWeapon), false, false, target, "melee", toNumber(counterWeapon.armorPierce, 0)));
        log(`${actorLabel(target)} ripostes for ${counter} damage.`);
      }
    }
    if (meleeSpellEffects.length) actor.effects = actor.effects.filter(effect => !meleeSpellEffects.includes(effect));
    triggerWeaponTraps(actor, target, weaponType);
    return landed;
  }
  function attackAftermathSkills(actor, weapon) {
    return skillsOf(actor).filter(skill => skill.durationAttacks && skillStates(actor)[classSkillKey(skill)]?.active && weaponMatches(weapon, skill.requiredWeaponType) && Array.isArray(skill.afterAttackEffects));
  }
  function applyAttackAftermath(actor, skills) {
    for (const skill of skills) applyTimedEffects(actor, classSkillKey(skill) + ":aftermath", skill.name, skill.afterAttackEffects);
  }
  function attack(actor, target) {
    const loadout = attackLoadout(actor);
    target = redirectedGuardTarget(actor, target, normalizeWeaponType(loadout.primary.type));
    if (!actionAllowed(actor, "attack") || !target || !opponents(actor).includes(target)) return false;
    if (!spend(actor, loadout.cost)) return false;
    battle.actionSerial = toInteger(battle.actionSerial, 0) + 1;
    return withEquipmentCharge(actor, () => {
      recordTurnAction(actor, "attack");
      if (loadout.primary.handed === 1 && !loadout.primary.unarmed) {
        const progress = actor.id === "player" ? classProgress : (actor.character.classProgress ||= { oneHandedAttacks: 0, summonCasts: {}, lootEnemiesCollected: 0 });
        progress.oneHandedAttacks = Math.max(0, toInteger(progress.oneHandedAttacks, 0)) + 1;
      }
      const aftermathSkills = attackAftermathSkills(actor, loadout.primary);
      if (loadout.primary.unarmed) {
        if (actor.id === "player") unarmedAttacks += 1;
        else actor.character.unarmedAttacks = Math.max(0, toInteger(actor.character.unarmedAttacks, 0)) + 1;
      }
      // Phantom Step Pin's bonus is spent on this attack, hit or miss (A30).
      const phantom = toNumber(actor.phantomStepBonus, 0);
      actor.phantomStepBonus = 0;
      const focusMultiplier = focusedPowerMultiplier(actor) * (1 + phantom / 100);
      const landed = strike(actor, target, loadout.primary, focusMultiplier);
      actor.firstStrikeUsed = true;
      consumeSkill(actor, "durationAttacks", loadout.primary);
      const offHandLanded = loadout.offHand && hp(target) > 0 ? strike(actor, target, loadout.offHand, 0.25 * focusMultiplier, true) : false;
      if (!landed && !offHandLanded && actor.gambitMissRefundTurn !== actor.turns) {
        const refund = passiveEffectsOf(actor).filter(effect => effect.type === "gambit-miss-refund-ap").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
        if (refund) {
          actor.gambitMissRefundTurn = actor.turns;
          battle.ap = Math.min(actionPointMaximum(actor), battle.ap + refund);
          log(`${actorLabel(actor)} recovers ${refund} AP with Second Chance.`);
        }
      }
      actor.currentTurnAttackTargetIds ||= [];
      if (!actor.currentTurnAttackTargetIds.includes(target.id)) actor.currentTurnAttackTargetIds.push(target.id);
      applyAttackAftermath(actor, aftermathSkills);
      return true;
    });
  }
  // Port task 3 (the Shop Rework, R17–R21): Tripwire, Smoke Trap, Frost Glyph, Weighted Net and Gravity Well.
  const controlTrapEffects = ["trap-tripwire", "trap-smoke", "trap-frost-glyph", "trap-weighted-net", "trap-gravity-well"];
  const trapEffects = new Set(["trap-spring", "trap-spell-rune", "trap-sentry", ...controlTrapEffects]);
  const supportedEffects = new Set([
    "damage", "weapon-charge-all-enemies", "heal", "restore-mana", "stat-percent", "all-stats-percent", "defense-flat", "defense-percent",
    "stat-permanent-percent", "defense-permanent-percent", "delayed-hp-loss-percent", "stun", "stun-chance",
    "summon-friendly", "cast-archived-spell", "next-melee-attack", "remove-random-debuff", "flee-disabled", "healing-received-percent", "root-ap", "seal-wound", "storm-surge",
    "incoming-race-damage-percent", "incoming-ranged-armor-percent", "incoming-ranged-hit-percent",
    "incoming-spell-damage-from-target-percent", "incoming-spell-damage-percent", "incoming-spell-miss-chance",
    "incoming-weapon-damage-percent", "instant-death-per-thousand", "melee-retaliation",
    "outgoing-attack-damage-percent", "spell-power-percent", "unarmed-bonus-damage", "weather-control", ...trapEffects
  ]);
  function activeTrapCount(actor) {
    return battle.actors
      .filter(entry => entry.team === actor.team && hp(entry) > 0)
      .reduce((count, entry) => count + entry.effects.filter(effect => trapEffects.has(effect.type)).length, 0);
  }
  function trapCapacity(actor) {
    return withActor(actor, () => calculateTrapLimit());
  }
  function trapPlacementCount(effects) {
    return effects.filter(effect => trapEffects.has(effect.type)).length;
  }
  function trapCapacityReason(actor, effects) {
    const placements = trapPlacementCount(effects);
    if (!placements) return "";
    const active = activeTrapCount(actor), limit = trapCapacity(actor);
    return active + placements > limit ? `Trap limit reached (${active}/${limit})` : "";
  }
  function spellEffects(spell) {
    if (spell.effects?.length) return spell.effects;
    // Damage spells have an unambiguous default; support spells require editable effects.
    return spell.type.toLowerCase() === "damage" ? [{ type: "damage", amount: "power" }] : [];
  }
  function playTierTenCinematic(actor, spell) {
    // The animated battlefield plays its own tier X animation on the field.
    if (window.BattleStage?.enabled()) return;
    if (spell?.tier === 10) window.TierTenCinematics?.play(spell, actor?.name || "Caster");
  }
  function effectMatchesElement(effect, element) {
    const included = effect.elements || (effect.element ? [effect.element] : []);
    const excluded = effect.excludedElements || [];
    const active = elementList(element);
    return (!included.length || active.some(candidate => included.includes(candidate))) && !active.some(candidate => excluded.includes(candidate));
  }
  function timedSpellPercent(actor, type, spell) {
    const elements = withActor(actor, () => effectiveSpellElements(spell));
    return actor.effects.filter(effect => effect.type === type && effectMatchesElement(effect, elements)).reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
  }
  function spellMissChanceAgainst(actor, target, spell) {
    let chance = timedSpellPercent(actor, "spell-miss-chance", spell);
    if (target) {
      chance += racePassiveEffectsOf(target).filter(effect => effect.type === "incoming-spell-miss-chance").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
      chance += target.effects.filter(effect => effect.type === "incoming-spell-miss-chance").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
    }
    return rules.chance(chance);
  }
  // A body to raise. Never a fallen hero of the party (a lich's Call of the Dead must not take one
  // out of the battle), an inert object, or a lich who is only dust while his phylactery stands.
  function defeatedEnemyFor(actor) {
    return battle.actors.find(candidate => candidate.team !== actor.team && hp(candidate) <= 0 && !candidate.corpseConsumed && candidate.id !== "player" && !candidate.companion && !candidate.inert && !phylacteryHolds(candidate)) || null;
  }
  function effectsUsable(effects) {
    return effects.length > 0 && effects.every(effect => {
      const isSummon = effect.type === "summon-friendly";
      const isStatPercent = effect.type === "stat-percent" || effect.type === "stat-permanent-percent";
      const isDefense = effect.type === "defense-flat" || effect.type === "defense-percent" || effect.type === "defense-permanent-percent";
      const isTrap = trapEffects.has(effect.type);
      const isMeleeSpell = effect.type === "next-melee-attack";
      return supportedEffects.has(effect.type) &&
        (isSummon ? typeof effect.enemyId === "string" && effect.enemyId.length > 0 : effect.amount === "power" || Number.isFinite(effect.amount)) &&
        (effect.flatAmount == null || Number.isFinite(effect.flatAmount)) &&
        (effect.maxPercent == null || Number.isFinite(effect.maxPercent) && effect.maxPercent >= 0) &&
        (!isSummon || effect.durationTurns == null || Number.isInteger(effect.durationTurns) && effect.durationTurns > 0) &&
        (!isSummon || effect.maxActive == null || Number.isInteger(effect.maxActive) && effect.maxActive > 0) &&
        (!isSummon || effect.count == null || Number.isInteger(effect.count) && effect.count > 0) &&
        (!isSummon || effect.summonMaxHpPercent == null || Number.isFinite(effect.summonMaxHpPercent) && effect.summonMaxHpPercent >= 0) &&
        (!isSummon || effect.summonAttackDamagePercent == null || Number.isFinite(effect.summonAttackDamagePercent)) &&
        (!isTrap || effect.cap == null || Number.isFinite(effect.cap) && effect.cap >= 0) &&
        (effect.type !== "trap-sentry" || effect.recipient === "friendly-target") &&
        (!isMeleeSpell || effect.statDebuff == null || bonusDefinitions.includes(effect.statDebuff.target) && Number.isFinite(effect.statDebuff.amount) && Number.isInteger(effect.statDebuff.durationTurns) && effect.statDebuff.durationTurns > 0) &&
        (!isMeleeSpell || effect.stunChance == null || Number.isFinite(effect.stunChance) && effect.stunChance >= 0) &&
        (!isMeleeSpell || effect.lifeSteal == null || Number.isFinite(effect.lifeSteal) && effect.lifeSteal >= 0) &&
        (!isStatPercent || bonusDefinitions.includes(effect.target)) &&
        (!isDefense || defenseBonusDefinitions.includes(effect.target));
    });
  }
  function summonLimitReached(actor, effect) {
    const maximum = Math.max(0, toInteger(effect.maxActive, 0));
    return maximum > 0 && battle.actors.filter(entry => entry.team === actor.team && entry.summonDefinitionId === effect.enemyId && hp(entry) > 0).length >= maximum;
  }
  function aiSummonAllowed(actor, effects) {
    if (actor.team !== "enemy" || !actor.enemyDefinitionId || !effects.some(effect => effect.type === "summon-friendly")) return true;
    // Read the current enemy definition so the rule also applies to resumed
    // battles; existing saves do not need new actor fields or a migration.
    const condition = cacheCalculation("ai-summon-condition", () => enemyDefinitions.find(definition => definition.id === actor.enemyDefinitionId)?.aiSummonCondition, actor);
    if (!condition) return true;
    const count = cacheCalculation("ai-living-owned-summons", () => living().filter(entry => entry.summoned && entry.team === actor.team && entry.summonerId === actor.id && condition.raceIds.includes(actorRace(entry))).length, actor);
    return count <= condition.maximumLiving;
  }
  function summonEquipmentBonuses(actor) {
    return cacheCalculation("summon-equipment-bonuses", () => withActor(actor, () => equippedWeaponItems().flatMap(item => item.effects || []).filter(effect => effect.kind === "summon-empowerment").reduce((total, effect) => ({ hp: total.hp + effect.amount, mana: total.mana + effect.manaPercent, damage: total.damage + effect.damagePercent }), { hp: 0, mana: 0, damage: 0 })), actor);
  }
  function summonFriendly(actor, effect, source) {
    const definition = enemyDefinitions.find(enemy => enemy.id === effect.enemyId || slugify(enemy.name) === effect.enemyId);
    const corpse = effect.requiresDefeatedEnemy ? defeatedEnemyFor(actor) : null;
    if (!definition || battle.actors.length >= 100 || summonLimitReached(actor, { ...effect, enemyId: definition.id }) || effect.requiresDefeatedEnemy && !corpse) return false;
    if (corpse && effect.consumeDefeatedEnemy) {
      corpse.corpseConsumed = true;
      battle.actors = battle.actors.filter(candidate => candidate !== corpse);
      log(`${source} consumes ${actorLabel(corpse)}.`);
    }
    battle.summonSequence = Math.max(0, toInteger(battle.summonSequence, 0)) + 1;
    const duplicateCount = battle.actors.filter(entry => entry.team === actor.team && entry.summonDefinitionId === definition.id).length;
    const summoned = {
      id: `summon-${battle.summonSequence}`,
      name: duplicateCount ? `${definition.name} ${duplicateCount + 1}` : definition.name,
      team: actor.team,
      counter: 0,
      turns: 0,
      effects: [],
      skillExpiry: {},
      alive: true,
      summoned: true,
      summonerId: actor.id,
      summonDefinitionId: definition.id,
      summonDurationTurns: effect.durationTurns || null,
      baseArmor: definition.baseArmor,
      naturalWeapon: definition.naturalWeapon || null,
      maxHpPercent: definition.maxHpPercent,
      maxManaPercent: definition.maxManaPercent,
      turnStartManaPercent: definition.turnStartManaPercent,
      element: definition.element,
      character: armEnemy(copy(definition.character)).character,
      resistances: copy(definition.resistances),
    };
    if (summoned.team === "enemy") summoned.naturalArmor = spawnNaturalArmor(summoned.character);
    summoned.character.activeSkills = {};
    const summonStatPercent = withActor(actor, () => calculateClassEffects().summonBaseStatsPercent) + toNumber(effect.summonBaseStatsPercent, 0);
    if (summonStatPercent) {
      for (const key of Object.keys(summoned.character.stats)) {
        summoned.character.stats[key] = Math.max(0, Math.floor(summoned.character.stats[key] * (1 + summonStatPercent / 100)));
      }
    }
    battle.actors.push(summoned);
    stage("summon", { actorId: summoned.id, ownerId: actor.id });
    const summonedValues = values(summoned);
    const equipmentBonus = summonEquipmentBonuses(actor);
    // Titanfall's golem at its level's share of its strength (port task 4, A16): HP and damage.
    const strength = effect.summonStrengthPercent != null ? Math.max(1, toNumber(effect.summonStrengthPercent, 100)) / 100 : 1;
    const maximumHp = Math.max(1, Math.floor(summonedValues.maxHp * (1 + Math.max(0, toNumber(effect.summonMaxHpPercent, 0)) / 100) * (1 + equipmentBonus.hp / 100) * strength));
    const maximumMana = Math.max(0, Math.floor(summonedValues.maxMana * (1 + equipmentBonus.mana / 100)));
    summoned.resourceMax = { hp: maximumHp, mana: maximumMana };
    summoned.character.currentHp = maximumHp;
    summoned.character.currentMana = maximumMana;
    if (equipmentBonus.damage) summoned.effects.push({ key: `${source}:equipment-summon-damage`, name: "Staff of the Guild", type: "outgoing-attack-damage-percent", amount: equipmentBonus.damage, expiresTurn: Number.MAX_SAFE_INTEGER });
    if (strength !== 1) summoned.effects.push({ key: `${source}:summon-strength`, name: source, type: "outgoing-attack-damage-percent", amount: Math.round((strength - 1) * 1000) / 10, expiresTurn: Number.MAX_SAFE_INTEGER });
    if (effect.summonAttackDamagePercent) summoned.effects.push({
      key: `${source}:summon-attack-damage`, name: source, type: "outgoing-attack-damage-percent",
      attackKinds: ["melee", "ranged"], amount: toNumber(effect.summonAttackDamagePercent, 0), expiresTurn: Number.MAX_SAFE_INTEGER
    });
    log(`${actorLabel(actor)} summons ${summoned.name}${summoned.summonDurationTurns ? ` for ${summoned.summonDurationTurns} turns` : ""} with ${source}.`);
    return true;
  }
  /* ---------------- The cycle of seasons ----------------
     A boss with a "seasonCycle" (Juila) turns the battlefield to its next season every few of
     her turns. A season only strengthens her side ("allies") or hinders the heroes ("foes"),
     and calls a spirit of that season, which fades when the season ends. The coming season is
     foretold a turn ahead. Winter also roots a hero at each of her turns (AP lost next turn).
     The cycle stops, and its effects end, when she falls. */
  const seasonCycleOf = actor => actor && enemyDefinitions.find(definition => definition.id === actor.enemyDefinitionId)?.seasonCycle || null;
  function seasonState() {
    const state = battle?.season;
    if (!state || state.ended) return null;
    const owner = actorById(state.ownerId), cycle = seasonCycleOf(owner);
    if (!owner || !cycle) return null;
    const count = cycle.seasons.length;
    return { state, owner, cycle, season: cycle.seasons[((state.index % count) + count) % count], next: cycle.seasons[(state.index + 1) % count] };
  }
  function applySeasonTo(recipient) {
    recipient.effects = recipient.effects.filter(effect => !effect.season);
    const current = seasonState();
    recipient.seasonIndex = current ? current.state.index : null;
    if (!current || hp(recipient) <= 0) return;
    const foe = recipient.team !== current.owner.team, definitions = (foe ? current.season.foes : current.season.allies) || [];
    applyTimedEffects(recipient, `season:${current.season.id}`, current.season.name, definitions.map(entry => ({ ...entry, durationTurns: 1e7, unpreventable: true, unremovable: true })));
    recipient.effects.filter(effect => String(effect.key || "").startsWith(`season:${current.season.id}:`)).forEach(effect => { effect.season = true; effect.seasonFoe = foe; });
  }
  function fadeSeasonSpirits(owner, reason) {
    for (const spirit of living().filter(actor => actor.seasonSpirit && actor.summonerId === owner.id)) {
      setHp(spirit, 0); spirit.alive = false; spirit.summonLossRecorded = true;
      log(`${spirit.name} fades ${reason}.`);
    }
  }
  function beginSeason(owner, index) {
    const cycle = seasonCycleOf(owner), previous = battle.season.index >= 0 ? cycle.seasons[battle.season.index % cycle.seasons.length] : null;
    Object.assign(battle.season, { index, startTurn: owner.turns, foretold: null });
    const { season } = seasonState();
    if (previous) fadeSeasonSpirits(owner, `as ${previous.name} passes`);
    log(`${season.name} comes to the battlefield. ${season.description || ""}`.trim());
    stage("season", { season: season.id, name: season.name });
    for (const recipient of living()) applySeasonTo(recipient);
    if (season.spirit && summonFriendly(owner, { type: "summon-friendly", enemyId: season.spirit }, season.name)) {
      const spirit = battle.actors.at(-1);
      spirit.seasonSpirit = true;
      applySeasonTo(spirit);
    }
  }
  function endSeasons(owner) {
    const current = seasonState();
    if (!current) return;
    current.state.ended = true;
    for (const actor of battle.actors) { actor.effects = actor.effects.filter(effect => !effect.season); actor.seasonIndex = null; }
    fadeSeasonSpirits(owner, "as the seasons fall still");
    log(`With ${owner.name} fallen, the seasons fall still.`);
    stage("season", { season: null, name: null });
  }
  /* ---------------- The warband ----------------
     A chief with a "warband" (Zik Lun) leads every other fighter on his side. At the start of
     each of his turns he bellows an order that holds until his next turn: Charge! marks a hero
     as the warband's prey, Hold the Line! armors the warband and sets his Shieldbearer before
     him, Regroup! cleanses and heals it. A living War-drummer beats every order stronger. Each
     warband troll that falls feeds his Vengeance; if he falls first, the rest lose heart. */
  const warbandOf = actor => actor?.team === "enemy" && actor.enemyDefinitionId ? enemyDefinitions.find(definition => definition.id === actor.enemyDefinitionId)?.warband || null : null;
  function warbandState() {
    const state = battle?.warband;
    if (!state || state.ended) return null;
    const chief = actorById(state.chiefId);
    return chief ? { state, chief, config: warbandOf(chief) } : null;
  }
  const warbandMembers = chief => battle.actors.filter(actor => actor.team === chief.team && actor.id !== chief.id);
  function warbandPreyFor(actor) {
    const current = warbandState();
    if (!current || current.state.order !== "charge" || actor.team !== current.chief.team) return null;
    const prey = actorById(current.state.preyId);
    return prey && hp(prey) > 0 ? prey : null;
  }
  // The drummer beats every order half again as strong.
  const orderScale = (chief, config) => living().some(actor => actor.team === chief.team && actor.enemyDefinitionId === config.drummer) ? 1 + toNumber(config.drumPercent, 0) / 100 : 1;
  function applyOrderTo(recipient, current) {
    const { state, chief, config } = current;
    recipient.effects = recipient.effects.filter(effect => !effect.warbandOrder);
    recipient.warbandOrderSerial = state.serial;
    const order = config.orders[state.order];
    if (!order || hp(recipient) <= 0) return;
    const scaled = list => (list || []).map(entry => ({ ...entry, amount: Math.round(toNumber(entry.amount, 0) * state.scale), durationTurns: 1e7, unpreventable: true }));
    const definitions = recipient.team === chief.team ? scaled(order.allies) : recipient.id === state.preyId ? scaled(order.prey) : [];
    if (!definitions.length) return;
    const key = `warband-order:${state.order}`;
    applyTimedEffects(recipient, key, recipient.team === chief.team ? order.name : `Prey of ${chief.name}`, definitions);
    recipient.effects.filter(effect => String(effect.key || "").startsWith(key + ":")).forEach(effect => { effect.warbandOrder = true; });
  }
  function chooseOrder(chief, config, state) {
    const band = [chief, ...warbandMembers(chief)].filter(actor => hp(actor) > 0);
    const hurt = band.filter(actor => hp(actor) < values(actor).maxHp * 0.5).length;
    const recent = state.history.slice(-2);
    if (config.orders.regroup && band.length > 1 && hurt >= 2 && !recent.includes("regroup")) return "regroup";
    const guard = band.find(actor => actor.enemyDefinitionId === config.guard);
    if (config.orders.hold && guard && hp(chief) < values(chief).maxHp * 0.6 && state.history.at(-1) !== "hold") return "hold";
    return "charge";
  }
  function issueOrder(chief, config, state) {
    const id = chooseOrder(chief, config, state), order = config.orders[id];
    Object.assign(state, { order: id, preyId: null, serial: state.serial + 1, scale: orderScale(chief, config) });
    state.history.push(id);
    const band = [chief, ...warbandMembers(chief)].filter(actor => hp(actor) > 0);
    if (id === "charge") {
      // The warband hunts the hero closest to falling.
      const heroes = living().filter(actor => actor.team !== chief.team);
      const prey = heroes.sort((left, right) => hp(left) / Math.max(1, values(left).maxHp) - hp(right) / Math.max(1, values(right).maxHp) || hp(left) - hp(right))[0];
      state.preyId = prey?.id || null;
    }
    // The last order ends everywhere before the new one is given.
    for (const actor of battle.actors) actor.effects = actor.effects.filter(effect => !effect.warbandOrder);
    const prey = actorById(state.preyId);
    log(`${chief.name} bellows "${order.name}"${prey ? ` and points at ${actorLabel(prey)}` : ""}${state.scale > 1 ? ", and the war drums beat it louder" : ""}.`);
    for (const actor of living()) applyOrderTo(actor, { state, chief, config });
    // Hold the Line: the Shieldbearer takes every single-target blow meant for the chief.
    const guard = id === "hold" && order.ward ? band.find(actor => actor.enemyDefinitionId === config.guard) : null;
    if (guard) chief.effects.push({ key: "warband-order:hold:ward", name: order.name, type: "guardian-ward", guardianId: guard.id, amount: 1, expiresTurn: Number.MAX_SAFE_INTEGER, warbandOrder: true });
    if (id === "regroup") {
      for (const actor of band) {
        if (order.cleanse) actor.effects = actor.effects.filter(effect => effect.unremovable || !isDebuffDefinition(effect));
        // Rallying is the order's own effect, not ordinary healing: it is not reduced.
        phaseHealingDepth += 1;
        try { healHp(actor, Math.floor(values(actor).maxHp * toNumber(order.healPercent, 0) * state.scale / 100)); } finally { phaseHealingDepth -= 1; }
      }
    }
    stage("order", { order: id, name: order.name, chiefId: chief.id, preyId: state.preyId });
  }
  function addVengeance(chief, config, fallen) {
    const vengeance = config.vengeance || {}, state = battle.warband;
    state.vengeance = Math.min(Math.max(1, toInteger(vengeance.maxStacks, 5)), state.vengeance + 1);
    const stacks = state.vengeance;
    applyTimedEffects(chief, "warband-vengeance", `Vengeance (${stacks})`, [
      { type: "outgoing-attack-damage-percent", amount: toNumber(vengeance.damagePercent, 0) * stacks, attackKinds: ["melee", "ranged"], durationTurns: 1e7, unpreventable: true, unremovable: true },
      { type: "stat-percent", target: "speed", amount: toNumber(vengeance.speedPercent, 0) * stacks, durationTurns: 1e7, unpreventable: true, unremovable: true },
    ]);
    log(`${chief.name} roars over the fallen ${fallen.name}: Vengeance (${stacks}).`);
    stage("status", { actorId: chief.id, label: `Vengeance ${stacks}` });
  }
  function loseHeart(chief, config, state) {
    state.ended = true;
    for (const actor of battle.actors) actor.effects = actor.effects.filter(effect => !effect.warbandOrder);
    const band = warbandMembers(chief).filter(actor => hp(actor) > 0);
    for (const actor of band) applyTimedEffects(actor, "warband-heartless", "Lost Heart", (config.loseHeart || []).map(entry => ({ ...entry, durationTurns: 1e7, unpreventable: true, unremovable: true })));
    if (band.length) log(`With ${chief.name} fallen, his warband loses heart.`);
    stage("order", { order: null, name: null });
  }
  // Runs at the start of every turn in a battle with a warband chief.
  function warbandTurn(next) {
    if (!battle.warband) {
      const chief = battle.actors.find(actor => actor.team === "enemy" && !actor.summoned && hp(actor) > 0 && warbandOf(actor));
      if (!chief) { battle.warband = { ended: true }; return; }
      battle.warband = { chiefId: chief.id, order: null, preyId: null, serial: 0, scale: 1, history: [], fallen: [], vengeance: 0, ended: false };
    }
    const current = warbandState();
    if (!current) return;
    const { state, chief, config } = current;
    for (const member of warbandMembers(chief)) {
      if (hp(member) > 0 || state.fallen.includes(member.id)) continue;
      state.fallen.push(member.id);
      if (hp(chief) > 0) addVengeance(chief, config, member);
    }
    if (hp(chief) <= 0) { loseHeart(chief, config, state); return; }
    if (next.id === chief.id) issueOrder(chief, config, state);
    // Trolls that answered the horn since the order was given take it up too.
    else if (state.order && next.team === chief.team && next.warbandOrderSerial !== state.serial) applyOrderTo(next, current);
  }

  /* ---------------- The gathering storm ----------------
     A boss with a "storm" (Tankina) fights under a sky that darkens with every lightning spell
     cast in the battle, by anyone: from a light drizzle to an extreme thunderstorm. It never
     calms. Each stage strengthens the boss's whole side ("allies" effects, flagged storm). */
  const stormOf = actor => actor?.team === "enemy" && actor.enemyDefinitionId ? enemyDefinitions.find(definition => definition.id === actor.enemyDefinitionId)?.storm || null : null;
  function stormState() {
    const state = battle?.storm;
    if (!state || state.ended) return null;
    const owner = actorById(state.ownerId), config = owner ? stormOf(owner) : null;
    if (!config) return null;
    const stage = config.stages[state.stageIndex], next = config.stages[state.stageIndex + 1] || null;
    return { state, owner, config, stage, next };
  }
  function applyStormTo(recipient) {
    recipient.effects = recipient.effects.filter(effect => !effect.storm);
    const current = stormState();
    recipient.stormStage = current ? current.state.stageIndex : null;
    if (!current || hp(recipient) <= 0 || recipient.team !== current.owner.team) return;
    const key = `storm:${current.stage.id}`;
    applyTimedEffects(recipient, key, current.stage.name, (current.stage.allies || []).map(entry => ({ ...entry, durationTurns: 1e7, unpreventable: true, unremovable: true })));
    recipient.effects.filter(effect => String(effect.key || "").startsWith(key + ":")).forEach(effect => { effect.storm = true; });
  }
  function startStorm() {
    const owner = battle.actors.find(actor => actor.team === "enemy" && !actor.summoned && hp(actor) > 0 && stormOf(actor));
    battle.storm = owner ? { ownerId: owner.id, intensity: 0, stageIndex: 0, ended: false } : { ended: true };
    if (owner) {
      const first = stormOf(owner).stages[0];
      log(`${first.name} begins to fall. ${first.description || ""}`.trim());
      stage("season", { season: first.id, name: first.name });
    }
  }
  // Every lightning spell darkens the sky; the stage follows the storm's intensity.
  function feedStorm(amount = 1) {
    if (!battle.storm) startStorm();
    const current = stormState();
    if (!current) return;
    const { state, config } = current;
    state.intensity += Math.max(0, toNumber(amount, 1));
    let index = state.stageIndex;
    while (config.stages[index + 1] && state.intensity >= toNumber(config.stages[index + 1].at, Infinity)) index += 1;
    if (index === state.stageIndex) return;
    state.stageIndex = index;
    const reached = config.stages[index];
    log(`The storm grows: ${reached.name}. ${reached.description || ""}`.trim());
    stage("season", { season: reached.id, name: reached.name });
    for (const actor of living()) applyStormTo(actor);
  }
  // At every turn start: the storm begins with the battle, and newcomers on her side take it up.
  function stormTurn(next) {
    if (!battle.storm) startStorm();
    const current = stormState();
    if (current && next.stormStage !== current.state.stageIndex) applyStormTo(next);
  }

  /* ---------------- Reforging ----------------
     A boss with "reforge" (Ornn) works at his anvil every few of his turns. At the end of a
     turn he heats the metal for one of his tempers, announced to the heroes; at the start of
     his next turn the temper sets, unless the heroes dealt him "spoilPercent" of his maximum
     HP in between: then the metal cracks, there is no temper, and he loses "spoilApLoss" AP
     on that turn. A finished temper holds until the next one replaces it. While he works, his
     "guard" (the Vulcanic Ram) takes the single-target blows meant for him. */
  const reforgeOf = actor => actor?.team === "enemy" && actor.enemyDefinitionId ? enemyDefinitions.find(definition => definition.id === actor.enemyDefinitionId)?.reforge || null : null;
  function reforgeState() {
    const state = battle?.reforge;
    if (!state || state.ended) return null;
    const smith = actorById(state.smithId), config = smith ? reforgeOf(smith) : null;
    if (!config) return null;
    const temper = id => config.tempers.find(entry => entry.id === id) || null;
    return { state, smith, config, working: temper(state.working), current: temper(state.temper) };
  }
  function creditReforgeDamage(target, attacker, dealt) {
    const current = reforgeState();
    if (!current?.working || dealt <= 0 || target.id !== current.smith.id || attacker?.team === current.smith.team) return;
    current.state.damage += dealt;
  }
  function setTemper(current, id) {
    const { state, smith, config } = current, temper = config.tempers.find(entry => entry.id === id);
    smith.effects = smith.effects.filter(effect => !effect.temper);
    state.temper = temper ? id : null;
    if (!temper) return;
    applyTimedEffects(smith, "temper", temper.name, temper.effects.map(entry => ({ ...entry, durationTurns: 1e7, unpreventable: true, unremovable: true })));
    smith.effects.filter(effect => String(effect.key || "").startsWith("temper:")).forEach(effect => { effect.temper = true; });
  }
  // He reads the fight, never forging the same temper twice running: "wounded" tempers when
  // he is hurt, "melee" ones against blades, "always" ones otherwise.
  function chooseTemper(current) {
    const { state, smith, config } = current;
    const heroes = living().filter(actor => actor.team !== smith.team && !actor.summoned);
    const melee = heroes.filter(actor => normalizeWeaponType(attackLoadout(actor).primary?.type) === "melee").length / Math.max(1, heroes.length);
    const hurt = 1 - hp(smith) / Math.max(1, values(smith).maxHp);
    const score = temper => temper.favor === "wounded" ? 1 + hurt * 3 : temper.favor === "melee" ? 0.5 + melee * 2 : 1.5;
    return config.tempers.filter(temper => temper.id !== state.temper).sort((left, right) => score(right) - score(left))[0] || null;
  }
  const anvilGuard = current => current.config.guard ? living().find(actor => actor.team === current.smith.team && [actor.enemyDefinitionId, actor.summonDefinitionId].includes(current.config.guard)) || null : null;
  // At the end of his turn, when the time comes, he heats the metal.
  function reforgeTurnEnd(actor) {
    const current = reforgeState();
    if (!current || actor?.id !== current.smith.id || hp(actor) <= 0 || current.working || actor.turns < current.state.nextTurn) return;
    const { state, smith, config } = current, temper = chooseTemper(current);
    if (!temper) return;
    Object.assign(state, { working: temper.id, damage: 0, threshold: Math.max(1, Math.floor(values(smith).maxHp * config.spoilPercent / 100)) });
    const guard = anvilGuard(current);
    if (guard) smith.effects.push({ key: "reforge:ward", name: "Guarding the Anvil", type: "guardian-ward", guardianId: guard.id, amount: 1, expiresTurn: Number.MAX_SAFE_INTEGER, reforgeWard: true, unremovable: true });
    log(`${smith.name} turns to his anvil and heats the metal: ${temper.name}. Deal him ${config.spoilPercent}% of his health before his next turn to crack it!${guard ? ` ${actorLabel(guard)} guards the anvil.` : ""}`);
    stage("order", { order: "reforge", name: `Reforging · ${temper.name}` });
  }
  // At the start of his next turn the temper sets, or the cracked metal is lost.
  function reforgeTurn(next) {
    if (!battle.reforge) {
      const smith = battle.actors.find(actor => actor.team === "enemy" && !actor.summoned && hp(actor) > 0 && reforgeOf(actor));
      battle.reforge = smith ? { smithId: smith.id, working: null, temper: null, damage: 0, threshold: 0, nextTurn: Math.max(1, toInteger(reforgeOf(smith).firstTurn, 1)), forged: 0, spoiled: 0, ended: false } : { ended: true };
    }
    const current = reforgeState();
    if (!current) return;
    const { state, smith, config, working } = current;
    if (hp(smith) <= 0) {
      smith.effects = smith.effects.filter(effect => !effect.temper && !effect.reforgeWard);
      state.ended = true;
      stage("order", { order: null, name: null });
      return;
    }
    if (next.id !== smith.id || !working) return;
    smith.effects = smith.effects.filter(effect => !effect.reforgeWard);
    if (state.damage >= state.threshold) {
      state.spoiled += 1;
      next.staggerAp = Math.max(toInteger(next.staggerAp, 0), Math.max(0, toInteger(config.spoilApLoss, 2)));
      next.staggerReason = "reels from the cracked metal";
      log(`The metal cracks under the heroes' blows! ${smith.name}'s ${working.name} is spoiled.`);
      stage("status", { actorId: smith.id, label: "Temper spoiled", debuff: true });
    } else {
      state.forged += 1;
      setTemper(current, working.id);
      log(`${smith.name} quenches the glowing metal: ${working.name}. ${working.description || ""}`.trim());
      stage("status", { actorId: smith.id, label: working.name });
    }
    Object.assign(state, { working: null, damage: 0, threshold: 0, nextTurn: smith.turns + Math.max(1, config.everyTurns) - 1 });
    stage("order", { order: null, name: null });
  }

  /* ---------------- Regrowing heart-cores ----------------
     A boss with "heartCores" (Gaian) draws on Cores of the land ("enemyId"; their shield is his
     livingAllyProtection). A fallen Core regrows "regrowTurns" of his turns after it fell. If every
     Core is down at once he is Severed for "severed.turns" of his turns: he takes more damage and
     has fewer AP. Then every Core regrows together. */
  const heartCoresOf = actor => actor?.team === "enemy" && actor.enemyDefinitionId ? enemyDefinitions.find(definition => definition.id === actor.enemyDefinitionId)?.heartCores || null : null;
  function heartCoreState() {
    const state = battle?.heartCores;
    if (!state || state.ended) return null;
    const avatar = actorById(state.avatarId), config = avatar ? heartCoresOf(avatar) : null;
    if (!config) return null;
    const cores = battle.actors.filter(actor => actor.team === avatar.team && !actor.summoned && actor.enemyDefinitionId === config.enemyId);
    return { state, avatar, config, cores, severed: state.severedUntil != null };
  }
  function regrowCore(current, core) {
    core.effects = []; core.alive = true; core.counter = 0;
    const maximum = values(core);
    core.character.currentHp = maximum.maxHp; core.character.currentMana = maximum.maxMana;
    delete current.state.fallen[core.id];
    log(`${actorLabel(core)} regrows from the roots of the land.`);
    stage("status", { actorId: core.id, label: "Regrown" });
  }
  function setSevered(current, severed) {
    const { avatar, config } = current;
    avatar.effects = avatar.effects.filter(effect => !effect.severed);
    if (!severed) return;
    const exposed = toNumber(config.severed.damageTakenPercent, 0);
    applyTimedEffects(avatar, "severed", "Severed", [{ type: "incoming-weapon-damage-percent", amount: exposed }, { type: "incoming-spell-damage-percent", amount: exposed }].map(entry => ({ ...entry, durationTurns: 1e7, unpreventable: true, unremovable: true })));
    avatar.effects.filter(effect => String(effect.key || "").startsWith("severed:")).forEach(effect => { effect.severed = true; });
  }
  // Runs at the start of every turn: fallen Cores are noted, a lone fallen Core regrows in time,
  // and when every Core is down the avatar is Severed until they all regrow.
  function heartCoreTurn(next) {
    if (!battle.heartCores) {
      const avatar = battle.actors.find(actor => actor.team === "enemy" && !actor.summoned && hp(actor) > 0 && heartCoresOf(actor));
      battle.heartCores = avatar ? { avatarId: avatar.id, fallen: {}, severedUntil: null, severings: 0, regrown: 0, ended: false } : { ended: true };
    }
    const current = heartCoreState();
    if (!current) return;
    const { state, avatar, config, cores } = current;
    if (hp(avatar) <= 0) {
      setSevered(current, false);
      state.ended = true;
      stage("order", { order: null, name: null });
      return;
    }
    for (const core of cores) if (hp(core) <= 0 && state.fallen[core.id] == null) state.fallen[core.id] = avatar.turns;
    const down = cores.filter(core => hp(core) <= 0);
    if (state.severedUntil == null && cores.length && down.length === cores.length) {
      state.severedUntil = (next.id === avatar.id ? avatar.turns : avatar.turns + 1) + config.severed.turns - 1;
      state.severings += 1;
      setSevered(current, true);
      log(`Every Core has fallen! ${avatar.name} is Severed from the land: for his next ${config.severed.turns} turns he takes ${config.severed.damageTakenPercent}% more damage and has ${config.severed.apLoss} fewer AP.`);
      stage("order", { order: "severed", name: "Severed" });
      window.Music?.sync();
    }
    if (next.id !== avatar.id) return;
    if (state.severedUntil != null) {
      if (avatar.turns > state.severedUntil) {
        state.severedUntil = null;
        setSevered(current, false);
        for (const core of cores.filter(core => hp(core) <= 0)) { regrowCore(current, core); state.regrown += 1; }
        log(`The roots knit together: the Cores regrow and ${avatar.name} is whole again.`);
        stage("order", { order: null, name: null });
        window.Music?.sync();
      } else {
        next.staggerAp = Math.max(toInteger(next.staggerAp, 0), config.severed.apLoss);
        next.staggerReason = "is cut off from the land";
      }
      return;
    }
    for (const core of down) if (avatar.turns - state.fallen[core.id] >= config.regrowTurns) { regrowCore(current, core); state.regrown += 1; }
  }

  /* ---------------- Infernal sacrifice ----------------
     A boss with "infernalSacrifice" (Archdevil Saffron) feeds on his Servants. At the start of
     his turn he consumes the weakest Servant below "belowPercent" of its HP: he heals
     "healPercent" of his max HP and gains a stacking Infernal Power ("powerPercent" spell power
     each). A hell-rift returns a sacrificed Servant at full HP "returnTurns" of his turns later;
     Servants the heroes kill outright stay dead. A sacrificed Servant awards nothing until the
     heroes kill it themselves. */
  const sacrificeOf = actor => actor?.team === "enemy" && actor.enemyDefinitionId ? enemyDefinitions.find(definition => definition.id === actor.enemyDefinitionId)?.infernalSacrifice || null : null;
  function sacrificeState() {
    const state = battle?.sacrifice;
    if (!state || state.ended) return null;
    const lord = actorById(state.lordId), config = lord ? sacrificeOf(lord) : null;
    if (!config) return null;
    const servants = battle.actors.filter(actor => actor.team === lord.team && !actor.summoned && config.servants.includes(actor.enemyDefinitionId));
    return { state, lord, config, servants };
  }
  const servantName = actor => actor.name.replace(/^Great Devil Servant - /, "");
  function setInfernalPower(current) {
    const { state, lord, config } = current;
    lord.effects = lord.effects.filter(effect => !effect.infernal);
    if (!state.stacks) return;
    applyTimedEffects(lord, "infernal-power", `Infernal Power (${state.stacks})`, [{ type: "spell-power-percent", amount: config.powerPercent * state.stacks, durationTurns: 1e7, unpreventable: true, unremovable: true }]);
    lord.effects.filter(effect => String(effect.key || "").startsWith("infernal-power:")).forEach(effect => { effect.infernal = true; });
  }
  function sacrificeTurn(next) {
    if (!battle.sacrifice) {
      const lord = battle.actors.find(actor => actor.team === "enemy" && !actor.summoned && hp(actor) > 0 && sacrificeOf(actor));
      battle.sacrifice = lord ? { lordId: lord.id, consumed: {}, stacks: 0, sacrifices: 0, returns: 0, ended: false } : { ended: true };
    }
    const current = sacrificeState();
    if (!current) return;
    const { state, lord, config, servants } = current;
    if (hp(lord) <= 0) { state.ended = true; stage("order", { order: null, name: null }); return; }
    if (next.id !== lord.id) return;
    // The hell-rifts open first: Servants consumed long enough ago return at full strength.
    for (const servant of servants) {
      const consumed = state.consumed[servant.id];
      if (consumed == null || lord.turns - consumed < config.returnTurns) continue;
      delete state.consumed[servant.id];
      servant.effects = []; servant.alive = true; servant.counter = 0; servant.sacrificed = false; servant.experienceAwarded = false;
      const maximum = values(servant);
      servant.character.currentHp = maximum.maxHp; servant.character.currentMana = maximum.maxMana;
      state.returns += 1;
      log(`A hell-rift tears open: ${actorLabel(servant)} steps back out of the fire.`);
      stage("status", { actorId: servant.id, label: "Returned" });
    }
    // Then he feeds on the weakest Servant that has fallen low.
    const prey = servants.filter(servant => hp(servant) > 0 && hp(servant) < values(servant).maxHp * config.belowPercent / 100)
      .sort((left, right) => hp(left) / values(left).maxHp - hp(right) / values(right).maxHp)[0];
    if (!prey) return;
    state.consumed[prey.id] = lord.turns;
    state.stacks += 1; state.sacrifices += 1;
    prey.sacrificed = true; prey.experienceAwarded = true;
    prey.character.currentHp = 0; prey.alive = false;
    phaseHealingDepth += 1;
    try { healHp(lord, Math.max(1, Math.floor(values(lord).maxHp * config.healPercent / 100))); } finally { phaseHealingDepth -= 1; }
    setInfernalPower(current);
    log(`${lord.name} drains ${actorLabel(prey)} into the hellfire: he heals and his spells burn hotter (Infernal Power ${state.stacks}). A rift will return it in ${config.returnTurns} of his turns.`);
    stage("status", { actorId: lord.id, label: `Infernal Power ${state.stacks}` });
    stage("status", { actorId: prey.id, label: "Sacrificed", debuff: true });
  }

  /* ---------------- The phylactery ----------------
     A boss with a "phylactery" (Ancient Lich Ool) keeps his soul in a vessel on the battlefield:
     an inert actor ("enemyId") that never acts. Its turns only count the rounds as they pass.
     While it stands he cannot truly die: at 0 HP he crumbles to dust and re-forms
     "reformRounds" rounds later with "reformHpPercent" of his health. His "guards" throw
     themselves before every single-target attack on the vessel; area attacks still reach it.
     When it breaks he is mortal and weakened ("broken" effects), and "cinematic" plays; if he
     is dust at that moment, he stays dust. The vessel takes the place of the Lich class's
     Immortal Body. */
  const phylacteryOf = actor => actor?.team === "enemy" && actor.enemyDefinitionId ? enemyDefinitions.find(definition => definition.id === actor.enemyDefinitionId)?.phylactery || null : null;
  function phylacteryState() {
    const state = battle?.phylactery;
    if (!state || state.ended) return null;
    const lich = actorById(state.lichId), vessel = actorById(state.vesselId), config = lich ? phylacteryOf(lich) : null;
    if (!config || !vessel) return null;
    return { state, lich, vessel, config };
  }
  const phylacteryGuards = current => living().filter(actor => actor.team === current.lich.team && current.config.guards.some(id => [actor.enemyDefinitionId, actor.summonDefinitionId].includes(id)));
  // While his phylactery stands, a fallen lich is only dust: he awards nothing yet.
  function phylacteryHolds(actor) {
    const current = phylacteryState();
    return Boolean(current && actor.id === current.lich.id && !current.state.broken && hp(current.vessel) > 0);
  }
  // The grave-knight who throws itself before a single-target blow meant for the vessel: the
  // one with the most health left.
  function phylacteryGuardFor(target, eligible, area = false) {
    const current = area ? null : phylacteryState();
    if (!current || current.state.broken || target.id !== current.vessel.id) return null;
    return eligible.filter(candidate => current.config.guards.some(id => [candidate.enemyDefinitionId, candidate.summonDefinitionId].includes(id)))
      .sort((left, right) => hp(right) - hp(left))[0] || null;
  }
  function turnToDust(current) {
    const { state, lich, vessel, config } = current;
    if (state.dustSince != null) return;
    state.dustSince = vessel.turns; state.deaths += 1;
    lich.alive = false;
    log(`${actorLabel(lich)} crumbles to dust, but his soul is bound to the phylactery: he re-forms in ${config.reformRounds} round${config.reformRounds === 1 ? "" : "s"} unless it breaks.`);
    stage("status", { actorId: vessel.id, label: "Soul-fire flares" });
  }
  function reformLich(current) {
    const { state, lich, config } = current;
    const maximum = values(lich);
    lich.effects = []; lich.alive = true; lich.counter = 0;
    lich.character.currentHp = Math.max(1, Math.floor(maximum.maxHp * config.reformHpPercent / 100));
    lich.character.currentMana = Math.max(mana(lich), Math.floor(maximum.maxMana * config.reformHpPercent / 100));
    state.dustSince = null; state.reforms += 1;
    log(`Green soul-fire pours out of the phylactery: ${actorLabel(lich)} re-forms from his dust with ${config.reformHpPercent}% of his health.`);
    stage("status", { actorId: lich.id, label: "Re-formed" });
  }
  function shatterPhylactery(current) {
    const { state, lich, vessel, config } = current;
    if (state.broken) return;
    state.broken = true;
    vessel.effects = [];
    if (hp(lich) <= 0) {
      // Dust when the vessel breaks: he stays dust, and the heroes are owed his defeat.
      state.ended = true;
      log(`The phylactery shatters! ${lich.name}'s soul screams out of the broken crystal and scatters. His dust never stirs again.`);
    } else {
      applyTimedEffects(lich, "phylactery-broken", "Mortal", (config.broken?.effects || []).map(entry => ({ ...entry, durationTurns: 1e7, unpreventable: true, unremovable: true })));
      lich.effects.filter(effect => String(effect.key || "").startsWith("phylactery-broken:")).forEach(effect => { effect.mortal = true; });
      log(`The phylactery shatters! ${lich.name}'s soul screams out of the broken crystal: he is mortal now, and weakened.`);
      stage("status", { actorId: lich.id, label: "Mortal", debuff: true });
    }
    if (config.cinematic) playPhaseIntro(lich, config.cinematic);
    window.Music?.sync();
  }
  // A fall is logged here, so that a lich held by his phylactery turns to dust instead and the
  // vessel's own fall breaks it at once.
  function logFall(target) {
    const current = phylacteryState();
    if (current && target.id === current.vessel.id) { log(`${actorLabel(target)} falls.`); shatterPhylactery(current); return; }
    if (current && target.id === current.lich.id && phylacteryHolds(target)) { turnToDust(current); return; }
    const stellar = starState();
    if (stellar && target.id === stellar.star.id) endStarTrials(stellar);
    // Made mortal, he simply dies.
    if (current && target.id === current.lich.id && current.state.broken) current.state.ended = true;
    log(`${actorLabel(target)} falls.`);
    noteFall(target);
  }
  // Runs at the start of every turn: notes a fall or a break by any other path, ends with the
  // lich, and on each of the vessel's turns (one round) brings dust nearer to re-forming.
  function phylacteryTurn(next) {
    if (!battle.phylactery) {
      const lich = battle.actors.find(actor => actor.team === "enemy" && !actor.summoned && hp(actor) > 0 && phylacteryOf(actor));
      const vessel = lich ? battle.actors.find(actor => actor.team === lich.team && actor.enemyDefinitionId === phylacteryOf(lich).enemyId && hp(actor) > 0) : null;
      battle.phylactery = vessel ? { lichId: lich.id, vesselId: vessel.id, dustSince: null, deaths: 0, reforms: 0, broken: false, ended: false } : { ended: true };
    }
    const current = phylacteryState();
    if (!current) return;
    const { state, lich, vessel, config } = current;
    if (!state.broken && hp(vessel) <= 0) shatterPhylactery(current);
    if (state.ended) return;
    if (state.broken) {
      if (hp(lich) <= 0) state.ended = true;
      return;
    }
    if (hp(lich) <= 0 && state.dustSince == null) turnToDust(current);
    if (next.id !== vessel.id || state.dustSince == null) return;
    const passed = vessel.turns - state.dustSince;
    if (passed >= config.reformRounds) { reformLich(current); return; }
    const left = config.reformRounds - passed;
    log(`The phylactery pulses with green soul-fire: ${lich.name}'s dust stirs. He re-forms in ${left} round${left === 1 ? "" : "s"}.`);
  }

  /* ---------------- Dream laws ----------------
     A boss with "dreamLaws" (Ur-Mat) dreams the battle, and rewrites its law every "everyTurns" of
     his turns in a fixed cycle that starts at a random law. A law's "foes" effects bind the heroes
     and its "allies" effects his side; "apLoss" costs every hero AP each turn. The next law is
     foretold a turn ahead. After his phase change ("doubleAfterPhase") two laws hold at once: the
     current one and the next. Laws use their own effect types: "spell-rebound-percent" (a hero's
     spell damage partly returns to the caster), "inverted-mercy" (healing changed by "amount"
     percent, and every heal wounds by "hp" percent of it), "skills-sealed" (no class skills) and
     "spell-echo-percent" (damaging spells strike again at that power). */
  const dreamLawsOf = actor => actor?.team === "enemy" && actor.enemyDefinitionId ? enemyDefinitions.find(definition => definition.id === actor.enemyDefinitionId)?.dreamLaws || null : null;
  function dreamState() {
    const state = battle?.dream;
    if (!state || state.ended) return null;
    const dreamer = actorById(state.dreamerId), config = dreamer ? dreamLawsOf(dreamer) : null;
    if (!config) return null;
    const count = config.laws.length, at = index => config.laws[((index % count) + count) % count];
    const double = Boolean(config.doubleAfterPhase) && toInteger(dreamer.phaseIndex, 0) >= 1;
    const span = double ? 2 : 1;
    const laws = Array.from({ length: span }, (_, offset) => at(state.index + offset));
    const next = Array.from({ length: span }, (_, offset) => at(state.index + 1 + offset));
    return { state, dreamer, config, laws, next, double };
  }
  const lawNames = laws => laws.map(law => law.name).join(" and ");
  function applyLawsTo(recipient) {
    recipient.effects = recipient.effects.filter(effect => !effect.dreamLaw);
    const current = dreamState();
    recipient.dreamSerial = current ? current.state.serial : null;
    if (!current || hp(recipient) <= 0) return;
    const foe = recipient.team !== current.dreamer.team;
    for (const law of current.laws) {
      const key = `dream-law:${law.id}`;
      applyTimedEffects(recipient, key, law.name, ((foe ? law.foes : law.allies) || []).map(entry => ({ ...entry, durationTurns: 1e7, unpreventable: true, unremovable: true })));
      recipient.effects.filter(effect => String(effect.key || "").startsWith(key + ":")).forEach(effect => { effect.dreamLaw = true; });
    }
  }
  function writeLaw(index) {
    const dreamer = actorById(battle.dream.dreamerId);
    Object.assign(battle.dream, { index, startTurn: dreamer.turns, foretold: false, serial: toInteger(battle.dream.serial, 0) + 1, written: toInteger(battle.dream.written, 0) + 1 });
    const current = dreamState();
    log(`${dreamer.name} rewrites the dream: ${lawNames(current.laws)}. ${current.laws.map(law => law.description || "").join(" ")}`.trim());
    stage("season", { season: current.laws[0].id, name: lawNames(current.laws) });
    for (const recipient of living()) applyLawsTo(recipient);
  }
  function endDream() {
    const current = dreamState();
    if (!current) return;
    current.state.ended = true;
    for (const actor of battle.actors) { actor.effects = actor.effects.filter(effect => !effect.dreamLaw); actor.dreamSerial = null; }
    log(`With ${current.dreamer.name} fallen, the dream ends and its laws unravel.`);
    stage("season", { season: null, name: null });
  }
  // Runs at the start of every turn: the first law is written, a new law every few of his turns,
  // the next one foretold; heroes under a law that steals time lose AP.
  function dreamTurn(next) {
    if (!battle.dream) {
      const dreamer = battle.actors.find(actor => actor.team === "enemy" && !actor.summoned && hp(actor) > 0 && dreamLawsOf(actor));
      if (!dreamer) { battle.dream = { ended: true }; return; }
      const config = dreamLawsOf(dreamer);
      battle.dream = { dreamerId: dreamer.id, index: 0, startTurn: 0, foretold: false, serial: 0, written: 0, doubled: false, ended: false };
      writeLaw(config.randomStart ? Math.floor(Math.random() * config.laws.length) : 0);
      // The first law counts from his first turn, whoever acts first.
      battle.dream.startTurn = next.id === dreamer.id ? dreamer.turns : dreamer.turns + 1;
    }
    let current = dreamState();
    if (!current) return;
    const { state, dreamer, config } = current;
    if (hp(dreamer) <= 0) { endDream(); return; }
    // His awakening (Lucid Change) doubles the dream: the next law joins the current one at once.
    if (current.double && !state.doubled) {
      state.doubled = true; state.serial += 1;
      log(`The dream doubles: ${lawNames(current.laws)} now hold together.`);
      stage("season", { season: current.laws[0].id, name: lawNames(current.laws) });
      for (const recipient of living()) applyLawsTo(recipient);
    }
    if (next.id === dreamer.id) {
      const elapsed = dreamer.turns - state.startTurn;
      if (elapsed >= config.everyTurns) writeLaw(state.index + 1);
      else if (elapsed === config.everyTurns - 1 && !state.foretold) {
        state.foretold = true;
        log(`The dream stirs: ${lawNames(current.next)} will be written at ${dreamer.name}'s next turn.`);
        stage("season-omen", { season: current.next[0].id, name: lawNames(current.next) });
      }
      return;
    }
    if (next.dreamSerial !== state.serial) applyLawsTo(next);
    current = dreamState();
    const apLoss = next.team !== dreamer.team ? Math.max(0, ...current.laws.map(law => toInteger(law.apLoss, 0))) : 0;
    if (apLoss > 0) {
      next.staggerAp = Math.max(toInteger(next.staggerAp, 0), apLoss);
      next.staggerReason = "is slowed by the dream";
    }
  }
  const lawAmount = (actor, type) => (actor?.effects || []).filter(effect => effect.type === type && effect.expiresTurn > actor.turns).reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
  let mercyDepth = 0, reboundDepth = 0;

  /* ---------------- The star trials ----------------
     A boss with "starTrials" (Anatemis) tests the heroes with one trial per form, and each form
     keeps the trials before it:
     - "mirror" (from the start): her Mirror Scales throw "reflectPercent" of every hero's spell
       damage back at the caster, until weapon blows deal her "breakPercent" of her max HP; then they
       shatter and reform "reformTurns" of her turns later.
     - "sunfall" (from phase "fromPhase"): at each of her turns she marks a hero; at her next turn
       the sun falls on them for "damagePercent" of their max HP, unless the party has dealt her
       "deflectPercent" of her max HP since the mark.
     - "supernova" (from phase "fromPhase"): she gathers a supernova over "chargeTurns" of her turns;
       it strikes every hero for "damagePercent" of their max HP, unless the party deals her
       "collapsePercent" of her max HP first, which collapses it and costs her "staggerAp" AP. */
  const starTrialsOf = actor => actor?.team === "enemy" && actor.enemyDefinitionId ? enemyDefinitions.find(definition => definition.id === actor.enemyDefinitionId)?.starTrials || null : null;
  function starState() {
    const state = battle?.star;
    if (!state || state.ended) return null;
    const star = actorById(state.starId), config = star ? starTrialsOf(star) : null;
    if (!config) return null;
    const phase = toInteger(star.phaseIndex, 0);
    return { state, star, config, phase, sunfall: Boolean(config.sunfall) && phase >= toInteger(config.sunfall.fromPhase, 1), supernova: Boolean(config.supernova) && phase >= toInteger(config.supernova.fromPhase, 2) };
  }
  const starHeroes = star => living().filter(actor => actor.team !== star.team && !actor.summoned);
  const starShare = (current, percent) => Math.max(1, Math.floor(values(current.star).maxHp * toNumber(percent, 0) / 100));
  // A trial's blow: a share of each hero's own maximum HP, which no armor or ward can soften.
  function starStrike(current, hero, percent, element, label) {
    const dealt = directDamage(hero, Math.max(1, Math.floor(values(hero).maxHp * toNumber(percent, 0) / 100)), false, element, current.star, true);
    log(`${label} strikes ${actorLabel(hero)} for ${dealt}.`);
    if (onStage(hero)) stage("status", { actorId: hero.id, label, debuff: true });
    return dealt;
  }
  function setMirror(current, up) {
    const { state, star, config } = current;
    state.mirrorUp = up; state.mirrorDamage = 0;
    if (up) { state.mirrorBrokenAt = null; state.mirrorReforms += 1; log(`${star.name}'s Mirror Scales reform: her scales gleam like a sheet of starlit glass.`); stage("status", { actorId: star.id, label: "Mirror Scales" }); }
    else { state.mirrorBrokenAt = star.turns; state.mirrorShatters += 1; log(`The Mirror Scales shatter under the heroes' blows! For ${config.mirror.reformTurns} of her turns, spells reach ${star.name} unreflected.`); stage("status", { actorId: star.id, label: "Mirror shattered", debuff: true }); }
  }
  function markSunfall(current) {
    const { state, star } = current;
    for (const hero of battle.actors) hero.effects = hero.effects.filter(effect => effect.type !== "sunfall-mark");
    const heroes = starHeroes(star), pool = heroes.filter(hero => hero.id !== state.sunfallTarget);
    const hero = (pool.length ? pool : heroes)[Math.floor(Math.random() * (pool.length || heroes.length))];
    Object.assign(state, { sunfallTarget: hero?.id || null, sunfallDamage: 0 });
    if (!hero) return;
    hero.effects.push({ key: "star:sunfall", name: "Marked by Sunfall", type: "sunfall-mark", amount: 1, expiresTurn: Number.MAX_SAFE_INTEGER, unremovable: true });
    log(`${star.name} marks ${actorLabel(hero)} for Sunfall: the sun will fall at her next turn, unless the heroes deal her ${starShare(current, current.config.sunfall.deflectPercent)} damage first.`);
    if (onStage(hero)) stage("status", { actorId: hero.id, label: "Sunfall", debuff: true });
  }
  function collapseSupernova(current) {
    const { state, star, config } = current;
    Object.assign(state, { novaStart: null, novaDamage: 0 });
    state.novaCollapses += 1;
    star.staggerAp = Math.max(toInteger(star.staggerAp, 0), Math.max(0, toInteger(config.supernova.staggerAp, 2)));
    star.staggerReason = "reels as her supernova collapses";
    log(`The heroes break her concentration: the supernova collapses into her, and ${star.name} reels!`);
    stage("status", { actorId: star.id, label: "Supernova collapses", debuff: true });
  }
  // Every blow against her counts toward deflecting the Sunfall and collapsing the supernova.
  function creditStarDamage(target, attacker, dealt) {
    const current = starState();
    if (!current || !attacker || dealt <= 0 || target.id !== current.star.id) return;
    const credited = attacker.summoned ? actorById(attacker.summonerId) || attacker : attacker;
    if (credited.team === current.star.team) return;
    const { state, config } = current;
    if (state.sunfallTarget) state.sunfallDamage += dealt;
    if (state.novaStart != null) {
      state.novaDamage += dealt;
      if (state.novaDamage >= starShare(current, config.supernova.collapsePercent)) collapseSupernova(current);
    }
  }
  // The Mirror Scales: spells come back at their caster; weapon blows wear the mirror down.
  function mirrorScales(target, attacker, amount, spell, attackKind) {
    const current = starState();
    if (!current || !attacker || amount <= 0 || target.id !== current.star.id || !current.state.mirrorUp || attacker.team === target.team) return;
    const { state, config } = current;
    if (spell && !reboundDepth && hp(attacker) > 0) {
      reboundDepth += 1;
      try {
        const back = directDamage(attacker, Math.max(1, Math.floor(amount * toNumber(config.mirror.reflectPercent, 0) / 100)), false, "basic", target);
        if (back > 0) log(`The Mirror Scales throw ${back} damage back at ${actorLabel(attacker)}.`);
      } finally { reboundDepth -= 1; }
    } else if (["melee", "ranged"].includes(attackKind)) {
      state.mirrorDamage += amount;
      if (state.mirrorDamage >= starShare(current, config.mirror.breakPercent)) setMirror(current, false);
    }
  }
  function endStarTrials(current) {
    for (const hero of battle.actors) hero.effects = hero.effects.filter(effect => effect.type !== "sunfall-mark");
    current.state.ended = true;
    stage("order", { order: null, name: null });
  }
  // Runs at the start of every turn; her own turns drive the trials.
  function starTurn(next) {
    if (!battle.star) {
      const star = battle.actors.find(actor => actor.team === "enemy" && !actor.summoned && hp(actor) > 0 && starTrialsOf(actor));
      battle.star = star ? { starId: star.id, mirrorUp: Boolean(starTrialsOf(star).mirror), mirrorDamage: 0, mirrorBrokenAt: null, mirrorShatters: 0, mirrorReforms: 0, sunfallTarget: null, sunfallDamage: 0, sunfalls: 0, deflections: 0, novaStart: null, novaDamage: 0, novaCollapses: 0, detonations: 0, ended: false } : { ended: true };
    }
    const current = starState();
    if (!current) return;
    const { state, star, config } = current;
    if (hp(star) <= 0) { endStarTrials(current); return; }
    if (next.id !== star.id) return;
    if (config.mirror && !state.mirrorUp && star.turns - state.mirrorBrokenAt >= config.mirror.reformTurns) setMirror(current, true);
    if (current.sunfall) {
      const marked = state.sunfallTarget ? actorById(state.sunfallTarget) : null;
      if (marked && hp(marked) > 0) {
        if (state.sunfallDamage >= starShare(current, config.sunfall.deflectPercent)) { state.deflections += 1; log(`The heroes' blows turn the Sunfall aside from ${actorLabel(marked)}.`); }
        else { state.sunfalls += 1; starStrike(current, marked, config.sunfall.damagePercent, "holy", "Sunfall"); }
      }
      if (isActive()) markSunfall(current);
    }
    if (current.supernova && isActive()) {
      if (state.novaStart == null) {
        Object.assign(state, { novaStart: star.turns, novaDamage: 0 });
        log(`${star.name} begins to gather a supernova. In ${config.supernova.chargeTurns} of her turns it will strike every hero, unless they deal her ${starShare(current, config.supernova.collapsePercent)} damage first.`);
        stage("status", { actorId: star.id, label: "Supernova" });
      } else if (star.turns - state.novaStart >= config.supernova.chargeTurns) {
        state.detonations += 1;
        Object.assign(state, { novaStart: null, novaDamage: 0 });
        log(`The supernova detonates!`);
        for (const hero of starHeroes(star)) if (isActive() && hp(hero) > 0) starStrike(current, hero, config.supernova.damagePercent, "basic", "The supernova");
      }
    }
  }

  /* ---------------- The royal duel ----------------
     A boss with a "royalDuel" (the Deep Sea King) fights one champion at a time. At the start of
     his turn, when no duel stands, he challenges the hero who dealt him the most damage since
     his last duel. The duel lasts "durationTurns" of his turns or until the champion falls. In it
     he strikes only his champion; the other heroes still reach him, but deal less damage. A
     fallen champion lifts him (he heals and gains Triumph); a champion who survives is honored. */
  const royalDuelOf = actor => actor?.team === "enemy" && actor.enemyDefinitionId ? enemyDefinitions.find(definition => definition.id === actor.enemyDefinitionId)?.royalDuel || null : null;
  function royalDuelState() {
    const state = battle?.royalDuel;
    if (!state || state.ended) return null;
    const king = actorById(state.kingId);
    return king ? { state, king, config: royalDuelOf(king) } : null;
  }
  // The champion a king in a duel is locked onto (he can reach no one else).
  function royalDuelPartner(actor, state = battle) {
    const duel = state?.royalDuel;
    if (!duel || duel.ended || duel.kingId !== actor?.id || !duel.championId) return null;
    const champion = actorById(duel.championId, state);
    return champion && hp(champion) > 0 ? champion : null;
  }
  // Damage from anyone but the champion is reduced while a duel stands.
  function royalDuelDamagePercent(target, attacker) {
    const current = royalDuelState();
    if (!current || !attacker || target.id !== current.king.id || !current.state.championId) return 0;
    const credited = attacker.summoned ? actorById(attacker.summonerId) || attacker : attacker;
    return credited.id === current.state.championId ? 0 : toNumber(current.config.outsideDamagePercent, 0);
  }
  // Remembers who hurt the king, so he can challenge the heaviest hitter next.
  function creditRoyalDuelDamage(target, attacker, dealt) {
    const current = royalDuelState();
    if (!current || !attacker || dealt <= 0 || target.id !== current.king.id) return;
    const credited = attacker.summoned ? actorById(attacker.summonerId) || attacker : attacker;
    if (credited.team === current.king.team) return;
    current.state.damageBy[credited.id] = toNumber(current.state.damageBy[credited.id], 0) + dealt;
  }
  function endRoyalDuel(current, survived) {
    const { state, king, config } = current, champion = actorById(state.championId);
    king.effects = king.effects.filter(effect => !effect.royalDuel);
    if (champion) champion.effects = champion.effects.filter(effect => !effect.royalDuel);
    if (champion && survived && hp(champion) > 0) {
      applyTimedEffects(champion, "royal-honor", "Honored by the King", (config.honor || []).map(entry => ({ ...entry, durationTurns: toInteger(config.honorTurns, 2) })));
      log(`${actorLabel(champion)} stands to the end of the royal duel, and ${king.name} honors the champion.`);
      stage("status", { actorId: champion.id, label: "Honored" });
    } else if (champion) {
      const triumph = config.triumph || {};
      state.triumphs = Math.min(Math.max(1, toInteger(triumph.maxStacks, 5)), state.triumphs + 1);
      applyTimedEffects(king, "royal-triumph", `Triumph (${state.triumphs})`, [{ type: "outgoing-attack-damage-percent", amount: toNumber(triumph.damagePercent, 0) * state.triumphs, attackKinds: ["melee", "ranged"], durationTurns: 1e7, unpreventable: true, unremovable: true }]);
      // The triumph is the duel's own reward, not ordinary healing: it is not reduced.
      phaseHealingDepth += 1;
      try { healHp(king, Math.floor(values(king).maxHp * toNumber(triumph.healPercent, 0) / 100)); } finally { phaseHealingDepth -= 1; }
      log(`${actorLabel(champion)} falls in the royal duel. ${king.name} is lifted by his victory: Triumph (${state.triumphs}).`);
      stage("status", { actorId: king.id, label: `Triumph ${state.triumphs}` });
    }
    Object.assign(state, { championId: null, endsTurn: null });
    stage("order", { order: null, name: null });
  }
  function challenge(current) {
    const { state, king, config } = current;
    const heroes = living().filter(actor => actor.team !== king.team && !actor.summoned);
    if (!heroes.length) return;
    // The heaviest hitter since the last duel; before anyone has hurt him, the toughest hero.
    const champion = heroes.sort((left, right) => toNumber(state.damageBy[right.id], 0) - toNumber(state.damageBy[left.id], 0) || values(right).maxHp - values(left).maxHp)[0];
    Object.assign(state, { championId: champion.id, endsTurn: king.turns + Math.max(1, toInteger(config.durationTurns, 2)), damageBy: {}, duels: state.duels + 1 });
    king.effects.push({ key: "royal-duel:king", name: "Royal Duel", type: "royal-duel", partnerId: champion.id, amount: 1, expiresTurn: Number.MAX_SAFE_INTEGER, royalDuel: true, unremovable: true });
    champion.effects.push({ key: "royal-duel:champion", name: `Champion of ${king.name}'s duel`, type: "royal-duel-champion", partnerId: king.id, amount: 1, expiresTurn: Number.MAX_SAFE_INTEGER, royalDuel: true, unremovable: true });
    log(`${king.name} levels the Royal Trident at ${actorLabel(champion)}: a royal duel for his next ${Math.max(1, toInteger(config.durationTurns, 2))} turns! He will strike only his champion, and every other blow against him loses ${Math.abs(toNumber(config.outsideDamagePercent, 0))}% of its force.`);
    stage("order", { order: "duel", name: "Royal Duel", chiefId: king.id, preyId: champion.id });
  }
  // Runs at the start of every turn in a battle with a dueling king.
  function royalDuelTurn(next) {
    if (!battle.royalDuel) {
      const king = battle.actors.find(actor => actor.team === "enemy" && !actor.summoned && hp(actor) > 0 && royalDuelOf(actor));
      if (!king) { battle.royalDuel = { ended: true }; return; }
      battle.royalDuel = { kingId: king.id, championId: null, endsTurn: null, damageBy: {}, duels: 0, triumphs: 0, ended: false };
    }
    const current = royalDuelState();
    if (!current) return;
    const { state, king } = current;
    if (hp(king) <= 0) {
      for (const actor of battle.actors) actor.effects = actor.effects.filter(effect => !effect.royalDuel);
      state.ended = true;
      stage("order", { order: null, name: null });
      return;
    }
    // A champion who fell lifts the king at once.
    if (state.championId && championFallen(state)) endRoyalDuel(current, false);
  }
  // A champion who left the battle counts as fallen.
  const championFallen = state => { const champion = actorById(state.championId); return !champion || hp(champion) <= 0; };
  /* The king names his champion at the end of his turn, so the party has a round to prepare
     them; the duel covers his next turns. When a duel ends he names the next champion at once. */
  function royalDuelTurnEnd(actor) {
    const current = royalDuelState();
    if (!current || actor?.id !== current.king.id || hp(actor) <= 0) return;
    const { state } = current;
    if (state.championId && championFallen(state)) endRoyalDuel(current, false);
    else if (state.championId && actor.turns >= state.endsTurn) endRoyalDuel(current, true);
    if (!state.championId) challenge(current);
  }

  /* ---------------- Old wounds ----------------
     A boss with "oldWounds" (Nygheir) carries scars that critical hits tear open. Each open
     wound bleeds him at the start of his turns and makes him take more damage, but rage makes
     him hit harder. Stonecrust seals a wound again. When he reaches his burst phase every
     wound tears open and burns: no more bleeding, and they can no longer be sealed. */
  const oldWoundsOf = actor => actor?.team === "enemy" && actor.enemyDefinitionId ? enemyDefinitions.find(definition => definition.id === actor.enemyDefinitionId)?.oldWounds || null : null;
  function setOpenWounds(actor, count) {
    const wounds = oldWoundsOf(actor);
    if (!wounds) return;
    actor.openWounds = Math.max(0, Math.min(wounds.count, toInteger(count, 0)));
    actor.effects = actor.effects.filter(effect => !effect.oldWound);
    const open = actor.openWounds;
    if (!open) return;
    const name = actor.woundsBurst ? `Burning Scars (${open}/${wounds.count})` : `Old Wounds (${open}/${wounds.count})`;
    const effects = [
      { type: "incoming-weapon-damage-percent", amount: wounds.damageTakenPercent * open },
      { type: "incoming-spell-damage-percent", amount: wounds.damageTakenPercent * open },
      // The rage drives his claws, not his spells.
      { type: "outgoing-attack-damage-percent", amount: wounds.rageDamagePercent * open, attackKinds: ["melee", "ranged"] },
      ...(actor.woundsBurst || !wounds.bleedPercent ? [] : [{ type: "turn-start-max-hp-loss-percent", amount: wounds.bleedPercent * open }]),
    ];
    applyTimedEffects(actor, "old-wounds", name, effects.map(effect => ({ ...effect, durationTurns: 1e7, unpreventable: true, unremovable: true })));
    actor.effects.filter(effect => String(effect.key || "").startsWith("old-wounds:")).forEach(effect => { effect.oldWound = true; });
  }
  function tearOldWound(actor) {
    const wounds = oldWoundsOf(actor);
    if (!wounds || actor.woundsBurst || toInteger(actor.openWounds, 0) >= wounds.count) return;
    setOpenWounds(actor, toInteger(actor.openWounds, 0) + 1);
    log(`The blow tears open one of ${actor.name}'s old wounds (${actor.openWounds}/${wounds.count}): it bleeds and rages.`);
    if (onStage(actor)) stage("status", { actorId: actor.id, label: "Old wound torn open", debuff: true });
  }
  function burstOldWounds(actor) {
    const wounds = oldWoundsOf(actor);
    if (!wounds || actor.woundsBurst) return;
    actor.woundsBurst = true;
    setOpenWounds(actor, wounds.count);
    log(`Every old wound on ${actor.name} tears open and burns.`);
  }
  /* Turning Seasons (Rootbark): armor that cycles through its own seasons on its wearer,
     changing every few of the wearer's turns, Spring first. */
  function wearerSeasonTurn(next) {
    const cycle = withActor(next, () => equippedDefenseItems()).flatMap(item => (item.effects || []).map(effect => ({ effect, item }))).find(entry => entry.effect.kind === "turning-seasons");
    if (!cycle) { if (next.effects.some(effect => effect.wearerSeason)) next.effects = next.effects.filter(effect => !effect.wearerSeason); return; }
    const { effect, item } = cycle, seasons = effect.seasons || [];
    if (!seasons.length) return;
    const index = Math.floor(Math.max(0, next.turns - 1) / Math.max(1, effect.turns || 2)) % seasons.length, season = seasons[index];
    if (next.wearerSeasonIndex !== index || !next.effects.some(entry => entry.wearerSeason)) {
      next.effects = next.effects.filter(entry => !entry.wearerSeason);
      const name = `${item.name}: ${season.name}`;
      applyTimedEffects(next, `wearer-season:${season.id}`, name, (season.effects || []).map(entry => ({ ...entry, durationTurns: 1e7, unpreventable: true, unremovable: true })));
      next.effects.filter(entry => String(entry.key || "").startsWith(`wearer-season:${season.id}:`)).forEach(entry => { entry.wearerSeason = true; });
      if (!season.effects?.length) next.effects.push({ key: `wearer-season:${season.id}`, name, type: "wearer-season", amount: 0, wearerSeason: true, unremovable: true, expiresTurn: Number.MAX_SAFE_INTEGER });
      next.wearerSeasonIndex = index;
      log(`${actorLabel(next)}'s ${item.name} turns to ${season.name}.`);
    }
    if (season.regenHpPercent && hp(next) > 0 && !healingDisabled(next)) {
      const restored = healHp(next, Math.max(1, Math.floor(values(next).maxHp * season.regenHpPercent / 100)), battle, next);
      if (restored > 0) log(`${actorLabel(next)} regains ${restored} HP with ${item.name}.`);
    }
  }
  // Runs at the start of every turn of the real battle (never in forecasts).
  function seasonTurn(next) {
    if (!battle.season) {
      const owner = battle.actors.find(actor => actor.team === "enemy" && !actor.summoned && hp(actor) > 0 && seasonCycleOf(actor));
      if (!owner) { battle.season = { ended: true }; return; }
      battle.season = { ownerId: owner.id, index: -1, startTurn: 0, foretold: null, ended: false };
      beginSeason(owner, 0);
      // The first season counts from her first turn, whoever acts first.
      battle.season.startTurn = next.id === owner.id ? owner.turns : owner.turns + 1;
    }
    const current = seasonState();
    if (!current) return;
    const { owner, cycle } = current;
    if (hp(owner) <= 0) { endSeasons(owner); return; }
    if (next.id !== owner.id) { if (next.seasonIndex !== current.state.index) applySeasonTo(next); return; }
    const elapsed = owner.turns - current.state.startTurn;
    if (elapsed >= cycle.turns) beginSeason(owner, current.state.index + 1);
    else if (elapsed === cycle.turns - 1 && current.state.foretold !== current.next.id) {
      current.state.foretold = current.next.id;
      log(`${current.next.omen ? current.next.omen + " " : ""}${current.next.name} will come at ${owner.name}'s next turn.`);
      stage("season-omen", { season: current.next.id, name: current.next.name });
    }
    const season = seasonState().season;
    if (season.roots) {
      const heroes = living().filter(actor => actor.team !== owner.team), pool = heroes.filter(actor => !actor.summoned);
      const hero = (pool.length ? pool : heroes)[Math.floor(Math.random() * (pool.length || heroes.length))];
      if (hero) {
        hero.rootedAp = season.roots.ap;
        hero.effects = hero.effects.filter(effect => effect.type !== "rooted");
        hero.effects.push({ key: "season:roots", name: "Binding Roots", type: "rooted", amount: -season.roots.ap, seasonFoe: true, unremovable: true, expiresTurn: hero.turns + 1 });
        log(`Frozen roots bind ${actorLabel(hero)}: ${season.roots.ap} AP lost on their next turn.`);
        stage("status", { actorId: hero.id, label: "Rooted", debuff: true });
      }
    }
  }
  function randomAnimalCandidates(actor, effect = {}) {
    const actorLevel = Math.max(1, toInteger(actor.id === "player" ? level : actor.character.level, 1));
    const maximumLevel = Math.max(1, Math.floor(actorLevel * Math.max(0.1, toNumber(effect.maxLevelFactor, 0.5))));
    const races = new Set((effect.raceIds || ["wolf", "bear", "griffon"]).map(slugify));
    return enemyDefinitions.filter(definition =>
      definition.character.level <= maximumLevel &&
      races.has(slugify(definition.character.raceId)) &&
      toInteger(definition.character.classLevels?.beast, 0) > 0
    );
  }
  function summonRandomAnimals(actor, effect, source) {
    const candidates = randomAnimalCandidates(actor, effect);
    if (!candidates.length) return 0;
    let summoned = 0;
    for (let index = 0; index < Math.max(1, toInteger(effect.count, 1)); index += 1) {
      const chosen = candidates[Math.floor(Math.random() * candidates.length)];
      if (summonFriendly(actor, { type: "summon-friendly", enemyId: chosen.id, durationTurns: effect.durationTurns }, source)) summoned += 1;
    }
    return summoned;
  }
  function triggerLowHealthPassives(actor) {
    triggerLowHealthEquipment(actor);
    const maximumHp = values(actor).maxHp;
    if (maximumHp <= 0) return;
    for (const passive of passivesOf(actor)) {
      const effects = [...(Array.isArray(passive.effects) ? passive.effects : []), ...(passive.effect ? [passive.effect] : [])];
      for (const effect of effects.filter((entry) => entry.type === "low-health-random-summon")) {
        const threshold = Math.max(0, toNumber(effect.hpThresholdPercent, 10));
        if (hp(actor) >= maximumHp * threshold / 100) continue;
        const key = `passive:${passive.classId}:${passive.id}`;
        const states = skillStates(actor);
        if (states[key]?.lastUsedDay === todayKey()) continue;
        const actorLevel = actor.id === "player" ? level : actor.character.level;
        const maximumLevel = actorLevel * Math.max(0, toNumber(effect.maxLevelFactor, 0.5));
        const requiredClass = slugify(effect.classId || "beast");
        const candidates = enemyDefinitions.filter((definition) =>
          definition.character.level < maximumLevel && toInteger(definition.character.classLevels?.[requiredClass], 0) > 0
        );
        if (!candidates.length) continue;
        const chosen = candidates[Math.floor(Math.random() * candidates.length)];
        if (!summonFriendly(actor, { type: "summon-friendly", enemyId: chosen.id }, passive.name)) continue;
        states[key] = { active: false, lastUsedDay: todayKey() };
      }
    }
  }
  function triggerLowHealthEquipment(actor) {
    if (!isActive() || hp(actor) <= 0) return;
    const artifacts = withActor(actor, equippedArtifactItems).filter(item => item.effects.some(effect => effect.kind === "low-hp-summon"));
    if (!artifacts.length) return;
    for (const item of artifacts) {
      if (actor.equipmentSummons?.[item.id]) continue;
      for (const effect of item.effects.filter(effect => effect.kind === "low-hp-summon")) {
        if (hp(actor) >= values(actor).maxHp * effect.thresholdPercent / 100) continue;
        // Mark before summoning so allied-stat and damage hooks cannot recurse.
        (actor.equipmentSummons ||= {})[item.id] = true;
        let count = 0;
        for (let index = 0; index < effect.amount; index++) if (summonFriendly(actor, { type: "summon-friendly", enemyId: effect.enemyId, summonStrengthPercent: effect.strengthPercent }, item.name)) count++;
        if (!count) delete actor.equipmentSummons[item.id];
        break;
      }
    }
  }
  function isRemovableDebuff(effect) {
    if (effect.unremovable) return false;
    if (["stun", "hunter-mark", "delayed-hp-loss-percent", "turn-start-max-hp-loss-percent", "incoming-weapon-hit-always"].includes(effect.type)) return true;
    if (["stat-percent", "all-stats-percent", "defense-flat", "defense-percent", "stat-permanent-percent", "defense-permanent-percent", "weapon-hit-flat", "weapon-hit-percent"].includes(effect.type)) return toNumber(effect.amount, 0) < 0;
    if (["incoming-weapon-damage-percent", "incoming-spell-damage-percent", "incoming-attack-damage-percent", "incoming-damage-percent"].includes(effect.type)) return toNumber(effect.amount, 0) > 0;
    return false;
  }
  /* Armor that drinks spells: the Impshield absorbs a share of hostile Dark spells whole and
     returns the Mana their caster spent. Rolled once per cast for each hostile recipient;
     while the spell resolves, recipientsFor leaves out everyone who absorbed it. */
  let absorbedSpellRecipients = null;
  function rollSpellAbsorption(actor, target, effects, elements, cost, spell) {
    const absorbed = new Set();
    const hostile = [...new Set(effects.filter(effect => ["target", "all-enemies", "all-combatants"].includes(effect.recipient))
      .flatMap(effect => recipientsFor(actor, target, effect)))].filter(recipient => recipient && recipient.team !== actor.team && hp(recipient) > 0);
    for (const recipient of hostile) for (const item of withActor(recipient, equippedDefenseItems)) {
      const effect = (item.effects || []).find(entry => entry.kind === "absorb-element-spell-chance" && elements.includes(entry.element));
      if (!effect || !rules.roll(toNumber(effect.chance, 0))) continue;
      absorbed.add(recipient.id);
      const restored = Math.max(0, Math.min(values(recipient).maxMana - mana(recipient), cost));
      setMana(recipient, mana(recipient) + restored);
      stageEquipment(recipient, item);
      log(`${item.name} absorbs ${spell.name}${restored ? ` and restores ${restored} Mana to ${actorLabel(recipient)}` : ""}.`);
      break;
    }
    return absorbed;
  }
  function recipientsFor(actor, target, effect) {
    const list = baseRecipientsFor(actor, target, effect);
    return absorbedSpellRecipients ? list.filter(recipient => !recipient || recipient.team === actor.team || !absorbedSpellRecipients.has(recipient.id)) : list;
  }
  function baseRecipientsFor(actor, target, effect) {
    if (effect.recipient === "all-enemies") return [...opponents(actor)];
    if (effect.recipient === "all-allies") return living().filter(candidate => candidate.team === actor.team);
    if (effect.recipient === "all-combatants") return [...living()];
    if (effect.recipient === "friendly-target" || effect.recipient === "target") return target ? [target] : [];
    if (effect.recipient === "random-ally") {
      const allies = living().filter(candidate => candidate.team === actor.team);
      return allies.length ? [allies[Math.floor(Math.random() * allies.length)]] : [];
    }
    return [actor];
  }
  function forecastRecipients(actor, target, effect) {
    // A random recipient is an expectation over eligible allies, not a dice
    // roll made while considering an action.
    if (effect.recipient === "random-ally") return aiAllies(actor);
    return recipientsFor(actor, target, effect).filter(Boolean);
  }
  function requiresEnemyTarget(effects) {
    return effects.some(effect => effect.recipient === "target" ||
      effect.type === "damage" && effect.recipient !== "all-enemies" && effect.recipient !== "self" ||
      ["stun", "stun-chance", "instant-death-per-thousand"].includes(effect.type) && !["self", "all-enemies", "all-allies", "all-combatants"].includes(effect.recipient));
  }
  /* Lament (port task 5, M19, M20): a single-target spell, at an enemy or an ally, can take a second target for 50% more
     Mana. The second target gets the spell's targeted effects only, with its own miss roll. */
  const lamentTargeted = effect => ["target", "friendly-target"].includes(effect.recipient) || !effect.recipient && effect.type === "damage";
  const lamentEligible = effects => effects.some(lamentTargeted) && !effects.some(effect => ["all-enemies", "all-allies", "all-combatants", "random-ally"].includes(effect.recipient));
  function lamentEffect(actor) {
    return withActor(actor, equippedWeaponItems).flatMap(item => item.effects || []).find(effect => effect.kind === "extra-target-spell") || null;
  }
  function apexSpellEffects(effects, choice) {
    if (choice !== "sweep") return effects;
    return effects.map(effect => effect.recipient === "target" || !effect.recipient && ["damage", "stun", "stun-chance", "instant-death-per-thousand"].includes(effect.type)
      ? { ...effect, recipient: "all-enemies" } : effect);
  }
  function applyWeather(actor, effect, source, choiceId) {
    const choice = (effect.choices || []).find(entry => entry.id === choiceId) || (effect.choices || [])[0];
    if (!choice) return;
    for (const recipient of living()) recipient.effects = recipient.effects.filter(active => !active.weather);
    for (const recipient of living()) {
      applyTimedEffects(recipient, `weather:${choice.id}`, choice.name, (choice.effects || []).map(entry => ({ ...entry, durationTurns: effect.durationTurns, unpreventable: true, unremovable: true })));
      recipient.effects.filter(active => String(active.key || "").startsWith(`weather:${choice.id}:`)).forEach(active => { active.weather = true; });
    }
    battle.weather = { id: choice.id, name: choice.name, sourceActorId: actor.id };
    log(`${actorLabel(actor)} changes the weather to ${choice.name} with ${source}.`);
  }
  function applyEffects(actor, target, effects, power = 0, element = "basic", source = "Item", spell = false, weatherChoiceId = null, apexChoice = null, sourceSpell = null) {
    if (spell && effects.some(effect => ["damage", "weapon-charge-all-enemies"].includes(effect.type)) && !equipmentActionDamage.has(actor.id) && equipmentChargeBonuses(actor).length) {
      return withEquipmentCharge(actor, () => applyEffects(actor, target, effects, power, element, source, spell, weatherChoiceId, apexChoice, sourceSpell));
    }
    for (const effect of effects) {
      const amount = (effect.amount === "power" ? power : effect.amount) + toNumber(effect.flatAmount, 0);
      if (effect.type === "summon-friendly") {
        for (let count = Math.max(1, toInteger(effect.count, 1)); count > 0; count--) summonFriendly(actor, effect, source);
      } else if (effect.type === "weather-control") {
        applyWeather(actor, effect, source, weatherChoiceId);
      } else if (effect.type === "storm-surge") {
        // Counted when the spell is cast: it darkens a gathering storm further.
      } else if (effect.type === "seal-wound") {
        // Stonecrust: stone crusts over one open old wound and the caster recovers a little.
        const wounds = oldWoundsOf(actor);
        if (wounds && !actor.woundsBurst && actor.openWounds > 0) {
          setOpenWounds(actor, actor.openWounds - 1);
          const healed = healingDisabled(actor) ? 0 : healHp(actor, Math.max(1, Math.floor(values(actor).maxHp * Math.max(0, toNumber(amount, 0)) / 100)), battle, actor);
          log(`${source}: stone crusts over one of ${actor.name}'s wounds${healed ? ` and ${actor.name} recovers ${healed} HP` : ""}.`);
          if (onStage(actor)) stage("status", { actorId: actor.id, label: "Wound sealed", debuff: false });
        }
      } else if (effect.type === "root-ap") {
        // Roots: the recipient loses Action Points at the start of their next turn.
        for (const recipient of recipientsFor(actor, target, effect).filter(entry => entry && hp(entry) > 0)) {
          recipient.rootedAp = Math.max(toInteger(recipient.rootedAp, 0), Math.max(1, toInteger(amount, 1)));
          recipient.effects = recipient.effects.filter(entry => entry.type !== "rooted");
          recipient.effects.push({ key: "rooted", name: "Binding Roots", type: "rooted", amount: -recipient.rootedAp, seasonFoe: true, unremovable: true, expiresTurn: recipient.turns + 1 });
          log(`${source} roots ${actorLabel(recipient)}: ${recipient.rootedAp} AP lost on their next turn.`);
          if (onStage(recipient)) stage("status", { actorId: recipient.id, label: "Rooted", debuff: true });
        }
      } else if (effect.type === "weapon-charge-all-enemies") {
        const weapon = attackLoadout(actor).primary, multiplier = focusedPowerMultiplier(actor);
        const aftermathSkills = attackAftermathSkills(actor, weapon);
        for (const enemy of [...opponents(actor)]) {
          if (hp(actor) <= 0) break;
          const recipient = redirectedGuardTarget(actor, enemy, "melee", true);
          if (!recipient || hp(recipient) <= 0) continue;
          const landed = strike(actor, recipient, weapon, multiplier);
          if (landed && hp(recipient) > 0) {
            const bonus = hurt(recipient, weaponAttackValues(actor, weapon).power * multiplier * amount / 100, effect.element || "fire", false, false, actor, "bonus");
            log(`${source} deals ${bonus} additional ${effect.element || "fire"} damage.`);
          }
        }
        consumeSkill(actor, "durationAttacks", weapon);
        actor.firstStrikeUsed = true;
        applyAttackAftermath(actor, aftermathSkills);
      } else if (effect.type === "damage") {
        for (const recipient of recipientsFor(actor, target, effect).filter(Boolean)) {
          if (spell && effect.recipient === "all-enemies" && rules.roll(spellMissChanceAgainst(actor, recipient, { element }))) {
            log(`${source} misses ${actorLabel(recipient)}.`);
            continue;
          }
          const raceBonus = (effect.raceIds || []).map(slugify).includes(actorRace(recipient)) ? toNumber(effect.raceDamagePercent, 0) : 0;
          const apexDamageMultiplier = apexChoice === "sweep" ? 1.35 : apexChoice === "focus" ? 2 : 1;
          const damage = hurt(recipient, amount * apexDamageMultiplier * (1 + raceBonus / 100), element, false, spell, actor, spell ? "spell" : "item", effect.armorPiercePercent, spell && apexChoice === "focus", sourceSpell?.tier || 0);
          log(`${source} deals ${damage} ${elementLabel(element)} damage to ${actorLabel(recipient)}.`);
          if (effect.lifeSteal && !healingDisabled(actor)) {
            const healing = healHp(actor, Math.max(0, Math.floor(damage * effect.lifeSteal)), battle, actor);
            log(`${actorLabel(actor)} drains ${actor.team === "player" ? healing + " " : ""}HP.`);
          }
          if (effect.manaSiphonPercent) {
            const drained = Math.min(mana(recipient), Math.max(0, Math.floor(damage * toNumber(effect.manaSiphonPercent, 0) / 100)));
            setMana(recipient, mana(recipient) - drained);
            setMana(actor, Math.min(values(actor).maxMana, mana(actor) + drained));
            log(`${source} siphons ${drained} Mana from ${actorLabel(recipient)}.`);
          }
          if (spell) for (const passive of racePassiveEffectsOf(actor).filter(entry => entry.type === "spell-damage-mana-burn-percent")) {
            const burned = Math.min(mana(recipient), Math.max(0, Math.floor(damage * toNumber(passive.amount, 0) / 100)));
            setMana(recipient, mana(recipient) - burned);
            if (burned) log(`${actorLabel(recipient)} loses ${burned} Mana.`);
          }
        }
      } else if (trapEffects.has(effect.type)) {
        const recipient = effect.recipient === "friendly-target" ? target : actor;
        if (!recipient || recipient.team !== actor.team || hp(recipient) <= 0) continue;
        const trapBonuses = withActor(actor, () => calculateClassEffects());
        const key = `trap:${effect.type}`;
        recipient.effects = recipient.effects.filter(existing => existing.key !== key);
        recipient.effects.push({
          key, name: source, type: effect.type, amount, cap: toNumber(effect.cap, 0),
          damagePercent: trapBonuses.trapDamagePercent,
          armorPiercePercent: trapBonuses.trapArmorPiercePercent,
          remainingTriggers: 1 + Math.max(0, Math.floor(trapBonuses.trapAdditionalTriggers)),
          ...(effect.durationTurns != null ? { durationTurns: Math.max(1, toInteger(effect.durationTurns, 1)) } : {}),
          expiresTurn: Number.MAX_SAFE_INTEGER
        });
        log(`${actorLabel(actor)} places ${source}${recipient === actor ? "" : " around " + actorLabel(recipient)}.`);
      } else if (effect.type === "heal" || effect.type === "restore-mana") {
        for (const recipient of recipientsFor(actor, target, effect).filter(Boolean)) {
          const restoreHp = effect.type === "heal", max = values(recipient)[restoreHp ? "maxHp" : "maxMana"];
          const current = restoreHp ? hp(recipient) : mana(recipient);
          const restoredAmount = rules.resourceRestore(amount, max, effect.maxPercent);
          const triageBonus = restoreHp && recipient.id !== actor.id && hp(actor) < values(actor).maxHp * .5 ? passiveEffectsOf(actor).filter(active => active.type === "low-hp-healing-percent").reduce((sum, active) => sum + toNumber(active.amount, 0), 0) : 0;
          const healingPercent = restoreHp ? recipient.effects.filter(active => active.type === "healing-received-percent").reduce((sum, active) => sum + toNumber(active.amount, 0), 0) - recipient.effects.filter(active => active.type === "class-poison").reduce((maximum, active) => Math.max(maximum, toNumber(active.healingPenalty, 0)), 0) + triageBonus : 0;
          const restoration = Math.max(0, Math.floor(restoredAmount * (1 + healingPercent / 100)));
          const effectiveRestoration = restoreHp ? healingAmount(recipient, restoration, actor) : restoration;
          const gain = restoreHp && healingDisabled(recipient) ? 0 : Math.max(0, Math.min(max - current, effectiveRestoration));
          if (restoreHp && !healingDisabled(recipient)) for (const ward of passiveEffectsOf(actor).filter(active => active.type === "overheal-barrier-percent")) recipient.barrierHp = Math.min(Math.floor(max * toNumber(ward.amount, 0) / 100), Math.max(0, toNumber(recipient.barrierHp, 0)) + Math.max(0, effectiveRestoration - gain));
          if (restoreHp) setHp(recipient, current + gain); else setMana(recipient, current + gain);
          log(`${actorLabel(recipient)} restores ${actor.team === "player" ? gain + " " : ""}${restoreHp ? "HP" : "Mana"}${actor.team === "player" ? " with " + source : ""}.`);
        }
      } else if (effect.type === "stun") {
        for (const recipient of recipientsFor(actor, target, effect).filter(Boolean)) {
          if (debuffImmune(recipient) || withActor(recipient, () => calculateClassEffects().stunImmune)) {
            log(`${actorLabel(recipient)} resists the stun from ${source}.`);
            continue;
          }
          recipient.effects = recipient.effects.filter(existing => existing.type !== "stun");
          recipient.effects.push({ key: `${source}:stun`, name: "Stunned", type: "stun", amount: 1, expiresTurn: recipient.turns + Math.max(1, toInteger(effect.durationTurns, 1)) });
          log(`${actorLabel(recipient)} is stunned by ${source}.`);
        }
      } else if (effect.type === "stun-chance") {
        for (const recipient of recipientsFor(actor, target, effect).filter(Boolean)) {
          if (rules.roll(amount) && !debuffImmune(recipient) && !withActor(recipient, () => calculateClassEffects().stunImmune)) {
            recipient.effects = recipient.effects.filter(existing => existing.type !== "stun");
            recipient.effects.push({ key: `${source}:stun`, name: "Stunned", type: "stun", amount: 1, expiresTurn: recipient.turns + 1 });
            log(`${source} stuns ${actorLabel(recipient)}.`);
          }
        }
      } else if (effect.type === "next-melee-attack") {
        const key = `${source}:next-melee-attack`;
        actor.effects = actor.effects.filter(existing => existing.key !== key);
        actor.effects.push({
          key, name: source, type: effect.type, amount, element: effect.element || element,
          lifeSteal: toNumber(effect.lifeSteal, 0), stunChance: toNumber(effect.stunChance, 0),
          armorPiercePercent: toNumber(effect.armorPiercePercent, 0),
          stunDurationTurns: Math.max(1, toInteger(effect.stunDurationTurns, 1)),
          statDebuff: effect.statDebuff ? copy(effect.statDebuff) : null,
          remainingHits: 1, expiresTurn: Number.MAX_SAFE_INTEGER
        });
        log(`${actorLabel(actor)} enchants their next melee attack with ${source}.`);
      } else if (effect.type === "remove-random-debuff") {
        const candidates = effect.recipient === "random-ally" ? living().filter(candidate => candidate.team === actor.team && candidate.effects.some(isRemovableDebuff)) : recipientsFor(actor, target, effect);
        const recipients = effect.recipient === "all-allies" ? candidates : candidates.length ? [candidates[Math.floor(Math.random() * candidates.length)]] : [];
        for (const recipient of recipients) {
          const debuffs = recipient.effects.filter(isRemovableDebuff);
          if (!debuffs.length) {
            log(`${source} finds no debuff to remove from ${actorLabel(recipient)}.`);
            continue;
          }
          const removed = debuffs[Math.floor(Math.random() * debuffs.length)];
          recipient.effects = recipient.effects.filter(existing => existing !== removed);
          log(`${source} removes ${removed.name || "a debuff"} from ${actorLabel(recipient)}.`);
        }
      } else if (effect.type === "instant-death-per-thousand") {
        for (const recipient of recipientsFor(actor, target, effect).filter(Boolean)) {
          if (Math.random() * 1000 < Math.max(0, amount)) {
            setHp(recipient, 0);
            if (!tryRaceDeathSave(recipient, element)) log(`${source} instantly defeats ${actorLabel(recipient)}.`);
          } else log(`${actorLabel(recipient)} resists ${source}.`);
        }
      } else {
        for (const recipient of recipientsFor(actor, target, effect).filter(Boolean)) {
          const timed = { ...effect, amount, sourceActorId: effect.type === "incoming-spell-damage-from-target-percent" ? target?.id : effect.sourceActorId };
          applyTimedEffects(recipient, source, source, [timed], actor);
          if (recipient.effects.some(active => active.name === source && active.type === effect.type)) log(`${actorLabel(recipient)} gains ${source} for ${effect.durationTurns || 1} turns.`);
        }
      }
    }
  }
  // A spell with season variants takes the effects of the battle's current season.
  function seasonalSpell(actor, spell) {
    const variants = spell.seasonVariants, current = seasonState();
    const id = current && current.owner.id === actor.id && variants[current.season.id] ? current.season.id : Object.keys(variants)[0];
    return { ...spell, name: `${spell.name}: ${variants[id].name}`, effects: variants[id].effects, seasonVariant: id };
  }
  function cast(actor, target, spell, weatherChoiceId = null, options = {}) {
    if (spell?.seasonVariants) spell = seasonalSpell(actor, spell);
    const archivedItem = options.archivedItem || null;
    if (!actionAllowed(actor, "spell") || archivedItem && !actionAllowed(actor, "item") || !spell || !casterAllowed(actor, spell) || !archivedItem && !spellsOf(actor).some(entry => entry.id === spell.id)) return false;
    const effects = spellEffects(spell);
    if (!aiSummonAllowed(actor, effects)) return false;
    const hasApex = goldenApexEquipped(actor);
    const apexChoice = hasApex ? options.apexChoice || (actor.team === "enemy" ? opponents(actor).length > 1 && effects.some(effect => effect.type === "damage") ? "sweep" : "focus" : null) : null;
    if (hasApex && !["sweep", "focus"].includes(apexChoice)) return false;
    const targetEffects = apexSpellEffects(effects, apexChoice);
    const needsEnemy = requiresEnemyTarget(targetEffects);
    const needsFriendly = effects.some(effect => effect.recipient === "friendly-target");
    // Lament's second target: another living combatant on the first one's side (M19, M20).
    const lament = options.extraTargetId != null && !archivedItem ? lamentEffect(actor) : null;
    let extra = null;
    if (options.extraTargetId != null) {
      extra = lament && lamentEligible(targetEffects) ? actorById(options.extraTargetId) : null;
      if (!extra || extra.id === target?.id || hp(extra) <= 0 || (needsEnemy ? !opponents(actor).includes(extra) : extra.team !== actor.team)) return false;
    }
    if (needsEnemy) target = redirectedGuardTarget(actor, target, "spell");
    if (extra && needsEnemy) extra = redirectedGuardTarget(actor, extra, "spell");
    if (!effectsUsable(effects) || needsEnemy && !opponents(actor).includes(target) || needsFriendly && (!target || target.team !== actor.team || hp(target) <= 0) || effects.some(effect => effect.type === "summon-friendly" && (!enemyDefinitions.some(enemy => enemy.id === effect.enemyId) || summonLimitReached(actor, effect) || effect.requiresDefeatedEnemy && !defeatedEnemyFor(actor)))) return false;
    const successive = actor.lastSpellId === spell.id ? Math.max(0, toInteger(actor.spellCastStreak, 0)) : 0;
    const successiveMana = Math.max(0, ...effects.map(effect => toNumber(effect.successiveCastManaPercent, 0)));
    const successiveDamage = Math.max(0, ...effects.map(effect => toNumber(effect.successiveCastDamagePercent, 0)));
    const baseCost = withActor(actor, () => calculateSpellManaCost(spell));
    const cost = archivedItem ? 0 : Math.max(0, Math.floor(baseCost * (1 - timedSpellPercent(actor, "spell-cost-percent", spell) / 100) * (1 + successive * successiveMana / 100) * (extra ? 1 + toNumber(lament.amount, 0) / 100 : 1)));
    const actionPoints = archivedItem ? 1 : withActor(actor, () => calculateSpellActionPoints(spell));
    const basePower = withActor(actor, () => calculateSpellValue(spell));
    const stormPower = Math.max(0, toInteger(actor.stormCharges, 0)) * passiveEffectsOf(actor).filter(effect => effect.type === "storm-alternation-charge").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
    const spellPenalty = actor.effects.find(effect => effect.type === "next-spell-penalty");
    const power = Math.max(0, Math.floor(basePower * (1 + (timedSpellPercent(actor, "spell-power-percent", spell) + stormPower) / 100) * (1 + successive * successiveDamage / 100) * focusedPowerMultiplier(actor) * (1 - Math.min(100, Math.max(0, toNumber(spellPenalty?.amount, 0))) / 100)));
    if (mana(actor) < cost || !spend(actor, actionPoints)) return false;
    battle.actionSerial = toInteger(battle.actionSerial, 0) + 1;
    return withEquipmentCharge(actor, () => {
      // Echo Bell: a chance the spell costs no Mana.
      const echoBell = cost > 0 && !archivedItem ? artifactEffectsOf(actor, "free-spell-chance").find(effect => rules.roll(effect.amount)) : null;
      if (echoBell) log(`${echoBell.itemName} rings: ${spell.name} costs no Mana.`);
      else setMana(actor, mana(actor) - cost);
      const spellElementOverride = actor.nextSpellElement;
      actor.nextSpellElement = null;
      if (spellPenalty) actor.effects = actor.effects.filter(effect => effect !== spellPenalty);
      recordTurnAction(actor, archivedItem ? "item" : "spell", cost);
      if (onStage(actor)) {
        const stageElements = spellElementOverride ? [spellElementOverride] : withActor(actor, () => effectiveSpellElements(spell));
        const recipients = new Set(targetEffects.map(effect => effect.recipient || (effect.type === "damage" ? "target" : "self")));
        stage("spell", { actorId: actor.id, targetId: target?.id || null, spellId: spell.id, name: spell.name, element: stageElements[0] || spell.element, elements: stageElements, tier: spell.tier, category: isSummonSpell(spell) ? "summon" : needsEnemy || String(spell.type).toLowerCase() === "damage" || recipients.has("all-enemies") || recipients.has("target") ? "offensive" : "support", area: apexChoice === "sweep" || recipients.has("all-enemies") ? "enemies" : recipients.has("all-allies") ? "allies" : recipients.has("all-combatants") ? "all" : null, selfOnly: recipients.size === 1 && recipients.has("self"), apex: apexChoice || null });
        for (const item of withActor(actor, equippedWeaponItems)) if (item.rarity === "masterwork" && (apexChoice && (item.effects || []).some(effect => effect.kind === "golden-apex-choice") || (item.effects || []).some(effect => effect.kind === "spell-add-element") || isSummonSpell(spell) && (item.effects || []).some(effect => effect.kind === "summon-empowerment"))) stageEquipment(actor, item);
      }
      log(actor.team === "player" ? `${actorLabel(actor)} ${archivedItem ? `uses ${archivedItem.name} to cast` : "casts"} ${spell.name} (${cost} Mana, ${actionPoints} AP).` : `${actorLabel(actor)} casts a spell.`);
      if (spell.seasonVariant) {
        log(`${actor.name} turns the ${spell.name}!`);
        stage("season", { season: spell.seasonVariant, name: spell.name });
        if (seasonState()?.owner.id === actor.id) battle.season.wheelIndex = battle.season.index;
      }
      if (actor.team === "enemy" && living().some(ally => ally.team === "player" && passiveEffectsOf(ally).some(effect => effect.type === "archive-enemy-spells"))) {
        (battle.observedSpells ||= []).push(spell.id);
      }
      const ward = target?.effects.find(effect => effect.type === "stance" && effect.name === "Guard");
      const countercast = ward ? passiveEffectsOf(target).find(effect => effect.type === "guard-spell-interrupt-percent") : null;
      if (needsEnemy && countercast && target.guardInterruptedTurn !== target.turns) {
        target.guardInterruptedTurn = target.turns;
        if (rules.roll(toNumber(countercast.amount, 0))) {
          stage("interrupt", { actorId: actor.id, targetId: target.id });
          log(`${actorLabel(target)} interrupts ${actorLabel(actor)}'s spell while Guarding.`);
          actor.lastSpellId = spell.id;
          consumeSkill(actor, "durationSpells", spell);
          return true;
        }
      }
      const activeSpellSkills = skillsOf(actor).filter(skill => skill.durationSpells && skillStates(actor)[classSkillKey(skill)]?.active && skillMatchesSpell(actor, skill, spell));
      const repeatCount = 1 + activeSpellSkills.flatMap(skill => skill.effects || []).filter(effect => effect.type === "repeat-spell").reduce((sum, effect) => sum + Math.max(1, toInteger(effect.amount, 1)), 0);
      const classEffects = withActor(actor, () => calculateClassEffects());
      const spellElement = spellElementOverride ? [spellElementOverride] : withActor(actor, () => effectiveSpellElements(spell));
      const appliedEffects = targetEffects.map(effect => {
        const enhanced = effect.type === "summon-friendly" && classEffects.additionalSummonCount > 0
          ? { ...effect, count: Math.max(1, toInteger(effect.count, 1)) + classEffects.additionalSummonCount }
          : effect;
        return enhanced;
      });
      if (apexChoice) log(`${actorLabel(actor)} channels Golden Apex: ${apexChoice === "sweep" ? "Starlit Sweep" : "Unbound Zenith"}.`);
      playTierTenCinematic(actor, spell);
      const absorbed = rollSpellAbsorption(actor, target, appliedEffects, spellElement, cost, spell);
      absorbedSpellRecipients = absorbed.size ? absorbed : null;
      const fork = appliedEffects.some(effect => effect.type === "damage") ? artifactEffectsOf(actor, "spell-chain-percent")[0] : null;
      if (fork) actor.forkHits = new Map();
      try {
        for (let repetition = 0; repetition < repeatCount; repetition += 1) {
          const missed = needsEnemy && target && !absorbed.has(target.id) && apexChoice !== "sweep" && rules.roll(spellMissChanceAgainst(actor, target, spell));
          if (missed && onStage(target)) stage("miss", { actorId: target.id, attackerId: actor.id });
          if (missed) log(`${spell.name}${repetition ? " (repeat)" : ""} misses ${actorLabel(target)}.`);
          else applyEffects(actor, target, appliedEffects, power, spellElement, repetition ? `${spell.name} (repeat)` : spell.name, true, weatherChoiceId, apexChoice, spell);
        }
        // Lament: the spell's targeted effects again, on the second target.
        if (extra && isActive() && hp(extra) > 0) {
          const extraEffects = appliedEffects.filter(lamentTargeted);
          const absorbedExtra = rollSpellAbsorption(actor, extra, extraEffects, spellElement, cost, spell);
          absorbedSpellRecipients = absorbedExtra.size ? absorbedExtra : null;
          log(`Lament carries ${spell.name} to ${actorLabel(extra)}.`);
          for (let repetition = 0; repetition < repeatCount; repetition += 1) {
            const missed = needsEnemy && !absorbedExtra.has(extra.id) && rules.roll(spellMissChanceAgainst(actor, extra, spell));
            if (missed && onStage(extra)) stage("miss", { actorId: extra.id, attackerId: actor.id });
            if (missed) log(`${spell.name} (Lament) misses ${actorLabel(extra)}.`);
            else applyEffects(actor, extra, extraEffects, power, spellElement, `${spell.name} (Lament)`, true, weatherChoiceId, apexChoice, spell);
          }
        }
      } finally {
        absorbedSpellRecipients = null;
      }
      // Forked Lightning Rod (A24, A31): one strike per cast, on an enemy the spell didn't hit, as a spell hit.
      if (fork) {
        const hits = actor.forkHits;
        actor.forkHits = null;
        const others = living().filter(foe => foe.team !== actor.team && !foe.inert && !hits.has(foe.id));
        const largest = Math.max(0, ...hits.values());
        if (hits.size && largest > 0 && others.length && isActive()) {
          const struck = others[Math.floor(Math.random() * others.length)];
          chainStrikeDepth += 1;
          let damage = 0;
          try { damage = hurt(struck, Math.floor(largest * fork.amount / 100), spellElement, false, true, actor, "spell"); } finally { chainStrikeDepth -= 1; }
          log(`${fork.itemName}: the spell forks to ${actorLabel(struck)} for ${damage} damage.`);
        }
      }
      // A dream law may echo a damaging spell at part of its power.
      const echo = lawAmount(actor, "spell-echo-percent"), echoed = appliedEffects.filter(effect => effect.type === "damage");
      if (echo > 0 && echoed.length && hp(actor) > 0 && isActive() && (!needsEnemy || target && hp(target) > 0)) {
        log(`${spell.name} echoes through the dream.`);
        applyEffects(actor, target, echoed, Math.floor(power * echo / 100), spellElement, `${spell.name} (echo)`, true, weatherChoiceId, apexChoice, spell);
      }
      actor.lastSpellId = spell.id;
      // A lightning spell, anyone's, darkens a boss's gathering storm.
      if (battle.storm?.ended !== true && elementList(spellElement).includes("lightning")) feedStorm(1 + effects.filter(effect => effect.type === "storm-surge").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0));
      const storm = passiveEffectsOf(actor).find(effect => effect.type === "storm-alternation-charge");
      if (storm && ["wind", "lightning"].includes(spell.element)) {
        actor.stormCharges = actor.lastStormElement && actor.lastStormElement !== spell.element ? Math.min(Math.max(1, toInteger(storm.maximum, 3)), Math.max(0, toInteger(actor.stormCharges, 0)) + 1) : 0;
        actor.lastStormElement = spell.element;
      }
      actor.spellCastStreak = successive + 1;
      if (isSummonSpell(spell)) {
        incrementActorSummonSpellsUsed(actor, effectiveSpellElement(spell, classEffects), repeatCount);
        const progress = actor.id === "player" ? classProgress : (actor.character.classProgress ||= { oneHandedAttacks: 0, summonCasts: {}, lootEnemiesCollected: 0 });
        progress.summonCasts[spell.id] = Math.max(0, toInteger(progress.summonCasts[spell.id], 0)) + repeatCount;
        consumeSkill(actor, "durationSummonSpells", spell);
      }
      if (passiveEffectsOf(actor).some(effect => effect.type === "sorcery-point-on-spell")) {
        actor.sorceryPoints = Math.max(0, toInteger(actor.sorceryPoints, 0)) + repeatCount;
        log(`${actorLabel(actor)} gains ${repeatCount} Sorcery Point${repeatCount === 1 ? "" : "s"} (${actor.sorceryPoints}).`);
      }
      consumeSkill(actor, "durationSpells", spell);
      return true;
    }, effects.some(effect => ["damage", "weapon-charge-all-enemies"].includes(effect.type)));
  }
  function useItem(actor, target, item, options = {}) {
    if (item?.archivedSpellId) {
      const spell = getSpell(item.archivedSpellId);
      if (!spell || !consumablesOf(actor).some(entry => entry.id === item.id) || !cast(actor, target, spell, null, { archivedItem: item, apexChoice: options.apexChoice })) return false;
      const owned = actor.id === "player" ? inventory : actor.character.inventory;
      if (!rules.roll(withActor(actor, calculateClassEffects).consumableRetentionChance)) owned.splice(owned.indexOf(item.id), 1);
      return true;
    }
    if (!actionAllowed(actor, "item") || !item || !consumablesOf(actor).some(entry => entry.id === item.id) || !effectsUsable(item.effects || [])) return false;
    if (item.effects.some(effect => effect.type === "damage") && !opponents(actor).includes(target)) return false;
    if (item.effects.some(effect => effect.recipient === "friendly-target") && (!target || target.team !== actor.team || hp(target) <= 0)) return false;
    if (trapCapacityReason(actor, item.effects)) return false;
    if (!spend(actor, 1)) return false;
    battle.actionSerial = toInteger(battle.actionSerial, 0) + 1;
    recordTurnAction(actor, "item");
    if (onStage(actor)) stage("item", { actorId: actor.id, targetId: target?.id || null, itemId: item.id, name: item.name, potion: Boolean(item.potionResource), resource: item.potionResource || null, trap: trapPlacementCount(item.effects || []) > 0 });
    const owned = actor.id === "player" ? inventory : actor.character.inventory;
    const placements = trapPlacementCount(item.effects || []);
    const retentionBonuses = withActor(actor, calculateClassEffects);
    const retentionChance = placements > 0 ? retentionBonuses.trapRetentionChance : retentionBonuses.consumableRetentionChance;
    const retained = rules.roll(retentionChance);
    if (!retained) owned.splice(owned.indexOf(item.id), 1);
    if (placements > 0) {
      if (actor.id === "player") trapsUsed += placements;
      else actor.character.trapsUsed = Math.max(0, toInteger(actor.character.trapsUsed, 0)) + placements;
    }
    const supplyBonus = living().filter(ally => ally.team === actor.team).flatMap(passiveEffectsOf).filter(effect => effect.type === "party-consumable-power-percent").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
    const scaledEffects = (item.effects || []).map(effect => ["heal", "restore-mana"].includes(effect.type) ? { ...effect, amount: toNumber(effect.amount, 0) * (1 + supplyBonus / 100), maxPercent: toNumber(effect.maxPercent, 0) * (1 + supplyBonus / 100) } : effect);
    applyEffects(actor, target, scaledEffects, 0, item.element || "basic", item.name);
    if (retained) log(`${actorLabel(actor)} recovers ${item.name} after placing it.`);
    return true;
  }
  function isDebuffDefinition(effect) {
    if (["stun", "flee-disabled"].includes(effect.type)) return true;
    if (["stat-percent", "all-stats-percent", "defense-flat", "defense-percent", "stat-permanent-percent", "defense-permanent-percent", "weapon-hit-flat", "weapon-hit-percent"].includes(effect.type)) return toNumber(effect.amount, 0) < 0;
    if (["incoming-weapon-damage-percent", "incoming-spell-damage-percent", "incoming-attack-damage-percent", "incoming-race-damage-percent", "healing-received-percent"].includes(effect.type)) return toNumber(effect.amount, 0) > 0 || effect.type === "healing-received-percent" && toNumber(effect.amount, 0) < 0;
    return false;
  }
  function applyTimedEffects(actor, key, name, effects, healer = null) {
    for (const effect of effects || []) {
      if (!Number.isFinite(effect.durationTurns) && !Number.isFinite(effect.durationHits) || !Number.isFinite(effect.amount) && !Number.isFinite(effect.hp) && !Number.isFinite(effect.mana)) continue;
      if (!effect.unpreventable && debuffImmune(actor) && isDebuffDefinition(effect)) continue;
      const elementKey = effect.element || (effect.elements || []).join("+") || (effect.excludedElements || []).join("!") || "all";
      const effectKey = `${key}:${effect.type}:${effect.target || "all"}:${elementKey}`;
      actor.effects = actor.effects.filter((existing) => existing.key !== effectKey);
      actor.effects.push({
        key: effectKey,
        name,
        type: effect.type,
        target: effect.target,
        amount: toNumber(effect.amount, 0),
        hp: toNumber(effect.hp, 0),
        mana: toNumber(effect.mana, 0),
        attackKinds: Array.isArray(effect.attackKinds) ? effect.attackKinds.map(normalizeWeaponType) : [],
        element: effect.element,
        elements: Array.isArray(effect.elements) ? [...effect.elements] : [],
        excludedElements: Array.isArray(effect.excludedElements) ? [...effect.excludedElements] : [],
        raceIds: Array.isArray(effect.raceIds) ? [...effect.raceIds] : [],
        weaponType: effect.weaponType,
        sourceActorId: effect.sourceActorId,
        ...(healer && effect.type === "turn-start-restore-percent" ? { healerId: healer.id } : {}),
        ...(effect.burnPercent != null ? { burnPercent: toNumber(effect.burnPercent, 0), burnTurns: Math.max(1, toInteger(effect.burnTurns, 2)) } : {}),
        unpreventable: effect.unpreventable === true,
        unremovable: effect.unremovable === true,
        remainingHits: Number.isFinite(effect.remainingHits) ? Math.max(1, toInteger(effect.remainingHits, 1)) : Number.isFinite(effect.durationHits) ? Math.max(1, toInteger(effect.durationHits, 1)) : undefined,
        expiresTurn: Number.isFinite(effect.durationTurns) ? actor.turns + Math.max(1, toInteger(effect.durationTurns, 1)) : Number.MAX_SAFE_INTEGER
      });
    }
  }
  function castRandomSkillSpell(actor, selected, effect, source) {
    const highestKnownTier = Math.max(0, ...spellsOf(actor).map(spell => Math.max(1, toInteger(spell.tier, 1))));
    if (!highestKnownTier) return false;
    const candidates = spells.filter(spell => {
      if (!casterAllowed(actor, spell)) return false;
      if (Math.max(1, toInteger(spell.tier, 1)) > highestKnownTier) return false;
      const effects = spellEffects(spell);
      return effectsUsable(effects) && !effects.some(entry => entry.type === "summon-friendly" && (!enemyDefinitions.some(enemy => enemy.id === entry.enemyId) || summonLimitReached(actor, entry) || entry.requiresDefeatedEnemy && !defeatedEnemyFor(actor)));
    });
    if (!candidates.length) return false;
    const spell = candidates[Math.floor(Math.random() * candidates.length)], effects = spellEffects(spell);
    const needsEnemy = requiresEnemyTarget(effects);
    const needsFriendly = effects.some(entry => entry.recipient === "friendly-target");
    let target = needsFriendly ? actor : needsEnemy && opponents(actor).includes(selected) ? selected : needsEnemy ? opponents(actor)[0] : actor;
    if (needsEnemy) target = redirectedGuardTarget(actor, target, "spell");
    if (needsEnemy && !opponents(actor).includes(target)) return false;
    const power = withActor(actor, () => Math.floor(calculateSpellValue(spell)));
    const classEffects = withActor(actor, () => calculateClassEffects());
    const appliedEffects = effects.map(entry => entry.type === "summon-friendly" && classEffects.additionalSummonCount > 0
      ? { ...entry, count: Math.max(1, toInteger(entry.count, 1)) + classEffects.additionalSummonCount }
      : entry);
    const weather = effects.find(entry => entry.type === "weather-control");
    playTierTenCinematic(actor, spell);
    const missed = needsEnemy && target && rules.roll(spellMissChanceAgainst(actor, target, spell));
    if (missed) log(`${source} randomly casts ${spell.name}, but it misses ${actorLabel(target)}.`);
    else {
      applyEffects(actor, target, appliedEffects, power, withActor(actor, () => effectiveSpellElements(spell)), `${source}: ${spell.name}`, true, weather?.choices?.[0]?.id || null);
      log(`${actorLabel(actor)} uses ${source} to cast ${spell.name} at random.`);
    }
    actor.lastSpellId = spell.id;
    actor.spellCastStreak = 1;
    if (isSummonSpell(spell)) incrementActorSummonSpellsUsed(actor, spell.element, 1);
    if (passiveEffectsOf(actor).some(entry => entry.type === "sorcery-point-on-spell")) actor.sorceryPoints = Math.max(0, toInteger(actor.sorceryPoints, 0)) + 1;
    return true;
  }
  function skillEffectPower(actor, effect, classId = "") {
    const actorValues = values(actor);
    const primary = effect.primary || {};
    const secondary = effect.secondary || {};
    const base = Math.max(0, toNumber(effect.basePower, 0)
      + toNumber(actorValues[primary.stat], 0) * toNumber(primary.ratio, 0)
      + toNumber(actorValues[secondary.stat], 0) * toNumber(secondary.ratio, 0));
    const psychicBonus = classId === "psykic" ? passiveEffectsOf(actor).filter(entry => entry.type === "psychic-skill-damage-percent").reduce((sum, entry) => sum + toNumber(entry.amount, 0), 0) : 0;
    return Math.floor(base * (1 + psychicBonus / 100));
  }
  function useSkill(actor, key, target = null, option = null, phaseActivation = false) {
    const skill = skillsOf(actor).find(entry => classSkillKey(entry) === key);
    const states = skillStates(actor), state = states[key];
    if (skill?.target === "enemy") target = redirectedTauntTarget(actor, target);
    const actionPoints = Math.max(0, toInteger(skill?.actionPoints, 0));
    const invalidEnemy = skill?.target === "enemy" && !opponents(actor).includes(target);
    const invalidAlly = skill?.target === "ally" && (!target || target.team !== actor.team || target.id === actor.id || hp(target) <= 0);
    const needsPrediction = (skill?.effects || []).some(effect => effect.type === "predict-action");
    const requiredRace = skill?.requiredTargetRaceIds;
    const needsCopiedSpell = skill?.onActivateEffects?.some(effect => effect.type === "copy-enemy-spell");
    if (!phaseActivation && !actionAllowed(actor, "skill") || !skill || skill.mapOnly || !skillReady(actor, skill, phaseActivation) || invalidEnemy || invalidAlly || requiredRace?.length && !(requiredRace || []).map(slugify).includes(actorRace(target)) || needsCopiedSpell && !getSpell(target?.lastSpellId) || needsPrediction && !["attack", "spell", "skill", "defend", "meditate", "item"].includes(option)) return false;
    const skillManaCost = Math.floor(values(actor).maxMana * Math.max(0, toNumber(skill.manaCostMaxPercent, 0)) / 100);
    const moneyCost = Math.max(0, toInteger(skill.moneyCostSilver, 0));
    const remainingMoney = moneyCost ? ShopRules.spend(actorCurrencies(actor), moneyCost) : actorCurrencies(actor);
    if (mana(actor) < skillManaCost || !remainingMoney || !phaseActivation && !spend(actor, actionPoints)) return false;
    if (skillManaCost) setMana(actor, mana(actor) - skillManaCost);
    if (skill.kiCost) actor.ki = kiOf(actor) - Math.max(0, toInteger(skill.kiCost, 0));
    if (skill.oncePerTurn) (actor.skillTurnUses ||= {})[key] = actor.turns;
    if (moneyCost) setActorCurrencies(actor, remainingMoney);
    recordTurnAction(actor, "skill");
    if (onStage(actor)) stage("skill", { actorId: actor.id, targetId: target?.id || null, key, name: skill.name });
    if (skill.rollUntilSuccess) {
      const sides = Math.max(2, toInteger(skill.dieSides, 6));
      const roll = 1 + Math.floor(Math.random() * sides);
      if (roll < sides) {
        log(`${actorLabel(actor)} uses ${skill.name} and rolls ${roll}. The skill remains ready.`);
        return true;
      }
      log(`${actorLabel(actor)} uses ${skill.name} and rolls ${roll}. Their next attack is a guaranteed critical hit.`);
    }
    let charges = Math.max(0, toInteger(state?.charges, 0));
    if (skill.cooldown === "item") {
      if (charges > 0) charges -= 1;
      else {
        const owned = actor.id === "player" ? inventory : actor.character.inventory;
        const itemIndex = owned.indexOf(skill.requiredItemId);
        if (itemIndex < 0) return false;
        owned.splice(itemIndex, 1);
        const actorEquipment = actor.id === "player" ? equipment : actor.character.equipment;
        for (const slot of equipmentSlotDefinitions) if (actorEquipment[slot.key] === skill.requiredItemId) actorEquipment[slot.key] = null;
      }
    }
    states[key] = skill.cooldown === "combat"
      ? { ...state, active: true, usedThisCombat: true, usesThisCombat: toInteger(state?.usesThisCombat, state?.usedThisCombat ? 1 : 0) + 1, remainingUses: skill.durationAttacks || skill.durationSpells || skill.durationSummonSpells || undefined }
      : skill.cooldown === "item"
      ? { ...state, active: true, charges, remainingUses: skill.durationAttacks || skill.durationSpells || skill.durationSummonSpells || undefined }
      : skill.cooldown === "none"
      ? { ...state, active: !skill.instant, remainingUses: skill.durationAttacks || skill.durationSpells || skill.durationSummonSpells || undefined }
      : { ...state, active: true, lastUsedDay: todayKey(), remainingUses: skill.durationAttacks || skill.durationSpells || skill.durationSummonSpells || undefined };
    if (skill.instant) states[key].active = false;
    const dynamicDuration = skill.dynamicDurationTurns
      ? Math.max(1, toInteger(skill.dynamicDurationTurns.base, 1)) + Math.floor(toNumber(values(actor)[skill.dynamicDurationTurns.stat], 0) / Math.max(1, toNumber(skill.dynamicDurationTurns.per, 1)))
      : Math.max(0, toInteger(skill.durationTurns, 0));
    if (dynamicDuration) actor.skillExpiry[key] = actor.turns + dynamicDuration;
    for (const effect of skill.effects || []) {
      if (effect.type !== "mark-target") continue;
      target.effects = target.effects.filter(existing => existing.type !== "hunter-mark" || existing.ownerId !== actor.id);
      target.effects.push({ key: `${key}:mark`, name: skill.name, type: "hunter-mark", ownerId: actor.id, skillKey: key, damagePercent: toNumber(effect.damagePercent, 0), hitFlat: toNumber(effect.hitFlat, 0), hitPercent: toNumber(effect.hitPercent, 0), expiresTurn: Number.MAX_SAFE_INTEGER });
    }
    for (const effect of skill.effects || []) {
      if (effect.type === "tactical-mark") {
        for (const opponent of hostiles(actor)) opponent.effects = opponent.effects.filter(entry => entry.type !== "tactical-mark" || entry.team !== actor.team);
        target.effects.push({ key: `${key}:mark`, name: skill.name, type: "tactical-mark", team: actor.team, damagePercent: toNumber(effect.damagePercent, 0), turnAdvance: toNumber(effect.turnAdvance, 0), expiresTurn: Number.MAX_SAFE_INTEGER });
      }
      if (effect.type === "read-intent") {
        const next = aiCandidates(target)[0];
        log(`${actorLabel(actor)} reads ${actorLabel(target)}'s intent: ${next?.kind === "stance" ? next.stance : next?.kind || "end turn"} is most likely right now.`);
      }
      if (effect.type === "predict-action") {
        const guess = option;
        target.predictions ||= [];
        target.predictions = target.predictions.filter(entry => entry.ownerId !== actor.id);
        target.predictions.push({ ownerId: actor.id, guess, defensePercent: toNumber(effect.defensePercent, 75), durationTurns: toInteger(effect.durationTurns, 2) });
        log(`${actorLabel(actor)} foretells that ${actorLabel(target)} will ${guess}.`);
      }
    }
    for (const effect of skill.onActivateEffects || []) {
      if (effect.type === "restore-full-resources") {
        const maximum = values(actor); if (!healingDisabled(actor)) healHp(actor, maximum.maxHp - hp(actor), battle, actor); setMana(actor, maximum.maxMana);
      } else if (effect.type === "refresh-other-skills") {
        for (const [otherKey, otherState] of Object.entries(states)) {
          if (otherKey === key) continue;
          states[otherKey] = { ...otherState, active: false, lastUsedDay: "", usedThisCombat: false, usesThisCombat: 0 };
          delete actor.skillExpiry[otherKey];
          actor.effects = actor.effects.filter(existing => !String(existing.key || "").startsWith(otherKey + ":"));
        }
      } else if (effect.type === "turn-start-max-hp-loss-percent") {
        actor.effects = actor.effects.filter(existing => existing.type !== effect.type);
        actor.effects.push({ key: `${key}:turn-loss`, name: skill.name, type: effect.type, amount: toNumber(effect.amount, 0), startsTurn: actor.turns + (phaseActivation ? 2 : 1), expiresTurn: Number.MAX_SAFE_INTEGER });
      } else if (effect.type === "consume-all-blood-supplies-stat-percent") {
        const consumed = actorBloodSupplies(actor);
        if (consumed > 0) {
          setActorBloodSupplies(actor, 0);
          applyTimedEffects(actor, key + ":blood-frenzy", skill.name, [{ type: "all-stats-percent", amount: consumed * toNumber(effect.amountPerStack, 10), durationTurns: skill.durationTurns }]);
        }
      } else if (effect.type === "restore-resource-percent") {
        const recipients = effect.recipient === "all-allies" || skill.target === "all-allies" ? living().filter(candidate => candidate.team === actor.team) : [actor];
        for (const recipient of recipients) {
          const maximum = values(recipient);
          if (effect.hp && !healingDisabled(recipient)) {
            const healing = healingAmount(recipient, Math.max(1, Math.floor(maximum.maxHp * Math.max(0, toNumber(effect.hp, 0)) / 100)), actor);
            const overflow = Math.max(0, hp(recipient) + healing - maximum.maxHp);
            setHp(recipient, Math.min(maximum.maxHp, hp(recipient) + healing));
            for (const ward of passiveEffectsOf(actor).filter(passive => passive.type === "overheal-barrier-percent")) recipient.barrierHp = Math.min(Math.floor(maximum.maxHp * toNumber(ward.amount, 0) / 100), Math.max(0, toNumber(recipient.barrierHp, 0)) + overflow);
          }
          if (effect.mana) setMana(recipient, Math.min(maximum.maxMana, mana(recipient) + Math.max(1, Math.floor(maximum.maxMana * Math.max(0, toNumber(effect.mana, 0)) / 100))));
        }
      } else if (effect.type === "ki-restore-mana") {
        const maximum = values(actor).maxMana;
        const amount = Math.max(0, Math.floor(maximum * Math.max(0, toNumber(effect.percent, 0)) / 100 + values(actor).mind * Math.max(0, toNumber(effect.mindRatio, 0))));
        const restored = Math.min(maximum - mana(actor), amount);
        setMana(actor, mana(actor) + restored);
        log(`${actorLabel(actor)} restores ${restored} Mana with ${skill.name}.`);
      } else if (effect.type === "random-animal-summon") {
        summonRandomAnimals(actor, effect, skill.name);
      } else if (effect.type === "cast-random-spell") {
        castRandomSkillSpell(actor, target, effect, skill.name);
      } else if (effect.type === "weapon-attack-all-enemies") {
        const weapon = attackLoadout(actor).primary;
        const targets = [...opponents(actor)];
        const aftermathSkills = attackAftermathSkills(actor, weapon);
        withEquipmentCharge(actor, () => {
          for (const enemy of targets) {
            if (hp(actor) <= 0) break;
            if (hp(enemy) <= 0) continue;
            const recipient = redirectedGuardTarget(actor, enemy, normalizeWeaponType(weapon.type), true);
            if (recipient && opponents(actor).includes(recipient)) strike(actor, recipient, weapon, focusedPowerMultiplier(actor) * Math.max(0, toNumber(effect.damageMultiplier, 1)));
          }
        });
        // One area attack spends one attack charge, not zero charges and not
        // one per victim. Its downside still happens after the action.
        consumeSkill(actor, "durationAttacks", weapon);
        actor.firstStrikeUsed = true;
        applyAttackAftermath(actor, aftermathSkills);
      } else if (effect.type === "apply-skill-effects") {
        const power = skillEffectPower(actor, effect, skill.classId);
        applyEffects(actor, target, effect.effects || [], power, effect.elements?.length ? effect.elements : effect.element || "basic", skill.name, true);
      } else if (effect.type === "resource-exchange") {
        const maximum = values(actor);
        const hpCost = Math.max(0, Math.floor(maximum.maxHp * Math.max(0, toNumber(effect.hpCostPercent, 0)) / 100));
        const manaGain = Math.max(0, Math.floor(maximum.maxMana * Math.max(0, toNumber(effect.manaRestorePercent, 0)) / 100));
        const paidHp = Math.min(Math.max(0, hp(actor) - 1), hpCost);
        if (paidHp) directDamage(actor, paidHp, false, effect.element || "blood");
        if (manaGain) setMana(actor, Math.min(maximum.maxMana, mana(actor) + manaGain));
        log(`${actorLabel(actor)} exchanges ${paidHp} HP for ${manaGain} Mana with ${skill.name}.`);
      } else if (effect.type === "start-duel" && target) {
        const duration = Math.max(1, toInteger(effect.durationTurns, skill.durationTurns || 1));
        actor.effects = actor.effects.filter(existing => existing.type !== "duel-lock");
        target.effects = target.effects.filter(existing => existing.type !== "duel-lock");
        actor.effects.push({ key: `${key}:duel`, name: skill.name, type: "duel-lock", partnerId: target.id, unremovable: true, expiresTurn: actor.turns + duration });
        target.effects.push({ key: `${key}:duel-target`, name: skill.name, type: "duel-lock", partnerId: actor.id, unremovable: true, expiresTurn: target.turns + duration });
        battle.targetId = target.id;
        log(`${actorLabel(actor)} challenges ${actorLabel(target)} to a protected duel.`);
      } else if (effect.type === "guard-ally" && target) {
        const duration = Math.max(1, toInteger(effect.durationTurns, skill.durationTurns || 1));
        target.effects = target.effects.filter(existing => existing.type !== "guardian-ward");
        target.effects.push({ key: `${key}:ward`, name: skill.name, type: "guardian-ward", guardianId: actor.id, unremovable: true, expiresTurn: target.turns + duration });
        log(`${actorLabel(actor)} guards ${actorLabel(target)} with ${skill.name}.`);
      } else if (effect.type === "copy-enemy-spell" && target) {
        const spell = getSpell(target.lastSpellId), copiedEffects = spellEffects(spell);
        const friendly = copiedEffects.some(entry => entry.recipient === "friendly-target");
        const enemy = copiedEffects.some(entry => entry.recipient === "target" || entry.type === "damage" && entry.recipient !== "all-enemies");
        const recipient = friendly ? actor : enemy ? target : actor;
        const power = Math.floor(withActor(actor, () => calculateSpellValue(spell)) * Math.max(0, toNumber(effect.powerPercent, 70)) / 100);
        playTierTenCinematic(actor, spell);
        applyEffects(actor, recipient, copiedEffects, power, spell.element, `${skill.name}: ${spell.name}`, true);
        log(`${actorLabel(actor)} copies ${spell.name} at ${effect.powerPercent}% power.`);
      } else if (effect.type === "shift-turn-meters" && target) {
        shiftTurnMeter(actor, toNumber(effect.allyAmount, 0));
        shiftTurnMeter(target, toNumber(effect.enemyAmount, 0));
      } else if (effect.type === "attune-last-hit-element") {
        actor.nextSpellElement = actor.lastDamageElement;
        log(`${actorLabel(actor)} attunes their next spell to ${actor.nextSpellElement}.`);
      } else if (effect.type === "raise-standard") {
        const standardCharacter = defaultCharacter();
        standardCharacter.level = actor.id === "player" ? level : actor.character.level;
        standardCharacter.stats = { ...standardCharacter.stats, speed: 0, vitality: 1 };
        battle.summonSequence = Math.max(0, toInteger(battle.summonSequence, 0)) + 1;
        const standard = { id: `standard-${battle.summonSequence}`, name: "Battle Standard", team: actor.team, counter: 0, turns: 0, effects: [], skillExpiry: {}, alive: true, summoned: true, summonerId: actor.id, standardAura: toNumber(effect.damagePercent, 15), character: standardCharacter, baseArmor: 0 };
        const maximum = Math.max(1, Math.floor(values(actor).maxHp * Math.max(1, toNumber(effect.hpPercent, 15)) / 100));
        standard.resourceMax = { hp: maximum, mana: 0 };
        standard.character.currentHp = maximum;
        standard.character.currentMana = 0;
        battle.actors.push(standard);
        log(`${actorLabel(actor)} raises a vulnerable Battle Standard (${maximum} HP).`);
      } else if (effect.type === "gambit-strike" && target) {
        const hit = strike(actor, target, attackLoadout(actor).primary, 1 + Math.max(0, toNumber(effect.powerPercent, 0)) / 100, false, toNumber(effect.hitPercent, 0));
        if (hit) battle.ap = Math.min(actionPointMaximum(actor), battle.ap + Math.max(0, toInteger(effect.refundAp, 0)));
        else battle.ap = 0;
      } else if (effect.type === "execute-strike" && target) {
        const multiplier = hp(target) < values(target).maxHp * toNumber(effect.threshold, 35) / 100 ? 1 + toNumber(effect.damagePercent, 35) / 100 : 1;
        strike(actor, target, attackLoadout(actor).primary, multiplier);
      } else if (effect.type === "mute-next-spell-percent" && target) {
        target.effects.push({ key: `${key}:mute`, name: skill.name, type: "next-spell-penalty", amount: Math.max(0, toNumber(effect.amount, 0)), expiresTurn: Number.MAX_SAFE_INTEGER });
      } else if (effect.type === "blood-transfusion" && target && hp(actor) > 1) {
        const cost = Math.min(hp(actor) - 1, Math.floor(values(actor).maxHp * Math.max(0, toNumber(effect.hpCostPercent, 0)) / 100));
        if (cost) { setHp(actor, hp(actor) - cost); actor.damageTaken = Math.max(0, toNumber(actor.damageTaken, 0)) + cost; }
        const triage = hp(actor) < values(actor).maxHp / 2 ? passiveEffectsOf(actor).filter(passive => passive.type === "low-hp-healing-percent").reduce((sum, passive) => sum + toNumber(passive.amount, 0), 0) : 0;
        const heal = healingAmount(target, Math.floor(values(target).maxHp * Math.max(0, toNumber(effect.healPercent, 0)) / 100 * (1 + triage / 100)), actor);
        if (!healingDisabled(target)) setHp(target, Math.min(values(target).maxHp, hp(target) + heal));
        log(`${actorLabel(actor)} gives ${cost} HP to heal ${actorLabel(target)} by ${heal}.`);
      } else if (effect.type === "restore-summons-percent") {
        for (const summoned of living().filter(candidate => candidate.team === actor.team && candidate.summoned)) {
          const maximum = values(summoned);
          if (!healingDisabled(summoned)) healHp(summoned, Math.floor(maximum.maxHp * toNumber(effect.amount, 0) / 100), battle, actor);
          setMana(summoned, Math.min(maximum.maxMana, mana(summoned) + Math.floor(maximum.maxMana * toNumber(effect.amount, 0) / 100)));
        }
      } else if (["buff-summons-defense-flat", "buff-summons-defense-percent"].includes(effect.type)) {
        for (const summoned of living().filter(candidate => candidate.team === actor.team && candidate.summoned && (!effect.animalOnly || ["wolf", "bear", "griffon"].includes(actorRace(candidate))))) applyTimedEffects(summoned, key, skill.name, [{ type: effect.type.endsWith("percent") ? "defense-percent" : "defense-flat", target: effect.target || "armor", amount: effect.amount, durationTurns: effect.durationTurns || 2 }]);
      } else if (effect.type === "poison-all-enemies") {
        for (const enemy of opponents(actor).filter(candidate => !debuffImmune(candidate))) enemy.effects.push({ key: `${key}:poison`, name: skill.name, type: "class-poison", ownerId: actor.id, amount: Math.max(0, toNumber(effect.damage, 0)), percentMaxHp: Math.max(0, toNumber(effect.percentMaxHp, 0)), expiresTurn: enemy.turns + Math.max(1, toInteger(effect.durationTurns, 1)) });
      } else if (effect.type === "sunflare") {
        const minutes = (worldState.minutes % 1440 + 1440) % 1440;
        const damage = minutes >= 360 && minutes < 1080 ? effect.dayDamage : effect.nightDamage;
        for (const enemy of [...opponents(actor)]) hurt(enemy, Math.max(0, toNumber(damage, 0)), "holy", false, true, actor, "spell");
      } else if (effect.type === "stormburst") {
        const charges = Math.max(0, toInteger(actor.stormCharges, 0));
        for (const enemy of [...opponents(actor)]) hurt(enemy, charges * Math.max(0, toNumber(effect.damagePerCharge, 0)), ["wind", "lightning"], false, true, actor, "spell");
        for (const ally of living().filter(candidate => candidate.team === actor.team)) ally.counter = Math.min(999, ally.counter + charges * Math.max(0, toNumber(effect.turnAdvancePerCharge, 0)));
        actor.stormCharges = 0;
      }
    }
    if (skill.consumeSorceryPoints) {
      const points = Math.max(0, toInteger(actor.sorceryPoints, 0));
      actor.sorceryPoints = 0;
      const damage = target ? hurt(target, points * Math.max(0, toNumber(skill.damagePerSorceryPoint, 5)), skill.element || "basic", false, true, actor, "spell") : 0;
      if (target) log(`${skill.name} consumes ${points} Sorcery Point${points === 1 ? "" : "s"} and deals ${damage} ${skill.element || "basic"} damage to ${actorLabel(target)}.`);
    }
    const timedEffects = (skill.onActivateEffects || []).filter(effect => !["restore-full-resources", "refresh-other-skills", "turn-start-max-hp-loss-percent", "consume-all-blood-supplies-stat-percent", "restore-resource-percent", "ki-restore-mana", "random-animal-summon", "cast-random-spell", "weapon-attack-all-enemies", "apply-skill-effects", "resource-exchange", "start-duel", "guard-ally", "copy-enemy-spell", "shift-turn-meters", "attune-last-hit-element", "raise-standard", "gambit-strike", "execute-strike", "mute-next-spell-percent", "blood-transfusion", "restore-summons-percent", "buff-summons-defense-flat", "buff-summons-defense-percent", "poison-all-enemies", "sunflare", "stormburst"].includes(effect.type));
    const recipients = skill.target === "all-allies" ? living().filter(candidate => candidate.team === actor.team) : [actor];
    recipients.forEach(recipient => applyTimedEffects(recipient, key, skill.name, timedEffects, actor));
    log(`${actorLabel(actor)} activates ${skill.name} (${actionPoints} AP).`);
    return true;
  }
  function stance(actor, name) {
    if (!actionAllowed(actor, name.toLowerCase()) || !spend(actor, 2)) return false;
    recordTurnAction(actor, name.toLowerCase());
    if (actor.id === "player") defensiveActionsUsed += 1;
    else actor.character.defensiveActionsUsed = Math.max(0, toInteger(actor.character.defensiveActionsUsed, 0)) + 1;
    actor.effects = actor.effects.filter(effect => effect.type !== "stance");
    actor.effects.push({ type: "stance", name, expiresTurn: actor.turns + 1 });
    if (onStage(actor)) stage("stance", { actorId: actor.id, stance: name, shield: withActor(actor, equippedDefenseItems).some(item => item.type === "shield") });
    log(`${actorLabel(actor)} uses ${name} until their next turn.`);
    return true;
  }
  function meditate(actor) {
    if (!actionAllowed(actor, "meditate") || !spend(actor, 4)) return false;
    recordTurnAction(actor, "meditate");
    if (onStage(actor)) stage("meditate", { actorId: actor.id });
    const v = values(actor), gain = Math.min(v.maxMana - mana(actor), rules.meditate(v.maxMana, v.mind, v.luck));
    setMana(actor, mana(actor) + gain); log(`${actorLabel(actor)} meditates and restores ${actor.team === "player" ? gain + " " : ""}Mana.`);
    return true;
  }
  function run(actor) {
    if (!actionAllowed(actor, "run") || !spend(actor, 1)) return false;
    recordTurnAction(actor, "run");
    const v = values(actor), fastest = Math.max(0, ...opponents(actor).map(enemy => values(enemy).speed));
    const chance = rules.escape(v.speed, v.luck, fastest);
    const escaped = rules.roll(chance);
    if (onStage(actor)) stage("run", { actorId: actor.id, success: escaped });
    if (escaped) finish(`Escaped — you fled the battle (${formatValue(chance)}% chance).`);
    else log(`${actorLabel(actor)} fails to escape (${formatValue(chance)}% chance).`);
    return true;
  }
  function selectedTarget() { return actorById(battle?.targetId); }
  function action(name, key, option = null) {
    pendingLament = null;
    if (!isControlledTurn() || hasPlayerEquipmentReaction()) return false;
    const actor = currentActor(), target = selectedTarget();
    let done = false;
    if (name === "attack") done = attack(actor, target);
    if (name === "spell") done = cast(actor, target, getSpell(key), typeof option === "string" ? option : option?.weatherChoiceId || null, typeof option === "object" && option ? option : {});
    if (name === "item") done = useItem(actor, target, getItem(key), typeof option === "object" && option ? option : {});
    if (name === "skill") done = useSkill(actor, key, target, option);
    if (name === "guard" || name === "evade") done = stance(actor, name === "guard" ? "Guard" : "Evade");
    if (name === "meditate") done = meditate(actor);
    if (name === "run") done = run(actor);
    if (name === "end") { endTurn(); return true; }
    $("combat-action-status").textContent = done ? "" : "This action is unavailable. Check your AP, Mana, Ki, target, and the selected ability.";
    if (done) refresh();
    return done;
  }
  // Score one action against the live battlefield, execute it, then replan after refresh.
  const aiRatio = (current, maximum) => Math.max(0, Math.min(1, toNumber(current, 0) / Math.max(1, toNumber(maximum, 1))));
  const aiLevel = actor => Math.max(1, toInteger(actor.id === "player" ? level : actor.character?.level, 1));
  const aiAllies = actor => living().filter(candidate => candidate.team === actor.team);

  function estimatedDamage(target, power, element, critical = false, spell = false, attacker = null, attackKind = "", defenseOverride = null, armorPierceOverride = 0, unreducible = false) {
    if (!(power > 0)) return 0;
    const mark = attacker && target.effects.find(effect => effect.type === "hunter-mark" && effect.ownerId === attacker.id && skillStates(attacker)[effect.skillKey]?.active);
    const attackerEffects = attacker ? withActor(attacker, () => calculateClassEffects()) : null;
    const spellDamagePercent = spell ? (attackerEffects?.spellDamagePercentModifiers || []).filter(modifier => {
      const restricted = [modifier.element, ...(modifier.elements || [])].filter(Boolean);
      return !restricted.length || restricted.some(candidate => elementList(element).includes(candidate));
    }).reduce((sum, modifier) => sum + toNumber(modifier.amount, 0), 0) : 0;
    const tacticalMark = attacker && target.effects.find(effect => effect.type === "tactical-mark" && effect.team === attacker.team);
    const eminenceBonus = attacker ? passiveEffectsOf(attacker).filter(effect => effect.type === "damage-percent-by-untouched").reduce((sum, effect) => sum + untouchedBonus(attacker, effect), 0) : 0;
    const canCharge = attacker && ["melee", "ranged", "spell"].includes(attackKind);
    const worldSaver = canCharge ? chargedSkillEffect(attacker, "next-damage-unreducible") : null;
    const worldEnder = canCharge ? chargedSkillEffect(attacker, "next-damage-overload") : null;
    const terrorBonus = attacker ? passiveEffectsOf(attacker).filter(effect => effect.type === "damage-percent-vs-low-hp" && hp(target) < values(target).maxHp * toNumber(effect.threshold, 50) / 100).reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0) : 0;
    const chargeMultiplier = attacker && ["melee", "ranged", "spell", "bonus"].includes(attackKind) ? equipmentActionDamage.get(attacker.id) || equipmentChargeMultiplier(attacker) : 1;
    const markedPower = power * chargeMultiplier * (1 + (spell ? toNumber(attacker?.phaseSpellDamagePercent, 0) : 0) / 100) * (1 + toNumber(mark?.damagePercent, 0) / 100) * (1 + toNumber(tacticalMark?.damagePercent, 0) / 100) * (1 + damagePercentAgainstTarget(attacker, target) / 100) * (1 + (toNumber(attackerEffects?.outgoingDamagePercent, 0) + spellDamagePercent + eminenceBonus + terrorBonus + artifactDamagePercent(attacker, target)) / 100) * (1 + toNumber(worldEnder?.effect?.damagePercent, 0) / 100);
    const armorPierce = Math.max(0, Math.min(100, toNumber(armorPierceOverride, 0) + toNumber(worldEnder?.effect?.armorPiercePercent, 0) + (["melee", "ranged"].includes(attackKind) ? toNumber(attackerEffects?.weaponArmorPiercePercent, 0) + toNumber(attackerEffects?.weaponArmorPiercePercentByType?.[attackKind], 0) : 0)));
    const armor = Number.isFinite(defenseOverride?.armor) ? defenseOverride.armor : values(target).armor;
    const criticalBonusPercent = toNumber(attackerEffects?.criticalDamagePercent, 0) + (["melee", "ranged"].includes(attackKind) ? activeWeaponCritDamage : 0) + artifactAmount(attacker, "crit-damage-percent");
    const rangedArmorPercent = attackKind === "ranged" ? target.effects.filter(effect => effect.type === "incoming-ranged-armor-percent").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0) : 0;
    let amount = worldSaver || unreducible ? Math.floor(markedPower * rules.criticalMultiplier(critical, criticalBonusPercent)) : rules.damage(markedPower, spell ? 0 : armor * (1 + rangedArmorPercent / 100) * (1 - armorPierce / 100), resistance(target, element, spell), critical, criticalBonusPercent) * incomingDamageFactor(target, attacker, attackKind, element);
    if (spell && !worldSaver && !unreducible) {
      for (const effect of passiveEffectsOf(target).filter(effect => effect.type === "incoming-spell-extra-reduction-chance")) {
        const chance = rules.chance(toNumber(effect.chance, 0)) / 100;
        const reduction = Math.max(0, Math.min(100, toNumber(effect.amount, 0))) / 100;
        amount *= 1 - chance * reduction;
      }
      const rune = target.effects.find(effect => effect.type === "trap-spell-rune");
      if (rune) amount *= 1 - Math.max(0, Math.min(100, toNumber(rune.amount, 0))) / 100;
    }
    return Math.max(0, Math.floor(amount));
  }

  function withSuppressedAttackSkills(actor, callback) {
    const states = skillStates(actor), changed = [];
    for (const skill of skillsOf(actor)) {
      const key = classSkillKey(skill), state = states[key];
      if (!skill.durationAttacks || !state?.active) continue;
      changed.push([state, state.active]); state.active = false;
    }
    // The off-hand strike must not reuse primary-hand charged skill bonuses.
    try { return changed.length ? withCalculationFrame(callback) : callback(); }
    finally { changed.forEach(([state, active]) => { state.active = active; }); }
  }

  function estimatedWeaponStrike(actor, target, weapon, damageMultiplier = 1, offHand = false, defenseOverride = null) {
    const targetValues = values(target);
    const evaluate = () => weaponAttackValues(actor, weapon);
    const attackValues = offHand ? withSuppressedAttackSkills(actor, evaluate) : evaluate();
    const weaponType = normalizeWeaponType(weapon.type);
    const mark = target.effects.find(effect => effect.type === "hunter-mark" && effect.ownerId === actor.id && skillStates(actor)[effect.skillKey]?.active);
    const targetHitEffects = withActor(target, () => calculateClassEffects().incomingWeaponHitPercentByType);
    const weatherHitPercent = actor.effects.filter(effect => effect.type === "weapon-hit-percent" && (!effect.weaponType || normalizeWeaponType(effect.weaponType) === weaponType)).reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
    const rangedHitPercent = weaponType === "ranged" ? target.effects.filter(effect => effect.type === "incoming-ranged-hit-percent").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0) : 0;
    const incomingHitPercent = toNumber(targetHitEffects.all, 0) + toNumber(targetHitEffects[weaponType], 0) + rangedHitPercent;
    const targetEvasion = Number.isFinite(defenseOverride?.evasion) ? defenseOverride.evasion : targetValues.evasion;
    const hitChance = rules.weaponHitChance((attackValues.hit + toNumber(mark?.hitFlat, 0)) * (1 + (weatherHitPercent + toNumber(mark?.hitPercent, 0)) / 100) * (1 + incomingHitPercent / 100), evasionAgainst(targetEvasion, weaponType));
    const critChance = weaponCritChanceAgainst(target, weapon, attackValues.crit, attackValues.critRating);
    const guaranteedHit = attackValues.alwaysHit || target.effects.some(effect => effect.type === "incoming-weapon-hit-always");
    const hitProbability = guaranteedHit ? 1 : hitChance / 100;
    const criticalProbability = attackValues.alwaysCrit ? 1 : rules.critChanceAfterMisses(critChance, actor.critMisses) / 100;
    const incomingDamagePercent = target.effects.filter(effect => effect.type === "incoming-weapon-damage-percent").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0);
    const firstTurnDamagePercent = (target.turns === 0 ? passiveEffectsOf(actor).filter(effect => effect.type === "weapon-damage-before-first-turn-percent").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0) : 0) + (!offHand && actor.turns === 1 && !actor.firstStrikeUsed ? passiveEffectsOf(actor).filter(effect => effect.type === "first-attack-damage-percent").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0) : 0);
    const activeAttackSkills = offHand ? [] : skillsOf(actor).filter(skill => skill.durationAttacks && skillStates(actor)[classSkillKey(skill)]?.active && weaponMatches(weapon, skill.requiredWeaponType));
    const conditionalPower = activeAttackSkills.flatMap(skill => skill.effects || []).filter(effect =>
      effect.type === "weapon-power-percent-vs-race" && (effect.raceIds || [effect.raceId]).filter(Boolean).map(slugify).includes(actorRace(target)) ||
      effect.type === "weapon-power-percent-vs-class" && (effect.classIds || [effect.classId]).filter(Boolean).some(classId => hasClass(target, slugify(classId))) ||
      effect.type === "weapon-power-percent-by-untouched"
    ).reduce((sum, effect) => sum + (effect.type === "weapon-power-percent-by-untouched" ? untouchedBonus(actor, effect) : toNumber(effect.amount, 0)), 0);
    const actorPassives = passiveEffectsOf(actor);
    const passivePower = (actor.previousTurnAttackTargetIds || []).includes(target.id) ? actorPassives.filter(effect => effect.type === "repeat-target-damage-percent").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0) : 0;
    const slowerTargetPower = values(actor).speed > targetValues.speed ? (weapon.effects || []).filter(effect => effect.kind === "damage-percent-vs-slower").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0) : 0;
    const largerHpBonus = largerHpWeaponBonus(actor, target, weapon);
    const starscale = (weapon.effects || []).find(effect => effect.kind === "bonus-basic-attack-percent");
    const power = attackValues.power * damageMultiplier * (1 + (incomingDamagePercent + firstTurnDamagePercent + conditionalPower + passivePower + slowerTargetPower + weaponRaceDamagePercent(weapon, target) + woundedTargetPercent(weapon, target) + largerHpBonus.damagePercent) / 100);
    const armorPierce = largerHpBonus.armorPiercePercent + toNumber(starscale?.armorPiercePercent, 0) + offHandArmorPierce(weapon, offHand) + toNumber(weapon.armorPierce, 0);
    const basicBonus = starscale ? estimatedDamage(target, power * starscale.amount / 100, "basic", false, false, actor, "bonus", defenseOverride, armorPierce) : 0;
    const armorBonusPercent = (weapon.effects || []).filter(effect => effect.kind === "bonus-damage-from-armor-percent").reduce((sum, effect) => sum + effect.amount, 0);
    const armorBonus = armorBonusPercent ? estimatedDamage(target, values(actor).armor * armorBonusPercent / 100 * damageMultiplier, "physical", false, false, actor, "bonus", defenseOverride) : 0;
    const normalDamage = withWeaponElement(weapon, () => estimatedDamage(target, power, strikeElement(actor, weapon), false, false, actor, weaponType, defenseOverride, armorPierce), target) + basicBonus + armorBonus;
    const criticalDamage = withWeaponElement(weapon, () => estimatedDamage(target, power, strikeElement(actor, weapon), true, false, actor, weaponType, defenseOverride, armorPierce), target) + basicBonus + armorBonus;
    const resources = forecastResources(target);
    const normal = forecastDamageResources(target, normalDamage, resources);
    const critical = forecastDamageResources(target, criticalDamage, resources);
    const expectedDamage = hitProbability * ((1 - criticalProbability) * normalDamage + criticalProbability * criticalDamage);
    const effectiveDamage = hitProbability * ((1 - criticalProbability) * forecastResourceLoss(resources, normal) + criticalProbability * forecastResourceLoss(resources, critical));
    const onHitLoss = amount => {
      const after = forecastDamageResources(target, amount, resources);
      return resources.hp - after.hp + resources.mana - after.mana;
    };
    // On-hit healing uses the primary strike's actual resource damage, not
    // barrier pressure, off-hand damage, or separate unique-weapon bonuses.
    const onHitDamage = hitProbability * ((1 - criticalProbability) * onHitLoss(normalDamage - basicBonus - armorBonus) + criticalProbability * onHitLoss(criticalDamage - basicBonus - armorBonus));
    const killProbability = hitProbability * ((1 - criticalProbability) * Number(normal.hp <= 0) + criticalProbability * Number(critical.hp <= 0));
    const outcomes = [{ probability: 1 - hitProbability, damage: 0 },
      { probability: hitProbability * (1 - criticalProbability), damage: normalDamage },
      { probability: hitProbability * criticalProbability, damage: criticalDamage }].filter(outcome => outcome.probability > 0);
    return { expectedDamage, effectiveDamage, onHitDamage, killProbability, hitChance, critChance, normalDamage, criticalDamage, weaponType, outcomes };
  }

  function forecastResources(target) {
    return { hp: hp(target), mana: mana(target), barrier: Math.max(0, toNumber(target.barrierHp, 0)) };
  }
  function forecastDamageResources(target, rawAmount, previous = forecastResources(target)) {
    const result = { ...previous };
    let amount = Math.max(0, Math.floor(rawAmount));
    if (result.hp <= 0 || !amount) return result;
    const shielded = Math.min(amount, result.barrier);
    result.barrier -= shielded; amount -= shielded;
    const floor = phaseDamageFloor(target);
    if (lichManaBarrier(target) && result.mana > 0) {
      const healthLoss = Math.min(amount, Math.max(0, result.hp - Math.max(1, floor)));
      const manaLoss = Math.min(result.mana, amount - healthLoss);
      const remaining = Math.min(floor ? 0 : 1, amount - healthLoss - manaLoss);
      result.hp -= healthLoss + remaining; result.mana -= manaLoss;
    } else result.hp -= Math.min(Math.max(0, result.hp - floor), amount);
    return result;
  }
  function forecastResourceLoss(before, after) {
    // Breaking a shield or draining Immortal Body's Mana is useful pressure,
    // but neither is a guaranteed finishing blow to the protected target.
    return Math.max(0, before.hp - after.hp) + Math.max(0, before.mana - after.mana) * 0.65 + Math.max(0, before.barrier - after.barrier) * 0.35;
  }

  function estimatedAttack(actor, target, defenseOverride = null) {
    const key = `attack:${target.id}:${defenseOverride?.armor ?? "normal"}:${defenseOverride?.evasion ?? "normal"}`;
    return cacheCalculation(key, () => calculateEstimatedAttack(actor, target, defenseOverride), actor);
  }
  function forecastRedirections(actor, target, attackKind) {
    return cacheCalculation(`redirect:${target.id}:${attackKind}`, () => {
      if (target.team === actor.team) return [{ target, probability: 1 }];
      const foes = opponents(actor);
      const taunter = foes.filter(foe => foe.id !== target.id && foe.effects.some(effect => effect.type === "taunt-chance"))
        .sort((a, b) => values(b).armor - values(a).armor)[0];
      const tauntChance = taunter ? rules.chance(Math.max(0, ...taunter.effects.filter(effect => effect.type === "taunt-chance").map(effect => toNumber(effect.amount, 0)))) / 100 : 0;
      const initial = [{ target, probability: 1 - tauntChance }, ...(taunter ? [{ target: taunter, probability: tauntChance }] : [])];
      const result = new Map();
      const add = (recipient, probability) => { if (probability > 0) result.set(recipient, (result.get(recipient) || 0) + probability); };
      for (const entry of initial) {
        const eligible = living().filter(ally => ally.team === entry.target.team && ally.id !== entry.target.id && foes.includes(ally));
        const ward = entry.target.effects.find(effect => effect.type === "guardian-ward" && effect.expiresTurn > entry.target.turns);
        const guardian = phylacteryGuardFor(entry.target, eligible) || ward && eligible.find(ally => ally.id === ward.guardianId);
        if (guardian) { add(guardian, entry.probability); continue; }
        const interceptor = eligible.map(ally => ({ target: ally, probability: rules.chance(Math.max(0, ...passiveEffectsOf(ally).filter(effect => effect.type === "intercept-ally-attack-chance").map(effect => toNumber(effect.amount, 0)))) / 100 }))
          .sort((a, b) => b.probability - a.probability || values(b.target).armor - values(a.target).armor)[0];
        add(entry.target, entry.probability * (1 - (interceptor?.probability || 0)));
        if (interceptor) add(interceptor.target, entry.probability * interceptor.probability);
      }
      return [...result].map(([recipient, probability]) => ({ target: recipient, probability }));
    }, actor);
  }
  function calculateEstimatedAttack(actor, target, defenseOverride = null) {
    const loadout = attackLoadout(actor);
    const focusMultiplier = focusedPowerMultiplier(actor);
    const redirections = forecastRedirections(actor, target, normalizeWeaponType(loadout.primary.type));
    if (redirections.length !== 1 || redirections[0].target !== target) {
      const outcomes = redirections.map(entry => ({ probability: entry.probability, outcome: calculateEstimatedDirectAttack(actor, entry.target, loadout, focusMultiplier, entry.target === target ? defenseOverride : null) }));
      const dominant = outcomes.sort((a, b) => b.probability - a.probability)[0]?.outcome;
      return { ...dominant, cost: loadout.cost,
        expectedDamage: outcomes.reduce((sum, entry) => sum + entry.outcome.expectedDamage * entry.probability, 0),
        effectiveDamage: outcomes.reduce((sum, entry) => sum + entry.outcome.effectiveDamage * entry.probability, 0),
        onHitDamage: outcomes.reduce((sum, entry) => sum + entry.outcome.onHitDamage * entry.probability, 0),
        killProbability: outcomes.reduce((sum, entry) => sum + entry.outcome.killProbability * entry.probability, 0) };
    }
    return calculateEstimatedDirectAttack(actor, target, loadout, focusMultiplier, defenseOverride);
  }
  function calculateEstimatedDirectAttack(actor, target, loadout, focusMultiplier, defenseOverride) {
    const primary = estimatedWeaponStrike(actor, target, loadout.primary, focusMultiplier, false, defenseOverride);
    let expectedDamage = primary.expectedDamage, effectiveDamage = primary.effectiveDamage, killProbability = primary.killProbability;
    let offHand = null;
    if (loadout.offHand) {
      offHand = estimatedWeaponStrike(actor, target, loadout.offHand, 0.25 * focusMultiplier, true, defenseOverride);
      const resources = forecastResources(target);
      expectedDamage = 0; effectiveDamage = 0; killProbability = 0;
      for (const first of primary.outcomes) for (const second of offHand.outcomes) {
        const probability = first.probability * second.probability;
        const afterPrimary = forecastDamageResources(target, first.damage, resources);
        const after = afterPrimary.hp > 0 ? forecastDamageResources(target, second.damage, afterPrimary) : afterPrimary;
        expectedDamage += (first.damage + (afterPrimary.hp > 0 ? second.damage : 0)) * probability;
        effectiveDamage += forecastResourceLoss(resources, after) * probability;
        killProbability += Number(after.hp <= 0) * probability;
      }
    }
    return { ...primary, expectedDamage, effectiveDamage, killProbability, offHand, cost: loadout.cost };
  }

  function effectAmount(effect, power) {
    const amount = effect.amount === "power" ? power : toNumber(effect.amount, 0);
    return amount + toNumber(effect.flatAmount, 0);
  }

  function estimatedChargeDamage(actor, target, effect, defenseOverride = null) {
    const weapon = attackLoadout(actor).primary, multiplier = focusedPowerMultiplier(actor);
    const hit = estimatedWeaponStrike(actor, target, weapon, multiplier, false, defenseOverride);
    const guaranteed = weaponAttackValues(actor, weapon).alwaysHit || target.effects.some(entry => entry.type === "incoming-weapon-hit-always");
    const fire = estimatedDamage(target, weaponAttackValues(actor, weapon).power * multiplier * effectAmount(effect, 0) / 100, effect.element || "fire", false, false, actor, "bonus", defenseOverride);
    return hit.expectedDamage + fire * (guaranteed ? 1 : hit.hitChance / 100);
  }

  function estimatedSpellDamage(actor, target, spell, defenseOverride = null) {
    const power = forecastSpellPower(actor, spell);
    const spellElements = forecastSpellElements(actor, spell);
    return spellEffects(spell).reduce((sum, effect) => {
      if (effect.type === "weapon-charge-all-enemies") return sum + estimatedChargeDamage(actor, target, effect, defenseOverride);
      return effect.type === "damage" ? sum + estimatedSpellEffectDamage(actor, target, spell, effect, power, spellElements, defenseOverride) : sum;
    }, 0) * forecastSpellRepeats(actor, spell);
  }
  function forecastSpellRepeats(actor, spell) {
    return 1 + skillsOf(actor).filter(skill => skill.durationSpells && skillStates(actor)[classSkillKey(skill)]?.active && skillMatchesSpell(actor, skill, spell))
      .flatMap(skill => skill.effects || []).filter(effect => effect.type === "repeat-spell").reduce((sum, effect) => sum + Math.max(1, toInteger(effect.amount, 1)), 0);
  }

  function estimatedSpellEffectDamage(actor, target, spell, effect, power, elements, defenseOverride = null, apexChoice = null) {
    return estimatedSpellEffectOutcome(actor, target, spell, effect, power, elements, defenseOverride, apexChoice).expectedDamage;
  }
  function estimatedSpellEffectOutcome(actor, target, spell, effect, power, elements, defenseOverride = null, apexChoice = null) {
    const apex = goldenApexEquipped(actor) ? apexChoice || (opponents(actor).length > 1 ? "sweep" : "focus") : null;
    const multiplier = apex === "focus" ? 2 : apex === "sweep" ? 1.35 : 1;
    const recipients = effect.recipient === "all-enemies" || apex === "sweep" ? [{ target, probability: 1 }] : forecastRedirections(actor, target, "spell");
    return recipients.reduce((outcome, entry) => {
      const raceBonus = (effect.raceIds || []).map(slugify).includes(actorRace(entry.target)) ? toNumber(effect.raceDamagePercent, 0) : 0;
      const probability = 1 - rules.chance(spellMissChanceAgainst(actor, entry.target, spell)) / 100;
      const damage = estimatedDamage(entry.target, effectAmount(effect, power) * multiplier * (1 + raceBonus / 100), elements, false, true, actor, "spell", entry.target === target ? defenseOverride : null, toNumber(effect.armorPiercePercent, 0), apex === "focus");
      const repeats = forecastSpellRepeats(actor, spell);
      const resources = forecastResources(entry.target);
      const after = forecastDamageResources(entry.target, damage * repeats, resources);
      const effective = forecastResourceLoss(resources, after);
      const chance = probability * entry.probability;
      outcome.expectedDamage += damage * chance;
      outcome.effectiveDamage += effective * chance;
      outcome.dealtDamage += (resources.hp - after.hp + resources.mana - after.mana) * chance;
      outcome.killProbability += Number(after.hp <= 0) * chance;
      return outcome;
    }, { expectedDamage: 0, effectiveDamage: 0, dealtDamage: 0, killProbability: 0 });
  }

  function combatantThreat(actor, target, defenseOverride = null) {
    if (!actor || hp(actor) <= 0) return 0;
    const key = `threat:${target.id}:${defenseOverride?.armor ?? "normal"}:${defenseOverride?.evasion ?? "normal"}`;
    return cacheCalculation(key, () => calculateCombatantThreat(actor, target, defenseOverride), actor);
  }
  function calculateCombatantThreat(actor, target, defenseOverride = null) {
    const maximumAp = actionPointMaximum(actor);
    const weapon = estimatedAttack(actor, target, defenseOverride);
    const weaponTurns = Math.max(1, Math.floor(maximumAp / Math.max(1, weapon.cost)));
    let best = weapon.expectedDamage * weaponTurns;
    for (const spell of spellsOf(actor)) {
      const ap = forecastSpellAp(actor, spell);
      const cost = forecastSpellCost(actor, spell);
      if (ap > maximumAp || cost > mana(actor) || !spellEffects(spell).some(effect => ["damage", "weapon-charge-all-enemies"].includes(effect.type))) continue;
      const casts = Math.max(1, Math.min(3, Math.floor(maximumAp / ap), cost > 0 ? Math.floor(mana(actor) / cost) : maximumAp));
      best = Math.max(best, estimatedSpellDamage(actor, target, spell, defenseOverride) * casts);
    }
    const speedFactor = Math.max(0.65, Math.min(1.6, values(actor).speed / Math.max(1, values(target).speed)));
    return best * speedFactor + aiLevel(actor) * 0.15;
  }

  function targetPriority(actor, target, outcome = null) {
    const targetValues = values(target), health = aiRatio(hp(target), targetValues.maxHp);
    const threat = combatantThreat(target, actor);
    const threatPriority = Math.min(14, threat / Math.max(1, values(actor).maxHp) * 12);
    const healer = spellsOf(target).some(spell => spellEffects(spell).some(effect => effect.type === "heal")) ? 4 : 0;
    const exposed = target.effects.some(effect => effect.type === "incoming-weapon-hit-always" || effect.type === "incoming-weapon-damage-percent" && effect.amount > 0) ? 3 : 0;
    const sentryRisk = target.effects.some(effect => effect.type === "trap-sentry") ? Math.min(8, hp(actor) * 0.08) : 0;
    const hasAlly = living().some(ally => ally.team === target.team && ally.id !== target.id && !ally.summoned);
    const stealthPenalty = hasAlly ? passiveEffectsOf(target).filter(effect => effect.type === "target-priority-flat").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0) + target.effects.filter(effect => effect.type === "target-priority-flat").reduce((sum, effect) => sum + toNumber(effect.amount, 0), 0) : 0;
    const focus = actor.ai?.focusId === target.id ? 3 : 0;
    const coordinated = aiAllies(actor).some(ally => ally.id !== actor.id && ally.ai?.focusId === target.id) ? 2 : 0;
    const disabled = target.effects.some(effect => effect.type === "stun" && effect.expiresTurn > target.turns) ? 3 : 0;
    return threatPriority + (1 - health) * 9 + toNumber(outcome?.killProbability, 0) * 36 + healer + exposed + focus + coordinated + Math.min(3, target.counter / 300) - sentryRisk + stealthPenalty - disabled;
  }

  function attackCandidates(actor) {
    if (!actionAllowed(actor, "attack")) return [];
    const loadout = attackLoadout(actor);
    if (battle.ap < loadout.cost) return [];
    return opponents(actor).map(target => {
      const outcome = estimatedAttack(actor, target);
      const score = outcome.effectiveDamage * 4 / Math.max(1, outcome.cost) + targetPriority(actor, target, outcome);
      const cleave = (loadout.primary.effects || []).filter(effect => effect.kind === "unreducible-cleave-percent").reduce((sum, effect) => sum + effect.amount, 0);
      const collateral = cleave ? opponents(actor).filter(other => other !== target).reduce((sum, other) => sum + Math.min(hp(other), outcome.expectedDamage * cleave / 100), 0) : 0;
      const reducedCleave = (loadout.primary.effects || []).filter(effect => effect.kind === "cleave-percent").reduce((sum, effect) => sum + effect.amount, 0);
      const splashDamage = reducedCleave ? hostiles(actor).filter(other => other !== target).reduce((sum, other) => sum + Math.min(hp(other), estimatedDamage(other, outcome.expectedDamage * reducedCleave / 100, loadout.primary.element || "physical", false, false, actor, "bonus")), 0) : 0;
      const splash = normalizeWeaponType(loadout.primary.type) === "ranged" ? rangedSplashPercent(actor) : 0;
      const rainDamage = splash ? opponents(actor).filter(other => other !== target).reduce((sum, other) => sum + Math.min(hp(other), Math.floor(outcome.expectedDamage * splash / 100 * incomingDamageFactor(other))), 0) : 0;
      return { kind: "attack", target, outcome, score: score + (collateral + rainDamage + splashDamage) * 4 / Math.max(1, outcome.cost), tactics: { ap: loadout.cost, mana: 0, damage: outcome.effectiveDamage + collateral + rainDamage + splashDamage, killChance: outcome.killProbability, hostileTarget: true } };
    });
  }

  function spellTargetOptions(actor, effects) {
    const needsFriendly = effects.some(effect => effect.recipient === "friendly-target");
    const needsEnemy = requiresEnemyTarget(effects);
    if (needsFriendly && needsEnemy) return [];
    if (needsFriendly) return aiAllies(actor);
    if (needsEnemy) return opponents(actor);
    return [actor];
  }

  function timedEffectUtility(actor, target, effect, amount, source = "") {
    const recipient = effect.recipient === "target" ? target : actor;
    const fullDuration = Math.max(1, toInteger(effect.durationTurns, 1));
    const existing = source && recipient?.effects.find(active => active.name === source && active.type === effect.type && active.target === effect.target && active.expiresTurn > recipient.turns);
    const duration = existing ? Math.max(0, fullDuration - (existing.expiresTurn - recipient.turns)) : fullDuration;
    if (!duration) return 0;
    const enemyEffect = recipient?.team !== actor.team;
    if (enemyEffect && (debuffImmune(recipient) || withActor(recipient, () => calculateClassEffects().debuffImmune))) return 0;
    const sign = enemyEffect ? -1 : 1;
    if (effect.type === "all-stats-percent" || effect.type === "all-stats-permanent-percent") return sign * amount * duration * 0.7;
    if (["stat-percent", "stat-permanent-percent"].includes(effect.type)) {
      // Locked resource caps do not grow during combat. Do not spend a turn
      // increasing Mind/Vitality unless an equipped weapon or spell scales on it.
      const scales = [attackLoadout(recipient).primary, attackLoadout(recipient).offHand, ...spellsOf(recipient), ...withActor(recipient, equippedDefenseItems)].filter(Boolean)
        .some(entry => entry.primary?.stat === effect.target || entry.secondary?.stat === effect.target || entry.scaling?.stat === effect.target || entry.additionalScaling?.some(scale => scale.stat === effect.target));
      if (["mind", "vitality"].includes(effect.target) && !scales) return 0;
      if (effect.target === "speed" && (values(recipient).speed <= 0 || enemyEffect && values(recipient).speed < values(actor).speed * 0.3)) return 0;
      const relevance = ["strength", "dexterity", "intelligence", "mind"].includes(effect.target) ? 0.55 : ["vitality", "speed"].includes(effect.target) ? 0.7 : 0.4;
      return sign * amount * duration * relevance;
    }
    if (["defense-flat", "defense-percent", "defense-permanent-percent"].includes(effect.type)) {
      const pressure = recipient ? combatantThreat(opponents(recipient)[0], recipient) : 0;
      return sign * amount * duration * (0.35 + Math.min(0.45, pressure / Math.max(1, values(recipient || actor).maxHp)));
    }
    if (effect.type === "weapon-hit-flat") return sign * amount * duration * 0.32;
    if (effect.type === "incoming-weapon-hit-percent") return -sign * amount * duration * 0.22;
    if (effect.type === "incoming-attack-damage-percent" || effect.type === "incoming-damage-percent") {
      return -sign * amount * duration * 0.3;
    }
    if (effect.type === "incoming-spell-damage-percent" || effect.type === "incoming-spell-damage-from-target-percent") return -sign * amount * duration * 0.4;
    if (["damage-over-time", "bleed", "poison", "hp-loss-flat"].includes(effect.type)) return enemyEffect ? Math.max(0, amount) * duration * 2 : -Math.max(0, amount) * duration * 2;
    if (["hp-loss-percent", "damage-over-time-percent"].includes(effect.type)) return enemyEffect ? values(recipient).maxHp * Math.max(0, amount) / 100 * duration * 2 : 0;
    if (effect.type === "melee-retaliation") return Math.max(0, amount) * duration * 0.3;
    if (effect.type === "barrier") return Math.min(estimateIncomingPressure(recipient), Math.max(0, amount)) * 1.5;
    if (effect.type === "spell-power-percent" || effect.type === "weapon-power-percent") return sign * amount * duration * 0.65;
    if (effect.type === "spell-cost-percent") return sign * amount * duration * (aiRatio(mana(recipient), values(recipient).maxMana) < 0.5 ? 0.8 : 0.3);
    if (effect.type === "healing-received-percent") return sign * amount * duration * (hp(recipient) < values(recipient).maxHp * 0.6 ? 0.5 : 0.1);
    if (effect.type === "incoming-weapon-damage-percent") return -sign * amount * duration * 0.55;
    if (effect.type === "outgoing-attack-damage-percent") return sign * amount * duration * 0.65;
    if (effect.type === "incoming-spell-miss-chance") return sign * amount * duration * (opponents(recipient).some(foe => spellsOf(foe).length) ? 0.55 : 0);
    if (effect.type === "incoming-ranged-hit-percent" || effect.type === "incoming-ranged-armor-percent") return -sign * amount * duration * (opponents(recipient).some(foe => normalizeWeaponType(attackLoadout(foe).primary.type) === "ranged") ? 0.4 : 0);
    if (effect.type === "incoming-race-damage-percent") return -sign * amount * duration * (opponents(recipient).some(foe => (effect.raceIds || []).includes(actorRace(foe))) ? 0.5 : 0);
    if (effect.type === "unarmed-bonus-damage") return attackLoadout(recipient).primary.unarmed ? Math.max(0, amount) * duration * 2 : 0;
    if (effect.type === "flee-disabled") return enemyEffect && recipient.lastActionKind === "run" ? 8 : 0;
    if (effect.type === "delayed-hp-loss-percent") return -Math.max(0, amount) * values(recipient || actor).maxHp / 100 * 1.5;
    return 0;
  }

  function weatherEffectValue(recipient, effect, duration) {
    const amount = toNumber(effect.amount, 0), known = spellsOf(recipient);
    const fraction = entries => entries.length ? entries.filter(spell => effectMatchesElement(effect, forecastSpellElements(recipient, spell))).length / entries.length : 0;
    if (["spell-power-percent", "spell-cost-percent", "spell-miss-chance"].includes(effect.type)) {
      const relevant = known.filter(spell => effect.type === "spell-cost-percent" ? forecastSpellCost(recipient, spell) > 0 : spellEffects(spell).some(entry => entry.type === "damage" || entry.type === "heal"));
      const value = effect.type === "spell-power-percent" ? 0.65 : effect.type === "spell-miss-chance" ? -0.55 : 0.35 + (1 - aiRatio(mana(recipient), values(recipient).maxMana)) * 0.45;
      return amount * duration * value * fraction(relevant);
    }
    if (effect.type === "incoming-spell-damage-percent") {
      const hostileSpells = opponents(recipient).flatMap(enemy => spellsOf(enemy).filter(spell => spellEffects(spell).some(entry => entry.type === "damage")).map(spell => ({ spell, enemy })));
      return -amount * duration * 0.5 * (hostileSpells.length ? hostileSpells.filter(entry => effectMatchesElement(effect, forecastSpellElements(entry.enemy, entry.spell))).length / hostileSpells.length : 0);
    }
    if (effect.type === "weapon-hit-percent") return weaponMatches(attackLoadout(recipient).primary, effect.weaponType) ? amount * duration * 0.3 : 0;
    return timedEffectUtility(recipient, recipient, { ...effect, durationTurns: duration, recipient: "target" }, amount);
  }
  function weatherUtility(actor, effect, choiceId) {
    const choice = effect.choices?.find(entry => entry.id === choiceId);
    if (!choice) return 0;
    const horizon = Math.min(5, Math.max(1, toInteger(effect.durationTurns, 1)));
    if (battle.weather?.id === choice.id && living().every(recipient => !choice.effects?.length || recipient.effects.some(active => active.weather && active.expiresTurn - recipient.turns > 2))) return 0;
    return living().reduce((total, recipient) => {
      const before = recipient.effects.filter(active => active.weather && active.expiresTurn > recipient.turns)
        .reduce((sum, active) => sum + weatherEffectValue(recipient, active, Math.min(horizon, active.expiresTurn - recipient.turns)), 0);
      const after = (choice.effects || []).reduce((sum, active) => sum + weatherEffectValue(recipient, active, horizon), 0);
      return total + (after - before) * (recipient.team === actor.team ? 1 : -1);
    }, 0);
  }

  function evaluateSpellForTarget(actor, spell, target, effects, ap, cost, power, apexChoice = null, weatherChoiceId = null) {
    let utility = 0, didSomething = false;
    const tactics = { ap, mana: cost, damage: 0, healing: 0, killChance: 0, control: false, freshControl: false, support: false, hostileTarget: target?.team !== actor.team, recipientHealth: 1 };
    const recordHealing = (recipient, gain) => {
      tactics.healing += gain;
      const ratio = aiRatio(hp(recipient), values(recipient).maxHp);
      if (gain > 0 && ratio < tactics.recipientHealth) {
        tactics.recipientHealth = ratio; tactics.recipientMaxHp = values(recipient).maxHp; tactics.selfHeal = recipient.id === actor.id;
      }
    };
    const spellElements = forecastSpellElements(actor, spell);
    for (const effect of effects) {
      const amount = effectAmount(effect, power);
      if (effect.type === "weather-control") {
        const value = weatherUtility(actor, effect, weatherChoiceId);
        utility += value; didSomething ||= value > 0; tactics.support ||= value > 0;
      } else if (effect.type === "weapon-charge-all-enemies") {
        for (const recipient of opponents(actor)) {
          const damage = estimatedChargeDamage(actor, recipient, effect);
          utility += Math.min(hp(recipient), damage) * 4 + Number(damage >= hp(recipient)) * 34;
          tactics.damage += Math.min(hp(recipient), damage); tactics.killChance = Math.max(tactics.killChance, Number(damage >= hp(recipient)));
          didSomething ||= damage > 0;
        }
      } else if (effect.type === "damage") {
        const recipients = effect.recipient === "all-enemies" ? opponents(actor) : [target];
        for (const recipient of recipients.filter(Boolean)) {
          const outcome = estimatedSpellEffectOutcome(actor, recipient, spell, effect, power, spellElements, null, apexChoice);
          const effective = outcome.effectiveDamage;
          utility += effective * 4 + outcome.killProbability * 34 + (effective > 0 ? combatantThreat(recipient, actor) * 0.25 : 0);
          tactics.damage += effective; tactics.killChance = Math.max(tactics.killChance, outcome.killProbability);
          if (effect.lifeSteal) {
            const missing = Math.max(0, values(actor).maxHp - hp(actor));
            const gain = healingDisabled(actor) ? 0 : Math.min(missing, healingAmount(actor, outcome.dealtDamage * Math.max(0, toNumber(effect.lifeSteal, 0)), actor));
            utility += gain * 2;
            recordHealing(actor, gain);
          }
          didSomething ||= effective > 0;
        }
      } else if (effect.type === "heal" || effect.type === "restore-mana") {
        const recipients = forecastRecipients(actor, target, effect);
        for (const recipient of recipients) {
          const restoreHp = effect.type === "heal", maximum = values(recipient)[restoreHp ? "maxHp" : "maxMana"];
          const current = restoreHp ? hp(recipient) : mana(recipient);
          const restoration = rules.resourceRestore(amount, maximum, effect.maxPercent);
          const gain = (restoreHp && healingDisabled(recipient) ? 0 : Math.max(0, Math.min(maximum - current, restoreHp ? healingAmount(recipient, restoration, actor) : restoration))) / (effect.recipient === "random-ally" ? Math.max(1, recipients.length) : 1);
          const ratio = aiRatio(current, maximum);
          if (restoreHp) {
            utility += gain * (1.6 + (1 - ratio) * 2.6) + Number(gain > 0 && ratio < 0.3) * 18;
            recordHealing(recipient, gain);
          }
          else utility += gain * (0.65 + (1 - ratio) * 1.2) * (spellsOf(recipient).length ? 1 : 0.35);
          tactics.support ||= gain > 0; tactics.manaRecovery ||= !restoreHp && gain > 0;
          didSomething ||= gain > 0;
        }
      } else if (effect.type === "stun" || effect.type === "stun-chance") {
        const recipients = forecastRecipients(actor, target, effect);
        for (const recipient of recipients) {
          const probability = (effect.type === "stun" ? 1 : rules.chance(amount) / 100) / (effect.recipient === "random-ally" ? Math.max(1, recipients.length) : 1);
          const swing = combatantThreat(recipient, actor) + actionPointMaximum(recipient) * 1.5;
          const alreadyStunned = recipient.effects.some(active => active.type === "stun" && active.expiresTurn > recipient.turns);
          const immune = debuffImmune(recipient) || withActor(recipient, () => calculateClassEffects().stunImmune);
          utility += immune ? 0 : (recipient.team === actor.team ? -swing : swing) * probability * (alreadyStunned ? 0.05 : 1);
          tactics.control ||= !immune && recipient.team !== actor.team && probability > 0;
          tactics.freshControl ||= !immune && recipient.team !== actor.team && probability > 0 && !alreadyStunned;
          didSomething ||= !immune && probability > 0;
        }
      } else if (effect.type === "summon-friendly") {
        const definition = enemyDefinitions.find(enemy => enemy.id === effect.enemyId);
        if (!definition || summonLimitReached(actor, effect)) continue;
        const duration = effect.durationTurns ? Math.min(5, effect.durationTurns) / 5 : 1;
        const count = Math.max(1, toInteger(effect.count, 1));
        const summonPower = (1 + Math.max(0, toNumber(effect.summonMaxHpPercent, 0)) / 200) * (1 + Math.max(0, toNumber(effect.summonAttackDamagePercent, 0)) / 100);
        const gear = summonEquipmentBonuses(actor);
        const owned = living().filter(ally => ally.summoned && ally.summonerId === actor.id).length;
        const crowding = 1 / (1 + Math.max(0, owned - 1) * 0.65);
        utility += (18 + Math.max(1, toInteger(definition.character?.level, 1)) * 4) * (0.55 + duration * 0.45) * count * summonPower * (1 + gear.hp / 200) * (1 + gear.mana / 400) * (1 + gear.damage / 100) * crowding;
        tactics.support = true;
        didSomething = true;
      } else if (effect.type === "next-melee-attack") {
        const loadout = attackLoadout(actor);
        if (normalizeWeaponType(loadout.primary.type) !== "melee" || actor.effects.some(active => active.key === `${spell.name}:next-melee-attack`)) continue;
        const averageBonus = estimatedDamage(opponents(actor)[0] || actor, amount, effect.element || spell.element, false, true, actor, "melee");
        utility += averageBonus * 3 + toNumber(effect.stunChance, 0) * 0.25 + Math.abs(toNumber(effect.statDebuff?.amount, 0)) * 0.6 + healingAmount(actor, averageBonus * toNumber(effect.lifeSteal, 0), actor);
        didSomething = true;
      } else if (effect.type === "remove-random-debuff") {
        const recipients = forecastRecipients(actor, target, effect);
        for (const recipient of recipients) {
          const removable = recipient.effects.filter(isRemovableDebuff);
          if (!removable.length) continue;
          utility += (14 + removable.length * 5 + Number(removable.some(entry => entry.type === "stun")) * 16) / (effect.recipient === "random-ally" ? recipients.length : 1);
          didSomething = true; tactics.support = true;
        }
      } else if (effect.type === "instant-death-per-thousand") {
        for (const recipient of forecastRecipients(actor, target, effect)) {
          if (recipient.team === actor.team || debuffImmune(recipient)) continue;
          const probability = Math.min(1, Math.max(0, amount) / 1000);
          const available = Math.max(0, hp(recipient) - phaseDamageFloor(recipient));
          utility += available * probability * 3;
          tactics.damage += available * probability;
          tactics.killChance = Math.max(tactics.killChance, phaseDamageFloor(recipient) ? 0 : probability);
          didSomething ||= available > 0 && probability > 0;
        }
      } else if (trapEffects.has(effect.type)) {
        if (activeTrapCount(actor) + trapPlacementCount(effects) > trapCapacity(actor)) continue;
        utility += 9 + Math.min(18, Math.max(toNumber(effect.cap, 0), values(actor).maxHp * toNumber(effect.amount, 0) / 100));
        didSomething = true;
      } else {
        // Forecast recipients deterministically; never roll random-ally during
        // a preview. Buffs aimed at friendly targets/all allies must not be
        // mistaken for self buffs, and refreshed debuffs are not new value.
        const recipients = effect.recipient === "random-ally" ? aiAllies(actor) : recipientsFor(actor, target, effect).filter(Boolean);
        for (const recipient of recipients) {
          const value = timedEffectUtility(actor, recipient, { ...effect, recipient: "target" }, amount, spell.name) / (effect.recipient === "random-ally" ? Math.max(1, recipients.length) : 1);
          utility += value; didSomething ||= value > 0;
          tactics.support ||= value > 0 && recipient.team === actor.team;
          tactics.control ||= value > 0 && recipient.team !== actor.team;
          tactics.freshControl ||= value > 0 && recipient.team !== actor.team;
        }
      }
    }
    if (!didSomething) return null;
    const maximumMana = values(actor).maxMana;
    const scarcity = 0.35 + (1 - aiRatio(mana(actor), maximumMana)) * 0.75;
    const score = utility / Math.max(1, ap) - cost / Math.max(1, maximumMana) * 12 * scarcity;
    return { kind: "spell", spell, target, score, utility, cost, ap, apexChoice, weatherChoiceId, tactics };
  }

  function spellCandidates(actor, options = {}) {
    if (!actionAllowed(actor, "spell")) return [];
    const availableAp = options.availableAp ?? battle.ap;
    const candidates = [];
    for (const spell of spellsOf(actor)) {
      const originalEffects = spellEffects(spell), ap = forecastSpellAp(actor, spell);
      if (!effectsUsable(originalEffects) || ap > availableAp || !aiSummonAllowed(actor, originalEffects)) continue;
      const cost = forecastSpellCost(actor, spell);
      if (!options.ignoreMana && cost > mana(actor)) continue;
      if (originalEffects.some(effect => effect.type === "summon-friendly" && (!enemyDefinitions.some(enemy => enemy.id === effect.enemyId) || summonLimitReached(actor, effect) || effect.requiresDefeatedEnemy && !defeatedEnemyFor(actor)))) continue;
      const power = forecastSpellPower(actor, spell);
      for (const apexChoice of goldenApexEquipped(actor) ? ["focus", "sweep"] : [null]) {
        const effects = apexSpellEffects(originalEffects, apexChoice);
        const weather = effects.find(effect => effect.type === "weather-control");
        for (const weatherChoiceId of weather ? (weather.choices || []).map(choice => choice.id) : [null]) {
          for (const target of spellTargetOptions(actor, effects)) {
            const candidate = evaluateSpellForTarget(actor, spell, target, effects, ap, cost, power, apexChoice, weatherChoiceId);
            if (candidate) candidates.push(candidate);
          }
        }
      }
    }
    return candidates;
  }

  function withPreviewSkill(actor, skill, callback) {
    const states = skillStates(actor), key = classSkillKey(skill);
    const existed = Object.prototype.hasOwnProperty.call(states, key), previous = states[key];
    states[key] = { ...(previous || {}), active: true, remainingUses: skill.durationAttacks || skill.durationSpells || skill.durationSummonSpells || previous?.remainingUses };
    const recipients = skill.target === "all-allies" ? aiAllies(actor) : [actor];
    const effectSnapshots = recipients.map(recipient => [recipient, recipient.effects]);
    const timedTypes = new Set(["stat-percent", "all-stats-percent", "defense-flat", "defense-percent", "incoming-damage-percent", "incoming-attack-damage-percent", "spell-power-percent", "weapon-hit-flat", "weapon-hit-percent", "target-priority-flat", "outgoing-attack-damage-percent"]);
    for (const recipient of recipients) {
      recipient.effects = [...recipient.effects];
      applyTimedEffects(recipient, key, skill.name, (skill.onActivateEffects || []).filter(effect => timedTypes.has(effect.type)));
    }
    // A hypothetical skill changes the snapshot. Give it an independent cache
    // and restore the baseline cache along with the real skill state.
    try { return withCalculationFrame(callback); }
    finally {
      effectSnapshots.forEach(([recipient, effects]) => { recipient.effects = effects; });
      if (existed) states[key] = previous; else delete states[key];
    }
  }

  function bestCandidate(candidates) {
    return [...candidates].sort((left, right) => right.score - left.score || String(left.target?.id || left.spell?.id || "").localeCompare(String(right.target?.id || right.spell?.id || "")))[0] || null;
  }
  function bestTacticalFollowup(actor, candidates, pressure, availableAp = battle.ap) {
    return CombatAI.rank(candidates, { ...tacticalContext(actor, { pressure }), ap: availableAp })[0] || null;
  }

  function skillCandidate(actor, skill, baseline = null) {
    if (skill.mapOnly) return null;
    if (!actionAllowed(actor, "skill") || !skillReady(actor, skill)) return null;
    const key = classSkillKey(skill), loadout = attackLoadout(actor), foes = opponents(actor);
    if (!foes.length) return null;
    const skillActionPoints = Math.max(0, toInteger(skill.actionPoints, 0));
    const skillManaCost = Math.floor(values(actor).maxMana * Math.max(0, toNumber(skill.manaCostMaxPercent, 0)) / 100);
    if (skill.moneyCostSilver && !ShopRules.spend(actorCurrencies(actor), Math.max(0, toInteger(skill.moneyCostSilver, 0)))) return null;
    if (skill.rollUntilSuccess && (actor.ai?.history || []).filter(entry => entry.key === key && entry.turn === actor.turns).length >= 4) return null;
    if (battle.ap < skillActionPoints) return null;
    if (skill.manaCostMaxPercent && mana(actor) < Math.floor(values(actor).maxMana * skill.manaCostMaxPercent / 100)) return null;
    if (skill.durationAttacks && (battle.ap < loadout.cost || !weaponMatches(loadout.primary, skill.requiredWeaponType))) return null;

    const beforeAttack = baseline ? baseline.attack : bestCandidate(attackCandidates(actor));
    const beforeSpell = baseline ? baseline.spell : bestCandidate(spellCandidates(actor));
    let target = bestCandidate(foes.map(candidate => ({ target: candidate, score: combatantThreat(candidate, actor) + values(candidate).maxHp * 0.08 })))?.target || foes[0];
    if (skill.target === "ally") {
      const allies = aiAllies(actor).filter(candidate => candidate.id !== actor.id);
      if (!allies.length) return null;
      const protects = skill.onActivateEffects?.some(effect => effect.type === "guard-ally");
      target = [...allies].sort((left, right) => protects
        ? estimateIncomingPressure(right) / Math.max(1, hp(right)) - estimateIncomingPressure(left) / Math.max(1, hp(left))
        : aiRatio(hp(left), values(left).maxHp) - aiRatio(hp(right), values(right).maxHp))[0];
    }
    if (skill.requiredTargetRaceIds?.length) {
      target = foes.filter(foe => skill.requiredTargetRaceIds.map(slugify).includes(actorRace(foe))).sort((a, b) => targetPriority(actor, b) - targetPriority(actor, a))[0];
      if (!target) return null;
    }
    let score = 0, followupKey = null, followup = null, option = null, healing = 0, recipientHealth = 1, damage = 0, killChance = 0, manaRecovery = false;
    const plan = candidate => {
      followupKey = CombatAI.actionKey(candidate);
      followup = { key: followupKey, ap: candidate.tactics.ap, mana: candidate.tactics.mana || 0 };
    };
    const recordHealing = (recipient, gain) => {
      healing += gain;
      if (gain > 0) recipientHealth = Math.min(recipientHealth, aiRatio(hp(recipient), values(recipient).maxHp));
    };

    if (skill.consumeSorceryPoints) {
      const points = Math.max(0, toInteger(actor.sorceryPoints, 0));
      if (!points) return null;
      target = foes.sort((left, right) => hp(left) - hp(right))[0];
      const expected = estimatedDamage(target, points * Math.max(0, toNumber(skill.damagePerSorceryPoint, 5)), skill.element || "basic", false, true, actor, "spell");
      score += expected * 3 + Number(expected >= hp(target)) * 30;
      const resources = forecastResources(target), after = forecastDamageResources(target, expected, resources);
      damage += forecastResourceLoss(resources, after); killChance = Number(after.hp <= 0);
    }

    if (skill.rollUntilSuccess) score += 4 + values(actor).luck * 0.03;

    if (skill.durationAttacks) {
      const after = withPreviewSkill(actor, skill, () => bestCandidate(attackCandidates(actor)));
      if (!after) return null;
      target = after.target;
      plan(after);
      score += Math.max(0, after.outcome.effectiveDamage - toNumber(beforeAttack?.outcome?.effectiveDamage, 0)) * 5;
      score += Math.max(0, after.outcome.killProbability - toNumber(beforeAttack?.outcome?.killProbability, 0)) * 36;
      for (const effect of skill.onHitEffects || []) {
        if (effect.type === "lifesteal-percent") {
          const gain = healingDisabled(actor) ? 0 : Math.min(values(actor).maxHp - hp(actor), healingAmount(actor, after.outcome.onHitDamage * Math.max(0, toNumber(effect.amount, 0)) / 100, actor));
          score += gain * 2; recordHealing(actor, gain);
        }
        if (effect.type === "stun" || effect.type === "stun-chance") score += combatantThreat(target, actor) * (effect.type === "stun" ? 1 : rules.chance(effect.amount) / 100);
        if (effect.type === "target-all-stats-percent") score += Math.max(0, -toNumber(effect.amount, 0)) * Math.max(1, toInteger(effect.durationTurns, 1)) * 0.8;
        if (effect.type === "bonus-damage-max-mana-percent") score += estimatedDamage(target, values(actor).maxMana * Math.max(0, toNumber(effect.amount, 0)) / 100, effect.elements?.length ? effect.elements : effect.element || "physical", false, false, actor, "bonus") * 3;
        if (effect.type === "steal-equipped-weapon-on-crit") {
          const slots = weaponSlotsOf(target);
          if ([slots.primary, slots.offHand].some(weapon => weapon && itemCanDrop(weapon.id))) score += after.critChance / 100 * (8 + combatantThreat(target, actor));
        }
      }
      for (const effect of skill.afterAttackEffects || []) {
        if (effect.type === "stun") score -= (baseline?.pressure ?? estimateIncomingPressure(actor)) * Math.max(1, toInteger(effect.durationTurns, 1));
        if (effect.type === "incoming-weapon-hit-always") score -= (baseline?.pressure ?? estimateIncomingPressure(actor)) * Math.max(1, toInteger(effect.durationTurns, 1)) * 0.35;
        if (effect.type === "incoming-weapon-damage-percent" || effect.type === "incoming-attack-damage-percent") score -= (baseline?.pressure ?? estimateIncomingPressure(actor)) * Math.max(0, toNumber(effect.amount, 0)) / 100;
      }
    }

    if (skill.durationSpells || skill.durationSummonSpells) {
      const after = withPreviewSkill(actor, skill, () => bestTacticalFollowup(actor, spellCandidates(actor, { availableAp: battle.ap - skillActionPoints }), baseline?.pressure ?? estimateIncomingPressure(actor), battle.ap - skillActionPoints));
      if (!after) return null;
      target = after.target;
      plan(after);
      score += Math.max(0, after.score - toNumber(beforeSpell?.score, 0)) * 2.5 + Math.max(0, after.utility - toNumber(beforeSpell?.utility, 0)) * 0.35;
    }

    if (skill.durationTurns) {
      const basePressure = baseline?.pressure ?? estimateIncomingPressure(actor);
      const preview = withPreviewSkill(actor, skill, () => ({
        attack: bestCandidate(attackCandidates(actor)),
        spell: bestCandidate(spellCandidates(actor)),
        pressure: estimateIncomingPressure(actor),
        actorValues: values(actor)
      }));
      const duration = Math.min(4, Math.max(1, toInteger(skill.durationTurns, 1)));
      const offenseBefore = Math.max(toNumber(beforeAttack?.score, 0), toNumber(beforeSpell?.score, 0));
      const offenseAfter = Math.max(toNumber(preview.attack?.score, 0), toNumber(preview.spell?.score, 0));
      score += (offenseAfter - offenseBefore) * duration * 0.8;
      score += (basePressure - preview.pressure) * duration * 1.4;
      score += Math.max(0, preview.actorValues.speed - values(actor).speed) * 0.35;
    }

    for (const effect of skill.effects || []) {
      if (effect.type === "next-damage-unreducible" || effect.type === "next-damage-overload") {
        const upcoming = withPreviewSkill(actor, skill, () => bestTacticalFollowup(actor, [
          ...attackCandidates(actor), ...spellCandidates(actor, { availableAp: battle.ap - skillActionPoints })
        ].filter(candidate => candidate.tactics.ap <= battle.ap - skillActionPoints && candidate.tactics.damage > 0), baseline?.pressure ?? estimateIncomingPressure(actor), battle.ap - skillActionPoints));
        if (!upcoming) return null;
        target = upcoming.target || target;
        plan(upcoming);
        const before = Math.max(toNumber(beforeAttack?.score, 0), toNumber(beforeSpell?.score, 0));
        score += Math.max(0, upcoming.score - before) * 2;
        if (effect.type === "next-damage-unreducible") score += (baseline?.pressure ?? estimateIncomingPressure(actor)) * 0.5;
      }
      if (effect.type === "mark-target" || effect.type === "tactical-mark") {
        target = beforeAttack?.target || beforeSpell?.target || target;
        if (!target || target.team === actor.team) target = foes[0];
        const markedOutcome = beforeAttack?.target === target ? beforeAttack.outcome : estimatedAttack(actor, target);
        const expectedTurns = Math.min(4, Math.max(1, Math.ceil(hp(target) / Math.max(1, markedOutcome.expectedDamage))));
        score += markedOutcome.expectedDamage * Math.max(0, toNumber(effect.damagePercent, 0)) / 100 * expectedTurns * 3;
        score += (Math.max(0, toNumber(effect.hitFlat, 0)) + Math.max(0, toNumber(effect.hitPercent, 0)) / 2) * expectedTurns * 0.18;
      }
      if (effect.type === "read-intent" || effect.type === "predict-action") {
        const remembered = target.lastActionKind;
        option = ["attack", "spell", "skill", "meditate", "item"].includes(remembered) ? remembered : ["guard", "evade"].includes(remembered) ? "defend" : spellsOf(target).some(spell => spellEffects(spell).some(entry => entry.type === "damage")) ? "spell" : "attack";
        score += Math.min(estimateIncomingPressure(actor) * 0.25, values(actor).maxHp * 0.2);
      }
    }

    for (const effect of skill.onActivateEffects || []) {
      if (effect.type === "restore-full-resources") {
        const gain = healingDisabled(actor) ? 0 : healingAmount(actor, values(actor).maxHp - hp(actor), actor);
        score += gain * 2.4 + (values(actor).maxMana - mana(actor)) * 0.8;
        recordHealing(actor, gain); manaRecovery ||= mana(actor) < values(actor).maxMana;
      } else if (effect.type === "refresh-other-skills") {
        score += skillsOf(actor).filter(other => other !== skill && !skillReady(actor, other)).length * 7;
      } else if (effect.type === "consume-all-blood-supplies-stat-percent") {
        score += actorBloodSupplies(actor) * Math.max(0, toNumber(effect.amountPerStack, 0)) * 0.9;
      } else if (effect.type === "turn-start-restore-percent") {
        const missingHp = values(actor).maxHp - hp(actor), missingMana = values(actor).maxMana - mana(actor);
        score += Math.min(missingHp, healingAmount(actor, values(actor).maxHp * Math.max(0, toNumber(effect.hp, 0)) / 100, actor)) * 2;
        score += Math.min(missingMana, values(actor).maxMana * Math.max(0, toNumber(effect.mana, 0)) / 100) * 0.7;
      } else if (effect.type === "turn-start-max-hp-loss-percent") {
        score -= values(actor).maxHp * Math.max(0, toNumber(effect.amount, 0)) / 100 * 3;
      } else if (effect.type === "ki-restore-mana") {
        const maximum = values(actor).maxMana;
        const amount = Math.floor(maximum * Math.max(0, toNumber(effect.percent, 0)) / 100 + values(actor).mind * Math.max(0, toNumber(effect.mindRatio, 0)));
        score += Math.min(maximum - mana(actor), amount) * 0.8;
        manaRecovery ||= maximum > mana(actor) && amount > 0;
      } else if (effect.type === "restore-resource-percent") {
        const recipients = effect.recipient === "all-allies" || skill.target === "all-allies" ? aiAllies(actor) : [actor];
        for (const recipient of recipients) {
          const gain = healingDisabled(recipient) ? 0 : Math.min(values(recipient).maxHp - hp(recipient), healingAmount(recipient, values(recipient).maxHp * Math.max(0, toNumber(effect.hp, 0)) / 100, actor));
          score += gain * 2; healing += gain;
          if (gain) recipientHealth = Math.min(recipientHealth, aiRatio(hp(recipient), values(recipient).maxHp));
          score += Math.min(values(recipient).maxMana - mana(recipient), values(recipient).maxMana * Math.max(0, toNumber(effect.mana, 0)) / 100) * 0.8;
          manaRecovery ||= values(recipient).maxMana > mana(recipient) && effect.mana > 0;
        }
      } else if (effect.type === "random-animal-summon") {
        const animals = randomAnimalCandidates(actor, effect);
        score += animals.length ? Math.max(1, toInteger(effect.count, 1)) * 18 : -100;
      } else if (effect.type === "cast-random-spell") {
        score += spellsOf(actor).length ? 18 + Math.max(0, ...spellsOf(actor).map(spell => spell.tier)) * 3 : -100;
      } else if (effect.type === "weapon-attack-all-enemies") {
        const multiplier = Math.max(0, toNumber(effect.damageMultiplier, 1));
        for (const foe of foes) for (const entry of forecastRedirections(actor, foe, normalizeWeaponType(loadout.primary.type))) {
          const outcome = estimatedWeaponStrike(actor, entry.target, loadout.primary, focusedPowerMultiplier(actor) * multiplier);
          score += (outcome.effectiveDamage * 4 + outcome.killProbability * 34) * entry.probability;
          damage += outcome.effectiveDamage * entry.probability;
          killChance = Math.max(killChance, outcome.killProbability * entry.probability);
        }
      } else if (effect.type === "apply-skill-effects") {
        const power = skillEffectPower(actor, effect, skill.classId);
        for (const nested of effect.effects || []) {
          const amount = effectAmount(nested, power), recipients = forecastRecipients(actor, target, nested);
          const weight = nested.recipient === "random-ally" ? 1 / Math.max(1, recipients.length) : 1;
          for (const recipient of recipients) {
            if (nested.type === "damage") {
              const expected = estimatedDamage(recipient, amount, effect.elements?.length ? effect.elements : effect.element || "basic", false, true, actor, "spell", null, toNumber(nested.armorPiercePercent, 0));
              const resources = forecastResources(recipient), after = forecastDamageResources(recipient, expected, resources);
              const effective = forecastResourceLoss(resources, after), sign = recipient.team === actor.team ? -1 : 1;
              score += sign * (effective * 4 + Number(after.hp <= 0) * 34) * weight;
              if (sign > 0) { damage += effective * weight; killChance = Math.max(killChance, Number(after.hp <= 0) * weight); }
            } else if (nested.type === "heal" || nested.type === "restore-mana") {
              const restoreHp = nested.type === "heal", maximum = values(recipient)[restoreHp ? "maxHp" : "maxMana"];
              const restoration = rules.resourceRestore(amount, maximum, nested.maxPercent);
              const gain = Math.max(0, Math.min(maximum - (restoreHp ? hp(recipient) : mana(recipient)), restoreHp ? healingDisabled(recipient) ? 0 : healingAmount(recipient, restoration, actor) : restoration)) * weight;
              score += gain * (restoreHp ? 2.4 : 0.8) * (recipient.team === actor.team ? 1 : -1);
              if (recipient.team === actor.team) { if (restoreHp) recordHealing(recipient, gain); else manaRecovery ||= gain > 0; }
            } else if (["stun", "stun-chance"].includes(nested.type)) {
              if (!debuffImmune(recipient) && !withActor(recipient, () => calculateClassEffects().stunImmune) && !recipient.effects.some(active => active.type === "stun" && active.expiresTurn > recipient.turns)) {
                score += combatantThreat(recipient, actor) * (recipient.team === actor.team ? -1 : 1) * (nested.type === "stun" ? 1 : rules.chance(amount) / 100) * weight;
              }
            } else if (nested.type === "summon-friendly") {
              if (!summonLimitReached(actor, nested)) score += Math.max(1, toInteger(nested.count, 1)) * 18;
              break;
            } else score += timedEffectUtility(actor, recipient, { ...nested, recipient: "target" }, amount, skill.name) * weight;
          }
        }
      } else if (effect.type === "resource-exchange") {
        const maximum = values(actor);
        const manaGain = Math.min(maximum.maxMana - mana(actor), maximum.maxMana * Math.max(0, toNumber(effect.manaRestorePercent, 0)) / 100);
        const hpCost = Math.min(Math.max(0, hp(actor) - 1), maximum.maxHp * Math.max(0, toNumber(effect.hpCostPercent, 0)) / 100);
        if (mana(actor) > maximum.maxMana * 0.5 || manaGain < maximum.maxMana * 0.15 || hp(actor) - hpCost < maximum.maxHp * 0.35) return null;
        score += manaGain * 0.9 - hpCost * 0.75;
        manaRecovery ||= manaGain > 0;
      } else if (effect.type === "start-duel") {
        score += target ? combatantThreat(target, actor) * 1.5 + Math.max(0, aiAllies(actor).length - foes.length) * 4 : -100;
      } else if (effect.type === "guard-ally") {
        const alreadyGuarded = target?.effects.some(entry => entry.type === "guardian-ward" && entry.expiresTurn > target.turns && actorById(entry.guardianId) && hp(actorById(entry.guardianId)) > 0);
        const sustainable = hp(actor) > estimateIncomingPressure(actor) * 0.75;
        score += target && !alreadyGuarded && sustainable ? estimateIncomingPressure(target) * Math.max(1, toInteger(effect.durationTurns, skill.durationTurns || 1)) * 1.4 : -100;
      } else if (effect.type === "copy-enemy-spell") {
        target = foes.filter(foe => getSpell(foe.lastSpellId)).sort((a, b) => targetPriority(actor, b) - targetPriority(actor, a))[0];
        if (!target) return null;
        const copied = getSpell(target.lastSpellId), effects = spellEffects(copied);
        const spellTarget = effects.some(entry => entry.recipient === "friendly-target") ? actor : target;
        const power = withActor(actor, () => calculateSpellValue(copied)) * Math.max(0, toNumber(effect.powerPercent, 70)) / 100;
        score += evaluateSpellForTarget(actor, copied, spellTarget, effects, 1, 0, power)?.utility || 0;
      } else if (effect.type === "shift-turn-meters") {
        score += Math.max(0, toNumber(effect.allyAmount, 0)) / 100 * Math.max(5, beforeAttack?.score || beforeSpell?.score || 5) * 0.3;
        score += Math.max(0, -toNumber(effect.enemyAmount, 0)) / 100 * combatantThreat(target, actor) * 0.15;
      } else if (effect.type === "raise-standard") {
        score += aiAllies(actor).filter(ally => !ally.summoned).length * Math.max(0, toNumber(effect.damagePercent, 15)) * 2;
      } else if (effect.type === "gambit-strike" || effect.type === "execute-strike") {
        target = beforeAttack?.target || target;
        const multiplier = effect.type === "gambit-strike" ? 1 + Math.max(0, toNumber(effect.powerPercent, 0)) / 100 : hp(target) < values(target).maxHp * toNumber(effect.threshold, 35) / 100 ? 1 + toNumber(effect.damagePercent, 35) / 100 : 1;
        const outcome = estimatedWeaponStrike(actor, target, loadout.primary, multiplier);
        score += outcome.effectiveDamage * 3 + outcome.killProbability * 35;
        damage += outcome.effectiveDamage; killChance = Math.max(killChance, outcome.killProbability);
        if (effect.type === "gambit-strike") score -= (1 - outcome.hitChance / 100) * (beforeAttack?.score || 0) * battle.ap;
      } else if (effect.type === "mute-next-spell-percent") {
        const targetCaster = foes.filter(foe => spellsOf(foe).some(spell => spellEffects(spell).some(entry => entry.type === "damage")))
          .sort((a, b) => combatantThreat(b, actor) - combatantThreat(a, actor))[0];
        if (!targetCaster || targetCaster.effects.some(entry => entry.type === "next-spell-penalty")) return null;
        target = targetCaster;
        score += combatantThreat(target, actor) * Math.max(0, toNumber(effect.amount, 0)) / 100;
      } else if (effect.type === "poison-all-enemies") {
        score += foes.filter(foe => !debuffImmune(foe) && !foe.effects.some(entry => entry.key === `${key}:poison`)).reduce((sum, foe) => sum + Math.max(toNumber(effect.damage, 0), values(foe).maxHp * toNumber(effect.percentMaxHp, 0) / 100) * Math.max(1, toInteger(effect.durationTurns, 1)) * 2, 0);
      } else if (effect.type === "sunflare" || effect.type === "stormburst") {
        const minutes = (worldState.minutes % 1440 + 1440) % 1440;
        const power = effect.type === "sunflare" ? minutes >= 360 && minutes < 1080 ? effect.dayDamage : effect.nightDamage : Math.max(0, toInteger(actor.stormCharges, 0)) * toNumber(effect.damagePerCharge, 0);
        for (const foe of foes) {
          const resources = forecastResources(foe), after = forecastDamageResources(foe, estimatedDamage(foe, power, effect.type === "sunflare" ? "holy" : ["wind", "lightning"], false, true, actor, "spell"), resources);
          const effective = forecastResourceLoss(resources, after);
          score += effective * 3 + Number(after.hp <= 0) * 34;
          damage += effective; killChance = Math.max(killChance, Number(after.hp <= 0));
        }
      } else if (effect.type === "restore-summons-percent") {
        for (const ally of aiAllies(actor).filter(ally => ally.summoned)) score += Math.min(values(ally).maxHp - hp(ally), values(ally).maxHp * toNumber(effect.amount, 0) / 100) * 2 + Math.min(values(ally).maxMana - mana(ally), values(ally).maxMana * toNumber(effect.amount, 0) / 100) * 0.6;
      } else if (["buff-summons-defense-flat", "buff-summons-defense-percent"].includes(effect.type)) {
        for (const ally of aiAllies(actor).filter(ally => ally.summoned && (!effect.animalOnly || ["wolf", "bear", "griffon"].includes(actorRace(ally))))) score += timedEffectUtility(actor, ally, { ...effect, type: effect.type.endsWith("percent") ? "defense-percent" : "defense-flat", recipient: "target", target: effect.target || "armor" }, effect.amount, skill.name);
      } else if (effect.type === "blood-transfusion") {
        const gain = target && !healingDisabled(target) ? Math.min(values(target).maxHp - hp(target), healingAmount(target, values(target).maxHp * toNumber(effect.healPercent, 0) / 100, actor)) : 0;
        const paid = Math.min(hp(actor) - 1, values(actor).maxHp * toNumber(effect.hpCostPercent, 0) / 100);
        score += gain * 2.5 - paid * (hp(actor) < values(actor).maxHp * 0.4 ? 3 : 1);
        healing += gain; if (gain) recipientHealth = aiRatio(hp(target), values(target).maxHp);
      } else if (effect.type === "attune-last-hit-element") {
        if (!actor.lastDamageElement || !beforeSpell) return null;
        score += foes.reduce((sum, foe) => sum + Math.max(0, estimatedDamage(foe, forecastSpellPower(actor, beforeSpell.spell), actor.lastDamageElement, false, true, actor, "spell") - estimatedSpellDamage(actor, foe, beforeSpell.spell)), 0);
      } else {
        const recipients = skill.target === "all-allies" ? aiAllies(actor) : [actor];
        score += recipients.reduce((total, recipient) => total + timedEffectUtility(actor, recipient, { ...effect, recipient: "target" }, toNumber(effect.amount, 0), skill.name), 0);
      }
    }

    for (const effect of skill.onExpireEffects || []) {
      if (effect.type === "set-resources-flat") {
        score -= Math.max(0, hp(actor) - toNumber(effect.hp, 0)) * 0.9;
        score -= Math.max(0, mana(actor) - toNumber(effect.mana, 0)) * 0.25;
      }
    }

    const health = aiRatio(hp(actor), values(actor).maxHp);
    const pressure = baseline?.pressure ?? estimateIncomingPressure(actor);
    const danger = pressure / Math.max(1, values(actor).maxHp) + Math.max(0, foes.length - aiAllies(actor).length) * 0.08;
    if (key === "vampire:bloodfeed" && health < 0.75) score += (1 - health) * 18;
    if (key === "vampire-lord:blood-frenzy" && health > 0.65 && danger < 0.3) score -= 30;
    if (key === "sharmat:lucid-change" && health > 0.45 && aiRatio(mana(actor), values(actor).maxMana) > 0.35) score -= 35;
    if (key === "sharmat:lucid-change" && health > 0.65 && aiRatio(mana(actor), values(actor).maxMana) > 0.5) return null;
    if (key === "knight:all-or-nothing" && beforeAttack && hp(target) > beforeAttack.outcome.criticalDamage * 3 && health < 0.45) score -= 16;
    if (skill.cooldown === "daily") {
      const alliedStrength = aiAllies(actor).reduce((total, ally) => total + aiLevel(ally) * (0.45 + aiRatio(hp(ally), values(ally).maxHp) * 0.55), 0);
      const opposingStrength = foes.reduce((total, foe) => total + aiLevel(foe) * (0.45 + aiRatio(hp(foe), values(foe).maxHp) * 0.55), 0);
      score -= Math.max(3, 9 - danger * 12);
      if (health > 0.72 && alliedStrength > opposingStrength * 1.45) score -= 12;
    }
    if (skill.cooldown === "item") score -= 18;
    score /= Math.max(1, skillActionPoints);
    score -= skillManaCost * (aiRatio(mana(actor), values(actor).maxMana) < 0.35 ? 1.5 : 0.45);
    score -= skillActionPoints * 1.5;
    return score > 1 ? { kind: "skill", skill, key, target, option, score, tactics: { ap: skillActionPoints, mana: skillManaCost, healing, recipientHealth, damage, killChance, setup: !!followupKey, followupKey, followup, support: !!skill.durationTurns || healing > 0 || skill.target === "ally" || skill.target === "all-allies", hostileTarget: skill.target === "enemy", manaRecovery } } : null;
  }

  function skillCandidates(actor, baseline = null) {
    return skillsOf(actor).map(skill => skillCandidate(actor, skill, baseline)).filter(Boolean);
  }

  function estimateIncomingPressure(actor, defenseOverride = null) {
    const key = `incoming-pressure:${defenseOverride?.armor ?? "normal"}:${defenseOverride?.evasion ?? "normal"}`;
    return cacheCalculation(key, () => calculateIncomingPressure(actor, defenseOverride), actor);
  }
  function calculateIncomingPressure(actor, defenseOverride = null) {
    const actorValues = values(actor);
    const allies = aiAllies(actor);
    const actorWait = Math.max(0.1, (1000 - Math.min(999, actor.counter)) / Math.max(1, actorValues.speed));
    return opponents(actor).reduce((total, enemy) => {
      if (values(enemy).speed <= 0 && enemy.counter < 1000) return total;
      const weights = allies.map(ally => {
        const v = values(ally);
        return { id: ally.id, weight: (enemy.ai?.focusId === ally.id ? 3 : 1) * (0.55 + (1 - aiRatio(hp(ally), v.maxHp)) * 1.5) * (1 + Math.min(0.8, 100 / Math.max(100, v.armor))) };
      });
      const targetWeight = (weights.find(entry => entry.id === actor.id)?.weight || 1) / Math.max(1, weights.reduce((sum, entry) => sum + entry.weight, 0));
      const weapon = estimatedAttack(enemy, actor, defenseOverride);
      const attacks = Math.max(1, Math.floor(actionPointMaximum(enemy) / Math.max(1, weapon.cost)));
      let turnDamage = weapon.expectedDamage * attacks * targetWeight;
      for (const spell of spellsOf(enemy)) {
        const ap = forecastSpellAp(enemy, spell);
        const cost = forecastSpellCost(enemy, spell);
        if (ap > actionPointMaximum(enemy) || cost > mana(enemy) || !spellEffects(spell).some(effect => ["damage", "weapon-charge-all-enemies"].includes(effect.type))) continue;
        const casts = Math.max(1, Math.min(3, Math.floor(actionPointMaximum(enemy) / ap), cost > 0 ? Math.floor(mana(enemy) / cost) : actionPointMaximum(enemy)));
        const area = spellEffects(spell).some(effect => ["damage", "weapon-charge-all-enemies"].includes(effect.type) && (effect.recipient === "all-enemies" || effect.type === "weapon-charge-all-enemies"));
        turnDamage = Math.max(turnDamage, estimatedSpellDamage(enemy, actor, spell, defenseOverride) * casts * (area ? 1 : targetWeight));
      }
      if (enemy.effects.some(effect => effect.type === "stun" && effect.expiresTurn > enemy.turns)) turnDamage *= 0.2;
      const enemyWait = Math.max(0.1, (1000 - Math.min(999, enemy.counter)) / Math.max(1, values(enemy).speed));
      const tempo = Math.max(0.2, Math.min(1.25, actorWait / enemyWait));
      return total + turnDamage * tempo;
    }, 0);
  }

  function defenseCandidate(actor, baseline = null) {
    if (battle.ap < 2 || actor.effects.some(effect => effect.type === "stance")) return null;
    const canGuard = actionAllowed(actor, "guard"), canEvade = actionAllowed(actor, "evade");
    if (!canGuard && !canEvade) return null;
    const actorValues = values(actor), basePressure = baseline?.pressure ?? estimateIncomingPressure(actor);
    const choices = [];
    // Evaluate the real stance rules, including class/team improvements, flat
    // defenses and stance-specific protection. No AP is spent by the preview.
    for (const name of [canGuard && "Guard", canEvade && "Evade"].filter(Boolean)) {
      const pressure = cacheCalculation(`stance-pressure:${name}`, () => {
        const effects = actor.effects;
        actor.effects = [...effects, { type: "stance", name, expiresTurn: actor.turns + 1 }];
        try { return withCalculationFrame(() => estimateIncomingPressure(actor)); }
        finally { actor.effects = effects; }
      }, actor);
      choices.push({ name, pressure });
    }
    const choice = choices.sort((left, right) => left.pressure - right.pressure || left.name.localeCompare(right.name))[0];
    const reduction = basePressure - choice.pressure;
    const health = aiRatio(hp(actor), actorValues.maxHp), outnumbered = Math.max(0, opponents(actor).length - aiAllies(actor).length);
    const urgent = basePressure >= hp(actor) || health < 0.62 || outnumbered > 0 || basePressure > actorValues.maxHp * 0.22;
    if (!urgent || reduction < 0.45) return null;
    let score = reduction * 2 + (1 - health) * 12 + Number(basePressure >= hp(actor)) * 24;
    const finishingAttack = baseline ? baseline.attack : bestCandidate(attackCandidates(actor));
    if (battle.ap === 2 && finishingAttack?.outcome.killProbability > 0.5) score -= 24;
    return score > 1 ? { kind: "stance", stance: choice.name, score, pressure: choice.pressure, reduction, tactics: { ap: 2, mana: 0, damage: 0 } } : null;
  }

  function meditateCandidate(actor, baseline = null) {
    if (battle.ap < 4 || !actionAllowed(actor, "meditate") || !spellsOf(actor).length) return null;
    const actorValues = values(actor), gain = Math.min(actorValues.maxMana - mana(actor), rules.meditate(actorValues.maxMana, actorValues.mind, actorValues.luck));
    if (gain <= 0) return null;
    const potential = bestCandidate(spellCandidates(actor, { ignoreMana: true, availableAp: actionPointMaximum(actor) }));
    if (!potential || potential.cost <= mana(actor) || potential.cost > mana(actor) + gain) return null;
    const health = aiRatio(hp(actor), actorValues.maxHp), pressure = baseline?.pressure ?? estimateIncomingPressure(actor);
    if (health < 0.28 && pressure >= hp(actor)) return null;
    const score = 12 + gain / Math.max(1, actorValues.maxMana) * 30 + Math.max(0, potential.score) * 0.35 - pressure / Math.max(1, actorValues.maxHp) * 10;
    return score > 1 ? { kind: "meditate", score, gain, tactics: { ap: 4, mana: 0, manaRecovery: true } } : null;
  }

  function itemCandidates(actor) {
    if (battle.ap < 1 || !actionAllowed(actor, "item")) return [];
    const result = [];
    for (const item of consumablesOf(actor)) {
      if (item.archivedSpellId) {
        const spell = getSpell(item.archivedSpellId);
        if (!spell || !casterAllowed(actor, spell) || !actionAllowed(actor, "spell") || !effectsUsable(spellEffects(spell))) continue;
        for (const apexChoice of goldenApexEquipped(actor) ? ["focus", "sweep"] : [null]) {
          const effects = apexSpellEffects(spellEffects(spell), apexChoice);
          for (const target of spellTargetOptions(actor, effects)) {
            const candidate = evaluateSpellForTarget(actor, spell, target, effects, 1, 0, forecastSpellPower(actor, spell), apexChoice);
            if (candidate) result.push({ ...candidate, kind: "item", spell: null, item, score: candidate.score * 0.85 });
          }
        }
        continue;
      }
      if (!effectsUsable(item.effects || []) || trapCapacityReason(actor, item.effects)) continue;
      for (const target of spellTargetOptions(actor, item.effects || [])) {
        const candidate = evaluateSpellForTarget(actor, { id: item.id, element: item.element || "basic", effects: item.effects }, target, item.effects, 1, 0, 0);
        if (!candidate) continue;
        // Save scarce supplies when the restoration would largely be wasted.
        if (candidate.tactics.healing && candidate.tactics.recipientHealth > 0.8) continue;
        result.push({ ...candidate, kind: "item", spell: null, item, score: candidate.score * 0.85 });
      }
    }
    return result;
  }

  function tacticalContext(actor, baseline) {
    const v = values(actor), foes = opponents(actor), allies = aiAllies(actor);
    const damaging = spellsOf(actor).filter(spell => spellEffects(spell).some(effect => ["damage", "weapon-charge-all-enemies"].includes(effect.type)));
    const healing = spellsOf(actor).filter(spell => spellEffects(spell).some(effect => effect.type === "heal"));
    const levels = actor.id === "player" ? classLevels : actor.character.classLevels;
    const primary = attackLoadout(actor).primary;
    const martial = !primary.unarmed && !["magic-wand", "spell-staff"].includes(primary.type);
    const role = healing.length && (levels.priest || levels.lifewarden || healing.length > damaging.length) ? "support" : damaging.length && martial ? "battlemage" : damaging.length ? "caster" : levels.guardian || levels.bulwark ? "protector" : "fighter";
    const focusCounts = new Map();
    for (const ally of allies) if (ally.id !== actor.id && foes.some(foe => foe.id === ally.ai?.focusId)) focusCounts.set(ally.ai.focusId, (focusCounts.get(ally.ai.focusId) || 0) + 1);
    const teamFocus = [...focusCounts].sort((a, b) => b[1] - a[1])[0]?.[0] || null;
    return { actorId: actor.id, turn: actor.turns, phase: actor.phaseIndex || 0, ap: battle.ap, maxAp: actionPointMaximum(actor), hp: hp(actor), maxHp: v.maxHp, mana: mana(actor), maxMana: v.maxMana, pressure: baseline.pressure, role, enemyCount: foes.length, spellCount: spellsOf(actor).length, teamFocus, allyEmergency: allies.some(ally => hp(ally) < values(ally).maxHp * 0.35 && !healingDisabled(ally)), memory: actor.ai };
  }

  function aiCandidates(actor) {
    return withCalculationFrame(() => calculateAiCandidates(actor));
  }
  /* Boss AI profiles: hand-made priorities on top of the general tactics. A profile sees the
     scored candidates and returns either a fixed order to try ({ order }) or a narrowed
     list that is ranked as usual ({ candidates }); null leaves the choice untouched. */
  const isSummonSpell2 = spell => spellEffects(spell).some(effect => effect.type === "summon-friendly");
  const summonCountOf = spell => spellEffects(spell).filter(effect => effect.type === "summon-friendly").reduce((sum, effect) => sum + Math.max(1, toInteger(effect.count, 1)), 0);
  // `areaWeight` lets a damaging spell that hits the whole party count as several strikes.
  function oneStrikeATurn(actor, candidates, skipSkillIds, limit = 1, areaWeight = 1) {
    const skip = new Set(skillsOf(actor).filter(skill => skipSkillIds.includes(skill.id)).map(classSkillKey));
    const strikes = (kind, spell) => kind === "attack" ? 1 : kind !== "spell" || !spell ? 0 : !spellEffects(spell).some(effect => effect.type === "damage") ? 0 : spellEffects(spell).some(effect => effect.type === "damage" && effect.recipient === "all-enemies") ? areaWeight : 1;
    const struck = (actor.ai?.history || []).filter(entry => entry.turn === actor.turns).reduce((sum, entry) => sum + strikes(entry.kind, entry.kind === "spell" ? getSpell(entry.key) : null), 0);
    return { candidates: candidates.filter(candidate => candidate.kind !== "stance" && !skip.has(candidate.key) && !(strikes(candidate.kind, candidate.spell) && struck + strikes(candidate.kind, candidate.spell) > limit)) };
  }
  const AI_PROFILES = {
    // Great Imp Ji'ruan hides behind his Imps (Summoner's Overprotective) and fights with dark magic.
    jiruan(actor, candidates) {
      const imps = living().filter(entry => entry.summoned && entry.summonerId === actor.id && actorRace(entry) === "impid").length;
      const opening = actor.turns <= 1;
      // A whole retinue needs a turn to answer the call again: after one he waits a turn before
      // the next, and meanwhile can only raise a single Lesser Imp. That turn is the party's chance.
      // The opening retinue does not count: his first rebuild is always ready.
      const lastRetinue = Math.max(-Infinity, ...(actor.ai?.history || []).filter(entry => entry.kind === "spell" && entry.turn > 1 && summonCountOf(getSpell(entry.key) || { effects: [] }) >= 3).map(entry => entry.turn));
      const retinueReady = opening || actor.turns - lastRetinue >= 2;
      // Summon spells straight from his spell book, most Imps first; casting checks AP, Mana and room.
      const summons = spellsOf(actor).filter(spell => isSummonSpell2(spell) && (retinueReady || summonCountOf(spell) < 3)).sort((a, b) => summonCountOf(b) - summonCountOf(a))
        .map(spell => ({ kind: "spell", spell, target: actor, score: 1, tactics: { ap: forecastSpellAp(actor, spell) } }));
      const double = skillsOf(actor).find(skill => skill.id === "double-summon");
      const doubleKey = double ? classSkillKey(double) : null;
      // His first turn: as many Imps as possible, with Double Summon paying for the first retinue.
      if (opening) return { order: [...(doubleKey && !skillStates(actor)[doubleKey]?.active ? [{ kind: "skill", key: doubleKey, target: actor, score: 1 }] : []), ...summons] };
      // Later turns: rebuild the retinue once it has fallen. Until then a last Imp still shields
      // him, so the party's window is the moment between the last Imp's death and his turn.
      // Once badly wounded (below 40% HP) he no longer trusts a single Imp to cover him.
      if (!imps || imps === 1 && hp(actor) < values(actor).maxHp * 0.4) return { order: summons, then: true };
      // Otherwise dark magic and his rapier. While Imps shield him, Guard is wasted, and he keeps
      // his Mana for the retinue rather than pouring it into Mana Overload.
      const overload = skillsOf(actor).find(skill => skill.id === "mana-overload");
      const skip = new Set([doubleKey, overload ? classSkillKey(overload) : null].filter(Boolean));
      return { candidates: candidates.filter(candidate => !(candidate.kind === "spell" && isSummonSpell2(candidate.spell)) && candidate.kind !== "stance" && !skip.has(candidate.key)) };
    },
    // Juila the Grand Forest Matriarch: her seasons and their spirits are her summons, so she never
    // calls random animals; she casts rather than guards, and keeps Mana Overload's burst out of
    // the raid fight. Everything else (heals, curses, storms of thorns) follows the usual tactics.
    // A matriarch more than a striker: at most one damage spell a turn; the rest of her Action
    // Points go to healing and shielding her court. Quickened Spell's extra
    // opening cast is left out, as is Mana Overload's burst.
    juila(actor, candidates) {
      const plan = oneStrikeATurn(actor, candidates, ["call-of-the-ancient-pack", "mana-overload", "quickened-spell"]);
      // Wheel of the Year: once each season, as the first thing she does in it.
      const wheel = spellsOf(actor).find(spell => spell.seasonVariants), current = seasonState();
      // A Wheel that strikes (Summer's Blaze) is her one damaging action that turn.
      const wheelStruck = wheel && current?.season.id === "summer" && (actor.ai?.history || []).some(entry => entry.turn === actor.turns && entry.key === wheel.id);
      const damaging = candidate => candidate.kind === "attack" || candidate.kind === "spell" && spellEffects(candidate.spell).some(effect => effect.type === "damage");
      const others = plan.candidates.filter(candidate => !(candidate.kind === "spell" && candidate.spell?.seasonVariants) && !(wheelStruck && damaging(candidate)));
      if (!wheel || !current || current.owner.id !== actor.id || current.state.wheelIndex === current.state.index) return { candidates: others };
      const offensive = current.season.id !== "spring", target = offensive ? opponents(actor)[0] || actor : actor;
      return { order: [{ kind: "spell", spell: wheel, target, score: 1, tactics: { ap: forecastSpellAp(actor, wheel) } }], then: true, candidates: others };
    },
    // Nygheir: no Mana Overload burst, two damaging actions a turn. Before his wounds burst he crusts stone over
    // them when two are open, or one while he is below half health.
    nygheir(actor, candidates) {
      // At most two damaging actions a turn, and a spell that strikes the whole party is both of them;
      // the rest goes to sealing wounds or bracing. Risen as an earth caster, he strikes once a turn.
      const stances = candidates.filter(candidate => candidate.kind === "stance");
      const risen = toInteger(actor.phaseIndex, 0) >= 1;
      // The ground needs a turn to settle: no spell that strikes the whole party two turns running.
      const sweeping = spell => spellEffects(spell).some(effect => effect.type === "damage" && effect.recipient === "all-enemies");
      const shookLastTurn = (actor.ai?.history || []).some(entry => entry.turn === actor.turns - 1 && entry.kind === "spell" && sweeping(getSpell(entry.key) || { effects: [] }));
      const others = [...oneStrikeATurn(actor, candidates, ["mana-overload"], risen ? 1 : 2, risen ? 1 : 2).candidates, ...stances].filter(candidate => !(candidate.kind === "spell" && (spellEffects(candidate.spell).some(effect => effect.type === "seal-wound") || shookLastTurn && sweeping(candidate.spell))));
      const seal = spellsOf(actor).find(spell => spellEffects(spell).some(effect => effect.type === "seal-wound"));
      const open = toInteger(actor.openWounds, 0), sealedThisTurn = (actor.ai?.history || []).some(entry => entry.turn === actor.turns && entry.key === seal?.id);
      if (seal && !actor.woundsBurst && !sealedThisTurn && (open >= 2 || open >= 1 && hp(actor) < values(actor).maxHp * 0.5)) return { order: [{ kind: "spell", spell: seal, target: actor, score: 1, tactics: { ap: forecastSpellAp(actor, seal) } }], then: true, candidates: others };
      return { candidates: others };
    },
    // Her Grove Matriarchs keep the same discipline.
    "juila-court"(actor, candidates) { return oneStrikeATurn(actor, candidates, []); },
    // Tankina: at most two damaging actions a turn, a spell that strikes the whole party is both,
    // and never such a spell two turns running.
    tankina(actor, candidates) {
      const sweeping = spell => spellEffects(spell).some(effect => effect.type === "damage" && effect.recipient === "all-enemies");
      const sweptLastTurn = (actor.ai?.history || []).some(entry => entry.turn === actor.turns - 1 && entry.kind === "spell" && sweeping(getSpell(entry.key) || { effects: [] }));
      // She favors her lightning: every lightning spell darkens her storm.
      for (const candidate of candidates) if (candidate.kind === "spell" && candidate.spell?.element === "lightning") candidate.score *= 1.5;
      return { candidates: oneStrikeATurn(actor, candidates, ["mana-overload", "quickened-spell", "voltaic-overcharge"], 2, 2).candidates.filter(candidate => !(sweptLastTurn && candidate.kind === "spell" && sweeping(candidate.spell))) };
    },
    // Her Apostles pray to the storm on their second turn and every third turn after; they strike once a turn.
    "talos-apostle"(actor, candidates) {
      const call = spellsOf(actor).find(spell => spellEffects(spell).some(effect => effect.type === "storm-surge"));
      const others = oneStrikeATurn(actor, candidates, ["all-or-nothing", "perfect-form"], 1).candidates.filter(candidate => !(candidate.kind === "spell" && candidate.spell === call));
      const lastCall = Math.max(-Infinity, ...(actor.ai?.history || []).filter(entry => entry.kind === "spell" && entry.key === call?.id).map(entry => entry.turn));
      if (call && actor.turns >= 2 && actor.turns - lastCall >= 3) return { order: [{ kind: "spell", spell: call, target: actor, score: 1, tactics: { ap: forecastSpellAp(actor, call) } }], then: true, candidates: others };
      return { candidates: others };
    },
    // The Deep Sea King duels by his own rules: never the Gladiator's Duel; one measured strike a
    // turn against his champion, two when no duel stands.
    "deep-sea-king"(actor, candidates) { return oneStrikeATurn(actor, candidates, ["duel", "mana-overload"], royalDuelPartner(actor) ? 1 : 2); },
    // Ornn works with a smith's rhythm: no stance dancing and no all-or-nothing gambles, two
    // hammer blows a turn at most; his anvil work (reforging) happens at the end of his turns.
    ornn(actor, candidates) { return oneStrikeATurn(actor, candidates, ["mana-overload", "duel", "all-or-nothing", "perfect-form"], 2); },
    // Archdevil Saffron: no overloaded, quickened or random (Wild Thesis) spells; one damaging spell
    // a turn, and never the same sweeping spell two turns running.
    saffron(actor, candidates) {
      const sweeping = spell => spellEffects(spell).some(effect => effect.type === "damage" && effect.recipient === "all-enemies");
      const swept = (actor.ai?.history || []).filter(entry => entry.turn === actor.turns - 1 && entry.kind === "spell").map(entry => entry.key);
      return { candidates: oneStrikeATurn(actor, candidates, ["mana-overload", "quickened-spell", "magnum-opus", "wild-thesis"], 1, 1).candidates.filter(candidate => !(candidate.kind === "spell" && sweeping(candidate.spell) && swept.includes(candidate.spell.id))) };
    },
    // Ancient Lich Ool: no overloaded, quickened or random spells; two damaging spells a turn (a
    // sweeping one counts as both), never the same sweeping spell two turns running.
    ool(actor, candidates) {
      const sweeping = spell => spellEffects(spell).some(effect => effect.type === "damage" && effect.recipient === "all-enemies");
      const swept = (actor.ai?.history || []).filter(entry => entry.turn === actor.turns - 1 && entry.kind === "spell").map(entry => entry.key);
      return { candidates: oneStrikeATurn(actor, candidates, ["mana-overload", "quickened-spell", "magnum-opus", "wild-thesis"], 2, 2).candidates.filter(candidate => !(candidate.kind === "spell" && sweeping(candidate.spell) && swept.includes(candidate.spell.id))) };
    },
    // Anatemis, the White Star: claw and spell together, up to two blows a turn (a sweeping spell
    // counts as both), never a sweep two turns running; her grand spells (Tier 10) wait for her
    // second turn and then every third turn; she heals herself at most once a turn, and only when
    // hurt. No overloads, repeats or gambles: she needs none.
    anatemis(actor, candidates) {
      const sweeping = spell => spellEffects(spell).some(effect => effect.type === "damage" && effect.recipient === "all-enemies");
      const grand = spell => toInteger(spell?.tier, 0) >= 10 && spellEffects(spell).some(effect => effect.type === "damage");
      const healing = spell => spellEffects(spell).some(effect => effect.type === "heal");
      const history = actor.ai?.history || [];
      const sweptLastTurn = history.some(entry => entry.turn === actor.turns - 1 && entry.kind === "spell" && sweeping(getSpell(entry.key)));
      const lastGrand = Math.max(-Infinity, ...history.filter(entry => entry.kind === "spell" && grand(getSpell(entry.key))).map(entry => entry.turn));
      const grandReady = actor.turns >= 2 && actor.turns - lastGrand >= 3;
      const healedNow = history.some(entry => entry.turn === actor.turns && entry.kind === "spell" && healing(getSpell(entry.key)));
      const hurt = hp(actor) < values(actor).maxHp * 0.6;
      const skip = ["mana-overload", "quickened-spell", "magnum-opus", "wild-thesis", "arcane-echo", "arcane-chorus", "doubled-spell", "arcane-compression", "battle-cry", "sorcery-blast", "all-or-nothing", "perfect-form", "duel", "champions-challenge", "rampage"];
      return { candidates: oneStrikeATurn(actor, candidates, skip, 2, 2).candidates.filter(candidate => !(candidate.kind === "spell" && (
        sweeping(candidate.spell) && sweptLastTurn || grand(candidate.spell) && !grandReady || healing(candidate.spell) && (healedNow || !hurt)))) };
    },
    // Ur-Mat, the False Dreamer: the greatest sorcerer of his age needs no tricks of overload,
    // chance or repetition. One damaging spell a turn and never a sweeping spell two turns running;
    // his grand spells (Tier 10) wait for his second turn and then every third turn; he heals
    // himself at most once a turn, and only when hurt; he calls no new servant while one stands. Lucid Change is his phase (it wakes
    // him at the brink of death), never a heal he reaches for himself.
    "ur-mat"(actor, candidates) {
      const sweeping = spell => spellEffects(spell).some(effect => effect.type === "damage" && effect.recipient === "all-enemies");
      const grand = spell => toInteger(spell?.tier, 0) >= 10 && spellEffects(spell).some(effect => effect.type === "damage");
      const healing = spell => spellEffects(spell).some(effect => effect.type === "heal");
      const history = actor.ai?.history || [];
      const sweptLastTurn = history.some(entry => entry.turn === actor.turns - 1 && entry.kind === "spell" && sweeping(getSpell(entry.key)));
      const summoned = living().some(entry => entry.summoned && entry.summonerId === actor.id);
      const lastGrand = Math.max(-Infinity, ...history.filter(entry => entry.kind === "spell" && grand(getSpell(entry.key))).map(entry => entry.turn));
      const grandReady = actor.turns >= 2 && actor.turns - lastGrand >= 3;
      const healedNow = history.some(entry => entry.turn === actor.turns && entry.kind === "spell" && healing(getSpell(entry.key)));
      const hurt = hp(actor) < values(actor).maxHp * 0.6;
      const skip = ["mana-overload", "quickened-spell", "magnum-opus", "wild-thesis", "lucid-change", "arcane-echo", "arcane-chorus", "doubled-spell", "arcane-compression", "battle-cry", "sorcery-blast"];
      return { candidates: oneStrikeATurn(actor, candidates, skip, 1, 1).candidates.filter(candidate => !(candidate.kind === "spell" && (
        sweeping(candidate.spell) && sweptLastTurn || grand(candidate.spell) && !grandReady || healing(candidate.spell) && (healedNow || !hurt) || isSummonSpell2(candidate.spell) && summoned))) };
    },
    // His grave-knights guard before they kill: one strike a turn, no duels and no reckless blows.
    "grave-guard"(actor, candidates) { return oneStrikeATurn(actor, candidates, ["all-or-nothing", "rampage", "execute", "perfect-form", "mana-overload", "duel", "champions-challenge"], 1); },
    // His Servants fight as a court, not as assassins: one strike a turn, no executions or all-or-nothing gambles.
    "devil-servant"(actor, candidates) { return oneStrikeATurn(actor, candidates, ["execute", "all-or-nothing", "perfect-form", "mana-overload"], 1); },
    // Gaian swings his great hammer twice a turn at most, without all-or-nothing gambles.
    gaian(actor, candidates) { return oneStrikeATurn(actor, candidates, ["mana-overload", "duel", "all-or-nothing", "perfect-form"], 2); },
    // His Shieldbearer is a defender first: one strike a turn.
    "troll-guard"(actor, candidates) { return oneStrikeATurn(actor, candidates, ["mana-overload"], 1); },
    // Zik Lun commands more than he swings: one hammer blow a turn. He blows his war horn every
    // few of his turns (never on his first) while fewer than two called trolls stand.
    "zik-lun"(actor, candidates) {
      const horn = spellsOf(actor).find(spell => isSummonSpell2(spell));
      const others = oneStrikeATurn(actor, candidates, ["mana-overload"], 1).candidates.filter(candidate => !(candidate.kind === "spell" && isSummonSpell2(candidate.spell)));
      if (!horn) return { candidates: others };
      const every = Math.max(1, toInteger(warbandOf(actor)?.hornEveryTurns, 3));
      const lastHorn = Math.max(-Infinity, ...(actor.ai?.history || []).filter(entry => entry.kind === "spell" && entry.key === horn.id).map(entry => entry.turn));
      const ready = actor.turns >= 2 && actor.turns - lastHorn >= every;
      return ready ? { order: [{ kind: "spell", spell: horn, target: actor, score: 1, tactics: { ap: forecastSpellAp(actor, horn) } }], then: true, candidates: others } : { candidates: others };
    },
  };
  function aiProfileOf(actor) {
    if (actor.team !== "enemy") return null;
    return cacheCalculation("ai-profile", () => {
      if (actor.enemyDefinitionId) return enemyDefinitions.find(definition => definition.id === actor.enemyDefinitionId)?.aiProfile || null;
      // A lich's summoned guards fight by his phylactery's rules ("guardAiProfile").
      const config = actor.summoned ? phylacteryOf(actorById(actor.summonerId)) : null;
      return config?.guardAiProfile && config.guards.includes(actor.summonDefinitionId) ? config.guardAiProfile : null;
    }, actor);
  }
  function calculateAiCandidates(actor) {
    // Most skill evaluations compare against the same unbuffed attack/spell.
    // Reuse those costly forecasts for every skill and for the final choice.
    const attacks = attackCandidates(actor), spells = spellCandidates(actor);
    const pressure = estimateIncomingPressure(actor);
    const baseline = { attack: bestCandidate(attacks), spell: bestTacticalFollowup(actor, spells, pressure), pressure };
    const candidates = [
      ...skillCandidates(actor, baseline),
      ...spells,
      ...attacks,
      ...itemCandidates(actor),
      defenseCandidate(actor, baseline),
      meditateCandidate(actor, baseline)
    ].filter(candidate => candidate && Number.isFinite(candidate.score) && candidate.score > 0);
    for (const candidate of candidates) {
      if (!candidate.target || !candidate.tactics) continue;
      candidate.tactics.targetActingSoon = candidate.target.counter > 750;
      candidate.tactics.healerTarget = spellsOf(candidate.target).some(spell => spellEffects(spell).some(effect => effect.type === "heal"));
    }
    // On Charge! the whole warband hunts its prey.
    const prey = warbandPreyFor(actor);
    if (prey) for (const candidate of candidates) if (candidate.target === prey && candidate.tactics?.damage > 0) candidate.score *= 1.6;
    const profile = aiProfileOf(actor), plan = profile ? AI_PROFILES[profile](actor, candidates) : null;
    // A fixed order is tried first; "then" falls back to the usual ranking if none of it works.
    if (plan?.order) return plan.then ? [...plan.order, ...CombatAI.rank((plan.candidates || candidates).filter(candidate => !(candidate.kind === "spell" && isSummonSpell2(candidate.spell))), tacticalContext(actor, baseline))] : plan.order;
    return CombatAI.rank(plan?.candidates || candidates, tacticalContext(actor, baseline));
  }

  function runAiCandidate(actor, candidate) {
    const before = hostiles(actor).map(target => ({ target, hp: hp(target) }));
    const context = { turn: actor.turns, phase: actor.phaseIndex || 0 };
    let done = false;
    if (candidate.kind === "skill") done = useSkill(actor, candidate.key, candidate.target, candidate.option);
    if (candidate.kind === "spell") done = cast(actor, candidate.target, candidate.spell, candidate.weatherChoiceId || null, candidate.apexChoice ? { apexChoice: candidate.apexChoice } : {});
    if (candidate.kind === "attack") done = attack(actor, candidate.target);
    if (candidate.kind === "item") done = useItem(actor, candidate.target, candidate.item, candidate.apexChoice ? { apexChoice: candidate.apexChoice } : {});
    if (candidate.kind === "stance") done = stance(actor, candidate.stance);
    if (candidate.kind === "meditate") done = meditate(actor);
    if (done) actor.ai = CombatAI.remember(actor.ai, candidate, context, before.reduce((sum, entry) => sum + Math.max(0, entry.hp - hp(entry.target)), 0));
    return done;
  }

  function enemyAction() {
    if (!isActive() || !view || isControlledTurn() || hasPlayerEquipmentReaction()) return;
    if (window.BattleStage?.enabled() && window.BattleStage.busy()) { window.BattleStage.whenIdle(scheduleAI); return; }
    const actor = currentActor();
    if (!actor || hp(actor) <= 0) { endTurn(); return; }
    if (!opponents(actor).length) {
      // Either the battle is over, or everyone left is out of reach (for example
      // shielded by another fighter's duel). In that case the turn passes.
      checkOutcome();
      if (isActive() && battle.current === actor.id) { log(`${actorLabel(actor)} has no one within reach.`); endTurn(); }
      else refresh();
      return;
    }
    for (const candidate of aiCandidates(actor)) {
      if (!runAiCandidate(actor, candidate)) continue;
      refresh(); return;
    }
    endTurn();
  }

  function options(id, entries, empty) {
    const select = $(id), previous = select.value;
    select.innerHTML = entries.length ? entries.map(entry => `<option value="${escapeHtml(entry.id)}">${escapeHtml(entry.label)}</option>`).join("") : `<option value="">${escapeHtml(empty)}</option>`;
    if (entries.some(entry => entry.id === previous)) select.value = previous;
  }
  // Portrait medallions and a short identity line for the combat cards.
  const portraitCache = new Map();
  function portraitOf(actor) {
    if (!actor || !window.Sprites) return "";
    const look = stageLook(actor), key = actor.id + "|" + JSON.stringify(look);
    if (!portraitCache.has(key)) {
      if (portraitCache.size > 200) portraitCache.clear();
      portraitCache.set(key, window.Sprites.portrait(look));
    }
    return portraitCache.get(key);
  }
  function actorMeta(actor) {
    if (actor.summoned) return "Summoned ally";
    const raceId = actor.id === "player" ? selectedRaceId : actor.character?.raceId;
    const raceName = raceCatalog.find(entry => entry.id === raceId)?.name || "Adventurer";
    return `${raceName} · Level ${Math.max(1, toInteger(actor.id === "player" ? level : actor.character?.level, 1))}`;
  }
  function actorCard(actor) {
    const v = values(actor), active = battle.current === actor.id && isActive();
    if (actor.team !== player().team) {
      const health = Math.max(0, Math.min(1, hp(actor) / Math.max(1, v.maxHp)));
      const protectedBySummon = protectedByOwnedSummon(actor);
      const elementalClass = SPELL_ELEMENTS.includes(actor.element) ? ` has-elemental-accent spell-element-${actor.element}` : "";
      return `<button type="button" class="combatant combat-enemy${elementalClass}${actor.unique ? " is-unique" : ""}${actor.sparkling ? " is-sparkling" : ""} ${active ? "is-current" : ""} ${battle.targetId === actor.id ? "is-targeted" : ""} ${hp(actor) <= 0 ? "is-defeated" : ""} ${actor.phaseFlashUntil > Date.now() ? "is-transforming" : ""}" data-combat-target="${escapeHtml(actor.id)}" title="${escapeHtml(actor.name)}" aria-label="${protectedBySummon ? "Protected by summon: " : "Target "}${escapeHtml(actor.name)}" aria-pressed="${battle.targetId === actor.id}" ${!isActive() || hp(actor) <= 0 || protectedBySummon ? "disabled" : ""}><span class="combatant-portrait" aria-hidden="true">${portraitOf(actor)}</span><span class="combat-enemy-body"><span class="enemy-name">${escapeHtml(actor.name)}</span><span class="enemy-health-bar" role="img" aria-label="Health bar"><span style="transform:scaleX(${health})"></span></span></span></button>`;
    }
    const badges = [...new Set([...equipmentChargeBonuses(actor).map(charge => `${charge.item.name} · ${charge.stacks}/${charge.maximum} charges · +${charge.percent}% next damage`), ...actor.effects.map(effect => trapEffects.has(effect.type)
      ? `${effect.name} · Armed`
      : effect.remainingHits != null
      ? `${effect.name} · ${effect.remainingHits} hit(s)`
      : effect.season || effect.expiresTurn >= 1e6 ? effect.name
      : `${effect.name} · ${effect.expiresTurn - actor.turns} turn(s)`), ...skillsOf(actor).filter(skill => skillStates(actor)[classSkillKey(skill)]?.active).map(skill => skill.name), ...(actor.sorceryPoints ? [`Sorcery Points · ${actor.sorceryPoints}`] : []), ...(kiMaximum(actor) ? [`Ki · ${kiOf(actor)} / ${kiMaximum(actor)}`] : [])])];
    return `<article class="combatant combat-friendly-target ${active ? "is-current" : ""} ${battle.targetId === actor.id ? "is-targeted" : ""}" data-combat-target="${escapeHtml(actor.id)}" role="button" tabindex="0" aria-label="Target ${escapeHtml(actor.name)}" aria-pressed="${battle.targetId === actor.id}"><div class="combatant-heading"><span class="combatant-portrait" aria-hidden="true">${portraitOf(actor)}</span><div><h2>${escapeHtml(actor.name)}</h2><p class="combatant-meta">${escapeHtml(actorMeta(actor))}</p></div></div><div class="combat-resource"><span>HP <b>${hp(actor)} / ${v.maxHp}</b></span><progress value="${hp(actor)}" max="${Math.max(1, v.maxHp)}"></progress></div><div class="combat-resource mana"><span>Mana <b>${mana(actor)} / ${v.maxMana}</b></span><progress value="${mana(actor)}" max="${Math.max(1, v.maxMana)}"></progress></div><dl><div><dt>Armor</dt><dd>${v.armor}</dd></div><div><dt>Evasion</dt><dd>${v.evasion}</dd></div><div><dt>Speed</dt><dd>${v.speed}</dd></div></dl>${badges.length ? '<p class="combat-buffs" title="' + escapeHtml(badges.join(" · ")) + '">' + escapeHtml(badges.join(" · ")) + '</p>' : ""}</article>`;
  }
  function render() {
    return withCalculationFrame(renderBattle);
  }
  // What the animated battlefield needs to draw this moment of the battle.
  function stageStatuses(actor) {
    return actor.effects.filter(effect => !effect.hidden).slice(0, 8).map(effect => {
      const kind = effect.type === "stance" ? (effect.name === "Guard" ? "guard" : "evade")
        : effect.type === "stun" ? "stun" : trapEffects.has(effect.type) ? "trap"
        : /mark/.test(effect.type) || /mark/i.test(effect.name || "") ? "mark" : effect.seasonFoe || isDebuffDefinition(effect) ? "debuff" : "buff";
      const turns = effect.remainingHits != null ? `${effect.remainingHits} hit(s)` : trapEffects.has(effect.type) ? "armed" : Number.isFinite(effect.expiresTurn) && effect.expiresTurn < 1e6 ? `${Math.max(0, effect.expiresTurn - actor.turns)} turn(s)` : "";
      return { kind, label: [effect.name || effect.type, turns].filter(Boolean).join(" · ") };
    });
  }
  function stageLook(actor) {
    const definition = id => enemyDefinitions.find(enemy => enemy.id === id)?.name || null;
    if (actor.id === "player") return { raceId: selectedRaceId, appearance: typeof appearance !== "undefined" ? appearance : null, equipment: { ...equipment }, seed: "player" };
    const enemyName = actor.summoned ? definition(actor.summonDefinitionId) : actor.team === "enemy" ? definition(actor.enemyDefinitionId) : null;
    const look = { raceId: actor.character?.raceId || null, appearance: actor.character?.appearance || null, equipment: { ...(actor.character?.equipment || {}), natural: naturalWeaponKind(actor) }, enemyName, seed: actor.id };
    // The season's owner wears the season (Juila's foliage changes colour).
    const current = seasonState();
    if (current && current.owner.id === actor.id) look.season = current.season.id;
    // A boss's later phase may change its look (Nygheir's scars burn), and open old wounds show.
    if (actor.team === "enemy" && toInteger(actor.phaseIndex, 0) > 0) look.phase = toInteger(actor.phaseIndex, 0);
    if (oldWoundsOf(actor) && toInteger(actor.openWounds, 0) > 0) look.openWounds = toInteger(actor.openWounds, 0);
    const weather = stormState();
    if (weather && actor.team === weather.owner.team) look.storm = weather.stage.id;
    // An archdevil's hellfire grows with every sacrifice.
    const court = sacrificeState();
    if (court && actor.id === court.lord.id && court.state.stacks) look.infernal = Math.min(3, court.state.stacks);
    // The star's scales gleam while her mirror holds; her heart-star swells as a supernova gathers.
    const stellar = starState();
    if (stellar && actor.id === stellar.star.id) { look.mirror = stellar.state.mirrorUp; if (stellar.state.novaStart != null) look.nova = true; }
    // The dreamer wears the law he has written; his eyes open once he wakes (Lucid Change).
    const dream = dreamState();
    if (dream && actor.id === dream.dreamer.id) look.law = dream.laws[0].id;
    // A lich's soul-fire dims once his phylactery is broken; the vessel shows its cracks.
    const soul = phylacteryState();
    if (soul && actor.id === soul.lich.id && soul.state.broken) look.soulDimmed = true;
    if (soul && actor.id === soul.vessel.id) look.cracks = Math.min(3, Math.floor((1 - hp(actor) / Math.max(1, values(actor).maxHp)) * 4));
    // A severed avatar's veins go dark.
    const land = heartCoreState();
    if (land && actor.id === land.avatar.id && land.severed) look.severed = true;
    // A smith shows his temper, and the glow of metal on the anvil while he works.
    const forge = reforgeState();
    if (forge && actor.id === forge.smith.id) { if (forge.current) look.temper = forge.current.id; if (forge.working) look.reforging = true; }
    return look;
  }
  function stageSnapshot(turnOrder) {
    const allyTeam = player().team;
    return {
      active: isActive(), result: battle.result, current: battle.current, targetId: battle.targetId, log: battle.log,
      region: worldState.regionId, zone: WorldRules.zoneAt(worldState.position, worldState.regionId), minutes: worldState.minutes,
      turnOrder, order: (() => {
        const stellar = starState();
        if (stellar) {
          const { state, star, config } = stellar, marked = state.sunfallTarget ? actorById(state.sunfallTarget) : null;
          const mirror = !config.mirror ? "" : state.mirrorUp ? ` Her Mirror Scales reflect ${config.mirror.reflectPercent}% of spell damage until weapon blows deal her ${starShare(stellar, config.mirror.breakPercent)} (${state.mirrorDamage} so far).` : " Her Mirror Scales are shattered.";
          const sunfall = marked && hp(marked) > 0 ? ` Sunfall falls on ${marked.name} at her next turn unless she takes ${starShare(stellar, config.sunfall.deflectPercent)} damage (${state.sunfallDamage} so far).` : "";
          if (state.novaStart != null) return { id: "supernova", name: "Supernova", description: `It strikes every hero for ${config.supernova.damagePercent}% of their HP unless she takes ${starShare(stellar, config.supernova.collapsePercent)} damage first (${state.novaDamage} so far).${sunfall}${mirror}`, turnsLeft: Math.max(1, config.supernova.chargeTurns - (star.turns - state.novaStart)), prey: marked && hp(marked) > 0 ? marked.name : null, preyLabel: "Sunfall on" };
          if (marked && hp(marked) > 0) return { id: "sunfall", name: "Sunfall", description: `${sunfall.trim()}${mirror}`, prey: marked.name, preyLabel: "Marked:" };
          if (config.mirror) return state.mirrorUp ? { id: "mirror", name: "Mirror Scales", description: mirror.trim() } : { id: "mirror-shattered", name: "Mirror shattered", description: "Spells reach her unreflected until her scales reform.", turnsLeft: Math.max(1, config.mirror.reformTurns - (star.turns - state.mirrorBrokenAt)) };
        }
        const soul = phylacteryState();
        if (soul?.state.broken) return { id: "mortal", name: "Mortal", description: `${soul.lich.name}'s phylactery is broken: he can die now, and his spells are weaker.` };
        if (soul && soul.state.dustSince != null) return { id: "dust", name: `${soul.lich.name} is dust`, description: `While his phylactery stands, ${soul.lich.name} re-forms with ${soul.config.reformHpPercent}% of his health. Break it before he does, and he stays dust.`, turnsLeft: Math.max(1, soul.config.reformRounds - (soul.vessel.turns - soul.state.dustSince)), unit: "round" };
        if (soul) {
          const guards = phylacteryGuards(soul).length;
          return { id: "phylactery", name: "Phylactery", description: `While his phylactery stands, ${soul.lich.name} cannot truly die: at 0 HP he crumbles to dust and re-forms ${soul.config.reformRounds} rounds later. ${guards ? "His Legionnaires take every single-target blow meant for it; area attacks still reach it." : "No Legionnaire guards it now."}`, prey: guards ? `${guards} Legionnaire${guards === 1 ? "" : "s"}` : null, preyLabel: "Guarded by" };
        }
        const court = sacrificeState();
        if (court) {
          const pending = court.servants.filter(servant => court.state.consumed[servant.id] != null).sort((left, right) => court.state.consumed[left.id] - court.state.consumed[right.id]);
          if (pending.length) return { id: "sacrifice", name: court.state.stacks ? `Infernal Power ${court.state.stacks}` : "Infernal Sacrifice", description: `${court.lord.name} feeds on his Servants when they fall below ${court.config.belowPercent}% HP: each sacrifice heals him and adds ${court.config.powerPercent}% spell power. A hell-rift returns a sacrificed Servant ${court.config.returnTurns} of his turns later; Servants you kill outright stay dead.`, prey: servantName(pending[0]), preyLabel: "Rift returns", turnsLeft: Math.max(1, court.state.consumed[pending[0].id] + court.config.returnTurns - court.lord.turns) };
          if (court.state.stacks) return { id: "infernal", name: `Infernal Power ${court.state.stacks}`, description: `Each sacrifice adds ${court.config.powerPercent}% to ${court.lord.name}'s spell power.` };
        }
        const land = heartCoreState();
        if (land?.severed) return { id: "severed", name: "Severed", description: `${land.avatar.name} is cut off from the land: he takes ${land.config.severed.damageTakenPercent}% more damage and has fewer AP until his Cores regrow.`, turnsLeft: Math.max(1, land.state.severedUntil - land.avatar.turns + (currentActor()?.id === land.avatar.id ? 1 : 0)) };
        const regrowing = land ? land.cores.filter(core => hp(core) <= 0 && land.state.fallen[core.id] != null) : [];
        if (regrowing.length) return { id: "regrowing", name: "Core regrowing", description: `A fallen Core regrows after ${land.config.regrowTurns} of ${land.avatar.name}'s turns. Bring every Core down at once to sever him from the land.`, turnsLeft: Math.max(1, Math.min(...regrowing.map(core => land.state.fallen[core.id] + land.config.regrowTurns - land.avatar.turns))) };
        const forge = reforgeState();
        if (forge?.working) return { id: "reforge", name: `Reforging · ${forge.working.name}`, description: `${forge.working.description || ""} Deal ${forge.smith.name} ${forge.config.spoilPercent}% of his health before his next turn to crack the metal.`.trim(), prey: `${Math.min(100, Math.floor(100 * forge.state.damage / Math.max(1, forge.state.threshold)))}%`, preyLabel: "Cracked" };
        if (forge?.current) return { id: "temper", name: forge.current.name, description: forge.current.description || "" };
        const duel = royalDuelState(), champion = duel?.state.championId ? actorById(duel.state.championId) : null;
        if (champion && hp(champion) > 0) return { id: "duel", name: "Royal Duel", description: `${duel.king.name} strikes only his champion; other blows against him land at reduced strength.`, prey: champion.name, preyLabel: "Champion", turnsLeft: Math.max(0, duel.state.endsTurn - duel.king.turns) };
        const current = warbandState(), order = current?.state.order ? current.config.orders[current.state.order] : null;
        if (!order) return null;
        const prey = actorById(current.state.preyId);
        return { id: current.state.order, name: order.name, description: order.description || "", prey: prey && hp(prey) > 0 ? prey.name : null, drums: current.state.scale > 1 };
      })(), season: (() => { const dream = dreamState(); if (dream) return { id: dream.laws[0].id, name: lawNames(dream.laws), description: dream.laws.map(law => `${law.name}: ${law.description || ""}`).join(" "), next: lawNames(dream.next), foretold: dream.state.foretold, dream: true }; const weather = stormState(); if (weather) return { id: weather.stage.id, name: weather.stage.name, description: weather.stage.description || "", next: weather.next?.name || "", foretold: false, storm: true }; const current = seasonState(); return current ? { id: current.season.id, name: current.season.name, description: current.season.description || "", next: current.next.name, foretold: current.state.foretold === current.next.id } : null; })(),
      actors: battle.actors.filter(actor => !actor.summoned || hp(actor) > 0).map(actor => {
        const v = values(actor), alive = hp(actor) > 0, ally = actor.team === allyTeam;
        const guarded = !ally && protectedByOwnedSummon(actor);
        return {
          id: actor.id, name: actor.name, label: actor.name, team: actor.team, ally, alive, summoned: Boolean(actor.summoned), companion: Boolean(actor.companion), unique: Boolean(actor.unique), sparkling: Boolean(actor.sparkling),
          hp: hp(actor), maxHp: v.maxHp, mana: mana(actor), maxMana: v.maxMana,
          current: battle.current === actor.id && isActive(), targeted: battle.targetId === actor.id, targetable: isActive() && alive && !guarded, protected: guarded,
          stance: actor.effects.find(effect => effect.type === "stance")?.name || null, statuses: stageStatuses(actor), transforming: actor.phaseFlashUntil > Date.now(),
          look: stageLook(actor),
        };
      }),
    };
  }
  function renderBattle() {
    $("combat-view-toggle").disabled = !isActive();
    document.querySelectorAll(".combat-view-toggle").forEach(button => button.classList.toggle("is-active", isActive()));
    $("combat-open-button").disabled = isActive();
    if (!battle) { reactionModal.hidden = true; window.BattleStage?.sync(null); return; }
    $("combat-outcome").textContent = isActive() ? "Combat active" : battle.result;
    const enemyTargets = opponents(player());
    const selectableTargets = living();
    if (!selectableTargets.some(actor => actor.id === battle.targetId)) battle.targetId = enemyTargets[0]?.id || player()?.id || null;
    const visibleRoster = battle.actors.filter(actor => !actor.summoned || hp(actor) > 0);
    $("combat-player").innerHTML = visibleRoster.filter(actor => actor.team === player().team).map(actorCard).join("");
    $("combat-opponents").innerHTML = visibleRoster.filter(actor => actor.team !== player().team).map(actorCard).join("");
    const forecast = predict();
    $("combat-turn-order").innerHTML = forecast.map((actor, index) => { const entry = actorById(actor.id); return `<li class="${actor.id === "player" ? "is-player" : ""} ${entry?.team === player().team ? "team-ally" : "team-enemy"}"><span class="turn-chip-portrait" aria-hidden="true">${portraitOf(entry)}</span><small>${index + 1}</small>${escapeHtml(actorLabel(entry))}</li>`; }).join("");
    const actingMaximumAp = currentActor() ? actionPointMaximum(currentActor()) : 5;
    $("combat-turn-status").textContent = !isActive() ? battle.result : battle.current ? `${battle.current === "player" ? "Your" : actorLabel(currentActor()) + "'s"} turn · ${battle.ap} / ${actingMaximumAp} AP${!view ? " · Paused on character sheet" : ""}` : "No participant has positive Speed. Change Speed on the character sheet to resume.";
    const actor = isControlledTurn() ? currentActor() : player();
    const loadout = attackLoadout(actor);
    $("combat-ap").textContent = `${isActive() ? battle.ap : 0} / ${actingMaximumAp}`;
    $("combat-equipped-weapon").textContent = loadout.offHand ? `${loadout.primary.name} + ${loadout.offHand.name} (off-hand)` : loadout.primary.name;
    $("combat-spell-button").disabled = !isControlledTurn() || !actionAllowed(actor, "spell");
    $("combat-item-button").disabled = !isControlledTurn() || !actionAllowed(actor, "item");
    const actions = [
      ["attack", "Attack", loadout.cost, opponents(actor).includes(selectedTarget())],
      ["guard", "Guard", 2, true], ["evade", "Evade", 2, true], ["meditate", "Meditate", 4, true], ["run", "Run", 1, true], ["end", "End turn", 0, true]
    ];
    const actionIcons = { attack: "⚔", guard: "◈", evade: "≈", meditate: "✺", run: "↶", end: "»" };
    $("combat-actions").innerHTML = actions.map(([id, label, cost, available]) => `<button type="button" data-action="${id}" ${!isControlledTurn() || battle.ap < cost || !available || !actionAllowed(actor, id) ? "disabled" : ""}><span class="combat-action-icon" aria-hidden="true">${actionIcons[id]}</span>${label}<small>${id === "end" ? "Finish acting" : cost + " AP"}</small></button>`).join("");
    const priorGuess = $("combat-guess")?.value || "attack";
    $("combat-skills").innerHTML = `<p class="eyebrow">SKILLS</p>` + (skillsOf(actor).filter(skill => !skill.mapOnly).map((skill, index) => {
      const key = classSkillKey(skill), state = skillStates(actor)[key];
      const unavailable = !skillReady(actor, skill);
      const targetUnavailable = skill.target === "enemy"
        ? !opponents(actor).includes(selectedTarget())
        : skill.target === "ally"
        ? !selectedTarget() || selectedTarget().team !== actor.team || selectedTarget().id === actor.id || hp(selectedTarget()) <= 0
        : false;
      const skillAp = Math.max(0, toInteger(skill.actionPoints, 0));
      const description = skill.description || "No description provided.";
      const tooltipId = `combat-skill-description-${index}`;
      const targetPrompt = skill.target === "ally" ? "Choose ally" : "Choose enemy";
      return `<span class="combat-skill-wrap" tabindex="0"><button type="button" aria-describedby="${tooltipId}" data-skill="${escapeHtml(key)}" class="${state?.active ? "is-active" : ""}" ${!isControlledTurn() || battle.ap < skillAp || !actionAllowed(actor, "skill") || state?.active || unavailable || targetUnavailable ? "disabled" : ""}>${escapeHtml(skill.name)} · ${state?.active ? "Active" : targetUnavailable ? targetPrompt : unavailable ? "Unavailable" : `Use · ${skillAp} AP${skill.kiCost ? ` · ${skill.kiCost} Ki` : ""}`}</button><span class="combat-skill-tooltip" id="${tooltipId}" role="tooltip"><strong>${escapeHtml(skill.name)}</strong>${escapeHtml(description)}</span></span>`;
    }).join("") || "<span>No skills unlocked.</span>") + (skillsOf(actor).some(skill => (skill.effects || []).some(effect => effect.type === "predict-action")) ? `<label class="combat-guess-label">Foretell enemy action <select id="combat-guess"><option value="attack">Attack</option><option value="spell">Spell</option><option value="skill">Skill</option><option value="defend">Guard or Evade</option><option value="meditate">Meditate</option><option value="item">Item</option></select></label>` : "");
    if ($("combat-guess")) $("combat-guess").value = priorGuess;
    $("combat-log").innerHTML = battle.log.map(entry => `<li class="${window.BattleStage?.logKind(entry) || ""}">${escapeHtml(entry)}</li>`).join("");
    $("combat-log").scrollTop = $("combat-log").scrollHeight;
    if (!abilityPicker.hidden) {
      if (!isControlledTurn()) closePicker();
      else renderPicker();
    }
    renderEquipmentReaction();
    window.BattleStage?.sync(stageSnapshot(forecast));
  }

  function closePicker() {
    abilityPicker.hidden = true;
    updateModalState();
    $(pickerKind === "item" ? "combat-item-button" : "combat-spell-button").focus();
    weatherSpellId = null;
    pendingApex = null;
  }
  function openPicker(kind) {
    if (!isControlledTurn() || hasPlayerEquipmentReaction()) return;
    pendingLament = null;
    pickerKind = kind;
    pendingApex = null;
    abilityPicker.hidden = false;
    renderPicker(); updateModalState();
    abilityPicker.querySelector(".modal-close").focus();
  }
  function beginApexChoice(kind, id, weatherChoiceId = null) {
    if (!isControlledTurn() || !goldenApexEquipped(currentActor())) return false;
    pendingApex = { kind, id, weatherChoiceId };
    pickerKind = "apex";
    abilityPicker.hidden = false;
    renderPicker(); updateModalState();
    abilityPicker.querySelector("[data-apex-choice]")?.focus();
    return true;
  }
  function renderPicker() {
    const actor = isControlledTurn() ? currentActor() : player(), isSpell = pickerKind === "spell" || pickerKind === "weather";
    if (pickerKind === "apex") {
      const spell = pendingApex?.kind === "item" ? getSpell(getItem(pendingApex.id)?.archivedSpellId) : getSpell(pendingApex?.id);
      $("combat-ability-title").textContent = "Golden Apex · Choose a form";
      $("combat-picker-resources").textContent = `${spell?.name || "Spell"} · ${battle.ap} AP remaining · ${mana(actor)} Mana`;
      $("combat-ability-list").innerHTML = `<article class="combat-ability-entry"><div><strong>Starlit Sweep</strong><p>Hostile effects target all enemies. Spell damage +35%.</p></div><button type="button" data-apex-choice="sweep">Choose</button></article><article class="combat-ability-entry"><div><strong>Unbound Zenith</strong><p>Spell damage +100%. Damage bypasses Armor, resistance, and other reductions.</p></div><button type="button" data-apex-choice="focus">Choose</button></article>`;
      return;
    }
    if (pickerKind === "weather") {
      const spell = getSpell(weatherSpellId), weather = spellEffects(spell).find(effect => effect.type === "weather-control");
      $("combat-ability-title").textContent = "Choose the weather";
      $("combat-picker-resources").textContent = `${battle.ap} AP remaining · ${mana(actor)} Mana · ${spell?.name || "Control Weather"}`;
      $("combat-ability-list").innerHTML = (weather?.choices || []).map(choice => `<article class="combat-ability-entry"><div><strong>${escapeHtml(choice.name)}</strong><p>${escapeHtml(choice.effects?.length ? choice.effects.map(effect => `${effect.type.replace(/-/g, " ")} ${toNumber(effect.amount, 0) >= 0 ? "+" : ""}${toNumber(effect.amount, 0)}%`).join(" · ") : "No combat modifiers." )}</p></div><button type="button" data-combat-weather="${escapeHtml(choice.id)}">Choose</button></article>`).join("");
      return;
    }
    $("combat-ability-title").textContent = isSpell ? "Choose a spell" : "Use an item";
    const trapStatus = isSpell ? "" : ` · Traps ${activeTrapCount(actor)}/${trapCapacity(actor)}`;
    $("combat-picker-resources").textContent = `${battle.ap} AP remaining · ${mana(actor)} Mana${trapStatus} · ${selectedTarget() ? actorLabel(selectedTarget()) + " targeted" : "No target"}`;
    const entries = isSpell ? spellsOf(actor) : consumablesOf(actor);
    $("combat-ability-list").innerHTML = entries.length ? entries.map(entry => {
      const effects = isSpell ? spellEffects(entry) : entry.archivedSpellId ? spellEffects(getSpell(entry.archivedSpellId) || {}) : entry.effects || [];
      const ap = isSpell ? withActor(actor, () => calculateSpellActionPoints(entry)) : 1;
      const cost = isSpell ? withActor(actor, () => calculateSpellManaCost(entry)) : 0;
      const needsEnemy = requiresEnemyTarget(effects);
      const needsFriendly = effects.some(effect => effect.recipient === "friendly-target");
      const summonBlocked = effects.some(effect => effect.type === "summon-friendly" && (summonLimitReached(actor, effect) || effect.requiresDefeatedEnemy && !defeatedEnemyFor(actor)));
      const trapBlocked = !isSpell ? trapCapacityReason(actor, effects) : "";
      const target = selectedTarget();
      const reason = !effectsUsable(effects) ? "No usable effect" : needsEnemy && !opponents(actor).includes(target) ? "Choose an enemy" : needsFriendly && (!target || target.team !== actor.team) ? "Choose yourself or an ally" : summonBlocked ? effects.some(effect => effect.requiresDefeatedEnemy) && !defeatedEnemyFor(actor) ? "No defeated enemy" : "Summon already active" : trapBlocked || (battle.ap < ap ? "Not enough AP" : mana(actor) < cost ? "Not enough Mana" : "");
      const description = isSpell ? withActor(actor, () => spellDescription(entry)) : entry.description;
      const owned = actor.id === "player" ? inventory : actor.character.inventory;
      const meta = isSpell ? `${cost} Mana · ${ap} AP` : `×${owned.filter(id => id === entry.id).length} owned · 1 AP`;
      // Lament: cast it on a second target too, for 50% more Mana (M19).
      const lament = isSpell && pickerKind === "spell" && lamentEligible(effects) ? lamentEffect(actor) : null;
      const sideCount = lament ? (needsEnemy ? opponents(actor).length : living().filter(ally => ally.team === actor.team).length) : 0;
      const lamentReason = !lament ? "" : reason || (sideCount < 2 ? "No second target" : mana(actor) < Math.floor(cost * (1 + toNumber(lament.amount, 0) / 100)) ? "Not enough Mana" : "");
      const lamentButton = lament ? `<button type="button" class="combat-lament-button" data-combat-lament="${escapeHtml(entry.id)}" ${lamentReason ? "disabled" : ""}>${escapeHtml(lamentReason || `Lament: +1 target (+${formatValue(lament.amount)}% Mana)`)}</button>` : "";
      return `<article class="combat-ability-entry"><div><strong>${escapeHtml(entry.name)}</strong><small>${meta}</small><p>${escapeHtml(description || "")}</p></div><button type="button" data-combat-ability="${escapeHtml(entry.id)}" ${reason ? "disabled" : ""}>${escapeHtml(reason || (isSpell ? "Cast" : "Use"))}</button>${lamentButton}</article>`;
    }).join("") : `<p class="combat-picker-empty">${isSpell ? "No spells learned yet." : "No usable items in your inventory."}</p>`;
  }

  async function fetchJson(path) {
    const response = await fetch(path, { cache: "no-cache" });
    if (!response.ok) throw new Error(`Could not load ${path}`);
    return response.json();
  }
  async function loadConsumables() {
    try {
      const [consumableRecords, keyItemRecords] = await Promise.all([fetchJson("objects/consumables.json"), fetchJson("objects/key-items.json")]);
      if (!Array.isArray(consumableRecords)) throw new Error("Consumables must be an array");
      const seen = new Set();
      const catalog = consumableRecords.map(item => {
        const mapOnly = item?.mapAction === "camp" && Array.isArray(item.effects) && item.effects.length === 0;
        if (!item || typeof item.id !== "string" || !item.id.startsWith("consumable-") || seen.has(item.id) || typeof item.name !== "string" || !Number.isInteger(item.priceSilver) || item.priceSilver < 0 || !mapOnly && !effectsUsable(item.effects || [])) throw new Error("Invalid consumable definition");
        seen.add(item.id);
        return { ...item, category: "consumable", type: "consumable", rarity: item.rarity || "common", description: item.description || "" };
      });
      if (!Array.isArray(keyItemRecords)) throw new Error("Key items must be an array");
      const rarities = ["common", "uncommon", "rare", "epic", "legendary", "masterwork"];
      const keyItems = keyItemRecords.map(item => {
        const rarity = typeof item?.rarity === "string" ? item.rarity.toLowerCase() : "";
        if (!item || typeof item.id !== "string" || !item.id.startsWith("key-item-") || seen.has(item.id) || typeof item.name !== "string" || !item.name.trim() || typeof item.description !== "string" || !rarities.includes(rarity) || !Number.isInteger(item.priceSilver) || item.priceSilver < 0 || item.shopAvailable !== false) throw new Error("Invalid key item definition");
        seen.add(item.id);
        return { ...item, name: item.name.trim(), category: "key-item", type: "key-item", rarity, catalogueAvailable: item.catalogueAvailable !== false, shopAvailable: false };
      });
      itemCatalog.consumables = catalog;
      itemCatalog.keyItems = keyItems;
      rebuildItems();
      window.Shop?.onCatalogChange();
      updateItemSourceStatus();
    } catch (error) { setInventoryStatus(error.message + ". Keeping the last loaded inventory catalogue.", "error"); }
  }
  function validateEnemy(raw) {
    if (!raw || typeof raw.name !== "string" || !raw.stats || !raceCatalog.some(race => race.id === raw.raceId)) throw new Error("Enemy needs a name, stats and a valid race.");
    if (raw.element != null && !SPELL_ELEMENTS.includes(raw.element)) throw new Error(`${raw.name}: unknown spell element ${raw.element}`);
    const character = normalizeCharacter({ ...raw, startingPerkPointGranted: true });
    for (const id of Object.keys(character.classLevels)) if (!getClass(id)) throw new Error(`${raw.name}: missing class ${id}`);
    for (const id of [...character.inventory, ...Object.values(character.equipment).filter(Boolean)]) if (!getItem(id)) throw new Error(`${raw.name}: missing item ${id}`);
    const drops = Array.isArray(raw.drops) ? raw.drops.map((drop) => {
      const item = getItem(drop?.itemId);
      if (!item || !itemCanDrop(item.id)) throw new Error(`${raw.name}: invalid drop ${drop?.itemId || "entry"}`);
      const minimum = Math.max(1, toInteger(drop.minimum, 1));
      const maximum = Math.max(minimum, toInteger(drop.maximum, minimum));
      if (drop.guaranteedUntilClaimed != null && (drop.guaranteedUntilClaimed !== true || !isMasterworkEquipment(item))) throw new Error(`${raw.name}: guaranteed drops must be masterwork equipment`);
      if (drop.guaranteedOnFirstKill != null && (drop.guaranteedOnFirstKill !== true || !isMasterworkEquipment(item) || drop.guaranteedUntilClaimed)) throw new Error(`${raw.name}: first-kill guarantees must name masterwork equipment and cannot also guarantee until claimed`);
      return { itemId: item.id, chance: Math.max(0, Math.min(100, toNumber(drop.chance, 100))), minimum, maximum, guaranteedUntilClaimed: drop.guaranteedUntilClaimed === true, guaranteedOnFirstKill: drop.guaranteedOnFirstKill === true };
    }) : [];
    if (character.raceId === "goblin") {
      const swordId = "masterwork-stunted-sword";
      if (!itemCanDrop(swordId)) throw new Error(`${raw.name}: Stunted Sword must be loaded for Goblin drops`);
      const swordDrop = { itemId: swordId, chance: 5, minimum: 1, maximum: 1 };
      const existing = drops.findIndex(drop => drop.itemId === swordId);
      if (existing < 0) drops.push(swordDrop);
      else drops[existing] = swordDrop;
    }
    if (toInteger(character.classLevels.vampire, 0) > 0) {
      const crystalId = "key-item-blood-crystal";
      if (!itemCanDrop(crystalId)) throw new Error(`${raw.name}: Blood Crystal must be loaded for Vampire drops`);
      const crystalDrop = { itemId: crystalId, chance: 1, minimum: 1, maximum: 1 };
      const existing = drops.findIndex(drop => drop.itemId === crystalId);
      if (existing < 0) drops.push(crystalDrop);
    }
    // Port task 5: Noble Hero at 1% from every demon and devil but the bosses (M22), and the Shadow Crystal at 0.05% from
    // every enemy that gives loot, bosses too, not objects (M24).
    // The Hexbane Coat at 1% from every non-boss undead, demon and devil, not objects (M26).
    for (const [itemId, chance, applies] of [["masterwork-noble-hero", 1, ["demon", "devil"].includes(character.raceId) && raw.unique !== true], ["masterwork-hexbane-coat", 1, ["undead", "demon", "devil"].includes(character.raceId) && raw.unique !== true && raw.inert !== true], ["key-item-shadow-crystal", 0.05, raw.inert !== true]]) {
      if (!applies) continue;
      if (!itemCanDrop(itemId)) throw new Error(`${raw.name}: ${itemId} must be loaded for its drops`);
      if (!drops.some(drop => drop.itemId === itemId)) drops.push({ itemId, chance, minimum: 1, maximum: 1 });
    }
    const baseArmor = raw.baseArmor == null ? null : Math.max(0, toNumber(raw.baseArmor, NaN));
    if (raw.baseArmor != null && !Number.isFinite(baseArmor)) throw new Error(`${raw.name}: baseArmor must be a nonnegative number`);
    const maxManaPercent = raw.maxManaPercent == null ? 0 : Math.max(0, toNumber(raw.maxManaPercent, NaN));
    if (raw.maxManaPercent != null && !Number.isFinite(maxManaPercent)) throw new Error(`${raw.name}: maxManaPercent must be a nonnegative number`);
    const maxHpPercent = raw.maxHpPercent == null ? 0 : toNumber(raw.maxHpPercent, NaN);
    if (!Number.isFinite(maxHpPercent) || maxHpPercent < 0) throw new Error(`${raw.name}: maxHpPercent must be a nonnegative number`);
    const turnStartManaPercent = raw.turnStartManaPercent == null ? 0 : raw.turnStartManaPercent;
    if (!Number.isFinite(turnStartManaPercent) || turnStartManaPercent < 0 || turnStartManaPercent > 100) throw new Error(`${raw.name}: invalid turn-start Mana regeneration`);
    const introCinematic = raw.introCinematic ?? null;
    if (introCinematic != null && (typeof introCinematic !== "string" || window.BossIntros && !window.BossIntros.get(introCinematic))) throw new Error(`${raw.name}: unknown intro cutscene ${introCinematic}`);
    for (const phase of raw.phaseTransitions || (raw.phaseTransition ? [raw.phaseTransition] : [])) {
      if (phase?.cinematic != null && (typeof phase.cinematic !== "string" || window.BossIntros && !window.BossIntros.get(phase.cinematic))) throw new Error(`${raw.name}: unknown phase cutscene ${phase.cinematic}`);
    }
    const aiProfile = raw.aiProfile ?? null;
    if (aiProfile != null && !Object.hasOwn(AI_PROFILES, aiProfile)) throw new Error(`${raw.name}: unknown AI profile ${aiProfile}`);
    const oldWounds = raw.oldWounds ?? null;
    if (oldWounds != null && (!Number.isInteger(oldWounds.count) || oldWounds.count < 1 || oldWounds.count > 10 || ["bleedPercent", "damageTakenPercent", "rageDamagePercent"].some(key => !Number.isFinite(oldWounds[key]) || oldWounds[key] < 0))) throw new Error(`${raw.name}: invalid old wounds`);
    const stormConfig = raw.storm ?? null;
    if (stormConfig != null && (!Array.isArray(stormConfig.stages) || stormConfig.stages.length < 2 || stormConfig.stages.some((stage, index) => !stage || typeof stage.id !== "string" || typeof stage.name !== "string" || !Array.isArray(stage.allies || []) || !Number.isFinite(stage.at) || index > 0 && stage.at <= stormConfig.stages[index - 1].at))) throw new Error(`${raw.name}: invalid storm`);
    const infernalSacrifice = raw.infernalSacrifice ?? null;
    if (infernalSacrifice != null && (!Array.isArray(infernalSacrifice.servants) || !infernalSacrifice.servants.length || infernalSacrifice.servants.some(id => typeof id !== "string")
      || !Number.isFinite(infernalSacrifice.belowPercent) || !Number.isFinite(infernalSacrifice.healPercent) || !Number.isFinite(infernalSacrifice.powerPercent) || !Number.isInteger(infernalSacrifice.returnTurns) || infernalSacrifice.returnTurns < 1)) throw new Error(`${raw.name}: invalid infernal sacrifice`);
    const starTrials = raw.starTrials ?? null;
    if (starTrials != null && (typeof starTrials !== "object" || starTrials.mirror && !(Number.isFinite(starTrials.mirror.reflectPercent) && Number.isFinite(starTrials.mirror.breakPercent) && Number.isInteger(starTrials.mirror.reformTurns) && starTrials.mirror.reformTurns >= 1)
      || starTrials.sunfall && !(Number.isFinite(starTrials.sunfall.damagePercent) && Number.isFinite(starTrials.sunfall.deflectPercent))
      || starTrials.supernova && !(Number.isInteger(starTrials.supernova.chargeTurns) && starTrials.supernova.chargeTurns >= 1 && Number.isFinite(starTrials.supernova.damagePercent) && Number.isFinite(starTrials.supernova.collapsePercent)))) throw new Error(`${raw.name}: invalid star trials`);
    const dreamLaws = raw.dreamLaws ?? null;
    if (dreamLaws != null && (!Number.isInteger(dreamLaws.everyTurns) || dreamLaws.everyTurns < 1 || !Array.isArray(dreamLaws.laws) || dreamLaws.laws.length < 2
      || dreamLaws.laws.some(law => !law || typeof law.id !== "string" || typeof law.name !== "string" || !Array.isArray(law.foes || []) || !Array.isArray(law.allies || []) || law.apLoss != null && !(Number.isInteger(law.apLoss) && law.apLoss >= 0)))) throw new Error(`${raw.name}: invalid dream laws`);
    const phylactery = raw.phylactery ?? null;
    if (phylactery != null && (typeof phylactery.enemyId !== "string" || !Array.isArray(phylactery.guards) || phylactery.guards.some(id => typeof id !== "string") || !Number.isInteger(phylactery.reformRounds) || phylactery.reformRounds < 1
      || !Number.isFinite(phylactery.reformHpPercent) || phylactery.reformHpPercent <= 0 || phylactery.reformHpPercent > 100 || !Array.isArray(phylactery.broken?.effects || [])
      || phylactery.cinematic != null && (typeof phylactery.cinematic !== "string" || window.BossIntros && !window.BossIntros.get(phylactery.cinematic))
      || phylactery.guardAiProfile != null && !Object.hasOwn(AI_PROFILES, phylactery.guardAiProfile))) throw new Error(`${raw.name}: invalid phylactery`);
    const heartCores = raw.heartCores ?? null;
    if (heartCores != null && (typeof heartCores.enemyId !== "string" || !Number.isInteger(heartCores.regrowTurns) || heartCores.regrowTurns < 1 || !heartCores.severed
      || !Number.isInteger(heartCores.severed.turns) || heartCores.severed.turns < 1 || !Number.isFinite(heartCores.severed.damageTakenPercent) || !Number.isInteger(heartCores.severed.apLoss) || heartCores.severed.apLoss < 0)) throw new Error(`${raw.name}: invalid heart cores`);
    const reforge = raw.reforge ?? null;
    if (reforge != null && (!Number.isInteger(reforge.everyTurns) || reforge.everyTurns < 1 || !Number.isFinite(reforge.spoilPercent) || reforge.spoilPercent <= 0 || !Array.isArray(reforge.tempers) || reforge.tempers.length < 2
      || reforge.tempers.some(temper => !temper || typeof temper.id !== "string" || typeof temper.name !== "string" || !Array.isArray(temper.effects)))) throw new Error(`${raw.name}: invalid reforge`);
    const royalDuel = raw.royalDuel ?? null;
    if (royalDuel != null && (!Number.isInteger(royalDuel.durationTurns) || royalDuel.durationTurns < 1 || !Number.isFinite(royalDuel.outsideDamagePercent) || royalDuel.outsideDamagePercent > 0 || !Array.isArray(royalDuel.honor || []))) throw new Error(`${raw.name}: invalid royal duel`);
    const warband = raw.warband ?? null;
    if (warband != null && (typeof warband.orders !== "object" || !warband.orders.charge || Object.values(warband.orders).some(order => !order || typeof order.name !== "string" || !Array.isArray(order.allies || []) || !Array.isArray(order.prey || []))
      || !Array.isArray(warband.loseHeart || []) || warband.vengeance != null && !Number.isFinite(warband.vengeance.damagePercent))) throw new Error(`${raw.name}: invalid warband`);
    const seasonCycle = raw.seasonCycle ?? null;
    if (seasonCycle != null && (!Number.isInteger(seasonCycle.turns) || seasonCycle.turns < 1 || !Array.isArray(seasonCycle.seasons) || seasonCycle.seasons.length < 2
      || seasonCycle.seasons.some(season => !season || typeof season.id !== "string" || typeof season.name !== "string" || !Array.isArray(season.allies || []) || !Array.isArray(season.foes || [])
        || season.spirit != null && typeof season.spirit !== "string" || season.roots != null && !(Number.isInteger(season.roots.ap) && season.roots.ap > 0)))) throw new Error(`${raw.name}: invalid season cycle`);
    const aiSummonCondition = raw.aiSummonCondition;
    if (aiSummonCondition != null && (!Number.isInteger(aiSummonCondition.maximumLiving) || aiSummonCondition.maximumLiving < 0 || aiSummonCondition.maximumLiving > 100 || !Array.isArray(aiSummonCondition.raceIds) || !aiSummonCondition.raceIds.length || aiSummonCondition.raceIds.some(id => !raceCatalog.some(race => race.id === id)))) throw new Error(`${raw.name}: invalid AI summon condition`);
    const noDropItems = raw.noDropItems || [];
    if (!Array.isArray(noDropItems) || noDropItems.some(id => typeof id !== "string" || !character.inventory.includes(id) || !getItem(id))) throw new Error(`${raw.name}: invalid non-droppable equipment`);
    const startingSummons = raw.startingSummons || [];
    if (!Array.isArray(startingSummons) || startingSummons.some(entry => !entry || typeof entry.enemyId !== "string" || !entry.enemyId.trim() || !Number.isInteger(entry.count) || entry.count < 1 || entry.count > 50) || startingSummons.reduce((total, entry) => total + entry.count, 0) > 50) throw new Error(`${raw.name}: invalid starting summons`);
    const startingAllies = raw.startingAllies || [];
    if (!Array.isArray(startingAllies) || startingAllies.some(entry => !entry || typeof entry.enemyId !== "string" || !entry.enemyId.trim() || !Number.isInteger(entry.count) || entry.count < 1 || entry.count > 49) || startingAllies.reduce((total, entry) => total + entry.count, 0) > 49) throw new Error(`${raw.name}: invalid starting allies`);
    const unlockedSkills = skillsOf({ character, effects: [], skillExpiry: {} }, null).map(classSkillKey);
    const initialSkillCharges = raw.initialSkillCharges || {};
    if (typeof initialSkillCharges !== "object" || Array.isArray(initialSkillCharges) || Object.entries(initialSkillCharges).some(([key, count]) => !unlockedSkills.includes(key) || !Number.isInteger(count) || count < 1 || count > 100)) throw new Error(`${raw.name}: invalid initial skill charges`);
    if (raw.phaseTransitions != null && (!Array.isArray(raw.phaseTransitions) || raw.phaseTransition != null)) throw new Error(`${raw.name}: invalid phase transitions`);
    const phases = raw.phaseTransitions || (raw.phaseTransition ? [raw.phaseTransition] : []);
    if (phases.some(phase => phase?.spellDamagePercent != null && (!Number.isFinite(phase.spellDamagePercent) || phase.spellDamagePercent < 0 || phase.spellDamagePercent > 500))) throw new Error(`${raw.name}: invalid phase spell damage bonus`);
    if (phases.some((phase, index) => !phase || typeof phase !== "object" || typeof phase.name !== "string" || !phase.name.trim() || !Number.isFinite(phase.thresholdHpPercent) || phase.thresholdHpPercent <= 0 || phase.thresholdHpPercent >= 100 || (index > 0 && phase.thresholdHpPercent >= phases[index - 1].thresholdHpPercent) || !Number.isFinite(phase.healMaxHpPercent) || phase.healMaxHpPercent < 0 || phase.healMaxHpPercent > 100 || !Array.isArray(phase.knownSpells) || phase.knownSpells.some(id => !getSpell(id)) || Object.entries(phase.statBonuses || {}).some(([key, amount]) => !bonusDefinitions.includes(key) || !Number.isFinite(amount)) || Object.entries(phase.defenseBonuses || {}).some(([key, amount]) => !defenseBonusDefinitions.includes(key) || !Number.isFinite(amount)))) throw new Error(`${raw.name}: invalid phase transition`);
    if (phases.some(phase => phase.activateSkill != null && (!unlockedSkills.includes(phase.activateSkill) || !initialSkillCharges[phase.activateSkill]) || phase.summons != null && (!Array.isArray(phase.summons) || phase.summons.some(summon => typeof summon.enemyId !== "string" || !Number.isInteger(summon.count) || summon.count < 1 || summon.count > 10)))) throw new Error(`${raw.name}: invalid phase activation`);
    const spellPowerAura = raw.spellPowerAura;
    if (spellPowerAura != null && (typeof spellPowerAura.targetEnemyId !== "string" || !spellPowerAura.targetEnemyId.trim() || !Number.isFinite(spellPowerAura.amount) || spellPowerAura.amount <= 0)) throw new Error(`${raw.name}: invalid spell-power aura`);
    const livingAllyProtection = raw.livingAllyProtection;
    if (livingAllyProtection != null && (typeof livingAllyProtection.enemyId !== "string" || !livingAllyProtection.enemyId.trim() || !Number.isFinite(livingAllyProtection.amount) || livingAllyProtection.amount <= 0 || livingAllyProtection.amount > 100)) throw new Error(`${raw.name}: invalid living-ally protection`);
    const naturalWeapon = raw.naturalWeapon ?? null;
    if (naturalWeapon != null && !window.ForgeRules?.NATURAL_WEAPONS[naturalWeapon]) throw new Error(`${raw.name}: unknown natural weapon ${naturalWeapon}`);
    const experienceRewardMultiplier = Math.max(0, toNumber(raw.experienceRewardMultiplier, 1));
    const moneyRewardMultiplier = Math.max(0, toNumber(raw.moneyRewardMultiplier, 1));
    // A corrupted creature may strike with another element than its weapon's (port task 6: the Corrupted Mire Stalker's Dark).
    const attackElement = raw.attackElement ?? null;
    if (attackElement != null && !SPELL_ELEMENTS.includes(attackElement)) throw new Error(`${raw.name}: unknown attack element ${attackElement}`);
    return {
      id: raw.id || slugify(raw.name), name: raw.name, character, element: raw.element || null, attackElement, resistances: raw.resistances || {}, drops, naturalWeapon,
      oldWounds: oldWounds ? copy(oldWounds) : null, warband: warband ? copy(warband) : null, royalDuel: royalDuel ? copy(royalDuel) : null, storm: stormConfig ? copy(stormConfig) : null, reforge: reforge ? copy(reforge) : null, heartCores: heartCores ? copy(heartCores) : null, infernalSacrifice: infernalSacrifice ? copy(infernalSacrifice) : null, phylactery: phylactery ? copy(phylactery) : null, dreamLaws: dreamLaws ? copy(dreamLaws) : null, starTrials: starTrials ? copy(starTrials) : null, inert: raw.inert === true, unlivingCaster: raw.unlivingCaster === true,
      formerNames: Array.isArray(raw.formerNames) ? raw.formerNames.filter(name => typeof name === "string" && name.trim()) : [],
      baseArmor, maxHpPercent, maxManaPercent, turnStartManaPercent, aiProfile, introCinematic, seasonCycle: seasonCycle ? copy(seasonCycle) : null, aiSummonCondition: aiSummonCondition ? copy(aiSummonCondition) : null, noDropItems: [...new Set(noDropItems)], startingSummons: copy(startingSummons), startingAllies: copy(startingAllies), initialSkillCharges, phaseTransition: raw.phaseTransition || null, phaseTransitions: raw.phaseTransitions || null, spellPowerAura: spellPowerAura || null, livingAllyProtection: livingAllyProtection ? copy(livingAllyProtection) : null, unique: raw.unique === true, experienceRewardMultiplier, moneyRewardMultiplier
    };
  }
  let enemyLoadPromise = null;
  async function loadEnemies() {
    let files;
    try {
      const response = await fetch("enemies/?v=" + Date.now(), { cache: "no-store" });
      if (!response.ok) throw new Error("No directory listing");
      const doc = new DOMParser().parseFromString(await response.text(), "text/html");
      files = [...doc.querySelectorAll("a[href]")].map(link => decodeURIComponent(link.getAttribute("href"))).filter(file => /^[^/]+\.json$/i.test(file) && file !== "index.json");
      if (!files.length) throw new Error("No enemy files found");
    } catch { files = await fetchJson("enemies/index.json"); }
    files.sort((first, second) => first.localeCompare(second));
    const results = await Promise.allSettled(files.map(async file => {
      if (typeof file !== "string" || !/^[^/\\]+\.json$/i.test(file) || file === "index.json") throw new Error("Invalid enemy filename");
      return validateEnemy(await fetchJson("enemies/" + encodeURIComponent(file)));
    }));
    enemyDefinitions = results.filter(result => result.status === "fulfilled").map(result => result.value);
    const errors = results.filter(result => result.status === "rejected").map(result => result.reason.message);
    options("combat-enemy-select", enemyDefinitions.map((enemy, i) => ({ id: String(i), label: enemy.name })), "No enemies available");
    if (!enemyDefinitions.length) throw new Error(errors.join(" ") || "No enemies available");
    return errors.join(" ");
  }
  function ensureEnemiesLoaded() {
    if (enemyDefinitions.length) return Promise.resolve("");
    if (enemyLoadPromise) return enemyLoadPromise;
    enemyLoadPromise = loadEnemies().finally(() => { enemyLoadPromise = null; });
    return enemyLoadPromise;
  }
  async function openSetup() {
    if (!editingEnabled) return;
    if (isActive()) { switchView(true); return; }
    setup.hidden = false; updateModalState();
    $("combat-start").disabled = true; $("combat-setup-status").textContent = "Loading enemies…";
    try {
      await synchronizeSourceFiles();
      const errors = await ensureEnemiesLoaded();
      $("combat-start").disabled = currentHp <= 0;
      $("combat-setup-status").textContent = currentHp <= 0 ? "Restore some HP before starting combat." : errors;

    } catch (error) { $("combat-setup-status").textContent = error.message + ". Open the desktop shortcut to synchronize enemy files."; }
    $("combat-enemy-select").focus();
  }
  function triggerCombatStartPassives(actor) {
    for (const effect of passiveEffectsOf(actor).filter(entry => entry.type === "persistent-summon")) {
      const progress = actor.id === "player" ? classProgress : (actor.character.classProgress ||= { oneHandedAttacks: 0, summonCasts: {}, lootEnemiesCollected: 0, boundFamiliar: null });
      const candidates = spellsOf(actor).filter(isSummonSpell).filter(entry => effectsUsable(spellEffects(entry)));
      const spell = candidates.find(entry => entry.id === progress.boundFamiliar?.spellId)
        || candidates.sort((left, right) => toInteger(progress.summonCasts?.[right.id], 0) - toInteger(progress.summonCasts?.[left.id], 0))[0];
      if (!spell || !progress.boundFamiliar && !toInteger(progress.summonCasts?.[spell.id], 0)) continue;
      if (spell.id !== progress.boundFamiliar?.spellId) progress.boundFamiliar = { spellId: spell.id, victories: 0 };
      const hpCost = Math.min(Math.max(0, hp(actor) - 1), Math.floor(values(actor).maxHp * Math.max(0, toNumber(effect.hpCostPercent, 0)) / 100));
      if (hpCost) directDamage(actor, hpCost, false, "dark");
      const growth = Math.min(20, Math.max(0, toInteger(progress.boundFamiliar.victories, 0)) * 2);
      const existing = new Set(battle.actors);
      applyEffects(actor, null, spellEffects(spell).filter(entry => entry.type === "summon-friendly").map(entry => ({ ...entry, summonBaseStatsPercent: toNumber(entry.summonBaseStatsPercent, 0) + growth })), withActor(actor, () => calculateSpellValue(spell)), spell.element, `Bound Familiar: ${spell.name}`, true);
      for (const summoned of battle.actors) if (!existing.has(summoned) && summoned.summonerId === actor.id) summoned.boundFamiliar = true;
    }
    for (const effect of passiveEffectsOf(actor).filter(entry => entry.type === "combat-start-random-animal")) {
      const count = summonRandomAnimals(actor, effect, effect.name || "Animal Companion");
      if (count) log(`${actorLabel(actor)} begins combat alongside ${count === 1 ? "an animal companion" : count + " animal companions"}.`);
    }
    for (const effect of passiveEffectsOf(actor).filter(entry => entry.type === "combat-start-random-summon-spell")) {
      const maximumTier = Math.max(1, toInteger(effect.maxTier, 3));
      const element = String(effect.element || "dark").toLowerCase();
      const candidates = spellsOf(actor).filter(spell => spell.tier <= maximumTier && spell.element === element && isSummonSpell(spell) && effectsUsable(spellEffects(spell)));
      if (!candidates.length) continue;
      const spell = candidates[Math.floor(Math.random() * candidates.length)];
      const power = withActor(actor, () => calculateSpellValue(spell));
      applyEffects(actor, null, spellEffects(spell), power, spell.element, `${effect.name || "Void Tear"}: ${spell.name}`, true);
      incrementActorSummonSpellsUsed(actor, spell.element, 1);
      log(`${actorLabel(actor)} automatically casts ${spell.name} through ${effect.name || "Void Tear"}.`);
    }
  }
  function startRoster(enemyRoster, openingNote = "", guildObjective = null, sparkling = []) {
    if (isActive() || currentHp <= 0 || !Array.isArray(enemyRoster) || !enemyRoster.length || enemyRoster.length > 50 || enemyRoster.some(enemy => !enemy)) return false;
    const originalRoster = [...enemyRoster], requiredAllies = new Map();
    for (const enemy of originalRoster) for (const ally of enemy.startingAllies || []) requiredAllies.set(ally.enemyId, (requiredAllies.get(ally.enemyId) || 0) + ally.count);
    enemyRoster = [...originalRoster];
    for (const [id, count] of requiredAllies) {
      const definition = enemyDefinitions.find(enemy => enemy.id === id);
      const missing = Math.max(0, count - originalRoster.filter(enemy => enemy.id === id).length);
      if (!definition || enemyRoster.length + missing > 50) return false;
      enemyRoster.push(...Array(missing).fill(definition));
    }
    stopPhaseCinematic(false);
    window.TierTenCinematics?.stop();
    window.BattleStage?.reset();
    pendingLament = null;
    const makeActor = (id, name, team) => ({ id, name, team, counter: 0, turns: 0, effects: [], skillExpiry: {}, alive: true, combatKills: 0, multiKillCombatRecorded: false });
    equipmentActionDamage.clear();
    reactionModal.hidden = true;
    battle = { version: 1, active: true, current: null, ap: 0, result: "", experienceEarned: 0, moneyEarned: 0, lootItems: [], observedSpells: [], reactions: [], lootResolved: false, soloVictoryRecorded: false, log: [], actors: [makeActor("player", "You", "player")] };
    for (const companion of companions) {
      const actor = { ...makeActor(companion.id, companion.name, "player"), character: companion.character, companion: true };
      battle.actors.push(actor);
      for (const skill of skillsOf(actor)) if (skill.cooldown === "combat") delete actor.character.activeSkills[classSkillKey(skill)];
    }
    for (const skill of skillsOf(player())) {
      if (skill.cooldown === "combat") delete activeSkills[classSkillKey(skill)];
    }
    const totals = enemyRoster.reduce((counts, enemy) => ({ ...counts, [enemy.name]: (counts[enemy.name] || 0) + 1 }), {});
    const occurrences = {};
    const empoweredObjective = ["hunt", "artifact"].includes(guildObjective);
    // The encounter's Sparkling enemies (M2): none in a battle with a boss, never an object, never an added ally.
    const sparklingIndexes = new Set(enemyRoster.some(enemy => enemy.unique) || !Array.isArray(sparkling) ? [] : sparkling.filter(index => Number.isInteger(index) && index >= 0 && index < originalRoster.length && !originalRoster[index].inert));
    for (let i = 0; i < enemyRoster.length; i++) {
      const enemy = enemyRoster[i];
      occurrences[enemy.name] = (occurrences[enemy.name] || 0) + 1;
      const actorName = totals[enemy.name] > 1 ? `${enemy.name} ${occurrences[enemy.name]}` : enemy.name;
      const armed = armEnemy(copy(enemy.character), enemy.noDropItems);
      const actor = {
        ...makeActor(`enemy-${i + 1}`, `${empoweredObjective && i === 0 ? "Empowered " : ""}${sparklingIndexes.has(i) ? "Sparkling " : ""}${actorName}`, "enemy"),
        experienceAwarded: false, character: armed.character, resistances: copy(enemy.resistances), dropTable: copy(enemy.drops || []), noDropItems: armed.noDropItems,
        baseArmor: enemy.baseArmor, naturalArmor: spawnNaturalArmor(armed.character), naturalWeapon: enemy.naturalWeapon || null, attackElement: enemy.attackElement || null, maxHpPercent: enemy.maxHpPercent, maxManaPercent: enemy.maxManaPercent, turnStartManaPercent: enemy.turnStartManaPercent, element: enemy.element, unique: enemy.unique, experienceRewardMultiplier: enemy.experienceRewardMultiplier, moneyRewardMultiplier: enemy.moneyRewardMultiplier,
        enemyDefinitionId: enemy.id, phaseTransition: copy(enemy.phaseTransition), phaseTransitions: copy(enemy.phaseTransitions), phaseIndex: 0, spellPowerAura: copy(enemy.spellPowerAura), livingAllyProtection: copy(enemy.livingAllyProtection)
      };
      if (guildObjective && i === 0) {
        actor.guildObjective = guildObjective;
        if (guildObjective === "great-hunt-rematch") actor.guildRematchId = window.Guild?.rematchAt(worldState.position, true)?.uniqueId || null;
        if (empoweredObjective) {
          actor.empowered = true;
          for (const key of Object.keys(actor.character.stats)) actor.character.stats[key] = Math.max(0, Math.floor(actor.character.stats[key] * 1.3));
        }
      }
      // A Sparkling enemy (M6): all eight attributes ×1.4; values() then doubles its maximum HP.
      if (sparklingIndexes.has(i)) {
        actor.sparkling = true;
        for (const key of Object.keys(actor.character.stats)) actor.character.stats[key] = Math.max(0, Math.floor(actor.character.stats[key] * SPARKLING.statMultiplier));
      }
      if (enemy.inert) actor.inert = true;
      if (enemy.unlivingCaster) actor.unlivingCaster = true;
      actor.character.activeSkills = {};
      for (const [key, charges] of Object.entries(enemy.initialSkillCharges || {})) actor.character.activeSkills[key] = { charges };
      battle.actors.push(actor);
    }
    // Lock resource caps only after the full roster exists, so start-of-battle
    // opponent and class effects are included for every participant.
    for (const actor of battle.actors) {
      const maximum = values(actor);
      actor.resourceMax = { hp: maximum.maxHp, mana: maximum.maxMana };
      if (actor.team === "enemy") {
        actor.character.currentHp = maximum.maxHp;
        actor.character.currentMana = maximum.maxMana;
      }
    }
    for (const actor of [...battle.actors].filter(entry => entry.team === "enemy")) {
      const definition = enemyDefinitions.find(entry => entry.id === actor.enemyDefinitionId);
      for (const summon of definition?.startingSummons || []) for (let count = 0; count < summon.count; count++) summonFriendly(actor, { type: "summon-friendly", enemyId: summon.enemyId }, actor.name);
    }
    for (const actor of [...battle.actors]) { triggerCombatStartPassives(actor); triggerLowHealthEquipment(actor); }
    for (const actor of [...battle.actors]) triggerArtifactBattleStart(actor);
    for (const skill of skillsOf(player())) if (activeSkills[classSkillKey(skill)]?.active && skill.durationTurns) player().skillExpiry[classSkillKey(skill)] = skill.durationTurns + 1;
    const summary = Object.entries(totals).map(([name, count]) => count > 1 ? `${name} ×${count}` : name).join(", ");
    syncEnemyCount();
    if (openingNote) log(openingNote);
    log(`Combat begins against ${empoweredObjective ? "Empowered " : ""}${summary}.`);
    const next = advance(); if (next) log(`${next.id === "player" ? "Your" : actorLabel(next) + "'s"} turn — ${actionPointMaximum(next)} AP.`);
    // refresh() renders the fully initialized battle. Avoid building the
    // roster and ten-turn forecast a second time in switchView().
    setup.hidden = true; updateModalState(); switchView(true, false); playBossIntro(); refresh();
    window.Music?.sync();
    return true;
  }
  function start(enemy, count) {
    if (!enemy || !Number.isInteger(count) || count < 1 || count > 50) return false;
    return startRoster(Array(count).fill(enemy));
  }
  async function resolveEncounterRoster(enemyNames) {
    if (isActive() || currentHp <= 0 || !Array.isArray(enemyNames) || !enemyNames.length || enemyNames.length > 50) return false;
    await waitForSourceFiles();
    await ensureEnemiesLoaded();
    // Renamed enemies still answer to their former names (saved encounters and quests).
    const definitionsByName = new Map([...enemyDefinitions.flatMap(enemy => (enemy.formerNames || []).map(name => [name, enemy])), ...enemyDefinitions.map(enemy => [enemy.name, enemy])]);
    const roster = enemyNames.map(name => definitionsByName.get(name));
    if (roster.some(enemy => !enemy)) throw new Error("An encounter references an unavailable enemy.");
    return roster;
  }
  async function startEncounter(enemyNames, openingNote = "", guildObjective = null, sparkling = []) {
    const roster = await resolveEncounterRoster(enemyNames);
    return roster ? startRoster(roster, openingNote, guildObjective, sparkling) : false;
  }
  async function attemptTravelEscape(enemyNames, bonus = 25, guildObjective = null, sparkling = []) {
    const roster = await resolveEncounterRoster(enemyNames);
    if (!roster) return { success: false, chance: 0, fastestEnemySpeed: 0 };
    const fastestEnemySpeed = Math.max(0, ...roster.map((enemy, index) => {
      const actor = { id: `encounter-preview-${index}`, name: enemy.name, team: "enemy", effects: [], skillExpiry: {}, character: copy(enemy.character) };
      if (["hunt", "artifact"].includes(guildObjective) && index === 0) for (const key of Object.keys(actor.character.stats)) actor.character.stats[key] = Math.max(0, Math.floor(actor.character.stats[key] * 1.3));
      if (Array.isArray(sparkling) && sparkling.includes(index)) for (const key of Object.keys(actor.character.stats)) actor.character.stats[key] = Math.max(0, Math.floor(actor.character.stats[key] * SPARKLING.statMultiplier));
      return withActor(actor, () => currentStats().speed, { active: false, actors: [actor] });
    }));
    const playerValues = currentStats();
    const normal = rules.chance(rules.escape(playerValues.speed, playerValues.luck, fastestEnemySpeed) + toNumber(bonus, 0));
    // Quest fights are harder to escape before they start: Search 50% less, Escort bandits 70% less (task 2, G14).
    const chance = window.Guild?.escapeChance ? window.Guild.escapeChance(normal, guildObjective) : normal;
    return { success: rules.roll(chance), chance, fastestEnemySpeed };
  }
  function restore(saved) {
    window.BattleStage?.reset();
    stopPhaseCinematic(false);
    stopBossIntro(false);
    window.TierTenCinematics?.stop();
    clearTimeout(timer); timer = null;
    lootModal.hidden = true;
    reactionModal.hidden = true;
    equipmentActionDamage.clear();
    resolvingEquipmentReaction = false;
    battle = null;
    if (saved?.version === 1 && Array.isArray(saved.actors) && saved.actors.length >= 2 && saved.actors.length <= 100) {
      try {
        const candidate = copy(saved), ids = new Set();
        candidate.actors.forEach(actor => {
          if (typeof actor.id !== "string" || ids.has(actor.id) || typeof actor.name !== "string") throw new Error("Invalid participant");
          for (const [before, after] of Object.entries(window.WorldRules?.RENAMED_ENEMIES || {})) if (actor.name === before || actor.name.startsWith(before + " ")) actor.name = after + actor.name.slice(before.length);
          ids.add(actor.id); actor.counter = Math.max(0, toNumber(actor.counter)); actor.turns = Math.max(0, toInteger(actor.turns));
          actor.phaseFlashUntil = 0;
          actor.effects = (actor.effects || []).filter(effect => Number.isFinite(effect.expiresTurn));
          actor.skillExpiry = actor.skillExpiry || {};
          actor.ai = CombatAI.memory(actor.ai);
          if (!actor.equipmentCharges || typeof actor.equipmentCharges !== "object" || Array.isArray(actor.equipmentCharges)) actor.equipmentCharges = {};
          if (!actor.equipmentSummons || typeof actor.equipmentSummons !== "object" || Array.isArray(actor.equipmentSummons)) actor.equipmentSummons = {};
          if (actor.id !== "player") {
            actor.character = normalizeCharacter(actor.character);
            if (actor.companion) {
              const member = companions.find(entry => entry.id === actor.id);
              if (!member) throw new Error("Missing companion");
              member.character = actor.character;
            }
          }
        });
        if (!ids.has("player") || (candidate.current && !ids.has(candidate.current))) throw new Error("Invalid current turn");
        candidate.ap = Math.max(0, Math.min(100, toInteger(candidate.ap)));
        candidate.experienceEarned = Math.max(0, toInteger(candidate.experienceEarned, 0));
        candidate.moneyEarned = Math.max(0, toInteger(candidate.moneyEarned, 0));
        candidate.lootItems = Array.isArray(candidate.lootItems) ? candidate.lootItems.filter(id => typeof id === "string" && itemCanDrop(id)) : [];
        candidate.lootResolved = candidate.lootResolved === true;
        candidate.soloVictoryRecorded = candidate.soloVictoryRecorded === true;
        candidate.log = (candidate.log || []).filter(entry => typeof entry === "string").slice(-200);
        candidate.reactions = (Array.isArray(candidate.reactions) ? candidate.reactions : []).filter(record => record && ids.has(record.actorId) && ids.has(record.attackerId) && typeof record.itemId === "string" && Number.isFinite(record.damage) && record.damage > 0).slice(0, 32);
        battle = candidate;
        if (battle.active) for (const actor of battle.actors) {
          const savedMaximum = actor.resourceMax;
          if (Number.isFinite(savedMaximum?.hp) && Number.isFinite(savedMaximum?.mana)) {
            actor.resourceMax = { hp: Math.max(0, Math.floor(savedMaximum.hp)), mana: Math.max(0, Math.floor(savedMaximum.mana)) };
          } else {
            // Older saves have no fixed cap; freeze their last known resources.
            const legacyHp = actor.maxHp, legacyMana = actor.maxMana;
            const calculated = values(actor);
            actor.resourceMax = {
              hp: Number.isFinite(legacyHp) ? Math.max(0, Math.floor(legacyHp)) : calculated.maxHp,
              mana: Number.isFinite(legacyMana) ? Math.max(0, Math.floor(legacyMana)) : calculated.maxMana
            };
          }
        }
        if (currentActor()) battle.ap = Math.min(battle.ap, actionPointMaximum(currentActor()));
      } catch { battle = null; }
    }
    syncEnemyCount(); switchView(false);
    window.Music?.sync();
    if (battle && !battle.active && !battle.lootResolved && battle.result.startsWith("Victory")) queueMicrotask(renderLoot);
  }

  window.Combat = {
    armorForEquipment() {
      const state = evaluationBattle || battle, actor = evaluating || actorById("player", state);
      return state && actor && (state.active || evaluating) ? values(actor, state).armor : null;
    },
    // Targets an actor chosen on the animated battlefield.
    chooseTargetId(id) {
      const actor = actorById(id);
      if (!isActive() || !actor || hp(actor) <= 0 || actor.team !== player().team && protectedByOwnedSummon(actor)) return false;
      if (pendingLament) return castLamentOn(id);
      battle.targetId = id; render(); saveCharacter();
      return true;
    },
    active: isActive, statPercent, permanentStatPercent, permanentDefensePercent, statValue, defense, resourceMaximum, cacheCalculation, serialize: () => battle ? copy(battle) : null, restore, onSheetChange, loadConsumables, startEncounter, attemptTravelEscape, rollSparkling, sparklingChance, SPARKLING: () => copy(SPARKLING),
    async enemyDropSources() {
      await ensureEnemiesLoaded();
      return enemyDefinitions.map(enemy => ({ id: enemy.id, name: enemy.name, level: enemy.character.level,
        inventory: [...enemy.character.inventory], noDropItems: [...enemy.noDropItems], drops: copy(enemy.drops) }));
    },
    musicTheme() {
      if (!battle?.active) return null;
      // Tankina's storm ritual turns wild once she is the Chosen of Talos.
      const tankina = battle.actors.find(actor => actor.team === "enemy" && actor.enemyDefinitionId === "tankina-disciple-of-talos-level-44");
      if (tankina) return toInteger(tankina.phaseIndex, 0) >= 1 ? "tankina-chosen-of-talos" : "tankina-disciple-of-talos";
      // Ur-Mat's dream waltz shatters and races once Lucid Change wakes him.
      const urMat = battle.actors.find(actor => actor.team === "enemy" && actor.enemyDefinitionId === "ur-mat-false-dreamer-unique-level-90" && hp(actor) > 0);
      if (urMat) return toInteger(urMat.phaseIndex, 0) >= 1 ? "ur-mat-lucid-change" : "ur-mat-false-dreamer";
      if (battle.actors.some(actor => actor.team === "enemy" && actor.enemyDefinitionId === "archdevil-saffron-unique-level-66" && hp(actor) > 0)) return "archdevil-saffron";
      // Ool's requiem breaks into a frantic dirge once his phylactery is broken.
      if (battle.actors.some(actor => actor.team === "enemy" && actor.enemyDefinitionId === "ancient-lich-ool-unique-level-78" && (hp(actor) > 0 || phylacteryHolds(actor)))) return battle.phylactery?.broken ? "ancient-lich-ool-mortal" : "ancient-lich-ool";
      // Gaian's heartbeat falters while he is severed from his Cores.
      if (battle.actors.some(actor => actor.team === "enemy" && actor.enemyDefinitionId === "gaian-avatar-of-the-land-unique-level-53" && hp(actor) > 0)) return heartCoreState()?.severed ? "gaian-severed" : "gaian-avatar-of-the-land";
      // Ornn's work-song turns savage when the forge erupts.
      const ornn = battle.actors.find(actor => actor.team === "enemy" && actor.enemyDefinitionId === "ornn-god-of-the-forge-unique-level-48" && hp(actor) > 0);
      if (ornn) return toInteger(ornn.phaseIndex, 0) >= 1 ? "ornn-forge-erupts" : "ornn-god-of-the-forge";
      // Anatemis's hymn changes with each form: the White Star, the Golden Star's march, the Zenith's finale.
      const starDragon = battle.actors.find(actor => actor.team === "enemy" && actor.enemyDefinitionId === "anatemis-white-star-unique-level-100" && hp(actor) > 0);
      if (starDragon) return ["anatemis-white-star", "anatemis-golden-star", "anatemis-zenith"][Math.min(2, toInteger(starDragon.phaseIndex, 0))];
      if (battle.actors.some(actor => actor.team === "enemy" && actor.enemyDefinitionId === "juila-grand-forest-matriach-unique-level-22" && hp(actor) > 0)) return "juila-grand-forest-matriach";
      if (battle.actors.some(actor => actor.team === "enemy" && actor.enemyDefinitionId === "deep-sea-king-unique-level-32" && hp(actor) > 0)) return "deep-sea-king";
      // Nygheir's lament turns to fury once his scars burst.
      const nygheir = battle.actors.find(actor => actor.team === "enemy" && actor.enemyDefinitionId === "nygheir-the-wounded-unique-level-23" && hp(actor) > 0);
      if (nygheir) return toInteger(nygheir.phaseIndex, 0) >= 1 ? "nygheir-the-scarred" : "nygheir-the-wounded";
      if (battle.actors.some(actor => actor.team === "enemy" && actor.unique && actor.name.replace(/^(?:(?:Empowered|Sparkling) )+/, "").replace(/ \d+$/, "") === "Zik Lun, the armored Hulk")) return "zik-lun-armored-hulk";
      return battle.actors.some(actor => actor.team === "enemy" && actor.unique && /^Great Imp Ji'ruan(?: \d+)?$/.test(actor.name))
        ? "great-imp-jiruan" : "combat";
    },
    alliedSpellPowerPercent() {
      const state = evaluationBattle || battle, actor = evaluating || actorById("player", state);
      if (!state?.active || !actor) return 0;
      // An absent aura and a player/companion's absent enemyDefinitionId must
      // never compare equal: undefined === undefined would select that ally.
      return living(state).filter(ally => ally.team === actor.team && ally.id !== actor.id && ally.spellPowerAura && ally.spellPowerAura.targetEnemyId === actor.enemyDefinitionId)
        .reduce((total, ally) => total + toNumber(ally.spellPowerAura.amount, 0), 0);
    },
    canUseSkill(skill) { return isPlayerTurn() && skillReady(player(), skill); },
    partySpellCostPercent() {
      const state = evaluationBattle || battle, actor = evaluating || actorById("player", state);
      if (!state?.active || !actor) return 0;
      return living(state).filter(candidate => candidate.team === actor.team).reduce((total, ally) => total + passiveEffectsOf(ally)
        .filter(effect => effect.type === "party-spell-cost-percent")
        .reduce((sum, effect) => sum + Math.max(0, toNumber(effect.amount, 0)), 0), 0);
    },
    hasLivingPartyAlly() {
      const state = evaluationBattle || battle, actor = evaluating || actorById("player", state);
      if (!state?.active || !actor) return null;
      return living(state).some(candidate => candidate.team === actor.team && candidate.id !== actor.id && !candidate.summoned);
    },
    livingOpponentCount() {
      const state = evaluationBattle || battle, actor = evaluating || actorById("player", state);
      return state?.active && actor ? opponents(actor, state).length : 0;
    },
    previewAiDecision(actorId = battle?.current) {
      const actor = actorById(actorId);
      if (!isActive() || !actor || hp(actor) <= 0 || !opponents(actor).length) return null;
      const candidate = aiCandidates(actor)[0];
      if (!candidate) return { kind: "end-turn", targetId: null, score: 0 };
      return {
        kind: candidate.kind,
        targetId: candidate.target?.id || null,
        spellId: candidate.spell?.id || null,
        skillKey: candidate.key || null,
        stance: candidate.stance || null,
        reason: candidate.reason || null,
        intent: candidate.intent || null,
        score: Math.round(candidate.score * 100) / 100,
        expectedDamage: Math.round(toNumber(candidate.outcome?.expectedDamage, 0) * 100) / 100,
        killChance: Math.round(toNumber(candidate.outcome?.killProbability, 0) * 10000) / 100
      };
    },
    previewValues(character) {
      const live = isActive() && battle.actors.find(actor => actor.companion && actor.character === character);
      const actor = live || { id: "companion-preview", team: "player", character, effects: [], skillExpiry: {} };
      return withCalculationFrame(() => values(actor, live ? battle : { active: false, actors: [actor] }));
    },
    previewDefenseBreakdown(character) {
      const live = isActive() && battle.actors.find(actor => actor.companion && actor.character === character);
      const actor = live || { id: "companion-preview", team: "player", character, effects: [], skillExpiry: {} };
      return withCalculationFrame(() => defenseBreakdown(actor, live ? battle : { active: false, actors: [actor] }));
    },
    previewClassEffects(character) { const actor = { id: "companion-preview", team: "player", character, effects: [], skillExpiry: {} }; return withActor(actor, () => calculateClassEffects(), { active: false, actors: [actor] }); },
    previewSheetDetails(character, item = null, spell = null) {
      const actor = battle?.actors?.find(candidate => candidate.companion && candidate.character === character)
        || { id: "companion-preview", team: "player", character, effects: [], skillExpiry: {} };
      const state = isActive() && actor.companion ? battle : { active: false, actors: [actor] };
      return withCalculationFrame(() => withActor(actor, () => ({
        values: values(actor, state), defenses: defenseBreakdown(actor, state),
        baseStats: Object.fromEntries(bonusDefinitions.map(key => [key, displayedBaseStat(key)])),
        permanent: Object.fromEntries(bonusDefinitions.map(key => [key, effectivePermanentBonus(key)])),
        permanentDefense: Object.fromEntries(defenseBonusDefinitions.map(key => [key, effectivePermanentDefenseBonus(key)])),
        itemStats: item && ["weapon", "armor", "artifact"].includes(item.category) ? equipmentItemStats(item) : [],
        spell: spell ? { power: Math.floor(calculateSpellValue(spell)), mana: calculateSpellManaCost(spell), ap: calculateSpellActionPoints(spell), description: spellDescription(spell) } : null
      }), state));
    },
    previewClassOptions(character) {
      // Check exactly the same requirements as the main sheet, in this member's
      // context. Include the main character in Guardian's party comparison.
      const playerMaximum = withActor({ id: "player" }, () => currentResourceRequirementValues().maxHp);
      const actor = { id: "companion-preview", team: "player", character, effects: [], skillExpiry: {} };
      return withCalculationFrame(() => withActor(actor, () => classCatalog.map(definition => {
        const rank = currentClassLevel(definition);
        const partyHpMet = definition.requirements.other?.type !== "party-highest-max-hp" || currentResourceRequirementValues().maxHp >= playerMaximum;
        const met = classRequirementsMet(definition) && partyHpMet;
        let reason = classActionReason(definition);
        if (!rank && !partyHpMet) reason = "Requires the most maximum HP in the party";
        if (pendingClassSpellChoice) reason = "Finish your pending class spell choice first";
        if (isActive()) reason = "Training is unavailable during combat";
        return { definition, rank, met, reason, requirements: classRequirementSummary(definition) };
      }), { active: false, actors: [actor] }));
    },
    healthBelowPercent(percent) {
      if (!isActive()) return null;
      const actor = evaluating || player();
      const maximumHp = toNumber(actor?.maxHp, 0);
      return maximumHp > 0 && hp(actor) < maximumHp * Math.max(0, toNumber(percent, 0)) / 100;
    },
    fightingRace(raceId) {
      const state = evaluationBattle || battle, actor = evaluating || actorById("player", state);
      return Boolean(state?.active && actor && opponents(actor, state).some(opponent => actorRace(opponent) === slugify(raceId)));
    },
    castFromSheet(spell) { if (isPlayerTurn()) { switchView(true); if (!beginApexChoice("spell", spell.id)) action("spell", spell.id); } else setSpellbookStatus("Spells can only be cast during your combat turn", "error"); },
    skillFromSheet(skill) {
      if (!isPlayerTurn()) return setSkillsStatus("Skills can only be used during your combat turn", "error");
      if ((skill.effects || []).some(effect => effect.type === "predict-action")) {
        switchView(true);
        $("combat-action-status").textContent = "Choose the enemy's predicted action, then use Foretell.";
        $("combat-guess")?.focus();
        return;
      }
      action("skill", classSkillKey(skill));
    },
  };
  $("combat-open-button").addEventListener("click", openSetup);
  window.addEventListener("statfick:settingschange", () => {
    if (window.GameSettings?.state().combatAnimations === false) {
      stopPhaseCinematic(false);
      stopBossIntro(false);
      window.TierTenCinematics?.stop();
      battle?.actors?.forEach(actor => { actor.phaseFlashUntil = 0; });
      if (isActive()) render();
    }
    scheduleAI();
  });
  $("anatemis-cinematic-skip")?.addEventListener("click", () => stopPhaseCinematic());
  $("boss-special-cinematic-skip")?.addEventListener("click", () => stopPhaseCinematic());
  $("boss-intro-skip")?.addEventListener("click", () => stopBossIntro());
  bossIntro?.addEventListener("keydown", event => {
    if (event.key === "Escape") { event.preventDefault(); stopBossIntro(); }
  });
  specialCinematic?.addEventListener("keydown", event => {
    if (event.key === "Escape") { event.preventDefault(); stopPhaseCinematic(); }
  });
  starCinematic?.addEventListener("animationend", event => {
    if (event.target === starCinematic && event.animationName === "anatemis-curtain") stopPhaseCinematic();
  });
  starCinematic?.addEventListener("keydown", event => {
    if (event.key === "Escape") { event.preventDefault(); stopPhaseCinematic(); }
  });
  $("combat-view-toggle").addEventListener("click", () => { if (isActive()) switchView(true); });
  $("combat-sheet-button").addEventListener("click", () => switchView(false));
  $("combat-loot-keep").addEventListener("click", () => resolveLoot(true));
  $("combat-loot-discard").addEventListener("click", () => resolveLoot(false));
  document.querySelectorAll("[data-combat-close]").forEach(button => button.addEventListener("click", () => { setup.hidden = true; updateModalState(); $("combat-open-button").focus(); }));
  document.addEventListener("keydown", event => { if (event.key === "Escape" && !setup.hidden) { setup.hidden = true; updateModalState(); } });
  $("combat-setup-form").addEventListener("submit", event => { event.preventDefault(); if ($( "combat-start").disabled) return; start(enemyDefinitions[Number($("combat-enemy-select").value)], Number($("combat-enemy-quantity").value)); });
  $("combat-actions").addEventListener("click", event => { const button = event.target.closest("[data-action]"); if (button && !button.disabled) action(button.dataset.action); });
  $("combat-skills").addEventListener("click", event => { const button = event.target.closest("[data-skill]"); if (button && !button.disabled) action("skill", button.dataset.skill, $("combat-guess")?.value || null); });
  // Lament's second target: the spell is cast on the chosen target and this one.
  function castLamentOn(id) {
    const waiting = pendingLament;
    pendingLament = null;
    return action("spell", waiting.spellId, { extraTargetId: id });
  }
  function chooseTarget(event) {
    const button = event.target.closest("[data-combat-target]");
    if (!button || button.disabled || !isActive()) return;
    if (pendingLament) { castLamentOn(button.dataset.combatTarget); return; }
    battle.targetId = button.dataset.combatTarget;
    render(); saveCharacter();
    document.querySelector('[data-combat-target="' + battle.targetId + '"]')?.focus();
  }
  $("combat-opponents").addEventListener("click", chooseTarget);
  $("combat-player").addEventListener("click", chooseTarget);
  $("combat-player").addEventListener("keydown", event => {
    if (event.key !== "Enter" && event.key !== " ") return;
    event.preventDefault(); chooseTarget(event);
  });
  $("combat-spell-button").addEventListener("click", () => openPicker("spell"));
  $("combat-item-button").addEventListener("click", () => openPicker("item"));
  $("combat-reaction-use").onclick = () => chooseEquipmentReaction(true);
  $("combat-reaction-decline").onclick = $("combat-reaction-close").onclick = () => chooseEquipmentReaction(false);
  document.addEventListener("keydown", event => {
    if (!isActive() || reactionModal.hidden) return;
    if (event.key === "Escape") { event.preventDefault(); chooseEquipmentReaction(false); }
    if (event.key === "Tab") {
      const controls = [...reactionModal.querySelectorAll("button:not(:disabled)")];
      if (event.shiftKey && document.activeElement === controls[0]) { event.preventDefault(); controls.at(-1)?.focus(); }
      else if (!event.shiftKey && document.activeElement === controls.at(-1)) { event.preventDefault(); controls[0]?.focus(); }
    }
  });
  document.querySelectorAll("[data-combat-picker-close]").forEach(button => button.addEventListener("click", closePicker));
  $("combat-ability-list").addEventListener("click", event => {
    const apexButton = event.target.closest("[data-apex-choice]");
    if (apexButton && pickerKind === "apex" && pendingApex) {
      const choice = { apexChoice: apexButton.dataset.apexChoice, weatherChoiceId: pendingApex.weatherChoiceId };
      if (action(pendingApex.kind, pendingApex.id, choice)) closePicker();
      return;
    }
    const weatherButton = event.target.closest("[data-combat-weather]");
    if (weatherButton && pickerKind === "weather") {
      if (!beginApexChoice("spell", weatherSpellId, weatherButton.dataset.combatWeather) && action("spell", weatherSpellId, weatherButton.dataset.combatWeather)) closePicker();
      return;
    }
    // Lament: close the list and wait for the second target's click.
    const lamentButton = event.target.closest("[data-combat-lament]");
    if (lamentButton && !lamentButton.disabled && pickerKind === "spell") {
      const spellId = lamentButton.dataset.combatLament;
      closePicker();
      pendingLament = { spellId };
      $("combat-action-status").textContent = `Lament: choose a second target for ${getSpell(spellId)?.name || "the spell"} (Escape cancels).`;
      return;
    }
    const button = event.target.closest("[data-combat-ability]");
    if (!button || button.disabled) return;
    if (pickerKind === "spell" && spellEffects(getSpell(button.dataset.combatAbility)).some(effect => effect.type === "weather-control")) {
      weatherSpellId = button.dataset.combatAbility;
      pickerKind = "weather";
      renderPicker();
      return;
    }
    const selectedId = button.dataset.combatAbility;
    const archivedSpell = pickerKind === "item" && getItem(selectedId)?.archivedSpellId;
    if (pickerKind === "spell" || archivedSpell) {
      if (beginApexChoice(pickerKind, selectedId)) return;
    }
    if (action(pickerKind, selectedId)) closePicker();
  });
  document.addEventListener("keydown", event => {
    if (pendingLament && abilityPicker.hidden && event.key === "Escape") {
      pendingLament = null;
      $("combat-action-status").textContent = "Lament cancelled.";
      return;
    }
    if (abilityPicker.hidden) return;
    if (event.key === "Escape") { event.preventDefault(); closePicker(); }
    if (event.key === "Tab") {
      const controls = [...abilityPicker.querySelectorAll("button:not(:disabled)")];
      const first = controls[0], last = controls.at(-1);
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
    }
  });
  restore(character.combatState);
  synchronizeSourceFiles().then(async () => {
    if (!itemCatalog.consumables?.length && !itemCatalog.keyItems?.length) await loadConsumables();
    if (!battle) return;
    try { await loadEnemies(); } catch { /* Existing participants remain usable if source files are unavailable. */ }
    refresh();
  });
})();

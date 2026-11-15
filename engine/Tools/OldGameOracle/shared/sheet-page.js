/* The sheet's state in the old game's page, for the sheet modules (GM2 step 2). A case's input names the sheet in the
   port's words (actor.sheet: race, level, experience, freePoints, classPoints, attributes, spentPoints, bonuses, hp,
   mana, purse), Dev Mode, Limitless (GM15's cap), the enemies in a battle, a crafted race ("stubs.race", a race in the
   old file's form, id "stub-race", put in the catalogue through the old normalizeRaceDefinition: the old fold reads it
   as any race), and hand-made totals ("stubs") for what GM2 doesn't have yet: the artifacts and the worn equipment (GM3),
   the classes (GM8), the battle's overrides (GM5). The page's globals are set to it, the stubs put in place of the old
   functions they stand for, then the old code runs as the sheet runs it. Every function is the old game's own except
   the stubs, whose formulas the port's tests repeat. */
const SHEET_KEYS = ["strength", "vitality", "mind", "dexterity", "intelligence", "wisdom", "speed", "luck"];
const sheetEach = make => Object.fromEntries(SHEET_KEYS.map(key => [key, make(key)]));
const sheetOriginal = {
  functions: { calculateArtifactEffects, calculateClassEffects, calculateEquipmentDefense, equippedDefenseItems, equippedDefenseCap, wieldsTurnAndSpeedLock },
  combat: { resourceMaximum: Combat.resourceMaximum, permanentStatPercent: Combat.permanentStatPercent, permanentDefensePercent: Combat.permanentDefensePercent, statValue: Combat.statValue, defense: Combat.defense },
  races: [...raceCatalog],
};

function sheetRestore() {
  ({ calculateArtifactEffects, calculateClassEffects, calculateEquipmentDefense, equippedDefenseItems, equippedDefenseCap, wieldsTurnAndSpeedLock } = sheetOriginal.functions);
  Object.assign(Combat, sheetOriginal.combat);
  raceCatalog = [...sheetOriginal.races];
}

// The crafted race in the catalogue; the stubs, each a plain formula of its numbers.
function sheetStubs(stubs) {
  sheetRestore();
  if (stubs.race) raceCatalog = [...raceCatalog, normalizeRaceDefinition(stubs.race, raceCatalog.length)];
  const artifacts = stubs.artifacts;
  if (artifacts) calculateArtifactEffects = () => {
    const totals = emptyArtifactEffects();
    for (const key of SHEET_KEYS) {
      totals.statFlat[key] += artifacts.statFlat?.[key] ?? 0;
      totals.statPercent[key] += artifacts.statPercent?.[key] ?? 0;
      totals.statPermanentPercent[key] += artifacts.statPermanentPercent?.[key] ?? 0;
    }
    for (const field of ["maxHpFlat", "maxHpPercent", "maxManaFlat", "maxManaPercent"]) totals[field] += artifacts[field] ?? 0;
    return totals;
  };
  const classes = stubs.classes;
  if (classes) calculateClassEffects = () => {
    const totals = emptyClassEffects();
    for (const key of SHEET_KEYS) totals.statPermanentPercent[key] += classes.statPermanentPercent?.[key] ?? 0;
    for (const key of ["armor", "evasion"]) {
      totals.defensePermanentPercent[key] += classes.defensePermanentPercent?.[key] ?? 0;
      totals.defenseFlat[key] += classes.defenseFlat?.[key] ?? 0;
      if (classes.defenseCap?.[key] != null) totals.defenseCap[key] = classes.defenseCap[key];
    }
    totals.maxHpPercent += classes.maxHpPercent ?? 0;
    totals.maxManaPercent += classes.maxManaPercent ?? 0;
    return totals;
  };
  const equipment = stubs.equipment;
  if (equipment) {
    // Its Armor may grow with the current Strength (a forged piece's power reads the attributes).
    calculateEquipmentDefense = values => ({ armor: (equipment.armor ?? 0) + Math.floor(values.strength * (equipment.armorPerStrength ?? 0)), evasionDebuff: equipment.evasionDebuff ?? 0, evasionFlat: equipment.evasionFlat ?? 0, evasionPercent: equipment.evasionPercent ?? 0 });
    equippedDefenseItems = () => [{ hpPercent: equipment.hpPercent ?? 0 }];
    equippedDefenseCap = key => equipment.defenseCap?.[key] ?? Infinity;
    wieldsTurnAndSpeedLock = () => equipment.speedLock === true;
  }
  const battle = stubs.battle;
  if (battle) Object.assign(Combat, {
    resourceMaximum: kind => battle.resourceMaximum?.[kind] ?? null,
    permanentStatPercent: key => battle.permanentStatPercent?.[key] ?? 0,
    permanentDefensePercent: key => battle.permanentDefensePercent?.[key] ?? 0,
    statValue: (key, base) => base + (battle.statPlus?.[key] ?? 0),
    defense: (key, base) => base * 2 + (battle.defensePlus?.[key] ?? 0),
  });
}

// The page's globals from a case's input; the stat cards and the purse's fields made again for them.
function sheetSet(input) {
  const sheet = input.sheet;
  sheetStubs(input.stubs || {});
  selectedRaceId = sheet.race;
  appearance = sheet.race ? { gender: "male", variant: 0 } : null;
  level = sheet.level;
  exp = sheet.experience;
  availablePoints = sheet.freePoints;
  perkPoints = sheet.classPoints;
  stats = { ...sheet.attributes };
  manualAllocations = { ...sheet.spentPoints };
  permanentBonuses = sheetEach(key => sheet.bonuses[key]);
  permanentDefenseBonuses = { armor: sheet.bonuses.armor, evasion: sheet.bonuses.evasion };
  currentHp = sheet.hp;
  currentMana = sheet.mana;
  currencies = { ...sheet.purse };
  editingEnabled = input.devMode === true;
  enemyCount = input.enemyCount ?? 0;
  limitless = LimitlessRules.normalize(input.limitless ? { unlocked: true, active: true } : null);
  equipment = { ...defaultEquipment };
  classLevels = {};
  createStatCards();
  document.querySelectorAll(".currency-input").forEach(field => { field.value = currencies[field.dataset.currency]; });
}

// The sheet as the port's record holds it.
function sheetSnapshot() {
  return {
    race: selectedRaceId, level, experience: exp, freePoints: availablePoints, classPoints: perkPoints,
    attributes: sheetEach(key => stats[key]), spentPoints: sheetEach(key => manualAllocations[key]),
    bonuses: { ...sheetEach(key => permanentBonuses[key]), armor: permanentDefenseBonuses.armor, evasion: permanentDefenseBonuses.evasion },
    hp: currentHp, mana: currentMana, purse: { silver: currencies.silver, gold: currencies.gold, platinum: currencies.platinum },
  };
}

// Which of the sheet's controls work (updateEditingControls: Dev Mode's and the free points').
function sheetControls() {
  updateEditingControls();
  const first = selector => document.querySelector(selector);
  return {
    attributeFields: !first("#stats-grid .stat-input").disabled,
    raise: !first('#stats-grid .stat-stepper[data-action="increase"]').disabled,
    lower: !first('#stats-grid .stat-stepper[data-action="decrease"]').disabled,
    bonusFields: !first("#stats-grid .stat-bonus-input").disabled,
    defenseBonusFields: !first(".defense-bonus-input").disabled,
    purse: !first(".currency-input").disabled,
    respec: !first("#respec-button").disabled,
    devOnly: !first("[data-dev-only]").hidden,
  };
}

// What the sheet shows for the state (updateDerivedValues, which also holds HP and Mana to their maxima).
function sheetValues() {
  // The EXP bar's width as the old code writes it (`${expPercent}%`, its number whole: the CSSOM would shorten it when
  // read back), caught by a setter on the bar's own style for the one call.
  const bar = document.getElementById("exp-bar");
  let barWidth = null;
  Object.defineProperty(bar, "style", { configurable: true, get: () => ({ set width(value) { barWidth = value; } }) });
  try { updateDerivedValues(); } finally { delete bar.style; }
  const current = currentStats();
  const text = id => document.getElementById(id).textContent;
  return {
    baseAttributes: sheetEach(key => displayedBaseStat(key)),
    currentAttributes: sheetEach(key => current[key]),
    bonuses: { ...sheetEach(key => effectivePermanentBonus(key)), armor: effectivePermanentDefenseBonus("armor"), evasion: effectivePermanentDefenseBonus("evasion") },
    maxHp: calculateMaxHpWithArtifacts(current),
    maxMana: calculateMaxManaWithEffects(current),
    armor: { base: Number(text("armor-base-value")), modified: Number(text("armor-modified-value")), current: Number(text("armor-value")) },
    evasion: { base: Number(text("evasion-base-value")), modified: Number(text("evasion-modified-value")), current: Number(text("evasion-value")) },
    hp: currentHp,
    mana: currentMana,
    levelCap: LimitlessRules.cap(limitless.active),
    experienceRequired: CombatRules.experienceRequired(level),
    levelUpEnabled: !document.getElementById("level-up-button").disabled,
    levelUpAlert: document.getElementById("level-icon").classList.contains("has-level-up"),
    // The texts and the bar as the page shows them (the sheet's screen, GM2 step 7).
    texts: {
      cap: document.querySelector(".level-card .level-cap").textContent, threshold: text("exp-threshold"), helper: text("exp-helper"),
      barWidth,
      valueMax: document.getElementById("exp-track").getAttribute("aria-valuemax"), valueNow: document.getElementById("exp-track").getAttribute("aria-valuenow"),
    },
    controls: sheetControls(),
  };
}

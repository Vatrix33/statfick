window.runCriticalChecks = async function () {
  const assert = (condition, message) => { if (!condition) throw new Error(message); };
  const rules = CombatRules;
  assert(rules.weaponCritChance(0) === 5, "Base crit chance should be 5%");
  assert(rules.weaponCritChance(30) === 9, "Luck should add 0.15% crit per point, rounded down");
  assert(rules.weaponCritChance(30, 20) === 29, "Flat crit bonuses should add to the Luck-based chance");
  assert(rules.weaponCritChance(1000) === 40, "Ordinary crit chance should cap at 40%");
  assert(rules.weaponCritChance(5, -20) === 0, "Crit chance should not become negative");
  assert(rules.damage(100, 100, 20) === 26, "Normal armor and resistance mitigation changed");
  assert(rules.damage(100, 100, 20, true) === 33, "Crits should add 25% damage without innate armor pierce");
  assert(rules.damage(100, 100, 20, true, 25) === 40, "Crit-damage class bonuses should add to, not multiply, base crit damage");
  assert(rules.damage(100, 100, 0, true, 100) === 58, "Crit-damage bonuses should cap at 75% extra");
  assert(rules.damage(100, 100, 20, false, 100) === 26, "Crit-damage bonuses must not affect regular hits");

  const weapon = getItem("weapon-forged-longsword-30-dwiro-rtoak-dwoak-s0");
  const statsA = { strength: 10, dexterity: 10, luck: 30 };
  const statsB = { strength: 200, dexterity: 200, luck: 30 };
  assert(calculateWeaponHitChance(weapon, statsB) > calculateWeaponHitChance(weapon, statsA), "Accuracy test setup did not change hit chance");
  assert(calculateWeaponCritChance(weapon, statsA) === calculateWeaponCritChance(weapon, statsB), "Weapon accuracy must not affect crit chance");
  assert(calculateWeaponCritChance(weapon, { ...statsA, luck: 100 }) > calculateWeaponCritChance(weapon, statsA), "Luck should improve weapon crit chance");
  assert(equipmentItemStats(weapon).some(entry => entry.label === "Crit chance" && entry.value === calculateWeaponCritChance(weapon) + "%"), "Displayed crit chance must match combat calculation");
};

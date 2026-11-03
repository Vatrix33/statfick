/* Pure combat rules, shared by the battle view and its regression tests. */
(function (root) {
  const clamp = (value, min, max) => Math.min(max, Math.max(min, value));
  const chance = value => clamp(Number(value) || 0, 0, 100);
  // Accuracy against Evasion: hit chance = Accuracy / (Accuracy + Evasion), so an
  // attacker with twice the target's Evasion hits two times in three.
  function weaponHitChance(accuracy, evasion = 0) {
    const attack = Math.max(0, Number(accuracy) || 0), defense = Math.max(0, Number(evasion) || 0);
    if (attack + defense <= 0) return 100;
    return clamp(Math.round(100 * attack / (attack + defense)), 1, 100);
  }
  /* Critical hits. Every source adds Crit Rating: the weapon's base, 0.15 per Luck,
     and class, artifact and equipment bonuses. Rating becomes a chance on a
     smooth curve with no hard cap: early points count almost fully, later ones a
     little less, and only guaranteed-crit skills reach 100%.
       rating 10 → 10%   rating 25 → 22%   rating 50 → 39%
       rating 100 → 63%  rating 150 → 78%  rating 200 → 86%
     Crits are a separate roll after a hit; accuracy and target Evasion do not
     improve or suppress the chance a landed hit finds a weak point. */
  const CRIT_CURVE = 100;
  const critChanceFromRating = rating => clamp(Math.round(100 * (1 - Math.exp(-Math.max(0, Number(rating) || 0) / CRIT_CURVE))), 0, 99);
  const weaponCritChance = (luck = 0, bonus = 0, base = 5) => critChanceFromRating((Number(base) || 0) + Math.max(0, Number(luck) || 0) * 0.15 + (Number(bonus) || 0));
  /* Crit momentum: each landed hit that does not crit makes the next one more
     likely, and a crit resets it (a pseudo-random distribution). Over many hits
     the rate equals the shown chance, but crits arrive steadily instead of in
     streaks and droughts. */
  const momentumSteps = new Map();
  function momentumStep(chance) {
    const percent = clamp(Math.round(Number(chance) || 0), 0, 100);
    if (momentumSteps.has(percent)) return momentumSteps.get(percent);
    const rateFor = step => {
      let none = 1, expected = 0;
      for (let attempt = 1; none > 1e-9; attempt++) {
        const p = Math.min(1, step * attempt);
        expected += attempt * none * p;
        none *= 1 - p;
      }
      return 1 / expected;
    };
    let low = 0, high = percent / 100;
    if (percent > 0 && percent < 100) for (let iteration = 0; iteration < 40; iteration++) {
      const middle = (low + high) / 2;
      if (rateFor(middle) < percent / 100) low = middle; else high = middle;
    }
    const step = percent >= 100 ? 1 : (low + high) / 2;
    momentumSteps.set(percent, step);
    return step;
  }
  // The chance for the next landed hit after this many hits without a crit.
  const critChanceAfterMisses = (chance, misses = 0) => chance <= 0 ? 0 : chance >= 100 ? 100 : Math.min(100, momentumStep(chance) * (Math.max(0, Math.floor(Number(misses) || 0)) + 1) * 100);
  const critRoll = (chance, misses = 0, random = Math.random) => random() * 100 < critChanceAfterMisses(chance, misses);
  const criticalMultiplier = (critical = false, bonusPercent = 0) => critical ? clamp(1.25 + (Number(bonusPercent) || 0) / 100, 1, 1.75) : 1;
  function nextTurn(actors, speedOf) {
    const ready = actors.filter(actor => actor.alive !== false);
    let next = null, elapsed = Infinity;
    for (const actor of ready) {
      const speed = Math.max(0, Number(speedOf(actor)) || 0);
      const wait = actor.counter >= 1000 - 1e-7 ? 0 : speed > 0 ? (1000 - actor.counter) / speed : Infinity;
      if (wait < elapsed - 1e-7) { next = actor; elapsed = wait; }
    }
    if (!next) return null;
    for (const actor of ready) actor.counter += Math.max(0, Number(speedOf(actor)) || 0) * elapsed;
    next.counter = Math.max(0, next.counter - 1000);
    return next;
  }
  function damage(power, armor, resistance = 0, critical = false, criticalBonusPercent = 0) {
    const resisted = Math.max(0, power) * criticalMultiplier(critical, criticalBonusPercent) * (1 - clamp(resistance, -100, 100) / 100);
    if (resisted <= 0) return 0;
    return Math.max(1, Math.floor(resisted * 100 / (100 + Math.max(0, armor) * 2)));
  }
  function trapDamage(maxHp, cap, percent = 10) {
    const maximum = Math.max(0, Number(maxHp) || 0);
    if (!maximum) return 0;
    return Math.min(Math.max(0, Number(cap) || 0), Math.max(1, Math.floor(maximum * Math.max(0, Number(percent) || 0) / 100)));
  }
  function runeReduction(damageAmount, percent = 10) {
    const damage = Math.max(0, Math.floor(Number(damageAmount) || 0));
    const reducedDamage = Math.floor(damage * (1 - clamp(Number(percent) || 0, 0, 100) / 100));
    return { damage: reducedDamage, prevented: damage - reducedDamage };
  }
  function resourceRestore(flatAmount, maximum, percent = 0) {
    return Math.max(0, Math.floor((Number(flatAmount) || 0) + Math.max(0, Number(maximum) || 0) * Math.max(0, Number(percent) || 0) / 100));
  }
  function trapLimit(base = 1, ...modifiers) {
    const added = modifiers.reduce((sum, modifier) => sum + (Number(modifier) || 0), 0);
    return Math.max(0, Math.floor((Number(base) || 0) + added));
  }
  // The EXP a level needs: an enemy of that level's EXP times the victories the curve below asks for.
  function experienceRequired(level) {
    return enemyExperience(level) * sameLevelVictories(level);
  }
  // Victories over enemies of one's own level per level-up (user decision 2026-11-03): a fast start,
  // 3–6 up to level 12 (51 to reach it), then a curve rising to 24 at level 99 (1265 to reach 100).
  // The EXP targets follow it; enemies' EXP stays as it was.
  const EARLY_VICTORIES = [3, 3, 4, 4, 4, 5, 5, 5, 6, 6, 6];
  function sameLevelVictories(level) {
    const enemyLevel = Math.max(1, Math.floor(Number(level) || 1));
    if (enemyLevel <= EARLY_VICTORIES.length) return EARLY_VICTORIES[enemyLevel - 1];
    const t = Math.min(1, (enemyLevel - 12) / 87);
    return Math.round(7 + 17 * Math.pow(t, 1.5));
  }
  // An enemy's EXP, kept as it was before the 2026-11-03 curve (user decision): (level + 1) × 100 over
  // 5 (level 1), 6 (level 2), then 8 rising by one every ten levels to 12 from level 43.
  function enemyExperience(level) {
    const enemyLevel = Math.max(1, Math.floor(Number(level) || 1));
    const share = enemyLevel === 1 ? 5 : enemyLevel === 2 ? 6 : Math.min(12, 8 + Math.floor((enemyLevel - 3) / 10));
    return Math.ceil((enemyLevel + 1) * 100 / share);
  }
  function meditate(maxMana, mind, luck) { return Math.max(0, Math.floor(maxMana * 0.2 + mind * 0.5 + luck * 0.2)); }
  function escape(speed, luck, enemySpeed) { return Math.max(5, chance(50 + speed * 0.5 + luck * 0.2 - enemySpeed * 0.4)); }
  const rules = { nextTurn, damage, criticalMultiplier, trapDamage, runeReduction, resourceRestore, trapLimit, experienceRequired, sameLevelVictories, enemyExperience, meditate, escape, chance, weaponHitChance, weaponCritChance, critChanceFromRating, critChanceAfterMisses, critRoll, CRIT_CURVE, roll: (percent, random = Math.random) => random() * 100 < chance(percent) };
  root.CombatRules = rules;
  if (typeof module !== "undefined") module.exports = rules;
})(globalThis);

async function runTrapChecks() {
  const wait = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));
  const assert = (value, message) => { if (!value) throw new Error(message); };
  async function begin(itemId, enemyName, carriedItems = [itemId], characterChanges = {}) {
    Combat.restore(null);
    applyCharacter(normalizeCharacter({ ...defaultCharacter(), ...characterChanges, inventory: carriedItems }));
    document.getElementById("combat-open-button").click();
    for (let index = 0; index < 120 && document.getElementById("combat-start").disabled; index += 1) await wait(25);
    const select = document.getElementById("combat-enemy-select");
    const option = [...select.options].find(entry => entry.textContent === enemyName);
    assert(option, "Missing test enemy " + enemyName);
    select.value = option.value;
    document.getElementById("combat-enemy-quantity").value = 1;
    document.getElementById("combat-setup-form").requestSubmit();
    let state = Combat.serialize();
    state.current = "player"; state.ap = 5; state.targetId = "player";
    Combat.restore(state); document.getElementById("combat-view-toggle").click();
    document.getElementById("combat-item-button").click();
    const use = document.querySelector(`[data-combat-ability="${itemId}"]`);
    assert(use && !use.disabled, itemId + " can be placed");
    use.click();
    return Combat.serialize();
  }
  async function enemyAttack(state) {
    state.current = "enemy-1"; state.ap = 2; state.targetId = "player";
    Combat.restore(state); document.getElementById("combat-view-toggle").click();
    await wait(800);
    return Combat.serialize();
  }

  assert(CombatRules.trapLimit() === 1 && CombatRules.trapLimit(1, 1) === 2, "Trap limit supports additive increases");
  const classTotals = emptyClassEffects();
  addClassEffect({ type: "trap-limit-flat", amount: 1 }, classTotals);
  assert(classTotals.trapLimitFlat === 1, "Classes can increase the trap limit");
  assert(parseArtifactEffects("trap-limit-flat:1")[0]?.amount === 1, "Artifacts can increase the trap limit");

  const raisedLimitClass = normalizeClassDefinition({ id: "test-trapper", name: "Test Trapper", maxLevel: 1, levels: [{ level: 1, effects: [{ type: "trap-limit-flat", amount: 1 }] }] });
  classCatalog.push(raisedLimitClass);
  let raisedState = await begin("consumable-spring-trap", "Goblin", ["consumable-spring-trap", "consumable-sentry-trap"], { classLevels: { "test-trapper": 1 } });
  document.getElementById("combat-item-button").click();
  const allowedTrap = document.querySelector('[data-combat-ability="consumable-sentry-trap"]');
  assert(allowedTrap && !allowedTrap.disabled && document.getElementById("combat-picker-resources").textContent.includes("Traps 1/2"), "A trap-limit modifier permits a second active trap");
  allowedTrap.click();
  raisedState = Combat.serialize();
  assert(raisedState.actors[0].effects.filter(effect => effect.type.startsWith("trap-")).length === 2, "The raised limit is enforced by combat placement");
  classCatalog.splice(classCatalog.indexOf(raisedLimitClass), 1);

  let state = await begin("consumable-spring-trap", "Goblin", ["consumable-spring-trap", "consumable-sentry-trap"]);
  assert(state.ap === 4 && state.actors[0].effects.some(effect => effect.type === "trap-spring"), "Spring Trap arms for 1 AP");
  document.getElementById("combat-item-button").click();
  const blockedTrap = document.querySelector('[data-combat-ability="consumable-sentry-trap"]');
  assert(blockedTrap?.disabled && blockedTrap.textContent.includes("Trap limit reached (1/1)"), "A second active trap is blocked at the base limit");
  assert(document.getElementById("combat-picker-resources").textContent.includes("Traps 1/1"), "The item picker shows current trap capacity");
  document.querySelector("#combat-ability-picker .modal-close").click();
  let enemyHp = state.actors[1].character.currentHp;
  const springDamage = CombatRules.trapDamage(state.actors[1].maxHp, 20, 10);
  Math.random = () => .9999;
  state = await enemyAttack(state);
  assert(!state.actors[0].effects.some(effect => effect.type === "trap-spring"), "Spring Trap is consumed by a melee attack");
  assert(state.actors[1].character.currentHp === enemyHp - springDamage, "Spring Trap deals capped maximum-HP damage even when the attack misses");

  state = await begin("consumable-spell-rune-trap", "Goblin Archer");
  assert(state.actors[0].effects.some(effect => effect.type === "trap-spell-rune"), "Spell Rune Trap arms");
  enemyHp = state.actors[1].character.currentHp;
  Math.random = () => 0;
  state = await enemyAttack(state);
  const runeLog = state.log.findLast(entry => entry.includes("Spell Rune Trap prevents"));
  const prevented = Number(runeLog?.match(/prevents (\d+)/)?.[1]);
  assert(prevented > 0 && state.actors[1].character.currentHp === enemyHp - prevented, "Spell Rune Trap reflects exactly the prevented damage");
  assert(!state.actors[0].effects.some(effect => effect.type === "trap-spell-rune"), "Spell Rune Trap is consumed after reducing damage");

  state = await begin("consumable-sentry-trap", "Goblin");
  assert(state.actors[0].effects.some(effect => effect.type === "trap-sentry"), "Sentry Trap can target the player");
  enemyHp = state.actors[1].character.currentHp;
  const sentryDamage = CombatRules.trapDamage(state.actors[1].maxHp, 25, 10);
  Math.random = () => .9999;
  state = await enemyAttack(state);
  assert(!state.actors[0].effects.some(effect => effect.type === "trap-sentry"), "Sentry Trap is consumed by an attack");
  assert(state.actors[1].character.currentHp === enemyHp - sentryDamage, "Sentry Trap deals capped maximum-HP damage even when the attack misses");
  Combat.restore(null);
  return { passed: true, springDamage, prevented, sentryDamage };
}

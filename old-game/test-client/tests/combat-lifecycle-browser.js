/* Only artificial test characters in the invisible runner's fresh profile. */
async function runLifecycleMeasurements(options = {}) {
  const savedRandom = Math.random;
  await waitForSourceFiles(); Combat.restore(null); WorldMap.setAmbientEnabled(false);
  GameSettings.setCombatAnimations(false);
  const report = { samples: [], states: [], coldEnemyLoadMs: null }, original = {}, phase = { current: null };
  const functions = ['currentStatsUncached', 'calculateClassEffectsUncached', 'calculateArtifactEffectsUncached', 'calculateRaceEffectsUncached', 'calculateEquipmentDefense', 'updateDerivedValues', 'renderClasses', 'renderKnownSpells', 'renderSkills', 'renderPassives', 'saveCharacter'];
  for (const name of functions) {
    original[name] = window[name];
    window[name] = function (...args) {
      if (!phase.current) return original[name].apply(this, args);
      const entry = phase.current.functions[name] ||= { calls: 0, ms: 0 };
      entry.calls++; const started = performance.now();
      try { return original[name].apply(this, args); } finally { entry.ms += performance.now() - started; }
    };
  }
  async function measure(name, run) {
    const entry = { name, functions: {} }; phase.current = entry; const started = performance.now();
    try { await run(entry); } finally { entry.ms = performance.now() - started; phase.current = null; report.samples.push(entry); }
    return entry;
  }
  const characterFor = kind => {
    if (kind === 'endgame') return structuredClone(window.__lifecycleFixture.character);
    const c = defaultCharacter(); c.raceId = 'human'; c.level = 10; c.editingEnabled = false;
    c.stats = { ...c.stats, strength: 150, dexterity: 80, vitality: 80, mind: 80, speed: 1000 };
    c.inventory = ['weapon-forged-longsword-30-dwiro-hfoak-elque-s0', 'armor-forged-leather-armor-30-elraw-bbraw-hfraw-s0'];
    c.equipment = { ...defaultEquipment, primaryHand: c.inventory[0], armor: c.inventory[1] };
    c.classLevels = { warrior: 5, mage: 5 }; c.currentHp = c.currentMana = 5000;
    c.completedClassChoices = ['mage:mage-starting-spells']; c.pendingClassSpellChoice = null;
    c.knownSpells = spells.filter(s => s.tier === 1 && s.catalogueAvailable !== false && !s.requiredCasterEnemyId).slice(0, 9).map(s => s.id);
    return c;
  };
  function fresh(kind) {
    Combat.restore(null); document.querySelectorAll('.modal-layer').forEach(node => { node.hidden = true; }); updateModalState();
    const c = characterFor(kind); c.combatState = null; c.worldState = defaultCharacter().worldState;
    applyCharacter(normalizeCharacter(c)); WorldMap.setAmbientEnabled(false);
  }
  function snapshot(name) {
    report.states.push(structuredClone({ name, battle: Combat.serialize(), player: {
      currentHp, currentMana, exp, currencies, classLevels, activeSkills,
      classProgress, companions: structuredClone(companions), inventory: [...inventory],
    }, forecast: document.getElementById('combat-turn-order').textContent }));
  }
  try {
    fresh('regular');
    const cold = performance.now(); await Combat.enemyDropSources(); report.coldEnemyLoadMs = performance.now() - cold;
    for (const kind of ['regular', 'endgame']) for (const count of [1, 5]) for (let repeat = 0; repeat < (options.repeats || 3); repeat++) {
      let seed = 1753;
      const initialRandom = Math.random;
      Math.random = () => ((seed = (1664525 * seed + 1013904223) >>> 0) / 4294967296);
      fresh(kind);
      const label = `${kind}-${count}-enemies-${repeat + 1}`;
      const started = await measure(label + '-start', async entry => {
        if (!await Combat.startEncounter(Array(count).fill('Goblin'))) throw Error('Could not start fixture');
        entry.actors = Combat.serialize().actors.length;
      });
      snapshot(label + '-start');
      if (document.getElementById('combat-actions').querySelector('[data-action="attack"]').disabled) throw Error('Fixture needs a controlled opening turn');
      Math.random = () => .01;
      try {
        await measure(label + '-defeat', entry => {
          document.querySelector('#combat-actions [data-action="attack"]').click();
          const state = Combat.serialize(); entry.deadEnemies = state.actors.filter(a => a.team === 'enemy' && a.character.currentHp <= 0).length;
          entry.awarded = state.actors.filter(a => a.team === 'enemy' && a.experienceAwarded).length;
          entry.result = state.result;
          if (!entry.deadEnemies || entry.deadEnemies !== entry.awarded) throw Error('Fixture did not defeat and reward an enemy');
        });
        snapshot(label + '-defeat');
      } finally { Math.random = initialRandom; }
      Combat.restore(null);
    }
    if (options.save !== false) await statFickDesktop.saveCharacter(JSON.stringify({ stats: defaultStats, lifecycle: report }));
    return report;
  } finally {
    Math.random = savedRandom; phase.current = null; for (const name of functions) window[name] = original[name]; Combat.restore(null);
  }
}

async function runLifecycleChecks() {
  const assert = (condition, message) => { if (!condition) throw new Error(message); };
  assert(typeof window.__lifecycleFixture === 'object', 'The lifecycle runner must inject its artificial party fixture');
  assert(Combat.cacheCalculation.toString().includes('__disableCombatCache'), 'Use the test runner lifecycle suite (or --compare) for a real uncached comparison');
  const originalRandom = Math.random;
  try {
    window.__disableCombatCache = false;
    const optimized = await runLifecycleMeasurements({ save: false });
    window.__disableCombatCache = true;
    const uncached = await runLifecycleMeasurements({ repeats: 1, save: false });
    window.__disableCombatCache = false;
    for (const state of uncached.states) {
      const match = optimized.states.find(entry => entry.name === state.name);
      assert(JSON.stringify(match) === JSON.stringify(state), state.name + ': cached calculations changed combat, resources, rewards, or the turn forecast');
    }
    const c = defaultCharacter(); c.raceId = 'human'; c.level = 25;
    c.stats.speed = 40; c.classLevels = { soldier: 6 }; c.currentHp = c.currentMana = 1000;
    applyCharacter(normalizeCharacter(c)); WorldMap.setAmbientEnabled(false);
    assert(await Combat.startEncounter(['Goblin', 'Goblin', 'Goblin']), 'Forecast fixture starts');
    document.getElementById('combat-sheet-button').click();
    const fixture = Combat.serialize(), actor = fixture.actors[0];
    actor.effects.push({ type: 'stat-percent', target: 'speed', amount: 150, name: 'Test Haste', expiresTurn: actor.turns + 2 }, { type: 'stun', name: 'Test Stun', expiresTurn: actor.turns + 1 });
    fixture.actors[1].effects.push({ type: 'stat-percent', target: 'speed', amount: 200, name: 'Enemy Haste', expiresTurn: 2 });
    const summon = structuredClone(fixture.actors[2]);
    Object.assign(summon, { id: 'summon-lifecycle', name: 'Test Summon', team: 'player', summoned: true, summonerId: 'player', summonDurationTurns: 2, counter: 900 });
    summon.character.classLevels = { bard: 5 }; fixture.actors.push(summon);
    const forecastChecks = [];
    for (const variant of ['expiring buffs and summon', 'dead aura source']) {
      const state = structuredClone(fixture);
      if (variant === 'dead aura source') state.actors.at(-1).character.currentHp = 0;
      const results = [];
      for (const disabled of [false, true]) {
        window.__disableCombatCache = disabled; Combat.restore(state);
        results.push({ state: Combat.serialize(), forecast: document.getElementById('combat-turn-order').textContent, friendlies: document.getElementById('combat-player').textContent });
      }
      assert(JSON.stringify(results[0]) === JSON.stringify(results[1]), variant + ': a frame reused stats across turn-start effects or a death');
      assert(results[0].state.actors[0].turns === state.actors[0].turns && results[0].state.actors[0].effects.length === state.actors[0].effects.length, 'Forecasts must not expire real buffs or advance real turns');
      forecastChecks.push(variant);
    }
    window.__disableCombatCache = false; Combat.restore(null);
    const originalCatalogue = itemCatalog, originalSpells = spells;
    try {
      if (!itemCatalog.weapons.length) itemCatalog = { ...itemCatalog, weapons: parseItemFile("weapons", JSON.stringify([{ id: "weapon-lifecycle-blade-meele", name: "Lifecycle Blade", type: "meele", handed: 1, basePower: 10, primary: { stat: "strength", multiplier: 1 }, secondary: { stat: "dexterity", multiplier: 0.5 }, effects: [], description: "A catalogue fixture.", rarity: "common", shopAvailable: false, automaticDrop: false, priceSilver: 10 }])) };
      const first = itemCatalog.weapons[0], edited = { ...first, basePower: first.basePower + 7 };
      itemCatalog = { ...itemCatalog, weapons: [edited, ...itemCatalog.weapons.slice(1)], armor: [{ ...edited, basePower: 999 }, ...itemCatalog.armor] };
      rebuildItems();
      assert(getItem(first.id) === edited, 'Catalogue indexing must retain first-ID-wins semantics');
      assert(items.every(item => getItem(item.id) === item), 'Every item and Archived consumable resolves to the current catalogue entry');
      itemCatalog = { ...originalCatalogue, weapons: originalCatalogue.weapons.slice(1) }; rebuildItems();
      assert(!getItem(first.id), 'Removed JSON items must not remain in the lookup index');
      if (spells.length) {
        spells = [{ ...spells[0], name: 'Updated lifecycle spell' }, ...spells.slice(1)]; rebuildItems();
        assert(getItem('consumable-archived-' + spells[0].id)?.name === 'Archived: Updated lifecycle spell', 'Spell refresh must replace derived Archived items too');
      }
    } finally { itemCatalog = originalCatalogue; spells = originalSpells; rebuildItems(); }
    return { passed: true, optimized, uncached, forecastChecks, catalogueRefresh: true };
  } finally { Math.random = originalRandom; window.__disableCombatCache = false; Combat.restore(null); }
}

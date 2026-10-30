/* Run in the isolated headless test app, never a player's profile. */
async function runMapSpeedChecks() {
  let checks = 0;
  const assert = (ok, message) => { if (!ok) throw Error(message); checks++; };
  const $ = id => document.getElementById(id), wait = ms => new Promise(resolve => setTimeout(resolve, ms));
  const key = (value, options = {}, target = document) => target.dispatchEvent(new KeyboardEvent('keydown', { key: value, bubbles: true, cancelable: true, ...options }));
  const originalRoll = EncounterRules.roll;
  const main = defaultCharacter(); main.raceId = 'human'; main.stats.vitality = 1000; main.stats.mind = 1000; main.stats.speed = 2000;
  applyCharacter(normalizeCharacter(main)); Combat.restore(null); WorldMap.setAmbientEnabled(false); WorldMap.open();
  const ids = ['map-speed-half', 'map-speed-normal', 'map-speed-fast', 'map-speed-double'], speeds = [0.5, 1, 1.5, 2];
  const selected = speed => ids.every((id, index) => $(id).getAttribute('aria-pressed') === String(speeds[index] === speed));
  const move = (point, regionId = 'alder-vale') => {
    WorldMap.pause(false);
    worldState = WorldRules.normalize({ ...worldState, regionId, position: point, destination: null, route: [], pendingEncounter: null, pendingRegionTransition: null });
    WorldRules.setActiveRegion(regionId); WorldMap.resetView();
  };
  const measurements = [];
  try {
    EncounterRules.roll = () => null;
    assert(worldState.timeSpeed === 1 && selected(1), 'Regular speed is selected for a new character');
    assert(document.querySelectorAll('.map-time-speeds button').length === 4, 'Exactly four speed controls exist under the map clock');
    const controls = document.querySelector('.map-time-speeds');
    assert(controls.getBoundingClientRect().top >= $('map-clock').getBoundingClientRect().bottom, 'Speed controls are below the time');
    const initial = JSON.stringify(worldState);
    $('map-speed-double').click();
    assert(worldState.timeSpeed === 2 && selected(2), 'The double-speed button selects and highlights 2×');
    const unchanged = { ...worldState, timeSpeed: 1 };
    assert(JSON.stringify(unchanged) === initial, 'Selecting a speed does not move the character or advance time');
    assert(backupPayload().character.worldState.timeSpeed === 2, 'The selected speed is included in character saves');
    const saved = backupPayload().character;
    applyCharacter(normalizeCharacter(defaultCharacter()));
    assert(worldState.timeSpeed === 1 && selected(1), 'A new character returns to regular speed');
    applyCharacter(normalizeCharacter(saved));
    assert(worldState.timeSpeed === 2 && selected(2), 'Loading restores the speed selection and its highlight');

    for (let index = 0; index < speeds.length; index++) {
      assert($(ids[index]).getAttribute('aria-keyshortcuts') === 'Shift+' + (index + 1), 'Every speed button has an accessible shortcut');
      key(['!', '"', '§', '$'][index], { code: 'Digit' + (index + 1), shiftKey: true });
      assert(worldState.timeSpeed === speeds[index] && selected(speeds[index]), 'Shift+' + (index + 1) + ' selects its speed independently of keyboard layout');
      const before = worldState.timeSpeed;
      key('!', { code: 'Digit1', shiftKey: true, ctrlKey: true });
      key('!', { code: 'Digit1', shiftKey: true }, $('map-track-enemy'));
      assert(worldState.timeSpeed === before, 'Modified hotkeys and typing in fields do not change speed');
    }
    key('1'); assert(WorldMap.isTravelling() && worldState.destination.x === 420, 'Unmodified settlement shortcuts still travel instead of changing time speed');
    $('map-cancel').click();

    for (let index = 0; index < speeds.length; index++) {
      move({ x: 1110, y: 225 }); worldState.minutes = 480;
      $(ids[index]).click();
      const started = performance.now();
      const target = document.querySelector('[data-map-town="millhaven"]');
      const screen = new DOMPoint(1120, 20).matrixTransform($('regional-map').getScreenCTM());
      $('regional-map').dispatchEvent(new MouseEvent('click', { bubbles: true, clientX: screen.x, clientY: screen.y }));
      assert(target && WorldMap.isTravelling(), 'Travel starts normally at ' + speeds[index] + '×');
      await wait(500); WorldMap.pause(false);
      const realSeconds = (performance.now() - started) / 1000, gameMinutes = worldState.minutes - 480;
      const expected = 36 * speeds[index] * realSeconds;
      assert(Math.abs(gameMinutes / expected - 1) < 0.25, 'Travel time runs at the selected ' + speeds[index] + '× rate');
      assert(worldState.distanceTraveledKm > 0, 'The character moves with the clock at ' + speeds[index] + '×');
      measurements.push({ speed: speeds[index], travelling: true, realSeconds, gameMinutes });
    }

    // Change speed during a journey: neither the route nor its destination is lost.
    move({ x: 1110, y: 225 }); $('map-speed-half').click();
    const screen = new DOMPoint(1120, 20).matrixTransform($('regional-map').getScreenCTM());
    $('regional-map').dispatchEvent(new MouseEvent('click', { bubbles: true, clientX: screen.x, clientY: screen.y }));
    await wait(200);
    const route = JSON.stringify(worldState.route), destination = JSON.stringify(worldState.destination), position = { ...worldState.position }, minutes = worldState.minutes;
    $('map-speed-double').click();
    assert(WorldMap.isTravelling() && JSON.stringify(worldState.route) === route && JSON.stringify(worldState.destination) === destination, 'Changing speed while travelling preserves the active journey');
    assert(worldState.minutes === minutes && worldState.position.x === position.x && worldState.position.y === position.y, 'Changing speed does not teleport or jump the clock');
    await wait(200); WorldMap.pause(false);
    assert(worldState.minutes - minutes > 3, 'A running journey immediately adopts the faster speed');

    for (let index = 0; index < speeds.length; index++) {
      move({ x: 420, y: 490 }); worldState.minutes = 480;
      $(ids[index]).click(); WorldMap.setAmbientEnabled(true);
      const started = performance.now();
      await wait(4400); WorldMap.setAmbientEnabled(false);
      const realSeconds = (performance.now() - started) / 1000, gameMinutes = worldState.minutes - 480;
      const expected = realSeconds * 0.5 * speeds[index];
      assert(gameMinutes > 0 && gameMinutes <= Math.floor(expected) && Math.floor(expected) - gameMinutes <= 1, 'Idle time runs at ' + speeds[index] + '× without fractional-minute duplication');
      measurements.push({ speed: speeds[index], travelling: false, realSeconds, gameMinutes });
    }
    // The chosen rate continues while the character sheet is shown.
    $('map-speed-double').click(); document.querySelector('[data-map-close]').click();
    WorldMap.setAmbientEnabled(true); const sheetTime = worldState.minutes;
    await wait(1400); WorldMap.setAmbientEnabled(false);
    assert(worldState.minutes === sheetTime + 1 && $('map-modal').hidden, 'Double-speed idle time also applies on the character sheet');
    WorldMap.open();

    const beforeRest = worldState.minutes; $('map-rest').click();
    assert(worldState.minutes === beforeRest + 600, 'Rest still advances exactly ten in-game hours at double speed');
    inventory.push('consumable-camping-supplies'); WorldMap.render();
    const beforeCamp = worldState.minutes; $('map-camp').click();
    assert(worldState.minutes === beforeCamp + 480, 'Camp still advances exactly eight in-game hours at double speed');
    const port = WorldRules.getRegion('sunward-coast').settlements.find(place => place.boat);
    move(port, 'sunward-coast'); const beforeBoat = worldState.minutes; $('map-boat').click();
    assert(worldState.minutes === beforeBoat + port.boat.minutes && worldState.timeSpeed === 2, 'Boat duration stays fixed and region travel retains the selected speed');

    // Pending choices and combat keep the clock paused, including at 2×.
    worldState.pendingEncounter = { zone: 'Plains', enemies: ['Goblin'], objective: null }; WorldMap.render();
    const pausedTime = worldState.minutes;
    assert(ids.every(id => $(id).disabled), 'An unresolved encounter disables the speed controls');
    $('map-speed-half').dispatchEvent(new MouseEvent('click', { bubbles: true }));
    WorldMap.setAmbientEnabled(true); await wait(1400); WorldMap.setAmbientEnabled(false);
    assert(worldState.minutes === pausedTime && worldState.timeSpeed === 2, 'Encounter time cannot advance or be changed through a disabled button');
    worldState.pendingEncounter = null; WorldMap.render();
    assert(await Combat.startEncounter(['Goblin']), 'A combat fixture starts');
    $('combat-sheet-button').click(); WorldMap.open();
    const combatTime = worldState.minutes;
    assert(ids.every(id => $(id).disabled), 'Combat disables the speed controls');
    $('map-speed-half').dispatchEvent(new MouseEvent('click', { bubbles: true }));
    WorldMap.setAmbientEnabled(true); await wait(1400); WorldMap.setAmbientEnabled(false);
    assert(worldState.minutes === combatTime && worldState.timeSpeed === 2, 'Combat still pauses world time even at double speed');
    Combat.restore(null); WorldMap.render();
    assert(ids.every(id => !$(id).disabled), 'Speed controls become available again after combat');

    $('map-speed-fast').click();
    if (window.statFickDesktop?.captureTestScreen) {
      await statFickDesktop.captureTestScreen('map-time-speeds', 1440, 1000);
      await statFickDesktop.captureTestScreen('map-time-speeds-mobile', 390, 844);
      const dialog = document.querySelector('.map-dialog').getBoundingClientRect(), buttons = ids.map(id => $(id).getBoundingClientRect());
      assert(buttons.every(bounds => bounds.left >= dialog.left && bounds.right <= dialog.right && bounds.bottom <= $('map-viewport').getBoundingClientRect().top), 'All four controls remain visible below the clock on narrow screens');
      await statFickDesktop.captureTestScreen('map-time-speeds', 1440, 1000);
    }
  } finally {
    EncounterRules.roll = originalRoll;
    WorldMap.setAmbientEnabled(false); Combat.restore(null); applyCharacter(normalizeCharacter(defaultCharacter()));
  }
  return { passed: true, checks, measurements };
}

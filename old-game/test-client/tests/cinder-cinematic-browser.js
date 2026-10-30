async function previewCinderCinematic(name, threshold) {
  const c = defaultCharacter(); c.level = 100; c.raceId = "human";
  for (const key of Object.keys(c.stats)) c.stats[key] = 250;
  c.stats.speed = 1000; c.currentHp = 100000; c.currentMana = 100000;
  applyCharacter(normalizeCharacter(c));
  if (!await Combat.startEncounter([name])) throw new Error("Preview boss failed to load");
  document.getElementById("boss-intro-skip").click();
  const state = Combat.serialize(), boss = state.actors.find(actor => actor.team === "enemy");
  boss.character.currentHp = Math.floor(boss.resourceMax.hp * threshold / 100);
  state.current = null; state.actors.forEach(actor => { actor.counter = actor.id === boss.id ? 1000 : 0; });
  Combat.restore(state); Combat.onSheetChange();
  document.getElementById("combat-view-toggle").click();
  await new Promise(resolve => setTimeout(resolve, 1700));
  const cinematic = document.getElementById("boss-intro-cinematic");
  if (cinematic.hidden || cinematic.dataset.boss !== "ur-mat-lucid" || getComputedStyle(cinematic).opacity < .5) throw new Error("Lucid Change's cutscene is not visibly playing");
}
async function previewDreamerCinematic() { await previewCinderCinematic("Ur-Mat, the False Dreamer", 15); }
async function previewForgeCinematic() {
  const c = defaultCharacter(); c.level = 100; c.raceId = "human";
  for (const key of Object.keys(c.stats)) c.stats[key] = 250;
  c.stats.speed = 1000; c.currentHp = 100000; c.currentMana = 100000;
  applyCharacter(normalizeCharacter(c));
  if (!await Combat.startEncounter(["Ornn, God of the Forge"])) throw new Error("Preview boss failed to load");
  document.getElementById("boss-intro-skip").click();
  const state = Combat.serialize(), boss = state.actors.find(actor => actor.team === "enemy");
  boss.character.currentHp = Math.floor(boss.resourceMax.hp * 0.5);
  state.current = null; state.actors.forEach(actor => { actor.counter = actor.id === boss.id ? 1000 : 0; });
  Combat.restore(state); Combat.onSheetChange();
  document.getElementById("combat-view-toggle").click();
  await new Promise(resolve => setTimeout(resolve, 1700));
  const cinematic = document.getElementById("boss-intro-cinematic");
  if (cinematic.hidden || cinematic.dataset.boss !== "ornn-phase" || getComputedStyle(cinematic).opacity < .5) throw new Error("Ornn's phase cutscene is not visibly playing");
}

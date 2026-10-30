async function previewTalosCinematic() {
  WorldMap.setAmbientEnabled(false);
  const c = defaultCharacter(); c.raceId = "human"; c.level = 100;
  for (const key of Object.keys(c.stats)) c.stats[key] = 250;
  c.stats.speed = 1000; c.currentHp = 100000; c.currentMana = 100000; applyCharacter(normalizeCharacter(c));
  if (!await Combat.startEncounter(["Tankina, Disciple of Talos", "Apostle of Talos", "Apostle of Talos"])) throw new Error("Boss preview failed");
  const state = Combat.serialize(), boss = state.actors.find(actor => actor.enemyDefinitionId === "tankina-disciple-of-talos-level-44");
  boss.character.currentHp = Math.floor(boss.resourceMax.hp * .5);
  state.current = null; state.actors.forEach(actor => { actor.counter = actor.id === boss.id ? 1000 : 0; });
  document.getElementById("boss-intro-skip").click();
  Combat.restore(state); Combat.onSheetChange(); document.getElementById("combat-view-toggle").click();
  await new Promise(resolve => setTimeout(resolve, 1800));
  const cinematic = document.getElementById("boss-intro-cinematic");
  if (cinematic.hidden || cinematic.dataset.boss !== "tankina-phase" || getComputedStyle(cinematic).opacity < .5) throw new Error("Talos phase cutscene is not visible");
}

/* Read-only Guild intel rules, shared by the interface and regression tests. */
(function (root) {
  const cost = 65;
  const slug = value => String(value || "").trim().toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "");
  const ids = values => Array.isArray(values) ? [...new Set(values.filter(id => typeof id === "string" && id.trim()).map(id => id.trim()))] : [];
  function normalize(saved) {
    return {
      offers: { class: ids(saved?.offers?.class).slice(0, 3), item: ids(saved?.offers?.item).slice(0, 5) },
      revealed: { class: ids(saved?.revealed?.class), item: ids(saved?.revealed?.item) },
    };
  }
  function playerClasses(classes, races) {
    const playableRaces = new Set(races.filter(race => race.playable).map(race => slug(race.id)));
    const candidates = classes.filter(definition => {
      const other = definition.requirements?.other;
      const type = slug(typeof other === "string" ? other : other?.type);
      if (definition.maxLevel < 1 || definition.requirements?.level > 100 || ["unobtainable", "non-playable-race", "race-playability"].includes(type)) return false;
      if (type === "race") return playableRaces.has(slug(other.raceId || other.name));
      if (type === "race-in-list") return (other.raceIds || []).some(id => playableRaces.has(slug(id)));
      return true;
    });
    // A player class cannot depend solely on an NPC-only prerequisite.
    let pool = candidates;
    for (;;) {
      const byId = new Map(pool.map(definition => [definition.id, definition]));
      const possible = ([id, rank]) => (byId.get(id)?.maxLevel || 0) >= rank;
      const next = pool.filter(definition => {
        const requirements = definition.requirements || {};
        const alternatives = Object.entries(requirements.anyClasses || {});
        const other = requirements.other;
        return Object.entries(requirements.classes || {}).every(possible)
          && (!alternatives.length || alternatives.some(possible))
          && (other?.type !== "class-level-count" || (other.classes || []).filter(id => possible([id, other.level])).length >= other.count);
      });
      if (next.length === pool.length) return next;
      pool = next;
    }
  }
  // Masterwork weapons, armor and artifacts carry the drop rules unique items once had.
  const masterworkEquipment = item => item?.rarity === "masterwork" && ["weapon", "armor", "artifact"].includes(item.category);
  function itemPool(items, enemies, classes) {
    const classItems = new Set(classes.map(definition => definition.requirements?.other).filter(other => other?.type === "inventory-item").map(other => other.itemId));
    const candidates = items.filter(item => item.droppable !== false && (masterworkEquipment(item) || classItems.has(item.id)));
    return candidates.map(item => {
      const sources = enemies.flatMap(enemy => {
        if ((enemy.noDropItems || []).includes(item.id)) return [];
        const carried = item.automaticDrop !== false && (enemy.inventory || []).includes(item.id);
        const drop = (enemy.drops || []).find(entry => entry.itemId === item.id);
        if (!carried && (!drop || drop.chance <= 0 && !drop.guaranteedUntilClaimed && !drop.guaranteedOnFirstKill)) return [];
        return [{ enemyId: enemy.id, enemyName: enemy.name, enemyLevel: enemy.level, chance: carried ? 100 : drop.chance,
          guaranteedUntilClaimed: !carried && drop.guaranteedUntilClaimed === true,
          guaranteedOnFirstKill: !carried && drop.guaranteedOnFirstKill === true }];
      });
      return { item, classUnlock: classItems.has(item.id), sources };
    }).filter(entry => entry.sources.length);
  }
  function enemyLocations(regions, encounters, specialEncounters = []) {
    const locations = new Map();
    function add(enemyName, regionId, zone, kind, offerRegionId = null, encounterName = null) {
      const region = regions[regionId];
      if (!enemyName || !region || !zone) return;
      const entries = locations.get(enemyName) || [];
      if (entries.some(entry => entry.regionId === regionId && entry.zone === zone && entry.kind === kind && entry.encounterName === encounterName)) return;
      entries.push({ regionId, regionName: region.name, zone, kind, encounterName,
        offerRegionId, offerRegionName: regions[offerRegionId]?.name || null });
      locations.set(enemyName, entries);
    }
    for (const regionId of Object.keys(regions)) {
      const rules = encounters.regionRules(regionId);
      for (const zone of Object.keys(rules.tables || {})) {
        if (!(rules.chances[zone] > 0)) continue;
        // The existing encounter enumerator uses deterministic rolls. Reading
        // information must not consume RNG or change the active map region.
        for (const name of encounters.enemiesForZone(zone, regionId)) add(name, regionId, zone, "encounter");
      }
    }
    for (const definition of specialEncounters) {
      const kind = definition.kind || "great-hunt";
      const offerRegionId = kind === "great-hunt" ? definition.offerRegionId || definition.regionId : null;
      const zones = definition.zones?.length ? definition.zones.map(zone => zone.name) : [definition.zone];
      const names = [...new Set([definition.enemy, ...(definition.roster || [])].filter(Boolean))];
      for (const name of names) for (const zone of zones) add(name, definition.regionId, zone, kind, offerRegionId, definition.enemy || null);
    }
    return locations;
  }
  function dropChance(item, source, luck, classBonus = 0) {
    const bonus = masterworkEquipment(item) ? Math.max(0, Number(luck) || 0) * 0.05 + Math.max(0, Number(classBonus) || 0) : 0;
    return Math.min(100, Math.max(0, source.chance + bonus));
  }
  const api = { cost, normalize, playerClasses, itemPool, enemyLocations, dropChance };
  root.GuildInformationRules = api;
  if (typeof module !== "undefined" && module.exports) module.exports = api;
})(globalThis);

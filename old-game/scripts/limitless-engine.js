/* Limitless (port task 7, decisions L1–L28 in engine/Docs/Engine-Plan.md): the rules, without the page. Loaded before
   app.js, which reads saves with them. A character's record: { unlocked, active, kept, spent } for the player (only the
   player's unlocked and active count; companions follow them), { kept, spent } for a companion.
   - kept: what turning Limitless off set aside (L2): the Limitless level, the EXP toward its next level, the attribute
     gains above Level 100 (+1 each per level, plus the free points spent above 100), the unspent free points, and which
     spent points were taken back. Turning it on gives all of it back.
   - spent: the free points spent while above Level 100, per attribute, so turning it off takes back exactly those. */
(root => {
  "use strict";
  const BASE_CAP = 100, LIMITLESS_CAP = 200, POINTS_PER_LEVEL = 5;
  const STAT_KEYS = ["strength", "vitality", "mind", "dexterity", "intelligence", "wisdom", "speed", "luck"];
  const ANATEMIS_ID = "anatemis-white-star-unique-level-100";
  const whole = value => Math.max(0, Math.floor(Number(value) || 0));
  const perStat = (value, keep = whole) => Object.fromEntries(STAT_KEYS.map(key => [key, keep(value?.[key])]));
  function normalizeKept(kept) {
    if (!kept || typeof kept !== "object" || !(whole(kept.level) >= BASE_CAP)) return null;
    return {
      level: Math.min(LIMITLESS_CAP, whole(kept.level)), exp: whole(kept.exp), statGains: perStat(kept.statGains),
      availablePoints: whole(kept.availablePoints), allocations: perStat(kept.allocations),
    };
  }
  // A saved record, made safe; old saves start locked and off (L17).
  function normalize(value, companion = false) {
    const record = { kept: normalizeKept(value?.kept), spent: perStat(value?.spent) };
    if (companion) return record;
    const unlocked = value?.unlocked === true;
    return { unlocked, active: unlocked && value?.active === true, ...record };
  }
  const cap = active => active ? LIMITLESS_CAP : BASE_CAP;
  /* Turning Limitless off (L2, L4): a sheet ({ level, exp, stats, manualAllocations, availablePoints, limitless }) at
     Level 100 or more goes back to its Level-100 values; the rest is kept aside. Returns whether anything was. */
  function keepAside(sheet) {
    const record = sheet.limitless;
    if (!record || record.kept || whole(sheet.level) < BASE_CAP) return false;
    const levelsAbove = whole(sheet.level) - BASE_CAP, points = levelsAbove * POINTS_PER_LEVEL;
    const fromAvailable = Math.min(whole(sheet.availablePoints), points);
    let rest = points - fromAvailable;
    const allocations = perStat({});
    for (const key of STAT_KEYS) {
      const take = Math.min(rest, whole(record.spent?.[key]), whole(sheet.manualAllocations?.[key]));
      allocations[key] = take; rest -= take;
    }
    const statGains = Object.fromEntries(STAT_KEYS.map(key => [key, levelsAbove + allocations[key]]));
    record.kept = { level: whole(sheet.level), exp: whole(sheet.exp), statGains, availablePoints: fromAvailable, allocations };
    sheet.level = BASE_CAP; sheet.exp = 0; sheet.availablePoints = whole(sheet.availablePoints) - fromAvailable;
    for (const key of STAT_KEYS) {
      sheet.stats[key] = Math.floor(Number(sheet.stats[key]) || 0) - statGains[key];
      sheet.manualAllocations[key] = whole(sheet.manualAllocations[key]) - allocations[key];
    }
    record.spent = perStat({});
    return true;
  }
  // Turning it on again: everything kept aside comes back (L2).
  function restore(sheet) {
    const kept = sheet.limitless?.kept;
    if (!kept) return false;
    sheet.level = kept.level; sheet.exp = kept.exp; sheet.availablePoints = whole(sheet.availablePoints) + kept.availablePoints;
    for (const key of STAT_KEYS) {
      sheet.stats[key] = Math.floor(Number(sheet.stats[key]) || 0) + kept.statGains[key];
      sheet.manualAllocations[key] = whole(sheet.manualAllocations[key]) + kept.allocations[key];
    }
    sheet.limitless.spent = { ...kept.allocations };
    sheet.limitless.kept = null;
    return true;
  }
  // A free point spent (or, for a companion, taken back) while above Level 100.
  function noteSpent(record, key, change, sheetLevel) {
    if (!record || !STAT_KEYS.includes(key)) return;
    if (change > 0 && whole(sheetLevel) > BASE_CAP) record.spent[key] = whole(record.spent[key]) + 1;
    if (change < 0) record.spent[key] = Math.max(0, whole(record.spent[key]) - 1);
  }
  /* Limitless enemies (L3, L5, L7): below your level an enemy rises to it, +1 to each attribute per level (no free
     points); then every attribute ×1.25. Applied once, to the copy an actor spawns from. A boss also gets the class
     levels that bring it to a Level-100 character's 100 (L6; the draft below, for the user's review): these entries
     raise a class it has or add one; Anatemis has her 100 already. */
  const ENEMY_STAT_MULTIPLIER = 1.25;
  const BOSS_CLASSES = {
    "ancient-lich-ool-unique-level-78": {"hierophant": 9, "plaguebinder": 6, "soul-shepherd": 5, "death-paladin": 4, "chronomancer": 5, "mirror-mage": 3},  // Ancient Lich Ool
    "archdevil-saffron-unique-level-66": {"umbramancer": 9, "hierophant": 9, "warlock": 7, "plaguebinder": 6, "mirror-mage": 4, "wise-man": 5, "avatar": 4},  // Archdevil Saffron
    "deep-sea-king-unique-level-32": {"monster": 3, "champion": 3, "knight": 7, "guardian": 7, "heavy-knight": 5, "slayer": 7, "reaver": 7, "echo-knight": 7, "ambusher": 7, "assassin": 6, "barbarian": 7, "berserker": 3},  // Deep Sea King
    "gaian-avatar-of-the-land-unique-level-53": {"geomancer": 9, "druid": 7, "guardian": 7, "heavy-knight": 5, "gladiator": 7, "reaver": 7, "echo-knight": 5},  // Gaian, Avatar of the Land
    "great-imp-jiruan-unique-level-18": {"spellcaster": 7, "wizard": 7, "sorcerer": 5, "scholar": 5, "great-mage": 7, "umbramancer": 9, "warlock": 7, "cultist": 7, "soul-shepherd": 5, "dreamweaver": 6, "hexblade": 5, "sage": 5, "energist": 5, "rogue": 5},  // Great Imp Ji'ruan
    "juila-grand-forest-matriach-unique-level-22": {"wizard": 7, "scholar": 5, "sorcerer": 4, "great-mage": 7, "wise-man": 5, "sage": 5, "druid": 6, "lifewarden": 6, "priest": 7, "cardinal-priest": 4, "energist": 5, "soul-shepherd": 5, "strategist": 7, "general": 5},  // Juila the Grand Forest Matriarch
    "nygheir-the-wounded-unique-level-23": {"knight": 6, "guardian": 7, "great-beast": 10, "geomancer": 9, "spellcaster": 7, "wizard": 7, "great-mage": 5, "slayer": 7, "reaver": 7, "heavy-knight": 5, "bulwark": 7, "soldier": 6},  // Nygheir the Wounded
    "ornn-god-of-the-forge-unique-level-48": {"berserker": 5, "gladiator": 7, "slayer": 7, "echo-knight": 7, "templar": 6, "oathsworn": 7, "geomancer": 9, "champion": 3, "reaver": 4},  // Ornn, God of the Forge
    "tankina-disciple-of-talos-level-44": {"electromancer": 9, "hemomancer": 9, "vampire-lord": 2, "stormcaller": 5, "spellblade": 5, "blood-hunter": 5, "knight": 6, "soldier": 6, "berserker": 6, "great-mage": 7, "sorcerer": 5, "nullblade": 6},  // Tankina, Disciple of Talos
    "ur-mat-false-dreamer-unique-level-90": {"dreamweaver": 6, "psykic": 6, "mirror-mage": 3},  // Ur-Mat, the False Dreamer
    "zik-lun-armored-hulk-unique-level-24": {"strategist": 7, "general": 5, "flagbearer": 4, "guardian": 7, "heavy-knight": 5, "bulwark": 7, "reaver": 7, "berserker": 5, "soldier": 6, "energist": 5, "echo-knight": 7, "brawler": 6, "nullblade": 5},  // Zik Lun, the armored Hulk
  };
  function scaleEnemy(character, playerLevel, definition = {}) {
    if (!character?.stats) return character;
    const target = Math.min(LIMITLESS_CAP, whole(playerLevel)), own = whole(character.level);
    if (target > own) {
      for (const key of STAT_KEYS) character.stats[key] = Math.floor(Number(character.stats[key]) || 0) + target - own;
      character.level = target;
    }
    for (const key of STAT_KEYS) character.stats[key] = Math.max(0, Math.floor((Number(character.stats[key]) || 0) * ENEMY_STAT_MULTIPLIER));
    const classes = definition?.unique ? BOSS_CLASSES[definition.id] : null;
    if (classes) character.classLevels = { ...(character.classLevels || {}), ...classes };
    return character;
  }
  // An item's level: a forged piece's, a masterwork artifact copy's; 0 when it has none.
  const itemLevel = item => whole(item?.forged?.level ?? (item?.masterworkArtifactBaseId ? item.level : 0));
  // While Limitless is off, nothing above Level 100 can be worn (L10); Exotics at 100 or below keep working (L25).
  const wearable = (item, active) => active || itemLevel(item) <= BASE_CAP;
  root.LimitlessRules = { BASE_CAP, LIMITLESS_CAP, POINTS_PER_LEVEL, STAT_KEYS, ANATEMIS_ID, ENEMY_STAT_MULTIPLIER, BOSS_CLASSES, normalize, cap, keepAside, restore, noteSpent, scaleEnemy, itemLevel, wearable };
  if (typeof module !== "undefined") module.exports = root.LimitlessRules;
})(globalThis);

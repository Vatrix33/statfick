/* Limitless (port task 7, decisions L1–L22 in engine/Docs/Engine-Plan.md): the rules, without the page. Loaded before
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
  // An item's level: a forged piece's, a masterwork artifact copy's; 0 when it has none.
  const itemLevel = item => whole(item?.forged?.level ?? (item?.masterworkArtifactBaseId ? item.level : 0));
  // While Limitless is off, nothing above Level 100 can be worn (L10, L13).
  const wearable = (item, active) => active || itemLevel(item) <= BASE_CAP;
  root.LimitlessRules = { BASE_CAP, LIMITLESS_CAP, POINTS_PER_LEVEL, STAT_KEYS, ANATEMIS_ID, normalize, cap, keepAside, restore, noteSpent, itemLevel, wearable };
  if (typeof module !== "undefined") module.exports = root.LimitlessRules;
})(globalThis);

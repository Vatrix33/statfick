// The user's small changes of 2026-11-03 (old-game/Docs/Notes.md, "Small changes (2026-11-03)").
// Each rule has its own entry, so each can be shown failing on its own; runSmallChangeChecks runs them all.
const smallChangeTools = () => {
  const $ = id => document.getElementById(id);
  const assert = (ok, message) => { if (!ok) throw new Error(message); };
  const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
  const fresh = (extra = {}) => { Combat.restore(null); applyCharacter(normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: 30, editingEnabled: false, ...extra })); };
  return { $, assert, wait, fresh };
};

async function runPurseDevModeChecks() {
  const { $, assert, fresh } = smallChangeTools();
  fresh({ currencies: { ...defaultCharacter().currencies, gold: 12 } });
  const inputs = [...document.querySelectorAll(".currency-input")];
  assert(inputs.length === 3 && inputs.every(input => input.disabled), "The purse can't be edited outside Dev Mode");
  assert(inputs.every(input => input.closest(".sidebar-currency").title.includes("Dev Mode")), "The locked purse says Dev Mode edits it");
  assert(inputs.find(input => input.dataset.currency === "gold").value === "12", "The locked purse still shows the money");
  $("editing-toggle").click();
  assert(inputs.every(input => !input.disabled && !input.closest(".sidebar-currency").title), "Dev Mode unlocks the purse");
  const gold = inputs.find(input => input.dataset.currency === "gold");
  gold.value = "40"; gold.dispatchEvent(new Event("input", { bubbles: true }));
  assert(currencies.gold === 40, "Money edited in Dev Mode is kept");
  $("editing-toggle").click();
  assert(inputs.every(input => input.disabled), "Leaving Dev Mode locks the purse again");
}

async function runDevModeTextChecks() {
  const { $, assert, fresh } = smallChangeTools();
  fresh();
  assert(!$("editing-status") && !document.querySelector(".brand-subtitle"), "The Dev-mode status text is gone");
  assert(document.querySelector(".brand").textContent.replace(/\s+/g, " ").trim() === "✦ Stat Fick", "The corner keeps the ✦ button and the name");
  assert($("editing-toggle").title === "Enable Dev-mode" && !$("editing-toggle").classList.contains("is-on"), "The button's tooltip and look show Dev Mode is off");
  $("editing-toggle").click();
  assert($("editing-toggle").title === "Disable Dev-mode" && $("editing-toggle").classList.contains("is-on"), "The button's tooltip and look show Dev Mode is on");
  $("editing-toggle").click();
}

async function runTemporaryBoostChecks() {
  const { $, assert, wait, fresh } = smallChangeTools();
  const base = { stats: { ...defaultCharacter().stats, strength: 20 }, permanentDefenseBonuses: { armor: 0, evasion: 0 } };
  fresh(base);
  const plain = { strength: currentStats().strength, armor: $("armor-value").textContent };
  assert(!document.querySelector('[data-bonus="temporary"]') && !document.body.textContent.includes("Temporary bonus"), "The sheet has no temporary bonus fields");
  assert(document.querySelectorAll('.stat-bonus-input[data-bonus="permanent"]').length === 8 && document.querySelectorAll('.defense-bonus-input[data-bonus="permanent"]').length === 2, "The permanent bonus fields stay");
  // An old save's temporary values no longer count, and aren't kept.
  const loaded = normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: 30, editingEnabled: false, ...base, temporaryBonuses: { strength: 50 }, temporaryDefenseBonuses: { armor: 50, evasion: 50 } });
  assert(!("temporaryBonuses" in loaded) && !("temporaryDefenseBonuses" in loaded), "Saved temporary bonuses are dropped on load");
  Combat.restore(null); applyCharacter(loaded);
  assert(currentStats().strength === plain.strength && $("armor-value").textContent === plain.armor, "Saved temporary bonuses don't change the stats");
  saveCharacter();
  const saved = JSON.parse(localStorage.getItem(STORAGE_KEY));
  assert(!("temporaryBonuses" in saved) && !("temporaryDefenseBonuses" in saved), "Saves no longer write temporary bonuses");
  // A companion's sheet has none either.
  const offer = { id: "companion-small-changes", name: "Mira Vale", gender: "female", character: normalizeCharacter({ ...defaultCharacter(), name: "Mira Vale", raceId: "human", level: 20 }) };
  Companions.add(offer); Companions.open(offer.id); await wait(300);
  const sheet = $("companion-sheet");
  assert(sheet.querySelectorAll('[data-comp-bonus="permanentBonuses"]').length === 8 && sheet.querySelectorAll('[data-comp-bonus="permanentDefenseBonuses"]').length === 2, "The companion sheet keeps its permanent bonus fields");
  assert(!sheet.querySelector('[data-comp-bonus^="temporary"]') && !/Temporary/.test(sheet.textContent), "The companion sheet has no temporary bonus fields");
  for (const layer of document.querySelectorAll(".modal-layer")) layer.hidden = true; updateModalState();
  companions.splice(0, companions.length);
}

async function runEquipmentFillChecks() {
  const { assert, wait, fresh } = smallChangeTools();
  const F = ForgeRules;
  const sword = F.encode(F.generate({ type: "longsword", level: 30, source: "shop", random: () => 0.5 }));
  const shield = F.encode(F.generateArmor({ type: "kite-shield", level: 30, source: "shop", random: () => 0.5 }));
  fresh({ inventory: [sword, shield], equipment: { ...defaultEquipment, primaryHand: sword, offHand: shield } });
  await wait(100);
  const box = key => document.querySelector(`[data-equipment-slot="${key}"] .equipment-slot-content > *`).getBoundingClientRect();
  const content = key => document.querySelector(`[data-equipment-slot="${key}"] .equipment-slot-content`).getBoundingClientRect();
  assert(box("primaryHand").height > 0 && Math.abs(box("offHand").height - box("primaryHand").height) < 1, "The shield's button is as tall as the sword's in the same row");
  assert(Math.abs(box("offHand").top - box("primaryHand").top) < 1, "The shield's button starts where the sword's does");
  for (const key of ["offHand", "armor", "artifact1", "artifact2"])
    assert(Math.abs(box(key).height - content(key).height) < 1 && Math.abs(box(key).bottom - content(key).bottom) < 1, `The ${key} button fills its whole field`);
}

async function runTargetHighlightChecks() {
  const { assert, fresh } = smallChangeTools();
  fresh();
  // Two of each card, one targeted, styled by the game's stylesheet.
  const host = document.createElement("div");
  host.innerHTML = '<div class="battle-actor team-enemy is-targeted"><span class="battle-actor-marker"></span><span class="battle-actor-shadow"></span><span class="battle-actor-plate"></span></div>' +
    '<div class="battle-actor team-enemy"><span class="battle-actor-marker"></span><span class="battle-actor-shadow"></span><span class="battle-actor-plate"></span></div>' +
    '<button class="combatant combat-enemy is-targeted is-unique"></button><button class="combatant combat-enemy is-unique"></button>';
  document.body.append(host);
  try {
    const [targeted, other] = host.querySelectorAll(".battle-actor");
    const style = (el, selector, pseudo = null) => getComputedStyle(el.querySelector(selector), pseudo);
    assert(style(targeted, ".battle-actor-marker").opacity === "1" && parseFloat(style(targeted, ".battle-actor-marker").borderTopWidth) >= 18, "The stage marker over the target is larger");
    assert(style(targeted, ".battle-actor-marker").filter.includes("drop-shadow") && style(targeted, ".battle-actor-marker").filter.split("drop-shadow").length > 2, "The stage marker glows");
    assert(style(targeted, ".battle-actor-shadow", "::after").content === '""' && style(other, ".battle-actor-shadow", "::after").content === "none", "A ring circles the target's feet, and only the target's");
    assert(style(targeted, ".battle-actor-shadow", "::after").animationName === "battle-target-ring", "The ring pulses");
    assert(style(targeted, ".battle-actor-plate").boxShadow.startsWith("rgb(255, 213, 106) 0px 0px 0px 2px") && !style(other, ".battle-actor-plate").boxShadow.includes("255, 213, 106"), "The target's name tag has a gold edge");
    const [card, plain] = host.querySelectorAll(".combat-enemy");
    assert(getComputedStyle(card).borderTopWidth === "3px" && getComputedStyle(plain).borderTopWidth === "1px", "The classic view's targeted card has a thicker border, also on a boss");
    assert(getComputedStyle(card).boxShadow.includes("inset") && getComputedStyle(card).boxShadow.split("rgb").length > 2, "The targeted card glows");
    assert(card.getBoundingClientRect().height === plain.getBoundingClientRect().height, "Targeting doesn't change the card's size");
  } finally { host.remove(); }
}

async function runLevellingCurveChecks() {
  const { assert } = smallChangeTools();
  const R = CombatRules;
  let kills = 0; const reach = {};
  for (let level = 1; level < 100; level++) {
    assert(R.experienceRequired(level) === R.enemyExperience(level) * R.sameLevelVictories(level), `Level ${level} needs its victories times an enemy's EXP`);
    kills += R.sameLevelVictories(level);
    reach[level + 1] = kills;
  }
  assert(reach[12] === 51 && reach[30] === 186 && reach[50] === 388 && reach[100] === 1265, "Reaching 12, 30, 50 and 100 takes 51, 186, 388 and 1265 same-level victories");
  const victories = [1, 5, 11, 12, 30, 50, 75, 99].map(level => R.sameLevelVictories(level));
  assert(victories.join() === "3,4,6,7,9,12,17,24", "3–6 victories per level up to 12, then a curve rising to 24 at 99");
  for (let level = 12; level < 99; level++) assert(R.sameLevelVictories(level + 1) >= R.sameLevelVictories(level), "Later levels never take fewer victories");
  const experience = [1, 11, 12, 30, 50, 99].map(level => R.enemyExperience(level));
  assert(experience.join() === "40,150,163,310,425,834", "Enemies give the EXP they gave before the change");
  assert([1, 12, 50, 99].map(level => R.experienceRequired(level)).join() === "120,1141,5100,20016", "The EXP targets follow the curve");
}

async function runDragonbloodDropChecks() {
  const { assert } = smallChangeTools();
  const names = await (await fetch("enemies/index.json")).json();
  const dragons = [];
  for (const name of names) {
    const enemy = await (await fetch("enemies/" + encodeURIComponent(name))).json();
    if (enemy.raceId === "dragon") dragons.push(enemy);
  }
  assert(dragons.length >= 22, "Every dragon enemy is checked");
  for (const enemy of dragons) {
    const vial = (enemy.drops || []).find(drop => drop.itemId === "key-item-vial-of-dragonblood");
    assert(vial, `${enemy.name} can drop a Vial of Dragonblood`);
    assert(vial.chance === (enemy.id.startsWith("nygheir-the-wounded") ? 1 : 0.5), `${enemy.name} drops it at ${enemy.id.startsWith("nygheir-the-wounded") ? 1 : 0.5}%`);
  }
}

async function runMatchingBonusChecks() {
  const { assert } = smallChangeTools();
  const F = ForgeRules;
  const KEYS = ["finalPct", "basePowerPct", "primaryPct", "secondaryPct", "hit", "crit", "pierce", "spellCost", "pricePct"];
  const BONUS = { roundtable: { finalPct: 3 }, dwarven: { pierce: 5 }, elven: { hit: 5 }, "boreal-bolt": { basePowerPct: 5 }, abyss: { crit: 5 }, hellmade: { secondaryPct: 5 }, hearthforge: { hit: 3, crit: 3 }, outlaw: {} };
  const TEXT = { roundtable: "+3% final power", dwarven: "+5% armor pierce", elven: "+5 Accuracy", "boreal-bolt": "+5% base power", abyss: "+5 Crit Rating", hellmade: "+5% secondary scaling", hearthforge: "+3 Accuracy · +3 Crit Rating", outlaw: "" };
  // The parts' own sum, as the forge adds it: materials (the main part twice) and the secondary parts' makers.
  const partSum = spec => {
    const sum = Object.fromEntries(KEYS.map(key => [key, 0]));
    spec.parts.forEach((part, index) => {
      if (!part) return;
      for (const key of KEYS) sum[key] += (F.MATERIALS[part.material].effect[key] || 0) * (index === 0 ? 2 : 1) + (index > 0 ? F.MAKERS[part.maker].effect[key] || 0 : 0);
    });
    return sum;
  };
  let s = 4242; const random = () => { s = (s * 16807) % 2147483647; return s / 2147483647; };
  const seen = {}; let plain = 0;
  for (let i = 0; i < 20000 && (Object.keys(seen).length < 8 || plain < 20); i++) {
    const spec = F.generate({ level: 40, source: "loot", random });
    if (F.WEAPONS[spec.type].caster || spec.runes.length) continue;
    const parts = spec.parts.filter(Boolean), maker = parts.length > 1 && parts.every(part => part.maker === parts[0].maker) ? parts[0].maker : null;
    const extra = Object.fromEntries(KEYS.map(key => [key, F.totals(spec)[key] - partSum(spec)[key]]));
    if (maker) {
      seen[maker] = true;
      assert(KEYS.every(key => extra[key] === (BONUS[maker][key] || 0)), `A Matching ${F.MAKERS[maker].name} weapon gets its own bonus (${JSON.stringify(extra)})`);
      assert(F.matchingBonusText(spec) === TEXT[maker] && F.name(spec).includes("Matching"), `A Matching ${F.MAKERS[maker].name} weapon lists ${TEXT[maker] || "no bonus"}`);
      if (TEXT[maker]) assert(forgedPartsMarkup(F.item(F.encode(spec))).includes("Every part by " + F.MAKERS[maker].name), "Its parts list shows the Matching bonus");
    } else {
      plain++;
      assert(KEYS.every(key => extra[key] === 0) && !F.matchingBonusText(spec) && !forgedPartsMarkup(F.item(F.encode(spec))).includes("Matching bonus"), "Other weapons get no Matching bonus");
    }
  }
  assert(Object.keys(seen).length === 8 && plain >= 20, "Every maker's Matching weapon was checked");
  // Flawless masterworks get it; staffs, wands, armor and shields don't.
  let flawless = null;
  for (let i = 0; i < 400 && !flawless; i++) { const spec = F.generateMasterwork({ key: "giantslayer", level: 60, random }); if (spec.parts.every(part => part.maker === spec.parts[0].maker)) flawless = spec; }
  assert(flawless && F.name(flawless).startsWith("Flawless") && F.matchingBonusText(flawless) === TEXT[flawless.parts[0].maker], "A Flawless masterwork gets its maker's Matching bonus");
  const caster = F.generate({ type: "staff", level: 40, source: "loot", random });
  assert(!F.matchingBonusText(caster), "Staffs and wands get no Matching bonus");
  for (let i = 0; i < 200; i++) assert(!F.matchingBonusText(F.generateArmor({ type: "kite-shield", level: 40, source: "loot", random })), "Armor and shields get no Matching bonus");
}

async function runSmallChangeChecks() {
  for (const check of [runPurseDevModeChecks, runDevModeTextChecks, runTemporaryBoostChecks, runEquipmentFillChecks, runTargetHighlightChecks, runLevellingCurveChecks, runDragonbloodDropChecks, runMatchingBonusChecks]) await check();
}

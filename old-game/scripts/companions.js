/* Recruits have their own character records; summons never enter this roster. */
(() => {
  const $ = id => document.getElementById(id);
  const modal = $("companions-modal");
  let selectedId = null;
  let pendingImport = null, importLoading = false, importRequest = 0;
  const importLimit = 10 * 1024 * 1024;
  const companionId = () => "companion-" + (globalThis.crypto?.randomUUID?.() || Date.now() + "-" + Math.random().toString(36).slice(2));
  const importReason = () => window.Combat?.active() ? "Finish combat before importing a companion." : companions.length >= 3 ? "Your party already has three companions." : "";
  function renderImportControls() {
    const reason = importReason();
    $("companion-import").disabled = importLoading || Boolean(reason);
    $("companion-import").title = reason || "Choose a saved character JSON file to preview as a companion.";
    $("companion-import-confirm").disabled = !pendingImport || Boolean(reason);
  }
  function clearImport() {
    pendingImport = null; importLoading = false; importRequest++;
    $("companion-import-panel").hidden = true;
    $("companion-import-name").value = "";
    $("companion-import-file").value = "";
    renderImportControls();
  }
  function prepareImport(text, filename) {
    if (importReason()) throw new Error(importReason());
    if (typeof text !== "string" || text.length > importLimit) throw new Error("Character files must be smaller than 10 MB.");
    const payload = JSON.parse(text), raw = payload?.character ?? payload;
    if (!raw || typeof raw !== "object" || Array.isArray(raw) || !statDefinitions.some(({ key }) => {
      const value = raw.stats?.[key] ?? raw[key];
      return value != null && Number.isFinite(Number(value));
    })) throw new Error("That file is not a valid Stat Fick character save.");
    const defaults = defaultCharacter();
    const character = normalizeCharacter({ ...raw, companions: [], recruitOffers: [], combatState: null, enemyCount: 0, worldState: defaults.worldState, shopState: defaults.shopState, editingEnabled: false });
    for (const state of Object.values(character.activeSkills)) {
      state.active = false; state.usedThisCombat = false; state.usesThisCombat = 0; delete state.remainingUses;
    }
    const race = raceCatalog.find(entry => entry.id === character.raceId);
    const fileLabel = String(filename || "").replace(/\.json$/i, "").replace(/[_-]+/g, " ").trim();
    const suggestedName = raw.name || payload?.name || (!/^(stat fick character|emberfall character|desktop test character|character)$/i.test(fileLabel) && fileLabel) || `Imported ${race?.name || "Adventurer"}`;
    const offer = { id: companionId(), name: String(suggestedName).trim().slice(0, 80), gender: raw.gender === "female" || payload?.gender === "female" ? "female" : "male", character };
    const values = preview(offer);
    character.currentHp = Math.min(values.maxHp, character.currentHp);
    character.currentMana = Math.min(values.maxMana, character.currentMana);
    pendingImport = offer;
    $("companion-import-name").value = offer.name;
    const classCount = Object.keys(character.classLevels).filter(id => character.classLevels[id] > 0).length;
    $("companion-import-summary").textContent = `${filename || "Saved character"} · ${race?.name || "Adventurer"} · Level ${character.level} · ${classCount} class${classCount === 1 ? "" : "es"} · ${character.knownSpells.length} spell${character.knownSpells.length === 1 ? "" : "s"} · ${character.inventory.length} item${character.inventory.length === 1 ? "" : "s"}. Your current character will not be replaced.`;
    $("companion-import-panel").hidden = false;
    $("companion-status").textContent = "Review the import and choose a name. Combat, map progress, and nested companions are not imported.";
    renderImportControls(); $("companion-import-name").focus(); $("companion-import-name").select();
  }
  async function loadImport(readFile) {
    if (importLoading || modal.hidden || importReason()) return;
    clearImport(); importLoading = true; renderImportControls();
    const request = importRequest;
    try {
      const file = await readFile();
      if (request !== importRequest || modal.hidden || !file) return;
      prepareImport(file.text, file.name);
    } catch (error) {
      if (request === importRequest && !modal.hidden) $("companion-status").textContent = error instanceof SyntaxError ? "That file is not valid JSON. Choose a saved character file." : error.message;
    } finally {
      if (request === importRequest) { importLoading = false; renderImportControls(); }
    }
  }
  function confirmImport(event) {
    event.preventDefault();
    const reason = importReason(), name = $("companion-import-name").value.trim();
    if (!pendingImport || reason || !name) { $("companion-status").textContent = reason || "Enter a name for this companion."; return; }
    const offer = pendingImport; offer.name = name.slice(0, 80);
    if (!add(offer)) { $("companion-status").textContent = "This companion could not be added. Check your party slots."; return; }
    clearImport(); render(true); updateDerivedValues(); window.Guild?.render();
    $("companion-status").textContent = `${offer.name} joined your party from the saved character. Your main character is unchanged.`;
  }
  const names = {
    human: {
      male: "Alden Bram Cedric Dorian Edric Finn Gareth Hugo Ivor Julian",
      female: "Ada Beatrice Clara Elara Freya Gwen Iris Lyra Mara Nora",
      family: "Ashford Blackwell Brightwood Calder Corwin Dale Duskwood Fairchild Fenwick Grey Hartwell Hawthorne Highfield Kendric Langley Marlow Morrow Northwood Oakley Redfern Rowan Silverton Thornfield Vale Westbrook",
    },
    elf: {
      male: "Aelar Caelith Elion Faelar Galen Ilmarion Lareth Maelor Naeris Thalion",
      female: "Aelira Calithra Elenwe Faelora Galadri Illyra Lirael Maeriel Naivara Thalira",
      family: "Amariel Aurelith Brighthollow Dawnleaf Dewbranch Evenstar Fernsong Greenbough Hallowglade Lightbloom Moonbrook Moonleaf Nightbloom Oakenbough Rainwhisper Silverbough Silverleaf Starbloom Starlight Summerglade Sunpetal Thistlewind Willowshade Windmere Woodwhisper",
    },
    "dark-elf": {
      male: "Azael Draven Eryndor Kaelis Lethar Malek Nythar Raezor Sareth Vhaeron",
      female: "Azyra Driselle Eryssa Kaelira Lethira Malith Nysara Raezra Saryth Vhaela",
      family: "Ashveil Blackthorn Darkbloom Darkweave Duskblade Duskwind Ebonveil Gloamshade Hollowveil Moonshroud Nightbriar Nightfall Nightscar Obsidian Shadowbloom Shadowmere Shadowthorn Silentweb Silvergloom Starless Thornveil Umbraclaw Umbraveil Voidwhisper Webshade",
    },
    dwarf: {
      male: "Balin Borik Dagan Durin Eirik Fargrim Grom Haldor Keldrin Torvik",
      female: "Bera Brynja Dagny Disa Elda Freydis Greta Hilda Kelda Tora",
      family: "Anvilhand Ashbeard Battleforge Blackhammer Boulderback Bronzeheart Coalbeard Coppervein Deepdelver Flintaxe Forgeguard Gemseeker Goldmantle Granitefist Hardstone Ironbeard Ironvein Orebreaker Runebrow Shieldheart Stonebraid Stonehelm Thunderforge Trollbane Warhammer",
    },
    hobbit: {
      male: "Albie Barnaby Cobby Doderic Fenton Halbert Milo Osric Pippin Wilfred",
      female: "Amabel Bess Celandine Dottie Elspeth Hettie Marigold Nellie Poppy Willa",
      family: "Applebrook Barleycorn Bramblefoot Butterbur Ciderhill Cloverfield Copperkettle Cottonvale Dandelion Fernbank Goodbody Greenhill Hedgewick Honeywell Littlebrook Meadowbank Merryweather Mossfoot Oakbottom Puddlefoot Quickwhistle Rosebank Thistledown Underhill Warmhearth",
    },
    lizardman: {
      male: "Akhaz Chokar Drass Hissar Isshek Krazh Moktar Rassik Ssarik Thok",
      female: "Akssha Chassa Drisha Hisska Issara Krazza Moksha Rassha Ssatha Thessa",
      family: "Amberclaw Ashscale Blackfang Bogrunner Brinefang Coldscale Dawncrest Deepmarsh Duskcoil Embercrest Fernscale Flinttail Greenthroat Ironcrest Mirefang Mossback Mudscale Reedstalker Riverclaw Scalehide Sharpcrest Siltwalker Stonejaw Sunscale Thornback",
    },
  };
  const words = value => value.split(" ");
  const choose = values => values[Math.floor(Math.random() * values.length)];
  const allowedSlots = (item, character) => {
    if (!item || item.unavailable) return [];
    if (item.category === "weapon" && item.minimumStrength > (window.Combat?.previewValues?.(character)?.strength ?? character.stats.strength)) return [];
    const restrictions = raceCatalog.find(race => race.id === character.raceId)?.passive?.effects || [];
    if (restrictions.some(effect => effect.type === "equipment-restriction" && effect.category === item.category && (effect.typeName || effect.target) === item.type)) return [];
    if (item.category === "weapon") return item.handed === 2 ? ["primaryHand"] : ["primaryHand", "offHand"];
    if (item.category === "armor") return item.type === "shield" ? ["offHand"] : ["armor"];
    if (item.category === "artifact") return ["artifact1", "artifact2"];
    return [];
  };
  const skillsFor = character => classCatalog.flatMap(definition => (definition.levels || []).filter(rank => rank.level <= (character.classLevels[definition.id] || 0)).flatMap(rank => rank.effects.filter(effect => effect.type === "skill").map(effect => ({ ...effect, key: classSkillKey({ ...effect, classId: definition.id }), source: definition.name }))));
  const spellPriceFor = (character, spell) => {
    const modifiers = window.Combat?.previewClassEffects?.(character)?.spellLearningCostPercentModifiers || [];
    const discount = modifiers.reduce((total, modifier) => {
      const tierMatches = modifier.maxTier == null || spell.tier <= modifier.maxTier;
      const elementMatches = !modifier.element || modifier.element === spell.element;
      return tierMatches && elementMatches ? total + modifier.amount : total;
    }, 0);
    return Math.max(0, Math.floor(toNumber(spell.priceSilver, 0) * (1 - (discount + partySpellLearningDiscount(spell, character.knownSpells)) / 100)));
  };
  const active = () => companions.find(entry => entry.id === selectedId) || null;
  const canLevelUp = character => character.level < LEVEL_CAP && character.exp >= CombatRules.experienceRequired(character.level);
  const preview = companion => window.Combat?.previewValues?.(companion.character) || {
    ...companion.character.stats,
    maxHp: calculateMaxHp(companion.character.stats), maxMana: calculateMaxMana(companion.character.stats), armor: BASE_ARMOR, evasion: BASE_EVASION,
  };
  function makeOffer() {
    const race = choose(raceCatalog.filter(entry => entry.playable && names[entry.id]));
    const gender = Math.random() < .5 ? "male" : "female";
    const culture = names[race.id];
    const name = choose(words(culture[gender])) + " " + choose(words(culture.family));
    const targetLevel = Math.max(1, level - 10);
    const character = defaultCharacter();
    // Recruits get one of the four looks for the gender of their name.
    character.appearance = { gender, variant: Math.floor(Math.random() * 4) };
    character.raceId = race.id; character.level = targetLevel; character.editingEnabled = false;
    character.perkPoints = targetLevel;
    // Recruits carry a common forged weapon of their own level (about a quarter are staffs or wands)
    // and forged body armor of a kind their race can wear.
    const forge = window.ForgeRules;
    const weapon = forge ? getItem(forge.encode(forge.generate({ level: targetLevel, source: "shop", maxRarity: "common" }))) : null;
    const bodyTypes = forge ? forge.ARMOR_IDS.filter(id => forge.ARMORS[id].category !== "shield").sort(() => Math.random() - 0.5) : [];
    let coat = null;
    for (const type of bodyTypes) {
      const candidate = getItem(forge.encode(forge.generateArmor({ type, level: targetLevel, source: "shop", maxRarity: "common" })));
      if (candidate && allowedSlots(candidate, character).includes("armor")) { coat = candidate; break; }
    }
    if (weapon) { character.inventory.push(weapon.id); character.equipment.primaryHand = weapon.id; if (weapon.handed === 2) character.equipment.offHand = weapon.id; }
    if (coat) { character.inventory.push(coat.id); character.equipment.armor = coat.id; }
    for (let index = 1; index < targetLevel; index++) {
      for (const stat of statDefinitions) character.stats[stat.key]++;
      for (let point = 0; point < 5; point++) character.stats[choose(statDefinitions).key]++;
    }
    const id = companionId();
    const offer = { id, name, gender, character };
    const values = preview(offer);
    character.currentHp = values.maxHp; character.currentMana = values.maxMana;
    return offer;
  }
  function makeOffers() {
    if (!raceCatalog.some(race => race.playable && names[race.id])) return [];
    const result = [];
    for (let attempt = 0; result.length < 3 && attempt < 30; attempt++) {
      const offer = makeOffer();
      if (!result.some(existing => existing.name === offer.name) && !companions.some(existing => existing.name === offer.name)) result.push(offer);
    }
    return result;
  }
  function add(offer) {
    if (companions.length >= 3 || companions.some(entry => entry.id === offer.id)) return false;
    companions.push(offer); selectedId = offer.id; render(); saveCharacter(); return true;
  }
  function render(refreshSheet = false) {
    renderImportControls();
    if (!active()) selectedId = companions[0]?.id || null;
    $("companion-count").textContent = `${companions.length} / 3`;
    $("companion-sidebar-list").innerHTML = companions.length ? companions.map(entry => {
      const values = preview(entry);
      const hpPercent = values.maxHp ? Math.max(0, Math.min(100, entry.character.currentHp / values.maxHp * 100)) : 0;
      const manaPercent = values.maxMana ? Math.max(0, Math.min(100, entry.character.currentMana / values.maxMana * 100)) : 0;
      return `<button type="button" class="companion-sidebar-entry${canLevelUp(entry.character) ? " has-level-up" : ""}" data-companion-open="${escapeHtml(entry.id)}" ${canLevelUp(entry.character) ? 'title="Level up available"' : ""}><strong>${escapeHtml(entry.name)}</strong><span>Level ${entry.character.level} · HP ${entry.character.currentHp}/${values.maxHp} · Mana ${entry.character.currentMana}/${values.maxMana}</span><i class="companion-sidebar-meter is-hp"><i style="width:${hpPercent}%"></i></i><i class="companion-sidebar-meter is-mana"><i style="width:${manaPercent}%"></i></i></button>`;
    }).join("") : '<p class="companion-empty">No companions recruited yet.</p>';
    if (!modal.hidden) {
      $("companion-roster").innerHTML = companions.length ? companions.map(entry => {
        const values = preview(entry), race = raceCatalog.find(candidate => candidate.id === entry.character.raceId);
        const hpPercent = values.maxHp ? Math.max(0, Math.min(100, entry.character.currentHp / values.maxHp * 100)) : 0;
        return `<button type="button" data-companion-select="${escapeHtml(entry.id)}" class="${entry.id === selectedId ? "is-selected" : ""}${canLevelUp(entry.character) ? " has-level-up" : ""}" ${canLevelUp(entry.character) ? 'title="Level up available"' : ""}><span class="companion-roster-name">${escapeHtml(entry.name)}</span><small>${escapeHtml(race?.name || "Adventurer")} · Level ${entry.character.level}</small><span class="companion-roster-health"><i style="width:${hpPercent}%"></i></span><small>HP ${entry.character.currentHp} / ${values.maxHp}</small></button>`;
      }).join("") : '<p class="companion-roster-empty">Recruit at an Adventurers Guild, or import a saved character above.</p>';
      if (refreshSheet || $("companion-sheet").dataset.companionId !== selectedId) renderSheet();
    }
  }
  function optionList(source, selected, allowed) {
    return `<option value="">— Empty —</option>` + [...new Set(source)].map(getItem).filter(item => item && allowed(item)).map(item => `<option value="${escapeHtml(item.id)}" ${item.id === selected ? "selected" : ""}>${escapeHtml(item.name)}</option>`).join("");
  }
  const resourcePercent = (current, maximum) => maximum ? Math.max(0, Math.min(100, current / maximum * 100)) : 0;
  const companionDefenseBreakdown = character => {
    if (window.Combat?.previewDefenseBreakdown) return window.Combat.previewDefenseBreakdown(character);
    const current = preview({ character });
    return {
      armor: { base: BASE_ARMOR, modified: BASE_ARMOR, current: current.armor },
      evasion: { base: current.evasion, modified: current.evasion, current: current.evasion },
    };
  };
  function gearDetail(item, occupied = false) {
    if (!item) return '<span class="companion-gear-empty">Empty slot</span>';
    const details = item.category === "weapon"
      ? `Base power ${formatValue(item.basePower || 0)} · ${item.handed === 2 ? "Two-handed" : "One-handed"} · ${itemStatLabel(item.primary)}${item.secondary?.none ? "" : ` / ${itemStatLabel(item.secondary)}`}`
      : item.category === "armor"
        ? `Armor +${formatValue(item.armorValue || 0)} · Evasion -${formatValue(item.evasionDebuff || 0)} · ${itemStatLabel(item.scaling)}`
        : (item.effects || []).map(artifactEffectLabel).join(" · ") || item.description;
    return `<span class="companion-gear-item ${itemRarityClass(item)}"><strong>${escapeHtml(item.name)}</strong><small>${escapeHtml(itemTypeLabel(item.type))} · ${escapeHtml(itemRarityLabel(item.rarity))}${occupied ? " · Occupies both hands" : ""}</small><em title="${escapeHtml(item.description || details)}">${escapeHtml(details)}</em></span>`;
  }
  function renderSheet() {
    const entry = active(), panel = $("companion-sheet");
    if (!entry) { panel.innerHTML = '<div class="companion-sheet-empty"><span aria-hidden="true">♟</span><p>Select a companion to see their sheet.</p></div>'; delete panel.dataset.companionId; return; }
    const openSections = panel.dataset.companionId === entry.id ? [...panel.querySelectorAll("details")].map(section => section.open) : [];
    const c = entry.character, values = preview(entry), defenseValues = companionDefenseBreakdown(c), race = raceCatalog.find(item => item.id === c.raceId);
    const locked = Boolean(window.Combat?.active()), atLevelCap = c.level >= LEVEL_CAP;
    if (!locked) {
      c.currentHp = Math.max(0, Math.min(values.maxHp, c.currentHp));
      c.currentMana = Math.max(0, Math.min(values.maxMana, c.currentMana));
    }
    const required = CombatRules.experienceRequired(c.level), expPercent = atLevelCap ? 100 : Math.max(0, Math.min(100, c.exp / required * 100));
    const twoHanded = getItem(c.equipment.primaryHand)?.handed === 2;
    const gear = equipmentSlotDefinitions.map(slot => {
      const occupied = slot.key === "offHand" && twoHanded, item = getItem(c.equipment[slot.key]);
      return `<article class="companion-gear-slot" data-companion-equipment="${slot.key}"><label><span>${escapeHtml(slot.label)}</span><select data-comp-gear="${slot.key}" ${occupied ? "disabled" : ""}>${occupied ? `<option value="${escapeHtml(c.equipment.primaryHand)}">Occupied by two-handed weapon</option>` : optionList(c.inventory, c.equipment[slot.key], candidate => allowedSlots(candidate, c).includes(slot.key))}</select></label><div class="companion-gear-detail">${gearDetail(item, occupied)}</div></article>`;
    }).join("");
    const statsHtml = statDefinitions.map(stat => `<article class="companion-stat" style="--companion-stat-color:${stat.color}"><header><span>${escapeHtml(stat.short)}</span><b>${escapeHtml(stat.label)}</b></header><div class="companion-stat-value"><span><small>Base</small><strong>${c.stats[stat.key]}</strong></span><span><small>Current</small><strong data-comp-current-stat="${stat.key}">${values[stat.key]}</strong></span><div class="companion-stat-stepper"><button type="button" data-comp-stat="${stat.key}" data-direction="-1" aria-label="Decrease ${escapeHtml(stat.label)}" ${!c.manualAllocations[stat.key] ? "disabled" : ""}>−</button><button type="button" data-comp-stat="${stat.key}" data-direction="1" aria-label="Increase ${escapeHtml(stat.label)}" ${c.availablePoints < 1 ? "disabled" : ""}>+</button></div></div><div class="companion-stat-bonuses"><label><span>Permanent</span><span class="companion-percent-field"><input type="number" step="any" data-comp-bonus="permanentBonuses" data-key="${stat.key}" value="${c.permanentBonuses[stat.key]}" ${editingEnabled ? "" : "disabled title=\"Enable Dev Mode to edit permanent bonuses\""}><i>%</i></span></label><label><span>Temporary</span><span class="companion-percent-field"><input type="number" step="any" data-comp-bonus="temporaryBonuses" data-key="${stat.key}" value="${c.temporaryBonuses[stat.key]}"><i>%</i></span></label></div></article>`).join("");
    const defenses = defenseBonusDefinitions.map(key => `<article class="companion-defense-card is-${key}"><div class="companion-defense-icon" aria-hidden="true">${key === "armor" ? "▣" : "◌"}</div><div class="companion-defense-content"><span class="companion-defense-label">${key}</span><div class="companion-defense-values"><span><small>Base</small><strong data-comp-defense-base="${key}">${defenseValues[key].base}</strong></span><span><small>Modified</small><strong data-comp-defense-modified="${key}">${defenseValues[key].modified}</strong></span><span><small>Current</small><strong data-comp-defense-value="${key}">${defenseValues[key].current}</strong></span></div></div><div class="companion-defense-bonuses"><label><span>Permanent</span><span class="companion-percent-field"><input type="number" step="any" data-comp-bonus="permanentDefenseBonuses" data-key="${key}" value="${c.permanentDefenseBonuses[key]}" ${editingEnabled ? "" : "disabled title=\"Enable Dev Mode to edit permanent bonuses\""}><i>%</i></span></label><label><span>Temporary</span><span class="companion-percent-field"><input type="number" step="any" data-comp-bonus="temporaryDefenseBonuses" data-key="${key}" value="${c.temporaryDefenseBonuses[key]}"><i>%</i></span></label></div><span class="companion-defense-formula">${key === "armor" ? "BASE 5 + EQUIPMENT" : "ATTRIBUTE EVASION × ARMOR TYPE"}</span></article>`).join("");
    const eligibleClasses = Combat.previewClassOptions(c).filter(option => (option.rank || option.met) && option.rank < option.definition.maxLevel).map(option => option.definition);
    const acquiredClasses = Object.entries(c.classLevels).filter(([, rank]) => rank > 0).map(([id, rank]) => ({ id, rank, definition: getClass(id) }));
    const skills = skillsFor(c);
    const passives = [race?.passive ? { ...race.passive, source: race.name } : null, ...classCatalog.flatMap(definition => (definition.levels || []).filter(rank => rank.level <= (c.classLevels[definition.id] || 0)).flatMap(rank => rank.effects.filter(effect => effect.type === "passive").map(effect => ({ ...effect, source: definition.name }))))].filter(Boolean);
    const settlement = WorldRules.settlementAt(worldState.position);
    const availableSpellChoices = editingEnabled ? spells : settlement ? (window.Shop?.stockFor(settlement)?.spells || []).map(getSpell).filter(Boolean) : [];
    const availableTransfer = inventory.filter((id, index) => inventory.indexOf(id) === index && inventory.filter(value => value === id).length > (requiredEquipmentCounts(equipment)[id] || 0)).map(getItem).filter(Boolean);
    const inventoryGroups = [...new Set(c.inventory)].map(id => ({ item: getItem(id), quantity: c.inventory.filter(value => value === id).length })).filter(group => group.item);
    const racePortrait = race?.icon ? `<img src="${escapeHtml(race.icon)}" alt="" /><span aria-hidden="true">♟</span>` : '<span aria-hidden="true">♟</span>';
    panel.innerHTML = `<fieldset class="companion-sheet-fields" ${locked ? "disabled" : ""}>
      <div class="companion-sheet-heading"><div class="companion-identity"><div class="companion-portrait">${racePortrait}</div><div><p class="eyebrow">${escapeHtml(race?.name || "Companion")} · ${escapeHtml(entry.gender)}</p><h3>${escapeHtml(entry.name)}</h3><p title="${escapeHtml(race?.passive?.description || "Adventuring companion")}">Ancestry passive · ${escapeHtml(race?.passive?.name || "None")}</p></div></div><button class="companion-dismiss-button" type="button" id="companion-dismiss">Dismiss companion</button></div>
      ${locked ? '<p class="companion-lock-notice">Combat is active. Companion sheets are read-only until the battle ends.</p>' : ""}
      <nav class="companion-sheet-nav" aria-label="Companion sheet sections"><a href="#companion-section-attributes" data-companion-jump="companion-section-attributes"><span aria-hidden="true">◆</span> Attributes</a><a href="#companion-section-equipment" data-companion-jump="companion-section-equipment"><span aria-hidden="true">▣</span> Equipment</a><a href="#companion-section-training" data-companion-jump="companion-section-training"><span aria-hidden="true">✦</span> Training</a></nav>
      <div class="companion-summary"><div class="companion-level"><span>Level</span><strong>${c.level}</strong><small>Cap ${LEVEL_CAP}</small></div><div class="companion-experience"><div><span>Experience</span><strong>${atLevelCap ? "Maximum level" : `${formatValue(c.exp)} / ${formatValue(required)} EXP`}</strong></div><span class="companion-exp-track" role="progressbar" aria-label="Experience toward next level" aria-valuemin="0" aria-valuemax="${atLevelCap ? 1 : required}" aria-valuenow="${atLevelCap ? 1 : Math.min(c.exp, required)}"><i style="width:${expPercent}%"></i></span></div><div class="companion-point-summary"><span><small>Stat points</small><strong>${c.availablePoints}</strong></span><span><small>Perk points</small><strong>${c.perkPoints}</strong></span></div><button class="companion-level-up" type="button" id="companion-level-up" ${atLevelCap || c.exp < required ? "disabled" : ""}>Level up</button></div>
      <div class="companion-resources"><article class="companion-resource-card is-hp" data-comp-resource-card="currentHp"><div class="companion-resource-icon" aria-hidden="true">♡</div><div class="companion-resource-control"><span>Hit points</span><strong class="companion-resource-edit"><button type="button" data-comp-resource-adjust="currentHp" data-direction="-1" aria-label="Decrease companion Hit Points">−</button><input type="number" min="0" max="${values.maxHp}" data-comp-resource="currentHp" value="${c.currentHp}" aria-label="Current companion Hit Points"><i>/</i><b class="companion-resource-max">${values.maxHp}</b><button type="button" data-comp-resource-adjust="currentHp" data-direction="1" aria-label="Increase companion Hit Points">+</button></strong></div><span class="companion-resource-track"><i style="width:${resourcePercent(c.currentHp, values.maxHp)}%"></i></span></article><article class="companion-resource-card is-mana" data-comp-resource-card="currentMana"><div class="companion-resource-icon" aria-hidden="true">◈</div><div class="companion-resource-control"><span>Mana</span><strong class="companion-resource-edit"><button type="button" data-comp-resource-adjust="currentMana" data-direction="-1" aria-label="Decrease companion Mana">−</button><input type="number" min="0" max="${values.maxMana}" data-comp-resource="currentMana" value="${c.currentMana}" aria-label="Current companion Mana"><i>/</i><b class="companion-resource-max">${values.maxMana}</b><button type="button" data-comp-resource-adjust="currentMana" data-direction="1" aria-label="Increase companion Mana">+</button></strong></div><span class="companion-resource-track"><i style="width:${resourcePercent(c.currentMana, values.maxMana)}%"></i></span></article><div class="companion-combat-values"><span><small>Armor</small><strong data-comp-overview="armor">${values.armor}</strong></span><span><small>Evasion</small><strong data-comp-overview="evasion">${values.evasion}</strong></span><span><small>Speed</small><strong data-comp-overview="speed">${values.speed}</strong></span></div></div>
      <details id="companion-section-attributes" open><summary><span><small>CORE ATTRIBUTES & DEFENSES</small>The eight facets · Guard and grace</span><i aria-hidden="true">⌄</i></summary><div class="companion-section-intro"><p>Base values include allocated points. Current values include ancestry, class, item, permanent, and temporary modifiers.</p><span>${c.availablePoints} points available</span></div><div class="companion-subheading"><div><p class="eyebrow">DEFENSIVE MEASURES</p><h4>Guard and grace</h4></div></div><div class="companion-defenses">${defenses}</div><div class="companion-subheading companion-attribute-heading"><div><p class="eyebrow">CORE ATTRIBUTES</p><h4>The eight facets</h4></div><span>${c.availablePoints} unspent</span></div><div class="companion-stats">${statsHtml}</div></details>
      <details id="companion-section-equipment" open><summary><span><small>EQUIPMENT & INVENTORY</small>Worn and carried</span><i aria-hidden="true">⌄</i></summary><div class="companion-gear">${gear}</div><div class="companion-subheading companion-inventory-heading"><div><p class="eyebrow">CARRIED ITEMS</p><h4>Companion inventory</h4></div><span>${c.inventory.length} item${c.inventory.length === 1 ? "" : "s"}</span></div><div class="companion-transfer companion-action-row"><label><span>Give an item from your inventory</span><select id="companion-give-item"><option value="">Select item</option>${availableTransfer.map(item => `<option value="${escapeHtml(item.id)}">${escapeHtml(item.name)}</option>`).join("")}</select></label><button type="button" id="companion-give">Give item</button></div><div class="companion-owned companion-inventory-list">${inventoryGroups.map(({ item, quantity }) => `<article class="companion-owned-item ${itemRarityClass(item)}"><div><strong>${escapeHtml(item.name)} <b>×${quantity}</b></strong><small>${escapeHtml(itemCategoryLabel(item.category))} · ${escapeHtml(itemTypeLabel(item.type))} · ${escapeHtml(itemRarityLabel(item.rarity))}</small><p>${escapeHtml(item.description || "No description provided.")}</p></div><button type="button" data-comp-take="${escapeHtml(item.id)}">Take one</button></article>`).join("") || '<div class="companion-list-empty">No items carried.</div>'}</div></details>
      <details id="companion-section-training"><summary><span><small>TRAINING & ABILITIES</small>Classes, passives, skills, and spells</span><i aria-hidden="true">⌄</i></summary><div class="companion-training-grid"><section><div class="companion-subheading"><div><p class="eyebrow">CLASSES</p><h4>Training</h4></div><span>${c.perkPoints} perk point${c.perkPoints === 1 ? "" : "s"}</span></div><div class="companion-class-list">${acquiredClasses.map(({ id, rank, definition }) => { const colorClass = ["sharmat", "vampire-lord", "joker"].includes(id) ? "is-legendary" : ["dragonkin", "vampire", "chaos-cultist", "champion"].includes(id) ? "is-purple" : ["jack-of-all-trades", "spellblade", "spell-sniper", "salesman", "slayer", "wise-man"].includes(id) ? "is-blue" : ""; return `<span class="${colorClass}"><strong>${escapeHtml(definition?.name || id)}</strong><small>Level ${rank} / ${definition?.maxLevel || rank}</small></span>`; }).join("") || '<div class="companion-list-empty">No classes acquired.</div>'}</div><div class="companion-transfer companion-action-row"><label><span>Acquire or improve a class</span><select id="companion-class-choice"><option value="">Choose class</option>${eligibleClasses.map(definition => `<option value="${escapeHtml(definition.id)}">${escapeHtml(definition.name)} → ${(c.classLevels[definition.id] || 0) + 1}/${definition.maxLevel}</option>`).join("")}</select></label><button type="button" id="companion-class-up" ${c.perkPoints < 1 ? "disabled" : ""}>Spend perk point</button></div></section><section><div class="companion-subheading"><div><p class="eyebrow">PASSIVES</p><h4>Always available</h4></div></div><div class="companion-ability-list">${passives.map(passive => `<article><div><strong>${escapeHtml(passive.name || "Passive")}</strong><small>${escapeHtml(passive.source || "Class")}${passive.condition ? ` · ${escapeHtml(passive.condition)}` : ""}</small><p>${escapeHtml(passive.description || "No description provided.")}</p></div></article>`).join("") || '<div class="companion-list-empty">No passives unlocked.</div>'}</div></section></div><div class="companion-training-grid companion-training-lower"><section><div class="companion-subheading"><div><p class="eyebrow">SKILLS</p><h4>Combat techniques</h4></div></div><div class="companion-ability-list companion-owned">${skills.map(skill => { const state = c.activeSkills[skill.key]; return `<article><div><strong>${escapeHtml(skill.name)}</strong><small>${escapeHtml(skill.source)}</small><p>${escapeHtml(skill.description || "No description provided.")}</p></div><button type="button" data-comp-skill="${escapeHtml(skill.key)}" ${state?.lastUsedDay === todayKey() && !state?.active ? "disabled" : ""}>${state?.active ? "Deactivate" : "Activate"}</button></article>`; }).join("") || '<div class="companion-list-empty">No skills unlocked.</div>'}</div></section><section><div class="companion-subheading"><div><p class="eyebrow">SPELLS</p><h4>Known repertoire</h4></div><span>${c.knownSpells.length} known</span></div><div class="companion-ability-list companion-owned">${c.knownSpells.map(id => { const spell = getSpell(id); return `<article class="${spell ? `spell-element-${escapeHtml(spell.element)}` : ""}"><div><strong>${escapeHtml(spell?.name || id)}</strong><small>${spell ? `${escapeHtml(spellElementLabel(spell.element))} · ${escapeHtml(spellTierLabel(spell.tier))}` : "Unavailable spell"}</small><p>${escapeHtml(spell?.description || "The source definition for this spell is unavailable.")}</p></div><button type="button" data-comp-unlearn="${escapeHtml(id)}">Unlearn</button></article>`; }).join("") || '<div class="companion-list-empty">No spells known.</div>'}</div><div class="companion-transfer companion-action-row"><label><span>${editingEnabled ? "Teach a spell" : "Learn at a castle city's Magic Tower"}</span><select id="companion-spell-choice"><option value="">Choose spell</option>${availableSpellChoices.filter(spell => !c.knownSpells.includes(spell.id)).map(spell => `<option value="${escapeHtml(spell.id)}">${escapeHtml(spell.name)} · ${escapeHtml(ShopRules.formatPrice(spellPriceFor(c, spell)))}</option>`).join("")}</select></label><button type="button" id="companion-learn-spell" ${settlement?.kind !== "Castle city" && !editingEnabled ? "disabled title=\"Visit the Magic Tower of a castle city\"" : ""}>${editingEnabled ? "Teach · Dev Mode" : "Learn spell"}</button></div></section></div></details>
      </fieldset>`;
    panel.dataset.companionId = entry.id;
    panel.querySelectorAll(".companion-class-list > span").forEach((badge, index) => {
      if (acquiredClasses[index]?.definition?.color === "blue" || ["diviner", "general"].includes(acquiredClasses[index]?.id)) badge.classList.add("is-blue");
      if (acquiredClasses[index]?.definition?.color === "purple") badge.classList.add("is-purple");
      if (acquiredClasses[index]?.definition?.color === "gold") badge.classList.add("is-legendary");
    });
    panel.querySelectorAll("details").forEach((section, index) => { if (openSections[index] !== undefined) section.open = openSections[index]; });
    panel.querySelector(".companion-summary").insertAdjacentHTML("beforeend", `<div class="companion-dev-tools" data-dev-only ${editingEnabled ? "" : "hidden"}><span>Dev Mode</span><label class="companion-dev-exp" data-dev-only ${editingEnabled ? "" : "hidden"}><span>Add companion EXP</span><input id="companion-exp-input" type="number" min="0" step="1" placeholder="Amount"></label><button type="button" id="companion-add-exp" data-dev-only ${editingEnabled ? "" : "hidden"}>Add EXP</button></div>`);
    const choice = c.pendingClassSpellChoice;
    if (choice?.candidateIds?.length) panel.querySelector(".companion-sheet-fields").insertAdjacentHTML("beforeend", `<div class="companion-spell-pick"><p class="eyebrow">CLASS SPELL CHOICE · PICK ${choice.requiredPicks}</p>${choice.candidateIds.map(id => `<label><input type="checkbox" data-comp-spell-pick="${escapeHtml(id)}">${escapeHtml(getSpell(id)?.name || id)}</label>`).join("")}<button type="button" id="companion-confirm-spells">Learn selected spells</button></div>`);
    panel.querySelectorAll("[data-comp-skill]").forEach(button => {
      const skill = skills.find(entry => entry.key === button.dataset.compSkill);
      if (!skill) return;
      const state = c.activeSkills[skill.key];
      button.disabled = true;
      button.textContent = state?.active ? "Active in combat" : "Use in combat";
    });
    window.CompanionSheetDetails?.mount(entry, panel);
  }
  function changed() { render(true); window.Combat?.onSheetChange(); saveCharacter(); }
  function updateSheetInPlace(entry, slot = null) {
    const panel = $("companion-sheet"), c = entry.character, values = preview(entry), defenseValues = companionDefenseBreakdown(c);
    c.currentHp = Math.max(0, Math.min(values.maxHp, c.currentHp));
    c.currentMana = Math.max(0, Math.min(values.maxMana, c.currentMana));
    for (const stat of statDefinitions) panel.querySelector(`[data-comp-current-stat="${stat.key}"]`).textContent = values[stat.key];
    for (const [key, maximum] of [["currentHp", values.maxHp], ["currentMana", values.maxMana]]) {
      const card = panel.querySelector(`[data-comp-resource-card="${key}"]`), input = card.querySelector("input");
      input.max = String(maximum); input.value = String(c[key]);
      card.querySelector(".companion-resource-max").textContent = maximum;
      card.querySelector(".companion-resource-track > i").style.width = resourcePercent(c[key], maximum) + "%";
    }
    for (const key of ["armor", "evasion", "speed"]) panel.querySelector(`[data-comp-overview="${key}"]`).textContent = values[key];
    for (const key of defenseBonusDefinitions) {
      panel.querySelector(`[data-comp-defense-base="${key}"]`).textContent = defenseValues[key].base;
      panel.querySelector(`[data-comp-defense-modified="${key}"]`).textContent = defenseValues[key].modified;
      panel.querySelector(`[data-comp-defense-value="${key}"]`).textContent = defenseValues[key].current;
    }
    if (slot === "primaryHand") {
      const offHand = panel.querySelector('[data-comp-gear="offHand"]');
      const twoHanded = getItem(c.equipment.primaryHand)?.handed === 2;
      offHand.disabled = twoHanded;
      offHand.innerHTML = twoHanded
        ? `<option value="${escapeHtml(c.equipment.primaryHand)}">Occupied by two-handed weapon</option>`
        : optionList(c.inventory, c.equipment.offHand, item => allowedSlots(item, c).includes("offHand"));
    }
    panel.querySelectorAll("[data-companion-equipment]").forEach(card => {
      const key = card.dataset.companionEquipment;
      card.querySelector(".companion-gear-detail").innerHTML = gearDetail(getItem(c.equipment[key]), key === "offHand" && getItem(c.equipment.primaryHand)?.handed === 2);
    });
    window.CompanionSheetDetails?.refreshEquipment(entry, panel);
    render();
    saveCharacter();
  }
  function open(id = null) {
    if (id) selectedId = id;
    modal.hidden = false; render(true); updateModalState(); modal.querySelector(".modal-close").focus();
  }
  function close() { clearImport(); window.CompanionSheetDetails?.close(); modal.hidden = true; updateModalState(); $("companions-open-button").focus(); }
  $("companions-open-button").addEventListener("click", () => open());
  $("companion-import").addEventListener("click", () => {
    if (importLoading || importReason()) return;
    if (window.statFickDesktop?.loadCharacter) loadImport(() => window.statFickDesktop.loadCharacter());
    else $("companion-import-file").click();
  });
  $("companion-import-file").addEventListener("change", event => {
    const file = event.target.files?.[0];
    if (!file) return;
    loadImport(async () => {
      if (file.size > importLimit) throw new Error("Character files must be smaller than 10 MB.");
      return { name: file.name, text: await file.text() };
    });
  });
  $("companion-import-panel").addEventListener("submit", confirmImport);
  $("companion-import-cancel").addEventListener("click", () => { clearImport(); $("companion-status").textContent = "Import cancelled. No companion was added."; $("companion-import").focus(); });
  $("companion-sidebar-list").addEventListener("click", event => { const button = event.target.closest("[data-companion-open]"); if (button) open(button.dataset.companionOpen); });
  $("companion-roster").addEventListener("click", event => { const button = event.target.closest("[data-companion-select]"); if (button) { selectedId = button.dataset.companionSelect; render(true); } });
  modal.querySelectorAll("[data-companions-close]").forEach(button => button.addEventListener("click", close));
  modal.addEventListener("keydown", event => { if (event.key === "Escape") { event.preventDefault(); close(); } });
  $("companion-sheet").addEventListener("change", event => {
    const entry = active(); if (!entry || window.Combat?.active()) return;
    const c = entry.character, field = event.target;
    if (field.dataset.compResource) c[field.dataset.compResource] = Math.max(0, Math.min(preview(entry)[field.dataset.compResource === "currentHp" ? "maxHp" : "maxMana"], toInteger(field.value, 0)));
    else if (field.dataset.compBonus && field.dataset.key) c[field.dataset.compBonus][field.dataset.key] = toNumber(field.value, 0);
    else if (field.dataset.compGear) {
      const slot = field.dataset.compGear, item = getItem(field.value);
      if (item && (!c.inventory.includes(item.id) || !allowedSlots(item, c).includes(slot))) return render(true);
      if (slot === "offHand" && getItem(c.equipment.primaryHand)?.handed === 2) return render(true);
      const candidate = { ...c.equipment, [slot]: item?.id || null };
      if (slot === "primaryHand") candidate.offHand = item?.handed === 2 ? item.id : getItem(c.equipment.primaryHand)?.handed === 2 ? null : candidate.offHand;
      if (Object.entries(requiredEquipmentCounts(candidate)).some(([id, count]) => c.inventory.filter(owned => owned === id).length < count)) return render(true);
      c.equipment = candidate;
    } else return;
    updateSheetInPlace(entry, field.dataset.compGear || null);
  });
  $("companion-sheet").addEventListener("click", event => {
    const jump = event.target.closest("[data-companion-jump]");
    if (jump) {
      event.preventDefault();
      const section = document.getElementById(jump.dataset.companionJump);
      if (section) { section.open = true; section.scrollIntoView({ behavior: "smooth", block: "start" }); }
      return;
    }
    const entry = active(); if (!entry || window.Combat?.active()) return;
    const c = entry.character, button = event.target.closest("button"); if (!button || button.disabled) return;
    if (button.dataset.compResourceAdjust) {
      const key = button.dataset.compResourceAdjust, maximum = preview(entry)[key === "currentHp" ? "maxHp" : "maxMana"];
      c[key] = Math.max(0, Math.min(maximum, c[key] + Number(button.dataset.direction)));
      updateSheetInPlace(entry); return;
    }
    if (button.id === "companion-dismiss") {
      if (!window.confirm(`Dismiss ${entry.name}? Their equipment will be returned to your inventory.`)) return;
      inventory.push(...c.inventory); companions = companions.filter(item => item.id !== entry.id); selectedId = null; recruitOffers = [];
      changed(); window.Guild?.render(); return;
    }
    if (button.id === "companion-add-exp") {
      if (!editingEnabled) return;
      const amount = toInteger($("companion-exp-input").value, 0);
      if (amount <= 0) return;
      c.exp += amount;
    } else if (button.id === "companion-level-up") {
      const required = CombatRules.experienceRequired(c.level);
      if (c.level >= LEVEL_CAP || c.exp < required) return;
      c.exp -= required; c.level++; c.availablePoints += 5; c.perkPoints++;
      statDefinitions.forEach(stat => { c.stats[stat.key]++; });
      const values = preview(entry); c.currentHp = values.maxHp; c.currentMana = values.maxMana;
    } else if (button.dataset.compStat) {
      const key = button.dataset.compStat, delta = Number(button.dataset.direction);
      if (delta > 0 && c.availablePoints > 0) { c.stats[key]++; c.manualAllocations[key]++; c.availablePoints--; }
      else if (delta < 0 && c.manualAllocations[key] > 0) { c.stats[key]--; c.manualAllocations[key]--; c.availablePoints++; }
      else return;
    } else if (button.id === "companion-give") {
      const id = $("companion-give-item").value, free = inventory.filter(value => value === id).length - (requiredEquipmentCounts(equipment)[id] || 0);
      if (!id || free < 1) return;
      inventory.splice(inventory.indexOf(id), 1); c.inventory.push(id); renderOwnedItems();
    } else if (button.dataset.compTake) {
      const id = button.dataset.compTake, index = c.inventory.indexOf(id);
      if (index < 0 || c.inventory.filter(owned => owned === id).length <= (requiredEquipmentCounts(c.equipment)[id] || 0)) return;
      c.inventory.splice(index, 1);
      for (const slot of equipmentSlotDefinitions) if (c.equipment[slot.key] === id && !c.inventory.includes(id)) c.equipment[slot.key] = null;
      inventory.push(id); renderOwnedItems();
    } else if (button.id === "companion-class-up") {
      const id = $("companion-class-choice").value, definition = getClass(id);
      const option = Combat.previewClassOptions(c).find(option => option.definition.id === id);
      if (!option || option.reason || (!option.rank && !option.met)) return;
      if (!definition || c.perkPoints < 1 || c.level < definition.requirements.level || (c.classLevels[id] || 0) >= definition.maxLevel || ![...$("companion-class-choice").options].some(option => option.value === id)) return;
      const rank = (c.classLevels[id] || 0) + 1; c.classLevels[id] = rank; c.perkPoints--;
      const requirement = definition.requirements.other;
      if (rank === 1 && requirement?.type === "inventory-item" && requirement.consume === true) {
        c.inventory.splice(c.inventory.indexOf(requirement.itemId), 1);
        for (const slot of equipmentSlotDefinitions) if (c.equipment[slot.key] === requirement.itemId && !c.inventory.includes(requirement.itemId)) c.equipment[slot.key] = null;
        if (requirement.skillCharge) {
          const state = c.activeSkills[requirement.skillCharge] || {};
          c.activeSkills[requirement.skillCharge] = { ...state, active: false, charges: Math.max(0, toInteger(state.charges, 0)) + 1 };
        }
      }
      for (const effect of definition.levels.find(levelEntry => levelEntry.level === rank)?.effects || []) {
        if (effect.type === "grant-and-equip-item" && getItem(effect.itemId)) {
          const item = getItem(effect.itemId), slot = effect.slot || allowedSlots(item, c)[0];
          c.inventory.push(item.id);
          if (slot && allowedSlots(item, c).includes(slot) && !(slot === "offHand" && getItem(c.equipment.primaryHand)?.handed === 2)) {
            if (slot === "primaryHand" && getItem(c.equipment.primaryHand)?.handed === 2) c.equipment.offHand = null;
            c.equipment[slot] = item.id;
            if (slot === "primaryHand" && item.handed === 2) c.equipment.offHand = item.id;
          }
        }
        if (effect.type === "choose-random-spells") {
          const excluded = new Set(effect.excludedElements || []);
          const pool = spells.filter(spell => spell.tier === (effect.tier || 1) && !excluded.has(spell.element) && !c.knownSpells.includes(spell.id));
          const shuffled = [...pool];
          for (let index = shuffled.length - 1; index > 0; index--) { const other = Math.floor(Math.random() * (index + 1)); [shuffled[index], shuffled[other]] = [shuffled[other], shuffled[index]]; }
          const candidateIds = shuffled.slice(0, effect.options || 5).map(spell => spell.id);
          if (candidateIds.length) c.pendingClassSpellChoice = { classId: definition.id, effectId: effect.id || "spell-choice", candidateIds, selectedIds: [], requiredPicks: Math.min(effect.picks || 2, candidateIds.length), optionCount: effect.options || 5 };
        }
        if (effect.type === "choose-stat-permanent-percent") {
          const choices = (effect.options || []).filter(key => bonusDefinitions.includes(key));
          if (choices.length) {
            const labels = choices.map(key => statDefinitions.find(stat => stat.key === key)?.label || key);
            const response = window.prompt(`Choose a stat for ${entry.name}'s ${definition.name} level ${rank}: ${labels.join(" or ")}.`, labels[0]);
            const index = labels.findIndex(label => slugify(label) === slugify(response || labels[0]));
            const selected = choices[index >= 0 ? index : 0];
            c.permanentBonuses[selected] += toNumber(effect.amount, 0);
            (c.classProgress.statChoiceBonuses ||= {})[definition.id + ":" + rank + ":" + (effect.id || (definition.levels.find(entry => entry.level === rank)?.effects || []).indexOf(effect))] = { stat: selected, amount: toNumber(effect.amount, 0) };
          }
        }
      }
    } else if (button.id === "companion-confirm-spells") {
      const choice = c.pendingClassSpellChoice;
      if (!choice) return;
      const selected = [...$("companion-sheet").querySelectorAll("[data-comp-spell-pick]:checked")].map(input => input.dataset.compSpellPick);
      if (selected.length !== choice.requiredPicks || selected.some(id => !choice.candidateIds.includes(id))) { $("companion-status").textContent = `Pick exactly ${choice.requiredPicks} spells.`; return; }
      c.knownSpells.push(...selected.filter(id => !c.knownSpells.includes(id)));
      c.completedClassChoices.push(choice.classId + ":" + choice.effectId);
      c.pendingClassSpellChoice = null;
    } else if (button.dataset.compUnlearn) {
      const id = button.dataset.compUnlearn;
      if (!window.confirm(`Unlearn ${getSpell(id)?.name || id} for ${entry.name}?`)) return;
      c.knownSpells = c.knownSpells.filter(value => value !== id);
    } else if (button.id === "companion-learn-spell") {
      const spell = getSpell($("companion-spell-choice").value);
      if (!spell || c.knownSpells.includes(spell.id)) return;
      if (!editingEnabled) {
        if (!WorldRules.settlementAt(worldState.position)) return;
        const stock = window.Shop?.stockFor(WorldRules.settlementAt(worldState.position));
        if (!stock?.spells.includes(spell.id)) { $("companion-status").textContent = "That spell is not taught at this Magic Tower."; return; }
        const remaining = ShopRules.spend(currencies, spellPriceFor(c, spell));
        if (!remaining) { $("companion-status").textContent = "Not enough coin to learn that spell."; return; }
        currencies = remaining;
        document.querySelectorAll(".currency-input").forEach(input => { input.value = currencies[input.dataset.currency]; });
      }
      c.knownSpells.push(spell.id);
    } else if (button.dataset.compSkill) {
      return;
    } else return;
    changed();
  });
  function restoreAll() {
    for (const entry of companions) {
      const values = preview(entry);
      entry.character.currentHp = values.maxHp; entry.character.currentMana = values.maxMana;
    }
    render();
  }
  window.Companions = { makeOffers, add, render, open, preview, restoreAll, allowedSlots, spellPriceFor, onCharacterApplied() { clearImport(); window.CompanionSheetDetails?.close(); modal.hidden = true; selectedId = null; render(); updateModalState(); } };
  render();
})();

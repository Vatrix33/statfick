async function runCompanionClassFeatureChecks() {
  const $ = id => document.getElementById(id);
  let checks = 0;
  const assert = (condition, message) => { if (!condition) throw Error(message); checks++; };
  const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
  Combat.restore(null);
  applyCharacter(normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: 30, perkPoints: 30 }));
  const warrior = getClass("warrior");
  showClassDetails(warrior);
  let detail = $("class-detail-panel");
  assert(detail.querySelectorAll(".class-feature-ability").length === 2, "Main class features expand the passive and skill");
  assert(detail.textContent.includes("When fighting 2 or more enemies, you gain a 5% boost to armor."), "Main menu shows the passive's complete effect, not just its name");
  assert(detail.textContent.includes("Armor +5%"), "Single-object passive effects have a readable numerical summary");
  assert(detail.textContent.includes("Armor permanent bonus −15%") && detail.textContent.includes("Strength permanent bonus +10%"), "Skill effects include all bonuses and penalties");
  assert(detail.textContent.includes("Once per day") && detail.textContent.includes("3 turns"), "Skill details include cooldown and duration");
  assert(detail.querySelectorAll(".is-locked .class-feature-ability").length === 2, "Future abilities remain inspectable before acquisition");

  let abilityCount = 0;
  for (const definition of classCatalog) {
    showClassDetails(definition);
    const abilities = definition.levels.flatMap(rank => rank.effects).filter(effect => ["skill", "passive"].includes(effect.type));
    const rendered = [...detail.querySelectorAll(".class-feature-ability")];
    assert(rendered.length === abilities.length, definition.name + " renders every passive and skill");
    abilities.forEach((ability, index) => {
      assert(rendered[index].textContent.includes(ability.name), definition.name + " includes its ability name");
      assert(!ability.description || rendered[index].textContent.includes(ability.description), definition.name + " includes the full ability description");
    });
    assert(!detail.textContent.includes("undefined") && !detail.textContent.includes("NaN"), definition.name + " has readable effect values");
    abilityCount += abilities.length;
  }
  const htmlProbe = document.createElement("div");
  htmlProbe.innerHTML = classLevelAbilityDetails({ effects: [{ type: "skill", name: '<img src=x onerror="alert(1)">', description: "<script>bad()</script>", cooldown: "none", effects: [{ type: "custom", description: "<em>Only text</em>" }], onHitEffects: [{ type: "defense-flat", target: "armor", amount: -2 }], onExpireEffects: [{ type: "set-resources-flat", hp: 1, mana: 0 }] }] });
  assert(!htmlProbe.querySelector("img,script,em"), "Editable class data is safely escaped");
  assert(htmlProbe.textContent.includes("On hit:") && htmlProbe.textContent.includes("When it ends:"), "Triggered effects retain their activation context");

  const offered = Companions.makeOffers()[0];
  offered.character = normalizeCharacter({ ...defaultCharacter(), raceId: "human", level: 30, perkPoints: 4, classLevels: { warrior: 3 } });
  assert(Companions.add(offered), "Test companion recruited");
  const member = companions[0], mainBefore = JSON.stringify({ perkPoints, classLevels, stats });
  const choose = id => $("companion-sheet").querySelector(`[data-comp-class-select="${id}"]`).click();
  Companions.open(member.id);
  $("companion-section-training").open = true;
  choose("warrior");
  const toolbar = $("companion-sheet").querySelector(".companion-class-actions");
  assert(toolbar && toolbar.contains($("companion-class-up")), "Training action is in the class toolbar");
  assert(!$("companion-class-up").closest(".companion-class-detail"), "Training button is no longer buried below class features");
  assert(Boolean(toolbar.compareDocumentPosition($("companion-sheet").querySelector(".companion-class-browser")) & Node.DOCUMENT_POSITION_FOLLOWING), "Training action precedes all three class columns");
  assert($("companion-sheet").querySelectorAll("#companion-class-up").length === 1, "Only one class training action exists");
  assert($("companion-class-choice").value === "warrior" && !$("companion-class-up").disabled, "Toolbar targets the selected learned class");
  assert($("companion-sheet").querySelector(".companion-class-detail").textContent.includes("Armor permanent bonus −15%"), "Companion menu shows the same full skill effects as the main menu");
  toolbar.scrollIntoView({ block: "start" });
  $("companion-sheet").scrollTop += 250;
  await wait(60);
  const sheetBounds = $("companion-sheet").getBoundingClientRect(), buttonBounds = $("companion-class-up").getBoundingClientRect();
  assert(buttonBounds.top >= sheetBounds.top && buttonBounds.bottom <= sheetBounds.bottom, "Training button remains visible while scrolling through features");
  assert(toolbar.getBoundingClientRect().top >= $("companion-sheet").querySelector(".companion-sheet-nav").getBoundingClientRect().bottom, "Sticky training toolbar does not overlap the sheet navigation");
  assert(document.elementFromPoint(buttonBounds.left + buttonBounds.width / 2, buttonBounds.top + buttonBounds.height / 2) === $("companion-class-up"), "Visible training action is directly clickable and not covered by another panel");
  $("companion-class-up").click();
  assert(member.character.classLevels.warrior === 4 && member.character.perkPoints === 3, "Toolbar levels up the companion and spends exactly one perk point");
  assert($("companion-section-training").open, "Training section stays expanded after leveling up");
  $("companion-class-up").click();
  assert(member.character.classLevels.warrior === 5 && $("companion-class-up").disabled, "Maximum-level class cannot be advanced");
  choose("archer");
  assert($("companion-class-up").textContent.includes("Acquire class") && $("companion-class-choice").value === "archer", "Selecting an available class switches the action to acquire");
  $("companion-class-up").click();
  assert(member.character.classLevels.archer === 1 && member.character.perkPoints === 1, "Acquisition gives the chosen class at level one");
  $("companion-class-up").click();
  assert(member.character.classLevels.archer === 2 && member.character.perkPoints === 0 && $("companion-class-up").disabled, "Class training disables when perk points run out");
  assert($("companion-class-action-reason").textContent.includes("free perk point"), "Disabled training explains the missing perk point");
  assert(JSON.stringify({ perkPoints, classLevels, stats }) === mainBefore, "Companion training never alters main-character progression");
  const search = $("companion-sheet").querySelector("[data-comp-class-search]");
  search.value = "warrior"; search.dispatchEvent(new Event("input", { bubbles: true }));
  advanceWorldTime(60); Companions.render();
  assert(search === $("companion-sheet").querySelector("[data-comp-class-search]") && search.value === "warrior", "Background time keeps the selected class and search stable");

  $("companions-modal").querySelector(".modal-close").click();
  member.character.perkPoints = 1;
  assert(await Combat.startEncounter(["Goblin"]), "Combat read-only test starts");
  $("combat-sheet-button").click();
  Companions.open(member.id); $("companion-section-training").open = true;
  choose("archer");
  assert($("companion-class-up").disabled && $("companion-class-action-reason").textContent.includes("combat"), "Toolbar cannot change classes during combat");
  $("companions-modal").querySelector(".modal-close").click(); Combat.restore(null);

  Companions.open(member.id); $("companion-section-training").open = true; choose("warrior");
  $("companion-sheet").querySelector(".companion-class-actions").scrollIntoView({ block: "start" });
  await wait(700);
  console.log(JSON.stringify({ passed: true, checks, classes: classCatalog.length, abilities: abilityCount }));
  return { passed: true, checks, classes: classCatalog.length, abilities: abilityCount };
}

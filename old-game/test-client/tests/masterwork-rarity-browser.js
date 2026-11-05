async function runMasterworkRarityChecks() {
  const assert = (condition, message) => { if (!condition) throw Error(message); };
  await waitForSourceFiles();
  const special = items.filter(item => isMasterworkEquipment(item) || item.category === "key-item");
  // 31 masterworks since the Masterwork Elementals (2026-11-05), and five key items.
  assert(special.length === 36 && special.every(item => item.rarity === "masterwork"), "All 31 masterwork items and five key items load as Masterwork");
  assert(normalizeRarity("Masterwork") === "masterwork" && itemRarityLabel("masterwork") === "Masterwork", "Masterwork parses and displays correctly");
  const original = normalizeCharacter(defaultCharacter());
  const main = defaultCharacter(); main.raceId = "human";
  main.inventory = ["weapon-royal-trident-meele", "key-item-phylactery-core"];
  main.equipment.primaryHand = main.inventory[0]; main.equipment.offHand = main.inventory[0];
  applyCharacter(normalizeCharacter(main));
  const saved = normalizeCharacter(backupPayload().character);
  assert(saved.inventory.every(id => getItem(id)?.rarity === "masterwork") && saved.equipment.primaryHand === saved.inventory[0], "Existing inventory IDs and equipment survive character normalization with the new rarity");
  showItemDetails(getItem("key-item-phylactery-core"));
  let detail = document.querySelector("#item-detail-panel .item-rarity-masterwork");
  if (!detail) detail = itemDetailPanel.querySelector(".item-rarity-masterwork");
  assert(detail && detail.textContent.includes("Masterwork"), "Key item details display the Masterwork badge");
  assert(getComputedStyle(detail).getPropertyValue("--spell-accent").trim() === "#d6783f", "Masterwork item details use the orange light-theme accent");
  document.body.classList.add("dark-mode");
  assert(getComputedStyle(detail).getPropertyValue("--spell-accent").trim() === "#ee9863", "Masterwork item details use the orange dark-theme accent");
  document.body.classList.remove("dark-mode");
  showItemDetails(getItem("masterwork-royal-trident"));
  const unique = itemDetailPanel.querySelector(".item-unique-status");
  assert(!unique, "Unique badges are gone; masterwork rarity says it all");
  const intel = GuildInformationRules.itemPool(items, await Combat.enemyDropSources(), classCatalog);
  assert(intel.some(entry => entry.item.id === "masterwork-royal-trident" && entry.sources.some(source => source.enemyName === "Deep Sea King" && source.chance === 12)), "Masterwork conversion preserves unique drop chances and Guild Information eligibility");
  assert(intel.some(entry => entry.item.id === "key-item-phylactery-core" && entry.sources.some(source => source.enemyName === "Ancient Lich Ool" && source.chance === 20)), "Masterwork class-unlock items retain their sources and chances");
  assert(special.filter(isMasterworkEquipment).every(item => item.shopAvailable === false), "Masterwork unique items remain excluded from shop stock");
  applyCharacter(original);
  return { passed: true, items: special.length };
}

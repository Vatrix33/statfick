/* Pure shop rules shared by the town storefront and regression tests. */
(function (root) {
  const SILVER_PER_GOLD = 100;
  const SILVER_PER_PLATINUM = 10000;
  const rarities = ["common", "uncommon", "rare", "epic", "legendary", "masterwork", "exotic"];

  function walletValue(wallet) {
    return Math.max(0, Math.floor(Number(wallet?.silver) || 0)) +
      Math.max(0, Math.floor(Number(wallet?.gold) || 0)) * SILVER_PER_GOLD +
      Math.max(0, Math.floor(Number(wallet?.platinum) || 0)) * SILVER_PER_PLATINUM;
  }

  function walletFromValue(value) {
    let remaining = Math.max(0, Math.floor(Number(value) || 0));
    const platinum = Math.floor(remaining / SILVER_PER_PLATINUM);
    remaining -= platinum * SILVER_PER_PLATINUM;
    const gold = Math.floor(remaining / SILVER_PER_GOLD);
    return { silver: remaining - gold * SILVER_PER_GOLD, gold, platinum };
  }

  function spend(wallet, price) {
    const cost = Math.max(0, Math.floor(Number(price) || 0));
    const total = walletValue(wallet);
    return total < cost ? null : walletFromValue(total - cost);
  }

  function salePrice(originalPrice) {
    return Math.floor(Math.max(0, Number(originalPrice) || 0) / 2);
  }

  function formatPrice(value) {
    const wallet = walletFromValue(value);
    const parts = [];
    if (wallet.platinum) parts.push(wallet.platinum + " Platinum");
    if (wallet.gold) parts.push(wallet.gold + " Gold");
    if (wallet.silver || !parts.length) parts.push(wallet.silver + " Silver");
    return parts.join(" · ");
  }

  function levelBand(level) {
    const value = Math.max(1, Math.min(100, Math.floor(Number(level) || 1)));
    if (value <= 15) return { rarity: "common", tiers: [1, 2] };
    if (value <= 30) return { rarity: "uncommon", tiers: [3, 4] };
    if (value <= 60) return { rarity: "rare", tiers: [5, 6] };
    if (value <= 90) return { rarity: "epic", tiers: [7, 8] };
    return { rarity: "legendary", tiers: [9, 10] };
  }

  function rarityWeight(rarity, level) {
    const target = rarities.indexOf(levelBand(level).rarity);
    const candidate = Math.max(0, rarities.indexOf(String(rarity || "common").toLowerCase()));
    if (candidate === target) return 60;
    if (candidate < target) return 18 / (target - candidate);
    return 4 / Math.pow(4, candidate - target - 1);
  }

  function tierWeight(tier, level) {
    const value = Math.max(1, Math.min(10, Math.floor(Number(tier) || 1)));
    // Tier 10 is mythical: even the highest-level shops should almost never stock it.
    if (value === 10) return 0.1;
    const [low, high] = levelBand(level).tiers;
    if (value >= low && value <= high) return 30;
    if (value < low) return 12 / (low - value);
    return 3 / Math.pow(3, value - high - 1);
  }

  function weightedGroupSample(entries, count, groupKey, weightOfGroup, random = Math.random) {
    const groups = new Map();
    for (const entry of entries) {
      const key = groupKey(entry);
      if (!groups.has(key)) groups.set(key, []);
      groups.get(key).push(entry);
    }
    const selected = [];
    while (selected.length < count && groups.size) {
      const available = [...groups.entries()];
      const totalWeight = available.reduce((sum, [key]) => sum + Math.max(0, Number(weightOfGroup(key)) || 0), 0);
      let roll = random() * totalWeight;
      let chosen = available.at(-1);
      for (const group of available) {
        roll -= Math.max(0, Number(weightOfGroup(group[0])) || 0);
        if (roll <= 0) { chosen = group; break; }
      }
      const [key, groupEntries] = chosen;
      const index = Math.min(groupEntries.length - 1, Math.floor(random() * groupEntries.length));
      selected.push(groupEntries.splice(Math.max(0, index), 1)[0]);
      if (!groupEntries.length) groups.delete(key);
    }
    return selected;
  }

  const itemStock = (entries, count, level, random = Math.random) => weightedGroupSample(entries, count, entry => entry.rarity, rarity => rarityWeight(rarity, level), random);
  const spellStock = (entries, count, level, random = Math.random) => weightedGroupSample(entries, count, entry => entry.tier, tier => tierWeight(tier, level), random);
  const refreshWindow = minutes => Math.floor(Math.max(0, Number(minutes) || 0) / 360);

  root.ShopRules = { SILVER_PER_GOLD, SILVER_PER_PLATINUM, rarities, walletValue, walletFromValue, spend, salePrice, formatPrice, levelBand, rarityWeight, tierWeight, weightedGroupSample, itemStock, spellStock, refreshWindow };
  if (typeof module !== "undefined") module.exports = root.ShopRules;
})(globalThis);

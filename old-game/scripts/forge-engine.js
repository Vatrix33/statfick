/* Forged weapons: melee and ranged weapons assembled from three parts, each made
   of a material by a manufacturer, at the level where they were found.
   A forged weapon's whole recipe lives in its item id, so inventories, saves,
   shop stock, loot and battles store it exactly like a catalogue id:
     weapon-forged-<type>-<level>-<main>-<guard>-<third>-s<slots><runes>
   Each part is a two-letter maker code plus a three-letter material code ("x"
   for a weapon without that part); runes are three-letter codes.
   Pure rules, shared by the game and its regression tests. */
(function (root) {
  "use strict";
  const RARITIES = ["common", "uncommon", "rare", "epic", "legendary", "masterwork"];
  const ID_PREFIX = "weapon-forged-";
  const STAT_CODES = { strength: "S", vitality: "V", mind: "M", dexterity: "D", intelligence: "I", wisdom: "W", speed: "P", luck: "L" };
  const STAT_SHORT = { strength: "STR", vitality: "VIT", mind: "MND", dexterity: "DEX", intelligence: "INT", wisdom: "WIS", speed: "SPD", luck: "LCK" };
  const STAT_NAMES = { strength: "Strength", vitality: "Vitality", mind: "Mind", dexterity: "Dexterity", intelligence: "Intelligence", wisdom: "Wisdom", speed: "Speed", luck: "Luck" };
  const ALL_PARTS = ["head", "handle", "body", "guard", "string"];

  /* Manufacturers. Their bonus applies only through a weapon's secondary parts. */
  const MAKERS = {
    roundtable: { code: "rt", name: "Roundtable", parts: ALL_PARTS, effect: { primaryPct: 15 },
      motto: "Knightly steel in the oldest patterns.", flavor: "Its clean, knightly lines follow the Roundtable's oldest patterns." },
    dwarven: { code: "dw", name: "Dwarven", parts: ["head", "handle", "guard"], effect: { basePowerPct: 15 },
      motto: "Built to outlast its wielder.", flavor: "Dwarven smiths built it thick and solid, to outlast its wielder." },
    elven: { code: "el", name: "Elven", parts: ALL_PARTS, effect: { secondaryPct: 15 },
      motto: "Shaped along the grain of living things.", flavor: "Elven craft bent it along the grain of living things." },
    "boreal-bolt": { code: "bb", name: "Boreal Bolt", parts: ["body", "guard", "string"], effect: { crit: 10 },
      motto: "Effect over form. The hunt comes first.", flavor: "Boreal Bolt left it bare: every ounce serves the hunt for the undead." },
    abyss: { code: "ab", name: "Abyss", parts: ["head", "handle", "guard"], effect: { finalPct: 7 },
      motto: "Every edge jagged, every edge hungry.", flavor: "Abyss forges left every edge jagged and hungry." },
    hellmade: { code: "hm", name: "Hellmade", parts: ALL_PARTS, effect: { pierce: 10 },
      motto: "Ornament without a single flaw.", flavor: "Hellmade artisans adorned it and finished it without a single flaw." },
    hearthforge: { code: "hf", name: "Hearthforge", parts: ["head", "handle", "guard", "body"], effect: { primaryPct: 10, secondaryPct: 10 },
      motto: "Masters of the folded flame.", flavor: "Only Hearthforge masters could fold and temper it this finely." },
    outlaw: { code: "ol", name: "Outlaw", parts: ALL_PARTS, effect: { finalPct: -10, pricePct: -50 }, lootOnly: true,
      motto: "Lashed together. Barely holding.", flavor: "Outlaw hands lashed it together; it barely holds." },
  };
  /* Spell staffs and wands (item rework phase 2). A maker's caster effect applies
     through the handle and the inscription; Abyss, which only makes jewels and gems,
     applies its effect from those instead. Spell effects read
     { stat: "cost" | "power", amount, categories, element, excludeElement }, where a
     positive cost amount is a discount and the categories are "damage" (offensive
     and attack spells), "support" (support spells that are not summons) and "summon". */
  const SE = (stat, amount, extra = {}) => ({ stat, amount, ...extra });
  const OFFENSIVE = { categories: ["damage"] }, SUPPORT = { categories: ["support"] }, SUMMON = { categories: ["summon"] };
  Object.assign(MAKERS, {
    "institute-of-magic": { code: "im", name: "Institute of Magic", byName: "the Institute of Magic", parts: [],
      motto: "Plain work, precise script.", flavor: "Institute scholars set it plainly, and its script reads like lecture notes." },
    "church-of-talos": { code: "ct", name: "Church of Talos", byName: "the Church of Talos", parts: [],
      motto: "Zeal for the Lord of Storms.", flavor: "The Church of Talos shaped it in jagged zeal for the Lord of Storms." },
    vampiric: { code: "vp", name: "Vampiric", parts: [],
      motto: "Old blood, high station.", flavor: "Vampiric artisans crowned it like a relic of old blood and high station." },
  });
  const CASTER_MAKERS = {
    "institute-of-magic": { parts: ["jewel", "gem", "inscription"], effects: [SE("cost", 10)] },
    "church-of-talos": { parts: ["jewel", "gem", "handle", "inscription"], effects: [SE("power", 5)] },
    dwarven: { parts: ["handle"], effects: [SE("cost", 15, OFFENSIVE)] },
    elven: { parts: ["jewel", "gem", "handle", "inscription"], effects: [SE("cost", 15, SUPPORT)] },
    hellmade: { parts: ["jewel", "gem", "handle", "inscription"], effects: [SE("power", 5, OFFENSIVE), SE("cost", 8, OFFENSIVE)] },
    abyss: { parts: ["jewel", "gem"], effects: [SE("cost", 20, SUMMON)], fromMainPart: true },
    // Vampiric staffs and wands appear only on vampires and in the vampire castle town.
    vampiric: { parts: ["jewel", "gem", "inscription"], effects: [SE("power", 8, OFFENSIVE), SE("cost", -8, { ...OFFENSIVE, excludeElement: "blood" }), SE("cost", 8, { ...OFFENSIVE, element: "blood" })], restricted: true },
    hearthforge: { parts: ["handle", "inscription"], effects: [SE("cost", 5), SE("power", 3)] },
  };
  /* A Matching weapon (every part by one maker; a masterwork calls it Flawless) adds a bonus of its own
     per maker, different from the maker's usual one (user decision 2026-11-03). Staffs, wands, armor
     and shields don't; Outlaw adds nothing. */
  const MATCHING_BONUS = {
    roundtable: { finalPct: 3 }, dwarven: { pierce: 5 }, elven: { hit: 5 }, "boreal-bolt": { basePowerPct: 5 },
    abyss: { crit: 5 }, hellmade: { secondaryPct: 5 }, hearthforge: { hit: 3, crit: 3 },
  };
  const MAKER_IDS = Object.keys(MAKERS);
  const MAKER_BY_CODE = Object.fromEntries(MAKER_IDS.map(id => [MAKERS[id].code, id]));

  /* Materials: [id, code, name, rarity, usable parts, makers, effect]. A material
     on the head or body doubles its effect and sets the weapon's rarity. */
  const MATERIAL_ROWS = [
    ["scrap-metal", "scr", "Scrap Metal", "common", ["head", "guard"], ["outlaw"], { finalPct: -15 }],
    ["driftwood", "dri", "Driftwood", "common", ["handle", "guard", "body"], ["outlaw"], { hit: -10 }],
    ["linen", "lin", "Linen", "common", ["string"], ["outlaw"], { crit: -10 }],
    ["iron", "iro", "Iron", "common", ["head", "guard"], ["roundtable", "dwarven", "boreal-bolt", "hearthforge", "outlaw"], { finalPct: 3 }],
    ["steel", "ste", "Steel", "common", ["head", "guard"], ["roundtable", "dwarven", "elven", "boreal-bolt", "hearthforge", "hellmade"], { hit: 7 }],
    ["oakwood", "oak", "Oakwood", "common", ["handle", "guard", "body"], ["roundtable", "dwarven", "boreal-bolt", "hearthforge", "outlaw"], { crit: 4 }],
    ["quenchwood", "que", "Quenchwood", "common", ["handle", "guard", "body"], ["dwarven", "elven", "boreal-bolt", "hearthforge", "hellmade", "outlaw"], { pierce: 4 }],
    ["wool", "woo", "Wool", "common", ["string"], ["roundtable", "elven", "boreal-bolt", "outlaw"], { hit: 2 }],
    ["elvenleaf-string", "elv", "Elvenleaf String", "common", ["string"], ["elven", "boreal-bolt", "hellmade"], { element: "basic" }],
    ["damascus-steel", "dam", "Damascus Steel", "uncommon", ["head", "guard"], ["roundtable", "dwarven", "boreal-bolt", "abyss", "hellmade", "hearthforge"], { hit: 9 }],
    ["dwarven-steel", "dws", "Dwarven Steel", "uncommon", ["head", "guard"], ["dwarven", "hearthforge"], { finalPct: 5 }],
    ["boreum", "bor", "Boreum", "uncommon", ["head", "guard", "handle", "body"], ["dwarven", "boreal-bolt"], { pierce: 3, crit: 1 }],
    ["elven-birch", "bir", "Elven Birch", "uncommon", ["handle", "guard", "body"], ["dwarven", "elven", "boreal-bolt", "hellmade"], { crit: 2, pierce: 2 }],
    ["reinforced-wood", "rei", "Reinforced Wood", "uncommon", ["handle", "guard", "body"], ["roundtable", "dwarven", "boreal-bolt", "abyss", "hearthforge", "outlaw"], { finalPct: 3 }],
    ["steel-thread", "sth", "Steel Thread", "uncommon", ["string"], ["roundtable", "dwarven", "boreal-bolt", "hellmade", "outlaw"], { finalPct: 2 }],
    ["magithread", "mag", "Magithread", "uncommon", ["string"], ["elven"], { pierce: 5 }],
    ["blacksteel", "bla", "Blacksteel", "rare", ["head", "guard"], ["roundtable", "dwarven", "boreal-bolt", "abyss", "hellmade", "hearthforge", "outlaw"], { hit: 7, pierce: 3 }],
    ["lumium", "lum", "Lumium", "rare", ["head", "guard"], ["dwarven", "elven", "abyss", "hellmade", "hearthforge"], { spellCost: 3 }],
    ["gold-manyullin-alloy", "gma", "Gold-Manyullin Alloy", "rare", ["head", "handle", "guard", "body"], ["roundtable", "dwarven", "elven", "boreal-bolt"], { crit: 8, pricePct: 10 }],
    ["moonstone", "moo", "Moonstone", "rare", ["head", "guard", "handle"], ["roundtable", "dwarven", "elven", "hearthforge"], { secondaryPct: 10, pierce: 2 }],
    ["boreal-pine", "pin", "Boreal Pine", "rare", ["handle", "guard", "body"], ["roundtable", "dwarven", "boreal-bolt", "hellmade", "outlaw"], { finalPct: 5 }],
    ["manic-oak", "man", "Manic Oak", "rare", ["handle", "guard", "body"], ["dwarven", "elven", "boreal-bolt", "abyss", "hearthforge"], { pierce: 4, hit: 4, spellCost: 2 }],
    ["golden-thread", "gth", "Golden Thread", "rare", ["string"], ["roundtable", "dwarven", "elven", "hearthforge"], { crit: 3, pricePct: 5 }],
    ["dragonvein", "dra", "Dragonvein", "rare", ["string"], ["dwarven", "boreal-bolt", "abyss", "hellmade", "hearthforge", "outlaw"], { finalPct: 3, crit: 2 }],
    ["vulcan-alloy", "vul", "Vulcan Alloy", "epic", ["head", "guard"], ["roundtable", "dwarven", "elven", "boreal-bolt", "hearthforge"], { finalPct: 3, crit: 2, pierce: 4 }],
    ["hellstone", "hel", "Hellstone", "epic", ["head", "guard"], ["roundtable", "dwarven", "hellmade", "hearthforge"], { finalPct: 8, spellCost: 3 }],
    ["abyssal-iron", "aby", "Abyssal Iron", "epic", ["head", "guard"], ["dwarven", "boreal-bolt", "abyss", "hearthforge", "outlaw"], { pierce: 8, hit: 5, crit: 5 }],
    ["elven-greatwood", "egw", "Elven Greatwood", "epic", ["handle", "guard", "body"], ["roundtable", "elven", "hellmade", "hearthforge"], { hit: 4, crit: 4, spellCost: 3 }],
    ["foundationbark", "fou", "Foundationbark", "epic", ["handle", "guard", "body"], ["roundtable", "dwarven", "boreal-bolt", "abyss", "outlaw"], { finalPct: 5, pierce: 8 }],
    ["voidthread", "voi", "Voidthread", "epic", ["string"], ["roundtable", "boreal-bolt", "hellmade", "outlaw"], { pierce: 10 }],
    ["woven-mana", "wma", "Woven Mana", "epic", ["string"], ["elven", "boreal-bolt", "hellmade"], { finalPct: 5, spellCost: 3 }],
    ["star-metal", "sta", "Star Metal", "legendary", ["head", "guard", "handle"], ["roundtable", "dwarven", "elven", "hearthforge"], { finalPct: 5, hit: 5, pierce: 5, spellCost: 4 }],
    ["nullstone", "nul", "Nullstone", "legendary", ["head", "guard", "body"], ["dwarven", "boreal-bolt", "abyss", "hellmade", "hearthforge"], { hit: 9, crit: 9, pierce: 9 }],
    ["ancient-boreum", "abo", "Ancient Boreum", "legendary", ["head", "handle", "guard", "body"], ["elven", "boreal-bolt", "hearthforge"], { finalPct: 6, pierce: 12, crit: 4 }],
    ["lifetree-branch", "lif", "Lifetree Branch", "legendary", ["handle", "guard", "body"], ["dwarven", "elven", "hearthforge"], { hit: 8, crit: 4, spellCost: 6 }],
    ["woven-starlight", "wst", "Woven Starlight", "legendary", ["string"], ["elven", "hellmade", "hearthforge"], { finalPct: 5, hit: 10 }],
    ["gold-vulcan-thread", "gvt", "Gold-Vulcan Thread", "legendary", ["string"], ["roundtable", "boreal-bolt"], { finalPct: 4, hit: 4, crit: 4, pierce: 4 }],
    // Mythilium never appears on a generated weapon; it is reserved for a later phase.
    ["mythilium", "myt", "Mythilium", "masterwork", ["head", "body"], MAKER_IDS, { finalPct: 5, hit: 5, crit: 5, pierce: 5 }, false],
  ];
  const MATERIALS = Object.fromEntries(MATERIAL_ROWS.map(([id, code, name, rarity, parts, makers, effect, natural = true]) =>
    [id, { id, code, name, rarity, parts, makers, effect, natural }]));
  const MATERIAL_BY_CODE = Object.fromEntries(Object.values(MATERIALS).map(material => [material.code, material.id]));

  // Staff and wand handles: [id, code, name, rarity, makers, spell effects]. A handle sets the weapon's rarity.
  const CASTER_HANDLE_ROWS = [
    ["oakwood", "oak", "Oakwood", "common", ["church-of-talos", "dwarven", "elven"], [SE("cost", 1)]],
    ["dwarven-steel", "dws", "Dwarven Steel", "uncommon", ["dwarven", "hearthforge"], [SE("power", 3, OFFENSIVE)]],
    ["elven-birch", "bir", "Elven Birch", "uncommon", ["church-of-talos", "elven", "hellmade"], [SE("cost", 3)]],
    ["lumium", "lum", "Lumium", "rare", ["church-of-talos", "dwarven", "elven", "hellmade", "hearthforge"], [SE("cost", 5), SE("power", 2)]],
    ["moonstone", "moo", "Moonstone", "rare", ["church-of-talos", "elven", "hearthforge"], [SE("cost", 7, { categories: ["support", "summon"] })]],
    ["manic-oak", "man", "Manic Oak", "rare", ["church-of-talos", "dwarven", "hearthforge"], [SE("power", 5, OFFENSIVE), SE("cost", 3, OFFENSIVE)]],
    ["hellstone", "hel", "Hellstone", "epic", ["dwarven", "hellmade", "hearthforge"], [SE("power", 10, OFFENSIVE)]],
    ["elven-greatwood", "egw", "Elven Greatwood", "epic", ["church-of-talos", "elven"], [SE("cost", 8), SE("power", 5, SUPPORT)]],
    ["foundationbark", "fou", "Foundationbark", "epic", ["church-of-talos", "hearthforge"], [SE("cost", 20, SUMMON), SE("power", 3, OFFENSIVE)]],
    ["star-metal", "sta", "Star Metal", "legendary", ["dwarven", "hellmade", "hearthforge"], [SE("cost", 10), SE("power", 15)]],
    ["lifetree-branch", "lif", "Lifetree Branch", "legendary", ["church-of-talos", "elven"], [SE("cost", 30)]],
    // Like on weapons, Mythilium never appears naturally.
    ["mythilium", "myt", "Mythilium", "masterwork", Object.keys(CASTER_MAKERS), [SE("cost", 15), SE("power", 15)], false],
  ];
  const CASTER_HANDLES = Object.fromEntries(CASTER_HANDLE_ROWS.map(([id, code, name, rarity, makers, effects, natural = true]) => [id, { id, code, name, rarity, makers, effects, natural }]));
  const CASTER_HANDLE_BY_CODE = Object.fromEntries(Object.values(CASTER_HANDLES).map(material => [material.code, material.id]));
  // Jewels (staffs) cut the Mana cost of their element's spells; gems (wands) raise their power.
  const STONE_ROWS = [
    ["diamond", "dia", "Diamond", "basic", ["institute-of-magic", "hellmade", "vampiric"]],
    ["ruby", "rub", "Ruby", "fire", ["institute-of-magic", "church-of-talos", "hellmade"]],
    ["aquamarine", "aqu", "Aquamarine", "water", ["institute-of-magic", "church-of-talos", "elven", "vampiric"]],
    ["zircon", "zir", "Zircon", "earth", ["institute-of-magic", "elven", "hellmade"]],
    ["emerald", "eme", "Emerald", "wind", ["institute-of-magic", "church-of-talos", "elven"]],
    ["sapphire", "sap", "Sapphire", "lightning", ["institute-of-magic", "church-of-talos", "abyss"]],
    ["lapis-lazuli", "lap", "Lapis Lazuli", "ice", ["institute-of-magic", "abyss", "vampiric"]],
    ["topaz", "top", "Topaz", "holy", ["church-of-talos", "elven", "vampiric"]],
    ["obsidian", "obs", "Obsidian", "dark", ["abyss", "hellmade", "vampiric"]],
    ["jade", "jad", "Jade", "nature", ["church-of-talos", "elven", "vampiric"]],
    ["cinnabar", "cin", "Cinnabar", "blood", ["hellmade", "abyss", "vampiric"]],
    ["hematite", "hem", "Hematite", "death", ["hellmade", "abyss", "vampiric"]],
  ];
  const STONES = Object.fromEntries(STONE_ROWS.map(([id, code, name, element, makers]) => [id, { id, code, name, element, makers }]));
  const STONE_BY_CODE = Object.fromEntries(Object.values(STONES).map(stone => [stone.code, stone.id]));
  const TIERS = [null, { name: "Lesser", value: 5 }, { name: "Regular", value: 10 }, { name: "Greater", value: 20 }];
  // Inscription inks: any inscription maker can use any ink.
  const INK_ROWS = [
    ["white-ink", "wht", "White Ink", "basic"], ["red-ink", "red", "Red Ink", "fire"], ["blue-ink", "blu", "Blue Ink", "water"], ["brown-ink", "brn", "Brown Ink", "earth"],
    ["green-ink", "grn", "Green Ink", "wind"], ["light-blue-ink", "lbl", "Light Blue Ink", "lightning"], ["cyan-ink", "cyn", "Cyan Ink", "ice"], ["golden-ink", "gld", "Golden Ink", "holy"],
    ["black-ink", "blk", "Black Ink", "dark"], ["light-green-ink", "lgr", "Light Green Ink", "nature"], ["grey-ink", "gry", "Grey Ink", "death"], ["dark-red-ink", "drd", "Dark Red Ink", "blood"],
  ];
  const INKS = Object.fromEntries(INK_ROWS.map(([id, code, name, element]) => [id, { id, code, name, element, effects: [SE("cost", 15, { element })] }]));
  const INK_BY_CODE = Object.fromEntries(Object.values(INKS).map(ink => [ink.code, ink.id]));

  /* Part shapes. The role is the slot a shape fills; "alsoRole" lets a long handle
     used as a quarterstaff or club head take handle materials too. */
  const SHAPES = {
    "long-blade": { name: "Long Blade", role: "head" }, "short-blade": { name: "Short Blade", role: "head" },
    "heavy-blade": { name: "Heavy Blade", role: "head" }, "thin-blade": { name: "Thin Blade", role: "head" },
    "axe-head": { name: "Axe Head", role: "head" }, "blunt-head": { name: "Blunt Head", role: "head" },
    spearhead: { name: "Spearhead", role: "head" }, "staff-shaft": { name: "Long Handle", role: "head", alsoRole: "handle" },
    "small-limb": { name: "Small Limb", role: "body" }, "short-limb": { name: "Short Limb", role: "body" },
    "long-limb": { name: "Long Limb", role: "body" }, heft: { name: "Heft", role: "body" },
    crossguard: { name: "Crossguard", role: "guard" }, pommel: { name: "Pommel", role: "guard" },
    reinforcement: { name: "Reinforcement", role: "guard" }, mechanism: { name: "Mechanism", role: "guard" },
    "short-handle": { name: "Short Handle", role: "handle" }, "medium-handle": { name: "Medium Handle", role: "handle" },
    "long-handle": { name: "Long Handle", role: "handle" },
    "light-string": { name: "Light String", role: "string" }, "strong-string": { name: "Strong String", role: "string" },
    jewel: { name: "Jewel", role: "jewel" }, gem: { name: "Gem", role: "gem" }, inscription: { name: "Inscription", role: "inscription" },
    "staff-handle": { name: "Handle", role: "caster-handle" }, "wand-handle": { name: "Handle", role: "caster-handle" },
  };

  /* Weapon types: power and scaling from level 1 to level 100, and their parts.
     CRIT gives each type its base Crit Rating and its extra critical damage:
     light, precise weapons crit often; heavy ones crit rarely but hit harder. */
  const CRIT = {
    dagger: [9, 0], rapier: [8, 0], longsword: [6, 5], spear: [6, 5], flail: [5, 10], axe: [5, 10], quarterstaff: [5, 0],
    mace: [4, 10], club: [4, 10], greatclub: [4, 15], zweihander: [5, 15], greatsword: [4, 15], greataxe: [4, 20], lance: [3, 20], greathammer: [3, 25],
    shortbow: [7, 0], compoundbow: [7, 5], crossbow: [8, 10], longbow: [6, 10], warbow: [4, 15], ballistabow: [3, 25],
    staff: [5, 0], wand: [5, 0],
  };
  const W = (name, ranged, handed, power, primary, secondary, parts, extra = {}) => ({ name, ranged, handed, power, primary, secondary, parts, ...extra });
  const WEAPONS = {
    longsword: W("Longsword", false, 1, [10, 100], ["strength", 0.3, 1.2], ["dexterity", 0.1, 0.8], ["long-blade", "crossguard", "short-handle"]),
    dagger: W("Dagger", false, 1, [5, 50], ["dexterity", 0.2, 1], ["speed", 0.1, 0.8], ["short-blade", "crossguard", "short-handle"]),
    greatsword: W("Greatsword", false, 2, [12, 130], ["strength", 0.3, 1.5], ["speed", 0.1, 0.6], ["heavy-blade", "crossguard", "short-handle"]),
    zweihander: W("Zweihander", false, 2, [12, 140], ["strength", 0.3, 1.3], ["luck", 0.2, 0.7], ["heavy-blade", "crossguard", "medium-handle"]),
    rapier: W("Rapier", false, 1, [8, 90], ["dexterity", 0.4, 1.5], ["intelligence", 0.1, 0.4], ["thin-blade", "crossguard", "short-handle"]),
    axe: W("Axe", false, 1, [11, 110], ["strength", 0.4, 1.6], ["vitality", 0.1, 0.4], ["axe-head", "pommel", "medium-handle"]),
    greataxe: W("Greataxe", false, 2, [14, 150], ["strength", 0.5, 1.5], ["vitality", 0.1, 0.6], ["axe-head", "pommel", "long-handle"]),
    mace: W("Mace", false, 1, [12, 120], ["vitality", 0.4, 1.2], ["strength", 0.1, 0.8], ["blunt-head", "pommel", "medium-handle"]),
    greathammer: W("Greathammer", false, 2, [15, 160], ["vitality", 0.5, 1.8], ["strength", 0.1, 0.4], ["blunt-head", "pommel", "long-handle"]),
    quarterstaff: W("Quarterstaff", false, 2, [2, 40], ["wisdom", 0.3, 0.8], ["strength", 0.1, 0.6], ["staff-shaft", "reinforcement", null], { unarmedBonuses: true }),
    club: W("Club", false, 1, [7, 80], ["strength", 0.3, 1.0], ["vitality", 0.1, 0.5], ["staff-shaft", "pommel", "short-handle"], { onlyMaker: "outlaw" }),
    greatclub: W("Greatclub", false, 1, [10, 90], ["strength", 0.3, 1.3], ["vitality", 0.2, 0.6], ["blunt-head", "pommel", "short-handle"]),
    spear: W("Spear", false, 1, [11, 110], ["dexterity", 0.3, 1.2], ["vitality", 0.2, 0.8], ["spearhead", "pommel", "medium-handle"], { twoHandedPrimaryBonus: 0.2 }),
    lance: W("Lance", false, 2, [12, 140], ["vitality", 0.4, 1.5], ["dexterity", 0.1, 0.6], ["spearhead", "pommel", "long-handle"]),
    flail: W("Flail", false, 1, [11, 110], ["strength", 0.3, 1.1], ["luck", 0.2, 0.9], ["blunt-head", "pommel", "short-handle"]),
    shortbow: W("Shortbow", true, 2, [7, 70], ["dexterity", 0.3, 1.2], ["speed", 0.1, 0.6], ["small-limb", "reinforcement", "light-string"]),
    longbow: W("Longbow", true, 2, [10, 100], ["dexterity", 0.3, 1.4], ["strength", 0.2, 0.6], ["long-limb", "reinforcement", "light-string"]),
    warbow: W("Warbow", true, 2, [12, 120], ["strength", 0.4, 1.5], ["dexterity", 0.1, 0.6], ["long-limb", "reinforcement", "strong-string"]),
    ballistabow: W("Ballista Bow", true, 2, [15, 150], ["vitality", 0.5, 1.6], ["strength", 0.1, 0.6], ["long-limb", "reinforcement", "strong-string"]),
    crossbow: W("Crossbow", true, 2, [11, 130], ["strength", 0.3, 1.5], ["luck", 0.2, 0.5], ["heft", "mechanism", "strong-string"]),
    compoundbow: W("Compound Bow", true, 2, [12, 120], ["speed", 0.3, 1.4], ["luck", 0.2, 0.6], ["short-limb", "reinforcement", "strong-string"]),
    // Staffs and wands strike with fixed melee power; their parts work on spells.
    staff: W("Staff", false, 2, [7, 7], ["strength", 0.4, 0.4], null, ["jewel", "staff-handle", "inscription"], { caster: "jewel", kindName: "Spell Staff", itemType: "spell-staff" }),
    wand: W("Wand", false, 1, [3, 3], ["dexterity", 0.3, 0.3], null, ["gem", "wand-handle", "inscription"], { caster: "gem", kindName: "Spell Wand", itemType: "magic-wand" }),
  };
  const WEAPON_IDS = Object.keys(WEAPONS);
  const CASTER_IDS = WEAPON_IDS.filter(id => WEAPONS[id].caster);
  const REGULAR_IDS = WEAPON_IDS.filter(id => !WEAPONS[id].caster);

  /* Runes. Runes of one group cannot share a weapon. */
  const R = (code, name, rarity, group, effect) => ({ code, name, rarity, group, effect });
  const RUNES = {
    muscle: R("mus", "Rune of Muscle", "rare", "scaling", { secondaryStat: "strength" }),
    heart: R("hea", "Rune of Heart", "rare", "scaling", { secondaryStat: "vitality" }),
    brain: R("bra", "Rune of Brain", "rare", "scaling", { secondaryStat: "mind" }),
    reflex: R("ref", "Rune of Reflex", "rare", "scaling", { secondaryStat: "dexterity" }),
    knowledge: R("kno", "Rune of Knowledge", "rare", "scaling", { secondaryStat: "intelligence" }),
    wit: R("wit", "Rune of Wit", "rare", "scaling", { secondaryStat: "wisdom" }),
    accel: R("acc", "Rune of Accel", "rare", "scaling", { secondaryStat: "speed" }),
    fortune: R("for", "Rune of Fortune", "rare", "scaling", { secondaryStat: "luck" }),
    tarot: R("tar", "Rune of Tarot", "rare", "element", { element: "basic" }),
    cinder: R("cin", "Rune of Cinder", "rare", "element", { element: "fire" }),
    humidity: R("hum", "Rune of Humidity", "rare", "element", { element: "water" }),
    basalt: R("bas", "Rune of Basalt", "rare", "element", { element: "earth" }),
    breeze: R("bre", "Rune of Breeze", "rare", "element", { element: "wind" }),
    charge: R("cha", "Rune of Charge", "rare", "element", { element: "lightning" }),
    still: R("sti", "Rune of Still", "rare", "element", { element: "ice" }),
    day: R("day", "Rune of Day", "epic", "element", { element: "holy" }),
    night: R("nig", "Rune of Night", "epic", "element", { element: "dark" }),
    gaia: R("gai", "Rune of Gaia", "epic", "element", { element: "nature" }),
    beyond: R("bey", "Rune of Beyond", "epic", "element", { element: "death" }),
    crimson: R("cri", "Rune of Crimson", "epic", "element", { element: "blood" }),
    heft: R("hef", "Rune of Heft", "rare", "heft", { pierce: 15 }),
    precision: R("pre", "Rune of Precision", "rare", "precision", { hit: 15 }),
    "vital-hit": R("vit", "Rune of Vital Hit", "rare", "vital-hit", { crit: 8 }),
    empower: R("emp", "Rune of Empower", "rare", "empower", { finalPct: 5 }),
  };
  const RUNE_IDS = Object.keys(RUNES);
  const RUNE_BY_CODE = Object.fromEntries(RUNE_IDS.map(id => [RUNES[id].code, id]));
  const ELEMENT_SHARE = 50;
  const ELEMENT_PREFIX = { basic: "Fated", fire: "Igniting", water: "Cascading", earth: "Eroding", wind: "Gusting", lightning: "Crackling", ice: "Freezing", holy: "Radiant", dark: "Darkening", nature: "Blooming", death: "Withering", blood: "Bleeding" };
  const ELEMENT_NAMES = { basic: "Basic", fire: "Fire", water: "Water", earth: "Earth", wind: "Wind", lightning: "Lightning", ice: "Ice", holy: "Holy", dark: "Dark", nature: "Nature", death: "Death", blood: "Blood", physical: "Physical" };
  /* A rune costs about as much as a Tier 6 spell. Runes are inventory items until a
     runecarver fits them: fitting costs a quarter of the rune's price, and removing a
     piece's runes costs three times their prices and returns them to the inventory. */
  const RUNE_PRICE = { rare: 12000, epic: 18000, legendary: 26000 };
  const RUNE_FIT_SHARE = 0.25;
  const RUNE_REMOVAL_MULTIPLIER = 3;

  /* ---------- Numbers ---------- */
  const clamp = (value, min, max) => Math.min(max, Math.max(min, value));
  const round2 = value => Math.round(value * 100) / 100;
  const clampLevel = level => clamp(Math.floor(Number(level) || 1), 1, 100);
  // Power and scaling rise quickly early and ease into their maximum at level 100.
  const levelCurve = level => 1 - Math.pow(1 - (clampLevel(level) - 1) / 99, 1.4);
  const lerp = (from, to, t) => from + (to - from) * t;

  // Rarity odds at levels 1, 50 and 100, joined by one smooth curve per rarity.
  const ODDS_POINTS = { 1: [50, 25, 15, 7, 3], 50: [20, 25, 30, 15, 10], 100: [5, 10, 20, 40, 25] };
  /* Jewel and gem tiers (Lesser, Regular, Greater) shift gradually with level. Lesser
     leads up to level 35, Regular from 36 to 70 and Greater from 71; between these
     points the odds change a little with every level. */
  const TIER_ANCHORS = [[1, [85, 13, 2]], [35, [47, 45, 8]], [50, [15, 75, 10]], [70, [8, 47, 45]], [100, [3, 17, 80]]];
  const rarityOdds = level => curveOdds(ODDS_POINTS, level);
  function tierOdds(level) {
    const x = clampLevel(level);
    const upper = TIER_ANCHORS.findIndex(([anchor]) => anchor >= x);
    if (upper <= 0) return [...TIER_ANCHORS[0][1]];
    const [fromLevel, from] = TIER_ANCHORS[upper - 1], [toLevel, to] = TIER_ANCHORS[upper];
    const t = (x - fromLevel) / (toLevel - fromLevel);
    return from.map((value, index) => value + (to[index] - value) * t);
  }
  function curveOdds(points, level) {
    const x = clampLevel(level);
    const a = points[1], b = points[50], c = points[100];
    const la = (x - 50) * (x - 100) / ((1 - 50) * (1 - 100)), lb = (x - 1) * (x - 100) / ((50 - 1) * (50 - 100)), lc = (x - 1) * (x - 50) / ((100 - 1) * (100 - 50));
    const raw = a.map((_, index) => Math.max(0, a[index] * la + b[index] * lb + c[index] * lc));
    const total = raw.reduce((sum, value) => sum + value, 0) || 1;
    return raw.map(value => value * 100 / total);
  }
  function weightedIndex(weights, random) {
    const total = weights.reduce((sum, value) => sum + Math.max(0, value), 0);
    let roll = random() * total;
    for (let index = 0; index < weights.length; index++) { roll -= Math.max(0, weights[index]); if (roll < 0) return index; }
    return weights.length - 1;
  }
  const pick = (list, random) => list[Math.min(list.length - 1, Math.floor(random() * list.length))];
  function rollRarity(level, maxIndex, random) {
    const odds = rarityOdds(level).map((value, index) => index <= maxIndex ? value : 0);
    return weightedIndex(odds, random);
  }
  // Rune slots are standard from levels 40, 60 and 85; rarely one or two arrive early.
  function rollSlots(level, random) {
    const value = clampLevel(level);
    let slots = value >= 85 ? 3 : value >= 60 ? 2 : value >= 40 ? 1 : 0;
    if (slots < 3 && random() < 0.04) { slots++; if (slots < 3 && random() < 0.15) slots++; }
    return slots;
  }

  /* ---------- Parts ---------- */
  function shapeRole(shapeId) { return SHAPES[shapeId]?.role || null; }
  function materialFits(material, shapeId) {
    const shape = SHAPES[shapeId];
    return Boolean(shape) && (material.parts.includes(shape.role) || Boolean(shape.alsoRole) && material.parts.includes(shape.alsoRole));
  }
  function makerFits(makerId, shapeId) {
    const shape = SHAPES[shapeId], maker = MAKERS[makerId];
    return Boolean(shape && maker) && (maker.parts.includes(shape.role) || Boolean(shape.alsoRole) && maker.parts.includes(shape.alsoRole));
  }
  // Every (maker, material) pair that could produce this shape at this rarity.
  function partOptions(shapeId, rarityIndex, { allowOutlaw = true, onlyMaker = null } = {}) {
    const options = [];
    for (const material of Object.values(MATERIALS)) {
      if (!material.natural || RARITIES.indexOf(material.rarity) !== rarityIndex || !materialFits(material, shapeId)) continue;
      for (const makerId of material.makers) {
        if (!makerFits(makerId, shapeId) || makerId === "outlaw" && !allowOutlaw || onlyMaker && makerId !== onlyMaker) continue;
        options.push({ maker: makerId, material: material.id });
      }
    }
    return options;
  }
  function pickPart(shapeId, level, maxIndex, rules, random) {
    const first = Number.isInteger(rules.rarityIndex) ? rules.rarityIndex : rollRarity(level, maxIndex, random);
    // A rarity this shape cannot be made in falls back to the next lower one, then upward.
    const order = [...Array.from({ length: first + 1 }, (_, index) => first - index), ...Array.from({ length: maxIndex - first }, (_, index) => first + index + 1)];
    for (const rarityIndex of order) {
      const options = partOptions(shapeId, rarityIndex, rules);
      if (!options.length) continue;
      const makers = [...new Set(options.map(option => option.maker))];
      // The main part's maker is very slightly more likely to make the other parts too.
      const maker = makers[weightedIndex(makers.map(id => id === rules.preferMaker ? 1.5 : 1), random)];
      return { shape: shapeId, ...pick(options.filter(option => option.maker === maker), random) };
    }
    return null;
  }

  /* ---------- Generation ---------- */
  // Generates a weapon spec. source "shop" never makes Outlaw weapons, clubs or filled runes.
  // Without a type, about a quarter of generated weapons are staffs or wands (casterShare).
  // maxRarity caps the main part's rarity (recruits arrive with common gear). A staff or wand
  // can be given its maker (the Church of Talos sells its own).
  const rarityCap = maxRarity => maxRarity && RARITIES.includes(maxRarity) ? Math.min(4, RARITIES.indexOf(maxRarity)) : 4;
  function generate({ type = null, level = 1, source = "shop", random = Math.random, runeFillChance = 0, casterShare = 0.25, allowVampiric = false, maxRarity = null, rarity = null, element = null, maker = null } = {}) {
    const shop = source === "shop", fixed = RARITIES.indexOf(rarity), cap = fixed >= 0 ? fixed : rarityCap(maxRarity);
    // A maker that forges no staff or wand jewel or gem (Hearthforge) gets a regular weapon whose main part it makes.
    const regularMaker = maker && MAKERS[maker] && !casterMakers("jewel", true).includes(maker) && !casterMakers("gem", true).includes(maker) ? maker : null;
    const candidates = REGULAR_IDS.filter(id => (!shop || !WEAPONS[id].onlyMaker) && (!regularMaker || makerFits(regularMaker, WEAPONS[id].parts[0])));
    const typeId = WEAPONS[type] && (!shop || !WEAPONS[type].onlyMaker) ? type : !regularMaker && random() < casterShare ? pick(CASTER_IDS, random) : pick(candidates, random);
    if (WEAPONS[typeId].caster) return generateCaster(typeId, level, random, allowVampiric, cap, { element, rarityIndex: fixed >= 0 ? fixed : null, maker });
    const weapon = WEAPONS[typeId], value = clampLevel(level);
    const onlyMaker = weapon.onlyMaker || regularMaker;
    const main = pickPart(weapon.parts[0], value, cap, { allowOutlaw: !shop, onlyMaker, rarityIndex: fixed >= 0 ? fixed : null }, random);
    const mainIndex = RARITIES.indexOf(MATERIALS[main.material].rarity);
    const outlawMain = main.maker === "outlaw";
    const secondary = shape => shape ? pickPart(shape, value, mainIndex, { allowOutlaw: outlawMain, onlyMaker: weapon.onlyMaker || null, preferMaker: main.maker }, random) : null;
    const parts = [main, secondary(weapon.parts[1]), secondary(weapon.parts[2])];
    const slots = rollSlots(value, random);
    const runes = [];
    if (!shop && runeFillChance > 0) for (let slot = 0; slot < slots; slot++) {
      if (random() >= runeFillChance) continue;
      const pool = RUNE_IDS.filter(id => !runes.some(owned => RUNES[owned].group === RUNES[id].group));
      const epic = random() < 0.2;
      const choices = pool.filter(id => (RUNES[id].rarity === "epic") === epic);
      const chosen = choices.length ? pick(choices, random) : pool.length ? pick(pool, random) : null;
      if (chosen) runes.push(chosen);
    }
    return normalizeSpec({ type: typeId, level: value, parts, slots, runes });
  }
  function casterMakers(role, allowVampiric = true) {
    return Object.keys(CASTER_MAKERS).filter(id => CASTER_MAKERS[id].parts.includes(role) && (allowVampiric || !CASTER_MAKERS[id].restricted));
  }
  function generateCaster(typeId, level, random, allowVampiric, cap = 4, { element = null, rarityIndex = null, maker = null } = {}) {
    const weapon = WEAPONS[typeId], value = clampLevel(level), role = weapon.caster;
    // A fixed element picks a maker that cuts that element's stone, and that element's ink.
    const elementStones = element ? Object.keys(STONES).filter(id => STONES[id].element === element) : null;
    const makerPool = casterMakers(role, allowVampiric).filter(id => !elementStones || elementStones.some(stoneId => STONES[stoneId].makers.includes(id)));
    const mainMaker = maker && makerPool.includes(maker) ? maker : pick(makerPool, random);
    const stone = pick(Object.keys(STONES).filter(id => STONES[id].makers.includes(mainMaker) && (!elementStones || elementStones.includes(id))), random);
    const tier = 1 + weightedIndex(tierOdds(value), random);
    const preferred = makers => makers[weightedIndex(makers.map(id => id === mainMaker ? 1.5 : 1), random)];
    // The handle's rarity is rolled by level like a weapon's head or body.
    const first = rarityIndex ?? rollRarity(value, cap, random);
    let handle = null;
    for (const rarityIndex of [...Array.from({ length: first + 1 }, (_, index) => first - index), ...Array.from({ length: 4 - first }, (_, index) => first + index + 1)]) {
      const options = Object.values(CASTER_HANDLES).filter(material => material.natural && RARITIES.indexOf(material.rarity) === rarityIndex);
      const makers = [...new Set(options.flatMap(material => material.makers))].filter(id => CASTER_MAKERS[id]?.parts.includes("handle"));
      if (!makers.length) continue;
      const maker = preferred(makers);
      handle = { shape: weapon.parts[1], maker, material: pick(options.filter(material => material.makers.includes(maker)), random).id };
      break;
    }
    // Vampiric inscriptions appear only on staffs and wands with a Vampiric jewel or gem.
    const inscriptionMaker = preferred(casterMakers("inscription").filter(id => !CASTER_MAKERS[id].restricted || mainMaker === id));
    const ink = element ? Object.keys(INKS).find(id => INKS[id].element === element) : pick(Object.keys(INKS), random);
    const parts = [{ shape: role, maker: mainMaker, material: stone, tier }, handle, { shape: "inscription", maker: inscriptionMaker, material: ink }];
    return { type: typeId, level: value, parts, slots: 0, runes: [] };
  }
  function normalizeSpec(spec) {
    return { ...spec, runes: [...spec.runes].sort((a, b) => RUNE_IDS.indexOf(a) - RUNE_IDS.indexOf(b)) };
  }

  /* ---------- Ids ---------- */
  const partToken = part => part ? MAKERS[part.maker].code + MATERIALS[part.material].code : "x";
  function encode(spec) {
    if (spec.masterwork) return encodeMasterwork(spec);
    if (isArmorSpec(spec)) return encodeArmor(spec);
    if (WEAPONS[spec.type]?.caster) {
      const [main, handle, inscription] = spec.parts;
      return `${ID_PREFIX}${spec.type}-${clampLevel(spec.level)}-${MAKERS[main.maker].code}${STONES[main.material].code}${main.tier}-${MAKERS[handle.maker].code}${CASTER_HANDLES[handle.material].code}-${MAKERS[inscription.maker].code}${INKS[inscription.material].code}`;
    }
    const normal = normalizeSpec(spec);
    return `${ID_PREFIX}${normal.type}-${clampLevel(normal.level)}-${normal.parts.map(partToken).join("-")}-s${normal.slots}${normal.runes.map(id => RUNES[id].code).join("")}`;
  }
  function decodeCaster(rest) {
    const match = /^(staff|wand)-(\d{1,3})-([a-z]{2})([a-z]{3})([1-3])-([a-z]{2})([a-z]{3})-([a-z]{2})([a-z]{3})$/.exec(rest);
    if (!match) return null;
    const [, typeId, levelText, mainCode, stoneCode, tierText, handleMakerCode, handleCode, inkMakerCode, inkCode] = match;
    const weapon = WEAPONS[typeId], level = Number(levelText);
    const mainMaker = MAKER_BY_CODE[mainCode], stone = STONE_BY_CODE[stoneCode];
    const handleMaker = MAKER_BY_CODE[handleMakerCode], handle = CASTER_HANDLE_BY_CODE[handleCode];
    const inkMaker = MAKER_BY_CODE[inkMakerCode], ink = INK_BY_CODE[inkCode];
    const makes = (maker, role) => Boolean(CASTER_MAKERS[maker]?.parts.includes(role));
    if (level < 1 || level > 100 || !stone || !handle || !ink || !makes(mainMaker, weapon.caster) || !makes(handleMaker, "handle") || !makes(inkMaker, "inscription")) return null;
    return { type: typeId, level, parts: [{ shape: weapon.caster, maker: mainMaker, material: stone, tier: Number(tierText) }, { shape: weapon.parts[1], maker: handleMaker, material: handle }, { shape: "inscription", maker: inkMaker, material: ink }], slots: 0, runes: [] };
  }
  function decode(id) {
    if (typeof id === "string" && id.startsWith(ARMOR_PREFIX)) return decodeArmor(id.slice(ARMOR_PREFIX.length));
    if (typeof id === "string" && id.startsWith(MASTERWORK_PREFIX)) return decodeMasterwork(id.slice(MASTERWORK_PREFIX.length));
    if (typeof id === "string" && id.startsWith(MASTERWORK_ARMOR_PREFIX)) return decodeArmorMasterwork(id.slice(MASTERWORK_ARMOR_PREFIX.length));
    if (typeof id !== "string" || !id.startsWith(ID_PREFIX)) return null;
    if (/^(staff|wand)-/.test(id.slice(ID_PREFIX.length))) return decodeCaster(id.slice(ID_PREFIX.length));
    const match = /^([a-z]+)-(\d{1,3})-([a-z]{5}|x)-([a-z]{5}|x)-([a-z]{5}|x)-s([0-3])((?:[a-z]{3})*)$/.exec(id.slice(ID_PREFIX.length));
    if (!match) return null;
    const [, typeId, levelText, ...rest] = match;
    const weapon = WEAPONS[typeId];
    const level = Number(levelText);
    if (!weapon || level < 1 || level > 100) return null;
    const parts = [];
    for (let index = 0; index < 3; index++) {
      const token = rest[index], shape = weapon.parts[index];
      if (!shape) { if (token !== "x") return null; parts.push(null); continue; }
      if (token === "x") return null;
      const maker = MAKER_BY_CODE[token.slice(0, 2)], material = MATERIAL_BY_CODE[token.slice(2)];
      if (!maker || !material || !materialFits(MATERIALS[material], shape)) return null;
      parts.push({ shape, maker, material });
    }
    const slots = Number(rest[3]);
    const runes = (rest[4].match(/.{3}/g) || []).map(code => RUNE_BY_CODE[code]);
    if (runes.some(rune => !rune) || runes.length > slots || new Set(runes.map(rune => RUNES[rune].group)).size !== runes.length) return null;
    return normalizeSpec({ type: typeId, level, parts, slots, runes });
  }
  const isForgedId = id => typeof id === "string" && (id.startsWith(ID_PREFIX) || id.startsWith("armor-forged-") || id.startsWith(MASTERWORK_PREFIX) || id.startsWith("armor-masterwork-"));

  /* ---------- Derived stats ---------- */
  // A masterwork's fixed runes (the Masterwork Elementals): part of the masterwork, never in its id or slots.
  const fixedRunes = spec => (spec?.masterwork && MASTERWORKS[spec.masterwork]?.fixedRunes) || [];
  const allRunes = spec => [...fixedRunes(spec), ...(spec?.runes || [])];
  const EFFECT_KEYS = ["finalPct", "basePowerPct", "primaryPct", "secondaryPct", "hit", "crit", "pierce", "spellCost", "pricePct"];
  // The maker of a weapon whose parts all come from one maker (its name says Matching, or Flawless), else null.
  function matchingMaker(spec) {
    if (!WEAPONS[spec.type] || WEAPONS[spec.type].caster) return null;
    const parts = spec.parts.filter(Boolean);
    return parts.length > 1 && parts.every(part => part.maker === parts[0].maker) ? parts[0].maker : null;
  }
  const matchingBonus = spec => MATCHING_BONUS[matchingMaker(spec)] || null;
  const matchingBonusText = spec => matchingBonus(spec) ? effectText(matchingBonus(spec)) : "";
  function totals(spec) {
    const sum = Object.fromEntries(EFFECT_KEYS.map(key => [key, 0]));
    if (WEAPONS[spec.type]?.caster) return { ...sum, element: null, secondaryStat: null, runeElement: null };
    let element = null, secondaryStat = null, runeElement = null;
    spec.parts.forEach((part, index) => {
      if (!part) return;
      const material = MATERIALS[part.material], factor = index === 0 ? 2 : 1;
      for (const key of EFFECT_KEYS) sum[key] += (material.effect[key] || 0) * factor;
      if (material.effect.element) element = material.effect.element;
      if (index > 0) for (const key of EFFECT_KEYS) sum[key] += MAKERS[part.maker].effect[key] || 0;
    });
    const matching = matchingBonus(spec);
    if (matching) for (const key of EFFECT_KEYS) sum[key] += matching[key] || 0;
    // A masterwork's own fixed additions (Tidepiercer's armor pierce).
    const bonus = spec.masterwork && MASTERWORKS[spec.masterwork]?.totalsBonus;
    if (bonus) for (const key of EFFECT_KEYS) sum[key] += bonus[key] || 0;
    for (const runeId of allRunes(spec)) {
      const effect = RUNES[runeId].effect;
      for (const key of EFFECT_KEYS) sum[key] += effect[key] || 0;
      if (effect.secondaryStat) secondaryStat = effect.secondaryStat;
      if (effect.element) runeElement = effect.element;
    }
    return { ...sum, element, secondaryStat, runeElement };
  }
  function component(stat, ratio) { return { ratio: round2(ratio), code: STAT_CODES[stat], key: stat }; }
  function stats(spec) {
    const weapon = WEAPONS[spec.type], t = levelCurve(spec.level), sum = totals(spec);
    const basePower = Math.max(1, Math.round(lerp(weapon.power[0], weapon.power[1], t) * (1 + sum.basePowerPct / 100)));
    const primaryRatio = lerp(weapon.primary[1], weapon.primary[2], t) * (1 + sum.primaryPct / 100);
    const secondaryRatio = lerp(weapon.secondary[1], weapon.secondary[2], t) * (1 + sum.secondaryPct / 100);
    return {
      totals: sum, basePower,
      primary: component(weapon.primary[0], primaryRatio),
      secondary: component(sum.secondaryStat || weapon.secondary[0], secondaryRatio),
      finalPowerPercent: sum.finalPct, hitBonus: sum.hit, critBonus: sum.crit, armorPierce: clamp(sum.pierce, 0, 100), spellCost: sum.spellCost,
      element: sum.element || null, elementShare: sum.runeElement ? { element: sum.runeElement, percent: ELEMENT_SHARE } : null,
    };
  }

  const RARITY_PRICE = { common: 120, uncommon: 600, rare: 2400, epic: 6000, legendary: 12000, masterwork: 24000 };
  function price(spec, sum) {
    if (isArmorSpec(spec)) return armorPrice(spec);
    sum = sum || totals(spec);
    if (WEAPONS[spec.type]?.caster) return casterPrice(spec);
    const weapon = WEAPONS[spec.type];
    const [main, ...secondary] = spec.parts;
    const partValue = RARITY_PRICE[MATERIALS[main.material].rarity] + secondary.filter(Boolean).reduce((total, part) => total + RARITY_PRICE[MATERIALS[part.material].rarity] * 0.2, 0);
    const runeValue = spec.runes.reduce((total, id) => total + RUNE_PRICE[RUNES[id].rarity] * 0.4, 0);
    const value = (partValue * (1 + (clampLevel(spec.level) - 1) * 0.01) * (weapon.handed === 2 ? 1.2 : 1) * (1 + 0.12 * spec.slots) + runeValue) * (1 + clamp(sum.pricePct, -75, 100) / 100);
    return Math.max(10, Math.round(value / 10) * 10);
  }

  /* ---------- Names and words ---------- */
  function statPrefix(spec, sum = totals(spec)) {
    // Masterworks take the same prefixes under names of their own.
    const named = prefix => spec.masterwork ? MASTERWORK_PREFIXES[prefix] || "" : prefix;
    if (matchingMaker(spec)) return named("Matching");
    const scores = [
      ["Empowered", Math.max(sum.finalPct, sum.basePowerPct, sum.primaryPct, sum.secondaryPct)],
      ["Piercing", sum.pierce], ["Surehit", sum.hit], ["Critical", sum.crit],
    ];
    const best = scores.reduce((top, entry) => entry[1] > top[1] ? entry : top, ["", 0]);
    return best[1] > 0 ? named(best[0]) : "";
  }
  function name(spec, sum) {
    if (isArmorSpec(spec)) return armorName(spec);
    sum = sum || totals(spec);
    if (WEAPONS[spec.type]?.caster) return casterName(spec);
    // Masterworks keep their own name and take one prefix, never a rune prefix.
    if (spec.masterwork) return [statPrefix(spec, sum), MASTERWORKS[spec.masterwork].name].filter(Boolean).join(" ");
    const runePrefix = sum.runeElement ? ELEMENT_PREFIX[sum.runeElement] : spec.runes.length ? "Runed" : "";
    return [runePrefix, statPrefix(spec, sum), MATERIALS[spec.parts[0].material].name, WEAPONS[spec.type].name].filter(Boolean).join(" ");
  }
  function description(spec) {
    if (isArmorSpec(spec)) return armorDescription(spec);
    if (WEAPONS[spec.type]?.caster) return casterDescription(spec);
    if (spec.masterwork) return masterworkDescription(spec);
    const weapon = WEAPONS[spec.type], [main, guard, third] = spec.parts;
    const article = /^[aeiou]/i.test(weapon.name) ? "An" : "A";
    const piece = part => { const shape = SHAPES[part.shape].name.toLowerCase(); return `${/^[aeiou]/.test(shape) ? "an" : "a"} ${shape} of ${MATERIALS[part.material].name} by ${MAKERS[part.maker].name}`; };
    const rest = [guard, third].filter(Boolean).map(piece);
    return `${article} ${weapon.name.toLowerCase()} with ${piece(main)}${rest.length ? ", " + rest.join(" and ") : ""}. ${MAKERS[main.maker].flavor}`;
  }
  /* One part's material and maker can both add the same thing (two armor bonuses, two
     Fire resistances). Parts list them added together: numbers are summed, nested
     resistances per element, and anything that sums to zero is left out. */
  function scaleEffect(effect, factor) {
    return Object.fromEntries(Object.entries(effect || {}).map(([key, value]) => [key,
      typeof value === "number" ? value * factor : value && typeof value === "object" && !Array.isArray(value) ? scaleEffect(value, factor) : value]));
  }
  function mergeEffects(...effects) {
    const out = {};
    for (const effect of effects) for (const [key, value] of Object.entries(effect || {})) {
      if (typeof value === "number") out[key] = (out[key] || 0) + value;
      else if (value && typeof value === "object" && !Array.isArray(value)) out[key] = mergeEffects(out[key], value);
      else if (out[key] == null) out[key] = value;
    }
    for (const [key, value] of Object.entries(out)) if (value === 0 || value && typeof value === "object" && !Array.isArray(value) && !Object.keys(value).length) delete out[key];
    return out;
  }
  function effectText(effect, doubled = false) {
    const factor = doubled ? 2 : 1, out = [];
    const signed = (value, label, unit = "%") => out.push(`${value > 0 ? "+" : "−"}${Math.abs(value * factor)}${unit} ${label}`);
    if (effect.finalPct) signed(effect.finalPct, "final power");
    if (effect.basePowerPct) signed(effect.basePowerPct, "base power");
    if (effect.primaryPct) signed(effect.primaryPct, "primary scaling");
    if (effect.secondaryPct) signed(effect.secondaryPct, "secondary scaling");
    if (effect.hit) signed(effect.hit, "Accuracy", "");
    if (effect.crit) signed(effect.crit, "Crit Rating", "");
    if (effect.pierce) signed(effect.pierce, "armor pierce");
    if (effect.spellCost) out.push(`−${effect.spellCost * factor}% spell cost`);
    if (effect.pricePct) signed(effect.pricePct, "price");
    if (effect.element) out.push(`Attacks deal ${ELEMENT_NAMES[effect.element]} damage`);
    return out.join(" · ");
  }
  function runeText(runeId) {
    const effect = RUNES[runeId].effect;
    if (effect.secondaryStat) return `Secondary scaling becomes ${STAT_NAMES[effect.secondaryStat]}`;
    if (effect.element) return `${ELEMENT_SHARE}% of the weapon's damage is dealt as ${ELEMENT_NAMES[effect.element]} damage`;
    return effectText(effect);
  }

  /* ---------- Items ---------- */
  const itemCache = new Map();
  // The item object the game reads: catalogue weapon fields plus the forged details.
  function item(id) {
    if (itemCache.has(id)) return itemCache.get(id);
    const runeId = runeOfItem(id), masterworkKey = masterworkTemplateKey(id), template = !runeId && !masterworkKey && isTemplateId(id);
    const spec = runeId || masterworkKey || template ? null : decode(id);
    const built = runeId ? buildRuneItem(runeId) : masterworkKey ? buildMasterworkTemplate(masterworkKey) : template ? buildTemplateItem(id) : spec ? buildItem(spec, id) : null;
    if (itemCache.size > 4000) itemCache.clear();
    itemCache.set(id, built);
    return built;
  }
  function buildItem(spec, id = encode(spec)) {
    if (isArmorSpec(spec)) return buildArmorItem(spec, id);
    if (WEAPONS[spec.type].caster) {
      const item = buildCasterItem(spec, id), def = spec.masterwork ? MASTERWORKS[spec.masterwork] : null;
      if (!def) return item;
      // A caster masterwork keeps its forged gem, handle and inscription, under its own name; one that attacks
      // as another weapon (the Plasma Blade as a longsword) takes that weapon's power, crit and scaling ratios.
      const melee = def.attacksAs && WEAPONS[def.attacksAs.type], t = levelCurve(spec.level);
      const attacks = melee ? { basePower: Math.max(1, Math.round(lerp(melee.power[0], melee.power[1], t))), handed: melee.handed,
        primary: component(def.attacksAs.scaling[0], lerp(melee.primary[1], melee.primary[2], t)), secondary: component(def.attacksAs.scaling[1], lerp(melee.secondary[1], melee.secondary[2], t)),
        critBase: CRIT[def.attacksAs.type][0], critDamage: CRIT[def.attacksAs.type][1], ...(def.attacksAs.element ? { element: def.attacksAs.element } : {}), attacksAs: def.attacksAs.type } : {};
      return { ...item, ...attacks, name: `${TIERS[spec.parts[0].tier].name} ${def.name}`, rarity: "masterwork", masterwork: spec.masterwork,
        description: `A masterwork ${WEAPONS[spec.type].name.toLowerCase()} with a ${TIERS[spec.parts[0].tier].name} ${STONES[spec.parts[0].material].name} ${WEAPONS[spec.type].caster} by ${byName(spec.parts[0].maker)}. ${def.story}`,
        effects: [...def.effects.map(effect => ({ ...effect })), ...item.effects], priceSilver: Math.max(item.priceSilver, Math.round(RARITY_PRICE.masterwork * (1 + (clampLevel(spec.level) - 1) * 0.01) / 10) * 10),
        shopAvailable: false, forged: { ...item.forged, masterwork: spec.masterwork, masterworkName: def.name } };
    }
    const weapon = WEAPONS[spec.type], derived = stats(spec), mainMaterial = MATERIALS[spec.parts[0].material];
    const outlaw = spec.parts[0].maker === "outlaw", masterwork = spec.masterwork ? MASTERWORKS[spec.masterwork] : null;
    return {
      id, category: "weapon", name: name(spec, derived.totals), rarity: mainMaterial.rarity, description: description(spec),
      type: weapon.ranged ? "ranged" : "meele", handed: weapon.handed, basePower: derived.basePower,
      primary: derived.primary, secondary: derived.secondary, minimumStrength: 0,
      effects: [...(masterwork ? masterwork.effects.map(effect => ({ ...effect })) : []), ...(derived.spellCost > 0 ? [{ kind: "spell-cost-percent", amount: derived.spellCost, maxTier: null }] : [])],
      priceSilver: price(spec, derived.totals),
      catalogueAvailable: false, shopAvailable: masterwork ? Boolean(masterwork.shopOffer) : !outlaw && !weapon.onlyMaker, playerSellable: true, droppable: true, automaticDrop: true,
      ...(masterwork ? { masterwork: spec.masterwork, ...(masterwork.shopOffer ? { shopOffer: { ...masterwork.shopOffer } } : {}) } : {}),
      ...(derived.element ? { element: derived.element } : {}),
      elementShare: derived.elementShare, hitBonus: derived.hitBonus, critBonus: derived.critBonus, armorPierce: derived.armorPierce,
      finalPowerPercent: derived.finalPowerPercent, isBow: weapon.ranged, unarmedBonuses: Boolean(weapon.unarmedBonuses),
      twoHandedPrimaryBonus: weapon.twoHandedPrimaryBonus || 0,
      critBase: CRIT[spec.type][0], critDamage: CRIT[spec.type][1],
      forged: { ...spec, typeName: weapon.name, maker: spec.parts[0].maker, totals: derived.totals, ...(masterwork ? { masterworkName: masterwork.name } : {}) },
    };
  }

  /* ---------- Staffs and wands ---------- */
  const article = word => /^[aeiou]/i.test(word) ? "an" : "a";
  const byName = makerId => MAKERS[makerId].byName || MAKERS[makerId].name;
  // Every spell effect a staff or wand carries, tagged with the part it comes from.
  function casterEffects(spec) {
    const weapon = WEAPONS[spec.type], [main, handle, inscription] = spec.parts;
    const list = [], add = (effects, source) => effects.forEach(effect => list.push({ ...effect, source }));
    add([SE(weapon.caster === "jewel" ? "cost" : "power", TIERS[main.tier].value, { element: STONES[main.material].element })], "main");
    if (CASTER_MAKERS[main.maker].fromMainPart) add(CASTER_MAKERS[main.maker].effects, "main-maker");
    add(CASTER_HANDLES[handle.material].effects, "handle");
    add(CASTER_MAKERS[handle.maker].effects, "handle-maker");
    add(INKS[inscription.material].effects, "inscription");
    add(CASTER_MAKERS[inscription.maker].effects, "inscription-maker");
    return list;
  }
  const spellEffectKey = effect => [effect.stat, (effect.categories || []).join("+"), effect.element || "", effect.excludeElement || ""].join("|");
  // Spell effects of the same kind added together, in the order they first appear.
  function mergeSpellEffects(effects) {
    const merged = new Map();
    for (const effect of effects) {
      const key = spellEffectKey(effect);
      if (merged.has(key)) merged.get(key).amount += effect.amount;
      else merged.set(key, { ...effect });
    }
    return [...merged.values()].filter(effect => effect.amount !== 0);
  }
  // The same effects merged into equipment effects the spell rules read.
  function casterItemEffects(spec) {
    const merged = new Map();
    for (const effect of casterEffects(spec)) {
      const key = spellEffectKey(effect);
      if (!merged.has(key)) merged.set(key, { kind: effect.stat === "cost" ? "spell-cost-percent" : "spell-power-percent", amount: 0, maxTier: null,
        ...(effect.categories ? { categories: effect.categories } : {}), ...(effect.element ? { element: effect.element } : {}), ...(effect.excludeElement ? { excludeElement: effect.excludeElement } : {}) });
      merged.get(key).amount += effect.amount;
    }
    return [...merged.values()].filter(effect => effect.amount !== 0);
  }
  const CATEGORY_WORDS = { damage: "offensive", support: "support", summon: "summon" };
  function spellEffectText(effect) {
    const scope = [effect.element ? ELEMENT_NAMES[effect.element] : "", effect.categories ? effect.categories.map(category => CATEGORY_WORDS[category]).join(" and ") : ""].filter(Boolean).join(" ");
    const subject = scope ? scope + " spell" : "spell";
    const amount = Math.abs(effect.amount);
    if ((effect.stat || (effect.kind === "spell-cost-percent" ? "cost" : "power")) === "cost") return `${effect.amount > 0 ? "−" : "+"}${amount}% ${subject} cost${effect.excludeElement ? ` (not ${ELEMENT_NAMES[effect.excludeElement]})` : ""}`;
    return `${effect.amount >= 0 ? "+" : "−"}${amount}% ${subject} power`;
  }
  function casterName(spec) {
    const [main, , inscription] = spec.parts;
    return `${ELEMENT_PREFIX[INKS[inscription.material].element]} ${STONES[main.material].name} ${WEAPONS[spec.type].name}`;
  }
  function casterDescription(spec) {
    const weapon = WEAPONS[spec.type], [main, handle, inscription] = spec.parts;
    const handleName = CASTER_HANDLES[handle.material].name, inkName = INKS[inscription.material].name;
    return `A spell ${weapon.name.toLowerCase()} with a ${TIERS[main.tier].name} ${STONES[main.material].name} ${weapon.caster} by ${byName(main.maker)}, ${article(handleName)} ${handleName} handle by ${byName(handle.maker)} and ${article(inkName)} ${inkName} inscription by ${byName(inscription.maker)}. ${MAKERS[main.maker].flavor}`;
  }
  // The regular weapon price rule, with the handle setting the rarity.
  function casterPrice(spec) {
    const weapon = WEAPONS[spec.type], rarity = CASTER_HANDLES[spec.parts[1].material].rarity;
    return Math.max(10, Math.round(RARITY_PRICE[rarity] * (1 + (clampLevel(spec.level) - 1) * 0.01) * (weapon.handed === 2 ? 1.2 : 1) / 10) * 10);
  }
  function buildCasterItem(spec, id) {
    const weapon = WEAPONS[spec.type], handle = CASTER_HANDLES[spec.parts[1].material];
    return {
      id, category: "weapon", name: casterName(spec), rarity: handle.rarity, description: casterDescription(spec),
      type: weapon.itemType, handed: weapon.handed, basePower: weapon.power[0],
      primary: component(weapon.primary[0], weapon.primary[1]), secondary: { ratio: 0, code: "N", key: null, none: true }, minimumStrength: 0,
      effects: casterItemEffects(spec), priceSilver: casterPrice(spec),
      catalogueAvailable: false, shopAvailable: true, playerSellable: true, droppable: true, automaticDrop: true,
      elementShare: null, hitBonus: 0, critBonus: 0, armorPierce: 0, finalPowerPercent: 0, isBow: false, unarmedBonuses: false, twoHandedPrimaryBonus: 0,
      critBase: CRIT[spec.type][0], critDamage: CRIT[spec.type][1],
      forged: { ...spec, typeName: weapon.kindName, caster: weapon.caster, maker: spec.parts[0].maker, tierName: TIERS[spec.parts[0].tier].name, vampiric: spec.parts[0].maker === "vampiric" },
    };
  }
  // One part, described for the item details: shape, material, maker and what it does.
  function describePart(spec, index) {
    const part = spec.parts[index];
    if (!part) return null;
    if (isArmorSpec(spec)) return describeArmorPart(spec, index);
    const maker = MAKERS[part.maker], base = { shape: SHAPES[part.shape].name, maker: maker.name, makerId: part.maker, main: index === 0 };
    if (WEAPONS[spec.type].caster) {
      const sources = [["main", "main-maker"], ["handle", "handle-maker"], ["inscription", "inscription-maker"]][index];
      const effects = mergeSpellEffects(casterEffects(spec).filter(effect => sources.includes(effect.source))).map(spellEffectText).join(" · ");
      if (index === 0) return { ...base, material: `${TIERS[part.tier].name} ${STONES[part.material].name}`, rarity: null, element: STONES[part.material].element, effects };
      if (index === 1) return { ...base, material: CASTER_HANDLES[part.material].name, rarity: CASTER_HANDLES[part.material].rarity, effects };
      return { ...base, material: INKS[part.material].name, rarity: null, element: INKS[part.material].element, effects };
    }
    const material = MATERIALS[part.material];
    return { ...base, material: material.name, rarity: material.rarity, effects: effectText(mergeEffects(scaleEffect(material.effect, index === 0 ? 2 : 1), index > 0 ? maker.effect : null)) };
  }

  /* ---------- Runes on weapons ---------- */
  function runeConflict(spec, runeId) {
    if (isArmorSpec(spec)) return armorRuneConflict(spec, runeId);
    const rune = RUNES[runeId];
    if (!rune) return "Unknown rune";
    if (spec.runes.length >= spec.slots) return "No empty rune slot";
    if (allRunes(spec).some(owned => RUNES[owned].group === rune.group)) return "A rune of this kind is already slotted";
    return "";
  }
  function withRune(id, runeId) {
    const spec = decode(id);
    if (!spec || runeConflict(spec, runeId)) return null;
    return encode({ ...spec, runes: [...spec.runes, runeId] });
  }
  function withoutRune(id, runeId) {
    const spec = decode(id);
    if (!spec || !spec.runes.includes(runeId)) return null;
    return encode({ ...spec, runes: spec.runes.filter(owned => owned !== runeId) });
  }
  const runePrice = runeId => RUNE_PRICE[RUNES[runeId]?.rarity] || 0;
  const runeFitPrice = runeId => Math.round(runePrice(runeId) * RUNE_FIT_SHARE);
  const runeRemovalPrice = runeId => runePrice(runeId) * RUNE_REMOVAL_MULTIPLIER;

  /* ---------- Rune items (item rework phase 4) ---------- */
  const RUNE_ITEM_PREFIX = "rune-";
  const runeItemId = runeId => RUNES[runeId] ? RUNE_ITEM_PREFIX + runeId : null;
  const runeOfItem = id => typeof id === "string" && id.startsWith(RUNE_ITEM_PREFIX) && RUNES[id.slice(RUNE_ITEM_PREFIX.length)] ? id.slice(RUNE_ITEM_PREFIX.length) : null;
  const RUNE_GROUP_NAMES = { scaling: "Scaling rune", element: "Elemental rune" };
  function buildRuneItem(runeId) {
    const rune = RUNES[runeId];
    return {
      id: runeItemId(runeId), category: "rune", type: "rune", name: rune.name, rarity: rune.rarity, rune: runeId, runeGroup: rune.group,
      kindName: RUNE_GROUP_NAMES[rune.group] || "Empowering rune", weaponText: runeText(runeId), armorText: armorRuneText(runeId),
      description: `A carved ${rune.group === "element" ? "elemental " : ""}rune, ready for a runecarver to fit into a forged weapon or piece of armor.`,
      priceSilver: runePrice(runeId), fitPrice: runeFitPrice(runeId), removalPrice: runeRemovalPrice(runeId),
      catalogueAvailable: false, shopAvailable: false, playerSellable: true, droppable: false, automaticDrop: false, effects: [],
    };
  }

  /* ---------- Armor (item rework phase 3) ----------
     Twelve armor types in four categories, each built from a main part and two
     secondary parts. Armor rises with level the way weapon power does; the type
     sets how much, which stat it scales with and how it changes Evasion. */
  const ARMOR_PREFIX = "armor-forged-";
  const AT = (name, category, armor, scaling, evasionPercent, parts) => ({ name, category, armor, scaling, evasionPercent, parts });
  /* Armor by level, calibrated so armored fights end about 1.2x faster than before the
     weapon rework: high from the start (forged weapons hit hard even at level 1), with
     growth from the scaling stat. Clothing trades armor for Evasion. */
  const ARMORS = {
    "full-plate": AT("Full Plate", "heavy", [76, 100], ["vitality", 0.1, 0.22], -60, ["plates", "lining", "fittings"]),
    "splint-mail": AT("Splint Mail", "heavy", [64, 87], ["strength", 0.1, 0.22], -50, ["plates", "lining", "fittings"]),
    chainmail: AT("Chainmail", "heavy", [52, 74], ["strength", 0.1, 0.2], -40, ["plates", "lining", "fittings"]),
    "scale-armor": AT("Scale Armor", "light", [62, 74], ["vitality", 0.08, 0.2], -20, ["shell", "lining", "fittings"]),
    brigandine: AT("Brigandine", "light", [52, 62], ["speed", 0.08, 0.2], -10, ["shell", "lining", "fittings"]),
    "leather-armor": AT("Leather Armor", "light", [42, 50], ["dexterity", 0.08, 0.2], 0, ["shell", "lining", "fittings"]),
    robe: AT("Robe", "clothing", [37, 53], ["intelligence", 0.05, 0.15], 30, ["weave", "lining", "trim"]),
    coat: AT("Coat", "clothing", [42, 58], ["speed", 0.05, 0.15], 35, ["weave", "lining", "trim"]),
    garb: AT("Garb", "clothing", [32, 47], ["dexterity", 0.05, 0.15], 45, ["weave", "lining", "trim"]),
    buckler: AT("Buckler", "shield", [6, 40], ["dexterity", 0.05, 0.14], 0, ["face", "rim", "grip"]),
    "kite-shield": AT("Kite Shield", "shield", [12, 68], ["vitality", 0.05, 0.18], -10, ["face", "rim", "grip"]),
    "tower-shield": AT("Tower Shield", "shield", [18, 95], ["strength", 0.05, 0.22], -20, ["face", "rim", "grip"]),
  };
  const ARMOR_IDS = Object.keys(ARMORS);
  const ARMOR_PARTS = {
    plates: { name: "Plates", kinds: ["metal"] }, shell: { name: "Shell", kinds: ["leather", "metal"] }, weave: { name: "Weave", kinds: ["fabric"] },
    face: { name: "Face", kinds: ["wood", "metal"] }, lining: { name: "Lining", kinds: ["fabric", "leather"] }, fittings: { name: "Fittings", kinds: ["metal", "leather"] },
    trim: { name: "Trim", kinds: ["fabric", "metal"] }, rim: { name: "Rim", kinds: ["metal"] }, grip: { name: "Grip", kinds: ["leather", "wood"] },
  };
  /* Armor effects: armorPct (this piece's armor), evasion, hpPct, resist {element: %},
     resistAll, spellCost, thorns (melee attackers take this share back), lifesteal
     (heal from weapon damage dealt), critDefense, totalArmorPct and pricePct. */
  const ARMOR_MAKERS = {
    roundtable: { categories: ["heavy", "shield"], effect: { armorPct: 10 }, flavor: "Its knightly cut follows the Roundtable's oldest patterns." },
    dwarven: { categories: ["heavy", "shield"], effect: { hpPct: 8 }, flavor: "Dwarven smiths built it thick and solid, to outlast its wearer." },
    elven: { categories: ["light", "clothing"], effect: { evasion: 12 }, flavor: "Elven hands shaped it to move like leaves in the wind." },
    "boreal-bolt": { categories: ["light", "clothing"], effect: { resist: { dark: 15, blood: 15 } }, flavor: "Boreal Bolt cut it bare and quiet, made for hunting the undead." },
    abyss: { categories: ["heavy", "light"], effect: { thorns: 10 }, flavor: "Abyss forges left it bristling and cruel to the touch." },
    hellmade: { categories: ["heavy", "light", "clothing", "shield"], effect: { resist: { fire: 15 }, armorPct: 5 }, flavor: "Hellmade artisans adorned it and finished it without a single flaw." },
    hearthforge: { categories: ["heavy", "light", "shield"], effect: { armorPct: 5, evasion: 6 }, flavor: "Only Hearthforge masters could fit and temper it this finely." },
    outlaw: { categories: ["light", "clothing", "shield"], effect: { armorPct: -10, pricePct: -50 }, flavor: "Outlaw hands lashed it together; it barely holds." },
    "institute-of-magic": { categories: ["clothing"], effect: { spellCost: 5 }, flavor: "Institute tailors cut it plainly and stitched wards along its seams like lecture notes." },
    "church-of-talos": { categories: ["heavy", "clothing", "shield"], effect: { resist: { lightning: 15, holy: 15 } }, flavor: "The Church of Talos marked it with jagged storm sigils in zeal for its lord." },
    // Vampiric armor appears only on vampires and in the vampire castle town.
    vampiric: { categories: ["light", "clothing"], effect: { lifesteal: 5 }, restricted: true, flavor: "Vampiric tailors cut it like a court garment of old blood and high station." },
  };
  // Weapon metals, woods and threads with armor effects, plus leathers and fabrics.
  // onlyOutlaw marks crude materials; noOutlaw marks fine ones the Outlaws never get.
  const AM = (id, name, rarity, kind, effect, extra = {}) => ({ id, code: extra.code || MATERIALS[id]?.code, name, rarity, kind, effect, natural: extra.natural !== false, onlyOutlaw: Boolean(extra.onlyOutlaw), noOutlaw: Boolean(extra.noOutlaw) });
  const FINE = { noOutlaw: true };
  const ARMOR_MATERIAL_LIST = [
    AM("scrap-metal", "Scrap Metal", "common", "metal", { armorPct: -15 }, { onlyOutlaw: true }),
    AM("iron", "Iron", "common", "metal", { armorPct: 3 }),
    AM("steel", "Steel", "common", "metal", { armorPct: 5 }),
    AM("damascus-steel", "Damascus Steel", "uncommon", "metal", { evasion: 4 }),
    AM("dwarven-steel", "Dwarven Steel", "uncommon", "metal", { armorPct: 6 }),
    AM("boreum", "Boreum", "uncommon", "metal", { resist: { ice: 8 } }),
    AM("blacksteel", "Blacksteel", "rare", "metal", { armorPct: 6, resist: { dark: 5 } }),
    AM("lumium", "Lumium", "rare", "metal", { resist: { holy: 8 }, spellCost: 2 }, FINE),
    AM("gold-manyullin-alloy", "Gold-Manyullin Alloy", "rare", "metal", { hpPct: 4, pricePct: 10 }, FINE),
    AM("moonstone", "Moonstone", "rare", "metal", { evasion: 6, resist: { water: 5 } }, FINE),
    AM("vulcan-alloy", "Vulcan Alloy", "epic", "metal", { armorPct: 4, resist: { fire: 10 } }),
    AM("hellstone", "Hellstone", "epic", "metal", { armorPct: 8, resist: { fire: 5 } }),
    AM("abyssal-iron", "Abyssal Iron", "epic", "metal", { resist: { dark: 10 }, thorns: 5 }),
    AM("star-metal", "Star Metal", "legendary", "metal", { armorPct: 8, evasion: 6, resistAll: 4 }, FINE),
    AM("nullstone", "Nullstone", "legendary", "metal", { resistAll: 6 }),
    AM("ancient-boreum", "Ancient Boreum", "legendary", "metal", { armorPct: 8, resist: { ice: 12 } }),
    AM("mythilium", "Mythilium", "masterwork", "metal", { armorPct: 10, evasion: 10, resistAll: 5 }, { natural: false }),
    AM("driftwood", "Driftwood", "common", "wood", { evasion: -5 }, { onlyOutlaw: true }),
    AM("oakwood", "Oakwood", "common", "wood", { armorPct: 3 }),
    AM("quenchwood", "Quenchwood", "common", "wood", { resist: { fire: 6 } }),
    AM("elven-birch", "Elven Birch", "uncommon", "wood", { evasion: 4 }, FINE),
    AM("reinforced-wood", "Reinforced Wood", "uncommon", "wood", { armorPct: 5 }),
    AM("boreal-pine", "Boreal Pine", "rare", "wood", { armorPct: 3, resist: { ice: 8 } }),
    AM("manic-oak", "Manic Oak", "rare", "wood", { thorns: 4, evasion: 3 }),
    AM("elven-greatwood", "Elven Greatwood", "epic", "wood", { evasion: 6, resist: { nature: 8 } }, FINE),
    AM("foundationbark", "Foundationbark", "epic", "wood", { armorPct: 8, resist: { earth: 8 } }),
    AM("lifetree-branch", "Lifetree Branch", "legendary", "wood", { hpPct: 6, resist: { nature: 10 } }, FINE),
    AM("linen", "Linen", "common", "fabric", { evasion: 2 }),
    AM("wool", "Wool", "common", "fabric", { resist: { ice: 5 } }),
    AM("steel-thread", "Steel Thread", "uncommon", "fabric", { armorPct: 4 }),
    AM("magithread", "Magithread", "uncommon", "fabric", { spellCost: 3 }, FINE),
    AM("silk", "Silk", "uncommon", "fabric", { evasion: 4 }, { code: "slk", noOutlaw: true }),
    AM("golden-thread", "Golden Thread", "rare", "fabric", { resist: { holy: 6 }, pricePct: 5 }, FINE),
    AM("dragonvein", "Dragonvein", "rare", "fabric", { resist: { fire: 6 }, hpPct: 2 }),
    AM("velvet", "Velvet", "rare", "fabric", { evasion: 5, spellCost: 2 }, { code: "vel", noOutlaw: true }),
    AM("voidthread", "Voidthread", "epic", "fabric", { resist: { dark: 10 } }),
    AM("woven-mana", "Woven Mana", "epic", "fabric", { spellCost: 5 }, FINE),
    AM("woven-starlight", "Woven Starlight", "legendary", "fabric", { evasion: 8, resist: { holy: 8 } }, FINE),
    AM("gold-vulcan-thread", "Gold-Vulcan Thread", "legendary", "fabric", { armorPct: 4, resist: { fire: 8, lightning: 8 } }),
    AM("rawhide", "Rawhide", "common", "leather", { evasion: 2 }, { code: "raw" }),
    AM("boiled-leather", "Boiled Leather", "common", "leather", { armorPct: 4 }, { code: "boi" }),
    AM("scalehide", "Scalehide", "uncommon", "leather", { resist: { water: 8 } }, { code: "sca" }),
    AM("wolfhide", "Wolfhide", "uncommon", "leather", { evasion: 3, resist: { ice: 4 } }, { code: "wol" }),
    AM("wyvern-hide", "Wyvern Hide", "rare", "leather", { evasion: 3, resist: { wind: 8 } }, { code: "wyv" }),
    AM("shadowhide", "Shadowhide", "rare", "leather", { evasion: 5, resist: { dark: 4 } }, { code: "sha" }),
    AM("drakehide", "Drakehide", "epic", "leather", { armorPct: 4, resist: { fire: 10 } }, { code: "drk" }),
    AM("hellhide", "Hellhide", "epic", "leather", { resist: { fire: 8, blood: 8 } }, { code: "hhd" }),
    AM("dragonhide", "Dragonhide", "legendary", "leather", { armorPct: 8, resist: { fire: 8, lightning: 8, ice: 8 } }, { code: "dgh" }),
  ];
  const ARMOR_MATERIALS = Object.fromEntries(ARMOR_MATERIAL_LIST.map(material => [material.id, material]));
  const ARMOR_MATERIAL_BY_CODE = Object.fromEntries(ARMOR_MATERIAL_LIST.map(material => [material.code, material.id]));
  const ALL_ELEMENTS = ["basic", "fire", "water", "earth", "wind", "lightning", "ice", "holy", "dark", "nature", "death", "blood"];
  // On armor, elemental runes grant 25% resistance (several may share one piece);
  // the others become defensive and scaling runes change the armor's scaling stat.
  const ARMOR_RUNE_RESIST = 25, ARMOR_MAX_SLOTS = 2;
  const ARMOR_RUNE_EFFECTS = { heft: { armorPct: 15 }, precision: { evasion: 15 }, "vital-hit": { critDefense: 8 }, empower: { totalArmorPct: 5 } };
  const ARMOR_ELEMENT_PREFIX = { basic: "Spellproof", fire: "Fireproof", water: "Tideproof", earth: "Stoneproof", wind: "Galeproof", lightning: "Stormproof", ice: "Frostproof", holy: "Sunproof", dark: "Shadowproof", nature: "Thornproof", death: "Deathproof", blood: "Bloodproof" };
  const ARMOR_KEYS = ["armorPct", "evasion", "hpPct", "spellCost", "thorns", "lifesteal", "critDefense", "totalArmorPct", "pricePct"];

  const isArmorSpec = spec => Boolean(ARMORS[spec?.type]);
  function rollArmorSlots(level, random) {
    const value = clampLevel(level);
    let slots = value >= 60 ? 2 : value >= 40 ? 1 : 0;
    if (slots < ARMOR_MAX_SLOTS && random() < 0.04) slots++;
    return slots;
  }
  function armorPartOptions(role, category, rarityIndex, { allowOutlaw = false, allowVampiric = false, onlyMaker = null } = {}) {
    const kinds = ARMOR_PARTS[role].kinds, options = [];
    for (const material of ARMOR_MATERIAL_LIST) {
      if (!material.natural || RARITIES.indexOf(material.rarity) !== rarityIndex || !kinds.includes(material.kind)) continue;
      for (const [makerId, maker] of Object.entries(ARMOR_MAKERS)) {
        if (!maker.categories.includes(category) || onlyMaker && makerId !== onlyMaker) continue;
        if (makerId === "outlaw" ? !allowOutlaw || material.noOutlaw : material.onlyOutlaw) continue;
        if (maker.restricted && !allowVampiric) continue;
        options.push({ maker: makerId, material: material.id });
      }
    }
    return options;
  }
  function pickArmorPart(role, category, level, maxIndex, rules, random) {
    const first = Number.isInteger(rules.rarityIndex) ? rules.rarityIndex : rollRarity(level, maxIndex, random);
    const order = [...Array.from({ length: first + 1 }, (_, index) => first - index), ...Array.from({ length: maxIndex - first }, (_, index) => first + index + 1)];
    for (const rarityIndex of order) {
      const options = armorPartOptions(role, category, rarityIndex, rules);
      if (!options.length) continue;
      const makers = [...new Set(options.map(option => option.maker))];
      const maker = makers[weightedIndex(makers.map(id => id === rules.preferMaker ? 1.5 : 1), random)];
      return { shape: role, ...pick(options.filter(option => option.maker === maker), random) };
    }
    return null;
  }
  function armorRuneConflict(spec, runeId) {
    const rune = RUNES[runeId];
    if (!rune) return "Unknown rune";
    if (spec.runes.length >= spec.slots) return "No empty rune slot";
    if (allRunes(spec).includes(runeId)) return "This rune is already slotted";
    if (rune.group !== "element" && allRunes(spec).some(owned => RUNES[owned].group === rune.group)) return "A rune of this kind is already slotted";
    return "";
  }
  // Generates an armor spec. source "shop" never makes Outlaw armor or fills rune slots.
  // A fixed "rarity" (from a "~rare" placeholder) sets the main part's rarity, as for weapons.
  function generateArmor({ type = null, level = 1, source = "shop", random = Math.random, runeFillChance = 0, allowVampiric = false, maxRarity = null, rarity = null, maker = null, types = null } = {}) {
    const shop = source === "shop", fixed = RARITIES.indexOf(rarity);
    // A fixed main maker (port task 3: Hearth Home's Hearthforge armor, the Institute's clothing) picks among the types it makes.
    const mainMaker = maker && ARMOR_MAKERS[maker] ? maker : null;
    const pool = (Array.isArray(types) && types.length ? types.filter(id => ARMORS[id]) : ARMOR_IDS).filter(id => !mainMaker || ARMOR_MAKERS[mainMaker].categories.includes(ARMORS[id].category));
    const typeId = ARMORS[type] ? type : pick(pool.length ? pool : ARMOR_IDS, random);
    const armor = ARMORS[typeId], value = clampLevel(level);
    const main = pickArmorPart(armor.parts[0], armor.category, value, fixed >= 0 ? fixed : rarityCap(maxRarity), { allowOutlaw: !shop, allowVampiric: allowVampiric || mainMaker === "vampiric", onlyMaker: mainMaker, rarityIndex: fixed >= 0 ? fixed : null }, random);
    const mainIndex = RARITIES.indexOf(ARMOR_MATERIALS[main.material].rarity);
    const secondary = role => pickArmorPart(role, armor.category, value, mainIndex, { allowOutlaw: main.maker === "outlaw", allowVampiric: main.maker === "vampiric", preferMaker: main.maker }, random);
    const spec = { type: typeId, level: value, parts: [main, secondary(armor.parts[1]), secondary(armor.parts[2])], slots: rollArmorSlots(value, random), runes: [] };
    if (!shop && runeFillChance > 0) for (let slot = 0; slot < spec.slots; slot++) {
      if (random() >= runeFillChance) continue;
      const epic = random() < 0.2;
      const pool = RUNE_IDS.filter(id => !armorRuneConflict(spec, id));
      const choices = pool.filter(id => (RUNES[id].rarity === "epic") === epic);
      const chosen = choices.length ? pick(choices, random) : pool.length ? pick(pool, random) : null;
      if (chosen) spec.runes.push(chosen);
    }
    return normalizeSpec(spec);
  }
  function encodeArmor(spec) {
    const normal = normalizeSpec(spec);
    return `${ARMOR_PREFIX}${normal.type}-${clampLevel(normal.level)}-${normal.parts.map(part => MAKERS[part.maker].code + ARMOR_MATERIALS[part.material].code).join("-")}-s${normal.slots}${normal.runes.map(id => RUNES[id].code).join("")}`;
  }
  const ARMOR_ID_PATTERN = new RegExp(`^(${ARMOR_IDS.join("|")})-(\\d{1,3})-([a-z]{5})-([a-z]{5})-([a-z]{5})-s([0-2])((?:[a-z]{3})*)$`);
  function decodeArmor(rest) {
    const match = ARMOR_ID_PATTERN.exec(rest);
    if (!match) return null;
    const [, typeId, levelText, ...tokens] = match;
    const armor = ARMORS[typeId], level = Number(levelText);
    if (level < 1 || level > 100) return null;
    const parts = [];
    for (let index = 0; index < 3; index++) {
      const token = tokens[index], role = armor.parts[index];
      const maker = MAKER_BY_CODE[token.slice(0, 2)], material = ARMOR_MATERIAL_BY_CODE[token.slice(2)];
      if (!maker || !material || !ARMOR_MAKERS[maker]?.categories.includes(armor.category) || !ARMOR_PARTS[role].kinds.includes(ARMOR_MATERIALS[material].kind)) return null;
      parts.push({ shape: role, maker, material });
    }
    const slots = Number(tokens[3]);
    const runes = (tokens[4].match(/.{3}/g) || []).map(code => RUNE_BY_CODE[code]);
    if (runes.some(rune => !rune) || runes.length > slots || new Set(runes).size !== runes.length) return null;
    const groups = runes.map(rune => RUNES[rune].group).filter(group => group !== "element");
    if (new Set(groups).size !== groups.length) return null;
    return normalizeSpec({ type: typeId, level, parts, slots, runes });
  }
  function armorTotals(spec) {
    const sum = Object.fromEntries(ARMOR_KEYS.map(key => [key, 0])), resist = {};
    let scalingStat = null;
    const add = (effect, factor) => {
      for (const key of ARMOR_KEYS) sum[key] += (effect[key] || 0) * factor;
      for (const [element, amount] of Object.entries(effect.resist || {})) resist[element] = (resist[element] || 0) + amount * factor;
      if (effect.resistAll) for (const element of ALL_ELEMENTS) resist[element] = (resist[element] || 0) + effect.resistAll * factor;
    };
    spec.parts.forEach((part, index) => {
      add(ARMOR_MATERIALS[part.material].effect, index === 0 ? 2 : 1);
      if (index > 0) add(ARMOR_MAKERS[part.maker].effect, 1);
    });
    for (const runeId of allRunes(spec)) {
      const rune = RUNES[runeId];
      if (rune.group === "element") resist[rune.effect.element] = (resist[rune.effect.element] || 0) + ARMOR_RUNE_RESIST;
      else if (rune.group === "scaling") scalingStat = rune.effect.secondaryStat;
      else add(ARMOR_RUNE_EFFECTS[runeId] || {}, 1);
    }
    return { ...sum, resist, scalingStat };
  }
  function armorEffectText(effect, doubled = false) {
    const factor = doubled ? 2 : 1, out = [];
    const signed = (value, text) => out.push(`${value > 0 ? "+" : "−"}${Math.abs(value * factor)}${text}`);
    if (effect.armorPct) signed(effect.armorPct, "% armor");
    if (effect.totalArmorPct) signed(effect.totalArmorPct, "% total armor");
    if (effect.evasion) signed(effect.evasion, " Evasion");
    if (effect.hpPct) signed(effect.hpPct, "% max HP");
    for (const [element, amount] of Object.entries(effect.resist || {})) signed(amount, `% ${ELEMENT_NAMES[element]} resistance`);
    if (effect.resistAll) signed(effect.resistAll, "% resistance to every element");
    if (effect.spellCost) out.push(`−${effect.spellCost * factor}% spell cost`);
    if (effect.thorns) out.push(`melee attackers take ${effect.thorns * factor}% of their damage back`);
    if (effect.lifesteal) out.push(`heal ${effect.lifesteal * factor}% of weapon damage dealt`);
    if (effect.critDefense) out.push(`−${effect.critDefense * factor} Crit Rating for attacks against you`);
    if (effect.pricePct) signed(effect.pricePct, "% price");
    return out.join(" · ");
  }
  function armorRuneText(runeId) {
    const rune = RUNES[runeId];
    if (rune.group === "element") return `+${ARMOR_RUNE_RESIST}% ${ELEMENT_NAMES[rune.effect.element]} resistance`;
    if (rune.group === "scaling") return `Armor scaling becomes ${STAT_NAMES[rune.effect.secondaryStat]}`;
    return armorEffectText(ARMOR_RUNE_EFFECTS[runeId] || {});
  }
  function armorStatPrefix(spec, sum) {
    if (spec.parts.every(part => part.maker === spec.parts[0].maker)) return "Matching";
    const scores = [["Fortified", sum.armorPct + sum.totalArmorPct], ["Nimble", sum.evasion], ["Hardy", sum.hpPct], ["Resistant", Math.max(0, ...Object.values(sum.resist))], ["Spiked", sum.thorns], ["Thirsting", sum.lifesteal], ["Arcane", sum.spellCost]];
    const best = scores.reduce((top, entry) => entry[1] > top[1] ? entry : top, ["", 0]);
    return best[1] > 0 ? best[0] : "";
  }
  function armorName(spec, sum = armorTotals(spec)) {
    if (spec.masterwork) return [MASTERWORK_ARMOR_PREFIXES[armorStatPrefix(spec, sum)] || "", MASTERWORKS[spec.masterwork].name].filter(Boolean).join(" ");
    const elements = spec.runes.filter(id => RUNES[id].group === "element");
    const runePrefix = elements.length > 1 ? "Warded" : elements.length ? ARMOR_ELEMENT_PREFIX[RUNES[elements[0]].effect.element] : spec.runes.length ? "Runed" : "";
    return [runePrefix, armorStatPrefix(spec, sum), ARMOR_MATERIALS[spec.parts[0].material].name, ARMORS[spec.type].name].filter(Boolean).join(" ");
  }
  // "A suit of chainmail", "a robe"; plates and fittings are plural, the other parts take "a".
  const ARMOR_NOUNS = { "full-plate": "A suit of full plate", "splint-mail": "A suit of splint mail", chainmail: "A shirt of chainmail", "scale-armor": "A suit of scale armor", "leather-armor": "A suit of leather armor", garb: "A set of garb" };
  function armorDescription(spec) {
    const armor = ARMORS[spec.type];
    if (spec.masterwork) {
      const piece = part => { const shape = ARMOR_PARTS[part.shape].name.toLowerCase(); return `${article(shape)} ${shape} of ${ARMOR_MATERIALS[part.material].name} by ${byName(part.maker)}`; };
      const [main, ...rest] = spec.parts;
      return `A masterwork ${armor.name.toLowerCase()} with ${piece(main)}, ${rest.map(piece).join(" and ")}. ${MASTERWORKS[spec.masterwork].story}`;
    }
    const piece = part => { const shape = ARMOR_PARTS[part.shape].name.toLowerCase(); return `${["plates", "fittings"].includes(part.shape) ? "" : "a "}${shape} of ${ARMOR_MATERIALS[part.material].name} by ${MAKERS[part.maker].byName || MAKERS[part.maker].name}`; };
    const [main, ...rest] = spec.parts;
    const noun = ARMOR_NOUNS[spec.type] || `${/^[aeiou]/i.test(armor.name) ? "An" : "A"} ${armor.name.toLowerCase()}`;
    return `${noun} with ${piece(main)}, ${rest.map(piece).join(" and ")}. ${ARMOR_MAKERS[main.maker].flavor}`;
  }
  const ARMOR_CATEGORY_PRICE = { heavy: 1.2, light: 1, clothing: 0.9, shield: 0.8 };
  function armorPrice(spec, sum = armorTotals(spec)) {
    const armor = ARMORS[spec.type], [main, ...secondary] = spec.parts;
    const partValue = RARITY_PRICE[ARMOR_MATERIALS[main.material].rarity] + secondary.reduce((total, part) => total + RARITY_PRICE[ARMOR_MATERIALS[part.material].rarity] * 0.2, 0);
    const runeValue = spec.runes.reduce((total, id) => total + RUNE_PRICE[RUNES[id].rarity] * 0.4, 0);
    const value = (partValue * (1 + (clampLevel(spec.level) - 1) * 0.01) * ARMOR_CATEGORY_PRICE[armor.category] * (1 + 0.12 * spec.slots) + runeValue) * (1 + clamp(sum.pricePct, -75, 100) / 100);
    return Math.max(10, Math.round(value / 10) * 10);
  }
  function buildArmorItem(spec, id) {
    const armor = ARMORS[spec.type], sum = armorTotals(spec), t = levelCurve(spec.level), main = spec.parts[0];
    const masterwork = spec.masterwork ? MASTERWORKS[spec.masterwork] : null;
    return {
      id, category: "armor", name: armorName(spec, sum), rarity: ARMOR_MATERIALS[main.material].rarity, description: armorDescription(spec),
      type: armor.category, armorValue: Math.max(1, Math.round(lerp(armor.armor[0], armor.armor[1], t) * (1 + (Number(masterwork?.armorBonusPercent) || 0) / 100))), evasionDebuff: 0,
      scaling: component(sum.scalingStat || armor.scaling[0], lerp(armor.scaling[1], armor.scaling[2], t)),
      effects: [...(masterwork ? masterwork.effects.map(effect => ({ ...effect })) : []), ...(sum.spellCost > 0 ? [{ kind: "spell-cost-percent", amount: sum.spellCost, maxTier: null }] : [])],
      priceSilver: armorPrice(spec, sum),
      catalogueAvailable: false, shopAvailable: masterwork ? Boolean(masterwork.shopOffer) : main.maker !== "outlaw", playerSellable: true, droppable: true, automaticDrop: true,
      ...(masterwork?.shopOffer ? { shopOffer: { ...masterwork.shopOffer } } : {}), ...(masterwork?.soldAt ? { soldAt: masterwork.soldAt } : {}),
      armorPercent: sum.armorPct, evasionPercent: armor.evasionPercent, evasionFlat: sum.evasion, hpPercent: sum.hpPct,
      resistances: masterwork?.resist ? Object.fromEntries([...new Set([...Object.keys(sum.resist || {}), ...Object.keys(masterwork.resist)])].map(element => [element, (sum.resist?.[element] || 0) + (masterwork.resist[element] || 0)])) : sum.resist,
      thorns: sum.thorns, lifesteal: sum.lifesteal, critDefense: sum.critDefense, armorTotalPercent: sum.totalArmorPct,
      forged: { ...spec, armor: true, typeName: armor.name, category: armor.category, maker: main.maker, vampiric: main.maker === "vampiric", totals: sum, masterwork: spec.masterwork || null },
    };
  }
  function describeArmorPart(spec, index) {
    const part = spec.parts[index], material = ARMOR_MATERIALS[part.material];
    return { shape: ARMOR_PARTS[part.shape].name, maker: MAKERS[part.maker].name, makerId: part.maker, main: index === 0, material: material.name, rarity: material.rarity,
      effects: armorEffectText(mergeEffects(scaleEffect(material.effect, index === 0 ? 2 : 1), index > 0 ? ARMOR_MAKERS[part.maker].effect : null)) };
  }

  /* ---------- Masterworks (item rework phase 5) ----------
     A masterwork is a forged weapon with a name of its own, a fixed set of parts and an
     effect only it has. Its main part is always Mythilium; each other part may be any
     material that part can take, and is Mythilium a quarter of the time. Masterworks carry
     one prefix from their own set and never the rune prefixes. Ids look like
     weapon-masterwork-<key>-<level>-<main>-<second>-<third>-s<slots><runes>. */
  const MASTERWORK_PREFIX = "weapon-masterwork-";
  const MASTERWORK_TEMPLATE_PREFIX = "masterwork-";
  const MASTERWORK_MYTHILIUM_CHANCE = 0.25;
  const MASTERWORK_PREFIXES = { Matching: "Flawless", Empowered: "Exalted", Piercing: "Rending", Surehit: "Unerring", Critical: "Fateful" };
  // Masterwork armor takes armor prefixes under names of its own.
  const MASTERWORK_ARMOR_PREFIXES = { Matching: "Flawless", Fortified: "Unyielding", Nimble: "Fleeting", Hardy: "Enduring", Resistant: "Sealed", Spiked: "Thorned", Thirsting: "Sanguine", Arcane: "Eldritch" };
  const MASTERWORK_ARMOR_PREFIX = "armor-masterwork-";
  const MASTERWORKS = {
    "stunted-sword": {
      name: "Stunted Sword", type: "longsword", parts: ["short-blade", "crossguard", "short-handle"], mainMaker: "outlaw",
      effects: [{ kind: "damage-percent-vs-wounded", amount: 25, hpPercent: 30, raceBonus: { goblin: 50 } }],
      story: "Short and misshapen, it has an uncanny talent for finishing off the wounded, goblins most of all.",
    },
    "bloody-mary": {
      name: "Bloody Mary", type: "spear", parts: ["spearhead", "crossguard", "long-handle"], mainMaker: "boreal-bolt",
      effects: [{ kind: "element-resistance", elements: ["dark", "death", "blood"], amount: 15 }, { kind: "crit-vs-targets", classIds: ["monster"], raceIds: ["undead"], chance: 30, damage: 25 }],
      story: "A crimson-tipped spear the Ravenwatch blacksmith trades for five Blood Crystals.",
      shopOffer: { regionId: "duskpine-forest", settlementId: "ravenwatch", costItemId: "key-item-blood-crystal", costQuantity: 5 },
    },
    // Giantslayer (user design, 2026-10-11): a Roundtable greatsword whose bite grows with the size
    // of its prey: +10% damage for every 20% more maximum HP than its wielder, up to +60%, and 20%
    // Armor Pierce against anything that much bigger. Zik Lun drops it.
    giantslayer: {
      name: "Giantslayer", type: "greatsword", parts: ["heavy-blade", "crossguard", "short-handle"], mainMaker: "roundtable",
      effects: [{ kind: "damage-and-pierce-vs-larger-max-hp", stepPercent: 20, amount: 10, maxAmount: 60, armorPiercePercent: 20 }],
      story: "Forged by knights to fell giants, it bites deeper the bigger the foe. Zik Lun tore it from the last knight who came for him.",
    },
    // The Royal Trident (user design, 2026-10-11): the Deep Sea King's spear, its main part always
    // Hellmade. Every hit its wielder takes adds +5% to all basic stats, up to +15%, for 2 turns.
    "royal-trident": {
      name: "Royal Trident", type: "spear", parts: ["spearhead", "pommel", "medium-handle"], mainMaker: "hellmade",
      effects: [{ kind: "all-stats-on-damage", amount: 5, maxStacks: 3, durationTurns: 2 }],
      story: "The three-pronged spear of the Deep Sea King, flawless as a pearl; every wound its bearer takes only stirs the tide.",
    },
    // The Anvil Hammer (user design, 2026-10-11): Ornn's mace, its main part always Hearthforge.
    // Every damaging blow adds 15% of its wielder's current Armor as physical damage.
    "anvil-hammer": {
      name: "Anvil Hammer", type: "mace", parts: ["blunt-head", "pommel", "medium-handle"], mainMaker: "hearthforge",
      effects: [{ kind: "bonus-damage-from-armor-percent", amount: 15 }],
      story: "Ornn shaped the first anvil with it; every blow it strikes carries the weight of its bearer's armor.",
    },
    // Hellish Rebuke (user design, 2026-10-12): Archdevil Saffron's spell wand, always a Greater
    // Ruby gem by Hellmade. Fire spells cost 20% less; after taking damage its wielder may spend
    // 50 Mana to return 20% of it as Fire damage. (A caster masterwork: no parts list.)
    "hellish-rebuke": {
      name: "Hellish Rebuke", type: "wand", mainMaker: "hellmade", element: "fire",
      effects: [{ kind: "spell-cost-percent", element: "fire", amount: 20 }, { kind: "mana-retaliation", element: "fire", amount: 20, manaCost: 50 }],
      story: "Archdevil Saffron's infernal wand: it makes his fire cheap, and every wound dealt to him returns as flame.",
    },
    // Quasar Jet (user design, 2026-10-16): the Starscale Blade reforged from Anatemis's scales, a
    // longsword with a pommel instead of a crossguard, its main part always Roundtable. It never
    // misses; each critical hit charges the next attack with +30% damage, and every further
    // critical hit in a row adds another +30%, up to +300%.
    "quasar-jet": {
      name: "Quasar Jet", type: "longsword", parts: ["long-blade", "pommel", "short-handle"], mainMaker: "roundtable",
      effects: [{ kind: "always-hit", amount: 1 }, { kind: "critical-chain-damage-percent", amount: 30, maxAmount: 300 }],
      story: "Forged from the fallen scales of the White Star. It never misses, and each critical strike gathers light for the next, like a star collapsing into a jet of fire.",
    },
    // The Golden Apex (user design, 2026-10-16): Anatemis's spell staff, always a Greater Diamond by
    // the Institute of Magic. Its prism splits the wielder's Basic damage into four quarters: one
    // stays Basic, the other three become random other elements.
    "golden-apex": {
      name: "Golden Apex", type: "staff", mainMaker: "institute-of-magic", element: "basic",
      effects: [{ kind: "prism-basic-damage", amount: 25 }],
      story: "The crown of the White Star's hoard: a diamond that breaks pure force into all the colors of the sky.",
    },
    // Keening (user design, 2026-10-13): a longsword from Ur-Mat's hoard, its main part always
    // Elven. When an attack deals damage, every other enemy takes 40% of it; the echo cannot be
    // reduced.
    keening: {
      name: "Keening", type: "longsword", parts: ["long-blade", "crossguard", "short-handle"], mainMaker: "elven",
      effects: [{ kind: "unreducible-cleave-percent", amount: 40 }],
      story: "A blade from the False Dreamer's hoard that wails as it cuts; every wound it opens echoes through the enemy's ranks.",
    },
    // Sunder (user design, 2026-10-13): a greathammer from Ur-Mat's hoard, its main part always
    // Hearthforge. Enemies it hits lose 10% of their current Mana, shared among the wielder's party.
    sunder: {
      name: "Sunder", type: "greathammer", parts: ["blunt-head", "pommel", "long-handle"], mainMaker: "hearthforge",
      effects: [{ kind: "party-mana-drain-on-hit-percent", amount: 10 }],
      story: "A warhammer from the False Dreamer's hoard that cracks the vessel of a spellcaster's power and pours it out for its bearer's friends.",
    },
    // The Staff of the Guild (user design, 2026-10-12): Ancient Lich Ool's spell staff, always a
    // Greater Death jewel by the Institute of Magic, made for summoners: summon spells cost 10% less,
    // and summoned creatures gain +40% HP and Mana and deal 30% more damage.
    "staff-of-the-guild": {
      name: "Staff of the Guild", type: "staff", mainMaker: "institute-of-magic", element: "death",
      effects: [{ kind: "spell-cost-percent", amount: 10, summonOnly: true }, { kind: "summon-empowerment", amount: 40, manaPercent: 40, damagePercent: 30 }],
      story: "An ancient guildmaster's staff, carried beyond the grave; the dead it calls rise stronger than they ever lived.",
    },
    // The Heartless Enricher (user design, 2026-10-12): Ool's dagger, its main part always Abyss.
    // Off-hand attacks with it ignore all Armor; primary-hand attacks do not.
    "heartless-enricher": {
      name: "Heartless Enricher", type: "dagger", parts: ["short-blade", "crossguard", "short-handle"], mainMaker: "abyss",
      effects: [{ kind: "offhand-armor-pierce-percent", amount: 100 }],
      story: "A heartless collector's dagger, steeped in the wealth of dead empires; from the off-hand it slips through any armor.",
    },
    // The Jackhammer (user design, 2026-10-11): Gaian's greathammer, its main part always Elven.
    // Weapon power +20% of its wielder's Armor; every damaging hit splashes 10% onto all other foes.
    jackhammer: {
      name: "Jackhammer", type: "greathammer", parts: ["blunt-head", "pommel", "long-handle"], mainMaker: "elven",
      effects: [{ kind: "weapon-power-from-armor-percent", amount: 20 }, { kind: "cleave-percent", amount: 10 }],
      story: "The Avatar of the Land swings the living rock itself; every blow shakes the ground beneath the whole battle line.",
    },
    // The Claw of Beowulf (user design, 2026-10-11): a zweihander, its main part always Boreal Bolt,
    // that every wolf may carry. Its hits may mark the target for 2 turns: melee and ranged
    // attacks hit a marked target 20% harder.
    "claw-of-beowulf": {
      name: "Claw of Beowulf", type: "zweihander", parts: ["heavy-blade", "crossguard", "medium-handle"], mainMaker: "boreal-bolt",
      effects: [{ kind: "mark-on-hit", amount: 20, chance: 35, durationTurns: 2 }],
      story: "A storm-black blade that once anointed Talos's sacrifices. The wolves of the north dragged it from the altar long ago, and it still marks whatever it bites.",
    },
    // Masterwork armor: the type is an armor type and the parts are its roles.
    impshield: {
      name: "Impshield", type: "kite-shield", parts: ["face", "rim", "grip"], mainMaker: "abyss",
      effects: [{ kind: "incoming-race-damage-percent", raceIds: ["impid", "demon"], amount: -15 }, { kind: "absorb-element-spell-chance", element: "dark", chance: 5 }],
      story: "Great Imp Ji'ruan's retinue carried it before him; its grinning imp sigil drinks the darkness hurled at it.",
    },
    // Juila's Rootbark (user design, 2026-10-10): living bark grown into scales. It resists
    // Nature, and in every battle it turns with the seasons, every two of its wearer's turns.
    rootbark: {
      name: "Rootbark", type: "scale-armor", parts: ["shell", "lining", "fittings"], mainMaker: "elven", resist: { nature: 15 },
      effects: [{ kind: "turning-seasons", amount: 0, turns: 2, seasons: [
        { id: "spring", name: "Spring", regenHpPercent: 3 },
        { id: "summer", name: "Summer", effects: [{ type: "outgoing-attack-damage-percent", amount: 10, attackKinds: ["melee", "ranged"] }, { type: "spell-power-percent", amount: 10 }] },
        { id: "autumn", name: "Autumn", effects: [{ type: "melee-retaliation-percent", amount: 15, element: "nature" }] },
        { id: "winter", name: "Winter", effects: [{ type: "incoming-weapon-damage-percent", amount: -15 }, { type: "incoming-spell-damage-percent", amount: -15 }] },
      ] }],
      story: "Juila the Grand Forest Matriarch grew it from her own bark; it blossoms, burns, withers and freezes as the seasons turn.",
    },
    // The Stormhide Cuirass (user design, 2026-10-11): leather armor, its main part always the
    // Church of Talos's, sold only in the Church's Storm Reliquary.
    // Doctor's Orders (port task 3, the user's design, 2026-11-06): a white Institute robe sold only at the
    // Institute of Magic, forged at the buyer's level. Every heal the wearer gives (themselves included) is half
    // again as strong; every heal they receive in battle a quarter stronger. Its white look is placeholder art.
    "doctors-orders": {
      name: "Doctor's Orders", type: "robe", parts: ["weave", "lining", "trim"], mainMaker: "institute-of-magic", soldAt: "Institute of Magic", placeholderArt: true, color: "#ece8de",
      effects: [{ kind: "healing-given-percent", amount: 50 }, { kind: "healing-received-percent", amount: 25 }],
      story: "The Institute's healers wear it white so that every stain shows. Its wards were stitched by physicians who refused to lose a patient.",
    },
    "stormhide-cuirass": {
      name: "Stormhide Cuirass", type: "leather-armor", parts: ["shell", "lining", "fittings"], mainMaker: "church-of-talos", soldAt: "Storm Reliquary · Church of Talos",
      effects: [{ kind: "evasion-flat", amount: 10 }, { kind: "incoming-spell-damage-percent", element: "lightning", amount: -50 }],
      story: "Supple stormbeast hide that the Church of Talos threads with lightning. It turns blows aside and drinks half of every lightning spell.",
    },
    // The Godforged Plate (user design, 2026-10-11): Ornn's full plate, its main part always
    // Hearthforge, with a quarter more armor than any other full plate; it locks Evasion at 0.
    "godforged-plate": {
      name: "Godforged Plate", type: "full-plate", parts: ["plates", "lining", "fittings"], mainMaker: "hearthforge", armorBonusPercent: 25,
      effects: [{ kind: "defense-cap", target: "evasion", amount: 0 }],
      story: "Plate the god of the forge made for himself. Nothing pierces it easily, but nothing dodges in it either.",
    },
    // Seventh Cycle (user design, 2026-10-12): Saffron's chainmail, its main part always Hellmade,
    // etched with seven infernal cycles. Every hit taken adds a charge (up to seven); the next
    // damaging attack or spell gains 10% per charge, and 200% at seven.
    "seventh-cycle": {
      name: "Seventh Cycle", type: "chainmail", parts: ["plates", "lining", "fittings"], mainMaker: "hellmade",
      effects: [{ kind: "damage-charge", amount: 10, maximum: 7, fullBonusPercent: 200 }],
      story: "Seven infernal cycles are etched into its links; every wound its wearer takes winds them tighter, until the seventh releases them all at once.",
    },
    // The Soulbound Barrier (user design, 2026-10-11): Gaian's living shroud as garb, its main part
    // always the Institute of Magic's. Armor from a quarter of its wearer's max HP, and a spell ward
    // that weakens as the wearer is hurt.
    "soulbound-barrier": {
      name: "Soulbound Barrier", type: "garb", parts: ["weave", "lining", "trim"], mainMaker: "institute-of-magic",
      effects: [{ kind: "armor-from-max-hp-percent", amount: 25 }, { kind: "missing-hp-spell-ward", amount: 50, missingHpPercentPerPoint: 2 }],
      story: "A living shroud bound to its wearer's soul: it turns spells aside while its wearer is whole.",
    },
    // The Transformer (user design, 2026-10-11): Tankina's kite shield, its main part always the
    // Church of Talos's. Non-Basic spell damage raises its bearer's basic stats.
    transformer: {
      name: "Transformer", type: "kite-shield", parts: ["face", "rim", "grip"], mainMaker: "church-of-talos",
      effects: [{ kind: "all-stats-on-nonbasic-spell-damage-per-tier", amount: 10, durationTurns: 3 }],
      story: "Tankina's copper-bound shield converts sorcery into strength: every spell beyond the Basic that strikes it hardens its bearer.",
    },
    /* The Masterwork Elementals (port task 1, user design, 2026-11-05): the elementals' former unique
       items remade, each dropped at 5% by the elementals of its element. placeholderArt: their looks are
       placeholders (their type's normal look with simple hints) that the user wants fully remade in the
       port. fixedRunes are part of the masterwork: not removable, on top of the rune slots it rolls. The
       stories are drafts the user may change. */
    // Plasma Blade: a Hellmade fire wand (always a Greater Ruby) that attacks as a longsword: power and
    // crit by level, Intelligence then Dexterity at a longsword's ratios, all its attack damage Fire. Its
    // design: a red laser beam from the gem, like a laser sword.
    "plasma-blade": {
      name: "Plasma Blade", type: "wand", mainMaker: "hellmade", element: "fire", placeholderArt: true,
      attacksAs: { type: "longsword", scaling: ["intelligence", "dexterity"], element: "fire" },
      effects: [],
      story: "The Fire Elementals' ruby burns so hot that a blade of red light leaps from it: it casts like a wand and cuts like a sword.",
    },
    // Tidepiercer: a Dwarven lance shaped like a trident, a crossguard instead of a pommel. Its attacks
    // ignore 50% more of the target's armor (added to its parts' pierce, up to 100%). Water rune.
    tidepiercer: {
      name: "Tidepiercer", type: "lance", parts: ["spearhead", "crossguard", "long-handle"], mainMaker: "dwarven", placeholderArt: true,
      fixedRunes: ["humidity"], totalsBonus: { pierce: 50 },
      effects: [{ kind: "armor-pierce-bonus", amount: 50 }],
      story: "Dwarven smiths forged it for the Water Elementals' champion, three-pronged like the tide's own spear; it slips through armor as water through sand.",
    },
    // First Comet: a Hearthforge mace; every hit has a 25% chance to stun for 1 turn. Earth rune.
    "first-comet": {
      name: "First Comet", type: "mace", parts: ["blunt-head", "pommel", "medium-handle"], mainMaker: "hearthforge", placeholderArt: true,
      fixedRunes: ["basalt"],
      effects: [{ kind: "stun-on-hit-chance", amount: 25, chance: 25 }],
      story: "Hammered from the fallen star the Earth Elementals guarded for an age; its blows land like the comet that made it and leave the struck reeling.",
    },
    // Peace and Balance: an Elven quarterstaff (two parts, as every quarterstaff). Nothing pushes its
    // wielder's turn meter back or lowers their Speed, from enemies or their own skills and gear. Wind rune.
    "peace-and-balance": {
      name: "Peace and Balance", type: "quarterstaff", parts: ["staff-shaft", "reinforcement", null], mainMaker: "elven", placeholderArt: true,
      fixedRunes: ["breeze"],
      effects: [{ kind: "turn-and-speed-lock", amount: 0 }],
      story: "The Wind Elementals carried it in the still eye of every storm; no gust can push its bearer from their course.",
    },
    // Kugelblitz: a shortbow whose main part is always the Church of Talos's. 15% of each hit splashes onto
    // every other enemy (reduced by their defenses, like Jackhammer's); every enemy it damaged is marked
    // until the end of its next turn (refreshed, never stacked): its spells cost 50% more Mana. Lightning rune.
    // Lament (port task 5, M19, M20): 2% from the Tide Siren. PLACEHOLDER ART: the normal forged wand, its Greater water gem.
    lament: {
      name: "Lament", type: "wand", mainMaker: "elven", element: "water", placeholderArt: true,
      effects: [{ kind: "extra-target-spell", amount: 50 }],
      story: "A siren's wand of pale driftwood around a drowned aquamarine. Its song reaches one more listener than the caster meant.",
    },
    // Noble Hero (port task 5, M21, M22): 1% from every demon and devil but the bosses. PLACEHOLDER ART: the normal crossbow.
    "noble-hero": {
      name: "Noble Hero", type: "crossbow", parts: ["heft", "mechanism", "strong-string"], mainMaker: "roundtable", placeholderArt: true,
      effects: [{ kind: "second-strike", amount: 25 }],
      story: "A Roundtable crossbow that never strikes a fiend only once: each bolt is answered by a second.",
    },
    // Moneyshot (port task 5, M16, M18): only Sparkling enemies drop it. PLACEHOLDER ART: the normal forged longbow.
    moneyshot: {
      name: "Moneyshot", type: "longbow", parts: ["long-limb", "reinforcement", "light-string"], mainMaker: "outlaw", placeholderArt: true,
      effects: [{ kind: "fifth-attack-boost", amount: 50, every: 5, criticalAmount: 200, echoPercent: 5 }],
      story: "An outlaw's longbow strung with gilded gut. Only Sparkling enemies carry it, and every fifth shot it pays out.",
    },
    kugelblitz: {
      name: "Kugelblitz", type: "shortbow", parts: ["small-limb", "reinforcement", "light-string"], mainMaker: "church-of-talos", placeholderArt: true,
      fixedRunes: ["charge"],
      effects: [{ kind: "cleave-percent", amount: 15 }, { kind: "spell-cost-mark-on-damage", amount: 50, durationTurns: 1 }],
      story: "The Church of Talos strung it with the Lightning Elementals' ball lightning; every arrow bursts across the enemy line and leaves their magic sputtering.",
    },
    // Avalanche: a brigandine whose main part is always Roundtable's. Each damage taken adds a counter; at
    // 7 every enemy loses 30% Speed for 3 turns and the counters reset. Ice resistance rune.
    avalanche: {
      name: "Avalanche", type: "brigandine", parts: ["shell", "lining", "fittings"], mainMaker: "roundtable", placeholderArt: true,
      fixedRunes: ["still"],
      effects: [{ kind: "avalanche-counters", amount: 30, maximum: 7, durationTurns: 3 }],
      story: "Roundtable plates the Ice Elementals buried beneath the glacier; every blow its wearer takes gathers snow, until the whole slope comes down on the enemy.",
    },
  };
  const MASTERWORK_IDS = Object.keys(MASTERWORKS);
  const isArmorMasterwork = def => Boolean(def && ARMORS[def.type]);
  const isCasterMasterwork = def => Boolean(def && WEAPONS[def.type]?.caster);
  const masterworkMakers = shapeId => MAKER_IDS.filter(id => id !== "outlaw" && makerFits(id, shapeId));
  // Makers for a masterwork armor part: those that make this category, never Outlaw or Vampiric.
  const armorMasterworkMakers = category => Object.keys(ARMOR_MAKERS).filter(id => id !== "outlaw" && !ARMOR_MAKERS[id].restricted && ARMOR_MAKERS[id].categories.includes(category));
  /* A masterwork with a "mainMaker" always has that maker's main part, even one the
     maker never forges otherwise (Outlaw's Stunted Sword, Boreal Bolt's Bloody Mary).
     The other two parts never come from Outlaw. */
  const masterworkMainMakerOk = (def, maker) => def.mainMaker ? maker === def.mainMaker
    : isArmorMasterwork(def) ? armorMasterworkMakers(ARMORS[def.type].category).includes(maker) : maker !== "outlaw" && makerFits(maker, def.parts[0]);
  // Masterworks are forged at the level of the character who gets them.
  function generateMasterwork({ key, level = 1, random = Math.random } = {}) {
    const def = MASTERWORKS[key];
    if (!def) return null;
    // A caster masterwork: its maker's stone of its element, always a Greater cut, the finest handle.
    if (isCasterMasterwork(def)) {
      const spec = generateCaster(def.type, level, random, false, 4, { element: def.element || null, rarityIndex: 4, maker: def.mainMaker });
      // Like every masterwork's, its main maker is fixed, even one that never cuts this element's
      // stone otherwise (the Institute's Hematite in the Staff of the Guild).
      const stones = Object.keys(STONES).filter(id => !def.element || STONES[id].element === def.element);
      if (spec.parts[0].maker !== def.mainMaker || !stones.includes(spec.parts[0].material)) spec.parts[0] = { ...spec.parts[0], maker: def.mainMaker, material: stones.find(id => STONES[id].makers.includes(def.mainMaker)) || stones[0] };
      spec.parts[0].tier = 3;
      return { ...spec, masterwork: key };
    }
    const armor = isArmorMasterwork(def), category = armor ? ARMORS[def.type].category : null;
    const value = clampLevel(level), mainMaker = def.mainMaker || pick(armor ? armorMasterworkMakers(category) : masterworkMakers(def.parts[0]), random);
    if (armor) {
      const secondary = role => {
        if (random() < MASTERWORK_MYTHILIUM_CHANCE) {
          const makers = armorMasterworkMakers(category);
          return { shape: role, maker: makers[weightedIndex(makers.map(id => id === mainMaker ? 1.5 : 1), random)], material: "mythilium" };
        }
        return pickArmorPart(role, category, value, 4, { allowOutlaw: false, preferMaker: mainMaker }, random);
      };
      return normalizeSpec({ type: def.type, masterwork: key, level: value, parts: [{ shape: def.parts[0], maker: mainMaker, material: "mythilium" }, secondary(def.parts[1]), secondary(def.parts[2])], slots: rollArmorSlots(value, random), runes: [] });
    }
    const secondary = shapeId => {
      if (random() < MASTERWORK_MYTHILIUM_CHANCE) {
        const makers = masterworkMakers(shapeId);
        return { shape: shapeId, maker: makers[weightedIndex(makers.map(id => id === mainMaker ? 1.5 : 1), random)], material: "mythilium" };
      }
      return pickPart(shapeId, value, 4, { allowOutlaw: false, preferMaker: mainMaker }, random);
    };
    return normalizeSpec({ type: def.type, masterwork: key, level: value, parts: [{ shape: def.parts[0], maker: mainMaker, material: "mythilium" }, secondary(def.parts[1]), def.parts[2] ? secondary(def.parts[2]) : null], slots: rollSlots(value, random), runes: [] });
  }
  function encodeMasterwork(spec) {
    if (isCasterMasterwork(MASTERWORKS[spec.masterwork])) {
      const [main, handle, inscription] = spec.parts;
      return `${MASTERWORK_PREFIX}${spec.masterwork}-${clampLevel(spec.level)}-${MAKERS[main.maker].code}${STONES[main.material].code}${main.tier}-${MAKERS[handle.maker].code}${CASTER_HANDLES[handle.material].code}-${MAKERS[inscription.maker].code}${INKS[inscription.material].code}`;
    }
    const normal = normalizeSpec(spec), armor = isArmorMasterwork(MASTERWORKS[normal.masterwork]);
    const token = part => !part ? "x" : MAKERS[part.maker].code + (armor ? ARMOR_MATERIALS[part.material].code : MATERIALS[part.material].code);
    return `${armor ? MASTERWORK_ARMOR_PREFIX : MASTERWORK_PREFIX}${normal.masterwork}-${clampLevel(normal.level)}-${normal.parts.map(token).join("-")}-s${normal.slots}${normal.runes.map(id => RUNES[id].code).join("")}`;
  }
  function decodeArmorMasterwork(rest) {
    const match = /^([a-z]+(?:-[a-z]+)*)-(\d{1,3})-([a-z]{5})-([a-z]{5})-([a-z]{5})-s([0-2])((?:[a-z]{3})*)$/.exec(rest);
    if (!match) return null;
    const [, key, levelText, ...tokens] = match, def = MASTERWORKS[key], level = Number(levelText);
    if (!isArmorMasterwork(def) || level < 1 || level > 100) return null;
    const category = ARMORS[def.type].category, parts = [];
    for (let index = 0; index < 3; index++) {
      const role = def.parts[index], maker = MAKER_BY_CODE[tokens[index].slice(0, 2)], material = ARMOR_MATERIAL_BY_CODE[tokens[index].slice(2)];
      const makerOk = index === 0 ? masterworkMainMakerOk(def, maker) : armorMasterworkMakers(category).includes(maker);
      if (!maker || !material || !makerOk || material !== "mythilium" && (index === 0 || !ARMOR_PARTS[role].kinds.includes(ARMOR_MATERIALS[material].kind))) return null;
      parts.push({ shape: role, maker, material });
    }
    const slots = Number(tokens[3]), runes = (tokens[4].match(/.{3}/g) || []).map(code => RUNE_BY_CODE[code]);
    if (runes.some(rune => !rune) || runes.length > slots || new Set(runes).size !== runes.length) return null;
    const groups = runes.map(rune => RUNES[rune].group).filter(group => group !== "element");
    if (new Set(groups).size !== groups.length) return null;
    return normalizeSpec({ type: def.type, masterwork: key, level, parts, slots, runes });
  }
  function decodeMasterwork(rest) {
    const casterMatch = /^([a-z]+(?:-[a-z]+)*)-(\d{1,3})-([a-z]{2})([a-z]{3})([1-3])-([a-z]{2})([a-z]{3})-([a-z]{2})([a-z]{3})$/.exec(rest);
    if (casterMatch && isCasterMasterwork(MASTERWORKS[casterMatch[1]])) {
      const [, key, levelText, mainCode, stoneCode, tierText, handleMakerCode, handleCode, inkMakerCode, inkCode] = casterMatch, def = MASTERWORKS[key], weapon = WEAPONS[def.type], level = Number(levelText);
      const stone = STONE_BY_CODE[stoneCode], handle = CASTER_HANDLE_BY_CODE[handleCode], ink = INK_BY_CODE[inkCode];
      const handleMaker = MAKER_BY_CODE[handleMakerCode], inkMaker = MAKER_BY_CODE[inkMakerCode];
      const makes = (maker, role) => Boolean(CASTER_MAKERS[maker]?.parts.includes(role));
      if (level < 1 || level > 100 || MAKER_BY_CODE[mainCode] !== def.mainMaker || !stone || def.element && STONES[stone].element !== def.element || !handle || !ink || !makes(handleMaker, "handle") || !makes(inkMaker, "inscription")) return null;
      return { type: def.type, masterwork: key, level, parts: [{ shape: weapon.caster, maker: def.mainMaker, material: stone, tier: Number(tierText) }, { shape: weapon.parts[1], maker: handleMaker, material: handle }, { shape: "inscription", maker: inkMaker, material: ink }], slots: 0, runes: [] };
    }
    const match = /^([a-z]+(?:-[a-z]+)*)-(\d{1,3})-([a-z]{5})-([a-z]{5})-([a-z]{5}|x)-s([0-3])((?:[a-z]{3})*)$/.exec(rest);
    if (!match) return null;
    const [, key, levelText, ...tokens] = match, def = MASTERWORKS[key], level = Number(levelText);
    if (!def || isArmorMasterwork(def) || level < 1 || level > 100) return null;
    const parts = [];
    for (let index = 0; index < 3; index++) {
      // A masterwork without a third part (a quarterstaff) has "x" in its place.
      if (!def.parts[index] || tokens[index] === "x") {
        if (def.parts[index] || tokens[index] !== "x") return null;
        parts.push(null);
        continue;
      }
      const shape = def.parts[index], maker = MAKER_BY_CODE[tokens[index].slice(0, 2)], material = MATERIAL_BY_CODE[tokens[index].slice(2)];
      const makerOk = index === 0 ? masterworkMainMakerOk(def, maker) : maker !== "outlaw" && makerFits(maker, shape);
      if (!maker || !material || !makerOk || material !== "mythilium" && (index === 0 || !materialFits(MATERIALS[material], shape))) return null;
      parts.push({ shape, maker, material });
    }
    const slots = Number(tokens[3]), runes = (tokens[4].match(/.{3}/g) || []).map(code => RUNE_BY_CODE[code]);
    if (runes.some(rune => !rune) || runes.length > slots || new Set(runes.map(rune => RUNES[rune].group)).size !== runes.length) return null;
    return normalizeSpec({ type: def.type, masterwork: key, level, parts, slots, runes });
  }
  function masterworkDescription(spec) {
    const def = MASTERWORKS[spec.masterwork], weapon = WEAPONS[spec.type];
    const piece = part => { const shape = SHAPES[part.shape].name.toLowerCase(); return `${article(shape)} ${shape} of ${MATERIALS[part.material].name} by ${byName(part.maker)}`; };
    const [main, ...rest] = spec.parts;
    const others = rest.filter(Boolean).map(piece);
    return `A masterwork ${weapon.name.toLowerCase()} with ${piece(main)}${others.length > 1 ? ", " : others.length ? " and " : ""}${others.join(" and ")}. ${def.story}`;
  }
  // Drop tables and shop offers name a masterwork by its template; the real piece is forged when it is found.
  const masterworkTemplateKey = id => typeof id === "string" && id.startsWith(MASTERWORK_TEMPLATE_PREFIX) && MASTERWORKS[id.slice(MASTERWORK_TEMPLATE_PREFIX.length)] ? id.slice(MASTERWORK_TEMPLATE_PREFIX.length) : null;
  const masterworkTemplateId = key => MASTERWORKS[key] ? MASTERWORK_TEMPLATE_PREFIX + key : null;
  function buildMasterworkTemplate(key) {
    const def = MASTERWORKS[key], kind = WEAPONS[def.type] || ARMORS[def.type], sample = buildItem(generateMasterwork({ key, level: 50, random: () => 0.5 }));
    return { ...sample, id: masterworkTemplateId(key), name: def.name, masterworkTemplate: key, forged: undefined, catalogueAvailable: false, shopAvailable: false, effects: def.effects.map(effect => ({ ...effect })),
      description: `A masterwork ${kind.name.toLowerCase()} whose Mythilium is forged to the level of whoever ${def.soldAt ? "buys" : "finds"} it. ${def.story}` };
  }

  /* ---------- Natural weapons (item rework phase 5) ----------
     Beasts fight with claws or horns rather than a held weapon. An attack costs 2 AP like
     any weapon attack and hits like an uncommon forged two-handed weapon of the beast's
     level: a greatsword's curve with the average part bonuses of uncommon two-handed
     weapons. Anatemis's Stardragon Claws hit like a Level 100 masterwork, with the average
     part bonuses of Level 100 masterworks (both measured from the forge's own rolls). */
  const NATURAL_UNCOMMON = { finalPct: 5, basePowerPct: 5, primaryPct: 8, secondaryPct: 8, hit: 15, crit: 3, pierce: 6 };
  const NATURAL_MASTERWORK = { finalPct: 18, basePowerPct: 5, primaryPct: 8, secondaryPct: 9, hit: 17, crit: 17, pierce: 20 };
  const NATURAL_WEAPONS = {
    "beast-claws": { name: "Beast Claws", primary: "strength", secondary: "dexterity", crit: [4, 15], description: "Natural claws, as sharp as the beast is seasoned." },
    "sunhorn-antlers": { name: "Sunhorn Antlers", primary: "strength", secondary: "dexterity", crit: [4, 18], description: "Antlers that burn like midsummer noon, swept low to gore and toss." },
    "vulcanic-ram-horns": { name: "Vulcanic Ram Horns", primary: "strength", secondary: "vitality", crit: [3, 25], description: "Molten horns and a stone-plated skull made to batter the gates of a god's forge." },
    "stardragon-claws": { name: "Stardragon Claws", primary: "strength", secondary: "dexterity", crit: [4, 15], level: 100, masterwork: true, description: "Anatemis's celestial claws, harder than starmetal and bright as a dying star." },
  };
  const NATURAL_CURVE = { power: [12, 130], primary: [0.3, 1.5], secondary: [0.1, 0.6] };
  function naturalWeapon(kind, level = 1) {
    const def = NATURAL_WEAPONS[kind];
    if (!def) return null;
    const value = clampLevel(def.level || level), t = levelCurve(value);
    const bonus = def.masterwork ? NATURAL_MASTERWORK : NATURAL_UNCOMMON;
    return {
      id: "natural-" + kind, natural: kind, category: "weapon", name: def.name, description: def.description, rarity: def.masterwork ? "masterwork" : "uncommon",
      type: "meele", handed: 2, level: value, effects: [], minimumStrength: 0,
      basePower: Math.max(1, Math.round(lerp(NATURAL_CURVE.power[0], NATURAL_CURVE.power[1], t) * (1 + (bonus.basePowerPct || 0) / 100))),
      primary: component(def.primary, lerp(NATURAL_CURVE.primary[0], NATURAL_CURVE.primary[1], t) * (1 + (bonus.primaryPct || 0) / 100)),
      secondary: component(def.secondary, lerp(NATURAL_CURVE.secondary[0], NATURAL_CURVE.secondary[1], t) * (1 + (bonus.secondaryPct || 0) / 100)),
      finalPowerPercent: bonus.finalPct || 0, hitBonus: bonus.hit || 0, critBonus: bonus.crit || 0, armorPierce: bonus.pierce || 0,
      critBase: def.crit[0], critDamage: def.crit[1], priceSilver: 0, catalogueAvailable: false, shopAvailable: false, playerSellable: false, droppable: false, automaticDrop: false,
    };
  }

  /* ---------- Placeholders and retired catalogue items ----------
     Enemy files name the kind of gear an enemy fights with, such as
     "forge-weapon-1-longsword+rapier" (held in one hand; the first type is the likeliest)
     or "forge-armor-full-plate+splint-mail". armCharacter forges a real piece from it when
     the enemy appears. The old catalogue weapons and armor were retired for forged pieces;
     LEGACY_ITEMS maps each old id to its placeholder so old saves can be converted. */
  const TEMPLATE_PATTERN = /^forge-(weapon|armor)-(?:([12])-)?([a-z-]+(?:\+[a-z-]+)*)(?:~([a-z]+))?(?:@([a-z]+))?$/;
  function templateSpec(id) {
    const match = typeof id === "string" ? TEMPLATE_PATTERN.exec(id) : null;
    if (!match) return null;
    const [, category, handed, list, rarity, element] = match, types = list.split("+");
    if (category === "weapon" ? !handed || !types.every(type => WEAPONS[type]) : handed || !types.every(type => ARMORS[type])) return null;
    // An optional "~rare" fixes the rarity of the main part, and so of the piece.
    if (rarity && (!RARITIES.includes(rarity) || rarity === "masterwork")) return null;
    // An optional "@nature" makes a staff or wand of that element (its jewel or gem and its ink).
    if (element && (category !== "weapon" || !types.every(type => WEAPONS[type].caster) || !Object.values(STONES).some(stone => stone.element === element))) return null;
    return { category, handed: Number(handed) || null, types, rarity: rarity || null, element: element || null };
  }
  const isTemplateId = id => Boolean(templateSpec(id));
  function buildTemplateItem(id) {
    const spec = templateSpec(id);
    if (!spec) return null;
    const first = spec.category === "weapon" ? WEAPONS[spec.types[0]] : ARMORS[spec.types[0]];
    const base = { id, category: spec.category, name: "Forged " + first.name, rarity: spec.rarity || "common", forgedAs: spec.types, forgedRarity: spec.rarity, forgedElement: spec.element, placeholder: true, effects: [],
      description: "A placeholder for gear that is forged when its bearer appears.", priceSilver: 0, catalogueAvailable: false, shopAvailable: false, playerSellable: false, droppable: false, automaticDrop: false };
    if (spec.category === "armor") return { ...base, type: first.category, armorValue: 0, evasionDebuff: 0, scaling: component(first.scaling[0], 0) };
    return { ...base, type: first.caster ? first.itemType : first.ranged ? "ranged" : "meele", handed: spec.handed, basePower: 0, primary: component(first.primary?.[0] || "strength", 0), secondary: component("strength", 0) };
  }
  const LEGACY_ITEMS = Object.fromEntries(Object.entries({
      "forge-weapon-1-longsword+rapier": ["weapon-iron-sword-meele", "weapon-elongated-iron-sword-meele", "weapon-steel-sword-meele", "weapon-damascus-sword-meele", "weapon-dwarven-steel-sword-meele", "weapon-black-steel-sword-meele", "weapon-manasteel-sword-meele", "weapon-lumium-sword-meele", "weapon-vulcan-alloy-sword-meele", "weapon-hellsteel-sword-meele", "weapon-voidmetal-sword-meele", "weapon-starmetal-sword-meele"],
      "forge-weapon-1-dagger": ["weapon-iron-dagger-meele", "weapon-steel-dagger-meele", "weapon-damascus-dagger-meele", "weapon-dwarven-steel-dagger-meele", "weapon-black-steel-dagger-meele", "weapon-manasteel-dagger-meele", "weapon-lumium-dagger-meele", "weapon-vulcan-alloy-dagger-meele", "weapon-hellsteel-dagger-meele", "weapon-voidmetal-dagger-meele", "weapon-starmetal-dagger-meele"],
      "forge-weapon-2-greatsword+zweihander": ["weapon-iron-greatsword-meele", "weapon-steel-greatsword-meele", "weapon-damascus-greatsword-meele", "weapon-dwarven-steel-greatsword-meele", "weapon-black-steel-greatsword-meele", "weapon-manasteel-greatsword-meele", "weapon-lumium-greatsword-meele", "weapon-vulcan-alloy-greatsword-meele", "weapon-hellsteel-greatsword-meele", "weapon-voidmetal-greatsword-meele", "weapon-starmetal-greatsword-meele"],
      "forge-weapon-2-shortbow": ["weapon-wooden-bow-ranged", "weapon-crude-bow-ranged", "weapon-reinforced-bow-ranged"],
      "forge-weapon-1-club": ["weapon-stone-club-meele"],
      "forge-weapon-1-mace+flail": ["weapon-iron-mace-meele", "weapon-steel-mace-meele", "weapon-damascus-mace-meele", "weapon-dwarven-steel-mace-meele", "weapon-black-steel-mace-meele", "weapon-manasteel-mace-meele", "weapon-lumium-mace-meele", "weapon-vulcan-alloy-mace-meele", "weapon-hellsteel-mace-meele", "weapon-voidmetal-mace-meele", "weapon-starmetal-mace-meele"],
      "forge-weapon-2-greataxe+axe": ["weapon-iron-axe-meele", "weapon-woodcutters-axe-meele", "weapon-steel-axe-meele", "weapon-damascus-axe-meele", "weapon-dwarven-steel-axe-meele", "weapon-black-steel-axe-meele", "weapon-manasteel-axe-meele", "weapon-lumium-axe-meele", "weapon-vulcan-alloy-axe-meele", "weapon-hellsteel-axe-meele", "weapon-voidmetal-axe-meele", "weapon-starmetal-axe-meele"],
      "forge-weapon-2-longbow+compoundbow": ["weapon-longbow-ranged", "weapon-scaled-bow-ranged"],
      "forge-weapon-2-warbow+ballistabow": ["weapon-greatbow-ranged", "weapon-cannonshot-greatbow-ranged"],
      "forge-weapon-2-compoundbow+longbow": ["weapon-silverplate-bow-ranged", "weapon-quickgold-bow-ranged"],
      "forge-weapon-2-crossbow": ["weapon-grand-bolt-bow-ranged"],
      "forge-weapon-2-longbow+shortbow": ["weapon-elvenwood-bow-ranged"],
      "forge-weapon-2-ballistabow+warbow": ["weapon-ballista-warbow-ranged"],
      "forge-weapon-1-wand": ["weapon-ash-wand-magic-wand", "weapon-copper-wand-magic-wand", "weapon-apprentice-wand-magic-wand", "weapon-runed-wand-magic-wand", "weapon-crystal-wand-magic-wand", "weapon-sagewood-wand-magic-wand", "weapon-manacrystal-wand-magic-wand", "weapon-arcanist-wand-magic-wand", "weapon-astral-wand-magic-wand", "weapon-phoenix-wand-magic-wand", "weapon-voidglass-wand-magic-wand", "weapon-starmetal-wand-magic-wand", "weapon-galebound-wand-magic-wand"],
      "forge-weapon-2-staff": ["weapon-ash-staff-spell-staff", "weapon-copperbound-staff-spell-staff", "weapon-pilgrim-staff-spell-staff", "weapon-runed-staff-spell-staff", "weapon-crystal-staff-spell-staff", "weapon-sagewood-staff-spell-staff", "weapon-manacrystal-staff-spell-staff", "weapon-arcanist-staff-spell-staff", "weapon-astral-staff-spell-staff", "weapon-phoenix-staff-spell-staff", "weapon-voidglass-staff-spell-staff", "weapon-celestial-staff-spell-staff", "weapon-cinderheart-staff-spell-staff"],
      "forge-weapon-2-spear+lance": ["weapon-iron-spear-meele", "weapon-steel-spear-meele", "weapon-damascus-spear-meele", "weapon-dwarven-steel-spear-meele", "weapon-black-steel-spear-meele", "weapon-manasteel-spear-meele", "weapon-lumium-spear-meele", "weapon-vulcan-alloy-spear-meele", "weapon-hellsteel-spear-meele", "weapon-voidmetal-spear-meele", "weapon-starmetal-spear-meele"],
      "forge-armor-full-plate+chainmail": ["armor-iron-armor-heavy"],
      "forge-armor-brigandine+leather-armor": ["armor-linen-armor-light"],
      "forge-armor-coat": ["armor-wool-cloak-clothing", "armor-travelers-coat-clothing", "armor-rangers-mantle-clothing", "armor-lumium-thread-coat-clothing"],
      "forge-armor-buckler": ["armor-wooden-shield-shield"],
      "forge-armor-full-plate": ["armor-full-plated-iron-armor-heavy", "armor-dwarven-steel-plate-heavy", "armor-lumium-full-plate-heavy", "armor-hellsteel-fortress-heavy", "armor-starmetal-plate-heavy"],
      "forge-armor-garb": ["armor-rags-clothing", "armor-workers-clothes-clothing", "armor-vitalweave-garb-clothing", "armor-fateweave-garb-clothing"],
      "forge-armor-leather-armor": ["armor-fur-armor-light", "armor-leather-armor-light", "armor-hardened-leather-light", "armor-elvenwood-leafmail-light", "armor-hellhide-light"],
      "forge-armor-buckler+kite-shield": ["armor-iron-shield-shield"],
      "forge-armor-garb+coat": ["armor-noble-clothing-clothing"],
      "forge-armor-robe": ["armor-magicians-robe-clothing", "armor-enchanters-robe-clothing", "armor-manasilk-robe-clothing", "armor-moonweave-vestments-clothing", "armor-phoenixweave-robe-clothing", "armor-voidweave-raiment-clothing", "armor-starlight-robe-clothing"],
      "forge-armor-leather-armor+scale-armor": ["armor-hide-armor-light"],
      "forge-armor-chainmail": ["armor-chainmail-heavy"],
      "forge-armor-robe+garb": ["armor-silk-battlerobe-clothing"],
      "forge-armor-scale-armor": ["armor-scalehide-light", "armor-quickgold-mail-light", "armor-dragonscale-harness-light"],
      "forge-armor-brigandine": ["armor-scout-brigandine-light", "armor-black-steel-brigandine-light", "armor-lumium-chainweave-light", "armor-manasteel-spellguard-light", "armor-voidmetal-chainweave-light"],
      "forge-armor-brigandine+scale-armor": ["armor-damascus-chain-shirt-light"],
      "forge-armor-full-plate+splint-mail": ["armor-damascus-plate-heavy", "armor-black-steel-plate-heavy", "armor-vulcan-alloy-plate-heavy"],
      "forge-armor-kite-shield": ["armor-reinforced-shield-shield", "armor-damascus-shield-shield", "armor-manasteel-aegis-shield", "armor-voidmetal-ward-shield", "armor-celestial-ward-shield"],
      "forge-armor-tower-shield": ["armor-dwarven-tower-shield-shield", "armor-bedrock-shield-shield"],
      "forge-armor-splint-mail+full-plate": ["armor-manasteel-bulwark-heavy"],
      "forge-armor-kite-shield+tower-shield": ["armor-black-steel-shield-shield", "armor-vulcan-alloy-shield-shield"],
      "forge-armor-tower-shield+kite-shield": ["armor-starmetal-aegis-shield"],
      "forge-weapon-1-rapier": ["weapon-winged-piercer-meele"],
      "forge-weapon-2-spear": ["weapon-tidepiercer-spear-meele"],
      "forge-weapon-1-mace": ["weapon-stonecore-mace-meele"],
      "forge-weapon-1-longsword": ["weapon-stormbrand-sword-meele"],
      "forge-weapon-2-greatsword": ["weapon-frosthewn-greatsword-meele"],
  }).flatMap(([template, ids]) => ids.map(id => [id, template])));
  // The old catalogue masterworks are now forged recipes; the natural weapons are no items at all.
  Object.assign(LEGACY_ITEMS, { "weapon-stunted-sword-meele": "masterwork-stunted-sword", "weapon-bloody-mary-meele": "masterwork-bloody-mary", "armor-impshield-shield": "masterwork-impshield",
    // Juila's Branch of the Forest was retired (user decision, 2026-10-10); she carries a forged Nature staff instead.
    "weapon-branch-of-the-forest-spell-staff": "forge-weapon-2-staff~epic@nature",
    // The Great Forest Plate became the Rootbark masterwork (user decision, 2026-10-10).
    "armor-great-forest-plate-light": "masterwork-rootbark",
    // Zik Lun's Puny Smasher and Great Steel-Plated Warsuit were retired (user decision, 2026-10-11).
    "weapon-puny-smasher-meele": "forge-weapon-2-greathammer~epic", "armor-great-steel-plated-warsuit-heavy": "forge-armor-full-plate~epic",
    // Giantslayer became a forged masterwork (user design, 2026-10-11).
    "weapon-giantslayer-meele": "masterwork-giantslayer",
    // The Royal Trident became a forged masterwork (user design, 2026-10-11).
    "weapon-royal-trident-meele": "masterwork-royal-trident",
    // Tankina's Claw of Beowulf, Stormhide Cuirass and Transformer became forged masterworks (user design, 2026-10-11).
    "weapon-claw-of-beowulf-meele": "masterwork-claw-of-beowulf",
    // Ornn's Anvil Hammer and Godforged Plate became forged masterworks (user design, 2026-10-11).
    "weapon-anvil-hammer-meele": "masterwork-anvil-hammer", "armor-godforged-plate-heavy": "masterwork-godforged-plate",
    // Gaian's Jackhammer and Soulbound Barrier became forged masterworks (user design, 2026-10-11).
    "weapon-jackhammer-meele": "masterwork-jackhammer", "armor-soulbound-barrier-clothing": "masterwork-soulbound-barrier",
    // Saffron's Hellish Rebuke and Seventh Cycle became forged masterworks (user design, 2026-10-12).
    "weapon-hellish-rebuke-magic-wand": "masterwork-hellish-rebuke", "armor-seventh-cycle-heavy": "masterwork-seventh-cycle", "armor-stormhide-cuirass-light": "masterwork-stormhide-cuirass", "armor-transformer-shield": "masterwork-transformer",
    // Ool's Staff of the Guild and Heartless Enricher became forged masterworks (user design, 2026-10-12).
    "weapon-staff-of-the-guild-spell-staff": "masterwork-staff-of-the-guild", "weapon-heartless-enricher-meele": "masterwork-heartless-enricher",
    // Ur-Mat's Keening and Sunder became forged masterworks (user design, 2026-10-13).
    "weapon-keening-meele": "masterwork-keening", "weapon-sunder-meele": "masterwork-sunder",
    // Anatemis's Starscale Blade became Quasar Jet, and her Golden Apex a forged staff (user design, 2026-10-16).
    "weapon-starscale-blade-meele": "masterwork-quasar-jet", "weapon-golden-apex-spell-staff": "masterwork-golden-apex" });
  const RETIRED_ITEMS = new Set(["weapon-beast-claws-meele", "weapon-vulcanic-ram-horns-meele", "weapon-stardragon-claws-meele"]);
  // A repeatable random sequence from a text, so converting the same save twice gives the same pieces.
  function seededRandom(text) {
    let state = 2166136261;
    for (let index = 0; index < text.length; index++) state = Math.imul(state ^ text.charCodeAt(index), 16777619) >>> 0;
    state ||= 1;
    return () => { state ^= state << 13; state >>>= 0; state ^= state >>> 17; state ^= state << 5; state >>>= 0; return state / 4294967296; };
  }
  // Old saves: each retired catalogue piece becomes a forged piece of the same kind at the
  // character's level, held the same way. Equipped pieces stay equipped, favorites stay favorites.
  function fixedMasterworkId(id) {
    const prefix = typeof id === "string" ? [MASTERWORK_PREFIX, MASTERWORK_ARMOR_PREFIX].find(candidate => id.startsWith(candidate)) : null;
    if (!prefix) return id;
    const match = /^([a-z]+(?:-[a-z]+)*)-(\d{1,3})-([a-z]{2})([a-z]{3})-(.+)$/.exec(id.slice(prefix.length));
    const def = match && MASTERWORKS[match[1]];
    if (!def?.mainMaker || MAKER_BY_CODE[match[3]] === def.mainMaker) return id;
    return `${prefix}${match[1]}-${match[2]}-${MAKERS[def.mainMaker].code}${match[4]}-${match[5]}`;
  }
  function migrateLegacyItems({ inventory, equipment, favorites }, level, seed = "") {
    const made = new Map();
    let fixedMakers = false;
    const fixList = list => { if (Array.isArray(list)) list.forEach((id, index) => { const fixed = fixedMasterworkId(id); if (fixed !== id) { list[index] = fixed; fixedMakers = true; } }); };
    fixList(inventory); fixList(favorites);
    if (equipment) for (const slot of Object.keys(equipment)) { const fixed = fixedMasterworkId(equipment[slot]); if (fixed !== equipment[slot]) { equipment[slot] = fixed; fixedMakers = true; } }
    const forgeFor = (legacyId, copy) => {
      const random = seededRandom(`${seed}|${legacyId}|${copy}`), masterworkKey = masterworkTemplateKey(LEGACY_ITEMS[legacyId]);
      if (masterworkKey) return encode(generateMasterwork({ key: masterworkKey, level, random }));
      const spec = templateSpec(LEGACY_ITEMS[legacyId]);
      const weights = types => types.map((_, index) => index === 0 ? 3 : 1);
      if (spec.category === "armor") return encode(generateArmor({ type: spec.types[weightedIndex(weights(spec.types), random)], level, source: "shop", random, rarity: spec.rarity }));
      const held = spec.types.filter(type => WEAPONS[type].handed === spec.handed), types = held.length ? held : spec.types;
      const type = types[weightedIndex(weights(types), random)];
      // Shops never forge maker-only types such as Outlaw clubs, so those come as loot.
      return encode(generate({ type, level, source: WEAPONS[type].onlyMaker ? "loot" : "shop", random, rarity: spec.rarity, element: spec.element }));
    };
    let changed = fixedMakers;
    if (Array.isArray(inventory) && inventory.some(id => RETIRED_ITEMS.has(id))) { inventory.splice(0, inventory.length, ...inventory.filter(id => !RETIRED_ITEMS.has(id))); changed = true; }
    if (equipment) for (const slot of Object.keys(equipment)) if (RETIRED_ITEMS.has(equipment[slot])) { equipment[slot] = null; changed = true; }
    if (Array.isArray(favorites) && favorites.some(id => RETIRED_ITEMS.has(id))) { favorites.splice(0, favorites.length, ...favorites.filter(id => !RETIRED_ITEMS.has(id))); changed = true; }
    if (Array.isArray(inventory)) inventory.forEach((id, index) => {
      if (!LEGACY_ITEMS[id]) return;
      const list = made.get(id) || [], newId = forgeFor(id, list.length);
      list.push(newId); made.set(id, list); inventory[index] = newId; changed = true;
    });
    if (equipment) {
      const used = new Map();
      const take = (legacyId, reuse) => {
        const list = made.get(legacyId) || [], next = used.get(legacyId) || 0;
        if (reuse != null) return reuse;
        if (next < list.length) { used.set(legacyId, next + 1); return list[next]; }
        const newId = forgeFor(legacyId, list.length);
        list.push(newId); made.set(legacyId, list); used.set(legacyId, next + 1);
        if (Array.isArray(inventory)) inventory.push(newId);
        return newId;
      };
      const primaryOld = equipment.primaryHand;
      for (const slot of Object.keys(equipment)) {
        const id = equipment[slot];
        if (!LEGACY_ITEMS[id]) continue;
        // A two-handed piece fills both hands with the same forged weapon; a one-handed
        // masterwork that was held in both hands keeps only the main hand.
        const pairedOff = slot === "offHand" && id === primaryOld;
        if (pairedOff && masterworkTemplateKey(LEGACY_ITEMS[id])) { const held = item(equipment.primaryHand); equipment.offHand = held?.handed === 2 ? equipment.primaryHand : null; changed = true; continue; }
        const twoHandedPair = pairedOff && templateSpec(LEGACY_ITEMS[id]).handed === 2;
        equipment[slot] = take(id, twoHandedPair ? equipment.primaryHand : null);
        changed = true;
      }
    }
    if (Array.isArray(favorites)) {
      const kept = favorites.filter(id => !LEGACY_ITEMS[id]);
      const added = favorites.filter(id => LEGACY_ITEMS[id]).flatMap(id => made.get(id) || []);
      if (kept.length !== favorites.length) { favorites.splice(0, favorites.length, ...new Set([...kept, ...added])); changed = true; }
    }
    return changed;
  }

  /* ---------- Enemies ---------- */
  // Replaces catalogue weapons marked "forgedAs" with generated weapons around the enemy's level.
  // Replaces catalogue weapons and armor marked "forgedAs" with generated pieces around the enemy's level.
  function armCharacter(character, enemyLevel, lookup, random = Math.random, runeFillChance = 0.3) {
    const equipment = character?.equipment;
    if (!equipment) return character;
    const legacyOf = (id, category) => { const found = id ? lookup(id) : null; return found?.forgedAs?.length && found.category === category ? found : null; };
    const legacy = id => legacyOf(id, "weapon");
    const legacyArmor = id => legacyOf(id, "armor");
    const vampire = Number(character.classLevels?.vampire) > 0;
    const forge = (item, oneHanded = false) => {
      let types = item.forgedAs.filter(id => WEAPONS[id] && (!oneHanded || WEAPONS[id].handed === 1));
      if (!types.length) types = oneHanded ? ["longsword"] : item.forgedAs.filter(id => WEAPONS[id]);
      const weights = types.map((_, index) => index === 0 ? 3 : 1);
      const level = clampLevel(Math.floor(Number(enemyLevel) || 1) - 1 + Math.floor(random() * 5));
      return encode(generate({ type: types[weightedIndex(weights, random)], level, source: "loot", random, runeFillChance, allowVampiric: vampire, rarity: item.forgedRarity || null, element: item.forgedElement || null }));
    };
    const replaced = new Map(), added = [];
    const primaryId = equipment.primaryHand, offId = equipment.offHand;
    const primary = legacy(primaryId), off = legacy(offId);
    if (primary) {
      const sameBoth = offId === primaryId;
      const shieldInOff = offId && !sameBoth && !off;
      const newPrimary = forge(primary, shieldInOff);
      const newItem = item(newPrimary);
      equipment.primaryHand = newPrimary; added.push(newPrimary);
      if (sameBoth) {
        if (newItem.handed === 2) equipment.offHand = newPrimary;
        else if (Number(primary.handed) === 1) { const second = forge(primary, true); equipment.offHand = second; added.push(second); }
        else equipment.offHand = null;
      } else if (newItem.handed === 2 && offId) equipment.offHand = newPrimary;
      replaced.set(primaryId, newPrimary);
    }
    if (off && offId !== primaryId) {
      const newOff = forge(off, true);
      equipment.offHand = newOff; added.push(newOff);
      replaced.set(offId, newOff);
    }
    // Worn armor and shields.
    const forgeArmor = item => {
      const types = item.forgedAs.filter(id => ARMORS[id]);
      const level = clampLevel(Math.floor(Number(enemyLevel) || 1) - 1 + Math.floor(random() * 5));
      return encode(generateArmor({ type: types[weightedIndex(types.map((_, index) => index === 0 ? 3 : 1), random)], level, source: "loot", random, runeFillChance, allowVampiric: vampire, rarity: item.forgedRarity || null }));
    };
    for (const slot of ["armor", "offHand"]) {
      const oldId = equipment[slot], old = legacyArmor(oldId);
      if (!old || !old.forgedAs.some(id => ARMORS[id])) continue;
      const newId = forgeArmor(old);
      equipment[slot] = newId; added.push(newId); replaced.set(oldId, newId);
    }
    if (!added.length) return character;
    character.inventory = [...(character.inventory || []).filter(id => !legacy(id) && !legacyArmor(id)), ...new Set(added)];
    if (Array.isArray(character.noDropItems)) character.noDropItems = character.noDropItems.map(id => replaced.get(id) || id);
    return character;
  }

  /* ---------- Reforging (port task 3, the Shop Rework, 2026-11-06) ----------
     A reforge keeps a piece's type, level, main part, rune slots, runes and masterwork, and swaps its two
     other parts for any shop-made part of that shape (R2: never Outlaw, never Vampiric), never above the
     main part's rarity. Masterworks (Hearth Home only) may take Mythilium (R5); a staff's or wand's handle
     is capped by its jewel's or gem's tier, its inscription takes any ink (R6). Each changed part costs
     three times its rarity's part price; an inscription is priced at the handle's rarity (R3, R16).
     The anvils: "smith" (weapons, heavy and light armor, shields), "tailor" (clothing), "institute"
     (staffs and wands) and "hearth" (the smith's pieces plus masterworks of every kind). The user's
     answers: engine/Docs/Engine-Plan.md, "Decisions for task 3, Shop Rework". */
  const REFORGE_COST_TIMES = 3;
  const TIER_HANDLE_CAP = [null, 1, 3, 4]; // Lesser: uncommon, Regular: epic, Greater: legendary
  const REFORGE_ANVILS = ["smith", "tailor", "institute", "hearth"];
  const specRarityIndex = (spec, part) => {
    if (!part) return -1;
    if (WEAPONS[spec.type]?.caster) return part.shape === "inscription" ? -1 : RARITIES.indexOf(CASTER_HANDLES[part.material]?.rarity);
    return RARITIES.indexOf(isArmorSpec(spec) ? ARMOR_MATERIALS[part.material]?.rarity : MATERIALS[part.material]?.rarity);
  };
  // What a piece is for reforging: "weapon", "armor" (heavy, light, shield), "clothing", "caster", or null.
  function reforgeKind(spec) {
    if (!spec?.parts) return null;
    if (isArmorSpec(spec)) return ARMORS[spec.type].category === "clothing" ? "clothing" : "armor";
    if (!WEAPONS[spec.type]) return null;
    return WEAPONS[spec.type].caster ? "caster" : "weapon";
  }
  // Why this anvil can't take the piece ("" when it can).
  function reforgeReason(spec, anvil) {
    const kind = reforgeKind(spec);
    if (!kind) return "This piece can't be reforged.";
    if (!REFORGE_ANVILS.includes(anvil)) return "No anvil here.";
    if (spec.masterwork) return anvil === "hearth" ? "" : "Only Hearth Home reforges masterworks.";
    if (kind === "weapon" && WEAPONS[spec.type].onlyMaker) return "No smith makes parts for this weapon.";
    if (anvil === "tailor") return kind === "clothing" ? "" : "The Tailor reforges clothing only.";
    if (anvil === "institute") return kind === "caster" ? "" : "The Institute reforges staffs and wands only.";
    return ["weapon", "armor"].includes(kind) ? "" : kind === "clothing" ? "Clothing is reforged at a Tailor." : "Staffs and wands are reforged at the Institute of Magic.";
  }
  // The parts that may take the place of part 1 or 2 (the current part among them).
  function reforgeOptions(spec, index) {
    const kind = reforgeKind(spec), current = spec?.parts?.[index];
    if (!kind || index < 1 || index > 2 || !current) return [];
    const masterwork = Boolean(spec.masterwork);
    const unique = list => list.filter((option, at) => list.findIndex(other => other.maker === option.maker && other.material === option.material) === at);
    if (kind === "caster") {
      if (index === 2) return Object.keys(INKS).flatMap(ink => casterMakers("inscription", false).map(maker => ({ shape: "inscription", maker, material: ink })));
      const cap = TIER_HANDLE_CAP[spec.parts[0].tier] ?? 1;
      const handles = Object.values(CASTER_HANDLES).filter(handle => handle.natural ? RARITIES.indexOf(handle.rarity) <= cap : masterwork);
      return unique(handles.flatMap(handle => handle.makers.filter(maker => CASTER_MAKERS[maker]?.parts.includes("handle") && !CASTER_MAKERS[maker].restricted).map(maker => ({ shape: current.shape, maker, material: handle.id }))));
    }
    const mainIndex = specRarityIndex(spec, spec.parts[0]);
    if (kind === "weapon") {
      const options = [];
      for (let rarity = 0; rarity <= Math.min(4, mainIndex); rarity++) options.push(...partOptions(current.shape, rarity, { allowOutlaw: false }));
      if (masterwork) options.push(...masterworkMakers(current.shape).map(maker => ({ maker, material: "mythilium" })));
      return unique(options).map(option => ({ shape: current.shape, ...option }));
    }
    const category = ARMORS[spec.type].category, options = [];
    for (let rarity = 0; rarity <= Math.min(4, mainIndex); rarity++) options.push(...armorPartOptions(current.shape, category, rarity, { allowOutlaw: false, allowVampiric: false }));
    if (masterwork) options.push(...armorMasterworkMakers(category).map(maker => ({ maker, material: "mythilium" })));
    return unique(options).map(option => ({ shape: current.shape, ...option }));
  }
  const samePart = (a, b) => Boolean(a && b) && a.maker === b.maker && a.material === b.material;
  // Silver for changing the parts to these (unchanged parts are free).
  function reforgeCost(spec, parts) {
    let total = 0;
    const caster = WEAPONS[spec.type]?.caster;
    for (const index of [1, 2]) {
      const part = parts[index];
      if (!part || samePart(part, spec.parts[index])) continue;
      const rarity = caster && index === 2 ? specRarityIndex(spec, parts[1] || spec.parts[1]) : specRarityIndex(spec, part);
      total += REFORGE_COST_TIMES * RARITY_PRICE[RARITIES[Math.max(0, rarity)]];
    }
    return total;
  }
  // The reforged spec, or null when a part isn't allowed.
  function reforgedSpec(spec, parts) {
    if (!reforgeKind(spec) || !Array.isArray(parts)) return null;
    const next = spec.parts.map((part, index) => index === 0 || !part ? part : parts[index] || part);
    for (const index of [1, 2]) {
      if (!next[index] || samePart(next[index], spec.parts[index])) continue;
      if (!reforgeOptions(spec, index).some(option => samePart(option, next[index]))) return null;
      next[index] = { ...next[index], shape: spec.parts[index].shape };
    }
    return normalizeSpec({ ...spec, parts: next });
  }
  // Reforges the piece with this id at this anvil: { id, cost } or { reason }.
  function reforge(id, parts, anvil) {
    const spec = decode(id);
    if (!spec) return { reason: "This piece can't be reforged." };
    const reason = reforgeReason(spec, anvil);
    if (reason) return { reason };
    const next = reforgedSpec(spec, parts);
    if (!next) return { reason: "That part can't go on this piece." };
    const newId = encode(next);
    if (!decode(newId)) return { reason: "That part can't go on this piece." };
    return { id: newId, cost: reforgeCost(spec, next.parts), changed: newId !== encode(spec) };
  }

  const api = {
    RARITIES, CRIT, MAKERS, MAKER_IDS, CASTER_MAKERS, CASTER_HANDLES, STONES, INKS, TIERS, CASTER_IDS, REGULAR_IDS, tierOdds, casterEffects, spellEffectText, describePart, MATERIALS, SHAPES, WEAPONS, WEAPON_IDS, RUNES, RUNE_IDS, ELEMENT_PREFIX, ELEMENT_NAMES, STAT_SHORT, STAT_NAMES, ELEMENT_SHARE,
    levelCurve, rarityOdds, rollSlots, partOptions, generate, encode, decode, isForgedId, totals, stats, price, name, statPrefix, description, effectText, runeText,
    MATCHING_BONUS, matchingMaker, matchingBonusText, fixedRunes, allRunes,
    item, buildItem, MASTERWORKS, MASTERWORK_IDS, MASTERWORK_PREFIXES, MASTERWORK_MYTHILIUM_CHANCE, generateMasterwork, fixedMasterworkId, masterworkTemplateKey, masterworkTemplateId, NATURAL_WEAPONS, naturalWeapon, isTemplateId, templateSpec, LEGACY_ITEMS, migrateLegacyItems, runeConflict, withRune, withoutRune, runePrice, runeRemovalPrice, runeFitPrice, runeItemId, runeOfItem, RUNE_FIT_SHARE, RUNE_REMOVAL_MULTIPLIER, armCharacter,
    ARMORS, ARMOR_IDS, ARMOR_PARTS, ARMOR_MAKERS, ARMOR_MATERIALS, ARMOR_RUNE_RESIST, ARMOR_ELEMENT_PREFIX, ALL_ELEMENTS, generateArmor, armorTotals, armorEffectText, armorRuneText, isArmorSpec,
    RARITY_PRICE, REFORGE_COST_TIMES, TIER_HANDLE_CAP, REFORGE_ANVILS, reforgeKind, reforgeReason, reforgeOptions, reforgeCost, reforgedSpec, reforge,
  };
  root.ForgeRules = api;
  if (typeof module !== "undefined" && module.exports) module.exports = api;
})(globalThis);

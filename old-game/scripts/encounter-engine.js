/* Pure regional encounter tables shared by map travel and regression tests. */
(function (root) {
  const chances = {
    "Town": 0,
    "Road": 0,
    "Plains - near Town": 15,
    "Plains": 30,
    "River": 20,
    "Forest": 45,
    "Deep Forest": 60,
  };
  /* A character below a zone's range meets its easiest encounters; a character
     above it meets its hardest. This keeps the region geographically dangerous
     without letting ordinary encounters drift far away from the player. */
  const profiles = {
    "Town": { minimum: 1, maximum: 1, safe: true },
    "Road": { minimum: 1, maximum: 1, safe: true },
    "Plains - near Town": { minimum: 1, maximum: 3 },
    "Plains": { minimum: 2, maximum: 7 },
    "River": { minimum: 3, maximum: 8 },
    "Forest": { minimum: 5, maximum: 11 },
    "Deep Forest": { minimum: 10, maximum: 15 },
  };
  const dragonElements = {
    "Silver Dragon": "basic",
    "Red Dragon": "fire",
    "Blue Dragon": "water",
    "Golden Dragon": "holy",
    "Yellow Dragon": "lightning",
    "Crimson Dragon": "blood",
    "Black Dragon": "dark",
    "White Dragon": "ice",
    "Bronze Dragon": "earth",
    "Green Dragon": "wind",
    "Emerald Dragon": "nature",
    "Violet Dragon": "death",
  };
  const enemyLevels = {
    "Goblin": 1, "Lesser Imp": 1, "Juvenile Wolf": 2, "Orc": 3,
    "Juvenile Bear": 3, "Water Spirit": 3, "Goblin Archer": 3, "Goblin Warrior": 4,
    "Bandit": 4, "Bandit Archer": 4, "Adult Wolf": 5,
    "Juvenile Griffon": 6, "Goblin Boss": 7, "Corrupted Wolf": 7,
    "Adult Bear": 8, "Adult Griffon": 9, "Corrupted Bear": 10,
    "Corrupted Griffon": 13,
    "Highland Goblin": 10, "Highway Bandit": 10, "Highway Bandit Archer": 10,
    "Highland Wolf": 11, "Harpy Skirmisher": 12, "Goblin Raider": 13,
    "Highland Griffon": 13, "Greater Water Spirit": 13, "Hill Troll": 14,
    "Earth Spirit": 14, "Bandit Captain": 15, "Harpy Huntress": 15,
    "Mountain Bear": 15, "Dire Wolf": 16, "Goblin Warlord": 17,
    "Raging Water Spirit": 17, "Young Giant": 17, "Great Griffon": 18,
    "Greater Earth Spirit": 18, "Elder Troll": 19, "Harpy Matriarch": 19,
    "Mountain Wyrm": 20,
    "Fire Elemental": 25, "Water Elemental": 25, "Earth Elemental": 25,
    "Wind Elemental": 25, "Lightning Elemental": 25, "Ice Elemental": 25,
    "Coastal Corsair": 20, "Merfolk Spearfisher": 18, "Tide Siren": 24,
    "Merfolk Tidecaller": 27, "Shore Wyrm": 30,
    "Merfolk Reef Guard": 27, "Ash Harpy": 28, "Merfolk Steamcaller": 29,
    "Lava Stalker": 33, "Magma Elemental": 36, "Ember Drake": 40,
    "Rainbound Marauder": 21, "Storm Harpy": 29, "Thunder Troll": 35,
    "Reed Merfolk": 26, "Dune Raider": 36, "Sand Giant": 40,
    "Oasis Guardian": 38, "Sirocco Harpy": 42, "Sunscale Lamia": 45, "Sand Wraith": 47,
    "Tomb Guardian": 50, "Dust Revenant": 52, "Pyramid Sentinel": 55,
    "Caravan Bandit": 44, "Bloodsand Vampire": 47, "Vampire Stalker": 49,
    "Vampire Hexer": 52, "Crimson Vampire Knight": 55,
    "Bloodstone Reaver": 58, "Sanguine Oracle": 62, "Bloodrock Sentinel": 66, "Nocturne Castellan": 70,
    "Battlefield Scavenger": 65, "Gravebound Legionnaire": 68, "Rogue Vampire Hunter": 71, "Blood Exile": 73,
    "Duskpine Wolf": 60, "Boreal Direwolf": 63, "Duskpine Wraith": 69, "Nightwood Vampire": 72,
    "Satyr Scout": 7, "Forest Orc": 8, "Young Treant": 10,
    "Dryad Warden": 11, "Satyr Warrior": 12, "Orc Berserker": 14,
    "Nature Spirit": 15, "Satyr Grovekeeper": 16, "Dryad Matriarch": 18,
    "Orc Chieftain": 19, "Elder Treant": 20, "Greater Nature Spirit": 20,
    "Crag Troll": 45, "Harpy Sentinel": 46, "Stone Giant": 48,
    "Bog Troll": 36, "Mire Stalker": 39, "Fen Spirit": 43,
    "Canopy Stalker": 45, "Mossback Lamia": 48, "Jungle Treant": 56, "Canopy Wyrm": 65,
    "Storm Elemental": 50, "High Griffon": 52, "Mountain Lamia": 54,
    "Frost Elemental": 55, "Peak Giant": 56, "Cliff Wyrm": 57,
    "Glacier Giant": 85, "Ancient Frost Dragon": 88, "Stormforged Giant": 90,
    "Tempest Dragon": 92, "Crownfire Dragon": 95,
    ...Object.fromEntries(Object.keys(dragonElements).map(name => [name, 60])),
  };
  const animalFamilies = {
    "Juvenile Wolf": "wolf", "Adult Wolf": "wolf", "Corrupted Wolf": "wolf",
    "Juvenile Bear": "bear", "Adult Bear": "bear", "Corrupted Bear": "bear",
    "Juvenile Griffon": "griffon", "Adult Griffon": "griffon", "Corrupted Griffon": "griffon",
    "Highland Wolf": "wolf", "Dire Wolf": "wolf", "Highland Griffon": "griffon", "Great Griffon": "griffon",
    "Mountain Bear": "bear",
    "Duskpine Wolf": "wolf", "Boreal Direwolf": "wolf",
    "Shore Wyrm": "dragon", "Ember Drake": "dragon",
    "High Griffon": "griffon", "Cliff Wyrm": "dragon",
    "Canopy Wyrm": "dragon",
    "Ancient Frost Dragon": "dragon", "Tempest Dragon": "dragon", "Crownfire Dragon": "dragon",
    ...Object.fromEntries(Object.keys(dragonElements).map(name => [name, "dragon"])),
  };
  const unitRandom = random => {
    const value = Number(random());
    return Number.isFinite(value) ? Math.min(1 - Number.EPSILON, Math.max(0, value)) : 0;
  };
  const integer = (minimum, maximum, random) => minimum + Math.floor(unitRandom(random) * (maximum - minimum + 1));
  const copies = (name, minimum, maximum, random) => Array(integer(minimum, maximum, random)).fill(name);
  const entry = (level, weight, create) => ({ level, weight, create });

  /* Entry levels describe the intended difficulty of the whole group, including
     action-economy pressure; they are not merely the sum of enemy levels. */
  const tables = {
    "Plains - near Town": [
      entry(1, 10, () => ["Goblin"]),
      entry(2, 8, () => ["Goblin", "Goblin"]),
      entry(2, 6, () => ["Juvenile Wolf"]),
      entry(3, 8, () => ["Goblin", "Juvenile Wolf"]),
      entry(3, 4, () => ["Juvenile Wolf", "Juvenile Wolf"]),
      entry(3, 3, () => ["Goblin", "Goblin", "Goblin"]),
    ],
    "Plains": [
      entry(2, 8, () => ["Goblin", "Goblin"]),
      entry(2, 6, () => ["Juvenile Wolf"]),
      entry(3, 8, () => ["Orc"]),
      entry(3, 7, () => ["Juvenile Bear"]),
      entry(3, 5, () => ["Goblin", "Juvenile Wolf"]),
      entry(4, 8, () => ["Goblin Warrior"]),
      entry(4, 7, () => ["Goblin Archer"]),
      entry(4, 5, () => ["Bandit"]),
      entry(4, 5, () => ["Bandit Archer"]),
      entry(5, 8, () => ["Adult Wolf"]),
      entry(5, 5, () => ["Orc", "Goblin"]),
      entry(5, 4, () => ["Juvenile Bear", "Goblin"]),
      entry(6, 8, () => ["Juvenile Griffon"]),
      entry(6, 6, () => ["Orc", "Orc"]),
      entry(6, 4, () => ["Bandit", "Goblin"]),
      entry(7, 8, () => ["Goblin Warrior", "Goblin Archer"]),
      entry(7, 7, () => ["Adult Wolf", "Juvenile Wolf"]),
      entry(7, 3, () => ["Bandit", "Bandit"]),
    ],
    "River": [
      entry(3, 9, () => ["Water Spirit"]),
      entry(3, 7, () => ["Orc"]),
      entry(3, 6, () => ["Juvenile Bear"]),
      entry(4, 7, () => ["Goblin Warrior"]),
      entry(4, 6, () => ["Goblin Archer"]),
      entry(4, 5, () => ["Bandit Archer"]),
      entry(5, 8, () => ["Adult Wolf"]),
      entry(5, 6, () => ["Water Spirit", "Goblin"]),
      entry(5, 4, () => ["Bandit"]),
      entry(6, 8, () => ["Juvenile Griffon"]),
      entry(6, 6, () => ["Orc", "Orc"]),
      entry(6, 4, () => ["Juvenile Bear", "Goblin Archer"]),
      entry(7, 8, () => ["Goblin Warrior", "Goblin Archer"]),
      entry(7, 6, () => ["Adult Wolf", "Juvenile Wolf"]),
      entry(7, 4, () => ["Bandit", "Juvenile Wolf"]),
      entry(8, 8, () => ["Adult Bear"]),
      entry(8, 6, () => ["Bandit", "Bandit Archer"]),
      entry(8, 5, () => ["Juvenile Griffon", "Goblin", "Goblin"]),
    ],
    "Forest": [
      entry(5, 9, () => ["Adult Wolf"]),
      entry(5, 7, () => ["Orc", "Juvenile Wolf"]),
      entry(5, 5, () => ["Lesser Imp", "Lesser Imp", "Juvenile Wolf"]),
      entry(6, 8, () => ["Juvenile Griffon"]),
      entry(6, 7, () => ["Orc", "Orc"]),
      entry(6, 5, () => ["Juvenile Bear", "Goblin Archer"]),
      entry(6, 4, () => ["Goblin Warrior", "Goblin", "Goblin"]),
      entry(7, 8, () => ["Adult Wolf", "Juvenile Wolf"]),
      entry(7, 7, () => ["Goblin Warrior", "Goblin Archer"]),
      entry(7, 4, () => ["Goblin Boss"]),
      entry(7, 3, () => ["Corrupted Wolf"]),
      entry(8, 9, () => ["Adult Bear"]),
      entry(8, 7, () => ["Adult Wolf", "Orc"]),
      entry(8, 5, () => ["Juvenile Bear", "Juvenile Bear"]),
      entry(9, 9, () => ["Adult Griffon"]),
      entry(9, 7, () => ["Orc", "Orc", "Orc"]),
      entry(9, 5, () => ["Corrupted Wolf", "Lesser Imp"]),
      entry(9, 4, () => ["Goblin Boss", "Goblin"]),
      entry(10, 7, () => ["Adult Wolf", "Adult Wolf"]),
      entry(10, 6, () => ["Goblin Boss", "Goblin Archer"]),
      entry(10, 5, () => ["Adult Bear", "Lesser Imp", "Lesser Imp"]),
      entry(10, 3, () => ["Corrupted Bear"]),
      entry(11, 8, () => ["Adult Griffon", "Lesser Imp", "Lesser Imp"]),
      entry(11, 7, () => ["Goblin Boss", "Goblin Warrior"]),
      entry(11, 5, () => ["Corrupted Wolf", "Orc"]),
      entry(11, 3, () => ["Juvenile Griffon", "Juvenile Griffon"]),
    ],
    "Deep Forest": [
      entry(10, 8, () => ["Adult Bear", "Lesser Imp", "Lesser Imp"]),
      entry(10, 7, () => ["Goblin Boss", "Goblin Archer"]),
      entry(10, 4, () => ["Corrupted Bear"]),
      entry(11, 8, () => ["Adult Griffon", "Lesser Imp", "Lesser Imp"]),
      entry(11, 7, () => ["Corrupted Bear", "Lesser Imp"]),
      entry(11, 5, () => ["Corrupted Wolf", "Orc"]),
      entry(12, 8, () => ["Corrupted Wolf", "Adult Wolf"]),
      entry(12, 7, () => ["Goblin Boss", "Goblin Warrior", "Goblin"]),
      entry(12, 5, random => copies("Orc", 3, 4, random)),
      entry(12, 4, random => ["Corrupted Bear", ...copies("Lesser Imp", 1, 2, random)]),
      entry(13, 9, () => ["Corrupted Griffon"]),
      entry(13, 7, () => ["Corrupted Bear", "Orc"]),
      entry(13, 5, () => ["Goblin Boss", "Goblin Warrior", "Goblin Archer"]),
      entry(14, 8, () => ["Corrupted Griffon", "Lesser Imp"]),
      entry(14, 7, () => ["Corrupted Bear", "Goblin Warrior"]),
      entry(14, 6, () => ["Adult Griffon", "Goblin Warrior", "Lesser Imp"]),
      entry(14, 4, () => ["Orc", "Orc", "Orc", "Orc"]),
      entry(15, 9, () => ["Corrupted Griffon", "Goblin", "Goblin"]),
      entry(15, 8, () => ["Corrupted Bear", "Juvenile Bear", "Lesser Imp", "Lesser Imp"]),
      entry(15, 5, () => ["Adult Wolf", "Adult Wolf", "Juvenile Wolf", "Juvenile Wolf"]),
      entry(15, 4, () => ["Goblin Boss", "Goblin Warrior", "Goblin Warrior"]),
    ],
  };

  const stoneveilChances = {
    "Town": 0, "Road": 0,
    "Plains - near Town": 12, "Hill-Plains - near Town": 15, "River - near Town": 12, "Mountainfoot - near Town": 18,
    "Plains": 26, "Hill-Plains": 34, "River": 23, "Mountainfoot": 42,
  };
  const stoneveilProfiles = {
    "Town": { minimum: 10, maximum: 10, safe: true },
    "Road": { minimum: 10, maximum: 10, safe: true },
    "Plains - near Town": { minimum: 10, maximum: 12 },
    "Hill-Plains - near Town": { minimum: 10, maximum: 13 },
    "River - near Town": { minimum: 10, maximum: 13 },
    "Mountainfoot - near Town": { minimum: 12, maximum: 15 },
    "Plains": { minimum: 11, maximum: 15 },
    "Hill-Plains": { minimum: 13, maximum: 17 },
    "River": { minimum: 12, maximum: 17 },
    "Mountainfoot": { minimum: 15, maximum: 20 },
  };
  const stoneveilTables = {
    "Plains - near Town": [
      entry(10, 10, () => ["Highland Goblin"]), entry(10, 7, () => ["Highway Bandit"]),
      entry(11, 8, () => ["Highland Wolf"]), entry(12, 5, () => ["Highland Goblin", "Highland Goblin"]),
    ],
    "Hill-Plains - near Town": [
      entry(11, 9, () => ["Highland Wolf"]), entry(12, 8, () => ["Harpy Skirmisher"]),
      entry(13, 6, () => ["Highland Griffon"]), entry(13, 4, () => ["Harpy Skirmisher", "Harpy Skirmisher"]),
    ],
    "River - near Town": [
      entry(10, 9, () => ["Highland Goblin"]), entry(11, 7, () => ["Highland Wolf"]),
      entry(12, 7, () => ["Harpy Skirmisher"]), entry(13, 6, () => ["Greater Water Spirit"]),
    ],
    "Mountainfoot - near Town": [
      entry(12, 9, () => ["Harpy Skirmisher"]), entry(13, 8, () => ["Highland Griffon"]),
      entry(14, 7, () => ["Hill Troll"]), entry(15, 6, () => ["Mountain Bear"]),
    ],
    "Plains": [
      entry(11, 9, () => ["Highland Wolf"]), entry(11, 8, () => ["Highland Goblin", "Highland Goblin"]),
      entry(12, 7, () => ["Highway Bandit", "Highway Bandit Archer"]), entry(13, 9, () => ["Goblin Raider"]),
      entry(13, 7, () => ["Highland Griffon"]), entry(14, 6, () => ["Goblin Raider", "Highland Goblin"]),
      entry(15, 7, () => ["Bandit Captain", "Highway Bandit Archer"]), entry(15, 5, () => ["Highland Griffon", "Highland Griffon"]),
    ],
    "Hill-Plains": [
      entry(13, 9, () => ["Highland Griffon"]), entry(13, 8, () => ["Harpy Skirmisher", "Harpy Skirmisher"]),
      entry(14, 8, () => ["Hill Troll"]), entry(14, 6, () => ["Highland Wolf", "Highland Wolf"]),
      entry(15, 8, () => ["Harpy Huntress"]), entry(16, 8, () => ["Dire Wolf"]),
      entry(16, 6, () => ["Hill Troll", "Harpy Skirmisher"]), entry(17, 7, () => ["Great Griffon"]),
    ],
    "River": [
      entry(12, 9, () => ["Highland Goblin", "Highland Goblin"]), entry(12, 7, () => ["Harpy Skirmisher"]),
      entry(13, 9, () => ["Greater Water Spirit"]), entry(13, 7, () => ["Highland Griffon"]),
      entry(14, 7, () => ["Goblin Raider", "Highland Goblin"]), entry(15, 8, () => ["Harpy Huntress"]),
      entry(16, 7, () => ["Dire Wolf"]), entry(17, 8, () => ["Raging Water Spirit"]),
      entry(17, 5, () => ["Great Griffon"]),
    ],
    "Mountainfoot": [
      entry(15, 9, () => ["Mountain Bear"]), entry(15, 8, () => ["Harpy Huntress"]),
      entry(16, 8, () => ["Hill Troll", "Hill Troll"]), entry(16, 7, () => ["Highland Griffon", "Highland Griffon"]),
      entry(17, 8, () => ["Young Giant"]), entry(17, 7, () => ["Dire Wolf", "Dire Wolf"]),
      entry(18, 8, () => ["Greater Earth Spirit"]), entry(18, 7, () => ["Great Griffon"]),
      entry(19, 8, () => ["Elder Troll"]), entry(19, 7, () => ["Harpy Matriarch", "Harpy Huntress"]),
      entry(20, 6, () => ["Mountain Wyrm"]), entry(20, 5, () => ["Young Giant", "Hill Troll"]),
    ],
  };

  const verdantChances = {
    "Town": 0, "Road": 0, "Plains": 26,
    "Outer Forest": 34, "Forest": 45, "Deep Forest": 58,
  };
  const verdantProfiles = {
    "Town": { minimum: 5, maximum: 5, safe: true },
    "Road": { minimum: 5, maximum: 5, safe: true },
    "Plains": { minimum: 5, maximum: 12 },
    "Outer Forest": { minimum: 7, maximum: 14 },
    "Forest": { minimum: 10, maximum: 17 },
    "Deep Forest": { minimum: 15, maximum: 20 },
  };
  const verdantTables = {
    "Plains": [
      entry(5, 9, () => ["Adult Wolf"]),
      entry(6, 9, () => ["Juvenile Griffon"]),
      entry(7, 9, () => ["Goblin Boss"]),
      entry(7, 6, () => ["Goblin Warrior", "Goblin Archer"]),
      entry(8, 9, () => ["Forest Orc"]),
      entry(9, 9, () => ["Adult Griffon"]),
      entry(10, 9, () => ["Highland Goblin"]),
      entry(10, 8, () => ["Highway Bandit"]),
      entry(11, 9, () => ["Highland Wolf"]),
      entry(11, 7, () => ["Highway Bandit Archer"]),
      entry(12, 9, () => ["Highland Goblin", "Highland Goblin"]),
      entry(12, 8, () => ["Highway Bandit", "Highway Bandit Archer"]),
    ],
    "Outer Forest": [
      entry(7, 9, () => ["Satyr Scout"]),
      entry(7, 8, () => ["Goblin Boss"]),
      entry(8, 9, () => ["Forest Orc"]),
      entry(8, 8, () => ["Adult Bear"]),
      entry(9, 8, () => ["Satyr Scout", "Goblin"]),
      entry(10, 9, () => ["Highland Goblin"]),
      entry(10, 7, () => ["Adult Bear"]),
      entry(11, 9, () => ["Highland Wolf"]),
      entry(11, 7, () => ["Forest Orc", "Goblin"]),
      entry(12, 9, () => ["Satyr Warrior"]),
      entry(12, 7, () => ["Highland Goblin", "Highland Goblin"]),
      entry(13, 9, () => ["Goblin Raider"]),
      entry(13, 7, () => ["Satyr Scout", "Satyr Scout"]),
      entry(14, 9, () => ["Orc Berserker"]),
      entry(14, 7, () => ["Forest Orc", "Forest Orc"]),
    ],
    "Forest": [
      entry(10, 9, () => ["Young Treant"]),
      entry(10, 7, () => ["Highland Goblin"]),
      entry(11, 9, () => ["Dryad Warden"]),
      entry(11, 7, () => ["Highland Wolf"]),
      entry(12, 9, () => ["Satyr Warrior"]),
      entry(12, 7, () => ["Forest Orc", "Goblin Warrior"]),
      entry(13, 9, () => ["Goblin Raider"]),
      entry(13, 7, () => ["Young Treant", "Goblin"]),
      entry(14, 9, () => ["Orc Berserker"]),
      entry(14, 7, () => ["Dryad Warden", "Satyr Scout"]),
      entry(15, 9, () => ["Mountain Bear"]),
      entry(15, 7, () => ["Young Treant", "Dryad Warden"]),
      entry(16, 9, () => ["Dire Wolf"]),
      entry(16, 8, () => ["Satyr Grovekeeper"]),
      entry(17, 9, () => ["Goblin Warlord"]),
      entry(17, 7, () => ["Orc Berserker", "Forest Orc"]),
    ],
    "Deep Forest": [
      entry(15, 9, () => ["Nature Spirit"]),
      entry(15, 8, () => ["Mountain Bear"]),
      entry(16, 9, () => ["Dire Wolf"]),
      entry(16, 8, () => ["Satyr Grovekeeper"]),
      entry(17, 9, () => ["Goblin Warlord"]),
      entry(17, 7, () => ["Young Treant", "Dryad Warden"]),
      entry(18, 9, () => ["Dryad Matriarch"]),
      entry(18, 7, () => ["Orc Berserker", "Forest Orc"]),
      entry(19, 9, () => ["Orc Chieftain"]),
      entry(19, 7, () => ["Satyr Grovekeeper", "Satyr Scout"]),
      entry(20, 9, () => ["Elder Treant"]),
      entry(20, 9, () => ["Greater Nature Spirit"]),
    ],
  };
  const highStoneveilChances = {
    "Town": 0, "Road": 0, "Lower Slopes": 25,
    "Cliffside": 32, "High Pass": 38, "Frozen Heights": 44, "Dragonspine": 50,
  };
  const highStoneveilProfiles = {
    "Town": { minimum: 45, maximum: 45, safe: true },
    "Road": { minimum: 45, maximum: 45, safe: true },
    "Lower Slopes": { minimum: 45, maximum: 48 },
    "Cliffside": { minimum: 47, maximum: 52 },
    "High Pass": { minimum: 50, maximum: 55 },
    "Frozen Heights": { minimum: 54, maximum: 58 },
    "Dragonspine": { minimum: 57, maximum: 60 },
  };
  const highStoneveilTables = {
    "Lower Slopes": [
      entry(45, 9, () => ["Crag Troll"]),
      entry(46, 8, () => ["Harpy Sentinel"]),
      entry(47, 8, () => ["Crag Troll", "Harpy Sentinel"]),
      entry(48, 7, () => ["Stone Giant"]),
    ],
    "Cliffside": [
      entry(47, 9, () => ["Harpy Sentinel", "Harpy Sentinel"]),
      entry(48, 8, () => ["Stone Giant"]),
      entry(49, 7, () => ["Crag Troll", "Crag Troll"]),
      entry(50, 8, () => ["Storm Elemental"]),
      entry(51, 7, () => ["Stone Giant", "Harpy Sentinel"]),
      entry(52, 8, () => ["High Griffon"]),
    ],
    "High Pass": [
      entry(50, 9, () => ["Storm Elemental"]),
      entry(51, 7, () => ["Stone Giant", "Crag Troll"]),
      entry(52, 9, () => ["High Griffon"]),
      entry(53, 7, () => ["Harpy Sentinel", "Storm Elemental"]),
      entry(54, 9, () => ["Mountain Lamia"]),
      entry(55, 8, () => ["Frost Elemental"]),
    ],
    "Frozen Heights": [
      entry(54, 9, () => ["Mountain Lamia"]),
      entry(55, 9, () => ["Frost Elemental"]),
      entry(56, 8, () => ["Peak Giant"]),
      entry(57, 8, () => ["Cliff Wyrm"]),
      entry(58, 7, () => ["Peak Giant", "Frost Elemental"]),
    ],
    "Dragonspine": [
      entry(57, 8, () => ["Cliff Wyrm"]),
      entry(58, 7, () => ["Cliff Wyrm", "Cliff Wyrm"]),
      entry(59, 8, () => ["Mountain Lamia", "Cliff Wyrm"]),
      ...Object.keys(dragonElements).map(name => entry(60, 1, () => [name])),
    ],
  };
  const stoneveilSummitChances = {
    "Road": 0, "Windcut Ascent": 32, "Giant Steps": 40,
    "Cloudbreak Ridge": 44, "Dragon Eyries": 48, "Frozen Crown": 50,
  };
  const stoneveilSummitProfiles = {
    "Road": { minimum: 85, maximum: 85, safe: true },
    "Windcut Ascent": { minimum: 85, maximum: 88 },
    "Giant Steps": { minimum: 86, maximum: 90 },
    "Cloudbreak Ridge": { minimum: 88, maximum: 92 },
    "Dragon Eyries": { minimum: 90, maximum: 95 },
    "Frozen Crown": { minimum: 92, maximum: 95 },
  };
  const stoneveilSummitTables = {
    "Windcut Ascent": [
      entry(85, 9, () => ["Glacier Giant"]),
      entry(87, 7, () => ["Glacier Giant", "Glacier Giant"]),
      entry(88, 5, () => ["Ancient Frost Dragon"]),
    ],
    "Giant Steps": [
      entry(85, 8, () => ["Glacier Giant"]),
      entry(88, 5, () => ["Ancient Frost Dragon"]),
      entry(89, 7, () => ["Glacier Giant", "Glacier Giant"]),
      entry(90, 8, () => ["Stormforged Giant"]),
    ],
    "Cloudbreak Ridge": [
      entry(88, 8, () => ["Ancient Frost Dragon"]),
      entry(90, 8, () => ["Stormforged Giant"]),
      entry(92, 7, () => ["Tempest Dragon"]),
    ],
    "Dragon Eyries": [
      entry(90, 6, () => ["Stormforged Giant"]),
      entry(92, 9, () => ["Tempest Dragon"]),
      entry(93, 5, () => ["Ancient Frost Dragon", "Ancient Frost Dragon"]),
      entry(95, 7, () => ["Crownfire Dragon"]),
    ],
    "Frozen Crown": [
      entry(92, 8, () => ["Tempest Dragon"]),
      entry(93, 6, () => ["Stormforged Giant", "Stormforged Giant"]),
      entry(95, 9, () => ["Crownfire Dragon"]),
    ],
  };
  const sunwardChances = {
    "Town": 0, "Road": 0, "Open Sea": 0,
    "Coastal Plains": 22, "Windswept Plains": 29, "Salt Marsh": 35, "Dunes": 36, "Beach": 40,
  };
  const sunwardProfiles = {
    "Town": { minimum: 15, maximum: 15, safe: true }, "Road": { minimum: 15, maximum: 15, safe: true },
    "Open Sea": { minimum: 15, maximum: 15, safe: true },
    "Coastal Plains": { minimum: 15, maximum: 20 }, "Windswept Plains": { minimum: 18, maximum: 24 },
    "Salt Marsh": { minimum: 22, maximum: 27 }, "Dunes": { minimum: 24, maximum: 28 }, "Beach": { minimum: 26, maximum: 30 },
  };
  const sunwardTables = {
    "Coastal Plains": [
      entry(15, 9, () => ["Bandit Captain"]), entry(15, 7, () => ["Mountain Bear"]),
      entry(16, 8, () => ["Dire Wolf"]), entry(17, 8, () => ["Raging Water Spirit"]),
      entry(18, 7, () => ["Great Griffon"]), entry(18, 8, () => ["Merfolk Spearfisher"]),
      entry(19, 7, () => ["Elder Troll"]),
      entry(20, 8, () => ["Coastal Corsair"]), entry(20, 5, () => ["Merfolk Spearfisher", "Merfolk Spearfisher"]),
    ],
    "Windswept Plains": [
      entry(18, 9, () => ["Great Griffon"]), entry(19, 8, () => ["Elder Troll"]),
      entry(20, 9, () => ["Coastal Corsair"]), entry(20, 6, () => ["Merfolk Spearfisher"]),
      entry(21, 6, () => ["Bandit Captain", "Highway Bandit Archer"]),
      entry(22, 7, () => ["Coastal Corsair", "Highway Bandit"]), entry(23, 7, () => ["Mountain Wyrm"]),
      entry(24, 7, () => ["Tide Siren"]),
    ],
    "Salt Marsh": [
      entry(22, 9, () => ["Raging Water Spirit", "Greater Water Spirit"]),
      entry(22, 6, () => ["Merfolk Spearfisher", "Merfolk Spearfisher"]),
      entry(23, 8, () => ["Coastal Corsair", "Greater Water Spirit"]),
      entry(24, 9, () => ["Tide Siren"]), entry(25, 8, () => ["Water Elemental"]),
      entry(26, 7, () => ["Tide Siren", "Greater Water Spirit"]),
      entry(27, 6, () => ["Water Elemental", "Raging Water Spirit"]),
      entry(27, 8, () => ["Merfolk Tidecaller"]),
    ],
    "Dunes": [
      entry(24, 9, () => ["Tide Siren"]), entry(25, 8, () => ["Wind Elemental"]),
      entry(26, 8, () => ["Coastal Corsair", "Highway Bandit Archer"]),
      entry(27, 7, () => ["Tide Siren", "Coastal Corsair"]),
      entry(27, 7, () => ["Merfolk Tidecaller"]),
      entry(28, 7, () => ["Wind Elemental", "Harpy Huntress"]),
    ],
    "Beach": [
      entry(26, 8, () => ["Water Elemental"]), entry(27, 8, () => ["Tide Siren", "Greater Water Spirit"]),
      entry(28, 7, () => ["Water Elemental", "Coastal Corsair"]),
      entry(28, 7, () => ["Merfolk Tidecaller"]),
      entry(29, 6, () => ["Tide Siren", "Tide Siren"]),
      entry(29, 6, () => ["Merfolk Tidecaller", "Merfolk Spearfisher"]),
      entry(30, 8, () => ["Shore Wyrm"]),
    ],
  };
  const cinderChances = {
    "Town": 0, "Road": 0, "Open Sea": 0, "Hearth Home": 0,
    "Black Sand Shore": 27, "Ember Scrub": 33, "Ashen Slope": 40, "Lava Fields": 46, "Caldera": 54,
  };
  const cinderProfiles = {
    "Town": { minimum: 25, maximum: 25, safe: true }, "Road": { minimum: 25, maximum: 25, safe: true },
    "Open Sea": { minimum: 25, maximum: 25, safe: true }, "Hearth Home": { minimum: 48, maximum: 48, safe: true },
    "Black Sand Shore": { minimum: 25, maximum: 29 }, "Ember Scrub": { minimum: 28, maximum: 33 },
    "Ashen Slope": { minimum: 32, maximum: 36 }, "Lava Fields": { minimum: 36, maximum: 39 },
    "Caldera": { minimum: 39, maximum: 40 },
  };
  const cinderTables = {
    "Black Sand Shore": [
      entry(25, 9, () => ["Fire Elemental"]), entry(26, 8, () => ["Earth Elemental"]),
      entry(27, 8, () => ["Tide Siren", "Raging Water Spirit"]),
      entry(27, 8, () => ["Merfolk Reef Guard"]),
      entry(28, 8, () => ["Ash Harpy"]),
      entry(28, 6, () => ["Merfolk Reef Guard"]),
      entry(29, 6, () => ["Fire Elemental", "Ash Harpy"]),
      entry(29, 9, () => ["Merfolk Steamcaller"]),
    ],
    "Ember Scrub": [
      entry(28, 9, () => ["Ash Harpy"]), entry(29, 8, () => ["Fire Elemental", "Harpy Huntress"]),
      entry(30, 8, () => ["Earth Elemental", "Fire Elemental"]),
      entry(30, 6, () => ["Merfolk Steamcaller"]),
      entry(31, 7, () => ["Ash Harpy", "Ash Harpy"]),
      entry(32, 7, () => ["Lava Stalker"]), entry(33, 8, () => ["Lava Stalker"]),
    ],
    "Ashen Slope": [
      entry(32, 8, () => ["Lava Stalker"]), entry(33, 9, () => ["Lava Stalker"]),
      entry(34, 7, () => ["Ash Harpy", "Lava Stalker"]),
      entry(35, 7, () => ["Fire Elemental", "Earth Elemental"]),
      entry(36, 9, () => ["Magma Elemental"]),
    ],
    "Lava Fields": [
      entry(36, 9, () => ["Magma Elemental"]),
      entry(37, 7, () => ["Lava Stalker", "Fire Elemental"]),
      entry(38, 8, () => ["Lava Stalker", "Lava Stalker"]),
      entry(39, 7, () => ["Magma Elemental", "Ash Harpy"]),
    ],
    "Caldera": [
      entry(39, 8, () => ["Magma Elemental", "Lava Stalker"]),
      entry(40, 9, () => ["Ember Drake"]),
      entry(40, 6, () => ["Magma Elemental", "Magma Elemental"]),
    ],
  };
  const stormwakeChances = { "Town": 0, "Road": 0, "Church of Talos": 0, "Rainwashed Downs": 26, "Floodplain": 30, "Thunder Hills": 37, "Stormcrest": 45 };
  const stormwakeProfiles = {
    "Town": { minimum: 15, maximum: 15, safe: true }, "Road": { minimum: 15, maximum: 15, safe: true },
    "Rainwashed Downs": { minimum: 15, maximum: 20 }, "Floodplain": { minimum: 19, maximum: 24 },
    "Thunder Hills": { minimum: 23, maximum: 29 }, "Stormcrest": { minimum: 29, maximum: 35 },
    "Church of Talos": { minimum: 44, maximum: 44, safe: true },
  };
  const stormwakeTables = {
    "Rainwashed Downs": [
      entry(15, 8, () => ["Harpy Huntress"]), entry(16, 8, () => ["Dire Wolf"]),
      entry(17, 8, () => ["Raging Water Spirit"]), entry(18, 8, () => ["Great Griffon"]),
      entry(19, 8, () => ["Elder Troll"]), entry(20, 8, () => ["Rainbound Marauder"]),
    ],
    "Floodplain": [
      entry(19, 8, () => ["Raging Water Spirit", "Greater Water Spirit"]),
      entry(20, 8, () => ["Rainbound Marauder"]), entry(21, 8, () => ["Rainbound Marauder"]),
      entry(22, 8, () => ["Rainbound Marauder", "Greater Water Spirit"]),
      entry(23, 8, () => ["Tide Siren"]), entry(24, 8, () => ["Water Elemental"]),
    ],
    "Thunder Hills": [
      entry(23, 8, () => ["Rainbound Marauder", "Harpy Huntress"]),
      entry(24, 8, () => ["Tide Siren"]), entry(25, 8, () => ["Lightning Elemental"]),
      entry(26, 8, () => ["Wind Elemental"]),
      entry(27, 8, () => ["Lightning Elemental", "Raging Water Spirit"]),
      entry(28, 8, () => ["Storm Harpy"]), entry(29, 8, () => ["Storm Harpy"]),
    ],
    "Stormcrest": [
      entry(29, 8, () => ["Storm Harpy"]), entry(30, 8, () => ["Storm Harpy"]),
      entry(31, 7, () => ["Storm Harpy", "Raging Water Spirit"]),
      entry(32, 7, () => ["Storm Harpy", "Rainbound Marauder"]),
      entry(33, 8, () => ["Thunder Troll"]), entry(34, 8, () => ["Thunder Troll"]),
      entry(35, 8, () => ["Thunder Troll"]),
    ],
  };
  const gloamfenChances = { "Town": 0, "Road": 0, "Open Mire": 29, "Reedwater": 35, "Deep Mire": 45, "Rocky Rise": 38 };
  const gloamfenProfiles = {
    "Town": { minimum: 35, maximum: 35, safe: true }, "Road": { minimum: 35, maximum: 35, safe: true },
    "Open Mire": { minimum: 35, maximum: 39 }, "Reedwater": { minimum: 38, maximum: 43 },
    "Deep Mire": { minimum: 42, maximum: 48 }, "Rocky Rise": { minimum: 45, maximum: 50 },
  };
  const gloamfenTables = {
    "Open Mire": [
      entry(35, 9, () => ["Bog Troll"]), entry(37, 7, () => ["Bog Troll", "Bog Troll"]),
      entry(39, 8, () => ["Mire Stalker"]),
    ],
    "Reedwater": [
      entry(39, 9, () => ["Mire Stalker"]), entry(41, 7, () => ["Mire Stalker", "Mire Stalker"]),
      entry(43, 8, () => ["Fen Spirit"]),
    ],
    "Deep Mire": [
      entry(43, 9, () => ["Fen Spirit"]), entry(45, 7, () => ["Fen Spirit", "Mire Stalker"]),
      entry(48, 8, () => ["Mossback Lamia"]),
    ],
    "Rocky Rise": [
      entry(45, 9, () => ["Crag Troll"]), entry(48, 8, () => ["Stone Giant"]),
      entry(50, 7, () => ["Mossback Lamia", "Crag Troll"]),
    ],
  };
  const verdigrisChances = { "Town": 0, "Road": 0, "Jungle Verge": 30, "Outer Jungle": 36, "Dense Jungle": 45, "High Canopy": 49, "Old Growth": 55 };
  const verdigrisProfiles = {
    "Town": { minimum: 45, maximum: 45, safe: true }, "Road": { minimum: 45, maximum: 45, safe: true },
    "Jungle Verge": { minimum: 45, maximum: 50 }, "Outer Jungle": { minimum: 48, maximum: 55 },
    "Dense Jungle": { minimum: 52, maximum: 60 }, "High Canopy": { minimum: 55, maximum: 65 },
    "Old Growth": { minimum: 58, maximum: 65 },
  };
  const verdigrisTables = {
    "Jungle Verge": [
      entry(45, 8, () => ["Canopy Stalker"]), entry(47, 8, () => ["Canopy Stalker", "Canopy Stalker"]),
      entry(49, 7, () => ["Mossback Lamia"]), entry(50, 7, () => ["Canopy Stalker", "Mossback Lamia"]),
    ],
    "Outer Jungle": [
      entry(48, 8, () => ["Mossback Lamia"]), entry(50, 8, () => ["Canopy Stalker", "Canopy Stalker"]),
      entry(52, 7, () => ["Mossback Lamia", "Canopy Stalker"]), entry(54, 7, () => ["Jungle Treant"]),
      entry(55, 7, () => ["Jungle Treant"]),
    ],
    "Dense Jungle": [
      entry(52, 8, () => ["Mossback Lamia"]), entry(54, 8, () => ["Canopy Stalker", "Canopy Stalker"]),
      entry(56, 9, () => ["Jungle Treant"]), entry(58, 7, () => ["Jungle Treant", "Canopy Stalker"]),
      entry(60, 6, () => ["Emerald Dragon"]),
    ],
    "High Canopy": [
      entry(56, 8, () => ["Jungle Treant"]), entry(58, 8, () => ["Canopy Stalker", "Mossback Lamia"]),
      entry(60, 7, () => ["Emerald Dragon"]), entry(62, 7, () => ["Jungle Treant", "Jungle Treant"]),
      entry(64, 8, () => ["Canopy Wyrm"]), entry(65, 7, () => ["Canopy Wyrm"]),
    ],
    "Old Growth": [
      entry(58, 9, () => ["Jungle Treant"]), entry(60, 7, () => ["Emerald Dragon"]),
      entry(62, 8, () => ["Jungle Treant", "Jungle Treant"]),
      entry(64, 7, () => ["Canopy Wyrm"]), entry(65, 8, () => ["Canopy Wyrm"]),
    ],
  };
  const mirrormereChances = { "Town": 0, "Road": 0, "Open Water": 0, "Institute of Magic": 0, "Lake Shore": 25, "Reed Marsh": 33, "Windcut Bluffs": 36, "Dry Steppe": 39, "Desert Fringe": 45 };
  const mirrormereProfiles = {
    "Town": { minimum: 20, maximum: 20, safe: true }, "Road": { minimum: 20, maximum: 20, safe: true }, "Open Water": { minimum: 20, maximum: 20, safe: true },
    "Institute of Magic": { minimum: 20, maximum: 20, safe: true },
    "Lake Shore": { minimum: 20, maximum: 26 }, "Reed Marsh": { minimum: 23, maximum: 29 },
    "Windcut Bluffs": { minimum: 27, maximum: 33 }, "Dry Steppe": { minimum: 31, maximum: 37 },
    "Desert Fringe": { minimum: 35, maximum: 40 },
  };
  const mirrormereTables = {
    "Lake Shore": [
      entry(20, 8, () => ["Coastal Corsair"]), entry(21, 8, () => ["Rainbound Marauder"]),
      entry(22, 8, () => ["Rainbound Marauder", "Greater Water Spirit"]),
      entry(23, 8, () => ["Tide Siren"]), entry(24, 8, () => ["Tide Siren"]),
      entry(25, 8, () => ["Water Elemental"]), entry(26, 8, () => ["Reed Merfolk"]),
    ],
    "Reed Marsh": [
      entry(23, 8, () => ["Tide Siren"]), entry(24, 8, () => ["Tide Siren"]),
      entry(25, 8, () => ["Water Elemental"]), entry(26, 8, () => ["Reed Merfolk"]),
      entry(27, 8, () => ["Merfolk Reef Guard"]),
      entry(28, 8, () => ["Merfolk Tidecaller"]), entry(29, 8, () => ["Merfolk Steamcaller"]),
    ],
    "Windcut Bluffs": [
      entry(27, 8, () => ["Earth Elemental"]), entry(28, 8, () => ["Wind Elemental"]),
      entry(29, 8, () => ["Storm Harpy"]), entry(30, 8, () => ["Shore Wyrm"]),
      entry(31, 7, () => ["Storm Harpy", "Rainbound Marauder"]),
      entry(32, 7, () => ["Shore Wyrm"]), entry(33, 8, () => ["Thunder Troll"]),
    ],
    "Dry Steppe": [
      entry(31, 7, () => ["Storm Harpy"]), entry(32, 7, () => ["Shore Wyrm"]),
      entry(33, 7, () => ["Rainbound Marauder", "Coastal Corsair"]),
      entry(34, 8, () => ["Dune Raider"]), entry(35, 8, () => ["Dune Raider"]),
      entry(36, 8, () => ["Dune Raider"]), entry(37, 7, () => ["Dune Raider", "Rainbound Marauder"]),
    ],
    "Desert Fringe": [
      entry(35, 8, () => ["Dune Raider"]), entry(36, 8, () => ["Dune Raider"]),
      entry(37, 8, () => ["Dune Raider", "Rainbound Marauder"]),
      entry(38, 8, () => ["Dune Raider", "Dune Raider"]),
      entry(39, 8, () => ["Sand Giant"]), entry(40, 8, () => ["Sand Giant"]),
    ],
  };
  const saffronChances = { "Town": 0, "Road": 0, "Oasis Grove": 26, "Sunburnt Flats": 30, "Salt Pans": 34, "Shifting Dunes": 38, "Glass Dunes": 44 };
  const saffronProfiles = {
    "Town": { minimum: 35, maximum: 35, safe: true }, "Road": { minimum: 35, maximum: 35, safe: true },
    "Oasis Grove": { minimum: 35, maximum: 39 }, "Sunburnt Flats": { minimum: 35, maximum: 41 },
    "Salt Pans": { minimum: 38, maximum: 43 }, "Shifting Dunes": { minimum: 41, maximum: 46 },
    "Glass Dunes": { minimum: 46, maximum: 50 },
  };
  const saffronTables = {
    "Oasis Grove": [
      entry(35, 8, () => ["Dune Raider"]), entry(36, 8, () => ["Dune Raider"]),
      entry(37, 8, () => ["Dune Raider", "Dune Raider"]), entry(38, 7, () => ["Oasis Guardian"]),
      entry(39, 8, () => ["Sand Giant"]),
    ],
    "Sunburnt Flats": [
      entry(35, 8, () => ["Dune Raider"]), entry(36, 8, () => ["Magma Elemental"]),
      entry(37, 8, () => ["Dune Raider", "Dune Raider"]), entry(38, 8, () => ["Dune Raider", "Magma Elemental"]),
      entry(39, 8, () => ["Sand Giant"]), entry(40, 8, () => ["Ember Drake"]),
      entry(41, 7, () => ["Sand Giant", "Dune Raider"]),
    ],
    "Salt Pans": [
      entry(38, 8, () => ["Dune Raider", "Dune Raider"]), entry(39, 8, () => ["Sand Giant"]),
      entry(40, 8, () => ["Magma Elemental", "Dune Raider"]), entry(41, 8, () => ["Ember Drake"]),
      entry(42, 8, () => ["Sirocco Harpy"]), entry(43, 7, () => ["Sirocco Harpy", "Dune Raider"]),
    ],
    "Shifting Dunes": [
      entry(41, 8, () => ["Sand Giant", "Dune Raider"]), entry(42, 8, () => ["Sirocco Harpy"]),
      entry(43, 8, () => ["Sirocco Harpy", "Dune Raider"]), entry(44, 8, () => ["Sirocco Harpy", "Sirocco Harpy"]),
      entry(45, 8, () => ["Sunscale Lamia"]), entry(46, 8, () => ["Sunscale Lamia", "Dune Raider"]),
    ],
    "Glass Dunes": [
      entry(46, 8, () => ["Sunscale Lamia"]), entry(47, 8, () => ["Sand Wraith"]),
      entry(48, 8, () => ["Stone Giant"]), entry(49, 8, () => ["Sand Wraith", "Sirocco Harpy"]),
      entry(50, 8, () => ["Tomb Guardian"]),
    ],
  };
  const deepDesertChances = { "Town": 0, "Road": 0, "Outer Wastes": 34, "Howling Dunes": 40, "Obsidian Flats": 46, "Buried Necropolis": 52, "Pyramid Sanctum": 58 };
  const deepDesertProfiles = {
    "Town": { minimum: 45, maximum: 45, safe: true }, "Road": { minimum: 45, maximum: 45, safe: true },
    "Outer Wastes": { minimum: 45, maximum: 48 }, "Howling Dunes": { minimum: 47, maximum: 51 },
    "Obsidian Flats": { minimum: 49, maximum: 53 }, "Buried Necropolis": { minimum: 51, maximum: 54 },
    "Pyramid Sanctum": { minimum: 54, maximum: 55 },
  };
  const deepDesertTables = {
    "Outer Wastes": [
      entry(45, 8, () => ["Sunscale Lamia"]), entry(46, 8, () => ["Sunscale Lamia", "Dune Raider"]),
      entry(47, 8, () => ["Sand Wraith"]), entry(48, 8, () => ["Stone Giant"]),
    ],
    "Howling Dunes": [
      entry(47, 8, () => ["Sand Wraith"]), entry(48, 8, () => ["Stone Giant"]),
      entry(49, 8, () => ["Sand Wraith", "Sunscale Lamia"]), entry(50, 8, () => ["Tomb Guardian"]),
      entry(51, 8, () => ["Tomb Guardian", "Sand Wraith"]),
    ],
    "Obsidian Flats": [
      entry(49, 8, () => ["Stone Giant", "Sand Wraith"]), entry(50, 8, () => ["Tomb Guardian"]),
      entry(51, 8, () => ["Tomb Guardian", "Sunscale Lamia"]), entry(52, 8, () => ["Dust Revenant"]),
      entry(53, 8, () => ["Dust Revenant", "Sand Wraith"]),
    ],
    "Buried Necropolis": [
      entry(51, 8, () => ["Tomb Guardian", "Sand Wraith"]), entry(52, 8, () => ["Dust Revenant"]),
      entry(53, 8, () => ["Dust Revenant", "Tomb Guardian"]), entry(54, 8, () => ["Mountain Lamia"]),
    ],
    "Pyramid Sanctum": [
      entry(54, 8, () => ["Dust Revenant", "Tomb Guardian"]), entry(55, 8, () => ["Pyramid Sentinel"]),
    ],
  };
  const crimsonDivideChances = { "Town": 0, "Road": 0, "Caravan Dunes": 30, "Open Desert": 34, "Sunscorched Escarpment": 39, "Crimson Dunes": 44, "Bloodglass Flats": 50, "Heart of the Red Waste": 55 };
  const crimsonDivideProfiles = {
    "Town": { minimum: 40, maximum: 40, safe: true }, "Road": { minimum: 40, maximum: 40, safe: true },
    "Caravan Dunes": { minimum: 40, maximum: 43 }, "Open Desert": { minimum: 42, maximum: 46 },
    "Sunscorched Escarpment": { minimum: 44, maximum: 47 }, "Crimson Dunes": { minimum: 47, maximum: 50 },
    "Bloodglass Flats": { minimum: 50, maximum: 53 }, "Heart of the Red Waste": { minimum: 52, maximum: 55 },
  };
  const crimsonDivideTables = {
    "Caravan Dunes": [
      entry(40, 8, () => ["Sand Giant"]), entry(41, 8, () => ["Dune Raider", "Dune Raider"]),
      entry(42, 8, () => ["Sirocco Harpy"]), entry(43, 8, () => ["Sirocco Harpy", "Dune Raider"]),
    ],
    "Open Desert": [
      entry(42, 8, () => ["Sirocco Harpy"]), entry(43, 8, () => ["Dune Raider", "Sirocco Harpy"]),
      entry(44, 8, () => ["Caravan Bandit"]), entry(45, 8, () => ["Caravan Bandit", "Sunscale Lamia"]),
      entry(46, 8, () => ["Sunscale Lamia"]),
    ],
    "Sunscorched Escarpment": [
      entry(44, 8, () => ["Caravan Bandit"]), entry(45, 8, () => ["Sunscale Lamia"]),
      entry(46, 8, () => ["Caravan Bandit", "Sirocco Harpy"]), entry(47, 8, () => ["Sand Wraith"]),
    ],
    "Crimson Dunes": [
      entry(47, 8, () => ["Bloodsand Vampire"]), entry(48, 8, () => ["Bloodsand Vampire", "Bloodsand Vampire"]),
      entry(49, 8, () => ["Vampire Stalker"]), entry(50, 8, () => ["Vampire Stalker", "Bloodsand Vampire"]),
    ],
    "Bloodglass Flats": [
      entry(50, 8, () => ["Vampire Stalker"]), entry(51, 8, () => ["Vampire Stalker", "Sand Wraith"]),
      entry(52, 8, () => ["Vampire Hexer"]), entry(53, 8, () => ["Vampire Hexer", "Vampire Stalker"]),
    ],
    "Heart of the Red Waste": [
      entry(52, 8, () => ["Vampire Hexer"]), entry(53, 8, () => ["Vampire Hexer", "Vampire Stalker"]),
      entry(54, 8, () => ["Dust Revenant", "Vampire Hexer"]), entry(55, 8, () => ["Crimson Vampire Knight", "Vampire Stalker"]),
    ],
  };
  const bloodstoneChances = { "Town": 0, "Road": 0, "Redwind Dunes": 38, "Western Crags": 44, "Sanguine Wastes": 48, "Giant Rock Spires": 52, "Blood Coast": 56, "Vampire Citadel": 100 };
  const bloodstoneProfiles = {
    "Town": { minimum: 55, maximum: 55, safe: true }, "Road": { minimum: 55, maximum: 55, safe: true },
    "Redwind Dunes": { minimum: 55, maximum: 58 }, "Western Crags": { minimum: 58, maximum: 62 },
    "Sanguine Wastes": { minimum: 60, maximum: 65 }, "Giant Rock Spires": { minimum: 62, maximum: 67 },
    "Blood Coast": { minimum: 65, maximum: 70 }, "Vampire Citadel": { minimum: 67, maximum: 70 },
  };
  const bloodstoneTables = {
    "Redwind Dunes": [
      entry(55, 9, () => ["Crimson Vampire Knight"]), entry(56, 9, () => ["Vampire Hexer", "Vampire Stalker"]),
      entry(57, 9, () => ["Bloodstone Reaver", "Vampire Stalker"]), entry(58, 9, () => ["Bloodstone Reaver"]),
    ],
    "Western Crags": [
      entry(58, 9, () => ["Bloodstone Reaver"]), entry(59, 9, () => ["Bloodstone Reaver", "Vampire Stalker"]),
      entry(60, 9, () => ["Crimson Vampire Knight", "Bloodstone Reaver"]), entry(61, 9, () => ["Sanguine Oracle", "Bloodsand Vampire"]),
      entry(62, 9, () => ["Sanguine Oracle"]),
    ],
    "Sanguine Wastes": [
      entry(60, 9, () => ["Bloodstone Reaver", "Vampire Hexer"]), entry(61, 9, () => ["Sanguine Oracle", "Bloodsand Vampire"]),
      entry(62, 9, () => ["Sanguine Oracle"]), entry(63, 9, () => ["Sanguine Oracle", "Bloodstone Reaver"]),
      entry(64, 9, () => ["Bloodrock Sentinel", "Vampire Stalker"]), entry(65, 9, () => ["Bloodrock Sentinel"]),
    ],
    "Giant Rock Spires": [
      entry(62, 9, () => ["Sanguine Oracle"]), entry(63, 9, () => ["Bloodstone Reaver", "Sanguine Oracle"]),
      entry(64, 9, () => ["Bloodrock Sentinel", "Vampire Stalker"]), entry(65, 9, () => ["Bloodrock Sentinel"]),
      entry(66, 9, () => ["Bloodrock Sentinel", "Bloodstone Reaver"]), entry(67, 9, () => ["Bloodrock Sentinel", "Sanguine Oracle"]),
    ],
    "Blood Coast": [
      entry(65, 9, () => ["Bloodrock Sentinel"]), entry(66, 9, () => ["Bloodrock Sentinel", "Bloodstone Reaver"]),
      entry(67, 9, () => ["Bloodrock Sentinel", "Sanguine Oracle"]), entry(68, 9, () => ["Nocturne Castellan", "Vampire Stalker"]),
      entry(69, 9, () => ["Nocturne Castellan", "Bloodstone Reaver"]), entry(70, 9, () => ["Nocturne Castellan"]),
    ],
    "Vampire Citadel": [
      entry(67, 9, () => ["Bloodrock Sentinel", "Bloodstone Reaver"]),
      entry(68, 9, () => ["Nocturne Castellan", "Vampire Stalker"]),
      entry(69, 9, () => ["Nocturne Castellan", "Sanguine Oracle"]),
      entry(70, 9, () => ["Nocturne Castellan", "Bloodrock Sentinel"]),
    ],
  };
  const battlefieldChances = { "Town": 0, "Road": 0, "Scavenger Verge": 42, "Bone Pits": 48, "Shattered Causeway": 52, "Mass Graves": 58, "Bloodied Trenches": 62, "Silent Westfield": 60 };
  const battlefieldProfiles = {
    "Town": { minimum: 65, maximum: 65, safe: true }, "Road": { minimum: 65, maximum: 65, safe: true },
    "Scavenger Verge": { minimum: 65, maximum: 68 }, "Bone Pits": { minimum: 67, maximum: 71 },
    "Shattered Causeway": { minimum: 68, maximum: 72 }, "Mass Graves": { minimum: 70, maximum: 74 },
    "Bloodied Trenches": { minimum: 72, maximum: 75 }, "Silent Westfield": { minimum: 72, maximum: 75 },
  };
  const battlefieldTables = {
    "Scavenger Verge": [
      entry(65, 9, () => ["Battlefield Scavenger"]), entry(66, 9, () => ["Battlefield Scavenger", "Battlefield Scavenger"]),
      entry(67, 9, () => ["Bloodrock Sentinel", "Battlefield Scavenger"]), entry(68, 9, () => ["Gravebound Legionnaire"]),
    ],
    "Bone Pits": [
      entry(67, 9, () => ["Bloodrock Sentinel"]), entry(68, 9, () => ["Gravebound Legionnaire"]),
      entry(69, 9, () => ["Duskpine Wraith"]), entry(70, 9, () => ["Nocturne Castellan", "Gravebound Legionnaire"]),
      entry(71, 9, () => ["Rogue Vampire Hunter"]),
    ],
    "Shattered Causeway": [
      entry(68, 9, () => ["Gravebound Legionnaire"]), entry(69, 9, () => ["Battlefield Scavenger", "Gravebound Legionnaire"]),
      entry(70, 9, () => ["Nocturne Castellan"]), entry(71, 9, () => ["Rogue Vampire Hunter"]),
      entry(72, 9, () => ["Nightwood Vampire", "Battlefield Scavenger"]),
    ],
    "Mass Graves": [
      entry(70, 9, () => ["Gravebound Legionnaire", "Duskpine Wraith"]), entry(71, 9, () => ["Rogue Vampire Hunter"]),
      entry(72, 9, () => ["Nightwood Vampire"]), entry(73, 9, () => ["Blood Exile"]),
      entry(74, 9, () => ["Blood Exile", "Gravebound Legionnaire"]),
    ],
    "Bloodied Trenches": [
      entry(72, 9, () => ["Nightwood Vampire", "Gravebound Legionnaire"]), entry(73, 9, () => ["Blood Exile"]),
      entry(74, 9, () => ["Rogue Vampire Hunter", "Blood Exile"]), entry(75, 9, () => ["Blood Exile", "Gravebound Legionnaire"]),
    ],
    "Silent Westfield": [
      entry(72, 9, () => ["Nightwood Vampire"]), entry(73, 9, () => ["Blood Exile"]),
      entry(74, 9, () => ["Rogue Vampire Hunter", "Battlefield Scavenger"]), entry(75, 9, () => ["Blood Exile", "Rogue Vampire Hunter"]),
    ],
  };
  const duskpineChances = { "Town": 0, "Road": 0, "Frostpine Fringe": 38, "Blackpine Woods": 45, "Deep Boreal": 55, "Frostbound Bog": 58, "Shadowroot Hollow": 62 };
  const duskpineProfiles = {
    "Town": { minimum: 60, maximum: 60, safe: true }, "Road": { minimum: 60, maximum: 60, safe: true },
    "Frostpine Fringe": { minimum: 60, maximum: 64 }, "Blackpine Woods": { minimum: 63, maximum: 68 },
    "Deep Boreal": { minimum: 68, maximum: 72 }, "Frostbound Bog": { minimum: 70, maximum: 74 },
    "Shadowroot Hollow": { minimum: 72, maximum: 75 },
  };
  const duskpineTables = {
    "Frostpine Fringe": [
      entry(60, 9, () => ["Duskpine Wolf"]), entry(61, 9, () => ["Duskpine Wolf", "Duskpine Wolf"]),
      entry(62, 9, () => ["Boreal Direwolf"]), entry(63, 9, () => ["Boreal Direwolf"]),
      entry(64, 9, () => ["Battlefield Scavenger"]),
    ],
    "Blackpine Woods": [
      entry(63, 9, () => ["Boreal Direwolf"]), entry(64, 9, () => ["Boreal Direwolf"]),
      entry(65, 9, () => ["Battlefield Scavenger"]), entry(66, 9, () => ["Battlefield Scavenger", "Duskpine Wolf"]),
      entry(67, 9, () => ["Bloodrock Sentinel"]), entry(68, 9, () => ["Gravebound Legionnaire"]),
    ],
    "Deep Boreal": [
      entry(68, 9, () => ["Gravebound Legionnaire"]), entry(69, 9, () => ["Duskpine Wraith"]),
      entry(70, 9, () => ["Duskpine Wraith", "Boreal Direwolf"]), entry(71, 9, () => ["Rogue Vampire Hunter"]),
      entry(72, 9, () => ["Nightwood Vampire"]),
    ],
    "Frostbound Bog": [
      entry(70, 9, () => ["Duskpine Wraith"]), entry(71, 9, () => ["Duskpine Wraith", "Gravebound Legionnaire"]),
      entry(72, 9, () => ["Nightwood Vampire"]), entry(73, 9, () => ["Blood Exile"]),
      entry(74, 9, () => ["Nightwood Vampire", "Duskpine Wraith"]),
    ],
    "Shadowroot Hollow": [
      entry(72, 9, () => ["Nightwood Vampire"]), entry(73, 9, () => ["Blood Exile"]),
      entry(74, 9, () => ["Nightwood Vampire", "Duskpine Wraith"]), entry(75, 9, () => ["Blood Exile", "Nightwood Vampire"]),
    ],
  };
  const regionRuleSets = {
    "alder-vale": { chances, profiles, tables },
    "stoneveil-foothills": { chances: stoneveilChances, profiles: stoneveilProfiles, tables: stoneveilTables },
    "verdant-reach": { chances: verdantChances, profiles: verdantProfiles, tables: verdantTables },
    "high-stoneveil": { chances: highStoneveilChances, profiles: highStoneveilProfiles, tables: highStoneveilTables },
    "stoneveil-summit": { chances: stoneveilSummitChances, profiles: stoneveilSummitProfiles, tables: stoneveilSummitTables },
    "sunward-coast": { chances: sunwardChances, profiles: sunwardProfiles, tables: sunwardTables },
    "cinder-isle": { chances: cinderChances, profiles: cinderProfiles, tables: cinderTables },
    "stormwake-hills": { chances: stormwakeChances, profiles: stormwakeProfiles, tables: stormwakeTables },
    "gloamfen-marsh": { chances: gloamfenChances, profiles: gloamfenProfiles, tables: gloamfenTables },
    "verdigris-jungle": { chances: verdigrisChances, profiles: verdigrisProfiles, tables: verdigrisTables },
    "mirrormere-basin": { chances: mirrormereChances, profiles: mirrormereProfiles, tables: mirrormereTables },
    "saffron-expanse": { chances: saffronChances, profiles: saffronProfiles, tables: saffronTables },
    "deep-desert": { chances: deepDesertChances, profiles: deepDesertProfiles, tables: deepDesertTables },
    "crimson-divide": { chances: crimsonDivideChances, profiles: crimsonDivideProfiles, tables: crimsonDivideTables },
    "bloodstone-coast": { chances: bloodstoneChances, profiles: bloodstoneProfiles, tables: bloodstoneTables },
    "ancient-battlefield": { chances: battlefieldChances, profiles: battlefieldProfiles, tables: battlefieldTables },
    "duskpine-forest": { chances: duskpineChances, profiles: duskpineProfiles, tables: duskpineTables },
  };
  function regionRules(regionId) {
    return regionRuleSets[regionId] || regionRuleSets["alder-vale"];
  }
  function profileFor(zone, regionId = "alder-vale") { return regionRules(regionId).profiles[zone] || null; }

  function contextAndRandom(zone, context, random) {
    const requestedRegionId = typeof context === "object" ? context?.regionId : null;
    const regionId = regionRuleSets[requestedRegionId] ? requestedRegionId : "alder-vale";
    const selectedProfiles = regionRules(regionId).profiles;
    if (typeof context === "function") return { level: selectedProfiles[zone]?.minimum || 1, random: context, regionId };
    const requestedLevel = typeof context === "number" ? context : context?.level;
    return {
      level: Math.max(1, Math.floor(Number(requestedLevel) || selectedProfiles[zone]?.minimum || 1)),
      random: typeof random === "function" ? random : Math.random,
      regionId,
    };
  }

  function targetLevel(zone, requestedLevel = 1, regionId = "alder-vale") {
    const profile = profileFor(zone, regionId);
    if (!profile) return Math.max(1, Math.floor(Number(requestedLevel) || 1));
    return Math.min(profile.maximum, Math.max(profile.minimum, Math.floor(Number(requestedLevel) || profile.minimum)));
  }

  function weightedPick(entries, weightOf, random) {
    const total = entries.reduce((sum, candidate) => sum + Math.max(0, Number(weightOf(candidate)) || 0), 0);
    if (!entries.length || total <= 0) return null;
    let roll = unitRandom(random) * total;
    for (const candidate of entries) {
      roll -= Math.max(0, Number(weightOf(candidate)) || 0);
      if (roll < 0) return candidate;
    }
    return entries.at(-1);
  }

  function select(zone, context = {}, random = Math.random) {
    const resolved = contextAndRandom(zone, context, random);
    const entries = regionRules(resolved.regionId).tables[zone] || [];
    if (!entries.length) return null;
    const target = targetLevel(zone, resolved.level, resolved.regionId);
    const levels = [...new Set(entries.map(candidate => candidate.level))];
    const selectedLevel = weightedPick(levels, candidateLevel => {
      const distance = Math.abs(candidateLevel - target);
      return distance === 0 ? 72 : distance === 1 ? 14 : distance === 2 ? 2 : 0;
    }, resolved.random);
    const choices = entries.filter(candidate => candidate.level === selectedLevel);
    const selected = weightedPick(choices, candidate => candidate.weight, resolved.random);
    return selected ? { level: selected.level, targetLevel: target, enemies: selected.create(resolved.random) } : null;
  }

  function generate(zone, context = {}, random = Math.random) {
    return select(zone, context, random)?.enemies || [];
  }

  // An encounter of this zone that includes the named enemy, in one of its usual groups: the groups
  // nearest the target level are weighted as select() weights them (a Cull quest's target, task 2).
  function generateIncluding(zone, name, context = {}, random = Math.random) {
    const resolved = contextAndRandom(zone, context, random);
    const samples = [0, 0.2, 0.4, 0.6, 0.8, 0.999];
    const entries = (regionRules(resolved.regionId).tables[zone] || []).filter(candidate => samples.some(value => candidate.create(() => value).includes(name)));
    if (!entries.length) return [name];
    const target = targetLevel(zone, resolved.level, resolved.regionId);
    const levels = [...new Set(entries.map(candidate => candidate.level))];
    const nearest = Math.min(...levels.map(candidateLevel => Math.abs(candidateLevel - target)));
    const selectedLevel = weightedPick(levels, candidateLevel => {
      const distance = Math.abs(candidateLevel - target) - nearest;
      return distance === 0 ? 72 : distance === 1 ? 14 : distance === 2 ? 2 : 0;
    }, resolved.random);
    const selected = weightedPick(entries.filter(candidate => candidate.level === selectedLevel), candidate => candidate.weight, resolved.random);
    // A group whose make-up is rolled may come out without the target; roll it again a few times.
    for (let attempt = 0; selected && attempt < 12; attempt++) {
      const enemies = selected.create(resolved.random);
      if (enemies.includes(name)) return enemies;
    }
    return [name];
  }

  function encounterChance(zone, context = {}) {
    const resolved = contextAndRandom(zone, context, Math.random);
    const profile = profileFor(zone, resolved.regionId);
    const aboveArea = resolved.level - (profile?.maximum ?? resolved.level);
    const levelMultiplier = aboveArea >= 20 ? .4 : aboveArea >= 10 ? .7 : 1;
    const requestedMultiplier = Number(context?.encounterChanceMultiplier ?? 1);
    const classMultiplier = Number.isFinite(requestedMultiplier) ? Math.max(0, requestedMultiplier) : 1;
    return Math.min(100, (regionRules(resolved.regionId).chances[zone] || 0) * levelMultiplier * classMultiplier);
  }

  function roll(zone, context = {}, random = Math.random) {
    const resolved = contextAndRandom(zone, context, random);
    const chance = encounterChance(zone, context);
    return unitRandom(resolved.random) * 100 < chance ? generate(zone, { level: resolved.level, regionId: resolved.regionId }, resolved.random) : null;
  }

  function enemiesForZone(zone, regionId = "alder-vale") {
    return [...new Set((regionRules(regionId).tables[zone] || []).flatMap(candidate => candidate.create(() => 0)))];
  }

  root.EncounterRules = {
    chances, profiles, enemyLevels, animalFamilies, dragonElements, tables,
    stoneveilChances, stoneveilProfiles, stoneveilTables,
    verdantChances, verdantProfiles, verdantTables,
    highStoneveilChances, highStoneveilProfiles, highStoneveilTables,
    stoneveilSummitChances, stoneveilSummitProfiles, stoneveilSummitTables,
    sunwardChances, sunwardProfiles, sunwardTables,
    cinderChances, cinderProfiles, cinderTables,
    stormwakeChances, stormwakeProfiles, stormwakeTables,
    gloamfenChances, gloamfenProfiles, gloamfenTables,
    verdigrisChances, verdigrisProfiles, verdigrisTables,
    mirrormereChances, mirrormereProfiles, mirrormereTables,
    saffronChances, saffronProfiles, saffronTables,
    deepDesertChances, deepDesertProfiles, deepDesertTables,
    crimsonDivideChances, crimsonDivideProfiles, crimsonDivideTables,
    bloodstoneChances, bloodstoneProfiles, bloodstoneTables,
    battlefieldChances, battlefieldProfiles, battlefieldTables,
    duskpineChances, duskpineProfiles, duskpineTables,
    regionRules, profileFor, targetLevel, select, generate, generateIncluding, roll, encounterChance, enemiesForZone,
  };
  if (typeof module !== "undefined") module.exports = root.EncounterRules;
})(globalThis);

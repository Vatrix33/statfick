#!/usr/bin/env python3
"""Task 9's content draft (the Potion Rework, decisions P1-P29 in ../../Engine-Plan.md): the ingredients, potions,
poisons and recipes, checked and written out as draft.md for the user's review (P5).

    python3 draft.py            # checks the counts and the recipes, writes draft.md next to this file

Durations count the recipient's own turns in battle (the game's timed effects). Prices are in silver.
"""
import os, sys
from collections import Counter, defaultdict

RARITIES = ["common", "uncommon", "rare", "epic", "legendary"]

# ---------------------------------------------------------------- ingredients
# name: (rarity, price, forage zones, enemies that drop it)
INGREDIENTS = {
    # Common: the lowland regions (Levels 1-35) and their common enemies.
    "Meadowmint": ("common", 40, ["Alder Vale: Plains", "Stoneveil Foothills: Plains, Hill-Plains", "Verdant Reach: Plains", "Sunward Coast: Coastal Plains, Windswept Plains", "Stormwake Hills: Rainwashed Downs"], ["Bandits (all four)"]),
    "Redcap Berry": ("common", 40, ["Alder Vale: Forest", "Verdant Reach: Outer Forest, Forest"], ["Juvenile Bear", "Adult Bear", "Bandit Archer"]),
    "Riverreed": ("common", 40, ["every region's rivers", "Mirrormere Basin: Lake Shore, Reed Marsh", "Gloamfen Marsh: Reedwater"], ["Water Spirit"]),
    "Clearwater Moss": ("common", 50, ["every region's rivers", "Mirrormere Basin: Lake Shore", "Saffron Expanse: Oasis Grove"], ["Water Spirit", "Greater Water Spirit"]),
    "Spirit Dew": ("common", 60, ["Alder Vale: Deep Forest", "Verdant Reach: Deep Forest"], ["Nature Spirit", "Harvest Wisp", "Fire Spirit"]),
    "Wolf Fang": ("common", 50, [], ["Juvenile Wolf", "Adult Wolf", "Highland Wolf", "Dire Wolf", "Corrupted Wolf"]),
    "Bear Fat": ("common", 50, [], ["Juvenile Bear", "Adult Bear", "Mountain Bear", "Corrupted Bear"]),
    "Griffon Down": ("common", 60, ["Stoneveil Foothills: Hill-Plains", "High Stoneveil: Lower Slopes"], ["Juvenile Griffon", "Adult Griffon", "Highland Griffon", "Corrupted Griffon"]),
    "Harpy Feather": ("common", 60, ["High Stoneveil: Cliffside", "Mirrormere Basin: Windcut Bluffs"], ["Harpy Skirmisher", "Harpy Huntress", "Harpy Matriarch"]),
    "Stonebloom Lichen": ("common", 50, ["Stoneveil Foothills: Mountainfoot", "High Stoneveil: Lower Slopes", "Gloamfen Marsh: Rocky Rise", "Bloodstone Coast: Western Crags"], ["Earth Spirit", "Greater Earth Spirit"]),
    "Mire Bulb": ("common", 40, ["Gloamfen Marsh: Open Mire, Reedwater", "Stormwake Hills: Floodplain", "Sunward Coast: Salt Marsh"], ["Goblin Raider"]),
    "Saltcrust": ("common", 40, ["Saffron Expanse: Salt Pans", "Sunward Coast: Salt Marsh, Beach", "Cinder Isle: Black Sand Shore"], ["Coastal Corsair"]),
    "Dune Thistle": ("common", 50, ["Mirrormere Basin: Dry Steppe, Desert Fringe", "Saffron Expanse: Sunburnt Flats", "Crimson Divide: Caravan Dunes", "Deep Desert: Outer Wastes", "Sunward Coast: Dunes"], ["Dune Raider", "Caravan Bandit"]),
    "Grubroot": ("common", 40, ["Alder Vale: Plains", "Stoneveil Foothills: Plains"], ["Goblin", "Goblin Archer", "Goblin Warrior", "Goblin Boss", "Highland Goblin", "Goblin Warlord", "Orc", "Forest Orc"]),
    "Oakbark": ("common", 50, ["Alder Vale: Forest, Deep Forest", "Verdant Reach: Forest"], ["Young Treant", "Dryad Warden"]),
    # Uncommon: the middle regions (Levels 15-55) and their enemies.
    "Dryad Sap": ("uncommon", 200, ["Verdant Reach: Deep Forest"], ["Dryad Warden", "Dryad Matriarch", "Blossom Sprite", "Grove Matriarch"]),
    "Troll Blood": ("uncommon", 250, [], ["Hill Troll", "Elder Troll", "Troll Shieldbearer", "Troll War-drummer", "Thunder Troll", "Bog Troll", "Crag Troll"]),
    "Treant Heartwood": ("uncommon", 220, ["Verdant Reach: Deep Forest", "Alder Vale: Deep Forest"], ["Young Treant", "Elder Treant", "Jungle Treant"]),
    "Elemental Essence": ("uncommon", 250, [], ["the six Elementals of Level 25", "Greater Water, Earth and Nature Spirits", "Raging Water Spirit", "Radiant Sentinel", "Hemoghost", "Sunhorn Stag"]),
    "Stormcap": ("uncommon", 200, ["Stormwake Hills: Thunder Hills, Stormcrest, Rainwashed Downs"], ["Storm Harpy", "Lightning Elemental", "Rainbound Marauder"]),
    "Merfolk Scale": ("uncommon", 220, ["Sunward Coast: Beach", "Bloodstone Coast: Blood Coast"], ["Merfolk Spearfisher", "Reed Merfolk", "Merfolk Reef Guard", "Merfolk Tidecaller", "Merfolk Steamcaller"]),
    "Warpaint Clay": ("uncommon", 180, ["Stormwake Hills: Floodplain"], ["Orc Berserker", "Orc Chieftain", "Apostle of Talos"]),
    "Satyr's Laurel": ("uncommon", 180, ["Verdant Reach: Forest"], ["Satyr Scout", "Satyr Warrior", "Satyr Grovekeeper"]),
    "Bogbean": ("uncommon", 180, ["Gloamfen Marsh: Deep Mire", "Duskpine Forest: Frostbound Bog", "Mirrormere Basin: Reed Marsh"], ["Mire Stalker", "Bog Troll"]),
    "Glowcap": ("uncommon", 220, ["Verdigris Jungle: Dense Jungle", "Duskpine Forest: Shadowroot Hollow", "Gloamfen Marsh: Deep Mire"], ["Fen Spirit", "Canopy Stalker"]),
    "Jungle Orchid": ("uncommon", 200, ["Verdigris Jungle: Jungle Verge, Outer Jungle, High Canopy"], ["Canopy Stalker"]),
    "Nightshade": ("uncommon", 200, ["Alder Vale: Deep Forest", "Duskpine Forest: Blackpine Woods", "Ancient Battlefield: Mass Graves"], ["Void Imp", "Undead Thrall"]),
    "Sunfruit": ("uncommon", 200, ["Saffron Expanse: Oasis Grove, Sunburnt Flats"], ["Oasis Guardian"]),
    "Glass Sand": ("uncommon", 200, ["Saffron Expanse: Glass Dunes, Shifting Dunes", "Deep Desert: Howling Dunes"], ["Sand Giant", "Sirocco Harpy"]),
    "Frostpine Needle": ("uncommon", 220, ["Duskpine Forest: Frostpine Fringe, Blackpine Woods", "High Stoneveil: Frozen Heights"], ["Rime Wolf", "Ice Elemental", "Duskpine Wolf"]),
    "Ember Moss": ("uncommon", 220, ["Cinder Isle: Ember Scrub, Ashen Slope, Black Sand Shore"], ["Fire Spirit", "Fire Elemental", "Ash Harpy", "Lava Stalker"]),
    "Bone Dust": ("uncommon", 180, ["Ancient Battlefield: Bone Pits, Scavenger Verge", "Deep Desert: Buried Necropolis"], ["Undead Thrall", "Undead Hulk", "Gravebound Legionnaire", "Battlefield Scavenger"]),
    "Imp Horn": ("uncommon", 220, ["Gloamfen Marsh: Abyssal Rift"], ["Lesser Imp", "Void Imp", "Void Stalker", "Abyssal Imp"]),
    # Rare: the dangerous regions (Levels 30-75), their stronger enemies.
    "Wyrm Scale": ("rare", 800, ["High Stoneveil: Dragonspine"], ["Mountain Wyrm", "Shore Wyrm", "Cliff Wyrm", "Canopy Wyrm", "Ember Drake"]),
    "Giant Marrow": ("rare", 700, ["Bloodstone Coast: Giant Rock Spires"], ["Young Giant", "Sand Giant", "Stone Giant", "Peak Giant"]),
    "Lamia Venom": ("rare", 700, ["Crimson Divide: Sunscorched Escarpment"], ["Sunscale Lamia", "Mossback Lamia", "Mountain Lamia", "Corrupted Mossback Lamia"]),
    "Obsidian Flake": ("rare", 600, ["Deep Desert: Obsidian Flats", "Cinder Isle: Lava Fields"], ["Magma Elemental", "Vulcanic Ram", "Steelguard Golem"]),
    "Siren Pearl": ("rare", 900, ["Sunward Coast: Beach", "Bloodstone Coast: Blood Coast"], ["Tide Siren", "Merfolk Tidecaller"]),
    "Rime Lotus": ("rare", 800, ["High Stoneveil: Frozen Heights", "Duskpine Forest: Frostbound Bog", "Stoneveil Summit: Windcut Ascent"], ["Frost Elemental", "Boreal Direwolf"]),
    "Thunderglass": ("rare", 800, ["Stormwake Hills: Stormcrest", "Saffron Expanse: Glass Dunes"], ["Lightning Elemental", "Storm Elemental", "Thunder Troll", "Thunder Boar Gorthok"]),
    "Tomb Myrrh": ("rare", 700, ["Deep Desert: Pyramid Sanctum, Buried Necropolis"], ["Tomb Guardian", "Pyramid Sentinel", "Dust Revenant", "Sand Wraith"]),
    "Old-Growth Amber": ("rare", 800, ["Verdigris Jungle: Old Growth, High Canopy"], ["Jungle Core", "Jungle Treant", "Canopy Stalker"]),
    "Vampire Ash": ("rare", 800, ["Duskpine Forest: Shadowroot Hollow"], ["Bloodsand Vampire", "Vampire Stalker", "Vampire Hexer", "Crimson Vampire Knight", "Nightwood Vampire", "Blood Exile"]),
    "Abyssal Thorn": ("rare", 700, ["Gloamfen Marsh: Abyssal Rift"], ["Rift Fiend", "Abyss Witch", "Corrupted Bog Troll", "Corrupted Mire Stalker"]),
    "Blood Glass": ("rare", 800, ["Crimson Divide: Bloodglass Flats, Crimson Dunes", "Bloodstone Coast: Sanguine Wastes"], ["Bloodstone Reaver", "Bloodrock Sentinel", "Sanguine Oracle"]),
    "Stormfeather": ("rare", 700, ["High Stoneveil: High Pass", "Mirrormere Basin: Windcut Bluffs"], ["Great Griffon", "High Griffon", "Harpy Sentinel", "Wind Elemental"]),
    # Epic: the high regions (Levels 55-95) and the strongest common enemies.
    "Dragon Blood": ("epic", 2500, ["Stoneveil Summit: Dragon Eyries"], ["the twelve colour Dragons of Level 60"]),
    "Phoenix Ash": ("epic", 2500, ["Cinder Isle: Caldera (seldom)", "Crimson Divide: Heart of the Red Waste"], ["Magma Elemental (seldom)"]),
    "Cloudbreak Edelweiss": ("epic", 2200, ["Stoneveil Summit: Cloudbreak Ridge, Giant Steps, Windcut Ascent", "High Stoneveil: High Pass (seldom)"], ["Peak Giant", "Glacier Giant", "Stormforged Giant"]),
    "Brimstone Heart": ("epic", 2500, ["Crimson Divide: Heart of the Red Waste"], ["the three Great Devil Servants"]),
    "Gravecrown Moss": ("epic", 2000, ["Ancient Battlefield: Mass Graves, Silent Westfield, Bloodied Trenches", "Duskpine Forest: Shadowroot Hollow (seldom)"], ["Gravebound Legionnaire", "Duskpine Wraith", "Nocturne Castellan"]),
    "Moonshade Lily": ("epic", 2200, ["Duskpine Forest: Shadowroot Hollow, Deep Boreal"], ["Nightwood Vampire", "Pale Monarch", "Rogue Vampire Hunter"]),
    # Legendary: one per high region with a town to buy it in (P13), and from the last bosses.
    "Starfall Dust": ("legendary", 8000, ["Stoneveil Summit: Frozen Crown, Dragon Eyries", "High Stoneveil: Dragonspine (seldom)"], ["Ancient Frost Dragon", "Tempest Dragon", "Crownfire Dragon"]),
    "Heartseed": ("legendary", 7000, ["Verdigris Jungle: Old Growth (seldom)"], []),
    "Soulglass Shard": ("legendary", 7000, ["Duskpine Forest: Shadowroot Hollow (seldom)", "Ancient Battlefield: Silent Westfield"], ["Pale Monarch"]),
    "Sunheart Ember": ("legendary", 7500, ["Crimson Divide: Heart of the Red Waste (seldom)"], ["Crownfire Dragon"]),
}

# The bosses' own ingredients: always one, and a 35% chance of a second (P12: bosses drop more and rarer ones).
BOSS_DROPS = {
    "Great Imp Ji'ruan": ["Imp Horn", "Stormfeather"],
    "Juila the Grand Forest Matriarch": ["Dryad Sap", "Old-Growth Amber"],
    "Nygheir the Wounded": ["Wyrm Scale", "Dragon Blood"],
    "Zik Lun, the armored Hulk": ["Troll Blood", "Giant Marrow"],
    "Deep Sea King": ["Siren Pearl", "Merfolk Scale"],
    "Tankina, Disciple of Talos": ["Thunderglass", "Warpaint Clay"],
    "Ornn, God of the Forge": ["Obsidian Flake", "Phoenix Ash"],
    "Gaian, Avatar of the Land": ["Heartseed", "Old-Growth Amber"],
    "Archdevil Saffron": ["Brimstone Heart", "Sunheart Ember"],
    "Ancient Lich Ool": ["Soulglass Shard", "Gravecrown Moss"],
    "Ur-Mat, the False Dreamer": ["Moonshade Lily", "Soulglass Shard"],
    "Anatemis, the White Star": ["Starfall Dust", "Dragon Blood"],
}

# ---------------------------------------------------------------- potions
# name: (rarity, price, effect text, duration text, [three ingredients], use outside battle)
HEALTH_MANA = {
    "Potion of Lesser Health": ("common", 450, "Heals 25 HP plus 10% of max HP (unchanged)", "instant", ["Meadowmint", "Redcap Berry", "Riverreed"], True),
    "Potion of Health": ("uncommon", 1200, "Heals 50 HP plus 15% of max HP (unchanged)", "instant", ["Meadowmint", "Redcap Berry", "Dryad Sap"], True),
    "Potion of Greater Health": ("rare", 3500, "Heals 75 HP plus 15% of max HP (unchanged)", "instant", ["Redcap Berry", "Troll Blood", "Old-Growth Amber"], True),
    "Potion of Superior Health": ("epic", 8000, "Heals 100 HP plus 20% of max HP (unchanged)", "instant", ["Troll Blood", "Old-Growth Amber", "Phoenix Ash"], True),
    "Potion of Ultimate Health": ("legendary", 18000, "Heals 200 HP plus 25% of max HP (unchanged)", "instant", ["Troll Blood", "Phoenix Ash", "Heartseed"], True),
    "Potion of Lesser Mana": ("common", 400, "Restores 10 Mana plus 5% of max Mana (unchanged)", "instant", ["Spirit Dew", "Clearwater Moss", "Riverreed"], True),
    "Potion of Mana": ("uncommon", 1000, "Restores 20 Mana plus 10% of max Mana (unchanged)", "instant", ["Spirit Dew", "Clearwater Moss", "Elemental Essence"], True),
    "Potion of Greater Mana": ("rare", 3000, "Restores 30 Mana plus 15% of max Mana (unchanged)", "instant", ["Clearwater Moss", "Elemental Essence", "Siren Pearl"], True),
    "Potion of Superior Mana": ("epic", 7000, "Restores 40 Mana plus 20% of max Mana (unchanged)", "instant", ["Elemental Essence", "Siren Pearl", "Cloudbreak Edelweiss"], True),
    "Potion of Ultimate Mana": ("legendary", 16000, "Restores 50 Mana plus 20% of max Mana (unchanged)", "instant", ["Elemental Essence", "Cloudbreak Edelweiss", "Starfall Dust"], True),
}
POTIONS = {
    # Common (8): modest, cheap, for ordinary trips.
    "Draught of Vigor": ("common", 350, "+10% Strength", "3 turns", ["Wolf Fang", "Bear Fat", "Meadowmint"], False),
    "Mindspark Tonic": ("common", 400, "+10% spell power", "3 turns", ["Spirit Dew", "Clearwater Moss", "Grubroot"], False),
    "Barkskin Tonic": ("common", 350, "+15% Armor", "3 turns", ["Oakbark", "Stonebloom Lichen", "Bear Fat"], False),
    "Swiftwind Draught": ("common", 400, "+10% Speed (your turns come sooner)", "3 turns", ["Griffon Down", "Harpy Feather", "Meadowmint"], False),
    "Hawkeye Drops": ("common", 350, "+10% hit chance with weapons", "3 turns", ["Harpy Feather", "Saltcrust", "Redcap Berry"], False),
    "Antidote": ("common", 300, "Removes one negative effect (a poison first, if any)", "instant", ["Riverreed", "Saltcrust", "Mire Bulb"], True),
    "Mending Draught": ("common", 450, "Heals 4% of max HP at the start of each of your turns", "4 turns", ["Meadowmint", "Oakbark", "Clearwater Moss"], False),
    "Emberward Tonic": ("common", 400, "+20% Fire resistance", "4 turns", ["Saltcrust", "Dune Thistle", "Clearwater Moss"], False),
    # Uncommon (12): stronger or more specialised; for preparing for a fight.
    "Elixir of Might": ("uncommon", 1100, "+20% Strength", "3 turns", ["Wolf Fang", "Troll Blood", "Warpaint Clay"], False),
    "Arcane Draught": ("uncommon", 1200, "+20% spell power", "3 turns", ["Spirit Dew", "Elemental Essence", "Glowcap"], False),
    "Stoneskin Potion": ("uncommon", 1200, "-15% weapon damage taken", "3 turns", ["Stonebloom Lichen", "Treant Heartwood", "Bear Fat"], False),
    "Spellward Potion": ("uncommon", 1200, "-15% spell damage taken", "3 turns", ["Elemental Essence", "Saltcrust", "Satyr's Laurel"], False),
    "Catseye Potion": ("uncommon", 1000, "+15% Evasion", "3 turns", ["Griffon Down", "Harpy Feather", "Jungle Orchid"], False),
    "Potion of Precision": ("uncommon", 1300, "+10% critical hit chance", "3 turns", ["Harpy Feather", "Glass Sand", "Wolf Fang"], False),
    "Tidal Ward": ("uncommon", 1000, "+25% Water and Ice resistance", "4 turns", ["Merfolk Scale", "Frostpine Needle", "Clearwater Moss"], False),
    "Storm Ward": ("uncommon", 1000, "+25% Lightning and Wind resistance", "4 turns", ["Stormcap", "Griffon Down", "Stonebloom Lichen"], False),
    "Grave Ward": ("uncommon", 1100, "+25% Dark and Death resistance", "4 turns", ["Bone Dust", "Saltcrust", "Satyr's Laurel"], False),
    "Clarity Draught": ("uncommon", 900, "Removes up to two negative effects", "instant", ["Riverreed", "Saltcrust", "Sunfruit"], True),
    "Manaflow Tonic": ("uncommon", 1100, "Restores 5% of max Mana at the start of each of your turns", "4 turns", ["Clearwater Moss", "Elemental Essence", "Jungle Orchid"], False),
    "Bloodroot Tonic": ("uncommon", 1000, "+20% healing received", "4 turns", ["Redcap Berry", "Dryad Sap", "Bogbean"], False),
    # Rare (10): an edge in hard fights.
    "Berserker's Brew": ("rare", 3000, "+25% weapon damage dealt, but -10% Armor", "3 turns", ["Troll Blood", "Warpaint Clay", "Imp Horn"], False),
    "Sorcerer's Philtre": ("rare", 3500, "+30% spell power", "3 turns", ["Elemental Essence", "Glowcap", "Thunderglass"], False),
    "Potion of Steadfastness": ("rare", 3000, "Immune to stuns and AP loss", "4 turns", ["Stonebloom Lichen", "Treant Heartwood", "Giant Marrow"], False),
    "Duelist's Draught": ("rare", 3500, "+30% critical damage and +5% critical hit chance", "3 turns", ["Harpy Feather", "Glass Sand", "Stormfeather"], False),
    "Wraithstep Elixir": ("rare", 3500, "+25% Evasion, and spells aimed at you miss 15% of the time", "3 turns", ["Nightshade", "Vampire Ash", "Griffon Down"], False),
    "Bulwark Potion": ("rare", 3000, "A barrier of 15% of max HP that takes damage first", "3 turns (what's left then fades)", ["Treant Heartwood", "Obsidian Flake", "Stonebloom Lichen"], False),
    "Elixir of Renewal": ("rare", 3500, "Heals 7% of max HP at the start of each of your turns, and +15% healing received", "4 turns", ["Dryad Sap", "Troll Blood", "Old-Growth Amber"], False),
    "Prismatic Ward": ("rare", 4000, "+20% resistance to every element", "3 turns", ["Elemental Essence", "Ember Moss", "Rime Lotus"], False),
    "Piercing Draught": ("rare", 3000, "Weapon attacks ignore 20% of the target's Armor", "3 turns", ["Wyrm Scale", "Glass Sand", "Warpaint Clay"], False),
    "Purging Elixir": ("rare", 2800, "Removes every negative effect; none can take hold until your next turn", "instant, then until your next turn", ["Saltcrust", "Sunfruit", "Tomb Myrrh"], True),
    # Epic (6): for bosses, Empowered and Sparkling enemies.
    "Titan's Draught": ("epic", 9000, "+20% to every attribute", "3 turns", ["Giant Marrow", "Troll Blood", "Dragon Blood"], False),
    "Elixir of the Unbroken": ("epic", 8000, "Immune to every negative effect (those you have stay)", "3 turns", ["Tomb Myrrh", "Obsidian Flake", "Gravecrown Moss"], False),
    "Draught of Haste": ("epic", 9000, "+1 AP at the start of each of your turns", "3 turns", ["Stormfeather", "Thunderglass", "Cloudbreak Edelweiss"], False),
    "Giantsbane Draught": ("epic", 8500, "+30% damage dealt to bosses and Empowered or Sparkling enemies", "4 turns", ["Wyrm Scale", "Blood Glass", "Brimstone Heart"], False),
    "Phoenix Tonic": ("epic", 9500, "If you fall, you rise again once with 30% of max HP", "4 turns", ["Phoenix Ash", "Ember Moss", "Sunfruit"], False),
    "Archmage's Elixir": ("epic", 9000, "+30% spell power, and spells cost 25% less Mana", "3 turns", ["Elemental Essence", "Siren Pearl", "Moonshade Lily"], False),
    # Legendary (4): exceptional, for the fights that matter most.
    "Elixir of Ascendance": ("legendary", 24000, "+35% to every attribute and +15% damage dealt; afterwards -15% to every attribute for 2 turns", "3 turns", ["Dragon Blood", "Giant Marrow", "Sunheart Ember"], False),
    "Undying Draught": ("legendary", 22000, "You can't fall below 1 HP; when it ends you heal 20% of max HP", "2 turns", ["Phoenix Ash", "Troll Blood", "Soulglass Shard"], False),
    "Panacea": ("legendary", 20000, "Heals 40% of max HP and Mana, removes every negative effect, and none can take hold", "instant, immunity 2 turns", ["Heartseed", "Dryad Sap", "Moonshade Lily"], True),
    "Elixir of Kinship": ("legendary", 25000, "Every ally heals 25% of max HP and gains +15% to every attribute", "3 turns", ["Starfall Dust", "Cloudbreak Edelweiss", "Dragon Blood"], False),
}

# ---------------------------------------------------------------- poisons
# name: (rarity, price, target, effect text, duration text, [three ingredients])
POISONS = {
    "Toxic Vial": ("common", 350, "one enemy", "Nature damage of 6 plus 2% of its max HP at the start of each of its turns", "3 turns", ["Mire Bulb", "Grubroot", "Dune Thistle"]),
    "Blinding Powder": ("common", 300, "one enemy", "-15% hit chance", "2 turns", ["Saltcrust", "Dune Thistle", "Stonebloom Lichen"]),
    "Corrosive Flask": ("uncommon", 1000, "one enemy", "-20% Armor", "3 turns", ["Mire Bulb", "Saltcrust", "Ember Moss"]),
    "Enfeebling Vial": ("uncommon", 1100, "one enemy", "-15% damage dealt (attacks and spells)", "3 turns", ["Nightshade", "Bogbean", "Bone Dust"]),
    "Viper Flask": ("uncommon", 1300, "one enemy", "Nature damage of 12 plus 3% of its max HP each turn, and -25% healing received", "3 turns", ["Nightshade", "Mire Bulb", "Bogbean"]),
    "Sluggard's Dust": ("uncommon", 1000, "one enemy", "-20% Speed", "3 turns", ["Bogbean", "Glowcap", "Frostpine Needle"]),
    "Paralytic Toxin": ("rare", 3500, "one enemy", "35% chance to stun it for 1 turn, and it loses 1 AP on its next turn", "1 turn", ["Lamia Venom", "Glowcap", "Imp Horn"]),
    "Hexing Vial": ("rare", 3200, "one enemy", "-20% resistance to every element", "3 turns", ["Abyssal Thorn", "Imp Horn", "Bone Dust"]),
    "Wyrmbane Venom": ("rare", 3800, "one enemy", "Nature damage of 20 plus 4% of its max HP each turn", "4 turns", ["Lamia Venom", "Nightshade", "Wyrm Scale"]),
    "Mindrot Toxin": ("rare", 3000, "one enemy", "-20% Intelligence and Wisdom (its spells weaken)", "3 turns", ["Vampire Ash", "Glowcap", "Nightshade"]),
    "Plague Bomb": ("epic", 8500, "every enemy", "Nature damage of 15 plus 2% of max HP each turn, and -15% Armor", "3 turns", ["Abyssal Thorn", "Gravecrown Moss", "Mire Bulb"]),
    "Deathcap Essence": ("epic", 9000, "one enemy", "Nature damage of 35 plus 5% of its max HP each turn, -50% healing received, -15% to every attribute", "3 turns", ["Lamia Venom", "Vampire Ash", "Gravecrown Moss"]),
}

COUNTS = {"potions": {"common": 8, "uncommon": 12, "rare": 10, "epic": 6, "legendary": 4},
          "poisons": {"common": 2, "uncommon": 4, "rare": 4, "epic": 2}}


def check():
    problems = []
    for kind, table in (("potions", POTIONS), ("poisons", POISONS)):
        got = Counter(entry[0] for entry in table.values())
        if dict(got) != COUNTS[kind]: problems.append(f"{kind} by rarity {dict(got)} != {COUNTS[kind]}")
    if len(INGREDIENTS) < 50: problems.append(f"only {len(INGREDIENTS)} ingredients")
    recipes = {**{k: v[4] for k, v in HEALTH_MANA.items()}, **{k: v[4] for k, v in POTIONS.items()}, **{k: v[5] for k, v in POISONS.items()}}
    seen = {}
    for name, parts in recipes.items():
        if len(parts) != 3 or len(set(parts)) != 3: problems.append(f"{name}: needs three different ingredients")
        for part in parts:
            if part not in INGREDIENTS: problems.append(f"{name}: unknown ingredient {part}")
        key = frozenset(parts)
        if key in seen: problems.append(f"{name} has the same recipe as {seen[key]}")
        seen[key] = name
    used = Counter(part for parts in recipes.values() for part in parts)
    for name in INGREDIENTS:
        if not used[name]: problems.append(f"{name} is in no recipe")
        rarity, price, zones, enemies = INGREDIENTS[name]
        if not zones and not enemies and not any(name in drops for drops in BOSS_DROPS.values()): problems.append(f"{name} has no source")
    for boss, drops in BOSS_DROPS.items():
        for drop in drops:
            if drop not in INGREDIENTS: problems.append(f"{boss}: unknown ingredient {drop}")
    # Every recipe's rarest ingredient is at most the product's rarity, and legendary products use one legendary ingredient.
    products = {**{k: v[0] for k, v in HEALTH_MANA.items()}, **{k: v[0] for k, v in POTIONS.items()}, **{k: v[0] for k, v in POISONS.items()}}
    for name, parts in recipes.items():
        top = max(RARITIES.index(INGREDIENTS[p][0]) for p in parts)
        if top > RARITIES.index(products[name]): problems.append(f"{name}: an ingredient rarer than the product")
        if sum(INGREDIENTS[p][0] == "legendary" for p in parts) > 1: problems.append(f"{name}: two legendary ingredients")
    # Brewing must pay: the ingredients cost less than the potion.
    prices = {**{k: v[1] for k, v in HEALTH_MANA.items()}, **{k: v[1] for k, v in POTIONS.items()}, **{k: v[1] for k, v in POISONS.items()}}
    for name, parts in recipes.items():
        cost = sum(INGREDIENTS[p][1] for p in parts)
        if cost >= prices[name] * 0.75: problems.append(f"{name}: ingredients {cost} against price {prices[name]}")
    return problems, recipes, used, prices


def write(path):
    problems, recipes, used, prices = check()
    uses = defaultdict(list)
    for name, parts in recipes.items():
        for part in parts: uses[part].append(name)
    lines = []
    out = lines.append
    out("# Task 9 draft: potions, poisons, ingredients and recipes")
    out("")
    out("Approved by the user on 2026-11-07 (P31, after the classes were reshaped, P30), and built into the old game the same")
    out("day; kept as the reference for the port. Made by `draft.py`, which checks the counts and recipes. The decisions")
    out("behind it are P1–P31 in `engine/Docs/Engine-Plan.md`, \"Decisions for task 9\". The old game's data now lives in")
    out("`old-game/objects/consumables.json` and `old-game/scripts/alchemy-data.js` (generated from this once); where the two")
    out("differ later, the game's data is the newer.")
    out("")
    out("**How to read it**")
    out("- Durations count the turns of whoever has the effect, in battle (the game's timed effects). A potion's buff is")
    out("  held once: drinking it again refreshes it; different potions add up (P7).")
    out("- A \"+X%\" to an attribute, Armor, Evasion or spell power adds to that stat's other percent bonuses, as the game's")
    out("  buff spells do. Damage and resistance changes add to the target's other changes of that kind.")
    out("- Prices are in silver. No item in the game has a carry limit, so neither do these.")
    out("- Any potion can be drunk or handed to an ally, 1 AP (P9). Health, Mana and cures also work outside battle (P6).")
    out("- A poison is thrown at an enemy, 1 AP. Poison damage is Nature damage, and Nature resistance lowers each tick (the")
    out("  Poisoner's own poison ignores resistance; thrown ones don't). On the 12 bosses every poison works at half")
    out("  strength: its damage, its reductions, its chances and its duration halved, rounded up (P8). The same poison")
    out("  again refreshes it.")
    out("")
    out(f"**Totals:** {len(POTIONS)} new potions, {len(POISONS)} poisons, {len(INGREDIENTS)} ingredients, "
        f"{len(recipes)} recipes ({len(HEALTH_MANA)} Health and Mana, {len(POTIONS)} new potions, {len(POISONS)} poisons).")
    out("")
    def table(title, rows, header):
        out(f"### {title}")
        out("")
        out("| " + " | ".join(header) + " |")
        out("|" + "---|" * len(header))
        for row in rows: out("| " + " | ".join(row) + " |")
        out("")
    out("## Potions")
    out("")
    for rarity in RARITIES:
        rows = [[f"**{n}**", v[2], v[3], f"{v[1]:,}", ", ".join(v[4]), "yes" if v[5] else ""] for n, v in POTIONS.items() if v[0] == rarity]
        table(f"{rarity.capitalize()} ({len(rows)})", rows, ["Potion", "Effect", "Lasts", "Price", "Recipe", "Outside battle"])
    out("## The Health and Mana Potions")
    out("")
    out("Their effects and prices stay as they are. Each Apothecary always sells your level band's pair and every weaker")
    out("pair, unlimited (P3). You start knowing the two Lesser recipes (P4).")
    out("")
    rows = [[f"**{n}**", v[0], v[2], f"{v[1]:,}", ", ".join(v[4])] for n, v in HEALTH_MANA.items()]
    table("Their recipes", rows, ["Potion", "Rarity", "Effect", "Price", "Recipe"])
    out("## Poisons")
    out("")
    for rarity in RARITIES[:4]:
        rows = [[f"**{n}**", v[2], v[3], v[4], f"{v[1]:,}", ", ".join(v[5])] for n, v in POISONS.items() if v[0] == rarity]
        table(f"{rarity.capitalize()} ({len(rows)})", rows, ["Poison", "Thrown at", "Effect", "Lasts", "Price", "Recipe"])
    out("## Ingredients")
    out("")
    out("Rarity sets how often they turn up, where, which enemies drop them, their price and which recipes use them.")
    out("\"Seldom\" means a low weight in that zone's forage table.")
    out("")
    for rarity in RARITIES:
        rows = []
        for n, v in INGREDIENTS.items():
            if v[0] != rarity: continue
            bosses = [b for b, d in BOSS_DROPS.items() if n in d]
            drops = ", ".join(v[3] + [b + " (boss)" for b in bosses]) or "—"
            rows.append([f"**{n}**", f"{v[1]:,}", "; ".join(v[2]) or "— (drops only)", drops, ", ".join(uses[n])])
        table(f"{rarity.capitalize()} ({len(rows)})", rows, ["Ingredient", "Price", "Foraged in", "Dropped by", "Used in"])
    out("## How ingredients are found")
    out("")
    out("- **Forage** (D; P10, P11): one hour of game time, and the zone's encounter chance rolled as for an hour of travel")
    out("  there. In every wild zone, rivers and lake shores included; not in towns, on roads, at sites or in the uncharted")
    out("  regions. Each zone has its own table: the ingredients listed above for it, common ones weighted 10, uncommon 5,")
    out("  rare 2, epic 1, legendary 0.5, \"seldom\" halved; rivers add Riverreed and Clearwater Moss to any zone they cross.")
    out("  A forage finds something 45% of the time (plus the Alchemist's bonus): one of the table's ingredients, 1–2 of a")
    out("  common one, otherwise 1. Otherwise: \"You search for an hour but find nothing useful.\"")
    out("- **Enemies** (P12): each enemy above drops one of its ingredients 25% of the time (if it has several, picked by")
    out("  the same weights). Empowered and Sparkling enemies: 50%, and two rolls. Bosses: always one of their two, and a")
    out("  35% chance of the other. The Luck bonus doesn't apply (as for every non-masterwork drop). Every one of the")
    out("  174 enemies has at least one ingredient, except the Phylactery of Ool (part of Ool's own fight).")
    out("- **The Apothecary** (P13): every common ingredient always (10 of each, refilled at each restock), and the region's")
    out("  own uncommon and rarer ones: castle cities 5 kinds, towns 3, 2–5 of each (1–2 of an epic one, 1 of a legendary).")
    out("  Ingredient prices are fixed and don't rise when a Collect quest asks for one.")
    out("")
    out("## The Apothecary")
    out("")
    out("- In every settlement, its button (O) beside the General Store's (P26). Banners: a grand castle-city shop and a")
    out("  small town one (placeholder art, P2). Four menus, switched without leaving: Potions, Ingredients, Brewing, Recipes.")
    out("- **Potions:** your band's Health and Mana pairs and the weaker ones, unlimited (P3); then a random selection of the")
    out("  new potions and poisons, weighted toward your level band's rarity like the General Store's goods: castle cities")
    out("  8 kinds, towns 5, castle 3–6 of each, town 2–4. Restocked every 6 game hours with the other shops.")
    out("- **Recipes:** three you don't know, weighted toward your level band's rarity (P16), each 10× its product's price")
    out("  (P14); the same three until the next 6-hour restock (P15); fewer when fewer remain; bought ones are never offered")
    out("  again.")
    out("- **Selling:** it buys potions, poisons and ingredients at half price (P28). The General Store stops selling and")
    out("  buying potions.")
    out("- **Brewing:** a cauldron in the middle, three ingredient boxes above it, a Brew button, the result shown on the")
    out("  cauldron, your known recipes listed on the right. Picking a recipe fills the boxes when you have the ingredients;")
    out("  the order doesn't matter; a box can be emptied or swapped. Brew checks that the three are filled, that you have")
    out("  them, and that they make a known recipe; only then are the three used up. Brewing costs nothing else. The")
    out("  potion appears on the cauldron (a bubbling, glowing brew), and Collect puts it in your pack.")
    out("")
    out("## The classes")
    out("")
    out("Built like the other classes (P30): one gain per level, a passive that is the class's identity at Level 1, small")
    out("fixed bonuses on most levels, and a skill as the capstone.")
    out("")
    out("### Alchemist (5 levels; Level 20 and 10 potions brewed, P18)")
    out("")
    out("| Level | Gains |")
    out("|---|---|")
    out("| 1 | Gain **Ingredient Lore**: an ingredient's details list every potion and poison it goes into, by name and rarity. A recipe you don't know shows only that it exists, not its other ingredients. |")
    out("| 2 | Forage finds something 10% more often. |")
    out("| 3 | Wisdom permanent bonus +5%. |")
    out("| 4 | Forage finds something 10% more often (+20% in all); a find of a common or uncommon ingredient brings one more. |")
    out("| 5 | Unlock **Keen Harvest** (outside battle, once a day): your next forage is sure to find something, and rare and rarer ingredients weigh double in it. |")
    out("")
    out("### Assimilator (7 levels; Level 10 and 25 ingredients foraged, P19)")
    out("")
    out("| Level | Gains |")
    out("|---|---|")
    out("| 1 | Gain **Raw Assimilation**: in battle, eat an ingredient from the item menu (1 AP) for a buff made from the potions it goes into, a third as strong as they are, lasting 2 turns (common), 3 (uncommon, rare) or 4 (epic, legendary). |")
    out("| 2 | Vitality permanent bonus +5%. |")
    out("| 3 | Nature resistance +10%. |")
    out("| 4 | Raw Assimilation buffs are half as strong as their potions and last 1 turn longer. |")
    out("| 5 | Eating an ingredient has a 25% chance not to use it up. |")
    out("| 6 | Raw Assimilation buffs are two-thirds as strong as their potions and carry their special effects too (AP, rising again, immunities, cures). |")
    out("| 7 | Unlock **Devour** (in battle, once per battle): eat an ingredient without spending AP; its buff lasts twice as long. |")
    out("")
    out("**What an eaten ingredient gives** (Raw Assimilation): the buffs of the potions it goes into, effects of the same kind")
    out("(two Strength bonuses, two resistances to one element) counted once at the strongest, the two strongest kinds kept,")
    out("at the level's strength. Healing and Mana from restoring potions come at the start of each turn of the buff. An")
    out("ingredient only in poisons puts the poison's effect on your weapon hits instead. The same ingredient again refreshes")
    out("its buff; different ingredients add up (P21).")
    out("")
    out("Example: **Glass Sand** is in the Potion of Precision (+10% crit chance, uncommon), Duelist's Draught (+30% crit")
    out("damage and +5% crit chance, rare) and the Piercing Draught (20% armor pierce, rare). Crit chance counts once, at")
    out("+10%; the two strongest kinds are the rare ones, crit damage +30% and armor pierce 20%: at Level 1 +10% crit damage")
    out("and 7% armor pierce for 3 turns; at Level 6 +20% and 13% for 4 turns. (Corrected 2026-11-07: the example first left")
    out("out the Piercing Draught; the rule is unchanged.)")
    out("")
    out("## Collect quests (P22–P25)")
    out("")
    out("- An ingredient found in the guild's region or a neighbouring one, of your level band's rarity or below.")
    out("- Quantity: 6 of a common one, 4 uncommon, 3 rare, 2 epic, 1 legendary.")
    out("- Pays like the other quests at the quest's level, by the ingredient's rarity: common ×1.0, uncommon ×1.1, rare")
    out("  ×1.2, epic and legendary ×1.35. Never as much as Cull (×1.55) or Hunt (×1.8).")
    out("- Progress shows the copies in your pack against the quantity. Handed in at the guild that offered it, at its")
    out("  counter; the copies leave your pack and the silver and reputation are paid then. Any copies count (P24). With")
    out("  one standard quest at a time, a delivery can only ever count for one quest.")
    out("")
    if problems:
        out("## Problems")
        for p in problems: out(f"- {p}")
    open(path, "w").write("\n".join(lines) + "\n")
    return problems


if __name__ == "__main__":
    problems = write(os.path.join(os.path.dirname(os.path.abspath(__file__)), "draft.md"))
    for p in problems: print("PROBLEM", p)
    print(f"{len(POTIONS)} potions, {len(POISONS)} poisons, {len(INGREDIENTS)} ingredients; {len(problems)} problems")
    sys.exit(1 if problems else 0)

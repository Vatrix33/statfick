# Task 9 draft: potions, poisons, ingredients and recipes

For review (P5). Made by `draft.py`, which checks the counts and recipes. The decisions behind it are P1–P29 in
`engine/Docs/Engine-Plan.md`, "Decisions for task 9".

**How to read it**
- Durations count the turns of whoever has the effect, in battle (the game's timed effects). A potion's buff is
  held once: drinking it again refreshes it; different potions add up (P7).
- A "+X%" to an attribute, Armor, Evasion or spell power adds to that stat's other percent bonuses, as the game's
  buff spells do. Damage and resistance changes add to the target's other changes of that kind.
- Prices are in silver. No item in the game has a carry limit, so neither do these.
- Any potion can be drunk or handed to an ally, 1 AP (P9). Health, Mana and cures also work outside battle (P6).
- A poison is thrown at an enemy, 1 AP. Poison damage is Nature damage (Nature resistance lowers it, as for
  the Poisoner's poison). On the 12 bosses every poison works at half strength: its damage, its
  reductions, its chances and its duration halved, rounded up (P8). The same poison again refreshes it.

**Totals:** 40 new potions, 12 poisons, 56 ingredients, 62 recipes (10 Health and Mana, 40 new potions, 12 poisons).

## Potions

### Common (8)

| Potion | Effect | Lasts | Price | Recipe | Outside battle |
|---|---|---|---|---|---|
| **Draught of Vigor** | +10% Strength | 3 turns | 350 | Wolf Fang, Bear Fat, Meadowmint |  |
| **Mindspark Tonic** | +10% spell power | 3 turns | 400 | Spirit Dew, Clearwater Moss, Grubroot |  |
| **Barkskin Tonic** | +15% Armor | 3 turns | 350 | Oakbark, Stonebloom Lichen, Bear Fat |  |
| **Swiftwind Draught** | +10% Speed (your turns come sooner) | 3 turns | 400 | Griffon Down, Harpy Feather, Meadowmint |  |
| **Hawkeye Drops** | +10% hit chance with weapons | 3 turns | 350 | Harpy Feather, Saltcrust, Redcap Berry |  |
| **Antidote** | Removes one negative effect (a poison first, if any) | instant | 300 | Riverreed, Saltcrust, Mire Bulb | yes |
| **Mending Draught** | Heals 4% of max HP at the start of each of your turns | 4 turns | 450 | Meadowmint, Oakbark, Clearwater Moss |  |
| **Emberward Tonic** | +20% Fire resistance | 4 turns | 400 | Saltcrust, Dune Thistle, Clearwater Moss |  |

### Uncommon (12)

| Potion | Effect | Lasts | Price | Recipe | Outside battle |
|---|---|---|---|---|---|
| **Elixir of Might** | +20% Strength | 3 turns | 1,100 | Wolf Fang, Troll Blood, Warpaint Clay |  |
| **Arcane Draught** | +20% spell power | 3 turns | 1,200 | Spirit Dew, Elemental Essence, Glowcap |  |
| **Stoneskin Potion** | -15% weapon damage taken | 3 turns | 1,200 | Stonebloom Lichen, Treant Heartwood, Bear Fat |  |
| **Spellward Potion** | -15% spell damage taken | 3 turns | 1,200 | Elemental Essence, Saltcrust, Satyr's Laurel |  |
| **Catseye Potion** | +15% Evasion | 3 turns | 1,000 | Griffon Down, Harpy Feather, Jungle Orchid |  |
| **Potion of Precision** | +10% critical hit chance | 3 turns | 1,300 | Harpy Feather, Glass Sand, Wolf Fang |  |
| **Tidal Ward** | +25% Water and Ice resistance | 4 turns | 1,000 | Merfolk Scale, Frostpine Needle, Clearwater Moss |  |
| **Storm Ward** | +25% Lightning and Wind resistance | 4 turns | 1,000 | Stormcap, Griffon Down, Stonebloom Lichen |  |
| **Grave Ward** | +25% Dark and Death resistance | 4 turns | 1,100 | Bone Dust, Saltcrust, Satyr's Laurel |  |
| **Clarity Draught** | Removes up to two negative effects | instant | 900 | Riverreed, Saltcrust, Sunfruit | yes |
| **Manaflow Tonic** | Restores 5% of max Mana at the start of each of your turns | 4 turns | 1,100 | Clearwater Moss, Elemental Essence, Jungle Orchid |  |
| **Bloodroot Tonic** | +20% healing received | 4 turns | 1,000 | Redcap Berry, Dryad Sap, Bogbean |  |

### Rare (10)

| Potion | Effect | Lasts | Price | Recipe | Outside battle |
|---|---|---|---|---|---|
| **Berserker's Brew** | +25% weapon damage dealt, but -10% Armor | 3 turns | 3,000 | Troll Blood, Warpaint Clay, Imp Horn |  |
| **Sorcerer's Philtre** | +30% spell power | 3 turns | 3,500 | Elemental Essence, Glowcap, Thunderglass |  |
| **Potion of Steadfastness** | Immune to stuns and AP loss | 4 turns | 3,000 | Stonebloom Lichen, Treant Heartwood, Giant Marrow |  |
| **Duelist's Draught** | +30% critical damage and +5% critical hit chance | 3 turns | 3,500 | Harpy Feather, Glass Sand, Stormfeather |  |
| **Wraithstep Elixir** | +25% Evasion, and spells aimed at you miss 15% of the time | 3 turns | 3,500 | Nightshade, Vampire Ash, Griffon Down |  |
| **Bulwark Potion** | A barrier of 15% of max HP that takes damage first | 3 turns (what's left then fades) | 3,000 | Treant Heartwood, Obsidian Flake, Stonebloom Lichen |  |
| **Elixir of Renewal** | Heals 7% of max HP at the start of each of your turns, and +15% healing received | 4 turns | 3,500 | Dryad Sap, Troll Blood, Old-Growth Amber |  |
| **Prismatic Ward** | +20% resistance to every element | 3 turns | 4,000 | Elemental Essence, Ember Moss, Rime Lotus |  |
| **Piercing Draught** | Weapon attacks ignore 20% of the target's Armor | 3 turns | 3,000 | Wyrm Scale, Glass Sand, Warpaint Clay |  |
| **Purging Elixir** | Removes every negative effect; none can take hold until your next turn | instant, then until your next turn | 2,800 | Saltcrust, Sunfruit, Tomb Myrrh | yes |

### Epic (6)

| Potion | Effect | Lasts | Price | Recipe | Outside battle |
|---|---|---|---|---|---|
| **Titan's Draught** | +20% to every attribute | 3 turns | 9,000 | Giant Marrow, Troll Blood, Dragon Blood |  |
| **Elixir of the Unbroken** | Immune to every negative effect (those you have stay) | 3 turns | 8,000 | Tomb Myrrh, Obsidian Flake, Gravecrown Moss |  |
| **Draught of Haste** | +1 AP at the start of each of your turns | 3 turns | 9,000 | Stormfeather, Thunderglass, Cloudbreak Edelweiss |  |
| **Giantsbane Draught** | +30% damage dealt to bosses and Empowered or Sparkling enemies | 4 turns | 8,500 | Wyrm Scale, Blood Glass, Brimstone Heart |  |
| **Phoenix Tonic** | If you fall, you rise again once with 30% of max HP | 4 turns | 9,500 | Phoenix Ash, Ember Moss, Sunfruit |  |
| **Archmage's Elixir** | +30% spell power, and spells cost 25% less Mana | 3 turns | 9,000 | Elemental Essence, Siren Pearl, Moonshade Lily |  |

### Legendary (4)

| Potion | Effect | Lasts | Price | Recipe | Outside battle |
|---|---|---|---|---|---|
| **Elixir of Ascendance** | +35% to every attribute and +15% damage dealt; afterwards -15% to every attribute for 2 turns | 3 turns | 24,000 | Dragon Blood, Giant Marrow, Sunheart Ember |  |
| **Undying Draught** | You can't fall below 1 HP; when it ends you heal 20% of max HP | 2 turns | 22,000 | Phoenix Ash, Troll Blood, Soulglass Shard |  |
| **Panacea** | Heals 40% of max HP and Mana, removes every negative effect, and none can take hold | instant, immunity 2 turns | 20,000 | Heartseed, Dryad Sap, Moonshade Lily | yes |
| **Elixir of Kinship** | Every ally heals 25% of max HP and gains +15% to every attribute | 3 turns | 25,000 | Starfall Dust, Cloudbreak Edelweiss, Dragon Blood |  |

## The Health and Mana Potions

Their effects and prices stay as they are. Each Apothecary always sells your level band's pair and every weaker
pair, unlimited (P3). You start knowing the two Lesser recipes (P4).

### Their recipes

| Potion | Rarity | Effect | Price | Recipe |
|---|---|---|---|---|
| **Potion of Lesser Health** | common | Heals 25 HP plus 10% of max HP (unchanged) | 450 | Meadowmint, Redcap Berry, Riverreed |
| **Potion of Health** | uncommon | Heals 50 HP plus 15% of max HP (unchanged) | 1,200 | Meadowmint, Redcap Berry, Dryad Sap |
| **Potion of Greater Health** | rare | Heals 75 HP plus 15% of max HP (unchanged) | 3,500 | Redcap Berry, Troll Blood, Old-Growth Amber |
| **Potion of Superior Health** | epic | Heals 100 HP plus 20% of max HP (unchanged) | 8,000 | Troll Blood, Old-Growth Amber, Phoenix Ash |
| **Potion of Ultimate Health** | legendary | Heals 200 HP plus 25% of max HP (unchanged) | 18,000 | Troll Blood, Phoenix Ash, Heartseed |
| **Potion of Lesser Mana** | common | Restores 10 Mana plus 5% of max Mana (unchanged) | 400 | Spirit Dew, Clearwater Moss, Riverreed |
| **Potion of Mana** | uncommon | Restores 20 Mana plus 10% of max Mana (unchanged) | 1,000 | Spirit Dew, Clearwater Moss, Elemental Essence |
| **Potion of Greater Mana** | rare | Restores 30 Mana plus 15% of max Mana (unchanged) | 3,000 | Clearwater Moss, Elemental Essence, Siren Pearl |
| **Potion of Superior Mana** | epic | Restores 40 Mana plus 20% of max Mana (unchanged) | 7,000 | Elemental Essence, Siren Pearl, Cloudbreak Edelweiss |
| **Potion of Ultimate Mana** | legendary | Restores 50 Mana plus 20% of max Mana (unchanged) | 16,000 | Elemental Essence, Cloudbreak Edelweiss, Starfall Dust |

## Poisons

### Common (2)

| Poison | Thrown at | Effect | Lasts | Price | Recipe |
|---|---|---|---|---|---|
| **Toxic Vial** | one enemy | Nature damage of 6 plus 2% of its max HP at the start of each of its turns | 3 turns | 350 | Mire Bulb, Grubroot, Dune Thistle |
| **Blinding Powder** | one enemy | -15% hit chance | 2 turns | 300 | Saltcrust, Dune Thistle, Stonebloom Lichen |

### Uncommon (4)

| Poison | Thrown at | Effect | Lasts | Price | Recipe |
|---|---|---|---|---|---|
| **Corrosive Flask** | one enemy | -20% Armor | 3 turns | 1,000 | Mire Bulb, Saltcrust, Ember Moss |
| **Enfeebling Vial** | one enemy | -15% damage dealt (attacks and spells) | 3 turns | 1,100 | Nightshade, Bogbean, Bone Dust |
| **Viper Flask** | one enemy | Nature damage of 12 plus 3% of its max HP each turn, and -25% healing received | 3 turns | 1,300 | Nightshade, Mire Bulb, Bogbean |
| **Sluggard's Dust** | one enemy | -20% Speed | 3 turns | 1,000 | Bogbean, Glowcap, Frostpine Needle |

### Rare (4)

| Poison | Thrown at | Effect | Lasts | Price | Recipe |
|---|---|---|---|---|---|
| **Paralytic Toxin** | one enemy | 35% chance to stun it for 1 turn, and it loses 1 AP on its next turn | 1 turn | 3,500 | Lamia Venom, Glowcap, Imp Horn |
| **Hexing Vial** | one enemy | -20% resistance to every element | 3 turns | 3,200 | Abyssal Thorn, Imp Horn, Bone Dust |
| **Wyrmbane Venom** | one enemy | Nature damage of 20 plus 4% of its max HP each turn | 4 turns | 3,800 | Lamia Venom, Nightshade, Wyrm Scale |
| **Mindrot Toxin** | one enemy | -20% Intelligence and Wisdom (its spells weaken) | 3 turns | 3,000 | Vampire Ash, Glowcap, Nightshade |

### Epic (2)

| Poison | Thrown at | Effect | Lasts | Price | Recipe |
|---|---|---|---|---|---|
| **Plague Bomb** | every enemy | Nature damage of 15 plus 2% of max HP each turn, and -15% Armor | 3 turns | 8,500 | Abyssal Thorn, Gravecrown Moss, Mire Bulb |
| **Deathcap Essence** | one enemy | Nature damage of 35 plus 5% of its max HP each turn, -50% healing received, -15% to every attribute | 3 turns | 9,000 | Lamia Venom, Vampire Ash, Gravecrown Moss |

## Ingredients

Rarity sets how often they turn up, where, which enemies drop them, their price and which recipes use them.
"Seldom" means a low weight in that zone's forage table.

### Common (15)

| Ingredient | Price | Foraged in | Dropped by | Used in |
|---|---|---|---|---|
| **Meadowmint** | 40 | Alder Vale: Plains; Stoneveil Foothills: Plains, Hill-Plains; Verdant Reach: Plains; Sunward Coast: Coastal Plains, Windswept Plains; Stormwake Hills: Rainwashed Downs | Bandits (all four) | Potion of Lesser Health, Potion of Health, Draught of Vigor, Swiftwind Draught, Mending Draught |
| **Redcap Berry** | 40 | Alder Vale: Forest; Verdant Reach: Outer Forest, Forest | Juvenile Bear, Adult Bear, Bandit Archer | Potion of Lesser Health, Potion of Health, Potion of Greater Health, Hawkeye Drops, Bloodroot Tonic |
| **Riverreed** | 40 | every region's rivers; Mirrormere Basin: Lake Shore, Reed Marsh; Gloamfen Marsh: Reedwater | Water Spirit | Potion of Lesser Health, Potion of Lesser Mana, Antidote, Clarity Draught |
| **Clearwater Moss** | 50 | every region's rivers; Mirrormere Basin: Lake Shore; Saffron Expanse: Oasis Grove | Water Spirit, Greater Water Spirit | Potion of Lesser Mana, Potion of Mana, Potion of Greater Mana, Mindspark Tonic, Mending Draught, Emberward Tonic, Tidal Ward, Manaflow Tonic |
| **Spirit Dew** | 60 | Alder Vale: Deep Forest; Verdant Reach: Deep Forest | Nature Spirit, Harvest Wisp, Fire Spirit | Potion of Lesser Mana, Potion of Mana, Mindspark Tonic, Arcane Draught |
| **Wolf Fang** | 50 | — (drops only) | Juvenile Wolf, Adult Wolf, Highland Wolf, Dire Wolf, Corrupted Wolf | Draught of Vigor, Elixir of Might, Potion of Precision |
| **Bear Fat** | 50 | — (drops only) | Juvenile Bear, Adult Bear, Mountain Bear, Corrupted Bear | Draught of Vigor, Barkskin Tonic, Stoneskin Potion |
| **Griffon Down** | 60 | Stoneveil Foothills: Hill-Plains; High Stoneveil: Lower Slopes | Juvenile Griffon, Adult Griffon, Highland Griffon, Corrupted Griffon | Swiftwind Draught, Catseye Potion, Storm Ward, Wraithstep Elixir |
| **Harpy Feather** | 60 | High Stoneveil: Cliffside; Mirrormere Basin: Windcut Bluffs | Harpy Skirmisher, Harpy Huntress, Harpy Matriarch | Swiftwind Draught, Hawkeye Drops, Catseye Potion, Potion of Precision, Duelist's Draught |
| **Stonebloom Lichen** | 50 | Stoneveil Foothills: Mountainfoot; High Stoneveil: Lower Slopes; Gloamfen Marsh: Rocky Rise; Bloodstone Coast: Western Crags | Earth Spirit, Greater Earth Spirit | Barkskin Tonic, Stoneskin Potion, Storm Ward, Potion of Steadfastness, Bulwark Potion, Blinding Powder |
| **Mire Bulb** | 40 | Gloamfen Marsh: Open Mire, Reedwater; Stormwake Hills: Floodplain; Sunward Coast: Salt Marsh | Goblin Raider | Antidote, Toxic Vial, Corrosive Flask, Viper Flask, Plague Bomb |
| **Saltcrust** | 40 | Saffron Expanse: Salt Pans; Sunward Coast: Salt Marsh, Beach; Cinder Isle: Black Sand Shore | Coastal Corsair | Hawkeye Drops, Antidote, Emberward Tonic, Spellward Potion, Grave Ward, Clarity Draught, Purging Elixir, Blinding Powder, Corrosive Flask |
| **Dune Thistle** | 50 | Mirrormere Basin: Dry Steppe, Desert Fringe; Saffron Expanse: Sunburnt Flats; Crimson Divide: Caravan Dunes; Deep Desert: Outer Wastes; Sunward Coast: Dunes | Dune Raider, Caravan Bandit | Emberward Tonic, Toxic Vial, Blinding Powder |
| **Grubroot** | 40 | Alder Vale: Plains; Stoneveil Foothills: Plains | Goblin, Goblin Archer, Goblin Warrior, Goblin Boss, Highland Goblin, Goblin Warlord, Orc, Forest Orc | Mindspark Tonic, Toxic Vial |
| **Oakbark** | 50 | Alder Vale: Forest, Deep Forest; Verdant Reach: Forest | Young Treant, Dryad Warden | Barkskin Tonic, Mending Draught |

### Uncommon (18)

| Ingredient | Price | Foraged in | Dropped by | Used in |
|---|---|---|---|---|
| **Dryad Sap** | 200 | Verdant Reach: Deep Forest | Dryad Warden, Dryad Matriarch, Blossom Sprite, Grove Matriarch, Juila the Grand Forest Matriarch (boss) | Potion of Health, Bloodroot Tonic, Elixir of Renewal, Panacea |
| **Troll Blood** | 250 | — (drops only) | Hill Troll, Elder Troll, Troll Shieldbearer, Troll War-drummer, Thunder Troll, Bog Troll, Crag Troll, Zik Lun, the armored Hulk (boss) | Potion of Greater Health, Potion of Superior Health, Potion of Ultimate Health, Elixir of Might, Berserker's Brew, Elixir of Renewal, Titan's Draught, Undying Draught |
| **Treant Heartwood** | 220 | Verdant Reach: Deep Forest; Alder Vale: Deep Forest | Young Treant, Elder Treant, Jungle Treant | Stoneskin Potion, Potion of Steadfastness, Bulwark Potion |
| **Elemental Essence** | 250 | — (drops only) | the six Elementals of Level 25, Greater Water, Earth and Nature Spirits, Raging Water Spirit, Radiant Sentinel, Hemoghost, Sunhorn Stag | Potion of Mana, Potion of Greater Mana, Potion of Superior Mana, Potion of Ultimate Mana, Arcane Draught, Spellward Potion, Manaflow Tonic, Sorcerer's Philtre, Prismatic Ward, Archmage's Elixir |
| **Stormcap** | 200 | Stormwake Hills: Thunder Hills, Stormcrest, Rainwashed Downs | Storm Harpy, Lightning Elemental, Rainbound Marauder | Storm Ward |
| **Merfolk Scale** | 220 | Sunward Coast: Beach; Bloodstone Coast: Blood Coast | Merfolk Spearfisher, Reed Merfolk, Merfolk Reef Guard, Merfolk Tidecaller, Merfolk Steamcaller, Deep Sea King (boss) | Tidal Ward |
| **Warpaint Clay** | 180 | Stormwake Hills: Floodplain | Orc Berserker, Orc Chieftain, Apostle of Talos, Tankina, Disciple of Talos (boss) | Elixir of Might, Berserker's Brew, Piercing Draught |
| **Satyr's Laurel** | 180 | Verdant Reach: Forest | Satyr Scout, Satyr Warrior, Satyr Grovekeeper | Spellward Potion, Grave Ward |
| **Bogbean** | 180 | Gloamfen Marsh: Deep Mire; Duskpine Forest: Frostbound Bog; Mirrormere Basin: Reed Marsh | Mire Stalker, Bog Troll | Bloodroot Tonic, Enfeebling Vial, Viper Flask, Sluggard's Dust |
| **Glowcap** | 220 | Verdigris Jungle: Dense Jungle; Duskpine Forest: Shadowroot Hollow; Gloamfen Marsh: Deep Mire | Fen Spirit, Canopy Stalker | Arcane Draught, Sorcerer's Philtre, Sluggard's Dust, Paralytic Toxin, Mindrot Toxin |
| **Jungle Orchid** | 200 | Verdigris Jungle: Jungle Verge, Outer Jungle, High Canopy | Canopy Stalker | Catseye Potion, Manaflow Tonic |
| **Nightshade** | 200 | Alder Vale: Deep Forest; Duskpine Forest: Blackpine Woods; Ancient Battlefield: Mass Graves | Void Imp, Undead Thrall | Wraithstep Elixir, Enfeebling Vial, Viper Flask, Wyrmbane Venom, Mindrot Toxin |
| **Sunfruit** | 200 | Saffron Expanse: Oasis Grove, Sunburnt Flats | Oasis Guardian | Clarity Draught, Purging Elixir, Phoenix Tonic |
| **Glass Sand** | 200 | Saffron Expanse: Glass Dunes, Shifting Dunes; Deep Desert: Howling Dunes | Sand Giant, Sirocco Harpy | Potion of Precision, Duelist's Draught, Piercing Draught |
| **Frostpine Needle** | 220 | Duskpine Forest: Frostpine Fringe, Blackpine Woods; High Stoneveil: Frozen Heights | Rime Wolf, Ice Elemental, Duskpine Wolf | Tidal Ward, Sluggard's Dust |
| **Ember Moss** | 220 | Cinder Isle: Ember Scrub, Ashen Slope, Black Sand Shore | Fire Spirit, Fire Elemental, Ash Harpy, Lava Stalker | Prismatic Ward, Phoenix Tonic, Corrosive Flask |
| **Bone Dust** | 180 | Ancient Battlefield: Bone Pits, Scavenger Verge; Deep Desert: Buried Necropolis | Undead Thrall, Undead Hulk, Gravebound Legionnaire, Battlefield Scavenger | Grave Ward, Enfeebling Vial, Hexing Vial |
| **Imp Horn** | 220 | Gloamfen Marsh: Abyssal Rift | Lesser Imp, Void Imp, Void Stalker, Abyssal Imp, Great Imp Ji'ruan (boss) | Berserker's Brew, Paralytic Toxin, Hexing Vial |

### Rare (13)

| Ingredient | Price | Foraged in | Dropped by | Used in |
|---|---|---|---|---|
| **Wyrm Scale** | 800 | High Stoneveil: Dragonspine | Mountain Wyrm, Shore Wyrm, Cliff Wyrm, Canopy Wyrm, Ember Drake, Nygheir the Wounded (boss) | Piercing Draught, Giantsbane Draught, Wyrmbane Venom |
| **Giant Marrow** | 700 | Bloodstone Coast: Giant Rock Spires | Young Giant, Sand Giant, Stone Giant, Peak Giant, Zik Lun, the armored Hulk (boss) | Potion of Steadfastness, Titan's Draught, Elixir of Ascendance |
| **Lamia Venom** | 700 | Crimson Divide: Sunscorched Escarpment | Sunscale Lamia, Mossback Lamia, Mountain Lamia, Corrupted Mossback Lamia | Paralytic Toxin, Wyrmbane Venom, Deathcap Essence |
| **Obsidian Flake** | 600 | Deep Desert: Obsidian Flats; Cinder Isle: Lava Fields | Magma Elemental, Vulcanic Ram, Steelguard Golem, Ornn, God of the Forge (boss) | Bulwark Potion, Elixir of the Unbroken |
| **Siren Pearl** | 900 | Sunward Coast: Beach; Bloodstone Coast: Blood Coast | Tide Siren, Merfolk Tidecaller, Deep Sea King (boss) | Potion of Greater Mana, Potion of Superior Mana, Archmage's Elixir |
| **Rime Lotus** | 800 | High Stoneveil: Frozen Heights; Duskpine Forest: Frostbound Bog; Stoneveil Summit: Windcut Ascent | Frost Elemental, Boreal Direwolf | Prismatic Ward |
| **Thunderglass** | 800 | Stormwake Hills: Stormcrest; Saffron Expanse: Glass Dunes | Lightning Elemental, Storm Elemental, Thunder Troll, Thunder Boar Gorthok, Tankina, Disciple of Talos (boss) | Sorcerer's Philtre, Draught of Haste |
| **Tomb Myrrh** | 700 | Deep Desert: Pyramid Sanctum, Buried Necropolis | Tomb Guardian, Pyramid Sentinel, Dust Revenant, Sand Wraith | Purging Elixir, Elixir of the Unbroken |
| **Old-Growth Amber** | 800 | Verdigris Jungle: Old Growth, High Canopy | Jungle Core, Jungle Treant, Canopy Stalker, Juila the Grand Forest Matriarch (boss), Gaian, Avatar of the Land (boss) | Potion of Greater Health, Potion of Superior Health, Elixir of Renewal |
| **Vampire Ash** | 800 | Duskpine Forest: Shadowroot Hollow | Bloodsand Vampire, Vampire Stalker, Vampire Hexer, Crimson Vampire Knight, Nightwood Vampire, Blood Exile | Wraithstep Elixir, Mindrot Toxin, Deathcap Essence |
| **Abyssal Thorn** | 700 | Gloamfen Marsh: Abyssal Rift | Rift Fiend, Abyss Witch, Corrupted Bog Troll, Corrupted Mire Stalker | Hexing Vial, Plague Bomb |
| **Blood Glass** | 800 | Crimson Divide: Bloodglass Flats, Crimson Dunes; Bloodstone Coast: Sanguine Wastes | Bloodstone Reaver, Bloodrock Sentinel, Sanguine Oracle | Giantsbane Draught |
| **Stormfeather** | 700 | High Stoneveil: High Pass; Mirrormere Basin: Windcut Bluffs | Great Griffon, High Griffon, Harpy Sentinel, Wind Elemental, Great Imp Ji'ruan (boss) | Duelist's Draught, Draught of Haste |

### Epic (6)

| Ingredient | Price | Foraged in | Dropped by | Used in |
|---|---|---|---|---|
| **Dragon Blood** | 2,500 | Stoneveil Summit: Dragon Eyries | the twelve colour Dragons of Level 60, Nygheir the Wounded (boss), Anatemis, the White Star (boss) | Titan's Draught, Elixir of Ascendance, Elixir of Kinship |
| **Phoenix Ash** | 2,500 | Cinder Isle: Caldera (seldom); Crimson Divide: Heart of the Red Waste | Magma Elemental (seldom), Ornn, God of the Forge (boss) | Potion of Superior Health, Potion of Ultimate Health, Phoenix Tonic, Undying Draught |
| **Cloudbreak Edelweiss** | 2,200 | Stoneveil Summit: Cloudbreak Ridge, Giant Steps, Windcut Ascent; High Stoneveil: High Pass (seldom) | Peak Giant, Glacier Giant, Stormforged Giant | Potion of Superior Mana, Potion of Ultimate Mana, Draught of Haste, Elixir of Kinship |
| **Brimstone Heart** | 2,500 | Crimson Divide: Heart of the Red Waste | the three Great Devil Servants, Archdevil Saffron (boss) | Giantsbane Draught |
| **Gravecrown Moss** | 2,000 | Ancient Battlefield: Mass Graves, Silent Westfield, Bloodied Trenches; Duskpine Forest: Shadowroot Hollow (seldom) | Gravebound Legionnaire, Duskpine Wraith, Nocturne Castellan, Ancient Lich Ool (boss) | Elixir of the Unbroken, Plague Bomb, Deathcap Essence |
| **Moonshade Lily** | 2,200 | Duskpine Forest: Shadowroot Hollow, Deep Boreal | Nightwood Vampire, Pale Monarch, Rogue Vampire Hunter, Ur-Mat, the False Dreamer (boss) | Archmage's Elixir, Panacea |

### Legendary (4)

| Ingredient | Price | Foraged in | Dropped by | Used in |
|---|---|---|---|---|
| **Starfall Dust** | 8,000 | Stoneveil Summit: Frozen Crown, Dragon Eyries; High Stoneveil: Dragonspine (seldom) | Ancient Frost Dragon, Tempest Dragon, Crownfire Dragon, Anatemis, the White Star (boss) | Potion of Ultimate Mana, Elixir of Kinship |
| **Heartseed** | 7,000 | Verdigris Jungle: Old Growth (seldom) | Gaian, Avatar of the Land (boss) | Potion of Ultimate Health, Panacea |
| **Soulglass Shard** | 7,000 | Duskpine Forest: Shadowroot Hollow (seldom); Ancient Battlefield: Silent Westfield | Pale Monarch, Ancient Lich Ool (boss), Ur-Mat, the False Dreamer (boss) | Undying Draught |
| **Sunheart Ember** | 7,500 | Crimson Divide: Heart of the Red Waste (seldom) | Crownfire Dragon, Archdevil Saffron (boss) | Elixir of Ascendance |

## How ingredients are found

- **Forage** (D; P10, P11): one hour of game time, and the zone's encounter chance rolled as for an hour of travel
  there. In every wild zone, rivers and lake shores included; not in towns, on roads, at sites or in the uncharted
  regions. Each zone has its own table: the ingredients listed above for it, common ones weighted 10, uncommon 5,
  rare 2, epic 1, legendary 0.5, "seldom" halved; rivers add Riverreed and Clearwater Moss to any zone they cross.
  A forage finds something 45% of the time (plus the Alchemist's bonus): one of the table's ingredients, 1–2 of a
  common one, otherwise 1. Otherwise: "You search for an hour but find nothing useful."
- **Enemies** (P12): each enemy above drops one of its ingredients 25% of the time (if it has several, picked by
  the same weights). Empowered and Sparkling enemies: 50%, and two rolls. Bosses: always one of their two, and a
  35% chance of the other. The Luck bonus doesn't apply (as for every non-masterwork drop). Every one of the
  174 enemies has at least one ingredient, except the Phylactery of Ool (part of Ool's own fight).
- **The Apothecary** (P13): every common ingredient always (10 of each, refilled at each restock), and the region's
  own uncommon and rarer ones: castle cities 5 kinds, towns 3, 2–5 of each (1–2 of an epic one, 1 of a legendary).
  Ingredient prices are fixed and don't rise when a Collect quest asks for one.

## The Apothecary

- In every settlement, its button (O) beside the General Store's (P26). Banners: a grand castle-city shop and a
  small town one (placeholder art, P2). Four menus, switched without leaving: Potions, Ingredients, Brewing, Recipes.
- **Potions:** your band's Health and Mana pairs and the weaker ones, unlimited (P3); then a random selection of the
  new potions and poisons, weighted toward your level band's rarity like the General Store's goods: castle cities
  8 kinds, towns 5, castle 3–6 of each, town 2–4. Restocked every 6 game hours with the other shops.
- **Recipes:** three you don't know, weighted toward your level band's rarity (P16), each 10× its product's price
  (P14); the same three until the next 6-hour restock (P15); fewer when fewer remain; bought ones are never offered
  again.
- **Selling:** it buys potions, poisons and ingredients at half price (P28). The General Store stops selling and
  buying potions.
- **Brewing:** a cauldron in the middle, three ingredient boxes above it, a Brew button, the result shown on the
  cauldron, your known recipes listed on the right. Picking a recipe fills the boxes when you have the ingredients;
  the order doesn't matter; a box can be emptied or swapped. Brew checks that the three are filled, that you have
  them, and that they make a known recipe; only then are the three used up. Brewing costs nothing else. The
  potion appears on the cauldron (a bubbling, glowing brew), and Collect puts it in your pack.

## The classes

Built like the other classes (P30): one gain per level, a passive that is the class's identity at Level 1, small
fixed bonuses on most levels, and a skill as the capstone.

### Alchemist (5 levels; Level 20 and 10 potions brewed, P18)

| Level | Gains |
|---|---|
| 1 | Gain **Ingredient Lore**: an ingredient's details list every potion and poison it goes into, by name and rarity. A recipe you don't know shows only that it exists, not its other ingredients. |
| 2 | Forage finds something 10% more often. |
| 3 | Wisdom permanent bonus +5%. |
| 4 | Forage finds something 10% more often (+20% in all); a find of a common or uncommon ingredient brings one more. |
| 5 | Unlock **Keen Harvest** (outside battle, once a day): your next forage is sure to find something, and rare and rarer ingredients weigh double in it. |

### Assimilator (7 levels; Level 10 and 25 ingredients foraged, P19)

| Level | Gains |
|---|---|
| 1 | Gain **Raw Assimilation**: in battle, eat an ingredient from the item menu (1 AP) for a buff made from the potions it goes into, a third as strong as they are, lasting 2 turns (common), 3 (uncommon, rare) or 4 (epic, legendary). |
| 2 | Vitality permanent bonus +5%. |
| 3 | Nature resistance +10%. |
| 4 | Raw Assimilation buffs are half as strong as their potions and last 1 turn longer. |
| 5 | Eating an ingredient has a 25% chance not to use it up. |
| 6 | Raw Assimilation buffs are two-thirds as strong as their potions and carry their special effects too (AP, rising again, immunities, cures). |
| 7 | Unlock **Devour** (in battle, once per battle): eat an ingredient without spending AP; its buff lasts twice as long. |

**What an eaten ingredient gives** (Raw Assimilation): the buffs of the potions it goes into, effects of the same kind
(two Strength bonuses, two resistances to one element) counted once at the strongest, the two strongest kinds kept,
at the level's strength. Healing and Mana from restoring potions come at the start of each turn of the buff. An
ingredient only in poisons puts the poison's effect on your weapon hits instead. The same ingredient again refreshes
its buff; different ingredients add up (P21).

Example: **Glass Sand** is in the Potion of Precision (+10% crit chance) and Duelist's Draught (+30% crit damage, +5%
crit chance). Crit chance counts once, at +10%; with crit damage +30%: at Level 1 about +3% crit chance and +10% crit
damage for 3 turns; at Level 6 about +7% and +20% for 4 turns.

## Collect quests (P22–P25)

- An ingredient found in the guild's region or a neighbouring one, of your level band's rarity or below.
- Quantity: 6 of a common one, 4 uncommon, 3 rare, 2 epic, 1 legendary.
- Pays like the other quests at the quest's level, by the ingredient's rarity: common ×1.0, uncommon ×1.1, rare
  ×1.2, epic and legendary ×1.35. Never as much as Cull (×1.55) or Hunt (×1.8).
- Progress shows the copies in your pack against the quantity. Handed in at the guild that offered it, at its
  counter; the copies leave your pack and the silver and reputation are paid then. Any copies count (P24). With
  one standard quest at a time, a delivery can only ever count for one quest.


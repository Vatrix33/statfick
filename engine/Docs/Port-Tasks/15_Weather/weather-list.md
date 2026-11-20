# Port task 15, Weather: the weather events (approved)

Approved by the user on 2026-11-20 (WX7 in `Docs/Engine-Plan.md`, "Decisions for task 15, Weather"): draft 2's list, less
four events (Heatwave, Sleet, Heat Haze, Grave Mist), with Mana Storm made a rare event anywhere and Eruption changed to
Heavy Ash; then Heavy Rain added between Light Rain and Torrential Rain (WX14). 23 events.

How it works (decided, WX2–WX6 and WX9): weather comes as fronts that form from each region's climate (from the save's
seed), drift across the continent with a prevailing wind and weaken as they go (Torrential Rain to Heavy Rain to
Light Rain to Overcast, Blizzard to Snowfall, Heavy Ash to Ashfall), changed by mountains (rain to snow on the Stoneveil heights); a
front can cover part of a region, and a battle takes the weather where it starts. Which events move and how each one
eases are drafted and approved when the task is built (the user: "for all weather events where it makes sense"). Control
Weather is also castable on the map, setting the weather around the hero for a few game hours (WX10). Three classes go
with it, designed at GM8's start (WX11): Forecast (travel: predicts the weather, lessens its debuffs), Cloudstrider (a
mage whose spells grow with the weather) and Natural Disaster (Cloudstrider's upgrade, with Botanomancer levels). Every event here can also be cast with Control Weather (one shared set); a battle starts in the region's current
weather and Control Weather overrides it for its turns. Snow, sand and heavy rain slow travel; rain and fog change only
the looks of travel. The battle effects below are directions only; their numbers, each region's climate table (which
events, how often, by time of day) and the travel slowdowns are drafted and approved when the task is built.

**The battle effects and the locations below are examples only** (the user, 2026-11-20, WX12 and WX13): "the current
weather buffs and debuffs are only example-buffs. I want them to be properly designed when weather is implemented", then
"Do the same for the locations." What each event does in battle and where it occurs (each region's climate) are
designed properly, asked and approved when the weather is built (GM4 for the climates, GM5 for battle, GM7 for Control
Weather); the "Where" and "Battle" lines below show only the kind meant. The events themselves and what slows travel
stay as approved.

---

## Everyday weather

### 1. Clear
*Control Weather's "Sunny Day".*
- **Where:** everywhere
- **Travel:** no effect
- **Battle:** no effect (the neutral baseline)

### 2. Overcast
- **Where:** temperate regions
- **Travel:** no effect
- **Battle:** no effect (looks only)

### 3. Light Rain
- **Where:** temperate regions and coasts
- **Travel:** no effect
- **Battle:** water spells slightly stronger, fire spells slightly weaker

### 4. Heavy Rain *(added, WX14)*
*A step above Light Rain and below Torrential Rain.*
- **Where:** temperate regions, the coasts, Stormwake Hills
- **Travel:** a little slower off the roads (less than Torrential Rain)
- **Battle:** water spells stronger, fire spells weaker (between Light Rain and Torrential Rain)

### 5. Torrential Rain
*In Control Weather today.*
- **Where:** Stormwake Hills, the coasts, Verdigris Jungle, Gloamfen Marsh
- **Travel:** slower off the roads (mud)
- **Battle:** water spells stronger, fire spells weaker

### 6. Thunderstorm
*In Control Weather today.*
- **Where:** Stormwake Hills (mostly at night, as today), the coasts, Verdigris Jungle
- **Travel:** slower off the roads (mud)
- **Battle:** lightning and water spells stronger, ranged attacks hit less often

### 7. Fog
- **Where:** Gloamfen Marsh, Mirrormere Basin, Duskpine Forest, the coasts (sea mist), river valleys at dawn
- **Travel:** no effect (looks only)
- **Battle:** dark spells stronger, ranged attacks hit less often

### 8. Gale
- **Where:** the coasts, the steppe, the rocky highlands, the Stoneveil
- **Travel:** no effect
- **Battle:** wind spells stronger, ranged attacks hit less often

### 9. Snowfall
- **Where:** the heights of the Stoneveil Foothills, High Stoneveil
- **Travel:** slower
- **Battle:** ice spells stronger, fire spells slightly weaker

### 10. Blizzard
*In Control Weather today.*
- **Where:** High Stoneveil, Stoneveil Summit
- **Travel:** much slower
- **Battle:** ice spells much stronger, fire spells weaker, Speed and Dexterity lower

### 11. Drought (desert heat)
*In Control Weather today.*
- **Where:** Saffron Expanse, Deep Desert, Crimson Divide
- **Travel:** no effect
- **Battle:** fire and earth spells stronger, ice and water spells weaker

### 12. Sandstorm
- **Where:** Saffron Expanse, Deep Desert
- **Travel:** slower
- **Battle:** earth and wind spells stronger, all attacks hit less often

### 13. Hail
- **Where:** Stormwake Hills, the steppe, Stoneveil Foothills
- **Travel:** no effect
- **Battle:** ice and earth spells stronger, ranged attacks hit less often

### 14. Frost
- **Where:** clear nights in Duskpine Forest, the Stoneveil and the steppe
- **Travel:** no effect
- **Battle:** ice spells stronger, water spells weaker

---

## Regional weather

### 15. Ashfall
- **Where:** Cinder Isle; rarely the coasts facing it: the Sunward Coast (Seabridge's boat crosses to the isle), and the
  rocky highlands' eastern shore if GM4's draft gives them one (they lie due west of the isle)
- **Travel:** no effect
- **Battle:** fire and earth spells stronger, water and nature spells weaker

### 16. Heavy Ash
*An even rarer version of Ashfall (the user's change of draft 2's Eruption).*
- **Where:** blankets a huge area around Cinder Isle: the isle and the regions round it (which regions it reaches is
  drafted with the climates and approved)
- **Travel:** no effect
- **Battle:** as Ashfall, stronger: fire and earth spells much stronger, water and nature spells much weaker

### 17. Abyssal Gloom
- **Where:** the Abyssal Rift only
- **Travel:** no effect
- **Battle:** dark, death and blood spells stronger, holy spells weaker

### 18. Spore Bloom
- **Where:** Verdigris Jungle, Verdant Reach
- **Travel:** no effect (looks: drifting pollen and glowing spores)
- **Battle:** nature spells stronger, healing stronger

### 19. Marsh Miasma
- **Where:** Gloamfen Marsh
- **Travel:** no effect
- **Battle:** death and nature spells stronger, healing weaker

---

## Rare, anywhere

### 20. Mana Storm
*A rare event anywhere (the user's change of draft 2, where it was only at Mirrormere).*
- **Where:** rarely, anywhere
- **Travel:** no effect (looks: shimmering arcs in the sky)
- **Battle:** every spell stronger and cheaper, but spells sometimes miss

### 21. Aurora
- **Where:** clear nights in the north (Stoneveil Foothills, High Stoneveil, Stoneveil Summit)
- **Travel:** no effect
- **Battle:** holy and ice spells stronger, spells cost a little less Mana

### 22. Blood Moon
- **Where:** rare nights anywhere, more often over the Bloodstone Coast, the Crimson Divide and the Ancient Battlefield
- **Travel:** no effect
- **Battle:** blood and dark spells stronger, holy spells weaker

### 23. Starfall
- **Where:** rare clear nights anywhere
- **Travel:** no effect (looks: a meteor shower)
- **Battle:** every spell slightly stronger

---

**Not taken from draft 2 (the user, 2026-11-20):** Heatwave, Sleet, Heat Haze, Grave Mist; Eruption (became Heavy Ash).

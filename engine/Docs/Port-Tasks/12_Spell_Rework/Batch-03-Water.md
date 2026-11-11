# Batch 3: Water (task 12)

**Status: a draft for the user's review (SP2).** Every Water spell players can learn, reworked: 32 spells (Water has
no boss-only spell). They follow the approved system and curve (`Spell-Augmentation.md`), the sample's style
(`Spell-Sample.md`) and the element statuses (`Element-Statuses.md`): each spell sets its status's chance and duration
(SP32). Tiers and prices stay. "(new)" marks a mechanic the game doesn't have yet. [X] values are for the raw caster of
the system draft (no classes or gear) at the level the tier unlocks and at Level 100; Water's spells scale with Wisdom
first, so from this batch on a Wisdom spell is shown for a Wisdom-first caster (the same points, Intelligence and
Wisdom swapped), now and reworked alike.

## Water's identity

The healing and cleansing element, and the one that sets up others:
- **Soaked** (its status: Speed and Evasion −10%) sets up Lightning's **Electrocute** and Ice's **Flash Freeze**; its
  own reaction, **Steam**, puts out Burning (and blinds a burning enemy). Deluge, Abyssal Torrent and Deepcurrent Spear
  are built around soaking; Pressure Wave, Undertow Spear and Abyssal Compression pay it off.
- **Heals that flow:** Mending Spring overflows to the most wounded ally, Mending Current flows on to a second one,
  Surging Cure heals more in an emergency. Holy's heals Bless and Nature's will regrow; Water's move.
- **Cleansing:** Tide Ward, Restoration Tide, Life Current, Tideborn Refuge and the Water Elemental wash debuffs away,
  and Water ends Burning on its own side.
- **Mana for the party:** Mana Spring now restores an ally's Mana, no longer the caster's own, as Mana stays scarce
  (SP17).

New mechanics this batch adds for the port: overflow healing, a heal that flows on to a second ally, an emergency heal,
a ward that washes off the next debuff, a cleanse at the start of each turn, a summon that heals the party when it falls,
a summon that cleanses each turn, losing AP to the undertow.

## At a glance

| Tier | Spell | Kind | AP | Mana | What changes |
|---|---|---|---|---|---|
| 1 | Water Bolt | offensive, one target | 3 | 11 | 20% chance to Soak |
| 1 | Mending Spring | defensive, yourself or an ally | 3 | 10 | a cheap heal whose overflow reaches the most wounded ally |
| 1 | Summon - Water Spirit | summon | 3 | 17 | the spirit grows with spell power; 30% Soak on its hits |
| 1 | Tide Ward | defensive, yourself or an ally | 3 | 12 | washes off a debuff and wards off the next |
| 2 | Seablade | offensive, one target | 2 | 17 | 2 AP, 30% Soak |
| 2 | Soothing Rain | defensive and offensive, the whole field | 3 | 25 | ends your side's Burning, a debuff off each ally, 40% Soak on enemies |
| 2 | Lesser Cascade | offensive, one target | 3 | 20 | grows with every cast in a row (+15%), 25% Soak |
| 2 | Rainveil | defensive, all allies | 3 | 24 | every ally's Evasion +15% for 3 turns |
| 3 | Tidal Spear | offensive, one target | 3 | 34 | 50% chance to Soak |
| 3 | Mending Current | defensive, yourself or an ally | 3 | 36 | heals, then flows on to the most wounded other ally (40%) |
| 3 | Mana Spring | defensive, an ally | 3 | 34 | restores an ally's Mana (no longer your own) |
| 3 | Surging Cure | defensive, yourself or an ally | 3 | 36 | heals 50% more below 30% HP |
| 4 | Summon - Greater Water Spirit | summon | 3 | 55 | a stronger spirit that heals the party when it falls |
| 4 | Deluge | offensive, all enemies | 3 | 50 | a flood: 60% chance to Soak each enemy |
| 4 | Restoration Tide | defensive, yourself or an ally | 3 | 45 | heals and removes 2 debuffs |
| 4 | Pressure Wave | offensive, all enemies | 3 | 52 | +25% on Soaked enemies |
| 5 | Abyssal Torrent | offensive, all enemies | 3 | 76 | every enemy Soaked for certain |
| 5 | Life Current | defensive, yourself or an ally | 3 | 74 | heals; for 3 turns washes off a debuff each turn |
| 5 | Undertow Spear | offensive, one target | 3 | 71 | a Soaked target loses 1 AP |
| 5 | Dragonbreath - Drowning Torrent | offensive, all enemies | 3 | 76 | Wisdom −10% kept (Dragonbreath), 25% Soak |
| 5 | Tidal Sanctuary | defensive, yourself or an ally | 3 | 76 | heals, −20% spell damage, Burning ends |
| 6 | Summon - Water Elemental | summon | 3 | 100 | while it stands, it washes a debuff off an ally each turn |
| 6 | Tidal Rupture | offensive, all enemies | 3 | 96 | Evasion −20% kept, for the party's weapons |
| 6 | Abyssal Pressure | offensive, one target | 3 | 104 | 20% Mana siphon kept, 40% Soak |
| 7 | Dragonbreath - Tidal Maw | offensive, all enemies | 3 | 134 | Speed −20% kept (Dragonbreath), 30% Soak |
| 7 | Deepcurrent Spear | offensive, one target | 3 | 146 | always Soaks for 3 turns |
| 7 | Reservoir Blessing | defensive, all allies | 3 | 140 | heals every ally and ends their Burning |
| 8 | Ocean's Mercy | defensive, all allies | 3 | 170 | heals every ally and removes one debuff from each |
| 8 | Abyssal Spear | offensive, one target | 3 | 190 | Evasion −30% kept, 40% Soak |
| 9 | Tideborn Refuge | defensive, all allies | 3 | 240 | heals, removes every debuff, −15% spell damage |
| 9 | Abyssal Compression | offensive, one target | 3 | 260 | +50% on a Soaked target, ending its Soaked |
| 10 | Ocean Without Shores | defensive, all allies; offensive, all enemies | 3 | 350 | heals, restores Mana, cleanses all; every enemy Soaked |

## The spells

### Water Bolt: Tier 1, offensive, one target

- **Now:** 3 AP, 11 Mana, power 5 + Wis ×0.3 + Int ×0.2. A small bolt of water that deals [X] Water damage.
- **Reworked:** 3 AP, 11 Mana; power 6 + Wis ×0.33 + Int ×0.1 (95% of the tier's reference). A 20% chance to **Soak** the target for 2 turns.
- **Description:** "A bolt of water that deals [X] Water damage, with a 20% chance to Soak the target for 2 turns."
- **[X], now → reworked:** Level 1: 10 → 10; Level 100: 158 → 147.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Water's plain bolt, now able to set up Lightning's Electrocute and Ice's Flash Freeze.

### Mending Spring: Tier 1, defensive, yourself or an ally

- **Now:** 3 AP, 8 Mana, power 5 + Wis ×0.5 + - ×None. Cover yourself or an ally in healing water, restoring [X] HP.
- **Reworked:** 3 AP, 10 Mana; power 6 + Wis ×0.35 + Int ×0.1 (100% of the tier's reference). Heals [X]; healing past their maximum HP **overflows** to the most wounded other ally (new).
- **Description:** "Cover yourself or an ally in healing water, restoring [X] HP; whatever they can't take flows on to your most wounded ally."
- **[X], now → reworked:** Level 1: 10 → 10; Level 100: 183 → 154.
- **Augments:** Power, Mana cost.
- **Why:** The cheapest heal in the game, which no longer wastes itself on a nearly healthy target.

### Summon - Water Spirit: Tier 1, summon

- **Now:** 3 AP, 17 Mana, power none. Summon a friendly Water Spirit to help you in battle.
- **Reworked:** 3 AP, 17 Mana; power 6 + Wis ×0.35 + Int ×0.1 (100% of the tier's reference, as a summon). Summons a Water Spirit (Level 3, 50 HP today); its maximum HP rises by 200% of [X] and every attack and spell it makes by 25% of [X] (SP29). Its attacks and spells have a 30% chance to **Soak** their target for 2 turns. One at a time.
- **Description:** "Summon a friendly Water Spirit whose maximum HP rises by twice [X] and whose attacks and spells deal a quarter of [X] more, each with a 30% chance to Soak its target. Only one may be active at a time."
- **[X], now → reworked:** Level 1: - → 10; Level 100: - → 154.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Today a Level 3 spirit fades after the first hours; it now grows with its caster's spell power.

### Tide Ward: Tier 1, defensive, yourself or an ally

- **Now:** 3 AP, 13 Mana, power none. Wrap yourself or an ally in water, granting 5 Armor for 2 turns.
- **Reworked:** 3 AP, 12 Mana. Removes one debuff (Burning first) and wards them for 3 turns: the next debuff put on them is washed away at once (new). Was 5 Armor for 2 turns.
- **Description:** "Wrap yourself or an ally in a tide that washes away one debuff and, for 3 turns, the next one that would take hold."
- **Augments:** Effect duration, Mana cost.
- **Why:** Five Armor did almost nothing; Water becomes the cleansing element from its first tier.

### Seablade: Tier 2, offensive, one target

- **Now:** 3 AP, 24 Mana, power 12 + Wis ×0.4 + Int ×0.2. Shoot out a slice of seawater. Deal [X] Water damage and decrease the target's Vitality by 5% for 2 turns.
- **Reworked:** 2 AP, 17 Mana; power 6 + Wis ×0.35 + Int ×0.1 (65% of the tier's reference, the 2 AP rule). A quick 2 AP slice; a 30% chance to **Soak** the target for 2 turns (was Vitality −5% for 2 turns).
- **Description:** "Shoot out a quick slice of seawater for [X] Water damage, with a 30% chance to Soak the target for 2 turns."
- **[X], now → reworked:** Level 10: 34 → 24; Level 100: 201 → 154.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Water's fast spell, to Soak a target and still cast a 3 AP spell in the same turn.

### Soothing Rain: Tier 2, defensive and offensive, the whole field

- **Now:** 3 AP, 25 Mana, power none. Remove a random debuff from a random ally. All combatants take 30% less Fire damage for 4 turns.
- **Reworked:** 3 AP, 25 Mana. Ends every Burning on your side, removes one debuff from each ally, and each enemy has a 40% chance to be **Soaked** for 2 turns (was one debuff off a random ally and 30% less Fire damage for every combatant).
- **Description:** "Bring a soothing rain over the field: every ally is cleansed of one debuff and any Burning, and each enemy has a 40% chance to be Soaked for 2 turns."
- **Augments:** Effect duration, Effect chance, Mana cost.
- **Why:** Today's random target and its help to enemies (Fire damage −30% for everyone) made it unreliable; it now always helps your side.

### Lesser Cascade: Tier 2, offensive, one target

- **Now:** 3 AP, 20 Mana, power 10 + Wis ×0.5 + Int ×0.2. A building torrent of water that deals [X] Water damage. Each successive cast increases damage and Mana cost by 10%.
- **Reworked:** 3 AP, 20 Mana; power 8 + Wis ×0.46 + Int ×0.14 (85% of the tier's reference). Each cast in a row (yours, without another spell between) adds 15% damage and 10% Mana (was 10% and 10%); a 25% chance to **Soak** the target for 2 turns.
- **Description:** "A building torrent of water that deals [X] Water damage, with a 25% chance to Soak the target. Each cast in a row deals 15% more and costs 10% more Mana."
- **[X], now → reworked:** Level 10: 37 → 32; Level 100: 235 → 205.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Its building torrent stays, a little stronger, as Water's patient damage.

### Rainveil: Tier 2, defensive, all allies

- **Now:** 3 AP, 24 Mana, power none. Veil all allies in rain, increasing their Evasion by 15% for 2 turns.
- **Reworked:** 3 AP, 24 Mana. Every ally's Evasion +15% for 3 turns (was 2).
- **Description:** "Veil all allies in rain, increasing their Evasion by 15% for 3 turns."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Kept, a turn longer: a cheap party defence against weapons.

### Tidal Spear: Tier 3, offensive, one target

- **Now:** 3 AP, 34 Mana, power 34 + Wis ×0.6 + Int ×0.3. Launch a pressurized spear of water that deals [X] Water damage and decreases the target's Speed by 15% for 2 turns.
- **Reworked:** 3 AP, 34 Mana; power 12 + Wis ×0.69 + Int ×0.2 (90% of the tier's reference). A 50% chance to **Soak** the target for 2 turns (was Speed −15%, which Soaked covers).
- **Description:** "Launch a pressurized spear of water for [X] Water damage, with a 50% chance to Soak the target for 2 turns."
- **[X], now → reworked:** Level 20: 96 → 76; Level 100: 318 → 305.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Its Speed loss becomes the element's status.

### Mending Current: Tier 3, defensive, yourself or an ally

- **Now:** 3 AP, 37 Mana, power 30 + Wis ×0.6 + Int ×0.3. Wash restorative water over yourself or an ally, restoring [X] HP plus 10% of maximum HP.
- **Reworked:** 3 AP, 36 Mana; power 11 + Wis ×0.65 + Int ×0.19 (85% of the tier's reference). Heals [X] plus 5% of maximum HP, then **flows on** to the most wounded other ally for 40% of [X] (new; was [X] plus 10%).
- **Description:** "Wash a restorative current over yourself or an ally for [X] HP plus 5% of maximum HP; it flows on to your most wounded other ally for 40% of [X]."
- **[X], now → reworked:** Level 20: 92 → 71; Level 100: 314 → 287.
- **Augments:** Power, Mana cost.
- **Why:** Today Mending Current, Surging Cure and Greater Heal are the same spell; Water's heals now flow.

### Mana Spring: Tier 3, defensive, an ally (not yourself)

- **Now:** 3 AP, 34 Mana, power 26 + Wis ×0.6 + Int ×0.3. Call up a clear spring around yourself or an ally, restoring [X] Mana plus 5% of maximum Mana.
- **Reworked:** 3 AP, 34 Mana; power 6 + Wis ×0.39 + Int ×0.11 (50% of the tier's reference, as Mana restored). Restores [X] Mana plus 5% of maximum Mana to an ally, no longer to yourself (was yourself or an ally).
- **Description:** "Call up a clear spring around an ally, restoring [X] Mana plus 5% of their maximum Mana."
- **[X], now → reworked:** Level 20: 88 → 42; Level 100: 310 → 171.
- **Augments:** Power, Mana cost.
- **Why:** Today a caster can cast it on themselves for about 280 Mana more than it costs at Level 100, half a pool every turn; with Mana kept scarce (SP17) it becomes a healer's gift to the party's casters.

### Surging Cure: Tier 3, defensive, yourself or an ally

- **Now:** 3 AP, 36 Mana, power 35 + Wis ×0.6 + Int ×0.3. Restore [X] HP plus 10% of maximum HP to yourself or one ally.
- **Reworked:** 3 AP, 36 Mana; power 11 + Wis ×0.65 + Int ×0.19 (85% of the tier's reference). Heals [X] plus 5% of maximum HP, 50% more on a target below 30% of its HP (new; was [X] plus 10%).
- **Description:** "Restore [X] HP plus 5% of maximum HP to yourself or an ally, half again as much if they are below 30% HP."
- **[X], now → reworked:** Level 20: 97 → 71; Level 100: 319 → 287.
- **Augments:** Power, Mana cost.
- **Why:** Water's emergency heal, unlike its flowing ones.

### Summon - Greater Water Spirit: Tier 4, summon

- **Now:** 3 AP, 55 Mana, power none. Summon a friendly Greater Water Spirit with 40% increased max HP and 25% increased attack damage.
- **Reworked:** 3 AP, 55 Mana; power 16 + Wis ×0.94 + Int ×0.27 (100% of the tier's reference, as a summon). Summons a Greater Water Spirit (Level 13, 133 HP today, summoned with +40% HP and +25% damage); its maximum HP rises by 200% of [X] and every attack and spell it makes by 25% of [X] (SP29). Its hits have a 40% chance to **Soak** their target for 2 turns; when it falls, it heals every ally for half of [X] (new). One at a time.
- **Description:** "Summon a friendly Greater Water Spirit whose maximum HP rises by twice [X] and whose attacks and spells deal a quarter of [X] more, each with a 40% chance to Soak its target; when it falls, it heals all allies for half of [X]. Only one may be active at a time."
- **[X], now → reworked:** Level 30: - → 142; Level 100: - → 414.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** It grows with spell power, and its end helps the party.

### Deluge: Tier 4, offensive, all enemies

- **Now:** 3 AP, 50 Mana, power 38 + Wis ×0.7 + Int ×0.3. Crash a surging flood over all enemies for [X] Water damage and decrease their Speed by 10% for 2 turns.
- **Reworked:** 3 AP, 50 Mana; power 10 + Wis ×0.61 + Int ×0.18 (65% of the tier's reference, 75% for all enemies, less its likely soak). Each enemy struck has a 60% chance to be **Soaked** for 2 turns (was Speed −10%).
- **Description:** "Crash a flood over all enemies for [X] Water damage; each has a 60% chance to be Soaked for 2 turns."
- **[X], now → reworked:** Level 30: 138 → 92; Level 100: 357 → 270.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Built around soaking, so it usually does: the opener for a Lightning or Ice party.

### Restoration Tide: Tier 4, defensive, yourself or an ally

- **Now:** 3 AP, 45 Mana, power 42 + Wis ×0.7 + Int ×0.3. Surround yourself or an ally with restorative water, restoring [X] HP plus 20% of maximum HP and removing one random debuff.
- **Reworked:** 3 AP, 45 Mana; power 14 + Wis ×0.8 + Int ×0.23 (85% of the tier's reference). Heals [X] plus 10% of maximum HP and removes two debuffs (was [X] plus 20% and one).
- **Description:** "Surround yourself or an ally with a restoring tide for [X] HP plus 10% of maximum HP, washing away two debuffs."
- **[X], now → reworked:** Level 30: 142 → 121; Level 100: 361 → 353.
- **Augments:** Power, Mana cost.
- **Why:** The cleansing heal of the mid tiers.

### Pressure Wave: Tier 4, offensive, all enemies

- **Now:** 3 AP, 52 Mana, power 36 + Wis ×0.7 + Int ×0.3. Crash through all enemies for [X] Water damage and reduce their Armor by 10% for 2 turns.
- **Reworked:** 3 AP, 52 Mana; power 11 + Wis ×0.66 + Int ×0.19 (70% of the tier's reference, 75% for all enemies, less the bonus). Enemies that are **Soaked** take 25% more (was Armor −10% for 2 turns).
- **Description:** "Crash a pressure wave through all enemies for [X] Water damage, 25% more to those already Soaked."
- **[X], now → reworked:** Level 30: 136 → 99; Level 100: 355 → 291.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** The group payoff for a soaked field; Armor loss belongs to Earth.

### Abyssal Torrent: Tier 5, offensive, all enemies

- **Now:** 3 AP, 76 Mana, power 43 + Wis ×0.8 + Int ×0.3. Crush all enemies beneath deep water for [X] Water damage and reduce their Speed by 15% for 2 turns.
- **Reworked:** 3 AP, 76 Mana; power 13 + Wis ×0.73 + Int ×0.21 (60% of the tier's reference, 75% for all enemies, less its sure soak). Every enemy struck is always **Soaked** for 2 turns (was Speed −15%).
- **Description:** "Crush all enemies beneath deep water for [X] Water damage and Soak every one of them for 2 turns."
- **[X], now → reworked:** Level 40: 190 → 141; Level 100: 398 → 323.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** Water's sure group soak, for less damage than Deluge's gamble.

### Life Current: Tier 5, defensive, yourself or an ally

- **Now:** 3 AP, 74 Mana, power 54 + Wis ×0.8 + Int ×0.3. Restore [X] HP plus 25% of maximum HP to yourself or an ally and remove one random debuff.
- **Reworked:** 3 AP, 74 Mana; power 18 + Wis ×1.04 + Int ×0.3 (85% of the tier's reference). Heals [X] plus 10% of maximum HP; for 3 turns, the current washes one debuff off them at the start of each of their turns (new; was [X] plus 25% and one debuff off).
- **Description:** "Set a living current around yourself or an ally: it heals [X] HP plus 10% of maximum HP and, at the start of each of their next 3 turns, washes away one debuff."
- **[X], now → reworked:** Level 40: 201 → 200; Level 100: 409 → 459.
- **Augments:** Power, Effect duration, Mana cost.
- **Why:** A cleanse that keeps working against enemies who debuff every turn.

### Undertow Spear: Tier 5, offensive, one target

- **Now:** 3 AP, 71 Mana, power 60 + Wis ×0.8 + Int ×0.3. Drive a spear of deep water through one enemy for [X] Water damage and reduce its Mind by 15% for 2 turns.
- **Reworked:** 3 AP, 71 Mana; power 19 + Wis ×1.1 + Int ×0.32 (90% of the tier's reference). If the target is **Soaked**, the undertow drags it: it loses 1 AP at its next turn (new; was Mind −15%).
- **Description:** "Drive a spear of deep water through one enemy for [X] Water damage; if it is Soaked, the undertow costs it 1 AP on its next turn."
- **[X], now → reworked:** Level 40: 207 → 212; Level 100: 415 → 486.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** A single-target payoff for Soaked that slows the strongest enemy.

### Dragonbreath - Drowning Torrent: Tier 5, offensive, all enemies (Dragonbreath)

- **Now:** 3 AP, 76 Mana, power 42 + Wis ×0.8 + Int ×0.3. Unleash a crushing torrent across all enemies for [X] Water damage and reduce their Wisdom by 10% for 2 turns.
- **Reworked:** 3 AP, 76 Mana; power 14 + Wis ×0.79 + Int ×0.23 (65% of the tier's reference, 75% for all enemies, less its effects). Its weakening effect stays (Wisdom −10% for 2 turns); each enemy struck has a 25% chance to be **Soaked** for 2 turns.
- **Description:** "Unleash a crushing torrent across all enemies for [X] Water damage, reducing their Wisdom by 10% for 2 turns, with a 25% chance to Soak each for 2 turns."
- **[X], now → reworked:** Level 40: 189 → 153; Level 100: 397 → 350.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Dragonbreath spells keep their weakening effect.

### Tidal Sanctuary: Tier 5, defensive, yourself or an ally

- **Now:** 3 AP, 76 Mana, power 54 + Wis ×0.8 + Int ×0.3. Restore [X] HP plus 20% of maximum HP to yourself or an ally and reduce their incoming spell damage by 20% for 2 turns.
- **Reworked:** 3 AP, 76 Mana; power 17 + Wis ×0.98 + Int ×0.28 (80% of the tier's reference). Heals [X] plus 10% of maximum HP, ends their Burning and reduces the spell damage they take by 20% for 2 turns (was [X] plus 20%).
- **Description:** "Shelter yourself or an ally in a tidal sanctuary: [X] HP plus 10% of maximum HP restored, any Burning put out, and 20% less spell damage for 2 turns."
- **[X], now → reworked:** Level 40: 201 → 188; Level 100: 409 → 432.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** Kept as Water's heal against casters, with fire put out.

### Summon - Water Elemental: Tier 6, summon

- **Now:** 3 AP, 100 Mana, power none. Summon a friendly Water Elemental with 25% increased maximum HP. Only one may be active at a time.
- **Reworked:** 3 AP, 100 Mana; power 25 + Wis ×1.47 + Int ×0.42 (100% of the tier's reference, as a summon). Summons a Water Elemental (Level 25, 228 HP today); its maximum HP rises by 200% of [X] and every attack and spell it makes by 25% of [X] (SP29). Its hits have a 40% chance to **Soak** their target for 2 turns; while it stands, at the start of its turn it washes one debuff off the most afflicted ally (new). One at a time.
- **Description:** "Summon a friendly Water Elemental whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more, each with a 40% chance to Soak its target; at the start of each of its turns it washes one debuff off an ally. Only one may be active at a time."
- **[X], now → reworked:** Level 50: - → 343; Level 100: - → 648.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Water's strongest summon cleanses the party while it lives.

### Tidal Rupture: Tier 6, offensive, all enemies

- **Now:** 3 AP, 96 Mana, power 52 + Wis ×0.9 + Int ×0.3. Crush all enemies in a surging tide for [X] Water damage and reduce their Evasion by 20% for 2 turns.
- **Reworked:** 3 AP, 96 Mana; power 18 + Wis ×1.03 + Int ×0.29 (70% of the tier's reference, 75% for all enemies, less the Evasion loss). Every enemy's Evasion −20% for 2 turns (kept).
- **Description:** "Crush all enemies in a surging tide for [X] Water damage and reduce their Evasion by 20% for 2 turns."
- **[X], now → reworked:** Level 50: 251 → 240; Level 100: 443 → 453.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Kept: it helps the party's weapon users land their blows.

### Abyssal Pressure: Tier 6, offensive, one target

- **Now:** 3 AP, 104 Mana, power 68 + Wis ×0.9 + Int ×0.3. Compress one enemy beneath deep water for [X] Water damage and siphon Mana equal to 20% of the damage dealt.
- **Reworked:** 3 AP, 104 Mana; power 22 + Wis ×1.32 + Int ×0.38 (90% of the tier's reference). Siphons Mana: you regain 20% of the damage dealt (kept); a 40% chance to **Soak** the target for 2 turns.
- **Description:** "Compress one enemy beneath deep water for [X] Water damage, regaining Mana equal to 20% of the damage dealt, with a 40% chance to Soak it for 2 turns."
- **[X], now → reworked:** Level 50: 267 → 308; Level 100: 459 → 582.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Kept as the water healer's way to win back Mana between heals.

### Dragonbreath - Tidal Maw: Tier 7, offensive, all enemies (Dragonbreath)

- **Now:** 3 AP, 134 Mana, power 64 + Wis ×0.9 + Int ×0.4. Crash over all enemies for [X] Water damage and lower Speed by 20% for 2 turns.
- **Reworked:** 3 AP, 134 Mana; power 20 + Wis ×1.18 + Int ×0.34 (65% of the tier's reference, 75% for all enemies, less its effects). Its weakening effect stays (Speed −20% for 2 turns); each enemy struck has a 30% chance to be **Soaked** for 2 turns.
- **Description:** "Crash over all enemies with the tide's maw for [X] Water damage, lowering their Speed by 20% for 2 turns, with a 30% chance to Soak each for 2 turns."
- **[X], now → reworked:** Level 60: 316 → 325; Level 100: 478 → 520.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Dragonbreath spells keep their weakening effect.

### Deepcurrent Spear: Tier 7, offensive, one target

- **Now:** 3 AP, 146 Mana, power 82 + Wis ×0.9 + Int ×0.4. Strike one enemy for [X] Water damage and reduce Evasion by 25% for 2 turns.
- **Reworked:** 3 AP, 146 Mana; power 26 + Wis ×1.55 + Int ×0.44 (85% of the tier's reference). The target is always **Soaked** for 3 turns (was Evasion −25% for 2 turns).
- **Description:** "Strike one enemy with the deep current for [X] Water damage and Soak it for 3 turns."
- **[X], now → reworked:** Level 60: 334 → 425; Level 100: 496 → 682.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** A sure, long Soak on the one enemy the party's Lightning or Ice must reach.

### Reservoir Blessing: Tier 7, defensive, all allies

- **Now:** 3 AP, 140 Mana, power 64 + Wis ×0.9 + Int ×0.4. Restore [X] HP plus 10% maximum HP to every ally.
- **Reworked:** 3 AP, 140 Mana; power 20 + Wis ×1.18 + Int ×0.34 (65% of the tier's reference, 75% for all allies, less the cleanse). Heals every ally for [X] plus 5% of maximum HP and ends their Burning (was [X] plus 10%).
- **Description:** "Open a reservoir over the party: every ally regains [X] HP plus 5% of maximum HP and any Burning is put out."
- **[X], now → reworked:** Level 60: 316 → 325; Level 100: 478 → 520.
- **Augments:** Power, Mana cost.
- **Why:** Water's party heal, kept, with the element's answer to Fire.

### Ocean's Mercy: Tier 8, defensive, all allies

- **Now:** 3 AP, 170 Mana, power 70 + Wis ×1 + Int ×0.5. Restore [X] HP plus 20% maximum HP to all allies.
- **Reworked:** 3 AP, 170 Mana; power 25 + Wis ×1.47 + Int ×0.42 (70% of the tier's reference, 75% for all allies, less the cleanse). Heals every ally for [X] plus 10% of maximum HP and removes one debuff from each (was [X] plus 20%).
- **Description:** "Pour the ocean's mercy over the party: every ally regains [X] HP plus 10% of maximum HP and is cleansed of one debuff."
- **[X], now → reworked:** Level 70: 404 → 465; Level 100: 543 → 648.
- **Augments:** Power, Mana cost.
- **Why:** The party heal of the high tiers, now also a cleanse.

### Abyssal Spear: Tier 8, offensive, one target

- **Now:** 3 AP, 190 Mana, power 90 + Wis ×1 + Int ×0.5. Strike one enemy for [X] Water damage and lower Evasion by 30% for 2 turns.
- **Reworked:** 3 AP, 190 Mana; power 34 + Wis ×1.99 + Int ×0.57 (95% of the tier's reference). Evasion −30% for 2 turns (kept); a 40% chance to **Soak** the target for 2 turns.
- **Description:** "Strike one enemy with an abyssal spear for [X] Water damage, lowering its Evasion by 30% for 2 turns, with a 40% chance to Soak it."
- **[X], now → reworked:** Level 70: 424 → 630; Level 100: 563 → 877.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Kept for the party's weapon users, with the element's status.

### Tideborn Refuge: Tier 9, defensive, all allies

- **Now:** 3 AP, 240 Mana, power 85 + Wis ×1.1 + Int ×0.5. Restore [X] HP plus 10% maximum HP to all allies and clear one debuff from each.
- **Reworked:** 3 AP, 240 Mana; power 29 + Wis ×1.7 + Int ×0.49 (65% of the tier's reference, 75% for all allies, less its effects). Heals every ally for [X] plus 10% of maximum HP, removes all of their debuffs and reduces the spell damage they take by 15% for 2 turns (was one debuff each).
- **Description:** "Raise a tideborn refuge around the party: every ally regains [X] HP plus 10% of maximum HP, is cleansed of every debuff and takes 15% less spell damage for 2 turns."
- **[X], now → reworked:** Level 80: 494 → 609; Level 100: 594 → 750.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** The element's great cleanse.

### Abyssal Compression: Tier 9, offensive, one target

- **Now:** 3 AP, 260 Mana, power 95 + Wis ×1.1 + Int ×0.5. Crush one enemy for [X] Water damage and lower Speed by 25% for 2 turns.
- **Reworked:** 3 AP, 260 Mana; power 43 + Wis ×2.49 + Int ×0.71 (95% of the tier's reference). Deals 50% more to a **Soaked** target, and its Soaked ends (was Speed −25%).
- **Description:** "Crush one enemy under the abyss for [X] Water damage, half again as much if it is Soaked (which ends it)."
- **[X], now → reworked:** Level 80: 504 → 891; Level 100: 604 → 1,097.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Water's great single-target payoff, spending the Soak it built.

### Ocean Without Shores: Tier 10, defensive, all allies; offensive, all enemies

- **Now:** 3 AP, 350 Mana, power 240 + Wis ×1.2 + Int ×0.6. Restore [X] HP plus 25% maximum HP and 10% maximum Mana to all allies, then clear one debuff from each.
- **Reworked:** 3 AP, 350 Mana; power 55 + Wis ×3.22 + Int ×0.92 (80% of the tier's reference, as a party heal). Heals every ally for [X] plus 25% of maximum HP, restores 10% of their maximum Mana and removes all of their debuffs (was one each); every enemy is always **Soaked** for 3 turns (new).
- **Description:** "Call an ocean without shores: every ally regains [X] HP plus 25% of maximum HP and 10% of maximum Mana and is cleansed of every debuff, and every enemy is Soaked for 3 turns."
- **[X], now → reworked:** Level 90: 752 → 1,285; Level 100: 808 → 1,419.
- **Augments:** Power, Effect duration, Mana cost.
- **Why:** Water's Tier 10 heals the whole party and sets up the whole field.

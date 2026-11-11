# Batch 2: Fire (task 12)

**Status: a draft for the user's review (SP2).** Every Fire spell players can learn, reworked: 31 spells (Vulcanic
Charge, the Vulcanic Ram's, is left out, SP4). They follow the approved system and curve (`Spell-Augmentation.md`), the
sample's style (`Spell-Sample.md`, where Fire Bolt was approved) and the element statuses (`Element-Statuses.md`):
each spell sets its status's chance and duration (SP32). Tiers and prices stay. "(new)" marks a mechanic the game
doesn't have yet; [X] values are for the raw caster of the system draft (no classes or gear), at the level the tier
unlocks and at Level 100.

## Fire's identity

The element of damage and burning:
- **Burning** (its status: damage at the start of each of the target's turns, a share of the hit) and **Melt** (its
  reaction: +40% on Chilled or Frozen enemies, ending them). Spells built around fire (walls, floods, crowns) burn
  often; spells whose goal is the hit burn now and then (SP32). Wind spreads Burning (Wildfire), Water puts it out
  (Steam).
- **Payoffs for a burning field:** Flame Spiral strikes Burning enemies twice, Ember Lance and Burning Horizon hit them
  harder.
- **The phoenix:** cloaks and wards that burn melee attackers, a rising from defeat (Flame Rebirth) and a revival
  (Phoenix Renewal).
- **Dragonbreath:** Fire has five; each keeps its weakening effect for task 10's Dragonkin and gains a chance to burn.
- Today Fire's spells sit above the other elements' at the same tier (Dragonbreath - Cinderstorm hits every enemy
  harder than Tier 3's reference single-target spell), so several low-tier Fire attacks hit less on the curve and
  make it up in burns. Healing loss moves to Death's Withered (the Dragonbreath spells and The First Flame keep
  theirs), and of six lances that all lowered Armor, only Sunforge Lance still does.

New mechanics this batch adds for the port: splash damage, a summon that bursts when it falls, a summon that empowers
its side's spells, rising from defeat on a spell (the Phoenix Plume's effect), a revival spell, a Mana refund on a
kill, a spear that pierces into a second enemy, a burn that ignores immunity.

## At a glance

| Tier | Spell | Kind | AP | Mana | What changes |
|---|---|---|---|---|---|
| 1 | Fire Bolt | offensive, one target | 3 | 12 | 20% chance to Burn (approved in the sample) |
| 1 | Minor Firewall | offensive, all enemies | 3 | 16 | a wall of fire: 80% chance to Burn each enemy |
| 1 | Dragonbreath - Minor Ignition | offensive, one target | 3 | 14 | Vitality −5% kept (Dragonbreath), 20% Burn |
| 1 | Ember Brand | offensive, one target | 3 | 14 | 50% chance of a long, low burn (3 turns) |
| 2 | Fire Whip | offensive, one target | 2 | 15 | 2 AP, 25% Burn |
| 2 | Summon - Fire Spirit | summon | 3 | 25 | the spirit grows with spell power; 30% Burn on its hits |
| 2 | Burning Cloak | defensive, yourself | 3 | 20 | retaliation, 50% chance to Burn the attacker |
| 2 | Ashfall | offensive, all enemies | 3 | 26 | Accuracy −10% on every enemy |
| 3 | Dragonbreath - Cinderstorm | offensive, all enemies | 3 | 38 | Vitality −8% kept (Dragonbreath), 25% Burn |
| 3 | Summon - Greater Fire Spirit | summon | 3 | 40 | a stronger spirit that bursts when it falls; 50% Burn |
| 3 | Phoenix Mantle | defensive, yourself or an ally | 3 | 27 | −40% Fire damage, can't Burn, Chill thawed |
| 3 | Flame Spiral | offensive, all enemies | 3 | 37 | Burning enemies are struck twice |
| 4 | Inferno Orb | offensive, one target | 3 | 52 | splashes 30% on the others, 30% Burn |
| 4 | Hellfire Wave | offensive, all enemies | 3 | 55 | 40% chance of a hotter burn (20%, 3 turns) |
| 4 | Flame Rebirth | defensive, yourself or an ally | 3 | 43 | if they fall within 3 turns, they rise with [X] HP |
| 4 | Ember Lance | offensive, one target | 3 | 50 | +30% against a Burning target |
| 5 | Dragonbreath - Starfire | offensive, all enemies | 3 | 79 | Vitality −10% kept (Dragonbreath), 25% Burn |
| 5 | Solar Lance | offensive, one target | 3 | 76 | pierces 30 resistance, 50% Burn |
| 5 | Summon - Fire Elemental | summon | 3 | 82 | while it stands, the party's Fire spells +10% |
| 5 | Burning Horizon | offensive, all enemies | 3 | 78 | +30% on Burning enemies, 50% Burn on the rest |
| 6 | Dragonbreath - Crown of Embers | offensive, all enemies | 3 | 96 | healing received −25% kept (Dragonbreath), 30% Burn |
| 6 | Phoenix Spear | offensive, one target | 3 | 104 | a kill returns half its Mana |
| 6 | Phoenix Ward | defensive, all allies | 3 | 100 | the whole party retaliates, 30% Burn |
| 7 | Cinder Flood | offensive, all enemies | 3 | 134 | 70% chance of a 3-turn burn on every enemy |
| 7 | Sunforge Lance | offensive, one target | 3 | 146 | melts Armor −20% |
| 7 | Phoenix Renewal | defensive, yourself or an ally, fallen or not | 3 | 140 | revives a fallen ally, or heals and cleanses |
| 8 | Dragonbreath - Worldfire | offensive, all enemies | 3 | 170 | healing received −30% kept (Dragonbreath), 35% Burn |
| 8 | Starfire Spear | offensive, one target | 3 | 190 | also pierces a second enemy for 60% |
| 9 | Ashfall Crown | offensive, all enemies | 3 | 240 | every enemy Burns for certain (20%, 3 turns) |
| 9 | Solar Pike | offensive, one target | 3 | 260 | a sure, heavy burn (25%, 3 turns) |
| 10 | The First Flame | offensive, all enemies | 3 | 350 | sure 3-turn burn even on fire-immune enemies; healing −50% kept |

## The spells

### Fire Bolt: Tier 1, offensive, one target

- **Now:** 3 AP, 12 Mana, power 7 + Int ×0.4 + Wis ×0.1. A bolt of fire that deals [X] Fire damage.
- **Reworked:** 3 AP, 12 Mana; power 6 + Int ×0.33 + Wis ×0.1 (95% of the tier's reference). A 20% chance to set the target **Burning** for 2 turns, for 15% of the hit each turn (the sample's version, approved, SP31–SP32).
- **Description:** "Hurl a bolt of fire at one enemy for [X] Fire damage, with a 20% chance to set it Burning: it takes 15% of that again at the start of each of its next 2 turns."
- **[X], now → reworked:** Level 1: 12 → 10; Level 100: 173 → 147.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** As approved in the sample.

### Minor Firewall: Tier 1, offensive, all enemies

- **Now:** 3 AP, 16 Mana, power 3 + Int ×0.3 + Wis ×0.1. Extend a small wall of fire that deals [X] Fire damage to all enemies.
- **Reworked:** 3 AP, 16 Mana; power 4 + Int ×0.21 + Wis ×0.06 (60% of the tier's reference, 75% for all enemies, less its likely burn). Each enemy struck has an 80% chance to be **Burning** for 2 turns, for 15% of the hit each turn.
- **Description:** "Raise a wall of fire through the enemy line for [X] Fire damage; each enemy has an 80% chance to be set Burning for 2 turns."
- **[X], now → reworked:** Level 1: 7 → 7; Level 100: 133 → 93.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** The wall your note named: built around Burning, so it nearly always burns; its hit is the lighter for it.

### Dragonbreath - Minor Ignition: Tier 1, offensive, one target (Dragonbreath)

- **Now:** 3 AP, 14 Mana, power 5 + Int ×0.4 + Wis ×0.1. Breathe out a small cloud of fire. Deal [X] Fire damage and reduce the target's Vitality by 5% for 4 turns.
- **Reworked:** 3 AP, 14 Mana; power 5 + Int ×0.3 + Wis ×0.09 (85% of the tier's reference). Its weakening effect stays (Vitality −5% for 4 turns, which Dragon's Breath lengthens); a 20% chance to set the target **Burning** for 2 turns (15% of the hit each turn).
- **Description:** "Breathe a small flame on one enemy for [X] Fire damage and reduce its Vitality by 5% for 4 turns, with a 20% chance to set it Burning for 2 turns."
- **[X], now → reworked:** Level 1: 10 → 9; Level 100: 171 → 133.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Dragonbreath spells keep their weakening effect (task 10's Dragonkin builds on it).

### Ember Brand: Tier 1, offensive, one target

- **Now:** 3 AP, 14 Mana, power 6 + Int ×0.4 + Wis ×0.1. Brand one enemy for [X] Fire damage and reduce their healing received by 10% for 2 turns.
- **Reworked:** 3 AP, 14 Mana; power 5 + Int ×0.3 + Wis ×0.09 (85% of the tier's reference). A 50% chance to brand the target **Burning** for 3 turns, for 10% of the hit each turn (was healing received −10% for 2 turns).
- **Description:** "Brand one enemy with embers for [X] Fire damage, with a 50% chance to leave it Burning for 3 turns, taking 10% of that each turn."
- **[X], now → reworked:** Level 1: 11 → 9; Level 100: 172 → 133.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Fire Bolt's patient sibling: less on the hit, a long burn more often; healing loss now belongs to Death's Withered.

### Fire Whip: Tier 2, offensive, one target

- **Now:** 3 AP, 27 Mana, power 14 + Int ×0.6 + Wis ×0.1. Lash out with a strike of fire that deals [X] Fire damage.
- **Reworked:** 2 AP, 15 Mana; power 6 + Int ×0.35 + Wis ×0.1 (65% of the tier's reference, the 2 AP rule). A quick 2 AP lash; a 25% chance to set the target **Burning** for 2 turns (15% of the hit each turn).
- **Description:** "Lash one enemy with a whip of flame for [X] Fire damage, with a 25% chance to set it Burning for 2 turns."
- **[X], now → reworked:** Level 10: 42 → 24; Level 100: 252 → 154.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Today a plain bolt; it becomes Fire's fast spell, to fill the 2 AP left after a 3 AP spell.

### Summon - Fire Spirit: Tier 2, summon

- **Now:** 3 AP, 25 Mana, power none. Summon a friendly Fire Spirit to aid you in battle.
- **Reworked:** 3 AP, 25 Mana; power 9 + Int ×0.54 + Wis ×0.16 (100% of the tier's reference, as a summon). Summons a Fire Spirit (Level 8, 68 HP today); its maximum HP rises by 200% of [X] and every attack and spell it makes by 25% of [X] (SP29, as the sample's Stone Giant). Its attacks and spells have a 30% chance to set their target **Burning** for 2 turns (15% of the hit each turn). One at a time.
- **Description:** "Summon a friendly Fire Spirit whose maximum HP rises by twice [X] and whose attacks and spells deal a quarter of [X] more, each with a 30% chance to set its target Burning. Only one may be active at a time."
- **[X], now → reworked:** Level 10: - → 36; Level 100: - → 239.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Today a Level 8 spirit fades after the early game; it now grows with its caster's spell power.

### Burning Cloak: Tier 2, defensive, yourself

- **Now:** 3 AP, 20 Mana, power 12 + Int ×0.4 + Wis ×0.1. Cloak yourself in flame for 2 turns. Melee attacks against you deal [X] Fire damage to the attacker.
- **Reworked:** 3 AP, 20 Mana; power 4 + Int ×0.27 + Wis ×0.08 (50% of the tier's reference, per retaliation). For 2 turns, every melee attacker takes [X] Fire damage and has a 50% chance to be set **Burning** for 2 turns (15% of [X] each turn).
- **Description:** "Wrap yourself in a cloak of flame for 2 turns: melee attackers take [X] Fire damage, with a 50% chance to be set Burning for 2 turns."
- **[X], now → reworked:** Level 10: 32 → 18; Level 100: 178 → 119.
- **Augments:** Power, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Today's retaliation, with the element's status added.

### Ashfall: Tier 2, offensive, all enemies

- **Now:** 3 AP, 26 Mana, power 9 + Int ×0.5 + Wis ×0.2. Scatter embers over all enemies for [X] Fire damage and reduce their healing received by 10% for 2 turns.
- **Reworked:** 3 AP, 26 Mana; power 6 + Int ×0.38 + Wis ×0.11 (70% of the tier's reference, 75% for all enemies, less the blinding ash). The falling ash lowers every enemy's Accuracy by 10% for 2 turns (was healing received −10%).
- **Description:** "Bring down burning ash on all enemies for [X] Fire damage and reduce their Accuracy by 10% for 2 turns."
- **[X], now → reworked:** Level 10: 36 → 25; Level 100: 234 → 167.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Healing loss belongs to Death; ash in the eyes suits Fire and protects the party.

### Dragonbreath - Cinderstorm: Tier 3, offensive, all enemies (Dragonbreath)

- **Now:** 3 AP, 38 Mana, power 32 + Int ×0.6 + Wis ×0.2. Exhale a storm of burning cinders that deals [X] Fire damage to all enemies and decreases their Vitality by 8% for 2 turns.
- **Reworked:** 3 AP, 38 Mana; power 8 + Int ×0.5 + Wis ×0.14 (65% of the tier's reference, 75% for all enemies, less its effects). Its weakening effect stays (Vitality −8% for 2 turns); each enemy struck has a 25% chance to be **Burning** for 2 turns (15% of the hit each turn).
- **Description:** "Exhale a storm of cinders across all enemies for [X] Fire damage, reducing their Vitality by 8% for 2 turns, with a 25% chance to set each Burning for 2 turns."
- **[X], now → reworked:** Level 20: 89 → 54; Level 100: 293 → 219.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Dragonbreath spells keep their weakening effect; the rest follows the curve.

### Summon - Greater Fire Spirit: Tier 3, summon

- **Now:** 3 AP, 40 Mana, power none. Summon a strengthened Fire Spirit with 50% increased max HP and 25% increased attack damage.
- **Reworked:** 3 AP, 40 Mana; power 13 + Int ×0.77 + Wis ×0.22 (100% of the tier's reference, as a summon). Summons a Greater Fire Spirit (today the same Level 8 spirit with +50% HP and +25% damage); its maximum HP rises by 200% of [X] and every attack and spell it makes by 25% of [X] (SP29, as the sample's Stone Giant). Its attacks and spells have a 50% chance to set their target **Burning** for 2 turns; when it falls, it bursts on every enemy for half of [X] Fire damage (new). One at a time.
- **Description:** "Summon a friendly Greater Fire Spirit whose maximum HP rises by twice [X] and whose attacks and spells deal a quarter of [X] more, each with a 50% chance to set its target Burning; when it falls, it bursts on all enemies for half of [X] Fire damage. Only one may be active at a time."
- **[X], now → reworked:** Level 20: - → 84; Level 100: - → 339.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Today it calls the very same spirit as Summon - Fire Spirit; it now grows with a Tier 3 spell's power and ends in a burst.

### Phoenix Mantle: Tier 3, defensive, yourself or an ally

- **Now:** 3 AP, 27 Mana, power 28 + Int ×0.6 + Wis ×0.2. Surround yourself with living flame for 3 turns. Melee attackers take [X] Fire damage and you take 25% less Fire spell damage.
- **Reworked:** 3 AP, 27 Mana. For 3 turns: 40% less Fire damage taken, can't be set Burning, and any Chill or Frozen on them ends at once and can't return (was melee retaliation and 25% less Fire spell damage on yourself).
- **Description:** "Wrap yourself or an ally in a phoenix's warmth for 3 turns: 40% less Fire damage, no Burning, and any Chill or Freeze thawed and kept away."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Burning Cloak already retaliates; the Mantle becomes the party's shield against fire and ice.

### Flame Spiral: Tier 3, offensive, all enemies

- **Now:** 3 AP, 37 Mana, power 28 + Int ×0.6 + Wis ×0.2. Swirl flames across all enemies for [X] Fire damage and reduce their Speed by 10% for 2 turns.
- **Reworked:** 3 AP, 37 Mana; power 8 + Int ×0.5 + Wis ×0.14 (65% of the tier's reference, 75% for all enemies, less the second strike). Every enemy already **Burning** is struck a second time for 40% of [X] (new; was Speed −10%).
- **Description:** "Send a spiral of flame through all enemies for [X] Fire damage; each one already Burning is struck again for 40% of that."
- **[X], now → reworked:** Level 20: 85 → 54; Level 100: 289 → 219.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** A follow-up for a burning field: weak alone, strong after a Firewall.

### Inferno Orb: Tier 4, offensive, one target

- **Now:** 3 AP, 52 Mana, power 45 + Int ×0.7 + Wis ×0.2. Hurl a sphere of white-hot flame that deals [X] Fire damage and decreases the target's Vitality by 10% for 3 turns.
- **Reworked:** 3 AP, 52 Mana; power 14 + Int ×0.8 + Wis ×0.23 (85% of the tier's reference). The orb splashes every other enemy for 30% of [X] (new); a 30% chance to set the target **Burning** for 2 turns (15% of the hit each turn). (Was Vitality −10% for 3 turns.)
- **Description:** "Hurl an orb of fire at one enemy for [X] Fire damage that splashes every other enemy for 30% of that, with a 30% chance to set the target Burning for 2 turns."
- **[X], now → reworked:** Level 30: 139 → 121; Level 100: 342 → 353.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** A single-target spell that still touches the rest of a group.

### Hellfire Wave: Tier 4, offensive, all enemies

- **Now:** 3 AP, 55 Mana, power 35 + Int ×0.7 + Wis ×0.2. Sweep hellfire over all enemies for [X] Fire damage and decrease their Vitality by 10% for 3 turns.
- **Reworked:** 3 AP, 55 Mana; power 10 + Int ×0.61 + Wis ×0.18 (65% of the tier's reference, 75% for all enemies, less the hotter burn). Each enemy struck has a 40% chance to be **Burning** for 3 turns, for 20% of the hit each turn (hellfire burns hotter; was Vitality −10% for 3 turns).
- **Description:** "Send a wave of hellfire through all enemies for [X] Fire damage; each has a 40% chance to burn for 20% of that at the start of each of its next 3 turns."
- **[X], now → reworked:** Level 30: 129 → 92; Level 100: 332 → 270.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** The element's mid-tier group burn.

### Flame Rebirth: Tier 4, defensive, yourself or an ally

- **Now:** 3 AP, 43 Mana, power 40 + Int ×0.7 + Wis ×0.2. Draw vitality from a rebirthing flame, restoring [X] HP plus 15% of your maximum HP and increasing Fire spell power by 15% for 2 turns.
- **Reworked:** 3 AP, 43 Mana; power 19 + Wis ×1.13 + Int ×0.32 (120% of the tier's reference, as the HP they rise with). For 3 turns, if they fall they rise again at once with [X] HP (once; the Phoenix Plume's rising, on a spell). Was a heal and +15% Fire spell power for 2 turns.
- **Description:** "Mark yourself or an ally with the phoenix: if they fall within the next 3 turns, they rise again with [X] HP."
- **[X], now → reworked:** Level 30: 134 → 140; Level 100: 337 → 397.
- **Augments:** Power, Effect duration, Mana cost.
- **Why:** Rebirth was only in the name; it now does what a phoenix does.

### Ember Lance: Tier 4, offensive, one target

- **Now:** 3 AP, 50 Mana, power 48 + Int ×0.7 + Wis ×0.2. Impale one enemy for [X] Fire damage and reduce its healing received by 15% for 2 turns.
- **Reworked:** 3 AP, 50 Mana; power 14 + Int ×0.85 + Wis ×0.24 (90% of the tier's reference). Deals 30% more to a target that is **Burning** (was healing received −15% for 2 turns).
- **Description:** "Drive a lance of embers through one enemy for [X] Fire damage, 30% more if it is Burning."
- **[X], now → reworked:** Level 30: 142 → 127; Level 100: 345 → 373.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** The single-target payoff for Fire's burns.

### Dragonbreath - Starfire: Tier 5, offensive, all enemies (Dragonbreath)

- **Now:** 3 AP, 79 Mana, power 43 + Int ×0.8 + Wis ×0.2. Exhale star-bright flames across all enemies for [X] Fire damage and reduce their Vitality by 10% for 2 turns.
- **Reworked:** 3 AP, 79 Mana; power 14 + Int ×0.79 + Wis ×0.23 (65% of the tier's reference, 75% for all enemies, less its effects). Its weakening effect stays (Vitality −10% for 2 turns); each enemy struck has a 25% chance to be **Burning** for 2 turns (15% of the hit each turn).
- **Description:** "Exhale starfire across all enemies for [X] Fire damage, reducing their Vitality by 10% for 2 turns, with a 25% chance to set each Burning for 2 turns."
- **[X], now → reworked:** Level 40: 181 → 153; Level 100: 376 → 350.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Dragonbreath spells keep their weakening effect.

### Solar Lance: Tier 5, offensive, one target

- **Now:** 3 AP, 76 Mana, power 61 + Int ×0.8 + Wis ×0.2. Sear one enemy for [X] Fire damage, melting away 15% of its Armor for 2 turns.
- **Reworked:** 3 AP, 76 Mana; power 20 + Int ×1.16 + Wis ×0.33 (95% of the tier's reference). The target's resistance counts 30 points lower against it; a 50% chance to set it **Burning** for 2 turns (15% of the hit each turn). (Was Armor −15% for 2 turns.)
- **Description:** "Pierce one enemy with sunlight for [X] Fire damage that cuts through 30 points of resistance, with a 50% chance to set it Burning for 2 turns."
- **[X], now → reworked:** Level 40: 199 → 223; Level 100: 394 → 511.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Six Fire lances all lowered Armor; each now does one thing of its own, and Armor loss stays with Sunforge Lance (and Earth).

### Summon - Fire Elemental: Tier 5, summon

- **Now:** 3 AP, 82 Mana, power none. Summon a friendly Fire Elemental. Only one may be active at a time.
- **Reworked:** 3 AP, 82 Mana; power 21 + Int ×1.22 + Wis ×0.35 (100% of the tier's reference, as a summon). Summons a Fire Elemental (Level 25, 225 HP today); its maximum HP rises by 200% of [X] and every attack and spell it makes by 25% of [X] (SP29, as the sample's Stone Giant). Its hits have a 40% chance to set their target **Burning** for 2 turns; while it stands, its side's Fire spells gain 10% power (new). One at a time.
- **Description:** "Summon a friendly Fire Elemental whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more, each with a 40% chance to set its target Burning; while it stands, your side's Fire spells gain 10% power. Only one may be active at a time."
- **[X], now → reworked:** Level 40: - → 235; Level 100: - → 538.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Fire's strongest summon also feeds the element's spells while it lives.

### Burning Horizon: Tier 5, offensive, all enemies

- **Now:** 3 AP, 78 Mana, power 43 + Int ×0.8 + Wis ×0.2. Scorch all enemies for [X] Fire damage, each with a 10% chance to be stunned for 1 turn.
- **Reworked:** 3 AP, 78 Mana; power 14 + Int ×0.79 + Wis ×0.23 (65% of the tier's reference, 75% for all enemies, less its effects). Enemies already **Burning** take 30% more; each of the others has a 50% chance to be set Burning for 2 turns (15% of the hit each turn). (Was a 10% stun chance on each.)
- **Description:** "Set the whole horizon alight for [X] Fire damage to all enemies, 30% more to those already Burning; each of the others has a 50% chance to catch fire for 2 turns."
- **[X], now → reworked:** Level 40: 181 → 153; Level 100: 376 → 350.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Fire's group payoff: it finishes a burning field or starts one.

### Dragonbreath - Crown of Embers: Tier 6, offensive, all enemies (Dragonbreath)

- **Now:** 3 AP, 96 Mana, power 52 + Int ×0.9 + Wis ×0.2. Bathe all enemies in dragonfire for [X] Fire damage and reduce their healing received by 25% for 3 turns.
- **Reworked:** 3 AP, 96 Mana; power 16 + Int ×0.96 + Wis ×0.27 (65% of the tier's reference, 75% for all enemies, less its effects). Its weakening effect stays (healing received −25% for 3 turns); each enemy struck has a 30% chance to be **Burning** for 2 turns (15% of the hit each turn).
- **Description:** "Crown all enemies in embers for [X] Fire damage, reducing their healing received by 25% for 3 turns, with a 30% chance to set each Burning for 2 turns."
- **[X], now → reworked:** Level 50: 240 → 223; Level 100: 420 → 422.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Dragonbreath spells keep their weakening effect, even where it overlaps Death's Withered.

### Phoenix Spear: Tier 6, offensive, one target

- **Now:** 3 AP, 104 Mana, power 70 + Int ×0.9 + Wis ×0.2. Hurl concentrated flame for [X] Fire damage and reduce the target's Armor by 20% for 2 turns.
- **Reworked:** 3 AP, 104 Mana; power 24 + Int ×1.4 + Wis ×0.4 (95% of the tier's reference). If it defeats its target, you regain half of its Mana cost (new; was Armor −20% for 2 turns).
- **Description:** "Hurl a phoenix spear at one enemy for [X] Fire damage; if it falls, half of the spell's Mana returns to you."
- **[X], now → reworked:** Level 50: 258 → 327; Level 100: 438 → 617.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** A finisher that pays for itself, as the phoenix rises from what it burns.

### Phoenix Ward: Tier 6, defensive, all allies

- **Now:** 3 AP, 100 Mana, power 58 + Int ×0.9 + Wis ×0.2. For 2 turns, melee attackers take [X] Fire damage from you and your healing received increases by 30%.
- **Reworked:** 3 AP, 100 Mana; power 10 + Int ×0.59 + Wis ×0.17 (40% of the tier's reference, per retaliation, for all allies). For 2 turns, every melee attacker of any ally takes [X] Fire damage and has a 30% chance to be set **Burning** for 2 turns (was retaliation and +30% healing received on yourself).
- **Description:** "Ward the whole party with phoenix fire for 2 turns: melee attackers take [X] Fire damage, with a 30% chance to be set Burning for 2 turns."
- **[X], now → reworked:** Level 50: 246 → 138; Level 100: 426 → 260.
- **Augments:** Power, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Burning Cloak for the party, the answer to a pack of melee enemies.

### Cinder Flood: Tier 7, offensive, all enemies

- **Now:** 3 AP, 134 Mana, power 64 + Int ×0.9 + Wis ×0.3. Bathe all enemies in dragonfire for [X] Fire damage and reduce healing received by 20% for 2 turns.
- **Reworked:** 3 AP, 134 Mana; power 19 + Int ×1.09 + Wis ×0.31 (60% of the tier's reference, 75% for all enemies, less its likely burn). Each enemy struck has a 70% chance to be **Burning** for 3 turns, for 15% of the hit each turn (was healing received −20%).
- **Description:** "Flood the field with cinders for [X] Fire damage to all enemies; each has a 70% chance to burn for 15% of that at the start of each of its next 3 turns."
- **[X], now → reworked:** Level 60: 303 → 300; Level 100: 456 → 480.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** The late Firewall: a field set burning for Wind's Wildfire and Flame Spiral.

### Sunforge Lance: Tier 7, offensive, one target

- **Now:** 3 AP, 146 Mana, power 82 + Int ×0.9 + Wis ×0.3. Pierce one enemy for [X] Fire damage and reduce Armor by 25% for 2 turns.
- **Reworked:** 3 AP, 146 Mana; power 26 + Int ×1.55 + Wis ×0.44 (85% of the tier's reference). Melts the target's Armor by 20% for 2 turns (kept from today's −25%, the one Fire lance that still does).
- **Description:** "Strike one enemy with a sunforged lance for [X] Fire damage and melt its Armor by 20% for 2 turns."
- **[X], now → reworked:** Level 60: 321 → 425; Level 100: 474 → 682.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Kept as Fire's one armor-melter, for a party with weapon users.

### Phoenix Renewal: Tier 7, defensive, yourself or an ally, fallen or not

- **Now:** 3 AP, 140 Mana, power 64 + Int ×0.9 + Wis ×0.3. Restore [X] HP plus 15% maximum HP to an ally or yourself and remove one debuff.
- **Reworked:** 3 AP, 140 Mana; power 31 + Wis ×1.82 + Int ×0.52 (100% of the tier's reference, as a heal). Revives a fallen ally with [X] HP plus 20% of their maximum HP (new); on a living ally, heals [X] and removes all their debuffs (was a heal and one debuff removed).
- **Description:** "Call the phoenix on an ally: a fallen one rises with [X] HP plus 20% of maximum HP; a living one is healed for [X] and cleansed of every debuff."
- **[X], now → reworked:** Level 60: 303 → 404; Level 100: 456 → 641.
- **Augments:** Power, Mana cost.
- **Why:** Renewal becomes the phoenix's true gift, a revival, at the tier where fights are long enough to need it.

### Dragonbreath - Worldfire: Tier 8, offensive, all enemies (Dragonbreath)

- **Now:** 3 AP, 170 Mana, power 70 + Int ×1 + Wis ×0.4. Incinerate all enemies for [X] Fire damage and reduce healing received by 30% for 2 turns.
- **Reworked:** 3 AP, 170 Mana; power 23 + Int ×1.37 + Wis ×0.39 (65% of the tier's reference, 75% for all enemies, less its effects). Its weakening effect stays (healing received −30% for 2 turns); each enemy struck has a 35% chance to be **Burning** for 2 turns (15% of the hit each turn).
- **Description:** "Exhale the world's fire across all enemies for [X] Fire damage, reducing their healing received by 30% for 2 turns, with a 35% chance to set each Burning for 2 turns."
- **[X], now → reworked:** Level 70: 389 → 433; Level 100: 521 → 603.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Dragonbreath spells keep their weakening effect.

### Starfire Spear: Tier 8, offensive, one target

- **Now:** 3 AP, 190 Mana, power 90 + Int ×1 + Wis ×0.4. Pierce one enemy for [X] Fire damage and reduce Armor by 30% for 2 turns.
- **Reworked:** 3 AP, 190 Mana; power 36 + Int ×2.1 + Wis ×0.6 (100% of the tier's reference). Pierces through to a second, random enemy for 60% of [X] (new; was Armor −30% for 2 turns).
- **Description:** "Hurl a spear of starfire through one enemy for [X] Fire damage and on into another for 60% of that."
- **[X], now → reworked:** Level 70: 409 → 664; Level 100: 541 → 926.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** A single-target spell that still wins against two.

### Ashfall Crown: Tier 9, offensive, all enemies

- **Now:** 3 AP, 240 Mana, power 85 + Int ×1.1 + Wis ×0.4. Scorch all enemies for [X] Fire damage and reduce healing received by 25% for 2 turns.
- **Reworked:** 3 AP, 240 Mana; power 27 + Int ×1.57 + Wis ×0.45 (60% of the tier's reference, 75% for all enemies, less its sure burn). Every enemy struck is always **Burning** for 3 turns, for 20% of the hit each turn (was healing received −25%).
- **Description:** "Crown the enemy line in falling fire for [X] Fire damage; every one of them burns for 20% of that at the start of each of its next 3 turns."
- **[X], now → reworked:** Level 80: 477 → 562; Level 100: 572 → 692.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Fire's great group spell: a sure, hot burn on everyone.

### Solar Pike: Tier 9, offensive, one target

- **Now:** 3 AP, 260 Mana, power 95 + Int ×1.1 + Wis ×0.4. Burn through one enemy for [X] Fire damage and reduce Armor by 25% for 2 turns.
- **Reworked:** 3 AP, 260 Mana; power 36 + Int ×2.1 + Wis ×0.6 (80% of the tier's reference). The target is always **Burning** for 3 turns, for 25% of the hit each turn (was Armor −25% for 2 turns).
- **Description:** "Drive a pike of the sun into one enemy for [X] Fire damage; it burns for 25% of that at the start of each of its next 3 turns."
- **[X], now → reworked:** Level 80: 487 → 752; Level 100: 582 → 926.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** The heaviest single burn in the game, 155% of [X] in all over its turns.

### The First Flame: Tier 10, offensive, all enemies

- **Now:** 3 AP, 350 Mana, power 240 + Int ×1.2 + Wis ×0.5. Unleash the fire before all other fires for [X] Fire damage to all enemies, reduce healing received by 50% for 3 turns and reduce Armor by 20% for 2 turns.
- **Reworked:** 3 AP, 350 Mana; power 45 + Int ×2.61 + Wis ×0.75 (65% of the tier's reference, 75% for all enemies, less its effects). Every enemy struck is always **Burning** for 3 turns, for 25% of the hit each turn, even one immune to Burning; healing received −50% for 3 turns stays (its Armor −20% goes).
- **Description:** "Call down the first flame on all enemies for [X] Fire damage; they burn for 25% of that at the start of each of their next 3 turns, even those fire can't touch, and their healing received is halved for 3 turns."
- **[X], now → reworked:** Level 90: 732 → 1,043; Level 100: 786 → 1,152.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Fire's Tier 10: the flame no creature is immune to.

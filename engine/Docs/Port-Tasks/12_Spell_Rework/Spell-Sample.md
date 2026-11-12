# The sample: four reworked spells (task 12)

**Status: approved (2026-11-11, SP31); then updated for SP32 (each spell sets its status's chance and duration):
Fire Bolt now burns on a 20% chance with its hit raised to match, the Stone Giant Cracks on a 40% chance.** One spell of each kind, from four elements, to settle the style
before the element batches: a low-tier single-target attack, a mid-tier all-enemies attack, a defensive spell and a
summon. They follow the approved system (`Spell-Augmentation.md`: the curve, the all-enemies rule, the attributes) and
the element statuses and reactions approved beside this sample (`Element-Statuses.md`, SP30). "(new)" marks a
mechanic the game doesn't have yet.

The numbers by level are for a raw caster (a Human with half of every level's points in Intelligence and a quarter
each in Mind and Wisdom, no classes or gear; Level 100: Intelligence 357, Wisdom 233, Speed 109), as in the system
draft. "Now" is the old game's spell as it is.

## 1. Fire Bolt: Tier 1 Fire, offensive, one target

| | Now | Reworked |
|---|---|---|
| AP, Mana | 3 AP, 12 Mana | 3 AP, 12 Mana |
| Power | 7 + Int ×0.4 + Wis ×0.1 | 6 + Int ×0.33 + Wis ×0.10 (about 95% of Tier 1's reference: it may also burn) |
| Effect | none | a 20% chance to set the target **Burning** for 2 turns, for 15% of the hit each turn (new) |
| Price | 650 | 650 |

**Description:** "Hurl a bolt of fire at one enemy for [X] Fire damage, with a 20% chance to set it Burning: it
takes 15% of that again at the start of each of its next 2 turns."

| Level | 1 | 20 | 40 | 60 | 80 | 100 |
|---|---|---|---|---|---|---|
| Now: the hit | 12 | 43 | 76 | 108 | 141 | 173 |
| Reworked: the hit (+ Burning, 20%) | 10 (+ 1 + 1) | 37 (+ 5 + 5) | 64 (+ 10 + 10) | 92 (+ 14 + 14) | 120 (+ 18 + 18) | 147 (+ 22 + 22) |

**Why:** a cheap damage spell whose goal is the hit, so it burns only now and then (SP32); Burning is what Wind spreads
(Wildfire) and Water puts out (Steam). With the burn's chance counted, the total stays about today's (SP22).
**Augments:** Power, Resistance penetration, Effect strength (the burn's share), Effect duration (its turns), Effect
chance (the 20%), Mana cost.

## 2. Chain Resonance: Tier 5 Lightning, offensive, all enemies

| | Now | Reworked |
|---|---|---|
| AP, Mana | 3 AP, 75 Mana | 3 AP, 75 Mana |
| Power | 42 + Int ×0.7 + Speed ×0.3 | 21 + Int ×1.22 + Speed ×0.35 (Tier 5's reference) |
| Targets | all enemies, the same damage | the chosen enemy for [X], then it arcs to every other enemy for 70% of [X] (new): about 75% on average with three or four enemies (SP18) |
| Effect | Speed −10% for 3 turns | each enemy struck: a 25% chance to be **Shocked** (loses 1 AP at its next turn) |
| Price | 8,700 | 8,700 |

**Description:** "Strike one enemy with lightning for [X] Lightning damage; it arcs on to every other enemy for 70% of
that. Each enemy struck has a 25% chance to be Shocked."

| Level | 40 | 60 | 80 | 100 |
|---|---|---|---|---|
| Now: every enemy | 160 | 215 | 270 | 325 |
| Reworked: the chosen enemy / the others | 217 / 152 | 310 / 217 | 402 / 282 | 495 / 346 |

**Why:** a chain, not a flat wave, so whom you strike first matters; on Soaked enemies it also sets off Electrocute.
The Speed debuff goes, as Speed loss now belongs to Water and Ice. Tier 5 gains power (SP22). **Augments:** Power,
Resistance penetration, Effect chance (Shocked), Mana cost.

## 3. Greater Heal: Tier 3 Holy, defensive, yourself or an ally

| | Now | Reworked |
|---|---|---|
| AP, Mana | 3 AP, 34 Mana | 3 AP, 35 Mana |
| Power | 30 + Wis ×0.5 + Int ×0.3 | 12 + Wis ×0.70 + Int ×0.20 |
| Effect | heals [X] + 10% of maximum HP | heals [X] + 5% of maximum HP, and always **Blesses** them for 2 turns: 20% of [X] at the start of each of their turns (new; certain, as Blessing is half of what the spell is for) |
| Reaction | none | **Purify:** ends Hexed and Withered on them |
| Price | 3,300 | 3,300 |

**Description:** "Restore [X] HP plus 5% of maximum HP to yourself or an ally and Bless them: they regain 20% of [X]
at the start of each of their next 2 turns. Ends Hexed and Withered."

| Level | 20 | 40 | 60 | 80 | 100 |
|---|---|---|---|---|---|
| Now | 79 + 10% HP | 123 + 10% HP | 166 + 10% HP | 210 + 10% HP | 253 + 10% HP |
| Reworked: the heal + Blessed | 64 + 5% HP + 13 + 13 | 110 + 5% HP + 22 + 22 | 156 + 5% HP + 31 + 31 | 201 + 5% HP + 40 + 40 | 246 + 5% HP + 49 + 49 |

**Why:** today's heals at Tiers 3–4 (Greater Heal, Mending Current, Surging Cure, Regrowth, Divine Restoration, …)
are nearly the same spell in different elements; Holy's heals now carry Blessed and Purify, Water's and Nature's will
get their own turn in their batches. About the same total as now for an ally with 1,000 HP. **Augments:** Power (the
heal), Effect strength (Blessed's share), Effect duration (Blessed's turns), Mana cost.

## 4. Summon - Stone Giant: Tier 7 Earth, summon

| | Now | Reworked |
|---|---|---|
| AP, Mana | 3 AP, 140 Mana | 3 AP, 140 Mana |
| Power | none | 31 + Int ×1.82 + Wis ×0.52 (Tier 7's reference; it drives the giant, SP29) |
| Effect | summons a Level 48 Stone Giant (1,518 HP) with +15% maximum HP; one at a time | summons the Level 48 Stone Giant; its maximum HP rises by 200% of [X] and every attack it makes by 25% of [X] (new); each of its hits has a 40% chance to **Crack** the target for 3 turns (1 stack, new); one at a time |
| Price | 19,000 | 19,000 |

**Description:** "Summon a friendly Stone Giant whose maximum HP rises by twice [X] and whose attacks deal a quarter of
[X] more. Each of its blows has a 40% chance to Crack its target for 3 turns. Only one may be active at a time."

| Caster's level | 60 | 80 | 100 |
|---|---|---|---|
| Now: the giant's HP | 1,745 | 1,745 | 1,745 |
| Reworked: HP / extra damage per attack | 2,519 / +125 | 2,820 / +163 | 3,122 / +200 |

**Why:** today's giant stays a Level 48 giant however strong its caster is; it now grows with spell power (SP29), and
its Cracks open enemies to the party's weapons and to Nature's Overgrowth. **Augments (SP26):** Mana cost, Summon
Vitality, Summon Might, Summon Speed. Power isn't offered on summons: Vitality and Might already raise what it
drives.

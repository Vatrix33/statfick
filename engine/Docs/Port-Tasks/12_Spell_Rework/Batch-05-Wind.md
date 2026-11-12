# Batch 5: Wind (task 12)

**Status: a draft for the user's review (SP2).** Every Wind spell players can learn, reworked: 32 spells (Wind has no
boss-only spell). They follow the approved system and curve (`Spell-Augmentation.md`), the sample's style
(`Spell-Sample.md`) and the element statuses (`Element-Statuses.md`): each spell sets its status's chance and duration
(SP32). Tiers and prices stay. "(new)" marks a mechanic the game doesn't have yet. [X] values are for the raw caster of
the system draft (no classes or gear) at the level the tier unlocks and at Level 100, shown for a Wisdom-first caster
as Wind's spells scale with Wisdom and Speed.
**Updated 2026-11-12 (SP60):** some spells here borrow another element's status; each says so ("borrowed, SP60").

## Wind's identity

The element of speed and of time in battle:
- **Off-Balance** (its status: Evasion and Accuracy −15%) sets up Earth's **Topple**; its own reaction, **Wildfire**,
  spreads Burning from one enemy to all. Wind Shear, Hurricane Choir and Skyglass Spear put enemies Off-Balance for
  certain; Crosswind and Sky Scythe often.
- **Time:** Speed decides how often everyone acts, and Wind now moves turns directly (new): Backwind, Gale Step,
  Skyward Rush, Skyfall Chorus and Crown of the Open Sky bring your side's next turns sooner; Tempest and Pressure
  Vortex push enemies' back. Tailwind, Minor Boost, Eye of the Gale and the Wind Elemental raise Speed.
- **Fast spells:** Quick Gust and, at Tier 7, Tempest Needle cost 2 AP; Zephyr Execution returns 2 AP on a kill.
- **Dodging:** Gale Step, Cyclone Guard, Aerial Riposte (striking back at every miss), Deflect Missiles and Tempest
  Veil (enemy spells and ranged attacks may miss).
- Gale Step and Cyclone Guard used their power as an Evasion percentage, which grew to about +150% at Level 100; they
  give set amounts now. Backwind was Tailwind at twice the Mana. Four spells all hit and raised your Speed; only Sky
  Cutter still does. Control Weather stays as it is.

New mechanics this batch adds for the port: turns brought sooner or pushed back (the turn gauge), immunity to Speed
loss, damage from a Speed difference, AP returned on a kill, striking back at a missed attack, a summon that raises its
side's Speed, two gusts in one spell.

## At a glance

| Tier | Spell | Kind | AP | Mana | What changes |
|---|---|---|---|---|---|
| 1 | Wind Bolt | offensive, one target | 3 | 12 | 20% chance to put Off-Balance |
| 1 | Minor Boost | defensive, yourself | 3 | 10 | Speed +10% for 3 turns |
| 1 | Quick Gust | offensive, one target | 2 | 8 | 2 AP kept, 25% Off-Balance |
| 1 | Tailwind | defensive, all allies | 3 | 14 | every ally's Speed +10% (kept) |
| 2 | Backwind | defensive, yourself or an ally | 3 | 22 | their next turn comes sooner |
| 2 | Allgust | offensive, all enemies | 3 | 25 | 25% Off-Balance on each enemy |
| 2 | Deflect Missiles | defensive, all allies | 3 | 27 | enemy ranged hit chance −30% for 3 turns |
| 2 | Crosswind | offensive, all enemies | 3 | 25 | 50% Off-Balance on each enemy |
| 3 | Razor Squall | offensive, all enemies | 3 | 38 | two gusts at every enemy, 20% Off-Balance and 15% Bleed each (borrowed status) |
| 3 | Gale Step | defensive, yourself | 3 | 34 | Evasion +30% and your next turn comes sooner |
| 3 | Sky Cutter | offensive, one target | 3 | 33 | you gain Speed +10% (kept) |
| 3 | Wind Shear | offensive, one target | 3 | 35 | always Off-Balance |
| 4 | Tempest | offensive, all enemies | 3 | 54 | every enemy's next turn comes 20% later |
| 4 | Skyward Rush | offensive, one target | 3 | 49 | your next turn comes 30% sooner |
| 4 | Cyclone Guard | defensive, all allies | 3 | 47 | every ally's Evasion +20% for 3 turns |
| 4 | Eye of the Gale | defensive, all allies | 3 | 52 | every ally's Speed +15%, no Speed loss |
| 5 | Sky Scythe | offensive, all enemies | 3 | 74 | 50% Off-Balance on each enemy |
| 5 | Pressure Vortex | offensive, one target | 3 | 74 | its next turn comes 40% later; can't flee |
| 5 | Gale Lance | offensive, one target | 3 | 72 | +1% per point of Speed you have over the target (up to +40%) |
| 5 | Dragonbreath - Razor Gale | offensive, all enemies | 3 | 77 | 10% stun kept (Dragonbreath), 25% Off-Balance |
| 5 | Summon - Wind Elemental | summon | 3 | 82 | while it stands, your side's Speed +10% |
| 6 | Hurricane Choir | offensive, all enemies | 3 | 96 | every enemy Off-Balance for certain |
| 6 | Zephyr Execution | offensive, one target | 3 | 104 | a kill gives you 2 AP back |
| 6 | Aerial Riposte | defensive and offensive, yourself | 3 | 100 | Evasion +20%; every miss against you is struck back |
| 7 | Control Weather | the whole field | 3 | 160 | kept: choose the weather for 15 turns |
| 7 | Skyfall Chorus | offensive, all enemies | 3 | 130 | every ally's next turn comes 20% sooner |
| 7 | Tempest Needle | offensive, one target | 2 | 98 | 2 AP, Speed −25% kept |
| 8 | Dragonbreath - Sky Rupture | offensive, all enemies | 3 | 170 | Evasion −30% kept (Dragonbreath), 30% Off-Balance |
| 8 | Tempest Lancer | offensive, one target | 3 | 190 | Speed −30% kept |
| 9 | Tempest Veil | offensive, all enemies; defensive, all allies | 3 | 240 | enemy spells and ranged attacks 20% likely to miss your side |
| 9 | Skyglass Spear | offensive, one target | 3 | 260 | Off-Balance for 3 turns, 20% stun kept |
| 10 | Crown of the Open Sky | offensive, all enemies | 3 | 350 | Evasion −40% and Speed −25% kept; every ally's next turn 50% sooner |

## The spells

### Wind Bolt: Tier 1, offensive, one target

- **Now:** 3 AP, 12 Mana, power 5 + Wis ×0.3 + Speed ×0.2. A bolt of wind that deals [X] Wind damage.
- **Reworked:** 3 AP, 12 Mana; power 6 + Wis ×0.33 + Speed ×0.1 (95% of the tier's reference). A 20% chance to put the target **Off-Balance** for 2 turns.
- **Description:** "A bolt of wind that deals [X] Wind damage, with a 20% chance to put the target Off-Balance for 2 turns."
- **[X], now → reworked:** Level 1: 10 → 10; Level 100: 133 → 135.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Wind's plain bolt, with the element's status (which Earth's Topple uses).

### Minor Boost: Tier 1, defensive, yourself

- **Now:** 3 AP, 10 Mana, power none. Increase Speed's permanent bonus by 5% for 5 turns.
- **Reworked:** 3 AP, 10 Mana. Speed +10% for 3 turns on yourself (was +5% for 5 turns).
- **Description:** "Lighten your step with the wind, increasing your Speed by 10% for 3 turns."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Speed decides how often you act; a short, real boost suits a Tier 1 spell better than a long, small one.

### Quick Gust: Tier 1, offensive, one target

- **Now:** 2 AP, 5 Mana, power 1 + Wis ×0.2 + Speed ×0.1. A fast gust of wind that deals [X] Wind damage. This spell costs 2 Action Points.
- **Reworked:** 2 AP, 8 Mana; power 4 + Wis ×0.23 + Speed ×0.07 (65% of the tier's reference, the 2 AP rule). Stays a 2 AP spell (now 8 Mana, from 5); a 25% chance to put the target **Off-Balance** for 2 turns.
- **Description:** "A fast gust of wind that deals [X] Wind damage, with a 25% chance to put the target Off-Balance for 2 turns. This spell costs 2 AP."
- **[X], now → reworked:** Level 1: 4 → 7; Level 100: 83 → 94.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Already Wind's fast spell; its power and Mana follow the 2 AP rule.

### Tailwind: Tier 1, defensive, all allies

- **Now:** 3 AP, 14 Mana, power none. Increase every ally's Speed by 10% for 2 turns.
- **Reworked:** 3 AP, 14 Mana. Every ally's Speed +10% for 2 turns (kept).
- **Description:** "Send a tailwind behind the party, increasing every ally's Speed by 10% for 2 turns."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Kept: the cheap party haste.

### Backwind: Tier 2, defensive, yourself or an ally

- **Now:** 3 AP, 27 Mana, power none. Increase the Speed of all allies by 10% for 2 turns.
- **Reworked:** 3 AP, 22 Mana. Their next turn comes sooner: their turn gauge fills 40% at once (new; was every ally's Speed +10% for 2 turns, the same as Tailwind at twice the Mana).
- **Description:** "Push yourself or an ally forward on the wind: their next turn comes 40% sooner."
- **Augments:** Effect strength, Mana cost.
- **Why:** Today it is Tailwind for twice the Mana; it becomes Wind's way to hurry one hero's turn.

### Allgust: Tier 2, offensive, all enemies

- **Now:** 3 AP, 25 Mana, power 11 + Wis ×0.3 + Speed ×0.2. A striking gust that deals [X] Wind damage to all enemies.
- **Reworked:** 3 AP, 25 Mana; power 6 + Wis ×0.38 + Speed ×0.11 (70% of the tier's reference, 75% for all enemies, less its effect). Each enemy struck has a 25% chance to be **Off-Balance** for 2 turns.
- **Description:** "A striking gust that deals [X] Wind damage to all enemies, each with a 25% chance to be put Off-Balance for 2 turns."
- **[X], now → reworked:** Level 10: 27 → 24; Level 100: 139 → 154.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** The cheap group spell, with the element's status.

### Deflect Missiles: Tier 2, defensive, all allies

- **Now:** 3 AP, 27 Mana, power none. Reduce the hit chance of all enemy ranged attacks by 30% for 2 turns.
- **Reworked:** 3 AP, 27 Mana. Enemy ranged attacks against your side have 30% less hit chance for 3 turns (was 2).
- **Description:** "Raise a wall of wind around the party: for 3 turns, enemy ranged attacks have 30% less chance to hit."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Kept, a turn longer: the answer to archers.

### Crosswind: Tier 2, offensive, all enemies

- **Now:** 3 AP, 25 Mana, power 9 + Wis ×0.3 + Speed ×0.2. Buffet all enemies for [X] Wind damage and reduce their Speed by 10% for 2 turns.
- **Reworked:** 3 AP, 25 Mana; power 6 + Wis ×0.35 + Speed ×0.1 (65% of the tier's reference, 75% for all enemies, less its effect). Each enemy struck has a 50% chance to be **Off-Balance** for 2 turns (was Speed −10%, which Water's and Ice's statuses carry now).
- **Description:** "Buffet all enemies with crosswinds for [X] Wind damage; each has a 50% chance to be put Off-Balance for 2 turns."
- **[X], now → reworked:** Level 10: 25 → 23; Level 100: 137 → 142.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Built around unbalancing, so it often does.

### Razor Squall: Tier 3, offensive, all enemies

- **Now:** 3 AP, 38 Mana, power 26 + Wis ×0.4 + Speed ×0.3. Fill the battlefield with cutting gusts that deal [X] Wind damage to all enemies and decrease their Evasion by 10% for 2 turns.
- **Reworked:** 3 AP, 38 Mana; power 4 + Wis ×0.25 + Speed ×0.07 (33% of the tier's reference, per gust; two gusts, 70% in all). Two gusts (new) each strike every enemy for [X], each with a 20% chance to put it **Off-Balance** for 2 turns and a 15% chance to make it **Bleed** (Blood's status, borrowed, SP60) for 3 turns (was one strike and Evasion −10%).
- **Description:** "Fill the battlefield with two cutting gusts, each dealing [X] Wind damage to all enemies with a 20% chance to put each Off-Balance and a 15% chance to make it Bleed."
- **[X], now → reworked:** Level 20: 65 → 25; Level 100: 201 → 101 (each gust).
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Two rolls for the status, and two hits that Basic's Exposed and Burning's Wildfire both like.

### Gale Step: Tier 3, defensive, yourself

- **Now:** 3 AP, 34 Mana, power 30 + Wis ×0.4 + Speed ×0.3. Ride a circling current, increasing your Evasion by [X]% for 2 turns.
- **Reworked:** 3 AP, 34 Mana. Evasion +30% for 2 turns and your next turn comes 25% sooner (was Evasion +[X]%, a power used as a percentage: about +150% at Level 100).
- **Description:** "Ride a circling current: your Evasion rises by 30% for 2 turns and your next turn comes 25% sooner."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Its Evasion grew with spell power into the hundreds of percent; it becomes a set, strong dodge with a quick step.

### Sky Cutter: Tier 3, offensive, one target

- **Now:** 3 AP, 33 Mana, power 34 + Wis ×0.4 + Speed ×0.3. Hurl a focused crescent of wind for [X] Wind damage and increase your Speed by 10% for 2 turns.
- **Reworked:** 3 AP, 33 Mana; power 12 + Wis ×0.69 + Speed ×0.2 (90% of the tier's reference). Your Speed +10% for 2 turns (kept).
- **Description:** "Hurl a focused crescent of wind for [X] Wind damage and increase your Speed by 10% for 2 turns."
- **[X], now → reworked:** Level 20: 73 → 71; Level 100: 209 → 280.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Kept as Wind's hit-and-haste; the other three that did the same now do something of their own.

### Wind Shear: Tier 3, offensive, one target

- **Now:** 3 AP, 35 Mana, power 37 + Wis ×0.4 + Speed ×0.3. Slice one enemy for [X] Wind damage and reduce its Evasion by 15% for 2 turns.
- **Reworked:** 3 AP, 35 Mana; power 11 + Wis ×0.65 + Speed ×0.19 (85% of the tier's reference). Always puts the target **Off-Balance** for 2 turns (was Evasion −15%).
- **Description:** "Slice one enemy with shearing wind for [X] Wind damage and put it Off-Balance for 2 turns."
- **[X], now → reworked:** Level 20: 76 → 67; Level 100: 212 → 264.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Wind's sure single Off-Balance, ready for Earth's Topple.

### Tempest: Tier 4, offensive, all enemies

- **Now:** 3 AP, 54 Mana, power 35 + Wis ×0.5 + Speed ×0.3. Engulf all enemies in a violent tempest for [X] Wind damage and decrease their Speed by 10% for 3 turns.
- **Reworked:** 3 AP, 54 Mana; power 10 + Wis ×0.61 + Speed ×0.18 (65% of the tier's reference, 75% for all enemies, less the delay). Every enemy's next turn comes 20% later (its turn gauge drops by 20%; new; was Speed −10% for 3 turns).
- **Description:** "Engulf all enemies in a violent tempest for [X] Wind damage, pushing back each one's next turn by 20%."
- **[X], now → reworked:** Level 30: 102 → 85; Level 100: 246 → 247.
- **Augments:** Power, Resistance penetration, Effect strength, Mana cost.
- **Why:** Wind controls time in battle: a tempest holds the enemy back.

### Skyward Rush: Tier 4, offensive, one target

- **Now:** 3 AP, 49 Mana, power 45 + Wis ×0.5 + Speed ×0.3. Ride a violent updraft into your target for [X] Wind damage and increase your Speed by 20% for 2 turns.
- **Reworked:** 3 AP, 49 Mana; power 14 + Wis ×0.8 + Speed ×0.23 (85% of the tier's reference). Your next turn comes 30% sooner (new; was your Speed +20% for 2 turns).
- **Description:** "Ride a violent updraft into one enemy for [X] Wind damage; your next turn comes 30% sooner."
- **[X], now → reworked:** Level 30: 112 → 113; Level 100: 256 → 325.
- **Augments:** Power, Resistance penetration, Effect strength, Mana cost.
- **Why:** A strike that buys an earlier turn instead of a Speed buff.

### Cyclone Guard: Tier 4, defensive, all allies

- **Now:** 3 AP, 47 Mana, power 40 + Wis ×0.5 + Speed ×0.3. Surround every ally with a defensive cyclone, increasing Evasion by [X]% for 2 turns.
- **Reworked:** 3 AP, 47 Mana. Every ally's Evasion +20% for 3 turns (was +[X]%, a power used as a percentage).
- **Description:** "Surround every ally with a defensive cyclone, increasing their Evasion by 20% for 3 turns."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Like Gale Step, its Evasion grew into the hundreds of percent; it becomes a set party dodge.

### Eye of the Gale: Tier 4, defensive, all allies

- **Now:** 3 AP, 52 Mana, power none. Increase all allies' Evasion by 25% and Speed by 10% for 2 turns.
- **Reworked:** 3 AP, 52 Mana. For 3 turns every ally's Speed +15% and no effect can lower their Speed (Soaked, Chilled and others; new). Was Evasion +25% and Speed +10% for 2 turns.
- **Description:** "Gather the party in the eye of the gale for 3 turns: their Speed rises by 15% and nothing can slow them."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Cyclone Guard gives the dodge; the Eye becomes the answer to Water's and Ice's slows.

### Sky Scythe: Tier 5, offensive, all enemies

- **Now:** 3 AP, 74 Mana, power 42 + Wis ×0.6 + Speed ×0.3. Sweep all enemies with cutting wind for [X] Wind damage and reduce their Speed by 15% for 2 turns.
- **Reworked:** 3 AP, 74 Mana; power 14 + Wis ×0.79 + Speed ×0.23 (65% of the tier's reference, 75% for all enemies, less its effect). Each enemy struck has a 50% chance to be **Off-Balance** for 2 turns (was Speed −15%).
- **Description:** "Sweep all enemies with cutting wind for [X] Wind damage; each has a 50% chance to be put Off-Balance for 2 turns."
- **[X], now → reworked:** Level 40: 144 → 141; Level 100: 288 → 321.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Crosswind's stronger sibling.

### Pressure Vortex: Tier 5, offensive, one target

- **Now:** 3 AP, 74 Mana, power 60 + Wis ×0.6 + Speed ×0.3. Crush one enemy in a vortex for [X] Wind damage and reduce its Evasion by 20% for 2 turns.
- **Reworked:** 3 AP, 74 Mana; power 18 + Wis ×1.04 + Speed ×0.3 (85% of the tier's reference). Its next turn comes 40% later and it can't flee for 2 turns (new; was Evasion −20%).
- **Description:** "Crush one enemy in a vortex for [X] Wind damage; its next turn comes 40% later and it can't flee for 2 turns."
- **[X], now → reworked:** Level 40: 162 → 186; Level 100: 306 → 422.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Tempest's hold on a single enemy, stronger.

### Gale Lance: Tier 5, offensive, one target

- **Now:** 3 AP, 72 Mana, power 54 + Wis ×0.6 + Speed ×0.3. Hurl a focused gust for [X] Wind damage, then increase your Speed by 20% for 2 turns.
- **Reworked:** 3 AP, 72 Mana; power 19 + Wis ×1.1 + Speed ×0.32 (90% of the tier's reference). Deals 1% more for every point of Speed you have over the target, up to +40% (new; was your Speed +20% for 2 turns).
- **Description:** "Hurl a focused gust for [X] Wind damage, 1% more for every point of Speed you have over your target (up to 40%)."
- **[X], now → reworked:** Level 40: 156 → 196; Level 100: 300 → 447.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** The payoff for a fast caster against a slowed enemy.

### Dragonbreath - Razor Gale: Tier 5, offensive, all enemies (Dragonbreath)

- **Now:** 3 AP, 77 Mana, power 40 + Wis ×0.6 + Speed ×0.3. Sweep all enemies with a cutting gale for [X] Wind damage and give each a 10% chance to be stunned for 1 turn.
- **Reworked:** 3 AP, 77 Mana; power 14 + Wis ×0.79 + Speed ×0.23 (65% of the tier's reference, 75% for all enemies, less its effects). Its weakening effect stays (a 10% chance to stun each for 1 turn); each enemy struck has a 25% chance to be **Off-Balance** for 2 turns.
- **Description:** "Sweep all enemies with a cutting gale for [X] Wind damage; each has a 10% chance to be stunned for 1 turn and a 25% chance to be put Off-Balance for 2 turns."
- **[X], now → reworked:** Level 40: 142 → 141; Level 100: 286 → 321.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Dragonbreath spells keep their weakening effect.

### Summon - Wind Elemental: Tier 5, summon

- **Now:** 3 AP, 82 Mana, power none. Summon a friendly Wind Elemental. Only one may be active at a time.
- **Reworked:** 3 AP, 82 Mana; power 21 + Wis ×1.22 + Speed ×0.35 (100% of the tier's reference, as a summon). Summons a Wind Elemental (Level 25, 190 HP today); its maximum HP rises by 200% of [X] and every attack it makes by 25% of [X] (SP29). Its hits have a 40% chance to put their target **Off-Balance** for 2 turns; while it stands, its side's Speed is 10% higher (new). One at a time.
- **Description:** "Summon a friendly Wind Elemental whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more, each with a 40% chance to put its target Off-Balance; while it stands, your side's Speed is 10% higher. Only one may be active at a time."
- **[X], now → reworked:** Level 40: - → 217; Level 100: - → 495.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Wind's only summon, which hastens the party while it lives.

### Hurricane Choir: Tier 6, offensive, all enemies

- **Now:** 3 AP, 96 Mana, power 52 + Wis ×0.7 + Speed ×0.3. Sweep all enemies with a roaring gale for [X] Wind damage and reduce their Evasion by 20% for 2 turns.
- **Reworked:** 3 AP, 96 Mana; power 15 + Wis ×0.88 + Speed ×0.25 (60% of the tier's reference, 75% for all enemies, less its sure status). Always puts every enemy struck **Off-Balance** for 2 turns (was Evasion −20%).
- **Description:** "Sweep all enemies with a roaring gale for [X] Wind damage and put every one of them Off-Balance for 2 turns."
- **[X], now → reworked:** Level 50: 197 → 190; Level 100: 334 → 356.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Wind's sure group status.

### Zephyr Execution: Tier 6, offensive, one target

- **Now:** 3 AP, 104 Mana, power 70 + Wis ×0.7 + Speed ×0.3. Strike one enemy with compressed wind for [X] Wind damage and increase your Speed by 25% for 2 turns.
- **Reworked:** 3 AP, 104 Mana; power 22 + Wis ×1.32 + Speed ×0.38 (90% of the tier's reference). If it defeats its target, you regain 2 AP at once (new; was your Speed +25% for 2 turns).
- **Description:** "Strike one enemy with compressed wind for [X] Wind damage; if it falls, you regain 2 AP."
- **[X], now → reworked:** Level 50: 215 → 285; Level 100: 352 → 535.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** An execution that lets you keep going in the same turn.

### Aerial Riposte: Tier 6, defensive and offensive, yourself

- **Now:** 3 AP, 100 Mana, power 58 + Wis ×0.7 + Speed ×0.3. Your next melee attack deals [X] extra Wind damage; gain 20% Evasion for 2 turns.
- **Reworked:** 3 AP, 100 Mana; power 15 + Wis ×0.88 + Speed ×0.25 (60% of the tier's reference, per riposte). For 2 turns: Evasion +20%, and every enemy attack that misses you is answered with [X] Wind damage to the attacker (new; was a charged melee attack and Evasion +20%).
- **Description:** "Take to the air for 2 turns: your Evasion rises by 20%, and every attack that misses you is answered with [X] Wind damage."
- **[X], now → reworked:** Level 50: 203 → 190; Level 100: 340 → 356.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** An evasive duellist's spell: the more they miss, the more they take.

### Control Weather: Tier 7, the whole field

- **Now:** 3 AP, 160 Mana, power none. Choose Torrential Rain, Blizzard, Drought, Sunny Day, or Thunderstorm. The chosen weather lasts 15 turns or until replaced.
- **Reworked:** 3 AP, 160 Mana. Kept as it is: choose Torrential Rain, Blizzard, Drought, Sunny Day or Thunderstorm for 15 turns or until replaced, each with its effects on every combatant.
- **Description:** "Choose Torrential Rain, Blizzard, Drought, Sunny Day, or Thunderstorm. The chosen weather lasts 15 turns or until replaced."
- **Augments:** Mana cost.
- **Why:** Already one of a kind; nothing about it is dated.

### Skyfall Chorus: Tier 7, offensive, all enemies

- **Now:** 3 AP, 130 Mana, power 64 + Wis ×0.7 + Speed ×0.4. Lash all enemies with cutting winds for [X] Wind damage and lower Evasion by 20% for 2 turns.
- **Reworked:** 3 AP, 130 Mana; power 20 + Wis ×1.18 + Speed ×0.34 (65% of the tier's reference, 75% for all enemies, less the haste). Every ally's next turn comes 20% sooner (new; was Evasion −20% for 2 turns).
- **Description:** "Lash all enemies with a chorus of falling winds for [X] Wind damage; every ally's next turn comes 20% sooner."
- **[X], now → reworked:** Level 60: 243 → 300; Level 100: 357 → 478.
- **Augments:** Power, Resistance penetration, Effect strength, Mana cost.
- **Why:** Damage and tempo for the whole party at once.

### Tempest Needle: Tier 7, offensive, one target

- **Now:** 3 AP, 130 Mana, power 82 + Wis ×0.7 + Speed ×0.4. Drive a compressed gale through one enemy for [X] Wind damage and lower Speed by 25% for 2 turns.
- **Reworked:** 2 AP, 98 Mana; power 20 + Wis ×1.18 + Speed ×0.34 (65% of the tier's reference, the 2 AP rule). Becomes a 2 AP spell (was 3); Speed −25% for 2 turns (kept).
- **Description:** "Drive a compressed gale through one enemy for [X] Wind damage and lower its Speed by 25% for 2 turns. This spell costs 2 AP."
- **[X], now → reworked:** Level 60: 261 → 300; Level 100: 375 → 478.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Wind's fast spell for the high tiers, to pair with a 3 AP spell in one turn.

### Dragonbreath - Sky Rupture: Tier 8, offensive, all enemies (Dragonbreath)

- **Now:** 3 AP, 170 Mana, power 70 + Wis ×0.8 + Speed ×0.5. Tear through all enemies for [X] Wind damage and reduce Evasion by 30% for 2 turns.
- **Reworked:** 3 AP, 170 Mana; power 23 + Wis ×1.37 + Speed ×0.39 (65% of the tier's reference, 75% for all enemies, less its effects). Its weakening effect stays (Evasion −30% for 2 turns); each enemy struck has a 30% chance to be **Off-Balance** for 2 turns.
- **Description:** "Tear through all enemies for [X] Wind damage, reducing their Evasion by 30% for 2 turns, with a 30% chance to put each Off-Balance."
- **[X], now → reworked:** Level 70: 311 → 399; Level 100: 410 → 555.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Dragonbreath spells keep their weakening effect.

### Tempest Lancer: Tier 8, offensive, one target

- **Now:** 3 AP, 190 Mana, power 90 + Wis ×0.8 + Speed ×0.5. Strike one enemy for [X] Wind damage and lower Speed by 30% for 2 turns.
- **Reworked:** 3 AP, 190 Mana; power 32 + Wis ×1.89 + Speed ×0.54 (90% of the tier's reference). Speed −30% for 2 turns (kept).
- **Description:** "Strike one enemy with a tempest lance for [X] Wind damage and lower its Speed by 30% for 2 turns."
- **[X], now → reworked:** Level 70: 331 → 551; Level 100: 430 → 766.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Kept as Wind's great slow on a single enemy, for Gale Lance and the party.

### Tempest Veil: Tier 9, offensive, all enemies; defensive, all allies

- **Now:** 3 AP, 240 Mana, power 85 + Wis ×0.9 + Speed ×0.5. Rake all enemies for [X] Wind damage and lower Speed by 20% for 2 turns.
- **Reworked:** 3 AP, 240 Mana; power 27 + Wis ×1.57 + Speed ×0.45 (60% of the tier's reference, 75% for all enemies, less the veil). For 2 turns, enemy spells and ranged attacks against your side have a 20% chance to miss (the spell-miss effect exists on races and items; was Speed −20%).
- **Description:** "Rake all enemies for [X] Wind damage and draw a veil of storms over the party: for 2 turns, enemy spells and ranged attacks have a 20% chance to miss them."
- **[X], now → reworked:** Level 80: 387 → 518; Level 100: 460 → 637.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** An attack that also shields the party from casters and archers.

### Skyglass Spear: Tier 9, offensive, one target

- **Now:** 3 AP, 260 Mana, power 95 + Wis ×0.9 + Speed ×0.5. Pierce one enemy for [X] Wind damage with a 20% chance to stun for 1 turn.
- **Reworked:** 3 AP, 260 Mana; power 43 + Wis ×2.49 + Speed ×0.71 (95% of the tier's reference). Always puts the target **Off-Balance** for 3 turns; its 20% chance to stun for 1 turn stays.
- **Description:** "Pierce one enemy with a skyglass spear for [X] Wind damage, putting it Off-Balance for 3 turns, with a 20% chance to stun it."
- **[X], now → reworked:** Level 80: 397 → 821; Level 100: 470 → 1,009.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Wind's great single-target spell, ready for Earth's Topple.

### Crown of the Open Sky: Tier 10, offensive, all enemies

- **Now:** 3 AP, 350 Mana, power 220 + Wis ×1 + Speed ×0.6. Unleash [X] Wind damage on all enemies, lower their Evasion by 40% and their Speed by 25% for 2 turns.
- **Reworked:** 3 AP, 350 Mana; power 41 + Wis ×2.41 + Speed ×0.69 (60% of the tier's reference, 75% for all enemies, less its effects). Evasion −40% and Speed −25% for 2 turns on every enemy (kept), and every ally's next turn comes 50% sooner (new).
- **Description:** "Open the sky over all enemies for [X] Wind damage, lowering their Evasion by 40% and their Speed by 25% for 2 turns; every ally's next turn comes 50% sooner."
- **[X], now → reworked:** Level 90: 601 → 885; Level 100: 642 → 977.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Wind's Tier 10: the enemies slowed and the party hurried at once.

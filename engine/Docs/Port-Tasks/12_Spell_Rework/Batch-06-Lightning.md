# Batch 6: Lightning (task 12)

**Status: approved (2026-11-11, SP41), for the port (SP2).** Every Lightning spell players can learn, reworked: 31 spells (Call the
Storm and Thunderbolt of Talos, the Church of Talos's followers', are left out, SP4). They follow the approved system
and curve (`Spell-Augmentation.md`), the sample's style (`Spell-Sample.md`, where Chain Resonance was approved) and the
element statuses (`Element-Statuses.md`): each spell sets its status's chance and duration (SP32). Tiers and prices
stay. "(new)" marks a mechanic the game doesn't have yet. [X] values are for the raw caster of the system draft (no
classes or gear) at the level the tier unlocks and at Level 100; Lightning's spells scale with Intelligence and Speed.

## Lightning's identity

The element of heavy strikes, chains and lost turns:
- **Shocked** (its status: the target loses 1 AP at its next turn) and **Electrocute** (its reaction: on a Soaked
  enemy, every other Soaked enemy is struck for 50% and all of them are Shocked; the Tier 1 spell keeps the same name,
  SP40). Static Jolt and Static Cage are built around Shocking, Tempest Circuit Shocks every enemy, and Storm Dominion
  turns every Shock into a stun.
- **Chains and arcs:** Chain Lightning runs through up to three enemies, Chain Resonance arcs to all (the sample), the
  Lightning Elemental's blows arc to a second enemy.
- **Costly, heavy strikes:** Lightning Bolt keeps its high cost for a much bigger hit; Lightning Form makes the
  element's spells cheaper and stronger for 2 turns; Voltage Spike wins Mana back; Thunderbolt Ascendant grows with
  every Lightning spell cast in the battle.
- **Stuns** stay on Conduit Pulse, Thunder Judgment, Thunderhead Verdict, Storm Burst, the Dragonbreath Thunder Maw
  and the Storm King's judgment; the other stunning spells now cage, brand or strike twice instead.
- Speed loss moves to Water and Ice; Lightning's spells that lowered Speed now Shock. The storm of Tankina's fight still
  grows with every Lightning spell, reworked or not.

New mechanics this batch adds for the port: a chain that runs out after three enemies, a cage that Shocks at each of
the target's turns, a brand that raises one element's damage on a target, a buff that cheapens and strengthens one
element's spells, an aftershock at the target's next turn, Shocks turned into stuns, damage that grows with the spells
cast this battle, a summon whose blows arc, a summon that answers each of its side's spells.

## At a glance

| Tier | Spell | Kind | AP | Mana | What changes |
|---|---|---|---|---|---|
| 1 | Lightning Bolt | offensive, one target | 3 | 19 | the heavy Tier 1 bolt (19 Mana), 25% Shock |
| 1 | Spark | offensive, one target | 2 | 8 | 2 AP kept, 20% Shock |
| 1 | Electrocute | offensive, yourself | 2 | 10 | 2 AP: next weapon attack +[X], 40% Shock |
| 1 | Static Jolt | offensive, one target | 3 | 13 | 40% chance to Shock |
| 2 | Forecast | defensive, yourself | 3 | 20 | Evasion +15%, enemy spells 15% likely to miss you |
| 2 | Lightning Spear | offensive, one target | 3 | 23 | +30% against a Shocked target |
| 2 | 1000 Seagulls | offensive, yourself | 3 | 22 | your next 3 weapon attacks +[X] each, 15% Shock |
| 2 | Conduit Pulse | offensive, one target | 3 | 24 | 15% stun |
| 3 | Chain Lightning | offensive, up to three enemies | 3 | 38 | chains through up to 3 enemies (100%, 70%, 50%), 20% Shock |
| 3 | Thunderstep | offensive, one target | 3 | 34 | your Evasion +15% for 2 turns |
| 3 | Static Veil | defensive, yourself | 3 | 33 | Evasion +15%, retaliation with 50% Shock |
| 3 | Static Cage | offensive, one target | 3 | 37 | Shocked for certain at its next 2 turns; can't flee |
| 4 | Dragonbreath - Thunder Maw | offensive, one target | 3 | 53 | 30% stun kept (Dragonbreath), 25% Shock |
| 4 | Stormchain | offensive, all enemies | 3 | 55 | 30% Shock on each enemy |
| 4 | Lightning Form | defensive, yourself | 3 | 42 | your Lightning spells −20% Mana and +10% power; Speed +20% |
| 4 | Storm Burst | offensive, all enemies | 3 | 52 | 10% stun kept, 20% Shock on each enemy |
| 5 | Thunder Judgment | offensive, one target | 3 | 78 | 25% stun kept |
| 5 | Chain Resonance | offensive, all enemies | 3 | 75 | as approved in the sample |
| 5 | Static Surge | defensive and offensive, yourself or an ally | 3 | 70 | an ally's weapon attacks +[X] and 30% Shock for 2 turns |
| 5 | Voltage Spike | offensive, one target | 3 | 77 | 20% Mana siphon kept, 25% Shock |
| 6 | Summon - Lightning Elemental | summon | 3 | 100 | its attacks arc to a second enemy for 50% |
| 6 | Tempest Circuit | offensive, all enemies | 3 | 96 | Shocks every enemy for certain |
| 6 | Thunderbrand | offensive, one target | 3 | 104 | brands: +20% from Lightning spells for 3 turns |
| 7 | Summon - Storm Elemental | summon | 3 | 140 | every Lightning spell your side casts calls a bolt from it |
| 7 | Dragonbreath - Thunder Crown | offensive, all enemies | 3 | 134 | Mind −20% kept (Dragonbreath), 30% Shock |
| 7 | Volt Pike | offensive, one target | 3 | 146 | an aftershock at the target's next turn (40%) |
| 8 | Storm Dominion | offensive, all enemies | 3 | 170 | Shocked enemies are stunned instead |
| 8 | Thunderbolt Ascendant | offensive, one target | 3 | 190 | +10% per Lightning spell you've cast this battle (up to +50%) |
| 9 | Dragonbreath - Storm of Ages | offensive, all enemies | 3 | 240 | Mind −20% kept (Dragonbreath), 35% Shock |
| 9 | Thunderhead Verdict | offensive, one target | 3 | 260 | always Shocks, 25% stun kept |
| 10 | Judgment of the Storm King | offensive, all enemies | 3 | 350 | 35% stun and Mind −25% kept, Shocks every enemy |

## The spells

### Lightning Bolt: Tier 1, offensive, one target

- **Now:** 3 AP, 19 Mana, power 10 + Int ×0.4 + Speed ×0.1. A bolt of lightning that deals [X] Lightning damage.
- **Reworked:** 3 AP, 19 Mana; power 8 + Int ×0.47 + Speed ×0.14 (135% of the tier's reference, a heavy bolt for its 19 Mana). Keeps its high cost (19 Mana) for a much bigger hit than other Tier 1 bolts; a 25% chance to **Shock** the target.
- **Description:** "A bolt of lightning that deals [X] Lightning damage, with a 25% chance to Shock the target."
- **[X], now → reworked:** Level 1: 15 → 14; Level 100: 163 → 191.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** Today it costs half again as much as other bolts for little more; Lightning becomes the element of costly, heavy strikes.

### Spark: Tier 1, offensive, one target

- **Now:** 2 AP, 7 Mana, power 3 + Int ×0.3 + Speed ×0.1. A small spark that deals [X] Lightning damage. This spell costs 2 Action Points.
- **Reworked:** 2 AP, 8 Mana; power 4 + Int ×0.23 + Speed ×0.07 (65% of the tier's reference, the 2 AP rule). Stays a 2 AP spell; a 20% chance to **Shock** the target.
- **Description:** "A small spark that deals [X] Lightning damage, with a 20% chance to Shock the target. This spell costs 2 AP."
- **[X], now → reworked:** Level 1: 7 → 7; Level 100: 121 → 94.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** Lightning's fast spell; its power and Mana follow the 2 AP rule.

### Electrocute: Tier 1, offensive, yourself (your next weapon attack)

- **Now:** 3 AP, 12 Mana, power 5 + Int ×0.4 + - ×None. Your next melee attack deals [X] additional Lightning damage and decreases the enemy's Speed by 10% for 2 turns.
- **Reworked:** 2 AP, 10 Mana; power 4 + Int ×0.23 + Speed ×0.07 (65% of the tier's reference, the 2 AP rule). Your next weapon attack (melee or ranged, was melee) deals [X] more Lightning damage and has a 40% chance to **Shock** the target (was Speed −10%). Shares its name with the reaction (SP40).
- **Description:** "Charge your weapon with lightning: your next attack deals [X] more Lightning damage, with a 40% chance to Shock the target."
- **[X], now → reworked:** Level 1: 9 → 7; Level 100: 147 → 94.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** Lightning's quick charge for weapon users, like the other elements' charges.

### Static Jolt: Tier 1, offensive, one target

- **Now:** 3 AP, 13 Mana, power 6 + Int ×0.4 + Speed ×0.1. Shock one enemy for [X] Lightning damage and reduce their Speed by 5% for 2 turns.
- **Reworked:** 3 AP, 13 Mana; power 5 + Int ×0.3 + Speed ×0.09 (85% of the tier's reference). A 40% chance to **Shock** the target (was Speed −5%).
- **Description:** "Jolt one enemy with static for [X] Lightning damage, with a 40% chance to Shock it."
- **[X], now → reworked:** Level 1: 11 → 9; Level 100: 159 → 122.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** The Tier 1 spell built around Shocking.

### Forecast: Tier 2, defensive, yourself

- **Now:** 3 AP, 20 Mana, power none. Increase Evasion by 20% for 3 turns. Spells against you have a 5% chance to miss while active.
- **Reworked:** 3 AP, 20 Mana. For 3 turns: Evasion +15% and enemy spells aimed at you have a 15% chance to miss (was +20% and 5%).
- **Description:** "Read the coming storm for 3 turns: your Evasion rises by 15% and enemy spells aimed at you have a 15% chance to miss."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** A 5% chance to dodge spells was too small to notice; Forecast becomes a real ward against casters.

### Lightning Spear: Tier 2, offensive, one target

- **Now:** 3 AP, 23 Mana, power 10 + Int ×0.3 + Speed ×0.3. A fast strike of lightning that deals [X] Lightning damage and ignores 50% of Armor.
- **Reworked:** 3 AP, 23 Mana; power 8 + Int ×0.49 + Speed ×0.14 (90% of the tier's reference). Deals 30% more to a **Shocked** target (was: ignores 50% of Armor, which spells already ignore).
- **Description:** "Hurl a spear of lightning at one enemy for [X] Lightning damage, 30% more if it is Shocked."
- **[X], now → reworked:** Level 10: 28 → 31; Level 100: 149 → 198.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Its Armor pierce did nothing on a spell; it becomes the cheap payoff for a Shock.

### 1000 Seagulls: Tier 2, offensive, yourself (your next three weapon attacks)

- **Now:** 3 AP, 22 Mana, power 12 + Int ×0.4 + Speed ×0.2. Coat your attack in lightning. Your next melee attack deals [X] additional Lightning damage.
- **Reworked:** 3 AP, 22 Mana; power 3 + Int ×0.19 + Speed ×0.06 (35% of the tier's reference, per attack). Your next 3 weapon attacks each deal [X] more Lightning damage, each with a 15% chance to **Shock** (was one melee attack).
- **Description:** "A thousand seagulls of lightning follow your weapon: your next three attacks each deal [X] more Lightning damage, with a 15% chance to Shock."
- **[X], now → reworked:** Level 10: 32 → 12; Level 100: 176 → 77.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** A swarm, as its name says: many small charges instead of one.

### Conduit Pulse: Tier 2, offensive, one target

- **Now:** 3 AP, 24 Mana, power 13 + Int ×0.4 + Speed ×0.2. Strike one enemy for [X] Lightning damage with a 10% chance to stun it for 1 turn.
- **Reworked:** 3 AP, 24 Mana; power 8 + Int ×0.49 + Speed ×0.14 (90% of the tier's reference). A 15% chance to stun the target for 1 turn (was 10%).
- **Description:** "Pulse lightning through one enemy for [X] Lightning damage, with a 15% chance to stun it for 1 turn."
- **[X], now → reworked:** Level 10: 33 → 31; Level 100: 177 → 198.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** Kept as Lightning's first stunning spell.

### Chain Lightning: Tier 3, offensive, up to three enemies

- **Now:** 3 AP, 38 Mana, power 26 + Int ×0.5 + Speed ×0.3. Loose branching lightning that deals [X] Lightning damage to all enemies and decreases their Speed by 10% for 2 turns.
- **Reworked:** 3 AP, 38 Mana; power 11 + Int ×0.65 + Speed ×0.19 (85% of the tier's reference, on the first enemy; 70% and 50% of that on the next two). Strikes the chosen enemy for [X], then chains to up to two more for 70% and 50% of [X] (new); each enemy struck has a 20% chance to be **Shocked** (was every enemy and Speed −10%).
- **Description:** "Loose lightning that strikes one enemy for [X] Lightning damage and chains to up to two more for 70% and then 50% of that, each with a 20% chance to be Shocked."
- **[X], now → reworked:** Level 20: 73 → 67; Level 100: 237 → 264.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** A chain that runs out, unlike Chain Resonance's arc to every enemy: the right spell for two or three.

### Thunderstep: Tier 3, offensive, one target

- **Now:** 3 AP, 34 Mana, power 34 + Int ×0.5 + Speed ×0.3. Strike in a flash for [X] Lightning damage and increase your Speed by 15% for 2 turns.
- **Reworked:** 3 AP, 34 Mana; power 12 + Int ×0.69 + Speed ×0.2 (90% of the tier's reference). You flicker away: your Evasion +15% for 2 turns (was Speed +15%, which is Wind's now).
- **Description:** "Strike in a flash for [X] Lightning damage and flicker away, increasing your Evasion by 15% for 2 turns."
- **[X], now → reworked:** Level 20: 81 → 71; Level 100: 245 → 280.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** A caster's hit-and-dodge; raising Speed belongs to Wind.

### Static Veil: Tier 3, defensive, yourself

- **Now:** 3 AP, 33 Mana, power 30 + Int ×0.5 + Speed ×0.3. Surround yourself with static for 2 turns, increasing Evasion by 15% and dealing [X] Lightning damage to melee attackers.
- **Reworked:** 3 AP, 33 Mana; power 6 + Int ×0.39 + Speed ×0.11 (50% of the tier's reference, per retaliation). For 2 turns: Evasion +15%, and melee attackers take [X] Lightning damage with a 50% chance to be **Shocked** (kept, with the status).
- **Description:** "Surround yourself with static for 2 turns: Evasion +15%, and melee attackers take [X] Lightning damage with a 50% chance to be Shocked."
- **[X], now → reworked:** Level 20: 77 → 39; Level 100: 241 → 157.
- **Augments:** Power, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Kept, with the element's status.

### Static Cage: Tier 3, offensive, one target

- **Now:** 3 AP, 37 Mana, power 35 + Int ×0.5 + Speed ×0.3. Trap one enemy in lightning for [X] damage with a 15% chance to stun it for 1 turn.
- **Reworked:** 3 AP, 37 Mana; power 10 + Int ×0.58 + Speed ×0.17 (75% of the tier's reference). The cage holds for 2 turns: the target can't flee and is **Shocked** at the start of each of them (loses 1 AP each; new; was a 15% stun chance).
- **Description:** "Trap one enemy in a cage of lightning for [X] Lightning damage; for 2 turns it can't flee and loses 1 AP at the start of each of its turns."
- **[X], now → reworked:** Level 20: 82 → 60; Level 100: 246 → 236.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** A sure lockdown on one enemy, for less damage.

### Dragonbreath - Thunder Maw: Tier 4, offensive, one target (Dragonbreath)

- **Now:** 3 AP, 53 Mana, power 45 + Int ×0.6 + Speed ×0.3. Exhale concentrated lightning for [X] Lightning damage and gain a 30% chance to stun the target.
- **Reworked:** 3 AP, 53 Mana; power 13 + Int ×0.75 + Speed ×0.22 (80% of the tier's reference). Its weakening effect stays (a 30% chance to stun for 1 turn); a 25% chance to **Shock** the target.
- **Description:** "Exhale concentrated lightning at one enemy for [X] Lightning damage, with a 30% chance to stun it and a 25% chance to Shock it."
- **[X], now → reworked:** Level 30: 123 → 106; Level 100: 291 → 305.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** Dragonbreath spells keep their weakening effect.

### Stormchain: Tier 4, offensive, all enemies

- **Now:** 3 AP, 55 Mana, power 35 + Int ×0.6 + Speed ×0.3. Bind every enemy in linked lightning, dealing [X] Lightning damage and decreasing their Speed by 15% for 2 turns.
- **Reworked:** 3 AP, 55 Mana; power 10 + Int ×0.61 + Speed ×0.18 (65% of the tier's reference, 75% for all enemies, less its effect). Each enemy struck has a 30% chance to be **Shocked** (was Speed −15%).
- **Description:** "Bind every enemy in linked lightning for [X] Lightning damage; each has a 30% chance to be Shocked."
- **[X], now → reworked:** Level 30: 113 → 85; Level 100: 281 → 247.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** The mid-tier group Shock; Speed loss belongs to Water and Ice.

### Lightning Form: Tier 4, defensive, yourself

- **Now:** 3 AP, 42 Mana, power 40 + Int ×0.6 + Speed ×0.3. Become living lightning for 2 turns, increasing Speed and Evasion by 20% and dealing [X] Lightning damage to melee attackers.
- **Reworked:** 3 AP, 42 Mana. For 2 turns: Speed +20%, and your Lightning spells cost 20% less Mana and gain 10% power (new; was Speed and Evasion +20% and retaliation).
- **Description:** "Become living lightning for 2 turns: your Speed rises by 20%, and your Lightning spells cost 20% less Mana and gain 10% power."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Static Veil already retaliates; the Form becomes the Lightning caster's burst window, paying for the element's costly spells.

### Storm Burst: Tier 4, offensive, all enemies

- **Now:** 3 AP, 52 Mana, power 36 + Int ×0.6 + Speed ×0.3. Burst over all enemies for [X] Lightning damage, each with a 10% chance to be stunned for 1 turn.
- **Reworked:** 3 AP, 52 Mana; power 10 + Int ×0.61 + Speed ×0.18 (65% of the tier's reference, 75% for all enemies, less its effects). Each enemy struck keeps its 10% chance to be stunned for 1 turn and has a 20% chance to be **Shocked**.
- **Description:** "Burst over all enemies for [X] Lightning damage; each has a 10% chance to be stunned and a 20% chance to be Shocked."
- **[X], now → reworked:** Level 30: 114 → 85; Level 100: 282 → 247.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** Kept, with the element's status.

### Thunder Judgment: Tier 5, offensive, one target

- **Now:** 3 AP, 78 Mana, power 61 + Int ×0.7 + Speed ×0.3. Call down a precise thunderbolt for [X] Lightning damage with a 25% chance to stun the target.
- **Reworked:** 3 AP, 78 Mana; power 19 + Int ×1.1 + Speed ×0.32 (90% of the tier's reference). A 25% chance to stun the target for 1 turn (kept).
- **Description:** "Call down a precise thunderbolt on one enemy for [X] Lightning damage, with a 25% chance to stun it."
- **[X], now → reworked:** Level 40: 178 → 196; Level 100: 343 → 447.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** Kept as Lightning's mid-tier stun.

### Chain Resonance: Tier 5, offensive, all enemies (arcing)

- **Now:** 3 AP, 75 Mana, power 42 + Int ×0.7 + Speed ×0.3. Arc lightning through all enemies for [X] Lightning damage and reduce their Speed by 10% for 3 turns.
- **Reworked:** 3 AP, 75 Mana; power 21 + Int ×1.22 + Speed ×0.35 (100% of the tier's reference, on the chosen enemy; 70% of that on every other). As approved in the sample (SP31): strikes the chosen enemy for [X] and arcs to every other enemy for 70% of [X]; each enemy struck has a 25% chance to be **Shocked**.
- **Description:** "Strike one enemy with lightning for [X] Lightning damage; it arcs on to every other enemy for 70% of that. Each enemy struck has a 25% chance to be Shocked."
- **[X], now → reworked:** Level 40: 159 → 217; Level 100: 324 → 495.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** As approved in the sample.

### Static Surge: Tier 5, defensive and offensive, yourself or an ally

- **Now:** 3 AP, 70 Mana, power 48 + Int ×0.7 + Speed ×0.3. Charge yourself for 2 turns: melee attackers take [X] Lightning damage and your Speed increases by 20%.
- **Reworked:** 3 AP, 70 Mana; power 8 + Int ×0.49 + Speed ×0.14 (40% of the tier's reference, per weapon attack). For 2 turns, the weapon attacks of yourself or an ally each deal [X] more Lightning damage and have a 30% chance to **Shock** (new; was retaliation and Speed +20% on yourself).
- **Description:** "Surge static into yourself or an ally for 2 turns: each of their weapon attacks deals [X] more Lightning damage, with a 30% chance to Shock."
- **[X], now → reworked:** Level 40: 165 → 87; Level 100: 330 → 198.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Lightning's support for the party's weapon users; Static Veil already retaliates.

### Voltage Spike: Tier 5, offensive, one target

- **Now:** 3 AP, 77 Mana, power 61 + Int ×0.7 + Speed ×0.3. Strike one enemy for [X] Lightning damage and siphon Mana equal to 20% of the damage dealt.
- **Reworked:** 3 AP, 77 Mana; power 19 + Int ×1.1 + Speed ×0.32 (90% of the tier's reference). Siphons Mana: you regain 20% of the damage dealt (kept); a 25% chance to **Shock** the target.
- **Description:** "Spike one enemy with voltage for [X] Lightning damage, regaining Mana equal to 20% of the damage dealt, with a 25% chance to Shock it."
- **[X], now → reworked:** Level 40: 178 → 196; Level 100: 343 → 447.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** Kept: Lightning's costly spells need a way to win Mana back.

### Summon - Lightning Elemental: Tier 6, summon

- **Now:** 3 AP, 100 Mana, power none. Summon a friendly Lightning Elemental whose attacks deal 20% more damage. Only one may be active at a time.
- **Reworked:** 3 AP, 100 Mana; power 25 + Int ×1.47 + Speed ×0.42 (100% of the tier's reference, as a summon). Summons a Lightning Elemental (Level 25, 207 HP today, summoned with +20% damage); its maximum HP rises by 200% of [X] and every attack it makes by 25% of [X] (SP29). Its attacks arc to a second, random enemy for 50% of the damage (new), and each enemy it strikes has a 40% chance to be **Shocked**. One at a time.
- **Description:** "Summon a friendly Lightning Elemental whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more and arc to a second enemy for half; each enemy it strikes has a 40% chance to be Shocked. Only one may be active at a time."
- **[X], now → reworked:** Level 50: - → 317; Level 100: - → 596.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Lightning's elemental chains its blows like the element's spells.

### Tempest Circuit: Tier 6, offensive, all enemies

- **Now:** 3 AP, 96 Mana, power 52 + Int ×0.8 + Speed ×0.3. Arc lightning through all enemies for [X] Lightning damage and reduce their Speed by 15% for 3 turns.
- **Reworked:** 3 AP, 96 Mana; power 14 + Int ×0.81 + Speed ×0.23 (55% of the tier's reference, 75% for all enemies, less its sure Shock). Always **Shocks** every enemy struck (was Speed −15% for 3 turns).
- **Description:** "Close a tempest circuit through all enemies for [X] Lightning damage, Shocking every one of them."
- **[X], now → reworked:** Level 50: 215 → 175; Level 100: 370 → 328.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** A sure AP loss on the whole enemy side, for much less damage.

### Thunderbrand: Tier 6, offensive, one target

- **Now:** 3 AP, 104 Mana, power 68 + Int ×0.8 + Speed ×0.3. Brand one enemy with a thunderbolt for [X] Lightning damage and gain a 30% chance to stun it.
- **Reworked:** 3 AP, 104 Mana; power 21 + Int ×1.25 + Speed ×0.36 (85% of the tier's reference). **Brands** the target for 3 turns (new): every Lightning spell against it deals 20% more (was a 30% stun chance).
- **Description:** "Brand one enemy with a thunderbolt for [X] Lightning damage; for 3 turns every Lightning spell against it deals 20% more."
- **[X], now → reworked:** Level 50: 231 → 270; Level 100: 386 → 506.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Marks the one enemy a Lightning party should bring down.

### Summon - Storm Elemental: Tier 7, summon

- **Now:** 3 AP, 140 Mana, power none. Summon a friendly Storm Elemental whose attacks deal 10% more damage. Only one may be active at a time.
- **Reworked:** 3 AP, 140 Mana; power 31 + Int ×1.82 + Speed ×0.52 (100% of the tier's reference, as a summon). Summons a Storm Elemental (Level 50, 409 HP today, summoned with +10% damage); its maximum HP rises by 200% of [X] and every attack it makes by 25% of [X] (SP29). Each enemy it strikes has a 40% chance to be **Shocked**; while it stands, every Lightning spell its side casts also calls a bolt from it on a random enemy for 25% of [X] (new). One at a time.
- **Description:** "Summon a friendly Storm Elemental whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more, each with a 40% chance to Shock; while it stands, every Lightning spell your side casts also calls a bolt from it on a random enemy for a quarter of [X]. Only one may be active at a time."
- **[X], now → reworked:** Level 60: - → 462; Level 100: - → 737.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** The storm answers every Lightning spell while its elemental lives.

### Dragonbreath - Thunder Crown: Tier 7, offensive, all enemies (Dragonbreath)

- **Now:** 3 AP, 134 Mana, power 64 + Int ×0.8 + Speed ×0.4. Sweep all enemies with lightning for [X] damage and lower Mind by 20% for 2 turns.
- **Reworked:** 3 AP, 134 Mana; power 20 + Int ×1.18 + Speed ×0.34 (65% of the tier's reference, 75% for all enemies, less its effects). Its weakening effect stays (Mind −20% for 2 turns); each enemy struck has a 30% chance to be **Shocked**.
- **Description:** "Sweep all enemies with a crown of lightning for [X] Lightning damage, lowering their Mind by 20% for 2 turns, with a 30% chance to Shock each."
- **[X], now → reworked:** Level 60: 265 → 300; Level 100: 393 → 478.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Dragonbreath spells keep their weakening effect.

### Volt Pike: Tier 7, offensive, one target

- **Now:** 3 AP, 146 Mana, power 82 + Int ×0.8 + Speed ×0.4. Strike one enemy for [X] Lightning damage with a 15% chance to stun for 1 turn.
- **Reworked:** 3 AP, 146 Mana; power 26 + Int ×1.55 + Speed ×0.44 (85% of the tier's reference, and 40% of [X] again as an aftershock). An aftershock strikes the target again at the start of its next turn for 40% of [X] (new; was a 15% stun chance).
- **Description:** "Drive a volt pike into one enemy for [X] Lightning damage; it strikes again at the start of the enemy's next turn for 40% of that."
- **[X], now → reworked:** Level 60: 283 → 393; Level 100: 411 → 627.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** More damage in all than a plain spell, paid for by waiting for the aftershock.

### Storm Dominion: Tier 8, offensive, all enemies

- **Now:** 3 AP, 170 Mana, power 70 + Int ×0.9 + Speed ×0.5. Strike all enemies for [X] Lightning damage and lower Mind by 25% for 2 turns.
- **Reworked:** 3 AP, 170 Mana; power 22 + Int ×1.26 + Speed ×0.36 (60% of the tier's reference, 75% for all enemies, less the stuns). Every enemy that is already **Shocked** is stunned for 1 turn instead (its Shock turns into a stun; new; was Mind −25% for 2 turns).
- **Description:** "Strike all enemies with the storm's dominion for [X] Lightning damage; every one that is Shocked is stunned for 1 turn instead."
- **[X], now → reworked:** Level 70: 336 → 368; Level 100: 445 → 511.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** The great payoff for a Shocked field: Tempest Circuit then Storm Dominion stuns every enemy.

### Thunderbolt Ascendant: Tier 8, offensive, one target

- **Now:** 3 AP, 190 Mana, power 90 + Int ×0.9 + Speed ×0.5. Strike one enemy for [X] Lightning damage with a 25% chance to stun for 1 turn.
- **Reworked:** 3 AP, 190 Mana; power 32 + Int ×1.89 + Speed ×0.54 (90% of the tier's reference). Deals 10% more for each Lightning spell you have cast this battle, up to +50% (new; was a 25% stun chance).
- **Description:** "Call down an ascendant thunderbolt on one enemy for [X] Lightning damage, 10% more for each Lightning spell you have cast this battle (up to half again)."
- **[X], now → reworked:** Level 70: 356 → 551; Level 100: 465 → 766.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** The finisher of a Lightning caster's long fight.

### Dragonbreath - Storm of Ages: Tier 9, offensive, all enemies (Dragonbreath)

- **Now:** 3 AP, 240 Mana, power 85 + Int ×1 + Speed ×0.5. Exhale an ancient storm for [X] Lightning damage to all enemies and lower Mind by 20% for 2 turns.
- **Reworked:** 3 AP, 240 Mana; power 29 + Int ×1.7 + Speed ×0.49 (65% of the tier's reference, 75% for all enemies, less its effects). Its weakening effect stays (Mind −20% for 2 turns); each enemy struck has a 35% chance to be **Shocked**.
- **Description:** "Exhale an ancient storm on all enemies for [X] Lightning damage, lowering their Mind by 20% for 2 turns, with a 35% chance to Shock each."
- **[X], now → reworked:** Level 80: 416 → 561; Level 100: 496 → 689.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Dragonbreath spells keep their weakening effect.

### Thunderhead Verdict: Tier 9, offensive, one target

- **Now:** 3 AP, 260 Mana, power 95 + Int ×1 + Speed ×0.5. Call down [X] Lightning damage on one enemy with a 25% chance to stun for 1 turn.
- **Reworked:** 3 AP, 260 Mana; power 40 + Int ×2.36 + Speed ×0.68 (90% of the tier's reference). Always **Shocks** the target; its 25% chance to stun for 1 turn stays.
- **Description:** "Call down the thunderhead's verdict on one enemy for [X] Lightning damage, Shocking it, with a 25% chance to stun it."
- **[X], now → reworked:** Level 80: 426 → 778; Level 100: 506 → 957.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** Lightning's great single-target strike, which always costs its target a turn's AP or more.

### Judgment of the Storm King: Tier 10, offensive, all enemies

- **Now:** 3 AP, 350 Mana, power 230 + Int ×1.1 + Speed ×0.6. Strike all enemies for [X] Lightning damage with a 35% chance to stun each for 1 turn and lower their Mind by 25% for 2 turns.
- **Reworked:** 3 AP, 350 Mana; power 41 + Int ×2.41 + Speed ×0.69 (60% of the tier's reference, 75% for all enemies, less its effects). Each enemy struck keeps its 35% chance to be stunned for 1 turn and Mind −25% for 2 turns, and is always **Shocked**.
- **Description:** "Pass the Storm King's judgment on all enemies for [X] Lightning damage: each is Shocked, has a 35% chance to be stunned, and has its Mind lowered by 25% for 2 turns."
- **[X], now → reworked:** Level 90: 643 → 885; Level 100: 688 → 977.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Lightning's Tier 10 keeps its stuns and gains a sure Shock.

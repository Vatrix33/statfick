# Batch 1: Basic (task 12)

**Status: approved (2026-11-11, SP35), for the port (SP2).** Every Basic spell players can learn, reworked: 33 spells (War Horn,
Zik Lun's, is left out, SP4). They follow the approved system and curve (`Spell-Augmentation.md`), the sample's style
(`Spell-Sample.md`) and the element statuses (`Element-Statuses.md`): each spell sets its status's chance and duration
(SP32). Tiers and prices stay. "(new)" marks a mechanic the game doesn't have yet; [X] values are for the raw caster of
the system draft (no classes or gear), at the level the tier unlocks and at Level 100.

## Basic's identity

Raw Mana, the generalist's element:
- **Exposed** (its status: +5% spell damage taken per stack, up to 3) and **Resonance** (its reaction: +10% damage per
  different status on the target): Basic opens enemies to the party's other spells and profits from the statuses they
  leave.
- **Mana:** the element that wins Mana back (siphons) and takes it from enemy casters (Mana burn, new), as Mana stays
  as scarce as now (SP17).
- **Barriers and wards:** today's Basic shields add flat Armor that outgrows armor pieces at high levels; they become
  barriers (absorbing weapon and spell damage alike) and wards against spells.
- **Spellbreaking:** dispels, a spell intercept, a spell reflection and a silence (Sever) against enemy casters.
- **Charges for weapon users:** Manastrike and Arcane Infusion load the next attacks with Basic damage.

New mechanics this batch adds for the port: barriers from spells, next-spell power, Mana burn, the spell intercept,
random bolts, a delayed second wave, the star's pulses, Sever (no spells next turn), ending an enemy's buffs, being
"unmade" below a share of HP (bosses excepted).

## At a glance

| Tier | Spell | Kind | AP | Mana | What changes |
|---|---|---|---|---|---|
| 1 | Spell Bolt | offensive, one target | 3 | 10 | 15% chance to Expose |
| 1 | Magic Shield | defensive, yourself | 3 | 12 | a barrier on yourself (was Armor) |
| 1 | Reinforce | defensive, yourself or an ally | 3 | 12 | Strength and Dexterity +8%, on an ally too |
| 1 | Overcharge Cannon | offensive, one target | 5 | 17 | a 5 AP shot instead of stunning you |
| 1 | Mana Needle | offensive, one target | 3 | 11 | regain 25% of the damage as Mana |
| 2 | Power Shot | offensive, one target | 3 | 20 | your next spell +15% power |
| 2 | Manastrike | offensive, yourself | 2 | 15 | 2 AP: your next weapon attack +[X], 50% Expose |
| 2 | Elemental Screen | defensive, yourself or an ally | 3 | 20 | 20% less damage from all 11 other elements, on an ally too |
| 2 | Mana Fracture | offensive, one target | 3 | 22 | burns Mana: the target loses half the damage as Mana |
| 3 | Arcane Lance | offensive, one target | 3 | 34 | pierces 40 points of resistance, 30% Expose |
| 3 | Mana Ward | defensive, yourself or an ally | 3 | 36 | a barrier on yourself or an ally (was Armor) |
| 3 | Arcane Infusion | offensive, yourself | 3 | 35 | your next 2 weapon attacks +[X] each, 50% Expose |
| 3 | Arcane Intercept | defensive, yourself or an ally | 2 | 25 | 2 AP: the next enemy spell at them deals 50% less |
| 4 | Arcane Barrage | offensive, random enemies | 3 | 48 | 5 bolts at random enemies, 20% Expose each |
| 4 | Grand Mana Ward | defensive, all allies | 3 | 50 | a barrier on every ally (was Armor) |
| 4 | Aether Rupture | offensive, one target | 3 | 52 | always Exposes twice |
| 4 | Arcane Shrapnel | offensive, all enemies | 3 | 52 | regain 10% of all the damage as Mana, 15% Expose |
| 5 | Aether Cascade | offensive, all enemies | 3 | 74 | burns Mana on every enemy |
| 5 | Spellbreaker Lance | offensive, one target | 3 | 74 | ends the target's newest buff, or +20% damage |
| 5 | Mana Bastion | defensive, all allies | 3 | 72 | every ally: −25% spell and −10% weapon damage |
| 5 | Dragonbreath - Aether Storm | offensive, all enemies | 3 | 75 | Evasion −15% kept (Dragonbreath), 25% Expose |
| 5 | Aether Nova | offensive, all enemies | 3 | 76 | always Exposes every enemy |
| 6 | Arcane Cataclysm | offensive, all enemies | 3 | 96 | strikes again next turn for half |
| 6 | Aether Severance | offensive, one target | 3 | 104 | 50% chance: no spells on its next turn |
| 6 | Arcane Reversal | defensive, yourself | 3 | 100 | reflects the next enemy spell at you (60%) |
| 7 | Arcane Sundering | offensive, all enemies | 3 | 134 | ends every enemy's newest buff |
| 7 | Aether Verdict | offensive, one target | 3 | 146 | double Resonance: +20% per status, up to +60% |
| 7 | Prism Aegis | defensive, yourself or an ally | 3 | 140 | a barrier, −30% spell damage, bursts on all enemies when it ends |
| 8 | Null Star | offensive, all enemies | 3 | 170 | strikes again at your next 2 turns, Exposing each time |
| 8 | Absolute Aether | offensive, one target | 3 | 190 | ignores all resistance and barriers |
| 9 | Fracture the Law | offensive, all enemies | 3 | 240 | ends every buff on every enemy, Exposes 3 times |
| 9 | Axiom Lance | offensive, one target | 3 | 260 | unmakes an enemy left below 15% HP (not bosses) |
| 10 | Axiom Unbound | offensive, all enemies | 3 | 350 | all stats −15% and Exposed 3 times, on every enemy |

## The spells

### Spell Bolt: Tier 1, offensive, one target

- **Now:** 3 AP, 10 Mana, power 5 + Int ×0.3 + Wis ×0.2. A bolt of Mana that deals [X] Basic damage.
- **Reworked:** 3 AP, 10 Mana; power 6 + Int ×0.33 + Wis ×0.1 (95% of the tier's reference). A 15% chance to **Expose** the target (1 stack) for 2 turns.
- **Description:** "A bolt of raw Mana that deals [X] Basic damage, with a 15% chance to Expose the target for 2 turns."
- **[X], now → reworked:** Level 1: 10 → 10; Level 100: 159 → 147.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Basic's cheapest bolt (10 Mana against Fire Bolt's 12) at about today's power (SP22); it starts the Exposed stacks the element builds on.

### Magic Shield: Tier 1, defensive, yourself

- **Now:** 3 AP, 14 Mana, power 10 + Mind ×0.2 + Wis ×0.2. A shield of Mana that increases your Armor by [X] for 2 turns.
- **Reworked:** 3 AP, 12 Mana; power 5 + Mind ×0.32 + Wis ×0.09 (90% of the tier's reference). A **barrier** of [X] on yourself for 2 turns: it absorbs damage from weapons and spells until it breaks or fades (barriers exist on artifacts; new on spells).
- **Description:** "Wrap yourself in a shield of Mana that absorbs the next [X] damage you take within 2 turns."
- **[X], now → reworked:** Level 1: 13 → 9; Level 100: 102 → 101.
- **Augments:** Power, Effect duration, Mana cost.
- **Why:** Flat Armor from spells grows far past what armor pieces give (today's Tier 5 Mana Bastion adds about 370 Armor to every ally at Level 100, where full plate gives about 170), so Basic's shields become barriers, which work against spells too and stay in proportion.

### Reinforce: Tier 1, defensive, yourself or an ally

- **Now:** 3 AP, 13 Mana, power none. Increase your Strength by 5% for 3 turns.
- **Reworked:** 3 AP, 12 Mana. Strength and Dexterity +8% for 3 turns on yourself or an ally (was Strength +5% on yourself).
- **Description:** "Reinforce yourself or an ally with Mana, raising Strength and Dexterity by 8% for 3 turns."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Today's spell is the weakest in the game (Strength +5% on yourself); it becomes a cheap support for the party's weapon users.

### Overcharge Cannon: Tier 1, offensive, one target

- **Now:** 3 AP, 30 Mana, power 10 + Int ×0.5 + Wis ×0.3. Fire a mighty shot of Mana that deals [X] Basic damage. You are stunned for 1 turn afterward.
- **Reworked:** 5 AP, 17 Mana; power 10 + Int ×0.56 + Wis ×0.16 (160% of the tier's reference, the 5 AP rule). Takes your whole turn (5 AP) instead of stunning you for the next one.
- **Description:** "Pour your whole turn into a mighty shot of Mana that deals [X] Basic damage."
- **[X], now → reworked:** Level 1: 18 → 17; Level 100: 258 → 247.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Today it costs your next turn; as a 5 AP spell (SP7) it costs only this one, and its power follows the AP rule (160% of the power for 140% of the Mana).

### Mana Needle: Tier 1, offensive, one target

- **Now:** 3 AP, 11 Mana, power 6 + Int ×0.3 + Wis ×0.2. Pierce one enemy with Mana for [X] Basic damage and siphon Mana equal to 20% of damage dealt.
- **Reworked:** 3 AP, 11 Mana; power 5 + Int ×0.32 + Wis ×0.09 (90% of the tier's reference). Siphons Mana: you regain 25% of the damage dealt as Mana (was 20%).
- **Description:** "Pierce one enemy with Mana for [X] Basic damage and regain Mana equal to 25% of the damage dealt."
- **[X], now → reworked:** Level 1: 11 → 9; Level 100: 160 → 140.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** With Mana as scarce as now (SP17), Basic is the element that wins some of it back; the needle is the cheap way.

### Power Shot: Tier 2, offensive, one target

- **Now:** 3 AP, 20 Mana, power 10 + Int ×0.4 + Wis ×0.2. Fire a strengthening bolt of Mana. Deal [X] Basic damage and increase your Intelligence by 5% for 2 turns.
- **Reworked:** 3 AP, 20 Mana; power 8 + Int ×0.49 + Wis ×0.14 (90% of the tier's reference). Your next spell within 2 turns gains 15% power (new; was Intelligence +5% for 2 turns).
- **Description:** "Fire a strengthening bolt of Mana for [X] Basic damage; your next spell within 2 turns gains 15% power."
- **[X], now → reworked:** Level 10: 33 → 33; Level 100: 199 → 216.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** A setup shot: cheap damage now, a stronger big spell next.

### Manastrike: Tier 2, offensive, yourself (your next weapon attack)

- **Now:** 3 AP, 21 Mana, power 11 + Int ×0.3 + Wis ×0.3. Your next melee attack deals [X] additional Basic damage.
- **Reworked:** 2 AP, 15 Mana; power 6 + Int ×0.35 + Str ×0.1 (65% of the tier's reference, the 2 AP rule). Your next weapon attack (melee or ranged, was melee only) deals [X] more Basic damage and has a 50% chance to **Expose** the target (1 stack) for 2 turns.
- **Description:** "Charge your weapon with Mana: your next attack deals [X] more Basic damage, with a 50% chance to Expose the target for 2 turns."
- **[X], now → reworked:** Level 10: 32 → 23; Level 100: 188 → 142.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** A spellblade's quick charge: 2 AP leaves room for the attack itself (2 AP) in the same turn; Strength in the scaling suits the hybrids who use it.

### Elemental Screen: Tier 2, defensive, yourself or an ally

- **Now:** 3 AP, 16 Mana, power none. Take 20% less damage from Fire, Water, Earth, Wind, Ice, and Lightning spells for 3 turns.
- **Reworked:** 3 AP, 20 Mana. Yourself or an ally takes 20% less damage from the spells of every element but Basic for 3 turns (was six elements, yourself only).
- **Description:** "Screen yourself or an ally from elemental magic: 20% less damage from Fire, Water, Earth, Wind, Lightning, Ice, Holy, Dark, Nature, Death and Blood spells for 3 turns."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Today's six elements were the old game's first six; the screen now covers all eleven and can protect an ally.

### Mana Fracture: Tier 2, offensive, one target

- **Now:** 3 AP, 23 Mana, power 12 + Int ×0.4 + Wis ×0.2. Fracture one enemy's Mana flow for [X] Basic damage and reduce its Mind by 8% for 2 turns.
- **Reworked:** 3 AP, 22 Mana; power 8 + Int ×0.49 + Wis ×0.14 (90% of the tier's reference). **Burns Mana** (new): the target loses Mana equal to 50% of the damage dealt (was Mind −8% for 2 turns).
- **Description:** "Fracture one enemy's Mana flow for [X] Basic damage; it loses Mana equal to half of the damage."
- **[X], now → reworked:** Level 10: 35 → 33; Level 100: 201 → 216.
- **Augments:** Power, Resistance penetration, Effect strength, Mana cost.
- **Why:** Lowering Mind did little in a fight; burning an enemy caster's Mana keeps its big spells away.

### Arcane Lance: Tier 3, offensive, one target

- **Now:** 3 AP, 34 Mana, power 34 + Int ×0.5 + Wis ×0.3. Condense raw Mana into a piercing lance that deals [X] Basic damage and ignores 30% of Armor.
- **Reworked:** 3 AP, 34 Mana; power 12 + Int ×0.73 + Wis ×0.21 (95% of the tier's reference). The target's resistance counts 40 points lower against it (never below 0); a 30% chance to **Expose** it (1 stack) for 2 turns. (Was: ignores 30% of Armor, which spells already ignore.)
- **Description:** "Condense raw Mana into a piercing lance for [X] Basic damage that cuts through 40 points of resistance, with a 30% chance to Expose the target for 2 turns."
- **[X], now → reworked:** Level 20: 88 → 79; Level 100: 282 → 322.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Its Armor pierce did nothing, since spells ignore Armor; it pierces resistance instead, the spells' own defence.

### Mana Ward: Tier 3, defensive, yourself or an ally

- **Now:** 3 AP, 36 Mana, power 30 + Int ×0.5 + Wis ×0.3. Wrap yourself or an ally in condensed Mana, increasing Armor by [X] for 3 turns.
- **Reworked:** 3 AP, 36 Mana; power 12 + Mind ×0.69 + Wis ×0.2 (90% of the tier's reference). A **barrier** of [X] on yourself or an ally for 3 turns (was Armor +[X]).
- **Description:** "Wrap yourself or an ally in condensed Mana that absorbs the next [X] damage they take within 3 turns."
- **[X], now → reworked:** Level 20: 84 → 59; Level 100: 278 → 219.
- **Augments:** Power, Effect duration, Mana cost.
- **Why:** Magic Shield's bigger sibling: it can guard an ally and lasts a turn longer.

### Arcane Infusion: Tier 3, offensive, yourself (your next two weapon attacks)

- **Now:** 3 AP, 35 Mana, power 26 + Int ×0.5 + Wis ×0.3. Infuse your weapon with raw Mana. Your next melee attack deals [X] additional Basic damage, and your Intelligence increases by 10% for 2 turns.
- **Reworked:** 3 AP, 35 Mana; power 8 + Int ×0.46 + Str ×0.13 (60% of the tier's reference, per attack). Your next 2 weapon attacks each deal [X] more Basic damage and have a 50% chance to **Expose** the target (1 stack) for 2 turns (was one melee attack and Intelligence +10%).
- **Description:** "Infuse your weapon with raw Mana: your next two attacks each deal [X] more Basic damage, with a 50% chance to Expose the target for 2 turns."
- **[X], now → reworked:** Level 20: 80 → 47; Level 100: 274 → 186.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Manastrike for a whole turn of attacks: two charged blows that stack Exposed for the party's casters.

### Arcane Intercept: Tier 3, defensive, yourself or an ally

- **Now:** 3 AP, 35 Mana, power none. Grant yourself or an ally 15 Armor and reduce incoming spell damage by 20% for 2 turns.
- **Reworked:** 2 AP, 25 Mana. The next enemy spell that targets them deals 50% less (new); lasts 3 turns or until it intercepts one (was 15 Armor and 20% less spell damage for 2 turns).
- **Description:** "Set a Mana intercept on yourself or an ally: the next enemy spell aimed at them within 3 turns deals 50% less."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** A quick (2 AP) answer to an enemy caster's big spell, cast before it lands.

### Arcane Barrage: Tier 4, offensive, random enemies

- **Now:** 3 AP, 48 Mana, power 35 + Int ×0.6 + Wis ×0.3. Rain disruptive bolts of Mana across the enemy line, dealing [X] Basic damage and decreasing every enemy's Intelligence by 10% for 2 turns.
- **Reworked:** 3 AP, 48 Mana; power 4 + Int ×0.21 + Wis ×0.06 (22% of the tier's reference, per bolt; five bolts, 110% in all). Fires 5 bolts at random enemies (new), each for [X] and each with a 20% chance to **Expose** its target (1 stack) for 2 turns (was every enemy, Intelligence −10%).
- **Description:** "Rain five bolts of Mana on random enemies, each dealing [X] Basic damage with a 20% chance to Expose its target for 2 turns."
- **[X], now → reworked:** Level 30: 125 → 32; Level 100: 319 → 93 (each bolt).
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Against one enemy it's a strong single-target spell, against many it spreads; five rolls stack Exposed better than any other spell.

### Grand Mana Ward: Tier 4, defensive, all allies

- **Now:** 3 AP, 50 Mana, power 40 + Int ×0.6 + Wis ×0.3. Raise a broad Mana barrier that increases the Armor of every ally by [X] for 3 turns.
- **Reworked:** 3 AP, 50 Mana; power 10 + Mind ×0.61 + Wis ×0.18 (65% of the tier's reference, 75% for all allies, less its length). A **barrier** of [X] on every ally for 3 turns (was Armor +[X]).
- **Description:** "Raise a broad Mana barrier that absorbs the next [X] damage each ally takes within 3 turns."
- **[X], now → reworked:** Level 30: 130 → 69; Level 100: 324 → 194.
- **Augments:** Power, Effect duration, Mana cost.
- **Why:** Mana Ward for the whole party, at the all-allies share.

### Aether Rupture: Tier 4, offensive, one target

- **Now:** 3 AP, 52 Mana, power 45 + Int ×0.6 + Wis ×0.3. Rupture the Mana surrounding an enemy for [X] Basic damage, causing it to take 15% more spell damage for 2 turns.
- **Reworked:** 3 AP, 52 Mana; power 13 + Int ×0.75 + Wis ×0.22 (80% of the tier's reference). Always **Exposes** the target with 2 stacks for 3 turns (was: takes 15% more spell damage for 2 turns).
- **Description:** "Rupture the Mana around one enemy for [X] Basic damage and Expose it twice over for 3 turns."
- **[X], now → reworked:** Level 30: 135 → 114; Level 100: 329 → 332.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Today's effect was Exposed in all but name; it becomes the element's sure way to stack it, for less damage.

### Arcane Shrapnel: Tier 4, offensive, all enemies

- **Now:** 3 AP, 52 Mana, power 37 + Int ×0.6 + Wis ×0.3. Shatter Mana over all enemies for [X] Basic damage and siphon Mana equal to 10% of damage dealt.
- **Reworked:** 3 AP, 52 Mana; power 10 + Int ×0.61 + Wis ×0.18 (65% of the tier's reference, 75% for all enemies, less the siphon). Siphons Mana: you regain 10% of all the damage dealt as Mana; each enemy struck has a 15% chance to be **Exposed** (1 stack) for 2 turns.
- **Description:** "Shatter Mana over all enemies for [X] Basic damage each; regain Mana equal to 10% of the damage dealt, and each enemy has a 15% chance to be Exposed for 2 turns."
- **[X], now → reworked:** Level 30: 127 → 92; Level 100: 321 → 270.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** The siphon that grows with the number of enemies.

### Aether Cascade: Tier 5, offensive, all enemies

- **Now:** 3 AP, 74 Mana, power 42 + Int ×0.7 + Wis ×0.3. Flood the enemy line with unstable Mana for [X] Basic damage and reduce their Mind by 10% for 2 turns.
- **Reworked:** 3 AP, 74 Mana; power 15 + Int ×0.85 + Wis ×0.24 (70% of the tier's reference, 75% for all enemies, less the Mana burn). **Burns Mana:** each enemy struck loses Mana equal to 30% of the damage it took (was Mind −10% for 2 turns).
- **Description:** "Flood the enemy line with unstable Mana for [X] Basic damage; each enemy loses Mana equal to 30% of the damage it took."
- **[X], now → reworked:** Level 40: 175 → 163; Level 100: 362 → 374.
- **Augments:** Power, Resistance penetration, Effect strength, Mana cost.
- **Why:** Mana Fracture for a whole group of casters.

### Spellbreaker Lance: Tier 5, offensive, one target

- **Now:** 3 AP, 74 Mana, power 60 + Int ×0.7 + Wis ×0.3. Drive a Mana lance through an enemy for [X] Basic damage and siphon Mana equal to 25% of the damage dealt.
- **Reworked:** 3 AP, 74 Mana; power 19 + Int ×1.1 + Wis ×0.32 (90% of the tier's reference). Ends the target's newest buff (new); if it had none, the lance deals 20% more (was a 25% Mana siphon).
- **Description:** "Drive a spellbreaking lance through one enemy for [X] Basic damage, ending its newest buff; an enemy without one takes 20% more."
- **[X], now → reworked:** Level 40: 193 → 212; Level 100: 380 → 486.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Its name promised breaking spells: it now strips an enemy's buff, or hits harder when there's none.

### Mana Bastion: Tier 5, defensive, all allies

- **Now:** 3 AP, 72 Mana, power 48 + Int ×0.7 + Wis ×0.3. Grant all allies [X] Armor and 20% less incoming spell damage for 2 turns.
- **Reworked:** 3 AP, 72 Mana. Every ally takes 25% less spell damage and 10% less weapon damage for 3 turns (was Armor +[X] and 20% less spell damage for 2 turns).
- **Description:** "Raise a bastion of Mana around the party: for 3 turns every ally takes 25% less spell damage and 10% less weapon damage."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Grand Mana Ward absorbs a set amount; the Bastion cuts every hit for longer, the answer to a long fight against casters.

### Dragonbreath - Aether Storm: Tier 5, offensive, all enemies (Dragonbreath)

- **Now:** 3 AP, 75 Mana, power 42 + Int ×0.7 + Wis ×0.3. Exhale raw aether across all enemies for [X] Basic damage and reduce their Evasion by 15% for 2 turns.
- **Reworked:** 3 AP, 75 Mana; power 15 + Int ×0.85 + Wis ×0.24 (70% of the tier's reference, 75% for all enemies, less its weakening). Its weakening effect stays (Evasion −15% for 2 turns, which the Dragonkin's Dragon's Breath lengthens); each enemy also has a 25% chance to be **Exposed** (1 stack) for 2 turns.
- **Description:** "Exhale raw aether across all enemies for [X] Basic damage, reducing their Evasion by 15% for 2 turns, with a 25% chance to Expose each for 2 turns."
- **[X], now → reworked:** Level 40: 175 → 163; Level 100: 362 → 374.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Dragonbreath spells keep their weakening effect, which task 10's Dragonkin builds on; the rest follows the curve.

### Aether Nova: Tier 5, offensive, all enemies

- **Now:** 3 AP, 76 Mana, power 43 + Int ×0.7 + Wis ×0.3. Detonate Mana across all enemies for [X] Basic damage and reduce their Armor by 15% for 2 turns.
- **Reworked:** 3 AP, 76 Mana; power 14 + Int ×0.79 + Wis ×0.23 (65% of the tier's reference, 75% for all enemies, less its sure Exposed). Always **Exposes** every enemy struck (1 stack) for 2 turns (was Armor −15% for 2 turns, which is Earth's now).
- **Description:** "Detonate Mana across all enemies for [X] Basic damage and Expose every one of them for 2 turns."
- **[X], now → reworked:** Level 40: 176 → 153; Level 100: 363 → 350.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** The element's sure way to Expose a whole group; Armor loss belongs to Earth's Cracked.

### Arcane Cataclysm: Tier 6, offensive, all enemies

- **Now:** 3 AP, 96 Mana, power 52 + Int ×0.8 + Wis ×0.3. Unleash unstable Mana across all enemies for [X] Basic damage and reduce their Mind by 15% for 2 turns.
- **Reworked:** 3 AP, 96 Mana; power 15 + Int ×0.88 + Wis ×0.25 (60% of the tier's reference, now, and 30% more at the start of your next turn). A second wave strikes every enemy at the start of your next turn for half of [X] (new; was Mind −15% for 2 turns).
- **Description:** "Unleash unstable Mana across all enemies for [X] Basic damage; it strikes them again at the start of your next turn for half of that."
- **[X], now → reworked:** Level 50: 234 → 205; Level 100: 408 → 387.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** More than an all-enemies spell's 75% in all, paid for by waiting for the second half.

### Aether Severance: Tier 6, offensive, one target

- **Now:** 3 AP, 104 Mana, power 70 + Int ×0.8 + Wis ×0.3. Sever one enemy's Mana flow for [X] Basic damage and siphon Mana equal to 30% of the damage dealt.
- **Reworked:** 3 AP, 104 Mana; power 21 + Int ×1.25 + Wis ×0.36 (85% of the tier's reference). A 50% chance to **Sever** the target (new): it can't cast spells on its next turn (was a 30% Mana siphon).
- **Description:** "Sever one enemy's Mana flow for [X] Basic damage, with a 50% chance that it can't cast spells on its next turn."
- **[X], now → reworked:** Level 50: 252 → 292; Level 100: 426 → 551.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** The element's answer to a caster too strong to outlast: silence it for a turn.

### Arcane Reversal: Tier 6, defensive, yourself

- **Now:** 3 AP, 100 Mana, power 58 + Int ×0.8 + Wis ×0.3. For 2 turns, melee attackers take [X] Basic damage from you and incoming spell damage is reduced by 25%.
- **Reworked:** 3 AP, 100 Mana. The next enemy spell that targets you within 3 turns is turned back on its caster with 60% of its power (Ur-Mat's Rebounding Words, on a spell; was melee retaliation and 25% less spell damage, the same as Prism Aegis).
- **Description:** "Weave a reversal around yourself: the next enemy spell aimed at you within 3 turns rebounds on its caster with 60% of its power."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Today it is the same spell as Prism Aegis one tier lower; it becomes the element's spell reflection.

### Arcane Sundering: Tier 7, offensive, all enemies

- **Now:** 3 AP, 134 Mana, power 64 + Int ×0.8 + Wis ×0.4. Rend all enemies for [X] Basic damage and reduce Mind by 20% for 2 turns.
- **Reworked:** 3 AP, 134 Mana; power 20 + Int ×1.18 + Wis ×0.34 (65% of the tier's reference, 75% for all enemies, less the dispel). Ends the newest buff on every enemy struck (was Mind −20% for 2 turns).
- **Description:** "Rend all enemies for [X] Basic damage and tear away each one's newest buff."
- **[X], now → reworked:** Level 60: 295 → 325; Level 100: 443 → 520.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Spellbreaker Lance for a whole group, the answer to buffing bosses and their courts.

### Aether Verdict: Tier 7, offensive, one target

- **Now:** 3 AP, 146 Mana, power 82 + Int ×0.8 + Wis ×0.4. Strike one enemy for [X] Basic damage with a 15% chance to stun for 1 turn.
- **Reworked:** 3 AP, 146 Mana; power 26 + Int ×1.55 + Wis ×0.44 (85% of the tier's reference). Its **Resonance** counts double: +20% for each different status on the target, up to +60% (was a 15% chance to stun).
- **Description:** "Pass judgement on one enemy for [X] Basic damage; it deals 20% more for each different status the target carries, up to 60%."
- **[X], now → reworked:** Level 60: 313 → 425; Level 100: 461 → 682.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** The finisher for a party that mixes elements: the more statuses the target carries, the harder it lands.

### Prism Aegis: Tier 7, defensive, yourself or an ally

- **Now:** 3 AP, 140 Mana, power 64 + Int ×0.8 + Wis ×0.4. For 2 turns, melee attackers take [X] Basic retaliation damage and you take 25% less spell damage.
- **Reworked:** 3 AP, 140 Mana; power 31 + Mind ×1.82 + Wis ×0.52 (100% of the tier's reference, as a barrier). A **barrier** of [X] on yourself or an ally for 3 turns; while it holds they take 30% less spell damage, and when it breaks or fades it bursts on every enemy for half of [X] Basic damage (new; was melee retaliation and 25% less spell damage).
- **Description:** "Raise a prismatic aegis on yourself or an ally that absorbs the next [X] damage within 3 turns and cuts spell damage by 30% while it holds; when it ends, it bursts on all enemies for half of [X] Basic damage."
- **[X], now → reworked:** Level 60: 295 → 366; Level 100: 443 → 576.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** The element's great shield, which strikes back when it falls.

### Null Star: Tier 8, offensive, all enemies

- **Now:** 3 AP, 170 Mana, power 70 + Int ×0.9 + Wis ×0.5. Unmake all enemies for [X] Basic damage and lower Intelligence by 25% for 2 turns.
- **Reworked:** 3 AP, 170 Mana; power 16 + Int ×0.95 + Wis ×0.27 (45% of the tier's reference, now; each later strike 55% of [X], about 25% of the reference). The star hangs over the enemies (new): at the start of each of your next 2 turns it strikes every enemy for 55% of [X], each strike always **Exposing** them (1 stack) for 2 turns (was Intelligence −25% for 2 turns).
- **Description:** "Raise a null star over all enemies for [X] Basic damage; at the start of each of your next 2 turns it strikes them again for 55% of that and Exposes them."
- **[X], now → reworked:** Level 70: 380 → 300; Level 100: 508 → 418.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** A slow, sure stacker of Exposed for the whole group, stronger in all than a plain all-enemies spell because it waits.

### Absolute Aether: Tier 8, offensive, one target

- **Now:** 3 AP, 190 Mana, power 90 + Int ×0.9 + Wis ×0.5. Blast one enemy for [X] Basic damage with a 20% chance to stun for 1 turn.
- **Reworked:** 3 AP, 190 Mana; power 34 + Int ×1.99 + Wis ×0.57 (95% of the tier's reference). Ignores all resistance and barriers (new; was a 20% chance to stun).
- **Description:** "Blast one enemy with absolute aether for [X] Basic damage that no resistance or barrier can lessen."
- **[X], now → reworked:** Level 70: 400 → 630; Level 100: 528 → 877.
- **Augments:** Power, Mana cost.
- **Why:** The element of raw Mana can't be resisted at its height; Resistance penetration isn't offered, as it has nothing left to do.

### Fracture the Law: Tier 9, offensive, all enemies

- **Now:** 3 AP, 240 Mana, power 85 + Int ×1 + Wis ×0.5. Unravel all enemies for [X] Basic damage and lower Intelligence by 20% for 2 turns.
- **Reworked:** 3 AP, 240 Mana; power 27 + Int ×1.57 + Wis ×0.45 (60% of the tier's reference, 75% for all enemies, less the dispel and Exposed). Ends every buff on every enemy struck, and always **Exposes** each with 3 stacks for 2 turns (was Intelligence −20% for 2 turns).
- **Description:** "Unravel the laws that hold all enemies for [X] Basic damage, ending all of their buffs and Exposing each three times over for 2 turns."
- **[X], now → reworked:** Level 80: 467 → 562; Level 100: 559 → 692.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** Arcane Sundering at its height: everything the enemies built up, undone, and the party's spells hit 15% harder after.

### Axiom Lance: Tier 9, offensive, one target

- **Now:** 3 AP, 260 Mana, power 95 + Int ×1 + Wis ×0.5. Pierce one enemy for [X] Basic damage with a 20% chance to stun for 1 turn.
- **Reworked:** 3 AP, 260 Mana; power 45 + Int ×2.62 + Wis ×0.75 (100% of the tier's reference). An enemy left below 15% of its maximum HP is unmade (defeated; new); bosses can't be unmade (was a 20% chance to stun).
- **Description:** "Pierce one enemy with an axiom for [X] Basic damage; if it is left below 15% of its maximum HP, it is unmade. Bosses can't be."
- **[X], now → reworked:** Level 80: 477 → 938; Level 100: 569 → 1,155.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Three single-target spells at Tiers 7–9 all ended in a stun chance; the lance becomes the element's executioner.

### Axiom Unbound: Tier 10, offensive, all enemies

- **Now:** 3 AP, 350 Mana, power 220 + Int ×1.1 + Wis ×0.6. Unmake the laws governing all enemies for [X] Basic damage and reduce all their basic stats by 15% for 2 turns.
- **Reworked:** 3 AP, 350 Mana; power 48 + Int ×2.81 + Wis ×0.8 (70% of the tier's reference, 75% for all enemies, less its effects). All of every enemy's basic stats −15% for 3 turns (was 2), and always **Exposes** each with 3 stacks for 3 turns.
- **Description:** "Unmake the laws governing all enemies for [X] Basic damage, reducing all their basic stats by 15% and Exposing each three times over for 3 turns."
- **[X], now → reworked:** Level 90: 701 → 1,121; Level 100: 753 → 1,238.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Basic's Tier 10: the most power in the element, and every enemy left weaker and open to the party's spells.

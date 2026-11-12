# Batch 4: Earth (task 12)

**Status: approved (2026-11-11, SP38), for the port (SP2).** Every Earth spell players can learn, reworked: 31 spells (Stonecrust,
Nygheir's, is left out, SP4). They follow the approved system and curve (`Spell-Augmentation.md`), the sample's style
(`Spell-Sample.md`, where Summon - Stone Giant was approved) and the element statuses (`Element-Statuses.md`): each
spell sets its status's chance and duration (SP32). Tiers and prices stay. "(new)" marks a mechanic the game doesn't
have yet. [X] values are for the raw caster of the system draft (no classes or gear) at the level the tier unlocks and
at Level 100; Earth's spells scale with Intelligence and Strength, so a hybrid with more Strength gets more from them
than this caster.
**Updated 2026-11-12 (SP61):** some spells here borrow another element's status; each says so ("borrowed, SP61").

## Earth's identity

The element of stone, armor and the party's weapon users:
- **Cracked** (its status: Armor −8% per stack, up to 3) opens enemies to the party's weapons and to Nature's
  **Overgrowth**; its own reaction, **Topple**, knocks down Off-Balance enemies (Wind's status). Stone Spike, Fault
  Breaker, Sundering Rift, Continental Break and Mantlebreaker crack for certain; Mountain Crusher and Faultline Burst
  pay the Cracks off.
- **Armor for the party,** as percentages now: today's flat Armor buffs outgrow armor pieces at high levels (as in the
  Basic batch). Mountain Shelter and Worldheart Bastion cut damage outright.
- **Heavy blows:** stuns (Quake Hammer, Obsidian Spire, Meteor Spire, World's Weight), AP losses (Avalanche, Seismic
  Cathedral) and an anti-armor blow (World's Weight).
- **Guardians:** the Greater Earth Spirit may step in front of attacks on you (task 10's reworked Overprotective), the
  Earth Elemental raises your side's Armor, Obsidian Pillar shields its caster.
- Evasion loss moves to Wind's Off-Balance; Earth's Armor-lowering spells become Cracks (Dragonbreath - Erode keeps
  its Armor −30% for the Dragonkin).

New mechanics this batch adds for the port: bonus damage per Cracked stack, a summon that intercepts attacks on its
summoner, a summon that raises its side's Armor, a barrier from damage dealt, damage that grows with the target's
Armor, AP losses from a spell.

## At a glance

| Tier | Spell | Kind | AP | Mana | What changes |
|---|---|---|---|---|---|
| 1 | Minor Fortify | defensive, yourself | 3 | 9 | Armor +15% for 4 turns |
| 1 | Rock Bolt | offensive, one target | 3 | 13 | 20% chance to Crack |
| 1 | Knocked | offensive, yourself | 2 | 10 | 2 AP: next weapon attack +[X], 40% Crack, 10% stun |
| 1 | Pebble Spray | offensive, all enemies | 3 | 16 | 25% chance to Crack each enemy |
| 2 | Rock Body | defensive and offensive, yourself | 3 | 24 | Armor +20%; unarmed hits +[X] and 30% Crack, 3 turns |
| 2 | Dragonbreath - Erode | offensive, one target | 3 | 25 | Armor −30% for 3 turns kept (Dragonbreath) |
| 2 | Quick Drill | offensive, one target | 2 | 16 | 2 AP kept, 25% Crack |
| 2 | Stone Spike | offensive, one target | 3 | 24 | always Cracks |
| 3 | Faultline | offensive, all enemies | 3 | 36 | 40% chance to Crack each enemy |
| 3 | Stone Bastion | defensive, yourself or an ally | 3 | 35 | Armor +40%, Evasion −10%, 3 turns |
| 3 | Petrifying Grasp | offensive, one target | 3 | 34 | Speed −15% kept, 30% Crack |
| 3 | Fault Breaker | offensive, one target | 3 | 36 | always Cracks twice |
| 4 | Summon - Greater Earth Spirit | summon | 3 | 56 | a spirit that may intercept attacks on you; 40% Crack |
| 4 | Mountain Crusher | offensive, one target | 3 | 52 | +15% per Cracked stack, then Cracks twice |
| 4 | Fortress of Stone | defensive, all allies | 3 | 42 | every ally's Armor +25%, Evasion −10%, 3 turns |
| 4 | Quake Hammer | offensive, one target | 3 | 51 | 20% stun, 30% Crack |
| 5 | Sundering Rift | offensive, all enemies | 3 | 75 | always Cracks every enemy |
| 5 | Obsidian Spire | offensive, one target | 3 | 76 | 25% stun |
| 5 | Mountain Shelter | defensive, all allies | 3 | 71 | every ally takes 25% less weapon damage for 3 turns |
| 5 | Avalanche | offensive, all enemies | 3 | 77 | buries (25% lose 1 AP), 30% Chill (borrowed status) |
| 6 | Summon - Earth Elemental | summon | 3 | 100 | while it stands, your side's Armor +10% |
| 6 | Seismic Crown | offensive, all enemies | 3 | 96 | always Cracks every enemy, 40% a second time |
| 6 | Meteor Spire | offensive, one target | 3 | 104 | splashes 25%, 20% stun, 30% Burn (borrowed status) |
| 7 | Summon - Stone Giant | summon | 3 | 140 | as approved in the sample |
| 7 | Faultline Burst | offensive, all enemies | 3 | 134 | +10% per Cracked stack on each enemy |
| 7 | Obsidian Pillar | offensive, one target | 3 | 146 | a barrier of 30% of the damage dealt |
| 8 | Continental Break | offensive, all enemies | 3 | 170 | always Cracks every enemy twice |
| 8 | World's Weight | offensive, one target | 3 | 190 | hits harder the more Armor the target has; 20% stun |
| 9 | Seismic Cathedral | offensive, all enemies | 3 | 240 | every enemy loses 1 AP at its next turn |
| 9 | Mantlebreaker | offensive, one target | 3 | 260 | Cracks the target to the full 3 stacks |
| 10 | Worldheart Bastion | defensive, all allies | 3 | 350 | heals, Armor +50%, −20% spell damage, 3 turns |

## The spells

### Minor Fortify: Tier 1, defensive, yourself

- **Now:** 3 AP, 9 Mana, power none. Increase Armor's permanent bonus by 10% for 5 turns.
- **Reworked:** 3 AP, 9 Mana. Armor +15% for 4 turns on yourself (was +10% for 5 turns).
- **Description:** "Fortify yourself with earth, increasing your Armor by 15% for 4 turns."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Earth keeps its Armor buffs, as percentages, which stay in proportion with armor pieces.

### Rock Bolt: Tier 1, offensive, one target

- **Now:** 3 AP, 13 Mana, power 7 + Int ×0.3 + Str ×0.2. A bolt of rocks that deals [X] Earth damage.
- **Reworked:** 3 AP, 13 Mana; power 6 + Int ×0.33 + Str ×0.1 (95% of the tier's reference). A 20% chance to **Crack** the target (1 stack) for 3 turns.
- **Description:** "A bolt of rocks that deals [X] Earth damage, with a 20% chance to Crack the target for 3 turns."
- **[X], now → reworked:** Level 1: 12 → 10; Level 100: 135 → 135.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Earth's plain bolt, which now and then opens a target to the party's weapons.

### Knocked: Tier 1, offensive, yourself (your next weapon attack)

- **Now:** 3 AP, 13 Mana, power 3 + Int ×0.2 + Str ×0.1. Your next melee attack deals [X] additional Earth damage and has a 10% chance to stun the enemy.
- **Reworked:** 2 AP, 10 Mana; power 4 + Int ×0.23 + Str ×0.07 (65% of the tier's reference, the 2 AP rule). Your next weapon attack (melee or ranged, was melee) deals [X] more Earth damage, has a 40% chance to **Crack** the target (1 stack, 3 turns) and keeps its 10% chance to stun.
- **Description:** "Weigh your weapon with stone: your next attack deals [X] more Earth damage, with a 40% chance to Crack the target and a 10% chance to stun it."
- **[X], now → reworked:** Level 1: 6 → 7; Level 100: 85 → 94.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Earth's quick charge for weapon users, like Basic's Manastrike, with the element's status.

### Pebble Spray: Tier 1, offensive, all enemies

- **Now:** 3 AP, 16 Mana, power 3 + Int ×0.3 + Str ×0.2. Spray all enemies with sharp stones for [X] Earth damage.
- **Reworked:** 3 AP, 16 Mana; power 4 + Int ×0.23 + Str ×0.07 (65% of the tier's reference, 75% for all enemies, less its effect). Each enemy struck has a 25% chance to be **Cracked** (1 stack) for 2 turns.
- **Description:** "Spray all enemies with sharp stones for [X] Earth damage; each has a 25% chance to be Cracked for 2 turns."
- **[X], now → reworked:** Level 1: 8 → 7; Level 100: 131 → 94.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** The cheap group spell, with the element's status.

### Rock Body: Tier 2, defensive and offensive, yourself

- **Now:** 3 AP, 24 Mana, power 5 + Vit ×0.3 + - ×None. Transform your skin into rock. Increase Armor by 20% and add [X] Earth damage to unarmed attacks for 2 turns.
- **Reworked:** 3 AP, 24 Mana; power 4 + Str ×0.27 + Vit ×0.08 (50% of the tier's reference, per unarmed hit; Strength and Vitality, for the unarmed fighters who use it). For 3 turns (was 2): Armor +20%, your unarmed attacks deal [X] more Earth damage and have a 30% chance to **Crack** the target (1 stack, 3 turns).
- **Description:** "Turn your skin to rock for 3 turns: Armor +20%, and your unarmed blows deal [X] more Earth damage, each with a 30% chance to Crack."
- **[X], now → reworked:** Level 10: 10 → 11; Level 100: 37 → 42.
- **Augments:** Power, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Kept for unarmed fighters, whose 1 AP blows land it often.

### Dragonbreath - Erode: Tier 2, offensive, one target (Dragonbreath)

- **Now:** 3 AP, 25 Mana, power 11 + Int ×0.4 + Str ×0.2. Breathe a gust of eroding dust. Deal [X] Earth damage and decrease the target's Armor by 30% for 3 turns.
- **Reworked:** 3 AP, 25 Mana; power 7 + Int ×0.43 + Str ×0.13 (80% of the tier's reference). Its weakening effect stays (Armor −30% for 3 turns, which Dragon's Breath lengthens).
- **Description:** "Breathe a gust of eroding dust on one enemy for [X] Earth damage and reduce its Armor by 30% for 3 turns."
- **[X], now → reworked:** Level 10: 31 → 28; Level 100: 175 → 175.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Dragonbreath spells keep their weakening effect; its heavy Armor loss is already more than Cracked gives.

### Quick Drill: Tier 2, offensive, one target

- **Now:** 2 AP, 23 Mana, power 8 + Int ×0.4 + Str ×0.3. Shoot out a small earthen drill that deals [X] Earth damage. This spell costs 2 Action Points.
- **Reworked:** 2 AP, 16 Mana; power 6 + Int ×0.35 + Str ×0.1 (65% of the tier's reference, the 2 AP rule). Stays a 2 AP spell; a 25% chance to **Crack** the target (1 stack) for 3 turns.
- **Description:** "Shoot out a small earthen drill for [X] Earth damage, with a 25% chance to Crack the target for 3 turns. This spell costs 2 AP."
- **[X], now → reworked:** Level 10: 30 → 23; Level 100: 183 → 142.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Already Earth's fast spell; its power and Mana follow the 2 AP rule.

### Stone Spike: Tier 2, offensive, one target

- **Now:** 3 AP, 24 Mana, power 14 + Int ×0.4 + Str ×0.2. Drive stone into one enemy for [X] Earth damage and reduce its Armor by 10% for 2 turns.
- **Reworked:** 3 AP, 24 Mana; power 8 + Int ×0.46 + Str ×0.14 (85% of the tier's reference). Always **Cracks** the target (1 stack) for 3 turns (was Armor −10% for 2 turns).
- **Description:** "Drive stone into one enemy for [X] Earth damage and Crack it for 3 turns."
- **[X], now → reworked:** Level 10: 34 → 30; Level 100: 178 → 187.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Earth's sure single Crack at a low tier.

### Faultline: Tier 3, offensive, all enemies

- **Now:** 3 AP, 36 Mana, power 26 + Int ×0.5 + Str ×0.3. Split the ground beneath all enemies, dealing [X] Earth damage and decreasing their Evasion by 10% for 2 turns.
- **Reworked:** 3 AP, 36 Mana; power 8 + Int ×0.5 + Str ×0.14 (65% of the tier's reference, 75% for all enemies, less its effect). Each enemy struck has a 40% chance to be **Cracked** (1 stack) for 3 turns (was Evasion −10%, which is Wind's now).
- **Description:** "Split the ground beneath all enemies for [X] Earth damage; each has a 40% chance to be Cracked for 3 turns."
- **[X], now → reworked:** Level 20: 73 → 51; Level 100: 237 → 202.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Evasion loss belongs to Wind's Off-Balance; Earth cracks.

### Stone Bastion: Tier 3, defensive, yourself or an ally

- **Now:** 3 AP, 35 Mana, power 30 + Int ×0.5 + Str ×0.3. Encircle yourself or an ally with stone for 3 turns, increasing Armor by [X] but decreasing Evasion by 10%.
- **Reworked:** 3 AP, 35 Mana. Armor +40% and Evasion −10% for 3 turns on yourself or an ally (was Armor +[X], which grew past what armor pieces give).
- **Description:** "Encircle yourself or an ally with stone for 3 turns, increasing Armor by 40% but decreasing Evasion by 10%."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** A tank's buff, as a percentage so it stays in proportion (flat Armor from spells outgrew armor pieces, as in the Basic batch).

### Petrifying Grasp: Tier 3, offensive, one target

- **Now:** 3 AP, 34 Mana, power 34 + Int ×0.5 + Str ×0.3. Stone creeps over the target, dealing [X] Earth damage and decreasing Speed by 15% for 3 turns.
- **Reworked:** 3 AP, 34 Mana; power 11 + Int ×0.65 + Str ×0.19 (85% of the tier's reference). Speed −15% for 3 turns (kept); a 30% chance to **Crack** the target (1 stack) for 3 turns.
- **Description:** "Stone creeps over one enemy for [X] Earth damage, decreasing its Speed by 15% for 3 turns, with a 30% chance to Crack it."
- **[X], now → reworked:** Level 20: 81 → 67; Level 100: 245 → 264.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Kept as Earth's slow, with the element's status.

### Fault Breaker: Tier 3, offensive, one target

- **Now:** 3 AP, 36 Mana, power 38 + Int ×0.5 + Str ×0.3. Crack one enemy's guard for [X] Earth damage and reduce its Armor by 15% for 2 turns.
- **Reworked:** 3 AP, 36 Mana; power 10 + Int ×0.62 + Str ×0.18 (80% of the tier's reference). Always **Cracks** the target with 2 stacks for 3 turns (was Armor −15% for 2 turns).
- **Description:** "Crack one enemy's guard for [X] Earth damage, Cracking it twice over for 3 turns."
- **[X], now → reworked:** Level 20: 85 → 63; Level 100: 249 → 251.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** The quick way to a deeply Cracked target.

### Summon - Greater Earth Spirit: Tier 4, summon

- **Now:** 3 AP, 56 Mana, power none. Summon a friendly Greater Earth Spirit with 30% increased max HP and 20% increased attack damage.
- **Reworked:** 3 AP, 56 Mana; power 16 + Int ×0.94 + Str ×0.27 (100% of the tier's reference, as a summon). Summons a Greater Earth Spirit (Level 18, 195 HP today, summoned with +30% HP and +20% damage); its maximum HP rises by 200% of [X] and every attack it makes by 25% of [X] (SP29). Its hits have a 40% chance to **Crack** their target (1 stack, 3 turns); it has a 25% chance to intercept a weapon attack aimed at its summoner (task 10's reworked Overprotective, on a spell). One at a time.
- **Description:** "Summon a friendly Greater Earth Spirit whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more, each with a 40% chance to Crack; it has a 25% chance to step in front of a weapon attack aimed at you. Only one may be active at a time."
- **[X], now → reworked:** Level 30: - → 132; Level 100: - → 381.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Earth's summons guard; the interception follows the rule you gave Overprotective in task 10.

### Mountain Crusher: Tier 4, offensive, one target

- **Now:** 3 AP, 52 Mana, power 45 + Int ×0.6 + Str ×0.3. Drop crushing stone upon an enemy for [X] Earth damage, ignoring 60% of Armor and decreasing Armor by 20% for 3 turns.
- **Reworked:** 3 AP, 52 Mana; power 13 + Int ×0.75 + Str ×0.22 (80% of the tier's reference). Deals 15% more for each **Cracked** stack the target already has (up to +45%), then Cracks it with 2 stacks for 3 turns (was: ignores 60% of Armor, which spells already ignore, and Armor −20%).
- **Description:** "Drop crushing stone on one enemy for [X] Earth damage, 15% more for each Crack it already has; it is Cracked twice over for 3 turns."
- **[X], now → reworked:** Level 30: 123 → 106; Level 100: 291 → 305.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Its Armor pierce did nothing on a spell; it becomes the payoff for a Cracked target that also cracks it further.

### Fortress of Stone: Tier 4, defensive, all allies

- **Now:** 3 AP, 42 Mana, power 35 + Int ×0.6 + Str ×0.3. Raise protective stone around all allies, increasing Armor by [X] and decreasing Evasion by 10% for 3 turns.
- **Reworked:** 3 AP, 42 Mana. Every ally's Armor +25% and Evasion −10% for 3 turns (was Armor +[X]).
- **Description:** "Raise protective stone around all allies, increasing their Armor by 25% and decreasing their Evasion by 10% for 3 turns."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Stone Bastion for the party, as a percentage.

### Quake Hammer: Tier 4, offensive, one target

- **Now:** 3 AP, 51 Mana, power 48 + Int ×0.6 + Str ×0.3. Crush one enemy for [X] Earth damage with a 10% chance to stun it for 1 turn.
- **Reworked:** 3 AP, 51 Mana; power 14 + Int ×0.8 + Str ×0.23 (85% of the tier's reference). A 20% chance to stun the target for 1 turn (was 10%); a 30% chance to **Crack** it (1 stack) for 3 turns.
- **Description:** "Crush one enemy with a quaking hammer for [X] Earth damage, with a 20% chance to stun it and a 30% chance to Crack it."
- **[X], now → reworked:** Level 30: 126 → 113; Level 100: 294 → 325.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Earth's mid-tier stunning blow.

### Sundering Rift: Tier 5, offensive, all enemies

- **Now:** 3 AP, 75 Mana, power 43 + Int ×0.7 + Str ×0.3. Split the ground beneath all enemies for [X] Earth damage and reduce their Armor by 15% for 2 turns.
- **Reworked:** 3 AP, 75 Mana; power 13 + Int ×0.73 + Str ×0.21 (60% of the tier's reference, 75% for all enemies, less its sure Crack). Always **Cracks** every enemy struck (1 stack) for 3 turns (was Armor −15% for 2 turns).
- **Description:** "Split the ground beneath all enemies for [X] Earth damage and Crack every one of them for 3 turns."
- **[X], now → reworked:** Level 40: 160 → 131; Level 100: 325 → 296.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Earth's sure group Crack, for a party of weapon users and for Nature's Overgrowth.

### Obsidian Spire: Tier 5, offensive, one target

- **Now:** 3 AP, 76 Mana, power 61 + Int ×0.7 + Str ×0.3. Impale one enemy with jagged stone for [X] Earth damage and gain a 20% chance to stun it.
- **Reworked:** 3 AP, 76 Mana; power 19 + Int ×1.1 + Str ×0.32 (90% of the tier's reference). A 25% chance to stun the target for 1 turn (was 20%).
- **Description:** "Impale one enemy with jagged obsidian for [X] Earth damage, with a 25% chance to stun it for 1 turn."
- **[X], now → reworked:** Level 40: 178 → 196; Level 100: 343 → 447.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** Kept as Earth's plain stunning spire; the other spires of Tiers 6–8 now do something else.

### Mountain Shelter: Tier 5, defensive, all allies

- **Now:** 3 AP, 71 Mana, power 48 + Int ×0.7 + Str ×0.3. Raise [X] Armor around all allies and reduce incoming weapon damage by 15% for 2 turns.
- **Reworked:** 3 AP, 71 Mana. Every ally takes 25% less weapon damage for 3 turns (was Armor +[X] and 15% less weapon damage for 2 turns).
- **Description:** "Raise the mountain around all allies: for 3 turns they take 25% less weapon damage."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** A flat cut that works whatever the party's armor; note that task 10's Peakwarden has a skill named Mountain's Shelter, one letter apart.

### Avalanche: Tier 5, offensive, all enemies

- **Now:** 3 AP, 77 Mana, power 43 + Int ×0.7 + Str ×0.3. Bury all enemies for [X] Earth damage and reduce their Evasion by 20% for 2 turns.
- **Reworked:** 3 AP, 77 Mana; power 13 + Int ×0.73 + Str ×0.21 (60% of the tier's reference, 75% for all enemies, less its effect). Each enemy struck has a 25% chance to be buried: it loses 1 AP at its next turn (was Evasion −20%). Each enemy also has a 30% chance to be **Chilled** (Ice's status, borrowed, SP61; 1 stack) for 2 turns: snow and ice.
- **Description:** "Bury all enemies under an avalanche for [X] Earth damage; each has a 25% chance to lose 1 AP on its next turn. Each also has a 30% chance to be Chilled."
- **[X], now → reworked:** Level 40: 160 → 131; Level 100: 325 → 296.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Evasion loss belongs to Wind; an avalanche slows what it buries.

### Summon - Earth Elemental: Tier 6, summon

- **Now:** 3 AP, 100 Mana, power none. Summon a friendly Earth Elemental with 30% increased maximum HP. Only one may be active at a time.
- **Reworked:** 3 AP, 100 Mana; power 25 + Int ×1.47 + Str ×0.42 (100% of the tier's reference, as a summon). Summons an Earth Elemental (Level 25, 300 HP today, summoned with +30% HP); its maximum HP rises by 200% of [X] and every attack it makes by 25% of [X] (SP29). Its hits have a 40% chance to **Crack** their target (1 stack, 3 turns); while it stands, its side's Armor is 10% higher (new). One at a time.
- **Description:** "Summon a friendly Earth Elemental whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more, each with a 40% chance to Crack; while it stands, your side's Armor is 10% higher. Only one may be active at a time."
- **[X], now → reworked:** Level 50: - → 317; Level 100: - → 596.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Earth's elemental shields the party while it lives.

### Seismic Crown: Tier 6, offensive, all enemies

- **Now:** 3 AP, 96 Mana, power 52 + Int ×0.8 + Str ×0.3. Fracture the ground under all enemies for [X] Earth damage and reduce their Armor by 20% for 2 turns.
- **Reworked:** 3 AP, 96 Mana; power 15 + Int ×0.88 + Str ×0.25 (60% of the tier's reference, 75% for all enemies, less its Cracks). Always **Cracks** every enemy struck (1 stack) for 3 turns, with a 40% chance of a second stack (was Armor −20% for 2 turns).
- **Description:** "Fracture the ground under all enemies for [X] Earth damage, Cracking each for 3 turns, with a 40% chance to Crack it twice."
- **[X], now → reworked:** Level 50: 215 → 190; Level 100: 370 → 356.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Sundering Rift's stronger sibling.

### Meteor Spire: Tier 6, offensive, one target

- **Now:** 3 AP, 104 Mana, power 68 + Int ×0.8 + Str ×0.3. Impale one enemy with a falling stone spire for [X] Earth damage and gain a 30% chance to stun it.
- **Reworked:** 3 AP, 104 Mana; power 20 + Int ×1.18 + Str ×0.34 (80% of the tier's reference). The falling spire splashes every other enemy for 25% of [X] (new); a 20% chance to stun the target for 1 turn (was 30%). A 30% chance to set the target **Burning** (Fire's status, borrowed, SP61) for 2 turns, for 15% of the hit each turn: a falling meteor.
- **Description:** "Bring a stone spire down on one enemy for [X] Earth damage, splashing every other enemy for 25% of that, with a 20% chance to stun the target. The target has a 30% chance to catch fire for 2 turns."
- **[X], now → reworked:** Level 50: 231 → 255; Level 100: 386 → 478.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** A stunning spell that still touches the rest of a group.

### Summon - Stone Giant: Tier 7, summon

- **Now:** 3 AP, 140 Mana, power none. Summon a friendly Stone Giant with 15% increased maximum HP. Only one may be active at a time.
- **Reworked:** 3 AP, 140 Mana; power 31 + Int ×1.82 + Wis ×0.52 (100% of the tier's reference, as a summon). As approved in the sample (SP31, SP32): the Level 48 Stone Giant; its maximum HP rises by 200% of [X] and every attack it makes by 25% of [X]; each of its hits has a 40% chance to **Crack** the target (1 stack) for 3 turns. One at a time.
- **Description:** "Summon a friendly Stone Giant whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more. Each of its blows has a 40% chance to Crack its target for 3 turns. Only one may be active at a time."
- **[X], now → reworked:** Level 60: - → 500; Level 100: - → 802.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** As approved in the sample.

### Faultline Burst: Tier 7, offensive, all enemies

- **Now:** 3 AP, 134 Mana, power 64 + Int ×0.8 + Str ×0.4. Fracture the ground beneath all enemies for [X] Earth damage and reduce Armor by 25% for 2 turns.
- **Reworked:** 3 AP, 134 Mana; power 20 + Int ×1.18 + Str ×0.34 (65% of the tier's reference, 75% for all enemies, less the bonus). Each enemy takes 10% more for each **Cracked** stack it has (up to +30%) (was Armor −25% for 2 turns).
- **Description:** "Burst the fault lines beneath all enemies for [X] Earth damage, 10% more against each for every Crack it carries."
- **[X], now → reworked:** Level 60: 265 → 300; Level 100: 393 → 478.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** The group payoff for a Cracked field.

### Obsidian Pillar: Tier 7, offensive, one target

- **Now:** 3 AP, 146 Mana, power 82 + Int ×0.8 + Str ×0.4. Impale one enemy for [X] Earth damage with a 15% chance to stun for 1 turn.
- **Reworked:** 3 AP, 146 Mana; power 26 + Int ×1.55 + Str ×0.44 (85% of the tier's reference). The pillar stays between you: you gain a **barrier** of 30% of the damage dealt for 2 turns (new; was a 15% chance to stun).
- **Description:** "Raise an obsidian pillar through one enemy for [X] Earth damage; it shields you with a barrier of 30% of the damage dealt for 2 turns."
- **[X], now → reworked:** Level 60: 283 → 393; Level 100: 411 → 627.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** A spell that strikes and guards, for a caster on the front line.

### Continental Break: Tier 8, offensive, all enemies

- **Now:** 3 AP, 170 Mana, power 70 + Int ×0.9 + Str ×0.5. Crush all enemies for [X] Earth damage and reduce Armor by 30% for 2 turns.
- **Reworked:** 3 AP, 170 Mana; power 22 + Int ×1.26 + Str ×0.36 (60% of the tier's reference, 75% for all enemies, less its sure Cracks). Always **Cracks** every enemy struck with 2 stacks for 3 turns (was Armor −30% for 2 turns).
- **Description:** "Break the continent beneath all enemies for [X] Earth damage, Cracking each twice over for 3 turns."
- **[X], now → reworked:** Level 70: 336 → 368; Level 100: 445 → 511.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Earth's great group Crack.

### World's Weight: Tier 8, offensive, one target

- **Now:** 3 AP, 190 Mana, power 90 + Int ×0.9 + Str ×0.5. Crush one enemy for [X] Earth damage with a 25% chance to stun for 1 turn.
- **Reworked:** 3 AP, 190 Mana; power 32 + Int ×1.89 + Str ×0.54 (90% of the tier's reference). Deals 1% more for every 2 Armor the target has, up to +50% (new); a 20% chance to stun it for 1 turn (was 25%).
- **Description:** "Crush one enemy under the world's weight for [X] Earth damage, more the heavier its Armor (up to half again), with a 20% chance to stun it."
- **[X], now → reworked:** Level 70: 356 → 551; Level 100: 465 → 766.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** The answer to the most heavily armored enemies, whom spells otherwise treat like everyone.

### Seismic Cathedral: Tier 9, offensive, all enemies

- **Now:** 3 AP, 240 Mana, power 85 + Int ×1 + Str ×0.5. Shake all enemies for [X] Earth damage and lower Armor by 20% for 2 turns.
- **Reworked:** 3 AP, 240 Mana; power 27 + Int ×1.57 + Str ×0.45 (60% of the tier's reference, 75% for all enemies, less the AP loss). The ground heaves: every enemy struck loses 1 AP at its next turn (new; was Armor −20% for 2 turns).
- **Description:** "Shake the ground beneath all enemies for [X] Earth damage; every one of them loses 1 AP on its next turn."
- **[X], now → reworked:** Level 80: 416 → 518; Level 100: 496 → 637.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** A sure slow on the whole enemy side, at the tier where it can decide a fight.

### Mantlebreaker: Tier 9, offensive, one target

- **Now:** 3 AP, 260 Mana, power 95 + Int ×1 + Str ×0.5. Strike one enemy for [X] Earth damage and lower Evasion by 25% for 2 turns.
- **Reworked:** 3 AP, 260 Mana; power 38 + Int ×2.23 + Str ×0.64 (85% of the tier's reference). **Cracks** the target to the full 3 stacks for 3 turns (was Evasion −25% for 2 turns).
- **Description:** "Break one enemy's mantle for [X] Earth damage and Crack it three times over for 3 turns."
- **[X], now → reworked:** Level 80: 426 → 735; Level 100: 506 → 904.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Opens the strongest enemy fully to the party's weapons.

### Worldheart Bastion: Tier 10, defensive, all allies

- **Now:** 3 AP, 350 Mana, power 230 + Int ×1.1 + Str ×0.6. Restore [X] HP plus 10% maximum HP to all allies, increase their Armor by 50% and reduce incoming spell damage by 20% for 2 turns.
- **Reworked:** 3 AP, 350 Mana; power 41 + Int ×2.41 + Str ×0.69 (60% of the tier's reference, as a party heal, less its buffs). Heals every ally for [X] plus 10% of maximum HP, and for 3 turns (was 2) their Armor is 50% higher and they take 20% less spell damage.
- **Description:** "Call on the world's heart: every ally regains [X] HP plus 10% of maximum HP and, for 3 turns, has 50% more Armor and takes 20% less spell damage."
- **[X], now → reworked:** Level 90: 643 → 885; Level 100: 688 → 977.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** Earth's Tier 10 stays the great defence, a turn longer.

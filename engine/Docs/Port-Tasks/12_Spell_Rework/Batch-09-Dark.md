# Batch 9: Dark (task 12)

**Status: a draft for the user's review (SP2).** Every Dark spell players can learn, reworked: 32 spells (Summon - Imp
Retinue, which only enemies cast, is left out, SP4). They follow the approved system and curve
(`Spell-Augmentation.md`), the sample's style (`Spell-Sample.md`) and the element statuses (`Element-Statuses.md`):
each spell sets its status's chance and duration (SP32). Tiers and prices stay. "(new)" marks a mechanic the game
doesn't have yet. [X] values are for the raw caster of the system draft (no classes or gear) at the level the tier
unlocks and at Level 100; Dark's spells scale with Intelligence and Mind.

## Dark's identity

The element of curses and theft:
- **Hexed** (its status: the target deals 5% less damage per stack, up to 3) and **Devour** (its reaction: Dark on an
  Exposed enemy uses up Basic's stacks for +15% each). Holy's **Purify** ends Hex for extra damage; Blood's **Blood
  Harvest** feeds on Death's Withered. Shade Needle, Curse of Frailty, Void Maw, Eclipse Verdict and Blackened Horizon
  are built around Hexing; Abyssal Bolt pays it off.
- **Theft:** Dark takes what it gains. Dark Siphon, Nightpiercer and Eventide Absolute steal Mana (the target loses
  what you gain, unlike Basic's siphons, which only create it); Umbral Hook steals a buff and Sovereign's Nightlance
  every buff; Void Lance steals Intelligence and Wisdom; Night Drain and Nightspike drain life.
- **Curses on casters:** Spellstop swallows a spell whole and returns its Mana; Oblivion Needle makes the target's
  spells dearer; Void Crown and the Black Dragon weaken enemy spells; Eclipse Mantle and Umbral Veil make them miss.
- Today eight Dark spells all took Mana and three all lowered every stat; each now does something of its own. Night
  Mantle's Evasion used its power as a percentage; it gives a set amount now. Flat Armor goes.

New mechanics this batch adds for the port: stealing Mana (the target loses what you gain), stealing one buff or every
buff, stealing stats, swallowing a spell, dearer spells on a target, cutting enemy spells' damage, a more elusive
summon, a summon that weakens enemy spells.

## At a glance

| Tier | Spell | Kind | AP | Mana | What changes |
|---|---|---|---|---|---|
| 1 | Summon - Lesser Imp | summon | 3 | 14 | the imp grows with spell power; 30% Hex on its hits |
| 1 | Dark Bolt | offensive, one target | 3 | 10 | 20% chance to Hex |
| 1 | Dragonbreath - Intimidate | offensive, all enemies | 3 | 16 | Strength and Dexterity −5% kept (Dragonbreath), 30% Hex |
| 1 | Shade Needle | offensive, one target | 3 | 12 | 40% chance to Hex |
| 2 | Dark Siphon | offensive, one target | 3 | 27 | steals Mana: 25% of the damage |
| 2 | Spellstop | defensive, yourself | 3 | 25 | swallows the next spell that hits you and returns its Mana |
| 2 | Hellstride | defensive and offensive, yourself | 3 | 19 | Speed, Luck and weapon damage +15% for 3 turns |
| 2 | Umbral Hook | offensive, one target | 3 | 23 | steals the target's newest buff |
| 3 | Abyssal Bolt | offensive, one target | 3 | 36 | +15% per Hex stack on the target |
| 3 | Night Mantle | defensive, yourself | 3 | 34 | Evasion +25%, Dark damage −30% |
| 3 | Curse of Frailty | offensive, one target | 3 | 35 | all stats −10% for 3 turns and always Hexes |
| 3 | Night Drain | offensive, one target | 3 | 36 | drains life: heals you for 25% of the damage |
| 4 | Event Horizon | offensive, all enemies | 3 | 54 | all stats −10% on every enemy (kept) |
| 4 | Void Lance | offensive, one target | 3 | 50 | steals 10% Intelligence and Wisdom for 3 turns |
| 4 | Black Sanctuary | defensive, yourself or an ally | 3 | 46 | −30% spell damage; the next enemy to hit them is Hexed |
| 4 | Summon - Void Stalker | summon | 3 | 50 | an elusive hunter: Evasion +30%, 40% Hex |
| 4 | Shadow Rend | offensive, one target | 3 | 51 | 50% chance to Hex twice |
| 5 | Void Maw | offensive, all enemies | 3 | 75 | always Hexes every enemy |
| 5 | Nightpiercer | offensive, one target | 3 | 76 | steals Mana: 30% of the damage; 30% Hex |
| 5 | Eclipse Mantle | defensive, yourself | 3 | 70 | spells against you 25% likely to miss; 50% Hex on melee attackers |
| 5 | Eclipse Verdict | offensive, one target | 3 | 77 | always Hexes, twice if already Hexed |
| 6 | Nightfall Surge | offensive, all enemies | 3 | 96 | all stats −12% kept, 30% Hex |
| 6 | Oblivion Needle | offensive, one target | 3 | 104 | ends its newest buff; its spells cost 30% more for 2 turns |
| 6 | Umbral Veil | defensive, all allies | 3 | 100 | enemy spells 15% likely to miss every ally |
| 7 | Abyssal Wake | offensive, all enemies | 3 | 134 | 40% chance to Hex each enemy twice |
| 7 | Nightspike | offensive, one target | 3 | 146 | drains life: heals you for 30% of the damage |
| 7 | Shroud of Silence | defensive and offensive, yourself | 3 | 140 | Evasion +25%, −20% spell damage; your next attack +[X] |
| 8 | Summon - Black Dragon | summon | 3 | 180 | its attacks Hex every enemy they touch; enemies' spells −10% |
| 8 | Void Crown | offensive, all enemies | 3 | 170 | enemy spells deal 25% less for 2 turns |
| 9 | Blackened Horizon | offensive, all enemies | 3 | 240 | always Hexes every enemy twice |
| 9 | Sovereign's Nightlance | offensive, one target | 3 | 260 | steals every buff on the target |
| 10 | Eventide Absolute | offensive, all enemies | 3 | 350 | Hexes every enemy fully; steals Mana (20%) |

## The spells

### Summon - Lesser Imp: Tier 1, summon

- **Now:** 3 AP, 14 Mana, power none. Summon a friendly Lesser Imp to help you in battle.
- **Reworked:** 3 AP, 14 Mana; power 6 + Int ×0.35 + Mind ×0.1 (100% of the tier's reference, as a summon). Summons a Lesser Imp (Level 1, 11 HP today); its maximum HP rises by 200% of [X] and every attack and spell it makes by 25% of [X] (SP29). Its hits have a 30% chance to **Hex** their target (1 stack) for 3 turns. One at a time.
- **Description:** "Summon a friendly Lesser Imp whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more, each with a 30% chance to Hex. Only one may be active at a time."
- **[X], now → reworked:** Level 1: - → 10; Level 100: - → 154.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Today a Level 1 imp with 11 HP; it now grows with its caster's spell power.

### Dark Bolt: Tier 1, offensive, one target

- **Now:** 3 AP, 10 Mana, power 5 + Int ×0.3 + Wis ×0.2. A bolt of dark Mana that deals [X] Dark damage.
- **Reworked:** 3 AP, 10 Mana; power 6 + Int ×0.33 + Mind ×0.1 (95% of the tier's reference). A 20% chance to **Hex** the target (1 stack) for 3 turns.
- **Description:** "A bolt of dark Mana that deals [X] Dark damage, with a 20% chance to Hex the target for 3 turns."
- **[X], now → reworked:** Level 1: 10 → 10; Level 100: 158 → 147.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Dark's plain bolt, with the element's status.

### Dragonbreath - Intimidate: Tier 1, offensive, all enemies (Dragonbreath, no damage)

- **Now:** 3 AP, 19 Mana, power none. A gust of dark energy. All enemies lose 5% Strength and Dexterity for 3 turns.
- **Reworked:** 3 AP, 16 Mana. Its weakening effect stays (Strength and Dexterity −5% for 3 turns on every enemy); each enemy has a 30% chance to be **Hexed** (1 stack) for 3 turns (16 Mana, from 19).
- **Description:** "Breathe a gust of dark dread over all enemies: they lose 5% Strength and Dexterity for 3 turns, and each has a 30% chance to be Hexed."
- **Augments:** Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Dragonbreath spells keep their weakening effect; a curse fits the dread.

### Shade Needle: Tier 1, offensive, one target

- **Now:** 3 AP, 12 Mana, power 6 + Int ×0.3 + Mind ×0.1. Strike one enemy from its own shadow for [X] Dark damage and reduce its Evasion by 10% for 2 turns.
- **Reworked:** 3 AP, 12 Mana; power 5 + Int ×0.3 + Mind ×0.09 (85% of the tier's reference). A 40% chance to **Hex** the target (1 stack) for 3 turns (was Evasion −10%, which is Wind's now).
- **Description:** "Strike one enemy from its own shadow for [X] Dark damage, with a 40% chance to Hex it for 3 turns."
- **[X], now → reworked:** Level 1: 10 → 9; Level 100: 136 → 133.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** The Tier 1 spell built around Hexing.

### Dark Siphon: Tier 2, offensive, one target

- **Now:** 3 AP, 27 Mana, power 12 + Int ×0.5 + - ×None. Deal [X] Dark damage. The target loses Mana equal to 10% of the damage and you restore the same amount.
- **Reworked:** 3 AP, 27 Mana; power 8 + Int ×0.46 + Mind ×0.14 (85% of the tier's reference). **Steals Mana:** the target loses Mana equal to 25% of the damage and you gain it (was 10%).
- **Description:** "Siphon one enemy for [X] Dark damage; it loses Mana equal to a quarter of the damage, and you gain it."
- **[X], now → reworked:** Level 10: 33 → 32; Level 100: 190 → 205.
- **Augments:** Power, Resistance penetration, Effect strength, Mana cost.
- **Why:** Dark is the thief: it takes what it gains (Basic's siphons only create Mana).

### Spellstop: Tier 2, defensive, yourself

- **Now:** 3 AP, 25 Mana, power none. Take 50% less damage from the next spell that hits you.
- **Reworked:** 3 AP, 25 Mana. The next enemy spell that hits you within 3 turns is swallowed: it has no effect on you and you gain Mana equal to its cost (the Abyss items' absorbing, on a spell; was 50% less from the next spell).
- **Description:** "Open a void before you for 3 turns: the next enemy spell that hits you is swallowed whole, and you gain Mana equal to its cost."
- **Augments:** Effect duration, Mana cost.
- **Why:** Basic's Arcane Intercept halves a spell; Dark's Spellstop swallows it and feeds on it.

### Hellstride: Tier 2, defensive and offensive, yourself

- **Now:** 3 AP, 19 Mana, power none. Increase Speed and Luck by 10% for 2 turns. Melee and ranged attacks deal 10% more damage while active.
- **Reworked:** 3 AP, 19 Mana. For 3 turns: Speed and Luck +15% and your weapon attacks deal 15% more (was +10% for 2 turns).
- **Description:** "Stride with hellish swagger for 3 turns: your Speed and Luck rise by 15%, and your weapon attacks deal 15% more."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Kept for Dark's weapon users, made worth its turn.

### Umbral Hook: Tier 2, offensive, one target

- **Now:** 3 AP, 23 Mana, power 12 + Int ×0.5 + Mind ×0.1. Snare one enemy with shadow for [X] Dark damage and reduce its Speed by 10% for 2 turns.
- **Reworked:** 3 AP, 23 Mana; power 8 + Int ×0.46 + Mind ×0.14 (85% of the tier's reference). **Steals** the target's newest buff: it ends on the target and you gain it for its remaining turns (new; was Speed −10%).
- **Description:** "Snare one enemy with a hook of shadow for [X] Dark damage and steal its newest buff for yourself."
- **[X], now → reworked:** Level 10: 36 → 32; Level 100: 213 → 205.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** The thief's first trick: what the enemy cast on itself becomes yours.

### Abyssal Bolt: Tier 3, offensive, one target

- **Now:** 3 AP, 36 Mana, power 34 + Int ×0.6 + Mind ×0.1. Strike with compressed darkness for [X] Dark damage, siphoning Mana equal to 15% of the damage dealt.
- **Reworked:** 3 AP, 36 Mana; power 12 + Int ×0.69 + Mind ×0.2 (90% of the tier's reference). Deals 15% more for each **Hex** stack on the target, up to +45% (new; was a 15% Mana siphon).
- **Description:** "Strike one enemy with compressed darkness for [X] Dark damage, 15% more for each Hex on it."
- **[X], now → reworked:** Level 20: 85 → 76; Level 100: 271 → 305.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** The single-target payoff for a Hexed enemy.

### Night Mantle: Tier 3, defensive, yourself

- **Now:** 3 AP, 34 Mana, power 30 + Int ×0.6 + Mind ×0.1. Disappear beneath a mantle of night, increasing Evasion by [X]% and reducing Dark spell damage by 30% for 2 turns.
- **Reworked:** 3 AP, 34 Mana. For 2 turns: Evasion +25% and 30% less Dark damage (was Evasion +[X]%, a power used as a percentage).
- **Description:** "Disappear beneath a mantle of night for 2 turns: your Evasion rises by 25% and Dark damage against you drops by 30%."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Its Evasion grew with spell power into the hundreds of percent, as Wind's did; it gives a set amount now.

### Curse of Frailty: Tier 3, offensive, one target

- **Now:** 3 AP, 35 Mana, power 26 + Int ×0.6 + Mind ×0.1. Afflict an enemy for [X] Dark damage and decrease all of its basic stats by 10% for 2 turns.
- **Reworked:** 3 AP, 35 Mana; power 10 + Int ×0.58 + Mind ×0.17 (75% of the tier's reference). All of the target's basic stats −10% for 3 turns (was 2), and it is always **Hexed** (1 stack) for 3 turns.
- **Description:** "Curse one enemy with frailty for [X] Dark damage: all its basic stats drop by 10% and it is Hexed, both for 3 turns."
- **[X], now → reworked:** Level 20: 77 → 64; Level 100: 263 → 257.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Dark's single-target curse.

### Night Drain: Tier 3, offensive, one target

- **Now:** 3 AP, 36 Mana, power 36 + Int ×0.6 + Mind ×0.1. Drain one enemy for [X] Dark damage and siphon Mana equal to 20% of the damage dealt.
- **Reworked:** 3 AP, 36 Mana; power 11 + Int ×0.65 + Mind ×0.19 (85% of the tier's reference). **Drains life:** you heal for 25% of the damage dealt (was a 20% Mana siphon).
- **Description:** "Drain one enemy's life for [X] Dark damage, healing yourself for a quarter of it."
- **[X], now → reworked:** Level 20: 87 → 71; Level 100: 273 → 287.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** One of eight Dark spells that took Mana; this one takes life instead.

### Event Horizon: Tier 4, offensive, all enemies

- **Now:** 3 AP, 54 Mana, power 35 + Int ×0.7 + Mind ×0.1. Collapse darkness around all enemies for [X] Dark damage and decrease all of their basic stats by 10% for 2 turns.
- **Reworked:** 3 AP, 54 Mana; power 10 + Int ×0.56 + Mind ×0.16 (60% of the tier's reference, 75% for all enemies, less the curse). All of every enemy's basic stats −10% for 2 turns (kept).
- **Description:** "Collapse darkness around all enemies for [X] Dark damage and decrease all their basic stats by 10% for 2 turns."
- **[X], now → reworked:** Level 30: 120 → 85; Level 100: 308 → 247.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Kept as Dark's group curse.

### Void Lance: Tier 4, offensive, one target

- **Now:** 3 AP, 50 Mana, power 45 + Int ×0.7 + Mind ×0.1. Pierce an enemy with a lance of void energy for [X] Dark damage, ignoring 35% of Armor and siphoning Mana equal to 20% of damage dealt.
- **Reworked:** 3 AP, 50 Mana; power 14 + Int ×0.8 + Mind ×0.23 (85% of the tier's reference). **Steals** the target's power: its Intelligence and Wisdom −10% and yours +10% for 3 turns (new; was: ignores 35% of Armor, which spells already ignore, and a 20% Mana siphon).
- **Description:** "Pierce one enemy with a lance of void for [X] Dark damage, stealing a tenth of its Intelligence and Wisdom for yourself for 3 turns."
- **[X], now → reworked:** Level 30: 130 → 121; Level 100: 318 → 353.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Its Armor pierce did nothing on a spell; it steals an enemy caster's strength instead.

### Black Sanctuary: Tier 4, defensive, yourself or an ally

- **Now:** 3 AP, 46 Mana, power 40 + Int ×0.7 + Mind ×0.1. Enshroud yourself or an ally in protective darkness, increasing Armor by [X] and reducing all spell damage by 30% for 3 turns.
- **Reworked:** 3 AP, 46 Mana. For 3 turns: 30% less spell damage, and the next enemy that hits them is always **Hexed** (1 stack) for 3 turns (was Armor +[X], which outgrew armor pieces).
- **Description:** "Enshroud yourself or an ally in protective darkness for 3 turns: 30% less spell damage, and the next enemy to strike them is Hexed."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Flat Armor goes; the darkness curses whoever reaches through it.

### Summon - Void Stalker: Tier 4, summon

- **Now:** 3 AP, 50 Mana, power none. Summon a friendly Void Stalker, an elusive Impid hunter that fights with claws and Dark magic.
- **Reworked:** 3 AP, 50 Mana; power 16 + Int ×0.94 + Mind ×0.27 (100% of the tier's reference, as a summon). Summons a Void Stalker (Level 16, 129 HP today); its maximum HP rises by 200% of [X] and every attack and spell it makes by 25% of [X] (SP29). It is elusive: its Evasion is 30% higher (new); its hits have a 40% chance to **Hex** their target (1 stack, 3 turns). One at a time.
- **Description:** "Summon a friendly Void Stalker, an elusive Impid hunter whose maximum HP rises by twice [X], whose attacks and spells deal a quarter of [X] more, each with a 40% chance to Hex, and whose Evasion is 30% higher. Only one may be active at a time."
- **[X], now → reworked:** Level 30: - → 142; Level 100: - → 414.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** The hunter its description promises, growing with spell power.

### Shadow Rend: Tier 4, offensive, one target

- **Now:** 3 AP, 51 Mana, power 48 + Int ×0.7 + Mind ×0.1. Tear through one enemy for [X] Dark damage and reduce its healing received by 20% for 2 turns.
- **Reworked:** 3 AP, 51 Mana; power 14 + Int ×0.8 + Mind ×0.23 (85% of the tier's reference). A 50% chance to **Hex** the target with 2 stacks for 3 turns (was healing received −20%, which is Death's now).
- **Description:** "Tear through one enemy with shadow for [X] Dark damage, with a 50% chance to Hex it twice over for 3 turns."
- **[X], now → reworked:** Level 30: 133 → 121; Level 100: 321 → 353.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Healing loss belongs to Death's Withered.

### Void Maw: Tier 5, offensive, all enemies

- **Now:** 3 AP, 75 Mana, power 42 + Int ×0.8 + Mind ×0.1. Open a hungry void beneath all enemies for [X] Dark damage and reduce all their basic stats by 10% for 2 turns.
- **Reworked:** 3 AP, 75 Mana; power 13 + Int ×0.73 + Mind ×0.21 (60% of the tier's reference, 75% for all enemies, less its sure Hex). Always **Hexes** every enemy struck (1 stack) for 3 turns (was all stats −10%, as Event Horizon does).
- **Description:** "Open a hungry void beneath all enemies for [X] Dark damage, Hexing every one of them for 3 turns."
- **[X], now → reworked:** Level 40: 169 → 141; Level 100: 350 → 323.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Today it repeats Event Horizon; it becomes Dark's sure group Hex.

### Nightpiercer: Tier 5, offensive, one target

- **Now:** 3 AP, 76 Mana, power 61 + Int ×0.8 + Mind ×0.1. Strike one enemy for [X] Dark damage and siphon Mana equal to 30% of the damage dealt.
- **Reworked:** 3 AP, 76 Mana; power 19 + Int ×1.1 + Mind ×0.32 (90% of the tier's reference). **Steals Mana:** the target loses Mana equal to 30% of the damage and you gain it (was a siphon that only gave you Mana); a 30% chance to **Hex** it (1 stack) for 3 turns.
- **Description:** "Pierce one enemy with night for [X] Dark damage, stealing Mana equal to 30% of the damage, with a 30% chance to Hex it."
- **[X], now → reworked:** Level 40: 188 → 212; Level 100: 369 → 486.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Dark Siphon's stronger sibling.

### Eclipse Mantle: Tier 5, defensive, yourself

- **Now:** 3 AP, 70 Mana, power 48 + Int ×0.8 + Mind ×0.1. Wrap yourself in darkness for 2 turns: melee attackers take [X] Dark damage and incoming spells have a 20% chance to miss.
- **Reworked:** 3 AP, 70 Mana. For 2 turns: enemy spells aimed at you have a 25% chance to miss (was 20%), and melee attackers have a 50% chance to be **Hexed** (1 stack) for 3 turns (was retaliation damage).
- **Description:** "Wrap yourself in an eclipse for 2 turns: enemy spells have a 25% chance to miss you, and melee attackers have a 50% chance to be Hexed."
- **Augments:** Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** A caster's shield of darkness that curses whoever closes in.

### Eclipse Verdict: Tier 5, offensive, one target

- **Now:** 3 AP, 77 Mana, power 61 + Int ×0.8 + Mind ×0.1. Condemn one enemy for [X] Dark damage and reduce its Wisdom by 15% for 2 turns.
- **Reworked:** 3 AP, 77 Mana; power 18 + Int ×1.04 + Mind ×0.3 (85% of the tier's reference). Always **Hexes** the target for 3 turns: 1 stack, or 2 if it is already Hexed (was Wisdom −15%).
- **Description:** "Condemn one enemy under the eclipse for [X] Dark damage, Hexing it for 3 turns, twice over if it already is."
- **[X], now → reworked:** Level 40: 188 → 200; Level 100: 369 → 459.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** The quick way to a fully Hexed target.

### Nightfall Surge: Tier 6, offensive, all enemies

- **Now:** 3 AP, 96 Mana, power 52 + Int ×0.9 + Mind ×0.1. Drown all enemies in darkness for [X] Dark damage and reduce all their basic stats by 12% for 2 turns.
- **Reworked:** 3 AP, 96 Mana; power 14 + Int ×0.81 + Mind ×0.23 (55% of the tier's reference, 75% for all enemies, less its effects). All of every enemy's basic stats −12% for 2 turns (kept); each enemy struck has a 30% chance to be **Hexed** (1 stack) for 3 turns.
- **Description:** "Drown all enemies in nightfall for [X] Dark damage, reducing all their basic stats by 12% for 2 turns, with a 30% chance to Hex each."
- **[X], now → reworked:** Level 50: 227 → 189; Level 100: 396 → 357.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Event Horizon's stronger sibling, with the element's status.

### Oblivion Needle: Tier 6, offensive, one target

- **Now:** 3 AP, 104 Mana, power 70 + Int ×0.9 + Mind ×0.1. Pierce one enemy with void energy for [X] Dark damage and siphon Mana equal to 35% of the damage dealt.
- **Reworked:** 3 AP, 104 Mana; power 21 + Int ×1.25 + Mind ×0.36 (85% of the tier's reference). The target forgets: its newest buff ends and its spells cost 30% more Mana for 2 turns (the Plasma Blade's dearer spells, on a spell; was a 35% Mana siphon).
- **Description:** "Pierce one enemy with oblivion for [X] Dark damage: its newest buff ends, and for 2 turns its spells cost 30% more Mana."
- **[X], now → reworked:** Level 50: 245 → 292; Level 100: 414 → 551.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** An answer to an enemy caster that doesn't take its Mana, but makes it last less.

### Umbral Veil: Tier 6, defensive, all allies

- **Now:** 3 AP, 100 Mana, power 58 + Int ×0.9 + Mind ×0.1. For 2 turns, melee attackers take [X] Dark damage from you and incoming spells have a 25% chance to miss.
- **Reworked:** 3 AP, 100 Mana. For 2 turns, enemy spells aimed at any ally have a 15% chance to miss (was retaliation and 25% on yourself, the same as Eclipse Mantle).
- **Description:** "Draw an umbral veil over the party: for 2 turns, enemy spells aimed at any of them have a 15% chance to miss."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Today it repeats Eclipse Mantle; it becomes the party's version.

### Abyssal Wake: Tier 7, offensive, all enemies

- **Now:** 3 AP, 134 Mana, power 64 + Int ×0.9 + Mind ×0.2. Engulf all enemies for [X] Dark damage and lower Wisdom by 20% for 2 turns.
- **Reworked:** 3 AP, 134 Mana; power 19 + Int ×1.09 + Mind ×0.31 (60% of the tier's reference, 75% for all enemies, less its effect). Each enemy struck has a 40% chance to be **Hexed** with 2 stacks for 3 turns (was Wisdom −20%).
- **Description:** "Engulf all enemies in an abyssal wake for [X] Dark damage; each has a 40% chance to be Hexed twice over for 3 turns."
- **[X], now → reworked:** Level 60: 287 → 300; Level 100: 431 → 480.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** A strong group Hex for the high tiers.

### Nightspike: Tier 7, offensive, one target

- **Now:** 3 AP, 146 Mana, power 82 + Int ×0.9 + Mind ×0.2. Pierce one enemy for [X] Dark damage and heal for 15% of damage dealt.
- **Reworked:** 3 AP, 146 Mana; power 28 + Int ×1.64 + Mind ×0.47 (90% of the tier's reference). **Drains life:** you heal for 30% of the damage dealt (was 15%).
- **Description:** "Pierce one enemy with a nightspike for [X] Dark damage, healing yourself for 30% of it."
- **[X], now → reworked:** Level 60: 305 → 451; Level 100: 449 → 723.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Kept as Dark's great life drain, made stronger.

### Shroud of Silence: Tier 7, defensive and offensive, yourself

- **Now:** 3 AP, 140 Mana, power 64 + Int ×0.9 + Mind ×0.2. Your next melee attack deals [X] extra Dark damage. Gain 25% Evasion and take 20% less spell damage for 2 turns.
- **Reworked:** 3 AP, 140 Mana; power 19 + Int ×1.09 + Str ×0.31 (60% of the tier's reference, on the next weapon attack). For 2 turns: Evasion +25% and 20% less spell damage; your next weapon attack (melee or ranged, was melee) deals [X] more Dark damage and always **Hexes** (1 stack, 3 turns).
- **Description:** "Wrap yourself in a shroud of silence for 2 turns: Evasion +25% and 20% less spell damage; your next attack deals [X] more Dark damage and Hexes its target."
- **[X], now → reworked:** Level 60: 287 → 277; Level 100: 431 → 442.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** Kept for Dark's shadowy fighters, with the element's status.

### Summon - Black Dragon: Tier 8, summon

- **Now:** 3 AP, 180 Mana, power none. Summon a friendly Black Dragon. Only one may be active at a time.
- **Reworked:** 3 AP, 180 Mana; power 36 + Int ×2.1 + Mind ×0.6 (100% of the tier's reference, as a summon). Summons a Black Dragon (Level 60, 1,639 HP today); its maximum HP rises by 200% of [X] and every attack and spell it makes by 25% of [X] (SP29). Its hits have a 40% chance to **Hex** their target (1 stack, 3 turns); while it stands, enemy spells deal 10% less (new). One at a time.
- **Description:** "Summon a friendly Black Dragon whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more, each with a 40% chance to Hex; while it stands, enemy spells deal 10% less. Only one may be active at a time."
- **[X], now → reworked:** Level 70: - → 664; Level 100: - → 926.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Dark's great summon shadows the enemy's casters while it lives.

### Void Crown: Tier 8, offensive, all enemies

- **Now:** 3 AP, 170 Mana, power 70 + Int ×1 + Mind ×0.3. Engulf all enemies for [X] Dark damage and lower Wisdom by 30% for 2 turns.
- **Reworked:** 3 AP, 170 Mana; power 22 + Int ×1.26 + Mind ×0.36 (60% of the tier's reference, 75% for all enemies, less the weakening). Every enemy's spells deal 25% less for 2 turns (was Wisdom −30%).
- **Description:** "Crown all enemies in void for [X] Dark damage; for 2 turns their spells deal 25% less."
- **[X], now → reworked:** Level 70: 371 → 399; Level 100: 496 → 556.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Wisdom loss did little; a direct cut on enemy spells is the point of it.

### Blackened Horizon: Tier 9, offensive, all enemies

- **Now:** 3 AP, 240 Mana, power 85 + Int ×1.1 + Mind ×0.3. Enshroud all enemies for [X] Dark damage and lower Wisdom by 20% for 2 turns.
- **Reworked:** 3 AP, 240 Mana; power 25 + Int ×1.44 + Mind ×0.41 (55% of the tier's reference, 75% for all enemies, less its sure Hexes). Always **Hexes** every enemy struck with 2 stacks for 3 turns (was Wisdom −20%).
- **Description:** "Enshroud all enemies under a blackened horizon for [X] Dark damage, Hexing each twice over for 3 turns."
- **[X], now → reworked:** Level 80: 457 → 515; Level 100: 547 → 635.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Dark's great group Hex: every enemy deals 10% less.

### Sovereign's Nightlance: Tier 9, offensive, one target

- **Now:** 3 AP, 260 Mana, power 95 + Int ×1.1 + Mind ×0.3. Pierce one enemy for [X] Dark damage and drain Mana equal to 15% of the damage dealt.
- **Reworked:** 3 AP, 260 Mana; power 40 + Int ×2.36 + Mind ×0.68 (90% of the tier's reference). **Steals** every buff on the target: they end on it and you gain them for their remaining turns (new; was a 15% Mana drain).
- **Description:** "Pierce one enemy with the sovereign's nightlance for [X] Dark damage and take every one of its buffs for yourself."
- **[X], now → reworked:** Level 80: 467 → 845; Level 100: 557 → 1,041.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Umbral Hook at its height: a buffed boss's strength becomes yours.

### Eventide Absolute: Tier 10, offensive, all enemies

- **Now:** 3 AP, 350 Mana, power 230 + Int ×1.2 + Mind ×0.4. Cast the last night over all enemies for [X] Dark damage, drain Mana equal to 30% of damage dealt and lower their Wisdom by 25% for 2 turns.
- **Reworked:** 3 AP, 350 Mana; power 38 + Int ×2.21 + Mind ×0.63 (55% of the tier's reference, 75% for all enemies, less its effects). Every enemy struck is **Hexed** to the full 3 stacks for 3 turns, and **steals Mana:** each loses Mana equal to 20% of the damage it took and you gain it (was a 30% drain and Wisdom −25%).
- **Description:** "Cast the last night over all enemies for [X] Dark damage: each is Hexed three times over for 3 turns and loses Mana equal to a fifth of the damage, which you gain."
- **[X], now → reworked:** Level 90: 700 → 882; Level 100: 751 → 974.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Dark's Tier 10: every enemy weakened to the full and robbed.

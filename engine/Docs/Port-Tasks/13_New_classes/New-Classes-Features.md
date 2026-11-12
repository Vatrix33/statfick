# Task 13, New Classes: the features

**For the user's review: the sample of four (written 2026-11-12, NC10).** The level-by-level features of the 79 new
classes, made for the port (NC1). Each keeps the identity, levels, requirements and rarity approved in
`New-Classes-Draft.md` (NC9). It starts with a sample of four, one class from each focus drawn at random, as task 10's
features began (C20); the rest follow in batches once the sample is approved, the user's notes on each batch shaping
the next (C22).

**The rules** (task 10's, approved 2026-11-10, C21, C28; `../10_Class_Rework/Class-Rework-Features.md`):
- A class has **one passive and one skill**, and its last level gives one of them (the capstone rule). The Lycanthrope,
  a class from a rare item, may have more, as the item classes do.
- Every level does something worth taking; small bonuses are merged rather than spread over several levels.
- Skills work once per battle unless they are big bursts.
- No penalties that make a feature miss more than it helps.
- Power follows the rarity.
- Requirements and skills fit within a turn's 5 AP.
- A passive applies in full when it is gained (SP68).

**How to read it:** the new classes have no old version, so each has one column, its levels as proposed, then "How it
plays", what the levels add up to. Numbers are proposals to tune when built. "(new)" marks something the old game can't
do and the port would need to add.

**Contents**
- The sample of four (drawn at random, one from each focus): Bladebound (melee), Harrier (ranged), Catalyst (spells),
  Mimic (the rogue's path).

# The sample of four

## Bladebound

6 levels · rarity blue · Requires: Level 30 · Weaponmaster 5 · Defeat 300 enemies with one and the same weapon
Identity: Bound to one weapon for life: the weapon it has fought with longest grows stronger with it, and fights best
in its hands.

| Level | Feature |
|---|---|
| 1 | Strength and Dexterity +5% |
| 2 | **Bound Blade** (passive): the weapon you have defeated the most enemies with is your bound weapon. It rises to your level whenever you level up (its power and scaling as if forged at your level), and gains 1% weapon power for every 20 enemies it has defeated, up to +25% (at 500). A Blacksmith can bind another weapon for a fee, carrying half of the growth over (new) |
| 3 | With your bound weapon: Accuracy +10% and Crit Rating +5 |
| 4 | Maximum HP and Armor +10% |
| 5 | Your bound weapon also scales with your highest stat, at 0.2× (new) |
| 6 | **One with the Blade** (skill, capstone; once per battle, 1 AP): for 3 turns your bound weapon's growth counts double and its hits can't miss |

How it plays: it takes up the weapon it already knows best (the requirement's 300 enemies start it at +15%) and never
has to give it up: the weapon keeps pace with its level, so an old favourite never falls behind a newly forged one.
Every enemy it fells makes the weapon a little stronger, and once a battle the bond shows in full. A Weaponmaster
masters every weapon; the Bladebound masters one.

## Harrier

5 levels · rarity no colour · Requires: Level 20 · Archer 5 · Scout 3
Identity: Never lets the enemy settle: shots that slow, pin and unsettle, so the party moves first and the enemy never
gets close.

| Level | Feature |
|---|---|
| 1 | Dexterity and Speed +5% |
| 2 | **Harrying Shots** (passive): your first ranged hit on an enemy each turn slows it: its turn bar fills 10% slower for 2 turns, up to 2 stacks (−20%) (new) |
| 3 | Ranged weapon Accuracy +10% |
| 4 | Enemies you have slowed have 10% less Accuracy |
| 5 | **Pin Down** (skill, capstone; once per battle, 2 AP): a ranged attack that Roots the target for 2 turns (Evasion halved, can't flee) and pushes its next turn back by 150 |

How it plays: it does less damage than the hunters and sharpshooters, but every shot costs the enemy time: spread over
the field, its slows let the whole party act more often than the enemy, and the slowed enemies miss more. Once a battle
it pins the most dangerous one in place. Where the Chronomancer bends time with spells, the Harrier does it with
arrows, one enemy at a time.

## Catalyst

6 levels · rarity blue · Requires: Level 30 · Spellcaster 5 or Wizard 5 · Set off 50 element reactions
Identity: Plays the elements against each other: lays one element's status and sets it off with another, making every
reaction bigger.

| Level | Feature |
|---|---|
| 1 | Spell power +5%; Mind +5% |
| 2 | **Catalysis** (passive): reactions you set off are 30% stronger (their extra damage, healing and effects), and your spells are 10% more likely to apply their status (new) |
| 3 | Maximum Mana +10% |
| 4 | After you set off a reaction, your next spell of another element costs 20% less Mana (new) |
| 5 | The statuses your spells apply last 1 turn longer (not Shocked or Frozen, which act once) |
| 6 | **Chain Reaction** (skill, capstone; once per battle, 1 AP): your next spell sets off its reaction on every enemy that carries the status it reacts with, not only on its target (new) |

How it plays: a caster of two or more elements that thinks in pairs: Ice then Fire (Melt), Water then Lightning
(Electrocute), Nature on Cracked enemies (Overgrowth). Its statuses land more often and last longer, its reactions hit
harder, and the switch between elements pays for itself. Once a battle a single spell sets off a reaction across the
whole field. Task 12's statuses and reactions (`../12_Spell_Rework/Element-Statuses.md`) are its toolkit.

## Mimic

5 levels · rarity blue · Requires: Level 25 · Shadow 3 · Defeat 40 different kinds of enemy
Identity: Learns by watching: takes the last trick an enemy used and turns it straight back on them.

| Level | Feature |
|---|---|
| 1 | Speed and Dexterity +5% |
| 2 | **Copycat** (skill, once per battle, 2 AP): use the last skill an enemy used in this battle (not a spell) as if it were yours, with your stats, at 75% power (new) |
| 3 | Evasion +10%; you take 10% less damage from enemies' skills |
| 4 | Copycat's copy has full power |
| 5 | **Learned by Heart** (passive, capstone): a skill you copied stays yours until the battle ends, usable as your own skill would be, and Copycat can be used a second time each battle (new) |

How it plays: it watches the enemy's first moves, then answers with the best of them: a boss's crushing blow, a
knight's charge, a rogue's poison strike. Copying a spell on the spot stays the Mirror Mage's; the Mimic takes skills,
the ones enemies get from their classes (174 enemy files carry class levels; against an enemy without a skill it has
nothing to copy). At its capstone a copied skill stays for the rest of the fight.

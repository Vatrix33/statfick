# Task 13, New Classes: the features

**Under review: the sample of four approved (2026-11-12, NC11); batch 1 written for review.** The level-by-level features of the 79 new
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
- **The classes aren't all built the same way** (the user on the sample, NC11): where the passive or the skill comes,
  which of the two is the capstone, and where the stat levels fall differ from class to class, so that not every class
  opens with a stat bonus and gets its passive or skill at level 2.

**How to read it:** the new classes have no old version, so each has one column, its levels as proposed, then "How it
plays", what the levels add up to. Numbers are proposals to tune when built. "(new)" marks something the old game can't
do and the port would need to add.

**Contents**
- The sample of four (approved 2026-11-12; drawn at random, one from each focus): Bladebound (melee), Harrier
  (ranged), Catalyst (spells), Mimic (the rogue's path).
- Batch 1 (for review): the rest of the task's single-focus classes: Twinblade, Avenger, Crusher (melee); Arbalest,
  Fletcher, Deadeye (ranged); Enchanter, Riftmage, Glyphmage (spells); Cutthroat, Opportunist, Con Artist (the rogue's
  path).

# The sample of four (approved 2026-11-12)

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

# Batch 1: the rest of the task's single-focus classes (for review)

Twelve classes, three from each focus, built in different shapes (NC11): five get their passive at level 1, three
their skill at level 1, the others their passive or skill at level 3; the capstone is a skill for seven and a passive
for five. Turn order: a fighter acts when its turn bar reaches 1,000, so pushing a turn back by 100 costs the enemy a
tenth of a turn.

## Twinblade

6 levels · rarity no colour · Requires: Level 20 · Weaponmaster 3 · Make 30 attacks with a weapon in each hand
Identity: Fights with a weapon in each hand: each hand's strike sets up the other, and the longer it keeps both moving,
the faster the blows come.

| Level | Feature |
|---|---|
| 1 | **Twin Rhythm** (passive): while you hold a one-handed weapon in each hand, each turn in which you attack makes your turn bar fill 4% faster for the rest of the battle, up to 5 times (+20%); a turn without an attack takes one away (new) |
| 2 | In your attacks the off-hand weapon strikes at 50% of its damage instead of 25% (new) |
| 3 | Dexterity and Speed +5% |
| 4 | Weapon Accuracy +10%; at full Twin Rhythm, weapon Crit Rating +5 |
| 5 | Maximum HP +10%; Evasion +5% |
| 6 | **Crossing Cut** (skill, capstone; once per battle, 2 AP): an attack in which each of your weapons strikes twice (the off-hand at its usual share) |

How it plays: an attack with two one-handed weapons already strikes with both, the off-hand at a quarter of its
damage (`attack` in `old-game/scripts/combat.js`); the Twinblade doubles that share and turns it into tempo. Every turn
it keeps attacking, its next turn comes sooner, until after five it acts a fifth more often. It builds on the
Weaponmaster's off-hand bonus and, once a battle, doubles every blade.

## Avenger

5 levels · rarity no colour · Requires: Level 20 · Rearguard 3 · Win 15 battles in which a companion fell
Identity: Fights for those beside it: every blow its allies take, and every ally that falls, makes it hit harder
against the enemy that did it.

| Level | Feature |
|---|---|
| 1 | **Avenge** (skill, once a turn, 2 AP): a weapon attack on the enemy that last damaged an ally, with 25% more damage; 50% more if that enemy has defeated an ally in this battle (new) |
| 2 | Strength and Vitality +5% |
| 3 | When an ally falls, your next turn comes 200 sooner |
| 4 | Weapon Accuracy +10%; Avenge gains 10 Crit Rating |
| 5 | **Blood Debt** (passive, capstone): every enemy that has damaged an ally in this battle takes 5% more from your attacks for each ally it hurt, up to +20% (new) |

How it plays: its main attack from level 1 is the answer to whatever just hurt the party, and the debts add up over a
battle. The Rearguard keeps allies standing; the Avenger makes the enemy pay when they don't.

## Crusher

5 levels · rarity no colour · Requires: Level 15 · Barbarian 3 or Knight 3 · A two-handed melee weapon equipped
Identity: Swings the heaviest weapons there are: each blow staggers the enemy and pushes its next turn further away.

| Level | Feature |
|---|---|
| 1 | Strength +5%; two-handed melee weapons gain 10% Armor Pierce |
| 2 | Maximum HP +10% |
| 3 | **Staggering Blows** (passive): your two-handed melee hits push the target's next turn back by 60, a critical hit by 120 (new) |
| 4 | An enemy you staggered deals 10% less damage until its next turn |
| 5 | **Skullcrusher** (skill, capstone; once per battle, 2 AP): a two-handed melee attack with 40% more damage; the target loses its next turn (a boss's next turn is pushed back by 300 instead) |

How it plays: two heavy blows a turn push one enemy back by about an eighth of a turn and leave it reeling; against a
single strong enemy the Crusher keeps it acting late and hitting soft. The Titan crushes many at once; the Crusher
breaks one enemy's rhythm.

## Arbalest

5 levels · rarity no colour · Requires: Level 15 · Sharpshooter 3 · A crossbow equipped
Identity: The crossbow's patience: slow, heavy bolts that punch through armour, each one loaded and placed with care.

| Level | Feature |
|---|---|
| 1 | **Cranked Shot** (skill, once a turn, 3 AP): a crossbow shot with 50% more damage that ignores 15% of the target's Armor (new) |
| 2 | Crossbow attacks ignore 15% of Armor; crossbow Accuracy +10% |
| 3 | Strength +5%; Maximum HP +10% |
| 4 | Crossbows gain 0.2× Strength scaling; Cranked Shot gains 10 Crit Rating |
| 5 | **Bolt Through** (passive, capstone): a crossbow hit that defeats its target carries on into another random enemy with the damage left over (new) |

How it plays: each turn a cranked bolt and one plain shot (5 AP: about 250% of a shot, against an archer's 200%),
both cutting through armour; strongest against armoured enemies, and at its capstone a bolt that kills keeps going.

## Fletcher

5 levels · rarity no colour · Requires: Level 15 · Hunter 3 · Reforge 3 bows or crossbows
Identity: Makes its own arrows and picks the right one for each enemy: barbed for the tough, blunt for the quick, heavy
for the armoured.

| Level | Feature |
|---|---|
| 1 | **Fletching** (passive): before each battle you fill your quiver with one kind of arrow (bolts for a crossbow), and can change it in battle for 1 AP: **Barbed** (a hit makes the target Bleed for 2 turns: 3% of its maximum HP each turn, you heal for half), **Heavy** (hits ignore 20% of Armor), **Blunt** (a hit pushes the target's next turn back by 80) (new) |
| 2 | Bow and crossbow Accuracy +10%; Dexterity +5% |
| 3 | A fourth kind, **Whistling**: its hits can't miss and leave the target Off-Balance for 1 turn (Evasion and Accuracy −15%) |
| 4 | Changing arrows costs no AP; each kind grows: Barbed bleeds 3 turns, Heavy ignores 30%, Blunt pushes back 120, Whistling's Off-Balance lasts 2 turns |
| 5 | **Mixed Quiver** (skill, capstone; once per battle, 2 AP): a shot with all four kinds' effects at once |

How it plays: an archer that prepares rather than overpowers: the right arrow for each enemy, switched freely from
level 4. Task 12's Bleeding and Off-Balance carry its arrows' effects, so they also feed the party's reactions.

## Deadeye

6 levels · rarity blue · Requires: Level 30 · Sharpshooter 5 · Defeat 25 enemies with a single ranged attack each
Identity: Takes its time: aims at one target for as long as it needs, then fires a shot that rarely needs a second.

| Level | Feature |
|---|---|
| 1 | Ranged weapon Accuracy +10%; critical ranged hits deal 10% more damage |
| 2 | Dexterity +5%; Maximum HP +10% |
| 3 | **Take Aim** (skill, once a turn, 1 AP): aim at an enemy: your next ranged attack on it deals 50% more damage and gains 15 Crit Rating; each of your turns the aim waits unfired adds another 50%, up to +150% (new) |
| 4 | Your aimed shot ignores 25% of the target's Armor and can't miss |
| 5 | Speed +5%; critical ranged hits deal another 10% more |
| 6 | **Dead Eye** (passive, capstone): an aimed shot that defeats its target brings your next turn 250 closer, and its aim bonus passes to the next enemy you aim at (new) |

How it plays: every turn an aimed shot and a plain one (about 250% of a shot), or it holds the aim for a turn or two
and fires one shot of up to 250% with a big chance to crit, for the enemy that must fall now. At its capstone a kill
hands the aim on and brings its turn back early.

## Enchanter

5 levels · rarity no colour · Requires: Level 20 · Wizard 5 · Know spells of 3 elements
Identity: Fights through its allies' weapons: lays elements and power onto the party's gear, so that the fighters
strike with its magic.

| Level | Feature |
|---|---|
| 1 | **Enchant Weapon** (skill, once a turn, 1 AP): enchant an ally's weapon (or your own) with an element whose spells you know, for 3 turns: its hits deal 15% more as extra damage of that element and have a 15% chance to apply the element's status (new) |
| 2 | Intelligence and Wisdom +5% |
| 3 | Enchanted weapons gain 10% Accuracy |
| 4 | Enchanted weapon hits set off reactions as a spell of their element would (task 12) (new) |
| 5 | **Lasting Enchantments** (passive, capstone): each ally starts every battle with its weapon enchanted in an element you choose before the battle, lasting the whole battle (new) |

How it plays: a caster that makes the fighters better: one enchantment a turn for 1 AP leaves room for its own spells.
From level 4 an enchanted warrior's blows set off the reactions its spells prepare, and at the capstone the party
starts every battle armed with its magic.

## Riftmage

6 levels · rarity blue · Requires: Level 35 · Sorcerer 5 · Know 8 Basic spells
Identity: Opens rifts in space: sends enemies away for a few turns and brings them back when it suits it, splitting
the fight into pieces it can win.

| Level | Feature |
|---|---|
| 1 | Basic spells cost 10% less Mana |
| 2 | Intelligence +5%; maximum Mana +10% |
| 3 | **Rift** (skill, once per battle, 2 AP): send an enemy out of the fight for 2 turns: it can't act or be targeted and its turn bar stops; it comes back Exposed (3 stacks: +15% spell damage taken). A boss is sent away for 1 turn (new) |
| 4 | Basic spell power +10% |
| 5 | Rift can be used twice per battle |
| 6 | **Divided Field** (passive, capstone): while an enemy is away in a rift, your spells deal 15% more to the enemies left (new) |

How it plays: the fight against four becomes a fight against three, twice a battle; the party deals with the rest
while one waits outside, then the Riftmage welcomes it back with Basic magic on its Exposed stacks.

## Glyphmage

5 levels · rarity no colour · Requires: Level 15 · Scholar 3 · Know 15 different spells
Identity: Writes its spells onto the battlefield as glyphs that wait: an enemy that acts sets them off.

| Level | Feature |
|---|---|
| 1 | **Glyphs** (passive): you can write a damage spell as a glyph instead of casting it (the same AP and Mana): it waits on the battlefield and goes off on the first enemy that acts, with 20% more power; up to 2 glyphs at a time (new) |
| 2 | Spells you write as glyphs cost 10% less Mana |
| 3 | Intelligence and Mind +5% |
| 4 | A glyph that goes off also pushes that enemy's turn back by 100 |
| 5 | **Glyph Ward** (skill, capstone; once per battle, 2 AP): write a glyph of one damage spell you know under every enemy at once, without its Mana cost, at 60% power; each goes off when that enemy acts (new) |

How it plays: it casts ahead of the enemy instead of after it: a glyph strikes the next enemy to move, stronger and a
little cheaper. The Ritual Caster delays its spells for Mana; the Glyphmage lets the enemy set them off.

## Cutthroat

5 levels · rarity no colour · Requires: Level 15 · Rogue 5 · Make 30 attacks with a dagger
Identity: Fights close with a short blade: many small, deep cuts that keep bleeding long after the blow.

| Level | Feature |
|---|---|
| 1 | Dagger hits gain 5 Crit Rating; Dexterity +5% |
| 2 | Speed and Evasion +5% |
| 3 | **Open Veins** (passive): your dagger hits make the target Bleed for 2 turns (3% of its maximum HP each turn, you heal for half), 3 turns on a critical hit (new) |
| 4 | Your dagger hits on a Bleeding enemy deal 10% more damage |
| 5 | **Bleed Out** (skill, capstone; once per battle, 2 AP): a dagger strike that deals all of the target's remaining Bleeding at once, doubled |

How it plays: quick dagger work that keeps every target bleeding and the Cutthroat on its feet; at the capstone it
cashes the wound in all at once. Death spells' Exsanguinate does the same for casters; the Cutthroat does it with steel.

## Opportunist

5 levels · rarity no colour · Requires: Level 15 · Rogue 5 · Hit 50 enemies that carry a status or are stunned
Identity: Never wastes an opening: whatever already ails an enemy (a status, a stun, a broken guard) is where it
strikes, and hardest.

| Level | Feature |
|---|---|
| 1 | **Every Opening** (passive): your weapon hits deal 8% more damage for each different status on the target, up to +24%, and 15% more on a stunned or Frozen one (new) |
| 2 | Dexterity and Speed +5% |
| 3 | Weapon hits on an enemy that carries a status gain 5 Crit Rating |
| 4 | Maximum HP +10%; Evasion +5% |
| 5 | **Pounce** (skill, capstone; twice per battle, no AP): when an enemy is stunned, Frozen or Rooted, strike it at once with a weapon attack at 130% damage, outside your turn (new) |

How it plays: the party's statuses are its damage: the more the casters lay on an enemy, the harder the Opportunist
hits it, and at its capstone it leaps on a stunned or rooted enemy the moment it's helpless. Basic magic's Resonance
does this for spells; the Opportunist does it with weapons.

## Con Artist

4 levels · rarity no colour · Requires: Level 15 · Thief 3 · Sell at least 100 items
Identity: Talks its way through everything: better deals in town, and in battle bluffs and bribes that make enemies
hesitate or leave the fight.

| Level | Feature |
|---|---|
| 1 | **Silver Tongue** (passive): shops sell to you for 10% less and buy from you for 10% more, and guild quests pay 10% more money (new) |
| 2 | Speed and Luck +5%; your first turn in each battle comes 100 sooner |
| 3 | Enemies are 15% less likely to target you with attacks (they can't tell if you're worth it) |
| 4 | **Bribe** (skill, capstone; once per battle, 2 AP): pay 50 silver × the enemy's level to make a non-boss enemy leave the battle (its experience and drops are lost with it); a boss is talked into losing its next turn instead (new) |

How it plays: the most use outside battle, in every shop and guild payout; in battle a slippery talker, and once a
fight it buys an enemy out of it, trading that enemy's rewards for a shorter, safer battle.

# Task 13, New Classes: the features

**Under review: the sample of four and batches 1–6 approved (2026-11-12, NC11–NC17); batch 7, the last, revised on the
user's notes (NC18) for review.** The level-by-level features of the 79 new
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
- Batch 1 (approved 2026-11-12, Bribe never on bosses): the rest of the task's single-focus classes: Twinblade,
  Avenger, Crusher (melee); Arbalest, Fletcher, Deadeye (ranged); Enchanter, Riftmage, Glyphmage (spells); Cutthroat,
  Opportunist, Con Artist (the rogue's path).
- Batch 2 (approved 2026-11-12): the six pairs: Warcaster, Cinderblade (melee and spells); Wandslinger, Prism Archer (ranged and
  spells); Spellthief, Illusionist (rogue and spells); Corsair, Thug (melee and rogue); Ranger, Skirmisher (melee and
  ranged); Sniper, Poacher (rogue and ranged).
- Batch 3 (approved 2026-11-12, the Winning Streak up to 50 wins): the nine without a focus: Prodigy, Steadfast,
  Equilibrist, Fateweaver, Conqueror, Martyr, Financier, Collector, Chameleon.
- Batch 4 (approved 2026-11-12), the first of the 42 more: the next step for eight new classes (Bladestorm, Transmuter,
  Phantasmist, Voidwalker, Doppelganger, Kingpin, Artificer, Warlord) and the fighters with an element (Frostwarden,
  Stormblade, Thornguard).
- Batch 5 (approved 2026-11-12): the melee extras (Lancer, Scourge, Shieldmaster, Grappler, Drunken Master, Skald,
  Knight-Errant) and the ranged extras (Trickshot, Huntmaster, Bowmaster, Giantslayer, Lightbearer).
- Batch 6 (approved 2026-11-12): the rogue extras (Saboteur, Accomplice, Vigilante) and the spell extras (Soulbinder, Beguiler,
  Astromancer, Golemancer, Hedge Witch, Dirgesinger, Abjurer).
- Batch 7 (revised on your notes, for review), the last: the extras without a focus: Guildmaster, Limitbreaker, Treasure Hunter, Daredevil,
  Surgeon, Ascetic, Lycanthrope, Paragon, Schemer.

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

# Batch 1: the rest of the task's single-focus classes (approved 2026-11-12)

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
| 4 | **Bribe** (skill, capstone; once per battle, 2 AP): pay 50 silver × the enemy's level to make an enemy leave the battle (its experience and drops are lost with it); it doesn't work on bosses (new) |

*Your note, applied: Make it so the Bribe of the Con Artist does not work on bosses at all. (The draft had a boss lose
its next turn instead.)*

How it plays: the most use outside battle, in every shop and guild payout; in battle a slippery talker, and once a
fight it buys an enemy out of it, trading that enemy's rewards for a shorter, safer battle. Bosses can't be bought.

# Batch 2: the six pairs (approved 2026-11-12)

Twelve classes, two for each pair of paths, each built in its own shape (NC11): four get their passive at level 1,
three their skill at level 1, two their skill at level 2 after a first feature, three their passive at level 3; the
capstone is a skill for seven and a passive for five.

## Warcaster

6 levels · rarity no colour · Requires: Level 20 · Warrior 5 · Spellcaster 3
Identity: Steel and spell in turn: each weapon blow strengthens its next spell and each spell its next blow, rewarding
a fighter who switches between them.

| Level | Feature |
|---|---|
| 1 | **Spell and Steel** (passive): a weapon hit makes your next spell (this turn or the next) 20% stronger; a spell makes your next weapon attack 20% stronger. Each bonus is used by the next of the other kind and doesn't stack (new) |
| 2 | Strength and Intelligence +5% |
| 3 | Your weapon hits restore 2% of your maximum Mana |
| 4 | Maximum HP and maximum Mana +10% |
| 5 | Spell and Steel's bonuses rise to 30% |
| 6 | **Battle Weave** (skill, capstone; once per battle, 3 AP): a weapon attack and a spell of up to 3 AP on the same target at once, both with Spell and Steel's bonus |

How it plays: a turn's 5 AP buy one weapon attack and one 3-AP spell, and each makes the other stronger, so the
Warcaster alternates every turn instead of choosing. The Spellblade charges its blows with Mana; the Warcaster trades
between steel and spell, and its blows keep its Mana up.

## Cinderblade

5 levels · rarity no colour · Requires: Level 20 · Spellblade 3 · Know 5 Fire spells
Identity: Sets its blade alight: its strikes leave enemies burning, and the more of them burn, the hotter it fights.

| Level | Feature |
|---|---|
| 1 | Your weapon hits deal 10% extra as Fire damage |
| 2 | **Flame Edge** (skill, once a turn, 1 AP): your next weapon hit this turn sets the target Burning for 3 turns (it takes 20% of the hit as Fire damage at the start of each of its turns) (new) |
| 3 | Strength and Intelligence +5% |
| 4 | Your weapon hits on a Burning enemy deal 10% more damage |
| 5 | **Pyre Heart** (passive, capstone): you fight hotter for every Burning enemy on the field: +5% weapon and Fire spell damage for each, up to +20% (new) |

How it plays: each turn it lights one enemy with a 1-AP Flame Edge and a 2-AP blow, then spreads the fire: its Fire
spells and the Wind's Wildfire reaction set more of them burning, and every one that burns makes it fight harder.

## Wandslinger

5 levels · rarity no colour · Requires: Level 15 · Archer 3 · Mage 5 · A wand equipped
Identity: Duels with a wand as an archer does with a bow: a stream of quick magic bolts, with spells woven in between.

| Level | Feature |
|---|---|
| 1 | **Wand Bolts** (passive): with a wand in hand, your weapon attack becomes a ranged magic bolt costing 1 AP: Basic damage with a shortbow's power at your level, scaling with Intelligence instead of Dexterity, at 45% (new) |
| 2 | Intelligence and Dexterity +5% |
| 3 | After two wand bolts in one turn, your next spell that turn costs 1 AP less (at least 1) (new) |
| 4 | Wand bolts gain 10% Accuracy and 5 Crit Rating |
| 5 | **Spellshot** (skill, capstone; once per battle, 1 AP): fire your next spell through the wand: it costs 1 AP and half its Mana |

How it plays: a turn of five bolts (about 225% of a shot) or two bolts and a spell made cheaper by them; a caster that
never stands idle when its Mana runs low. The Spell Sniper charges a bow with magic; the Wandslinger needs no bow.

## Prism Archer

6 levels · rarity blue · Requires: Level 25 · Spell Sniper 5 · Know spells of 4 elements
Identity: Turns its arrows into spells: each shot carries an element and its status, and sets off reactions from
afar.

| Level | Feature |
|---|---|
| 1 | Bow Accuracy +10%; spell power +5% |
| 2 | Dexterity and Intelligence +5% |
| 3 | **Prismatic Arrows** (passive): before each battle choose an element whose spells you know (change it in battle for 1 AP): your bow hits deal 15% extra damage of that element, have a 20% chance to apply its status, and set off reactions as a spell of it would (new) |
| 4 | Prismatic Arrows' status chance rises to 35% |
| 5 | Maximum Mana +10%; Speed +5% |
| 6 | **Spectrum Shot** (skill, capstone; once per battle, 2 AP): one shot at every enemy, each in the element it resists least of those you know, applying that element's status for certain (new) |

How it plays: an archer that sets up and sets off the casters' reactions from range: Water arrows, then a Lightning
one; Ice arrows, then a Fire one. Once a battle it marks the whole field with statuses for the party to react on.

## Spellthief

5 levels · rarity blue · Requires: Level 20 · Thief 3 · Scholar 3
Identity: Steals magic: lifts the enemy's blessings and Mana for itself, and for a moment even their spells.

| Level | Feature |
|---|---|
| 1 | **Lift** (skill, once a turn, 2 AP): a weapon attack that takes the target's strongest buff for yourself, with the turns it has left, and 10% of its current Mana (new) |
| 2 | Intelligence and Dexterity +5% |
| 3 | All your weapon hits drain 3% of the target's maximum Mana into yours |
| 4 | Speed and Evasion +5% |
| 5 | **Stolen Spell** (passive, capstone): Lift also takes the target's last spell: it can't cast it for 2 turns, and you can cast it once in this battle at 75% power for its Mana (new) |

How it plays: its main attack robs: the enemy's shields and blessings become its own, its Mana feeds the Spellthief's
spells, and at the capstone the enemy's best spell is taken away for two turns. The Mirror Mage copies a spell; the
Spellthief takes it.

## Illusionist

6 levels · rarity no colour · Requires: Level 20 · Trickster 3 · Mage 5
Identity: Fights with what isn't there: false copies of the party draw the enemy's blows, and the enemy is never sure
what it is hitting.

| Level | Feature |
|---|---|
| 1 | **Mirror Images** (skill, once per battle, 2 AP): give an ally (or yourself) 2 illusory copies for 3 turns: each takes one attack or single-target spell aimed at that ally and vanishes (new) |
| 2 | Intelligence and Mind +5% |
| 3 | Enemies have 10% less Accuracy against an ally who has an image |
| 4 | Mirror Images can be used twice per battle |
| 5 | Maximum Mana +10%; Evasion +5% |
| 6 | **Veil of Doubt** (passive, capstone): at the start of every battle each ally has one image (new) |

How it plays: it guards the party by misdirection rather than by blocking or healing: the blows land on nothing. The
Guardian takes the hits; the Illusionist makes sure there is nothing real to hit.

## Corsair

5 levels · rarity no colour · Requires: Level 20 · Fencer 3 · Warrior 5 · Travel by boat 5 times
Identity: The sea's swordsman: a boarder's boldness, plunder from every fight, and at its best by the water.

| Level | Feature |
|---|---|
| 1 | **Plunder** (passive): you find 15% more money after battles; in battles by water (the coast, rivers and lakes, as the Leywalker reads the land) your weapon hits deal 10% more damage (new) |
| 2 | Dexterity and Strength +5% |
| 3 | One-handed weapon Accuracy +10%; Evasion +10% by water |
| 4 | Maximum HP +10%; boat crossings cost you nothing |
| 5 | **Cutlass Sweep** (skill, capstone; once per battle, 2 AP): a sweeping attack on every enemy at 60% damage; each enemy it hits that falls in this battle leaves 10% more money |

How it plays: a duelling swordsman who pays its way: more money from every fight, free boats, and a sweep that hits
the whole crew. On the coast and the rivers it is at its best; inland it is still a good blade.

## Thug

4 levels · rarity no colour · Requires: Level 10 · Rogue 5 · Brawler 3
Identity: Wins by brute surprise: blows from behind that knock enemies senseless, and a pocket picked while they're
down.

| Level | Feature |
|---|---|
| 1 | **Sap** (skill, once per battle, 2 AP): a blow with a club, a mace or your fists: 30% more damage and a 50% chance to stun the target for its next turn (not a boss) (new) |
| 2 | Strength and Dexterity +5% |
| 3 | Sap stuns for certain (not a boss) and can be used twice per battle |
| 4 | **Shakedown** (passive, capstone): your hits on a stunned enemy deal 20% more damage, and each one takes some of its money: the battle pays 5% more for each, up to 20% (new) |

How it plays: an early class (Level 10) for a rogue who brawls: knock the dangerous one out, rob it while it's down.
The Brawler fights dirty; the Thug turns a stun into a payday.

## Ranger

6 levels · rarity no colour · Requires: Level 15 · Hunter 3 · Warrior 5
Identity: As good with a blade as with a bow: switches between them without losing a step, each change of weapon
landing a stronger blow.

| Level | Feature |
|---|---|
| 1 | You carry a bow and a melee weapon at once (a second weapon set), and switching between them costs no AP (new) |
| 2 | Strength and Dexterity +5% |
| 3 | **Shifting Stance** (passive): once a turn, the first attack after a switch deals 25% more damage and can't miss (new) |
| 4 | Maximum HP +10%; Speed +5% |
| 5 | Shifting Stance's bonus rises to 40% |
| 6 | **Bow and Blade** (skill, capstone; once per battle, 3 AP): a bow shot and a melee strike on one enemy, both with Shifting Stance's bonus |

How it plays: shot, switch, strike: every attack after a switch hits harder and surely, so the Ranger alternates
weapons from turn to turn (about 225% of an attack a turn at level 3, 240% at level 5) and never has to choose between
range and the front line.

## Skirmisher

5 levels · rarity no colour · Requires: Level 15 · Gladiator 3 · Archer 5
Identity: Never where the enemy expects: strikes, shoots and slips away, harder to pin down after every attack and
quick to punish whoever closes in.

| Level | Feature |
|---|---|
| 1 | After you dodge an attack, your next attack deals 20% more damage |
| 2 | **Fall Back** (skill, once per battle, 1 AP): until your next turn, melee attacks on you have half their chance to hit, and you strike back at each attacker at 60% damage (new) |
| 3 | Dexterity and Strength +5% |
| 4 | Maximum HP +10%; Speed +5% |
| 5 | **Hit and Run** (passive, capstone): each attack you make gives you 8% Evasion until your next turn, up to 3 times (+24%) (new) |

How it plays: it fights with whatever weapon is at hand and never stays still: its dodges feed its attacks and its
attacks feed its dodges. Once a battle it falls back and makes the enemies that rush it pay.

## Sniper

6 levels · rarity blue · Requires: Level 25 · Shadow 3 · Sharpshooter 3
Identity: Shoots from hiding: unseen until it fires, its shots from cover are deadly, and it slips back out of sight
between them.

| Level | Feature |
|---|---|
| 1 | Ranged weapon Accuracy +10%; ranged Crit Rating +5 |
| 2 | Dexterity and Evasion +5% |
| 3 | **From Cover** (passive): at the start of each of your turns you are hidden, unless an enemy hit you since your last turn: while hidden, single-target attacks can't pick you; your first shot from hiding deals 50% more damage and gains 15 Crit Rating, and reveals you (new) |
| 4 | A shot from hiding ignores 20% of the target's Armor |
| 5 | Maximum HP +10%; Speed +5% |
| 6 | **Vanish** (skill, capstone; once per battle, no AP): after a shot, you are hidden again at once |

How it plays: every turn it isn't hit, it opens from hiding with a heavy shot and follows with a plain one (about 250%
of a shot); the better the party keeps enemies off it, the more often it shoots from cover. Once a battle it fires two
shots from hiding in one turn.

## Poacher

4 levels · rarity no colour · Requires: Level 15 · Thief 3 · Hunter 3
Identity: Hunts with snares and arrows where it shouldn't: catches beasts unawares and takes more from every one it
brings down.

| Level | Feature |
|---|---|
| 1 | **Poacher's Haul** (passive): Beast-class enemies and animals the party defeats drop 25% more money, and their item drops are 25% more likely (new) |
| 2 | +15% damage against Beast-class enemies and animals |
| 3 | Dexterity and Luck +5% |
| 4 | **Snare** (skill, capstone; once per battle, 1 AP): snare a Beast-class enemy or an animal (not a boss): it is Rooted for 3 turns and loses its next turn (new) |

How it plays: a hunter's trade turned to profit: the beasts of the wild pay better when the Poacher is along, and its
snare holds the most dangerous of them for the party. The Beast Tamer keeps animals; the Poacher sells them.

# Batch 3: the nine without a focus (approved 2026-11-12)

Nine classes that belong to no path, built in their own shapes (NC11): two get their passive at level 1, two their
skill at level 1, two their skill at level 2 after a first feature, two their passive at level 3, one its skill at
level 3; the capstone is a skill for four and a passive for five. Some do most of their work outside battle, as the
road and trade classes of task 10 do.

## Prodigy

4 levels · rarity no colour · Requires: Level 5 · Defeat an enemy at least 10 levels above you
Identity: A fast learner: gains experience quicker than anyone, and picks up a new class's ways in a fraction of the
time.

| Level | Feature |
|---|---|
| 1 | You gain 15% more experience, 30% more from enemies above your level |
| 2 | **Beginner's Luck** (skill, once per battle, no AP): your next attack or spell gains 25% power for every 5 levels its target is above you, up to +75% |
| 3 | Your lowest stat +10% |
| 4 | **Gifted** (passive, capstone): the first level of the next 2 classes you take costs no perk point (new) |

How it plays: an early class (Level 5) that speeds up the climb, best for a character that likes to fight above its
level. A perk point comes with each level up to 100 and a class level costs one; Gifted gives two of Prodigy's four
back, so its levels are cheap as well as quick.

## Steadfast

5 levels · rarity no colour · Requires: Level 20 · Win 20 battles while carrying a status an enemy put on you
Identity: Shrugs off what the enemy puts on it: statuses and weakenings fade fast, and often never take hold.

| Level | Feature |
|---|---|
| 1 | Statuses and debuffs on you last 1 turn less (at least 1 turn) |
| 2 | Vitality and Mind +5% |
| 3 | **Unshaken** (passive): each status, debuff or stun an enemy puts on you has a 35% chance not to take hold (new) |
| 4 | Maximum HP +10%; Armor +5% |
| 5 | **Shake It Off** (skill, capstone; once per battle, 1 AP): remove every status and debuff from yourself; for 2 turns nothing new takes hold on you |

How it plays: once the port's enemies cast task 12's statuses on heroes (Burning, Chilled, Hexed and the rest), the
Steadfast is the one they don't stick to; a sound class for any character that keeps getting frozen, stunned or cursed.

## Equilibrist

5 levels · rarity no colour · Requires: Level 30 · No stat more than a quarter above your lowest
Identity: Strength in balance: the more evenly its stats are spread, the better it does everything, with no single
weakness to exploit.

| Level | Feature |
|---|---|
| 1 | **Balance** (passive): while no stat of yours is more than a quarter above your lowest, all your stats are 8% higher; up to a third above, 4% (new) |
| 2 | Your two lowest stats +5% |
| 3 | Maximum HP and maximum Mana +10% |
| 4 | Each of your elemental resistances counts as at least 15 (new) |
| 5 | **Centered** (skill, capstone; once per battle, 1 AP): for 3 turns your Balance bonus doubles and you can't be critically hit |

How it plays: the reward for a build that spreads its points: a little better at everything, with no element that
hurts it much more than another. It suits a character that fights with weapons and spells alike.

## Fateweaver

5 levels · rarity blue · Requires: Level 25 · Bard 5 · At least 100 Luck
Identity: Doesn't trust to luck, it spends it: chooses when fortune turns, sparing an ally a deadly blow or making a
hit land.

| Level | Feature |
|---|---|
| 1 | **Twist Fate** (skill, no AP, also outside your turn; once per battle for every 50 Luck you have, up to 4 times): turn an ally's miss into a hit, or an enemy's critical hit on an ally into a normal hit (new) |
| 2 | Luck +10% |
| 3 | Twist Fate can also turn an ally's hit into a critical hit |
| 4 | Speed and Mind +5% |
| 5 | **Fate's Favourite** (passive, capstone): the first time in each battle that an ally would fall, they keep 1 HP instead (new) |

How it plays: the luck classes of task 10 (Bard, Jester, Joker, Gambit) let chance decide; the Fateweaver decides when
chance goes the party's way, a few times a battle, more with more Luck. At its capstone it keeps an ally standing once
a battle.

## Conqueror

5 levels · rarity no colour · Requires: Level 20 · Win 50 battles in a row without a defeat
Identity: Lives for the next victory: every battle won in a row makes it stronger, until it loses.

| Level | Feature |
|---|---|
| 1 | While you have won 5 battles or more in a row, battles give you 10% more experience |
| 2 | Maximum HP +10%; Speed +5% |
| 3 | **Winning Streak** (passive): each battle won in a row gives you +1% damage and +1% Armor, up to +50% at 50 wins; a defeat or a battle fled from starts it again (new) |
| 4 | With 10 wins or more in a row, your first turn in each battle comes 100 sooner |
| 5 | **Claim Victory** (skill, capstone; once per battle, 2 AP): a weapon attack with double damage on an enemy below 30% HP; if it defeats it, your streak counts one more win |

*Your note, applied: The Conquerors Winning Streak is too weak. Increase it to a cap of 50 wins. (The draft capped it
at +10% at 10 wins.)*

How it plays: a careful player's class: never lose, never run, and it stays at its best. Its requirement is 50 wins in
a row, so a Conqueror whose streak still holds starts at the full +50%; after a loss, fifty wins rebuild it. Its
finisher feeds the streak it lives on.

## Martyr

4 levels · rarity no colour · Requires: Level 15 · Fall in 10 battles your party went on to win
Identity: Its fall is never in vain: when it goes down its allies rise stronger, and the harder it fought, the more
they gain.

| Level | Feature |
|---|---|
| 1 | **Last Gift** (passive): when you fall, each ally heals 15% of their maximum HP and deals 15% more damage for 3 turns; both rise by 5% for each enemy you defeated in the battle, up to +15% more (new) |
| 2 | Vitality and Mind +5% |
| 3 | Maximum HP +10%; Last Gift also removes one debuff from each ally |
| 4 | **Final Stand** (skill, capstone; once per battle, 2 AP): for 2 turns every attack aimed at an ally strikes you instead, and you take 30% less damage; if you fall during it, Last Gift is doubled |

How it plays: a front-liner whose fall turns a battle rather than losing it: the party heals and hits harder the
moment it drops, and the more it did before, the bigger the gift. Revived afterwards, it can give again.

## Financier

4 levels · rarity no colour · Requires: Level 20 · Hold 50,000 silver at once
Identity: Makes money work while it adventures: its savings grow day by day, and its stakes in the towns pay back in
goods and favours.

| Level | Feature |
|---|---|
| 1 | **Stake** (skill, in a town, once a day): invest up to 20,000 silver in the town's trade, in one town at a time: after 7 days it comes back with 15% more, and while it is invested the town's shops sell to the party for 10% less (new) |
| 2 | Luck and Mind +5% |
| 3 | Your stakes come back after 5 days, and you can hold stakes in up to 3 towns at once |
| 4 | **Interest** (passive, capstone): each day the silver you carry grows by 0.5%, up to 500 silver a day (new) |

How it plays: a class for the road and the town, like the Caravanist and the Salesman: it barely fights better, but
its money grows while the party travels, and the towns it invests in treat the party well. The Salesman earns more
from selling; the Financier earns from what it already has.

## Collector

5 levels · rarity blue · Requires: Level 25 · Own 10 different masterworks over time
Identity: Keeps one of everything: grows stronger with each different masterwork and artifact it has owned, whether
it carries them or not.

| Level | Feature |
|---|---|
| 1 | The masterworks you wear are 10% stronger (their power and their effects) (new) |
| 2 | Mind and Luck +5% |
| 3 | **Showcase** (skill, once per battle, 1 AP): call on a masterwork you own but aren't wearing: for 3 turns you have its effects as if you wore it (new) |
| 4 | Maximum HP and maximum Mana +10% |
| 5 | **Collection** (passive, capstone): +1% to all your stats for every 2 different masterworks you have ever owned, up to +10% (at 20) (new) |

How it plays: every masterwork found is worth something to it, even one it never wears: its whole collection lifts its
stats, and once a battle it borrows the effect of a piece left in the pack. The Relic Warden makes the most of what it
wears; the Collector of everything it has ever had.

## Chameleon

5 levels · rarity no colour · Requires: Level 20 · Be hit by spells of 10 different elements
Identity: Adapts as it fights: hardens against whatever hits it, an element or a kind of attack, the longer a battle
goes.

| Level | Feature |
|---|---|
| 1 | Armor +5%; you take 5% less damage of the element that last hit you |
| 2 | **Mimetic Hide** (skill, once per battle, 1 AP): take on an enemy's resistances for 3 turns: each of your elemental resistances becomes the higher of yours and its, up to 50 (new) |
| 3 | Vitality and Mind +5% |
| 4 | Maximum HP +10% |
| 5 | **Adaptation** (passive, capstone): each hit of an element raises your resistance to it by 5 for the rest of the battle, up to 25; each weapon hit raises your Armor by 3%, up to +15% (new) |

How it plays: the longer the fight, the less the same attack hurts: against an enemy that keeps casting one element,
it soon shrugs it off. The Elemental Siphon turns the elements back on the enemy; the Chameleon simply stops minding
them.

# Batch 4: the next steps and the fighters with an element (approved 2026-11-12)

The first of the 42 more: the next step for eight of the new classes, each stronger than the class it grows from, as
its rarity says (blue or purple), and the three fighters with an element besides the Cinderblade. Shapes (NC11): three
get their passive at level 1, three their skill at level 1, two their skill at level 2 after a first feature, one its
skill at level 3, two their passive at level 3; the capstone is a skill for five and a passive for six.

## Bladestorm

4 levels · rarity blue · Requires: Level 40 · Twinblade 6
Identity: The Twinblade's mastery: both blades in a whirl of steel that reaches every enemy around it.

| Level | Feature |
|---|---|
| 1 | **Whirling Blades** (passive): with a one-handed weapon in each hand, your attacks also strike every other enemy at 20% damage (new) |
| 2 | Strength and Dexterity +5%; Evasion +5% |
| 3 | In your attacks the off-hand weapon strikes at 75% of its damage (the Twinblade's 50% raised) |
| 4 | **Bladestorm** (skill, capstone; once per battle, 3 AP): spin through the enemy: both your weapons strike every enemy at full damage |

How it plays: the Twinblade fought one enemy at a time; the Bladestorm cuts into all of them with every attack, while
Twin Rhythm still speeds it up. Against a crowd it is one of the strongest melee classes; against a lone boss it is a
Twinblade with a sharper off-hand.

## Transmuter

5 levels · rarity purple · Requires: Level 50 · Catalyst 6 · Know spells of 8 elements
Identity: The Catalyst perfected: turns one element into another mid-fight, changing statuses into others and
reactions into chains.

| Level | Feature |
|---|---|
| 1 | Your spells deal 10% more damage when their element differs from your last spell's |
| 2 | Intelligence and Wisdom +5%; maximum Mana +10% |
| 3 | **Transmute** (skill, once a turn, 1 AP): turn one status on an enemy into another element's status (an element whose spells you know), with the turns it has left (new) |
| 4 | A reaction you set off leaves the target carrying your spell's own status, at the spell's usual chance (new) |
| 5 | **Chain of Elements** (passive, capstone): when the status a reaction left (level 4) can react with another status the target still carries, that reaction follows at once at half strength, up to 2 more in a chain (new) |

How it plays: the Catalyst needed the right statuses on the field; the Transmuter makes them: a Burning enemy becomes
a Chilled one for the Fire spell to Melt, a Soaked one for the Lightning. At its capstone one spell sets off two or
three reactions in a row.

## Phantasmist

4 levels · rarity blue · Requires: Level 40 · Illusionist 6
Identity: Illusions that hurt: its phantoms strike like the real thing, and the enemy's own fear does the rest.

| Level | Feature |
|---|---|
| 1 | An enemy that strikes one of your images is Hexed (1 stack: it deals 5% less damage per stack, up to 3) |
| 2 | **Phantasm** (skill, once a turn, 2 AP): send a phantom at an enemy: it strikes as your strongest Basic damage spell would, at 70% power and without Mana, and Hexes it (1 stack) (new) |
| 3 | Intelligence and Mind +5%; maximum Mana +10% |
| 4 | **Dread** (passive, capstone): an enemy with 3 Hexed stacks loses its next turn (once per enemy in a battle), and Hexed enemies take 10% more damage from the party (new) |

How it plays: the Illusionist only defended; the Phantasmist attacks with its illusions, a free spell each turn that
leaves the enemy weaker and more afraid, until it freezes in terror. Task 12's Hexed is its fear.

## Voidwalker

5 levels · rarity purple · Requires: Level 50 · Riftmage 6
Identity: Walks between places: steps through the void itself, and casts whole groups of enemies out of the fight.

| Level | Feature |
|---|---|
| 1 | Basic spell power +10%; Rift sends enemies away 1 turn longer |
| 2 | **Void Gate** (skill, once per battle, 3 AP): send up to 3 enemies (not bosses) out of the fight for 2 turns; each comes back Exposed (3 stacks) (new) |
| 3 | Intelligence and Mind +5% |
| 4 | Maximum HP and maximum Mana +10%; enemies coming back from a rift or the void act last (their turn bar starts at 0) |
| 5 | **Voidstep** (passive, capstone): each single-target attack or spell aimed at you has a 25% chance to miss as you step through the void (new) |

How it plays: in a large fight it empties most of the field for two turns, so the party fights one or two enemies at a
time; it slips away from the blows aimed at it as easily as it sends enemies away.

## Doppelganger

4 levels · rarity purple · Requires: Level 45 · Mimic 5 · Defeat 80 different kinds of enemy
Identity: Becomes its enemy: takes an enemy's shape, strength and tricks for a time, and fights it with its own power.

| Level | Feature |
|---|---|
| 1 | **Take Shape** (skill, once per battle, 2 AP): become a copy of an enemy that isn't a boss for 3 turns: you take its stats where they are higher than yours, its resistances and its skills (usable as your own), and keep your HP, gear and own skills (new) |
| 2 | Speed and Dexterity +5%; maximum HP +10% |
| 3 | Take Shape lasts 4 turns, and works on a boss, giving half of how far its stats are above yours |
| 4 | **Second Skin** (passive, capstone): in another's shape you deal 15% more damage and take 15% less from enemies of the kind you copied; when the shape ends, you keep its resistances for the rest of the battle (new) |

How it plays: the Mimic copied one trick; the Doppelganger copies the whole enemy and adds its own power on top. It
picks the most dangerous enemy on the field and becomes it; from level 3 even a boss.

## Kingpin

4 levels · rarity blue · Requires: Level 40 · Con Artist 4 · Informant 2
Identity: Runs the underworld: thieves and informants work for it in every town, and hired muscle fights at its side.

| Level | Feature |
|---|---|
| 1 | **Network** (passive): thieves and informants work for you in every town: each day you are paid 20 silver for each of your levels, and the hooded man's information costs you half the Reputation (new) |
| 2 | Luck and Speed +5%; maximum HP +10% |
| 3 | The Con Artist's Bribe costs you half and can be used twice per battle (still never on bosses) |
| 4 | **Hired Muscle** (skill, capstone; once per battle, 2 AP): call two hired thugs who fight for the party until the battle ends, each with half your stats and a club (new) |

How it plays: the Con Artist's road to the top: a daily income and cheaper secrets in town, two bribes a battle, and at
its capstone a crew of its own in every fight.

## Artificer

5 levels · rarity blue · Requires: Level 35 · Enchanter 5 · Smith 3
Identity: Makes magic that lasts: enchants artifacts and gear between battles, waking powers in them that no shop
sells.

| Level | Feature |
|---|---|
| 1 | **Imbue** (skill, in a town or at camp, once a day): give one weapon, armour or artifact a lasting enchantment in an element whose spells you know: a weapon's hits deal 10% extra damage of that element, armour gives 15 resistance to it, an artifact's effects become 10% stronger; one enchantment per item, replaced if imbued again (new) |
| 2 | Intelligence and Mind +5% |
| 3 | Your imbued enchantments are half again as strong (15% extra damage, 22 resistance, 15% stronger effects) |
| 4 | Reforging an imbued item keeps its enchantment, and reforging costs you 20% less |
| 5 | **Awakened Craft** (passive, capstone): your imbued items wake a second power: a weapon applies its element's status on 15% of its hits, armour makes that element's status on its wearer last 1 turn less, an artifact's effects are another 10% stronger (new) |

How it plays: the Enchanter's magic lasted a battle; the Artificer's stays. A day at a time it outfits the whole party,
companions too, and its work survives the forge. The Mastersmith makes better forged gear; the Artificer adds magic
to any gear.

## Warlord

4 levels · rarity purple · Requires: Level 50 · Conqueror 5 · Strategist 5
Identity: Leads the party from victory to victory: its winning streak carries every ally, and enemies falter before
it.

| Level | Feature |
|---|---|
| 1 | Enemies deal 1% less damage for every 5 wins of your Winning Streak, up to 10% less |
| 2 | Strength and Vitality +5%; maximum HP +10% |
| 3 | **Banner of Victory** (passive): your allies gain a quarter of your Winning Streak's bonus (up to +12% damage and Armor) (new) |
| 4 | **Conquest** (skill, capstone; once per battle, 2 AP): for 3 turns every ally gains your full Winning Streak bonus |

How it plays: the Conqueror's streak, up to +50% since the batch 3 note, now lifts the whole party: a quarter of it
always, all of it for three turns once a battle, while the enemies weaken before a commander that hasn't lost in
fifty fights.

## Frostwarden

6 levels · rarity no colour · Requires: Level 25 · Knight 5 · Know 5 Ice spells
Identity: A defender wrapped in frost: whatever strikes it grows cold and slow, until it freezes in place.

| Level | Feature |
|---|---|
| 1 | **Rime** (passive): an enemy that hits you with a melee attack has a 50% chance to be Chilled (Speed −10% a stack; a third stack Freezes it for its next turn) (new) |
| 2 | Vitality +5%; Armor +5% |
| 3 | Ice spells cost 10% less Mana and gain 5% power |
| 4 | Maximum HP +10% |
| 5 | Your weapon hits on a Chilled or Frozen enemy deal 15% more damage |
| 6 | **Glacial Wall** (skill, capstone; once per battle, 2 AP): for 2 turns you take 30% less damage, and every enemy that attacks an ally is Chilled |

How it plays: a knight that punishes being hit: the enemies that close in on it slow down and freeze, and its blows and
the party's Fire spells (Melt) finish them. Task 12's Chilled and Frozen are its armour.

## Stormblade

5 levels · rarity no colour · Requires: Level 20 · Spellblade 3 · Know 5 Lightning spells
Identity: Lightning in the blade: fast strikes that jolt enemies out of their turns.

| Level | Feature |
|---|---|
| 1 | Your weapon hits deal 10% extra as Lightning damage |
| 2 | Dexterity and Intelligence +5% |
| 3 | **Static Charge** (passive): each weapon hit gives you a charge; at 3 charges your next hit uses them to Shock its target (it loses 1 AP at its next turn) and arc to another enemy for 50% as Lightning damage (new) |
| 4 | Speed +5%; your turn bar fills 5% faster |
| 5 | **Lightning Rush** (skill, capstone; once per battle, 2 AP): three quick strikes on random enemies at 60% damage each, each Shocking its target |

How it plays: fast and relentless: every third hit jolts an enemy out of an AP and jumps to another, and once a battle
it Shocks three at once. With Water spells on the field, its Lightning sets off Electrocute.

## Thornguard

5 levels · rarity no colour · Requires: Level 20 · Guardian 3 · Druid 3
Identity: Grows thorns and roots: enemies that hit it tear themselves on its thorns, and those it strikes are held
where they stand.

| Level | Feature |
|---|---|
| 1 | **Entangling Strike** (skill, once a turn, 2 AP): a weapon attack with a 50% chance to Root the target for 2 turns (Evasion halved, can't flee); certain on a Cracked target, as Nature's Overgrowth (new) |
| 2 | Vitality and Wisdom +5% |
| 3 | Nature spells cost 10% less Mana; Armor +5% |
| 4 | Maximum HP +10%; Rooted enemies deal 10% less damage to you |
| 5 | **Thorns** (passive, capstone): enemies that hit you with a melee attack take 20% of the damage back as Nature damage, 30% if they are Rooted (new) |

How it plays: a guardian that holds enemies in place and makes them pay for every blow; Earth's Cracked from the
party's spells makes its roots certain. The Frostwarden slows its attackers; the Thornguard hurts them.

# Batch 5: the melee and ranged extras (approved 2026-11-12)

Seven melee and five ranged classes of the 42 more: weapons and fighting styles no class was built around (spear and
lance, flail, shield, grappling, drinking), songs and the road, and the ranged path's trick shots, hunts, bows, giants
and light. Shapes (NC11): four get their passive at level 1, two their skill at level 1, two their skill at level 2
after a first feature, one its skill at level 3, one its skill at level 4, two their passive at level 3; the capstone
is a skill for six and a passive for six. Shields add Armor only (the game has no block chance), so the Shieldmaster's
blocks are the attacks it takes while Guarding.

## Lancer

5 levels · rarity no colour · Requires: Level 15 · Knight 3 · A spear or a lance equipped
Identity: Fights at the length of a spear: charges each new enemy with a heavy first blow and keeps the rest at the
point of its weapon.

| Level | Feature |
|---|---|
| 1 | **First Blow** (passive): your first spear or lance hit on each enemy in a battle deals 40% more damage (new) |
| 2 | Strength and Vitality +5% |
| 3 | Spears and lances gain 10% Armor Pierce and 10% Accuracy |
| 4 | An enemy that attacks you in melee is struck first by your weapon at 40% damage (once per enemy turn) (new) |
| 5 | **Lance Charge** (skill, capstone; once per battle, 2 AP): charge an enemy: a hit with double damage that pushes its next turn back by 200 |

How it plays: it opens on every enemy with a heavy blow, so it is at its best against many, and the enemies that come
for it meet the point first. The Gladiator pins one enemy down; the Lancer greets each new one.

## Scourge

5 levels · rarity no colour · Requires: Level 15 · Warrior 5 · A flail equipped
Identity: Fights with a flail: blows that wrap around shields and guards, hard to block and harder to predict.

| Level | Feature |
|---|---|
| 1 | Your flail hits ignore what a Guard would stop and the Armor a shield gives (new) |
| 2 | Strength and Luck +5% |
| 3 | **Wild Swing** (skill, once per battle, 2 AP): a flail attack that strikes 2 to 4 times at random, at 50% damage each |
| 4 | Flail Crit Rating +5; your critical flail hits deal 15% more damage |
| 5 | **Unpredictable** (passive, capstone): enemies' Evasion counts only half against your flail attacks (new) |

How it plays: the answer to enemies that hide behind shields, guard or dodge: its blows get around all three. The
flail scales on Luck, so a lucky Scourge crits often.

## Shieldmaster

5 levels · rarity no colour · Requires: Level 20 · Heavy Knight 3
Identity: The shield is its weapon too: every blow it blocks feeds a bash, and it fights as well with the shield as
with the blade.

| Level | Feature |
|---|---|
| 1 | **Shield Bash** (skill, once a turn, 2 AP): strike with your shield, as a one-handed mace of your level scaling with Vitality; it pushes the target's next turn back by 50 (new) |
| 2 | Your shield's Armor +15% |
| 3 | Vitality and Strength +5% |
| 4 | Maximum HP +10%; each attack you take while Guarding makes your next Shield Bash 20% stronger, up to 3 times |
| 5 | **Counterbash** (passive, capstone): when an enemy attacks you while you Guard, you bash it back at 50% (once per enemy turn) (new) |

How it plays: it guards and bashes in turn; the more it's struck while guarding, the harder its next bash, and at its
capstone every attack on its guard gets an answer. The Bulwark only holds the line; the Shieldmaster hits back.

## Grappler

5 levels · rarity no colour · Requires: Level 15 · Brawler 4
Identity: Fights at the closest range: grabs one enemy and holds it, so that it can't act while the party works on it.

| Level | Feature |
|---|---|
| 1 | Your unarmed hits deal 10% more damage; Strength +5% |
| 2 | **Grapple** (skill, once per battle, 2 AP): grab an enemy that isn't a boss: it can't act on its next turn and can't dodge until then; while you hold it, your attacks go to it (new) |
| 3 | Vitality +5%; maximum HP +10% |
| 4 | Grapple can be used twice per battle; on a boss it pushes its next turn back by 200 instead |
| 5 | **Chokehold** (passive, capstone): your unarmed hits on an enemy you hold deal 25% more damage and make it lose 1 AP at its next turn (new) |

How it plays: it takes the most dangerous enemy out of the fight for a turn, twice a battle, and wears it down while
it's held. The Monk stuns with a precise blow; the Grappler simply doesn't let go.

## Drunken Master

6 levels · rarity no colour · Requires: Level 20 · Brawler 3 · Drink 30 potions in battle
Identity: Fights as if drunk, and often is: swaying and stumbling, impossible to predict, and harder to hit the more it
has had.

| Level | Feature |
|---|---|
| 1 | **Liquid Courage** (passive): each potion you drink in battle gives you 5% Evasion and 5% unarmed damage for the battle, up to 4 times (new) |
| 2 | Drinking a potion in battle costs you 1 AP less |
| 3 | Dexterity and Vitality +5% |
| 4 | With at least one Liquid Courage, your unarmed attacks gain 10% Accuracy and 5 Crit Rating |
| 5 | Maximum HP +10%; potions heal you 20% more |
| 6 | **Drunken Flurry** (skill, capstone; once per battle, 2 AP): four staggering unarmed strikes on random enemies at 60% damage each, each with a 25% chance to leave its target Off-Balance |

How it plays: it drinks to fight: the healing and buff potions it would use anyway make it harder to hit and harder
hitting, and cost it less time. Task 9's potions are its stock.

## Skald

5 levels · rarity no colour · Requires: Level 15 · Warrior 5 · Bard 3
Identity: The warrior-poet: battle songs that lift the party, roaring louder with every blow it lands.

| Level | Feature |
|---|---|
| 1 | **War Chant** (passive): each of your weapon hits makes the party's weapon hits 2% stronger for the rest of the battle, up to +10% (new) |
| 2 | Strength and Mind +5% |
| 3 | Each of your weapon hits also heals the ally with the lowest HP for 2% of their maximum HP |
| 4 | Maximum HP +10%; Vitality +5% |
| 5 | **Saga** (skill, capstone; once per battle, 1 AP): for 3 turns War Chant's bonus is doubled and the party's turn bars fill 10% faster |

How it plays: the Bard lifts the party with music and luck from the back; the Skald sings from the front line, and its
song grows with every blow it lands. Five hits into a battle the whole party fights a tenth harder.

## Knight-Errant

5 levels · rarity no colour · Requires: Level 20 · Knight 3 · Complete 5 guild Escort quests
Identity: The wandering knight who takes up causes: fights hardest for those in its care, on the road and on the
field.

| Level | Feature |
|---|---|
| 1 | Escort quests pay you 15% more, and in their ambushes the party takes 15% less damage |
| 2 | Strength and Vitality +5% |
| 3 | **Sworn Protector** (passive): at the start of each battle the ally with the lowest maximum HP becomes your ward: it takes 10% less damage, and you deal 20% more damage to every enemy that has hurt it (new) |
| 4 | Maximum HP +10%; when your ward is hit, your next turn comes 50 sooner |
| 5 | **Oath of the Road** (skill, capstone; once per battle, 1 AP): for 3 turns your ward can't fall below 1 HP |

How it plays: it picks the party's most fragile member and fights for it: whoever hurts the ward becomes its target.
The Guardian takes the blows for an ally; the Knight-Errant makes the enemy regret them. On the guild's Escort quests
it is at home.

## Trickshot

5 levels · rarity no colour · Requires: Level 20 · Hotshot 3 · At least 80 Luck
Identity: Shots no one else would try: arrows that ricochet to a second enemy, curve around cover and land where
fortune puts them.

| Level | Feature |
|---|---|
| 1 | **Ricochet** (passive): each ranged hit has a chance of a fifth of your Luck in percent (up to 40%) to ricochet to another enemy at 50% damage (new) |
| 2 | Dexterity and Luck +5% |
| 3 | Ranged weapon Accuracy +10%; ricochets can be critical hits |
| 4 | A ricochet can bounce once more, to a third enemy at 25% damage |
| 5 | **Impossible Shot** (skill, capstone; once per battle, 2 AP): a shot that ricochets through every enemy: full damage on the first, 60% on each other, each able to crit |

How it plays: the Hotshot's quick shots, with luck on top: the more Luck, the more of them bounce on. At 80 Luck one
shot in six ricochets; at 200 Luck two in five.

## Huntmaster

5 levels · rarity blue · Requires: Level 40 · Hunter 7 · Complete 5 Great Hunts
Identity: Leads the Great Hunts: the bigger the quarry, the better it hunts, and the whole party hunts with it.

| Level | Feature |
|---|---|
| 1 | **Call the Hunt** (skill, once per battle, 1 AP): name an enemy the quarry: for the battle every ally deals 10% more damage to it and has 15% more Accuracy against it, and your Hunter's Mark on it counts double (new) |
| 2 | Dexterity and Speed +5% |
| 3 | Great Hunts pay you 25% more money and Reputation; against a Great Hunt's quarry or a boss your weapon hits deal 15% more |
| 4 | Maximum HP +10%; bow damage +10% |
| 5 | **Pack Hunt** (passive, capstone): when an ally hits the quarry, your next attack on it deals 15% more, and when you hit it, the next ally attack on it does too (new) |

How it plays: the Hunter's mark, made the whole party's: it names the quarry and everyone hunts it, each hit setting
up the next. Built for the guild's Great Hunts and the bosses.

## Bowmaster

6 levels · rarity no colour · Requires: Level 25 · Archer 5 · Attack with 4 different kinds of bow
Identity: Master of every bow: gets the most out of whatever it draws, from the shortbow to the ballista bow.

| Level | Feature |
|---|---|
| 1 | All bows and crossbows gain 2 base power |
| 2 | Ranged weapon Accuracy +10% |
| 3 | **Masterful Draw** (passive): bows and crossbows gain 0.2× Dexterity and 0.2× Strength scaling (new) |
| 4 | Each kind of bow gains a mastery: shortbow Speed +5%, longbow Accuracy +10%, warbow Armor Pierce +10%, ballista bow damage +10%, crossbow Crit Rating +5, compound bow Evasion +5% (new) |
| 5 | Ranged weapon power +10%; Dexterity +5% |
| 6 | **Arsenal** (skill, capstone; once per battle, no AP): switch to another bow from your pack and keep the previous one's mastery as well for 3 turns |

How it plays: the archer's Weaponmaster: whatever bow it holds, it draws more from it, and each kind adds its own
strength. Once a battle it swaps bows mid-fight and has the best of both.

## Giantslayer

6 levels · rarity no colour · Requires: Level 1 · Hunter 7 · Defeat 30 giants
Identity: The hunter of giants: the bigger the enemy, the more surely it finds the weak spot.

| Level | Feature |
|---|---|
| 1 | Your weapon hits deal 10% more damage to enemies with more maximum HP than you |
| 2 | Dexterity and Vitality +5% |
| 3 | Maximum HP +10% |
| 4 | **Find the Weak Spot** (skill, once per battle, 1 AP): study an enemy: for the battle your attacks on it ignore 25% of its Armor and gain 10 Crit Rating, twice that on a giant (new) |
| 5 | You take 15% less damage from enemies with more maximum HP than you |
| 6 | **The Bigger They Are** (passive, capstone): your weapon hits deal 1% more damage for every 5% more maximum HP the target has than you, up to +40% (new) |

How it plays: like the Drakeslayer and the Demon Hunter, the Hunter's path against one kind of foe, here the big ones:
giants first, and anything with far more HP than it, bosses most of all.

## Lightbearer

5 levels · rarity no colour · Requires: Level 25 · Archer 5 · Priest 3
Identity: Shoots light: its arrows mend the allies they pass and sear the undead and the dark.

| Level | Feature |
|---|---|
| 1 | Your bow hits on the undead, demons, devils and impids deal 15% extra as Holy damage |
| 2 | **Arrow of Light** (skill, once a turn, 2 AP): a bow shot that heals the ally with the lowest HP for 30% of the damage it deals (new) |
| 3 | Wisdom and Dexterity +5% |
| 4 | Holy spells cost 10% less Mana; the ally Arrow of Light heals is Blessed for 2 turns |
| 5 | **Dawn's Reach** (passive, capstone): Arrow of Light also removes Hexed and Withered from the ally it heals, and your bow hits set off Purify on Hexed or Withered enemies as a Holy spell would (new) |

How it plays: a healer that never stops shooting: each turn one shot mends an ally while it hurts an enemy, and against
the undead and the demons of the Abyss its arrows burn. Task 12's Blessed and Purify carry its light.

# Batch 6: the rogue and spell extras (approved 2026-11-12)

Three rogue and seven spell classes of the 42 more. Shapes (NC11): three get their passive at level 1, three their
skill at level 1, one its skill at level 2 after a first feature, one its skill at level 3, two their passive at level
3; the capstone is a skill for five and a passive for five.

## Saboteur

5 levels · rarity no colour · Requires: Level 20 · Trap Layer 3
Identity: Ruins the enemy's fight before it starts: blunted weapons, cut straps and spoiled supplies leave them weaker
all battle.

| Level | Feature |
|---|---|
| 1 | **Sabotage** (passive): at the start of each battle each enemy has a 40% chance to start sabotaged for the battle: a blunted weapon (it deals 10% less damage) or cut straps (its Armor −15%) (new) |
| 2 | Dexterity and Speed +5% |
| 3 | Your traps are 20% stronger, and setting one costs you 1 AP less |
| 4 | Sabotage's chance rises to 60%, and a sabotaged enemy's potions and healing items heal half (spoiled supplies) |
| 5 | **Spoil the Plan** (skill, capstone; once per battle, 1 AP): sabotage every enemy at once, with both a blunted weapon and cut straps |

How it plays: its work is done before the first blow: a good share of the enemies start the fight weaker, and its
traps finish what the sabotage began. The Sunderer breaks armour blow by blow; the Saboteur has it broken before the
battle.

## Accomplice

4 levels · rarity no colour · Requires: Level 15 · Rogue 5 · 2 companions
Identity: The partner in crime: sets up its allies' attacks, leaving every enemy it touches open to their critical
hits.

| Level | Feature |
|---|---|
| 1 | **Set Up** (skill, once a turn, 2 AP): a weapon attack that leaves its target open: the next ally attack on it gains 20 Crit Rating (new) |
| 2 | Dexterity and Speed +5% |
| 3 | Set Up also gives that ally attack 10% more damage |
| 4 | **Partner in Crime** (passive, capstone): enemies you have hit in this battle take 10% more critical damage from your allies, and when an ally crits an enemy you hit, your next turn comes 50 sooner (new) |

How it plays: a rogue for a full party: its own blows are the opening, the allies' critical hits the finish. With two
companions who hit hard, it is worth more than another striker.

## Vigilante

5 levels · rarity no colour · Requires: Level 20 · Stalker 2 · Defeat 40 outlaws
Identity: Hunts the outlaws of the land: deadly against bandits and raiders, and rewarded by every town it rids of
them.

| Level | Feature |
|---|---|
| 1 | Your weapon hits deal 15% more damage to outlaws (the bandit and raider enemies) |
| 2 | Strength and Dexterity +5% |
| 3 | **Bring to Justice** (skill, once per battle, 2 AP): an attack with double damage on an outlaw or on any enemy below 30% HP; an outlaw it defeats pays a bounty of 30 silver × its level (new) |
| 4 | Maximum HP +10%; you take 15% less damage from outlaws |
| 5 | **Town's Champion** (passive, capstone): for every 10 outlaws you defeat, the next town you enter thanks you: 500 silver and 10 guild Reputation (new) |

How it plays: the Stalker's road turned to the bandits: it is at its best on the roads they haunt and in the guild's
Escort ambushes, and the towns pay it for every gang it breaks.

## Soulbinder

5 levels · rarity blue · Requires: Level 30 · Wizard 5 · Priest 3
Identity: Binds the party's lives together: harm to one is shared by all, and healing on one spreads to the rest.

| Level | Feature |
|---|---|
| 1 | Healing spells cost 10% less Mana |
| 2 | Wisdom and Mind +5% |
| 3 | **Soul Link** (passive): while you stand, 20% of the damage any ally takes is shared out among the other allies (you too), and 20% of any healing an ally receives also heals every other ally (new) |
| 4 | Maximum HP +10%; damage shared through the link is 20% lower |
| 5 | **Bound Fate** (skill, capstone; once per battle, 2 AP): for 3 turns the link shares 40% of the damage and spreads 40% of the healing |

How it plays: no ally is hit alone and none is healed alone: a single heal on the tank reaches the whole party, and a
heavy blow on the mage is spread over everyone. It makes single-target healing count for all.

## Beguiler

5 levels · rarity blue · Requires: Level 30 · Dreamweaver 3
Identity: Charms its enemies: turns a foe to its side for a few turns, or makes it falter before it strikes.

| Level | Feature |
|---|---|
| 1 | **Enthralling Presence** (passive): each enemy's first attack in a battle has a 25% chance to falter: it hesitates and the attack is lost (new) |
| 2 | Intelligence and Mind +5% |
| 3 | The falter chance rises to 35%, and an enemy that falters is Hexed (1 stack) |
| 4 | Maximum Mana +10%; spells cost you 5% less Mana |
| 5 | **Charm** (skill, capstone; once per battle, 3 AP): charm an enemy that isn't a boss: it fights for the party for 3 turns, then is Hexed (2 stacks) (new) |

How it plays: the Dreamweaver puts enemies to sleep; the Beguiler makes them hesitate and, once a battle, fight their
own side. The Beast Tamer keeps an animal for good; the Beguiler borrows any enemy for three turns.

## Astromancer

5 levels · rarity no colour · Requires: Level 20 · Wizard 5 · Win 20 battles at night
Identity: Reads the stars: each battle they grant it a different boon, and at night their power is fullest.

| Level | Feature |
|---|---|
| 1 | Spells cost 10% less Mana at night (18:00–06:00) |
| 2 | Intelligence and Wisdom +5% |
| 3 | **Starlit Boon** (passive): at the start of each battle the stars grant the party one boon at random for the battle: the Hunter (Accuracy +10%), the Scholar (spell power +10%), the Dancer (Evasion +10%) or the Healer (each ally heals 3% of their maximum HP at the start of their turns); at night it is twice as strong (new) |
| 4 | Maximum Mana +10%; you can see which boon the next battle will bring |
| 5 | **Starfall** (skill, capstone; once per battle, 3 AP): stars fall on every enemy: Basic damage as your strongest damage spell would deal it, at 80% power, 120% at night |

How it plays: every battle starts with a gift for the whole party, a different one each time, doubled after dark. The
Sunstrider and the Nightwalker are strongest by day or by night; the Astromancer reads the sky and shares it.

## Golemancer

6 levels · rarity blue · Requires: Level 30 · Geomancer 3
Identity: Builds a golem of stone and iron that fights beside it, grows with it, and can be rebuilt when it falls.

| Level | Feature |
|---|---|
| 1 | **Stone Servant** (passive): every battle begins with your golem at your side: a summon of your level with 60% of your maximum HP, whose blows scale with your Intelligence (new) |
| 2 | Enemies are 20% more likely to attack your golem than you |
| 3 | Intelligence and Vitality +5% |
| 4 | Your golem has 80% of your maximum HP, and your Armor |
| 5 | Earth spells cost 10% less Mana; your golem's blows make their target Cracked a quarter of the time |
| 6 | **Rebuild** (skill, capstone; once per battle, 2 AP): raise your fallen golem with full HP, or, if it stands, make it 30% stronger for the battle |

How it plays: a caster with a bodyguard that is always there: the golem takes the blows and cracks the enemy's armour
for the Earth spells, and it comes back once a battle when it falls. The Pactkeeper's familiar grows over many battles;
the golem grows with its maker's level.

## Hedge Witch

5 levels · rarity no colour · Requires: Level 10 · Mage 5 · Forage 20 ingredients
Identity: Magic from the hedgerow: gathers what the land offers and folds it into its spells.

| Level | Feature |
|---|---|
| 1 | Foraging finds something 15% more often |
| 2 | **Fold In** (skill, once a turn, no AP): spend one foraged ingredient to give your next spell this turn 20% more power, 30% with a rare ingredient or better (new) |
| 3 | Wisdom and Intelligence +5% |
| 4 | A spell you Fold In costs 15% less Mana |
| 5 | **Green Wisdom** (passive, capstone): one Fold In in three uses no ingredient, and each spell you Fold In restores 3% of your maximum Mana (new) |

How it plays: an early caster (Level 10) that turns task 9's foraging into spell power: every ingredient on the road
is a stronger spell later. The Alchemist brews ingredients into potions; the Hedge Witch casts them.

## Dirgesinger

5 levels · rarity no colour · Requires: Level 20 · Mage 5 · Bard 3
Identity: Sings the enemy down: mournful songs that slow and weaken every foe that hears them.

| Level | Feature |
|---|---|
| 1 | **Lament** (skill, once per battle, 2 AP): for 3 turns every enemy is Hexed (1 stack) and fills its turn bar 10% slower (new) |
| 2 | Mind and Wisdom +5% |
| 3 | Lament can be sung twice per battle |
| 4 | Maximum Mana +10%; Dark spells cost 10% less Mana |
| 5 | **Endless Dirge** (passive, capstone): while you stand, every enemy fills its turn bar 5% slower and deals 5% less damage (new) |

How it plays: the Bard's opposite: it lifts nobody, it brings every enemy down a little and keeps them there. Its
Hexed stacks feed Dark's Devour and the Phantasmist's Dread.

## Abjurer

5 levels · rarity no colour · Requires: Level 25 · Spellcaster 5 · Know spells of 5 elements
Identity: Wards the party against the enemy's element: the better it knows an element, the better it guards against
it.

| Level | Feature |
|---|---|
| 1 | **Ward** (skill, once a turn, 1 AP): ward an ally (or yourself) against an element whose spells you know, for 3 turns: they take 20% less damage of that element, and its status lasts at most 1 turn on them (new) |
| 2 | Wisdom and Mind +5% |
| 3 | A ward is stronger the more of its element's spells you know: 2% more for each, up to +20% (40% in all) |
| 4 | Maximum HP and maximum Mana +10% |
| 5 | **Great Ward** (passive, capstone): at the start of each battle the whole party is warded against the element the enemies use most (new) |

How it plays: the defensive caster of many elements: it reads the enemy and wards the party against exactly what it
throws. The Templar shields against the undead and the dark; the Abjurer against any element it knows.

# Batch 7: the extras without a focus (for review)

The last nine of the 42 more, among them the second red class (the Limitbreaker, NC6), a gold one (the Paragon) and a
class from a rare item (the Lycanthrope, which like the other item classes may have more than one skill). Shapes
(NC11): two get their passive at level 1, three their skill at level 1, two their skill at level 2 after a first
feature, one its skill at level 3, one its passive at level 3; the capstone is a skill for four and a passive for five.

## Guildmaster

4 levels · rarity blue · Requires: Level 40 · Bounty Broker 3 · Complete 50 guild quests
Identity: The guild's own: commands its quests and recruits, and the companions it brings in join stronger.

| Level | Feature |
|---|---|
| 1 | **Call the Guild** (skill, once per battle, 2 AP): call a guild adventurer of your level, of a random class the guild trains, to fight for the party until the battle ends (new) |
| 2 | Mind and Luck +5%; maximum HP +10% |
| 3 | Recruiting at the tavern costs you 25% less, and your companions gain 10% more experience |
| 4 | **Guild's Own** (passive, capstone): the guild's daily boards offer you one more quest, guild quests pay you 15% more Reputation, and the recruits at the tavern are 2 levels higher (new) |

How it plays: the Bounty Broker profits from guild work; the Guildmaster runs it: better boards, cheaper and stronger
recruits, and in battle a guild adventurer at its call. Task 2's guild is its home.

## Limitbreaker

3 levels · rarity red · Requires: Level 110 (only in Limitless, task 7; perk points saved up before Level 100)
Identity: Born of Limitless: turns its scaling around, growing stronger the stronger Limitless makes its enemies.

| Level | Feature |
|---|---|
| 1 | **Closing the Gap** (passive): while Limitless is on, you gain what it gives every enemy: all your stats are 25% higher, so you meet them as equals (new) |
| 2 | All your stats +1% for every 5 levels above 100 (+2% at Level 110, up to +20% at 200); maximum HP and Mana +15% |
| 3 | **Breaking Point** (skill, capstone; once per battle, no AP): for 3 turns there is no limit: your attacks and spells ignore Armor and resistances, and you can't fall below 1 HP (new) |

*Your note, applied: Remake the Limitbreaker Passive to not debuff the enemies. Instead it should help you close the
gap. (Asked after: the 25% that Limitless adds to every enemy, as +25% to all your stats. The batch had taken the 25%
off the enemies as well.)*

How it plays: the class for those who go on past Level 100: Limitless's own margin matched, so the enemies it lifts
to your level and 25% beyond meet you as equals, and a body that keeps growing to Level 200. Above gold, like the
Unreality; it takes three perk points saved before Level 100, since none come after (task 7). The hooded man won't
speak of it below Level 100 (NC7).

## Treasure Hunter

4 levels · rarity blue · Requires: Level 20 · Defeat 3 Sparkling enemies
Identity: Follows the glitter: finds Sparkling enemies more often and gets more out of them.

| Level | Feature |
|---|---|
| 1 | Sparkling enemies give the party 25% more experience and money |
| 2 | Luck and Speed +5% |
| 3 | **Glint** (passive): enemies are three times as likely to be Sparkling in your battles (0.1% becomes 0.3%, a Cull quest's target 2% becomes 6%) (new) |
| 4 | **Strike the Glitter** (skill, capstone; once per battle, 2 AP): an attack on a Sparkling enemy that can't miss and deals double damage; if it defeats it, the enemy rolls once more for a masterwork |

How it plays: task 5's Sparkling enemies are rare luck for everyone else; for the Treasure Hunter they are a regular
find, worth more each time, and the masterworks they carry come more often.

## Daredevil

5 levels · rarity no colour · Requires: Level 25 · Win 20 battles against an enemy above your level
Identity: Asks for harder fights: makes battles more dangerous on purpose, and is paid in power and loot for every
risk it takes.

| Level | Feature |
|---|---|
| 1 | **Raise the Stakes** (skill, once per battle, no AP, at the start of a battle): the enemies gain 20% to their stats, and the battle gives the party 25% more experience and money and 25% better drop chances (new) |
| 2 | Speed and Luck +5% |
| 3 | While the stakes are raised, you deal 10% more damage and gain 5 Crit Rating |
| 4 | Maximum HP +10%; you can raise the stakes higher: the enemies gain 40% for 60% more rewards |
| 5 | **Thrill of Danger** (passive, capstone): the lower your HP, the harder you hit: 1% more damage for every 4% of your HP missing, up to +20%, and 5% more against an enemy above your level (new) |

How it plays: the player decides how hard each battle is, and the Daredevil is paid for it: easy fights made worth
fighting, and the dangerous ones worth the danger. The Champion seeks the strongest enemy; the Daredevil makes every
enemy stronger.

## Surgeon

4 levels · rarity blue · Requires: Level 35 · Field Medic 5 · Use 100 healing items in battle
Identity: The Field Medic's mastery: brings the fallen back in battle without magic, and mends wounds no potion could.

| Level | Feature |
|---|---|
| 1 | Healing items you use on others heal 20% more |
| 2 | **Field Surgery** (skill, once per battle, 2 AP): bring a fallen ally back with 30% of their HP, without magic (new) |
| 3 | Wisdom and Vitality +5%; maximum HP +10% |
| 4 | **Steady Hands** (passive, capstone): each healing item you use also removes one debuff or status, and the ally you heal takes 15% less damage until your next turn (new) |

How it plays: a healer for any party, needing no Mana and no spells: its bandages and potions do more, clean wounds as
they heal them, and once a battle bring someone back from the brink.

## Ascetic

5 levels · rarity no colour · Requires: Level 20 · Monk 4 or Hermit 3 · No artifacts equipped
Identity: Owns almost nothing: the less it carries and wears, the stronger it is.

| Level | Feature |
|---|---|
| 1 | Unarmed and quarterstaff attacks gain 10% Accuracy; without armour, Evasion +5% |
| 2 | Wisdom and Vitality +5% |
| 3 | **Clear Mind** (skill, once per battle, 1 AP): for 3 turns you take 25% less damage and your attacks can't miss |
| 4 | Maximum HP +10%; without armour you take 10% less damage |
| 5 | **Nothing to Lose** (passive, capstone): all your stats are 14% higher for each of your off-hand, armour and two artifact slots that is empty, up to +56% (new) |

*Your note, applied: For the Ascetic, buff Nothing to lose to increase stats by 14% for up to 56% since taking away
Artifacts is a huge debuff. (The batch had 4% a slot, up to +16%.)*

How it plays: the opposite of the Collector and the Relic Warden: it gives up its gear for power. Without its two
artifacts, as its requirement asks, it is 28% stronger in everything; fully bare, 56%. It suits the Monk's fists and
the Hermit's road.

## Lycanthrope

5 levels · rarity blue · Requires: Level 1 · Moonfang (consumed on acquisition)
Identity: Carries the beast's curse: turns into a wolf in battle, savage and quick, and the night draws it out.

| Level | Feature |
|---|---|
| 1 | **Wolf Form** (skill, once per battle, 1 AP): turn into a wolf for 4 turns: your attacks become claws and fangs (as a two-handed weapon of your level scaling with Strength and Speed), you gain 20% Speed and Evasion, and you can't cast spells or use items (new) |
| 2 | **Night's Call** (passive): at night (18:00–06:00) you deal 10% more damage and gain 10% Speed, and your Wolf Form lasts the whole battle (new) |
| 3 | Strength and Speed +5%; maximum HP +10% |
| 4 | In Wolf Form your hits make the target Bleed for 2 turns |
| 5 | **Howl** (skill, capstone; once per battle, 2 AP): every ally gains 15% damage and Speed for 3 turns, and every enemy below your level is Hexed (1 stack) |

How it plays: a class from a rare item, like the Vampire and the Dragonkin (two skills, as they may): a fighter that
becomes a beast for part of each battle and all of it at night. The Moonfang is a new key item, proposed as a 1% drop
from a wolf defeated at night (the seven wolf enemies). The Half-Beast's Wildheart is born to its beast; the
Lycanthrope is cursed with one.

## Paragon

3 levels · rarity gold · Requires: Level 85 · Jack of all Trades 4 · Knight 5 · Sharpshooter 5 · Spellcaster 5 ·
Assassin 5
Identity: The Jack of all Trades grown into a legend: every path mastered, and it excels at all of them.

| Level | Feature |
|---|---|
| 1 | Your melee attacks, ranged attacks and spells all deal 15% more damage, and your Crit Rating is +10 |
| 2 | **Peerless** (skill, once per battle, no AP): for your next 2 turns you have 2 more AP, and your attacks and spells can't miss |
| 3 | **Paragon of Paths** (passive, capstone): all your stats are 3% higher for each class you have at its final level, without a limit (new) |

*Your note, applied: The Paragon of Paths should not have a limit and should increase by 3% instead. (The batch had
1% a class, up to +20%.)*

How it plays: a gold legend like the Hero and the Demon Lord, but for one's own sake: strong with blade, bow and spell
alike, and stronger for every class it has mastered, with no ceiling: a character with ten classes at their final
level is 30% stronger in everything, with twenty 60%. Once a battle it acts as two for two turns.

## Schemer

5 levels · rarity no colour · Requires: Level 25 · Strategist 3
Identity: Plans every move: what it doesn't use this turn it keeps for the next, building up to one decisive turn.

| Level | Feature |
|---|---|
| 1 | **Plan Ahead** (passive): AP you don't use in a turn is kept for your next turn, up to 2 (new) |
| 2 | Mind and Speed +5% |
| 3 | You can keep up to 3 AP |
| 4 | Maximum HP +10%; in a turn in which you keep AP, you take 10% less damage until your next turn |
| 5 | **Grand Scheme** (skill, capstone; once per battle, no AP): this turn you gain as many AP again as you kept (up to +3) |

How it plays: it never wastes an AP: a turn with an odd point left over pays into the next, and once a battle a turn of
eleven AP does the work of two normal turns. The Sentinel spends its kept AP on one strike; the Schemer on a
whole turn.

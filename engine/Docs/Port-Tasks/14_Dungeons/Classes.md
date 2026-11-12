# The two new classes (task 14)

**Status: approved by the user (2026-11-12, DG78: "Approved") and added to the end of task 10's two drafts
(`../10_Class_Rework/`), which the port reads; this file stays as the task's record.** Task 14's two classes, the Dungeon Delver and the Throwing
Specialist, drafted in one go: identity, levels, requirements and rarity, then the features level by level (DG76).
Once approved they go to the end of task 10's two drafts (`../10_Class_Rework/`), as task 13's did, and this file stays
as the task's record (DG77). The rest of task 14 is built by the port from its decisions (`../../Engine-Plan.md`,
"Decisions for task 14, Dungeons", DG1 onward).

**The rules they follow** (task 10's, DG3; `../10_Class_Rework/Class-Rework-Features.md`, at its top): one passive and
one skill; the last level gives one of them (the capstone rule); every level worth taking; power by rarity; no
penalties that miss more than they help; requirements a turn's 5 AP allows (C28); identities a short playstyle
without skills or stats (C15); each class in its own shape (NC11). Numbers are proposals to tune when built; "(new)"
marks what the old game can't do.

**What they build on** (task 14's answers): a dungeon's stages and its pools of groups (DG7), Camping Supplies at
most once after each cleared stage, 20% of maximum HP and Mana to everyone (DG11); "dungeon enemies" are every enemy
fought in a dungeon, the bosses included (DG67). The Throwing Specialist's items are the eleven new ones and task 9's
twelve thrown poisons (DG4), kept apart from the Poisoner's features; a throw costs 1 AP (DG44); the knives roll to
hit and deal damage from the thrower's own stats (DG42, DG43); bombs always land, their stun chances halved on bosses
(DG42, DG47), each enemy stunned at most once per battle by each kind of bomb (DG48); hit chances and the escape bonus
are points (DG46, DG50); the same item's effect refreshes rather than adds up (DG49). The Emergency Rope and the
Waylight are used outside battle and aren't thrown, so the Specialist's bonuses don't touch them; the four extra
throwing items are drafted at port time (DG39), so its features name them by their kinds, to be checked against them
then.

## Dungeon Delver

5 levels (the task) · rarity blue

- **Requires:** Level 15 · Finish 2 different dungeons (a new count; the Goblin Cave, 4–8, and the River Portal,
  10–14, are the first two)
- **Identity:** Made for the long way down: lasts through stage after stage, makes the most of every rest, and is at
  its strongest by the time the boss is reached.

| Level | Feature |
|---|---|
| 1 | While you are in the party, Camping Supplies used in a dungeon restore 30% of maximum HP and Mana instead of 20% (new) |
| 2 | **Scout Ahead** (skill; on the dungeon's overview, between stages): see the next stage's enemies before it begins; once per dungeon run, draw its group again from the stage's pool (new) |
| 3 | In a dungeon, you take 10% less damage, and your spells and skills cost 10% less Mana (new) |
| 4 | In a dungeon, after each cleared stage, you and every companion recover 5% of maximum HP and Mana (new) |
| 5 | **Deep Delve** (passive, capstone): in a dungeon, each stage cleared on this run gives you 2% more damage and 2% less damage taken, up to 8 stages (+16%, −16%); it ends with the run (new) |

How it plays: a specialist for the expeditions. Before each stage it looks ahead and can turn away the worst draw
once; between stages its party heals a little by itself and more from each rest; and the deeper the dungeon, the
harder it hits and the less it takes, so it meets the boss at its peak. Outside dungeons it has nothing but its stats,
so it doesn't replace the road's classes (Traveler, Scavenger, Camp Cook) or the fighters.

How it meets the task: survival over many stages (levels 3 and 5), HP and Mana kept up (1 and 4), Camping Supplies
used better (1), dungeon-specific advantages and more against dungeon enemies (2, 3, 5), bosses met strongest (5),
long expeditions worth more (4 and 5 grow with the stages). It doesn't trivialise them: one supply per stage still
(DG11), and its bonuses are its own except the 5% recovery and the better supplies, which help the party.

Found while drafting: the Quartermaster's Emergency Rations (every ally 10% HP and Mana once per battle) and the
camping classes' chances to keep Camping Supplies already exist; the Delver neither restores in battle nor keeps
supplies, so they add to it rather than overlap.

## Throwing Specialist

6 levels · rarity no colour

- **Requires:** Level 12 · Throw 30 throwing items in battle (a new count; task 9's thrown poisons count, DG4)
- **Identity:** Fights from its bandolier: knives, bombs and poisons land harder and linger longer in its hands, and
  cost it less to throw.

| Level | Feature |
|---|---|
| 1 | Your thrown knives gain 15% Accuracy and deal 20% more damage |
| 2 | Your bombs' stun chances +15 points (a Flash Bomb 55%), and the lasting effects of your knives, bombs and the four extra throws (the mark, the Dust Bomb's, the escape bonus) last 1 turn longer; poisons excepted (new) |
| 3 | **Quick Hands** (passive): your first throw each turn costs no AP (new) |
| 4 | Against bosses, your throws count at three-quarters strength instead of half: your thrown poisons, and your bombs' stun chances (a Sparkle Bomb 75%) (new) |
| 5 | Your throwing items have a 20% chance not to be used up |
| 6 | **Barrage** (skill, capstone; once per battle, 1 AP): throw up to three different throwing items in this one action, each at its own target (new) |

How it plays: a thrower for any party: a knife that rarely misses to open, a Dust Bomb a turn longer when the enemy
line is dangerous, a stun that lands more often, and once a battle three different throws at once (a mark, a bomb and
a poison together). Its bonuses are by kind (knives hit, bombs stun and linger, poisons reach bosses), so it pays to
carry a mix and to pick the right one, not to throw any item at all.

Kept apart from the Poisoner (DG4): the Poisoner makes poisons last longer and deal more, coats its blade with them,
ignores Nature resistance and throws one at every enemy; the Specialist leaves poison durations and damage alone
(level 2 excepts them) and only lifts the bosses' half strength to three-quarters (level 4) and keeps some poisons
(level 5, with the Poisoner's own 25% where both are taken). Its Barrage throws different items at chosen targets, not
one poison at all of them.

Checks against the task's rules: the stun limits stay (each enemy at most one Sparkle Bomb stun and one Flash Bomb stun
per battle, DG48; Barrage can't throw two of the same item); the same item's effect still refreshes rather than adds
up (DG49); stuns still last 1 turn (the game's stun rule; level 2 lengthens the other effects only); bosses keep their
reduced share (three-quarters, not full). Quick Hands' free throw and Barrage's 1 AP stay within a turn's 5 AP (C28).

## What the port adds for them

- Counts: different dungeons finished (Dungeon Delver); throwing items thrown in battle (Throwing Specialist).
- With them the drafts hold 256 classes, 252 for players (NC2); rarity of the two: 1 blue, 1 no colour.

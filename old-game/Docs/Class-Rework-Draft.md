# Task 10, Class Rework: the draft, every class's identity, levels, requirements and rarity

**For the user's review (written 2026-11-10).** The one draft of task 10 (C14): every class, the 130 in the game and the 40 new ones, with its identity, its number of levels, its requirements and its rarity (colour). Nothing is built yet. The decisions behind it are C1–C14 in `engine/Docs/Engine-Plan.md`, "Decisions for task 10, Class Rework"; the task is `engine/Docs/Port-Tasks/10_Class_Rework.txt`.

**How to read it**
- **Identity:** a short explanation of what the class does: what it is for and how it plays. For a current class it is drawn from all of its features (its passives and skills included) and kept, only put into words; where I propose to change one it says **Sharpened** and why (almost always two classes doing the same thing). For a new class it is my proposal, the features to be made from it.
- **Levels:** kept unless an arrow shows the change.
- **Requirements:** every current class keeps its own (the task: change them only when absolutely necessary; none is). The new classes' are proposed, in the same kinds the game already uses where it can; a few need a new count the game would keep (days away from towns, caravan trips, runes fitted and the like).
- **Rarity** (no colour, blue, purple, gold): kept unless an arrow shows the change, with the reason. A rarer class gets a stronger power level when its features are made.
- **Found:** what I noticed while mapping a current class (overlaps, the capstone rule, power), for the features round; nothing there is decided. **The capstone rule** (the task): a class's last level must give its passive or its skill; ten classes break it today, and they are marked.
- **New classes:** the task's names, the seven kept from `old-game/classes/newc/`, and eight of my own.
- **How to answer:** approve as a whole, or name the classes to change and how (in chat, or in this file).

**Totals:** 130 current classes (1 with a change of levels: Scavenger 7 → 5; 8 identities sharpened: Champion, Bulwark, Cleric, Paladin, Oathsworn, Templar, Assassin, Archivist; 9 rarities raised: Avatar → blue, Cardinal Priest → blue, Great Mage → blue, Chaos Cultist → blue, Joker → blue, Dragonkin → blue, Vampire → blue, Madman → purple, Sharmat → purple), 40 new classes, 170 in all. Rarity across all of them: 116 without a colour, 38 blue, 13 purple, 3 gold.

## Contents

- Current: The four starting classes (4)
- Current: Melee fighters (8)
- Current: Knights and defenders (6)
- Current: Holy warriors (5)
- Current: Unarmed fighters (4)
- Current: The archer's path (6)
- Current: The rogue's path (10)
- Current: Weapons and spells together (5)
- Current: The mage's path (14)
- Current: The twelve elemental mages (12)
- Current: Masters of elements (4)
- Current: Priests and healers (4)
- Current: Summoners and dark pacts (9)
- Current: Leaders and minds (5)
- Current: Luck (4)
- Current: The road, trade and the guild (12)
- Current: Race classes (8)
- Current: Classes from rare items (6)
- Current: Legends and the rest (4)
- New: The task's list (25)
- New: The seven from classes/newc/ (7)
- New: Eight of my own (8)

# Current classes

## The four starting classes (4)

The first step on each path; every other class grows from one of these.

### Warrior
5 levels · rarity no colour
- **Requires:** Level 1
- **Identity:** The first melee class and the way into most fighter classes. It raises Strength and melee Accuracy and makes heavy armour hinder Evasion less, so plate can be worn early. Against two or more enemies its Armor rises a little, and once a day it can lower its guard to hit harder and dodge better for three turns.
- Found: Both features are faint (5% Armor against two or more enemies; a once-a-day trade of Armor for Strength and Evasion).

### Archer
5 levels · rarity no colour
- **Requires:** Level 1
- **Identity:** The first ranged class and the way into the bow classes. It raises Dexterity, bow power and ranged Accuracy, and aims best when there are few enemies to watch. Once a day it can line up a shot that is far more likely to hit.
- Found: Single Target is small and shrinks with every enemy present; Sharpshoot only once a day.

### Mage
5 levels · rarity no colour
- **Requires:** Level 1
- **Identity:** The first spell class and the way into nearly every caster. It teaches two Tier 1 spells to choose from a random five, makes low-tier spells cheaper to cast and to learn, and adds Wisdom. Once a day it can pour double the Mana into one spell to double its power.
- Found: Mana Overload once a day.

### Rogue
5 levels · rarity no colour
- **Requires:** Level 1
- **Identity:** The first rogue class and the way into the rogue path. It adds Speed, Evasion in light armour or clothing, and Accuracy and critical hits with one-handed weapons and bows. Once a day it can make its next attack more likely to be a critical hit.
- Found: Lucky Hit once a day for +15 Crit Rating is weak.

## Melee fighters (8)

Fighters who win with the weapon in their hand.

### Barbarian
7 levels · rarity no colour
- **Requires:** Level 1 · Warrior 5 · No Light or Heavy Armor equipped
- **Identity:** A fighter who wears neither light nor heavy armour and wins by raging. Once a battle it rages for five turns: it deals more damage, takes less and pierces armour, but can only attack, run or end its turn. It hits harder whenever it strikes the enemy it hit the turn before, and grows in Strength, Vitality, Speed and melee Accuracy.
- Found: Breaks the capstone rule: Rage comes at level 2 and Seeing Red at 5; level 7 is only stats.

### Berserker
6 levels · rarity no colour
- **Requires:** Level 15 · Warrior 5 or Archer 5 · Kill at least 3 enemies in one combat 5 times
- **Identity:** A fighter who wants to be surrounded. It deals more damage for every enemy still standing, gains weapon power, HP, Strength, Dexterity and critical hits, and once per battle swings at every enemy at once with its main weapon.
- Found: Bonuses per living enemy are spread over many classes (Champion, Slayer, Traveler, Warrior); here they are the core.

### Champion
4 levels · rarity no colour
- **Requires:** Level 40 · Slayer 5 or Gladiator 5
- **Identity:** **Sharpened.** A seasoned all-round fighter who seeks out the strongest enemy on the field (the boss, or the foe with the most HP) and grows stronger the tougher that opponent is. It keeps its broad Strength, Vitality, HP and weapon power; its challenge becomes a way to take on that strongest foe instead of a duel, which stays the Gladiator's.
- Found: Champion's Challenge is almost the Gladiator's Duel (one enemy, three turns, protected), and Arena Champion is the Berserker's per-enemy bonus. Sharpened so the duel stays the Gladiator's.

### Gladiator
7 levels · rarity no colour
- **Requires:** Level 15 · Warrior 3
- **Identity:** The arena duellist. Once per battle it challenges one enemy: for three turns the two can only fight each other, and no one else can reach either of them. It is at its strongest against a single opponent or in a duel (more Strength and Evasion), and adds Armor, critical hits and weapon power.

### Reaver
7 levels · rarity blue
- **Requires:** Level 25 · Barbarian 5 or Berserker 6
- **Identity:** A finisher that feeds on kills. Every enemy it kills brings its next turn sooner, it deals more damage to enemies below 35% HP, and it gains weapon power, Armor Pierce and Accuracy. Its strike lands far harder on an enemy that is already low.

### Slayer
7 levels · rarity no colour
- **Requires:** Level 1 · Warrior 5 · Win at least 30 combats with no living ally remaining
- **Identity:** A warrior who fights best alone. With no living companion beside it (summons don't count) all its stats rise, and it gains HP, weapon power and Armor Pierce. Once a day it can make a stand against a crowd: less damage taken, and more damage dealt for every living enemy.

### Soldier
6 levels · rarity no colour
- **Requires:** Level 25 · Warrior 5 or Archer 5 or Rogue 5
- **Identity:** A disciplined veteran who gets better the longer a battle lasts: each turn it completes adds to all its stats, up to five turns. It is steady rather than flashy (Vitality, Dexterity, Accuracy, HP, weapon power), and once per battle it can push through three turns of more damage, less damage taken and more Armor and Evasion.
- Found: Veteran's Momentum (+25% damage, −20% damage taken, +20% Armor and Evasion for 3 turns) is very strong for a class without a colour.

### Weaponmaster
7 levels · rarity no colour
- **Requires:** Level 20 · Warrior 5
- **Identity:** Master of every weapon rather than one. It adds base power, Accuracy, Armor Pierce, critical hits and weapon power to whatever it holds, and makes every weapon scale on both Strength and Dexterity. Once a day it makes its next three attacks stronger, surer and more likely to be critical.

## Knights and defenders (6)

Armour, shields and standing between the enemy and the party.

### Knight
7 levels · rarity no colour
- **Requires:** Level 1 · Warrior 5
- **Identity:** The armoured knight of the warrior path. It gains Strength in heavy armour, more Strength scaling on melee weapons, Vitality, Armor and HP. Near death (below 10% HP) all its stats surge, and once a day it can stake everything on one blow: triple damage that always hits, after which it is wide open for three turns.

### Heavy Knight
5 levels · rarity no colour
- **Requires:** Level 1 · Knight 5 · Heavy Armor and a Shield equipped
- **Identity:** The tank in heavy armour with a shield. It can taunt, so enemies will likely attack it instead of its allies until its next turn, and every melee or ranged hit it takes hardens its Armor for the rest of the battle. It adds Armor, HP and Strength.

### Guardian
7 levels · rarity no colour
- **Requires:** Level 1 · Knight 3 · Most maximum HP in the party
- **Identity:** The party's bodyguard, for the one with the most HP. It may step in front of an attack aimed at an ally and take it itself, and once per battle it can bind itself to one ally so that every attack on that ally comes to it for three turns. It adds Vitality, Armor, HP, shield Armor and less damage from everything.

### Bulwark
7 levels · rarity no colour
- **Requires:** Level 20 · Knight 4 · 10 defensive actions (Guard or Evade)
- **Identity:** **Sharpened.** The immovable wall that makes the Guard action its weapon: Guarding gives it far more Armor and cuts the damage it takes, and it adds Armor, HP and shield Armor. Its power comes from Guarding well, not from covering an ally, which stays the Guardian's.
- Found: Its skill Interpose shares its name and job with the Guardian's passive Interpose (and Aegis Bond). Sharpened so protecting allies stays the Guardian's.

### Echo Knight
7 levels · rarity blue
- **Requires:** Level 35 · Knight 7 · Summoner 4
- **Identity:** A knight with a spectral double. Once a turn its first weapon hit repeats as a ghostly echo for part of the damage, and once per battle its next hit repeats at half damage. It adds Accuracy, Armor, weapon power and HP.

### Black Knight
5 levels · rarity no colour
- **Requires:** Level 1 · Knight 7 · At least 10 Dark spells known
- **Identity:** A knight who has turned to Dark magic. Its Dark spells are stronger and cheaper, its weapon pierces more Armor, its heavy armour protects more and Dark spells hurt it less. Once a day it can transform for three turns, with much stronger weapon and Dark spells and less damage taken, though Holy spells hurt it more.
- Found: The engine's tests read this class file: if it changes, the engine's test suite runs too.

## Holy warriors (5)

Warriors of the faith. Today their features overlap a lot (four of them get bonuses against demons, two share Divine Smite); each is sharpened to one job.

### Cleric
7 levels · rarity no colour
- **Requires:** Level 1 · Warrior 5 · 3 Holy spells known
- **Identity:** **Sharpened.** The battle priest: a warrior of the faith who fights in the front line and keeps the party going with prayers in the middle of battle, blessings that make everyone hit and dodge better, and Holy spells that cost less. Its stand against demons and devils is left to the Paladin and the Templar.
- Found: Breaks the capstone rule: the passive comes at level 4 and the skill at 5; levels 6 and 7 are stats. Holy Warrior's demon bonus repeats the Paladin's and Templar's.

### Paladin
7 levels · rarity no colour
- **Requires:** Level 10 · Knight 4 or Cleric 4 · No Dark or Blood spells known · Not together with Oathsworn
- **Identity:** **Sharpened.** The holy smiter: Divine Smite turns its Mana into Holy damage on its weapon hits, and its power is aimed at demons and devils, which it banishes with heavier blows. It forswears Dark and Blood magic and adds Strength, Mind, Mana and Armor. Its bonus against Monster-class enemies is left to the Monster Hunter.
- Found: Sworn Oath works against Monster-class enemies and Banish Evil against demons; Oathsworn has the same Divine Smite and a stronger Sworn Oath.

### Death Paladin
6 levels · rarity no colour
- **Requires:** Level 1 · Paladin 7 · At least 10 Death spells known
- **Identity:** A paladin who has turned to Death magic. Its Death spells are stronger and cheaper, and its Divine Smite deals more and counts as both Holy and Death. Once a day it can perform a final rite: a far stronger weapon hit with extra Death damage that also weakens all of the target's stats for two turns.

### Oathsworn
7 levels · rarity purple
- **Requires:** Level 10 · Knight 4 · Hierophant 4 · Know no Dark or Blood spells · Not together with Paladin
- **Identity:** **Sharpened.** A knight and Basic-magic caster bound by vows. At the start of a battle it swears an oath (to protect an ally, to hold its ground, to slay a chosen enemy) and draws great power while it keeps it, losing it if the oath breaks. It channels Basic and Holy magic through its blade and still forswears Dark and Blood magic. The vows set it apart from the Paladin, which today it copies with bigger numbers.
- Found: Today it is a stronger Paladin: Arcane Vow is Sworn Oath with bigger numbers, and its skill is Divine Smite again.

### Templar
6 levels · rarity no colour
- **Requires:** Level 1 · Cleric 7 or Priest 7
- **Identity:** **Sharpened.** The holy defender of the party: armour, HP, Mana and Holy power, a protection over every ally (less damage taken, more Armor and Accuracy), and a stand against the undead and dark magic. Its share of demon hunting is left to the Paladin.
- Found: Consecrated Arms (demons, devils, undead) repeats the demon bonus of the Cleric and Paladin.

## Unarmed fighters (4)

Fists, discipline and inner power.

### Brawler
6 levels · rarity no colour
- **Requires:** Level 1 · Use 5 unarmed attacks
- **Identity:** The tavern fighter, learnt by fighting with bare fists. It adds Strength, Vitality, Evasion and unarmed power, and its melee attacks often land a little extra damage. Once a day it can brawl rough: for three turns it takes less melee damage and pierces armour with fists and weapons.

### Monk
8 levels · rarity no colour
- **Requires:** Level 6 · 30 unarmed attacks used
- **Identity:** The disciplined unarmed fighter. Its fists hit far harder and more surely, scale on Wisdom, and once per battle can stun. It adds Strength, Wisdom and Speed, and its training lets it shrug off part of spell damage.

### Elder Monk
4 levels · rarity blue
- **Requires:** Level 45 · Monk 8
- **Identity:** The monk's mastery. Its unarmed power and stun chance grow a lot, its fists scale further on Wisdom, and spell damage hurts it less. Once per battle it can land a blow that hits much harder and heals it for part of the damage.

### Ki Warrior
10 levels · rarity purple
- **Requires:** Level 30 · Energist 5 or Psykic 8
- **Identity:** A fighter who runs on Ki: it starts each battle with Ki according to its level and regains one every second turn. It spends Ki to empower an attack or spell, to restore much of its Mana, or to go into a two-turn overdrive of extra damage. It adds Strength, Dexterity, Speed, Accuracy and weapon and spell power, so it suits blade and spell alike.

## The archer's path (6)

Bows, marks and hunting one kind of prey.

### Hunter
7 levels · rarity no colour
- **Requires:** Level 1 · Archer 5
- **Identity:** The bow hunter and the root of the hunter classes. It adds bow damage, Speed and Evasion (more without heavy armour), Luck and Dexterity, and strikes harder at enemies that haven't acted yet. Its Hunter's Mark marks one enemy for more damage and much better Accuracy and comes back whenever the marked enemy dies; the other hunters build on it.
- Found: Silent Killer is the same effect as the Assassin's Combatstalker (+25% against enemies that haven't acted).

### Sharpshooter
5 levels · rarity no colour
- **Requires:** Level 1 · Archer 5
- **Identity:** The precision archer. It adds Dexterity, Luck and a lot of bow Accuracy, and once a day each it can fire a shot that is far surer and more likely to be critical, or a shot that is sure to be critical at the price of a stunned turn.
- Found: Two skills and no passive.

### Blood Hunter
5 levels · rarity no colour
- **Requires:** Level 1 · Hunter 7 · At least 5 Blood spells known
- **Identity:** A hunter who uses Blood magic. Its Blood spells and weapon grow stronger, it gains HP, its Hunter's Mark grows, and it deals more to the undead and vampires. Once per battle its next three hits are stronger and heal it for part of the damage.

### Demon Hunter
6 levels · rarity no colour
- **Requires:** Level 1 · Hunter 7 · Defeat 40 devils, demons and impids
- **Identity:** The hunter of demons, devils and impids: it deals more damage to them and takes less from them, and its Hunter's Mark grows stronger. Once per battle it can strike with much more power, and far more against those foes.

### Drakeslayer
6 levels · rarity no colour
- **Requires:** Level 1 · Hunter 7 · Defeat 3 dragons
- **Identity:** The hunter of dragons: more damage against them, less damage from them (their breath and spells too) and more Armor Pierce. It can mark a dragon for much better Accuracy and damage against it.
- Found: Dragons are few, so it is seldom useful.

### Monster Hunter
7 levels · rarity no colour
- **Requires:** Level 1 · Hunter 7 · At least 30 Monster-class enemies slain
- **Identity:** The hunter of Monster-class enemies: more damage against them, less damage from them, more Accuracy, HP and Armor Pierce, and a stronger Hunter's Mark. Once per battle its next hit on a monster is far stronger and pierces armour.

## The rogue's path (10)

Speed, stealth, poisons, traps and the killing blow.

### Assassin
6 levels · rarity no colour
- **Requires:** Level 1 · Rogue 5 or Sharpshooter 3
- **Identity:** **Sharpened.** The killer of one target. It adds Luck, Dexterity and Speed and makes its critical hits deal more damage, and once a day its next attack is a sure critical hit that ignores much of the target's Armor. Its bonus against enemies that haven't acted yet is left to the Ambusher (the Hunter has the same).
- Found: Breaks the capstone rule: Execute comes at level 5; level 6 is critical damage. Combatstalker is the Hunter's Silent Killer, and the Ambusher's First Strike is the same idea.

### Ambusher
7 levels · rarity no colour
- **Requires:** Level 1 · Rogue 5 · Speed is your highest stat
- **Identity:** Strikes first, for the fastest of the party (Speed must be its highest stat). It adds Speed, Dexterity, Luck, weapon power and Accuracy, and its first weapon attack of the battle is much stronger. Once per battle it can put a lot of extra power and Accuracy into any attack.

### Fencer
6 levels · rarity no colour
- **Requires:** Level 18 · Rogue 5 · 7 one-handed attacks
- **Identity:** The one-handed swordsman who keeps the other hand free. With a one-handed sword and an empty off-hand it gains Accuracy, Evasion, power and critical hits, and it counterattacks when an enemy misses it (once a turn). It can lunge for a surer, stronger thrust.

### Poisoner
7 levels · rarity no colour
- **Requires:** Level 15 · Rogue 5
- **Identity:** Poisons its blade. Its weapon hits poison the target for a share of its maximum HP over two turns, poisoned enemies heal less, and the poison grows stronger with mastery. It can coat its weapon so the next two hits poison more and make the target miss more. It adds Accuracy, Dexterity and critical hits.
- Found: Already in the game; reworked like the other current classes, not added again (C9).

### Shadow
6 levels · rarity no colour
- **Requires:** Level 18 · Rogue 5
- **Identity:** The one the enemy doesn't see. While another real ally is alive, enemies are less likely to target it; it gains Evasion, Speed, Luck and Accuracy, and once per battle it can vanish for two turns: much more Evasion, and hardly ever targeted.

### Eminence
4 levels · rarity blue
- **Requires:** Level 1 · Shadow 6 · 7 flawless victories
- **Identity:** The untouched. The less damage it has taken in a battle, the more it deals (up to half again as much while unhurt, nothing extra once it has lost half its HP), and once per battle it can put the same bonus into a single strike. It adds weapon power and Luck.

### Stalker
4 levels · rarity no colour
- **Requires:** Level 12 · Rogue 5
- **Identity:** The tracker on the map. It shows which kinds of enemy can be tracked in the current zone, and once a day it can pick one and start a fight with it, handy for quests and drops. It adds Speed and Accuracy.

### Thief
5 levels · rarity no colour
- **Requires:** Level 1 · Rogue 5
- **Identity:** Steals. Enemies drop more money, it gains Luck, Speed and Evasion, and once a day, if its next attack is a critical hit, it steals the enemy's weapon.

### Trap Layer
4 levels · rarity no colour
- **Requires:** Level 1 · Rogue 5 · 3 traps used
- **Identity:** Sets traps (bought at the General Store). Its traps deal more damage, pierce armour, are sometimes not used up, and trigger one more time. It has no passive or skill today.
- Found: No passive and no skill, so nothing for a capstone.

### Trap Master
3 levels · rarity no colour
- **Requires:** Level 30 · Trap Layer 4
- **Identity:** The trap expert: even more trap damage and Armor Pierce, traps kept more often, and two traps active at once. It has no passive or skill today.
- Found: No passive and no skill, so nothing for a capstone.

## Weapons and spells together (5)

Hybrids that fight with a weapon and magic at once.

### Spellblade
5 levels · rarity no colour
- **Requires:** Level 1 · Warrior 3 · Mage 3 · 3 different spells known
- **Identity:** The warrior-mage. Its melee weapons also scale on Intelligence, it gains Strength, Intelligence, Mana, melee Accuracy and spell power, and once per battle it can spend Mana to add a burst of Basic damage to its next melee hit.

### Spell Sniper
5 levels · rarity no colour
- **Requires:** Level 1 · Archer 3 · Mage 3 · 3 different spells known
- **Identity:** The archer-mage, the Spellblade's twin for bows. Bows also scale on Intelligence, it gains Dexterity, Intelligence, Mana, bow Accuracy and spell power, and once per battle it can spend Mana to add Basic damage to its next shot.

### Hexblade
5 levels · rarity blue
- **Requires:** Level 1 · Spellblade 5 · Know 5 Dark spells
- **Identity:** A spellblade of curses. Its weapon hits lower the target's Armor for two turns, its Dark spells grow stronger and cheaper, it gains Accuracy, and it can brand an enemy so that all its attacks and spells deal more to it.

### Nullblade
6 levels · rarity no colour
- **Requires:** Level 25 · Spellblade 5
- **Identity:** Cuts magic away. Each weapon hit strips one magical buff from the target, and it gains Accuracy, weapon power, a little spell resistance and cheaper spells. Weakening the target's next spell is left to the Spellbreaker.
- Found: Null Strike (weaken the target's next spell) is the Spellbreaker's Mute Sigil, smaller; left to the Spellbreaker.

### Spellbreaker
6 levels · rarity blue
- **Requires:** Level 30 · Enlightened 5 or Spellblade 5
- **Identity:** The mage-killer. It takes less damage from spells, Guards better, and while Guarding the first spell aimed at it each turn may fizzle. It can mute an enemy's next spell, weakening its damage or healing a lot.

## The mage's path (14)

Spellcraft itself: learning, casting, Mana and the mind.

### Spellcaster
7 levels · rarity no colour
- **Requires:** Level 1 · Mage 5 · Intelligence higher than Wisdom
- **Identity:** The Intelligence caster (Intelligence above Wisdom). It adds Intelligence, Wisdom, Mind and Basic damage, makes spells cheaper to learn, casts one spell a battle for one Action Point less, and gets part of the Mana back on any turn in which it cast exactly one spell and did nothing else.

### Wizard
7 levels · rarity no colour
- **Requires:** Level 1 · Mage 5 · Wisdom higher than Intelligence
- **Identity:** The Wisdom caster (Wisdom above Intelligence). It adds Wisdom, Intelligence and Mind, makes Basic spells cheaper and all spells cheaper to learn, and once a day casts a spell twice for one Mana price. Any spell another party member knows costs half as much for it to learn.

### Sorcerer
5 levels · rarity no colour
- **Requires:** Level 30 · Mage 5 · Know 20 different spells
- **Identity:** Builds Sorcery Points: every spell it casts gives one, and the points make it take less spell damage. It can spend them all in a blast of Basic damage. It adds Intelligence, Wisdom, Evasion and cheaper spells.
- Found: Sorcery Blast (5 damage per point) is weak at the level it needs (30).

### Great Mage
7 levels · **rarity no colour → blue** (needs Level 50 and is already among the strongest casters; a colour fits it better than weakening it)
- **Requires:** Level 50 · Mage 5
- **Identity:** The archmage. It raises spell power, Mana, spell damage and the three mental stats across the board, makes spells much cheaper to cast and to learn and cost one Action Point less, and once a day empowers its next three spells further.
- Found: Very strong for a class without a colour (hence the rarity proposed).

### Scholar
5 levels · rarity no colour
- **Requires:** Level 1 · Mage 5 · Know at least 10 different spells
- **Identity:** The learner. Spells cost much less to learn, it gains Intelligence, Wisdom, Mind and Mana, and once per battle it casts a random spell from the whole game (no higher than its best tier) for free.

### Wise Man
5 levels · rarity no colour
- **Requires:** Level 1 · Scholar 5
- **Identity:** The party's teacher. Spells cost much less to learn, it gains Mana and Wisdom, and every party member's spells cost less Mana (several Wise Men add up). Once a day it can cast one spell three times at reduced power for one Mana price.

### Archivist
5 levels · rarity blue
- **Requires:** Level 20 · Scholar 5 · Know 20 spells
- **Identity:** **Sharpened.** Records the enemy's magic: it remembers every enemy spell cast in a battle and, after winning, turns one of them into a scroll that casts it once. It learns spells for less and gains Mind. Copying an enemy's spell on the spot is left to the Mirror Mage.
- Found: Open Ledger (copy the last spell at 70%) is the Mirror Mage's Spell Mirror (75%).

### Chronomancer
5 levels · rarity blue
- **Requires:** Level 45 · Wizard 7 · Know 10 Basic spells
- **Identity:** Time magic. Its turn comes round faster without more Speed, its Basic spells grow cheaper and stronger, and it can push its own turn forward while pushing an enemy's back.

### Mirror Mage
4 levels · rarity blue
- **Requires:** Level 40 · Wizard 7
- **Identity:** The mirror. It takes less spell damage, throws part of every enemy spell back at its caster, and once per battle copies the enemy's last spell at three quarters of its power. It adds Mind.

### Dreamweaver
6 levels · rarity no colour
- **Requires:** Level 30 · Sorcerer 5
- **Identity:** Puts enemies to sleep and preys on them. Once per battle it stuns an enemy for a turn, it deals more damage to stunned enemies, and it adds Dark and Basic spell power, Mind and cheaper spells.

### Element Siphon
5 levels · rarity blue
- **Requires:** Level 30 · Spellcaster 7 · Know spells of 6 elements
- **Identity:** Feeds on enemy magic. It takes less elemental spell damage, gets Mana back from the elemental hits it suffers, and once per battle turns its next spell into the element an enemy just hit it with. It adds Mana and spell power.

### Sage
5 levels · rarity blue
- **Requires:** Level 1 · Spellcaster 7 or Wizard 7 · Maximum Mana above maximum HP
- **Identity:** The deep well of Mana, for one whose Mana outgrows its HP. It greatly raises maximum Mana and Mind, regenerates Mana every turn, and once per battle restores a quarter of every ally's Mana.

### Energist
5 levels · rarity no colour
- **Requires:** Level 20 · Maximum Mana is at least 70% of maximum HP
- **Identity:** Balances body and mind, for one whose Mana is at least 70% of its HP. It regenerates some HP and Mana every turn, makes spells cheaper, raises HP, Mana, Vitality and Mind, and once per battle restores a fifth of both at once.

### Psykic
8 levels · rarity purple
- **Requires:** Level 30 · Spellcaster 7 or Wizard 7 · Know at most 15 spells
- **Identity:** Fights with its own mind instead of many spells (it must know few). It has its own attacks paid with Mana: a single-target burst usable every turn, a lance that can stun, a storm that hits and slows every enemy, and a mental barrier for defence; mastery makes them stronger. It adds Mind, Intelligence and Mana.

## The twelve elemental mages (12)

One per element, all built the same way: an early skill (level 3), a passive (5) and a great skill (9). Levels 4, 6, 7 and 8 are the same in all twelve (10% cheaper spells, +10% Mana, +15% damage, 30% cheaper learning); they get a look in the features round.

### Aeromancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Wind spells
- **Identity:** The Wind mage. Wind spells grow stronger and cheaper and cost one Action Point less, and it adds Speed and Mana. Once per battle the whole party gains Speed and Evasion for two turns, and once a day a greater storm gives the party even more and boosts its Wind spells.

### Aquamancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Water spells
- **Identity:** The Water mage. Water spells grow stronger and cheaper, and it adds Mind and Mana. Once per battle it restores part of its own HP and Mana, and once a day it casts a Water spell twice for the price of one.

### Botanomancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Nature spells
- **Identity:** The Nature mage. Nature spells grow stronger and cheaper, and it gains Wisdom, Mana and HP. Once per battle every ally regenerates HP for three turns, and once a day it calls two animals to fight beside it.
- Found: Call of the Ancient Pack is the Beastmaster's Call the Pack.

### Cryomancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Ice spells
- **Identity:** The Ice mage. Ice spells grow stronger and cheaper, it gains Wisdom and Mana and takes less damage. Once per battle it freezes one enemy for a turn, and once a day it freezes every enemy.

### Electromancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Lightning spells
- **Identity:** The Lightning mage. Lightning spells grow much stronger and cheaper, and it gains Intelligence, Speed and Mana. Once per battle it overcharges one Lightning spell, and once a day it strikes every enemy with lightning.

### Geomancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Earth spells
- **Identity:** The Earth mage. Earth spells grow stronger and cheaper, it gains Vitality, Mana and Armor and takes less Earth damage. Once per battle it turns to stone for a lot of Armor, and once a day it hits one enemy with a heavy, armour-piercing Earth strike.

### Hemomancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Blood spells
- **Identity:** The Blood mage. Blood spells grow much stronger and cheaper, and it gains Vitality, HP and Mana. Once per battle it turns HP into Mana, and once a day it sacrifices more HP for a lot of Mana and three turns of greater Blood power and stats.

### Hierophant
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Basic spells
- **Identity:** The Basic mage. Basic spells grow stronger and cheaper and cost one Action Point less, and it gains Intelligence, Wisdom and Mana. Once per battle one Basic spell is stronger and quicker, and once a day one Basic spell is cast three times for one price.

### Luxomancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Holy spells
- **Identity:** The Holy mage. Holy spells grow stronger and cheaper, it gains Mind and Mana and takes less Dark damage. Once per battle it heals the whole party, and once a day it greatly heals everyone and removes a debuff from each.

### Necromancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Death spells
- **Identity:** The Death mage. Death spells grow stronger and cheaper, its summons are stronger, and it gains Mind and Mana. Once per battle it raises an undead thrall (two can stand at once), and once a day it raises three at once.

### Pyromancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Fire spells
- **Identity:** The Fire mage. Fire spells grow stronger and cheaper, it gains Intelligence and Mana and takes less Fire damage. Once per battle one Fire spell is much stronger and cheaper, and once a day it becomes a living inferno for three turns: much stronger Fire, but it takes more damage.

### Umbramancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Dark spells
- **Identity:** The Dark mage. Dark spells grow much stronger and cheaper and it gains Intelligence and Mana, but Holy spells hurt it more. Once per battle it hides in shadow for two turns (Evasion and Dark power), and once a day it weakens all of every enemy's stats for three turns.

## Masters of elements (4)

Classes built on several elements or one element's special side.

### Avatar
4 levels · **rarity no colour → blue** (needs four elemental mages at Level 5)
- **Requires:** Level 1 · At least 4 core elemental mancer classes at Level 5
- **Identity:** Master of the six core elements (Fire, Water, Earth, Wind, Lightning, Ice). Those spells grow stronger and cheaper, it takes less damage from those elements, and each such spell counts as all six, so it always strikes the target's weakest resistance. Once a day it enters a three-turn state of much stronger, cheaper and quicker elemental spells.

### Stormcaller
7 levels · rarity purple
- **Requires:** Level 30 · Aeromancer 5 · Electromancer 5
- **Identity:** Wind and Lightning in rhythm. Casting Wind and Lightning spells in turn builds up to 20 charges, each adding spell power, and it can spend them all at once to damage every enemy and bring the party's turns forward. It adds Wind and Lightning power, Speed and cheaper spells.

### Sunstrider
6 levels · rarity purple
- **Requires:** Level 25 · Luxomancer 5 · Priest 5
- **Identity:** The sun's Holy power. Its Holy spells are cheaper and stronger, most of all by day (06:00 to 18:00), it gains Speed in daylight and more Mana, and it can burn every enemy with Holy fire, harder by day.

### Plaguebinder
6 levels · rarity no colour
- **Requires:** Level 25 · Warlock 5 or Necromancer 5
- **Identity:** Spreads afflictions. When an enemy dies poisoned or cursed, what is left of it jumps to another enemy; it adds Dark and Death spell power, Mind and cheaper spells, and it can poison every enemy for three turns with a plague cloud.

## Priests and healers (4)

Healing and protecting the party with magic.

### Priest
7 levels · rarity no colour
- **Requires:** Level 1 · Mage 5 · 3 Holy spells known
- **Identity:** The holy healer of the mage path. It adds Wisdom, Mind, Mana, Holy power and cheaper Holy spells, gains HP and takes less Dark damage, and once a day raises a sanctuary: for three turns everyone takes less damage and heals at the start of each turn.

### Cardinal Priest
4 levels · **rarity no colour → blue** (needs Level 55 and Priest 7)
- **Requires:** Level 55 · Priest 7
- **Identity:** The high priest. Its Holy power, Wisdom and Mana grow a lot, every party member's spells cost less and Holy spells cost it half to learn, and once a day it decrees three turns in which everyone takes much less damage and restores HP and Mana each turn.

### Lifewarden
6 levels · rarity no colour
- **Requires:** Level 30 · Priest 5 or Cleric 5
- **Identity:** The keeper of life. Healing beyond an ally's maximum HP becomes a barrier, it adds Holy and Water power, HP, Mana and cheaper spells, and it can heal every ally for a fifth of their HP.

### Sanguine Medic
5 levels · rarity blue
- **Requires:** Level 1 · Hemomancer 5 · Priest 5
- **Identity:** Heals with its own blood. It spends its own HP to restore an ally's, heals better while it is below half HP, and adds HP and Blood and Holy power.

## Summoners and dark pacts (9)

Creatures, pacts and rituals.

### Summoner
7 levels · rarity no colour
- **Requires:** Level 1 · Mage 5 · 10 summon spells used
- **Identity:** Lets its creatures fight for it. Summon spells are much cheaper, its creatures are stronger, and while one of them lives it can't be attacked at all. Once a day it can summon for free and quicker. It adds Mind and Speed.
- Found: Overprotective (can't be attacked at all while a summon lives) is very strong.

### Beastmaster
7 levels · rarity no colour
- **Requires:** Level 1 · Summoner 3 · At least 1 Nature Summon spell known
- **Identity:** Animals at its side. It starts every battle with a wolf, bear or griffon beside it, its summons grow much stronger and cheaper, and once a day it calls two more animals. It adds Mind and Mana.

### Wildwarden
5 levels · rarity no colour
- **Requires:** Level 1 · Beastmaster 5 · Know 1 Nature summon spell
- **Identity:** The pack's protector. Its animal summons take less damage while it lives, grow stronger and can be given more Armor on command; Nature summons cost less, and it has more Armor in forests.

### Soul Shepherd
5 levels · rarity blue
- **Requires:** Level 35 · Summoner 7
- **Identity:** Shepherd of summoned souls. Its summons are stronger and cheaper, it recovers HP and Mana whenever one of them dies, and it can give all of them more Armor for two turns.

### Druid
7 levels · rarity no colour
- **Requires:** Level 1 · Mage 5 · 3 Nature spells known
- **Identity:** Nature's own caster. Nature spells are much cheaper to cast and to learn and much stronger, Nature damage hurts it less, and it gains Mind and Wisdom. Once a day it becomes one with the world (all stats and Nature power up for five turns), and when it falls below 10% HP an animal comes to its aid.
- Found: Two passives and one skill.

### Warlock
7 levels · rarity no colour
- **Requires:** Level 1 · Mage 5 · 3 Dark spells known
- **Identity:** The Dark caster of the mage path. Dark spells are stronger and cheaper to cast and to learn, Dark damage hurts it less, it gains Intelligence, Mind and Mana, and once per battle it can embrace the dark for three turns: much stronger, cheaper Dark spells, but it takes more damage.

### Pactkeeper
4 levels · rarity blue
- **Requires:** Level 30 · Warlock 7 · Summoner 5 · Cast the same summon 10 times
- **Identity:** Bound to one familiar. It binds one summon it knows, which returns at the start of every battle for a little of its HP and grows a little stronger with each battle it survives. Its summons are stronger, and it can restore their HP and Mana.

### Cultist
7 levels · rarity no colour
- **Requires:** Level 1 · Use 10 Dark-Element summon spells
- **Identity:** Dark summoning rites. Summon spells (Dark ones most) are much cheaper, its other summons count as Dark, and once per battle a ritual makes its next summon cheaper and its creatures stronger. It adds Mind, Speed, Intelligence and Wisdom.
- Found: Breaks the capstone rule: Summon Ritual comes at level 3 and Dark Rituals at 6; level 7 is stats.

### Chaos Cultist
3 levels · **rarity no colour → blue** (needs Level 50, Cultist 7 and 100 Dark summons)
- **Requires:** Level 50 · Cultist 7 · Use 100 Dark-Element summon spells
- **Identity:** The cult's high caller. Every battle starts with one of its Dark summons called for free, and once a day, at the price of a stunned turn, it performs a grand summoning that brings one more creature for no Mana. It adds Mind.

## Leaders and minds (5)

Commanding the party, and seeing what the enemy will do.

### Strategist
7 levels · rarity no colour
- **Requires:** Level 20 · 15 defensive actions by the whole party
- **Identity:** Commands the party. Every ally gains Speed, Dexterity and Luck, every ally's Guard and Evade work better, and once per battle it marks one enemy that the whole party deals more damage to until it falls. It adds Mind and Evasion.

### General
5 levels · rarity blue
- **Requires:** Level 50 · Strategist 7
- **Identity:** The commander. Allies gain more Speed and better Guard and Evade, its Focus Fire grows stronger and every hit on the marked enemy brings the other allies' turns forward, and once per battle the whole line gains Armor and Evasion for two turns. It adds Mind.

### Flagbearer
4 levels · rarity blue
- **Requires:** Level 1 · Strategist 5 · 2 companions
- **Identity:** Carries the colours for a party with companions. Its living companions gain Strength, Dexterity and Speed, it gains Armor, and it can raise a battle standard that makes allies deal more damage until the enemy destroys it.

### Enlightened
5 levels · rarity no colour
- **Requires:** Level 10 · 25 defensive actions (Guard or Evade)
- **Identity:** Insight in defence. Guard and Evade work better, it takes less damage while Guarding or Evading, it gains Mind, and once per battle it can see an enemy's most likely next action.

### Diviner
7 levels · rarity blue
- **Requires:** Level 35 · Enlightened 5
- **Identity:** Foresight. Once per battle it predicts an enemy's next action; if right, it gains a lot of Armor and Evasion for two turns (and takes less damage at full mastery). It also adds Mind, Evasion, Armor and better Guard and Evade.

## Luck (4)

Fortune, dice and wagers.

### Bard
5 levels · rarity no colour
- **Requires:** Level 1 · At least 50 Luck
- **Identity:** The lucky performer. It adds Luck and Mind, cheaper spells, Mana and spell power, and once a day its ballad gives the whole party more stats, Accuracy and Evasion for three turns.

### Jester
5 levels · rarity no colour
- **Requires:** Level 1 · 120 Luck
- **Identity:** The fool fortune loves. Masterwork weapons, armour and artifacts drop more often, it gains Luck and Evasion, and it can roll a die until it comes up six (once a battle), making its next attack a sure critical hit.
- Found: Level 4 has no effect at all.

### Joker
3 levels · **rarity no colour → blue** (needs 220 Luck and Jack of all Trades 4 or Jester 5)
- **Requires:** Level 1 · Jack of all Trades 4 or Jester 5 · 220 Luck
- **Identity:** Pure chaos for enormous Luck. Any damage it deals may be boosted by a random share of its Luck, and once a day it can cast spells almost free and quicker for several turns at a fraction of their damage, then is stunned for three turns.

### Gambit
5 levels · rarity blue
- **Requires:** Level 20 · Jester 3
- **Identity:** The gambler. A missed attack gives back an Action Point (once a turn), it gains Luck, Accuracy and critical hits, and once per battle it can wager three Action Points on one strong strike: a hit returns two, a miss ends its turn.

## The road, trade and the guild (12)

Travel, money, supplies, quests, artifacts and alchemy.

### Traveler
3 levels · rarity no colour
- **Requires:** Level 10 · 600 km traveled
- **Identity:** The road. It travels faster on the map and often keeps its Camping Supplies after using them; in battle (oddly for a traveller) it gains Speed for every living enemy.
- Found: The Road Ahead (Speed per living enemy in battle) has nothing to do with travelling.

### Cartographer
5 levels · rarity no colour
- **Requires:** Level 1 · Traveler 3 · Travel 1,500 km
- **Identity:** Maps the land. It travels faster, meets fewer wild encounters, keeps Camping Supplies more often, and sees more on the map (its zone, nearby towns and quest markers). In battle it can give every ally Evasion and Accuracy for two turns.
- Found: Surveyor's Eye predates the one continent of task 8; checked in the features round.

### Courier
3 levels · rarity no colour
- **Requires:** Level 1 · Traveler 3 · Complete 15 guild Travel quests
- **Identity:** Delivers for the guild. It travels faster, has more Armor and Evasion while on a Travel, Escort or Deliver quest, and in battle can speed up the whole party for two turns.

### Pilgrim
5 levels · rarity no colour
- **Requires:** Level 1 · Priest 4 · Traveler 3
- **Identity:** The holy traveller. It travels faster, keeps Camping Supplies, earns more Reputation, and after 60 km of travel since its last rest the party gains Armor and Evasion. Once per battle it restores HP and Mana to everyone.

### Riverstrider
5 levels · rarity no colour
- **Requires:** Level 18 · Traveler 3 or Aquamancer 3
- **Identity:** At home on rivers. It crosses rivers faster, has more Evasion in river zones and some Water spell power, and it can dance out of harm's way for two turns while restoring Mana.
- Found: Narrow: most of it works only in river zones.

### Salesman
4 levels · rarity no colour
- **Requires:** Level 1 · Sell at least 200 items
- **Identity:** Money. Items sell for more, enemies drop more money, and in battle every 5 gold it carries raises all its stats (up to a fifth); once per battle it can pay a gold for three turns of more damage and Evasion.

### Scavenger
**7 → 5 levels** · rarity no colour
- **Requires:** Level 1 · Loot 30 enemies
- **Identity:** Picks the battlefield clean. Enemies drop more money, masterworks drop more often, defeated enemies may leave Camping Supplies, and once a day it can scrounge up a Camping Supplies item.
- Found: Seven levels, three of them the same +5% money and two the same +1 masterwork chance; five levels keep every kind of feature.

### Quartermaster
3 levels · rarity no colour
- **Requires:** Level 20 · 2 companions
- **Identity:** Keeps a party with companions supplied. Consumables are sometimes not used up and restore more to everyone, and once per battle it restores HP and Mana to every ally.

### Bounty Broker
4 levels · rarity no colour
- **Requires:** Level 1 · Hunter 4 or Salesman 2 · Complete 5 guild quests
- **Identity:** The guild's contractor. Guild quests pay more money and Reputation, the guild offers it one more quest, it deals more damage to its current quest target, and it can mark an enemy so the whole party deals more damage to it.

### Relic Warden
5 levels · rarity no colour
- **Requires:** Level 25 · 2 artifacts equipped
- **Identity:** The keeper of artifacts, with two of them worn. Its artifacts' bonuses grow, its allies share a quarter of their stat bonuses, and it can make every ally take less damage for two turns. It adds Armor and HP.

### Alchemist
5 levels · rarity no colour
- **Requires:** Level 20 · 10 potions brewed
- **Identity:** The brewer (task 9). An ingredient's details show every potion and poison it goes into, foraging finds more and more often, and once a day its next forage is sure to find something, rare ingredients more likely. It adds Wisdom.
- Found: New with task 9.

### Assimilator
7 levels · rarity no colour
- **Requires:** Level 10 · 25 ingredients foraged
- **Identity:** Eats ingredients in battle (task 9). For one Action Point an eaten ingredient gives a buff made from the potions it goes into, growing with mastery to two thirds of their strength with their special effects; sometimes the ingredient isn't used up, and once per battle one costs nothing and lasts twice as long. It adds Vitality and Nature resistance.
- Found: New with task 9.

## Race classes (8)

Classes for one race. Beast, Great Beast and Monster belong to races players can't choose (enemies use them).

### Noble
3 levels · rarity no colour
- **Requires:** Level 30 · Race: Human
- **Identity:** The human's class. It earns much more money and Reputation from quests, has more Evasion in clothing, and once a day inspires every ally with a lot of Accuracy and Evasion for two turns.

### Reveler
3 levels · rarity no colour
- **Requires:** Level 30 · Race: Hobbit
- **Identity:** The hobbit's class. Every ally (itself too) gains Dexterity, Speed and Luck, it has more Mana, and once a day it holds a feast: Mana for everyone and much more Dexterity, Speed and Luck for three turns.

### Stonebreaker
3 levels · rarity no colour
- **Requires:** Level 30 · Race: Dwarf
- **Identity:** The dwarf's class. It takes less melee and ranged damage and can't be stunned, heavy armour protects it more, and once a day it can shatter an enemy with a doubled, stunning blow, at the price of taking more from the next hit.

### Sylvan
3 levels · rarity no colour
- **Requires:** Level 30 · Race: Elf or Dark Elf
- **Identity:** The elf's and dark elf's class. It regenerates Mana every turn, has much more Mana and Mind, and once a day restores a lot of Mana at once and more over three turns.

### Dragonblood
3 levels · rarity no colour
- **Requires:** Level 30 · Race: Lizardman
- **Identity:** The lizardman's class. All its stats and HP rise, it gains Armor and Strength, and once a day it awakens its blood for three turns of higher stats, at no cost in Action Points.

### Beast
3 levels · rarity no colour
- **Requires:** Level 1 · Non-playable race
- **Identity:** For characters of a beast race (not playable). It fights with natural claws that grow with its level, raises Strength, Vitality, Dexterity and Speed, and counts as having the four starting classes at Level 5.
- Found: No passive and no skill, so nothing for a capstone. Not playable.

### Great Beast
10 levels · rarity no colour
- **Requires:** Level 1 · Beast 3 · Non-playable race
- **Identity:** The beast grown great (not playable). Its claws, HP and stats grow, weaker enemies are cowed (less Strength and Evasion), and once per battle it charges with a sure but never critical blow, leaving itself open.
- Found: Not playable.

### Monster
3 levels · rarity no colour
- **Requires:** Level 1 · Non-playable race
- **Identity:** For characters of a monster race (not playable). All its stats, HP and Mana rise, and it counts as having the four starting classes at Level 5.
- Found: No passive and no skill, so nothing for a capstone. Not playable.

## Classes from rare items (6)

Unlocked by an item that is used up (a vial, a core, a crystal, a gem).

### Dragonkin
7 levels · **rarity no colour → blue** (needs a Vial of Dragonblood (0.5% from dragons))
- **Requires:** Level 1 · Vial of Dragonblood (consumed on acquisition)
- **Identity:** For one who drank a Vial of Dragonblood. All its stats, HP, Armor and Mana rise, it heals a little every turn and takes less damage from attacks, and with another vial it can take to the air: much more Speed and Evasion, and melee attacks struggle to reach it.

### Lich
5 levels · rarity purple
- **Requires:** Level 1 · Phylactery Core (consumed on acquisition)
- **Identity:** For one who consumed a Phylactery Core. While it has Mana, damage can't kill it: the Mana is drained instead. It regenerates Mana, its Death spells cost half, and once a day it raises three strong undead thralls.

### Madman
3 levels · **rarity no colour → purple** (needs a Shadow Crystal (0.05% from any enemy))
- **Requires:** Level 1 · Shadow Crystal (consumed on acquisition)
- **Identity:** For one who consumed a Shadow Crystal. It has no Armor or Evasion at all, but half again as much HP, higher stats and much more damage, and once a day it falls into a three-turn revelation: far higher stats, double damage, every weapon hit a critical, though it takes more damage.

### Sharmat
3 levels · **rarity no colour → purple** (needs a Gem of False Dreams, Ur-Mat's drop at Level 93)
- **Requires:** Level 1 · Gem of False Dreams (consumed on acquisition)
- **Identity:** For one who consumed a Gem of False Dreams. It takes less damage from everything and gains all stats, and its lucid change restores all HP and Mana and every other skill and strengthens it for the battle, while it loses HP every later turn; each further use takes another gem.

### Vampire
4 levels · **rarity no colour → blue** (needs a Blood Crystal, a boss's drop (Tankina))
- **Requires:** Level 1 · Blood Crystal (consumed on acquisition)
- **Identity:** For one who consumed a Blood Crystal. Its feeding attack (three times a battle) heals it, weakens the enemy and stores Blood Supplies, which it drinks at the start of its turns to restore HP and Mana. It gains stats and cheaper Blood spells.
- Found: Breaks the capstone rule: Bloodfeed comes at level 1 and Heart of Crimson at 2; levels 3 and 4 are stats and cheaper Blood spells.

### Vampire Lord
3 levels · rarity no colour
- **Requires:** Level 40 · Vampire 4
- **Identity:** The vampire's lord. It feeds more often and without losing Accuracy, its Blood Supplies restore more, and once a week it can burn all its supplies in a frenzy of far higher stats and regeneration, after which it is left with almost nothing.

## Legends and the rest (4)

### Hero
3 levels · rarity gold
- **Requires:** Level 92 · Not together with Demon Lord
- **Identity:** The legend who saves the world (never together with the Demon Lord). All its stats rise, every ally gains stats and restores HP and Mana each turn, and once per battle it lands a strike that nothing can lessen, after which the party takes much less damage for four of its turns.

### Demon Lord
3 levels · rarity gold
- **Requires:** Level 92 · Not together with Hero
- **Identity:** The legend who ends it (never together with the Hero). All its stats rise, every enemy loses stats, it deals much more to enemies below half HP, and once per battle it lands a world-ending strike that also burns the target's Mana.

### Raid Boss
1 level · rarity no colour
- **Requires:** Level 1 · Unobtainable
- **Identity:** Enemy only: what makes a raid boss (far more HP, Mana and stats, more Action Points each turn, triple EXP when defeated). Players can't get it.
- Found: Can't be acquired by players.

### Jack of all Trades
4 levels · rarity no colour
- **Requires:** Level 1 · Warrior 3 · Archer 3 · Mage 3 · Rogue 3
- **Identity:** A bit of everything, for one with all four starting classes at Level 3. It raises the four main stats, Armor and Evasion, gains all stats for every class it has at Level 3 or more, and counts as having the four starting classes at Level 5.
- Found: Breaks the capstone rule: its passive comes at level 2; level 4 fills the prerequisites.

# New classes

## The task's list (25)

The 26 names in the task, but Poisoner, which is already in the game (C9); "Ascendend" is Ascended (C10).

### Acolyte
5 levels · rarity no colour
- **Requires:** Level 1 · Know 1 Holy spell
- **Identity:** A novice of the faith, the way into holy magic without the mage's path. Its small prayers bless an ally (a little more Armor or Accuracy) or mend a little HP, and each prayer it says in a battle builds devotion that makes the next one stronger. It adds Wisdom and makes Holy spells cheaper to learn.

### Bruiser
6 levels · rarity no colour
- **Requires:** Level 15 · Warrior 5 · Win 10 battles in which you fell below half HP
- **Identity:** Takes the hits so it can hit back. Every blow it takes builds Grit; its next attacks spend the Grit for extra damage, so it is strongest after being hurt. It adds Strength, Vitality and HP and works with any weapon, and at its peak it can unleash all its Grit in one crushing blow.

### Spell Sparkler
5 levels · rarity no colour
- **Requires:** Level 10 · Mage 5 · Cast 3 spells in one turn, 10 times
- **Identity:** A caster of many small spells instead of one big one. Tier 1 to 3 spells cost less Mana and fewer Action Points, and each spell it casts in a row in the same turn sparkles brighter, adding power to the next. Its skill fires a quick burst of free low-tier spells.

### Hermit
5 levels · rarity blue
- **Requires:** Level 25 · Spend 10 days in a row without entering a town
- **Identity:** Draws strength from solitude. The longer it stays out in the wild without entering a town, the more its stats and resistances grow (a town resets them), and it fights best with few companions. It regenerates its own HP and Mana and can withdraw into itself in battle to recover.

### Blade Dancer
7 levels · rarity blue
- **Requires:** Level 20 · Rogue 5 · Bard 3 · Land 5 weapon hits in a row in one battle, 10 times
- **Identity:** Fights like a dance. Every weapon hit in a row raises its Evasion and Speed a little further, and a miss breaks the rhythm. It does best with light or paired blades in light armour, and its skill is a flurry that strikes several times in one fluid move.

### Hotshot
6 levels · rarity no colour
- **Requires:** Level 15 · Archer 5 · Make 3 ranged attacks in one turn, 20 times
- **Identity:** Fast and flashy with bows and crossbows. Its ranged attacks cost fewer Action Points but hit for less, so it fires more often each turn, and it deals extra damage for spreading its shots over different enemies. Its skill is a quick volley at several targets.

### Juggernaut
6 levels · rarity blue
- **Requires:** Level 30 · Knight 5 · Heavy Armor equipped
- **Identity:** Turns heavy armour into a weapon. Part of its Armor is added to its damage, it can't be stunned or slowed, and it gets harder to stop the longer the battle goes. Its skill is a charge that strikes through the enemy line.

### Rearguard
5 levels · rarity no colour
- **Requires:** Level 15 · Warrior 5 · Win 20 battles in which no companion fell
- **Identity:** Covers the back of the party. It protects the weakest allies (casters and the wounded) by striking back at enemies that attack them and giving them more Armor, and it makes running from a battle safe for the whole party. Its skill holds the line while the party regroups.

### Vanguard
5 levels · rarity no colour
- **Requires:** Level 15 · Warrior 5 · Take the first turn of a battle 25 times
- **Identity:** The front of the charge. It is strongest in the opening turns of a battle (Speed and damage), and each of its early strikes brings the other allies' turns forward. Its skill is a charge that opens the battle and sets the party's pace.

### Caravanist
4 levels · rarity no colour
- **Requires:** Level 20 · Traveler 3 · Travel by caravan 5 times
- **Identity:** A merchant of the road, built on task 8's caravans. Caravan fares cost less and trips go faster, it buys cheaper and sells dearer in towns, and its wagon now and then supplies the party with a free consumable. In battle it can hand out supplies to everyone.

### Acrobat
6 levels · rarity no colour
- **Requires:** Level 10 · Rogue 5 · Dexterity is your highest stat
- **Identity:** Dodges everything. Its Evade action is stronger and cheaper, every attack it dodges builds momentum that strengthens its next move, and it adds Dexterity, Speed and Evasion. Its skill is a flip that dodges every attack until its next turn.

### Explorer
5 levels · rarity no colour
- **Requires:** Level 15 · Traveler 3 · Enter 8 regions
- **Identity:** Goes where no one has been. Entering a region for the first time and meeting a new kind of enemy reward it with EXP and silver, it finds more when foraging, and it can uncover hidden caches on the map. In battle its sharp eye finds an enemy's weak spot.

### Smith
5 levels · rarity no colour
- **Requires:** Level 10 · Reforge 5 items
- **Identity:** The craftsman in the field. Reforging and fitting runes cost it less, and before a battle it can whet a weapon or mend a piece of armour for a short boost. It adds Strength and raises its forged gear's own bonuses a little.

### Mastersmith
4 levels · rarity purple
- **Requires:** Level 45 · Smith 5 · Reforge 25 items
- **Identity:** The Smith perfected. Gear it reforges comes out better than the forges sell it: stronger parts, an extra rune slot, larger bonuses for matching makers. Its own forged gear gains power as it masters the craft.

### Tinkerer
5 levels · rarity no colour
- **Requires:** Level 15 · Trap Layer 3
- **Identity:** Builds gadgets in battle. It deploys small clockwork devices that act on their own turn (a turret that shoots, a decoy that draws attacks, a pump that mends), and its traps are better. Its skill builds a bigger device.

### Informant
4 levels · rarity no colour
- **Requires:** Level 20 · Rogue 5 · Buy information from the hooded man 3 times
- **Identity:** Knows everything about the enemy. In battle it can reveal an enemy's stats, resistances and drops, and the party deals more damage to what it has revealed. The hooded man sells it information for less.

### Infiltrator
5 levels · rarity blue
- **Requires:** Level 20 · Rogue 5 · Shadow 3
- **Identity:** Gets past the defenders. Its attacks ignore taunts, guards and interceptions, it strikes harder at enemies sheltering behind others, and its skill sabotages an enemy so its next action fails or weakens.

### Rune Knight
6 levels · rarity blue
- **Requires:** Level 25 · Knight 5 · Spellblade 3 · Fit 10 runes
- **Identity:** Wakes the runes in its gear. Runes slotted in its weapon and armour grow stronger, and in battle it can invoke a rune for a burst of its power (a fire rune for a blast of flame, a rune of heft for a crushing blow).

### Martial Artist
7 levels · rarity blue
- **Requires:** Level 20 · Monk 4 · Brawler 3
- **Identity:** Fights in stances. Each turn it takes a stance: strike (more damage), guard (less damage taken) or counter (strikes back when attacked), and each stance shapes its attacks until it changes. It fights unarmed or with light weapons.

### Reaper
6 levels · rarity purple
- **Requires:** Level 40 · Reaver 5 · Know 5 Death spells
- **Identity:** Harvests souls. Every enemy that dies in the battle gives it a soul, which it spends on strikes with added Death damage or to heal itself. Enemies close to death are easier for it to finish.

### Ascended
4 levels · rarity gold
- **Requires:** Level 85 · Awakened 5
- **Identity:** A mortal grown beyond itself, the step after Awakened. All its powers rise with its character level, and once a day it ascends for a few turns: part of every hit passes through it, and its attacks carry heavenly power.

### Titan
5 levels · rarity purple
- **Requires:** Level 50 · Strength is your highest stat · Defeat 30 giants
- **Identity:** Colossal. It has huge HP and Strength but less Speed, and its blows are slow but crushing, striking several enemies at once. Its skill shakes the ground under every enemy.

### Seeker
5 levels · rarity no colour
- **Requires:** Level 20 · Hunter 3 · Complete 5 Search quests
- **Identity:** Never loses its target. After a miss, its next attack can't miss; evasive enemies lose part of their Evasion against it; and it finds what it looks for faster (the guild's Search quests). Its skill locks onto one enemy for sure hits.

### Cycle
4 levels · rarity blue
- **Requires:** Level 40 · In one battle, Attack, Guard, use a healing item and cast a spell; 10 times
- **Identity:** Lives in a cycle. Each turn it moves to the next of four phases (strike, guard, mend, focus) and gets that phase's bonus; when it completes a full cycle it releases a burst of power. Its skill lets it skip to the phase it needs.

### Unreality
5 levels · rarity purple
- **Requires:** Level 60 · Dreamweaver 5
- **Identity:** Bends what is real. Illusions of itself take some of the hits meant for it, enemy attacks sometimes strike nothing, and its own spells may come out stronger or weaker than cast. Its skill makes the whole battlefield unreal for a turn.

## The seven from classes/newc/ (7)

Names only; everything about them proposed fresh (C4, C7).

### Chronicler
5 levels · rarity blue
- **Requires:** Level 20 · Scholar 3 · Defeat 50 different kinds of enemy
- **Identity:** Keeps the record of every battle. The more of one kind of enemy it has fought, the better it fights them (a bestiary of its own), and it earns more EXP from kinds it hasn't recorded yet.

### Shaman
6 levels · rarity no colour
- **Requires:** Level 10 · Mage 5 · Know 2 Nature and 2 Wind spells
- **Identity:** Speaks with spirits. It raises totems on the battlefield that help the party every turn (one mends, one wards, one empowers) until the enemy breaks them. It adds Wisdom and Mind.

### Elder Shaman
4 levels · rarity blue
- **Requires:** Level 45 · Shaman 6
- **Identity:** The Shaman's elder. It can keep several totems up at once, its spirits are stronger and harder to break, and it can call the ancestors to fight beside the party.

### Trickster
5 levels · rarity no colour
- **Requires:** Level 10 · Rogue 5 · At least 60 Luck
- **Identity:** Fights with tricks. Its feints make an enemy waste its turn, it can turn an enemy's attack onto another enemy, and it swaps its own debuffs onto enemies and their buffs onto itself.

### Awakened
5 levels · rarity purple
- **Requires:** Level 50 · 4 classes at their final level
- **Identity:** Mastery awakened. It grows stronger for every class it has brought to its final level, so it rewards a character who masters classes instead of dabbling. Its skill draws on that mastery for a burst of power.

### Warden
6 levels · rarity blue
- **Requires:** Level 30 · Spellbreaker 3
- **Identity:** Keeps the enemy in check. Its seals stop an enemy from using its skills for some turns, and its wards put barriers on allies that soak up damage.

### Scout
5 levels · rarity no colour
- **Requires:** Level 5 · Archer 3 · Travel 300 km
- **Identity:** Ahead of the party. On the map it sees an encounter before it starts and can choose to fight, avoid or ambush it; in an ambush the party acts first. It is light and quick (Speed, Evasion, ranged Accuracy).

## Eight of my own (8)

Picked for what no class does yet, and kept clear of the classes tasks 11–14 add (the three racial classes, Spellweaver, Ritual Caster, Dungeon Delver, Throwing Specialist).

### Field Medic
5 levels · rarity no colour
- **Requires:** Level 10 · Use 20 healing items in battle
- **Identity:** Heals without magic. Its bandages and salves heal without Mana, it cures poisons and other afflictions, and healing items work better in its hands.

### Sentinel
6 levels · rarity no colour
- **Requires:** Level 25 · Soldier 3
- **Identity:** Watches and reacts. Instead of acting it can stand ready: the first enemy that moves before its next turn is struck, and the longer it waits, the harder it strikes.

### Beast Tamer
5 levels · rarity blue
- **Requires:** Level 15 · Beastmaster 3 · Defeat 50 wolves, bears and griffons
- **Identity:** Tames animals mid-battle. An animal enemy brought low (a wolf, a bear, a griffon) can be tamed to fight for the party until the battle ends, and its tamed animals grow stronger with its level.

### Sunderer
5 levels · rarity no colour
- **Requires:** Level 20 · Warrior 5
- **Identity:** Breaks armour. Its hits tear away part of the enemy's Armor for the rest of the battle, piling up with each hit, and it deals the most to heavily armoured foes.

### Camp Cook
4 levels · rarity no colour
- **Requires:** Level 10 · Forage 30 ingredients
- **Identity:** Cooks at camp. When the party rests it can cook a meal from foraged ingredients that gives everyone a buff through the next battles (more HP, Strength or Mana regeneration, depending on the meal).

### Leywalker
6 levels · rarity blue
- **Requires:** Level 30 · Mage 5 · Know spells of 4 elements
- **Identity:** Draws on the land. Its spells change with where the battle is fought (a forest strengthens Nature, a river Water, a mountain Earth, the coast Wind, a marsh Death), and its skill draws the place's power into one great spell.

### Nightwalker
5 levels · rarity blue
- **Requires:** Level 25 · Rogue 5 · Win 30 battles at night
- **Identity:** The night's own, the Sunstrider's opposite. After dark (18:00 to 06:00) its weapon hits and Dark spells grow stronger and enemies target it less; by day it is weaker.

### Mentor
4 levels · rarity no colour
- **Requires:** Level 40 · Strategist 3 · 2 companions
- **Identity:** Teaches its companions. They gain more EXP, the lowest-level ally fights as if a few levels higher, and in battle it can share a lesson that gives an ally its own Accuracy and Evasion bonuses.

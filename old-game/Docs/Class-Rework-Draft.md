# Task 10, Class Rework: the draft, every class's identity, levels, requirements and rarity

**For the user's review (written 2026-11-10).** The one draft of task 10 (C14): every class, the 130 in the game and the 40 new ones, with its identity, its number of levels, its requirements and its rarity (colour). Nothing is built yet. The decisions behind it are C1–C14 in `engine/Docs/Engine-Plan.md`, "Decisions for task 10, Class Rework"; the task is `engine/Docs/Port-Tasks/10_Class_Rework.txt`.

**How to read it**
- **Identity:** what the class is about and what it achieves, drawn from all of its features (its passives and skills included). Most identities of current classes are kept and only put into words. Where I propose to change one, it says **Sharpened** and why (almost always two classes doing the same thing).
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
- **Identity:** The first step on the melee path: Strength, heavy armour and melee Accuracy, at home in the thick of several enemies.
- Found: Both features are faint (5% Armor against two or more enemies; a once-a-day trade of Armor for Strength and Evasion).

### Archer
5 levels · rarity no colour
- **Requires:** Level 1
- **Identity:** The first step on the ranged path: Dexterity and bow Accuracy, at its best against a single target.
- Found: Single Target is small and shrinks with every enemy present; Sharpshoot only once a day.

### Mage
5 levels · rarity no colour
- **Requires:** Level 1
- **Identity:** The first step on the spell path: the first spells, cheaper low-tier magic and one big overloaded spell.
- Found: Mana Overload once a day.

### Rogue
5 levels · rarity no colour
- **Requires:** Level 1
- **Identity:** The first step on the rogue path: Speed, Evasion in light armour or clothing, and critical hits with one-handed weapons and bows.
- Found: Lucky Hit once a day for +15 Crit Rating is weak.

## Melee fighters (8)

Fighters who win with the weapon in their hand.

### Barbarian
7 levels · rarity no colour
- **Requires:** Level 1 · Warrior 5 · No Light or Heavy Armor equipped
- **Identity:** The unarmoured berserk: a rage that trades choices for damage and toughness, hammering the same foe again and again.
- Found: Breaks the capstone rule: Rage comes at level 2 and Seeing Red at 5; level 7 is only stats.

### Berserker
6 levels · rarity no colour
- **Requires:** Level 15 · Warrior 5 or Archer 5 · Kill at least 3 enemies in one combat 5 times
- **Identity:** Thrives in a crowd: the more enemies alive, the harder it hits, and Rampage sweeps them all.
- Found: Bonuses per living enemy are spread over many classes (Champion, Slayer, Traveler, Warrior); here they are the core.

### Champion
4 levels · rarity no colour
- **Requires:** Level 40 · Slayer 5 or Gladiator 5
- **Identity:** Sharpened: the one who takes on the strongest enemy: an all-round fighter that grows stronger against the toughest foe on the field (the boss, the enemy with the most HP).
- Found: Champion's Challenge is almost the Gladiator's Duel (one enemy, three turns, protected), and Arena Champion is the Berserker's per-enemy bonus. Sharpened so the duel stays the Gladiator's.

### Gladiator
7 levels · rarity no colour
- **Requires:** Level 15 · Warrior 3
- **Identity:** The duellist: challenges one enemy to single combat and is strongest when facing only one opponent.

### Reaver
7 levels · rarity blue
- **Requires:** Level 25 · Barbarian 5 or Berserker 6
- **Identity:** The finisher: hits wounded enemies harder and gets its next turn sooner after every kill.

### Slayer
7 levels · rarity no colour
- **Requires:** Level 1 · Warrior 5 · Win at least 30 combats with no living ally remaining
- **Identity:** The lone warrior: strongest with no companion beside it, outnumbered and still winning.

### Soldier
6 levels · rarity no colour
- **Requires:** Level 25 · Warrior 5 or Archer 5 or Rogue 5
- **Identity:** The disciplined veteran: steady Accuracy and toughness, growing stronger each turn the battle lasts.
- Found: Veteran's Momentum (+25% damage, −20% damage taken, +20% Armor and Evasion for 3 turns) is very strong for a class without a colour.

### Weaponmaster
7 levels · rarity no colour
- **Requires:** Level 20 · Warrior 5
- **Identity:** Master of every weapon: weapon power, Accuracy, critical hits and Armor Pierce with any weapon, scaling on Strength and Dexterity at once.

## Knights and defenders (6)

Armour, shields and standing between the enemy and the party.

### Knight
7 levels · rarity no colour
- **Requires:** Level 1 · Warrior 5
- **Identity:** The armoured knight: heavy armour, Strength, stronger melee scaling, a last stand when nearly dead and one all-or-nothing blow.

### Heavy Knight
5 levels · rarity no colour
- **Requires:** Level 1 · Knight 5 · Heavy Armor and a Shield equipped
- **Identity:** The taunting tank in heavy armour and shield: draws the enemy's attacks and hardens with every hit it takes.

### Guardian
7 levels · rarity no colour
- **Requires:** Level 1 · Knight 3 · Most maximum HP in the party
- **Identity:** The bodyguard: intercepts attacks aimed at its allies and takes them itself.

### Bulwark
7 levels · rarity no colour
- **Requires:** Level 20 · Knight 4 · 10 defensive actions (Guard or Evade)
- **Identity:** Sharpened: the immovable wall: the Guard action made its weapon, much stronger while Guarding and hard to break, rather than a second bodyguard.
- Found: Its skill Interpose shares its name and job with the Guardian's passive Interpose (and Aegis Bond). Sharpened so protecting allies stays the Guardian's.

### Echo Knight
7 levels · rarity blue
- **Requires:** Level 35 · Knight 7 · Summoner 4
- **Identity:** The knight with a spectral echo: its weapon hits repeat as ghostly echoes.

### Black Knight
5 levels · rarity no colour
- **Requires:** Level 1 · Knight 7 · At least 10 Dark spells known
- **Identity:** The dark knight: Dark magic woven into heavy armour and weapon, and an abyssal transformation.
- Found: The engine's tests read this class file: if it changes, the engine's test suite runs too.

## Holy warriors (5)

Warriors of the faith. Today their features overlap a lot (four of them get bonuses against demons, two share Divine Smite); each is sharpened to one job.

### Cleric
7 levels · rarity no colour
- **Requires:** Level 1 · Warrior 5 · 3 Holy spells known
- **Identity:** Sharpened: the battle priest: a warrior who keeps the party going with prayers in the fight (blessings, mending), not a demon fighter.
- Found: Breaks the capstone rule: the passive comes at level 4 and the skill at 5; levels 6 and 7 are stats. Holy Warrior's demon bonus repeats the Paladin's and Templar's.

### Paladin
7 levels · rarity no colour
- **Requires:** Level 10 · Knight 4 or Cleric 4 · No Dark or Blood spells known · Not together with Oathsworn
- **Identity:** Sharpened: the holy smiter: Divine Smite, Mana turned into Holy weapon damage, against demons and devils; it forswears Dark and Blood magic.
- Found: Sworn Oath works against Monster-class enemies and Banish Evil against demons; Oathsworn has the same Divine Smite and a stronger Sworn Oath.

### Death Paladin
6 levels · rarity no colour
- **Requires:** Level 1 · Paladin 7 · At least 10 Death spells known
- **Identity:** The paladin turned to Death: smites with Holy and Death together and weakens what it strikes.

### Oathsworn
7 levels · rarity purple
- **Requires:** Level 10 · Knight 4 · Hierophant 4 · Know no Dark or Blood spells · Not together with Paladin
- **Identity:** Sharpened: the knight bound by vows: swears an oath for the battle (to protect, to stand, to slay) and draws great power while it keeps it; Basic and Holy magic through the blade.
- Found: Today it is a stronger Paladin: Arcane Vow is Sworn Oath with bigger numbers, and its skill is Divine Smite again.

### Templar
6 levels · rarity no colour
- **Requires:** Level 1 · Cleric 7 or Priest 7
- **Identity:** Sharpened: the holy defender: armour and Holy power that shield the whole party, against the undead and dark magic.
- Found: Consecrated Arms (demons, devils, undead) repeats the demon bonus of the Cleric and Paladin.

## Unarmed fighters (4)

Fists, discipline and inner power.

### Brawler
6 levels · rarity no colour
- **Requires:** Level 1 · Use 5 unarmed attacks
- **Identity:** The tavern fighter: fists and rough melee, Strength and Vitality, and dirty extra damage.

### Monk
8 levels · rarity no colour
- **Requires:** Level 6 · 30 unarmed attacks used
- **Identity:** The disciplined unarmed fighter: Wisdom-powered fists, stunning blows and resistance to spells.

### Elder Monk
4 levels · rarity blue
- **Requires:** Level 45 · Monk 8
- **Identity:** The monk's mastery: much stronger fists, surer stuns and blows that heal.

### Ki Warrior
10 levels · rarity purple
- **Requires:** Level 30 · Energist 5 or Psykic 8
- **Identity:** Ki as a resource: gathers Ki through the battle and spends it to empower attacks and spells, restore Mana or go into overdrive.

## The archer's path (6)

Bows, marks and hunting one kind of prey.

### Hunter
7 levels · rarity no colour
- **Requires:** Level 1 · Archer 5
- **Identity:** The hunter of one quarry: bows, Evasion in light armour, and the Hunter's Mark the hunter classes build on.
- Found: Silent Killer is the same effect as the Assassin's Combatstalker (+25% against enemies that haven't acted).

### Sharpshooter
5 levels · rarity no colour
- **Requires:** Level 1 · Archer 5
- **Identity:** Precision with the bow: Accuracy and guaranteed critical shots.
- Found: Two skills and no passive.

### Blood Hunter
5 levels · rarity no colour
- **Requires:** Level 1 · Hunter 7 · At least 5 Blood spells known
- **Identity:** The hunter who uses Blood magic: hunts vampires and the undead and drains life with its attacks.

### Demon Hunter
6 levels · rarity no colour
- **Requires:** Level 1 · Hunter 7 · Defeat 40 devils, demons and impids
- **Identity:** The hunter of demons, devils and impids.

### Drakeslayer
6 levels · rarity no colour
- **Requires:** Level 1 · Hunter 7 · Defeat 3 dragons
- **Identity:** The hunter of dragons.
- Found: Dragons are few, so it is seldom useful.

### Monster Hunter
7 levels · rarity no colour
- **Requires:** Level 1 · Hunter 7 · At least 30 Monster-class enemies slain
- **Identity:** The hunter of Monster-class enemies.

## The rogue's path (10)

Speed, stealth, poisons, traps and the killing blow.

### Assassin
6 levels · rarity no colour
- **Requires:** Level 1 · Rogue 5 or Sharpshooter 3
- **Identity:** Sharpened: the killer of one target: critical hits and a guaranteed execution (the first-turn bonus is left to the Ambusher).
- Found: Breaks the capstone rule: Execute comes at level 5; level 6 is critical damage. Combatstalker is the Hunter's Silent Killer, and the Ambusher's First Strike is the same idea.

### Ambusher
7 levels · rarity no colour
- **Requires:** Level 1 · Rogue 5 · Speed is your highest stat
- **Identity:** Strikes first: Speed, and an opening blow far stronger than the rest.

### Fencer
6 levels · rarity no colour
- **Requires:** Level 18 · Rogue 5 · 7 one-handed attacks
- **Identity:** The one-handed swordsman with an empty off-hand: Accuracy, Evasion, ripostes and lunges.

### Poisoner
7 levels · rarity no colour
- **Requires:** Level 15 · Rogue 5
- **Identity:** Poisons its weapon: hits poison for a share of the target's maximum HP and weaken its healing.
- Found: Already in the game; reworked like the other current classes, not added again (C9).

### Shadow
6 levels · rarity no colour
- **Requires:** Level 18 · Rogue 5
- **Identity:** The unseen: Evasion, being targeted less, and vanishing when it must.

### Eminence
4 levels · rarity blue
- **Requires:** Level 1 · Shadow 6 · 7 flawless victories
- **Identity:** The untouched: deadly while it has taken no damage, weaker as it gets hurt.

### Stalker
4 levels · rarity no colour
- **Requires:** Level 12 · Rogue 5
- **Identity:** The tracker on the map: sees what can be hunted in a zone and forces an encounter with it.

### Thief
5 levels · rarity no colour
- **Requires:** Level 1 · Rogue 5
- **Identity:** The thief: more money from enemies, Evasion, and a chance to steal the enemy's weapon.

### Trap Layer
4 levels · rarity no colour
- **Requires:** Level 1 · Rogue 5 · 3 traps used
- **Identity:** Sets traps: stronger, armour-piercing traps that are sometimes kept and trigger more than once.
- Found: No passive and no skill, so nothing for a capstone.

### Trap Master
3 levels · rarity no colour
- **Requires:** Level 30 · Trap Layer 4
- **Identity:** The trap expert: even stronger traps, and two of them at once.
- Found: No passive and no skill, so nothing for a capstone.

## Weapons and spells together (5)

Hybrids that fight with a weapon and magic at once.

### Spellblade
5 levels · rarity no colour
- **Requires:** Level 1 · Warrior 3 · Mage 3 · 3 different spells known
- **Identity:** Warrior and mage: melee weapons that scale with Intelligence and blades charged with Mana.

### Spell Sniper
5 levels · rarity no colour
- **Requires:** Level 1 · Archer 3 · Mage 3 · 3 different spells known
- **Identity:** Archer and mage: bows that scale with Intelligence and shots charged with Mana.

### Hexblade
5 levels · rarity blue
- **Requires:** Level 1 · Spellblade 5 · Know 5 Dark spells
- **Identity:** The spellblade of curses: Dark magic, weapon hits that curse, and a hex mark.

### Nullblade
6 levels · rarity no colour
- **Requires:** Level 25 · Spellblade 5
- **Identity:** Cuts magic away: its weapon hits strip the enemy's magical buffs.
- Found: Null Strike (weaken the target's next spell) is the Spellbreaker's Mute Sigil, smaller; left to the Spellbreaker.

### Spellbreaker
6 levels · rarity blue
- **Requires:** Level 30 · Enlightened 5 or Spellblade 5
- **Identity:** The mage-killer: resists spells, makes them fizzle while Guarding, and silences the next one.

## The mage's path (14)

Spellcraft itself: learning, casting, Mana and the mind.

### Spellcaster
7 levels · rarity no colour
- **Requires:** Level 1 · Mage 5 · Intelligence higher than Wisdom
- **Identity:** The Intelligence caster: efficient casting, quickened spells, and Mana back for a turn of one pure spell.

### Wizard
7 levels · rarity no colour
- **Requires:** Level 1 · Mage 5 · Wisdom higher than Intelligence
- **Identity:** The Wisdom caster: doubled spells, and spell knowledge shared across the party.

### Sorcerer
5 levels · rarity no colour
- **Requires:** Level 30 · Mage 5 · Know 20 different spells
- **Identity:** Builds Sorcery Points with every spell, which protect it and fuel a blast.
- Found: Sorcery Blast (5 damage per point) is weak at the level it needs (30).

### Great Mage
7 levels · **rarity no colour → blue** (needs Level 50 and is already among the strongest casters; a colour fits it better than weakening it)
- **Requires:** Level 50 · Mage 5
- **Identity:** The archmage: more spell power, Mana and cheaper casting across the board, and Magnum Opus.
- Found: Very strong for a class without a colour (hence the rarity proposed).

### Scholar
5 levels · rarity no colour
- **Requires:** Level 1 · Mage 5 · Know at least 10 different spells
- **Identity:** The learner: cheaper spell learning, and random spells from the whole catalogue.

### Wise Man
5 levels · rarity no colour
- **Requires:** Level 1 · Scholar 5
- **Identity:** The party's teacher: cheaper spells for everyone, and one spell cast three times.

### Archivist
5 levels · rarity blue
- **Requires:** Level 20 · Scholar 5 · Know 20 spells
- **Identity:** Sharpened: records enemy spells: turns spells it has seen into scrolls to cast later (copying a spell on the spot is left to the Mirror Mage).
- Found: Open Ledger (copy the last spell at 70%) is the Mirror Mage's Spell Mirror (75%).

### Chronomancer
5 levels · rarity blue
- **Requires:** Level 45 · Wizard 7 · Know 10 Basic spells
- **Identity:** Time magic: its own turns come sooner and the enemy's later.

### Mirror Mage
4 levels · rarity blue
- **Requires:** Level 40 · Wizard 7
- **Identity:** The mirror: takes less spell damage, reflects part of it, and copies enemy spells.

### Dreamweaver
6 levels · rarity no colour
- **Requires:** Level 30 · Sorcerer 5
- **Identity:** Puts enemies to sleep and preys on them: stuns, and more damage to stunned foes.

### Element Siphon
5 levels · rarity blue
- **Requires:** Level 30 · Spellcaster 7 · Know spells of 6 elements
- **Identity:** Feeds on enemy magic: takes Mana from the elemental hits it suffers and turns their element back on the enemy.

### Sage
5 levels · rarity blue
- **Requires:** Level 1 · Spellcaster 7 or Wizard 7 · Maximum Mana above maximum HP
- **Identity:** The deep well of Mana: far more Mana, regeneration, and Mana for the party.

### Energist
5 levels · rarity no colour
- **Requires:** Level 20 · Maximum Mana is at least 70% of maximum HP
- **Identity:** Balances HP and Mana: regenerates both each turn and restores both at once.

### Psykic
8 levels · rarity purple
- **Requires:** Level 30 · Spellcaster 7 or Wizard 7 · Know at most 15 spells
- **Identity:** Mind over matter: its own mind-powered attacks paid with Mana (Telekinetic Burst, Psychic Lance, Mindstorm), for those who keep few spells.

## The twelve elemental mages (12)

One per element, all built the same way: an early skill (level 3), a passive (5) and a great skill (9). Levels 4, 6, 7 and 8 are the same in all twelve (10% cheaper spells, +10% Mana, +15% damage, 30% cheaper learning); they get a look in the features round.

### Aeromancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Wind spells
- **Identity:** Wind magic: Speed and Evasion for the party, and Wind spells that cost fewer Action Points.

### Aquamancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Water spells
- **Identity:** Water magic: restoring HP and Mana, efficient Water spells, and a Water spell cast twice.

### Botanomancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Nature spells
- **Identity:** Nature magic: healing over time and animal allies.
- Found: Call of the Ancient Pack is the Beastmaster's Call the Pack.

### Cryomancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Ice spells
- **Identity:** Ice magic: freezing (stunning) enemies and less damage taken.

### Electromancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Lightning spells
- **Identity:** Lightning magic: overcharged bursts, and damage to every enemy.

### Geomancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Earth spells
- **Identity:** Earth magic: stone defence, and heavy Earth strikes on one enemy.

### Hemomancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Blood spells
- **Identity:** Blood magic: HP turned into Mana, and blood spell power.

### Hierophant
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Basic spells
- **Identity:** Basic magic: cheaper, quicker Basic spells, cast again as echoes.

### Luxomancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Holy spells
- **Identity:** Holy magic: healing the party and warding off darkness.

### Necromancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Death spells
- **Identity:** Death magic: raising undead thralls.

### Pyromancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Fire spells
- **Identity:** Fire magic: raw fire damage, at the price of taking more.

### Umbramancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Dark spells
- **Identity:** Dark magic: hiding in shadow, and weakening every enemy.

## Masters of elements (4)

Classes built on several elements or one element's special side.

### Avatar
4 levels · **rarity no colour → blue** (needs four elemental mages at Level 5)
- **Requires:** Level 1 · At least 4 core elemental mancer classes at Level 5
- **Identity:** The master of the six core elements: all of them together, hitting each enemy's weakest resistance.

### Stormcaller
7 levels · rarity purple
- **Requires:** Level 30 · Aeromancer 5 · Electromancer 5
- **Identity:** Wind and Lightning in rhythm: alternating the two builds charges for a storm burst.

### Sunstrider
6 levels · rarity purple
- **Requires:** Level 25 · Luxomancer 5 · Priest 5
- **Identity:** The sun's Holy power: stronger by day.

### Plaguebinder
6 levels · rarity no colour
- **Requires:** Level 25 · Warlock 5 or Necromancer 5
- **Identity:** Spreads afflictions: poisons and curses that jump from the dead to the living, and a plague cloud over every enemy.

## Priests and healers (4)

Healing and protecting the party with magic.

### Priest
7 levels · rarity no colour
- **Requires:** Level 1 · Mage 5 · 3 Holy spells known
- **Identity:** The holy healer: Wisdom, Holy power, and a sanctuary for the party.

### Cardinal Priest
4 levels · **rarity no colour → blue** (needs Level 55 and Priest 7)
- **Requires:** Level 55 · Priest 7
- **Identity:** The high priest: far stronger Holy power, cheaper spells for the whole party, and a decree that shields and restores everyone.

### Lifewarden
6 levels · rarity no colour
- **Requires:** Level 30 · Priest 5 or Cleric 5
- **Identity:** The keeper of life: healing that turns its excess into barriers, and a heal for everyone.

### Sanguine Medic
5 levels · rarity blue
- **Requires:** Level 1 · Hemomancer 5 · Priest 5
- **Identity:** Heals with its own blood: spends HP to heal allies, and heals better when wounded.

## Summoners and dark pacts (9)

Creatures, pacts and rituals.

### Summoner
7 levels · rarity no colour
- **Requires:** Level 1 · Mage 5 · 10 summon spells used
- **Identity:** Summons fight for it: cheaper summons, stronger creatures, and it can't be attacked while one of them lives.
- Found: Overprotective (can't be attacked at all while a summon lives) is very strong.

### Beastmaster
7 levels · rarity no colour
- **Requires:** Level 1 · Summoner 3 · At least 1 Nature Summon spell known
- **Identity:** Animals at its side: an animal companion in every fight, and the pack on call.

### Wildwarden
5 levels · rarity no colour
- **Requires:** Level 1 · Beastmaster 5 · Know 1 Nature summon spell
- **Identity:** The pack's protector: animal summons take less damage and gain Armor.

### Soul Shepherd
5 levels · rarity blue
- **Requires:** Level 35 · Summoner 7
- **Identity:** The shepherd of summoned souls: stronger summons, and their deaths restore it.

### Druid
7 levels · rarity no colour
- **Requires:** Level 1 · Mage 5 · 3 Nature spells known
- **Identity:** Nature's own caster: one with the world, stronger Nature spells, and nature answering when it is near death.
- Found: Two passives and one skill.

### Warlock
7 levels · rarity no colour
- **Requires:** Level 1 · Mage 5 · 3 Dark spells known
- **Identity:** The Dark caster: power from a pact with darkness.

### Pactkeeper
4 levels · rarity blue
- **Requires:** Level 30 · Warlock 7 · Summoner 5 · Cast the same summon 10 times
- **Identity:** Bound to one familiar: a chosen summon that returns every fight and grows with each battle it survives.

### Cultist
7 levels · rarity no colour
- **Requires:** Level 1 · Use 10 Dark-Element summon spells
- **Identity:** Dark summoning rites: cheaper summons, made stronger by ritual.
- Found: Breaks the capstone rule: Summon Ritual comes at level 3 and Dark Rituals at 6; level 7 is stats.

### Chaos Cultist
3 levels · **rarity no colour → blue** (needs Level 50, Cultist 7 and 100 Dark summons)
- **Requires:** Level 50 · Cultist 7 · Use 100 Dark-Element summon spells
- **Identity:** The cult's high caller: a demon at the start of every fight, and a grand summoning.

## Leaders and minds (5)

Commanding the party, and seeing what the enemy will do.

### Strategist
7 levels · rarity no colour
- **Requires:** Level 20 · 15 defensive actions by the whole party
- **Identity:** Commands the party: Speed, Dexterity and Luck for everyone, stronger defensive actions, and Focus Fire.

### General
5 levels · rarity blue
- **Requires:** Level 50 · Strategist 7
- **Identity:** The commander: Focus Fire turned into tempo for the party, and a line that holds.

### Flagbearer
4 levels · rarity blue
- **Requires:** Level 1 · Strategist 5 · 2 companions
- **Identity:** Carries the colours: strengthens the companions, and raises a standard the enemy must break.

### Enlightened
5 levels · rarity no colour
- **Requires:** Level 10 · 25 defensive actions (Guard or Evade)
- **Identity:** Insight in defence: stronger Guard and Evade, and reading an enemy's intent.

### Diviner
7 levels · rarity blue
- **Requires:** Level 35 · Enlightened 5
- **Identity:** Foresight: foretells an enemy's next action for great protection.

## Luck (4)

Fortune, dice and wagers.

### Bard
5 levels · rarity no colour
- **Requires:** Level 1 · At least 50 Luck
- **Identity:** The lucky performer: Luck and Mind, cheaper spells, and a heroic ballad for the party.

### Jester
5 levels · rarity no colour
- **Requires:** Level 1 · 120 Luck
- **Identity:** The fool fortune loves: more masterwork drops, Evasion, and a die roll for a sure critical hit.
- Found: Level 4 has no effect at all.

### Joker
3 levels · **rarity no colour → blue** (needs 220 Luck and Jack of all Trades 4 or Jester 5)
- **Requires:** Level 1 · Jack of all Trades 4 or Jester 5 · 220 Luck
- **Identity:** Pure chaos: random damage boosts from enormous Luck, and a reckless spree of cheap spells.

### Gambit
5 levels · rarity blue
- **Requires:** Level 20 · Jester 3
- **Identity:** The gambler: wagers Action Points on strikes.

## The road, trade and the guild (12)

Travel, money, supplies, quests, artifacts and alchemy.

### Traveler
3 levels · rarity no colour
- **Requires:** Level 10 · 600 km traveled
- **Identity:** The road: faster travel and camping supplies that last.
- Found: The Road Ahead (Speed per living enemy in battle) has nothing to do with travelling.

### Cartographer
5 levels · rarity no colour
- **Requires:** Level 1 · Traveler 3 · Travel 1,500 km
- **Identity:** Maps the land: faster travel, fewer encounters, and more shown on the map.
- Found: Surveyor's Eye predates the one continent of task 8; checked in the features round.

### Courier
3 levels · rarity no colour
- **Requires:** Level 1 · Traveler 3 · Complete 15 guild Travel quests
- **Identity:** Delivers: faster travel, and protection while carrying the guild's deliveries.

### Pilgrim
5 levels · rarity no colour
- **Requires:** Level 1 · Priest 4 · Traveler 3
- **Identity:** The holy traveller: blessings earned on the road, and a prayer for the party.

### Riverstrider
5 levels · rarity no colour
- **Requires:** Level 18 · Traveler 3 or Aquamancer 3
- **Identity:** At home on rivers: faster river crossings, Evasion in river zones, Water magic.
- Found: Narrow: most of it works only in river zones.

### Salesman
4 levels · rarity no colour
- **Requires:** Level 1 · Sell at least 200 items
- **Identity:** Money: sells dearer, and silver in the purse turns into power.

### Scavenger
**7 → 5 levels** · rarity no colour
- **Requires:** Level 1 · Loot 30 enemies
- **Identity:** Picks the battlefield clean: more money, more masterworks, and camping supplies.
- Found: Seven levels, three of them the same +5% money and two the same +1 masterwork chance; five levels keep every kind of feature.

### Quartermaster
3 levels · rarity no colour
- **Requires:** Level 20 · 2 companions
- **Identity:** Keeps the party supplied: consumables that last longer and heal more.

### Bounty Broker
4 levels · rarity no colour
- **Requires:** Level 1 · Hunter 4 or Salesman 2 · Complete 5 guild quests
- **Identity:** The guild's contractor: better rewards, more quests, and marks on bounty targets.

### Relic Warden
5 levels · rarity no colour
- **Requires:** Level 25 · 2 artifacts equipped
- **Identity:** The keeper of artifacts: stronger artifacts, and their power shared with the party.

### Alchemist
5 levels · rarity no colour
- **Requires:** Level 20 · 10 potions brewed
- **Identity:** The brewer (task 9): finds more when foraging, and knows every ingredient's uses.
- Found: New with task 9.

### Assimilator
7 levels · rarity no colour
- **Requires:** Level 10 · 25 ingredients foraged
- **Identity:** Eats ingredients in battle for buffs (task 9).
- Found: New with task 9.

## Race classes (8)

Classes for one race. Beast, Great Beast and Monster belong to races players can't choose (enemies use them).

### Noble
3 levels · rarity no colour
- **Requires:** Level 30 · Race: Human
- **Identity:** The human noble: money and reputation, and inspiring the party.

### Reveler
3 levels · rarity no colour
- **Requires:** Level 30 · Race: Hobbit
- **Identity:** The hobbit's good company: Dexterity, Speed and Luck for everyone, and a feast.

### Stonebreaker
3 levels · rarity no colour
- **Requires:** Level 30 · Race: Dwarf
- **Identity:** The dwarf's iron body: can't be stunned, stronger heavy armour, shattering blows.

### Sylvan
3 levels · rarity no colour
- **Requires:** Level 30 · Race: Elf or Dark Elf
- **Identity:** The elf's forest soul: Mana and its renewal.

### Dragonblood
3 levels · rarity no colour
- **Requires:** Level 30 · Race: Lizardman
- **Identity:** The lizardman's dragon blood: a dragon's body, and all stats awakened.

### Beast
3 levels · rarity no colour
- **Requires:** Level 1 · Non-playable race
- **Identity:** For beast-race characters: natural claws, the body of a beast, and the four basic prerequisites filled.
- Found: No passive and no skill, so nothing for a capstone. Not playable.

### Great Beast
10 levels · rarity no colour
- **Requires:** Level 1 · Beast 3 · Non-playable race
- **Identity:** The beast grown great: stronger claws and body, terrifying weaker foes, a mighty charge.
- Found: Not playable.

### Monster
3 levels · rarity no colour
- **Requires:** Level 1 · Non-playable race
- **Identity:** For monster-race characters: a monster's body, and the four basic prerequisites filled.
- Found: No passive and no skill, so nothing for a capstone. Not playable.

## Classes from rare items (6)

Unlocked by an item that is used up (a vial, a core, a crystal, a gem).

### Dragonkin
7 levels · **rarity no colour → blue** (needs a Vial of Dragonblood (0.5% from dragons))
- **Requires:** Level 1 · Vial of Dragonblood (consumed on acquisition)
- **Identity:** Has drunk a dragon's blood: draconic flight and a dragon's heart.

### Lich
5 levels · rarity purple
- **Requires:** Level 1 · Phylactery Core (consumed on acquisition)
- **Identity:** Undying: damage drains Mana instead of killing it, Death magic, and an undead army.

### Madman
3 levels · **rarity no colour → purple** (needs a Shadow Crystal (0.05% from any enemy))
- **Requires:** Level 1 · Shadow Crystal (consumed on acquisition)
- **Identity:** Sees without the veil: no Armor or Evasion, but huge HP and damage, and a mad revelation.

### Sharmat
3 levels · **rarity no colour → purple** (needs a Gem of False Dreams, Ur-Mat's drop at Level 93)
- **Requires:** Level 1 · Gem of False Dreams (consumed on acquisition)
- **Identity:** The false dreamer's gem: a dream state, and a lucid change that restores everything at a lasting price.

### Vampire
4 levels · **rarity no colour → blue** (needs a Blood Crystal, a boss's drop (Tankina))
- **Requires:** Level 1 · Blood Crystal (consumed on acquisition)
- **Identity:** Feeds on blood: drains enemies to store Blood Supplies that heal it.
- Found: Breaks the capstone rule: Bloodfeed comes at level 1 and Heart of Crimson at 2; levels 3 and 4 are stats and cheaper Blood spells.

### Vampire Lord
3 levels · rarity no colour
- **Requires:** Level 40 · Vampire 4
- **Identity:** The vampire's lord: more feeding, and a blood frenzy.

## Legends and the rest (4)

### Hero
3 levels · rarity gold
- **Requires:** Level 92 · Not together with Demon Lord
- **Identity:** The legend who saves the world: inspires the party, restores everyone each turn, and lands a strike nothing can lessen.

### Demon Lord
3 levels · rarity gold
- **Requires:** Level 92 · Not together with Hero
- **Identity:** The legend who ends it: terror over every enemy, and a world-ending strike.

### Raid Boss
1 level · rarity no colour
- **Requires:** Level 1 · Unobtainable
- **Identity:** Enemy only: the raid bosses' extra strength.
- Found: Can't be acquired by players.

### Jack of all Trades
4 levels · rarity no colour
- **Requires:** Level 1 · Warrior 3 · Archer 3 · Mage 3 · Rogue 3
- **Identity:** A bit of everything: rewards for every class at Level 3, and the four basic prerequisites filled.
- Found: Breaks the capstone rule: its passive comes at level 2; level 4 fills the prerequisites.

# New classes

## The task's list (25)

The 26 names in the task, but Poisoner, which is already in the game (C9); "Ascendend" is Ascended (C10).

### Acolyte
5 levels · rarity no colour
- **Requires:** Level 1 · Know 1 Holy spell
- **Identity:** A novice of the faith: small prayers that bless and mend allies; each prayer in a battle builds devotion that makes the next one stronger.

### Bruiser
6 levels · rarity no colour
- **Requires:** Level 15 · Warrior 5 · Win 10 battles in which you fell below half HP
- **Identity:** Takes hits to hit back: every blow it takes builds Grit, which its next attacks spend for extra damage; a Strength and Vitality front-liner with any weapon.

### Spell Sparkler
5 levels · rarity no colour
- **Requires:** Level 10 · Mage 5 · Cast 3 spells in one turn, 10 times
- **Identity:** Showers of small spells: low-tier spells cost less, and each spell cast in a row in the same turn sparkles brighter (more power), so a turn of many cheap spells beats one big one.

### Hermit
5 levels · rarity blue
- **Requires:** Level 25 · Spend 10 days in a row without entering a town
- **Identity:** Strength from solitude: grows stronger and wiser the longer it stays in the wild away from towns, fights best in a small party, and restores itself without help.

### Blade Dancer
7 levels · rarity blue
- **Requires:** Level 20 · Rogue 5 · Bard 3 · Land 5 weapon hits in a row in one battle, 10 times
- **Identity:** A dance with blades: each hit in a row raises its Evasion and Speed, and a miss breaks the rhythm; at its best with light or paired blades.

### Hotshot
6 levels · rarity no colour
- **Requires:** Level 15 · Archer 5 · Make 3 ranged attacks in one turn, 20 times
- **Identity:** Fast and flashy with ranged weapons: quicker, cheaper shots at less power each, rewarded for spreading them over several enemies.

### Juggernaut
6 levels · rarity blue
- **Requires:** Level 30 · Knight 5 · Heavy Armor equipped
- **Identity:** Heavy armour as a weapon: its Armor adds to its damage, it can't be stunned or slowed, and it charges through the enemy line.

### Rearguard
5 levels · rarity no colour
- **Requires:** Level 15 · Warrior 5 · Win 20 battles in which no companion fell
- **Identity:** Covers the back of the party: protects the weaker allies behind it, punishes enemies that attack them, and makes a retreat (Run) safe.

### Vanguard
5 levels · rarity no colour
- **Requires:** Level 15 · Warrior 5 · Take the first turn of a battle 25 times
- **Identity:** The front of the charge: strongest in the opening turns, and its first strikes bring the party's turns forward.

### Caravanist
4 levels · rarity no colour
- **Requires:** Level 20 · Traveler 3 · Travel by caravan 5 times
- **Identity:** A merchant of the road (task 8's caravans): cheaper caravan fares, better prices, and supplies for the party from its wagon.

### Acrobat
6 levels · rarity no colour
- **Requires:** Level 10 · Rogue 5 · Dexterity is your highest stat
- **Identity:** Dodges everything: a stronger, cheaper Evade, and every dodge builds momentum for its next move; Dexterity and Speed.

### Explorer
5 levels · rarity no colour
- **Requires:** Level 15 · Traveler 3 · Enter 8 regions
- **Identity:** Goes where no one has been: rewards for entering new regions and meeting new kinds of enemy, more finds when foraging, and hidden caches on the map.

### Smith
5 levels · rarity no colour
- **Requires:** Level 10 · Reforge 5 items
- **Identity:** The craftsman in the field: cheaper forge work (reforging, fitting runes), and before a battle it whets weapons and mends armour for short boosts.

### Mastersmith
4 levels · rarity purple
- **Requires:** Level 45 · Smith 5 · Reforge 25 items
- **Identity:** The Smith perfected: makes forged gear better than the forges sell it: stronger parts, more rune slots, larger bonuses for matching makers.

### Tinkerer
5 levels · rarity no colour
- **Requires:** Level 15 · Trap Layer 3
- **Identity:** Builds gadgets in battle: small clockwork devices that act on their own (a turret, a decoy, a healing pump), and better traps.

### Informant
4 levels · rarity no colour
- **Requires:** Level 20 · Rogue 5 · Buy information from the hooded man 3 times
- **Identity:** Knows everything about the enemy: reveals an enemy's stats, resistances and drops, exploits what it has revealed, and buys information from the hooded man for less.

### Infiltrator
5 levels · rarity blue
- **Requires:** Level 20 · Rogue 5 · Shadow 3
- **Identity:** Gets past the line: ignores taunts, guards and interceptions, strikes the enemy's back row, and spoils an enemy's next action.

### Rune Knight
6 levels · rarity blue
- **Requires:** Level 25 · Knight 5 · Spellblade 3 · Fit 10 runes
- **Identity:** Wakes the runes in its gear: slotted runes grow stronger, and it can invoke them in battle for a burst of their power.

### Martial Artist
7 levels · rarity blue
- **Requires:** Level 20 · Monk 4 · Brawler 3
- **Identity:** Fighting stances: changes stance each turn (strike, guard, counter), each one shaping its next attacks; unarmed or with light weapons.

### Reaper
6 levels · rarity purple
- **Requires:** Level 40 · Reaver 5 · Know 5 Death spells
- **Identity:** Harvests souls: every enemy that falls gives it a soul, spent on Death-touched strikes or to heal; enemies near death are easier to reap.

### Ascended
4 levels · rarity gold
- **Requires:** Level 85 · Awakened 5
- **Identity:** A mortal grown beyond itself: all its powers rise with its character level, and for a few turns it ascends, partly beyond harm. The step after Awakened.

### Titan
5 levels · rarity purple
- **Requires:** Level 50 · Strength is your highest stat · Defeat 30 giants
- **Identity:** Colossal: huge maximum HP and Strength, slow but crushing blows that hit several enemies at once.

### Seeker
5 levels · rarity no colour
- **Requires:** Level 20 · Hunter 3 · Complete 5 Search quests
- **Identity:** Never loses its target: after a miss its next attack can't miss, evasive enemies lose their edge against it, and it finds what is sought (the guild's Search quests).

### Cycle
4 levels · rarity blue
- **Requires:** Level 40 · In one battle, Attack, Guard, use a healing item and cast a spell; 10 times
- **Identity:** Lives in a cycle: every turn moves it to the next of four phases (strike, guard, mend, focus), each with its own bonus; a full cycle releases a burst.

### Unreality
5 levels · rarity purple
- **Requires:** Level 60 · Dreamweaver 5
- **Identity:** Bends what is real: illusions of itself take hits meant for it, enemy attacks may strike nothing, and its own spells may turn out stronger or weaker than cast.

## The seven from classes/newc/ (7)

Names only; everything about them proposed fresh (C4, C7).

### Chronicler
5 levels · rarity blue
- **Requires:** Level 20 · Scholar 3 · Defeat 50 different kinds of enemy
- **Identity:** Keeps the record of every battle: the more of an enemy kind it has fought, the better it fights them (its own bestiary), and more EXP for deeds not yet recorded.

### Shaman
6 levels · rarity no colour
- **Requires:** Level 10 · Mage 5 · Know 2 Nature and 2 Wind spells
- **Identity:** Speaks with spirits: raises totems on the battlefield that help the party each turn (mending, warding, empowering) until the enemy breaks them.

### Elder Shaman
4 levels · rarity blue
- **Requires:** Level 45 · Shaman 6
- **Identity:** The Shaman's elder: several totems at once, stronger spirits, and the ancestors called to fight.

### Trickster
5 levels · rarity no colour
- **Requires:** Level 10 · Rogue 5 · At least 60 Luck
- **Identity:** Tricks and misdirection: feints that waste an enemy's turn, attacks turned onto the enemy's own allies, buffs and debuffs swapped.

### Awakened
5 levels · rarity purple
- **Requires:** Level 50 · 4 classes at their final level
- **Identity:** Mastery awakened: grows stronger for every class it has brought to its final level.

### Warden
6 levels · rarity blue
- **Requires:** Level 30 · Spellbreaker 3
- **Identity:** Keeps the enemy in check: seals that stop an enemy's skills, and wards (barriers) on its allies.

### Scout
5 levels · rarity no colour
- **Requires:** Level 5 · Archer 3 · Travel 300 km
- **Identity:** Ahead of the party: sees an encounter before it starts (fight, avoid or ambush), the party acts first when it ambushes; light and quick.

## Eight of my own (8)

Picked for what no class does yet, and kept clear of the classes tasks 11–14 add (the three racial classes, Spellweaver, Ritual Caster, Dungeon Delver, Throwing Specialist).

### Field Medic
5 levels · rarity no colour
- **Requires:** Level 10 · Use 20 healing items in battle
- **Identity:** Heals without magic: bandages and salves that cost no Mana, cures poisons and other afflictions, and makes healing items stronger.

### Sentinel
6 levels · rarity no colour
- **Requires:** Level 25 · Soldier 3
- **Identity:** Watches and reacts: readies an attack instead of acting, and strikes the first enemy that moves; the longer it waits, the harder it strikes.

### Beast Tamer
5 levels · rarity blue
- **Requires:** Level 15 · Beastmaster 3 · Defeat 50 wolves, bears and griffons
- **Identity:** Tames animals mid-battle: an animal enemy brought low (a wolf, a bear, a griffon) can be tamed to fight for the party until the battle ends.

### Sunderer
5 levels · rarity no colour
- **Requires:** Level 20 · Warrior 5
- **Identity:** Breaks armour: its hits tear away the enemy's Armor for the rest of the battle, and hit hardest on heavily armoured foes.

### Camp Cook
4 levels · rarity no colour
- **Requires:** Level 10 · Forage 30 ingredients
- **Identity:** Cooks at camp: meals from foraged ingredients give the party buffs that last through the next battles.

### Leywalker
6 levels · rarity blue
- **Requires:** Level 30 · Mage 5 · Know spells of 4 elements
- **Identity:** Draws on the land: its spells change with where it fights (forest, river, mountain, coast, marsh), each place lending its own power.

### Nightwalker
5 levels · rarity blue
- **Requires:** Level 25 · Rogue 5 · Win 30 battles at night
- **Identity:** The night's own, the Sunstrider's opposite: after dark its weapon hits and Dark spells grow stronger and it is harder to see.

### Mentor
4 levels · rarity no colour
- **Requires:** Level 40 · Strategist 3 · 2 companions
- **Identity:** Teaches its companions: they gain more EXP, the lowest-level ally fights above its level, and lessons are shared in battle.

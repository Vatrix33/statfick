# Task 10, Class Rework: the draft, every class's identity, levels, requirements and rarity

**For the user's review (written 2026-11-10; the user's review notes applied the same day).** The one draft of task 10 (C14): every class, the 130 in the game and the 40 new ones, with its identity, its number of levels, its requirements and its rarity (colour). Nothing is built yet. The decisions behind it are C1–C19 in `engine/Docs/Engine-Plan.md`, "Decisions for task 10, Class Rework"; the task is `engine/Docs/Port-Tasks/10_Class_Rework.txt`.

**How to read it**
- **Identity:** a short description of the class's playstyle, without its skills or passives (they can change) and without specific stat gains. For a current class it is kept, only put into words; where I propose to change one it says **Sharpened** and why (almost always two classes doing the same thing). For a new class it is my proposal.
- **Levels:** kept unless an arrow shows the change.
- **Requirements:** every current class keeps its own (the task: change them only when absolutely necessary; none is). The new classes' are proposed, in the same kinds the game already uses where it can; a few need a new count the game would keep (days away from towns, caravan trips, runes fitted and the like).
- **Rarity** (no colour, blue, purple, gold, and red above gold, for Unreality alone): kept unless an arrow shows the change, with the reason. A rarer class gets a stronger power level when its features are made.
- **Your notes** from the review of 2026-11-10 are applied; each changed class shows its note, and the three questions they raised were asked (C16–C19 in `engine/Docs/Engine-Plan.md`).
- **Found:** what I noticed while mapping a current class (overlaps, the capstone rule, power), for the features round; nothing there is decided. **The capstone rule** (the task): a class's last level must give its passive or its skill; ten classes break it today, and they are marked.
- **New classes:** the task's names, the seven kept from `old-game/classes/newc/`, and eight of my own.
- **How to answer:** approve as a whole, or name the classes to change and how (in chat, or in this file).

**Totals:** 130 current classes (1 with a change of levels: Scavenger 7 → 5; 8 identities sharpened: Champion, Bulwark, Cleric, Paladin, Oathsworn, Templar, Assassin, Archivist; 1 renamed: Element Siphon → Elemental Siphon; 1 remade: Poisoner; 15 rarities raised: Avatar → purple, Cardinal Priest → blue, Great Mage → blue, Chaos Cultist → blue, Joker → purple, Dragonkin → blue, Vampire → blue, Madman → gold, Sharmat → gold, Death Paladin → blue, Nullblade → blue, Plaguebinder → blue, Jester → blue, Vampire Lord → purple, Jack of all Trades → blue), 40 new classes, 170 in all. Rarity across all of them: 109 without a colour, 42 blue, 13 purple, 5 gold, 1 red.

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
- **Identity:** The starting melee fighter: wades into the fight in heavy armour and trades blows up close.
- Found: Both features are faint (5% Armor against two or more enemies; a once-a-day trade of Armor for Strength and Evasion).

### Archer
5 levels · rarity no colour
- **Requires:** Level 1
- **Identity:** The starting ranged fighter: picks enemies off from a distance with a bow, best when focusing on one target.
- Found: Single Target is small and shrinks with every enemy present; Sharpshoot only once a day.

### Mage
5 levels · rarity no colour
- **Requires:** Level 1
- **Identity:** The starting caster: fights with spells, and leads into every magic path.
- Found: Mana Overload once a day.

### Rogue
5 levels · rarity no colour
- **Requires:** Level 1
- **Identity:** The starting rogue: quick and light, relying on dodging and critical hits with light weapons and bows.
- Found: Lucky Hit once a day for +15 Crit Rating is weak.

## Melee fighters (8)

Fighters who win with the weapon in their hand.

### Barbarian
7 levels · rarity no colour
- **Requires:** Level 1 · Warrior 5 · No Light or Heavy Armor equipped
- **Identity:** Fights without real armour and wins by raging: reckless, hard-hitting and hard to bring down, hammering one foe after another.
- Found: Breaks the capstone rule: Rage comes at level 2 and Seeing Red at 5; level 7 is only stats.

### Berserker
6 levels · rarity no colour
- **Requires:** Level 15 · Warrior 5 or Archer 5 · Kill at least 3 enemies in one combat 5 times
- **Identity:** Wants to be surrounded: the more enemies on the field, the harder it hits, cutting through whole groups.
- Found: Bonuses per living enemy are spread over many classes (Champion, Slayer, Traveler, Warrior); here they are the core.

### Champion
4 levels · rarity no colour
- **Requires:** Level 40 · Slayer 5 or Gladiator 5
- **Identity:** **Sharpened.** Seeks out the strongest enemy on the field and grows more dangerous the tougher its opponent; a fighter for boss battles.
- Found: Champion's Challenge is almost the Gladiator's Duel (one enemy, three turns, protected), and Arena Champion is the Berserker's per-enemy bonus. Sharpened so the duel stays the Gladiator's.

### Gladiator
7 levels · rarity no colour
- **Requires:** Level 15 · Warrior 3
- **Identity:** The duellist: pins one enemy down in single combat and is at its best one-on-one.

### Reaver
7 levels · rarity blue
- **Requires:** Level 25 · Barbarian 5 or Berserker 6
- **Identity:** The finisher: picks off wounded enemies and carries its momentum from kill to kill.

### Slayer
7 levels · rarity no colour
- **Requires:** Level 1 · Warrior 5 · Win at least 30 combats with no living ally remaining
- **Identity:** The lone warrior: fights best with no companions beside it, outnumbered and still winning.

### Soldier
6 levels · rarity no colour
- **Requires:** Level 25 · Warrior 5 or Archer 5 or Rogue 5
- **Identity:** The steady veteran: reliable rather than flashy, and stronger the longer a battle lasts.
- Found: Veteran's Momentum (+25% damage, −20% damage taken, +20% Armor and Evasion for 3 turns) is very strong for a class without a colour.

### Weaponmaster
7 levels · rarity no colour
- **Requires:** Level 20 · Warrior 5
- **Identity:** Master of every weapon: gets the most out of whatever it holds, with power and precision.

## Knights and defenders (6)

Armour, shields and standing between the enemy and the party.

### Knight
7 levels · rarity no colour
- **Requires:** Level 1 · Warrior 5
- **Identity:** The armoured knight: a durable front-liner in heavy armour that hits hard, refuses to fall, and can stake everything on one blow.

### Heavy Knight
5 levels · rarity no colour
- **Requires:** Level 1 · Knight 5 · Heavy Armor and a Shield equipped
- **Identity:** The tank: draws the enemy's attacks onto itself and gets harder to hurt the more it is hit.

### Guardian
7 levels · rarity no colour
- **Requires:** Level 1 · Knight 3 · Most maximum HP in the party
- **Identity:** The bodyguard: steps in front of attacks meant for its allies and takes them itself.

### Bulwark
7 levels · rarity no colour
- **Requires:** Level 20 · Knight 4 · 10 defensive actions (Guard or Evade)
- **Identity:** **Sharpened.** The immovable wall: makes Guarding its strength and holds the line, rather than covering one ally.
- Found: Its skill Interpose shares its name and job with the Guardian's passive Interpose (and Aegis Bond). Sharpened so protecting allies stays the Guardian's.

### Echo Knight
7 levels · rarity blue
- **Requires:** Level 35 · Knight 7 · Summoner 4
- **Identity:** A knight with a spectral double: its strikes echo, landing again as ghostly blows.

### Black Knight
5 levels · rarity no colour
- **Requires:** Level 1 · Knight 7 · At least 10 Dark spells known
- **Identity:** The dark knight: heavy armour and weapon fused with Dark magic, able to turn into something monstrous for a short time.
- Found: The engine's tests read this class file: if it changes, the engine's test suite runs too.

## Holy warriors (5)

Warriors of the faith. Today their features overlap a lot (four of them get bonuses against demons, two share Divine Smite); each is sharpened to one job.

### Cleric
7 levels · rarity no colour
- **Requires:** Level 1 · Warrior 5 · 3 Holy spells known
- **Identity:** **Sharpened.** The battle priest: fights in the front line while keeping the party going with prayers.
- Found: Breaks the capstone rule: the passive comes at level 4 and the skill at 5; levels 6 and 7 are stats. Holy Warrior's demon bonus repeats the Paladin's and Templar's.

### Paladin
7 levels · rarity no colour
- **Requires:** Level 10 · Knight 4 or Cleric 4 · No Dark or Blood spells known · Not together with Oathsworn
- **Identity:** **Sharpened.** The holy smiter: turns its Mana into holy strikes and hunts demons and devils, forswearing dark and blood magic.
- Found: Sworn Oath works against Monster-class enemies and Banish Evil against demons; Oathsworn has the same Divine Smite and a stronger Sworn Oath.

### Death Paladin
6 levels · **rarity no colour → blue** (your note)
- **Requires:** Level 1 · Paladin 7 · At least 10 Death spells known
- **Identity:** A paladin turned to Death: smites with holy and death power together and weakens what it strikes.
- *Your note, applied: This class can have the blue rarity*

### Oathsworn
7 levels · rarity purple
- **Requires:** Level 10 · Knight 4 · Hierophant 4 · Know no Dark or Blood spells · Not together with Paladin
- **Identity:** **Sharpened.** A knight bound by vows: swears an oath each battle and draws great power while it keeps it.
- Found: Today it is a stronger Paladin: Arcane Vow is Sworn Oath with bigger numbers, and its skill is Divine Smite again.

### Templar
6 levels · rarity no colour
- **Requires:** Level 1 · Cleric 7 or Priest 7
- **Identity:** **Sharpened.** The holy defender: shields the whole party and stands against the undead and dark magic.
- Found: Consecrated Arms (demons, devils, undead) repeats the demon bonus of the Cleric and Paladin.

## Unarmed fighters (4)

Fists, discipline and inner power.

### Brawler
6 levels · rarity no colour
- **Requires:** Level 1 · Use 5 unarmed attacks
- **Identity:** The tavern fighter: fists and dirty tricks, scrappy and tough.

### Monk
8 levels · rarity no colour
- **Requires:** Level 6 · 30 unarmed attacks used
- **Identity:** The disciplined unarmed fighter: precise, stunning blows and a mind that resists magic.

### Elder Monk
4 levels · rarity blue
- **Requires:** Level 45 · Monk 8
- **Identity:** The monk's mastery: devastating fists that stun and heal.

### Ki Warrior
10 levels · rarity purple
- **Requires:** Level 30 · Energist 5 or Psykic 8
- **Identity:** Runs on Ki, an inner energy gathered during battle and spent on bursts of power; suits blade and spell alike.

## The archer's path (6)

Bows, marks and hunting one kind of prey.

### Hunter
7 levels · rarity no colour
- **Requires:** Level 1 · Archer 5
- **Identity:** The hunter: marks one quarry and brings it down with the bow, striking hardest before it can react.
- Found: Silent Killer is the same effect as the Assassin's Combatstalker (+25% against enemies that haven't acted).

### Sharpshooter
5 levels · rarity no colour
- **Requires:** Level 1 · Archer 5
- **Identity:** The precision archer: few shots, each one sure to land and likely to be critical.
- Found: Two skills and no passive.

### Blood Hunter
5 levels · rarity no colour
- **Requires:** Level 1 · Hunter 7 · At least 5 Blood spells known
- **Identity:** A hunter armed with blood magic: hunts vampires and the undead and drains life from its prey.

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
- **Identity:** **Sharpened.** The killer of one target: everything goes into a single lethal, critical strike.
- Found: Breaks the capstone rule: Execute comes at level 5; level 6 is critical damage. Combatstalker is the Hunter's Silent Killer, and the Ambusher's First Strike is the same idea.

### Ambusher
7 levels · rarity no colour
- **Requires:** Level 1 · Rogue 5 · Speed is your highest stat
- **Identity:** Strikes first: fast, with an opening blow far stronger than the rest.

### Fencer
6 levels · rarity no colour
- **Requires:** Level 18 · Rogue 5 · 7 one-handed attacks
- **Identity:** The duelling swordsman with a free off-hand: precise, evasive, and punishing every miss with a riposte.

### Poisoner
7 levels · rarity no colour
- **Requires:** Level 15 · Rogue 5
- **Identity:** **Remade.** The master of the game's poisons (task 9): throws them and coats its weapons with them, and every poison works stronger and longer in its hands.
- Found: Already in the game; reworked like the other current classes, not added again (C9).
- *Your note, applied: Since Poisons have been added to the game, this class should be remade into using them*

### Shadow
6 levels · rarity no colour
- **Requires:** Level 18 · Rogue 5
- **Identity:** The one the enemy overlooks: avoids being targeted and slips out of sight when it must.

### Eminence
4 levels · rarity blue
- **Requires:** Level 1 · Shadow 6 · 7 flawless victories
- **Identity:** The untouched: deadly as long as it isn't hit, so it plays to avoid damage entirely.

### Stalker
4 levels · rarity no colour
- **Requires:** Level 12 · Rogue 5
- **Identity:** The tracker: hunts down a chosen kind of enemy on the map.

### Thief
5 levels · rarity no colour
- **Requires:** Level 1 · Rogue 5
- **Identity:** The thief: profits from every fight and can even steal an enemy's weapon.

### Trap Layer
4 levels · rarity no colour
- **Requires:** Level 1 · Rogue 5 · 3 traps used
- **Identity:** Sets traps that strike enemies for it.
- Found: No passive and no skill, so nothing for a capstone.

### Trap Master
3 levels · rarity no colour
- **Requires:** Level 30 · Trap Layer 4
- **Identity:** The trap expert: more and deadlier traps on the field at once.
- Found: No passive and no skill, so nothing for a capstone.

## Weapons and spells together (5)

Hybrids that fight with a weapon and magic at once.

### Spellblade
5 levels · rarity no colour
- **Requires:** Level 1 · Warrior 3 · Mage 3 · 3 different spells known
- **Identity:** The warrior-mage: melee strikes charged with magic.

### Spell Sniper
5 levels · rarity no colour
- **Requires:** Level 1 · Archer 3 · Mage 3 · 3 different spells known
- **Identity:** The archer-mage: bow shots charged with magic.

### Hexblade
5 levels · rarity blue
- **Requires:** Level 1 · Spellblade 5 · Know 5 Dark spells
- **Identity:** A spellblade of curses: weakens enemies with every hit and brands one to take more from it.

### Nullblade
6 levels · **rarity no colour → blue** (your note)
- **Requires:** Level 25 · Spellblade 5
- **Identity:** Cuts magic away: its strikes strip enemies of their magical protection.
- Found: Null Strike (weaken the target's next spell) is the Spellbreaker's Mute Sigil, smaller; left to the Spellbreaker.
- *Your note, applied: This classes color should be blue*

### Spellbreaker
6 levels · rarity blue
- **Requires:** Level 30 · Enlightened 5 or Spellblade 5
- **Identity:** The mage-killer: shrugs off and shuts down enemy spells.

## The mage's path (14)

Spellcraft itself: learning, casting, Mana and the mind.

### Spellcaster
7 levels · rarity no colour
- **Requires:** Level 1 · Mage 5 · Intelligence higher than Wisdom
- **Identity:** The caster of intellect: efficient, focused spellcasting, one well-placed spell at a time.

### Wizard
7 levels · rarity no colour
- **Requires:** Level 1 · Mage 5 · Wisdom higher than Intelligence
- **Identity:** The caster of wisdom: casts its spells twice over and shares its knowledge with the party.

### Sorcerer
5 levels · rarity no colour
- **Requires:** Level 30 · Mage 5 · Know 20 different spells
- **Identity:** Builds up sorcery with every spell it casts, which protects it and can be unleashed all at once.
- Found: Sorcery Blast (5 damage per point) is weak at the level it needs (30).

### Great Mage
7 levels · **rarity no colour → blue** (needs Level 50 and is already among the strongest casters; a colour fits it better than weakening it)
- **Requires:** Level 50 · Mage 5
- **Identity:** The archmage: raw, all-round spell power for a dedicated caster.
- Found: Very strong for a class without a colour (hence the rarity proposed).

### Scholar
5 levels · rarity no colour
- **Requires:** Level 1 · Mage 5 · Know at least 10 different spells
- **Identity:** The learner: picks up spells cheaply and can cast surprising spells from anywhere in the catalogue.

### Wise Man
5 levels · rarity no colour
- **Requires:** Level 1 · Scholar 5
- **Identity:** The party's teacher: makes magic cheaper for everyone and can cast one spell three times.

### Archivist
5 levels · rarity blue
- **Requires:** Level 20 · Scholar 5 · Know 20 spells
- **Identity:** **Sharpened.** Records the enemy's magic and keeps it as scrolls to use later.
- Found: Open Ledger (copy the last spell at 70%) is the Mirror Mage's Spell Mirror (75%).

### Chronomancer
5 levels · rarity blue
- **Requires:** Level 45 · Wizard 7 · Know 10 Basic spells
- **Identity:** Bends time: acts sooner while the enemy acts later.

### Mirror Mage
4 levels · rarity blue
- **Requires:** Level 40 · Wizard 7
- **Identity:** The mirror: turns enemy magic back on its casters and copies their spells.

### Dreamweaver
6 levels · rarity no colour
- **Requires:** Level 30 · Sorcerer 5
- **Identity:** Puts enemies to sleep and preys on them while they can't act.

### Elemental Siphon
Renamed from Element Siphon; its id stays `element-siphon`.

5 levels · rarity blue
- **Requires:** Level 30 · Spellcaster 7 · Know spells of 6 elements
- **Identity:** Feeds on enemy magic, turning the elements thrown at it back on the enemy.
- *Your note, applied: Rename it to Elemental Siphon*

### Sage
5 levels · rarity blue
- **Requires:** Level 1 · Spellcaster 7 or Wizard 7 · Maximum Mana above maximum HP
- **Identity:** The deep well of Mana: outlasts every fight and keeps the party's Mana flowing.

### Energist
5 levels · rarity no colour
- **Requires:** Level 20 · Maximum Mana is at least 70% of maximum HP
- **Identity:** Balances body and mind, steadily renewing both through a fight.

### Psykic
8 levels · rarity purple
- **Requires:** Level 30 · Spellcaster 7 or Wizard 7 · Know at most 15 spells
- **Identity:** Fights with its own mind instead of many spells: psychic attacks and barriers paid for with Mana.

## The twelve elemental mages (12)

One per element, all built the same way: an early skill (level 3), a passive (5) and a great skill (9). Levels 4, 6, 7 and 8 are the same in all twelve (10% cheaper spells, +10% Mana, +15% damage, 30% cheaper learning); they get a look in the features round.

### Aeromancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Wind spells
- **Identity:** The Wind mage: swift casting that makes the whole party faster and harder to hit.

### Aquamancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Water spells
- **Identity:** The Water mage: efficient spells, restoring itself, and casting twice.

### Botanomancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Nature spells
- **Identity:** The Nature mage: healing over time and animal allies.
- Found: Call of the Ancient Pack is the Beastmaster's Call the Pack.

### Cryomancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Ice spells
- **Identity:** The Ice mage: freezes enemies in place and weathers their attacks.

### Electromancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Lightning spells
- **Identity:** The Lightning mage: overcharged bursts and storms over every enemy.

### Geomancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Earth spells
- **Identity:** The Earth mage: stone defence and heavy, armour-breaking strikes.

### Hemomancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Blood spells
- **Identity:** The Blood mage: spends its own blood as fuel for its magic.

### Hierophant
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Basic spells
- **Identity:** The Basic mage: fast, efficient pure magic that echoes.

### Luxomancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Holy spells
- **Identity:** The Holy mage: heals the party and wards off darkness.

### Necromancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Death spells
- **Identity:** The Death mage: raises the undead to fight for it.

### Pyromancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Fire spells
- **Identity:** The Fire mage: raw destruction at the price of its own safety.

### Umbramancer
9 levels · rarity no colour
- **Requires:** Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Dark spells
- **Identity:** The Dark mage: hides in shadow and weakens every enemy.

## Masters of elements (4)

Classes built on several elements or one element's special side.

### Avatar
4 levels · **rarity no colour → purple** (your note; it needs four elemental mages at Level 5)
- **Requires:** Level 1 · At least 4 core elemental mancer classes at Level 5
- **Identity:** Master of the six core elements: wields them all at once and always finds the enemy's weakness.
- *Your note, applied: Since this is somewhat hard to get, upgrade its color to purple*

### Stormcaller
7 levels · rarity purple
- **Requires:** Level 30 · Aeromancer 5 · Electromancer 5
- **Identity:** Wind and Lightning in rhythm: alternates the two to build up a storm.

### Sunstrider
6 levels · rarity purple
- **Requires:** Level 25 · Luxomancer 5 · Priest 5
- **Identity:** The sun's holy power: strongest by day.

### Plaguebinder
6 levels · **rarity no colour → blue** (your note)
- **Requires:** Level 25 · Warlock 5 or Necromancer 5
- **Identity:** Spreads sickness: poisons and curses that jump from enemy to enemy.
- *Your note, applied: Increase its color to blue*

## Priests and healers (4)

Healing and protecting the party with magic.

### Priest
7 levels · rarity no colour
- **Requires:** Level 1 · Mage 5 · 3 Holy spells known
- **Identity:** The holy healer: keeps the party alive and protected.

### Cardinal Priest
4 levels · **rarity no colour → blue** (needs Level 55 and Priest 7)
- **Requires:** Level 55 · Priest 7
- **Identity:** The high priest: powerful holy magic and protection for the whole party.

### Lifewarden
6 levels · rarity no colour
- **Requires:** Level 30 · Priest 5 or Cleric 5
- **Identity:** The keeper of life: heals so much that the excess turns into shields.

### Sanguine Medic
5 levels · rarity blue
- **Requires:** Level 1 · Hemomancer 5 · Priest 5
- **Identity:** Heals with its own blood, giving up its own health to save its allies.

## Summoners and dark pacts (9)

Creatures, pacts and rituals.

### Summoner
7 levels · rarity no colour
- **Requires:** Level 1 · Mage 5 · 10 summon spells used
- **Identity:** Lets its creatures fight for it and stays safe behind them.
- Found: Overprotective (can't be attacked at all while a summon lives) is very strong.

### Beastmaster
7 levels · rarity no colour
- **Requires:** Level 1 · Summoner 3 · At least 1 Nature Summon spell known
- **Identity:** Fights with animals at its side, never without a companion beast.

### Wildwarden
5 levels · rarity no colour
- **Requires:** Level 1 · Beastmaster 5 · Know 1 Nature summon spell
- **Identity:** The pack's protector: keeps its animal summons alive and strong.

### Soul Shepherd
5 levels · rarity blue
- **Requires:** Level 35 · Summoner 7
- **Identity:** The shepherd of summoned souls: its summons are its strength, and even their loss feeds it.

### Druid
7 levels · rarity no colour
- **Requires:** Level 1 · Mage 5 · 3 Nature spells known
- **Identity:** Nature's own caster: one with the wild, and nature answers when it is in danger.
- Found: Two passives and one skill.

### Warlock
7 levels · rarity no colour
- **Requires:** Level 1 · Mage 5 · 3 Dark spells known
- **Identity:** The Dark caster: draws power from a pact with darkness, at a risk to itself.

### Pactkeeper
4 levels · rarity blue
- **Requires:** Level 30 · Warlock 7 · Summoner 5 · Cast the same summon 10 times
- **Identity:** Bound to one familiar that returns every battle and grows over time.

### Cultist
7 levels · rarity no colour
- **Requires:** Level 1 · Use 10 Dark-Element summon spells
- **Identity:** Dark summoning rites: calls creatures cheaply and makes them stronger through ritual.
- Found: Breaks the capstone rule: Summon Ritual comes at level 3 and Dark Rituals at 6; level 7 is stats.

### Chaos Cultist
3 levels · **rarity no colour → blue** (needs Level 50, Cultist 7 and 100 Dark summons)
- **Requires:** Level 50 · Cultist 7 · Use 100 Dark-Element summon spells
- **Identity:** The cult's high caller: opens every battle with a demon and can call a whole host.

## Leaders and minds (5)

Commanding the party, and seeing what the enemy will do.

### Strategist
7 levels · rarity no colour
- **Requires:** Level 20 · 15 defensive actions by the whole party
- **Identity:** Commands the party: makes everyone quicker and better at defending, and directs the attack at one target.

### General
5 levels · rarity blue
- **Requires:** Level 50 · Strategist 7
- **Identity:** The commander: keeps the party's tempo high and its line firm.

### Flagbearer
4 levels · rarity blue
- **Requires:** Level 1 · Strategist 5 · 2 companions
- **Identity:** Carries the colours: strengthens its companions and raises a standard the enemy must break.

### Enlightened
5 levels · rarity no colour
- **Requires:** Level 10 · 25 defensive actions (Guard or Evade)
- **Identity:** Insight in defence: guards and dodges well and reads what the enemy will do.

### Diviner
7 levels · rarity blue
- **Requires:** Level 35 · Enlightened 5
- **Identity:** Foresight: predicts the enemy's next move and is nearly untouchable when right.

## Luck (4)

Fortune, dice and wagers.

### Bard
5 levels · rarity no colour
- **Requires:** Level 1 · At least 50 Luck
- **Identity:** The lucky performer: lifts the whole party with music and fortune.

### Jester
5 levels · **rarity no colour → blue** (your note)
- **Requires:** Level 1 · 120 Luck
- **Identity:** The fool fortune loves: lucky drops and lucky hits.
- Found: Level 4 has no effect at all.
- *Your note, applied: Increase its color to blue*

### Joker
3 levels · **rarity no colour → purple** (your note; it needs 220 Luck and Jack of all Trades 4 or Jester 5)
- **Requires:** Level 1 · Jack of all Trades 4 or Jester 5 · 220 Luck
- **Identity:** Pure chaos: wildly random power fuelled by enormous Luck.
- *Your note, applied: Increase its color to purple*

### Gambit
5 levels · rarity blue
- **Requires:** Level 20 · Jester 3
- **Identity:** The gambler: wagers its actions on risky strikes.

## The road, trade and the guild (12)

Travel, money, supplies, quests, artifacts and alchemy.

### Traveler
3 levels · rarity no colour
- **Requires:** Level 10 · 600 km traveled
- **Identity:** The road: travels faster and further.
- Found: The Road Ahead (Speed per living enemy in battle) has nothing to do with travelling.

### Cartographer
5 levels · rarity no colour
- **Requires:** Level 1 · Traveler 3 · Travel 1,500 km
- **Identity:** Maps the land: travels efficiently, avoids trouble and knows what lies ahead.
- Found: Surveyor's Eye predates the one continent of task 8; checked in the features round.

### Courier
3 levels · rarity no colour
- **Requires:** Level 1 · Traveler 3 · Complete 15 guild Travel quests
- **Identity:** Delivers for the guild: fast and safe on the road.

### Pilgrim
5 levels · rarity no colour
- **Requires:** Level 1 · Priest 4 · Traveler 3
- **Identity:** The holy traveller: blessed by the road it walks, and blesses the party in turn.

### Riverstrider
5 levels · rarity no colour
- **Requires:** Level 18 · Traveler 3 or Aquamancer 3
- **Identity:** At home on rivers: fast across water and elusive near it.
- Found: Narrow: most of it works only in river zones.

### Salesman
4 levels · rarity no colour
- **Requires:** Level 1 · Sell at least 200 items
- **Identity:** Money: earns more, and turns its wealth into power.

### Scavenger
**7 → 5 levels** · rarity no colour
- **Requires:** Level 1 · Loot 30 enemies
- **Identity:** Picks the battlefield clean: more money, masterworks and supplies from every fight.
- Found: Seven levels, three of them the same +5% money and two the same +1 masterwork chance; five levels keep every kind of feature.

### Quartermaster
3 levels · rarity no colour
- **Requires:** Level 20 · 2 companions
- **Identity:** Keeps the party supplied: consumables go further in its hands.

### Bounty Broker
4 levels · rarity no colour
- **Requires:** Level 1 · Hunter 4 or Salesman 2 · Complete 5 guild quests
- **Identity:** The guild's contractor: profits from guild work and hunts bounty targets.

### Relic Warden
5 levels · rarity no colour
- **Requires:** Level 25 · 2 artifacts equipped
- **Identity:** The keeper of artifacts: makes the most of them and shares their power with the party.

### Alchemist
5 levels · rarity no colour
- **Requires:** Level 20 · 10 potions brewed
- **Identity:** The brewer: forages, and knows ingredients better than anyone (task 9).
- Found: New with task 9.

### Assimilator
7 levels · rarity no colour
- **Requires:** Level 10 · 25 ingredients foraged
- **Identity:** Eats ingredients in battle for their power (task 9).
- Found: New with task 9.

## Race classes (8)

Classes for one race. Beast, Great Beast and Monster belong to races players can't choose (enemies use them).

### Noble
3 levels · rarity no colour
- **Requires:** Level 30 · Race: Human
- **Identity:** The human's class: wealth, reputation, and inspiring the party.

### Reveler
3 levels · rarity no colour
- **Requires:** Level 30 · Race: Hobbit
- **Identity:** The hobbit's class: good company that makes the whole party nimbler and luckier.

### Stonebreaker
3 levels · rarity no colour
- **Requires:** Level 30 · Race: Dwarf
- **Identity:** The dwarf's class: an iron body in heavy armour that shatters what it hits.

### Sylvan
3 levels · rarity no colour
- **Requires:** Level 30 · Race: Elf or Dark Elf
- **Identity:** The elf's and dark elf's class: a well of Mana that never runs dry.

### Dragonblood
3 levels · rarity no colour
- **Requires:** Level 30 · Race: Lizardman
- **Identity:** The lizardman's class: a dragon's body, and blood that can awaken.

### Beast
3 levels · rarity no colour
- **Requires:** Level 1 · Non-playable race
- **Identity:** For characters of a beast race (not playable): fights with claws and a beast's body.
- Found: No passive and no skill, so nothing for a capstone. Not playable.

### Great Beast
10 levels · rarity no colour
- **Requires:** Level 1 · Beast 3 · Non-playable race
- **Identity:** The beast grown great (not playable): a terrifying predator that cows weaker foes.
- Found: Not playable.

### Monster
3 levels · rarity no colour
- **Requires:** Level 1 · Non-playable race
- **Identity:** For characters of a monster race (not playable): a monster's body.
- Found: No passive and no skill, so nothing for a capstone. Not playable.

## Classes from rare items (6)

Unlocked by an item that is used up (a vial, a core, a crystal, a gem).

### Dragonkin
7 levels · **rarity no colour → blue** (needs a Vial of Dragonblood (0.5% from dragons))
- **Requires:** Level 1 · Vial of Dragonblood (consumed on acquisition)
- **Identity:** Has drunk a dragon's blood: a draconic body that can take to the air.

### Lich
5 levels · rarity purple
- **Requires:** Level 1 · Phylactery Core (consumed on acquisition)
- **Identity:** Undying: can't be killed while it has Mana, and commands the undead.

### Madman
3 levels · **rarity no colour → gold** (your note; it needs a Shadow Crystal, 0.05% from any enemy)
- **Requires:** Level 1 · Shadow Crystal (consumed on acquisition)
- **Identity:** Sees without the veil: no defence at all, but enormous damage and health.
- *Your note, applied: Increase its color to golden*

### Sharmat
3 levels · **rarity no colour → gold** (your note; it needs a Gem of False Dreams, Ur-Mat's drop at Level 93)
- **Requires:** Level 1 · Gem of False Dreams (consumed on acquisition)
- **Identity:** The false dreamer's power: can restore everything once in a battle, at a lasting price.
- *Your note, applied: Increase its color to golden*

### Vampire
4 levels · **rarity no colour → blue** (needs a Blood Crystal, a boss's drop (Tankina))
- **Requires:** Level 1 · Blood Crystal (consumed on acquisition)
- **Identity:** Feeds on blood: drains enemies to heal itself and stores blood for later.
- Found: Breaks the capstone rule: Bloodfeed comes at level 1 and Heart of Crimson at 2; levels 3 and 4 are stats and cheaper Blood spells.

### Vampire Lord
3 levels · **rarity no colour → purple** (your note)
- **Requires:** Level 40 · Vampire 4
- **Identity:** The vampire's lord: feeds freely and can burn all its blood in a frenzy.
- *Your note, applied: Increase its color to purple*

## Legends and the rest (4)

### Hero
3 levels · rarity gold
- **Requires:** Level 92 · Not together with Demon Lord
- **Identity:** The legend who saves the world: lifts and protects the whole party (never together with the Demon Lord).

### Demon Lord
3 levels · rarity gold
- **Requires:** Level 92 · Not together with Hero
- **Identity:** The legend who ends it: terrifies every enemy and destroys the weakened (never together with the Hero).

### Raid Boss
1 level · rarity no colour
- **Requires:** Level 1 · Unobtainable
- **Identity:** Enemy only: what makes a raid boss. Players can't get it.
- Found: Can't be acquired by players.

### Jack of all Trades
4 levels · **rarity no colour → blue** (your note)
- **Requires:** Level 1 · Warrior 3 · Archer 3 · Mage 3 · Rogue 3
- **Identity:** A bit of everything: rewards a character who has tried all four starting paths.
- Found: Breaks the capstone rule: its passive comes at level 2; level 4 fills the prerequisites.
- *Your note, applied: Increase its color to blue*

# New classes

## The task's list (25)

The 26 names in the task, but Poisoner, which is already in the game (C9); "Ascendend" is Ascended (C10).

### Acolyte
5 levels · rarity no colour
- **Requires:** Level 1 · Know 1 Holy spell
- **Identity:** A novice of the faith: supports allies with small prayers that grow stronger the more it helps in a battle.

### Bruiser
6 levels · rarity no colour
- **Requires:** Level 15 · Warrior 5 · Vitality is your highest stat
- **Identity:** A big, tough front-liner whose blows grow with its body: the higher its maximum HP, the harder it hits.
- *Your note, applied: I want this classes identity to do stronger hits the higher max HP it has (requirement asked after: Vitality is your highest stat)*

### Spell Sparkler
5 levels · rarity blue
- **Requires:** Level 10 · Mage 5 · Spellcaster 5 · Cast 3 spells in one turn, 5 times
- *Changed on your answer (C28): Spellcaster 5 added; its Quickened Spell is the way to three spells in a turn (5 AP a turn, most spells 3, the cheapest 2).*
- **Identity:** Showers the enemy with many small spells in one turn instead of one big one.
- *Your note, applied: Since this is quite difficult to achieve, increase its color to blue. Also make its requirement a small tad easier (10 times → 5)*

### Hermit
5 levels · rarity blue
- **Requires:** Level 25 · Spend 10 days in a row without entering a town
- **Identity:** Draws strength from solitude: stronger the longer it stays in the wild, and self-sufficient in a small party.

### Blade Dancer
7 levels · rarity blue
- **Requires:** Level 20 · Rogue 5 · Bard 3 · Land 5 weapon hits in a row in one battle, 10 times
- **Identity:** Fights like a dance: each hit in a row makes it quicker and harder to hit, and a miss breaks the rhythm.

### Hotshot
6 levels · rarity no colour
- **Requires:** Level 15 · Archer 5 · Hit 2 different enemies with ranged attacks in one turn, 25 times
- *Changed on your answer (C28): three ranged attacks in a turn need 6 AP, one more than a turn has.*
- **Identity:** Fast and flashy with ranged weapons: many quick, lighter shots spread over the enemy.

### Juggernaut
6 levels · rarity blue
- **Requires:** Level 30 · Knight 5 · Heavy Armor equipped
- **Identity:** Heavy armour as a weapon: unstoppable, its armour making it hit harder, and for all its weight its turns come sooner.
- *Your note, applied: I also want to add to its identity to increase its turn counter to act faster*

### Rearguard
5 levels · rarity no colour
- **Requires:** Level 15 · Warrior 5 · Win 20 battles in which no companion fell
- **Identity:** Covers the back of the party: protects the weaker allies and makes retreating safe.

### Vanguard
5 levels · rarity no colour
- **Requires:** Level 15 · Warrior 5 · Take the first turn of a battle 25 times
- **Identity:** The front of the charge: strongest at the start of a battle, getting the party moving first.

### Caravanist
4 levels · rarity no colour
- **Requires:** Level 20 · Traveler 3 · Travel by caravan 5 times
- **Identity:** A merchant of the road: cheaper and faster caravan travel, better trading, and supplies for the party.

### Acrobat
6 levels · rarity no colour
- **Requires:** Level 10 · Rogue 5 · Dexterity is your highest stat
- **Identity:** Dodges everything, and turns each dodge into momentum.

### Explorer
5 levels · rarity no colour
- **Requires:** Level 15 · Traveler 3 · Enter 8 regions
- **Identity:** Goes where no one has been: rewarded for discovering new places, enemies and hidden finds.

### Smith
5 levels · rarity no colour
- **Requires:** Level 10 · Reforge 5 items
- **Identity:** The craftsman in the field: better and cheaper forge work, and gear sharpened and mended before a fight.

### Mastersmith
4 levels · rarity purple
- **Requires:** Level 45 · Smith 5 · Reforge 25 items
- **Identity:** The Smith perfected: makes forged gear better than any forge sells it.

### Tinkerer
5 levels · rarity no colour
- **Requires:** Level 15 · Trap Layer 3
- **Identity:** Builds clockwork gadgets in battle that act on their own, and better traps.

### Informant
4 levels · rarity no colour
- **Requires:** Level 20 · Rogue 5 · Buy information from the hooded man 3 times
- **Identity:** Knows everything about the enemy and helps the party exploit it.

### Infiltrator
5 levels · rarity blue
- **Requires:** Level 20 · Rogue 5 · Shadow 3
- **Identity:** Gets past the enemy's defenders to strike the ones behind them and spoil their plans.

### Rune Knight
6 levels · rarity blue
- **Requires:** Level 25 · Knight 5 · Spellblade 3 · Fit 10 runes
- **Identity:** Awakens the runes in its gear, making them stronger and able to unleash their power.

### Martial Artist
7 levels · rarity blue
- **Requires:** Level 20 · Monk 4 · Brawler 3
- **Identity:** Fights in stances, switching between offence, defence and counters as the battle demands.

### Reaper
6 levels · rarity purple
- **Requires:** Level 40 · Reaver 5 · Know 5 Death spells
- **Identity:** Harvests the souls of the fallen and uses them for deadly strikes or healing.

### Ascended
4 levels · rarity gold
- **Requires:** Level 85 · Awakened 5
- **Identity:** A mortal grown beyond itself, transcending its limits as it rises in level; the step after Awakened.

### Titan
5 levels · rarity purple
- **Requires:** Level 50 · Strength is your highest stat · Defeat 30 giants
- **Identity:** Colossal: slow, enormous and crushing, hitting several enemies at once.

### Seeker
5 levels · rarity no colour
- **Requires:** Level 20 · Hunter 3 · Complete 5 Search quests
- **Identity:** Never loses its target: its attacks find even the most elusive enemy.

### Cycle
4 levels · rarity blue
- **Requires:** Level 40 · In one battle, Attack, Guard, use a healing item and cast a spell; 10 times
- **Identity:** Lives in a cycle of phases (strike, guard, mend, focus), each turn bringing the next.

### Unreality
3 levels · rarity red
- **Requires:** Level 1 · Sharmat 3 · Madman 3
- **Identity:** Rewrites the rules of reality: in its battles what is real gives way, and the rules of the fight itself are its to change.
- *Your note, applied: I want this class to be the only red rarity class (higher than gold). It requires both Sharmat and Madman at maximum Level and focuses on completely rewrtiting the rules of reality. I also want this class to have only 3 Levels. (Asked after: no level or other class of its own.)*

## The seven from classes/newc/ (7)

Names only; everything about them proposed fresh (C4, C7).

### Chronicler
5 levels · rarity blue
- **Requires:** Level 20 · Scholar 3 · Defeat 50 different kinds of enemy
- **Identity:** Keeps the record of every battle: the more of an enemy kind it has fought, the better it fights them.

### Shaman
6 levels · rarity no colour
- **Requires:** Level 10 · Mage 5 · Know 2 Nature and 2 Wind spells
- **Identity:** Speaks with spirits: raises totems on the battlefield that aid the party until they are broken.

### Elder Shaman
4 levels · rarity blue
- **Requires:** Level 45 · Shaman 6
- **Identity:** The Shaman's elder: commands more and stronger spirits and calls on the ancestors.

### Trickster
5 levels · rarity no colour
- **Requires:** Level 10 · Rogue 5 · At least 60 Luck
- **Identity:** Fights with tricks: feints, misdirection, and the enemy's own strength turned against it.

### Awakened
5 levels · rarity purple
- **Requires:** Level 50 · 4 classes at their final level
- **Identity:** Mastery awakened: rewards a character who masters classes rather than dabbling.

### Warden
6 levels · rarity blue
- **Requires:** Level 30 · Spellbreaker 3
- **Identity:** Keeps the enemy in check: seals their abilities and wards its allies.

### Scout
5 levels · rarity no colour
- **Requires:** Level 5 · Archer 3 · Travel 300 km
- **Identity:** Ahead of the party: sees encounters coming and chooses whether to fight, avoid or ambush.

## Eight of my own (8)

Picked for what no class does yet, and kept clear of the classes tasks 11–14 add (the three racial classes, Spellweaver, Ritual Caster, Dungeon Delver, Throwing Specialist).

### Field Medic
5 levels · rarity no colour
- **Requires:** Level 10 · Use 20 healing items in battle
- **Identity:** Heals without magic, with bandages and salves.

### Sentinel
6 levels · rarity no colour
- **Requires:** Level 25 · Soldier 3
- **Identity:** Watches and reacts: waits for the enemy to move, then strikes first.

### Beast Tamer
5 levels · rarity blue
- **Requires:** Level 15 · Beastmaster 3 · Defeat 50 Beast-class enemies
- **Identity:** Tames animals in battle (wolves, bears, griffons, and any animal kind added later) to fight for the party.
- *Your note, applied: Change its requirements to be all beast-class enemies, in case more Beasts get added to the game (asked after: it tames animals only)*

### Sunderer
5 levels · rarity no colour
- **Requires:** Level 20 · Warrior 5
- **Identity:** Breaks armour: strips the enemy's protection away piece by piece.

### Camp Cook
4 levels · rarity no colour
- **Requires:** Level 10 · Forage 30 ingredients
- **Identity:** Cooks at camp: meals that prepare the party for the battles ahead.

### Leywalker
6 levels · rarity blue
- **Requires:** Level 30 · Mage 5 · Know spells of 4 elements
- **Identity:** Draws on the land: its magic changes with where it fights.

### Nightwalker
5 levels · rarity blue
- **Requires:** Level 25 · Rogue 5 · Win 30 battles at night
- **Identity:** The night's own: strongest after dark, the Sunstrider's opposite.

### Mentor
4 levels · rarity no colour
- **Requires:** Level 40 · Strategist 3 · 2 companions
- **Identity:** Teaches its companions, helping them grow and fight above their level.

# Task 10, Class Rework: the features

The level-by-level features of every class, remade group by group for the user's review (C22). Each class keeps its
identity, levels, requirements and rarity from `Class-Rework-Draft.md`, uses its old features as the reference, and
gives its passive or its skill at its last level (the capstone rule). **Not built in the old game:** the port builds the
reworked classes from these drafts once every class is approved (C23).

**The rules** (the sample's, approved 2026-11-10, C21):
- A class has **one passive and one skill**. Exceptions: the twelve elemental mages, the classes acquired by an item
  (Dragonkin, Lich, Madman, Sharmat, Vampire) and the Vampire Lord; the enemy-only classes (Raid Boss, Beast, Great
  Beast, Monster) stay outside the rule.
- Every level does something worth taking; repeated small bonuses are merged rather than spread over several levels.
- Skills that worked once a day become once per battle where they aren't a big burst, so they are used, not saved.
- No penalties that make a feature miss more than it helps.
- Power follows the rarity.
- Hierophant's shape is the template for all twelve elemental mages.

**How to read it:** each class has a table of its levels, the old feature beside the remade one, then why. Numbers are
proposals to tune when built. "(new)" marks something the old game can't do and the port would need to add.

**Contents**
- The sample of four (approved 2026-11-10): Warrior, Vampire, Hierophant, Reaver.
- Batch 1, for review: the starting classes (Archer, Mage, Rogue) and the melee fighters (Barbarian, Berserker,
  Champion, Gladiator, Slayer, Soldier, Weaponmaster).

# The sample of four (approved 2026-11-10)

Drawn at random as the user asked: a starting class (Warrior), a class from a rare item (Vampire), an elemental mage
(Hierophant) and any class (Reaver).

## Warrior

5 levels · rarity no colour · Requires: Level 1
Identity: The starting melee fighter: wades into the fight in heavy armour and trades blows up close.

| Level | Before | Remade |
|---|---|---|
| 1 | Strength +5% | Strength +5% |
| 2 | Heavy armour's Evasion penalty −20% | Heavy armour's Evasion penalty −25% |
| 3 | **Warrior Spirit** (passive): with 2 or more enemies, Armor +5% | **Warrior Spirit** (passive): with 2 or more enemies, Armor +10% and melee weapon power +5% |
| 4 | Melee weapon Accuracy +10% | Melee weapon Accuracy +10% and maximum HP +5% |
| 5 | **Battle Cry** (skill, once a day): Armor −15%, Strength and Evasion +10% for 3 turns | **Battle Cry** (skill, capstone; once per battle, 1 AP): for 3 turns you gain 15% Strength and every ally 5% |

Why: its two features were faint, and Battle Cry traded away the Armor a heavy-armour fighter is built on for Evasion
it barely has. Now the passive pays off in the crowd it is meant for, and the cry is a short rally for the whole party,
usable every battle, which suits the class most players take first. Still a modest, no-colour class.

## Vampire

4 levels · rarity blue (raised in the draft) · Requires: Blood Crystal (consumed)
Identity: Feeds on blood: drains enemies to heal itself and stores blood for later.

| Level | Before | Remade |
|---|---|---|
| 1 | **Bloodfeed** (skill, 3 times a battle): next melee attack −40% Accuracy; on a hit heal 50% of the damage, the enemy loses 10% of all stats for 5 turns, +2 Blood Supplies | **Bloodfeed** (skill, 3 times a battle): your next melee hit drains: heal 50% of the damage, the enemy loses 10% of all stats for 3 turns, +2 Blood Supplies. No Accuracy penalty |
| 2 | **Heart of Crimson** (passive): at each turn start drink 1 Blood Supply: +10% HP, +5% Mana | **Heart of Crimson** (passive): at the start of your turn, only if you are hurt, drink 1 Blood Supply: +10% HP, +5% Mana (new: the "only if hurt") |
| 3 | All stats +5% | All stats +5%, and Blood spells cost 30% less Mana |
| 4 | Blood spells cost 30% less Mana | **Night Hunger** (skill, capstone; once per battle, no AP): for 3 turns every melee hit heals you for 25% of its damage and stores 1 Blood Supply, and you gain 15% Strength and Speed |

Why: it broke the capstone rule (its skill came at level 1 and its passive at 2, levels 3 and 4 were plain bonuses).
Bloodfeed lost 40% Accuracy, so it missed about as often as it fed; now it lands, its weakening lasts 3 turns instead of
5, and the stored blood is drunk only when it is needed instead of being wasted at full health. The last level becomes
the class's peak: a few turns of feeding on every hit, the power of a blue class. The Vampire Lord's Blood Frenzy (burn
all the blood at once) stays the Lord's.

## Hierophant

9 levels · rarity no colour · Requires: Level 20 · Spellcaster 7 or Wizard 7 · Know at least 7 Basic spells
Identity: The Basic mage: fast, efficient pure magic that echoes.

| Level | Before | Remade |
|---|---|---|
| 1 | Basic spell power +10% | Basic spell power +10% |
| 2 | Intelligence and Wisdom +5% | Intelligence and Wisdom +5% |
| 3 | **Arcane Compression** (skill, once per battle): next Basic spell +25% power, −1 AP | **Arcane Compression** (skill, once per battle): next Basic spell +25% power, −1 AP |
| 4 | Basic spells cost 10% less Mana | Basic spells cost 10% less Mana and 30% less to learn |
| 5 | **Perfect Formula** (passive): Basic spells +15% power, −1 AP (at least 1) | **Perfect Formula** (passive): Basic spells +10% power, −1 AP (at least 1) |
| 6 | Maximum Mana +10% | Maximum Mana +10% |
| 7 | Basic spell damage +15% | **Resonance** (passive): each Basic spell you cast this battle adds 3% Basic spell power, up to 15% (new) |
| 8 | Basic spells cost 30% less to learn | Basic spell damage +15% |
| 9 | **Arcane Echo** (skill, once a day): next Basic spell +20% power and cast 2 more times for free | **Arcane Echo** (skill, capstone; once per battle): your next Basic spell echoes twice more at half power, for no Mana or AP |

Why: four of its nine levels (4, 6, 7, 8) are the same in all twelve elemental mages; the cheaper learning moves into
level 4 so level 7 can carry a second passive that makes it the mage that "echoes": its power builds with every Basic
spell in a battle. Arcane Echo was a huge burst once a day (three casts at +20%, 360% in all) that players save and
seldom use; now it is two echoes at half power (200%) every battle. Perfect Formula's power goes from 15% to 10% to pay
for Resonance. If this shape is approved, the other eleven elemental mages get it too, each with its own level-7 passive.

## Reaver

7 levels · rarity blue · Requires: Level 25 · Barbarian 5 or Berserker 6
Identity: The finisher: picks off wounded enemies and carries its momentum from kill to kill.

| Level | Before | Remade |
|---|---|---|
| 1 | Weapon power +5% | Weapon power +5% |
| 2 | Strength +5% | Strength +5% |
| 3 | **Killing Momentum** (passive): a kill advances your turn by 200 | **Killing Momentum** (passive): a kill advances your turn by 200 and restores 5% of your maximum HP |
| 4 | +10% damage to enemies below 35% HP | +15% damage to enemies below 35% HP |
| 5 | Weapon Armor Pierce +10% | Weapon Armor Pierce +10% and Accuracy +10% |
| 6 | Weapon Accuracy +10% | **Blood Scent:** +10 Crit Rating against enemies below 35% HP |
| 7 | **Final Blow** (skill, once per battle): strike one enemy, +35% damage if it is below 35% HP | **Final Blow** (skill, capstone; once per battle): strike one enemy for 135% damage, 175% if it is below 35% HP; if it kills, Final Blow can be used again this battle |

Why: Final Blow was barely stronger than a normal attack (its +35% only below 35% HP, the same threshold the level-4
bonus already covers) and once a battle. Now it is a real finisher that chains through a weakened enemy line, the way
the Hunter's Mark comes back on a kill. Each kill also heals a little, so the momentum keeps it standing, and level 6
makes its finishing blows more likely to be critical. Accuracy merges into level 5. A blue class's power.

# Batch 1: the starting classes and the melee fighters (for review)

Warrior and Reaver, from these groups, are in the sample above.

## Archer

5 levels · rarity no colour · Requires: Level 1
Identity: The starting ranged fighter: picks enemies off from a distance with a bow, best when focusing on one target.

| Level | Before | Remade |
|---|---|---|
| 1 | Dexterity +5% | Dexterity +5% |
| 2 | Bows gain 2 base power | Bows gain 2 base power |
| 3 | **Single Target** (passive): bow Accuracy +8%, 2% less for every enemy in the battle | **Single Target** (passive): bow attacks gain 10% Accuracy and 5% damage, twice that when only one enemy is left |
| 4 | Ranged weapon Accuracy +10% | Ranged weapon Accuracy +10% and Speed +5% |
| 5 | **Sharpshoot** (skill, once a day): next bow attack +40% Accuracy | **Sharpshoot** (skill, capstone; once per battle): your next bow attack gains 40% Accuracy and 15% damage |

Why: Single Target shrank with every enemy, so in most fights it did nothing; now it always helps and peaks when the
fight is down to one. Sharpshoot is used every battle and also hits a little harder. A modest starting class.

## Mage

5 levels · rarity no colour · Requires: Level 1
Identity: The starting caster: fights with spells, and leads into every magic path.

| Level | Before | Remade |
|---|---|---|
| 1 | Learn 2 of 5 random Tier 1 spells (not Dark, Blood or Nature) | Learn 2 of 5 random Tier 1 spells (not Dark, Blood or Nature) |
| 2 | Tier 1–2 spells cost 5% less Mana | Tier 1–2 spells cost 5% less Mana, and Intelligence +5% |
| 3 | **Magic Apprentice** (passive): Tier 1–2 spells cost half to learn | **Magic Apprentice** (passive): Tier 1–2 spells cost half to learn and gain 10% power |
| 4 | Wisdom +5% | Wisdom +5% and maximum Mana +5% |
| 5 | **Mana Overload** (skill, once a day): next spell costs double Mana for +100% power | **Mana Overload** (skill, capstone; once per battle): your next spell costs double Mana for +100% power |

Why: Magic Apprentice only helped in the shop; now it also helps the first spells in battle. Mana Overload pays for
itself with double Mana, so once a battle is enough of a limit. Levels 2 and 4 each add a little more so none is thin.

## Rogue

5 levels · rarity no colour · Requires: Level 1
Identity: The starting rogue: quick and light, relying on dodging and critical hits with light weapons and bows.

| Level | Before | Remade |
|---|---|---|
| 1 | Speed +5% | Speed +5% |
| 2 | Evasion +10% in light armour or clothing | Evasion +10% in light armour or clothing |
| 3 | **Crit Sense** (passive): one-handed weapons and bows +10 Crit Rating | **Crit Sense** (passive): one-handed weapons and bows +10 Crit Rating |
| 4 | Accuracy +10% with one-handed weapons and bows | Accuracy +10% with one-handed weapons and bows, and Dexterity +5% |
| 5 | **Lucky Hit** (skill, once a day): next attack +15 Crit Rating | **Lucky Hit** (skill, capstone; once per battle, no AP): your next attack gains 30 Crit Rating and 10% damage |

Why: Lucky Hit once a day for +15 Crit Rating was hardly worth a press; now it is a likely critical blow every battle.
The rest already worked and stays.

## Barbarian

7 levels · rarity no colour · Requires: Level 1 · Warrior 5 · No light or heavy armour
Identity: Fights without real armour and wins by raging: reckless, hard-hitting and hard to bring down, hammering one foe
after another.

| Level | Before | Remade |
|---|---|---|
| 1 | Strength +5% | Strength and Vitality +5% |
| 2 | **Rage** (skill, once per battle): 5 turns of +20% damage, −20% damage taken, +30% Armor Pierce; only Attack, Run or end turn | **Rage** (skill, once per battle): 5 turns of +20% damage, −20% damage taken, +30% Armor Pierce; only Attack, Run or end turn |
| 3 | Vitality +5% | Speed +5% |
| 4 | Speed +5% | Melee weapon Accuracy +15% |
| 5 | **Seeing Red** (passive): +10% damage to the enemy you attacked on your previous turn | **Relentless:** +10% damage to the enemy you attacked on your previous turn (no longer a passive) |
| 6 | Melee weapon Accuracy +20% | Strength and Vitality +5% |
| 7 | Strength and Vitality +5% | **Unending Rage** (passive, capstone): Rage lasts 2 more turns, and while raging you regain 3% of your maximum HP at the start of each turn |

Why: it broke the capstone rule (Rage at level 2, its passive at 5, level 7 only stats). Rage stays early, since raging
is the class; the capstone becomes a passive that makes the rage longer and keeps it standing ("hard to bring down").
Seeing Red keeps its effect as a plain bonus, so the class has one passive and one skill.

## Berserker

6 levels · rarity no colour · Requires: Level 15 · Warrior 5 or Archer 5 · Kill at least 3 enemies in one battle, 5 times
Identity: Wants to be surrounded: the more enemies on the field, the harder it hits, cutting through whole groups.

| Level | Before | Remade |
|---|---|---|
| 1 | Weapon power +15% | Weapon power +10% |
| 2 | Maximum HP +10% | Maximum HP +10% |
| 3 | **Battle Hunger** (passive): +5% damage per living enemy, up to 25% | **Battle Hunger** (passive): +5% damage per living enemy, up to 25% |
| 4 | Strength and Dexterity +5% | Strength and Dexterity +5% |
| 5 | Weapon Crit Rating +10 | Weapon Crit Rating +10, and +5% weapon power |
| 6 | **Rampage** (skill, once per battle): attack every enemy with the main weapon at 75% damage | **Rampage** (skill, capstone; once per battle): attack every enemy with the main weapon at 85% damage; each enemy it kills restores 5% of your maximum HP |

Why: it already fits its identity and the rules; only small changes. Part of level 1's weapon power moves to level 5 so
the first level isn't the strongest, and Rampage hits a little harder and rewards clearing a group.

## Champion

4 levels · rarity no colour · Requires: Level 40 · Slayer 5 or Gladiator 5
Identity (sharpened): Seeks out the strongest enemy on the field and grows more dangerous the tougher its opponent; a
fighter for boss battles.

| Level | Before | Remade |
|---|---|---|
| 1 | Strength and Vitality +5% | Strength and Vitality +5% |
| 2 | **Arena Champion** (passive): +2% Strength and Vitality per living enemy, +10% Accuracy | **Worthy Foe** (passive): against the enemy with the most maximum HP on the field (a boss above all), deal 15% more damage and take 10% less from it (new: finding the strongest enemy) |
| 3 | Maximum HP and weapon power +10% | Maximum HP and weapon power +10% |
| 4 | **Champion's Challenge** (skill, once a day): a protected duel with one enemy for 3 turns, +25% damage, −15% damage taken | **Champion's Challenge** (skill, capstone; once per battle): call out the strongest enemy: for 3 turns your attacks on it deal 25% more damage, and it is more likely to attack you than your allies |

Why: sharpened in the draft. Its duel copied the Gladiator's and its passive the Berserker's per-enemy bonus; now both
aim at the toughest foe, which makes it the class for boss fights. The challenge draws that foe's attacks onto the
Champion instead of locking the two away from the battle.

## Gladiator

7 levels · rarity no colour · Requires: Level 15 · Warrior 3
Identity: The duellist: pins one enemy down in single combat and is at its best one-on-one.

| Level | Before | Remade |
|---|---|---|
| 1 | Strength +5% | Strength and Vitality +5% |
| 2 | Vitality +5% | Armor +10% |
| 3 | Armor +10% | Weapon Crit Rating +10 |
| 4 | **Duel** (skill, once per battle): for 3 turns you and one enemy can only fight each other | **Duel** (skill, once per battle): for 3 turns you and one enemy can only fight each other |
| 5 | Weapon Crit Rating +10 | Weapon power +10% |
| 6 | Weapon power +10% | **Arena Footwork:** Evasion +10% and melee Accuracy +10% |
| 7 | **Single Combat** (passive): +10% Strength and +20% Evasion against a single opponent or in a duel | **Single Combat** (passive, capstone): +15% Strength and +20% Evasion against a single opponent or in a duel |

Why: it already follows the rules. The two single-stat levels merge, which leaves room for footwork at level 6, and the
capstone passive grows a little.

## Slayer

7 levels · rarity no colour · Requires: Level 1 · Warrior 5 · Win 30 battles with no living ally left
Identity: The lone warrior: fights best with no companions beside it, outnumbered and still winning.

| Level | Before | Remade |
|---|---|---|
| 1 | Strength +5% | Strength and Vitality +5% |
| 2 | Vitality +5% | Weapon power +10% |
| 3 | Weapon power +10% | Maximum HP +10% |
| 4 | **Lone Legion** (passive): all stats +15% with no living companion (summons don't count) | **Lone Legion** (passive): all stats +15% with no living companion (summons don't count) |
| 5 | Maximum HP +10% | **Last One Standing:** when your last companion falls, restore 15% of your maximum HP (new) |
| 6 | Weapon Armor Pierce +15% | Weapon Armor Pierce +15% |
| 7 | **Against All Odds** (skill, once a day): 3 turns of −20% damage taken and +10% damage per living enemy, up to 50% | **Against All Odds** (skill, capstone; once per battle): 3 turns of −20% damage taken and +10% damage per living enemy, up to 40% |

Why: Against All Odds becomes once per battle, with its top a little lower (40%) to pay for that. Level 5 is new: the
moment the Slayer is left alone, it gets back on its feet, which is when it is strongest.

## Soldier

6 levels · rarity no colour · Requires: Level 25 · Warrior 5 or Archer 5 or Rogue 5
Identity: The steady veteran: reliable rather than flashy, and stronger the longer a battle lasts.

| Level | Before | Remade |
|---|---|---|
| 1 | Vitality and Dexterity +5% | Vitality and Dexterity +5% |
| 2 | Weapon Accuracy +20% | Weapon Accuracy +15% |
| 3 | Maximum HP +10% | Maximum HP +10% |
| 4 | **Battle-Hardened** (passive): +3% to all stats after each turn, up to 15% | **Battle-Hardened** (passive): +3% to all stats after each turn, up to 15% |
| 5 | Weapon power +15% | Weapon power +10% |
| 6 | **Veteran's Momentum** (skill, once per battle, 1 AP): 3 turns of +25% damage, −20% damage taken, +20% Armor and Evasion | **Veteran's Momentum** (skill, capstone; once per battle, 1 AP): 3 turns of +15% damage and −15% damage taken |

Why: the draft found it too strong for a class without a colour: Veteran's Momentum gave four big bonuses at once.
It keeps the two that fit a steady veteran, smaller, and levels 2 and 5 come down a little. Battle-Hardened, the heart
of the class, stays.

## Weaponmaster

7 levels · rarity no colour · Requires: Level 20 · Warrior 5
Identity: Master of every weapon: gets the most out of whatever it holds, with power and precision.

| Level | Before | Remade |
|---|---|---|
| 1 | All weapons gain 2 base power | All weapons gain 2 base power |
| 2 | Weapon Accuracy +10% | Weapon Accuracy +10% |
| 3 | **Masterful Grip** (passive): all weapons gain 0.2× Strength and 0.2× Dexterity scaling | **Masterful Grip** (passive): all weapons gain 0.2× Strength and 0.2× Dexterity scaling |
| 4 | Weapon Armor Pierce +10% | Weapon Armor Pierce +10% and Crit Rating +5 |
| 5 | Weapon Crit Rating +5 | Off-hand weapon attacks deal 20% more damage |
| 6 | Weapon power +15% | Weapon power +10% |
| 7 | **Perfect Form** (skill, once a day): next 3 weapon attacks +20% power, +20% Accuracy, +10 Crit Rating | **Masterstroke** (skill, capstone; once per battle): your next 3 weapon attacks gain 20% power, 20% Accuracy and 10 Crit Rating |

Why: its skill shared the name Perfect Form with the Elder Monk's passive, so it is renamed Masterstroke, and it works
every battle. Armor Pierce and Crit Rating merge, freeing level 5 for the off-hand, so a master of every weapon also
fights well with two. Weapon power at level 6 comes down a little to pay for that.

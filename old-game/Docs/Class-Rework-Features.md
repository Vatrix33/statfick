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
- Batch 1 (approved 2026-11-10): the starting classes (Archer, Mage, Rogue) and the melee fighters (Barbarian,
  Berserker, Champion, Gladiator, Slayer, Soldier, Weaponmaster).
- Batch 2, for review: the knights and defenders (Knight, Heavy Knight, Guardian, Bulwark, Echo Knight, Black Knight),
  the holy warriors (Cleric, Paladin, Death Paladin, Oathsworn, Templar) and the unarmed fighters (Brawler, Monk, Elder
  Monk, Ki Warrior).

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

# Batch 1: the starting classes and the melee fighters (approved 2026-11-10)

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

# Batch 2: knights and defenders, holy warriors, unarmed fighters (for review)

## Knight

7 levels · rarity no colour · Requires: Level 1 · Warrior 5
Identity: The armoured knight: a durable front-liner in heavy armour that hits hard, refuses to fall, and can stake
everything on one blow.

| Level | Before | Remade |
|---|---|---|
| 1 | Strength +10% in heavy armour | Strength +10% in heavy armour |
| 2 | Vitality +5% | Vitality +5% |
| 3 | **Last Stand** (passive): below 10% HP, all stats +20% | **Last Stand** (passive): below 25% HP, all stats +15% |
| 4 | Melee weapons gain 0.2× Strength scaling | Melee weapons gain 0.2× Strength scaling |
| 5 | Armor +10% | Armor +10% |
| 6 | Maximum HP +10% | Maximum HP +10% |
| 7 | **All or Nothing** (skill, once a day): next attack deals triple damage and always hits, can't be critical; then for 3 turns weapon attacks on you always hit and deal 50% more | **All or Nothing** (skill, capstone; once per battle): next attack deals double damage and always hits, can't be critical; then for 2 turns weapon attacks on you always hit and deal 25% more |

Why: Last Stand needed the Knight below 10% HP, a moment that seldom comes and seldom lasts; at 25% it actually fires.
All or Nothing becomes a once-per-battle gamble, so its blow and its price both come down a step. The rest already
fits.

## Heavy Knight

5 levels · rarity no colour · Requires: Level 1 · Knight 5 · Heavy armour and a shield
Identity: The tank: draws the enemy's attacks onto itself and gets harder to hurt the more it is hit.

| Level | Before | Remade |
|---|---|---|
| 1 | **Taunt** (skill, no limit): until your next turn, enemies have a 50% chance to attack you instead of an ally | **Taunt** (skill, no limit): until your next turn, enemies have a 50% chance to attack you instead of an ally |
| 2 | Armor +10% | Armor +10% |
| 3 | Maximum HP +5% | Maximum HP +5% and Strength +5% |
| 4 | Strength +5% | Shields grant 15% more Armor |
| 5 | **Ramping Defense** (passive): each melee or ranged hit taken adds 2% Armor for the battle, up to 100% | **Ramping Defense** (passive, capstone): each melee or ranged hit taken adds 3% Armor for the battle, up to 60% |

Why: it already follows the rules. Ramping Defense needed fifty hits to reach its top, so it hardened too slowly to
matter; now it ramps faster to a lower top. The two small stats merge, leaving room for a shield bonus that fits a
class that must carry one.

## Guardian

7 levels · rarity no colour · Requires: Level 1 · Knight 3 · Most maximum HP in the party
Identity: The bodyguard: steps in front of attacks meant for its allies and takes them itself.

| Level | Before | Remade |
|---|---|---|
| 1 | Vitality +5% | Vitality +5% |
| 2 | Armor +10% | Armor +10% |
| 3 | **Interpose** (passive): 35% chance to take a single-target attack aimed at an ally | **Interpose** (passive): 35% chance to take a single-target attack aimed at an ally; attacks you take this way deal 15% less to you |
| 4 | Maximum HP +10% | Maximum HP +10% |
| 5 | Shields grant 25% more Armor | Shields grant 25% more Armor |
| 6 | Take 10% less damage from everything | Take 10% less damage from everything |
| 7 | **Aegis Bond** (skill, once per battle): for 3 turns every single-target attack on one ally comes to you; Armor +20% | **Aegis Bond** (skill, capstone; once per battle): for 3 turns every single-target attack on one ally comes to you; Armor +20% |

Why: it fits its identity and the rules. The only change: what the Guardian takes for its allies hurts it a little
less, so stepping in doesn't simply move the damage.

## Bulwark

7 levels · rarity no colour · Requires: Level 20 · Knight 4 · 10 defensive actions
Identity (sharpened): The immovable wall: makes Guarding its strength and holds the line, rather than covering one ally.

| Level | Before | Remade |
|---|---|---|
| 1 | Armor +16% | Armor +15% |
| 2 | Guard's Armor bonus +10% | Guard's Armor bonus +20% |
| 3 | Maximum HP +8% | Maximum HP +8% |
| 4 | **Wall of Steel** (passive): take 10% less damage while Guarding | **Wall of Steel** (passive): take 15% less damage while Guarding |
| 5 | Shield Armor +12% | Shield Armor +12% |
| 6 | Guard's Armor bonus +10% | Guarding restores 3% of your maximum HP at the start of your next turn (new) |
| 7 | **Interpose** (skill, once per battle): guard one ally for 2 of their turns | **Hold the Line** (skill, capstone; once per battle, no AP): you Guard at once, and until your next turn you can't be stunned, take 30% less damage, and melee attackers take back 20% of what they deal you |

Why: sharpened in the draft. Its skill Interpose copied the Guardian (even its name); Hold the Line makes its last level
the Bulwark's own: a turn of standing firm that punishes whoever strikes it. The two Guard bonuses merge into level 2,
and level 6 lets a Bulwark that Guards well recover while it does.

## Echo Knight

7 levels · rarity blue · Requires: Level 35 · Knight 7 · Summoner 4
Identity: A knight with a spectral double: its strikes echo, landing again as ghostly blows.

| Level | Before | Remade |
|---|---|---|
| 1 | Weapon Accuracy +10% | Weapon Accuracy +10% |
| 2 | Armor +10% | Armor +10% |
| 3 | **Echo Blade** (passive): the first weapon hit each turn repeats for 15% of its damage | **Echo Blade** (passive): the first weapon hit each turn repeats for 15% of its damage |
| 4 | Weapon power +5% | Weapon power +5% and maximum HP +5% |
| 5 | Maximum HP +5% | Echoes can be critical hits (new) |
| 6 | Echo Blade's echo +10% | Echo Blade's echo +10% |
| 7 | **Spectral Double** (skill, once per battle): your next weapon hit repeats at 50% | **Spectral Double** (skill, capstone; once per battle): for 2 turns every weapon hit you land repeats at 40% |

Why: Spectral Double was one half-strength echo, small for a blue class's last level; now the double fights beside the
knight for two turns. Weapon power and HP merge so level 5 can let the echoes crit.

## Black Knight

5 levels · rarity no colour · Requires: Level 1 · Knight 7 · At least 10 Dark spells known
Identity: The dark knight: heavy armour and weapon fused with Dark magic, able to turn into something monstrous for a
short time.

| Level | Before | Remade |
|---|---|---|
| 1 | Dark spell power +15%, Dark spells cost 10% less Mana | Dark spell power +15%, Dark spells cost 10% less Mana |
| 2 | Strength and Mind +5% | Strength and Mind +5% |
| 3 | Weapon Armor Pierce +15% | Weapon Armor Pierce +15% |
| 4 | **Void-Forged Plate** (passive): heavy armour gives 25% more Armor; Dark spells deal 30% less to you | **Void-Forged Plate** (passive): heavy armour gives 25% more Armor; Dark spells deal 30% less to you |
| 5 | **Abyssal Transformation** (skill, once a day, 1 AP): 3 turns of +35% weapon and Dark spell power, −20% damage taken, +20% Holy damage taken | **Abyssal Transformation** (skill, capstone; once per battle, 1 AP): 3 turns of +25% weapon and Dark spell power, −15% damage taken, +20% Holy damage taken |

Why: it follows the rules; its transformation becomes once per battle and a step smaller to pay for that. (The
engine's tests read this class file; since C23 the old game's files don't change, so that stays as it is.)

## Cleric

7 levels · rarity no colour · Requires: Level 1 · Warrior 5 · 3 Holy spells known
Identity (sharpened): The battle priest: fights in the front line while keeping the party going with prayers.

| Level | Before | Remade |
|---|---|---|
| 1 | Wisdom +5% | Wisdom and Vitality +5% |
| 2 | Holy spells cost 10% less Mana | Holy spells cost 10% less Mana |
| 3 | Mind +5% | Mind +5% |
| 4 | **Holy Warrior** (passive): Dark spells deal 20% less to you; all stats +10% against demons and devils | **Combat Prayer** (skill, once per battle, 1 AP): for 3 turns every ally gains 15% Accuracy and Evasion and restores 4% of their maximum HP at the start of each turn |
| 5 | **Combat Prayer** (skill, once a day): you and every ally gain 20% Accuracy and Evasion for 3 turns | Holy spells gain 10% power and cost 30% less to learn |
| 6 | Vitality +5% | Melee weapon power +10% |
| 7 | Holy spells cost 30% less to learn | **Battle Faith** (passive, capstone): each weapon hit you land restores 3% of maximum HP to the ally with the least HP (new) |

Why: it broke the capstone rule (passive at 4, skill at 5, then plain bonuses), and its demon bonus repeated the
Paladin's and Templar's. Now it is a fighting priest: its prayer also mends and works every battle, and at its peak
every blow it lands heals the ally who needs it most, so it heals by fighting.

## Paladin

7 levels · rarity no colour · Requires: Level 10 · Knight 4 or Cleric 4 · No Dark or Blood spells · Not with Oathsworn
Identity (sharpened): The holy smiter: turns its Mana into holy strikes and hunts demons and devils, forswearing dark and
blood magic.

| Level | Before | Remade |
|---|---|---|
| 1 | Strength +5% | Strength +5% |
| 2 | Mind +5% | Mind +5% and maximum Mana +5% |
| 3 | **Sworn Oath** (passive): +10% damage to Monster-class enemies, −10% damage from them | **Holy Edge:** weapon hits on demons and devils deal 10% more damage |
| 4 | Maximum Mana +5% | Armor +10% |
| 5 | **Divine Smite** (skill, once per battle): spend 10% Mana; next weapon hit adds Holy damage equal to 10% of maximum Mana | **Divine Smite** (skill, once per battle): spend 10% Mana; your next weapon hit adds Holy damage equal to 15% of maximum Mana, double against demons and devils |
| 6 | Armor +10% | Holy spell power +10% |
| 7 | **Banish Evil** (skill, once a day): next attack +50% damage against a demon or devil | **Sworn Oath** (passive, capstone): +15% damage to demons and devils and −15% damage from them, and Divine Smite can be used twice a battle |

Why: it had two skills; Banish Evil goes and Divine Smite, the class's heart, does its job against demons and devils.
Sworn Oath now aims at the same foes (its Monster-class bonus is left to the Monster Hunter, as the draft says) and
becomes the capstone, with a second smite each battle.

## Death Paladin

6 levels · rarity blue (raised in the draft) · Requires: Level 1 · Paladin 7 · At least 10 Death spells known
Identity: A paladin turned to Death: smites with holy and death power together and weakens what it strikes.

| Level | Before | Remade |
|---|---|---|
| 1 | Death spell power +15% | Death spell power +15% and Death spells cost 10% less Mana |
| 2 | Death spells cost 10% less Mana | **Withering Touch:** weapon hits lower the target's Armor by 5% for 2 turns |
| 3 | Strength and Mind +5% | Strength and Mind +5% |
| 4 | Maximum Mana and Armor +10% | Maximum Mana and Armor +10% |
| 5 | **Grave Smite** (passive): Divine Smite's damage rises to 15% of maximum Mana and counts as Holy and Death | **Grave Smite** (passive): Divine Smite's damage rises by another 5% of maximum Mana and counts as Holy and Death |
| 6 | **Final Rite** (skill, once a day): next weapon attack +75% power, extra Death damage equal to 15% of maximum Mana, target's stats −10% for 2 turns | **Final Rite** (skill, capstone; once per battle): next weapon attack +60% power, extra Death damage equal to 15% of maximum Mana, target's stats −10% for 2 turns |

Why: it follows the rules. The two Death-spell levels merge, and level 2 gives every blow a little of the weakening its
identity speaks of. Final Rite works every battle at a little less power; as a blue class now, it keeps most of it.
Grave Smite adds to the Paladin's new, larger smite rather than setting it.

## Oathsworn

7 levels · rarity purple · Requires: Level 10 · Knight 4 · Hierophant 4 · No Dark or Blood spells · Not with Paladin
Identity (sharpened): A knight bound by vows: swears an oath each battle and draws great power while it keeps it.

| Level | Before | Remade |
|---|---|---|
| 1 | Strength and Mind +5% | Strength and Mind +5% |
| 2 | Maximum Mana +10% | Maximum Mana +10% |
| 3 | **Arcane Vow** (passive): +15% damage to Monster-class enemies, −10% damage from them | **Sworn Vow** (passive): at the start of each battle, swear one of three oaths. Protection: allies take 10% less damage; broken if an ally falls. Steadfast: you take 15% less damage; broken if you Run or Evade. Vengeance: pick an enemy and deal 25% more damage to it; broken if you attack another first. While it holds, all your stats +10%; once broken, all stats −10% for the rest of the battle (new) |
| 4 | Armor +10% | Armor +10% |
| 5 | Maximum Mana +10%; Basic and Holy spell power +10% | Maximum Mana +10%; Basic and Holy spell power +10% |
| 6 | Weapon power +10% | Weapon power +10% |
| 7 | **Divine Smite** (skill, once per battle): spend 10% Mana; next weapon hit adds Holy damage equal to 15% of maximum Mana | **Oathblade** (skill, capstone; once per battle): spend 10% Mana; your next weapon hit adds Basic and Holy damage equal to 20% of maximum Mana, 40% while your oath holds |

Why: sharpened in the draft: it was a Paladin with bigger numbers (the same Divine Smite, a stronger Sworn Oath). Now
its vow is the class: a choice each battle that rewards keeping it and punishes breaking it, and its last strike is
strongest while the oath holds. Purple power, purple risk.

## Templar

6 levels · rarity no colour · Requires: Level 1 · Cleric 7 or Priest 7
Identity (sharpened): The holy defender: shields the whole party and stands against the undead and dark magic.

| Level | Before | Remade |
|---|---|---|
| 1 | Strength and Wisdom +5% | Strength and Wisdom +5% |
| 2 | Holy spell power +15% | Holy spell power +15% |
| 3 | Armor +15% | Armor +15% |
| 4 | Maximum HP and Mana +10% | Maximum HP and Mana +10% |
| 5 | **Consecrated Arms** (passive): +20% damage to demons, devils and undead; Dark spells deal 20% less to you | **Consecrated Ground** (passive): every ally takes 15% less damage from Dark spells and from the undead, and you deal 20% more to the undead |
| 6 | **Templar's Aegis** (skill, once a day, 1 AP): for 3 turns every ally takes 20% less damage and gains 25% Armor and 20% Accuracy | **Templar's Aegis** (skill, capstone; once per battle, 1 AP): for 3 turns every ally takes 15% less damage and gains 20% Armor |

Why: sharpened in the draft: its passive now shields the whole party against what the Templar stands against, and its
demon bonus is left to the Paladin. The Aegis works every battle and drops the Accuracy that didn't fit a defender.

## Brawler

6 levels · rarity no colour · Requires: Level 1 · Use 5 unarmed attacks
Identity: The tavern fighter: fists and dirty tricks, scrappy and tough.

| Level | Before | Remade |
|---|---|---|
| 1 | Strength and Vitality +5% | Strength and Vitality +5% |
| 2 | Unarmed attacks gain 2 base power | Unarmed attacks gain 3 base power |
| 3 | **Rough Battle** (skill, once a day): 3 turns of −10% melee damage taken and +10% Armor Pierce for unarmed and melee attacks | **Rough Battle** (skill, once per battle): 3 turns of −15% melee damage taken and +15% Armor Pierce for unarmed and melee attacks |
| 4 | Strength +5% | Strength +5% |
| 5 | Evasion +20% | Evasion +10% and unarmed Accuracy +10% |
| 6 | **Tavern Brawler** (passive): melee attacks have a 30% chance to deal 2–10 more damage | **Tavern Brawler** (passive, capstone): melee and unarmed hits have a 30% chance to deal 15% more damage |

Why: its passive added a flat 2–10 damage, which is nothing past the first levels; now it grows with the Brawler. Rough
Battle works every battle. A little of level 5's Evasion becomes Accuracy, so the fists land.

## Monk

8 levels · rarity no colour · Requires: Level 6 · 30 unarmed attacks
Identity: The disciplined unarmed fighter: precise, stunning blows and a mind that resists magic.

| Level | Before | Remade |
|---|---|---|
| 1 | Unarmed attacks deal 50% more damage | Unarmed attacks deal 50% more damage |
| 2 | Unarmed attacks gain 20% Accuracy | Unarmed attacks gain 20% Accuracy |
| 3 | **Stunning Strike** (skill, once per battle): next unarmed attack has a 20% chance to stun for 1 turn | **Stunning Strike** (skill, once per battle): your next unarmed attack has a 35% chance to stun for 1 turn |
| 4 | Strength +5% | Strength and Wisdom +5% |
| 5 | Wisdom +5% | Unarmed attacks gain 10 Crit Rating |
| 6 | Unarmed attacks gain 0.1× Wisdom scaling | Unarmed attacks gain 0.1× Wisdom scaling |
| 7 | Speed +5% | Speed +5% |
| 8 | **Discipline Training** (passive): 5% less spell damage, with a 10% chance to cut a hit by 20% more | **Discipline Training** (passive, capstone): 10% less spell damage, and each spell that hits you makes your next unarmed attack deal 10% more (new) |

Why: Stunning Strike missed its stun four times in five, and the capstone passive was hardly felt; both now do what
the identity promises. Strength and Wisdom merge, and level 5 adds precision.

## Elder Monk

4 levels · rarity blue · Requires: Level 45 · Monk 8
Identity: The monk's mastery: devastating fists that stun and heal.

| Level | Before | Remade |
|---|---|---|
| 1 | Unarmed power +35% | Unarmed power +35% |
| 2 | Stunning Strike's stun chance +15 points | Stunning Strike's stun chance +15 points |
| 3 | **Inner Fist** (skill, once per battle): next unarmed attack +65% damage, heals you for 25% of it | **Inner Fist** (skill, once per battle): your next unarmed attack deals 80% more damage and heals you for 30% of it |
| 4 | **Perfect Form** (passive): unarmed attacks gain 0.2× Wisdom scaling; 10% less spell damage | **Perfect Form** (passive, capstone): unarmed attacks gain 0.2× Wisdom scaling; 10% less spell damage |

Why: it follows the rules and fits its identity; Inner Fist grows a little for a blue class.

## Ki Warrior

10 levels · rarity purple · Requires: Level 30 · Energist 5 or Psykic 8
Identity: Runs on Ki, an inner energy gathered during battle and spent on bursts of power; suits blade and spell alike.

| Level | Before | Remade |
|---|---|---|
| 1 | **Inner Ki** (passive): start each battle with Ki equal to 10% of your level (at least 1) | **Inner Ki:** start each battle with Ki equal to 10% of your level (at least 1); it lasts only for the battle (no longer a passive) |
| 2 | Strength and Dexterity +5% | Strength and Dexterity +5% |
| 3 | Weapon Accuracy +10%, spell power +5% | Weapon Accuracy +10%, spell power +5% |
| 4 | **Ki Focus** (skill, no limit): spend 1 Ki: next attack or spell +20% power | **Ki Focus** (skill, no limit, no AP): spend 1 Ki: your next attack or spell gains 20% power |
| 5 | **Ki Infusion** (skill, once per battle): spend 2 Ki: restore 40% Mana plus half your Mind | **Ki to Mana:** at the start of your turn, if your Mana is below 25%, 1 Ki turns into 20% of your maximum Mana (new) |
| 6 | Strength +5%; spells cost 5% less Mana | Strength +5%; spells cost 5% less Mana |
| 7 | **Flow of Ki** (passive): regain 1 Ki every second turn | **Flow of Ki:** regain 1 Ki every second turn (no longer a passive) |
| 8 | Speed +5% | Speed +5% |
| 9 | Weapon power and spell power +10% | Weapon power and spell power +10% |
| 10 | **Ki Overdrive** (skill, once per battle): spend 3 Ki: +45% damage with attacks and spells for 2 turns | **Overflowing Ki** (passive, capstone): while you hold 3 or more Ki, your attacks and spells deal 20% more damage and you take 10% less damage |

Why: it had two passives and three skills; now Ki Focus is its one skill and Overflowing Ki its one passive. Gaining
Ki stays (as plain features at levels 1 and 7), Ki Infusion becomes an automatic Mana rescue, and the capstone turns
Ki into a choice: spend it on Focus for bursts, or hold three for a lasting edge. A purple class's peak.

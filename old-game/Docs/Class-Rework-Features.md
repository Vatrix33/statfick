# Task 10, Class Rework: the features

The level-by-level features of every class, remade group by group for the user's review (C22). Each class keeps its
identity, levels, requirements and rarity from `Class-Rework-Draft.md`, uses its old features as the reference, and
gives its passive or its skill at its last level (the capstone rule). **Not built in the old game:** the port builds the
reworked classes from these drafts once every class is approved (C23).

**The rules** (the sample's, approved 2026-11-10, C21):
- A class has **one passive and one skill**. Exceptions: the twelve elemental mages, the classes acquired by an item
  (Dragonkin, Lich, Madman, Sharmat, Vampire), the Vampire Lord, and the Ki Warrior and the Psykic, whose skills stand in
  for spells (C26, C27); the enemy-only classes (Raid Boss, Beast, Great Beast, Monster) stay outside the rule.
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
- Batch 2 (approved 2026-11-10, the Ki Warrior after its revision): the knights and defenders (Knight, Heavy Knight, Guardian, Bulwark, Echo Knight, Black Knight),
  the holy warriors (Cleric, Paladin, Death Paladin, Oathsworn, Templar) and the unarmed fighters (Brawler, Monk, Elder
  Monk, Ki Warrior).
- Batch 3 (approved 2026-11-10, Grand Theft still on a critical hit): the archer's path (Hunter, Sharpshooter, Blood Hunter, Demon Hunter, Drakeslayer, Monster
  Hunter), the rogue's path (Assassin, Ambusher, Fencer, Poisoner, Shadow, Eminence, Stalker, Thief, Trap Layer, Trap
  Master) and weapons and spells together (Spellblade, Spell Sniper, Hexblade, Nullblade, Spellbreaker).
- Batch 4 (approved 2026-11-10): the mage's path (Spellcaster, Wizard, Sorcerer, Great Mage, Scholar, Wise Man, Archivist,
  Chronomancer, Mirror Mage, Dreamweaver, Elemental Siphon, Sage, Energist, Psykic).
- Batch 5 (approved 2026-11-10, the Stormcaller's peak kept at +100%): the eleven other elemental mages on the Hierophant's template (Aeromancer, Aquamancer,
  Botanomancer, Cryomancer, Electromancer, Geomancer, Hemomancer, Luxomancer, Necromancer, Pyromancer, Umbramancer) and
  the masters of elements (Avatar, Stormcaller, Sunstrider, Plaguebinder).
- Batch 6, for review: priests and healers (Priest, Cardinal Priest, Lifewarden, Sanguine Medic), summoners and dark
  pacts (Summoner, Beastmaster, Wildwarden, Soul Shepherd, Druid, Warlock, Pactkeeper, Cultist, Chaos Cultist) and
  leaders and minds (Strategist, General, Flagbearer, Enlightened, Diviner).

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

# Batch 2: knights and defenders, holy warriors, unarmed fighters (approved 2026-11-10; the Ki Warrior after its revision)

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
| 5 | **Ki Infusion** (skill, once per battle): spend 2 Ki: restore 40% Mana plus half your Mind | **Ki Blast** (skill, no limit but Ki; 2 AP): spend 2 Ki: hurl your Ki at one enemy for Basic damage of 30 plus 0.4× Strength and 0.4× Intelligence (new) |
| 6 | Strength +5%; spells cost 5% less Mana | Strength and Speed +5%; spells cost 5% less Mana |
| 7 | **Flow of Ki** (passive): regain 1 Ki every second turn | **Flow of Ki:** regain 1 Ki every second turn (no longer a passive) |
| 8 | Speed +5% | **Ki Wave** (skill, no limit but Ki; 3 AP): spend 3 Ki: release a wave of Ki that deals Basic damage of 20 plus 0.3× Strength and 0.3× Intelligence to every enemy (new) |
| 9 | Weapon power and spell power +10% | Weapon power and spell power +10% |
| 10 | **Ki Overdrive** (skill, once per battle): spend 3 Ki: +45% damage with attacks and spells for 2 turns | **Overflowing Ki** (passive, capstone): while you hold 3 or more Ki, your attacks and spells deal 20% more damage and you take 10% less damage |

Why: the Ki Warrior is the one fighter whose skills stand in for spells, so it keeps several (C26): Ki Focus to
empower an attack or a spell, Ki Blast and Ki Wave as its own attacks on one enemy or all of them, paid with Ki instead
of Mana and scaling on Strength and Intelligence alike, so they suit blade and spell. Its one passive, Overflowing Ki,
turns Ki into a choice: spend it, or hold three for a lasting edge. Gaining Ki stays as plain features (levels 1 and
7). Level 8's Speed moves into level 6; the old Ki Infusion (Ki into Mana) gives its place to Ki Blast. A purple class's
peak.
Revised on the user's note (2026-11-10): "I wanted it to have many skills to simulate a spell replacement. Overflowing
Ki can stay, but add 2 more skills to that class that use Ki points." (The first version had Ki Focus as its only skill
and an automatic "Ki to Mana" at level 5.)

# Batch 3: the archer's path, the rogue's path, weapons and spells together (approved 2026-11-10)

## Hunter

7 levels · rarity no colour · Requires: Level 1 · Archer 5
Identity: The hunter: marks one quarry and brings it down with the bow, striking hardest before it can react.

| Level | Before | Remade |
|---|---|---|
| 1 | Bow damage +10% | Bow damage +10% |
| 2 | Evasion +10%, another 10% without heavy armour | Evasion +10%, another 10% without heavy armour |
| 3 | **Silent Killer** (passive): +25% weapon damage to enemies that haven't acted yet | **Silent Killer** (passive): +25% weapon damage to enemies that haven't acted yet |
| 4 | Speed +5%, another 5% without heavy armour | Speed +5%, another 5% without heavy armour |
| 5 | Luck +5% | Luck and Dexterity +5% |
| 6 | Dexterity +5% | Bow attacks gain 10% Armor Pierce |
| 7 | **Hunter's Mark** (skill, once a day, back when the mark dies): marked enemy takes +10% weapon and spell damage, weapon attacks on it +40% Accuracy | **Hunter's Mark** (skill, capstone; once per battle, back when you kill the marked enemy): marked enemy takes +10% weapon and spell damage, weapon attacks on it +40% Accuracy |

Why: it fits; the mark the other hunters build on now works every battle (still coming back on each kill). Luck and
Dexterity merge so level 6 can pierce armour.

## Sharpshooter

5 levels · rarity no colour · Requires: Level 1 · Archer 5
Identity: The precision archer: few shots, each one sure to land and likely to be critical.

| Level | Before | Remade |
|---|---|---|
| 1 | **Over-Precision** (skill, once a day): next bow attack +40% Accuracy, +20 Crit Rating | **Deadeye** (passive): bow attacks gain 10 Crit Rating, 20 against the enemy you shot last |
| 2 | Dexterity +5% | Dexterity and Luck +5% |
| 3 | Luck +5% | Critical hits with bows deal 15% more damage |
| 4 | Bow Accuracy +20% | Bow Accuracy +20% |
| 5 | **True Shot** (skill, once a day): next bow attack is a sure critical hit; then you are stunned for 1 turn | **True Shot** (skill, capstone; once per battle): your next bow attack is a sure critical hit and gains 40% Accuracy |

Why: it had two skills and no passive. Over-Precision's lift becomes Deadeye, a passive that rewards staying on one
target; True Shot works every battle and loses the stunned turn that cost as much as it gave.

## Blood Hunter

5 levels · rarity no colour · Requires: Level 1 · Hunter 7 · At least 5 Blood spells known
Identity: A hunter armed with blood magic: hunts vampires and the undead and drains life from its prey.

| Level | Before | Remade |
|---|---|---|
| 1 | Blood spells cost 10% less Mana | Blood spells cost 10% less Mana |
| 2 | Weapon power and Blood spell power +10% | Weapon power and Blood spell power +10% |
| 3 | Maximum HP +10% | Maximum HP +10% |
| 4 | **Blood Quarry** (passive): Hunter's Mark +10% damage and +10% Accuracy; +20% damage to the undead and vampires | **Blood Quarry** (passive): Hunter's Mark +10% damage and +10% Accuracy; +20% damage to the undead and vampires |
| 5 | **Crimson Pursuit** (skill, once per battle): next 3 weapon attacks +30% power, heal 25% of the damage | **Crimson Pursuit** (skill, capstone; once per battle): next 3 weapon attacks +30% power, heal 25% of the damage |

Why: unchanged; it already fits its identity and the rules.

## Demon Hunter

6 levels · rarity no colour · Requires: Level 1 · Hunter 7 · Defeat 40 devils, demons and impids
Identity: The hunter of demons, devils and impids.

| Level | Before | Remade |
|---|---|---|
| 1 | +15% damage to devils, demons and impids | +15% damage to devils, demons and impids |
| 2 | Luck +5% | Luck and Strength +5% |
| 3 | −10% damage from devils, demons and impids | −10% damage from devils, demons and impids |
| 4 | **Fiend's Bane** (passive): Hunter's Mark +15% damage and +20% Accuracy; another +10% damage to those foes | **Fiend's Bane** (passive): Hunter's Mark +15% damage and +20% Accuracy; another +10% damage to those foes |
| 5 | Strength +5% | +10% Accuracy against devils, demons and impids |
| 6 | **Exorcising Strike** (skill, once per battle): next weapon attack +40% power, +60% more against those foes | **Exorcising Strike** (skill, capstone; once per battle): next weapon attack +40% power, +60% more against those foes |

Why: it fits; the two small stats merge so level 5 adds to the hunt itself.

## Drakeslayer

6 levels · rarity no colour · Requires: Level 1 · Hunter 7 · Defeat 3 dragons
Identity: The hunter of dragons.

| Level | Before | Remade |
|---|---|---|
| 1 | +10% damage to dragons | +15% damage to dragons |
| 2 | −10% damage from dragons | −10% damage from dragons |
| 3 | **Breath Reader** (passive): −15% spell damage from dragons | **Breath Reader** (passive): −20% spell damage from dragons |
| 4 | Weapon Armor Pierce +10% | Weapon Armor Pierce +10% |
| 5 | +10% damage to dragons | Dragons you defeat are twice as likely to drop a Vial of Dragonblood (new) |
| 6 | **Drake's Tell** (skill, once per battle): mark a dragon: +30% Accuracy and +15% damage against it | **Drake's Tell** (skill, capstone; once per battle): mark a dragon: +30% Accuracy and +25% damage against it until it falls |

Why: the draft found it seldom useful, since dragons are few. Its damage bonuses are gathered at level 1 and in the mark,
and level 5 gives a reason to hunt them: the vial that makes a Dragonkin.

## Monster Hunter

7 levels · rarity no colour · Requires: Level 1 · Hunter 7 · At least 30 Monster-class enemies slain
Identity: The hunter of Monster-class enemies.

| Level | Before | Remade |
|---|---|---|
| 1 | +15% damage to Monster-class enemies | +15% damage to Monster-class enemies |
| 2 | Weapon Accuracy +20% | Weapon Accuracy +20% |
| 3 | −10% damage from Monster-class enemies | −10% damage from Monster-class enemies |
| 4 | Maximum HP +10% | Maximum HP +10% |
| 5 | Weapon Armor Pierce +15% | Weapon Armor Pierce +15% |
| 6 | **Relentless Quarry** (passive): Hunter's Mark +10% damage and +20% Accuracy | **Relentless Quarry** (passive): Hunter's Mark +10% damage and +20% Accuracy |
| 7 | **Cull the Aberration** (skill, once per battle): next weapon attack on a monster +80% power, +20% Armor Pierce | **Cull the Aberration** (skill, capstone; once per battle): next weapon attack on a monster +80% power, +20% Armor Pierce |

Why: unchanged; it already fits, and it now also holds the bonus against monsters the Paladin gave up (draft).

## Assassin

6 levels · rarity no colour · Requires: Level 1 · Rogue 5 or Sharpshooter 3
Identity (sharpened): The killer of one target: everything goes into a single lethal, critical strike.

| Level | Before | Remade |
|---|---|---|
| 1 | Luck +5% | Luck and Dexterity +5% |
| 2 | **Combatstalker** (passive): +25% damage to enemies that haven't acted yet | **Marked for Death** (passive): each attack on the same enemy adds 5 Crit Rating against it, up to 25; switching targets starts again (new) |
| 3 | Dexterity +5% | Speed +10% |
| 4 | Speed +10% | Critical hits deal 25% more damage |
| 5 | **Execute** (skill, once a day): next attack is a sure critical hit with +50% Armor Pierce | One-handed weapons and bows gain 10% Armor Pierce |
| 6 | Critical hits deal 25% more damage | **Execute** (skill, capstone; once per battle): your next attack is a sure critical hit with +50% Armor Pierce, and deals 25% more to an enemy below half HP |

Why: it broke the capstone rule (Execute at 5, critical damage at 6), and its passive was the Hunter's Silent Killer;
the draft leaves striking first to the Ambusher. Now it works one target over, growing more lethal with each attack on
it, and finishes with Execute every battle.

## Ambusher

7 levels · rarity no colour · Requires: Level 1 · Rogue 5 · Speed is your highest stat
Identity: Strikes first: fast, with an opening blow far stronger than the rest.

| Level | Before | Remade |
|---|---|---|
| 1 | Speed +5% | Speed and Dexterity +5% |
| 2 | Weapon power +5% | Weapon power +5% |
| 3 | Dexterity +5% | Your turn bar starts each battle a little filled, so you tend to act first (new) |
| 4 | **First Strike** (passive): first weapon attack of the first turn +35% damage | **First Strike** (passive): your first weapon attack of the battle deals 50% more damage, and attacks on enemies that haven't acted yet deal 15% more |
| 5 | Luck +5% | Luck +5% |
| 6 | Weapon Accuracy +20% | Weapon Accuracy +20% |
| 7 | **Shadow Opening** (skill, once per battle): next weapon attack +60% power, +40% Accuracy | **Shadow Opening** (skill, capstone; once per battle): next weapon attack +60% power, +40% Accuracy |

Why: it now holds striking first (the Assassin's old bonus, as the draft says): a stronger opening blow, a smaller edge on
every enemy that hasn't moved yet, and a head start so it really does go first.

## Fencer

6 levels · rarity no colour · Requires: Level 18 · Rogue 5 · 7 one-handed attacks
Identity: The duelling swordsman with a free off-hand: precise, evasive, and punishing every miss with a riposte.

| Level | Before | Remade |
|---|---|---|
| 1 | One-handed sword Accuracy +20% (empty off-hand) | One-handed sword Accuracy +20% (empty off-hand) |
| 2 | Evasion +10% (one-handed sword, empty off-hand) | Evasion +10% (one-handed sword, empty off-hand) |
| 3 | **Riposte** (passive): counterattack at 35% damage when a foe misses you, once a turn | **Riposte** (passive): counterattack at 50% damage when a foe misses you, once a turn |
| 4 | One-handed sword power +10% (empty off-hand) | One-handed sword power +10% (empty off-hand) |
| 5 | One-handed sword Crit Rating +5 (empty off-hand) | One-handed sword Crit Rating +10 (empty off-hand) |
| 6 | **Perfect Lunge** (skill, once per battle): next one-handed sword attack +40% Accuracy, +25% power | **Perfect Lunge** (skill, capstone; once per battle): next one-handed sword attack +40% Accuracy, +25% power |

Why: it fits; the riposte, its heart, hits a little harder, and level 5 counts for more.

## Poisoner

7 levels · rarity no colour · Requires: Level 15 · Rogue 5
Identity (remade on your note): The master of the game's poisons (task 9): throws them and coats its weapons with them,
and every poison works stronger and longer in its hands.

| Level | Before | Remade |
|---|---|---|
| 1 | Weapon Accuracy +10% | Poisons you throw last 1 turn longer |
| 2 | Dexterity +5% | Dexterity +5% and weapon Accuracy +10% |
| 3 | **Toxic Edge** (passive): weapon hits poison for 3% of maximum HP a turn for 2 turns | **Coated Blade** (passive): when you throw a poison, your weapon takes it too: your next 2 weapon hits apply it at half strength (new) |
| 4 | Poisoned targets heal 20% less | Poisoned enemies heal 20% less, and your poisons deal 15% more damage |
| 5 | Weapon Crit Rating +5 | Throwing a poison has a 25% chance not to use it up |
| 6 | Toxic Edge +2% of maximum HP a turn | Your poisons ignore Nature resistance, and their weakening effects (on Armor, hit chance, Speed and the like) are 25% stronger |
| 7 | **Noxious Coating** (skill, once per battle): next 2 weapon hits add 2% maximum-HP poison and −20% Accuracy | **Venom Volley** (skill, capstone; once per battle): throw one poison at every enemy, for the price of one |

Why: remade on your note: it poisoned with its own blade, untouched by the 12 poisons task 9 added. Now those poisons
are its weapon: thrown longer, stronger and cheaper, carried onto its blade, and at its peak thrown at every enemy at
once.

## Shadow

6 levels · rarity no colour · Requires: Level 18 · Rogue 5
Identity: The one the enemy overlooks: avoids being targeted and slips out of sight when it must.

| Level | Before | Remade |
|---|---|---|
| 1 | Evasion +10% | Evasion +10% |
| 2 | Speed +5% | Speed and Luck +5% |
| 3 | **Low Profile** (passive): with another living ally (not a summon), enemies are less likely to attack you | **Low Profile** (passive): with another living ally (not a summon), enemies are less likely to attack you |
| 4 | Luck +5% | Weapon attacks on an enemy that isn't attacking you this turn deal 10% more damage (new) |
| 5 | Weapon Accuracy +20% | Weapon Accuracy +20% |
| 6 | **Vanish** (skill, once per battle, no AP): 2 turns of +30% Evasion and far less likely to be attacked | **Vanish** (skill, capstone; once per battle, no AP): 2 turns of +30% Evasion and far less likely to be attacked, and your first attack out of it deals 30% more |

Why: it hid well but did little from hiding; now striking from the shadows pays (level 4, and the attack out of
Vanish). Speed and Luck merge to make room.

## Eminence

4 levels · rarity blue · Requires: Level 1 · Shadow 6 · 7 flawless victories
Identity: The untouched: deadly as long as it isn't hit, so it plays to avoid damage entirely.

| Level | Before | Remade |
|---|---|---|
| 1 | Weapon power +10% | Weapon power +10% |
| 2 | **Untouched Edge** (skill, once per battle): next weapon attack up to +60% power, less the more damage taken | **Untouched Edge** (skill, once per battle): next weapon attack up to +60% power, less the more damage taken |
| 3 | Luck +5% | Luck +5% and Evasion +10% |
| 4 | **Unscathed Presence** (passive): all damage up to +50%, less the more damage taken (none after losing half your HP) | **Unscathed Presence** (passive, capstone): all damage up to +50%, less the more damage taken (none after losing half your HP) |

Why: it fits; level 3 adds Evasion, which is how an untouched class stays untouched.

## Stalker

4 levels · rarity no colour · Requires: Level 12 · Rogue 5
Identity: The tracker: hunts down a chosen kind of enemy on the map.

| Level | Before | Remade |
|---|---|---|
| 1 | **Trail Sense** (passive): the map lists the enemy kinds you can track in your zone | **Trail Sense** (passive): the map lists the enemy kinds you can track in your zone |
| 2 | Speed +5% | Speed +5% |
| 3 | Weapon Accuracy +20% | Weapon Accuracy +15%, and +15% damage to the kind of enemy you tracked today (new) |
| 4 | **Track Quarry** (skill, once a day, on the map): start a fight with a chosen local enemy kind | **Track Quarry** (skill, capstone; once a day, on the map): start a fight with a chosen local enemy kind |

Why: it fits; Track Quarry stays once a day since it starts fights on the map rather than acting in one. Level 3 lets
the Stalker fight its quarry better once it has found it.

## Thief

5 levels · rarity no colour · Requires: Level 1 · Rogue 5
Identity: The thief: profits from every fight and can even steal an enemy's weapon.

| Level | Before | Remade |
|---|---|---|
| 1 | Luck +5% | Luck +5% |
| 2 | **Greedy Loot** (passive): enemies drop 15% more money | **Greedy Loot** (passive): enemies drop 15% more money |
| 3 | Speed +5% | Speed +5% |
| 4 | Evasion +20% | Evasion +20% |
| 5 | **Grand Theft** (skill, once a day): if your next attack is a critical hit, steal the enemy's weapon | **Grand Theft** (skill, capstone; once a day): if your next attack is a critical hit, steal the enemy's weapon and some of its money |

Why: Grand Theft also takes some of the enemy's money now. It still needs a critical hit (the user's note on the first
version, which let any hit steal), and stays once a day, as it brings loot rather than fighting power.

## Trap Layer

4 levels · rarity no colour · Requires: Level 1 · Rogue 5 · 3 traps used
Identity: Sets traps that strike enemies for it.

| Level | Before | Remade |
|---|---|---|
| 1 | Traps deal 50% more damage | Traps deal 50% more damage |
| 2 | Traps gain 50% Armor Pierce | **Hidden Snares** (passive): enemies caught in your traps lose 15% Speed for 2 turns (new) |
| 3 | Placing a trap has a 30% chance not to use it up | Traps gain 50% Armor Pierce, and placing one has a 30% chance not to use it up |
| 4 | Each trap triggers one more time | **Lay Snare** (skill, capstone; once per battle, no AP): place a trap that triggers one more time |

Why: it had neither a passive nor a skill, so nothing for a capstone. Its bonuses gather into levels 1 and 3, the snare
now slows what it catches, and the extra trigger becomes a skill for one free trap each battle.

## Trap Master

3 levels · rarity no colour · Requires: Level 30 · Trap Layer 4
Identity: The trap expert: more and deadlier traps on the field at once.

| Level | Before | Remade |
|---|---|---|
| 1 | Traps deal 50% more damage and gain 50% Armor Pierce | Traps deal 50% more damage and gain 50% Armor Pierce, and another 20% chance not to use one up |
| 2 | Another 20% chance not to use a trap up | **Twin Traps** (passive): you can have 2 traps active at once |
| 3 | You can have 2 traps active at once | **Detonate** (skill, capstone; once per battle): set off all your traps at once, against every enemy (new) |

Why: it had neither a passive nor a skill either. Two traps at once becomes its passive, and its capstone sets them all
off together.

## Spellblade

5 levels · rarity no colour · Requires: Level 1 · Warrior 3 · Mage 3 · 3 different spells known
Identity: The warrior-mage: melee strikes charged with magic.

| Level | Before | Remade |
|---|---|---|
| 1 | Strength and Intelligence +5% | Strength and Intelligence +5% |
| 2 | Maximum Mana +5% | Maximum Mana +5% |
| 3 | **Arcane Edge** (passive): melee weapons gain 0.2× Intelligence scaling | **Arcane Edge** (passive): melee weapons gain 0.2× Intelligence scaling |
| 4 | Melee Accuracy +10%, spell power +5% | Melee Accuracy +10%, spell power +5% |
| 5 | **Spellstrike** (skill, once per battle): spend 10% Mana; next melee attack adds Basic damage equal to 15% of maximum Mana | **Spellstrike** (skill, capstone; once per battle): spend 10% Mana; next melee attack adds Basic damage equal to 15% of maximum Mana |

Why: unchanged; it already fits its identity and the rules.

## Spell Sniper

5 levels · rarity no colour · Requires: Level 1 · Archer 3 · Mage 3 · 3 different spells known
Identity: The archer-mage: bow shots charged with magic.

| Level | Before | Remade |
|---|---|---|
| 1 | Dexterity and Intelligence +5% | Dexterity and Intelligence +5% |
| 2 | Maximum Mana +5% | Maximum Mana +5% |
| 3 | **Arcane Ballistics** (passive): bows gain 0.2× Intelligence scaling | **Arcane Ballistics** (passive): bows gain 0.2× Intelligence scaling |
| 4 | Bow Accuracy +10%, spell power +5% | Bow Accuracy +10%, spell power +5% |
| 5 | **Spellshot** (skill, once per battle): spend 10% Mana; next bow attack adds Basic damage equal to 15% of maximum Mana | **Spellshot** (skill, capstone; once per battle): spend 10% Mana; next bow attack adds Basic damage equal to 15% of maximum Mana |

Why: unchanged, like its twin the Spellblade.

## Hexblade

5 levels · rarity blue · Requires: Level 1 · Spellblade 5 · Know 5 Dark spells
Identity: A spellblade of curses: weakens enemies with every hit and brands one to take more from it.

| Level | Before | Remade |
|---|---|---|
| 1 | Dark spell power +10% | Dark spell power +10% |
| 2 | Weapon Accuracy +10% | Weapon Accuracy +10% |
| 3 | **Cursed Edge** (passive): weapon hits lower the target's Armor by 10% for 2 turns | **Cursed Edge** (passive): weapon hits lower the target's Armor by 10% for 2 turns |
| 4 | Dark spells cost 10% less Mana | Dark spells cost 10% less Mana |
| 5 | **Hexbrand** (skill, once per battle): your attacks and spells deal 20% more to one marked enemy | **Hexbrand** (skill, capstone; once per battle): your attacks and spells deal 25% more to one marked enemy |

Why: it fits; the brand grows a little for a blue class.

## Nullblade

6 levels · rarity blue (raised on your note) · Requires: Level 25 · Spellblade 5
Identity: Cuts magic away: its strikes strip enemies of their magical protection.

| Level | Before | Remade |
|---|---|---|
| 1 | Weapon Accuracy +10% | Weapon Accuracy +10% |
| 2 | 5% less spell damage taken | 5% less spell damage taken |
| 3 | **Unraveling Edge** (passive): weapon hits strip one magical buff from the target | **Unraveling Edge** (passive): weapon hits strip one magical buff from the target |
| 4 | Weapon power +5% | Weapon power +5%, spells cost 5% less Mana |
| 5 | Spells cost 5% less Mana | Weapon hits on an enemy with no magical buffs left deal 10% more damage (new) |
| 6 | **Null Strike** (skill, once per battle): next weapon hit weakens the target's next spell by 25% | **Severing Cut** (skill, capstone; once per battle): strip every magical buff from one enemy, and your hit deals 10% more for each one removed (new) |

Why: Null Strike was a smaller copy of the Spellbreaker's Mute Sigil, left to it in the draft. The Nullblade's own last
skill strips everything at once, and level 5 rewards cutting an enemy bare. A blue class's power now.

## Spellbreaker

6 levels · rarity blue · Requires: Level 30 · Enlightened 5 or Spellblade 5
Identity: The mage-killer: shrugs off and shuts down enemy spells.

| Level | Before | Remade |
|---|---|---|
| 1 | 10% less spell damage taken | 10% less spell damage taken |
| 2 | Guard's favoured defence +10% | Guard's favoured defence +10% |
| 3 | **Countercast** (passive): while Guarding, the first enemy spell at you each turn has a 25% chance to fizzle | **Countercast** (passive): while Guarding, the first enemy spell at you each turn has a 40% chance to fizzle |
| 4 | Weapon Accuracy +10% | Weapon Accuracy +10% |
| 5 | 5% less spell damage taken | 5% less spell damage taken |
| 6 | **Mute Sigil** (skill, once per battle): one enemy's next spell deals or heals 40% less | **Mute Sigil** (skill, capstone; once per battle): one enemy's next spell deals or heals 50% less |

Why: it fits; Countercast fizzled a spell one time in four while Guarding, too seldom to count on, and both features
grow a little for a blue class.

# Batch 4: the mage's path (approved 2026-11-10)

## Spellcaster

7 levels · rarity no colour · Requires: Level 1 · Mage 5 · Intelligence higher than Wisdom
Identity: The caster of intellect: efficient, focused spellcasting, one well-placed spell at a time.

| Level | Before | Remade |
|---|---|---|
| 1 | Intelligence +5% | Intelligence and Wisdom +5% |
| 2 | Wisdom +5% | Spell power +5% |
| 3 | Basic spells deal 5% more damage | Basic spells deal 10% more damage |
| 4 | Spells cost 5% less to learn | Spells cost 10% less to learn |
| 5 | **Quickened Spell** (skill, once per battle): next spell uses 1 fewer AP | **Quickened Spell** (skill, once per battle): your next spell uses 1 fewer AP |
| 6 | Mind +5% | Mind and maximum Mana +5% |
| 7 | **Pure Spells** (passive): if you cast exactly one spell in a turn and did nothing else, 25% of its Mana comes back | **Pure Spells** (passive, capstone): if you cast exactly one spell in a turn and did nothing else, 30% of its Mana comes back |

Why: it fits its identity; its levels were each a single small stat, so they are gathered and given a little more
weight, and the focused-casting passive grows a step.

## Wizard

7 levels · rarity no colour · Requires: Level 1 · Mage 5 · Wisdom higher than Intelligence
Identity: The caster of wisdom: casts its spells twice over and shares its knowledge with the party.

| Level | Before | Remade |
|---|---|---|
| 1 | Wisdom +5% | Wisdom and Intelligence +5% |
| 2 | Intelligence +5% | Spell power +5% |
| 3 | Basic spells cost 5% less Mana | Basic spells cost 10% less Mana |
| 4 | Spells cost 5% less to learn | Spells cost 10% less to learn |
| 5 | **Doubled Spell** (skill, once a day): next spell costs 2 more AP and is cast twice for one Mana price | **Doubled Spell** (skill, once per battle): your next spell costs 2 more AP and is cast twice for one Mana price |
| 6 | Mind +5% | Mind and maximum Mana +5% |
| 7 | **Knowledge Guide** (passive): a spell another party member knows costs the others half to learn | **Knowledge Guide** (passive, capstone): a spell another party member knows costs the others half to learn, and allies casting a spell you also know cast it 10% stronger (new) |

Why: Doubled Spell pays with two extra AP, a fair price every battle. Its capstone helped only when buying spells; now
the shared knowledge also shows in battle. The single small stats are gathered, as for the Spellcaster.

## Sorcerer

5 levels · rarity no colour · Requires: Level 30 · Mage 5 · Know 20 different spells
Identity: Builds up sorcery with every spell it casts, which protects it and can be unleashed all at once.

| Level | Before | Remade |
|---|---|---|
| 1 | Spells cost 5% less Mana | Spells cost 5% less Mana |
| 2 | Intelligence and Wisdom +5% | Intelligence and Wisdom +5% |
| 3 | **Sorcery Point** (passive): +1 point per spell cast; 1% less spell damage taken per 2 points | **Sorcery Point** (passive): +1 point per spell cast, up to 20; each 2 points give 1% less spell damage taken and 1% more spell power |
| 4 | Evasion +10% | Evasion +10% and maximum Mana +5% |
| 5 | **Sorcery Blast** (skill, 2 AP): spend all points: 5 Basic damage per point to one enemy | **Sorcery Blast** (skill, capstone; 2 AP): spend all points: Basic damage of 12 per point plus 0.4× Intelligence and 0.4× Wisdom to one enemy |

Why: the draft found Sorcery Blast weak at the level the class needs (30): 5 damage a point hardly mattered. Now it
grows with the Sorcerer, and the points also strengthen spells while they build, so holding them and spending them are
both worth it.

## Great Mage

7 levels · rarity blue (raised in the draft) · Requires: Level 50 · Mage 5
Identity: The archmage: raw, all-round spell power for a dedicated caster.

| Level | Before | Remade |
|---|---|---|
| 1 | Spell power +20% | Spell power +20% |
| 2 | Maximum Mana +20% | Maximum Mana +20% |
| 3 | Spells cost 15% less Mana | Spells cost 15% less Mana |
| 4 | **Grand Theory** (passive): spells deal 20% more damage, cost 50% less to learn | **Grand Theory** (passive): spells deal 20% more damage, cost 50% less to learn |
| 5 | Mind, Intelligence and Wisdom +10% | Mind, Intelligence and Wisdom +10% |
| 6 | All spells use 1 fewer AP (at least 1) | All spells use 1 fewer AP (at least 1) |
| 7 | **Magnum Opus** (skill, once a day): next 3 spells +50% power, 25% less Mana, 1 fewer AP | **Magnum Opus** (skill, capstone; once a day): next 3 spells +50% power, 25% less Mana, 1 fewer AP |

Why: unchanged. Its strength now matches its rarity (raised to blue in the draft rather than weakened), and Magnum
Opus stays once a day as the big burst it is.

## Scholar

5 levels · rarity no colour · Requires: Level 1 · Mage 5 · Know at least 10 different spells
Identity: The learner: picks up spells cheaply and can cast surprising spells from anywhere in the catalogue.

| Level | Before | Remade |
|---|---|---|
| 1 | Spells cost 15% less to learn | Spells cost 15% less to learn |
| 2 | Intelligence and Wisdom +5% | Intelligence and Wisdom +5% |
| 3 | **Careful Study** (passive): spells cost another 15% less to learn; maximum Mana +5% | **Careful Study** (passive): spells cost another 15% less to learn; maximum Mana +5% |
| 4 | Mind +5% | Mind +5% |
| 5 | **Wild Thesis** (skill, once per battle): cast a random spell from the whole game (no higher than your best tier) for free | **Wild Thesis** (skill, capstone; once per battle): cast a random spell from the whole game (no higher than your best tier) for free |

Why: unchanged; it already fits its identity and the rules.

## Wise Man

5 levels · rarity no colour · Requires: Level 1 · Scholar 5
Identity: The party's teacher: makes magic cheaper for everyone and can cast one spell three times.

| Level | Before | Remade |
|---|---|---|
| 1 | Spells cost 25% less to learn | Spells cost 25% less to learn |
| 2 | Maximum Mana +10% | Maximum Mana +10% |
| 3 | **Shared Insight** (passive): every party member's spells cost 10% less Mana (several Wise Men add up) | **Shared Insight** (passive): every party member's spells cost 10% less Mana (several Wise Men add up) |
| 4 | Wisdom +5%; your spells cost 10% less Mana | Wisdom +5%; your spells cost 10% less Mana |
| 5 | **Arcane Chorus** (skill, once a day): next spell costs 2 more AP and is cast 3 times at 40% less power, Mana paid once | **Arcane Chorus** (skill, capstone; once per battle): your next spell costs 2 more AP and is cast 3 times at 40% less power, Mana paid once |

Why: it fits; Arcane Chorus (180% in all, for two extra AP) isn't a burst that needs saving, so it works every battle.

## Archivist

5 levels · rarity blue · Requires: Level 20 · Scholar 5 · Know 20 spells
Identity (sharpened): Records the enemy's magic and keeps it as scrolls to use later.

| Level | Before | Remade |
|---|---|---|
| 1 | Spells cost 10% less to learn | Spells cost 20% less to learn |
| 2 | Mind +5% | Mind +5% |
| 3 | **Field Notes** (passive): after a victory, turn one enemy spell seen in the battle (up to your best tier) into a scroll that casts it once | **Field Notes** (passive): after a victory, turn one enemy spell seen in the battle (up to your best tier) into a scroll that casts it once |
| 4 | Spells cost another 10% less to learn | Scrolls you made cast 20% stronger |
| 5 | **Open Ledger** (skill, once per battle): copy the target's last spell at 70% power | **Transcribe** (skill, capstone; once per battle, no AP): write down an enemy spell seen this battle at once: its scroll is yours now, usable in this battle (new) |

Why: sharpened in the draft: copying a spell on the spot is the Mirror Mage's. The Archivist's skill now stays with its
records: it can make a scroll mid-battle instead of waiting for the victory, and its scrolls cast stronger. The two
cheaper-learning levels merge into level 1.

## Chronomancer

5 levels · rarity blue · Requires: Level 45 · Wizard 7 · Know 10 Basic spells
Identity: Bends time: acts sooner while the enemy acts later.

| Level | Before | Remade |
|---|---|---|
| 1 | Basic spells cost 5% less Mana | Basic spells cost 5% less Mana |
| 2 | Mind +5% | Mind +5% |
| 3 | **Temporal Drift** (passive): your turn bar fills 8% faster without more Speed | **Temporal Drift** (passive): your turn bar fills 10% faster without more Speed |
| 4 | Basic spell power +10% | Basic spell power +10% |
| 5 | **Time Shift** (skill, once per battle): your turn bar +250, one enemy's −250 | **Time Shift** (skill, capstone; once per battle): your turn bar +250, one enemy's −250 |

Why: it fits; Temporal Drift grows a step for a blue class.

## Mirror Mage

4 levels · rarity blue · Requires: Level 40 · Wizard 7
Identity: The mirror: turns enemy magic back on its casters and copies their spells.

| Level | Before | Remade |
|---|---|---|
| 1 | 10% less spell damage taken | 10% less spell damage taken |
| 2 | **Reflective Veil** (passive): 15% of enemy spell damage taken goes back to its caster | **Reflective Veil** (passive): 20% of enemy spell damage taken goes back to its caster |
| 3 | Mind +5% | Mind +5% |
| 4 | **Spell Mirror** (skill, once per battle): copy the target's last spell at 75% power | **Spell Mirror** (skill, capstone; once per battle): copy the target's last spell at 75% power |

Why: it fits, and now alone holds copying a spell on the spot (the Archivist's copy is gone). The reflection grows a
step for a blue class.

## Dreamweaver

6 levels · rarity no colour · Requires: Level 30 · Sorcerer 5
Identity: Puts enemies to sleep and preys on them while they can't act.

| Level | Before | Remade |
|---|---|---|
| 1 | Dark and Basic spell power +5% | Dark and Basic spell power +10% |
| 2 | Mind +5% | Mind +5% |
| 3 | **Nightmare Harvest** (passive): +15% damage to stunned enemies | **Nightmare Harvest** (passive): +15% damage to stunned enemies |
| 4 | Spells cost 5% less Mana | Spells cost 5% less Mana |
| 5 | Dark and Basic spell power +5% | Enemies you stun wake from it with 10% less Speed for 2 turns (new) |
| 6 | **Lullaby** (skill, once per battle): stun one enemy for 1 turn | **Lullaby** (skill, capstone; once per battle): stun one enemy for 1 turn |

Why: it fits; its two identical spell-power levels merge into level 1, and level 5 lets the nightmare linger after the
sleep.

## Elemental Siphon

5 levels · rarity blue · Requires: Level 30 · Spellcaster 7 · Know spells of 6 elements
Identity: Feeds on enemy magic, turning the elements thrown at it back on the enemy. (Renamed from Element Siphon on your
note.)

| Level | Before | Remade |
|---|---|---|
| 1 | 10% less elemental spell damage taken | 10% less elemental spell damage taken |
| 2 | Maximum Mana +8% | Maximum Mana +8% |
| 3 | **Arcane Intake** (passive): get back Mana equal to 20% of the enemy elemental spell damage you take | **Arcane Intake** (passive): get back Mana equal to 20% of the enemy elemental spell damage you take |
| 4 | Spell power +5% | Spell power +5% |
| 5 | **Siphon Charge** (skill, once per battle): after an enemy's elemental hit, turn your next spell into that element | **Siphon Charge** (skill, capstone; once per battle): after an enemy's elemental hit, turn your next spell into that element and give it 20% more power |

Why: it fits; Siphon Charge only changed the element, so it now also carries the stolen power.

## Sage

5 levels · rarity blue · Requires: Level 1 · Spellcaster 7 or Wizard 7 · Maximum Mana above maximum HP
Identity: The deep well of Mana: outlasts every fight and keeps the party's Mana flowing.

| Level | Before | Remade |
|---|---|---|
| 1 | Maximum Mana +20% | Maximum Mana +20% |
| 2 | Mind +5% | Mind +5% |
| 3 | **Mana Well** (skill, once per battle, 1 AP): every ally restores 25% of their maximum Mana | **Mana Well** (skill, once per battle, 1 AP): every ally restores 25% of their maximum Mana |
| 4 | Maximum Mana +20% | While your Mana is above 75%, your spells deal 10% more damage (new) |
| 5 | **Deep Reservoir** (passive): maximum Mana +20%; restore 4% of maximum Mana each turn | **Deep Reservoir** (passive, capstone): maximum Mana +20%; restore 4% of maximum Mana each turn |

Why: three of its five levels gave the same +20% Mana. Level 4 now turns a full well into power, the reward for a class
that never runs dry.

## Energist

5 levels · rarity no colour · Requires: Level 20 · Maximum Mana at least 70% of maximum HP
Identity: Balances body and mind, steadily renewing both through a fight.

| Level | Before | Remade |
|---|---|---|
| 1 | Maximum HP and Mana +5% | Maximum HP and Mana +5% |
| 2 | Vitality and Mind +5% | Vitality and Mind +5% |
| 3 | **Inner Current** (passive): restore 3% HP and 5% Mana at the start of each turn | **Inner Current** (passive): restore 3% HP and 5% Mana at the start of each turn |
| 4 | Spells cost 10% less Mana; maximum HP +5% | Spells cost 10% less Mana; maximum HP +5% |
| 5 | **Equilibrium** (skill, once per battle): restore 20% HP and 20% Mana | **Equilibrium** (skill, capstone; once per battle): restore 20% HP and 20% Mana |

Why: unchanged; it already fits its identity and the rules.

## Psykic

8 levels · rarity purple · Requires: Level 30 · Spellcaster 7 or Wizard 7 · Know at most 15 spells
Identity: Fights with its own mind instead of many spells: psychic attacks and barriers paid for with Mana.

| Level | Before | Remade |
|---|---|---|
| 1 | **Telekinetic Burst** (skill, once a turn; 12% Mana, 2 AP): 30 + 0.5× Intelligence + 0.2× Mind Basic damage to one enemy | **Telekinetic Burst** (skill, once a turn; 12% Mana, 2 AP): 30 + 0.5× Intelligence + 0.2× Mind Basic damage to one enemy |
| 2 | Mind +10% | Mind +10% |
| 3 | **Mind Barrier** (skill, once per battle; 15% Mana, 1 AP): 2 turns of +40% Armor and Evasion, 15% less damage taken | **Mind Barrier** (skill, once per battle; 15% Mana, 1 AP): 2 turns of +40% Armor and Evasion, 15% less damage taken |
| 4 | Maximum Mana +15% | Maximum Mana +15% |
| 5 | **Psychic Lance** (skill, once per battle; 18% Mana, 2 AP): 50 + 0.6× Intelligence + 0.3× Mind Basic damage, 25% chance to stun | **Psychic Lance** (skill, once per battle; 18% Mana, 2 AP): 50 + 0.6× Intelligence + 0.3× Mind Basic damage, 35% chance to stun |
| 6 | Intelligence +10% | Intelligence +10% |
| 7 | **Mental Pressure** (passive): Psykic skills deal 15% more damage | **Mental Pressure** (passive): Psykic skills deal 15% more damage and cost 10% less Mana |
| 8 | **Mindstorm** (skill, once per battle; 25% Mana, 3 AP): 40 + 0.5× Intelligence + 0.3× Wisdom Basic damage to every enemy, −15% Speed for 2 turns | **Mindstorm** (skill, capstone; once per battle; 25% Mana, 3 AP): 40 + 0.5× Intelligence + 0.3× Wisdom Basic damage to every enemy, −15% Speed for 2 turns |

Why: an exception to the one-skill rule like the Ki Warrior (your answer, C27): its four skills stand in for the
spells it doesn't know, and keep their shape. Its passive also makes them a little cheaper, since a Psykic lives on
Mana, and the lance stuns a little more often.

# Batch 5: the elemental mages and the masters of elements (approved 2026-11-10)

## The eleven elemental mages, on the Hierophant's template

All twelve share one shape (the Hierophant's, approved with the sample): 9 levels, rarity no colour, Requires: Level 20 ·
Spellcaster 7 or Wizard 7 · Know at least 7 spells of the element. As an exception to the one-skill rule (C21) each has
two skills and two passives. **Levels 1, 4, 6 and 8 are the same for all of them:**

| Level | Before | Remade |
|---|---|---|
| 1 | Element spell power +10% | Element spell power +10% |
| 4 | Element spells cost 10% less Mana | Element spells cost 10% less Mana and 30% less to learn |
| 6 | Maximum Mana +10% | Maximum Mana +10% |
| 7 | Element spell damage +15% | *each mage's own second passive (below)* |
| 8 | Element spells cost 30% less to learn | Element spell damage +15% |

Below, each mage's own levels (2, 3, 5, 7, 9). Level 2 now gives two stats, as the Hierophant's always did. The great
skill at level 9 becomes once per battle, a step smaller, unless it is one of the big bursts that stays once a day (a
stun on every enemy, the great party heal, an army of the dead, the blood apotheosis), as the rules say.

### Aeromancer

Identity: The Wind mage: swift casting that makes the whole party faster and harder to hit.

| Level | Before | Remade |
|---|---|---|
| 2 | Speed +5% | Speed and Intelligence +5% |
| 3 | **Tailwind** (skill, once per battle, 1 AP): every ally +20% Speed and Evasion for 2 turns | **Tailwind** (skill, once per battle, 1 AP): every ally +20% Speed and Evasion for 2 turns |
| 5 | **Weightless Casting** (passive): Wind spells use 1 fewer AP (at least 1) | **Weightless Casting** (passive): Wind spells use 1 fewer AP (at least 1) |
| 7 | Wind spell damage +15% | **Gale Step** (passive): each Wind spell you cast gives you 5% Speed for the battle, up to 15% (new) |
| 9 | **Eye of the Storm** (skill, once a day, 1 AP): every ally +30% Speed and Evasion for 3 turns; Wind spell power +40% | **Eye of the Storm** (skill, capstone; once per battle, 1 AP): every ally +25% Speed and Evasion for 3 turns; Wind spell power +30% |

### Aquamancer

Identity: The Water mage: efficient spells, restoring itself, and casting twice.

| Level | Before | Remade |
|---|---|---|
| 2 | Mind +5% | Mind and Wisdom +5% |
| 3 | **Replenishing Tide** (skill, once per battle, 1 AP): restore 20% HP and 15% Mana | **Replenishing Tide** (skill, once per battle, 1 AP): restore 20% HP and 15% Mana |
| 5 | **Living Current** (passive): Water spells +10% power and another 10% cheaper | **Living Current** (passive): Water spells +10% power and another 10% cheaper |
| 7 | Water spell damage +15% | **Ebb and Flow** (passive): each Water spell you cast gives back 2% of your maximum Mana (new) |
| 9 | **Sovereign Tide** (skill, once a day): next Water spell +25% power and cast a second time for free | **Sovereign Tide** (skill, capstone; once per battle): your next Water spell is cast a second time at 75% power, for no Mana or AP |

### Botanomancer

Identity: The Nature mage: healing over time and animal allies.

| Level | Before | Remade |
|---|---|---|
| 2 | Wisdom +5% | Wisdom and Vitality +5% |
| 3 | **Regenerative Bloom** (skill, once per battle, 1 AP): every ally restores 5% HP at the start of their turn for 3 turns | **Regenerative Bloom** (skill, once per battle, 1 AP): every ally restores 5% HP at the start of their turn for 3 turns |
| 5 | **Cycle of Growth** (passive): Nature spells +15% power, maximum HP +10% | **Cycle of Growth** (passive): Nature spells +15% power, maximum HP +10% |
| 7 | Nature spell damage +15% | **Deep Roots** (passive): your healing over time lasts 1 turn longer and heals 20% more (new) |
| 9 | **Call of the Ancient Pack** (skill, once a day, 2 AP): summon 2 animals | **Call the Grove** (skill, capstone; once per battle, 2 AP): summon a treant ally, its level half yours, for the battle (new) |

Why the change at 9: the draft found Call of the Ancient Pack to be the Beastmaster's Call the Pack. Its animal ally
becomes a treant, the forest's own, which keeps the identity without copying the Beastmaster.

### Cryomancer

Identity: The Ice mage: freezes enemies in place and weathers their attacks.

| Level | Before | Remade |
|---|---|---|
| 2 | Wisdom +5% | Wisdom and Intelligence +5% |
| 3 | **Flash Freeze** (skill, once per battle, 2 AP): stun one enemy for 1 turn | **Flash Freeze** (skill, once per battle, 2 AP): stun one enemy for 1 turn |
| 5 | **Permafrost** (passive): Ice spells +10% power, 5% less damage taken | **Permafrost** (passive): Ice spells +10% power, 5% less damage taken |
| 7 | Ice spell damage +15% | **Frostbite** (passive): Ice spells slow their target: 10% less Speed for 2 turns |
| 9 | **Absolute Dominion** (skill, once a day; 15% Mana, 3 AP): stun every enemy for 1 turn | **Absolute Dominion** (skill, capstone; once a day; 15% Mana, 3 AP): stun every enemy for 1 turn |

### Electromancer

Identity: The Lightning mage: overcharged bursts and storms over every enemy.

| Level | Before | Remade |
|---|---|---|
| 2 | Intelligence +5% | Intelligence and Speed +5% |
| 3 | **Voltaic Overcharge** (skill, once per battle): next Lightning spell +50% power, 20% more Mana | **Voltaic Overcharge** (skill, once per battle): next Lightning spell +50% power, 20% more Mana |
| 5 | **Static Momentum** (passive): Lightning spells +20% power; Speed +5% | **Static Momentum** (passive): Lightning spells +15% power; Speed +5% |
| 7 | Lightning spell damage +15% | **Arc** (passive): a Lightning spell on one enemy has a 20% chance to jump to a second for half its damage (new) |
| 9 | **Chain Overload** (skill, once a day; 12% Mana, 2 AP): Lightning damage to every enemy | **Chain Overload** (skill, capstone; once per battle; 12% Mana, 2 AP): Lightning damage to every enemy |

Static Momentum's power comes down 5 points to pay for Arc, as Perfect Formula's did for the Hierophant's Resonance.

### Geomancer

Identity: The Earth mage: stone defence and heavy, armour-breaking strikes.

| Level | Before | Remade |
|---|---|---|
| 2 | Vitality +5% | Vitality and Wisdom +5% |
| 3 | **Stoneform** (skill, once per battle): 3 turns of +50% Armor and −25% Evasion | **Stoneform** (skill, once per battle): 3 turns of +50% Armor and −25% Evasion |
| 5 | **Stone Soul** (passive): Armor +15%; 25% less Earth spell damage taken | **Stone Soul** (passive): Armor +15%; 25% less Earth spell damage taken |
| 7 | Earth spell damage +15% | **Tremor** (passive): Earth spells lower their target's Armor by 10% for 2 turns |
| 9 | **Worldshaker** (skill, once a day; 10% Mana, 2 AP): heavy Earth damage to one enemy, 25% Armor Pierce | **Worldshaker** (skill, capstone; once per battle; 10% Mana, 2 AP): heavy Earth damage to one enemy, 25% Armor Pierce |

### Hemomancer

Identity: The Blood mage: spends its own blood as fuel for its magic.

| Level | Before | Remade |
|---|---|---|
| 2 | Vitality +5% | Vitality and Intelligence +5% |
| 3 | **Blood Channel** (skill, once per battle): give 15% of maximum HP (not below 1) for 30% Mana | **Blood Channel** (skill, once per battle): give 15% of maximum HP (not below 1) for 30% Mana |
| 5 | **Blood Is Power** (passive): Blood spells +25% power; maximum HP +5% | **Blood Is Power** (passive): Blood spells +20% power; maximum HP +5% |
| 7 | Blood spell damage +15% | **Sanguine Flow** (passive): Blood spells heal you for 10% of the damage they deal (new) |
| 9 | **Crimson Apotheosis** (skill, once a day): give 25% HP for 50% Mana; 3 turns of +60% Blood spell power and +15% all stats | **Crimson Apotheosis** (skill, capstone; once a day): give 25% HP for 50% Mana; 3 turns of +60% Blood spell power and +15% all stats |

Blood Is Power comes down 5 points to pay for Sanguine Flow, which gives back some of the blood it spends.

### Luxomancer

Identity: The Holy mage: heals the party and wards off darkness.

| Level | Before | Remade |
|---|---|---|
| 2 | Mind +5% | Mind and Wisdom +5% |
| 3 | **Healing Halo** (skill, once per battle, 1 AP): heal every ally (grows with Mind) | **Healing Halo** (skill, once per battle, 1 AP): heal every ally (grows with Mind) |
| 5 | **Sacred Radiance** (passive): Holy spells +15% power; 20% less Dark spell damage taken | **Sacred Radiance** (passive): Holy spells +15% power; 20% less Dark spell damage taken |
| 7 | Holy spell damage +15% | **Light That Mends** (passive): your damaging Holy spells heal the ally with the least HP for 10% of the damage (new) |
| 9 | **Miracle** (skill, once a day; 15% Mana, 2 AP): greatly heal every ally and remove one debuff from each | **Miracle** (skill, capstone; once a day; 15% Mana, 2 AP): greatly heal every ally and remove one debuff from each |

### Necromancer

Identity: The Death mage: raises the undead to fight for it.

| Level | Before | Remade |
|---|---|---|
| 2 | Mind +5% | Mind and Intelligence +5% |
| 3 | **Raise Thrall** (skill, once per battle, 2 AP): summon an undead thrall (two can stand at once) | **Raise Thrall** (skill, once per battle, 2 AP): summon an undead thrall (two can stand at once) |
| 5 | **Grave Authority** (passive): Death spells +15% power; summons +15% base stats | **Grave Authority** (passive): Death spells +15% power; summons +15% base stats |
| 7 | Death spell damage +15% | **Death's Toll** (passive): each enemy that dies restores 5% of your maximum Mana and gives your summons 5% more stats for the battle, up to 25% (new) |
| 9 | **Army of the Dead** (skill, once a day; 15% Mana, 3 AP): summon 3 undead thralls | **Army of the Dead** (skill, capstone; once a day; 15% Mana, 3 AP): summon 3 undead thralls |

### Pyromancer

Identity: The Fire mage: raw destruction at the price of its own safety.

| Level | Before | Remade |
|---|---|---|
| 2 | Intelligence +5% | Intelligence and Mind +5% |
| 3 | **Kindle** (skill, once per battle): next Fire spell +40% power, 25% less Mana | **Kindle** (skill, once per battle): next Fire spell +40% power, 25% less Mana |
| 5 | **Kindled Soul** (passive): Fire spells deal 15% more damage; 20% less Fire spell damage taken | **Kindled Soul** (passive): Fire spells deal 15% more damage; 20% less Fire spell damage taken |
| 7 | Fire spell damage +15% | **Ablaze** (passive): Fire spells set their target burning: 2% of its maximum HP at the start of each of its turns for 2 turns (new) |
| 9 | **Living Inferno** (skill, once a day): 3 turns of +50% Fire power, 25% less Mana; you take 10% more damage | **Living Inferno** (skill, capstone; once per battle): 3 turns of +40% Fire power, 25% less Mana; you take 10% more damage |

### Umbramancer

Identity: The Dark mage: hides in shadow and weakens every enemy.

| Level | Before | Remade |
|---|---|---|
| 2 | Intelligence +5% | Intelligence and Mind +5% |
| 3 | **Shadow Veil** (skill, once per battle): 2 turns of +50% Evasion and +20% Dark spell power | **Shadow Veil** (skill, once per battle): 2 turns of +50% Evasion and +20% Dark spell power |
| 5 | **Eclipse Born** (passive): Dark spells +20% power; Holy spells deal 10% more to you | **Eclipse Born** (passive): Dark spells +15% power; Holy spells deal 10% more to you |
| 7 | Dark spell damage +15% | **Creeping Dark** (passive): Dark spells lower their target's Accuracy by 10% for 2 turns |
| 9 | **Total Eclipse** (skill, once a day; 12% Mana, 2 AP): every enemy loses 20% of all stats for 3 turns | **Total Eclipse** (skill, capstone; once per battle; 12% Mana, 2 AP): every enemy loses 15% of all stats for 3 turns |

Eclipse Born comes down 5 points to pay for Creeping Dark.

## Avatar

4 levels · rarity purple (raised on your note) · Requires: Level 1 · At least 4 core elemental mages at Level 5
Identity: Master of the six core elements: wields them all at once and always finds the enemy's weakness.

| Level | Before | Remade |
|---|---|---|
| 1 | Core elemental spells +20% power and +10% damage | Core elemental spells +20% power and +10% damage |
| 2 | Core elemental spells cost 15% less; 15% less damage from those elements | Core elemental spells cost 15% less; 15% less damage from those elements |
| 3 | **Prismatic Soul** (passive): core elemental spells count as all six elements and use the target's lowest resistance | **Prismatic Soul** (passive): core elemental spells count as all six elements and use the target's lowest resistance |
| 4 | **Avatar State** (skill, once a day): 3 turns of core elemental spells +60% power, 30% less Mana, 1 fewer AP | **Avatar State** (skill, capstone; once per battle): 3 turns of core elemental spells +40% power, 30% less Mana, 1 fewer AP |

Why: it fits; Avatar State works every battle at a step less power, a purple class's peak now that its rarity rose.

## Stormcaller

7 levels · rarity purple · Requires: Level 30 · Aeromancer 5 · Electromancer 5
Identity: Wind and Lightning in rhythm: alternates the two to build up a storm.

| Level | Before | Remade |
|---|---|---|
| 1 | Wind and Lightning spell power +5% | Wind and Lightning spell power +5% |
| 2 | Speed +5% | Speed +5% |
| 3 | **Storm Rhythm** (passive): alternating Wind and Lightning spells builds up to 20 charges, each +5% spell power | **Storm Rhythm** (passive): alternating Wind and Lightning spells builds up to 25 charges, each +4% spell power |
| 4 | Wind and Lightning spells cost 5% less | Wind and Lightning spells cost 5% less |
| 5 | Lightning spell power +5% | Wind and Lightning spell power +5% |
| 6 | Wind spell power +5% | A Wind spell makes your next Lightning spell cost 10% less, and the other way round (new) |
| 7 | **Stormburst** (skill, once per battle): spend all charges: 10 damage per charge to every enemy; allies' turn bars +60 per charge | **Stormburst** (skill, capstone; once per battle): spend all charges: Lightning damage of 8 per charge plus 0.5× Intelligence to every enemy; allies' turn bars +40 per charge |

Why: Storm Rhythm keeps its peak of +100% spell power (the user's note: "I want Stormcaller to peak out at +100% spell
power"), now over 25 charges of 4% instead of 20 of 5%, so the storm takes a little longer to build. Stormburst's flat
damage grows with the caster, and level 6 rewards the rhythm itself. Its two single-element levels merge.

## Sunstrider

6 levels · rarity purple · Requires: Level 25 · Luxomancer 5 · Priest 5
Identity: The sun's holy power: strongest by day.

| Level | Before | Remade |
|---|---|---|
| 1 | Holy spells 10% cheaper by day, 5% at night | Holy spells 10% cheaper by day, 5% at night |
| 2 | Speed +5% by day | Speed +5% by day |
| 3 | **Solar Reserve** (passive): Holy spell power +15% by day (06:00–18:00), +5% at night | **Solar Reserve** (passive): Holy spell power +15% by day (06:00–18:00), +5% at night |
| 4 | Maximum Mana +10% | Maximum Mana +10% |
| 5 | Holy spells 5% cheaper at all hours | By day your healing spells heal 15% more (new) |
| 6 | **Sunflare** (skill, once per battle): 30 Holy damage to every enemy by day, 15 at night | **Sunflare** (skill, capstone; once per battle): Holy damage of 30 plus 0.5× Wisdom to every enemy, half again as much by day |

Why: Sunflare's flat 30 damage was nothing at the levels a purple class is played; it now grows with the caster. The
second cheaper-spells level gives way to stronger healing by day, for a class that comes through the Priest.

## Plaguebinder

6 levels · rarity blue (raised on your note) · Requires: Level 25 · Warlock 5 or Necromancer 5
Identity: Spreads sickness: poisons and curses that jump from enemy to enemy.

| Level | Before | Remade |
|---|---|---|
| 1 | Dark and Death spell power +5% | Dark and Death spell power +5% |
| 2 | Mind +5% | Mind +5% |
| 3 | **Contagion** (passive): when an enemy dies poisoned or cursed, what's left spreads to one other enemy | **Contagion** (passive): when an enemy dies poisoned or cursed, what's left spreads to two other enemies |
| 4 | Dark and Death spells cost 5% less | Dark and Death spells cost 5% less |
| 5 | Spell power +5% | Your poisons and curses last 1 turn longer |
| 6 | **Pestilent Cloud** (skill, once per battle): poison every enemy for 6% of maximum HP a turn for 3 turns | **Pestilent Cloud** (skill, capstone; once per battle): poison every enemy for 6% of maximum HP a turn for 3 turns |

Why: it fits; now blue, its sickness spreads wider and lingers longer.

# Batch 6: priests and healers, summoners and dark pacts, leaders and minds (for review)

## Priest

7 levels · rarity no colour · Requires: Level 1 · Mage 5 · 3 Holy spells known
Identity: The holy healer: keeps the party alive and protected.

| Level | Before | Remade |
|---|---|---|
| 1 | Wisdom +5% | Wisdom and Mind +5% |
| 2 | Holy spells cost 10% less Mana | Holy spells cost 10% less Mana |
| 3 | Mind +5% | Your healing spells heal 10% more |
| 4 | **Blessed Vessel** (passive): maximum HP +10%; 15% less Dark spell damage | **Blessed Vessel** (passive): maximum HP +10%; 15% less Dark spell damage |
| 5 | Holy spells gain 10% power | Holy spells gain 10% power |
| 6 | Maximum Mana +10% | Maximum Mana +10% |
| 7 | **Sanctuary** (skill, once a day): 3 turns: you and every ally take 15% less damage and restore 5% HP each turn | **Sanctuary** (skill, capstone; once per battle): 3 turns: you and every ally take 15% less damage and restore 4% HP each turn |

Why: it fits; Sanctuary works every battle with slightly less healing, and the two small stats merge so level 3 makes
the healer heal better.

## Cardinal Priest

4 levels · rarity blue (raised in the draft) · Requires: Level 55 · Priest 7
Identity: The high priest: powerful holy magic and protection for the whole party.

| Level | Before | Remade |
|---|---|---|
| 1 | Holy spell power +20% | Holy spell power +20% |
| 2 | Wisdom +10%, maximum Mana +15% | Wisdom +10%, maximum Mana +15% |
| 3 | **Cardinal's Benediction** (passive): every party member's spells cost 10% less Mana; Holy spells cost you half to learn | **Cardinal's Benediction** (passive): every party member's spells cost 10% less Mana; Holy spells cost you half to learn |
| 4 | **Pontifical Decree** (skill, once a day, 1 AP): 3 turns: every ally takes 25% less damage and restores 7% HP and 5% Mana each turn | **Pontifical Decree** (skill, capstone; once a day, 1 AP): 3 turns: every ally takes 25% less damage and restores 7% HP and 5% Mana each turn |

Why: unchanged. Its rarity rose to blue in the draft, and the Decree stays once a day as the big burst it is.

## Lifewarden

6 levels · rarity no colour · Requires: Level 30 · Priest 5 or Cleric 5
Identity: The keeper of life: heals so much that the excess turns into shields.

| Level | Before | Remade |
|---|---|---|
| 1 | Maximum Mana +5% | Maximum Mana +5%; spells cost 5% less Mana |
| 2 | Spells cost 5% less Mana | Your healing is 10% stronger |
| 3 | **Overflow Ward** (passive): healing beyond maximum HP becomes a barrier of up to 10% of maximum HP | **Overflow Ward** (passive): healing beyond maximum HP becomes a barrier of up to 10% of maximum HP |
| 4 | Maximum HP +5% | Maximum HP +5% |
| 5 | Holy and Water spell power +10% | Holy and Water spell power +10% |
| 6 | **Sanctuary Pulse** (skill, once per battle): heal every ally for 20% of maximum HP | **Sanctuary Pulse** (skill, capstone; once per battle): heal every ally for 20% of maximum HP |

Why: it fits; its two small levels merge so level 2 can make the healing stronger, which also fills the overflow
barriers sooner.

## Sanguine Medic

5 levels · rarity blue · Requires: Level 1 · Hemomancer 5 · Priest 5
Identity: Heals with its own blood, giving up its own health to save its allies.

| Level | Before | Remade |
|---|---|---|
| 1 | Maximum HP +5% | Maximum HP +10% |
| 2 | Blood and Holy spell power +5% | Blood and Holy spell power +5% |
| 3 | **Red Triage** (passive): healing you give is 20% stronger while you are below half HP | **Red Triage** (passive): healing you give is 20% stronger while you are below half HP |
| 4 | Maximum HP +5% | Each time you heal an ally, you regain 3% of your maximum HP (new) |
| 5 | **Blood Transfusion** (skill, once per battle): spend 15% of your maximum HP to restore 30% of an ally's | **Blood Transfusion** (skill, capstone; once per battle): spend 15% of your maximum HP to restore 30% of an ally's |

Why: it fits; the two HP levels merge, and level 4 gives back a little of the blood it spends, so it can keep giving.

## Summoner

7 levels · rarity no colour · Requires: Level 1 · Mage 5 · 10 summon spells used
Identity: Lets its creatures fight for it and stays safe behind them.

| Level | Before | Remade |
|---|---|---|
| 1 | Summon spells cost 30% less Mana | Summon spells cost 30% less Mana |
| 2 | Mind +5% | Mind and Speed +5% |
| 3 | **Overprotective** (passive): while one of your summons lives, you can't be attacked | **Overprotective** (passive): while one of your summons lives, enemies are far less likely to attack you, and you take 15% less damage |
| 4 | Speed +5% | Your summons' base stats +20% |
| 5 | **Double Summon** (skill, once a day): next summon spell costs no Mana and only 2 AP | Mind and maximum Mana +5% |
| 6 | Mind +5% | Your summons' turn bars start each battle a little filled (new) |
| 7 | **Powerful Friends** (passive): your summons' base stats +20% | **Double Summon** (skill, capstone; once per battle): your next summon spell costs no Mana and only 2 AP |

Why: it had two passives. Powerful Friends keeps its effect as a plain bonus (level 4) and Double Summon becomes the
capstone, every battle. The draft found Overprotective too strong (no attack could reach the Summoner at all); now it
still keeps the Summoner safe behind its creatures, but not untouchable.

## Beastmaster

7 levels · rarity no colour · Requires: Level 1 · Summoner 3 · At least 1 Nature summon spell known
Identity: Fights with animals at its side, never without a companion beast.

| Level | Before | Remade |
|---|---|---|
| 1 | Summon spells cost 15% less Mana | Summon spells cost 15% less Mana |
| 2 | Mind +5% | Mind +5% |
| 3 | **Animal Companion** (passive): each battle starts with a wolf, bear or griffon beside you, at most half your level | **Animal Companion** (passive): each battle starts with a wolf, bear or griffon beside you, at most half your level |
| 4 | Your summons' base stats +15% | Your summons' base stats +25% |
| 5 | Maximum Mana +10% | Maximum Mana +10% |
| 6 | Your summons' base stats +15% | Your animals can reach three quarters of your level instead of half |
| 7 | **Call the Pack** (skill, once a day): summon two wolves, bears or griffons, at most half your level | **Call the Pack** (skill, capstone; once per battle): summon two wolves, bears or griffons, at most your animals' level |

Why: it fits; Call the Pack works every battle, and the two summon-stat levels merge so level 6 can make its animals
grow up with it.

## Wildwarden

5 levels · rarity no colour · Requires: Level 1 · Beastmaster 5 · Know 1 Nature summon spell
Identity: The pack's protector: keeps its animal summons alive and strong.

| Level | Before | Remade |
|---|---|---|
| 1 | Nature summon spells cost 10% less Mana | Nature summon spells cost 10% less Mana |
| 2 | Armor +10% in forest zones | Armor +10%, another 10% in forest zones |
| 3 | **Pack Guardian** (passive): your animal summons take 10% less damage while you live | **Pack Guardian** (passive): your animal summons take 10% less damage while you live |
| 4 | Summon base stats +10% | Summon base stats +10% |
| 5 | **Shield the Pack** (skill, once per battle): your animal summons gain 20% Armor for 2 turns | **Shield the Pack** (skill, capstone; once per battle): your animal summons gain 25% Armor and take 10% less damage for 2 turns |

Why: it fits; level 2 no longer works only in forests, and its shield grows a little.

## Soul Shepherd

5 levels · rarity blue · Requires: Level 35 · Summoner 7
Identity: The shepherd of summoned souls: its summons are its strength, and even their loss feeds it.

| Level | Before | Remade |
|---|---|---|
| 1 | Summon base stats +10% | Summon base stats +15% |
| 2 | Summon spells cost 10% less | Summon spells cost 10% less |
| 3 | **Soul Reclaim** (passive): when one of your summons dies, recover 5% HP and Mana | **Soul Reclaim** (passive): when one of your summons dies, recover 5% HP and Mana |
| 4 | Summon base stats +10% | When one of your summons dies, your other summons gain 10% stats for the battle (new) |
| 5 | **Spirit Shelter** (skill, once per battle): your summons gain 25% Armor for 2 turns | **Guide the Soul** (skill, capstone; once per battle): bring back your last fallen summon at half its HP (new) |

Why: Spirit Shelter was the Wildwarden's Shield the Pack for every summon. The shepherd's own skill now brings a lost
soul back, and level 4 lets a loss strengthen the rest, as the identity says.

## Druid

7 levels · rarity no colour · Requires: Level 1 · Mage 5 · 3 Nature spells known
Identity: Nature's own caster: one with the wild, and nature answers when it is in danger.

| Level | Before | Remade |
|---|---|---|
| 1 | Nature spells cost 15% less Mana | Nature spells cost 15% less Mana |
| 2 | **One with the World** (skill, once a day): 5 turns of +10% all stats; Nature spells 15% cheaper and +30% power | **One with the World** (skill, once per battle): 3 turns of +10% all stats; Nature spells 15% cheaper and +25% power |
| 3 | Mind and Wisdom +5% | Mind and Wisdom +5% |
| 4 | Nature spells cost 50% less to learn | Nature spells cost 50% less to learn |
| 5 | **Preservation of Nature** (passive): 20% less Nature spell damage; Nature spells +20% power | Nature spells +20% power; 20% less Nature spell damage taken (no longer a passive) |
| 6 | Mind +5% | Mind +5% |
| 7 | **Call of Nature** (passive, once a day): below 10% HP, an animal at most half your level comes to your aid | **Call of Nature** (passive, capstone; once per battle): when you fall below 30% HP, an animal at most half your level comes to your aid and you restore 10% of your HP |

Why: it had two passives; Preservation keeps its effect as a plain bonus. Call of Nature needed the Druid below 10%
HP, once a day; now nature answers at 30%, every battle, and the Druid also heals. One with the World works every
battle, shorter and a little weaker.

## Warlock

7 levels · rarity no colour · Requires: Level 1 · Mage 5 · 3 Dark spells known
Identity: The Dark caster: draws power from a pact with darkness, at a risk to itself.

| Level | Before | Remade |
|---|---|---|
| 1 | Intelligence +5% | Intelligence and Mind +5% |
| 2 | Dark spells cost 10% less Mana | Dark spells cost 10% less Mana |
| 3 | Maximum Mana +10% | Maximum Mana +10% |
| 4 | **Pactbound** (passive): Dark spells +10% power; 15% less Dark spell damage taken | **Pactbound** (passive): Dark spells +10% power; 15% less Dark spell damage taken |
| 5 | Mind +5% | Dark spells deal 15% more damage while you are below half HP (new) |
| 6 | Dark spells cost 30% less to learn | Dark spells cost 30% less to learn |
| 7 | **Dark Ascendance** (skill, once per battle): 3 turns of Dark spells +30% power and 20% cheaper; you take 10% more damage | **Dark Ascendance** (skill, capstone; once per battle): 3 turns of Dark spells +30% power and 20% cheaper; you take 10% more damage |

Why: it fits; its two single stats merge, and level 5 is the pact's risk and reward: the darkness answers best when the
Warlock is hurt.

## Pactkeeper

4 levels · rarity blue · Requires: Level 30 · Warlock 7 · Summoner 5 · Cast the same summon 10 times
Identity: Bound to one familiar that returns every battle and grows over time.

| Level | Before | Remade |
|---|---|---|
| 1 | Summon base stats +10% | Summon base stats +10% |
| 2 | Maximum Mana +5% | Maximum Mana +5% |
| 3 | **Bound Familiar** (passive): a chosen summon returns at the start of every battle for 5% of your maximum HP and grows 2% per battle survived, up to 20% | **Bound Familiar** (passive): a chosen summon returns at the start of every battle for 5% of your maximum HP and grows 2% per battle survived, up to 20% |
| 4 | **Pact Renewal** (skill, once per battle): your summons restore 25% HP and Mana | **Pact Renewal** (skill, capstone; once per battle): your summons restore 25% HP and Mana |

Why: unchanged; it already fits its identity and the rules.

## Cultist

7 levels · rarity no colour · Requires: Level 1 · Use 10 Dark summon spells
Identity: Dark summoning rites: calls creatures cheaply and makes them stronger through ritual.

| Level | Before | Remade |
|---|---|---|
| 1 | Mind +5% | Mind +5% |
| 2 | Summon spells cost 20% less (30% for Dark ones) | Summon spells cost 20% less (30% for Dark ones) |
| 3 | **Summon Ritual** (skill, once per battle, 5 AP): next summon 50% cheaper, its creatures +10% all stats | **Summon Ritual** (skill, once per battle, 3 AP): your next summon is 50% cheaper and its creatures gain 10% all stats |
| 4 | Mind +5% | Intelligence and Wisdom +5% |
| 5 | Speed +5% | Speed and Mind +5% |
| 6 | **Dark Rituals** (passive): your other summons count as Dark | Your non-Dark summons count as Dark (no longer a passive) |
| 7 | Intelligence and Wisdom +5% | **Dark Communion** (passive, capstone): your Dark summons gain 15% all stats, and each one alive makes your spells 5% cheaper, up to 15% (new) |

Why: it broke the capstone rule (its skill at 3, its passive at 6, stats at 7). Dark Rituals keeps its effect as a plain
level, and the new capstone passive rewards keeping a dark host alive. The ritual cost 5 AP, a whole turn and more for
a small gain; 3 is a fair price.

## Chaos Cultist

3 levels · rarity blue (raised in the draft) · Requires: Level 50 · Cultist 7 · Use 100 Dark summon spells
Identity: The cult's high caller: opens every battle with a demon and can call a whole host.

| Level | Before | Remade |
|---|---|---|
| 1 | **Void Tear** (passive): each battle starts with a random Dark summon of Tier 3 or lower that you know | **Void Tear** (passive): each battle starts with a random Dark summon of Tier 4 or lower that you know |
| 2 | Mind +10% | Mind +10% |
| 3 | **Grand Demon Summoning** (skill, once a day, 5 AP): you are stunned next turn; your next summon costs no Mana, 5 AP, and brings one more creature | **Grand Demon Summoning** (skill, capstone; once a day, 4 AP): your next summon costs no Mana and brings one more creature |

Why: the summoning stunned its caller and took 10 AP in all, a price that outweighed the creature it added. Now it is a
plain once-a-day call for the host, and Void Tear reaches a tier higher, as a blue class's.

## Strategist

7 levels · rarity no colour · Requires: Level 20 · 15 defensive actions by the whole party
Identity: Commands the party: makes everyone quicker and better at defending, and directs the attack at one target.

| Level | Before | Remade |
|---|---|---|
| 1 | Every ally +5% Speed | Every ally +5% Speed |
| 2 | Mind +5% | Mind +5% |
| 3 | Every ally +5% Dexterity | Every ally +5% Dexterity |
| 4 | **Coordinated Defense** (passive): every ally's Guard and Evade give 15% more | **Coordinated Defense** (passive): every ally's Guard and Evade give 15% more |
| 5 | Every ally +5% Luck | Every ally +5% Luck |
| 6 | Evasion +20% | Evasion +20% |
| 7 | **Focus Fire** (skill, once per battle): mark one enemy; every ally deals 20% more to it until it falls | **Focus Fire** (skill, capstone; once per battle): mark one enemy; every ally deals 20% more to it until it falls |

Why: unchanged; it already fits its identity and the rules.

## General

5 levels · rarity blue · Requires: Level 50 · Strategist 7
Identity: The commander: keeps the party's tempo high and its line firm.

| Level | Before | Remade |
|---|---|---|
| 1 | Every ally another 5% Speed | Every ally another 5% Speed |
| 2 | Every ally's Guard and Evade another 15% | Every ally's Guard and Evade another 15% |
| 3 | Mind +10% | Mind +10% |
| 4 | **Battlefield Command** (passive): Focus Fire +10% more damage; each hit on the marked enemy brings the other allies' turns 100 closer | **Battlefield Command** (passive): Focus Fire +10% more damage; each hit on the marked enemy brings the other allies' turns 100 closer |
| 5 | **Rally the Line** (skill, once per battle): every ally +25% Armor and Evasion for 2 turns | **Rally the Line** (skill, capstone; once per battle): every ally +25% Armor and Evasion for 2 turns |

Why: unchanged; it already fits its identity and the rules.

## Flagbearer

4 levels · rarity blue · Requires: Level 1 · Strategist 5 · 2 companions
Identity: Carries the colours: strengthens its companions and raises a standard the enemy must break.

| Level | Before | Remade |
|---|---|---|
| 1 | Living companions +5% Strength and Dexterity | Living companions +5% Strength and Dexterity |
| 2 | Armor +10% | Armor +10% |
| 3 | **Rallying Colors** (passive): living companions +5% Speed while you stand | **Rallying Colors** (passive): while you stand, living companions gain 10% Speed and deal 5% more damage |
| 4 | **Raise Standard** (skill, once per battle): a fragile standard: allies deal 15% more damage while it stands | **Raise Standard** (skill, capstone; once per battle): a fragile standard: allies deal 15% more damage while it stands |

Why: it fits; its passive was a small +5% Speed for a blue class and now carries the colours properly.

## Enlightened

5 levels · rarity no colour · Requires: Level 10 · 25 defensive actions
Identity: Insight in defence: guards and dodges well and reads what the enemy will do.

| Level | Before | Remade |
|---|---|---|
| 1 | Guard and Evade give 10% more | Guard and Evade give 10% more |
| 2 | Mind +5% | Mind +5% |
| 3 | **Read Intent** (skill, once per battle, free): see an enemy's most likely next action | **Read Intent** (skill, once per battle, free): see an enemy's most likely next action |
| 4 | Guard and Evade give another 10% | Guard and Evade give another 10% |
| 5 | **Measured Defense** (passive): 10% less damage while Guarding or Evading | **Measured Defense** (passive, capstone): 10% less damage while Guarding or Evading |

Why: unchanged; it already fits its identity and the rules.

## Diviner

7 levels · rarity blue · Requires: Level 35 · Enlightened 5
Identity: Foresight: predicts the enemy's next move and is nearly untouchable when right.

| Level | Before | Remade |
|---|---|---|
| 1 | Mind +10% | Mind +10% |
| 2 | Guard and Evade give 15% more | Guard and Evade give 15% more |
| 3 | Evasion +20% | Evasion +20% |
| 4 | **Foretell** (skill, once per battle): predict an enemy's next action; right: +75% Armor and Evasion for 2 turns; wrong: +10% for 1 turn | **Foretell** (skill, once per battle): predict an enemy's next action; right: +75% Armor and Evasion for 2 turns; wrong: +10% for 1 turn |
| 5 | Armor +20% | Armor +20% |
| 6 | Guard and Evade give another 15% | Guard and Evade give another 15% |
| 7 | **Perfect Foresight** (passive): a right Foretell gives +125% Armor and Evasion and 20% less damage for 2 turns | **Perfect Foresight** (passive, capstone): a right Foretell gives +125% Armor and Evasion and 20% less damage for 2 turns |

Why: unchanged; it already fits its identity and the rules.

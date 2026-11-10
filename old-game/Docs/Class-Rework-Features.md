# Task 10, Class Rework: the features, a first sample of four

**For the user's review (written 2026-11-10).** The features round begins with four classes remade as a sample, drawn
at random as the user asked: a starting class (**Warrior**), a class from a rare item (**Vampire**), an elemental mage
(**Hierophant**) and any class (**Reaver**). Each keeps its identity, levels, requirements and rarity from the draft
(`Class-Rework-Draft.md`), uses its old features as the reference, and gives its passive or its skill at its last level
(the capstone rule). Nothing is built in the game yet; the class files change once the user approves.

**How to read it:** each class has a table of its levels, the old feature beside the remade one, then why. Numbers are
proposals to tune when built. "(new)" marks something the game can't do yet and would need code; everything else uses
effects the game already has.

**What the sample tries out, for the user to judge before the other classes follow:**
- Every level does something worth taking; repeated small bonuses are merged rather than spread over several levels.
- Skills that worked once a day become once per battle where they aren't a big burst, so they are used, not saved.
- Penalties that made a feature miss more than it helped (Bloodfeed's −40% Accuracy) are gone.
- Power follows the rarity: Warrior (no colour) stays modest, Vampire (blue in the draft) and Reaver (blue) get more.
- Hierophant is a template: the twelve elemental mages share one shape today, so if its new shape is approved, the
  other eleven would get the same shape with their own element's flavour.

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

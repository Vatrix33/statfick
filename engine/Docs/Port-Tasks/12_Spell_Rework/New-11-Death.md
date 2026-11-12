# New spells 11: Death (task 12)

**Status: approved (2026-11-12, SP59), for the port (SP2, SP3).** Death's fourteen new spells: two in each of Tiers 1–5, one in
each of Tiers 6–9, among them **Death's new summon** (the task gives Nature and Death one each). They follow the
approved system and curve (`Spell-Augmentation.md`), the sample's style and the element statuses
(`Element-Statuses.md`). Prices are the tier's usual. [X] values are for the raw caster of the system draft (no classes
or gear) at the level the tier unlocks and at Level 100, for a Wisdom-first caster.

## What Death gains

- **Summon - Wraith** (Tier 7): the game's Duskpine Wraith, incorporeal (weapons deal 30% less to it), filling the gap
  between the Undead Hulk and the Pale Monarch. Raise Dead (Tier 5) and Danse Macabre (Tier 9) also raise the battle's
  fallen enemies as thralls.
- **The battle's dead:** Corpse Burst (a fallen enemy burst over every enemy), Bone Armor (stronger for each death),
  Harvest of Souls (HP and Mana for each death).
- **Withering, spreading:** Grave Touch (2 AP), Wail (no damage, every enemy), Plague (Withers one, spreads each turn),
  Necrotic Wave (4 AP, every enemy for 3 turns).
- **Rot and doom:** Rot (damage over time), Grim Toll (more against the half-dead), Requiem Mark (triples doom on one
  enemy), Reaper's Scythe (2 AP doom at Tier 8). Bosses still can't be defeated outright (SP46).

New mechanics: a status that spreads to another enemy each turn, a corpse burst, gains for each enemy death, doom
chances tripled on a marked enemy, raising several fallen enemies at once, an incorporeal summon.

## At a glance

| Tier | Spell | Kind | AP | Mana | What it does |
|---|---|---|---|---|---|
| 1 | Grave Touch | offensive, one target | 2 | 8 | 2 AP, 30% Wither |
| 1 | Wail | offensive, all enemies | 3 | 8 | 25% chance to Wither each enemy, no damage |
| 2 | Rot | offensive, one target | 3 | 22 | rots it: 60% of [X] at each of its next 3 turns |
| 2 | Bone Armor | defensive, yourself or an ally | 3 | 22 | Armor +25%, +5% more per enemy fallen this battle, 3 turns |
| 3 | Corpse Burst | offensive, all enemies | 3 | 35 | uses up a fallen enemy to hit every enemy |
| 3 | Plague | offensive, one target, spreading | 3 | 35 | Withers it; the Wither spreads to another enemy each turn |
| 4 | Harvest of Souls | defensive, yourself | 2 | 35 | 2 AP: 3 turns: every enemy death gives you 10% HP and 10% Mana |
| 4 | Grim Toll | offensive, all enemies | 3 | 50 | +30% on enemies below half HP |
| 5 | Raise Dead | summon | 3 | 75 | raises up to two fallen enemies as thralls |
| 5 | Requiem Mark | offensive, one target | 3 | 75 | 3 turns: your side's doom on it is tripled (bosses can't be defeated outright) |
| 6 | Necrotic Wave | offensive, all enemies | 4 | 120 | 4 AP: always Withers every enemy for 3 turns |
| 7 | Summon - Wraith | summon | 3 | 140 | Death's new summon: a wraith weapons hardly touch; 40% Wither |
| 8 | Reaper's Scythe | offensive, one target | 2 | 126 | 2 AP: 20 in 1000 to die, 40 if Withered |
| 9 | Danse Macabre | offensive, all enemies | 3 | 250 | every fallen enemy rises for your side (up to 4); every enemy Withered |

## The spells

### Grave Touch: Tier 1, offensive, one target (new spell)

- **The spell:** 2 AP, 8 Mana, 700 silver; power 4 + Wis ×0.23 + Mind ×0.07 (65% of the tier's reference, the 2 AP rule). A 30% chance to **Wither** the target for 2 turns.
- **Description:** "Touch one enemy with the grave's cold for [X] Death damage, with a 30% chance to Wither it. This spell costs 2 AP."
- **[X]:** Level 1: 7; Level 100: 102.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Death's fast spell at Tier 1 (Eerie Shock is the Tier 2 one).

### Wail: Tier 1, offensive, all enemies (no damage) (new spell)

- **The spell:** 3 AP, 8 Mana, 700 silver. Each enemy has a 25% chance to be **Withered** for 2 turns.
- **Description:** "Let out a deathly wail; each enemy has a 25% chance to be Withered for 2 turns."
- **Augments:** Effect duration, Effect chance, Mana cost.
- **Why:** The cheapest way to touch every enemy with the element's status.

### Rot: Tier 2, offensive, one target (new spell)

- **The spell:** 3 AP, 22 Mana, 1,450 silver; power 4 + Wis ×0.27 + Mind ×0.08 (50% of the tier's reference, now; 30% of the reference at each of its next 3 turns). Rot takes hold (new): at the start of each of its next 3 turns the target takes 60% of [X] again; it is **Withered** for those turns.
- **Description:** "Set rot in one enemy for [X] Death damage; it rots for 60% of that at the start of each of its next 3 turns, Withered all the while."
- **[X]:** Level 10: 18; Level 100: 119.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** Death's damage over time.

### Bone Armor: Tier 2, defensive, yourself or an ally (new spell)

- **The spell:** 3 AP, 22 Mana, 1,450 silver. For 3 turns: Armor +25%, plus 5% for every enemy defeated this battle (up to +25% more).
- **Description:** "Clad yourself or an ally in bone for 3 turns: Armor +25%, and 5% more for every enemy that has fallen this battle."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Death's guard, stronger as the battle's dead pile up.

### Corpse Burst: Tier 3, offensive, all enemies (needs a fallen enemy) (new spell)

- **The spell:** 3 AP, 35 Mana, 3,300 silver; power 10 + Wis ×0.62 + Mind ×0.18 (80% of the tier's reference, for all enemies, as it needs a corpse). Uses up a fallen enemy (as Call of the Dead does) and bursts it over every enemy (new); each struck has a 30% chance to be **Withered** for 2 turns.
- **Description:** "Burst a fallen enemy's corpse over all enemies for [X] Death damage, each with a 30% chance to be Withered. Needs a fallen enemy, which it uses up."
- **[X]:** Level 20: 67; Level 100: 273.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** A strong group spell that needs the battle's dead.

### Plague: Tier 3, offensive, one target, spreading (new spell)

- **The spell:** 3 AP, 35 Mana, 3,300 silver; power 9 + Wis ×0.54 + Mind ×0.15 (70% of the tier's reference). Always **Withers** the target for 3 turns; at the start of each of its turns while Withered, the plague spreads to another random enemy, Withering it for 2 turns (new).
- **Description:** "Infect one enemy with plague for [X] Death damage: it is Withered for 3 turns, and at each of its turns the plague spreads to another enemy."
- **[X]:** Level 20: 59; Level 100: 237.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** A single cast that reaches the whole group in time.

### Harvest of Souls: Tier 4, defensive, yourself (new spell)

- **The spell:** 2 AP, 35 Mana, 6,000 silver. For 3 turns, whenever an enemy dies, you regain 10% of your maximum HP and 10% of your maximum Mana (new).
- **Description:** "Ready yourself to harvest souls for 3 turns: whenever an enemy dies, you regain 10% of maximum HP and Mana. This spell costs 2 AP."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** A Death caster's reward for a fight against many.

### Grim Toll: Tier 4, offensive, all enemies (new spell)

- **The spell:** 3 AP, 50 Mana, 6,000 silver; power 10 + Wis ×0.56 + Mind ×0.16 (60% of the tier's reference, 75% for all enemies, less the bonus). Deals 30% more to enemies below half of their HP.
- **Description:** "Ring a grim toll over all enemies for [X] Death damage, 30% more against those below half of their HP."
- **[X]:** Level 30: 85; Level 100: 247.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** A group finisher for a worn-down enemy side.

### Raise Dead: Tier 5, summon (needs fallen enemies) (new spell)

- **The spell:** 3 AP, 75 Mana, 8,800 silver; power 21 + Wis ×1.22 + Mind ×0.35 (100% of the tier's reference, as a summon, for each thrall). Uses up to two fallen enemies and raises each as an Undead Thrall for your side (Call of the Dead's thrall, at this spell's [X]: its maximum HP rises by 200% of [X] and its attacks by 25% of [X]); its hits have a 30% chance to **Wither**. Up to two at a time.
- **Description:** "Raise up to two fallen enemies as friendly Undead Thralls whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more, each with a 30% chance to Wither. Up to two may be active at a time."
- **[X]:** Level 40: 235; Level 100: 538.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Call of the Dead for a battle's many dead.

### Requiem Mark: Tier 5, offensive, one target (new spell)

- **The spell:** 3 AP, 75 Mana, 8,800 silver; power 16 + Wis ×0.92 + Mind ×0.26 (75% of the tier's reference). Marks the target for 3 turns: every chance your side has to defeat it outright is tripled; bosses can't be defeated outright (SP46).
- **Description:** "Mark one enemy for a requiem for [X] Death damage: for 3 turns, your side's chances to defeat it outright are tripled. Bosses can't be."
- **[X]:** Level 40: 177; Level 100: 405.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** Ties Death's doom spells together on the one enemy that matters.

### Necrotic Wave: Tier 6, offensive, all enemies (new spell)

- **The spell:** 4 AP, 120 Mana, 11,700 silver; power 24 + Wis ×1.4 + Mind ×0.4 (95% of the tier's reference, the 4 AP rule on an all-enemies spell). A heavy 4 AP wave; every enemy struck is always **Withered** for 3 turns.
- **Description:** "Send a necrotic wave through all enemies for [X] Death damage, Withering every one of them for 3 turns. This spell costs 4 AP."
- **[X]:** Level 50: 327; Level 100: 617.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** Death's biggest group hit below Tier 10.

### Summon - Wraith: Tier 7, summon (new spell)

- **The spell:** 3 AP, 140 Mana, 17,500 silver; power 31 + Wis ×1.82 + Mind ×0.52 (100% of the tier's reference, as a summon). Summons a Wraith (the game's Duskpine Wraith, Level 69, 451 HP today); its maximum HP rises by 200% of [X] and every attack it makes by 25% of [X] (SP29). Being incorporeal, it takes 30% less weapon damage (new); its hits have a 40% chance to **Wither** their target for 2 turns. One at a time.
- **Description:** "Summon a friendly Wraith whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more, each with a 40% chance to Wither; weapons pass half through it, dealing 30% less. Only one may be active at a time."
- **[X]:** Level 60: 500; Level 100: 802.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Death's new summon (the task), filling the gap between the Undead Hulk (Tier 5) and the Pale Monarch (Tier 9), from a creature the game already has.

### Reaper's Scythe: Tier 8, offensive, one target (new spell)

- **The spell:** 2 AP, 126 Mana, 25,000 silver; power 23 + Wis ×1.37 + Mind ×0.39 (65% of the tier's reference, the 2 AP rule). A 2 AP spell; a 20 in 1000 chance to defeat the target outright, 40 if it is **Withered**; bosses can't be defeated outright (SP46).
- **Description:** "Swing the reaper's scythe at one enemy for [X] Death damage, with a 20 in 1000 chance to defeat it outright (40 if it is Withered). Bosses can't be. This spell costs 2 AP."
- **[X]:** Level 70: 433; Level 100: 603.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** Death's fast spell for the high tiers.

### Danse Macabre: Tier 9, offensive, all enemies (new spell)

- **The spell:** 3 AP, 250 Mana, 35,000 silver; power 22 + Wis ×1.31 + Mind ×0.38 (50% of the tier's reference, 75% for all enemies, less its raising). Every enemy struck is **Withered** for 3 turns, and every fallen enemy rises to fight for your side (up to 4 at a time, as Undead Thralls at this spell's [X]).
- **Description:** "Begin the danse macabre over all enemies for [X] Death damage: every one is Withered for 3 turns, and every fallen enemy rises to dance for you (up to four)."
- **[X]:** Level 80: 469; Level 100: 578.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** Death's great raising: a battle's dead turned on the living.

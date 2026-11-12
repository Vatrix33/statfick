# Batch 11: Death (task 12)

**Status: a draft for the user's review (SP2).** Every Death spell players can learn, reworked: 32 spells (Death has no
boss-only spell). They follow the approved system and curve (`Spell-Augmentation.md`), the sample's style
(`Spell-Sample.md`) and the element statuses (`Element-Statuses.md`): each spell sets its status's chance and duration
(SP32). Tiers and prices stay. "(new)" marks a mechanic the game doesn't have yet. [X] values are for the raw caster of
the system draft (no classes or gear) at the level the tier unlocks and at Level 100, shown for a Wisdom-first caster
as Death's spells scale with Wisdom and Mind.
**Updated 2026-11-12 (SP60):** some spells here borrow another element's status; each says so ("borrowed, SP60").

## Death's identity

The element of withering and doom:
- **Withered** (its status: healing received −30%) and **Exsanguinate** (its reaction: on a Bleeding enemy, Blood's
  status, the rest of the bleed is dealt at once). Blood's **Blood Harvest** feeds on Withered enemies, and Holy's
  **Purify** ends Withered for extra damage. Health Grasp and Deathwave Wither for certain, Withering Choir often;
  Soul Rend, Mourning Bell and Soul Leech pay it off.
- **Doom:** chances to defeat an enemy outright, Death's old mark, now surer against Withered enemies (Soul Extinction,
  Last Requiem, Gravesong), with a sentence (Grave Sentence: below 20% HP at its next turn, it dies) and the Pale
  Monarch doubling them. **Bosses can't be defeated outright** (new: today they can; this is the rule Basic's Axiom
  Lance already follows), so doom is for the courts and the wilds, not the bosses.
- **The dead serve:** Call of the Dead still raises a thrall from a fallen enemy, Sepulcher Verdict raises one from
  whatever it kills, Grave Chorus grows with every death in the battle, Pallbearer's Touch heals the party when its
  target dies.
- **Guards:** Death Ward and Necropolis Aegis keep Withering (and, for Death Ward, instant death) away; Tomb Ward cuts
  all damage; Grave Aegis becomes a barrier. Flat Armor goes.
- Today six Death spells all lowered Strength and five all lowered healing received; Withered takes over healing loss,
  and Strength loss stays on Pale Requiem, Ossuary Storm and the Dragonbreath Grave Tempest. Sudden Heart Attack stays
  as it is.

New mechanics this batch adds for the port: bosses safe from instant death, doom surer on Withered targets, a delayed
sentence below a share of HP, a thrall raised from a kill, damage that grows with the deaths in a battle, a mark that
heals the party when its target dies, immunity to Withered and to instant death, a summon that doubles its side's doom.

## At a glance

| Tier | Spell | Kind | AP | Mana | What changes |
|---|---|---|---|---|---|
| 1 | Dimming Touch | offensive, one target | 3 | 14 | 40% chance to Wither |
| 1 | Sudden Heart Attack | offensive, one target | 3 | 1 | kept: a Luck-based chance in 1000 to die; bosses can't be defeated outright |
| 1 | Deadly Glare | offensive, one target | 3 | 12 | can't flee for 4 turns (kept), 30% Wither |
| 1 | Death Bolt | offensive, one target | 3 | 14 | 20% chance to Wither |
| 2 | Summon - Call of the Dead | summon | 3 | 18 | raises a thrall from a fallen enemy, growing with spell power; 30% Wither |
| 2 | Health Grasp | offensive, one target | 3 | 24 | always Withers for 3 turns |
| 2 | Eerie Shock | offensive, one target | 2 | 15 | 2 AP kept, 25% Wither |
| 2 | Soul Leech | offensive, one target | 3 | 25 | feeds on the Withered: 30% of the damage as Mana |
| 3 | Soul Rend | offensive, one target | 3 | 36 | +30% on a Withered target |
| 3 | Grave Chill | offensive, all enemies | 3 | 37 | 30% Wither, 30% Chill on each enemy (borrowed status) |
| 3 | Death Ward | defensive, yourself | 3 | 32 | −20% spell damage; immune to Withered and instant death |
| 3 | Pale Requiem | offensive, all enemies | 3 | 38 | Strength −10% on every enemy (kept) |
| 4 | Deathwave | offensive, all enemies | 3 | 55 | always Withers every enemy |
| 4 | Soul Extinction | offensive, one target | 3 | 50 | 20 in 1000 to die, 40 if Withered; bosses can't be defeated outright |
| 4 | Grave Aegis | defensive, yourself or an ally | 3 | 45 | a barrier and Holy −30% |
| 4 | Grave Lance | offensive, one target | 3 | 52 | Vitality −10% kept, 30% Wither |
| 5 | Summon - Undead Hulk | summon | 3 | 82 | a tank: 20% less weapon damage; 40% Wither |
| 5 | Withering Choir | offensive, all enemies | 3 | 76 | 50% chance to Wither each enemy for 3 turns |
| 5 | Grave Sentence | offensive, one target | 3 | 75 | if below 20% HP at its next turn, it dies; bosses can't be defeated outright |
| 5 | Dragonbreath - Sepulchral Miasma | offensive, all enemies | 3 | 78 | healing received −20% kept (Dragonbreath), 25% Wither |
| 5 | Mourning Bell | offensive, all enemies | 3 | 77 | +25% on Withered enemies |
| 6 | Dragonbreath - Grave Tempest | offensive, all enemies | 3 | 96 | Strength −20% kept (Dragonbreath), 30% Wither |
| 6 | Sepulcher Verdict | offensive, one target | 3 | 104 | 30 in 1000 to die; a kill raises an Undead Thrall |
| 6 | Necropolis Aegis | defensive, all allies | 3 | 100 | every ally: −20% weapon damage, immune to Withered, 3 turns |
| 7 | Grave Chorus | offensive, all enemies | 3 | 134 | +10% for every enemy defeated this battle (up to +50%) |
| 7 | Pallbearer's Touch | offensive, one target | 3 | 146 | marks it: when it dies within 3 turns, every ally heals 10% |
| 7 | Tomb Ward | defensive, all allies | 3 | 140 | every ally takes 15% less of all damage for 2 turns |
| 8 | Ossuary Storm | offensive, all enemies | 3 | 170 | Strength −25% kept, 30% Wither |
| 8 | Last Requiem | offensive, one target | 3 | 190 | 40 in 1000 to die, 80 if Withered |
| 9 | Gravesong | offensive, all enemies | 3 | 250 | 50% Wither; every Withered enemy 30 in 1000 to die |
| 9 | Summon - Pale Monarch | summon | 3 | 250 | while it stands, your side's instant-death chances are doubled |
| 10 | Silence at the End | offensive, one target | 3 | 350 | 80 in 1000 to die (kept), always Withers; a kill Withers every enemy |

## The spells

### Dimming Touch: Tier 1, offensive, one target

- **Now:** 3 AP, 14 Mana, power 6 + Wis ×0.4 + Mind ×0.1. Drain one enemy for [X] Death damage and reduce its Strength by 5% for 2 turns.
- **Reworked:** 3 AP, 14 Mana; power 5 + Wis ×0.3 + Mind ×0.09 (85% of the tier's reference). A 40% chance to **Wither** the target for 2 turns (was Strength −5%).
- **Description:** "Drain one enemy with a dimming touch for [X] Death damage, with a 40% chance to Wither it for 2 turns."
- **[X], now → reworked:** Level 1: 11 → 9; Level 100: 172 → 133.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** The Tier 1 spell built around Withering.

### Sudden Heart Attack: Tier 1, offensive, one target

- **Now:** 3 AP, 1 Mana, power 0 + Luck ×0.1 + - ×None. Send out a small burst of Death Mana. The target has a [X] in 1000 chance to die instantly.
- **Reworked:** 3 AP, 1 Mana. Kept as it is (1 Mana; the target has a chance in 1000 equal to a tenth of your Luck to die at once), except that bosses can't be defeated outright (new: today they can; Basic's Axiom Lance follows the same rule).
- **Description:** "Send out a small burst of Death Mana. The target has a [X] in 1000 chance to die instantly. Bosses can't be."
- **Augments:** Effect chance, Mana cost.
- **Why:** Already one of a kind: Death's lottery ticket.

### Deadly Glare: Tier 1, offensive, one target

- **Now:** 3 AP, 12 Mana, power 0 + Wis ×0.3 + - ×None. Glare at an enemy with Death magic, dealing [X] Death damage. The target cannot flee for 4 turns.
- **Reworked:** 3 AP, 12 Mana; power 5 + Wis ×0.3 + Mind ×0.09 (85% of the tier's reference). It can't flee for 4 turns (kept); a 30% chance to **Wither** it for 2 turns.
- **Description:** "Glare at one enemy with Death magic for [X] Death damage; it can't flee for 4 turns and has a 30% chance to be Withered."
- **[X], now → reworked:** Level 1: 3 → 9; Level 100: 107 → 133.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Kept, with the element's status (today it has no base power).

### Death Bolt: Tier 1, offensive, one target

- **Now:** 3 AP, 14 Mana, power 10 + Wis ×0.5 + - ×None. A bolt of Death Mana that deals [X] Death damage.
- **Reworked:** 3 AP, 14 Mana; power 6 + Wis ×0.33 + Mind ×0.1 (95% of the tier's reference). A 20% chance to **Wither** the target for 3 turns.
- **Description:** "A bolt of Death Mana that deals [X] Death damage, with a 20% chance to Wither the target for 3 turns."
- **[X], now → reworked:** Level 1: 15 → 10; Level 100: 188 → 147.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Death's plain bolt, with the element's status.

### Summon - Call of the Dead: Tier 2, summon (needs a fallen enemy)

- **Now:** 3 AP, 18 Mana, power none. Consume a defeated enemy to summon a friendly Undead Thrall.
- **Reworked:** 3 AP, 18 Mana; power 9 + Wis ×0.54 + Mind ×0.16 (100% of the tier's reference, as a summon). Summons an Undead Thrall (Level 10, 94 HP today) from a fallen enemy, which it uses up (kept); its maximum HP rises by 200% of [X] and every attack it makes by 25% of [X] (SP29). Its hits have a 30% chance to **Wither** their target for 2 turns. One at a time.
- **Description:** "Consume a defeated enemy to raise a friendly Undead Thrall whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more, each with a 30% chance to Wither. Only one may be active at a time."
- **[X], now → reworked:** Level 10: - → 36; Level 100: - → 239.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Its corpse mechanic stays; the thrall now grows with spell power.

### Health Grasp: Tier 2, offensive, one target

- **Now:** 3 AP, 24 Mana, power 10 + Wis ×0.2 + - ×None. Deal [X] Death damage and reduce the target's healing received by 50% for 2 turns.
- **Reworked:** 3 AP, 24 Mana; power 7 + Wis ×0.43 + Mind ×0.13 (80% of the tier's reference). Always **Withers** the target for 3 turns (was healing received −50% for 2 turns).
- **Description:** "Grasp one enemy's health for [X] Death damage and Wither it for 3 turns."
- **[X], now → reworked:** Level 10: 18 → 29; Level 100: 81 → 191.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Death's sure single Wither.

### Eerie Shock: Tier 2, offensive, one target

- **Now:** 2 AP, 22 Mana, power 8 + Wis ×0.5 + - ×None. Create a sudden strike of Death Mana that deals [X] Death damage. This spell costs 2 Action Points.
- **Reworked:** 2 AP, 15 Mana; power 6 + Wis ×0.35 + Mind ×0.1 (65% of the tier's reference, the 2 AP rule). Stays a 2 AP spell; a 25% chance to **Wither** the target for 2 turns.
- **Description:** "Create a sudden strike of Death Mana for [X] Death damage, with a 25% chance to Wither the target. This spell costs 2 AP."
- **[X], now → reworked:** Level 10: 29 → 24; Level 100: 186 → 154.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Death's fast spell; its power and Mana follow the 2 AP rule.

### Soul Leech: Tier 2, offensive, one target

- **Now:** 3 AP, 25 Mana, power 12 + Wis ×0.5 + Mind ×0.1. Drain one enemy for [X] Death damage and siphon Mana equal to 20% of the damage dealt.
- **Reworked:** 3 AP, 25 Mana; power 8 + Wis ×0.46 + Mind ×0.14 (85% of the tier's reference). Against a **Withered** target you regain Mana equal to 30% of the damage (new; was a 20% siphon against anyone).
- **Description:** "Leech one enemy's soul for [X] Death damage; if it is Withered, you regain Mana equal to 30% of the damage."
- **[X], now → reworked:** Level 10: 36 → 32; Level 100: 213 → 205.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Death feeds on what it has already weakened.

### Soul Rend: Tier 3, offensive, one target

- **Now:** 3 AP, 36 Mana, power 34 + Wis ×0.6 + Mind ×0.1. Tear at the target's life force for [X] Death damage and reduce healing received by 60% for 3 turns.
- **Reworked:** 3 AP, 36 Mana; power 12 + Wis ×0.69 + Mind ×0.2 (90% of the tier's reference). Deals 30% more to a **Withered** target (was healing received −60% for 3 turns).
- **Description:** "Tear at one enemy's life force for [X] Death damage, 30% more if it is Withered."
- **[X], now → reworked:** Level 20: 85 → 76; Level 100: 271 → 305.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** The single-target payoff for Withered.

### Grave Chill: Tier 3, offensive, all enemies

- **Now:** 3 AP, 37 Mana, power 26 + Wis ×0.6 + Mind ×0.1. Release the chill of the grave, dealing [X] Death damage to all enemies and decreasing their Speed by 10% for 2 turns.
- **Reworked:** 3 AP, 37 Mana; power 8 + Wis ×0.5 + Mind ×0.14 (65% of the tier's reference, 75% for all enemies, less its effect). Each enemy struck has a 30% chance to be **Withered** for 2 turns and a 30% chance to be **Chilled** (Ice's status, borrowed, SP60; 1 stack) for 2 turns (was Speed −10%).
- **Description:** "Release the chill of the grave over all enemies for [X] Death damage; each has a 30% chance to be Withered and a 30% chance to be Chilled."
- **[X], now → reworked:** Level 20: 77 → 54; Level 100: 263 → 219.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Speed loss belongs to Water and Ice.

### Death Ward: Tier 3, defensive, yourself

- **Now:** 3 AP, 32 Mana, power 30 + Wis ×0.6 + Mind ×0.1. Surround yourself with grave energy for 3 turns, increasing Armor by [X] and reducing all spell damage by 20%.
- **Reworked:** 3 AP, 32 Mana. For 3 turns: 20% less spell damage, and you can't be Withered or defeated outright (new; was Armor +[X] and −20% spell damage).
- **Description:** "Surround yourself with grave energy for 3 turns: 20% less spell damage, and neither Withering nor instant death can touch you."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** A guard against Death's own weapons in enemy hands.

### Pale Requiem: Tier 3, offensive, all enemies

- **Now:** 3 AP, 38 Mana, power 28 + Wis ×0.6 + Mind ×0.1. Sweep all enemies with grave-cold for [X] Death damage and reduce their Strength by 10% for 2 turns.
- **Reworked:** 3 AP, 38 Mana; power 9 + Wis ×0.54 + Mind ×0.15 (70% of the tier's reference, 75% for all enemies, less the weakening). Every enemy's Strength −10% for 2 turns (kept: the one early Strength loss of the six Death spells that had one).
- **Description:** "Sweep all enemies with grave-cold for [X] Death damage and reduce their Strength by 10% for 2 turns."
- **[X], now → reworked:** Level 20: 79 → 59; Level 100: 265 → 237.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Kept; most of Death's other Strength-lowering spells now do something else.

### Deathwave: Tier 4, offensive, all enemies

- **Now:** 3 AP, 55 Mana, power 35 + Wis ×0.7 + Mind ×0.1. Release a wave of deathly force for [X] Death damage to all enemies and reduce their healing received by 50% for 2 turns.
- **Reworked:** 3 AP, 55 Mana; power 10 + Wis ×0.56 + Mind ×0.16 (60% of the tier's reference, 75% for all enemies, less its sure Wither). Always **Withers** every enemy struck for 2 turns (was healing received −50%).
- **Description:** "Release a wave of deathly force on all enemies for [X] Death damage, Withering every one of them for 2 turns."
- **[X], now → reworked:** Level 30: 120 → 85; Level 100: 308 → 247.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Death's sure group Wither.

### Soul Extinction: Tier 4, offensive, one target

- **Now:** 3 AP, 50 Mana, power 45 + Wis ×0.7 + Mind ×0.1. Tear deeply into an enemy's soul for [X] Death damage with an additional 10 in 1000 chance to defeat it instantly.
- **Reworked:** 3 AP, 50 Mana; power 14 + Wis ×0.8 + Mind ×0.23 (85% of the tier's reference). A 20 in 1000 chance to defeat it outright, 40 if it is **Withered** (was 10); bosses can't be defeated outright (new: today they can; Basic's Axiom Lance follows the same rule).
- **Description:** "Tear deeply into one enemy's soul for [X] Death damage, with a 20 in 1000 chance to defeat it outright (40 if it is Withered). Bosses can't be."
- **[X], now → reworked:** Level 30: 130 → 121; Level 100: 318 → 353.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** Death's doom, surer on what it has weakened.

### Grave Aegis: Tier 4, defensive, yourself or an ally

- **Now:** 3 AP, 45 Mana, power 40 + Wis ×0.7 + Mind ×0.1. Wrap yourself in grave energy for 3 turns, increasing Armor by [X] and reducing Holy spell damage by 30%.
- **Reworked:** 3 AP, 45 Mana; power 13 + Wis ×0.75 + Mind ×0.22 (80% of the tier's reference, as a barrier). A **barrier** of [X] for 3 turns and 30% less Holy damage (was Armor +[X] on yourself).
- **Description:** "Wrap yourself or an ally in grave energy for 3 turns: a barrier that absorbs [X] damage, and 30% less Holy damage."
- **[X], now → reworked:** Level 30: 125 → 114; Level 100: 313 → 332.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** Flat Armor becomes a barrier, as in the earlier batches.

### Grave Lance: Tier 4, offensive, one target

- **Now:** 3 AP, 52 Mana, power 48 + Wis ×0.7 + Mind ×0.1. Pierce one enemy for [X] Death damage and reduce its Vitality by 10% for 2 turns.
- **Reworked:** 3 AP, 52 Mana; power 14 + Wis ×0.8 + Mind ×0.23 (85% of the tier's reference). Vitality −10% for 2 turns (kept); a 30% chance to **Wither** the target for 2 turns.
- **Description:** "Pierce one enemy with a grave lance for [X] Death damage, reducing its Vitality by 10% for 2 turns, with a 30% chance to Wither it."
- **[X], now → reworked:** Level 30: 133 → 121; Level 100: 321 → 353.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Kept, with the element's status.

### Summon - Undead Hulk: Tier 5, summon

- **Now:** 3 AP, 82 Mana, power none. Summon a friendly Undead Hulk. Only one may be active at a time.
- **Reworked:** 3 AP, 82 Mana; power 21 + Wis ×1.22 + Mind ×0.35 (100% of the tier's reference, as a summon). Summons an Undead Hulk (Level 23, 297 HP today); its maximum HP rises by 200% of [X] and every attack it makes by 25% of [X] (SP29). It takes 20% less weapon damage (new); its hits have a 40% chance to **Wither** their target for 2 turns. One at a time.
- **Description:** "Summon a friendly Undead Hulk whose maximum HP rises by twice [X], whose attacks deal a quarter of [X] more, each with a 40% chance to Wither, and who takes 20% less weapon damage. Only one may be active at a time."
- **[X], now → reworked:** Level 40: - → 235; Level 100: - → 538.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Death's tank, growing with spell power.

### Withering Choir: Tier 5, offensive, all enemies

- **Now:** 3 AP, 76 Mana, power 42 + Wis ×0.8 + Mind ×0.1. Sweep all enemies with a deathly chorus for [X] Death damage and reduce their Strength by 15% for 3 turns.
- **Reworked:** 3 AP, 76 Mana; power 13 + Wis ×0.73 + Mind ×0.21 (60% of the tier's reference, 75% for all enemies, less its likely Wither). Each enemy struck has a 50% chance to be **Withered** for 3 turns (was Strength −15%).
- **Description:** "Sweep all enemies with a withering choir for [X] Death damage; each has a 50% chance to be Withered for 3 turns."
- **[X], now → reworked:** Level 40: 169 → 141; Level 100: 350 → 323.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Its name promised Withering.

### Grave Sentence: Tier 5, offensive, one target

- **Now:** 3 AP, 75 Mana, power 61 + Wis ×0.8 + Mind ×0.1. Condemn one enemy for [X] Death damage and gain an additional 20 in 1000 chance to defeat it instantly.
- **Reworked:** 3 AP, 75 Mana; power 19 + Wis ×1.1 + Mind ×0.32 (90% of the tier's reference). Sentenced: if the target is below 20% of its maximum HP at the start of its next turn, it is defeated (new; was a 20 in 1000 chance); bosses can't be defeated outright (new: today they can; Basic's Axiom Lance follows the same rule).
- **Description:** "Condemn one enemy for [X] Death damage; if it is below 20% of its maximum HP when its next turn begins, it dies. Bosses can't be."
- **[X], now → reworked:** Level 40: 188 → 212; Level 100: 369 → 486.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** A sentence the party can carry out together.

### Dragonbreath - Sepulchral Miasma: Tier 5, offensive, all enemies (Dragonbreath)

- **Now:** 3 AP, 78 Mana, power 40 + Wis ×0.8 + Mind ×0.1. Exhale grave mist across all enemies for [X] Death damage and reduce their healing received by 20% for 2 turns.
- **Reworked:** 3 AP, 78 Mana; power 14 + Wis ×0.79 + Mind ×0.23 (65% of the tier's reference, 75% for all enemies, less its effects). Its weakening effect stays (healing received −20% for 2 turns); each enemy struck has a 25% chance to be **Withered** for 2 turns.
- **Description:** "Exhale a sepulchral miasma over all enemies for [X] Death damage, reducing their healing received by 20% for 2 turns, with a 25% chance to Wither each."
- **[X], now → reworked:** Level 40: 167 → 153; Level 100: 348 → 350.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Dragonbreath spells keep their weakening effect.

### Mourning Bell: Tier 5, offensive, all enemies

- **Now:** 3 AP, 77 Mana, power 42 + Wis ×0.8 + Mind ×0.1. Sound a deathly toll over all enemies for [X] Death damage and reduce their Intelligence by 15% for 2 turns.
- **Reworked:** 3 AP, 77 Mana; power 14 + Wis ×0.79 + Mind ×0.23 (65% of the tier's reference, 75% for all enemies, less the bonus). Enemies that are **Withered** take 25% more (was Intelligence −15%).
- **Description:** "Sound a deathly toll over all enemies for [X] Death damage, 25% more against those that are Withered."
- **[X], now → reworked:** Level 40: 169 → 153; Level 100: 350 → 350.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** The group payoff for a withered field.

### Dragonbreath - Grave Tempest: Tier 6, offensive, all enemies (Dragonbreath)

- **Now:** 3 AP, 96 Mana, power 52 + Wis ×0.9 + Mind ×0.1. Exhale a deathly storm across all enemies for [X] Death damage and reduce their Strength by 20% for 3 turns.
- **Reworked:** 3 AP, 96 Mana; power 16 + Wis ×0.96 + Mind ×0.27 (65% of the tier's reference, 75% for all enemies, less its effects). Its weakening effect stays (Strength −20% for 3 turns); each enemy struck has a 30% chance to be **Withered** for 2 turns.
- **Description:** "Exhale a deathly storm across all enemies for [X] Death damage, reducing their Strength by 20% for 3 turns, with a 30% chance to Wither each."
- **[X], now → reworked:** Level 50: 227 → 223; Level 100: 396 → 422.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Dragonbreath spells keep their weakening effect.

### Sepulcher Verdict: Tier 6, offensive, one target

- **Now:** 3 AP, 104 Mana, power 70 + Wis ×0.9 + Mind ×0.1. Condemn one enemy for [X] Death damage with an additional 25 in 1000 chance to defeat it instantly.
- **Reworked:** 3 AP, 104 Mana; power 22 + Wis ×1.32 + Mind ×0.38 (90% of the tier's reference). A 30 in 1000 chance to defeat it outright (was 25); bosses can't be defeated outright (new: today they can; Basic's Axiom Lance follows the same rule). If the spell defeats its target, an Undead Thrall rises from it for your side (Call of the Dead's thrall, at this spell's [X]; new).
- **Description:** "Condemn one enemy to the sepulcher for [X] Death damage, with a 30 in 1000 chance to defeat it outright (bosses can't be); whatever it kills rises as your Undead Thrall."
- **[X], now → reworked:** Level 50: 245 → 308; Level 100: 414 → 582.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** Death's verdict raises its own servants.

### Necropolis Aegis: Tier 6, defensive, all allies

- **Now:** 3 AP, 100 Mana, power 58 + Wis ×0.9 + Mind ×0.1. Grant all allies [X] Armor and reduce incoming weapon damage by 20% for 2 turns.
- **Reworked:** 3 AP, 100 Mana. For 3 turns, every ally takes 20% less weapon damage and can't be Withered (was Armor +[X] and −20% for 2 turns).
- **Description:** "Raise the necropolis around the party for 3 turns: 20% less weapon damage, and no Withering can take hold."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Flat Armor goes; the guard stays.

### Grave Chorus: Tier 7, offensive, all enemies

- **Now:** 3 AP, 134 Mana, power 64 + Wis ×0.9 + Mind ×0.2. Batter all enemies for [X] Death damage and lower Strength by 20% for 2 turns.
- **Reworked:** 3 AP, 134 Mana; power 19 + Wis ×1.09 + Mind ×0.31 (60% of the tier's reference, 75% for all enemies, less the bonus). Deals 10% more for every enemy defeated this battle, up to +50% (new; was Strength −20%).
- **Description:** "Batter all enemies with the grave's chorus for [X] Death damage, 10% more for every enemy that has fallen this battle (up to half again)."
- **[X], now → reworked:** Level 60: 287 → 300; Level 100: 431 → 480.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** The chorus swells with every death: Death's finisher for a long fight.

### Pallbearer's Touch: Tier 7, offensive, one target

- **Now:** 3 AP, 146 Mana, power 82 + Wis ×0.9 + Mind ×0.2. Deal [X] Death damage to one enemy with an additional 20 in 1000 chance to defeat it instantly.
- **Reworked:** 3 AP, 146 Mana; power 29 + Wis ×1.73 + Mind ×0.49 (95% of the tier's reference). Marks the target for 3 turns: if it dies while marked, every ally heals 10% of their maximum HP (new; was a 20 in 1000 chance).
- **Description:** "Lay the pallbearer's touch on one enemy for [X] Death damage; if it dies within 3 turns, every ally regains 10% of maximum HP."
- **[X], now → reworked:** Level 60: 305 → 474; Level 100: 449 → 761.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** Death that sustains the living.

### Tomb Ward: Tier 7, defensive, all allies

- **Now:** 3 AP, 140 Mana, power 64 + Wis ×0.9 + Mind ×0.2. Grant all allies [X] Armor for 2 turns.
- **Reworked:** 3 AP, 140 Mana. For 2 turns, every ally takes 15% less damage of every kind (was Armor +[X]).
- **Description:** "Seal the party in a tomb ward for 2 turns: they take 15% less damage of every kind."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Flat Armor goes; a cut that also works against spells.

### Ossuary Storm: Tier 8, offensive, all enemies

- **Now:** 3 AP, 170 Mana, power 70 + Wis ×1 + Mind ×0.3. Pummel all enemies for [X] Death damage and lower Strength by 25% for 2 turns.
- **Reworked:** 3 AP, 170 Mana; power 22 + Wis ×1.26 + Mind ×0.36 (60% of the tier's reference, 75% for all enemies, less its effects). Every enemy's Strength −25% for 2 turns (kept); each has a 30% chance to be **Withered** for 2 turns.
- **Description:** "Pummel all enemies with an ossuary storm for [X] Death damage, lowering their Strength by 25% for 2 turns, with a 30% chance to Wither each."
- **[X], now → reworked:** Level 70: 371 → 399; Level 100: 496 → 556.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Kept as Death's great weakening of enemy fighters.

### Last Requiem: Tier 8, offensive, one target

- **Now:** 3 AP, 190 Mana, power 90 + Wis ×1 + Mind ×0.3. Deal [X] Death damage to one enemy with an additional 30 in 1000 chance to defeat it instantly.
- **Reworked:** 3 AP, 190 Mana; power 36 + Wis ×2.1 + Mind ×0.6 (100% of the tier's reference). A 40 in 1000 chance to defeat it outright, 80 if it is **Withered** (was 30); bosses can't be defeated outright (new: today they can; Basic's Axiom Lance follows the same rule).
- **Description:** "Sing the last requiem over one enemy for [X] Death damage, with a 40 in 1000 chance to defeat it outright (80 if it is Withered). Bosses can't be."
- **[X], now → reworked:** Level 70: 391 → 664; Level 100: 516 → 926.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** Soul Extinction at its height.

### Gravesong: Tier 9, offensive, all enemies

- **Now:** 3 AP, 250 Mana, power 90 + Wis ×1.1 + Mind ×0.3. Assail all enemies for [X] Death damage and lower Strength by 20% for 2 turns.
- **Reworked:** 3 AP, 250 Mana; power 25 + Wis ×1.44 + Mind ×0.41 (55% of the tier's reference, 75% for all enemies, less its effects). Each enemy struck has a 50% chance to be **Withered** for 3 turns, and every enemy that is Withered has a 30 in 1000 chance to be defeated outright (new; was Strength −20%); bosses can't be defeated outright (new: today they can; Basic's Axiom Lance follows the same rule).
- **Description:** "Sing the gravesong over all enemies for [X] Death damage; each has a 50% chance to be Withered, and every Withered one has a 30 in 1000 chance to die outright. Bosses can't be."
- **[X], now → reworked:** Level 80: 462 → 515; Level 100: 552 → 635.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Death's doom on a whole group.

### Summon - Pale Monarch: Tier 9, summon

- **Now:** 3 AP, 250 Mana, power none. Summon a friendly Pale Monarch, an ancient undead sovereign. Only one may be active at a time.
- **Reworked:** 3 AP, 250 Mana; power 45 + Wis ×2.62 + Mind ×0.75 (100% of the tier's reference, as a summon). Summons the Pale Monarch (Level 80, 730 HP today), an ancient undead sovereign; its maximum HP rises by 200% of [X] and every attack it makes by 25% of [X] (SP29). Its hits have a 40% chance to **Wither** their target for 2 turns; while it stands, every chance its side has to defeat an enemy outright is doubled (new). One at a time.
- **Description:** "Summon the Pale Monarch, an ancient undead sovereign whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more, each with a 40% chance to Wither; while it stands, your side's chances to defeat enemies outright are doubled. Only one may be active at a time."
- **[X], now → reworked:** Level 80: - → 938; Level 100: - → 1,155.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Death's sovereign makes the element's doom surer while it reigns.

### Silence at the End: Tier 10, offensive, one target

- **Now:** 3 AP, 350 Mana, power 370 + Wis ×1.2 + Mind ×0.4. Deal [X] Death damage to one enemy with an additional 80 in 1000 chance to defeat it instantly, then reduce its healing received by 50% for 2 turns.
- **Reworked:** 3 AP, 350 Mana; power 62 + Wis ×3.62 + Mind ×1.03 (90% of the tier's reference). An 80 in 1000 chance to defeat it outright (kept; bosses can't be defeated outright (new: today they can; Basic's Axiom Lance follows the same rule)); it is always **Withered** for 3 turns (was healing received −50% for 2); if it dies, every other enemy is Withered for 3 turns (new).
- **Description:** "Bring the silence at the end on one enemy for [X] Death damage, with an 80 in 1000 chance to defeat it outright (bosses can't be); it is Withered for 3 turns, and if it falls, every other enemy is Withered too."
- **[X], now → reworked:** Level 90: 840 → 1,444; Level 100: 891 → 1,594.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Death's Tier 10 stays a single-target doom, now spreading its silence.

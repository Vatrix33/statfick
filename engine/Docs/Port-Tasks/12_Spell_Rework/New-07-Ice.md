# New spells 7: Ice (task 12)

**Status: a draft for the user's review (SP2, SP3).** Ice's fourteen new spells: two in each of Tiers 1–5, one in each
of Tiers 6–9. They follow the approved system and curve (`Spell-Augmentation.md`), the sample's style and the element
statuses (`Element-Statuses.md`). Prices are the tier's usual. [X] values are for the raw caster of the system draft (no
classes or gear) at the level the tier unlocks and at Level 100.
**Updated 2026-11-12 (SP61):** some spells here borrow another element's status; each says so ("borrowed, SP61").

## What Ice gains

- **Protection, Ice's way:** Ice Block (no damage for a turn, at the cost of that turn), Cryostasis (the same for an
  ally, who thaws healed and cleansed), Ice Mirror (a decoy), Frost Armor (an ally's guard), Winter's Embrace (a party
  heal with Ice's stand against Fire).
- **Chill, sooner and steadier:** Frost Nip (2 AP), Frost Weapon (for the party's fighters), Icicle Volley, Numbing
  Frost (sure), Frozen Ground (a field that chills each turn), Frost Nova (one more stack on every enemy).
- **Big ice:** Glacier Crash (Ice's first 4 AP spell), Rime Spike (2 AP, shatters), Frozen Tomb (a freeze for 2 turns,
  half resisted by bosses).

New mechanics: taking no damage for a turn at the cost of the turn (on yourself or an ally), a decoy mirror, a field that
chills at each enemy turn, a two-turn freeze.

## At a glance

| Tier | Spell | Kind | AP | Mana | What it does |
|---|---|---|---|---|---|
| 1 | Frost Nip | offensive, one target | 2 | 8 | 2 AP, 30% Chill |
| 1 | Frost Weapon | offensive, yourself or an ally | 3 | 12 | 2 turns: their weapon attacks +[X] Ice, 25% Chill |
| 2 | Icicle Volley | offensive, random enemies | 3 | 22 | three icicles at random enemies, 20% Chill and 15% Bleed each (borrowed status) |
| 2 | Frost Armor | defensive, yourself or an ally | 3 | 22 | Armor +20%; melee attackers 40% Chilled, 3 turns |
| 3 | Ice Block | defensive, yourself | 2 | 25 | 2 AP: no damage until your next turn, but you skip it |
| 3 | Numbing Frost | offensive, one target | 3 | 35 | always Chills for 3 turns; Accuracy −10% |
| 4 | Ice Mirror | defensive, yourself | 3 | 50 | a mirror image: the next 2 single-target attacks at you 50% likely to hit it |
| 4 | Frozen Ground | offensive, the whole field | 3 | 50 | 3 turns: each enemy 40% likely to be Chilled at the start of its turns |
| 5 | Cryostasis | defensive, an ally | 3 | 75 | freezes an ally: they skip their next turn, take no damage, heal 25%, lose their debuffs |
| 5 | Glacier Crash | offensive, one target | 4 | 90 | 4 AP: a glacier on one enemy, Chills twice |
| 6 | Winter's Embrace | defensive, all allies | 3 | 100 | heals every ally; no Chill, Fire −20%, 3 turns |
| 7 | Frost Nova | offensive, all enemies | 3 | 140 | every enemy Chilled once more for certain |
| 8 | Rime Spike | offensive, one target | 2 | 126 | 2 AP: always Chills; shatters a Frozen target (+50%) |
| 9 | Frozen Tomb | offensive, one target | 3 | 250 | freezes one enemy for 2 turns; it takes half of [X] again as it thaws |

## The spells

### Frost Nip: Tier 1, offensive, one target (new spell)

- **The spell:** 2 AP, 8 Mana, 700 silver; power 4 + Int ×0.23 + Mind ×0.07 (65% of the tier's reference, the 2 AP rule). A 30% chance to **Chill** the target (1 stack) for 2 turns.
- **Description:** "Nip one enemy with frost for [X] Ice damage, with a 30% chance to Chill it. This spell costs 2 AP."
- **[X]:** Level 1: 7; Level 100: 102.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Ice's fast spell at Tier 1 (Sudden Spike is the Tier 2 one).

### Frost Weapon: Tier 1, offensive, yourself or an ally (new spell)

- **The spell:** 3 AP, 12 Mana, 700 silver; power 2 + Int ×0.1 + Str ×0.03 (30% of the tier's reference, per weapon attack). For 2 turns, the weapon attacks of yourself or an ally each deal [X] more Ice damage and have a 25% chance to **Chill** the target (1 stack, 2 turns).
- **Description:** "Rime the weapon of yourself or an ally for 2 turns: each attack deals [X] more Ice damage, with a 25% chance to Chill."
- **[X]:** Level 1: 3; Level 100: 41.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Ice's help for the party's weapon users, whose many blows stack Chill fast.

### Icicle Volley: Tier 2, offensive, random enemies (new spell)

- **The spell:** 3 AP, 22 Mana, 1,450 silver; power 3 + Int ×0.15 + Mind ×0.04 (28% of the tier's reference, per icicle; three, 90% in all). Three icicles (new) each strike a random enemy for [X], each with a 20% chance to **Chill** it (1 stack, 2 turns). Each icicle also has a 15% chance to make its target **Bleed** (Blood's status, borrowed, SP61) for 3 turns.
- **Description:** "Loose three icicles at random enemies, each dealing [X] Ice damage with a 20% chance to Chill its target. Each icicle also has a 15% chance to make its target Bleed."
- **[X]:** Level 10: 10; Level 100: 66 (each icicle).
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Several early rolls for Chill on a group.

### Frost Armor: Tier 2, defensive, yourself or an ally (new spell)

- **The spell:** 3 AP, 22 Mana, 1,450 silver. For 3 turns: Armor +20%, and melee attackers have a 40% chance to be **Chilled** (1 stack, 2 turns).
- **Description:** "Coat yourself or an ally in frost armor for 3 turns: Armor +20%, and melee attackers have a 40% chance to be Chilled."
- **Augments:** Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Icebound Aegis for an ally, a tier earlier.

### Ice Block: Tier 3, defensive, yourself (new spell)

- **The spell:** 2 AP, 25 Mana, 3,300 silver. You encase yourself in ice (new): you take no damage until the end of your next turn, but you can't act on it; your debuffs don't count down meanwhile.
- **Description:** "Encase yourself in a block of ice: you take no damage until the end of your next turn, but you can't act on it. This spell costs 2 AP."
- **Augments:** Mana cost.
- **Why:** An escape for a caster about to fall: a turn traded for safety.

### Numbing Frost: Tier 3, offensive, one target (new spell)

- **The spell:** 3 AP, 35 Mana, 3,300 silver; power 10 + Int ×0.62 + Mind ×0.18 (80% of the tier's reference). Always **Chills** the target (1 stack) for 3 turns, and its Accuracy −10% for 2 turns (numbed fingers).
- **Description:** "Numb one enemy with frost for [X] Ice damage: it is Chilled for 3 turns and its Accuracy drops by 10% for 2 turns."
- **[X]:** Level 20: 67; Level 100: 273.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** A sure single Chill that also blunts an enemy's weapon.

### Ice Mirror: Tier 4, defensive, yourself (new spell)

- **The spell:** 3 AP, 50 Mana, 6,000 silver. Raises a mirror of ice (new): the next 2 single-target attacks or spells aimed at you within 3 turns each have a 50% chance to strike the mirror instead.
- **Description:** "Raise a mirror of ice beside you: within 3 turns, the next two attacks or spells aimed at you each have an even chance to strike the mirror instead."
- **Augments:** Effect duration, Mana cost.
- **Why:** A decoy for a caster the enemy has singled out.

### Frozen Ground: Tier 4, offensive, the whole field (new spell)

- **The spell:** 3 AP, 50 Mana, 6,000 silver. For 3 turns the ground freezes (new): at the start of each of its turns, every enemy has a 40% chance to be **Chilled** (1 stack, 2 turns).
- **Description:** "Freeze the ground under the enemies for 3 turns: at the start of each of their turns, each has a 40% chance to be Chilled."
- **Augments:** Effect duration, Effect chance, Mana cost.
- **Why:** Ice's field, which keeps stacking Chill while the party does other things.

### Cryostasis: Tier 5, defensive, an ally (new spell)

- **The spell:** 3 AP, 75 Mana, 8,800 silver. Freezes an ally in stasis (new): they take no damage until the end of their next turn and can't act on it; when it ends they regain 25% of their maximum HP and all their debuffs end.
- **Description:** "Freeze an ally in stasis: until the end of their next turn they take no damage and can't act; then they thaw with 25% of their maximum HP restored and every debuff gone."
- **Augments:** Mana cost.
- **Why:** Ice Block for an ally, a rescue that also cleanses and heals.

### Glacier Crash: Tier 5, offensive, one target (new spell)

- **The spell:** 4 AP, 90 Mana, 8,800 silver; power 27 + Int ×1.59 + Mind ×0.45 (130% of the tier's reference, the 4 AP rule). A heavy 4 AP strike; it always **Chills** the target with 2 stacks for 2 turns.
- **Description:** "Drop a glacier on one enemy for [X] Ice damage, Chilling it twice over. This spell costs 4 AP."
- **[X]:** Level 40: 305; Level 100: 699.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Ice's first 4 AP spell: a big hit that brings the target to the edge of freezing.

### Winter's Embrace: Tier 6, defensive, all allies (new spell)

- **The spell:** 3 AP, 100 Mana, 11,700 silver; power 14 + Int ×0.81 + Mind ×0.23 (55% of the tier's reference, 75% for all allies, less its wards). Heals every ally for [X]; for 3 turns they can't be Chilled and take 20% less Fire damage.
- **Description:** "Wrap the party in winter's embrace: every ally regains [X] HP, can't be Chilled, and takes 20% less Fire damage for 3 turns."
- **[X]:** Level 50: 189; Level 100: 357.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** Ice's party heal, with its usual stand against Fire.

### Frost Nova: Tier 7, offensive, all enemies (new spell)

- **The spell:** 3 AP, 140 Mana, 17,500 silver; power 17 + Int ×1 + Mind ×0.29 (55% of the tier's reference, 75% for all enemies, less its Chills). Every enemy struck is always **Chilled** with one more stack for 2 turns, so any at 2 stacks freezes.
- **Description:** "Burst frost from you across all enemies for [X] Ice damage, Chilling every one of them once more."
- **[X]:** Level 60: 275; Level 100: 442.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** The nova that finishes a half-chilled field: the twos freeze, the rest move closer.

### Rime Spike: Tier 8, offensive, one target (new spell)

- **The spell:** 2 AP, 126 Mana, 25,000 silver; power 23 + Int ×1.37 + Mind ×0.39 (65% of the tier's reference, the 2 AP rule). A 2 AP spell; it always **Chills** the target (1 stack) for 2 turns, and a Frozen target **shatters** (+50%, its Frozen ends).
- **Description:** "Drive a rime spike into one enemy for [X] Ice damage, Chilling it; a Frozen one shatters for 50% more. This spell costs 2 AP."
- **[X]:** Level 70: 433; Level 100: 603.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** Ice's fast spell for the high tiers.

### Frozen Tomb: Tier 9, offensive, one target (new spell)

- **The spell:** 3 AP, 250 Mana, 35,000 silver; power 31 + Int ×1.83 + Mind ×0.52 (70% of the tier's reference, now, and half of [X] again when it thaws). The target is entombed (new): Frozen for 2 turns instead of 1, and when it thaws it takes half of [X] again. A boss resists half the time (then it is Chilled twice over instead).
- **Description:** "Seal one enemy in a frozen tomb for [X] Ice damage: it stays Frozen for 2 turns and takes half of that again as it thaws. A boss resists half the time and is Chilled twice over instead."
- **[X]:** Level 80: 654; Level 100: 805.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Ice's great single-target control.

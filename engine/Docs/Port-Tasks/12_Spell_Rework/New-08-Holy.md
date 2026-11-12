# New spells 8: Holy (task 12)

**Status: a draft for the user's review (SP2, SP3).** Holy's fourteen new spells: two in each of Tiers 1–5, one in
each of Tiers 6–9. They follow the approved system and curve (`Spell-Augmentation.md`), the sample's style and the
element statuses (`Element-Statuses.md`). Prices are the tier's usual. [X] values are for the raw caster of the system
draft (no classes or gear) at the level the tier unlocks and at Level 100, for a Wisdom-first caster.
**Updated 2026-11-12 (SP60, SP61):** some spells here borrow another element's status; each says so ("borrowed, SP60" or "SP61").

## What Holy gains

- **Fast spells it lacked:** Hallowed Strike (2 AP at Tier 1), Piercing Radiance (2 AP at Tier 8), Aegis of Faith
  (2 AP: the next big blow does nothing).
- **For fighters:** Bless Weapon (Holy damage on an ally's weapon, more against the old foes).
- **Heals ahead and around:** Mending Vow (heals when its target is next hit), Ward of Light (a barrier that heals),
  Radiant Burst (hurts every enemy and heals every ally), Devoted Aura (a long party aura), Hallowed Refuge (damage cut
  and healing each turn for the party).
- **Life and death:** Resurrection (a 4 AP revival at Tier 5), Last Grace (for 2 turns no ally can fall).
- **Judgment:** Reprimand (an enemy's next attack weakened), Sacred Flame (holy fire over 3 turns, double on the old
  foes), Dawnbringer (a 4 AP strike that blinds).

Names were chosen so as not to copy other games' well-known spell names. New mechanics: a heal that waits for its target
to be hit, a guard against the next blow over a share of HP, a party that can't fall below 1 HP, a strike that also
heals the party, holy fire over time.

## At a glance

| Tier | Spell | Kind | AP | Mana | What it does |
|---|---|---|---|---|---|
| 1 | Hallowed Strike | offensive, one target | 2 | 8 | 2 AP; +30% on Devils, Demons and Undead |
| 1 | Bless Weapon | offensive, yourself or an ally | 3 | 12 | 2 turns: their weapon attacks +[X] Holy, +50% on Devils, Demons and Undead |
| 2 | Ward of Light | defensive, yourself or an ally | 3 | 22 | a barrier; Blessed (2% HP a turn) while it holds |
| 2 | Reprimand | offensive, one target | 3 | 22 | its next attack on your side deals 30% less |
| 3 | Sacred Flame | offensive, one target | 3 | 35 | always Burns for 3 turns (45% of the hit), twice as hot on Devils, Demons and Undead (borrowed status) |
| 3 | Mending Vow | defensive, an ally | 3 | 35 | heals [X] the next time they're hit (within 3 turns) |
| 4 | Devoted Aura | defensive, all allies | 3 | 50 | 4 turns: every ally's Armor +10%, 2% HP a turn |
| 4 | Radiant Burst | offensive, all enemies; defensive, all allies | 3 | 50 | damages every enemy and heals every ally |
| 5 | Resurrection | defensive, a fallen ally | 4 | 90 | 4 AP: raises a fallen ally with [X] + 10% HP |
| 5 | Aegis of Faith | defensive, yourself or an ally | 2 | 52 | 2 AP: the next blow over 20% of their HP does nothing |
| 6 | Dawnbringer | offensive, one target | 4 | 120 | 4 AP: +50% on Devils, Demons and Undead, blinds, 40% Burn (borrowed status) |
| 7 | Hallowed Refuge | defensive, all allies | 3 | 140 | 3 turns: every ally takes 25% less damage and heals 5% a turn |
| 8 | Piercing Radiance | offensive, one target | 2 | 126 | 2 AP: blinds (Accuracy −30%); +50% on Devils, Demons and Undead |
| 9 | Last Grace | defensive, all allies | 3 | 250 | 2 turns: no ally can fall below 1 HP |

## The spells

### Hallowed Strike: Tier 1, offensive, one target (new spell)

- **The spell:** 2 AP, 8 Mana, 700 silver; power 4 + Wis ×0.23 + Int ×0.07 (65% of the tier's reference, the 2 AP rule). Deals 30% more to Devils, Demons and Undead.
- **Description:** "Strike one enemy with hallowed light for [X] Holy damage, 30% more against Devils, Demons and Undead. This spell costs 2 AP."
- **[X]:** Level 1: 7; Level 100: 102.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Holy's fast spell; it has none today.

### Bless Weapon: Tier 1, offensive, yourself or an ally (new spell)

- **The spell:** 3 AP, 12 Mana, 700 silver; power 2 + Wis ×0.1 + Str ×0.03 (30% of the tier's reference, per weapon attack). For 2 turns, the weapon attacks of yourself or an ally each deal [X] more Holy damage, 50% more against Devils, Demons and Undead.
- **Description:** "Bless the weapon of yourself or an ally for 2 turns: each attack deals [X] more Holy damage, half again against Devils, Demons and Undead."
- **[X]:** Level 1: 3; Level 100: 41.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** A paladin's blessing for the party's fighters.

### Ward of Light: Tier 2, defensive, yourself or an ally (new spell)

- **The spell:** 3 AP, 22 Mana, 1,450 silver; power 7 + Wis ×0.43 + Int ×0.13 (80% of the tier's reference, as a barrier). A **barrier** of [X] for 3 turns; while it holds they are **Blessed**, regaining 2% of their maximum HP at the start of each of their turns.
- **Description:** "Ring yourself or an ally with a ward of light: a barrier that absorbs [X] damage within 3 turns, healing them a little each turn while it holds."
- **[X]:** Level 10: 29; Level 100: 191.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** Holy's own shield, which mends as it guards.

### Reprimand: Tier 2, offensive, one target (new spell)

- **The spell:** 3 AP, 22 Mana, 1,450 silver; power 8 + Wis ×0.46 + Int ×0.14 (85% of the tier's reference). The target's next attack or spell against your side deals 30% less.
- **Description:** "Reprimand one enemy with holy light for [X] Holy damage; its next attack or spell against the party deals 30% less."
- **[X]:** Level 10: 32; Level 100: 205.
- **Augments:** Power, Resistance penetration, Effect strength, Mana cost.
- **Why:** An early strike that also protects.

### Sacred Flame: Tier 3, offensive, one target (new spell)

- **The spell:** 3 AP, 35 Mana, 3,300 silver; power 7 + Wis ×0.42 + Int ×0.12 (55% of the tier's reference, its Burning is the rest). Holy fire clings to it: the target is always set **Burning** (Fire's status, borrowed, SP60) for 3 turns, for 45% of the hit each turn, twice as hot on Devils, Demons and Undead.
- **Description:** "Set one enemy alight with sacred flame for [X] Holy damage; it Burns for 45% of that at the start of each of its next 3 turns, twice as much against Devils, Demons and Undead."
- **[X]:** Level 20: 46; Level 100: 185.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Holy fire as true Burning (SP60), still the old foes' bane.

### Mending Vow: Tier 3, defensive, an ally (new spell)

- **The spell:** 3 AP, 35 Mana, 3,300 silver; power 13 + Wis ×0.77 + Int ×0.22 (100% of the tier's reference, as the heal when it triggers). A vow on an ally (new): the next time they take damage within 3 turns, they are healed for [X] at once.
- **Description:** "Lay a mending vow on an ally: the next time they are hurt within 3 turns, they are healed for [X] at once."
- **[X]:** Level 20: 84; Level 100: 339.
- **Augments:** Power, Effect duration, Mana cost.
- **Why:** A heal cast ahead, for a tank about to be struck.

### Devoted Aura: Tier 4, defensive, all allies (new spell)

- **The spell:** 3 AP, 50 Mana, 6,000 silver. For 4 turns, every ally's Armor +10% and they are **Blessed**, regaining 2% of their maximum HP at the start of each of their turns.
- **Description:** "Raise an aura of devotion around the party for 4 turns: every ally's Armor rises by 10% and they regain 2% of maximum HP each turn."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** A long, steady aura for the whole party.

### Radiant Burst: Tier 4, offensive, all enemies; defensive, all allies (new spell)

- **The spell:** 3 AP, 50 Mana, 6,000 silver; power 8 + Wis ×0.47 + Int ×0.14 (50% of the tier's reference, to each enemy; 40% of the reference healed on each ally). Deals [X] to every enemy and heals every ally for 80% of [X].
- **Description:** "Burst with radiance: every enemy takes [X] Holy damage, and every ally regains 80% of that as HP."
- **[X]:** Level 30: 71; Level 100: 208.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Holy's two sides in one spell.

### Resurrection: Tier 5, defensive, a fallen ally (new spell)

- **The spell:** 4 AP, 90 Mana, 8,800 silver; power 21 + Wis ×1.22 + Int ×0.35 (100% of the tier's reference, as the HP they rise with). Raises a fallen ally with [X] plus 10% of their maximum HP. A 4 AP spell.
- **Description:** "Call a fallen ally back to life with [X] HP plus 10% of their maximum HP. This spell costs 4 AP."
- **[X]:** Level 40: 235; Level 100: 538.
- **Augments:** Power, Mana cost.
- **Why:** Holy's own revival, two tiers before Fire's Phoenix Renewal, for most of a turn.

### Aegis of Faith: Tier 5, defensive, yourself or an ally (new spell)

- **The spell:** 2 AP, 52 Mana, 8,800 silver. For 3 turns, the next hit that would deal more than 20% of their maximum HP deals nothing (new).
- **Description:** "Raise an aegis of faith over yourself or an ally: within 3 turns, the next blow that would take more than a fifth of their HP is turned aside entirely. This spell costs 2 AP."
- **Augments:** Effect duration, Mana cost.
- **Why:** A guard against the one big blow a boss is winding up.

### Dawnbringer: Tier 6, offensive, one target (new spell)

- **The spell:** 4 AP, 120 Mana, 11,700 silver; power 31 + Wis ×1.84 + Int ×0.53 (125% of the tier's reference, the 4 AP rule). A heavy 4 AP strike; 50% more to Devils, Demons and Undead, and the target is blinded (Accuracy −25% for 2 turns). A 40% chance to set the target **Burning** (Fire's status, borrowed, SP61) for 2 turns, for 15% of the hit each turn: the dawn's fire.
- **Description:** "Bring the dawn down on one enemy for [X] Holy damage, half again against Devils, Demons and Undead, blinding it for 2 turns. It has a 40% chance to set the target Burning. This spell costs 4 AP."
- **[X]:** Level 50: 429; Level 100: 811.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Holy's heaviest strike below Tier 10.

### Hallowed Refuge: Tier 7, defensive, all allies (new spell)

- **The spell:** 3 AP, 140 Mana, 17,500 silver. For 3 turns, every ally takes 25% less damage and is **Blessed**, regaining 5% of their maximum HP at the start of each of their turns.
- **Description:** "Open a hallowed refuge for the party for 3 turns: every ally takes 25% less damage and regains 5% of maximum HP each turn."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Holy's great party defence before Seraphic Shelter.

### Piercing Radiance: Tier 8, offensive, one target (new spell)

- **The spell:** 2 AP, 126 Mana, 25,000 silver; power 23 + Wis ×1.37 + Int ×0.39 (65% of the tier's reference, the 2 AP rule). A 2 AP spell; 50% more to Devils, Demons and Undead, and the target is blinded (Accuracy −30% for 2 turns).
- **Description:** "Pierce one enemy with radiance for [X] Holy damage, half again against Devils, Demons and Undead, blinding it for 2 turns. This spell costs 2 AP."
- **[X]:** Level 70: 433; Level 100: 603.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Holy's fast spell for the high tiers.

### Last Grace: Tier 9, defensive, all allies (new spell)

- **The spell:** 3 AP, 250 Mana, 35,000 silver. For 2 turns, no ally can fall below 1 HP (new).
- **Description:** "Grant the party a last grace: for 2 turns, no ally can fall below 1 HP."
- **Augments:** Effect duration, Mana cost.
- **Why:** Holy's great answer to a boss's killing turn.

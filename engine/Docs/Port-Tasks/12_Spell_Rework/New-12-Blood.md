# New spells 12: Blood (task 12)

**Status: approved (2026-11-12, SP62), for the port (SP2, SP3).** Blood's fourteen new spells, the last of the 168: two in each of
Tiers 1–5, one in each of Tiers 6–9. They follow the approved system and curve (`Spell-Augmentation.md`), the sample's
style and the element statuses (`Element-Statuses.md`). Prices are the tier's usual. [X] values are for the raw caster
of the system draft (no classes or gear) at the level the tier unlocks and at Level 100.
**Updated 2026-11-12 (SP60, SP61):** some spells here borrow another element's status; each says so ("borrowed, SP60" or "SP61").

## What Blood gains

- **Life paid and shared:** Sanguine Shell (a barrier bought with HP), Blood Link (an enemy takes part of the damage you
  take), Sanguine Bond (you take half of an ally's damage).
- **Bleeding, sooner and heavier:** Crimson Dart (2 AP), Open Veins (a sure Bleed at Tier 2), Crimson Rain, Crimson
  Tide (4 AP, every enemy), Vein Spike (2 AP at Tier 8), Bloodbath (every enemy bleeding twice as fast).
- **Feeding the party:** Sanguine Aura (every ally's weapon attacks heal them), Crimson Fury (a frenzy: more damage and
  healing, more damage taken), Blood Moon (life drains heal more, every attack heals).
- **Control:** Puppet Strings makes an enemy attack its own ally on its next turn (not bosses).
- **Boiling Blood:** a second damage over time beside Bleeding, with a slow.

With this batch every element has its fourteen new spells (168 in all, SP3), the four new summons among them: Basic's
Mana Golem, Wind's Storm Roc, Nature's Dryad and Death's Wraith (and Death's Raise Dead).

New mechanics: a barrier bought with HP, damage shared with a linked enemy, an ally's damage moved to you, a frenzy, a
night when every attack heals, an enemy made to strike its own ally.

## At a glance

| Tier | Spell | Kind | AP | Mana | What it does |
|---|---|---|---|---|---|
| 1 | Crimson Dart | offensive, one target | 2 | 8 | 2 AP, 25% Bleed |
| 1 | Sanguine Shell | defensive, yourself | 3 | 6 | blood price 8% HP: a barrier of [X] plus twice what you paid |
| 2 | Open Veins | offensive, one target | 3 | 22 | always Bleeds |
| 2 | Sanguine Aura | offensive, all allies | 3 | 22 | 3 turns: every ally's weapon attacks heal them for 10% |
| 3 | Blood Link | offensive, one target | 3 | 35 | 3 turns: 30% of the damage you take is dealt to it too |
| 3 | Boiling Blood | offensive, one target | 3 | 35 | always Burns (25% of the hit, 2 turns), Speed −10% (borrowed status) |
| 4 | Crimson Fury | offensive, yourself or an ally | 3 | 50 | 3 turns: attacks +20% and heal 10%, but 10% more damage taken |
| 4 | Crimson Rain | offensive, all enemies | 3 | 50 | 40% Bleed and 25% Soak on each enemy (borrowed status), heals you 10% |
| 5 | Puppet Strings | offensive, one target | 3 | 75 | on its next turn it attacks one of its own allies (not bosses) |
| 5 | Sanguine Bond | defensive, an ally | 3 | 75 | 3 turns: half the damage they take comes to you instead |
| 6 | Crimson Tide | offensive, all enemies | 4 | 120 | 4 AP: every enemy Bleeds; heals you 15% |
| 7 | Blood Moon | the whole field | 3 | 140 | 3 turns: your side's life drains heal 50% more, attacks heal 10% |
| 8 | Vein Spike | offensive, one target | 2 | 126 | 2 AP: always Bleeds, heals you 25% |
| 9 | Bloodbath | offensive, all enemies | 3 | 250 | every enemy Bleeds twice as fast (6% a turn) |

## The spells

### Crimson Dart: Tier 1, offensive, one target (new spell)

- **The spell:** 2 AP, 8 Mana, 700 silver; power 4 + Int ×0.23 + Vit ×0.07 (65% of the tier's reference, the 2 AP rule). A 25% chance to make the target **Bleed** for 3 turns.
- **Description:** "Flick a dart of blood at one enemy for [X] Blood damage, with a 25% chance to make it Bleed. This spell costs 2 AP."
- **[X]:** Level 1: 7; Level 100: 94.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Blood's fast attack (Wounded Claw charges a weapon).

### Sanguine Shell: Tier 1, defensive, yourself (new spell)

- **The spell:** 3 AP, 6 Mana, 700 silver; power 5 + Vit ×0.32 + Int ×0.09 (90% of the tier's reference, as a barrier, besides the HP paid). **Blood price:** costs 6 Mana and 8% of your maximum HP; you gain a **barrier** of [X] plus twice the HP you paid, for 3 turns.
- **Description:** "Harden your own blood into a shell: you pay 8% of your maximum HP and gain a barrier that absorbs [X] damage plus twice what you paid, for 3 turns."
- **[X]:** Level 1: 9; Level 100: 72.
- **Augments:** Power, Effect duration, Mana cost.
- **Why:** A Blood caster's guard, paid in life, as Blood pays.

### Open Veins: Tier 2, offensive, one target (new spell)

- **The spell:** 3 AP, 22 Mana, 1,450 silver; power 7 + Int ×0.43 + Vit ×0.13 (80% of the tier's reference). Always makes the target **Bleed** for 3 turns.
- **Description:** "Open one enemy's veins for [X] Blood damage; it Bleeds for 3 turns."
- **[X]:** Level 10: 28; Level 100: 175.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** A sure Bleed a tier earlier than Hemorrhage Lance.

### Sanguine Aura: Tier 2, offensive, all allies (new spell)

- **The spell:** 3 AP, 22 Mana, 1,450 silver. For 3 turns, every ally's weapon attacks heal them for 10% of their damage.
- **Description:** "Spread a sanguine aura over the party for 3 turns: every ally's attacks heal them for a tenth of the damage they deal."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Crimson Pact for the whole party, smaller.

### Blood Link: Tier 3, offensive, one target (new spell)

- **The spell:** 3 AP, 35 Mana, 3,300 silver; power 8 + Int ×0.46 + Vit ×0.13 (60% of the tier's reference). Links your blood with the target for 3 turns (new): 30% of the damage you take is also dealt to it.
- **Description:** "Link your blood with one enemy for [X] Blood damage: for 3 turns, it takes 30% of every hurt you take."
- **[X]:** Level 20: 47; Level 100: 186.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** A Blood caster who pays in HP makes an enemy pay too.

### Boiling Blood: Tier 3, offensive, one target (new spell)

- **The spell:** 3 AP, 35 Mana, 3,300 silver; power 9 + Int ×0.54 + Vit ×0.15 (70% of the tier's reference, its Burning is the rest). Its blood boils into flame: the target is always set **Burning** (Fire's status, borrowed, SP60) for 2 turns, for 25% of the hit each turn, and its Speed −10% for 2 turns.
- **Description:** "Boil one enemy's blood for [X] Blood damage until it burns: it is set Burning for 2 turns, taking a quarter of that each turn, and is slowed by 10%."
- **[X]:** Level 20: 55; Level 100: 218.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** A Blood spell that Burns (SP60): Wind can spread its fire and Water put it out.

### Crimson Fury: Tier 4, offensive, yourself or an ally (new spell)

- **The spell:** 3 AP, 50 Mana, 6,000 silver. For 3 turns: their attacks and spells deal 20% more and heal them for 10% of the damage, but they take 10% more damage.
- **Description:** "Drive yourself or an ally into a crimson fury for 3 turns: 20% more damage and a tenth of it healed back, at the cost of taking 10% more."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** A berserker's frenzy for the party's strongest hitter.

### Crimson Rain: Tier 4, offensive, all enemies (new spell)

- **The spell:** 3 AP, 50 Mana, 6,000 silver; power 9 + Int ×0.52 + Vit ×0.15 (55% of the tier's reference, 75% for all enemies, less its effects). Each enemy struck has a 40% chance to **Bleed** for 3 turns; you heal for 10% of the damage. Each enemy also has a 25% chance to be **Soaked** (Water's status, borrowed, SP61) for 2 turns: a rain of blood.
- **Description:** "Bring a crimson rain down on all enemies for [X] Blood damage; each has a 40% chance to Bleed, and you heal for a tenth of the damage. Each also has a 25% chance to be Soaked."
- **[X]:** Level 30: 73; Level 100: 211.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** A mid-tier group Bleed.

### Puppet Strings: Tier 5, offensive, one target (new spell)

- **The spell:** 3 AP, 75 Mana, 8,800 silver; power 12 + Int ×0.67 + Vit ×0.19 (55% of the tier's reference). Pulls the target's blood like strings (new): on its next turn it makes one attack against one of its own allies instead of your side. Bosses and unique enemies resist.
- **Description:** "Pull the strings of one enemy's blood for [X] Blood damage: on its next turn it attacks one of its own allies. Bosses resist."
- **[X]:** Level 40: 120; Level 100: 272.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Blood's control: a single turn of Dark's Dominate, four tiers earlier.

### Sanguine Bond: Tier 5, defensive, an ally (not yourself) (new spell)

- **The spell:** 3 AP, 75 Mana, 8,800 silver. Bonds your blood to an ally's for 3 turns (new): half of the damage they take is moved to you instead.
- **Description:** "Bond your blood to an ally's for 3 turns: half of every hurt they take comes to you instead."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** A sturdy Blood caster shields the party's fragile one.

### Crimson Tide: Tier 6, offensive, all enemies (new spell)

- **The spell:** 4 AP, 120 Mana, 11,700 silver; power 24 + Int ×1.4 + Vit ×0.4 (95% of the tier's reference, the 4 AP rule on an all-enemies spell). A heavy 4 AP tide; every enemy struck always **Bleeds** for 3 turns, and you heal for 15% of the damage.
- **Description:** "Raise a crimson tide over all enemies for [X] Blood damage: every one of them Bleeds for 3 turns, and you heal for 15% of the damage. This spell costs 4 AP."
- **[X]:** Level 50: 302; Level 100: 567.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** Blood's biggest group hit below Tier 10.

### Blood Moon: Tier 7, the whole field (new spell)

- **The spell:** 3 AP, 140 Mana, 17,500 silver. A blood moon rises for 3 turns (new): every life drain your side has heals 50% more, and every ally's attacks and spells heal them for 10% of their damage.
- **Description:** "Raise a blood moon for 3 turns: your side's life drains heal half again as much, and every ally's attacks and spells heal them for a tenth of the damage."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** A night when the whole party feeds.

### Vein Spike: Tier 8, offensive, one target (new spell)

- **The spell:** 2 AP, 126 Mana, 25,000 silver; power 23 + Int ×1.37 + Vit ×0.39 (65% of the tier's reference, the 2 AP rule). A 2 AP spell; it always makes the target **Bleed** for 3 turns, and you heal for 25% of the damage.
- **Description:** "Drive a spike into one enemy's veins for [X] Blood damage; it Bleeds for 3 turns and you heal for a quarter of the damage. This spell costs 2 AP."
- **[X]:** Level 70: 399; Level 100: 555.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** Blood's fast spell for the high tiers.

### Bloodbath: Tier 9, offensive, all enemies (new spell)

- **The spell:** 3 AP, 250 Mana, 35,000 silver; power 25 + Int ×1.44 + Vit ×0.41 (55% of the tier's reference, 75% for all enemies, less its doubled Bleed). Every enemy struck always **Bleeds** twice as fast (6% of its maximum HP a turn) for 3 turns.
- **Description:** "Turn the field into a bloodbath for [X] Blood damage: every enemy bleeds 6% of its maximum HP at each of its next 3 turns."
- **[X]:** Level 80: 475; Level 100: 584.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Arterial Ruin's double Bleed on the whole enemy side.

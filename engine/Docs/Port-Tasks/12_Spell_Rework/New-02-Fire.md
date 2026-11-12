# New spells 2: Fire (task 12)

**Status: approved (2026-11-11, SP50), for the port (SP2, SP3).** Fire's fourteen new spells: two in each of Tiers 1–5, one in
each of Tiers 6–9. They follow the approved system and curve (`Spell-Augmentation.md`), the sample's style and the
element statuses (`Element-Statuses.md`). Prices are the tier's usual. [X] values are for the raw caster of the system
draft (no classes or gear) at the level the tier unlocks and at Level 100.

## What Fire gains

The reworked Fire spells are strong on damage and burns; the new ones add what they lacked:
- **Fast and heavy:** Cinder Flick (2 AP at Tier 1), Hellfire Pillar (2 AP at Tier 7), Inferno Strike (Fire's first
  4 AP spell) and Starburst (a whole 5 AP turn for every enemy).
- **Burns kept and spent:** Searing Ray renews a burn, Burning Ground burns the field for 3 turns, Combustion spends a
  burn at once for half again (Fire's own, as Death's Exsanguinate spends a Bleed), Sunfire Brand makes one enemy burn
  its own side.
- **Fireball**, the classic, explodes over the group.
- **For the party:** Flame Blade (weapon attacks wreathed in fire), Smoke Screen (enemy Accuracy down), Cauterize
  (Fire's one heal, which stops Bleeding), Wings of the Phoenix (every ally who falls rises once).
- **Immolate:** spells +20% for 3 turns, paid in HP.

New mechanics: a field that burns enemies at each of their turns, a burn renewed or spent at once, a branded enemy
that spreads its Fire damage to its side, a buff paid in HP each turn, a party-wide rising from defeat, a 4 AP spell.

## At a glance

| Tier | Spell | Kind | AP | Mana | What it does |
|---|---|---|---|---|---|
| 1 | Cinder Flick | offensive, one target | 2 | 8 | 2 AP, 30% Burn |
| 1 | Flame Blade | offensive, yourself or an ally | 3 | 12 | 2 turns: their weapon attacks +[X] Fire, 20% Burn |
| 2 | Searing Ray | offensive, one target | 3 | 22 | renews a Burning target's burn; 30% Burn otherwise |
| 2 | Smoke Screen | defensive, all allies | 3 | 20 | enemy Accuracy against your side −15% for 2 turns |
| 3 | Fireball | offensive, one target and the others | 3 | 36 | explodes: 40% on every other enemy, 30% Burn |
| 3 | Cauterize | defensive, yourself or an ally | 3 | 34 | heals, ends Bleeding, no Bleeding for 2 turns |
| 4 | Combustion | offensive, one target | 3 | 50 | detonates a Burning target's burn at once, +50% |
| 4 | Burning Ground | offensive, the whole field | 3 | 50 | 3 turns: each enemy takes [X] at its turns, 30% Burn |
| 5 | Immolate | offensive, yourself | 2 | 50 | 2 AP: 3 turns of spells +20% power, for 3% of your HP a turn |
| 5 | Inferno Strike | offensive, one target and the others | 4 | 90 | 4 AP: a great blast that splashes 50%, 40% Burn |
| 6 | Wings of the Phoenix | defensive, all allies | 3 | 100 | 3 turns: every ally who falls rises once with [X] HP |
| 7 | Hellfire Pillar | offensive, one target | 2 | 98 | 2 AP: always Burns (20%, 2 turns) |
| 8 | Starburst | offensive, all enemies | 5 | 252 | 5 AP: the whole turn for a burst on every enemy; always Burns |
| 9 | Sunfire Brand | offensive, one target | 3 | 250 | 3 turns: Fire damage to it also strikes every other enemy for 30% |

## The spells

### Cinder Flick: Tier 1, offensive, one target (new spell)

- **The spell:** 2 AP, 8 Mana, 700 silver; power 4 + Int ×0.23 + Wis ×0.07 (65% of the tier's reference, the 2 AP rule). A 30% chance to set the target **Burning** for 2 turns, for 15% of the hit each turn.
- **Description:** "Flick a cinder at one enemy for [X] Fire damage, with a 30% chance to set it Burning for 2 turns. This spell costs 2 AP."
- **[X]:** Level 1: 7; Level 100: 102.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Fire's fast spell at Tier 1 (Fire Whip is the Tier 2 one).

### Flame Blade: Tier 1, offensive, yourself or an ally (new spell)

- **The spell:** 3 AP, 12 Mana, 700 silver; power 2 + Int ×0.1 + Str ×0.03 (30% of the tier's reference, per weapon attack). For 2 turns, the weapon attacks of yourself or an ally each deal [X] more Fire damage and have a 20% chance to set the target **Burning** for 2 turns.
- **Description:** "Wreathe the weapon of yourself or an ally in flame for 2 turns: each attack deals [X] more Fire damage, with a 20% chance to set its target Burning."
- **[X]:** Level 1: 3; Level 100: 41.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Fire has no help for the party's weapon users before Tier 6.

### Searing Ray: Tier 2, offensive, one target (new spell)

- **The spell:** 3 AP, 22 Mana, 1,450 silver; power 8 + Int ×0.46 + Wis ×0.14 (85% of the tier's reference). A target already **Burning** has its Burning renewed to its full length; any other has a 30% chance to be set Burning for 2 turns (15% of the hit each turn).
- **Description:** "Sear one enemy with a ray of fire for [X] Fire damage; if it is Burning, its fire is renewed, otherwise it has a 30% chance to catch fire."
- **[X]:** Level 10: 32; Level 100: 205.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Keeps a burn going on the enemy Fire is wearing down.

### Smoke Screen: Tier 2, defensive, all allies (new spell)

- **The spell:** 3 AP, 20 Mana, 1,450 silver. Enemy weapon attacks against your side have 15% less Accuracy for 2 turns (new).
- **Description:** "Raise a screen of smoke over the party: for 2 turns, enemies' weapon attacks against them have 15% less Accuracy."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** An early party defence in Fire's own colours.

### Fireball: Tier 3, offensive, one target and the others (new spell)

- **The spell:** 3 AP, 36 Mana, 3,300 silver; power 10 + Int ×0.58 + Wis ×0.17 (75% of the tier's reference, on the target; 40% of [X] on every other enemy). Explodes on the target and splashes every other enemy for 40% of [X]; a 30% chance to set the target **Burning** for 2 turns.
- **Description:** "Hurl a fireball at one enemy for [X] Fire damage; it explodes over every other enemy for 40% of that, with a 30% chance to set the target Burning."
- **[X]:** Level 20: 64; Level 100: 257.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** The classic: a single-target spell that still reaches a group.

### Cauterize: Tier 3, defensive, yourself or an ally (new spell)

- **The spell:** 3 AP, 34 Mana, 3,300 silver; power 10 + Wis ×0.62 + Int ×0.18 (80% of the tier's reference). Heals [X] plus 5% of maximum HP, ends their **Bleeding** and keeps it away for 2 turns.
- **Description:** "Seal the wounds of yourself or an ally with fire: [X] HP plus 5% of maximum HP restored, any Bleeding stopped and kept away for 2 turns."
- **[X]:** Level 20: 67; Level 100: 273.
- **Augments:** Power, Effect duration, Mana cost.
- **Why:** Fire's one heal, and an answer to Blood.

### Combustion: Tier 4, offensive, one target (new spell)

- **The spell:** 3 AP, 50 Mana, 6,000 silver; power 11 + Int ×0.66 + Wis ×0.19 (70% of the tier's reference). If the target is **Burning**, all of its remaining Burning is dealt at once, half again as much, and the Burning ends (new).
- **Description:** "Make one enemy's fire combust for [X] Fire damage; if it is Burning, the rest of its burn is dealt at once, half again as much."
- **[X]:** Level 30: 99; Level 100: 291.
- **Augments:** Power, Resistance penetration, Effect strength, Mana cost.
- **Why:** Fire's own way to spend a burn, as Death's Exsanguinate spends a Bleed.

### Burning Ground: Tier 4, offensive, the whole field (new spell)

- **The spell:** 3 AP, 50 Mana, 6,000 silver; power 4 + Int ×0.23 + Wis ×0.07 (25% of the tier's reference, per enemy, at each of its turns). For 3 turns the ground burns (new): every enemy takes [X] Fire damage at the start of each of its turns, with a 30% chance to be set **Burning** for 2 turns.
- **Description:** "Set the ground alight for 3 turns: every enemy takes [X] Fire damage at the start of each of its turns, with a 30% chance to catch fire."
- **[X]:** Level 30: 35; Level 100: 102.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** A field that keeps the enemy side burning while Fire casts other spells.

### Immolate: Tier 5, offensive, yourself (new spell)

- **The spell:** 2 AP, 50 Mana, 8,800 silver. For 3 turns your spells gain 20% power, but you lose 3% of your maximum HP at the start of each of your turns (new).
- **Description:** "Set yourself alight for 3 turns: your spells gain 20% power, but the fire takes 3% of your maximum HP at the start of each of your turns. This spell costs 2 AP."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** A risky burst window for a Fire caster.

### Inferno Strike: Tier 5, offensive, one target and the others (new spell)

- **The spell:** 4 AP, 90 Mana, 8,800 silver; power 27 + Int ×1.59 + Wis ×0.45 (130% of the tier's reference, the 4 AP rule; 50% of [X] on every other enemy). A heavy 4 AP strike; it splashes every other enemy for 50% of [X], and has a 40% chance to set the target **Burning** for 3 turns (15% of the hit each turn).
- **Description:** "Call down an inferno on one enemy for [X] Fire damage that splashes every other enemy for half of that, with a 40% chance to set the target Burning. This spell costs 4 AP."
- **[X]:** Level 40: 305; Level 100: 699.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Fire's first 4 AP spell: most of a turn for the biggest blast of its tier.

### Wings of the Phoenix: Tier 6, defensive, all allies (new spell)

- **The spell:** 3 AP, 100 Mana, 11,700 silver; power 15 + Wis ×0.88 + Int ×0.25 (60% of the tier's reference, as the HP they rise with). For 3 turns, every ally who falls rises again at once with [X] HP (once each; Flame Rebirth for the whole party).
- **Description:** "Spread the phoenix's wings over the party: for 3 turns, any ally who falls rises again with [X] HP."
- **[X]:** Level 50: 205; Level 100: 387.
- **Augments:** Power, Effect duration, Mana cost.
- **Why:** Fire's great party defence: not to avoid the blows, but to rise from them.

### Hellfire Pillar: Tier 7, offensive, one target (new spell)

- **The spell:** 2 AP, 98 Mana, 17,500 silver; power 20 + Int ×1.18 + Wis ×0.34 (65% of the tier's reference, the 2 AP rule). A 2 AP spell; it always sets the target **Burning** for 2 turns, for 20% of the hit each turn.
- **Description:** "Raise a pillar of hellfire under one enemy for [X] Fire damage and set it Burning for 2 turns. This spell costs 2 AP."
- **[X]:** Level 60: 325; Level 100: 520.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Fire's fast spell for the high tiers.

### Starburst: Tier 8, offensive, all enemies (new spell)

- **The spell:** 5 AP, 252 Mana, 25,000 silver; power 43 + Int ×2.52 + Wis ×0.72 (120% of the tier's reference, the 5 AP rule on an all-enemies spell). Takes your whole turn (5 AP); every enemy struck is always **Burning** for 2 turns, for 15% of the hit each turn.
- **Description:** "Pour your whole turn into a starburst over all enemies for [X] Fire damage, setting every one of them Burning for 2 turns. This spell costs 5 AP."
- **[X]:** Level 70: 797; Level 100: 1,110.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** The biggest group hit Fire has below Tier 10, for a whole turn.

### Sunfire Brand: Tier 9, offensive, one target (new spell)

- **The spell:** 3 AP, 250 Mana, 35,000 silver; power 38 + Int ×2.23 + Wis ×0.64 (85% of the tier's reference). Brands the target for 3 turns (new): whenever it takes Fire damage, every other enemy takes 30% of it; it is always **Burning** for 3 turns (15% of the hit each turn).
- **Description:** "Brand one enemy with sunfire for [X] Fire damage: for 3 turns it Burns, and every time it takes Fire damage, every other enemy takes 30% of it."
- **[X]:** Level 80: 798; Level 100: 983.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Turns one enemy into a sun that burns its own side.

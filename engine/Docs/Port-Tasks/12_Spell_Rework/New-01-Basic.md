# New spells 1: Basic (task 12)

**Status: approved (2026-11-11, SP49), for the port (SP2, SP3).** The task's new spells, element by element in the game's order:
two per element in each of Tiers 1–5 and one in each of Tiers 6–9, fourteen per element, 168 in all (SP3). Basic's
fourteen are here, among them **Basic's summon** (SP34: Basic had none players can learn). They follow the approved
system and curve (`Spell-Augmentation.md`), the sample's style and the element statuses (`Element-Statuses.md`).
"(new)" marks a mechanic the game doesn't have yet, beyond those the twelve batches already added. Prices are the
tier's usual (the median of today's spells of that tier). [X] values are for the raw caster of the system draft (no
classes or gear) at the level the tier unlocks and at Level 100.
**Updated 2026-11-12 (SP61):** some spells here borrow another element's status; each says so ("borrowed, SP61").

## What Basic gains

The reworked Basic spells left gaps for a generalist: no fast attack, no group spell before Tier 4, no plain dispel, no
help with the party's Mana or spell power, no summon. The new ones fill them:
- **Fast and cheap:** Mana Dart (2 AP), Mana Focus (2 AP, your next spell 30% cheaper), Dispel Magic (2 AP, either way
  round), Recursion (2 AP, your next spell twice).
- **Exposed, built up and paid off:** Mana Scatter, Glyph of Exposure (to the full 3 stacks over three turns), Arcane
  Missiles, Arcane Overload; Grand Resonance pays off every status on the field.
- **For the party:** Mana Transfer (your Mana to an ally), Arcane Empowerment (every ally's spells +15%), Spellward
  Dome (the next spell at each ally halved).
- **Prismatic Bolt** strikes with whichever element its target resists least.
- **Summon - Mana Golem** (Tier 4) is a new creature, made by the port with its look and stats beside the other new
  summons' creatures.

New mechanics: a spell cheaper by a share, a glyph that adds a status each turn, giving an ally your Mana, a party spell
power buff, a spell of the element its target resists least, a spell cast twice, a summon that returns Mana when it
falls.

## At a glance

| Tier | Spell | Kind | AP | Mana | What it does |
|---|---|---|---|---|---|
| 1 | Mana Dart | offensive, one target | 2 | 8 | 2 AP, 20% Expose |
| 1 | Mana Focus | defensive, yourself | 2 | 6 | 2 AP: your next spell within 2 turns costs 30% less Mana |
| 2 | Mana Scatter | offensive, all enemies | 3 | 22 | 20% Expose on each enemy |
| 2 | Dispel Magic | an enemy or an ally | 2 | 16 | 2 AP: ends an enemy's newest buff or an ally's newest debuff |
| 3 | Glyph of Exposure | offensive, one target | 3 | 34 | a glyph that Exposes it at each of its next 3 turns |
| 3 | Mana Transfer | defensive, an ally | 3 | 10 | give an ally up to [X] of your Mana, plus 20% |
| 4 | Summon - Mana Golem | summon | 3 | 56 | Basic's summon (SP34): a golem of Mana that returns Mana when it falls |
| 4 | Arcane Missiles | offensive, one target | 3 | 50 | three missiles at one enemy, 25% Expose each |
| 5 | Arcane Empowerment | defensive, all allies | 3 | 75 | every ally's spells +15% power for 3 turns |
| 5 | Prismatic Bolt | offensive, one target | 3 | 75 | strikes as the element the target resists least; 30% chance of that element's status (borrowed) |
| 6 | Spellward Dome | defensive, all allies | 3 | 100 | the next enemy spell at each ally deals 50% less |
| 7 | Arcane Overload | offensive, one target | 5 | 196 | 5 AP: a whole turn for one enormous blast |
| 8 | Recursion | defensive, yourself | 2 | 126 | 2 AP: your next spell is cast twice, the second at 60% |
| 9 | Grand Resonance | offensive, all enemies | 3 | 250 | Resonance counts triple: +30% per status, up to +90% |

## The spells

### Mana Dart: Tier 1, offensive, one target (new spell)

- **The spell:** 2 AP, 8 Mana, 700 silver; power 4 + Int ×0.23 + Wis ×0.07 (65% of the tier's reference, the 2 AP rule). A 20% chance to **Expose** the target (1 stack) for 2 turns.
- **Description:** "Flick a dart of raw Mana at one enemy for [X] Basic damage, with a 20% chance to Expose it for 2 turns. This spell costs 2 AP."
- **[X]:** Level 1: 7; Level 100: 102.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Basic has no fast attack (Manastrike charges a weapon); every other element has one.

### Mana Focus: Tier 1, defensive, yourself (new spell)

- **The spell:** 2 AP, 6 Mana, 700 silver. Your next spell within 2 turns costs 30% less Mana (new).
- **Description:** "Gather your Mana: your next spell within 2 turns costs 30% less Mana. This spell costs 2 AP."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Mana stays scarce (SP17): a cheap way to make a big spell affordable, and a use for the 2 AP left after a 3 AP spell.

### Mana Scatter: Tier 2, offensive, all enemies (new spell)

- **The spell:** 3 AP, 22 Mana, 1,450 silver; power 6 + Int ×0.38 + Wis ×0.11 (70% of the tier's reference, 75% for all enemies, less its effect). Each enemy struck has a 20% chance to be **Exposed** (1 stack) for 2 turns.
- **Description:** "Scatter raw Mana over all enemies for [X] Basic damage; each has a 20% chance to be Exposed for 2 turns."
- **[X]:** Level 10: 25; Level 100: 167.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Basic's first group spell is at Tier 4 today.

### Dispel Magic: Tier 2, an enemy or an ally (new spell)

- **The spell:** 2 AP, 16 Mana, 1,450 silver. On an enemy, ends its newest buff; on an ally (or yourself), ends their newest debuff (new).
- **Description:** "Unravel one spell: an enemy loses its newest buff, or an ally is freed of their newest debuff. This spell costs 2 AP."
- **Augments:** Mana cost.
- **Why:** The plain dispel the generalist element should have early, either way round.

### Glyph of Exposure: Tier 3, offensive, one target (new spell)

- **The spell:** 3 AP, 34 Mana, 3,300 silver; power 8 + Int ×0.46 + Wis ×0.13 (60% of the tier's reference). Places a glyph (new): the target is **Exposed** (1 stack) at the start of each of its next 3 turns, so it reaches 3 stacks.
- **Description:** "Burn a glyph into one enemy for [X] Basic damage; at the start of each of its next 3 turns it is Exposed once more."
- **[X]:** Level 20: 50; Level 100: 203.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** A slow, sure way to the full 3 stacks on the one enemy the party's casters will burn down.

### Mana Transfer: Tier 3, defensive, an ally (not yourself) (new spell)

- **The spell:** 3 AP, 10 Mana, 3,300 silver; power 8 + Mind ×0.46 + Wis ×0.13 (60% of the tier's reference, as the Mana you give). You give an ally up to [X] of your Mana; they gain it plus 20% (new).
- **Description:** "Pour your own Mana into an ally: you give up to [X] Mana, and they gain it plus a fifth more."
- **[X]:** Level 20: 39; Level 100: 145.
- **Augments:** Power, Mana cost.
- **Why:** A party's Mana where it is needed most: the support caster feeds the damage dealer.

### Summon - Mana Golem: Tier 4, summon (new spell)

- **The spell:** 3 AP, 56 Mana, 6,000 silver; power 16 + Int ×0.94 + Mind ×0.27 (100% of the tier's reference, as a summon). Summons a Mana Golem (a new creature for the port: Level 18, built like the game's Level 18 spirits, about 200 HP); its maximum HP rises by 200% of [X] and every attack it makes by 25% of [X] (SP29). Its hits have a 40% chance to **Expose** their target (1 stack, 2 turns); when it falls, its summoner regains Mana equal to 10% of the golem's maximum HP. One at a time.
- **Description:** "Summon a friendly Mana Golem whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more, each with a 40% chance to Expose; when it falls, you regain Mana equal to a tenth of its maximum HP. Only one may be active at a time."
- **[X]:** Level 30: 142; Level 100: 414.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Basic's summon (SP34); its look and stats are made with the port's other new creatures.

### Arcane Missiles: Tier 4, offensive, one target (new spell)

- **The spell:** 3 AP, 50 Mana, 6,000 silver; power 5 + Int ×0.31 + Wis ×0.09 (33% of the tier's reference, per missile; three, 99% in all). Three missiles (new) each strike the target for [X], each with a 25% chance to **Expose** it (1 stack) for 2 turns.
- **Description:** "Loose three missiles of raw Mana at one enemy, each dealing [X] Basic damage with a 25% chance to Expose it for 2 turns."
- **[X]:** Level 30: 46; Level 100: 137 (each missile).
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Arcane Barrage scatters its bolts; the Missiles keep theirs on one enemy, for stacking Exposed.

### Arcane Empowerment: Tier 5, defensive, all allies (new spell)

- **The spell:** 3 AP, 75 Mana, 8,800 silver. Every ally's spells gain 15% power for 3 turns (new).
- **Description:** "Empower the whole party with raw Mana: for 3 turns, every ally's spells gain 15% power."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** The party spell buff a caster party lacks today.

### Prismatic Bolt: Tier 5, offensive, one target (new spell)

- **The spell:** 3 AP, 75 Mana, 8,800 silver; power 19 + Int ×1.1 + Wis ×0.32 (90% of the tier's reference). Deals the damage of whichever element the target resists least, and counts as that element for reactions (new). A 30% chance to apply that element's status for 2 turns (Basic's Exposed if it strikes as Basic; borrowed, SP61).
- **Description:** "Split a bolt of Mana through a prism at one enemy for [X] damage of whichever element it resists least. It has a 30% chance to leave that element's status for 2 turns."
- **[X]:** Level 40: 212; Level 100: 486.
- **Augments:** Power, Effect duration, Effect chance, Mana cost.
- **Why:** Raw Mana takes any form: the generalist's answer to an enemy that resists Basic.

### Spellward Dome: Tier 6, defensive, all allies (new spell)

- **The spell:** 3 AP, 100 Mana, 11,700 silver. For 3 turns, the next enemy spell that targets each ally deals 50% less (Arcane Intercept for the whole party).
- **Description:** "Raise a dome of wards over the party: for 3 turns, the next enemy spell aimed at each of them deals half damage."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** A party-wide answer to an enemy caster's big spell.

### Arcane Overload: Tier 7, offensive, one target (new spell)

- **The spell:** 5 AP, 196 Mana, 17,500 silver; power 50 + Int ×2.91 + Wis ×0.83 (160% of the tier's reference, the 5 AP rule). Takes your whole turn (5 AP) for one great blast; a 50% chance to **Expose** the target (1 stack) for 2 turns.
- **Description:** "Pour your whole turn into an arcane overload that blasts one enemy for [X] Basic damage, with a 50% chance to Expose it. This spell costs 5 AP."
- **[X]:** Level 60: 800; Level 100: 1,282.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Overcharge Cannon's high-tier sibling, for a boss.

### Recursion: Tier 8, defensive, yourself (new spell)

- **The spell:** 2 AP, 126 Mana, 25,000 silver. Your next spell within 2 turns is cast a second time at once, with 60% of its power, at no cost (Ur-Mat's Echoing Spells law, on a spell).
- **Description:** "Fold your next spell back on itself: within 2 turns, it is cast a second time for free with 60% of its power. This spell costs 2 AP."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** A burst for the high tiers: with a 3 AP spell, a whole turn of two casts.

### Grand Resonance: Tier 9, offensive, all enemies (new spell)

- **The spell:** 3 AP, 250 Mana, 35,000 silver; power 25 + Int ×1.44 + Wis ×0.41 (55% of the tier's reference, 75% for all enemies, less its bonus). Its **Resonance** counts three times: +30% for each different status on each enemy, up to +90%.
- **Description:** "Ring every status on the enemy line into one grand resonance for [X] Basic damage to all, 30% more against each for every different status it carries (up to 90%)."
- **[X]:** Level 80: 515; Level 100: 635.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Aether Verdict's double Resonance for a whole group: the finisher of a party that mixes elements.

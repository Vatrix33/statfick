# New spells 6: Lightning (task 12)

**Status: a draft for the user's review (SP2, SP3).** Lightning's fourteen new spells: two in each of Tiers 1–5, one
in each of Tiers 6–9. They follow the approved system and curve (`Spell-Augmentation.md`), the sample's style and the
element statuses (`Element-Statuses.md`). Prices are the tier's usual. [X] values are for the raw caster of the system
draft (no classes or gear) at the level the tier unlocks and at Level 100.
**Updated 2026-11-12 (SP60, SP61):** some spells here borrow another element's status; each says so ("borrowed, SP60" or "SP61").

## What Lightning gains

- **Charging up:** Static Charge (your next Lightning spell +25%), Supercharge (an ally's next spell +40%, dearer),
  Capacitor (Mana back for every Shock), Wrath Bolt (a whole 5 AP turn for one bolt).
- **Earlier and wider:** Crackle (a group spell at Tier 1), Arc Flash (2 AP at Tier 3), Thunderclap (a group stun
  chance), Raging Storm (4 AP, Shocks every enemy), Superconductor (Lightning damage to one enemy strikes all).
- **Lasting effects:** Ball Lightning strikes a random enemy at each of your next 3 turns; Static Field hurts and Shocks
  enemies who cast spells.
- **Magnetize** helps the party's weapons.
- **Guards against Lightning:** Grounding (the party), Lightning Rod (draws enemy Lightning spells and turns some of
  them into Mana).

New mechanics: an orb that strikes at each of your turns, a field that punishes spellcasting, Mana regained for each
Shock, drawing enemy spells of one element onto one hero, enemies linked so damage to one spreads to all, a spell
made stronger and dearer.

## At a glance

| Tier | Spell | Kind | AP | Mana | What it does |
|---|---|---|---|---|---|
| 1 | Static Charge | offensive, yourself | 2 | 6 | 2 AP: your next Lightning spell +25% power |
| 1 | Crackle | offensive, all enemies | 3 | 14 | Lightning's first group spell, 15% Shock |
| 2 | Grounding | defensive, all allies | 3 | 22 | every ally: Lightning damage −30%, can't be Shocked, 3 turns |
| 2 | Ball Lightning | offensive, random enemies over 3 turns | 3 | 22 | an orb strikes a random enemy at each of your next 3 turns |
| 3 | Magnetize | offensive, one target | 3 | 35 | Evasion −20%, weapon attacks on it +10%, 2 turns |
| 3 | Arc Flash | offensive, one target | 2 | 25 | 2 AP, 30% Shock |
| 4 | Thunderclap | offensive, all enemies | 3 | 50 | 15% stun and 25% Off-Balance on each enemy (borrowed status) |
| 4 | Capacitor | defensive, yourself | 2 | 35 | 3 turns: you regain 3% Mana whenever your side Shocks an enemy |
| 5 | Static Field | offensive, the whole field | 3 | 75 | 3 turns: every enemy that casts a spell takes [X], 50% Shock |
| 5 | Supercharge | offensive, yourself or an ally | 2 | 52 | 2 AP: their next spell +40% power, costs 30% more |
| 6 | Lightning Rod | defensive, yourself or an ally | 3 | 100 | 3 turns: draws enemy Lightning spells; takes half and turns some into Mana |
| 7 | Raging Storm | offensive, all enemies | 4 | 168 | 4 AP: Shocks every enemy; 30% Soak (borrowed status) |
| 8 | Wrath Bolt | offensive, one target | 5 | 252 | 5 AP: one enormous bolt, 50% stun |
| 9 | Superconductor | offensive, all enemies | 3 | 250 | 3 turns: Lightning damage to one enemy strikes all others for 30% |

## The spells

### Static Charge: Tier 1, offensive, yourself (new spell)

- **The spell:** 2 AP, 6 Mana, 700 silver. Your next Lightning spell within 2 turns gains 25% power (new).
- **Description:** "Build a static charge: your next Lightning spell within 2 turns gains 25% power. This spell costs 2 AP."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** A charge-up that fits the element of heavy strikes, and the 2 AP after a 3 AP spell.

### Crackle: Tier 1, offensive, all enemies (new spell)

- **The spell:** 3 AP, 14 Mana, 700 silver; power 4 + Int ×0.21 + Speed ×0.06 (60% of the tier's reference, 75% for all enemies, less again at Tier 1). Each enemy struck has a 15% chance to be **Shocked**.
- **Description:** "Let lightning crackle over all enemies for [X] Lightning damage; each has a 15% chance to be Shocked."
- **[X]:** Level 1: 7; Level 100: 86.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** Lightning's first group spell is at Tier 4 today (Chain Lightning reaches three).

### Grounding: Tier 2, defensive, all allies (new spell)

- **The spell:** 3 AP, 22 Mana, 1,450 silver. For 3 turns, every ally takes 30% less Lightning damage and can't be Shocked.
- **Description:** "Ground the whole party for 3 turns: 30% less Lightning damage, and no Shock can take hold."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** A guard against Lightning in enemy hands, as Ice's spells stand against Fire.

### Ball Lightning: Tier 2, offensive, random enemies over 3 turns (new spell)

- **The spell:** 3 AP, 22 Mana, 1,450 silver; power 4 + Int ×0.22 + Speed ×0.06 (40% of the tier's reference, per strike, one at each of your next 3 turns). An orb of lightning hovers for 3 turns (new): at the start of each of your turns it strikes a random enemy for [X], with a 20% chance to **Shock** it.
- **Description:** "Release a ball of lightning that hovers for 3 turns: at the start of each of your turns it strikes a random enemy for [X] Lightning damage, with a 20% chance to Shock."
- **[X]:** Level 10: 14; Level 100: 89 (each strike).
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Damage that keeps coming while you cast other spells.

### Magnetize: Tier 3, offensive, one target (new spell)

- **The spell:** 3 AP, 35 Mana, 3,300 silver; power 10 + Int ×0.62 + Speed ×0.18 (80% of the tier's reference). Magnetizes the target for 2 turns (new): its Evasion −20% and weapon attacks against it deal 10% more.
- **Description:** "Magnetize one enemy for [X] Lightning damage: for 2 turns, its Evasion drops by 20% and weapons strike it 10% harder."
- **[X]:** Level 20: 63; Level 100: 251.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Lightning's help for the party's weapon users: iron is drawn to the magnet.

### Arc Flash: Tier 3, offensive, one target (new spell)

- **The spell:** 2 AP, 25 Mana, 3,300 silver; power 8 + Int ×0.5 + Speed ×0.14 (65% of the tier's reference, the 2 AP rule). A 30% chance to **Shock** the target.
- **Description:** "Flash an arc of lightning at one enemy for [X] Lightning damage, with a 30% chance to Shock it. This spell costs 2 AP."
- **[X]:** Level 20: 51; Level 100: 202.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** A fast spell between Spark (Tier 1) and nothing above it until now.

### Thunderclap: Tier 4, offensive, all enemies (new spell)

- **The spell:** 3 AP, 50 Mana, 6,000 silver; power 8 + Int ×0.47 + Speed ×0.14 (50% of the tier's reference, 75% for all enemies, less the stuns). Each enemy struck has a 15% chance to be stunned for 1 turn and a 25% chance to be staggered **Off-Balance** (Wind's status, borrowed, SP60) for 2 turns.
- **Description:** "Clap thunder over all enemies for [X] Lightning damage; each has a 15% chance to be stunned and a 25% chance to be staggered Off-Balance."
- **[X]:** Level 30: 66; Level 100: 191.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** A group stun chance for the mid tiers.

### Capacitor: Tier 4, defensive, yourself (new spell)

- **The spell:** 2 AP, 35 Mana, 6,000 silver. For 3 turns, you regain 3% of your maximum Mana every time your side Shocks an enemy (new).
- **Description:** "Turn yourself into a capacitor for 3 turns: every time your side Shocks an enemy, you regain 3% of your maximum Mana. This spell costs 2 AP."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Lightning's costly spells need Mana; a Shocking party gives it back.

### Static Field: Tier 5, offensive, the whole field (new spell)

- **The spell:** 3 AP, 75 Mana, 8,800 silver; power 8 + Int ×0.49 + Speed ×0.14 (40% of the tier's reference, per spell an enemy casts). For 3 turns the field is charged (new): every enemy that casts a spell takes [X] Lightning damage and has a 50% chance to be **Shocked**.
- **Description:** "Charge the battlefield for 3 turns: every enemy that casts a spell takes [X] Lightning damage, with a 50% chance to be Shocked."
- **[X]:** Level 40: 87; Level 100: 198.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Thorn Dominion punishes weapons; the Static Field punishes spells.

### Supercharge: Tier 5, offensive, yourself or an ally (new spell)

- **The spell:** 2 AP, 52 Mana, 8,800 silver. The next spell of yourself or an ally within 2 turns gains 40% power and costs 30% more Mana (new).
- **Description:** "Supercharge yourself or an ally: their next spell within 2 turns gains 40% power but costs 30% more Mana. This spell costs 2 AP."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** A burst for the party's strongest caster, paid in Mana as Lightning always pays.

### Lightning Rod: Tier 6, defensive, yourself or an ally (new spell)

- **The spell:** 3 AP, 100 Mana, 11,700 silver. For 3 turns, enemy Lightning spells aimed at your side strike them instead, for 50% less, and they regain Mana equal to 20% of the damage (new).
- **Description:** "Make yourself or an ally a lightning rod for 3 turns: enemy Lightning spells at the party strike them instead for half damage, and a fifth of it returns as Mana."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** The answer to the storm of Talos and every Lightning caster.

### Raging Storm: Tier 7, offensive, all enemies (new spell)

- **The spell:** 4 AP, 168 Mana, 17,500 silver; power 28 + Int ×1.64 + Speed ×0.47 (90% of the tier's reference, the 4 AP rule on an all-enemies spell). A heavy 4 AP storm; every enemy struck is always **Shocked**. Each enemy also has a 30% chance to be **Soaked** (Water's status, borrowed, SP61) for 2 turns by the storm's rain, ready for Electrocute.
- **Description:** "Unleash a raging storm on all enemies for [X] Lightning damage, Shocking every one of them. The storm's rain has a 30% chance to Soak each of them. This spell costs 4 AP."
- **[X]:** Level 60: 416; Level 100: 665.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Lightning's biggest group hit below Tier 10, for most of a turn.

### Wrath Bolt: Tier 8, offensive, one target (new spell)

- **The spell:** 5 AP, 252 Mana, 25,000 silver; power 58 + Int ×3.36 + Speed ×0.96 (160% of the tier's reference, the 5 AP rule). Takes your whole turn (5 AP); a 50% chance to stun the target for 1 turn.
- **Description:** "Pour your whole turn into a bolt of wrath on one enemy for [X] Lightning damage, with a 50% chance to stun it. This spell costs 5 AP."
- **[X]:** Level 70: 981; Level 100: 1,362.
- **Augments:** Power, Resistance penetration, Effect chance, Mana cost.
- **Why:** The heaviest single strike of the element of heavy strikes.

### Superconductor: Tier 9, offensive, all enemies (new spell)

- **The spell:** 3 AP, 250 Mana, 35,000 silver; power 25 + Int ×1.44 + Speed ×0.41 (55% of the tier's reference, 75% for all enemies, less the link). Links every enemy struck for 3 turns (new): whenever one takes Lightning damage, every other linked enemy takes 30% of it.
- **Description:** "Turn the enemy line into a superconductor for [X] Lightning damage: for 3 turns, Lightning damage to any of them also strikes all the others for 30%."
- **[X]:** Level 80: 475; Level 100: 584.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Lightning's great group payoff: every bolt after it chains through them all.

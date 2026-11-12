# Batch 12: Blood (task 12)

**Status: approved (2026-11-11, SP47), for the port (SP2).** Every Blood spell players can learn, reworked: 32 spells (Blood has no
boss-only spell). They follow the approved system and curve (`Spell-Augmentation.md`), the sample's style
(`Spell-Sample.md`) and the element statuses (`Element-Statuses.md`): each spell sets its status's chance and duration
(SP32). Tiers and prices stay. "(new)" marks a mechanic the game doesn't have yet. [X] values are for the raw caster of
the system draft (no classes or gear) at the level the tier unlocks and at Level 100; Blood's spells scale with
Intelligence and Vitality (Drain with Vitality first), so a sturdier caster gets more from them than this one.

## Blood's identity

The element of life taken and life paid:
- **Bleeding** (its status: 3% of maximum HP lost at the start of each turn, the caster healing for half) and **Blood
  Harvest** (its reaction: on a Withered enemy, Death's status, the caster heals for 30% of the damage). Death's
  **Exsanguinate** deals the rest of a Bleed at once. Crimson Pinprick, Hemorrhage Lance, Hemalock and Sanguine Deluge
  are built around bleeding; Heartpiercer, Hemorrhage Halo, Crimson Harvest and Arterial Ruin pay it off.
- **Blood price** (new): a few spells cost a share of the caster's HP and much less Mana (Piercing Blood, Sanguine Pact,
  Vessel Covenant), and Crimson Renewal heals an ally with a transfusion of the caster's own HP. Blood is the element
  that spares Mana, kept scarce (SP17), by paying in life.
- **Life drains:** Drain, Sanguine Lance, Scarlet Execution and Blood Sun Apotheosis keep theirs; Heartbreaker shares
  its drain with the party; the Hemoghost drains for its summoner; Sanguine Pike grows the more wounded its caster is.
- **Blood fighters:** Wounded Claw, Pierce In, Bloodbrand, Sanguine Pact and Vessel Covenant charge weapon attacks
  (Pierce In's Armor pierce works, being on a weapon attack); Crimson Pact gives an ally's weapon a life drain.
- Today almost every Blood attack drains life and three of them "ignore Armor", which spells already do; the drains
  stay on some, the Armor pierce goes. Six spells charged one melee attack; they now differ.

New mechanics this batch adds for the port: spells that cost HP (blood price), a transfusion heal, a Bleed that can't be
removed and lasts longer, a Bleed made faster, damage that grows with the caster's missing HP, a drain shared with the
party, a summon that drains life for its summoner, immunity to Bleeding.

## At a glance

| Tier | Spell | Kind | AP | Mana | What changes |
|---|---|---|---|---|---|
| 1 | Drain | offensive, one target | 3 | 15 | drains life: heals you for 40% of the damage |
| 1 | Blood Bolt | offensive, one target | 3 | 14 | 20% chance to Bleed |
| 1 | Wounded Claw | offensive, yourself | 2 | 12 | 2 AP: next weapon attack +[X], heals 25%, 40% Bleed |
| 1 | Crimson Pinprick | offensive, one target | 3 | 14 | 40% chance to Bleed |
| 2 | Piercing Blood | offensive, one target | 3 | 11 | blood price: 11 Mana and 5% of your HP; pierces 30 resistance |
| 2 | Time of the Month | defensive, yourself | 3 | 20 | your Blood spells +15% for 3 turns (kept) |
| 2 | Pierce In | offensive, yourself | 3 | 23 | next weapon attack +[X], 35% Armor pierce kept, 50% Bleed |
| 2 | Coagulate | defensive, yourself | 3 | 23 | heals you, ends your Bleeding, no Bleeding for 2 turns |
| 3 | Hemorrhage Lance | offensive, one target | 3 | 36 | always Bleeds, heals you 15% |
| 3 | Crimson Renewal | defensive, an ally | 3 | 30 | a transfusion: you give 5% of your HP, they gain twice that and more |
| 3 | Bloodbrand | offensive, yourself | 3 | 35 | 2 turns: your weapon hits heal 15% and 30% Bleed; Strength +10% |
| 3 | Summon - Hemoghost | summon | 3 | 35 | drains life for its summoner; 40% Bleed |
| 3 | Scarlet Surge | offensive, all enemies | 3 | 38 | heals you 10%, 25% Bleed on each enemy |
| 4 | Dragonbreath - Crimson Deluge | offensive, all enemies | 3 | 54 | heals you 10% (kept), 25% Bleed |
| 4 | Heartpiercer | offensive, one target | 3 | 52 | +30% on a Bleeding target; heals 20% |
| 4 | Sanguine Pact | offensive, yourself | 3 | 22 | blood price 10% HP: your next 2 weapon attacks +[X], Strength +15% |
| 4 | Crimson Shroud | defensive, yourself | 3 | 51 | retaliation that Bleeds attackers (50%) and heals you |
| 5 | Crimson Nova | offensive, all enemies | 3 | 76 | heals you 15% (kept), 30% Bleed |
| 5 | Sanguine Lance | offensive, one target | 3 | 76 | heals you 30% (kept), 30% Bleed |
| 5 | Crimson Pact | offensive, yourself or an ally | 3 | 70 | 3 turns: their weapon attacks heal them for 20% |
| 5 | Hemalock | offensive, one target | 3 | 77 | always Bleeds; its Bleeding lasts 2 turns longer and can't be removed |
| 6 | Hematic Storm | offensive, all enemies | 3 | 96 | 50% chance to Bleed each enemy |
| 6 | Scarlet Execution | offensive, one target | 3 | 104 | +50% below 30% HP; heals you 35% (kept) |
| 6 | Vessel Covenant | offensive, yourself | 3 | 50 | blood price 15% HP: 3 turns of weapon attacks +[X] |
| 7 | Hemorrhage Halo | offensive, all enemies | 3 | 134 | doubles the Bleeding on every bleeding enemy |
| 7 | Sanguine Pike | offensive, one target | 3 | 146 | +1% per 2% of your missing HP (up to +40%) |
| 7 | Veinward | defensive, all allies | 3 | 140 | heals every ally, ends their Bleeding, Armor +20% kept |
| 8 | Sanguine Deluge | offensive, all enemies | 3 | 170 | every enemy Bleeds for certain; heals you 15% |
| 8 | Heartbreaker | offensive, one target | 3 | 190 | heals every ally for 15% of the damage |
| 9 | Crimson Harvest | offensive, all enemies | 3 | 240 | heals you 30% of the damage to Bleeding enemies, 15% of the rest |
| 9 | Arterial Ruin | offensive, one target | 3 | 260 | a double Bleed (6% a turn); heals you 35% |
| 10 | Blood Sun Apotheosis | offensive, one target | 3 | 350 | heals you 60%, Intelligence +30% kept; always Bleeds |

## The spells

### Drain: Tier 1, offensive, one target

- **Now:** 3 AP, 15 Mana, power 5 + Vit ×0.5 + Int ×0.1. Deal [X] Blood damage, then heal yourself by half the damage dealt.
- **Reworked:** 3 AP, 15 Mana; power 6 + Vit ×0.33 + Int ×0.1 (95% of the tier's reference). **Drains life:** you heal for 40% of the damage dealt (was 50%).
- **Description:** "Drain one enemy for [X] Blood damage and heal yourself for 40% of it."
- **[X], now → reworked:** Level 1: 11 → 10; Level 100: 95 → 78.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Kept as Blood's first life drain; its heal now follows the curve with the hit.

### Blood Bolt: Tier 1, offensive, one target

- **Now:** 3 AP, 14 Mana, power 6 + Int ×0.3 + Vit ×0.2. A bolt of blood that deals [X] Blood damage.
- **Reworked:** 3 AP, 14 Mana; power 6 + Int ×0.33 + Vit ×0.1 (95% of the tier's reference). A 20% chance to make the target **Bleed** for 3 turns.
- **Description:** "A bolt of blood that deals [X] Blood damage, with a 20% chance to make the target Bleed for 3 turns."
- **[X], now → reworked:** Level 1: 11 → 10; Level 100: 134 → 135.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Blood's plain bolt, with the element's status.

### Wounded Claw: Tier 1, offensive, yourself (your next weapon attack)

- **Now:** 3 AP, 17 Mana, power 7 + Int ×0.4 + Vit ×0.2. Your next melee attack deals [X] additional Blood damage and heals you by 25% of that damage.
- **Reworked:** 2 AP, 12 Mana; power 4 + Int ×0.23 + Vit ×0.07 (65% of the tier's reference, the 2 AP rule). Your next weapon attack (melee or ranged, was melee) deals [X] more Blood damage, heals you for 25% of that and has a 40% chance to make the target **Bleed** for 3 turns.
- **Description:** "Wound your weapon with blood: your next attack deals [X] more Blood damage, heals you for a quarter of it, and has a 40% chance to make the target Bleed."
- **[X], now → reworked:** Level 1: 13 → 7; Level 100: 171 → 94.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Blood's quick charge for weapon users, like the other elements'.

### Crimson Pinprick: Tier 1, offensive, one target

- **Now:** 3 AP, 14 Mana, power 6 + Int ×0.3 + Vit ×0.2. Pierce one enemy for [X] Blood damage and heal for 20% of the damage dealt.
- **Reworked:** 3 AP, 14 Mana; power 5 + Int ×0.3 + Vit ×0.09 (85% of the tier's reference). A 40% chance to make the target **Bleed** for 3 turns (was a 20% life drain).
- **Description:** "Prick one enemy for [X] Blood damage, with a 40% chance to open a wound that Bleeds for 3 turns."
- **[X], now → reworked:** Level 1: 11 → 9; Level 100: 134 → 122.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** The Tier 1 spell built around Bleeding.

### Piercing Blood: Tier 2, offensive, one target

- **Now:** 3 AP, 26 Mana, power 12 + Int ×0.3 + Vit ×0.3. Shoot a piercing stream of blood that deals [X] Blood damage and ignores 70% of Armor.
- **Reworked:** 3 AP, 11 Mana; power 9 + Int ×0.54 + Vit ×0.16 (100% of the tier's reference, paid partly in blood). **Blood price** (new): costs 11 Mana and 5% of your maximum HP (was 26 Mana); the target's resistance counts 30 points lower against it (was: ignores 70% of Armor, which spells already ignore).
- **Description:** "Pay in your own blood to shoot a piercing stream for [X] Blood damage that cuts through 30 points of resistance. Costs 5% of your maximum HP as well as Mana."
- **[X], now → reworked:** Level 10: 30 → 35; Level 100: 151 → 219.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Blood magic pays with life: Blood's spells that take HP spare its caster's scarce Mana (SP17).

### Time of the Month: Tier 2, defensive, yourself

- **Now:** 3 AP, 20 Mana, power none. Blood spells deal 15% more damage for 3 turns.
- **Reworked:** 3 AP, 20 Mana. Your Blood spells deal 15% more for 3 turns (kept).
- **Description:** "Blood spells deal 15% more damage for 3 turns."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Kept as it is.

### Pierce In: Tier 2, offensive, yourself (your next weapon attack)

- **Now:** 3 AP, 23 Mana, power 10 + Int ×0.4 + Vit ×0.2. Your next melee attack deals [X] additional Blood damage and gains 35% Armor pierce.
- **Reworked:** 3 AP, 23 Mana; power 8 + Int ×0.46 + Vit ×0.14 (85% of the tier's reference). Your next weapon attack (melee or ranged, was melee) deals [X] more Blood damage, ignores 35% of the target's Armor (kept: on a weapon it works) and has a 50% chance to make it **Bleed** for 3 turns.
- **Description:** "Your next attack deals [X] more Blood damage, ignores 35% of Armor, and has a 50% chance to make the target Bleed."
- **[X], now → reworked:** Level 10: 30 → 30; Level 100: 174 → 187.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Kept, with the element's status: its Armor pierce does work, on a weapon attack.

### Coagulate: Tier 2, defensive, yourself

- **Now:** 3 AP, 23 Mana, power 18 + Int ×0.4 + Vit ×0.3. Seal your wounds, restoring [X] HP plus 5% of maximum HP.
- **Reworked:** 3 AP, 23 Mana; power 8 + Int ×0.49 + Vit ×0.14 (90% of the tier's reference). Heals you for [X] plus 5% of maximum HP (kept), ends your Bleeding and keeps it away for 2 turns (new).
- **Description:** "Seal your wounds, restoring [X] HP plus 5% of maximum HP; any Bleeding stops, and none can start for 2 turns."
- **[X], now → reworked:** Level 10: 40 → 31; Level 100: 193 → 198.
- **Augments:** Power, Effect duration, Mana cost.
- **Why:** Blood's own answer to its status in enemy hands.

### Hemorrhage Lance: Tier 3, offensive, one target

- **Now:** 3 AP, 36 Mana, power 34 + Int ×0.5 + Vit ×0.4. Drive a hardened lance of blood through the target for [X] Blood damage, ignoring 70% of Armor and healing you for 15% of the damage dealt.
- **Reworked:** 3 AP, 36 Mana; power 11 + Int ×0.65 + Vit ×0.19 (85% of the tier's reference). Always makes the target **Bleed** for 3 turns; you heal for 15% of the damage (kept; its 70% Armor pierce goes, as spells already ignore Armor).
- **Description:** "Drive a lance of hardened blood through one enemy for [X] Blood damage; it Bleeds for 3 turns and you heal for 15% of the damage."
- **[X], now → reworked:** Level 20: 84 → 67; Level 100: 256 → 264.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** Blood's sure single Bleed.

### Crimson Renewal: Tier 3, defensive, an ally (not yourself)

- **Now:** 3 AP, 34 Mana, power 30 + Int ×0.5 + Vit ×0.4. Manipulate living blood to restore [X] HP plus 10% of maximum HP to yourself or an ally.
- **Reworked:** 3 AP, 30 Mana; power 11 + Int ×0.65 + Vit ×0.19 (85% of the tier's reference). **Transfusion** (new): you lose 5% of your maximum HP; the ally regains [X] plus 10% of their maximum HP plus twice what you gave (was yourself or an ally, without the cost).
- **Description:** "Give your own blood to an ally: you lose 5% of your maximum HP, and they regain [X] HP plus 10% of their maximum HP plus twice what you gave."
- **[X], now → reworked:** Level 20: 80 → 67; Level 100: 252 → 264.
- **Augments:** Power, Mana cost.
- **Why:** Blood heals by giving blood: a healer who trades their HP for a bigger heal.

### Bloodbrand: Tier 3, offensive, yourself

- **Now:** 3 AP, 35 Mana, power 26 + Int ×0.5 + Vit ×0.4. Brand your weapon in blood. Your next melee attack deals [X] additional Blood damage and heals you for 30% of that bonus damage. Your Strength increases by 10% for 2 turns.
- **Reworked:** 3 AP, 35 Mana. For 2 turns your weapon attacks heal you for 15% of their damage and have a 30% chance to make the target **Bleed**; Strength +10% (was one charged melee attack).
- **Description:** "Brand your weapon in blood for 2 turns: your attacks heal you for 15% of their damage and have a 30% chance to make their target Bleed; your Strength rises by 10%."
- **Augments:** Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Six Blood spells charged one melee attack; Bloodbrand now lasts two turns of attacks.

### Summon - Hemoghost: Tier 3, summon

- **Now:** 3 AP, 35 Mana, power none. Summon a friendly Hemoghost to drain the life from your enemies and aid you in combat.
- **Reworked:** 3 AP, 35 Mana; power 13 + Int ×0.77 + Vit ×0.22 (100% of the tier's reference, as a summon). Summons a Hemoghost (Level 11, 143 HP today); its maximum HP rises by 200% of [X] and every attack it makes by 25% of [X] (SP29). It drains life for its summoner: you heal for 25% of the damage it deals (new); its hits have a 40% chance to make their target **Bleed** for 3 turns. One at a time.
- **Description:** "Summon a friendly Hemoghost whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more, each with a 40% chance to make its target Bleed; you heal for a quarter of the damage it deals. Only one may be active at a time."
- **[X], now → reworked:** Level 20: - → 79; Level 100: - → 312.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** It drains life, as its description promised, for you.

### Scarlet Surge: Tier 3, offensive, all enemies

- **Now:** 3 AP, 38 Mana, power 27 + Int ×0.5 + Vit ×0.4. Wash over all enemies for [X] Blood damage and heal for 10% of damage dealt.
- **Reworked:** 3 AP, 38 Mana; power 8 + Int ×0.5 + Vit ×0.14 (65% of the tier's reference, 75% for all enemies, less its effects). You heal for 10% of all the damage dealt (kept); each enemy struck has a 25% chance to **Bleed** for 3 turns.
- **Description:** "Wash over all enemies for [X] Blood damage, healing yourself for 10% of the damage; each has a 25% chance to Bleed."
- **[X], now → reworked:** Level 20: 77 → 51; Level 100: 249 → 202.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** The early group spell, with the element's status.

### Dragonbreath - Crimson Deluge: Tier 4, offensive, all enemies (Dragonbreath)

- **Now:** 3 AP, 54 Mana, power 35 + Int ×0.6 + Vit ×0.4. Exhale a crimson deluge that deals [X] Blood damage to all enemies and heals you for 10% of the damage dealt.
- **Reworked:** 3 AP, 54 Mana; power 10 + Int ×0.61 + Vit ×0.18 (65% of the tier's reference, 75% for all enemies, less its effects). Its life drain stays (you heal for 10% of the damage; it has no weakening effect for Dragon's Breath to lengthen); each enemy struck has a 25% chance to **Bleed** for 3 turns.
- **Description:** "Exhale a crimson deluge on all enemies for [X] Blood damage, healing yourself for 10% of the damage, with a 25% chance to make each Bleed."
- **[X], now → reworked:** Level 30: 117 → 85; Level 100: 292 → 247.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Blood's Dragonbreath keeps its own effect, like Holy's.

### Heartpiercer: Tier 4, offensive, one target

- **Now:** 3 AP, 52 Mana, power 45 + Int ×0.6 + Vit ×0.4. Shape a needle of hardened blood that deals [X] Blood damage, ignores 80% of Armor, and heals you for 25% of the damage dealt.
- **Reworked:** 3 AP, 52 Mana; power 14 + Int ×0.85 + Vit ×0.24 (90% of the tier's reference). Deals 30% more to a **Bleeding** target; you heal for 20% of the damage (was 25%; its 80% Armor pierce goes).
- **Description:** "Shape a needle of hardened blood for [X] Blood damage, 30% more if the target is Bleeding; you heal for 20% of the damage."
- **[X], now → reworked:** Level 30: 127 → 119; Level 100: 302 → 344.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** The single-target payoff for a Bleeding enemy.

### Sanguine Pact: Tier 4, offensive, yourself (your next two weapon attacks)

- **Now:** 3 AP, 44 Mana, power 40 + Int ×0.6 + Vit ×0.4. Empower your next melee attack to deal [X] additional Blood damage and heal you for half of that bonus damage. Your Strength increases by 15% for 2 turns.
- **Reworked:** 3 AP, 22 Mana; power 10 + Int ×0.56 + Vit ×0.16 (60% of the tier's reference, per attack, paid partly in blood). **Blood price:** costs 22 Mana and 10% of your maximum HP (was 44 Mana); your next 2 weapon attacks each deal [X] more Blood damage and heal you for half of it; Strength +15% for 2 turns (was one melee attack).
- **Description:** "Seal a pact in your own blood: your next two attacks each deal [X] more Blood damage and heal you for half of it, and your Strength rises by 15% for 2 turns. Costs 10% of your maximum HP as well as Mana."
- **[X], now → reworked:** Level 30: 122 → 79; Level 100: 297 → 227 (per attack).
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Blood fighters pay in life for strength.

### Crimson Shroud: Tier 4, defensive, yourself

- **Now:** 3 AP, 51 Mana, power 40 + Int ×0.6 + Vit ×0.4. For 2 turns, melee attackers take [X] Blood damage from you and you receive 15% more healing.
- **Reworked:** 3 AP, 51 Mana; power 8 + Int ×0.47 + Vit ×0.14 (50% of the tier's reference, per retaliation). For 2 turns, melee attackers take [X] Blood damage (kept), have a 50% chance to **Bleed** for 3 turns, and you heal for half of the retaliation (was healing received +15%).
- **Description:** "Wrap yourself in a crimson shroud for 2 turns: melee attackers take [X] Blood damage with a 50% chance to Bleed, and you heal for half of what they take."
- **[X], now → reworked:** Level 30: 122 → 66; Level 100: 297 → 191.
- **Augments:** Power, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Kept as Blood's retaliation, now feeding its caster.

### Crimson Nova: Tier 5, offensive, all enemies

- **Now:** 3 AP, 76 Mana, power 42 + Int ×0.7 + Vit ×0.4. Burst blood magic across all enemies for [X] Blood damage and heal yourself for 15% of the damage dealt.
- **Reworked:** 3 AP, 76 Mana; power 13 + Int ×0.73 + Vit ×0.21 (60% of the tier's reference, 75% for all enemies, less its effects). You heal for 15% of all the damage dealt (kept); each enemy struck has a 30% chance to **Bleed** for 3 turns.
- **Description:** "Burst blood magic across all enemies for [X] Blood damage, healing yourself for 15% of the damage; each has a 30% chance to Bleed."
- **[X], now → reworked:** Level 40: 164 → 131; Level 100: 335 → 296.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Kept, with the element's status.

### Sanguine Lance: Tier 5, offensive, one target

- **Now:** 3 AP, 76 Mana, power 60 + Int ×0.7 + Vit ×0.4. Skewer one enemy for [X] Blood damage and heal yourself for 30% of the damage dealt.
- **Reworked:** 3 AP, 76 Mana; power 19 + Int ×1.1 + Vit ×0.32 (90% of the tier's reference). You heal for 30% of the damage (kept); a 30% chance to make the target **Bleed** for 3 turns.
- **Description:** "Skewer one enemy with a sanguine lance for [X] Blood damage, healing yourself for 30% of it, with a 30% chance to make it Bleed."
- **[X], now → reworked:** Level 40: 182 → 196; Level 100: 353 → 447.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Kept as Blood's mid-tier drain.

### Crimson Pact: Tier 5, offensive, yourself or an ally

- **Now:** 3 AP, 70 Mana, power 48 + Int ×0.7 + Vit ×0.4. Your next melee attack deals [X] extra Blood damage, pierces 20% Armor and heals you for half the bonus damage. Gain 20% Strength for 2 turns.
- **Reworked:** 3 AP, 70 Mana. For 3 turns, the weapon attacks of yourself or an ally heal them for 20% of their damage (new; was one charged melee attack and Strength +20% on yourself).
- **Description:** "Bind yourself or an ally in a crimson pact for 3 turns: their weapon attacks heal them for 20% of the damage dealt."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** A pact a Blood caster can give the party's fighter.

### Hemalock: Tier 5, offensive, one target

- **Now:** 3 AP, 77 Mana, power 60 + Int ×0.7 + Vit ×0.4. Seal one enemy's blood for [X] Blood damage and reduce its healing received by 25% for 3 turns.
- **Reworked:** 3 AP, 77 Mana; power 17 + Int ×0.98 + Vit ×0.28 (80% of the tier's reference). Always makes the target **Bleed** for 3 turns; its Bleeding lasts 2 turns longer and can't be removed (new; was healing received −25%, which is Death's now).
- **Description:** "Seal one enemy's blood for [X] Blood damage: it Bleeds for 3 turns, its Bleeding lasts 2 turns longer, and nothing can stop it."
- **[X], now → reworked:** Level 40: 182 → 175; Level 100: 353 → 397.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** A Bleed that can't be cleansed, for a boss with a healer.

### Hematic Storm: Tier 6, offensive, all enemies

- **Now:** 3 AP, 96 Mana, power 52 + Int ×0.8 + Vit ×0.4. Spray all enemies with blood shards for [X] Blood damage and heal yourself for 15% of the damage dealt.
- **Reworked:** 3 AP, 96 Mana; power 15 + Int ×0.88 + Vit ×0.25 (60% of the tier's reference, 75% for all enemies, less its likely Bleed). Each enemy struck has a 50% chance to **Bleed** for 3 turns (was a 15% life drain).
- **Description:** "Spray all enemies with blood shards for [X] Blood damage; each has a 50% chance to Bleed for 3 turns."
- **[X], now → reworked:** Level 50: 221 → 190; Level 100: 381 → 356.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Built around bleeding, so it often does; Blood has life drains enough elsewhere.

### Scarlet Execution: Tier 6, offensive, one target

- **Now:** 3 AP, 104 Mana, power 70 + Int ×0.8 + Vit ×0.4. Impale one enemy for [X] Blood damage and heal yourself for 35% of the damage dealt.
- **Reworked:** 3 AP, 104 Mana; power 22 + Int ×1.32 + Vit ×0.38 (90% of the tier's reference). Deals 50% more to a target below 30% of its HP (new); you heal for 35% of the damage (kept).
- **Description:** "Impale one enemy for [X] Blood damage, 50% more if it is below 30% of its HP; you heal for 35% of the damage."
- **[X], now → reworked:** Level 50: 239 → 285; Level 100: 399 → 535.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** An execution that finishes a wounded enemy.

### Vessel Covenant: Tier 6, offensive, yourself

- **Now:** 3 AP, 100 Mana, power 58 + Int ×0.8 + Vit ×0.4. Your next melee attack deals [X] extra Blood damage and heals you for 40% of that bonus damage. Gain 25% Strength for 2 turns.
- **Reworked:** 3 AP, 50 Mana; power 12 + Int ×0.73 + Vit ×0.21 (50% of the tier's reference, per attack, paid partly in blood). **Blood price:** costs 50 Mana and 15% of your maximum HP (was 100 Mana); for 3 turns every weapon attack you make deals [X] more Blood damage and heals you for 30% of it (was one melee attack and Strength +25%).
- **Description:** "Make yourself a vessel of blood for 3 turns: every attack you make deals [X] more Blood damage and heals you for 30% of it. Costs 15% of your maximum HP as well as Mana."
- **[X], now → reworked:** Level 50: 227 → 157; Level 100: 387 → 296 (per attack).
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** Blood's great charge for a fighter, paid in life.

### Hemorrhage Halo: Tier 7, offensive, all enemies

- **Now:** 3 AP, 134 Mana, power 64 + Int ×0.8 + Vit ×0.5. Cut all enemies for [X] Blood damage and heal for 10% of damage dealt.
- **Reworked:** 3 AP, 134 Mana; power 19 + Int ×1.09 + Vit ×0.31 (60% of the tier's reference, 75% for all enemies, less the bonus). Every enemy already **Bleeding** bleeds twice as fast for the rest of its Bleeding (new; was a 10% life drain).
- **Description:** "Raise a halo of hemorrhage over all enemies for [X] Blood damage; every one that is Bleeding bleeds twice as fast for the rest of it."
- **[X], now → reworked:** Level 60: 272 → 277; Level 100: 404 → 442.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** The group payoff for a bleeding field.

### Sanguine Pike: Tier 7, offensive, one target

- **Now:** 3 AP, 146 Mana, power 82 + Int ×0.8 + Vit ×0.5. Impale one enemy for [X] Blood damage and heal for 25% of damage dealt.
- **Reworked:** 3 AP, 146 Mana; power 28 + Int ×1.64 + Vit ×0.47 (90% of the tier's reference). Deals 1% more for every 2% of your HP you are missing, up to +40% (new; was a 25% life drain).
- **Description:** "Impale one enemy on a sanguine pike for [X] Blood damage, more the more wounded you are (up to 40% more)."
- **[X], now → reworked:** Level 60: 290 → 416; Level 100: 422 → 665.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Blood's desperate strike, for a caster who pays in HP.

### Veinward: Tier 7, defensive, all allies

- **Now:** 3 AP, 140 Mana, power 64 + Int ×0.8 + Vit ×0.5. Restore [X] HP plus 10% maximum HP to all allies and increase their Armor by 20% for 2 turns.
- **Reworked:** 3 AP, 140 Mana; power 20 + Int ×1.18 + Vit ×0.34 (65% of the tier's reference, 75% for all allies, less its effects). Heals every ally for [X] plus 10% of maximum HP and ends their Bleeding; Armor +20% for 2 turns (kept).
- **Description:** "Ward the party's veins: every ally regains [X] HP plus 10% of maximum HP, stops Bleeding and has 20% more Armor for 2 turns."
- **[X], now → reworked:** Level 60: 272 → 300; Level 100: 404 → 478.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** Kept as Blood's party heal.

### Sanguine Deluge: Tier 8, offensive, all enemies

- **Now:** 3 AP, 170 Mana, power 70 + Int ×0.9 + Vit ×0.6. Cut all enemies for [X] Blood damage and heal for 15% of damage dealt.
- **Reworked:** 3 AP, 170 Mana; power 20 + Int ×1.16 + Vit ×0.33 (55% of the tier's reference, 75% for all enemies, less its sure Bleed). Every enemy struck always **Bleeds** for 3 turns; you heal for 15% of the damage (kept).
- **Description:** "Bring a sanguine deluge on all enemies for [X] Blood damage: every one of them Bleeds for 3 turns, and you heal for 15% of the damage."
- **[X], now → reworked:** Level 70: 344 → 338; Level 100: 456 → 470.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** Blood's sure group Bleed.

### Heartbreaker: Tier 8, offensive, one target

- **Now:** 3 AP, 190 Mana, power 90 + Int ×0.9 + Vit ×0.6. Pierce one enemy for [X] Blood damage and heal for 35% of damage dealt.
- **Reworked:** 3 AP, 190 Mana; power 34 + Int ×1.99 + Vit ×0.57 (95% of the tier's reference). Every ally heals for 15% of the damage dealt (new; was a 35% drain for you alone).
- **Description:** "Break one enemy's heart for [X] Blood damage; every ally heals for 15% of the damage dealt."
- **[X], now → reworked:** Level 70: 364 → 581; Level 100: 476 → 807.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Blood's drain shared with the party.

### Crimson Harvest: Tier 9, offensive, all enemies

- **Now:** 3 AP, 240 Mana, power 85 + Int ×1 + Vit ×0.6. Bleed all enemies for [X] Blood damage and heal for 15% of damage dealt.
- **Reworked:** 3 AP, 240 Mana; power 27 + Int ×1.57 + Vit ×0.45 (60% of the tier's reference, 75% for all enemies, less the harvest). You heal for 30% of the damage dealt to **Bleeding** enemies and 15% of the rest (was 15% of all).
- **Description:** "Harvest all enemies for [X] Blood damage; you heal for 30% of the damage dealt to those that are Bleeding, and 15% of the rest."
- **[X], now → reworked:** Level 80: 425 → 518; Level 100: 507 → 637.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** The great drain of a bleeding field.

### Arterial Ruin: Tier 9, offensive, one target

- **Now:** 3 AP, 260 Mana, power 95 + Int ×1 + Vit ×0.6. Pierce one enemy for [X] Blood damage, heal for 35% of damage dealt and lower its Armor by 20% for 2 turns.
- **Reworked:** 3 AP, 260 Mana; power 43 + Int ×2.49 + Vit ×0.71 (95% of the tier's reference). The target always **Bleeds** twice as fast (6% of its maximum HP a turn) for 3 turns; you heal for 35% of the damage (kept; its Armor −20% goes).
- **Description:** "Ruin one enemy's arteries for [X] Blood damage: it bleeds 6% of its maximum HP a turn for 3 turns, and you heal for 35% of the damage."
- **[X], now → reworked:** Level 80: 435 → 821; Level 100: 517 → 1,009.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** The heaviest Bleed in the game.

### Blood Sun Apotheosis: Tier 10, offensive, one target

- **Now:** 3 AP, 350 Mana, power 350 + Int ×1.1 + Vit ×0.7. Deal [X] Blood damage to one enemy, heal for 60% of damage dealt and increase your Intelligence by 30% for 3 turns.
- **Reworked:** 3 AP, 350 Mana; power 62 + Int ×3.62 + Vit ×1.03 (90% of the tier's reference). You heal for 60% of the damage and your Intelligence rises by 30% for 3 turns (kept); the target always **Bleeds** for 3 turns (new).
- **Description:** "Become the blood sun over one enemy for [X] Blood damage: you heal for 60% of it, your Intelligence rises by 30% for 3 turns, and the target Bleeds for 3 turns."
- **[X], now → reworked:** Level 90: 773 → 1,330; Level 100: 819 → 1,467.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Blood's Tier 10 stays a single-target apotheosis.

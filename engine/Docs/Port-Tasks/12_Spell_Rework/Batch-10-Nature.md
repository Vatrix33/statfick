# Batch 10: Nature (task 12)

**Status: a draft for the user's review (SP2).** Every Nature spell players can learn, reworked: 32 spells (Wheel of
the Year, Juila's, is left out, SP4). They follow the approved system and curve (`Spell-Augmentation.md`), the sample's
style (`Spell-Sample.md`) and the element statuses (`Element-Statuses.md`): each spell sets its status's chance and
duration (SP32). Tiers and prices stay. "(new)" marks a mechanic the game doesn't have yet. [X] values are for the raw
caster of the system draft (no classes or gear) at the level the tier unlocks and at Level 100, shown for a
Wisdom-first caster as Nature's spells scale with Wisdom and Mind.
**Updated 2026-11-12 (SP60):** some spells here borrow another element's status; each says so ("borrowed, SP60").

## Nature's identity

The element of growth, beasts and binding:
- **Rooted** (its status: Evasion halved, can't flee) makes enemies easy for the party's weapons; **Overgrowth** (its
  reaction: on a Cracked enemy, Earth's status, it Roots for certain). Bramble Bind, Verdant Eruption, Rooting Lance,
  Briarstorm and Ancestor Root are built around rooting; Verdant Tempest and Rootbreaker pay it off.
- **Beasts:** five summons, now growing with spell power: a wolf pack (two at once), two bears (the grown one enrages
  when wounded), the Elder Treant tending the party, the High Griffon striking two enemies.
- **Growth for the party:** Nature's heals raise the healing their target receives (Regrowth, Greater Regrowth,
  Ancient Bark, Worldroot Balm, Worldbloom) or its maximum HP (Verdant Renewal, Elderwood Renewal); Touch Grass and the
  Worldseed Miracle cleanse.
- **Thorns:** Thorn Dominion turns the field against enemy weapon attacks, Thorn Tyrant's thorns grow inside its target,
  Thorn Tempest and Heartwood Spear keep their weakening for weapon users.
- Speed loss moves to Water and Ice, Armor loss to Earth (Heartwood Spear keeps its own), healing loss to Death. Nature
  now gets a party heal at Tier 6 (Worldroot Balm), where today its first is at Tier 8.

New mechanics this batch adds for the port: two of one summon at once, a summon that enrages when wounded, a summon that
heals the party each turn, a summon whose first attack strikes two, a field that hurts enemies who make weapon attacks,
thorns that grow inside a target, raising maximum HP for a few turns.

## At a glance

| Tier | Spell | Kind | AP | Mana | What changes |
|---|---|---|---|---|---|
| 1 | Summon - Juvenile Wolf | summon | 3 | 16 | a pack: up to two wolves at once |
| 1 | Leaf Bolt | offensive, one target | 3 | 14 | 20% chance to Root |
| 1 | Touch Grass | defensive, yourself | 3 | 13 | heal yourself and remove a debuff |
| 1 | Sapling Guard | defensive, yourself or an ally | 3 | 14 | Armor +20%; the next melee attacker 50% Rooted |
| 2 | Summon - Juvenile Bear | summon | 3 | 27 | the cub grows with spell power; 30% Root |
| 2 | Return to Form | offensive, one target | 3 | 22 | +70% against Undead (kept) |
| 2 | Thorns Shot | offensive, one target | 2 | 15 | 2 AP kept, 25% Root |
| 2 | Bramble Bind | offensive, one target | 3 | 24 | 50% chance to Root |
| 3 | Summon - Adult Bear | summon | 3 | 39 | enrages below half HP (+30%); 40% Root |
| 3 | Briar Volley | offensive, all enemies | 3 | 36 | 25% Root and 25% Bleed on each enemy (borrowed status) |
| 3 | Regrowth | defensive, yourself or an ally | 3 | 30 | heals, removes a debuff, healing received +20% |
| 3 | Vine Whip | offensive, one target | 3 | 35 | 40% chance to Root |
| 4 | Verdant Eruption | offensive, all enemies | 3 | 55 | 50% chance to Root each enemy |
| 4 | Greater Regrowth | defensive, yourself or an ally | 3 | 50 | heals, removes a debuff, healing received +25% |
| 4 | Ancient Bark | defensive, yourself or an ally | 3 | 45 | Armor +25%, healing received +25% |
| 4 | Rooting Lance | offensive, one target | 3 | 51 | always Roots |
| 5 | Summon - Elder Treant | summon | 3 | 82 | heals the most wounded ally each turn; 50% Root |
| 5 | Briarstorm | offensive, all enemies | 3 | 75 | always Roots every enemy |
| 5 | Verdant Renewal | defensive, yourself or an ally | 3 | 74 | heals and raises their maximum HP by 10% |
| 5 | Dragonbreath - Blight Bloom | offensive, all enemies | 3 | 79 | all stats −5% kept (Dragonbreath), 25% Root |
| 5 | Verdant Tempest | offensive, all enemies | 3 | 77 | +25% on Rooted enemies |
| 6 | Thorn Dominion | offensive, all enemies | 3 | 96 | 2 turns: every enemy weapon attack costs it 30% of [X] |
| 6 | Rootbreaker | offensive, one target | 3 | 104 | +40% on a Rooted target, ending the Root |
| 6 | Worldroot Balm | defensive, all allies | 3 | 100 | heals every ally, healing received +20% |
| 7 | Summon - High Griffon | summon | 3 | 140 | flies: Evasion +30%, its first attack each turn strikes two |
| 7 | Thorn Tempest | offensive, all enemies | 3 | 134 | Dexterity −20% kept |
| 7 | Heartwood Spear | offensive, one target | 3 | 146 | Armor −25% kept, 40% Root |
| 8 | Worldbloom | defensive, all allies | 3 | 170 | heals every ally, healing received +20% |
| 8 | Ancestor Root | offensive, one target | 3 | 190 | always Roots for 3 turns |
| 9 | Elderwood Renewal | defensive, all allies | 3 | 240 | heals every ally, maximum HP +15% |
| 9 | Thorn Tyrant | offensive, one target | 3 | 260 | thorns grow in it: 20% of [X] each turn; always Roots |
| 10 | Worldseed Miracle | defensive, all allies | 3 | 350 | heals, all stats +10% kept, cleanses every ally |

## The spells

### Summon - Juvenile Wolf: Tier 1, summon

- **Now:** 3 AP, 16 Mana, power none. Summon a friendly Juvenile Wolf to help you in battle.
- **Reworked:** 3 AP, 16 Mana; power 6 + Wis ×0.35 + Mind ×0.1 (100% of the tier's reference, as a summon). Summons a Juvenile Wolf (Level 2, 16 HP today); its maximum HP rises by 200% of [X] and every attack it makes by 25% of [X] (SP29). A pack: up to two Juvenile Wolves may be active at once (new; today one). Their hits have a 25% chance to **Root** their target for 2 turns.
- **Description:** "Summon a friendly Juvenile Wolf whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more, each with a 25% chance to Root. Up to two may be active at a time."
- **[X], now → reworked:** Level 1: - → 10; Level 100: - → 154.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Wolves hunt in packs; today a single Level 2 wolf fades at once.

### Leaf Bolt: Tier 1, offensive, one target

- **Now:** 3 AP, 14 Mana, power 6 + Wis ×0.3 + Mind ×0.2. A bolt of leaves that deals [X] Nature damage.
- **Reworked:** 3 AP, 14 Mana; power 6 + Wis ×0.33 + Mind ×0.1 (95% of the tier's reference). A 20% chance to **Root** the target for 2 turns.
- **Description:** "A bolt of leaves that deals [X] Nature damage, with a 20% chance to Root the target for 2 turns."
- **[X], now → reworked:** Level 1: 11 → 10; Level 100: 159 → 147.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Nature's plain bolt, with the element's status.

### Touch Grass: Tier 1, defensive, yourself

- **Now:** 3 AP, 13 Mana, power 12 + Mind ×0.3 + Wis ×0.1. Heal yourself by [X] HP, then increase your Mind by 5% for 3 turns.
- **Reworked:** 3 AP, 13 Mana; power 5 + Wis ×0.32 + Mind ×0.09 (90% of the tier's reference). Heals you for [X] and removes one debuff (was a heal and Mind +5% for 3 turns).
- **Description:** "Touch the grass: heal yourself by [X] HP and shake off one debuff."
- **[X], now → reworked:** Level 1: 16 → 9; Level 100: 117 → 140.
- **Augments:** Power, Mana cost.
- **Why:** Mind +5% did little; a cleanse suits Nature's first heal.

### Sapling Guard: Tier 1, defensive, yourself or an ally

- **Now:** 3 AP, 14 Mana, power 5 + Wis ×0.3 + Mind ×0.2. Grow a living shield around yourself or an ally, granting [X] Armor for 2 turns.
- **Reworked:** 3 AP, 14 Mana. For 2 turns: Armor +20%, and the next melee attacker has a 50% chance to be **Rooted** for 2 turns (was Armor +[X]).
- **Description:** "Grow a living shield around yourself or an ally for 2 turns: Armor +20%, and the next enemy to strike them in melee has a 50% chance to be Rooted."
- **Augments:** Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Flat Armor becomes a percentage, as in the earlier batches.

### Summon - Juvenile Bear: Tier 2, summon

- **Now:** 3 AP, 27 Mana, power none. Summon a friendly Juvenile Bear to aid you in battle.
- **Reworked:** 3 AP, 27 Mana; power 9 + Wis ×0.54 + Mind ×0.16 (100% of the tier's reference, as a summon). Summons a Juvenile Bear (Level 3, 37 HP today); its maximum HP rises by 200% of [X] and every attack it makes by 25% of [X] (SP29). Its hits have a 30% chance to **Root** their target for 2 turns. One at a time.
- **Description:** "Summon a friendly Juvenile Bear whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more, each with a 30% chance to Root. Only one may be active at a time."
- **[X], now → reworked:** Level 10: - → 36; Level 100: - → 239.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Today a Level 3 cub; it now grows with spell power.

### Return to Form: Tier 2, offensive, one target

- **Now:** 3 AP, 22 Mana, power 11 + Wis ×0.4 + Mind ×0.1. A burst of Nature Mana that deals [X] Nature damage and deals 70% more damage to Undead.
- **Reworked:** 3 AP, 22 Mana; power 8 + Wis ×0.49 + Mind ×0.14 (90% of the tier's reference). Deals 70% more to Undead (kept): nature returns the dead to the earth.
- **Description:** "A burst of Nature Mana that deals [X] Nature damage, 70% more against Undead."
- **[X], now → reworked:** Level 10: 30 → 33; Level 100: 177 → 216.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** Kept: its bonus is one of a kind among Nature's spells.

### Thorns Shot: Tier 2, offensive, one target

- **Now:** 2 AP, 20 Mana, power 10 + Wis ×0.4 + Mind ×0.2. Fire a quick burst of thorns that deals [X] Nature damage. This spell costs 2 Action Points.
- **Reworked:** 2 AP, 15 Mana; power 6 + Wis ×0.35 + Mind ×0.1 (65% of the tier's reference, the 2 AP rule). Stays a 2 AP spell; a 25% chance to **Root** the target for 2 turns.
- **Description:** "Fire a quick burst of thorns for [X] Nature damage, with a 25% chance to Root the target. This spell costs 2 AP."
- **[X], now → reworked:** Level 10: 32 → 24; Level 100: 199 → 154.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Nature's fast spell; its power and Mana follow the 2 AP rule.

### Bramble Bind: Tier 2, offensive, one target

- **Now:** 3 AP, 24 Mana, power 12 + Wis ×0.4 + Mind ×0.2. Bind one enemy in thorns for [X] Nature damage and reduce its Evasion by 15% for 2 turns.
- **Reworked:** 3 AP, 24 Mana; power 8 + Wis ×0.46 + Mind ×0.14 (85% of the tier's reference). A 50% chance to **Root** the target for 2 turns (was Evasion −15%, which Rooted now halves).
- **Description:** "Bind one enemy in brambles for [X] Nature damage, with a 50% chance to Root it for 2 turns."
- **[X], now → reworked:** Level 10: 34 → 32; Level 100: 201 → 205.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** A binding, so its status is likely.

### Summon - Adult Bear: Tier 3, summon

- **Now:** 3 AP, 39 Mana, power none. Summon a friendly Adult Bear with 25% increased max HP and 20% increased attack damage.
- **Reworked:** 3 AP, 39 Mana; power 13 + Wis ×0.77 + Mind ×0.22 (100% of the tier's reference, as a summon). Summons an Adult Bear (Level 8, 93 HP today, summoned with +25% HP and +20% damage); its maximum HP rises by 200% of [X] and every attack it makes by 25% of [X] (SP29). Below half of its HP it enrages: its attacks deal 30% more (new); its hits have a 40% chance to **Root** their target for 2 turns. One at a time.
- **Description:** "Summon a friendly Adult Bear whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more, each with a 40% chance to Root; below half of its HP it enrages and deals 30% more. Only one may be active at a time."
- **[X], now → reworked:** Level 20: - → 84; Level 100: - → 339.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** A bear that is more dangerous wounded.

### Briar Volley: Tier 3, offensive, all enemies

- **Now:** 3 AP, 36 Mana, power 32 + Wis ×0.5 + Mind ×0.3. Launch a storm of living thorns that deals [X] Nature damage to all enemies.
- **Reworked:** 3 AP, 36 Mana; power 8 + Wis ×0.5 + Mind ×0.14 (65% of the tier's reference, 75% for all enemies, less its effect). Each enemy struck has a 25% chance to be **Rooted** for 2 turns and a 25% chance to **Bleed** (Blood's status, borrowed, SP60) for 3 turns.
- **Description:** "Launch a storm of living thorns at all enemies for [X] Nature damage; each has a 25% chance to be Rooted and a 25% chance to Bleed."
- **[X], now → reworked:** Level 20: 86 → 54; Level 100: 280 → 219.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** The early group spell, with the element's status.

### Regrowth: Tier 3, defensive, yourself or an ally

- **Now:** 3 AP, 30 Mana, power 28 + Wis ×0.5 + Mind ×0.3. Restore [X] HP plus 15% of maximum HP to yourself or an ally and remove one random debuff from them.
- **Reworked:** 3 AP, 30 Mana; power 11 + Wis ×0.65 + Mind ×0.19 (85% of the tier's reference). Heals [X] plus 10% of maximum HP, removes one debuff, and raises their healing received by 20% for 3 turns (was [X] plus 15% and one random debuff).
- **Description:** "Regrow yourself or an ally: [X] HP plus 10% of maximum HP restored, one debuff removed, and 20% more healing received for 3 turns."
- **[X], now → reworked:** Level 20: 82 → 71; Level 100: 276 → 287.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** Nature's heals help the next heals take: the party's healer works better after them.

### Vine Whip: Tier 3, offensive, one target

- **Now:** 3 AP, 35 Mana, power 37 + Wis ×0.5 + Mind ×0.3. Lash one enemy for [X] Nature damage and reduce its Speed by 15% for 2 turns.
- **Reworked:** 3 AP, 35 Mana; power 11 + Wis ×0.65 + Mind ×0.19 (85% of the tier's reference). A 40% chance to **Root** the target for 2 turns (was Speed −15%).
- **Description:** "Lash one enemy with a vine for [X] Nature damage, with a 40% chance to Root it."
- **[X], now → reworked:** Level 20: 91 → 71; Level 100: 285 → 287.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Speed loss belongs to Water and Ice.

### Verdant Eruption: Tier 4, offensive, all enemies

- **Now:** 3 AP, 55 Mana, power 35 + Wis ×0.6 + Mind ×0.3. Detonate a mass of grasping roots beneath all enemies for [X] Nature damage and decrease their Speed by 15% for 3 turns.
- **Reworked:** 3 AP, 55 Mana; power 10 + Wis ×0.56 + Mind ×0.16 (60% of the tier's reference, 75% for all enemies, less its likely Root). Each enemy struck has a 50% chance to be **Rooted** for 2 turns (was Speed −15% for 3 turns).
- **Description:** "Detonate grasping roots beneath all enemies for [X] Nature damage; each has a 50% chance to be Rooted."
- **[X], now → reworked:** Level 30: 124 → 85; Level 100: 319 → 247.
- **Augments:** Power, Resistance penetration, Effect duration, Effect chance, Mana cost.
- **Why:** Built around rooting, so it often does.

### Greater Regrowth: Tier 4, defensive, yourself or an ally

- **Now:** 3 AP, 50 Mana, power 45 + Wis ×0.6 + Mind ×0.3. Restore [X] HP plus 20% of maximum HP to yourself or an ally and remove one random debuff.
- **Reworked:** 3 AP, 50 Mana; power 14 + Wis ×0.8 + Mind ×0.23 (85% of the tier's reference). Heals [X] plus 15% of maximum HP, removes one debuff, and raises their healing received by 25% for 3 turns (was [X] plus 20% and one random debuff).
- **Description:** "Regrow yourself or an ally: [X] HP plus 15% of maximum HP restored, one debuff removed, and 25% more healing received for 3 turns."
- **[X], now → reworked:** Level 30: 134 → 121; Level 100: 329 → 353.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** Regrowth's stronger sibling.

### Ancient Bark: Tier 4, defensive, yourself or an ally

- **Now:** 3 AP, 45 Mana, power 40 + Wis ×0.6 + Mind ×0.3. Cover yourself or an ally in ancient bark for 3 turns, increasing Armor by [X] and healing received by 25%.
- **Reworked:** 3 AP, 45 Mana. For 3 turns: Armor +25% and healing received +25% (was Armor +[X]).
- **Description:** "Cover yourself or an ally in ancient bark for 3 turns, increasing Armor by 25% and healing received by 25%."
- **Augments:** Effect strength, Effect duration, Mana cost.
- **Why:** Kept, as a percentage.

### Rooting Lance: Tier 4, offensive, one target

- **Now:** 3 AP, 51 Mana, power 48 + Wis ×0.6 + Mind ×0.3. Impale one enemy with roots for [X] Nature damage and reduce its Speed by 20% for 2 turns.
- **Reworked:** 3 AP, 51 Mana; power 13 + Wis ×0.75 + Mind ×0.22 (80% of the tier's reference). Always **Roots** the target for 2 turns (was Speed −20%).
- **Description:** "Impale one enemy with roots for [X] Nature damage and Root it for 2 turns."
- **[X], now → reworked:** Level 30: 137 → 114; Level 100: 332 → 332.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** Nature's sure single Root.

### Summon - Elder Treant: Tier 5, summon

- **Now:** 3 AP, 82 Mana, power none. Summon a friendly Elder Treant with 20% increased maximum HP. Only one may be active at a time.
- **Reworked:** 3 AP, 82 Mana; power 21 + Wis ×1.22 + Mind ×0.35 (100% of the tier's reference, as a summon). Summons an Elder Treant (Level 20, 378 HP today, summoned with +20% HP); its maximum HP rises by 200% of [X] and every attack it makes by 25% of [X] (SP29). Its hits have a 50% chance to **Root** their target for 2 turns; at the start of each of its turns it heals the most wounded ally for 15% of [X] (new). One at a time.
- **Description:** "Summon a friendly Elder Treant whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more, each with a 50% chance to Root; at the start of each of its turns it heals your most wounded ally. Only one may be active at a time."
- **[X], now → reworked:** Level 40: - → 235; Level 100: - → 538.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** The forest's elder tends the party while it stands.

### Briarstorm: Tier 5, offensive, all enemies

- **Now:** 3 AP, 75 Mana, power 43 + Wis ×0.7 + Mind ×0.3. Entangle all enemies in cutting briars for [X] Nature damage and reduce their Speed by 20% for 2 turns.
- **Reworked:** 3 AP, 75 Mana; power 12 + Wis ×0.67 + Mind ×0.19 (55% of the tier's reference, 75% for all enemies, less its sure Root). Always **Roots** every enemy struck for 2 turns (was Speed −20%).
- **Description:** "Entangle all enemies in cutting briars for [X] Nature damage, Rooting every one of them for 2 turns."
- **[X], now → reworked:** Level 40: 175 → 129; Level 100: 362 → 295.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** Nature's sure group Root: every enemy easy to hit.

### Verdant Renewal: Tier 5, defensive, yourself or an ally

- **Now:** 3 AP, 74 Mana, power 54 + Wis ×0.7 + Mind ×0.3. Restore [X] HP plus 20% of maximum HP to yourself or an ally and increase their healing received by 25% for 3 turns.
- **Reworked:** 3 AP, 74 Mana; power 18 + Wis ×1.04 + Mind ×0.3 (85% of the tier's reference). Heals [X] plus 15% of maximum HP and raises their maximum HP by 10% for 3 turns (new; was [X] plus 20% and healing received +25%).
- **Description:** "Renew yourself or an ally with verdant growth: [X] HP plus 15% of maximum HP restored, and their maximum HP grows by 10% for 3 turns."
- **[X], now → reworked:** Level 40: 186 → 200; Level 100: 373 → 459.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** A heal that grows its target.

### Dragonbreath - Blight Bloom: Tier 5, offensive, all enemies (Dragonbreath)

- **Now:** 3 AP, 79 Mana, power 40 + Wis ×0.7 + Mind ×0.3. Exhale a cloud of barbed spores for [X] Nature damage to all enemies and reduce all their basic stats by 5% for 2 turns.
- **Reworked:** 3 AP, 79 Mana; power 14 + Wis ×0.79 + Mind ×0.23 (65% of the tier's reference, 75% for all enemies, less its effects). Its weakening effect stays (all basic stats −5% for 2 turns); each enemy struck has a 25% chance to be **Rooted** for 2 turns.
- **Description:** "Exhale a cloud of barbed spores on all enemies for [X] Nature damage, reducing all their basic stats by 5% for 2 turns, with a 25% chance to Root each."
- **[X], now → reworked:** Level 40: 172 → 153; Level 100: 359 → 350.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Dragonbreath spells keep their weakening effect.

### Verdant Tempest: Tier 5, offensive, all enemies

- **Now:** 3 AP, 77 Mana, power 43 + Wis ×0.7 + Mind ×0.3. Sweep all enemies with thorn-laden winds for [X] Nature damage and reduce their Armor by 15% for 2 turns.
- **Reworked:** 3 AP, 77 Mana; power 14 + Wis ×0.79 + Mind ×0.23 (65% of the tier's reference, 75% for all enemies, less the bonus). Enemies that are **Rooted** take 25% more (was Armor −15%, which is Earth's now).
- **Description:** "Sweep all enemies with thorn-laden winds for [X] Nature damage, 25% more against those that are Rooted."
- **[X], now → reworked:** Level 40: 175 → 153; Level 100: 362 → 350.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** The group payoff for a rooted field.

### Thorn Dominion: Tier 6, offensive, all enemies

- **Now:** 3 AP, 96 Mana, power 52 + Wis ×0.8 + Mind ×0.3. Fill the battlefield with briars for [X] Nature damage to all enemies and reduce their Speed by 25% for 2 turns.
- **Reworked:** 3 AP, 96 Mana; power 14 + Wis ×0.81 + Mind ×0.23 (55% of the tier's reference, 75% for all enemies, less the thorn field). Briars cover the field for 2 turns (new): every enemy that makes a weapon attack takes 30% of [X] Nature damage (was Speed −25%).
- **Description:** "Fill the battlefield with briars for [X] Nature damage to all enemies; for 2 turns, every enemy that makes a weapon attack takes 30% of that."
- **[X], now → reworked:** Level 50: 233 → 189; Level 100: 407 → 357.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** A field that punishes a pack of melee or ranged enemies.

### Rootbreaker: Tier 6, offensive, one target

- **Now:** 3 AP, 104 Mana, power 70 + Wis ×0.8 + Mind ×0.3. Drive a living root through one enemy for [X] Nature damage and reduce its Evasion by 25% for 2 turns.
- **Reworked:** 3 AP, 104 Mana; power 22 + Wis ×1.32 + Mind ×0.38 (90% of the tier's reference). Deals 40% more to a **Rooted** target, and its Root ends (was Evasion −25%).
- **Description:** "Drive a living root through one enemy for [X] Nature damage, 40% more if it is Rooted (which ends it)."
- **[X], now → reworked:** Level 50: 251 → 308; Level 100: 425 → 582.
- **Augments:** Power, Resistance penetration, Mana cost.
- **Why:** The single-target payoff, spending the Root it found.

### Worldroot Balm: Tier 6, defensive, all allies

- **Now:** 3 AP, 100 Mana, power 58 + Wis ×0.8 + Mind ×0.3. Restore [X] HP plus 20% of maximum HP to yourself or an ally and increase their healing received by 30% for 3 turns.
- **Reworked:** 3 AP, 100 Mana; power 15 + Wis ×0.88 + Mind ×0.25 (60% of the tier's reference, 75% for all allies, less its effect). Heals every ally for [X] plus 10% of maximum HP and raises their healing received by 20% for 3 turns (was one target).
- **Description:** "Spread the worldroot's balm over the party: every ally regains [X] HP plus 10% of maximum HP and receives 20% more healing for 3 turns."
- **[X], now → reworked:** Level 50: 239 → 205; Level 100: 413 → 387.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** Nature's first party heal (today its first is at Tier 8).

### Summon - High Griffon: Tier 7, summon

- **Now:** 3 AP, 140 Mana, power none. Summon a friendly High Griffon with 10% increased maximum HP. Only one may be active at a time.
- **Reworked:** 3 AP, 140 Mana; power 31 + Wis ×1.82 + Mind ×0.52 (100% of the tier's reference, as a summon). Summons a High Griffon (Level 52, 603 HP today, summoned with +10% HP); its maximum HP rises by 200% of [X] and every attack it makes by 25% of [X] (SP29). It flies: its Evasion is 30% higher, and its first attack each turn strikes a second enemy too (new). Its hits have a 30% chance to **Root**. One at a time.
- **Description:** "Summon a friendly High Griffon whose maximum HP rises by twice [X] and whose attacks deal a quarter of [X] more, each with a 30% chance to Root; it flies, with 30% more Evasion, and its first attack each turn strikes a second enemy. Only one may be active at a time."
- **[X], now → reworked:** Level 60: - → 500; Level 100: - → 802.
- **Augments:** Mana cost, Summon Vitality, Summon Might, Summon Speed.
- **Why:** Nature's great beast, swift and hard to hit.

### Thorn Tempest: Tier 7, offensive, all enemies

- **Now:** 3 AP, 134 Mana, power 64 + Wis ×0.8 + Mind ×0.4. Shred all enemies for [X] Nature damage and lower Dexterity by 20% for 2 turns.
- **Reworked:** 3 AP, 134 Mana; power 20 + Wis ×1.18 + Mind ×0.34 (65% of the tier's reference, 75% for all enemies, less its effect). Every enemy's Dexterity −20% for 2 turns (kept): their weapons miss more.
- **Description:** "Shred all enemies with a thorn tempest for [X] Nature damage and lower their Dexterity by 20% for 2 turns."
- **[X], now → reworked:** Level 60: 294 → 325; Level 100: 442 → 520.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Kept: a guard against a pack of weapon users.

### Heartwood Spear: Tier 7, offensive, one target

- **Now:** 3 AP, 146 Mana, power 82 + Wis ×0.8 + Mind ×0.4. Drive a living spear through one enemy for [X] Nature damage and reduce Armor by 25% for 2 turns.
- **Reworked:** 3 AP, 146 Mana; power 26 + Wis ×1.55 + Mind ×0.44 (85% of the tier's reference). Armor −25% for 2 turns (kept); a 40% chance to **Root** the target for 2 turns.
- **Description:** "Drive a living spear through one enemy for [X] Nature damage, reducing its Armor by 25% for 2 turns, with a 40% chance to Root it."
- **[X], now → reworked:** Level 60: 312 → 425; Level 100: 460 → 682.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Effect chance, Mana cost.
- **Why:** Kept for the party's weapon users, with the element's status.

### Worldbloom: Tier 8, defensive, all allies

- **Now:** 3 AP, 170 Mana, power 70 + Wis ×0.9 + Mind ×0.5. Restore [X] HP plus 20% maximum HP to all allies.
- **Reworked:** 3 AP, 170 Mana; power 25 + Wis ×1.47 + Mind ×0.42 (70% of the tier's reference, 75% for all allies, less its effect). Heals every ally for [X] plus 20% of maximum HP (kept) and raises their healing received by 20% for 3 turns.
- **Description:** "Make the world bloom: every ally regains [X] HP plus 20% of maximum HP and receives 20% more healing for 3 turns."
- **[X], now → reworked:** Level 70: 379 → 465; Level 100: 507 → 648.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** Kept as Nature's great party heal, with the element's growth.

### Ancestor Root: Tier 8, offensive, one target

- **Now:** 3 AP, 190 Mana, power 90 + Wis ×0.9 + Mind ×0.5. Skewer one enemy for [X] Nature damage and reduce Armor by 30% for 2 turns.
- **Reworked:** 3 AP, 190 Mana; power 32 + Wis ×1.89 + Mind ×0.54 (90% of the tier's reference). Always **Roots** the target for 3 turns (was Armor −30%, which is Earth's).
- **Description:** "Skewer one enemy with an ancestor's root for [X] Nature damage and Root it for 3 turns."
- **[X], now → reworked:** Level 70: 399 → 597; Level 100: 527 → 833.
- **Augments:** Power, Resistance penetration, Effect duration, Mana cost.
- **Why:** A long, sure Root on the one enemy the party must hit.

### Elderwood Renewal: Tier 9, defensive, all allies

- **Now:** 3 AP, 240 Mana, power 85 + Wis ×1 + Mind ×0.5. Restore [X] HP plus 10% maximum HP to all allies and increase their Armor by 10% for 2 turns.
- **Reworked:** 3 AP, 240 Mana; power 29 + Wis ×1.7 + Mind ×0.49 (65% of the tier's reference, 75% for all allies, less its effect). Heals every ally for [X] plus 10% of maximum HP and raises their maximum HP by 15% for 3 turns (new; was Armor +10% for 2 turns).
- **Description:** "Renew the party with elderwood: every ally regains [X] HP plus 10% of maximum HP and grows 15% in maximum HP for 3 turns."
- **[X], now → reworked:** Level 80: 466 → 609; Level 100: 558 → 750.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** Verdant Renewal for the whole party.

### Thorn Tyrant: Tier 9, offensive, one target

- **Now:** 3 AP, 260 Mana, power 95 + Wis ×1 + Mind ×0.5. Impale one enemy for [X] Nature damage and reduce healing received by 25% for 2 turns.
- **Reworked:** 3 AP, 260 Mana; power 38 + Wis ×2.23 + Mind ×0.64 (85% of the tier's reference, and 20% of [X] at each of its next 3 turns). The thorns grow inside it: at the start of each of its next 3 turns it takes 20% of [X] (new), and it is always **Rooted** for 3 turns (was healing received −25%).
- **Description:** "Impale one enemy on a tyrant's thorns for [X] Nature damage; they grow in it for 20% of that at the start of each of its next 3 turns, and it is Rooted for 3 turns."
- **[X], now → reworked:** Level 80: 476 → 798; Level 100: 568 → 983.
- **Augments:** Power, Resistance penetration, Effect strength, Effect duration, Mana cost.
- **Why:** Nature's great single-target spell; healing loss is Death's.

### Worldseed Miracle: Tier 10, defensive, all allies

- **Now:** 3 AP, 350 Mana, power 240 + Wis ×1.1 + Mind ×0.6. Restore [X] HP plus 25% maximum HP to all allies and increase all their basic stats by 10% for 3 turns.
- **Reworked:** 3 AP, 350 Mana; power 48 + Wis ×2.81 + Mind ×0.8 (70% of the tier's reference, 75% for all allies, less its effects). Heals every ally for [X] plus 25% of maximum HP and raises all their basic stats by 10% for 3 turns (kept), and removes all of their debuffs (new).
- **Description:** "Bring forth the worldseed's miracle: every ally regains [X] HP plus 25% of maximum HP, is cleansed of every debuff and has all basic stats raised by 10% for 3 turns."
- **[X], now → reworked:** Level 90: 720 → 1,121; Level 100: 772 → 1,238.
- **Augments:** Power, Effect strength, Effect duration, Mana cost.
- **Why:** Nature's Tier 10, a party renewed whole.
